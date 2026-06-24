namespace AgentTape.Redaction.Rules;

/// <summary>
/// Result of applying redaction to a string.
/// </summary>
public sealed record RedactionResult
{
    /// <summary>The redacted text.</summary>
    public required string Text { get; init; }

    /// <summary>Number of matches found and replaced.</summary>
    public int MatchCount { get; init; }

    /// <summary>Per-rule match summaries without secret values.</summary>
    public IReadOnlyList<RedactionMatchSummary> Summaries { get; init; } = Array.Empty<RedactionMatchSummary>();
}
