---
title: 'Five-image publication contract'
type: 'chore'
created: '2026-11-03'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
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
- Live evidence (2026-11-10): authorized credentialed Release runs 42108808279 (v3.0.24, source 859680b3) and 42134622308 (v3.0.25, source 246157a7) ran this contract after 4dac7dec. Each attached `hexalith-memories-release-evidence.tar.gz` and its `.sha256.json` receipt to its GitHub release. v3.0.25's source differs from HEAD only in Playwright E2E package files. Anonymous registry reads return 401; the local Docker credential store can read manifests. Read only: never push, tag, retag or delete.
- Multi-RID runtime config (review R3-B1). SDK 10.0.403 `Containers/build/Microsoft.NET.Build.Containers.targets` passes env vars to each inner RID build as `_ContainerEnvironmentVariables=@(ContainerEnvironmentVariable->'%(Identity):%(Value)')` (line 338). `_ParseItemsForPublishingSingleContainer` then rebuilds `ContainerEnvironmentVariables` with `.Split(':')[1]` (lines 394-400), so any value containing `:` is cut. The Server's declared `PATH` (`src/Hexalith.Memories.Server/Hexalith.Memories.Server.QualificationCurl.targets`) therefore ships as `PATH=/app` in every v3.0.23–v3.0.25 Server child. The deployment's exec probes then exit 127 (`wget: not found`). The other four images declare no colon-bearing value. Each inner build evaluates the project's own `ContainerEnvironmentVariable` items.
- Precedent: the approved `Props/ContainerProvenance.targets` target `RebindContainerProvenanceLabels` (`BeforeTargets="_PublishSingleContainer"`) removes and re-includes labels the same way. The single-RID Docker deployment archive keeps the declared config, so it is the parity reference. Published tags are immutable: never retag; only a new release carries the fix.

- Authorized Release run 43012088139 on source `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb` selected 3.0.26 but failed in `prepare` before publication. The diagnostic artifact `release-publication-evidence` (13850599888, ZIP SHA-256 `cb9e403a3d7c77a88e45c6433a94db9eff00d05424a96f37b9dcec75fefa1ee9`) identifies only MCP as failed; the other four archives passed runtime-config parity. MCP's multi-RID publish raises MSB4184 in SDK 10.0.404 `_ParseItemsForPublishingSingleContainer`, line 384 (`Split(':')[1]`). The same parser is present in local SDK 10.0.403.
- External commit `1143ba17a2336f0d718fcff71e049c06350a45a2` prefixed MCP's `Description` with `Compatibility binary, not retired; see ...`. The SDK generates both OCI description labels from that property, serializes labels into `_ContainerLabel` as `Identity:Value`, and reconstructs them as an MSBuild item list. The literal semicolon creates a description fragment without `:`, so parsing fails before the approved provenance rebinding target can run. Replace only that punctuation with `. See`; preserve the compatibility warning, README pointer, package identity and all dependencies/settings. Do not add a second label parser or modify SDK/shared Builds code.
- Extend the existing publication-contract tests with the real SDK boundary: obtain the actual project's generated `ContainerLabel` items through `ComputeContainerConfig`, then run `_ParseItemsForPublishingSingleContainer` with that transported list for both inner RIDs. The current description must parse successfully and retain both description-label values; a test-only old-semicolon negative control must reproduce MSB4184. This closes a different gap from the existing seven-provenance-label evaluation test. An SDK-target evaluation requires no image transport or registry write.

## Tasks & Acceptance

**Execution:**
- [x] `tools/release-containers.json`, `tools/container-publication-contract.py` — freeze exact project mappings, version, source, workflow run and approved SHA; validate the set before invoking shared phases once each.
- [x] `Directory.Build.targets`, `tools/publish-containers.ps1`, `tools/pack-release.ps1`, `tools/publish-release.ps1` — import approved provenance; build separate two-platform OCI archives; copy all children with preserved digests; retain stable deployment archives, rendering and aggregate interfaces.
- [x] `tools/verify-container-health.py`, `tools/verify-container-registry.ps1` — digest-bound per-platform health contracts and redacted auth results. Server/MCP/DomainService prove liveness; lifecycle proves disabled readiness; clock proves signed readiness with isolated test authorities. Failed cleanup fails verification.
- [x] `tools/archive-release-evidence.py`, `.github/workflows/release.yml`, `.releaserc.json`, `tools/verify-semantic-release-config.mjs` — hash all mapping/identity/auth/OCI/health records; reject omissions; upload failure diagnostics and retain successful evidence as a release asset.
- [x] `tests/tooling/publish_containers/*_test.py`, `tests/tooling/release_dispatch/*_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` — cover matrix failures, health, transport and compatibility.
- [x] `docs/dev/release-runbook.md` — document five-image contract, retained evidence, auth mode and current blockers.
- [x] `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md` — reconcile every Checkpoint Evidence row with the retained v3.0.25 packet: check the receipt hash, read the row's artifact, and cross-check its digests against the registry. Mark a proven row `verified <YYYY-MM-DD>: <run, artifact, digest>` and `complete`; keep an unproven row blocked with owner, consequence and reopen trigger. Rerun the Verification suites on the current tree and record exact results.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — for each Story 30.4 entry whose reopen trigger the authorized runs fired (release-job step timings, multi-RID provenance labels, exact-source CI), add dated evidence and resolve it only when the trigger is fully met. Entries whose trigger has not fired stay open.
- [x] `Directory.Build.targets` — add a target that runs `BeforeTargets="_PublishSingleContainer"`, only in a single-RID inner build of a multi-RID container publish. It removes each parsed `ContainerEnvironmentVariables` item that the project also declares as `ContainerEnvironmentVariable`, then re-includes the declared item with its full `Value`. Single-RID publishes stay byte-identical.
- [x] `tools/container-publication-contract.py`, `tools/archive-release-evidence.py` — in the build phase, before any preflight write or transport, refuse unless each OCI child's config `Env`, `Entrypoint`, `Cmd`, `WorkingDir`, `User` and `ExposedPorts` equal the config of that image's single-RID Docker deployment archive. Write any mismatch to the member's build diagnostics. Record the compared values' SHA-256 per archive in `archive-compatibility.json`, and have the archiver require it. `run()` must keep partial output in its log and refuse with a redacted error when a command times out (R3-E3).
- [x] `tests/tooling/publish_containers/publication_contract_test.py`:
  - (a) Evaluate the Server project's inner-build items with the release multi-RID properties via `dotnet msbuild`, following `test_approved_target_evaluates_canonical_provenance_labels_for_all_projects`. Assert the full declared `PATH` and the absolute `ContainerAppCommand`. A negative control without the new target yields `PATH=/app` (R3-B1, R3-V3).
  - (b) A runtime-config mismatch between a child and its archive refuses before transport, with a matching positive control.
  - (c) Drive the real `verify-container-health` `verify()` and `main()`, mocking shared helpers as `execution()` does and stubbing `run_platform` to pass and to fail. The passing `health.json` satisfies `archive-release-evidence.verify`'s health checks; the failing run exits nonzero (R3-V2).
  - (d) A timed-out command keeps its partial log (R3-E3).
- [x] `tests/tooling/publish_containers/publish_containers_test.py`, `tests/tooling/publish_containers/release_orchestration_test.py` — run the real `publish-containers.ps1` on a current tree, with render succeeding and a fake `python3` first on PATH:
  - `-PublicationContract` invokes `--phase build`, and `-PublicationContract -Push` invokes `--phase publish`, each with the registry and output directory.
  - A nonzero exit fails the script, and no legacy copy follows.
  - `pack-release.ps1` passes `-PublicationContract` (R3-V1).
- [x] `tests/tooling/publish_containers/registry_authorization_test.py` — run the real probe without `-EvidencePath` from a temporary working directory. The receipt lands where the authorize phase reads it by default, and `validate_authorization` accepts it (R3-V4).
- [x] `docs/dev/release-runbook.md` — document the dated effective retention policy: the historical 30-day cap on v3.0.24/v3.0.25 and the verified current 90-day policy from the authorized 2026-11-11 follow-up. Retain the authorized-run evidence and do-not-deploy warning for the affected Server images (R3-B5 and its retention follow-up).
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`:
  - Add a `## Deferred from: code review of spec-30-4-five-image-publication-contract.md (2026-11-03)` heading above the first existing Story 30.4 entry (R3-B11).
  - In the D1 resolution, note that five sequential 1800 s copies can still outlast the budget, and that the cancellation path stays as recorded in R2-B7 (R3-B7).
  - In the cohort resolution, cite owner commits 38572369 and 246157a7 as the profile reconciliation (R3-B9a).
  - Narrow the retention entry to the workflow/repository-setting mismatch.
  - Add an entry, with owner, consequence and reopen trigger, recording that the published v3.0.23–v3.0.25 Server images carry `PATH=/app` and must not be deployed.
- [x] `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`:
  - Reopen the Server health and Archive compatibility Checkpoint Evidence rows as blocked on the v3.0.25 `PATH=/app` evidence. Owner: Jérôme Piquot. Consequence: no v3.0.23–v3.0.25 Server image may be deployed. Reopen trigger: the next authorized release retains a packet whose children pass runtime-config parity.
  - Add a blocked "Runtime config parity" row with the same accountability, and keep the other eleven rows.
  - Rerun the Verification suites, plus one real multi-RID `PublishContainer` archive of the Server project, inspected for the full `PATH` on both children.

- [x] `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` — replace the description prefix's `not retired; see` with `not retired. See`, preserving its compatibility/removal-gate wording and every other property/reference.
- [x] `tests/tooling/publish_containers/publication_contract_test.py` — exercise real SDK-generated MCP description labels through the multi-RID inner label parser on both platforms, including a semicolon negative control that reproduces the failed authorized release. Verify red before the punctuation fix and green after it, then run the focused publication-contract tests and the relevant source Debug build. A real Debug multi-RID MCP archive may supplement the target-boundary proof; do not publish any image locally.
- [x] `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`, `_bmad-output/implementation-artifacts/deferred-work.md` — retain the failed release's identity, diagnostic hash, no-publication proof and named blocker. Keep the three live checkpoints and `in-progress` state until an authorized corrected release succeeds. Prepare the changed-source fix for review; no commit, push or changed-source release is part of this local amendment.

**Acceptance Criteria:**
- Given a semantic-release version, when publishing, then exactly the five canonical mappings share source/workflow/set identity and each passes declared platform and health checks.
- Given registry authorization, when verified, then record the actual mode and any accountable blocker without treating preemptive upload probes as push proof.
- Given production verification, when consuming archives, then all five stable filenames remain same-source compatible and rendering remains caller-owned.

## Checkpoint Evidence

Reconciled 2026-11-10 against the retained v3.0.25 packet from authorized Release run 42134622308 (source `246157a7ff8e2dcd8d356c833a77a02514490d2f`, set `8a10e0216545fa04f03c3ecef2aa5baf1eaedd6a51b1eaa4111c40cfbf941dfa`). Artifact paths are relative to `artifacts/release/container-evidence/` inside the `hexalith-memories-release-evidence.tar.gz` release asset. Its receipt SHA-256 `dfa54c81c8da35e002e99527f80a29a1bf8a0b3574876056bb3b81335f1d5c9f` matches the downloaded bytes, and all 123 manifest entries re-hash. Digests are 12-hex prefixes of values retained in full in the packet. Registry cross-checks are read-only `docker buildx imagetools inspect --raw` comparisons; no push, tag, retag or delete was issued. Health ran in Development with test-only authorities, so these rows grant no Production qualification credit (DW-17 and DW-730 are unchanged).

Review round 3 reopened Server health and Archive compatibility and added Runtime config parity. A read-only `docker buildx imagetools inspect` on 2026-11-10 returned `PATH=/app` on both Server children of v3.0.23, v3.0.24 and v3.0.25, while the Server declares the full `PATH`. Review round 4 (R4-B1) widened that read: v3.0.20, v3.0.21 and v3.0.22 also hold all five two-platform indexes whose Server children carry `PATH=/app`, v3.0.20 and v3.0.21 also keep the relative `dotnet` entrypoint so they cannot start, and v3.0.19 pushed no image. No packet check compared child runtime config with the deployment archive, so these three rows stay blocked until the next authorized release. The other eleven rows are unchanged.

Read-only follow-up 2026-11-11: the user authorized GitHub and registry evidence reads. GitHub still lists v3.0.25 as the newest release, and complete authenticated tag lists for all five repositories end at 3.0.25. Both published Server children still carry `PATH=/app`; no corrected release packet exists in the observed release set. The three blocked rows remain blocked. Current-source push CI and Builds qualification are now verified separately; see the dated follow-up below.

| Checkpoint | Owner | Artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Five mappings | Jérôme Piquot | identity.json | verified 2026-11-10: run 42134622308, identity.json (sha256 2d3325221210); the five project/repository/health mappings equal the current contract's CANONICAL_IMAGES and the packet manifest (sha256 2c31ab9b16ca, byte-identical to HEAD); source and workflow 246157a7, set 8a10e0216545 recomputes, and all seven helper hashes are approved Builds 81c88b34 blobs; all five 3.0.25 index digests match the registry | complete |
| Server platforms | Jérôme Piquot | server/oci-validation.json | verified 2026-11-10: run 42134622308, server/oci-validation.json (sha256 08b632e1d3cc), approved validator pass; index sha256:a857a0865ff3, linux/amd64 sha256:41a099ac9a60, linux/arm64 sha256:a64a9f9db671; the registry index and both child manifests are byte-identical, the remote graph equals local/, and both child configs carry the revision 246157a7 and version 3.0.25 labels | complete |
| MCP platforms | Jérôme Piquot | mcp/oci-validation.json | verified 2026-11-10: run 42134622308, mcp/oci-validation.json (sha256 5ad4d4c118f8), approved validator pass; index sha256:eabccac79d8a, linux/amd64 sha256:e4eb03b76399, linux/arm64 sha256:e256cc6495fa; the registry index and both child manifests are byte-identical, the remote graph equals local/, and both child configs carry the revision 246157a7 and version 3.0.25 labels | complete |
| Lifecycle platforms | Jérôme Piquot | access-telemetry/oci-validation.json | verified 2026-11-10: run 42134622308, access-telemetry/oci-validation.json (sha256 d87187846d8d), approved validator pass; index sha256:d227044d62db, linux/amd64 sha256:70d561e816de, linux/arm64 sha256:d7b9d5b550ac; the registry index and both child manifests are byte-identical, the remote graph equals local/, and both child configs carry the revision 246157a7 and version 3.0.25 labels | complete |
| Clock platforms | Jérôme Piquot | access-telemetry-clock/oci-validation.json | verified 2026-11-10: run 42134622308, access-telemetry-clock/oci-validation.json (sha256 408d0b440f60), approved validator pass; index sha256:e4edd4f62a43, linux/amd64 sha256:b57aff58b209, linux/arm64 sha256:3e9322fa7691; the registry index and both child manifests are byte-identical, the remote graph equals local/, and both child configs carry the revision 246157a7 and version 3.0.25 labels | complete |
| Domain platforms | Jérôme Piquot | domain-service/oci-validation.json | verified 2026-11-10: run 42134622308, domain-service/oci-validation.json (sha256 b8f72318053a), approved validator pass; index sha256:90d15d551cb1, linux/amd64 sha256:ccce28085804, linux/arm64 sha256:cc52db2176b8; the registry index and both child manifests are byte-identical, the remote graph equals local/, and both child configs carry the revision 246157a7 and version 3.0.25 labels | complete |
| Server health | Jérôme Piquot | server/health.json | blocked 2026-11-10 on the v3.0.25 `PATH=/app` evidence. Run 42134622308 server/health.json (sha256 e8fd2c723a60) records `/alive` liveness pass with cleanup pass on linux/amd64 sha256:41a099ac9a60 and linux/arm64 sha256:a64a9f9db671 through the external BusyBox probe. Both children nevertheless carry `PATH=/app`, as do the Server children of v3.0.20–v3.0.24, so the Production overlay's `wget` exec startup, readiness and liveness probes exit 127 and can never pass; consequence: no v3.0.20–v3.0.25 Server image may be deployed; reopen trigger: the next authorized release retains a packet whose children pass runtime-config parity | not complete |
| MCP health | Jérôme Piquot | mcp/health.json | verified 2026-11-10: run 42134622308, mcp/health.json (sha256 3aac6db4bb00); liveness `/alive` in Development, pass with cleanup pass on linux/amd64 sha256:e4eb03b76399 and linux/arm64 sha256:e256cc6495fa, both registry-matched; bound to oci-validation sha256 5ad4d4c118f8 and set 8a10e0216545 | complete |
| Lifecycle health | Jérôme Piquot | access-telemetry/health.json | verified 2026-11-10: run 42134622308, access-telemetry/health.json (sha256 d11733dae1e0); disabled readiness `/ready` in Development, pass with cleanup pass on linux/amd64 sha256:70d561e816de and linux/arm64 sha256:d7b9d5b550ac, both registry-matched, lifecycleWritesEnabled false; bound to oci-validation sha256 d87187846d8d and set 8a10e0216545 | complete |
| Clock health | Jérôme Piquot | access-telemetry-clock/health.json | verified 2026-11-10: run 42134622308, access-telemetry-clock/health.json (sha256 7cf65567ed28); signed readiness `/ready` in Development, pass with cleanup pass on linux/amd64 sha256:b57aff58b209 and linux/arm64 sha256:3e9322fa7691, both registry-matched, with an ephemeral P-256 key and isolated Dapr secret authority; bound to oci-validation sha256 408d0b440f60 and set 8a10e0216545 | complete |
| Domain health | Jérôme Piquot | domain-service/health.json | verified 2026-11-10: run 42134622308, domain-service/health.json (sha256 35caf360882a); liveness `/alive` in Development, pass with cleanup pass on linux/amd64 sha256:ccce28085804 and linux/arm64 sha256:cc52db2176b8, both registry-matched; bound to oci-validation sha256 b8f72318053a and set 8a10e0216545 | complete |
| Registry authorization | Jérôme Piquot | authorization.json | verified 2026-11-10: run 42134622308, authorization.json (sha256 54d88cbdf701) records mode preemptive-basic, transport skopeo-authfile, a 401 challenge, five 202 upload sessions, pushProven false and cleanup pass; push success rests on the five transported graphs whose index digests match the registry, never on the probe; anonymous `/v2/` and manifest reads returned 401 at 2026-11-10T09:14Z | complete |
| Archive compatibility | Jérôme Piquot | archive-compatibility.json | blocked 2026-11-10 on the v3.0.25 `PATH=/app` evidence. Run 42134622308 archive-compatibility.json (sha256 fc7dea100bad) binds five stable docker-archive filenames and their .oci.tar.gz members to source 246157a7 and set 8a10e0216545, the index digests match the registry, and a caller render from a fresh 246157a7 export is byte-identical (sha256 499c0a9b17e3) to the packet deployment and the release asset. The receipt records no runtime-config comparison, and the Server OCI children of v3.0.20–v3.0.25 carry `PATH=/app` while a single-RID Docker deployment archive keeps the declared full `PATH`, so the members are not runtime-compatible; consequence: no v3.0.20–v3.0.25 Server image may be deployed; reopen trigger: the next authorized release retains a packet whose children pass runtime-config parity | not complete |
| Runtime config parity | Jérôme Piquot | archive-compatibility.json runtimeConfig and `<member>/runtime-config-parity.json` | blocked 2026-11-10. The v3.0.20–v3.0.25 releases predate this check and record no child-versus-archive comparison of `Env`, `Entrypoint`, `Cmd`, `WorkingDir`, `User` and `ExposedPorts`. Read-only registry reads show `PATH=/app` on both Server children of all six versions, and v3.0.20 and v3.0.21 also keep the relative `dotnet` entrypoint. The rebinding target and the parity check pass offline and on local real multi-RID builds of all five images, which grant no release credit; consequence: no v3.0.20–v3.0.25 Server image may be deployed; reopen trigger: the next authorized release retains a packet whose children pass runtime-config parity | not complete |

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
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`
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
- 2026-11-10 (round-3 amendment G-A): `RebindMultiRidContainerEnvironmentVariables` in `Directory.Build.targets` runs before `_PublishSingleContainer` only when an inner build receives `_ContainerEnvironmentVariables` with `_IsMultiRIDBuild=false`. It replaces each parsed environment variable the project also declares with the declared item and its full value, keeping declaration order. The build phase compares each OCI child's `Env`, `Entrypoint`, `Cmd`, `WorkingDir`, `User` and `ExposedPorts` with the single-RID Docker deployment archive, writes `<member>/runtime-config-parity.json`, and fails the member on any mismatch. `archive-compatibility.json` records the compared values' SHA-256 for the archive and each child; `validate_archives` (authorize, publish and archiver) recomputes them, and the archiver also requires a passing diagnostics file. `run()` keeps partial output in its log and refuses with a redacted error on timeout. `DEFAULT_EVIDENCE_DIRECTORY` names the authorize phase's default, which the registry probe's default receipt path must match.

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
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`
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
| 2026-11-10 | dev-story | Reconciled all 13 Checkpoint Evidence rows against the retained v3.0.25 packet and the registry, and marked each verified and complete. Resolved three Story 30.4 deferred entries whose triggers the authorized runs fired (release-job timings, multi-RID labels, exact-source CI), recorded two whose triggers did not fire, and added one retention/runbook entry. No code, test, workflow, setting or dependency changed. | Container fixtures 90→90 (+0); dispatch 17→17 (+0); preflight 50→50 (+0); broad qualification 70→70 (+0); NuGet 32→32 (+0); C# inventory 138→138 (+0); no test added. | Matched 22/22 story paths; only this spec and deferred-work.md changed, both already listed. |
| 2026-11-10 | dev-story | Implemented the round-3 G-A amendment. `RebindMultiRidContainerEnvironmentVariables` restores declared environment values in multi-RID inner builds. The build phase refuses any OCI child whose runtime config differs from its Docker deployment archive and records per-archive SHA-256 values that authorize, publish and the archiver recompute. `run()` keeps partial logs on timeout. Added the R3-V1, R3-V2 and R3-V4 tests, aligned the runbook (30-day cap, authorized runs, do-not-deploy), updated five deferred-work items, and reopened Server health and Archive compatibility beside a new blocked Runtime config parity row. No dependency, setting, workflow or Production state changed, and nothing was published. | Container fixtures 90→97 (+7: contract 40→45, publish_containers 25→26, registry authorization 9→10; release orchestration 7→7 with a strengthened assertion); dispatch 17→17; preflight 50→50; broad qualification 70→70; NuGet 32→32; C# inventory 138→138; Server deployment artifacts 14→14. | Matched 22/22 story paths. Ten paths changed (`Directory.Build.targets`, the contract and archiver tools, four `publish_containers` tests, the runbook, this spec and deferred-work.md), all already listed. |
| 2026-11-10 | code-review patch | Applied the round-4 patch findings. The rebinding test now also runs the SDK inner target list with a stub `_PublishSingleContainer` that records the items reaching image creation (R4-V1, R4-B6). A new test drifts each of `Entrypoint`, `Cmd`, `WorkingDir`, `User` and `ExposedPorts` alone and pins the six-field tuple (R4-V2, R4-B8). The archiver's `--evidence-directory` defaults to `contract.DEFAULT_EVIDENCE_DIRECTORY` (R4-B14). The runbook states when the parity comparison runs (R4-E15), and the runbook, deferred work and the three blocked rows now cover v3.0.20–v3.0.25 (R4-B1, R4-B3). Deferred work also corrects the D1 timeout note (R4-B10) and the R3-B6 line and pin references (R4-B13). No remote action was taken. | Container fixtures 97→98 (+1: contract 45→46; the rebinding test gains two stub-boundary subtests). Focused runs: the rebinding, per-field, diagnostics-archive and malformed-identity cases passed 4/4. Moving the hook to `AfterTargets` or to `BeforeTargets="_ParseItemsForPublishingSingleContainer"`, or narrowing the fields to `("Env",)`, each turned its test red. | Matched 22/22 story paths. Changed this round: `publication_contract_test.py`, `tools/archive-release-evidence.py`, the runbook, deferred-work.md and this spec, all already listed. |
| 2026-11-11 | dev-story | Revalidated the already implemented contract at db769a95e2966dd42f5523ce091e7f8f317f22a4 without code changes. Recorded the current Builds profile drift and cleared stale source assets with an offline cache-only restore; retained the three blocked live checkpoints and in-progress status. | No tests added; selected container fixtures 97/97, preflight 50/50, NuGet 32/32, C# inventory 138/138. Dispatch and broad qualification are blocked; exact results follow. | Matched 2/2 working-tree paths: this spec and deferred-work.md, both already listed. |
| 2026-11-11 | dev-story | Revalidated the already implemented contract at f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb after the separately authorized Builds profile reconciliation. Current dispatch and broad qualification pass; preserved the three blocked live checkpoints, exact-source CI hold and in-progress status. Only this evidence record changed. | No tests added; selected container fixtures 97/97, dispatch 17/17, preflight 50/50, broad qualification 70/70, C# inventory 138/138. The Docker pull case is excluded and uncredited; the interrupted unaffected NuGet rerun grants no current result. | Matched 1/1 working-tree paths: this spec, already listed. |
| 2026-11-11 | dev-story | Per explicit user authorization, read GitHub and registry evidence without remote writes. Verified current-main push CI and Builds qualification at f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb, resolved the current Builds-profile ledger entry, and confirmed no corrected release exists. The three live checkpoints and in-progress status remain unchanged. | No local tests rerun. Exact-source CI container fixtures passed 98/98 with zero skips, including the pull-dependent case excluded locally; current CI and qualification runs succeeded. | Matched 2/2 working-tree paths: this spec and deferred-work.md, both already listed. |
| 2026-11-11 | dev-story | Repaired the authorized 3.0.26 prepare failure by replacing the MCP description semicolon with `. See`, preserving all other project bytes. Added a real SDK-generated-label regression on both inner RIDs, retained the failed release identity/diagnostic/no-publication evidence in the spec and ledger, and kept the three live checkpoints and in-progress hold. | One test added: publication-contract discovery 46→47 and total container discovery 98→99; new test red on both original-description RIDs, green after the punctuation fix, with both old-semicolon controls still reproducing MSB4184. Focused offline selection 46/46; MCP source Debug build 0 warnings/errors. | Matched 4/4 working-tree paths: MCP project, publication-contract test, this spec and deferred-work.md; all listed. |
| 2026-11-11 | dev-story | Reconciled the release runbook and retention ledger with the verified current 90-day repository policy; preserved historical 30-day observations and all live-checkpoint holds. | No tests added or rerun for this documentation-only follow-up; the MCP amendment's focused 46/46 and Debug build results remain current. | Matched 5/5 working-tree paths: the project/test repair plus spec, deferred-work.md and runbook, all listed. |


## Spec Change Log

- 2026-11-10 — review round 3, loopback 1 (bad_spec, group G-A: R3-B1, R3-E1, R3-V3).
  - **Trigger:** the published multi-RID Server children carry `PATH=/app`, cut by the SDK 10.0.403 inner-build `Split(':')`. The Production overlay's exec probes therefore exit 127, yet every publication check and Checkpoint row passed.
  - **Amended:** the Code Map (root cause, rebinding precedent, parity reference, immutable-tag rule) and the Tasks. The new tasks add inner-build env rebinding in `Directory.Build.targets` and a child-versus-archive runtime-config parity check before any write. They also carry the patch-routed findings R3-V1, R3-V2, R3-V4, R3-B5, R3-B7, R3-B9a, R3-B11 and R3-E3, and reopen the affected checkpoint rows.
  - **Known-bad state avoided:** a release whose registry image runtime config silently differs from the verified same-source deployment archive while passing every publication check.
  - **KEEP:**
    - all implementation committed in 41f8bdca and 4dac7dec;
    - the external release-attempt designs (in-namespace BusyBox probe, flat clock secret, Dapr sidecars, OCI layout transport) and the absolute `ContainerAppCommand`;
    - historical archive and recovery behaviour;
    - the 2026-11-10 live-evidence reconciliation: packet and registry cross-checks, the unaffected checkpoint rows, and the D1, D3 and cohort resolutions.
  - **Revert:** none. This run made no code change, and the committed implementation is already released.

- 2026-11-11 — reconciled the retention task with fresh production-policy evidence: the GitHub retention API now reports both configured and maximum days as 90, and the failed release's artifact retains that interval. The task/runbook retain the earlier 30-day observations as history. The frozen intent is unchanged and no remote setting was modified.

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
| R3-B1 | high | bad_spec (G-A) | Parent reproduced. Both Server children in v3.0.23 and v3.0.25 carry `Env` `PATH=/app`. Inside child sha256:41a099ac9a60, `/bin/sh -c 'wget …'` exits 127 (`wget: not found`), so the Production overlay's exec startup, readiness and liveness probes can never pass. Root cause: SDK 10.0.403 multi-RID inner builds receive env vars as `%(Identity):%(Value)` and parse `.Split(':')[1]`. The external BusyBox health probe cannot see this, and no check compares child runtime config. The live cluster stays Ready only because it runs the older single-platform `71e49b6e` image. |
| R3-E1 | high | bad_spec (G-A) | Same multi-RID PATH truncation as R3-B1. |
| R3-V3 | medium | bad_spec (G-A) | Pre-verified gap. No check evaluates the multi-RID Server runtime command or environment before push; same root-cause group as R3-B1. |
| R3-V1 | medium | patch (carried into the G-A amendment) | Pre-verified gap. No test runs the real `publish-containers.ps1` contract hand-off. Hard-coding `$phase`, deleting the `return`, or dropping `-PublicationContract` from `pack-release.ps1` keeps every test green. |
| R3-V2 | medium | patch (carried into the G-A amendment) | Pre-verified gap. `verify-container-health.verify()` and `main()` never run in tests, so producer/consumer `health.json` drift passes CI. |
| R3-V4 | medium | patch (carried into the G-A amendment) | Pre-verified gap. No test covers the default receipt path shared by `verify-container-registry.ps1` and the authorize phase. |
| R3-V5 | low | reject | Stale undated Implementation Notes. The fix edits this build's spec. |
| R3-V6 | low | reject | Status wording mismatch. The fix edits this build's spec, and sprint status syncs at step 5. |
| R3-B2 | low | reject | Archive compatibility row wording. The fix edits this build's spec; the G-A amendment reopens that row on the PATH evidence. |
| R3-B3 | low | reject | Same as R3-V6. |
| R3-B4 | low | reject | Same as R3-V5, plus the undated credential and "remain unverified" lines. Spec edit. |
| R3-B5 | medium | patch (carried into the G-A amendment) | `docs/dev/release-runbook.md:183` promises 90-day diagnostics, but both authorized runs logged the 30-day repository cap. Lines 188-191 still say credentials are absent and publication is unqualified. This story wrote both passages (41f8bdca) and owns the runbook. Direct documentation correction. |
| R3-B6 | low | defer | Runbook lines 44 and 50 ("Seven Coverlet project reports", "20-minute build timeout") and `CONTRIBUTING.md:507` predate the baseline (aa097b52, Story 30.1). External commits 3d6e4662 and 246157a7 made them stale; this story did not. |
| R3-B7 | low | patch (carried into the G-A amendment) | Five sequential `skopeo copy` calls each allow 1800 s (`run()` timeout), so a slow-failing registry can outlast the 120-minute job. The D1 resolution must say so. The cancellation/alert sub-claim is carried from R2-B7 (reject, low). |
| R3-B8 | low | reject | File List Exclusions versus the raw baseline diff. The fix edits this build's spec; C1 is skipped on `main`. |
| R3-B9a | low | patch (carried into the G-A amendment) | The cohort resolution does not cite the owner's reconciliation commits 38572369 and 246157a7, which satisfy "explicitly approved". Direct text correction in deferred-work.md. |
| R3-B9b | maybe-false | defer | `ci.yml:171` calls `domain-ci.yml@6e7011fc`, while `tools/builds-publication-qualification.json` binds 5fa2b97c. Both moved through owner commits, and no test asserts how they should relate. Medium if unintended. Settle with an owner statement or a pin-relation test. |
| R3-B10 | maybe-false | defer | External commit 49c913da re-pinned the SHA-256 values for the same curl 8.20.0 musl assets with no recorded reason. Medium-to-high if the upstream bytes were not legitimately re-released. Settle against upstream release checksums or notices. |
| R3-B11 | low | patch (carried into the G-A amendment) | The Story 30.4 deferred entries sit under `## Deferred from: Story 30.3 resumed build review (2026-11-03)`, after a stray blank line. Add a Story 30.4 heading (direct correction). |
| R3-B12 | low | reject | Change Log and Triage Log completeness. Spec edit. |
| R3-B13 | low | reject | An offline retained-packet verifier would be new surface. Consuming immutable evidence belongs to Story 30.5, per the epic context. |
| R3-B14 | false | reject | The archiver runs inside `publishCmd` (`publish-release.ps1`). A missing packet fails semantic-release, so "Detect release publication" never runs in that state. |
| R3-B15 | low | reject | Sidecar wording in the health rows. Spec edit. |
| R3-B16 | false | reject | `approved_builds_checkout()` fetches only when `git cat-file -e` finds the commit missing. Full developer clones never fetch, and on-demand recursion touches only initialized submodules. |
| R3-E2 | low | reject | In build mode, `write_summary` mirrors the legacy `publish-containers.ps1:244-256` family labels. `push: false` and the per-image `build-failed` disposition disambiguate, and a new label would change a shared summary vocabulary. |
| R3-E3 | medium | patch (carried into the G-A amendment) | `run()` calls `subprocess.run(..., timeout=1800)` and writes its log only after it returns. `TimeoutExpired` therefore leaves no `transport.log` or `build-oci.log`, contradicting the matrix requirement to preserve diagnostic evidence. |
| R3-E4 | false | reject | Only this job writes its workspace archives between validation and copy, and no concurrent writer exists (same reasoning as R2-E10). |
| R3-E5 | low | reject | carried: R2-B12/R2-E8 rejected the props, import, condition and namespace sub-claims as unreachable here (plain csproj files under src). |
| R3-E6 | low | reject | carried: R2-B5/R2-E9 rejected widening the ignored-input guard (fresh CI checkout; risk of refusing build-generated ignored files). |
| R3-E7 | low | reject | A `docker run --detach` create-then-start failure for the Dapr authority is rare on ephemeral runners. A safe fix needs a guarded removal that does not mask the original error. |
| R3-E8 | maybe-false | reject | External ingestion commit 3a62bfe1. It is unshown whether another workflow type can occupy the previous instance id, and if real the harm is a wrong error class (low). Settle with a test that stores a foreign workflow input under that id. |
| R4-V1 | medium | patch (group H1) | Pre-verified gap. Test (a) names `RebindMultiRidContainerEnvironmentVariables` in `-target:` and hand-sets the SDK hand-off. Moving the hook to `AfterTargets="_PublishSingleContainer"` or `BeforeTargets="_ParseItemsForPublishingSingleContainer"` therefore keeps it green. The approved Builds provenance test drives the SDK target list with a stubbed `_PublishSingleContainer` instead. |
| R4-V2 | medium | patch (group H2) | Pre-verified gap. Only `Env` drift is mutated. Narrowing `RUNTIME_CONFIG_FIELDS` to `("Env",)` left all 45 publication-contract tests green. |
| R4-B1 | high | patch | Parent read the registry read-only. v3.0.20, v3.0.21 and v3.0.22 hold all five two-platform indexes; v3.0.19 pushed none. Every Server child of those three releases carries `PATH=/app`, and v3.0.20 and v3.0.21 also keep the relative `dotnet` entrypoint, so they cannot even start. The do-not-deploy warning names only v3.0.23–v3.0.25. Direct correction in the runbook, the deferred entry and the checkpoint text. |
| R4-B2 | low | reject | The 205baddd comment shows the split was already known for the entrypoint. It still describes the SDK behavior accurately, and the absolute command remains a safe defence. The history note would edit this build's spec. |
| R4-B3 | medium | patch | The v3.0.24 and v3.0.25 releases still carry `hexalith-memories-production.yaml` rendered against the broken Server tags, and v3.0.25 is "Latest". The entry's reopen trigger would close while those tags stay deployable. Direct ledger correction: keep the entry open until the owner marks the affected releases and the Story 30.6 cutover refuses those tags. The owner action is recorded, not taken here. A renderer deny-check would be a new guard. |
| R4-B4 | low | reject | CI `production-deployment-verification` deploys the overlay into kind (`ci.yml:580-605`), so the in-image probes run against the single-RID archive. Parity now binds each child's runtime config to that archive. An arm64-only layer defect stays speculative, and a per-platform in-image probe would add new health surface. |
| R4-B5 | false | reject | carried: R2-B4 rejected layer byte-identity between the Docker and OCI builds. R3-B1's harm was runtime config, which parity now covers. |
| R4-B6 | medium | patch (group H1) | Same hook-bypass gap as R4-V1. |
| R4-B7 | low | reject | carried: R2-B16 rejected recording tool versions. An SDK roll-forward that renamed the hooked target would fail closed at the build-phase parity check, before any registry write. |
| R4-B8 | medium | patch (group H2) | Same single-field coverage gap as R4-V2. |
| R4-B9 | low | reject | A command that exceeds 1800 s receives SIGKILL, so the health tool's cleanup and its temporary key directory never run. On ephemeral runners these resources are discarded with the runner, verification still fails, and the temporary directory is never uploaded. A process-group SIGTERM design is more than a direct correction. |
| R4-B10 | low | patch | The R3-B7 note counts only five copies. Every `run()` call has the 1800 s cap: ten publishes, three preflight phases, five copies and five health runs. Direct text correction; the cancellation/alert sub-claim stays carried from R2-B7. |
| R4-B11 | low | reject | Requesting 90 days under a 30-day repository cap is harmless: GitHub applies the cap and logs it, and the runbook now documents it. Lowering the workflow value and its test pin would pre-empt the owner's choice to raise the repository maximum. The Implementation Notes wording is a spec edit. |
| R4-B12 | low | reject | carried: R3-V5, R3-V6, R3-B3, R3-B4 and R3-B12 (fixes that edit this build's spec). Status syncs at step 5. |
| R4-B13 | low | patch | `CONTRIBUTING.md:506-507` also names `domain-ci.yml` at 81c88b34, while `ci.yml` uses 6e7011fc. The R3-B6 entry cites runbook line 44, but that text is now on line 45. Direct correction to that deferred entry; the stale text itself stays deferred (pre-existing, aa097b52). |
| R4-B14 | low | patch | `release.yml:214` runs the diagnostics archiver without `--evidence-directory`, so the archiver's literal default must stay equal to the contract's. Use `contract.DEFAULT_EVIDENCE_DIRECTORY`. The PowerShell literals are only message text. |
| R4-E1 | low | reject | carried: R2-B5, R2-E9 and R3-E6. |
| R4-E2 | false | reject | The release job checks out submodules at their gitlinks on a fresh runner, and the superproject `sourceSha` pins those gitlinks. A dirty Builds checkout cannot exist before the build (same reasoning as R2-B5). |
| R4-E3 | low | reject | `archive_graph` validates only metadata objects, so a truncated layer would surface at that member's skopeo copy. SDK-produced archives reach that state only through local corruption, and a layer-size guard is more than a direct correction. |
| R4-E4 | low | reject | The SDK multi-RID archive always wraps exactly one image index, as both authorized runs and the parent's real-build check show. The unwrapped fixture shape is a fixture detail that cannot ship. |
| R4-E5 | false | reject | Continuing after one member fails pushes only correct immutable members, and leaves the same partial set that stopping would. Missing-only completion belongs to Story 30.5. |
| R4-E6 | low | reject | carried: R3-E7. |
| R4-E7 | low | reject | Same as R4-B9. |
| R4-E8 | false | reject | The temporary `DOCKER_CONFIG` carries the scoped registry auth that the digest-bound pulls need. GitHub runners use the default context with anonymous Docker Hub pulls, and both authorized runs completed every health pull this way. |
| R4-E9 | low | reject | The current ten package ids sort the same under both orders, and two live runs passed the archiver's exact comparison. A future id change reruns the NuGet fixtures. |
| R4-E10 | low | reject | carried: R2-B12, R2-E8 and R3-E5. |
| R4-E11 | false | reject | carried: R2-E10. |
| R4-E12 | low | reject | The real-Docker case deliberately fails loudly when a pull fails (recorded at the 2026-11-04 rebase). A skip would let the only real-Docker case disappear silently, which the matrix audit counts as missing. |
| R4-E13 | maybe-false | defer | External ingestion commit 3a62bfe1 (`SourceInventoryStore.cs:57-61`). A crash between `AdmitFailedRetryAsync` and the `Unscheduled` catch may leave a `Preparing` entry, and every later re-ingestion of that unit would then return Conflict. Medium if real. Settle with a crash-injection test. |
| R4-E14 | false | reject | carried: R2-B6's cross-origin sub-claim. Failing closed without cancellation credentials is the deliberate, tested hardening. |
| R4-E15 | low | patch | `build_archives` runs `shared_phase("verify")` (local preflight evidence and read-only destination probes) before the parity comparison. The runbook's "Before any preflight write or transport" therefore overstates it; the guarantee is "before the publish preflight and any registry write". Direct wording correction. |
| R4-E16 | false | reject | The claim concerns the new rebind target, which single-RID publishes skip. The absolute `ContainerAppCommand` came from external commit 205baddd. |

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

### Live evidence reconciliation (2026-11-10)

All registry access was read-only. `docker buildx imagetools inspect` used the local Docker credential store; nothing was pushed, tagged, retagged or deleted.

- Packet: `gh release download v3.0.25 -R Hexalith/Hexalith.Memories` fetched `hexalith-memories-release-evidence.tar.gz`. Its SHA-256 `dfa54c81c8da35e002e99527f80a29a1bf8a0b3574876056bb3b81335f1d5c9f` equals the `.sha256.json` receipt (`result: pass`). The archive has no absolute or traversal paths. All 123 `evidence-manifest.json` entries re-hash with matching sizes, and no unlisted file is present.
- Identity: source and workflow `246157a7ff8e2dcd8d356c833a77a02514490d2f`, run 42134622308 attempt 1. `setSha256` `8a10e0216545…` recomputes from the frozen fields with the current `canonical()`. Mappings, platforms, Builds SHA and the packet's `tools/release-containers.json` equal the current contract; that manifest is byte-identical to HEAD. All seven helper hashes are approved `81c88b34` blobs.
- Members: for each of the five images, the remote and `local/` validation records and all five raw objects are byte-identical. The raw index hashes to its index digest and lists exactly linux/amd64 then linux/arm64. Child manifest and config hashes match, and the raw config labels equal the recorded provenance (revision `246157a7`, version 3.0.25). `health.json` binds the validation hash, set, index digest and both child digests, each with `outcome: pass` and `cleanup: pass`. It also records the declared contract and endpoint, Development and `lifecycleWritesEnabled: false`. Every `destination-before-copy.json` is the exact absence receipt.
- Repository validators: run against the extracted packet, `archive-release-evidence.validate_summaries` and `container-publication-contract.validate_authorization` passed. The packet shows ten NuGet packages, five `pushed-and-verified` images with `transported: true`, and a succeeded aggregate.
- Registry (09:08Z): for every `:3.0.25` tag, `--raw` returned bytes identical to the packet `index.raw`. Each of the ten child digests returned its packet manifest bytes, and the parsed configs returned the same labels and architecture. Negative control: the same comparison with version 3.0.24 substituted failed all five index checks. Anonymous `/v2/` and manifest GETs returned `401 Basic realm="zot-distribution"` at 09:14Z.
- Deployment: the `hexalith-memories-production.yaml` release asset and both packet copies hash to `499c0a9b17e3…`. The renderer is unchanged from `246157a7` to HEAD, and a caller render from a fresh `git archive 246157a7` export is byte-identical. A render from this working tree differs (`8aa1e445…`) only in the Redis ConfigMap. Its `deploy/kubernetes/base/config/redis-stack.conf` is materialized LF (`git ls-files --eol`: `w/lf`, `eol=crlf`), while a fresh checkout materializes CRLF. No tracked byte differs.
- Corroboration: the v3.0.24 packet (run 42108808279, source `859680b3`, receipt `55f0f5352bcd38c15c0ed58b0608d3cd7cf3bd2e9b4426080c6a4e532b146449`) passes the same offline, validator and registry checks.
- Exact-source push CI succeeded for 41775358363 (`4dac7dec`), 42040080659 (`859680b3`), 42132182708 (`246157a7`) and 42220908958 (HEAD `4ff3e686`). Each run includes "Run container publish fixtures" and production-deployment-verification.
- Release-job timings (from the GitHub jobs API, job logs and packet member times): the `release` job took 14m35s (v3.0.24) and 16m50s (v3.0.25) of its 120-minute budget. Ten archive builds took about 67 s; five transports plus ten health runs took 8m31s and 9m52s; each linux/arm64 run under QEMU took 70–89 s. Both runs used image `ubuntu-26.04`.
- New finding: both runs logged that the 90-day `release-publication-evidence` retention is capped at 30 days, and artifact 13465662617 expires 2026-12-06. Successful packets are durable release assets, so only failure diagnostics are affected. The runbook's 90-day and credential-absent text is stale. This is recorded as a new deferred entry with owner, consequence and reopen trigger; no setting was changed.
- Deferred work: the release-job budget, multi-RID label and dependency-cohort entries are resolved. The registry immutability-policy and skopeo post-tag-write entries stay open, because these successful runs fire neither trigger.

### Current-tree verification (2026-11-10)

These checks ran on HEAD `4ff3e6866ecb6d1a497e7063e972c24259252c40` with every gitlink at its recorded commit, plus this reconciliation's two documentation edits.

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`: **90/90 passed**, zero skips, 233.019 s, including the real-Docker `--internal` probe case.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v`: **17/17 passed**.
- `python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -v`: **50/50 passed**, 105.661 s.
- `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v`: **70/70 passed**, 27.083 s.
- `ci_alignment` **12/12**, `line_endings` **4/4** and `publish_nuget` **32/32** (127.475 s) passed with the same `-p '*_test.py'` form.
- `npm run verify:semantic-release-config --silent`, `actionlint .github/workflows/release.yml` and `git diff --check` passed.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false --no-restore -m:1 -v:m` reported 0 warnings and 0 errors. `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **138/138 passed** with no errors, skips or not-run cases. A rerun after the final deferred-work.md edit also passed 138/138. `strings -el` found both a control literal and the DomainService.Tests literal added by `246157a7`.
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m` first failed with 9 errors from a stale local restore: MSB4018 (a missing restored `hexalith.eventstore.contracts/3.132.2` file), CS0012/CS0234 (AngleSharp) and CS1061 (MCP). `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=true` changed no tracked file or pin. The identical build command then reported 0 warnings and 0 errors, with 45 outputs including source `Hexalith.EventStore.Contracts`.
- `tools/check-story-file-scope.py --story-key spec-30-4-five-image-publication-contract --changed-files-file <the two changed paths>` passed; a negative control with `tools/check-release-source.py` was refused. `tools/check-story-review-readiness.py --story-key spec-30-4-five-image-publication-contract --derive-cumulative` passed C6 and C4. C1 is skipped by design on the default branch `main`. Its `check_evidence_rows` also reports no C6 failure for either `review` or `done`.

Status stays `in-progress` for the review loop. The recorded hold condition (an authorized credentialed run retaining the complete live packet) is now met, and every Checkpoint Evidence row is complete. Moving to `done` satisfies Story 30.5's activation gate, so that transition belongs to review. No release, registry write, protected-setting change, dependency change or Production lifecycle write took place.

### Round-3 amendment verification (2026-11-10)

These checks ran on HEAD `4ff3e6866ecb6d1a497e7063e972c24259252c40` plus this amendment's ten working-tree paths. The user-level pwsh startup cache `~/.cache/powershell/StartupProfileData-NonInteractive` was corrupt (rewritten 11:05 local, before this run's first pwsh call; a truncated `System.Runtime.Serialization.Formatters, Version=8.1.0.0, Culture=` entry), so every `pwsh` launch exited 134. A run with `XDG_CACHE_HOME` pointing at an empty directory started normally. All pwsh-backed suites below therefore ran with an isolated `XDG_CACHE_HOME`; the user's cache file was left untouched.

- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v`: **97/97 passed**, zero skips, 290.001 s, including the real-Docker `--internal` probe case.
- Mutation check, run sequentially with every file restored byte-identically (`git diff HEAD` matched the pre-mutation snapshot): hard-coding `$phase`, deleting the contract `return` and dropping `-PublicationContract` from `pack-release.ps1` (the three R3-V1 mutations) each fail their new test. So do disabling the rebinding target, skipping the mismatch refusal, ignoring the parity record in `validate_archives`, ignoring parity diagnostics in the archiver, dropping the timeout log, passing a failed health platform, and moving the probe's default receipt. All ten were caught.
- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py'`: **17/17**. `release_preflight`: **50/50** in 116.915 s. Broad `builds_publication_qualification`: **70/70** in 29.814 s. `publish_nuget`: **32/32** in 143.528 s. `ci_alignment` **12/12** and `line_endings` **4/4**.
- `npm run verify:semantic-release-config --silent`, `actionlint .github/workflows/release.yml`, `git diff --check` and Python AST parsing of every changed script passed.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false --no-restore -m:1 -v:m` reported 0 warnings and 0 errors. `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **138/138**.
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m`: 0 warnings and 0 errors, 45 outputs. `ProductionDeploymentArtifactsTests`, which reads `Directory.Build.targets` at run time: **14/14**. A package-reference `Server.Tests` build (`-p:UseHexalithProjectReferences=false`) fails with CS0234 (`Hexalith.Commons`) inside `references/Hexalith.EventStore` source. The same error occurs with HEAD's `Directory.Build.targets` restored, so it predates this change; the file was then restored byte-identically.
- Real multi-RID archive: `dotnet publish src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` with the contract's release properties, version `3.0.0-parity.1`, the approved `81c88b34` provenance target and `-p:ContainerArchiveOutputPath` under the session scratchpad (archive only, nothing pushed). It produced a two-RID OCI archive (index `sha256:fd5db692e034`) and the single-RID Docker deployment archive. Both children, linux/amd64 `sha256:3df5a03b93ac` and linux/arm64 `sha256:617ee0df5a71`, carry `PATH=/app:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin`, equal to the Docker archive. The approved validator's `archive_graph` passed, and `runtime_config_parity` passed with one runtime-config SHA-256 (`ab778282347d`) for the archive and both children. The same release-shaped builds of MCP, lifecycle, clock and DomainService also passed the approved validator and parity, so the check does not refuse a correct release on SDK 10.0.403.
- Negative control (read-only `docker buildx imagetools inspect`): the published Server children of 3.0.23, 3.0.24 and 3.0.25 all report `PATH=/app` on linux/amd64 and linux/arm64. Nothing was pushed, tagged, retagged or deleted.
- `tools/check-story-file-scope.py --story-key spec-30-4-five-image-publication-contract --changed-files-file <the ten changed paths>` passed; adding `tools/check-release-source.py` was refused (exit 1). `check_evidence_rows` reports no C6 failure for `in-progress`, `review` or `done` with the three blocked rows. Removing one row's `reopen trigger:` label produces a C6 failure.

Server health, Archive compatibility and Runtime config parity stay blocked until the next authorized release retains a packet whose children pass runtime-config parity. Status stays `in-progress` for review. No release, registry write, protected-setting change, dependency change or Production lifecycle write took place.

### Present (2026-11-10)

- Status decision: the build workflow's present step would mark this spec `done` and the sprint row `review`. Both stay `in-progress`, for two reasons:
  - Server health, Archive compatibility and Runtime config parity close only when the next authorized release retains a packet whose Server children pass runtime-config parity.
  - `done` would satisfy Story 30.5's activation gate while every published v3.0.20–v3.0.25 Server image is undeployable.
- Review rounds 3 and 4 are complete. Round 3 looped back once (bad_spec G-A). Round 4 patched nine findings in place, deferred one (R4-E13) and needed no loopback.
- Owner actions are recorded in deferred-work.md:
  - dispatch the next authorized release;
  - mark the v3.0.23–v3.0.25 GitHub releases do-not-deploy;
  - make the Story 30.6 cutover refuse the v3.0.20–v3.0.25 tags.
- Parent final verification on the patched tree:
  - `publish_containers` **98/98** (zero skips), `release_dispatch` **17/17**, `release_preflight` **50/50**, broad `builds_publication_qualification` **70/70**, `publish_nuget` **32/32**, `ci_alignment` **12/12** and `line_endings` **4/4**;
  - `npm run verify:semantic-release-config --silent` and `actionlint` on `release.yml` and `ci.yml`;
  - the Debug CLI test build (0 warnings, 0 errors) and `CiTestInventoryTests` **138/138**;
  - `git diff --check`.

### Local resume verification (2026-11-11)

Ran sequentially on HEAD `db769a95e2966dd42f5523ce091e7f8f317f22a4`, after reading the full spec and `epic-30-context.md`. Every Execution task was already implemented; no code, test, workflow, setting or dependency changed. The frozen block and `baseline_commit` remain unchanged. PowerShell-backed checks used isolated `XDG_CACHE_HOME=/tmp/memories-30-4-resume-20261111/pwsh-cache`.

- Container selection: `unittest.defaultTestLoader.discover('tests/tooling/publish_containers', pattern='*_test.py')`, flattened and excluding exactly `publication_contract_test.DockerWorkloadHealthTests.test_real_docker_internal_network_probe_reaches_liveness_and_cleans_up`, passed **97/97**, zero skips, 235.015 s. Discovery totals 98. The excluded test unconditionally pulls Docker images and was **not run** under this step's no-remote-operations rule; this is not a full-suite pass. Approved Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa` was already available locally, so no helper fetch ran.
- The following lanes used `python3 -m unittest discover -s tests/tooling/<lane> -p '*_test.py' -v`:

| Lane | Current result |
| --- | --- |
| `release_dispatch` | **16/17 passed**, one failure, 0.323 s: `test_release_pin_is_approved_and_profile_tracks_the_gitlink` finds Builds gitlink/checkout `891886ddc60a9924d053bb74bf6bcc73e101b2ce`, while `tools/builds-publication-qualification.json` binds `5fa2b97c8b22047c2c33e2c9f7017fd5d604c9ef`. |
| `release_preflight` | **50/50 passed**, 94.682 s. |
| `builds_publication_qualification` | **Failed: 60 tests attempted, 3 failures and 9 errors**, 1.742 s. The harness refuses `builds-gitlink-mismatch`; missing observation fields then cause cascading errors. Within this same run, all 28 focused `ProfileTests` (2), `ReleaseSetTests` (6), `CommittedEvidenceTests` (12) and `DocumentTests` (8) passed; this grants no broad-gate pass. |
| `publish_nuget` | **32/32 passed**, 112.141 s. |
| `ci_alignment` | **12/12 passed**. |
| `line_endings` | **4/4 passed**. |

- `npm run verify:semantic-release-config --silent` and `actionlint .github/workflows/release.yml .github/workflows/ci.yml` passed.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false --no-restore -m:1 -v:m` passed with 0 warnings and 0 errors. The freshly built assembly passed **138/138** with `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`, with no errors, skips or not-run cases.
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m` initially failed with MSB3243 and CS1704: source `Hexalith.EventStore.Contracts` 4.2.0 conflicted with the restored 4.1.0 package (1 warning, 1 error, 13.72 s). A bounded offline restore, `dotnet restore Hexalith.Memories.slnx -p:UseHexalithProjectReferences=true -p:NuGetAudit=false --configfile /tmp/memories-30-4-resume-20261111/NuGet.offline.config --ignore-failed-sources -m:1`, succeeded using only the existing global package cache; the temporary config contains `<packageSources><clear /></packageSources>` and no feed. The identical source build then **passed with 0 warnings and 0 errors**, 18.01 s. No tracked configuration, pin or reference changed. Only the current Builds profile blocker remains in deferred-work.md; the historical 2026-11-10 resolution remains intact.
- `python3 tools/check-story-file-scope.py --story-key spec-30-4-five-image-publication-contract --changed-files-file /tmp/memories-30-4-resume-20261111/changed-files.txt` passed for the two documentation paths. `python3 tools/check-story-review-readiness.py --story-key spec-30-4-five-image-publication-contract --derive-cumulative` passed C6/C4; C1 is skipped on `main` by design. `git diff --check`, CRLF preservation and byte-identical frozen-block checks passed.
- Logs and exact lane commands are retained under `/tmp/memories-30-4-resume-20261111/`, including `verification-results.json`. No retained `hexalith-memories-release-evidence.tar.gz` was found under `/tmp` or repository `artifacts/`; no packet was downloaded and no registry or new exact-source CI evidence was collected.

Server health, Archive compatibility and Runtime config parity remain blocked until an authorized release retains the required corrected packet. Spec and sprint status stay `in-progress`; Story 30.5 receives no activation credit. No release, remote operation, registry write, dependency/setting change or Production lifecycle write took place.

### Builds profile follow-up (2026-11-11)

Separately authorized configuration maintenance aligns `tools/builds-publication-qualification.json` and the qualification suite's `PINNED_SHA` with the existing Builds gitlink/checkout `891886ddc60a9924d053bb74bf6bcc73e101b2ce`. No submodule revision was selected or changed; the release-publication approval remains `81c88b34d09d34260b92d7f0928dbbc1c7446baa`. Story 30.4's frozen scope and three blocked live checkpoints are unchanged.

- `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v`: **17/17 passed**, 0.295 s.
- `XDG_CACHE_HOME=/tmp/memories-30-4-resume-20261111/pwsh-cache python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v`: **70/70 passed**, 25.119 s; log `/tmp/memories-builds-fix-qualification-20261111.log`.

These results supersede the local profile-mismatch failures above. Exact-source push CI remains pending; no commit, push or release was performed.

### Current-HEAD local verification (2026-11-11)

Ran sequentially on HEAD `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb`, after reading the full spec and its sole frontmatter context, `epic-30-context.md`. The working tree started clean. All Execution tasks were already implemented, so no code, test, workflow, setting, dependency or deferred-work entry changed. This section and the Change Log row are the only edits. The frozen block, `baseline_commit`, three blocked Checkpoint Evidence rows and spec/sprint `in-progress` status are unchanged. PowerShell-backed checks used isolated `XDG_CACHE_HOME=/tmp/memories-30-4-current-20261111-h4z9pkc3/pwsh-cache`.

- Container selection used `unittest.defaultTestLoader.discover('tests/tooling/publish_containers', pattern='*_test.py')`, recursively flattened, excluding exactly `publication_contract_test.DockerWorkloadHealthTests.test_real_docker_internal_network_probe_reaches_liveness_and_cleans_up`. Discovery totals 98; the selection passed **97/97**, zero skips, 242.934 s. The excluded case unconditionally pulls Docker images and was not run under this step's no-remote-operations rule; this is not a full-suite pass. Approved Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa` was verified present locally before execution, so no helper fetch was needed.
- All **19/19** unique covering tests named in the existing Frozen matrix audit appear as passing in this run's container/dispatch logs. The per-case check is retained in `frozen-matrix-results.json`; it grants no credit to the excluded Docker case.
- Each lane below used `python3 -m unittest discover -s tests/tooling/<lane> -p '*_test.py' -v`:

| Lane | Current result |
| --- | --- |
| `release_dispatch` | **17/17 passed**, 0.322 s, including current Builds gitlink/profile parity. |
| `release_preflight` | **50/50 passed**, 103.349 s. |
| `builds_publication_qualification` | **70/70 passed**, 30.030 s. |
| `ci_alignment` | **12/12 passed**, 3.796 s. |
| `line_endings` | **4/4 passed**, 0.461 s. |

- The unaffected `publish_nuget` rerun had already started when the resumed task narrowed its verification scope. Only this verification harness and its active NuGet fixture process tree were stopped; the interrupted lane grants no current result. The preceding resume's **32/32** remains historical evidence, and `git diff --quiet db769a95e2966dd42f5523ce091e7f8f317f22a4 HEAD -- tools/publish-nuget.ps1 tests/tooling/publish_nuget` confirmed those implementation/test bytes are unchanged.
- `npm run verify:semantic-release-config --silent` passed the offline lifecycle and fail-closed negative cases. `actionlint .github/workflows/release.yml .github/workflows/ci.yml` passed.
- `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m` passed with **0 warnings and 0 errors**, 45 outputs, 28.74 s. No restore or dependency/reference change was needed. The resulting Debug CLI assembly passed **138/138** with `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`, with no errors, failures, skips or not-run cases.
- No real multi-RID archive was rebuilt, packet downloaded, registry queried or exact-source CI read performed. Earlier real-archive/live-packet results remain dated historical evidence, and exact-source push CI for the profile reconciliation remains unverified here.
- `python3 tools/check-story-file-scope.py --story-key spec-30-4-five-image-publication-contract --changed-files-file /tmp/memories-30-4-current-20261111-h4z9pkc3/changed-files.txt` passed for the single spec path. `python3 tools/check-story-review-readiness.py --story-key spec-30-4-five-image-publication-contract --derive-cumulative` passed C6/C4; C1 is skipped on `main` by design and receives no pass credit. `git diff --check`, CRLF preservation and byte-identical frozen-block checks passed.
- Logs, exact commands and exit codes are retained under `/tmp/memories-30-4-current-20261111-h4z9pkc3/`, including `verification-results.json` and `frozen-matrix-results.json`.

Server health, Archive compatibility and Runtime config parity remain blocked until Jérôme Piquot's next authorized release retains the required corrected packet. No v3.0.20–v3.0.25 Server image may be deployed. Spec and sprint status stay `in-progress`; Story 30.5 receives no activation credit. No release, remote operation, registry write, dependency/setting change or Production lifecycle write took place.

### Authorized read-only evidence follow-up (2026-11-11)

The user explicitly authorized read-only GitHub and registry checks after the local verification above. Local and remote `main` both identify `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb`. This evidence supersedes the preceding current-source CI hold, without changing the frozen intent or release-publication approval.

- GitHub REST `GET /repos/Hexalith/Hexalith.Memories/releases?per_page=5` still lists v3.0.25 (published 2026-11-06T00:26:35Z) as the newest release. Its evidence asset 715337007 still advertises SHA-256 `dfa54c81c8da35e002e99527f80a29a1bf8a0b3574876056bb3b81335f1d5c9f`, the previously reconciled packet. No packet was downloaded or re-hashed in this follow-up. `GET /repos/Hexalith/Hexalith.Memories/actions/runs?per_page=30` includes the last Release run 42134622308 and every newer observed run; no subsequent Release run or corrected packet is present.
- Exact-source push CI run 43007809636 succeeded on the current full SHA above. Its jobs `source-debug` (130479570417), `test-unit-contract` (130479570560), `production-deployment-verification` (130479570124) and final `build` (130481692079) all succeeded. The container-fixture step's job log records `Ran 98 tests in 99.415s` followed by `OK`, with no skips. Thus the pull-dependent real Docker case excluded from the local selection has current-source CI evidence; this does not change the local selection into a full-suite run. Separate Builds qualification run 43007808801 also succeeded on the same SHA. No run was triggered, retried, approved or cancelled.
- `skopeo list-tags` returned authentication-required errors both with its default credentials and with the existing Docker auth file; these errors grant no registry-success credit. Read-only HTTP GETs using preemptive Basic credentials from the existing Docker credential store succeeded for `/v2/<repository>/tags/list?n=1000` in all five repositories, with no continuation link. Tag counts were 129 (`memories`), 124 (`memories-mcp`), 91 (`memories-access-telemetry`), 91 (`memories-access-telemetry-clock`) and 13 (`memories-domain-service`). All five highest stable versions are 3.0.25. Remaining non-stable tags are historical previews or retained 2.15.2 qualification tags, with no newer release candidate. No credentials were printed or retained, and no upload session or push probe was issued.
- `docker buildx imagetools inspect --raw registry.hexalith.com/memories:3.0.25` still returns index `sha256:a857a0865ff3781027cc392a7d36431fd13db1e7a3522690a8ef05df7036af3a`. Digest-bound `imagetools inspect --format '{{json .Image}}'` reads show `PATH=/app` on both linux/amd64 child `sha256:41a099ac9a60d97e6976b6e3620b8c5ec18a19b0c4f827e6afa997869d4cb06a` and linux/arm64 child `sha256:a64a9f9db67154787262345214d68c0bd6bb15c27e09f2aeaf415c633e48be27`. Both configs retain revision `246157a7ff8e2dcd8d356c833a77a02514490d2f` and the absolute dotnet entrypoint. These are the previously rejected children, not corrected images.
- Readback metadata, all five tag lists, the Server index/config reads and the CI container-step excerpt are retained under `/tmp/memories-30-4-remote-20261111-_gviollu/`, including `github-readback.json`, `registry-tag-read-results.json` and `ci-container-fixtures.log`. The current Builds-profile deferred entry is resolved because its local reconciliation, both exact local verification commands and exact-source push CI requirements are now met. The affected-release do-not-deploy entry remains open.

- Documentation-only follow-up checks passed: file scope for the two changed paths, review readiness C6/C4 (C1 skipped on `main`), `git diff --check`, preserved baseline and spec/sprint state, CRLF line endings, and a frozen block byte-identical to the pre-run working copy.

Server health, Archive compatibility and Runtime config parity remain blocked: no corrected release has retained the required packet. Owner: Jérôme Piquot. Consequence: Story 30.4 remains `in-progress`, Story 30.5 receives no activation credit, and no v3.0.20–v3.0.25 Server image may be deployed. Reopen trigger: an independently authorized release retains a corrected packet whose children pass runtime-config parity. This authorization covered reads only; no release, remote mutation, registry write, protected-setting change, dependency change or Production lifecycle write took place.

### Authorized release attempt and MCP preparation blocker (2026-11-11)

After the read-only follow-up, the user explicitly authorized dispatching the guarded Release workflow for the verified current source. This is a one-run human authorization exception to the implementation-phase publication prohibition; the frozen block remains unchanged. `gh workflow run release.yml --repo Hexalith/Hexalith.Memories --ref main` dispatched run 43012088139 at 2026-11-11T11:27:22Z, with exact source `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb`. Existing settings already had publication flag `true`, required owner reviewer `jpiquot`, `prevent_self_review: false`, disabled admin bypass, and a sole `main` deployment policy. The accepted sole-owner self-review exception in the runbook applied; the normal production approval was submitted through the configured owner account. No setting or protection was modified.

Source proof, preparation, all preflight checks, the second source/protection proof and OIDC login succeeded. Semantic-release computed 3.0.26 and packed ten packages locally on the runner, but its `prepare` hook failed at the MCP multi-RID container build. Only MCP failed; Server, lifecycle, clock and DomainService archive/config-parity checks passed. The Server's archive and both OCI children share runtime-config SHA-256 `321bd4b35ffab1438af9d485ecc1c15ab7d37dcc4a92203618bf3e89b23b3b71`, which proves build-phase parity but grants no publication-health credit.

The retained diagnostics are artifact 13850599888 (ZIP SHA-256 `cb9e403a3d7c77a88e45c6433a94db9eff00d05424a96f37b9dcec75fefa1ee9`, verified after download). `containers/release/build-summary.json` records MCP's exact SDK 10.0.404 MSB4184 failure; its `push: false` and every member's `transported: false` show the legacy `partial-publish`/`pushed` vocabulary denotes build outcomes here. `release/container-evidence/failure-build.json` is an accountable build refusal; the archived packet is diagnostic-only. The failure is reproducible from the MCP description semicolon added by external commit `1143ba17a2336f0d718fcff71e049c06350a45a2`, as detailed in the Code Map.

Post-failure authenticated registry reads found no 3.0.26 tag in any of the five repositories. Public NuGet version lists found no 3.0.26 in any of the ten package IDs, and the GitHub Git-ref read returned no `v3.0.26` tag. No publish hook ran. The evidence and side-effect readback are retained under `/tmp/memories-30-4-release-20261111-u6vf60k8/`, including `failed-release-evidence.zip`, `failed-evidence/` and `failed-release-side-effects.json`. No success packet or GitHub release was produced, and no retry was dispatched.

Owner: Jérôme Piquot. Consequence: 30.4 remains `in-progress`; the three live checkpoints still lack corrected-release evidence and 30.5 receives no activation credit. Reopen trigger: repair the MCP metadata, pass the real SDK label-parser regression and exact-source CI, then authorize publication of the corrected source. This local amendment prepares a reversible punctuation fix and regression within the same five-image goal; it changes no dependency, SDK pin, public surface or protected setting.

### Local MCP label-parser repair (2026-11-11)

Implemented the three-task MCP amendment sequentially on the existing `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb` working tree, preserving the prior local/read-only/authorized-release evidence. The only project change replaces `not retired; see` with `not retired. See` in `Description`; the compatibility warning, README removal-gate pointer, package identity, references and all other project bytes are unchanged. No SDK, shared Builds helper, dependency or setting changed.

- Added `PublicationContractTests.test_generated_mcp_description_labels_survive_the_sdk_multi_rid_parser`. It obtains the actual MCP project's generated `ContainerLabel` items through `ComputeContainerConfig`, checks both OCI description labels against the actual `Description`, carries the SDK's `Identity:Value` list into `_ParseItemsForPublishingSingleContainer`, and requires both descriptions to survive unchanged for `linux-musl-x64` and `linux-musl-arm64`. A temporary property file preserves the transported punctuation without shell/command-line quoting. Both test-only old-semicolon variants must reproduce MSB4184 in `Microsoft.NET.Build.Containers.targets`; no alternative parser or image transport is used.
- Red before the punctuation change: `XDG_CACHE_HOME=/tmp/memories-30-4-mcp-20261111-ylbho0j4/pwsh-cache python3 -m unittest discover -s tests/tooling/publish_containers -p publication_contract_test.py -k generated_mcp_description -v` exited 1 with exactly two failing current-description subtests (both RIDs), each showing MSB4184 at SDK 10.0.403 target line 384. Both old-semicolon negative controls passed their expected-failure assertions. The identical command after the change passed **1/1**, 2.545 s, with all four subtests satisfied.
- Focused selection: recursively flattened `unittest.defaultTestLoader.discover('tests/tooling/publish_containers', pattern='publication_contract_test.py')`, excluding exactly `publication_contract_test.DockerWorkloadHealthTests.test_real_docker_internal_network_probe_reaches_liveness_and_cleans_up`, passed **46/46**, zero skips, 41.476 s. Discovery totals 47 for this file (99 across the container lane). The excluded case unconditionally pulls Docker images and receives no local credit under the no-remote rule. This is not a full container-suite pass; the preceding current-source CI 98/98 predates this new test and punctuation change.
- `dotnet build src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m` passed with **0 warnings and 0 errors**, 0.95 s. No restore, real archive build or remote operation was needed for the actual SDK target-boundary proof.
- The parent's retained `failed-diagnostic-verification.json` additionally records that the failed run's diagnostic tar receipt matches SHA-256 `a723baeecc4e9150598afc459af955e1e5e30ed91182b936ec48ce71dbdd0729` and all **48/48** manifest entries re-hash. It binds source `f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb` and set `baeee36366281ed440bcbcb6920a887b6ae4fae79cb07d06ee5d282cc2c069cb`. Both unpublished Server children have the full declared `PATH` and six-field runtime-config SHA-256 `321bd4b35ffab1438af9d485ecc1c15ab7d37dcc4a92203618bf3e89b23b3b71`, equal to their deployment archive. These are diagnostic/build facts only. The failed release's 15 destination version reads and Git tag read found no 3.0.26 publication; the earlier ZIP hash and run identity remain recorded above.
- Final local checks passed: `python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -v` **4/4**; `python3 tools/check-story-file-scope.py --story-key spec-30-4-five-image-publication-contract --changed-files-file /tmp/memories-30-4-mcp-20261111-ylbho0j4/changed-files.txt` for all four working-tree paths; `python3 tools/check-story-review-readiness.py --story-key spec-30-4-five-image-publication-contract --derive-cumulative` C6/C4 (C1 skipped on `main`, without pass credit); Python AST parsing; `git diff --check`; byte-identical frozen intent/baseline and the exact punctuation-only project diff.
- Logs and exact command/exit records are retained under `/tmp/memories-30-4-mcp-20261111-ylbho0j4/`, including `label-parser-red.log`, `label-parser-green.log`, `publication_contract_offline.log`, `mcp_source_debug.log` and `verification-results.json`.

The local preparation blocker is repaired for review. Exact-source CI for these changed bytes and a separately authorized corrected release still remain required. Server health, Archive compatibility and Runtime config parity remain blocked, spec/sprint stay `in-progress`, and Story 30.5 receives no activation credit. No commit, push, changed-source release, registry write or Production lifecycle write was performed by this amendment.

### Current retention-policy reconciliation (2026-11-11)

Read-only `gh api --method GET repos/Hexalith/Hexalith.Memories/actions/permissions/artifact-and-log-retention` returned `days: 90` and `maximum_allowed_days: 90`. Artifact 13850599888 from failed authorized Release run 43012088139 expires 2027-02-09T11:27:25Z, confirming the 90-day policy in practice. The repository's maximum now accommodates the unchanged workflow request, so the retention mismatch entry's reopen trigger is fully met and the entry is resolved. The runbook now separates the current policy from the dated v3.0.24/v3.0.25 30-day cap. No retention setting or workflow was changed. The readback is retained under `/tmp/memories-30-4-release-20261111-u6vf60k8/artifact-retention-readback.json`. This is retention evidence only; the three publication checkpoints remain held pending changed-source CI and a corrected release.
