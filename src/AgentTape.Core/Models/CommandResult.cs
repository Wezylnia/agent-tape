namespace AgentTape.Core.Models;

public sealed record CommandResult
{
    public required CommandRun Run { get; init; }

    public required string Stdout { get; init; }

    public required string Stderr { get; init; }
}
