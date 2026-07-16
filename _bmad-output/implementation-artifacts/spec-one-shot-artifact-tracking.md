---
title: 'Clarify One-Shot Artifact Tracking'
type: 'refactor'
created: '2026-07-16'
status: 'done'
route: 'one-shot'
---

# Clarify One-Shot Artifact Tracking

## Intent

**Problem:** Bounded one-shot completion traces could be mistaken for unregistered stories, and duplicate approved proposals prescribed incompatible tracking models.

**Approach:** Keep terminal one-shot traces self-tracked outside `development_status`, require normal specs or registered stories when work outgrows that boundary, supersede the duplicate standalone-register proposal, and treat older traces as historical rather than precedent.

## Suggested Review Order

**Canonical Tracking Boundary**

- Start with the authoritative distinction between stories, one-shots, and supporting evidence.
  [`epics.md:448`](../planning-artifacts/epics.md#L448)

- Confirm sprint accounting excludes one-shots and preserves registered lifecycle ownership.
  [`sprint-status.yaml:26`](sprint-status.yaml#L26)

- Verify implementation agents receive the same concise prospective rule.
  [`project-context.md:108`](../project-context.md#L108)

**Decision Reconciliation**

- Review the approved rationale and historical-trace boundary behind the selected convention.
  [`sprint-change-proposal-2026-07-16-one-shot-artifact-tracking.md:89`](../planning-artifacts/sprint-change-proposal-2026-07-16-one-shot-artifact-tracking.md#L89)

- Confirm the incompatible standalone-register proposal is explicitly superseded and non-executable.
  [`sprint-change-proposal-2026-07-16-standalone-artifact-tracking.md:17`](../planning-artifacts/sprint-change-proposal-2026-07-16-standalone-artifact-tracking.md#L17)

- Confirm its draft spec is terminally retained as unimplemented decision history.
  [`spec-standalone-artifact-tracking.md:6`](spec-standalone-artifact-tracking.md#L6)

**Closure and Review Evidence**

- Check both retrospective actions remain preserved and closed under the chosen policy.
  [`sprint-status.yaml:499`](sprint-status.yaml#L499)

- Inspect unrelated credible findings captured without expanding this correction.
  [`deferred-work.md:2201`](deferred-work.md#L2201)
