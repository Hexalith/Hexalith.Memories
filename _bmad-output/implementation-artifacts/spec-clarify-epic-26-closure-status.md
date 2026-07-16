what next?
---
title: 'Clarify Epic 26 Closure Status'
type: 'chore'
created: '2026-07-16'
status: 'done'
route: 'one-shot'
---

# Clarify Epic 26 Closure Status

## Intent

**Problem:** The Epic 16 carry-forward convention suggests closing an epic when its stories and retrospective are done, but Epic 26 has an explicit unresolved benchmark quality gate that prevents closure.

**Approach:** Keep the Epic 26 row and alignment action in progress, and add dated explanations that tie the intentional exception to the unmet 7-of-8 benchmark gate.

## Suggested Review Order

- Preserve the hard quality gate while explaining why completed stories do not close Epic 26.
  [`sprint-status.yaml:391`](sprint-status.yaml#L391)

- Keep the recurring alignment action active and document the governed exception.
  [`sprint-status.yaml:446`](sprint-status.yaml#L446)

## Resolution — 2026-07-16

The intentional exception is resolved. Production RRF was recalibrated to `k=10` with live syntactic/semantic/graph defaults `0.30/0.35/0.35`; the unchanged Release benchmark passed 17/17 tests and 8/8 strict hybrid wins in two runs with identical per-query metrics. `epic-26`, the benchmark action, and the Epic 16 alignment carry-forward are now `done`. See [`epic-26-benchmark-remediation-evidence-2026-07-16.md`](epic-26-benchmark-remediation-evidence-2026-07-16.md).
