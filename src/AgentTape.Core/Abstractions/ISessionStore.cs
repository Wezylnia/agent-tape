using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface ISessionStore
{
    Task<SessionPaths> CreateSessionLayoutAsync(TapeSession session, CancellationToken cancellationToken);

    Task SaveSessionAsync(TapeSession session, SessionPaths paths, CancellationToken cancellationToken);

    Task SaveRedactionLogAsync(SessionPaths paths, IReadOnlyList<RedactionMatchSummary> summaries, CancellationToken cancellationToken);
}
