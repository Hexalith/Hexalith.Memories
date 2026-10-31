# Story 30.1 Implementation Notes and Lane Map

User approval: “apply recommended” on 2026-10-31 selects bounded shared CI first, the current ten-package/five-image inventory, and the pinned OIDC-preserving composite for later guarded release. The old guarded draft is now spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md; it is not implementation scope.

## Implementation constraints

- Read repository AGENTS.md, the permitted baseline, build configuration and tests before editing. Root .gitmodules declares references/Hexalith.AI.Tools; its hexalith-llm-instructions.md is the baseline. Never load skills under references. No staging, commits, pushes, dependency changes, nested-submodule initialization, publication or remote configuration changes are authorized.
- Planning reconciliation in epics.md, sprint-status.yaml and epic-30-context.md is already handled by the parent; preserve it. The implementation agent owns the workflow, its focused test updates and procedural docs; report verification to the parent.
- Invoke domain-ci.yml at approved 81c88b34d09d34260b92d7f0928dbbc1c7446baa. Supply solution Hexalith.Memories.slnx, global.json, microsoft-testing-platform, Contracts.Tests only as unit-test-projects, empty integration-test-projects/aspire-test-project, and false run-consumer-validation/run-coverage-gate. Set finite 20-minute build timeout. Leave module-manifest and governed-ci disabled: neither is necessary to pin the caller. The shared workflow uses pinned bootstrap and hardcodes Release/warnings-as-errors. Do not invent a module-build-configuration input or enable obsolete proof-feed preparation.
- Retain all existing test-unit-contract steps, Coverlet 76.5% gate and seven required project reports, package-only validation and 14-day artifacts. Shared MTP tests lack an integration-exclusion input; do not pass the entire local project inventory. Contracts.Tests runs twice intentionally because its canonical coverage report must remain until collector/report equivalence is proved. Do not add a collector or migrate coverage merely to remove this small overlap.
- Existing build job combines Release and Debug/source. Delegate its Release portion to shared-ci; retain Debug/source in source-debug with source-selected catalog tests, >=3 executed and no failed/errors/skipped/not-run, and source-catalog-verification upload. The existing stable build check becomes a small always-run aggregate needing shared-ci and source-debug, returning failure for either failure/cancelled/skipped/unknown result. Pass result values via env, not untrusted shell interpolation. Keep all other local jobs and nightly behavior.
- Four remaining local restore invocations must retain their declared audit exceptions. The shared Release/package graph uses Directory.Build.props audit policy, unlike retained Debug source dependencies. Validate the actual shared restore/build; do not suppress audits or loosen warnings for convenience.

## Lane migration and rollback

| Old owner | New owner | Trigger | Check | Evidence | Rollback |
| --- | --- | --- | --- | --- | --- |
| local build Release | pinned Builds domain-ci | main PR/push | shared-ci / build-and-test; aggregated by build | Release -warnaserror; Contracts TRX blocking-test-results (7 days) | Restore prior local Release steps from baseline |
| local build Debug/source | local source-debug | main PR/push | source-debug; aggregated by build | Debug source catalog XML, source-catalog-verification | Restore original combined build job |
| local build required status | local aggregate | always after both dependencies | build (unchanged) | All dependency-result combinations exercised | Restore prior combined build job |
| local test-unit-contract | same | main PR/push | test-unit-contract | Seven-project Coverlet TRX/Cobertura, 76.5%; pack validation; 14-day test-unit-contract-results | No lane migration |
| local story-file-scope | same | existing PR condition | story-file-scope | Scope/isolation/slice/readiness checks | No lane migration |
| local web E2E | same | main PR/push | web-e2e-specimen | Existing browser checks/artifacts | No lane migration |
| local fast integration | same | main PR/push | integration-fast | Existing tests, coverage verifier and failure artifacts | No lane migration |
| local deployment | same | main PR/push | production-deployment-verification | Existing archive/rollout/evidence checks | No lane migration |
| local SQL contract | same | main PR/push | access-telemetry-sql-contract | Existing PostgreSQL contract fixtures | No lane migration |
| commitlint | same | PR opened/sync/reopened/edited; main push | existing commitlint | Explicit PR title, existing Conventional Commit fixtures | No lane migration |
| nightly | same | existing schedule/manual | existing slow integration/benchmark jobs | Existing TRX, quality evidence | No lane migration |

## Focused testing requirements

Use Python stdlib unittest fixtures for real caller wiring and mutations if appropriate. Add a ci_alignment suite and wire it into test-unit-contract. Validate approved SHA, solution/platform and Contracts-only list; missing or bypassed required lanes; Debug catalog evidence; retained coverage/package gates; aggregate all-success vs failure/cancelled/skipped; no publication. Reuse existing YAML/test helpers rather than build a generic policy framework.

CiTestInventoryTests currently counts five literal restores; update it to semantic ownership (four local + delegated Release restore), not a relaxed arbitrary count. Its external-action allowlist currently permits Builds @main only; authorize exactly domain-ci.yml@the reviewed SHA with positive/negative cases, preserving other policy. Do not allow every Builds SHA or silently weaken other actions. The scope extraction in tests/tooling/story_review_readiness/readiness_entrypoints_test.py splits on the literal next build job; replace it with a true next-job boundary and cover a later job's unrelated condition.

Focused commands: actionlint .github/workflows/ci.yml; Python unittest discovery -p '*_test.py' individually for ci_alignment, coverage_gate, story_review_readiness, story_scope, story_slice_scope and commitlint. Build the CLI test project, then run its assembly with -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests. Also exercise the shared Release restore/build and Contracts MTP command when environment prerequisites permit; report exact blockers and focused fallback evidence without weakening gates. Run per-project tests, never solution-level dotnet test.

## Runtime baseline captured by parent

Before implementation, aspire start --non-interactive --isolated --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj succeeded. aspire describe reported Redis/FalkorDB/OpenBao and telemetry resources healthy, security unhealthy, EventStore finished, and Memories/MCP waiting. The exact owned AppHost was then stopped successfully with aspire stop --apphost ... --non-interactive to release build outputs; the unrelated Tenants AppHost was untouched. This is a mixed existing runtime baseline, not proof of full topology health. Do not repeat startup for this CI-only change.

## Verification results (2026-10-31)

- actionlint .github/workflows/ci.yml: passed. git diff --check: passed.
- PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/<suite> -p '*_test.py': ci_alignment 7, coverage_gate 36, story_review_readiness 105, story_scope 71, story_slice_scope 20, commitlint 13; every named discovery ran and passed, 252 unittest cases total. The six commands were run independently.
- dotnet restore Hexalith.Memories.slnx -p:Configuration=Release: passed unchanged from the shared workflow. Log /tmp/memories-story30-1-shared-restore.log.
- dotnet build Hexalith.Memories.slnx --no-restore --configuration Release -warnaserror: passed, 0 warnings/errors. Log /tmp/memories-story30-1-shared-build.log.
- dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-restore: passed. Log /tmp/memories-story30-1-cli-build.log.
- dotnet tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests -xml /tmp/memories-story30-1-cli-inventory.xml: 136 cases, zero failures/errors/skips/not-run.
- dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --no-build --configuration Release --results-directory /tmp/memories-story30-1-contracts-results --report-xunit-trx --report-xunit-trx-filename Hexalith.Memories.Contracts.Tests.trx: 596 passed, zero failed/skipped.
- Scope and slice guards pass for the explicit complete fifteen-path changed-file list; no default-branch no-op is used as evidence.

Matrix mapping: real caller preservation test covers PR/push wiring; aggregate execution covers all-success and unsuccessful dependency rows (49 combinations); config and bypass mutations cover wrong SHA/platform/projects and missing gates (126 gate mutations); the source command executes 11 report/process scenarios, preserving any produced XML and failing absent reports. Node/js-yaml reads the actual YAML. Live GitHub execution and live remote settings were not evaluated.

## Discovery arithmetic evidence

The ledger baseline was reproduced from commit 00919bdd6fe97284da5f4f1657da4a5b336867cc after implementation, transparently recording revision-based observations rather than inventing earlier execution. Python unittest.TestLoader().discover(path, pattern='*_test.py').countTestCases() observes the six current suite counts above. The baseline readiness_entrypoints_test.py source was loaded from git show with its unchanged helper imports and unittest.TestLoader().loadTestsFromModule: 6 -> 7 cases; the full readiness discovery is therefore 104 -> 105. ci_alignment is absent from the baseline tree: its named transition is 0 -> 7. Other four suites are byte-unchanged, yielding 244 -> 252 Python cases.

CLI baseline source was obtained by git show 00919bdd6fe97284da5f4f1657da4a5b336867cc:tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs and built through the unchanged test project with an isolated temporary output directory and a temporary BeforeTargets=CoreCompile replacement of that one Compile item. Exact build: dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-restore -p:CustomAfterMicrosoftCommonTargets=/tmp/memories-30-1-baseline-doojspq3/baseline.targets -p:OutputPath=/tmp/memories-30-1-baseline-doojspq3/bin/. Both baseline/current assemblies were discovered with dotnet <assembly> -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests -list full/json: 68 deferred-theory method records each. Both executed -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.Workflows_ExternalActionReferenceSecurityMatrix -result-xml <report>: baseline 15 cases, current 19 cases, all passed. Reports and snapshots are under /tmp/memories-30-1-baseline-doojspq3. Four added InlineData cases explain the runtime delta without pretending deferred discovery methods are expanded cases. No repository source was temporarily overwritten.

## Final review-patch verification

All three independent review layers completed. Ten blind findings were individually triaged and fixed; other layers found no gaps. The parent reran all six focused Python suites after patches: 10 + 36 + 105 + 71 + 20 + 13 = 255 unittest cases, all passed. Exact output: /tmp/memories-story30-1-final-python.log. The same CLI inventory class reran with -result-xml /tmp/memories-story30-1-final-cli.xml: 136 passed, zero failures/errors/skips/not-run. Contracts' unchanged shared MTP selection remains 596 passed from its actual run.

Actual source lane now also executed: dotnet restore Hexalith.Memories.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904; dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore. Both passed; build reports zero warnings/errors. Logs: /tmp/memories-story30-1-source-restore.log and /tmp/memories-story30-1-source-build.log. The parent parsed ci.yml with the pinned js-yaml parser and ran its exact Verify source-selected owner catalog contracts command through bash -e: three real tests passed, and the stricter XML integrity check passed. Output: /tmp/memories-story30-1-source-catalog.log; report: TestResults/source-catalog.xml. The review patch adds three Python unittest methods (7 -> 10 ci_alignment cases), no C# tests; cumulative Python delta is +11 from 244 -> 255. Source catalog tests already existed; this is new execution evidence, not three added tests.

Local verification totals 990 cases across the four explicitly named scopes: Python 255, CLI 136, Contracts 596 and source catalog three. The original source fixture's positive report was replaced with valid complete XML, and missing/contradictory summaries or individual records now fail. Current artifact assertions verify action identity/configuration and report preservation; they claim neither a live GitHub download nor a fresh seven-project coverage collection.
