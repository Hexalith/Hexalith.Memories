# Coordinator acceptance audit — 2026-11-16

The coordinator inspected the unified baseline-to-working-tree diff, including untracked files.
Bulk machine evidence was represented by Git binary markers and independently parsed/verified;
source, documentation, retirement and test changes were inspected as text.

- Snapshot fixture rerun: 16/16 pass, zero skips.
- CI alignment rerun: 15/15 pass, zero skips; changed CI actionlint clean.
- Every evidence JSON parses. Three sorted snapshot byte streams and their SHA-256 receipts
  match exactly: `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
- Persisted timestamps bracket all rehearsal intervals through the final isolated modules.
- Independently derived all 104 method IDs from the exact v4.0.0 Git source with Python AST,
  then matched that set to canonical IDs in the unchanged raw passing module logs. All 104
  executed exactly once, with no missing methods, duplicates or skips.
- The C# diff removes the dead runner/parser family, preserves the duplicate-ID guard and adds
  the absence fact. Retained Debug build/class output confirms 0 warnings/errors and 101/101 cases.

## I/O matrix coverage

| Scenario | Executed coverage | Result |
| --- | --- | --- |
| Identity unchanged | `test_identical_positive_and_every_added_removed_changed_domain`, CLI positive/receipt fixture; both live snapshot comparisons | pass |
| Additive release | `test_additive_positive_with_latest_unchanged_or_moved_to_new_release`, refusal mutations including new single-image tags and Latest moved elsewhere | pass |
| Historical image format | `test_historical_manifests_and_indexes_preserve_original_bytes_and_media_type`, ambiguous format/bytes refusal fixture | pass |
| Ambiguous probe | HTTP status, unparseable/timeout, missing digest, malformed Latest and credential fixtures | pass; no snapshot/receipt written |
| Parity regression | Manual negative control in a disposable copy: coordinator was presented a packages row with verdict `regression`, refused removal, and verified the original runner remained byte-identical; checkpoint incomplete | pass |

The regression row is a manual workflow gate under D5. No matrix validator or scripted target
check was added. The real matrix independently shows zero regression rows and its retirement
occurred after the full identity proof. The historical all-in-one HTTP/1.0 fixture failures remain
visible; only the complete passing isolated-module set receives qualification credit. D2's next
owner-dispatched additive verification remains a recorded obligation, with no live credit here.
