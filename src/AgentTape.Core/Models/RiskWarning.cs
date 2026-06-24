namespace AgentTape.Core.Models;

public sealed record RiskWarning
{
    public required string Code { get; init; }

    public RiskSeverity Severity { get; init; } = RiskSeverity.Warning;

    public required string Message { get; init; }

    public string? FilePath { get; init; }

    public string? CommandId { get; init; }
}
