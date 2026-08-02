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
  -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests \
  -parallel none -noLogo

dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll \
  -class Hexalith.Memories.Contracts.Tests.V1.IngestionInputSerializationTests \
  -parallel none -noLogo
```

The source guards reject the known mutable snapshot types, ambient trace capture, and
direct URL capture-order drift. Scheduler/workflow tests verify captured retry behavior
and scheduling. They are targeted guards, not a general determinism analyzer: they do
not recognize every new static, clock, random source, I/O API, or newly invented entry
point. Code review must apply the boundary rules above and add/extend a guard whenever
a new path or mutable dependency is introduced.

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
