# AgentTape Architecture

This document defines the public architecture for AgentTape. It is intentionally high-level and stable. The detailed task-by-task implementation plan is private project planning material and is kept outside the public documentation.

## Product Boundary

AgentTape is a local-first flight recorder for AI coding agent and terminal-based development sessions.

AgentTape does:

- record command execution metadata,
- capture redacted stdout and stderr,
- capture git status and final diffs,
- extract deterministic test signals,
- produce risk warnings from explicit rules,
- generate local HTML and Markdown reports,
- package session evidence in a reviewable format.

AgentTape does not:

- write code as an AI agent,
- make autonomous repair decisions,
- call an LLM by default,
- upload sessions to a cloud service,
- claim to sandbox commands,
- record every terminal keystroke,
- replay commands by default,
- act as a full SAST, malware scanner, or compliance tool.

The core value is a trustworthy developer timeline, not a terminal video.

## Solution Layout

```text
src/
  AgentTape.Cli/
  AgentTape.Core/
  AgentTape.Git/
  AgentTape.Redaction/
  AgentTape.Reporting/
  AgentTape.Testing/
  AgentTape.Rules/

tests/
  AgentTape.Core.Tests/
  AgentTape.Redaction.Tests/
  AgentTape.Reporting.Tests/
```

## Module Responsibilities

### AgentTape.Cli

The CLI is the entry point and orchestration layer.

Responsibilities:

- parse command-line arguments,
- dispatch commands such as `init`, `record`, `report`, `export`, and later `pack`,
- compose concrete implementations from the other modules,
- control process exit codes,
- print short human-readable summaries.

The CLI must not contain:

- secret detection regexes,
- git porcelain parsing,
- HTML templates,
- test output parsing rules,
- risk rule logic.

### AgentTape.Core

Core contains domain models, options, and abstraction contracts. It is the dependency root for the rest of the solution.

Core contains:

- `TapeSession`
- `CommandRun`
- `CommandRequest`
- `CommandResult`
- `GitSnapshot`
- `FileChange`
- `RiskWarning`
- `TestSummary`
- `EnvironmentSnapshot`
- `SessionPaths`
- `ICommandRunner`
- `IGitSnapshotProvider`
- `IRedactor`
- `IReportGenerator`
- `ITestResultDetector`
- `IRiskRule`
- `ISessionStore`

Core must not depend on any other AgentTape module.

### AgentTape.Git

Git integration captures repository state using the `git` CLI. This keeps the first implementation simple, portable, and free from native library dependencies.

Responsibilities:

- detect whether the working directory is inside a git repository,
- capture branch and HEAD SHA,
- capture `git status --porcelain=v1`,
- convert status output into `FileChange` values,
- capture final diffs,
- tolerate non-git directories.

Non-git directories are valid. A command recording session must not fail just because the working directory is not a git repository.

### AgentTape.Redaction

Redaction protects reports, exported summaries, and session bundles from common accidental secret exposure.

Modes:

- `Off`: no masking. Must never be the default.
- `Standard`: masks common tokens, passwords, JWTs, connection string secrets, and local user paths. This is the default.
- `Strict`: includes `Standard` behavior plus additional identity-oriented masking such as email addresses.

Redaction is local-only and rule-based in v0.1. It must not call an external service.

### AgentTape.Reporting

Reporting turns a `TapeSession` into local artifacts.

MVP report formats:

- Markdown for pull request and issue summaries,
- static HTML for shareable local reports.

Reporting must always HTML-encode untrusted content and must consume redacted data.

### AgentTape.Testing

Testing parsers extract deterministic test signals from command output or imported test files.

MVP scope:

- basic `dotnet test` text output detection.

Later scope:

- TRX,
- xUnit XML,
- NUnit XML,
- JUnit XML.

This module parses evidence. It must not run test commands itself.

### AgentTape.Rules

Rules produce risk warnings from captured command, git, and test evidence.

Initial warning families:

- config files changed,
- secret-looking files touched,
- suspicious command patterns,
- failed test commands,
- failed build commands,
- lockfiles changed,
- migrations changed,
- binary files changed,
- large deletes.

Rules should be deterministic and explainable. Do not add an opaque scoring engine in the MVP.

## Dependency Direction

Allowed dependencies:

```text
AgentTape.Cli       -> all implementation modules
AgentTape.Git       -> AgentTape.Core
AgentTape.Redaction -> AgentTape.Core
AgentTape.Reporting -> AgentTape.Core
AgentTape.Testing   -> AgentTape.Core
AgentTape.Rules     -> AgentTape.Core
AgentTape.Core      -> no AgentTape module dependency
```

Forbidden dependencies:

- `AgentTape.Core -> AgentTape.Reporting`
- `AgentTape.Core -> AgentTape.Git`
- `AgentTape.Core -> AgentTape.Redaction`
- `AgentTape.Redaction -> AgentTape.Reporting`
- `AgentTape.Rules -> AgentTape.Git`
- `AgentTape.Testing -> AgentTape.Cli`

## Recording Flow

The target `record` flow is:

1. Parse CLI arguments.
2. Load configuration or default options.
3. Create a session id and session directory.
4. Capture git state before command execution.
5. Run the requested command.
6. Capture stdout, stderr, exit code, start time, finish time, and duration.
7. Apply redaction before writing report-facing output.
8. Capture git state after command execution.
9. Capture final diff.
10. Run test detectors.
11. Run risk rules.
12. Save structured session files.
13. Generate Markdown and HTML reports.
14. Print a concise console summary.
15. Return the wrapped command exit code.

## Exit Code Rule

`agenttape record -- <command>` must return the wrapped command's exit code.

If `dotnet test` exits with `1`, AgentTape must also exit with `1`. AgentTape-specific failures should use separate usage or internal error codes.

## Session Layout

Target v0.1 layout:

```text
.agenttape/
  sessions/
    2026-06-24-142001-fix-tests/
      session.json
      commands.jsonl
      stdout/
        001.txt
      stderr/
        001.txt
      git/
        before-status.txt
        after-status.txt
        final.diff
      tests/
      redaction-log.json
  reports/
    session.html
    session.md
```

The layout should stay easy to inspect manually. Avoid opaque binary session formats until the plain layout is stable.

## Security Defaults

- Redaction defaults to `standard`.
- Raw output must not be shown in reports by default.
- Export and pack flows must prefer redacted data.
- Prompt capture, if added later, must be opt-in.
- Replay must start as dry-run only.
- AgentTape must remain useful without an LLM.

## Acceptable MVP Shortcuts

These are acceptable in v0.1:

- simple hand-written CLI parsing,
- direct constructor wiring instead of a dependency injection container,
- git CLI instead of libgit2,
- string-based HTML generation if all dynamic content is encoded,
- basic regex-based redaction,
- limited dotnet test text parsing.

These are not acceptable:

- skipping redaction,
- putting implementation logic into Core,
- writing raw secrets into generated reports,
- adding cloud upload,
- adding default LLM calls,
- adding real command replay execution.
