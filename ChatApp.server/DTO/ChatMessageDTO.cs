namespace ChatApp.server.DTO
{
    public class ChatMessageDTO
    {
        public string Content { get; set; } = string.Empty;
    }

    public class ChatMessageResponseDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
    }

    public class LoginRequestDTO
    {
        public string Username { get; set; } = string.Empty;
    }

    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
