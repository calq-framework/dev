namespace CalqFramework.Dev.Tests;

/// <summary>
///     Tests that DevManager integrates correctly with CalqFramework.Cli.
/// </summary>
public class DevManagerCliTest {
    [Fact]
    public void Execute_Help_ReturnsWithoutError() {
        var cli = new CommandLineInterface {
            InterfaceOut = new StringWriter()
        };
        object? result = cli.Execute(new DevManager(), ["--help"]);
        Assert.IsType<ValueTuple>(result);
    }

    [Theory]
    [InlineData("new")]
    [InlineData("format")]
    [InlineData("pull")]
    [InlineData("push")]
    [InlineData("switch")]
    [InlineData("merge")]
    [InlineData("relock")]
    [InlineData("issues")]
    public void Execute_SubcommandHelp_ReturnsWithoutError(string subcommand) {
        var cli = new CommandLineInterface {
            InterfaceOut = new StringWriter()
        };
        object? result = cli.Execute(new DevManager(), [subcommand, "--help"]);
        Assert.IsType<ValueTuple>(result);
    }

    [Fact]
    public void Execute_Help_ContainsAllSubcommands() {
        var output = new StringWriter();
        var cli = new CommandLineInterface {
            InterfaceOut = output
        };
        cli.Execute(new DevManager(), ["--help"]);

        string helpText = output.ToString();
        Assert.Contains("new", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("format", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pull", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("push", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("switch", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("merge", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("relock", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("issues", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("config", helpText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ConfigHelp_ContainsSubcommands() {
        var output = new StringWriter();
        var cli = new CommandLineInterface {
            InterfaceOut = output
        };
        cli.Execute(new DevManager(), ["config", "--help"]);

        string helpText = output.ToString();
        Assert.Contains("path", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("push", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pull", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preset", helpText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_NewHelp_ContainsParameters() {
        var output = new StringWriter();
        var cli = new CommandLineInterface {
            InterfaceOut = output
        };
        cli.Execute(new DevManager(), ["new", "--help"]);

        string helpText = output.ToString();
        Assert.Contains("type", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lang", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("private", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internal", helpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization", helpText, StringComparison.OrdinalIgnoreCase);
    }
}
