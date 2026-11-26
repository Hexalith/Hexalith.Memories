---
title: 'Complete six deferred restore, benchmark, routes, telemetry, and documentation items'
type: 'bugfix'
created: '2026-11-26'
status: 'done'
route: 'oneshot'
baseline_commit: '8b71994207ddc1fe40d45ffe649bfad8a256d60b'
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable entries in ledger order. Verify and close each item in its own commit, record why any skipped item remains open, and run the full build and test suite before pushing to main.

</frozen-after-approval>

## Implementation Notes

- DW-568 covers the HTTP size metadata and 413 mapping on both import routes, staging at 512 MiB and rejection at 512 MiB plus one byte in Redis, lease and data renewal and cleanup, and workflow paging for 100,003 IDs. The evidence covers the size and paging boundaries at their owning seams.
- DW-571 uses the benchmark runner already invoked by nightly CI. It keeps the full quality gate and runs the real benchmark in a fresh process against newly seeded stores. It compares all output fields other than the run timestamp.
- DW-575 uses the Server endpoint data source for the exact documented REST rows and count, including grouped routes and verb metadata.
- DW-576 checks route declarations across the production source tree and verifies that the running Server has no matching route. The separately documented EventStore SDK endpoint remains admitted on the dedicated domain-service host.
- DW-578 applies the lifecycle request-parameter projection before provider fan-out, preserving operator audit correlation and lifecycle identity marking.
- DW-583 shares the Markdown newline normalizer so diagnostics use the same line boundaries as document parsing.


## Review Triage Log

- DW-568 review prompted real one-byte-over rejection and cleanup, a pending terminal activity to prove cleanup ordering, and a Redis page straddling the append boundary. The large staged payload proves byte fidelity and storage behavior. The workflow test supplies unit-count evidence without claiming an end-to-end large recovery rehearsal.
- DW-571 review prompted rejection of matching payloads with Boolean query counts or contradictory passing scores.
- DW-575 review prompted explicit Production route inventory with only the Dapr token startup validator substituted out of the test host.
- DW-576 review prompted Roslyn constant resolution across files and a regression for a composed route alias.
- DW-578 review caught tenant-delete validation reading the raw operation key after projection. The lifecycle uses the projected target kind and now has a tenant-delete emitter-to-queue regression.
