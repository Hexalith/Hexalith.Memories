---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.7: Tenant-Configured Vector Dimension Verification

Status: backlog

Owner: Winston / Architect and Developer

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
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_UsesRequestedTenantConfigurationOnly`, `TenantIsolationVerifierTests.VerifyAsync_EqualIndexDimensionsDifferentFromTenantConfig_ReturnsFailed`, and `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`.
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
- **Result:** pending — this is a backlog evidence contract; tenant-A-only configuration lookup and denial-before-dependency must execute and pass before `done`.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Dimension authority negatives | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Includes passing `VerifyAsync_EqualIndexDimensionsDifferentFromTenantConfig_ReturnsFailed`, `VerifyAsync_UsesRequestedTenantConfigurationOnly`, and `VerifyAsync_EmbeddingConfigUnavailable_FailsClosed`. | pending |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Unauthorized tenant verification is denied before config and backend access where applicable. | pending |
