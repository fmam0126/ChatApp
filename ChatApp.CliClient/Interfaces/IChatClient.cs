using ChatApp.CliClient.Models;

namespace ChatApp.CliClient.Interfaces
{
    internal interface IChatClient
    {
        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int count = 50);
    }
}
