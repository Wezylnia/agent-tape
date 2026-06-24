using System.Diagnostics;
using System.Text;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Core;

/// <summary>
/// Runs external processes for command recording. Captures stdout/stderr separately,
/// records timestamps, and supports cancellation that kills the process.
/// </summary>
public sealed class ProcessCommandRunner(IClock clock) : ICommandRunner
{
    /// <summary>
    /// Maximum length of the preview stored in CommandRun for quick display.
    /// Full output is always returned in CommandResult.
    /// </summary>
    public const int MaxPreviewLength = 4000;

    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = clock.UtcNow;
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var item in request.Environment)
        {
            startInfo.Environment[item.Key] = item.Value;
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                $"Could not start process '{request.Executable}'. Verify the executable is installed and accessible.", ex);
        }

        if (process is null)
        {
            throw new InvalidOperationException(
                $"Could not start process '{request.Executable}'. The process handle is null.");
        }

        // Register cancellation before starting stream reads to avoid race
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort kill; process may have already exited.
            }
        });

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var stdoutTask = ReadStreamAsync(process.StandardOutput, stdoutBuilder, cancellationToken);
        var stderrTask = ReadStreamAsync(process.StandardError, stderrBuilder, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Process was killed via the cancellation registration above.
            // Fall through to collect any remaining output.
        }

        await Task.WhenAll(stdoutTask, stderrTask);

        var finishedAt = clock.UtcNow;
        var stdout = stdoutBuilder.ToString();
        var stderr = stderrBuilder.ToString();

        return new CommandResult
        {
            Stdout = stdout,
            Stderr = stderr,
            Run = new CommandRun
            {
                Id = GenerateCommandId(),
                Command = request.DisplayCommand,
                Kind = Classify(request.DisplayCommand),
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                ExitCode = process.HasExited ? process.ExitCode : -1,
                RedactedStdoutPreview = TrimPreview(stdout),
                RedactedStderrPreview = TrimPreview(stderr)
            }
        };
    }

    private static async Task ReadStreamAsync(StreamReader reader, StringBuilder builder, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        int charsRead;
        try
        {
            while ((charsRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
            {
                builder.Append(buffer, 0, charsRead);
            }
        }
        catch (OperationCanceledException)
        {
            // Process was killed; stream read cancellation is expected.
        }
    }

    private static string GenerateCommandId()
    {
        // Short unique id, e.g., "c1", "c2"
        return $"c{Guid.NewGuid():N}"[..10];
    }

    internal static CommandKind Classify(string command)
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
        return value.Length <= MaxPreviewLength ? value : value[..MaxPreviewLength];
    }
}

