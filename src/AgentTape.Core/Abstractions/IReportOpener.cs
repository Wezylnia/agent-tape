namespace AgentTape.Core.Abstractions;

/// <summary>
/// Opens reports in the system default application.
/// </summary>
public interface IReportOpener
{
    Task OpenAsync(string path, CancellationToken cancellationToken);
}
