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

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:26` -- `ITenantEmbeddingConfigProvider` field; the constructor at line 37 guards the dependency, `CheckSemanticIsolationAsync` at line 222 owns the single tenant lookup and three-way dimension evidence, and the safe classification-only warning is declared at line 699. Anchors re-derived 2026-08-14 after third-pass patches.
- `src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs:10` -- reuse `GetAsync(tenantId, cancellationToken)` unchanged; `TenantEmbeddingConfigProvider.cs:47` confirms ordinal tenant-keyed caching and actor IDs.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:196` -- reuse established configuration validation; do not invent a weaker validity policy.
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:424` -- reuse expected-versus-actual dimension wording and existing `FT.INFO` parsing; read-only.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:365` -- pass the existing singleton configuration provider into the verifier factory.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:81` -- cover one/equally/three-way dimension mismatches, the valid non-default dimension path at line 277, requested-tenant-only lookup, safe classified provider failures from line 225, cancellation, and no mutation.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs:75` -- add method-correct POST `/verify` denial-before-dependency evidence; a GET theory row is insufficient.
- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md`, `deferred-work.md`, and `sprint-status.yaml` -- synchronize lifecycle, the workflow-generated review deferrals, exact evidence, complete File List, and named external exclusion; retain the story's human-owned intent.

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

### Review Findings

Second-pass adversarial review, 2026-08-14 (six layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor, historical-slice-guard, story-phase-ledger).

- [x] [Review][Patch] Pin non-provider validation field labels in invalid-config evidence [tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:264] — `VerifyAsync_InvalidEmbeddingConfigDimensions_FailsClosed` never asserts `invalid in field 'dimensions'`; only the `provider` arm of `GetEmbeddingConfigurationValidationField` is pinned anywhere, so eight of nine arms can silently regress to the generic `configuration` label with the whole lane green.
- [x] [Review][Patch] Log the discarded provider failure in the new fail-closed paths [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:229] — the caught exception is dropped and all four unavailability kinds produce identical sanitized Details; add a `LoggerMessage` (infrastructure exists at the bottom of the class) so operators can distinguish a Dapr outage from a timeout from an HTTP failure server-side while the response stays sanitized.
- [x] [Review][Patch] Pin deliberate propagation of unrecognized provider exceptions [tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs] — Design Notes declare the narrow catch deliberate, but no test asserts that an unrecognized exception (for example `InvalidOperationException`) escapes `VerifyAsync` instead of being converted, leaving the deliberate boundary unprotected against accidental widening or narrowing.
- [x] [Review][Patch] Cover the null-lookup-Task guard [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:231] — the `lookup is null` branch has no test; the existing null tests cover `Task.FromResult(null!)`, not a null `Task`.
- [x] [Review][Patch] Use `ArgumentNullException.ThrowIfNull` for the new constructor guard [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:41] — the coalescing-throw form deviates from the documented project idiom for public-boundary validation.
- [x] [Review][Patch] Interpolate `VectorDimensions` in the all-checks-passed assertion [tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:74] — the assertion hardcodes `"validated 768-dimension configuration"` while sibling assertions interpolate the constant.
- [x] [Review][Patch] Resolve the pending TaskCompletionSource in the caller-cancellation test [tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:757] — `pendingConfiguration` is never completed or cancelled, leaving an abandoned task and `WaitAsync` continuation behind after the test.
- [x] [Review][Patch] Inline the verbatim evidence commands in the dev-story ledger cell [_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md:91] — the `Test count` cell records its commands by pointer, half of which targets the mutable Planned Verification section; the phase-ledger policy requires the exact command in the cell.
- [x] [Review][Patch] Correct the stale "all eight … declared below" dev-story reconciliation clause [_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md:91] — the final File List holds seven entries after the review-phase restoration of `epic-24-context.md`; add a dated bracketed correction so the historical cell no longer contradicts the list below it.
- [x] [Review][Patch] Align Design Notes with the implemented provider-side-cancellation classification [_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md:77] — "rethrow cancellation" reads as covering every `OperationCanceledException`, but provider-side cancellation without caller cancellation is deliberately classified as configuration unavailability; state both halves explicitly.
- [x] [Review][Patch] Cite Story 24.3 fail-closed verifier evidence in the completion record [_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md:72] — Dev Notes require citing Story 20.2 and Story 24.3 evidence; the Cross-Tenant Negative Evidence section carries only the Story 20.2 replacement.
- [x] [Review][Patch] Record the remediation-runtime-checklist applicability note [_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md] — the change touches no workflow/runtime dispatch, cleanup/dedup, or rollback surface, but the policy requires the explicit not-applicable note rather than leaving applicability implied.
- [x] [Review][Patch] Re-derive the stale Code Map anchors [_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md:46] — `TenantIsolationVerifier.cs:20` and "`CheckSemanticIsolationAsync` at line 214" predate the implementation recorded in the same commit; add dated corrections per the anchor-drift lesson.
- [x] [Review][Patch] Add the dated evidence comment on the sprint-status review entry [_bmad-output/implementation-artifacts/sprint-status.yaml:427] — every adjacent status transition carries a dated gate-evidence comment; the 24-7 `review` entry has none (to be applied at status synchronization).
- [x] [Review][Defer] Pre-existing constructor parameters remain unguarded [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:37] — deferred, pre-existing (`registry`, `redis`, `falkorDb`, and `logger` predate this story; only the new parameter was in scope).

Third-pass patch triage, 2026-08-14 (four parent-triaged patch groups).

- [x] [Review][Patch] Remove provider exception objects and messages from warning logs while retaining requested-tenant and exact safe failure-type classifications; add safe classifications for null lookup-task and null configuration-result contract violations.
- [x] [Review][Patch] Replace generated-logger-call inspection with observable capture assertions that pin warning level, requested tenant, exact classifications for Dapr/actor/timeout/HTTP/provider cancellation/null contracts, null exception payload, and absence of sensitive messages.
- [x] [Review][Patch] Prove a valid Google 1536-dimension configuration passes when both indexes agree, pin the secondary raw-versus-NL inconsistency diagnostic, and cover a complete config/raw/NL three-way mismatch.
- [x] [Review][Patch] Reconcile lifecycle records against the full baseline-to-worktree set: own `deferred-work.md`, exclude only the user-owned FrontComposer gitlink, execute C1 on `main`, correct cancellation wording/date/anchors, and append the current review ledger row.
- [x] [Review][Defer] The concrete `TenantEmbeddingConfigProvider` actor read ignores its caller cancellation token, so a cancelled verifier can stop waiting while the read continues and later populates the cache; recorded in `deferred-work.md` for a provider-focused change.
- [x] [Review][Defer] Semantic tenant-marker mismatches are accumulated without a count or length bound before being joined into `SemanticIsolation.Details`; recorded in `deferred-work.md` for the owning diagnostic-bounding slice.

## Spec Change Log

- 2026-08-13: Implemented all tasks at baseline `8feb2a2dff986c037de2a0875d00eb9aa32705bb`; the required Debug build succeeded with zero warnings/errors and the focused three-class lane passed 64/64 with zero skips.
- 2026-08-13: Applied code-review hardening for null safety, verifier-boundary cancellation, provider-side cancellation, Dapr actor invocation failures, and sanitized validation evidence; added five test cases and restored the Epic 24 context exactly to baseline. The Debug build remained clean and the focused three-class lane passed 69/69 with zero skips.
- 2026-08-14: Closed all fourteen second-pass patch findings: provider failures now retain server-side exception classification, unrecognized exceptions and null lookup tasks have explicit boundary tests, all nine validation-label outcomes plus cancellation cleanup are pinned, lifecycle evidence is reconciled, and the focused three-class lane passes 78/78 with zero skips.
- 2026-08-14: Applied all four third-pass patch groups: warning logs now carry only safe requested-tenant/type classifications, null contract violations are distinguishable, observable logging behavior and complete dimension diagnostics are pinned, and lifecycle scope is reconciled 9/9 as eight owned paths plus one named exclusion. Recorded the provider-cancellation and unbounded semantic-detail risks as pre-existing deferrals. The clean Debug build passed and the focused three-class lane passed 80/80 with zero skips.

## Design Notes

Validate the full returned embedding configuration so the verifier agrees with live ingestion/search policy. Catch only established provider-unavailable failures, keep validation failures explicit, and let unrecognized exceptions propagate. Caller-requested cancellation is rethrown; provider-side cancellation when the caller token is not cancelled is classified as configuration unavailability. Server diagnostics carry only the requested tenant ID and a safe concrete failure type/classification; exception objects and messages are never attached. The actor currently masks corrupt persisted state with defaults; this slice verifies the provider result and must not overclaim detection behind that boundary.

Remediation runtime checklist: not applicable — no workflow/runtime dispatch, cleanup/dedup, or rollback surface is touched.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: zero warnings and errors after any audit-only fallback is recorded separately.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests` -- expected: all focused verifier, cross-tenant denial, and provider-isolation tests pass with zero skips.
- `{ git diff --name-only 8feb2a2d; git ls-files --others --exclude-standard; } | sort -u > /tmp/hexalith-story-24-7-scope-paths.txt` -- expected: the complete baseline-to-worktree set contains eight owned paths and the named FrontComposer exclusion, with no untracked paths omitted.
- `python3 tools/check-story-review-readiness.py --story-key 24-7-tenant-configured-vector-dimension-verification --changed-files-file /tmp/hexalith-story-24-7-scope-paths.txt` -- expected: C1 executes on `main` and all nine paths are declared; do not use `--derive-cumulative`.
- `python3 tools/check-story-slice-scope.py --story-key 24-7-tenant-configured-vector-dimension-verification --changed-files-file /tmp/hexalith-story-24-7-scope-paths.txt --require-record` -- expected: historical-slice guard passes.
- `python3 tools/check-tenant-isolation-evidence.py --story-key 24-7-tenant-configured-vector-dimension-verification --changed-files-file /tmp/hexalith-story-24-7-scope-paths.txt` -- expected: cross-tenant negative evidence passes.
- `git diff --check` -- expected: no whitespace errors.

**Executed results (2026-08-14):** Clean Debug build passed in 11.58 seconds with 0 warnings and 0 errors; the focused verifier, authorization, and provider lane passed 80 tests in 7.188 seconds with 0 failures and 0 skips. The full baseline-to-worktree set contained nine paths and no untracked paths. Review readiness executed C1 on `main`, reported `C1: all 9 changed paths are declared.`, and passed; the historical-slice and tenant-isolation-evidence guards passed, and `git diff --check` reported no whitespace errors.

## Suggested Review Order

**Tenant-authoritative verification**

- Start with the requested-tenant lookup, validation, and independent dimension comparisons.
  [`TenantIsolationVerifier.cs:222`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L222)

- Independent configuration comparisons preserve raw-versus-NL consistency as a secondary assertion.
  [`TenantIsolationVerifier.cs:314`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L314)

- Classification-only warnings distinguish failures without logging exception payloads.
  [`TenantIsolationVerifier.cs:699`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L699)

- Confirm composition reuses the existing tenant-scoped configuration provider.
  [`MemoriesServerServiceCollectionExtensions.cs:365`](../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs#L365)

**Tenant isolation evidence**

- Three-way mismatch coverage pins both authoritative and secondary diagnostics.
  [`TenantIsolationVerifierTests.cs:154`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L154)

- Valid 1536-dimensional configuration proves the implementation is not default-bound.
  [`TenantIsolationVerifierTests.cs:277`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L277)

- Observable logging tests pin safe classifications and exclude sensitive exception messages.
  [`TenantIsolationVerifierTests.cs:230`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L230)

- Confirm mismatched-tenant POST verification is denied before every dependency.
  [`ServerEndpointAuthorizationTests.cs:104`](../../tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs#L104)

**Lifecycle and evidence**

- Review command-backed cross-tenant evidence and append-only phase accounting.
  [`24-7-tenant-configured-vector-dimension-verification.md:73`](24-7-tenant-configured-vector-dimension-verification.md#L73)

- Finish with explicitly deferred provider-cancellation and diagnostic-bounding risks.
  [`deferred-work.md:3287`](deferred-work.md#L3287)
