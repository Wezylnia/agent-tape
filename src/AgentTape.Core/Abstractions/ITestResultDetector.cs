using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface ITestResultDetector
{
    TestSummary Detect(string command, string stdout, string stderr);
}
