using System.Text.Json;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the export command: exports session data in the requested format.
/// </summary>
public static class ExportCommand
{
    public static async Task<int> ExecuteAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new AgentTapeOptions();
        var reportsDir = Path.Combine(options.AgentTapeDirectory, "reports");
        var sourcePath = parseResult.Format == "json"
            ? Path.Combine(options.AgentTapeDirectory, "sessions")
            : Path.Combine(reportsDir, "session.md");

        if (parseResult.Format == "markdown")
        {
            if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine("No Markdown report found. Run: agenttape record -- <command>");
                return CommandExitCodes.Success;
            }

            var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            Console.WriteLine(content);
            return CommandExitCodes.Success;
        }

        // JSON export: find the most recent session
        var sessionsDir = sourcePath;
        if (!Directory.Exists(sessionsDir))
        {
            Console.Error.WriteLine("No sessions found. Run: agenttape record -- <command>");
            return CommandExitCodes.Success;
        }

        var sessionDirs = Directory.GetDirectories(sessionsDir);
        if (sessionDirs.Length == 0)
        {
            Console.Error.WriteLine("No sessions found. Run: agenttape record -- <command>");
            return CommandExitCodes.Success;
        }

        // Find the most recent session by directory name (which starts with timestamp)
        var latest = sessionDirs.OrderDescending().First();
        var sessionJsonPath = Path.Combine(latest, "session.json");

        if (!File.Exists(sessionJsonPath))
        {
            Console.Error.WriteLine("Session data not found.");
            return CommandExitCodes.InternalFailure;
        }

        var json = await File.ReadAllTextAsync(sessionJsonPath, cancellationToken);
        // Pretty-print the JSON
        using var doc = JsonDocument.Parse(json);
        var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(formatted);
        return CommandExitCodes.Success;
    }
}
