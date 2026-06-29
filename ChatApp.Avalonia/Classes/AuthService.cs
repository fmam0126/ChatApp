using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatApp.Avalonia.Services;

/// <summary>
/// Provides authentication services for logging in users with username and password
/// to the chat server. Returns a structured <see cref="AuthResult"/> on success or failure.
/// </summary>
public class AuthService
{
    private static readonly HttpClient _httpClient = new(DevSslBypass.CreateHandler());

    /// <summary>
    /// Attempts to log in a user by sending their username and password to the specified server URL.
    /// Returns an <see cref="AuthResult"/> indicating success (with token, username, and user ID)
    /// or failure (with an error message from the server).
    /// </summary>
    /// <param name="serverUrl">The URL of the server to which to send the login request.</param>
    /// <param name="username">The username of the user attempting to log in.</param>
    /// <param name="password">The password of the user attempting to log in.</param>
    /// <returns>An <see cref="AuthResult"/> containing the authentication token and user details on success,
    /// or an error message on failure.</returns>
    public async Task<AuthResult> LoginAsync(string serverUrl, string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync($"{serverUrl}/auth/login", new { username, password });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
            return AuthResult.Failure(error?.Message ?? "Login failed.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result?.Token is null)
        {
            return AuthResult.Failure("Invalid response from server.");
        }

        return AuthResult.Success(result.Token, result.Username ?? username, result.UserId);
    }

    /// <summary>
    /// Represents the response received from the server upon a successful login attempt,
    /// containing the authentication token, username, and user ID.
    /// </summary>
    private class LoginResponse
    {
        /// <summary>
        /// Gets or sets the authentication token received from the server upon a successful login attempt.
        /// This token is used for subsequent authenticated requests to the server.
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username returned by the server.
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the user ID assigned by the server.
        /// </summary>
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// Represents an error response from the server.
    /// </summary>
    private class AuthErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message from the server.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Gets whether the authentication attempt was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets the JWT token on a successful authentication; otherwise null.
    /// </summary>
    public string? Token { get; private init; }

    /// <summary>
    /// Gets the username returned by the server on a successful authentication; otherwise null.
    /// </summary>
    public string? Username { get; private init; }

    /// <summary>
    /// Gets the user ID returned by the server on a successful authentication; otherwise 0.
    /// </summary>
    public int UserId { get; private init; }

    /// <summary>
    /// Gets the error message on a failed authentication; otherwise null.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Creates a successful <see cref="AuthResult"/>.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <param name="username">The username returned by the server.</param>
    /// <param name="userId">The user ID returned by the server.</param>
    public static AuthResult Success(string token, string username, int userId) => new()
    {
        IsSuccess = true,
        Token = token,
        Username = username,
        UserId = userId
    };

    /// <summary>
    /// Creates a failed <see cref="AuthResult"/> with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public static AuthResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
