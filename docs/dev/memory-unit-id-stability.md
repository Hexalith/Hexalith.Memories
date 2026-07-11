<!-- Review cadence: update when `IngestionWorkflow.ResolveMemoryUnitId`, `DedupKeyBuilder.BuildKey`/`BuildTokenKey`, `SaveDedupKeyActivity`'s `expiry` argument, `SourceUriMemoryUnitLookup`, or `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` changes; when a dedup-record retention/TTL/deletion policy is introduced; or when the `MemoryUnitId` generation shape changes; otherwise quarterly — whichever comes first. Last reviewed: 2026-06-25. -->

# MemoryUnitId Stability Contract (Story 18.6)

This document is the authoritative description of **when a `MemoryUnitId` is stable**, **what guarantees that stability**, and **how a downstream consumer should resolve a memory unit durably** without accumulating unbounded local id state. It exists so a consumer that keys its own per-entity mapping on `MemoryUnitId` (e.g. the Parties projection's `PartyMemoryUnitMappingStore`) cannot silently drift into a pile of ghost ids after a Memories restart or a contract change.

- **Status:** Stable contract (documentation + drift-guarded). No public API addition; this publishes existing behavior.
- **Origin:** MEM-6 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27).
- **Coupling:** Contract-coupled with **Story 18.5** — the source-URI lookup endpoint is the authoritative consumer resolution path; this document specifies the lifetime guarantee that lookup relies on.

> **Code is the source of truth.** Every claim below is mirrored from the authoritative source file named in its section. A content-asserting drift-guard test (see [Automated enforcement](#automated-enforcement)) fails the build if the documented guarantee diverges from the code paths that build, read, and write the source-URI dedup record.

---

## 1. What `MemoryUnitId` is (and is not)

`MemoryUnitId` is an **opaque id string**. Consumers MUST treat it as an unparseable token:

| Property | Value | Authoritative source |
| :--- | :--- | :--- |
| Type | Opaque `string` | `IngestionWorkflow.ResolveMemoryUnitId` |
| Derived from `sourceUri`? | **No** — it is **not derived from `sourceUri`** | `IngestionWorkflow.ResolveMemoryUnitId` |
| Guaranteed ULID / time-sortable? | **No** — it is **not guaranteed to be a ULID** and carries no ordering guarantee | `IngestionWorkflow.ResolveMemoryUnitId` |
| Today's concrete shape | The Dapr workflow instance id (a GUID/ULID-like string supplied by the host) for ordinary file/url ingests, or a fresh `context.NewGuid().ToString()` for `dedup:`-prefixed EventStore workflows | `IngestionWorkflow.ResolveMemoryUnitId` |

> **Do not promise a ULID.** Earlier architecture projections described the memory unit `Id` as a ULID, but live code and the current architecture supersede that wording: `ResolveMemoryUnitId` returns `context.InstanceId` (or a `NewGuid` string), neither of which is parsed, validated, or guaranteed to be a ULID. Treat `MemoryUnitId` as **opaque** and resolve it through the lookup below rather than reconstructing it.

### EventStore `dedup:`-prefixed workflow instance ids

For EventStore integration, a workflow can run under a `dedup:`-prefixed instance id. `ResolveMemoryUnitId` deliberately does **not** reuse a `dedup:` key as the memory id for `SourceType.Event`; it mints an independent id with `context.NewGuid().ToString()`. This keeps the dedup key (a routing/identity key) separate from the memory-unit id, so the memory id is never the dedup key itself.

## 2. The stability guarantee

For a given `(tenantId, caseId, sourceUri)`, Memories returns the **same canonical `MemoryUnitId`** on re-ingestion **for as long as the committed source-URI dedup record persists**:

```
dedup:{tenantId}:{caseId}:{sha256(sourceUri)}  ->  MemoryUnitId
```

The id is **not derived from `sourceUri`**; it is the **stored value** of that dedup record. `CheckIdempotencyActivity` reads the record and the `IngestionWorkflow` duplicate short-circuit returns the existing `MemoryUnitId` **without re-indexing**, so a duplicate or out-of-order re-ingest resolves to the same unit.

This record is **permanent (TTL-less)** and first-writer-wins: `SaveDedupKeyActivity` writes it with `expiry: null` and `When.NotExists`. The stability guarantee is therefore exactly as durable as that record — no more, no less.

### Lifetime dependency (the load-bearing invariant)

The guarantee depends on the source-URI dedup record being permanent. The following changes would **weaken or break** the guarantee and must only be made deliberately, with this contract updated in the same change:

- Changing `SaveDedupKeyActivity` to write a non-null `expiry` (TTL-bound the record) or to overwrite an existing winner.
- Deleting `dedup:*` records during normal retention or cleanup.
- Replacing the source-URI record with **token-only** dedup (see §4 — the token record augments, never replaces, this record).
- A key-format change to `DedupKeyBuilder.BuildKey` that does not migrate existing records.

## 3. Failure / loss modes (risk is documented, not hidden)

If the committed source-URI dedup record is lost or incompatibly mutated, a later ingest of the **same** `sourceUri` can mint a **new** `MemoryUnitId`. The known boundaries:

- **Redis eviction** of `dedup:*` keys under memory pressure.
- **Operator or manual deletion** of `dedup:*` keys.
- **TTL expiry** if a future retention policy makes the record TTL-bound.
- **Incompatible key-format change** that does not migrate old records.
- **Cross-environment reindex / migration** where old Redis state is not carried forward.

> **The dedup record is the id-resolution authority — not the backend index.** Backend index presence alone (syntactic / semantic / graph) does **not** make a `MemoryUnitId` recoverable: those backends are keyed *by* `MemoryUnitId`, so they cannot re-derive it from a `sourceUri`. Only the `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record maps a source URI back to its canonical id.

These are **contract boundaries**, not implementation tasks for this story. Introducing any dedup-record TTL or deletion policy is a deliberate, separate decision that must reckon with this guarantee.

## 4. Token semantics remain intact (Story 18.4)

Story 18.4 added explicit idempotency tokens. They **augment, never replace** the source-URI record:

- A token-supplied ingest writes a token-keyed record `dedup:{tenantId}:{caseId}:tok:{sha256(token)}` **in addition to** the source-URI record above. Both point at the **same** `MemoryUnitId`.
- `CheckIdempotencyActivity` checks the token-keyed record **first** (precedence), then falls back to the source-URI record.
- The source-URI record remains the **cross-story identity mapping** used by the Story 18.5 lookup and by this stability guarantee. Token-only dedup would break both.

## 5. Authoritative consumer resolution path (Story 18.5)

Downstream consumers SHOULD resolve the current id through the Story 18.5 lookup rather than maintaining an unbounded historical list of `MemoryUnitId`s:

- **Client:** `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync(tenantId, caseId, sourceUri, ct)` — returns `string?` (`null` on a structured 404 miss; throws for other non-success statuses, never silently a miss).
- **Route:** `GET /api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri?sourceUri=...` — a deterministic keyed read over the permanent dedup record, **not** a free-text search.

### When to key/dedup by `sourceUri` instead of only `MemoryUnitId`

Retain or recompute the **source identity** (`tenantId`, `caseId`, `sourceUri`) and resolve the id on demand when the consumer must survive:

- a **dedup-record loss** (eviction, manual deletion);
- a **retention reset** or a future TTL policy on the dedup record;
- a **cross-environment reindex / migration**.

Keep `MemoryUnitId` for graph traversal and memory-unit APIs **after** resolution; do not treat an accumulated list of historical `MemoryUnitId`s as the primary identity store. That is precisely the unbounded-growth / ghost-id failure this contract exists to prevent (a `MemoryUnitId`-keyed mapping that never reconciles can exceed the Dapr state-store value-size limit).

## 6. Parties "decision D1" is not Memories Architecture Decision D1

The Parties-side **"decision D1"** label (raised in the consumer intake) is **unrelated** to Memories **Architecture Decision D1**, which is *"FalkorDB for MVP"*. They share a label only by coincidence. Cross-repo discussions about `MemoryUnitId` stability must **not** cite Memories Architecture Decision D1 (FalkorDB for MVP) as if it governed id stability — it does not.

## Automated enforcement

A content-asserting drift-guard test protects this contract:
[`tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs`](../../tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs). It runs on every build (plain `[Fact]`s, no Docker/fixture, repo-root marker walk) and enforces:

- **Doc presence + mandatory claims:** this document exists and contains the guarantee key form `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`, the TTL-less marker `expiry: null`, the lookup method `LookupMemoryUnitIdBySourceUriAsync`, the token-record form `dedup:{tenantId}:{caseId}:tok:{sha256(token)}`, and the opaque/not-source-derived/not-ULID claims.
- **Doc ↔ code tie (the anti-drift guard):** `SaveDedupKeyActivity.cs` still writes `expiry: null` with `When.NotExists`; `DedupKeyBuilder.cs` still builds `dedup:{tenantId}:{caseId}:` for the source-URI key and keeps the `:tok:` namespace for the token key; `SourceUriMemoryUnitLookup.cs` still resolves via `DedupKeyBuilder.BuildKey`. A code-side change to any of these fails the build unless this document is reconciled.
- **D1 clarification tie:** this document keeps the statement that Parties "decision D1" is **not** Memories Architecture Decision D1 (FalkorDB for MVP).

The id-generation, duplicate short-circuit, and dual permanent-record behaviors are additionally covered by `Workflows/IngestionWorkflowTests.cs`, the token-key shape by `Activities/Ingestion/DedupKeyBuilderTests.cs`, the TTL-less first-writer-wins write by `Activities/Ingestion/SaveDedupKeyActivityTests.cs`, and the lookup read by `Ingestion/SourceUriMemoryUnitLookupTests.cs`.

## References

- Story 18.6 — MemoryUnitId Stability Contract (this document).
- MEM-6 — Parties consumer integration intake (Sprint Change Proposal 2026-05-27): document/guarantee the stability contract and its dedup-lifetime dependency.
- [`./ingest-contract.md`](./ingest-contract.md) — Story 18.4 stable ingest contract; token precedence and the augment-never-replace rule.
- [`../operations/route-surface.md`](../operations/route-surface.md) — Story 18.5 `GET .../memory-units/by-source-uri` route row.
- [`./public-surface-stability.md`](./public-surface-stability.md) — companion Story 18.1 additive-only stability posture.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — `ResolveMemoryUnitId`; duplicate short-circuit; permanent source-URI and token-keyed dedup writes.
- `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs` — `BuildKey` (`dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`) and `BuildTokenKey` (`:tok:`).
- `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs` — token precedence, source-URI fallback, transient-reservation exclusion.
- `src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs` — the TTL-less first-writer-wins permanent dedup write (`expiry: null`, `When.NotExists`).
- `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` — exact source-URI lookup over the permanent dedup record.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — `LookupMemoryUnitIdBySourceUriAsync`, the consumer-facing resolution path.
