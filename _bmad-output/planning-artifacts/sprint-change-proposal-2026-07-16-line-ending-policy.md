# Sprint Change Proposal: Repository Line-Ending Policy

**Date:** 2026-07-16  
**Mode:** Batch  
**Status:** Approved — routed for implementation  
**Scope classification:** Minor  
**Recommended owner:** Developer, with focused reviewer verification

## 1. Issue Summary

The repository declares CRLF in the root `.editorconfig`, but it has no root `.gitattributes`. Git therefore has no repository-owned normalization or checkout rule, so line endings depend on each contributor's Git configuration and editor behavior.

This gap is actively producing whole-file diff churn in `.razor` and `.razor.css` files:

- The 25 tracked `.razor` files currently contain 20 LF files, 1 CRLF file, and 4 mixed-ending files.
- The 5 tracked `.razor.css` files currently contain 4 LF files and 1 CRLF file.
- `git check-attr` returns no line-ending attributes for representative Razor files.
- The same unresolved action has been carried through Epics 17, 18, and 25.
- Story 17.6 and the Epic 18 retrospective record whole-file Razor rewrites caused by differing EOL decisions.
- Deferred-work evidence for Story 26.2 records approximately 2,500 LF-to-CRLF diff lines being folded into feature commits.

The trigger is not a product requirement change. It is a repository-policy gap exposed repeatedly during sprint implementation.

## 2. Impact Analysis

### Epic and story impact

- No epic, story, or acceptance criterion needs to be added, removed, or resequenced.
- Epics 17, 18, and 25 retain their delivered scope; their repeated line-ending follow-up is closed by this direct repository change.
- Future Razor implementation and review become deterministic across Windows and Unix development environments.
- The current sprint timeline is not expected to change.

### Artifact impact

| Artifact | Impact | Required action |
|---|---|---|
| PRD | None | No edit |
| Architecture | Clarification | Replace the unqualified `CRLF` statement with the two-layer Git/index and working-tree policy |
| UX specification | None | No edit |
| Epics/stories | None | No edit |
| Root `.gitattributes` | New | Add the durable repository-owned policy |
| Root `.editorconfig` | Alignment | Retain CRLF as the default and add LF overrides for Unix/tooling files |
| Root project context | Alignment | State the same policy for future agents and contributors |
| Sprint status | Tracking | Resolve the three duplicate open action items with implementation evidence |

### Technical impact

- Git will store normalized text with LF in the index.
- Working-tree text will use CRLF by default, consistent with the existing repository convention.
- Unix-executed and selected tooling files will remain LF.
- `.razor` and `.razor.css` will be explicitly pinned to CRLF in the working tree.
- Existing tracked text must be renormalized once in an isolated mechanical commit.
- No runtime behavior, public API, persistence contract, accessibility behavior, or UX flow changes.

### Delivery and risk impact

- **Effort:** Low, expected to fit within half a developer day including verification.
- **Product risk:** Low; no product semantics change.
- **Review risk:** Medium for the one-time normalization because the diff can be large.
- **Schedule impact:** None expected.
- **Primary safeguard:** Perform normalization from a clean dedicated worktree or branch and keep it out of feature commits.

## 3. Recommended Path Forward

Use a root `.gitattributes` and close the existing action items. Do not accept the issue as debt: it has already recurred across multiple epics, continues to affect active files, and has a small durable fix.

The selected path is **Direct Adjustment**:

1. Add the root policy and align `.editorconfig`.
2. Renormalize tracked text once in an isolated mechanical commit.
3. Verify that normalization changed no source content other than line endings.
4. Clarify the architecture and project-context wording.
5. Close the duplicate sprint action items with evidence.

Rollback and MVP-scope review are not viable or necessary because the issue is independent of delivered product scope.

## 4. Detailed Change Proposals

### Change A — Add root `.gitattributes`

**Before:** No root `.gitattributes`; line-ending behavior depends on contributor-local Git configuration.

**After:** Add the following repository-owned policy:

```gitattributes
# Normalize detected text to LF in Git and materialize CRLF by default.
* text=auto eol=crlf

# Repository metadata and Unix-executed/tooling files stay LF.
.gitattributes text eol=lf
*.sh text eol=lf
*.bash text eol=lf
*.py text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
Dockerfile text eol=lf
*.dockerfile text eol=lf

# Pin the recurring churn source explicitly.
*.razor text eol=crlf
*.razor.css text eol=crlf

# Common binary payloads must never be line-ending normalized.
*.gif binary
*.ico binary
*.jpeg binary
*.jpg binary
*.pdf binary
*.png binary
*.webp binary
*.zip binary
```

**Rationale:** The default preserves the repository's documented CRLF working-tree convention while Git keeps one canonical LF representation. Explicit LF exceptions protect Unix execution and common cross-platform tooling. Explicit Razor rules make the original failure mode directly testable and resistant to local `core.autocrlf` or `core.eol` settings.

### Change B — Align root `.editorconfig`

**Before:** `end_of_line = crlf` applies to every file type.

**After:** Keep the existing CRLF default and append:

```editorconfig
[*.{sh,bash,py,yaml,yml}]
end_of_line = lf

[Dockerfile]
end_of_line = lf

[*.dockerfile]
end_of_line = lf

[.gitattributes]
end_of_line = lf
```

**Rationale:** Editors and Git must agree about the explicit LF exceptions; otherwise saves can continually rewrite the working tree even if Git hides the churn at commit time.

### Change C — Perform isolated one-time renormalization

**Before:** Tracked Razor files include LF, CRLF, and mixed working-tree endings, and earlier work folded normalization into feature diffs.

**After:** From a clean dedicated worktree or branch, apply the attributes and run a full tracked-text renormalization. Commit the mechanical normalization separately from product work.

Suggested sequence:

```bash
git add .gitattributes .editorconfig
git add --renormalize .
git status --short
```

Do not run the renormalization in a worktree containing unrelated modifications. The current worktree already contains unrelated planning-artifact and sprint-status changes, so implementation must preserve those changes or use a clean dedicated worktree.

**Rationale:** A repository policy without a controlled one-time renormalization leaves historical blobs inconsistent and postpones the same review churn.

### Change D — Clarify architecture and agent guidance

**Architecture before:** `Line endings | CRLF | Enforced by .editorconfig`

**Architecture after:** `Line endings | Git index LF; CRLF working tree by default; LF for Unix/tooling exceptions | Enforced by root .gitattributes and aligned .editorconfig`

**Project-context before:** Guidance describes `.editorconfig` conventions as CRLF without exceptions.

**Project-context after:** Guidance states that `.gitattributes` is authoritative for Git normalization; `.editorconfig` mirrors CRLF-by-default working-tree behavior and the enumerated LF exceptions.

**Rationale:** Contributors and future agents need one unambiguous rule. The clarification does not alter product architecture.

### Change E — Resolve duplicated sprint follow-ups

**Before:** The sprint status contains three open variants of the same root `.gitattributes` or accepted-debt action, carried from Epics 17, 18, and 25.

**After:** Mark each duplicate complete and reference the policy/normalization change and verification evidence. Preserve all unrelated edits already present in `sprint-status.yaml`.

**Rationale:** Leaving duplicate open items after implementation would make sprint reporting inaccurate and encourage repeated rediscovery.

## 5. Verification and Acceptance Criteria

The implementation is complete only when all of the following are true:

1. A root `.gitattributes` is tracked and `git check-attr text eol` reports the intended attributes for representative `.razor`, `.razor.css`, `.sh`, `.py`, and `.yaml` files.
2. `git ls-files --eol` reports `i/lf` for every tracked `.razor` and `.razor.css`, with `attr/text eol=crlf` and no mixed index or working-tree state after a fresh checkout.
3. A fresh checkout materializes `.razor` and `.razor.css` as CRLF, while the explicit Unix/tooling exceptions materialize as LF, regardless of local `core.autocrlf` and `core.eol` settings.
4. A pre/post manifest that removes carriage returns before hashing shows no substantive content change in renormalized product files.
5. `git -c core.whitespace=cr-at-eol diff --cached --check` passes during the normalization commit.
6. A controlled one-line Razor edit produces a localized diff rather than a whole-file diff.
7. Unix shell scripts remain LF and pass their existing syntax or test checks.
8. `dotnet build Hexalith.Memories.slnx --configuration Release` succeeds.
9. Relevant Web/Razor tests succeed using the repository's documented .NET test runner path.
10. The three duplicated sprint action items are resolved with evidence, without overwriting unrelated worktree changes.

## 6. Alternatives Considered

### Accepted-debt entry only

Rejected. The repository has repeated evidence across several epics, the active Razor set is still inconsistent, and the durable fix is small. Recording debt would document the churn without preventing it.

### Pin only `.razor` and `.razor.css`

Rejected as incomplete. It would stop the immediate symptom but leave repository-wide behavior dependent on contributor-local Git configuration and allow the same problem to recur in other text formats.

### Force LF for every text working-tree file

Rejected for this change. It would contradict the current documented CRLF default and expand the migration beyond the demonstrated need. Canonical LF in Git plus deterministic working-tree exceptions provides stable diffs without an unnecessary convention reversal.

## 7. Implementation Handoff

**Classification:** Minor — direct developer implementation with focused review.

**Developer responsibilities:**

- Use a clean dedicated worktree or otherwise isolate unrelated local changes.
- Add `.gitattributes`, align `.editorconfig`, and perform the one-time renormalization.
- Keep the mechanical normalization separate from product changes.
- Update architecture, root project context, and sprint tracking as specified.
- Capture the attribute, EOL, content-equivalence, build, and test evidence.

**Reviewer responsibilities:**

- Confirm that the large diff is line-ending-only except for the named policy/documentation/tracking files.
- Confirm platform-sensitive files remain LF.
- Confirm Razor diffs are localized after a fresh checkout under differing local Git configurations.

**Success definition:** Git owns one durable line-ending policy, all Razor files have deterministic attributes and normalized index content, a fresh checkout is stable across platforms, and future single-line Razor edits no longer appear as whole-file rewrites.

## 8. Correct-Course Checklist Status

| Checklist item | Status | Finding |
|---|---|---|
| 1.1 Triggering story identified | Complete | Repeated implementation/review churn, first explicitly evidenced in Story 17.6 |
| 1.2 Core problem defined | Complete | Missing repository-owned line-ending policy |
| 1.3 Initial evidence assessed | Complete | Current file scan plus Epics 17, 18, 25 and Story 26.2 evidence |
| 2.1 Current epic impact | Complete | No scope change; direct hygiene correction |
| 2.2 Epic-level changes | Complete | None required |
| 2.3 Remaining epics reviewed | Complete | Future work benefits; no sequencing impact |
| 2.4 Epics invalidated | N/A | None |
| 2.5 New epic required | N/A | No |
| 3.1 PRD conflict/impact | Complete | None |
| 3.2 Architecture impact | Complete | Wording clarification required |
| 3.3 UX impact | N/A | None |
| 3.4 Other artifact impact | Complete | `.gitattributes`, `.editorconfig`, project context, sprint tracking |
| 4.1 Direct adjustment viability | Complete | Viable; low effort, low product risk, medium review risk |
| 4.2 Rollback viability | N/A | No relevant rollback target |
| 4.3 MVP review viability | N/A | Product scope is unaffected |
| 4.4 Recommended path selected | Complete | Direct Adjustment |
| 5.1 Issue summary | Complete | Section 1 |
| 5.2 Epic/product impact | Complete | Section 2 |
| 5.3 Recommended path and rationale | Complete | Section 3 |
| 5.4 Detailed change proposals | Complete | Section 4 |
| 5.5 Implementation handoff | Complete | Section 7 |
| 6.1 Checklist reviewed | Complete | No required analysis item omitted |
| 6.2 Proposal consistency checked | Complete | Policy, safeguards, and acceptance criteria align |
| 6.3 Explicit approval | Complete | Approved by Administrator on 2026-07-16 |
| 6.4 Approved epic/story edits executed | N/A | No epic/story content edit proposed |
| 6.5 Handoff confirmed | Complete | Minor change routed to the Developer agent for direct implementation |

## 9. Approval and Handoff Record

- **Decision:** Approved
- **Approved by:** Administrator
- **Approval date:** 2026-07-16
- **Final scope:** Minor
- **Routed to:** Developer agent for direct implementation
- **Implementation input:** The finalized changes, safeguards, and acceptance criteria in Sections 4, 5, and 7
- **Next action:** Implement from a clean dedicated worktree or branch, preserve unrelated local changes, and return the verification evidence listed in Section 5
