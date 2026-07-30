# Sprint Change Proposal — Story-Gate Hook Sequencing

**Date:** 2026-07-30
**Status:** approved for implementation — Administrator approval recorded 2026-07-30
**Author:** Developer (correct-course)
**Requested by:** Administrator
**Scope classification:** Minor — local contributor guard sequencing, focused tests, and matching guidance
**Decision record:** Incremental proposals 1, 2, and 3 were individually approved by the Administrator on 2026-07-30. Overall implementation approval remains pending.

---

## 1. Issue Summary

The story-file-scope policy permits an artifact owner to be supplied by an explicit CLI argument,
a Story or Story-Key commit trailer, or a branch name. The local pre-commit hook runs the validator
before the proposed commit message exists and supplies only the branch name. On main, or any branch
without a complete owner key, it therefore rejects the commit before the later commit-msg hook can
read a valid trailer.

The failure is sequencing, not the ownership policy. A changed commit must still resolve exactly one
story or standalone-spec owner and must remain inside that artifact's File Scope. The correction makes
pre-commit provisional only when no owner source exists at that phase; commit-msg remains the
definitive local gate.

### 1.1 Verified claim register

Verified 2026-07-30 against worktree HEAD
4a6f0d33689fde8335b5c7a8d429d885fa82040a plus the preserved user changes.

| Claim | Class | Re-runnable command / evidence | Observed | Verdict |
| :---- | :---- | :----------------------------- | :------- | :------ |
| "Story 12.3 permits explicit CLI, commit-trailer, or branch ownership." | Behavioral / existence | <code>sed -n '21,29p' _bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md</code> | AC1 names all three sources and requires fail-closed conflicts. | confirmed |
| "The epic permits a conventional-commit footer." | Behavioral / existence | <code>sed -n '2634,2645p' _bmad-output/planning-artifacts/epics.md</code> | Story 12.3 says a commit may reference a story through branch, footer, or explicit annotation. | confirmed |
| "pre-commit cannot inspect the proposed commit message." | Location / behavioral | <code>nl -ba .githooks/pre-commit</code> | The hook supplies branch-name and changed-files-file only; it has no message-file input. | confirmed |
| "commit-msg can inspect the proposed message and staged paths." | Location / behavioral | <code>sed -n '1,43p' .githooks/commit-msg</code> | The hook receives its message file and passes commit-message-file plus the staged changed-file list to the same validator. | confirmed |
| "The current failure occurs before trailer resolution." | Behavioral | <code>.githooks/pre-commit</code> with the current staged set on main | Exit 1: "No story key resolved..." while the staged set is references/Hexalith.EventStore. | confirmed |
| "The existing focused scope suite is green before correction." | Quantitative | <code>python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py'</code> | 51 tests ran and passed. | confirmed |
| "A changed set on main with no owner is intended to remain fail-closed." | Behavioral / policy | <code>sed -n '56,102p' CONTRIBUTING.md</code> | Guidance explicitly retains the unowned-main failure and documents commit-msg trailer validation. | confirmed |
| "The default dated proposal path is already occupied by user work." | Existence | <code>git status --short -- _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30.md</code> | The existing proposal is untracked and preserved. | confirmed |

No claim is corrected in PRD, architecture, UX, or epics. The present code contradicts no desired
Story 12.3 outcome; it prevents one of that story's approved owner sources from reaching the phase
that can read it.

## 2. Impact Analysis

### 2.1 Epic and story impact

- Epic 12 keeps its existing scope and status.
- Story 12.3 remains historical and done; it is not reopened.
- No new epic or numbered story is created.
- No sprint-status value or execution order changes.
- Story 14.1's CI fail-closed behavior remains unchanged.

### 2.2 Artifact conflict analysis

| Artifact | Impact |
| :------- | :----- |
| PRD | No change. Product outcomes and MVP scope are unaffected. |
| Architecture | No change. This is local contributor-tool sequencing, not a runtime or deployment decision. |
| UX | Not applicable. |
| epics.md | Read and verified only. Story 12.3 already permits trailer ownership and commit-time enforcement. |
| Story 12.3 artifact | Historical reference only; no edit and no status change. |
| sprint-status.yaml | No change. |
| Existing sprint-change-proposal-2026-07-30.md | Preserved without edit. |
| Git index and submodule pointers | Preserved without restaging, reset, or ownership reassignment. |

### 2.3 Technical impact

The validator receives one explicit mode used only by pre-commit. When changed files exist but no
artifact owner can yet be resolved, that mode returns success with an explanatory deferral message.
It does not defer malformed keys, multiple keys, conflicting keys, missing artifacts, invalid File
Scope declarations, forbidden-default changes, or out-of-scope files under a resolved owner.

The commit-msg hook does not use the mode. A commit that reaches commit-msg without a valid owner
still fails exactly as it does today. CI does not use the mode and remains fail-closed.

## 3. Options Considered

### Option A — Recommended: phase-aware deferral

Add a narrowly named defer-unresolved-owner mode to the shared validator and use it only from
pre-commit. Keep commit-msg and CI definitive.

**Benefits:** makes Story-Key trailers usable, preserves early checking on owner-named branches,
retains one validator implementation, and does not relax final enforcement.

**Risk:** a future caller could misuse the option. Mitigate with explicit help text, hook-wiring
tests, and documentation that the mode is valid only when a later definitive gate is guaranteed.

### Option B — Remove story-scope validation from pre-commit

Run the story-scope validator only from commit-msg and CI.

**Benefits:** simplest phase model.

**Cost:** loses early scope feedback on correctly named owner branches and leaves a tracked no-op
hook or requires unrelated line-ending-test changes.

### Option C — Require every working branch to contain the owner

Keep current behavior and prohibit trailer-only ownership.

**Rejected:** contradicts the currently approved Story 12.3 owner-source contract and the documented
main workflow. It also leaves an error message recommending trailer and CLI inputs that the hook
cannot consume.

## 4. Recommended Approach

Use Option A as a direct adjustment.

The repository is not overprotected in requiring an owner; it is overprotected at pre-commit by
treating an unavailable future input as already missing. The corrected sequence is:

1. pre-commit gathers staged paths.
2. If the branch resolves an owner, validate its File Scope immediately.
3. If no source resolves an owner, emit a deferral diagnostic and allow commit-message creation.
4. commit-msg reads the proposed message and performs definitive scope validation.
5. CI remains an independent fail-closed enforcement layer.

**Effort:** low.
**Implementation risk:** low.
**Schedule impact:** none outside this correction.
**MVP impact:** none.

## 5. Detailed Change Proposals

### 5.1 Validator mode

**OLD**

Any non-empty changed set with no resolved owner exits 1 immediately.

**NEW**

Add an explicit defer-unresolved-owner option. When and only when no owner source exists, the
validator prints that resolution is deferred to commit-msg and exits 0. Without the option, current
failure behavior is unchanged.

The implementation must distinguish an absent source from an invalid source. Malformed, partial,
multiple, or conflicting branch/spec keys must continue to exit 1 even in pre-commit mode.

### 5.2 Hook sequencing

**OLD**

pre-commit invokes the validator with branch-name and changed-files-file, so main fails before the
commit message exists.

**NEW**

pre-commit adds defer-unresolved-owner. commit-msg remains unchanged and invokes the definitive
validator with commit-message-file. No shell-level story-key parsing is added.

### 5.3 Regression coverage

Focused tests will prove:

1. A non-empty main changed set without a visible owner defers only when the explicit option is set.
2. The same input without that option still fails.
3. A malformed or multi-owner branch still fails with the option.
4. A resolved owner with an out-of-scope or forbidden-default path still fails with the option.
5. Trailer ownership remains accepted when commit-message-file is supplied.
6. pre-commit carries the defer option, while commit-msg and CI do not.
7. Existing scope-parser, override, forbidden-default, and conflict tests remain green.

### 5.4 Contributor guidance

Update CONTRIBUTING.md to state:

- pre-commit is provisional only when no owner can yet be observed;
- owner-named branches still receive immediate scope validation;
- commit-msg is the definitive local scope gate because it can read trailers;
- Story-Key trailers therefore work on main;
- a changed commit with neither a trailer nor an owner-named branch still fails;
- CI behavior is unchanged.

### 5.5 Implementation owner

Create the standalone implementation artifact:

_bmad-output/implementation-artifacts/spec-fix-story-gate-hook-sequencing.md

It owns one outcome: make an approved commit-trailer owner reachable without allowing an unowned
commit. It will contain the verified-claim table required by the Epic AC Verification policy.

Its File Scope will allow only:

- .githooks/pre-commit
- tools/check-story-file-scope.py
- tests/tooling/story_scope/story_scope_validator_test.py
- CONTRIBUTING.md
- _bmad-output/implementation-artifacts/spec-fix-story-gate-hook-sequencing.md
- _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30-story-gate-hook-sequencing.md

Read/verify-only inputs include .githooks/commit-msg, .github/workflows/ci.yml, Story 12.3,
Story 14.1, the existing standalone-spec commit-path artifact, project context, and the applicable
custom governance policies.

## 6. Historical Context Classification

| Reference | Classification | Permitted influence |
| :-------- | :------------- | :------------------ |
| Story 12.3 ownership, File Scope, and shared-validator invariant | current-narrow-pattern | Preserve exact-owner resolution, early scope feedback when possible, fail-closed conflicts, and one shared Python validator. Do not reuse the whole completed story shape. |
| Story 12.3 pre-commit missing-owner sequencing | anti-template | Evidence of the defect only. Do not reproduce a phase that recommends inputs it cannot observe. |
| spec-resolve-story-gate-commit-path | historical-reference-only | Evidence that a trailer cannot rescue the earlier pre-commit gate and that unowned main must remain fail-closed. Its frozen scope and implementation shape are not reused or edited. |
| Story 14.1 CI hardening | current-narrow-pattern | Preserve CI's loud, fail-closed behavior. Do not transfer the local deferral option into CI. |

## 7. Slice Proof

The correction has one independently demonstrable outcome: a valid Story-Key trailer can reach and
satisfy the definitive commit-msg scope gate without an earlier no-owner rejection. The change does
not alter scope matching, override authority, CI enforcement, story readiness, tenant-isolation
evidence, commitlint, product code, runtime behavior, or submodule state. The six owned paths form one
coherent implementation/test/documentation slice.

## 8. Compatibility, Risk, and Rollback

### Compatibility

- Owner-named branches behave as before, including early scope failure.
- Trailer-only ownership becomes usable.
- Unowned commits remain blocked by commit-msg and CI.
- Existing CLI callers remain fail-closed unless they explicitly opt into the new mode.

### Risks and mitigations

| Risk | Mitigation |
| :--- | :--------- |
| Deferral option is used by a definitive caller | Keep the option absent from commit-msg and CI; add wiring regressions and explicit help text. |
| Invalid branch metadata is mistaken for no metadata | Validate malformed and multi-owner branch values before considering deferral; cover both cases. |
| Scope violations are accidentally deferred | Defer only owner absence; once an owner resolves, run the full existing validation path. |
| User work is overwritten | Use distinct proposal/spec filenames and leave the existing proposal, index, and unrelated worktree changes untouched. |

### Rollback

Remove the pre-commit option and the corresponding validator argument, tests, and guidance. This
restores the current early failure without data migration or runtime rollback. No Git history,
submodule, or product-state repair is involved.

## 9. Implementation Handoff

**Scope classification:** Minor.

**Recipients:** repository maintainer/developer, with focused tooling review.

**Implementation sequence:**

1. Create the approved standalone spec with its File Scope, verified claims, classifications, and
   slice proof.
2. Add the validator mode and wire pre-commit.
3. Add regressions before updating contributor guidance.
4. Run focused validation and reconcile the spec's file/evidence record.

**Required validation:**

- <code>python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py'</code>
- direct fail-closed probe without defer mode
- direct pre-commit deferral probe with the current staged set
- <code>git diff --check</code> over the owned paths

**Success criteria:**

- pre-commit on main no longer emits the current terminal no-owner error;
- a valid trailer is read and enforced by commit-msg;
- a commit with no resolved owner still fails before creation;
- resolved-owner scope violations still fail at pre-commit;
- CI remains unchanged and fail-closed;
- focused tests and diff checks pass;
- no unrelated worktree or index state changes.

## 10. Approval Record

| Decision | State | Date |
| :------- | :---- | :--- |
| Proposal 1 — phase-aware hook sequencing | approved by Administrator | 2026-07-30 |
| Proposal 2 — standalone spec ownership and narrow scope | approved by Administrator | 2026-07-30 |
| Proposal 3 — validator, hook, tests, and documentation details | approved by Administrator | 2026-07-30 |
| Complete Sprint Change Proposal | approved by Administrator | 2026-07-30 |

Implementation was authorized by the Administrator after review of the complete proposal.
