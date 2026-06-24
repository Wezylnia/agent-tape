using AgentTape.Core.Models;

namespace AgentTape.Core.Tests.Models;

public sealed class TapeSessionTests
{
    [Fact]
    public void Duration_is_derived_from_start_and_finish()
    {
        var session = new TapeSession
        {
            Id = "test",
            Name = "sample",
            StartedAt = DateTimeOffset.UnixEpoch,
            FinishedAt = DateTimeOffset.UnixEpoch.AddSeconds(10),
            WorkingDirectory = "C:/repo"
        };

        Assert.Equal(TimeSpan.FromSeconds(10), session.Duration);
    }
}
