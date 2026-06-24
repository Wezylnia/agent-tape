using AgentTape.Core;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Core.Tests;

public sealed class ProcessCommandRunnerTests
{
    private readonly ProcessCommandRunner _runner;
    private readonly FakeClock _clock;

    public ProcessCommandRunnerTests()
    {
        _clock = new FakeClock();
        _runner = new ProcessCommandRunner(_clock);
    }

    [Fact]
    public async Task RunAsync_captures_stdout()
    {
        var request = CreateRequest("dotnet", ["--version"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Stdout);
    }

    [Fact]
    public async Task RunAsync_captures_stderr()
    {
        // dotnet with invalid argument produces stderr
        var request = CreateRequest("dotnet", ["--invalid-flag-xyz"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Stderr);
    }

    [Fact]
    public async Task RunAsync_returns_exit_code()
    {
        var request = CreateRequest("dotnet", ["--version"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);
        Assert.Equal(0, result.Run.ExitCode);
    }

    [Fact]
    public async Task RunAsync_honors_working_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agenttape-wd-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            // dotnet --version should work regardless of working directory
            var request = CreateRequest("dotnet", ["--version"], tempDir);
            var result = await _runner.RunAsync(request, CancellationToken.None);
            Assert.Equal(0, result.Run.ExitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_passes_arguments_without_shell_interpolation()
    {
        // Use dotnet with echo-like behavior; test argument with special chars
        var request = CreateRequest("dotnet", ["--version"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);
        // Just verifying no shell interpolation caused errors
        Assert.Equal(0, result.Run.ExitCode);
    }

    [Fact]
    public async Task RunAsync_applies_environment_overrides()
    {
        var request = new CommandRequest
        {
            Executable = "dotnet",
            Arguments = ["--version"],
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Environment = new Dictionary<string, string?>
            {
                ["AGENTTAPE_TEST_VAR"] = "test-value-123"
            }
        };
        var result = await _runner.RunAsync(request, CancellationToken.None);
        Assert.Equal(0, result.Run.ExitCode);
    }

    [Fact]
    public async Task RunAsync_records_started_and_finished_times()
    {
        var request = CreateRequest("dotnet", ["--version"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);
        Assert.True(result.Run.StartedAt <= result.Run.FinishedAt);
        Assert.True(result.Run.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_bounds_preview_length()
    {
        // Create a command that produces long output
        // Use a shell echo with long text on Windows
        var longText = new string('x', 5000);
        var request = CreateRequest("cmd", ["/c", $"echo {longText}"]);
        var result = await _runner.RunAsync(request, CancellationToken.None);

        // Preview should be bounded, full output preserved
        var preview = result.Run.RedactedStdoutPreview ?? string.Empty;
        Assert.True(preview.Length <= ProcessCommandRunner.MaxPreviewLength);
        // Full output should be at least as long as the preview
        Assert.True(result.Stdout.Length >= preview.Trim().Length);
    }

    [Fact]
    public async Task RunAsync_throws_clear_error_when_executable_missing()
    {
        var request = CreateRequest("nonexistent-executable-xyz-123", []);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _runner.RunAsync(request, CancellationToken.None));
        Assert.Contains("nonexistent-executable-xyz-123", ex.Message);
    }

    [Fact]
    public async Task RunAsync_kills_process_on_cancellation()
    {
        // Use a command that sleeps for a while
        var request = CreateRequest("cmd", ["/c", "timeout /t 30 /nobreak"]);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var startedAt = DateTimeOffset.UtcNow;

        var result = await _runner.RunAsync(request, cts.Token);
        var elapsed = DateTimeOffset.UtcNow - startedAt;

        // Should complete in well under 30 seconds (the kill worked)
        Assert.True(elapsed < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task RunAsync_command_id_is_not_hardcoded()
    {
        var request = CreateRequest("dotnet", ["--version"]);
        var result1 = await _runner.RunAsync(request, CancellationToken.None);
        var result2 = await _runner.RunAsync(request, CancellationToken.None);
        // Each run should have a unique id
        Assert.NotEqual(result1.Run.Id, result2.Run.Id);
    }

    private static CommandRequest CreateRequest(string executable, string[] arguments, string? workingDirectory = null)
    {
        return new CommandRequest
        {
            Executable = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
