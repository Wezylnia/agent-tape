namespace AgentTape.Core.Models;

public sealed record SessionPaths
{
    public required string RootDirectory { get; init; }

    public required string SessionJsonPath { get; init; }

    public required string CommandsJsonlPath { get; init; }

    public required string StdoutDirectory { get; init; }

    public required string StderrDirectory { get; init; }

    public required string GitDirectory { get; init; }

    public required string TestsDirectory { get; init; }

    public required string ReportsDirectory { get; init; }
}
