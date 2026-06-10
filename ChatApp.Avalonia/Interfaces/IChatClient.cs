using ChatApp.Avalonia.Models;

namespace ChatApp.Avalonia.Interfaces
{
    public interface IChatClient
    {
        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int count = 50);
    }
}
