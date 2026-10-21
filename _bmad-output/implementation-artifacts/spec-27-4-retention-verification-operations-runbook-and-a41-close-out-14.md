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

1. Use Platform's supported local tool **4.35.1**, source mode and Debug project references. Keep the declared Dapr **1.18.1** and PostgreSQL **18.6** immutable images. Platform/Builds owns environment, readiness, resource limits and disposal; Memories owns assertions.
2. Select one isolated copy of the required source/configuration or reserve the selected build outputs against concurrent rebuilds. Build once. Record the runner payload, loaded application/dependency files, test entry/imports, owner manifest, generated Platform/Dapr configuration and image identities. Preserve supported resolver/direct-mapping checks. Changes to an actual input require rebuilding/rechecking that affected input; unrelated Markdown, reviews, Git HEAD changes alone, or files in unused modules do not invalidate frozen inputs. No 19-repository byte census, repeated source-settling loop or private verification framework.
3. Prepare one concrete local run plan: resolved paths, tool/artifact identities, fresh evidence directory, resource ownership/limits, absolute window, retries and disposition. Keep the existing 14400-second execution ceiling, 600-second startup within it and separate 900-second stopping ceiling. Check actual capacity/isolation once immediately before launch. The current time window ends 2026-10-25T14:42:22Z (latest full-budget start 10:27:22Z); a later run needs a new window.
4. Obtain one operational authorization for that complete plan. It covers setup, test and normal stopping/disposition; no separate approval for unchanged steps, passing results or evidence-file hashes. Default: one launch, no automatic restart; uncertain cohort submission may reconcile the same identity once. Any startup retry must be explicitly bounded in that plan. Material changes to target, actions, permissions or resource/time limits require a new decision. The current approval implements this contract; it is not an approval of an as-yet-unprepared live plan.
5. Run the existing command below. Keep a failed result and fix the concrete failing input; do not restart the preparation ceremony. Old retained resources are outside this run's disposal authority.

From `/home/administrator/projects/hexalith/platform`, with `RETENTION_EVIDENCE` set to the plan's fresh absolute output path:

```bash
dotnet tool run hexalith-module -- test \
  --manifest references/Hexalith.Memories/module/hexalith.module-manifest.v2.json \
  --profile functional-retention --mode source \
  --evidence "${RETENTION_EVIDENCE:?set the approved fresh absolute output path}" --output json
```

The runner invokes `tools/verify-access-telemetry-lifecycle.py` through its selected-test handoff. Do not invoke the historical VM checkpoint or construct a handoff manually. Version/help checks establish CLI availability only; model/startup compatibility must still pass.

## Finish and report

Functional completion requires passing native TRX and `functional-observations.json`, plus Platform's actual terminal result and successful bounded resource disposition. Failure, withdrawal or unverified disposal is recorded explicitly. Update the [single current report](tests/27-4-retention-verification-evidence.md) with versions/configuration, command, timestamps, behavioral result, log/artifact paths and disposition. Keep private credentials out of the report. One ordinary review of changed behavior is sufficient; no repeated three-reviewer/readback chain for documentary changes.

| Track | Owner | Evidence | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| Local functional retention | Jérôme Piquot; Memories assertions, Platform/Builds lifecycle | Command above; native report, observations and terminal result | Offline checks only; live review pending | Pending real run; independently finishable |
| Full qualification / A41 | Jérôme Piquot; Platform Operations and Security roles | Current per-gate inventory linked below | Pending | Deferred to a separately scheduled campaign; open |

## Full qualification and close-out checkpoints

The [current per-gate inventory](../../docs/operations/access-telemetry-full-qualification.md) remains the full campaign's requirements/owner/state register, subject to adopting its exact current target/profile. Its workload, capacity, faults, backup/restore, long-duration, security, terminal and publication obligations do not gate local functional completion. Functional success earns no full-profile or Production credit and cannot close A41. Overall Story 27.4 stays in progress until its separate full close-out obligations are met.

## Dev Notes

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
