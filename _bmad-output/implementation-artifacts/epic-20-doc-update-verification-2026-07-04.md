# Epic 20 Documentation Update Verification

Project: Hexalith.Memories
Date: 2026-07-04
Mode: Autonomous post-retrospective documentation verification

## Verification Method

For each candidate document, current documentation was read, compared against the implemented Epic 20 code and story evidence, and either updated or discarded as already accurate / out of scope.

Code and artifact anchors checked:

- Server auth, tenant authorization, and rate limiting: `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/RateLimiting/*`.
- MCP upstream app-id and auth validation: `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`, `src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs`, `src/Hexalith.Memories.Mcp/README.md`.
- Safe ingestion status projection: `src/Hexalith.Memories.Contracts/V1/IngestionWorkflowStatus.cs`, `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowStatusMapper.cs`.
- Audit operation taxonomy and metrics: `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs`, `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs`, `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`.
- RediSearch escaping: `src/Hexalith.Memories.Server/Search/RediSearchQueryEscaper.cs`, `src/Hexalith.Memories.Server/Search/RediSearchErrorClassifier.cs`.
- Deferred-work state: `_bmad-output/implementation-artifacts/deferred-work.md`.

## Updated Documents

| Document | Verified discrepancy | Update applied |
|---|---|---|
| `_bmad-output/planning-artifacts/architecture.md` | Still described `TenantAuthorizationMiddleware` as deferred Phase 1.5 work and REST auth as direct tenant IDs. Code now has Server bearer auth, tenant authorization middleware/filters, and inbound request limiting. | Updated security architecture, growth component table, D8 decision rows, and REST auth boundary. |
| `docs/operations/deployment-configuration.md` | Still said local `security` initialized MCP auth only and Server authorization remained deferred. AppHost now propagates JWT bearer settings to both Server and MCP when Keycloak security is enabled. | Updated required runtime environment text. |
| `docs/dev/mcp-server.md` | Still used the stale upstream app-id `memories-server` and described Server-level auth as a future gated event. MCP code defaults to `memories`, supports `MEMORIES_SERVER_APP_ID`, and Server `/api/**` has bearer auth/tenant authorization. | Updated topology, server ingress inventory, Dapr invocation text, and local-dev service names. |
| `docs/operations/route-surface.md` | Route list did not describe the new `/api/**` auth posture, safe single-workflow status DTO, or batch tenant authorization before fan-out. | Updated review date, Dapr ACL framing, and ingestion status route purpose text. |
| `docs/operations/rate-limiting.md` | Document covered provider/extraction throttling only. Code now adds inbound HTTP request quotas under `InboundRateLimiting` and metric `memories.rate_limit.rejections`. It also referenced a stale embedding-config route. | Added inbound HTTP quota section, config defaults, rejection behavior, metric, and corrected embedding-config route. |
| `docs/dev/telemetry.md` | Audit schema still listed only five operation families, old event-id ranges, and spoofable/header-based user resolution. Code now has nine audited families, event IDs 7501-7509/7511-7519, and principal-derived audit identity. | Updated audit schema, event bank, operation type list, identity-resolution policy, and consistency cross-reference. |
| `docs/dev/eventstore-integration.md` | Handler endpoints section still said they shipped unauthenticated and auth was deferred. Code now protects `/api/**`; Dapr pub/sub routes remain explicit anonymous infrastructure exceptions. | Updated authentication/authorization section. |
| `docs/dev/consistency.md` | Access telemetry section still referenced the old four audited operation types. | Updated audited operation list while preserving the consistency-endpoint exclusion. |

## Discarded Updates

| Document | Candidate concern | Decision |
|---|---|---|
| `src/Hexalith.Memories.Mcp/README.md` | MCP production signing-key docs might be stale after 20.4. | Discarded. Current README already states production rejects `SigningKey`, requires HTTPS metadata, and checks tenant claims. |
| `docs/dev/export.md` | Export endpoints might need audit-scope updates after 20.5. | Discarded. Story 20.5 did not add export audit events; the document remains accurate that export is outside `AccessTelemetryEvent` scope. |
| `docs/dev/quickstart.md` | Quickstart audit caveat might be stale after principal-derived audit identity. | Discarded for this pass. The quickstart warning is about wizard invocation audit posture, not the Epic 20 Server audit identity implementation. |
| `docs/operations/embedding-providers.md` | Provider throttling docs might need update after inbound rate limiting. | Discarded. Epic 20 added inbound request limiting, not provider throttle semantics; `docs/operations/rate-limiting.md` is the correct operator doc for the combined view. |

## Validation

- YAML parse of `_bmad-output/implementation-artifacts/sprint-status.yaml` confirmed `epic-20: done`, `epic-20-retrospective: done`, and five Epic 20 action items.
- Stale-phrase scan found no remaining Epic 20-auth drift strings in the updated docs.
- `git diff --check` passed for the retrospective, sprint status, and updated documentation.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Deployment.DeploymentConfigurationContractTests -class Hexalith.Memories.Server.Tests.Deployment.RouteSurfaceContractTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.DocumentationCompletenessTests -class Hexalith.Memories.Server.Tests.Telemetry.InstrumentationInventoryTests` passed: 18 total, 0 failed.
