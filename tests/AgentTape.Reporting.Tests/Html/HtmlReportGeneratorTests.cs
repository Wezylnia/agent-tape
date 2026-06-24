using AgentTape.Core.Models;
using AgentTape.Reporting.Html;

namespace AgentTape.Reporting.Tests.Html;

public sealed class HtmlReportGeneratorTests
{
    private readonly HtmlReportGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_outputs_valid_html_shell()
    {
        var session = CreateBasicSession();
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.StartsWith("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public async Task GenerateAsync_html_encodes_session_name()
    {
        var session = CreateBasicSession() with { Name = "<script>alert('xss')</script>" };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task GenerateAsync_html_encodes_command_text()
    {
        var session = CreateBasicSession() with
        {
            Commands =
            [
                new CommandRun
                {
                    Id = "c1",
                    Command = "echo <unsafe> & more",
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = 0
                }
            ]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.DoesNotContain("<unsafe>", html);
        Assert.Contains("&lt;unsafe&gt;", html);
    }

    [Fact]
    public async Task GenerateAsync_html_encodes_file_paths()
    {
        var session = CreateBasicSession() with
        {
            FileChanges = [new FileChange { Path = "src/<unsafe>.cs", Kind = FileChangeKind.Modified }]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.DoesNotContain("<unsafe>", html);
    }

    [Fact]
    public async Task GenerateAsync_html_encodes_warning_messages()
    {
        var session = CreateBasicSession() with
        {
            Warnings = [new RiskWarning { Code = "TEST", Message = "<script>alert(1)</script>", Severity = RiskSeverity.High }]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.DoesNotContain("<script>alert", html);
    }

    [Fact]
    public async Task GenerateAsync_lists_commands()
    {
        var session = CreateBasicSession();
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("dotnet test", html);
    }

    [Fact]
    public async Task GenerateAsync_lists_file_changes()
    {
        var session = CreateBasicSession() with
        {
            FileChanges = [new FileChange { Path = "program.cs", Kind = FileChangeKind.Modified }]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("program.cs", html);
    }

    [Fact]
    public async Task GenerateAsync_lists_warnings()
    {
        var session = CreateBasicSession() with
        {
            Warnings = [new RiskWarning { Code = "TEST_WARN", Message = "Test warning", Severity = RiskSeverity.Warning }]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("TEST_WARN", html);
    }

    [Fact]
    public async Task GenerateAsync_includes_empty_states()
    {
        var session = CreateBasicSession() with { Commands = Array.Empty<CommandRun>() };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("No commands captured", html);
    }

    [Fact]
    public async Task GenerateAsync_does_not_include_raw_secret_fixture()
    {
        var session = CreateBasicSession() with
        {
            Commands =
            [
                new CommandRun
                {
                    Id = "c1",
                    Command = "set TOKEN=***",
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = 0,
                    RedactedStdoutPreview = "[REDACTED]",
                    RedactedStderrPreview = ""
                }
            ]
        };
        var html = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.DoesNotContain("ghp_", html);
        Assert.DoesNotContain("AKIA", html);
        Assert.DoesNotContain("sk-", html);
    }

    private static TapeSession CreateBasicSession()
    {
        return new TapeSession
        {
            Id = "test",
            Name = "sample",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            WorkingDirectory = "/repo",
            Commands =
            [
                new CommandRun
                {
                    Id = "001",
                    Command = "dotnet test",
                    StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = 0
                }
            ]
        };
    }
}
