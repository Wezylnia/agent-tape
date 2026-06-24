namespace AgentTape.Core.Models;

public sealed record CommandRequest
{
    public required string Executable { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public required string WorkingDirectory { get; init; }

    public IReadOnlyDictionary<string, string?> Environment { get; init; } =
        new Dictionary<string, string?>();

    public string DisplayCommand =>
        Arguments.Count == 0 ? Executable : $"{Executable} {string.Join(' ', Arguments)}";
}
