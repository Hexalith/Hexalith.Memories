# Experimental client API diagnostics

Each entry documents a `[System.Diagnostics.CodeAnalysis.Experimental("<id>")]` diagnostic id used in the Hexalith.Memories codebase. Consumers who genuinely need these methods suppress the warning locally with `#pragma warning disable <id>` — the suppression shows up in code review so the surface does not fossilize silently.

| Diagnostic id | Introduced | Scope | Notes |
| :------------ | :--------- | :---- | :---- |
| `HXL001` | Story 7.4 | Quickstart-wizard-support methods: `MemoriesClient.CreateTenantAsync`, `MemoriesClient.CreateCaseAsync`, `MemoriesClient.IngestAsync` | Signature may change in Phase 1.5 when `memories tenant create`, `memories case create`, and `memories ingest` CLI subcommands are wired. Until then, these methods exist primarily to unblock the `memories quickstart` wizard's happy path — see `src/Hexalith.Memories.Cli/Quickstart/` for the current call sites. |
