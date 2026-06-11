namespace ChatApp.Blazor.Services;

public class ChatState
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
}
