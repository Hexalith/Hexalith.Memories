---
title: 'Preserve deterministic ingestion workflow scheduling'
type: 'bugfix'
created: '2026-08-04'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'da5df10092461e5473d0e8fc09eacbb4a8e08d3a'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/dev/ingestion-workflow-determinism.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The determinism guard rejects mutable configuration reads and checks one URL schedule, but a new direct, activity-owned, or child `IngestionWorkflow` entry point can bypass the shared scheduler or durable retry/NL configuration without failing a test.

**Approach:** Make the Server scheduling topology a closed source inventory. Normal callers use `IIngestionWorkflowScheduler`; every direct or child exception is counted, classified, and tied to executable evidence that the input reaching the workflow carries the required captured state.

## Boundaries & Constraints

**Always:** Scan hand-written Server C# excluding `bin`/`obj`; recognize the documented multiline compile-time workflow-name forms and exact occurrences; require the shared scheduler or an explicit reviewed exception. Prove capture feeds each direct start. Claim-check new top-level/activity inputs that introduce inline bytes. The URL exception needs no claim-check because it schedules no bytes. The annotation child needs no second claim-check because its content is already durable parent input, but it must copy the parent's captured configuration unchanged. Prove live EventStore resolution and delegation use the shared scheduler. Preserve/exclude the approved `Hexalith.Builds`, `Hexalith.EventStore`, and `Hexalith.FrontComposer` pointer differences.

**Ask First:** Product/contract/package behavior changes, dependencies, a new direct/child exception, changes to annotation child completion/compensation semantics, or weaker matching/proof.

**Never:** Treat the package fallback as Server proof; read mutable host options in an orchestrator; replace enforcement with prose; require re-claim-checking the existing annotation child's already-durable content; touch submodules/unrelated source; claim dynamic, indirect constant/alias, reflection, wrapper, or inactive-preprocessor names are detected.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Normal top-level start | Full `IngestionInput` | Shared scheduler captures config/trace, claim-checks inline bytes, and schedules the prepared input | Existing tracking/cleanup behavior remains |
| Direct URL exception | URL input with `ContentBytes = null` | Assigned captured input is passed directly to Dapr | Inventory/proof fails if capture is discarded |
| Activity-owned exception | Inline annotation bytes | Existing config is preserved or captured as legacy fallback, then bytes are claim-checked before Dapr | Inventory/proof fails on order or data-flow drift |
| Durable annotation child | Content/config already on parent input | Child keeps observed-child semantics and copies parent config; no second claim-check | Existing compensation behavior remains |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs` -- retain mutable-config/trace guards; inventory exact Server starts and bind every reviewed exception to its input/capture proof; self-test supported matcher shapes and declared limitations.
- `src/Hexalith.Memories.Server/{Ingestion/DaprIngestionWorkflowScheduler,Endpoints/IngestionEndpoints,Activities/Cases/ScheduleAnnotationIngestionActivity,Workflows/AnnotationProjectionWorkflow,Cases/CaseService}.cs` -- read-only scheduling topology and proof anchors.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs` -- prove the actual Dapr call receives the captured, claim-checked input.
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` and `tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs` -- prove host capture reaches the durable parent and the identical config reaches its child.
- `src/Hexalith.Memories.Server/EventStoreIntegration/{ServerEventStoreIntegrationExtensions,EventIngestionWorkflowSchedulerAdapter}.cs`, `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`, and `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs` -- prove production registration resolves the adapter and delegates exact arguments through `IIngestionWorkflowScheduler`.
- `src/Hexalith.Memories.EventStore/DaprEventIngestionWorkflowScheduler.cs` -- read-only counterexample: package fallback lacks Server capture/claim-check.
- `docs/dev/ingestion-workflow-determinism.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- document the guarantee/limits and close only the matching Epic 23 action after green evidence.

## Tasks & Acceptance

**Execution:**
- [x] Generalize the architecture guard into an exact occurrence inventory with actionable diagnostics, supported-form self-tests, data-flow-specific exception proofs, and live host-composition proof.
- [x] Strengthen scheduler, annotation-parent/child, and EventStore adapter tests with behavioral assertions on the actual inputs and resolved implementation.
- [x] Update determinism guidance with the approved child claim-check boundary and honest static-matcher limitations.
- [x] Run fresh verification, then mark only the matching Epic 23 action done with exact results.

**Acceptance Criteria:**
- Given Server sources, when supported `IngestionWorkflow` starts are scanned, then the four reviewed sites occur exactly once and any added, removed, duplicated, relocated, or altered site fails actionably.
- Given the shared/direct/activity paths, when scheduling executes or its bound proof is inspected, then the same captured/prepared input reaches Dapr and inline bytes are claim-checked where required.
- Given annotation creation, when the durable parent and child are scheduled, then captured retry/NL config is preserved unchanged while observed-child compensation semantics remain unchanged.
- Given Server EventStore composition, when the scheduler contract resolves and is invoked, then it resolves the Server adapter and delegates exact arguments to `IIngestionWorkflowScheduler`.
- Given fresh focused evidence, when the ledger changes, then all selected tests have zero failures/skips and only the matching action becomes done.

## Spec Change Log

- 2026-08-05: Implemented the approved four-site inventory, bound capture/claim-check and host-composition proofs, behavioral scheduling coverage, deterministic guidance, and the matching Epic 23 ledger closure.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- expected: zero warnings/errors.
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -m:1 /nodeReuse:false --no-restore` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests -class Hexalith.Memories.Server.Tests.Cases.CaseServiceTests -class Hexalith.Memories.Server.Tests.Workflows.AnnotationProjectionWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionWorkflowSchedulerAdapterTests -noLogo` -- expected: zero errors/failures/skips/not-run.
- `dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -parallel none -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests -noLogo` -- expected: zero errors/failures/skips/not-run.
- `git diff --check -- tests/Hexalith.Memories.Server.Tests docs/dev/ingestion-workflow-determinism.md _bmad-output/implementation-artifacts/sprint-status.yaml && git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-preserve-deterministic-workflow-scheduling.md` -- expected: no whitespace errors; approved submodule differences remain excluded.
- `git diff --no-index --check /dev/null tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs` -- expected: no whitespace errors for the untracked adapter test.

**Results (2026-08-05):** Server build succeeded with 0 warnings/errors; Contracts build succeeded with 0 warnings/errors; the focused Server lane passed 131 tests with 0 errors/failures/skips/not-run; the Contracts serialization lane passed 11 tests with 0 errors/failures/skips/not-run. Whitespace checks emitted no diagnostics; the untracked-file `--no-index` checks returned Git's expected difference status `1` because each file differs from `/dev/null`.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-preserve-deterministic-workflow-scheduling.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/ingestion-workflow-determinism.md`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs`

Read/verify only:

- `src/Hexalith.Memories.Server/**`
- `src/Hexalith.Memories.EventStore/DaprEventIngestionWorkflowScheduler.cs`

## Suggested Review Order

**Closed scheduling topology**

- Start here: exact path-and-line inventory makes every supported exception explicit.
  [`IngestionWorkflowDeterminismGuardTests.cs:16`](../../tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs#L16)

- Bound proofs attach capture and claim-check preparation to the scheduled input.
  [`IngestionWorkflowDeterminismGuardTests.cs:54`](../../tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs#L54)

**Executable handoffs**

- Shared scheduler test observes the prepared input at the final Dapr boundary.
  [`DaprIngestionWorkflowSchedulerTests.cs:48`](../../tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs#L48)

- Production composition resolves the EventStore adapter and verifies exact delegation.
  [`EventIngestionWorkflowSchedulerAdapterTests.cs:23`](../../tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs#L23)

- Annotation parent test proves host configuration and trace capture before scheduling.
  [`CaseServiceTests.cs:136`](../../tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs#L136)

- Child test preserves captured state and the approved durable-content boundary.
  [`AnnotationProjectionWorkflowTests.cs:54`](../../tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs#L54)

**Guidance and closure**

- Maintainer guidance defines exceptions, enforcement coverage, and honest matcher limits.
  [`ingestion-workflow-determinism.md:44`](../../docs/dev/ingestion-workflow-determinism.md#L44)

- Epic 23 closes against the final focused and serialization evidence.
  [`sprint-status.yaml:683`](sprint-status.yaml#L683)

