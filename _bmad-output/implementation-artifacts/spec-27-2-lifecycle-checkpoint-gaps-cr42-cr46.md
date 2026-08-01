---
title: 'Story 27.2 lifecycle checkpoint gaps CR42-CR46'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
baseline_commit: '74e527f76c1fd859168d3f61bf1f4b28bcad837c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five Story 27.2 portable lifecycle checkpoint methods overclaim purge/write concurrency, measured 250-events/s admission, independent overlapping writers, a measured sixty-second outage/retry schedule, and structure-aware least-privilege enforcement. These gaps are registered as `DW 27.3-CR42` through `DW 27.3-CR46` and keep Story 27.3 checkpoint C0 open.

**Approach:** Strengthen the existing portable checkpoint with deterministic test-only coordination, trusted fake-time measurement, independent writer contexts, the real delivery-worker scheduling loop, and YAML node-tree assertions. Execute the fresh canonical eight-method lane, route evidence to Story 27.3, and defer C0 closure to an independent review.

## Boundaries & Constraints

**Always:** Keep implementation ownership with Story 27.2 and evidence receipt with Story 27.3; use fresh Release binaries; force rather than infer overlap; measure rates and outage timing against trusted time; assert exact YAML identities, operations, verbs, actions, scopes, and negative cases; preserve one C# type per file; record evidence only after commands execute successfully.

**Ask First:** Any production-code change, new package or package-version change, literal external deployment/process requirement, changed public contract, or scope beyond CR42-CR46.

**Never:** Enable Production lifecycle writes; advance Story 27.4; mutate or close A41; use sleeps, timing tolerances, substring YAML assertions, skipped/zero-test evidence, or mark C0 complete before an independent reviewer reruns and accepts the reviewed source.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Purge/write overlap | One due record, 500 live writes, coordinated store calls | Purge and writes overlap; due record is removed; 500 record/index pairs remain; all commits are atomic | Missing rendezvous fails the test |
| Rate-bound admission | 500 attempts paced over exactly two trusted-time seconds | Exactly 250/s attempted; first 250 retained within byte bound; newest 250 rejected as `QueueFull` | Timer coalescing or wrong rate fails closed |
| Independent writers | Two clocks, generators, queues/process-boundary clients, and overlapping writes | IDs are unique; tenant markers remain isolated; canonical storage contains no raw fields; both writes persist atomically | Any collision, marker mix-up, raw leak, or absent overlap fails |
| Timed outage | Dependency unavailable through trusted time `< T+60s` | Real worker retries while retaining work, succeeds at `T+60s`, and still drops work at the five-minute age cap | Early success, absent retry, late recovery, or stale work retention fails |
| Least privilege | Lifecycle Configuration and state Component YAML | Exact two-policy grant set and exact lifecycle-only component scope pass independently of formatting | Wildcards, duplicates, extra identities/grants/verbs/actions/scopes, or malformed YAML fail |
| Evidence routing | Fresh build, discovery, focused and canonical execution | CR42-CR46 receive exact command/result evidence in Story 27.3/deferred work; C0 remains open for review | Any failed, skipped, stale, or unreviewed result cannot close C0 |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs` -- eight-method C0 portable lane and the five overclaimed checkpoint behaviors.
- `tests/Hexalith.Memories.IntegrationTests/Telemetry/` -- one-type-per-file coordination, time, delivery, and YAML test helpers.
- `src/Hexalith.Memories.AccessTelemetry/Lifecycle/IAccessTelemetryStateStore.cs` -- existing decorator seam for deterministic concurrent operations.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryWorker.cs` -- real retry scheduler exercised without production modification.
- `deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml` and `deploy/kubernetes/base/dapr/access-telemetry-store.yaml` -- authoritative least-privilege inputs.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- CR42-CR46 status and executed discharge evidence.
- `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` -- C0 evidence recipient and phase ledger; stays open until review.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.IntegrationTests/Telemetry/` -- add test-only coordination and trusted-time helpers, then strengthen all five named checkpoint methods to cover every matrix row without production changes.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- resolve CR42-CR46 only from fresh executed evidence, with exact commands/results and reopen conditions preserved.
- [x] `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` -- append runner-derived discovery/execution and File List evidence as recipient while leaving C0 blocked for independent review.

**Acceptance Criteria:**
- Given a fresh Release IntegrationTests assembly, when each strengthened method and the canonical class selector run, then exactly eight methods are discovered and all eight pass with no failures, errors, skips, or not-run cases.
- Given the resulting evidence records, when implementation hands off to review, then Story 27.2 remains `done`, Story 27.3 remains `in-progress`, C0 is not complete, Story 27.4 remains `backlog`, Production writes remain disabled, and A41 remains open.

## Spec Change Log

- 2026-08-01: Implemented the five test-only lifecycle checkpoint closures in ten C# files, preserved one type per file, and made no production, package, version, public-contract, or deployment-process change. Fresh Release build passed with zero warnings/errors; exact discovery returned eight unique methods; the canonical class selector passed 8/8; each CR42-CR46 focused selector passed 1/1. Routed exact evidence to deferred work and Story 27.3. Status moved to `in-review`; C0 remains open pending independent review and reviewer-owned fresh execution.
- 2026-08-01: Applied all classified patches from the first independent review while preserving test-only scope and the KEEP constraints. Moved CR42/CR44 coordination to the state-store seams, made CR43 an acknowledged `PeriodicTimer` producer with independently binding byte capacity, made CR45 use and observe exact validator-legal five-second timers, and expanded CR46 additive/removal/API/root/spec mutations. Added the clean-before-build contract and a workflow-format defer for the normal readiness gate's inability to require executed C0 receipt evidence. Clean, fresh build, discovery, canonical, and focused selectors are green. Status remains `in-review`; the patch-producing review is not acceptance, so C0 remains open for a new independent review and fresh reviewer execution.
- 2026-08-01: Applied the substantive patches from the second independent review while preserving test-only scope and the KEEP constraints. CR44 now proves same-key consistency across writers in addition to tenant/user isolation; CR45 validates one fully populated write-enabled Development configuration, fences every attempt at the next worker timer, records attempted batches, and proves the exact `4:59.999` versus `5:00.000` boundary; CR46 now accepts reordered/reserialized equivalent YAML and rejects exact configuration, access-control, policy, component, auth, duplicate, missing, and non-mapping-root mutations, including the `1m` init timeout. Generic rendezvous cancellation and fail-first-store concurrency findings were rejected as outside the scoped lifecycle contracts. Post-clean build, discovery, canonical, and focused selectors are green. Status remains `in-review`; C0 remains open pending targeted independent acceptance and reviewer-owned fresh execution.
- 2026-08-01: Applied the targeted final-review rejection remediation while preserving test-only scope and the KEEP constraints. Reopened CR42 because the outer coordinator did not keep the already-completed inner due-read operation active. Added one type-only helper that is the actual inner store called by the coordinator; its due-read and write methods each record entry and await the counterpart before either can delegate or complete, and it records overlap only while neither participating operation has completed. Preserved the exact durability assertions, corrected `AccessTelemetryOperationRendezvous` attribution to CR44 only, and left the generic rendezvous and fail-first helper unchanged. Clean, fresh build, discovery, canonical, and all focused selectors are green. Status remains `in-review`; CR42 is resolved on implementation evidence, while C0 remains open pending independent re-acceptance and reviewer-owned fresh execution.
- 2026-08-01: Independent re-review accepted CR42-CR46 and C0 after source inspection and a reviewer-owned fresh clean lane. The reviewer observed a zero-warning/zero-error Release build, exactly eight discovered methods, CR42-CR46 at 1/1 each, the canonical selector at 8/8, exact 14-path readiness, and clean diff hygiene. Story 27.3 received and closed C0 on that evidence while remaining `in-progress`; Story 27.4 remains `backlog`, Production writes remain disabled, and A41 remains open. Status moved to `done`.

## Design Notes

Use test decorators around existing state-store and delivery-client seams. Coordination must expose an observed rendezvous, not merely start two tasks. For CR42, the gate is the actual inner store invoked by the capture decorator: both operation methods must enter before either may delegate or complete. Writer isolation is proven through separate clocks, generators, queues, and client calls crossing the delivery seam; do not describe same-process tasks as literal OS-process proof. Parse YAML into node trees and compare normalized exact grant/scope sets.

## Verification

**Commands:**
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet clean tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --verbosity minimal` -- expected: remove the prior Release output and exit zero before the evidence build.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: fresh Release build, zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests -noLogo` -- expected: exactly eight unique methods.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests -parallel none -noLogo` -- expected: 8 passed, 0 failed/errors/skipped/not-run.
- `git diff --check` -- expected: no whitespace errors.

**Observed results (2026-08-01):**

- Release build: exit `0`; `Build succeeded`; `0 Warning(s)`; `0 Error(s)`.
- Exact class discovery: exit `0`; exactly eight unique method lines.
- Canonical class execution: exit `0`; `Total: 8, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.430s`).
- CR42-CR46 focused executions: five exit-`0` selectors, each `Total: 1` with zero errors, failures, skips, or not-run cases; literal commands and timings are recorded in `deferred-work.md`.
- Independent review: pending by design; this specification and Story 27.3 C0 remain fail-closed until the reviewer accepts the changed source and fresh rerun.

**Observed results after first-review remediation (2026-08-01):**

- Exact clean command: exit `0` before the evidence build.
- Fresh Release build with the command above plus `--verbosity minimal`: exit `0`; `Build succeeded`; `0 Warning(s)`; `0 Error(s)`; `Time Elapsed 00:00:46.60`.
- Exact class discovery: exit `0`; exactly eight unique method lines.
- Canonical class execution: exit `0`; `Total: 8, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.402s`).
- Corrected focused execution: CR42 `1/1` (`0.179s`), CR43 `1/1` (`0.199s`), CR44 `1/1` (`0.159s`), CR45 `1/1` (`0.158s`), and CR46 `1/1` (`0.118s`); every selector exited `0` with zero errors, failures, skips, and not-run cases.
- First independent review: patches applied, not accepted as final evidence. C0 remains open pending a new independent source review and reviewer-owned fresh rerun.

**Observed results after second-review remediation (2026-08-01):**

- Exact clean command: exit `0` before the evidence build.
- Fresh Release build with the command above plus `--verbosity minimal`: exit `0`; `Build succeeded`; `0 Warning(s)`; `0 Error(s)`; `Time Elapsed 00:00:59.71`.
- Exact class discovery: exit `0`; exactly eight unique method lines.
- Canonical class execution: exit `0`; `Total: 8, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.405s`).
- Second-remediation focused execution: CR42 `1/1` (`0.214s`), CR43 `1/1` (`0.228s`), CR44 `1/1` (`0.207s`), CR45 `1/1` (`0.239s`), and CR46 `1/1` (`0.218s`); every selector exited `0` with zero errors, failures, skips, and not-run cases.
- Targeted independent acceptance: pending by design. This patch-producing review is not acceptance; C0 remains open pending a fresh reviewer-owned execution and acceptance decision.

**Observed results after targeted final-review rejection remediation (2026-08-01):**

- Exact clean command: exit `0` before the evidence build.
- Fresh Release build with the command above plus `--verbosity minimal`: exit `0`; `Build succeeded`; `0 Warning(s)`; `0 Error(s)`; `Time Elapsed 00:01:02.38`.
- Exact class discovery: exit `0`; exactly eight unique method lines.
- Canonical class execution: exit `0`; `Total: 8, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.410s`).
- Final-rejection-remediation focused execution: CR42 `1/1` (`0.227s`), CR43 `1/1` (`0.253s`), CR44 `1/1` (`0.223s`), CR45 `1/1` (`0.265s`), and CR46 `1/1` (`0.243s`); every selector exited `0` with zero errors, failures, skips, and not-run cases.
- CR42 is resolved on executed implementation evidence. Independent re-acceptance remains pending by design; this remediation is not acceptance, and C0 remains open pending a fresh reviewer-owned execution and acceptance decision.

**Independent acceptance and C0 closure (2026-08-01):**

- Reviewer source decision: `ACCEPT` for CR42-CR46 and C0. CR42's actual inner due-read and write both enter before either may delegate or complete; CR43-CR46 retain their previously accepted proofs; the 14-path inventory and CR44-only rendezvous attribution reconcile.
- Reviewer clean/build/discovery: clean exit `0`; fresh Release build exit `0`, `0 Warning(s)`, `0 Error(s)`, `64.77s`; discovery exit `0` with exactly eight methods.
- Reviewer focused execution: CR42 `1/1` (`0.406s`), CR43 `1/1` (`0.378s`), CR44 `1/1` (`0.352s`), CR45 `1/1` (`0.354s`), and CR46 `1/1` (`0.252s`); every error/failure/skip/not-run count is zero.
- Reviewer canonical/readiness/hygiene: canonical `8/8` (`0.556s`) with every non-pass count zero; exact 14-path readiness passed with `C1: all 14 changed paths are declared` and `Story review readiness validation passed`; diff check excluding `references/` exited `0` with only line-ending notices.

## Suggested Review Order

**Concurrency checkpoints**

- Start with the five strengthened lifecycle behaviors and their exact assertions.
  [`AccessTelemetryLifecycleIntegrationCheckpointTests.cs:32`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs#L32)

- Force CR42 operation entry before either inner call can complete.
  [`InnerOperationOverlapStateStore.cs:12`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/InnerOperationOverlapStateStore.cs#L12)

- Capture exact committed records while coordinating CR44 writers independently.
  [`CoordinatedAccessTelemetryStateStore.cs:12`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/CoordinatedAccessTelemetryStateStore.cs#L12)

**Time and authorization checkpoints**

- Prove exact 250-per-second admission with an independently binding byte limit.
  [`AccessTelemetryLifecycleIntegrationCheckpointTests.cs:110`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs#L110)

- Drive the real worker across retry and five-minute age boundaries.
  [`AccessTelemetryLifecycleIntegrationCheckpointTests.cs:178`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs#L178)

- Bind least privilege to parsed YAML structure and exact authoritative fields.
  [`AccessTelemetryYamlLeastPrivilegeValidator.cs:11`](../../tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryYamlLeastPrivilegeValidator.cs#L11)

**Evidence and governance**

- Review the independently accepted C0 receipt without changing Story 27.3 ownership.
  [`27-3-production-adapter-and-deployment-profile.md:784`](27-3-production-adapter-and-deployment-profile.md#L784)

- Trace CR42's rejection, remediation, executed evidence, and final acceptance.
  [`deferred-work.md:2711`](deferred-work.md#L2711)
