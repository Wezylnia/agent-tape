using System.Text.RegularExpressions;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Models;

namespace AgentTape.Redaction.Rules;

/// <summary>
/// Regex-based redactor supporting Standard and Strict modes.
/// Standard masks common tokens, credentials, and user paths.
/// Strict adds email and non-loopback IPv4 masking.
/// </summary>
public sealed partial class RegexRedactor : IRedactor
{
    public string Redact(string input, RedactionMode mode)
    {
        if (mode == RedactionMode.Off || string.IsNullOrEmpty(input))
        {
            return input;
        }

        var output = input;

        // Standard rules
        output = GitHubTokenRegex().Replace(output, "ghp_***");
        output = GitHubFineGrainedTokenRegex().Replace(output, "github_pat_***");
        output = OpenAiKeyRegex().Replace(output, "sk-***");
        output = AwsAccessKeyRegex().Replace(output, "AKIA***");
        output = JwtRegex().Replace(output, "eyJ***.***.***");
        output = PasswordAssignmentRegex().Replace(output, "$1=***");
        output = BearerTokenRegex().Replace(output, "Bearer ***");
        output = WindowsUserPathRegex().Replace(output, @"C:\Users\<user>");
        output = UnixHomePathRegex().Replace(output, "/home/<user>");

        // Strict rules (additive)
        if (mode == RedactionMode.Strict)
        {
            output = EmailRegex().Replace(output, "<email>");
            output = NonLoopbackIPv4Regex().Replace(output, "<ip>");
        }

        return output;
    }

    // --- Standard patterns ---

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

    [GeneratedRegex(@"(?i)\b(password|pwd|secret|token)\s*=\s*\S+", RegexOptions.Compiled)]
    private static partial Regex PasswordAssignmentRegex();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.Compiled)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"C:\\Users\\([^\\\r\n]+)\\?", RegexOptions.Compiled)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"/home/([^/\r\n]+)/?", RegexOptions.Compiled)]
    private static partial Regex UnixHomePathRegex();

    // --- Strict patterns ---

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?!127\.)(\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)]
    private static partial Regex NonLoopbackIPv4Regex();
}

