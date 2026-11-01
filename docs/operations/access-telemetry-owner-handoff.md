# Access-telemetry qualification: owner handoff

**Current state — 2026-11-01 scope reset.** The owner approved the
[Story 27.4 scope reset](../../_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md).
Story 27.4 and Epic 27 are done.

- **Proven:** local functional retention run `0673d50339f4e1058c26e3a9b2d680a2` (2026-10-25) passed the real
  one-hour, two-cohort scenario: native TRX 1/1, strong expiry absence, due-index/reminder progress and unchanged
  newer records, with verified disposal. Limits: one writer, Development, logical expiry only. See the
  [results report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md).
- **A41:** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is resolved by accepted debt (DW-17), not closed as qualified.
  Memories does not bound Production access-telemetry retention while the lifecycle store stays disabled.
- **Operator action:** configure access-telemetry retention of 7 days or less on every log/OTLP backend; see
  [Production access-telemetry retention](access-telemetry-lifecycle.md#production-access-telemetry-retention).
- **Parked:** the full Production qualification campaign ([historical inventory](access-telemetry-full-qualification.md))
  is parked in deferred-work entry DW-730. It reopens only when a Production deployment path (the Memories overlay or
  Platform composition) sets `AccessTelemetryLifecycle:Enabled=true` or runs a lifecycle Deployment above zero
  replicas. "Do recommended", a free schedule window or new Platform capability does not reopen it. When triggered,
  re-plan against the real Production target.
- **Stopped:** node1 rootless-Docker setup, independent calibration, host capture and S3 calibration preparation.
  Earlier approvals are not revived. Retained resources stay in place pending owner cleanup; the report lists them.
- **Next review:** 2027-05-01 (sprint-status Epic 27 action item), or earlier on the trigger.

The earlier handoff text (2026-10-21 to 2026-10-25), which named the full campaign as the next work, is historical
and remains in Git history.
