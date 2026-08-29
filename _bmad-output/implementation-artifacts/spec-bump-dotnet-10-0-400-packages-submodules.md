---
title: 'Bump to .NET SDK 10.0.400, Latest Packages, and Latest Submodules'
type: 'build'
created: '2026-08-29'
status: 'done'
baseline_commit: '7b3f29ce447275de28d643ca903df5d2d8a68865'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository currently pins .NET SDK 10.0.302 in `global.json`, specifies minimum SDK 10.0.302 in CLI prerequisite checks and documentation, and requires alignment with latest .NET SDK 10.0.400, latest packages, and latest submodule states.

**Approach:** Update `global.json` to SDK 10.0.400, update CLI prerequisite checks, error translation catalogs, and unit tests to enforce 10.0.400, update documentation references, verify all root-declared submodules are up to date with remote main, and validate the build and test suites cleanly.

## Boundaries & Constraints

**Always:** Follow Hexalith C# coding standards, TreatWarningsAsErrors=true, Conventional Commits, file-scoped namespaces, and validate with test execution.

**Ask First:** Introducing breaking changes to external package dependencies or changing build architecture.

**Never:** Initialize or update nested submodules (do not use recursive submodule updates); use the `chore` commit type; bypass commitlint.

</frozen-after-approval>

## Code Map

- `global.json` -- Root .NET SDK version configuration pinned to 10.0.400
- `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs` -- MinimumDotnetSdkVersion updated to 10.0.400 and diagnostic recovery suggestion
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` -- DOTNET_VERSION_INSUFFICIENT error catalog suggestion updated to 10.0.400
- `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartPrerequisiteTests.cs` -- Unit tests validating 10.0.400 SDK minimum threshold and diagnostics
- `README.md` -- Quickstart prerequisites and setup instructions updated to .NET SDK 10.0.400
- `docs/dev/quickstart.md` -- Prerequisite section and diagnostic strings updated to .NET SDK 10.0.400
- `docs/operations/upgrade-migration.md` -- Upgrade and deployment pins updated to .NET SDK 10.0.400
- `references/*` -- Root-declared submodules verified against latest origin/main

## Tasks & Acceptance

**Execution:**
- [x] `global.json` -- Update SDK version to 10.0.400 -- Ensure global toolchain uses .NET SDK 10.0.400
- [x] `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs` -- Update `MinimumDotnetSdkVersion` to `new(10, 0, 400)` and diagnostic strings -- Align CLI prerequisite verification
- [x] `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` -- Update `DOTNET_VERSION_INSUFFICIENT` message to recommend 10.0.400 -- Keep error catalog consistent with prerequisite rules
- [x] `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartPrerequisiteTests.cs` -- Update test assertions and test data for SDK 10.0.400 -- Verify CLI prerequisite check tests pass
- [x] `README.md` -- Update .NET SDK version mention to 10.0.400 -- Keep documentation accurate
- [x] `docs/dev/quickstart.md` -- Update .NET SDK version mention to 10.0.400 -- Keep developer guide accurate
- [x] `docs/operations/upgrade-migration.md` -- Update .NET SDK version mention to 10.0.400 -- Keep operations guide accurate
- [x] `references/*` -- Verify all root submodules are clean and at latest origin/main -- Ensure submodules are up to date
- [x] `Hexalith.Memories.slnx` -- Restore, build Release, and run tests -- Confirm end-to-end green validation

**Acceptance Criteria:**
- Given `global.json`, when read by dotnet CLI, then active SDK version is 10.0.400.
- Given `PrerequisiteChecks`, when `dotnet --list-sdks` returns 10.0.400, then check passes.
- Given `PrerequisiteChecks`, when `dotnet --list-sdks` returns only older SDKs (e.g. 10.0.302), then check fails with diagnostic mentioning 10.0.400.
- Given `Hexalith.Memories.slnx`, when building in Release configuration, then 0 errors and 0 warnings are emitted.
- Given `tests/Hexalith.Memories.Cli.Tests`, when tests run, then all unit tests pass.

## Spec Change Log

## File Scope

Allowed files for this story:
- `README.md`
- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-24-6-eighth-pass-action-item-closure.md`
- `_bmad-output/implementation-artifacts/spec-bump-dotnet-10-0-400-packages-submodules.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/project-context.md`
- `docs/dev/quickstart.md`
- `docs/operations/route-surface.md`
- `docs/operations/upgrade-migration.md`
- `global.json`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Tenants`
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`
- `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartPrerequisiteTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/AssemblyInfo.cs`
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ReleaseDedupKeyIfOwnedActivityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`

## Verification

**Commands:**
- `dotnet --version` -- expected: `10.0.400`
- `dotnet build Hexalith.Memories.slnx --configuration Release` -- expected: Build succeeded with 0 warnings, 0 errors
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj` -- expected: All tests pass

## Suggested Review Order

**Toolchain Configuration**

- Pin .NET SDK version to 10.0.400 in global configuration
  [`global.json:3`](../../global.json#L3)

**CLI Prerequisites & Error Handling**

- Enforce .NET SDK 10.0.400 minimum version in quickstart checks
  [`PrerequisiteChecks.cs:27`](../../src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs#L27)

- Align diagnostic error suggestion for insufficient .NET SDK version
  [`ErrorMessageCatalog.cs:156`](../../src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs#L156)

**Documentation**

- Update quickstart prerequisite instructions to .NET SDK 10.0.400
  [`README.md:12`](../../README.md#L12)

- Update developer guide prerequisite documentation to .NET SDK 10.0.400
  [`quickstart.md:17`](../../docs/dev/quickstart.md#L17)

- Update operations deployment pin reference to .NET SDK 10.0.400
  [`upgrade-migration.md:32`](../../docs/operations/upgrade-migration.md#L32)

**Test Coverage**

- Update test fixtures and assertions to validate .NET SDK 10.0.400 requirement
  [`QuickstartPrerequisiteTests.cs:73`](../../tests/Hexalith.Memories.Cli.Tests/Cli/QuickstartPrerequisiteTests.cs#L73)

