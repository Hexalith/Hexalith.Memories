---
title: 'Story 24.8: Semantic Isolation Key-Family Classification'
type: 'feature'
created: '2026-08-16'
status: 'done'
review_loop_iteration: 0
baseline_commit: '98e27534e5c9d0fbe84795228d5291868fb58b30'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-24-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
  - '{project-root}/_bmad/custom/remediation-runtime-checklist.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Broad raw and natural-language semantic key scans currently treat markerless migration-staging and legacy hashes as active tenant evidence, creating false isolation failures.

**Approach:** Add an explicit semantic key-family classifier based on canonical key reconstruction and bounded record shape, then restrict tenant-marker evidence to proven active raw base/chunk and current-NL records while failing unknown or ambiguous shapes as classification gaps.

## Boundaries & Constraints

**Always:** Preserve Story 24.7 configured-dimension checks, active missing/foreign-marker fail-closed behavior, cursor cancellation, cross-endpoint deduplication, authorization, and read-only verification. Keep classification gaps distinct from marker mismatches and retain exact requested-tenant scope.

**Ask First:** Any public V1 response-shape change, new persisted discriminator field, migration-writer/lifecycle change, or change outside the mapped classifier, schema, verifier, focused tests, and lifecycle records.

**Never:** Classify by a broad prefix/suffix shortcut or tenant-marker presence; mutate/delete/reindex data; expose payloads; weaken unknown-family failure; implement Story 24.9 remediation semantics; modify the five pre-existing Story 24.6/submodule dirty paths.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Healthy coexistence | Active raw base/chunk and current NL plus markerless raw/NL staging and legacy NL | Only three active families enter marker evidence; non-active families are excluded | Verification passes when active markers are healthy |
| Active marker defect | Proven-active record has a missing or foreign `tenantId` | Existing fail-closed marker evidence remains | `SemanticIsolation` fails without mutation |
| Opaque-ID collision | Active ID resembles `staging:`, `nl:`, a version, or numeric chunk suffix | Stored identity and record shape reconstruct the active canonical key | No false staging/legacy exclusion |
| True non-active collision | Staging/legacy key resembles an active opaque ID | Stored identity reconstructs only its registered non-active family | Exclude from marker evidence |
| Unknown or contradictory shape | No unique registered family matches key plus record shape | Report an evidence-classification gap | Fail closed without inventing a marker mismatch |
| Cancellation | Cursor or bounded hash-field read is cancelled | Cancellation propagates and later reads stop | No mutation or swallowed cancellation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:43` -- canonical active, staging, and legacy builders; add exact collision-safe staging parsing/registration reuse without changing wire keys.
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamily.cs` -- add the exhaustive active/non-active/unknown family model.
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs` -- add the single classifier/registry using canonical reconstruction and bounded hash fields.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:222` -- classify broad-scan discoveries before marker checks; preserve dimensions, cancellation, deduplication, and read-only behavior.
- `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:594` -- read-only evidence for raw/NL staging shape and intentional absence of `tenantId`; do not change unless Ask First is approved.
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs:20` -- family totality, exact reconstruction, future-family failure, and opaque-ID collision matrix.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs:608` -- coexistence, active-marker failures, gaps, cancellation, counts, and no-mutation evidence.
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs:25` -- pin staging provenance/shape and coexistence without lifecycle mutation.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs:103` -- rerun the existing method-correct POST denial-before-dependency proof.
- `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md` and `sprint-status.yaml` -- reconcile baseline, phase ledger, evidence, File List/exclusions, and lifecycle status.

## Tasks & Acceptance

**Execution:**
- [x] `Infrastructure/SemanticKeyFamily*.cs` and `IndexSchemaDefinitions.cs` -- register every current family and classify exact canonical key plus record shape, returning unknown/ambiguous rather than guessing.
- [x] `Tenants/TenantIsolationVerifier.cs` -- apply classification before active-marker evidence and report gaps separately while remaining bounded, cancellable, deduplicated, and read-only.
- [x] Focused server tests -- implement the matrix, totality/future guard, migration coexistence, cancellation, no-mutation, and existing authorization evidence.
- [x] Story lifecycle records -- adopt the 122-case pre-change baseline, record the external build blocker separately, reconcile all eleven owned paths, the five specified pre-existing exclusions, and three concurrent workspace-user exclusions, and execute repository story/isolation gates.

**Acceptance Criteria:**
- Given every current semantic key shape, when classification runs, then exactly one explicit family is returned and only active raw base/chunk and current NL permit marker evidence.
- Given markerless staging/legacy records coexist with healthy active records, when verification runs, then they are excluded while missing/foreign markers on proven-active records still fail closed.
- Given reserved-looking opaque IDs, when classification runs, then canonical reconstruction plus record shape—not token parsing—keeps legitimate active keys active; unresolved evidence becomes a classification gap.
- Given an unregistered future namespace or migration shape, when schema/classifier guards and verification run, then classification fails closed and no staging or tenant data is mutated.

### Review Findings

- [ ] [Review][Patch] Family-totality test partially compares the classifier registry against itself, so `Classify()`'s hardcoded dispatch could silently drift from the registry with no test catching it [tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs:267-270]
- [ ] [Review][Patch] Public `TryParseSemanticStagingVersion`/`TryParseNaturalLanguageSemanticStagingVersion` throw `ArgumentException` instead of returning `false` for null/whitespace input, violating the Try-pattern convention on a public API surface [src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:808-809]
- [ ] [Review][Patch] No test exercises a natural-language record that also carries complete chunk fields; it falls through to `Unknown` correctly today but is unverified [src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs:59-113]
- [ ] [Review][Patch] `discriminatorValues[0..4]` magic positional indices are tied to `_semanticDiscriminatorFields` field order 30+ lines away with no compile-time safety, feeding tenant-isolation classification [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:594-601]
- [ ] [Review][Patch] The new shared `CreateNoConnectedRedisServerException` helper extraction touches the pre-existing syntactic scan path, outside this story's declared "semantic classification" File Scope wording; message-only, no behavior change [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:504]
- [ ] [Review][Patch] `SemanticKeyFamily.Ambiguous` branch has no reachable/passing test proving it fires; three independent review layers converged on this [src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs:115-120]
- [x] [Review][Defer] Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence, downgrading to a generic "backend unavailable" result [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:343-397] — deferred, pre-existing pattern predating Story 24.8
- [x] [Review][Defer] `Remediation` text always prioritizes classification-gap wording over marker-mismatch (or dimension-mismatch) wording when both co-occur in the same check [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:365-377] — deferred; this diff did not close the pre-existing carried-forward ledger item `24.6-F8-W9` ("dual remediation when classification gap and dimension mismatch co-occur"), which already names Story 24.8 as its natural closure point
- [x] [Review][Defer] The two-round-trip discriminator read (`HashGetAsync` + separate `HashExistsAsync`) was not folded into one batched call [src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:578-585] — deferred; this diff did not close the pre-existing carried-forward ledger item `24.6-F8-W7`, which already names Story 24.8 as its natural closure point

## Spec Change Log

- 2026-08-17 — Implemented the approved family registry/classifier, bounded verifier integration, exact staging reconstruction, focused matrix, migration-shape proof, and lifecycle evidence without changing the frozen intent or migration writer.
- 2026-08-17 — Closed independent implementation-verification findings: registered every declared semantic key-prefix constant to a classifier disposition, preserved the exact pre-24.8 remediation outside classification gaps, corrected authorization evidence, and reconciled the complete 19-path worktree as ten owned paths plus nine exclusions.
- 2026-08-17 — Applied all Step 4 patch findings: tightened active chunk provenance to canonical positive spans, bounded exact read-command evidence, completed the active marker and cancellation matrices, preserved mixed diagnostics, and classified WRONGTYPE discriminator reads as gaps. Recorded the review's Redis-timeout finding as a deferral without modifying the frozen intent.

## Review Findings

- **Applied patches:** canonical chunk numeric/span proof; exact Redis/FalkorDB read-only call assertions; missing/foreign marker evidence for raw base, raw chunk, and current NL; mixed classification-gap/marker evidence; pending cursor and HEXISTS cancellation; and WRONGTYPE handling for both bounded discriminator reads.
- **Deferred:** `RedisTimeoutException` can still escape the verifier's structured backend-unavailable contract. The workflow-owned record is appended to `deferred-work.md`; no timeout behavior was changed by Story 24.8.
- **Disposition:** no intent gap or bad-spec loopback, no frozen-section change, and no Story 24.9 remediation redesign.

## Design Notes

Classification first reads only discriminator fields (`memoryUnitId`, `tenantId`, semantic/NL/chunk shape fields), never vector or content payloads. It rebuilds candidate keys through canonical builders and accepts exactly one family. Marker presence is evaluated only after an active classification, so a markerless active record still fails while a markerless staging record does not masquerade as contamination. Remediation runtime checklist category 4 applies at the staging-preservation boundary: focused evidence must prove the verifier performs no writes, deletes, transactions, alias changes, or index changes.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` -- expected: zero warnings/errors, or the exact pre-existing Builds-catalog blocker recorded separately without weakening the gate.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` -- expected: all focused cases pass with zero skips and runner-derived phase counts.
- `python3 tools/check-story-file-scope.py --story-key 24-8-semantic-isolation-key-family-classification --changed-files-file /tmp/hexalith-story-24-8-owned-paths.txt && python3 tools/check-story-slice-scope.py --story-key 24-8-semantic-isolation-key-family-classification --changed-files-file /tmp/hexalith-story-24-8-scope-paths.txt --require-record && python3 tools/check-tenant-isolation-evidence.py --story-key 24-8-semantic-isolation-key-family-classification --changed-files-file /tmp/hexalith-story-24-8-scope-paths.txt && python3 tools/check-story-review-readiness.py --story-key 24-8-semantic-isolation-key-family-classification --changed-files-file /tmp/hexalith-story-24-8-scope-paths.txt` -- expected: file-scope passes over the eleven owned paths, and every complete-set lifecycle/evidence gate passes over the full baseline-to-worktree set including machine-readable exclusions.
- `git diff --check` -- expected: no whitespace errors.

**Observed 2026-08-17:**

- The normal build command reached the compiler but remained blocked by inherited `StackExchange.Redis` 3.1.13 catalog diagnostics: `CS0618` in `TenantExportService.cs` and the existing no-server exception path in `TenantIsolationVerifier.cs`, plus `SER301` in `ReleaseDedupKeyIfOwnedActivity.cs`. A diagnostic build demoting exactly `CS0618;SER301` succeeded with 104 warnings and 0 errors in 13.24 seconds; this is compile evidence, not a replacement for the blocked normal gate.
- The final exact four-class focused command passed **161/161** test cases with 0 failures or skips in 7.784 seconds. The comparable pre-change run passed **122/122**, yielding a runner-derived cumulative delta of **+39** and a Step 4 phase delta of **+13** over the prior 148-case lane. Coverage now includes canonical positive-span chunks, all six active marker cells, mixed defects, both WRONGTYPE discriminator reads, pending cursor/HMGET/HEXISTS cancellation, exact read-only command arguments, and the schema/enum/disposition guards.
- Story file-scope passed against all eleven owned Story 24.8 paths. Slice-scope and tenant-isolation evidence passed, and story review-readiness passed against the complete 19-path baseline-to-worktree set: eleven File List paths plus eight machine-readable workspace-user exclusions.

## Suggested Review Order

**Verifier evidence flow**

- Classify each discovered hash before any tenant-marker evidence is trusted.
  [`TenantIsolationVerifier.cs:547`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L547)

- Keep classification gaps distinct while preserving ordinary marker remediation.
  [`TenantIsolationVerifier.cs:365`](../../src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs#L365)

**Canonical provenance**

- Reconstruct exact active and non-active candidates from stored identity and bounded shape.
  [`SemanticKeyFamilyClassifier.cs:34`](../../src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs#L34)

- Bind every declared semantic namespace to an explicit family disposition.
  [`IndexSchemaDefinitions.cs:22`](../../src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs#L22)

- Recover staging versions only when canonical builders reproduce the complete key.
  [`IndexSchemaDefinitions.cs:351`](../../src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs#L351)

- Keep active, staging, legacy, unknown, and ambiguous outcomes exhaustive.
  [`SemanticKeyFamily.cs:9`](../../src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamily.cs#L9)

**Boundary proofs**

- Pin missing and foreign markers across every active semantic family.
  [`TenantIsolationVerifierTests.cs:697`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L697)

- Prove healthy active records coexist with markerless staging and legacy hashes.
  [`TenantIsolationVerifierTests.cs:750`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L750)

- Preserve mixed gap and active-marker evidence without mutation.
  [`TenantIsolationVerifierTests.cs:855`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L855)

- Exercise WRONGTYPE and cancellation boundaries through the real verifier flow.
  [`TenantIsolationVerifierTests.cs:895`](../../tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs#L895)

- Guard namespace registration, enum totality, and active-marker disposition together.
  [`IndexSchemaDefinitionsTests.cs:247`](../../tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs#L247)

- Pin canonical markerless migration writes without lifecycle mutation.
  [`RedisEmbeddingMigrationStoreTests.cs:122`](../../tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs#L122)

**Lifecycle evidence**

- Reconcile phase counts, build blocker, owned paths, and exclusions.
  [`24-8-semantic-isolation-key-family-classification.md:92`](24-8-semantic-isolation-key-family-classification.md#L92)

- Carry the pre-existing Redis-timeout contract gap forward explicitly.
  [`deferred-work.md:3389`](deferred-work.md#L3389)

- Present Story 24.8 as ready for independent review.
  [`sprint-status.yaml:428`](sprint-status.yaml#L428)
