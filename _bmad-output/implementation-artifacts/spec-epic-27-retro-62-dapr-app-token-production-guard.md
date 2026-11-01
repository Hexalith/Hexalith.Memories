---
title: 'Ignore the Dapr app-token override in Production'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'oneshot'
baseline_commit: '76c737bfcb3b5ecf9ce26191eab13749fdade8d6'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 27 retro finding B1 (action item `epic-27-retro-item-62-honor-hexalith-dapr-applicationtokenover`). Since `32fc400e`, `DaprApplicationTokenMiddleware` honors `Hexalith:Dapr:ApplicationTokenOverride` in every environment. A present but empty value, or even a null-valued key, blanks the expected token and turns off the sidecar-to-app token check on every non-probe route. `DaprTokenStartupValidator` checks only the `APP_API_TOKEN` and `DAPR_API_TOKEN` environment variables, so a Production Server, MCP, AccessTelemetry or Clock pod with `Hexalith__Dapr__ApplicationTokenOverride=` passes startup unprotected. The middleware also walks the whole configuration tree on every request.

**Approach:** Fail Production startup when the override key holds a value, and make the middleware ignore the override in Production regardless, so the check never depends on the hosted validator being registered. Read the override once in the middleware constructor with a direct key lookup. Only a non-null value counts as an override. Development and the other non-Production environments keep the test-isolation behavior. Cover Production rejection, Production 401, the null-value rule and the unchanged Development override with focused tests.

</frozen-after-approval>

## Implementation Notes

Baseline `76c737bfcb3b5ecf9ce26191eab13749fdade8d6`. The pre-existing uncommitted retro outputs (`sprint-status.yaml` retro rows and `epic-27-retro-2026-11-01.md`) were preserved.

**Design: fail startup, and ignore the override in the middleware.** `DaprTokenStartupValidator` already fails Production closed on missing tokens, so a present override fails it the same way. A failed start surfaces the misconfiguration; a silent ignore would leave an operator believing the override applies. The middleware also ignores the override in Production, so the override has no effect even where the hosted validator is absent or removed. This guarantee covers only the override: as before, a Production middleware with no `APP_API_TOKEN` still passes requests, and the validator remains what guarantees the token.

**Production scope, and the remaining non-Production gap.** Production means `IHostEnvironment.IsProduction()`, matching the validator and the retro item's "only outside Production" wording. Outside Production, nothing requires `APP_API_TOKEN` at startup. So whoever can set the override can equally unset the token, and the override adds no capability there. It does still blank a token that is set. That matters for the deployed Qualification overlay (`deploy/kubernetes/overlays/qualification/qualification-enabled-patch.yaml:14,35,48`), whose pods carry `APP_API_TOKEN`. Hardening that is deferred: a Development-only override, or Qualification failing closed.

**Retro open question 1.** No manifest under `deploy/` sets the key. No initialized root submodule under `references/` mentions it either, including `references/Hexalith.Platform`. Platform composition repositories outside this workspace were not checked. Before rolling this out, confirm that no Production workload sets `Hexalith__Dapr__ApplicationTokenOverride`. A workload that does will crash-loop with the new message, and `docs/operations/deployment-configuration.md` now tells operators to remove it.

**Changes.**
- `DaprApplicationTokenMiddleware` takes `IHostEnvironment` and resolves the override once in a field initializer with a direct `configuration[key]` lookup. `AsEnumerable()` is gone. `APP_API_TOKEN` is still read per request; it is process state, not configuration, and the existing hosts and tests rely on that.
- The shared internal `ReadApplicationTokenOverride` defines "present" as non-null for both the middleware and the validator. The validator message names the colon key and the `__` environment-variable form, never the value.
- The fix covers all four hosts that use the middleware: Server, MCP, AccessTelemetry and AccessTelemetry.Clock. No `Program.cs` changed. `UseMiddleware` resolves the new constructor parameter from DI.
- The public constructor of the packable `Hexalith.Memories.ServiceDefaults` middleware gained a required parameter. This is binary-breaking only for direct `new` callers. The repository has none outside the auth tests, no package validation baseline exists, and hosts activate the middleware through `UseMiddleware`. So the change ships as a `fix` patch, without a breaking marker.
- This resolves the bug recorded at `deferred-work.md:5574`, scoped to Production. Lines 5570 and 5578 stay open. The tests now use the `DaprTokenEnvironmentCollection.Name` constant, but the duplicate `UseSetting` and `ConfigureAppConfiguration` override path in `EventStoreWebAppFactory` was left alone.

**Surprise: a chained host configuration hides empty values.** A generic `HostBuilder` passes host configuration into app configuration through `ChainedConfigurationProvider`. Its `TryGet` returns false for `""`, so the empty value is invisible and the override is not applied there. The old `AsEnumerable()` code honored such a key only through the null-counts bug. `WebApplicationBuilder` and `WebApplicationFactory` keep the value visible. A positive control proved it: with both EventStore factory overrides removed and a leaked `APP_API_TOKEN`, 13/13 tests failed; with only `UseSetting`, 13/13 passed. So `HealthCheckWebAppFactory` (only `UseSetting`) and `EventStoreWebAppFactory` needed no change. The host-level validator test uses `WebApplication.CreateBuilder` with `AddServiceDefaults`, the Server/MCP host shape.

**Files.**
- `src/Hexalith.Memories.ServiceDefaults/Security/DaprApplicationTokenMiddleware.cs` and `.../DaprTokenStartupValidator.cs`.
- `tests/Hexalith.Memories.Server.Tests/Authentication/DaprApplicationTokenMiddlewareTests.cs` and `.../DaprTokenStartupValidatorTests.cs`.
- `.../DaprTokenEnvironmentCollection.cs`, comment only.
- `docs/operations/deployment-configuration.md`, one operator note.
- Two entries appended to `deferred-work.md`.

**Fail-closed evidence.** This satisfies the project-context negative-evidence rule. The change is to Dapr channel authentication; no tenant surface changed.
- `DaprTokenStartupValidatorTests`: `StartAsync_InProductionHost_WithBothTokensAndEmptyHostConfigurationOverride_Throws` and `StartAsync_InProduction_WithBothTokensAndNonEmptyOverride_ThrowsWithoutEchoingValue`.
- `DaprApplicationTokenMiddlewareTests`, each returning 401:
  - `ProductionHost_EmptyOverride_RequestWithoutToken_IsRejected`.
  - `ProductionHost_NonEmptyOverride_DoesNotReplaceProcessToken`, 2 cases: the override token gets 401, the process token is accepted.
  - `HostConfiguration_NullOverrideUnderPresentSection_EnforcesProcessToken`, 2 cases.
  - `HostConfiguration_OverrideAddedAfterConstruction_IsNotHonored`.
- Development behavior is unchanged: `HostConfiguration_EmptyAppApiToken_IgnoresProcessEnvironment`, `HostConfiguration_NonEmptyOverride_ReplacesProcessToken` (2 cases) and `StartAsync_OutsideProduction_WithEmptyOverride_DoesNotThrow`.

**Mutation check.** Each mutation was killed by exactly its intended tests. Sources were restored and verified by SHA-256.
- M1 drops the Production guard: 1 middleware test fails.
- M2 drops the validator check: 2 validator tests fail.
- M3 restores the `AsEnumerable` null-counts semantics: 2 middleware and 2 validator cases fail.
- M4 switches to a per-request lookup: 1 middleware test fails.
- M5 honors a non-empty override in Production: both `ProductionHost_NonEmptyOverride_DoesNotReplaceProcessToken` cases fail.

**Validation.** `dotnet test` is sandbox-blocked. Each test project was built with `dotnet build <project> -c Debug` at 0 warnings and 0 errors. Freshness was confirmed with `strings -el`: the new message fragment and test literals are present, and the old message serves as a positive control. Then `DiffEngine_Disabled=true dotnet exec <dll> -class ...` ran the final post-review state:
- Server.Tests, 31 classes: all of `Authentication/`, `HealthChecks/` and `EventStoreIntegration/`, plus the doc guards `DeploymentConfigurationContractTests`, `AccessTelemetryOperationsContractTests` and `OperationalRunbookSetTests`. 239/239 passed.
- Mcp.Tests (`McpEndpointChallengeBodyTests`, `McpEndpointAllowAnonymousPathsTests`, `McpCompositionRootTests`): 11/11 passed.
- AccessTelemetry.Tests (`ClockAttestationCheckpointTests`, `ClockSigningReadinessHealthCheckTests`, `PhysicalReclamationEvidenceEndpointTests`): 31/31 passed.

Before review, the Server run with a leaked `APP_API_TOKEN=leaked-process-token` failed 5 `ServerEndpointRateLimitTests` cases. This is pre-existing: `TelemetryWebAppFactory` has never set the override (0 occurrences at baseline). Every EventStore-factory class passed under the leak.

The commit message `fix(security): ignore the Dapr app-token override in Production` was validated with the pinned `@commitlint/cli` 21.2.2 (exit 0); the negative control `Update stuff` exited 1. Nothing was committed, as requested.

## Review Triage Log

Blind Hunter, one pass, 16 findings.
1. `medium`, patched (wording) and deferred (behavior): the deployed Qualification overlay still honors the override. The spec's "opens nothing new" claim overstated the case and is corrected above. The behavior follows the retro item's Production-only scope, is pre-existing, and is deferred with hardening options.
2. `low`, patched: "defense in depth" overstated the guarantee. The notes now limit it to the override. Rejected: making the middleware fail closed in Production on a missing token is new behavior, and all four hosts register the validator through `AddServiceDefaults`.
3. `medium`, patched: there was no Production test with a non-empty override. Added `ProductionHost_NonEmptyOverride_DoesNotReplaceProcessToken`, which M5 kills, and the Development replacement test `HostConfiguration_NonEmptyOverride_ReplacesProcessToken`.
4. `low`, patched: the resolve-once comment read as a changelog and did not explain why `APP_API_TOKEN` stays per-request. The comment was rewritten.
5. `low`, rejected: no warning is logged when the override is honored or ignored. Production hosts now fail startup loudly. Outside Production the override is the intended test seam. Folded into the Qualification deferral.
6. `low`, split:
   - Patched: the message now also names `Hexalith__Dapr__ApplicationTokenOverride`.
   - Rejected: reporting every problem in one exception. It only matters when tokens are missing and the override is set at the same time, which is rare, and the fix is more than a simple correction.
7. `medium`, patched: operators had no documentation for the new startup failure. Added a note to `docs/operations/deployment-configuration.md`. The doc-guard classes pass.
8. `low`, patched: retro open question 1 was cited but not answered. The notes now record the answer.
9. `low`, deferred: no static guard asserts that manifests never set the key. Folded into the Qualification deferral.
10. `low`, rejected: the breaking marker. The constructor change breaks only direct `new` callers, and none exist. The patch-release rationale is recorded in the notes; listed for owner veto.
11. `low`, partly patched:
    - Patched: the notes now reference `deferred-work.md:5574`, and the tests use the collection-name constant.
    - Rejected: removing the duplicate override path in `EventStoreWebAppFactory`. It is pre-existing and already ledgered at `:5578`.
12. `low`, patched: the edited collection comment overgeneralized. It now names which factories overlay the override and states the Production startup failure.
13. `medium`, deferred: the planned `TelemetryWebAppFactory` overlay (`deferred-work.md:5570`) would make its derived Production host fail startup. Recorded with the fix constraint.
14. `low`, patched: added `baseline_commit` to the frontmatter.
15. `low`, patched: the public constant's XML doc no longer cross-references the internal validator or the retro ID. The retro ID moved to a code comment.
16. `low`, rejected:
    - The validator has no null check on `configuration`. It is an internal DI-only type, and the existing `environment` parameter has none either.
    - The middleware's null check on `environment` is untested.
    - Each test class keeps its own helpers, matching the existing self-contained style.
