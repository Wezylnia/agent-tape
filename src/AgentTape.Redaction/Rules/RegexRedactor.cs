using System.Text.RegularExpressions;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Redaction.Rules;

public sealed partial class RegexRedactor : IRedactor
{
    public string Redact(string input, RedactionMode mode)
    {
        if (mode == RedactionMode.Off || string.IsNullOrEmpty(input))
        {
            return input;
        }

        var output = input;
        output = GitHubTokenRegex().Replace(output, "ghp_****REDACTED****");
        output = GitHubFineGrainedTokenRegex().Replace(output, "github_pat_****REDACTED****");
        output = OpenAiKeyRegex().Replace(output, "sk-****REDACTED****");
        output = AwsAccessKeyRegex().Replace(output, "AKIA****REDACTED****");
        output = JwtRegex().Replace(output, "eyJ****REDACTED****");
        output = PasswordAssignmentRegex().Replace(output, "$1=****");
        output = WindowsUserPathRegex().Replace(output, @"C:\Users\<user>\");

        if (mode == RedactionMode.Strict)
        {
            output = EmailRegex().Replace(output, "<email>");
        }

        return output;
    }

    [GeneratedRegex(@"ghp_[A-Za-z0-9_]{20,}", RegexOptions.Compiled)]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex(@"github_pat_[A-Za-z0-9_]{20,}", RegexOptions.Compiled)]
    private static partial Regex GitHubFineGrainedTokenRegex();

    [GeneratedRegex(@"sk-[A-Za-z0-9_-]{20,}", RegexOptions.Compiled)]
    private static partial Regex OpenAiKeyRegex();

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"(?i)\b(password|pwd|secret|token)\s*=\s*[^;\s]+", RegexOptions.Compiled)]
    private static partial Regex PasswordAssignmentRegex();

    [GeneratedRegex(@"C:\\Users\\[^\\\r\n]+\\", RegexOptions.Compiled)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
