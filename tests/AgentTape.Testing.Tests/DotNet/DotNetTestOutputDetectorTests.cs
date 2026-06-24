using AgentTape.Core.Models;
using AgentTape.Testing.DotNet;

namespace AgentTape.Testing.Tests.DotNet;

public sealed class DotNetTestOutputDetectorTests
{
    private readonly DotNetTestOutputDetector _detector = new();

    [Fact]
    public void Detect_returns_empty_summary_for_unrelated_output()
    {
        var summary = _detector.Detect("dotnet build", "Build succeeded.", "");
        Assert.False(summary.HasAnySignal);
    }

    [Fact]
    public void Detect_parses_total_passed_failed_skipped()
    {
        var stdout = """
Test run for C:\proj\tests\bin\Debug\net10.0\MyTests.dll (.NET 10.0)
Total tests: 42
    Passed: 40
    Failed: 1
    Skipped: 1
""";
        var summary = _detector.Detect("dotnet test", stdout, "");
        Assert.Equal(42, summary.Total);
        Assert.Equal(40, summary.Passed);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Skipped);
    }

    [Fact]
    public void Detect_handles_failed_output_with_stderr()
    {
        var stdout = "Total tests: 5\nPassed: 3\nFailed: 2\nSkipped: 0";
        var stderr = "Some error output";
        var summary = _detector.Detect("dotnet test", stdout, stderr);
        Assert.Equal(5, summary.Total);
        Assert.Equal(3, summary.Passed);
        Assert.Equal(2, summary.Failed);
    }

    [Fact]
    public void Detect_handles_multiline_output()
    {
        var output = """
Microsoft (R) Test Execution Command Line Tool Version 18.0.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Total tests: 100
     Passed: 95
     Failed: 3
    Skipped: 2

Test Run Successful.
""";
        var summary = _detector.Detect("dotnet test", output, "");
        Assert.Equal(100, summary.Total);
        Assert.Equal(95, summary.Passed);
        Assert.Equal(3, summary.Failed);
        Assert.Equal(2, summary.Skipped);
    }

    [Fact]
    public void Detect_handles_missing_skipped_count()
    {
        var stdout = "Total tests: 10\nPassed: 10\nFailed: 0";
        var summary = _detector.Detect("dotnet test", stdout, "");
        Assert.Equal(10, summary.Total);
        Assert.Equal(10, summary.Passed);
        Assert.Equal(0, summary.Failed);
    }

    [Fact]
    public void Detect_handles_zero_tests()
    {
        var stdout = "Total tests: 0\nPassed: 0\nFailed: 0\nSkipped: 0";
        var summary = _detector.Detect("dotnet test", stdout, "");
        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.Passed);
    }

    [Fact]
    public void Detect_does_not_throw_on_malformed_output()
    {
        var summary = _detector.Detect("dotnet test", null!, null!);
        Assert.False(summary.HasAnySignal);
    }

    [Fact]
    public void Detect_does_not_capture_secret_like_text_as_test_name()
    {
        var stdout = "Total tests: 1\nPassed: 1\nFailed: 0\nSkipped: 0";
        var summary = _detector.Detect("dotnet test", stdout, "");
        // Should not capture any sensitive data
        Assert.NotNull(summary);
        Assert.Empty(summary.FailedTestNames);
    }
}
