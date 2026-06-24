using AgentTape.Core.Storage;

namespace AgentTape.Core.Tests.Storage;

public sealed class SessionIdFactoryTests
{
    [Fact]
    public void Create_uses_timestamp_prefix()
    {
        var startedAt = new DateTimeOffset(2026, 6, 24, 14, 20, 1, TimeSpan.Zero);
        var id = SessionIdFactory.Create("fix-tests", startedAt);
        Assert.StartsWith("2026-06-24-142001-", id);
    }

    [Fact]
    public void Create_lowercases_name()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("MY-PROJECT", startedAt);
        Assert.Contains("my-project", id);
    }

    [Fact]
    public void Create_replaces_spaces_with_hyphens()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("fix the build", startedAt);
        Assert.Contains("fix-the-build", id);
    }

    [Fact]
    public void Create_removes_invalid_path_characters()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("hello:world*test?path<ok>", startedAt);
        Assert.DoesNotContain(":", id);
        Assert.DoesNotContain("*", id);
        Assert.DoesNotContain("?", id);
        Assert.DoesNotContain("<", id);
        Assert.DoesNotContain(">", id);
    }

    [Fact]
    public void Create_collapses_repeated_hyphens()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("fix---tests", startedAt);
        Assert.DoesNotContain("---", id);
    }

    [Fact]
    public void Create_uses_session_when_name_is_empty()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("", startedAt);
        Assert.Contains("-session", id);
    }

    [Fact]
    public void Create_uses_session_when_name_is_whitespace()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var id = SessionIdFactory.Create("   ", startedAt);
        Assert.Contains("-session", id);
    }

    [Fact]
    public void Create_limits_length()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var veryLongName = new string('a', 200);
        var id = SessionIdFactory.Create(veryLongName, startedAt);
        Assert.True(id.Length <= 80);
    }

    [Fact]
    public void Create_is_deterministic_for_same_input()
    {
        var startedAt = new DateTimeOffset(2026, 6, 24, 14, 20, 1, TimeSpan.Zero);
        var id1 = SessionIdFactory.Create("fix-tests", startedAt);
        var id2 = SessionIdFactory.Create("fix-tests", startedAt);
        Assert.Equal(id1, id2);
    }
}
