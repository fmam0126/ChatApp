namespace ChatApp.Blazor.Models;

public enum WindowContentType
{
    Chat
}

public class WindowState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public WindowContentType ContentType { get; set; }
    public double Width { get; set; } = 500;
    public double Height { get; set; } = 400;
    public double X { get; set; }
    public double Y { get; set; }
    public int ZIndex { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsActive { get; set; }
}
