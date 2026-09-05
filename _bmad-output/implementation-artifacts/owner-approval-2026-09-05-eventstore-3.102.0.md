# Owner approval — EventStore published identity 3.102.0

- **Date:** 2026-09-05
- **Approver:** repository owner (chat directive: “I, owner Approve”)
- **Decision:** Memories may leave Story 28.1’s proof-local-feed pin and consume the published nuget.org EventStore family.

## Approved identity

| Field | Value |
| --- | --- |
| Package version | `3.102.0` |
| Source SHA / tag | `4ae9cee1e9abe050402fd1405a9abd54892ba13f` (`v3.102.0`) |
| Builds catalog | `308e3921d60d2e8f87dd69a7f9b6f3dd016df9ef` (`HexalithEventStoreVersion=3.102.0`) |
| Package source | nuget.org (tracked `NuGet.config`; no ephemeral proof feed) |

## Consequence

CI/Release no longer run `tools/ci/provision-eventstore-local-feed.sh` for restores. Debug/source still uses the EventStore submodule at the approved SHA; Release/package resolves `Hexalith.EventStore.*` at `3.102.0` from nuget.org.
