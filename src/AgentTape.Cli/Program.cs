using AgentTape.Core;
using AgentTape.Core.Models;
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

        return args[0] switch
        {
            "init" => Init(),
            "record" => await RecordAsync(args.Skip(1).ToArray(), cancellationToken),
            "report" => await ReportAsync(cancellationToken),
            _ => UnknownCommand(args[0])
        };
    }

    private static int Init()
    {
        const string configPath = ".agenttape.yml";
        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, """
project:
  name: agenttape-project
capture:
  stdout: true
  stderr: true
  gitDiff: true
redaction:
  mode: standard
  maskUserPaths: true
riskRules:
  warnOnConfigChanges: true
  warnOnSecretFiles: true
tests:
  detectDotnet: true
  trx: false
""");
        }

        Console.WriteLine($"Created {configPath}");
        return 0;
    }

    private static async Task<int> RecordAsync(string[] args, CancellationToken cancellationToken)
    {
        var separatorIndex = Array.IndexOf(args, "--");
        var commandParts = separatorIndex >= 0 ? args[(separatorIndex + 1)..] : args;
        if (commandParts.Length == 0)
        {
            Console.Error.WriteLine("Usage: agenttape record -- <command> [args]");
            return 2;
        }

        var workingDirectory = Directory.GetCurrentDirectory();
        var clock = new SystemClock();
        var git = new GitCliSnapshotProvider();
        var runner = new ProcessCommandRunner(clock);
        var redactor = new RegexRedactor();
        var testDetector = new DotNetTestOutputDetector();
        var riskRules = new DefaultRiskRules();

        var startedAt = clock.UtcNow;
        var beforeGit = await git.CaptureAsync(workingDirectory, cancellationToken);
        var result = await runner.RunAsync(new CommandRequest
        {
            Executable = commandParts[0],
            Arguments = commandParts.Skip(1).ToArray(),
            WorkingDirectory = workingDirectory
        }, cancellationToken);
        var afterGit = await git.CaptureAsync(workingDirectory, cancellationToken);

        var command = result.Run with
        {
            RedactedStdoutPreview = redactor.Redact(result.Stdout, RedactionMode.Standard),
            RedactedStderrPreview = redactor.Redact(result.Stderr, RedactionMode.Standard)
        };

        var session = new TapeSession
        {
            Id = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"),
            Name = commandParts[0],
            StartedAt = startedAt,
            FinishedAt = clock.UtcNow,
            WorkingDirectory = workingDirectory,
            RedactionMode = RedactionMode.Standard,
            BeforeGit = beforeGit,
            AfterGit = afterGit,
            Commands = [command],
            FileChanges = afterGit.Changes,
            TestSummaries = [testDetector.Detect(command.Command, result.Stdout, result.Stderr)]
        };

        session = session with { Warnings = riskRules.Evaluate(session) };

        Directory.CreateDirectory(".agenttape/reports");
        Directory.CreateDirectory($".agenttape/sessions/{session.Id}");
        await File.WriteAllTextAsync($".agenttape/sessions/{session.Id}/stdout.txt", command.RedactedStdoutPreview, cancellationToken);
        await File.WriteAllTextAsync($".agenttape/sessions/{session.Id}/stderr.txt", command.RedactedStderrPreview, cancellationToken);
        await File.WriteAllTextAsync($".agenttape/reports/session.md", await new MarkdownReportGenerator().GenerateAsync(session, cancellationToken), cancellationToken);
        await File.WriteAllTextAsync($".agenttape/reports/session.html", await new HtmlReportGenerator().GenerateAsync(session, cancellationToken), cancellationToken);

        Console.WriteLine($"Captured 1 command, {session.FileChanges.Count} changed files, {session.Warnings.Count} warnings.");
        Console.WriteLine("Reports generated: .agenttape/reports/session.md and .agenttape/reports/session.html");
        return command.ExitCode;
    }

    private static async Task<int> ReportAsync(CancellationToken cancellationToken)
    {
        var path = ".agenttape/reports/session.html";
        Console.WriteLine(File.Exists(path)
            ? $"Latest HTML report: {Path.GetFullPath(path)}"
            : "No report found. Run: agenttape record -- <command>");

        await Task.CompletedTask;
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
AgentTape

Usage:
  agenttape init
  agenttape record -- <command> [args]
  agenttape report

This skeleton intentionally supports only the first v0.1 flow.
Read docs/implementation-plan.md before adding commands.
""");
    }
}
