using AgentTape.Cli.Commands;
using AgentTape.Cli.Parsing;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;

namespace AgentTape.Cli.Tests.Commands;

public sealed class RecordCommandTests
{
    [Fact]
    public async Task ExecuteAsync_returns_wrapped_command_exit_code()
    {
        var cmd = CreateRecordCommand(commandExitCode: 42);
        var result = await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_captures_git_before_and_after_when_enabled()
    {
        var gitProvider = new FakeGitSnapshotProvider();
        var cmd = CreateRecordCommand(gitProvider: gitProvider);
        await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.True(gitProvider.CaptureCallCount >= 2);
    }

    [Fact]
    public async Task ExecuteAsync_skips_git_when_no_git_option_is_set()
    {
        var gitProvider = new FakeGitSnapshotProvider();
        var cmd = CreateRecordCommand(gitProvider: gitProvider);
        var parseResult = CreateParseResult("dotnet") with { NoGit = true };
        await cmd.ExecuteAsync(parseResult, CancellationToken.None);
        Assert.Equal(0, gitProvider.CaptureCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_applies_redaction_before_storage()
    {
        var redactor = new FakeRedactor();
        var sessionStore = new FakeSessionStore();
        var cmd = CreateRecordCommand(redactor: redactor, sessionStore: sessionStore);
        await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.True(redactor.RedactCallCount > 0);
        Assert.True(sessionStore.SaveCalled);
    }

    [Fact]
    public async Task ExecuteAsync_runs_test_detection()
    {
        var testDetector = new FakeTestResultDetector();
        var cmd = CreateRecordCommand(testDetector: testDetector);
        await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.True(testDetector.DetectCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ignores_trx_files_from_before_session_start()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"agenttape-stale-trx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        try
        {
            var testResults = Path.Combine(workingDirectory, "TestResults");
            Directory.CreateDirectory(testResults);
            var staleTrx = Path.Combine(testResults, "stale.trx");
            await File.WriteAllTextAsync(staleTrx, """
                <?xml version="1.0" encoding="utf-8"?>
                <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                  <ResultSummary outcome="Completed">
                    <Counters total="3" executed="3" passed="3" failed="0" />
                  </ResultSummary>
                </TestRun>
                """);
            File.SetLastWriteTimeUtc(staleTrx, DateTime.UtcNow.AddHours(-1));

            var sessionStore = new FakeSessionStore();
            var cmd = CreateRecordCommand(sessionStore: sessionStore, workingDirectory: workingDirectory);

            await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);

            Assert.NotNull(sessionStore.SavedSession);
            Assert.Empty(sessionStore.SavedSession.TestSummaries);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_runs_risk_rules()
    {
        var riskRule = new FakeRiskRule();
        var cmd = CreateRecordCommand(riskRule: riskRule);
        await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.True(riskRule.EvaluateCalled);
    }

    [Fact]
    public async Task ExecuteAsync_writes_reports()
    {
        var sessionStore = new FakeSessionStore();
        var cmd = CreateRecordCommand(sessionStore: sessionStore);
        await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.True(sessionStore.SaveCalled);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_call_report_generator_when_command_runner_throws()
    {
        var commandRunner = new ThrowingCommandRunner();
        var cmd = CreateRecordCommand(commandRunner: commandRunner);
        var result = await cmd.ExecuteAsync(CreateParseResult("dotnet"), CancellationToken.None);
        Assert.Equal(CommandExitCodes.InternalFailure, result);
    }

    private static CliParseResult CreateParseResult(string executable)
    {
        return new CliParseResult
        {
            IsSuccess = true,
            Command = "record",
            WrappedExecutable = executable,
            WrappedArguments = Array.Empty<string>(),
            Redact = "standard"
        };
    }

    private static RecordCommand CreateRecordCommand(
        ICommandRunner? commandRunner = null,
        IGitSnapshotProvider? gitProvider = null,
        IRedactor? redactor = null,
        ISessionStore? sessionStore = null,
        ITestResultDetector? testDetector = null,
        IRiskRule? riskRule = null,
        int commandExitCode = 0,
        string? workingDirectory = null)
    {
        var clock = new FakeClock();
        return new RecordCommand(
            clock,
            commandRunner ?? new FakeCommandRunner(commandExitCode),
            gitProvider ?? new FakeGitSnapshotProvider(),
            redactor ?? new FakeRedactor(),
            sessionStore ?? new FakeSessionStore(),
            new FakeReportGenerator("markdown"),
            new FakeReportGenerator("html"),
            testDetector ?? new FakeTestResultDetector(),
            riskRule ?? new FakeRiskRule(),
            new AgentTapeOptions { AgentTapeDirectory = Path.Combine(Path.GetTempPath(), $"agenttape-test-{Guid.NewGuid():N}") },
            workingDirectory is null ? null : () => workingDirectory);
    }

    // --- Fake implementations ---

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class FakeCommandRunner(int exitCode) : ICommandRunner
    {
        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CommandResult
            {
                Stdout = "fake stdout",
                Stderr = "fake stderr",
                Run = new CommandRun
                {
                    Id = "001",
                    Command = request.DisplayCommand,
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitCode = exitCode
                }
            });
        }
    }

    private sealed class ThrowingCommandRunner : ICommandRunner
    {
        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Command failed to start.");
        }
    }

    private sealed class FakeGitSnapshotProvider : IGitSnapshotProvider
    {
        public int CaptureCallCount { get; private set; }

        public Task<GitSnapshot> CaptureAsync(string workingDirectory, CancellationToken cancellationToken)
        {
            CaptureCallCount++;
            return Task.FromResult(new GitSnapshot
            {
                IsRepository = true,
                Branch = "main",
                HeadSha = "abc1234",
                Changes = Array.Empty<FileChange>()
            });
        }

        public Task<string> CaptureDiffAsync(string workingDirectory, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<IReadOnlyList<(string Path, int? AddedLines, int? DeletedLines, bool IsBinary)>> CaptureNumStatAsync(
            string workingDirectory, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<(string, int?, int?, bool)>>(Array.Empty<(string, int?, int?, bool)>());
        }
    }

    private sealed class FakeRedactor : IRedactor
    {
        public int RedactCallCount { get; private set; }

        public string Redact(string input, RedactionMode mode)
        {
            RedactCallCount++;
            return $"[REDACTED] {input}";
        }

        public RedactionResult RedactWithSummary(string input, RedactionMode mode)
        {
            RedactCallCount++;
            return new RedactionResult
            {
                Text = $"[REDACTED] {input}",
                MatchCount = 1,
                Summaries = [new RedactionMatchSummary { RuleName = "Test Rule", Count = 1 }]
            };
        }
    }

    private sealed class FakeSessionStore : ISessionStore
    {
        public bool SaveCalled { get; private set; }
        public TapeSession? SavedSession { get; private set; }

        public Task<SessionPaths> CreateSessionLayoutAsync(TapeSession session, CancellationToken cancellationToken)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "agenttape-fake", session.Id);
            var globalReports = Path.Combine(Path.GetTempPath(), "agenttape-fake", "reports");
            var paths = new SessionPaths
            {
                RootDirectory = tmp,
                SessionJsonPath = Path.Combine(tmp, "session.json"),
                CommandsJsonlPath = Path.Combine(tmp, "commands.jsonl"),
                StdoutDirectory = Path.Combine(tmp, "stdout"),
                StderrDirectory = Path.Combine(tmp, "stderr"),
                GitDirectory = Path.Combine(tmp, "git"),
                TestsDirectory = Path.Combine(tmp, "tests"),
                SessionReportsDirectory = Path.Combine(tmp, "reports"),
                GlobalReportsDirectory = globalReports
            };
            Directory.CreateDirectory(tmp);
            Directory.CreateDirectory(paths.SessionReportsDirectory);
            Directory.CreateDirectory(paths.GlobalReportsDirectory);
            return Task.FromResult(paths);
        }

        public Task SaveSessionAsync(TapeSession session, SessionPaths paths, CancellationToken cancellationToken)
        {
            SaveCalled = true;
            SavedSession = session;
            return Task.CompletedTask;
        }

        public Task SaveRedactionLogAsync(SessionPaths paths, IReadOnlyList<RedactionMatchSummary> summaries, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReportGenerator(string format) : IReportGenerator
    {
        public string Format => format;

        public Task<string> GenerateAsync(TapeSession session, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Fake {format} report for {session.Name}");
        }
    }

    private sealed class FakeTestResultDetector : ITestResultDetector
    {
        public bool DetectCalled { get; private set; }

        public TestSummary Detect(string command, string stdout, string stderr)
        {
            DetectCalled = true;
            return new TestSummary();
        }
    }

    private sealed class FakeRiskRule : IRiskRule
    {
        public bool EvaluateCalled { get; private set; }
        public string Code => "FAKE_RISK_RULE";

        public IReadOnlyList<RiskWarning> Evaluate(TapeSession session)
        {
            EvaluateCalled = true;
            return Array.Empty<RiskWarning>();
        }
    }
}
