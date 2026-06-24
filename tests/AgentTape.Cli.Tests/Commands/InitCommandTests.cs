using AgentTape.Cli.Commands;

namespace AgentTape.Cli.Tests.Commands;

public sealed class InitCommandTests : IDisposable
{
    private readonly string _tempDir;

    public InitCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"agenttape-init-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Init_creates_config_when_missing()
    {
        var configPath = Path.Combine(_tempDir, ".agenttape.yml");
        Assert.False(File.Exists(configPath));

        var exitCode = InitCommand.Execute(_tempDir);

        Assert.Equal(CommandExitCodes.Success, exitCode);
        Assert.True(File.Exists(configPath));

        var content = File.ReadAllText(configPath);
        Assert.Contains("project:", content);
        Assert.Contains("redaction:", content);
        Assert.Contains("mode: standard", content);
    }

    [Fact]
    public void Init_does_not_overwrite_existing_config()
    {
        var configPath = Path.Combine(_tempDir, ".agenttape.yml");
        var customContent = "project:\n  name: custom-project\n";
        File.WriteAllText(configPath, customContent);

        var exitCode = InitCommand.Execute(_tempDir);

        Assert.Equal(CommandExitCodes.Success, exitCode);
        var content = File.ReadAllText(configPath);
        // Should preserve original content
        Assert.Contains("custom-project", content);
        Assert.DoesNotContain("agenttape-project", content);
    }
}
