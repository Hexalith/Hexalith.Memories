---
title: 'Story 26.1: Production Deployment Artifacts'
type: 'feature'
created: '2026-07-11'
status: review
baseline_revision: 82b421998cd77cf7234ec4ff3f71266e1173105d
review_loop_iteration: 2
followup_review_recommended: true
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
- 2026-07-12: `dev-story` validation pass. Re-verified the committed implementation against every gate runnable in the development sandbox (Release build 0/0, 26 deployment-contract tests, deterministic production kustomize render, Server+MCP OCI publication with numeric non-root UID `1654` on port 8080 and matching version) and advanced sprint tracking from `in-progress` to `review`. The mandatory disposable-cluster DAPR rollout could not run (no `docker`/`kind` in the dev sandbox) and remains a blocking gate before `done`. See the Dev Story Validation section.

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

## Dev Story Validation

Status: review (dev-complete; awaiting code review and the live-cluster rollout gate)

Recorded by the `dev-story` workflow on 2026-07-12. The Story 26.1 production-deployment implementation already exists on `main` (working tree clean). This pass re-validated it against every check runnable in the development sandbox and advanced sprint tracking from `in-progress` to `review`. No source changed in this pass — it is validation-only.

**Verification performed (all passed):**
- `dotnet restore` + `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 --no-restore` — Build succeeded, 0 Warning(s), 0 Error(s) (warnings-as-errors gate across the full solution and submodules).
- Deployment contract tests via the xUnit v3 assembly `Hexalith.Memories.Server.Tests` (namespace `Hexalith.Memories.Server.Tests.Deployment`, run with `dotnet exec` and `DiffEngine_Disabled=true` per the sandbox test-runner workaround) — Total 26, Errors 0, Failed 0, Skipped 0, Not Run 0. Covers `ProductionDeploymentArtifactsTests`, `DeploymentConfigurationContractTests`, `RouteSurfaceContractTests`, and `AppHostSecurityConfigurationTests`: exact security/persistence/resource contracts, forbidden-value scans (no `conversation.echo`, `SigningKey`, `Authentication__ServerUpstream`, `kind: Secret`), resource-name-bound get-only Secret RBAC, DAPR deny-by-default ACL with `appId: memories-mcp` and `/api/v1/**`, and Services targeting `3500` never the application port `8080`.
- `kubectl kustomize deploy/kubernetes/overlays/production` — deterministic render, exit 0, 831 lines, complete topology (2 Deployments, 2 StatefulSets, 4 Services, 4 DAPR Components, 2 Configurations, 3 ServiceAccounts / 3 Roles / 3 RoleBindings, 3 ConfigMaps, 1 Namespace).
- `dotnet publish -t:PublishContainer` for Server and MCP (`ContainerArchiveOutputPath`, shared `Version=26.1.0-validation`) — both OCI archives built. Inspected image config on each: `User=1654` (numeric non-root), `ExposedPorts=8080/tcp`, `ASPNETCORE_HTTP_PORTS=8080`, `org.opencontainers.image.version=26.1.0-validation`, `RepoTags` carrying the identical version. Satisfies AC-1 (both artifacts run as non-root on 8080 and carry the same explicit release version).

**Environment-blocked verification (must run and pass before `done`):**
- The spec mandates a no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) proving both Server and MCP `/ready` documents reach aggregate `Healthy` within 60 seconds after their application containers are running, plus Secret RBAC / production ACL enforcement, optional-axis HTTP 200 `Degraded`, and Redis/DAPR/MCP-upstream HTTP 503 `Unhealthy` fail-closed behavior. That verifier requires `docker` + `kind`; neither is available in this sandbox (only `kubectl` client v1.34.1, `dapr`, `pwsh` 7.6.2, and `dotnet` 10.0.302 are present). This gate is therefore **deferred to CI or an operator cluster and is NOT satisfied by this pass**. Per the spec, operator documentation is not a substitute — do not advance 26.1 to `done` until this rollout executes with zero skips and passes.

**File List (validation-only; no changes this pass):** The Story 26.1 artifacts under review are the already-committed `Directory.Build.targets`; `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`; `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`; `deploy/kubernetes/base/**` and `deploy/kubernetes/overlays/production/**`; `deploy/dapr/config.yaml` and `deploy/dapr/components/**`; `tools/publish-containers.ps1` and `tools/verify-production-deployment.ps1`; `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs`; `.github/workflows/release.yml` and `.releaserc.json`; `docs/operations/deployment-configuration.md` and `docs/dev/health-checks.md`. This spec file is the only file modified by this pass (validation record + sprint status).

**Residual risks:** Live-cluster health/ACL/degradation behavior is asserted here only by content and render contracts; the runtime rollout remains the authoritative gate. Full Docker/Aspire integration lanes were not run.

## Review Findings

### Review Findings — 2026-07-12 (bmad-code-review, adversarial: blind-hunter + edge-case-hunter + verification-gap + acceptance-auditor)

Diff reviewed: `82b4219..HEAD` (60 files, ~+2300/−420). Triage: 0 decision-needed · 14 patch · 7 defer · 5 dismissed. All 6 Resolved Production Decisions were independently verified as honored in the authoritative render; findings below concern the deployed startup probe, the verifier's own correctness, verification strength, and hardening.

**Patch findings — applied 2026-07-12.** All patches except the OCI/partial-publish item below landed as code fixes. Evidence: full solution builds **0 warnings / 0 errors**; touched tests pass (**53** Cli inventory, **1** MCP health, **13** Server deployment/auth via `dotnet exec` + `DiffEngine_Disabled=true`); the three modified PowerShell scripts parse clean; the production overlay still renders deterministically (831 lines, anchored aggregate-`Healthy` probe present for both hosts). The `[MEDIUM]` OCI/partial-publish item is left **unchecked** because it is only partially addressed: its OCI UID/port/version half is now executed-verified via the fixed verifier image-tag load path and the newly pinned rollout job, but a stubbed-`dotnet` executed partial-publish test was not added (no in-repo pwsh test harness); that failure branch is exercised only by the `ci.yml` disposable-cluster rollout.

- [x] [Review][Patch] `[HIGH]` Startup probe never enforces aggregate `Healthy` — greps `'"status":"Healthy"'` across the whole `/ready` body, matching any per-entry status (e.g. `dapr-sidecar`, Healthy in most failure modes) even when the top-level aggregate is `Degraded`/`Unhealthy`; defeats Decision 6 / AC-3 ("HTTP 200 alone insufficient") and diverges from the verifier's correct top-level `Wait-AggregateStatus`. Anchor the match to the top-level field (JSON parse, or `grep -Eq '^\{"schemaVersion":[0-9]+,"status":"Healthy"'`). [deploy/kubernetes/base/server-deployment.yaml:124; deploy/kubernetes/base/mcp-deployment.yaml (same block)]
- [x] [Review][Patch] `[HIGH]` Mandatory rollout verifier will fail on first run — image-tag name mismatch: `publish-containers.ps1` builds archives with `-p:ContainerRegistry=registry.hexalith.com` (registry-qualified RepoTag), but the verifier re-tags from the registry-less source `hexalith/memories-server:$Version`, which won't exist after `docker load` → `Invoke-Checked` throws before any health assertion. Make the two scripts agree on the image name. [tools/verify-production-deployment.ps1:189-190 vs tools/publish-containers.ps1:64,72]
- [x] [Review][Patch] `[MEDIUM]` `production-deployment-verification` CI job is not pinned by `CiTestInventoryTests` — it can be deleted/renamed/neutered with every test green (the C# suite only pins `release.yml`/tool-script strings). Add an inventory assertion that `ci.yml` declares the job and invokes `verify-production-deployment.ps1` + `publish-containers.ps1`. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs; .github/workflows/ci.yml:331]
- [x] [Review][Patch] `[MEDIUM]` DAPR ACL asserted by loose substrings, not structural binding — test checks `defaultAction: deny`, `appId: memories-mcp`, `name: /api/v1/**` independently; widening `httpVerb` to include DELETE, changing the operation to `/**`, or adding a second allowed app-id all still pass. Parse the policy node and assert `action: allow` bound to `/api/v1/**` with verbs exactly `[GET,POST]` and exactly one policy app-id. [tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs:105-107]
- [x] [Review][Patch] `[MEDIUM]` OCI contract + partial-publish/retry-safety now has executed coverage: a stubbed failing MCP image publish proves `status=partial-publish`, a redacted summary, and a non-zero exit; a second invocation proves both images are retried and the summary is replaced by `succeeded`. The fixture is pinned in PR CI and before semantic-release. Note: registry-side immutable-tag policy remains an operator prerequisite because OCI registries have no universal `--skip-duplicate` equivalent. [tests/tooling/publish_containers/publish_containers_test.py; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs; .github/workflows/ci.yml; .github/workflows/release.yml]
- [x] [Review][Patch] `[MEDIUM]` `DaprTokenStartupValidator` has no test — the production fail-closed guard (throws when `APP_API_TOKEN`/`DAPR_API_TOKEN` empty) could be made fail-open (log instead of throw, or wrong env-var) undetected. Add a unit test: throws in Production when a token is absent; no-op in Development / when both present. [src/Hexalith.Memories.ServiceDefaults/Security/DaprTokenStartupValidator.cs]
- [x] [Review][Patch] `[MEDIUM]` Token middleware pipeline registration is not integration-tested — the unit test covers the reject logic by constructing the middleware directly, but nothing exercises the `app.UseMiddleware<DaprApplicationTokenMiddleware>()` wiring, so removing that line ships silently (fail-open app port). Add a booted-pipeline test (APP_API_TOKEN set) asserting `/api/v1/*` → 401 without the header, 200 with it. [src/Hexalith.Memories.Server/Program.cs:31; src/Hexalith.Memories.Mcp/Program.cs:18]
- [x] [Review][Patch] `[MEDIUM]` MCP upstream health-check behavior change (removed 3-strike window → immediate `Unhealthy`) has zero tests — a regression reintroducing the degraded window would keep MCP in rotation while its upstream is down, undetected. Add a unit test: one failed probe → `Unhealthy`, success → `Healthy`. [src/Hexalith.Memories.Mcp/Health/MemoriesServerUpstreamHealthCheck.cs]
- [x] [Review][Patch] `[MEDIUM]` MCP readiness/startup/liveness probe `timeoutSeconds: 4` is shorter than the upstream health-check's `HealthProbeTimeout = 5s` — kubelet kills the exec before `ProbeHealthAsync` can return, so an upstream in the 4–5s band or unreachable records a probe timeout instead of the real result, flapping MCP out of rotation. Raise probe timeout above 5s, lower the client timeout, or wrap the check with a shorter per-check timeout. [deploy/kubernetes/base/mcp-deployment.yaml; src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:25]
- [x] [Review][Patch] `[MEDIUM]` Redis/FalkorDB password with special characters (`,` `=` space `$`) breaks the connection string and `--requirepass` — password is inlined in the comma-delimited `ConnectionStrings__redis` and in `REDIS_ARGS: --requirepass $(PASSWORD)`; a space sets a truncated password, a comma/equals injects spurious options. Verifier uses only simple passwords so never catches it. Document the required charset in the deployment contract and/or stop inlining the password. [deploy/kubernetes/base/server-deployment.yaml:57; deploy/kubernetes/base/redis-statefulset.yaml; deploy/kubernetes/base/falkordb-statefulset.yaml]
- [x] [Review][Patch] `[MEDIUM]` Deployment contract tests hard-depend on `kubectl` on PATH in the `test-unit-contract` lane — `ProductionDeploymentArtifactsTests.Run()` shells out to `kubectl kustomize` and throws (not skips) if absent; the lane installs no kubectl and passes today only via GitHub's ambient binary. On a runner without it the "26 tests, 0 skipped" contract silently becomes un-runnable. Add `setup-kubectl` to that lane or make the dependency explicit. [tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs; .github/workflows/ci.yml:173]
- [x] [Review][Patch] `[LOW]` Semver build-metadata (`+build`) passes the version regex but is an invalid OCI/Docker tag — `-p:ContainerImageTag=1.2.3+build7`, `docker tag`, and `kind load` all reject `+`. Reject build-metadata in the tag path or strip it before tagging. [tools/publish-containers.ps1:17; tools/render-production-deployment.ps1:18]
- [x] [Review][Patch] `[LOW]` Verifier `kubectl auth can-i` merges stderr into the verdict (`2>&1 -join ''`) — any deprecation/warning line concatenated onto `yes`/`no` fails the exact-equality RBAC check on a correctly-configured cluster. Capture stdout only or trim to the last line. [tools/verify-production-deployment.ps1:215-226]
- [x] [Review][Patch] `[LOW]` Verifier `Wait-AggregateStatus` measures the 60s startup budget from a stale `$runningAt` — set once on first-observed running container and never reset while `Get-PodName` re-selects the newest pod; a restart during startup fails a healthy new container spuriously. Reset `$runningAt` when the selected pod changes. [tools/verify-production-deployment.ps1:108-137]

**Deferred findings:**

- [x] [Review][Defer] `[HIGH]` Disposable-cluster DAPR rollout gate has never executed — every runtime AC (AC-3/4/5 + AC-1 container-start) rests on `verify-production-deployment.ps1`, unrunnable in the dev sandbox (no docker/kind). Environment-blocked; already tracked by the story. MUST run green with zero skips before `done` (and note the patch fixing the verifier image-tag mismatch above is a prerequisite for it passing). [tools/verify-production-deployment.ps1; sprint-status.yaml]
- [x] [Review][Defer] `[MEDIUM]` Redis Stack and FalkorDB containers run as root — only `allowPrivilegeEscalation:false` + `drop:[ALL]` + seccomp; no `runAsNonRoot`/`runAsUser`. Not AC-mandated (AC-1 covers Server/MCP); making the data stores non-root needs `fsGroup`/PVC-permission handling. Hardening beyond this story's AC. [deploy/kubernetes/base/redis-statefulset.yaml; deploy/kubernetes/base/falkordb-statefulset.yaml]
- [x] [Review][Defer] `[MEDIUM]` No NetworkPolicies or Pod Security Standards — app port 8080 (no Service) is reachable by pod IP cluster-wide and health endpoints are anonymous; data stores are reachable behind only a password. Network hardening beyond this story's AC. [deploy/kubernetes/base/**]
- [x] [Review][Defer] `[LOW]` Cross-tenant cache-safety guard validates a container-bundled copy (`deploy/dapr/components/conversation-llm.yaml`), not the deployed component (`deploy/kubernetes/base/dapr/conversation-openai.yaml`) — both are `0s` today and the Production no-TTL branch closes the missing-material hole, but a nonzero TTL set on the deployed component is invisible to the guard. Near-best-achievable (app cannot read a control-plane component); drift seam. [src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs]
- [x] [Review][Defer] `[LOW]` Two divergent DAPR config sources — `deploy/dapr/config.yaml` + `deploy/dapr/components/*` were rewritten but are not consumed by the authoritative `kubectl kustomize` render (which uses `deploy/kubernetes/base/dapr/*`); must be hand-synced (one file is load-bearing only via image copy). Also: `eventstore` gets a namespace-local ServiceAccount/Role/RoleBinding with no workload deployed here (external publisher by design). Cleanup. [deploy/dapr/**; deploy/kubernetes/base/service-accounts-rbac.yaml]
- [x] [Review][Defer] `[LOW]` Server/MCP images are tag-only with `imagePullPolicy: IfNotPresent` while data stores are digest-pinned (`@sha256:`) — safe only while release tags stay immutable; a reused tag lets nodes run a stale cached layer. [deploy/kubernetes/base/server-deployment.yaml; deploy/kubernetes/base/mcp-deployment.yaml]
- [x] [Review][Defer] `[LOW]` `readOnlyRootFilesystem: true` with only `/tmp` writable may fault ASP.NET Core Data Protection (default key ring `~/.aspnet/DataProtection-Keys`) if antiforgery/cookie key material is ever touched — unverified; no gate that ran boots the app under the read-only rootfs. [deploy/kubernetes/base/server-deployment.yaml; deploy/kubernetes/base/mcp-deployment.yaml]

**Dismissed (5):** `identity.example.com` OIDC authority in the production overlay (documented placeholder — "never deploy those placeholders unchanged"; operator replaces via downstream overlay); `eventstore` publisher has no in-namespace workload (external EventStore platform is the evidenced publisher by design, documented as a downstream contract); MCP serving path unverified / deny-all sidecar config (external MCP reachability is via infrastructure-owned ingress, explicitly out of scope; sidecar ACL governs DAPR invoke, not ingress); token middleware fails open when `APP_API_TOKEN` empty (deliberate for non-Production; Production fails closed via `DaprTokenStartupValidator`); MCP immediate-`Unhealthy` flap risk (the immediacy is spec-mandated by Decision 4 — the actionable probe-timeout part is captured as a patch above).

### Review Findings — 2026-07-12 (chunk 1: container publication and CI/release)

- [x] [Review][Patch] `[HIGH]` Two-image retries can oscillate forever against an immutable registry because every rerun republishes both tags and treats an already-existing matching tag as failure. [tools/publish-containers.ps1:65]
- [x] [Review][Patch] `[HIGH]` Release publication has no aggregate state, so cross-family partial releases can be missed while dual partial summaries can race into duplicate or incomplete reconciliation issues. [tools/publish-release.ps1:10]
- [x] [Review][Patch] `[HIGH]` Published Server and MCP images are not inspected to prove `appsettings.Development.json` and its development signing keys are absent. [src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:15]
- [x] [Review][Patch] `[HIGH]` Container fixtures record arguments but never assert distinct Server/MCP registry repositories, release tags, or `PublishContainer` targets. [tests/tooling/publish_containers/publish_containers_test.py:215]
- [x] [Review][Patch] `[HIGH]` The two-publisher release orchestrator is only text-checked, so a regression can stop after NuGet failure without attempting containers or aggregating both failures. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:279]
- [x] [Review][Patch] `[MEDIUM]` A post-push deployment-render failure can leave no current container summary, or expose a stale local summary, after irreversible image side effects. [tools/publish-containers.ps1:108]
- [x] [Review][Patch] `[MEDIUM]` Container artifacts are first built during the publish phase after NuGet side effects, allowing container build/base-image failures to create an avoidable cross-family partial release. [tools/publish-release.ps1:10]
- [x] [Review][Patch] `[MEDIUM]` Captured container failure output is persisted and issue-posted without redacting the inherited `NUGET_API_KEY`. [tools/publish-containers.ps1:47]
- [x] [Review][Patch] `[MEDIUM]` Total container publication failures retain per-image diagnostics only in an unuploaded summary and emit a generic terminal error. [tools/publish-containers.ps1:85]
- [x] [Review][Patch] `[MEDIUM]` The deterministic render and disposable rollout gates install floating kubectl, kind, and Kubernetes tool versions. [.github/workflows/ci.yml:357]
- [x] [Review][Patch] `[MEDIUM]` Successful `kubectl kustomize` warnings are merged into stdout and written into the released deployment YAML. [tools/render-production-deployment.ps1:33]
- [x] [Review][Patch] `[MEDIUM]` Deployment-asset generation through `pack-release.ps1` is not executed or verified at the exact path consumed by semantic-release. [tools/pack-release.ps1:62]
- [x] [Review][Patch] `[LOW]` A zero-image `publish-failed` result is mislabeled as `PARTIAL CONTAINER PUBLISH` in the Actions annotation. [tools/publish-containers.ps1:142]

**Chunk 1 remediation evidence:** Nine executable container/release orchestration fixtures pass; the established NuGet, release-package, preflight, and story-scope tooling suites pass; 54 CLI workflow inventory tests and 5 Server deployment-contract tests pass with zero skips. A real SDK prepare run produced non-empty Server and MCP archives plus the versioned deployment, and direct layer inspection proved both archives exclude `appsettings.Development.json`. The immutable-registry reconciliation branch is exercised with a stateful fake registry because this sandbox has no authenticated production-registry write path.

## Dev Agent Record

### Implementation Plan

- Add an executable fixture around `publish-containers.ps1` using stubbed `dotnet` and `kubectl` commands.
- Prove the partial-publish failure branch and full two-member retry/recovery behavior.
- Pin the fixture in both PR CI and the release lane before semantic-release.
- Re-run the complete Release, deployment-render, container, and disposable-cluster validation gates.

### Debug Log

- RED: `Workflows_ContainerPublishFixtures_RunBeforeReleasePublish` failed because the fixture was absent from both workflows.
- GREEN: two executed Python cases passed; the C# workflow guard passed after CI/release wiring.
- Release build passed with 0 warnings and 0 errors.
- Docker-free per-project regression lane passed 4,314 tests; one pre-existing submodule-marker guard remained skipped outside this story's scope.
- Production Kustomize rendered deterministically to 831 lines. The live cluster performed the authoritative schema dry-run successfully.
- Server and MCP archives published with version `26.1.0-validation`; both configs report UID `1654`, exposed port `8080/tcp`, `ASPNETCORE_HTTP_PORTS=8080`, and matching OCI version labels.
- `verify-production-deployment.ps1` passed with zero skips on kind v0.31.0 / Kubernetes 1.35 and DAPR 1.18.1, then deleted the disposable cluster.

### Completion Notes

- ✅ Resolved review finding [MEDIUM]: executed partial-publish and retry-safety coverage now validates summary state, non-zero failure exit, secret redaction, both-member retry, and recovery-summary replacement.
- CI and release workflow guards ensure this fixture cannot silently disappear or move after semantic-release.
- All five acceptance criteria are satisfied, including the previously blocked live DAPR/Kubernetes rollout evidence.

## File List

- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/publish_containers/publish_containers_test.py`
- `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-07-12: Closed the final Story 26.1 review patch with executed partial-publish/retry tests, CI/release enforcement, and a successful zero-skip disposable-cluster rollout.

## Status

done
