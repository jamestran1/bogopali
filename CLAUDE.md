## Agent skills

### Issue tracker

GitHub Issues (with External PRs as a triage surface). See `docs/agents/issue-tracker.md`.

### Triage labels

Default triage labels mapping to standard strings. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout at repository root. See `docs/agents/domain.md`.

## Socratic & Quality Workflow

To maximize developer learning (Rust syntax, patterns) and code quality, always follow this workflow:

1. **Task Initialization**: AI drafts local PRD -> Discuss with developer -> Publish to GitHub Issue with `ready-for-agent` label.
2. **Interface Design**: Run `/design-an-interface` to draft 2 distinct designs -> Compare trade-offs -> Agree on final interface.
3. **Socratic TDD Loop**: For each test case:
   - **RED**: Write the test and ask the developer about the test behavior/intent.
   - **GREEN**: Write minimal code to pass and ask the developer about Rust syntax used (ownership, match, types).
   - **REFACTOR**: Optimize code and ask the developer about design principles (depth, simplicity, locality).
4. **Critical Review**: Run `/review` -> AI asks 1-2 critical feedback questions -> Developer answers -> Commit.
5. **Knowledge Archiving**: AI drafts updates to `docs/agents/LEARNING.md` summarizing key Rust concepts and design decisions. Developer reviews and saves.

