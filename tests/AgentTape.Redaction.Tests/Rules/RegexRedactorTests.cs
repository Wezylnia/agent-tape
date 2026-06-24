using AgentTape.Core.Models;
using AgentTape.Redaction.Rules;

namespace AgentTape.Redaction.Tests.Rules;

public sealed class RegexRedactorTests
{
    [Fact]
    public void Redact_masks_common_secret_shapes()
    {
        var redactor = new RegexRedactor();

        var output = redactor.Redact("token=abc123 sk-abcdefghijklmnopqrstuvwxyz", RedactionMode.Standard);

        Assert.DoesNotContain("abc123", output);
        Assert.DoesNotContain("sk-abcdefghijklmnopqrstuvwxyz", output);
        Assert.Contains("REDACTED", output);
    }
}
