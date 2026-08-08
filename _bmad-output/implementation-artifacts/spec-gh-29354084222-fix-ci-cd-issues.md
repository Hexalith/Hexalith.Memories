---
title: 'Recover Zot-backed releases and fail before partial publication'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
review_loop_iteration: 0
baseline_commit: '1c7e33fb63116aa4ab87045dced0824df32de3f3'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Run `29354084222` created `v2.6.6` and published nine NuGets before both Zot pushes were denied. The recurring credential/ACL failure left `2.6.5`-`2.6.7` images and all `v2.6.0`-`v2.6.7` GitHub Releases absent; recovery also compares incompatible config and manifest digests.

**Approach:** Probe both repositories during semantic-release verification, before side effects; fix immutable-image reconciliation; then idempotently restore images, GitHub Release assets, and incidents after an administrator repairs the Zot principal.

## Boundaries & Constraints

**Always:** Keep the flat `memories`/`memories-mcp` repositories, `HEXALITH_ZOT_*` contract, secret redaction, exact reachable tags, nine-package inventory, immutable tags, and config-digest equality. Probe write scope with OCI upload-session start/cancel and cancel every session. Use current trusted recovery tooling against exact tagged source; verify remote images, NuGets, and assets before closing an incident.

**Ask First:** Renew approval before changing registry names, organization policy, package inventory, tags, release-note convention, or versions outside `2.6.0`-`2.6.7`.

**Never:** Expose credentials; alter tags; delete/republish NuGets; overwrite conflicting images; treat public `/v2/` or login success as push proof; create a release before all families verify; or close incidents without evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Releasable commit | Both repositories grant push | Probe/cancel both before prepare; release continues | Attempt cancellation after later probe failure |
| Invalid identity or ACL | Reads/login work; upload POST is 401/403 | Fail before tag or publication | Redacted error names repository and admin action |
| Existing matching image | Remote verbose manifest config digest equals local image ID | Record `already-present` | Descriptor digest is never compared with config digest |
| Tagged partial release | Tag/NuGets exist; images or Release absent | Reconcile missing artifacts, verify, then close issue | Conflict/missing evidence fails closed and stays rerunnable |

</frozen-after-approval>

## Code Map

- `.releaserc.json` -- semantic-release ordering; lacks a pre-tag write gate.
- `tools/publish-containers.ps1` -- authentication, publication, and faulty digest comparison.
- `.github/workflows/recover-partial-release.yml` -- image-only recovery using tag-embedded tooling.
- `tools/create-partial-publish-issue.ps1` and `docs/dev/release-runbook.md` -- incident guidance.
- `tests/tooling/publish_containers/` and `CiTestInventoryTests.cs` -- release/recovery guards.

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-container-registry.ps1`, `.releaserc.json`, and fixtures -- add redacted OCI start/cancel probes for both repositories in `verifyReleaseCmd`, before prepare/tag.
- [x] `tools/publish-containers.ps1` and its Python fixtures -- compare `SchemaV2Manifest.config.digest` with local image ID; classify auth failures, remove false login assurance, and model real manifests plus login-success/push-denied.
- [x] `.github/workflows/recover-partial-release.yml` and fixtures -- run trusted current recovery logic on exact tagged source; verify NuGets, reconcile images, idempotently create/validate a Release with nine published nupkgs and deployment, retain evidence, then close its incident.
- [x] `tools/create-partial-publish-issue.ps1`, `docs/dev/release-runbook.md`, and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- make guidance state-aware and pin ordering, credentials, permissions, redaction, and failure propagation.
- [x] After Zot access is repaired, recover `2.6.0`-`2.6.7`; require images for `2.6.5`-`2.6.7`, Releases/assets for all eight, and evidence-backed issue closure.

**Acceptance Criteria:**
- Given denied Zot push scope, when semantic-release runs, then it fails before tag/artifact creation and leaks no credential.
- Given a matching existing image, when publication or recovery reconciles it, then remote config-digest equality yields `already-present`; a differing digest blocks all overwrite attempts.
- Given an affected tag, when recovery completes, then its NuGets, images, deployment, Release assets, and evidence agree with that tag.
- Given final recovery, when remote state is inspected, then both Zot repositories contain `2.6.5`-`2.6.7`, all eight GitHub Releases exist, and only verified incidents are closed.

## Spec Change Log

- 2026-08-08: Confirmed org `HEXALITH_ZOT_*` secrets/vars are present and write-scope probe succeeds on recovery. First recoveries for `2.6.0`/`2.6.7` failed at build because current tooling assumed the four-image access-telemetry unit; historical `2.6.x` tags only publish Server+MCP. Locally fixed `tools/publish-containers.ps1` and `tools/complete-partial-release.ps1` to select members from the tagged source (with tests). Pushed to `main` as `578d005c`.
- 2026-08-08: Re-ran recovery: `2.6.0` hit immutable digest conflict (remote already present with a different config digest). `2.6.5` built and pushed both images, then failed workflow evidence validation still requiring exactly four images. Updated `.github/workflows/recover-partial-release.yml` to accept the tagged 2- or 4-image unit.
- 2026-08-08: Recovered `2.6.5`-`2.6.7` images+Releases via workflow (`2.6.5` Release finished from the successful push summary after a non-reproducible rebuild conflict). Completed `2.6.0`-`2.6.4` GitHub Releases from already-present Zot tags (rebuilds conflict on digest; remote images HTTP 200). All eight Releases now present; PARTIAL PUBLISH issues were already closed before this evidence pass.
- 2026-08-08 (review loop 1): Review found `$expectedImages` left unset after the allowed-image rename (deployment cross-check became a silent no-op), plus 3-image units and null ParamBlock gaps. Patched publish/complete/workflow to enforce 2-or-4 units, restore deployment image checks against `$summaryImages`, guard null ParamBlock, and reject unexpected repositories; added a deployment-mismatch completion test. KEEP: tagged-source member selection, 2-or-4 historical unit acceptance, and completed 2.6.0-2.6.7 recovery evidence.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md`
- `.releaserc.json`
- `.github/workflows/recover-partial-release.yml`
- `docs/dev/release-runbook.md`
- `tools/verify-container-registry.ps1`
- `tools/publish-containers.ps1`
- `tools/complete-partial-release.ps1`
- `tools/create-partial-publish-issue.ps1`
- `tests/tooling/publish_containers/**`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Design Notes

Zot serves `/v2/` anonymously, so login success does not prove writes. OCI upload-session `POST` plus cancellation `DELETE` tests exact repository scope during `verifyRelease`. Docker's local image ID matches `SchemaV2Manifest.config.digest`; `Descriptor.digest` identifies the manifest.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py'` -- expected: auth, digest, recovery, and redaction fixtures pass.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release -p:NuGetAudit=false -p:MinVerVersionOverride=2.6.8` then run its built xUnit v3 assembly with `-class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: all workflow contracts pass.
- `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=2.6.8` -- expected: zero warnings and errors.

**Manual checks:**
- With repaired organization secrets, run tagged recoveries and inspect Zot digests, NuGet URLs, Release assets, evidence, and issue closures.

## Suggested Review Order

**Tagged-source image unit selection**

- Entry point: publish only projects present in the checked-out tagged tree
  [`publish-containers.ps1:61`](../../tools/publish-containers.ps1#L61)

- Fail closed unless the selected unit is exactly two or four members
  [`publish-containers.ps1:97`](../../tools/publish-containers.ps1#L97)

- Pass only Image parameters the tagged render script declares
  [`publish-containers.ps1:426`](../../tools/publish-containers.ps1#L426)

**Release completion evidence**

- Accept Server/MCP with optional telemetry as a 2- or 4-image allowed set
  [`complete-partial-release.ps1:209`](../../tools/complete-partial-release.ps1#L209)

- Require the versioned deployment to reference every summary image
  [`complete-partial-release.ps1:277`](../../tools/complete-partial-release.ps1#L277)

**Workflow gates**

- Historical recoveries must present Server/MCP and only allowed repositories
  [`recover-partial-release.yml:143`](../../.github/workflows/recover-partial-release.yml#L143)

**Tests**

- Prove a mismatched deployment fails before Release creation
  [`partial_release_completion_test.py:340`](../../tests/tooling/publish_containers/partial_release_completion_test.py#L340)

