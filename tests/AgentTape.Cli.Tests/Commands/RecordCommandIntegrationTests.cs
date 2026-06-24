using System.Diagnostics;
using System.Text.Json;
using AgentTape.Cli.Commands;
using AgentTape.Cli.Parsing;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Core.Storage;
using AgentTape.Git.Snapshots;
using AgentTape.Redaction.Rules;
using AgentTape.Reporting.Html;
using AgentTape.Reporting.Markdown;
using AgentTape.Rules.Risk;
using AgentTape.Testing.DotNet;

namespace AgentTape.Cli.Tests.Commands;

/// <summary>
/// End-to-end integration tests for the record command flow.
/// Uses real (temporary) directories, git repos, and the actual RecordCommand.
/// </summary>
public sealed class RecordCommandIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    public RecordCommandIntegrationTests()
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
    public async Task Record_generates_session_layout_for_successful_command()
    {
        var parseResult = CreateParseResult("dotnet", ["--version"]);
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_tempDir, ".agenttape", "reports", "latest.md")));
        Assert.True(File.Exists(Path.Combine(_tempDir, ".agenttape", "reports", "latest.html")));

        // Verify session.json exists in the sessions directory
        var sessionsDir = Path.Combine(_tempDir, ".agenttape", "sessions");
        Assert.True(Directory.Exists(sessionsDir));
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        Assert.NotEmpty(sessionDirs);
        Assert.True(File.Exists(Path.Combine(sessionDirs[0], "session.json")));
    }

    [Fact]
    public async Task Record_returns_wrapped_exit_code_for_failing_command()
    {
        // dotnet with invalid args exits non-zero
        var parseResult = CreateParseResult("dotnet", ["--invalid-flag-xyz-123"]);
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        // Should return the dotnet exit code (non-zero)
        Assert.NotEqual(0, exitCode);
        Assert.NotEqual(CommandExitCodes.InternalFailure, exitCode);
    }

    [Fact]
    public async Task Record_generates_report_in_non_git_directory()
    {
        // _tempDir is NOT a git repo
        var parseResult = CreateParseResult("dotnet", ["--version"]);
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        Assert.Equal(0, exitCode);
        var reportPath = Path.Combine(_tempDir, ".agenttape", "reports", "latest.md");
        Assert.True(File.Exists(reportPath));

        var reportContent = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("AgentTape Session Summary", reportContent);
    }

    [Fact]
    public async Task Record_captures_git_diff_in_git_repository()
    {
        // Initialize a git repo
        RunGit("init");
        RunGit("config", "user.email", "test@agenttape.dev");
        RunGit("config", "user.name", "AgentTape Test");

        // Create and commit a file
        var filePath = Path.Combine(_tempDir, "test.cs");
        await File.WriteAllTextAsync(filePath, "// original");
        RunGit("add", ".");
        RunGit("commit", "-m", "Initial commit");

        // Modify the file so there's a diff
        await File.WriteAllTextAsync(filePath, "// modified");

        var parseResult = CreateParseResult("dotnet", ["--version"]);
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        Assert.Equal(0, exitCode);

        // Verify diff exists in session git directory
        var sessionsDir = Path.Combine(_tempDir, ".agenttape", "sessions");
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        Assert.NotEmpty(sessionDirs);
        var gitDir = Path.Combine(sessionDirs[0], "git");
        Assert.True(Directory.Exists(gitDir));
    }

    [Fact]
    public async Task Record_redacts_secret_output_before_report_generation()
    {
        // Write a script that outputs a fake secret
        var scriptPath = Path.Combine(_tempDir, "echo_secret.cmd");
        await File.WriteAllTextAsync(scriptPath, "@echo off\r\necho token=ghp_abc123def456ghijklmnopqrstuv\r\necho Done.");

        var parseResult = CreateParseResult("cmd", ["/c", scriptPath]);
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        // Verify reports don't contain the raw secret
        var reportPath = Path.Combine(_tempDir, ".agenttape", "reports", "latest.md");
        if (File.Exists(reportPath))
        {
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.DoesNotContain("ghp_abc123", content);
        }

        var htmlPath = Path.Combine(_tempDir, ".agenttape", "reports", "latest.html");
        if (File.Exists(htmlPath))
        {
            var content = await File.ReadAllTextAsync(htmlPath);
            Assert.DoesNotContain("ghp_abc123", content);
        }
    }

    [Fact]
    public async Task Record_respects_no_git_option()
    {
        // Initialize a git repo
        RunGit("init");
        RunGit("config", "user.email", "test@agenttape.dev");
        RunGit("config", "user.name", "AgentTape Test");

        var parseResult = CreateParseResult("dotnet", ["--version"]) with { NoGit = true };
        var options = new AgentTapeOptions { AgentTapeDirectory = Path.Combine(_tempDir, ".agenttape") };
        var cmd = CreateRecordCommand(options);

        var exitCode = await cmd.ExecuteAsync(parseResult, CancellationToken.None);

        Assert.Equal(0, exitCode);

        // Verify session JSON shows no git repo
        var sessionsDir = Path.Combine(_tempDir, ".agenttape", "sessions");
        var sessionDirs = Directory.GetDirectories(sessionsDir);
        Assert.NotEmpty(sessionDirs);
        var sessionJson = await File.ReadAllTextAsync(Path.Combine(sessionDirs[0], "session.json"));
        Assert.Contains("\"isRepository\": false", sessionJson);
    }

    private static CliParseResult CreateParseResult(string executable, string[] arguments)
    {
        return new CliParseResult
        {
            IsSuccess = true,
            Command = "record",
            WrappedExecutable = executable,
            WrappedArguments = arguments,
            Redact = "standard"
        };
    }

    private static RecordCommand CreateRecordCommand(AgentTapeOptions options)
    {
        var clock = new AgentTape.Core.SystemClock();
        return new RecordCommand(
            clock,
            new AgentTape.Core.ProcessCommandRunner(clock),
            new GitCliSnapshotProvider(),
            new RegexRedactor(),
            new FileSystemSessionStore(options),
            new MarkdownReportGenerator(),
            new HtmlReportGenerator(),
            new DotNetTestOutputDetector(),
            new DefaultRiskRules(),
            options);
    }

    private void RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in arguments)
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}");
        }
    }
}
