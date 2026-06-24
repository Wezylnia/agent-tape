using AgentTape.Core.Abstractions;

namespace AgentTape.Core;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
