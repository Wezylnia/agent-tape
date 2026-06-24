namespace AgentTape.Core.Models;

public sealed record FileChange
{
    public required string Path { get; init; }

    public string? OldPath { get; init; }

    public FileChangeKind Kind { get; init; } = FileChangeKind.Unknown;

    public bool IsBinary { get; init; }

    public int? AddedLines { get; init; }

    public int? DeletedLines { get; init; }
}
