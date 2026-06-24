using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface IGitSnapshotProvider
{
    Task<GitSnapshot> CaptureAsync(string workingDirectory, CancellationToken cancellationToken);

    Task<string> CaptureDiffAsync(string workingDirectory, CancellationToken cancellationToken);
}
