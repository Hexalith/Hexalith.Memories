# Stable Ingest Contract (Story 18.4)

The ingest path graduated to a **stable, non-experimental** contract so downstream consumers (e.g. the
Parties projection indexer) can ingest **without** `#pragma warning disable HXL001`. This document is the
authoritative description of the ingest dedup/idempotency guarantees.

- **Status:** Stable (graduated out of `HXL001` — see [experimental-apis.md](./experimental-apis.md))
- **Client:** [`MemoriesClient.IngestAsync`](../../src/Hexalith.Memories.Client.Rest/MemoriesClient.cs) (`Hexalith.Memories.Client.Rest`, NuGet-publishable)
- **Contract:** [`IngestionInput`](../../src/Hexalith.Memories.Contracts/V1/IngestionInput.cs) (`Hexalith.Memories.Contracts`, NuGet-publishable)
- **Ingress:** `POST /api/ingest` → `IngestionWorkflow` (Dapr Workflow)

---

## 1. Stable, additive entry point (AC1)

`MemoriesClient.IngestAsync` is no longer `[Experimental("HXL001")]`. The change is **strictly additive**:

- The original 8-parameter overload is unchanged (so existing callers keep compiling and binary-link).
- A new overload adds a trailing **optional** `string? idempotencyToken` before the `CancellationToken`.
- `IngestionInput` gains an **optional** `IdempotencyToken` property, serialized through `MemoriesJsonContext`
  as camelCase `idempotencyToken`. Payloads that omit it deserialize unchanged (back-compat).

No breaking signature change, no `BREAKING CHANGE:` footer — this is a `feat:` (MINOR) release.

## 2. Idempotency token: precedence and natural-key fallback (AC2)

`IngestionInput.IdempotencyToken` is **optional**.

- **Precedence:** when a non-blank token is supplied, it is the dedup identity. Duplicate detection checks the
  token-keyed record **first**.
- **Fallback:** when the token is absent, dedup falls back to the `sourceUri` natural key exactly as before.
- **Augment, never replace:** a token-supplied ingest still writes the permanent
  `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` → `MemoryUnitId` record **in addition to** a token-keyed
  record (`dedup:{tenantId}:{caseId}:tok:{sha256(token)}`). Both point at the **same** `MemoryUnitId`. This keeps
  the `sourceUri → MemoryUnitId` mapping that **Story 18.5** (source-URI-keyed lookup) and **Story 18.6**
  (`MemoryUnitId` stability) depend on. The committed records are permanent (TTL-less).

## 3. Atomic dedup — no check-then-act race (AC3)

Two near-simultaneous ingests of the same dedup identity resolve to **exactly one** memory unit; the loser
observes the winner's `MemoryUnitId`.

The REST `/api/ingest` ingress performs an **atomic preflight reservation** (Redis `SET … NX`) on a dedicated
`ingest-reserve:{dedupKey}` key whose value is the winning workflow's instance id, **before** scheduling the
workflow:

- **Reserved** (first writer) → schedule the workflow with that instance id; return it.
- **DuplicateInFlight** (a concurrent/recent ingest already reserved) → return the **winner's** instance id
  **without** scheduling a second workflow. For `SourceType.File` the `MemoryUnitId` equals the workflow
  instance id, so the loser observes the same `MemoryUnitId`.
- **FailOpen** (Redis unavailable) → proceed and schedule anyway (ADR 9.1-B); the permanent dedup key and
  `CheckIdempotencyActivity` remain the authoritative safety net.

If scheduling fails after a successful reservation, the reservation is released (`ReleaseAsync`); the
reservation TTL is the backstop. The reservation key namespace is **distinct** from the permanent `dedup:`
record. `CheckIdempotencyActivity` remains the authoritative read path, and `SaveDedupKeyActivity` commits
permanent records as TTL-less first-writer-wins writes (`expiry: null`, `When.NotExists`).

## 4. Idempotent under at-least-once, unordered delivery (AC4)

Ingestion runs on at-least-once, unordered Dapr pub/sub. A duplicate or out-of-order ingest returns the **same**
`MemoryUnitId` without creating a second unit: `CheckIdempotencyActivity` short-circuits on the permanent
token-keyed record (precedence) or `sourceUri`-keyed record (fallback), exactly as the duplicate short-circuit
in `IngestionWorkflow`.

## 5. Cross-story dependency

Stories **18.5** (`sourceUri → MemoryUnitId` lookup endpoint) and **18.6** (`MemoryUnitId` stability) rely on the
permanent `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record. The idempotency token **augments** this record;
it never replaces it. Do not change the committed `sourceUri`-keyed record to TTL-bound or token-only.

## 6. `MemoryUnitId` stability — authoritative guarantee (Story 18.6)

The precise lifetime guarantee for the returned `MemoryUnitId` — exactly when re-ingestion of the same
`(tenantId, caseId, sourceUri)` returns the same id, why that depends on the permanent source-URI dedup record,
the loss/failure modes, and the consumer resolution path — is published in
[`memory-unit-id-stability.md`](./memory-unit-id-stability.md), which is the **authoritative** contract for
`MemoryUnitId` stability. In short: `MemoryUnitId` is an **opaque** id string (not derived from `sourceUri`, not
guaranteed to be a ULID), stable only while the committed `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record
persists.

**Consumer note.** For long-lived downstream correlation, store or resolve by `sourceUri` plus `tenantId`/`caseId`
where possible — resolve the current id on demand via
`MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` (Story 18.5) — and use `MemoryUnitId` as the graph/start-node
id once resolved, rather than maintaining an unbounded historical list of ids.
