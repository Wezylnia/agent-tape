using System.Diagnostics;
using System.Runtime.InteropServices;
using AgentTape.Core.Models;

namespace AgentTape.Core;

/// <summary>
/// Captures the local development environment snapshot for session records.
/// </summary>
public static class EnvironmentSnapshotCapture
{
    public static async Task<EnvironmentSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        var snapshot = new EnvironmentSnapshot
        {
            OperatingSystem = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
            Shell = DetectShell(),
            DotNetVersion = await TryGetVersionAsync("dotnet", ["--version"], cancellationToken),
            Tools = new Dictionary<string, string>
            {
                ["git"] = await TryGetVersionAsync("git", ["--version"], cancellationToken)
            }
        };

        return snapshot;
    }

    public static string AgentTapeVersion => "1.0.0";

    private static string DetectShell()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Check for PowerShell
            var psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
            if (!string.IsNullOrEmpty(psModulePath))
                return "PowerShell";

            var comspec = Environment.GetEnvironmentVariable("ComSpec");
            if (!string.IsNullOrEmpty(comspec))
                return "cmd";
        }
        else
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell))
                return Path.GetFileName(shell);
        }

        return "unknown";
    }

    private static async Task<string> TryGetVersionAsync(string executable, string[] arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
                return "unknown";

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return output.Trim();
        }
        catch
        {
            return "unknown";
        }
    }
}
