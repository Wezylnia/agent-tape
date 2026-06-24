using System.Diagnostics;
using System.Text;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Core;

public sealed class ProcessCommandRunner(IClock clock) : ICommandRunner
{
    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        var startedAt = clock.UtcNow;
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var item in request.Environment)
        {
            startInfo.Environment[item.Key] = item.Value;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start process '{request.Executable}'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var finishedAt = clock.UtcNow;

        return new CommandResult
        {
            Stdout = stdout,
            Stderr = stderr,
            Run = new CommandRun
            {
                Id = "001",
                Command = request.DisplayCommand,
                Kind = Classify(request.DisplayCommand),
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                ExitCode = process.ExitCode,
                RedactedStdoutPreview = TrimPreview(stdout),
                RedactedStderrPreview = TrimPreview(stderr)
            }
        };
    }

    private static CommandKind Classify(string command)
    {
        var normalized = command.ToLowerInvariant();

        if (normalized.Contains(" test", StringComparison.Ordinal) || normalized.EndsWith("test", StringComparison.Ordinal))
        {
            return CommandKind.Test;
        }

        if (normalized.Contains(" build", StringComparison.Ordinal) || normalized.EndsWith("build", StringComparison.Ordinal))
        {
            return CommandKind.Build;
        }

        if (normalized.StartsWith("git ", StringComparison.Ordinal))
        {
            return CommandKind.Git;
        }

        if (normalized.StartsWith("codex", StringComparison.Ordinal) ||
            normalized.StartsWith("claude", StringComparison.Ordinal) ||
            normalized.StartsWith("aider", StringComparison.Ordinal))
        {
            return CommandKind.Agent;
        }

        return CommandKind.Unknown;
    }

    private static string TrimPreview(string value)
    {
        const int maxLength = 4000;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
