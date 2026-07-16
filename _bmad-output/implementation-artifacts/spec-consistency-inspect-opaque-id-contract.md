---
title: 'Reconcile consistency inspect with the opaque MemoryUnitId contract'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
baseline_commit: '119c0a4954b62d9551642dfbee6342b69817ec06'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-consistency-inspect-opaque-id-contract.md'
  - '{project-root}/docs/dev/memory-unit-id-stability.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Consistency inspect publicly describes `MemoryUnitId` as opaque, but the service and operator messaging reject every non-ULID/GUID value and normalize GUIDs before lookup. Operators therefore cannot inspect an exact safe identifier returned by Memories, such as `wf-file-instance-7`.

**Approach:** Accept every non-blank route-segment identifier, probe it unchanged across all backends, and use GUID-N-to-GUID-D conversion only as an exact-miss compatibility fallback. Align CLI, endpoint, client, contract, and developer-documentation wording and bind them with focused behavioral and drift-guard tests.

## Boundaries & Constraints

**Always:** Preserve tenant validation/authorization, rate limiting, cancellation, telemetry exclusions, backend-unavailable mapping, route and JSON shapes, parameterized Cypher, existing repair recommendations, and unrelated dirty-worktree changes. Return the identifier of the record actually found. Treat exact input as authoritative; do not trim, case-fold, or normalize it before the first probe.

**Ask First:** Any public signature or route-template change; any new dependency; any need to alter `RepairUnitActivity`, ID generation, key schemas, tenant routing, or serialization; any conflict with concurrent edits that cannot be merged without changing their intent.

**Never:** Add slash-bearing/catch-all route support, change `CaseValidator`, migrate/backfill data, rewrite completed stories, reopen an epic/story, weaken graph parameterization, or mark the Epic 21 action complete before all required validation succeeds. Keep the legacy `NormalizeMemoryUnitId` repair seam behavior unchanged; `InspectAsync` must stop using it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Opaque exact hit | `wf-file-instance-7` exists on any backend | Probe and return that exact value unchanged | None |
| Opaque miss | Safe non-blank opaque ID absent everywhere | Probe all backends once | `404 MEMORY_UNIT_NOT_FOUND`, not format `400` |
| Blank callable input | Empty or whitespace ID | No backend probe | `ArgumentException` / existing boundary validation |
| GUID-N exact hit | GUID-N value exists exactly | Exact value wins; no alias probe | None |
| GUID-N alias hit | Exact GUID-N absent; GUID-D equivalent exists | Retry GUID-D once and return GUID-D | `404` only if both attempts miss |
| Adversarial opaque text | Non-blank metacharacters in a valid route segment | Redis keys use the exact value; Cypher text stays constant and `$id` carries the value | Backend errors propagate unchanged |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` -- strict pre-probe format guard and backend probing; refactor to exact-first with nullable miss and GUID-N fallback.
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` -- existing constant-query/parameter-map seam to keep and verify.
- `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs` -- HTTP error mapping and stale recovery guidance.
- `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs` -- operator help text.
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` and `src/Hexalith.Memories.Contracts/V1/Consistency*.cs` -- public XML contract wording only; no CLR/JSON changes.
- `docs/dev/consistency.md` and `docs/dev/memory-unit-id-stability.md` -- exact-value consumer guidance and cross-links.
- `tests/Hexalith.Memories.Server.Tests/{Consistency,Endpoints,Ingestion}` and `tests/Hexalith.Memories.Cli.Tests/{Cli,ClientRest}` -- behavior, messaging, parameterization, and cross-surface drift guards.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- close only the approved Epic 21 retrospective action after validation.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs` -- replace syntax-rejection expectations with red tests for opaque exact hit/miss, blank rejection, GUID-N exact-wins/fallback, and parameterized graph input.
- [x] `src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs` -- extract a single-candidate probe, remove format validation from `InspectAsync`, and add exact-miss GUID-N fallback without swallowing cancellation/backend failures.
- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs` and `tests/Hexalith.Memories.Cli.Tests/{Cli/ConsistencyInspectCommandTests.cs,ClientRest/MemoriesClientConsistencyTests.cs}` -- prove opaque forwarding, 404 mapping, blank client/CLI validation, and exact-value help/recovery text.
- [x] `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs`, `src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs`, `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`, and `src/Hexalith.Memories.Contracts/V1/Consistency{InspectionResult,Discrepancy}.cs` -- remove positive ULID/GUID requirements while preserving signatures and envelopes.
- [x] `docs/dev/consistency.md` and `docs/dev/memory-unit-id-stability.md` -- publish exact/no-reformat guidance and reciprocal links without promising unrestricted URL grammar.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs` -- merge an inspect cross-surface drift guard into the concurrent documentation-hardening work without overwriting it.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- after all gates pass, mark only “Reconcile the consistency inspect opaque-ID contract across docs and API messaging” done with dated focused evidence. The exact-tree Release gate is currently blocked by unrelated dependency drift.

**Acceptance Criteria:**
- Given any present safe non-blank opaque identifier, when consistency inspect runs, then every probe receives the exact value and the response returns it unchanged.
- Given an absent opaque identifier, when the endpoint runs, then it returns `404 MEMORY_UNIT_NOT_FOUND` with exact-value recovery guidance and no ULID/GUID requirement.
- Given GUID-N input, when the exact record exists, then exact wins; when it is absent and GUID-D exists, then the alias is probed once and returned.
- Given opaque metacharacters, when graph queries are built, then query text is identifier-independent and the parameter map contains the exact identifier.
- Given the public inspect surfaces, when drift guards run, then CLI/API/client/contracts/docs agree on opaque exact-value semantics while route, JSON, authorization, telemetry, repair, and backend error behavior remain unchanged.

## Spec Change Log

## Design Notes

The compatibility decision is deliberately asymmetric: only GUID-N receives an exact-miss GUID-D retry, as approved. Uppercase/lowercase rewriting and GUID-D normalization are not primary lookup behavior. `NormalizeMemoryUnitId` remains available unchanged for the existing repair workflow, but `InspectAsync` no longer calls it; expanding opaque-ID repair semantics requires separate approval.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Release/net10.0/Hexalith.Memories.Contracts.Tests.dll -parallel none -class Hexalith.Memories.Contracts.Tests.V1.ConsistencyContractSerializationTests` -- expected: all contract tests pass (pre-change 19/19).
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -class Hexalith.Memories.Server.Tests.Consistency.ConsistencyInspectionServiceTests -class Hexalith.Memories.Server.Tests.Endpoints.ConsistencyEndpointTests -class Hexalith.Memories.Server.Tests.Ingestion.MemoryUnitIdStabilityContractTests` -- expected: all focused Server tests pass (pre-change 45/45).
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -parallel none -class Hexalith.Memories.Cli.Tests.Cli.ConsistencyInspectCommandTests -class Hexalith.Memories.Cli.Tests.ClientRest.MemoriesClientConsistencyTests` -- expected: all focused CLI/client tests pass (pre-change 9/9).
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release && dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1 /nodeReuse:false` -- expected: zero warnings and errors.

**Evidence (2026-07-16):** Red service coverage failed 4/18 before implementation and passed after the exact-first refactor. Post-review focused runs passed Contracts 19/19, Server 84/84 (including `IndexSchemaDefinitionsTests`), and CLI/client 13/13. A fresh Release solution restore/build passed with 0 warnings/errors using the spec-baseline `Hexalith.Builds@2044475` package props. The exact unpinned restore remains separately blocked by the concurrent user-owned `Hexalith.Builds@8e0e2da` pointer (`NU1605`: OTLP exporter 1.17.0 requires OpenTelemetry core 1.17.0 while this repository pins core 1.16.0); no scoped file changes that dependency state.

**Adversarial review (2026-07-16):** Three independent layers produced no `intent_gap` or `bad_spec`. Auto-fixed patch findings covered semantic-key owner collision (`high`), Redis glob-pattern amplification (`medium`), endpoint 400 recovery verification (`medium`), and exact CLI/client/encoded-endpoint forwarding (`medium`). The exact-tree package-version conflict was classified `defer` and recorded in `deferred-work.md`; the Epic 21 action remains `open` until that approved Release gate can run against the actual tree. The proposed uppercase GUID-D fallback was rejected because the frozen contract deliberately authorizes only GUID-N-to-GUID-D exact-miss fallback.

## Suggested Review Order

**Exact-first runtime behavior**

- Start here: exact input wins before the narrow GUID-N compatibility fallback.
  [`ConsistencyInspectionService.cs:74`](../../src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs#L74)

- One candidate probe preserves cancellation and backend error behavior across all stores.
  [`ConsistencyInspectionService.cs:103`](../../src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs#L103)

- Stored owner verification prevents opaque IDs colliding with another unit's chunk key.
  [`ConsistencyInspectionService.cs:128`](../../src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs#L128)

- Glob escaping keeps Redis scans literal for metacharacter-bearing opaque identifiers.
  [`IndexSchemaDefinitions.cs:166`](../../src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs#L166)

- Compatibility remains deliberately asymmetric and runs only after an exact miss.
  [`ConsistencyInspectionService.cs:191`](../../src/Hexalith.Memories.Server/Consistency/ConsistencyInspectionService.cs#L191)

**Public contract alignment**

- HTTP 400 and 404 envelopes now direct callers to the exact returned value.
  [`ConsistencyEndpoints.cs:155`](../../src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs#L155)

- CLI help describes the identifier as opaque without changing command shape.
  [`ConsistencyInspectCommand.cs:45`](../../src/Hexalith.Memories.Cli/Commands/ConsistencyInspectCommand.cs#L45)

- REST client documentation and routing preserve the identifier byte-for-byte.
  [`MemoriesClient.cs:979`](../../src/Hexalith.Memories.Client.Rest/MemoriesClient.cs#L979)

- Contract records remove the former positive syntax implication without changing serialization.
  [`ConsistencyInspectionResult.cs:15`](../../src/Hexalith.Memories.Contracts/V1/ConsistencyInspectionResult.cs#L15)

- Operator guidance defines exact-value use within the unchanged single-segment route.
  [`consistency.md:39`](../../docs/dev/consistency.md#L39)

- Stability guidance makes source-URI resolution the authoritative consumer path.
  [`memory-unit-id-stability.md:75`](../../docs/dev/memory-unit-id-stability.md#L75)

**Behavioral and drift verification**

- Collision regression proves foreign semantic chunks cannot create false presence.
  [`ConsistencyInspectionServiceTests.cs:121`](../../tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs#L121)

- Compatibility tests prove GUID-N exact-wins and exact-miss fallback ordering.
  [`ConsistencyInspectionServiceTests.cs:156`](../../tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs#L156)

- Parameterization regression keeps adversarial identifiers out of Cypher query text.
  [`ConsistencyInspectionServiceTests.cs:223`](../../tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs#L223)

- Endpoint tests cover 404, 400, encoded forwarding, and exact response identity.
  [`ConsistencyEndpointTests.cs:197`](../../tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs#L197)

- CLI tests prove whitespace and casing survive the command boundary unchanged.
  [`ConsistencyInspectCommandTests.cs:24`](../../tests/Hexalith.Memories.Cli.Tests/Cli/ConsistencyInspectCommandTests.cs#L24)

- Client tests prove reserved characters are escaped without semantic rewriting.
  [`MemoriesClientConsistencyTests.cs:98`](../../tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientConsistencyTests.cs#L98)

- Cross-surface drift guard binds service ordering, docs, endpoint, CLI, client, and contracts.
  [`MemoryUnitIdStabilityContractTests.cs:180`](../../tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs#L180)

- Infrastructure coverage locks Redis glob escaping independently of service behavior.
  [`IndexSchemaDefinitionsTests.cs:36`](../../tests/Hexalith.Memories.Server.Tests/Infrastructure/IndexSchemaDefinitionsTests.cs#L36)

**Tracking boundary**

- Epic action stays open until the exact working tree passes the Release gate.
  [`sprint-status.yaml:523`](sprint-status.yaml#L523)
