---
project: memories
date: 2026-07-16
status: approved
change_scope: minor
approved_by: Administrator
---

# Sprint Change Proposal — Reconcile consistency inspect with the opaque MemoryUnitId contract

## 1. Issue Summary

Epic 21 retrospective action item 1 requires the repository to reconcile the consistency-inspect opaque-ID contract across documentation and API messaging. Repository inspection confirms a real cross-surface conflict:

- The authoritative stability contract defines `MemoryUnitId` as an opaque stable string that callers must not parse, reconstruct, or assume is a ULID or time-sortable value.
- The architecture uses `string (opaque)` for the Memory Unit identifier.
- `docs/dev/consistency.md` says the inspect endpoint accepts an opaque, non-blank identifier and demonstrates a non-ULID value.
- `ConsistencyInspectionService` rejects every non-blank value that is not a Crockford-base32 ULID, GUID-D, or GUID-N, and it normalizes GUID-N input to GUID-D before lookup.
- The CLI option description, endpoint recovery suggestion, REST-client XML documentation, consistency-contract XML documentation, and regression tests repeat or enforce the obsolete ULID/GUID-only restriction.

The 2026-07-05 Epic 21 documentation verification corrected the operator guide but did not account for the service-specific format guard. The result is documentation that describes the intended opaque contract while the live API rejects a safe opaque value such as `wf-file-instance-7` before probing any backend.

This mismatch matters even though current REST ingestion normally produces GUID-shaped IDs: `IngestionWorkflow.ResolveMemoryUnitId` exposes a workflow instance ID without establishing ULID syntax as a public invariant. An operator must be able to pass the exact identifier returned by Memories to consistency inspect without parsing or reformatting it.

## 2. Impact Analysis

### Epic and story impact

- Epic 8 and Story 8.2 remain complete. The correction is a bounded maintenance adjustment to the shipped per-unit inspection surface; the completed story is not reopened or rewritten.
- Epic 18 and Story 18.6 remain complete. Their opaque-ID and stability semantics are the contract this correction follows.
- Epic 21 remains complete. Its retrospective action item stays open until the approved implementation, documentation, and validation evidence are present.
- No current or future epic needs resequencing, and no new epic or story is required.

### Artifact conflicts

- **PRD:** No conflict. FR73/FR74 and the MVP scope remain unchanged.
- **Epics:** No requirement or acceptance-criteria change is needed. Story 8.2 already calls the argument `<unit-id>` without imposing ULID syntax.
- **Architecture:** No edit is needed. The current Memory Unit inventory already defines `Id` as an opaque string and cross-links the stability contract.
- **UX:** Not applicable. Consistency inspection is a CLI/REST operator surface and this correction does not introduce a UI flow or visual change.
- **Developer documentation:** `docs/dev/consistency.md` has the correct headline predicate but needs exact-value guidance and a link to the authoritative stability contract. `docs/dev/memory-unit-id-stability.md` should identify consistency inspect as an exact-value consumer.
- **Historical implementation records:** Story 8.2's 2026-04 ULID/GUID decision remains historical evidence and will not be rewritten. This proposal and the retrospective action-status evidence record its deliberate supersession.

### Technical and public-contract impact

The change widens accepted input without changing the route, serialized response, error-envelope shape, or client method signature:

- Safe non-blank opaque IDs become inspectable.
- Unknown opaque IDs reach the backend probes and return `404 MEMORY_UNIT_NOT_FOUND` rather than failing syntactic validation with `400 INVALID_MEMORY_UNIT_ID`.
- Blank input remains invalid.
- Exact input is probed before any compatibility conversion.
- Existing GUID-N-to-GUID-D lookup compatibility is retained as a fallback only when the exact GUID-N value is absent.
- Cypher remains parameterized; accepting a non-ULID value does not interpolate it into query text.

The existing REST template remains `.../consistency/inspect/{memoryUnitId}`. This minor correction does not redesign the route for slash-bearing identifiers or change ASP.NET Core encoded-slash handling. Here, opaque means that callers must not infer identifier type or semantics; it does not promise unrestricted URL grammar. Callers continue to pass the exact route-segment value returned by Memories through the route builder/client, which URL-escapes segments.

### Operational impact

- Existing ULID and GUID calls continue to work.
- CLI scripts do not need argument or output changes.
- Error recovery becomes more actionable by directing operators to the ingest result or source-URI lookup rather than asking them to manufacture a value of a particular syntax.
- No deployment, data migration, state-store migration, configuration, or backfill is required.

## 3. Recommended Approach

Use **Direct Adjustment**.

- **Effort:** Small; one focused runtime refactor plus messaging and regression coverage.
- **Risk:** Low-to-moderate because accepted input broadens, mitigated by exact-first lookup, parameterized graph queries, unchanged tenant authorization, and focused endpoint tests.
- **Timeline impact:** None to sprint sequencing.
- **MVP impact:** None; this restores an existing cross-cutting contract.
- **Release impact:** Additive compatibility correction; no versioned wire-shape change.

Alternatives considered:

1. **Document the current ULID/GUID-only restriction.** Rejected because it would make the inspect API depend on syntax the authoritative MemoryUnitId contract explicitly does not guarantee.
2. **Remove GUID-N compatibility outright.** Rejected because the shipped API advertised GUID-N input and an exact opaque contract can coexist with an exact-miss compatibility fallback.
3. **Redesign inspect to use a query parameter or catch-all route.** Deferred as unnecessary scope expansion. Current IDs are representable in the existing single-segment route, and no slash-bearing-ID failure triggered this action.
4. **Rollback the opaque-ID documentation.** Rejected because it would restore stale guidance rather than reconcile the implementation with the authoritative contract.
5. **Review or reduce MVP scope.** Not applicable; the correction neither adds a product capability nor threatens MVP viability.

## 4. Detailed Change Proposal

### 4.1 Runtime inspection contract

**Artifact:** `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs`

**Old behavior:**

```text
Accept only ULID, GUID-D, or GUID-N values; normalize GUID-N to GUID-D before every probe.
Reject every other non-blank value with ArgumentException.
```

**Proposed behavior:**

```text
Require a non-blank MemoryUnitId and probe the exact value without type parsing or normalization.

If the exact value is absent everywhere and the input is a GUID-N value, retry the GUID-D
equivalent as a backward-compatibility alias. Return the identifier of the record actually found.

Never reject an identifier merely because it is not a ULID or GUID.
```

Implementation notes:

- Extract the backend probe sufficiently to support exact-first and optional compatibility-fallback attempts without duplicating recommendation/detail construction.
- Remove ULID syntax from the public validation contract and comments.
- Keep cancellation propagation and the all-backends-absent `KeyNotFoundException` behavior.
- Keep graph queries parameterized through `IGraphQueryBuilder`.
- Do not trim, lowercase, uppercase, or otherwise rewrite opaque exact input.

### 4.2 Operator, API, client, and contract messaging

| Artifact | Current stale message | Proposed message |
|---|---|---|
| `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs` | `26-char Crockford-base32 ULID or legacy GUID` | `Opaque memory unit identifier; pass the exact value returned by Memories.` |
| `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs` | Recovery suggestion requires a ULID or GUID | `Pass the exact non-blank MemoryUnitId returned by ingest or source-URI lookup; do not parse or reformat it.` |
| `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` | Parameter must match the ULID pattern | Parameter is an opaque identifier and must be passed exactly as returned |
| `src/Hexalith.Memories.Contracts/V1/ConsistencyInspectionResult.cs` | `Memory unit identifier (ULID)` | `Opaque memory unit identifier` |
| `src/Hexalith.Memories.Contracts/V1/ConsistencyDiscrepancy.cs` | `Memory unit identifier (ULID)` | `Opaque memory unit identifier` |
| `docs/dev/consistency.md` | Correct opaque predicate but limited explanation | Retain predicate; add exact-value/no-reformat guidance and link to the stability contract |
| `docs/dev/memory-unit-id-stability.md` | Defines opacity but does not identify inspect as a consumer | Add consistency inspect as an exact-value operator consumer |

The CLI walkthrough will continue using a non-ULID example such as `wf-file-instance-7`. No message may promise ULID/GUID syntax, time ordering, or caller-side normalization.

### 4.3 Regression and drift-guard coverage

**Artifacts:**

- `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyInspectCommandTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs`

Required evidence:

1. A safe non-ULID opaque value such as `wf-file-instance-7` is accepted, used unchanged for exact backend lookups, and returned unchanged when present.
2. An unknown non-ULID opaque value returns `404 MEMORY_UNIT_NOT_FOUND`, not `400 INVALID_MEMORY_UNIT_ID`.
3. Empty and whitespace-only input remains invalid at callable boundaries.
4. Graph query text remains independent of the identifier and the identifier is supplied through the parameter map.
5. Exact lookup wins when a GUID-N-shaped identifier exists as stored; GUID-D conversion occurs only after an exact miss and preserves the previously supported alias behavior.
6. CLI help and HTTP recovery guidance use the opaque/exact-value contract and contain no ULID/GUID requirement.
7. The MemoryUnitId stability drift guard ties the authoritative contract to the consistency guide and inspect-facing source descriptions, so documentation-only or code-only regression fails a focused test.

### 4.4 Sprint action tracking

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

Keep the Epic 21 action item `open` while this proposal is pending or implementation/validation is incomplete. After all success criteria pass, update it to `done` with a dated evidence comment naming the opaque exact lookup, compatibility fallback, messaging alignment, and focused validation.

No epic/story status row changes.

## 5. Implementation Handoff

**Classification:** Minor — direct implementation by the Developer agent, with documentation review by Paige.

### Ordered implementation plan

1. Add or update focused tests to express the approved opaque exact-value behavior and compatibility fallback.
2. Refactor `ConsistencyInspectionService` to probe exact input first and apply GUID-N-to-GUID-D fallback only after an exact all-backend miss.
3. Align endpoint, CLI, client, contract, and developer-documentation messaging.
4. Extend the stability drift guard across the consistency-inspect surfaces.
5. Run focused Contracts, Server, and CLI tests, then build `Hexalith.Memories.slnx` in Release configuration using the repository's bounded-build conventions.
6. Record validation evidence and only then mark the Epic 21 retrospective action `done`.

### Guardrails

- Preserve tenant validation, tenant authorization, rate limiting, backend-unavailable mapping, cancellation, telemetry exclusions, response serialization, and all consistency-repair behavior.
- Preserve public method signatures, route templates, response records, JSON property names, and error codes.
- Preserve exact opaque values; do not make normalization the primary lookup path.
- Preserve the shipped GUID-N compatibility path as exact-miss fallback.
- Do not broaden this correction into CaseValidator policy, route redesign, memory-id generation, key-schema migration, or slash-bearing-ID support.
- Do not rewrite completed Story 8.2 or unrelated historical evidence.
- Preserve unrelated working-tree changes.

### Success criteria

- `memories consistency inspect --tenant acme --id wf-file-instance-7` reaches the server probe rather than failing ULID/GUID syntax validation.
- A present opaque ID is probed and returned exactly.
- An absent opaque ID produces `MEMORY_UNIT_NOT_FOUND`.
- Existing ULID, GUID-D, and GUID-N compatibility scenarios remain covered.
- CLI help, endpoint recovery guidance, client XML documentation, contract XML documentation, and both developer guides state the opaque exact-value rule.
- Parameterized Cypher and current authorization/error/telemetry behavior remain green.
- Focused Contracts, Server, and CLI tests pass, followed by a successful Release solution build.
- The Epic 21 retrospective action is marked `done` only after the preceding evidence exists.

## 6. Risk and Mitigation Summary

| Risk | Mitigation |
|---|---|
| GUID-N callers lose the previously advertised alias behavior | Exact-first lookup followed by GUID-D fallback only on exact miss |
| A broad identifier is mistaken for query text | Preserve parameterized Cypher and add a parameterization regression test |
| Opaque is interpreted as unrestricted URL grammar | Document the unchanged single-segment route boundary; defer route redesign |
| Docs and runtime drift again | Cross-surface stability drift guard plus behavioral endpoint/CLI tests |
| A missing opaque ID changes from 400 to 404 unexpectedly | Treat this as the intended additive correction and assert the new domain-accurate error in endpoint tests |

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | Trigger confirmed from Epic 21 retrospective action item 1 | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, Story 8.2, Story 18.6, developer docs, source, tests, and sprint status reviewed | Complete |
| 2026-07-16 | Runtime opaque exact-value direction reviewed incrementally | Approved by Administrator |
| 2026-07-16 | Documentation and API messaging edit reviewed incrementally | Approved by Administrator |
| 2026-07-16 | Test and drift-guard edit reviewed incrementally | Approved by Administrator |
| 2026-07-16 | Scope, tracking, and handoff edit reviewed incrementally | Approved by Administrator |
| 2026-07-16 | Consolidated Sprint Change Proposal written | Complete |
| 2026-07-16 | Consolidated proposal approved by Administrator | Approved; Minor-scope Developer/Paige handoff authorized |

## Checklist Record

### 1. Understand the trigger and context

- [N/A] 1.1 No implementation story is currently failing; the trigger is Epic 21 retrospective action item 1 against completed Story 8.2 behavior.
- [x] 1.2 Core problem defined: authoritative opaque-ID guidance conflicts with the inspect service's ULID/GUID-only guard and its public messages.
- [x] 1.3 Evidence collected from the retrospective, documentation-verification record, planning artifacts, live source, tests, and sprint status.

### 2. Epic impact assessment

- [x] 2.1 Epic 8, Epic 18, Epic 21, and the current sprint plan remain viable.
- [N/A] 2.2 No epic-level modification is required.
- [x] 2.3 Remaining epics reviewed; no dependency or sequencing impact found.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No priority or sequencing change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict or modification required.
- [x] 3.2 Architecture reviewed; it already carries the authoritative opaque-ID wording.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Story 8.2, Story 18.6, developer docs, REST/client/CLI/contract messaging, runtime behavior, tests, route transport, and sprint tracking reviewed.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable; effort small and risk low-to-moderate.
- [x] 4.2 Rollback/document-the-restriction was evaluated and rejected because it conflicts with the authoritative identifier contract.
- [N/A] 4.3 PRD/MVP review is unnecessary.
- [x] 4.4 Direct Adjustment selected with exact-first lookup and compatibility fallback.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic, story, artifact, technical, and operational impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact, detailed changes, risk mitigations, and action plan documented.
- [x] 5.5 Minor-scope Developer/Paige handoff documented.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked against repository evidence and all incremental edits approved.
- [x] 6.3 Explicit approval received from Administrator on 2026-07-16.
- [!] 6.4 `sprint-status.yaml` intentionally remains open pending implementation evidence.
- [x] 6.5 Developer/Paige handoff and implementation success criteria are defined and authorized.
