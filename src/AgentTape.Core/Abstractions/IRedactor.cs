using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface IRedactor
{
    string Redact(string input, RedactionMode mode);
}
