---
title: 'Run all tests and fix issues'
type: 'bugfix'
created: '2026-07-03'
status: 'done'
baseline_commit: 'c2569f35cb3553f984f01f64e9371c633d85d680'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** The current working tree needs full validation across the root Hexalith.Memories test surface, and any failures that are repository issues should be fixed before handoff.

**Approach:** Discover the owned test commands from the solution, local test scripts, and workflows; run every root test project, including unit, Web, integration, slow integration, and benchmark lanes where the environment supports them; fix failing code or test issues without reverting pre-existing user changes.

## Boundaries & Constraints

**Always:** Preserve existing dirty worktree changes unless a failing test directly requires a compatible edit. Follow centralized package management, .NET 10, warnings-as-errors, xUnit v3, Shouldly, and root submodule rules. Include exact commands and blockers for any test lane that cannot run locally.

**Ask First:** Destructive git operations, recursive submodule initialization, modifying external submodule contents, broad dependency upgrades, or changing public contracts beyond what a failing test proves necessary.

**Never:** Do not use recursive submodule commands. Do not silence warnings globally, add package versions to `.csproj` files, skip root-owned test projects silently, or treat external service unavailability as a code fix without evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| All tests pass | Root solution and test inventories are runnable | Report successful commands and no code fixes needed | Include existing dirty-tree note |
| Test failure | A root-owned test command fails due to code or test issue | Fix the minimal affected source/test files and rerun the failing lane plus relevant broader lane | Preserve unrelated dirty changes |
| Environment blocker | Docker, Dapr, SDK, or external runtime support is unavailable | Report exact command, failing prerequisite, and any lanes already validated | Do not fake pass status |

</frozen-after-approval>

## Code Map

- `Hexalith.Memories.slnx` -- root solution test and build inventory.
- `tools/test.sh` / `tools/test.ps1` -- local test runner and category-to-project routing.
- `tools/test-projects.*.txt` -- owned project inventories for unit, integration, and benchmark lanes.
- `.github/workflows/ci.yml` / `.github/workflows/nightly.yml` -- CI and nightly test expectations.
- `tests/Hexalith.Memories.*` -- root-owned test projects to validate and fix.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.Memories.slnx` -- restore/build Release as the repo-wide compilation gate.
- [x] `tests/Hexalith.Memories.*` -- run every root-owned test project/category and capture failures.
- [x] `src/**` / `tests/**` -- fix minimal issues proven by failing tests.
- [x] `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md` -- update verification notes as needed.

**Acceptance Criteria:**
- Given the current root working tree, when all supported root-owned test lanes are executed, then every runnable lane passes or has an exact environmental blocker documented.
- Given a failing test caused by repository code, when the fix is applied, then the failing test and its containing lane pass.

## Spec Change Log

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release` -- expected: restore succeeds.
- `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore` -- expected: build succeeds with warnings as errors.
- `bash ./tools/test.sh --filter "Category!=Integration" --configuration Release` -- expected: unit/contract lane succeeds.
- `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --configuration Release` -- expected: Web tests succeed.
- `bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release` -- expected: fast integration lane succeeds or reports an environment blocker.
- `bash ./tools/test.sh --filter "Category=IntegrationSlow" --configuration Release` -- expected: slow integration lane succeeds or reports an environment blocker.
- `bash ./tools/test.sh --filter "Category=Benchmark" --configuration Release` -- expected: benchmark lane succeeds or reports an environment blocker.
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --no-build --filter "Category=Performance"` -- expected: performance smoke tests succeed.
- `python3 -m unittest discover -s tests/tooling/<folder> -p "*_test.py"` -- expected: release, publish, and story-scope tooling tests succeed.
- `pwsh -NoLogo -NoProfile -File ./tools/test-release.ps1` -- expected: release-blocking non-benchmark lane succeeds.

## Suggested Review Order

**Package Resolution**

- Central pins align Fluent UI and NSubstitute with current build/test expectations.
  [`Directory.Packages.props:39`](../../Directory.Packages.props#L39)

- Web RCL resolves IdentityModel assemblies at the shared FrontComposer-compatible version.
  [`Hexalith.Memories.Web.csproj:20`](../../src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj#L20)

- Governance test now accepts central `Update` pins and the current Fluent RC.
  [`Epic17ConformanceHardeningTests.cs:31`](../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs#L31)

**Workflow Governance**

- CI exposes the direct unit/contract lane required by workflow tests.
  [`ci.yml:197`](../../.github/workflows/ci.yml#L197)

- CI exposes the fast integration lane and coverage evidence gate.
  [`ci.yml:251`](../../.github/workflows/ci.yml#L251)

- Release restores the guarded preflight and package-validation flow.
  [`release.yml:67`](../../.github/workflows/release.yml#L67)

- Nightly action references use governance-accepted major tags.
  [`nightly.yml:23`](../../.github/workflows/nightly.yml#L23)

**Tooling Robustness**

- Release preflight writes stable raw stderr for parser-sensitive tests.
  [`release-preflight.ps1:190`](../../tools/release-preflight.ps1#L190)

- Publish test fixtures now match the canonical package inventory.
  [`publish_nuget_test.py:15`](../../tests/tooling/publish_nuget/publish_nuget_test.py#L15)

- Fake dotnet delegates PowerShell shim execution back to the real runtime.
  [`publish_nuget_test.py:55`](../../tests/tooling/publish_nuget/publish_nuget_test.py#L55)
