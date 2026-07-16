---
title: 'Establish Repository Line-Ending Policy'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
baseline_commit: 'c28a1d8ce0459abb713df9f029a028efa578702d'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-line-ending-policy.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository declares CRLF only through `.editorconfig`, leaving Git normalization dependent on contributor settings and repeatedly turning small `.razor` and `.razor.css` edits into whole-file diffs.

**Approach:** Add the approved root `.gitattributes`, align editor rules, perform one isolated repository-wide text renormalization, and close the three duplicate follow-up actions with verification evidence.

## Boundaries & Constraints

**Always:** Preserve canonical LF text in Git; materialize CRLF by default; keep `.gitattributes`, shell, Bash, Python, YAML, and Dockerfile variants LF; explicitly pin `.razor` and `.razor.css` to CRLF; preserve binary payloads; isolate mechanical normalization from unrelated work; retain the existing `references/Hexalith.EventStore` state.

**Ask First:** Halt if renormalization changes content after carriage-return-insensitive comparison, classifies a tracked payload unexpectedly, requires touching an unrelated submodule/pointer, or cannot be isolated safely.

**Never:** Substitute an accepted-debt entry for the approved durable fix; alter product semantics; hide normalization inside a feature diff; overwrite pre-existing worktree edits; mark the Epic 26 composite governance action done while its submodule-control half remains unresolved.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Razor checkout | `.razor` or `.razor.css` under differing local Git EOL settings | Index is LF and fresh working tree is CRLF | Fail verification on unspecified attributes, mixed EOL, or whole-file single-line diff |
| Unix/tooling checkout | `.sh`, `.bash`, `.py`, `.yml`, `.yaml`, `Dockerfile`, or `*.dockerfile` | Index and fresh working tree are LF | Stop if checkout or syntax validation reports CRLF-sensitive damage |
| Binary payload | Declared image, PDF, or archive extension | Git performs no text normalization | Stop on any byte change |
| Existing unrelated state | Dirty submodule or concurrent user edit | State is preserved and excluded from staging/diff evidence | Halt before overwrite or accidental staging |

</frozen-after-approval>

## Code Map

- `.gitattributes` -- authoritative Git text normalization, working-tree EOL, and binary rules.
- `.editorconfig` -- editor-side CRLF default and LF tooling exceptions.
- `_bmad-output/planning-artifacts/architecture.md` -- contributor-facing two-layer line-ending convention.
- `_bmad-output/project-context.md` -- durable implementation guidance for agents.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- Epic 17, 18, and 25 duplicate action closure; Epic 26 composite stays open.
- `src/**/*.razor`, `src/**/*.razor.css`, `tests/**/*.razor`, `tests/**/*.razor.css` -- primary churn surface and focused EOL proof.

## Tasks & Acceptance

**Execution:**
- [x] `.gitattributes` -- add `* text=auto eol=crlf`, explicit LF tooling rules, explicit Razor CRLF rules, and binary rules for GIF/ICO/JPEG/JPG/PDF/PNG/WEBP/ZIP.
- [x] `.editorconfig` -- retain the CRLF default and add matching LF sections for shell/Bash/Python/YAML, Dockerfiles, and `.gitattributes`.
- [x] `.` -- on `main`, capture a pre-change CR-stripped manifest and apply one full tracked-text renormalization with pathspec exclusions for `references/` and concurrent unrelated edits; prove all non-policy changes are EOL-only without staging another session's work.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/project-context.md` -- document LF-in-index, CRLF-by-default working trees, LF exceptions, and `.gitattributes` authority.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- mark only the Epic 17/18/25 duplicates done with dated policy/normalization evidence; preserve unrelated edits and leave the Epic 26 composite open.
- [x] `.` -- test attribute resolution, fresh-checkout behavior across local Git settings, localized Razor diffs, shell syntax, Release build, and Web tests.

**Acceptance Criteria:**
- Given any tracked Razor or Razor CSS file, when attributes and a fresh checkout are inspected, then Git reports `i/lf`, `w/crlf`, and `attr/text eol=crlf` with no mixed state.
- Given each explicit LF tooling class, when checked out with conflicting local Git EOL settings, then both index and working tree remain LF.
- Given the renormalized change set, when CR characters are removed before hashing, then every existing file matches its pre-change substantive content and declared binaries match byte-for-byte.
- Given a controlled one-line Razor edit, when its diff is inspected, then only the edited line and normal context appear rather than a whole-file rewrite.
- Given the isolated change, when whitespace, shell, Release build, and focused Web tests run, then all gates pass and no submodule change is staged.
- Given the duplicate Epic 17, 18, and 25 line-ending actions plus the broader Epic 26 governance action, when sprint tracking is updated, then only the three duplicates become `done` with dated verification evidence and Epic 26 remains open for its unresolved submodule-control half.

## Spec Change Log

- 2026-07-16: Implemented the approved root policy, one-time renormalization, documentation alignment, duplicate-action closure, and all verification gates on `main`.
- 2026-07-16: Adversarial review patched the extensionless Unix-hook EOL gap, narrowed archived patch/diff whitespace exceptions to `_bmad-output`, corrected the evidence-whitespace rationale, and added a CI-enforced attribute/index regression guard.

## Design Notes

The root policy deliberately keeps repository history/index text canonical as LF while preserving the established CRLF working-tree convention. Explicit LF exceptions protect Unix execution and cross-platform tooling; explicit Razor rules make the recurrent failure mode directly testable. The one-time broad mechanical diff must remain reviewable through content-equivalence evidence.

Whitespace attributes disable only trailing-space diagnostics for Markdown, archived `_bmad-output/**/*.patch`/`_bmad-output/**/*.diff` review evidence, and the exact historical `review-7-2/bundle.txt` artifact. This aligns Markdown with `.editorconfig`'s `trim_trailing_whitespace = false`, prevents pre-existing evidence whitespace from invalidating the EOL-only normalization check, and leaves source-file whitespace checks active.

Verification covered 4,035 pre-existing non-submodule index entries with zero unexpected CR-insensitive mismatches and byte-identical declared binaries. Fresh checkouts under conflicting local Git settings produced `i/lf w/crlf` Razor files and `i/lf w/lf` tooling files; a controlled Razor edit yielded one hunk with one added and one removed line. The exact cached whitespace check and shell syntax checks passed, the Release solution build completed with zero warnings/errors, and the focused Web suite passed 492/0/0.

## Verification

**Commands:**
- `git check-attr text eol -- src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css tools/test.sh tools/check-story-file-scope.py .github/workflows/ci.yml` -- expected: Razor is `text/eol=crlf`; tooling is `text/eol=lf`.
- `git check-attr text eol -- .githooks/pre-commit .githooks/commit-msg` -- expected: both extensionless executable hooks are `text/eol=lf`.
- `git ls-files --eol '*.razor' '*.razor.css'` -- expected after a fresh checkout: every entry is `i/lf w/crlf attr/text eol=crlf`.
- `git -c core.whitespace=cr-at-eol diff --cached --check` -- expected: no whitespace or conflict-marker errors.
- `bash -n tools/test.sh tools/verify-cli-pack.sh .githooks/pre-commit .githooks/commit-msg` -- expected: all LF-preserved scripts and hooks parse successfully.
- `python3 -m unittest discover -s tests/tooling/line_endings -p "*_test.py"` -- expected: the CI-enforced attribute and normalized-index contract passes.
- `dotnet build Hexalith.Memories.slnx --configuration Release` -- expected: success.
- `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --configuration Release` -- expected: success, using the documented built-assembly xUnit v3 fallback if the test host is environment-blocked.

## Suggested Review Order

**Policy and checkout contract**

- Start with the authoritative index and working-tree normalization rules.
  [`.gitattributes:1`](../../.gitattributes#L1)

- Confirm editor behavior mirrors Git's explicit LF exceptions.
  [`.editorconfig:59`](../../.editorconfig#L59)

**Unix execution boundary**

- Extensionless hooks remain LF despite the repository's CRLF default.
  [`.gitattributes:13`](../../.gitattributes#L13)

- Editor saves cannot reintroduce CRLF into executable hooks.
  [`.editorconfig:68`](../../.editorconfig#L68)

- The executable hook surface uses Unix shebangs and requires LF.
  [`pre-commit:1`](../../.githooks/pre-commit#L1)

**Guidance and tracking**

- Architecture explains the two-layer Git-index and working-tree convention.
  [`architecture.md:629`](../planning-artifacts/architecture.md#L629)

- Agent guidance makes `.gitattributes` authoritative for future changes.
  [`project-context.md:90`](../project-context.md#L90)

- Duplicate Epic 17/18/25 actions close with verification evidence.
  [`sprint-status.yaml:440`](sprint-status.yaml#L440)

**Regression verification**

- CI executes the repository-owned line-ending contract on every change.
  [`ci.yml:203`](../../.github/workflows/ci.yml#L203)

- Tests cover attributes, index normalization, checkout matrices, binaries, and localized diffs.
  [`line_ending_policy_test.py:76`](../../tests/tooling/line_endings/line_ending_policy_test.py#L76)
