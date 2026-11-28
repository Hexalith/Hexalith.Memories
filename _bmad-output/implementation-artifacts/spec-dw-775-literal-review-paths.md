---
title: 'Represent literal Git filenames in review readiness'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '448ac4b8b6a51de160d25d6d3b5416e926828283'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-775 addresses “Represent literal Git filenames in the story review-readiness File List.” NUL-delimited Git input retains literal backslashes and newlines, but the authored File List and exclusions only have normalized single-line paths. Provide a documented, unambiguous way to represent the same path in C1.

## Boundaries & Constraints

Preserve all existing File List and Exclusions forms for ordinary paths. Use an explicit opt-in escaped form with strict parsing and no aliasing. Keep changes local to this repository. The separate hosted story-scope transport belongs to Hexalith.CiCd. Use run-with-temp for scratch work. Do not edit the ledger/spec or commit.

</frozen-after-approval>

## Code Map

tools/check-story-review-readiness.py owns File List, Exclusions, and changed path collection. CONTRIBUTING.md documents the limitation. tests/tooling/story_review_readiness contains local gate fixtures; prefer actual Git NUL-delimited paths for focused regressions.

## Tasks & Acceptance

- [x] Implement the scoped behavior and focused regression evidence.

Given a Git path containing a literal backslash or newline, a properly encoded File List row matches exactly. Given malformed encoding, an alias, or a different literal file, C1 fails. Given an existing conventional File List, behavior remains compatible.

## Implementation Notes

## Spec Change Log

## Review Triage Log

The local exact-path regressions passed through the NUL-delimited entry point. Hosted filename transport and routine enrollment remain with Hexalith.CiCd.

## Verification

Run the entire story review readiness tooling suite, including red/green evidence against a disposable repository, and the relevant story-scope controls.

The four real Git regressions failed against the original gate. After the change, 15 readiness tests and 22 story-scope controls passed. The local gate round-trips literal paths through Git NUL delivery. Hosted CI path delivery stays outside this item.

## File List

- `CONTRIBUTING.md`
- `tests/tooling/story_review_readiness/story_artifact_resolution_test.py`
- `tools/check-story-review-readiness.py`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `b7f49e3396b5008508a075e4edece5c2d6a41046` | 15 readiness and 22 story-scope tests | Matched 3/3 |
| 2026-11-28 | code-review | `b7f49e3396b5008508a075e4edece5c2d6a41046` | 15 readiness and 22 story-scope tests | Matched 3/3 |
