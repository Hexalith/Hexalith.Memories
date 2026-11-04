---
title: 'Five-image publication contract'
type: 'chore'
created: '2026-11-03'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-4-five-image-publication-contract'
baseline_commit: 'd959bdae5af9c22f53573eda71c091cb5c0aae4c'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Publication lacks mapping identity, both architectures, workload health evidence and durable retention.

**Approach:** Bind the canonical five mappings to source/version/workflow/approved Builds identity. Publish multiarch OCI archives through skopeo and reuse approved preflight, provenance and validation helpers.

## Boundaries & Constraints

**Always:** Require Epic 30.4's exact mappings and linux/amd64 + linux/arm64 child digests. Preserve ten packages, OIDC, dispatch guards, aggregate summaries, caller rendering and five same-source deployment archives. Qualify the new transport adapter.

**Never:** Change dependencies/settings, enable publication or Production lifecycle writes, publish a release during implementation, overwrite occupied tags, or claim fixture evidence proves registry success. Historical recovery stays compatible; missing-only recovery belongs to 30.5.

## I/O & Edge-Case Matrix

| State | Required behavior |
| --- | --- |
| Missing/remapped member; wrong identity | Refuse before writes |
| Missing platform, health or evidence | Fail; preserve diagnostic evidence |
| Auth denial/ambiguous destination | Fail; name owner, consequence and reopen trigger |
| Historical archive build | Preserve original filenames and format |

</frozen-after-approval>

## Code Map

- Predecessors 30.2/30.3 are done. Publication approval binds Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa`; the newer CI profile grants no publication approval. Reuse `tools/check-release-destinations.py` loading and unchanged approved helpers.
- Approved smoke probes `/alive` in Development. Reuse its digest checks; provide workload configuration and isolated test dependencies.

## Tasks & Acceptance

**Execution:**
- [x] `tools/release-containers.json`, `tools/container-publication-contract.py` — freeze exact project mappings, version, source, workflow run and approved SHA; validate the set before invoking shared phases once each.
- [x] `Directory.Build.targets`, `tools/publish-containers.ps1`, `tools/pack-release.ps1`, `tools/publish-release.ps1` — import approved provenance; build separate two-platform OCI archives; copy all children with preserved digests; retain stable deployment archives, rendering and aggregate interfaces.
- [x] `tools/verify-container-health.py`, `tools/verify-container-registry.ps1` — digest-bound per-platform health contracts and redacted auth results. Server/MCP/DomainService prove liveness; lifecycle proves disabled readiness; clock proves signed readiness with isolated test authorities. Failed cleanup fails verification.
- [x] `tools/archive-release-evidence.py`, `.github/workflows/release.yml`, `.releaserc.json`, `tools/verify-semantic-release-config.mjs` — hash all mapping/identity/auth/OCI/health records; reject omissions; upload failure diagnostics and retain successful evidence as a release asset.
- [x] `tests/tooling/publish_containers/*_test.py`, `tests/tooling/release_dispatch/*_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` — cover matrix failures, health, transport and compatibility.
- [x] `docs/dev/release-runbook.md` — document five-image contract, retained evidence, auth mode and current blockers.

**Acceptance Criteria:**
- Given a semantic-release version, when publishing, then exactly the five canonical mappings share source/workflow/set identity and each passes declared platform and health checks.
- Given registry authorization, when verified, then record the actual mode and any accountable blocker without treating preemptive upload probes as push proof.
- Given production verification, when consuming archives, then all five stable filenames remain same-source compatible and rendering remains caller-owned.

## Checkpoint Evidence

Offline fixtures passed; the live evidence below remains blocked. Paths are relative to the future release evidence packet. These dispositions grant no live publication credit.

| Checkpoint | Owner | Artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Five mappings | Jérôme Piquot | identity.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Server platforms | Jérôme Piquot | server/oci-validation.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| MCP platforms | Jérôme Piquot | mcp/oci-validation.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Lifecycle platforms | Jérôme Piquot | access-telemetry/oci-validation.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Clock platforms | Jérôme Piquot | access-telemetry-clock/oci-validation.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Domain platforms | Jérôme Piquot | domain-service/oci-validation.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Server health | Jérôme Piquot | server/health.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| MCP health | Jérôme Piquot | mcp/health.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Lifecycle health | Jérôme Piquot | access-telemetry/health.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Clock health | Jérôme Piquot | access-telemetry-clock/health.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Domain health | Jérôme Piquot | domain-service/health.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Registry authorization | Jérôme Piquot | authorization.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |
| Archive compatibility | Jérôme Piquot | archive-compatibility.json | blocked; consequence: live proof unavailable; reopen trigger: authorized credentialed validation | not complete |

## File Scope

Allowed files for this story:

- `tools/release-containers.json`
- `tools/container-publication-contract.py`
- `tools/verify-container-health.py`
- `tools/archive-release-evidence.py`
- `tools/publish-containers.ps1`
- `tools/verify-container-registry.ps1`
- `tools/pack-release.ps1`
- `tools/publish-release.ps1`
- `Directory.Build.targets`
- `.github/workflows/release.yml`
- `.releaserc.json`
- `tools/verify-semantic-release-config.mjs`
- `tests/tooling/publish_containers/*_test.py`
- `tests/tooling/release_dispatch/*_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Implementation Notes

- Added the canonical five-image manifest and release-set adapter. Source/version/workflow/run/attempt, project-to-repository mappings, approved Builds helper hashes and a publisher creation instant are frozen before current publication. All three unchanged shared preflight phases execute once; the current path refuses every occupied or ambiguous tag and rechecks each destination immediately before its copy.
- Retained all five stable linux/amd64 Docker deployment archives and caller rendering. Publication uses separate two-platform OCI archives with the approved provenance target and `skopeo copy --all --preserve-digests`; approved OCI graph validation compares the transported index and children with the prepared graph. Historical archive/recovery fixtures use historical source trees without the new manifest.
- Added digest-bound Development health: Server/MCP/DomainService liveness; lifecycle explicitly disabled readiness; clock signed readiness with a temporary P-256 key, isolated Dapr local-file secret authority and Development UTC sources. Failed workload/authority/network cleanup fails verification. No Production lifecycle configuration or dependency pin was changed.
- Recorded redacted preemptive Basic upload-session authorization separately from transport success (`pushProven: false`). Hash-checked evidence requires mapping identity, auth, three preflight records, every local/remote OCI graph, per-platform health, archive compatibility and aggregate/family summaries. Success packets and their SHA-256 receipts become release assets; failure diagnostics remain diagnostic-only and upload for 90 days.
- The approved transport adapter was qualified offline using the unchanged approved preflight and OCI validator with controlled external source/destination/SDK/registry/process boundaries. Fixture success grants no live registry success or Production qualification. Every live Checkpoint Evidence row remains planned/not complete until the release owner supplies authorized live evidence.

## File List

- `tools/release-containers.json`
- `tools/container-publication-contract.py`
- `tools/verify-container-health.py`
- `tools/archive-release-evidence.py`
- `tools/publish-containers.ps1`
- `tools/verify-container-registry.ps1`
- `tools/pack-release.ps1`
- `tools/publish-release.ps1`
- `Directory.Build.targets`
- `.github/workflows/release.yml`
- `.releaserc.json`
- `tools/verify-semantic-release-config.mjs`
- `tests/tooling/publish_containers/publication_contract_test.py`
- `tests/tooling/publish_containers/publish_containers_test.py`
- `tests/tooling/publish_containers/registry_authorization_test.py`
- `tests/tooling/publish_containers/release_orchestration_test.py`
- `tests/tooling/release_dispatch/release_dispatch_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

### File List Exclusions

- `references/Hexalith.Builds` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.
- `references/Hexalith.EventStore` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.
- `references/Hexalith.FrontComposer` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.
- `references/Hexalith.McpCli` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.
- `references/Hexalith.Platform` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.
- `references/Hexalith.Tenants` — owner: workspace maintainer; external commit 8c0b907e advanced this dependency during implementation, outside Story 30.4.

- `.github/workflows/ci.yml` — owner: workspace maintainer; external commit 3d6e466238fe7f78dc6216918f5e749f513cca9b restores the approved Builds pin and changes the CI timeout.
- `tests/tooling/ci_alignment/ci_alignment_test.py` — owner: workspace maintainer; assertion updated by the same external CI fix, outside Story 30.4.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.QualificationCurl.targets` — owner: workspace maintainer; external commit 205baddda57911896d8c08708e80ecaf0bc14f82 qualifies the Server entrypoint for private Dapr workloads, outside Story 30.4.
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` — owner: workspace maintainer; external commit 49c913da13a8041fa70f87de40ffd44f19877f87 refreshes the pinned qualification curl hashes, outside Story 30.4.
- `tests/tooling/access_telemetry_lifecycle/test_retention_verification.py` — owner: workspace maintainer; external commit 49c913da13a8041fa70f87de40ffd44f19877f87 refreshes the pinned qualification curl hashes, outside Story 30.4.
- `tools/stage-qualification-curl.py` — owner: workspace maintainer; external commit 49c913da13a8041fa70f87de40ffd44f19877f87 refreshes the pinned qualification curl hashes, outside Story 30.4.
- `tools/publish-nuget.ps1` — owner: workspace maintainer; external commit 82c0aa0a7046d7bd91569ac1c9a38a3ed0386d0f retains the successful NuGet publication summary, outside Story 30.4.
- `tests/tooling/publish_nuget/publish_nuget_test.py` — owner: workspace maintainer; external commit 82c0aa0a7046d7bd91569ac1c9a38a3ed0386d0f retains the successful NuGet publication summary, outside Story 30.4.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-03 | create-story | Drafted the spec, investigated approved helpers and recorded explicit user approval before implementation. | Baseline container fixtures 46/46; no test added. | Matched 1/1 at creation: this new spec. |
| 2026-11-03 | dev-story | Added five-image adapter, provenance, workload checks, retained evidence and release integration; preserved independent dependency updates. | Container fixtures 46→68 (+22), dispatch 16→17 (+1), preflight 50→50 (+0), C# inventory 138→138 (+0); focused adapter rerun 20/20. Exact commands and broad blockers follow. | Matched 21/21 story paths against preserved baseline, excluding six externally committed gitlinks. |
| 2026-11-03 | code-review | Three independent layers produced 21 findings; the parent added P1. Applied 16 direct correction groups, logged every finding before grouping, and deferred only the unverified registry immutability policy. Code review is complete; story stays in-progress because current dependency and live qualification gates remain open. | Container fixtures 68→82 (+14: contract 20→33 and orchestration 6→7); dispatch 17→17 (+0); C# 138→138 (+0). Parent ran 81/81 container cases before P1, then 4/4 final archive cases including the new P1 test; all 82 distinct current container cases passed across those runs. Dispatch on the advanced dependency snapshot is 16/17; exact blocker below. | Matched 22/22 story paths; deferred-work.md joined for the two accountable owner follow-ups. Six external gitlinks are excluded and preserved. |
| 2026-11-03 | dev-story | Build verification of the re-dispatch found that a deployment-render failure on a current tree still wrote historical recovery guidance. That made the Story 30.3 deferred entry (reopen trigger: Story 30.4 edits publish-containers.ps1) due now. Recovery text now depends on the source tree. Two Story 30.3 entries whose 30.4 trigger fired are resolved. | Container fixtures 82→83 (+1: `test_render_failure_recovery_guidance_matches_the_source_tree_boundary`); dispatch 17→17 (+0); C# 138→138 (+0). | Matched 22/22 story paths. No path was added; `tools/publish-containers.ps1`, `publish_containers_test.py`, this spec and deferred-work.md were already listed. |
| 2026-11-03 | code-review | Round 2: three fresh layers produced 44 findings. Every finding is logged individually: 25 patched in 14 root-cause groups (G1–G14), 4 deferred as 3 unverified entries (D1–D3), 15 rejected. No intent gap or spec defect was found, so no loopback ran. Exact-source CI run 41707067835 on 41f8bdca confirmed R2-B6. | Container fixtures 83→89 (+6, all in `publication_contract_test.py` 33→39; a real-Docker health case ran, not skipped); dispatch 17→17 (+0); preflight 50→50 (+0); broad qualification 70→70 (+0); C# 138→138 (+0). | Matched 22/22 story paths. No path was added: every edited path (the four health/contract/archive/publisher tools, the runbook, four `publish_containers` tests, this spec and deferred-work.md) was already listed. |

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | high | patch | The common SDK arguments omit ContainerProvenanceRepositoryUrl; the approved target falls back to empty RepositoryUrl for four projects and emits invalid required labels. |
| B2 | high | patch | Server startup resolves both keyed multiplexer registrations; ConnectRequiredMultiplexer throws without the absent redis/falkordb connection strings. Supply isolated smoke dependencies/configuration. |
| B3 | medium | patch | The archiver checks only summary status/version, allowing an empty or contradictory image inventory and a different set digest to pass. |
| B4 | medium | patch | The deployment file is required but its content is never compared with caller rendering; arbitrary bytes pass as same-release deployment evidence. |
| B5 | medium | patch | destination-before-copy.json is required only by filename; failed or different-destination receipts are accepted. |
| B6 | medium | patch | Retained local raw graphs are existence-checked but not validated against prepared archives; corrupted local/index.raw passes. |
| B7 | high | patch | A successful copy followed by failed health is represented only as failed; all such outcomes produce pushed=[] despite immutable registry side effects. |
| B8 | medium | patch | The cleanliness command misses root build configuration and untracked SDK-glob source files, allowing bytes outside the claimed source revision. |
| B9 | medium | patch | The publisher supports custom package/container/summary directories but invokes an archiver that reads hardcoded default directories after publication. |
| B10 | medium | patch | Diagnostic archiving parses malformed identity.json and aborts instead of retaining that failure evidence. |
| B11 | medium | patch | The aggregate is written as succeeded before evidence creation; an evidence failure throws without changing the summary, so partial-publication alerting skips it. |
| E1 | high | patch | Same confirmed startup failure as B2: missing configured connections prevent actual Server liveness; this joins that fix group. |
| E2 | high | patch | load_shared calls the dynamic preflight loader before byte verification. Modified helper top-level code therefore executes before rejection. |
| E3 | medium | patch | The one-level project glob misses an added container project below src/nested/Extra, violating exact inventory checking. |
| E4 | medium | patch | Same demonstrated incomplete source-input guard as B8; joins the clean-source fix group. |
| E5 | maybe-false | defer | A post-probe writer could race skopeo if the registry permits mutable tags, but the deployed immutability policy is unknown. Authorized policy readback or an isolated concurrent registry fixture is needed to establish the claimed overwrite. |
| E6 | medium | patch | Same retained-local-graph corruption acceptance as B6; joins the evidence binding fix group. |
| E7 | medium | patch | Same contradictory-summary acceptance as B3; joins the summary validation fix group. |
| E8 | medium | patch | Same custom-directory regression as B9; joins the directory propagation fix group. |
| V1 | medium | patch | Pre-verified gap: disabling the readiness dispatch branch leaves all 20 adapter fixtures green because the health worker is replaced or only the parser is exercised. |
| V2 | medium | patch | Pre-verified gap: inverting the evidence exit guard leaves all six orchestration tests green; the existing FAKE_EVIDENCE_EXIT seam has no test caller. |
| P1 | high | patch | Parent synthetic reproduction shows diagnostic archiving follows evidence symlinks because paths are resolved before is_symlink; reject the link before resolution so unrelated target bytes cannot enter the packet. |
| R2-B1 | high | patch (G1) | Parent reproduced on local Docker 29.8.2. A container on a `docker network create --internal` network with `--publish 127.0.0.1::6379` reports "no public port published"; the same container on a plain user-defined bridge gets `127.0.0.1:50749`. `run_platform` uses `--internal`, so the approved `_poll_liveness` and `poll_readiness` cannot reach any workload. |
| R2-B2 | false | reject | Digest-bound health of the transported image is the approved design. A failure after transport yields the partial-publish aggregate and alert (test_all_successful_copies_remain_pushed_when_every_health_check_fails), and the frozen intent assigns missing-only recovery to Story 30.5. |
| R2-B3 | maybe-false | defer (D3) | Parent evaluated `RebindContainerProvenanceLabels` with the exact multi-RID arguments. MSBuild unquotes `RuntimeIdentifiers=linux-musl-x64;linux-musl-arm64` and emits labels identical to the single-RID case. The SDK's multi-RID inner-build label reconstruction is unverified, but the approved validator would refuse at prepareCmd, before any tag or write. Settle with a real multi-RID PublishContainer archive run through the approved validator. |
| R2-B4 | false | reject | The intent requires same-source archives (shared revision labels, one tree, one job), not layer byte-identity between the Docker and OCI builds. No consumer compares layers, and no harm was shown. |
| R2-B5 | low | reject | Release runs on a fresh CI checkout, where ignored files cannot exist before the build, and publication refuses outside the dispatch identity. Widening the filter to every ignored path risks falsely refusing build-generated ignored files after publication, so the safe fix is more than a direct correction. |
| R2-B6 | high | patch (G4) | Confirmed by CI run 41707067835 on 41f8bdca: test-unit-contract "Run container publish fixtures" raised CalledProcessError 128 in PublicationContractTests.setUpClass (`git show 81c88b34:...`), because the shallow Builds checkout lacks the approved commit. `recover-partial-release.yml` runs the same discover with `submodules: false`, so historical recovery validation would also fail. The cross-origin sub-claim is false: failing closed without cancellation credentials is the deliberate, tested hardening. |
| R2-B7 | maybe-false | defer (D1) | No publication-enabled release run exists to time five multi-RID builds plus ten QEMU health runs against the 45-minute budget; enabled-flag-off runs took 7–24 min. The cancellation sub-claim is low (reject): `artifacts/release/**` uploads with `if: always()`, and the alert's `if: failure()` predates this story. The symlink-abort sub-claim is low (reject): P1 refusal is deliberate, and no producer writes symlinks. |
| R2-B8 | false | reject | `verifyReleaseCmd` writes authorization.json in the same fresh CI workspace before prepareCmd/publishCmd. Publication refuses without the exact dispatch identity, so a stale receipt from another run cannot reach the gate. |
| R2-B9 | low | patch (G10) | On every authorization failure (the current state without live credentials) the container family summary is null. The aggregate then records `source: "NuGet and "` with null sourceSha/setSha256. A direct correction restores the registry fallback. |
| R2-B10 | medium | patch (G6) | test_occupied_and_ambiguous_destinations_refuse_before_builds_or_copy accepts any Exception. Nothing asserts that the matrix's owner/consequence/reopen trigger is recorded for an ambiguous destination; only `main()`'s failure-<phase>.json carries them, and no test reads it. |
| R2-B11 | medium | patch (G7) | Same gap as R2-V5: the source-refusal test lacks a clean-tree positive control. |
| R2-B12 | low | patch (G13) | `validate_manifest` detects a swap (dict comparison), but no test swaps ContainerRepository while keeping the five names, and the 30.3 resolution cites tests that only delete. `EnableContainer` of `True` is MSBuild-true but escapes the exact-text check; direct correction. The namespace, imported-props and outside-src sub-claims are not reachable in this repository (plain csproj files under src, verified with validate_manifest on the real tree), so they are rejected. |
| R2-B13 | medium | patch (G9) | `run_platform` maps every exception to workload-health-failure without recording the cause, so dependency-pull, openssl and timeout diagnostics are lost. This contradicts the matrix row "preserve diagnostic evidence". |
| R2-B14 | low | reject | The key, token and JWT values are per-run test-only material in removed containers. A redaction test suite adds complexity for an unlikely leak. |
| R2-B15 | low | patch (G14) | The pin-sync check is rejected (low, adds complexity). Runbook prerequisite 8 omits the Docker Hub Redis Stack/FalkorDB and ghcr daprd pulls that health needs, and the fix is a direct documentation correction. |
| R2-B16 | low | reject | Recording tool versions adds new evidence fields that the intent does not require. |
| R2-B17 | low | reject | Signing or reproducible packets is not in the intent, and the fix adds complexity. |
| R2-B18 | low | reject | Over-redaction errs safe. Tightening the Basic/Bearer pattern risks leaking credential-shaped text. |
| R2-B19 | false | reject | Status is mid-step by design (step 4 sets in-review; step 5 finalizes). The epic-30-context entry is an existing open deferred entry with an owner. The CI follow-up is recorded at presentation. |
| R2-B20 | low | reject | The fix would edit this build's spec. |
| R2-V1 | high | patch (G3) | Pre-verified gap, reproduced by the parent with pwsh 7.6.2. ConvertFrom-Json/ConvertTo-Json turns `.1234560Z` into `.123456Z` and `.0000000Z` into `Z`. `validate_summaries` then refuses the aggregate after both families published. No test runs the real PowerShell aggregation through the real archiver. |
| R2-V2 | high | patch (G1) | Pre-verified gap: every health test stubs `command` or the smoke port/liveness. This joins the --internal defect group. |
| R2-V3 | medium | patch (G5) | Pre-verified gap: `main()` phase dispatch (build creates identity; authorize runs shared phase `publish`) is never executed. Deleting the authorize `shared_phase` leaves every test green, while a real run would publish NuGet before `container` refuses with preflight-sequence-invalid. |
| R2-V4 | medium | patch (G8) | Pre-verified gap: the producer test never asserts `cleanup` or `registry`, which `validate_authorization` gates on, and contract tests use hand-written receipts. |
| R2-V5 | medium | patch (G7) | Pre-verified gap: a malformed git command makes `current_identity` refuse every clean tree, yet test_modified_root_build_inputs_and_untracked_source_refuse_before_freeze stays green. |
| R2-V6 | high | patch (G3) | Same timestamp round-trip defect as R2-V1 (Other finding). |
| R2-V7 | high | patch (G1) | Same --internal defect as R2-B1 (Other finding). |
| R2-V8 | low | patch (G11) | `run_probe` without `evidence_path` writes the receipt into the repository's real `artifacts/release/container-evidence/authorization.json` (present now with registry 127.0.0.1). Direct correction: default to a temporary path. |
| R2-E1 | high | patch (G1) | Same --internal defect as R2-B1. |
| R2-E2 | high | patch (G2) | Clock `Program.cs` calls `GetSecretAsync(store, "access-telemetry-clock-key")` and reads `signing-key-pkcs8` from the returned map. Dapr's `secretstores.local.file` without `multiValued: "true"` flattens nested JSON to `access-telemetry-clock-key:signing-key-pkcs8` (reviewer reproduced on daprd 1.18.7), so signed readiness can never pass. |
| R2-E3 | high | patch (G3) | Same timestamp round-trip defect as R2-V1. |
| R2-E4 | maybe-false | defer (D2) | Whether skopeo can exit nonzero or time out after writing the index tag (leaving `transported: false` for an occupied tag) is unshown. Settle with a registry fixture that fails skopeo after the manifest PUT, or a post-failure tag re-probe; medium if real. |
| R2-E5 | maybe-false | defer (D1) | Same timeout-budget question as R2-B7. The cancellation sub-claim is rejected as low for the same reason. |
| R2-E6 | false | reject | No producer creates symlinks in artifacts/release, and every evidence writer is this story's code. Unreachable as shown. |
| R2-E7 | false | reject | Symlinked evidence parents are unreachable for the same reason. |
| R2-E8 | low | patch (G13) | Same EnableContainer case-sensitivity as R2-B12. The namespace and props sub-claims are rejected as unreachable here. |
| R2-E9 | low | reject | Same as R2-B5. |
| R2-E10 | false | reject | The approved shared directory is a workflow checkout at the exact approved SHA, and only this job writes beside it. No path to plant a package or a matching-mtime pyc was shown. |
| R2-E11 | false | reject | Same as R2-B8. |
| R2-E12 | low | patch (G12) | `network_attempted` is set before `docker network create`. A creation failure then runs `network rm`, fails, and is reported as cleanup-failure. Direct correction. |
| R2-E13 | low | patch (G10) | Same aggregate source fallback as R2-B9. |
| R2-E14 | high | patch (G1) | Claim audit of the --internal defect; same group. |
| R2-E15 | high | patch (G2) | Claim audit of the multiValued defect; same group. |
| R2-E16 | high | patch (G3) | Claim audit of the timestamp defect; same group. |

## Verification

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`
- Baseline: equivalent `-q` run passed 46/46 in 177 seconds.
- Run affected dispatch/qualification/preflight fixtures, semantic-release verifier, actionlint and source Debug build/C# inventory; reconcile scope/readiness.
- 12:01 UTC: unauthenticated registry `/v2/` GET returned 401, `Basic realm="zot-distribution"`. Local credentials are absent. Push unverified; owner: Jérôme Piquot; consequence: no registry-success credit; reopen trigger: authorized credentialed validation. No remote writes.

### Implementation verification (2026-11-03)

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v` — **68 passed**, zero skips, 278.042 s. Log: `/tmp/memories-30-4-publication-all.log`. The initial unchanged legacy suite also passed 46/46 in 206.869 s. After final source/auth/archive checks, the new adapter suite was rerun separately with `-p 'publication_contract_test.py' -q`: **20 passed**.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -q` — **17 passed**, including failure diagnostics before always-upload and durable success assets.
- `python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -q` — **50 passed**, 159.182 s.
- `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -k ProfileTests -k ReleaseSetTests -k CommittedEvidenceTests -k DocumentTests -q` — **28 passed**.
- `npm run verify:semantic-release-config` — offline lifecycle and fail-closed negative cases passed. `actionlint .github/workflows/release.yml` — passed. Python compilation and PowerShell parsing passed for all changed scripts. `git diff --check` — passed.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false --no-restore -m:1` — passed with zero warnings/errors. `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` — **138 passed**, zero skips.
- Broad source gate: `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1` — blocked with **CS1704** in `Hexalith.Memories.EventStore`: conflicting `Hexalith.EventStore.Contracts` assemblies, source 4.0.4 and restored package 3.132.2 (MSB3243, one error). No build setting or dependency was changed to hide it.
- Broad qualification gate: `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -q` — 60 tests attempted, 3 failures/9 errors, caused by the preserved Builds checkout mismatch: actual `725754075e1d6369430a3adf94d46fb71e6eaf9e`, profile/prior gitlink `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`. Owner: workspace/release maintainer Jérôme Piquot; consequence: broad source/checkout qualification is not credited; reopen trigger: maintainer reconciles source references and declared checkout identities, then reruns those exact commands. Focused evidence above remains valid.
- Unrelated worktree pointer changes in `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, `references/Hexalith.McpCli`, `references/Hexalith.Platform`, and `references/Hexalith.Tenants` were preserved and excluded from the story File List. No stage, commit, branch, push, submodule update or remote publication command was issued.

### Frozen matrix audit

| Frozen row | Executed covering tests | Result |
| --- | --- | --- |
| Missing/remapped member or wrong identity refuses before writes | `test_manifest_requires_exact_five_mappings_health_platforms_and_approved_sha`, `test_missing_added_or_remapped_project_is_refused_before_sdk_or_transport`, `test_workflow_run_source_and_semver_identity_fail_closed`, `test_frozen_identity_rejects_mapping_version_run_and_digest_changes`, `test_failed_frozen_authorization_refuses_both_families_and_retains_aggregate` | passed |
| Missing platform, health or evidence fails and preserves diagnostics | `test_missing_platform_and_provenance_are_rejected_by_approved_validator`, `test_missing_source_provenance_is_rejected_by_approved_validator`, `test_missing_health_auth_or_platform_evidence_rejects_success_asset`, `test_health_digest_cleanup_or_auth_push_claim_cannot_be_substituted`, `test_diagnostics_archive_remains_explicitly_non_success_with_missing_evidence`, `test_cleanup_failure_fails_workload_and_keeps_digest_diagnostics`, `test_clock_authority_is_ephemeral_grpc_and_cleanup_failure_is_terminal` | passed |
| Auth denial/ambiguous destination fails with owner, consequence and reopen trigger | `test_occupied_and_ambiguous_destinations_refuse_before_builds_or_copy`, `test_destination_that_becomes_occupied_after_set_preflight_is_never_copied`, `test_transport_denial_and_health_failure_retain_aggregate_partial_outcomes`, `test_authorization_receipt_records_actual_mode_redaction_and_no_push_credit` | passed |
| Historical archive build preserves original filenames and format | `test_historical_source_publishes_only_server_and_mcp_unit`, `test_build_creates_all_archives_with_exact_publish_arguments`, `test_full_adapter_preserves_five_stable_archives_and_all_child_digests` | passed |

Live registry authorization, actual multiarch registry transport and both architecture runtime health remain unverified. Owner: Jérôme Piquot; consequence: no release/publication-success or Production qualification credit; reopen trigger: authorized credentialed run retaining the complete successful evidence packet. Missing-only current recovery remains Story 30.5.

Parent audit: the external workspace commit `8c0b907e0a200164d2c6ee7a732daefb9b29546d` advanced six gitlinks while this implementation ran. The working tree now matches those gitlinks; the unchanged qualification profile still selects the prior Builds revision. The exact build/qualification failures above occurred during that transition and remain uncredited. No dependency adjustment is part of this story.

Review routing: all verified defects have direct corrections within existing caller/helper behavior and need no frozen-intent change. Duplicate findings are individually logged before grouping. E5 is unverified high-impact registry policy work; it is deferred with no immutability or live-publication credit.


### Applied review corrections

| Findings | Correction and executed regression |
| --- | --- |
| B1 | Canonical repository URL passed to the approved target; `test_approved_target_evaluates_canonical_provenance_labels_for_all_projects` evaluates its actual MSBuild labels for all five projects. |
| B2, E1 | Server gets private pinned Redis/FalkorDB instances, keyed configuration and cleanup evidence; `test_server_caller_starts_both_keyed_dependencies_and_records_their_cleanup`. Clock authority hostnames also stay within the DNS label limit. |
| B3, E7 | Complete source/set/image/package/family/aggregate consistency is required; `test_empty_wrong_set_and_contradictory_summaries_are_refused`. |
| B4 | Retained deployment must equal a fresh existing caller render with frozen mappings; `test_deployment_requires_exact_caller_rendering_and_no_hosted_domain_service`. |
| B5 | Exact repository/version successful absence receipts required; `test_destination_receipts_and_local_raw_graphs_cannot_be_substituted`. |
| B6, E6 | Local and remote raw graph bytes/metadata bind the validated prepared archive; the same substitution test corrupts local index, manifest and config evidence. |
| B7 | Successful transports survive failed verification in existing publication summaries; `test_all_successful_copies_remain_pushed_when_every_health_check_fails`. |
| B8, E4 | Tracked build changes and untracked/ignored SDK-glob sources refuse; `test_modified_root_build_inputs_and_untracked_source_refuse_before_freeze`. |
| B9, E8 | Publisher directories propagate to evidence collection, which rejects stale defaults; `test_custom_directories_never_consume_stale_default_summaries`. |
| B10 | Malformed identity is retained with diagnostic-only unreadable attribution; `test_malformed_identity_bytes_are_retained_in_diagnostic_only_archive`. |
| B11, V2 | Failed evidence process preserves published side effects, records partial/incomplete aggregate state and exits nonzero; `test_evidence_failure_preserves_completed_side_effects_and_marks_aggregate_partial`. |
| E2 | Every approved byte is checked before importing executable helpers; `test_modified_top_level_shared_helper_cannot_execute`. |
| E3 | Nested container projects participate in exact inventory validation; `test_nested_container_project_is_refused`. |
| V1 | Workload caller routing requires actual readiness even if liveness would pass; `test_run_platform_lifecycle_and_clock_require_ready_even_when_alive_would_pass`. |
| P1 | Symlinks are rejected before resolution; `test_diagnostic_archive_rejects_symbolic_link_without_retaining_private_target` covers existing and dangling links. |
| E5 | Deferred unverified high-impact registry-policy question with owner, consequence and reopen trigger; no race-free publication credit is claimed. |

### Parent final verification and remaining gates

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v` — **81/81 passed**, zero skips, 260.052 seconds, before the final P1 test was added. Log: `/tmp/memories-30-4-parent-container-tests.log`.
- `python3 -m unittest discover -s tests/tooling/publish_containers -p 'publication_contract_test.py' -k symbolic -k symlink -k custom_directories -k malformed_identity -k diagnostics_archive -q` — **4/4 passed** after P1, including its new regression and existing compatibility checks. Log: `/tmp/memories-30-4-parent-final-archive-tests.log`. Current container suite contains **82** distinct cases; all were executed and passed across these runs.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` — **138/138 passed**, zero errors, failures, skips or not-run cases. Log: `/tmp/memories-30-4-parent-csharp-inventory.log`.
- `npm run verify:semantic-release-config --silent`, `actionlint .github/workflows/release.yml`, `python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -q` (4/4), Python compilation and `git diff --check` — passed on the corrected implementation.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v` — **16 passed, 1 failed** on current HEAD `8c0b907e0a200164d2c6ee7a732daefb9b29546d`. `test_release_pin_is_approved_and_profile_tracks_the_gitlink` reports current Builds gitlink `725754075e1d6369430a3adf94d46fb71e6eaf9e` versus unchanged approved CI qualification profile `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`. This supersedes the earlier 17/17 snapshot for the current tree. Log: `/tmp/memories-30-4-parent-dispatch-tests.log`.
- The prior exact source build CS1704 and broad qualification failures remain recorded above; the dependency cohort/profile has not been reconciled. Owner: Jérôme Piquot; consequence: current CI, broad source build and qualification are not green; reopen trigger: explicitly approved dependency/profile reconciliation, followed by those exact commands. The story's frozen dependency boundary prevents silently changing pins or discarding the external commit.
- Live authenticated registry publication, both-platform runtime health and registry immutability policy remain unverified. Owner: Jérôme Piquot; consequence: no live publication/Production qualification credit and no Story 30.5 activation; reopen trigger: an authorized credentialed run retaining the complete packet and immutability evidence. No release, remote setting, Production lifecycle write, commit or push was performed.
- Scope and readiness were checked against 22 exact owned paths. The preserved baseline additionally contains six gitlinks from the external workspace commit; machine-readable File List exclusions identify their owner. The spec and numeric sprint row are both `in-progress`, not `done`.

### Push follow-up (2026-11-03)

The user authorized committing and pushing all current changes. Meanwhile, external commit `3d6e466238fe7f78dc6216918f5e749f513cca9b` restored the approved Builds gitlink and extended the CI timeout. The clean Builds checkout was aligned to that already recorded `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`; no new dependency revision was selected. `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -q` now passes **17/17**, superseding the earlier current-tree pin-parity failure. The historical failures above remain accurate for their snapshots. Broad source-build/qualification checks and live publication were not rerun as part of this version-control request; those qualification checkpoints remain open. Both external CI-fix paths are excluded from this story's cumulative File List.

### Re-dispatch verification (2026-11-03)

The spec was dispatched again for implementation. Every Execution task was already implemented by `41f8bdca7ee5e8b719f600b95e22a43dbc26f1dc`, so no code, test, workflow or documentation path changed. Runs used the clean tree at that commit with the Builds checkout and gitlink both at `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`.

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`: **82/82 passed**, zero skips, 325.641 s.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v`: **17/17 passed**.
- `python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -v`: **50/50 passed**, 138.731 s.
- Broad qualification gate `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v`: **70/70 passed**, 30.882 s. This supersedes the earlier 3-failure/9-error snapshot, which was taken on the transitional checkout.
- Broad source gate `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m`: **passed**, 0 warnings and 0 errors, 45 project outputs including source `Hexalith.EventStore.Contracts`. This incremental build did not reproduce CS1704.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false --no-restore -m:1 -v:m` passed with 0 warnings and 0 errors. `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **138/138 passed**, zero skips. A freshness check confirmed the assembly contains the renamed `ReleaseConfiguration_PublishesFiveImageContractAndVersionedDeployment` test and not the former four-image name.
- `actionlint .github/workflows/release.yml`, `npm run verify:semantic-release-config --silent`, `python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -q` (4/4), Python AST parsing and PowerShell parsing of all changed scripts all passed.
- `tools/check-story-file-scope.py --changed-files-file` passed on the 22 story-commit paths, both with `--story-key` and with the `--commit-sha 41f8bdca` trailer form. On the raw `d959bdae..HEAD` diff (29 paths) it fails only on the seven paths listed under File List Exclusions; the Builds gitlink nets to zero. `tools/check-story-review-readiness.py` passed C1 (22/22), C6 and C4 on both inputs.
- Exact-source push CI run `41707067835` for `41f8bdca` was still in progress at 20:11 UTC. No CI green credit is claimed here.

All thirteen Checkpoint Evidence rows remain blocked and not complete. No registry credential was used, and no release, registry write, protected-setting change or Production lifecycle write took place. Story status stays `in-progress` until an authorized credentialed run retains the complete live evidence packet.

### Build verification fix (2026-11-03)

The parent checked the `d959bdae..HEAD` diff and the re-dispatch report against every task, acceptance criterion and matrix row. One gap came from the Story 30.3 deferred-work entry that names Story 30.4 edits to `tools/publish-containers.ps1` as its reopen trigger. `Write-ContainerSummary` always wrote "dispatch Recover Partial Release from main ... reconciled by config digest". The current five-image Python path writes Story 30.5 guidance, but a deployment-render failure on a current tree runs before that branch. It therefore recorded historical recovery guidance that contradicts the current fail-closed boundary and the aggregate summary.

- Fix: `tools/publish-containers.ps1` checks for `tools/release-containers.json` once, right after resolving the repository root. Current trees write "Stop and reconcile immutable evidence with Jérôme Piquot; current missing-only recovery belongs to Story 30.5." Historical tagged trees, staged by `recover-partial-release.yml` with `-RepositoryRoot`, keep the original text. The publication branch reuses the same `$currentContract` value. CRLF was preserved, and PowerShell parsing reports 0 errors.
- Regression: `test_render_failure_recovery_guidance_matches_the_source_tree_boundary` checks both source trees. Negative control: putting the old fixed text back failed only `current=True`, and restoring the fix returned the identical file (SHA-256 `4bb1e063…`).
- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`: **83/83 passed**, zero skips, 276.568 s.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v`: **17/17 passed**.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **138/138 passed**, no errors, skips or not-run cases. `ci_alignment` 12/12, `line_endings` 4/4 and `git diff --check` passed.
- Deferred work: the Story 30.3 entries "Qualify the five image project-to-repository mappings" (offline enforcement only; live checkpoint stays blocked) and "Align the container child summary's recovery text" are resolved. The 30.3 entry asking to regenerate `epic-30-context.md` also names Story 30.4 creation as its trigger. It stays open because that file is outside this story's File Scope.

### Review round 2 (2026-11-03)

The three layers reviewed the full `d959bdae..` diff, gitlinks included. The parent checked the working tree byte-identical against a pre-review snapshot before triage, because one layer temporarily mutated a source file during its probes. It then reproduced each high-severity claim itself before routing (Review Triage Log rows R2-*).

| Group | Findings | Correction and regression |
| --- | --- | --- |
| G1 | R2-B1, R2-V2, R2-V7, R2-E1, R2-E14 | Superseded on rebase by external commit 0433b4946e4f22b549ae88899b0ea44adbc62e73. Workloads keep their `--internal` network and publish no port; a digest-pinned BusyBox probe shares the workload's network namespace. The bridge variant was dropped. `DockerWorkloadHealthTests.test_real_docker_internal_network_probe_reaches_liveness_and_cleans_up` runs `run_platform` with real Docker (DomainService liveness against digest-pinned `jmalloc/echo-server`) on that design. |
| G2 | R2-E2, R2-E15 | Superseded on rebase by external commit 205baddda57911896d8c08708e80ecaf0bc14f82. The test authority writes a flat `access-telemetry-clock-key` secret, which the clock reads through its secret-name fallback. `multiValued` was dropped, and the clock test asserts the flat shape. |
| G3 | R2-V1, R2-V6, R2-E3, R2-E16 | Family summaries are compared after only trailing fractional-second zeros are normalized. A test drives the real pwsh aggregation over `.1234560Z`/`.0000000Z`, and a changed field or a different instant is still refused. Disabling normalization fails it. |
| G4 | R2-B6 | Approved-helper fixtures fetch exactly `81c88b34` into a shallow Builds checkout, fail loudly if that fetch fails, and skip with a reason only when no Builds checkout exists. The parent simulated a shallow checkout at `6e7011fc` (commit fetched, class green) and the recovery workflow's `submodules: false` layout (8 run, 2 skipped, no error) in a disposable worktree. |
| G5, G6 | R2-V3, R2-B10 | `main()` runs build, authorize and publish. Only build creates identity.json, and authorize writes the publish preflight record; removing that phase fails the test. An ambiguous 503 records owner, consequence and reopen trigger in failure-build.json with no SDK or transport command. The occupied/ambiguous test is narrowed to `version-collision`/`destination-probe-failure`. |
| G7 | R2-B11, R2-V5 | The source-input test first accepts the clean committed tree, then narrows each refusal to its message. |
| G8, G11 | R2-V4, R2-V8 | The receipt test asserts `cleanup` and `registry` and round-trips through `validate_authorization`. Probe receipts default to a per-call temporary path; the stale repository-local fixture receipt was removed. |
| G9, G12 | R2-B13, R2-E12 | Health logs keep the redacted exception type and message. The network counts as attempted only after creation succeeds. |
| G10 | R2-B9, R2-E13 | Without a container summary the aggregate source falls back to `HEXALITH_ZOT_REGISTRY`, then `registry.hexalith.com`. |
| G13 | R2-B12, R2-E8 | `EnableContainer` is case-insensitive. A two-project repository swap and an added `True` project are both refused. |
| G14 | R2-B15 | Runbook prerequisite 8 names the four digest-pinned health pulls: Redis Stack, FalkorDB, daprd (clock authority plus the Server/MCP sidecars from external commit 4f32c5d7417217cf99d37952fa0adb8a7a032784) and the BusyBox probe. |
| D1–D3 | R2-B7, R2-E5, R2-E4, R2-B3 | Deferred as unverified: the release-job timeout budget (high if real), skopeo exiting after the tag write, and real multi-RID SDK labels (medium if real). Each entry names an owner, consequence and reopen trigger. |

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`: **89/89 passed**, zero skips, 269.752 s.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -q`: **17/17**. `release_preflight`: **50/50** in 129.584 s. Broad `builds_publication_qualification`: **70/70** in 27.915 s.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **138/138**.
- These also passed: `ci_alignment`, `line_endings`, `npm run verify:semantic-release-config --silent`, `actionlint .github/workflows/release.yml`, Python compilation, PowerShell parsing of four release scripts (0 errors), `git diff --check`, and the story file-scope gate on all 12 working-tree paths.
- CI: run 41707067835 on 41f8bdca ended `cancelled`, with `test-unit-contract` failed at "Run container publish fixtures" (R2-B6, now corrected). No green CI credit exists for this story until the corrected tree's exact-source push CI succeeds.
- Live publication evidence is unchanged: all thirteen Checkpoint Evidence rows remain blocked and not complete.
- External workspace commits landed on `origin/main` during this review, touching only `.github/workflows/ci.yml` (already excluded) and `.github/workflows/release.yml` (already listed). 351f04a86068305bcc8ac3f945b3cbd121348f88 and 255efcd5bb2b4e00caf918e45bcd1d72b3b687f8 fetch `81c88b34` in CI, with retries, before the container fixtures; this complements G4's in-test fetch and recovery skip. 10ac2d676f8a61dc5359d46574b9709db28012a5 raises the release job budget to 120 minutes; D1 stays open because the budget is still unmeasured. Local `main` was fast-forwarded to it with no path overlap. Dispatch 17/17, `ci_alignment`, actionlint on both workflows and C# inventory 138/138 were then rerun and passed.
- Status decision: the build workflow's present step would mark this spec `done`. It stays `in-progress`, because the recorded rule is that the story stays in progress until an authorized credentialed run retains the live packet, and `done` would satisfy Story 30.5's activation gate ("Story 30.4 must complete") without live evidence.

### Rebase onto origin/main (2026-11-04)

Before the push, six external commits (49c913da…, 4536ae93…, 0433b494…, 205baddd…, 4f32c5d7…, 82c0aa0a…; tags v3.0.19–v3.0.23) had reworked the same release tooling. Commit `6e6b7341` was rebased onto `82c0aa0a7046d7bd91569ac1c9a38a3ed0386d0f`. Where both changed the same behaviour, the external design was kept, because it comes from those release attempts:

- `tools/verify-container-health.py`: kept `--internal` with the in-namespace BusyBox probe, the flat clock secret and the Dapr sidecars. Retained only G9 (logged failure cause) and G12 (network counts as attempted only after creation).
- `docs/dev/release-runbook.md`: took the external health paragraph and kept G14's prerequisite list, extended to four images.
- `publication_contract_test.py`: kept the external probe, sidecar and OCI-layout assertions; kept this story's cleanup-cause, network-creation-failure (now expecting `--internal`), CLI-phase and fixture-fetch tests; adapted the real-Docker and clock tests to the external design.
- The other paths merged without conflict. This story claims no live evidence from those release runs; the thirteen Checkpoint Evidence rows remain blocked until their owner records the retained packet.
- Rebased-tree verification: `publish_containers` **90/90** (zero skips; +1 external materialization test), `publish_nuget` **32/32**, `release_preflight` **50/50**, broad `builds_publication_qualification` **70/70**, `release_dispatch` 17/17, `ci_alignment`, `line_endings`, semantic-release config, actionlint on both workflows, C# inventory **138/138** and `git diff --check`. The first real-Docker run failed one BusyBox pull transiently; rerunning, including from a cleared image cache, passed, and the assertion now reports the pull output.
