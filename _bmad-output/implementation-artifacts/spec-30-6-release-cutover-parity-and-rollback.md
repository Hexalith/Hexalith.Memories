---
title: 'Release cutover parity and rollback'
type: 'feature'
created: '2026-11-15'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-6-release-cutover-parity-and-rollback'
baseline_commit: 'fad5ec5efb286a1b4589869a354895acd3aaf55c'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 30.3 (`8aee7a1323160cb39d7b1415256dbfdcb032939a`, first in v3.0.18) replaced the push-triggered caller (`46667b0d25c707bcdc961d59895bfe5a064bfd12`) with the guarded, pinned-Builds dispatch caller. v3.0.18 and v3.0.23–v4.0.0 published through it, and v3.0.19–v3.0.22 are partial tags. No lane-by-lane parity proof, parity-gated removal or release rollback procedure exists. The old `tools/test-release.ps1` lane still exists, and the runbook rolls back only CI.

**Approach:** Prove the 30.3 cutover against the prior caller using recorded live runs, a dry run and source evidence. Remove the remaining old-path file only after parity passes. Document and rehearse a rollback to the latest fully released guarded revision, and prove with read-only published-identity snapshots that nothing published changed.

## Boundaries & Constraints

**Always:**
- Live reads are read-only: `git ls-remote`, the GitHub REST API, NuGet flat-container/registration, and authenticated registry reads through the local credential store. A 401/403/5xx, timeout, redirect or unparseable reply is ambiguous: the snapshot fails and names the member.
- A snapshot covers every `v*` tag (ref → object → peeled commit), every GitHub Release (draft, prerelease, Latest, and asset name/size/digest), the versions and listed state of the ten `tools/release-packages.json` IDs, and the tag → exact top-level digest plus media type of the five `tools/release-containers.json` repositories. Capture recognized OCI/Docker image manifests and indexes, verify SHA-256 against the original response bytes and the registry digest, and refuse unknown formats or ambiguous replies. Existing tag identities must remain unchanged; the five new version tags in an additive release must be image indexes.
- Each parity lane records the prior value, the current value, the evidence class (`live-run`, `offline-fixture` or `static-source`) and a verdict. A verdict is `parity`, `stronger`, `shared-gap` (with owner and reopen trigger) or `regression`. Fixture success never counts as live proof.
- Every checkpoint row carries an owner, an exact command or artifact, a review state and a completion state.

**Never:** dispatch any workflow. Never change GitHub, nuget.org, registry or environment settings, repository variables, dependencies, Builds gitlinks, execution pins or the recovery allowlist. Never delete, retag, overwrite or unlist anything, and never move Latest. Never push the rollback rehearsal. Never change publisher, recovery or release-workflow behavior. Never rewrite historical evidence.

**Owner decisions (2026-11-15, Jérôme Piquot, "all recommended"):**
- **D1, cutover meaning.** Retroactively prove Story 30.3's caller cutover. D17's caller-owned publisher stays current. Migrating to Builds `domain-release.yml` is out of scope.
- **D2, rehearsal.** The v4.0.0 dispatched run `43432588932` (13 assets) is the controlled rehearsal, and the v3.0.19–v3.0.22 runs are live failure evidence. Both are labelled as code from before 30.5. The runbook adds an obligation: snapshot before the next owner-dispatched release, then verify an additive-only delta afterwards. Until then, live proof of the 30.4-fix and 30.5 code is a recorded obligation, not passed evidence.
- **D3, rollback target.** A rollback is a normal PR. It restores the release surface (caller, publisher, recovery tooling and their tests) to the latest fully released guarded tag, currently v4.0.0. Selection refuses v3.0.19–v3.0.25. The pre-30.3 caller is never a target: it ignores the publication flag and publishes on push.
- **D4, removal and hand-offs.** After parity passes, delete `tools/test-release.ps1`, its `CONTRIBUTING.md` instructions and its inventory facts. Record each 30.6-targeted hand-off as a parity row and re-target its trigger to `epic-30-retrospective`. Apply the triggered `epics.md` dated corrections. Do not implement release-path fixes.
- **D5, scope.** Keep one spec: a single read-only snapshot tool and a reviewed Markdown matrix, with no matrix validator and no scripted target check.
- **D6, historical image identity (owner approved 2026-11-15, "apply recommended").** Record the exact top-level digest and media type for every existing image tag, including recognized single-image manifests. Require indexes for the five new additive-release version tags. Use the before-snapshot to define existing entries; no historical allowlist, version cutoff, conversion or registry mutation. Update the small tool and fixtures, then capture before, repeat the scratch rollback, capture after and require identical identities before the removal gate can pass.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Identity unchanged | Before/after snapshots around the rollback rehearsal | `compare --expect identical` exits 0 | Any added, removed or changed entry → exit 1 naming it |
| Additive release | Snapshots around one release of version X at SHA S | Exit 0 only for one new tag at S, one release with the 13 expected assets, ten new package versions and five new image-index tags; Latest unchanged or moved to X | Any other delta → exit 1 naming it |
| Historical image format | Existing tags reference recognized image manifests or indexes | Capture each exact top-level digest and media type; identical comparison detects either changing | Unknown format or digest mismatch → exit 1 naming the member, with no snapshot written |
| Ambiguous probe | Registry, NuGet or GitHub reply that is ambiguous | No snapshot is written | Exit 1 naming the member and status |
| Parity regression | Any matrix row with verdict `regression` | Removal is not performed | The checkpoint stays incomplete |

</frozen-after-approval>

## Code Map

These anchors are from investigation on 2026-11-15. Re-derive them before editing.

- **Current caller:** `.github/workflows/release.yml`.
  - `release-source` exact main + CI proof and production readback (L35-68).
  - Pinned `prepare-domain-release@ae77d569…` (L97). It stops every later step unless `HEXALITH_RELEASE_PUBLISH_ENABLED` is `true` (currently `true`).
  - Preflight `-CheckDestinations` (L132), `NuGet/login@v1` OIDC (L169), `npx semantic-release` (L176).
  - Failure alert, diagnostics and recovery bundle (L203-237).
- **Prior caller** `git show 46667b0d:.github/workflows/release.yml`: `on: push: branches: [main]`; local restore/build plus `tools/test-release.ps1`; `environment: production`; OIDC key. It has no `HEXALITH_RELEASE_PUBLISH_ENABLED` read. It publishes 10 packages and 5 images (no frozen contract). Its `git show 46667b0d:.releaserc.json` assets are `*.nupkg` and the production yaml, 11 in total.
- **Publisher (unchanged):** `.releaserc.json:9-19`.
  - The current assets add the evidence tarball and its `.sha256.json`, for 13.
  - `publish-release.ps1:88-112` runs authorize → NuGet → containers.
  - `publish-nuget.ps1:165` uses `--skip-duplicate` (shared gap).
  - `create-partial-publish-issue.ps1:42` alerts only on `partial-publish` (shared gap).
- **Patterns to reuse:**
  - `tools/container-publication-contract.py`: `classify_image` (~L694-709) for present/absent/ambiguous, and its redaction helpers. Copy the patterns, not the write phases.
  - `tools/check-release-destinations.py`: read-only destination probes.
- **Live evidence (read-only `gh`):**
  - v4.0.0 run `43432588932`: success, `workflow_dispatch`, head `c4ee997702ede948ada5f99eeae7b3782e92003b`, 13 assets.
  - Failed dispatched runs on `d9d61d8a`, `10ac2d67`, `49c913da`, `4536ae93`, `0433b494`, `4f32c5d7` and `f2c1d8a8`.
  - v3.0.19–v3.0.22 have tags but no Release.
  - `gh run list --commit` needs the full SHA.
- **Dry run:** `python3 tools/qualify-builds-publication.py --output-directory <dir>` produces the offline complete-set dry run with the real pinned helpers. Run it on a clean tree, because evidence cannot bind its own commit.
- **Removal:** `tools/test-release.ps1` (54 lines, no workflow invokes it).
  - Remove: `CONTRIBUTING.md:163,670` (replace with `./tools/test.ps1`); `CiTestInventoryTests.cs:43` constant; `TestReleaseScript_ShouldDriveFromSharedUnitContractInventory` (~L250); `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` (~L1266); and the parser-helper fixtures if they become dead.
  - Keep: `release_dispatch_test.py:88` (an absence assertion) and the `story_scope_validator_test.py:36` fixture text.
- **CI wiring:** add `Run release identity snapshot fixtures` to the `test-unit-contract` job beside `release_dispatch` (`ci.yml:354`). Add the same command to `MANDATORY_COMMANDS` in `tests/tooling/ci_alignment/ci_alignment_test.py:64-110`.
- **Runbook:** `docs/dev/release-runbook.md`.
  - Add "Release Cutover Parity" and "Release Rollback" after "Current Release Contract" (L90-305).
  - Update the L255 do-not-deploy trigger and the L595 30.6 pointer.
  - Keep the CI rollback section (L43-89) unchanged.
- **Ledger:** `deferred-work.md` 30.6 entries at ~5980, 6033, 6040, 6062, 6072, 6116, 6173, 6218, 6237 and 6244.
  - Use the format `status: resolved 2026-11-15` / `resolution:`, or append a dated `retarget:` line.
  - 6173 (R6-H4) resolves through D3. 6237 resolves through the `epics.md` notes. 6116 stays open on the owner's do-not-deploy action.
  - The rest are re-targeted to `epic-30-retrospective` (`sprint-status.yaml:528`).
- **Planning:** `epics.md`:
  - Story 30.6 (~L5860-5894): dated notes at "existing caller and retained custom publisher" and "Owned paths".
  - Story 30.5: the F6 notes at ~L5830, 5832 and 5847.

## Tasks & Acceptance

**Execution:**
- [x] `tools/release-identity-snapshot.py` (new) -- `capture --output FILE`, which writes sorted JSON plus a `FILE.sha256` line, and `compare BEFORE AFTER --expect identical|additive [--version X --source-sha S]`. Standard library plus the `git`, `gh` and registry CLIs. -- One small read-only tool carries both the rollback proof and the next-release check.
- [x] `tests/tooling/release_cutover/release_identity_snapshot_test.py` (new) -- execute every I/O row with fake `git`, `gh`, NuGet and registry responders, including positive controls and a Latest-moved-elsewhere refusal -- the matrix needs executed coverage.
- [x] `_bmad-output/implementation-artifacts/30-6-cutover/` (new) -- the evidence set:
  - `dry-run.md`: the qualifier complete-set result at the clean base, plus the before-snapshot showing the next version absent.
  - `parity-matrix.md`: lanes for trigger/source, environment, Builds identity, packages, images, assets, NuGet OIDC, registry authorization, failure, recovery, test authority, version floor and terminal tag recheck.
  - `identity-before.json` and `identity-after-rollback.json`.
  - `rollback-rehearsal.md`.
  -- These are the checkpoint artifacts.
- [x] Rollback rehearsal -- in a scratch `git worktree` at HEAD, restore the D3 release surface from v4.0.0. Run actionlint and the release_packages, release_preflight, release_dispatch, publish_nuget and publish_containers lanes. Snapshot before and after with `--expect identical`, then remove the worktree. Never push. -- This proves the rollback PR works without mutation.
- [x] `tools/test-release.ps1`, `CONTRIBUTING.md`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- only after the matrix shows zero `regression`: delete the script, re-point the instructions to `./tools/test.ps1`, and replace the script facts with one absence fact -- this is the D4 gated removal.
- [x] `.github/workflows/ci.yml`, `tests/tooling/ci_alignment/ci_alignment_test.py` -- add and pin the new fixture step -- so the suite blocks in CI.
- [x] `docs/dev/release-runbook.md` -- write two sections and update two lines:
  - "Release Cutover Parity": the matrix summary, the evidence, the removal record, and the D2 next-release snapshot obligation with its exact commands.
  - "Release Rollback": the D3 target rule with refused tags, the PR procedure, and the fact that rolling back to v4.0.0 drops retained-byte recovery bundles. After any publication, the only continuation is recovery or a named blocker.
  - Update L255 and L595.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/planning-artifacts/epics.md` -- apply the D4 ledger resolutions, re-targets and dated correction notes.

**Acceptance Criteria:**
- Given the parity matrix, when it is reviewed, then every lane carries the prior value, the current value, the evidence class and a verdict, and the removal diff exists only with zero `regression` rows.
- Given the rollback rehearsal, when the before/after snapshots are compared, then they are identical and the rehearsed release lanes pass.
- Given the runbook, when an operator prepares a rollback, then v3.0.19–v3.0.25 and the pre-30.3 caller are refused as targets, and the next-release additive check is documented with exact commands.

## Checkpoint Evidence

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Dry-run parity | Memories Release Engineer | `30-6-cutover/dry-run.md`, `complete-set-dry-run.json`, `identity-before.json` | reviewed 2026-11-16 | complete; six clean-base offline checkpoints; candidate 4.0.1 absent in complete before-snapshot |
| Controlled rehearsal | Memories Release Engineer | v4.0.0 run `43432588932`, `30-6-cutover/live-runs.json`, `live-jobs.json`, `live-release-readback.json` (pre-30.5 code, D2) | reviewed 2026-11-16 | historical run/readback complete; no final-code live credit |
| Next-release additive verification | Jérôme Piquot | Runbook obligation: `compare --expect additive` on the next dispatch | accepted 2026-11-16 | not complete (D2 obligation); consequence: no final-code live publication credit; reopen trigger: next owner-authorized release followed by the additive comparison |
| Lane-by-lane comparison | Memories Release Engineer | `30-6-cutover/parity-matrix.md` and prior/current sources | reviewed 2026-11-16 | complete; zero regression verdicts |
| Prior publisher removal gate | Memories Release Engineer | `30-6-cutover/parity-matrix.md` removal record, `retirement.diff`, `ci-inventory-d6.txt` | reviewed 2026-11-16 | complete; old script/instructions/dead C# facts retired after identity proof; C# 101/101 pass |
| Rollback without artifact mutation | Platform Operations | `30-6-cutover/rollback-rehearsal.md` D6 addendum, `identity-d6-proof.json`, both `compare --expect identical` artifacts | reviewed 2026-11-16 | complete; all identities identical through the complete 104-ID isolated-module fallback; historical full-discovery flakes retained |
| Rollback target refusal | Memories Maintainer | Runbook Release Rollback; `30-6-cutover/live-release-readback.json` | reviewed 2026-11-16 | documented; v4.0.0 full target set read back |
| Snapshot fixtures | Memories Maintainer | `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v`; `30-6-cutover/snapshot-fixtures-numeric-links.txt` | reviewed 2026-11-16 | 29/29 methods pass; zero skips; review guards, Git checkout, actual redirects and numeric pagination covered |

## File Scope

Allowed files for this story:

- `.gitattributes` (preserve exact bytes of Story 30.6 hash-bound evidence through Git checkout)
- `tools/release-identity-snapshot.py`
- `tests/tooling/release_cutover/release_identity_snapshot_test.py`
- `_bmad-output/implementation-artifacts/30-6-cutover/*`
- `tools/test-release.ps1` (deletion)
- `CONTRIBUTING.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `.github/workflows/ci.yml`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/planning-artifacts/epics.md` (dated correction notes only)
- `_bmad-output/implementation-artifacts/spec-30-6-release-cutover-parity-and-rollback.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## File List

- `.gitattributes`
- `.github/workflows/ci.yml`
- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/acceptance-final-scope.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/acceptance-hardening-scope.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/actionlint-ci-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/actionlint-ci-final.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/approval.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/changed-files-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/changed-files.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-alignment-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-alignment-final.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-baseline-build.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-baseline.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6-build.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/complete-set-dry-run.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/coordinator-acceptance.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/dry-run.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/evidence-upload.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/final-changed-files.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/final-scope.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/final-verification.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/github-asset-digest-audit.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback-capture.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback-modules-capture.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback-modules.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback-modules.json.sha256`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback.json.sha256`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-before-capture.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-before.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-before.json.sha256`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-d6-proof.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-post-review.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-post-review.json.sha256`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-rollback-compare.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/identity-rollback-modules-compare.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/live-jobs.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/live-release-readback.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/live-runs.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/parity-matrix.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/partial-publication.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/phase-verification.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/post-review-capture-verified.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/post-review-capture-verified.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/post-review-capture.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/post-review-capture.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/post-review-compare.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/qualification.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/recovery.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/release-dispatch-retirement.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/retirement.diff`
- `_bmad-output/implementation-artifacts/30-6-cutover/review-claim-verification.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-actionlint.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-actionlint.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-affinity-full.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-affinity-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-affinity-targeted.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-rerun-full.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-rerun-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-containers-rerun-targeted.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-isolated-modules-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-isolated-modules-verified.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-module-partial_release_completion_test.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-module-publication_contract_test.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-module-publish_containers_test.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-module-registry_authorization_test.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-module-release_orchestration_test.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-publish_containers.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-publish_nuget.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-release_dispatch.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-release_packages.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-release_preflight.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-d6-surface.diff`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-final-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-nuget-environment.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-nuget-padding-20-targeted.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-nuget-padding-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-publish_containers.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-publish_nuget-padded.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-publish_nuget-short-cwd.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-publish_nuget.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-rehearsal.md`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-release_dispatch.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-release_packages.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-release_preflight.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-surface.diff`
- `_bmad-output/implementation-artifacts/30-6-cutover/rollback-surface.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/scope-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/set-drift.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-capture-refused.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-acceptance-hardening.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-final.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-numeric-links.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-review-fixes-final.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-review-fixes.txt`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-30-6-release-cutover-parity-and-rollback.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/dev/release-runbook.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `tests/tooling/release_cutover/release_identity_snapshot_test.py`
- `tools/release-identity-snapshot.py`
- `tools/test-release.ps1`

## Dev Notes

### Historical Context Classification

| Source | Classification | Permitted use |
| --- | --- | --- |
| Story 30.3 guarded caller (`8aee7a13`) | current-narrow-pattern | The cutover under proof, plus its source/readback evidence. |
| Story 30.5 recovery | current-narrow-pattern | Probe classification and redaction patterns only; no write phases. |
| Story 30.4 five-image contract | historical-reference-only | Set identity and the 13-asset expectation. |
| Story 30.2 qualification | historical-reference-only | Offline complete-set dry run; no live credit. |
| 2026-07-26 umbrella Story 30.1/30.5 text | anti-template | Wording origin only. Never import `domain-release.yml` adoption or the nine-package/four-image shape. |

### Slice Proof

One outcome: proven cutover parity with a parity-gated removal and a mutation-free rollback. Hand-offs are recorded and re-targeted, not implemented (D4).

### Epic AC Verification

Verified 2026-11-15 against `55693790997d7055ff673f72cd196cf80179ecd6`.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| "Stories 30.3, 30.4, and 30.5 must be done" | behavioral | `sprint-status.yaml:524-526` | done | confirmed |
| "the existing caller and retained custom publisher" | behavioral | `git tag --contains 8aee7a13` → v3.0.18; `architecture.md:572` | The caller already cut over in 30.3; D17 retains the publisher | corrected (D1) |
| "ten-package, five-image" | quantitative | `tools/release-packages.json`, `tools/release-containers.json`, and the `git show 46667b0d:` equivalents | 10/5 at both revisions | confirmed |
| "GitHub Release asset" | quantitative | `.releaserc.json:18`; `gh release view v4.0.0 --json assets` | Prior 11, current 13 | confirmed (stronger) |
| "controlled release rehearsal" | behavioral | `gh run view 43432588932` | No non-production feed/registry exists; v4.0.0 dispatched run is the rehearsal | corrected (D2) |
| "the old path is removed only after parity succeeds" | behavioral | `git show --stat 8aee7a13`; `tools/test-release.ps1` | Caller removed in 30.3 without a gate; script remains | confirmed gap (D4) |
| "rollback restores the prior caller" | behavioral | `git show 46667b0d:.github/workflows/release.yml` | The prior caller publishes on push and ignores the publication flag | corrected (D3) |
| "Owned paths: the cutover and rollback sections of `docs/dev/release-runbook.md`" | location | Runbook headings | No release cutover/rollback section; proof needs tooling and tests | corrected |

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-11-15 | create-story | The spec is created from epic Story 30.6 with owner decisions D1–D5. Owner: Jérôme Piquot. | `release_cutover`: new scope 0 → planned 8–12 methods. `ci_alignment` 15 methods (+0 planned): `python3 -B -c "import unittest; print(unittest.defaultTestLoader.discover('tests/tooling/ci_alignment', pattern='*_test.py', top_level_dir='tests/tooling/ci_alignment').countTestCases())"`. `release_dispatch` 17 methods (+0 planned): the same command with `release_dispatch`. CT not run at creation, because it needs an isolated Debug build; dev-story captures the baseline before the CT edit (planned −1 to −5 cases). Phase delta +0. | `spec-30-6-release-cutover-parity-and-rollback.md` added. |
| 2026-11-15 | implementation (pending gates) | Read-only snapshot tool/fixtures/CI, matrix and evidence, clean-base qualifier, scratch rollback, runbook and dated hand-offs. Full snapshot and removal gates stay incomplete on the two recorded blockers. | New release_cutover 0 → 12 methods (12 pass), ci_alignment 15 → 15 (15 pass), CT baseline 141 → 141 (141 pass; no C# edit), no fixture credit represented as live proof. Rollback historical lanes recorded independently. | Allowed implementation/doc/evidence paths only; old script/instructions/C# facts retained because retirement is gated. |
| 2026-11-15 | acceptance hardening | Require every GitHub asset SHA-256 digest; refuse missing/null digests and malformed releases/latest shapes with named member diagnostics before any snapshot write. Strict historical-index policy and pending gates remain unchanged. | release_cutover 12 → 14 methods (+2); 14/14 pass, zero skips. Exact output: `30-6-cutover/snapshot-fixtures-acceptance-hardening.txt`. No live probes or mutations in this correction. | Only snapshot tool/fixtures and their evidence/notes changed. |
| 2026-11-15 | rehearsal environment follow-up | Exact v4.0.0 NuGet method and full lane pass with a twenty-character child directory beneath the wrapper TMPDIR; no source/test edits or output normalization. Original failed evidence remains unchanged. Read-only audit finds valid SHA-256 digests on all 1,700 assets across 183 Releases. | Historical NuGet 32/32 pass, zero skips; snapshot fixtures remain 14/14. The full snapshot/removal gate remains pending only on the historical-index policy. | Added supplemental padding/audit/final-result evidence and updated current summaries; no live mutations. |
| 2026-11-16 | D6 completion and parity-gated retirement | Historical capture records recognized manifests/indexes with exact digest/media type. Full before/after plus supplemental final-fallback snapshots are identical; exact rollback modules qualify all 104 IDs after retained historical discovery flakes. Zero-regression coordinator parity verification gates old-runner/instructions/dead-fact retirement. Final workflow review remains pending. | release_cutover 14 → 16 (+2), 16 pass; ci_alignment 15 pass; C# inventory 141 → 101 (-40), 101 pass after isolated Debug/source build, zero skips/warnings/errors. Full container set 104/104 passes in isolated modules; original failed discoveries and collector correction retained. | D6 tool/fixtures, approved retirement paths, current reports/checkpoints and new evidence only. |
| 2026-11-16 | dev-story | D6 exact historical manifest/index capture, complete identity bracket and parity-gated retirement. | Snapshot 0 → 16; CI 15/15; dispatch 17/17; C# 141 → 101 (-40), 101 pass; historical container set 104/104 in complete isolated modules, zero skips. | matched 111/111; cumulative File List includes implementation, documentation, preserved evidence and retirement. |
| 2026-11-16 | code-review | Three independent reviewers plus final live verification; direct guard/coverage/doc fixes, one uncommon-low finding rejected and no new deferrals. | Snapshot 16 → 29 (+13), 29/29 pass; CI 15/15; unchanged C# 101/101 evidence retained; post-review full live capture and identical comparison exit 0. | matched 111/111; exact cumulative File List reconciled after review corrections. |

## Implementation Notes

Implemented from the complete frozen spec/context and owner-approved D6. The read-only snapshot
tool uses GET-only standard-library probes, `git ls-remote` with redirects disabled, `gh auth token`
and the existing local Docker credential store. Credentials remain in memory. Capture records
sorted tag object/peeled identities, every Release's draft/prerelease/Latest and asset name/size/
SHA-256 identity, every version/listed state for all ten package IDs, and each tag's exact top-level
digest plus media type in all five image repositories. OCI/Docker schema-2 manifests and indexes
are recognized; original response bytes must hash to the registry digest. Unknown formats,
conflicting/missing media types, encoded/ambiguous manifests and all other ambiguous probes refuse
without writing a snapshot. Existing entries come from the before-snapshot; new additive-release
tags must be indexes. Twenty-nine methods now exercise these rules, including real local HTTP and Git round-trip controls; fixture results grant no live publication credit.
The CI fixture step is pinned in both the unit-contract job and MANDATORY_COMMANDS.

The complete live before-snapshot was written on 2026-11-15 at 22:59:49 UTC (23:59:49 Europe/Paris).
It covers 196 `v*` tags, 183 Releases / 1,700 assets, all ten package inventories and 463 image tags:
418 Docker manifests and 45 OCI indexes. Candidate 4.0.1 is absent everywhere. The repeated D3
rollback and its final full-module fallback ran on 2026-11-16 Europe/Paris. Both after-captures
compare identically with the before-snapshot; all three JSON files share SHA-256
`68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`. `identity-d6-proof.json`
persists the before receipt's filesystem timestamp, rehearsal intervals, after-capture start/finish,
exact comparison commands/output and hashes. No published identity changed.

Actionlint and the four non-container release lanes pass in exact restored v4.0.0 source. Three
full container discoveries each fail one different historical HTTP/1.0 cancellation positive
control. Current baseline already fixes that fixture race; target source/tests remain unchanged.
The baseline's focused-validation fallback executes every complete container module once in its
own fresh Python process: 9/50/26/12/7 = 104/104 exact discovered IDs, zero skips, all subprocesses
exit 0. `rollback-d6-isolated-modules-verified.json` proves full ID equality and preserves the
original collector's duplicated-method-name parsing error with its unchanged raw logs. Its
metadata correction reran no tests. Original failures, refusal, prior passes and every rehearsal
log remain retained. The supplemental after-snapshot brackets this final fallback. Every scratch
worktree was removed after child completion; no rehearsal was pushed.

After the coordinator independently verified zero matrix regressions and the complete identical
proof passed, D4 retirement deleted the old runner, removed its CONTRIBUTING inventory mention,
replaced the runnable instruction with `./tools/test.ps1`, and replaced the C# runner facts and
now-dead parser/fixture family with one absence fact. The independent deferred-ID guard and the
two explicitly retained Python fixtures remain. The inventory class moves from 141 to 101 cases
(-40), all passing after an isolated Debug/source build with zero warnings/errors. `retirement.diff`
retains the exact change.

Runbook sections document D1–D6, exact next-release snapshot/additive commands, unsafe target
refusals, the v4.0.0 retained-byte recovery loss, and the precise isolated-module fallback. D4's dated
planning/ledger hand-offs stay applied and release-path gaps remain re-targeted to
`epic-30-retrospective`; the owner do-not-deploy action remains open. No publisher, recovery,
release-workflow, dependency, gitlink, execution pin, allowlist or external setting changed.
D2's final-code live publication proof remains the next owner-dispatched release obligation.
Independent three-layer review and final verification completed on 2026-11-16. All accepted findings are fixed; no new work was deferred. The spec is complete and sprint status is review under the build workflow.

## Spec Change Log

- 2026-11-16, review correction: declare `.gitattributes` solely to preserve already-required exact evidence bytes through Git checkout. No release behavior, frozen acceptance intent or public interface changes. Verified guard/coverage/doc defects route to minimal patches; no re-derivation is needed for these direct corrections.

- 2026-11-15, owner-approved D6: broaden historical capture to recognized manifests and indexes, retaining exact digest/media-type identity. New additive-release image tags remain index-only. The user explicitly approved the recommended amendment; all read-only, ambiguity and immutability constraints remain in force.

## Review Triage Log

| ID | Finding | Verdict | Evidence / disposition |
| --- | --- | --- | --- |
| B1 | Git checkout changes hash-bound snapshot bytes | high | Reproduced a temporary Git index/checkout round trip with the root text-auto/CRLF rule; snapshot bytes changed. Patch: pin the story's exact-byte evidence and add a checkout test. The identity requirement is already explicit; declaring this necessary supporting file does not change intent. |
| B2 | Malformed receipt-valid nested snapshots pass identical mode | medium | Reproduced exit 0 with tags=42, releases=[], and empty member inventories. Patch: validate the existing snapshot schema and nested member shapes before comparison; no new input format or CLI surface. |
| B3 | Flat-container membership loss is invisible | medium | Reproduced identical output after every flat versions list became empty. NuGet documents both listed and unlisted versions at the flat-container index. Patch: reconcile complete normalized version sets, refusing disagreement. |
| B4 | A short GitHub page with a next Link is truncated | medium | Reproduced one collected page despite an explicit next Link. Patch: follow validated same-repository next links for releases/assets and test the short-page case. |
| B5 | NuGet fetched page ignores parent count/bounds | medium | Reproduced acceptance of a parent advertising 100 leaves and a self-consistent fetched one-leaf page. Patch: reconcile fetched count/lower/upper metadata with the parent. |
| B6 | Latest can identify a draft/prerelease | medium | Reproduced capture with Latest pointing to the collected draft prerelease. Patch: require a published stable record and reconcile the endpoint's state. |
| B7 | Recognized format controls omit required descriptor fields | medium | Current positive index descriptors contain only digest; recognized OCI descriptors require mediaType and size too. Patch the mandatory-field checks and controls. Do not add a registry-wide child-media allowlist: unknown child media types are permitted by OCI and the tool proves top-level identity. |
| B8 | Unsupported JSON content encoding is accepted | medium | Reproduced capture of plain JSON marked br. Patch: explicitly allow identity/gzip and refuse unsupported encodings with the member named. |
| B9 | Capture overwrites evidence created during its long read | high | Reproduced another writer creating both files during capture; current writes silently replace them. Patch: exclusive output/receipt creation with safe cleanup only of this attempt's incomplete files. |
| B10 | Rollback instructions omit Builds materialization | medium | Exact v4.0.0 publication fixtures raise SkipTest when the scratch Builds directory is not its own checkout. Patch: document the unchanged root-gitlink worktree setup and require zero skips for every lane. |
| E1 | Identical-byte asset reupload changes API asset-ID URLs | low | Such an external reupload can change API IDs while retaining the frozen name/size/digest identity. Current release/runbook consumers address assets by version and name. Reject under the uncommon-low rule: extending identity metadata and its guards for byte-identical reuploads adds scope without everyday harm in the authorized immutable workflow. Changed bytes/names/sizes remain detected. |
| E2 | Concurrent capture overwrites retained evidence | high | Same demonstrated race as B9; retain this finding separately and group its patch with B9. |
| V1 | Redirect fixtures bypass the installed opener | medium | Pre-verified mutation left 16 tests green after removing NoRedirect from build_opener; existing mocks and standalone handler test do not cover installation. Patch: exercise the actual opener with a local redirect and prove the destination was never requested. |
| V2 | Identical comparison lacks a Latest-only negative control | medium | Pre-verified mutation omitting Latest from identical mode left 16 tests green; only additive mode mutates Latest. Patch: CLI refusal for two checksummed snapshots differing only in Latest, naming identity/latest. |
| P1 | Canonical numeric GitHub pagination was over-restricted by B4 fix | medium | The first post-review live capture refused the real repositories/1188027961/releases next link. Patch binds the numeric ID via cached authenticated repository metadata and tests correct/foreign numeric links; the final fresh live capture succeeds and compares identical. |

Review corrections completed 2026-11-16: B1–B10, E2, V1–V2 and P1 are addressed; E1 remains rejected under the uncommon-low rule. Snapshot coverage is 29/29. No new deferred entry was created.

## Design Notes

- **Why the trim:** a reviewed Markdown matrix carries the parity judgement. A validator would only check its shape. The snapshot tool is the one mechanism that is reused (rollback proof and next-release check).
- **Rollback cost:** v4.0.0 tooling predates the retained-byte bundle. A failed release after a rollback has no current-set recovery path, so the runbook must say so.

## Verification

Observed D6 completion checks: snapshot fixtures 16/16, ci_alignment 15/15, zero skips; changed
CI actionlint clean. The isolated Debug/source CLI inventory build has zero warnings/errors and
the changed class passes 101/101 cases (baseline 141, delta -40). The six clean-base qualifier
checkpoints remain passing offline evidence. Restored v4.0.0 actionlint, release_packages (28),
release_preflight (50), release_dispatch (17), and publish_nuget (32) pass. The entire container
set passes in five isolated module processes (104/104 exact IDs, zero skips); three historical
all-in-one discovery flakes remain failed and retained, with the canonical-ID collector correction
fully traced. Both complete before/after identity comparisons exit 0; the supplemental one covers
the final module fallback. Exact commands, timestamps, hashes, results and cleanup evidence are
under `30-6-cutover/`. Independent review is complete. The post-review live snapshot is identical to the before-snapshot; next-release additive verification remains the explicit D2 owner obligation.

**Commands:**
- `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v` -- expected: all methods pass, zero skips.
- `run-with-temp python3 -B -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' -v` -- expected: 15/15.
- `python3 tools/release-identity-snapshot.py compare _bmad-output/implementation-artifacts/30-6-cutover/identity-before.json _bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback.json --expect identical` -- expected: exit 0.
- `run-with-temp actionlint .github/workflows/ci.yml` -- expected: clean.
- The isolated Debug build, then `DiffEngine_Disabled=true dotnet exec <build>/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: all cases pass.
- `run-with-temp python3 -B tools/check-story-slice-scope.py --story-key 30-6-release-cutover-parity-and-rollback --require-record` and `tools/check-story-file-scope.py --story-key spec-30-6-release-cutover-parity-and-rollback --changed-files-file <list>` -- expected: pass.

## Final Verification — 2026-11-16

Snapshot fixtures pass 29/29, CI alignment 15/15, and changed CI actionlint is clean. The
Debug/source C# inventory remains 101/101 with zero build warnings/errors. Both original D6
comparisons and the new guarded live capture compare identically. All four complete snapshot
JSON byte streams have SHA-256 `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
`30-6-cutover/post-review-capture-verified.json` binds the final capture to its exact tool hash;
the earlier over-restricted pagination refusal remains preserved. Git checkout now preserves
all exact-byte evidence. `30-6-cutover/final-verification.json` records the final checks.
