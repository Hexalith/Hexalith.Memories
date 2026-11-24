---
title: 'Complete ten deferred work items in ledger order'
type: 'chore'
created: '2026-11-24'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem.** The deferred-work ledger includes actionable findings, migration artifacts, and entries that depend on future events or decisions.

**Approach.** Process the ledger in order, implement and verify ten actionable items, and record the reason for each skip. Commit each completed item separately. Rebase onto current origin/main, run the full suite, and push on success.

</frozen-after-approval>

## Implementation Notes

- DW-31 adds a dated link from Epic 14 to the later Epic 15 plan and Story 15.5 reconciliation. Both links resolve. Review clarified the date of the original observation and linked directly to the completion notes.
