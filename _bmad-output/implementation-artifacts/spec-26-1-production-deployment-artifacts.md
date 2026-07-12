---
title: 'Story 26.1: Production Deployment Artifacts'
type: 'feature'
created: '2026-07-11'
status: in-review
baseline_revision: 82b421998cd77cf7234ec4ff3f71266e1173105d
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-26-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** The repository models a development topology in Aspire but cannot produce or validate production-ready Server and MCP images plus a complete orchestrator deployment. Existing DAPR templates retain development-only components and permissive credential fallbacks.

**Approach:** Add .NET SDK container publication for the runnable hosts, commit an operator-renderable deployment derived from the Aspire topology, harden its DAPR/security/persistence configuration, and gate the resulting artifacts in tests and release automation.

## Resolved Production Decisions

1. **Artifact format:** Commit a Kustomize base at `deploy/kubernetes/base` and a production overlay at `deploy/kubernetes/overlays/production`. `kubectl kustomize deploy/kubernetes/overlays/production` is the authoritative deterministic render; neither an Aspire-published nor a hand-authored Helm chart is part of this story. The AppHost remains the topology reference, not a deployed workload.
2. **Conversation provider:** The production DAPR component is named `llm-openai`, has type `conversation.openai` and version `v1`, selects model `gpt-4o-mini`, sets `responseCacheTTL` to `0s`, and is scoped to app-id `memories`. Its `key` metadata uses `secretKeyRef` through `auth.secretStore: secretstore`; the production secret store is `secretstores.kubernetes`, and the referenced Kubernetes Secret is named `llm-secret` with data key `OPENAI_API_KEY`. Production must not render the development `llm` / `conversation.echo` component or unsupported PII-scrubbing component metadata.
3. **Pub/sub authorization:** The only authorized publisher app-id is `eventstore`; `memories` is the subscriber and must not publish. The Redis pub/sub component is scoped to `[eventstore, memories]`, fixes the topic to `memories-events`, and renders `allowedTopics` and `protectedTopics` as `memories-events`, `publishingScopes` as `eventstore=memories-events;memories=`, and `subscriptionScopes` as `eventstore=;memories=memories-events`. Do not use the invalid `publishAllowedTopics` metadata name or grant direct module app-ids that are not evidenced by the repository.
4. **MCP-to-Server identity:** Server and MCP use the same operator-supplied production OIDC authority, issuer, audience, and tenant-claim contract. After validating the inbound bearer and tenant claims, MCP forwards that same bearer unchanged on DAPR service invocation to Server; production does not mint an HS256 `Authentication:ServerUpstream` token. DAPR access control is deny-by-default and permits caller app-id `memories-mcp` from the configured trust domain and namespace to invoke only the required Server `/api/v1/**` operations. `APP_API_TOKEN` and `DAPR_API_TOKEN` remain external secret references, and the application port is not exposed outside the pod.
5. **Bootstrap resources:** Render the following operator-overridable requests, limits, and persistent-volume sizes. These are initial scheduling defaults, not capacity guarantees. Server, MCP, and daprd containers are stateless; only Redis `/data` and FalkorDB data receive PVCs. Redis capacity must additionally account for vector dimensions, metadata, actor/workflow history, AOF growth, and no-eviction headroom.

   | Resource | CPU request | Memory request | CPU limit | Memory limit | PVC |
   |----------|-------------|----------------|-----------|--------------|-----|
   | Server | `500m` | `512Mi` | `2` | `2Gi` | none |
   | MCP | `100m` | `128Mi` | `500m` | `512Mi` | none |
   | Server daprd | `250m` | `256Mi` | `1` | `512Mi` | none |
   | MCP daprd | `100m` | `128Mi` | `500m` | `256Mi` | none |
   | Redis Stack | `500m` | `1Gi` | `2` | `4Gi` | `20Gi` mounted at `/data` |
   | FalkorDB | `500m` | `1Gi` | `2` | `4Gi` | `10Gi` mounted at the image's persistent data path |

6. **Readiness semantics:** An isolated RediSearch, Redis Vector, or FalkorDB capability failure remains `Degraded` with HTTP 200 so available search axes continue serving. Redis connectivity, DAPR sidecar or state-store failure, MCP upstream failure, missing required secrets, and invalid production configuration remain `Unhealthy`, fail closed, and return HTTP 503 where the host is running. Startup/rollout validation must parse `/ready` and observe aggregate JSON `status: Healthy` for both Server and MCP within 60 seconds after their containers are running; HTTP 200 alone is insufficient because it may mean `Degraded`.

## Boundaries & Constraints

**Implementation baseline:** Re-drive this story from parent-repository commit `bb47d3007935d263eee663cd9e0a4705b33b4929` or its re-arm descendant. The committed gitlinks at that baseline for `references/Hexalith.Commons`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.PolymorphicSerializations` are approved dependency inputs, not story changes; do not revert or modify them for this story.

**Always:** Publish containers through the .NET SDK without Dockerfiles; deploy Server, MCP, Redis Stack, FalkorDB, DAPR sidecars, actor-enabled state store, pub/sub, secret store, health probes, persistent volumes, and explicit CPU/memory requests and limits; use pinned backend images and secret references rather than committed credentials; preserve tenant isolation and DAPR authentication; keep ingress infrastructure-owned; require Server and MCP to become ready within 60 seconds after their containers are running, excluding image pulls.

**Block If:** The Kustomize render requires literal secret values; deviates from the provider, publisher, identity, resource, or readiness decisions above; exposes an application port outside its pod; or cannot prove aggregate `Healthy` startup within 60 seconds when all required dependencies are available.

**Never:** Introduce Dockerfiles, deploy the AppHost as the production application, commit production Secret values, permit empty Redis passwords, ship `conversation.echo`, weaken authentication or tenant isolation, assume an ingress controller/TLS issuer not owned by the infrastructure, or broaden this story into backup/restore and operational-runbook work owned by later Epic 26 stories.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Container publication | Release version and Server/MCP projects | Two runnable OCI archives/images, non-root on port 8080, tagged with the release version | Publication fails when repository/image metadata is incomplete |
| Deployment rendering | Production Kustomize overlay and external secret references | `kubectl kustomize deploy/kubernetes/overlays/production` emits the complete deterministic topology with probes, limits, persistence, DAPR scoping, and no literal credentials | Render or client-side schema validation fails on missing required values |
| Startup readiness | Images are already present, containers start, and required dependencies are healthy | Server and MCP `/ready` JSON reports aggregate `Healthy` within 60 seconds | Rollout rejects HTTP 200 responses whose aggregate JSON status is `Degraded`, remains unready, and exposes dependency-specific evidence |
| Optional search degradation | RediSearch, Redis Vector, or FalkorDB alone becomes unavailable after startup | `/ready` returns HTTP 200 with aggregate `Degraded`; unaffected axes continue serving and the response identifies affected capabilities | Capability-specific requests expose actionable degradation without removing the pod from service |
| Critical dependency failure | Redis connectivity, DAPR sidecar/state store, or MCP upstream is unavailable | `/ready` returns HTTP 503 with aggregate `Unhealthy` | Workload fails closed without credential disclosure |
| Secret/config audit | Rendered production artifact | No echo provider, empty password, development signing key, or committed Secret value | Guard test fails and identifies the offending resource |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.AppHost/Program.cs` -- canonical local topology and DAPR resource relationships to project into production artifacts.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` -- runnable Server container publication opt-in.
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` -- runnable MCP container publication opt-in.
- `deploy/dapr/config.yaml` -- DAPR secret access and future service-invocation policy.
- `deploy/dapr/components/` -- current development-oriented state store, pub/sub, secret store, and echo Conversation templates.
- `.github/workflows/release.yml` -- NuGet-only semantic-release workflow that must publish matching image versions.
- `tests/Hexalith.Memories.Server.Tests/Deployment/` -- established content-based deployment contract guardrails.
- `docs/operations/deployment-configuration.md` -- published deployment configuration contract and operator entry point.

## Tasks & Acceptance

**Execution:**
- `Directory.Build.targets` and the Server/MCP project files -- centralize SDK container defaults and opt both runnable hosts into versioned publication.
- `deploy/kubernetes/base/` and `deploy/kubernetes/overlays/production/` -- define the full Kustomize topology, security boundaries, probes, limits, persistence, and ingress seam.
- `deploy/dapr/config.yaml` and `deploy/dapr/components/*.yaml` -- replace development fallbacks with scoped, secret-backed production components aligned to selected providers and app identities.
- `.github/workflows/release.yml` and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- publish both image tags with the semantic-release version and retain release-structure safeguards.
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` -- validate container metadata and rendered deployment invariants, including forbidden-value scans.
- `docs/operations/deployment-configuration.md` and `docs/dev/health-checks.md` -- document required values, render/apply/verify/rollback flow, port 8080 probes, and deployment evidence.

**Review-derived hardening tasks:**
- `Directory.Build.targets`, `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`, `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`, and executable deployment tests -- publish both images with a numeric non-root UID accepted by Kubernetes, inspect the produced OCI configuration, and start the images rather than proving publication through source-string assertions alone.
- `tools/publish-containers.ps1`, release configuration, and release-contract tests -- make Server/MCP publication retry-safe and observable as a two-image release unit; record per-image outcomes, surface partial publication to the existing failure alert, and render a release deployment artifact whose two image tags equal the semantic-release version instead of pinning an unreleased repository version.
- `deploy/kubernetes/base/` and deployment tests -- add namespace-local least-privilege ServiceAccounts/RBAC for the exact DAPR-managed Secrets, an external registry pull-secret reference, secret-store scope for both `memories` and `eventstore`, and every runtime embedding/conversation secret name required by production configuration. Structurally associate each scope, permission, resource bound, probe, and Service target with its owning object; do not rely only on global substring presence.
- Server and MCP composition roots plus focused tests -- enforce `APP_API_TOKEN`/`dapr-api-token` validation on sidecar-to-app traffic. Publish an anonymous application health operation under `/api/v1/health` whose exposure is constrained by DAPR workload identity, point MCP upstream health at that operation, keep the production ACL limited to `/api/v1/**`, and treat an unavailable MCP upstream as immediately `Unhealthy` rather than tolerating initial failures as `Degraded`.
- Server publication/deployment composition and cache-safety tests -- include or mount the canonical production Conversation component material at the exact runtime path consumed by cache-safety validation so the published image cannot silently bypass the nonzero shared-cache guard.
- `tests/Hexalith.Memories.Server.Tests/Deployment/`, a checked-in deployment verification tool, and CI/release wiring -- automate Kubernetes schema validation and a disposable DAPR-enabled cluster rollout. Load the two locally published images, provision non-literal test Secrets/ConfigMaps, validate Secret RBAC and production ACL behavior, prove both aggregate `/ready` documents become `Healthy` within 60 seconds measured after application containers are running, and exercise optional-axis degradation plus Redis/DAPR/MCP-upstream fail-closed behavior at their public surfaces. The test must run, pass, and have zero skips; operator documentation is not a substitute.
- Backend StatefulSets and deployment tests -- give Redis Stack and FalkorDB startup probes long enough for persistent-data recovery and make configuration changes trigger a controlled rollout while preserving the frozen resource/PVC defaults.

**Acceptance Criteria:**
- Given a Release build, when Server and MCP are published through the SDK, then both OCI artifacts run as non-root on port 8080 and carry the same explicit release version.
- Given production deployment inputs containing only external secret references, when `deploy/kubernetes/overlays/production` is rendered and client-side validated, then it contains the complete topology, persistence, probes, exact DAPR provider/topic/app-id restrictions, and bootstrap resource bounds above without forbidden development values.
- Given images are already available and all required backends are healthy, when the production workload starts, then Server and MCP report aggregate `Healthy` within 60 seconds and MCP-to-Server DAPR invocation succeeds by forwarding a validated same-authority/same-audience OIDC bearer under the deny-by-default DAPR ACL.
- Given only RediSearch, Redis Vector, or FalkorDB is degraded after startup, when readiness is probed, then `/ready` remains HTTP 200 with aggregate `Degraded`, identifies the affected capabilities, and leaves unaffected search axes available.
- Given a missing secret, invalid deployment value, Redis connectivity failure, DAPR sidecar/state-store failure, or MCP upstream failure, when render or rollout validation runs, then the workload fails closed with actionable validation or HTTP 503 health evidence and no credential disclosure.

## Spec Change Log

- 2026-07-12: Human selected the repository-aligned Kustomize production baseline. Frozen intent now fixes the OpenAI component and secret contract, `eventstore`-only publication, shared-OIDC bearer forwarding with a deny-by-default DAPR ACL, bootstrap resource/PVC values, and capability-aware readiness semantics.
- 2026-07-12: Human confirmed current parent `HEAD` as the implementation baseline after a re-arm race; its four committed dependency gitlinks are approved inputs and are outside this story's change set.
- 2026-07-12: Review found the first implementation plan could pass through source/render substring checks while shipping an unrunnable or security-incomplete topology. Amended the executable tasks and verification to require numeric non-root OCI evidence, release-version manifest coupling and partial-publish recovery, exact Secret RBAC/scopes/token enforcement, an ACL-compatible `/api/v1/health` path with immediate critical failure semantics, cache-safety material in the published runtime, and a no-skip disposable-cluster rollout proving aggregate `Healthy` within 60 seconds. Known-bad state avoided: stale/nonexistent image tags, named-user admission failure, denied MCP health, unresolved DAPR secrets, fail-open app-token/cache guards, and declarative-only readiness evidence. KEEP: the Kustomize base/production-overlay format; SDK container publication; exact OpenAI/pub-sub/app-id/resource/PVC decisions; ingress-free application ports; shared-OIDC bearer forwarding; local/production DAPR separation; aggregate-JSON startup probes; and operator render/apply/rollback documentation.

## Review Triage Log

### 2026-07-12 — Review pass
- intent_gap: 0
- bad_spec: 6: (high 6, medium 0, low 0)
- patch: 0
- defer: 0
- reject: 14: (high 0, medium 4, low 10)
- addressed_findings:
  - `[high]` `[bad_spec]` Added executable OCI publication/startup evidence and numeric Kubernetes-compatible non-root identity instead of source-only metadata checks.
  - `[high]` `[bad_spec]` Added release-version manifest coupling, retry-safe two-image publication accounting, and partial-publish alert evidence.
  - `[high]` `[bad_spec]` Added exact production Secret RBAC, secret-store scopes/names, registry seam, and sidecar-to-app API-token enforcement.
  - `[high]` `[bad_spec]` Resolved the deny-by-default ACL/readiness mismatch through `/api/v1/health` and immediate MCP-upstream `Unhealthy` semantics.
  - `[high]` `[bad_spec]` Required the cache-safety validator's production component material to exist in the published runtime.
  - `[high]` `[bad_spec]` Replaced declarative-only readiness evidence with automated schema validation and a no-skip DAPR-enabled rollout proving live aggregate health and failure semantics.

## Design Notes

The selected Kustomize artifact aligns with the existing downstream-overlay contract. Aspire-published Helm is not authoritative because its current publisher output requires manual DAPR supplementation. The EventStore platform is the evidenced publisher (`eventstore`); Tenants and Parties are logical CloudEvent sources, not production publisher app-id grants. Shared OIDC bearer forwarding matches the AppHost's common authority/audience model while DAPR access control supplies the workload-identity boundary.

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release && dotnet build Hexalith.Memories.slnx --configuration Release -m:1 --no-restore` -- expected: Release build succeeds with warnings as errors.
- `dotnet publish src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/memories-server.tar.gz` -- expected: Server OCI archive is created.
- `dotnet publish src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/memories-mcp.tar.gz` -- expected: MCP OCI archive is created.
- `kubectl kustomize deploy/kubernetes/overlays/production > /tmp/hexalith-memories-production.yaml && kubectl apply --dry-run=client -f /tmp/hexalith-memories-production.yaml` -- expected: deterministic valid resources and no forbidden development values.
- Build the focused test project and invoke its xUnit v3 assembly directly -- expected: deployment and release contract tests pass.
- Invoke the checked-in disposable-cluster deployment verifier with the two locally published images -- expected: schema validation, RBAC/ACL checks, live aggregate `Healthy` within 60 seconds after both application containers are running, optional-axis HTTP 200 `Degraded`, and critical-dependency HTTP 503 `Unhealthy`; zero skips are permitted.
