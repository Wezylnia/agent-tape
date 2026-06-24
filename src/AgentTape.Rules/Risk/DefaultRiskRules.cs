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

    public string Code => "DEFAULT_RISK_RULES";

    public IReadOnlyList<RiskWarning> Evaluate(TapeSession session)
    {
        var warnings = new List<RiskWarning>();

        foreach (var change in session.FileChanges)
        {
            var normalized = change.Path.Replace('\\', '/').ToLowerInvariant();
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

            if (normalized.EndsWith(".config", StringComparison.Ordinal) ||
                normalized.EndsWith(".json", StringComparison.Ordinal) && normalized.Contains("settings", StringComparison.Ordinal))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "CONFIG_CHANGED",
                    Severity = RiskSeverity.Warning,
                    FilePath = change.Path,
                    Message = $"Configuration file changed: {change.Path}"
                });
            }
        }

        foreach (var command in session.Commands)
        {
            var normalized = command.Command.ToLowerInvariant();
            if (normalized.Contains("rm -rf", StringComparison.Ordinal) ||
                normalized.Contains("del /s", StringComparison.Ordinal) ||
                normalized.Contains("invoke-expression", StringComparison.Ordinal) ||
                normalized.Contains("curl", StringComparison.Ordinal) && normalized.Contains("|", StringComparison.Ordinal))
            {
                warnings.Add(new RiskWarning
                {
                    Code = "SUSPICIOUS_COMMAND",
                    Severity = RiskSeverity.High,
                    CommandId = command.Id,
                    Message = $"Suspicious command pattern captured: {command.Command}"
                });
            }
        }

        return warnings;
    }
}
