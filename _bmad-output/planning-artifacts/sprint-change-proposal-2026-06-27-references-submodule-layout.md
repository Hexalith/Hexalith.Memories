---
project: Hexalith.Memories
date: 2026-06-27
workflow: bmad-correct-course
change_trigger: Move all root-declared submodules under /references.
mode: Batch
status: applied
prepared_for: Jerome
prepared_at: 2026-06-27T08:06:47+02:00
---

# Sprint Change Proposal - References Submodule Layout

## 1. Issue Summary

The requested course correction is to move every root-declared Hexalith.Memories git submodule from the repository root into `references/` and update repository guidance, build/project paths, CI, and planning artifacts to match.

This is a repository topology correction, not a product-scope change. The old layout made submodule paths compete with first-party repository folders at the root and left future story/tooling references vulnerable to reintroducing root-level assumptions.

Evidence gathered during the change:

- `.gitmodules` declared seven root-level submodules before the correction.
- `Directory.Build.props` resolved `Hexalith.FrontComposer` and `Hexalith.EventStore` from root-level or parent-root sibling paths.
- `CheckSubmodules` guarded bare root names instead of the `.gitmodules` paths.
- README, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, dev docs, and BMAD planning artifacts used old root-level language.
- `Hexalith.Memories.Aspire` resolved consumer AppHost project metadata from `Hexalith.Memories/src/...` instead of `references/Hexalith.Memories/src/...`.

## 2. Impact Analysis

### Epic Impact

No epic is invalidated and no epic resequencing is required. The existing epics still describe the same product and implementation outcomes.

The affected story and epic text is governance/build-context only:

- Story 0.0 and Epic 1 foundation language now describes root-declared submodules under `references/`.
- Story 15.6 submodule guard language now requires every `.gitmodules` path, including `references/Hexalith.Builds` and `references/Hexalith.PolymorphicSerializations`.
- Story 18.1 public-surface stability language now references clean clones with root-declared `references/` submodules.

### Artifact Impact

- PRD: no MVP requirement changes; shared dependency references now identify `references/Hexalith.Commons` and `references/Hexalith.EventStore` where repository layout matters.
- Architecture: submodule constraints, starter structure, and first implementation steps now point at `references/`.
- Epics: acceptance criteria and checkpoint language now match the new `.gitmodules` paths.
- UX specification: input-document paths now use `references/Hexalith.*`.
- Project context: LLM instructions now make `references/` the required submodule location.
- Repository docs: README, contributing, agent instructions, release notes, and EventStore integration links now use the new layout.
- CI: nightly checkout now initializes submodules non-recursively with `submodules: true`, matching CI/release behavior.
- Solution: `Hexalith.Memories.slnx` does not contain submodule paths and requires no file change. The solution build validates the new project-reference resolution.

### Technical Impact

- `.gitmodules` paths are now `references/Hexalith.*`.
- Submodule working trees moved to `references/` while preserving existing submodule pointer states.
- `Directory.Build.props` resolves FrontComposer and EventStore project references from `references/` first, with parent-checkout fallbacks for consumer repository layouts.
- `CheckSubmodules` validates every `.gitmodules` path and names the exact missing `references/...` path in the MSBuild error.
- Story-scope forbidden defaults now cover all seven submodule trees under `references/`, including bare pointer paths.
- `Hexalith.Memories.Aspire` now resolves consumer cross-repo metadata from `references/Hexalith.Memories/src/...`.

## 3. Recommended Path

Use Direct Adjustment.

Rationale:

- The change is mechanical and bounded to repository topology, build path resolution, documentation, and planning context.
- Product scope, MVP goals, package inventory, API contracts, tenant isolation behavior, storage, CLI, MCP, and UX behavior are unchanged.
- Rollback is not useful because the new layout is the requested target state and the existing build guard can enforce it.
- A PRD MVP review is unnecessary because no user-facing capability changes.

Effort: Low to Medium.

Risk: Low after build validation. The main risks are stale hard-coded paths and accidental submodule pointer changes; both are covered by the updated guard and validation commands.

## 4. Action Plan

1. Move all `.gitmodules` paths and working trees under `references/`.
2. Update MSBuild project-reference roots and missing-submodule guard logic.
3. Update repository docs, LLM instructions, project context, PRD, architecture, epics, and UX input paths.
4. Update CI checkout behavior where needed so `references/` submodules are initialized non-recursively.
5. Update story-scope tooling and tests so submodule content/pointer changes under `references/` remain forbidden by default.
6. Validate with focused tooling tests, MSBuild guard execution, and a solution build.

## 5. Handoff Plan

- Developer agent: completed the repository move, path updates, tests, and build validation.
- Product/PM follow-up: no backlog re-planning required.
- Architecture follow-up: no architectural redesign required; keep future topology references path-specific.
- QA follow-up: keep using the existing test lanes; add no new quality gate unless future CI starts running a standard `git diff --check` without CRLF-aware whitespace settings.

## 6. Validation

- `dotnet msbuild src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj -t:CheckSubmodules -nologo -v:minimal`
- `python3 tools/check-story-file-scope.py --story-key 12-3-story-file-scope-enforcement --changed-file references/Hexalith.EventStore` failed closed as expected for a forbidden bare submodule pointer path.
- `python3 -m unittest tests.tooling.story_scope.story_scope_validator_test`
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~SubmoduleGuardTests --no-restore --nologo`
- `dotnet build Hexalith.Memories.slnx --no-restore --nologo`

## 7. Decision

Applied. The canonical root-declared submodule layout for Hexalith.Memories is now `references/Hexalith.*`, initialized with non-recursive root submodule commands.
