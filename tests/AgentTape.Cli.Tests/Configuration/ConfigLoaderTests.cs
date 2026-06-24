using AgentTape.Cli.Configuration;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;

namespace AgentTape.Cli.Tests.Configuration;

public sealed class ConfigLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"agenttape-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void ConfigLoader_uses_defaults_when_file_missing()
    {
        var configPath = Path.Combine(_tempDir, "nonexistent.yml");
        var options = ConfigLoader.Load(configPath);
        Assert.Equal(RedactionMode.Standard, options.RedactionMode);
    }

    [Fact]
    public void ConfigLoader_reads_redaction_mode()
    {
        var configPath = WriteConfig("""
redaction:
  mode: strict
""");

        var options = ConfigLoader.Load(configPath);
        Assert.Equal(RedactionMode.Strict, options.RedactionMode);
    }

    [Fact]
    public void ConfigLoader_rejects_invalid_redaction_mode()
    {
        var configPath = WriteConfig("""
redaction:
  mode: none
""");

        Assert.Throws<InvalidOperationException>(() => ConfigLoader.Load(configPath));
    }

    [Fact]
    public void ConfigLoader_reads_capture_gitDiff()
    {
        var configPath = WriteConfig("""
capture:
  gitDiff: false
""");

        var options = ConfigLoader.Load(configPath);
        Assert.False(options.CaptureGit);
    }

    [Fact]
    public void ConfigLoader_reads_project_name()
    {
        var configPath = WriteConfig("""
project:
  name: my-test-project
""");

        var options = ConfigLoader.Load(configPath);
        Assert.Equal("my-test-project", options.ProjectName);
    }

    [Fact]
    public void ConfigLoader_defaults_to_standard_redaction()
    {
        var configPath = WriteConfig("""
project:
  name: test
""");

        var options = ConfigLoader.Load(configPath);
        Assert.Equal(RedactionMode.Standard, options.RedactionMode);
    }

    [Fact]
    public void ConfigLoader_ignores_comments_and_empty_lines()
    {
        var configPath = WriteConfig("""
# This is a comment
project:
  name: test

redaction:
  mode: strict
""");

        var options = ConfigLoader.Load(configPath);
        Assert.Equal(RedactionMode.Strict, options.RedactionMode);
        Assert.Equal("test", options.ProjectName);
    }

    private string WriteConfig(string content)
    {
        var path = Path.Combine(_tempDir, ".agenttape.yml");
        File.WriteAllText(path, content);
        return path;
    }
}
