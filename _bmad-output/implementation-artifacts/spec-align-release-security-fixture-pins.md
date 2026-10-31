---
title: Align release security fixtures with reviewed tooling pins
status: done
created: 2026-10-31
baseline_commit: 7489c91de4c24f55e86b6018b59a3945cd253033
---

# Align release security fixtures with reviewed tooling pins

Release run 41186510677, job 124494418621, fails only two CLI CI-inventory fixtures: stale exact commitlint/GitHub-plugin versions and the reviewed source-report upload action SHA being rejected by a major-tag-only policy. Align those assertions with the existing manifest/lock and authorize only the reviewed action identity.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - align exact existing dependency expectations and narrowly allow the reviewed upload-artifact SHA, with policy negative controls
- `_bmad-output/implementation-artifacts/spec-align-release-security-fixture-pins.md` - record authorized scope and focused validation

Read/verify only:

- `package.json` and `package-lock.json` - existing exact qualified release-tooling graph
- `.github/workflows/ci.yml` - existing source XML upload uses actions/upload-artifact SHA cf430e030ddbb5b0abf93d22962f4752f3646cd9 (v7.0.2)
- `tools/verify-semantic-release-config.mjs` - existing offline verifier and ten configuration negative controls

## Acceptance and Boundaries

Keep semantic-release exactly 25.0.8 and cosmiconfig exactly 9.0.2. Preserve the npm-plugin alias, excluded vulnerable package families, exact lock versions, undici resolved URL/integrity and release configuration security contracts. Change neither packages nor workflows. Existing major-tag and Hexalith.Builds main policies remain; the sole new SHA exception is the exact full reviewed upload-artifact reference, with near-miss and wrong-action SHA cases rejected.

Only focused tests covering the edited fixture and relevant security controls run. No solution-level verification, commits, pushes or publication occur in this handoff.

## Verification

- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --disable-build-servers -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true`: passed, zero warnings/errors. Log `/tmp/memories-release-fixture-build.log`.
- `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.ReleaseTooling_ManifestAndLockPinSecureSemanticReleaseGraph -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.Workflows_ExternalActions_UseAllowedRefs`: exactly 2 passed, failed/errors/skipped/not-run all zero. Log `/tmp/memories-release-fixture-reported-tests.log`.
- `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.Workflows_ExternalActionReferenceSecurityMatrix`: 15 cases passed (4 accepted, 11 rejected), failed/errors/skipped/not-run all zero. Negative cases include a one-character SHA mutation, an arbitrary 40-character SHA, the reviewed SHA on another action or owner, branch refs, a patch tag, missing/empty refs and unsupported Builds refs. Log `/tmp/memories-release-fixture-action-matrix.log`.
- `PATH=/home/administrator/.nvm/versions/node/v24.22.1/bin:$PATH npm run verify:semantic-release-config`: passed actual installed semantic-release 25.0.8/four-plugin lifecycle verification and all ten existing fail-closed configuration negative controls. Log `/tmp/memories-release-fixture-offline-verifier.log`.
- Exact manifest and lock expectations now match commitlint CLI/config 21.2.4 and GitHub plugin 12.0.10. All remaining lock identities, security overrides, alias/exclusion checks, undici resolved URL/integrity and fixed semantic-release/cosmiconfig pins remain unchanged.
- `python3 tools/check-story-file-scope.py --story-key spec-align-release-security-fixture-pins --branch-name main --changed-files-file /tmp/memories-release-fixture-changed-files.txt`: passed for exactly the fixture and this spec. Log `/tmp/memories-release-fixture-file-scope.log`. `git -c core.whitespace=cr-at-eol diff --check`: passed.

No package, lockfile, workflow, source XML report, CI source no-skip assertion or submodule selection changed. No commits or pushes.

## Cross-Tenant Negative Evidence

Not triggered: test-fixture version/ref alignment does not change runtime behavior, tenant routing or authorization.
