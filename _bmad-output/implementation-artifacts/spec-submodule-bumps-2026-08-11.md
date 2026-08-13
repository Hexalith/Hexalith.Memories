---
title: 'Refresh Hexalith submodules and package versions'
type: 'maintenance'
created: '2026-08-11'
status: 'done'
baseline_commit: 'e1bde828092f5b59d3e1717354f572a07f575af4'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

## Intent

Refresh every root-declared Hexalith submodule to the latest verified upstream
`main` revision and align the centrally owned Hexalith NuGet package versions
used by this repository with the latest compatible stable releases.

Preserve all unrelated Story 24.6 work already present in the root working tree.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md`
- `references/Hexalith.Builds/Props/Directory.Packages.props`
- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.Tenants`

## Tasks & Acceptance

- [x] Verify every root-declared submodule against its upstream `main` tip and advance only stale pointers.
- [x] Identify the Hexalith packages directly consumed by Memories and align their central version properties with the latest compatible stable NuGet releases.
- [x] Restore and build `Hexalith.Memories.slnx` in Release package mode.
- [x] Validate that dependency changes are isolated from the pre-existing Story 24.6 work and that all intended commit messages pass commitlint without bypasses.

## Acceptance Criteria

- Given a root-declared Hexalith submodule, when its checked-out revision is compared with upstream `main`, then it matches the verified upstream tip or remains unchanged because it already matched.
- Given a directly consumed Hexalith package, when the central catalog is evaluated, then it resolves to the latest compatible stable release verified from NuGet.
- Given the existing dirty root working tree, when this dependency refresh is reviewed, then no pre-existing Story 24.6 file content is modified, reverted, or absorbed into dependency commits.
- Given the updated dependency graph, when Release restore and build run in package mode, then they complete successfully with warnings treated as errors.
- Given any dependency or root commit created for this work, when commitlint and repository hooks run, then they accept it without bypasses.
