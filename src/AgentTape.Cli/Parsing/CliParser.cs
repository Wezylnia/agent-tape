namespace AgentTape.Cli.Parsing;

/// <summary>
/// Simple, deterministic CLI argument parser for AgentTape.
/// Does not use System.CommandLine. Keeps parsing explicit and testable.
/// </summary>
public static class CliParser
{
    private static readonly HashSet<string> ValidRedactValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "standard", "strict", "off"
    };

    private static readonly HashSet<string> ValidExportFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "markdown", "json"
    };

    /// <summary>
    /// Parses the command-line arguments into a CliParseResult.
    /// </summary>
    public static CliParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "No command specified. Use: init, record, report, or export."
            };
        }

        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "init" => ParseInit(args),
            "record" => ParseRecord(args),
            "report" => ParseReport(args),
            "export" => ParseExport(args),
            _ => new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unknown command: {args[0]}. Use: init, record, report, or export."
            }
        };
    }

    private static CliParseResult ParseInit(string[] args)
    {
        if (args.Length > 1)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "init does not accept options."
            };
        }

        return new CliParseResult { IsSuccess = true, Command = "init" };
    }

    private static CliParseResult ParseRecord(string[] args)
    {
        var separatorIndex = Array.IndexOf(args, "--");
        if (separatorIndex < 1)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "Usage: agenttape record [--name <name>] [--redact standard|strict|off] [--no-git] -- <command> [args]"
            };
        }

        var optionArgs = args[1..separatorIndex];
        var commandArgs = args[(separatorIndex + 1)..];

        if (commandArgs.Length == 0)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "No command to record. Usage: agenttape record -- <command> [args]"
            };
        }

        var result = new CliParseResult
        {
            IsSuccess = true,
            Command = "record",
            Redact = "standard",
            WrappedExecutable = commandArgs[0],
            WrappedArguments = commandArgs.Skip(1).ToArray()
        };

        for (var i = 0; i < optionArgs.Length; i++)
        {
            switch (optionArgs[i].ToLowerInvariant())
            {
                case "--name":
                    if (i + 1 >= optionArgs.Length || optionArgs[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--name requires a value."
                        };
                    }

                    result = result with { Name = optionArgs[++i] };
                    break;

                case "--redact":
                    if (i + 1 >= optionArgs.Length || optionArgs[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--redact requires a value (standard, strict, or off)."
                        };
                    }

                    var redactValue = optionArgs[++i];
                    if (!ValidRedactValues.Contains(redactValue))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Invalid redaction mode: {redactValue}. Use: standard, strict, or off."
                        };
                    }

                    result = result with { Redact = redactValue.ToLowerInvariant() };
                    break;

                case "--no-git":
                    result = result with { NoGit = true };
                    break;

                default:
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Unknown option: {optionArgs[i]}"
                    };
            }
        }

        return result;
    }

    private static CliParseResult ParseReport(string[] args)
    {
        var result = new CliParseResult { IsSuccess = true, Command = "report" };

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--html":
                    result = result with { Html = true };
                    break;
                case "--markdown":
                    result = result with { Markdown = true };
                    break;
                case "--open":
                    result = result with { Open = true };
                    break;
                default:
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Unknown option: {args[i]}"
                    };
            }
        }

        return result;
    }

    private static CliParseResult ParseExport(string[] args)
    {
        if (args.Length < 2 || args[1].ToLowerInvariant() != "--format")
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "export requires --format markdown|json"
            };
        }

        if (args.Length < 3)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "--format requires a value (markdown or json)."
            };
        }

        var format = args[2].ToLowerInvariant();
        if (!ValidExportFormats.Contains(format))
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = $"Invalid export format: {format}. Use: markdown or json."
            };
        }

        // Check for unknown trailing args
        if (args.Length > 3)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unknown option: {args[3]}"
            };
        }

        return new CliParseResult { IsSuccess = true, Command = "export", Format = format };
    }
}
