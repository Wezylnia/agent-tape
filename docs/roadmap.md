# AgentTape Roadmap

AgentTape should grow as a small, reliable developer tool. Each release must keep the product local-first, deterministic, and useful without an LLM.

## v1.0 - Local Flight Recorder

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

## v1.1 - Report Polish and Compatibility

Goal: improve report readability, compatibility, and package ergonomics without changing the core session format.

Scope:

- richer HTML timeline styling,
- stronger Markdown escaping,
- more detailed GitHub PR summaries,
- improved config documentation,
- compatibility tests for saved `session.json` files.

Release criteria:

- existing v1.0 sessions still load,
- reports remain static and local-only,
- no new raw secret exposure paths,
- CI and package install tests pass.

## v1.2 - Agent-Aware Workflows

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

## v2.0 - Stable Shareable Session Standard

Goal: provide a documented, versioned session format that can be shared across teams and tooling.

Scope:

- versioned session schema,
- explicit compatibility policy,
- stable pack format,
- dry-run replay,
- stronger import/export validation,
- signed release artifacts.

Release criteria:

- documented schema compatibility policy,
- no known raw secret exposure paths in default flows,
- public examples are reproducible,
- CI, CodeQL, and security workflows are passing,
- package install and uninstall instructions are tested.

## Positioning

Use this product sentence:

> AgentTape records, explains, and packages AI coding agent sessions into safe, reproducible developer timelines.

AgentTape is not a code-writing tool. It makes code-writing tools easier to audit.
