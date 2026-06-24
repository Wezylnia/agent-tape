using AgentTape.Core.Models;

namespace AgentTape.Core.Abstractions;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken);
}
