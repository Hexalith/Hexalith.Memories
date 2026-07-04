---
title: 'CI submodule metadata cleanup'
type: 'bugfix'
created: '2026-07-04'
status: 'done'
route: 'one-shot'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

# CI submodule metadata cleanup

## Intent

**Problem:** CI and Release runs for `bf8cb41083f6f0518fb5c67585920f2530b4ed29` failed during `Initialize build submodules` because the parent index still tracked a root-level `Hexalith.PolymorphicSerializations` gitlink, but `.gitmodules` only declares `references/Hexalith.PolymorphicSerializations`.

**Approach:** Remove the stale root-level gitlink and keep the root-declared `references/` submodule entry as the single authoritative dependency path.

## Verification

**Commands:**

- `gh run view 28693780227 --repo Hexalith/Hexalith.Memories --log-failed` -- expected: Release failure is confirmed as `fatal: No url found for submodule path 'Hexalith.PolymorphicSerializations' in .gitmodules`.
- `gh run view 28693780237 --repo Hexalith/Hexalith.Memories --log-failed` -- expected: CI build/test jobs fail at the same submodule init step; artifact upload failures are secondary.
- `git submodule status` -- expected: succeeds and lists only root-declared submodules under `references/`.
- `git ls-files -s | awk '$1 == "160000" { print }'` -- expected: no root-level `Hexalith.PolymorphicSerializations` gitlink remains.
- Isolated staged-tree worktree with `git -c submodule.recurse=false submodule update --init` -- expected: initializes only the seven root-declared `references/` submodules and exits successfully.

## File Scope

Allowed files for this story:

- `Hexalith.PolymorphicSerializations` - REMOVE. Delete the stale root-level submodule gitlink that has no `.gitmodules` mapping.
- `_bmad-output/implementation-artifacts/19-5-ci-submodule-metadata-cleanup.md` - ADD. Record the one-shot trace, validation, and review path for the CI fix.

Read/verify only:

- `.gitmodules`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `.github/workflows/nightly.yml`
- `Directory.Build.props`

Forbidden by default:

- Do not initialize nested submodules recursively.
- Do not add a second `.gitmodules` mapping for the stale root-level path.
- Do not modify submodule contents as part of this metadata cleanup.

## Suggested Review Order

**Submodule Metadata**

- The valid dependency remains rooted under `references/`.
  [`.gitmodules:19`](../../.gitmodules#L19)

- The root-level gitlink deletion removes the unmapped CI failure path.
  [`19-5-ci-submodule-metadata-cleanup.md:34`](19-5-ci-submodule-metadata-cleanup.md#L34)

**Validation**

- Verification commands map directly to the failed GitHub Actions steps.
  [`19-5-ci-submodule-metadata-cleanup.md:24`](19-5-ci-submodule-metadata-cleanup.md#L24)
