using AgentTape.Core.Models;
using AgentTape.Testing.DotNet;

namespace AgentTape.Testing.Tests.DotNet;

public sealed class TrxParserTests
{
    [Fact]
    public void Parse_reads_total_passed_failed_from_valid_trx()
    {
        var trx = """
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <ResultSummary outcome="Completed">
    <Counters total="42" executed="42" passed="40" failed="1" />
  </ResultSummary>
</TestRun>
""";
        var summary = TrxParser.Parse(trx);
        Assert.Equal(42, summary.Total);
        Assert.Equal(40, summary.Passed);
        Assert.Equal(1, summary.Failed);
    }

    [Fact]
    public void Parse_extracts_failed_test_names()
    {
        var trx = """
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <ResultSummary outcome="Completed">
    <Counters total="3" executed="3" passed="1" failed="2" />
  </ResultSummary>
  <Results>
    <UnitTestResult testName="Tests.FailingTest1" outcome="Failed" />
    <UnitTestResult testName="Tests.FailingTest2" outcome="Failed" />
    <UnitTestResult testName="Tests.PassingTest" outcome="Passed" />
  </Results>
</TestRun>
""";
        var summary = TrxParser.Parse(trx);
        Assert.Equal(3, summary.Total);
        Assert.Equal(2, summary.Failed);
        Assert.Contains("Tests.FailingTest1", summary.FailedTestNames);
        Assert.Contains("Tests.FailingTest2", summary.FailedTestNames);
    }

    [Fact]
    public void Parse_returns_empty_summary_for_invalid_xml()
    {
        var summary = TrxParser.Parse("not valid xml");
        Assert.False(summary.HasAnySignal);
    }

    [Fact]
    public void Parse_returns_empty_summary_for_empty_string()
    {
        var summary = TrxParser.Parse("");
        Assert.False(summary.HasAnySignal);
    }

    [Fact]
    public void Parse_handles_missing_counters()
    {
        var trx = """
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
</TestRun>
""";
        var summary = TrxParser.Parse(trx);
        Assert.False(summary.HasAnySignal);
    }
}
