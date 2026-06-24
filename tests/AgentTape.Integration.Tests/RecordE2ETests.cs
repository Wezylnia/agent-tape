using System.Diagnostics;
using AgentTape.Cli.Parsing;
using AgentTape.Core;
using AgentTape.Core.Configuration;
using AgentTape.Core.Storage;
using AgentTape.Git.Snapshots;
using AgentTape.Redaction.Rules;
using AgentTape.Reporting.Html;
using AgentTape.Reporting.Markdown;
using AgentTape.Rules.Risk;
using AgentTape.Testing.DotNet;
using AgentTape.Cli.Commands;

namespace AgentTape.Integration.Tests;

/// <summary>
/// End-to-end integration tests that exercise the full record workflow.
/// </summary>
public sealed class RecordE2ETests : IDisposable
{
    private readonly string _tempDir;

    public RecordE2ETests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"agenttape-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public async Task Record_creates_session_and_reports_with_no_git()
    {
        var agenttapeDir = Path.Combine(_tempDir, ".agenttape");
        var options = new AgentTapeOptions { AgentTapeDirectory = agenttapeDir };
        var cmd = CreateRecordCommand(options);

        var parseResult = CliParser.Parse(["--config", "nonexistent.yml", "record", "--name", "e2e-test", "--no-git", "--", "dotnet", "--version"]);
        Assert.True(parseResult.IsSuccess);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);
        Assert.Equal(0, exitCode);

        // Verify session directory exists
        var sessionsDir = Path.Combine(agenttapeDir, "sessions");
        Assert.True(Directory.Exists(sessionsDir));
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        Assert.NotEmpty(sessionDirs);

        // Verify session.json exists
        Assert.True(File.Exists(Path.Combine(sessionDirs[0], "session.json")));

        // Verify reports exist
        var reportsDir = Path.Combine(agenttapeDir, "reports");
        Assert.True(File.Exists(Path.Combine(reportsDir, "latest.html")));
        Assert.True(File.Exists(Path.Combine(reportsDir, "latest.md")));

        // Verify redaction log exists
        Assert.True(File.Exists(Path.Combine(sessionDirs[0], "redaction-log.json")));
    }

    [Fact]
    public async Task Record_in_git_repo_captures_branch_and_changes()
    {
        // Initialize git repo
        RunGit("init", _tempDir);
        RunGit("config user.email test@agenttape.dev", _tempDir);
        RunGit("config user.name Test", _tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "hello");
        RunGit("add file.txt", _tempDir);
        RunGit("commit -m initial", _tempDir);

        var agenttapeDir = Path.Combine(_tempDir, ".agenttape");
        var options = new AgentTapeOptions { AgentTapeDirectory = agenttapeDir };
        var cmd = CreateRecordCommand(options);

        var parseResult = CliParser.Parse(["record", "--name", "git-test", "--", "dotnet", "--version"]);
        Assert.True(parseResult.IsSuccess);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);
        Assert.Equal(0, exitCode);

        // Verify git info captured
        var sessionsDir = Path.Combine(agenttapeDir, "sessions");
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        var sessionJson = await File.ReadAllTextAsync(Path.Combine(sessionDirs[0], "session.json"));
        Assert.Contains("branch", sessionJson.ToLowerInvariant());
    }

    [Fact]
    public async Task Record_redacts_secrets_in_output()
    {
        var agenttapeDir = Path.Combine(_tempDir, ".agenttape");
        var options = new AgentTapeOptions { AgentTapeDirectory = agenttapeDir };
        var cmd = CreateRecordCommand(options);

        // Create a script that outputs fake secrets to stdout
        var scriptPath = Path.Combine(_tempDir, "emit-secret.cmd");
        await File.WriteAllTextAsync(scriptPath, "@echo off\necho Token: ghp_abcdefghijklmnopqrstuvwxyz123456");

        var parseResult = CliParser.Parse(["record", "--name", "secret-test", "--no-git", "--", scriptPath]);
        Assert.True(parseResult.IsSuccess);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);
        Assert.Equal(0, exitCode);

        // Verify secrets are redacted in stored files
        var sessionsDir = Path.Combine(agenttapeDir, "sessions");
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        var sessionJson = await File.ReadAllTextAsync(Path.Combine(sessionDirs[0], "session.json"));

        Assert.DoesNotContain("ghp_abcdefghijklmnopqrstuvwxyz123456", sessionJson);
        Assert.Contains("ghp_***", sessionJson);

        // Check redaction log
        var redactionLog = await File.ReadAllTextAsync(Path.Combine(sessionDirs[0], "redaction-log.json"));
        Assert.Contains("GitHub Classic Token", redactionLog);
    }

    private static RecordCommand CreateRecordCommand(AgentTapeOptions options)
    {
        var clock = new SystemClock();
        return new RecordCommand(
            clock,
            new ProcessCommandRunner(clock),
            new GitCliSnapshotProvider(),
            new RegexRedactor(),
            new FileSystemSessionStore(options),
            new MarkdownReportGenerator(),
            new HtmlReportGenerator(),
            new DotNetTestOutputDetector(),
            new DefaultRiskRules(),
            options);
    }

    private static void RunGit(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
    }
}
