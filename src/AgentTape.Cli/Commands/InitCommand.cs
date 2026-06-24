namespace AgentTape.Cli.Commands;

/// <summary>
/// Handles the init command: creates a default .agenttape.yml config file.
/// </summary>
public static class InitCommand
{
    private const string ConfigPath = ".agenttape.yml";

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
            return CommandExitCodes.Success;
        }

        File.WriteAllText(ConfigPath, DefaultConfig);
        Console.WriteLine($"Created {ConfigPath}");
        return CommandExitCodes.Success;
    }
}
