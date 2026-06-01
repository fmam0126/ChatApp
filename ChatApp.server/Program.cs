using ChatApp.server.Hubs;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme);
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

builder.Services.AddSignalR();

builder.Services.AddDbContext<ChatContext>(options =>
    options.UseInMemoryDatabase("ChatDatabase"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = BearerTokenDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = BearerTokenDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    // Configure the Authority to the expected value for
    // the authentication provider. This ensures the token
    // is appropriately validated.
    options.Authority = "Authority URL"; // TODO: Update URL

    // We have to hook the OnMessageReceived event in order to
    // allow the JWT authentication handler to read the access
    // token from the query string when a WebSocket or 
    // Server-Sent Events request comes in.

    // Sending the access token in the query string is required when using WebSockets or ServerSentEvents
    // due to a limitation in Browser APIs. We restrict it to only calls to the
    // SignalR hub in this code.
    // See https://docs.microsoft.com/aspnet/core/signalr/security#access-token-logging
    // for more information about security considerations when using
    // the query string to transmit the access token.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chatHub")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();
