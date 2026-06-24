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
        "markdown", "json", "html"
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

        // Extract global --config option before command dispatch
        string? configPath = null;
        var remainingArgs = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--config", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                {
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "--config requires a value."
                    };
                }
                configPath = args[++i];
            }
            else
            {
                remainingArgs.Add(args[i]);
            }
        }

        var remainingArray = remainingArgs.ToArray();
        if (remainingArray.Length == 0)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "No command specified. Use: init, record, report, or export."
            };
        }

        var command = remainingArray[0].ToLowerInvariant();

        var commandResult = command switch
        {
            "init" => ParseInit(remainingArray),
            "record" => ParseRecord(remainingArray),
            "report" => ParseReport(remainingArray),
            "export" => ParseExport(remainingArray),
            "list" => new CliParseResult { IsSuccess = true, Command = "list" },
            "show" => ParseShow(remainingArray),
            _ => new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unknown command: {remainingArray[0]}. Use: init, record, report, or export."
            }
        };

        return commandResult with { ConfigPath = configPath };
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
                case "--session":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--session requires a value."
                        };
                    }
                    result = result with { SessionId = args[++i] };
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
        var result = new CliParseResult { IsSuccess = true, Command = "export" };
        var formatSet = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--format":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--format requires a value (markdown, json, or html)."
                        };
                    }

                    var format = args[++i].ToLowerInvariant();
                    if (!ValidExportFormats.Contains(format))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Invalid export format: {format}. Use: markdown, json, or html."
                        };
                    }

                    result = result with { Format = format };
                    formatSet = true;
                    break;

                case "--github-pr":
                    result = result with { GitHubPr = true, Format = "github-pr" };
                    formatSet = true;
                    break;

                case "--output":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--output requires a value."
                        };
                    }
                    result = result with { Output = args[++i] };
                    break;

                case "--session":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        return new CliParseResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "--session requires a value."
                        };
                    }
                    result = result with { SessionId = args[++i] };
                    break;

                default:
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Unknown option: {args[i]}"
                    };
            }
        }

        if (!formatSet)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "export requires --format markdown|json|html or --github-pr"
            };
        }

        return result;
    }

    private static CliParseResult ParseShow(string[] args)
    {
        if (args.Length < 2)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "Usage: agenttape show <session-id>"
            };
        }

        if (args.Length > 2)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unknown option: {args[2]}"
            };
        }

        return new CliParseResult { IsSuccess = true, Command = "show", SessionId = args[1] };
    }
}
