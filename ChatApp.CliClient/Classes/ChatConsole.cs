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
    /// <summary>
    /// A thread-safe queue that holds pending chat messages to be displayed in the console.
    /// Each message is represented as a tuple containing the timestamp, user name, and message content
    /// </summary>
    private readonly ConcurrentQueue<(DateTime Timestamp, string User, string Message)> _pending = new();
    /// <summary>
    /// A list that holds the currently visible chat messages in the console.
    /// This list is used to manage the messages that are displayed in the chat area of the console, allowing for scrolling and rendering of messages as they arrive.
    /// </summary>
    private readonly List<string> _visible = new();
    /// <summary>
    /// A lock object used to synchronize access to the console rendering operations.
    /// This lock ensures that only one thread can perform rendering operations at a time, preventing concurrent modifications to the console output and maintaining the integrity of the displayed messages.
    /// </summary>
    private readonly Lock _renderLock = new();
    /// <summary>
    /// The following fields are used to manage the layout and state of the chat console, including the positions of the message area, instruction line, input line, and the current input text and cursor position.
    /// </summary>
    private int _msgTop, _msgBottom, _instrLine, _inputLine;
    /// <summary>
    /// The current input text entered by the user in the chat console. 
    /// </summary>
    private string _input = "";
    /// <summary>
    /// The current column position of the cursor within the input text.
    /// This value is used to track the position of the cursor as the user types, allowing for proper rendering of the input line and cursor movement within the chat console.
    /// </summary>
    private int _cursorCol;

    // ── Initialisation ──
    /// <summary>
    /// Initializes the chat console by setting up the console environment, hiding the cursor, clearing the console, and recalculating the layout of the message area, instruction line, and input line based on the current console window size.
    /// This method prepares the console for displaying chat messages and user input in a structured manner, ensuring that the layout is properly configured for the chat application.
    /// </summary>
    public void Initialize()
    {
        Console.CursorVisible = false;
        AnsiConsole.Cursor.Hide();
        AnsiConsole.Clear();
        RecalcLayout();
    }
    /// <summary>
    /// Recalculates the layout of the chat console based on the current console window size.
    /// This method updates the positions of the message area, instruction line, and input line to ensure that they are properly aligned and displayed within the console window.
    /// It is called whenever the console window is resized or when the layout needs to be adjusted to accommodate changes in the console dimensions.
    /// </summary>
    private void RecalcLayout()
    {
        _inputLine = Math.Max(0, Console.WindowHeight - 1);
        _instrLine = Math.Max(0, Console.WindowHeight - 2);
        _msgBottom = Math.Max(0, Console.WindowHeight - 3);
        _msgTop = 0;
    }
    /// <summary>
    /// Gets the maximum number of visible chat messages that can be displayed in the message area of the console.
    /// This value is calculated based on the positions of the top and bottom rows of the message area.
    /// </summary>
    private int MsgCapacity => Math.Max(1, _msgBottom - _msgTop + 1);

    // ── Thread-safe enqueue (called from SignalR callback) ──
    /// <summary>
    /// Enqueues a new chat message to be displayed in the console.
    /// This method is thread-safe and can be called from SignalR callbacks or other threads to add new messages to the pending queue. 
    /// The message will be processed and rendered in the console during the next input loop iteration.
    /// </summary>
    /// <param name="timestamp"></param>
    /// <param name="user"></param>
    /// <param name="message"></param>
    public void Enqueue(DateTime timestamp, string user, string message)
    {
        _pending.Enqueue((timestamp, user, message));
    }

    // ── Low-level rendering helpers (caller must hold _renderLock) ──
    /// <summary>
    /// Clears the specified row in the console by setting the cursor position to the beginning of the row and writing a string of spaces to overwrite any existing content.
    /// </summary>
    /// <param name="row">The row to clear.</param>
    private static void ClearRow(int row)
    {
        AnsiConsole.Cursor.SetPosition(0, row);
        AnsiConsole.Write(new string(' ', Math.Max(1, Console.WindowWidth)));
    }
    /// <summary>
    /// Writes the specified markup string at the given column and row in the console.
    /// This method sets the cursor position to the specified coordinates and uses the AnsiConsole.Markup method to render the provided markup string, allowing for formatted output in the console.
    /// </summary>
    /// <param name="col">The column to write at.</param>
    /// <param name="row">The row to write at.</param>
    /// <param name="markup">The markup string to write.</param>
    private static void WriteAt(int col, int row, string markup)
    {
        AnsiConsole.Cursor.SetPosition(col, row);
        AnsiConsole.Markup(markup);
    }
    /// <summary>
    /// Formats a chat message for display in the console, including the timestamp, user name, and message content.
    /// The formatted message is returned as a string with appropriate markup for rendering in the console.
    /// </summary>
    /// <param name="timestamp">The timestamp of the message.</param>
    /// <param name="user">The user who sent the message.</param>
    /// <param name="message">The message content.</param>
    /// <returns>The formatted message string.</returns>
    private static string FormatMessage(DateTime timestamp, string user, string message)
    {
        var timestampString = timestamp.ToString("HH:mm:ss");
        if (user == "System")
            return $"[grey][[{timestampString}]] [italic]{Markup.Escape(message)}[/][/]";
        return $"[grey][[{timestampString}]][/] [{SpectreDisplay.GetUserColor(user).ToMarkup()}]{Markup.Escape(user)}: [/][white]{Markup.Escape(message)}[/]";
    }

    // ── Drawing the fixed regions ──
    /// <summary>
    /// Draws the message area of the chat console, clearing any existing content and rendering the currently visible chat messages from the bottom up.
    /// </summary>
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
    /// <summary>
    /// Draws the instruction line of the chat console, clearing any existing content and displaying a message that provides guidance to the user on how to interact with the chat application.
    /// </summary>
    private void DrawInstruction()
    {
        AnsiConsole.Cursor.Hide();
        ClearRow(_instrLine);
        AnsiConsole.Cursor.SetPosition(0, _instrLine);
        AnsiConsole.Markup("[grey]Type your message and press Enter. Type [bold]/exit[/] to quit.[/]");
    }
    /// <summary>
    /// Draws the input line of the chat console, clearing any existing content and displaying the current input text entered by the user.
    /// </summary>
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
    /// <summary>
    /// Drains the pending chat messages from the thread-safe queue into the visible buffer, ensuring that the visible buffer does not exceed the maximum message capacity.
    /// If new messages are added to the visible buffer, the message area is redrawn to reflect the updated content. 
    /// This method is called during the input loop to process and display incoming messages in a thread-safe manner.
    /// </summary>
    private void Drain()
    {
        bool added = false;
        while (_pending.TryDequeue(out var m))
        {
            _visible.Add(FormatMessage(m.Timestamp, m.User, m.Message));
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
            //lock (_renderLock) { DrawInput(); }

            // Busy-wait with a short sleep so we can still process incoming messages
            while (!Console.KeyAvailable)
            {
                if (ct.IsCancellationRequested) return "";
                await Task.Delay(50, ct);
                Drain();
                //lock (_renderLock) { DrawInput(); }
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
            lock (_renderLock)
            {
                DrawInput();
            }
        }

        return "";
    }
}
