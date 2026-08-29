using AiSheepdog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Xunit;

namespace AiSheepdog.Tests;

public sealed class DefaultCommandTests
{
    [Fact]
    public void Default_command_exits_successfully()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<DefaultCommand>();

        var result = app.Run();

        result.ExitCode.ShouldBe(0);
    }
}
