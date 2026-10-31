---
title: Restore published EventStore catalog compatibility
status: done
created: 2026-10-31
baseline_commit: fe8d36446d8081f60806cc15353aa7ca288e9035
---

# Restore published EventStore catalog compatibility

Parties hosted Aspire-tier preparation fails while compiling Memories against published EventStore 3.128.4. The owner catalog references unpublished declaration attributes and operation schemas. Restore package-mode compilation while retaining source-selected development schemas and canonical intent enrollment.

## Boundaries

Use the existing `HEXALITH_DEVELOPMENT_CATALOG_SOURCE` symbol to select unpublished owner attributes, operation schemas, development-host registration and canonical adapters. Keep the six existing command declarations, receiver methods, runtime domain behavior and security unchanged. Package-mode canonical adapter enrollment must explicitly refuse with `NotSupportedException`; it must never return empty success or substitute weaker hashing. Preserve source schema coverage and add independently meaningful package inventory and fail-closed adapter tests. No dependency changes, nested submodule initialization, registry/NuGet publication, commits or pushes are part of this implementation.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` - execute the source catalog assertions after the existing Debug build

- `src/Hexalith.Memories.EventStore/Routing/MemoriesCatalogDeclarations.cs` - select unpublished schema APIs only with EventStore source
- `src/Hexalith.Memories.EventStoreExtensions/MemoriesCatalogAdapters.cs` - preserve canonical source adapters and explicitly reject unavailable package enrollment
- `src/Hexalith.Memories.Server/Program.cs` - select the native host development bootstrap only with EventStore source
- `src/Hexalith.Memories.DomainService/Program.cs` - select development catalog registration only with EventStore source
- `tests/Hexalith.Memories.EventStore.Tests/MemoriesCatalogTests.cs` - retain schema coverage and verify published inventory and unsupported enrollment
- `tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj` - reference the owner adapter library for compatibility coverage
- `_bmad-output/implementation-artifacts/spec-restore-published-eventstore-catalog-compatibility.md` - record this scope and exact verification evidence

## Acceptance

1. Native Memories Server and dedicated DomainService publish in Release/package mode against EventStore 3.128.4 without warning overrides or nested initialization.
2. Package-compatible tests prove exactly six unique command declarations with their actual CLR identities and process receiver routes; canonical adapter enrollment fails explicitly.
3. Source compilation retains all six operation schemas, their internal ingress/digests and the unmodified canonical typed/full-JSON hash implementation.

## Baseline evidence

- Owner `main` was clean at `fe8d36446d8081f60806cc15353aa7ca288e9035`; `git fetch --no-recurse-submodules origin` succeeded with zero divergence. No fast-forward was necessary.
- `timeout 90s aspire start --non-interactive --apphost /home/administrator/projects/hexalith/parties/references/Hexalith.Memories/src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`: exit 2, three CS0246 errors for `RouteCatalogDeclarationSourceAttribute`, `RouteCatalogDeclarationSource` and `RouteOperationContract`. Log: `/tmp/memories-published-catalog-aspire-baseline.log`.
- `aspire describe --apphost /home/administrator/projects/hexalith/parties/references/Hexalith.Memories/src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj --non-interactive`: no AppHost currently running. Log: `/tmp/memories-published-catalog-aspire-describe.log`.

## Verification

Executed from the Memories owner root, with no nested submodule initialization and no warning suppression:

- Baseline native-host command below failed with three catalog CS0246 diagnostics (plus the outer failed-MSBuild task). After the declaration fix, the native host exposed CS0234 for unpublished `DevelopmentRouteCatalogBootstrap`; `Server/Program.cs` was added to File Scope and that bootstrap was source-guarded before the final successful run. Baseline log: `/tmp/memories-published-catalog-server-baseline.log`.
- `dotnet publish src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Release --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -warnaserror -o /tmp/memories-published-catalog-server`: PASS, exit 0; no warnings/errors. Log: `/tmp/memories-published-catalog-server.log`.
- `dotnet publish src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj -c Release --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -warnaserror -o /tmp/memories-published-catalog-domain-service`: PASS, exit 0; no warnings/errors. Log: `/tmp/memories-published-catalog-domain-service.log`.
- Both published host assets select `Hexalith.EventStore.Contracts/3.128.4` and `Hexalith.EventStore.DomainService/3.128.4` with library type `package`.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj -c Release --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -warnaserror`: PASS, zero warnings/errors. Log: `/tmp/memories-published-catalog-package-tests-build.log`.
- `dotnet tests/Hexalith.Memories.EventStore.Tests/bin/Release/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests`: PASS, two passed, zero failed/skipped. Proves the six actual command CLR identities, unique command routes, process receivers and explicit package enrollment refusal. Log: `/tmp/memories-published-catalog-package-tests.log`.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj -c Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -warnaserror`: PASS, zero warnings/errors. Restored EventStore Contracts and DomainService are library type `project`. Log: `/tmp/memories-published-catalog-source-tests-build.log`.
- `dotnet tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests`: PASS, three passed, zero failed/skipped, including the preserved source operation-schema/internal-ingress/digest test and six source adapters. Log: `/tmp/memories-published-catalog-source-tests.log`.
- `dotnet build src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj -c Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -warnaserror`: PASS, zero warnings/errors; retained source catalog registration compiles. Log: `/tmp/memories-published-catalog-domain-source-build.log`.
- `dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -warnaserror`: PASS, zero warnings/errors; retained native source bootstrap compiles. Log: `/tmp/memories-published-catalog-server-source-build.log`.
- `python3 tools/check-story-file-scope.py --story-key spec-restore-published-eventstore-catalog-compatibility --branch-name main --changed-files-file /tmp/memories-published-catalog-changed-files.txt`: PASS, all seven changed files explicitly scoped. The changed-path inventory includes this previously untracked spec.
- The corresponding `check-story-review-readiness.py` invocation exits 0 and reports its documented no-op for a standalone artifact without a phase ledger, File List or evidence table. The corresponding `check-story-slice-scope.py` invocation exits 0 with no governed story file changed. These outcomes establish no completed review or runtime qualification.
- `git -c core.whitespace=cr-at-eol diff --check`: PASS.

This narrow correction does not qualify a running topology, persistent backend behavior or hosted release. The initial Aspire baseline remained stopped; no runtime was started after the package compiler fix. Parent owns validated `Story-Key` commit, push, gitlink adoption and hosted CI/publication evidence. No root Parties files were edited.


## Cross-Tenant Negative Evidence

**Not triggered:** The Program changes guard source-only catalog bootstrap registration. Existing authentication, tenant authorization, endpoint mapping and data routing remain unchanged; source registration is identical when selected.

Review follow-up: normal CI now invokes the built Debug/source MemoriesCatalogTests immediately after its existing source solution build; Release/package tests remain unchanged. The focused source class already passed three tests locally. Exact runtime publish commands and pass counts above are tracked evidence; fresh hosted CI supplies retained artifacts.

The source CI step writes an xUnit XML report, rejects empty selection or skipped/failed assertions, and uploads the report as source-catalog-verification. Local XML verification selected three passing tests. Independent parent review completed with this CI gap corrected.
