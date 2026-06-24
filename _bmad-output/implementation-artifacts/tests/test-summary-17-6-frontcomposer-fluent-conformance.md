# Test Automation Summary — Story 17.6 (FrontComposer / Fluent UI Blazor V5 Conformance Hardening)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-06-24 · **Role:** QA automation engineer (tests only — no code review / story validation)
- **Feature under test:** Story 17.6's conformance gate — the source-scanning `Epic17ConformanceTests` +
  `Epic17ConformanceAllowlist`, the remediated Evidence Cockpit restrictive banner / Scope Header, and the
  centrally pinned Fluent UI version.

The story shipped with the **source scan** in place. This pass adds the **behavioural and lock gates that a
source scan cannot provide**, and closes a source-scan **bypass**.

This is a host-less Razor Class Library (component-specimen / bUnit posture — no runnable route or Playwright
host), so "E2E" here means rendered-DOM specimen tests, not browser tests. No API surface exists, so no API
tests apply.

## Discovered Gaps (auto-applied)

| # | Gap (previously **uncovered**) | AC | New gate |
|---|--------------------------------|----|----------|
| 1 | The remediated restrictive banner renders via `FluentMessageBar`, but only `data-restrictive-kind` was asserted — the **Fluent intent mapping** (unauthorized → `error`, every other restrictive state → `warning`) had **zero behavioural coverage**. A regression dropping the Fluent intent would have passed. | AC2 | `Epic17ConformanceRemediationTests` |
| 2 | The scope header moved from a hand-authored `.mem-evidence-label` weight ramp to `FluentLabel` typography — no test asserted the **Fluent typography primitives actually render**. | AC2 | `Epic17ConformanceRemediationTests` |
| 3 | **AC6 (package version lock)** had **no test at all** — `Host.ValidateVersionAlignment()` only checks FrontComposer major/minor, not the pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`. | AC6 | `Epic17ConformanceHardeningTests` |
| 4 | The forbidden-control vs tracked-semantic element sets could silently **overlap**, and the allowlist could hold **duplicate `(File, Pattern)` entries** — either would make a file's classification ambiguous. | AC1/AC4 | `Epic17ConformanceHardeningTests` |
| 5 | The scoped-CSS scan reads only `.razor.css`, so a hand-authored **inline `style=` / `<style>` block** in a `.razor` file was a **bypass** for the legacy tokens / raw hex / theme primitives AC3 forbids. | AC3 | `Epic17ConformanceHardeningTests` |

## Generated Tests

### E2E / rendered-DOM specimen tests
- [x] `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceRemediationTests.cs` (10 cases)
  - `RestrictiveBanner_Unauthorized_RendersFluentMessageBarWithErrorIntent`
  - `RestrictiveBanner_RestrictiveButAuthorizedState_RendersFluentMessageBarWithWarningIntent` (×7 states)
  - `RestrictiveBanner_SupportedPacket_RendersNoBanner`
  - `ScopeHeader_RendersScopeCaptionsViaFluentTypographyPrimitives`

### Conformance / lock gates (source-scanning)
- [x] `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs` (5 tests)
  - `PackageLock_DirectoryPackagesProps_PinsTheAuthoritativeFluentUiVersion`
  - `PackageLock_NoMemoriesProject_OverridesTheFluentUiVersionLocally`
  - `Register_ForbiddenControlAndTrackedSemanticElementSets_AreDisjoint`
  - `Register_AllowlistEntries_HaveNoDuplicateFileAndPatternPairs`
  - `InlineStyle_NoRazorFile_SmugglesALegacyTokenHexOrThemePrimitivePastTheScopedCssScan`

All tests use the established stack: xUnit v3, Shouldly, bUnit + `FrontComposerTestBase`, AngleSharp
`QuerySelectorAll`, `data-testid`/role/attribute selectors only (no CSS-class selectors, no sleeps, no
cross-test state). The exact rendered DOM (`<fluent-message-bar intent="…">`, `<fluent-label>`) was verified
against the live `5.0.0-rc.3-26138.1` package before asserting — not assumed from MCP docs.

## Coverage

- Restrictive precedence intents covered: unauthorized (`error`) + 7 restrictive-but-authorized states (`warning`).
- AC coverage added by this pass: **AC6 (new)**, plus behavioural reinforcement of AC2/AC3 and robustness of the AC1/AC4 register.
- Web RCL surfaces: 11/11 still gated by the conformance register (unchanged); the new banner/scope gates target the Evidence Cockpit + Scope Header remediation specifically.

## Validation (sandbox-safe runner)

- Build: `0` warnings / `0` errors under `TreatWarningsAsErrors=true`.
- New gap tests: **15/15** pass.
- Full `Hexalith.Memories.Web.Tests` suite: **475/475** pass (was 460/460 before this pass — `+15`, no regressions).
- `Components/Validation` namespace: **184/184** pass.
- `git diff --check`: clean. New files are LF-only.
- Runner: `DiffEngine_Disabled=true dotnet exec …/Hexalith.Memories.Web.Tests.dll …` (VSTest `dotnet test`
  is blocked in this WSL sandbox by `SocketException (13): Permission denied`).

## Checklist (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated (N/A — host-less RCL, no API surface)
- [x] E2E (rendered-DOM specimen) tests generated
- [x] Tests use standard test framework APIs (xUnit v3 / bUnit / Shouldly / AngleSharp)
- [x] Tests cover happy path (supported packet → no banner; complete scope renders)
- [x] Tests cover critical cases (unauthorized error intent; 7 warning-intent restrictive states; bypass/lock gates)
- [x] All generated tests run successfully (15/15; full suite 475/475)
- [x] Proper locators (semantic `data-testid`/role/attribute — no class selectors)
- [x] Clear, descriptive test names
- [x] No hardcoded waits or sleeps
- [x] Tests are independent (no order dependency, no shared state)
- [x] Test summary created, saved under `implementation-artifacts/tests/`, includes coverage metrics

## Next Steps

- Run in CI alongside the existing `Epic17*` suite.
- The deferred browser/AT validation gaps (axe, contrast, forced-colors, reduced-motion, zoom, screen reader)
  remain fail-closed in `Epic17ValidationInventory.Gaps` and are **out of scope** for this RCL — unchanged by this pass.
