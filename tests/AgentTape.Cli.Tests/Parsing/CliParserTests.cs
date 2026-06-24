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
    public void Parse_record_does_not_default_redaction()
    {
        var result = CliParser.Parse(["record", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Redact);
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

    [Fact]
    public void Parse_record_accepts_no_git_option()
    {
        var result = CliParser.Parse(["record", "--no-git", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.True(result.NoGit);
    }

    [Fact]
    public void Parse_report_accepts_markdown()
    {
        var result = CliParser.Parse(["report", "--markdown"]);
        Assert.True(result.IsSuccess);
        Assert.True(result.Markdown);
    }

    [Fact]
    public void Parse_report_accepts_open()
    {
        var result = CliParser.Parse(["report", "--open"]);
        Assert.True(result.IsSuccess);
        Assert.True(result.Open);
    }

    [Fact]
    public void Parse_init_rejects_extra_arguments()
    {
        var result = CliParser.Parse(["init", "extra"]);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Parse_export_accepts_valid_markdown_format()
    {
        var result = CliParser.Parse(["export", "--format", "markdown"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("markdown", result.Format);
    }

    [Fact]
    public void Parse_export_accepts_valid_json_format()
    {
        var result = CliParser.Parse(["export", "--format", "json"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("json", result.Format);
    }

    [Fact]
    public void Parse_record_rejects_empty_command_after_separator()
    {
        var result = CliParser.Parse(["record", "--"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("No command to record", result.ErrorMessage);
    }

    [Fact]
    public void Parse_record_accepts_multiple_options_combined()
    {
        var result = CliParser.Parse(["record", "--name", "my-session", "--no-git", "--redact", "strict", "--", "dotnet", "test"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("my-session", result.Name);
        Assert.True(result.NoGit);
        Assert.Equal("strict", result.Redact);
        Assert.Equal("dotnet", result.WrappedExecutable);
        Assert.Contains("test", result.WrappedArguments);
    }

    [Fact]
    public void Parse_accepts_global_config_option_for_record()
    {
        var result = CliParser.Parse(["--config", "custom.yml", "record", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("custom.yml", result.ConfigPath);
        Assert.Equal("record", result.Command);
    }

    [Fact]
    public void Parse_accepts_global_config_option_for_report()
    {
        var result = CliParser.Parse(["--config", "custom.yml", "report", "--html"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("custom.yml", result.ConfigPath);
        Assert.Equal("report", result.Command);
    }

    [Fact]
    public void Parse_accepts_global_config_option_for_export()
    {
        var result = CliParser.Parse(["--config", "custom.yml", "export", "--format", "markdown"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("custom.yml", result.ConfigPath);
        Assert.Equal("export", result.Command);
    }

    [Fact]
    public void Parse_rejects_config_without_value()
    {
        var result = CliParser.Parse(["--config", "--", "record", "--", "dotnet"]);
        Assert.False(result.IsSuccess);
        Assert.Contains("--config requires a value", result.ErrorMessage);
    }

    [Fact]
    public void Parse_record_preserves_config_after_separator_as_wrapped_argument()
    {
        var result = CliParser.Parse(["record", "--", "dotnet", "--config", "test.yml"]);
        Assert.True(result.IsSuccess);
        Assert.Null(result.ConfigPath);
        Assert.Equal(["--config", "test.yml"], result.WrappedArguments);
    }

    [Fact]
    public void Parse_record_accepts_config_before_separator()
    {
        var result = CliParser.Parse(["record", "--config", "test.yml", "--", "dotnet"]);
        Assert.True(result.IsSuccess);
        Assert.Equal("test.yml", result.ConfigPath);
        Assert.Equal("dotnet", result.WrappedExecutable);
    }
}
