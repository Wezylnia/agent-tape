namespace AgentTape.Core.Models;

public sealed record EnvironmentSnapshot
{
    public string? OperatingSystem { get; init; }

    public string? DotNetVersion { get; init; }

    public string? Shell { get; init; }

    public IReadOnlyDictionary<string, string> Tools { get; init; } =
        new Dictionary<string, string>();
}
