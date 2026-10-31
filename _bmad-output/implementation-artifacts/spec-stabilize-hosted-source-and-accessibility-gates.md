---
title: Stabilize hosted source, coverage and health-action gates
status: done
created: 2026-10-31
baseline_commit: 7efa9f97726d4441da2bc64e64fd435cfb698cb6
---

# Stabilize hosted source, coverage and health-action gates

CI run 41183486670 exposes three independent integration defects: the committed EventStore source selection predates RouteOperationContract, the coverage fixture hardcodes coverlet.MTP 10.0.1 after the central pin advanced to 10.1.0, and the Operator Health Matrix disabled action's tooltip replaces its visible name. Release run 41183486672 also fails two AppHost fixture assertions that guess the Builds catalog location and duplicate the superseded EventStore 3.128.3 version. Correct the declared source identity and these narrow contracts without weakening source-schema, exact package identity, coverage, authorization or accessibility gates.

## File Scope

Allowed files for this story:

- `references/Hexalith.EventStore` - select published tested source commit 571784de9d289ec6c66969e926a70c92924cb1c9 through gitlink metadata only
- `tests/tooling/coverage_gate/coverage_contract_test.py` - validate the actual unconditional concrete central MTP collector pin and supported version floor
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` - resolve the owning tracked Builds import and require exact selected-version package identities
- `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` - capture the catalog identity and versions actually evaluated for the tested assembly
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/MemoriesOperatorHealthMatrix.razor` - preserve visible action content as the accessible name and move unavailable explanation to description
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/MemoriesLensComponentsTests.cs` - verify disabled/enabled action naming and unchanged disablement behavior
- `tests/Hexalith.Memories.Web.E2E/specs/specimen-a11y.spec.ts` - prove the real disabled health action's computed name and description while retaining the complete axe gate
- `_bmad-output/implementation-artifacts/spec-stabilize-hosted-source-and-accessibility-gates.md` - record scope, exact selected identities, verification commands and any remaining blockers

Read/verify only:

- `.github/workflows/ci.yml` - retain source-catalog xUnit XML generation, nonempty/no-skip assertions and artifact upload unchanged
- `tests/Directory.Packages.props` - authoritative coverlet.MTP central pin
- `_bmad-output/project-context.md` - owning implementation guidance

Forbidden by default:

- `references/**` contents - no nested initialization or checkout updates; only the exact allowed gitlink metadata changes
- `src/**/*.cs` - no runtime, authorization, transport or persistence changes

## Acceptance and Boundaries

The exact EventStore source contains the operation-schema APIs already qualified by the local sibling checkout. Coverage still enforces its existing threshold, required reports, first-party-only filters and private collector registration; the version assertion follows a concrete unconditional central pin at or above the previously required 10.0.1. Disabled actions remain disabled, retain the unavailable explanation, and expose their complete visible label in the computed name. Existing blocking axe rules remain enabled.

Only metadata is staged for the EventStore gitlink; no nested worktree is initialized or updated. No commits, pushes, releases, package publication or speculative FrontComposer version adoption belong to this handoff. Parent owns integration and publication. Shared producer builds wait for the parent's explicit runtime cleanup signal.

## Implementation

- The EventStore gitlink alone selects published source commit `571784de9d289ec6c66969e926a70c92924cb1c9`, replacing `7b1df62102efb19da03553a890c62eb5ad6aeafe`. The direct sibling used for the source check has exactly that HEAD and contains `RouteOperationContract`. `git ls-files --stage references/Hexalith.EventStore` confirms mode 160000 and the selected SHA; `references/Hexalith.EventStore/.git` remains absent. No nested checkout was initialized or updated.
- The coverage fixture reads the authoritative test-tree XML. It requires exactly one unconditional `Include` pin for coverlet.MTP, rejects duplicate/mutating rows, requires a concrete stable version and preserves the supported minimum 10.0.1. Conditional ancestors include `Choose`, `When` and `Otherwise`, even when those nodes have no Condition attribute themselves. The actual central pin is 10.1.0. Coverage thresholds, filters, private collector registration and required reports remain unchanged.
- The disabled health action keeps its visible content as its accessible name. Its localized unavailable explanation moves from FluentButton `Title` (which supplied a replacement aria-label) to `aria-description`; explicit `role="button"` preserves the previous native role after removing the tooltip. The existing visible explanation, disabled expression and callback guard remain unchanged. bUnit proves both disabled and enabled fixture sets are nonempty, names preserve visible labels, descriptions are conditional, disabled clicks produce no callback, and the enabled action retains its original scoped callback.
- The test project captures the evaluated EventStore Client item's `DefiningProjectFullPath` and `Version`, plus the evaluated central version, as assembly metadata before `GetAssemblyAttributes`. The AppHost fixture uses that actual defining file, checks it against the owning tracked import-path policy, and explicitly refuses a version differing from the tracked catalog default or a Client item differing from the evaluated central version. Import conditions and global property overrides are therefore reflected before validation. Restored Client and every other EventStore family library must retain the exact evaluated identity and `type=package`; the selected version is 3.128.4. The test no longer embeds a stale 3.128.3 literal or chooses a catalog by raw file existence.
- The existing source-catalog XML CI command, nonempty/no-skip checks and artifact upload remain unchanged. The real browser specimen test retains every blocking axe rule and first requires the health route exactly once, then checks the exact computed name/description for the actual disabled graph-traversal action.

## Verification

Hosted baseline failures were source job 124485541358 (`RouteOperationContract` CS0246), coverage job 124485541336 (stale 10.0.1 literal), and accessibility job 124485541315 (serious axe 4.14 `label-content-name-mismatch` on `mem-health-action-button` with visible text `Next action: Use graph traversal`). Release job 124485541104 failed `BuildsCatalog_PinsEventStoreFamilyAt31283` and `ProjectAssets_RestoreEventStorePackagesAtCatalogVersion`.

All commands below run from the owning Memories repository unless otherwise stated. Builds were serialized after the parent's runtime cleanup signal; no solution-level test run was used.

- `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'`: 35 passed. Log `/tmp/memories-hosted-gates-coverage-tests.log`.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj -c Debug --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -warnaserror`: succeeded, zero warnings/errors. Log `/tmp/memories-hosted-gates-source-build.log`.
- `dotnet tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests -xml TestResults/source-catalog.xml`: exactly 3 passed; failed/errors/skipped/not-run all zero. XML SHA256 `4173bda7ec71c365faec4700e9be0b4066a0af3cc104054c1b80ce996f0b78ec`. Generated XML is retained at the existing ignored CI report path. Source restore selected EventStore Contracts/3.128.4 and DomainService/3.128.4 as projects. Log `/tmp/memories-hosted-gates-source-tests.log`.
- `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --configuration Release --disable-build-servers -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true`: succeeded, zero warnings/errors. This nonpackable Web test build retains the existing FrontComposer source-helper exception; it is not evidence that every external edge resolves from NuGet. Log `/tmp/memories-hosted-gates-web-final-build.log`.
- `dotnet tests/Hexalith.Memories.Web.Tests/bin/Release/net10.0/Hexalith.Memories.Web.Tests.dll -class Hexalith.Memories.Web.Tests.Components.Lenses.MemoriesLensComponentsTests`: 9 passed, zero failed/errors/skipped/not-run. Log `/tmp/memories-hosted-gates-web-final-tests.log`.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true`: succeeded, zero warnings/errors. Server assets contain exactly selected EventStore Client, Contracts, DomainService and ServiceDefaults libraries at 3.128.4, all `type=package`. Log `/tmp/memories-hosted-gates-server-build.log`.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests`: 12 passed, zero failed/errors/skipped/not-run. Log `/tmp/memories-hosted-gates-server-tests.log`.
- From `tests/Hexalith.Memories.Web.E2E`, `PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH npm run typecheck`: passed. Log `/tmp/memories-hosted-gates-browser-typecheck.log`.
- `dotnet build tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj --configuration Debug --disable-build-servers -m:1`: succeeded, zero warnings/errors. Log `/tmp/memories-hosted-gates-browser-host-final-build.log`.

- From `tests/Hexalith.Memories.Web.E2E`, `PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH CI=true npm test -- specs/specimen-a11y.spec.ts --retries=0`: 1 Chromium test passed, exercising all 19 real specimen routes with positive scanned-node counts (16–233 per route), zero blocking violations, and exact disabled action computed name/description checks. The unchanged axe 4.14 gate is enabled. Log `/tmp/memories-hosted-gates-browser-final.log`. Existing ignored browser summary `tests/Hexalith.Memories.Web.E2E/test-results/evidence/axe-summary.json` SHA256 `517ea837d212d258ad169fd78f29c6511ba83e3d84f10ebee5716cfc353cbeb2`.
- `git -c core.whitespace=cr-at-eol diff --check`: passed; only expected line-ending conversion notices.
- `python3 tools/check-story-file-scope.py --story-key spec-stabilize-hosted-source-and-accessibility-gates --branch-name main --changed-files-file /tmp/memories-hosted-gates-changed-files.txt`: passed for all 7 changed paths, including the staged gitlink and untracked spec.

No known local verification failures remain. Full hosted verification, commit/push and release remain with the parent; no package pin or package publication belongs to this change.

## Focused Review Corrections

The original verification above precedes the three review corrections; this follow-up reran only the affected coverage fixture, AppHost class and browser specimen test. Existing source-catalog CI generation/upload and the retained three-test no-skip XML remain unchanged.

- Edge1: `_assert_unconditional_mtp_pin` rejects branch-control ancestry as well as Condition attributes. One negative test exercises a conditioned ItemGroup, a When branch and an inactive Otherwise pin whose sibling When matches. The actual stable unconditional central-pin check remains intact. `python3 -m unittest discover -s tests/tooling/coverage_gate -p coverage_contract_test.py`: 9 passed. Log `/tmp/memories-hosted-gates-review-coverage-tests.log`.
- Blind9: test-only assembly metadata records the evaluated defining catalog, central version and Client item version. Two focused negative tests refuse an evaluated version override and a defining file outside the owning import policy. `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true`: zero warnings/errors. `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests`: 14 passed, failed/errors/skipped/not-run all zero. Logs `/tmp/memories-hosted-gates-review-server-build.log` and `/tmp/memories-hosted-gates-review-server-tests.log`.
- Actual metadata controls: `dotnet msbuild tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -nologo -target:GetAssemblyAttributes -property:Configuration=Release -property:UseHexalithProjectReferences=false -property:UseNuGetDeps=true -getItem:AssemblyMetadata` records the allowed sibling Builds catalog with central and Client versions both 3.128.4. Adding `-property:HexalithEventStoreVersion=0.0.0` records both values as 0.0.0; the version-override negative test explicitly refuses that value. Adding `-property:Hexalith1BuildPackageProps=<temporary unowned.props>` with an existing temporary valid catalog makes the first conditional import run and records that actual unowned defining path, rather than the sibling catalog; the provenance negative test explicitly refuses out-of-policy paths. The temporary catalog was removed; normal configuration was rebuilt and all 14 class tests passed. Full captured control output: `/tmp/memories-hosted-gates-metadata-controls.json`.
- Blind10: the browser test requires `operator-health-matrix` to occur exactly once before enumeration. From `tests/Hexalith.Memories.Web.E2E`, `PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH CI=true npm test -- specs/specimen-a11y.spec.ts --retries=0`: 1 Chromium test passed; all 19 real routes scanned nonzero nodes, zero blocking violations, mandatory health route and exact computed name/description passed. Log `/tmp/memories-hosted-gates-review-browser.log`.
- Preserved source XML SHA256 `4173bda7ec71c365faec4700e9be0b4066a0af3cc104054c1b80ce996f0b78ec`: still exactly 3 passed, zero failed/errors/skipped/not-run. No source test rerun was necessary for these test-only corrections.
- Final File Scope passed for all 8 changed paths (including the test project metadata extension); `git -c core.whitespace=cr-at-eol diff --check` passed. Scope log `/tmp/memories-hosted-gates-review-file-scope.log`.

## Cross-Tenant Negative Evidence

Not triggered: dependency identity, coverage assertion and presentation-only action naming do not change tenant routing, authorization or data behavior. Existing action enablement and callback guards remain intact.


## Review Triage Log

| Finding | Verdict | Evidence / route |
|---|---|---|
| Blind9 effective import selection | medium | Raw-template first-existing lookup omits import conditions/actual build overrides; bind tested build selection and explicitly prove or refuse unsupported conditional/override cases. Patch under existing immutable-profile/qualification intent. |
| Blind10 mandatory specimen route | medium | Enumeration can silently omit renamed/removed health route and skip new assertions; require the route exactly once. Patch under existing immutable-profile/qualification intent. |
| Edge1 conditional collector ancestry | medium | Condition-only ancestry misses inactive Choose/Otherwise control; reject conditional branch ancestry, preserving unconditional stable pin checks. Patch under existing immutable-profile/qualification intent. |
