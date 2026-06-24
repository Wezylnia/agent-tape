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
        var redactionMode = parseResult.Redact is not null
            ? ParseRedactionMode(parseResult.Redact)
            : _options.RedactionMode;
        var sessionName = parseResult.Name ?? parseResult.WrappedExecutable ?? "record";

        var startedAt = _clock.UtcNow;
        var sessionId = SessionIdFactory.Create(sessionName, startedAt);

        var skipGit = parseResult.NoGit || !_options.CaptureGit;

        GitSnapshot beforeGit;
        if (skipGit)
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
        if (skipGit)
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

        // Redact git diff and working directory
        var redactedDiff = _redactor.Redact(diff, redactionMode);
        var redactedWorkingDirectory = _redactor.Redact(workingDirectory, redactionMode);

        // Compute session delta: separate pre-existing changes from session changes
        var (preExistingChanges, sessionChanges) = ComputeFileChangeDelta(beforeGit.Changes, afterGit.Changes);
        var hasDirtyBefore = beforeGit.IsRepository && beforeGit.Changes.Count > 0;

        var session = new TapeSession
        {
            Id = sessionId,
            Name = sessionName,
            StartedAt = startedAt,
            FinishedAt = _clock.UtcNow,
            WorkingDirectory = redactedWorkingDirectory,
            RedactionMode = redactionMode,
            BeforeGit = beforeGit,
            AfterGit = afterGit with { StatusText = redactedDiff },
            Commands = [command],
            FileChanges = afterGit.Changes,
            PreExistingChanges = preExistingChanges,
            SessionChanges = sessionChanges,
            TestSummaries = [_testDetector.Detect(command.Command, result.Stdout, result.Stderr)]
        };

        session = session with { Warnings = _riskRule.Evaluate(session) };

        // Add dirty-before warning if repository had uncommitted changes before recording
        if (hasDirtyBefore)
        {
            var dirtyWarning = new RiskWarning
            {
                Code = "DIRTY_REPOSITORY_BEFORE_RECORDING",
                Severity = RiskSeverity.Warning,
                Message = "Repository had uncommitted changes before recording. Some final changes may not belong to this session."
            };
            session = session with { Warnings = session.Warnings.Append(dirtyWarning).ToArray() };
        }

        // Build redaction summaries
        var stdoutResult = _redactor.RedactWithSummary(result.Stdout, redactionMode);
        var stderrResult = _redactor.RedactWithSummary(result.Stderr, redactionMode);
        var diffResult = _redactor.RedactWithSummary(diff, redactionMode);
        var wdResult = _redactor.RedactWithSummary(workingDirectory, redactionMode);

        var allSummaries = MergeSummaries(stdoutResult, stderrResult, diffResult, wdResult);

        // Store session and generate reports
        var paths = await _sessionStore.CreateSessionLayoutAsync(session, cancellationToken);
        await _sessionStore.SaveSessionAsync(session, paths, cancellationToken);

        // Write redaction log
        await _sessionStore.SaveRedactionLogAsync(paths, allSummaries, cancellationToken);

        // Write session-specific reports
        var markdownContent = await _markdownGenerator.GenerateAsync(session, cancellationToken);
        var htmlContent = await _htmlGenerator.GenerateAsync(session, cancellationToken);

        await File.WriteAllTextAsync(paths.SessionMarkdownReportPath, markdownContent, cancellationToken);
        await File.WriteAllTextAsync(paths.SessionHtmlReportPath, htmlContent, cancellationToken);

        // Update latest aliases
        await File.WriteAllTextAsync(paths.LatestMarkdownReportPath, markdownContent, cancellationToken);
        await File.WriteAllTextAsync(paths.LatestHtmlReportPath, htmlContent, cancellationToken);

        Console.WriteLine($"Captured 1 command, {session.FileChanges.Count} changed files, {session.Warnings.Count} warnings.");
        Console.WriteLine($"Reports: {paths.SessionHtmlReportPath} and {paths.SessionMarkdownReportPath}");

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

    private static (IReadOnlyList<FileChange> PreExisting, IReadOnlyList<FileChange> Session) ComputeFileChangeDelta(
        IReadOnlyList<FileChange> before, IReadOnlyList<FileChange> after)
    {
        if (before.Count == 0)
        {
            return (Array.Empty<FileChange>(), after);
        }

        var beforePaths = before.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var afterPaths = after.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var preExisting = before.ToArray();

        // Session changes are files that appear only in after, or were in both but changed
        var session = after
            .Where(f => !beforePaths.Contains(f.Path) || beforePaths.Contains(f.Path))
            .ToArray();

        // For files in both, mark as pre-existing modified
        var overlapping = after.Where(f => beforePaths.Contains(f.Path)).ToList();
        var newOnly = after.Where(f => !beforePaths.Contains(f.Path)).ToList();

        // Simple v1.0 approach: new files in after = session changes
        // Files present in both = ambiguous, mark as pre-existing
        var sessionChanges = newOnly;
        var preExistingChanges = preExisting.Concat(overlapping).ToList();

        return (preExistingChanges, sessionChanges);
    }

    private static IReadOnlyList<RedactionMatchSummary> MergeSummaries(params RedactionResult[] results)
    {
        return results
            .SelectMany(r => r.Summaries)
            .GroupBy(s => s.RuleName)
            .Select(g => new RedactionMatchSummary { RuleName = g.Key, Count = g.Sum(s => s.Count) })
            .OrderByDescending(s => s.Count)
            .ToArray();
    }
}
