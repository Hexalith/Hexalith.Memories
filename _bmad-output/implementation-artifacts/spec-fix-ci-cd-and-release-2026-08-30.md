---
title: 'Restore green CI and publish a patch release'
type: 'bugfix'
created: '2026-08-30'
status: 'in-review'
review_loop_iteration: 0
baseline_commit: 'aeae60f8654206d6d23408a9314c9afe743ea439'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run `33304096613` fails before unit execution because PowerShell expands Coverlet filter wildcards into repository directory names. Release run `33304096576` then fails because the newly added `tests/Directory.Packages.props` is mistaken for the repository root by `InstrumentationInventoryTests`; this blocks semantic-release and leaves `main` without a release for the repair.

**Approach:** Preserve native test-runner arguments through an exact process argument list, identify the repository with its solution marker, prove all local gates, then merge the validated fix and let the existing semantic-release workflow publish and verify the patch release.

## Boundaries & Constraints

**Always:** Keep MTP/Coverlet filters, the 76.5% coverage gate, test inventories, release package/image identity, immutable tags, and fail-closed workflow behavior unchanged. Preserve the existing modified `references/Hexalith.FrontComposer` worktree and use the approved fix branch/PR path. Validate the commit and PR title before and after use.

**Ask First:** Any product/runtime or submodule-content change; modification of nightly integration behavior; secret, branch-protection, package inventory, registry, or semantic-release policy changes; deletion/reuse/overwrite of a tag or published artifact; or a current-head nightly failure that requires more than this runner/root-resolution repair.

**Never:** Bypass hooks, commitlint, required checks, branch protection, semantic-release preflight, or publication validation; use `--admin`, `--auto`, `--no-verify`, force-push, manual tags/releases, weakened assertions, ignored failures, or longer timeouts as the repair.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| PowerShell coverage | Filter values resemble filesystem wildcards and matching directories exist | `dotnet` receives each bracketed value byte-for-byte | Any mutation remains a fixture failure |
| Nested package catalog | Test binary walks above `tests/Directory.Packages.props` | Root resolves only at `Hexalith.Memories.slnx`; telemetry doc is read | Missing solution/doc fails with the searched origin |
| Release publication | Validated `fix` commit reaches current `main` | CI succeeds and semantic-release creates the next patch release | Any partial/conflicting publication stops for evidence-driven recovery |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `tools/test.ps1` -- invoke dotnet with an exact ProcessStartInfo argument list.
- `tests/tooling/coverage_gate/test_runner_contract_test.py` -- prove Coverlet filters and child output are forwarded unchanged.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/InstrumentationInventoryTests.cs` -- resolve telemetry docs from the solution-marker root.
- `_bmad-output/implementation-artifacts/spec-fix-ci-cd-and-release-2026-08-30.md` -- this contract.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- record the unrelated embedding-options flake.
- `references/Hexalith.Builds` -- parent gitlink pointer only.
- `references/Hexalith.EventStore` -- parent gitlink pointer only.
- `references/Hexalith.FrontComposer` -- parent gitlink pointer only.
- `references/Hexalith.Tenants` -- parent gitlink pointer only.

## Code Map

- `tools/test.ps1:154-183` -- constructs Coverlet MTP arguments, then currently invokes `dotnet` through PowerShell wildcard expansion; use `ProcessStartInfo.ArgumentList` and inherit console output.
- `tests/tooling/coverage_gate/test_runner_contract_test.py:263-324` -- existing Bash/PowerShell fake-dotnet contract already exposes argument mutation (`artifacts` locally, `references` in CI).
- `tests/Directory.Packages.props:1-10` -- required nested CPM catalog that invalidates the telemetry test's old marker assumption; read-only.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/InstrumentationInventoryTests.cs:91-125` -- prematurely stops at any `Directory.Packages.props`; reuse the solution-marker walk used by sibling contract tests.
- `.github/workflows/ci.yml` -- read-only execution owner for coverage fixtures, unit/contract tests, coverage, integration, deployment, and web gates.
- `.github/workflows/release.yml` and `tools/test-release.ps1` -- read-only release gate and semantic-release entry point.
- `.github/workflows/nightly.yml` -- read-only fast/slow integration and benchmark confirmation; current-main baseline is run `33327358809`.

## Tasks & Acceptance

**Execution:**
- [x] `tools/test.ps1` -- invoke `dotnet` with a non-shell exact argument list while preserving streaming output and exit-code behavior.
- [x] `InstrumentationInventoryTests.cs` -- walk to the `Hexalith.Memories.slnx` root before resolving `docs/dev/telemetry.md`.
- [x] Local verification -- run coverage fixtures, the focused telemetry test, Release build, unit/non-Docker release suite, and relevant workflow inventory tests.
- [ ] GitHub verification and release -- commit with validated `fix(ci)` metadata, push, open and validate a PR, wait for its checks, merge only at the checked head, then wait for all `main` workflows and confirm the semantic-release output and nightly workflow are green.

**Acceptance Criteria:**
- Given matching directories in the working directory, when both test wrappers run the coverage contract, then every include/exclude reaches fake `dotnet` unchanged and all coverage fixtures pass.
- Given the nested test package catalog, when the telemetry inventory test runs from its Release output, then it locates the root document and passes.
- Given the merged fix, when `main` workflows finish, then CI, Release, Commitlint, CodeQL, and dispatched Nightly complete successfully without skipped required evidence.
- Given semantic-release publishes the patch, when remote evidence is inspected, then the tag, GitHub Release, nine NuGet packages, expected container tags, and publication summary agree on one version and source SHA.

## Spec Change Log

## Design Notes

PowerShell wildcard expansion occurs after the current argument array is assembled, so quoting the source string is not a stable native-process boundary. `ProcessStartInfo.ArgumentList` supplies each token directly without changing Coverlet syntax or shell-specific escaping.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` -- expected: 35 tests pass.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -m:1 -p:NuGetAudit=false` plus direct execution of `InstrumentationInventoryTests` -- expected: clean build and 3/3 pass.
- `pwsh -NoLogo -NoProfile -File ./tools/test-release.ps1 -Configuration Release` -- expected: every non-Docker release project passes.
- GitHub PR/main checks and semantic-release/nightly evidence -- expected: all conclusions `success` and one consistent patch release.

**Actual results:**
- Coverage-runner fixtures: 35/35 passed after CRLF normalization, including exact child exit-code and stdout/stderr forwarding.
- Focused telemetry inventory: 3/3 passed from the Release output directory, including decoy-document and missing-marker/document cases; focused project build completed with zero warnings and errors.
- `Hexalith.Memories.slnx` Release build: succeeded with zero warnings and errors.
- Non-Docker release suite after review patches: 4,857 tests completed: 4,856 passed and the explicitly conditional `SubmoduleGuardTests` case skipped because its submodule precondition was not met; the CLI workflow-contract project passed 498/498. An earlier broad run exposed a parallel static-options race in `AddMemoriesServerServices_WithHostEmbeddingProvidersConfig_SeedsCurrentOptionsAndOllama`; the method passed in isolation and both subsequent complete suites passed. That unrelated flake is recorded in the deferred-work ledger.
- Current-main Nightly run `33327358809`: benchmark passed, but the fast lane failed two OpenBao/access-telemetry readiness cases and the slow lane failed five ingestion/restart/performance cases. This is outside the approved runner/root-resolution repair and blocks the Nightly acceptance criterion pending scope direction.
