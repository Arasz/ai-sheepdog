using AiSheepdog;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Xunit;

namespace AiSheepdog.Tests;

public sealed class DefaultCommandTests
{
    private static CommandAppTester NewApp()
    {
        var app = new CommandAppTester();
        app.Configure(configuration =>
        {
            configuration.SetApplicationName("ai-sheepdog");
            configuration.SetApplicationVersion(ExpectedVersion());
            configuration.UseStrictParsing();
            configuration.PropagateExceptions();
        });
        app.SetDefaultCommand<DefaultCommand>("A minimal LLM agent harness (work in progress).");
        return app;
    }

    private static string ExpectedVersion()
    {
        var version = typeof(DefaultCommand).Assembly.GetName().Version!;
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    [Fact]
    public void Default_command_prints_banner_and_exits_successfully()
    {
        var result = NewApp().Run();

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldContain($"ai-sheepdog {ExpectedVersion()}");
        result.Output.ShouldContain("Work in progress. The sheep are not being herded yet.");
    }

    [Fact]
    public void Version_flag_prints_only_the_bare_version()
    {
        var result = NewApp().Run("--version");

        result.ExitCode.ShouldBe(0);
        result.Output.Trim().ShouldBe(ExpectedVersion());
    }

    [Fact]
    public void Help_names_the_application_and_describes_it()
    {
        var result = NewApp().Run("--help");

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldContain("ai-sheepdog");
        result.Output.ShouldNotContain("AiSheepdog.dll");
        result.Output.ShouldContain("DESCRIPTION:");
    }

    [Fact]
    public void Unknown_flag_fails_instead_of_being_swallowed()
    {
        Should.Throw<CommandParseException>(() => NewApp().Run("--bogus"));
    }

    [Fact]
    public void Stray_positional_fails_as_unknown_command()
    {
        Should.Throw<CommandParseException>(() => NewApp().Run("frobnicate"));
    }

    [Fact]
    public void MainInternal_runs_the_banner_through_the_supplied_writers()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = DefaultCommand.MainInternal([], stdout, stderr, null);

        exit.ShouldBe(0);
        stdout.ToString().ShouldContain($"ai-sheepdog {ExpectedVersion()}");
        stderr.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void MainInternal_routes_usage_errors_to_stderr_and_keeps_spectres_exit_code()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = DefaultCommand.MainInternal(["--bogus"], stdout, stderr, null);

        exit.ShouldBe(-1);
        stderr.ToString().ShouldContain("Unknown option");
        stdout.ToString().ShouldBeEmpty();
    }
}
