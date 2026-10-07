---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
repository_status: 'reviewed-awaiting-operator'
route: 'dispatch'
baseline_commit: 'ed4528d51279f488a9be81a762fa5522ceb46203'
story_key: '27-4-retention-verification-operations-runbook-and-a41-close-out'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-27-context.md'
  - '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/adoption.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repository producers and the PG2 handoff exist, but accepted C1 evidence and live execution inputs are missing. The canonical offline command omits the five A41 guards.

**Approach:** Correct the offline handoff; reuse existing qualification/close-out guards after accepted prerequisites. Record blockers and retain pending live tasks.

## Boundaries & Constraints

**Always:** Use PG-ONPREM-2 SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`. Require approved/done owners, 25 distinct passed C1 artifacts, PG2 C1.15 renewal, C1.16 linkage, independent Operations/Security approvals, authorized non-Production scope and external custody. Preserve disabled Production writes; restore disabled gate, released Lease and zero lifecycle/clock replicas. A41 closes only after terminal/postflight/remote proof.

**Never:** Credit PG1/fixtures, invent approvals/owners, implement the separate C1.15 renewal, contact unauthorized targets, expose secrets/content, enable Production, or alter protected history/four-path scope. Staging/commits/publication require explicit authorization after review.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Offline | Missing live prerequisites | Both architecture selectors execute; live states remain pending | Record exact blockers |
| Qualification | Accepted PG2 predecessor and scoped target | Immutable C0/C2-C4 packets and cleanup proof | Refuse invalid inputs before enablement |
| Close-out | Accepted C0-C6, terminal and publication authority | Authenticated four-path transition and remote containment | Refuse missing proof or drift |

## Decisions

- 2026-10-06: Administrator approved the recommended offline correction and continuation. Complete the repository handoff and review; leave live qualification/publication tasks pending and Story 27.4 incomplete/A41 open.

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1:27` — PG2 C1.15 refuses execution before target/output creation (asserted at `tests/tooling/access_telemetry_c1/gate_c1_16_test.py:223-235`). Renewal is separately owned. C1.16 is no longer pending: Story 27.22 is done with independently accepted captured identity/full-set linkage (2026-10-06), which grants no Story 27.4, A41 or other-gate credit by itself. 23 gates remain unregistered.
- `tools/verify_access_telemetry_lifecycle.py:2642` — reuse predecessor, producer, terminal, preflight/postflight and publication validators without relaxing gates.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` and `AccessTelemetryA41CloseOutTests.cs` in that directory — twelve retention-decision and five A41 guards; reuse unchanged.
- `docs/operations/access-telemetry-lifecycle.md` — current PG2 producer/close-out procedure already implemented.

## Dev Notes

### Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Original Story 27.4 spec `spec-27-4-retention-verification-operations-runbook-and-a41-close-out.md` (2026-09-02, `awaiting-operator`) | `historical-reference-only` (its oversized eight-loop shape is `anti-template`) | Source of the already-implemented producers, validators, runbook and guards that this spec reruns unchanged. Its loop structure, task density and Release-only verification lane are not reused. |
| Follow-up spec `spec-27-4-retention-verification-operations-runbook-and-a41-close-out-2.md` (2026-09-10, PG-ONPREM-1 operator handoff) | `historical-reference-only` | Dated prerequisite context only. Its PG1 operator actions and OpenBao 2.6.2 requirement are superseded by the 2026-10-04 PG2 adoption and earn no credit. |
| Follow-up spec `spec-27-4-retention-verification-operations-runbook-and-a41-close-out-3.md` (2026-10-04, PG2 offline audit) | `historical-reference-only` | Prior offline prerequisite audit; current blockers are re-derived here and in the canonical evidence matrix. |
| Story 27.3 | `historical-reference-only` | C0 and independent C2/C3/C4 ownership context. Its transferred C1 umbrella is neither reused nor credited. |
| Story 27.21 | `historical-reference-only` | Accepted historical PG1 C1.15 capture; no PG2 renewal credit. |
| Story 27.22 | `historical-reference-only` | Registered done owner of the accepted captured C1.16 identity/full-set linkage; no Story 27.4, A41 or other-gate credit. |
| Story 20.5 and Epic 20 | `historical-reference-only` | Source of `20.5-A41-ACCESS-TELEMETRY-RETENTION`; protected historical `done` records that close-out must not reopen. |
| `tests/tooling/access_telemetry_lifecycle` fixtures and the two architecture guard classes | `current-narrow-pattern` | Re-verified unchanged since `ed4528d5`; rerun as offline behavior evidence only, never as a running-target substitute. |

### Slice Proof

Story 27.4 is the explicitly approved checkpoint-tracking story for C0-C6 and the
A41 close-out (`epics.md` Story 27.4, corrected 2026-09-06 by
`sprint-change-proposal-2026-09-06-story-27-4-live-producer-contract.md`). The
story-scope guard permits one tracking story only when every checkpoint has its own
owner, evidence command or artifact, review state and completion state; the table
below is that record. This spec adds no outcome and does not split the story.

- Repository slice delivered here: the corrected offline handoff (Debug/source
  references, both exact selectors, fail-fast receipt) and its review records. It
  is independently demonstrable by the offline block in the evidence handoff.
- Live slices: C0/C2-C4 execution and the close-out chain stay inside the same
  approved story and remain pending until their prerequisites exist (Tasks 3-4).
  No checkpoint is completed by this spec.
- State authority is the canonical
  [C0-C6 matrix](tests/27-4-retention-verification-evidence.md#canonical-c0-c6-matrix).
  This table mirrors it for per-checkpoint tracking and yields to it on conflict.

| Checkpoint | Owner | Evidence command / artifact | Review state | Completion state |
| :--------- | :---- | :-------------------------- | :----------- | :--------------- |
| C0 exact adapter profile | Platform Operations (Story 27.3 C0 owner) | `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint adapter-profile … --c0-wrapper …` per the lifecycle runbook; offline: lifecycle fixtures | `repository-validated`; no live packet exists to review | open, blocked: no authorized non-Production target or external evidence custody; reopen when that authority is supplied |
| C1 canonical predecessor | C1.1-C1.25 gate owners; Platform Operations and Security approvers | Bundle of 25 distinct passed C1 artifacts validated by `_validate_predecessor` (`tools/verify_access_telemetry_lifecycle.py:2642`) | `operator-pending`; no bundle exists | open, blocked: PG2 C1.15 renewal and 23 unregistered gates; reopen when all 25 pass with two independent same-hash approvals |
| C1.15 PG2 runtime/control-plane identity | Deployment Adapter Developer (separately scoped renewal) | `tools/verify-access-telemetry-c1.ps1:27` refuses PG2 C1.15 before calls or output creation | `operator-pending`; renewal unregistered | open, blocked: renewal scope not approved and outside Story 27.4; reopen on approved renewal plus accepted same-PG2 capture |
| C1.16 component/backend identity | Deployment Adapter Developer (Story 27.22) | Story 27.22 independent acceptance disposition, SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70` | accepted 2026-10-06 by independent review; captured window only | completed 2026-10-06 in Story 27.22; counts toward C1 only inside an approved 25-gate bundle |
| C2 production replacement | Platform Operations | `python3 tools/verify-access-telemetry-lifecycle.py --checkpoint c2-production-replacement --scenario-input …` | `operator-pending` | open, blocked: needs accepted C1, authorized target and evidence root |
| C3 retention and reclamation | Lifecycle owner and adapter owner | Same producer with `--checkpoint c3-retention-reclamation` | `operator-pending` | open, blocked: needs accepted C1, authorized target and purge authority |
| C4 failure, privacy and observability | Platform Operations and Security | Same producer with `--checkpoint c4-failure-privacy-observability` | `operator-pending` | open, blocked: needs accepted C1, authorized target and fault authority |
| C5 operations acceptance | Platform Operations reviewer | Independent same-hash decision over actual C0-C4 packets | `operator-pending` | open, blocked: no C0-C4 packets |
| C6 security acceptance | Security reviewer, different from C5 | Independent same-hash decision over the same packets | `operator-pending` | open, blocked: no C0-C4 packets |
| Terminal validation and A41 close-out | Platform Operations; repository owner for publication | `--checkpoint a41-inventory`, `close-out-preflight`, `close-out-postflight`, `publish-verification` | `operator-pending` | open, blocked: C0-C6 not passed; staging, commit and publication need explicit post-review authority |

### Epic AC Verification

Verified 2026-10-06 against `6f3c727a` plus this worktree. Claims come from this
spec's acceptance criteria and boundaries and from the Story 27.4 and Epic 27 text
in `epics.md`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "twelve retention-decision and five A41 guards pass with no skips" | Quantitative | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class <class> -list methods -noLogo`, ANSI-stripped and filtered to `^Hexalith\.`; run with `-failSkips` in the evidence block | 12 and 5 methods; all Pass, 0 skipped (receipt in Change Log) | `confirmed` |
| "79 lifecycle fixtures" (Implementation Notes) | Quantitative | Command L1 in Change Log | 79 test cases | `confirmed` |
| "Use PG-ONPREM-2 SHA-256 `7f9f6932…`" | Location | `grep -n '^CURRENT_PROFILE_SHA256' tools/verify_access_telemetry_lifecycle.py` | line 259 holds the exact hash | `confirmed` |
| "25 distinct passed C1 artifacts" / "All twenty-five C1 child gates (C1.1-C1.25)" | Quantitative | `sed -n 2638,2639p tools/verify_access_telemetry_lifecycle.py` | `range(1, 26)` yields C1.1-C1.25 | `confirmed` |
| "All twenty-five C1 child gates … are `passed`" | Behavioral | Canonical matrix C1 row | `operator-pending`; no bundle exists | `confirmed` gap in a desired end state; not weakened |
| `tools/verify-access-telemetry-c1.ps1:27` refuses PG2 C1.15 before target/output creation | Location / behavioral | `sed -n 25,29p tools/verify-access-telemetry-c1.ps1`; `tests/tooling/access_telemetry_c1/gate_c1_16_test.py:223-235` | line 27 throws `unsupported-successor-gate-or-historical-opt-in`; fixture asserts no calls, packets or directory | `confirmed` |
| `tools/verify_access_telemetry_lifecycle.py:2642` predecessor validator | Location | `grep -n '^def _validate_predecessor' tools/verify_access_telemetry_lifecycle.py` | line 2642 | `confirmed` |
| "C1.16 linkage/review is pending" (former Code Map) | Behavioral | `grep -n '^  27-22-' _bmad-output/implementation-artifacts/sprint-status.yaml`; Story 27.22 checkpoint row | `27-22-component-and-backend-identity: done`; C1.16 accepted 2026-10-06 | `corrected` in this spec's Code Map and the evidence handoff |
| "23 gates remain unregistered" | Quantitative | `grep -c -e '^  27-2[1-9]-' -e '^  27-3[01]-' _bmad-output/implementation-artifacts/sprint-status.yaml` | 2 registered successors (27.21, 27.22), so 23 of 25 unregistered | `confirmed` |
| "Story 27.3 is `done`" | Existence | `grep -n '^  27-3-' _bmad-output/implementation-artifacts/sprint-status.yaml` | `done` | `confirmed` |
| "C0 and C2-C4 complete against the immutable `PG-ONPREM-1` profile" | Behavioral | `grep -n 'Earlier PG1-only' _bmad-output/planning-artifacts/epics.md`; canonical matrix | Current profile is PG-ONPREM-2; PG1 evidence earns no successor credit; C2-C4 are `operator-pending` | `corrected`; planning correction already present as the dated 2026-10-04 note at `epics.md:5192-5204` |
| "Story 27.21 is currently the only registered successor and is `in-progress`" | Existence | `grep -nE '^  27-2[12]-' _bmad-output/implementation-artifacts/sprint-status.yaml` | 27.21 and 27.22 are registered and `done` | `corrected`; the dated 2026-10-04 note records 27.21 done and 27.22 registration, and Story 27.22's 2026-10-06 completion is deferred to the Epic 27 planning sync (deferred-work.md) because `epics.md` is a protected close-out path |
| "at least two Server writers" | Quantitative | `grep -n 'writer_count=2' tools/verify_access_telemetry_lifecycle.py` | `ADR_TWO_WRITER_WORKLOAD` pins `writer_count=2`; live run absent | `confirmed` contract; live proof is a desired end state |
| Preserve disabled Production writes and zero lifecycle/clock replicas | Behavioral | `grep -c 'replicas: 0' deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` | 2 | `confirmed` |
| `20.5-A41-ACCESS-TELEMETRY-RETENTION` is still `carried-forward` with its sprint action `open` | Existence | `grep -n 'ID: 20.5-A41-ACCESS-TELEMETRY-RETENTION - Status: carried-forward' _bmad-output/implementation-artifacts/deferred-work.md`; `grep -n 'Keep 20.5-A41' -A2 _bmad-output/implementation-artifacts/sprint-status.yaml` | carried-forward; action `status: open` | `confirmed` precondition; closure is a desired end state |
| "Epic 20/Story 20.5 remain historical `done`" | Existence | `grep -n -e '^  epic-20:' -e '^  20-5-[a-z-]*:' _bmad-output/implementation-artifacts/sprint-status.yaml` | both `done` | `confirmed` |
| Runbook identifies owner, configuration, monitoring, incidents, recovery, rollback, RPO/RTO and decommissioning | Existence | `grep -n '^## ' docs/operations/access-telemetry-lifecycle.md` | sections for ownership, configuration, monitoring, incident response, rollback and RPO/RTO, decommissioning | `confirmed` for documentation; C5 acceptance is pending |

### Remediation Runtime Checklist

remediation runtime checklist: not applicable — this spec's changes are Markdown
evidence, spec and deferred-work records only; no workflow/runtime dispatch,
registration, cleanup, dedup or rollback behavior changed. The live tasks reuse the
existing C2-C4 producers unchanged; their disabled-gate, released-Lease and
zero-replica restoration is covered by the existing
`test_producer_restores_disabled_state_when_*` fixtures. Re-derive applicability if
a later task changes producer or close-out code.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` — change the offline command to Debug/source references and both exact architecture selectors below. Record counts/blockers; preserve all pending matrix states.
- [x] `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — run existing scenario/denial/cleanup/close-out fixtures and both architecture classes; no new tests for this command correction.
- [ ] `tools/verify-access-telemetry-lifecycle.py` — after accepted prerequisites/authority, run existing C0/C2-C4 producers and retain cleanup/evidence packets. Otherwise leave pending.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md` — after independent C5/C6 and terminal validation, prepare exact four-path close-out; stage/commit/publish only with explicit authority after review. Require postflight/remote containment; otherwise leave pending. The separate sprint action follows verified publication.

**Acceptance Criteria:**
- Given the offline handoff, when its commands run, then twelve retention-decision and five A41 guards pass with no skips while live states remain pending.
- Given missing, historical or unaccepted prerequisites, when qualification is evaluated, then exact blockers are recorded without target operations or A41 mutation.
- Given accepted PG2 evidence and publication authority, when close-out runs, then cleanup, C0-C6, terminal, exact postflight and remote containment pass before A41 resolves.

### Review Findings

Second-pass code review, 2026-10-06, of `b7a4377a..95a38fd8` (byte-identical to the receipt's `final-worktree.diff`, SHA-256 `51bce7acc36bf9e3d8fd09b79d581ff8e49c405ec72b54ddf9bed3392510b570`). Layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor, historical-slice-guard, story-phase-ledger; none failed. Readiness gate `python3 tools/check-story-review-readiness.py --story-key 27-4-retention-verification-operations-runbook-and-a41-close-out` exit 0, final line `Artifact carries no ledger, File List, or evidence table; check is a no-op.` (resolved the unnumbered spec); the `spec-…-close-out-4` key form gives the same no-op line. Slice gate `python3 tools/check-story-slice-scope.py --changed-files-file <b7a4377a..95a38fd8 names> --require-record` exit 1 (three violations).

- [x] [Review][Decision] No phase ledger, Test-count evidence or File List exists for Story 27.4 and the readiness gate is vacuous — None of the four 27.4 specs carries `## Change Log` or `## File List`, so the create, dev and earlier code-review phases have no rows, the architecture lane change (Release `RetentionDecisionTests` only -> Debug/source-reference both classes, `0 -> 12` newly tracked scope) has no same-unit mapping, and both reviewed paths are unreconciled (`matched 0/2`). The gate resolves sprint key `27-4-…` only to `<key>.md`/`spec-<key>.md` (the original 2026-09-02 spec), so every follow-up spec is never gate-checked by story key. Choose where the canonical ledger lives: (1) adopt it in this spec with a `create-story` adoption baseline row, dev-story row, the earlier offline code-review row and this review's row, plus a File List (recommended); (2) adopt it in the original unnumbered spec the gate resolves (its `awaiting-operator` status would then fail gate C3); or (3) record an accepted gap with owner/consequence/reopen trigger until the build route is wired to the ledger policy. Resolved 2026-10-06 by Administrator: option 1 — adopt the canonical ledger and File List in this spec; converted to the Patch item below.
- [x] [Review][Patch] Adopt the canonical phase ledger (create-story adoption baseline, dev-story, earlier offline code-review and this review's code-review rows) and cumulative File List in this spec [spec-27-4-…-close-out-4.md:64]
- [x] [Review][Patch] Spec fails story-slice-scope: no Historical Context Classification, Slice Proof or per-checkpoint evidence table [spec-27-4-…-close-out-4.md:1]
- [x] [Review][Patch] C1.16 blocker is stale — Story 27.22 is done with independently accepted C1.16 identity/full-set linkage [tests/27-4-retention-verification-evidence.md:42]
- [x] [Review][Patch] Spec Verification keeps the weak architecture command that BH2 rejected (no `-failSkips`, no XML count check, not fail-fast) [spec-27-4-…-close-out-4.md:134]
- [x] [Review][Patch] Epic AC Verification section is missing [spec-27-4-…-close-out-4.md:44]
- [x] [Review][Patch] `baseline_commit` was overwritten (`ed4528d5` -> `b7a4377a`) against bmad-build step-03's never-overwrite rule, dropping the planning commit's `epic-27-context.md` recompile from both reviews [spec-27-4-…-close-out-4.md:8]
- [x] [Review][Patch] Evidence blocker list omits credential-file paths and fault/purge authority named by the spec [tests/27-4-retention-verification-evidence.md:52]
- [x] [Review][Patch] Dated "Planning checks" line was rewritten in place, dropping C1.16 19/19 and the kubectl render result and crediting implementation-stage results to planning [spec-27-4-…-close-out-4.md:139]
- [x] [Review][Patch] Receipts are cited only by `/tmp` path, with no inline identifiers, and two post-run files (`final-a41.xml`, `final-worktree.diff`) are undisclosed [tests/27-4-retention-verification-evidence.md:153]
- [x] [Review][Patch] Fail-fast claim is broader than its receipt — the focused receipt covers only the XML validator ("Lifecycle and build were not rerun") [spec-27-4-…-close-out-4.md:116]
- [x] [Review][Patch] Remediation runtime checklist applicability is not stated [spec-27-4-…-close-out-4.md:64]
- [x] [Review][Defer] `sprint-status.yaml:165` epic-27 order reason still says C1.16 capture/linkage/acceptance pending while `:466` marks 27-22 done (anchor corrected 2026-10-07 from `:473`; the matching `deferred-work.md` entry keeps the original text) [sprint-status.yaml:165] — deferred: pre-existing, owned by the Story 27.22 sync; this spec forbids 27.4 sprint-status edits
- [x] [Review][Defer] Planning-commit `epic-27-context.md` recompile dropped Technical Decisions (Dapr-only typed-state/actor/reminders, millisecond logical timestamps and one-second attestation bound, writer/key-rotation barriers, separate clock/adapter authorities and secret-store prefixes, the 250-events/s envelope) [epic-27-context.md:24] — deferred: medium (unverified), agent-context file; settle by diffing `ed4528d5:_bmad-output/implementation-artifacts/epic-27-context.md` against ADR-27.1-001 and the architecture spine
- [x] [Review][Defer] bmad-build has no `_bmad/custom/bmad-build*.toml`, so the scope-guard, phase-ledger, epic-AC-verification and runtime-checklist policies never load on the route that authored every 27.4 spec [_bmad/custom/] — deferred: fix edits customization files; root cause of the Decision and four Patch items above
- [x] [Review][Defer] `check-story-review-readiness.py` resolves a sprint key only to `<key>.md` or `spec-<key>.md`, so numbered follow-up specs are never checked by story key [tools/check-story-review-readiness.py:771] — deferred: pre-existing tooling limitation

**Rejected:**

- `false` — build zero-warning claim unproven by an incremental build: `TreatWarningsAsErrors=true` (`Directory.Build.props:7`) means the compile that produced the DLL could not have emitted warnings, and this Markdown-only diff changed no C# source.
- `low` — dropped `-p:NuGetAudit=false`/MinVer pins: only a fresh restore with no network or a new advisory triggers it; the baseline instructions add those pins only "when needed", so the fix is a security trade-off, not a direct correction.
- `low` — lifecycle unittest count/skips unasserted: two layers verified no lifecycle test has a skip path and all 79 plus every named fixture appear in the receipt log; the fix adds a guard.
- `false` — 17/17 never ran against the committed text: an independent rerun of both classes against the committed documents returned 17 total, 0 failed/skipped, and the doc already scopes the receipt to the execution-time tree.
- `low` — receipt omits untracked files, submodule-internal edits and `project.assets.json`: unlikely in this flow and the fix adds capture steps.
- `low` — `git diff --check` ignores staged/untracked changes: pre-existing command; this flow never stages; the fix adds steps.
- `low` — `repository_status` is unrecognized and `in-progress` resumes into step-03: no recognized bmad-build status expresses repository-complete/awaiting-operator, and the remaining tasks self-gate on authority, so re-entry is harmless.
- `false` — commit made without recorded authorization and text says nothing was committed: `95a38fd8` was authored by the repository owner, the human authority the spec requires, and the statements are scoped to the workflow session.
- `rejected` — new frozen Decision dropped the old Open Question's input list: the fix edits frozen intent; the inputs survive in Implementation Notes and Never already forbids exposing secrets.
- `false` — triage table "Verdict" column holds severities: that is the bmad-build triage-log format.
- `false` — double blank line before "Final review resolution": no Markdown linter covers `_bmad-output` and rendering is unaffected.
- `false` — "run from repository root" not enforced: relative paths fail loudly under `set -euo pipefail`.
- `low` — receipt records `python3 -` without the heredoc body: the validator is in the committed document; the fix adds steps.
- `low` — source state captured once, before the build: concurrent worktree mutation mid-run is unlikely; the fix adds a guard.
- `false` — qualification blockers are hand-written without CLI proof: lifecycle fixtures run the producers via subprocess (`test_retention_verification.py:12`, `:689`, `:1380`), and the C1.15/PG2 refusal before calls and directory creation is asserted at `tests/tooling/access_telemetry_c1/gate_c1_16_test.py:223-235`.
- `false` — whole-story anti-template copy from earlier specs: the repeated live tasks are Story 27.4's own approved remaining scope (`epics.md:5291`, 2026-09-06 proposal), not a historical template; the hidden-slice concern is real and is carried by the per-checkpoint table patch above.

**Review record (for the code-review ledger row, pending patch item for ledger adoption):** Administrator chose to leave all 11 patches as action items on 2026-10-06; no patch was applied. Comparable discovery after review: lifecycle 79 unittest test cases, `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest; l=unittest.TestLoader(); s=l.discover('tests/tooling/access_telemetry_lifecycle', pattern='test_*.py'); print(s.countTestCases()); assert not l.errors, l.errors"`; architecture 12 `AccessTelemetryRetentionDecisionTests` + 5 `AccessTelemetryA41CloseOutTests` xUnit v3 test cases, all Pass, 0 skipped/not run, `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml <file>` (assembly SHA-256 `c14d72222e20f4964e9fe928189f143ae2328cda3ac1a558889815cc54932700`, unchanged C# since `b7a4377a`). Review phase delta +0 on both lanes. Review-touched paths: this spec and `_bmad-output/implementation-artifacts/deferred-work.md` (four deferred entries). The code-review ledger row cannot be appended until the ledger exists; `done` stays blocked by the open patch items, the missing ledger/File List, the missing Epic AC Verification table and the failing slice gate. Remediation runtime checklist re-derived from the diff: not applicable (Markdown-only; no dispatch, registration, cleanup, dedup or rollback behavior changed).

## Implementation Notes

### 2026-10-06 approved offline implementation

The canonical evidence handoff now builds Debug with source references and runs
both exact architecture selectors. It records 79 lifecycle fixtures and 17
architecture guards, all passing with no skips, plus a build with zero warnings
and errors. No production code or new tests were required. The C0-C6, identity
and close-out tables are unchanged. No workspace staging, commit, publication or
live target operation occurred.

Matrix audit: the offline row is covered by the two architecture classes (12 + 5
passing guards); qualification is covered by
`test_complete_c2_c3_and_c4_packets_validate`,
`test_c1_requires_unique_25_gate_artifacts_disabled_production_and_authorization`,
`test_current_target_predecessor_approvals_and_c0_reject_old_or_mixed_evidence`,
`test_producer_refuses_unverifiable_target_identity_without_mutating_target`, and
the two `test_producer_restores_disabled_state_when_*` fixtures; close-out is
covered by `test_registered_producers_and_complete_close_out_chain` and
`test_mutation_manifest_is_exact_and_verifier_owned`. All ran in the passing
79-test suite. These fixtures establish offline behavior only.

The last two tasks remain conditional and unchecked under the approved offline
scope. PG2 C1.15 renewal, C1.16 capture/linkage/review, 23 unregistered gates,
accepted external predecessor and target/evidence/credential-file/fault authority
are absent. No current-profile C0-C6/terminal/remote chain is available. Story
27.4 therefore remains incomplete/in-progress; A41 and its sprint action remain
open, and Production lifecycle writes remain disabled. The live acceptance
criterion has not been exercised.

### 2026-10-06 second-pass review patch application

All eleven second-pass patches are applied in the spec and the evidence handoff;
no production code, test, verifier, runbook, `epics.md` or `sprint-status.yaml`
byte changed. The paragraph above is kept as dated history, with one correction:
C1.16 is no longer absent. Story 27.22 is done and its independent disposition,
SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`,
accepts captured identity and full-set linkage for one closed window. It gives no
Story 27.4, A41, Production or other-gate credit until it is part of an approved
25-gate C1 bundle.

Frontmatter `baseline_commit` is restored to the planning baseline `ed4528d5`.
The planning commit `b7a4377a` and its `epic-27-context.md` recompile are now in the
story's cumulative scope and File List. Verifying that recompile's dropped
Technical Decisions stays with its existing deferred-work entry.

Current blockers, re-derived: the separately owned PG2 C1.15 renewal; 23
unregistered gates; no accepted current-profile C1 predecessor bundle with
independent named Platform Operations and Security approvals; and no authorized
non-Production kube context and namespace, external evidence root and custody,
credential-file paths, or fault and purge authority. The last two tasks stay
unchecked. Story 27.4 stays in progress, A41 and its sprint action stay open, and
Production lifecycle writes stay disabled. No target was contacted.

## Spec Change Log

- 2026-10-06, second-pass review patch application: restored frontmatter
  `baseline_commit` from `b7a4377a` to the original `ed4528d5`, following the
  bmad-build step-03 rule that a captured baseline is never overwritten. Added the
  non-frozen Dev Notes, Change Log and File List sections and corrected the Code Map
  C1.16 line. The frozen intent block and the task definitions are unchanged.
- 2026-10-07, post-review correction approved by the Administrator: fixed the
  `\|`-escaped Epic AC grep commands, the `sprint-status.yaml:473` anchor, the File
  List's shared-path attribution and the Verification outcome wording (triage rows
  PB1, PB4, PB5, PB11). Frozen intent, tasks and status are unchanged.

## Review Triage Log

### 2026-10-06 offline review

All three layers completed: edge-case review returned no findings after an
independent 79/79 + 17/17 run; verification-gap review reported no gaps.
Blind-hunter findings were verified individually before grouping:

| Finding | Verdict | Route | Evidence / disposition |
| :------ | :------ | :---- | :--------------------- |
| BH1: commands continue after failure | medium | patch | The Bash block lacks failure handling; a source-reference build failure can fall through to a stale DLL and the final successful whitespace command. Make the scoped block fail immediately. |
| BH2: success can mean zero tests or skipped guards | medium | patch | Runner help confirms skips are accepted by default; the reviewer executed an unmatched selector returning success with zero tests. Add failSkips and validate both per-class counts/pass states from the actual result report. |
| BH3: dated claims have no retained run/source receipt | low | patch | Only summarized output was retained in prose. Preserve local command logs/result XML and source/assembly/dependency identity alongside the offline check; do not grant live gate credit. |
| BH4: source-reference setup prerequisite omitted | low | patch | Directory.Build.props CheckSubmodules rejects missing root dependencies when UseHexalithProjectReferences=true. Add the README setup link and nonrecursive root-only guidance. |

The four findings have distinct root causes; each receives a direct correction
within the canonical evidence handoff. No intent gap or spec loopback is needed.


Final review resolution: all four BH patches are implemented in the evidence
handoff. Its scoped fail-fast execution, failSkips plus exact XML counts, local
run/source receipts and setup link passed focused positive/negative checks.
Parent executed the complete revised block successfully: 79 lifecycle tests,
17 architecture guards with exact 12/5 counts and all Pass, zero build warnings
or errors, and clean whitespace. All ten logged commands exited zero. Receipt:
`/tmp/story-27-4-offline.4PZnqjZw`; focused refusal receipts:
`/tmp/story-27-4-focused.EGvAOLs8`. The XML, baseline revision, built-assembly hash
and preserved matrix tables were checked independently. Review has no unresolved
findings within the approved offline scope; nothing was added to deferred-work.

Correction, 2026-10-06 (second-pass review patch): the focused receipt above is
narrower than the preceding paragraph says. Its `scope.txt` reads "Focused review-fix
verification only: two architecture classes and XML validation. Lifecycle and build
were not rerun." It reran both architecture classes (17 Pass) and six XML-validator
refusals (missing report, zero tests, wrong count, extra class, failed result,
skipped result). It did not test the block's fail-fast trap. A separate local folder,
`/tmp/story-27-4-failure-check.rnIsIZAz`, holds only a minimal harness: a substituted
`bash -c 'exit 17'` command exited 17 and nothing ran after it. That harness was not
the documented block. The documented block's own stop on the first failing command
was first exercised on 2026-10-06 during the patch application; the Change Log
records that receipt.

The repository portion is reviewed-awaiting-operator. Full spec/story completion
and sprint advancement remain withheld because the live tasks are still pending.
The workflow's default done/local-commit actions do not apply to this approved
partial scope: the frozen decision keeps 27.4 incomplete and requires separate
post-review authorization for staging/committing/publication. No such action ran.

### 2026-10-06 patch-application review

Review of the File List paths' cumulative diff since `ed4528d5` (spec, evidence
handoff, `deferred-work.md`, `epic-27-context.md`; 710 lines). The raw range also
holds Story 27.22 commits and dependency gitlinks, which have their own owners and
reviews. All three layers completed: blind-hunter (17), edge-case-hunter (15),
verification-gap (1 gap, 1 other). Prior dispositions from the second-pass review's
Rejected list were carried where location and claim matched. Under the step-04 rule,
findings whose only fix edits this spec were rejected even when real; the
presentation lists them.

| Finding | Verdict | Route | Evidence / disposition |
| :------ | :------ | :---- | :--------------------- |
| PB1/PE15: File List credits Story 27.22 edits in shared paths; "matched 4/4" overstated | low | reject | Real: `f5beeed5`/`b969cf32` changed `epic-27-context.md`; `f5beeed5`/`8b9d87ab`/`b969cf32` changed `deferred-work.md`. Fix edits this spec's File List/ledger. |
| PB2: epic-context deferral mis-graded, lacks owner/trigger | carried medium | defer | carried: same recompile-loss claim as the logged `[Review][Defer]` item; not re-deferred. ADR still holds the decisions (`adr-27.1-001…:133`, `:166`, `:235`). |
| PB3/PE10/PE11/PE12: recompile also dropped other content | medium | defer | The `epic-27-context.md` diff removes "Search, ingestion, mutation, and rejected access", "malformed" fail-closed scope, startup fail-closed qualification, the short-expiry crossing, the PostgreSQL pod/process zero-loss bound and Dapr-only sentences. The existing deferral does not list them. Step-01 loads a valid epic context instead of raw planning docs. Agent-context file: new deferred entry. |
| PB4: Epic AC grep commands use `\|` in table cells | low | reject | Real: the raw `grep -cE '^  27-(2[1-9]\|3[01])-'` prints 0, while `-e '^  27-2[1-9]-' -e '^  27-3[01]-'` prints 2. Same for the `epic-20` row. Fix edits this spec. |
| PB5/PE14: `sprint-status.yaml:473` anchor is wrong | low | reject | Real: the 27-22 row is at line 466 in HEAD and `6f3c727a`; line 473 is a comment. Fix edits this spec and an existing deferred-work entry. |
| PB6: Review record paragraph lacks a later-state note | low | reject | Superseded by the dated patch-application notes and ledger row. Fix edits this spec. |
| PB7: patch application unreviewed; loop counter; status | false | reject | This pass reviews it. `review_loop_iteration` counts loopbacks only, and none ran. `in-review` is the step-04 status. |
| PB8: Slice Proof C1 row names nonexistent owners | low | reject | The cell names a role class beside "23 unregistered gates". Fix edits this spec. |
| PB9: disabled-writes AC row weak; `disabled-pending-story-27-3` stale | low | reject | Row: fix edits this spec. The label is pinned by `AccessTelemetryA41CloseOutTests.cs` and `AccessTelemetryLifecycleIntegrationCheckpointTests.cs`, so changing it is more than a direct correction; it is a deliberate marker, and few readers will meet it. |
| PB10: Epic AC Verification omits AC2/AC3 and some Always claims | low | reject | Fix edits this spec. |
| PB11/PE7: Verification states outcomes the block does not assert | low | reject | Lifecycle count/skips: carried rejected `low`. Build: `NU1901`-`NU1904` are deliberately non-blocking (`Directory.Build.props:18`), so the block follows repository policy and only the spec wording overstates. Fix edits this spec. |
| PB12: review-time build identity ignores dependency moves | low | reject | The ledger cell is labelled "build identity only". Fix edits this spec. |
| PB13: planning-sync deferrals lack a pre-close-out deadline | false | reject | `A41_PROTECTED_PATHS` are read-only inputs to the close-out manifest (`tools/verify_access_telemetry_lifecycle.py:2105-2119`). A separate planning sync can still edit them, and close-out never edits them. |
| PB14: new deferred entry skips DW schema | false | reject | It uses step-04's prescribed `source_spec`/`summary`/`evidence` format, like its neighbours. |
| PB15: Story 27.22 deferred entries lack spaces ("Story27.4") | low | defer | Real: 6 `Story27.` hits. Pre-existing Story 27.22 text that this story must not modify. |
| PB16: receipt gaps (ephemeral `/tmp`, unhashed exits, uncited folder) | low | reject | Identifiers are inline since the second-pass patch. `/tmp/story-27-4-document-check-i34f8bny` is an uncited scratch folder, not a receipt. More capture adds steps and is unlikely to be needed. |
| PB17: Story 27.4 commits lack `Story:` trailers; subjects mislead | low | reject | The commits are owner-authored history; fixing needs a history rewrite (frozen Never) or a spec edit. |
| PE1: sibling `../X` checkout satisfies `CheckSubmodules` | low | reject | `Directory.Build.props:95` accepts siblings, but root submodules are initialized here, the receipt records `git submodule status`, and the fix adds a guard. |
| PE2: untracked or submodule-internal edits missing from the receipt | low | reject | carried: second-pass rejected `low`. |
| PE3: lifecycle skips or zero discovery not asserted | low | reject | carried: second-pass rejected `low`. |
| PE4: heredoc validator body not in `.command` | low | reject | carried: second-pass rejected `low`. |
| PE5: `git diff --check` ignores staged/untracked | low | reject | carried: second-pass rejected `low`. |
| PE6: no timeouts on lifecycle/build/runner | low | reject | No hang was observed; the fix adds guards. |
| PE8: `-p:NuGetAudit=false` removed | low | reject | carried: second-pass rejected `low`. |
| PE9: `--disable-build-servers /nr:false` removed | false | reject | Three documented-block runs built successfully without them, and later rebuilds worked. Lingering build servers are default dotnet behavior; no lock failure was shown. |
| PE13: 27.2x/3x key range misses differently keyed successors | low | reject | Correct today. Fix edits this spec. |
| PV1 (gap): no CI test runs the offline block's fail-fast/count/Pass checks | low | reject | Pre-verified. A regression needs a future edit to the Markdown block, and the fix is a new extraction harness. Filed `defer` weighed, but defer is unavailable for this story's own block. |
| PV2: readiness gate rejects bmad-build `in-review` (C3) and skips C1 | medium | defer | Reproduced: exit 1, `C3: status 'in-review' is not one of backlog, ready-for-dev, in-progress, review, done.` (`tools/check-story-review-readiness.py:83`). Pre-existing vocabulary mismatch. |

No intent_gap, bad_spec or patch entry survived, so no loopback ran. Three deferred
entries were added. The second-pass ledger row's readiness result was recorded while
the status was `in-progress`.

Post-review correction, 2026-10-07: after the presentation, the Administrator
approved fixing four spec-hosted findings that the review had rejected only because
the fix edits this spec. PB4: both Epic AC commands now use `-e` alternatives
without `|`, printing 2 and the two `done` rows. PB5/PE14: the `[Review][Defer]`
anchor is now `:466`. PB1/PE15: the File List gained a Shared Paths note.
PB11/PE7: the Verification lifecycle and build lines now state what the block
asserts and what must be checked in the logs. The other rejected rows are
unchanged.

## Verification

Run the scoped block in
[the evidence handoff](tests/27-4-retention-verification-evidence.md#offline-repository-verification)
from the repository root. It is the canonical verification. It stops at the first
failing command or pipeline and keeps every command, exit code, log, result XML and
source identity in a fresh local folder. Its commands, in order:

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — must exit 0. The block does not assert the count or skips, so confirm `Ran 79 tests` and a bare `OK` (no `skipped=`) in `lifecycle.stderr.log`.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` — must exit 0. `NU1901`-`NU1904` audit warnings stay non-blocking by repository policy (`Directory.Build.props:18`), so confirm `0 Warning(s)` and `0 Error(s)` in `build.stdout.log`.
- `env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo -failSkips -result-xml "$verification_dir/architecture.xml"` — any skip fails the run.
- The block's embedded XML validator — exactly 12 `AccessTelemetryRetentionDecisionTests` and 5 `AccessTelemetryA41CloseOutTests` results, every result `Pass`; a missing report, zero tests, wrong per-class counts or a non-Pass result exits nonzero.
- `git diff --check` — clean whitespace.

Planning checks, 2026-10-06: lifecycle 79/79, C1.16 19/19, Debug build 0 warnings/errors, A41 5/5; render also passed with installed kubectl v1.36.1. PG2 C1.15 is unsupported, C1.16 remains pending, and 23 gates are held/unregistered. Accepted external predecessor/target authority was not supplied.

Implementation-stage checks, 2026-10-06 (dev-story and first offline code-review;
moved here from the planning line, which is restored verbatim above): lifecycle
79/79, Debug/source-reference build zero warnings/errors, architecture 17/17 with no
skips/failures. No external predecessor or target authority supplied; no live
operations/A41 mutation/publication. Frozen intent preserved. The recommended
offline scope was approved by Administrator on 2026-10-06.

Later state, 2026-10-06: the planning line's "C1.16 remains pending" is
historical. Story 27.22 completed C1.16 later that day (see Implementation Notes).
The post-patch results are in the last Change Log row.

## Change Log

Discovery commands. Every row uses the same runner, scope, filters and
configuration unless it names a mapping.

- `L1`, lifecycle unittest test cases: `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -c "import unittest,hashlib; l=unittest.TestLoader(); s=l.discover('tests/tooling/access_telemetry_lifecycle', pattern='test_*.py'); f=lambda x: [i for t in x for i in (f(t) if isinstance(t, unittest.TestSuite) else [t.id()])]; ids=sorted(set(f(s))); assert not l.errors, l.errors; print(len(ids), hashlib.sha256(''.join(i+'\n' for i in ids).encode()).hexdigest())"` prints the count and the sorted test-id SHA-256.
- `L2a`, xUnit v3 test methods of `AccessTelemetryRetentionDecisionTests` in the Debug/source-reference build: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -list methods -noLogo | sed -e 's/\x1b\[[0-9;]*m//g' | grep '^Hexalith\.' | LC_ALL=C sort -u`, counted with `wc -l` and hashed with `sha256sum`.
- `L2b`, the same command for `AccessTelemetryA41CloseOutTests`.
- Test-source identity: `git diff --quiet ed4528d5 HEAD -- tests/tooling/access_telemetry_lifecycle tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs` exits 0, and the worktree has no change under those paths. Every row therefore counts the same test inventory.

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-06 | create-story | Adoption baseline. `story-phase-ledger.md` adopted for this spec on 2026-10-06 by the Administrator's second-pass review decision (option 1); owner: the Story 27.4 developer acting under that decision. Create work is the planning commit `b7a4377a` (this spec plus the `epic-27-context.md` recompile) on baseline `ed4528d5`. Earlier deltas are not reconstructed. No tests planned: this is a command correction. | Phase delta +0; cumulative +0. Baseline totals: L1 79 test cases (sorted-id SHA-256 `cd958099604e922418aae47b478a68742e3ab11599bcb909117e4418605af9df`); L2b 5 test methods (method-set `c490ea56a80ca0450884c9a49c9d079b7a6c1e853c55f69280d42c4a654306f1`) in the planning Debug lane; L2a 12 test methods (method-set `0bdae30ac4db01cba1d005b46a381bc24fb6a4d8ed32ca32c8e17a615ba2fed0`) tracked only by the then-canonical Release/package-reference handoff, 0 in the Debug lane. Measured 2026-10-06 on test sources unchanged since `ed4528d5` (test-source identity command exits 0); the planning line recorded lifecycle 79/79 and A41 5/5. | matched 2/2 against `ed4528d5`: `git diff --name-status ed4528d5 b7a4377a` lists A this spec and M `epic-27-context.md`. |
| 2026-10-06 | dev-story | Approved offline correction: the canonical handoff moved from Release/package-reference `AccessTelemetryRetentionDecisionTests` only to Debug/source-reference with both exact selectors. Counts and blockers recorded; matrix states preserved; no code or tests added. Tasks 1-2 checked; Tasks 3-4 left pending. | Phase delta +0 tests; cumulative +0. L1 79 -> 79. L2b 5 -> 5, lane remapped from Debug/package-reference to Debug/source-reference with the same class source. L2a: the Debug/source-reference lane goes `0 -> 12` as newly tracked scope, mapped from the Release handoff lane's same 12-method class. That is a tracked-scope transition, not an added test. This phase's runner output was streamed, not retained (evidence handoff, "Initial offline execution"); the next row's receipt re-derives the same totals. | matched 3/3 against `ed4528d5`: `git diff --name-status ed4528d5 95a38fd8` lists this spec, `epic-27-context.md` and `tests/27-4-retention-verification-evidence.md`. |
| 2026-10-06 | code-review | First offline review (blind-hunter, edge-case, verification-gap). Four patches, BH1-BH4, applied to the evidence handoff: fail-fast scoped block, `-failSkips` plus exact 12/5 XML validation, local receipts, setup link. Nothing deferred. Committed with the dev work in `95a38fd8` by the repository owner. | Phase delta +0; cumulative +0. L1 79 -> 79; L2a 12 -> 12; L2b 5 -> 5. Receipt `/tmp/story-27-4-offline.4PZnqjZw`: `Ran 79 tests`, `OK`; `architecture.xml` SHA-256 `f86ffcc3b3b64b4e3ab038b9c6e2b9bc1372a640a9722f38e8bbea67d46a8e3c` with 12 + 5 results, all Pass, 0 skipped or not run; ten exit codes 0. | matched 3/3 against `ed4528d5`: same command and paths as the dev-story row; the review changed only this spec and the evidence handoff. |
| 2026-10-06 | code-review | Second-pass review of `b7a4377a..95a38fd8` (diff SHA-256 `51bce7acc36bf9e3d8fd09b79d581ff8e49c405ec72b54ddf9bed3392510b570`), six layers: one decision (resolved, option 1), 11 patches left as action items, 4 deferrals added to `deferred-work.md`, 16 rejected. No patch applied in this row. Committed in `6f3c727a`, which also carries unrelated dependency gitlinks. | Phase delta +0; cumulative +0. L1 79 (from the Review record); L2a 12 and L2b 5, all Pass with `-failSkips` (assembly SHA-256 `c14d72222e20f4964e9fe928189f143ae2328cda3ac1a558889815cc54932700`, build identity only). | matched 4/4 against `ed4528d5`: raw `git diff --name-status ed4528d5 6f3c727a` lists 21 paths. Four are Story 27.4 paths: this spec, `epic-27-context.md`, the evidence handoff and `deferred-work.md`. The other 17 are the named File List Exclusions. |
| 2026-10-06 | code-review | Review-patch application. All 11 second-pass patches applied: ledger and File List adopted; Historical Context Classification, Slice Proof and per-checkpoint table added; C1.16 blocker re-derived; Verification strengthened; Epic AC Verification added; `baseline_commit` restored to `ed4528d5`; missing live inputs listed; planning line restored; receipt identifiers and post-run files disclosed; fail-fast claim narrowed and the documented block's fail-fast exercised; runtime checklist marked not applicable. One deferral added (Epic 27 `epics.md` currency). Tasks 3-4 stay pending. | Phase delta +0; cumulative +0. L1 79 -> 79 (`cd958099…`); L2a 12 -> 12 (`0bdae30a…`); L2b 5 -> 5 (`c490ea56…`). Post-patch receipt `/tmp/story-27-4-offline.al6C55hg` (revision `6f3c727a` plus the patched worktree): `Ran 79 tests`, `OK`; `architecture.xml` SHA-256 `240fd6244aa6982782057f25e685cf6ae58c57b2b0db0c133de83420f707c122` with 12 + 5 results, all Pass, 0 skipped or not run; build 0 warnings, 0 errors; ten exit codes 0. Fail-fast receipt `/tmp/story-27-4-failfast.njdCm7qY`: with the lifecycle command replaced by `false`, the block exits 1 and nothing runs after it. Post-record rerun of both classes against the final evidence handoff (file SHA-256 `5191682780d8c3b10c3281385da879b1c1ae4f7e14fd0e453aafaa1166f08255`), receipt `/tmp/story-27-4-postrecord.D2nYnKfs`: `architecture.xml` SHA-256 `66491b57a54a89c7cb18eba87a2edef659c2a631c69803aab5a290ed09bb97c9`, 17 = 12 + 5, all Pass, 0 skipped or not run; exit 0. | matched 4/4 against `ed4528d5`: `git diff --name-status ed4528d5` (HEAD `6f3c727a` plus the worktree) lists 21 paths: the same 4 Story 27.4 paths and the 17 named exclusions. Readiness: `python3 tools/check-story-review-readiness.py --story-key spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4 --changed-files-file <raw 21 paths>` exits 0 with final line `Story review readiness validation passed.`; adding one unlisted probe path makes it exit 1. Slice: `python3 tools/check-story-slice-scope.py --changed-files-file <b7a4377a..95a38fd8 names> --require-record` exits 0 with final line `story-slice-scope: OK - 1 story file(s) checked: _bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`. On the raw 21 paths it exits 1 only for Story 27.22's `spec-27-22-component-and-backend-identity.md`, a pre-existing out-of-scope result that is identical without this spec. |

## File List

- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
- `_bmad-output/implementation-artifacts/epic-27-context.md`
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

### Shared Paths

Two File List paths also carry Story 27.22 changes inside `ed4528d5..HEAD`, which
Story 27.4 does not own. The ledger's path-level "matched" counts include these
files but not those hunks.

- `_bmad-output/implementation-artifacts/epic-27-context.md` — Story 27.4 owns only
  the `b7a4377a` recompile. Story 27.22 commits `f5beeed5` (historical C1.15
  evidence continuity section) and `b969cf32` (Story 27.22 owner sentence) are
  Story 27.22 work.
- `_bmad-output/implementation-artifacts/deferred-work.md` — Story 27.4 owns only
  the entries whose `source_spec` is this spec: four from the second-pass review
  (`6f3c727a`), one from the patch application and three from the patch-application
  review. The `spec-27-22-*` entries from `f5beeed5`, `8b9d87ab` and `b969cf32` are
  Story 27.22 work.

### File List Exclusions

- `_bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md` — owner: Story 27.22 (Deployment Adapter Developer); changed by Story 27.22 commits `f5beeed5`, `8b9d87ab` and `b969cf32` inside `ed4528d5..HEAD`, not Story 27.4 work.
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity.md` — owner: Story 27.22 (Deployment Adapter Developer); Story 27.22 spec changed in `f5beeed5`, not Story 27.4 work.
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md` — owner: Story 27.22 (Deployment Adapter Developer); Story 27.22 spec added in `9ec9f505` and changed in `8b9d87ab`, not Story 27.4 work.
- `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md` — owner: Story 27.22 (Deployment Adapter Developer); Story 27.22 spec added in `b969cf32`, not Story 27.4 work.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — owner: Story 27.22 (Deployment Adapter Developer); its Story 27.22 status sync in `f5beeed5` and `b969cf32`; this spec forbids Story 27.4 sprint-status edits.
- `docs/operations/access-telemetry-adapter-production.md` — owner: Story 27.22 (Deployment Adapter Developer); C1.16 runbook changes in `f5beeed5`, `8b9d87ab` and `b969cf32`, not Story 27.4 work.
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py` — owner: Story 27.22 (Deployment Adapter Developer); C1.16 fixture change in `b969cf32`, not Story 27.4 work.
- `tests/tooling/access_telemetry_c1/linkage_test.py` — owner: Story 27.22 (Deployment Adapter Developer); C1.16 linkage fixtures from `f5beeed5` and `b969cf32`, not Story 27.4 work.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` — owner: Story 27.22 (Deployment Adapter Developer); fixture change in `f5beeed5`, not Story 27.4 work.
- `tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py` — owner: Story 27.22 (Deployment Adapter Developer); SQL contract tests added in `8b9d87ab`, not Story 27.4 work.
- `tools/access-telemetry-c1-component-backend.ps1` — owner: Story 27.22 (Deployment Adapter Developer); C1.16 producer change in `b969cf32`, not Story 27.4 work.
- `tools/verify-access-telemetry-c1-linkage.ps1` — owner: Story 27.22 (Deployment Adapter Developer); C1.16 linkage collector from `f5beeed5` and `8b9d87ab`, not Story 27.4 work.
- `references/Hexalith.Builds` — owner: repository owner (Jérôme Piquot); dependency gitlink bump in `6f3c727a`, not Story 27.4 work.
- `references/Hexalith.EventStore` — owner: repository owner (Jérôme Piquot); dependency gitlink bumps in `109e527b`, `9ec9f505`, `0a63ff9d` and `6f3c727a`, not Story 27.4 work.
- `references/Hexalith.McpCli` — owner: repository owner (Jérôme Piquot); dependency gitlink bump in `109e527b`, not Story 27.4 work.
- `references/Hexalith.Platform` — owner: repository owner (Jérôme Piquot); dependency gitlink bumps in `109e527b`, `9ec9f505`, `0a63ff9d` and `6f3c727a`, not Story 27.4 work.
- `references/Hexalith.Tenants` — owner: repository owner (Jérôme Piquot); dependency gitlink bump in `109e527b`, not Story 27.4 work.
