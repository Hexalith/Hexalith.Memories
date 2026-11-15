---
title: 'Partial-release recovery'
type: 'feature'
created: '2026-11-15'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
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
- [x] `tools/pack-release.ps1` -- after packing and before publication, write `artifacts/release/package-identity.json` (version, sourceSha, setSha256, and id/file/sha256/size per package). Refuse unless the ten IDs equal `release-packages.json` -- records pre-publication package bytes.
- [x] `tools/archive-release-evidence.py` -- add `--recovery-bundle`. It collects the ten nupkgs, five OCI archives, the deployment yaml and the evidence records, and writes a `recovery-manifest.json` plus its SHA-256 receipt. Missing pre-publication inputs mark the bundle unrecoverable with a reason. Add package identity to the success packet.
- [x] `.github/workflows/release.yml` -- on `failure()`, build the bundle and upload artifact `release-recovery-bundle` (90 days, compression-level 0). Update the L6-10 comment.
- [x] `tools/container-publication-contract.py` -- add a read-only `recovery-plan` phase and a `recover` phase over a verified bundle. Both verify the frozen identity offline, load helpers at the identity SHA (D2 list), and classify images by authenticated manifest probe plus validator graph match. `recover` copies only absent members, each after an immediate recheck, then validates them and runs health.
- [x] `tools/complete-partial-release.ps1` -- add a current-set mode (`-RecoveryBundle`, `-ReleaseRunId`). It verifies the bundle and its bindings, classifies packages (flat-container 200 → zip-entry comparison ignoring `.signature.p7s`; 404 → absent; anything else → ambiguous) and writes the plan, all before any write. It then publishes the missing images and pushes the missing nupkgs without `--skip-duplicate`. Finally it re-probes all 15 members, creates or completes the Release with 13 assets (retained nupkgs, deployment yaml, recovery evidence packet and receipt), verifies the downloads and closes the incident. Historical mode is unchanged except that it validates before any mutation.
- [x] `.github/workflows/recover-partial-release.yml` -- inputs `version` and `release-run-id`. A read-only source job proves the `main` tip, exact push CI and the production readback. The `recover` job runs under `environment: production` with `id-token: write`. It stages proven tooling and checks the failed run (`release.yml`, head SHA = tag commit, failed conclusion). It downloads the artifact with a digest check, then branches: the current-set path does not rebuild and scopes the registry to the identity images; the historical path rebuilds with tagged-repository scope and validates before `-Push`. Always upload the recovery evidence.
- [x] `tools/create-partial-publish-issue.ps1`, `tools/publish-release.ps1`, `tools/publish-containers.ps1`, `tools/container-publication-contract.py` -- point current-set guidance at Recover Partial Release, with the version and run ID.
- [x] `tests/tooling/publish_containers/partial_release_recovery_test.py` (new), `partial_release_completion_test.py`, `publication_contract_test.py` -- cover every I/O row with fake registry, flat-container, gh, skopeo and dotnet; assert no write before the full plan; include positive controls and an idempotent rerun.
- [x] `tests/tooling/publish_nuget/publish_nuget_test.py`, `tests/tooling/publish_containers/release_orchestration_test.py`, `tests/tooling/publish_containers/publish_containers_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py` -- realign the pins; the superseded-set finding is now asserted absent.
- [x] `docs/dev/release-runbook.md` -- rewrite the recovery notes (L630-647) and L46-51/L147-149: procedure, prerequisites and blockers. Add no cutover or rollback text.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- resolve each 30.5-owned entry whose trigger fired, with evidence. The release-path `--skip-duplicate` and publish-failed incident entries stay open and move to 30.6.

**Acceptance Criteria:**
- Given authorized recovery of a failed run, when it starts, then no registry, NuGet or GitHub write precedes a stored fifteen-member plan with zero refused members.
- Given a dispatch, when it is not the live `main` tip with green exact-source CI, or production approval is absent, then nothing runs past the source job.
- Given a recovery that writes its missing members, when it finishes, then all 15 members, the 13 assets and the packet share one source, version and `setSha256`, and historical fixtures keep passing.

### Review Findings

Code review 2026-11-15 (Claude Opus 5.5, full mode) of `b468a8eb..2e5e71ad` across six layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor, Historical Slice Guard and Story Phase Ledger. No layer failed. During the review a concurrent `/pushall` rebased `main` onto `origin/main` (16:20:58). The story commits became `56d93d6f`/`e201f169` on top of `7a5c8925` (an unrelated ingestion-erasure commit), and `git diff 2d0acd71..e201f169` is byte-identical to the reviewed snapshot. The gitlink-only `/pushall` sync (`e542613c`, now `10e8cbee`) is outside the story range. Severity is the triage verdict, not the layer's grade.

- [ ] [Review][Patch] Historical tag-set detection uses project existence, not the tagged published set (high; decision resolved 2026-11-15 by Administrator: option (a), derive the tagged set) [.github/workflows/recover-partial-release.yml:258] — The `Validate historical inventory before any mutation` step and the staged `publish-containers.ps1:109-110` count an image for every `src/<project>/<project>.csproj` that exists. Re-derived per tag with `git show <tag>:…`: v2.11.0–v2.15.x contain the AccessTelemetry projects, but their tagged publisher shipped only server/mcp. The gate passes 4, write-scope covers 4, and `-Push` would create AccessTelemetry tags those releases never had. Those immutable writes cannot be removed, and completion then refuses. v3.0.0–v3.0.11 contain a DomainService project, but the tag shipped 4, so the gate refuses every one of them. Both contradict the Code Map ranges (up to v2.15 2/9; v2.16.0–v3.0.11 4/9). They also make two Story 30.5 deferred-work resolutions and the runbook historical bullet overstate the scope. Options: (a) derive the tagged set from the tagged publisher's image candidates, read as data and never executed, and restrict the staged publisher to it with an explicit repository filter, with executed v2.11-, v2.16- and v3.0-shaped gate fixtures; (b) refuse before any mutation every historical tree whose project-derived set differs from its tagged publisher's set, with a named reason, narrow the runbook/DW claims to the supported ranges, and defer full support.
- [ ] [Review][Patch] Registry and GitHub write credentials are job-wide and the write token persists in `.git/config` (high) [.github/workflows/recover-partial-release.yml:69]
- [ ] [Review][Patch] Workflow-only `recovery-metadata` and `recover-images` phases are never executed by a test (medium) [tools/container-publication-contract.py:972]
- [ ] [Review][Patch] Producer path is never executed: `--recovery-bundle` CLI, `release.yml` retention/upload steps and the workflow download script; "Exercised partial failure" has no publish-run-failure→bundle→recovery fixture (medium) [tools/archive-release-evidence.py:305]
- [ ] [Review][Patch] Inline workflow gates are pinned only as text and fail without named reasons: production readback, failed-run check, artifact selection/digest (medium) [.github/workflows/recover-partial-release.yml:98]
- [ ] [Review][Patch] Identity-drift fixture never reaches `verify_bundle` bindings: all five mutations stop at the producer's unrecoverable receipt (probe-confirmed) (medium) [tests/tooling/publish_containers/partial_release_recovery_test.py:678]
- [ ] [Review][Patch] Current-mode incident comment/close, multiple-match and limit refusals have no assertion (medium) [tools/container-publication-contract.py:1107]
- [ ] [Review][Patch] Fake flat-container routes only on the first path segment, so URL regressions pass (medium) [tests/tooling/publish_containers/partial_release_recovery_test.py:43]
- [ ] [Review][Patch] PARTIAL PUBLISH incident body renders a blank version: `$version` is never assigned (pwsh probe-confirmed) (medium) [tools/create-partial-publish-issue.ps1:282]
- [ ] [Review][Patch] Recovery guidance tests never assert the rendered version and run ID (medium) [tests/tooling/publish_nuget/publish_nuget_test.py:557]
- [ ] [Review][Patch] Every package recovery fails its first run: post-push flat-container 404 during NuGet validation refuses final verification, and a rerun inside the window gets 409 (medium) [tools/container-publication-contract.py:1053]
- [ ] [Review][Patch] Re-running a failed `release.yml` run permanently refuses its bundle (frozen attempt binding), and no guidance warns against it (medium) [docs/dev/release-runbook.md:630]
- [ ] [Review][Patch] Recovery evidence upload re-uploads `bundle.zip` and the extracted bundle (multi-GB, 90 days) (medium) [.github/workflows/recover-partial-release.yml:301]
- [ ] [Review][Patch] Publishing a recovered draft can mark an older version as the repository's Latest release (medium) [tools/container-publication-contract.py:1103]
- [ ] [Review][Patch] Renderer byte comparison runs the dispatching `main` kubectl pin, not the failed run's (medium) [.github/workflows/recover-partial-release.yml:25]
- [ ] [Review][Patch] CI inventory pins dropped while still true: `ShouldNotContain("semantic-release")`, scope-before-restore, reconcile-before-complete; plan-before-image-write is unpinned (low) [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:610]
- [ ] [Review][Patch] `release-source` job has no `timeout-minutes` while holding the `release` concurrency group (low) [.github/workflows/recover-partial-release.yml:30]
- [ ] [Review][Patch] An unobservable draft after `release create` proceeds to uploads, then fails with `TypeError` (low) [tools/container-publication-contract.py:1090]
- [ ] [Review][Patch] Runbook: dangling "manual NuGet reconciliation, defined below"; removed do-not-rerun/never-push-locally warnings; no next action per blocker (medium) [docs/dev/release-runbook.md:595]
- [ ] [Review][Patch] Deferred-work inaccuracies: "90 days" ignores the recorded 30-day cap; the atomicity entry lacks status, consequence and reopen trigger; the D3 alert entry duplicates Round 3 Blind 8 (medium) [_bmad-output/implementation-artifacts/deferred-work.md:6070]
- [ ] [Review][Patch] No blocker record for the D1 Trusted Publishing prerequisite or the unverified nuget.org signing-entry rule the fixtures encode (medium) [_bmad-output/implementation-artifacts/deferred-work.md:6233]
- [ ] [Review][Patch] Story reads `done` while sprint-status reads `review`; `check-story-review-readiness.py --story-key 30-5-partial-release-recovery` exits 1 (C4) and no invocation is recorded (high) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:5]
- [ ] [Review][Patch] `create-story` ledger row was rewritten in place (Change cell, unit and separator differ from `1a70448b`) (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:155]
- [ ] [Review][Patch] One `code-review` row folds review R1, the dev re-derivation (PC 130→140) and review R2 (140→149) (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:157]
- [ ] [Review][Patch] Test-count cells lack before/after, cumulative, +0 lanes and commands; the replaced `test_new_catalog_unit_completes_with_ten_packages_and_domain_service` (+39/−1) is unrecorded (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:156]
- [ ] [Review][Patch] File List reconciliation cells name "allowed paths" without the compared baseline or name-status command (low) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:156]
- [ ] [Review][Patch] Verification still calls revision-1 totals (28/28, PC 140) "final" (low) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:268]
- [ ] [Review][Patch] Checkpoint/Verification cite git-ignored `artifacts/30-5-recovery-fixture/`, whose packet lacks the identity records the final code requires (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:92]
- [ ] [Review][Patch] Checkpoint table lacks rows for the other independently verifiable gates (AC2 source/production, AC3 assets and incident, historical pre-mutation, zero-write rerun, D1, D2, producer) (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:90]
- [ ] [Review][Patch] Historical Context Classification omits the pre-split umbrella anti-template and the 30.3/30.4 hand-offs, and mislabels the historical commits (low) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:124]
- [ ] [Review][Patch] Epic AC verdict "refuted (implementation target)" is outside the policy vocabulary; an unmet end state is a `confirmed` gap (low) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:143]
- [ ] [Review][Patch] Remediation runtime checklist applicability is unstated; Category 3 (owner-checked compensation/dedup of shared registry, NuGet and Release state) applies (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:137]
- [ ] [Review][Patch] `baseline_commit` `b468a8eb` is no longer reachable from `main` after the concurrent rebase; rebind it to the rebased equivalent `2d0acd71` (the story range is proven byte-identical), or every `baseline..HEAD` gate absorbs `7a5c8925` (medium) [_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md:9]
- [ ] [Review][Patch] `test_summary_root_must_be_an_object` fails in a colour terminal (pwsh ANSI codes split the message; also at baseline) (low) [tests/tooling/publish_nuget/publish_nuget_test.py:883]
- [x] [Review][Defer] `epics.md` Story 30.5 carries no dated correction for its `corrected` owned-paths and activation-gate claims [_bmad-output/planning-artifacts/epics.md:5832] — deferred: the fix edits another planning artifact outside File Scope; recorded in deferred-work.md with owner and trigger.
- [x] [Review][Defer] `publish-nuget.ps1` still prints manual-reconciliation guidance and `publish_nuget_test.py:472-482` pins it, contradicting the new aggregate guidance [tools/publish-nuget.ps1:45] — deferred: outside Story 30.5 File Scope; joins the D3 Story 30.6 `publish-nuget.ps1` hand-off.

#### Rejected

- `false` — Producer edits are a hidden second slice (Historical Slice Guard F1): Tasks L71-73 name them explicitly, and owner decision D3 keeps the full spec as one goal. The missing checkpoint row is the separate checkpoint-table patch.
- `false` — Bundle may include private auth config, and a symlink loses the bundle (Blind Hunter, Edge Case Hunter): registry auth is written only to private temp directories (`container-publication-contract.py:425`, `:739`), and no producer writes symlinks into the evidence directory.
- `low` — Historical recovery now needs a failed `release.yml` run: Task L76 mandates the failed-run check before branching; cancelled historical runs are rare, and a fix adds branching.
- `low` — Mixed recovery writes images before NuGet login can fail: D1 places login "immediately before the push"; reruns are idempotent.
- `low` — `--generate-notes` notes differ from semantic-release notes: cosmetic.
- `low` — Bundle re-extracted and re-hashed in each of four phases: efficiency only; a cache adds state.
- `low` — `calls == 6` magic in the immediate-recheck test: developer-only fragility.
- `low` — Bundle retained on failures before a tag exists: diagnostic value; gating adds conditions.
- `low` — Catalog-adapter DomainService guard unreachable in historical mode: dead defensive code, no user harm.
- `low` — Non-default `-SummaryDirectory` misplaces `package-identity.json`: `release.yml` uses the default; the fix needs parameter plumbing.
- `low` — `--nuget-base-uri` override probes one feed and pushes to nuget.org: a test-only override.
- `low` — Changes to main's inventory or `CANONICAL_IMAGES` refuse older bundles: a deliberate trusted-tooling pin; it fails closed and needs an inventory change first.

## Checkpoint Evidence

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Exercised partial failure | Platform Operations (Jérôme Piquot) | `test_copy_failure_after_manifest_put_is_reconciled_by_read_only_graph_on_rerun`; retained fixture bundle at `artifacts/30-5-recovery-fixture/` | Verified 2026-11-15 (offline fixtures) | fixture passed; no live credit |
| Source/version/inventory proof | Memories Maintainer (Jérôme Piquot) | Bundle identity, every-file-hash and helper-hash drift fixtures | Verified 2026-11-15 (offline fixtures) | fixture passed; no live credit |
| Already-published members skipped | Memories Maintainer (Jérôme Piquot) | `test_partial_images_copy_only_two_retained_archives_and_completed_rerun_writes_nothing` | Verified 2026-11-15 (offline fixtures) | fixture passed; no live credit |
| Missing members published once | Memories Maintainer (Jérôme Piquot) | `test_all_missing_positive_control_verifies_fifteen_and_thirteen_assets`, six-missing-package fixture | Verified 2026-11-15 (offline fixtures) | fixture passed; no live credit |
| Ambiguous destination refusal | Memories Maintainer (Jérôme Piquot) | 302/401/403/503/timeout, HTTP 409 and pending-flat-container fixtures | Verified 2026-11-15 (offline fixtures) | fixture passed; no live credit |

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
| --- | --- | --- | --- | --- |
| 2026-11-15 | create-story | Drafted approved recovery intent, inventory and owner decisions. | Baseline PC 111, PN 32, BQ 70; CT 69 methods / 141 cases; story +0, external +0. | Matched 1/1: initial spec. |
| 2026-11-15 | dev-story | Implemented retained-byte recovery, gated workflow, package identity, complete plans, missing-only publication and evidence. | PC 130 (+19 recovery); PN 32, BQ 70, CT 141 cases; no external test additions. | Matched 21/21 allowed paths. |
| 2026-11-15 | code-review | Two rounds across three independent layers; logged 29 findings individually, re-derived non-frozen implementation once and applied final local corrections. Registry atomicity remains an accountable external-evidence deferral. | Final inventory PC 149 (+38 from baseline: 37 recovery and 1 producer), PN 32, BQ 70, dispatch 17, alignment 15; CT 141 cases. Final results recorded under Verification. | Matched 21/21 allowed paths, including tracking, runbook and deferred ledger. |
| 2026-11-15 | code-review | Round 3, an independent re-review by Claude Opus 5.5: six layers over the full `b468a8eb..2e5e71ad` diff, unchunked (single invocation; snapshot byte-identical to that range). Raised 1 decision, resolved by Administrator as option (a) and converted to a patch. A concurrent rebase found after triage added 1 more patch: 34 patch action items left open by owner choice, 2 deferred to deferred-work.md, 12 rejected. No code patch applied; only Review Findings, this row, status and two deferred entries were written. Readiness gate before the status sync: `python3 tools/check-story-review-readiness.py --story-key 30-5-partial-release-recovery --changed-files-file <21 story paths>` exited 1, `C4: story declares status 'done' but sprint-status records 'review'`. | Review-patch phase delta +0 in every lane; cumulative story delta from the create baseline; external +0. PC (`python3 -B -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`, unittest methods): 149 → 149, cumulative +38 (+39/−1 vs baseline 111), 149/149 passed. PN (same, `-s tests/tooling/publish_nuget`): 32 → 32, cumulative +0, 32/32 passed with `TERM=dumb` (6 subtests fail in a colour terminal, also at baseline; patch item). BQ: 70 → 70, cumulative +0, passed. release_dispatch: 17 → 17, cumulative +0, passed. ci_alignment: 15 → 15, cumulative +0, passed. CT (isolated `dotnet build … --artifacts-path "$TMPDIR/build" -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1`, then `DiffEngine_Disabled=true dotnet exec … -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`, xUnit cases): 141 → 141, cumulative +0, 141/141 passed, 0 warnings/errors. All runs under `run-with-temp` with `~/.dotnet/tools` (pwsh 7.6.2) on PATH. | Matched 21/21 changed paths vs `git diff --name-status b468a8eb..2e5e71ad` (2 A, 19 M); no exclusions. After the concurrent 16:20:58 rebase, the same 21 paths and the byte-identical diff are `2d0acd71..e201f169`. `7a5c8925` (ingestion erasure) and `10e8cbee` (gitlink-only `/pushall` sync, Jérôme Piquot) are outside the story range and not absorbed. |

## File List

- `.github/workflows/recover-partial-release.yml`
- `.github/workflows/release.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/release-runbook.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`
- `tests/tooling/publish_containers/partial_release_completion_test.py`
- `tests/tooling/publish_containers/partial_release_recovery_test.py`
- `tests/tooling/publish_containers/publication_contract_test.py`
- `tests/tooling/publish_containers/publish_containers_test.py`
- `tests/tooling/publish_containers/release_orchestration_test.py`
- `tests/tooling/publish_nuget/publish_nuget_test.py`
- `tools/archive-release-evidence.py`
- `tools/complete-partial-release.ps1`
- `tools/container-publication-contract.py`
- `tools/create-partial-publish-issue.ps1`
- `tools/pack-release.ps1`
- `tools/publish-containers.ps1`
- `tools/publish-release.ps1`

## Implementation Notes

### Review re-derivation requirements (iteration 1)

The first-pass implementation is retained for positive preservation at `../../artifacts/30-5-first-pass.patch`. This is implementation evidence, not another source of requirements. Restore/reuse its working code while re-deriving the fixes below; do not lose the existing 19 recovery fixtures or the previously passing historical, source, helper, health and zero-write checks.

KEEP: the verified offline identity/receipt/file-hash bindings; the approved Builds allowlist and exact helper bytes; retained-byte-only image/package writes; the complete original fifteen-member plan and phase rechecks; immediate rechecks; authenticated full graph comparison; package ZIP-entry comparison; production/live-main/exact-CI gates; historical two/four-image nine-package boundaries; all existing successful refusal and idempotence fixtures; the 21-file scope.

- `.github/workflows/recover-partial-release.yml`: initialize the dispatched SDK before running SDK-backed trusted fixtures and initialize the tagged SDK for current recovery before any NuGet push. Use the existing allowed SDK setup pattern without changing global.json or dependency pins.
- `tools/archive-release-evidence.py`: when building recovery evidence, validate preparation identity, package bytes/metadata and archive-compatibility result/bindings before calling a bundle recoverable. Retain incomplete/invalid preparation with named unrecoverable reasons. If a normal successful evidence packet and receipt exist, retain those exact bytes in the failed-run bundle. Compare real package bytes/metadata in success verification too. Exclude the large recovery bundle/receipt from the separate generic workflow evidence upload.
- `tools/container-publication-contract.py`: verify deployment bytes against the tagged renderer and four frozen hosted mappings before destination writes; validate the tagged renderer input rather than merely searching text. Stage the reviewed archiver if reused, or keep the renderer helper internal. Never rebuild a release member.
- Existing ordinary evidence is an immutable valid Release asset when its exact retained bytes, manifest, every record hash and source/version/set match. Preserve that packet and receipt and complete missing assets. Emit a recovery packet when no ordinary packet already occupies the name; do not overwrite or replace a valid ordinary packet. A recovery packet must include the raw OCI validation and health records/logs referenced by its embedded health/plan/results, with inventory hashes. Keep exactly thirteen assets. The Actions recovery evidence still records the recovery plan/results when the Release retains its ordinary packet.
- Keep the original fifteen-member plan unchanged and record actual attempted/verified/uncertain side effects in separate progress/failure evidence. Reprobe after failed transport/push where possible; preserve the failure and never grant success credit to uncertainty.
- Correct current incident lookup using the existing exact-title convention and sufficient result collection; report ambiguity instead of silently leaving a matching incident open.
- Extend `release_orchestration_test.py` so the real pack-release producer records ten actual package hashes/sizes/bindings and failed identity emission fails preparation. Extend `partial_release_recovery_test.py` with current staged PowerShell planning/publication entry-point fixtures; normal packet and receipt subsets; malformed/comment-only deployment rejection; invalid preparation receipts; success-package hash mismatch; raw packet record tampering; and partial-write failure summaries. These must exercise real producer/entry-point boundaries rather than call a helper in their place.


- Current-set PowerShell mode delegates to the verified Python recovery adapter. Offline identity/bundle verification precedes approved helper loading; all fifteen authenticated destination classifications precede writes. Phase rechecks retain the initial plan instead of replacing evidence of the first write decision.
- Failure bundles include exact pre-publication nupkgs and both OCI/deployment archive bytes, manifests, deployment YAML and available evidence. Missing preparation produces an explicit unrecoverable receipt/reason.
- NuGet identity compares ZIP entries while ignoring repository signing; a push has no duplicate-success flag and final flat-container absence grants no completion credit. Recovery reuses the same immutable thirteen Release assets and closes the incident only after download verification.
- No external publication, registry/NuGet/GitHub setting change or dependency/Builds pin change was performed. NuGet recovery Trusted Publishing remains an owner prerequisite. The recorded repository artifact cap is 30 days despite the requested 90-day retention; expiry refuses.
- Ordinary packet and receipt-only subsets are verified against the exact retained normal packet and preserved. New recovery publication creates an empty draft and uploads the verified packet before its receipt, preventing its own receipt-only interruption state; interruption reruns and fresh-runner zero-write/zero-health reruns pass.

## Spec Change Log

- 2026-11-15, review iteration 1: BH1/BH2/BH4/BH7/BH10 (also EC1/VG-other1) require non-frozen implementation clarification: SDK setup, ordinary immutable packet preservation, tagged-renderer proof, durable referenced records and truthful partial-failure progress. Added explicit instructions above to avoid normal-asset dead ends, invalid deployment success and lost side-effect evidence. KEEP all verified first-pass core behavior and tests; use the preserved first-pass implementation as reusable working material. Frozen owner intent is unchanged.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R1 BH1 | medium | bad_spec | Current tooling validation invokes real dotnet msbuild and global.json selects SDK 11.0.100, but the current branch has no setup. Explicit SDK setup must precede trusted fixtures and run for the tag before package push. |
| R1 BH2 | high | bad_spec | The normal publisher and recovery share PACKET_NAME; normal evidence-manifest.json is rejected by validate_recovery_packet. Preserve and verify the retained original packet/receipt rather than replacing assets. |
| R1 BH3 | maybe-false | defer | Receipt-only recovery packets are refused, but concurrent gh release create upload ordering was not established from local CLI implementation. Verify interruption ordering or an isolated actual CLI fixture before claiming this subset is produced. Matching normal receipts are covered by the normal-packet fix. |
| R1 BH4 | high | bad_spec | verify_bundle only searches deployment substrings; the successful fixture itself has four strings rather than renderer YAML. Compare retained bytes with the tagged four-workload renderer before writes. |
| R1 BH5 | medium | patch | archive_recovery_bundle determines recoverable from file existence and ignores failed archive-compatibility/package receipts; the later validate_archives guard prevents writes but the advertised status is false. |
| R1 BH6 | high | patch | Success archiving validates package record shape but never checks prepared nupkg existence/hash/size/metadata, so fabricated identity gets a passing success packet. |
| R1 BH7 | medium | bad_spec | The recovery packet embeds health hashes but omits the validation records they reference. Include hash-bound raw validator and health evidence so the published packet remains inspectable after Actions expiry. |
| R1 BH8 | low | patch | The new recovery bundle lives under the existing artifacts/release/** upload wildcard, duplicating large OCI payloads. Two upload exclusions directly correct this. |
| R1 BH9 | medium | patch | The new current incident search uses broad text and default CLI result limit while filtering exact title only afterwards. An exact title query and explicit result collection avoid missing the matching open incident. |
| R1 BH10 | medium | bad_spec | Failure finally writes the initial classifications, and recover_images outcomes are ignored. A failure after an immutable side effect can leave summary members absent; record progress/reprobes separately from the immutable initial plan. |
| R1 EC1 | high | bad_spec | Confirmed same normal-packet rejection as BH2: a real archive with passing evidence-manifest.json fails the recovery-only format guard. |
| R1 VG1 | medium | patch | Pre-verified mutation removed the package-identity invocation while the real preparation fixture still passed. Assert actual producer identity bytes/hash/size/bindings and failure handling. |
| R1 VG2 | high | patch | Pre-verified mutation routed -Publish to planning while all recovery/historical tests passed. Add current-set staged PowerShell execution covering planning and actual completion. |
| R1 VG-other1 | high | bad_spec | Confirmed same normal-packet rejection as BH2 and EC1 at the shared evidence asset name; ordinary-release packet preservation is required. |


| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R2 BH1 | medium | patch | Recovery renderer needs byte-identical kubectl output, but release initializes a pinned KUBECTL_VERSION and recovery relies on the runner executable before checking it. Reuse the same pin/setup before metadata. |
| R2 BH2 | medium | patch | passing_health ignores required logs; missing retained health-amd64.log is accepted, health is reused, and six packages can be pushed before packet construction fails. Treat incomplete health as needing a fresh health run. |
| R2 BH3 | medium | patch | A NuGet push followed by flat-container 404 gets uncertain=false even though the documented validation window can hide an accepted package. Only positive identity verification clears package-write uncertainty. |
| R2 BH4 | medium | patch | The existing recovery packet is fully validated but its raw health evidence is not restored, so a clean-runner completed rerun needlessly repeats five health processes. Restore its verified evidence before health selection. |
| R2 BH5 | medium | patch | Each existing-asset iteration downloads all thirteen assets again without a pending write. Compute missing names from the validated snapshot and recheck only before actual writes; retain final full verification. |
| R2 BH6 | medium | patch | Recovery-bundle custom directory inputs are accepted but serialized at custom paths that verify_bundle cannot consume. Canonicalize retained artifact paths while reading the configured source locations, or mark unsupported layouts unrecoverable before publication. |
| R2 BH7 | medium | patch | Frozen run attempt is never compared to the failed-run metadata, allowing a stale named artifact to be treated as the failed current attempt. Require matching attempt alongside ID/head/conclusion and cover drift. |
| R2 BH8 | medium | patch | validate_recovery_packet omits Development, lifecycleWritesEnabled and endpoint checks despite trusting old packet health. Apply the complete existing health semantics consistently and test rebinding attempts. |
| R2 BH9 | medium | patch | Recovery packet omits raw frozen/package identities and tagged manifests, so its set hash/helper provenance cannot be recomputed after bundle expiry. Include and validate these already-known raw records. |
| R2 BH10 | low | patch | The supported mode explicitly checks failure conclusion and retained bytes; cancellation/runner loss/timeouts cannot be assumed to execute failure retention. Document these named unsupported/missing-bundle cases without broadening live recovery or changing pins/settings. |
| R2 EC1 | high | patch + defer | Materializing an archive occurs after the last absence check and may take significant time; move/repeat the authenticated recheck immediately before copy and test a writer during materialization. Residual probe-to-write atomicity depends on the already-open registry immutability policy and remains deferred, without live proof. |
| R2 EC2 | high | patch | New authoritative GitHub CLI implementation evidence establishes concurrency=5 and concurrent asset uploads; this settles the previously unverified R1 BH3 claim. Create a draft without assets, upload the packet before its receipt sequentially, then verify and publish; test interruption/rerun at upload boundaries. |
| R2 EC3 | medium | patch | Same demonstrated NuGet validation-window uncertainty defect as BH3; a timeout does not make a following 404 proof of non-publication. |
| R2 VG1 | medium | patch | Pre-verified mutation removed absent-packages GITHUB_OUTPUT emission while all 28 tests passed. Assert output zero/nonzero via the staged planning entry point so image-only recovery avoids login. |
| R2 VG2 | medium | patch | Pre-verified mutation removed historical pre-upload conflict validation while all historical cases passed. Add a conflicting occupied plus missing asset and assert no upload/inventory change. |

### Review resolution

All local correction groups from both rounds are implemented and verified in the final suites. R1 spec defects were re-derived under the non-frozen clarification; R1 patch groups and R2 patch groups are closed. R2 EC1's materialization-window recheck is closed; the residual server-side atomicity guarantee remains deferred to authoritative registry policy evidence (owner Jérôme Piquot). The prior R1 BH3 uncertainty is settled: [GitHub CLI uses concurrent uploads](https://github.com/cli/cli/blob/trunk/pkg/cmd/release/shared/upload.go); recovery now creates an empty draft and uploads its packet before its receipt, with interruption/rerun fixtures. Earlier verdict rows are preserved as review history.

## Design Notes

- Retain, don't rebuild: SDK container builds are not reproducible. A rebuilt member would not match the `archive-compatibility.json` index digests or package hashes, so "exact" could not be proven. Retained bytes keep `--preserve-digests` identity.
- Present NuGet identity: repository signing rewrites the zip. Compare every retained entry byte for byte and allow only an added `.signature.p7s`.
- A recovered Release keeps the normal 13 asset names. Preserve an already-uploaded, verified ordinary packet and its receipt from the retained bundle. Otherwise its new evidence packet is `artifactKind: partial-release-recovery`, embeds the bundle manifest, plan, results and referenced records, and binds the same `setSha256`. Recovery Actions evidence always retains the completion plan/results.
- Only failures after this change have a bundle. An earlier failed current tag refuses with a named blocker.

## Verification

Implementation evidence (2026-11-15; offline fixtures only):

Final result: PC 149/149, PN 32/32, BQ 70/70, release_dispatch 17/17 and ci_alignment 15/15 passed (283 tooling methods total). The CI inventory passed 141/141 cases, with no skips or failures. All 37 recovery fixtures ran in the full PC discovery and passed. Two independent three-layer review rounds are complete; local findings are corrected and the registry-policy atomicity evidence remains explicitly deferred.

Final parent verification uses `run-with-temp` for every disposable job, with logs saved outside scratch under `artifacts/partial-release-final-*.log`. Required tooling lanes are PC, PN, BQ, release_dispatch and ci_alignment, each with `python3 -B -m unittest discover -s tests/tooling/<lane> -p '*_test.py' -v`. The final Debug build command is `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug --artifacts-path "$TMPDIR/build" -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -m:1`, followed within the same disposable job by `DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.Cli.Tests/debug/Hexalith.Memories.Cli.Tests.dll" -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`. Build passed with zero warnings/errors; all 141 cases passed with no skips. Actionlint, Python AST/PowerShell parsing, file/slice scope and whitespace checks passed. Frozen owner intent was compared to HEAD and is unchanged.


Revision 1 supersedes the first-pass totals below: full PC discovery passed 139/139 before the last partial-upload test; the final recovery rerun passed 28/28 and the final PC inventory is 140 (baseline 111 + 19 first-pass recovery + 9 revision recovery + 1 preparation test). PN 32/32, BQ 70/70, dispatch 17/17, ci_alignment 15/15; isolated Debug build zero warnings/errors and CI inventory 141/141. Actionlint, Python/PowerShell parsers, file/slice scope and whitespace checks passed. Logs: `artifacts/partial-release-revision-*.log`. Original first-pass totals follow as historical evidence.


- `run-with-temp python3 -B -m unittest discover -s tests/tooling/publish_containers -p '*_test.py'` — 130/130 methods passed (baseline 111 + story 19). The final recovery-specific rerun after plan-retention hardening is recorded in `artifacts/partial-release-recovery-tests.log`; the full suite log is `artifacts/partial-release-containers-tests.log`.
- `run-with-temp python3 -B -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py'` — 32/32 passed; Builds publication qualification 70/70, release_dispatch 17/17 and ci_alignment 15/15 passed with the same discovery pattern. Logs: `artifacts/partial-release-{nuget,builds,dispatch,ci-alignment}-tests.log`. No skips.
- `run-with-temp bash -c 'dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --artifacts-path "$TMPDIR/build" -p:UseHexalithProjectReferences=true ...; DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.Cli.Tests/debug/Hexalith.Memories.Cli.Tests.dll" -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests ...'` — isolated Debug build succeeded with 0 warnings/errors; inventory 141/141 cases passed, no skips. Exact output: `artifacts/partial-release-dotnet-build.log`, `artifacts/partial-release-ci-inventory-tests.log`.
- `actionlint .github/workflows/recover-partial-release.yml .github/workflows/release.yml`, Python AST parsing and PowerShell parser checks — passed.
- Slice-scope with `--story-key 30-5-partial-release-recovery --require-record` passed. File scope requires `--story-key spec-30-5-partial-release-recovery`; the originally listed unprefixed command reports `Story artifact not found: .../30-5-partial-release-recovery.md`. The prefixed command passes all 21 changed files. `git diff --check` passed.
- Fixture bundle, receipt, first plan, completion summary and recovery packet are retained under `artifacts/30-5-recovery-fixture/`. They contain controlled bytes and provide no live NuGet, registry, production approval or Trusted Publishing proof. The optional real-boundary download comparison was not performed.


**Commands:**
- PC, PN, BQ (Discovery commands) -- expected: all pass; any skip is named and gets no credit.
- `python3 -B -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py'` and `-s tests/tooling/ci_alignment` -- expected: unchanged passes.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj`, then CT -- expected: all cases pass.
- `python3 tools/check-story-file-scope.py --story-key spec-30-5-partial-release-recovery --changed-files-file <diff list>`; `python3 tools/check-story-slice-scope.py --story-key 30-5-partial-release-recovery --require-record`; `git diff --check` -- expected: exit 0.

**Manual checks:**
- Read-only real-boundary check (if authorized): for one v4.0.0 package, compare the GitHub Release asset with the bytes nuget.org serves, using the entry rule.
