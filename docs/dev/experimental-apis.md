# Experimental client API diagnostics

Each entry documents a `[System.Diagnostics.CodeAnalysis.Experimental("<id>")]` diagnostic id used in the Hexalith.Memories codebase. Consumers who genuinely need these methods suppress the warning locally with `#pragma warning disable <id>` — the suppression shows up in code review so the surface does not fossilize silently.

| Diagnostic id | Introduced | Scope | Notes |
| :------------ | :--------- | :---- | :---- |
| `HXL001` | Story 7.4 | Quickstart-wizard-support methods: `MemoriesClient.CreateTenantAsync`, `MemoriesClient.CreateCaseAsync`. **Extended by Story 7.5:** `MemoriesClient.GetTelemetrySummaryAsync`. **Graduated by Story 18.4:** `MemoriesClient.IngestAsync` is now stable and **no longer carries `HXL001`** — see the [ingest contract](./ingest-contract.md). | Signature may change in Phase 1.5 when `memories tenant create`, `memories case create` CLI subcommands are wired; the telemetry method may gain latency-percentile fields when the observability surface stabilizes. Until then, these methods exist primarily to unblock the `memories quickstart` wizard and the `memories status telemetry` subcommand — see `src/Hexalith.Memories.Cli/Quickstart/` and `src/Hexalith.Memories.Cli/Commands/StatusTelemetryCommand.cs`. |
