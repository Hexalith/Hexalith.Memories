# Sprint Change Proposal — Executable Pre-Review Story Readiness Gate

- **Date:** 2026-07-28
- **Author:** Amelia (Developer), with Administrator
- **Workflow:** `bmad-correct-course` (Incremental mode)
- **Baseline commit:** `115d30b59101910d0fd30717f49a5fb7f1782547`
- **Trigger:** Retrospective action item, Epic 24 — *"Implement an executable pre-review guard or checklist for remediation stories covering File List completeness, evidence-table status, story/spec status, and sprint-status sync"*
- **Scope classification:** **Moderate** — no epic, PRD, architecture, or UX change; new governance tooling plus backlog/action-ledger reorganization.

---

## 1. Issue Summary

### Problem statement

The same retrospective action item has been recorded, restated, and carried forward across **four consecutive epics** without ever becoming executable. Each epic applied it as a *convention* — propagated through story Dev Notes and reviewer habit — and each epic's review still caught the drift the convention was supposed to prevent.

The chain, verbatim from `_bmad-output/implementation-artifacts/sprint-status.yaml`:

| Epic | Action item | Line | Status |
|---|---|---|---|
| 22 | "Add an explicit File List and evidence-table pre-review check to future remediation story close-out" | 647 | in-progress |
| 23 | "Turn File List and evidence-table checking into a pre-review source guard or checklist that runs before remediation story review" | 651 | in-progress |
| 24 | "Implement an executable pre-review guard or checklist for remediation stories covering File List completeness, evidence-table status, story/spec status, and sprint-status sync" | 671 | in-progress |
| 25 (#7) | "Turn the propagated review-discipline convention (File List completeness, constant-tied drift guards) into an executable pre-review gate; merges the still-open Epic 24 checklist item" | 719 | open |

### Issue category

**Failed approach requiring a different solution.** This is not a new requirement, a stakeholder change, or a misunderstanding. The requirement has been correctly stated four times. The *solution class* — prose convention carried forward by discipline — has failed four times in the same way.

### Evidence

1. **`sprint-status.yaml:653`** (Epic 23 item, Epic 24 assessment): *"review checks continued to catch hygiene gaps; executable pre-review guard still needed."*

2. **`sprint-status.yaml:671`** (Epic 24 item, Epic 25 assessment): *"applied as a convention and propagated 25.1->25.2->25.3, but reviews still caught falsely-passing drift guards; not yet an executable gate."*

3. **`epic-25-retro-2026-07-12.md`, Challenges #5:** *"Drift-guard tests that passed falsely were a repeated review finding. Reviewers caught weak guards in six-plus stories (25.1 route-source scan, 25.2 centralization guard, 25.3 route-surface regex, 25.5 DI-payload discovery, 25.6 IL decoding, 25.8 nuspec/inventory). … 'the guard passes without proving the thing' was the epic's dominant review signature."*

4. **`epic-25-retro-2026-07-12.md`, follow-through table:** *"Applied as a **convention** and propagated forward (25.1 → 25.2 → 25.3 Dev Notes), but review still repeatedly caught weak drift-guards and status drift. **Still not an executable gate.**"*

5. **The decisive evidence.** `_bmad/custom/story-phase-ledger.md` (landed 2026-07-16 under `spec-track-test-counts-in-story-phase-change-log.md`) **already encodes these exact rules, fail-closed**, including a "Cumulative File List Reconciliation" section and explicit "Fail-Closed Status Gates" for `ready-for-dev`, `review`, and `done`. Drift continued anyway. The rules are not missing and are not wrong — they are unenforced. Enforcement is LLM-obeyed prose loaded through a skill override, and an agent that does not read carefully simply does not comply.

### What is *not* wrong

The existing prose policies are correct and are **retained unchanged**. This proposal does not replace `story-phase-ledger.md`, `story-scope-guard.md`, or `remediation-runtime-checklist.md`. It makes the mechanically checkable subset of one of them executable, and leaves every judgement-requiring rule where it is.

---

## 2. Impact Analysis

### Epic impact — none

| Check | Finding |
|---|---|
| Current epic containing the trigger | No epic owns this work. It originates from retrospectives, not from story execution. |
| Epic scope / AC modification | None required. |
| New or removed epic | None. Registered as a **non-epic spec**, matching precedent (`spec-track-test-counts-in-story-phase-change-log.md`, `spec-remediation-runtime-defect-checklist.md`). |
| Remaining planned epics | Epics 27, 29, 30, 31 are *governed by* the gate, not changed by it. |
| Epic order / priority | Unchanged. |

**One real coupling.** Stories 27.3, 31.1, and 31.2 currently record File List exclusions as free prose (4, 2, and 1 annotation lines respectively). A parser cannot honour free prose, so those artifacts require a declared-exclusion block before the gate can evaluate them. See §4, EP-5.

### Story impact

The governed set is small, current, and entirely post-2026-07-16. The canonical ledger header `| Date | Phase | Change | Test count | File List reconciliation |` appears in **8 of 228** artifacts:

| Artifact | Status | Gate effect |
|---|---|---|
| `27-1-access-telemetry-retention-ownership-decision.md` | done | C2/C3/C4 verified; C1 skipped |
| `27-2-bounded-retention-ttl-and-purge-implementation.md` | done | C2/C3/C4 verified; C1 skipped |
| `27-3-production-adapter-and-deployment-profile.md` | in-progress | **Full gate.** Retrofit required (48 declared paths, 4 layered correction notes) |
| `29-1-openbao-backed-apphost-secret-topology.md` | done | C2/C3/C4 verified; C1 skipped |
| `31-1-openbao-platform-hardening-and-documentation.md` | review | **Full gate.** Retrofit required — **deferred until its review closes** |
| `31-2-runtime-dapr-secret-store-migration.md` | ready-for-dev | Retrofit required (untracked; cheapest) |
| `spec-track-test-counts-in-story-phase-change-log.md` | done | C2/C3 verified |
| `spec-infrastructure-dependency-abstraction.md` | in-progress | C2/C3 verified |

The other 220 artifacts carry no ledger and are **no-ops** — the gate exits 0 on them, exactly as `check-story-file-scope.py` no-ops on a story with no `## File Scope` section. There is no historical retrofit.

### Artifact conflicts

| Artifact | Conflict | Status |
|---|---|---|
| PRD (`prd.md`) | None. Process governance; MVP scope, goals, and requirements untouched. | N/A |
| Architecture (`architecture.md`) | None. No component, pattern, technology, data model, API contract, or integration point changes. | N/A |
| UX / UI specifications | None. | N/A |
| **CI/CD pipelines** | `.github/workflows/ci.yml` — one step in `story-file-scope`, one discover step in `test-unit-contract`. | Action-needed |
| **Git hooks** | `.githooks/commit-msg` — third gate invocation. | Action-needed |
| **Testing strategy** | New `tests/tooling/story_review_readiness/`. | Action-needed |
| **BMAD skill overrides** | `_bmad/custom/story-phase-ledger.md`, `bmad-dev-story.toml`, `bmad-code-review.toml`, `tests/tooling/bmad_customization/`. | Action-needed |
| **Documentation** | `CONTRIBUTING.md`, `_bmad-output/process-notes/story-creation-lessons.md`. | Action-needed |
| **Sprint tracking** | `sprint-status.yaml` — four action items updated. | Action-needed |

### Technical impact

New standalone stdlib-only Python verifier. No product code, no dependency, no build change. Blast radius is the commit path and one CI job.

### Verified current state

Both claims below were checked against the tree at `115d30b5`, not assumed:

- **All six governed stories are currently in status sync** (`Status:` ↔ `development_status`). C4 passes on today's tree. This gate is **preventive**, not a cleanup of existing drift — do not expect it to find anything on day one.
- **All eight governed artifacts carry `baseline_commit`** in frontmatter, in three formats the parser must accept: bare SHA, single-quoted SHA, and abbreviated 8-character (`27-1`: `119c0a49`).

---

## 3. Recommended Approach

### Path evaluation

| Option | Verdict | Effort | Risk | Rationale |
|---|---|---|---|---|
| **1. Direct Adjustment** | **Selected** | Medium | Low-Medium | One new spec plus implementation. No story rewrites, no epic surgery, no replan. |
| 2. Potential Rollback | Not viable | — | — | Nothing to roll back. The prose policies are correct and are retained; the gate makes a checkable subset executable. |
| 3. PRD MVP Review | Not applicable | — | — | MVP unaffected. This is process governance. |

### Justification

Direct Adjustment is the only option that engages the actual failure. The requirement is already agreed four times over; what is missing is a mechanism that does not depend on an agent choosing to comply. Effort is bounded (one verifier, one test suite, three wiring edits, three retrofits). Risk is concentrated in false positives on shared dirty worktrees, which the declared-exclusion block and the status-window scoping in §4 address directly.

### Risk assessment

| Risk | Mitigation |
|---|---|
| False positives on shared/dirty worktrees | Machine-readable `### File List Exclusions` block with named owner and reason per path. |
| `baseline..HEAD` returning unrelated work for `done` stories | C1 evaluates only when Status ∈ {in-progress, review}. CI uses the PR base..head diff verbatim. |
| CRLF parse failures | `read_text_lf()` strips `\r` before any parse; byte-level CRLF fixtures in the suite. |
| Green gate misread as full verification | Explicit scope-limit paragraph in `story-phase-ledger.md`; the same wording repeated in both skill directives. |
| Gate rots as story format drifts | Liveness test asserts all six live governed stories exit 0 — format drift breaks CI the day it happens. |

### Timeline impact

None on Epics 27, 29, 30, or 31. The 31.1 retrofit is deliberately deferred until that story's review closes.

---

## 4. Detailed Change Proposals

All six proposals below were reviewed and approved individually in Incremental mode.

### EP-1 — The gate contract

`tools/check-story-review-readiness.py`. Story resolution identical to `check-story-file-scope.py`: `--story-key` > `Story:`/`Story-Key:` trailer > branch name, resolving to `_bmad-output/implementation-artifacts/<key>.md`. **Governed if and only if** the file contains the canonical ledger header; otherwise exit 0.

```
C1  FILE LIST COMPLETENESS          [runs only when Status ∈ {in-progress, review}]
    changed-set := cumulative story diff  minus  declared exclusions
    listed-set  := first backticked path per bullet under `### File List`
    exit 1 if changed-set ⊄ listed-set          ("changed but unlisted")
    exit 1 if a listed path is absent from the story's cumulative diff
                                                ("listed but unchanged / stale")

C2  EVIDENCE-TABLE STATUS
    exit 1 if no ledger row has Phase ∈ {create-story, dev-story,
            qa-gap-closure, code-review}
    exit 1 if Status ∈ {review, done} and no `dev-story` row exists
    exit 1 if Status = done and no `code-review` row exists
    exit 1 if any row's `Test count` or `File List reconciliation` cell is
            empty, "TBD", "N/A", or "-"
    exit 1 if the newest row's reconciliation cell lacks `matched N/N`
            or an explicit blocked-evidence record

C3  STORY / SPEC STATUS
    exit 1 if no `Status:` line (stories) or `status:` frontmatter key (specs)
    exit 1 if the value ∉ {backlog, ready-for-dev, in-progress, review, done}

C4  SPRINT-STATUS SYNC
    exit 1 if a story key has no `development_status:` row
    exit 1 if that row's value ≠ the story's Status
    spec-*.md: no row expected — absence is not a violation

Fail-closed: a governed story with no resolvable baseline_commit exits 1.
             "Unverifiable" must never render as "passed."
Bypass:      `Story-Review-Readiness-Bypass: <non-empty reason>` trailer,
             mirroring tools/check-tenant-isolation-evidence.py:69-190.
Exit codes:  0 pass/no-op, 1 violation. All output to stdout. Stdlib only.
```

**Deliberate limit.** C2 checks row *presence and non-placeholder shape*; it does **not** verify count arithmetic against a live test run. A commit hook cannot execute a .NET discovery pass. Arithmetic verification stays with the `story-phase-ledger` review layer in `bmad-code-review.toml`. A gate that provably cannot lie is worth more than one that pretends to verify counts.

### EP-2 — Verifier and test suite

`tools/check-story-review-readiness.py` (~520 lines) mirrors `check-story-file-scope.py` structurally. Reused verbatim from the sibling — there is no shared helper module, each verifier is standalone: `normalize_path`, `matches_glob`/`_glob_match`, `extract_story_keys`, `resolve_story_source`, `parse_trailers`, `class ValidationError`.

New: `read_text_lf`, `parse_status`, `parse_ledger`, `parse_file_list`, `parse_exclusions`, `parse_sprint_status`. CLI: `--story-key --branch-name --commit-message-file --changed-files-file --sprint-status-file`.

`tests/tooling/story_review_readiness/story_review_readiness_test.py`, ~38 stdlib `unittest` cases (siblings carry 42 and 34). `REPO_ROOT = Path(__file__).resolve().parents[3]`. Subprocess-driven CLI tests plus importlib white-box load (`sys.modules[spec.name] = module` before `exec_module`). `FROZEN_STORY_FIXTURE` is an inline snapshot of Story 31.1's shape so parser drift is not masked by later edits to live artifacts.

| Group | Cases |
|---|---|
| No-op | story without ledger → 0; unresolvable story key → 0 |
| C1 | changed-but-unlisted → 1; listed-but-unchanged → 1; exact match → 0; rename (old+new listed) → 0; declared exclusion honoured → 0; undeclared exclusion → 1; path normalisation (`./a`, `a//b`, backslash) → 0 |
| C2 | missing row → 1; `review` without `dev-story` → 1; `done` without `code-review` → 1; empty/`TBD`/`N/A`/`-` cell → 1 ×4; newest row missing `matched N/N` → 1; blocked-evidence accepted → 0; repeated phase → 0 |
| C3 | no `Status:` → 1; unknown value → 1; spec frontmatter → 0; each valid value → 0 |
| C4 | missing row → 1; mismatch → 1; **CRLF sprint-status parses clean** → 0; **CRLF story file parses clean** → 0; `spec-*.md` with no row → 0 |
| Bypass | reason present → 0; empty reason → 1; no reason → 1 |
| Liveness | all six live governed stories exit 0 after the EP-5 retrofit |

**The two CRLF cases are the highest-value tests in the suite.** During this analysis, an unstripped `\r` produced two false status MISMATCHes on the first attempt. That is precisely how this gate would ship broken-but-green. Fixtures are byte-level CRLF.

**The liveness test is what stops this becoming shelfware.** A synthetic-only suite passes forever while the real format drifts away from the parser.

### EP-3 — Enforcement wiring

**Correctness prerequisite: the staged set is not the story set.** `.githooks/commit-msg` builds its list with `git diff --cached --name-only` — one commit's staged files. C1 compares against the *cumulative* File List, so on commit 2 of a 3-commit story every file from commit 1 reports as "listed but unchanged." Anchoring is mandatory.

```
EDIT .githooks/commit-msg   (after the tenant-isolation invocation, line 25)

+ # Story review-readiness gate. Runs here for the same reason as the tenant
+ # gate: the Story: and bypass trailers live in the commit message, which
+ # pre-commit cannot see.
+ "$py" tools/check-story-review-readiness.py \
+     --branch-name "$branch_name" \
+     --commit-message-file "$message_file" \
+     --changed-files-file "$changed_file"
```

```
EDIT .github/workflows/ci.yml   (job story-file-scope, after line 153)

+      - name: Validate story review readiness
+        run: python3 tools/check-story-review-readiness.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"
```

Reuses the job's existing ~120-line diff-collection block and its `$BRANCH_NAME` / `$COMMIT_MESSAGE_FILE` / `$CHANGED_FILES_FILE`. No duplication. The job already checks out `fetch-depth: 0`.

```
EDIT .github/workflows/ci.yml   (job test-unit-contract, after line 243)

+      - name: Run story review readiness tooling tests
+        run: python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py"
```

Without this the suite runs in no lane at all — the exact gap Epic 25's retro recorded against `publish_nuget_test.py`.

| Enforcement point | Blocks | Bypassable by |
|---|---|---|
| `.githooks/commit-msg` | local commit | `--no-verify`, or the declared bypass trailer |
| `ci.yml` `story-file-scope` | the PR | declared bypass trailer only |
| `ci.yml` `test-unit-contract` | the PR, if the gate's own tests break | — |

`--no-verify` still exists locally, but CI re-runs the identical verifier on the same inputs, so a skipped hook surfaces at PR time rather than at review. **That is the structural difference from the previous four attempts.**

### EP-4 — Policy, template, and skill wiring

**Correction adopted from EP-2.** A bare `--story-key` invocation with an empty staged set must not exit 0 vacuously — that is a known weakness of the sibling gate. EP-3's anchoring prevents it, since the cumulative set stays non-empty with nothing staged. The test case inverts: a governed story whose *cumulative* set is empty exits 1.

```
EDIT _bmad/custom/story-phase-ledger.md
Under "## Cumulative File List Reconciliation":

+ ### Declared Exclusions
+
+ A named exclusion is only honoured in machine-readable form. Free prose in
+ the File List or a ledger cell records intent for a human reader; it does
+ not exempt a path from the executable gate.
+
+ ```markdown
+ ### File List Exclusions
+
+ - `path/to/file` — owner: <name>; <reason>
+ ```
+
+ Every bullet needs a backticked path, a named owner, and a reason. An
+ exclusion without all three is a violation, not an exclusion.
+
+ ### Executable Gate
+
+ `tools/check-story-review-readiness.py` enforces the mechanically checkable
+ subset of this policy: File List completeness in both directions, required
+ ledger rows and non-placeholder cells, a recognised story status, and
+ sprint-status agreement. It does NOT verify count arithmetic, discovery
+ evidence, or whether a recorded command was truly run — those remain with
+ the code-review ledger auditor. A green gate is a floor, never a ceiling.
```

That final paragraph is load-bearing. Without it a green gate becomes the *next* "reviews stopped looking" failure — the same shape as the `integration-fast` required-surfaces gate, which asserts a class is present in a TRX and is easily misread as proof it passed.

```
EDIT _bmad/custom/bmad-dev-story.toml       (append one directive)
EDIT _bmad/custom/bmad-code-review.toml     (append one directive)

+ "STORY_REVIEW_READINESS_GATE: Before setting review (dev-story) / before
+  synchronizing status to done (code-review), run
+  `python3 tools/check-story-review-readiness.py --story-key <key>` and record
+  the exact command and exit code in the story record. Exit 1 is a fail-closed
+  blocker. A green exit covers only File List completeness, ledger row
+  presence, status validity, and sprint-status agreement — it is not evidence
+  of count arithmetic or executed tests; continue to audit those independently."
```

Arrays append over the skill's base `customize.toml`, so the existing `HISTORICAL_SLICE_GUARD`, `STORY_PHASE_LEDGER`, and `REMEDIATION_RUNTIME_CHECKLIST` directives and both `review_layers` are preserved unchanged.

```
EDIT tests/tooling/bmad_customization/bmad_customization_test.py
Assert the new directive BODY (not merely the count) resolves for
bmad-dev-story and bmad-code-review across BOTH the .agents and .claude
surfaces — the shared _bmad/custom/*.toml feeds both. 14 cases → ~18.

EDIT CONTRIBUTING.md
New `## Story Review Readiness` section after `## Cross-Tenant Negative
Evidence` (line ~177): the four exit-1 classes, the exclusion-block format,
and the `Story-Review-Readiness-Bypass:` trailer.

EDIT _bmad-output/process-notes/story-creation-lessons.md
+ ## L12 - A Convention Repeated Across Four Epics Is a Missing Gate
Records the 22→23→24→25 chain and the rule: when a retro action item recurs
unchanged for two epics, stop restating it and make it executable.
```

### EP-5 — Retrofit the live governed set

**C1 revision forced by the live artifacts.** `baseline..HEAD` is only the story's diff on a story branch. For the three `done` artifacts, with baselines weeks old, that range now returns everything everyone has committed since — C1 would fabricate hundreds of phantom violations. The current branch is `main`, not a story branch, so the naive derivation is wrong on this very tree.

```
C1 runs when story Status ∈ {in-progress, review}
    → the pre-review window the action item actually names
C1 skips (printing a note) for {backlog, ready-for-dev, done}
C2, C3, C4 run for every governed status, always

Changed-set source, by caller:
  CI    : --changed-files-file verbatim (the job's PR base..head IS the story
          diff — no baseline derivation, no drift)
  local : git diff --name-status <baseline_commit>..HEAD ∪ staged
          exit 1 if HEAD is on the default branch and the derived set exceeds
          the File List beyond a configured margin — a wrong-branch commit
          must fail loudly, not silently pass
```

A `done`-status escape hatch is closed by C2 (done requires a `code-review` row) and C4 (sprint-status must agree).

| Artifact | Status | C1 | Work |
|---|---|---|---|
| `31-2-runtime-dapr-secret-store-migration.md` | ready-for-dev | skip | Add `### File List Exclusions`; convert the one `partial ownership` note on `sprint-status.yaml`. Untracked — cheapest retrofit. |
| `27-3-production-adapter-and-deployment-profile.md` | in-progress | **runs** | Heaviest: 48 declared paths, four layered correction notes (a code-review-added `references/Hexalith.Builds` exclusion, a withdrawn dev-story note). Convert to the block **without altering any disposition**. |
| `31-1-openbao-platform-hardening-and-documentation.md` | review | **runs** | **Deferred until its review closes.** |
| `27-1`, `27-2`, `29-1` | done | skip | Verify C2/C3/C4 pass. No exclusion prose present. |
| `spec-track-test-counts…`, `spec-infrastructure-dependency-abstraction` | done / in-progress | mixed | Verify C2/C3; C4 expects no `development_status` row for `spec-*`. |

**Hazard 1 — 31.1 is at `review` right now.** The worktree has 31.1, its evidence file, `epics.md`, and `sprint-status.yaml` dirty, plus two untracked course-correction proposals. Retrofitting a story mid-review injects this spec's changes into that story's diff and into whatever File List reconciliation its reviewer is currently computing. **Approved decision: defer 31.1 until its review closes.**

**Hazard 2 — the current branch is `main`, with submodule gitlink drift.** `references/Hexalith.Builds` and `references/Hexalith.EventStore` both show modified gitlinks. Per Epic 25's still-open submodule action item these must not ride along. This spec's work goes on a feature branch, and both gitlinks are reverted with `git submodule update -- <paths>` before any commit — otherwise the first thing this gate does is fail on its own commit.

### EP-6 — Tracking records

```
NEW _bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md

Frontmatter mirrors spec-track-test-counts-in-story-phase-change-log.md:
  title: 'Executable pre-review story readiness gate'
  type: 'feature'
  created: '2026-07-28'
  status: 'backlog'
  baseline_commit: '<HEAD at spec creation>'
  review_loop_iteration: 0
  context:
    - '{project-root}/_bmad-output/planning-artifacts/
       sprint-change-proposal-2026-07-28-executable-pre-review-story-gate.md'

<frozen-after-approval> Intent / Boundaries & Constraints / I/O & Edge-Case
Matrix </frozen-after-approval>, then Code Map, Tasks & Acceptance, and the
canonical ## Change Log ledger — so the spec is governed by the gate it
introduces.

NO development_status row. Verified against precedent: no spec-*.md carries
one; specs are referenced only from action-item evidence comments (the
remediation-runtime-checklist spec is cited that way at sprint-status.yaml:629).
```

**Action items are updated, not closed.** Closing them on an approved proposal would repeat exactly what the previous four epics did — record intent and call it delivered.

```
EDIT _bmad-output/implementation-artifacts/sprint-status.yaml

epic 22 "Add an explicit File List and evidence-table pre-review check…"
epic 23 "Turn File List and evidence-table checking into a pre-review source guard…"
epic 24 "Implement an executable pre-review guard or checklist…"
    status: in-progress   # unchanged status, new dated evidence comment
epic 25 item 7 "Turn the propagated review-discipline convention … into an
                executable pre-review gate"
    status: open -> in-progress

Shared comment:
  2026-07-28: superseded by the approved
  sprint-change-proposal-2026-07-28-executable-pre-review-story-gate.md and
  consolidated into spec-executable-pre-review-story-gate.md. Not closed —
  the proposal is approved, not implemented. Closes when
  tools/check-story-review-readiness.py is merged, wired into
  .githooks/commit-msg and both ci.yml jobs, and its tooling suite is green
  in test-unit-contract.
```

**Explicitly out of scope.** Epic 26's *"Establish an executable operational-review entry gate that requires realistic execution evidence, exact end-state assertions, fail-closed behavior, complete artifact inventory, and zero unapproved skips before review"* is a sibling but a different gate. It demands evidence-quality judgement a commit hook cannot make. It stays `open` and untouched. **This proposal closes four action items, not five.**

---

## 5. Implementation Handoff

### Scope classification: Moderate

New governance tooling plus action-ledger reorganization. No PRD, architecture, or UX involvement; no PM or Architect escalation required.

### Recipients and responsibilities

| Recipient | Responsibility |
|---|---|
| **Amelia (Developer)** | Author `spec-executable-pre-review-story-gate.md`; implement EP-1 through EP-4; execute the EP-5 retrofit for 31.2 and 27.3; apply the EP-6 ledger updates. |
| **Murat (Test Architect)** | Review the test suite for genuine coverage — specifically that the CRLF and liveness cases fail when the parser is broken. This gate exists because guards that pass without proving the thing were Epic 25's dominant review signature; it must not become another one. |
| **Administrator** | Owns the deferred 31.1 retrofit trigger (its review closing) and any bypass-trailer use. |

### Sequencing

1. Create the spec on a feature branch; revert the two submodule gitlinks first.
2. EP-1/EP-2 — verifier and tests, green locally.
3. EP-5 retrofit for **31.2 and 27.3** (the liveness test depends on it).
4. EP-3 — wiring. CI must be green on the spec's own PR, gate included.
5. EP-4 — policy, skill overrides, resolver fixture, `CONTRIBUTING.md`, lesson L12.
6. EP-6 — ledger updates.
7. **Deferred:** 31.1 retrofit once its review closes.

### Success criteria

- `tools/check-story-review-readiness.py` exits 1 on each of the four violation classes and 0 on all six live governed stories.
- `.githooks/commit-msg` and both `ci.yml` jobs invoke it; the tooling suite runs in `test-unit-contract`.
- The gate blocks its own PR when deliberately fed a violation (demonstrated, not asserted).
- `story-phase-ledger.md` carries the declared-exclusion format and the scope-limit paragraph.
- Lesson L12 recorded; the four action items carry the dated evidence comment.

### Definition of *closed* for the four action items

All of: verifier merged; wired into `.githooks/commit-msg` and both `ci.yml` jobs; tooling suite green in `test-unit-contract`; 31.2 and 27.3 retrofitted; **and one real story has passed through the gate on the way to `review`.** Absent that last condition, this becomes the fifth restatement rather than the delivery.

### Reopen trigger

The gate is removed from `.githooks/commit-msg` or `ci.yml`; or a governed story reaches `review` with an unlisted changed file, a placeholder ledger cell, or a sprint-status mismatch. Note the gate cannot self-detect its own removal — that remains a human/review check, the same limitation recorded for the `integration-fast` required-surfaces gate.

---

## 6. Amendment 2026-07-28 — Merged Story Close-Out Evidence Gate

**Approved by the Administrator, 2026-07-28.** A second `bmad-correct-course` session ran concurrently on the same four-row action-item chain, entering from the **Epic 22** row rather than the Epic 24 row, and produced `sprint-change-proposal-2026-07-28-story-closeout-evidence-gate.md`. Rather than ship two overlapping verifiers, two fixture lanes, two hook invocations and two competing claims on the same four ledger rows, that proposal is **merged into this one and marked superseded**. This document is the single authority. `tools/check-story-closeout.py` is never created; the merged gate keeps this proposal's name, `tools/check-story-review-readiness.py`.

**What the merge changes: one added check, C6.** Everything in EP-1 through EP-6 above stands unaltered.

> ### ⚠ Implementation correction, 2026-07-28 — C5 withdrawn
>
> This amendment originally added **two** checks. **C5 was specified, implemented, measured, and withdrawn before wiring.** It is not to be built. The correction is recorded here rather than by rewriting §6.2, so the reasoning survives.
>
> C5 asserted `set(### File List) == set(## File Scope)`. Measurement against the live tree refuted it twice over:
>
> - **Equality is wrong.** `## File Scope` is a forward-looking allow-list and `### File List` is a backward-looking record, so "allowed but unchanged" is the *normal* case — **17 of 21** artifacts carrying both sections. The claim was over-generalised from Story 27.3, which measurement shows is one of only two artifacts where the two sets coincide exactly.
> - **Subset is also wrong.** Story `15-4` legitimately lists `Hexalith.EventStore`, which sits under **"Forbidden by default:"** in its File Scope and was authorised by a `Scope-Override:` **commit trailer**. Those trailers live in the commit message, which a story-file check cannot see. Any story-file-only assertion in this direction produces false positives on every override.
>
> `tools/check-story-file-scope.py` already enforces this relation at commit time, with override support. Adding C5 would have put a false-positive generator inside a fail-closed gate — precisely the "guard passes without proving the thing" failure the Epic 25 retrospective named as its dominant review signature.
>
> **Consequently void in this amendment:** the C5 block in §6.2, the C5 fixture row in §6.4, and the C5 bullet in §6.7. The C5 half of §6.2's vacuous-pass argument is void; the C6 half stands unchanged and is what the anti-vacuity claim now rests on. §6.8's "assertion **A** becomes C5" no longer holds — assertion **A** is withdrawn outright, and only assertion **C** (→ C6) carries forward from the superseded proposal.

### 6.1 Why the merge was necessary, not merely tidy

The two sessions read *"evidence-table status"* differently, and the difference is load-bearing:

- **This proposal's C2** reads the phase-ledger **Change Log** table — `| Date | Phase | Change | Test count | File List reconciliation |` — checking row presence and non-placeholder cells. That maps to Epic 24's explicit wording, *"story/spec status, and sprint-status sync"*.
- **The merged proposal's C6** reads the **`### Evidence Table` / checkpoint tables** — those carrying a `Review status` or `Review state` column — checking that no row is left at `pending` once the story reaches `review` or `done`.

On the source wording, C6 is the better reading of the **Epic 22** row that started the chain. The Epic 22 retrospective raised it after the 22.4, 22.5 and 22.7 reviews each hand-fixed *"stale evidence-table statuses"*, and those stories carry `### Evidence Table` sections with `Review status` columns — not phase ledgers, which did not exist until 2026-07-16.

**The consequence is concrete: C1–C4 as specified do not catch the only two confirmed violations in the repository.** Both `22-2` and `26-5` predate the phase ledger, carry none, and are therefore no-ops under §2's governed set of 8. C1 skips them for being `done`. They pass this gate clean while asserting, in their own tables, that their declared proof was never produced.

### 6.2 C5 and C6 — added to the EP-1 contract

```
C5  FILE LIST / FILE SCOPE AGREEMENT     [runs when BOTH sections exist]
    scope-set  := allow-list paths under `## File Scope`
    listed-set := first backticked path per bullet under `### File List`
    exit 1 if scope-set != listed-set, reporting the symmetric difference
    Compared as SETS, never as raw text: 27-3's File List legitimately repeats
    two paths (once in the enumeration, once in its ownership-notes block).
    Set identity holds; naive line comparison would fail it falsely.

C6  EVIDENCE-ROW STATUS                  [runs when a Review status/state
                                          column exists, at ANY status]
    Governs every table whose header carries `Review status` or `Review state`.
    exit 1 if Status in {review, done} and any row's status cell is
            `pending`, `-`, or empty
    Rows in an explicit blocked state naming owner, consequence and reopen
    trigger are accepted — blocked is a recorded decision, pending is an
    unanswered question.
```

**C6 deliberately does not take C1's status window, and the asymmetry is justified rather than inconsistent.** C1 is windowed to `{in-progress, review}` because deriving a cumulative diff from `baseline..HEAD` returns unrelated work on a `done` story with a weeks-old baseline. C6 derives nothing: it reads the story file and `sprint-status.yaml` only, touching no git range. It therefore carries none of the hazard that forced C1's window, and excluding `done` would exclude precisely the failure class C6 exists to catch.

**C5 and C6 also close the vacuous-pass hole from the other direction.** Both read committed artifacts only and run regardless of the changed-file set, so a cited run always verified something even where EP-3's cumulative-set anchoring is unavailable.

### 6.3 Governed set for C6 — measured, not estimated

Measured against the tree on 2026-07-28: **14 artifacts** carry a `Review status`/`Review state` column. Only **4** of them (`27-2`, `27-3`, `31-1`, `31-2`) are among §2's ledger-bearing 8, so **C6 governs 10 artifacts this gate would otherwise never inspect.**

| Outcome | Count | Artifacts |
|---|---|---|
| Pass — `done`, no pending row | 10 | `21-9`, `21-10`, `22-1`, `22-3`, `22-4`, `22-5`, `22-6`, `22-7`, `27-2`, `31-1` |
| Pass — pre-`review` status, pending rows correct | 2 | `27-3` (in-progress, 14 pending), `31-2` (ready-for-dev, 5 pending) |
| **Exit 1 — violation** | **2** | `22-2` (done, 5 of 5 `Pending`, no completion dates), `26-5` (done, 10 of 10 `pending \| -`) |

Two violations, zero false positives, across 14 artifacts. The retrofit cost is two correction notes — materially smaller than EP-5's `27-3` retrofit.

`26-5` is the sharper case: its ten `pending | -` rows sit directly beneath the table's own preamble, *"Complete every row before moving the story to review,"* and the story passed a three-chunk adversarial review that closed 54 accepted findings without any of them noticing.

### 6.4 Additions to EP-2 — fixture cases

Added to `tests/tooling/story_review_readiness/story_review_readiness_test.py` (~38 cases → ~48):

| Group | Cases |
|---|---|
| C5 | scope ⊃ list → 1; list ⊃ scope → 1; identical → 0; **duplicate bullet with set identity preserved → 0** (the real `27-3` shape); one section absent → no-op 0 |
| C6 | `done` + `pending` row → 1 (the real `26-5` shape); `done` + `Pending` with empty date → 1 (the real `22-2` shape); `in-progress` + 14 pending → 0 (the real `27-3` shape); `ready-for-dev` + 5 pending → 0 (the real `31-2` shape); blocked-with-owner row → 0; `Review state` spelling honoured → 0; no such column → no-op 0 |
| Liveness | extended from 6 to **14** artifacts: all 14 `Review status` bearers exit 0 after the §6.5 retrofit |

The `22-2` and `26-5` cases must be built from the real shapes. A synthetic-only C6 suite passes forever while the live format drifts — the same failure mode this proposal's liveness test already guards against.

### 6.5 Additions to EP-5 — retrofit `22-2` and `26-5`

Both are `done` and neither is reopened; review history is not rewritten. Each receives a dated correction note, recorded not backdated:

> **2026-07-28 correction (story close-out gate).** These evidence rows were left at `Pending` when the story closed. Their status is not re-derived here; the actual completion evidence is the code-review record above. Recorded, not backdated.

The status of neither story is re-derived. The note records that the table was stale at close and points at where the real evidence lives. Scoping the annotation this way is what keeps the gate honest: it does not manufacture a completion date nobody observed.

### 6.6 Additions to EP-4 — policy

Appended to `_bmad/custom/story-phase-ledger.md`, after the `### Declared Exclusions` and `### Executable Gate` blocks:

```
+ ### Evidence-Table Status Reconciliation
+
+ A governed story that declares an evidence, checkpoint, or gate table — any
+ table carrying a `Review status` or `Review state` column — owns that
+ table's truthfulness at every status transition. Before `review`, each row
+ holds either a completed state with its completion date, or an explicit
+ blocked state naming owner, consequence, and reopen trigger. Before `done`,
+ no row remains `pending` or dateless.
+
+ A row left at `pending` under a story that reads `review` or `done` is a
+ false record, not a formatting lapse: it asserts that the story's own
+ declared proof was never produced. Repair the row, or restate the status.
```

The `STORY_REVIEW_READINESS_GATE` directive body in both tomls extends its green-exit disclaimer to name the two added checks, so the scope-limit paragraph stays accurate: *"…File List completeness, File Scope agreement, ledger row presence, evidence-row status, status validity, and sprint-status agreement."* No additional directive is added — the merged checks ride the existing one, keeping the count at one new directive per skill.

### 6.7 Amended success criteria

Added to §5:

- `tools/check-story-review-readiness.py` exits **1** on the real `22-2` and `26-5` shapes and **0** on all **14** `Review status` bearers after the §6.5 retrofit — not the 6 originally specified.
- C5 passes on `27-3`'s duplicate-bullet File List, proving set-based rather than text-based comparison.
- `story-phase-ledger.md` carries the Evidence-Table Status Reconciliation section alongside the declared-exclusion and scope-limit blocks.

### 6.8 Superseded artifact

`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-story-closeout-evidence-gate.md` is superseded by this amendment and retained for provenance only. Its §4.1 assertion **A** becomes C5 and its assertion **C** becomes C6; its assertion **B** is dropped as strictly weaker than C1, which checks the same relation in both directions. Its remaining sections — verifier scaffolding, hook and CI wiring, spec, lesson L12, ledger consolidation — duplicate EP-2 through EP-6 and are **not** carried forward. Nothing in it is implemented separately.

**The Epic 22 row (`sprint-status.yaml:631`) closes on C6 specifically.** That row is the one that names evidence-table status, and C1–C4 alone would have closed it without ever checking the thing it names.

---

## 7. Build Record 2026-07-28 — EP-1 and EP-2 delivered

EP-1 and EP-2 are implemented and green. EP-3 through EP-6 are **not started**: every remaining surface (`ci.yml`, `story-phase-ledger.md`, the three lifecycle tomls, `bmad_customization_test.py`, `story-creation-lessons.md`, `sprint-status.yaml`) is held by a concurrent session building the separate `check-story-slice-scope.py` gate, and the Administrator elected on 2026-07-28 to wait for a settled tree rather than race append-only edits.

| Artifact | State |
| :------- | :---- |
| `tools/check-story-review-readiness.py` | 886 lines, LF per `eol=lf`, stdlib only |
| `tests/tooling/story_review_readiness/story_review_readiness_test.py` | 541 lines, **37/37 passing** |

### 7.1 Live sweep — 22 governed artifacts, 19 pass, 3 fail

Every failure is a true positive. No false positive was produced on any of the other 19.

| Artifact | Status | Finding |
| :------- | :----- | :------ |
| `26-5-operational-runbook-set` | `done` | 10 evidence rows `pending` — predicted in §6.3 |
| `22-2-bounded-cancellable-graph-traversal` | `done` | 5 rows `Pending`, empty completion dates — predicted in §6.3 |
| `27-3-production-adapter-and-deployment-profile` | `in-progress` | **Newly discovered.** Its `correct-course` ledger row reconciles as *"48 -> 49 paths. One path joined…"* and never in the contracted `matched N/N` form |

**The 27-3 finding is the gate's first live catch of something no human or review process had flagged.** The row was authored the same day, by the concurrent session, under the `correct-course` phase that was admitted to the canonical set hours earlier. Per the Administrator decision of 2026-07-28, C2 stays strict: `story-phase-ledger.md` requires `matched N/N` in the phase row, the row reconciles substantively but not in the contracted form, and a gate that accepts free-form alternatives is the "passes without proving the thing" failure the Epic 25 retrospective identified as its dominant review signature. The owning session repairs the cell; the gate is not relaxed.

### 7.2 Defects found by measurement, not by reasoning

Three parser defects were found only because the verifier was run against live artifacts before being wired. Each would have shipped a gate that was green and wrong.

| Defect | Effect | Fix |
| :----- | :----- | :-- |
| `Matched **27/27**` | A pattern anchored directly after `matched` breaks on the bold markers. **2 false positives** (`29-1`, `31-2`) | Strip emphasis before matching |
| `2>&1 \| grep …` in a ledger cell | Escaped pipes were split as delimiters, shifting every column in **9 rows** of `27-3` and silently misreading the phase and reconciliation cells | Tokenise on unescaped pipes only |
| `status: 'done'` in spec frontmatter | YAML quotes leaked into the value, failing C3 on punctuation alone | Strip quotes in cell normalisation |

Two further contract gaps were closed: `correct-course` was absent from `CANONICAL_PHASES` (admitted to the policy mid-build by the concurrent session), and `spec-*` artifacts were unreachable by `--story-key` despite §EP-1 governing them for C2/C3.

### 7.3 Deliberate scope limits, stated rather than silent

- **C1 is skipped on the default branch.** `baseline..HEAD` returns unrelated work there, so the tool prints an explicit skip note instead of fabricating violations. C1 is enforced in CI against the PR diff. The current tree is on `main`, so the sweep above did not exercise C1 against real cumulative diffs.
- **An empty changed set fails closed** on a governed story, inverting the sibling gate's known vacuous pass.
- The liveness test pins the measured 2026-07-28 state: **14** evidence-table bearers, at least **8** ledger bearers, and exactly two C6-violating artifacts. A new violation breaks the fixture rather than passing quietly.
