using AgentTape.Core;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Core.Storage;
using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the report command: displays path to the latest generated report.
/// </summary>
public static class ReportCommand
{
    public static Task<int> ExecuteAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new AgentTapeOptions();
        var reportsDir = Path.Combine(options.AgentTapeDirectory, "reports");

        if (!Directory.Exists(reportsDir))
        {
            Console.Error.WriteLine("No reports found. Run: agenttape record -- <command>");
            return Task.FromResult(CommandExitCodes.Success);
        }

        var htmlPath = Path.Combine(reportsDir, "session.html");
        var markdownPath = Path.Combine(reportsDir, "session.md");

        var found = false;

        if (parseResult.Html || (!parseResult.Html && !parseResult.Markdown))
        {
            if (File.Exists(htmlPath))
            {
                Console.WriteLine($"Latest HTML report: {Path.GetFullPath(htmlPath)}");
                found = true;
            }
        }

        if (parseResult.Markdown || (!parseResult.Html && !parseResult.Markdown))
        {
            if (File.Exists(markdownPath))
            {
                Console.WriteLine($"Latest Markdown report: {Path.GetFullPath(markdownPath)}");
                found = true;
            }
        }

        if (!found)
        {
            Console.Error.WriteLine("No reports found. Run: agenttape record -- <command>");
        }

        return Task.FromResult(CommandExitCodes.Success);
    }
}
