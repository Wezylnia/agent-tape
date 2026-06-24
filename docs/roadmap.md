# AgentTape Roadmap

AgentTape should grow as a small, reliable developer tool. Each release must keep the product local-first, deterministic, and useful without an LLM.

## v0.1 - Local Flight Recorder

Goal: record a local command session, capture git evidence, redact sensitive output, and generate HTML and Markdown reports.

Scope:

- `agenttape init`
- `agenttape record -- <command>`
- stdout and stderr capture
- exit code and duration capture
- git status before and after
- final diff capture
- standard secret redaction by default
- basic risk warnings
- static HTML report
- Markdown pull request summary
- .NET global tool packaging

Release criteria:

- builds on Windows and Linux,
- tests pass on Windows and Linux,
- global tool packaging works,
- README quick start works from a clean clone,
- sample session is documented,
- generated reports do not expose known test secrets.

## v0.2 - Test-Aware Reports

Goal: make test outcomes first-class evidence in the session timeline.

Scope:

- TRX parser,
- xUnit XML parser,
- NUnit XML parser,
- before/after test comparison,
- failed and fixed test lists,
- improved command classification,
- GitHub Actions Markdown summary.

Release criteria:

- parsers are covered by positive, negative, and malformed input tests,
- no parser throws on unknown or partial input,
- reports show test evidence without raw noisy logs,
- imported test files never bypass redaction.

## v0.3 - Agent-Aware Workflows

Goal: improve the experience for common AI coding agents while preserving local-first defaults.

Scope:

- Codex profile,
- Claude profile,
- Aider profile,
- session goal and label handling,
- opt-in prompt capture,
- optional LLM explanation command.

Release criteria:

- prompt capture is off by default,
- each agent profile is isolated behind explicit detection or configuration,
- optional LLM features never run during normal `record` or `report`,
- reports remain useful when no profile is detected.

## v1.0 - Shareable AI Coding Session Reports

Goal: provide a stable CLI and session report format that can be used in real pull request and open-source review workflows.

Scope:

- stable command surface,
- polished HTML timeline,
- polished Markdown export,
- stable pack format,
- dry-run replay,
- strong redaction,
- stable config file,
- example repositories,
- demo media,
- complete public documentation.

Release criteria:

- documented backwards compatibility policy,
- no known raw secret exposure paths in default flows,
- public examples are reproducible,
- CI, CodeQL, and security workflows are passing,
- package install and uninstall instructions are tested.

## Positioning

Use this product sentence:

> AgentTape records, explains, and packages AI coding agent sessions into safe, reproducible developer timelines.

AgentTape is not a code-writing tool. It makes code-writing tools easier to audit.
