using System.Diagnostics;
using AgentTape.Core.Models;
using AgentTape.Git.Snapshots;

namespace AgentTape.Git.Tests.Snapshots;

public sealed class GitCliSnapshotProviderTests : IDisposable
{
    private readonly string _tempRepo;
    private readonly GitCliSnapshotProvider _provider;

    public GitCliSnapshotProviderTests()
    {
        _tempRepo = Path.Combine(Path.GetTempPath(), $"agenttape-git-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRepo);
        _provider = new GitCliSnapshotProvider();

        // Initialize git repo
        RunGit("init");
        RunGit("config", "user.email", "test@agenttape.dev");
        RunGit("config", "user.name", "AgentTape Test");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRepo))
            {
                Directory.Delete(_tempRepo, recursive: true);
            }
        }
        catch
        {
            // Git may hold file locks briefly; ignore cleanup failures in tests.
        }
    }

    // --- Unit tests for porcelain parser ---

    [Fact]
    public void ParsePorcelainStatus_parses_added_file()
    {
        var status = "A  newfile.cs\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
        Assert.Equal("newfile.cs", changes[0].Path);
        Assert.Equal(FileChangeKind.Added, changes[0].Kind);
    }

    [Fact]
    public void ParsePorcelainStatus_parses_modified_file()
    {
        var status = " M modified.cs\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
        Assert.Equal("modified.cs", changes[0].Path);
        Assert.Equal(FileChangeKind.Modified, changes[0].Kind);
    }

    [Fact]
    public void ParsePorcelainStatus_parses_deleted_file()
    {
        var status = " D deleted.cs\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
        Assert.Equal("deleted.cs", changes[0].Path);
        Assert.Equal(FileChangeKind.Deleted, changes[0].Kind);
    }

    [Fact]
    public void ParsePorcelainStatus_parses_renamed_file()
    {
        var status = "R  old.cs -> new.cs\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
        Assert.Equal("new.cs", changes[0].Path);
        Assert.Equal("old.cs", changes[0].OldPath);
        Assert.Equal(FileChangeKind.Renamed, changes[0].Kind);
    }

    [Fact]
    public void ParsePorcelainStatus_ignores_empty_lines()
    {
        var status = "\n\n M file.cs\n\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
    }

    [Fact]
    public void ParsePorcelainStatus_handles_untracked_file()
    {
        var status = "?? newfile.txt\n";
        var changes = GitCliSnapshotProvider.ParsePorcelainStatus(status);
        Assert.Single(changes);
        Assert.Equal("newfile.txt", changes[0].Path);
        Assert.Equal(FileChangeKind.Untracked, changes[0].Kind);
    }

    // --- Integration tests using temporary git repository ---

    [Fact]
    public async Task CaptureAsync_returns_non_repository_snapshot_outside_git_repo()
    {
        var nonRepoDir = Path.Combine(Path.GetTempPath(), $"agenttape-nonrepo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(nonRepoDir);
        try
        {
            var snapshot = await _provider.CaptureAsync(nonRepoDir, CancellationToken.None);
            Assert.False(snapshot.IsRepository);
        }
        finally
        {
            Directory.Delete(nonRepoDir, recursive: true);
        }
    }

    [Fact]
    public async Task CaptureAsync_returns_branch_for_git_repo()
    {
        // Create initial commit so repo has a branch
        File.WriteAllText(Path.Combine(_tempRepo, "README.md"), "# Test");
        RunGit("add", ".");
        RunGit("commit", "-m", "Initial commit");

        var snapshot = await _provider.CaptureAsync(_tempRepo, CancellationToken.None);
        Assert.True(snapshot.IsRepository);
        Assert.NotNull(snapshot.Branch);
        Assert.NotEmpty(snapshot.Branch);
    }

    [Fact]
    public async Task CaptureAsync_returns_head_sha_after_initial_commit()
    {
        File.WriteAllText(Path.Combine(_tempRepo, "README.md"), "# Test");
        RunGit("add", ".");
        RunGit("commit", "-m", "Initial commit");

        var snapshot = await _provider.CaptureAsync(_tempRepo, CancellationToken.None);
        Assert.NotNull(snapshot.HeadSha);
        Assert.NotEmpty(snapshot.HeadSha);
    }

    [Fact]
    public async Task CaptureAsync_lists_modified_file()
    {
        // Create and commit a file
        var filePath = Path.Combine(_tempRepo, "program.cs");
        File.WriteAllText(filePath, "// original");
        RunGit("add", ".");
        RunGit("commit", "-m", "Initial commit");

        // Modify it
        File.WriteAllText(filePath, "// modified");

        var snapshot = await _provider.CaptureAsync(_tempRepo, CancellationToken.None);
        Assert.Contains(snapshot.Changes, c => c.Path == "program.cs" && c.Kind == FileChangeKind.Modified);
    }

    [Fact]
    public async Task CaptureDiffAsync_returns_diff_for_modified_file()
    {
        // Create and commit a file
        var filePath = Path.Combine(_tempRepo, "program.cs");
        File.WriteAllText(filePath, "// original\n");
        RunGit("add", ".");
        RunGit("commit", "-m", "Initial commit");

        // Modify it
        File.WriteAllText(filePath, "// modified\n");

        var diff = await _provider.CaptureDiffAsync(_tempRepo, CancellationToken.None);
        Assert.Contains("// original", diff);
        Assert.Contains("// modified", diff);
    }

    private void RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _tempRepo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}");
        }
    }
}
