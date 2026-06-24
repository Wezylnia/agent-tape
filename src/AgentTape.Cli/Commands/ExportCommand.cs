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

        // Determine which session to export
        string? sessionId = parseResult.SessionId;
        if (sessionId is null)
        {
            sessionId = FindLatestSessionId(options);
            if (sessionId is null)
            {
                Console.Error.WriteLine("No sessions found. Run: agenttape record -- <command>");
                return CommandExitCodes.Success;
            }
        }

        var sessionDir = Path.Combine(options.AgentTapeDirectory, "sessions", sessionId);
        if (!Directory.Exists(sessionDir))
        {
            Console.Error.WriteLine($"Session not found: {sessionId}");
            return CommandExitCodes.UsageError;
        }

        string content;
        var format = parseResult.Format ?? "markdown";

        switch (format)
        {
            case "markdown":
            {
                var reportPath = Path.Combine(sessionDir, "reports", "session.md");
                if (!File.Exists(reportPath))
                {
                    Console.Error.WriteLine("No Markdown report found for this session.");
                    return CommandExitCodes.Success;
                }
                content = await File.ReadAllTextAsync(reportPath, cancellationToken);
                break;
            }

            case "html":
            {
                var reportPath = Path.Combine(sessionDir, "reports", "session.html");
                if (!File.Exists(reportPath))
                {
                    Console.Error.WriteLine("No HTML report found for this session.");
                    return CommandExitCodes.Success;
                }
                content = await File.ReadAllTextAsync(reportPath, cancellationToken);
                break;
            }

            case "json":
            {
                var sessionJsonPath = Path.Combine(sessionDir, "session.json");
                if (!File.Exists(sessionJsonPath))
                {
                    Console.Error.WriteLine("Session data not found.");
                    return CommandExitCodes.InternalFailure;
                }
                var json = await File.ReadAllTextAsync(sessionJsonPath, cancellationToken);
                using var doc = JsonDocument.Parse(json);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                break;
            }

            case "github-pr":
            {
                var sessionJsonPath = Path.Combine(sessionDir, "session.json");
                if (!File.Exists(sessionJsonPath))
                {
                    Console.Error.WriteLine("Session data not found.");
                    return CommandExitCodes.InternalFailure;
                }
                var json = await File.ReadAllTextAsync(sessionJsonPath, cancellationToken);
                var session = JsonSerializer.Deserialize<TapeSession>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                content = session is not null ? GenerateGitHubPrSummary(session) : "Session data unavailable.";
                break;
            }

            default:
                Console.Error.WriteLine($"Unknown export format: {format}");
                return CommandExitCodes.UsageError;
        }

        // Write output
        if (parseResult.Output is not null)
        {
            await File.WriteAllTextAsync(parseResult.Output, content, cancellationToken);
            Console.WriteLine($"Export written to {Path.GetFullPath(parseResult.Output)}");
        }
        else
        {
            Console.WriteLine(content);
        }

        return CommandExitCodes.Success;
    }

    private static string GenerateGitHubPrSummary(TapeSession session)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## AgentTape Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Session:** `{session.Name}`");
        sb.AppendLine($"- **Duration:** {FormatDuration(session.Duration)}");
        sb.AppendLine($"- **Commands:** {session.Commands.Count}");
        sb.AppendLine($"- **Files changed:** {session.FileChanges.Count}");
        sb.AppendLine($"- **Risk warnings:** {session.Warnings.Count}");
        sb.AppendLine($"- **Redaction:** {session.RedactionMode.ToString().ToLowerInvariant()}");
        sb.AppendLine();

        // Test results
        sb.AppendLine("### Test Results");
        sb.AppendLine();
        if (session.TestSummaries.Count > 0 && session.TestSummaries[0].HasAnySignal)
        {
            var ts = session.TestSummaries[0];
            sb.AppendLine($"- **Total:** {ts.Total?.ToString() ?? "unknown"}");
            sb.AppendLine($"- **Passed:** {ts.Passed?.ToString() ?? "unknown"}");
            sb.AppendLine($"- **Failed:** {ts.Failed?.ToString() ?? "unknown"}");
            sb.AppendLine($"- **Skipped:** {ts.Skipped?.ToString() ?? "unknown"}");
        }
        else
        {
            sb.AppendLine("_No test signals detected._");
        }
        sb.AppendLine();

        // Changed files
        sb.AppendLine("### Changed Files");
        sb.AppendLine();
        if (session.FileChanges.Count > 0)
        {
            sb.AppendLine("| Kind | Path |");
            sb.AppendLine("|---|---|");
            foreach (var change in session.FileChanges)
            {
                sb.AppendLine($"| {change.Kind} | `{change.Path}` |");
            }
        }
        else
        {
            sb.AppendLine("_No changed files._");
        }
        sb.AppendLine();

        // Risk warnings
        sb.AppendLine("### Risk Warnings");
        sb.AppendLine();
        if (session.Warnings.Count > 0)
        {
            foreach (var warning in session.Warnings)
            {
                sb.AppendLine($"- `{warning.Code}`: {warning.Message}");
            }
        }
        else
        {
            sb.AppendLine("_No warnings._");
        }
        sb.AppendLine();

        // Reproduction
        sb.AppendLine("### Reproduction");
        sb.AppendLine();
        sb.AppendLine("```bash");
        foreach (var cmd in session.Commands)
        {
            sb.AppendLine(cmd.Command);
        }
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("_Generated by AgentTape._");

        return sb.ToString();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1) return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1) return $"{duration.TotalSeconds:F1}s";
        return $"{duration.TotalMinutes:F1}m";
    }

    private static string? FindLatestSessionId(AgentTapeOptions options)
    {
        var sessionsDir = Path.Combine(options.AgentTapeDirectory, "sessions");
        if (!Directory.Exists(sessionsDir))
            return null;

        var dirs = Directory.GetDirectories(sessionsDir);
        if (dirs.Length == 0)
            return null;

        return dirs.Select(Path.GetFileName).OrderDescending().First();
    }
}
