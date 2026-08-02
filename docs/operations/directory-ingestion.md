# Directory Ingestion Operations

## Endpoint, authorization, and enablement

`POST /api/v1/ingest/directory` schedules one ingestion workflow per accepted
server-local file and returns `202 Accepted` with a batch ID and status location.
The route requires an authenticated caller authorized for the body-bound tenant,
passes through the inbound tenant rate limiter, and rejects a tenant that is not
active. This is server-filesystem ingestion; the caller does not upload a client
directory.

Directory ingestion is disabled by default because
`Ingestion:AllowedDirectoryRoots` defaults to an empty list. Enable it only with
absolute roots that the server process may read. An empty list returns
`403 DIRECTORY_INGESTION_DISABLED`.

`GET /api/v1/ingest/batches/{batchId}` reads the persisted batch owner and authorizes
the caller for that tenant before returning status. A missing or expired record
returns `404 BATCH_NOT_FOUND`. See the complete [route-surface contract](./route-surface.md)
for Dapr service-invocation mapping.

## Filesystem safety and filtering order

The service requires an absolute, existing directory beneath an allowed root. It
canonicalizes the requested directory, every configured root, and enumerated files;
reparse points/symbolic links are resolved to their final targets. A file whose final
path escapes the requested canonical directory is skipped as `OUTSIDE_ROOT`.
Comparison is ordinal on Unix and ordinal-ignore-case on Windows. Invalid,
unresolvable, inaccessible, or I/O-failing directory traversal returns
`INVALID_DIRECTORY_PATH` or records an `INVALID_PATH`/`FILE_UNREADABLE` skip as
appropriate.

Filtering is deliberately fail-closed and occurs before file bytes are read:

1. Normalize the extension to lowercase with a leading dot.
2. Require membership in `Ingestion:SupportedExtensions`.
3. Apply `Ingestion:UnsupportedExtensions` as a stricter overlay: membership there
   wins even if the same extension is supported.
4. Read file metadata and reject empty or oversized content.
5. Only then read bytes and prepare the workflow payload.

The default supported set is `.md`, `.txt`, `.pdf`, `.docx`, `.doc`, `.html`,
`.htm`, `.xlsx`, `.xls`, `.pptx`, `.ppt`, `.csv`, `.json`, `.rtf`, and `.epub`.
The default unsupported overlay is `.exe`, `.dll`, `.bin`, `.iso`, `.dmg`, `.so`,
`.dylib`, `.app`, `.msi`, `.deb`, and `.rpm`. Skip reasons are
`UNSUPPORTED_EXTENSION`, `EMPTY_FILE`, `PAYLOAD_TOO_LARGE`, `FILE_UNREADABLE`,
`INVALID_PATH`, and `OUTSIDE_ROOT`.

## Bounds and batch-state checkpoints

| Setting | Default | Effective behavior |
|---------|---------|--------------------|
| `Ingestion:MaxBatchSize` | `500` | Reject with `BATCH_TOO_LARGE` when accepted candidates exceed the configured count. |
| `Ingestion:MaxSkippedReportSize` | `100` | Retain at most this many skip-detail rows; continue counting discovery and set `SkippedTruncated` in the POST result when more are omitted. |
| `Ingestion:DirectorySchedulingParallelism` | `4` | Clamp to `1..32` concurrent scheduling workers. |
| `Ingestion:DirectoryBatchCheckpointSize` | `50` | Clamp to `1..250` successful schedules between progress checkpoints. |
| `Ingestion:BatchStateTtlHours` | `24` | Persist batch state with an effective TTL of `max(1, configured)` hours. |

The service sorts accepted candidate paths ordinally before scheduling, but concurrent
workers can complete out of order. Before every persisted snapshot it sorts scheduled
file rows by source URI using `StringComparer.Ordinal` and rebuilds the instance-ID
array in the same order. Consequently, the final successful POST result and persisted
scheduled-file inventory have deterministic source-URI order.

The state-store sequence is:

1. Persist an initial record after discovery/filtering and before scheduling. If this
   fails, return `503 BATCH_TRACKING_UNAVAILABLE` and schedule nothing.
2. Persist bounded progress after each configured number of successful schedules and
   after a file becomes unreadable during its later byte read.
3. Persist a final snapshot after all candidates finish. Failure of that write returns
   `BATCH_TRACKING_UNAVAILABLE` rather than reporting an untrackable successful batch.

On a scheduling failure, the service cancels remaining bounded scheduling work,
attempts a cancellation-independent progress snapshot, and returns
`DAPR_UNAVAILABLE` or `BATCH_SCHEDULING_FAILED`. Already accepted workflows are not
rolled back and may continue.

## Response counts and status lookup

The successful POST response contains:

- `Discovered`: every file yielded by enumeration, before path, extension, size, or
  readability rejection;
- `Enqueued`: workflows successfully scheduled;
- `Skipped`: the bounded detail list, not an authoritative total after truncation;
- `SkippedTruncated`: whether additional skip rows were omitted;
- `InstanceIds`: scheduled workflows in deterministic source-URI order.

When `SkippedTruncated` is true, do **not** infer a total skip count from
`Skipped.Count`, and do not require `Discovered == Enqueued + Skipped.Count`.
The persisted batch state stores only the bounded skip rows, not the truncation flag.
Therefore the later status response's `Skipped` field is also the retained report-row
count, not the total number skipped when the original POST response was truncated.
Preserve the POST response as evidence.

The status endpoint reads workflow state with at most 50 concurrent lookups. It reports
missing/pending/suspended workflow state as `queued`, running or otherwise unclassified
state as `extracting`, completed output by its memory-unit status, and failed/terminated
state as `failed`. Its aggregate `Counts` are derived from those per-instance rows;
they are a lookup-time view, not proof that the entire batch reached a terminal state.

## Cancellation, partial failure, and payload cleanup

Caller cancellation can occur after the initial or a progress snapshot. It propagates
instead of producing a successful final response, so the last persisted snapshot may
lag successfully scheduled work. Workflows already accepted by Dapr remain scheduled.
Reconcile with server logs and workflow state before retrying the directory.

For a candidate whose source bytes were moved into the payload store but whose workflow
was not accepted, the worker attempts to delete that claim-checked payload on Dapr
failure, ordinary scheduling failure, or cancellation. This cleanup is **best effort**:
deletion failure is logged and TTL expiry remains the backstop. The service does not
delete payloads owned by workflows that were successfully scheduled.

A file that becomes unreadable during byte loading is recorded as skipped and does not
abort its peers unless the resulting checkpoint cannot be persisted. Any batch-level
non-success can coexist with already scheduled workflows; do not assume an error
response means zero side effects.

## Related guidance and sources

- [Rate limiting and shared provider admission](./rate-limiting.md)
- [Failure recovery and re-ingestion](./failure-recovery.md)
- [Invocable route and operation surface](./route-surface.md)
- [`DirectoryIngestionService`](../../src/Hexalith.Memories.Server/Ingestion/DirectoryIngestionService.cs)
- [`IngestionSettings`](../../src/Hexalith.Memories.Server/Ingestion/IngestionSettings.cs)
