using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface IRiskRule
{
    string Code { get; }

    IReadOnlyList<RiskWarning> Evaluate(TapeSession session);
}
