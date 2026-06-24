# AgentTape

[![CI](https://github.com/Wezylnia/agent-tape/actions/workflows/ci.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Wezylnia/agent-tape/actions/workflows/codeql.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://github.com/Wezylnia/agent-tape/actions/workflows/scorecard.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/scorecard.yml)
[![License](https://img.shields.io/github/license/Wezylnia/agent-tape)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

Flight recorder for AI coding agent sessions.

AgentTape records, explains, and packages AI coding agent sessions into safe, reproducible developer timelines. It captures what command ran, what changed in git, which test signals appeared, and which risk warnings matter, then generates local HTML and Markdown reports with secret redaction enabled by default.

AgentTape is intentionally deterministic and local-first. It is not an AI agent, sandbox, cloud dashboard, terminal video recorder, or code quality judge.

## Why

AI coding agents can run many commands and modify many files in a single session. Afterward, maintainers still need practical answers:

- What exactly ran?
- Which files changed?
- Did tests fail before passing?
- Did a config or secret-looking file change?
- Can this session be summarized safely in a pull request?

AgentTape turns that local session into a reviewable timeline instead of a raw terminal transcript.

## Current Status

The v1.0 CLI is functional. All core features including config loading, session management, redaction, git delta tracking, TRX parsing, environment capture, and report/export generation are implemented.

Quick start:

```bash
agenttape init
agenttape record --name demo -- dotnet test --logger trx
agenttape list
agenttape show <session-id>
agenttape report --open
agenttape export --github-pr --output pr-summary.md
```

## Quick Start From Source

Requirements:

- .NET SDK 10.0 or newer
- Git

```bash
git clone https://github.com/Wezylnia/agent-tape.git
cd agent-tape
dotnet restore AgentTape.slnx
dotnet build AgentTape.slnx
dotnet test AgentTape.slnx
```

Run the CLI from source:

```bash
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- --help
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- record -- dotnet --version
```

## What Gets Recorded

Current v1.0 capture scope:

- command text, start time, finish time, duration, and exit code
- redacted stdout and stderr
- working directory
- git branch, HEAD, status, and final diff
- changed files
- basic dotnet test output signals
- risk warnings for sensitive files, config changes, suspicious commands, and failed build/test commands

## What Never Happens By Default

- No LLM call
- No cloud upload
- No account system
- No terminal keystroke recording
- No sandbox claim
- No execution replay
- No raw secret exposure in generated reports

## Architecture

The solution is split by responsibility:

- `src/AgentTape.Cli`: command-line entry point and orchestration
- `src/AgentTape.Core`: domain models, ports, options, and shared contracts
- `src/AgentTape.Git`: git snapshot and diff capture adapters
- `src/AgentTape.Redaction`: local-only secret and path masking
- `src/AgentTape.Reporting`: Markdown and static HTML report generation
- `src/AgentTape.Testing`: deterministic test output parsers
- `src/AgentTape.Rules`: risk warning rules

Read [docs/architecture.md](docs/architecture.md) and [docs/roadmap.md](docs/roadmap.md) before implementing features.

## Roadmap

The v1.0 local flight recorder is the first stable release target. See [docs/roadmap.md](docs/roadmap.md) for follow-up directions.

## Contributing

Contributor help is welcome. Start with [CONTRIBUTING.md](CONTRIBUTING.md), then look for issues labeled `good first issue`, `contributor-ready`, or `help wanted`.

## Security

AgentTape handles terminal output, diffs, and file paths, so redaction and local-first behavior are core design constraints. Please report suspected vulnerabilities through the process in [SECURITY.md](SECURITY.md).

## License

AgentTape is licensed under the [Apache License 2.0](LICENSE).
