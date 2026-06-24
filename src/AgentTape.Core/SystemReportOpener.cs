using System.Diagnostics;
using AgentTape.Core.Abstractions;

namespace AgentTape.Core;

/// <summary>
/// Opens reports using the OS default handler.
/// </summary>
public sealed class SystemReportOpener : IReportOpener
{
    public Task OpenAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            Console.Error.WriteLine($"Could not open report. Path: {Path.GetFullPath(path)}");
        }

        return Task.CompletedTask;
    }
}
