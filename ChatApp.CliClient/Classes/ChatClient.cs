using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using ChatApp.CliClient.Interfaces;
using ChatApp.CliClient.Models;

namespace ChatApp.CliClient.Classes
{
    internal class ChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        public ChatClient(HttpClient httpClient) => _httpClient = httpClient;
        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync()
        {
            var response =  await _httpClient.GetAsync("/ChatMessages");
            // Process the response and return the messages
            response.EnsureSuccessStatusCode();
            var messages = new List<ChatMessage>();
            messages = await JsonSerializer.DeserializeAsync<List<ChatMessage>>(await response.Content.ReadAsStreamAsync());
            return messages;

        }

        public async Task<bool> SendMessageAsync(string message)
        {
            // Implementation for sending a message

            var chatMessage = new ChatMessage
            {
                content = message,
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(chatMessage), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/ChatMessages", jsonContent);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
    }
}
