using System.Text.Json;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;

namespace AgentTape.Core.Storage;

/// <summary>
/// File-system-based session storage that writes sessions to the .agenttape directory layout.
/// </summary>
public sealed class FileSystemSessionStore : ISessionStore
{
    private readonly AgentTapeOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions JsonLineOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileSystemSessionStore(AgentTapeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<SessionPaths> CreateSessionLayoutAsync(TapeSession session, CancellationToken cancellationToken)
    {
        var root = _options.AgentTapeDirectory;
        var sessionDir = Path.Combine(root, "sessions", session.Id);

        var paths = new SessionPaths
        {
            RootDirectory = root,
            SessionJsonPath = Path.Combine(sessionDir, "session.json"),
            CommandsJsonlPath = Path.Combine(sessionDir, "commands.jsonl"),
            StdoutDirectory = Path.Combine(sessionDir, "stdout"),
            StderrDirectory = Path.Combine(sessionDir, "stderr"),
            GitDirectory = Path.Combine(sessionDir, "git"),
            TestsDirectory = Path.Combine(sessionDir, "tests"),
            ReportsDirectory = Path.Combine(root, "reports")
        };

        Directory.CreateDirectory(sessionDir);
        Directory.CreateDirectory(paths.StdoutDirectory);
        Directory.CreateDirectory(paths.StderrDirectory);
        Directory.CreateDirectory(paths.GitDirectory);
        Directory.CreateDirectory(paths.TestsDirectory);
        Directory.CreateDirectory(paths.ReportsDirectory);

        return Task.FromResult(paths);
    }

    public async Task SaveSessionAsync(TapeSession session, SessionPaths paths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(paths);

        EnsureWithinRoot(paths.RootDirectory, paths.SessionJsonPath);
        EnsureWithinRoot(paths.RootDirectory, paths.CommandsJsonlPath);

        // Write session.json
        var sessionJson = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(paths.SessionJsonPath, sessionJson, cancellationToken);

        // Write commands.jsonl (one JSON object per line)
        await using var commandsStream = new FileStream(paths.CommandsJsonlPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await using var commandsWriter = new StreamWriter(commandsStream);
        foreach (var command in session.Commands)
        {
            var line = JsonSerializer.Serialize(command, JsonLineOptions);
            await commandsWriter.WriteLineAsync(line);
        }

        // Write stdout files (redacted)
        for (var i = 0; i < session.Commands.Count; i++)
        {
            var command = session.Commands[i];
            var stdoutPath = GetStdoutPath(paths, i);
            EnsureWithinRoot(paths.RootDirectory, stdoutPath);
            await File.WriteAllTextAsync(stdoutPath, command.RedactedStdoutPreview ?? string.Empty, cancellationToken);

            var stderrPath = GetStderrPath(paths, i);
            EnsureWithinRoot(paths.RootDirectory, stderrPath);
            await File.WriteAllTextAsync(stderrPath, command.RedactedStderrPreview ?? string.Empty, cancellationToken);
        }

        // Write git diff if available
        if (!string.IsNullOrEmpty(session.AfterGit?.StatusText))
        {
            var diffPath = Path.Combine(paths.GitDirectory, "diff.txt");
            EnsureWithinRoot(paths.RootDirectory, diffPath);
            await File.WriteAllTextAsync(diffPath, session.AfterGit.StatusText, cancellationToken);
        }

        // Write redaction log placeholder
        var redactionLogPath = Path.Combine(Path.GetDirectoryName(paths.SessionJsonPath)!, "redaction-log.json");
        EnsureWithinRoot(paths.RootDirectory, redactionLogPath);
        await File.WriteAllTextAsync(redactionLogPath, "[]", cancellationToken);
    }

    public static string GetStdoutPath(SessionPaths paths, int index)
    {
        return Path.Combine(paths.StdoutDirectory, $"{index + 1:D3}.txt");
    }

    public static string GetStderrPath(SessionPaths paths, int index)
    {
        return Path.Combine(paths.StderrDirectory, $"{index + 1:D3}.txt");
    }

    private static void EnsureWithinRoot(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(path);

        if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path '{path}' is outside the configured agenttape root '{root}'.");
        }
    }
}
