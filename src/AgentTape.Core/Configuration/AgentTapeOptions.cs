using AgentTape.Core.Models;

namespace AgentTape.Core.Configuration;

public sealed record AgentTapeOptions
{
    public string ProjectName { get; init; } = "agenttape-project";

    public bool CaptureGit { get; init; } = true;

    public bool CaptureStdout { get; init; } = true;

    public bool CaptureStderr { get; init; } = true;

    public RedactionMode RedactionMode { get; init; } = RedactionMode.Standard;

    public string AgentTapeDirectory { get; init; } = ".agenttape";
}
