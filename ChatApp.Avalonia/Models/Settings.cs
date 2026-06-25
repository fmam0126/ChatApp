namespace ChatApp.Avalonia.Models
{
    /// <summary>
    /// Represents the settings for the chat application, including the server URL.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the URL of the server that the chat application connects to.
        /// This URL is used to establish a connection with the server for sending and receiving chat messages.
        /// </summary>
        public required string ServerUrl { get; set; }
    }
}
