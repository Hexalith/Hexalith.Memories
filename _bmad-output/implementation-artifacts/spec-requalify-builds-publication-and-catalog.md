---
title: Requalify the current Builds publisher and catalog
status: done
baseline_commit: b93b32cf77f3d76e51d3221550eff99f4d1e93cb
review_loop_iteration: 1
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
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - retain exact upgraded tooling and reviewed workflow expectations
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` - bind the default SDK expectation to the tracked catalog
- `_bmad-output/implementation-artifacts/spec-requalify-builds-publication-and-catalog.md` - record scope and actual verification
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/**` - new genuine qualification results, once generated
- `tests/README.md` - current shared CI ownership pin
- `docs/dev/release-runbook.md` - current shared CI ownership pin
- `docs/dev/branch-protection.md` - current shared CI ownership pin

## Verification

Run focused catalog cases and shared CI alignment controls. Execute the exact pinned upstream publisher fixtures and all six Memories checkpoints in CI. Verify the downloaded artifact and committed bytes before selecting the new evidence directory. The existing approved `30-2-qualification` directory remains historical.

## File List

- `.github/workflows/ci.yml`
- `.github/workflows/qualify-builds-publication.yml`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/approval.md`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/artifact-receipt.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/capture.zip`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/complete-set-dry-run.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/evidence-upload.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/partial-publication.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/phase-verification.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/recovery.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/approval.md`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/artifact-receipt.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/capture.zip`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/complete-set-dry-run.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/evidence-upload.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/partial-publication.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/phase-verification.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/recovery.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/run-41304156187-a4788c89/set-drift.json`
- `_bmad-output/implementation-artifacts/30-2-qualification-20261101/set-drift.json`
- `_bmad-output/implementation-artifacts/spec-requalify-builds-publication-and-catalog.md`
- `docs/dev/branch-protection.md`
- `docs/dev/release-runbook.md`
- `references/Hexalith.Builds`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `tests/README.md`
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `tools/builds-publication-qualification.json`


## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-11-01 | create-story | Adopt the authorized upgrade scope at b93b32c; preserve historical approvals. | Command: python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py'; blocker: local nested Builds initialization is prohibited; owner: workspace upgrade; consequence: full current qualification is open; reopen trigger: exact-source CI artifact. No count claimed. | matched 8/8 planned and implemented paths against b93b32c; git diff --name-only plus untracked workflow/spec paths. |
| 2026-11-01 | dev-story | Align the released dependency/profile and add a clean-source CI capture; remove only the obsolete empty-selection test literal. | Command: focused catalog and CI qualification checks; blocker: execution pending; owner: workspace upgrade; consequence: no qualification or completion claimed; reopen trigger: focused and exact-source CI results. | matched 8/8 paths against b93b32c; git diff --name-only plus untracked workflow/spec paths. |
| 2026-11-01 | code-review | Final summary of the two completed independent three-layer reviews and accepted repairs recorded below. Preserved rejected findings, existing approvals and immutable captures; human validation waived by the user. | Publisher methods 62 -> 70, cumulative +8, final repair phase +1; CI alignment 10 -> 12, cumulative +2, final phase +0. Current actual execution: publisher70/70, alignment12/12 and linked catalog13/13, zero failures/skips; exact commands/logs in Parent Final Verification. Catalog cumulative +1 theory/+4 cases. Prior real upstream166/166 remains separate support. | matched 30/30 cumulative paths against b93b32cf; git diff --name-only b93b32cf plus staged/new evidence compared with File List; no exclusions. |

| 2026-11-01 | dev-story | Focused checks pass; repair the wrong-SHA negative fixture so it always changes the selected digest. | Python qualification discovery 62 -> 62 methods, phase delta +0, cumulative +0; shared CI discovery 10 -> 10 methods, phase delta +0, cumulative +0. Commands: unittest loader in /tmp/works-memories-requalification-discovery.json; python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' passes10. ProfileTests passes2. Actual linked catalog test source executes9 cases,0skipped in a private Debug harness; dotnet tests/Hexalith.Memories.Server.Tests/bin/FocusedCatalog/net10.0/FocusedCatalog.dll -method '*RootDirectoryPackages_RequiresDeliveredCatalogSdkWithoutLoweringNewerSelectionsAsync' -method '*BuildsCatalog_Refuses*'. No full Server discovery delta claimed. Both workflows pass actionlint; all10 supplied shared inputs exist at f0. Exact-source qualification remains open. | matched 8/8 cumulative paths against b93b32c; git diff --name-only HEAD plus the two new workflow/spec paths. |

Focused Debug build has zero warnings/errors. Harness source and logs are in /tmp/works-memories-focused-catalog-20261101; it compiles the actual modified test file and imports this repository's real catalog. It is narrow test evidence, not the complete Server suite or publisher qualification. The first harness configuration lacked NuGet central-version initialization and failed NU1015; using the standard Directory.Packages.props discovery corrected that harness configuration. The first shared-CI run exposed the no-op wrong-SHA fixture; its repaired run passes10/10. Historical failed logs remain.

| 2026-11-01 | dev-story | Adopt genuine exact-source CI evidence from run41297442389; retain all original approved files unchanged and retain the raw artifact ZIP. | Six checkpoints pass130 assertions; upstream publisher suite passes166 tests. Counts are separate scopes, with no historical discovery delta claimed. Qualification Python discovery remains62 methods, delta +0. Command: CI Qualify Builds publication at73510013, job124821149717. The raw artifact digest and each member digest match GitHub and the receipt. Full62-method suite and complete current CI remain open. | matched 17/17 cumulative paths against b93b32c; git diff --name-only HEAD plus nine new evidence files. |

CI artifact13079897620 has SHA-256 db2080f1cb9d3ef834210d685a984b8969808710cdea89ca10a065fd8eb7f081. Every checkpoint binds clean Memories73510013 and Builds f0, the actual unchanged harness, current profile and exact executed/read helper bytes. The committed proof selects that genuine capture. This is offline publisher contract qualification, not live container or NuGet publication. The generated approval remains exactly pending; the user's instruction to bypass human validation is not represented as a new named owner signature.

| 2026-11-01 | dev-story | Install global.json SDK explicitly before upstream qualification. Run41297768102 encountered a runner with10.0.402 and failed18 provenance test cases before the harness; the prior genuine735 capture passed166 tests. | Command: actual upstream fixtures in run41297768102; result166 methods with18 environment failures. No test discovery changed. The workflow now installs the pinned SDK and records dotnet --version; fresh CI remains required. | matched17/17 cumulative paths against b93b32c; workflow/spec repair only. |

| 2026-11-01 | dev-story | Preserve generated pending approvals by parsing blank table cells correctly. The old regex demanded two spaces for an empty cell and rejected the unmodified generated table; no approval bytes or decisions changed. | Full CI qualification discovery62,61 passed/1 failed before repair. The actual failing method now passes. Exact old approved rows parse2 before/2 after; generated pending rows parse0 before/2 after. Report /tmp/works-memories-pending-approval-regex-proof.json. Method count delta +0; current full CI remains required. | matched17/17 cumulative paths against b93b32c; existing test/spec repair only. |

| 2026-11-01 | dev-story | CI run 41299252427 passes integration-fast, shared build, source-debug, browser, SQL, deployment and all 62 publisher controls, but exposes three stale CLI guard methods. Align exact npm pins (semantic-release 25.0.9, fast-uri 3.1.9, undici 7.30.1) and the qualified f0 workflow; retain wrong-digest and wrong-action refusal cases. | Actual whole CiTestInventoryTests source compiled in a private Debug harness: build 0 warnings/errors; three repaired methods plus existing action matrix execute 22 cases, 0 failed, 0 skipped. Source attributes baseline/current 56 Facts and 12 Theories; phase and cumulative method delta +0. No full CLI assembly discovery or suite claim. Registry independently confirms the exact undici 7.30.1 integrity retained by the assertion. Commands: dotnet build /tmp/works-memories-focused-cli-20261101/FocusedCli.csproj --configuration Debug; focused DLL with four -method selections in /tmp/works-memories-focused-cli-tests.log. | matched 18/18 cumulative paths against b93b32c; adds only the actual failing CLI test source. |


Before review repair, full CI41304156426 at a4788c89 succeeded: unit/contract, integration-fast, source-debug, shared build, browser specimen, SQL contracts and deployment verification. Qualification41304156187, CodeQL, dependency-review and commitlint also succeeded. At that initial review handoff, this spec was the only working-tree overlay; the repair implementation and its affected verification are recorded below. No Main merge or release has occurred in this story.


## Review Triage Log

All three fresh layers returned final before triage. Each finding has its own verdict; only the duplicate trigger omission is grouped.

| Finding | Verdict | Category | Evidence |
|---|---|---|---|
| blind-1 | medium | bad_spec | CommittedEvidenceTests accepts legitimate failed checkpoint documents and dirty-source bindings after only consistency/schema checks. Reviewer reproduced all four methods passing with fail/dirty values. Adoption must explicitly require pass statuses, clean source, passing checks and no failure reasons; a failed qualification artifact is uploaded naturally on the always path. |
| blind-2 | medium | bad_spec | The selected capture.zip and artifact-receipt.json are genuine and independently verified, but current four committed-evidence methods also pass when both are absent. Protect actual selected ZIP size/digest/member inventory/bytes and receipt source/helper bindings automatically; a missing/mismatched retained carrier must not count as adopted evidence. |
| blind-3 | medium | bad_spec | The workflow checks clean source only before upstream fixtures. Real fixture/qualification processes execute afterward, and harness source dirtiness is recorded rather than rejected. Preserve before/after clean-tree assertions and reject dirty emitted bindings; do not clear or reset source to obtain cleanliness. |
| blind-4 | medium | patch | Selected genuine735 evidence predates explicit SDK installation/version capture. Fresh successful qualification41304156187 at a478 exists with the repaired workflow. Adopt that genuine downloaded capture and provenance under a new directory while preserving735 and historical approved bytes; never relabel the older capture. |
| blind-5 | low | patch | Dedicated qualification path selectors omit actual SDK/build/recovery inputs. Add global.json, Directory.Build.props/targets, Directory.Packages.props and recover-partial-release.yml to both selectors; preserve existing triggers and all gates. |
| blind-6 | low | reject | global.json explicitly permits latestFeature roll-forward. Recording the actual selected dotnet version with that policy is valid; no actual wrong selected version or unsupported execution was shown. Do not silently replace the repository policy with an exact-version failure guard. |
| blind-7 | low | reject | The59 scratch entries belong to explicitly offline publisher contract fixtures, not claimed live OCI/publication proof. The genuine producer f0 carrier55 files and public tools were verified separately. Persisting all mock scratch payloads adds a new archive lifecycle without demonstrating an ordinary incorrect qualification; retain the offline-only scope and existing observed checkpoint outputs. |
| blind-8 | low | patch | Current story scope checks and parent hash comparisons preserve the old approved seven files today, but switching the selected directory removed their former selected-evidence coverage. A direct hash check against the retained historical mapping preserves the explicit immutable-history requirement without rewriting any old files. |
| blind-9 | low | patch | The active catalog is now3.130.1 and existing explicit3.130.0 no longer exercises above-default selection. Add a derived newer-than-catalog case plus empty source-mode case; preserve existing minimum/explicit cases and production behavior. |
| blind-10 | low | patch | All three cited descriptions are active lane ownership/current-pin prose, not historical approvals. Update only those current descriptions to f0; preserve the explicitly historical settings/approval records. |
| edge-1 | low | patch | Independently identified the same missing global.json workflow-trigger handling. Retain this separate verdict before grouping with blind-5. |

## Intent

Strengthen adoption of genuine successful exact-source offline publisher evidence under the existing authorized upgrade. Preserve the actual f0 producer binding, all CI/integration/coverage gates, original minimum/explicit SDK selections, permitted latestFeature policy, original approved evidence and735 capture bytes. Human validation remains waived, no human approval is fabricated and no offline result becomes live publication evidence.

## Repair Code Map and Scope

The implementation repository is /tmp/works-npm-adoption-20261101/Hexalith.Memories at committeda4788c89, with only this review spec overlay. Three current omissions are in adopted-evidence status validation, ZIP/receipt validation and post-execution cleanliness. Re-derive those seams; do not roll back the verified catalog/CI repair. The current profile/harness still selects released f0 and helper bytes are unchanged. Parent will supply the genuine41304156187/a478 capture; preserve735 in place and select a new nested run directory, not overwrite historical bytes.

Allowed additional paths: tests/README.md, docs/dev/release-runbook.md, docs/dev/branch-protection.md, and new genuine files under the already allowed30-2-qualification-20261101/**. Reconcile all actual added paths into File List and scope before handoff. Existing allowlisted workflow, server/CLI tests and tooling qualification tests remain the code scope. No runtime/domain/security/auth code, dependency versions, or unrelated workflows may change.

## Tasks & Acceptance

- [x] Require successful statuses/checks, empty failure reasons and clean source for every adopted checkpoint; add actual failure/dirty negative controls.
- [x] Validate the selected ZIP/receipt size, digest, unique exact members, result bytes and provenance bindings; missing/corrupt/mismatched carriers must fail. Preserve earlier directories and directly check the seven historical file hashes with actual repository line-ending rules.
- [x] Add clean-tree assertions after upstream fixtures and after qualification, without resetting or clearing evidence; require emitted bindings clean.
- [x] Add the five omitted SDK/build/recovery trigger paths to both event filters. Preserve existing jobs, triggers and gates.
- [x] Adopt only the genuine new capture parent downloads, retain actual SDK selection/version and immutable source coordinates, and retain older source-specific proof unchanged. If acquisition is pending, finish independent code work and report pending data rather than invent it.
- [x] Add a meaningful explicit newer-than-delivered-catalog case and empty source-mode case, preserving all existing minimum and explicit cases. Update active pin prose only.
- [x] Run affected actual qualification, CI-alignment, catalog cases and actionlint. Report baseline/current method discovery plus exact phase/cumulative deltas. All committed proof checks and negative controls must run. Do not rerun unrelated full application CI locally.

## Execution handoff

Work only in the owning Memories clone and this spec. Read its AGENTS, ordinary copied AI.Tools baseline/Git guidance, relevant tracked configuration and changed callers before edits. No nested initialization. Registry reads and local affected builds/tests are authorized. No Git remote operations, commits, pushes, merges, releases or package publication in this handoff. Do not render or invoke BMAD again or spawn a recursive implementation agent. Read the spec as the sole implementation handoff. Return changed paths, exact command results, discovery reconciliation and remaining work; parent owns review/commit/push. Keep artifacts task-owned and preserve the frozen18-path review snapshot.

## Spec Change Log

2026-11-01 review1: amend the three acceptance seams demonstrated above. KEEP original functioning CI/build/runtime behavior, catalog binding, old minimum/explicit selections, genuine62-method qualification and full greena478 CI as historical support, and all immutable old approvals/captures. Carry the five trivial patch groups. Reject new exact-SDK policy and mock-payload archive expansion. No raw evidence reconstruction or forged approval.


## Review 1 Implementation and Verification

The selected evidence now lives in `30-2-qualification-20261101/run-41304156187-a4788c89/`.
It is the genuine run 41304156187/attempt 1, job 124841016777, artifact 13082461185,
from clean source `a4788c891958c1f1b6f012ff604c0f9321094751` and Builds
`f0feeb234cbc16f88c2886a6ffae7122e05dd64b`. The retained ZIP has 29,886 bytes and
SHA-256 `5de2f80e37641b05efb22ba7de65f8f2088d543f31dea618af7575c67b017c3e`,
matching the parent's fresh authoritative GitHub download receipt. All 11 unique
member sizes/digests and all seven extracted result/approval bytes were checked
before adoption. The recorded SDK is 10.0.403 with the tracked latestFeature
policy. The emitted approval is copied unchanged and remains pending. No owner
signature, live publication evidence or post-repair CI run is implied.

Adoption controls now require all six result/checksResult/overallResult fields to
pass, every check to pass, no failure reasons and an exactly false dirty flag.
They also require the selected immutable source/run/job/artifact coordinates,
archive digest/size, unique exact member inventory, every member digest/size,
matching extracted bytes and receipt source/harness/profile/helper/reference
bindings. Negative controls exercise failed/incomplete/dirty checkpoint documents,
missing/corrupt/changed carriers, mismatched receipts, duplicate/missing/extra ZIP
members and altered result bytes. Direct repository-CRLF hashes protect the seven
historical approved files; the earlier 735 receipt, ZIP and extracted results are
also pinned unchanged.

The qualification workflow checks source cleanliness before execution, after the
upstream fixtures and after the harness, and rejects every dirty or changed
emitted source binding. It preserves failure artifacts without resetting sources.
Both event filters include global.json, Directory.Build.props,
Directory.Build.targets, Directory.Packages.props and recover-partial-release.yml.
The actual shell is exercised with clean, initially dirty, post-upstream dirty,
post-harness dirty, dirty binding, changed binding and failing-process fixtures.
The catalog test retains all prior minimum/explicit cases and adds an empty
source selection and a version explicitly derived above the delivered catalog.
Only the three active shared-CI pin descriptions change in the documentation.

Verification is task-owned under `/tmp/works-memories-review1-*`. The real affected
publisher suite runs in `/tmp/works-memories-review1-verification/Hexalith.Memories`,
a private local a478 checkout with the actual working-tree repair overlay and a
full local checkout of the genuine f0 root Builds dependency. No nested submodule
was initialized, no remote operation occurred, and no local output was adopted as
clean CI evidence. An initial private setup attempt used a sparse source alias;
Git correctly refused its submodule symlink and missing helper bytes. Its failed
log remains `qualification.log`; replacing that private alias with a full local
root dependency checkout resolved the verification setup without changing gates.
An initial shell-fixture interpreter alias recursed through its own PATH; the
fixture now uses the actual interpreter path. That setup failure was corrected
before the passing alignment run.

| Scope | Exact command and result | Discovery reconciliation |
| --- | --- | --- |
| Publisher contracts | `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v`, private verification checkout: 69 pass, none skipped; actual six-checkpoint harness runs. Logs: `/tmp/works-memories-review1-qualification-attempt2.log`, final `/tmp/works-memories-review1-qualification-final.log`. All 11 committed-proof methods also pass separately in `/tmp/works-memories-review1-committed-proof-final.log`. | Baseline b93 and pre-review a478: 62 methods; current 69; phase +7, cumulative +7. |
| Shared CI and qualification shell | `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' -v`: 12 pass. Log: `/tmp/works-memories-review1-ci-alignment-fixed.log`. | Baseline b93 and a478: 10 methods; current 12; phase +2, cumulative +2. |
| Pinned upstream publisher fixtures | `pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-publish-containers.ps1`, private verification checkout: 166 tests pass. Log: `/tmp/works-memories-review1-upstream-fixtures.log`. | Separate upstream scope, no changed method inventory or delta. |
| Actual linked catalog source | `dotnet build /tmp/works-memories-focused-catalog-20261101/FocusedCatalog.csproj --configuration Debug`: zero warnings/errors. Then `dotnet tests/Hexalith.Memories.Server.Tests/bin/FocusedCatalog/net10.0/FocusedCatalog.dll -method '*RootDirectoryPackages_RequiresDeliveredCatalogSdkWithoutLoweringNewerSelectionsAsync' -method '*BuildsCatalog_Refuses*'`: 11 pass, none skipped. Logs: `/tmp/works-memories-review1-catalog-build.log`, `/tmp/works-memories-review1-catalog-tests.log`. | Source attributes b93/a478/current: 13 Facts, 2 Theories; phase/cumulative method +0. InlineData 10 -> 12, phase/cumulative case +2. Focused executions 9 -> 11. No complete Server suite claim. |
| Workflows | `/home/administrator/.local/bin/actionlint .github/workflows/qualify-builds-publication.yml .github/workflows/ci.yml`: exit 0. | No test-method delta. |
| Immutable evidence and scope | ZIP/member receipt checks and direct historical-file controls pass. `git diff --check`: exit 0. Cumulative tracked diff against b93 plus untracked evidence reconciles exactly 30/30 File List paths. | New review scope is 8 modified existing paths plus 9 new evidence files; the frozen earlier 18-path story snapshot is preserved. |

Discovery details are recorded in `/tmp/works-memories-review1-discovery.json`.
The earlier green a478 whole-application CI and qualification remain historical
support. A fresh current-commit CI run, review, commit and push belong to the
parent; no unrelated full application CI was rerun locally. No runtime/domain,
authentication, dependency-version or unrelated workflow change was made.


## Review2 Triage Log

All three layers returned final before classification. Twelve individual rows are retained; only the independently confirmed shallow-history defect is grouped. No loopback: all survivors are direct corrections to existing inputs/evidence or active operational prose.

| Finding | Verdict | Category | Evidence |
|---|---|---|---|
| blind-1 | medium | patch | Actual blocking test invokes git show for bound ancestor a478 while test-unit-contract has default depth1. All three reviewers independently trace/reproduce this. Fetch full owning-repo history for that job, preserving source selection and root-only dependency initialization. |
| blind-2 | medium | patch | Actual workflow accepts clean HEAD movement because it reads expected HEAD only after upstream/harness commands; reviewer shim reproduced acceptance. Capture and verify the originally requested event/candidate SHA before execution and compare HEAD/bindings to that fixed identity throughout. This corrects the existing exact-source guard, no new scope or public surface. |
| blind-3 | false | reject | The source-binding guard supplements the actual harness, whose main already constructs complete checkpoint documents and exits nonzero unless all results pass. Shell pipefail propagates that failure. A deliberately minimal workflow shim does not demonstrate the real harness emitting incomplete passing documents; duplicating the complete validator in shell is unnecessary. |
| blind-4 | low | patch | The retained upstream success regex accepts OK(skipped=166), which would falsely attest166executed cases. Require explicit zero skips and add the direct skipped-suite negative control; preserve actual successful logs. |
| blind-5 | low | patch | The root .gitmodules file is a real input to scoped Builds acquisition and is missing from both trigger lists. Add it and the existing path mutation controls. |
| blind-6 | low | reject | Current workflow already pins exact candidate checkout, false recursive setting and explicit root-only dependency command. No incorrect normal path was shown; new assertions against arbitrary source rewrites add coverage beyond the demonstrated corrections. Keep the currently correct acquisition path. |
| blind-7 | false | reject | The adopted carrier is immutably pinned to actual a478/13082461185/digest5de2 and genuinely has11members. Adding files while retaining that provenance must fail. A future thirteen-plus-member carrier necessarily has a new source/run/digest/inventory and requires an explicit adoption update; this story does not claim generic automatic future capture acceptance. Do not loosen the exact selected inventory. |
| blind-8 | low | patch | Document the existing operational requirement that the dedicated qualification run be successful at its bound source before adopting fresh evidence. Current required domain build aggregate is separate and current all69committed-proof checks validate the adopted passing source. Do not change remote branch protection, introduce new bypasses or silently replace existing aggregate semantics. |
| blind-9 | low | patch | Record actual github.workflow_sha/workflow_ref for future captures alongside their source SHA, e.g. an uploaded separate workflow-context record. This directly retains known runtime provenance; old captures lacking it remain honest and cannot be relabeled. Keep current pinned a478 inventory unchanged. |
| blind-10 | low | patch | Empty source-mode expectation should come from the validated catalog declaration, because package-mode assembly metadata can include the owner floor. Preserve current above-floor behavior and add the bounded below-floor/source distinction without changing production selection. |
| edge-1 | medium | patch | Same genuine shallow-history consumer failure as blind-1, separately verified by source tracing before grouping. |
| verify-other-1 | medium | patch | Same genuine missing ancestor object as blind-1; actual checker passes in full-history source and equivalent depth1fixture fails exit128. This is an Other CI defect; reviewer reports no separate missing-assertion verification gap. |


## Review 2 Targeted Repairs

All seven accepted correction groups are implemented; the twelve-row triage and
its three rejections remain unchanged. The normal blocking `test-unit-contract`
checkout now fetches full superproject history for the selected archive checker's
bound a478 global.json lookup. Its existing source selection and dependency
acquisition are preserved.

Qualification receives the originally requested event/candidate SHA as an Actions
environment value, captures it before publisher execution and checks initial HEAD,
post-upstream HEAD, post-harness HEAD and emitted source bindings against that fixed
value. The existing shell control now rejects clean HEAD changes at all three
stages, including the demonstrated case whose moved HEAD and emitted bindings
agree. Future runs upload a separate `workflow-context.json` containing the actual
source SHA, github.workflow_sha and github.workflow_ref. No workflow coordinates
were invented for the adopted a478 capture: its exact source/run/digest/eleven-member
inventory and all emitted bytes remain unchanged. Future adoption requires an
explicit new source/run/digest/inventory update.

The upstream-suite success parser now requires the exact unqualified `OK` summary
line, refusing skipped-suite summaries. A direct control exercises `skipped=1` and
`skipped=166` against the genuine retained log without changing its bytes. Both
qualification trigger filters include `.gitmodules`, covered by their existing
path-removal mutations. Active release-runbook prose records the existing dedicated
successful-bound-source qualification requirement before fresh evidence adoption;
remote branch protection and the required build aggregate are preserved.

The empty source-mode catalog expectation now comes from the validated catalog
declaration. Two bounded real-MSBuild cases use a private below-owner-floor catalog
to show the empty package selection receives 3.129.0 while source selection retains
3.128.4. Production package configuration is unchanged.

Only checks covering edited files were run for this repair; broader verification
belongs to the parent:

- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' -v`: 12 pass, including the actual workflow shell, fixed-source/clean-HEAD controls, workflow-context record, trigger mutations and shallow-checkout refusal. Log `/tmp/works-memories-review2-ci-alignment.log`.
- In the existing private verification checkout, `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -k retained_upstream_suite -k selected_capture_and_receipt -v`: 2 pass, covering the direct skipped-suite refusal and unchanged genuine carrier. Log `/tmp/works-memories-review2-retained-upstream.log`. The entire publisher/upstream suites were not rerun in this phase.
- `dotnet build /tmp/works-memories-focused-catalog-20261101/FocusedCatalog.csproj --configuration Debug`: zero warnings/errors. `dotnet tests/Hexalith.Memories.Server.Tests/bin/FocusedCatalog/net10.0/FocusedCatalog.dll -method '*RootDirectoryPackages_*'`: 10 cases pass, none skipped. Logs `/tmp/works-memories-review2-catalog-build.log` and `/tmp/works-memories-review2-catalog-tests.log`.
- `/home/administrator/.local/bin/actionlint .github/workflows/qualify-builds-publication.yml .github/workflows/ci.yml` and `git diff --check`: exit 0.

Static method discovery: qualification 69 -> 70, phase +1/cumulative +8 from b93;
CI alignment 12 -> 12, phase +0/cumulative +2. Catalog source attributes now have
13 Facts/3 Theories/14 InlineData: phase +1 method/+2 cases; cumulative +1 method/+4
cases. No full current-suite execution is claimed. Details are in
`/tmp/works-memories-review2-discovery.json`. The File List remains exactly the
same 30 cumulative paths; this second repair edits seven already allowed paths.
No commit, remote operation, nested initialization or publication occurred.

## Parent Final Verification

After synchronizing the exact current overlays into the standalone real-f0
verification checkout, the complete publisher qualification suite passed 70/70
with no skips. The owning CI-alignment suite passed 12/12, and the actual linked
catalog assembly passed all 13 selected cases with zero failures or skips.
Actionlint for both affected workflows and git diff --check passed. Logs are
/tmp/works-memories-parent-final-current-qualification.log,
/tmp/works-memories-parent-final-alignment.log, and
/tmp/works-memories-parent-final-catalog.log.

All three review layers completed before each triage. Review2 required only
direct repairs, now verified; no further specification loop was required. The
user authorized pushing and waived human validation. Hosted CI for the final
commit remains a separate required gate before release; earlier a478 success
is supporting historical evidence only. The external normal merge f80a7e58
contains a478 and has an identical tree, so its ancestry is retained before
committing this remaining repair. No historical capture or approval changed.
