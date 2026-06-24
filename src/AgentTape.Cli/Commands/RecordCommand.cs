using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;
using AgentTape.Core.Storage;
using AgentTape.Cli.Parsing;

namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the record command: captures a command session with git snapshots, redaction, and reporting.
/// </summary>
public sealed class RecordCommand
{
    private readonly IClock _clock;
    private readonly ICommandRunner _commandRunner;
    private readonly IGitSnapshotProvider _gitSnapshotProvider;
    private readonly IRedactor _redactor;
    private readonly ISessionStore _sessionStore;
    private readonly IReportGenerator _markdownGenerator;
    private readonly IReportGenerator _htmlGenerator;
    private readonly ITestResultDetector _testDetector;
    private readonly IRiskRule _riskRule;
    private readonly AgentTapeOptions _options;

    public RecordCommand(
        IClock clock,
        ICommandRunner commandRunner,
        IGitSnapshotProvider gitSnapshotProvider,
        IRedactor redactor,
        ISessionStore sessionStore,
        IReportGenerator markdownGenerator,
        IReportGenerator htmlGenerator,
        ITestResultDetector testDetector,
        IRiskRule riskRule,
        AgentTapeOptions options)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
        _gitSnapshotProvider = gitSnapshotProvider ?? throw new ArgumentNullException(nameof(gitSnapshotProvider));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _markdownGenerator = markdownGenerator ?? throw new ArgumentNullException(nameof(markdownGenerator));
        _htmlGenerator = htmlGenerator ?? throw new ArgumentNullException(nameof(htmlGenerator));
        _testDetector = testDetector ?? throw new ArgumentNullException(nameof(testDetector));
        _riskRule = riskRule ?? throw new ArgumentNullException(nameof(riskRule));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<int> ExecuteAsync(CliParseResult parseResult, CancellationToken cancellationToken)
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var redactionMode = ParseRedactionMode(parseResult.Redact);
        var sessionName = parseResult.Name ?? parseResult.WrappedExecutable ?? "record";

        var startedAt = _clock.UtcNow;
        var sessionId = SessionIdFactory.Create(sessionName, startedAt);

        GitSnapshot beforeGit;
        if (parseResult.NoGit)
        {
            beforeGit = new GitSnapshot { IsRepository = false };
        }
        else
        {
            try
            {
                beforeGit = await _gitSnapshotProvider.CaptureAsync(workingDirectory, cancellationToken);
            }
            catch
            {
                beforeGit = new GitSnapshot { IsRepository = false };
            }
        }

        CommandResult result;
        try
        {
            result = await _commandRunner.RunAsync(new CommandRequest
            {
                Executable = parseResult.WrappedExecutable!,
                Arguments = parseResult.WrappedArguments,
                WorkingDirectory = workingDirectory
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Recording failed: {ex.Message}");
            return CommandExitCodes.InternalFailure;
        }

        GitSnapshot afterGit;
        string diff;
        if (parseResult.NoGit)
        {
            afterGit = new GitSnapshot { IsRepository = false };
            diff = string.Empty;
        }
        else
        {
            try
            {
                afterGit = await _gitSnapshotProvider.CaptureAsync(workingDirectory, cancellationToken);
                diff = await _gitSnapshotProvider.CaptureDiffAsync(workingDirectory, cancellationToken);
            }
            catch
            {
                afterGit = beforeGit;
                diff = string.Empty;
            }
        }

        var command = result.Run with
        {
            RedactedStdoutPreview = _redactor.Redact(result.Stdout, redactionMode),
            RedactedStderrPreview = _redactor.Redact(result.Stderr, redactionMode)
        };

        var session = new TapeSession
        {
            Id = sessionId,
            Name = sessionName,
            StartedAt = startedAt,
            FinishedAt = _clock.UtcNow,
            WorkingDirectory = workingDirectory,
            RedactionMode = redactionMode,
            BeforeGit = beforeGit,
            AfterGit = afterGit with { StatusText = diff },
            Commands = [command],
            FileChanges = afterGit.Changes,
            TestSummaries = [_testDetector.Detect(command.Command, result.Stdout, result.Stderr)]
        };

        session = session with { Warnings = _riskRule.Evaluate(session) };

        // Store session and generate reports
        var paths = await _sessionStore.CreateSessionLayoutAsync(session, cancellationToken);
        await _sessionStore.SaveSessionAsync(session, paths, cancellationToken);

        var markdownPath = Path.Combine(paths.ReportsDirectory, "session.md");
        var htmlPath = Path.Combine(paths.ReportsDirectory, "session.html");

        await File.WriteAllTextAsync(markdownPath,
            await _markdownGenerator.GenerateAsync(session, cancellationToken), cancellationToken);
        await File.WriteAllTextAsync(htmlPath,
            await _htmlGenerator.GenerateAsync(session, cancellationToken), cancellationToken);

        Console.WriteLine($"Captured 1 command, {session.FileChanges.Count} changed files, {session.Warnings.Count} warnings.");
        Console.WriteLine($"Reports: {htmlPath} and {markdownPath}");

        return command.ExitCode;
    }

    private static RedactionMode ParseRedactionMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "off" => RedactionMode.Off,
            "strict" => RedactionMode.Strict,
            _ => RedactionMode.Standard
        };
    }
}
