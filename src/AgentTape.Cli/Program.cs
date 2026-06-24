using AgentTape.Cli.Commands;
using AgentTape.Cli.Configuration;
using AgentTape.Cli.Parsing;
using AgentTape.Core;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
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
        if (args.Length == 0 || (args.Length == 1 && args[0] is "-h" or "--help"))
        {
            PrintHelp();
            return 0;
        }

        // Check for --help anywhere in the args (before -- separator for record)
        if (args.Any(a => a is "-h" or "--help"))
        {
            PrintHelp();
            return 0;
        }

        // Check for --version
        if (args.Any(a => a is "--version" or "-v"))
        {
            Console.WriteLine("AgentTape 1.0.0");
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
            "list" => await ListCommand.ExecuteAsync(cancellationToken),
            "show" => await ShowCommand.ExecuteAsync(parseResult, cancellationToken),
            _ => CommandExitCodes.UsageError
        };
    }

    private static async Task<int> ExecuteRecordAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var clock = new SystemClock();
        AgentTapeOptions options;
        if (parseResult.ConfigPath is not null)
        {
            if (!File.Exists(parseResult.ConfigPath))
            {
                Console.Error.WriteLine($"Config file not found: {parseResult.ConfigPath}");
                return CommandExitCodes.UsageError;
            }
            options = ConfigLoader.Load(parseResult.ConfigPath);
        }
        else
        {
            options = ConfigLoader.Load();
        }

        // CLI overrides config
        if (parseResult.Redact is not null)
        {
            var mode = parseResult.Redact.ToLowerInvariant() switch
            {
                "standard" => RedactionMode.Standard,
                "strict" => RedactionMode.Strict,
                "off" => RedactionMode.Off,
                _ => options.RedactionMode
            };
            options = options with { RedactionMode = mode };
        }

        if (parseResult.NoGit)
        {
            options = options with { CaptureGit = false };
        }

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
  agenttape record [--name <name>] [--redact standard|strict|off] [--no-git] [--config <path>] -- <command> [args]
  agenttape list
  agenttape show <session-id>
  agenttape report [--html] [--markdown] [--open] [--session <session-id>]
  agenttape export --format markdown|json [--session <session-id>]

Exit codes:
  0  Success
  2  CLI usage error
  3  Internal recording failure

The wrapped command exit code is returned by record.
""");
    }
}
