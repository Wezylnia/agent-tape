using System.Xml.Linq;
using AgentTape.Core.Models;

namespace AgentTape.Testing.DotNet;

/// <summary>
/// Parses .NET TRX test result files to extract test summaries.
/// </summary>
public static class TrxParser
{
    private const string Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static TestSummary Parse(string trxContent)
    {
        try
        {
            var doc = XDocument.Parse(trxContent);
            var root = doc.Root;
            if (root is null) return new TestSummary();

            var resultSummary = root.Element(XName.Get("ResultSummary", Ns));
            if (resultSummary is null) return new TestSummary();

            var counters = resultSummary.Element(XName.Get("Counters", Ns));
            if (counters is null) return new TestSummary();

            var total = ParseIntAttr(counters, "total");
            var passed = ParseIntAttr(counters, "passed");
            var failed = ParseIntAttr(counters, "failed");
            var skipped = ParseIntAttr(counters, "total") - ParseIntAttr(counters, "executed");

            // Extract failed test names
            var failedNames = new List<string>();
            var results = root.Element(XName.Get("Results", Ns));
            if (results is not null)
            {
                foreach (var unitTestResult in results.Elements(XName.Get("UnitTestResult", Ns)))
                {
                    var outcome = unitTestResult.Attribute("outcome")?.Value;
                    if (outcome == "Failed")
                    {
                        var testName = unitTestResult.Attribute("testName")?.Value;
                        if (testName is not null)
                        {
                            failedNames.Add(testName);
                        }
                    }
                }
            }

            return new TestSummary
            {
                Total = total,
                Passed = passed,
                Failed = failed,
                Skipped = skipped > 0 ? skipped : null,
                FailedTestNames = failedNames
            };
        }
        catch
        {
            return new TestSummary();
        }
    }

    private static int? ParseIntAttr(XElement element, string name)
    {
        var attr = element.Attribute(name);
        if (attr is not null && int.TryParse(attr.Value, out var val))
            return val;
        return null;
    }
}
