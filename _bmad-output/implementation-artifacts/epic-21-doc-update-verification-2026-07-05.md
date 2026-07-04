# Epic 21 Documentation Update Verification

Project: Hexalith.Memories
Date: 2026-07-05
Mode: Autonomous post-retrospective documentation verification

## Verification Method

For each candidate document, current documentation was read, compared against implemented Epic 21 code and story evidence, and either updated or discarded as already accurate / out of scope.

Code and artifact anchors checked:

- EventStore command boundary and projection workflows: `src/Hexalith.Memories.Server/Cases/CaseService.cs`, `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs`, `src/Hexalith.Memories.Server/EventStoreIntegration/*`, `src/Hexalith.Memories.Server/Workflows/*ProjectionWorkflow.cs`.
- Natural-language vector namespace and key helpers: `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`, `src/Hexalith.Memories.Server/Migration/RedisNaturalLanguageNamespaceMigrator.cs`.
- Dedup first-writer-wins behavior: `src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs`, `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`.
- EventStore event intake retry posture: `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, `src/Hexalith.Memories.EventStore/EventIngestionService.cs`, `tests/Hexalith.Memories.EventStore.Tests/EventIngestionControllerTests.cs`.
- Tenant registry CAS/transaction behavior: `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs`, `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`.
- Blue/green migration and rollback/abort behavior: `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs`, `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs`, `tools/MigrateEmbeddingVectors/Program.cs`, `tests/Hexalith.Memories.Server.Tests/Migration/*`, `tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs`.
- Opaque memory-unit ID behavior: `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.Server/Cases/CaseValidator.cs`, `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`, `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs`.

## Updated Documents

| Document | Verified discrepancy | Update applied |
|---|---|---|
| `_bmad-output/planning-artifacts/architecture.md` | Top FR6 driver still described the consistency pattern as "eventual consistency + compensation"; the Multi-Backend Consistency section still said Story 21.2 had not implemented the target mutation path. Code now accepts case/memory-unit/tenant lifecycle commands through EventStore before projection fan-out. | Updated FR6 driver phrasing and removed the obsolete pre-21.2 transitional warning. |
| `docs/dev/consistency.md` | Consistency inspect docs said `memoryUnitId` must be a 26-character Crockford ULID, but live implementation treats memory-unit IDs as opaque strings. Mutation APIs validate alphanumeric/hyphen/max-length in `CaseValidator`; consistency inspect itself delegates to storage helpers that only require a non-blank ID. | Updated the endpoint predicate and CLI example to use an opaque memory-unit ID. |
| `docs/dev/memory-unit-id-stability.md` | The document still said the architecture projection "still lists" the memory unit `Id` as `string (ULID)`, but architecture has already been corrected to opaque/not-ULID wording. | Updated the note to refer to earlier stale projections rather than the current architecture document. |

## Discarded Updates

| Document | Candidate concern | Decision |
|---|---|---|
| `docs/dev/eventstore-integration.md` | Might be stale after 21.3, 21.6, and 21.7. | Discarded. Current doc already covers tenant-not-found/deleting retry posture, duplicate outcomes, permanent workflow-level dedup, and `{tenant}:vecnl:*` with legacy `{tenant}:vec:nl:*` migration. |
| `docs/operations/embedding-providers.md` | Might be stale after 21.9/21.10 blue/green migration fixes. | Discarded. Current runbook already documents live blue/green migration, staging aliases, owner lock/heartbeat, rollback, abort, and deleted-tenant cleanup. |
| `docs/dev/ingest-contract.md` | Might be stale after 21.7 dedup changes. | Discarded. Current doc already states permanent dedup records are TTL-less first-writer-wins writes with `expiry: null` and `When.NotExists`. |
| `README.md` | Might need new Epic 21 links. | Discarded. README only links stable top-level docs; no implementation contract in README diverged from code. |
| `docs/governance/PII_ACKNOWLEDGMENT.md` | Might need NL namespace details after 21.3. | Discarded. The relevant NL namespace details are already in `docs/dev/eventstore-integration.md` and `docs/operations/embedding-providers.md`; no governance text was contradicted by code. |

## Validation

- Stale-phrase scan confirmed no remaining `Until Story 21.2 implements the target mutation path` text.
- Stale-phrase scan confirmed no remaining `26-char Crockford-base32 ULID` predicate in `docs/dev/consistency.md`.
- Code scan confirmed `IMemoriesCommandStore` is injected into `CaseService` and `TenantRegistryService`, projection workflows are registered, `SaveDedupKeyActivity` uses `expiry: null` with `When.NotExists`, and `IndexSchemaDefinitions.NaturalLanguageSemanticKeyPrefixSuffix` is `:vecnl:`.
