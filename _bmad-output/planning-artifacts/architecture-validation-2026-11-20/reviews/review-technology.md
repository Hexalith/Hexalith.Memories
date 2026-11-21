# Technology and implementation evidence

**Verdict — revisions required.** The selected stack and seven image digests match the repository. One high and two medium findings affect how teams interpret the current implementation.

Reviewed the working-tree spine `e97c37a46cc0d5c557621d4f6cc075a7f22a8ff1f3a752dd6118d10ff93a2c28` at superproject `75c5b5f5` on November 20, 2026. Line references below refer to that snapshot. Checks used source inspection, manifest comparison and Release-profile MSBuild evaluation. Product qualification requires separate execution evidence.

## High · Memory-unit identifiers issued today differ from the adopted grammar

AD-23 describes uppercase ULIDs as shipped at line 284 and in the grammar table at line 292. The dated correction on line 284 acknowledges multiple issuers but predates the ownership-generation prefix and explicit GUID selection in native ingestion. A new reader enforcing the table could reject identifiers that a current writer has just created.

Native file/URL paths call `NewUnitId()`, which creates a GUID string and applies the configured ownership generation. Generation qualification can produce `owned-<generation>-<identifier>`. Ordinary workflow output preserves its instance ID, retries reuse the prior unit ID, and legacy unqualified CloudEvent workflow IDs can select a fresh GUID. The case issuer uses ULIDs. The tenant validator also rejects reserved names in addition to enforcing the character pattern listed in the rule. [Native ingestion](../../../../src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs) (121, 353) · [ID generation](../../../../src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowConfigurationCapture.cs) (41–44) · [Generation qualifier](../../../../src/Hexalith.Memories.Server/Ingestion/UnitOwnershipAuthority.cs) (34–41) · [Workflow output](../../../../src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs) (822–836) · [Case and annotation issuance](../../../../src/Hexalith.Memories.Server/Cases/CaseService.cs) (86, 152) · [Tenant name guard](../../../../src/Hexalith.Memories.Server/Activities/Indexing/TenantIdGuard.cs) (22–42)

**Disposition.** Retain the ratified uppercase ULID target. Replace the contradictory present-tense statements and historical issuer inventory. Record current forms and the Story 34.36 cutover and pre-release re-ingestion boundary before applying strict validation. Existing integration assertions demonstrate prefixed IDs in supported producer paths. [Ownership assertions](../../../../tests/Hexalith.Memories.IntegrationTests/Ingestion/UnitOwnershipAuthorityIntegrationTests.cs) (208, 239, 353)

## Medium · Preflight release lacks the owner check required by the registry

The direct Redis exception at spine line 324 requires owner release. The associated ledger entry at line 492 asks for a separate key prefix. EventStore writes a common reservation marker, and scheduling-failure cleanup deletes by key. REST already uses a separate prefix, but its ordinary failure cleanup also deletes by key. REST compares the stored owner only in late canceled-write cleanup. A delayed release can remove a newer reservation after the previous lease expires. A distinct preflight namespace alone does not prevent that inconsistency. [EventStore reservation](../../../../src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs) (47–52, 76–84) · [Scheduling compensation](../../../../src/Hexalith.Memories.EventStore/EventIngestionService.cs) (201–209) · [REST reservation](../../../../src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs) (47–48, 125–139, 155–171) · [REST caller](../../../../src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs) (161)

**Disposition.** Extend the ledger to cover owner-conditional release on both paths. Require an ownership token and evidence that a stale release preserves the newer lease. Keep the separate key-family and durable idempotency obligations open.

## Medium · The Server liveness probe depends on the Dapr sidecar

The Health convention at spine line 312 and PRD FR72 define liveness as process viability. In the Server, the Dapr sidecar check carries both `live` and `ready` tags and an `Unhealthy` failure status. The `/alive` endpoint selects checks tagged `live` and maps an unhealthy result to HTTP 503. The deployed liveness probe calls that endpoint. The operator runbook describes the resulting restart after sustained sidecar loss. The register-readiness item at spine line 473 covers a different dependency. [FR72](../../prd.md) · [Server registrations](../../../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs) (178–183) · [Endpoint mapping](../../../../src/Hexalith.Memories.ServiceDefaults/Extensions.cs) (644–667) · [Kubernetes probe](../../../../deploy/kubernetes/base/server-deployment.yaml) (146–148) · [Operator runbook](../../../../docs/dev/health-checks.md) (220–225)

**Disposition.** Add the FR72 liveness mismatch to the alignment ledger. Require evidence that a responsive Server stays live through a sidecar outage while readiness reports failure.

## Checks that passed

- Evaluating the Server project with `dotnet msbuild -p:Configuration=Release -getProperty:TargetFramework,LangVersion,NETCoreSdkVersion,MemoriesEventStoreMinimumVersion,HexalithEventStoreVersion,HexalithAspireAppHostSdkVersion,HexalithAspireHostingDaprVersion -getItem:PackageVersion` confirmed SDK 11.0.100, net11.0, C# 14, EventStore 5.4.0, Aspire 17.0.0 and CommunityToolkit Dapr hosting beta.1078. The evaluated catalog supports the Dapr, Redis, FalkorDB, Kreuzberg, MCP, Fluent UI and OpenTelemetry package versions. The Builds catalog sets FrontComposer to 6.0.1 and self-hosted Dapr runtime to 1.19.2. Memories CI and nightly select runtime 1.19.1. [SDK selector](../../../../global.json) · [Project settings](../../../../Directory.Build.props) · [Consumer overrides](../../../../Directory.Packages.props) · [Working Builds catalog](../../../../references/Hexalith.Builds/Props/Directory.Packages.props) · [CI](../../../../.github/workflows/ci.yml) · [Nightly](../../../../.github/workflows/nightly.yml)
- The seven table digests match the owned ASP.NET image base, Dapr telemetry sidecar, Redis, FalkorDB, PostgreSQL, approved OpenBao server and smoke client. The OpenBao runbook records server 2.6.2, chart label 0.29.5, three Ready pods on one node and unmeasured voting membership on November 16. The architecture correctly limits the observation. [Container base](../../../../Directory.Build.targets) · [Kubernetes manifests](../../../../deploy/kubernetes/base) · [OpenBao inputs](../../../../deploy/openbao) · [Observed deployment](../../../../docs/operations/openbao.md)
- The superproject records EventStore `607072c68`. Comparing `git ls-tree HEAD` and `git submodule status` confirms Builds `2c33b2d7 → eb298a4d`, FrontComposer `aa016201 → 956353e1` and Tenants `8318f917 → 6627fc3d`. The spine identifies the working revisions used for its observations.

## Repeat validation · November 20, 2026

**Revisions still required.** The checked spine has SHA-256 `e97c37a46cc0d5c557621d4f6cc075a7f22a8ff1f3a752dd6118d10ff93a2c28` at repository HEAD `75c5b5f5`.

| Finding | Fresh check | Result |
| --- | --- | --- |
| H7 · Issued identifiers | Traced native file and URL submission through `NewUnitId()` to workflow output. New GUID strings can carry the ownership-generation prefix. The ULID case issuer and the integration assertions for prefixed unit IDs are both present. | High, open |
| M7 · Reservation release | Followed both ordinary scheduling-failure callbacks to key-only deletion. REST checks the stored owner in its separate late-cancellation cleanup. The EventStore reservation uses a common marker. | Medium, open |
| M8 · Process liveness | Followed the Server sidecar check’s live tag to the HTTP 503 mapping and Kubernetes liveness probe. A sustained sidecar failure can restart the Server. | Medium, open |

A fresh Release MSBuild evaluation reproduced the reported SDK, target, language, package floor, and catalog selections. A fresh comparison matched all seven complete SHA-256 digests to their manifests. The ten submodule worktrees have the same revisions as the initial validation. These checks covered source and configuration; they supply no additional product-gate execution evidence.
