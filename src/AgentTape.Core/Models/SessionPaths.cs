namespace AgentTape.Core.Models;

public sealed record SessionPaths
{
    public required string RootDirectory { get; init; }

    public required string SessionJsonPath { get; init; }

    public required string CommandsJsonlPath { get; init; }

    public required string StdoutDirectory { get; init; }

    public required string StderrDirectory { get; init; }

    public required string GitDirectory { get; init; }

    public required string TestsDirectory { get; init; }

    /// <summary>Session-specific reports directory.</summary>
    public required string SessionReportsDirectory { get; init; }

    /// <summary>Global reports directory for latest aliases.</summary>
    public required string GlobalReportsDirectory { get; init; }

    public string SessionHtmlReportPath => Path.Combine(SessionReportsDirectory, "session.html");

    public string SessionMarkdownReportPath => Path.Combine(SessionReportsDirectory, "session.md");

    public string LatestHtmlReportPath => Path.Combine(GlobalReportsDirectory, "latest.html");

    public string LatestMarkdownReportPath => Path.Combine(GlobalReportsDirectory, "latest.md");
}
