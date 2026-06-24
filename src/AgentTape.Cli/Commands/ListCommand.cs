using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the list command: displays available recorded sessions.
/// </summary>
public static class ListCommand
{
    public static async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var options = new AgentTapeOptions();
        var reader = new Core.Storage.FileSystemSessionReader(options);

        var sessions = await reader.ListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            Console.WriteLine("No sessions found. Run: agenttape record -- <command>");
            return CommandExitCodes.Success;
        }

        Console.WriteLine("Session ID                         Name       Duration  Commands  Files  Warnings");
        Console.WriteLine(new string('-', 90));

        foreach (var session in sessions)
        {
            var duration = FormatDuration(session.Duration);
            Console.WriteLine(
                $"{session.Id,-35} {Truncate(session.Name, 10),-10} {duration,-9} {session.Commands.Count,8} {session.FileChanges.Count,6} {session.Warnings.Count,8}");
        }

        return CommandExitCodes.Success;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1) return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1) return $"{duration.TotalSeconds:F1}s";
        return $"{duration.TotalMinutes:F1}m";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }
}
