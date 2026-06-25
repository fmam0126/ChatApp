using ChatApp.CliClient.Models;

namespace ChatApp.CliClient.Interfaces
{
    /// <summary>
    /// Defines the contract for a chat client that can retrieve chat messages from a server. 
    /// Implementations of this interface are responsible for handling the communication with the server and returning the list of chat messages.
    /// </summary>
    internal interface IChatClient
    {
        /// <summary>
        /// Asynchronously retrieves a list of chat messages from the server, limited to the specified count.
        /// </summary>
        /// <param name="count">The maximum number of messages to retrieve.</param>
        /// <returns>A task representing the asynchronous operation, with the result being the list of chat messages.</returns>
        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int count = 50);
    }
}
