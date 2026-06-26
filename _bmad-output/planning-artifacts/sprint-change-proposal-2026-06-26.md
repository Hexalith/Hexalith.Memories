---
project: Hexalith.Memories
date: 2026-06-26
workflow: bmad-correct-course
change_trigger: Initialize the local security service in the Memories Aspire host using HexalithEventStoreSecurityExtensions.
mode: Batch
status: implemented
approval_status: approved
approved_by: Jerome
approved_at: 2026-06-26T13:31:03+02:00
implemented_at: 2026-06-26T13:35:29+02:00
---

# Sprint Change Proposal - Memories Aspire Security Service Initialization

## 1. Issue Summary

The requested course correction is to use `HexalithEventStoreSecurityExtensions` to initialize the local security service in the Hexalith.Memories Aspire host.

The EventStore platform now exposes `HexalithEventStoreSecurityExtensions` from `Hexalith.EventStore.Aspire`. That helper adds a Keycloak-backed Aspire resource named `security` and provides reusable wiring methods for JWT bearer validation, EventStore client credentials, OpenID Connect, and plain security dependencies.

The Memories AppHost currently does not initialize that shared security resource. It only has DAPR API token propagation and an environment-variable pass-through for MCP JWT settings. The MCP host is already authenticated, but the Memories Server still has a documented deferred auth gap: `_bmad-output/implementation-artifacts/deferred-work.md` records `Story-9.3-MemoriesServerAuthN`, noting no `AddAuthentication`, `UseAuthentication`, or `RequireAuthorization` coverage on Server endpoints.

Evidence:

- `src/Hexalith.Memories.AppHost/Program.cs` wires Redis, FalkorDB, DAPR components, the Server, and MCP, but does not call `AddHexalithEventStoreSecurity()`.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` does not reference `Hexalith.EventStore.Aspire`.
- `src/Hexalith.Memories.Mcp/Program.cs` uses `UseAuthentication()`, `UseAuthorization()`, and `MapMcp("/mcp").RequireAuthorization()`.
- `src/Hexalith.Memories.Server/Program.cs` maps public API, controller, DAPR subscription, and health endpoints without auth middleware.
- `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` already provides `AddHexalithEventStoreSecurity`, `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, `WithOpenIdConnectSecurity`, and `WithSecurityDependency`.
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` already uses this helper successfully as the reference pattern.

## 2. Impact Analysis

Epic Impact:

- No PRD scope change is required.
- Epic 9 remains done for EventStore integration. This correction affects hosting/security composition, not the event-ingest contract itself.
- Epic 10 remains done for MCP authentication; the AppHost can now supply the local OIDC authority instead of relying only on dev symmetric-key configuration or ambient env vars.
- Epic 18 is already closed. If this correction is tracked as sprint work, add a new Engineering/Operational Readiness story rather than hiding it inside completed Story 18.8.
- The deferred `Story-9.3-MemoriesServerAuthN` should be promoted only if the intent is to secure all Memories Server endpoints, not merely initialize the local security service.

Story Impact:

- Add an AppHost-focused implementation task or story: initialize `security` in `Hexalith.Memories.AppHost`, wire MCP JWT settings through the shared helper, and add focused static/build coverage.
- Do not mark the Server auth gap closed unless Server middleware and endpoint authorization policies are implemented in the same approved change.
- If Server auth is included, the scope becomes moderate because every `/api/*`, `/events/ingest`, `/dapr/subscribe`, `/alive`, `/ready`, and `/health` route needs an explicit allow/deny decision.

Artifact Conflicts:

- PRD: no requirement conflict.
- Architecture: aligns with Aspire as the local orchestration owner and with NFR10/NFR11 security direction.
- UX: no UI/UX change.
- Docs/tests: update AppHost/security docs and tests so the Aspire resource name is `security`, not an implied ad hoc Keycloak resource.

Technical Impact:

- `Hexalith.Memories.AppHost` needs access to `Hexalith.EventStore.Aspire` as a hosting helper dependency with `IsAspireProjectResource="false"`.
- A Keycloak realm import must exist for Memories AppHost, or `AddHexalithEventStoreSecurity()` must be configured with a valid realm import path. Calling the helper without a realm import would leave the AppHost runtime incomplete.
- MCP audience handling must be explicit. Either reuse the EventStore platform audience (`hexalith-eventstore`) for the shared local realm or add a Memories-specific audience/client to the realm import and pass `audience: "hexalith-memories-mcp"` to `WithJwtBearerSecurity`.
- Server can depend on `security` for startup ordering now, but JWT bearer enforcement should remain a separate approved Server auth story unless explicitly included.

## 3. Recommended Approach

Use Direct Adjustment with a guardrail.

Implement the AppHost security service initialization now, but do not claim full Memories Server authentication is complete unless the deferred Server auth story is also approved and implemented.

Effort estimate: low for AppHost/MCP wiring; medium if Server endpoint authorization is included.

Risk level: low for AppHost resource initialization; medium for full Server auth because DAPR pub/sub delivery and health probes need precise anonymous/authenticated route policy.

## 4. Detailed Change Proposals

### AppHost Project Reference

File: `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`

OLD:

```xml
<ItemGroup>
  <ProjectReference Include="..\Hexalith.Memories.Mcp\Hexalith.Memories.Mcp.csproj" />
  <ProjectReference Include="..\Hexalith.Memories.Server\Hexalith.Memories.Server.csproj" />
</ItemGroup>
```

NEW:

```xml
<ItemGroup>
  <ProjectReference Include="..\Hexalith.Memories.Mcp\Hexalith.Memories.Mcp.csproj" />
  <ProjectReference Include="..\Hexalith.Memories.Server\Hexalith.Memories.Server.csproj" />
  <ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" IsAspireProjectResource="false" />
</ItemGroup>
```

Rationale: consume the EventStore Aspire helper library as hosting glue, not as an Aspire project resource.

### EventStore Root Resolution

File: `Directory.Build.props`

ADD:

```xml
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)Hexalith.EventStore\src\Hexalith.EventStore.Aspire')">$(MSBuildThisFileDirectory)Hexalith.EventStore</HexalithEventStoreRoot>
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)..\Hexalith.EventStore\src\Hexalith.EventStore.Aspire')">$(MSBuildThisFileDirectory)..\Hexalith.EventStore</HexalithEventStoreRoot>
```

Rationale: avoid hardcoding the submodule path in multiple project files and mirror existing root-resolution patterns.

### AppHost Security Resource Initialization

File: `src/Hexalith.Memories.AppHost/Program.cs`

OLD:

```csharp
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
string secretsFile = EnsureSecretsFile();
```

NEW:

```csharp
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
string secretsFile = EnsureSecretsFile();
```

Required using:

```csharp
using Hexalith.EventStore.Aspire;
```

Rationale: initialize the shared local security service through the platform helper so the Aspire dashboard and downstream AppHost pattern use the same `security` resource.

### MCP Authentication Wiring

File: `src/Hexalith.Memories.AppHost/Program.cs`

OLD:

```csharp
mcp = PropagateJwtBearerAuthenticationEnvironment(mcp);
```

NEW:

```csharp
if (security is not null)
{
    mcp = mcp.WithJwtBearerSecurity(security);
}
else
{
    mcp = PropagateJwtBearerAuthenticationEnvironment(mcp);
}
```

Rationale: when the local security service is enabled, use the shared OIDC authority/issuer/audience wiring. When `EnableKeycloak=false`, keep the existing env-var fallback path.

Approval note: if the desired MCP audience must remain `hexalith-memories-mcp`, add that audience/client to the realm import and pass the override explicitly. Do not wire an audience value the realm cannot issue.

### Server Security Dependency

File: `src/Hexalith.Memories.AppHost/Program.cs`

ADD:

```csharp
if (security is not null)
{
    server = server.WithSecurityDependency(security);
}
```

Rationale: makes Server startup wait for the shared security service without falsely claiming Server endpoints enforce JWT authorization.

### Realm Import

Target: `src/Hexalith.Memories.AppHost/KeycloakRealms/hexalith-realm.json`

ADD a Memories-owned realm import, or configure `HexalithEventStoreSecurityOptions.RealmImportPath` to a valid existing realm import.

Minimum content decision:

- If sharing the EventStore platform audience, keep `hexalith-eventstore`.
- If preserving a Memories-specific MCP audience, add a `hexalith-memories-mcp` audience/client and ensure test tokens include the tenant claim expected by `MemoriesMcpClaimsTransformation`.

Rationale: `AddHexalithEventStoreSecurity()` defaults to `./KeycloakRealms`; Memories currently has no such AppHost directory.

### Tests and Docs

ADD focused tests/docs:

- Static AppHost test asserting `AddHexalithEventStoreSecurity(` is present and `Hexalith.EventStore.Aspire` is referenced with `IsAspireProjectResource="false"`.
- Static or build-level test asserting MCP is wired through `WithJwtBearerSecurity(security)` when Keycloak is enabled.
- Documentation update in `docs/dev/mcp-server.md` or `docs/operations/deployment-configuration.md` stating the local Aspire identity resource is `security` and `EnableKeycloak=false` keeps the no-Keycloak fallback.

## 5. Change Analysis Checklist

| Item | Status | Finding |
|---|---:|---|
| 1.1 Triggering story | [N/A] | Direct user-triggered hosting/security correction, not a user-facing story failure. |
| 1.2 Core problem | [x] | Memories AppHost does not initialize the shared EventStore Aspire security service. |
| 1.3 Evidence | [x] | Current AppHost, MCP, Server, EventStore helper, Tenants reference pattern, and deferred Server auth gap inspected. |
| 2.1 Current epic impact | [x] | Epic 9 and 10 remain done; this is hosting/security composition. |
| 2.2 Epic-level changes | [!] | If tracked as sprint backlog, create a new operational/security story; do not hide it in closed Epic 18. |
| 2.3 Remaining epics | [x] | No UX or search/ingestion epic changes required. |
| 2.4 Future epic invalidation | [N/A] | None. |
| 2.5 Epic order/priority | [x] | AppHost security init can precede full Server auth; full Server auth should be separate if approved. |
| 3.1 PRD conflicts | [x] | No product scope conflict. |
| 3.2 Architecture conflicts | [x] | Aligns with Aspire-owned local orchestration and platform helper reuse. |
| 3.3 UI/UX conflicts | [N/A] | No UI work. |
| 3.4 Secondary artifacts | [x] | AppHost project reference, realm import, docs, and static tests affected. |
| 4.1 Direct adjustment | [x] | Viable and recommended for AppHost/MCP wiring. |
| 4.2 Potential rollback | [N/A] | No rollback useful. |
| 4.3 MVP review | [N/A] | MVP/product scope unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment with Server-auth guardrail. |
| 5.1 Issue summary | [x] | Included. |
| 5.2 Epic/artifact needs | [x] | Included. |
| 5.3 Recommendation | [x] | Included. |
| 5.4 MVP impact/action plan | [x] | No MVP impact; action plan scoped. |
| 5.5 Handoff plan | [x] | Developer agent can implement after approval. |
| 6.1 Proposal completion | [x] | Complete for batch review. |
| 6.2 Accuracy check | [x] | Consistent with current source and planning artifacts. |
| 6.3 User approval | [x] | Approved by Jerome on 2026-06-26T13:31:03+02:00. |
| 6.4 Sprint status update | [N/A] | No status update until approved and a tracking story/task is chosen. |
| 6.5 Handoff | [x] | Defined below. |

## 6. Implementation Handoff

Scope classification: Minor if limited to AppHost/MCP security service initialization; Moderate if Server endpoint authorization is included.

Route to: Developer agent for direct implementation.

Developer responsibilities:

- Add the EventStore Aspire helper reference without adding it as an Aspire project resource.
- Initialize `security` via `builder.AddHexalithEventStoreSecurity()`.
- Add or configure a valid realm import.
- Wire MCP through `WithJwtBearerSecurity(security)` when security is enabled, preserving `EnableKeycloak=false` fallback.
- Add `WithSecurityDependency(security)` to Server only as a startup dependency unless full Server auth is approved.
- Add focused static/build tests and docs.

Success criteria:

- `dotnet build` for the impacted AppHost/test slice succeeds with warnings as errors.
- Aspire AppHost shows a `security` resource when Keycloak is enabled.
- MCP receives OIDC JWT bearer config from the AppHost in security-enabled local runs.
- Existing env-var symmetric-key fallback still works when `EnableKeycloak=false`.
- Server auth gap remains explicitly tracked unless implemented.

## 7. Approval

Approved scope: minor AppHost/MCP security initialization, with full Memories Server auth kept as a separate follow-up unless explicitly approved later.

Implementation may proceed under this approved scope.

## 8. Implementation Summary

Implemented under the approved minor scope:

- AppHost now references `Hexalith.EventStore.Aspire` as hosting glue only and initializes `security` with `AddHexalithEventStoreSecurity()`.
- MCP receives JWT bearer settings through `WithJwtBearerSecurity(security)` when Keycloak-backed local security is enabled.
- Server waits on `security` with `WithSecurityDependency(security)` but does not claim endpoint authorization.
- A Memories-owned Keycloak realm import is present under the AppHost.
- Aspire integration fixture sets `EnableKeycloak=false` to preserve existing Docker-backed test topology cost unless a test explicitly opts into Keycloak.
- Docs and drift-guard tests cover the local `security` resource and fallback behavior.
