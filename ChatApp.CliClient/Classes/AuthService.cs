using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatApp.CliClient.Classes;

internal class AuthService
{
    private static readonly HttpClient _httpClient = new(DevSslBypass.CreateHandler());
    /// <summary>
    /// Attempts to log in a user by sending their username to the specified server URL. 
    /// If the username is already taken, it returns null; otherwise, it returns the authentication token received from the server.
    /// </summary>
    /// <param name="serverUrl">The URL of the server to which to send the login request.</param>
    /// <param name="username">The username of the user attempting to log in.</param>
    /// <returns>The authentication token if login is successful; otherwise, null.</returns>
    public async Task<string?> LoginAsync(string serverUrl, string username)
    {
        var response = await _httpClient.PostAsJsonAsync($"{serverUrl}/auth/login", new { username });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return null; // Username taken
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        //Console.WriteLine(result.Token);
        return result?.Token;
    }
    /// <summary>
    /// Represents the response received from the server upon a successful login attempt, containing the authentication token.
    /// </summary>
    private class LoginResponse
    {
        /// <summary>
        /// Gets or sets the authentication token received from the server upon a successful login attempt. 
        /// This token is used for subsequent authenticated requests to the server.
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
