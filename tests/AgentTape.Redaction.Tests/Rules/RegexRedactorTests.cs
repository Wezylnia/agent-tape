using AgentTape.Core.Models;
using AgentTape.Redaction.Rules;

namespace AgentTape.Redaction.Tests.Rules;

public sealed class RegexRedactorTests
{
    private readonly RegexRedactor _redactor = new();

    // --- Standard mode tests ---

    [Fact]
    public void Standard_masks_github_classic_token()
    {
        var output = _redactor.Redact("export GITHUB_TOKEN=ghp_abc123def456ghijklmnopqrstuv", RedactionMode.Standard);
        Assert.DoesNotContain("ghp_abc123", output);
        Assert.Contains("ghp_***", output);
    }

    [Fact]
    public void Standard_masks_github_fine_grained_token()
    {
        var output = _redactor.Redact("github_pat_11ABCDEFGHIJKLMNOPQRSTUVWXYZ1234", RedactionMode.Standard);
        Assert.DoesNotContain("github_pat_11", output);
        Assert.Contains("github_pat_***", output);
    }

    [Fact]
    public void Standard_masks_openai_key()
    {
        var output = _redactor.Redact("OPENAI_API_KEY=sk-proj-abcdefghijklmnopqrstuvwxyz", RedactionMode.Standard);
        Assert.DoesNotContain("sk-proj", output);
        Assert.Contains("sk-***", output);
    }

    [Fact]
    public void Standard_masks_aws_access_key()
    {
        var output = _redactor.Redact("AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE", RedactionMode.Standard);
        Assert.DoesNotContain("AKIAIOS", output);
        Assert.Contains("AKIA***", output);
    }

    [Fact]
    public void Standard_masks_jwt()
    {
        var output = _redactor.Redact("Authorization: eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U", RedactionMode.Standard);
        Assert.DoesNotContain("eyJhbGci", output);
        Assert.Contains("eyJ***", output);
    }

    [Fact]
    public void Standard_masks_password_assignment()
    {
        var output = _redactor.Redact("Password=MySecret123!", RedactionMode.Standard);
        Assert.DoesNotContain("MySecret123", output);
        Assert.Contains("Password=***", output);
    }

    [Fact]
    public void Standard_masks_token_assignment()
    {
        var output = _redactor.Redact("token=abcdef123456", RedactionMode.Standard);
        Assert.DoesNotContain("abcdef", output);
        Assert.Contains("token=***", output);
    }

    [Fact]
    public void Standard_masks_bearer_token()
    {
        var output = _redactor.Redact("Authorization: Bearer abcdefghijklmnop1234567890", RedactionMode.Standard);
        Assert.DoesNotContain("abcdefghijklmnop", output);
        Assert.Contains("Bearer ***", output);
    }

    [Fact]
    public void Standard_masks_windows_user_path()
    {
        var output = _redactor.Redact(@"C:\Users\Alice\Documents\secret.txt", RedactionMode.Standard);
        Assert.DoesNotContain("Alice", output);
        Assert.Contains(@"C:\Users\<user>", output);
    }

    [Fact]
    public void Standard_masks_unix_home_path()
    {
        var output = _redactor.Redact("/home/alice/.ssh/id_rsa", RedactionMode.Standard);
        Assert.DoesNotContain("alice", output);
        Assert.Contains("/home/<user>", output);
    }

    [Fact]
    public void Standard_does_not_mask_normal_text()
    {
        var input = "The build completed successfully in 12.4 seconds.";
        var output = _redactor.Redact(input, RedactionMode.Standard);
        Assert.Equal(input, output);
    }

    // --- Strict mode tests ---

    [Fact]
    public void Strict_masks_email()
    {
        var output = _redactor.Redact("Contact: alice@example.com", RedactionMode.Strict);
        Assert.DoesNotContain("alice@example.com", output);
        Assert.Contains("<email>", output);
    }

    [Fact]
    public void Strict_masks_public_ipv4()
    {
        var output = _redactor.Redact("Server: 192.168.1.100", RedactionMode.Strict);
        Assert.DoesNotContain("192.168.1.100", output);
        Assert.Contains("<ip>", output);
    }

    [Fact]
    public void Strict_does_not_mask_loopback_ipv4()
    {
        var output = _redactor.Redact("Listening on 127.0.0.1:5000", RedactionMode.Strict);
        Assert.Contains("127.0.0.1", output);
        Assert.DoesNotContain("<ip>", output);
    }

    [Fact]
    public void Strict_includes_standard_rules()
    {
        var output = _redactor.Redact("token=secret123 alice@example.com", RedactionMode.Strict);
        Assert.DoesNotContain("secret123", output);
        Assert.DoesNotContain("alice@example.com", output);
    }

    // --- Off mode ---

    [Fact]
    public void Off_returns_input_unchanged()
    {
        var input = "ghp_abc123def456ghijklmnopqrstuv secret=value";
        var output = _redactor.Redact(input, RedactionMode.Off);
        Assert.Equal(input, output);
    }

    // --- Safety tests ---

    [Fact]
    public void Redact_handles_empty_string()
    {
        var output = _redactor.Redact("", RedactionMode.Standard);
        Assert.Equal("", output);
    }

    [Fact]
    public void Redact_handles_large_input()
    {
        var largeInput = new string('x', 100_000) + " token=secret123 ";
        var output = _redactor.Redact(largeInput, RedactionMode.Standard);
        Assert.DoesNotContain("secret123", output);
    }

    [Fact]
    public void Redact_is_idempotent()
    {
        var input = "ghp_abc123def456ghijklmnopqrstuv token=secret123";
        var first = _redactor.Redact(input, RedactionMode.Standard);
        var second = _redactor.Redact(first, RedactionMode.Standard);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Redact_does_not_include_original_secret_in_replacement()
    {
        var secret = "ghp_abc123def456ghijklmnopqrstuv";
        var output = _redactor.Redact(secret, RedactionMode.Standard);
        Assert.DoesNotContain("abc123", output);
    }
}

