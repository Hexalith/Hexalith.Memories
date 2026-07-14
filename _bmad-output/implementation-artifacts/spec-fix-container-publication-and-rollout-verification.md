---
title: 'Fix container publication and rollout verification'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'f68cb82b8a33aaa0c92d73d2c249a677d94dca80'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run 29317961001 authenticates to Zot and builds both OCI archives but never attempts a push because .NET SDK archive mode loads registry-less tags while the publisher inspects registry-qualified tags. CI run 29317960987 also has a timing-sensitive Dapr outage check whose newest-pod selection can wait on a non-running replacement and report an empty diagnostic.

**Approach:** Canonicalize each loaded archive to its intended Zot reference before digest reconciliation and push. Make the disposable rollout inject a stable Dapr dependency failure while the host remains running, select runnable pods, label every fault stage, and persist failure evidence.

## Boundaries & Constraints

**Always:** Keep `registry.hexalith.com/memories:<version>` and `registry.hexalith.com/memories-mcp:<version>` as the canonical two-image release unit; authenticate only for `-Push` through `HEXALITH_ZOT_USERNAME` and `HEXALITH_ZOT_API_KEY` using password-stdin; redact secrets; retain immutable-tag digest reconciliation, non-root/port/image inspection, aggregate-health assertions, and zero-skip kind verification; preserve unrelated worktree changes.

**Ask First:** Changing registry/repository names, credential contracts, immutable-tag policy, published NuGet artifacts, or the required Healthy/Degraded/Unhealthy semantics; pushing a tag other than the intended missing release images; proceeding with live key verification if the matching Zot username is unavailable.

**Never:** Add Dockerfiles; weaken or skip the rollout gate; treat a non-running host as HTTP 503 evidence; print credentials; republish NuGet packages merely to retry containers; accept an unparseable loaded-image reference or a conflicting remote digest.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Archive load | SDK archive contains `memories:<version>` | Retag canonical Zot reference, inspect, reconcile, then push | Fail member with redacted load/tag evidence |
| Canonical archive | Loaded reference already equals target | Skip retag and continue | Preserve existing digest checks |
| Existing immutable tag | Remote config digest matches or conflicts | Mark matching tag present; reject conflict | No overwrite or blind retry |
| Dapr fault | Server is running with invalid app-to-sidecar authentication | `/ready` returns 503 aggregate `Unhealthy` with Dapr evidence, then returns `Healthy` after restoration | Capture labeled pod, event, describe, and log evidence on failure |

</frozen-after-approval>

## Code Map

- `tools/publish-containers.ps1` -- builds archives, authenticates, loads, reconciles, and pushes both images.
- `tests/tooling/publish_containers/publish_containers_test.py` -- fake Docker publisher contract; currently masks the registry-less loaded tag.
- `tools/verify-production-deployment.ps1` -- kind rollout and fault-injection verifier.
- `.github/workflows/ci.yml` -- runs the verifier and uploads its evidence.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- pins workflow and release/verifier safeguards.

## Tasks & Acceptance

**Execution:**
- [x] `tools/publish-containers.ps1` -- parse Docker load output, retag registry-less images to the canonical reference, and fail safely on parse/tag errors before existing inspect/reconcile/push logic.
- [x] `tests/tooling/publish_containers/publish_containers_test.py` -- model real loaded-image state and cover retag, canonical-load, parse failure, tag failure, reconciliation, and redaction paths.
- [x] `tools/verify-production-deployment.ps1` -- replace the sidecar-removal rollout race with a reversible Dapr token fault, select a running target container, label waits, restore deterministically, and write terminal cluster diagnostics.
- [x] `.github/workflows/ci.yml` and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- upload/pin the new evidence and stable verifier contract.

**Acceptance Criteria:**
- Given both SDK archives and valid Zot credentials, when publication runs, then both canonical images are pushed or reconciled as identical without `No such image` failures.
- Given a registry-less or canonical loaded tag, when publication normalizes it, then the canonical reference is inspected before any remote operation.
- Given the Dapr fault stage, when the dependency becomes unusable, then a running Server exposes aggregate `Unhealthy` over HTTP 503 and returns to `Healthy` after restoration.
- Given any rollout failure, when CI uploads evidence, then the failed scenario, pod/container state, events, descriptions, and current/previous logs are available without secrets.

## Spec Change Log

## Design Notes

SDK 10.0.301 writes `RepoTags:["memories:<version>"]` in archive mode even when `ContainerRegistry` is supplied. The verifier already demonstrates the robust pattern: parse `Loaded image:` or `Loaded image ID:`, then tag only when the loaded reference differs from the canonical target.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/publish_containers -p '*_test.py'` -- expected: real-load/tag, partial-publish, retry, and redaction fixtures pass.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release` followed by the focused `CiTestInventoryTests` executable lane -- expected: workflow contracts pass.
- Build both archives with `tools/publish-containers.ps1`, inspect `manifest.json`, and run the complete kind verifier -- expected: zero skips and all recovery stages pass.
- Run `tools/publish-containers.ps1 -Push -Version 2.6.0` with the supplied API key and matching Zot username -- expected: both missing v2.6.0 images push or reconcile by digest; no NuGet publication occurs.

**Actual results:**
- Rebuilt and published both v2.6.0 images from the exact release commit; an authenticated rerun reconciled both as `already-present` by descriptor digest.
- Repaired the partial v2.6.1 release by rebuilding from commit `86f5186`, publishing both missing images, and reconciling both on rerun.
- Updated the repository Zot Actions secrets to the verified username/key pair without persisting either value in the repository.
- Reproduced CI run 29329108431 from its exact archives; evidence identified an unschedulable surge pod, and the capacity-bounded rollout passed with zero skips after scaling Server 2→1→2 around the Dapr fault.

## Suggested Review Order

**Container publication normalization**

- Start with the release boundary that normalizes and reconciles each archive.
  [`publish-containers.ps1:261`](../../tools/publish-containers.ps1#L261)

- Reject ambiguous, unrelated, or malformed loaded references before canonical tagging.
  [`publish-containers.ps1:275`](../../tools/publish-containers.ps1#L275)

**Rollout fault and recovery semantics**

- Select only running containers from the required rollout revision.
  [`verify-production-deployment.ps1:120`](../../tools/verify-production-deployment.ps1#L120)

- Enforce HTTP status, aggregate health, stage labels, and bounded retries together.
  [`verify-production-deployment.ps1:195`](../../tools/verify-production-deployment.ps1#L195)

- Persist redacted Kubernetes state and per-container logs before teardown.
  [`verify-production-deployment.ps1:265`](../../tools/verify-production-deployment.ps1#L265)

- Require faulted and restored Dapr revisions instead of accepting older replicas.
  [`verify-production-deployment.ps1:524`](../../tools/verify-production-deployment.ps1#L524)

**Evidence enforcement**

- Validate the complete evidence schema, log set, and secret canaries.
  [`validate-production-deployment-evidence.ps1:13`](../../tools/validate-production-deployment-evidence.ps1#L13)

- Run validation even after verifier failure, then upload retained evidence.
  [`ci.yml:412`](../../.github/workflows/ci.yml#L412)

**Supporting verification**

- Exercise real registry-less, image-ID, ambiguous, and immutable-tag cases.
  [`publish_containers_test.py:345`](../../tests/tooling/publish_containers/publish_containers_test.py#L345)

- Prove complete success/failure evidence and redaction failure gates.
  [`production_deployment_evidence_test.py:56`](../../tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py#L56)

- Pin workflow wiring and the revision-aware verifier contract.
  [`CiTestInventoryTests.cs:328`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L328)
