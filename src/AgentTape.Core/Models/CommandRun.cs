namespace AgentTape.Core.Models;

public sealed record CommandRun
{
    public required string Id { get; init; }

    public required string Command { get; init; }

    public CommandKind Kind { get; init; } = CommandKind.Unknown;

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset FinishedAt { get; init; }

    public TimeSpan Duration => FinishedAt - StartedAt;

    public required int ExitCode { get; init; }

    public string? StdoutPath { get; init; }

    public string? StderrPath { get; init; }

    public string? RedactedStdoutPreview { get; init; }

    public string? RedactedStderrPreview { get; init; }
}
