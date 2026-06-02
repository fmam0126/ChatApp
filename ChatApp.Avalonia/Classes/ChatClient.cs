using System.Net.Http.Headers;
using System.Text.Json;
using ChatApp.Avalonia.Interfaces;
using ChatApp.Avalonia.Models;

namespace ChatApp.Avalonia.Services
{
    public class ChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;

        public ChatClient(string serverUrl, string accessToken)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(serverUrl)
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int count = 50)
        {
            var response = await _httpClient.GetAsync($"/ChatMessages?count={count}");
            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var messages = await JsonSerializer.DeserializeAsync<List<ChatMessage>>(
                await response.Content.ReadAsStreamAsync(), options);
            return messages ?? [];
        }
    }
}
