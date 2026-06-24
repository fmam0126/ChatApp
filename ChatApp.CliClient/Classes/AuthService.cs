using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatApp.CliClient.Classes;

internal class AuthService
{
    private static readonly HttpClient _httpClient = new(DevSslBypass.CreateHandler());

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

    private class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
