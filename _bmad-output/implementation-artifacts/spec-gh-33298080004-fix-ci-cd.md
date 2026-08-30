---
title: 'Run Memories tests through Microsoft.Testing.Platform'
type: 'bugfix'
created: '2026-08-30'
status: 'done'
baseline_commit: '116cbef9acb153550139f79e7404e25581bfe357'
---

## Intent

**Problem:** CI run `33298080004` and Release run `33298080126` failed because
`dotnet test --filter` executed zero tests. `global.json` selects
Microsoft.Testing.Platform under SDK 10.0.400, so VSTest filter, TRX logger,
and XPlat coverage arguments never discover tests.

**Approach:** Keep the wrapper `--filter` CLI and project inventories. Translate
Category expressions into MTP `--filter-trait` / `--filter-not-trait`, emit TRX
with `--report-xunit-trx`, and collect Cobertura with coverlet.MTP `--coverlet`
using the same first-party include/exclude contract as `tests/tests.runsettings`.
Microsoft.Testing.Extensions.CodeCoverage listed Server lines but missed most
hits on GitHub-hosted runners (23% vs Coverlet ~75%), which failed the 76.5%
gate after tests themselves started running.

## File Scope

Allowed files for this story:

- `tools/test.sh` -- bash wrapper MTP translation and coverlet.MTP coverage.
- `tools/test.ps1` -- PowerShell wrapper MTP translation and coverlet.MTP coverage.
- `tools/test-release.ps1` -- release-lane MTP trait exclusion.
- `tests/Directory.Build.props` -- coverlet.MTP package for MTP coverage.
- `tests/Directory.Packages.props` -- test-tree pin for coverlet.MTP 10.0.1.
- `tests/tests.runsettings` -- stop documenting VSTest filter usage.
- `tests/README.md` -- wrapper-first MTP guidance.
- `tests/tooling/coverage_gate/test_runner_contract_test.py` -- fake runner and MTP assertions.
- `tests/tooling/coverage_gate/coverage_contract_test.py` -- coverlet.MTP package contract.
- `_bmad-output/implementation-artifacts/spec-gh-33298080004-fix-ci-cd.md` -- this contract.
