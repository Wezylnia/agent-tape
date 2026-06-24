using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface IReportGenerator
{
    string Format { get; }

    Task<string> GenerateAsync(TapeSession session, CancellationToken cancellationToken);
}
