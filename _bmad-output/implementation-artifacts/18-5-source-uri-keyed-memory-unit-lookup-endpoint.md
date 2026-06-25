---
baseline_commit: c4af9b0cdc9e956cff806a9b01711918189e7581
---
# Story 18.5: Source-URI-Keyed Memory-Unit Lookup Endpoint

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 — Downstream Consumer Integration Contract Hardening |
| Story key | `18-5-source-uri-keyed-memory-unit-lookup-endpoint` |
| Origin | MEM-5 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27, pass 9-6 chunk A) |
| Lifecycle track | Engineering / Operational Readiness — Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **Additive `feat`.** New public `MemoriesClient` method + new `Contracts.V1` response record + new REST route — all additive, non-breaking (minor bump). Use `feat(client):` / `feat(server):`. **NOT** release-timing-gating (only Story 18.4 was). No `tools/release-packages.json` edit (new types ship inside already-packable `Hexalith.Memories.Contracts` / `Hexalith.Memories.Client.Rest`); no `public-surface-stability.md` change (project/assembly/namespace unchanged). |
| Deliverable | A tenant- and case-scoped **exact source-URI → canonical `MemoryUnitId` lookup endpoint** that reads the existing permanent dedup record as the authoritative index (no parallel store), exposed via `MemoriesClient` (+ CLI diagnostic command), returning a structured not-found instead of a best-effort search hit. |
| Coupling | **Mutually dependent with Story 18.6** (`MemoryUnitId` stability). This endpoint's correctness rests on the dedup record's `sourceUri → MemoryUnitId` permanence that 18.6 documents. Land 18.5 with — or after — 18.6's contract text (Epic 18 sequencing note). |
| Parties-side follow-up | Parties switches `MemoriesPartySearchService.ResolveGraphStartNodeIdAsync` from the free-text URN search to the keyed lookup. |

## Story

As a downstream service resolving a graph start node from a source URI,
I want an exact source-URI-keyed lookup that returns the canonical `MemoryUnitId`,
so that graph mode no longer silently degrades to local mode when the canonical match falls outside a free-text search's top hits.

## Acceptance Criteria

1. **Exact keyed lookup, not a search hit.** A tenant- and case-scoped endpoint resolves a known `sourceUri` to its canonical `MemoryUnitId` by **exact key** (not by ranked/free-text search), returning a **structured not-found** result (`404` + `ErrorResponse` with code `MEMORY_UNIT_NOT_FOUND`) when no committed unit exists for that `(tenantId, caseId, sourceUri)`. The endpoint MUST NOT delegate to the search engine. _(Epic AC1)_

2. **Reuse the dedup record as the authoritative index — no parallel store.** The lookup resolves through the existing permanent dedup record `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}` (built by `DedupKeyBuilder.BuildKey`, read from the keyed `redis` `IConnectionMultiplexer`). It MUST NOT introduce a new/parallel `sourceUri → MemoryUnitId` index. _(Epic AC2)_

3. **Transient reservation is excluded.** When the dedup key currently holds the transient in-flight reservation marker (`PreflightDedupReservation.ReservedValue` / `IsTransientReservation(...)` → `true`), the lookup treats it as **not-found** — it never returns the `"reserved"` marker as if it were a `MemoryUnitId`. _(Refinement; mirrors `CheckIdempotencyActivity`)_

4. **Public client + CLI exposure, additive contract.** The capability is exposed through `MemoriesClient` (concrete, `public virtual`, **no** `IMemoriesClient` — Architecture Decision D9) and a CLI diagnostic command. The new request/response shape is **additive** to `Contracts.V1` (camelCase wire, registered in the source-gen JSON context). **MCP exposure is deliberately declined** (operational/diagnostic resolution, not an LLM-agent user-facing task; documented in Dev Notes). _(Epic AC3 — "as appropriate")_

5. **Tenant isolation + cross-tenant rejection.** Tenant id format is validated (`TenantIdGuard.Validate` → `400 INVALID_TENANT_ID`). Because the dedup key embeds `tenantId` and `caseId`, a lookup under tenant B (or case Y) for a `sourceUri` ingested under tenant A (or case X) resolves to **not-found**. Covered by success, not-found, and **cross-tenant-rejection** tests. _(Epic AC3 + project tenant-isolation rules)_

6. **Backend-unavailability does not become a false not-found.** On a Redis read failure the endpoint returns a **structured backend error** (not `404`), so a consumer never mistakes a transient outage for "no unit" and re-ingests into a duplicate. (Contrast with the ingest path's fail-open posture, ADR 9.1-B — for an identity-resolving read, fail-open to not-found is wrong.) _(Refinement)_

7. **Published surface stays drift-guarded.** The new `/api/*` route is added to `docs/operations/route-surface.md` (with method, path, and Dapr/operation semantics), keeping `RouteSurfaceContractTests` green — the count-tie and forward code→doc tie derive the route list from `Program.cs`, so an undocumented new route fails the build. _(Story 18.3 guard)_

8. **Ledger + tests.** `MEM-5` in `deferred-work.md` flips `carried-forward → resolved` with an `Evidence:` line (Story 14.5 schema). All new/changed code is covered by unit + contract + endpoint tests and the full affected test projects pass. _(Process)_

## Tasks / Subtasks

- [x] **Task 1 — Lookup seam over the dedup record** (AC: 2, 3, 6)
  - [x] Add `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` (NEW) — a small testable seam mirroring `IngestDedupReservation.cs`: ctor `([FromKeyedServices("redis")] IConnectionMultiplexer redis)`; method `Task<string?> ResolveMemoryUnitIdAsync(string tenantId, string caseId, string sourceUri, CancellationToken ct)`.
  - [x] Build the key with `DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri)` (reuse — do **not** re-implement the hash). Read with `_redis.GetDatabase().StringGetAsync(key)`.
  - [x] Exclude the transient marker: `if (PreflightDedupReservation.IsTransientReservation(value.ToString())) return null;` (AC3). Return `value.HasValue ? value.ToString() : null` (AC2).
  - [x] Validate inputs with `ArgumentException.ThrowIfNullOrWhiteSpace`. Let `RedisException`/`RedisConnectionException` propagate so the endpoint maps them to a backend error (AC6) — do **not** swallow them into `null`.
  - [x] Register the seam in `Program.cs` DI (next to `IngestDedupReservation`).
- [x] **Task 2 — Response contract (additive)** (AC: 1, 4)
  - [x] Add `src/Hexalith.Memories.Contracts/V1/MemoryUnitIdLookupResponse.cs` (NEW) — `public sealed record MemoryUnitIdLookupResponse { public required string MemoryUnitId { get; init; } }` with XML docs + ITANEO header.
  - [x] Add `[JsonSerializable(typeof(MemoryUnitIdLookupResponse))]` to `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`. (No request record needed — `sourceUri` travels as a query-string parameter on a GET; keep the surface minimal.)
- [x] **Task 3 — REST endpoint** (AC: 1, 3, 5, 6)
  - [x] Add `app.MapGet("/api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri", …)` in `Program.cs` (mirror the GetMemoryUnit handler at `Program.cs:1690`). Accept `sourceUri` as a `[FromQuery]`/query-bound string.
  - [x] Validate tenant via `TenantIdGuard.Validate` → `400 INVALID_TENANT_ID` (AC5). Validate `sourceUri` non-blank → `400`.
  - [x] Call `SourceUriMemoryUnitLookup.ResolveMemoryUnitIdAsync`. `null` → `Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", …))` (AC1). Value → `Results.Ok(new MemoryUnitIdLookupResponse { MemoryUnitId = id })`.
  - [x] Wrap in `EndpointTelemetryScope` like neighboring read endpoints; record `sourceUri` only as a low-cardinality query tag (do not log secrets). Allocate a **fresh, distinct** success/error event-id pair — do **not** reuse GetMemoryUnit's `7504`/`7514`.
  - [x] Map Redis failure to a structured backend error (e.g. `503` + `ErrorResponse("LOOKUP_BACKEND_UNAVAILABLE", …)`), **not** `404` (AC6).
  - [x] Confirm the literal segment `by-source-uri` is selected over the sibling `{memoryUnitId}` template (ASP.NET literal-beats-parameter precedence) — assert it in an endpoint test (AC1).
- [x] **Task 4 — Public client method** (AC: 4)
  - [x] Add `public virtual async Task<string?> LookupMemoryUnitIdBySourceUriAsync(string tenantId, string caseId, string sourceUri, CancellationToken ct)` to `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (mirror `GetMemoryUnitAsync`, `MemoriesClient.cs:215`). `Uri.EscapeDataString` all segments + the `sourceUri` query value; deserialize via `MemoriesJsonContext.Options`; `404 → null`; other non-2xx → `MemoriesRemoteException`. Keep it concrete/`virtual` (D9), `ConfigureAwait(false)`.
- [x] **Task 5 — CLI diagnostic command** (AC: 4)
  - [x] Add `src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs` (NEW) — `memories search lookup --tenant --case --source-uri`, mirroring `SearchInspectCommand.cs` for option/executor wiring and output. Route output through `OutputFormatterRouter` (human + json); missing/blank args → `CliErrorWriter` + `CliExitCodes.Plumbing`. **Not-found path is new, not copied:** `SearchInspectCommand` calls `GetMemoryUnitAsync`, which *throws* `MemoriesRemoteException` on 404 — so it has no not-found branch to mirror. The new client method returns `string?` (null on 404), so handle `null → CliExitCodes.NotFound` explicitly in this command.
  - [x] Wire it into the `search` command group in the CLI root/command factory (next to `search inspect`).
- [x] **Task 6 — Published route surface + drift guard** (AC: 7)
  - [x] Add the new route row to `docs/operations/route-surface.md` (method `GET`, path, purpose, Dapr/operation semantics) and bump the prose "45" counts (lines ~7, ~24, ~119, ~143) to "46". The count-tie test derives the source count automatically and will fail until the row exists.
- [x] **Task 7 — Tests** (AC: 1, 3, 5, 6, 8)
  - [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs` (NEW) — mirror `IngestDedupReservationTests`: hit returns id; miss returns null; `"reserved"` marker → null (AC3); Redis throw propagates (AC6). Substitute `IConnectionMultiplexer`.
  - [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs` (NEW) — `200` success; `404` structured not-found; `400` invalid tenant; cross-tenant → not-found (AC5); different-case → not-found; transient-reserved → not-found (AC3); Redis-down → backend error not `404` (AC6); literal-route precedence.
  - [x] `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs` (NEW) — camelCase round-trip (`memoryUnitId`).
  - [x] Client tests in `tests/Hexalith.Memories.Cli.Tests/ClientRest/` (extend the `MemoriesClient` suite) — path/encoding, `200 → id`, `404 → null`, error → `MemoriesRemoteException`, via `TestDelegatingHandler` (D9 `HttpClient` seam).
  - [x] `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs` (NEW) — missing arg → plumbing; found → prints id + success; not-found → `NotFound` exit.
- [x] **Task 8 — Ledger** (AC: 8)
  - [x] Flip `MEM-5` in `_bmad-output/implementation-artifacts/deferred-work.md` to `Status: resolved` and add an `Evidence:` line (mirror the MEM-4 worked example) naming the endpoint, client method, contract, and proving tests. Keep the schema well-formed (`CiTestInventoryTests` parses it).

## Dev Notes

### Scope and intent (read first)

This story exposes an **exact, deterministic** `sourceUri → MemoryUnitId` resolution. The motivating bug: a downstream consumer (Parties) currently resolves a graph start node by running a free-text/URN search and taking the top hit; when the canonical unit falls outside the top results, graph mode silently degrades to local mode. A keyed lookup removes the guesswork. **Do not implement this as a search query** — read the dedup record by exact key (AC1, AC2). Keep the change additive and surgical; this is an operational-readiness story, not a feature expansion.

### The authoritative index — reuse the permanent dedup record (AC2)

The ingestion pipeline already writes the mapping this endpoint needs. There is **no separate store to build**:

- **Key builder (reuse, do not duplicate the hash):** `DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri)` → `dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}`, where `ComputeHash` = `Convert.ToHexString(SHA256.HashData(UTF8(input))).ToLowerInvariant()` (lowercase hex). [Source: src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs:15-16,36-37]
- **The value IS the canonical `MemoryUnitId`** — a plain Redis string, written permanently with `expiry: null, When.Always` after a unit indexes successfully. This is the exact record to read back. [Source: src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs:32-37]
- **Read pattern to mirror:** `CheckIdempotencyActivity` already reads this key from the keyed `redis` `IConnectionMultiplexer` and excludes the transient marker. Copy its read shape exactly. [Source: src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:34,50-66]
- **Connection:** inject `[FromKeyedServices("redis")] IConnectionMultiplexer` and call `GetDatabase().StringGetAsync(key)` — the same DI seam `SaveDedupKeyActivity` / `CheckIdempotencyActivity` use. [Source: src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs:17]

`DedupKeyBuilder` is `internal static` in `Hexalith.Memories.Server`, and `PreflightDedupReservation` lives in the referenced `Hexalith.Memories.EventStore` project — both are reachable from the new Server-side seam. Lookup logic therefore belongs in `Hexalith.Memories.Server` (not the client or contracts assembly).

### Exclude the transient reservation marker (AC3)

The permanent `dedup:` key can **transiently hold the marker** `PreflightDedupReservation.ReservedValue` (`"reserved"`) while an event-driven ingest is in flight, before the permanent `MemoryUnitId` overwrites it. The mechanism is the **EventStore pub/sub preflight**: `RedisPreflightDedupStore.TryReserveAsync` writes `"reserved"` with `When.NotExists` to the dedup key, where the key comes from `EventStoreDedupKey.Build` — which produces the **identical** `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` namespace that `DedupKeyBuilder.BuildKey` produces and this lookup reads. [Source: src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs:45] [Source: src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs:16-17]

> Do **not** confuse this with Story 18.4's REST-ingress reservation (`IngestDedupReservation`), which writes the winner's workflow `instanceId` to a **distinct `ingest-reserve:` namespace** — that key is never read by this lookup and never holds `"reserved"`. Only the EventStore preflight touches the permanent `dedup:` key.

The lookup must treat the marker as **not-found** — exactly as `CheckIdempotencyActivity` does on the same key:

```csharp
if (PreflightDedupReservation.IsTransientReservation(existing.ToString()))
{
    return null; // in-flight reservation, not a committed MemoryUnitId
}
```

[Source: src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:58-61] `IsTransientReservation` / `ReservedValue` are defined on `PreflightDedupReservation` in `src/Hexalith.Memories.EventStore/PreflightDedupReservation.cs`. Returning the marker as an id would hand the consumer a graph start node id of `"reserved"` — a silent data-corruption bug.

### Endpoint design (AC1, AC5)

- **Route (recommended):** `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri?sourceUri=<urlencoded>` — RESTfully nested under the case, so tenant+case scope is in the path. The neighboring read endpoint to mirror is the GetMemoryUnit GET handler. [Source: src/Hexalith.Memories.Server/Program.cs:1690]
- **Routing precedence note:** `by-source-uri` is a literal sibling of the existing `/memory-units/{memoryUnitId}` template segment (`Program.cs:1690`). ASP.NET Core gives literal segments higher precedence than route parameters, so the literal route wins — no `AmbiguousMatchException`. Add an endpoint test asserting `GET …/memory-units/by-source-uri` hits the lookup, not the get-by-id handler. (If you prefer to sidestep the sibling entirely, `…/cases/{caseId}/source-uri-lookup?sourceUri=` is an acceptable alternative; keep it case-scoped either way.)
- **GET, not POST:** this is a side-effect-free read; `sourceUri` as a urlencoded query value keeps it cacheable and consistent with `search inspect`. Server-side the value is SHA-256'd into the key, so length is not a backend concern; only the URL length limit applies (acceptable for source URIs).
- **Not-found is structured (AC1):** `Results.NotFound(new ErrorResponse("MEMORY_UNIT_NOT_FOUND", …))`, matching the GetMemoryUnit precedent — never a `200` with an empty/best-effort body.
- **Tenant isolation (AC5):** the dedup key embeds `tenantId` and `caseId`, so isolation is **structural** — a different tenant/case produces a different Redis key that simply misses. Validate the tenant format up front with `TenantIdGuard.Validate` (→ `400 INVALID_TENANT_ID`). A defense-in-depth re-fetch of the unit via `CaseService.GetMemoryUnitAsync` (which logs `Critical` + bumps `TenantMismatchMonitor` on a tenant mismatch) is **optional** and not required for correctness; if you add it, do not turn it into a second store lookup that violates AC2.

### Backend-unavailability behavior (AC6)

The ingest path fails **open** on Redis unavailability (schedule a fresh id; ADR 9.1-B) because dropping an ingest is worse than a rare duplicate. The lookup's calculus is the opposite: returning `404` on a transient Redis outage would tell the consumer "no unit exists," and a consumer acting on that may **re-ingest and create a duplicate**. So the lookup must **not** fail-open to not-found — let the `RedisException` propagate and map it to a structured backend error (e.g. `503 LOOKUP_BACKEND_UNAVAILABLE`). The seam returns `null` only for a genuine miss or transient marker, never for an I/O error.

### Client method (AC4) — Architecture Decision D9

Add the method to the concrete `MemoriesClient` as `public virtual`; **do not** introduce `IMemoriesClient`. D9 keeps the client concrete ("avoid abstraction tax; extract when a second implementation arrives"); the supported mock seam is the `HttpClient` / `IHttpClientFactory` boundary (Story 18.7), exercised via a `TestDelegatingHandler`. Mirror `GetMemoryUnitAsync` exactly: `ArgumentException.ThrowIfNullOrWhiteSpace` guards, `Uri.EscapeDataString`, `ReadFromJsonAsync<MemoryUnitIdLookupResponse>(MemoriesJsonContext.Options, ct)`, `404 → null`, other errors → `MemoriesRemoteException`, `ConfigureAwait(false)`. [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:215-261] [Source: _bmad-output/planning-artifacts/architecture.md#Complete-Decision-Registry (D9)]

Returning `string?` (the id) keeps the client ergonomic for the Parties caller (`ResolveGraphStartNodeIdAsync`). The wire body is still the additive `MemoryUnitIdLookupResponse` record so the JSON contract is explicit and versioned.

### CLI exposure (AC4) — included; MCP — deliberately declined

- **CLI (in scope):** add `memories search lookup` next to `search inspect`, mirroring `SearchInspectCommand` (options `--tenant`, `--case`, `--source-uri`; `OutputFormatterRouter` for human/json; `CliExitCodes.NotFound` on miss). The CLI is the reference implementation and a natural operator/diagnostic home for "what unit owns this URI". [Source: src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs]
- **MCP (declined, documented):** per the architecture's interface philosophy, "a capability goes to MCP if an LLM agent needs it to complete a user-facing task (search, ingest, traverse, case info)"; lookup-by-source-uri is an **operational/diagnostic resolution**, and the actual consumer (Parties) calls the REST client, not MCP. Record the decision in the story (this section) so it reads as a deliberate scope call. If a future agent need appears, follow the `GetCaseInfoTool` pattern with `TenantClaimAuthorizationFilter`. [Source: _bmad-output/planning-artifacts/architecture.md#Interface-Philosophy]

### Coupling with Story 18.6 (dedup-record lifetime)

This endpoint's guarantee — "given `(tenantId, caseId, sourceUri)`, return the canonical `MemoryUnitId`" — holds **for as long as the dedup record persists**. Today that record is permanent/TTL-less (`SaveDedupKeyActivity` writes `expiry: null`). Story 18.6 documents the `MemoryUnitId` stability contract and the record's TTL/retention dependency; the Epic 18 sequencing note requires landing 18.5 **with or after** 18.6's contract so the lookup is never published against an unstated stability guarantee. The existing stable-ingest doc already cross-links 18.5/18.6 and states the committed `sourceUri`-keyed record must stay TTL-less — reference it rather than restating the guarantee. [Source: docs/dev/ingest-contract.md] [Source: _bmad-output/planning-artifacts/epics.md#Story-18.6]

> **Cross-story invariant (from 18.4):** the idempotency token *augments*, never replaces, the `sourceUri → MemoryUnitId` record. 18.5 keys on `sourceUri` (always-permanent), **not** the token (which is not guaranteed permanent in the sourceUri-fallback edge), so the lookup is on solid ground keying off `DedupKeyBuilder.BuildKey`.

### Route-surface drift guard (AC7) — mandatory or the build breaks

`RouteSurfaceContractTests` derives the `/api/*` route list from `Program.cs` and asserts (a) every route is documented in `docs/operations/route-surface.md` and (b) a **count tie** (documented `/api/` rows == mapped `/api/` literals). Adding a route without a doc row fails `DocumentedApiRowCount_EqualsMappedApiRouteCount`. So: add the new GET row to the route table (method, path, purpose, Dapr-op semantics) and bump the prose "45 → 46" mentions. [Source: tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs:69-81] [Source: docs/operations/route-surface.md:24,119]

### Deferred-work ledger (AC8)

Flip `MEM-5` `carried-forward → resolved` and add an `Evidence:` line following the MEM-4 worked example (schema: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, `Evidence`). Name the route, the `MemoriesClient` method, the `MemoryUnitIdLookupResponse` contract, the lookup seam, and the proving tests. The `CiTestInventoryTests` parser validates the schema — keep it well-formed. [Source: _bmad-output/implementation-artifacts/deferred-work.md:1432-1443]

### Running tests in this sandbox (mandatory workaround)

`dotnet test` fails here with `SocketException (13)` (VSTest TCP listener). Build the test project, then run the xUnit v3 assembly directly with the diff tool disabled:

```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
DiffEngine_Disabled=true dotnet exec \
  tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Ingestion.SourceUriMemoryUnitLookupTests
# `-list methods` prints the discovery count for the Change Log delta.
```

Repeat per affected test project (`…Server.Tests`, `…Contracts.Tests`, `…Cli.Tests`). xUnit v3 + Shouldly + NSubstitute; the global `using Xunit;` is provided by `tests/Directory.Build.props` — do not re-add it. (Concurrency proof at the unit level is a deterministic substitute-based test like `IngestDedupReservationTests`, not a real two-thread race; a real-Redis race belongs in the deferred Aspire/Testcontainers lane.)

### CRLF requirement (hard gate)

`.editorconfig` mandates `end_of_line = crlf` for `[*]` (there is no `.gitattributes` override). `Write`/`Edit` emit LF — 18.4's senior review flagged LF-only new files as a MEDIUM. After creating/editing any `.cs` or `.md` file, normalize:

```bash
sed -i 's/$/\r/' <path-to-new-file>
```

### Process guardrails

- Warnings are errors (`TreatWarningsAsErrors=true`): XML docs on all public members; preserve the ITANEO MIT copyright header on every new `.cs`; `sealed record`/`file-scoped namespace`; `_camelCase` fields; `ConfigureAwait(false)` in client/library code.
- Central package management — never add a `Version` to a `.csproj` `PackageReference`.
- Conventional Commits: this story is an additive **`feat`** (new public client capability + endpoint). Do not mislabel as `refactor`/`docs`. No `tools/release-packages.json` change required.
- Keep contracts camelCase and registered in `MemoriesJsonContext`; preserve story-id comments around the dedup code you read.

### Project Structure Notes

- New code lands in: `Hexalith.Memories.Server/Ingestion/` (lookup seam), `Hexalith.Memories.Server/Program.cs` (route + DI), `Hexalith.Memories.Contracts/V1/` (response record + JSON context), `Hexalith.Memories.Client.Rest/MemoriesClient.cs` (client method), `Hexalith.Memories.Cli/Commands/` (CLI command). All five are existing projects/folders — no new project, no `.slnx` edit, no AppHost change.
- This module is **not** a pure EventStore domain module — it is a Dapr-Workflow ingestion host; REST routes are **minimal-API `app.MapX` literals in `Program.cs`** (no controllers). The architecture doc's "`Controllers/*Controller.cs`" naming is stale; code is the source of truth.
- Doc drift: the architecture doc calls `MemoryUnitId` a "ULID" and tests "xUnit 2.9.3" — both are stale. In code, `MemoryUnitId` is the workflow `InstanceId` (a GUID) or a fresh GUID (`ResolveMemoryUnitId` in `IngestionWorkflow.cs`), and the repo uses **xUnit v3 (3.2.2)** per `Directory.Packages.props`/project-context. Treat the dedup record value as an **opaque id string** — do not assume ULID shape.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-18.5] — story statement + ACs + Parties follow-up.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-18 (sequencing + release-timing notes)] — 18.5/18.6 coupling; additive-endpoint, non-breaking.
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs:15-16,36-37] — key format + hash.
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs:32-37] — permanent (`expiry: null`) `MemoryUnitId` write — the record this endpoint reads.
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:34,50-66] — keyed-redis read + transient-marker exclusion to mirror.
- [Source: src/Hexalith.Memories.EventStore/PreflightDedupReservation.cs] — `ReservedValue` / `IsTransientReservation`.
- [Source: src/Hexalith.Memories.Server/Program.cs:1690] — GetMemoryUnit GET handler (endpoint pattern, tenant validation, structured not-found).
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:215-261] — `GetMemoryUnitAsync` client pattern (D9, escaping, error mapping).
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs] — source-gen JSON context to extend.
- [Source: src/Hexalith.Memories.Cli/Commands/SearchInspectCommand.cs] — CLI query-command pattern.
- [Source: docs/operations/route-surface.md:24,119] + [Source: tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs:69-81] — route surface + drift guard.
- [Source: docs/dev/ingest-contract.md] — dedup-record permanence; 18.5/18.6 cross-links.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md:1432-1443] — MEM-4 (schema example) + MEM-5 entry to resolve.
- [Source: _bmad-output/implementation-artifacts/18-4-stable-ingest-contract-with-explicit-idempotency-token-and-atomic-dedup.md] — predecessor: atomic reservation, invariants, sandbox/CRLF gotchas.
- [Source: _bmad-output/project-context.md] — project rules (tenant isolation, idempotency, camelCase contracts, CRLF, central package management).

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) via bmad-dev-story.

### Debug Log References

- Sandbox test runner: `dotnet test` fails (`SocketException 13`), so per Dev Notes each affected test project was built and the xUnit v3 assembly run directly with `DiffEngine_Disabled=true dotnet exec …`.
- Drift guard caught a real gap on first full CLI run: `ErrorCatalogDriftTests.Catalog_CoversEveryCurrentServerErrorCode` flagged the two new server codes (`INVALID_SOURCE_URI`, `LOOKUP_BACKEND_UNAVAILABLE`) as unmapped; both were added to `ErrorMessageCatalog` (exit 1 / exit 2 respectively) and the suite went green.
- Endpoint handler extracted to `MemoryUnitLookupEndpoint.HandleAsync` (mirroring the `TenantEndpointHandlers` precedent) so branch behaviour is unit-testable without a host; the literal-route-precedence case (AC1) runs through the real router via the existing keyed-redis-faking `WebApplicationFactory`.

### Completion Notes List

- **AC1** Exact keyed lookup, not a search: new `GET …/memory-units/by-source-uri` reads the dedup record by exact key and returns a structured `404 MEMORY_UNIT_NOT_FOUND`; never delegates to the search engine. Literal-route precedence over the `{memoryUnitId}` sibling asserted through the real router.
- **AC2** Reuses the permanent dedup record via `DedupKeyBuilder.BuildKey` (no parallel store); the lookup seam reads the keyed `redis` `IConnectionMultiplexer` exactly like `CheckIdempotencyActivity`.
- **AC3** Transient `PreflightDedupReservation.ReservedValue` marker is treated as not-found (seam + endpoint tests).
- **AC4** Surfaced via additive `Contracts.V1.MemoryUnitIdLookupResponse` (camelCase, source-gen registered), public `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` (`string?`, 404→null, D9 concrete/virtual), and the CLI `memories search lookup`. MCP deliberately declined (operational/diagnostic resolution; documented in Dev Notes).
- **AC5** Tenant format validated via `TenantIdGuard.Validate` (`400 INVALID_TENANT_ID`); tenant/case isolation is structural (key embeds both) — cross-tenant and different-case tests resolve to not-found.
- **AC6** Redis read failures propagate from the seam and map to a structured `503 LOOKUP_BACKEND_UNAVAILABLE` (never a false `404`); client surfaces non-404 errors as `MemoriesRemoteException`.
- **AC7** Route added to `docs/operations/route-surface.md` with the "45→46" prose counts bumped; `RouteSurfaceContractTests` green (forward tie + count tie).
- **AC8** `MEM-5` flipped `carried-forward → resolved` with an `Evidence:` line; `CiTestInventoryTests` parses the ledger. All new/changed code covered; full affected suites pass (Contracts 549, Server 1925/+1 pre-existing skip, CLI 397).
- Additive `feat` (new public client capability + endpoint + contract record); no `tools/release-packages.json` or `public-surface-stability.md` change required.

### File List

- `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` (new)
- `src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs` (new)
- `src/Hexalith.Memories.Server/Program.cs` (modified — DI registration + route mapping + using)
- `src/Hexalith.Memories.Contracts/V1/MemoryUnitIdLookupResponse.cs` (new)
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (modified — `[JsonSerializable]` registration)
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (modified — `LookupMemoryUnitIdBySourceUriAsync`)
- `src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs` (new)
- `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs` (modified — wired `search lookup`)
- `src/Hexalith.Memories.Cli/Execution/CliExitCodes.cs` (modified — added `NotFound`)
- `src/Hexalith.Memories.Cli/CliServices.cs` (modified — registered lookup formatters)
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitIdLookupHumanFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/MemoryUnitIdLookupJsonFormatter.cs` (new)
- `src/Hexalith.Memories.Cli/Output/Formatters/CommandPayloadRegistry.cs` (modified — `search lookup` error-envelope shape)
- `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs` (modified — envelope source-gen registration)
- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` (modified — `INVALID_SOURCE_URI`, `LOOKUP_BACKEND_UNAVAILABLE`)
- `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs` (new)
- `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` (new)
- `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs` (new)
- `docs/operations/route-surface.md` (modified — new route row + 45→46 counts)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified — MEM-5 resolved + Evidence)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 18-5 → review)

## Change Log

| Date | Version | Description | Author |
| :--- | :------ | :---------- | :----- |
| 2026-06-25 | 0.1 | Story drafted via create-story (ultimate context engine analysis). Status → ready-for-dev. | Bob (SM) |
| 2026-06-25 | 1.0 | Implemented source-URI-keyed lookup seam, endpoint, contract, client method, CLI command, route-surface doc, and MEM-5 ledger resolution. All 8 tasks complete; affected suites green. Status → review. | Amelia (Dev) |
| 2026-06-25 | 1.1 | Senior Developer Review (AI): auto-fixed 1 MEDIUM (audit event-id collision — payload `EventId` 7505/7515 reused the Delete-access pair; corrected to the canonical case-access pair 7504/7514). Aligned a stale "45→46" prose count in route-surface.md. Rebuilt clean; new + drift suites green (Server 65, Contracts 3, CLI 16, route-surface 10, error-catalog 2, CI-inventory 48). Status → done. | Jérôme Piquot (Review) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-25 · **Outcome:** Approved (1 MEDIUM auto-fixed)

Adversarial review of the full File List against the eight ACs, with the affected test projects built and the
relevant xUnit v3 assemblies executed directly (sandbox `dotnet exec` + `DiffEngine_Disabled=true`). All three
projects build with **0 warnings** (warnings-as-errors). New-test totals: **Server 25, Contracts 3, CLI 16** —
all green; drift guards **RouteSurfaceContractTests 10**, **ErrorCatalogDriftTests 2**, **CiTestInventoryTests
48**, **EndpointTelemetryScopeTests** all green.

### AC verdicts
- **AC1–AC3, AC5, AC6** — Implemented and proven. Exact-key lookup (no search delegation), permanent dedup
  record reuse via `DedupKeyBuilder.BuildKey`, transient-`reserved` exclusion, structural tenant/case isolation,
  and Redis-failure → `503 LOOKUP_BACKEND_UNAVAILABLE` (never a false `404`) are each covered by seam +
  endpoint tests, including literal-route precedence through the real router.
- **AC4** — Additive `Contracts.V1` record (camelCase, source-gen registered), `public virtual` `MemoriesClient`
  method (D9, `404→null`, `INVALID_RESPONSE` on empty/garbled 2xx), and `memories search lookup` CLI; MCP
  deliberately declined and documented.
- **AC7** — New route row added; the four `45→46` count mentions bumped and the count-tie/forward-tie guards
  stay green.
- **AC8** — `MEM-5` flipped to `resolved` with a complete `Evidence:` line; `CiTestInventoryTests` parses it.

### Findings
1. **[MEDIUM · FIXED] Audit event-id collision (telemetry integrity).** The endpoint passed `successEventId`/
   `errorEventId` = `7505`/`7515` to `EndpointTelemetryScope`, populating the `AccessTelemetryEvent.EventId`
   payload field. That field is **operation-keyed** in `AccessTelemetryLog` (search 7501/7511 … case-access
   7504/7514 … **delete 7505/7515**) and the scope emits this case-scoped read through the case-access channel
   (`OperationCaseAccess` → outer ILogger id 7504/7514). So every lookup audit event self-reported `EventId=7505`
   — the **Delete-access** id pinned by `EndpointTelemetryScopeTests` — mislabeling reads as deletes in the FR67
   audit trail. The story's "distinct from 7504/7514" note misread the design (the pair is per-operation, not
   per-endpoint). **Fix:** use the canonical case-access pair `7504/7514` (matching `GetMemoryUnit`).
   `src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`. Verified: 65/65 server tests green
   (including the Delete-pair scope tests).
2. **[LOW · ACCEPTED] Line-ending normalization churn.** `Program.cs` (+122), `MemoriesClient.cs` (+142),
   `CommandPayloadRegistry.cs`, and `route-surface.md` show LF→CRLF normalization of pre-existing lines unrelated
   to this story (the committed baselines were mixed), inflating the diff and `git blame`. Left as-is: CRLF is
   what `.editorconfig` mandates, so the change moves toward the standard; reverting would re-introduce
   violations. Noted so the diff size is not mistaken for scope.
3. **[LOW · FIXED] Stale prose count.** `route-surface.md` still read "45 minimal-API endpoints" in the
   OpenAPI-deferral paragraph (not a drift-guarded table row, so build-neutral); aligned to 46 for consistency.

### Notes
- The gRPC "Connection refused" lines during the `WebApplicationFactory` route-precedence test are background
  hosted services reaching for an absent Dapr sidecar — unrelated to the assertion, which passes.
