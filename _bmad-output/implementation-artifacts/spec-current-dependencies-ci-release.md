---
title: 'Validate current dependencies and restore release readiness'
type: 'bugfix'
created: '2026-11-13'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6d8031776fbe2a94b173ff19bb5cc9d637ec4eb6'
context: []
---

<frozen-after-approval reason="Intent authorized by the user; human validation explicitly waived">

## Intent

**Problem:** The owner requests synchronizing all pending repository and root submodule changes, current SDK and packages, successful CI/CD, a release without another human checkpoint, and verification of NuGet publication. SDK 11.0.100 and stable NuGet catalog selections are already current, but source Debug CI, publication qualification and nightly sidecar startup have concrete failures.

**Approach:** Apply the available npm updates, preserve compatibility with supported framework consumers, repair the observed failures and align execution pins with the latest declared Builds gitlink. Validate locally and through exact-source GitHub CI before using the established CI release workflow and reading publication results back from NuGet.

## Boundaries & Constraints

**Always:** Preserve user work, existing automated release checks, package inventory, current runtime contracts and immutable historical evidence. Read the Hexalith baseline and owning repository guidance. Work on main; only root-declared submodules may be synchronized. Commitlint validation is mandatory before and after every commit. Push changed submodules before recording their gitlinks in Memories. The coordinator owns all commits, pushes, workflow dispatch and publication verification after implementation review. The user explicitly authorizes bypassing human checkpoints, including existing administrator/self approval in production; keep the technical gates intact.

**Never:** Initialize nested submodules, rewrite published history, fabricate qualification evidence, disable failing checks, publish from this workstation, or remove FrontComposer's supported net10 surface merely to match the current SDK. Do not create a release while current exact-source CI fails.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Legacy framework source dependency | FrontComposer net10 Shell/Testing restored with SDK 11 | Current compatible OpenIdConnect assets resolve without NU1202; net11 Memories retains its current dependencies | Fail restore on incompatible assets |
| Dynamic Redis port | Clock sidecar starts while Redis receives a dynamic endpoint | Clock observes rewritten pubsub endpoint after Redis readiness barrier | Readiness timeout remains blocking |
| Builds pin | Current Builds gitlink differs from old declared execution pin | Current workflow, profile, helpers and assertions select the same latest gitlink | Mismatched source/proof remains rejected |

</frozen-after-approval>

## Code Map

- `global.json`, `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`: SDK 11.0.100 and all 320 stable catalog pins already current; authoritative audit is `/tmp/memories-package-audit.json`.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: owns the shared net10 compatibility views. FrontComposer Shell/Testing currently select unconditional OpenIdConnect 11, causing NU1202. Scope the latest compatible 10.0.15 selection through the existing exact-project compatibility pattern; account for transitive CPM pinning. FrontComposer's graph policy requires its own catalog to remain import-only, so preserve that invariant and project TFMs. Inspect SignalR compatibility before changing additional pins.
- `src/Hexalith.Memories.AppHost/Program.cs`: BeforeResourceStartedEvent `usesRedis` list omits the clock project and sidecar names; generated pubsub initially contains port 6379 until endpoint rewrite. Existing lifecycle/server patterns provide the implementation.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs`: behavioral sidecar test must cover clock Redis readiness and pubsub endpoint. Use existing fixture conventions.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml`: execution pin currently 6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b, latest Builds gitlink is 0e7424fdce6373920fb2019d1ad2a74d8fe5295c. Publication helper/action/shared CI/provenance bytes are identical across those revisions.
- `tools/builds-publication-qualification.json`, `tools/release-containers.json`, `tools/check-release-source.py`, `tools/check-release-destinations.py`, `tools/container-publication-contract.py`: align current execution declarations.
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`, `tests/tooling/ci_alignment/ci_alignment_test.py`, `tests/tooling/release_dispatch/release_dispatch_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`: current pin assertions; preserve historical captures and fetch old revisions needed by them.
- `package.json`, `package-lock.json`, `tests/Hexalith.Memories.Web.E2E/package.json` and lock: github release plugin 12.0.11 and Node typings 26.6.6 are available. E2E uses current Node LTS.
- `docs/dev/release-runbook.md`: add concise current maintenance evidence without changing historic captures.

## Tasks & Acceptance

**Execution:**
- [x] Apply the two npm updates with coherent lockfiles.
- [x] Repair FrontComposer net10 source compatibility in the owning shared Builds catalog; verify restore and representative builds. Do not commit or push. The coordinator will review, commit/push Builds and replace all current execution pins with its resulting canonical SHA before final parent validation.
- [x] Repair clock Redis startup ordering and extend its behavioral regression test; follow the allowed repository Aspire skill and inspect APIs before AppHost edits.
- [x] Align current Builds execution pins and current test expectations; retain old pin in historical fetches where fixtures need it.
- [x] Run focused tooling suites, package Release build and source Debug restore/build; fix any demonstrated regressions. Record exact outcomes and genuine environment blockers here.
- [x] Add current maintenance guidance to the release runbook. Leave final remote run links for the coordinator.

**Acceptance Criteria:**
- Given the supported source graph, when source Debug restore/build runs, then it resolves compatible packages without the observed NU1202 failure.
- Given each root-declared submodule, when synchronization is inspected, then its recorded commit is pushed and no pending work is discarded.
- Given the final main SHA, when CI and publication qualification run, then all required automated checks succeed before release dispatch.
- Given the established release workflow, when publication completes, then every manifest NuGet package is downloadable at the release version and the GitHub release identifies the final source.

## Implementation Notes

Baseline package Release solution build succeeded locally with zero warnings/errors. All nine root-declared submodules are clean and match fetched origin/main. No existing unpushed changes were found. A temporary latest-Builds qualification profile already passed all six checkpoints without tracked edits. Remote acceptance checks are coordinator follow-through after implementation review, explicitly authorized by the user.

Local implementation completed without commits or pushes. The shared Builds catalog now selects OpenIdConnect 10.0.15 for the nine exact FrontComposer Shell-dependent net10 project identities; its import-only owner catalog remains unchanged. MSBuild evaluation verified Shell/Testing/UI select 10.0.15 and unrelated/Memories identities retain the 11.0.0 catalog selection. SignalR 11 assets were inspected (netstandard2.0 fallback for net10); its pins remain unchanged, and representative Shell/Testing builds succeeded. Clock project/sidecar startup now shares the existing Redis rewrite and PING barrier. Its behavioral regression checks the allocated pubsub endpoint and PING at the clock start boundary. The coordinator reviewed, committed and pushed the catalog fix as `bf15785ba1338122619a2f1f1eccc08a39817046` after passing 108 catalog view probes, 17 authority controls and pre/post commitlint. Current execution pins must select that canonical SHA before final parent validation. Historical captures were not modified.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and route | Evidence |
|---------|-------------------|----------|
| Blind: omitted FrontComposer samples | high, patch | Actual Counter.Web restore fails NU1202; the supported sample dependency closure must select compatible OIDC assets too. |
| Blind: new catalog view lacks persistent tests | medium, patch | The existing 108 probes exercise other views. Add effective positive and unrelated-identity probes for this compatibility selection. |
| Blind: startup test lacks a forced delay | maybe-false, defer | The actual start-boundary test passes and production explicitly awaits rewrite and PING. A mutation run under controlled Redis delay would establish whether ordinary scheduling can mask removal of the clock enrollment. |
| Blind: clock startup health is unverified | false | The existing fast/nightly AspireIngestionPipelineFixture waits for the clock sidecar HTTP endpoint at its readiness boundary; the changed test specifically proves component file ordering, rather than being the only runtime readiness check. |
| Blind: resource model omits Redis wait | low, patch | Runtime ordering is already guarded, but declaring the actual Redis dependency is a direct correction that improves Aspire dependency reporting. |
| Blind: Node 26 types conflict with Node 24 minimum | false | Current E2E code uses APIs available in Node 24; CI and the local check use current Node 26. No reachable unsupported API or runtime failure was identified. |
| Blind: freshness audit is only temporary | low, patch | The shared Builds repository already retains a durable generated audit. Refreshing that artifact and recording its generator command provides reproducible evidence for the final catalog. |
| Edge: cancellation can leave a late test Redis connection | low, rejected | A cancellation during the bounded connection attempt could leave a connection until test-process exit. This rare test-only path would require continuation cleanup complexity; it has no production effect. |
| Edge: omitted sample identities | high, patch | Independent Counter.Domain restore reproduced NU1202; same missing compatibility closure as the first finding. |
| Verification: stale Builds package audit hash | high, patch | The owning validator and remote catalog job reject the changed catalog's hash. Regenerate the current audit rather than editing historical evidence. |

## Verification

- `dotnet build Hexalith.Memories.slnx --configuration Release -p:UseHexalithProjectReferences=false -m:1`
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true` and focused Debug builds/catalog tests.
- Existing `tests/tooling/{release_dispatch,ci_alignment,publish_containers,builds_publication_qualification}` unittest suites.
- Existing AppHostComponentFileOrderingTests via the individual integration test assembly; no solution-level test invocation.
- npm semantic-release configuration verification and E2E typecheck.
- Dedicated exact-source qualification workflow and all current main CI jobs, followed by workflow release and NuGet readback.

### Local execution evidence (2026-11-13)

- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true`: passed; the observed NU1202 is resolved.
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:normal`: passed, zero warnings/errors (7.93 seconds); `/tmp/memories-source-build-diagnostic.log`.
- Focused Debug/source builds for `Hexalith.Memories.IntegrationTests`, `Hexalith.Memories.Web.Tests`, `Hexalith.Memories.EventStore.Tests` and `Hexalith.Memories.Cli.Tests`: passed, zero warnings/errors. The Web lane built both FrontComposer Shell and Testing on net10.
- `dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net11.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Fixtures.AppHostComponentFileOrderingTests -xml /tmp/memories-apphost-ordering.xml`: passed 1/1, no skips/errors; the owned AppHost was disposed.
- Individual Debug test assemblies: `MemoriesCatalogTests` passed 3/3 and `CiTestInventoryTests` passed 141/141, no skips/errors. The latter caught the plugin expectation update from 12.0.10 to 12.0.11, which was repaired and retested.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py'`: 17 passed; equivalent suites `ci_alignment`: 15 passed, `publish_containers`: 104 passed, `builds_publication_qualification`: 70 passed, and `line_endings`: 4 passed.
- `npm run verify:semantic-release-config` and E2E `npm run typecheck`: passed. `npm audit signatures`: all 320 registry signatures verified; 58 attestations verified. Both npm updates retain coherent locks and required CRLF bytes.
- Coordinator's baseline package Release solution build passed with zero warnings/errors. It was not repeated locally after generated Release output was reclaimed; final remote CI must repeat it at the reviewed source.
- Initial full source build failed while copying Kreuzberg native assets with `MSB3026: No space left on device`; publication/line-ending fixtures also failed during that exhaustion. After the coordinator reclaimed only task-generated Release output, all those checks passed on retry. One intervening full-build retry exited 1 without diagnostics; the diagnostic retry above passed. No current local build blocker remains, but disk headroom is limited.
- Root and Builds `git diff --check`: passed. No stage, commit, push, submodule initialization, workflow dispatch, publication or historical capture changes were performed by the implementation agent.

Final canonical Builds pin, clean-source remote CI/qualification links, release dispatch, NuGet readback and final acceptance remain coordinator follow-through.

### Review patch validation (2026-11-13)

- Completed the exact supported net10 compatibility closure with `Counter.Domain`, `Counter.Web`, `Counter.Specimens`, `Counter.Specimens.Domain` and `IdeParityCounter`; unrelated and Memories identities continue to select OpenIdConnect 11.0.0.
- Added persisted effective-selection checks to `Tools/test-authoritative-package-catalog.ps1`: all 14 compatible identities and 9 unrelated/lookalike negatives now run alongside the unchanged existing probes. `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` passed 131 view probes and 17 independent authority controls.
- From FrontComposer, `dotnet restore samples/Counter/Counter.Web/Counter.Web.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=false` passed, including all four Counter sample projects. `dotnet restore samples/IdeParityCounter/IdeParityCounter.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=false` also passed. No sample TFMs or owner catalog declarations were changed.
- Added `.WaitFor(redis)` to the clock project's resource graph beside its existing OpenBao waits. Focused source Debug integration-project build passed with zero warnings/errors. The existing `AppHostComponentFileOrderingTests` assembly invocation passed 1/1 with no errors/skips; result `/tmp/memories-review-clock-ordering.xml`.
- Saved the unchanged prior current audit at `/tmp/memories-review-package-audit.before.json`. The owning generator requires catalog declarations to match actual `HEAD`; coordinator-owned declaration commit is required before generation. The implementation agent performs no commit or push and does not edit audit hashes by hand.

Reproducible audit refresh from the Builds root, after the coordinator records the declaration commit:

```bash
cp Tools/package-version-audit.json /tmp/memories-review-package-audit.before.json
pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath /tmp/memories-review-package-audit.before.json -Family dotnet-10 -OutputPath ./Tools/package-version-audit.json
pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1
```

The coordinator recorded declaration/probe commit `2c1e81297a4943c2c1521ce29f96d8420a2a1f53` after pre/post commitlint, without pushing. The generator command above then succeeded against that actual HEAD: 317 packages, incremental mode, `dotnet-10` refreshed and 156 families preserved. The owning validator passed for 317 packages, 157 families and one source. Generator/validator logs are `/tmp/memories-review-package-audit-generation.log` and `/tmp/memories-review-package-audit-validation.log`.

The generated audit has a minimal 20-line metadata diff. Package inventory and all default selected versions are unchanged; preserved-family package rows remain identical to the prior audit. Historical captures/evidence were not edited, and no hash was edited by hand. Root and Builds `git diff --check` passed. Only scoped catalog/audit validation, sample restores and the existing clock ordering regression were run for this review patch; full verification remains with the coordinator. The coordinator must commit the generated audit, push Builds and align current parent pins with the final canonical SHA before final acceptance.

Coordinator reviewed the patch and verified the final Builds commit ae77d5690a9411a2154fa4ed5231edc373942573 on origin/main. All current execution declarations and assertions now select that commit. Local implementation/review is complete; exact-source remote validation and publication are the remaining explicitly authorized operator follow-through. The package audit validator passed 317 packages across 157 families; its catalog declaration revision is 2c1e81297a4943c2c1521ce29f96d8420a2a1f53, an ancestor of the selected commit.

A final synchronization fetch found two new upstream Platform commits. The coordinator fast-forwarded the clean root-declared Platform checkout to da27ecf004300bc3678bed05320bfd3b1803cf3b without initializing nested submodules. The solution contains no direct Platform project references; final remote validation includes this synchronized gitlink. All nine root submodule tips were verified available on their origin/main branches.


### Exact-source CI catalog reader follow-up (2026-11-13)

CI at Memories `520037ad` exposed nine failures in `AppHostSecurityConfigurationTests`: the reader and its default-version/floor-mutation cases called `Single()` across all `HexalithEventStoreVersion` declarations, although Builds legitimately declares conditional SDK4.2 views and its default5.0.0. These were catalog-reader failures; the other server runtime cases had passed.

The reader now selects exactly one default declaration in an unconditional top-level `PropertyGroup` with the exact condition `'$(HexalithEventStoreVersion)' == ''`. All Builds default reads/mutations use that selector. It rejects a missing or duplicate default, a changed property condition and a conditioned property group through four additional negative cases. The owning Memories minimum remains independently validated; effective MSBuild version/client metadata, exact package assets and tracked-import-path rejection assertions remain intact. No arbitrary first row, catalog/submodule edit or package guard relaxation was introduced.

Validation was individual-project only:

- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1`: passed, zero warnings/errors. Direct Debug assembly execution selected `AppHostSecurityConfigurationTests` and reserved only `ProjectAssets_RestoreEventStorePackagesAtCatalogVersion` for its required package lane: 30/30 passed.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1`: passed, zero warnings/errors. The complete security class then passed 31/31, including the package-assets guard, using `-class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests`.
- Full Release/package Server assembly with `-trait- Category=Integration`: 3,195 passed, zero failures/errors, one existing skipped test. The skip is the previously disabled `SubmoduleGuardTests.CheckSubmodulesTarget_FailsBuildWhenSubmoduleGitMarkerIsMissing`, which mutates the shared submodule marker; its skip declaration was not changed.
- Each previously unreached project was individually rebuilt in Release/package mode and its assembly executed with the same non-integration trait filter: AccessTelemetry265, Cli594, Mcp120, EventStore150, Web493 and DomainService13 passed, with zero failures/errors/skips (1,635 additional tests). No full solution build was run.
- Focused XML/log evidence: `/tmp/memories-security-source-tests.xml`, `/tmp/memories-security-package-tests.xml`, `/tmp/memories-server-package-unit-tests.xml`, and `/tmp/memories-remaining-*-tests.xml`; corresponding narrow build logs are retained beside them.

The implementation agent performed no commit, push or submodule change. Final exact-source remote verification, release and publication readback remain coordinator-owned.

Coordinator repeated the complete eight-project CI unit/contract wrapper with coverage in Release/package mode after the catalog reader fix. All executed tests passed, with the existing marker-mutation skip unchanged. The real coverage validator passed 34,655/42,545 lines (81.45%) against the unchanged 76.50% threshold; eight unique reports and all required source entry points were present. Builds CI run 43425958565 completed successfully for ae77d5690a9411a2154fa4ed5231edc373942573. The prior Memories source passed nightly, CodeQL and publication qualification; its newly exposed catalog-reader failure is fixed and requires a fresh exact-source CI run.
