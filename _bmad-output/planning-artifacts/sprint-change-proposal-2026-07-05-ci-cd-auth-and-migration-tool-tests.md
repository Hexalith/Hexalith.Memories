# Sprint Change Proposal — CI/CD Recovery: Migration-Tool Test Path + Integration-Test Authentication

- **Date:** 2026-07-05
- **Author:** Jerome (via Correct-Course workflow)
- **Trigger:** Red CI/CD on `main` — GitHub Actions runs
  [28740341552 (CI)](https://github.com/Hexalith/Hexalith.Memories/actions/runs/28740341552)
  and [28740341543 (Release)](https://github.com/Hexalith/Hexalith.Memories/actions/runs/28740341543)
- **Change scope:** Moderate (test-harness) + Moderate (MCP service-to-service auth)
- **Mode:** Incremental

---

## Section 1 — Issue Summary

`main` is red across many consecutive commits. Two GitHub Actions workflows were provided; between them there are **three failing jobs** driven by **two independent root causes**.

| Run | Job | Failing step | Root cause |
|-----|-----|--------------|-----------|
| CI `28740341552` | `test-unit-contract` | Docker-free unit + contract tests | **#1** Migration-tool test hard-codes the `Debug` build path |
| CI `28740341552` | `integration-fast` | Docker-backed integration tests | **#2** 129 tests receive `401` — server requires a JWT the fixture never sends |
| Release `28740341543` | `release` | "Test unit and non-Docker suite" | **#1** (same migration-tool test) |

### Root cause #1 — hard-coded `Debug` tool path
`tests/Hexalith.Memories.Server.Tests/Migration/MigrateEmbeddingVectorsToolTests.cs` resolved the migration
tool at `…/bin/Debug/net10.0/MigrateEmbeddingVectors.dll`. CI builds and tests in **Release**
(`--configuration Release`), so on a clean checkout that path does not exist and
`File.Exists(toolPath)` fails the two process-spawning tests (`Help_IncludesAbortAndBlueGreenWording`,
`MultipleModes_ReturnsParserErrorIncludingAbort`). It passes locally only because a developer machine has
*both* configurations built.

### Root cause #2 — integration fixture never authenticates against the auth-protected server
Story 20.1 added a fallback authorization policy `RequireAuthenticatedUser()`
(`src/Hexalith.Memories.Server/Program.cs:78-81`), so every non-anonymous endpoint — including
`POST /api/tenants` — needs a valid bearer. Story 20.2 added a per-tenant authorization filter that requires
an **exact** match between the token's `memories:tenant` claim and the accessed tenant
(`TenantAuthorizationEndpointFilter.cs:72-77`), with **no** admin/wildcard bypass.

The integration fixture's `MemoriesClient` sends **no** bearer, and its `MintDevBearer` helper mints
**MCP-realm** tokens (wrong issuer/audience/key for the server). These integration tests were previously
`[Skip]`-ped and were un-skipped in commit `eb28361` ("ensuring all tests are runnable by default"), so they
now execute against an authentication wall that never gated them before — which is why `main` has been red
since.

The failure has **two layers**:
- **2a (test harness):** provisioning + all tenant-scoped `MemoriesClient` calls return `401`.
- **2b (product):** even after 2a, MCP tool-execution tests fail because the MCP forwards to the server's
  protected API with only `dapr-api-token` and no server JWT
  (`MemoriesMcpDaprInvocationHandler.cs`), so the server returns `401`.

---

## Section 2 — Impact Analysis

- **Epic/Story impact:** No scope change. This is CI/CD recovery for already-delivered Story 20.1/20.2
  (server authentication) and Story 21.x (migration tool). It closes a test-harness gap left when the
  integration suite was un-skipped, and completes the MCP→server service-to-service auth leg implied by
  Story 20.x.
- **Artifacts:** No PRD/architecture/UX changes. Test-harness + MCP composition + MCP dev config only.
- **Technical impact:**
  - `test-unit-contract` (CI) and `release` (Release) unblocked by #1.
  - `integration-fast` (CI) unblocked by 2a (~120 non-MCP tests + provisioning) and 2b (MCP tool-execution
    tests).
  - **Security model:** 2b widens nothing on the server; instead the MCP performs a scoped
    service-to-service token exchange, minting a server-realm JWT that carries **only** the caller's
    already-authorized tenant claim(s). The server's auth wall stays intact.

---

## Section 3 — Recommended Approach

**Direct adjustment** (no rollback, no MVP change). Approved scope: **full fix (2a + 2b)**, with 2b
implemented as **"MCP mints a server-realm token"** (service-to-service token exchange) per the
owner's decision.

- **Effort:** Small for #1; moderate for 2a (single-point handler); moderate for 2b (new options + factory
  + composition wiring + dev config).
- **Risk:** Low for #1 and 2a (unit-verified / compile-verified). 2b is product security code that
  **cannot be runtime-verified in the dev sandbox** (no Docker/Aspire/Dapr/Redis/FalkorDB topology) — CI is
  the verification gate.

---

## Section 4 — Detailed Change Proposals

### #1 — Dynamic tool-path resolution (Minor) — DONE, verified
`MigrateEmbeddingVectorsToolTests.cs`: replaced the hard-coded `Debug` segment with `ResolveToolPath`, which
resolves the DLL under the test assembly's actual build configuration via `AssemblyConfigurationAttribute`,
with a defensive Release/Debug fallback probe.

> Verified: deleted the Debug output to simulate a clean Release-only CI checkout — all **11** tests in the
> class pass. Full `Server.Tests` run: **2423 total, 0 failed, 1 skipped**.
>
> Note: this fix was auto-committed by the repository's own tooling as commit
> `3f416f7 feat(tests): Enhance migration tool tests with dynamic tool path resolution`.

### 2a — Fixture mints & attaches a per-request server-realm bearer (Moderate) — DONE, compile-verified
`tests/…/Fixtures/AspireIngestionPipelineFixture.cs`:
- Added server-realm constants (`ServerDevIssuer` / `ServerDevAudience` / `ServerDevSigningKey`) mirroring
  `src/Hexalith.Memories.Server/appsettings.Development.json`, and a `MintServerBearer(tenantId?)` helper.
- Added `ServerBearerAuthHandler : DelegatingHandler` that derives the tenant from each outgoing request
  (route `/api/tenants/{tenantId}/…` → query `?tenantId=` → JSON body `tenantId`) and attaches a matching
  server-realm token. Requests that already carry `Authorization` are left untouched (negative-auth tests
  keep control). It is concurrency-safe (per-request) and requires **no** per-test edits.
- Routed `MemoriesClient` through the handler while reusing Aspire's endpoint resolution for the base
  address.

### 2b — MCP mints a server-realm token for upstream calls (Moderate) — DONE, compile-verified
- **New** `src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpUpstreamAuthenticationOptions.cs` — options
  bound from `Authentication:ServerUpstream` (issuer/audience/signing-key/tenant-claim-name). Optional:
  empty signing key ⇒ no-op (unconfigured environments keep prior behavior).
- **New** `src/Hexalith.Memories.Mcp/Authentication/ServerUpstreamTokenFactory.cs` — mints a short-lived
  server-realm JWT carrying the caller's tenant claim(s) as a single space-joined `tenant_id` value (the
  server maps `tenant_id` → `memories:tenant`, splitting on spaces; tenant ids never contain spaces).
- **Edit** `McpCompositionRoot.cs` — register the options + factory; in the `MemoriesClient` invoke-client
  factory, read the caller's `memories:tenant` claims from `IHttpContextAccessor` and attach the minted
  token (mirrors the existing `ApplyDaprApiToken` pattern).
- **Edit** `src/Hexalith.Memories.Mcp/appsettings.Development.json` — add `Authentication:ServerUpstream`
  with the server's dev/test realm so the integration topology mints server-acceptable tokens. Production
  operators must configure this section (shared signing key) via secrets.

---

## Section 5 — Implementation Handoff

- **Classification:** Moderate.
- **State:** All four change sets implemented. Full solution builds clean in Release (0 warnings, 0 errors).
  Unit suites green: `Server.Tests` 2423/0-fail, `Mcp.Tests` 90/0-fail. #1 runtime-verified.
- **Verification gate:** `integration-fast` (2a + 2b) **must be verified by CI** — the dev sandbox has no
  Docker/Aspire topology. Re-run CI on the branch after these changes land.
- **Follow-up / watch items:**
  1. Confirm on CI green that `integration-fast` passes end-to-end, especially the MCP tool-execution tests
     (`CallTool_ValidBearer_MatchingTenantClaim_Succeeds`).
  2. **Production auth for 2b:** the shared upstream signing key is dev-only in appsettings. A production
     story should provision the `Authentication:ServerUpstream` secret (or migrate to asymmetric
     issuer-trust) so the MCP→server token exchange works outside dev.
  3. **Housekeeping:** the working tree carries unrelated `references/` submodule pointer changes not
     produced by this work; keep them out of the fix commit.
