namespace AgentTape.Cli.Parsing;

/// <summary>
/// Result of parsing CLI arguments for AgentTape commands.
/// </summary>
public sealed record CliParseResult
{
    /// <summary>True when the parse succeeded. False when there is a usage error.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Error message when IsSuccess is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The command name (init, record, report, export).</summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>Session name for the record command.</summary>
    public string? Name { get; init; }

    /// <summary>Redaction mode for the record command. Null when not explicitly set by user.</summary>
    public string? Redact { get; init; }

    /// <summary>When true, git capture is skipped.</summary>
    public bool NoGit { get; init; }

    /// <summary>Wrapped command executable for record.</summary>
    public string? WrappedExecutable { get; init; }

    /// <summary>Wrapped command arguments for record.</summary>
    public IReadOnlyList<string> WrappedArguments { get; init; } = Array.Empty<string>();

    /// <summary>Enable HTML report output.</summary>
    public bool Html { get; init; }

    /// <summary>Enable Markdown report output.</summary>
    public bool Markdown { get; init; }

    /// <summary>Open report after generation.</summary>
    public bool Open { get; init; }

    /// <summary>Export format (markdown, json).</summary>
    public string? Format { get; init; }

    /// <summary>Explicit config file path. Null when not specified.</summary>
    public string? ConfigPath { get; init; }

    /// <summary>Session ID for show, report --session, and export --session.</summary>
    public string? SessionId { get; init; }

    /// <summary>Export as GitHub PR summary.</summary>
    public bool GitHubPr { get; init; }

    /// <summary>Output file path for export.</summary>
    public string? Output { get; init; }

    /// <summary>Shell command string for record --shell.</summary>
    public string? Shell { get; init; }
}
