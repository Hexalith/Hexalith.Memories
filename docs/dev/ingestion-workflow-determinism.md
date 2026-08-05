# Ingestion Workflow Determinism

## Durable configuration contract

`IngestionWorkflow` consumes retry and natural-language decisions from the nullable
`IngestionInput.WorkflowConfiguration` field. `IngestionWorkflowConfigurationCapture`
copies the current host `Ingestion:RetryPolicies` values and
`NaturalLanguage:PersistInMetadata` into that durable contract at scheduling time.
Once scheduled, replay must use the serialized values; a host configuration reload or
server restart must not change an existing workflow's decisions.

`IIngestionWorkflowScheduler` is the normal top-level scheduling seam. Its Dapr
implementation applies configuration and trace capture, then runs the payload
claim-check, tracks the in-flight instance, and finally calls
`ScheduleNewWorkflowAsync`. `IngestionPayloadClaimCheck` returns a `with` copy that
sets `ContentBytes = null` and adds `PayloadReference`; the already captured
`WorkflowConfiguration` and all other durable fields are preserved.

Do not construct a slim `IngestionInput` and call Dapr directly from a new top-level
entry point. Pass the full input to `IIngestionWorkflowScheduler`. The current
directory path performs an earlier claim-check so it can clean up a payload if that
specific schedule is not accepted; the scheduler still captures configuration on the
resulting input and preserves the existing source reference before scheduling.

## Direct and child workflow entry points

Every path that starts `IngestionWorkflow` has one of these obligations:

- **Normal top-level path:** call `IIngestionWorkflowScheduler.ScheduleAsync`; it
  freshly captures current configuration before its claim-check and Dapr scheduling.
- **Necessary direct Dapr path:** explicitly apply
  `IngestionWorkflowConfigurationCapture` before `ScheduleNewWorkflowAsync`, as the
  URL endpoint does. Also capture trace context at the non-orchestrator boundary and
  run claim-check first when the input has inline bytes.
- **Child workflow path:** capture configuration before the parent workflow is
  scheduled, carry it on the parent's durable input, and copy the same value to every
  `IngestionInput` child. `AnnotationProjectionWorkflow` follows this rule. Never read
  host options from the parent orchestrator to build a child.
- **Activity-owned scheduling path:** preserve configuration already supplied by the
  durable parent input. An activity may capture a fallback only when a legacy input
  contains no configuration because activity execution is an I/O boundary, not
  replayed orchestration.

The Server inventory is intentionally closed to these four reviewed starts:

| Start owner | Classification | Required data-flow proof |
|-------------|----------------|--------------------------|
| `DaprIngestionWorkflowScheduler` | Shared top-level scheduler | Capture configuration and trace, then claim-check, then pass the prepared input to Dapr |
| URL endpoint in `IngestionEndpoints` | Direct URL exception | Assign captured configuration/trace to the same byte-free input passed to Dapr |
| `ScheduleAnnotationIngestionActivity` | Activity-owned exception | Preserve parent configuration (or capture the legacy fallback), claim-check inline bytes, then pass the prepared input to Dapr |
| `AnnotationProjectionWorkflow` | Observed child exception | Copy the parent's captured configuration unchanged and preserve the existing child failure/compensation semantics |

The annotation child is deliberately not claim-checked a second time. Its content is
already part of the durable `AnnotationProjectionInput`; the child construction copies
that durable content and the identical captured configuration. By contrast, the legacy
activity-owned annotation path creates new inline bytes at an activity boundary, so it
must claim-check those bytes before its direct Dapr call.

Server EventStore composition must resolve
`IEventIngestionWorkflowScheduler` as `EventIngestionWorkflowSchedulerAdapter`. The
adapter delegates the exact instance ID, full input, and cancellation token through
`IIngestionWorkflowScheduler`, which owns Server capture and claim-check behavior. The
package-owned direct Dapr fallback is not evidence for Server composition because it
does not perform those Server-owned preparations.

Changing an entry point includes reviewing every retry, re-ingestion, bulk, directory,
event, annotation, command/projection, and adapter path that can reach it. A path that
preserves `PayloadReference` but drops `WorkflowConfiguration` is incomplete.

## Orchestrator and activity boundary

Durable orchestrator code can replay from the beginning. Keep its control flow a pure
function of serialized input and workflow history:

- do not inject/read `IOptions`, option snapshots, process-global retry builders, or
  mutable static configuration;
- do not perform network, filesystem, actor, provider, Redis, Dapr state, or other I/O
  directly; call an activity;
- do not use `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, `Guid.NewGuid`, `Random`, or
  thread timing; use deterministic workflow-context time/ID/timer APIs;
- do not move provider-rate feedback, payload/state cleanup, index writes, secret
  resolution, or other side effects out of activities;
- use a replay-safe logger and serialized trace context; do not capture ambient
  `Activity.Current` or start spans from orchestration.

Activities may read current external state and options where the product contract
intends activity-time behavior. Any value that changes orchestration branches, retry
schedules, or durable child input must instead be captured before scheduling.

## Defaults and JSON compatibility

Legacy serialized `IngestionInput` values can omit `workflowConfiguration`. A null
configuration deterministically falls back to new contract defaults:

| Decision | Legacy/default value |
|----------|----------------------|
| Activity max attempts | `5` |
| First retry interval | `2` seconds |
| Backoff coefficient | `1.5` |
| Maximum retry interval | `300` seconds |
| Persist generated natural-language description in metadata | `false` |

Keep `IngestionInput`, `IngestionWorkflowConfiguration`,
`IngestionActivityRetryConfiguration`, `WorkflowActivityRetryPolicy`, and
`NaturalLanguageWorkflowOptions` registered in `MemoriesJsonContext`. New durable
nested contract types need source-generation registration and round-trip/legacy JSON
tests. Preserve camel-case wire names and default-initialized collections so missing
legacy properties do not introduce null or comparer drift.

## Required guards and their limits

Build the Server and Contracts test projects when their Debug test executables are
not current, then run:

```bash
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
  -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests \
  -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests \
  -class Hexalith.Memories.Server.Tests.Cases.CaseServiceTests \
  -class Hexalith.Memories.Server.Tests.Workflows.AnnotationProjectionWorkflowTests \
  -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests \
  -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionWorkflowSchedulerAdapterTests \
  -parallel none -noLogo

dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll \
  -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests \
  -parallel none -noLogo
```

The source guards reject the known mutable snapshot types and ambient trace capture,
inventory every supported Server start exactly once, and bind each reviewed exception
to its capture/claim-check data flow. The matcher recognizes a direct
`ScheduleNewWorkflowAsync` or `CallChildWorkflowAsync<T>` invocation when its first
argument is positional or named `workflowName:` and uses an unqualified or qualified
compile-time `nameof(...IngestionWorkflow)` form, or the exact `"IngestionWorkflow"`
literal. Whitespace, multiline variants, and child calls with a balanced one-level
nested generic result type are supported. API identifiers and workflow-name expressions
must end at their real token boundaries. The scanner lexically masks actual comments,
character literals, and non-workflow string text so invocation-shaped prose is not
inventoried and comment markers inside strings cannot hide a following real start;
executable interpolation holes remain visible to the inventory.

This remains a targeted textual guard, not a C# semantic analyzer. It does not resolve
dynamic or indirect workflow names, constants/aliases, reflection, or wrapper methods,
and conditional-compilation blocks are deliberately ignored rather than evaluated.
It also does not recognize every new static, clock, random source, I/O API, or generic
shape deeper than the documented balanced matcher. Code review must apply the boundary
rules above and extend the guard whenever a new path or mutable dependency is
introduced; weakening the matcher or adding a direct/child exception requires explicit
review.

## New entry-point review checklist

- [ ] Uses `IIngestionWorkflowScheduler`, or documents why a direct/child path is
  necessary.
- [ ] Captures configuration outside orchestration and before Dapr scheduling.
- [ ] Preserves configuration, trace context, and source reference through `with`
  copies, payload slimming, parent inputs, and child inputs.
- [ ] Adds source-generated JSON metadata plus current and legacy round-trip coverage
  for any new durable fields/types.
- [ ] Keeps all I/O, provider, actor, state, secret, and cleanup work in activities.
- [ ] Uses workflow-context time, IDs, and timers and replay-safe logging.
- [ ] Extends scheduling/determinism guards if the new shape is not already inspected.
- [ ] Runs the required guard and serialization lanes above.

## Authoritative sources

- [`DaprIngestionWorkflowScheduler`](../../src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs)
- [`IngestionWorkflowConfigurationCapture`](../../src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowConfigurationCapture.cs)
- [`IngestionWorkflow`](../../src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs)
- [`MemoriesJsonContext`](../../src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs)
