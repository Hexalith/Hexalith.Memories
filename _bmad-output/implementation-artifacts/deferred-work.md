# Deferred Work

## Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)

- **DataContract/DataMember attributes missing on V1 contracts** — Systematic gap across all V1 contracts (ExtractionInput, ExtractionResult, MemoryUnit, GraphEdge, etc.). None use DataContract/DataMember/JsonPropertyOrder/JsonConstructor attributes per project-context.md rules. Should be addressed as a batch across all V1 types.
- **No transient/permanent exception classification for Kreuzberg errors** — AC4 is met (exceptions propagate for DAPR Workflow retry). However, permanent failures (corrupt files) will be retried indefinitely. Future work: classify KreuzbergValidationException as non-retriable, KreuzbergOcrException as retriable.
- **Large byte[] in ExtractionInput persisted to workflow state store** — DAPR Workflow serializes activity inputs to state store. For 1MB files, this means ~1.33MB base64 per workflow instance. Accepted per D13 (MVP payloads ≤1MB). Future work: consider streaming or external storage for larger payloads.
- **byte[] mutable on immutable record** — ExtractionInput uses byte[] which is mutable, breaking record immutability semantics and reference-based equality. No practical alternative exists in .NET for JSON-serializable binary data (ImmutableArray/ReadOnlyMemory don't serialize well).

## Deferred from: code review of 1-4-embedding-generation (2026-03-29)

- **End-to-end embedding flow is not wired into orchestration** — Deferred because orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
- **Rate-limiting scope conflicts with credential scope** — Deferred because Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
- **Story transition rationale is comment-only and not machine-readable** — The sprint tracking update relies on a free-form YAML comment for rationale, so tooling cannot query or validate why a story moved between workflow states.
- **Story status requires manual dual-write across tracking files** — The workflow duplicates status in both the story artifact and `sprint-status.yaml`, which is a pre-existing consistency risk whenever one file changes without the other.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)

- **ValidateResult.IsValid/ErrorMessage is dead code on failure path** — ValidateContentActivity throws exceptions for invalid input; the ValidateResult record is only used for the success path. Spec mandates this contract shape, so keeping as-is.
- **SaveDedupKeyActivity: no TTL on dedup keys** — Dedup keys persist forever in DAPR state store, preventing re-ingestion of deleted content. Cleanup mechanism belongs to Epic 8 (Story 8.2 consistency verification).
- **ContentBytes serialized inline in DAPR workflow state** — Base64-encoded byte[] in IngestionInput causes replay amplification for large files. Accepted per D13 (MVP payloads <= 1MB). Same issue as Story 1.3 ExtractionInput. Future: external blob storage for content.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **Duplicate dedup entries are returned without confirming the referenced memory unit still exists** — The workflow fast-returns on a dedup hit without verifying that the stored `MemoryUnitId` is still present in the indexed backends, so manual cleanup or backend drift can leave callers with a duplicate response that points at missing data.

## Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **indexedAt set to ingestedAt in GraphQueryBuilder** — `BuildMergeMemoryUnitNode` sets the FalkorDB `indexedAt` property to the workflow's `ingestedAt` timestamp. These are semantically different (when ingestion started vs when the graph write happened). Fixing requires adding a separate `indexedAt` parameter to `IndexInput`, which is a cross-story contract change (Story 1.5).
- **CaseId not validated for special characters** — `TenantId` has a strict alphanumeric+hyphen regex via `TenantIdGuard.Validate`, but `CaseId` only checks for null/empty. Not spec-required; CaseId is used as hash field values (not key names or graph names), so the blast radius is limited to potential key scan interference.
