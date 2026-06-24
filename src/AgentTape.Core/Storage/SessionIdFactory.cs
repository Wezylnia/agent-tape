using System.Text.RegularExpressions;

namespace AgentTape.Core.Storage;

/// <summary>
/// Produces deterministic, filesystem-safe session ids from a name and start time.
/// </summary>
public static partial class SessionIdFactory
{
    private const int MaxLength = 80;
    private const string FallbackName = "session";

    /// <summary>
    /// Creates a session id like "2026-06-24-142001-fix-tests".
    /// Uses UTC time and lowercases the name.
    /// </summary>
    public static string Create(string name, DateTimeOffset startedAt)
    {
        var timestamp = startedAt.UtcDateTime.ToString("yyyy-MM-dd-HHmmss");

        var normalized = NormalizeName(name);

        var id = $"{timestamp}-{normalized}";

        if (id.Length > MaxLength)
        {
            id = id[..MaxLength];
        }

        return id;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FallbackName;
        }

        // Lowercase
        var normalized = name.ToLowerInvariant().Trim();

        // Replace spaces with hyphens
        normalized = SpaceOrUnderscoreRegex().Replace(normalized, "-");

        // Remove or replace unsafe path characters
        normalized = UnsafePathCharRegex().Replace(normalized, "-");

        // Collapse repeated hyphens
        normalized = RepeatedHyphenRegex().Replace(normalized, "-");

        // Trim leading/trailing hyphens
        normalized = normalized.Trim('-');

        if (normalized.Length == 0)
        {
            return FallbackName;
        }

        if (normalized.Length > MaxLength - 20) // reserve room for timestamp prefix
        {
            normalized = normalized[..(MaxLength - 20)];
        }

        return normalized;
    }

    [GeneratedRegex(@"[\s_]+")]
    private static partial Regex SpaceOrUnderscoreRegex();

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex UnsafePathCharRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex RepeatedHyphenRegex();
}
