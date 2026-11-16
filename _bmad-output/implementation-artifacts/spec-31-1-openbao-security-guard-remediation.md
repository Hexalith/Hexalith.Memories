---
title: '31.1: Repair secret-safe diagnostics and independent review-state guards'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'd2a39a84ccf2bcc1f89c428b50591c3c462aaedf'
story_key: '31-1-openbao-platform-hardening-and-documentation'
parent_story_status: 'in-progress'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 31.1's credential detector discloses detected values through assertion diagnostics (EVAL-03). Its security-record validator rejects legitimate reviewer appointment and separately signed checkpoints (EVAL-04).

**Approach:** Make failure diagnostics secret-safe and validate appointment, individual signatures and parent completion separately. Retain the existing unsigned and fully signed formats and add adversarial regression controls.

**Decision:** The user's 2026-11-16 “yes” authorizes these two local guard fixes and preservation of the full closeout history. The prior closeout draft remains pending; this bounded remediation can complete independently of real security sign-off.

## Boundaries & Constraints

**Always:** Preserve detection patterns, digest/identifier exceptions, exact section/row bindings, rejected owner/persona identities, expired waiver and Administrator accountability. A pending checkpoint has no completion date; a completed checkpoint needs its own verified signer, valid date, current-profile/scope and independence evidence. Different independent signers/dates may cover different checkpoints. Parent completion requires all mandatory checkpoints. Synthetic approval never changes real gates. Keep the parent story, epic and sprint in-progress; C4b/C5b/C7 remain pending. Preserve CRLF, public test identities, signed report bytes and recorded measurements.

**Never:** Print candidate values or scanned text through assertion expression capture, collection rendering, inner exceptions or custom messages. Invent sign-off, renew a waiver, deploy, read credentials, send messages, stage/commit/push, alter dependencies, runtime or Story 31.2 activation. This test/documentation-only change needs no running AppHost.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected result | Failure handling |
|----------|---------------|-----------------|------------------|
| Detected material | Synthetic PEM, token, init label, UUID, long hex/base64 | Actual guard refuses it; diagnostic contains safe path/category/count only | Canary and surrounding text absent from full exception rendering |
| Published metadata | Existing labelled digests and UUID metadata | Remain accepted | No detector weakening |
| Appointment | Consistently assigned independent reviewer; no signature | Pending records accepted | Appointment supplies no completion date or closure |
| Partial signature | Only C4b signed; C5b/C7 pending | Separate evidence and summaries agree; parent in-progress | Missing provenance, stale summary, pending date or early parent closure refused |
| Separate authorities | Valid checkpoint signatures with differing signer/date | Each validated independently | Owner/persona, invalid/future dates or missing scope rejected |
| Existing states | Entirely unsigned or entirely signed controls | Remain accepted | Inconsistent records and unsupported closure fail closed |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`: actual guards and synthetic controls. `PlatformRecords_ContainNoSecretShapedMaterial` currently calls string/collection assertions with scanned content and joins detected values; `ShouldHaveNoMatch` is already boolean-based. Reuse detectors and `MarkdownContractDocument`, extracting a pure guard called by the real-record test so captured synthetic failures exercise the real path. `ValidateCurrentSecurityRecords` currently uses one global `signed` branch, hard-codes unassigned and requires one signer/date for all gates. Keep the legacy whole-review format while adding explicit per-checkpoint evidence for partial reviews. Keep current summaries bound to exact sections and cells.
- Same class may use focused partial files beside it for additional controls; one C# type per file and XML documentation still apply. Existing controls are fixed in-memory fixtures independent of mutable repository state.
- `docs/governance/openbao-independent-security-review.md`: required independent signed return and prior findings; add a dated remediation note without rewriting historical findings or implying sign-off.
- `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md`: append completion/phase evidence and reconcile cumulative File List. Preserve its large historical sections and checkpoint cells.
- `_bmad-output/implementation-artifacts/tests/31-1-assistant-security-evaluation-2026-11-16.{md,json}`: immutable attested findings; do not edit.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` (and focused partial files if needed) — fix EVAL-03/EVAL-04 and add executable positive/negative controls for every matrix row, including per-gate provenance and actual failure-output secrecy.
- [x] `docs/governance/openbao-independent-security-review.md` — record exact guard remediation scope and validation while retaining pending approval and platform findings.
- [x] `_bmad-output/implementation-artifacts/31-1-openbao-platform-hardening-and-documentation.md` — append actual commands/results, method/case delta, File List and continuing external dependency.
- [x] This spec — record exact verification and completed tasks; retain parent status and the separate closeout draft.

**Acceptance Criteria:**

- Given existing real unsigned records, when corrected guards execute, then they pass with no changed security checkpoint or migration activation state.
- Given each matrix control, when the production guard path executes, then its expected acceptance/refusal is demonstrated by passing tests, without synthetic credential disclosure.
- Given completed local remediation, when findings are reported, then EVAL-03/EVAL-04 have bounded regression evidence and independent sign-off plus other platform findings remain open.

## Implementation Notes

Planning uses actual `global.json`/build configuration: .NET SDK 11.0.100, net11.0, Debug with `UseHexalithProjectReferences=true`; older project-context version prose is historical. No irreversible actions or remaining intent gaps. Preserve the already-refreshed epic context and closeout draft from the preceding turn.

## Spec Change Log

- 2026-11-16: Implemented only the approved EVAL-03/EVAL-04 local guard remediation. Frozen intent and parent status are unchanged. Separate partial files keep the single test type focused; no runtime or external approval state changed.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| :--- | :--- | :--- | :--- |
| BH-01 | medium | patch | Reviewer parsing removes underscores; the permitted signature grammar and matching independence/provenance retain them, so consistent identities fail. Group IDENTITY. |
| BH-02 | medium | patch | Per-checkpoint C7 scope checks only platform-wide; the authoritative C7 checkpoint also requires both limitations and the AC2 qualifier. Group SCOPE. |
| BH-03 | medium | patch | Substring scope checks accept the reproduced does-not-cover statement because all required terms remain present. Require the affirmative canonical scope clause. Group SCOPE. |
| BH-04 | medium | patch | The newly required provenance source/method accept not-yet-available and verification-pending fragments as substantive; this contradicts completed provenance. Group PROVENANCE. |
| BH-05 | medium | patch | A per-gate independence declaration with a contradictory owner basis passes the current prefix/length checks. Reject explicitly contradictory or unresolved evidence. Group INDEPENDENCE. |
| BH-06 | medium | patch | The pending-date regex permits completion date is followed by an ISO date. The pending state must reject a stated completion date. Group PENDING-DATE. |
| BH-07 | medium | patch | The new prerequisite prefix check accepts complete only after missing evidence is supplied. Preserve complete or complete followed by the existing explanatory dash delimiter. Group PREREQUISITES. |
| BH-08 | medium | patch | Prerequisite review and evidence cells are not consulted by the new parent-completion check, so a pending review can coexist with done. Require verified review and substantive evidence. Group PREREQUISITES. |
| BH-09 | medium | patch | The new per-checkpoint branch skips the reopen-trigger checks, accepting an explicit replacement waiver despite remaining pending peers. Group REOPEN. |
| BH-10 | medium | patch | Sprint matching counts only declarations with the expected value and ignores a duplicate opposite-value declaration. Group TRACKING-UNIQUENESS. |
| BH-11 | medium | patch | Per-gate summary matching recognizes is but ignores the reproduced contradictory remains-pending claim. Bind current checkpoint statements consistently. Group SUMMARIES. |
| EC-01 | medium | patch | Independently reproduced underscore identity corruption at reviewer parsing; same concrete failure as BH-01. Group IDENTITY. |
| EC-02 | medium | patch | Independently reproduced opposite-value duplicate sprint key passing; same concrete failure as BH-10. Group TRACKING-UNIQUENESS. |
| EC-03 | medium | patch | Pending-date detection also permits numeric non-ISO dates after an explicit completion-date label. Group PENDING-DATE. |
| VG-01 | medium | patch | Pre-verified regression gap: new C7 fallback is exercised by no synthetic control, while all current real records stay pending. Extract the existing C7 routine and exercise its separate-signature branch. Group C7-CALLER-COVERAGE. |
| VG-02 | medium | patch | The verification reviewer reproduced the allowed underscore reviewer failing; inspected parser confirms destructive Markdown removal. Group IDENTITY. |

## Verification

Use `run-with-temp` for every disposable job and retain logs in `artifacts/story-31-1/guard-remediation-2026-11-16/`. Build Server.Tests with `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --artifacts-path "$TMPDIR/build" --disable-build-servers -m:1 /nr:false -p:MinVerVersionOverride=1.0.0` (audit enabled). Copy solution marker and required records/manifests under TMPDIR so root discovery reads current files. Invoke the built assembly with `-class Hexalith.Memories.Server.Tests.Deployment.OpenBaoPlatformDocumentationTests -parallel none -noLogo`; run method `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`. All cases must pass without skips, and build with zero warnings/errors. Capture comparable before/after focused method discovery where practical. Run `git diff --check`, CRLF and immutable-report hashes, and compare authoritative checkpoint rows/status to baseline. No runtime/tenant routing surface changes; tenant-isolation evidence is not applicable.


### Executed results — 2026-11-16

All four implementation tasks are complete. The parent story remains **in-progress**, C4b/C5b/C7
remain **pending / not complete** without completion dates, and the independent evaluator remains
unassigned. Administrator retains scheduling accountability. The separate closeout draft remains
pending, and independent sign-off plus the other platform findings remain open.

- Exact jobs: `run-with-temp bash artifacts/story-31-1/guard-remediation-2026-11-16/verify.sh before`
  and the same command with `final`. The script records the required Debug/source build command
  above with audit enabled, copies the solution marker and every required record/manifest under
  TMPDIR for root discovery, and invokes the built assembly for the focused class, method discovery
  and pinned deployment regression. Both baseline and final builds passed with **0 warnings/errors**.
- Comparable focused **xUnit methods: 18 → 27 (+9)**, none removed; **executed cases: 18 → 74 (+56)**.
  Final documentation result: **74 total, 0 errors, 0 failed, 0 skipped, 0 not run**. The pinned
  `ProductionDeploymentArtifactsTests.OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`
  regression passed **1/1** without errors/failures/skips. An intermediate implementation passed
  66 cases; final results include the additional authority/consistency controls. Final source hashes
  were recorded before the final build and verified unchanged afterward.
- Focused sorted method-set SHA-256:
  `5c78500c285a108d669f1e7aa2f3885b3d8869ea366f31c77d62f216c4b67916` →
  `9fdedc364fc7bdbab2a3b21ca190c69b2c82a70aba06406e2f0be037c8765896`.
- Secret controls exercise `ValidateSecretSafeRecord`, which is called by the real published-record
  test: fourteen PEM/token/init/UUID/hex/base64 cases assert full exception rendering excludes the
  candidate and surrounding text and has no inner exception. Published digest/UUID labels remain
  accepted; multi-value failure counts are checked without rendering scanned text or collections.
- Fixed review controls demonstrate unsigned and legacy signed acceptance, independent appointment,
  each separately signed checkpoint and distinct independent signers/dates. Twenty-two per-gate
  corruptions cover missing/mismatched provenance and independence, invalid/stale/future dates,
  missing profile/checkpoint/limitation scope, pending completion dates, stale summaries/consequence/
  limitation text and early parent closure. Seven owner/persona username/email authorities are
  refused in both appointment and whole-review signature controls; all six documentation prerequisites
  are independently required before parent completion. Controls use a fixed synthetic date for
  differing signature dates, while real records use today's UTC date.
- Legacy whole-review signed records now explicitly require `Evaluator provenance` with the verified
  signer, durable signed source and signature-verification method, alongside existing independence
  evidence. Per-checkpoint records use separate evaluation/provenance/independence fields and exact
  per-gate summaries. The guard verifies recorded consistency; authentic external signature
  verification remains the maintainer's obligation before any real gate transition.
- `run-with-temp python3 artifacts/story-31-1/guard-remediation-2026-11-16/verify-integrity.py`
  passed. All authoritative checkpoint rows and parent status match the before snapshot; measured
  evidence, operations runbook, epic/sprint records, refreshed context and separate closeout draft
  preserve their bytes. Both attested files retain their before hashes; all four earlier report
  files also match baseline bytes after Git checkout filters. All six owned C#/Markdown files are
  CRLF with no doubled carriage returns. `git diff --check` passed. The initial integrity harness
  compared LF Git blobs to CRLF worktree bytes and failed; checkout-filtered baseline comparison
  corrected the harness without editing any report. Its first result is retained separately.

Evidence and exact scripts/logs: `artifacts/story-31-1/guard-remediation-2026-11-16/`, including
`before-*`, `final-*`, `settled-source-sha256.json`, `discovery-summary.json`, and `integrity.json`.
The story's completion note, 31-path cumulative File List and appended phase ledger retain the full
history. Six implementation paths are owned by this bounded phase; preceding context/closeout edits
and immutable report history receive no new implementation credit. No runtime or tenant routing
surface changed, so tenant-isolation evidence is not applicable.


### Review corrections and focused validation — 2026-11-16

The earlier **74-case** class execution remains historical evidence, with its logs and source hashes
unchanged. The review phase fixes the demonstrated gaps without changing frozen intent or real
checkpoint/approval state:

- Remove bold/code wrappers without rewriting underscore usernames or emails; fixed appointment,
  legacy signature and individual-signature controls accept both forms.
- Require the affirmative canonical scope clause, including both accepted limitations and the
  amended AC2 qualifier for C7; reject missing C7 scope and explicit negative coverage clauses.
- Refuse explicitly unresolved/unverified provenance and contradictory/unresolved independence,
  while retaining completed references and an explicit “not the platform owner” basis.
- Reject numeric dates after a pending completion label, including `completion date is 2026-11-16`
  and `completion date: 11/16/2026`; preserve historical waiver prose.
- Require canonical unconditional `complete` and `verified` states, allowing their real
  ` — explanation` forms, plus substantive evidence for each parent prerequisite.
- Validate expired-waiver/pending-review and profile-reassessment triggers for per-checkpoint states;
  reject waiver renewal or “no further review needed” claims. Require one exact Story 31.1 sprint
  declaration and reject extra conflicting current checkpoint statements in every checked summary.
- Extract the real C7-state validation into `ValidateSecurityReviewerEvidence`. A fixed C7-only
  record exercises that routine, accepting its separate evaluation and refusing a missing signature.

Exact focused command:
`run-with-temp python3 artifacts/story-31-1/guard-remediation-2026-11-16/review-corrections/verify-review.py`.
It builds Server.Tests in Debug with source references, audit enabled and disposable TMPDIR output,
then executes only review-state methods and the two affected real current-state/C7 facts. Result:
**76 cases across 20 selected methods, 0 errors, 0 failures, 0 skips, 0 not run**; build **0 warnings,
0 errors**, 1m16.97s. Ten added methods supply 32 declared controls. The selected-case count is not
comparable to the earlier whole-class 74-case count; no class-wide or deployment-regression rerun is
claimed here. The parent runs the complete prescribed verification separately.

Exact argument arrays, selected methods, logs and stable source hashes are retained under
`artifacts/story-31-1/guard-remediation-2026-11-16/review-corrections/`. The same six implementation
paths are owned; the cumulative File List remains 31 paths. Real checkpoint cells, parent status,
measurements, report bytes, preceding context/closeout edits and activation criteria remain
unchanged. CRLF and `git diff --check` passed. Independent sign-off and the other platform findings
remain open; C4b/C5b/C7 remain pending without completion dates.


### Parent verification and bounded completion — 2026-11-16

All 16 review findings, independently triaged above and grouped by ten root causes, are fixed.
No review finding was deferred. Parent verification used the settled review-source hashes and
`run-with-temp bash artifacts/story-31-1/guard-remediation-2026-11-16/verify.sh parent-final`:
Debug/source build with audit enabled passed with **0 warnings/errors**; the complete documentation
class passed **106 cases**, and the pinned deployment regression passed **1 case**, with zero errors,
failures, skips or not-run cases. Comparable focused discovery is **18 → 37 methods (+19)**, no
removals; cases are **18 → 106 (+88)**. Final method-set SHA-256: `3b0b17ed79298d6969485337c5c0c4e7b4f46186cbddd91d297732f897e579a2`.

`run-with-temp python3 artifacts/story-31-1/guard-remediation-2026-11-16/verify-parent.py` confirms
all source hashes, attested/historical report bytes, measured inputs, preceding context/closeout
edits, checkpoint rows and parent status remain preserved; all six owned files use CRLF and
`git diff --check` passes. Exact results are in `parent-verification.json` and `parent-final-*` logs
beside that script. Every matrix row and the additional review controls executed successfully.

Only this bounded remediation spec is done. The approved parent-status invariant takes precedence
over the workflow's generic sprint promotion: Story 31.1, its epic and sprint remain in-progress,
and C4b/C5b/C7 remain pending without completion dates. The frozen no-stage/no-commit/no-push
constraint is retained; no local commit or remote operation was performed. Independent evaluation,
the pending closeout draft and other platform findings remain separate obligations.


### Technical sign-off — 2026-11-16T21:16:18Z

**Signed by:** Codex (AI assistant), as implementer and coordinating technical reviewer.
**Decision:** I sign off the completed EVAL-03/EVAL-04 guard remediation and its 16 resolved
implementation-review findings, with none deferred. The recorded 106 documentation cases plus
1 deployment regression passed, with zero build warnings/errors or failed/skipped cases.
The exact verified source hashes were rechecked at signing; the source remains uncommitted.

**Form:** Typed, non-cryptographic assistant attestation. Its covered pre-sign-off prefix is
**20976 bytes**, SHA-256 `bff27d9737569d885419b3a737a2549cbb5a80b0d2c14c1da61c2c8d9598c3b2`,
preserving UTF-8/CRLF bytes. The appended signature is outside that prefix. Attribution, source
and evidence hashes, covered prefixes and resulting document hashes are retained in
`artifacts/story-31-1/guard-remediation-2026-11-16/technical-signoff.json` and the dated sign-off
section of `docs/governance/openbao-independent-security-review.md`.

This attests the bounded local implementation and recorded verification. It supplies no independent
human signature or owner attestation; C4b/C5b/C7, parent status, the separate closeout draft and
other platform findings retain their existing states. No commit or remote action was performed.
