namespace AgentTape.Core.Models;

public sealed record TestSummary
{
    public int? Total { get; init; }

    public int? Passed { get; init; }

    public int? Failed { get; init; }

    public int? Skipped { get; init; }

    public IReadOnlyList<string> FailedTestNames { get; init; } = Array.Empty<string>();

    public bool HasAnySignal => Total.HasValue || Passed.HasValue || Failed.HasValue || Skipped.HasValue;
}
