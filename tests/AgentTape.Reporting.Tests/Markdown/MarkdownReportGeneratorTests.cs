using AgentTape.Core.Models;
using AgentTape.Reporting.Markdown;

namespace AgentTape.Reporting.Tests.Markdown;

public sealed class MarkdownReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_includes_core_session_counts()
    {
        var session = new TapeSession
        {
            Id = "test",
            Name = "sample",
            StartedAt = DateTimeOffset.UnixEpoch,
            FinishedAt = DateTimeOffset.UnixEpoch.AddSeconds(3),
            WorkingDirectory = "C:/repo",
            Commands =
            [
                new CommandRun
                {
                    Id = "001",
                    Command = "dotnet test",
                    StartedAt = DateTimeOffset.UnixEpoch,
                    FinishedAt = DateTimeOffset.UnixEpoch.AddSeconds(3),
                    ExitCode = 0
                }
            ]
        };

        var markdown = await new MarkdownReportGenerator().GenerateAsync(session, CancellationToken.None);

        Assert.Contains("Commands executed: 1", markdown);
        Assert.Contains("dotnet test", markdown);
    }
}
