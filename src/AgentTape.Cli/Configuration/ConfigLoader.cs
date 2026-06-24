using System.Text.RegularExpressions;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;

namespace AgentTape.Cli.Configuration;

/// <summary>
/// Loads AgentTape configuration from .agenttape.yml.
/// Uses a simple line-based parser to avoid YAML library dependency.
/// </summary>
public static class ConfigLoader
{
    private const string DefaultConfigPath = ".agenttape.yml";

    /// <summary>
    /// Loads configuration from the default path, or returns defaults if the file is missing.
    /// </summary>
    public static AgentTapeOptions Load()
    {
        return Load(DefaultConfigPath);
    }

    /// <summary>
    /// Loads configuration from the given path, or returns defaults if the file is missing.
    /// Throws for invalid configuration values.
    /// </summary>
    public static AgentTapeOptions Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new AgentTapeOptions();
        }

        var lines = File.ReadAllLines(configPath);
        var options = new AgentTapeOptions();

        string? currentSection = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Section header
            if (line.EndsWith(':') && !line.Contains(' '))
            {
                currentSection = line.TrimEnd(':').Trim();
                continue;
            }

            // Key: value
            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
                continue;

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();

            switch (currentSection)
            {
                case "project":
                    if (key == "name")
                        options = options with { ProjectName = value };
                    break;

                case "capture":
                    if (key == "gitDiff")
                        options = options with { CaptureGit = ParseBool(value, key) };
                    else if (key == "stdout")
                        options = options with { CaptureStdout = ParseBool(value, key) };
                    else if (key == "stderr")
                        options = options with { CaptureStderr = ParseBool(value, key) };
                    break;

                case "redaction":
                    if (key == "mode")
                    {
                        var mode = ParseRedactionMode(value);
                        options = options with { RedactionMode = mode };
                    }
                    break;

                case "riskRules":
                    // Risk rule toggles are read but stored in options for future use
                    break;

                case "tests":
                    // Test settings are read but stored in options for future use
                    break;
            }
        }

        return options;
    }

    private static bool ParseBool(string value, string key)
    {
        if (bool.TryParse(value, out var result))
            return result;

        throw new InvalidOperationException(
            $"Invalid boolean value for '{key}': '{value}'. Use true or false.");
    }

    internal static RedactionMode ParseRedactionMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "off" => RedactionMode.Off,
            "standard" => RedactionMode.Standard,
            "strict" => RedactionMode.Strict,
            _ => throw new InvalidOperationException(
                $"Invalid redaction mode: '{value}'. Use: standard, strict, or off.")
        };
    }
}
