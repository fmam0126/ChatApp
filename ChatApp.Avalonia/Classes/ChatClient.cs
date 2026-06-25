using System.Net.Http.Headers;
using System.Text.Json;
using ChatApp.Avalonia.Interfaces;
using ChatApp.Avalonia.Models;

namespace ChatApp.Avalonia.Services
{
    public class ChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        /// <summary>
        /// Initializes a new instance of the ChatClient class with the specified server URL and access token.
        /// The HttpClient is configured with the server URL as the base address and the access token is set in the Authorization header for authenticated requests.
        /// </summary>
        /// <param name="serverUrl">The URL of the server.</param>
        /// <param name="accessToken">The access token for authenticated requests.</param>
        public ChatClient(string serverUrl, string accessToken)
        {
            _httpClient = new HttpClient(DevSslBypass.CreateHandler())
            {
                BaseAddress = new Uri(serverUrl)
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
        /// <summary>
        /// Asynchronously retrieves a list of chat messages from the server, limited to the specified count.
        /// The method sends a GET request to the server's ChatMessages endpoint with the count parameter
        /// </summary>
        /// <param name="count">The maximum number of messages to retrieve.</param>
        /// <returns>A task representing the asynchronous operation, with the result being the list of chat messages.</returns>
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
