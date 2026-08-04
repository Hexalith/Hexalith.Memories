---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.9: Non-Destructive Tenant-Marker Diagnostics

Status: backlog

Owner: Winston / Architect, Murat / Test Architect, and Developer

## Story

As an operator,
I want missing and foreign tenant markers reported with distinct, safe recovery guidance,
so that incomplete evidence is not mislabeled as confirmed leakage or remediated by broad deletion.

## Acceptance Criteria

1. Given a proven-active semantic hash has a foreign non-empty `tenantId`, when verification runs, then `SemanticIsolation` fails and `Details` identify a confirmed marker mismatch/possible contamination, the exact key, expected tenant, and observed tenant without exposing payload data.
2. Given a proven-active semantic hash has no `tenantId`, when verification runs, then `SemanticIsolation` remains fail-closed but `Details` classify the result as incomplete evidence, not confirmed cross-tenant leakage.
3. Given either failure, when `Remediation` is returned, then it directs the operator to inspect and quarantine the named key and to run tenant-scoped marker repair or reindex only after provenance verification. It never recommends blanket prefix deletion, and missing versus foreign markers receive different guidance.
4. Given the V1 `TenantIsolationCheckResult` shape, when this story completes, then distinct semantics are pinned through `Details` and `Remediation` without a breaking JSON-contract change. A machine-readable issue taxonomy requires a separate versioned-contract story.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 24.3 | `historical-reference-only` | Preserve fail-closed marker evidence while correcting diagnostic meaning and remediation. |
| `remove mismatched target-prefix hashes` | `anti-template` | Quote only in negative assertions proving broad/destructive guidance is absent. |
| V1 `Details` and `Remediation` fields | `current-narrow-pattern` | Preserve the compatible contract and pin distinct operator wording in tests. |
| Story 24.8 | `current-narrow-pattern` | Consume its proven-active family classification; do not reimplement namespace membership here. |
| Story 20.2 | `current-narrow-pattern` | Re-run denial-before-dependency evidence for the tenant verifier surface. |

## Slice Proof

- One independently demonstrable outcome: safe, distinct marker diagnosis and recovery semantics.
- Demonstration boundary: focused verifier and serialization/endpoint tests cover healthy, missing, and foreign proven-active markers and prove blanket-delete guidance is absent.
- Excluded: executing repair, bulk backfill, changing the V1 JSON shape, family classification owned by Story 24.8, and physical enforcement.

## Dev Notes

Story 24.9 depends on Story 24.8 because diagnostic meaning is valid only after active-family membership is proven. A foreign marker is possible contamination; a missing marker is incomplete evidence. Both fail closed, but neither is permission to delete a prefix. Keep exact keys and marker values payload-safe in `Details`; recovery is named-key inspection/quarantine followed by tenant-scoped repair or reindex after provenance is established.

Affected tenant-sensitive surfaces are `TenantIsolationVerifier`, `TenantIsolationCheckResult`, tenant verification endpoint/serialization, CLI/MCP/operator evidence rendering, and remediation documentation. Completion must retain Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed/tenant-marker evidence. Planned results are `pending` and block `done` until executed.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| Story 24.3 currently treats missing and foreign markers through one mismatch collection. | implementation | `rg -n 'missing tenantId field|remove mismatched target-prefix hashes' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` | Missing markers join mismatch evidence and broad removal guidance is emitted. | confirmed |
| V1 exposes compatible free-text diagnostic and remediation fields. | contract | `sed -n '1,90p' src/Hexalith.Memories.Contracts/V1/TenantIsolationCheckResult.cs` | Nullable `Details` and `Remediation` exist; no machine-readable issue taxonomy exists. | confirmed |
| Story 24.8 must decide active-family membership before marker meaning is assigned. | dependency | `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md` | Story 24.8 is registered as the predecessor and remains backlog. | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** `TenantIsolationVerifier`, V1 tenant-isolation result serialization, tenant verification endpoint, operator evidence rendering, and remediation text.
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance`, `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, and `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`.
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Tenants.TenantEndpointTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
- **Result:** pending — this is a backlog evidence contract; missing, foreign, healthy, contract-compatibility, payload-safety, and denial-before-dependency cases must execute and pass before `done`.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Distinct marker diagnostics | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Healthy passes; missing fails as incomplete evidence; foreign fails as possible contamination; exact named-key guidance is non-destructive. | pending |
| Contract and endpoint compatibility | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Tenants.TenantEndpointTests` | V1 JSON shape is unchanged and new wording is pinned without payload leakage. | pending |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Unauthorized tenant verification is denied before verifier/backend access where applicable. | pending |
