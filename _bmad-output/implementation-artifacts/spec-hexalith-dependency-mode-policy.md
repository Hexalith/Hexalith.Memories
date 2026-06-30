---
title: 'Hexalith Dependency Mode Policy'
type: 'chore'
created: '2026-06-30'
status: 'done'
baseline_commit: '24757db93c90427b83195deb5e5a54b092affc2b'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** Cross-repository Hexalith dependencies currently use unconditional project references in Memories Web, Web.Tests, and AppHost. That preserves local source debugging but lets Release builds and package publication depend on checked-out submodule state.

**Approach:** Introduce a root dependency-mode switch that defaults to source `ProjectReference` in Debug and NuGet `PackageReference` in Release, then convert affected project files to conditional project/package pairs. Release or pack/publish work must fail fast if source-reference mode is forced.

## Boundaries & Constraints

**Always:** Keep package versions centralized in `Directory.Packages.props`; keep `.csproj` package references versionless; do not edit submodule contents or pointers; use root-detected `$(Hexalith*Root)` properties for source mode; preserve helpful submodule diagnostics for source-debug mode.

**Ask First:** Adding or changing package sources; pinning to a package version that is not available in the configured source or current global package cache; changing release package inventory; modifying submodule files or pointer revisions.

**Never:** Use recursive submodule commands; add `Version` attributes to project files; let `Pack` or `Publish` proceed with `UseHexalithProjectReferences=true`; silently fall back to source references in Release.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Debug source mode | `Configuration=Debug`, root-declared source exists | External Hexalith dependencies resolve through `ProjectReference` | Missing required source submodule emits the existing helpful submodule diagnostic |
| Release package mode | `Configuration=Release` | External Hexalith dependencies resolve through versionless `PackageReference` entries pinned centrally | Missing/unavailable package fails restore with NuGet's package diagnostic |
| Forced source in Release | `Configuration=Release -p:UseHexalithProjectReferences=true` | Build/restore fails before producing release output | MSBuild error names the dependency-mode policy |
| Pack/publish source leak | Any pack/publish with `UseHexalithProjectReferences=true` | Pack/publish fails fast | MSBuild error states package publication cannot use source references |

</frozen-after-approval>

## Code Map

- `Directory.Build.props` -- root dependency-mode defaults, source-presence flags, submodule guard condition, release/package guard targets.
- `Directory.Packages.props` -- central pins for external Hexalith NuGet fallback packages.
- `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj` -- FrontComposer project/package switch.
- `tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj` -- FrontComposer.Testing project/package switch.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` -- EventStore.Aspire project/package switch.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29.md` -- approved correct-course record and handoff status.

## Tasks & Acceptance

**Execution:**
- [x] `Directory.Build.props` -- add `UseHexalithProjectReferences` defaults, source flags, source-only submodule check, and Release/pack guard targets -- enforce the policy centrally.
- [x] `Directory.Packages.props` -- add `HexalithEventStoreVersion`, `HexalithFrontComposerVersion`, and package versions for EventStore.Aspire plus FrontComposer Contracts/Shell/Testing -- keep package mode centrally pinned.
- [x] `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj`, `tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`, `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` -- convert external Hexalith references to conditional project/package pairs -- make Debug and Release dependency graphs explicit.
- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29.md` -- mark approved/finalized and record implementation handoff -- close the correct-course workflow.

**Acceptance Criteria:**
- Given Debug source mode and initialized root-declared submodules, when the solution restores/builds, then the affected external Hexalith dependencies are project references.
- Given Release package mode, when the solution restores/builds, then the affected external Hexalith dependencies are NuGet package references.
- Given `UseHexalithProjectReferences=true` in a Release build or package publication, when MSBuild evaluates the root props, then it fails with a clear dependency-mode error.
- Given project files are reviewed, when package references are inspected, then they remain versionless and all versions live in `Directory.Packages.props`.

## Spec Change Log

## Design Notes

The FrontComposer package fallback uses `0.2.0-review.77962d15`, the latest matching package version currently present in the local global package cache for Contracts, Shell, and Testing. `dotnet package search` did not list these packages from nuget.org, so release CI must either have the same package source/cache available or publish/configure those packages before relying on clean-runner package-mode restore.

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=true` -- passed; source-mode restore succeeded.
- `dotnet build Hexalith.Memories.slnx -c Debug -p:UseHexalithProjectReferences=true --no-restore` -- passed; Debug build produced FrontComposer/EventStore submodule project outputs.
- `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=false` -- passed; package-mode restore succeeded from configured source/cache.
- `dotnet build Hexalith.Memories.slnx -c Release -p:UseHexalithProjectReferences=false --no-restore` -- passed; Release build succeeded without building FrontComposer/EventStore submodule projects.
- `dotnet build Hexalith.Memories.slnx -c Release -p:UseHexalithProjectReferences=true --no-restore` -- failed as expected with `Release builds must use NuGet package references for external Hexalith libraries`.
- `dotnet pack src/Hexalith.Memories.Contracts/Hexalith.Memories.Contracts.csproj -c Debug -p:UseHexalithProjectReferences=true --no-build --no-restore` -- failed as expected with `Package publication must not use external Hexalith project references`; no `.nupkg` was produced after tightening the guard.
- `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1` -- passed.
- `git diff --check -- Directory.Build.props Directory.Packages.props src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29.md _bmad-output/implementation-artifacts/spec-hexalith-dependency-mode-policy.md` -- passed.
