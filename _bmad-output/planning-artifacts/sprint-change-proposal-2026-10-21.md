# Story 27.4 — approved functional-path simplification

**Decision:** On 2026-10-21 the owner said **“do recommended”** after the recommendation to replace accumulated execution instructions with a short contract, reuse one real retention test, bind relevant runtime inputs, use one bounded operational authorization, keep a short report and schedule full qualification separately. This approves the documented implementation below. It does not claim a live test pass or close A41. Batch implementation is used; repeating approval of this same scope is unnecessary.

## Issue and approach

The previous preparation passed seven offline operations but stopped when an unrelated Platform planning document appeared. Repeated whole-workspace freezes and reviews made preparation the dominant activity. The approved remedy is direct adjustment of the existing story, not another successor spec or a product/runtime rollback.

[Active Spec 14](../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) is now the short execution contract. [The current report](../implementation-artifacts/tests/27-4-retention-verification-evidence.md) is the sole human results surface. The owner subsequently requested **“also remove any obsolete history”**. Superseded continuation specs, copied preparation diaries and obsolete execution proposals are removed. The [full campaign checklist](../../docs/operations/access-telemetry-full-qualification.md), current results and retained-resource dispositions remain.

## Applied changes

| Surface | Before | After / reason |
| :--- | :--- | :--- |
| Functional preparation | All tracked/nonignored bytes across 19 repositories had to stay identical; even a planning file stopped progress. | Freeze inputs that affect the selected build/run. Preserve supported resolver and mapping checks. Bind runner/application/dependency binaries, selected test and imports, manifest, runtime configuration and image identities. Document-only changes and unrelated files cannot invalidate those frozen inputs. |
| Tool selection | Retained private diagnostic 4.31.2 with an out-of-range exception. | Select installed Platform pin 4.35.1. Runtime/model compatibility still needs ordinary verification; no silent fallback to the diagnostic build. |
| Authorization/reviews | Exact evidence digests, repeated preparation reviews and independent wrapper audits repeatedly reopened decisions. | One concrete operational plan covers setup, behavior and disposition with stated bounds/retries. Reapproval follows material target/action/permission/resource/time changes. One ordinary review of changed behavior; no recursive evidence audits. |
| Completion | Functional feedback was buried in the full close-out campaign. | The local functional outcome has its own finish line and report. The full campaign retains its per-gate owner/evidence/review/completion inventory and is separately scheduled. Overall Story 27.4 stays in progress; A41 remains open. |
| Active documents | Large chronological spec/report, stale current headings. | Short current contract/report, obsolete history removed, consistent epics/context/handoff pointers. Preserve the existing report's full-qualification matrix and former profile identity. |

## Impact and scope

- **Epic 27:** immediate priority is the existing real local two-cohort test. Full workload, capacity, faults, backup/restore, long-duration and Production security remain separate campaign obligations. No story is created, renumbered or falsely completed.
- **PRD:** NFR34 and the access-telemetry assurance boundary are unchanged. Local feedback neither grants Production admission nor meets full retention assurance.
- **Architecture:** environment ownership stays with Platform/Builds; Memories asserts behavior through existing services/Dapr. Add a dated sequencing note; no service, schema, backend or API redesign.
- **UX:** no interface, accessibility or user-flow change. No UX artifact needs editing.
- **Other work:** Epics 31/35 keep their infrastructure/security gates; no dependency revision, generated declaration, CI configuration, AGENTS baseline or unrelated completed story is changed.
- **Effort/risk:** a bounded documentation/routing change using existing code; lower preparation cost. Main remaining risk is actual runtime compatibility/readiness, resolved by normal model/startup checks rather than a fabricated readiness claim. The test itself needs real elapsed time (roughly 60–75 minutes after cohort A, within the existing four-hour cap).

## Verification

These commands are re-runnable from Memories unless a different cwd is stated. Source observations confirm existing mechanisms; live outcomes remain acceptance targets.

| Claim being relied on | Command / evidence | Verdict |
| :--- | :--- | :--- |
| “The latest preparation stopped on an unrelated Platform planning file.” | Read `current-source-blocker.json` in private `/home/administrator/.local/state/hexalith/memories/access-telemetry/resume-spec14-20261021-current-5clz3zwi` | confirmed — recorded source hold; not a retention failure. |
| “The selected test already exists.” | `rg -n 'functional-retention|executionTimeoutSeconds|stopTimeoutSeconds|daprRuntimeVersion' module/hexalith.module-manifest.v2.json`; `rg -n 'run_selected_platform_retention' tools/verify-access-telemetry-lifecycle.py` | confirmed — declared entry, source mode, 14400/900-second limits and selected runner dispatch. |
| “The two-cohort assertions and refusal paths exist.” | `python3 -B -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p 'test_selected*.py' -v` | confirmed — 11 tests passed in 116.390s; offline doubles provide no live proof. |
| “Platform 4.35.1 is installed and supported.” | Platform cwd: `dotnet tool run hexalith-module --version`; read `.config/dotnet-tools.json` and `hexalith-platform.json`; `dotnet tool run hexalith-module -- test --help` | confirmed — version 4.35.1, accepted range `[4.35.1, 5.0.0-0)`, declared command options available; not a runtime pass. |
| “The selected profile pins real Dapr/PostgreSQL and one-hour retention.” | `rg -n 'DaprImage|PostgresImage|retentionSeconds|sslMode' ../platform/src/Hexalith.Platform.Composition/PlatformRetentionProfile.cs` | confirmed — Dapr 1.18.1/PostgreSQL 18.6 image digests, retention 3600, verify-full. |
| “A complete live run passes retention and disposal.” | The active contract's selected `test` command, native TRX, `functional-observations.json` and real Platform terminal result | unverifiable now — no run executed. Owner: Platform/Builds for readiness/disposition, Memories for assertions. Reopen after relevant inputs are frozen and one concrete operational plan is authorized. Not used as justification for this simplification. |

The old source-binding and exact-digest requirements were policy choices, not measured product behavior. Their replacement is explicitly authorized by the owner's decision above, with corresponding current guidance added to the source epics and architecture. No desired retention assertion is weakened to match a test result.

## Handoff and checklist disposition

Moderate process scope, implemented directly under the owner's approval. Developer: use the active contract and supported runner; freeze relevant inputs and produce one concrete plan. Platform/Builds owns environment and actual disposition. Jérôme Piquot owns the operational decision and separately schedules full qualification. No messages to other people or external applications are sent.

| Checklist group | Disposition |
| :--- | :--- |
| 1 — trigger/context/evidence | Done: repeated preparation holds; exact final historical hold retained. |
| 2 — epic/current/future/order impact | Done: existing functional track first; full campaign separately scheduled; no new epic/story. |
| 3 — PRD/architecture/UX/other conflicts | Done: same product requirements/ownership; sequencing notes updated. UX not applicable. |
| 4 — alternatives | Direct adjustment selected. Runtime rollback and MVP reduction rejected: no observed retention defect supports either. |
| 5 — proposal, concrete edits, scope, handoff | Done: before/after above; active documents updated. |
| 6 — review/approval/status/handoff | Owner approval recorded. Sprint stays in progress with an execution-pointer comment. Functional run and full campaign remain pending; neither is claimed complete by this correction. |
