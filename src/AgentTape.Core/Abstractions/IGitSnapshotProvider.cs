using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface IGitSnapshotProvider
{
    Task<GitSnapshot> CaptureAsync(string workingDirectory, CancellationToken cancellationToken);

    Task<string> CaptureDiffAsync(string workingDirectory, CancellationToken cancellationToken);

    /// <summary>
    /// Captures git diff --numstat output to get line counts and binary detection.
    /// Returns a list of (path, addedLines, deletedLines, isBinary) tuples.
    /// </summary>
    Task<IReadOnlyList<(string Path, int? AddedLines, int? DeletedLines, bool IsBinary)>> CaptureNumStatAsync(
        string workingDirectory, CancellationToken cancellationToken);
}
