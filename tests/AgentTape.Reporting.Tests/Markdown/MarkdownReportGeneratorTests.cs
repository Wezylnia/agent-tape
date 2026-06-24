using AgentTape.Core.Models;
using AgentTape.Reporting.Markdown;

namespace AgentTape.Reporting.Tests.Markdown;

public sealed class MarkdownReportGeneratorTests
{
    private readonly MarkdownReportGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_includes_session_name()
    {
        var session = CreateBasicSession();
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("sample", markdown);
    }

    [Fact]
    public async Task GenerateAsync_includes_duration()
    {
        var session = CreateBasicSession();
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("Duration", markdown);
    }

    [Fact]
    public async Task GenerateAsync_includes_branch_when_available()
    {
        var session = CreateBasicSession() with
        {
            AfterGit = new GitSnapshot { IsRepository = true, Branch = "main", HeadSha = "abc1234" }
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("main", markdown);
    }

    [Fact]
    public async Task GenerateAsync_lists_commands()
    {
        var session = CreateBasicSession();
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("dotnet test", markdown);
    }

    [Fact]
    public async Task GenerateAsync_lists_changed_files()
    {
        var session = CreateBasicSession() with
        {
            FileChanges = [new FileChange { Path = "src/program.cs", Kind = FileChangeKind.Modified }]
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("program.cs", markdown);
    }

    [Fact]
    public async Task GenerateAsync_lists_warnings()
    {
        var session = CreateBasicSession() with
        {
            Warnings = [new RiskWarning { Code = "TEST_WARN", Message = "Test warning", Severity = RiskSeverity.Warning }]
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("TEST_WARN", markdown);
    }

    [Fact]
    public async Task GenerateAsync_includes_test_summary_when_available()
    {
        var session = CreateBasicSession() with
        {
            TestSummaries = [new TestSummary { Total = 42, Passed = 40, Failed = 1, Skipped = 1 }]
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("42", markdown);
    }

    [Fact]
    public async Task GenerateAsync_omits_empty_test_section_when_no_signal()
    {
        var session = CreateBasicSession();
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("No test signals detected", markdown);
    }

    [Fact]
    public async Task GenerateAsync_escapes_markdown_table_pipes()
    {
        var session = CreateBasicSession() with
        {
            FileChanges = [new FileChange { Path = "src|test.cs", Kind = FileChangeKind.Modified }]
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        Assert.Contains("src\\|test.cs", markdown);
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
                    Command = "echo ***",
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = 0,
                    RedactedStdoutPreview = "[REDACTED]",
                    RedactedStderrPreview = ""
                }
            ]
        };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        // Secret fixture values like ghp_abc123 should never appear in report output
        Assert.DoesNotContain("ghp_", markdown);
        Assert.DoesNotContain("AKIA", markdown);
        Assert.DoesNotContain("sk-", markdown);
    }

    [Fact]
    public void Escape_handles_special_characters()
    {
        Assert.Equal("test\\|pipe", MarkdownReportGenerator.Escape("test|pipe"));
        Assert.Equal("back\\`tick", MarkdownReportGenerator.Escape("back`tick"));
    }

    [Fact]
    public void Escape_handles_formatting_characters()
    {
        Assert.Equal("\\*bold\\*", MarkdownReportGenerator.Escape("*bold*"));
        Assert.Equal("\\#heading", MarkdownReportGenerator.Escape("#heading"));
        Assert.Equal("\\~strike\\~", MarkdownReportGenerator.Escape("~strike~"));
        Assert.Equal("\\[link\\]", MarkdownReportGenerator.Escape("[link]"));
        Assert.Equal("block\\>quote", MarkdownReportGenerator.Escape("block>quote"));
    }

    [Fact]
    public async Task GenerateAsync_escapes_markdown_in_session_name()
    {
        var session = CreateBasicSession() with { Name = "# DANGER *bold*" };
        var markdown = await _generator.GenerateAsync(session, CancellationToken.None);
        // The escaped version should appear, not the raw heading marker
        Assert.Contains("\\# DANGER", markdown);
    }

    [Fact]
    public void FormatDuration_handles_milliseconds()
    {
        var result = MarkdownReportGenerator.FormatDuration(TimeSpan.FromMilliseconds(500));
        Assert.Contains("ms", result);
    }

    [Fact]
    public void FormatDuration_handles_minutes()
    {
        var result = MarkdownReportGenerator.FormatDuration(TimeSpan.FromMinutes(2.5));
        Assert.Contains("m", result);
    }

    private static TapeSession CreateBasicSession()
    {
        return new TapeSession
        {
            Id = "test",
            Name = "sample",
            StartedAt = DateTimeOffset.UnixEpoch,
            FinishedAt = DateTimeOffset.UnixEpoch.AddSeconds(3),
            WorkingDirectory = "/repo",
            Commands =
            [
                new CommandRun
                {
                    Id = "001",
                    Command = "dotnet test",
                    StartedAt = DateTimeOffset.UnixEpoch,
                    FinishedAt = DateTimeOffset.UnixEpoch.AddSeconds(3),
                    ExitCode = 0
                }
            ]
        };
    }
}

