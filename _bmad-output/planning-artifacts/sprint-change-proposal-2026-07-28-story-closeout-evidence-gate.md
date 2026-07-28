# Sprint Change Proposal 2026-07-28 — Executable Story Close-Out Evidence Gate

> ## ⚠ SUPERSEDED 2026-07-28 — do not implement from this document
>
> A concurrent `bmad-correct-course` session targeted the same four action-item rows and produced **`sprint-change-proposal-2026-07-28-executable-pre-review-story-gate.md`**. By Administrator decision of 2026-07-28 the two are **merged into that document**, which is the single authority. See its **§6 Amendment**.
>
> **What was carried forward:** assertion **A** (File List ≡ File Scope) became **C5**; assertion **C** (`Review status` rows not `pending` under `review`/`done`) became **C6**; the `22-2` / `26-5` evidence, the 14-artifact governed set, the fixture cases, the policy section, and the two correction notes moved with them.
>
> **What was dropped:** assertion **B** (`changed ⊆ File List`), as strictly weaker than that proposal's **C1**, which checks the same relation in both directions. `tools/check-story-closeout.py` is **never created** — the merged gate is `tools/check-story-review-readiness.py`.
>
> Sections 1–7 below are retained for provenance: the four-generation ledger analysis, the `22-2` / `26-5` violation evidence, and the reasoning that produced C5 and C6. Every implementation instruction here is void.

**Date:** 2026-07-28
**Author:** `correct-course` (Developer)
**Approver:** Administrator
**Trigger:** Epic 22 retrospective action item #1 (`epic-22-retro-2026-07-05.md:132`), carried forward unclosed through the Epic 23, 24, and 25 retrospectives
**Scope classification:** Moderate — process governance and CI tooling. No epic, story, PRD, architecture, or UX artifact changes. No story is added, removed, renumbered, split, or advanced.

---

## 1. Issue Summary

A retrospective action item requiring an explicit File List and evidence-table check before remediation-story review has been raised four consecutive times and has never closed. Each retrospective re-scoped it harder after observing that the previous formulation had not prevented the failure:

| Epic | Ledger line | Ask | Status |
| :--- | :---------- | :-- | :----- |
| 22 | `sprint-status.yaml:631` | "explicit File List and evidence-table pre-review **check**" | `in-progress` |
| 23 | `sprint-status.yaml:651` | "pre-review **source guard or checklist**" | `in-progress` |
| 24 | `sprint-status.yaml:671` | "**executable** pre-review guard or checklist" | `in-progress`, "merged into Epic 25 action item 7" |
| 25 | `sprint-status.yaml:719` | "**executable pre-review gate**" | `open` |

**Root cause.** Both halves of the control are enforced only as LLM-obeyed prose in `_bmad/custom/story-phase-ledger.md`. Prose enforcement has now failed four times in a row. Specifically:

- **The File List half has no executable check at all.** `tools/check-story-file-scope.py` validates changed paths against `## File Scope` — the forward-looking allow-list. Nothing compares them against `### File List`, the backward-looking record. A path can be in scope, be changed, and be absent from the File List with every existing gate green. Story 27.3 had to perform that set comparison by hand and record the result in prose: *"The `## File Scope` section and the File List were compared as sets programmatically and are identical (48 unique paths each...)"*.
- **The evidence-table half has no policy and no check.** `story-phase-ledger.md` governs File List reconciliation and test-count arithmetic; `remediation-runtime-checklist.md` governs four runtime-defect classes and explicitly defers File List reconciliation to the phase ledger. Neither binds evidence-table row status to the story's status transition. That obligation exists today only inside individual story files, as prose in a table preamble.

### Evidence

A scan of every story file in `_bmad-output/implementation-artifacts/` against its `sprint-status.yaml` row, 2026-07-28:

| Story | Sprint status | Rows still `Pending` | Assessment |
| :---- | :------------ | :------------------- | :--------- |
| `22-2-bounded-cancellable-graph-traversal.md` | `done` | 5 of 5 Evidence Table rows (`:80`–`:84`), all with empty completion dates | **Violation.** From Epic 22 — the epic whose retrospective raised this action item. The story that helped trigger the item was never repaired by it. |
| `26-5-operational-runbook-set.md` | `done` | 10 of 10 checkpoint rows, all `pending \| -` | **Violation.** The rows sit directly beneath the table's own preamble, *"Complete every row before moving the story to review."* The story passed a three-chunk adversarial review that closed 54 accepted findings; none caught this. |
| `27-3-production-adapter-and-deployment-profile.md` | `in-progress` | 14 | Correct. Not a violation. |
| `31-2-runtime-dapr-secret-store-migration.md` | `ready-for-dev` | 5 | Correct. Not a violation. |

A status-conditioned check therefore finds exactly **two violations and zero false positives** across roughly 150 story files. The signal is both real and quiet.

---

## 2. Impact Analysis

### Epic impact — none

No epic changes. Epics 0–31 are product scope; none owns BMAD process tooling. The precedent for this exact class of work is `spec-remediation-runtime-defect-checklist.md`, which closed the sibling Epic 21 action item under a plain spec with no epic entry and no `development_status` row. `project-context.md:110` sanctions that route for multi-stage non-epic work.

### Story impact — none advanced, two annotated

No story is added, removed, split, or advanced. Stories `22-2` and `26-5` receive a dated correction note only; their `done` status is not reopened and their review history is not rewritten.

### Artifact conflicts — none

PRD, `epics.md`, `architecture.md`, and UX specifications carry no conflicting content. MVP scope is unaffected.

### Technical impact

| Surface | Change |
| :------ | :----- |
| `tools/` | One new stdlib-only verifier |
| `.githooks/commit-msg` | One added invocation, reusing the existing changed-file capture |
| `.github/workflows/ci.yml` | Two added steps; no new job |
| `tests/tooling/` | One new fixture lane; one existing lane extended |
| `_bmad/custom/` | One policy section; three activation directives; one review-layer prompt extended |
| `_bmad-output/` | One spec; four ledger rows; one lesson; two story correction notes |

No product code, no C# source, no runtime behavior, and no deployment artifact is touched.

---

## 3. Recommended Approach

**Direct Adjustment** — build the executable gate that Epics 24 and 25 explicitly asked for, using the two enforcement patterns this repository has already proven.

**Options evaluated:**

- **Direct Adjustment — viable, selected.** Effort Medium, Risk Low, timeline impact negligible. Every mechanism already exists and has shipped twice: the changed-file governance gate pattern (`check-story-file-scope.py`, `check-tenant-isolation-evidence.py`) and the BMAD process-policy pattern (`story-scope-guard.md`, `story-phase-ledger.md`, `remediation-runtime-checklist.md`).
- **Rollback — not viable.** There is nothing to roll back. The failure is an absent control, not an incorrect one.
- **MVP Review — not viable.** No product scope is involved; the PRD MVP is untouched.

**Rationale.** The distinguishing feature of this proposal against its four failed predecessors is that the check becomes executable and non-vacuously citable. Two of its three assertions read only committed artifacts, so unlike `check-story-file-scope.py` — which exits `0` printing *"No changed files; story-scope check is a no-op"* against an unstaged tree — this gate cannot be cited as a pass when it verified nothing. That property is what converts the control from a convention into evidence.

**Risk and mitigation.** The main risk is a noisy gate blocking unrelated work. It is bounded by design: the verifier inspects only the story resolved from the commit trailer or branch, never the repository at large, so closed history is never re-litigated. The measured false-positive rate against the current tree is zero. A `Story-Closeout: not-applicable - <reason>` bypass trailer follows the `check-tenant-isolation-evidence.py:44` convention for genuine exceptions.

---

## 4. Detailed Change Proposals

### 4.1 New verifier — `tools/check-story-closeout.py`

Mirrors `tools/check-story-file-scope.py`: self-contained, stdlib-only, `def main(argv) -> int`, exit `0` (pass or no-op) or `1` (violation), all output to stdout, a local `ValidationError` caught only in `main`. Reuses the sibling's `normalize_path`, `matches_glob`/`_glob_match`, `extract_story_keys`, `resolve_story_source`, and `parse_trailers`. Story resolution precedence: `--story-key` > `Story:`/`Story-Key:` trailer > branch name. Bypass trailer `Story-Closeout: not-applicable - <reason>`.

| # | Assertion | Applies when |
| :- | :-------- | :----------- |
| **A** | `set(### File List) == set(## File Scope)`; reports the symmetric difference | a `### File List` section exists |
| **B** | `set(changed paths) ⊆ set(### File List)` | changed files are supplied; `references/*` gitlinks are excluded, since release builds dirty them as a side effect |
| **C** | no row of any table carrying a `Review status` or `Review state` column holds `pending`, `-`, or empty | the story's `sprint-status.yaml` row reads `review` or `done` |

**Non-vacuity.** Assertion **A** reads only the story file; assertion **C** reads only the story file and `sprint-status.yaml`. Both execute regardless of the changed-file set, so a cited run always verified something.

**Deliberately excluded:** Change Log phase-row presence and test-count arithmetic. Those belong to `story-phase-ledger.md`. The action item names two things — File List and evidence table — and this gate implements exactly those two.

### 4.2 New fixture — `tests/tooling/story_closeout/story_closeout_test.py`

Stdlib `unittest`, `REPO_ROOT = Path(__file__).resolve().parents[3]`, subprocess-driven CLI tests. Required cases:

- **Regression, built from the real `26-5` shape:** status `done` with all checkpoint rows `pending | -` → exit `1`.
- **False-positive guard, built from the real `27-3` shape:** status `in-progress` with 14 `pending` rows → exit `0`.
- File List / File Scope symmetric-difference detection in both directions.
- A changed path absent from the File List → exit `1`.
- Bypass trailer honored; unresolvable story → documented no-op.

### 4.3 Hook wiring — `.githooks/commit-msg`

Appended after the existing tenant-isolation invocation at `:25`, reusing `$changed_file`, `$branch_name`, `$message_file`, and `$py`:

```bash
# Story close-out gate. Runs here (not pre-commit) because the Story and bypass
# trailers live in the commit message, which pre-commit cannot see.
"$py" tools/check-story-closeout.py --branch-name "$branch_name" --commit-message-file "$message_file" --changed-files-file "$changed_file"
```

### 4.4 CI wiring — `.github/workflows/ci.yml`

Two steps, no new job:

- After `:153` — the end of the existing `Validate cross-tenant negative evidence` step — inside the existing `story-file-scope` job, reusing its `$BRANCH_NAME` / `$COMMIT_MESSAGE_FILE` / `$CHANGED_FILES_FILE` environment (the ~120-line diff-collection block is **not** duplicated):

  ```yaml
  - name: Validate story close-out
    run: python3 tools/check-story-closeout.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"
  ```

- After `:244`, inside `test-unit-contract` (tooling fixtures are not auto-discovered):

  ```yaml
  - name: Run story close-out gate fixtures
    run: python3 -m unittest discover -s tests/tooling/story_closeout -p "*_test.py"
  ```

### 4.5 Author-facing documentation — `CONTRIBUTING.md`

A section covering what the gate asserts, the bypass trailer syntax, and an explicit warning that a run reporting no resolvable story is a no-op rather than a pass.

### 4.6 Policy — `_bmad/custom/story-phase-ledger.md`

**NEW** section, inserted after *Cumulative File List Reconciliation*:

> ## Evidence-Table Status Reconciliation
>
> A governed story that declares an evidence, checkpoint, or gate table — any table carrying a `Review status` or `Review state` column — owns that table's truthfulness at every status transition. Before `review`, each row must hold either a completed state with its completion date, or an explicit blocked state naming owner, consequence, and reopen trigger. Before `done`, no row may remain `pending` or dateless.
>
> A row left at `pending` under a story that reads `review` or `done` is a false record, not a formatting lapse: it asserts that the story's own declared proof was never produced. Repair the row, or restate the story status.

**MODIFIED** — the `review` bullet under *Fail-Closed Status Gates*:

*OLD:*
> - Do not set `review` unless the required create and development rows, discovery evidence or precise blocker record, same-unit arithmetic, and cumulative File List all reconcile.

*NEW:*
> - Do not set `review` unless the required create and development rows, discovery evidence or precise blocker record, same-unit arithmetic, cumulative File List, and evidence-table row states all reconcile, and `python3 tools/check-story-closeout.py --story-key <key> --changed-files-file <file>` exits `0`. Record the exact invocation and its final line. A run that reports no resolvable story or no changed files is a no-op, not evidence.

*Rationale:* binds the executable gate to the status transition the action item names, and forecloses the vacuous-citation failure mode observed with the sibling scope gate.

### 4.7 Lifecycle skill overrides — one directive each

Appended to `activation_steps_append` in the committed overrides. Arrays append over each skill's base `customize.toml`; generated `.agents/skills/**` and `.claude/skills/**` are never edited, since refreshes overwrite them.

| File | `STORY_CLOSEOUT_GATE` directive |
| :--- | :------------------------------ |
| `_bmad/custom/bmad-create-story.toml` | Every evidence, checkpoint, or gate row is created with an explicit initial state, accountable owner, and validation command or artifact, so the row is machine-checkable later. File List identity binds from `dev-story` onward, not at creation. |
| `_bmad/custom/bmad-dev-story.toml` | Run the gate before setting `review`; record the exact invocation and final line in the `dev-story` Change Log row; fail closed while it exits `1`; never cite a no-resolvable-story or no-changed-files run as a pass. |
| `_bmad/custom/bmad-code-review.toml` | Re-run the gate independently rather than accepting the `dev-story` citation; route an unambiguous File List or evidence-row repair to `patch` and ambiguous ownership to `decision_needed`; treat an unreconciled row as a fail-closed blocker for `done`. |

**MODIFIED** — the existing `story-phase-ledger` review layer prompt in `bmad-code-review.toml` gains evidence-row auditing, rather than a fifth standalone review layer being added. Same audit surface, no extra subagent per full review.

### 4.8 Resolver fixture — `tests/tooling/bmad_customization/bmad_customization_test.py`

Extended to assert, per skill and by directive **body** rather than count, that each of the three lifecycle skills resolves exactly one `STORY_CLOSEOUT_GATE` directive, across both the `.agents` and `.claude` surfaces (the shared `_bmad/custom/*.toml` feeds both). Existing `HISTORICAL_SLICE_GUARD`, `STORY_PHASE_LEDGER`, `REMEDIATION_RUNTIME_CHECKLIST`, and `EPIC_AC_VERIFICATION` directives, the four `_bmad/custom/*.md` persistent facts, and all review layers must survive exactly once.

> **Note (2026-07-28).** A fourth policy, `_bmad/custom/epic-ac-verification.md`, landed in all three lifecycle overrides while this proposal was being drafted — closing the Epic 25 action item at `sprint-status.yaml:699`. `STORY_CLOSEOUT_GATE` becomes the fifth directive and must append to, not clobber, the four now in place. That policy's arrival independently confirms the enforcement pattern this proposal follows.

### 4.9 Process lesson — `_bmad-output/process-notes/story-creation-lessons.md`

New lesson **L12**, following the L11 shape: a process control that four retrospectives could not close as prose became executable; record why the prose form kept failing and what the executable form asserts. CRLF file — normalize after edit.

### 4.10 Spec — `_bmad-output/implementation-artifacts/spec-story-closeout-evidence-gate.md`

New spec using the frontmatter and `<frozen-after-approval>` structure of `spec-remediation-runtime-defect-checklist.md`: Intent, Boundaries & Constraints, I/O & Edge-Case Matrix, Code Map, Tasks & Acceptance. No epic entry and no `development_status` row.

### 4.11 Ledger consolidation — `sprint-status.yaml`

| Line | Epic | Change |
| :--- | :--- | :----- |
| `:719` | 25 | `open` → `done`, with a dated evidence comment naming the verifier, hook, both CI steps, both fixture lanes, the policy section, and the three directives |
| `:633` | 22 | `in-progress` → `done  # 2026-07-28: superseded — consolidated into the Epic 25 executable pre-review gate item and closed by it. See spec-story-closeout-evidence-gate.md.` |
| `:653` | 23 | same supersession comment |
| `:673` | 24 | same supersession comment; its row already records "Merged into Epic 25 action item 7", so this completes a consolidation that was started but never finished |

### 4.12 Closed-story correction notes — `22-2` and `26-5`

Appended to each story's completion record. Recorded, not backdated; neither story is reopened and no review history is rewritten:

> **2026-07-28 correction (story close-out gate).** These evidence rows were left at `Pending` when the story closed. Their status is not re-derived here; the actual completion evidence is the code-review record above. Recorded, not backdated.

---

## 5. Implementation Handoff

**Scope classification:** **Moderate** — governance tooling plus backlog-ledger reorganization, no product scope change.

**Route to:** Developer agent (Amelia) for implementation, with Test Architect (Murat) as the named co-owner on both action items, matching the `owner: "Amelia, Murat"` field carried on all four ledger rows.

**Responsibilities:**

| Role | Deliverable |
| :--- | :---------- |
| Developer (Amelia) | The verifier, both fixture lanes, hook and CI wiring, policy and toml edits, lesson L12, spec, ledger consolidation, correction notes |
| Test Architect (Murat) | Adversarial review of the assertion set and fixture coverage; confirmation that the `26-5` regression case and `27-3` false-positive case genuinely reproduce the observed shapes |
| Administrator | Approval of this proposal; approval of the ledger consolidation |

**Sequencing.** 4.1 and 4.2 first (the verifier must be green before anything depends on it), then 4.3–4.5, then 4.6–4.9, then 4.10–4.12. The ledger rows close last, on landed and green evidence.

**Success criteria:**

1. `python3 tools/check-story-closeout.py` exits `1` on the `26-5` shape and `0` on the `27-3` shape, proven by the committed fixture.
2. `python3 -m unittest discover -s tests/tooling/story_closeout -p "*_test.py"` passes, and its CI step is present in `test-unit-contract`.
3. `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` passes with the extended assertions.
4. All three lifecycle skills resolve exactly one `STORY_CLOSEOUT_GATE` directive on both the `.agents` and `.claude` surfaces, with every pre-existing directive and review layer intact exactly once.
5. The gate runs against the current tree without failing any commit that does not actually violate it.
6. `.py`, `.yml`, and `.githooks/*` files are LF; `.md` files are CRLF, per `.gitattributes`.

**Reopen trigger.** A future story review finds an omitted changed file, a File List / File Scope divergence, or an evidence row left at `pending` under a `review` or `done` status — and the gate did not catch it. In that event the gate's assertion set, not the convention, is what gets strengthened.

---

## 6. Approval

| Field | Value |
| :---- | :---- |
| Proposal status | Approved 2026-07-28 by Administrator |
| Mode | Incremental; P1–P4 approved individually |
| Backfill decision | Gate scoped to the resolved story only; `22-2` and `26-5` annotated, not backdated |
| File List assertion | `File List ≡ File Scope` **and** `changed ⊆ File List` |
| Review-layer decision | Extend the existing `story-phase-ledger` layer; no fifth layer added |
| Ledger decision | Consolidate all four generations into the Epic 25 row |
| Full-proposal approval | Granted 2026-07-28 by Administrator, unconditional, on the complete document |

---

## 7. Handoff Record

**Routed 2026-07-28.** Scope **Moderate**; deliverables are this proposal plus the backlog-ledger reorganization in §4.11.

| Recipient | Responsibility | Entry point |
| :-------- | :------------- | :---------- |
| Developer (Amelia) | Author `spec-story-closeout-evidence-gate.md` from §4, then execute §4.1–4.12 in the §5 sequence | `bmad-spec`, then `bmad-dev-story` |
| Test Architect (Murat) | Adversarial review of the assertion set and fixture coverage; confirm the `26-5` regression case and `27-3` false-positive case reproduce the observed shapes | `bmad-code-review` |
| Administrator | Approver of record; owner of any decision that widens the assertion set beyond §4.1 |

**No artifact was mutated by this `correct-course` session other than the creation of this proposal.** In particular, the four `sprint-status.yaml` action-item rows in §4.11 are deliberately **not** closed here: §5 sequences them last, on landed and green evidence. Closing them at proposal time would repeat the failure this proposal exists to correct — recording a control as complete before anything executable proves it.

**State at handoff.** No epic, story, PRD, architecture, or UX artifact changed. `sprint-status.yaml` is untouched by this session. Epic 22 `:633`, Epic 23 `:653`, Epic 24 `:673`, and Epic 25 `:719` remain open and are the gate's own success condition.

**Pre-existing worktree state, not owned by this session:** `_bmad/custom/bmad-{create-story,dev-story,code-review}.toml` and `_bmad/custom/epic-ac-verification.md` carry the concurrent `EPIC_AC_VERIFICATION` work; `references/Hexalith.Builds` carries a gitlink drift; `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md` is untracked Story 31.2 work. None is credited to or modified by this proposal.
