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

        // Register the user first
        await client.PostAsJsonAsync("/auth/register", request);

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

        // Register the user first (username will be trimmed to "validuser")
        await client.PostAsJsonAsync("/auth/register", request);

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
        var registerRequest = new LoginRequestDTO { Username = "returning", Password = "password123" };

        // Register the user first
        await client.PostAsJsonAsync("/auth/register", registerRequest);

        // Act
        // First login
        var responseUserCreation = await client.PostAsJsonAsync("/auth/login", registerRequest);

        Assert.Equal(HttpStatusCode.OK, responseUserCreation.StatusCode);
        var bodyCreation = await responseUserCreation.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true});


        // Second login should find existing user and return same UserId
        var responseUserLogin = await client.PostAsJsonAsync("/auth/login", registerRequest);

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

    // ── Register tests ──────────────────────────────────────────

    [Fact]
    public async Task Register_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "newuser", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Equal("newuser", body.Username);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Register_TooShortUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "ab", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_TooLongUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = new string('x', 31), Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhitespaceOnlyUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "   ", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UsernameWithLeadingTrailingSpaces_ReturnsTrimmedOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "  freshuser  ", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal("freshuser", body!.Username);
    }

    [Fact]
    public async Task Register_TooShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "validuser", Password = "short" }; // 5 chars

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "validuser", Password = "" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhitespaceOnlyPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "validuser", Password = "        " };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "duplicate", Password = "password123" };

        // First registration succeeds
        var first = await client.PostAsJsonAsync("/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Act - second registration with same username
        var second = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_MinimumValidUsername_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "abc", Password = "password123" }; // exactly 3 chars

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal("abc", body!.Username);
    }

    [Fact]
    public async Task Register_MaximumValidUsername_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var username = new string('y', 30); // exactly 30 chars
        var request = new LoginRequestDTO { Username = username, Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(username, body!.Username);
    }

    [Fact]
    public async Task Register_MinimumValidPassword_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDTO { Username = "passwordtest", Password = "12345678" }; // exactly 8 chars

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.False(string.IsNullOrEmpty(body!.Token));
    }
}
