using Spectre.Console;
using ChatApp.CliClient.Models;

namespace ChatApp.CliClient.Classes;

internal static class SpectreDisplay
{
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

    private static Style GetUserColor(string username)
    {
        var hash = Math.Abs(username.GetHashCode());
        return UserColors[hash % UserColors.Length];
    }

    public static void ShowWelcome()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("ChatApp")
                .Centered()
                .Color(Color.Cyan1));
        AnsiConsole.Write(new Rule("[grey]Real-time Chat CLI[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

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
            var panel = new Panel(Markup.Escape(message))
            {
                Header = new PanelHeader($"{username}  [{now}]"),
                BorderStyle = color,
            };
            AnsiConsole.Write(panel);
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

    public static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[red][[!]] {Markup.Escape(message)}[/]");
    }

    public static void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue][[i]] {Markup.Escape(message)}[/]");
    }

    public static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green][[✓]] {Markup.Escape(message)}[/]");
    }
}
