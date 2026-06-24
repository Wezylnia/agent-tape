using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Tests.Parsing;

public sealed class CliParserTests
{
    [Fact]
    public void Parse_init_accepts_no_options()
    {
        var result = CliParser.Parse(["init"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("init", result.Command);
    }

    [Fact]
    public void Parse_record_requires_separator()
    {
        var result = CliParser.Parse(["record", "dotnet", "--version"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("--", result.ErrorMessage);
    }

    [Fact]
    public void Parse_record_extracts_wrapped_executable()
    {
        var result = CliParser.Parse(["record", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("dotnet", result.WrappedExecutable);
    }

    [Fact]
    public void Parse_record_preserves_wrapped_arguments()
    {
        var result = CliParser.Parse(["record", "--", "dotnet", "test", "--filter", "Category=Unit"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("dotnet", result.WrappedExecutable);
        Assert.Equal(["test", "--filter", "Category=Unit"], result.WrappedArguments);
    }

    [Fact]
    public void Parse_record_accepts_name()
    {
        var result = CliParser.Parse(["record", "--name", "my-session", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("my-session", result.Name);
    }

    [Fact]
    public void Parse_record_rejects_missing_name_value()
    {
        var result = CliParser.Parse(["record", "--name", "--", "dotnet"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("--name requires a value", result.ErrorMessage);
    }

    [Fact]
    public void Parse_record_rejects_unknown_option()
    {
        var result = CliParser.Parse(["record", "--verbose", "--", "dotnet"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("Unknown option", result.ErrorMessage);
    }

    [Fact]
    public void Parse_record_defaults_to_standard_redaction()
    {
        var result = CliParser.Parse(["record", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("standard", result.Redact);
    }

    [Fact]
    public void Parse_record_accepts_strict_redaction()
    {
        var result = CliParser.Parse(["record", "--redact", "strict", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("strict", result.Redact);
    }

    [Fact]
    public void Parse_record_accepts_off_redaction()
    {
        var result = CliParser.Parse(["record", "--redact", "off", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("off", result.Redact);
    }

    [Fact]
    public void Parse_record_rejects_invalid_redaction_mode()
    {
        var result = CliParser.Parse(["record", "--redact", "none", "--", "dotnet"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid redaction mode", result.ErrorMessage);
    }

    [Fact]
    public void Parse_report_accepts_html()
    {
        var result = CliParser.Parse(["report", "--html"]);
        Assert.True(result.IsSuccess);
        Assert.True(result.Html);
    }

    [Fact]
    public void Parse_export_requires_format()
    {
        var result = CliParser.Parse(["export"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("--format", result.ErrorMessage);
    }

    [Fact]
    public void Parse_export_rejects_unknown_format()
    {
        var result = CliParser.Parse(["export", "--format", "xml"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid export format", result.ErrorMessage);
    }
}
