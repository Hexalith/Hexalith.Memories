---
title: 'Story-own-commit File List scoping'
date: '2026-08-14'
author: 'Administrator'
route: 'correct-course'
status: 'approved'
approved_by: 'Administrator'
approved_date: '2026-08-14'
scope_classification: 'moderate'
trigger_story: '24-6-graph-content-level-tenant-isolation-evidence'
context:
  - '{project-root}/_bmad/custom/story-phase-ledger.md'
  - '{project-root}/_bmad/custom/story-scope-guard.md'
  - '{project-root}/_bmad/custom/epic-ac-verification.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-24-6-graph-content-level-tenant-isolation-evidence.md'
---

# Sprint Change Proposal — Story-Own-Commit File List Scoping

## 1. Issue Summary

### Problem statement

`_bmad/custom/story-phase-ledger.md:102-116` requires a governed story to reconcile
its cumulative File List against the **uncurated** changed-file union of
`baseline_commit..HEAD` plus the worktree. On a trunk-based repository where every
story branches from and merges to `main`, that range is not the story's diff — it is
*everything everyone committed since the story was created*. The story is then
required to hand-author one `### File List Exclusions` bullet per foreign path, and
that hand-count is invalidated by the next concurrent commit, whoever makes it.

The rule therefore produces a record that is correct only at the instant it is
written, and a gate result that decays without anyone touching the story.

### How it was discovered

Story 24.6's eighth-pass code review (2026-08-14) raised two fail-closed blockers,
**E-P3** (File List unreconciled at 30 of 34 raw paths) and **E-P4** ("no external
same-lane delta" is false; 86 test cases observed against 58 recorded). The
Administrator's judgement on handing these over was that both are one symptom:

> E-P4 and E-P3 are both symptoms of the same thing — Story 24.7 landed inside
> 24.6's baseline range and moved a lane 24.6 measures. […] A durable fix scopes the
> union to the story's own commits rather than a hand-counted total any concurrent
> commit invalidates — that would close the fourth recurrence of this defect rather
> than the instance.

That reading is confirmed by the evidence below. E-P4's arithmetic half has already
been closed by the appended ledger row (`52 + 6 + 28 = 86`, external delta named and
owned). What remains is the File List half, and it is a rule defect rather than a
Story 24.6 defect.

### Evidence

Story 24.7's own `baseline_commit` is `8feb2a2d`, which is itself inside Story 24.6's
`0ecdffed..HEAD` range. 24.7's commits `0f577c48` and `dc5fde62` land there, touching
two files that are Story 24.6 File List paths (`TenantIsolationVerifierTests.cs`,
`ServerEndpointAuthorizationTests.cs`) plus one that is not
(`MemoriesServerServiceCollectionExtensions.cs`).

**Fourth recurrence of the same defect family, all four inside Story 24.6:**

| Pass | ID | Anchor | Defect |
| :--- | :- | :----- | :----- |
| Second (2026-08-12) | D1 | `spec-24-6-…md:260` | Unaccounted in-scope changed path breaks File List reconciliation; resolved by *adding a hand-written exclusion*. |
| Fifth (2026-08-13) | F5-P1 | `spec-24-6-…md:423` | Three in-scope changed paths exempted by prose only. |
| Sixth (2026-08-13) | F6-P1 | `spec-24-6-…md:602` | The recorded raw-union command is index-blind and does not reproduce its own result. |
| Eighth (2026-08-14) | E-P3 | `spec-24-6-…md:735` | Four unaccounted paths; readiness exits 1. |

D1's own resolution — write another bullet — is what F5-P1, F6-P1, and E-P3 each
found insufficient. Lesson **L13** (`story-creation-lessons.md:162-198`) names this
exact shape: *"When a retro item recurs unchanged for two epics, stop restating it
and make it executable."*

**The scale is not specific to Story 24.6.** Measured at HEAD across every in-flight
story:

| Story | Status | Commits in range | Own | Foreign | Raw union paths |
| :---- | :----- | ---------------: | --: | ------: | --------------: |
| 24-6-graph-content-level-tenant-isolation-evidence | in-progress | 27 | 13 | 14 | 34 |
| 24-7-tenant-configured-vector-dimension-verification | review | 3 | 2 | 1 | 11 |
| 27-3-production-adapter-and-deployment-profile | in-progress | 161 | 6 | **155** | **1,336** |
| 31-1-openbao-platform-hardening-and-documentation | in-progress | 111 | 0 | 111 | 1,068 |
| 31-2-runtime-dapr-secret-store-migration | ready-for-dev | 109 | 0 | 109 | 1,062 |

Under the current rule, reconciling Story 27.3 requires hand-authoring on the order
of 1,300 exclusion bullets. That is not a discipline problem; the rule is not
executable at that scale, so it is silently not executed.

**The gate's own recorded limit is the same root cause.** `sprint-status.yaml:738`,
the row that closed the four-epic action-item chain on 2026-07-28, states under
HONEST LIMITS: *"C1 self-skips on the default branch so it has never run against a
real cumulative diff — it first bites in CI on a PR range."* That skip
(`tools/check-story-review-readiness.py:653-658`) exists **because**
`baseline..HEAD` on `main` returns unrelated work. Removing the contamination
removes the reason for the skip.

---

## 2. Impact Analysis

### Epic impact

**None.** No epic changes scope, sequence, priority, or acceptance criteria. Epic 24
remains as written; Story 24.6 remains the graph content-level tenant isolation
evidence story. This correction changes how *any* story's File List is reconciled,
which is governance machinery, not epic content.

Checklist §2.1–2.5 recorded: 2.1 `[x] Done` (Epic 24 completes as planned), 2.2
`[N/A]`, 2.3 `[x] Done` (all remaining epics benefit; none requires change), 2.4
`[N/A]`, 2.5 `[N/A]`.

### Story impact

| Story | Impact |
| :---- | :----- |
| **24.6** | E-P3 closes by re-running reconciliation under the new rule rather than by adding four bullets. Eleven of its twelve exclusion bullets become unnecessary. Seven stale "30-path" assertions (E-P7) are corrected in the same pass. E-D2's hand-enumeration of multi-owner paths becomes machine-derived. |
| **24.7** | No edit required; already reconciles at nine paths. Named as the auto-attributed owner of three paths in 24.6's record. |
| **27.3, 31.1, 31.2** | Newly reconcilable. 27.3 goes from a 1,336-path hand-count to a 6-commit derivation. 31.1 and 31.2 carry **zero** commits with their trailer anywhere in history, so their derived sets are empty and C1 fails closed — correct, and diagnosable rather than skipped. |
| **All future stories** | Reconciliation becomes a command, not a hand-count. |

### Artifact conflicts

| Artifact | Conflict | Action |
| :------- | :------- | :----- |
| `_bmad/custom/story-phase-ledger.md` | §"Cumulative File List Reconciliation" prescribes the uncurated raw union — the defect's origin | Amend to prescribe story-own-commit scoping; keep declared exclusions for genuine `Scope-Override:` paths |
| `tools/check-story-review-readiness.py` | No derivation exists for the story's own commits; `--derive-cumulative` self-skips on `main` | Add `--derive-story-commits`; allow it on the default branch |
| `tests/tooling/story_review_readiness/` | 45 tests, none covering trailer-scoped derivation | Extend |
| `.githooks/commit-msg:43` | Passes `--derive-cumulative` | Switch to the new mode |
| `.github/workflows/ci.yml:168` | Passes the PR range verbatim; deliberately correct today | **No change** — see §3 rationale |
| `CONTRIBUTING.md:223-268` | Documents `--derive-cumulative` as the local flag | Update |
| `24-6-…md` / `spec-24-6-…md` | Record a 30/34 reconciliation and twelve exclusions | Re-record under 24.6's ownership |
| **PRD** | `grep -ciE "file list\|reconcil" prd.md` → **0** | No conflict |
| **Architecture** | Same grep → **0** | No conflict |
| **UX** | Governance tooling, no user-facing surface | N/A |

Checklist §3.1 `[x] Done` (no PRD conflict, MVP unaffected), §3.2 `[x] Done` (no
architecture conflict), §3.3 `[N/A]`, §3.4 `[x] Done` (CI/CD, hooks, testing,
documentation enumerated above).

### Technical impact

Python 3 stdlib only, matching the existing verifier. `git interpret-trailers
--parse` is already a hard dependency of the tool (`check-story-review-readiness.py:257-277`).
Runtime cost is one `git log` plus one `git show` per commit in range — 27 commits
for Story 24.6, 161 for Story 27.3; the 27.3 derivation was measured under two
seconds. No production code, no runtime behavior, no contract surface is touched.

---

## 3. Recommended Approach

**Selected path: Option 1 — Direct Adjustment (hybrid execution).** Effort:
**Medium**. Risk: **Low**.

Add a story-own-commit derivation mode to the existing verifier, amend the policy it
enforces, and let Story 24.6 close E-P3 by applying the new rule.

### Why not the alternatives

- **Option 2, Rollback** — `[ ] Not viable`. No completed work is the cause; the
  defect is in a governance rule. Reverting Story 24.7 would neither fix the rule nor
  help Stories 27.3/31.1/31.2.
- **Option 3, PRD MVP review** — `[ ] Not viable`. The PRD contains zero references
  to File List reconciliation. MVP scope is untouched.
- **Policy-prose-only amendment** — rejected on evidence. This defect family has been
  restated four times inside one story and closed each time by a hand-written bullet;
  L13 is explicit that a rule recurring unchanged must be made executable.

### The derivation

The story's changed set is:

> the union of the **first-parent** diffs of every commit in `baseline_commit..HEAD`
> whose `Story:`/`Story-Key:` trailer resolves to this story key, plus the unstaged,
> staged, and untracked worktree sets.

Every remaining path in the range is **auto-attributed**: the gate reads the owning
story key and commit SHA from the foreign commit's own trailer and reports it. No
human writes that line.

First-parent handling is load-bearing. Story 24.6 has two merge commits; diffing a
merge against *both* parents pulls in everything `main` did meanwhile and inflates
the set from 19 to 24 paths, re-importing four submodule pointers and one foreign
spec. First-parent gives the changes the merge actually introduced.

### Measured result on Story 24.6, today, before any code is written

```
raw baseline..HEAD union                         34 paths → readiness EXIT=1 (4 unaccounted)
story-own-commit union (first-parent) + worktree 19 paths → readiness EXIT=0
```

The 19 paths are exactly the 18 declared File List entries **plus one exclusion** —
`tests/tooling/bmad_customization/bmad_customization_test.py`, the genuine
`Scope-Override:` carried in Story 24.6's own commit `c64e5514`. The other eleven
exclusion bullets exist solely to absorb other stories' commits.

All 27 commits in range carry a `Story-Key:` or `Story:` trailer; the prototype
reported **zero** unattributable paths. The mechanism has no gap on this repository's
*committed* history.

### Known limit — worktree paths cannot be trailer-attributed

Found by re-running the derivation after this proposal file was written, not by
reading the design. An uncommitted path has no commit and therefore no trailer, so
the unstaged/staged/untracked component of the union is always credited to the
resolved story. Writing this very artifact moved Story 24.6's derived set from 19 to
20 and the gate to exit 1:

```
C1: '…/sprint-change-proposal-2026-08-14-story-own-commit-file-list-scoping.md'
     changed but is not in the File List.
```

That is *correct* behaviour — an unaccounted path in the worktree of a governed story
must fail closed — but it means a `correct-course` or cross-story session leaves an
artifact that inflates the in-flight story's set until it is committed.

**Resolution: commit before reconciling.** Once this proposal is committed under its
own `Story-Key: spec-story-own-commit-file-list-scoping` trailer, the derivation
attributes it away automatically and Story 24.6 returns to 19. No exclusion bullet is
needed, and no special case is added to the tool.

This limit is stated in the policy amendment (proposal E) and asserted by a fixture
(proposal B) so it is never rediscovered as a defect.

### What auto-attribution independently confirms

Running the attribution prototype reproduces eighth-pass Decision **E-D2**'s
resolution without a human writing it:

```
references/Hexalith.FrontComposer <- spec-pushall-sync-2026-08-13,
                                     spec-pushall-sync-2026-08-14,
                                     spec-submodule-bumps-2026-08-11
                                     (30104162, c58d6431, cb863b46, 09b3eb61)
references/Hexalith.Builds        <- spec-pushall-sync-2026-08-14,
                                     spec-submodule-bumps-2026-08-11
                                     (30104162, cb863b46, 09b3eb61)
```

E-D2 was resolved as "enumerate all owners per path"; the derivation *is* that
enumeration. One correction falls out: E-D2's text places `Hexalith.Builds` under
"the first and the last" envelope, but `cb863b46` also moved it, so the recorded
enumeration is incomplete by one commit. The derived form carries it automatically.

### Why CI is deliberately left alone

`ci.yml:168` passes the pull-request `base..head` set, and its inline comment states
that `--derive-cumulative` is omitted on purpose because *"the PR base..head set IS
the story diff."* That reasoning is sound and this change does not disturb it. The
defect lives in the **default-branch and manual/agent invocation**, which is where
every one of the four recurrences happened. Changing CI would add risk without
closing anything.

Checklist §4.1 `[x] Viable` · §4.2 `[ ] Not viable` · §4.3 `[ ] Not viable` · §4.4
`[x] Done`, selected **Option 1 (Hybrid)**.

---

## 4. Detailed Change Proposals

Presented in **Batch** mode as elected. Ownership is split deliberately: proposals
**A–F** belong to a new standalone spec; proposal **G** belongs to Story 24.6 and is
executed under Story 24.6's own phase ledger.

### Group 1 — Tooling

#### A. `tools/check-story-review-readiness.py` — add `--derive-story-commits`

**Section:** `parse_args`, `derive_cumulative_changed`, `validate`

**OLD** (`:645-670`, abridged)

```python
def derive_cumulative_changed(baseline: str, supplied: list[str]) -> tuple[list[str], str | None]:
    """Union of baseline..HEAD, the staged set, and any supplied paths. …"""
    branch = current_branch()
    if branch in DEFAULT_BRANCHES:
        return supplied, (
            f"C1 SKIPPED: HEAD is on default branch '{branch}', where baseline..HEAD "
            "returns unrelated work. C1 is enforced in CI against the PR diff."
        )
```

**NEW** — add a sibling `derive_story_commit_changed(baseline, story_key, supplied)` that:

1. lists `baseline..HEAD`, reads each commit's trailer via the existing
   `parse_trailers`, and partitions commits into **own** (trailer resolves to the
   story key) and **foreign**;
2. unions each own commit's `git show --name-only --format= --first-parent <sha>`
   with the unstaged, staged, and untracked worktree sets;
3. attributes every foreign path not in the own set to `(story key, [SHAs])` read
   from the foreign commit's own trailer;
4. **runs on the default branch** — the skip is no longer needed, because the derived
   set no longer contains unrelated work.

`validate` prints the attribution block as informational output and does **not**
fail on it:

```
C1: all 19 changed paths are declared.
C1 external (auto-attributed, not story-owned): 15 paths
  references/Hexalith.FrontComposer <- spec-pushall-sync-2026-08-13, … (30104162, …)
  …
```

**Fail-closed conditions preserved and extended:**

- an empty derived set on a governed story still fails closed
  (`:851-855`), with a message naming the actual cause — *no commit in
  `baseline..HEAD` carries this story's trailer and the worktree is clean* — so
  Story 31.1's empty result is diagnosable rather than mysterious;
- a foreign commit carrying **no** trailer cannot be attributed; its paths stay in
  the comparison set and must be declared or excluded by hand, exactly as today.

**Rationale:** makes the reconciliation rule executable instead of hand-counted, and
retires the documented default-branch blind spot at `sprint-status.yaml:738`.

#### B. `tests/tooling/story_review_readiness/story_review_readiness_test.py` — extend

**OLD:** 45 tests, verified green at HEAD; none exercises trailer-scoped derivation.

**NEW:** add fixtures covering — own-commit selection by both `Story:` and
`Story-Key:` spellings; **first-parent merge handling** (the 19-vs-24 case, asserted
directly, since this is the subtlest part); auto-attribution of a foreign path to its
owning key and SHA; multi-owner attribution for one path; an untrailered foreign
commit remaining in the comparison set; empty-derived-set fail-closed with the new
message; and default-branch execution no longer skipping.

**Rationale:** L13 — *"Measure the check against live artifacts before wiring it."* A
proposed File List/File Scope check was approved, implemented, then refuted by
running it. First-parent merge handling is the equivalent trap here.

#### C. `.githooks/commit-msg:43` — switch derivation mode

**OLD**

```bash
"$py" tools/check-story-review-readiness.py --branch-name "$branch_name" --commit-message-file "$message_file" --changed-files-file "$changed_file" --derive-cumulative
```

**NEW**

```bash
"$py" tools/check-story-review-readiness.py --branch-name "$branch_name" --commit-message-file "$message_file" --changed-files-file "$changed_file" --derive-story-commits
```

**Rationale:** the hook is the local surface where `--derive-cumulative` currently
self-skips on `main`. This is where the change bites.

#### D. `.github/workflows/ci.yml` — **no change**

Recorded explicitly so a later reader does not read the omission as an oversight. The
PR range is already the story diff; `:165-168`'s comment says so and remains true.

### Group 2 — Policy and documentation

#### E. `_bmad/custom/story-phase-ledger.md` — amend "Cumulative File List Reconciliation"

**Section:** `:102-116`

**OLD**

> At every phase, compare the cumulative story-scoped changed-file set with the story
> File List. […] Record `matched N/N` in the phase row together with the declared
> comparison baseline, the name-status/diff command or artifact, and any named
> exclusions with owner and reason.

**NEW** — same obligation, redefined comparison set:

> The cumulative story-scoped changed-file set is the union of the first-parent diffs
> of every commit in `baseline_commit..HEAD` whose `Story:`/`Story-Key:` trailer
> resolves to this story, plus the unstaged, staged, and untracked worktree sets. A
> commit in range owned by another story is **not** part of this story's set and does
> not require an exclusion bullet; the gate attributes it from its own trailer.
>
> A declared exclusion remains required for a path this story's **own** commits
> changed but that the story does not own — the `Scope-Override:` case. A commit in
> range carrying no story trailer cannot be attributed; its paths stay in the
> comparison set and must be declared or excluded.
>
> Record `matched N/N` with the declared baseline, the derivation command, and any
> named exclusions with owner and reason.

Add to "Executable Gate" (`:146-167`), replacing the default-branch carve-out:

> `--derive-story-commits` runs on the default branch. The carve-out that skipped C1
> there existed because `baseline..HEAD` returned unrelated work; story-own-commit
> scoping removes that cause. The check remains skipped only outside
> `in-progress`/`review`.

**Rationale:** the policy text is the defect's origin. Fixing the tool without fixing
the prose leaves the next agent reconciling against the raw union.

#### F. `CONTRIBUTING.md:223-268` — update the Story Review Readiness section

Replace the `--derive-cumulative` guidance with `--derive-story-commits`, document
auto-attribution, and narrow the exclusion example to the `Scope-Override:` case it
now actually covers:

**OLD**

```markdown
- `path/to/file` — owner: Another Story; concurrent work, not credited here
```

**NEW**

```markdown
- `path/to/file` — owner: <name>; carried under this story's own commit <sha> with a
  `Scope-Override:` trailer, not credited to this story
```

with a note that concurrent work owned by another story is auto-attributed and needs
no bullet.

**Rationale:** the current example teaches the hand-count this change removes.

### Group 3 — Story 24.6 (executed under Story 24.6's ownership)

#### G. Close E-P3 by adopting the derivation

**Files:** `24-6-graph-content-level-tenant-isolation-evidence.md`,
`spec-24-6-graph-content-level-tenant-isolation-evidence.md`,
`sprint-status.yaml:426`

1. Re-record `## File List` reconciliation as **`matched 19/19`** — 18 story-owned
   paths plus one retained exclusion — with the declared baseline `0ecdffed`, the
   derivation command, and the auto-attributed external list.
2. Remove the eleven exclusion bullets that exist only to absorb other stories'
   commits; retain `tests/tooling/bmad_customization/bmad_customization_test.py`.
3. Append the `code-review` ledger row recording the re-reconciliation, phase delta
   `+0 test cases / +0 test methods`, and the E-P3 reopen trigger discharged.
4. Correct the seven stale "30-path" assertions catalogued by **E-P7** to the derived
   figure.
5. Record E-D2 as satisfied by derivation, noting the `cb863b46` addition to
   `Hexalith.Builds`.

**Not closed by this proposal:** E-P1 (`epic-24-context.md` anchor loss), E-P2
(integration-fast evidence predating its assertions), and the remaining 24 eighth-pass
patch items. Those are independent blockers on Story 24.6's `done` transition and are
untouched here.

**Sequencing note:** Story 24.6 is not blocked on proposals A–F shipping. The
derivation reproduces today with plain `git`, and that command is what proposal G
records as evidence:

```bash
{ git log --format='%H' <baseline>..HEAD \
    | while read c; do git log -1 --format='%B' "$c" | git interpret-trailers --parse \
        | grep -qiE '^story(-key)?: *<story-key>$' && git show --name-only --format= --first-parent "$c"; done
  git diff --name-only HEAD
  git ls-files --others --exclude-standard
} | sed '/^$/d' | sort -u
```

---

## 5. Implementation Handoff

**Scope classification: Moderate.** No epic or PRD change, but the work spans a
governance policy, a shared verifier, its fixture lane, a git hook, contributor
documentation, and one in-flight story's record.

### Registration

A new standalone spec, `spec-story-own-commit-file-list-scoping.md`, following the
precedent of `spec-executable-pre-review-story-gate.md` (2026-07-28): a
governance/tooling change with its own frozen `## Intent`, **no `development_status`
row**, closing the limit recorded at `sprint-status.yaml:738`.

No numbered story is created, renamed, or split. Story 24.6's slice is graph
content-level tenant isolation evidence; a readiness-gate tooling change does not
belong inside it.

### Recipients

| Role | Deliverable |
| :--- | :---------- |
| **Developer agent** (`bmad-build`) | Proposals A–F under the new spec: derivation mode, fixtures, hook, policy, CONTRIBUTING |
| **Developer agent** (Story 24.6 next phase) | Proposal G under Story 24.6's phase ledger |
| **Administrator** | Approval of this proposal; ruling on any `unverifiable` verdict |

### Success criteria

1. `check-story-review-readiness.py --story-key 24-6-… --derive-story-commits` exits
   **0** on the default branch, printing `matched 19/19` and the auto-attributed
   external block — **after this proposal is committed under its own trailer**. Run
   against the current uncommitted worktree it derives 20 and exits 1 on this
   proposal's own path; see the worktree limit in §3.
2. The same command on Story 27.3 derives from 6 commits rather than 1,336 raw paths.
3. Stories 31.1 and 31.2 fail closed with the *new* diagnostic message naming zero
   trailered commits — not a silent skip.
4. `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'`
   green, with new tests including the first-parent merge case.
5. Story 24.6's E-P3 reopen trigger is discharged; E-P1, E-P2, and the remaining
   patch items stay open on their own merits.

### Sequencing

A → B (in step) → C → E → F. G runs independently, at any time.

---

## Appendix I — Epic AC Verification

Per `_bmad/custom/epic-ac-verification.md`. Verified **2026-08-14** against HEAD
`30104162`. Every verifiable claim this proposal asserts or inherits, with a
re-runnable command.

| Claim | Class | Command / evidence | Observed | Verdict |
| :---- | :---- | :----------------- | :------- | :------ |
| "Story 24.7 landed inside 24.6's baseline range" | Existence | `git log --oneline 0ecdffed..HEAD` | `0f577c48`, `dc5fde62` present; 24.7's own `baseline_commit` is `8feb2a2d`, inside the range | confirmed |
| 24.6 raw union is 34 paths | Quantitative | `{ git diff --name-only 0ecdffed..HEAD; git diff --name-only HEAD; git ls-files --others --exclude-standard; } \| sort -u \| wc -l` | 34 | confirmed |
| Readiness exits 1 naming four paths | Behavioral | `check-story-review-readiness.py --story-key 24-6-… --changed-files-file <raw 34>` | exit 1; the four E-P3 paths | confirmed |
| Story-own-commit union is 19 and passes | Quantitative + Behavioral | derivation command in §4-G, then readiness with `--changed-files-file` | 19 paths; `C1: all 19 changed paths are declared.` exit 0 | confirmed |
| The 19 are 18 File List paths + 1 exclusion | Quantitative | `comm` of derived set against `## File List` | 18 + `bmad_customization_test.py` | confirmed |
| Every commit in range carries a story trailer | Existence | attribution prototype over `0ecdffed..HEAD` | 27/27 attributed; unattributable = **none** | confirmed |
| Worktree paths join the story's own set | Behavioral | re-ran the derivation after writing this proposal | derived set 19 → **20**; readiness exits 1 on this proposal's path | confirmed — recorded as a stated limit in §3, resolved by committing under its own trailer |
| First-parent handling changes the result | Behavioral | `git show --first-parent` vs `-m` over the 13 own commits | 19 vs 24 paths; `-m` adds 4 submodules + 1 foreign spec | confirmed |
| Fourth recurrence (D1, F5-P1, F6-P1, E-P3) | Existence | `grep -nE 'D1\.\|F5-P1\|F6-P1\|E-P3' spec-24-6-…md` | `:260`, `:423`, `:602`, `:735` | confirmed — with the nuance that F6-P1's proximate defect is an index-blind command; same family, not identical wording |
| C1 self-skips on the default branch | Behavioral | `check-story-review-readiness.py:653-658`; `sprint-status.yaml:738` HONEST LIMITS | skip confirmed in code and in the closing record | confirmed |
| Gate is wired into hook and CI | Location | `grep -rn check-story-review-readiness` | `.githooks/commit-msg:43`; `.github/workflows/ci.yml:168` | confirmed |
| Fixture lane is green | Quantitative | `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'` | `Ran 45 tests … OK` | **corrected** — `sprint-status.yaml:738` records `37/37` at closure; the lane is now 45. Historical row left intact per `CONTRIBUTING.md:218-221`; observed value recorded here per L14. |
| Story 27.3: 161 commits, 6 own, 1,336 raw paths | Quantitative | in-flight sweep, §1 | as stated | confirmed |
| Stories 31.1/31.2 have zero trailered commits | Absence | `git log --format='%H %B' --all \| grep -ciE '^story(-key)?: *31-[12]-…'` | 0 and 0 | confirmed |
| E-D2's owner enumeration matches derivation | Quantitative | attribution prototype vs `spec-24-6-…md` E-D2 resolution | FrontComposer's four commits match exactly; **Builds is short one commit** (`cb863b46`) | **corrected** — corrected in proposal G, which is the E-D2-owning artifact |
| PRD and architecture carry no File List rule | Absence | `grep -ciE "file list\|reconcil" prd.md architecture.md` | 0 and 0 | confirmed |
| Precedent: governance spec carries no `development_status` row | Existence | `grep -n executable-pre-review sprint-status.yaml`; `spec-executable-pre-review-story-gate.md:1-13` | referenced only from action-item rows; no `development_status` entry | confirmed |

No `unverifiable` rows. Both `corrected` rows carry their planning-artifact
correction: the fixture-count correction is recorded here and left un-backdated in
the historical row by design; the E-D2 correction is assigned to proposal G, which
owns that artifact. Neither correction changes scope, epic intent, or a ratified
decision, so neither requires escalation under `epic-ac-verification.md:101-104`.

## Appendix II — Historical Context Classification

Per `_bmad/custom/story-scope-guard.md`. This correction creates **no numbered
story**, so the creation gate binds on nothing registered in `epics.md` or
`development_status`. The classification is recorded regardless, because prior
artifacts influenced this proposal.

| Prior artifact | Classification | Basis |
| :------------- | :------------- | :---- |
| `spec-executable-pre-review-story-gate.md` (2026-07-28) | `current-narrow-pattern` | Only the wiring pattern is reused — stdlib verifier + `commit-msg` hook + `tests/tooling/<name>/` fixture lane + policy amendment + CONTRIBUTING section — re-verified at HEAD by reading `check-story-review-readiness.py`, `.githooks/commit-msg:43`, `ci.yml:168`, and running the 45-test lane. Its whole-artifact shape is not reused. |
| Story 24.6 | `historical-reference-only` | Supplies the trigger, the four-recurrence evidence, and the measured before/after. Its scope, task structure, and umbrella/checkpoint shape are not templates for this work. |
| Story 24.7 | `historical-reference-only` | Named as the concurrent owner in the evidence. No structure reused. |
| Stories 26.5 / 22.2 (cited by L13) | `historical-reference-only` | Cited only as the failure evidence that motivated the executable gate. |
| Story 31.1's checkpoint split | `anti-template` | Permitted use: cited **only** as an example of a bundled checkpoint-heavy shape this proposal must not reproduce. No tasks, AC density, file list, or proof shape carried forward. |

## Appendix III — Slice Proof

**One independently demonstrable outcome:** *the story review-readiness gate
reconciles a story's File List against that story's own commits.*

Demonstrated by a single observable command — `--derive-story-commits` exits 0 on
Story 24.6 on the default branch at `matched 19/19`, where the raw-union form exits 1
at 30/34.

Proposals B–F are **delivery surfaces of that one outcome**, not independent
outcomes: fixtures prove it, the hook invokes it, the policy states it, CONTRIBUTING
documents it. None ships value on its own and none is separately deployable, so this
is not a bundled umbrella and requires no checkpoint table.

Proposal G is deliberately **excluded from this slice** and assigned to Story 24.6.
Applying the rule to a story's record is that story's work; folding it in here would
make this artifact carry two outcomes — the exact shape `story-scope-guard.md:36-40`
forbids a correction from reproducing.

*This judgement is recorded so a reviewer can challenge it directly: if the delivery
surfaces are read as independent gates, the count exceeds five and a checkpoint table
with per-row owner, evidence, review state, and completion state becomes mandatory.*

## Appendix IV — Change Navigation Checklist

| Item | Status | Note |
| :--- | :----- | :--- |
| 1.1 Triggering story | `[x] Done` | Story 24.6, eighth-pass review, blockers E-P3/E-P4 |
| 1.2 Core problem | `[x] Done` | Technical limitation in a governance rule discovered during implementation |
| 1.3 Evidence | `[x] Done` | Appendix I; measured before/after; five-story sweep |
| 2.1 Current epic completable | `[x] Done` | Yes, unchanged |
| 2.2 Epic-level changes | `[N/A]` | None |
| 2.3 Remaining epics reviewed | `[x] Done` | All benefit; none requires change |
| 2.4 Epics invalidated / new needed | `[N/A]` | None |
| 2.5 Resequencing | `[N/A]` | None |
| 3.1 PRD conflicts | `[x] Done` | Zero matches; MVP unaffected |
| 3.2 Architecture conflicts | `[x] Done` | Zero matches |
| 3.3 UI/UX conflicts | `[N/A]` | No user-facing surface |
| 3.4 Other artifacts | `[x] Done` | Verifier, fixtures, hook, CI (no-change, recorded), policy, CONTRIBUTING |
| 4.1 Direct Adjustment | `[x] Viable` | Selected. Effort Medium, Risk Low |
| 4.2 Rollback | `[ ] Not viable` | No completed work is the cause |
| 4.3 PRD MVP review | `[ ] Not viable` | MVP untouched |
| 4.4 Path selected | `[x] Done` | Option 1, hybrid execution |
| 5.1 Issue summary | `[x] Done` | §1 |
| 5.2 Epic/artifact impact | `[x] Done` | §2 |
| 5.3 Recommended path | `[x] Done` | §3 |
| 5.4 MVP impact + action plan | `[x] Done` | MVP unaffected; §4 |
| 5.5 Handoff plan | `[x] Done` | §5 |
| 6.1 Checklist complete | `[x] Done` | This table |
| 6.2 Proposal accuracy | `[x] Done` | Every claim carries a command in Appendix I |
| 6.3 User approval | `[x] Done` | Approved as written by the Administrator, 2026-08-14, with no conditions |
| 6.4 `sprint-status.yaml` update | `[N/A]` | No epic or story added, removed, or renumbered; the spec carries no `development_status` row by precedent |
| 6.5 Next steps confirmed | `[x] Done` | Handoff recorded in §5: A–F to `spec-story-own-commit-file-list-scoping.md`, G to Story 24.6's next phase |

## Approval and Handoff Log

- **2026-08-14** — Approved as written by the Administrator via `bmad-correct-course`,
  Batch mode, with no conditions and no revisions requested.
- **Scope classification:** Moderate — no epic or PRD change, but the work spans a
  governance policy, a shared verifier, its fixture lane, a git hook, contributor
  documentation, and one in-flight story's record.
- **Routed to:** Developer agent (`bmad-build`) for proposals A–F under a new
  standalone `spec-story-own-commit-file-list-scoping.md`; Developer agent for
  proposal G under Story 24.6's own phase ledger.
- **Guards cleared before approval was recorded:** `story-scope-guard.md` — no story
  created, renamed, or split; five prior influences classified (Appendix II); single
  outcome proven (Appendix III). `epic-ac-verification.md` — 15 verifiable claims,
  13 `confirmed`, 2 `corrected` with their planning-artifact corrections assigned,
  0 `unverifiable` (Appendix I).
- **`sprint-status.yaml`:** intentionally unmodified. No epic or story was added,
  removed, or renumbered, and the governance spec carries no `development_status`
  row by the `spec-executable-pre-review-story-gate.md` precedent.
