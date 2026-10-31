---
status: in-progress
date: '2026-10-31'
baseline_commit: 00919bdd6fe97284da5f4f1657da4a5b336867cc
---

# Diagnose the owned workflow readiness boundary

## Intent

Latest dependency-only source00919bdd passes release, build, unit, SQL, web and
deployment checks. CI41215639592 fails one real integration case before its
embedding fault plan or provisioning POST: the owned workflow readiness predicate
never converges within the existing five-minute deadline. Successful metadata
reads leave only `Last error: n/a`, so the failed predicate is not observable.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/WorkflowWorkerReadinessTests.cs`
- `_bmad-output/implementation-artifacts/spec-diagnose-workflow-readiness.md`

## Change

Retain a bounded final readiness summary containing only worker count, host-ready
state and two expected actor-registration booleans. Never serialize arbitrary
metadata, actor names, application identity, errors or secret fields into it.
Preserve exact app identity, worker, placement and actor predicates, all deadlines,
read-only polling, caller cancellation and non-transient refusal behavior.

Preserve all nine upstream dependency gitlinks, production sources and published
3.0.13 artifacts. Run focused readiness controls and the exact failing real case;
use resulting evidence to distinguish a fixture defect from runtime startup failure.
Do not weaken assertions or blindly rerun the failed CI job.

## Existing evidence

Failed app `memories-it-b631c981bb14449c9acb8a8158dedc11` starts its sidecar at
15:49:10UTC. The first workflow stream rejects an actor backend that is not yet
initialized; a second stream appears before registered actors and placement
dissemination complete at15:49:13.355. The worker had an earlier HTTP/2 keepalive
timeout and reconnect. Current logs do not identify the missing readiness field.
Raw exact log: `/tmp/memories-009-integration-failure.log`.

The pinned [Dapr1.18.2 metadata implementation](https://raw.githubusercontent.com/dapr/dapr/v1.18.2/pkg/api/universal/metadata.go)
and [actor runtime status](https://raw.githubusercontent.com/dapr/dapr/v1.18.2/pkg/actors/actors.go)
confirm the existing metadata/readiness fields. The registered actor table includes
types with zero instances; that condition must remain enforced.

## Focused proof

The unchanged predicate controls pass13/13 with0skips in1.090seconds. A fresh
Release package-mode IntegrationTests build against the owner-default published
SDK3.129.0 passes with0warnings/errors in19.09seconds. The exact failed real
provider-retry method passes1/1 with0skips in24.184seconds using isolated
placement/scheduler1.18.2 and the existing installed sidecar1.18.7. This is useful
functional evidence but is not an exact CI-runtime1.18.2 qualification: Aspire
prefers its standard installed CLI path before PATH. The exact CI-runtime probe
uses only an explicit DaprPath option in a disposable workspace, verified official
CLI/runtime archives and separate placement/scheduler containers. Its result will
be recorded separately; no probe option belongs in this patch.

Logs: `/tmp/memories-workflow-readiness-build.log`,
`/tmp/memories-workflow-readiness-controls.log`,
`/tmp/memories-workflow-readiness-real-case-2.log`.
Code diff: `/tmp/memories-workflow-readiness-code.diff`.
The original full CI still owns the runtime failure; this patch makes a future
failure actionable and does not claim an unobserved runtime root cause is fixed.

## File List

- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/WorkflowWorkerReadinessTests.cs`
- `_bmad-output/implementation-artifacts/spec-diagnose-workflow-readiness.md`
