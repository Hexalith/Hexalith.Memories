---
title: 'Refresh the Memories SDK and dependencies and qualify a release'
type: 'chore'
created: '2026-11-11'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9a959b22383c32779da8930dee5a0e5260603b36'
context:
  - AGENTS.md
---

<frozen-after-approval reason="User authorized synchronization, dependency updates, CI repairs and release without intermediate human validation">

## Intent

**Problem:** Memories has pending review documents, outdated root submodule pointers and an older SDK/package cohort. The latest push CI has a failing browser lane, preventing an exact-source release.

**Approach:** Preserve pending work, synchronize the nine root dependencies, select the latest stable SDK and compatible package cohort, repair evidenced CI failures, validate and push, then dispatch the existing guarded release and verify every manifest package on NuGet.

## Boundaries & Constraints

**Always:** Work in each owning repository; preserve existing changes; use Conventional Commits and commitlint before and after commits. Root SDK becomes 11.0.100 while preserving net10.0/C#14 and existing package/image inventories. Keep central package ownership in Builds and its reviewed compatibility exceptions. Update safe remaining compatible package selections through Builds with its revision-bound audit procedures; avoid consumer version overrides. Push any submodule content commits before recording their pointers. Keep current Builds qualification profile and test constant aligned with its gitlink. Preserve historical publisher approval pins and evidence. Release remains CI-only, bound to exact-source successful push CI, production approval controls and NuGet OIDC. User authorization replaces optional conversational checkpoints; required external human approval cannot be forged or bypassed.

**Never:** Initialize or update nested submodules; load skills in references; force-push; disable tests or approval controls; publish from the workstation; retarget the framework merely to consume incompatible packages; modify unrelated open Story 30.4 action items.

</frozen-after-approval>

## Code Map

- `global.json`: older 10.0.403 SDK pin; 11.0.100 is installed and verified against Microsoft release metadata.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: sole version authority; current dd68a5ef cohort supplies Aspire13.6.5, Dapr1.19.1, bunit2.15.2, OpenTelemetry core1.21 and System.CommandLine3. The Builds README/Tools README document catalog, exception and revision-bound audit validation. Retain compatibility pins with documented reasons; remaining candidate patches include Test SDK18.12 and OTel instrumentation1.21.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`: explicit Aspire SDK13.6.4 must match catalog13.6.5; no AppHost API changes intended.
- `tools/builds-publication-qualification.json`, `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`: buildsSha/PINNED_SHA track current gitlink; historical release pins remain81c88b34d09d34260b92d7f0928dbbc1c7446baa.
- `.github/workflows/ci.yml`: exact-source CI, source-debug, tooling, individual unit/integration suites and deployment probes. Dapr toolchain1.18 is stale against shared1.19.1. Browser job130592650549 failed in run43041060983; logs currently HTTP502 and no browser artifacts. Diagnose before changing expected outcomes.
- `tests/Hexalith.Memories.Web.E2E/package.json`: Node>=24; @types/node24.19.2 is latest compatible24.x; preserve that floor. Root npm dependencies are already latest and semantic-release25.0.9 plus alias has a reviewed security exception.
- `tools/release-packages.json`: canonical ten-package inventory; `release.yml` and release runbook define dispatch and protected production flow.

## Tasks & Acceptance

**Execution:**
- [x] Preserve the three pre-existing review document edits in a validated commit and integrate origin/main.
- [x] Synchronize all nine root-declared submodules without nested initialization; no pre-existing submodule commits or dirty files were found.
- [x] `global.json`, AppHost project, qualification profile and test -- align SDK and immutable current dependency selection.
- [x] Builds catalog and audit, only if needed -- update remaining compatible safe versions and validate the owner procedures; report deliberate compatibility exceptions.
- [x] `.github/workflows/ci.yml` and affected code/projects -- repair evidenced SDK/package/browser/toolchain failures, retaining existing test intent and runtime diagnostic dependencies.
- [ ] Run restore/build, relevant individual test projects, release/package fixtures, npm configuration verification and package validation; record exact blockers and commands.
- [ ] Commit/push owner changes with commitlint, verify remote submodule availability, push main and observe successful CI.
- [ ] Dispatch existing release after exact-source CI succeeds; if production waits for human approval, report the run and required action without bypassing it. Verify package publication for any completed release.

**Acceptance Criteria:**
- Given the refreshed graph, when the repository is built and tested, then existing contract checks pass with SDK11.0.100 and net10.0 package compatibility.
- Given synchronized dependencies, when main is pushed, then all recorded submodule commits are remotely available and required CI passes on that exact SHA.
- Given green exact-source CI, when guarded release is dispatched, then existing protection is retained and all ten expected NuGet versions are verified after successful publication, or the concrete external blocker is reported.

## Implementation Notes

User requested no intermediate human validation; the existing required production reviewer is an external release gate. Initial root docs preserved by9a959b22 atop13fdb88a. Root submodules were fast-forwarded only, no nested initialization.

## Spec Change Log

## Review Triage Log

All three configured review lenses completed; none was skipped. Edge Case Hunter returned no findings. Blind Hunter and Verification Gap findings were individually triaged before grouping:

| Finding | Verdict and route | Evidence |
| --- | --- | --- |
| B1: shared CI runtime | medium, patch | The immutable reusable workflow provisions only the pinned SDK; the existing package preparation hook can ensure the required .NET 10 frameworks without changing its approved SHA. |
| B2: README runtime | medium, patch | Ordinary .NET Runtime lacks Microsoft.AspNetCore.App 10, required by server and specimen runtimeconfig files. |
| B3: OS installation guide | medium, patch | The linked guide still prescribes SDK 10.0.400 and cannot satisfy global.json 11.0.100. |
| B4: wizard prerequisites | medium, patch | The wizard prints a repository AppHost command but accepts SDK 10 alone; SDK 11 alone also lacks net10 shared frameworks. The existing process seam supports regression checks. |
| B5: DNS versus HTTP availability | false, reject | The action explicitly promises DNS mirror recovery; its description says unavailable Ubuntu DNS mirrors. HTTP availability repair was never its contract. |
| B6: interruption during mirror installation | low, reject | Existing bounded commands fail closed; a rare partial install/interruption requires new mutation and trap machinery. It does not justify additional branches for this DNS prerequisite correction. |
| B7: disposable real APT qualification | low, reject | Fixtures protect the command, signature refusal and rollback contract; a real network qualification is additional infrastructure work. Required hosted browser CI will exercise the real consumer. |
| B8: retained audit decisions | low, reject | This artifact records current catalog discovery and revision binding, not blanket owner acceptance. The generated retained wording remains accurate; actual Memories acceptance is recorded in local evidence. Editing this spec as the proposed evidence correction is excluded by review routing. |
| B9: direct owner Test SDK consumers | medium, patch verification | Memories uses MTP; the existing Builds VSTest fixture should execute against the upgraded SDK before declaring the shared test cohort qualified. |
| B10: final AppHost startup | medium, patch verification | Baseline startup predates package updates. A final exact AppHost start/describe/stop will verify SDK and dependency activation, while retaining the known unhealthy baseline resource limitations. |
| V1: APT suite discovery | medium, patch | Builds runner discovers Github/*/tests but not test/test_initialize_apt.py; move the existing suite into the recognized helper directory and preserve its assertions. |

B1-B4, B9-B10 and V1 were sent to the original implementation agent for minimal corrections and focused verification. Separately, the long audit validator suite exposed SDK 11 build-server fallback text preceding console JSON; the existing dedicated MSBuild result-file output is used to preserve strict parsing without suppressing errors.

## Verification

Use SDK version, serialized solution build, individually invoked test projects, affected Python tooling suites, npm semantic-release verifier, package validator, GitHub Actions exact-head results and NuGet flat-container endpoints. Preserve command output in task-owned temporary logs and summarize decisive results here.

Implementation handoff: the implementation agent owns configuration/code edits and local validation. The parent owns commits, pushes, remote CI observation and release dispatch after implementation and review. Do not perform remote mutations in the implementation handoff. For submodule catalog edits, leave audit refresh pending the parent-owned catalog commit if its revision binding requires a commit first.


Initial implementation validation (2026-11-11; catalog/provenance superseded by the final graph verification below): SDK `11.0.100` is selected; framework/language remain `net10.0`/C#14. Shared Builds changes advance Test SDK18.12.0, OpenTelemetry instrumentation1.21.0 (retaining existing GrpcNetClient/Redis preview channels at1.21.0-beta.1), and all five Tenants packages7.0.7. The revision-bound audit refresh covers three families and preserves155 others, generated from committed declarations at `d0a4663fb416b3f0b0fe2150eaa56654fd1aad9f`. Initial qualification profile/test bound Builds `a9f443f0bd482359731dcd54711e44fbd23d58fe`; historical publisher approvals remain unchanged. A version-free direct `Aspire.Hosting` reference resolves the restore-observed vulnerable transitive13.6.4 to central13.6.5. Owned CI, nightly, qualification and release jobs install .NET10 runtime/targeting packs alongside SDK11; CI/nightly Dapr CLI/runtime pins now1.19.1. Browser expectations are unchanged.

Completed local commands and results (full logs under `/tmp/memories-sdk-refresh/`):

- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904 --disable-parallel` and `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1`: pass, zero warnings/errors after Hosting alignment.
- Source Debug restore/build with `-p:Configuration=Debug -p:UseHexalithProjectReferences=true` and serialized build: pass, zero warnings/errors. After the final EventStore/Tenants/Platform synchronization, `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1` and the built assembly's `-class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests` invocation pass all3 catalog tests.
- `bash ./tools/test.sh --filter 'Category!=Integration' --configuration Release --no-build --results-directory TestResults/sdk-refresh-unit-contract`: eight individual projects,5415 passed, zero failed, one existing skipped SubmoduleGuard test (5416 total).
- `pwsh -NoProfile -File tools/pack-release.ps1 -Version 0.0.264 -OutputDirectory artifacts/packages/sdk-refresh -PackageOnly`: all10 local packages built and inventory validated; no publication.
- `npm run verify:semantic-release-config`: explicit four-plugin lifecycle and fail-closed cases pass. Browser workspace `npm ci`, `npm run typecheck`, `CI=true npm run test` and `npm run validate:artifacts`: zero npm vulnerabilities,5 Chromium tests pass,9 bounded artifacts validated.
- `python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py'`:12 pass. Strict `builds_publication_qualification` suite:70 pass after root commit `e19d7a704ef10ba01cbaf141776160e6467dff19` recorded the matching Builds gitlink. An earlier precommit attempt correctly rejected HEAD/index gitlink drift; no checks were weakened.
- Builds owner central catalog validator:318 entries pass. Authoritative catalog contracts:68 package identities,4 shared versions and3 canonical families pass. Audit deterministic validator:318 packages/158 families pass. Audit generator fixtures:113 scenarios pass.

At the initial implementation handoff, release fixtures and the audit-validator fixtures were still running. Their completed results are 235 passing release fixtures and 101 passing audit-validator scenarios, recorded below. Parent owns final commits/push, review, exact-source remote CI and protected release dispatch. Existing AppHost baseline startup succeeded but its security resource was unhealthy before edits; the task-owned AppHost was stopped cleanly. No local Docker integration or production deployment claim is made. The pinned shared reusable CI job now provisions missing .NET10 support through its existing package-prepare-script hook; owned jobs install it explicitly.

The browser job now invokes the synchronized, immutable Builds `initialize-apt` helper immediately before Chromium system-dependency installation. The helper restores verified signed Ubuntu mirror availability when the default mirror fails. This prerequisite repair preserves browser checks; the old failed job cause remains unconfirmed because its logs are unavailable.

### External CI/release blocker observed 2026-11-11

Official GitHub status at 20:55 UTC reports Actions `major_outage` (incident `dvy0t3mvh1lg`, investigating since 20:48 UTC). Builds push CI run 43097270955 and commitlint 43097271053 remain queued on a9f443f0. Memories' earlier browser retry 43041060983 ended `startup_failure` with zero jobs; job and run logs returned HTTP 502, so the original browser failure cause is unconfirmed. Repository Actions remain enabled. Do not classify queued CI as passed, create a release without exact-source CI, or bypass the required production reviewer. Existing v3.0.26 was independently verified present for all ten manifest package IDs via NuGet flat-container indexes.


Review remediation local evidence (2026-11-11): prerequisite documentation now consistently requires SDK11.0.100 plus ASP.NET Core10 and its .NET10 runtime dependency. The wizard checks both stable10.0 shared frameworks through the process-runner seam; SDK10-only and absent/other-major framework regressions are covered. The shared immutable CI caller uses `tools/ensure-dotnet10.sh`, which skips downloading when both frameworks exist and otherwise invokes Microsoft's official installer for the10.0 SDK channel in the current DOTNET_ROOT, retaining SDK11 selection. Focused `ci_alignment` discovery passes15tests, including no-op, missing/prerelease framework bootstrap and installer failure controls. The complete CLI project builds without warnings and passes594tests with no skips, including the exact reviewed APT action identity allowlist and wrong-path/wrong-SHA negatives.

The relocated Builds APT fixture suite at `Github/initialize-apt/tests/test_initialize_apt.py` passes8tests through unittest discovery and is included exactly once in the normal `Tools/ci/run_python_tests.py` command inventory. The direct `P0Fixture.NativeTests.VsTest` consumer builds with zero warnings/errors under TestSDK18.12 and `dotnet test --configuration Debug --no-build --no-restore --list-tests` discovers its2tests; live execution still requires a real descriptor-owned endpoint/token handoff and is not claimed.

The final upgraded AppHost was compiled through a focused serialized Debug build (zero warnings/errors), then successfully started using `DOTNET_CLI_USE_MSBUILD_SERVER=0 aspire start --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj --isolated --non-interactive --no-build`, inspected with `aspire describe`, and stopped with `aspire stop`. The first Aspire-managed build exited1 without a compiler diagnostic. The observed resource limitations match the pre-edit baseline: security Unhealthy, EventStore Finished, and4dependent Memories resources Waiting; no full topology-readiness claim is made. All235release tooling fixtures passed: package28, preflight50, dispatch17, NuGet32, container104, line-endings4. Detailed logs remain under `/tmp/memories-sdk-refresh/`.

### Review corrections and final local verification

- Aligned OS setup and CLI error guidance with SDK 11.0.100 plus both stable .NET 10 shared frameworks. The wizard now rejects SDK 10-only installations, missing/prerelease/other-major runtime inventories and runtime inspection failures; SDK parse advisory remains advisory only when the required runtimes are established.
- Retained immutable shared CI/publication execution SHAs; the shared CI package-preparation hook now conditionally installs SDK 10 from Microsoft only when its required frameworks are missing. Bootstrap no-op, installer invocation and failing installer cases pass with the existing CI alignment suite (15 tests).
- Moved the existing eight APT helper tests into the established Builds Github helper discovery path and proved that group appears once. Added only the exact reviewed initialize-apt action path/SHA to the caller action policy, including wrong-path and wrong-SHA refusals.
- Focused final CLI project: 594/594 passed, zero skipped, zero errors. Combined with the unchanged other seven project results, local unit/contract evidence is 5,425 passed plus one existing platform-specific skip (5,426 total).
- Release tooling fixtures: packages 28, preflight 50, dispatch 17, NuGet 32, containers 104, line endings 4: all 235 passed. Browser: 5/5 Chromium checks and nine bounded artifacts passed.
- Direct Builds VSTest consumer builds with zero warnings/errors and discovers both tests. Actual execution requires its live runner-issued endpoint/token descriptor; no live acceptance is claimed.
- Final upgraded AppHost starts, describes resources and stops after a zero-warning serialized Debug build and invocation-scoped MSBuild server disable. Security remains Unhealthy, EventStore Finished and four dependent resources Waiting, as at baseline; Redis/Falkor/OpenBao and telemetry are Running/Healthy. This proves activation, not complete runtime readiness.
- Current deterministic Builds audit validation passes for 318 packages and 158 families after merging the compatible Microsoft 11 catalog and logging-generator selection. The validated Builds graph is e9c40ee27fb6e464f394c64659feb69dae2f39b4. Its complete 158-family audit refresh is bound to the valid declaration ancestor c4e06162bb91ec80a171cdcc2b7f769fd0ad7e8c, superseding the initial d0a4663f audit provenance.

Compatibility holds remain explicit for framework-dependent ASP.NET Core 10 packages, Microsoft.Build 18.9.6, Microsoft.OpenApi 2.x, System.Collections.Immutable 10.0.15, the existing CommunityToolkit Dapr preview, and Node 24 types for the supported Node 24 floor. Compatible Microsoft.Extensions packages, selected ASP.NET libraries, System.Text.Json and cryptography packages now use 11.0.0 while applications retain net10.0/C#14. The previous blanket Microsoft.Extensions 10.x hold is superseded; incompatible or unqualified major selections remain governed by the shared audit.


Audit-validator repair verification: the initial owner fixture run failed9diagnostics because SDK11 emitted an MSBuild-server fallback message before console JSON. `Tools/validate-package-version-audit.ps1` now uses MSBuild's dedicated `-getResultOutputFile` channel for real catalog evaluation, preserves strict JSON parsing and failure console diagnostics, and removes its temporary result file in `finally`. Catalog and consumer declaration bytes did not change, so the existing revision-bound audit remains valid. The exact focused rerun `pwsh -NoProfile -File Tools/test-package-version-audit-validator.ps1` completed successfully with101scenarios; log `/tmp/memories-sdk-refresh/builds-audit-validator-tests-fixed.log`. All implementation-owned validation processes and task AppHosts are complete/stopped.

### Concurrent final catalog refresh

Upstream Builds advanced during publication preparation with the compatible Microsoft 11 package cohort, shared runtime support and logging generator selection repair. Those commits and their revision-bound audit were merged without rewriting history. This supersedes the earlier Microsoft.Extensions 10.x compatibility hold: compatible Extensions and selected Microsoft packages now resolve 11.0.0, while runtime-targeted ASP.NET packages and documented incompatible pins retain their net10-compatible versions. Final Builds e9c40ee27fb6e464f394c64659feb69dae2f39b4 is pushed and remotely verified. Its complete 158-family audit binds c4e06162bb91ec80a171cdcc2b7f769fd0ad7e8c. Catalog and deterministic audit validation pass; logging generator selection passes three cases. The root Release/source builds, unit projects, browser and package inventory are being revalidated for this final graph.

GitHub status changed Actions back to operational at 21:07 UTC with the incident monitoring. Queued exact-source CI is still not success evidence. Release remains gated on the final main revision's completed successful push CI and protected production approval.

### Final graph acceptance and CI lint correction

Final compatible Microsoft 11 cohort verification passed: Release restore/build with zero warnings/errors; eight individually invoked unit/contract projects with 5,424 passed, zero failed and one existing skip (5,425 total); three source catalog tests; five Chromium tests and nine artifacts; ten-package pack/inventory validation; deterministic catalog/audit validation; three logging-generator cases. The unit-lane total supersedes the earlier mixed-scope 5,425-pass calculation: the unfiltered CLI run includes one case excluded by the canonical lane filters.

Source Debug compilation identified two genuine MSB3277 Json 10/11 assembly conflicts in AppHost and IntegrationTests. Version-free System.Text.Json references align both with the central 11 selection. Both consumers now build with MSB3277 treated as an error and zero warnings/errors. The final package-mode solution build also has zero warnings/errors.

Fresh Builds CI 43100973835 exposed thirteen workflow-lint findings. Eleven were pre-existing approved exceptions whose line locations shifted under added SDK support; their existing locations were corrected without adding exceptions or changing frozen workflow bytes. Both changed initialize-dotnet action references are now SHA-pinned to the official reviewed v6.1.0 commit. Authenticated online actionlint/zizmor passes with zero new findings, and fourteen linter fixtures pass. No baseline was regenerated. Final pushed and remotely verified Builds revision: 4e79e00ee38a6c95cad1ebaf5496f81524feb021. Final root qualification profile and test constant follow that exact selection; immutable historical release execution remains unchanged.

Implementation and local review corrections are complete. The remaining external work is final exact-source push CI, protected release dispatch/approval, and NuGet verification of the resulting new version. Status remains in-review rather than claiming release completion prematurely.
