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

### Review Findings

Code review 2026-11-16 (`/bmad-code-review 30.6`), range `fad5ec5efb286a1b4589869a354895acd3aaf55c..e1cded9ffc6f6cc13c956a900e7ad1065bfd4830` (111 paths). Six layers ran and none failed: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor, Historical Slice Guard and Story Phase Ledger Auditor. They read a 5,245-line core diff. Bulky raw captures were excluded by size only, and their hashes and comparisons were replayed by the parent. Decisions were resolved with the recommended option under the owner's standing review preference and are listed for veto.

- [x] [Review][Decision] Container rollback lane credited through isolated modules — Inside the D6 bracket, three full exact-v4.0.0 `publish_containers` discoveries each failed one different positive cancellation control (103/104). The cause is the documented v4.0.0 HTTP/1.0 204 fixture race, which `76de3ea4` fixes. Resolved (recommended, owner veto): AC2 is satisfied by the combined evidence.
  - The same target passed full discovery 104/104 in the 2026-11-15 rehearsal (`30-6-cutover/rollback-publish_containers.txt`).
  - All 104 IDs pass once in five fresh module processes.
  - The fallback reuse is now classified.
  - The runbook warns that a v4.0.0 rollback PR inherits this race in CI's full `publish_containers` step.
  - `rollback-d6-results.json` (`allReleaseLanesPass: false`) stays unchanged.
- [x] [Review][Decision] Rollback surface for a post-retirement rollback — `rollback-surface.txt` lists `tools/test-release.ps1` and the whole `CiTestInventoryTests.cs`. At HEAD, restoring that list from v4.0.0 would resurrect the retired runner (the v4.0.0 caller never invokes it) and revert 772 lines of shared inventory facts, including the D4 absence fact. Resolved (recommended, owner veto):
  - The runbook excludes the retired runner.
  - The runbook forbids a wholesale restore of `CiTestInventoryTests.cs`.
  - The C# inventory class becomes a required rollback lane, and the PR adjusts only the release-surface facts it reports.
  - The retained `rollback-surface.txt` stays unchanged as the 2026-11-15 rehearsal record.
- [x] [Review][Decision] Live recovery proof has no trigger — The Failure and Recovery rows rest on `static-source`, and D2's additive check observes only a successful release, so it can never exercise recovery. Resolved (recommended, owner veto): both rows cite the offline qualifier artifacts, and a `live-recovery` shared-gap row plus a ledger entry are added. Owner: Jérôme Piquot. Trigger: the first owner-authorized Recover Partial Release run.
- [x] [Review][Decision] D2 final-code live proof is tracked only in the runbook after `done` — Resolved (recommended, owner veto): the obligation stays in 30.6 as frozen D2 records it (accepted, not passed). A ledger entry with owner, consequence and reopen trigger is registered, and no new story is split.
- [x] [Review][Decision] Ownership of `30-6-cutover/coordinator-acceptance.md` — Resolved (recommended): dev-story close-out. It was written 2026-11-16 00:27, after `changed-files-d6.txt` (00:24) and before the first review fixes (00:46).
- [x] [Review][Decision] Status of the runbook obligation table — Resolved (recommended): treat it as story evidence, and copy the spec's accepted D2 disposition (owner, consequence, reopen trigger) into both obligation rows.
- [x] [Review][Patch] D4 retirement deleted the only real-register check of the structured `deferred-work.md` field contract (status vocabulary, `Evidence:` on resolved, `Rationale:` on accepted/carried-forward, required fields). It covers 7 live `27.21-C1.15-*` entries [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs]
- [x] [Review][Patch] Runbook rollback surface resurrects the retired runner, reverts shared C# inventory facts and runs no C# lane [docs/dev/release-runbook.md]
- [x] [Review][Patch] Additive and identical refusal tests accept any `ValueError`. Several checks are unreachable or uncovered: Latest moved elsewhere, pre-existing NuGet version/image tag, new-release tag/prerelease state, zero-size asset, unlisted new version, prerelease positive control, and well-formed identical-mode deltas [tests/tooling/release_cutover/release_identity_snapshot_test.py:209]
- [x] [Review][Patch] The registry pagination guard that keeps Basic credentials on the registry host is untested [tests/tooling/release_cutover/release_identity_snapshot_test.py:322]
- [x] [Review][Patch] Spec `done` disagrees with sprint `review`. The recorded readiness green used the `spec-` key, which skipped C4, and ran while `done`, which skipped C1 [spec frontmatter; 30-6-cutover/final-verification.json]
- [x] [Review][Patch] Change Log rows use non-canonical phase names, the dev-story row repeats already-recorded deltas, reconciliation cells are false or missing, and count cells lack commands and cumulative deltas [spec Change Log]
- [x] [Review][Patch] Evidence-table rows are pending, dateless or stale under `review`/`done` [30-6-cutover/parity-matrix.md; 30-6-cutover/rollback-rehearsal.md; docs/dev/release-runbook.md; spec Verification]
- [x] [Review][Patch] The CONTRIBUTING forbidden-default list no longer matches its named source of truth (`tools/check-story-file-scope.py:53`) [CONTRIBUTING.md:163]
- [x] [Review][Patch] `./tools/test.ps1` without a filter runs the whole solution, including Integration and Benchmark tests. That is not the retired lane's unit-contract set [CONTRIBUTING.md:670]
- [x] [Review][Patch] Rollback snapshots are written inside the disposable scratch worktree, and steps 1 and 4 give no exact capture command [docs/dev/release-runbook.md]
- [x] [Review][Patch] The fallback loop's `|| exit 1` skips the mandated cleanup-on-failure [docs/dev/release-runbook.md]
- [x] [Review][Patch] Next-release evidence goes to git-ignored `artifacts/`, outside the exact-byte `-text` rule [docs/dev/release-runbook.md]
- [x] [Review][Patch] No guidance for NuGet flat-container/registration lag at the after-capture [docs/dev/release-runbook.md]
- [x] [Review][Patch] The next semantic-release candidate is 4.1.0 (`feat` e201f169 follows v4.0.0), not 4.0.1. It is also absent from every domain [spec Checkpoint Evidence; 30-6-cutover/dry-run.md; docs/dev/release-runbook.md]
- [x] [Review][Patch] The F6 note miscounts the Story 30.5 delivered paths [_bmad-output/planning-artifacts/epics.md]
- [x] [Review][Patch] The D2 obligation is not tracked in the ledger [deferred-work.md]
- [x] [Review][Patch] The Failure and Recovery parity rows cite only static source, and there is no live-recovery shared gap [30-6-cutover/parity-matrix.md]
- [x] [Review][Patch] The Slice Proof says "one outcome" for a D5-approved checkpoint story, and the ledger/planning hand-offs have no checkpoint row [spec Slice Proof; Checkpoint Evidence]
- [x] [Review][Patch] The classification record omits the Story 14.5 parser, the 30.4 R6/30.5 F6 hand-offs and the 30.5 fallback. The documented slice-scope command also differs from the one that ran [spec Historical Context Classification; Verification]
- [x] [Review][Patch] The prior values for Controlled rehearsal and Complete-set dry run are not prior-caller values [30-6-cutover/parity-matrix.md]
- [x] [Review][Patch] The parity-regression I/O row is a D5 manual gate, but the task claims executed fixtures [spec review record]
- [x] [Review][Patch] The rollback procedure depends on the user-local `run-with-temp` wrapper [docs/dev/release-runbook.md]
- [x] [Review][Patch] The rollback procedure never re-captures identities after the PR merges and its push CI runs [docs/dev/release-runbook.md]
- [x] [Review][Patch] Ledger schema entries DW-4 and DW-11 still locate the retired release-filter pairing [deferred-work.md]

#### Rejected

- Blind: the rollback procedure lacks branch/commit/PR steps. `low`: the ordinary PR process applies, rollbacks are rare, and the fix adds procedure.
- Blind: the procedure hard-codes v4.0.0 values. `low`: the text says "currently" and requires re-establishing the target first, and the fix adds variables to a rare path.
- Blind, Edge, Verification Gap: the snapshot records no capture metadata and couples to the working-tree inventory with fixed 10/5 counts, `compare X X` passes, and timing uses mtimes. `low`: the counts are frozen Always text, a change fails loudly, and D5 keeps the tool small.
- Blind: additive accepts a stable release that did not become Latest. Rejected: the frozen I/O matrix specifies "Latest unchanged or moved to X".
- Blind: `retarget:` lines leave the original target and trigger text, `epic-30-retrospective` is optional, and version-floor lacks a status. Rejected: the Code Map mandates appended retarget lines and D4 is an owner decision. The status gap is pre-existing.
- Blind: `EvidenceCheckoutTests` inherits `GIT_*` variables. `low`: no hook runs it, and the worst case is loose objects and a temp index.
- Blind, Edge: `\w` accepts non-ASCII in tag and helper patterns. `false`: a non-ASCII tag fails request encoding and refuses as `ambiguous UnicodeEncodeError` naming the member, and capture never writes one.
- Blind: `qualification.txt` names a moved `qualifier/` path, and logs carry absolute local paths. `low`: raw tool output is retained evidence that the frozen intent forbids rewriting.
- Blind: the dry run is credited without noting the pending `approval.md`. `false`: `dry-run.md:36-38` states approval remains pending and grants no credit.
- Edge: a GitHub `next` link may skip pages. `false`: GitHub pagination is sequential, and every link is bound to the validated collection.
- Edge: non-object Docker config, an internal urllib `ValueError` and the null-reply reason text. `low`: each still refuses fail-closed; only the message is less specific.
- Edge: slow or oversized replies. `low`: trusted endpoints with 60-second socket timeouts.
- Edge: a short registry username redacts member names. `low`: cosmetic.
- Edge: the redirect test is proxied when `HTTP_PROXY` is set. `low`: CI sets no proxy.
- Edge: the fixed 20-character NuGet padding is host-dependent. `low`: it is a rollback-only workaround for one historical assertion, and computing it adds logic.
- Edge: the zero-skip rule is not counted mechanically. `low`: the requirement is explicit, and a fix adds tooling.
- Historical Slice: Story 30.3 should be `historical-reference-only`. `false`: its caller is the current source under proof and was re-verified.
- Acceptance: the removed-Latest-release case expects `identity/latest`. `false`: refusing at Latest validation, naming `identity/latest`, is the correct fail-closed result.
- Verification Gap: the agent memory note about `Evidence:` enforcement is stale. Not a repository defect; it becomes true again once the schema guard is restored.

## Checkpoint Evidence

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Dry-run parity | Memories Release Engineer | `30-6-cutover/dry-run.md`, `complete-set-dry-run.json`, `identity-before.json` | reviewed 2026-11-16 | complete; six clean-base offline checkpoints; candidate 4.0.1 absent in complete before-snapshot; the actual next candidate 4.1.0 is also absent (code review 2026-11-16) |
| Controlled rehearsal | Memories Release Engineer | v4.0.0 run `43432588932`, `30-6-cutover/live-runs.json`, `live-jobs.json`, `live-release-readback.json` (pre-30.5 code, D2) | reviewed 2026-11-16 | historical run/readback complete; no final-code live credit |
| Next-release additive verification | Jérôme Piquot | Runbook obligation: `compare --expect additive` on the next dispatch | accepted 2026-11-16 | not complete (D2 obligation); consequence: no final-code live publication credit; reopen trigger: next owner-authorized release followed by the additive comparison |
| Lane-by-lane comparison | Memories Release Engineer | `30-6-cutover/parity-matrix.md` and prior/current sources | reviewed 2026-11-16 | complete; zero regression verdicts |
| Prior publisher removal gate | Memories Release Engineer | `30-6-cutover/parity-matrix.md` removal record, `retirement.diff`, `ci-inventory-d6.txt` | reviewed 2026-11-16 | complete; old script/instructions/runner C# facts retired after identity proof; code review restored the live deferred-work schema guard the retirement had removed, C# 118/118 (`30-6-cutover/ci-inventory-code-review.txt`) |
| Rollback without artifact mutation | Platform Operations | `30-6-cutover/rollback-rehearsal.md` D6 addendum, `identity-d6-proof.json`, both `compare --expect identical` artifacts | reviewed 2026-11-16 | complete; all identities identical through the complete 104-ID isolated-module fallback; historical full-discovery flakes retained; AC2 container credit accepted in code review 2026-11-16 (2026-11-15 full discovery 104/104 plus isolated modules), recorded for owner veto |
| Rollback target refusal | Memories Maintainer | Runbook Release Rollback; `30-6-cutover/live-release-readback.json` | reviewed 2026-11-16 | documented; v4.0.0 full target set read back |
| Snapshot fixtures | Memories Maintainer | `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v`; `30-6-cutover/snapshot-fixtures-code-review.txt`, `code-review-mutation-check.txt` | reviewed 2026-11-16 | 34/34 methods pass; zero skips; refusal reasons pinned; 7/7 mutations caught |
| Ledger/planning hand-offs | Memories Maintainer | `deferred-work.md` retarget/resolution lines and the two 2026-11-16 code-review obligation entries; `epics.md` dated notes; parity rows | reviewed 2026-11-16 | complete; eight entries re-targeted (seven shared gaps plus the do-not-deploy owner action), five resolved, D2 and live-recovery obligations registered |

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
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-code-review.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6-build.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6-results.json`
- `_bmad-output/implementation-artifacts/30-6-cutover/ci-inventory-d6.txt`
- `_bmad-output/implementation-artifacts/30-6-cutover/code-review-mutation-check.txt`
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
- `_bmad-output/implementation-artifacts/30-6-cutover/snapshot-fixtures-code-review.txt`
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
| Story 14.5 deferred-work structured reader (`ParseStructuredDeferredEntries`) | current-narrow-pattern | Restored in code review from the baseline source, without the `Test:` capture, as the register's field-contract guard (DW-1..DW-6); the release-lane `Test:` pairing is not reused. |
| Story 30.4 R6 and Story 30.5 F6 review hand-offs | historical-reference-only | Inputs for the dated `epics.md` notes and ledger resolutions only. |
| Story 30.5 isolated-module fallback | current-narrow-pattern | One fresh process per complete module for the exact v4.0.0 container lane only. It supplements, and never replaces, the retained full-discovery result (code review decision 2026-11-16). |
| 2026-07-26 umbrella Story 30.1/30.5 text | anti-template | Wording origin only. Never import `domain-release.yml` adoption or the nine-package/four-image shape. |

### Slice Proof

D5-approved checkpoint story: the owner approved keeping one spec. Each outcome maps to its own checkpoint row, with an owner, evidence, review state and completion state:

- cutover parity → Lane-by-lane comparison, Dry-run parity, Controlled rehearsal;
- parity-gated removal → Prior publisher removal gate;
- mutation-free rollback → Rollback without artifact mutation, Rollback target refusal;
- read-only snapshot tool and its CI step → Snapshot fixtures;
- next-release obligation → Next-release additive verification;
- ledger and planning hand-offs → Ledger/planning hand-offs.

Hand-offs are recorded and re-targeted, not implemented (D4). Corrected in code review 2026-11-16; the earlier text read "One outcome".

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
| 2026-11-15 | dev-story | Implementation (pending gates; phase name canonicalised in code review 2026-11-16): read-only snapshot tool/fixtures/CI, matrix and evidence, clean-base qualifier, scratch rollback, runbook and dated hand-offs. Full snapshot and removal gates stay incomplete on the two recorded blockers. | New release_cutover 0 → 12 methods (12 pass), ci_alignment 15 → 15 (15 pass), CT baseline 141 → 141 (141 pass; no C# edit), no fixture credit represented as live proof. Rollback historical lanes recorded independently. Phase delta: release_cutover +12 (0 → 12 methods; new scope), ci_alignment +0, CT +0 cases; cumulative: release_cutover +12, CT +0. Command: `python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py'` (12-method runner output not retained); CT baseline `30-6-cutover/ci-inventory-baseline.txt`. | Allowed implementation/doc/evidence paths only; old script/instructions/C# facts retained because retirement is gated. Phase-local set `30-6-cutover/changed-files.txt` (46 entries) against baseline `fad5ec5e`; no `matched N/N` was recorded in this phase. |
| 2026-11-15 | dev-story | Acceptance hardening (phase name canonicalised in code review 2026-11-16): require every GitHub asset SHA-256 digest; refuse missing/null digests and malformed releases/latest shapes with named member diagnostics before any snapshot write. Strict historical-index policy and pending gates remain unchanged. | release_cutover 12 → 14 methods (+2); 14/14 pass, zero skips. Exact output: `30-6-cutover/snapshot-fixtures-acceptance-hardening.txt`. No live probes or mutations in this correction. Cumulative release_cutover +14; same command. | Only snapshot tool/fixtures and their evidence/notes changed. Phase-local scope output `30-6-cutover/acceptance-hardening-scope.txt`, baseline `fad5ec5e`; no `matched N/N` recorded. |
| 2026-11-15 | dev-story | Rehearsal environment follow-up (phase name canonicalised in code review 2026-11-16): exact v4.0.0 NuGet method and full lane pass with a twenty-character child directory beneath the wrapper TMPDIR; no source/test edits or output normalization. Original failed evidence remains unchanged. Read-only audit finds valid SHA-256 digests on all 1,700 assets across 183 Releases. | Phase delta +0 for every story unit (release_cutover 14, cumulative +14). Historical v4.0.0 NuGet 32/32 is an outside-story rollback lane, not a story discovery unit. The full snapshot/removal gate remains pending only on the historical-index policy. | Added supplemental padding/audit/final-result evidence and updated current summaries; no live mutations. Phase-local scope output `30-6-cutover/acceptance-final-scope.txt`; no `matched N/N` recorded. |
| 2026-11-16 | dev-story | D6 completion and parity-gated retirement (phase name canonicalised in code review 2026-11-16): historical capture records recognized manifests/indexes with exact digest/media type. Full before/after plus supplemental final-fallback snapshots are identical; exact rollback modules qualify all 104 IDs after retained historical discovery flakes. Zero-regression coordinator parity verification gates old-runner/instructions/dead-fact retirement. Final workflow review remains pending. | release_cutover 14 → 16: +3 added, −1 removed (`test_registry_pagination_and_bad_digest_or_non_index_refusal`), net +2, cumulative +16; 16 pass (`30-6-cutover/snapshot-fixtures-d6.txt`). ci_alignment 15 pass (+0). C# inventory 141 → 101 cases: 23 methods / 41 cases removed and 1 case added (`RetiredReleaseTestScript_ShouldRemainAbsent`), net −40, cumulative −40. 101 pass after an isolated Debug/source build, zero skips/warnings/errors; argv in `30-6-cutover/ci-inventory-d6-results.json`. The historical container set 104/104 in isolated modules is an outside-story rollback lane; original failed discoveries and collector correction retained. | D6 tool/fixtures, approved retirement paths, current reports/checkpoints and new evidence only. Phase-local set `30-6-cutover/changed-files-d6.txt` (95 entries), baseline `fad5ec5e`. |
| 2026-11-16 | dev-story | Close-out of D6 exact historical manifest/index capture, complete identity bracket and parity-gated retirement. Counts corrected in code review 2026-11-16: this summary repeats no delta. | Phase delta +0 for every unit. Cumulative: release_cutover +16 (16 methods), ci_alignment +0 (15), release_dispatch +0 (17), CT −40 cases (101). Historical container set 104/104 in isolated modules is outside story units. | matched 96/96 against baseline `fad5ec5e`: `30-6-cutover/changed-files-d6.txt` (95) plus `30-6-cutover/coordinator-acceptance.md` (dev-story close-out, code-review decision). Corrected in code review 2026-11-16; the cell first read 111/111. |
| 2026-11-16 | code-review | Three independent reviewers plus final live verification; direct guard/coverage/doc fixes, one uncommon-low finding rejected and no new deferrals. | Snapshot 16 → 29 (+13; cumulative +29), 29/29 pass (`30-6-cutover/snapshot-fixtures-numeric-links.txt`); CI 15/15 (+0); CT +0 cases (101, cumulative −40; the D6 run, not a post-patch rerun); post-review full live capture and identical comparison exit 0. | matched 111/111 against `git diff --name-status fad5ec5e..e1cded9f` and `30-6-cutover/final-changed-files.txt`, baseline `fad5ec5efb286a1b4589869a354895acd3aaf55c`. |
| 2026-11-16 | code-review | `/bmad-code-review 30.6`. Six layers; 6 decisions resolved (recommended, listed for owner veto), 24 patches applied, 19 rejected, 0 deferred. Restored the deferred-work schema guard. Pinned snapshot refusal reasons and added the missing additive, identical and registry-pagination cases. Corrected the runbook rollback surface, evidence paths, cleanup, wrapper and post-merge check. Repaired the ledger, checkpoint and evidence-table records. Added the D2 and live-recovery obligation entries. | Phase delta: release_cutover +5 methods (29 → 34); CT +17 cases (101 → 118: `DeferredWorkRegister_RealRepo_StructuredEntriesSatisfyFieldContract` plus 16 `ParseStructuredDeferredEntries_*` cases); ci_alignment +0 (15); release_dispatch +0 (17). Cumulative from the create baseline: release_cutover +34 (0 → 34 methods), CT −23 cases (141 → 118), ci_alignment +0, release_dispatch +0. No external same-lane delta. Commands: `python3 -B -m unittest discover -s tests/tooling/release_cutover -p '*_test.py' -v` → `30-6-cutover/snapshot-fixtures-code-review.txt` (34/34, zero skips). Isolated Debug build, then `DiffEngine_Disabled=true dotnet exec <build>/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` → `30-6-cutover/ci-inventory-code-review.txt` (118/118; the DLL carries the new literal). CT 141 re-derived in a scratch worktree at `fad5ec5e` and 101 at `e1cded9f` before patching. ci_alignment/release_dispatch counted with the create-story `countTestCases()` command and run with discover. | matched 114/114 against baseline `fad5ec5efb286a1b4589869a354895acd3aaf55c`: `git diff --name-status fad5ec5e..e1cded9f` (111 paths) plus the uncommitted review set (`git diff --name-only fad5ec5e` and `git ls-files --others --exclude-standard`), which adds `30-6-cutover/ci-inventory-code-review.txt`, `code-review-mutation-check.txt` and `snapshot-fixtures-code-review.txt`. |

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

Observed D6 completion checks (historical; current counts are in the 2026-11-16 code-review ledger rows): snapshot fixtures 16/16, ci_alignment 15/15, zero skips; changed
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
- `run-with-temp python3 -B tools/check-story-slice-scope.py --story-key 30-6-release-cutover-parity-and-rollback --changed-files-file <list> --require-record` (the bare `--story-key` form passes vacuously) and `tools/check-story-file-scope.py --story-key spec-30-6-release-cutover-parity-and-rollback --changed-files-file <list>` -- expected: pass.

## Final Verification — 2026-11-16

Snapshot fixtures pass 29/29, CI alignment 15/15, and changed CI actionlint is clean. The
Debug/source C# inventory remains 101/101 with zero build warnings/errors. Both original D6
comparisons and the new guarded live capture compare identically. All four complete snapshot
JSON byte streams have SHA-256 `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
`30-6-cutover/post-review-capture-verified.json` binds the final capture to its exact tool hash;
the earlier over-restricted pagination refusal remains preserved. Git checkout now preserves
all exact-byte evidence. `30-6-cutover/final-verification.json` records the final checks.

## Code Review Record — 2026-11-16

- **Range:** `fad5ec5efb286a1b4589869a354895acd3aaf55c..e1cded9ffc6f6cc13c956a900e7ad1065bfd4830`, plus the review patch set above. HEAD did not move during the review; the baseline is an ancestor. The review is not chunked.
- **Readiness gate (frontmatter key):** `python3 -B tools/check-story-review-readiness.py --story-key 30-6-release-cutover-parity-and-rollback --changed-files-file <raw 114-path cumulative set>`, run while the status read `review`.
  - C1 printed "File List exactly matches all 114 in-scope changed paths".
  - Final line: `Story review readiness validation passed.` (exit 0).
  - At review start, the same key exited 1 with `C4: story declares status 'done' but sprint-status records 'review'.`
  - The earlier recorded green run (`30-6-cutover/final-scope.txt:137-145`) used the `spec-` key while the status read `done`, so it skipped both C4 and C1. It is superseded and is not evidence.
- **Scope gates:**
  - `python3 -B tools/check-story-slice-scope.py --story-key 30-6-release-cutover-parity-and-rollback --changed-files-file <set> --require-record` printed `story-slice-scope: OK - 1 story file(s) checked`.
  - `python3 -B tools/check-story-file-scope.py --story-key spec-30-6-release-cutover-parity-and-rollback --changed-files-file <set>` printed `Story file scope validation passed.`
- **Identity replay:**
  - Each `compare` of `identity-before.json` against the rollback, module-fallback and post-review snapshots, with `--expect identical`, exits 0.
  - All four JSON files hash to `68165b901643f2005f450474c0cf7ba3710ced1c0d5c7da51cd2b3ba486d2158`.
  - The tool is unchanged by the review (`6bd754f0…`, the same hash the post-review capture recorded).
- **Remediation runtime checklist:**
  - Categories 1–3 do not apply: no Dapr workflow, activity registration, or tenant-scoped cleanup or dedup is touched.
  - Category 4 applies to the release rollback. Published identities are preserved: three identical comparisons, and nothing was deleted, retagged or overwritten. Scratch staging worktrees are removed on success and on failure: `temporaryWorktreesRemoved: true` in `rollback-d6-results.json` and in the failed full-discovery `rollback-d6-containers-rerun-results.json` and `rollback-d6-containers-affinity-results.json`.
  - This review found a defect of the same class: the runbook fallback loop exited before cleanup. It is fixed and recorded as a checklist item on this story. It is not a new category.
  - Category 5: `matched 114/114`.
- **Epic AC Verification audit:** the commands behind every `corrected` row were re-run read-only.
  - `git tag --contains 8aee7a13` → v3.0.18 first.
  - `git show 46667b0d:.github/workflows/release.yml` → `on: push` to main, no `HEXALITH_RELEASE_PUBLISH_ENABLED`.
  - `git show 46667b0d:.releaserc.json` → `*.nupkg` plus the production YAML (11).
  - `gh run view 43432588932` → `workflow_dispatch`, `success`, head `c4ee9977…`.
  - `gh release view v4.0.0 --json assets` → 13.
  - The runbook now carries both headings.
  - Each corrected row has its dated `epics.md` note (D1–D5). No row was weakened.
- **I/O matrix "Parity regression" row:** this row is a D5 manual gate, not a snapshot-tool behavior. Its evidence is the reviewed matrix with zero `regression` rows, which precedes `retirement.diff`. The fixture claim in the task applies to the other four rows.
- **Decisions taken for owner veto:**
  1. Accept AC2 container credit from the 2026-11-15 full discovery (104/104) plus the isolated modules.
  2. Exclude the retired runner from post-retirement rollbacks and reconcile `CiTestInventoryTests.cs` instead of restoring it wholesale.
  3. Register the live-recovery shared gap with its trigger.
  4. Keep the D2 final-code proof in 30.6 as an accepted obligation with a ledger entry.
  5. Assign `coordinator-acceptance.md` to the dev-story close-out.
  6. Treat the runbook obligation rows as story evidence.
