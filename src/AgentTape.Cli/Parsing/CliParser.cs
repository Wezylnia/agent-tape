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

        string? configPath = null;
        var remainingArray = args;
        if (args[0].Equals("--config", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 3 || args[1].StartsWith("--"))
            {
                return new CliParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "--config requires a value."
                };
            }

            configPath = args[1];
            remainingArray = args[2..];
        }

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

        return commandResult with { ConfigPath = commandResult.ConfigPath ?? configPath };
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
        var shellIndex = Array.IndexOf(args, "--shell");
        var hasShell = shellIndex >= 0;

        if (!hasShell && separatorIndex < 1)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "Usage: agenttape record [--name <name>] [--redact standard|strict|off] [--no-git] [--shell <command>] -- <command> [args]"
            };
        }

        if (hasShell && separatorIndex >= 0)
        {
            return new CliParseResult
            {
                IsSuccess = false,
                ErrorMessage = "--shell cannot be combined with -- <command>."
            };
        }

        // Handle --shell mode
        if (hasShell)
        {
            if (shellIndex + 1 >= args.Length)
            {
                return new CliParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "--shell requires a command string."
                };
            }

            var shellCommand = args[shellIndex + 1];

            string executable;
            string[] shellArgs;

            if (OperatingSystem.IsWindows())
            {
                executable = "cmd";
                shellArgs = ["/c", shellCommand];
            }
            else
            {
                executable = "/bin/sh";
                shellArgs = ["-lc", shellCommand];
            }

            var shellResult = new CliParseResult
            {
                IsSuccess = true,
                Command = "record",
                Shell = shellCommand,
                WrappedExecutable = executable,
                WrappedArguments = shellArgs
            };

            // Parse options before --shell
            for (var i = 1; i < shellIndex; i++)
            {
                var optResult = ParseRecordOption(shellResult, args, ref i);
                if (!optResult.IsSuccess) return optResult;
                shellResult = optResult;
            }

            return shellResult;
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
            var optResult = ParseRecordOption(result, optionArgs, ref i);
            if (!optResult.IsSuccess) return optResult;
            result = optResult;
        }

        return result;
    }

    private static CliParseResult ParseRecordOption(CliParseResult result, string[] args, ref int i)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--name":
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                {
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "--name requires a value."
                    };
                }

                result = result with { Name = args[++i] };
                break;

            case "--redact":
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                {
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "--redact requires a value (standard, strict, or off)."
                    };
                }

                var redactValue = args[++i];
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

            case "--config":
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                {
                    return new CliParseResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "--config requires a value."
                    };
                }

                result = result with { ConfigPath = args[++i] };
                break;

            default:
                return new CliParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unknown option: {args[i]}"
                };
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
