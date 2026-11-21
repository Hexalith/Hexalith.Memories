# Technology and deployment evidence review

**November 20, 2026 · Memories `75c5b5f5` plus refreshed top-level submodules**

**Verdict.** The selected software versions, image identities and package/source distinctions match the repository. One deployment claim needs a narrower description of the observed OpenBao topology.

| Finding | Evidence | Required correction |
| --- | --- | --- |
| Medium · OpenBao voting membership | The November 16 capture reports three Ready OpenBao 2.6.2 pods on one Kubernetes node, an active endpoint and two standbys. Voting membership and failover quorum were unmeasured. [Deployment observations](../../../../../docs/operations/openbao.md) | Describe the observed three pods and shared node failure domain. Reserve three-voter membership and failover qualification for evidence that exercises them. |

## Checks

- The root SDK selector names 11.0.100 with `latestFeature` roll-forward. The project targets `net11.0` and C# 14. The selected package catalog and Memories overrides support Aspire 17.0.0, Dapr hosting beta.1078, EventStore 5.4.0 and the listed .NET dependencies. The EventStore gitlink resolves to `607072c68`. The current CI and nightly workflows select Dapr runtime 1.19.1, and Builds selects 1.19.2 for self-hosted development. [SDK](../../../../../global.json) · [Project settings](../../../../../Directory.Build.props) · [Consumer overrides](../../../../../Directory.Packages.props) · [Builds catalog](../../../../../references/Hexalith.Builds/Props/Directory.Packages.props) · [CI](../../../../../.github/workflows/ci.yml) · [Nightly](../../../../../.github/workflows/nightly.yml)
- The seven SHA-256 values in the Stack table match the owned ASP.NET base, Dapr telemetry sidecar, Redis, FalkorDB, PostgreSQL, approved OpenBao server and smoke client. Local hosts and test fixtures use the same FalkorDB digest, while Kubernetes selects a different one. The approved OpenBao input is 2.6.4/chart 0.29.6. The November 16 observation proves server 2.6.2/chart label 0.29.5 and a 2.6.0 client status/TLS probe. [Owned base](../../../../../Directory.Build.targets) · [Kubernetes manifests](../../../../../deploy/kubernetes/base) · [OpenBao inputs](../../../../../deploy/openbao) · [OpenBao observations](../../../../../docs/operations/openbao.md)
- The SDK DomainService has authenticated command, replay and metadata routes for three domains. Native Memories REST and Dapr event ingress retain their own routes. The McpCli inventory requires operation-specific authorization and eligibility for target enrollment. The recorded retention run covers one Development writer and logical expiry. Full Production qualification reopens under the DW-730 trigger. [Route scope](../../../../../docs/operations/route-surface.md) · [McpCli inventory](../../../../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md) · [Retention evidence](../../../../implementation-artifacts/tests/27-4-retention-verification-evidence.md)

**Final disposition. Pass.** The revised Deployment row matches the November 16 observation of three Ready OpenBao pods on one node and identifies voting membership and failover behavior as unmeasured. The finding is closed.
