---
baseline_commit: 98e27534e5c9d0fbe84795228d5291868fb58b30
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.8: Semantic Isolation Key-Family Classification

Status: review

Owner: Developer and Murat / Test Architect

Implementation source: [Story 24.8 implementation spec](spec-24-8-semantic-isolation-key-family-classification.md).
The approved requirements remain canonical here; implementation tasks, executable evidence, and lifecycle reconciliation are maintained in the linked spec and recorded below.

## Story

As an operator,
I want tenant-marker scans limited to proven active semantic key families,
so that migration staging and legacy hashes do not create false isolation failures.

## Acceptance Criteria

1. Given active raw base/chunk hashes, active current-NL hashes, raw/NL migration staging hashes, and legacy nested-NL hashes, when semantic family classification runs, then every shape has one explicit classification and only proven active raw base/chunk and current-NL hashes enter active tenant-marker evidence.
2. Given markerless staging or legacy hashes coexist with healthy active hashes, when verification runs, then those non-active families are excluded from marker-mismatch evidence, while a markerless or foreign-marked proven-active hash still fails closed.
3. Given memory-unit identifiers are opaque and may contain colon-delimited text that resembles `staging:`, `nl:`, a version, or a chunk suffix, when classification runs, then canonical namespace provenance and record shape—not a broad prefix or suffix shortcut—decide the family. Collision-shaped opaque-ID tests prove a legitimate active key is not excluded as staging or legacy. If provenance cannot distinguish a key, the verifier reports an evidence-classification gap rather than inventing an active-marker mismatch.
4. Given a future semantic namespace or Story 21.9 migration state is introduced, when verifier, schema, and migration tests run, then unknown families fail the classification guard and the verifier never deletes, mutates, or treats staging state as active evidence.

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Stories 21.3 and 21.9 | `current-narrow-pattern` | Reuse canonical legacy/current/staging builders and migration ownership, after verifying them against current source. |
| Story 24.3 | `historical-reference-only` | Preserve fail-closed active marker checks; correct only evidence membership and classification wording. |
| Broad `keyPrefix + "*"` scan | `anti-template` | Use only to reproduce the false-positive baseline; never use it as an active-family classifier. |
| Prefix-only staging/legacy parsing | `anti-template` | Use only as a negative test oracle because opaque memory-unit IDs can mimic reserved-looking suffixes. |
| Story 20.2 | `current-narrow-pattern` | Re-run denial-before-dependency evidence for the tenant verifier surface. |

## Slice Proof

- One independently demonstrable outcome: collision-safe membership in active semantic marker evidence.
- Demonstration boundary: a focused inclusion/exclusion matrix covers active raw base/chunk, current NL, raw/NL staging, legacy nested NL, unknown families, and opaque-ID collision shapes.
- Excluded: missing-versus-foreign remediation wording, data backfill, vector dimensions, graph proof, and physical isolation enforcement.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md` — UPDATE lifecycle, evidence, phase ledger, scope, and File List.
- `_bmad-output/implementation-artifacts/deferred-work.md` — UPDATE only with the workflow-generated Story 24.8 review deferral.
- `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` — ADD the approved implementation contract and verification record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATE only through workflow status synchronization.
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` — UPDATE exact canonical staging reconstruction helpers.
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamily.cs` — ADD the exhaustive semantic family model.
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs` — ADD the bounded, collision-safe family registry and classifier.
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` — UPDATE semantic classification before tenant-marker evidence.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` — UPDATE only if method-correct denial-before-dependency evidence requires it.
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs` — UPDATE family totality, exact reconstruction, future-family, and opaque-ID coverage.
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs` — UPDATE markerless staging shape and no-lifecycle-mutation evidence.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` — UPDATE coexistence, marker failure, classification gap, cancellation, deduplication, and read-only coverage.

## Dev Notes

The implementation must establish provenance without assuming `ValidateMemoryUnitId` restricts colons: it currently validates nonblank input only. A bare key prefix cannot distinguish a raw active memory-unit ID shaped like `staging:{version}:{id}` from a staging key. Use canonical builders plus authoritative migration/index or record metadata and field shape; unresolved ambiguity is an evidence-classification gap, not proof of a tenant-marker mismatch. Add a schema/architecture guard so future families must be registered explicitly.

Affected tenant-sensitive surfaces are `TenantIsolationVerifier`, `IndexSchemaDefinitions`, semantic index/schema registration, `RedisEmbeddingMigrationStore`, migration marker evidence, and tenant verification output. Completion must retain Story 20.2 denial-before-dependency evidence and Story 24.3 fail-closed active-marker evidence. Planned results are `pending` and block `done` until executed.

### Epic AC Verification

Verified 2026-08-04 against `e902181dcdce599187e74fd2c3c9b12f995dcc18`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| Story 24.3 broad raw/NL scan patterns overlap staging and legacy namespaces. | implementation | `rg -n 'SemanticKeyPrefixSuffix|LegacyNaturalLanguageSemanticKeyPrefixSuffix|Staging.*KeyPrefix' src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` | Current constants nest staging and legacy shapes beneath broad active prefixes. | confirmed |
| Migration staging hashes omit the `tenantId` field used by verifier marker evidence. | implementation | `sed -n '595,678p' src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs` | Both staging write entry lists omit `tenantId`. | confirmed |
| Opaque memory-unit IDs make prefix-only family classification ambiguous. | implementation | `rg -n 'ValidateMemoryUnitId|ThrowIfNullOrWhiteSpace' src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` | Validation requires nonblank text but does not reserve colon-delimited namespace tokens. | confirmed |

## Cross-Tenant Negative Evidence

- **Surfaces:** `TenantIsolationVerifier`, semantic key/index routing, `IndexSchemaDefinitions`, migration staging storage, and tenant marker evidence.
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_MarkerlessStagingAndLegacyKeys_DoNotReportActiveMismatch`, `IndexSchemaDefinitionsTests.SemanticFamilyClassifier_ReservedLookingOpaqueId_UsesStoredIdentityAndCanonicalReconstruction`, and `ServerEndpointAuthorizationTests.TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies`.
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
- **Result:** passed 2026-08-17 — the final required four-class lane passed 161/161 test cases with zero failures or skips in 7.784 seconds. It covered healthy active/staging/legacy coexistence, missing and foreign markers across all active families, reserved-looking opaque identifiers, canonical positive-span chunk provenance, true non-active collisions, mixed and WRONGTYPE classification gaps, schema namespace registration coverage, cursor/HMGET/HEXISTS cancellation, cross-endpoint deduplication, markerless migration storage, exact read-only Redis/FalkorDB commands, no mutation, exact pre-24.8 remediation preservation outside classification gaps, and the existing denial-before-dependency proof.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Family inclusion/exclusion matrix | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests` | Active families are included; staging/legacy excluded; unknown and opaque-ID collision cases fail safely without false marker-mismatch claims. | passed 2026-08-17 — covered by the 161/161 combined lane with zero failures/skips; all six active-family marker cells, canonical chunk discriminators, mixed/WRONGTYPE gaps, and every declared semantic key-prefix registration are pinned |
| Migration coexistence | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Active migration staging is neither mutated nor reported as active marker contamination. | passed 2026-08-17 — markerless raw/NL staging shapes were pinned and verifier mutation calls remained absent |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Unauthorized tenant verification is denied before Redis/config dependencies where applicable. | passed 2026-08-17 — existing authorization class was included in the 161/161 combined lane; exact method `TenantVerifyPost_WithMismatchedTenant_ReturnsTenantForbiddenBeforeDependencies` passed |

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-08-17 | create-story | Adopted the canonical phase ledger at the implementation handoff, linked the approved implementation spec, aligned the story baseline and lifecycle status, and preserved the approved Story and Acceptance Criteria unchanged. Owner: Amelia / Developer. Earlier deltas are not reconstructed; this runner-observed point is the story baseline. | Phase delta **+0 test cases**; cumulative story delta **+0**. Baseline discovery: **122 test cases**, 0 failed, 0 skipped. Command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`. | `matched 2/2` against baseline `98e27534e5c9d0fbe84795228d5291868fb58b30`: the canonical story and approved implementation spec were the pre-implementation lifecycle artifacts. The five inherited Story 24.6/submodule paths listed below were explicitly excluded. |
| 2026-08-17 | dev-story | Added an exhaustive semantic family registry and collision-safe classifier using exact canonical key reconstruction plus bounded hash shape; classified before marker evidence; excluded proven markerless staging/legacy records; failed unknown or ambiguous evidence separately; preserved configured dimensions, cancellation, deduplication, authorization, and read-only behavior; preserved the exact pre-24.8 remediation for ordinary failures; and added the full matrix, schema namespace guard, and migration-shape proofs. | Phase delta **+26 test cases**; cumulative story delta **+26**. The comparable four-class xUnit lane increased **122 -> 148 test cases** and the final run passed 148/148 with 0 failures/skips in 8.089 seconds. Normal build remained blocked only by the inherited `StackExchange.Redis` 3.1.13 catalog diagnostics `CS0618` and `SER301`; a diagnostic build demoting exactly those two known catalog diagnostics completed with 0 errors. | `matched 19/19` against baseline `98e27534e5c9d0fbe84795228d5291868fb58b30`: all ten Story 24.8 File List paths plus nine named workspace-user exclusions cover the complete baseline-to-worktree set. The overlapping sprint-status path owns only the 24.8 row and 2026-08-17 synchronization; concurrent Story 24.6/24.7 content remains user-owned. |
| 2026-08-17 | code-review | Applied the complete Step 4 patch set without changing frozen intent: required canonical positive chunk spans, made non-canonical numeric discriminator text unknown, converted WRONGTYPE discriminator reads into distinct fail-closed gaps, pinned all active marker cells and mixed defects, covered cursor and HEXISTS cancellation, and restricted no-mutation evidence to exact verifier read commands/arguments. Recorded the independently triaged Redis timeout issue as one workflow deferral. | Phase delta **+13 test cases**; cumulative story delta **+39**. The comparable four-class lane increased **148 -> 161 test cases** during review (**122 -> 161** cumulative) and passed 161/161 with 0 failures/skips in 7.784 seconds. Normal build remained blocked only by three inherited `CS0618` diagnostics and one inherited `SER301`; the exact diagnostic compile succeeded with 104 warnings and 0 errors in 13.24 seconds. | `matched 19/19` against baseline `98e27534e5c9d0fbe84795228d5291868fb58b30`: all eleven Story 24.8 File List paths plus eight named workspace-user exclusions cover the complete baseline-to-worktree set. `deferred-work.md` is now an overlapping Story-owned review output; its pre-existing content was preserved. |

### Final Gate Evidence

- Build gate: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --disable-build-servers -m:1 /nr:false` — blocked 2026-08-17 by the inherited `StackExchange.Redis` 3.1.13 catalog diagnostics `CS0618` in `TenantExportService.cs` and `TenantIsolationVerifier.cs`, plus `SER301` in `ReleaseDedupKeyIfOwnedActivity.cs`; no Story 24.8 compile error remained.
- Diagnostic compile: the same command with `-p:WarningsNotAsErrors=CS0618%3BSER301 -clp:ErrorsOnly` — succeeded 2026-08-17 with 104 warnings and 0 errors in 13.24 seconds; this does not replace the blocked normal gate.
- Focused lane: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` — final run passed 161/161, 0 failed/skipped, in 7.784 seconds.
- Story gates: file-scope validation passed for the eleven owned paths; slice-scope and tenant-evidence validation passed; complete-set review-readiness passed against all 19 baseline-to-worktree paths (`C1: all 19 changed paths are declared`, consisting of eleven File List paths and eight machine-readable exclusions).
- Whitespace: `git diff --check` passed 2026-08-17 with no whitespace errors after CRLF-policy normalization of the Story 24.8 source, test, and lifecycle artifacts.

## File List

- `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamily.cs`
- `src/Hexalith.Memories.Server/Infrastructure/SemanticKeyFamilyClassifier.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`

### File List Exclusions

- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md` — owner: workspace user; pre-existing unrelated Story 24.6 lifecycle work explicitly preserved and not owned by Story 24.8.
- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md` — owner: workspace user; concurrent unrelated Story 24.7 lifecycle work explicitly preserved and not owned by Story 24.8.
- `_bmad-output/implementation-artifacts/spec-24-6-eighth-pass-action-item-closure.md` — owner: workspace user; pre-existing unrelated Story 24.6 review work explicitly preserved and not owned by Story 24.8.
- `_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md` — owner: workspace user; pre-existing unrelated Story 24.6 specification work explicitly preserved and not owned by Story 24.8.
- `_bmad-output/implementation-artifacts/spec-24-7-fourth-pass-action-item-closure.md` — owner: workspace user; concurrent unrelated Story 24.7 action-item closure explicitly preserved and not owned by Story 24.8.
- `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md` — owner: workspace user; concurrent unrelated Story 24.7 specification work explicitly preserved and not owned by Story 24.8.
- `references/Hexalith.Builds` — owner: workspace user; pre-existing unrelated submodule work explicitly preserved and not owned by Story 24.8.
- `references/Hexalith.FrontComposer` — owner: workspace user; pre-existing unrelated submodule work explicitly preserved and not owned by Story 24.8.
