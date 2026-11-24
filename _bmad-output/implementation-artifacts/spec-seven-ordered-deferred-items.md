---
title: 'Complete seven ordered deferred work items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resume the priority-ordered deferred register after DW-331 and complete seven eligible items. Record the reason for each blocked, duplicate or already resolved item. Give every completed item its own Conventional Commit and ledger evidence. Run the complete build and test suite, fix failures and push the commits directly to main.

- DW-400 makes the migration-marker Redis connection mandatory for both embedding activities, so a missing keyed registration fails instead of skipping the guard.
- DW-413 rejects an unrecognized `DAPR_API_TOKEN_MODE` value in the AppHost instead of silently disabling token authentication.
- DW-414 removes the obsolete project-level Dapr component references that duplicate the existing sidecar references.
- DW-416 makes the first-run `test-data/README.md` creation safe when several AppHosts start together.
- DW-418 names the resource, endpoint and available endpoints when the allocated-endpoint lookup fails.
- DW-419 backs off the Redis readiness probe exponentially with jitter inside its existing deadline.
- DW-421 installs the generated Dapr component files by atomic replacement.

</frozen-after-approval>

## Implementation Notes

- Baseline main and origin/main both pointed to 2db030f5ee34a7cd5caa53f526c830392f4f1df8. The previous batch ended at DW-331.
- Skipped with ledger notes: DW-359 and its duplicate DW-380 (evidence-only replay drill, no trigger), DW-389 (umbrella for DW-311 to DW-330), DW-401 (StackExchange.Redis has no per-command cancellation), DW-402 (clearing target fields would break resume), DW-406 (TTL contradicts the migration contract; commands need McpCli), DW-407 and DW-408 (historical process notes), DW-412 (duplicates DW-249), DW-417 (intended dual sinks) and DW-420 (a partial submodule already fails loudly; a stricter guard needs shared build-layout work).
- Closed as already resolved: DW-391 (same item as DW-331), DW-393 and DW-394 (fixed by Story 14.4) and DW-415 (descriptive locator message since acfdf211).
- DW-400 makes the keyed Redis parameter required in both embedding activities. Dapr.Workflow creates activities through ActivatorUtilities, which now refuses to build an unguarded activity.
- DW-414: the decompiled CommunityToolkit.Aspire.Hosting.Dapr 17.0.0-preview.1.261120-0454 lifecycle hook reads DaprComponentReferenceAnnotation only from sidecar resources, so the project-level references were dead. The AppHost-wide CS0618 NoWarn became pragmas around the four AddProject calls; Directory.Build.props still keeps CS0618 out of warnings-as-errors.
- DW-413, DW-416, DW-418, DW-419 and DW-421 moved Program.cs local functions into single-purpose internal AppHost classes so the IntegrationTests friend assembly can test them. The new classes follow the existing OpenBao test convention and carry Category=Integration.
- DW-421 reuses OpenBaoProtectedFileSystem.WriteAllTextAtomically, which already installed the OpenBao component files.


## Review Triage Log

- Medium, patched DW-419. The first jittered delays (125 to 500 ms) probed faster than the former fixed 500 ms cadence. The policy now starts at 1 s and caps at 4 s, and a test asserts the 500 ms floor.
- Low, patched DW-418. The no-endpoints message omitted the requested endpoint and an empty-list branch was unreachable because TryGetEndpoints already returns false without endpoint annotations.
- Low, patched DW-413. MCP handler comments named the removed ResolveDaprApiTokens function. A padded enabled or disabled value now gets a whitespace hint without echoing the value.
- Low, patched DW-400. The chunk marker test matched any key and flags; it now requires the tenant active-marker key with DemandMaster. The chunk constructor documents its parameters.
- Deferred, low. Each Redis-consuming resource still runs its own readiness probe loop. Sharing one probe per allocation cycle predates this change and is recorded in the ledger.
- Deferred, low. IndexGraphActivity keeps a compatibility constructor that skips the tenant-ownership check without keyed Redis. The server host always registers it; recorded in the ledger.
- Rejected, low. The probe loop wiring and the separate OpenBao initialization retry lack loop-level tests. The real AppHost topology test exercises the Redis probe, and the OpenBao retry is outside DW-419.
- Rejected, low. The reused helper's chmod failure text mentions OpenBao, and TestDataRoot stages its own README. The text appears only when restricting a file fails, and the README must not become owner-only.
- False. Windows readers cannot observe a partial component either: File.Move replaces atomically or fails loudly, as the earlier truncating write would against an exclusive reader.
- False. DW-400 cannot start a healthy host that fails later: the server host always calls AddKeyedRedisConnections, and the indexing activities already require the same connection when first used.
- Rejected, low. Runtime MCP and access-telemetry readers still treat unknown token modes as disabled; with sidecar token enforcement that produces a loud 401 rather than silent exposure, and DW-413 targets the AppHost.
- Rejected, low. The pure AppHost helper tests carry Category=Integration like the existing OpenBao tests in the only friend assembly, and they run in the fast integration lane on every pull request.
- False. Concurrent model builds cannot share the component directory: both model tests hold the same EnvVarScope gates for their whole run, which serializes them. The duplicated eventstore row is negligible.
- Rejected, low. The Redis substitute helpers now appear in three test files; moving them to TestHelpers is broader than this batch.
