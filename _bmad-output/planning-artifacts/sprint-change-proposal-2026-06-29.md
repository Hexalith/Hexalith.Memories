# Sprint Change Proposal - Hexalith Dependency Mode Policy

Date: 2026-06-29
Project: memories
Mode: Batch
Status: Approved and finalized

Approval: User approved on 2026-06-30.
Scope classification: Moderate.
Routed to: Developer implementation in this repository; Product Owner / Developer follow-up only if package-source availability needs backlog tracking.

## 1. Issue Summary

Hexalith.Memories currently uses unconditional source `ProjectReference` entries for cross-repository Hexalith dependencies in several project files:

- `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj` references `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Shell` by project.
- `tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj` references `Hexalith.FrontComposer.Testing` by project.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` references `Hexalith.EventStore.Aspire` by project.

The corrected policy is:

- Debug builds use local `ProjectReference` entries for Hexalith libraries when the root-declared source under `references/` is available.
- Release/package builds use versionless `PackageReference` entries pinned through `Directory.Packages.props`.
- Package publication must not run with source project references enabled.

This aligns Memories with the current Hexalith.EventStore dependency rule: cross-repo Hexalith libraries are source-debug, package-release, and release/package publication must never use the source-reference mode.

## 2. Impact Analysis

### Epic Impact

Epic 0 remains viable. Story 0.0 and Story 0.4 should be amended to clarify that the clean-build and CI preflight checks cover both Debug/source-reference and Release/package-reference dependency modes.

Epic 14 remains viable. Story 14.2 is the main operational-readiness anchor because this is release pipeline/package validation hardening.

Epic 15 remains viable. Story 15.6 already owns scaffolding and submodule build hardening, so it should carry the `Directory.Build.props` dependency-mode property work if that story remains active.

Epic 17 is indirectly affected because `Hexalith.Memories.Web` consumes FrontComposer. No UX behavior changes are required.

Epic 18 is indirectly affected because downstream AppHost stability depends on package-mode validation. Story 18.1 should reference this policy as part of public-surface stability for downstream consumers.

No new epic is required. No epic resequencing is required.

### Story Impact

Affected current/future story anchors:

- Story 0.0: add the dependency-mode rule to scaffolding/build acceptance.
- Story 0.4: add CI evidence for Debug and Release restore/build modes.
- Story 14.2: add release validation that fails if release/package builds carry `ProjectReference` dependencies to external Hexalith projects.
- Story 15.6: add implementation checkpoint for conditional Hexalith dependency mode in `Directory.Build.props` and affected `.csproj` files.
- Story 18.1: add downstream stability note that consumers can build against NuGet packages without requiring checked-out source submodules.

### Artifact Conflicts

PRD: No product-scope change. The PRD already states Memories is delivered as NuGet packages and uses root-declared Hexalith submodules for ecosystem consistency. Add a build-policy clarification only if the PRD is updated.

Architecture: Update the build/package management sections to document the Debug/source and Release/package invariant.

UX: No UI/UX specification change is required.

Implementation artifacts: affected story files should be updated after approval, especially 14.2, 15.6, and any active story currently touching project scaffolding or release validation.

### Technical Impact

Expected implementation work:

- Add a repository-level dependency-mode property, using the EventStore-compatible default:
  - `UseHexalithProjectReferences=true` by default for `Debug`.
  - `UseHexalithProjectReferences=false` by default otherwise.
  - Allow explicit override only for intentional source-debug sessions.
- Add source-presence flags for each relevant external dependency:
  - `HexalithFrontComposerFromSource`
  - `HexalithEventStoreFromSource`
- Add centralized package versions for package-mode fallback:
  - `Hexalith.FrontComposer.Contracts`
  - `Hexalith.FrontComposer.Shell`
  - `Hexalith.FrontComposer.Testing`
  - `Hexalith.EventStore.Aspire`
- Replace unconditional external `ProjectReference` entries with conditional project/package pairs.
- Add release/build validation that package publication cannot run with `UseHexalithProjectReferences=true`.
- Rerun restore after switching modes to avoid stale project-reference assets.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The change is a build/release policy correction, not a product requirement change.
- Existing project structure already has root detection properties for FrontComposer and EventStore.
- The pattern already exists in sibling Hexalith repositories.
- No rollback is needed because this can be applied incrementally and validated with restore/build commands.
- MVP scope is unchanged.

Effort estimate: Low to Medium.

Risk level: Medium. The main risk is stale restore assets or an incomplete project/package pair that makes Debug work locally but Release fail in CI. This is controlled by explicit Debug and Release validation.

## 4. Detailed Change Proposals

### Story: 0.0 Project Scaffolding & Single-Command Boot

Section: Acceptance Criteria

OLD:

```markdown
**When** I run `dotnet build`
**Then** the build succeeds with projects: Contracts, Server, Redis, AppHost, ServiceDefaults
**And** if git submodules are missing, the build prints a helpful error message instead of cryptic MSBuild failures
```

NEW:

```markdown
**When** I run `dotnet build`
**Then** the build succeeds with projects: Contracts, Server, Redis, AppHost, ServiceDefaults
**And** Debug builds use available root-declared Hexalith source dependencies through conditional `ProjectReference` entries
**And** Release builds use centrally pinned NuGet `PackageReference` entries for external Hexalith libraries
**And** if required root-declared submodules are missing for source-debug mode, the build prints a helpful error message instead of cryptic MSBuild failures
```

Rationale: Keeps the original scaffolding guarantee while adding the source-debug/package-release invariant.

### Story: 0.4 Minimum Build/Test CI Preflight

Section: Acceptance Criteria

OLD:

```markdown
**When** the minimum CI preflight runs
**Then** the repository restores and builds `Hexalith.Memories.slnx` with `TreatWarningsAsErrors=true`
**And** the build uses the SDK pinned by `global.json`
**And** package versions remain centrally managed.
```

NEW:

```markdown
**When** the minimum CI preflight runs
**Then** the repository restores and builds `Hexalith.Memories.slnx` with `TreatWarningsAsErrors=true`
**And** the build uses the SDK pinned by `global.json`
**And** package versions remain centrally managed
**And** Debug/source-reference restore-build and Release/package-reference restore-build are both validated, or the story records an explicit deferred validation owner.
```

Rationale: CI must catch both local-development and release-consumption dependency modes.

### Story: 14.2 Release Pipeline Audit Hardening

Section: Acceptance Criteria

OLD:

```markdown
**Given** package validation runs,
**When** `tools/validate-release-packages.ps1` scans `src/**/*.csproj`,
**Then** every packable and non-packable project is accounted for in release package inventory
**And** direct operator version inputs with build metadata fail or normalize with a clear message.
```

NEW:

```markdown
**Given** package validation runs,
**When** `tools/validate-release-packages.ps1` scans `src/**/*.csproj`,
**Then** every packable and non-packable project is accounted for in release package inventory
**And** direct operator version inputs with build metadata fail or normalize with a clear message
**And** Release/package validation fails if any external Hexalith dependency is resolved through `ProjectReference` instead of a centrally pinned NuGet `PackageReference`.

**Given** release publication is prepared,
**When** a pack or publish command runs,
**Then** the command fails fast if `UseHexalithProjectReferences=true` is present in the release context.
```

Rationale: Release validation is the control that prevents source checkout state from leaking into NuGet package metadata.

### Story: 15.6 Scaffolding Hardening Sweep

Section: Implementation Checkpoints

OLD:

```markdown
- Checkpoint B — Submodule guard expansion: `Directory.Build.props` `CheckSubmodules` MSBuild target validates every `.gitmodules` entry under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, `references/Hexalith.PolymorphicSerializations`).
```

NEW:

```markdown
- Checkpoint B — Submodule guard expansion and Hexalith dependency mode: `Directory.Build.props` `CheckSubmodules` MSBuild target validates every `.gitmodules` entry under `references/` (`references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.AI.Tools`, `references/Hexalith.Tenants`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, `references/Hexalith.PolymorphicSerializations`). The same checkpoint introduces the conditional dependency-mode policy: Debug/source builds may use external Hexalith `ProjectReference` entries when source is present; Release/package builds use centrally pinned NuGet `PackageReference` entries.
```

Rationale: This is the closest existing story for repository-level scaffolding and submodule build behavior.

### Story: 18.1 AppHost Project-Resolution Guard and Public-Surface Stability Contract

Section: Acceptance Criteria

OLD:

```markdown
**Given** external AppHosts depend on stable project and assembly names,
**When** this story completes,
**Then** the project name, assembly name, and root namespace of `Hexalith.Memories.Server` and `Hexalith.Memories.Mcp` are recorded as a stability contract under `docs/dev`, and any future rename is flagged as requiring a breaking-change note.
```

NEW:

```markdown
**Given** external AppHosts depend on stable project, assembly, and package names,
**When** this story completes,
**Then** the project name, assembly name, root namespace, and NuGet package consumption path of `Hexalith.Memories.Server`, `Hexalith.Memories.Mcp`, and relevant Hexalith platform dependencies are recorded as a stability contract under `docs/dev`
**And** downstream AppHosts can validate Release/package mode without requiring checked-out Hexalith source submodules
**And** any future rename or dependency-mode regression is flagged as requiring release-impact review.
```

Rationale: Downstream consumers need the package-mode contract, not only source project symbol stability.

### Architecture Update

Section: Source of Truth / Package management

ADD:

```markdown
External Hexalith dependencies follow the source-debug/package-release invariant. Debug builds default to `UseHexalithProjectReferences=true` and may consume root-declared Hexalith source through conditional `ProjectReference` entries when present. Release builds default to `UseHexalithProjectReferences=false` and consume centrally pinned NuGet `PackageReference` entries. Package publication must fail if source-reference mode is enabled.
```

Rationale: Architecture should record the invariant where future build and release decisions are made.

### PRD Update

Section: Developer Tool / API Backend Specific Requirements

ADD:

```markdown
Build and release policy: Hexalith.Memories uses local project references for external Hexalith libraries during Debug/source-development builds, and NuGet package references during Release/package builds. This preserves debug-first contributor experience while keeping published package metadata reproducible and independent of local submodule checkout state.
```

Rationale: Product scope is unchanged, but the PRD should not imply that root-declared submodules are the release dependency mechanism.

## 5. Implementation Handoff

Scope classification: Moderate.

Reason: No product replan is required, but the change touches build policy, project files, release validation, and multiple operational-readiness story anchors.

Route to:

- Developer agent: implement conditional MSBuild/project changes and focused validation.
- Product Owner / Developer agents: update affected story text and sprint status after approval.

Implementation tasks:

1. Update `Directory.Build.props` with `UseHexalithProjectReferences` defaults and `Hexalith*FromSource` flags.
2. Update `Directory.Packages.props` with package-mode fallback versions for external Hexalith packages.
3. Convert external Hexalith project references in Web, Web.Tests, and AppHost to conditional project/package pairs.
4. Add release guard coverage in `tools/validate-release-packages.ps1` or an adjacent focused validation script.
5. Validate:
   - `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=true`
   - `dotnet build Hexalith.Memories.slnx -c Debug -p:UseHexalithProjectReferences=true`
   - `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=false`
   - `dotnet build Hexalith.Memories.slnx -c Release -p:UseHexalithProjectReferences=false`
   - release package validation command used by the repo.
6. Update affected story artifacts and `sprint-status.yaml` only after approval.

Success criteria:

- Debug/source mode uses project references where expected.
- Release/package mode uses NuGet package references for external Hexalith dependencies.
- Central package management remains the only source of package versions.
- Release validation fails if source-reference mode is enabled for package publication.
- No root-declared submodule is initialized recursively or modified.

## 6. Finalization Notes

User approval was received on 2026-06-30. The proposal is approved for implementation as a Moderate change because it affects build policy, project files, package-mode validation, and planning traceability while leaving product scope unchanged.

Implementation route:

- Developer agent applies the root MSBuild dependency-mode policy.
- Developer agent converts affected Memories project files from unconditional external Hexalith project references to conditional source/package pairs.
- Developer agent validates Debug/source and Release/package restore/build behavior.
- Product Owner / Developer follow-up is needed only if clean CI runners cannot access the currently pinned FrontComposer review packages. The local package cache contains `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer.Testing` version `0.2.0-review.77962d15`; nuget.org search did not list those packages through the configured source.

Sprint-status impact: no epic or story status transition is required by this proposal because no epic or story was added, removed, renumbered, or reopened. The implementation work is tracked by `_bmad-output/implementation-artifacts/spec-hexalith-dependency-mode-policy.md`.

## Checklist Progress

- [x] 1.1 Trigger identified: cross-Hexalith dependency mode policy correction.
- [x] 1.2 Core problem defined: release builds must not depend on local source checkout state.
- [x] 1.3 Evidence gathered: current unconditional project references found in Web, Web.Tests, and AppHost; EventStore project-context confirms target policy.
- [x] 2.1 Current epic viability assessed: affected epics remain viable.
- [x] 2.2 Epic-level changes determined: no new epic; amend operational-readiness/scaffolding stories.
- [x] 2.3 Remaining epics reviewed: Epic 17 and 18 have indirect dependency-contract impact only.
- [x] 2.4 Future epic invalidation checked: none invalidated.
- [x] 2.5 Epic order checked: no resequencing required.
- [x] 3.1 PRD conflict checked: clarification only.
- [x] 3.2 Architecture conflict checked: build/package policy section should be updated.
- [N/A] 3.3 UI/UX conflict checked: no user-facing UX behavior change.
- [x] 3.4 Other artifacts checked: `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, release validation, story files.
- [x] 4.1 Direct Adjustment evaluated: viable.
- [x] 4.2 Potential Rollback evaluated: not viable/needed.
- [x] 4.3 PRD MVP Review evaluated: not needed; MVP unchanged.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic impact and artifact adjustment needs documented.
- [x] 5.3 Recommended path with rationale documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal accuracy reviewed.
- [x] 6.3 User approval received on 2026-06-30.
- [N/A] 6.4 `sprint-status.yaml` update not required; no epic/story status change.
- [x] 6.5 Handoff confirmed: Moderate scope routed to Developer implementation, with PO/Developer follow-up only for package-source availability if CI lacks FrontComposer review packages.
