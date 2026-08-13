---
title: 'Story 24.7: Tenant-Configured Vector Dimension Verification'
type: 'feature'
created: '2026-08-13'
status: 'done'
review_loop_iteration: 0
baseline_commit: '8feb2a2dff986c037de2a0875d00eb9aa32705bb'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Semantic isolation currently compares the raw and natural-language index dimensions only with each other, so an equally wrong pair can pass verification for the requested tenant.

**Approach:** Make the requested tenant's validated embedding configuration authoritative, compare each semantic index independently with it, and preserve index-to-index agreement as a secondary fail-closed assertion.

## Boundaries & Constraints

**Always:** Resolve configuration once for the exact requested tenant; preserve existing prefix, schema, marker, and backend checks; report failures through the existing `SemanticIsolation` result with expected/actual dimensions and tenant-scoped operator guidance; preserve cancellation and read-only verification.

**Ask First:** Any public response-shape change, provider/actor fallback-policy change, lifecycle or authorization behavior change, or implementation that requires files outside the mapped verifier, composition, tests, and lifecycle records.

**Never:** Let raw-versus-natural-language equality replace configuration authority; use another tenant's configuration; swallow cancellation; create, alter, migrate, invalidate, reindex, or delete tenant infrastructure; add Story 24.8 key-family or Story 24.9 marker-remediation scope.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| All agree | Config, raw, and NL dimensions are 768 | `SemanticIsolation` passes | Existing checks still govern the aggregate result |
| One index wrong | Config/raw are 768; NL is 1536, or the reverse | The independently wrong index fails | Name expected and actual dimensions |
| Equally wrong pair | Config is 768; raw and NL are 1536 | Verification fails | Give tenant-scoped reindex guidance |
| Cross-tenant values differ | Tenant A is requested; tenant B has another value | Only tenant A can affect the result | No tenant B lookup or fallback |
| Configuration unavailable | Provider throws a recognized backend failure | `SemanticIsolation` fails without an endpoint exception | Actionable configuration/backend retry guidance |
| Configuration invalid | Provider returns zero, negative, or otherwise invalid dimensions | Equality cannot rescue the check | Fail closed; perform no mutation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:20` -- inject `ITenantEmbeddingConfigProvider`; `CheckSemanticIsolationAsync` at line 214 owns the single tenant lookup and three-way dimension evidence while retaining current structural checks.
- `src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs:10` -- reuse `GetAsync(tenantId, cancellationToken)` unchanged; `TenantEmbeddingConfigProvider.cs:47` confirms ordinal tenant-keyed caching and actor IDs.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:196` -- reuse established configuration validation; do not invent a weaker validity policy.
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:424` -- reuse expected-versus-actual dimension wording and existing `FT.INFO` parsing; read-only.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:365` -- pass the existing singleton configuration provider into the verifier factory.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:513` -- extend the verifier fixture with a valid default provider and cover agreement, one/equally wrong indexes, requested-tenant-only lookup, unavailable/invalid configuration, cancellation, and no mutation.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs:75` -- add method-correct POST `/verify` denial-before-dependency evidence; a GET theory row is insufficient.
- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md` and `sprint-status.yaml` -- synchronize lifecycle, exact evidence, and complete File List; retain the story's human-owned intent.

## Tasks & Acceptance

**Execution:**
- [x] `TenantIsolationVerifier.cs` -- obtain and validate the requested tenant's configuration once, convert recognized provider failures into a failed check, and compare both indexes independently plus secondarily to each other.
- [x] `MemoriesServerServiceCollectionExtensions.cs` -- inject the already-registered provider into the singleton verifier.
- [x] `TenantIsolationVerifierTests.cs` -- implement every matrix row with valid default fixtures, exact tenant-call assertions, cancellation propagation, and read-only dependency assertions.
- [x] `ServerEndpointAuthorizationTests.cs` -- prove mismatched-tenant POST verification is forbidden before Dapr/actor, Redis, or FalkorDB access.
- [x] Story and sprint artifacts -- adopt the current implementation baseline, record phase/file deltas, replace pending tenant evidence with executed results, and reconcile review readiness.

**Acceptance Criteria:**
- Given raw and natural-language `FT.INFO` dimensions and the requested tenant configuration, when verification runs, then it passes only when all three authoritative values are present, valid, and equal.
- Given any index/configuration mismatch or unavailable/invalid requested-tenant configuration, when verification runs, then `SemanticIsolation` fails closed with bounded actionable evidence and no mutation or fallback.
- Given tenant A and B have different configurations, when tenant A is verified, then only tenant A is queried and tenant B cannot satisfy or fail the check.
- Given a principal scoped to tenant A posts to tenant B's verify route, when authorization runs, then access is denied before configuration or backend dependencies execute.

## Spec Change Log

- 2026-08-13: Implemented all tasks at baseline `8feb2a2dff986c037de2a0875d00eb9aa32705bb`; the required Debug build succeeded with zero warnings/errors and the focused three-class lane passed 64/64 with zero skips.
- 2026-08-13: Applied code-review hardening for null safety, verifier-boundary cancellation, provider-side cancellation, Dapr actor invocation failures, and sanitized validation evidence; added five test cases and restored the Epic 24 context exactly to baseline. The Debug build remained clean and the focused three-class lane passed 69/69 with zero skips.

## Design Notes

Validate the full returned embedding configuration so the verifier agrees with live ingestion/search policy. Catch only established provider-unavailable failures, keep validation failures explicit, and rethrow cancellation. The actor currently masks corrupt persisted state with defaults; this slice verifies the provider result and must not overclaim detection behind that boundary.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: zero warnings and errors after any audit-only fallback is recorded separately.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests` -- expected: all focused verifier, cross-tenant denial, and provider-isolation tests pass with zero skips.
- `python3 tools/check-story-review-readiness.py --story-key 24-7-tenant-configured-vector-dimension-verification --changed-files-file <({ git diff --name-only HEAD; git ls-files --others --exclude-standard; } | sort -u) --derive-cumulative` -- expected: review-readiness gate passes on the reconciled cumulative file set.

**Executed results (2026-08-13):** Debug build passed in 11.15 seconds with 0 warnings and 0 errors; the focused verifier, authorization, and provider lane passed 69 tests in 6.126 seconds with 0 failures and 0 skips. The exact review-readiness command above passed on the reconciled seven-path file with final line `Story review readiness validation passed.`; C1 reported its documented default-branch skip and remains enforced on a PR diff. The story-slice-scope and tenant-isolation-evidence gates passed, and `git diff --check` reported no whitespace errors.

## Suggested Review Order

**Tenant-authoritative verification**

- Start with the requested-tenant lookup, validation, and independent dimension comparisons.
  [`TenantIsolationVerifier.cs:222`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L222)

- Review bounded fail-closed classification and sanitized validation evidence.
  [`TenantIsolationVerifier.cs:607`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L607)

- Confirm composition reuses the existing tenant-scoped configuration provider.
  [`MemoriesServerServiceCollectionExtensions.cs:365`](../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs#L365)

**Tenant isolation evidence**

- Inspect equally wrong indexes and requested-tenant-only configuration proof.
  [`TenantIsolationVerifierTests.cs:116`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L116)

- Inspect null, backend-failure, validation, and genuine pending-task cancellation coverage.
  [`TenantIsolationVerifierTests.cs:228`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L228)

- Confirm mismatched-tenant POST verification is denied before every dependency.
  [`ServerEndpointAuthorizationTests.cs:104`](../../tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs#L104)

**Lifecycle and evidence**

- Finish with command-backed cross-tenant evidence and append-only phase accounting.
  [`24-7-tenant-configured-vector-dimension-verification.md:72`](24-7-tenant-configured-vector-dimension-verification.md#L72)
