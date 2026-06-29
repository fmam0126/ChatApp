using Spectre.Console;
using ChatApp.CliClient.Models;

namespace ChatApp.CliClient.Classes;

internal static class SpectreDisplay
{
    /// <summary>
    /// Defines a set of colors to be used for different users in the chat application. Each user will be assigned a color based on their username, allowing for easy identification of messages from different users in the console display.
    /// </summary>
    private static readonly Style[] UserColors =
    [
        new(Color.Aqua),
        new(Color.Chartreuse1),
        new(Color.Fuchsia),
        new(Color.Gold1),
        new(Color.MediumPurple),
        new(Color.DeepSkyBlue1),
        new(Color.HotPink),
        new(Color.Orange1),
    ];
    /// <summary>
    /// Calculates a color for a given username by hashing the username and using the hash value to select a color from the predefined UserColors array. 
    /// This ensures that each user is consistently assigned the same color based on their username, allowing for easy identification of messages in the console display.
    /// </summary>
    /// <param name="username">The username for which to calculate a color.</param>
    /// <returns>The calculated color style.</returns>
    public static Style GetUserColor(string username)
    {
        var hash = Math.Abs(username.GetHashCode());
        return UserColors[hash % UserColors.Length];
    }
    /// <summary>
    /// Displays a welcome message in the console using ASCII art and a rule line. 
    /// </summary>
    public static void ShowWelcome()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("GamingChat!!!")
                .Centered()
                .Color(Color.Cyan1));
        AnsiConsole.Write(new Rule("[grey]Real-time Chat CLI[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    // unused
    /// <summary>
    /// Renders a chat message in the console with a timestamp, username, and message content.
    /// If the username is "System", the message is displayed in grey italics. Otherwise, the message is displayed with the username in red and the message content in white, using a color assigned to the user based on their username.
    /// </summary>
    /// <param name="username">The username of the message sender.</param>
    /// <param name="message">The message content.</param>
    public static void RenderMessage(string username, string message)
    {
        var now = DateTime.Now.ToString("HH:mm:ss");

        if (username == "System")
        {
            AnsiConsole.MarkupLine($"[grey][[{now}]] [italic]{Markup.Escape(message)}[/][/]");
        }
        else
        {

            var color = GetUserColor(username);


            AnsiConsole.MarkupLine($"[grey][[{now}]][/] [{color.ToMarkup()}]{Markup.Escape(username)}: [/][white]{Markup.Escape(message)}[/]");
            //var panel = new Panel(Markup.Escape(message))
            //{
            //    Header = new PanelHeader($"{username}  [{now}]"),
            //    BorderStyle = color,
            //};
            //AnsiConsole.Write(panel);
        }
    }

    public static void RenderHistory(IEnumerable<ChatMessage> messages)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Time[/]").Width(10))
            .AddColumn(new TableColumn("[bold]User[/]").Width(15))
            .AddColumn(new TableColumn("[bold]Message[/]"));

        foreach (var msg in messages)
        {
            var time = msg.Created.ToLocalTime().ToString("HH:mm:ss");
            var color = GetUserColor(msg.SenderName);
            table.AddRow(
                new Markup($"[grey]{time}[/]"),
                new Markup($"[{color.ToMarkup()}]{Markup.Escape(msg.SenderName)}[/]"),
                new Markup(Markup.Escape(msg.Content)));
        }

        if (!messages.Any())
        {
            table.AddRow("[grey]-[/]", "[grey]-[/]", "[grey]No messages yet[/]");
        }

        AnsiConsole.Write(new Rule("[grey]Chat History[/]").RuleStyle("grey"));
        AnsiConsole.Write(table);
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    public static async Task ShowSpinner(string text, Func<Task> action)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(text, async _ => await action());
    }

    public static string Prompt(string text)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold]{text}[/]")
                .PromptStyle("cyan"));
    }

    public static string PromptSecret(string text)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold]{text}[/]")
                .PromptStyle("cyan")
                .Secret());
    }
    public static string PromptForMessage(string text, string defaultValue = "")
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold]{text}[/]")
                .PromptStyle("cyan")
                .DefaultValue(defaultValue));
    }

    public static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"\n[red][[!]] {Markup.Escape(message)}[/]");
    }

    public static void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"\n[blue][[i]] {Markup.Escape(message)}[/]");
    }

    public static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"\n[green][[✓]] {Markup.Escape(message)}[/]");
    }
}
