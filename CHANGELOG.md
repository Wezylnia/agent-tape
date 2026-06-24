# Changelog

All notable changes to AgentTape will be documented in this file.

This project follows semantic versioning once packaged releases begin.

## 1.0.0 - 2026-06-24

### Added

- Config loading at runtime with CLI override support.
- `--config <path>` global option for explicit config files.
- Per-session report storage with latest aliases.
- `list` and `show` commands for session management.
- `--session <id>` support for report and export commands.
- Git diff and path redaction before storage.
- Real redaction log with rule name and match counts (no raw secrets).
- Git session delta: separate pre-existing changes from session changes.
- Git numstat integration: line counts and binary detection for file changes.
- `report --open` to open HTML reports in the default browser.
- Export improvements: `--github-pr`, `--output <path>`, `--format html`.
- TRX test result parser for .NET test output.
- Environment snapshot capture (OS, .NET SDK, git, shell versions).
- Expanded risk warnings: rmdir, iex, git clean, format, shutdown detection.
- `--shell` mode for shell built-in command recording.
- `--version` / `-v` flag.
- `.gitignore` protection in `init` command.
- CI workflow with build, test, and pack steps.
