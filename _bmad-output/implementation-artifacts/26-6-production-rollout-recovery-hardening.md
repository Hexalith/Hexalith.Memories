---
title: 'Production rollout recovery hardening'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
route: 'one-shot'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-container-publication-and-rollout-verification.md'
---

# Production rollout recovery hardening

## Intent

**Problem:** The disposable production verifier could exceed single-node rollout capacity, accept incomplete success evidence, or leave a retained cluster with altered replicas, strategy, or unhealthy Server/MCP state after a fault-path failure.

**Approach:** Capture the deployment's exact state, use a zero-surge fault strategy, mark mutations before issuing them, and restore replicas, strategy, rollout readiness, and both aggregate health surfaces before clearing recovery state. Preserve the 60-second startup contract by using the Kubernetes Ready transition when sequential probes observe an already-healthy container.

## Acceptance

- Fault rollouts remain schedulable with `maxSurge: 0` and `maxUnavailable: 1`.
- Every mutation path restores the captured replicas and strategy exactly.
- Cleanup verifies Server and MCP health before a retained cluster is considered restored.
- Success evidence is accepted only at `required-server-mcp-restored`.
- Startup timing remains bounded without extending retries or timeouts.

## Verification

- PowerShell parser validation passed for the verifier and evidence validator.
- Production deployment evidence fixtures passed: 6/6.
- `CiTestInventoryTests` passed: 55/55.
- The complete kind verifier passed with zero skips using the CI-pinned Dapr runtime and kind node image.
- The captured post-run evidence passed `validate-production-deployment-evidence.ps1`.
- After reconciling main's production-dependency staging, the combined inventory passed: 56/56.
- Partial-release recovery now rebuilds and reconciles container images from the trusted release tag when the original release commit is behind `main`.

## File Scope

Allowed files for this story:

- `tools/verify-production-deployment.ps1` - UPDATE. Preserve capacity, measure startup truthfully, and restore exact deployment/application health state.
- `tools/validate-production-deployment-evidence.ps1` - UPDATE. Require the final Server/MCP restoration stage for success.
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` - UPDATE. Cover final-stage evidence enforcement and recovery wiring.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE. Pin the startup, restoration, cleanup, and evidence contracts.
- `.github/workflows/recover-partial-release.yml` - ADD. Reconcile missing immutable container tags from an existing trusted release tag.
- `docs/dev/release-runbook.md` - UPDATE. Distinguish normal reruns from tagged container-only recovery after `main` advances.
- `_bmad-output/implementation-artifacts/26-6-production-rollout-recovery-hardening.md` - ADD. Record intent, evidence, and the exact permitted change surface.

Read/verify only:

- `.github/workflows/ci.yml`
- `deploy/kubernetes/base/server-deployment.yaml`
- `deploy/kubernetes/base/mcp-deployment.yaml`

Forbidden by default:

- Do not extend health or rollout timeouts.
- Do not make production verification advisory or skippable.
- Do not change registry credentials, release tags, or immutable publication policy.
