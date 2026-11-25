---
title: 'Complete seven ordered evidence cockpit follow-ups'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next seven actionable deferred items in ledger order, with individual Conventional Commits and ledger closure. Record reasons for skipped items, run the full solution build and tests, fix failures, and push to main.

- DW-459 displays the localized permission context in the scope header.
- DW-461 displays source case information and annotation counts within authorized citations.
- DW-462 displays reported result totals, returned counts, and indexed-memory availability within authorized results.
- DW-463 reflects backend degradation in the trust-strip health badge with restrictive scope taking precedence.
- DW-464 corrects Story 17.1's original accessibility claims and links the later specimen evidence and remaining gaps.
- DW-469 asserts actual source citation DOM order with deliberately nonsequential ranks.
- DW-470 treats negative source counts as unavailable and verifies that the maximum integer remains displayable.

</frozen-after-approval>

## Implementation Notes

- The baseline was `d603ffd53dc410a3c1245462dd29c6e63e5b8d69`. The previous batch ended at DW-421. This batch follows ledger order and records duplicate IDs against their original items.
- The UI work uses the existing Evidence Packet contract, Fluent V5 components, and the English/French resource catalogs. Product Web activation is outside this batch.

- DW-437 remains open pending a producer signal and a precedence decision. DW-460/DW-478 remain open pending an authorized design for opaque expansion handles. DW-468/DW-486 remain open pending a real assistive-technology landmark pass.
- DW-482 tracks the product-route and assistive-technology work after the Story 17.1 evidence correction. DW-477, DW-479, DW-480, DW-481, DW-487, and DW-488 are closed as duplicate records.
- Numbered ledger count after implementation: 216 current-format and 3 legacy open entries, for 219 total.

## Review Triage Log

- Medium, patched. The new permission, result and citation metadata honor authorization omissions. The trust strip, restrictive banner and citation visibility agree on that signal.
- Medium, patched. Citation case text uses the canonical packet sanitizer and the existing web sanitizer. The bUnit canary includes a synthetic connection string.
- Medium, patched. A degraded search with an available axis retains a reported zero. Diagnostic packets and packets with no available axes display unavailable counts.
- Medium, patched. Loading and error states clear the prior permission context. A tenant or case permission label must agree with the packet scope.
- Low, patched. Authorized citations render the localized fallbacks recorded in the mapping when case metadata or a valid annotation count is absent.
- Low, patched. The result-count mapping records all availability inputs. Index status explicitly refers to the tenant.
- Low, patched. Trust health tests assert the rendered badge slot and accessible name.
- Low, patched. Story 17.1 links the residual accessibility inventory, including specimen axe manual review, non-Chromium coverage and the measured touch gaps.

## Verification

- Full Debug/source and Release/package builds of Hexalith.Memories.slnx passed with zero warnings and errors.
- All nine unit and contract projects passed 5,688 cases with one existing skip. The initial Access Telemetry run exposed independent fake UTC sources whose intervals could stop overlapping when the requests were scheduled apart. With a common fake clock, all 265 Access Telemetry cases passed.
- Fast integration passed 690 cases with nine existing skips across the full run and the focused retry. The first run passed 635 cases and failed 55 at the same FalkorDB fixture initialization. All 55 passed after the fixture waited for the published port and used the IPv4 loopback address.
- The complete nightly run passed 45 of 55 cases. The originally failing cases all passed on isolated reruns. The last three passed with dedicated loopback Dapr placement and scheduler containers after shared-topology reruns showed a Redis container disappearing and Dapr state-store I/O timeouts. The existing restart flake entry records that evidence. The teardown fix tolerates only known Docker results when DCP removes the same owned container concurrently. Both nightly classes using the FalkorDB fixture passed all 32 cases.
- The benchmark project passed all 17 cases. The Chromium specimen suite passed all five checks and validated nine artifacts.
- Commitlint and npm registry-signature and semantic-release configuration validation passed. The supplemental Python suites at `release_preflight`, `publish_nuget`, `publish_containers`, and `production_deployment_evidence` exceeded both local time budgets. These runs ended with exit 124.
- The ledger contains 219 numbered and 19 unnumbered entries explicitly marked open.
