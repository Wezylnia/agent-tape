namespace AgentTape.Redaction.Rules;

/// <summary>
/// Summary of matches for a single redaction rule. No secret values are included.
/// </summary>
public sealed record RedactionMatchSummary
{
    /// <summary>The rule name that produced matches.</summary>
    public required string RuleName { get; init; }

    /// <summary>Number of matches found.</summary>
    public int Count { get; init; }
}
