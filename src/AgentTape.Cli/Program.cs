using AgentTape.Cli.Commands;
using AgentTape.Cli.Parsing;
using AgentTape.Core;
using AgentTape.Core.Configuration;
using AgentTape.Core.Storage;
using AgentTape.Git.Snapshots;
using AgentTape.Redaction.Rules;
using AgentTape.Reporting.Html;
using AgentTape.Reporting.Markdown;
using AgentTape.Rules.Risk;
using AgentTape.Testing.DotNet;

var exitCode = await AgentTapeProgram.RunAsync(args, CancellationToken.None);
return exitCode;

internal static class AgentTapeProgram
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return 0;
        }

        var parseResult = CliParser.Parse(args);
        if (!parseResult.IsSuccess)
        {
            Console.Error.WriteLine(parseResult.ErrorMessage);
            return CommandExitCodes.UsageError;
        }

        return parseResult.Command switch
        {
            "init" => InitCommand.Execute(),
            "record" => await ExecuteRecordAsync(parseResult, cancellationToken),
            "report" => await ReportCommand.ExecuteAsync(parseResult, cancellationToken),
            "export" => await ExportCommand.ExecuteAsync(parseResult, cancellationToken),
            _ => CommandExitCodes.UsageError
        };
    }

    private static async Task<int> ExecuteRecordAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var clock = new SystemClock();
        var options = new AgentTapeOptions();

        var recordCommand = new RecordCommand(
            clock,
            new ProcessCommandRunner(clock),
            new GitCliSnapshotProvider(),
            new RegexRedactor(),
            new FileSystemSessionStore(options),
            new MarkdownReportGenerator(),
            new HtmlReportGenerator(),
            new DotNetTestOutputDetector(),
            new DefaultRiskRules(),
            options);

        return await recordCommand.ExecuteAsync(parseResult, cancellationToken);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
AgentTape - Flight recorder for AI coding agent sessions.

Usage:
  agenttape init
  agenttape record [--name <name>] [--redact standard|strict|off] [--no-git] -- <command> [args]
  agenttape report [--html] [--markdown] [--open]
  agenttape export --format markdown|json

Exit codes:
  0  Success
  2  CLI usage error
  3  Internal recording failure

The wrapped command exit code is returned by record.
""");
    }
}
