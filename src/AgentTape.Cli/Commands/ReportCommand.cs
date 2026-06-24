using AgentTape.Core;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Core.Storage;
using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the report command: displays path to the latest or specified session report.
/// </summary>
public static class ReportCommand
{
    public static async Task<int> ExecuteAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new AgentTapeOptions();
        var globalReportsDir = Path.Combine(options.AgentTapeDirectory, "reports");

        string? htmlPath;
        string? markdownPath;

        if (!string.IsNullOrEmpty(parseResult.SessionId))
        {
            var sessionReportsDir = Path.Combine(options.AgentTapeDirectory, "sessions", parseResult.SessionId, "reports");
            if (!Directory.Exists(sessionReportsDir))
            {
                Console.Error.WriteLine($"Session not found: {parseResult.SessionId}");
                return CommandExitCodes.UsageError;
            }
            htmlPath = Path.Combine(sessionReportsDir, "session.html");
            markdownPath = Path.Combine(sessionReportsDir, "session.md");
        }
        else
        {
            htmlPath = Path.Combine(globalReportsDir, "latest.html");
            markdownPath = Path.Combine(globalReportsDir, "latest.md");
        }

        // Handle --open
        if (parseResult.Open)
        {
            if (parseResult.Markdown && !parseResult.Html)
            {
                Console.Error.WriteLine("Opening HTML report. Markdown reports cannot be opened as interactive reports.");
            }

            if (!File.Exists(htmlPath))
            {
                Console.Error.WriteLine("No HTML report found. Run: agenttape record -- <command>");
                return CommandExitCodes.UsageError;
            }

            var opener = new SystemReportOpener();
            await opener.OpenAsync(htmlPath, cancellationToken);
            Console.WriteLine($"Opened: {Path.GetFullPath(htmlPath)}");
            return CommandExitCodes.Success;
        }

        PrintReportPaths(htmlPath, markdownPath, parseResult.Html, parseResult.Markdown);
        return CommandExitCodes.Success;
    }

    private static void PrintReportPaths(string? htmlPath, string? markdownPath, bool preferHtml, bool preferMarkdown)
    {
        var found = false;

        if (preferHtml || (!preferHtml && !preferMarkdown))
        {
            if (htmlPath is not null && File.Exists(htmlPath))
            {
                Console.WriteLine($"HTML report: {Path.GetFullPath(htmlPath)}");
                found = true;
            }
        }

        if (preferMarkdown || (!preferHtml && !preferMarkdown))
        {
            if (markdownPath is not null && File.Exists(markdownPath))
            {
                Console.WriteLine($"Markdown report: {Path.GetFullPath(markdownPath)}");
                found = true;
            }
        }

        if (!found)
        {
            Console.Error.WriteLine("No reports found. Run: agenttape record -- <command>");
        }
    }
}
