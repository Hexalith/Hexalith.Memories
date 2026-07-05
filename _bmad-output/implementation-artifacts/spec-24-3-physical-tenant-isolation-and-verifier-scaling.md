---
title: '24.3 Physical Tenant Isolation & Verifier Scaling'
type: 'feature'
created: '2026-07-05'
status: 'done'
baseline_revision: 'e2c49e29ebf732231367b6dab75af9fc95d48b4e'
final_revision: '9e1b194a15023653f9a3f67f54cdbc2f0de88320'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Tenant isolation is documented as physical, but Redis still depends on shared-instance tenant prefixes and the verifier burns O(active tenants * indexed documents) doing pairwise deep `FT.SEARCH *` scans. The verifier also runs a static tenant-id validation self-test on every tenant verification, which inflates trust evidence without checking tenant storage.

**Approach:** Ratify the physical strategy in `architecture.md`: Redis enforcement moves toward per-tenant ACL users plus tenant-scoped backend resolution, while RediSearch/vector indexes and FalkorDB graph databases remain tenant-scoped lifecycle resources. Refactor the verifier to structural and cursor/metadata checks for the target tenant, remove the runtime input-validation self-test, and preserve the existing verification response contract.

## Boundaries & Constraints

**Always:** Preserve NFR8 zero cross-tenant leakage semantics; keep verifier behavior diagnostic rather than enforcement; keep tenant IDs explicit in storage, search, graph, telemetry, and API contracts; avoid unbounded peer fan-out or full-index deep pagination; use xUnit v3, Shouldly, and existing contracts.

**Block If:** Implementation proves the ratified Redis ACL strategy cannot be truthfully supported by the pinned Redis Stack capability or current deployment model.

**Never:** Do not implement the full tenant-scoped Redis ACL rollout in this story; do not migrate existing tenant data, rewrite provisioning into a new backend abstraction, change public JSON contract shape, or weaken endpoint tenant authorization/status checks.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Verify healthy tenant | Active target tenant with expected syntactic, raw semantic, NL semantic indexes and FalkorDB graph | `TenantIsolationVerificationResult.AllPassed` remains true and checks describe structural/cursor verification | No error expected |
| Many peer tenants | Target tenant plus many active peers | Verifier does not issue pairwise foreign-prefix `FT.SEARCH * LIMIT offset` scans per peer | No error expected |
| Planted tenant mismatch | Key under target tenant prefix has a different stored `tenantId` | Syntactic or semantic isolation check fails with actionable details | Return failed check, not exception |
| Malformed tenant id | `POST /api/tenants/bad_id/verify` | Endpoint still returns 400 before verifier runs; result no longer contains `InputValidation` | Existing `ErrorResponse` behavior preserved |
| Missing graph/index/backend | Missing target graph/index, orphan graph, or Redis/FalkorDB unavailable | Existing failed-check semantics and 503 endpoint mapping are preserved | Return structured failed check or existing endpoint error |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- main verifier; remove peerwise deep scans and runtime input validation self-test.
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` -- canonical index/key/alias names and FT.INFO helpers used by structural checks.
- `src/Hexalith.Memories.Server/Program.cs` -- verifier registration and `/api/tenants/{tenantId}/verify` endpoint; preserve endpoint contract.
- `_bmad-output/planning-artifacts/architecture.md` -- authoritative physical isolation decision and Gate 2 wording.
- `docs/dev/health-checks.md` -- adjust tenant-verifier note only if existing health-check wording contradicts the scalable verifier.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` -- focused unit coverage for scaled verifier behavior and removed self-test.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- update expected core checks and keep malformed/nonexistent endpoint coverage.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/architecture.md` -- ratify Redis physical isolation as per-tenant ACL users plus tenant-scoped backend resolution, retain per-tenant RediSearch/vector indexes and FalkorDB graph databases, and state hash-tags/logical DBs are placement tools rather than the primary security boundary -- make Story 24.3 decision-first output explicit.
- [x] `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- replace syntactic/semantic pairwise peer loops with target-tenant structural checks: FT.INFO prefix/schema metadata for expected indexes plus cursor scans of target key prefixes for stored `tenantId` mismatches -- remove O(tenants²) deep pagination.
- [x] `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- include natural-language semantic index/prefix in vector isolation verification where available, preserving backend-unavailable and missing-index failure details.
- [x] `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- delete `CheckInputValidationAsync` and remove the `InputValidation` check from verifier output -- endpoint validation and direct guard tests own malformed-id coverage.
- [x] `docs/dev/health-checks.md` -- update only contradictory wording so health checks remain non-tenant probes and tenant isolation evidence points to the scalable verifier.
- [x] `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` -- update expected check set, add regression coverage proving no peer-index `FT.SEARCH *` calls are issued, and prove planted target-prefix tenant-id mismatches fail.
- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- remove `InputValidation` from core check assertions while keeping malformed tenant 400, unknown tenant 404, and planted leakage coverage.
- [x] `_bmad-output/implementation-artifacts/spec-24-3-physical-tenant-isolation-and-verifier-scaling.md` -- record implementation notes, validation results, and any deliberate scope exclusions.

**Acceptance Criteria:**
- Given the architecture previously claimed physical Redis isolation without an enforcement direction, when Story 24.3 completes, then `architecture.md` ratifies per-tenant Redis ACL users with tenant-scoped backend resolution as the target strategy and distinguishes it from prefix-only naming.
- Given many active peer tenants exist, when `TenantIsolationVerifier.VerifyAsync` runs for one tenant, then verifier work is bounded by target tenant storage plus index/graph metadata and does not deep-page every peer index.
- Given a target tenant has syntactic, raw semantic, or natural-language semantic keys whose stored `tenantId` differs from the requested tenant, when verification runs, then the relevant isolation check fails with remediation guidance.
- Given malformed tenant IDs reach the verify endpoint, when endpoint validation rejects them, then the response remains the existing 400 `ErrorResponse` and verifier results no longer include an `InputValidation` self-test.
- Given Redis or FalkorDB is unavailable, when verification runs, then backend-dependent checks still fail closed with existing operator guidance instead of throwing unhandled exceptions.

## Spec Change Log

## Review Triage Log

### 2026-07-05 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 5: (high 2, medium 2, low 1)
- defer: 3: (high 0, medium 3, low 0)
- reject: 2: (high 0, medium 2, low 0)
- addressed_findings:
  - `[high]` `[patch]` Semantic verifier evidence depended on stored `tenantId`, but raw, chunked, and natural-language semantic writers omitted it; added explicit `tenantId` hash fields and focused writer assertions.
  - `[high]` `[patch]` Target-prefix hashes missing `tenantId` passed verification; the verifier now fails missing tenant markers and has regression coverage.
  - `[medium]` `[patch]` Redis target-prefix scans used only the first connected endpoint; scans now visit all connected endpoints with key de-duplication and multi-endpoint coverage.
  - `[medium]` `[patch]` Planted leakage integration coverage could pass for unrelated failures; it now asserts `SyntacticIsolation` failed and names the planted tenant.
  - `[low]` `[patch]` Frontmatter follow-up review recommendation was false despite behavior-affecting review fixes; set it to true.

### 2026-07-05 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 1: (high 0, medium 0, low 1)
- defer: 2: (high 0, medium 2, low 0)
- reject: 6: (high 0, medium 2, low 4)
- addressed_findings:
  - `[low]` `[patch]` Removed dead `TenantIdGuard.ReservedNames` property orphaned by the Story 24.3 removal of the `InputValidation` self-test; grep confirmed no remaining references in `src/` or `tests/`; server build and focused test build green (0 warnings), TenantIsolationVerifierTests (16) and Activities.Indexing (82) pass.

## Design Notes

The story is decision-first but not documentation-only. The decision ratifies the target physical boundary; code work makes the verifier stop producing expensive or misleading evidence. Full ACL user provisioning, tenant-scoped connection migration, and data migration remain follow-up enforcement work.

## Verification

**Commands:**
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` -- expected: server builds with warnings as errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- expected: focused test project builds.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~TenantIsolationVerifierTests|FullyQualifiedName~IndexSemanticActivityTests|FullyQualifiedName~IndexSemanticChunksActivityTests|FullyQualifiedName~IndexNaturalLanguageSemanticActivityTests"` -- expected: verifier and semantic-writer focused unit tests pass.
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~TenantIsolationIntegrationTests"` -- expected: run if Aspire/Docker prerequisites are available; otherwise record exact blocker.
- `git diff --check` -- expected: no whitespace errors.

## Implementation Notes

- Ratified Story 24.3 in `architecture.md`: Redis target boundary is per-tenant ACL users plus tenant-scoped backend resolution; tenant indexes/databases remain lifecycle resources; prefixes/hash tags/logical DBs are placement tools only.
- Refactored `TenantIsolationVerifier` to use target FT.INFO metadata checks and target-prefix cursor scans for syntactic, raw semantic, and natural-language semantic storage. Peer `FT.SEARCH *` deep paging and `InputValidation` runtime self-test were removed.
- Added tenant marker writes to raw semantic, semantic chunk, and natural-language semantic hashes so verifier target-prefix evidence is backed by explicit stored tenant identity.
- Hardened verifier target-prefix scans to fail missing tenant markers and to scan all connected Redis endpoints with de-duplication.
- Reviewed `docs/dev/health-checks.md`; no contradictory health-check wording was present, so no doc edit was needed.

## Validation Results

- PASS: `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`.
- PASS: `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`.
- PASS: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter "FullyQualifiedName~TenantIsolationVerifierTests|FullyQualifiedName~IndexSemanticActivityTests|FullyQualifiedName~IndexSemanticChunksActivityTests|FullyQualifiedName~IndexNaturalLanguageSemanticActivityTests"` -- 35 passed.
- BLOCKED: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` fails in `src/Hexalith.Memories.AppHost/Program.cs(11,16)` with CS0234: namespace `Hexalith.EventStore` does not exist. The prior `--no-build` integration run used a stale test assembly and returned 401 Unauthorized, so current integration source could not be validated.
- PASS: `git diff --check`.

## Auto Run Result

Status: done

Summary:
- Ratified Story 24.3 physical tenant isolation direction in architecture: Redis moves toward per-tenant ACL users plus tenant-scoped backend resolution, while tenant RediSearch/vector indexes and FalkorDB graph databases remain lifecycle resources.
- Reworked `TenantIsolationVerifier` from peer deep scans to target metadata/cursor evidence, removed the runtime `InputValidation` self-test, and hardened target-prefix hash checks.
- Added explicit semantic `tenantId` hash writes so verifier evidence can fail closed for raw semantic, chunked semantic, and natural-language semantic storage.

Files changed:
- `_bmad-output/planning-artifacts/architecture.md` -- Story 24.3 decision and Gate 2 wording.
- `_bmad-output/implementation-artifacts/epic-24-context.md` -- compiled Epic 24 context used by this run.
- `_bmad-output/implementation-artifacts/spec-24-3-physical-tenant-isolation-and-verifier-scaling.md` -- spec, triage, validation, and run result.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review-deferred follow-up entries.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` -- scalable target verification.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` -- semantic hash `tenantId`.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticChunksActivity.cs` -- semantic chunk hash `tenantId`.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs` -- NL semantic hash `tenantId`.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` -- verifier behavior/regression coverage.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs` -- raw semantic tenant marker assertion.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs` -- chunk tenant marker assertions.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs` -- NL semantic tenant marker assertion.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs` -- planted leakage assertion tightened to the intended check.

Review findings breakdown:
- Patches applied: 5 total, including semantic tenant markers, missing-marker fail-closed behavior, multi-endpoint Redis scans, planted-leakage assertion specificity, and follow-up review frontmatter.
- Items deferred: 3 follow-ups for graph content isolation evidence, integration tests whose names exceed their actual leakage scenarios, and tenant-config vector dimension checks.
- Items rejected: 2 findings where the review asked for out-of-scope ACL enforcement evidence or for removing explicitly preserved orphan graph registry checks from this story.
- Follow-up review recommendation: true, because the review fixes changed data evidence semantics in tenant isolation code and integration validation remains blocked by the AppHost EventStore build issue.

Verification performed:
- PASS: server build.
- PASS: server test project build.
- PASS: focused server tests, 35 passed.
- BLOCKED: integration test project build, AppHost CS0234 `Hexalith.EventStore` namespace/reference issue.
- PASS: `git diff --check`.

Residual risks:
- Integration test source could not be built or run until the AppHost EventStore reference issue is fixed.
- Graph content-level leakage checks and tenant-config vector dimension checks remain deferred follow-up work, not Story 24.3 closure scope.

### Follow-up Review Pass (2026-07-05)

Independent adversarial (Blind Hunter) + edge-case (Edge Case Hunter) review of `e2c49e2..d709adc`. Key conclusion: **no cross-tenant leakage-escape (fail-open) regression** — the new FT.INFO single-prefix metadata check plus target-prefix cursor scan is at least as strong as the old foreign-prefix `FT.SEARCH` for detecting foreign keys, and fails closed on parse failure, unknown index, and no-connected-server. All findings were false-positive robustness or evidence-quality nuances.

- Patch applied (1): removed dead `TenantIdGuard.ReservedNames` property orphaned by the `InputValidation` self-test removal.
- Deferred (2 new ledger entries): (a) semantic prefix scans over-capture staging (`:vec:staging:`/`:vecnl:staging:`) and legacy-NL (`:vec:nl:`) key families lacking the `tenantId` marker → possible false `SemanticIsolation` failure during a migration window or with legacy data; (b) decide missing-marker diagnostic semantics + non-destructive remediation given the never-migrate-data constraint.
- Rejected (6): `num_docs`-vs-scanned-count reconciliation (a naive `M==N` assert is harmful because chunks/staging/un-indexed keys legitimately diverge); NL-index-treated-as-mandatory (matches the provisioning contract — `ProvisionRedisVectorActivity` always creates the NL index); schema-conformance folded into isolation verdict (defensible — the single-prefix check is isolation-relevant); `IsUnknownIndex` English substring match (fails closed; no error code available); multi-endpoint SCAN on replicas (deliberate cluster-coverage tradeoff with ordinal de-dup); `HashGetAsync` not passed `ct` (StackExchange.Redis async methods take no `CancellationToken`).

Verification: server build PASS (0 warnings); server test-project build PASS; `TenantIsolationVerifierTests` 16 passed; `Activities.Indexing` tests 82 passed; `git diff --check` clean. Integration lane remains BLOCKED by the pre-existing AppHost EventStore CS0234 build issue, unchanged by this pass.

Follow-up review recommended: false — this pass made only a trivial dead-code removal; substantive findings were deferred, not changed.
