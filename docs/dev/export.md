# Data export — developer reference

Developer-facing reference for the Memories Server's case and tenant export
streams. Describes the envelope shape, schema-versioning policy, streaming
behavior, operational guardrails, and known compromises.

Shipped in Story 8.3.

## Purpose

Export captures a portable JSON snapshot of a case or an entire tenant so
operators can back up knowledge, migrate data between deployments, or inspect
a tenant externally (e.g. with `jq`).

**What export captures:**

- Tenant configuration (status + registration timestamps + the existing
  operator-safe `TenantConfigurationView`, which redacts secret values — only
  secret-store key names appear).
- Every case record + its membership list.
- Every memory unit, including content, metadata, embedding provider/model,
  and failure details (when present), plus an `annotationTargets[]`
  projection listing the inbound annotations that point at the unit. The
  `classification` field is reserved on the export contract but remains null
  until the ingest/index path persists it upstream.
- Every graph edge — including the confidence-promotion audit trail
  (`verifiedBy`, `previousConfidence`) added by Story 4.3.
- Final `statistics` counts (memory units, edges, cases).

**What export does NOT capture:**

- Raw source bytes (PDFs, images). Only the extracted text.
- DAPR workflow state (in-flight ingestion, tenant deletion, consistency
  repair). Workflow state is ephemeral orchestration data.
- `AccessTelemetryEvent` audit logs (Story 7.5). Those are a stdout-only
  audit channel, not persisted in Redis/FalkorDB.
- Secret values. `TenantEmbeddingConfig.ApiSecretKeyName` is a secret-store
  identifier, never the secret itself.

## Endpoint summary

| Endpoint      | Path                                                | Consumer                                       | Success status           | Typical latency   |
| ------------- | --------------------------------------------------- | ---------------------------------------------- | ------------------------ | ----------------- |
| Case export   | `GET /api/v1/tenants/{tenantId}/cases/{caseId}/export` | `memories export case` CLI; operator scripts   | `200 OK` + streamed JSON | ~1 s per 1K units |
| Tenant export | `GET /api/v1/tenants/{tenantId}/export`                | `memories export tenant` CLI; operator scripts | `200 OK` + streamed JSON | ~1 s per 1K units |

All responses carry these headers:

- `Content-Type: application/json`
- `Content-Disposition: attachment; filename="{tenantId}-...-{snapshotAt}.json"`
- `X-Export-Schema-Version: 1`

Expected duration / size (rough baselines — calibrate in your deployment):

| Tenant size | Approx. duration | Approx. JSON size |
| ----------- | ---------------- | ----------------- |
| 1K units    | ~1 s             | ~5 MB             |
| 10K units   | ~10 s            | ~50 MB            |
| 100K units  | ~2 min           | ~500 MB           |
| 1M units    | ~20 min          | ~5 GB             |

## Schema reference

Exports are wrapped in a top-level object with these fields (in order, so
streaming parsers can consume them incrementally):

1. `manifest` — `ExportManifest` record.
2. `case` (case-scope only) or `tenant` + `cases[]` (tenant-scope only).
3. `memoryUnits[]` — array of `ExportedMemoryUnit` wrappers.
4. `edges[]` — array of `ExportedEdge` records.
5. `statistics` — `ExportStatistics` record.

### Worked example (case-scope)

```json
{
    "manifest": {
        "schemaVersion": 1,
        "scope": "case",
        "tenantId": "acme",
        "caseId": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
        "exportedAt": "2026-04-20T10:15:30.0000000+00:00",
        "snapshotAt": "2026-04-20T10:15:30.1234567+00:00"
    },
    "case": {
        "id": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
        "tenantId": "acme",
        "name": "Q1 Planning",
        "status": "active",
        "createdAt": "2026-02-01T09:30:00+00:00",
        "lastUpdated": "2026-04-15T10:00:00+00:00",
        "memoryUnitCount": 3,
        "members": [
            {
                "memberId": "alice@acme.com",
                "memberType": "user",
                "addedAt": "2026-02-02T00:00:00Z"
            }
        ]
    },
    "memoryUnits": [
        {
            "unit": {
                "id": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
                "tenantId": "acme",
                "caseId": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9",
                "content": "Observation text",
                "contentHash": "sha256:abc",
                "sourceUri": "file:///obs.md",
                "sourceType": "file",
                "ingestedBy": "alice",
                "ingestedAt": "2026-02-15T14:00:00+00:00",
                "lastUpdated": "2026-02-15T14:00:00+00:00",
                "status": "indexed",
                "metadata": {},
                "embeddingProvider": "google",
                "embeddingModel": "gemini-embedding-001",
                "embeddingDimensions": 768
            },
            "annotationTargets": []
        }
    ],
    "edges": [
        {
            "id": "4273",
            "sourceId": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X0",
            "targetId": "01HM5Q9WXGK6T8Q4Z5Y6V7W8X1",
            "edgeType": "causedBy",
            "confidence": 0.95,
            "origin": "inferred",
            "createdAt": "2026-02-15T14:01:00+00:00",
            "verifiedBy": null,
            "previousConfidence": null
        }
    ],
    "statistics": {
        "memoryUnitCount": 1,
        "edgeCount": 1,
        "caseCount": 1
    }
}
```

## Schema versioning

- `schemaVersion = 1` is emitted as the FIRST field of the manifest so
  consumers can dispatch after reading only the first KB.
- **Additive** changes (new fields on existing records) keep the version at
  `1`. Consumers must ignore unknown fields.
- **Breaking** changes (removed or renamed fields, changed value semantics)
  bump the version to `2`.

## Snapshot semantics

Snapshot isolation is advisory. At export start the server captures
`snapshotAt = DateTimeOffset.UtcNow` and uses it as a filter:

- Memory units with `ingestedAt <= snapshotAt` are included; newer units are
  skipped.
- Edges with `createdAt <= snapshotAt` are included; newer edges are skipped.
- A 500 ms tolerance window is applied to absorb typical NTP drift across
  pods. Operators lose ~500 ms of just-ingested units from the export in
  exchange for not having to guarantee cross-pod clock sync.

**Known limitation (Risk #2):** a unit whose ingestion committed before
`snapshotAt` but whose graph indexing completes after may be emitted without
its edges. For compliance-critical archives, run
`memories consistency verify` before the export and confirm the tenant is
consistent — the export captures the state as-is, divergences included.

## Case-scope edge semantics

Case-scope export emits every edge where either the source OR the target
memory unit belongs to the case. The "far" endpoint (outside the case) is
referenced by id only — it is NOT resolved into a full
`ExportedMemoryUnit`.

Consequence: a future re-importer MUST handle the "dangling target" case.

## Known Compromises

1. **Cross-case edges in case-scope exports produce dangling `targetId`
   references.** A future re-importer must resolve these explicitly (fetch
   the target from another tenant/case, create a stub node, or reject the
   edge).
2. **`ExportedEdge.Id` is graph-instance scoped.** FalkorDB assigns edge ids
   within a graph lifetime; they are NOT stable across graph deletions or
   recreations. A re-importer MUST NOT use `Id` as edge identity —
   reconstruct edges from the
   `(SourceId, TargetId, EdgeType, CreatedAt)` tuple.
3. **Snapshot isolation is advisory.** In-flight indexing can produce
   observable divergence between `memoryUnits[]` and `edges[]` for a unit
   whose ingestion committed before `snapshotAt` but whose edge indexing
   completed after. See "Snapshot semantics" above.
4. **Raw source bytes are not exported.** Only the extracted text appears
   in `content`. If a compliance export needs the original PDF / image, the
   operator must capture that separately from the source store.
5. **`classification` is contract-reserved but not yet persisted upstream.**
   `MemoryUnit.Classification` exists on the export contract, but the current
   ingest/index path does not write it into Redis, so exports cannot
   reconstruct it yet. A future story must persist classification before
   export can round-trip it.
6. **Export endpoints are NOT in the AccessTelemetryEvent scope
   (Story 7.5).** Export is a data-exfiltration surface, but a dedicated
   audit channel is out of MVP scope — a follow-up story will add an
   `ExportTelemetryEvent` bank (EventId 8320-8329 reserved). MVP relies on
   ASP.NET Core request logs captured by the host's log aggregator.

## Streaming behavior

The server streams the response directly from
`HttpContext.Response.BodyWriter`. `Utf8JsonWriter` flushes every 1000
memory units or 1 MiB of pending bytes, whichever comes first. The server
never materializes the full export in memory.

Consumers should expect incremental availability: the manifest lands within
milliseconds, well before enumeration completes. A streaming consumer can
read the manifest, inspect `schemaVersion` + `scope`, and then dispatch to
an incremental parser for `memoryUnits[]` + `edges[]`.

## CLI walkthrough

```sh
# Stream a full tenant export straight to stdout and pipe through jq:
memories export tenant --tenant acme | jq .manifest

# Write a case export to a file (refuses to overwrite without --force):
memories export case --tenant acme --case 01HM5Q9WXGK6T8Q4Z5Y6V7W8X9 --output case-1.json

# Overwrite an existing file:
memories export case --tenant acme --case 01HM5Q9WXGK6T8Q4Z5Y6V7W8X9 --output case-1.json --force

# Write outside the current working directory (safety opt-in):
memories export tenant --tenant acme --output /var/backups/tenant.json --allow-absolute-path
```

Progress is emitted to **stderr** (stdout carries the raw JSON payload).
Every 64 KiB of streamed bytes produces a `Exported X.YY MB` line. The CLI
writes to a `.part` file first and atomically renames on success; on
failure the `.part` file is deleted so no truncated output is left behind.

The CLI's global `--format` option is **ignored** for export. A warning is
printed to stderr if `--format=json` or `--format=table` is supplied; the
payload is raw JSON regardless.

## Error handling

Errors that occur **before** the response starts streaming are returned as a
structured `ErrorResponse` body with `Content-Type: application/json`:

| HTTP | Code                | Trigger                             |
| ---- | ------------------- | ----------------------------------- |
| 400  | `INVALID_TENANT_ID` | Tenant id fails the regex guard.    |
| 400  | `INVALID_CASE_ID`   | Case id fails the ULID regex guard. |
| 404  | `TENANT_NOT_FOUND`  | Tenant absent from the registry.    |
| 404  | `CASE_NOT_FOUND`    | Case absent in the tenant.          |

Errors **mid-stream** (backend connection loss, writer failure) cannot
produce a structured response — headers are already committed. The server
logs the failure at `Error` (EventId 8311 `ExportFailed`) and the client
observes a truncated response. A missing closing `}` or a missing
`statistics` block are reliable truncation indicators.

The CLI's `--output` path deletes the `.part` file on failure so callers
never see a half-written final file.

## Operational guardrails

- **Rate limiting** is NOT enforced at the endpoint level in MVP. Export is
  a DoS vector (long-running, reads every MU and edge). Operators are
  expected to gate these endpoints via network policy, reverse-proxy auth,
  or deployment-scoped throttling. A future story can add
  `Microsoft.AspNetCore.RateLimiting` policies if public exposure becomes
  needed.
- **Off-peak windows** are recommended for tenant-scale exports. A 1M-unit
  export can hold a FalkorDB connection for ~20 minutes.
- **Tenant lifecycle** — export is allowed for tenants in non-Active states
  (Provisioning, Deleting, Failed) so operators can back up or inspect
  tenants outside the happy path.

## Relation to Story 8.2 (Consistency)

Export is orthogonal to consistency. For compliance-critical archives, run
`memories consistency verify --tenant <id>` first and confirm no
discrepancies. If the tenant is divergent, the export captures the
divergence as-is — a re-import later will reproduce it.

## Relation to Story 7.5 (Access Telemetry)

Export is explicitly NOT in the `AccessTelemetryEvent` scope (Story 7.5
enumerates search, ingest, traverse, and case-access as the audited
operations). If operators need an audit trail of exports, a follow-up story
will ship a dedicated `ExportTelemetryEvent` channel.

## Phase-2 migration path

If resumable exports, scheduled exports, or exports that outlive a single
HTTP connection become required, the 8.3 shape is replaced by a
workflow-backed variant:

- `POST /api/v1/tenants/{tenantId}/export` returns a workflow instance id.
- `GET /api/v1/tenants/{tenantId}/export/{instanceId}` polls status.
- On completion a pre-signed blob URL is returned.

**The JSON schema stays v1** — only the transport envelope changes.

## Out of scope (deferred)

- Re-import / restore.
- Incremental / delta exports.
- Cross-tenant "platform dump" exports.
- Export-specific compression (the ASP.NET Core response-compression
  middleware, if enabled, transparently gzips).
- Binary content embedding.
- Signed / checksummed exports.
- Server-Sent Events (SSE) progress channel.
- MCP tool for export (Epic 10).
- `AccessTelemetryEvent` integration + dedicated `ExportTelemetryEvent`
  bank (EventId 8320-8329 reserved).
- Endpoint-level rate limiting.
- `CounterpartWorkflowInstanceId` manifest field (reserved for schema v2).

See the Story 8.3 spec for the full "What does NOT ship" list with
justifications.

