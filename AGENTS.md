# bogopali

Lightweight background utility for Windows (macOS later) that turns Telex-style
sequences into Romanized Pāli with diacritics. See `GLOSSARY.md` for the domain
language and `docs/adr/` for architectural decisions.

## Build and test

Requires the .NET 11 SDK (pinned in `global.json`). From the repository root:

- `dotnet build bogopali.slnx` (warnings are errors; naming and analyzer rules fail the build)
- `dotnet test --solution bogopali.slnx` (xUnit v3 on Microsoft.Testing.Platform)
- `dotnet format bogopali.slnx --verify-no-changes`

`src/Bogopali.Core` must stay platform-neutral (`net11.0`, no OS-specific APIs); CI runs its tests on Linux.

## Agent skills

### Issue tracker

Issues and specs live in GitHub Issues on `jamestran1/bogopali`, managed with the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

The five default triage roles, each label named after its role (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
