using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;
using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the show command: displays details for a specific session.
/// </summary>
public static class ShowCommand
{
    public static async Task<int> ExecuteAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new AgentTapeOptions();
        var reader = new Core.Storage.FileSystemSessionReader(options);

        var sessionId = parseResult.SessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            Console.Error.WriteLine("Usage: agenttape show <session-id>");
            return CommandExitCodes.UsageError;
        }

        var session = await reader.FindAsync(sessionId, cancellationToken);
        if (session is null)
        {
            Console.Error.WriteLine($"Session not found: {sessionId}");
            return CommandExitCodes.UsageError;
        }

        var sessionDir = Path.Combine(options.AgentTapeDirectory, "sessions", sessionId);
        var reportsDir = Path.Combine(sessionDir, "reports");

        Console.WriteLine($"Session: {session.Id}");
        Console.WriteLine($"Name: {session.Name}");
        Console.WriteLine($"Started: {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Duration: {FormatDuration(session.Duration)}");
        Console.WriteLine($"Working directory: {session.WorkingDirectory}");
        Console.WriteLine($"Commands: {session.Commands.Count}");
        Console.WriteLine($"Files changed: {session.FileChanges.Count}");
        Console.WriteLine($"Warnings: {session.Warnings.Count}");
        Console.WriteLine("Reports:");
        Console.WriteLine($"  HTML: {Path.Combine(reportsDir, "session.html")}");
        Console.WriteLine($"  Markdown: {Path.Combine(reportsDir, "session.md")}");

        return CommandExitCodes.Success;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1) return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1) return $"{duration.TotalSeconds:F1}s";
        return $"{duration.TotalMinutes:F1}m";
    }
}
