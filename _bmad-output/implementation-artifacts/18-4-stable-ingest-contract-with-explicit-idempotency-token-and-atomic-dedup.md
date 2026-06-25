---
baseline_commit: d1b2cb6
---
# Story 18.4: Stable Ingest Contract with Explicit Idempotency Token and Atomic Dedup

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 — Downstream Consumer Integration Contract Hardening |
| Story key | `18-4-stable-ingest-contract-with-explicit-idempotency-token-and-atomic-dedup` |
| Origin | MEM-4 (Parties consumer integration intake, Sprint Change Proposal 2026-05-27, pass 9-6 chunk A / 3rd pass) |
| Lifecycle track | Engineering / Operational Readiness — Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **`feat:` — MINOR release. The ONLY release-sensitive story in Epic 18.** Changes two packable public contracts — `Hexalith.Memories.Contracts` (`IngestionInput` gains an optional property) and `Hexalith.Memories.Client.Rest` (`IngestAsync` graduates out of `[Experimental("HXL001")]`). **Strictly additive — NO breaking signature change, NO `BREAKING CHANGE:` footer.** Removing an experimental marker and adding an optional property/parameter are both non-breaking. Must be cut **before** the Parties project pins the stabilised SDK. |
| Deliverable | A non-experimental `MemoriesClient.IngestAsync` path that accepts an optional explicit idempotency token, plus **race-safe atomic dedup** on the client/REST ingest path (reusing the proven `IPreflightDedupStore` `SET … NX` reservation primitive) so two near-simultaneous same-source ingests resolve to one memory unit and the loser observes the winner's `MemoryUnitId`. Consumers can drop `#pragma warning disable HXL001`. |
| Parties-side follow-up | Parties drops the `HXL001` suppression in `PartyMemoryIndexingService` and passes the idempotency token. |

## Story

As a downstream service indexing memories from near-simultaneous projection events,
I want a non-experimental ingest path that accepts an explicit idempotency token and resolves concurrent same-source ingests atomically,
so that two near-simultaneous ingests of the same party/source cannot race into duplicate or partially-written memory units, and consumers can drop the `HXL001` suppression.

## Acceptance Criteria

**AC1 — Stable (non-experimental), additive ingest entry point**
**Given** `MemoriesClient.IngestAsync` is currently `[Experimental("HXL001")]` (Story 7.4),
**When** this story stabilises the ingest path,
**Then** a non-experimental ingest entry point exists, the change is additive (experimental-marker removal OR a new overload — **no breaking signature change**), serialized through the existing `MemoriesJsonContext` JSON context, and covered by contract/client tests, so consumers can ingest **without** `#pragma warning disable HXL001`.

**AC2 — Explicit idempotency token participates in dedup with documented precedence/fallback**
**Given** the only dedup key today is `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}` derived server-side,
**When** the ingest contract is extended,
**Then** the request carries an **optional** explicit idempotency token that, when supplied, participates in dedup alongside `sourceUri`, and the contract documents **token precedence** and the **natural-key (`sourceUri`) fallback** when the token is absent.

**AC3 — Atomic dedup resolution (no check-then-act race)**
**Given** the current idempotency check in `CheckIdempotencyActivity` is check-then-act and can race under concurrency,
**When** two ingests with the same dedup key arrive near-simultaneously,
**Then** dedup resolution is **atomic** (e.g. a Redis `SET … NX` reservation) so **exactly one ingest wins** and the other **observes the existing `MemoryUnitId`**, proven by a **concurrent-ingest test**.

**AC4 — Idempotent under at-least-once, unordered delivery**
**Given** ingestion runs on at-least-once, unordered Dapr pub/sub,
**When** a duplicate or out-of-order ingest is received,
**Then** behavior remains idempotent and returns the **same `MemoryUnitId`** without creating a second unit, consistent with the project idempotency rules.

## Tasks / Subtasks

- [x] **Task 0 — Preflight: re-verify every cited anchor against live source (Epic 18 mandate).** (AC: 1,2,3,4)
  - [x] Re-confirm `MemoriesClient.IngestAsync` is `[System.Diagnostics.CodeAnalysis.Experimental("HXL001")]` at `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:414`, `public virtual async Task<string>`, params `(tenantId, caseId, sourceUri, byte[] content, contentType, ingestedBy, IReadOnlyDictionary<string,MetadataField>? metadata, ct)`, POSTs `"api/ingest"` via `PostAsJsonAsync(..., MemoriesJsonContext.Options, ct)` (`:453-454`), returns `instanceId`. Confirm the four `[Experimental("HXL001")]` attributes sit at `:279` (`CreateTenantAsync`), `:353` (`CreateCaseAsync`), `:414` (`IngestAsync`), `:646` (`GetTelemetrySummaryAsync`) — **only `IngestAsync` (`:414`) graduates; the other three stay HXL001.** Confirm there is **no** central `HXL001` constant (string literal in each attribute). → **Confirmed (see Debug Log).**
  - [x] Re-confirm `IngestionInput` is `sealed record` at `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:9-48` with required `TenantId/CaseId/SourceUri/ContentType/SourceType/IngestedBy`, optional `ContentBytes/Metadata(Ordinal-pinned, D6)/CausationId/CorrelationId`; registered in `MemoriesJsonContext` via `[JsonSerializable(typeof(IngestionInput))]` at `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs:69` (camelCase, source-gen + reflection fallback). → **Confirmed.**
  - [x] Re-confirm the **race**: `CheckIdempotencyActivity` (`src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:15-47`) does a GET-only `StringGetAsync(dedupKey)`, treats `PreflightDedupReservation.IsTransientReservation("reserved")` as **not** a duplicate, else `HasValue → duplicate`; `SaveDedupKeyActivity` (`.../SaveDedupKeyActivity.cs:13-40`) writes `StringSetAsync(dedupKey, memoryUnitId, expiry: null, when: When.Always)` — **permanent, unconditional**. The window is between the GET (check) and the `When.Always` SET (act). → **Confirmed.**
  - [x] Re-confirm the **existing atomic primitive to REUSE**: `IPreflightDedupStore.TryReserveAsync(dedupKey, ttl, ct) → Reserved | Duplicate | FailOpen` (`src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs`), impl `RedisPreflightDedupStore` uses `StringSetAsync(key, PreflightDedupReservation.ReservedValue, ttl, When.NotExists)` and **fails OPEN on `RedisException`/`TimeoutException` per ADR 9.1-B** (`src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs:34-71`), with `ReleaseAsync` deleting the key (cited as "AC #9" cleanup). `PreflightDedupReservation.ReservedValue == "reserved"`. Default `PreflightDedupTtl = 24h`, `PreflightDedupEnabled = true` (`TenantEventRoutingOptions.cs:46,50`). → **Confirmed; live `TryReserveAsync` returns `FailOpen` (not `Duplicate`) on an in-flight `"reserved"` marker — see Debug Log finding.**
  - [x] **Critical hazard to verify (prevents a regression):** the **EventStore pub/sub path already reserves atomically at ingress** — `EventIngestionService.ProcessAsync` (`src/Hexalith.Memories.EventStore/EventIngestionService.cs:147-174`) calls `TryReserveAsync` BEFORE scheduling the workflow, which is **why** `CheckIdempotencyActivity` special-cases the `"reserved"` marker as "proceed" (it is the workflow's OWN preflight reservation). The **REST `/api/ingest` ingress has NO preflight** (`src/Hexalith.Memories.Server/Program.cs:372-431` → straight to `ScheduleNewWorkflowAsync`). Confirm both, and confirm whether the EventStore preflight `dedupKey` is `sourceUri`-derived (matching the workflow `DedupKeyBuilder`) or `cloudEventId`-derived — the interface XML doc says `sha256(cloudEventId)` but `EventStoreDedupKey`/`DedupKeyBuilder` hash `sourceUri`; resolve this before choosing where to place the new reservation. → **Resolved: EventStore key is `cloudEventId`-derived (`EventStoreDedupKey.Build(..., envelope.Id)` despite the misleading `sourceUri` param name); distinct from the workflow's `sourceUri` key. REST ingress confirmed preflight-less. See Debug Log.**
  - [x] Re-confirm `ResolveMemoryUnitId` (`src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:521-534`): uses `context.InstanceId` unless the instance id is `"dedup:"`-prefixed AND `SourceType.Event` → `context.NewGuid()`. The duplicate short-circuit returning the existing id is `IngestionWorkflow.cs:79-103`. → **Confirmed.**
  - [x] Re-confirm `IdempotencyInput(string SourceUri, string TenantId, string CaseId)` (`.../IdempotencyInput.cs:12`) and `IdempotencyResult(bool IsDuplicate, string? ExistingMemoryUnitId)`; the workflow builds `dedupKey` at `IngestionWorkflow.cs:73` and calls `CheckIdempotencyActivity` at `:74-77`. → **Confirmed.**
  - [x] If any anchor moved, update this story's anchors before authoring code. → **No anchor moved; all anchors valid at the working tree.**

- [x] **Task 1 — Decide and record the atomic-dedup placement (the central design decision).** (AC: 3,4)
  - [x] Choose between the two legitimate placements and record the decision + rationale in the Dev Agent Record: → **Chose refined Option A (ingress reservation, distinct key carrying the winner's instance id). See Dev Agent Record → Design Decision (Task 1).**
    - **Option A (recommended) — extend the proven preflight reservation to the REST `/api/ingest` ingress.** Inject `IPreflightDedupStore` into the `/api/ingest` handler and `TryReserveAsync` (sourceUri/token `dedupKey`, reservation TTL) **before** `ScheduleNewWorkflowAsync`; on `Reserved` schedule as today, on `Duplicate` do **not** schedule a second workflow and return the existing result, on `FailOpen` proceed (ADR 9.1-B). This makes **both** ingest entry points race-safe with the **same** primitive, leaves `CheckIdempotencyActivity`/the EventStore path **unchanged**, and is the minimal-surface, regression-safe choice.
    - **Option B — make `CheckIdempotencyActivity` itself reserve atomically.** Higher risk: the shared `"reserved"` marker is ambiguous — in the EventStore path it means "my own preflight reservation, proceed", but for a second concurrent REST workflow it must mean "someone else is in-flight, do NOT proceed". Distinguishing them requires either an ownership-encoded marker or a replay-safe in-flight wait (`context.CreateTimer` poll until the reservation resolves to a permanent id) — and must NOT break the EventStore special-case. Only pick this if Task 0 shows Option A cannot cover the client path cleanly.
  - [x] Whichever is chosen, the implementation MUST satisfy every invariant in Dev Notes → "Invariants that MUST hold (disaster prevention)". → **Refined Option A satisfies all 8 invariants (mapping recorded in Dev Agent Record → Design Decision).**

- [x] **Task 2 — Add the optional idempotency token to the contract.** (AC: 1,2)
  - [x] Add an **optional** `string? IdempotencyToken { get; init; }` to `IngestionInput` (`Contracts/V1/IngestionInput.cs`) — additive, init-only, keep the ITANEO header + XML doc + `sealed record` shape. Source-gen registration already covers it via the existing `[JsonSerializable(typeof(IngestionInput))]`; verify camelCase round-trip (`idempotencyToken`). Do **not** make it `required`. → **Done; camelCase round-trip + back-compat covered by tests.**
  - [x] Extend the dedup-key derivation so a supplied token participates: add a `DedupKeyBuilder` overload (or parameter) that keys on the token when present, falling back to `sourceUri` when absent, per the documented precedence (Dev Notes → "Idempotency-token semantics"). **Preserve the existing `sourceUri`-keyed dedup record** so Stories 18.5 (source-URI lookup) and 18.6 (`MemoryUnitId` stability) keep working — see the cross-story constraint in Dev Notes. → **Added `DedupKeyBuilder.BuildTokenKey` (`:tok:` namespace) + `BuildIdentityKey` (token precedence/sourceUri fallback). `SaveDedupKeyActivity` still writes the sourceUri record; the token record is written additionally.**
  - [x] Thread the token through `IdempotencyInput` (extend the record additively) and the workflow's `dedupKey` build at `IngestionWorkflow.cs:73-77`. → **`IdempotencyInput` gained optional `IdempotencyToken`; `CheckIdempotencyActivity` checks token key first then sourceUri; workflow passes the token and writes the token-keyed permanent record after the sourceUri one.**

- [x] **Task 3 — Stabilise `MemoriesClient.IngestAsync` (drop HXL001, pass the token).** (AC: 1,2)
  - [x] Remove the `[Experimental("HXL001")]` attribute from `IngestAsync` **only** (keep it on `CreateTenantAsync`/`CreateCaseAsync`/`GetTelemetrySummaryAsync`). Prefer **marker removal on the existing method + an additive optional `idempotencyToken` parameter** (e.g. a trailing `string? idempotencyToken = null` before `ct`, or a new overload if a default-parameter change would be source/binary-incompatible for the `virtual` member — verify). Update the method's XML `<remarks>` to drop the "EXPERIMENTAL (HXL001)" note and document the token. Keep `public virtual` and the concrete `MemoriesClient` (Architecture D9 — do **not** add `IMemoriesClient`). → **Verified: adding an optional param to the existing `virtual` method is binary-incompatible, so kept the 8-param signature intact (marker removed, `<remarks>` updated) and added a NEW `virtual` overload with `string? idempotencyToken` before `ct`; both delegate to a private `IngestCoreAsync`. Other three HXL001 methods untouched. No `IMemoriesClient`.**
  - [x] Map the token into the `IngestionInput` it builds (`MemoriesClient.cs:441-451`); keep serialization via `MemoriesJsonContext.Options`. → **`IngestCoreAsync` maps the token (blank→null) into `IngestionInput.IdempotencyToken`; serialization unchanged via `MemoriesJsonContext.Options`.**

- [x] **Task 4 — Tests: contract round-trip, stable client, atomic concurrency, idempotency.** (AC: 1,2,3,4)
  - [x] **Contract (AC1/AC2):** extend `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` — assert `IdempotencyToken` round-trips as camelCase `idempotencyToken`, is omitted/null-tolerant when absent, and that existing payloads **without** the field still deserialize (back-compat). → **3 tests added (camelCase round-trip, null round-trip, legacy-payload back-compat).**
  - [x] **Stable client (AC1):** extend `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` — a test that calls `IngestAsync` **without** any `#pragma warning disable HXL001` (its mere compilation proves the marker is gone) and asserts the token is sent in the request body (capture via the existing `HttpClient`/handler test seam — the D9 mock boundary). → **2 tests added: stable overload (no pragma, `idempotencyToken:null` on wire) + token overload (token on wire), captured via `TestDelegatingHandler`.**
  - [x] **Atomic dedup — deterministic unit (AC3):** extend `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` (and/or the ingress per Task 1) — substitute `IConnectionMultiplexer`/`IPreflightDedupStore` so the `When.NotExists` reservation returns `true` (winner → proceeds) then `false` with an existing id (loser → observes the existing `MemoryUnitId`); cover the `"reserved"` transient-marker branch and the Redis-down **fail-open** branch (ADR 9.1-B). Use NSubstitute + Shouldly. → **New `IngestDedupReservationTests.cs` (8 tests) is the authoritative ingress-level proof: `SET NX` winner→Reserved, loser→DuplicateInFlight+winner id, fail-open on RedisConnection/Timeout, token-vs-sourceUri key selection, release. The `"reserved"` transient branch remains covered by existing `CheckIdempotencyActivityTests`. Added 4 token-precedence/fallback tests to `CheckIdempotencyActivityTests.cs`.**
  - [x] **Atomic dedup — concurrent proof (AC3, the AC-mandated "concurrent-ingest test"):** add a test that drives two near-simultaneous ingests on the same dedup key and asserts **exactly one** memory unit and the loser returns the **same** `MemoryUnitId`. A deterministic substitute-based race (winner/loser sequencing) satisfies the AC at unit level; if a true two-thread Redis race is warranted, add it as an integration test under `tests/Hexalith.Memories.IntegrationTests/` (Aspire/Testcontainers fixture) and keep it isolated from pure unit tests. State in the test which level provides the authoritative proof. → **`TryReserveAsync_TwoNearSimultaneousIngests_ExactlyOneWins_LoserGetsWinnerId` (deterministic `SET NX` true-then-false sequencing) is the authoritative unit-level proof — stated in the test-class doc-comment. A real-Redis two-thread race is noted as a future Aspire/Testcontainers integration test.**
  - [x] **Idempotency under redelivery (AC4):** extend `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — ingest the same source twice (duplicate path at `IngestionWorkflow.cs:79-103`) returns the same `MemoryUnitId`, `WasDuplicate: true`, and creates no second unit; confirm the token-keyed duplicate also short-circuits. → **`RunAsync_TokenKeyedDuplicate_ShouldShortCircuitWithSameId` + existing `RunAsync_DuplicateSource_ShouldReturnEarlyWithExistingId`; plus token threading + dual permanent-record tests.**
  - [x] **Compensation/cleanup (if a reservation is held):** assert a failed ingest after a successful reservation calls `ReleaseAsync` (or relies on the reservation TTL) so a retry is not permanently blocked — mirror `EventIngestionService` compensation. → **`ReleaseAsync_DeletesTheReservationKey` verifies the release primitive; the `/api/ingest` handler calls `ReleaseAsync` on scheduling failure after a held reservation (TTL is the backstop).**

- [x] **Task 5 — Docs + deferred-work + experimental-API ledger.** (AC: 1,2)
  - [x] Update `docs/dev/experimental-apis.md` — remove `MemoriesClient.IngestAsync` from the `HXL001` scope row (it has graduated to stable); keep `CreateTenantAsync`, `CreateCaseAsync`, `GetTelemetrySummaryAsync`. Note the graduation and the new idempotency token where the doc has an audience for it. → **Done; row now lists only the three still-experimental methods and cross-links the new ingest-contract doc.**
  - [x] Document the **stable ingest contract** (token precedence + `sourceUri` natural-key fallback, the atomic-dedup guarantee, the at-least-once idempotency guarantee, and the `MemoryUnitId`-on-duplicate behavior) in a `docs/dev` contract doc (extend the eventstore/client contract docs or add an ingest-contract section — mirror the house style of `docs/operations/route-surface.md` / `docs/dev/eventstore-integration.md`). Cross-link Stories 18.5/18.6 for the `sourceUri → MemoryUnitId` lifetime dependency. → **Added `docs/dev/ingest-contract.md` (AC1-AC4 + cross-story dependency).**
  - [x] In `_bmad-output/implementation-artifacts/deferred-work.md`: flip **MEM-4** (`carried-forward`, ~lines 1432-1437) to `resolved` with an `Evidence:` line pointing at the stabilised method + token + atomic-dedup tests (honor the Story 14.5 schema — `ID`, `Status ∈ {open|resolved|accepted|carried-forward}`, `Source story`, `Target artifact`, `Re-open trigger`, `Evidence:`). The `CiTestInventoryTests` parser validates these entries — keep them well-formed. → **MEM-4 → `resolved` + `Evidence:`; `CiTestInventoryTests` (48 tests) passes.**
  - [x] Confirm **no** `tools/release-packages.json` change is needed (both `Hexalith.Memories.Contracts` and `Hexalith.Memories.Client.Rest` are already packable). Confirm **no** `.slnx`/`Directory.Packages.props` edit and **no** new package version. → **Confirmed: both already in `release-packages.json`; no `.slnx`/`Directory.Packages.props`/package-version change made.**

- [x] **Task 6 — Verify and finalize.** (AC: 1,2,3,4)
  - [x] Build + run the new/extended tests via the sandbox workaround (Dev Notes → "Running tests in this sandbox"); record discovery counts. → **Done (see Change Log). New tests: +3 Contracts, +2 Cli, +16 Server (8 reservation + 4 idempotency-token + 4 workflow) = +21.**
  - [x] Run the full `Contracts.Tests`, `Cli.Tests` (incl. `CiTestInventoryTests`), and `Server.Tests` suites to confirm no regression; confirm `0 warnings` (warnings-as-errors). → **Contracts 545/0, Cli 384/0, Server 1887/0 (1 pre-existing skip), Mcp 83/0. Full `.slnx` build: 0 warnings, 0 errors.**
  - [x] **Release-type check:** the commit is `feat(story-18.4): …` (MINOR). Confirm there is **no** `BREAKING CHANGE:` footer and the public diff is additive only (new optional property + new optional param/overload + marker removal). Update File List, Completion Notes, and Change Log (with the test-count delta) before handoff. → **Additive-only confirmed: new optional `IngestionInput.IdempotencyToken`, new `IngestAsync` overload (8-param signature preserved), `HXL001` marker removed. No `BREAKING CHANGE:`.**

## Dev Notes

### Scope and intent (read first)
This is the **one code-bearing, release-sensitive story** in Epic 18 (18.1/18.2/18.3 were docs + drift-guards). It does three additive things: (1) **graduate `MemoriesClient.IngestAsync` out of `[Experimental("HXL001")]`**, (2) **add an optional idempotency token** to the ingest contract, (3) **make same-source dedup atomic** so concurrent ingests cannot create duplicate/partial units. The Parties consumer indexes from near-simultaneous projection events and must drop its `HXL001` suppression. **Do not** over-build: no `IMemoriesClient` (D9), no new persistence store, no breaking signature change, no aspirate/OpenAPI work.

### Release sensitivity (the highest-risk dimension)
- **Commit:** `feat(story-18.4): …` → triggers a **MINOR** release. This is the **only** Epic 18 story that may use `feat:` (Epic 18 release-timing note, `epics.md:3442`).
- **Two packable public contracts change** — `Hexalith.Memories.Contracts` (`IngestionInput` + optional property) and `Hexalith.Memories.Client.Rest` (`IngestAsync` marker/param). Both already in `tools/release-packages.json` → **no** package-map edit.
- **Strictly additive. NO `BREAKING CHANGE:`.** Removing an `[Experimental]` marker removes a compile-time diagnostic, not a signature; adding an **optional** property and an **optional** parameter (or a new overload) are additive. Renaming/removing any existing public member, or changing JSON shape semantics, would be breaking — avoid.
- **Cut before Parties pins the SDK.** Sequencing matters: Parties' follow-up depends on this minor being published.
- Adding a default-valued parameter to an existing **`virtual`** public method can be a source/binary-compat nuance — if in doubt, add a **new overload** rather than mutating the existing signature, and keep the old call shape working.

### Current code state — precise anchors (verified at baseline `d1b2cb6`)

| Anchor | Path | Current behavior |
| :--- | :--- | :--- |
| `MemoriesClient.IngestAsync` | `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:414-464` | `[Experimental("HXL001")]`, `public virtual async Task<string>`, POSTs `api/ingest` via `MemoriesJsonContext.Options`, returns `instanceId`. NOT sealed, `virtual` (D9). |
| HXL001 scope | same file `:279`(CreateTenant area), `:353`(CreateCase), `:646`(GetTelemetrySummary) | shared diagnostic id; **string literal, no constant**; only `IngestAsync` graduates here. |
| `IngestionInput` | `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:9-48` | `sealed record`; required `TenantId/CaseId/SourceUri/ContentType/SourceType/IngestedBy`; optional `ContentBytes/Metadata(Ordinal,D6)/CausationId/CorrelationId`. **Add `IdempotencyToken` here.** |
| JSON context | `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs:69,190-206` | source-gen `MemoriesJsonSourceGenerationContext`, camelCase web defaults + reflection fallback; `IngestionInput` already registered. |
| `DedupKeyBuilder` | `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs:12-20` | `BuildKey(tenantId,caseId,sourceUri) → dedup:{t}:{c}:{sha256_hex_lower(sourceUri)}`. Mirrored (isolated) as `EventStoreDedupKey` in the EventStore package. |
| `CheckIdempotencyActivity` | `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:15-47` | **check-then-act**: GET-only `StringGetAsync`; `"reserved"` marker → not duplicate; else `HasValue → duplicate`. Sealed `WorkflowActivity<IdempotencyInput,IdempotencyResult>`, `[FromKeyedServices("redis")] IConnectionMultiplexer`. |
| `SaveDedupKeyActivity` | `.../Activities/Ingestion/SaveDedupKeyActivity.cs:13-40` | `StringSetAsync(key, memoryUnitId, expiry: null, When.Always)` — **permanent, unconditional commit** (18.6 depends on this permanence). |
| `IngestionWorkflow` | `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:29-512` | builds `dedupKey` `:73`; `CheckIdempotencyActivity` `:74-77`; duplicate short-circuit returns existing id `:79-103`; `SaveDedupKeyActivity` `:424-428`. |
| `ResolveMemoryUnitId` | `IngestionWorkflow.cs:521-534` | `context.InstanceId` unless `"dedup:"`-prefixed + `SourceType.Event` → `context.NewGuid()`. |
| REST ingress | `src/Hexalith.Memories.Server/Program.cs:372-431` | `POST /api/ingest` binds `IngestionInput`, validates, tenant-guards, `ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input)`, returns `202 {instanceId}`, 2 MiB limit. **No preflight reservation today.** |
| Pub/sub ingress | `src/Hexalith.Memories.EventStore/EventIngestionController.cs` + `EventIngestionService.cs:147-174` | `POST /events/ingest` → **preflight `TryReserveAsync` BEFORE scheduling** (already race-safe). |
| **Atomic primitive to REUSE** | `IPreflightDedupStore` (`src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs`) + `RedisPreflightDedupStore` (`src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs:34-90`) | `TryReserveAsync(key,ttl,ct)` = `StringSetAsync(key, "reserved", ttl, When.NotExists)` → `Reserved|Duplicate|FailOpen`; **fails OPEN on Redis error (ADR 9.1-B)**; `ReleaseAsync` deletes the key. `PreflightDedupReservation.ReservedValue="reserved"`, `IsTransientReservation(...)`. Default TTL 24h (`TenantEventRoutingOptions.cs:50`). |

### The race, precisely (AC3)
Two ingests with the same `dedupKey`:
1. Workflow A `CheckIdempotencyActivity` GET → absent → proceeds.
2. Workflow B `CheckIdempotencyActivity` GET → still absent → proceeds.
3. Both index and both `SaveDedupKeyActivity` write their **distinct** `MemoryUnitId`s with `When.Always` → last-writer-wins; the earlier unit is orphaned.

The EventStore pub/sub path does **not** have this race because `EventIngestionService` already `TryReserveAsync`-es (NX) at the HTTP ingress, so only one delivery ever schedules a workflow. The **gap MEM-4 closes is the REST/client `IngestAsync → /api/ingest → workflow` path**, which has no preflight. **Reuse the existing NX primitive — do not invent a new one.**

> ⚠️ **Do NOT naively add `When.NotExists` inside `CheckIdempotencyActivity` without resolving the `"reserved"`-marker ambiguity.** In the EventStore path the key already holds `"reserved"` (the workflow's OWN preflight reservation), which the activity currently treats as "proceed". A second concurrent REST workflow that reads `"reserved"` must instead treat it as "someone else is in-flight". Same marker, opposite meaning — this is the trap. Option A (reserve at the REST ingress, leave the activity/EventStore path untouched) sidesteps it entirely.

### Idempotency-token semantics (AC2) — and the 18.5/18.6 cross-story constraint
- `IngestionInput.IdempotencyToken` is **optional**. **Precedence:** when present, the token is the dedup identity (token-keyed reservation/record takes precedence). **Fallback:** when absent, dedup falls back to the `sourceUri` natural key exactly as today.
- **Cross-story constraint (do not break 18.5/18.6):** Stories 18.5 (source-URI-keyed lookup) and 18.6 (`MemoryUnitId` stability) both rely on the **permanent `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}` record mapping `sourceUri → MemoryUnitId`**. If a token-supplied ingest only writes a *token-keyed* record, the `sourceUri → MemoryUnitId` mapping would be missing and 18.5's lookup / 18.6's stability guarantee would silently regress. **Therefore: a token must AUGMENT, not REPLACE, the sourceUri mapping** — the token gives precedence for *duplicate detection*, but the implementation must still maintain the `sourceUri`-keyed permanent record pointing at the same `MemoryUnitId`. Document this explicitly. (When the token is absent, behavior is unchanged.)
- Keep the **committed** dedup record **permanent** (`expiry: null`) as `SaveDedupKeyActivity` does today — a reservation TTL is fine for the *transient* reservation, but the final commit must drop the TTL so 18.6's stability guarantee holds.

### Invariants that MUST hold (disaster prevention)
1. **Exactly one unit per dedup key** under concurrency; the loser returns the winner's `MemoryUnitId` (`WasDuplicate: true`), matching the existing short-circuit at `IngestionWorkflow.cs:79-103`.
2. **`sourceUri → MemoryUnitId` permanent mapping preserved** for 18.5/18.6 (see above). Committed value stays TTL-less.
3. **EventStore pub/sub path unchanged / still race-safe.** Do not double-reserve or deadlock against the existing preflight reservation; do not break the `"reserved"`-marker special-case.
4. **Fail OPEN on Redis unavailability (ADR 9.1-B).** A Redis blip must not turn ingest into a hard failure — proceed and let the permanent dedup key be the authoritative safety net. Project rule: "never turn a degraded backend into total service failure unless required."
5. **Workflow determinism preserved.** Any in-workflow waiting/polling (Option B) must use replay-safe primitives (`context.CreateTimer`, `context.CurrentUtcDateTime`) — no wall-clock/`Task.Delay`/random in orchestration. Reservation Redis I/O belongs in an **activity**, never in orchestration code.
6. **Additive public surface only.** No breaking signature, no `BREAKING CHANGE:`; `MemoriesClient` stays concrete + non-sealed + `virtual` (D9).
7. **Tenant isolation intact.** Dedup keys remain tenant+case scoped; the token does not cross tenant/case boundaries.
8. **Reservation cleanup.** If a reservation is acquired but the ingest later fails, release it (`ReleaseAsync`) or rely on the reservation TTL so retries are not blocked.

### Architecture & decisions to honor
- **D9 (concrete client, no interface):** mock at the `HttpClient`/`IHttpClientFactory` boundary; keep `MemoriesClient` non-sealed with `virtual` members. Do **not** introduce `IMemoriesClient`. (`architecture.md` D9; this is also the subject of Story 18.7.)
- **ADR 9.1-B (preflight fails open):** the workflow-level permanent dedup key is authoritative; the reservation is a best-effort race-closer.
- **Dapr pub/sub at-least-once + unordered:** handlers/activities idempotent and duplicate/late-safe (project-context critical rules).
- **`MemoryUnitId` resolution:** unchanged — `ResolveMemoryUnitId` keeps using `context.InstanceId` (file/url) vs `NewGuid` (EventStore dedup-prefixed Event). The atomic-dedup change must not alter this.

### Testing strategy (xUnit v3 + Shouldly + NSubstitute)
- **Authoritative version note:** tests use **xUnit v3** (`xunit.v3` 3.2.2), `Shouldly` 4.3.0, `NSubstitute` 5.3.0 (`Directory.Packages.props`). (The architecture doc's "xUnit 2.9.3" is stale — trust `Directory.Packages.props`/`project-context.md`.) Global `using Xunit;` already exists via `tests/Directory.Build.props` — don't re-add it. ITANEO MIT header + file-scoped namespaces on new `.cs`.
- **Existing test homes to extend (don't create parallel files):**
  - `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` — token round-trip + back-compat.
  - `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` — stable `IngestAsync` (no `#pragma`), token sent on the wire.
  - `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` + `SaveDedupKeyActivityTests.cs` — atomic reserve winner/loser, `"reserved"` branch, fail-open branch.
  - `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — duplicate/redelivery returns same id, no second unit.
- **Concurrency proof:** a deterministic substitute-based winner/loser test satisfies AC3 at unit level (configure the `When.NotExists` set to return `true` then `false`+id). A true two-thread race, if added, belongs in `tests/Hexalith.Memories.IntegrationTests/` with a real-Redis Aspire/Testcontainers fixture, kept isolated from unit tests. State which level is the authoritative proof.
- Cover success, validation, failure, duplicate/idempotent, cancellation, and fail-open paths (project-context testing rules).

### Running tests in this sandbox (mandatory workaround)
`dotnet test` fails here with `SocketException (13)` (VSTest TCP-listener limitation). Build, then run the xUnit v3 dll directly:
```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
DiffEngine_Disabled=true dotnet exec <…>/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Activities.Ingestion.CheckIdempotencyActivityTests
# `-list methods` prints the discovery count for the Change Log delta.
```
`DiffEngine_Disabled=true` stops snapshot tooling from launching a diff tool. (Epic 17 retro Action Item 4; user auto-memory `running-dotnet-tests-in-sandbox.md`.)

### Process guardrails (Epic 17 retro carry-forwards)
- Track the test-count delta in the **Change Log at every phase** (Action Item 5) — count drift was a recurring review finding.
- Keep the **File List current through the QA phase** (Action Item 4).
- Respect `.editorconfig` (4-space C#, CRLF, UTF-8, final newline) and the ITANEO MIT header on any new `.cs`.
- Central package management — add no `Version` attributes to `.csproj`.

### Project Structure Notes
- Production edits: `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`, `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`, `src/Hexalith.Memories.Server/Activities/Ingestion/{DedupKeyBuilder,CheckIdempotencyActivity,IdempotencyInput}.cs`, `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`, and (Option A) the `/api/ingest` handler in `src/Hexalith.Memories.Server/Program.cs` + `IPreflightDedupStore` wiring. Reuse `IPreflightDedupStore`/`PreflightDedupReservation` from the EventStore package (Server already references it).
- Docs: `docs/dev/experimental-apis.md` (remove `IngestAsync` from HXL001) + an ingest-contract doc section under `docs/dev`.
- Deferred-work: flip `MEM-4` → `resolved` (Story 14.5 schema; `CiTestInventoryTests` parses it).
- No `.slnx` / `Directory.Packages.props` / `release-packages.json` change expected.

### References
- [Source: _bmad-output/planning-artifacts/epics.md#Story 18.4 (lines 3526-3552)] — story statement, ACs, Parties follow-up; Epic 18 preamble (3429-3445), preflight mandate (3437), **release-timing note (3442)**, sequencing note (3444).
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27-parties-consumer-integration-contract-hardening.md (MEM-4 rows: 47, 74, 103)] — residual gap: check-then-act TOCTOU race, no idempotency token, API still experimental.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md (MEM-4, lines 1432-1437; MEM-2/MEM-3 `resolved` precedent ~1408-1431)] — deferred-entry schema (Story 14.5) and the `resolved` + `Evidence:` pattern to copy.
- [Source: _bmad-output/implementation-artifacts/18-3-invocable-route-and-operation-surface-publication.md] — Epic 18 story house style, drift-guard/test patterns, sandbox runner, process guardrails (approved/done).
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:400-464] — `IngestAsync` `[Experimental("HXL001")]`, signature, `PostAsJsonAsync("api/ingest", input, MemoriesJsonContext.Options, ct)`, returns instanceId.
- [Source: src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:9-48] + [MemoriesJsonContext.cs:69,190-206] — contract record + source-gen registration.
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs:15-47; SaveDedupKeyActivity.cs:13-40; DedupKeyBuilder.cs:12-20; IdempotencyInput.cs:12; DedupKeyInput.cs:11] — current check-then-act dedup path.
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:29-103,424-428,521-534] — workflow orchestration, duplicate short-circuit, dedup persist, `ResolveMemoryUnitId`.
- [Source: src/Hexalith.Memories.Server/Program.cs:372-431] — REST `/api/ingest` ingress (no preflight today).
- [Source: src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs; PreflightDedupReservation.cs; EventIngestionService.cs:147-174; TenantEventRoutingOptions.cs:46,50] + [src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs:34-90] — the proven `SET … NX` reservation primitive to reuse, fail-open (ADR 9.1-B), `ReleaseAsync` cleanup, 24h TTL default.
- [Source: docs/dev/experimental-apis.md] — `HXL001` ledger to update (remove `IngestAsync`).
- [Source: _bmad-output/project-context.md] — release rules (`feat` = minor, additive contracts preferred), Dapr at-least-once idempotency, no recursive submodules, central package management, MIT header, CRLF/editorconfig, D9 (concrete client).
- [Source: _bmad-output/planning-artifacts/architecture.md] — Decision D9 (concrete extensibility points), ADR 9.1-B (preflight fails open), Dapr pub/sub at-least-once/unordered constraints, idempotency-testable rule (ingest same event twice → single unit).

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

**Task 0 — Preflight anchor re-verification (against working tree, baseline `d1b2cb6`):**

- ✅ `MemoriesClient.IngestAsync` confirmed `[System.Diagnostics.CodeAnalysis.Experimental("HXL001")]` at `MemoriesClient.cs:414`, `public virtual async Task<string>`, params `(tenantId, caseId, sourceUri, byte[] content, contentType, ingestedBy, IReadOnlyDictionary<string,MetadataField>? metadata, CancellationToken ct)`, POSTs `"api/ingest"` via `PostAsJsonAsync(..., MemoriesJsonContext.Options, ct)` (`:453-454`), returns instanceId via `ReadInstanceIdAsync(response, "instanceId", ct)` (`:463`). The four `[Experimental("HXL001")]` attributes sit at `:279` (CreateTenantAsync), `:353` (CreateCaseAsync), `:414` (IngestAsync), `:646` (GetTelemetrySummaryAsync). Confirmed **no central HXL001 constant** — string literal in each attribute. Confirmed in-repo consumer suppressions at `Cli/Quickstart/*`, `Cli/Commands/StatusTelemetryCommand.cs`, `Mcp/Tools/IngestContentTool.cs`.
- ✅ `IngestionInput` confirmed `sealed record` at `IngestionInput.cs:9-48`; required `TenantId/CaseId/SourceUri/ContentType/SourceType/IngestedBy`; optional `ContentBytes/Metadata(Ordinal-pinned, D6)/CausationId/CorrelationId`. Registered via `[JsonSerializable(typeof(IngestionInput))]` at `MemoriesJsonContext.cs:69`.
- ✅ The race confirmed: `CheckIdempotencyActivity` (`:34-45`) GET-only `StringGetAsync(dedupKey)`, `"reserved"` → `IsTransientReservation` → not duplicate, else `HasValue → duplicate`. `SaveDedupKeyActivity` (`:32-37`) `StringSetAsync(key, memoryUnitId, expiry: null, When.Always)` — permanent, unconditional.
- ✅ Atomic primitive to reuse: `IPreflightDedupStore.TryReserveAsync(key, ttl, ct) → Reserved|Duplicate|FailOpen` + `ReleaseAsync`. `RedisPreflightDedupStore` uses `StringSetAsync(key, "reserved", ttl, When.NotExists)`, fails OPEN on `RedisException`/`TimeoutException` (ADR 9.1-B). `PreflightDedupReservation.ReservedValue == "reserved"`. Default `PreflightDedupTtl = 24h`, `PreflightDedupEnabled = true` (`TenantEventRoutingOptions.cs:46,50`). Registered `TryAddSingleton<IPreflightDedupStore, RedisPreflightDedupStore>()` in `EventStoreIntegrationServiceCollectionExtensions.cs:85` (package impl).
- ✅ **Hazard resolved — EventStore preflight key is `cloudEventId`-derived, NOT `sourceUri`-derived.** `EventIngestionService.ProcessAsync` (`:144`) calls `EventStoreDedupKey.Build(route.TenantId, route.CaseId, envelope.Id)` — the third arg is `envelope.Id` (the CloudEvent id), even though the `EventStoreDedupKey.Build` parameter is misleadingly *named* `sourceUri` and its doc-comment says `sha256(sourceUri)`. The `IPreflightDedupStore` interface doc (`sha256(cloudEventId)`) is the correct description. So the EventStore reservation key (`sha256(cloudEventId)`) is distinct from the workflow's `CheckIdempotencyActivity`/`SaveDedupKeyActivity` key (`sha256(sourceUri)` via `DedupKeyBuilder`). The EventStore schedules with `instanceId == dedupKey` (`EventIngestionService.cs:176`) — its real race-closer is the **deterministic workflow instance id** (Dapr single-instance), with the reservation as an optimisation. The REST `/api/ingest` path (`Program.cs:372-431`) schedules via `ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input)` with **no explicit instanceId** (Dapr generates a random GUID) and **no preflight** — this is the MEM-4 gap.
- ⚠️ **Critical live-code finding that refines Option A:** `RedisPreflightDedupStore.TryReserveAsync` returns **`FailOpen` (not `Duplicate`)** when the key currently holds the transient `"reserved"` marker (`RedisPreflightDedupStore.cs:52-57`). It only returns `Duplicate` for a *permanent* (non-`"reserved"`) value. Consequence: a literal Option-A reuse of `TryReserveAsync` on the dedup key would let a **near-simultaneous** second REST ingest fail-open and proceed, so it would NOT close the AC3 race (it only catches *sequential* duplicates after the winner's permanent key is committed — which `CheckIdempotencyActivity` already handles). The design below accounts for this.
- ✅ `ResolveMemoryUnitId` (`:521-534`): `context.InstanceId` unless `RequiresIndependentMemoryUnitId` (`SourceType.Event` AND `"dedup:"`-prefixed instance id) → `context.NewGuid()`. For `SourceType.File` the memory unit id equals the workflow instance id (today a Dapr-generated GUID). `IdempotencyInput(SourceUri, TenantId, CaseId)` and `IdempotencyResult(IsDuplicate, ExistingMemoryUnitId?)` confirmed; workflow builds `dedupKey` at `:73`, calls CheckIdempotency `:74-77`, duplicate short-circuit `:79-103`, SaveDedupKey `:424-428`.

**Design Decision (Task 1) — Refined Option A: atomic preflight reservation at the REST `/api/ingest` ingress, on a distinct reservation key carrying the winner's workflow instance id.**

Rationale (driven by the two live-code findings above):
1. Reservation lives at the **HTTP ingress** (not in the workflow), so `CheckIdempotencyActivity`, `SaveDedupKeyActivity`, and the EventStore path are left **completely unchanged** — the `"reserved"`-marker ambiguity (Dev Notes trap) never arises.
2. Because the live `TryReserveAsync` maps an in-flight `"reserved"` marker to `FailOpen`, and because the ingress must hand the losing ingest the *winner's* workflow instance id (so it can poll the same result / observe the same `MemoryUnitId`), the reservation uses the proven `SET … NX` primitive **directly** (`IConnectionMultiplexer` injected like `CheckIdempotencyActivity`) via a small testable seam `IngestDedupReservation`, on a **distinct** key namespace `ingest-reserve:{dedupKey}` whose value is the winner's generated instance id. This keeps the public surface change confined to `Hexalith.Memories.Contracts` + `Hexalith.Memories.Client.Rest` (NO new public API on `IPreflightDedupStore` / the EventStore package) while genuinely closing the concurrent race.
3. The `/api/ingest` handler now **generates the workflow instance id** (a GUID) and passes it to `ScheduleNewWorkflowAsync(name, instanceId, input)`. For `SourceType.File`, `MemoryUnitId == instanceId` (unchanged shape — a GUID, exactly as Dapr generated before), so the loser returning the winner's instance id ⇒ both ingests observe the same `MemoryUnitId`. Winner (`Reserved`) → reserve, schedule, return id. Loser (`DuplicateInFlight`) → return the winner's id, do **not** schedule. Redis error (`FailOpen`) → schedule a fresh id (ADR 9.1-B). On scheduling failure after a reservation, `ReleaseAsync` deletes the reservation (compensation; mirrors `EventIngestionService`).
4. **`sourceUri → MemoryUnitId` permanent mapping preserved** (18.5/18.6): `SaveDedupKeyActivity` still writes the `sourceUri`-keyed permanent record exactly as today. The idempotency token **augments**: when a token is supplied, the workflow also writes a token-keyed permanent record (same `MemoryUnitId`) and `CheckIdempotencyActivity` checks the token key first (precedence) then the `sourceUri` key (fallback). When the token is absent, behaviour is byte-for-byte unchanged.

### Completion Notes List

- **AC1 (stable additive entry point):** `MemoriesClient.IngestAsync` graduated out of `[Experimental("HXL001")]`. The original 8-param signature is preserved byte-for-byte (binary-compatible); a new `virtual` overload adds a trailing optional `string? idempotencyToken` before `ct`. Both delegate to private `IngestCoreAsync`. `IngestionInput` gained optional `IdempotencyToken`, serialized as camelCase `idempotencyToken` through the existing `MemoriesJsonContext` registration. The other three HXL001 methods stay experimental. No `IMemoriesClient` (D9). The in-repo MCP consumer (`IngestContentTool`) dropped its now-stale `#pragma warning disable HXL001`.
- **AC2 (token precedence + sourceUri fallback, augment-not-replace):** `DedupKeyBuilder` gained `BuildTokenKey` (`dedup:{t}:{c}:tok:{sha256(token)}`) and `BuildIdentityKey` (token-precedence / sourceUri-fallback). `CheckIdempotencyActivity` checks the token key first then the sourceUri key. The workflow still writes the permanent `sourceUri` record and, when a token is present, additionally writes a token-keyed permanent record at the **same** `MemoryUnitId` — preserving the `sourceUri → MemoryUnitId` mapping for Stories 18.5/18.6.
- **AC3 (atomic dedup, exactly-one-winner):** refined Option A — the REST `/api/ingest` ingress generates the workflow instance id and performs an atomic `SET … NX` preflight reservation on a dedicated `ingest-reserve:{dedupKey}` key (value = winning instance id) **before** scheduling. The losing concurrent ingest returns the winner's instance id without scheduling a second workflow; for `SourceType.File` `MemoryUnitId == instanceId`, so the loser observes the winner's `MemoryUnitId`. `CheckIdempotencyActivity`/`SaveDedupKeyActivity` and the EventStore preflight path are untouched. Fails open on Redis outage (ADR 9.1-B); releases the reservation if scheduling fails (TTL backstop).
- **AC4 (idempotent under at-least-once/unordered):** redelivery short-circuits on the permanent token-keyed (precedence) or sourceUri-keyed (fallback) record and returns the same `MemoryUnitId` with `WasDuplicate: true` and no second unit.
- **Release type:** strictly additive — `feat(story-18.4)` MINOR, **no** `BREAKING CHANGE:`. No `release-packages.json` / `.slnx` / `Directory.Packages.props` / package-version change.
- **Live-code deviation from the literal Option A in the story:** the existing `RedisPreflightDedupStore.TryReserveAsync` maps an in-flight `"reserved"` marker to `FailOpen` (not `Duplicate`) and returns no existing value, so it cannot close a near-simultaneous race nor hand the loser the winner's id. The implementation therefore performs the `SET … NX` directly via `IConnectionMultiplexer` in a small testable seam (`IngestDedupReservation`) on a distinct key namespace — keeping the public surface confined to `Contracts` + `Client.Rest` (no new public API on `IPreflightDedupStore`). Rationale recorded in Debug Log → Design Decision.

### File List

**Production:**
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs` — added optional `IdempotencyToken`.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — removed `HXL001` from `IngestAsync`; added token overload + private `IngestCoreAsync`.
- `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs` — added `BuildTokenKey` + `BuildIdentityKey`.
- `src/Hexalith.Memories.Server/Activities/Ingestion/IdempotencyInput.cs` — added optional `IdempotencyToken`.
- `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs` — token-precedence/sourceUri-fallback check.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — thread token into idempotency check; write token-keyed permanent record additively.
- `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs` — **new** atomic REST-ingress preflight reservation seam.
- `src/Hexalith.Memories.Server/Program.cs` — registered `IngestDedupReservation`; wired the reservation into `/api/ingest` (generate instance id, reserve, schedule, release-on-failure).
- `src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs` — dropped the now-stale `#pragma warning disable HXL001`.

**Tests:**
- `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` — token round-trip + back-compat (+3).
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` — stable client + token on the wire (+2).
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` — **new**, atomic winner/loser + fail-open (+8).
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` — token precedence/fallback (+4).
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — token threading, duplicate short-circuit, dual permanent record (+4).

**QA phase (bmad-qa-generate-e2e-tests, 2026-06-25) — gap auto-apply (tests only, no production change):**
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/DedupKeyBuilderTests.cs` — **new**, direct key-derivation invariants: `:tok:` namespace augments-not-replaces, token precedence / sourceUri fallback, tenant/case isolation, lowercase-hex SHA-256 (+13).
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` — **+4**: `SET NX`-fails-then-key-expired → fail-open, blank `instanceId` validation (×2), `ReleaseAsync` swallows a Redis failure.
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` — **+1**: blank/whitespace token normalizes to `null` on the wire.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` — appended the Story 18.4 QA section (gaps, coverage map, deferred `/api/ingest` integration boundary).

**Docs / artifacts:**
- `docs/dev/experimental-apis.md` — `IngestAsync` removed from the `HXL001` row.
- `docs/dev/ingest-contract.md` — **new** stable ingest-contract doc.
- `_bmad-output/implementation-artifacts/deferred-work.md` — MEM-4 → `resolved` + `Evidence:`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story status `ready-for-dev` → `in-progress` → `review`.

## Change Log

| Date | Phase | Change | Test count |
| :--- | :--- | :--- | :--- |
| 2026-06-25 | create-story | Initial story context created (ready-for-dev). Release-sensitive `feat:` code story: graduate `IngestAsync` out of HXL001, add optional `IdempotencyToken`, make same-source dedup atomic by reusing the proven `IPreflightDedupStore` `SET … NX` primitive on the REST ingest path. All anchors verified against live source at baseline `d1b2cb6`; identified the EventStore-preflight `"reserved"`-marker ambiguity hazard and the 18.5/18.6 `sourceUri → MemoryUnitId` permanence constraint. | n/a (no tests added yet) |
| 2026-06-25 | dev-story (Task 0/1) | Re-verified all anchors against the working tree. Resolved the EventStore-key hazard (EventStore preflight is `cloudEventId`-derived, distinct from the workflow's `sourceUri` key). Found the live `RedisPreflightDedupStore.TryReserveAsync` maps in-flight `"reserved"` → `FailOpen` (not `Duplicate`) — drove the refined Option A (ingress `SET … NX` on a distinct key carrying the winner's instance id, public surface confined to Contracts + Client.Rest). | baseline: Contracts 542, Cli 382, Server 1871 |
| 2026-06-25 | dev-story (Tasks 2-4) | Implemented optional `IdempotencyToken` (contract + dedup-key builders + activity + workflow), graduated `IngestAsync` out of HXL001 (additive overload), and the atomic REST-ingress reservation (`IngestDedupReservation` + `/api/ingest` wiring). Added tests across all four ACs. | +21 new (Contracts 545, Cli 384, Server 1887) |
| 2026-06-25 | dev-story (Tasks 5-6) | Docs (`experimental-apis.md`, new `ingest-contract.md`), MEM-4 → `resolved`. Full suites green: Contracts 545/0, Cli 384/0 (incl. CiTestInventoryTests 48/0), Server 1887/0 (1 pre-existing skip), Mcp 83/0. Full `.slnx` build 0 warnings / 0 errors. Additive-only (`feat:` MINOR), no `BREAKING CHANGE:`. | Contracts 545, Cli 384, Server 1887, Mcp 83 |
| 2026-06-25 | qa-generate-e2e-tests | QA gap audit of the feature's own production branches/boundaries; auto-applied 5 gaps (tests only, no production change): new `DedupKeyBuilderTests` (+13, central token-augments-not-replaces / precedence / tenant-isolation invariants), `IngestDedupReservationTests` (+4: expired→fail-open, blank-id ×2, release-resilience), `MemoriesClientTests` (+1: blank-token→null normalization). Documented the `/api/ingest` handler-wiring integration boundary as deferred (matches story strategy). Full suites green: Server 1904/0 (1 pre-existing skip), Cli 385/0, Contracts 545/0. Build 0 warnings / 0 errors. | +18 QA (Server 1904, Cli 385, Contracts 545) |
| 2026-06-25 | senior-dev-review (AI) | Adversarial review: all 4 ACs verified IMPLEMENTED against live source; every `[x]` task confirmed done; File List matches git reality; per-class test counts re-run and confirmed (Contracts `IngestionInputSerializationTests` 9, Cli `MemoriesClientTests` 11, Server `IngestDedupReservationTests` 12 / `DedupKeyBuilderTests` 13 / `CheckIdempotencyActivityTests` 9 / `IngestionWorkflowTests` 39). **1 MEDIUM auto-fixed:** two NEW files (`src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs`, `docs/dev/ingest-contract.md`) were committed LF-only, violating `.editorconfig` `end_of_line = crlf` (every other touched file is CRLF; no `.gitattributes` to normalize) — normalized both to CRLF. Server rebuild after fix: 0 warnings / 0 errors. 0 CRITICAL / 0 HIGH → status `review` → `done`. | unchanged (line-ending fix only) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (AI adversarial review) · **Date:** 2026-06-25 · **Outcome:** ✅ Approve (status → `done`)

### Scope verified
Read every file in the File List against live source, cross-referenced the story File List against `git status` (matches — non-File-List changes are `_bmad-output/`/`.claude/` automation artifacts, excluded per review policy), and re-ran the affected test classes in the sandbox (`dotnet exec` + `DiffEngine_Disabled=true`).

### Acceptance Criteria
- **AC1 — stable additive entry point:** IMPLEMENTED. `IngestAsync` no longer `[Experimental("HXL001")]`; original 8-param signature preserved byte-for-byte; new `virtual` overload adds trailing optional `idempotencyToken`; both delegate to `IngestCoreAsync`. `IngestionInput.IdempotencyToken` added, serialized camelCase via `MemoriesJsonContext`. MCP `IngestContentTool` pragma removed. The other three HXL001 methods untouched. No `IMemoriesClient` (D9).
- **AC2 — token precedence + sourceUri fallback, augment-not-replace:** IMPLEMENTED. `DedupKeyBuilder.BuildTokenKey` (`:tok:` namespace) + `BuildIdentityKey` (token precedence / sourceUri fallback). `CheckIdempotencyActivity` checks token key first, then sourceUri. Workflow writes the permanent sourceUri record AND (token present) a token-keyed record at the same `MemoryUnitId` — preserving the 18.5/18.6 `sourceUri → MemoryUnitId` mapping.
- **AC3 — atomic dedup:** IMPLEMENTED. `IngestDedupReservation` performs `SET … NX` on a distinct `ingest-reserve:` key (value = winner's instance id) at the REST ingress before scheduling; loser returns the winner's instance id without scheduling; fail-open on Redis error; release-on-scheduling-failure. DI-registered in `Program.cs:141` and wired into `/api/ingest`. Winner/loser/fail-open/release proven by `IngestDedupReservationTests` (12/12 green).
- **AC4 — idempotent under at-least-once/unordered:** IMPLEMENTED. Duplicate short-circuit (`IngestionWorkflow.cs:79-103`) returns the same `MemoryUnitId`, `WasDuplicate: true`, no second unit; token-keyed duplicate also short-circuits (`IngestionWorkflowTests` green).

### Findings
- **[MEDIUM · FIXED] CRLF line-ending violation on two new files.** `src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs` and `docs/dev/ingest-contract.md` were written LF-only, breaking `.editorconfig` `end_of_line = crlf` (applies to `[*]`; not overridden for `.cs`/`.md`) and the story's own "Respect .editorconfig … CRLF" guardrail. With no `.gitattributes`, the LF bytes would be committed as-is. **Auto-fixed:** normalized both to CRLF; Server rebuild 0/0.
- **[LOW · noted, not changed] Unused `CancellationToken` parameter** in `IngestDedupReservation.TryReserveAsync`/`ReleaseAsync`. StackExchange.Redis async methods take `CommandFlags`, not a `CancellationToken`, and the `/api/ingest` handler passes `CancellationToken.None`. The parameter is retained for signature parity with the `IPreflightDedupStore.TryReserveAsync(key, ttl, ct)` primitive the design intentionally mirrors — defensible API shape; left as-is.
- **[LOW · noted, not changed] Token not permanently anchored when its first use fall-back-dedups to a pre-existing sourceUri unit.** If a token's very first ingest dedups via the `sourceUri` fallback (that sourceUri already has a unit), the workflow short-circuits before writing a token-keyed record, so the token relies on the 24h reservation key rather than a permanent record. Reaching a divergence requires the same token to later ingest a *different* sourceUri *after* the reservation TTL expires — outside the story's near-simultaneous-same-source threat model. Fixing would add a write to the guarded duplicate short-circuit path (regression risk > contrived benefit); recorded as a known edge for a future hardening story rather than changed here.

### Quality gates
Build: Contracts.Tests / Cli.Tests / Server.Tests + Server all 0 warnings / 0 errors. Tests re-run green per class (counts above). Release surface confirmed strictly additive (`feat:` MINOR, no `BREAKING CHANGE:`); no `release-packages.json` / `.slnx` / `Directory.Packages.props` change.
