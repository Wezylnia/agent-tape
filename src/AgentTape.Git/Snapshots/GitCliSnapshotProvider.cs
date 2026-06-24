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

    internal static IReadOnlyList<FileChange> ParsePorcelainStatus(string status)
    {
        var changes = new List<FileChange>();
        // Do NOT trim entries - leading space is significant in porcelain v1 format
        foreach (var line in status.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.TrimEnd('\r', '\n');
            if (trimmedLine.Length < 3)
            {
                continue;
            }

            var indexCode = line[0];
            var workTreeCode = line[1];
            var rest = line[3..];

            // Handle renamed/copied: "R  old -> new"
            string? oldPath = null;
            string path;
            var arrowIndex = rest.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrowIndex >= 0)
            {
                oldPath = rest[..arrowIndex].Trim();
                path = rest[(arrowIndex + 4)..].Trim();
            }
            else
            {
                path = rest.Trim();
                // Remove surrounding quotes if present
                if (path.StartsWith('"') && path.EndsWith('"'))
                {
                    path = path[1..^1];
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var kind = ClassifyStatus(indexCode, workTreeCode);

            changes.Add(new FileChange
            {
                Path = path,
                OldPath = oldPath,
                Kind = kind
            });
        }

        return changes;
    }

    private static FileChangeKind ClassifyStatus(char indexCode, char workTreeCode)
    {
        // Check index (staging area) first, then worktree
        var primary = indexCode != ' ' && indexCode != '?' && indexCode != '!' ? indexCode : workTreeCode;

        return primary switch
        {
            'A' => FileChangeKind.Added,
            'M' => FileChangeKind.Modified,
            'D' => FileChangeKind.Deleted,
            'R' => FileChangeKind.Renamed,
            'C' => FileChangeKind.Copied,
            'U' => FileChangeKind.Unmerged,
            'T' => FileChangeKind.TypeChanged,
            _ => FileChangeKind.Unknown
        };
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
