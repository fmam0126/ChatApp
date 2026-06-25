namespace ChatApp.CliClient.Models
{
    /// <summary>
    /// Represents a chat message in the chat application, containing information about the message content, creation time, sender ID, and sender name.
    /// </summary>
    internal class ChatMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier of the chat message. This ID is used to distinguish between different messages in the chat application.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the content of the chat message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the creation time of the chat message.
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Gets or sets the ID of the sender of the chat message.
        /// </summary>
        public int SenderId { get; set; }
        /// <summary>
        /// Gets or sets the name of the sender of the chat message.
        /// </summary>
        public string SenderName { get; set; } = string.Empty;
    }
}
