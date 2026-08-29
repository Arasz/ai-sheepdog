using Spectre.Console;
using Spectre.Console.Cli;

namespace AiSheepdog;

public sealed class DefaultCommand : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var version = typeof(DefaultCommand).Assembly.GetName().Version!;
        AnsiConsole.MarkupLine($"ai-sheepdog [bold]{version.Major}.{version.Minor}.{version.Build}[/]");
        AnsiConsole.MarkupLine("Work in progress. The sheep are not being herded yet.");
        return 0;
    }
}
