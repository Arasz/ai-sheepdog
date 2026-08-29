using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AiSheepdog;

public sealed partial class DefaultCommand : AsyncCommand
{
    private readonly IAnsiConsole _console;

    public DefaultCommand(IAnsiConsole console)
    {
        _console = console;
    }

    protected override Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        _console.MarkupLine($"ai-sheepdog [bold]{VersionString()}[/]");
        _console.MarkupLine("Work in progress. The sheep are not being herded yet.");
        return Task.FromResult(0);
    }

    /// <summary>
    /// Runs the command app. Output goes to <paramref name="outWriter"/>, usage errors to
    /// <paramref name="errorWriter"/> and the logger. When a command later needs
    /// <c>ILogger</c>, register it through a type registrar then.
    /// </summary>
    public static int MainInternal(string[] args, TextWriter outWriter, TextWriter errorWriter, ILogger? logger)
    {
        // The real process console keeps Spectre's terminal detection (styling in a TTY,
        // plain bytes when piped); a supplied writer gets a deliberately plain console.
        var console = ReferenceEquals(outWriter, Console.Out)
            ? AnsiConsole.Console
            : AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(outWriter),
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Interactive = InteractionSupport.No,
            });
        var app = new CommandApp<DefaultCommand>();
        app.WithDescription("A minimal LLM agent harness (work in progress).");
        app.Configure(configuration =>
        {
            configuration.SetApplicationName("ai-sheepdog");
            configuration.SetApplicationVersion(VersionString());
            configuration.UseStrictParsing();
            configuration.PropagateExceptions();
            configuration.ConfigureConsole(console);
        });
        try
        {
            return app.Run(args);
        }
        catch (CommandParseException exception)
        {
            errorWriter.WriteLine(exception.Message);
            if (logger is not null)
            {
                Log.UsageError(logger, exception.Message);
            }
            return -1;
        }
    }

    private static string VersionString()
    {
        var version = typeof(DefaultCommand).Assembly.GetName().Version!;
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Usage error: {Message}")]
        public static partial void UsageError(ILogger logger, string message);
    }
}
