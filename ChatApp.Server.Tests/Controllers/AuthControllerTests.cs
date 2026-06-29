using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.server.Class;
using ChatApp.server.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Server.Tests.Controllers;

public class AuthControllerTests : IClassFixture<HighRateLimitWebApplicationFactory>
{
    private readonly HighRateLimitWebApplicationFactory _factory;

    public AuthControllerTests(HighRateLimitWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidUsername_ReturnsOkWithToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "testuser", Password = "password123" };
        
        // Act
        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        // Assert
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Equal("testuser", body.Username);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Login_TooShortUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "ab", Password = "password123" }; // less than 3 chars
        
        // Act
        
        var response = await client.PostAsJsonAsync("/auth/login", request);
        
        // Assert
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_TooLongUsername_ReturnsBadRequest()
    {
        // Arrange
        
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = new string('x', 31), Password = "password123" }; // more than 30 chars

        // Act

        var response = await client.PostAsJsonAsync("/auth/login", request);

        // Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhitespaceOnlyUsername_ReturnsBadRequest()
    {
        // Arrange
        
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "   ", Password = "password123" };
        
        // Act
        
        var response = await client.PostAsJsonAsync("/auth/login", request);

        // Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_UsernameWithLeadingTrailingSpaces_ReturnsTrimmedOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "  validuser  ", Password = "password123" };
        
        // Act

        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        // Assert

        Assert.Equal("validuser", body!.Username);
    }

    [Fact]
    public async Task Login_SameUsernameTwice_UsesExistingUser()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        // First login creates the user
        var responseUserCreation = await client.PostAsJsonAsync("/auth/login", new LoginRequestDTO { Username = "returning", Password = "password123" });

        Assert.Equal(HttpStatusCode.OK, responseUserCreation.StatusCode);
        var bodyCreation = await responseUserCreation.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true});


        // Second login should find existing user and return same UserId
        var responseUserLogin = await client.PostAsJsonAsync("/auth/login", new LoginRequestDTO { Username = "returning", Password = "password123" });

        Assert.Equal(HttpStatusCode.OK, responseUserLogin.StatusCode);

        var bodyUserLogin = await responseUserLogin.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Assert

        Assert.Equal("returning", bodyUserLogin!.Username);
        Assert.Equal(bodyCreation!.UserId, bodyUserLogin.UserId);
        Assert.Equal(bodyCreation.Username, bodyUserLogin.Username);

    }

    [Fact]
    public async Task Login_TakenUsername_ReturnsConflict()
    {
        // Arrange
        // Pre-populate ConnectedUsersService with a "connected" user
        var connectedUsers = _factory.Services.GetRequiredService<ConnectedUsersService>();
        connectedUsers.TryAddUser("takenuser", "fake-connection-id");
        // Act
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "takenuser", Password = "password123" };

        var response = await client.PostAsJsonAsync("/auth/login", request);
        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
