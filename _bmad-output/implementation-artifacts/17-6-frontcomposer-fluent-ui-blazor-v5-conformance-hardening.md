---
baseline_commit: cce068cdb9e8d31233ac00693d7f83c10aae1c34
---

# Story 17.6: FrontComposer and Fluent UI Blazor V5 Conformance Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer of the Memories web UX,
I want all Memories web components to use only FrontComposer and Fluent UI Blazor V5 components and tokens,
so that Epic 17 cannot drift into a parallel design system or raw HTML/CSS implementation.

## Acceptance Criteria

1. **Classification audit.** Given the existing `Hexalith.Memories.Web` RCL from Story 17.1, when conformance is audited, then every `.razor` and `.razor.css` file is classified as one of: FrontComposer component usage, Fluent UI Blazor V5 component usage, unavoidable semantic/container markup, or a violation requiring remediation.
2. **Component preference.** Given a FrontComposer or Fluent UI Blazor V5 component exists for a control, status indicator, message, badge, stack/layout, grid/list, dialog/drawer, menu, tooltip, input, command surface, tab, or data display, when the Memories web component renders that function, then it uses the component rather than raw HTML or a custom UI primitive.
3. **CSS validation.** Given hand-authored CSS remains, when it is reviewed, then it contains only layout the design system does not own, uses Fluent 2 tokens where tokens are needed, and does not define theme primitives, direct typography ramps, direct foreground roles, legacy Fluent v4/FAST tokens, or one-off status color systems.
4. **Allowlist recording.** Given an exception is unavoidable, when it remains in source, then a conformance allowlist names the file, the selector or markup pattern, the reason, the missing FrontComposer/Fluent primitive, the owner story, and the removal condition.
5. **Future-story enforcement.** Given Stories 17.2 through 17.5 are implemented, when their code is reviewed, then they reuse the same conformance tests and cannot add new raw UI/CSS exceptions without an explicit allowlist entry.
6. **Package version lock.** Given the Fluent UI Blazor package version is checked, when component APIs are selected, then implementation follows the centrally pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` and the aligned `Hexalith.FrontComposer` submodule; incompatible Fluent UI MCP documentation examples (which target `5.0.0.26139`) are not copied blindly.
7. **Focused validation passes.** Given focused validation runs, then `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`, the new conformance tests, and `git diff --check` all pass.

## Conformance Boundary (authoritative)

This is the invariant Story 17.6 enforces. It is sourced from the UX Design Specification §1.1 "Design System Choice / Implementation Approach", the Architecture "Web UI / RCL (Epic 17)" boundary, `Hexalith.AI.Tools/hexalith-ux-instructions.md`, and Sprint Change Proposal `sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` (the story's origin).

- **Composition boundary:** FrontComposer is the application composition boundary; Fluent UI Blazor V5 is the component-primitive boundary. Memories web components compose FrontComposer shell/composition primitives and Fluent UI Blazor V5 primitives before any Memories-specific wrapper.
- **No raw controls when a component exists:** Raw HTML controls, custom UI primitives, JavaScript UI behavior, and third-party UI components are **not allowed** when a FrontComposer or Fluent UI Blazor V5 component exists for that function.
- **Hand-authored markup/CSS is exception-only:** Allowed only for unavoidable semantic/container structure or layout the design system does not own (flex/grid, gaps, user-agent resets, accessibility utilities), and every exception must be justified and covered by conformance tests (the allowlist).
- **Tokens, not theme forks:** Use Fluent UI V5 component parameters and Fluent 2 design tokens for color, typography, spacing, status, and focus. Do **not** redefine the theme, recreate Fluent primitives in scoped CSS, or use legacy Fluent v4/FAST tokens.
- **`Hexalith.Memories.Web` must not become** a standalone design system, a raw-HTML control library, or a CSS theme fork.

## Tasks / Subtasks

- [x] **Task 0 — Confirm scope, boundary, and authoritative version (AC: 1, 6)**
  - [x] Read Stories 17.1–17.5 and the Conformance Boundary above before changing any file. This is a hardening/remediation story over the existing RCL; do **not** add new product workflows, Evidence Packet semantics, recovery/filter/lens/benchmark/operator-health/MCP semantics, or FrontComposer framework changes.
  - [x] Verify the local Fluent UI Blazor package in `Directory.Packages.props` and `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The aligned package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the Fluent UI MCP documentation targets `5.0.0.26139` and is incompatible, so local package/submodule code and tests are authoritative when signatures differ.
  - [x] Inventory the FrontComposer shell components available under `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/` (e.g. `FcStatusBadge`, `FcDesaturatedBadge`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`) and the Fluent UI V5 component set, so remediation prefers an existing primitive over a custom one.
  - [x] Do not initialize nested submodules or run recursive submodule updates. FrontComposer is a root-level submodule; treat any submodule edit as out of scope for this story unless explicitly re-scoped.

- [x] **Task 1 — Audit and classify every `.razor` and `.razor.css` file (AC: 1)**
  - [x] Enumerate all 19 `.razor` components and 4 `.razor.css` files under `src/Hexalith.Memories.Web/` (see Dev Notes → Surface Inventory).
  - [x] Classify each file/region as: (a) FrontComposer component usage, (b) Fluent UI Blazor V5 component usage, (c) unavoidable semantic/container markup (allowlist candidate), or (d) a violation requiring remediation.
  - [x] Produce a machine-checked classification table so the audit is reproducible and fails closed when a new file appears unclassified (extend the `Epic17ValidationInventory` pattern — see Dev Notes → Allowlist Mechanism).
  - [x] Treat the following as known starting points (verified in source at baseline `cce068c`): `MemoriesEvidenceCockpit.razor` raw `article`/`section`/`p`/`strong`/`h2`; `MemoriesSourceCitationStack.razor` raw `section`/`ol`/`li`/`div`/`span`/`p`/`dl`/`dt`/`dd`; `MemoriesRetrievalAxisBreakdown.razor` raw `ol`/`li`/`dl`; `MemoriesGraphPathSummary.razor` raw `section`/`dl`/`dt`/`dd`/`span`; plus container `section`/`header`/`div`/`footer`/`strong` in `MemoriesLensShell.razor`, `MemoriesInteractionForm.razor`, and `MemoriesRecoveryActionPanel.razor`.

- [x] **Task 2 — Remediate raw-control usage to FrontComposer / Fluent V5 components (AC: 2)**
  - [x] For each region classified as a violation, replace raw markup with the equivalent FrontComposer or Fluent UI V5 component while preserving the existing `data-testid` anchors, `aria-label`/`role` semantics, accessible names, heading outline, redaction behavior, and contract-backed values that Stories 17.1–17.5 and the `Epic17*` validation suite assert.
  - [x] Replace the restrictive/alert status banner in `MemoriesEvidenceCockpit.razor` (`<section role="alert" class="mem-evidence-restrictive">`) with a Fluent message/notification primitive carrying the correct intent (e.g. `FluentMessageBar` with warning/error intent) so status color, icon, and accessible role come from the design system rather than hand-authored CSS.
  - [x] Prefer Fluent typography components/parameters (e.g. `FluentLabel`, `FluentText` with `Typo`/`Weight`/`Color`) over raw `p`/`strong`/`span` carrying visual weight; prefer `FluentStack` over layout-only `div`s where it expresses the layout intent.
  - [x] Keep all interactive controls Fluent/FrontComposer (the RCL already uses `FluentButton`, `FluentDataGrid`, `FluentCheckbox`, `FluentMessageBar`, `FcStatusBadge`, `FcDestructiveConfirmationDialog`); do not regress any of these to raw `button`/`input`/`select`/`form`.
  - [x] Do not change component public parameters, mapper outputs, view-model shapes, resource keys, or rendered trust semantics. This is a presentation-conformance refactor, not a behavior change.

- [x] **Task 3 — Remediate hand-authored CSS to Fluent 2 tokens / layout-only (AC: 3)**
  - [x] Remove legacy Fluent v4/FAST `*-rest` tokens and their hardcoded hex fallbacks from `MemoriesEvidenceCockpit.razor.css` (`--warning-fill-rest`/`#fef3c7`, `--warning-stroke-rest`/`#f59e0b`, `--warning-foreground-rest`/`#92400e`, `--danger-fill-rest`/`#fee2e2`, `--danger-stroke-rest`/`#ef4444`, `--danger-foreground-rest`/`#991b1b`, `--neutral-stroke-rest`) and from `MemoriesRetrievalAxisBreakdown.razor.css` / `MemoriesSourceCitationStack.razor.css` (`--neutral-stroke-rest`).
  - [x] Where the status color and border for the restrictive banner are handled by a Fluent message component (Task 2), delete the corresponding `.mem-evidence-restrictive*` color/border CSS entirely rather than re-tokenizing it.
  - [x] Where a token is still genuinely needed (e.g. a neutral stroke on a layout container the design system does not own), use Fluent 2 tokens (`--colorNeutralStroke1`, `--colorNeutral*`, `--strokeWidth*`, `--borderRadius*`, `--spacingHorizontal*`/`--spacingVertical*`, `--fontSizeBase*`, `--lineHeightBase*`) — never `*-rest`/FAST tokens, `--type-ramp-*`, `--accent-*`, `--neutral-fill-*`, `--palette-*`, raw hex, or one-off status color systems.
  - [x] Keep only layout the design system does not own (grid/flex, gaps, `overflow-wrap`, the `.visually-hidden` accessibility utility). `MemoriesGraphPathSummary.razor.css` is already layout-only (grid + `.visually-hidden`) and is the reference for an acceptable `.razor.css`.
  - [x] If a child component renders the styled element (Blazor scoped CSS does not cross component boundaries — see Story 17.1 review learning), place any remaining layout CSS in the child's own `.razor.css`.

- [x] **Task 4 — Establish the conformance allowlist (AC: 4)**
  - [x] Create a single source-of-truth allowlist for unavoidable exceptions. Each entry must carry all six required fields: `File`, `Selector or markup pattern`, `Reason`, `Missing FrontComposer/Fluent primitive`, `Owner story`, `Removal condition`. Model it on the existing `Epic17ValidationInventory` register (typed records + fail-closed tests) rather than a free-text doc.
  - [x] Keep the allowlist **minimal and justified**: prefer remediation over allowlisting. Acceptable allowlist candidates are semantic/container elements with no Fluent equivalent (e.g. `ol`/`li`/`dl`/`dt`/`dd` ordered/description lists, `article`/`section`/`header`/`footer` landmarks, the `.visually-hidden` utility) — each must still name the missing primitive and a removal condition.
  - [x] Every entry's `Removal condition` must be objective and verifiable (e.g. "when FrontComposer/Fluent ships an ordered-list primitive"), not "when we have time".
  - [x] Entries whose owner is a future story must reference a concrete story id; do not invent stories — record as the current epic's deferred work if no owner story exists yet.

- [x] **Task 5 — Build the reusable conformance test suite (AC: 1, 2, 3, 5, 7)**
  - [x] Add source-scanning conformance tests under `tests/Hexalith.Memories.Web.Tests/Components/Validation/` (alongside the `Epic17*` suite) that read the `.razor` and `.razor.css` source files at test time and assert conformance. Resolve the source path from the test assembly location (e.g. walk up from `AppContext.BaseDirectory` to the repo `src/Hexalith.Memories.Web`); the test project already has a `ProjectReference` to the Web RCL but does not embed `.razor` sources, so read from disk.
  - [x] Test: every `.razor` and `.razor.css` file is classified (no unclassified file) — fail closed when a new file is added without classification (mirrors `Epic17InventoryTests` "every required surface is named" gate).
  - [x] Test: no disallowed raw HTML control element appears in a `.razor` file unless its file+pattern is in the allowlist (scope the forbidden set to controls a component owns — buttons, inputs, selects, etc. — distinct from allowlisted semantic/container elements).
  - [x] Test: no `.razor.css` file contains legacy `*-rest`/FAST tokens, `--type-ramp-*`, `--accent-*`, `--neutral-fill-*`, `--palette-*`, raw hex colors, or `font-size`/`font-weight`/`line-height`/`color` declarations that recreate theme primitives, unless allowlisted.
  - [x] Test: every allowlist entry resolves to a real file and a real pattern present in that file (no stale/dead allowlist entries), and every entry carries all six required fields (mirrors `Epic17InventoryTests` "every row fills every required column").
  - [x] Ensure the conformance tests cover the full RCL surface so Stories 17.2–17.5 are also gated and cannot add new raw UI/CSS exceptions without an allowlist entry (AC5).
  - [x] Keep using the established test stack: xUnit v3, Shouldly, bUnit `2.8.4-preview`, `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`), `AddFluentUIComponents()`, AngleSharp `QuerySelectorAll` for rendered-DOM assertions. Use `data-testid`/role/label selectors, never CSS-class selectors, sleeps, or cross-test state.

- [x] **Task 6 — Re-verify the existing Epic 17 validation suite still passes (AC: 7)**
  - [x] Run the full `Hexalith.Memories.Web.Tests` suite and confirm the existing `Components/Validation` `Epic17*` tests (responsive parity, accessibility sweep, focus contracts, sanitization canaries, inventory) still pass after remediation — the refactor must not break any asserted `data-testid` anchor, heading outline, accessible name, live-region pairing, focus contract, or redaction canary.
  - [x] Confirm dual-language resource parity is preserved (EN `MemoriesWebResources.resx` + FR `MemoriesWebResources.fr.resx`); remediation must not drop or rename localized keys.
  - [x] Build under `TreatWarningsAsErrors=true` with `0` warnings / `0` errors. Run the new conformance tests. Run `git diff --check`.
  - [x] Use the sandbox-safe runner: `dotnet test` aborts here on a VSTest local-TCP listener (`SocketException (13): Permission denied`); run the xUnit v3 in-process runner directly with `DiffEngine_Disabled=true` against `tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll`.

- [x] **Task 7 — Record cross-story conformance reminders (AC: 5)**
  - [x] Confirm the per-story preflight conformance reminder added by commit `7a48396` is present in Stories 17.2–17.5 and the post-completion amendment is present in Story 17.1; refresh them only if remediation changes the cited facts. Keep edits to `_bmad-output/` story files documentation-only.

## Dev Notes

### Current Implementation State

- `Hexalith.Memories.Web` is a **host-less Razor Class Library** (no `Program.cs`, no `@page`, no Playwright host). It references `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Shell` (root submodule, via `$(HexalithFrontComposerRoot)`) and the `Microsoft.FluentUI.AspNetCore.Components` package; it builds over `Hexalith.Memories.Contracts.V1` Evidence Packet semantics. All Epic 17 validation is therefore **component-specimen** level (bUnit), not product-route — keep it that way.
- The RCL was built across Stories 17.1–17.5 using a consistent architecture: a thin `.razor` view + a typed **mapper** (`*Mapper.cs`) that projects the canonical `EvidencePacket` into a **view model** (`*ViewModel.cs`) + enums/records, with **field-to-source traceability** types and **dual-language** resources (`MemoriesWebResources.resx` / `.fr.resx`). **Preserve this architecture** — 17.6 is a presentation-conformance refactor only.
- Story 17.1 explicitly deferred conformance cleanup to this story (Story 17.1 completion amendment, via commit `7a48396`): *"raw markup/scoped CSS and non-Fluent-2 token usage must be audited and remediated under Story 17.6."* **There is no conformance allowlist or source-scanning conformance test yet** — 17.6 creates both.

### Verified Violations to Remediate (baseline `cce068c`)

These were confirmed by reading source directly (not inferred). Treat as the seed list, not the complete set — Task 1 must audit all 23 files.

- **Legacy FAST/Fluent-v4 tokens + hardcoded hex (AC3), `MemoriesEvidenceCockpit.razor.css`:** `--warning-fill-rest`/`#fef3c7` (L17), `--warning-stroke-rest`/`#f59e0b` (L18), `--warning-foreground-rest`/`#92400e` (L20), `--danger-fill-rest`/`#fee2e2` (L26), `--danger-stroke-rest`/`#ef4444` (L27), `--danger-foreground-rest`/`#991b1b` (L28), `--neutral-stroke-rest` (L40). The `*-rest` suffix is the FAST/Fluent-v4 rest-state convention — **not** Fluent 2 — and the hex fallbacks form a one-off status color system. Both are AC3 violations.
- **Legacy token (AC3), `MemoriesRetrievalAxisBreakdown.razor.css` L15 and `MemoriesSourceCitationStack.razor.css` L15:** `--neutral-stroke-rest`.
- **Raw status/alert control (AC2), `MemoriesEvidenceCockpit.razor` L11–17:** `<section role="alert" class="mem-evidence-restrictive">` is a hand-authored status banner where a Fluent message/notification component exists — remediate to `FluentMessageBar` (warning/error intent) so color/icon/role come from Fluent.
- **Raw text-weight markup (AC2), throughout `MemoriesEvidenceCockpit.razor` / source/axis/graph components:** `<p>`, `<strong>`, `<span>`, `<h2>` carrying visual emphasis — prefer Fluent typography components/parameters.
- **Semantic/container markup (likely AC1 "unavoidable" → allowlist candidates, confirm in Task 1):** landmark `article`/`section`/`header`/`footer`, ordered/description lists `ol`/`li`/`dl`/`dt`/`dd`, and the `.visually-hidden` utility. These have no direct Fluent primitive; if kept, each needs an allowlist entry (file, pattern, reason, missing primitive, owner, removal condition). `MemoriesGraphPathSummary.razor.css` (grid + `.visually-hidden` only) is the reference for an acceptable layout-only `.razor.css`.
- **Note on a prior false-positive read:** an automated audit pass during story creation initially characterized the `*-rest` tokens as "Fluent 2 tokens, no legacy detected." That is **wrong** — confirm tokens against the Fluent 2 naming families below; do not trust a green read that contradicts the Sprint Change Proposal's named violations.

### Fluent 2 Token Guidance

- **Forbidden (legacy Fluent v4 / FAST):** any `*-rest`/`*-hover`/`*-active` rest-state token, `--type-ramp-*`, `--accent-*`, `--neutral-fill-*`, `--neutral-foreground-*`, `--palette-*`, raw hex colors, and any CSS that recreates a heading ramp (`font-size`+`font-weight`+`line-height`) or a foreground role (`color:`).
- **Allowed (Fluent 2 design tokens), only when a component cannot express it:** color `--colorNeutralForeground*`, `--colorNeutralBackground*`, `--colorNeutralStroke*`, `--colorStatus{Success,Warning,Danger}{Foreground,Background,Border}*`; typography `--fontSizeBase*`, `--lineHeightBase*`, `--fontWeight*`; spacing `--spacingHorizontal*`, `--spacingVertical*`; shape `--borderRadius*`, `--strokeWidth*`.
- **Prefer component over token:** for status/typography/spacing, prefer a Fluent component/parameter (`FluentMessageBar` intent, `FluentLabel`/`FluentText` `Typo`/`Color`, `FluentStack` gaps) over a CSS token. Tokens are the fallback for layout the design system does not own; raw CSS is the last resort and must be allowlisted.

### Allowlist Mechanism (extend the existing pattern)

- The established pattern for a machine-checked, fail-closed register in this test project is `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` (typed `record` rows + `Epic17InventoryTests` that assert every required row is present and every column is filled). **Reuse this pattern** for the conformance allowlist — a typed collection of exception records, guarded by tests, not a free-text markdown file.
- Required fields per entry (AC4): `File`, `Selector or markup pattern`, `Reason`, `Missing FrontComposer/Fluent primitive`, `Owner story`, `Removal condition`.
- The allowlist is also the AC1 classification register: a file/region is either remediated, classified as unavoidable-and-allowlisted, or the conformance test fails. There is no silent-exempt path.

### Conformance Test Design

- **Location/stack:** add to `tests/Hexalith.Memories.Web.Tests/Components/Validation/` (the `Epic17*` namespace). Stack: xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit `2.8.4-preview`, `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`, `AddFluentUIComponents()`), AngleSharp for rendered-DOM queries.
- **Source scanning:** the test project references the Web RCL as a `ProjectReference` but does **not** copy/embed `.razor` sources, so the conformance test must read source files from disk. Resolve `src/Hexalith.Memories.Web` relative to the test assembly (`AppContext.BaseDirectory` → walk up to repo root → `src/Hexalith.Memories.Web`). Do not hardcode an absolute machine path (`git diff --check` and CI must pass on any clone).
- **Reuse `Epic17ValidationTestBase`** for any rendered-DOM assertions (it already renders all 11 packet surfaces via bUnit and exposes a `QueryAll` AngleSharp helper). Source-scanning (string/regex over file contents) is the new dimension this story adds.
- **Fail-closed**, like the existing inventory tests: a new `.razor`/`.razor.css` file with no classification, a disallowed token, a raw control with no allowlist entry, or a stale allowlist entry must turn the suite **red**.

### Established Architecture Patterns to Preserve

- Thin `.razor` (render only) + `*Mapper.cs` (pure, typed `EvidencePacket → ViewModel`) + `*ViewModel.cs`/enums/records + `*Traceability.cs` field-to-source tables + `*ResourceKeys.cs` + dual `.resx`. Remediation must not move logic into `.razor`, fork view models, or invent web-local DTOs.
- Redaction is a security surface: visible text **and** accessible names/`aria-label`/copy/export payloads run through the `*Display.SafeText`-style sanitizers. The `Epic17SanitizationCanaryTests` seed canaries and fail if any leak — do not bypass `SafeText` when swapping in Fluent components.
- Restrictive-state precedence, token-budget labeling, heading outline (`MemoriesEvidenceCockpit` uses a single top-level `h2`; no skipped levels), and live-region pairing (`status`↔`polite`, `alert`↔`assertive`) are asserted by `Epic17AccessibilitySweepTests` — preserve them through the refactor.

### Component & Token Reference

- **FrontComposer shell components** (`Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/`): `FcStatusBadge`, `FcDesaturatedBadge`, `FcDestructiveConfirmationDialog` (owns focus trap/return — already used by `MemoriesActionConfirmation`), `FcFormAbandonmentGuard`. Imports already wired in `_Imports.razor` (`Hexalith.FrontComposer.Contracts.Attributes`, `Hexalith.FrontComposer.Shell.Components.Badges`, `Hexalith.Memories.Contracts.V1`, `Microsoft.FluentUI.AspNetCore.Components`).
- **Fluent UI V5 components already in use:** `FluentButton`, `FluentLabel`, `FluentStack`, `FluentDataGrid`, `FluentCheckbox`, `FluentMessageBar`. Prefer extending this set over new primitives. When a Fluent API differs from MCP docs, the local `5.0.0-rc.3-26138.1` package/submodule is authoritative.
- **Page-section convention** (`hexalith-ux-instructions.md`): page-like surfaces with two or more sibling titled content sections should group them in a single `FluentAccordion` (one `FluentAccordionItem` per section), keeping titles/breadcrumbs/toolbars and a single primary content region outside it. This RCL is a component library (not pages), so apply this only if a composed surface presents multiple sibling titled sections; do not force it onto single-content components.

### Package Version Lock

- `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` (centrally pinned in `Directory.Packages.props`, comment tags it to Story 17.1; aligned with `Hexalith.FrontComposer/Directory.Packages.props`). `bunit` `2.8.4-preview`, `xunit.v3` `3.2.2`. **Never** add `Version` attributes to `.csproj` files; central package management is mandatory and `TreatWarningsAsErrors=true` is repo-wide.

### Testing Notes

- Target framework is `net10.0`; the test dll is `tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll`.
- Sandbox: `dotnet test` aborts on a VSTest local-TCP listener (`SocketException (13): Permission denied`). Run the xUnit v3 in-process runner directly with `DiffEngine_Disabled=true` (see `[[running-dotnet-tests-in-sandbox]]`). Baseline suite before this story: **453/453** passing (`Components/Validation` namespace **162/162**), build `0/0`.
- Follow project test rules: Shouldly assertions (`ShouldBe`, `Should.Throw`), NSubstitute mocks, descriptive PascalCase names, tests mirror product folders under `Components/...`, no `using Xunit;` (global using exists). Web tests use bUnit + FrontComposer testing helpers and assert rendered states/accessibility attributes/conformance.

### Previous Story Intelligence

- **17.1 (Evidence Cockpit):** built the RCL; review fixed a Blazor scoped-CSS boundary bug (a parent `.razor.css` cannot style markup rendered by a child component → move styling into the child's own `.razor.css`), tightened `SafeText` to replace only the matched span, and removed dead command UI. It explicitly left raw markup/CSS/non-Fluent-2 tokens for 17.6.
- **17.2–17.4:** recovery grammar, interaction patterns (forms/filters/grid/command/confirmation/navigation), and five role lenses — all on the mapper+thin-razor+dual-resx pattern, using `FcStatusBadge`/`FcDestructiveConfirmationDialog`/`FluentMessageBar`. A 17.3 review fixed a redaction-parity gap (raw value in a `data-*` attribute while the visible label was sanitized) — watch for the same when swapping components.
- **17.5 (validation):** introduced the `Components/Validation/Epic17*` suite and the fail-closed `Epic17ValidationInventory` register; established the host-less-RCL → component-specimen validation posture and the deferred browser/AT gap register (Playwright/axe/contrast/forced-colors/reduced-motion/zoom/screen-reader). 17.6 extends this register with the conformance allowlist; it does not close the browser/AT gaps.

### Git Intelligence

- `cce068c` 17.5, `9d6d2d3` 17.4, `a66019d` 17.3, `349c748` 17.2 — the Epic 17 build sequence. `7a48396 feat: Enforce FrontComposer and Fluent UI Blazor V5 usage in Hexalith.Memories UX` added the conformance boundary to the UX spec/architecture/epics, the Story 17.1 post-completion amendment, the 17.2–17.5 preflight reminders, and the 17.6 backlog entry. Commit type guidance: a presentation-conformance refactor with no new public capability is a `refactor`, not a `feat` (semantic-release sensitive) — but the new conformance test suite is additive; choose the conventional-commit type to match the net change and the team's release intent.

### Dependencies and Non-Goals

- **Depends on:** the existing `Hexalith.Memories.Web` RCL (Stories 17.1–17.5, all `done`) and the `Components/Validation/Epic17*` test infrastructure (17.5). No backend/contract dependency; Story 2.7 (`review`) is not required — only its already-consumed `Contracts.V1` fixtures.
- **Non-goals / Out of scope:** broad FrontComposer framework redesign; Fluent UI package upgrade beyond the pinned V5 prerelease; new Evidence Packet semantics; any backend, CLI, MCP, storage, ingestion, search, or tenant-isolation behavior; recursive submodule initialization or casual submodule changes; closing the deferred browser/axe/forced-colors/reduced-motion/zoom/screen-reader validation gaps (these remain fail-closed in `Epic17ValidationInventory.Gaps`).

### Project Structure Notes

- Source: `src/Hexalith.Memories.Web/Components/{Evidence,Grid,Filters,Forms,Interaction,Recovery,Lenses}/...`; tests: `tests/Hexalith.Memories.Web.Tests/Components/{...,Validation}/...`. New conformance tests belong in `Components/Validation/` (the `Epic17*` namespace). No new project, no `.csproj` package/version edits expected. Keep file-scoped namespaces, the ITANEO MIT header on new `.cs` files, `sealed` types, and `_camelCase` private fields.

### References

- `_bmad-output/planning-artifacts/epics.md` — Epic 17 "UX implementation boundary" and Story 17.6 acceptance criteria, target artifacts, and out-of-scope list.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24-frontcomposer-fluent-v5-ux-only.md` — story origin: the conformance rule, named current violations, allowlist field spec, removal-condition requirement, and version bump to `5.0.0-rc.3-26138.1`.
- `_bmad-output/planning-artifacts/ux-design-specification.md` §1.1 Design System Choice / Implementation Approach — FrontComposer + Fluent V5 boundary, token rules; UX-DR15.
- `_bmad-output/planning-artifacts/architecture.md` — "Web UI / RCL (Epic 17)" boundary (FrontComposer-aligned RCL over `Contracts.V1`; must not become a design system / raw-HTML library / CSS theme fork).
- `Hexalith.AI.Tools/hexalith-ux-instructions.md` — FrontComposer + Fluent V5 only; forbidden legacy/FAST tokens; Fluent 2 token families; `FluentAccordion` page-section rule.
- `_bmad-output/project-context.md` — .NET 10 / C# 14, central package management, warnings-as-errors, "Web UI must use FrontComposer + Fluent UI V5", "never copy legacy Fluent tokens into new UI", submodule policy.
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md` — RCL origin + scoped-CSS-boundary and `SafeText` learnings; the deferral of conformance cleanup to 17.6.
- `_bmad-output/implementation-artifacts/17-5-responsive-and-accessible-web-validation.md` — `Epic17*` validation suite, `Epic17ValidationInventory` fail-closed register, host-less-RCL posture, sandbox test workaround.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` + `Epic17InventoryTests.cs` — the register/fail-closed-test pattern to model the allowlist on.
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor` + `.razor.css` — primary remediation target (raw status banner, legacy tokens, hex fallbacks).
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/` — available FrontComposer primitives; `Hexalith.FrontComposer/Directory.Packages.props` — aligned Fluent version.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (create-story context engineering)

### Debug Log References

- Created from sprint-status backlog item `17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening`.
- Loaded: sprint-status, project-context, Epic 17 + Story 17.6 spec from `epics.md`, Stories 17.1–17.5 implementation artifacts, the FrontComposer/Fluent V5 sprint change proposal, UX spec / architecture conformance boundary, `hexalith-ux-instructions.md`, `Directory.Packages.props` pins, `.gitmodules`, and recent git history.
- Verified the current RCL state directly: all 19 `.razor` + 4 `.razor.css` files inventoried; legacy `*-rest`/FAST tokens and hardcoded hex confirmed present in `MemoriesEvidenceCockpit.razor.css` and `--neutral-stroke-rest` in the axis/source-stack `.razor.css` files; raw restrictive-state `<section role="alert">` confirmed in the cockpit `.razor`. Confirmed no conformance allowlist or source-scanning conformance test exists yet.
- No product code implemented in this create-story workflow.
- 2026-06-24: Dev workflow verification resumed with all task checkboxes already complete. Confirmed no Senior Developer Review follow-up section exists.
- 2026-06-24: `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-24: `DiffEngine_Disabled=true tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests -noLogo -noColor -parallel none` passed 460/460.
- 2026-06-24: `DiffEngine_Disabled=true tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests -noLogo -noColor -parallel none -class "*Epic17ConformanceTests"` passed 7/7.
- 2026-06-24: `git diff --check` passed.
- 2026-06-24: Exact VSTest command `DiffEngine_Disabled=true dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --no-build -m:1 /nr:false` still aborts in this sandbox with `SocketException (13): Permission denied`; used the story-prescribed xUnit v3 in-process runner for local test evidence.
- 2026-06-24 (Senior Developer Review): Rebuilt the test project (0 warnings / 0 errors, all four conformance files compile) and re-ran the in-process runner: full suite **475/475**, `Components/Validation` namespace **184/184**, `Epic17ConformanceHardeningTests` + `Epic17ConformanceRemediationTests` **15/15**. `git diff --check` clean.

### Completion Notes List

- Ready-for-dev story created on 2026-06-24.
- Scope: audit + classify every `.razor`/`.razor.css` in `Hexalith.Memories.Web`; remediate raw controls to FrontComposer/Fluent V5 and hand-authored CSS to Fluent 2 tokens/layout-only; establish a six-field conformance allowlist on the `Epic17ValidationInventory` fail-closed pattern; add reusable source-scanning conformance tests gating Stories 17.2–17.5; keep the existing `Epic17*` validation suite green.
- Presentation-conformance refactor only: no change to mapper outputs, view models, resource keys, Evidence Packet/recovery/lens/MCP semantics, or the host-less-RCL component-specimen posture. Browser/AT validation gaps remain deferred and fail-closed.
- Story 17.6 is ready for review: all tasks/subtasks are checked, the restrictive cockpit banner now uses `FluentMessageBar`, legacy/FAST tokens were removed from scoped CSS, the scope header uses Fluent typography/layout primitives, and the fail-closed source-scanning conformance register/tests cover the full RCL surface.
- Definition of Done: PASS. Build passed 0/0; the dev-story pass recorded the full web xUnit in-process suite at 460/460 and the focused conformance suite at 7/7; `git diff --check` passed. The local `dotnet test` VSTest runner remains sandbox-blocked as documented in Dev Notes.
- Post-dev QA-automation pass (`bmad-qa-generate-e2e-tests`) added `Epic17ConformanceHardeningTests` (5 tests) and `Epic17ConformanceRemediationTests` (10 cases), bringing the full suite to **475/475** (`Components/Validation` 184/184). The Senior Developer Review re-verified this final state and reconciled the File List / verification evidence below.

### File List

- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor.css`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor.css`
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs` (added by the QA-automation pass; reconciled into the File List by the Senior Developer Review)
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceRemediationTests.cs` (added by the QA-automation pass; reconciled into the File List by the Senior Developer Review)
- `_bmad-output/implementation-artifacts/17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-17-6-frontcomposer-fluent-conformance.md`

## Senior Developer Review (AI)

- **Reviewer:** Jérôme Piquot (story-automator adversarial review) on 2026-06-24
- **Outcome:** Approve — auto-fixes applied. 0 Critical, 0 High.
- **Scope reviewed:** the 5 modified RCL source files (`MemoriesEvidenceCockpit.razor`/`.css`, `MemoriesScopeHeader.razor`, `MemoriesRetrievalAxisBreakdown.razor.css`, `MemoriesSourceCitationStack.razor.css`) and all 4 conformance test files, cross-referenced against git reality and the seven acceptance criteria.

### Acceptance Criteria verdict

All seven ACs are implemented and test-backed: classification register (AC1) and six-field allowlist (AC4) are fail-closed in `Epic17ConformanceAllowlist`/`Epic17ConformanceTests`; the restrictive banner is now a `FluentMessageBar` with a verified Error/Warning intent mapping and the scope header uses `FluentLabel`/`FluentStack` (AC2, proven by `Epic17ConformanceRemediationTests`); legacy `*-rest`/FAST tokens and hex were removed and `--neutral-stroke-rest` → `--colorNeutralStroke1` (AC3); the whole RCL surface is gated so 17.2–17.5 cannot add unallowlisted exceptions (AC5); the pinned `5.0.0-rc.3-26138.1` is asserted (AC6); build is 0/0, full suite 475/475, `git diff --check` clean (AC7).

### Findings and resolution

- **[Medium][Fixed] File List out of sync with git reality.** The QA-automation pass added `Epic17ConformanceHardeningTests.cs` and `Epic17ConformanceRemediationTests.cs` (and the `test-summary-17-6-*.md` artifact) but they were never recorded in the Dev Agent Record File List. Added all three.
- **[Medium][Fixed] Stale verification evidence.** The dev-story Completion Notes claimed `460/460`, which predated the QA pass that brought the suite to `475/475`. Re-ran the suite independently (475/475 full, 184/184 Validation, 15/15 new) and corrected the recorded evidence.
- **[Low][Noted, not changed] Line-ending churn.** Four CRLF files were re-saved as LF, inflating the `MemoriesRetrievalAxisBreakdown.razor.css` / `MemoriesSourceCitationStack.razor.css` diffs to whole-file rewrites for a one-token change. Left as LF: the repo is LF-majority (Web RCL 99 LF / 23 CRLF) and the new test files are LF, so reverting to CRLF would oppose the dominant convention. `git diff --check` is clean. A future `.gitattributes` (out of scope here) would prevent the churn.
- **[Low][Noted] Nested live region.** The banner wraps `FluentMessageBar` in `<div role="alert" aria-live="assertive">`; `Epic17AccessibilitySweepTests` confirms no `alert`/`polite` conflict. Real screen-reader verification remains in the deferred, fail-closed `Epic17ValidationInventory.Gaps` register and is out of scope for this RCL.

No task marked `[x]` was found unimplemented, and no AC was missing. Status moved to **done**.

## Change Log

- 2026-06-24: Created ready-for-dev story for FrontComposer + Fluent UI Blazor V5 Conformance Hardening. Verified the current RCL violations directly (legacy `*-rest`/FAST tokens + hardcoded hex in cockpit CSS; `--neutral-stroke-rest` in axis/source-stack CSS; raw restrictive-state status banner in cockpit) and confirmed no conformance allowlist or source-scanning test exists yet.
- 2026-06-24: Completed conformance hardening and moved story to review. Added the fail-closed conformance allowlist/tests, replaced the cockpit restrictive banner with a Fluent message primitive, removed legacy/FAST CSS tokens, shifted scope-header typography/layout to Fluent primitives, and validated the web test suite.
- 2026-06-24: Senior Developer Review (adversarial, auto-fix). Re-verified build 0/0 and full suite 475/475; reconciled the File List with the two QA-added conformance test files and the test-summary artifact; corrected the stale 460/460 verification evidence. 0 Critical / 0 High — status moved to done.

## Story Completion Status

Done — Senior Developer Review passed (0 Critical, 0 High; 2 Medium documentation discrepancies auto-fixed). Full suite 475/475.
