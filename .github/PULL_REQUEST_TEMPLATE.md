## Summary

<!-- What changed, and why? Link the issue this PR closes. -->

- Closes #

## Change Type

- [ ] CLI behavior
- [ ] Session recording or storage
- [ ] Git capture
- [ ] Redaction
- [ ] Reporting
- [ ] Test parsing
- [ ] Risk rules
- [ ] Documentation
- [ ] GitHub workflow or repository maintenance

## Verification

- [ ] `dotnet build AgentTape.slnx --no-restore`
- [ ] `dotnet test AgentTape.slnx --no-build`
- [ ] CLI smoke test:
      `dotnet run --project src/AgentTape.Cli/AgentTape.Cli.csproj -- record -- dotnet --version`

## Safety Checklist

- [ ] Redaction remains enabled by default.
- [ ] Reports, logs, snapshots, and tests do not expose real secrets.
- [ ] No LLM call, cloud upload, or replay execution was added by default.
- [ ] `AgentTape.Core` does not depend on CLI, Git, reporting, redaction, testing, or rules modules.

## Notes For Reviewers

<!-- Anything reviewers should pay special attention to? -->
