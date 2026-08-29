using Spectre.Console;

namespace AiSheepdog;

/// <summary>Maps an out-buffer over a TextWriter so a console can be pointed at a chosen stream.</summary>
public sealed class AnsiConsoleOutput(TextWriter writer) : IAnsiConsoleOutput
{
    public TextWriter Writer { get; private set; } = writer;

    public bool IsTerminal => false;

    public int Width => 120;

    public int Height => 30;

    public void Close() { }

    public void SetEncoding(System.Text.Encoding encoding) { }
}
