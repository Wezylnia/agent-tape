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

This repository is at the v0.1 foundation stage. The architecture, project skeleton, domain contracts, initial CLI flow, reporting skeleton, redaction skeleton, and implementation plan are in place.

Target v0.1 workflow:

```bash
agenttape record -- dotnet test
agenttape report
agenttape export --format markdown
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

Run the current CLI skeleton:

```bash
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- --help
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- record -- dotnet --version
```

## What Gets Recorded

Planned v0.1 capture scope:

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

| Version | Focus |
| --- | --- |
| `v0.1` | Local flight recorder: command capture, git diff, redaction, HTML/Markdown reports |
| `v0.2` | Test-aware reports: TRX, xUnit/NUnit XML, before/after comparison |
| `v0.3` | Agent-aware workflows: Codex, Claude, Aider profiles and opt-in prompt capture |
| `v1.0` | Stable shareable AI coding session report format |

See [docs/roadmap.md](docs/roadmap.md) for the detailed plan.

## Contributing

Contributor help is welcome once v0.1 issues are opened. Start with [CONTRIBUTING.md](CONTRIBUTING.md), then look for issues labeled `good first issue`, `contributor-ready`, or `help wanted`.

## Security

AgentTape handles terminal output, diffs, and file paths, so redaction and local-first behavior are core design constraints. Please report suspected vulnerabilities through the process in [SECURITY.md](SECURITY.md).

## License

AgentTape is licensed under the [Apache License 2.0](LICENSE).
