---
title: 'Complete six deferred trust, lifecycle, deployment, and restore fixes'
type: 'bugfix'
created: '2026-11-26'
status: 'done'
route: 'oneshot'
baseline_commit: '66aa2f26ed4818768e3ae541cafe5907b16705eb'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Implement the next six actionable deferred items in ledger order. Suppress restricted trust metadata, reject permit requests safely during limiter shutdown, and pin the tenant workflow response vocabulary. Run the deployed data stores as non-root, bind production startup validation to the deployed Conversation configuration, and reject unsupported edge origins during restore. Record skipped items, verify the changes, and commit each completed item separately.

</frozen-after-approval>

## Implementation Notes

- DW-539 leaves the generic authorization or availability state visible and withholds all packet-derived trust values outside authorized scope.
- DW-543 registers the shared limiter with host-owned disposal and protects acquisition and partition creation with the same shutdown guard.
- DW-544 exercises real workflow outputs and checks the exact serialized backend names.
- DW-548 grants storage access through the pod group. The rollout verifier uses local PVs that apply group ownership to the data directory.
- DW-550 reads the named Component from the Kubernetes API with the projected service account and CA bundle.
- DW-555 counts unsupported edge origins as invalid records before creating graph stubs and continues with valid records.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-six-deferred-trust-lifecycle-and-restore-fixes.md` - implementation and verification
- `_bmad-output/implementation-artifacts/deferred-work.md` - dispositions
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor` - DW-539 trust metadata
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - DW-539 restriction regressions
- `tools/verify-production-deployment.ps1` - DW-548 provision fsGroup-capable local volumes in kind
- `src/Hexalith.Memories.Server/RateLimiting/InboundRequestRateLimiter.cs` - DW-543 shutdown ownership
- `src/Hexalith.Memories.Server/RateLimiting/NonDisposingRateLimiter.cs` - DW-543 partition delegation
- `src/Hexalith.Memories.Server/RateLimiting/ShutdownRateLimitLease.cs` - DW-543 rejected permit
- `tests/Hexalith.Memories.Server.Tests/RateLimiting/InboundRequestRateLimiterTests.cs` - DW-543 shutdown regressions
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs` - DW-544 compensated axis contract
- `tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs` - DW-544 deleted axis contract
- `deploy/kubernetes/base/redis-statefulset.yaml` - DW-548 Redis process identity and PVC permissions
- `deploy/kubernetes/base/falkordb-statefulset.yaml` - DW-548 FalkorDB process identity and PVC permissions
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` - deployment render regressions
- `docs/operations/deployment-configuration.md` - deployment behavior and storage prerequisites
- `deploy/kubernetes/base/service-accounts-rbac.yaml` - DW-550 scoped Component read
- `src/Hexalith.Memories.Server/NaturalLanguage/KubernetesComponentCacheReader.cs` - DW-550 live Conversation validation
- `tools/access_telemetry_functional_target.py` - DW-550 offline qualification Conversation Component
- `deploy/kubernetes/base/server-deployment.yaml` - DW-550 live configuration opt-in
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` - DW-543 limiter ownership and DW-550 authoritative component location
- `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs` - DW-550 operator diagnostics
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageDescriptionOptionsValidatorTests.cs` - DW-550 startup validation regression
- `src/Hexalith.Memories.Server/Activities/Restore/RestoreDataPlaneActivity.cs` - DW-555 edge origin validation
- `tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreDataPlaneActivityTests.cs` - DW-555 invalid-origin regressions
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/KubernetesComponentCacheReaderTests.cs` - DW-550 API transport and TTL regressions
- `src/Hexalith.Memories.Contracts/V1/ExportedEdge.cs` - DW-555 public origin vocabulary
- `docs/operations/backup-restore.md` - DW-555 skipped-edge recovery guidance

## Review Triage Log

- DW-543. Host registration now disposes the shared limiter and the tests exercise actual host disposal, waiting requests, and both acquisition modes during shutdown.
- DW-548. Rollback guidance preserves the newer datastore identities or restores PVC permissions offline before returning to the old identities.
- DW-550. A live Component read replaces the rendered copy. The reader rejects duplicate and secret-backed TTLs, supports integer TTLs, and verifies API TLS with the in-cluster address and CA bundle. Tests cover host selection, restricted API access, and the response format.
- DW-555. The public export example and restore runbook use the canonical origin values.

## Verification

- The Release solution build and all 69 focused Server tests passed.
- The 88 focused evidence-cockpit tests and all six Chromium specimen checks passed.
- Disposable kind runs proved persisted data survived root-to-non-root rollout and a second restart. Live API verification observed a changed TTL and denied reads outside the scoped Component grant.
- The offline functional-target suite passed all 26 cases.
