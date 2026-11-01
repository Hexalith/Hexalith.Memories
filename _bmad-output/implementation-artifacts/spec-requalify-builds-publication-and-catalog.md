---
title: Requalify the current Builds publisher and catalog
status: in-progress
baseline_commit: b93b32cf77f3d76e51d3221550eff99f4d1e93cb
---

The authorized workspace upgrade exposed two existing CI failures: the empty SDK selection expects an obsolete version, and the publisher qualification profile no longer matches the selected Builds checkout. Preserve the minimum and explicit newer/source selections. Bind the empty selection to the actual tracked catalog through the existing provenance guard. Select the released Builds source, rerun the actual offline publisher qualification and upstream controls, and retain the historical approved evidence unchanged.

Capture new results in CI from an exact clean commit, outside its working tree. This uses only the CI repository's root Builds dependency. The local umbrella workspace must not initialize nested submodules. All six qualification checkpoints must pass before their fresh evidence is adopted. Human validation is waived by the user's release instruction; do not invent a new human approval or change the old one. Existing automated checks remain mandatory.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` - align the selected shared CI source
- `.github/workflows/qualify-builds-publication.yml` - retain actual qualification outputs
- `references/Hexalith.Builds` - select the released shared dependency
- `tools/builds-publication-qualification.json` - declare the source actually qualified
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py` - preserve exact profile and evidence binding
- `tests/tooling/ci_alignment/ci_alignment_test.py` - preserve exact shared workflow binding
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` - bind the default SDK expectation to the tracked catalog
- `_bmad-output/implementation-artifacts/spec-requalify-builds-publication-and-catalog.md` - record scope and actual verification
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/**` - new genuine qualification results, once generated

## Verification

Run focused catalog cases and shared CI alignment controls. Execute the exact pinned upstream publisher fixtures and all six Memories checkpoints in CI. Verify the downloaded artifact and committed bytes before selecting the new evidence directory. The existing approved `30-2-qualification` directory remains historical.

## File List

- `.github/workflows/ci.yml`
- `.github/workflows/qualify-builds-publication.yml`
- `references/Hexalith.Builds`
- `tools/builds-publication-qualification.json`
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `_bmad-output/implementation-artifacts/spec-requalify-builds-publication-and-catalog.md`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-11-01 | create-story | Adopt the authorized upgrade scope at b93b32c; preserve historical approvals. | Command: python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py'; blocker: local nested Builds initialization is prohibited; owner: workspace upgrade; consequence: full current qualification is open; reopen trigger: exact-source CI artifact. No count claimed. | matched 8/8 planned and implemented paths against b93b32c; git diff --name-only plus untracked workflow/spec paths. |
| 2026-11-01 | dev-story | Align the released dependency/profile and add a clean-source CI capture; remove only the obsolete empty-selection test literal. | Command: focused catalog and CI qualification checks; blocker: execution pending; owner: workspace upgrade; consequence: no qualification or completion claimed; reopen trigger: focused and exact-source CI results. | matched 8/8 paths against b93b32c; git diff --name-only plus untracked workflow/spec paths. |

| 2026-11-01 | dev-story | Focused checks pass; repair the wrong-SHA negative fixture so it always changes the selected digest. | Python qualification discovery 62 -> 62 methods, phase delta +0, cumulative +0; shared CI discovery 10 -> 10 methods, phase delta +0, cumulative +0. Commands: unittest loader in /tmp/works-memories-requalification-discovery.json; python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' passes10. ProfileTests passes2. Actual linked catalog test source executes9 cases,0skipped in a private Debug harness; dotnet tests/Hexalith.Memories.Server.Tests/bin/FocusedCatalog/net10.0/FocusedCatalog.dll -method '*RootDirectoryPackages_RequiresDeliveredCatalogSdkWithoutLoweringNewerSelectionsAsync' -method '*BuildsCatalog_Refuses*'. No full Server discovery delta claimed. Both workflows pass actionlint; all10 supplied shared inputs exist at f0. Exact-source qualification remains open. | matched 8/8 cumulative paths against b93b32c; git diff --name-only HEAD plus the two new workflow/spec paths. |

Focused Debug build has zero warnings/errors. Harness source and logs are in /tmp/works-memories-focused-catalog-20261101; it compiles the actual modified test file and imports this repository's real catalog. It is narrow test evidence, not the complete Server suite or publisher qualification. The first harness configuration lacked NuGet central-version initialization and failed NU1015; using the standard Directory.Packages.props discovery corrected that harness configuration. The first shared-CI run exposed the no-op wrong-SHA fixture; its repaired run passes10/10. Historical failed logs remain.
