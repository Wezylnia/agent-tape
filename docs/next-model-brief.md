# Next Model Brief

This repository is an implementation skeleton. Do not invent a new architecture. Continue from the existing module boundaries and finish v0.1 in small, verifiable steps.

## Read First

1. `README.md`
2. `docs/architecture.md`
3. `docs/roadmap.md`

Follow-up implementation should stay within the public architecture and roadmap unless a maintainer provides more specific private planning notes.

## Current Technical Decisions

- Target framework: `net10.0`
- SDK pin: `global.json`
- Solution file: `AgentTape.slnx`
- Core is the domain and port layer
- Git adapter uses the `git` CLI
- Redaction is regex-based and local-only
- Reporting is static Markdown and HTML
- Test parsing starts with dotnet text output

## First Implementation Target

Finish the v0.1 session storage flow before adding new feature areas:

- `FileSystemSessionStore`
- `SessionIdFactory`
- strict session layout tests
- CLI integration that writes through the store instead of direct ad hoc file writes

Required checks:

```bash
dotnet build AgentTape.slnx
dotnet test AgentTape.slnx
dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- --help
```

## Guardrails

- Do not put HTML, git, redaction, test parsing, or rule logic into Core.
- Do not disable redaction by default.
- Do not add LLM calls.
- Do not add cloud upload.
- Do not add real replay execution.
- Do not start a web dashboard.
- Do not commit files under `private-docs/`.
