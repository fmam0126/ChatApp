using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.server.DTO;
using Microsoft.Extensions.DependencyInjection;
using ChatApp.server.Models;
using ChatApp.server.Class;

namespace ChatApp.Server.Tests.Controllers;

public class ChatMessagesControllerTests : IClassFixture<HighRateLimitWebApplicationFactory>
{
    private readonly HighRateLimitWebApplicationFactory _factory;

    public ChatMessagesControllerTests(HighRateLimitWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Seeds a user directly in the database and returns a valid JWT for them.
    /// </summary>
    private async Task<string> SeedAndGetToken(HttpClient client, string username, int userId)
    {
        // Seed the user directly in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatContext>();

        // Always add the user (skip the guard — InMemory Any can be unreliable)
        if (!db.Users.Any(u => u.Id == userId))
        {
            db.Users.Add(new User { Id = userId, Name = username });
        }
        else
        {
            // Update existing user's name
            var existing = db.Users.Find(userId);
            if (existing != null)
                existing.Name = username;
        }
        db.SaveChanges();

        // Verify the user was created
        var check = db.Users.Find(userId);

        // Create a valid token for this user
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var user = new User { Id = userId, Name = username };
        return tokenService.GenerateToken(user);
    }

    [Fact]
    public async Task GetMessages_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/ChatMessages");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await SeedAndGetToken(client, "testuser", 100);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        // Act
        var response = await client.GetAsync("/ChatMessages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessageResponseDTO>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Assert
        Assert.NotNull(messages);
    }

    [Fact]
    public async Task PostMessage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var dto = new ChatMessageDTO { Content = "Hello" };

        // Act
        var response = await client.PostAsJsonAsync("/ChatMessages", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostMessage_Authenticated_ReturnsOkWithMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await SeedAndGetToken(client, "author", 42);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new ChatMessageDTO { Content = "Hello, world!" };
        
        // Act
        var response = await client.PostAsJsonAsync("/ChatMessages", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ChatMessageResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(body);
        Assert.Equal("Hello, world!", body!.Content);
        Assert.Equal(42, body.SenderId);
        Assert.Equal("author", body.SenderName);
    }

    [Fact]
    public async Task PostThenGet_ReturnsMessagesInOrder()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await SeedAndGetToken(client, "poster", 1);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        await client.PostAsJsonAsync("/ChatMessages", new ChatMessageDTO { Content = "First" });
        await client.PostAsJsonAsync("/ChatMessages", new ChatMessageDTO { Content = "Second" });

        var response = await client.GetAsync("/ChatMessages");
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessageResponseDTO>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(messages);
        Assert.True(messages!.Count >= 2);

        // Should be in chronological order (oldest first)
        var lastTwo = messages.Skip(messages.Count - 2).ToList();
        Assert.Equal("First", lastTwo[0].Content);
        Assert.Equal("Second", lastTwo[1].Content);
    }
}
