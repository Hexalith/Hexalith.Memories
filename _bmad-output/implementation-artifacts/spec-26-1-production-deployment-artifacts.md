---
title: 'Story 26.1: Production Deployment Artifacts'
type: 'feature'
created: '2026-07-11'
status: 'blocked'
review_loop_iteration: 0
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

## Boundaries & Constraints

**Always:** Publish containers through the .NET SDK without Dockerfiles; deploy Server, MCP, Redis Stack, FalkorDB, DAPR sidecars, actor-enabled state store, pub/sub, secret store, health probes, persistent volumes, and explicit CPU/memory requests and limits; use pinned backend images and secret references rather than committed credentials; preserve tenant isolation and DAPR authentication; keep ingress infrastructure-owned; require Server and MCP to become ready within 60 seconds after their containers are running, excluding image pulls.

**Block If:** No decision identifies (1) Kustomize overlays versus a Helm chart/Aspire-published Helm artifact, (2) the production DAPR Conversation provider/model and required secret names, (3) the authorized pub/sub publisher app IDs, (4) the supported production MCP-to-Server identity flow, (5) initial CPU/memory and persistent-volume sizing, or (6) whether Redis search/FalkorDB degradation must make `/ready` return 503 rather than the current degraded HTTP 200.

**Never:** Introduce Dockerfiles, deploy the AppHost as the production application, commit production Secret values, permit empty Redis passwords, ship `conversation.echo`, weaken authentication or tenant isolation, assume an ingress controller/TLS issuer not owned by the infrastructure, or broaden this story into backup/restore and operational-runbook work owned by later Epic 26 stories.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Container publication | Release version and Server/MCP projects | Two runnable OCI archives/images, non-root on port 8080, tagged with the release version | Publication fails when repository/image metadata is incomplete |
| Deployment rendering | Production values and external secret references | Complete deterministic topology with probes, limits, persistence, DAPR scoping, and no literal credentials | Render or schema validation fails on missing required values |
| Startup readiness | Images are already present and containers start | Server and MCP report ready within 60 seconds with required dependencies available | Rollout remains unready and exposes dependency-specific health evidence |
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
- `deploy/` production orchestrator artifact path, selected by the blocked format decision -- define the full parameterized topology, security boundaries, probes, limits, persistence, and ingress seam.
- `deploy/dapr/config.yaml` and `deploy/dapr/components/*.yaml` -- replace development fallbacks with scoped, secret-backed production components aligned to selected providers and app identities.
- `.github/workflows/release.yml` and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- publish both image tags with the semantic-release version and retain release-structure safeguards.
- `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` -- validate container metadata and rendered deployment invariants, including forbidden-value scans.
- `docs/operations/deployment-configuration.md` and `docs/dev/health-checks.md` -- document required values, render/apply/verify/rollback flow, port 8080 probes, and deployment evidence.

**Acceptance Criteria:**
- Given a Release build, when Server and MCP are published through the SDK, then both OCI artifacts run as non-root on port 8080 and carry the same explicit release version.
- Given production deployment inputs containing only external secret references, when the selected artifact is rendered and client-side validated, then it contains the complete topology, persistence, probes, DAPR restrictions, and resource bounds without forbidden development values.
- Given images are already available and all required backends are healthy, when the production workload starts, then Server and MCP become ready within 60 seconds and authenticated DAPR invocation succeeds.
- Given a missing secret, unavailable required backend, or invalid deployment value, when rollout validation runs, then the workload fails closed with actionable health or validation evidence and no credential disclosure.

## Spec Change Log

## Review Triage Log

## Design Notes

Repository evidence supports both a portable Kustomize overlay and an AppHost-derived Helm chart. These produce different files, toolchain gates, and operator interfaces, so the artifact format must be selected before this spec can become actionable. The production identity and provider decisions similarly affect application configuration and security tests, not only deployment values.

## Verification

**Commands:**
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release && dotnet build Hexalith.Memories.slnx --configuration Release -m:1 --no-restore` -- expected: Release build succeeds with warnings as errors.
- `dotnet publish src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/memories-server.tar.gz` -- expected: Server OCI archive is created.
- `dotnet publish src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/memories-mcp.tar.gz` -- expected: MCP OCI archive is created.
- Render, schema-validate, and lint the selected production artifact -- expected: deterministic valid resources and no forbidden development values.
- Build the focused test project and invoke its xUnit v3 assembly directly -- expected: deployment and release contract tests pass.

## Auto Run Result

Status: blocked
Blocking condition: intent gap. Required decisions are the production artifact format, Conversation provider/model and secrets, authorized pub/sub publishers, MCP-to-Server identity flow, initial resource/storage sizing, and readiness semantics for degraded Redis search or FalkorDB. Evidence: no Kubernetes/Helm artifacts exist; current DAPR templates use `conversation.echo`, local-file secrets, and empty-password fallbacks; release automation publishes NuGet only; production appsettings expect `llm-openai` while the shipped component is `llm`; MCP upstream tokens use a symmetric signing key while production ingress uses OIDC; `/ready` currently returns HTTP 200 for degraded Redis/FalkorDB checks.
