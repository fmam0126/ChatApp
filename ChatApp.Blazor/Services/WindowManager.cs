using ChatApp.Blazor.Models;

namespace ChatApp.Blazor.Services;

public class WindowManager
{
    private readonly List<WindowState> _windows = [];
    private int _nextZIndex = 1;
    private double _cascadeOffset;

    public IReadOnlyList<WindowState> OpenWindows => _windows.AsReadOnly();

    public event Action? OnChanged;

    public WindowState OpenWindow(WindowContentType type, string title, double? width = null, double? height = null)
    {
        var window = new WindowState
        {
            ContentType = type,
            Title = title,
            Width = width ?? 600,
            Height = height ?? 500,
            ZIndex = _nextZIndex++,
            IsActive = true
        };

        // Cascade position with slight offset
        window.X = 120 + _cascadeOffset;
        window.Y = 60 + _cascadeOffset;
        _cascadeOffset = (_cascadeOffset + 24) % 200;

        // Deactivate other windows
        foreach (var w in _windows)
            w.IsActive = false;

        _windows.Add(window);
        OnChanged?.Invoke();
        return window;
    }

    public void CloseWindow(string id)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        _windows.Remove(window);
        OnChanged?.Invoke();
    }

    public void FocusWindow(string id)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        window.ZIndex = _nextZIndex++;
        window.IsMinimized = false;

        foreach (var w in _windows)
            w.IsActive = w.Id == id;

        OnChanged?.Invoke();
    }

    public void MinimizeWindow(string id)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        window.IsMinimized = !window.IsMinimized;
        if (!window.IsMinimized)
        {
            window.IsMaximized = false;
            FocusWindow(id);
        }

        OnChanged?.Invoke();
    }

    public void ToggleMaximize(string id)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        window.IsMaximized = !window.IsMaximized;
        if (window.IsMaximized)
            window.IsMinimized = false;

        FocusWindow(id);
        OnChanged?.Invoke();
    }

    public void ToggleWindow(string id)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        if (window.IsMinimized)
        {
            window.IsMinimized = false;
            FocusWindow(id);
        }
        else if (window.IsActive)
        {
            MinimizeWindow(id);
        }
        else
        {
            FocusWindow(id);
        }
    }

    public void UpdateWindowPosition(string id, double x, double y)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        window.X = x;
        window.Y = y;
    }

    public void UpdateWindowTitle(string id, string title)
    {
        var window = _windows.FirstOrDefault(w => w.Id == id);
        if (window == null) return;

        window.Title = title;
        OnChanged?.Invoke();
    }
}
