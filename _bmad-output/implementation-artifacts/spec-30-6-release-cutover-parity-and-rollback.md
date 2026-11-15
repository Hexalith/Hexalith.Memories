---
title: 'Release cutover parity and rollback'
type: 'feature'
created: '2026-11-15'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-6-release-cutover-parity-and-rollback'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 30.3 (`8aee7a1323160cb39d7b1415256dbfdcb032939a`, first in v3.0.18) replaced the push-triggered caller (`46667b0d25c707bcdc961d59895bfe5a064bfd12`) with the guarded, pinned-Builds dispatch caller. v3.0.18 and v3.0.23–v4.0.0 published through it, and v3.0.19–v3.0.22 are partial tags. No lane-by-lane parity proof, parity-gated removal or release rollback procedure exists. The old `tools/test-release.ps1` lane still exists, and the runbook rolls back only CI.

**Approach:** Prove the 30.3 cutover against the prior caller using recorded live runs, a dry run and source evidence. Remove the remaining old-path file only after parity passes. Document and rehearse a rollback to the latest fully released guarded revision, and prove with read-only published-identity snapshots that nothing published changed.

## Boundaries & Constraints

**Always:**
- Live reads are read-only: `git ls-remote`, the GitHub REST API, NuGet flat-container/registration, and authenticated registry reads through the local credential store. A 401/403/5xx, timeout, redirect or unparseable reply is ambiguous: the snapshot fails and names the member.
- A snapshot covers every `v*` tag (ref → object → peeled commit), every GitHub Release (draft, prerelease, Latest, and asset name/size/digest), the versions and listed state of the ten `tools/release-packages.json` IDs, and the tag → index digest of the five `tools/release-containers.json` repositories.
- Each parity lane records the prior value, the current value, the evidence class (`live-run`, `offline-fixture` or `static-source`) and a verdict. A verdict is `parity`, `stronger`, `shared-gap` (with owner and reopen trigger) or `regression`. Fixture success never counts as live proof.
- Every checkpoint row carries an owner, an exact command or artifact, a review state and a completion state.

**Never:** dispatch any workflow. Never change GitHub, nuget.org, registry or environment settings, repository variables, dependencies, Builds gitlinks, execution pins or the recovery allowlist. Never delete, retag, overwrite or unlist anything, and never move Latest. Never push the rollback rehearsal. Never change publisher, recovery or release-workflow behavior. Never rewrite historical evidence.

**Owner decisions (2026-11-15, Jérôme Piquot, "all recommended"):**
- **D1, cutover meaning.** Retroactively prove Story 30.3's caller cutover. D17's caller-owned publisher stays current. Migrating to Builds `domain-release.yml` is out of scope.
- **D2, rehearsal.** The v4.0.0 dispatched run `43432588932` (13 assets) is the controlled rehearsal, and the v3.0.19–v3.0.22 runs are live failure evidence. Both are labelled as code from before 30.5. The runbook adds an obligation: snapshot before the next owner-dispatched release, then verify an additive-only delta afterwards. Until then, live proof of the 30.4-fix and 30.5 code is a recorded obligation, not passed evidence.
- **D3, rollback target.** A rollback is a normal PR. It restores the release surface (caller, publisher, recovery tooling and their tests) to the latest fully released guarded tag, currently v4.0.0. Selection refuses v3.0.19–v3.0.25. The pre-30.3 caller is never a target: it ignores the publication flag and publishes on push.
- **D4, removal and hand-offs.** After parity passes, delete `tools/test-release.ps1`, its `CONTRIBUTING.md` instructions and its inventory facts. Record each 30.6-targeted hand-off as a parity row and re-target its trigger to `epic-30-retrospective`. Apply the triggered `epics.md` dated corrections. Do not implement release-path fixes.
- **D5, scope.** Keep one spec: a single read-only snapshot tool and a reviewed Markdown matrix, with no matrix validator and no scripted target check.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Identity unchanged | Before/after snapshots around the rollback rehearsal | `compare --expect identical` exits 0 | Any added, removed or changed entry → exit 1 naming it |
| Additive release | Snapshots around one release of version X at SHA S | Exit 0 only for one new tag at S, one release with the 13 expected assets, ten new package versions and five new image tags; Latest unchanged or moved to X | Any other delta → exit 1 naming it |
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
- [ ] `tools/release-identity-snapshot.py` (new) -- `capture --output FILE`, which writes sorted JSON plus a `FILE.sha256` line, and `compare BEFORE AFTER --expect identical|additive [--version X --source-sha S]`. Standard library plus the `git`, `gh` and registry CLIs. -- One small read-only tool carries both the rollback proof and the next-release check.
- [ ] `tests/tooling/release_cutover/release_identity_snapshot_test.py` (new) -- execute every I/O row with fake `git`, `gh`, NuGet and registry responders, including positive controls and a Latest-moved-elsewhere refusal -- the matrix needs executed coverage.
- [ ] `_bmad-output/implementation-artifacts/30-6-cutover/` (new) -- the evidence set:
  - `dry-run.md`: the qualifier complete-set result at the clean base, plus the before-snapshot showing the next version absent.
  - `parity-matrix.md`: lanes for trigger/source, environment, Builds identity, packages, images, assets, NuGet OIDC, registry authorization, failure, recovery, test authority, version floor and terminal tag recheck.
  - `identity-before.json` and `identity-after-rollback.json`.
  - `rollback-rehearsal.md`.
  -- These are the checkpoint artifacts.
- [ ] Rollback rehearsal -- in a scratch `git worktree` at HEAD, restore the D3 release surface from v4.0.0. Run actionlint and the release_packages, release_preflight, release_dispatch, publish_nuget and publish_containers lanes. Snapshot before and after with `--expect identical`, then remove the worktree. Never push. -- This proves the rollback PR works without mutation.
- [ ] `tools/test-release.ps1`, `CONTRIBUTING.md`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- only after the matrix shows zero `regression`: delete the script, re-point the instructions to `./tools/test.ps1`, and replace the script facts with one absence fact -- this is the D4 gated removal.
- [ ] `.github/workflows/ci.yml`, `tests/tooling/ci_alignment/ci_alignment_test.py` -- add and pin the new fixture step -- so the suite blocks in CI.
- [ ] `docs/dev/release-runbook.md` -- write two sections and update two lines:
  - "Release Cutover Parity": the matrix summary, the evidence, the removal record, and the D2 next-release snapshot obligation with its exact commands.
  - "Release Rollback": the D3 target rule with refused tags, the PR procedure, and the fact that rolling back to v4.0.0 drops retained-byte recovery bundles. After any publication, the only continuation is recovery or a named blocker.
  - Update L255 and L595.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/planning-artifacts/epics.md` -- apply the D4 ledger resolutions, re-targets and dated correction notes.

**Acceptance Criteria:**
- Given the parity matrix, when it is reviewed, then every lane carries the prior value, the current value, the evidence class and a verdict, and the removal diff exists only with zero `regression` rows.
- Given the rollback rehearsal, when the before/after snapshots are compared, then they are identical and the rehearsed release lanes pass.
- Given the runbook, when an operator prepares a rollback, then v3.0.19–v3.0.25 and the pre-30.3 caller are refused as targets, and the next-release additive check is documented with exact commands.

## Checkpoint Evidence

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Dry-run parity | Memories Release Engineer | `30-6-cutover/dry-run.md` | pending | not complete |
| Controlled rehearsal | Memories Release Engineer | v4.0.0 run `43432588932` in `parity-matrix.md` (pre-30.5 code, D2) | pending | not complete |
| Next-release additive verification | Jérôme Piquot | Runbook obligation: `compare --expect additive` on the next dispatch | pending | not complete (obligation; no live credit) |
| Lane-by-lane comparison | Memories Release Engineer | `30-6-cutover/parity-matrix.md` | pending | not complete |
| Prior publisher removal gate | Memories Release Engineer | Zero-`regression` matrix + `tools/test-release.ps1` removal diff | pending | not complete |
| Rollback without artifact mutation | Platform Operations | `rollback-rehearsal.md` + `compare --expect identical` | pending | not complete |
| Rollback target refusal | Memories Maintainer | Runbook rule refusing v3.0.19–v3.0.25 and `46667b0d` | pending | not complete |
| Snapshot fixtures | Memories Maintainer | `release_cutover` suite incl. ambiguity and additive refusals | pending | not complete |

## File Scope

Allowed files for this story:

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

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

- **Why the trim:** a reviewed Markdown matrix carries the parity judgement. A validator would only check its shape. The snapshot tool is the one mechanism that is reused (rollback proof and next-release check).
- **Rollback cost:** v4.0.0 tooling predates the retained-byte bundle. A failed release after a rollback has no current-set recovery path, so the runbook must say so.

## Verification

**Commands:**
- `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v` -- expected: all methods pass, zero skips.
- `run-with-temp python3 -B -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py' -v` -- expected: 15/15.
- `python3 tools/release-identity-snapshot.py compare _bmad-output/implementation-artifacts/30-6-cutover/identity-before.json _bmad-output/implementation-artifacts/30-6-cutover/identity-after-rollback.json --expect identical` -- expected: exit 0.
- `run-with-temp actionlint .github/workflows/ci.yml` -- expected: clean.
- The isolated Debug build, then `DiffEngine_Disabled=true dotnet exec <build>/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: all cases pass.
- `run-with-temp python3 -B tools/check-story-slice-scope.py --story-key 30-6-release-cutover-parity-and-rollback --require-record` and `tools/check-story-file-scope.py --story-key spec-30-6-release-cutover-parity-and-rollback --changed-files-file <list>` -- expected: pass.
