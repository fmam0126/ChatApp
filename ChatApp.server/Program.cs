using ChatApp.server.Class;
using ChatApp.server.Hubs;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Reconfigure to make appsettings.json optional — all settings can be provided via
// environment variables (e.g. JwtSettings__SecretKey, ConnectionStrings__DefaultConnection).
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

if (args.Length > 0)
{
    builder.Configuration.AddCommandLine(args);
}

// Bind JWT settings from configuration (may come from appsettings.json or environment variables)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
var expirationMinutes = jwtSettings["ExpirationMinutes"];

// Validate required configuration
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(
        "JWT SecretKey is required. Set JwtSettings__SecretKey environment variable or configure it in appsettings.json.");

if (string.IsNullOrWhiteSpace(issuer))
    throw new InvalidOperationException(
        "JWT Issuer is required. Set JwtSettings__Issuer environment variable or configure it in appsettings.json.");

if (string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException(
        "JWT Audience is required. Set JwtSettings__Audience environment variable or configure it in appsettings.json.");

if (!int.TryParse(expirationMinutes, out var expires))
    throw new InvalidOperationException(
        "JWT ExpirationMinutes is required and must be an integer. Set JwtSettings__ExpirationMinutes environment variable or configure it in appsettings.json.");

var keyBytes = Encoding.UTF8.GetBytes(secretKey);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization using the bearer scheme"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

builder.Services.AddSignalR(options =>
{
    options.AddFilter<JwtExpirationFilter>();
});
// Global rate limiting: 10 requests per second per user (or IP if unauthenticated)
builder.Services.AddRateLimiter((options) =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(1)

            });
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.AutoReplenishment = true;
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});


// PostgreSQL Database context
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Read token from query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/chatHub", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
// OpenTelemetry
const string serviceName = "Chat";

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddConsoleExporter();
});
builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(serviceName))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter())
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddMeter("ChatApp.SignalR")
          .AddConsoleExporter()
          .AddPrometheusExporter());

// Custom services

builder.Services.AddSingleton<ChatMetrics>();
builder.Services.AddSingleton(sp =>
    new TokenService(secretKey, issuer, audience, expires));
builder.Services.AddSingleton<ConnectedUsersService>();

var app = builder.Build();

// Ensure database is created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!string.Equals(builder.Configuration["DISABLE_HTTPS_REDIRECTION"], "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

// Wire up active-connections observable gauge
app.Services.GetRequiredService<ChatMetrics>()
    .CreateActiveConnectionsGauge(() =>
        app.Services.GetRequiredService<ConnectedUsersService>().ActiveCount);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.MapPrometheusScrapingEndpoint();

app.Run();
