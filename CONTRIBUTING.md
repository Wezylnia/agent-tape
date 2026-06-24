# Contributing

Thanks for considering a contribution to AgentTape.

AgentTape is intentionally narrow: it records, redacts, summarizes, and packages local developer sessions. Avoid changes that turn it into an AI agent, sandbox, cloud service, or full terminal emulator.

## Development Setup

Requirements:

- .NET SDK 10.0 or newer
- Git

```bash
dotnet restore AgentTape.slnx
dotnet build AgentTape.slnx
dotnet test AgentTape.slnx
```

## Before You Start

Read these files first:

- `docs/architecture.md`
- `docs/next-model-brief.md`
- `docs/roadmap.md`

## Pull Request Expectations

- Keep changes scoped to one feature or fix.
- Add or update tests for behavior changes.
- Keep `AgentTape.Core` free of Git, reporting, redaction, and CLI implementation details.
- Do not add network calls, LLM calls, replay execution, or cloud upload behavior without a dedicated design issue.
- Redact secrets in test fixtures, snapshots, reports, and logs.

## Validation

Run:

```bash
dotnet build AgentTape.slnx --no-restore
dotnet test AgentTape.slnx --no-build
```

For CLI changes, also run:

```bash
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- --help
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- record -- dotnet --version
```
