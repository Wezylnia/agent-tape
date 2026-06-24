# Maintainer Brief

This repository contains the v1.0 AgentTape implementation. Do not invent a new architecture for follow-up work. Continue from the existing module boundaries and make small, verifiable changes.

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

## Current Maintenance Target

Preserve the v1.0 CLI and package behavior while improving reliability, report quality, and parser coverage:

- keep global tool packaging working,
- keep redaction enabled by default,
- preserve wrapped command arguments after `--`,
- add tests before changing CLI parsing, storage, redaction, reporting, or git behavior,
- keep generated report formats backward-compatible unless a release note calls out the change.

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
