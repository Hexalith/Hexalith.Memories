---
baseline_commit: 34e0f95b76cd4217c7f7db0bd6b9a1b3d149dff3
---

# Story 23.8: Workflow Config Determinism

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want workflow orchestration to read config from its input, not mutable statics,
so that a config change mid-flight cannot break replay determinism.

## Acceptance Criteria

1. Workflow activity retry policies are captured into `IngestionInput` at scheduling time and serialized with the workflow instance; `IngestionWorkflow.RunAsync` must not read `RetryPolicyBuilder.SnapshotAll()`, `RetryPolicyBuilder.For(...)`, or any other mutable process-global retry state from orchestration code.
2. Natural-language metadata persistence policy is captured into the workflow input at scheduling time; `IngestionWorkflow.RunAsync` must not read `NaturalLanguageDescriptionOptionsSnapshot.Value` or any mutable process-global natural-language options from orchestration code.
3. All live ingestion scheduling paths populate the captured workflow config before `ScheduleNewWorkflowAsync` starts the ingestion workflow, including the `IIngestionWorkflowScheduler` path used by REST file ingest, directory ingest, re-ingestion, and EventStore integration, plus the direct URL ingest path in `Program.cs`.
4. `AnnotationProjectionWorkflow` remains replay-safe when it schedules `IngestionWorkflow` as a child workflow. It must not capture current host configuration during replay; instead, config must already be present on `AnnotationProjectionInput` or produced by an activity before the child workflow is scheduled.
5. Existing ingestion behavior is preserved: activity retry defaults and overrides still produce the same `WorkflowTaskOptions`, claim-check slimming still removes large payload bytes before scheduling, metadata comparer semantics remain ordinal, and NL metadata is still only emitted when the captured flag says to persist it and `EstimatedConfidence` is present.
6. Contract additions are additive and source-generation-aware: new workflow-config records or properties use stable camelCase JSON, defaults preserve existing clients/tests, and `MemoriesJsonContext` includes any new serializable types needed for AOT/source-generated serialization.
7. Replay-determinism tests prove an in-flight workflow keeps the captured retry/NL values even after process-global settings are changed before replay/execution continues.
8. Source guards or focused unit tests fail if `IngestionWorkflow` reintroduces reads from `RetryPolicyBuilder` or `NaturalLanguageDescriptionOptionsSnapshot` inside orchestration logic.

## Tasks / Subtasks

- [x] Define the captured workflow configuration contract (AC: 1, 2, 5, 6)
  - [x] Add serializable contract shape(s) under `src/Hexalith.Memories.Contracts/V1/` for workflow retry policy capture and NL workflow options. Prefer primitives and existing `ActivityRetryPolicy`-like values over `Dapr.Workflow.WorkflowTaskOptions`, because the workflow input is durable JSON contract data.
  - [x] Add an optional/defaulted property to `IngestionInput` so legacy callers that do not set it still get current default behavior.
  - [x] Update `MemoriesJsonContext` for any new nested/standalone records required by source generation.

- [x] Add a deterministic capture service at the scheduling boundary (AC: 1, 2, 3, 5)
  - [x] Extend `DaprIngestionWorkflowScheduler` or a small injected helper it owns to capture `IOptions<IngestionSettings>.Value.RetryPolicies` and `IOptions<NaturalLanguageDescriptionOptions>.Value.PersistInMetadata` before `IngestionPayloadClaimCheck.PrepareAsync(...)`.
  - [x] Preserve the existing claim-check behavior: the scheduled/slimmed input must retain the captured workflow config while replacing large inline payload bytes with `PayloadReference`.
  - [x] Keep `RetryPolicyBuilder` available only for non-workflow callers/tests during migration, or narrow it to conversion utilities if that is the cleanest local design.

- [x] Remove mutable static reads from `IngestionWorkflow` (AC: 1, 2, 5, 8)
  - [x] Replace the top-of-run `RetryPolicyBuilder.SnapshotAll()` lookup with a local map built from `input`'s captured retry config.
  - [x] Replace `NaturalLanguageDescriptionOptionsSnapshot.Value.PersistInMetadata` with the captured value from `input`.
  - [x] Keep existing retry semantics intact: default policy remains max attempts 5, first retry 2 seconds, backoff 1.5, max interval 5 minutes; activity overrides still apply by activity class name.

- [x] Cover all ingestion scheduling paths (AC: 3, 4, 5)
  - [x] Verify the REST file ingest endpoint, directory ingestion service, re-ingestion coordinator, and EventStore ingestion adapter flow through `IIngestionWorkflowScheduler` and receive captured config without duplicating capture code.
  - [x] Update the direct URL ingest path in `Program.cs` because it currently calls `workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input: input)` directly.
  - [x] Handle `AnnotationProjectionWorkflow` carefully: it is itself a workflow, so it cannot read host options directly. Capture config into `AnnotationProjectionInput` at its scheduling boundary, or route the child-ingestion scheduling through an activity that receives already-captured config and does side effects outside orchestration.
  - [x] Leave `ScheduleAnnotationIngestionActivity` consistent if it remains registered, but do not treat it as the live path unless code proves it is invoked.

- [x] Update tests and guards (AC: 1-8)
  - [x] Add workflow tests that initialize one retry/NL config, create an input with captured values, mutate/reset the process-global snapshots, then run/replay `IngestionWorkflow` and assert activity retry options and NL metadata use the captured input values.
  - [x] Update `RunAsync_PerActivityOverride_ShouldUseOverrideForEmbeddingAndDefaultForExtraction` and dual-embedding metadata tests to use captured input config instead of process-global setup.
  - [x] Add scheduler tests proving captured config is added before scheduling and survives claim-check slimming.
  - [x] Add source guard coverage for `IngestionWorkflow.cs` that rejects `RetryPolicyBuilder` and `NaturalLanguageDescriptionOptionsSnapshot` references in the workflow orchestration body.

- [x] Validate (AC: 1-8)
  - [x] `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore`
  - [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore`
  - [x] Focused xUnit v3 in-process fallback for workflow/scheduler/retry/NL tests if `dotnet test` is blocked by the known VSTest socket issue.
  - [x] `git diff --check`

## Dev Notes

- This story closes A35 from the 2026-07-04 architecture audit: `IngestionWorkflow` reads mutable process-global config statics, creating a replay-determinism hazard if configuration changes while an instance is in flight. [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md:66`]
- The Epic 23 story text requires retry-policy/NL options to be captured into workflow input at scheduling time, with no mutable static reads in orchestrator code and a replay-determinism test. [Source: `_bmad-output/planning-artifacts/epics.md:4310`]
- Current static reads are in `IngestionWorkflow.RunAsync`: `RetryPolicyBuilder.SnapshotAll()` at the top of the workflow and `NaturalLanguageDescriptionOptionsSnapshot.Value.PersistInMetadata` before adding `event.naturalLanguageDescription` metadata. [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:43`] [Source: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:287`]
- `RetryPolicyBuilder` is currently a process-global table initialized from `IngestionSettings` and exposes immutable snapshots plus `For(...)`; its existing comments explicitly rely on convention for workflow replay determinism, which this story replaces with durable input capture. [Source: `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs:10`]
- `NaturalLanguageDescriptionOptionsSnapshot` exists only because workflows are activated with `new()` and cannot receive constructor dependencies. Do not add DI directly to `IngestionWorkflow`; pass the needed value through durable input instead. [Source: `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsSnapshot.cs:10`]
- `Program.cs` currently initializes both process-global snapshots after `builder.Build()`. That startup binding can still feed the scheduler/capture service, but `IngestionWorkflow` itself must not read those statics. [Source: `src/Hexalith.Memories.Server/Program.cs:442`]
- `DaprIngestionWorkflowScheduler.ScheduleAsync` is the main scheduling choke point and already claim-checks/slims `IngestionInput` before calling `ScheduleNewWorkflowAsync`. This is the preferred place to add capture for most ingestion paths, while preserving payload-store behavior. [Source: `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs:27`]
- `IngestionInput` is the durable workflow contract and currently carries tenant/case/source/payload/metadata/idempotency fields. Add config as an additive defaulted property; do not rename or remove existing fields. [Source: `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:8`]
- `MemoriesJsonContext` source-generates JSON metadata and already lists `IngestionInput`. Add new workflow-config types there if they are separate/nested records to keep AOT/source-generated serialization intact. [Source: `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`]
- Direct URL ingestion still bypasses `IIngestionWorkflowScheduler` and calls `ScheduleNewWorkflowAsync` directly from `Program.cs`; this path needs explicit capture or should be routed through the same scheduler seam. [Source: `src/Hexalith.Memories.Server/Program.cs:756`]
- `AnnotationProjectionWorkflow` schedules `IngestionWorkflow` as a child workflow from workflow orchestration. Capturing live host config in this workflow would repeat the A35 problem. The child input must be built from already-captured data on `AnnotationProjectionInput` or from an activity boundary. [Source: `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs:32`]
- `ScheduleAnnotationIngestionActivity` builds an annotation `IngestionInput` and schedules a workflow, but current code search indicates the live annotation path is the child-workflow call in `AnnotationProjectionWorkflow`; keep the registered activity consistent if touched. [Source: `src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs:26`]
- Existing test patterns use xUnit v3, Shouldly, NSubstitute, and the `RetryPolicyBuilderStateCollection` to isolate global retry state. Story 23.8 tests should reduce reliance on global state by putting captured config on the test `IngestionInput`. [Source: `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs:1475`]
- Dual-embedding tests currently flip `NaturalLanguageDescriptionOptionsSnapshot` to prove `PersistInMetadata`; convert that evidence to captured-input behavior so replay does not depend on later static changes. [Source: `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowDualEmbeddingTests.cs:35`]
- Preserve project rules: .NET 10/C# 14, central package management, ITANEO headers on hand-written `.cs`, warnings as errors, xUnit v3 + Shouldly + NSubstitute, no recursive submodule updates, and workflow orchestration must remain replay-safe. [Source: `_bmad-output/project-context.md`]

### Project Structure Notes

- Contract changes belong in `src/Hexalith.Memories.Contracts/V1/`.
- Scheduler/capture code belongs in `src/Hexalith.Memories.Server/Ingestion/` near `DaprIngestionWorkflowScheduler`, unless a smaller helper type is clearer.
- Workflow changes belong in `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` and, for annotation child scheduling, `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs` or an activity boundary.
- Tests should mirror product areas: workflow tests in `tests/Hexalith.Memories.Server.Tests/Workflows/`, scheduler/capture tests in `tests/Hexalith.Memories.Server.Tests/Ingestion/`, and source guards in an existing architecture/governance-style test folder if one is used.
- No UI/UX scope.

### References

- `_bmad-output/planning-artifacts/epics.md:4310` - Story 23.8 source requirement.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md:120` - sprint-change summary for A35.
- `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md:66` - A35 audit finding.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:43` - current retry static read.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:287` - current NL options static read.
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs:27` - main scheduling seam.
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs:8` - durable workflow input contract.
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs:32` - child workflow scheduling path.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (direct fallback after story-automator child create attempts stalled)

### Debug Log References

- Story automator create attempt 1 (`codex`) crashed before artifact creation.
- Story automator create attempt 2 (`claude`) stalled after context discovery without writing the artifact.
- Story automator create attempt 3 (`codex`) repeated broad discovery without writing the artifact.
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings/errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` passed with 0 warnings/errors.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-build --filter ...` was blocked by the known VSTest socket permission issue.
- Focused xUnit v3 in-process fallback passed: 66 total, 0 failed, 0 skipped.
- Full server test assembly xUnit v3 in-process fallback passed: 2410 total, 0 failed, 1 skipped.
- Test automation added API/E2E coverage for `POST /api/ingest` scheduling through `IIngestionWorkflowScheduler`.
- Automation validation fallback passed: endpoint/claim-check xUnit classes 5 total, 0 failed, 0 skipped.
- Automation validation fallback passed: workflow/source-guard xUnit classes 63 total, 0 failed, 0 skipped.
- `git diff --check` passed.

### Completion Notes List

- Story created by direct fallback because automated child create sessions failed before producing source-of-truth artifacts.
- Added durable workflow configuration contracts for captured ingestion retry policy and natural-language metadata persistence options.
- Added scheduling-boundary capture through `DaprIngestionWorkflowScheduler`, the direct URL endpoint, and annotation projection scheduling while preserving claim-check slimming.
- Routed the REST file ingest endpoint through `IIngestionWorkflowScheduler` and added API-level regression coverage for valid/invalid file payload scheduling behavior.
- Removed mutable retry/NL static reads from `IngestionWorkflow`; orchestration now uses only captured input values with legacy default fallback behavior.
- Added replay-determinism, scheduler capture, annotation child config propagation, and source guard coverage.

### File List

- `_bmad-output/implementation-artifacts/23-8-workflow-config-determinism.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Memories.Contracts/V1/IngestionActivityRetryConfiguration.cs`
- `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`
- `src/Hexalith.Memories.Contracts/V1/IngestionWorkflowConfiguration.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Contracts/V1/NaturalLanguageWorkflowOptions.cs`
- `src/Hexalith.Memories.Contracts/V1/WorkflowActivityRetryPolicy.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/AnnotationProjectionInput.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/ScheduleAnnotationIngestionActivity.cs`
- `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- `src/Hexalith.Memories.Server/Ingestion/DaprIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowConfigurationCapture.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Workflows/AnnotationProjectionWorkflow.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionPayloadClaimCheckTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowDualEmbeddingTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex

Review Date: 2026-07-05

Outcome: Approved. No critical, high, or medium implementation findings remained after the direct review fallback.

Review Notes:
- Verified acceptance criteria against the implementation: captured retry/NL config is part of durable ingestion input, `IngestionWorkflow` no longer reads mutable retry/NL statics, scheduler and direct URL paths capture before scheduling, annotation child scheduling propagates captured config, and source-generated JSON context includes the new contract types.
- Verified changed source/test file list against git status, including untracked contract, capture, source-guard, and endpoint test files.
- Verified tests are concrete assertions rather than placeholders: replay-determinism tests cover retry/NL static changes, scheduler tests cover capture before claim-check slimming, annotation tests cover child config propagation, and endpoint tests cover REST file ingest scheduling through the deterministic scheduler seam.
- The first child review session exited after discovery without updating source-of-truth files, so the same review skill was completed directly in the orchestrator context.

### Change Log

- 2026-07-05: Implemented workflow config determinism for Story 23.8; captured retry/NL config in durable inputs, removed ingestion workflow static config reads, updated scheduling paths, and added regression/source-guard coverage.
- 2026-07-05: Senior developer review completed; story and sprint status moved to done.
