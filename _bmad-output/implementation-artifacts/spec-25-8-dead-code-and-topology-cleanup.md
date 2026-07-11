---
title: 'Story 25.8: Dead-Code & Topology Cleanup'
type: 'refactor'
created: '2026-07-11'
status: 'done'
baseline_revision: '169587537c41973b438bf9dbaaa844050406fb0b'
final_revision: '39229fbf09ecc76c6bca90ebee0f952f08bec843'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
warnings: [oversized, multiple-goals]
---

<intent-contract>

## Intent

**Problem:** The repository retains proven-unused implementation members and generated project caches, while backend and release dependencies are obscured by a compatibility package and a packable MCP project currently emits a dependency on non-published `Hexalith.Memories.ServiceDefaults`.

**Approach:** Delete only non-public or non-packaged dead code, migrate in-repo FalkorDB callers to the native selected-graph API with explicit package dependencies, retain the published Redis compatibility surface with accurate documentation, and make CI/release inventories fail closed on topology drift.

## Boundaries & Constraints

**Always:** Preserve REST, MCP, CLI, DAPR, graph-query, telemetry, UI, package IDs, and JSON behavior; keep `Hexalith.Memories.Redis` and its public compatibility symbols available; keep backend dependency versions centralized; run test projects individually; keep generated artifacts out of source control.

**Block If:** Native NFalkorDB selected-graph calls do not preserve query parameters, flags, timeout, or result behavior; ServiceDefaults cannot be package-only consumed by the packed MCP package; or a deletion has a production call site outside tests.

**Never:** Remove a published package or public type such as `ILogRecordCollector`; move backend implementations across projects; edit submodules; broaden into API versioning, feature behavior, or Aspire runtime-topology redesign.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| FalkorDB query | Graph id, query, optional parameters/flags/timeout | Native `SelectGraph(graphId).QueryAsync(...)` returns the same result and observes the same timeout | Existing caller-specific handling remains unchanged |
| MCP package | Release pack of all approved projects | MCP nuspec depends only on approved same-version Hexalith.Memories packages, including ServiceDefaults | Validation fails with the unexpected internal dependency named |
| CI inventory | Docker-free test-project inventory | Web tests execute with the other unit/contract assemblies | Missing or duplicate inventory entries fail the guard |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Redis/` -- published compatibility shim whose current role must be explicit, not mistaken for the backend implementation layer.
- `src/Hexalith.Memories.Server/{Graph,Activities,Cases,Consistency,Endpoints,Export,Search,Tenants,Migrations}/` -- native FalkorDB callers and proven-unused internal members.
- `src/Hexalith.Memories.ServiceDefaults/` and `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` -- currently invalid packable-to-nonpackable dependency edge.
- `tools/{release-packages.json,validate-release-packages.ps1,test-projects.unit-contract.txt}` -- canonical release and Docker-free test inventories.
- `tests/Hexalith.Memories.{Server.Tests,IntegrationTests,Benchmarks,Cli.Tests}/` and `tests/tooling/` -- behavior and topology guards.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Memories.Server/Graph/{IGraphQueryBuilder,GraphQueryBuilder}.cs`, `Endpoints/ErrorResults.cs`, `Consistency/NaturalLanguageConsistencyState.cs`, `Infrastructure/IndexSchemaDefinitions.cs`, `NaturalLanguage/NaturalLanguageIntegrationLog.cs`, and `src/Hexalith.Memories.Aspire/RepositoryProjectPaths.cs` -- remove the obsolete one-argument stub builder and the verified-unused internal helpers/log declarations; update focused tests to retain only deterministic explicit-timestamp behavior.
- `src/`, `tests/`, `tools/`, and `.gitignore` -- delete all tracked `*.csproj.lscache` files and ignore future regenerated caches; add a source-control guard proving none are tracked.
- `src/Hexalith.Memories.Server/`, `tests/Hexalith.Memories.{Benchmarks,IntegrationTests}/`, and their `.csproj` files -- replace compatibility-extension calls with `SelectGraph(...).QueryAsync(...)`, declare directly used `NFalkorDB`, `NRedisStack`, and `StackExchange.Redis` packages explicitly, and remove Server/benchmark project references used only for transitive backend packages.
- `src/Hexalith.Memories.Redis/{Hexalith.Memories.Redis.csproj,README.md,FalkorDbCompatibilityExtensions.cs,RedisPlaceholder.cs}` -- retain wire/API compatibility, remove the unused Contracts project edge, mark/document the package and symbols as compatibility-only with the native migration path and breaking-release removal trigger; do not add new adapters or runtime consumers.
- `src/Hexalith.Memories.ServiceDefaults/{Hexalith.Memories.ServiceDefaults.csproj,README.md}`, `tools/release-packages.json`, `CONTRIBUTING.md`, `docs/dev/release-runbook.md`, and `_bmad-output/project-context.md` -- make ServiceDefaults a metadata-complete approved package, add the omitted Aspire row, and align the documented package count/inventory.
- `tools/validate-release-packages.ps1` and its tooling tests -- reject every nuspec dependency beginning `Hexalith.Memories.` unless its package ID is in the approved inventory, then prove MCP resolves ServiceDefaults at the same release version.
- `tools/test-projects.unit-contract.txt` and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- include `Hexalith.Memories.Web.Tests` in the Docker-free blocking lane and fail on stale, missing, or duplicate runnable-project entries.
- `tests/Hexalith.Memories.Server.Tests/`, `tests/Hexalith.Memories.IntegrationTests/Graph/`, and `tests/Hexalith.Memories.Benchmarks/` -- update graph-query coverage for native selection and run the focused dead-code neighbor suites without weakening assertions.

**Acceptance Criteria:**
- Given any production or test FalkorDB query previously resolved through the global compatibility extension, when the affected graph suites run, then graph selection, parameters, timeout behavior, results, and tenant isolation remain unchanged and no in-repo project references `Hexalith.Memories.Redis` for transitive dependencies.
- Given the approved release projects are packed, when package validation inspects nuspecs, then every `Hexalith.Memories.*` dependency is present in the inventory at the release version and MCP has no dependency on an unpublished package.
- Given a pull-request CI run, when the Docker-free inventory executes, then `Hexalith.Memories.Web.Tests` is included and inventory drift or duplicates fail before tests are skipped.
- Given a fresh checkout, when tracked files and the Release solution are inspected, then no `*.lscache` is tracked, the deleted internal symbols are absent, and the solution builds with zero warnings or errors.
- Given an external consumer still references `Hexalith.Memories.Redis`, when it restores the release, then the existing package ID, compatibility extension, and port constants remain available with deprecation guidance.

## Spec Change Log

## Review Triage Log

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 2, medium 6, low 1)
- defer: 0
- reject: 6: (high 0, medium 3, low 3)
- addressed_findings:
  - `[high]` `[patch]` Removed compiler-level obsoletion from the retained Redis compatibility type, extension, and constants so warnings-as-errors consumers remain source-compatible.
  - `[high]` `[patch]` Added a warnings-as-errors compatibility-consumer build covering the legacy extension signature and both published port constants.
  - `[medium]` `[patch]` Added guards that keep Redis free of internal package dependencies and keep Server/Benchmarks off the compatibility dependency facade.
  - `[medium]` `[patch]` Documented both ServiceDefaults registration modes and the keyed Redis/FalkorDB prerequisite for instrumentation.
  - `[medium]` `[patch]` Guarded the scoped NU5104 suppression by rejecting unexpected ServiceDefaults prerelease dependencies in generated packages.
  - `[medium]` `[patch]` Tightened internal dependency version validation to accept only the exact release version or MSBuild's release lower-bound shape, rejecting wider ranges.
  - `[medium]` `[patch]` Rejected duplicate package IDs across the complete generated artifact directory.
  - `[medium]` `[patch]` Aligned current PRD and epic package-topology guidance with the nine-package release inventory and compatibility-only Redis role.
  - `[low]` `[patch]` Made cache/reference verification durable before and after commit and broadened the project-reference scan to every root project file.

### 2026-07-11 — Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 2: (high 0, medium 0, low 2)
- defer: 1
- reject: 8: (high 0, medium 0, low 8)
- addressed_findings:
  - `[low]` `[patch]` Normalized line endings in four previously-pure-CRLF files (`FalkorDbCompatibilityExtensions.cs`, `RedisPlaceholder.cs`, `CiTestInventoryTests.cs`, `tests/tooling/release_packages/release_packages_test.py`) and the new `ServiceDefaults/README.md` that this story had left with mixed CRLF/LF endings, restoring the repository's uniform-CRLF convention (the sibling `publish_nuget_test.py` was left untouched because it was already mixed at baseline — a pre-existing condition, not this story's regression).
  - `[low]` `[patch]` Added a passing-case release-tooling test (`test_service_defaults_allows_whitelisted_prerelease_dependency`) proving `Hexalith.Memories.ServiceDefaults` accepts the whitelisted `OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease dependency, closing the reject-only coverage of the prerelease-whitelist exception so a future typo or removal of the whitelist id fails a test instead of the next release.

## Design Notes

`Hexalith.Memories.Redis` is not deleted in this behavior-preserving story because it is already a published API surface. In-repo code moves to NFalkorDB's native graph selection and explicit dependencies, leaving the package as an honest compatibility-only boundary that can be removed only in an owned breaking release. ServiceDefaults takes the opposite path: MCP already emits it as a NuGet dependency, so publishing and validating that existing dependency closes a broken package graph without duplicating hosting code.

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx && dotnet build Hexalith.Memories.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- Build and invoke the Release xUnit v3 assemblies for `Hexalith.Memories.Server.Tests`, `Hexalith.Memories.IntegrationTests` graph classes, `Hexalith.Memories.Benchmarks`, `Hexalith.Memories.Web.Tests`, and `Hexalith.Memories.Cli.Tests` with `-parallel none -noLogo` -- expected: focused and Docker-free suites pass; Docker-required tests report only established environment skips.
- `pwsh ./tools/validate-release-packages.ps1` plus the release/publish Python tooling tests, followed by packing all approved projects to a temporary directory and validation with `-PackageDirectory` and one version -- expected: inventory, metadata, dependency closure, and version checks pass.
- `tracked=$(git ls-files '*.lscache' | wc -l); deleted=$(git diff --name-only --diff-filter=D -- '*.lscache' | wc -l); test -z "$(find . -path './references' -prune -o -name '*.lscache' -print -quit)" && { test "$tracked" -eq 0 || test "$deleted" -eq "$tracked"; } && ! rg -n 'ProjectReference.*Hexalith.Memories.Redis' src tests tools -g '*.csproj' && git diff --check` -- expected: no worktree caches, every formerly tracked cache is deleted before commit (or none remains tracked after commit), no obsolete dependency edges remain anywhere, and no whitespace errors are present.

## Auto Run Result

Status: done

Summary: Removed proven-unused internal code and 19 generated project caches; migrated all in-repo FalkorDB graph calls to native graph selection with explicit backend dependencies; retained the Redis compatibility package without consumer-breaking warnings; published ServiceDefaults to close MCP package dependency resolution; and hardened release/test topology inventories.

Files changed:
- `.gitignore` and 19 root-owned `*.csproj.lscache` paths under `src/`, `tests/`, and `tools/` -- ignore regenerated language-server caches and delete every tracked instance.
- `CONTRIBUTING.md`, `_bmad-output/project-context.md`, `_bmad-output/planning-artifacts/{prd,epics}.md`, and `docs/dev/release-runbook.md` -- align current nine-package topology, compatibility roles, and release documentation.
- `src/Hexalith.Memories.Aspire/RepositoryProjectPaths.cs` -- remove the superseded path helper.
- `src/Hexalith.Memories.Redis/{FalkorDbCompatibilityExtensions,RedisPlaceholder}.cs`, `.csproj`, and `README.md` -- retain source-compatible published symbols, remove the unused internal project edge, and document the native migration/removal trigger.
- `src/Hexalith.Memories.Server/Graph/{IGraphQueryBuilder,GraphQueryBuilder}.cs`, `Endpoints/ErrorResults.cs`, `Consistency/NaturalLanguageConsistencyState.cs`, `Infrastructure/IndexSchemaDefinitions.cs`, and `NaturalLanguage/NaturalLanguageIntegrationLog.cs` -- remove verified-unused members and the nondeterministic stub-builder overload.
- `src/Hexalith.Memories.Server/{Activities,Cases,Consistency,Endpoints,Export,Graph,Migrations,Search,Tenants}/` and `Hexalith.Memories.Server.csproj` -- migrate FalkorDB calls to `SelectGraph(...).QueryAsync(...)`, preserve timeout/parameter behavior, and declare backend packages directly.
- `src/Hexalith.Memories.ServiceDefaults/{Hexalith.Memories.ServiceDefaults.csproj,README.md}` -- create a metadata-complete package, document both instrumentation modes, and scope the prerelease warning with a removal trigger.
- `tests/Hexalith.Memories.{Benchmarks,IntegrationTests}/` -- migrate graph setup/assertions to the native API, use explicit backend dependencies, and retain fixed-timestamp stub coverage.
- `tests/Hexalith.Memories.Server.Tests/{Activities/Indexing/IndexGraphActivityTests,Graph/GraphQueryBuilderTests}.cs` -- remove obsolete-overload expectations and guard deterministic production usage.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` and `tools/test-projects.unit-contract.txt` -- add Web.Tests to the Docker-free lane and guard inventory, cache, and Redis-facade drift.
- `tools/{release-packages.json,validate-release-packages.ps1}` and `tests/tooling/{release_packages,publish_nuget}/` -- publish ServiceDefaults and fail closed on unapproved, duplicate, wide-range, wrong-version, or unexpected-prerelease package dependencies; compile the Redis compatibility consumer with warnings as errors.
- `_bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md` -- record the executable contract, review triage, verification, and run result.

Review findings breakdown:
- patches applied: 9 (high 2, medium 6, low 1)
- items deferred: 0
- items rejected: 6 (unsupported transitive-package coupling, non-security IVT concern, already-covered actual-pack evidence, sparse-worktree-only cache case, pre-finalization evidence timing, and runtime-topology reading contradicted by Epic 25 context)
- follow-up review recommendation: true

Verification performed:
- `dotnet restore Hexalith.Memories.slnx` and Release solution builds -- passed with 0 warnings/errors before and after review patches.
- Server.Tests -- 2,556 passed, 1 established skip; Web.Tests -- 492/492 passed; Cli.Tests -- 441/441 passed.
- IntegrationTests graph namespace -- 42/42 passed; focused graph/reproducibility benchmark methods -- 2/2 passed.
- Release-package validator/tooling -- 27/27 release-package tests, 8/8 publish tests, and 9/9 release-preflight tests passed.
- `tools/pack-release.ps1 -Version 25.8.0` -- all nine packages packed and package-directory dependency/metadata validation passed, including MCP to ServiceDefaults closure and Redis compatibility constraints.
- Static native-call, project-reference, cache, and whitespace checks -- passed.

Residual risks:
- The full benchmark assembly remains baseline-red at 16/17 because `ThesisValidation_HybridOutperforms80Percent` measures 75% (6/8) against its existing 80% threshold; changed-area benchmark coverage passes.
- `OpenTelemetry.Instrumentation.StackExchangeRedis` remains centrally pinned to a prerelease; ServiceDefaults keeps a scoped NU5104 suppression plus a package validator that rejects any additional prerelease dependency.
- The Aspire baseline reached dashboard/resource startup and then disconnected; `aspire describe` reported no running AppHost, while all static, build, package, focused integration, and Docker-backed graph gates passed.

## Follow-up Review Result (2026-07-11)

Status: done

Independent follow-up review — four parallel reviewers (adversarial, edge-case, verification-gap, intent-alignment) at session model capability. They confirmed the core change is sound: the FalkorDB `SelectGraph(id).QueryAsync(...)` migration is behavior-preserving (NFalkorDB 1.0.6 decompiled — identical flags/timeout/null-parameter/graph-id-guard semantics to the retired compatibility shim), every deleted member is genuinely dead (no live non-test caller), and the package-topology fixes match the nine-package inventory.

Patches applied (2, both low):
- Normalized line endings in `src/Hexalith.Memories.Redis/FalkorDbCompatibilityExtensions.cs`, `src/Hexalith.Memories.Redis/RedisPlaceholder.cs`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/release_packages/release_packages_test.py`, and the new `src/Hexalith.Memories.ServiceDefaults/README.md` — this story had introduced bare-LF lines into files that were uniformly CRLF at baseline; restored uniform CRLF matching the repository's C# and `tests/tooling/` Python conventions. (`tests/tooling/publish_nuget/publish_nuget_test.py` was left as-is: already mixed at baseline, so its endings are a pre-existing condition outside this story's scope.)
- Added `test_service_defaults_allows_whitelisted_prerelease_dependency` to `tests/tooling/release_packages/release_packages_test.py`, verifying the ServiceDefaults prerelease-whitelist exception (`OpenTelemetry.Instrumentation.StackExchangeRedis`) in the passing direction (previously only the reject direction was tested).

Deferred (1): the new release-package topology validation (PowerShell `-PackageDirectory` throws, `release_packages`/`publish_nuget` fixtures, real packed-nuspec closure) runs only post-merge in `release.yml`, never on the PR `ci.yml` lane — pre-existing CI-lane architecture surfaced by this story's added validation; logged as a new Story 25.8 entry in `_bmad-output/implementation-artifacts/deferred-work.md`.

Rejected (8): prerelease release-version false positive and version-less-dependency NRE (both unreachable — `.releaserc.json` has only a `main` branch, so release versions are always stable and real packed-nuspec dependencies always carry versions); no-`-Version` fallback false-pass (both real callers always pass `-Version`); `File.Exists` softening of the tracked-`.lscache` guard (intentional sparse-worktree handling, a previously-rejected concern); git-subprocess stdout/stderr deadlock (theoretical — `git ls-files` stderr is empty); "exactly one package" throws lacking dedicated fixtures (unreachable behind the set-equality assertion); `BackendProjects` guard covering only the negative half (positive half enforced by build + integration); inventory-membership as a proxy for web-tests-execute (execution follows from the Docker-free lane reading the inventory).

Follow-up verification:
- `python3 -m unittest` — new passing-case test plus reject-direction and base MCP-dependency validation tests: 3/3 passed (pwsh 7.6.2).
- Line-ending normalization re-checked: all four files uniform CRLF (bareLF=0); `git diff --check` clean.

Follow-up review recommendation: false — two localized, low-consequence fixes with no behavior/API/security/data impact.
