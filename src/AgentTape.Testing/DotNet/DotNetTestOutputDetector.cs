using System.Text.RegularExpressions;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Testing.DotNet;

public sealed partial class DotNetTestOutputDetector : ITestResultDetector
{
    public TestSummary Detect(string command, string stdout, string stderr)
    {
        var combined = $"{stdout}\n{stderr}";
        var match = DotNetSummaryRegex().Match(combined);
        if (!match.Success)
        {
            return new TestSummary();
        }

        return new TestSummary
        {
            Total = ParseInt(match.Groups["total"].Value),
            Passed = ParseInt(match.Groups["passed"].Value),
            Failed = ParseInt(match.Groups["failed"].Value),
            Skipped = ParseInt(match.Groups["skipped"].Value)
        };
    }

    private static int? ParseInt(string value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"Total tests:\s*(?<total>\d+).+?Passed:\s*(?<passed>\d+).+?Failed:\s*(?<failed>\d+).+?Skipped:\s*(?<skipped>\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex DotNetSummaryRegex();
}
