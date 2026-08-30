---
title: 'Bump Memories EventStore consumption to 3.100.0'
type: 'build'
created: '2026-08-30'
status: 'done'
review_loop_iteration: 0
baseline_commit: '691eeeacba01a9893793020f61f95e52c35879a2'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-29.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Memories `origin/main` still consumes EventStore through committed gitlinks `references/Hexalith.EventStore` `9fc0edb8` (`v3.99.0-17`) and `references/Hexalith.Builds` `2b0faab9` (`HexalithEventStoreVersion` `3.99.0`). The working tree already stages EventStore `10051a68` (`v3.100.0`) and Builds `e1026cb6` (`HexalithEventStoreVersion` `3.100.0`), plus companion FrontComposer and Tenants pointers. Release package mode must resolve the published `3.100.0` family.

**Approach:** Adopt EventStore `3.100.0` from that staged snapshot. Prove package-mode restore and Release build. Change Memories consumer code only if that build names a break. Do not finish `spec-pushall-sync-2026-08-29` (no parent commit or push of the four gitlinks unless Ask First).

## Boundaries & Constraints

**Always:** Resolve EventStore package identity only through Builds `HexalithEventStoreVersion` (no Memories-local `PackageVersion` override). Keep root `Directory.Packages.props` as an import-only CPM bridge. Preserve the four already-staged gitlinks. Edit submodule *contents* only inside the owning repo. Validate with `UseHexalithProjectReferences=false` restore and Release `-warnaserror` build. Follow Conventional Commits and `hexalith-git-instructions.md` if any Git write is later authorized.

**Ask First:** Unstaging or committing any parent gitlink. Completing or pushing the `spec-pushall-sync-2026-08-29` envelope. Adding EventStore packages or host SDK wiring beyond the current Client + Aspire security surface. Accepting a compile fix that changes command-submit or JWT security semantics.

**Never:** Nested or `--recursive`/`--remote` submodule updates. Editing files inside `references/Hexalith.EventStore`, `references/Hexalith.Builds`, `references/Hexalith.FrontComposer`, or `references/Hexalith.Tenants`. Splitting the EventStore NuGet family across versions. Using `chore` or machine-shaped commit subjects. Bypassing commit hooks. Claiming source-SHA byte identity with the NuGet packages, or claiming runtime/Pact/provider success from catalog selection alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Package restore | Staged Builds `e1026cb6`, `UseHexalithProjectReferences=false` | `Hexalith.EventStore.Client` and `Hexalith.EventStore.Aspire` restore at `3.100.0` | Halt on restore failure; do not invent a local pin |
| Release build | Same graph, TreatWarningsAsErrors | 0 errors, 0 warnings | If compile names a consumer break, patch only the listed consumer files |
| Empty API delta | `git diff 9fc0edb8..10051a68 -- src` empty | Consumer files stay unchanged | Do not drive-by refactor from the tag notes |
| Isolation | Four staged gitlinks plus this spec | Unrelated paths untouched; pushall spec remains in-progress | Do not unstage companion gitlinks to “narrow” the bump |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds` -- staged gitlink `e1026cb61162546571ee0102c525bcf42b9ce7fa`; `Props/Directory.Packages.props:8` `HexalithEventStoreVersion=3.100.0`; EventStore `PackageVersion` rows `:40-52` bind to that property. Committed parent still `2b0faab9` / `3.99.0`.
- `references/Hexalith.EventStore` -- staged gitlink `10051a68eb1db322a4f7fa91934d880ce1409687` tag `v3.100.0`. Range `9fc0edb8..10051a68` is three commits (evidence hashes, nested pointers); **no `src/` library diff**. nuget.org lists all 14 release packages at `3.100.0`.
- `references/Hexalith.FrontComposer` / `references/Hexalith.Tenants` -- already-staged companion gitlinks `f84b68b4` / `c5fa0082`. Preserve; do not treat as this bump’s product work. Publication remains `spec-pushall-sync-2026-08-29`.
- `Directory.Packages.props` -- import-only; no EventStore override. Read-only.
- `Directory.Build.props:41-48` -- `UseHexalithProjectReferences` defaults false; `HexalithEventStoreFromSource` only when source-debug.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:15-19` -- `Hexalith.EventStore.Aspire` package unless `HexalithEventStoreFromSource`.
- `src/Hexalith.Memories.AppHost/Program.cs:12,38,355-357,487-489` -- `AddHexalithEventStoreSecurity()`, `HexalithEventStoreSecurityResources`, `WithJwtBearerSecurity`. AppHost does **not** call `AddHexalithEventStore`.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:52` -- versionless `Hexalith.EventStore.Client`.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:91-99` -- `AddEventStoreGatewayClient` Dapr invoke base `…/invoke/eventstore/method/`.
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs:12-13,19,42-56` -- `IEventStoreGatewayClient.SubmitCommandAsync` / `SubmitCommandRequest`.
- `src/Hexalith.Memories.EventStore/**` -- local Memories adapter namespace; **not** a platform package. Leave unless a named compile error points here.
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs:18-26` -- source-drift guard for Aspire usings and security helpers.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds` -- Confirm staged gitlink `e1026cb61162546571ee0102c525bcf42b9ce7fa` catalogs `HexalithEventStoreVersion` `3.100.0`; do not edit Builds files -- Package identity lives in Builds
- [x] `references/Hexalith.EventStore` -- Confirm staged gitlink `10051a68eb1db322a4f7fa91934d880ce1409687` equals tag `v3.100.0`; do not edit EventStore files -- Source pointer already at the requested release
- [x] `references/Hexalith.FrontComposer` -- Leave the staged gitlink untouched -- Companion pointer is not EventStore product work
- [x] `references/Hexalith.Tenants` -- Leave the staged gitlink untouched -- Companion pointer is not EventStore product work
- [x] `src/Hexalith.Memories.AppHost/Program.cs` -- Keep Aspire security helpers compiling against `3.100.0`; change only if Release build names a break -- Current consumer surface
- [x] `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` -- Keep the Aspire package / source dual-ref; change only if that shape breaks -- Default remains NuGet
- [x] `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` -- Keep versionless `Hexalith.EventStore.Client` -- Version must stay catalog-driven
- [x] `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- Keep `AddEventStoreGatewayClient` compiling; change only if the registration API breaks -- Server write-path registration
- [x] `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs` -- Keep `SubmitCommandAsync` compiling; change only if the gateway contract breaks -- Server write-path client
- [x] `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` -- Ran the class (11 facts); added catalog, assets, gateway-contract, and no-local-pin guards so every I/O matrix row has a passing covering test -- Guard EventStore 3.100.0 consumption
- [x] `_bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md` -- Record restore, build, and focused-test evidence -- Story closeout

**Acceptance Criteria:**
- Given the staged Builds catalog, when `HexalithEventStoreVersion` is read from `references/Hexalith.Builds/Props/Directory.Packages.props`, then its value is `3.100.0` and EventStore `PackageVersion` rows use that property.
- Given `Hexalith.Memories.slnx` and `UseHexalithProjectReferences=false`, when restore and Release build with `-warnaserror` run, then they finish with 0 errors and 0 warnings and restore `Hexalith.EventStore.Client` plus `Hexalith.EventStore.Aspire` at `3.100.0`.
- Given `git diff 9fc0edb8..10051a68 -- src` is empty, when the consumer files above are reviewed, then they are unchanged unless the Release build names a compile error in those files.
- Given `AppHostSecurityConfigurationTests`, when the test class runs, then every fact passes.
- Given the dirty root tree, when this story finishes, then the four staged gitlinks remain staged, no parent commit or push has been created, and `spec-pushall-sync-2026-08-29` is still `in-progress`.

## Spec Change Log

- 2026-08-30 — Adopted staged EventStore `3.100.0` / Builds `e1026cb6` without consumer-file edits. Release package restore and `-warnaserror` build named no API break; `AppHostSecurityConfigurationTests` passed. No parent commit or push.
- 2026-08-30 — Matrix test audit: added six covering facts to `AppHostSecurityConfigurationTests` (catalog pin, versionless Client/Aspire refs, gateway submit contract, no root CPM override, restore assets at `3.100.0`). Class re-ran: 11 passed. Production consumer files still unchanged.
- 2026-08-30 — Tightened `AppHostSecurityConfigurationTests` EventStore 3.100.0 guards (CRLF, Server-only JSON assets parse, catalog family scan, VersionOverride, unsuffixed Hexalith.EventStore forbid).

## File Scope

Allowed files for this story:
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Tenants`
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Design Notes

EventStore `v3.100.0` vs consumed `9fc0edb8` is evidence/tooling and nested-pointer work, not a public API migration. Package identity (`3.100.0` via Builds) and source SHA are separately governed. The four staged gitlinks are the authorized starting snapshot; publishing them is a different spec.

## Verification

**Commands:**
- `git -C references/Hexalith.EventStore describe --tags --exact-match HEAD` -- expected: `v3.100.0`
- `git -C references/Hexalith.Builds rev-parse HEAD` -- expected: `e1026cb61162546571ee0102c525bcf42b9ce7fa`
- `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=false` -- expected: `Hexalith.EventStore.Client` and `Hexalith.EventStore.Aspire` at `3.100.0`
- `dotnet build Hexalith.Memories.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=false` -- expected: 0 errors, 0 warnings
- Build `tests/Hexalith.Memories.Server.Tests` then invoke the test assembly with `-class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` -- expected: all facts pass

**Observed results (2026-08-30):**
- Staged Builds gitlink `e1026cb61162546571ee0102c525bcf42b9ce7fa` matches checkout HEAD. `HexalithEventStoreVersion` is `3.100.0`; EventStore `PackageVersion` rows 40–52 all bind to `$(HexalithEventStoreVersion)`. Builds files were not edited.
- Staged EventStore gitlink `10051a68eb1db322a4f7fa91934d880ce1409687` matches checkout HEAD and `git describe --tags --exact-match HEAD` → `v3.100.0`. `git diff 9fc0edb8..10051a68 -- src` is empty (three non-`src` commits). EventStore files were not edited.
- Staged companion gitlinks remain `references/Hexalith.FrontComposer` `f84b68b4e147238f28ca70219f19233d4b4b64d1` and `references/Hexalith.Tenants` `c5fa0082f610e15046fb2df9a1e0104ef0160762`. Neither index pointer was unstaged. FrontComposer checkout matches the staged SHA. Tenants checkout is still at pre-existing unstaged `4a3eec38d071de0e6622b8b418c59d470ad41c3e` (`MM`); that worktree drift was left untouched because this story must not edit Tenants or unstage the companion gitlink. Package-mode restore does not consume Tenants source.
- `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=false` succeeded (projects already up to date). `src/Hexalith.Memories.Server/obj/project.assets.json` contains `Hexalith.EventStore.Client/3.100.0`; `src/Hexalith.Memories.AppHost/obj/project.assets.json` contains `Hexalith.EventStore.Aspire/3.100.0`.
- `dotnet build Hexalith.Memories.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=false` succeeded: 0 Warning(s), 0 Error(s). No consumer compile break was named, so AppHost `Program.cs` / `.csproj`, Server `.csproj` / `MemoriesServerServiceCollectionExtensions.cs` / `EventStoreMemoriesCommandStore.cs` were left unchanged.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false` then `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` → Total: 11, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0. Matrix coverage: restore → `BuildsCatalog_PinsEventStoreFamilyAt31000` + `ProjectAssets_RestoreEventStorePackagesAtCatalogVersion` + versionless Client/Aspire facts; Release build / empty API delta → `AppHost_InitializesSharedSecurityResource` + `Server_UsesEventStoreGatewaySubmitContract` + dual-ref Aspire helper fact; isolation → `RootDirectoryPackages_DoesNotOverrideEventStoreVersion`.
- Guard re-run (2026-08-30): `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror` → 0 Warning(s), 0 Error(s); then `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` → Total: 11, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0. Assets fact now parses Server `project.assets.json` only (exact `Hexalith.EventStore.Client/3.100.0` package; every `Hexalith.EventStore.*` library version suffix `3.100.0`). Catalog fact scans every EventStore `PackageVersion` row. Client/Aspire facts also forbid `VersionOverride`. Root CPM fact forbids unsuffixed `Hexalith.EventStore`. Spec status remains `in-review`; no parent commit or gitlink unstage.
- Root `HEAD` remains `691eeeacba01a9893793020f61f95e52c35879a2` equal to `origin/main`. The four gitlinks stay staged. `spec-pushall-sync-2026-08-29` remains `status: 'in-progress'`. Production consumer files were not edited. The test class gained six covering facts after the matrix audit.
- Post-review verification (2026-08-30): EventStore `describe` → `v3.100.0`; Builds HEAD `e1026cb61162546571ee0102c525bcf42b9ce7fa`; restore Client/Aspire `3.100.0`; Release slnx `-warnaserror` 0/0; `AppHostSecurityConfigurationTests` Total: 11, Failed: 0. Test file is CRLF.

## Suggested Review Order

**Package identity**

- Catalog default is the only EventStore version Memories may restore.
  [`Directory.Packages.props:8`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L8)

- Scan every EventStore `PackageVersion` row for the shared property.
  [`AppHostSecurityConfigurationTests.cs:78`](../../tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs#L78)

**Restore graph**

- Server assets must restore Client `3.100.0` with no mixed EventStore versions.
  [`AppHostSecurityConfigurationTests.cs:187`](../../tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs#L187)

- AppHost still consumes Aspire through a versionless package reference.
  [`Hexalith.Memories.AppHost.csproj:19`](../../src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj#L19)

- Server still consumes Client through a versionless package reference.
  [`Hexalith.Memories.Server.csproj:52`](../../src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj#L52)

**Isolation**

- Root CPM must stay an import-only bridge with no EventStore pin.
  [`AppHostSecurityConfigurationTests.cs:175`](../../tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs#L175)

- Staged gitlinks stay unpublished; pushall still owns the parent snapshot.
  [`spec-bump-eventstore-3-100-0.md:19`](./spec-bump-eventstore-3-100-0.md#L19)

