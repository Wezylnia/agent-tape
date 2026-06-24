namespace AgentTape.Cli.Commands;

/// <summary>
/// Standard exit codes for AgentTape CLI commands.
/// </summary>
public static class CommandExitCodes
{
    /// <summary>AgentTape command succeeded.</summary>
    public const int Success = 0;

    /// <summary>CLI usage error (bad arguments, unknown options).</summary>
    public const int UsageError = 2;

    /// <summary>AgentTape internal recording failure.</summary>
    public const int InternalFailure = 3;
}
