using System.Text.RegularExpressions;

namespace AgentTape.Redaction.Rules;

/// <summary>
/// A single redaction rule with a pattern and replacement.
/// </summary>
public sealed record RedactionRule
{
    /// <summary>Human-readable name for the rule (e.g., "GitHub Classic Token").</summary>
    public required string Name { get; init; }

    /// <summary>The compiled regex pattern.</summary>
    public required Regex Pattern { get; init; }

    /// <summary>Replacement string with optional capture group references.</summary>
    public required string Replacement { get; init; }

    /// <summary>Whether this rule is active in Standard mode.</summary>
    public bool Standard { get; init; } = true;

    /// <summary>Whether this rule is active in Strict mode (in addition to Standard).</summary>
    public bool Strict { get; init; }
}
