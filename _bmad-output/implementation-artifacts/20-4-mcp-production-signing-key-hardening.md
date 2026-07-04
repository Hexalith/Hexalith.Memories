---
baseline_commit: ef57bd560cf34a2440d466b24a7c7ada87cf62e1
---

# Story 20.4: MCP Production Signing-Key Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want the MCP server to refuse a development symmetric signing key in production,
so that the corpus cannot be reached with a static shared secret.

## Acceptance Criteria

1. Given `ValidateMcpAuthenticationOptions` validates MCP JWT bearer configuration at startup, when `Authentication:JwtBearer:SigningKey` is configured and the host environment is `Production`, then startup validation fails with a clear sanitized message that identifies the forbidden production signing-key mode and directs operators to use OIDC `Authority` metadata instead.

2. Given the development/test symmetric key mode is still needed for local and test hosts, when `SigningKey` is configured outside `Production`, then the existing validation rules still apply: issuer, audience, tenant claim, valid algorithms, and at least 32 effective bytes of key material are required, and `appsettings.Development.json` remains valid.

3. Given OIDC `Authority` mode is configured, when the host environment is `Production` and `RequireHttpsMetadata` is `false`, then startup validation fails with a clear message because production metadata discovery must use HTTPS.

4. Given OIDC `Authority` mode is configured with `RequireHttpsMetadata=true`, when `ConfigureJwtBearerOptions` applies the bearer options, then `JwtBearerOptions.Authority`, strict token-validation parameters, and `RequireHttpsMetadata=true` are preserved.

5. Given MCP authentication failures and challenges are security-sensitive, when this story is implemented, then no validation failure, log assertion, challenge body, test display name, or documentation example prints the configured signing key or raw token material.

6. Given Epic 20 closes audit finding A20, when implementation finishes, then focused tests prove production signing-key rejection, non-production signing-key acceptance, production authority+HTTP-metadata rejection, authority+HTTPS success, startup validation wiring, and no regression in existing MCP authorization/authentication tests.

## Tasks / Subtasks

- [x] Task 1 - Re-run the audit-anchor preflight before editing (AC: 1, 3, 6)
  - [x] Confirm `src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs` still validates only presence, issuer, audience, signing-key length, tenant claim, and algorithms, with no `IHostEnvironment`/production guard.
  - [x] Confirm `src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs` still applies `authConfig.RequireHttpsMetadata` directly when `Authority` is set.
  - [x] Confirm `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs` still binds `Authentication:JwtBearer`, uses `.ValidateOnStart()`, registers `ValidateMcpAuthenticationOptions`, and forces startup validation through `StartupValidationHostedService`.
  - [x] Confirm `src/Hexalith.Memories.Mcp/appsettings.Development.json` is the only MCP file carrying the development signing-key default and `RequireHttpsMetadata=false`.
  - [x] Record the preflight result in the Dev Agent Record with current commit, moved anchors, and any implementation adaptation.

- [x] Task 2 - Add environment-aware MCP options validation (AC: 1, 2, 3, 5)
  - [x] Inject `IHostEnvironment` into `ValidateMcpAuthenticationOptions` and update DI registration through constructor injection; avoid service locators or static environment reads.
  - [x] Add a `Production` guard that fails when `SigningKey` is nonblank, even when an `Authority` is also configured. The failure message must not include the key value.
  - [x] Add a `Production` guard that fails when `Authority` is nonblank and `RequireHttpsMetadata` is `false`.
  - [x] Preserve all existing validation checks and effective key-length calculation for non-production signing-key mode.
  - [x] Keep `Development` local/test signing-key flows valid; do not remove `appsettings.Development.json` signing-key mode.

- [x] Task 3 - Keep JWT bearer configuration strict and explicit (AC: 4, 5)
  - [x] Preserve `MapInboundClaims=false`, issuer/audience/signing-key/lifetime validation, expiration requirements, signed-token requirement, one-minute clock skew, configured issuer/audience, and configured algorithm allowlist.
  - [x] Preserve `Authority` mode setting `JwtBearerOptions.Authority`.
  - [x] Ensure production `Authority` mode cannot result in `JwtBearerOptions.RequireHttpsMetadata=false` after validation.
  - [x] Do not add a second auth scheme, a fallback anonymous MCP mode, token minting helpers, or any logging that emits raw JWTs or signing-key values.

- [x] Task 4 - Add focused MCP tests (AC: 1-6)
  - [x] Update `tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs` to construct the validator with a fake/substituted `IHostEnvironment`.
  - [x] Add `ProductionWithSigningKey_Fails` and assert the message contains `Production` and `SigningKey` but not the actual key.
  - [x] Add `DevelopmentWithSigningKey_Succeeds` or preserve the existing symmetric-development success test under an explicit `Development` environment.
  - [x] Add `ProductionWithAuthorityAndRequireHttpsMetadataFalse_Fails`.
  - [x] Add `ProductionWithAuthorityAndRequireHttpsMetadataTrue_Succeeds`.
  - [x] Keep `ConfigureJwtBearerOptionsTests.Configure_OidcMode_SetsAuthorityAndHttpsMetadata` passing and add a negative/guard assertion only if needed to prove the configured options cannot bypass validation.
  - [x] Add or update a composition/startup validation test proving `McpCompositionRoot` still wires the validator so invalid production MCP auth options fail during startup, not after the first request.

- [x] Task 5 - Update operator-facing MCP security docs only if stale (AC: 1, 2, 3, 5)
  - [x] Review `docs/dev/mcp-server.md` and `src/Hexalith.Memories.Mcp/README.md` for signing-key and `RequireHttpsMetadata` wording.
  - [x] If they do not already state the production rule, update them to say `SigningKey` is development/test only, production must use OIDC `Authority`, and production OIDC metadata requires HTTPS.
  - [x] Keep docs free of real secrets and do not add production examples with symmetric keys.

- [x] Task 6 - Validate and document completion (AC: 1-6)
  - [x] Run focused MCP tests:

    ```bash
    DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --disable-build-servers -m:1 /nr:false
    DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.MemoriesMcpAuthenticationOptionsTests -class Hexalith.Memories.Mcp.Tests.ConfigureJwtBearerOptionsTests
    ```

  - [x] Run the MCP auth/authorization regression set:

    ```bash
    DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests -class Hexalith.Memories.Mcp.Tests.TenantClaimAuthorizationTests
    ```

  - [x] Run `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` or document the exact blocker.
  - [x] Run `git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md`.
  - [x] Update this story's Dev Agent Record with commands, outcomes, changed files, and any validation blockers.

## Dev Notes

This story is implementation scope and closes audit finding A20. It is intentionally narrow: harden the MCP JWT bearer configuration so production cannot run with a static HS256 development signing key and cannot disable HTTPS metadata discovery for production OIDC authority mode. It must not reopen Server authentication, tenant claim normalization, workflow status projection, inbound rate limiting, RediSearch escaping, MCP tool executor extraction, or Program.cs decomposition. Those are Stories 20.1, 20.2, 20.3, 20.5, 20.6, 25.6, and 25.1 respectively. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.4; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A20]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Phase: Post-MVP Audit Remediation and Story 20.4 under Epic 20 API Security & Tenant Authorization.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are MCP as a first-class agent surface, authenticated ingress, Dapr sidecar boundaries, and strict tenant/security handling.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR54 MCP tools, FR58 typed MCP schemas, NFR11 external ingress authentication, and NFR20 MCP protocol conformance.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI implementation is in scope, but MCP responses and failures must remain schema-first, bounded, and safe for agents.
- Loaded persistent facts from `_bmad-output/project-context.md`.
- Loaded previous story `_bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md`.

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against current `HEAD` `ef57bd5` plus the dirty working tree:

- The audit finding A20 is still present. `ValidateMcpAuthenticationOptions` validates missing Authority/SigningKey, issuer, audience, weak signing key, tenant claim, and algorithm list, but it does not know the host environment and has no production guard. [Source: src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs:13-52]
- `ConfigureJwtBearerOptions` still sets strict token-validation parameters, then in `Authority` mode assigns `options.Authority = authConfig.Authority` and `options.RequireHttpsMetadata = authConfig.RequireHttpsMetadata`; a production configuration with `Authority` and `RequireHttpsMetadata=false` is not rejected by this class. [Source: src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs:31-55]
- Symmetric-key mode still creates a `SymmetricSecurityKey` from `authConfig.SigningKey` when no Authority is configured. [Source: src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs:52-55]
- `McpCompositionRoot` still uses `.BindConfiguration("Authentication:JwtBearer").ValidateOnStart()`, registers `ValidateMcpAuthenticationOptions`, and adds `StartupValidationHostedService`, so a validator failure is the right startup-time failure mechanism. [Source: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:40-48]
- `appsettings.Development.json` intentionally configures a development signing key and `RequireHttpsMetadata=false`; this story must preserve that outside Production. [Source: src/Hexalith.Memories.Mcp/appsettings.Development.json:8-14]
- Existing MCP tests cover missing auth config, weak signing keys, empty algorithms, non-production signing-key success, and OIDC options with HTTPS metadata, but they do not cover `Production`. [Source: tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs:16-95; tests/Hexalith.Memories.Mcp.Tests/ConfigureJwtBearerOptionsTests.cs:19-67]

If any anchor moves before dev starts, update this section first. Epics 20-26 require current-code re-verification before implementation. [Source: _bmad-output/planning-artifacts/epics.md#Phase-Post-MVP-Audit-Remediation]

### Existing Patterns to Reuse

- Reuse the existing options-validation path: `IValidateOptions<MemoriesMcpAuthenticationOptions>`, `.ValidateOnStart()`, and `StartupValidationHostedService`. Do not add request-time checks for a startup-only configuration invariant. [Source: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:40-48; src/Hexalith.Memories.Mcp/Hosting/StartupValidationHostedService.cs]
- Use `IHostEnvironment.IsProduction()` the same way existing production guards do. `NaturalLanguageDescriptionOptionsValidator` is the local pattern for injecting `IHostEnvironment`, collecting validation failures, and returning `ValidateOptionsResult.Fail(...)` with sanitized messages. [Source: src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs:38-86]
- Use xUnit v3, Shouldly, and NSubstitute/Fake environment helpers consistently with existing validator tests. [Source: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/NaturalLanguageDescriptionOptionsValidatorTests.cs:24-54; _bmad-output/project-context.md#Testing-Rules]
- Keep package references versionless. `Microsoft.AspNetCore.Authentication.JwtBearer` is already centrally pinned to `10.0.9`; no new dependency should be needed. [Source: references/Hexalith.Builds/Props/Directory.Packages.props:150; src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj:33]

### Architecture and Security Constraints

- MCP is an agent-facing ingress surface. It must preserve token-budget-aware responses, tenant authorization filtering, structured errors, and evidence packet mapping. This story changes startup auth configuration only; it should not alter tool contracts or downstream MemoriesClient behavior. [Source: _bmad-output/project-context.md#Framework-Specific-Rules]
- Never expose secrets. Validation messages and tests must not print the actual signing key, raw JWTs, bearer tokens, Dapr API tokens, or provider credentials. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Keep `MapInboundClaims=false` because tenant claim normalization and `TenantClaimAuthorizationFilter` depend on stable raw claim names. [Source: src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs:31-33; src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpAuthenticationOptions.cs:26-37]
- Production `SigningKey` must fail even if `Authority` is also set. A static shared secret in production is the risk; do not allow it to remain as a confusing fallback or accidental token-minting path.
- Production `Authority` with `RequireHttpsMetadata=false` must fail at startup. Local development may still use non-production signing-key mode and may keep `RequireHttpsMetadata=false` only outside Production.
- Official ASP.NET Core guidance supports keeping options validation in `IValidateOptions<TOptions>`, and the `JwtBearerOptions.RequireHttpsMetadata` property is intended to require HTTPS for metadata/authority and should only be disabled in development. [Source: Microsoft Learn, Options pattern in ASP.NET Core, `IValidateOptions<TOptions>` section; local NuGet XML `/home/administrator/.nuget/packages/microsoft.aspnetcore.authentication.jwtbearer/10.0.9/lib/net10.0/Microsoft.AspNetCore.Authentication.JwtBearer.xml`:193-197]

### File Structure Guidance

Expected production files:

- `src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs` (update; add `IHostEnvironment` dependency and production guards)
- `src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs` (update only if needed; expected to preserve existing strict configuration)
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs` (update only if constructor injection/registration needs adjustment; likely no change beyond DI resolving `IHostEnvironment`)
- `docs/dev/mcp-server.md` and/or `src/Hexalith.Memories.Mcp/README.md` (update only if production signing-key/HTTPS metadata wording is stale)

Expected test files:

- `tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs` (update)
- `tests/Hexalith.Memories.Mcp.Tests/ConfigureJwtBearerOptionsTests.cs` (update only if needed)
- A focused startup/composition test under `tests/Hexalith.Memories.Mcp.Tests/` if no existing test can prove startup validation wiring.

No contract, Server, Client.Rest, CLI, Redis, Web, AppHost, deployment, or Dapr component change should be needed. If implementation discovers AppHost security propagation can still inject a production `SigningKey`, document the finding and keep the code change scoped to MCP startup rejection unless the minimal fix is required to make the guard testable.

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test names should be behavior-focused PascalCase. [Source: _bmad-output/project-context.md#Testing-Rules]

Minimum focused validation:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --disable-build-servers -m:1 /nr:false
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.MemoriesMcpAuthenticationOptionsTests -class Hexalith.Memories.Mcp.Tests.ConfigureJwtBearerOptionsTests
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests -class Hexalith.Memories.Mcp.Tests.TenantClaimAuthorizationTests
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false
git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md
```

If `dotnet test` hits the known sandbox/VSTest TCP listener limitation, use the built xUnit v3 executable pattern above and record the exact fallback command. [Source: _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md#Testing-Standards]

### Scope Boundaries

- Do not change `Hexalith.Memories.Server` auth validators in this story unless a compile-time sharing requirement makes a tiny shared helper unavoidable. Server production signing-key policy, if desired, needs its own scoped story or explicit approval.
- Do not introduce Keycloak/OIDC provisioning changes, issuer/audience changes, token minting helpers, dynamic signing-key rotation, JWKS caching changes, or a new auth scheme.
- Do not make MCP anonymous in Development and do not weaken tenant authorization filters.
- Do not alter MCP tool schemas, tool executor structure, token-budget response shaping, `MemoriesClient` calls, Dapr sidecar app IDs, or upstream server invocation.
- Do not initialize or update nested submodules.

### Previous Story Intelligence

Story 20.3 completed tenant-scoped workflow and batch status endpoints on the Server side. Carry these learnings into 20.4:

- Keep Epic 20 changes tightly scoped. The prior story explicitly avoided reopening MCP signing-key hardening, rate limiting, RediSearch escaping, and Program.cs decomposition; this story should likewise avoid adjacent security work unless directly required by A20.
- Prefer startup-time or pre-dependency denial for security invariants. Story 20.3 denied cross-tenant batch access before workflow fan-out; this story should deny unsafe production auth configuration before the MCP process serves requests.
- Continue the Epic 20 testing pattern: focused unit/configuration tests plus a small regression set for auth route/tool behavior, with `dotnet exec` fallback when VSTest is blocked by sandbox TCP-listener restrictions.
- Keep failure output sanitized. Story 20.3 added structured safe status responses and avoided raw state leakage; this story must avoid raw signing-key/token leakage in validation messages and logs.

[Source: _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md#Completion-Notes-List; git commit `ef57bd5`]

### Git Intelligence

Recent commits show Epic 20 is in active security remediation:

- `ef57bd5 feat(story-20.3): Tenant-Scope Workflow & Batch Status Endpoints` added safe status projection, tenant-first status authorization, and endpoint tests.
- `ae9558f feat(story-20.2): Tenant Authorization Filter & Principal-Derived Audit Identity` added normalized tenant claims, endpoint filters/middleware, and cross-tenant denial tests.
- `b48a519 feat(story-20.1): Server Authentication Foundation` added bearer authentication, fallback authorization, anonymous route guardrails, and Server auth tests.

### Project Structure Notes

This story should touch MCP authentication validation and focused MCP tests, with optional docs if current wording is stale. It should not alter Server endpoints, tenant authorization middleware, Contracts, CLI formatting, Redis storage, ingestion workflow state, Web UI, or deployment topology.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.4 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A20 - MCP HS256 production guard gap]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-20 - approved remediation scope]
- [Source: _bmad-output/planning-artifacts/prd.md#FR54-FR58-NFR11-NFR20 - MCP and ingress-auth requirements]
- [Source: _bmad-output/project-context.md - C#, testing, package, MCP, and secrets rules]
- [Source: _bmad-output/implementation-artifacts/20-3-tenant-scope-workflow-and-batch-status-endpoints.md - previous story implementation and validation learnings]
- [Source: src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs:13-52 - current validator lacks production guard]
- [Source: src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs:31-55 - current bearer option configuration]
- [Source: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:40-48 - startup validation registration]
- [Source: src/Hexalith.Memories.Mcp/appsettings.Development.json:8-14 - development signing-key mode]
- [Source: tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs:16-95 - existing validator tests]
- [Source: tests/Hexalith.Memories.Mcp.Tests/ConfigureJwtBearerOptionsTests.cs:19-67 - existing JWT bearer options tests]
- [Source: /home/administrator/.nuget/packages/microsoft.aspnetcore.authentication.jwtbearer/10.0.9/lib/net10.0/Microsoft.AspNetCore.Authentication.JwtBearer.xml:193-197 - `RequireHttpsMetadata` development-only guidance]
- [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0#validate-options-in-a-dedicated-class-with-ivalidateoptionstoptions - options validation pattern]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: Dev-story activation complete. Resolved workflow customization with no activation steps, loaded `_bmad-output/project-context.md`, and confirmed sprint status story key `20-4-mcp-production-signing-key-hardening` was `ready-for-dev` before moving it to `in-progress`.
- 2026-07-04: Audit-anchor preflight re-run at `HEAD` `ef57bd560cf34a2440d466b24a7c7ada87cf62e1` with dirty working tree limited to existing sprint/story-automator/story artifacts before implementation. Anchors still match: `ValidateMcpAuthenticationOptions` has no `IHostEnvironment`/production guard; `ConfigureJwtBearerOptions` passes configured `RequireHttpsMetadata` through in authority mode; `McpCompositionRoot` binds `Authentication:JwtBearer`, uses `.ValidateOnStart()`, registers `ValidateMcpAuthenticationOptions`, and adds `StartupValidationHostedService`; `appsettings.Development.json` is the only MCP file with a development signing-key default and `RequireHttpsMetadata=false`. No implementation adaptation required.
- 2026-07-04: Red phase confirmed with `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --disable-build-servers -m:1 /nr:false`; expected failure `CS1729` because `ValidateMcpAuthenticationOptions` did not yet accept `IHostEnvironment`.
- 2026-07-04: Focused MCP test project build passed after implementation with 0 warnings and 0 errors.
- 2026-07-04: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.MemoriesMcpAuthenticationOptionsTests -class Hexalith.Memories.Mcp.Tests.ConfigureJwtBearerOptionsTests` passed: 10 total, 0 failed.
- 2026-07-04: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.McpCompositionRootTests` passed: 3 total, 0 failed. The expected startup validation failure log did not include the configured signing-key value.
- 2026-07-04: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests -class Hexalith.Memories.Mcp.Tests.TenantClaimAuthorizationTests` passed: 10 total, 0 failed.
- 2026-07-04: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-07-04: `git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added host-environment-aware MCP JWT bearer options validation so production rejects any configured `SigningKey` and rejects authority mode when `RequireHttpsMetadata=false`; non-production signing-key validation remains unchanged.
- Preserved existing JWT bearer configuration strictness and authority option behavior; no new auth schemes, fallback anonymous mode, token minting helpers, or secret/token logging were added.
- Added focused validator and startup wiring tests for production signing-key rejection, development signing-key acceptance, production HTTP metadata rejection, production HTTPS authority success, and startup-time validation.
- Updated MCP operator docs to state that symmetric signing keys are development/test-only and production authority metadata must use HTTPS.

### File List

- `_bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `docs/dev/mcp-server.md`
- `src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs`
- `src/Hexalith.Memories.Mcp/README.md`
- `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointChallengeBodyTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpCompositionRootTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs`

### Senior Developer Review (AI)

Reviewer: Codex GPT-5 on 2026-07-04

Outcome: Approved after automatic review fixes.

#### Findings Fixed

- [x] [AI-Review][Medium] Story File List omitted the new MCP challenge-body test file changed by the implementation. Added `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointChallengeBodyTests.cs`.
- [x] [AI-Review][Low] Story File List omitted the generated Story 20.4 test summary artifact. Added `_bmad-output/implementation-artifacts/tests/test-summary.md`.

#### Acceptance Criteria Verification

- AC1: Implemented. `ValidateMcpAuthenticationOptions` injects `IHostEnvironment`, rejects production `SigningKey`, and returns a sanitized message that names `Production` and `SigningKey` without echoing the configured key.
- AC2: Implemented. Non-production signing-key validation still enforces issuer, audience, tenant claim, algorithm list, and at least 32 effective key bytes.
- AC3: Implemented. Production authority mode with `RequireHttpsMetadata=false` fails startup validation.
- AC4: Implemented. `ConfigureJwtBearerOptions` preserves authority mode, strict token-validation parameters, and `RequireHttpsMetadata=true`.
- AC5: Implemented. Validation messages, challenge bodies, and focused assertions avoid raw signing-key and bearer-token material.
- AC6: Implemented. Focused validator/startup/configuration tests and MCP auth regression tests pass.

#### Review Validation

- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.MemoriesMcpAuthenticationOptionsTests -class Hexalith.Memories.Mcp.Tests.ConfigureJwtBearerOptionsTests` passed: 10 total, 0 failed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.McpCompositionRootTests` passed: 3 total, 0 failed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointChallengeBodyTests -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests` passed: 6 total, 0 failed.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Debug/net10.0/Hexalith.Memories.Mcp.Tests.dll -class Hexalith.Memories.Mcp.Tests.Authentication.McpEndpointAllowAnonymousPathsTests -class Hexalith.Memories.Mcp.Tests.TenantClaimAuthorizationTests` passed: 10 total, 0 failed.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- `git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary.md` passed.

### Change Log

- 2026-07-04: Implemented MCP production signing-key hardening and production HTTPS metadata validation; added focused validator/startup tests and updated MCP security docs.
- 2026-07-04: Senior developer review completed with automatic story-record fixes; story marked done.
