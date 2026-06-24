using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Rules.Risk;

public sealed class DefaultRiskRules : IRiskRule
{
    private static readonly string[] SensitivePathFragments =
    [
        ".env",
        "appsettings.production.json",
        "secrets.json",
        "id_rsa",
        ".pfx",
        ".pem",
        ".key"
    ];

    private static readonly string[] LockfilePatterns =
    [
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "packages.lock.json",
        "gemfile.lock",
        "cargo.lock",
        "poetry.lock"
    ];

    private static readonly string[] MigrationPatterns =
    [
        "migration",
        "migrations"
    ];

    public string Code => "DEFAULT_RISK_RULES";

    public IReadOnlyList<RiskWarning> Evaluate(TapeSession session)
    {
        var warnings = new List<RiskWarning>();

        EvaluateFileChanges(session, warnings);
        EvaluateCommands(session, warnings);

        return warnings;
    }

    private static void EvaluateFileChanges(TapeSession session, List<RiskWarning> warnings)
    {
        foreach (var change in session.FileChanges)
        {
            var normalized = change.Path.Replace('\\', '/').ToLowerInvariant();

            // Secret file touched
            if (SensitivePathFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal)))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "SECRET_FILE_TOUCHED",
                    Severity = RiskSeverity.High,
                    FilePath = change.Path,
                    Message = $"Sensitive-looking file changed: {change.Path}"
                });
            }

            // Config changed
            if ((normalized.EndsWith(".config", StringComparison.Ordinal) ||
                 (normalized.EndsWith(".json", StringComparison.Ordinal) && normalized.Contains("settings", StringComparison.Ordinal))) &&
                !normalized.Contains("secrets", StringComparison.Ordinal))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "CONFIG_CHANGED",
                    Severity = RiskSeverity.Warning,
                    FilePath = change.Path,
                    Message = $"Configuration file changed: {change.Path}"
                });
            }

            // Lockfile changed
            if (LockfilePatterns.Any(p => normalized.EndsWith(p, StringComparison.Ordinal) ||
                                           normalized.Contains($"/{p}", StringComparison.Ordinal)))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "LOCKFILE_CHANGED",
                    Severity = RiskSeverity.Warning,
                    FilePath = change.Path,
                    Message = $"Lockfile changed: {change.Path}"
                });
            }

            // Migration changed
            if (MigrationPatterns.Any(p => normalized.Contains(p, StringComparison.Ordinal)))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "MIGRATION_CHANGED",
                    Severity = RiskSeverity.Warning,
                    FilePath = change.Path,
                    Message = $"Migration file changed: {change.Path}"
                });
            }

            // Binary changed
            if (change.IsBinary)
            {
                warnings.Add(new RiskWarning
                {
                    Code = "BINARY_CHANGED",
                    Severity = RiskSeverity.Warning,
                    FilePath = change.Path,
                    Message = $"Binary file changed: {change.Path}"
                });
            }

            // Large delete
            if (change.Kind == FileChangeKind.Deleted && change.DeletedLines.GetValueOrDefault() > 100)
            {
                warnings.Add(new RiskWarning
                {
                    Code = "LARGE_DELETE",
                    Severity = RiskSeverity.High,
                    FilePath = change.Path,
                    Message = $"Large file deleted ({change.DeletedLines} lines): {change.Path}"
                });
            }
        }
    }

    private static void EvaluateCommands(TapeSession session, List<RiskWarning> warnings)
    {
        foreach (var command in session.Commands)
        {
            var normalized = command.Command.ToLowerInvariant();

            // Suspicious commands
            if (normalized.Contains("rm -rf", StringComparison.Ordinal) ||
                normalized.Contains("del /s", StringComparison.Ordinal) ||
                normalized.Contains("invoke-expression", StringComparison.Ordinal) ||
                (normalized.Contains("curl", StringComparison.Ordinal) && normalized.Contains('|')) ||
                (normalized.Contains("wget", StringComparison.Ordinal) && normalized.Contains('|')))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "SUSPICIOUS_COMMAND",
                    Severity = RiskSeverity.High,
                    CommandId = command.Id,
                    Message = $"Suspicious command pattern captured: {command.Command}"
                });
            }

            // Failed build
            if (command.Kind == CommandKind.Build && command.ExitCode != 0)
            {
                warnings.Add(new RiskWarning
                {
                    Code = "BUILD_FAILED",
                    Severity = RiskSeverity.High,
                    CommandId = command.Id,
                    Message = $"Build command failed with exit code {command.ExitCode}: {command.Command}"
                });
            }

            // Failed test
            if (command.Kind == CommandKind.Test && command.ExitCode != 0)
            {
                warnings.Add(new RiskWarning
                {
                    Code = "TEST_FAILED",
                    Severity = RiskSeverity.High,
                    CommandId = command.Id,
                    Message = $"Test command failed with exit code {command.ExitCode}: {command.Command}"
                });
            }
        }
    }
}
