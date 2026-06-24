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

    public static int Execute()
    {
        if (File.Exists(ConfigPath))
        {
            Console.WriteLine($"{ConfigPath} already exists. Not overwriting.");
        }
        else
        {
            File.WriteAllText(ConfigPath, DefaultConfig);
            Console.WriteLine($"Created {ConfigPath}");
        }

        // Ensure .agenttape/ is in .gitignore
        ProtectAgentTapeDirectory();

        return CommandExitCodes.Success;
    }

    private static void ProtectAgentTapeDirectory()
    {
        const string gitignoreHeader = "# AgentTape local session reports";
        const string gitignoreLine = ".agenttape/";

        if (!File.Exists(".gitignore"))
        {
            File.WriteAllText(".gitignore", $"{gitignoreHeader}\n{gitignoreLine}\n");
            Console.WriteLine("Created .gitignore with .agenttape/ entry.");
            return;
        }

        var lines = File.ReadAllLines(".gitignore");
        if (lines.Any(l => l.Trim() == gitignoreLine))
        {
            // Already ignored
            return;
        }

        // Append to existing .gitignore
        File.AppendAllText(".gitignore", $"\n{gitignoreHeader}\n{gitignoreLine}\n");
        Console.WriteLine("Added .agenttape/ to .gitignore.");
    }
}
