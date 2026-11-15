---
title: 'Partial-release recovery'
type: 'feature'
created: '2026-11-15'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-5-partial-release-recovery'
baseline_commit: 'b468a8eb9dd9779af7cc26137d5bcc7c80d678ad'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** semantic-release pushes the `v*` tag before any NuGet or image push. A release that then fails part-way leaves an immutable tag with no completion path. The failed run keeps no prepared `.nupkg` or OCI bytes, and `recover-partial-release.yml` rebuilds from source. It accepts only historical two- or four-image, nine-package units and requires every package to already be on nuget.org.

**Approach:** Retain the failed run's prepared, hash-bound release set as immutable recovery evidence. A production-gated recovery proves source, version and inventory from that evidence, then classifies all fifteen members read-only. It publishes only the absent members from the retained bytes, verifies the end state, and then completes the GitHub Release.

## Boundaries & Constraints

**Always:**
- Refuse before any write unless all of these agree: the bundle receipt, every file hash, the frozen identity (`setSha256` recomputed), tag commit = identity source = failed-run head SHA, the version, and the tagged ten-package/five-image manifests.
- Write a complete fifteen-member plan before the first write. Each member is present-identical (skip), absent (publish), present-different (refuse), or ambiguous (refuse).
- Publish only retained bytes. Recheck each destination immediately before its write, re-verify all fifteen members afterwards, and run digest-bound health for every image that lacks a passing retained health record.
- Recovery runs only from the live `main` tip with successful exact-source push CI, after `environment: production` approval, with `cancel-in-progress: false`. Builds helpers load at the identity's recorded SHA and helper hashes.
- Historical trees (no `tools/release-containers.json`) keep the existing behavior, scoped to the tagged repositories and validated before any mutation.
- A rerun after a successful recovery performs zero writes.

**Never:** publish or recover a real release during implementation. Never change GitHub, nuget.org or registry settings, or dependency or Builds pins. Never rebuild a current-set member from source. Never delete, retag or overwrite any package, image, tag or release asset. Never count fixture success as live NuGet or registry proof. Never take Story 30.6 cutover, rollback or version-floor scope.

**Owner decisions (2026-11-15, Jérôme Piquot):**
- **D1 — NuGet authorization.** The owner will add the nuget.org Trusted Publishing policy `Hexalith`/`Hexalith.Memories`/`recover-partial-release.yml`/`production`. Recovery runs `NuGet/login@v1` (user `vars.NUGET_USER`) only when the plan has absent packages, immediately before the push, and never uses a stored key. Until the policy exists, a package recovery fails closed at login with a named blocker (owner Jérôme Piquot); image- and asset-only recoveries proceed. Adding the policy is an owner action outside implementation.
- **D2 — Builds approval (activation gate).** The owner approves Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa` and `ae77d5690a9411a2154fa4ed5231edc373942573` for recovery. Recovery accepts only the Builds SHA and helper hashes recorded in the failed run's identity, and only if that SHA is on this explicit list in the recovery tooling. Any other SHA refuses before any write. Adding a SHA requires a recorded owner approval and a list edit.
- **D3 — Scope.** Keep the full spec, about 5,700 tokens and above the 1,600 guideline, as one goal. Release-path `--skip-duplicate` and the publish-failed and GitHub-plugin incident alerts stay out of scope and move to Story 30.6.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Partial images | 10/10 packages and 3/5 images present-identical | Skip 13; publish 2 from retained OCI archives; health; complete Release | N/A |
| Partial packages | 4/10 packages present-identical | Push 6 retained nupkgs; continue with images and assets | N/A |
| Completed rerun | All 15 members and the Release are complete | Zero writes; success | N/A |
| Occupied by other content | Tag digest ≠ retained index digest, or nupkg entries differ | Refuse before any write | Name the member; never overwrite |
| Ambiguous destination | Probe returns 401/403/5xx, times out or errors; push returns 409 | No success credit | Fail closed; retain diagnostics |
| Evidence drift | Bundle missing or expired; receipt, hash, identity, source, version or inventory mismatch | Refuse before any write | Named reason |
| Draft Release | Draft whose assets are a hash-identical subset | Upload the missing assets, then publish the draft | Unexpected or different asset → refuse |
| Historical tag | Tagged tree has no contract manifest | Existing historical completion | Unsupported set refused before `-Push` |

</frozen-after-approval>

## Code Map

- `.github/workflows/recover-partial-release.yml` -- single job; no `environment` or `id-token`. Stages tools from an unproven `main` checkout (L62-71). Probes five fixed repositories before checking out the tag (L86-91). Rebuilds through legacy `publish-containers.ps1`, then gates 2/4 images and 9 packages (L140, L184-186). `initialize-build@main` and `initialize-dotnet@main` are required by the ref rule.
- `tools/complete-partial-release.ps1` -- the historical completion flow. Keep its tag/HEAD proof (L178-187), `id`/`packageId` shape handling (L221-269), `gh release` create/upload/download verification (L320-387), incident close (L389-413) and `finally` summary. Reuse `Protect-LogText` and `Invoke-NativeCommand`. Its NuGet download path (L276-307) stays historical-only.
- `.github/workflows/release.yml` -- failure steps (L203-235) upload only summaries (`release-publication-evidence`, 90 days); nupkgs upload on success only (L237-251). Copy the pinned QEMU/skopeo setup, `NuGet/login@v1` (L169-175), the protection readback (L160-167) and the `release-source` job pattern (L35-68, `tools/check-release-source.py`).
- `tools/pack-release.ps1` -- rebuilds `artifacts/packages/release` (L41-45), then runs the contract build (L75-78).
- `tools/container-publication-contract.py` -- `current_identity` and `freeze_identity` (L160-203) are bound to the release-run environment. Recovery verifies the frozen file offline instead. Reuse `load_shared` (L112), `archive_graph` and `validate_archives` (L217, L346), `materialize_oci_archive` (L376), and the probe, copy, validate and health steps of `publish_archives` (L396-443). Its recovery text is at L343.
- `tools/archive-release-evidence.py` -- success packet plus `--diagnostics`; neither contains nupkg or OCI bytes (L22-27).
- Builds `Github/publish-containers/publication_preflight.py` -- reuse `destination_probe` (L1022, raw status). Never call `validate_destination_absence`, `validate_container_absence` or `validate_version_floor`: they refuse occupied destinations by design. Reuse `oci_registry_validator.py` `validate_registry` (L485) and `--observe-created`. Do not adopt `execution_identity.py`.
- `tools/verify-container-registry.ps1` -- the `-Repositories` parameter (L7-12) scopes the probe. Reuse `tools/verify-container-health.py` unchanged.
- NuGet facts: `publish-nuget.ps1:165` uses `--skip-duplicate`, so the release's "pushed" is untrusted. Flat-container returns 404 while a package validates. nuget.org repository-signs packages, so served bytes differ from the retained nupkg.
- Pinned texts: `create-partial-publish-issue.ps1:282` (`publish_nuget_test.py` L475-482, L556-559), `publish-release.ps1:196` (`release_orchestration_test.py` L271-273), `publish-containers.ps1:54-60` plus contract L343 (`publish_containers_test.py` L905-927), and the `release.yml` L6-10 comment.
- Pins: `CiTestInventoryTests.cs` L588-627 (recovery strings, step order, and the forbidden `NUGET_API_KEY` and `dotnet nuget push`) and L1070-1102 (action refs). `builds_publication_qualification_test.py` L319-322 asserts the finding `recovery-workflow-superseded-set`, which `qualify-builds-publication.py` L1835-1850 stops emitting once the 2/4 and 9 gates disappear. Update the test, not the tool.
- Tag ranges: up to v2.15 has 2 images/9 packages; v2.16.0–v3.0.11 has 4/9; v3.0.12–v3.0.18 has 5/10 without the contract and stays refused. From v3.0.19 trees carry the contract, but only failures after this change will have a bundle.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/pack-release.ps1` -- after packing and before publication, write `artifacts/release/package-identity.json` (version, sourceSha, setSha256, and id/file/sha256/size per package). Refuse unless the ten IDs equal `release-packages.json` -- records pre-publication package bytes.
- [ ] `tools/archive-release-evidence.py` -- add `--recovery-bundle`. It collects the ten nupkgs, five OCI archives, the deployment yaml and the evidence records, and writes a `recovery-manifest.json` plus its SHA-256 receipt. Missing pre-publication inputs mark the bundle unrecoverable with a reason. Add package identity to the success packet.
- [ ] `.github/workflows/release.yml` -- on `failure()`, build the bundle and upload artifact `release-recovery-bundle` (90 days, compression-level 0). Update the L6-10 comment.
- [ ] `tools/container-publication-contract.py` -- add a read-only `recovery-plan` phase and a `recover` phase over a verified bundle. Both verify the frozen identity offline, load helpers at the identity SHA (D2 list), and classify images by authenticated manifest probe plus validator graph match. `recover` copies only absent members, each after an immediate recheck, then validates them and runs health.
- [ ] `tools/complete-partial-release.ps1` -- add a current-set mode (`-RecoveryBundle`, `-ReleaseRunId`). It verifies the bundle and its bindings, classifies packages (flat-container 200 → zip-entry comparison ignoring `.signature.p7s`; 404 → absent; anything else → ambiguous) and writes the plan, all before any write. It then publishes the missing images and pushes the missing nupkgs without `--skip-duplicate`. Finally it re-probes all 15 members, creates or completes the Release with 13 assets (retained nupkgs, deployment yaml, recovery evidence packet and receipt), verifies the downloads and closes the incident. Historical mode is unchanged except that it validates before any mutation.
- [ ] `.github/workflows/recover-partial-release.yml` -- inputs `version` and `release-run-id`. A read-only source job proves the `main` tip, exact push CI and the production readback. The `recover` job runs under `environment: production` with `id-token: write`. It stages proven tooling and checks the failed run (`release.yml`, head SHA = tag commit, failed conclusion). It downloads the artifact with a digest check, then branches: the current-set path does not rebuild and scopes the registry to the identity images; the historical path rebuilds with tagged-repository scope and validates before `-Push`. Always upload the recovery evidence.
- [ ] `tools/create-partial-publish-issue.ps1`, `tools/publish-release.ps1`, `tools/publish-containers.ps1`, `tools/container-publication-contract.py` -- point current-set guidance at Recover Partial Release, with the version and run ID.
- [ ] `tests/tooling/publish_containers/partial_release_recovery_test.py` (new), `partial_release_completion_test.py`, `publication_contract_test.py` -- cover every I/O row with fake registry, flat-container, gh, skopeo and dotnet; assert no write before the full plan; include positive controls and an idempotent rerun.
- [ ] `tests/tooling/publish_nuget/publish_nuget_test.py`, `tests/tooling/publish_containers/release_orchestration_test.py`, `tests/tooling/publish_containers/publish_containers_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py` -- realign the pins; the superseded-set finding is now asserted absent.
- [ ] `docs/dev/release-runbook.md` -- rewrite the recovery notes (L630-647) and L46-51/L147-149: procedure, prerequisites and blockers. Add no cutover or rollback text.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md` -- resolve each 30.5-owned entry whose trigger fired, with evidence. The release-path `--skip-duplicate` and publish-failed incident entries stay open and move to 30.6.

**Acceptance Criteria:**
- Given authorized recovery of a failed run, when it starts, then no registry, NuGet or GitHub write precedes a stored fifteen-member plan with zero refused members.
- Given a dispatch, when it is not the live `main` tip with green exact-source CI, or production approval is absent, then nothing runs past the source job.
- Given a recovery that writes its missing members, when it finishes, then all 15 members, the 13 assets and the packet share one source, version and `setSha256`, and historical fixtures keep passing.

## Checkpoint Evidence

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Exercised partial failure | Platform Operations (Jérôme Piquot) | `partial_release_recovery_test.py` partial-failure fixture plus retained bundle | pending | not complete |
| Source/version/inventory proof | Memories Maintainer (Jérôme Piquot) | Bundle identity and drift fixtures | pending | not complete |
| Already-published members skipped | Memories Maintainer (Jérôme Piquot) | Skip-existing fixture plan and zero-write rerun | pending | not complete |
| Missing members published once | Memories Maintainer (Jérôme Piquot) | Complete-set end-state fixture | pending | not complete |
| Ambiguous destination refusal | Memories Maintainer (Jérôme Piquot) | 401/503/timeout/409 negative fixtures | pending | not complete |

## File Scope

Allowed files for this story:

- `.github/workflows/recover-partial-release.yml`
- `.github/workflows/release.yml`
- `tools/complete-partial-release.ps1`
- `tools/pack-release.ps1`
- `tools/archive-release-evidence.py`
- `tools/container-publication-contract.py`
- `tools/create-partial-publish-issue.ps1`
- `tools/publish-release.ps1`
- `tools/publish-containers.ps1`
- `tests/tooling/publish_containers/*_test.py`
- `tests/tooling/publish_nuget/publish_nuget_test.py`
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Dev Notes

### Historical Context Classification

| Source | Classification | Permitted use |
| --- | --- | --- |
| Story 30.4 five-image contract | current-narrow-pattern | Reuse identity, OCI, validator and health functions; never its publish-all preflight. |
| Story 30.2 recovery qualification | historical-reference-only | Vocabulary (published/ambiguous/not-attempted) and handoff; synthetic, no publication credit. |
| Story 30.3 guarded dispatch | current-narrow-pattern | Source proof and protection readback pattern only. |
| Historical recovery (`13f83956`, `dc473cb5`, `578d005c`) | current-narrow-pattern | Historical path only. |
| Former Story 30.4 Partial-Release Recovery alias | anti-template | Identity only. |
| Story 30.6 cutover | historical-reference-only | Downstream boundary; no cutover, rollback or version floor. |

### Slice Proof

One outcome: evidence-driven, missing-only completion of a partial current release, with five separately owned checkpoints. Declared cross-story edits keep evidence retention (`release.yml`, `pack-release.ps1`, the archiver and the contract), guidance text and pins truthful. No other story's goal is implemented.

### Epic AC Verification

| Epic claim | Class | Command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| "Story 30.4 must complete" | behavioral | `sprint-status.yaml:525` | done | confirmed |
| "owner-approved Builds revision must emit qualified immutable evidence sufficient for partial-release recovery" | behavioral | `30-2-qualification/approval.md`; `release.yml:97` | Approved and qualified at `81c88b34`; the release runs `ae77d569` | corrected (D2: owner approved both for recovery) |
| "consumes immutable release evidence" | behavioral | `recover-partial-release.yml`; `release.yml:224-251` | Rebuilds from source; no bytes retained | refuted (implementation target) |
| "ten-package/five-image set" | quantitative | `tools/release-packages.json`, `tools/release-containers.json` | 10 IDs, 5 repositories | confirmed |
| "Owned paths: the recovery workflow and `complete-partial-release.ps1`" | location | Code Map | Evidence retention needs the declared cross-story edits | corrected |

### Discovery commands

PC: `python3 -B -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`. PN: the same with `-s tests/tooling/publish_nuget`; BQ: `-s tests/tooling/builds_publication_qualification`. Units are unittest methods counted by `unittest.defaultTestLoader.discover`. CT: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net11.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` (xUnit cases).

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-11-15 | create-story | Drafted the spec from the investigation; no code changed. | Baseline PC 111, PN 32, BQ 70 unittest methods; CT 69 methods / 141 cases; story +0, external +0. | Matched 1/1: this new spec. |

## File List

- `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

- Retain, don't rebuild: SDK container builds are not reproducible. A rebuilt member would not match the `archive-compatibility.json` index digests or package hashes, so "exact" could not be proven. Retained bytes keep `--preserve-digests` identity.
- Present NuGet identity: repository signing rewrites the zip. Compare every retained entry byte for byte and allow only an added `.signature.p7s`.
- A recovered Release keeps the normal 13 asset names. Its evidence packet is `artifactKind: partial-release-recovery`, embeds the bundle manifest, plan and results, and is bound to the same `setSha256`.
- Only failures after this change have a bundle. An earlier failed current tag refuses with a named blocker.

## Verification

**Commands:**
- PC, PN, BQ (Discovery commands) -- expected: all pass; any skip is named and gets no credit.
- `python3 -B -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py'` and `-s tests/tooling/ci_alignment` -- expected: unchanged passes.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj`, then CT -- expected: all cases pass.
- `python3 tools/check-story-file-scope.py --story-key 30-5-partial-release-recovery --changed-files-file <diff list>`; `python3 tools/check-story-slice-scope.py --story-key 30-5-partial-release-recovery --require-record`; `git diff --check` -- expected: exit 0.

**Manual checks:**
- Read-only real-boundary check (if authorized): for one v4.0.0 package, compare the GitHub Release asset with the bytes nuget.org serves, using the entry rule.
