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

From `/home/administrator/projects/hexalith/platform`, with `RETENTION_EVIDENCE` set to a fresh canonical repository-relative JSON path under the declared Memories owner (the supported tool refuses absolute evidence paths):

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
| Local functional retention | Jérôme Piquot; Memories assertions, Platform/Builds lifecycle | Command above; latest native accounting failure plus actual terminal/disposal result; previous failed native TRX and partial cohorts retained | Focused source/preparation checks passed; real failures and executable drift preserved | In progress: additional approved run `42f30245ad0e0cbf77e3ec400ad4e74e` passed mapping/readiness, then failed first-cohort accounting; a read-only snapshot observed 124/125 A rows and no cause is established. Removed/verified empty. Diagnostic capture and 43 offline checks pass. Both authorized retention launches are consumed; no further launch is authorized. Shared deadline remains 2026-10-22T19:08:27.343741Z. |
| Full qualification / A41 | Jérôme Piquot; Platform Operations and Security roles | Current per-gate inventory linked below | Pending | Deferred to a separately scheduled campaign; open |

## Full qualification and close-out checkpoints

The [current per-gate inventory](../../docs/operations/access-telemetry-full-qualification.md) remains the full campaign's requirements/owner/state register, subject to adopting its exact current target/profile. Its workload, capacity, faults, backup/restore, long-duration, security, terminal and publication obligations do not gate local functional completion. Functional success earns no full-profile or Production credit and cannot close A41. Overall Story 27.4 stays in progress until its separate full close-out obligations are met.

## Dev Notes

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
