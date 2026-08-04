---
baseline_commit: e902181dcdce599187e74fd2c3c9b12f995dcc18
approved_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-04-story-24-3-verifier-residual-backlog-decisions.md
---

# Story 24.8: Semantic Isolation Key-Family Classification

Status: backlog

Owner: Developer and Murat / Test Architect

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
- **Tests:** `TenantIsolationVerifierTests.VerifyAsync_MarkerlessStagingAndLegacyKeys_DoNotReportActiveMismatch`, `IndexSchemaDefinitionsTests.SemanticFamilyClassifier_OpaqueReservedLookingId_RemainsActive`, and `ServerEndpointAuthorizationTests.TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState`.
- **Command:** `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
- **Result:** pending — this is a backlog evidence contract; the full family matrix, opaque-ID collision shapes, migration coexistence, and denial-before-dependency must execute and pass before `done`.

## Planned Verification

| Focused evidence | Command | Required result | Status |
| :--------------- | :------ | :-------------- | :----- |
| Family inclusion/exclusion matrix | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests` | Active families are included; staging/legacy excluded; unknown and opaque-ID collision cases fail safely without false marker-mismatch claims. | pending |
| Migration coexistence | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Migration.RedisEmbeddingMigrationStoreTests -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests` | Active migration staging is neither mutated nor reported as active marker contamination. | pending |
| Story 20.2 denial-before-dependency | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` | Unauthorized tenant verification is denied before Redis/config dependencies where applicable. | pending |
