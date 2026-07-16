# Story Phase Ledger Policy

This policy is the canonical, update-safe handoff contract for story-scoped
test counts and changed-file ownership. Workflows must use the exact phase names
`create-story`, `dev-story`, `qa-gap-closure`, and `code-review`.

## Canonical Change Log

Every governed story must contain this table before it enters `ready-for-dev`:

```markdown
## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
```

Append one row after each phase that performs governed work. Rows are
chronological and append-only. A repeated phase appends another row with that
same canonical phase name; it never overwrites an earlier row. The
`qa-gap-closure` row is optional only when no story-bound QA gap-closure work
occurred. All other completed phases require their row.

## Test-Count Evidence

Each `Test count` cell must record, for every affected discovery unit:

- the runner-derived phase delta, including `+0` when discovery count is
  unchanged, and tests removed when applicable;
- the cumulative story delta from the `create-story` baseline;
- before and after discovery totals when available;
- the named unit reported by the runner, such as test methods or test cases;
- the exact evidence command.

Arithmetic is valid only between comparable discoveries: the named unit,
runner, discovery scope, filters, and relevant configuration must match. If any
of those change, record an explicit mapping before subtraction. Treat a newly
added or deleted discovery scope as a named `0 -> N` or `N -> 0` transition;
map a renamed scope explicitly instead of silently treating its totals as the
same lane. Never combine methods, cases, projects, or assemblies into an
unlabeled total, and never infer a count from prose, checkboxes, or file totals.

Capture the observed total immediately before and after each governed phase.
The phase delta is the in-scope change between those snapshots; the cumulative
story delta is the sum of in-scope phase deltas from the create baseline. When
comparable out-of-scope work changes the same discovery lane between phases,
name its owner/evidence and record the external delta separately so that
`create baseline + cumulative story delta + external delta = observed total`.
Do not absorb external work into the story delta or leave unexplained drift.
When behavior is strengthened without changing discovery count, record `phase
delta +0` and describe the strengthened behavior in `Change`; do not claim an
added test.

When discovery cannot run, do not invent a count. Record the exact command,
blocker, owner, consequence, and reopen trigger. Blocked evidence remains an
open gate when the missing result is needed to prove count agreement.

## Phase Ownership

For a governed story created before this policy, the first participating
workflow must append a `create-story` adoption baseline before other edits. Its
`Change` cell must identify policy adoption, its owner, the current comparable
discovery totals and command (or complete blocked-evidence record), and the
reconciled cumulative File List. Earlier deltas are not reconstructed or
invented; the adoption point becomes the story baseline.

- `create-story` creates the canonical table and baseline row. Record actual
  phase delta `+0`, planned tests or range when quantified, baseline discovery
  totals when run, or a precise reason discovery was not run.
- `dev-story` reads the create baseline, obtains post-development discovery,
  records actual phase and cumulative deltas, and repairs stale permitted count
  references before handing off to review.
- `qa-gap-closure` starts from the latest chronological ledger row and the
  create baseline, records the QA delta, recomputes cumulative deltas and
  totals, and repairs all permitted stale count references. If no governing
  story exists and the work is not represented as story gap-closure, the test
  summary must state
  `story phase ledger: N/A — standalone QA`. Work represented as story
  gap-closure must resolve its governing story before any edits.
- `code-review` independently checks the ordered rows, same-unit arithmetic,
  live discovery evidence, reviewed diff, and stale permitted story-record
  references. After selected review patches, but before status synchronization,
  append the final row with the review-patch delta, including `+0` when no test
  count changed.

Workflows may repair count prose only in the Change Log, File List, Dev Agent
Record, dedicated QA test summary, and dedicated code-review record/findings
sections authorized by their base workflow. Story intent, acceptance criteria,
task definitions, and other human-owned or frozen sections remain read-only.

## Cumulative File List Reconciliation

At every phase, compare the cumulative story-scoped changed-file set with the
story File List. Include every added, modified, deleted, renamed implementation,
test, documentation, summary, and tracked workflow artifact in scope. Do not
absorb unrelated dirty-worktree changes. A policy-approved excluded session or
generated artifact must be named with its explicit reason.

Record `matched N/N` in the phase row together with the declared comparison
baseline, the name-status/diff command or artifact, and any named exclusions
with owner and reason. The compared name-status set is authoritative: a rename
is one entry that identifies both old and new paths, while a path restored
exactly to its baseline leaves the cumulative set. The cumulative File List and
the in-scope changed-file set must contain identical entries before handoff; a
partial or phase-local-only File List is not reconciled.

## Fail-Closed Status Gates

- Do not set `ready-for-dev` or mutate sprint status unless the canonical table
  and reconciled `create-story` row exist.
- Do not set `review` unless the required create and development rows, discovery
  evidence or precise blocker record, same-unit arithmetic, and cumulative File
  List all reconcile.
- Do not set `done` unless every performed phase has an ordered row, the final
  review row reflects post-patch evidence, live counts agree, and every in-scope
  changed path is reconciled.

For chunked review, the current invocation must carry explicit evidence that
all in-scope chunks are complete before it appends the final review row or sets
`done`. An intermediate chunk may emit findings, but it cannot finalize the
ledger or synchronize completion status.

Route an unambiguous count, row, stale-reference, or File List repair to
`patch`. Route ambiguous story scope or exclusion ownership to
`decision_needed`. Missing rows, stale totals, unresolved evidence required for
agreement, mixed-unit arithmetic, or unaccounted files halt status advancement.
