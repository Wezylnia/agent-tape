namespace AgentTape.Core.Models;

public sealed record TapeSession
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset FinishedAt { get; init; }

    public TimeSpan Duration => FinishedAt - StartedAt;

    public required string WorkingDirectory { get; init; }

    public RedactionMode RedactionMode { get; init; } = RedactionMode.Standard;

    public GitSnapshot? BeforeGit { get; init; }

    public GitSnapshot? AfterGit { get; init; }

    public EnvironmentSnapshot? Environment { get; init; }

    public IReadOnlyList<CommandRun> Commands { get; init; } = Array.Empty<CommandRun>();

    public IReadOnlyList<FileChange> FileChanges { get; init; } = Array.Empty<FileChange>();

    /// <summary>Changes that existed before recording started.</summary>
    public IReadOnlyList<FileChange> PreExistingChanges { get; init; } = Array.Empty<FileChange>();

    /// <summary>Changes caused during the session (after minus before).</summary>
    public IReadOnlyList<FileChange> SessionChanges { get; init; } = Array.Empty<FileChange>();

    public IReadOnlyList<TestSummary> TestSummaries { get; init; } = Array.Empty<TestSummary>();

    public IReadOnlyList<RiskWarning> Warnings { get; init; } = Array.Empty<RiskWarning>();
}
