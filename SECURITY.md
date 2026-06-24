# Security Policy

AgentTape records command output, git metadata, diffs, file paths, and test signals. Treat all captured session data as potentially sensitive.

## Supported Versions

Security support begins with the first tagged release. Until then, report issues against `main`.

| Version | Supported |
| --- | --- |
| `main` | Yes |
| pre-release branches | Best effort |

## Reporting a Vulnerability

Please do not open a public issue for vulnerabilities.

Use GitHub private vulnerability reporting if available, or contact the maintainer through the GitHub profile with:

- a short description of the issue
- reproduction steps
- affected files or commands
- whether secrets, paths, or raw command output can leak

## Security Design Rules

- Redaction must be enabled by default.
- Generated reports must not include raw secrets.
- Pack/export flows must prefer redacted data.
- Replay must stay dry-run by default.
- AgentTape must not execute downloaded scripts or scanned repository code as part of analysis.
