namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the init command: creates a default .agenttape.yml config file and protects .agenttape/ via .gitignore.
/// </summary>
public static class InitCommand
{
    private const string ConfigPath = ".agenttape.yml";
    private const string GitIgnoreMarker = ".agenttape/";

    private const string DefaultConfig = """
project:
  name: agenttape-project
capture:
  stdout: true
  stderr: true
  gitDiff: true
  environment: minimal
redaction:
  mode: standard
  maskUserPaths: true
  maskEmails: false
riskRules:
  warnOnConfigChanges: true
  warnOnSecretFiles: true
  warnOnLargeDeletes: true
tests:
  detectDotnet: true
  trx: false
""";

    public static int Execute(string? workingDirectory = null)
    {
        var root = workingDirectory ?? Directory.GetCurrentDirectory();
        var configPath = Path.Combine(root, ConfigPath);

        if (File.Exists(configPath))
        {
            Console.WriteLine($"{ConfigPath} already exists. Not overwriting.");
        }
        else
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(configPath, DefaultConfig);
            Console.WriteLine($"Created {ConfigPath}");
        }

        // Ensure .agenttape/ is in .gitignore
        ProtectAgentTapeDirectory(root);

        return CommandExitCodes.Success;
    }

    private static void ProtectAgentTapeDirectory(string workingDirectory)
    {
        const string gitignoreHeader = "# AgentTape local session reports";
        const string gitignoreLine = ".agenttape/";
        var gitignorePath = Path.Combine(workingDirectory, ".gitignore");

        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, $"{gitignoreHeader}\n{gitignoreLine}\n");
            Console.WriteLine("Created .gitignore with .agenttape/ entry.");
            return;
        }

        var lines = File.ReadAllLines(gitignorePath);
        if (lines.Any(l => l.Trim() == gitignoreLine))
        {
            // Already ignored
            return;
        }

        // Append to existing .gitignore
        File.AppendAllText(gitignorePath, $"\n{gitignoreHeader}\n{gitignoreLine}\n");
        Console.WriteLine("Added .agenttape/ to .gitignore.");
    }
}
