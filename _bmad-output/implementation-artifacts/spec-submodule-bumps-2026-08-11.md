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

## Verification

Implementation commit: `cb863b46` (`build(deps): refresh root Hexalith submodule tips`).

Root submodule tips vs `origin/main` after refresh:

| Submodule | Revision | Action |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `de38f78e` | advanced |
| `references/Hexalith.Builds` | `99d5a46c` | advanced |
| `references/Hexalith.Commons` | `6fbac0c5` | already at tip |
| `references/Hexalith.EventStore` | `24e5caea` | already at tip |
| `references/Hexalith.FrontComposer` | `1fa8b0b1` | advanced |
| `references/Hexalith.PolymorphicSerializations` | `5dd6aa88` | already at tip |
| `references/Hexalith.Tenants` | `acab0b51` | already at tip |

Direct Memories `PackageReference` Hexalith packages (EventStore Aspire/Client, FrontComposer Contracts/Shell/Testing) already match latest stable NuGet (`3.94.0` / `4.1.1`). No `Directory.Packages.props` edit required without breaking submodule tip equality.

Commands:

```bash
dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=false
dotnet build Hexalith.Memories.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=false
```

Both succeeded. Nested FrontComposer submodules were intentionally not initialized (root-submodule policy).

## Suggested Review Order

- Start with the story intent and verified tip/NuGet outcomes.
  [`spec-submodule-bumps-2026-08-11.md:11`](spec-submodule-bumps-2026-08-11.md#L11)

- Confirm AI.Tools gitlink advanced to origin/main tip.
  [`Hexalith.AI.Tools:1`](../../references/Hexalith.AI.Tools#L1)

- Confirm Builds gitlink advanced to origin/main tip.
  [`Hexalith.Builds:1`](../../references/Hexalith.Builds#L1)

- Confirm FrontComposer gitlink advanced to origin/main tip.
  [`Hexalith.FrontComposer:1`](../../references/Hexalith.FrontComposer#L1)

- Check deferred catalog/advisory residuals recorded from review.
  [`deferred-work.md:1`](deferred-work.md#L1)
