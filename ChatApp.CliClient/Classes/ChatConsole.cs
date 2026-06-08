using System.Collections.Concurrent;
using System.Text;
using Spectre.Console;

namespace ChatApp.CliClient.Classes;

/// <summary>
/// Manages a bottom-anchored chat TUI using cursor control.
/// The bottom two screen lines are reserved for the instruction and input prompt.
/// All chat messages render in the area above and scroll independently.
/// Thread-safe: SignalR callbacks enqueue messages; the main input loop drains and renders them.
/// </summary>
internal sealed class ChatConsole
{
    private readonly ConcurrentQueue<(string User, string Message)> _pending = new();
    private readonly List<string> _visible = new();
    private readonly Lock _renderLock = new();

    private int _msgTop, _msgBottom, _instrLine, _inputLine;
    private string _input = "";
    private int _cursorCol;

    // ── Initialisation ──

    public void Initialize()
    {
        Console.CursorVisible = false;
        AnsiConsole.Cursor.Hide();
        AnsiConsole.Clear();
        RecalcLayout();
    }

    private void RecalcLayout()
    {
        _inputLine   = Math.Max(0, Console.WindowHeight - 1);
        _instrLine   = Math.Max(0, Console.WindowHeight - 2);
        _msgBottom   = Math.Max(0, Console.WindowHeight - 3);
        _msgTop      = 0;
    }

    private int MsgCapacity => Math.Max(1, _msgBottom - _msgTop + 1);

    // ── Thread-safe enqueue (called from SignalR callback) ──

    public void Enqueue(string user, string message)
    {
        _pending.Enqueue((user, message));
    }

    // ── Low-level rendering helpers (caller must hold _renderLock) ──

    private static void ClearRow(int row)
    {
        AnsiConsole.Cursor.SetPosition(0, row);
        AnsiConsole.Write(new string(' ', Math.Max(1, Console.WindowWidth)));
    }

    private static void WriteAt(int col, int row, string markup)
    {
        AnsiConsole.Cursor.SetPosition(col, row);
        AnsiConsole.Markup(markup);
    }

    private static string FormatMessage(string user, string message)
    {
        var now = DateTime.Now.ToString("HH:mm:ss");
        if (user == "System")
            return $"[grey][[{now}]] [italic]{Markup.Escape(message)}[/][/]";
        return $"[grey][[{now}]][/] [red]{Markup.Escape(user)}: [/][white]{Markup.Escape(message)}[/]";
    }

    // ── Drawing the fixed regions ──

    private void DrawMessageArea()
    {
        AnsiConsole.Cursor.Hide();
        // Clear
        for (int r = _msgTop; r <= _msgBottom; r++)
            ClearRow(r);

        // Draw from the bottom up so newest messages appear at the bottom
        int startRow = _msgBottom - _visible.Count + 1;
        for (int i = 0; i < _visible.Count; i++)
            WriteAt(0, startRow + i, _visible[i]);
    }

    private void DrawInstruction()
    {
        AnsiConsole.Cursor.Hide();
        ClearRow(_instrLine);
        AnsiConsole.Cursor.SetPosition(0, _instrLine);
        AnsiConsole.Markup("[grey]Type your message and press Enter. Type [bold]/exit[/] to quit.[/]");
    }

    private void DrawInput()
    {
        AnsiConsole.Cursor.Hide();
        ClearRow(_inputLine);
        // Keep prompt prefix visible even when input is empty
        AnsiConsole.Cursor.SetPosition(0, _inputLine);
        AnsiConsole.Write("> " + _input);
        // Place cursor right after the last character
        AnsiConsole.Cursor.SetPosition(2 + _cursorCol, _inputLine);
    }

    // ── Drain pending messages into visible buffer ──

    private void Drain()
    {
        bool added = false;
        while (_pending.TryDequeue(out var m))
        {
            _visible.Add(FormatMessage(m.User, m.Message));
            added = true;
        }

        while (_visible.Count > MsgCapacity)
            _visible.RemoveAt(0);

        if (added)
        {
            lock (_renderLock)
            {
                DrawMessageArea();
            }
        }
    }

    // ── Input loop ──

    /// <summary>
    /// Reads one line of input with the cursor anchored to the bottom row.
    /// Incoming messages are rendered between keystrokes.
    /// Returns the entered text (empty string if cancelled).
    /// </summary>
    public async Task<string> ReadInputAsync(CancellationToken ct = default)
    {
        AnsiConsole.Cursor.Hide();
        _input = "";
        _cursorCol = 0;

        lock (_renderLock)
        {
            RecalcLayout();                 // handle terminal resize
            DrawInstruction();
            DrawInput();
        }

        while (!ct.IsCancellationRequested)
        {
            // Show any messages that arrived since last loop
            Drain();
            lock (_renderLock) { DrawInput(); }

            // Busy-wait with a short sleep so we can still process incoming messages
            while (!Console.KeyAvailable)
            {
                if (ct.IsCancellationRequested) return "";
                await Task.Delay(50, ct);
                Drain();
                lock (_renderLock) { DrawInput(); }
            }

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                {
                    string result = _input;
                    _input = "";
                    _cursorCol = 0;
                    lock (_renderLock) { ClearRow(_inputLine); }
                    return result;
                }

                case ConsoleKey.Backspace:
                    if (_cursorCol > 0)
                    {
                        _input = _input.Remove(_cursorCol - 1, 1);
                        _cursorCol--;
                    }
                    break;

                case ConsoleKey.Delete:
                    if (_cursorCol < _input.Length)
                        _input = _input.Remove(_cursorCol, 1);
                    break;

                case ConsoleKey.LeftArrow:
                    if (_cursorCol > 0) _cursorCol--;
                    break;

                case ConsoleKey.RightArrow:
                    if (_cursorCol < _input.Length) _cursorCol++;
                    break;

                case ConsoleKey.Home:
                    _cursorCol = 0;
                    break;

                case ConsoleKey.End:
                    _cursorCol = _input.Length;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        _input = _input.Insert(_cursorCol, key.KeyChar.ToString());
                        _cursorCol++;
                    }
                    break;
            }
        }

        return "";
    }
}
