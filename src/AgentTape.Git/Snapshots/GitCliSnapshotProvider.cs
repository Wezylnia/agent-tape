using System.Diagnostics;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Git.Snapshots;

public sealed class GitCliSnapshotProvider : IGitSnapshotProvider
{
    public async Task<GitSnapshot> CaptureAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        if (!await IsGitRepositoryAsync(workingDirectory, cancellationToken))
        {
            return new GitSnapshot { IsRepository = false };
        }

        var branch = await RunGitAsync(workingDirectory, ["branch", "--show-current"], cancellationToken);
        var head = await RunGitAsync(workingDirectory, ["rev-parse", "--short", "HEAD"], cancellationToken);
        var status = await RunGitAsync(workingDirectory, ["status", "--porcelain=v1"], cancellationToken);

        return new GitSnapshot
        {
            IsRepository = true,
            Branch = branch.Trim(),
            HeadSha = head.Trim(),
            StatusText = status,
            Changes = ParsePorcelainStatus(status)
        };
    }

    public async Task<string> CaptureDiffAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        if (!await IsGitRepositoryAsync(workingDirectory, cancellationToken))
        {
            return string.Empty;
        }

        return await RunGitAsync(workingDirectory, ["diff", "--no-ext-diff"], cancellationToken);
    }

    private static async Task<bool> IsGitRepositoryAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        var result = await RunGitAsync(workingDirectory, ["rev-parse", "--is-inside-work-tree"], cancellationToken, throwOnError: false);
        return result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<FileChange> ParsePorcelainStatus(string status)
    {
        var changes = new List<FileChange>();
        foreach (var line in status.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.Length < 4)
            {
                continue;
            }

            var code = line[..2];
            var path = line[3..];
            changes.Add(new FileChange
            {
                Path = path,
                Kind = code.Contains('A') ? FileChangeKind.Added :
                    code.Contains('D') ? FileChangeKind.Deleted :
                    code.Contains('R') ? FileChangeKind.Renamed :
                    code.Contains('M') ? FileChangeKind.Modified :
                    FileChangeKind.Unknown
            });
        }

        return changes;
    }

    private static async Task<string> RunGitAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        bool throwOnError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return string.Empty;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (throwOnError && process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}");
        }

        return await stdoutTask;
    }
}
