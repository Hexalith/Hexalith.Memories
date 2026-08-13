---
title: 'Authorize the 2026-08-11 submodule bumps'
type: 'maintenance'
created: '2026-08-11'
status: 'ready-for-dev'
baseline_commit: 'e1bde828092f5b59d3e1717354f572a07f575af4'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

## Intent

Authorize a pointer-only root commit for root-declared Hexalith submodules
whose checked-out `main` commits match their fetched `origin/main` tips.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md`
- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Tenants`

## Acceptance Criteria

- Every bumped submodule commit is available from its upstream `main` branch.
- The staged diff contains only this authorization record and the authorized gitlinks.
- Commitlint and the repository hooks accept the root commit without bypasses.
