---
title: 'Complete six deferred trust, lifecycle, deployment, and restore fixes'
type: 'bugfix'
created: '2026-11-26'
status: 'in-progress'
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
