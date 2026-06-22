namespace ChatApp.server.DTO
{
    public class ChatMessageResponseDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
    }
}