---
title: 'Executable pre-review story readiness gate'
type: 'feature'
created: '2026-07-28'
status: 'done'
baseline_commit: 'a4517654e7993237c3bfba473fae6b6a027e3ad1'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-executable-pre-review-story-gate.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-closeout-evidence-gate.md'
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** One retrospective action item was raised by four consecutive
retrospectives without ever closing — Epic 22 (`sprint-status.yaml:631`), Epic
23 (`:651`), Epic 24 (`:671`), Epic 25 (`:719`) — each restating it harder after
the previous form failed: *check* → *source guard or checklist* → *executable
guard* → *executable pre-review gate*. The rules were never missing: since
2026-07-16 `_bmad/custom/story-phase-ledger.md` has encoded them fail-closed.
Enforcement was LLM-obeyed prose, and drift continued. Two `done` stories
asserted in their own tables that their declared proof was never produced:
`26-5` (10 checkpoint rows at `pending`, under a preamble reading "Complete
every row before moving the story to review", after a three-chunk adversarial
review closing 54 findings) and `22-2` (5 rows).

**Approach:** One standalone stdlib-only verifier enforcing the mechanically
checkable subset of the phase-ledger policy, wired into `.githooks/commit-msg`
and both relevant `ci.yml` jobs, with its own tooling fixture lane; the policy
extended with the evidence-table rule, the declared-exclusion format, and an
explicit scope-limit paragraph; one activation directive on each of the two
status-advancing lifecycle skills; the existing code-review ledger review layer
extended rather than a new layer added; and the four action-item rows
consolidated into the Epic 25 row.

## Boundaries & Constraints

**Always:** Mirror `tools/check-story-file-scope.py` structurally (stdlib only,
`main(argv) -> int`, exit 0/1, all output to stdout, local `ValidationError`
caught only in `main`). Strip `\r` before any parse. Fail closed on an empty
changed set for a governed story. State every scope limit in the policy, both
directives, and `CONTRIBUTING.md` so a green gate is never read as full
verification. Preserve every existing persistent fact, activation directive, and
review layer exactly once.

**Ask First:** Adding a code-review review layer versus extending the existing
one; relaxing any check to accommodate a live artifact; widening the governed
set beyond artifacts carrying a ledger, File List, or evidence table.

**Never:** Edit generated `.agents/skills/**` or `.claude/skills/**`; verify
count arithmetic or test execution in this gate; reopen a closed story or
manufacture a completion date during retrofit; assert a `File List` /
`File Scope` set relation (see the withdrawal below).

**Withdrawn before wiring — do not reinstate.** A `File List` == `File Scope`
agreement check (C5) was approved, implemented, and then refuted by measurement:
"allowed but unchanged" is the normal case in **17 of 21** artifacts carrying
both sections, and a `Scope-Override:` **commit trailer** can legitimately place
a changed path outside the declared scope — trailers live in the commit message,
invisible to a story-file check. It was generalised from Story 27.3, one of only
two artifacts where the two sets coincide. `tools/check-story-file-scope.py`
already enforces that relation at commit time, with override support.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Story reaches `review`/`done` with a pending evidence row | any table with a `Review status`/`Review state` column | exit 1 naming each row | blocked at commit and in CI |
| Story below `review` with pending rows | `27-3` in-progress (14), `31-2` ready-for-dev (5) | exit 0 | not a violation |
| Blocked evidence row | row naming owner, consequence, reopen trigger | exit 0 | a recorded decision, not an open question |
| Changed path absent from File List | `in-progress`/`review` only | exit 1 naming the path | declared exclusions honoured; free prose is not |
| Ledger cell holds `TBD`/`N/A`/`-` | any canonical phase row | exit 1 | placeholder is not evidence |
| Newest ledger row lacks `matched N/N` | no blocked-evidence record either | exit 1 | policy requires the literal form |
| Status disagrees with sprint-status | story vs `development_status` | exit 1 | `spec-*` expected to carry no row |
| No story key resolves | `correct-course` commit | exit 0, documented no-op | never fails an unrelated commit |
| Empty changed set on governed story | bare `--story-key`, unstaged tree | exit 1 | inverts the sibling gate's vacuous pass |
| HEAD on default branch | `--derive-cumulative` | C1 skipped with a printed note | CI enforces C1 on the PR range |
| Genuine exception | `Story-Review-Readiness-Bypass: <reason>` | exit 0 | empty reason rejected |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `tools/check-story-review-readiness.py` — readiness verifier.
- `tests/tooling/story_review_readiness/**/*.py` — focused verifier fixtures.
- `.githooks/commit-msg` — local readiness-gate invocation only.
- `.github/workflows/ci.yml` — CI validation and fixture invocations.
- `CONTRIBUTING.md` — readiness-gate contributor guidance only.
- `_bmad/custom/story-phase-ledger.md` — executable ledger policy.
- `_bmad/custom/bmad-dev-story.toml` — development activation directive.
- `_bmad/custom/bmad-code-review.toml` — review activation directive and layer extension.
- `tests/tooling/bmad_customization/bmad_customization_test.py` — customization contract guards.
- `_bmad-output/process-notes/story-creation-lessons.md` — lesson L13.
- `_bmad-output/implementation-artifacts/22-2-bounded-cancellable-graph-traversal.md` — dated correction note.
- `_bmad-output/implementation-artifacts/26-5-operational-runbook-set.md` — dated correction note.
- `_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md` — standalone spec and completion record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — action-item reconciliation.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-executable-pre-review-story-gate.md` — implementation record.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-closeout-evidence-gate.md` — superseded-proposal disposition.

Read/verify only:

- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/planning-artifacts/epics.md`

Forbidden by default:

- shared instruction entry points
- commitlint configuration and workflow
- runtime source and tests
- submodule pointers and contents

## Code Map

- `tools/check-story-review-readiness.py` — NEW verifier: C1 File List
  completeness, C2 ledger rows and cells, C3 status vocabulary, C4 sprint-status
  agreement, C6 evidence-row status. No C5.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` — NEW
  fixture lane, including regression cases built from the real `26-5` and `22-2`
  shapes, false-positive guards from the real `27-3` and `31-2` shapes, byte-level
  CRLF cases, and liveness tests pinning the measured repository state.
- `.githooks/commit-msg` — third gate invocation, with `--derive-cumulative`.
- `.github/workflows/ci.yml` — validation step in `story-file-scope`
  (deliberately without `--derive-cumulative`); fixture step in
  `test-unit-contract`.
- `_bmad/custom/story-phase-ledger.md` — `### Declared Exclusions`,
  `## Evidence-Table Status Reconciliation`, `## Executable Gate`, and the
  `review`/`done` fail-closed gates extended.
- `_bmad/custom/bmad-dev-story.toml`, `_bmad/custom/bmad-code-review.toml` — one
  `STORY_REVIEW_READINESS_GATE` directive each; the code-review
  `story-phase-ledger` review layer extended to audit evidence rows.
- `tests/tooling/bmad_customization/bmad_customization_test.py` — directive-body
  and both-surface assertions, absence-from-creation-route guard, policy-content
  and review-layer assertions.
- `CONTRIBUTING.md` — `## Story Review Readiness`.
- `_bmad-output/process-notes/story-creation-lessons.md` — lesson L13.
- `_bmad-output/implementation-artifacts/22-2-...md`, `26-5-...md` — dated
  correction notes; neither story reopened.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — Epic 25 row closed
  on evidence; Epic 22/23/24 rows marked superseded.

## Tasks & Acceptance

**Execution:**
- [x] `tools/check-story-review-readiness.py` — five checks, bypass trailer,
      fail-closed empty changed set, default-branch C1 skip note, spec-key
      resolution.
- [x] `tests/tooling/story_review_readiness/` — 37 cases, all passing.
- [x] `.githooks/commit-msg` and both `ci.yml` steps.
- [x] `_bmad/custom/story-phase-ledger.md` — three new sections plus amended
      status gates.
- [x] Both toml directives; code-review ledger layer extended; all six review
      layers and all pre-existing directives preserved exactly once on both the
      `.agents` and `.claude` surfaces.
- [x] `tests/tooling/bmad_customization/` — 33 cases, all passing.
- [x] `CONTRIBUTING.md`, lesson L13, and the two story correction notes.
- [x] `sprint-status.yaml` — four action-item rows reconciled.

**Verification evidence (2026-07-28, baseline `a4517654`):**

- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py"` → **37 tests, OK**.
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` → **33 tests, OK**.
- Live sweep of all 22 governed artifacts: **19 pass, 3 fail, 0 false
  positives**. The three failures are `26-5` (10 pending rows), `22-2` (5), and
  `27-3` (newest ledger row reconciles as "48 -> 49 paths" rather than the
  contracted `matched N/N`).

**Known open item, deliberately not closed here:** the `27-3` finding is a real
deviation in a row authored 2026-07-28 by a concurrent session. Per the
Administrator decision of that date, C2 stays strict and the owning session
repairs the cell; the gate is not relaxed. Owner: Story 27.3. Reopen trigger:
the cell is rewritten in `matched N/N` form, or the policy is amended to accept
the arrow form — in which case C2 and this spec's matrix change together.

**Limit of this evidence:** C1 has never executed against a real cumulative
diff. It self-skips on the default branch and this work was done on `main`, so
C1 first exercises in CI against a pull-request range. Until a story passes
through the gate on the way to `review`, the Epic 25 closure rests on the gate
being wired and green, not on it having gated a real transition.
