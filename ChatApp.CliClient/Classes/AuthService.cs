using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatApp.CliClient.Classes;

internal class AuthService
{
    private static readonly HttpClient _httpClient = new(DevSslBypass.CreateHandler());

    /// <summary>
    /// Attempts to log in a user by sending their username and password to the specified server URL.
    /// Returns the authentication token on success, or an error message on failure.
    /// </summary>
    /// <param name="serverUrl">The URL of the server to which to send the login request.</param>
    /// <param name="username">The username of the user attempting to log in.</param>
    /// <param name="password">The password of the user attempting to log in.</param>
    /// <returns>A tuple containing the authentication token (null on failure) and an error message (null on success).</returns>
    public async Task<(string? Token, string? ErrorMessage)> LoginAsync(
        string serverUrl, string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{serverUrl}/auth/login", new { username, password });

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
            return (null, error?.Message ?? "Invalid request.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
            return (null, error?.Message ?? "Login failed.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result?.Token is null)
        {
            return (null, "Invalid response from server.");
        }

        return (result.Token, null);
    }

    /// <summary>
    /// Attempts to register a new user by sending their username and password to the specified server URL.
    /// Returns the authentication token on success, or an error message on failure.
    /// </summary>
    /// <param name="serverUrl">The URL of the server to which to send the registration request.</param>
    /// <param name="username">The desired username for the new account.</param>
    /// <param name="password">The desired password for the new account.</param>
    /// <returns>A tuple containing the authentication token (null on failure) and an error message (null on success).</returns>
    public async Task<(string? Token, string? ErrorMessage)> RegisterAsync(
        string serverUrl, string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{serverUrl}/auth/register", new { username, password });

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
            return (null, error?.Message ?? "Invalid request.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
            return (null, error?.Message ?? "Registration failed.");
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result?.Token is null)
        {
            return (null, "Invalid response from server.");
        }

        return (result.Token, null);
    }

    /// <summary>
    /// Represents the response received from the server upon a successful login attempt,
    /// containing the authentication token, username, and user ID.
    /// </summary>
    private class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// Represents an error response from the server.
    /// </summary>
    private class AuthErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
