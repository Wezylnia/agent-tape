# AgentTape

[![CI](https://github.com/Wezylnia/agent-tape/actions/workflows/ci.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Wezylnia/agent-tape/actions/workflows/codeql.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://github.com/Wezylnia/agent-tape/actions/workflows/scorecard.yml/badge.svg)](https://github.com/Wezylnia/agent-tape/actions/workflows/scorecard.yml)
[![Release](https://img.shields.io/github/v/release/Wezylnia/agent-tape)](https://github.com/Wezylnia/agent-tape/releases)
[![License](https://img.shields.io/github/license/Wezylnia/agent-tape)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

AgentTape is a local flight recorder for AI coding sessions.

It records the command you ran, the git changes it produced, test signals, risk warnings, and redacted output, then turns that evidence into reviewable HTML, Markdown, JSON, or pull request summaries.

## Why Use It

AI coding agents can change a repository quickly. AgentTape gives maintainers a compact answer to:

- What command ran?
- What files changed?
- Did tests pass or fail?
- Did anything risky or secret-looking appear?
- Can this session be summarized safely for review?

## Quick Start

Requirements:

- .NET SDK 10.0 or newer
- Git

Run from source:

```bash
git clone https://github.com/Wezylnia/agent-tape.git
cd agent-tape
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- init
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- record --name test-run -- dotnet test
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- report --open
```

Install as a local .NET tool from the release package you build:

```bash
dotnet pack src/AgentTape.Cli/AgentTape.Cli.csproj -c Release
dotnet tool install --global AgentTape --add-source src/AgentTape.Cli/bin/Release --version 1.0.0
agenttape --help
```

## Common Commands

```bash
agenttape init
agenttape record --name demo -- dotnet test --logger trx
agenttape record --name shell-demo --shell "dotnet test && git status"
agenttape list
agenttape show <session-id>
agenttape report --open
agenttape export --github-pr --output pr-summary.md
agenttape export --format json --output session.json
```

## What It Captures

| Evidence | Details |
| --- | --- |
| Command | command text, timing, duration, exit code |
| Output | redacted stdout and stderr previews |
| Git | branch, HEAD, status, changed files, line counts, final diff |
| Tests | .NET test output and TRX signals |
| Risk | deterministic warnings for sensitive files, config changes, suspicious commands, failed builds/tests |
| Reports | local HTML, Markdown, JSON, and GitHub PR summary exports |

Reports are written under `.agenttape/` and are ignored by git after `agenttape init`.

## Safety Model

AgentTape is local-first and deterministic:

- no LLM calls
- no cloud upload
- no account system
- no terminal keystroke recording
- no sandbox or malware-scanner claim
- redaction enabled by default

It is not an AI agent. It makes AI-assisted coding work easier to audit.

## Project Layout

```text
src/
  AgentTape.Cli        CLI entry point and command orchestration
  AgentTape.Core       domain models, ports, storage, process runner
  AgentTape.Git        git snapshot and diff capture
  AgentTape.Redaction  local secret and path masking
  AgentTape.Reporting  HTML and Markdown report generation
  AgentTape.Testing    deterministic test output parsers
  AgentTape.Rules      risk warning rules
```

For implementation details, read [docs/architecture.md](docs/architecture.md) and [docs/roadmap.md](docs/roadmap.md).

## Development

```bash
dotnet restore AgentTape.slnx
dotnet build AgentTape.slnx
dotnet test AgentTape.slnx
dotnet pack src/AgentTape.Cli/AgentTape.Cli.csproj -c Release
```

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md). Good first tasks should be small, tested, and keep the tool local-first.

## Security

AgentTape handles command output, diffs, paths, and test logs. Treat generated session data as sensitive and report vulnerabilities through [SECURITY.md](SECURITY.md).

## License

AgentTape is licensed under the [Apache License 2.0](LICENSE).
