---
title: 'Close five deferred story scope input and diagnostic gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-24, DW-25, DW-164, DW-165 and DW-171 in the existing story file scope validator. Given a direct allowed-list bullet without a backtick-wrapped path, when scope is parsed, then warn with the artifact and line number without authorizing that bullet; fenced, indented and read-only examples remain excluded. Given local artifact or input paths, when diagnostics render, then use relative POSIX paths and omit Windows drive prefixes without changing lookup or authorization. Given an invalid UTF-8 commit-message file, when validation runs, then fail with actionable encoding guidance and no traceback. Given a UTF-8 BOM in a changed-files input, when paths are checked, then treat its first path exactly like the corresponding BOM-free input, including forbidden paths. Given nested Markdown headings in File Scope, when allowed, read-only, forbidden or unrelated subsections begin, then collect only explicitly allowed lists; sibling or parent sections end File Scope.

Preserve owner discovery, exact glob matching, explicit overrides, forbidden-default protection and existing fence handling. Document the input and authoring contract, resolve the five deferred entries with verification evidence, independently review the changes, then commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Route assessment: one bounded developer-tool correction across the validator, its existing fixtures, contributor guidance, this trace and five register entries; no unresolved intent, new dependencies, runtime APIs, application state changes or deployment. Commit and push are explicitly authorized. No application or AppHost code changes, so an application launch is unrelated to verification.
- Baseline: clean main at `bd248eb09170bfed14bbda61320ddd9c6797e9bf`. Read root guidance, the declared AI.Tools baseline and Git rules, EditorConfig/attributes, build and contributor/test configuration, and architecture AD-1/AD-19/AD-20. Source/runtime and submodules remain outside the bounded implementation footprint.
- Code map: `tools/check-story-file-scope.py` owns input decoding, scope parsing and diagnostics. `tests/tooling/story_scope/story_scope_validator_test.py` supplies subprocess fixtures and direct-module loading. Reuse its frozen fixture and scope-section assertions. `CONTRIBUTING.md` owns authoring guidance. Existing CI already runs the fixture directory.
- Baseline command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -v` passed 53/53 methods, exit 0. Extend tests for each selected defect plus denial controls; run dependent commit-gate fixture suites if parser consumers or hooks require them.
- DW-171's historical wording calls `##` a subheading; Markdown makes it a sibling of `## File Scope`. Support real nested headings at levels three through six while keeping sibling/parent termination. Unknown nested headings reset authorization until an explicit allowed label; plain rationale prose retains existing behavior.

- Regression-first verification against the original tool produced 16 failing subcases and one missing-helper error across 65 methods; the original 53 controls remained green. Implemented a display-only relative-path formatter, shared strict UTF-8-sig reader, direct-bullet warnings and nested-heading transitions. Fixed fixture assertions to distinguish an absolute diagnostic path from the same suffix within a relative path.
- Implementation verification passed 67/67 methods in 4.505s, exit 0. Discovery increased from 53 to 67 (+14 methods); parameterized cases additionally cover actual Windows ntpath same-drive, cross-drive and UNC semantics, encoding errors, BOMs and refusal paths.
- Compared old and new scope parsing for 102 existing implementation artifacts. Only one list changed: a historical artifact repeated its three allowed paths under a nested File List, now correctly excluded (six entries became three, with the unique allowed set unchanged).
- Updated exactly DW-24, DW-25, DW-164, DW-165 and DW-171 with dated resolutions, evidence and reopen conditions.

- Final verification: 71/71 fixture methods passed in 4.367s, exit 0; independent review also reran all 71 successfully and reported no introduced regression remaining. Comparable discovery is 53 -> 71 (+18 methods). Real Windows path rules use ntpath with a controlled working directory on this Linux host; native Windows CLI execution was not run. All 102 historical artifact comparisons preserve their unique allowed sets.
- Only the configured Blind Hunter layer ran; no configured review layer was skipped. Five original review findings were patched, and one pre-existing unsupported Setext syntax gap was deferred. The trial Setext extension was withdrawn after follow-up review exposed Markdown block-context complexity; the final implementation retains the ATX authoring contract without a new dependency. Frozen intent and the five selected dispositions are unchanged.
- Exact five-file scope and pre-commit commitlint validation passed. Tenant isolation and story slice checks reported no affected surfaces/stories; review readiness reported a no-op because the minimal standalone spec has no governed ledger/File List/evidence table. These are scope no-ops, not runtime evidence. Git fetch confirmed main and origin/main matched the baseline before commit preparation. The repository-managed hooks remain enabled.

## Review Triage Log

- Medium, patched: a repeated `## File Scope` could reopen authorization after a sibling boundary; termination now precedes section recognition, with a repeated-heading refusal case.
- Medium, patched: indented example labels could authorize later direct bullets; four-column space/tab indentation is excluded before label parsing, with three indentation controls.
- Medium, deferred: Setext boundaries are an existing unsupported syntax outside DW-171's nested ATX goal. Documented the ATX-only authoring contract and appended one follow-up entry to deferred-work.md rather than adding a partial Markdown parser.
- Low, patched: a colon outside bold heading emphasis was rejected; label normalization now handles either colon placement and the positive heading matrix covers both.
- Medium, patched: test expectations raised ValueError when temp and checkout paths used different Windows drives; the expected-location helper handles drive/UNC differences independently of tool lookup.
- Medium, patched: Git subprocess input/output used locale-dependent codecs after UTF-8 file reads; explicit UTF-8 subprocess encoding and forced UTF-8 log output now pass a C-locale message with Latin/Japanese text and a real temporary Git repository configured for legacy log output.
- Medium, patched by withdrawal: trial Setext lookahead misclassified lazy list continuation and consecutive thematic breaks as headings. The final tool removes that lookahead; compatibility controls preserve later allowed entries.
- Medium, patched by withdrawal: trial Setext state handling dropped a two-space-indented list paragraph after a blank. The trial state machine is absent from the final tool; the broader syntax remains in the single deferred follow-up.
- Medium, patched by withdrawal: trial Setext state handling treated an ordered marker starting at two as a new list within an existing paragraph. The trial state machine is absent from the final tool; any future Setext implementation must qualify this context.

## File Scope

Allowed files for this story:

- `tools/check-story-file-scope.py`
- `tests/tooling/story_scope/story_scope_validator_test.py`
- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-story-scope.md`
