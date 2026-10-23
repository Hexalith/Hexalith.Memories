---
title: 'Story 27.4 sole-owner qualification and A41 close-out'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
route: 'dispatch'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
baseline_commit: 'b1d8014fa0dbacaf71528ddda74ff83cf19ca3ca'
context:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-21.md'
  - 'docs/operations/access-telemetry-owner-handoff.md'
---

# Story 27.4 — active functional retention contract

Approved 2026-10-21 by the owner's **“do recommended”** response. This replaces accumulated preparation, source-freeze and review-loop instructions. The owner subsequently requested removal of obsolete history; superseded continuation specs, preparation diaries and execution proposals are removed. Outstanding resource disposition is recorded in the [current report](tests/27-4-retention-verification-evidence.md#retained-resources).

## One outcome

Prove local retention with the existing `functional-retention` test: one writer emits 125 records in cohort A and 125 in B at least 1800 real seconds apart. At 3600-second retention, A remains strongly readable before expiry, then becomes strongly absent with due-index cleanup and reminder progress within 900 seconds. B must remain unchanged. Keep real Dapr/PostgreSQL, stored-row/TTL observations, signed-clock validation and UTC/monotonic measurements. No fake time, injected results or direct backend writes.

## Prepare once, run once

1. Use Platform's supported local tool **4.35.2**, source mode and Debug project references. Keep the declared Dapr **1.18.1** and PostgreSQL **18.6** immutable images. Platform/Builds owns environment, readiness, resource limits and disposal; Memories owns assertions.
2. Select one isolated copy of the required source/configuration or reserve the selected build outputs against concurrent rebuilds. Build once. Record the runner payload, loaded application/dependency files, test entry/imports, owner manifest, generated Platform/Dapr configuration and image identities. Preserve supported resolver/direct-mapping checks. Changes to an actual input require rebuilding/rechecking that affected input; unrelated Markdown, reviews, Git HEAD changes alone, or files in unused modules do not invalidate frozen inputs. No 19-repository byte census, repeated source-settling loop or private verification framework.
3. Prepare one concrete local run plan: resolved paths, tool/artifact identities, fresh evidence directory, resource ownership/limits, absolute window, retries and disposition. Keep the existing 14400-second execution ceiling, 600-second startup within it and separate 900-second stopping ceiling. Check actual capacity/isolation once immediately before launch. The current time window ends 2026-10-25T14:42:22Z (latest full-budget start 10:27:22Z); a later run needs a new window.
4. Obtain one operational authorization for that complete plan. It covers setup, test and normal stopping/disposition; no separate approval for unchanged steps, passing results or evidence-file hashes. Default: one launch, no automatic restart; uncertain cohort submission may reconcile the same identity once. Any startup retry must be explicitly bounded in that plan. Material changes to target, actions, permissions or resource/time limits require a new decision. The current approval implements this contract; it is not an approval of an as-yet-unprepared live plan.
5. Run the existing command below. Keep a failed result and fix the concrete failing input; do not restart the preparation ceremony. Old retained resources are outside this run's disposal authority.

From the selected Platform source directory in the [current report](tests/27-4-retention-verification-evidence.md), with `RETENTION_EVIDENCE` set to a fresh canonical repository-relative JSON path under the declared Memories owner (the supported tool refuses absolute evidence paths):

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence "${RETENTION_EVIDENCE:?set the fresh owner-relative JSON evidence path}" --output json
```

The runner invokes `tools/verify-access-telemetry-lifecycle.py` through its selected-test handoff. Do not invoke the historical VM checkpoint or construct a handoff manually. Version/help checks establish CLI availability only; model/startup compatibility must still pass.

## Finish and report

Functional completion requires passing native TRX and `functional-observations.json`, plus Platform's actual terminal result and successful bounded resource disposition. Failure, withdrawal or unverified disposal is recorded explicitly. Update the [single current report](tests/27-4-retention-verification-evidence.md) with versions/configuration, command, timestamps, behavioral result, log/artifact paths and disposition. Keep private credentials out of the report. One ordinary review of changed behavior is sufficient; no repeated three-reviewer/readback chain for documentary changes.

| Track | Owner | Evidence | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| Local functional retention | Jérôme Piquot; Memories assertions, Platform/Builds lifecycle | Existing command; current native TRX, both 125/125 accounting records, actual binary/source and final input proof, sidecar/listener trace, private transport diagnostic and verified disposal | Focused preparation checks passed; actual failed evidence reviewed | In progress: run `7448fa8d180acc30f866d87a3612e667` reached readiness and both cohorts, then failed the first pre-expiry read after sidecar launchers/listeners disappeared. 48 bounded connection refusals; initiating sidecar exit cause unresolved. Removed/verified empty within 308.377546s. Retention, expiry/purge and final newer-record preservation remain unproved. One authorized launch consumed; no retry or restart. Earlier results retained in the report. |
| Full qualification / A41 | Jérôme Piquot; Platform Operations and Security roles | Current per-gate inventory linked below | Pending | Deferred to a separately scheduled campaign; open |

## Full qualification and close-out checkpoints

The [current per-gate inventory](../../docs/operations/access-telemetry-full-qualification.md) remains the full campaign's requirements/owner/state register, subject to adopting its exact current target/profile. Its workload, capacity, faults, backup/restore, long-duration, security, terminal and publication obligations do not gate local functional completion. Functional success earns no full-profile or Production credit and cannot close A41. Overall Story 27.4 stays in progress until its separate full close-out obligations are met.

## Review Triage Log — sidecar-loss continuation

| Finding | Verdict / evidence | Resolution |
| :--- | :--- | :--- |
| Native shutdown completeness overclaimed | Medium: installed DCP stops public watchers before cleanup; a final Running snapshot does not prove exit. | Capture receipts now distinguish reported terminal state/exit code from delivered-record persistence and explicitly disclose absence. Stale-Running public-service regression passes; report/plan corrected. No lifetime or cleanup behavior changed. |
| Reported Terminated state discarded | Medium: controller termination is reported as Terminated by DCP but was mapped to unavailable. | Fixed safe-state list; public notification regression preserves Terminated and reported exit metadata. |

The single ordinary review is complete. Both patches are covered by the final **8/8** focused run and a clean compatible source/Debug model build. No findings deferred. Live retention, full qualification, Production admission and A41 remain open; no further launch is authorized.

## Dev Notes

### Tasks & Acceptance — adopt EventStore 3.127.0 into the prepared candidate (active)

Continue the already approved repository preparation after the owner reports the changes pushed. Preserve the remaining Builds catalog edit selecting 3.127.0; do not stage, commit, push, branch, change another dependency or launch/restart a topology. Overall Story 27.4 and A41 remain open. The previous native-capture proposal is historical until this affected-input adoption completes.

- [ ] In a fresh private sibling of `DIAG=/home/administrator/.local/state/hexalith/memories/access-telemetry/diagnose-spec14-native-capture-20261023-7wlh50dh`, reuse its physical `compatible-platform` candidate, supported 4.35.2 tool/configuration, compatible Platform capture correction and existing Memories retention assertions. Adopt only affected EventStore 3.127.0 source/catalog inputs from the approved direct release `e1d8b17160202761ab5c9606cafbcf4d1a4b92c3` and current owning Builds catalog, preserving all previous evidence/candidates. Inspect owning guidance/configuration before changes; never load skills under `references/`. Given the updated workspace, when the candidate is prepared, then supported direct mapping and physical project references select these adopted inputs without writing through links to shared repositories.
- [ ] Rebuild affected candidate Server and Platform composition outputs in source/Debug, run relevant existing catalog/configuration and public capture regressions, and retain exact commands/results plus DLL/PDB/source and affected-input identity checks in the fresh private directory. Reuse valid unaffected proof; retain failures and distinguish focused fallback results from broad blockers. Given the candidate, when builds and relevant checks finish, then the actual proposed outputs bind the approved EventStore release/catalog and existing capture correction with unchanged tool 4.35.2, native Dapr 1.18.1 and PostgreSQL 18.6. Do not run the long retention test or any real topology as a regression.
- [ ] Update this existing spec and `tests/27-4-retention-verification-evidence.md` with one concrete replacement run plan: resolved cwd, evidence/state paths, actual input/artifact identities, exact native command, valid absolute start/end window, unchanged resource/assertion/retry bounds and exact-run stopping/disposition. Preserve the 14400s execution (600s included startup), separate cumulative 900s stopping and existing 2026-10-25T14:42:22Z outer end; use the actual current UTC time for a feasible window and identify an expired window explicitly. Given successful preparation, when the root completes one ordinary review, then the owner can authorize this complete plan once; preparation itself grants no launch/restart authority. Include proof of all-four-sidecar capture and loaded-input binding before the long wait in the admission plan.

#### EventStore candidate adoption result — 2026-10-23; blocked

A fresh physical candidate is retained at `ADOPT=/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-eventstore31270-20261023-fz54y5zo`. Approved release source/catalog adoption, mapped Server and compatible composition Debug builds, **11/11** ordinary package/configuration checks, **8/8** public capture checks, catalog validators and DLL/PDB/source checks succeeded. The existing **307** assertion/capture inputs are unchanged; prior candidates/evidence remain preserved.

**Native admission is not satisfied.** Installed 4.35.2's actual public model-build API succeeded offline but materialized the candidate Platform identity's Builds **`66b9136a0b223925e51177a51eb48852e2f12787`** catalog: EventStore **3.126.1**, also observed in the native-built assemblies. Working-catalog 3.127.0 outputs cannot stand in for those native outputs. The concurrent owner push committed owning Builds **`58cebcca755eb80b678a1d2870e3779ea7eb5349`** with 3.127.0; current Platform **`858d02c05eb837a4ee85c5215430201f117dc24d`** still pins a 3.126.1 catalog. A matching approved compatible Platform catalog pin remains required; no Git identity or check was bypassed.

The [current report](tests/27-4-retention-verification-evidence.md#eventstore-candidate-adoption--2026-10-23-blocked-by-native-catalog-pin) records exact checks, retained failures and the concrete **conditional** replacement plan. Its feasible window is **2026-10-23T15:30:00Z–2026-10-25T10:27:22Z**, with unchanged **14400s / 600s included startup / separate cumulative 900s stopping** and **2026-10-25T14:42:22Z** outer end. The plan is **not ready for operational authorization**. The unchecked acceptance tasks remain incomplete until the native path binds the approved catalog and root reviews a ready plan. No topology was launched/restarted; no staging, commits, pushes, branches or shared source changes were performed by this preparation. Story 27.4, initiating sidecar cause, full qualification, Production and A41 remain open.

### Owner-directed EventStore update — 2026-10-23

The owner's later **“update to latest eventstore version”** authorizes EventStore **3.127.0**, superseding the prior no-dependency-update constraint for this dependency only. Memories' direct EventStore source now selects stable release **`e1d8b17160202761ab5c9606cafbcf4d1a4b92c3`**; the owning Builds central family pin and existing catalog/restore regression use **3.127.0**. Server, test-project and source AppHost Debug builds pass with zero warnings/errors; existing dependency/security configuration checks pass **11/11**, and both central catalog validators pass. The [current report](tests/27-4-retention-verification-evidence.md#owner-directed-eventstore-update--2026-10-23) retains exact checks and limitations.

The earlier private native-capture run proposal remains unexecuted and predates this workspace update; affected EventStore source/catalog outputs still require adoption/rebuild/binding before proposing a 3.127.0 run. Reuse unaffected preparation. No launch/restart is authorized, and all earlier permissions remain consumed. Platform tool **4.35.2**, native Dapr **1.18.1**, PostgreSQL **18.6**, source/Debug, real timing, assertions and resource limits remain fixed. Historical evidence and user changes are preserved; no staging, commits or pushes. The initiating sidecar exit cause, full qualification, Production admission and A41 remain open.

### Sidecar-loss continuation — 2026-10-23 (active)

Resume run `7448fa8d180acc30f866d87a3612e667` from the unchanged private evidence at `/home/administrator/.local/state/hexalith/memories/access-telemetry/prepare-spec14-transport-20261023-6yl941ny`. Diagnose the actual executed binary/source provenance before changing behavior. Readiness and retained listeners show resource names ending `-dapr-cli` ran native `daprd`; installed AppHost is `/home/administrator/.nuget/packages/hexalith.builds.module.cli/4.35.2/tools/net10.0/any/g4-host/Hexalith.Builds.Module.AppHost.dll`. The private Platform composition uses a compatible older source snapshot, not current shared HEAD. One-hour coincidence alone establishes no initiating cause.

#### Tasks & Acceptance (sidecar-loss continuation)

- [x] Trace actual installed AppHost/Tooling, private composed Platform DLL/PDB/source, native Dapr and retained container/configuration evidence. Save bounded, secret-free provenance and findings in a fresh sibling diagnostic directory; do not modify historical evidence. Given the failed run, when provenance is inspected, then distinguish executed inputs from merely current source and distinguish facts from hypotheses.
- [x] Establish an initiating exit cause before any lifetime/retry/security behavior fix. If retained evidence cannot establish it, fix supported native sidecar console-log and resource-state/exit capture in the owning `/home/administrator/projects/hexalith/platform` repository (or Builds only if necessary), using public Aspire resource logging/notification APIs compatible with installed 4.35.2; do not wrap sidecars or add another runner. Capture private bounded logs and attributable PID/exit metadata without dumping environment, arguments or secrets; disclose unavailable exit reason explicitly rather than inferring one. Relevant code: `src/Hexalith.Platform.Composition/PlatformAppModel.cs`, `PlatformDaprEndpointSubscriber.cs`, existing composition tests, and Builds' `src/hosts/Hexalith.Builds.Module.AppHost/AppHostRunner.cs`. Given missing native evidence, when an actual sidecar logs/exits, then evidence survives stopping and identifies its resource and reported exit data. Tests must exercise the actual public log/notification path, private file permissions, bounded capture, disposal/cancellation and secret exclusion. No real topology launch/restart is authorized for regressions.
- [x] Run focused Debug regressions and record exact commands, results, warnings and limitations in the current report. Preserve shared dirty files and existing resource limits. Read owning repository guidance/configuration first. No staging, commits, pushes, branches or dependency version changes; no skills under `references/`.
- [x] Root completes one ordinary review and prepares a fresh concrete run plan in this existing spec/report before requesting its single operational authorization. Preserve supported Platform tool 4.35.2, Dapr 1.18.1, PostgreSQL 18.6, source/Debug, real timing, current retries and all retention assertions. Reuse prior unaffected preparation and recheck only affected actual inputs; preserve prior evidence/source. Old authorizations are consumed: do not launch/restart. Full qualification, Production admission and A41 stay separate/open.

#### Capture correction and verification

The [current report](tests/27-4-retention-verification-evidence.md#sidecar-loss-diagnosis-and-capture-correction--2026-10-23) records executed installed tool/PDB/release-source provenance, actual runtime composition/PDB sources and verified native Dapr identity. The initiating exit cause remains unknown; no lifetime or retry change was made. Platform owns a supported public-API console/state/PID/exit collector with bounded private tails and explicit unavailable exit reason, failure and truncation markers. Focused direct public-service tests pass **8/8**; the compatible source/Debug model build passes with zero warnings/errors. The broad owning test-project build remains separately blocked by pre-existing EventStore `CS0234`; exact command/result and initial failed checks remain in the report.

The report's [fresh concrete native-capture plan](tests/27-4-retention-verification-evidence.md#fresh-native-capture-run-plan--awaiting-one-operational-authorization) uses the unchanged 4.35.2 / 1.18.1 / 18.6 source/Debug stack, existing real timing/assertions, resource limits and one-launch/zero-retry bounds. Start window **2026-10-23T12:15:00Z–2026-10-25T10:27:22Z**, outer end **2026-10-25T14:42:22Z**; **14400s execution / 600s included startup / cumulative separate 900s stopping**. All prior permissions remain consumed. One ordinary review is complete; its two verified findings are fixed and no finding is deferred. Public producers stop before native cleanup, so receipts explicitly disclose unavailable terminal evidence instead of claiming complete shutdown capture. No new topology has been launched. Overall Spec 14 / Story 27.4 remains **in progress**, and full qualification, Production admission and A41 stay separate/open.

### Diagnostic live result — 2026-10-23; failed, disposal verified

The owner approved the complete prepared plan with **“I approve”**. One invocation ran **09:18:03.098695Z–10:19:11.191117Z**, native run `7448fa8d180acc30f866d87a3612e667`, returning **exit 4 / HXT006** at the first pre-expiry strong read. All 15 resources reached readiness and both cohorts accounted for 125/125, with no rejected/dropped/conflicted records. The actual correction was loaded, and 303 selected inputs / 201 output files remained unchanged. The trace observes sidecar launchers disappearing at **10:18:31–35Z**, followed by 48 connection refusals in the unchanged bounded read batch; the initiating sidecar exit cause remains unresolved. Native TRX reports one failed test and no passing observations file exists. Retention, expiry/purge and final canonical B preservation remain unproved.

Supported exact-run disposal returned exit 0 and was verified **removed / empty / unverified:false** at **10:23:39.855987Z**, with all captures/children stopped. Conservatively charging from the earliest observed sidecar disappearance, stopping/capture/disposal took **308.377546s**, within 900s. No restart or retry occurred; this launch permission is consumed. The [single current report](tests/27-4-retention-verification-evidence.md#diagnostic-live-run--2026-10-23-failed-disposal-verified) retains actual commands, native evidence, availability timeline, limitations and disposition. Platform/Builds owns the remaining sidecar-lifetime/log-access diagnosis; full qualification, Production and A41 remain separate/open.

### Prepared diagnostic run — 2026-10-23

The [current report](tests/27-4-retention-verification-evidence.md#prepared-diagnostic-run--2026-10-23-historical-snapshot) contains the complete fresh plan: isolated source/configuration, supported 4.35.2, source/Debug, unchanged immutable images and retention assertions, fresh state/evidence, one launch with zero startup retries, admission and exact-run disposition. Proposed start window **2026-10-23T09:15:00Z–2026-10-25T10:27:22Z** retains the **14400s execution / 600s startup / separate cumulative 900s stopping** limits and **2026-10-25T14:42:22Z** outer end. At preparation no run was launched or authorized; this proposal snapshot is superseded by the actual approved result above.

The previous transport cause remains unknown. A bounded private diagnostic now retains fixed transport category/phase, attempt/ordinal and UTC/elapsed time, excluding keys, URLs, credentials, exception text and bodies; retry/security/acceptance behavior is unchanged. The fresh Debug build also demonstrated a submodule-guard gap, repaired in Memories by recognizing the genuine enclosing Platform root while retaining `CheckSubmodules` and missing-root refusal. Selected checks pass **49/49**, isolated diagnostics **19/19**, source/archive guards **4/4**, and both isolated builds pass with no warnings/errors. Input identities, supported mapping, binary/source binding and exact commands are in the current report. Earlier failed evidence and consumed permissions remain preserved. Functional retention, full qualification, Production credit and A41 remain open; the prepared plan needs one fresh operational decision.

### Corrected-queue live result — 2026-10-22

The owner approved the complete prepared plan. Its one invocation ran **19:52:29.939073Z–20:53:32.663803Z**, native run **`7323f05526580a313a045fc305def117`**. All 15 resources reached readiness. The actual loaded Server DLL/PDB binds to the three corrected source checksums; all 16 loaded DLLs remained unchanged at failure. Both cohorts reported exact **125/125** accounting with zero rejected/dropped/conflicted.

The native test failed **`functional-selected-state-read-transport-exhausted`**, CLI **4 / HXT006**, at A's first pre-expiry strong read. Expiry/purge, full one-hour retention and final newer-record preservation remain unproved; no passing observations file exists and the underlying transport cause is not established. Supported exact-run disposal returned exit 0. At **20:55:22.938586Z**, actual terminal was `removed`, ownership and fresh exact-run Docker scan were empty, `unverified: false`, and capture processes/children were stopped. Total stopping/capture/disposal was **110.650687s**, within the cumulative 900s allowance. No retry or restart occurred; this launch permission is consumed. The [current report](tests/27-4-retention-verification-evidence.md#corrected-queue-live-run--2026-10-22-failed-disposal-verified) retains commands, native TRX, binary proof, accounting, logs and disposition. Full qualification, Production credit and A41 remain separate/open.

### Prepared corrected-queue run — 2026-10-22

Preparation from `93307043953a02e3629488785cbedcec2ff888ac` is complete in the [single current report](tests/27-4-retention-verification-evidence.md#prepared-corrected-queue-run--2026-10-22-historical-snapshot). The selected isolated Memories checkout contains the three exact production correction files; its Debug Server DLL/PDB was checked against all three source checksums. The private Platform composition source reuses the previously ready 4.35.2-compatible revision; its model build passes. Shared concurrent edits remain untouched. Two duplicate package declarations were corrected only in the private copy, with both versions unchanged. Existing runner, diagnostics, 125/125 cohorts, one-hour retention and expiry/purge/newer-record checks are preserved. Per-run loaded binaries still require the recorded startup binding because Platform generates them only during launch.

**Historical preparation snapshot; the approved execution and consumed permission are recorded above.** The proposed fresh start window is **2026-10-22T18:20:00Z–20:00:00Z**, with one launch, zero startup/automatic retries, 14400s execution including 600s startup, and separate cumulative 900s cleanup. Absolute latest execution/cleanup is **2026-10-23T00:00:00Z / 00:15:00Z**. Approval of this complete plan would grant a new budget; the earlier continuation's permissions and deadline are not reused or reset. The report contains exact command, fresh paths, actual binary proof, resource limits, admission and disposal. Local functional, full qualification and A41 statuses remain open.

### Offline reliable-admission correction — 2026-10-22

The owner-approved [focused correction](spec-27-4-reliable-telemetry-admission.md) separates concurrent producer admission from consumer-owned pending delivery, retains atomic record/byte reservations through acknowledgement or expiry, and removes consumer-gate acquisition from logger metric reads. Worker drain passes are serialized, and qualification accounting is registered before a record is published. Marker ownership covers accepted reservations before publication and is capped for heartbeat reporting; transfer preserves head visibility, and ordinary logging omits the qualification callback. The [current report](tests/27-4-retention-verification-evidence.md#offline-reliable-admission-correction--2026-10-22) records exact commands and limits: Debug build passed with no warnings/errors; the final combined run passed **73/73** (44 checkpoint and 29 qualification cases), with zero errors, failures or skips. All review findings were resolved; none was deferred. The report preserves the earlier corrected fixture failures and final verification. Barrier-controlled fixed-workload coverage verifies **125/125 expected IDs**, retained canonical-byte size bounds and exact segment accounting, with acknowledgement complete before the last producer barrier is released. Direct drain overlap is tested; shutdown's reuse of that method is source-inspected, without a hosted shutdown-overlap claim. Payload bodies are not independently compared. The unchanged accounting diagnostic regressions passed **3/3** in final verification. Memories projects are source-built; existing unconditional external package references remain despite the source-reference flag and no fully external-source build is claimed.

These are offline correction results. The prior 124/125 live cause remains unproved, retention remains unproved, and the functional/full-qualification completion states above are unchanged. The existing live test, one-hour retention, 125/125 acceptance, diagnostic capture, native evidence, expiry/purge/newer-record assertions and Platform terminal/disposition requirements remain the execution contract. No further launch is authorized; any new run requires fresh authorization under this contract. No Production or A41 credit follows from this correction.

### Current resumption — 2026-10-22

The owner approved the prepared plan on 2026-10-22; its one launch `2397010f0d8da3011f1c7a1ead881586` failed during startup (HXR025; Dapr Sentry exited with code 1) and was removed/verified empty. The current report records that outcome. The previous operational budget ended at 2026-10-21T20:13:22.9004007Z. On 2026-10-22 the owner authorized this continuation inside the unchanged outer window (ends 2026-10-25T14:42:22Z; latest full-budget start 10:27:22Z) and existing resource limits:

- Diagnose and fix that demonstrated startup failure, then run the existing `functional-retention` test.
- Up to two diagnostic startup attempts and one retention-test launch.
- One shared 14400-second execution budget, anchored to the first launch of this continuation. Startup stays bounded to 600 seconds per attempt and remains inside that shared budget.
- 900 seconds cumulative cleanup. Capture actual resource logs and failure details before cleanup.
- Proceed within these bounds without further confirmation. No automatic restarts and no budget resets.
- Reuse existing preparation and relevant input identities. Preserve source/Debug mode, Platform tool 4.35.2, immutable Dapr 1.18.1 and PostgreSQL 18.6 pins, security checks, user changes, and previously retained resources. Old retained resources stay outside this run's disposal authority.
- Use the container graph only. Do not create a KVM or QEMU guest. The next container admission requires Docker memory and Linux `MemAvailable` of at least 17,179,869,184 bytes (16 GiB), the PostgreSQL container memory cap. Keep the 8 CPU floor and the 20 GiB disk floors. The qualification guest profile stays unused by this run.
- Fix the demonstrated cause in its owning repository and run focused regressions there. Do not modify another repository's unrelated existing fixes.
- Use supported Platform lifecycle commands. Verify removal of each new run's resources.
- Update the single current report with authentic results. Local functional completion stays separate from full qualification and A41 closure.

The earlier transport-edge repair remains historical preparation unless a later diagnosis shows it is the demonstrated cause. Do not restart the preparation ceremony, create another spec, or claim full qualification.

This continuation's once-before-launch check at 2026-10-22T07:32:14.751678Z refused admission. Linux `MemAvailable` was 34,637,570,048 bytes, below the 42,949,672,960-byte floor then in force. The shared 14400-second budget was not anchored. Zero of two diagnostic attempts and the one retention launch were used. No resources were created, and previously retained resources were not disposed. The HXR025 Sentry exit remains unexplained.

Later on 2026-10-22 the owner directed the functional run to stay on containers and lowered that memory prerequisite to 17,179,869,184 bytes (16 GiB), the PostgreSQL container cap. The 07:32:14Z refusal remains the check that ran. The lowered floor applies to the next container admission and does not by itself start a run.

The owner subsequently replied **“I approve”** on 2026-10-22 to using the currently supported Platform **4.35.2** payload for this same container continuation. This narrowly replaces 4.35.1; the target, source/Debug mode, immutable images, security checks, resource floors/limits, two diagnostic startup attempts, one retention launch, shared execution/startup/stopping budgets and outer window remain unchanged. The current report records the installed payload and supported resolver checks. No version pin was changed, and this decision does not claim startup or retention success.

The approved 4.35.2 continuation launched at **2026-10-22T15:08:27.343741Z**, run `8b77763653bf84d6ff2b35c02fffda74`. All 15 resources reached readiness; the native test failed `functional-selected-mapping-hash` before cohorts. Supported disposal completed and a fresh exact-run check verified removal/empty resources within 119.959932 seconds of terminal completion. The authentic mapping proves a Python/.NET JSON escaping mismatch; the narrow Memories repair and offline regressions are recorded in the current report. **The one retention launch is consumed; zero diagnostic-only attempts were used.** No additional native-test retry is authorized by the tool substitution. A fresh command is prepared in the report for an explicit additional-launch decision, preserving the shared **19:08:27.343741Z** execution deadline and **780.040068 seconds** cumulative cleanup remaining. No budget reset or full qualification credit is inferred.

The owner then replied **“approve”** for exactly one additional retention launch with the reviewed mapping repair, unchanged container/security/resource bounds, original **2026-10-22T19:08:27.343741Z** execution deadline, latest complete-observation start **17:43:26.343741Z**, and **780.040068 seconds** cumulative cleanup remaining. This is explicit authorization for that prepared additional command only; it grants no automatic restart or budget reset. Runtime inputs must be preserved before supported cleanup.

That one additional launch executed at **15:52:57.859407Z**, run `42f30245ad0e0cbf77e3ec400ad4e74e`, and reached all-resource readiness with the mapping repair accepted. It failed `functional-accounting-loss`; a later read-only PostgreSQL snapshot observed 124/125 A rows, ordinal 30 absent. The returned counters were not retained, so rejection/drop/incomplete accounting/durable loss and the cause remain unresolved. Generated runtime inputs and actual logs were captured before supported disposal; fresh verification at **15:57:27.053258Z** established `removed`/empty. Cumulative cleanup charge is **282.870915 seconds**, leaving **617.129085 seconds**. A narrow owner-private bounded accounting observation now precedes validation; all original assertions remain, and **43/43** selected offline regressions pass. **Both authorized retention launches are consumed; zero diagnostic-only attempts were used. No further launch is authorized or prepared.** The original shared **19:08:27.343741Z** deadline is unchanged. Functional completion and full qualification/A41 remain open; the current report retains actual evidence and its limits.

### Historical Context Classification

| Prior work | Classification | Permitted use |
| :--- | :--- | :--- |
| Accumulated Spec 14 preparation/review loops | anti-template | Removed; do not reproduce their execution shape. |
| Existing selected Platform retention consumer | current-narrow-pattern | Reuse the registered test and real two-cohort assertions. |
| Earlier approvals and failed runs | historical-reference-only | Use the retained-resource summary; do not revive consumed permissions. |

### Slice Proof

One measurable local retention outcome; full qualification remains separately tracked above. No new story number, runner, environment framework or gate packet is needed.

### Epic AC Verification

See the compact command/verdict table in the [approved correction](../planning-artifacts/sprint-change-proposal-2026-10-21.md#verification). Live behavior is an acceptance target, not an established fact.
