using ChatApp.server.Class;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace ChatApp.Server.Tests;

public class NormalRateLimitWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSecretKey = "TestSecretKeyForIntegrationTests-1234567890!";
    public const string TestIssuer = "TestIssuer";
    public const string TestAudience = "TestAudience";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = TestSecretKey,
                ["JwtSettings:Issuer"] = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
                ["JwtSettings:ExpirationMinutes"] = "60",
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all ChatContext-related services (including internal EF Core descriptors)
            var toRemove = services
                .Where(d => d.ServiceType == typeof(ChatContext) ||
                            d.ServiceType == typeof(DbContextOptions<ChatContext>) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GenericTypeArguments.Length > 0 &&
                             d.ServiceType.GenericTypeArguments.Contains(typeof(ChatContext))))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }
            
            // Use SQLite in-memory for tests (supports real relational behavior)
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            services.AddDbContext<ChatContext>(options =>
                options.UseSqlite(connection));


            // Override rate limiter policies
            var rateLimitConfigurators = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>))
                .ToList();

            foreach (var descriptor in rateLimitConfigurators)
            {
                services.Remove(descriptor);
            }

            services.Configure<RateLimiterOptions>(options =>
            {
                options.RejectionStatusCode = 429;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromSeconds(1),
                        }));
                
                options.AddFixedWindowLimiter("LoginPolicy", opt =>
                {
                    opt.PermitLimit = 4;
                    opt.Window = TimeSpan.FromSeconds(1); // set to 1 second for test
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });
            });
        });
    }

    /// <summary>
    /// Creates an HttpClient with a valid JWT token for the given user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string username = "testuser", int userId = 1)
    {
        var tokenService = new TokenService(TestSecretKey, TestIssuer, TestAudience, 60);
        var user = new User { Id = userId, Name = username };
        var token = tokenService.GenerateToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates a JWT token string for the given user.
    /// </summary>
    public string CreateToken(string username = "testuser", int userId = 1)
    {
        var tokenService = new TokenService(TestSecretKey, TestIssuer, TestAudience, 60);
        var user = new User { Id = userId, Name = username };
        return tokenService.GenerateToken(user);
    }
}
