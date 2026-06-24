using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

/// <summary>
/// Reads stored session data from disk for listing and querying.
/// </summary>
public interface ISessionReader
{
    /// <summary>
    /// Lists all stored sessions, newest first.
    /// </summary>
    Task<IReadOnlyList<TapeSession>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds a specific session by ID, or null if not found.
    /// </summary>
    Task<TapeSession?> FindAsync(string sessionId, CancellationToken cancellationToken);
}
