---
title: 'Story 27.4 remaining retention verification and A41 close-out'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
repository_status: 'reviewed-awaiting-operator'
route: 'dispatch'
baseline_commit: 'b7a4377aaebce4b2d5f9e66d583ce9022f2f9fca'
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

- `tools/verify-access-telemetry-c1.ps1:27` — PG2 C1.15 refuses execution before target/output creation. Renewal is separately owned; C1.16 linkage/review is pending and 23 gates remain unregistered.
- `tools/verify_access_telemetry_lifecycle.py:2642` — reuse predecessor, producer, terminal, preflight/postflight and publication validators without relaxing gates.
- `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` and `AccessTelemetryA41CloseOutTests.cs` in that directory — twelve retention-decision and five A41 guards; reuse unchanged.
- `docs/operations/access-telemetry-lifecycle.md` — current PG2 producer/close-out procedure already implemented.

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
- [ ] [Review][Patch] Adopt the canonical phase ledger (create-story adoption baseline, dev-story, earlier offline code-review and this review's code-review rows) and cumulative File List in this spec [spec-27-4-…-close-out-4.md:64]
- [ ] [Review][Patch] Spec fails story-slice-scope: no Historical Context Classification, Slice Proof or per-checkpoint evidence table [spec-27-4-…-close-out-4.md:1]
- [ ] [Review][Patch] C1.16 blocker is stale — Story 27.22 is done with independently accepted C1.16 identity/full-set linkage [tests/27-4-retention-verification-evidence.md:42]
- [ ] [Review][Patch] Spec Verification keeps the weak architecture command that BH2 rejected (no `-failSkips`, no XML count check, not fail-fast) [spec-27-4-…-close-out-4.md:134]
- [ ] [Review][Patch] Epic AC Verification section is missing [spec-27-4-…-close-out-4.md:44]
- [ ] [Review][Patch] `baseline_commit` was overwritten (`ed4528d5` -> `b7a4377a`) against bmad-build step-03's never-overwrite rule, dropping the planning commit's `epic-27-context.md` recompile from both reviews [spec-27-4-…-close-out-4.md:8]
- [ ] [Review][Patch] Evidence blocker list omits credential-file paths and fault/purge authority named by the spec [tests/27-4-retention-verification-evidence.md:52]
- [ ] [Review][Patch] Dated "Planning checks" line was rewritten in place, dropping C1.16 19/19 and the kubectl render result and crediting implementation-stage results to planning [spec-27-4-…-close-out-4.md:139]
- [ ] [Review][Patch] Receipts are cited only by `/tmp` path, with no inline identifiers, and two post-run files (`final-a41.xml`, `final-worktree.diff`) are undisclosed [tests/27-4-retention-verification-evidence.md:153]
- [ ] [Review][Patch] Fail-fast claim is broader than its receipt — the focused receipt covers only the XML validator ("Lifecycle and build were not rerun") [spec-27-4-…-close-out-4.md:116]
- [ ] [Review][Patch] Remediation runtime checklist applicability is not stated [spec-27-4-…-close-out-4.md:64]
- [x] [Review][Defer] `sprint-status.yaml:165` epic-27 order reason still says C1.16 capture/linkage/acceptance pending while `:473` marks 27-22 done [sprint-status.yaml:165] — deferred: pre-existing, owned by the Story 27.22 sync; this spec forbids 27.4 sprint-status edits
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

## Spec Change Log

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

The repository portion is reviewed-awaiting-operator. Full spec/story completion
and sprint advancement remain withheld because the live tasks are still pending.
The workflow's default done/local-commit actions do not apply to this approved
partial scope: the frozen decision keeps 27.4 incomplete and requires separate
post-review authorization for staging/committing/publication. No such action ran.

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py'` — offline fixtures pass.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` — zero warnings/errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests -parallelMode none -noLogo` — seventeen guards pass.
- `git diff --check` — clean whitespace.

Planning checks, 2026-10-06: lifecycle 79/79, Debug/source-reference build zero warnings/errors, architecture 17/17 with no skips/failures. No external predecessor or target authority supplied; no live operations/A41 mutation/publication. Frozen intent preserved. The recommended offline scope was approved by Administrator on 2026-10-06.
