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
        return RedactWithSummary(input, mode).Text;
    }

    public RedactionResult RedactWithSummary(string input, RedactionMode mode)
    {
        if (mode == RedactionMode.Off || string.IsNullOrEmpty(input))
        {
            return new RedactionResult { Text = input ?? string.Empty };
        }

        var output = input;
        var summaries = new List<RedactionMatchSummary>();

        // Standard rules
        (output, var count) = ApplyRule(GitHubTokenRegex(), output, "ghp_***", "GitHub Classic Token");
        AddSummary(summaries, "GitHub Classic Token", count);

        (output, count) = ApplyRule(GitHubFineGrainedTokenRegex(), output, "github_pat_***", "GitHub Fine-Grained Token");
        AddSummary(summaries, "GitHub Fine-Grained Token", count);

        (output, count) = ApplyRule(OpenAiKeyRegex(), output, "sk-***", "OpenAI API Key");
        AddSummary(summaries, "OpenAI API Key", count);

        (output, count) = ApplyRule(AwsAccessKeyRegex(), output, "AKIA***", "AWS Access Key");
        AddSummary(summaries, "AWS Access Key", count);

        (output, count) = ApplyRule(JwtRegex(), output, "eyJ***.***.***", "JWT Token");
        AddSummary(summaries, "JWT Token", count);

        (output, count) = ApplyRule(PasswordAssignmentRegex(), output, "$1=***", "Password/Secret Assignment");
        AddSummary(summaries, "Password/Secret Assignment", count);

        (output, count) = ApplyRule(BearerTokenRegex(), output, "Bearer ***", "Bearer Token");
        AddSummary(summaries, "Bearer Token", count);

        (output, count) = ApplyRule(WindowsUserPathRegex(), output, @"C:\Users\<user>", "Windows User Path");
        AddSummary(summaries, "Windows User Path", count);

        (output, count) = ApplyRule(UnixHomePathRegex(), output, "/home/<user>", "Unix Home Path");
        AddSummary(summaries, "Unix Home Path", count);

        // Strict rules (additive)
        if (mode == RedactionMode.Strict)
        {
            (output, count) = ApplyRule(EmailRegex(), output, "<email>", "Email Address");
            AddSummary(summaries, "Email Address", count);

            (output, count) = ApplyRule(NonLoopbackIPv4Regex(), output, "<ip>", "Non-Loopback IPv4");
            AddSummary(summaries, "Non-Loopback IPv4", count);
        }

        var totalCount = summaries.Sum(s => s.Count);
        return new RedactionResult
        {
            Text = output,
            MatchCount = totalCount,
            Summaries = summaries
        };
    }

    private static (string Output, int Count) ApplyRule(Regex regex, string input, string replacement, string _)
    {
        var matches = regex.Matches(input);
        var count = matches.Count;
        var output = count > 0 ? regex.Replace(input, replacement) : input;
        return (output, count);
    }

    private static void AddSummary(List<RedactionMatchSummary> summaries, string ruleName, int count)
    {
        if (count > 0)
        {
            summaries.Add(new RedactionMatchSummary { RuleName = ruleName, Count = count });
        }
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

