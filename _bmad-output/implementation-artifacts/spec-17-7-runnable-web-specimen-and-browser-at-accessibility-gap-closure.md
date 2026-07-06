---
title: '17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure'
type: 'feature'
created: '2026-07-06T19:16:56+02:00'
status: 'in-review'
baseline_revision: '3160c6bcde46c1c52cdaf30b64997352f7f4b178'
final_revision: '8b9439ddb01aa80ee77390e19f88c07f96077b3b'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-17-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Epic 17 currently proves Memories web trust surfaces only through host-less bUnit component specimens. The browser, layout, axe, media-emulation, live focus, touch-target, and assistive-technology dimensions in `Epic17ValidationInventory.Gaps` remain fail-closed and cannot be treated as resolved without a runnable specimen and bounded evidence.

**Approach:** Add a test-only Blazor specimen host exposing the existing Epic 17 RCL components through stable fixture routes, then add a Memories Playwright workspace that runs smoke, axe, media/layout, sanitization, and evidence-summary checks against those routes. Update the machine-checked inventory so each prior gap is either evidence-backed or explicitly carried forward fail-closed with owner, severity, waiver state, and release disposition.

## Boundaries & Constraints

**Always:** Reuse existing Evidence Packet, recovery, form, grid, interaction, and lens fixtures; keep the host specimen-only; use FrontComposer and Fluent UI Blazor V5 primitives; keep selectors stable with `data-testid`, roles, or accessible names; sanitize artifacts and summaries for secrets, local paths, raw payload fragments, bearer tokens, provider internals, stack traces, and restricted source details.

**Block If:** A production web application, backend dependency, new public API, package upgrade, Evidence Packet semantic change, FrontComposer framework change, or unbounded artifact policy becomes necessary to make the browser lane pass. Also block if a browser cannot be launched and no evidence can be produced for the automated checks.

**Never:** Do not modify submodules, initialize nested submodules, claim product-route validation, copy incompatible Fluent examples, use raw controls where Fluent/FrontComposer has a primitive, add backend calls, broaden tenant scope, hide unresolved manual screen-reader or unsupported-browser gaps, or mark `Epic17ValidationInventory.Gaps` empty unless every dimension has evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Browser smoke | Playwright opens each specimen route | Required selector count is greater than zero and route metadata is written to bounded evidence | Missing route/selector fails the Playwright test |
| Axe scan | A route root selector is included in an axe scan | WCAG-supported tags run with zero blocking/unknown violations and nonzero scanned nodes | Blocking or unknown impact fails; report-only findings are summarized |
| Media/layout | Viewports 360, 768, 1024, 1440 plus forced-colors, reduced-motion, zoom/reflow, touch target checks | Trust-critical fields and controls remain visible or reachable, no horizontal-scroll-only trust access, touch controls meet 44x44 where measurable | Unsupported browser/tooling dimensions remain fail-closed in evidence, not passed |
| Artifact redaction | Screenshots, traces, copied text, axe JSON, route metadata, manual checklist | Summary contains only bounded relative paths and sanitized text | Redaction canary, absolute path, bearer token, provider/internal payload, or stack trace match fails validation |
| Manual AT evidence | Screen reader unavailable in unattended environment | Checklist-method evidence records workflow, browser, OS, tester/date, pass/fail, defects, waiver, owner, and release disposition, and keeps OS screen-reader gap fail-closed | Missing manual/checklist evidence fails inventory or summary validation |

</intent-contract>

## Code Map

- `tests/Hexalith.Memories.Web.Specimens/` -- new test-only fixture/manifest library shared by bUnit and the browser specimen host.
- `tests/Hexalith.Memories.Web.SpecimenHost/` -- new test-only Blazor host for stable Epic 17 fixture routes.
- `tests/Hexalith.Memories.Web.E2E/` -- new Playwright workspace, helpers, specs, and artifact validators.
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`, `tests/Hexalith.Memories.Web.Tests/Components/Recovery/RecoveryPacketFixtures.cs`, `tests/Hexalith.Memories.Web.Tests/Components/Lenses/LensPacketFixtures.cs`, and `tests/Hexalith.Memories.Web.Tests/Components/Forms/FormFixtures.cs` -- existing fixture callers to redirect to the shared test-only fixture library without changing values.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` -- machine-checked surface/gap register to update from deferred-only to evidence-backed or fail-closed rows.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17InventoryTests.cs` -- fail-closed assertions for evidence rows, gap carry-forward, and no over-claiming.
- `Hexalith.Memories.slnx` -- add any new .NET test/specimen projects.
- `.github/workflows/ci.yml` -- run the Memories web E2E specimen lane in CI.
- `Directory.Packages.props` -- centralize any new package versions only if a .NET package is unavoidable; do not add inline package versions.
- `_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md` -- bounded story evidence summary.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move story status according to implementation result.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.Web.Specimens/Hexalith.Memories.Web.Specimens.csproj`, `Epic17SpecimenFixtures.cs`, and `Epic17SpecimenManifest.cs` -- add a non-packable test-only library that centralizes the existing Evidence Packet, recovery, lens, form, selector, and route fixture data -- avoids parallel fixture drift.
- [x] `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`, `Components/Recovery/RecoveryPacketFixtures.cs`, `Components/Lenses/LensPacketFixtures.cs`, `Components/Forms/FormFixtures.cs`, and `Hexalith.Memories.Web.Tests.csproj` -- redirect existing bUnit fixture callers to `Hexalith.Memories.Web.Specimens` while preserving current fixture values and test behavior -- proves the browser host and bUnit suite share one fixture source.
- [x] `tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj`, `Program.cs`, `Components/App.razor`, `Components/Routes.razor`, and `Components/_Imports.razor` -- add a non-packable Blazor Server test host referencing `Hexalith.Memories.Web` and `Hexalith.Memories.Web.Specimens` -- enables browser execution without creating a production web app.
- [x] `tests/Hexalith.Memories.Web.SpecimenHost/Components/Pages/Epic17SpecimenIndex.razor` and `Epic17SpecimenSurface.razor` -- expose `/__memories/specimens` plus stable routes for every Story 17.7 named surface, using the shared manifest and preserving `data-testid` anchors -- gives Playwright deterministic route roots.
- [x] `Hexalith.Memories.slnx` -- include the new specimen library and specimen host under `/tests/` -- keeps restore/build entry points aligned with repository conventions.
- [x] `tests/Hexalith.Memories.Web.E2E/package.json`, `package-lock.json`, `playwright.config.ts`, `tsconfig.json`, `helpers/a11y.ts`, `helpers/artifacts.ts`, and `helpers/specimen-routes.ts` -- add a local Playwright + axe workspace with webServer startup, bounded output directories, route manifest, axe helper, media/layout helper, and redaction validator -- creates the automated browser lane.
- [x] `tests/Hexalith.Memories.Web.E2E/specs/specimen-smoke.spec.ts`, `specimen-a11y.spec.ts`, `specimen-media-layout.spec.ts`, `specimen-artifacts.spec.ts`, and `scripts/validate-artifacts.mjs` -- add smoke, axe, viewport/media, touch-target, focus-return/checklist, copied-text/screenshot/trace redaction, and evidence-summary specs over all specimen routes -- closes browser-backed validation gaps where tooling supports it.
- [x] `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` and `Epic17InventoryTests.cs` -- add evidence row types and assertions that resolve or carry forward each prior gap with owner, severity, waiver state, release disposition, route/specimen metadata, and evidence artifact path -- prevents silent over-claiming.
- [x] `.github/workflows/ci.yml` -- add a CI job for the Memories web specimen host build, E2E dependency install, Playwright Chromium lane, typecheck, and artifact validation -- prevents the new browser lane from drifting unprotected.
- [x] `_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md` -- record commands, browser/OS/tool versions, route coverage, axe/media/layout/manual/checklist results, sanitized artifact locations, unresolved fail-closed gaps, and redaction scan result -- provides reviewable evidence.
- [x] `_bmad-output/implementation-artifacts/17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- create the story handoff/status artifacts after validation -- keeps BMAD tracking consistent.

**Acceptance Criteria:**
- Given Story 17.6 conformance evidence is complete, when the specimen host runs, then every named Epic 17 surface has a stable route, required selector, fixture family, and no backend/product/public-API dependency.
- Given Playwright runs, when smoke and axe scan each route, then scans fail on zero target nodes, record route/selector/fixture metadata, and fail on blocking or unknown axe violations.
- Given media and layout checks run, when viewport, forced-colors, reduced-motion, zoom/reflow, and touch-target validations execute, then supported dimensions produce evidence and unsupported dimensions remain fail-closed.
- Given manual AT or checklist validation is recorded, when the evidence summary is generated, then it names workflow, viewport, browser, OS, method, tester/date, result, defects, severity, owner, waiver state, and release disposition.
- Given browser artifacts are produced, when redaction validation runs, then no artifact or summary exposes bearer tokens, local absolute paths, raw payload fragments, tenant-sensitive diagnostics, provider internals, stack traces, or restricted source details.
- Given the story completes, when inventory tests run, then each prior `Epic17ValidationInventory.Gaps` dimension is either evidence-backed or explicitly fail-closed with no product-route over-claim.

## Spec Change Log

## Review Triage Log

### 2026-07-06 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 0, medium 9, low 0)
- defer: 1: (high 0, medium 1, low 0)
- reject: 1: (high 0, medium 0, low 1)
- addressed_findings:
  - `[medium]` `[patch]` Durable evidence pointers and required artifact validation were tightened so inventory rows no longer cite ignored transient files or missing per-surface JSON.
  - `[medium]` `[patch]` Specimen route loading now fails on duplicate or missing expected slugs.
  - `[medium]` `[patch]` Axe checks now scope required anchors inside the scanned root and bound known `aria-prohibited-attr` incomplete findings as fail-closed evidence instead of full WCAG clearance.
  - `[medium]` `[patch]` Artifact redaction now requires the expected evidence files, broadens local-path canary detection, and requires explicit policy for the bounded non-text screenshot.
  - `[medium]` `[patch]` Media checks now fail directly when Chromium forced-colors or reduced-motion emulation fails.
  - `[medium]` `[patch]` Reflow and viewport evidence now record page overflow without weakening the trust-anchor reachability assertion; data-heavy overflow remains fail-closed.
  - `[medium]` `[patch]` Touch-target evidence now records measured undersized controls as fail-closed rather than treating measurements as a pass.
  - `[medium]` `[patch]` Browser evidence summaries and inventory dispositions were updated to avoid over-claiming product-route, full axe/WCAG, touch-target, and AT clearance.
  - `[medium]` `[patch]` CI now runs the Memories web specimen E2E lane.

### 2026-07-06 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 10: (high 0, medium 5, low 5)
- defer: 1: (high 0, medium 1, low 0)
- reject: 4: (high 0, medium 0, low 4)
- addressed_findings:
  - `[medium]` `[patch]` Unified the redaction canary: `validateArtifactText` restricted patterns now match the sanitizer / `validate-artifacts.mjs` breadth (broad Windows/Linux absolute paths) and add bare `eyJ…` JWT and .NET stack-frame detection, closing the raw-copy-text and .NET-trace redaction gaps.
  - `[medium]` `[patch]` Media forced-colors / reduced-motion `supported` is now derived from the browser's `matchMedia` state instead of a hardcoded `true`, so emulation that fails to engage is fail-closed rather than a silent pass.
  - `[medium]` `[patch]` Trust-anchor horizontal reachability is measured after resetting scroll and without `scrollIntoViewIfNeeded`, so content only reachable by horizontal scrolling now fails the no-horizontal-only assertion instead of being pulled into view first.
  - `[medium]` `[patch]` Reflow evidence uses a 320 CSS-pixel WCAG 1.4.10 viewport instead of the non-standard CSS `zoom: 400%`.
  - `[low]` `[patch]` Touch-target selector broadened to more interactive Fluent roles; zero-size / unmeasurable interactive controls are recorded fail-closed and the tautological `every(measurable)` assertion was removed.
  - `[medium]` `[patch]` `validate-artifacts.mjs` now schema-validates the AC4 manual-AT-checklist required fields (each present and non-empty), not just file presence and redaction.
  - `[low]` `[patch]` The specimen surface switch gained a `default:` case (`mem-specimen-unmapped`) so a manifest slug with no render mapping fails loudly instead of rendering an empty surface.
  - `[low]` `[patch]` Copied-text evidence claim scoped precisely: the browser scan is bounded to the clean agent-packet fixture and sensitive-payload sanitization is credited to bUnit `Epic17SanitizationCanaryTests`, removing the browser-proven-sanitization over-claim.
  - `[low]` `[patch]` Removed a stale duplicate `<summary>` XML-doc block in `Epic17FormFixtures.cs`.
  - `[low]` `[patch]` Aligned the CI E2E job's specimen-host build to the Debug configuration the Playwright webServer runs (dropped the discarded Release restore/build).

## Design Notes

Use a specimen-host route prefix such as `/__memories/specimens/{surface}` so the browser lane is obviously non-product. Prefer a typed route manifest shared by host rendering and Playwright tests so selectors, surface names, and fixture families cannot drift.

Manual assistive-technology evidence is allowed to use a checklist method when NVDA/Edge or another OS screen reader is unavailable in the unattended environment. That checklist does not resolve the OS screen-reader gap by itself; it records what was verified and keeps the unavailable dimension fail-closed.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj -m:1` -- expected: 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj -m:1` -- expected: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll` -- expected: all tests pass.
- `npm --prefix tests/Hexalith.Memories.Web.E2E ci` -- expected: dependencies install from committed lockfile.
- `npm --prefix tests/Hexalith.Memories.Web.E2E run typecheck` -- expected: TypeScript helpers and specs compile.
- `npm --prefix tests/Hexalith.Memories.Web.E2E run test` -- expected: Playwright browser validation passes or documents environment-specific unsupported dimensions as fail-closed.
- `npm --prefix tests/Hexalith.Memories.Web.E2E run validate:artifacts` -- expected: redaction and bounded-artifact validation passes.
- `python3 - <<'PY' ... yaml.safe_load('.github/workflows/ci.yml') ... PY` -- expected: CI YAML parses.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

**Summary:** Added a test-only Epic 17 Memories web specimen lane with shared fixtures, a Blazor specimen host, Playwright + axe browser checks, bounded artifact validation, CI coverage, and updated validation inventory. Automated Chromium specimen evidence is now durable and product-route/full AT/broad-browser/touch-source claims remain explicitly fail-closed.

**Files changed:**
- `.github/workflows/ci.yml` -- adds the Memories web specimen E2E CI job.
- `Hexalith.Memories.slnx` -- includes the specimen fixture library and specimen host projects.
- `tests/Hexalith.Memories.Web.Specimens/` -- centralizes Epic 17 fixture data and route metadata for bUnit and browser tests.
- `tests/Hexalith.Memories.Web.SpecimenHost/` -- exposes non-product `/__memories/specimens` routes for the Epic 17 surfaces.
- `tests/Hexalith.Memories.Web.E2E/` -- adds Playwright, axe, route, media/layout, redaction, and artifact validation coverage.
- `tests/Hexalith.Memories.Web.Tests/Components/*/*Fixtures.cs` -- delegates existing bUnit fixtures to the shared specimen library.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/` -- records evidence-backed rows and fail-closed residual accessibility gaps.
- `_bmad-output/implementation-artifacts/17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` -- adds the story handoff.
- `_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md` -- adds the bounded evidence summary.
- `_bmad-output/implementation-artifacts/deferred-work.md` and `sprint-status.yaml` -- record the incidental deferred finding and story status.

**Review findings breakdown:** Two review passes ran. The initial pass applied 9 medium patch findings, deferred 1 medium finding, and rejected 1 low finding. The follow-up pass applied 10 patch findings (5 medium, 5 low) that hardened the browser lane's evidence validity and redaction, deferred 1 medium finding, and rejected 4 low findings. Follow-up patches: unified/broadened redaction canary (bare-JWT + .NET stack frames); real `matchMedia`-derived media-emulation support; horizontal-only measurement no longer masked by auto-scroll; 320px WCAG reflow instead of CSS zoom; broadened touch-target selector with fail-closed unmeasurable accounting; AC4 manual-checklist schema validation; specimen-switch `default:` guard; precise copied-text scoping; duplicate XML-doc removal; and CI E2E build/webServer configuration alignment. The follow-up deferral (new ledger entry) records that `Hexalith.Memories.Web.Tests` — including the Epic 17 machine-checked inventory guards — is in the `.slnx` inventory but absent from every CI test lane, so those guards run locally/pre-commit but are not yet CI-enforced.

**Verification performed (follow-up pass):**
- `npm --prefix tests/Hexalith.Memories.Web.E2E run typecheck` -- passed.
- `CI=1 npm --prefix tests/Hexalith.Memories.Web.E2E run test` -- passed, 5/5 Chromium specs (baseline re-run before and after the patches).
- `npm --prefix tests/Hexalith.Memories.Web.E2E run validate:artifacts` -- passed, 9 bounded evidence artifacts + AC4 manual-checklist schema validation.
- `dotnet build tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj -m:1 -v:m` -- passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj -m:1 -v:m` -- passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll` -- passed, 476 tests.
- `python3 ... yaml.safe_load('.github/workflows/ci.yml')` -- CI YAML parses.
- `git diff --check` (working tree and since baseline) -- no whitespace errors.

**Residual risks:** Product-route validation, OS screen-reader/live AT validation, non-Chromium browser coverage, source-owned horizontal overflow, source-owned under-44px touch-target remediation, and the existing benchmark happy-state progress-bar axe issue remain fail-closed or deferred. Additionally, the Memories web bUnit + inventory-guard suite is not yet wired into a CI test lane (deferred, new ledger entry); it runs locally/pre-commit only, so the machine-checked over-claim guards are not currently CI-enforced.
