using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Core.Storage;

namespace AgentTape.Core.Tests.Storage;

public sealed class FileSystemSessionStoreTests : IDisposable
{
    private readonly string _tempRoot;

    public FileSystemSessionStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"agenttape-tests-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task CreateSessionLayoutAsync_creates_all_required_directories()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();

        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "sessions", session.Id)));
        Assert.True(Directory.Exists(paths.StdoutDirectory));
        Assert.True(Directory.Exists(paths.StderrDirectory));
        Assert.True(Directory.Exists(paths.GitDirectory));
        Assert.True(Directory.Exists(paths.ReportsDirectory));
    }

    [Fact]
    public async Task SaveSessionAsync_writes_session_json()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        Assert.True(File.Exists(paths.SessionJsonPath));
        var content = await File.ReadAllTextAsync(paths.SessionJsonPath);
        Assert.Contains(session.Id, content);
        Assert.Contains(session.Name, content);
    }

    [Fact]
    public async Task SaveSessionAsync_writes_commands_jsonl()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        Assert.True(File.Exists(paths.CommandsJsonlPath));
        var lines = await File.ReadAllLinesAsync(paths.CommandsJsonlPath);
        Assert.Single(lines);
    }

    [Fact]
    public async Task SaveSessionAsync_writes_stdout_and_stderr_files()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        var stdoutPath = FileSystemSessionStore.GetStdoutPath(paths, 0);
        var stderrPath = FileSystemSessionStore.GetStderrPath(paths, 0);
        Assert.True(File.Exists(stdoutPath));
        Assert.True(File.Exists(stderrPath));
    }

    [Fact]
    public async Task SaveSessionAsync_writes_valid_json()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        var content = await File.ReadAllTextAsync(paths.SessionJsonPath);
        // Verify the file contains valid JSON by parsing it as a JsonDocument
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal(System.Text.Json.JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task SaveSessionAsync_does_not_escape_root_directory()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        // Try to save to a path outside root
        var badPaths = paths with { SessionJsonPath = Path.Combine(Path.GetTempPath(), "escape.json") };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.SaveSessionAsync(session, badPaths, CancellationToken.None));
    }

    [Fact]
    public async Task SaveSessionAsync_preserves_redacted_output_only()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession();
        session = session with
        {
            Commands =
            [
                session.Commands[0] with
                {
                    RedactedStdoutPreview = "[REDACTED] build output",
                    RedactedStderrPreview = "[REDACTED] error"
                }
            ]
        };
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        var stdoutContent = await File.ReadAllTextAsync(FileSystemSessionStore.GetStdoutPath(paths, 0));
        Assert.Equal("[REDACTED] build output", stdoutContent);
    }

    [Fact]
    public async Task SaveSessionAsync_handles_zero_commands()
    {
        var options = new AgentTapeOptions { AgentTapeDirectory = _tempRoot };
        var store = new FileSystemSessionStore(options);
        var session = CreateSampleSession() with { Commands = Array.Empty<CommandRun>() };
        var paths = await store.CreateSessionLayoutAsync(session, CancellationToken.None);

        await store.SaveSessionAsync(session, paths, CancellationToken.None);

        Assert.True(File.Exists(paths.SessionJsonPath));
        Assert.True(File.Exists(paths.CommandsJsonlPath));
        var lines = await File.ReadAllLinesAsync(paths.CommandsJsonlPath);
        Assert.Empty(lines);
    }

    private static TapeSession CreateSampleSession()
    {
        return new TapeSession
        {
            Id = "2026-06-24-142001-fix-tests",
            Name = "fix-tests",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            WorkingDirectory = "/tmp/repo",
            Commands =
            [
                new CommandRun
                {
                    Id = "001",
                    Command = "dotnet test",
                    StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = 0,
                    RedactedStdoutPreview = "All tests passed",
                    RedactedStderrPreview = ""
                }
            ]
        };
    }
}
