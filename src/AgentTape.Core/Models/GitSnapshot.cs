namespace AgentTape.Core.Models;

public sealed record GitSnapshot
{
    public bool IsRepository { get; init; }

    public string? Branch { get; init; }

    public string? HeadSha { get; init; }

    public string? StatusText { get; init; }

    public IReadOnlyList<FileChange> Changes { get; init; } = Array.Empty<FileChange>();
}
