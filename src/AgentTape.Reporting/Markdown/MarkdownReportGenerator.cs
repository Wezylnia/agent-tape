using System.Text;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Reporting.Markdown;

public sealed class MarkdownReportGenerator : IReportGenerator
{
    public string Format => "markdown";

    public Task<string> GenerateAsync(TapeSession session, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AgentTape Session Summary");
        builder.AppendLine();
        builder.AppendLine($"- Session: `{session.Name}`");
        builder.AppendLine($"- Duration: {FormatDuration(session.Duration)}");
        builder.AppendLine($"- Working directory: `{session.WorkingDirectory}`");
        builder.AppendLine($"- Commands executed: {session.Commands.Count}");
        builder.AppendLine($"- Files changed: {session.FileChanges.Count}");
        builder.AppendLine($"- Risk warnings: {session.Warnings.Count}");

        if (session.AfterGit is { IsRepository: true })
        {
            builder.AppendLine($"- Branch: `{session.AfterGit.Branch}`");
            builder.AppendLine($"- HEAD: `{session.AfterGit.HeadSha}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Commands");
        foreach (var command in session.Commands)
        {
            builder.AppendLine($"- `{command.Command}` exited `{command.ExitCode}` in {FormatDuration(command.Duration)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Changed Files");
        if (session.FileChanges.Count == 0)
        {
            builder.AppendLine("- No changed files captured.");
        }
        else
        {
            foreach (var change in session.FileChanges)
            {
                builder.AppendLine($"- {change.Kind}: `{change.Path}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Risk Warnings");
        if (session.Warnings.Count == 0)
        {
            builder.AppendLine("- No risk warnings detected.");
        }
        else
        {
            foreach (var warning in session.Warnings)
            {
                builder.AppendLine($"- **{warning.Code}** ({warning.Severity}): {warning.Message}");
            }
        }

        return Task.FromResult(builder.ToString());
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 60
            ? $"{duration.TotalSeconds:0.0}s"
            : $"{duration.TotalMinutes:0.0}m";
    }
}
