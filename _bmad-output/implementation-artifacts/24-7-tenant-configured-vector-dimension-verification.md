---
baseline_commit: 8feb2a2dff986c037de2a0875d00eb9aa32705bb
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.7: Tenant-Configured Vector Dimension Verification

Status: review

Owner: Winston / Architect and Developer

Implementation source: [Story 24.7 implementation spec](spec-24-7-tenant-configured-vector-dimension-verification.md).
The approved requirements and history remain canonical here; implementation tasks and command-backed
execution evidence are maintained in the linked spec and reconciled below.

## Story

As an operator,
I want semantic index dimensions checked against the requested tenant's embedding configuration,
so that two equally wrong indexes cannot pass isolation verification.

## Acceptance Criteria

1. Given the requested tenant's configured dimensions and `FT.INFO` for raw and natural-language semantic indexes, when verification runs, then each index is compared independently with the tenant configuration and the check passes only when all three values agree.
2. Given both indexes report the same dimension but the requested tenant's configuration reports a different dimension, when verification runs, then `SemanticIsolation` fails with expected and actual dimensions plus tenant-scoped reindex guidance.
3. Given tenant A and tenant B have different configured dimensions, when tenant A is verified, then `ITenantEmbeddingConfigProvider.GetAsync` is called only for tenant A and tenant B's value cannot satisfy or fail tenant A's check.
4. Given the tenant configuration source is unavailable or returns invalid dimensions, when verification runs, then it fails closed with actionable backend/configuration guidance and does not fall back to raw-versus-NL equality. This story does not recreate indexes or run migration.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 24.3 | `historical-reference-only` | Preserve FT.INFO parsing and the fail-closed result contract; do not treat pair equality as sufficient. |
| `ITenantEmbeddingConfigProvider` | `current-narrow-pattern` | Reuse the requested-tenant cached configuration source already consumed by search and tenant endpoints. |
| Raw-versus-NL equality alone | `anti-template` | Retain only as a secondary consistency assertion; never use it as dimension authority. |
| Story 20.2 | `current-narrow-pattern` | Re-run denial-before-dependency evidence for the tenant-scoped verifier surface. |

## Slice Proof

- One independently demonstrable outcome: both semantic index families match the requested tenant's configured dimension.
- Demonstration boundary: focused verifier tests cover correct, equally wrong, one wrong, cross-tenant configuration, unavailable source, and invalid source cases.
- Excluded: provider migration, index recreation, marker scanning, graph evidence, and ACL routing.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md` — UPDATE lifecycle, evidence, phase ledger, scope, and File List.
- `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md` — ADD implementation contract, verification record, and review trail.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATE only through workflow status synchronization.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` — UPDATE verifier composition with the existing configuration provider.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` — UPDATE tenant-authoritative dimension verification and fail-closed handling.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` — UPDATE method-correct denial-before-dependency evidence.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` — UPDATE dimension, tenant, failure, cancellation, and read-only coverage.

## Dev Notes

Inject or otherwise consume `ITenantEmbeddingConfigProvider` at the verifier boundary. The configured dimension is authoritative for the requested tenant; raw-versus-NL equality remains a secondary consistency check. Configuration lookup must fail closed and must not trigger tenant lifecycle or reindex behavior.

Affected tenant-sensitive surfaces are `TenantIsolationVerifier`, its dependency registration, `ITenantEmbeddingConfigProvider`, semantic `FT.INFO` evidence, and the tenant verification endpoint. Completion must cite Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed verifier/tenant-marker evidence. Planned results are `pending` and block `done` until executed.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| Story 24.3 compares raw and natural-language dimensions but an equally wrong pair can pass. | implementation | `rg -n 'rawDimensions|naturalLanguageDimensions' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | Current verifier compares the two index values with each other only. | confirmed |
| A requested-tenant embedding configuration source already exists. | capability | `rg -n 'interface ITenantEmbeddingConfigProvider|GetAsync\\(' src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs` | The provider exposes tenant-scoped `GetAsync`. | confirmed |
| Tenant configuration provider registration exists but the verifier does not consume it. | composition | `rg -n 'ITenantEmbeddingConfigProvider|TenantIsolationVerifier' src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | The provider is registered; the verifier has no provider dependency. | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** Tenant verification endpoint, `TenantIsolationVerifier`, requested-tenant embedding configuration lookup, and semantic index metadata.
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_UsesRequestedTenantConfigurationOnly`, `TenantIsolationVerifierTests.VerifyAsync_EqualIndexDimensionsDifferentFromTenantConfig_ReturnsFailed`, and `ServerEndpointAuthorizationTests.TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`.
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests`.
- **Result:** passed 2026-08-13 — 69 test cases passed with zero failures or skips; tenant A was the sole configuration lookup, equally wrong indexes failed, null/unavailable/invalid provider responses failed closed with sanitized evidence, caller cancellation interrupted a genuinely pending provider task, and the tenant-B POST verification route was forbidden before Dapr/actor, Redis, or FalkorDB access.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Dimension authority negatives | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests` | Includes passing `VerifyAsync_EqualIndexDimensionsDifferentFromTenantConfig_ReturnsFailed`, `VerifyAsync_UsesRequestedTenantConfigurationOnly`, and `VerifyAsync_EmbeddingConfigUnavailable_FailsClosed`. | passed 2026-08-13 — combined required lane 69/69, zero failures/skips |
| Story 20.2 denial-before-dependency | Same combined command above; method-correct proof is `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`. | Unauthorized tenant verification is denied before config and backend access where applicable. | passed 2026-08-13 — POST returned `TENANT_FORBIDDEN` with all Dapr/actor, Redis, and FalkorDB call ledgers empty |

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-08-13 | create-story | Adopted the canonical phase ledger before implementation, linked the implementation spec, aligned the story status and baseline to the current implementation handoff, and preserved the approved Story and Acceptance Criteria unchanged. Owner: Amelia / Developer. Earlier deltas are not reconstructed; this runner-observed point is the story baseline. | Phase delta **+0 test cases**; cumulative story delta **+0**. Baseline discovery: **52 test cases**, 0 failed, 0 skipped. Command: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false`, then `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests`. | `matched 4/4` against baseline `8feb2a2dff986c037de2a0875d00eb9aa32705bb`: the story record, implementation spec, epic context refresh, and sprint-status transition are the complete pre-implementation story-scoped worktree set. No exclusions. |
| 2026-08-13 | dev-story | Made the requested tenant's fully validated embedding configuration authoritative for both semantic index dimensions, preserved the independent raw-versus-NL assertion and existing structural checks, converted only established configuration-backend failures into a failed `SemanticIsolation` result, preserved cancellation, wired the existing singleton provider, and added matrix plus POST denial-before-dependency evidence. | Phase delta **+12 test cases**; cumulative story delta **+12**. The comparable three-class xUnit lane increased **52 -> 64 test cases** and passed with 0 failures/skips in 5.799 seconds. No external same-lane delta. Exact build and runner commands are the commands recorded in the create baseline and Planned Verification rows. | `matched 8/8` against baseline `8feb2a2dff986c037de2a0875d00eb9aa32705bb` using `{ git diff --name-only HEAD; git ls-files --others --exclude-standard; } | sort -u`: all eight cumulative story paths are declared below. No exclusions. |
| 2026-08-13 | code-review | Guarded the injected provider and null lookup results, enforced caller cancellation over a genuinely pending provider task with `WaitAsync`, classified provider-side cancellation and `ActorMethodInvocationException` as fail-closed configuration unavailability, and replaced validation exception text with bounded field-level evidence while retaining tenant-scoped correction/retry guidance. Restored the Epic 24 context exactly to baseline so it is absent from the cumulative diff. | Phase delta **+5 test cases**; cumulative story delta **+17**. The comparable three-class xUnit lane increased **64 -> 69 test cases** during review (**52 -> 69** cumulative) and passed with 0 failures/skips in 6.126 seconds. Exact commands/results: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` — succeeded in 11.15 seconds with 0 warnings/errors; `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests` — 69/69 passed, 0 failed/skipped. | `matched 7/7` against baseline `8feb2a2dff986c037de2a0875d00eb9aa32705bb` using `{ git diff --name-only HEAD; git ls-files --others --exclude-standard; } | sort -u`: all seven cumulative story paths are declared below; restored `_bmad-output/implementation-artifacts/epic-24-context.md` is absent. No exclusions. |

## File List

- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md`
- `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
