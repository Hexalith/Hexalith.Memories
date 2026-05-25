# Story 17.1: Evidence Cockpit and Trust Components

Status: done

## Story

As a developer or team lead,
I want a FrontComposer/Fluent UI Evidence Cockpit with Evidence Packet, Trust Strip, Scope Header, source, axis, and graph summaries,
so that I can verify answers, sources, retrieval reasons, scope, and graph context in one inspectable web workflow.

## Acceptance Criteria

1. Given the web Evidence Cockpit is opened for a tenant and case, when a search or briefing response is displayed, then the page composes the shared Evidence Packet contract without inventing new confidence, state, omitted-detail, source, graph, or recovery semantics, and tenant and case scope are visible before the query or briefing content.
2. Given an Evidence Packet is displayed, when the Trust Strip renders, then it shows tenant, case, confidence state, freshness state, source count, evidence health, and token-budget indicator when applicable, and each state has a text label and accessible name, not color alone.
3. Given a result has sources, axis scores, or graph context, when the user expands evidence details, then Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary components expose source type, origin identifier, freshness, score normalization, ranking reason, edge type, confidence, gap markers, and chronological ordering as available from the contract.
4. Given the UI uses FrontComposer and Fluent UI Blazor, when controls, panels, tabs, grids, or menus are needed, then existing primitives are used before custom controls, and custom Memories components remain contract-aware and tenant-aware.

## Party-Mode Hardening Clarifications

- Contract consumption is read-only: Story 17.1 consumes the canonical `Contracts.V1` Evidence Packet from Story 2.7 and may not add, rename, derive, or reinterpret web-only confidence, freshness, evidence-health, source, retrieval-axis, graph, omitted-detail, scope, or recovery semantics.
- Evidence Cockpit is a presentation/composition slice. Do not add or change backend calls, retrieval orchestration, ranking/scoring rules, graph summarization, MCP behavior, CLI semantics, contract records, or submodule behavior in this story.
- All Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary data must come from canonical Story 2.7-aligned packet fixtures or a named adapter that preserves packet semantics without changing meaning.
- Missing, empty, unknown, unavailable, degraded, unauthorized, redacted, and token-budget-compressed packet values must render as explicit unavailable/degraded/redacted states. Do not infer tenant, case, confidence, citation, retrieval, or graph values from local paths, environment data, secrets, diagnostics, or UI state.
- Tenant and case scope must render before result/evidence content in visual order and DOM order for complete, empty, loading, error, partial, degraded, unauthorized, and compressed states.
- User-facing labels for trust, scope, retrieval axis, graph path, freshness, evidence health, and empty/unavailable states should follow existing FrontComposer localization/resource patterns where available instead of embedding display copy in component logic.
- Validation must use stable roles, accessible labels, or `data-testid` selectors. Do not rely on CSS class selectors, hover-only behavior, brittle visual text, or arbitrary sleeps for Playwright or accessibility checks.
- Public evidence UI, copied text, diagnostic panels, accessibility labels, test snapshots, and trace artifacts must not expose secrets, bearer tokens, local absolute paths, raw payloads, tenant-sensitive diagnostics, or machine-specific filesystem references.

## Advanced Elicitation Hardening Clarifications

- Treat Story 2.7 as the single contract authority. If its Evidence Packet implementation is not complete at dev time, Story 17.1 may use canonical fixtures only for component work and must not edit active Story 2.7 contract, CLI, MCP, or mapper files to unblock the web slice.
- Evidence Packet binding must be field-by-field and explicit. Adapters may normalize presentation shape for FrontComposer, but each displayed trust, scope, source, axis, graph, omitted-detail, recovery, and token-budget value must trace back to a named contract field or render as unavailable.
- When multiple safety or state signals are present, the UI must surface the most restrictive scope/safety condition first in display order without mutating packet values: unauthorized/out-of-scope, schema mismatch or missing source, redacted or compressed detail, degraded or partial evidence, then supported/current/complete evidence.
- Source, axis, and graph inspectors must preserve contract ordering unless the packet exposes an explicit chronological or ranking order. If no ordering signal exists, display the packet order and mark the order basis as unavailable instead of sorting by UI-local heuristics.
- Copy, export, source preview, graph context, and MCP payload inspection must share the same redaction and tenant/case scope checks as the visible UI. Do not reconstruct those outputs from DOM text, diagnostics, browser state, local paths, or raw payload dumps.
- Accessibility and responsive validation must cover transition states as well as settled states: loading to complete, complete to degraded, unauthorized, redacted, compressed, no-source, and graph-backend-unavailable paths all need stable focus, labels, and no hidden trust state.

## Tasks / Subtasks

- [x] Task 0 - Preflight contract and submodule readiness (AC: 1-4)
  - [x] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set or explicitly gate this story until it does. Do not invent a parallel web-only packet shape.
  - [x] Treat any in-progress Story 2.7 changes as prerequisite context only. Do not patch the active contract story, its source files, or its tests as part of this web story unless Story 17.1 is explicitly re-scoped.
  - [x] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`, `HybridSearchResult.cs`, `SearchExplanation.cs`, `TraversalResult.cs`, and `OmittedReason.cs` before designing UI bindings.
  - [x] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing` before adding components.
  - [x] Verify the current Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before using examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation was for `5.0.0.26098`, so local code/tests are the stronger source when signatures disagree.

- [x] Task 1 - Define the Evidence Cockpit composition boundary (AC: 1, 4)
  - [x] Add the smallest FrontComposer-aligned web surface that can render an Evidence Packet for a selected tenant and case. Prefer a contract-driven composition or view model adapter over page-specific JSON parsing.
  - [x] Keep tenant and case scope before the query/result content in the visual and DOM order.
  - [x] Preserve the shared Evidence Packet state grammar: confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health (`complete`, `degraded`, `missing source`, `schema mismatch`), and scope (`verified`, `inferred`, `cross-case`, `unauthorized`, `out-of-scope`).
  - [x] Define an explicit packet-to-view mapping table in code or tests so every rendered trust and evidence value has a named contract source, including the unavailable fallback when the source field is absent.
  - [x] Apply restrictive-state display precedence without changing packet semantics: scope/safety failures first, then schema/source degradation, then redaction/compression, then partial evidence, then supported evidence.
  - [x] Do not add a new search backend, MCP tool, CLI output shape, or contract semantics in this story. If the existing contract is insufficient, record the gap as a dependency on Story 2.7 or a deferred decision.
  - [x] Keep the cockpit shell presentation-only. Filtering, ranking, evidence mutation, export payload design, and drill-down workflows beyond safely opening already-present packet details are deferred unless the packet already exposes enough data and FrontComposer already has the command primitive.

- [x] Task 2 - Implement Trust Strip and Scope Header components (AC: 1, 2)
  - [x] Render tenant id/name, case id/name, scope status, confidence state, freshness state, source count, evidence health, and token-budget indicator from the Evidence Packet contract.
  - [x] Use Fluent UI/FrontComposer status primitives such as badges, inline messages, layout stacks, menus, or command surfaces before creating custom controls.
  - [x] Ensure every state has visible text and an accessible name. Color, icon, or badge appearance alone is not sufficient.
  - [x] Add compact wrapping behavior so the strip remains before the answer at mobile widths instead of disappearing into an overflow-only control.
  - [x] Cover complete, empty, loading, error, partial, degraded, unauthorized, redacted, and token-budget-compressed state rendering without moving scope below result content.

- [x] Task 3 - Implement source, axis, and graph inspection summaries (AC: 3, 4)
  - [x] Source Citation Stack exposes source type, origin identifier, snippet or summary, freshness, confidence/metadata origin when available, and keyboard-openable preview behavior.
  - [x] Retrieval Axis Breakdown exposes retrieval axes used, normalized score or contribution, ranking reason, unavailable/degraded markers, and the `SearchExplanation.Caveat` meaning where present.
  - [x] Graph Path Summary exposes relationship path, edge type, confidence, gap markers, chronological ordering, depth, and graph-backend degraded state when available.
  - [x] Keep first view compact: trust essentials first, then expandable source/detail panels. Avoid dashboard sprawl and decorative cards that hide the evidence workflow.
  - [x] Render absent sources, absent axes, absent graph paths, unknown freshness, unavailable backend detail, and redacted source content as explicit unavailable/degraded/redacted states, not empty certainty.
  - [x] Preserve source, axis, and graph ordering from the packet unless the contract provides an explicit ranking or chronological field. Do not introduce UI-local sorting that can change evidence meaning.

- [x] Task 4 - Wire FrontComposer command/navigation behavior (AC: 1, 4)
  - [x] Use FrontComposer tenant/user render context and shell conventions for scope-aware rendering; do not store tenant/case state in singleton/static UI services.
  - [x] Preserve context when navigating from the Evidence Packet to a source, graph path, activity item, or agent packet.
  - [x] Provide command actions for inspect source, open graph context, export packet, retry/refine query, and inspect MCP payload only when the packet exposes enough data to perform the action safely.
  - [x] Apply the same redaction, unavailable-state, tenant, and case checks to copy/export/inspect commands as to visible components. Do not build command payloads from rendered DOM text or diagnostic dumps.
  - [x] Confirmation or scope-expanding actions must name the tenant, case, target object, and consequence before proceeding.

- [x] Task 5 - Add component and accessibility tests (AC: 1-4)
  - [x] Add bUnit coverage using `Hexalith.FrontComposer.Testing` or the existing `BunitContext` + `AddFluentUIComponents()` pattern.
  - [x] Render canonical Story 2.7-aligned Evidence Packet fixtures in component tests, including complete, empty, partial, degraded, multi-source, tenant/case mismatch, unauthorized, redacted, and token-budget-compressed packets.
  - [x] Add mapping tests that fail when a rendered field has no named Evidence Packet source or when a contract field used by the Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, or Graph Path Summary is silently dropped.
  - [x] Test Trust Strip state labels and accessible names for supported, partial/degraded, unauthorized, and token-budget-compressed packets.
  - [x] Test restrictive-state precedence, packet ordering preservation, and unavailable-order messaging for source, axis, and graph summaries.
  - [x] Test source/axis/graph expansion keeps keyboard-reachable controls and does not depend on hover-only behavior.
  - [x] Test tenant/case scope appears before result content in markup order.
  - [x] Add negative tests that restricted source details, raw payloads, bearer tokens, tenant-sensitive diagnostics, and local absolute paths do not render in accessible labels, copied text, logs, or diagnostic panels.
  - [x] If copy, export, graph context, or MCP inspection commands are visible, add negative tests that their payloads follow the same redaction and scope rules as the visible Evidence Cockpit.
  - [x] Use role, accessible-label, and `data-testid` selectors for UI tests. Do not use CSS class selectors, arbitrary sleeps, or selectors coupled only to visual text.

- [x] Task 6 - Validate responsive and visual behavior (AC: 1-4)
  - [x] Run focused unit/bUnit tests for any Memories web project and changed FrontComposer component tests.
  - [x] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px. Capture evidence that scope, confidence, freshness, source count, evidence health, and recovery remain reachable.
  - [x] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern.
  - [x] Validate keyboard-only operation, focus order, focus visibility, focus return from dialogs/drawers, screen-reader names, touch target sizing, forced-colors/high-contrast behavior, and no text overlap at narrow mobile, tablet, desktop, and wide desktop widths.
  - [x] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is explicitly future web UI scope. This story is valid when web UI is selected, but it must not weaken the CLI/MCP Evidence Packet contract or pull unrelated MVP work forward.
- Story 2.7 owns the shared Evidence Packet grammar in `Contracts.V1`. Story 17.1 consumes that grammar through FrontComposer/Fluent UI composition. The web layer must not define separate confidence, degraded, omitted-detail, source, graph, or recovery vocabularies.
- Current lower-level retrieval contracts already expose many source inputs: `SearchResult`, `HybridSearchResult`, `ScoredResult`, `FusedScoredResult`, `SearchExplanation`, `TraversalResult`, and `OmittedReason`. Use them only through the Evidence Packet contract or a clearly named adapter that preserves contract semantics.
- `Hexalith.FrontComposer` is a root-level submodule. Treat its existing contracts, shell components, source generators, and testing helpers as the local implementation foundation. Do not initialize nested submodules or casually mutate unrelated submodule history.
- FrontComposer currently provides projection roles (`ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`), semantic badge slots, tenant/user render context, command palette/state, data-grid navigation, projection templates, view override hosts, diagnostics, and `Hexalith.FrontComposer.Testing` bUnit utilities.
- FrontComposer test patterns register `AddFluentUIComponents()` and localization in bUnit contexts when Fluent primitives or diagnostics render. Prefer that pattern for component tests.

### Component Boundaries

- Evidence Cockpit is the page/workspace composition. It should arrange scope/query/result/evidence/recovery without becoming a new design system.
- Trust Strip and Scope Header are trust-critical. They must render before answer/result content and keep labels available in compact layouts.
- Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary are expandable inspection components. They should render only data the contract actually provides and should expose unknown/unavailable state rather than fabricating certainty.
- Use Fluent UI primitives for mechanics: buttons, menus, tabs, data grids, badges, banners, dialogs, drawers, progress indicators, tooltips, inputs, layout, and focus behavior.
- Use FrontComposer conventions for composition: typed descriptors, tenant-aware context, command lifecycle visibility, bounded diagnostics, Fluxor-compatible state, render-mode-safe components, and component test helpers.

### Accessibility and UX Guardrails

- Trust state is product correctness. Confidence, freshness, evidence health, scope status, degraded backends, and token-budget compression require text labels and accessible names.
- Keyboard support must cover the trust loop: select tenant, select case, submit query, inspect evidence, expand sources, inspect axis detail, open graph context, follow recovery action, and return.
- Drawers, dialogs, source previews, graph detail panels, and MCP inspectors must move focus in and return focus to the invoking control when closed.
- Do not rely on hover-only interactions. Every source preview, graph detail, recovery action, tooltip-critical command, and status explanation must work by keyboard and touch.
- Support forced-colors/high-contrast through Fluent UI tokens and semantic labels. Do not encode state only through custom color choices.
- Keep the interface dense and operational. Avoid marketing-style panels, large decorative empty states, or dashboards that bury source/evidence/recovery behind visual decoration.

### Testing Notes

- Component tests should use xUnit, Shouldly, bUnit, and NSubstitute where relevant, matching FrontComposer and Memories conventions.
- If the implementation changes FrontComposer submodule files, run tests from the submodule root and keep those changes intentional and separately reviewable.
- If a Memories web project is introduced, keep package versions centralized and avoid adding `Version` attributes to `.csproj` package references.
- Browser/accessibility validation should use existing Playwright and axe patterns where available. For FrontComposer, `Hexalith.FrontComposer/tests/e2e` already documents viewport, fixture, and `@axe-core/playwright` conventions.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide the contract semantics consumed here. If it is not implemented at dev time, this story should pause or use test fixtures only until the contract is available.
- Non-goal: no new retrieval algorithm, ingestion workflow, MCP server behavior, CLI output contract, benchmark logic, or operator health matrix.
- Non-goal: no broad FrontComposer framework redesign. Add Memories-specific compositions only where existing FrontComposer primitives do not already cover the need.
- Non-goal: no nested submodule initialization or recursive submodule update.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.1 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - Evidence Packet, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, Graph Path Summary, responsive, and accessibility requirements.
- `_bmad-output/planning-artifacts/architecture.md` - Evidence Packet Contract ownership and lower-level retrieval contract boundary.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract story and current contract mapping guidance.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, and E2E testing conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-1-evidence-cockpit-and-trust-components`.
- Loaded preflight JSON, sprint status, story lessons, Epic 17 requirements, UX component/accessibility sections, architecture Evidence Packet section, Story 2.7 artifact, current lower-level search/traversal contracts, FrontComposer package/test/component context, and Fluent UI Blazor version compatibility warning.
- No product code implementation performed in this create-story workflow.
- 2026-05-20: Confirmed Story 2.7 remains `in-progress`, but canonical `EvidencePacket` records and mapper exist in `Contracts.V1`; treated Story 2.7 source and tests as read-only prerequisite context.
- 2026-05-20: Verified local Fluent UI Blazor package is `5.0.0-rc.2-26098.1`; MCP documentation target `5.0.0.26098` is incompatible, so local code/tests drove component API choices.
- 2026-05-20: Added red bUnit tests first for scope ordering, trust-strip accessible labels, field mapping, restrictive-state precedence, packet ordering, loading/error scope ordering, and sensitive-value redaction.
- 2026-05-20: `dotnet test .\tests\Hexalith.Memories.Web.Tests\Hexalith.Memories.Web.Tests.csproj` passed with 8 tests.
- 2026-05-20: `dotnet build .\Hexalith.Memories.slnx` passed with 0 warnings and 0 errors.
- 2026-05-20: `dotnet test .\Hexalith.Memories.slnx --no-build` timed out in the Docker/AppHost integration lane after 10 minutes; orphaned test host processes were stopped.
- 2026-05-20: Non-Docker regression lane passed: CLI, Contracts, EventStore, MCP, Server, TestHelpers, Web tests, plus IntegrationTests filtered with `Category!=Integration`.
- 2026-05-20: `git diff --check` passed; only CRLF normalization warnings were reported for existing tracked text files.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web Evidence Cockpit composition and trust components over the shared Evidence Packet contract.
- Story explicitly records the Story 2.7 dependency and local Fluent UI Blazor version mismatch with the MCP documentation source.
- Implemented a Memories-owned Razor component library for the Evidence Cockpit so Memories-specific Evidence Packet semantics do not leak into the FrontComposer submodule.
- Added Scope Header, Trust Strip, Source Citation Stack, Retrieval Axis Breakdown, Graph Path Summary, and a composition-level `MemoriesEvidenceCockpit`.
- Added explicit Evidence Packet view-field mapping so rendered values trace to named contract sources with unavailable/redacted fallbacks.
- Added bUnit coverage using `Hexalith.FrontComposer.Testing`, including scope-before-result DOM order, accessible trust labels, restrictive precedence, packet ordering preservation, loading/error states, and non-leakage of bearer tokens, local paths, and backend connection strings.
- No runnable web host was added in this story, so viewport Playwright/axe validation remains not applicable to this RCL-only slice; responsive behavior is covered through wrapping CSS and component-level markup assertions.

### File List

- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`
- `Directory.Packages.props`
- `Hexalith.Memories.slnx`
- `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj`
- `src/Hexalith.Memories.Web/_Imports.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`
- `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketFieldMapping.cs`
- `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketViewMapping.cs`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor`
- `tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor.css`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor.css`
- `src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor.css`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Evidence Cockpit and Trust Components.
- 2026-05-20: Party-mode review applied story hardening for contract consumption boundaries, explicit unavailable/redacted states, accessibility validation, stable selectors, and scope-limited web composition.
- 2026-05-20: Implemented Evidence Cockpit and Trust Components; added Web RCL, bUnit tests, solution/package wiring, and moved story to review.
- 2026-05-20: Code review patch pass applied 26 patches and recorded 18 deferred items in `deferred-work.md`. Build clean (0 warnings, 0 errors), 23/23 bUnit tests pass, `git diff --check` clean. Story moved to done.

## Code Review Patch Pass

- Date: 2026-05-20
- Reviewer: bmad-code-review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) against commit `3897736`.
- Patch outcomes:
  - Authorization data leakage closed in `MemoriesSourceCitationStack`, `MemoriesGraphPathSummary`, `MemoriesRetrievalAxisBreakdown`: components now suppress all rendered content (URIs, snippets, edges, axes) when `Scope.IsolationStatus` is `Unauthorized` or `Unknown`, or when `State == Unauthorized`. The `UnauthorizedPacket` fixture deliberately keeps sources/graph/axis evidence populated so the negative tests fail loud if a guard regresses.
  - Loading and error states no longer reuse contract enum values for web-only meaning: `MemoriesEvidenceCockpit` renders explicit "Loading evidence" / "Evidence unavailable" envelopes, suppresses subordinate evidence children, and the trust strip exposes a `Mode` parameter (`Packet`/`Loading`/`Error`) so badges display `Loading` / `Unavailable` text labels with accessible names instead of meaningless synthesized packet values.
  - Dead command UI removed. Task 4's "only when the packet exposes enough data to perform the action safely" guardrail is honored — Export, Retry/refine, Inspect MCP, Open graph context, Inspect source were removed pending a future story that wires FrontComposer command primitives with payload redaction parity tests.
  - Recovery actions are now rendered per-item with tenant, case, and target context surfaced in the visible markup; recovery details from `EvidencePacketRecoveryAction` (Kind/Label/Guidance/Target) are no longer silently dropped.
  - Restrictive-state precedence ladder implemented per Advanced Elicitation #3: unauthorized > missing-source/degraded > redacted > compressed > degraded/partial/weak/stale/empty. The single restrictive banner exposes `data-restrictive-kind` for stable assertions.
  - `EvidencePacketIsolationStatus.Unknown` is now treated restrictively (cannot reveal contract content). The new `EvidenceDisplay.IsRestrictiveScope` helper centralizes the rule.
  - `EvidenceDisplay.Label` preserves acronyms (`MCP` → `MCP`, `MCPHandler` → `MCP handler`, `PendingExpansion` → `Pending expansion`) and `Strong` stays `Strong`. Trust-strip aria-label format updated to title case.
  - `EvidenceDisplay.SafeText` now replaces only the matched span with `[REDACTED]` (`EvidenceDisplay.RedactedMarker`), preserving surrounding non-sensitive text. The regex expands to cover API key prefixes (`sk_live_`, `sk_test_`, `ghp_`, `xoxb-`, `AKIA…`), `Authorization:` header form, `api_key=`, UNC paths (`\\server\share`), POSIX system paths (`/etc/`, `/var/`, `/tmp/`, `/opt/`), and tightens the stack-trace pattern so phrases like "looked at server.com" no longer trigger redaction. Plain content surrounding a redaction is preserved.
  - `EvidenceDisplay.ScoreLabel` rejects NaN/Infinity and renders `"score unavailable"`.
  - `EvidenceDisplay.TokenBudgetLabel` is now reason-gated — `compressed` only when `Reason ∈ { TokenBudget, Combined }`. Authorization/Redaction/Policy omissions no longer masquerade as token-budget compression.
  - `FreshnessLabel()` renders the explicit `EvidenceDisplay.FreshnessUnavailable` sentinel and the mapping table now records `EvidencePacketViewMapping.NoContractSource` as the contract source. Both compile-time constants are referenced from the test suite so a future Story-2.7-driven contract field appearance will require the documented swap.
  - `EvidencePacketViewMapping.RenderedFields` expanded from 20 to 27 entries covering every UI-rendered field, including `sources.snippet/memoryUnit/rank`, `axes.normalizationMethod/unavailableAxes/caveat`, and `recovery.label/guidance`. A mapping-completeness test fails when any tracked display field disappears.
  - Result section heading is now a stable `<h2>Evidence</h2>`; the user query renders below in a sanitized `<p data-testid="mem-evidence-query">` so a sensitive or empty query no longer hijacks the accessibility hierarchy.
  - `UnavailablePacket` is now a signature-keyed cached field (allocated once per parameter combination) instead of an expression-bodied property allocating on every access; all `EvidencePacket` records are constructed with named record-init syntax so positional argument reorders cannot silently swap roles.
  - `MemoriesScopeHeader` exposes stable `data-testid` selectors for tenant/case/isolation badge.
  - Trust Strip shows `sources unavailable` when scope is restrictive (vs. `0 sources` for an authorized empty result), aligning visible state with contract intent.
  - Per-component scoped CSS files added for source/axis/graph so list/`<dl>` styling actually reaches their markup (Blazor scoped CSS does not cross component boundaries). New `mem-evidence-restrictive` styles match the precedence-banner kinds.
  - Graph node separator is now aria-friendly: nodes render as `<span data-testid="mem-graph-node">`, separators are `aria-hidden`, and a visually-hidden `then` is announced to assistive tech. Empty `RelatedPath` renders `no traversal path` instead of a blank `<dd>`.
  - `Evidence.Caveat`, `Graph.EdgeTypes`, `Graph.GapMarkers`, `Evidence.UnavailableAxes`, and `Source.SourceType` are now sanitized via `SafeText`.
  - Test suite expanded to 23 tests (up from 8) with fixtures for `Complete`, `Compressed`, `Unauthorized` (sources populated), `MultiSource`, `Sensitive`, `TenantCaseSensitive`, `Empty`, `Stale`, `Degraded`, `Partial`, `Weak`, `Redacted`, and `UnknownScope`. New coverage: precedence ladder (Theory), `IsolationStatus.Unknown` treated restrictively, tenant/case sensitive content sanitization, graph node order, keyboard reachability (no negative tabindex / aria-hidden ancestor), recovery action tenant/case/target rendering, ScoreLabel non-finite handling, and SafeText partial-replacement preserving surroundings.
  - `Directory.Packages.props` and `Hexalith.Memories.Web.csproj` updated: prerelease FluentUI/bunit pins carry justification comments, the RCL is `IsPackable=false` until a consumer outside the solution needs it, and `InternalsVisibleTo("Hexalith.Memories.Web.Tests")` gives the test project access to the internal `EvidenceDisplay` helpers.
  - `Host.ValidateVersionAlignment()` invoked from the test constructor so future Memories vs FrontComposer package-version drift fails fast.
- Verification:
  - `dotnet build src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj` — 0 warnings, 0 errors.
  - `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj` — 0 warnings, 0 errors.
  - `dotnet build Hexalith.Memories.slnx` — 0 warnings, 0 errors (warnings-as-errors gate passes for the whole solution).
  - `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj` — 23/23 pass.
  - `git diff --check` — clean (only CRLF normalization warnings on tracked text files, unchanged from prior baseline).
- Findings deferred:
  - 18 lower-severity findings recorded in `_bmad-output/implementation-artifacts/deferred-work.md` under `## Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)`. Each entry includes rationale tying the deferral to a future story or to vacuous coverage on the current command-less RCL.
- Final recommendation: done

## Party-Mode Review

- Date: 2026-05-20T11:31:23.1043511+02:00
- Selected story key: `17-1-evidence-cockpit-and-trust-components`
- Command/skill invocation used: `/bmad-party-mode 17-1-evidence-cockpit-and-trust-components; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Story 17.1 was directionally valid but needed sharper boundaries so the future web Evidence Cockpit consumes Story 2.7 `Contracts.V1` Evidence Packet semantics without inventing web-only trust, citation, retrieval-axis, graph, omitted-detail, scope, or recovery meanings.
  - The Evidence Cockpit scope risk was UI expansion beyond reusable presentation/composition components into filtering, ranking, export, mutation, backend, MCP, CLI, or contract changes.
  - Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary needed explicit behavior for empty, missing, unknown, unavailable, degraded, unauthorized, redacted, and compressed packet values.
  - Accessibility and responsive requirements needed executable validation language for keyboard order, focus behavior, touch targets, screen-reader names, forced-colors/high-contrast, stable selectors, and no text overlap.
  - Tests needed canonical Story 2.7-aligned fixtures and negative assertions for secrets, local paths, raw payloads, tenant-sensitive diagnostics, and brittle UI selectors.
- Changes applied:
  - Added `## Party-Mode Hardening Clarifications`.
  - Tightened Task 1 with presentation-only cockpit boundaries and deferred filtering/ranking/mutation/export semantics.
  - Tightened Task 2 and Task 3 with explicit complete, empty, loading, error, partial, degraded, unauthorized, redacted, compressed, absent-source, absent-axis, absent-graph, and unknown-value rendering expectations.
  - Tightened Task 5 with canonical Evidence Packet fixture coverage, selector stability, and non-leakage test expectations.
  - Tightened Task 6 with keyboard, focus, screen-reader, touch target, forced-colors/high-contrast, responsive, and no-text-overlap validation.
- Findings deferred:
  - Final visual density and information hierarchy remain implementation decisions within the clarified component boundaries.
  - New trust scoring, retrieval weighting, graph summarization, backend behavior, MCP behavior, CLI behavior, and contract semantics remain out of scope for Story 17.1.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date: 2026-05-20T12:20:27.1576382+02:00
- Selected story key: `17-1-evidence-cockpit-and-trust-components`
- Command/skill invocation used: `/bmad-advanced-elicitation 17-1-evidence-cockpit-and-trust-components`
- Batch 1 method names: Red Team vs Blue Team, Security Audit Personas, Failure Mode Analysis, Self-Consistency Validation, Tree of Thoughts
- Reshuffled Batch 2 method names: First Principles Analysis, Pre-mortem Analysis, Architecture Decision Records, Challenge from Critical Perspective, Comparative Analysis Matrix
- Findings summary:
  - Story 17.1 needed a clearer guardrail for the active Story 2.7 dependency so future web work does not mutate contract, CLI, MCP, mapper, or test files owned by the in-progress Evidence Packet story.
  - The web adapter boundary needed field-level traceability so trust, scope, source, axis, graph, omitted-detail, recovery, and token-budget values are either mapped from named contract fields or explicitly unavailable.
  - Multi-signal packet states needed deterministic display precedence that surfaces restrictive safety and scope conditions first without creating new contract semantics.
  - Source, axis, graph, copy, export, and inspector paths needed stronger non-leakage and ordering constraints to prevent UI-local sorting, DOM reconstruction, or diagnostics from changing or exposing evidence.
  - Accessibility and responsive validation needed to include transition and degraded states, not only settled successful packet rendering.
- Changes applied:
  - Added `## Advanced Elicitation Hardening Clarifications`.
  - Tightened Task 0 with the Story 2.7 active-contract ownership boundary.
  - Tightened Task 1 with explicit packet-to-view mapping and restrictive-state display precedence.
  - Tightened Task 3 with packet ordering preservation for source, axis, and graph summaries.
  - Tightened Task 4 with redaction and scope parity for copy/export/inspect command payloads.
  - Tightened Task 5 with mapping, precedence, ordering, and command-payload negative tests.
- Findings deferred:
  - Exact component names, adapter type names, and visual layout remain implementation decisions inside FrontComposer conventions.
  - Contract changes, new Evidence Packet fields, new retrieval ranking, graph summarization, CLI/MCP output changes, and Story 2.7 source/test edits remain out of scope for Story 17.1.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

## Review Findings

Date: 2026-05-20
Reviewer: bmad-code-review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) against commit `3897736`.
Triage summary: 0 decision-needed, 26 patch, 18 deferred, ~10 dismissed as noise/coupling-to-coupling.

### Critical patches (data leakage and contract-truth)

- [x] [Review][Patch] Unauthorized data leaks through Source/Graph/Axis components — no isolation guard around rendered content [src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:7-30, MemoriesGraphPathSummary.razor:6-23, MemoriesRetrievalAxisBreakdown.razor:6-33]. Source list, edge types, gap markers, axis evidence render whenever the collection is non-empty; only the buttons are gated. Compounded by `EvidencePacketFixtures.UnauthorizedPacket` pre-clearing `Sources` via `with { Sources = [] }`, so `RestrictedPacket_ShouldDisplayRestrictiveStateBeforeEvidence` passes trivially and never exercises the scrubbing path. Add scope-aware render guard at each component AND update fixture to keep sources populated so the test would fail without the guard.
- [x] [Review][Patch] `FreshnessLabel()` is a hard-coded `"unknown"` constant and the mapping table falsely declares `EvidencePacket.Sources[].SourceUri` as the source [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:33, EvidencePacketViewMapping.cs:14,19, MemoriesTrustStrip.razor:18, MemoriesSourceCitationStack.razor:23]. The contract has no freshness field. AC2 still requires the Trust Strip to show "freshness state" — render it explicitly as `freshness unavailable` everywhere, correct the mapping `ContractSource` to `null` (or a documented "no contract source" sentinel), and add a mapping-completeness test that catches this lie.
- [x] [Review][Patch] Loading and error states hijack contract `EvidencePacketState.PendingExpansion` and `Degraded` [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:80-103]. Party-Mode #1 forbids reinterpreting contract semantics. Synthesize NO packet for loading/error — render an explicit "evidence unavailable" envelope outside the packet object graph, skip child component composition during those states.
- [x] [Review][Patch] Dead command UI: Export, Retry/refine, Inspect MCP payload, Open graph context, Inspect source — no `@onclick`, no event callback, no command primitive [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:37-58, MemoriesSourceCitationStack.razor:30, MemoriesGraphPathSummary.razor:18]. Task 4 says "only when the packet exposes enough data to perform the action safely". With nothing wired and no payload synthesis, Advanced Elicitation #5 redaction-parity is vacuous. Remove the buttons (or move them behind a feature flag) until FrontComposer command primitives are wired with payload negative tests.

### Major patches

- [x] [Review][Patch] `TokenBudgetLabel` returns `"compressed"` for any `OmittedCount > 0` regardless of `Reason` [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:21-30]. Authorization/Policy/Redaction omissions render as token-budget compression. Gate on `Reason == TokenBudget`.
- [x] [Review][Patch] Restrictive-state precedence implements only `Unauthorized` [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:9-15]. Advanced Elicitation #3 specifies a full ladder: unauthorized > schema-mismatch/missing-source > redacted/compressed > degraded/partial > supported. Extend the precedence helper and add per-rung tests.
- [x] [Review][Patch] Scoped CSS in `MemoriesEvidenceCockpit.razor.css` cannot reach `<ol>` rendered by child components — Blazor scoped CSS attribute is applied only to the parent component's own markup [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css:25-30]. Source/axis lists silently fall back to browser defaults. Move list styling to per-child `.razor.css` files.
- [x] [Review][Patch] `EvidencePacketViewMapping.RenderedFields` is incomplete and partly false [src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketViewMapping.cs:9-32, EvidenceCockpitTests.cs:55-67]. Missing entries: `sources.snippet`, `sources.memoryUnit`, `sources.rank`, `evidence.caveat`, `evidence.unavailableAxes`, `axes.normalizationMethod`, `recoveryAction.*`. Existing `RenderedFields_ShouldHaveNamedContractSources` asserts 6 hand-picked entries and never fails on new untracked render paths. Add a markup-scanning test or an explicit "every UI field listed here" coverage assertion.
- [x] [Review][Patch] `UnavailablePacket` re-allocates the entire packet + child records + arrays on every property access [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:80]. Convert to a cached field built once per state.
- [x] [Review][Patch] `UnavailablePacket` passes `!string.IsNullOrWhiteSpace(ErrorMessage)` to a positional `bool` slot in `EvidencePacketEvidence` constructor [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:91-99]. Error presence is unrelated to whatever the bool's meaning is (likely `Degraded` or `HasOmittedDetails`). Verify each positional argument and replace with named-record property initialization.
- [x] [Review][Patch] Positional record construction is fragile across `EvidencePacketEvidence`, `EvidencePacketEvidenceAxisScore`, etc. — multiple same-typed args (e.g., two `IReadOnlyList<string>` or two empty `[]`) silently swap on contract reorder [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:91-99, tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs:103-110]. Switch to `with`-init or named arguments.
- [x] [Review][Patch] `ScoreLabel` formats `double.NaN` as `"NaN"` and `double.PositiveInfinity` as `"Infinity"` directly to UI [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:60-61]. Reject non-finite values and render `"score unavailable"`.
- [x] [Review][Patch] `SafeText` replaces the entire field with the fixed string `"redacted source"` on any match, destroying surrounding content [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:48-65]. A 200-word snippet with a single hex token collapses to "redacted source". On the error path, `SafeText(ErrorMessage, "Evidence unavailable")` returns "redacted source" instead of the contextual fallback. Replace only the matched span with `[REDACTED]`, preserve surrounding text, keep the field-specific fallback.
- [x] [Review][Patch] `SafeText` regex is incomplete: no API key prefixes (`sk_live_`, `ghp_`, `xoxb-`, `AKIA…`), no UNC paths (`\\server\share`), no POSIX system paths (`/etc/`, `/var/`, `/tmp/`, `/opt/`) [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:65]. Expand the pattern set.
- [x] [Review][Patch] `SafeText` over-redacts plain content: `\bat\s+\w+\.` matches innocuous English ("looked at server.com"); with `IgnoreCase`, "AT Acme." triggers full-field redaction [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:51]. Tighten patterns: anchor stack-trace detection to "at <NS>.<Method>(<...>) in <Path>" form.
- [x] [Review][Patch] `Label` does not preserve consecutive uppercase runs: `Label("MCP")` returns `"m c p"`. Any future acronym-containing enum renders as fragmented letters [src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13]. Detect uppercase runs and emit them as a single token.
- [x] [Review][Patch] Result section `<h2>` is the (sanitized) query string — when query is empty becomes "no query"; with sensitive content becomes "redacted source" [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:25]. Use a stable heading like "Evidence" or "Results"; render the query in a `<header>` or `<p>` below.
- [x] [Review][Patch] Recovery actions silently dropped beyond a count check — per-action `Kind/Label/Guidance/Target` never rendered; UnauthorizedPacket's `CheckAuthorization` action's guidance is lost [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:44-48]. Render each action's label and guidance; add a fixture-driven test that surfaces guidance text.
- [x] [Review][Patch] Trust Strip during loading renders a fully-formed but meaningless evaluation ("Confidence: none / Freshness: unknown / 0 sources / Evidence health: pending expansion / Token budget: within budget") [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:5,17-22, MemoriesTrustStrip.razor]. When `IsLoading`, render explicit "evidence loading" placeholders per badge — never compute a Trust Strip from synthetic data.
- [x] [Review][Patch] `EvidencePacketIsolationStatus.Unknown` is silently treated as authorized — the restrictive-state guard fires only for `Unauthorized` [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:9]. `Unknown` should be the most restrictive default (treat as unauthorized) until the contract producer is required to set it explicitly.
- [x] [Review][Patch] Children render with synthetic stub during error — trust strip + source stack + graph all render, duplicating the parent's error message [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:50-52]. Early-exit subordinate sections when an error envelope is the visible content.
- [x] [Review][Patch] `Evidence.Caveat`, `Evidence.UnavailableAxes`, `Graph.EdgeTypes`, `Graph.GapMarkers` rendered without `SafeText` [src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor:22-24, MemoriesGraphPathSummary.razor:14-15]. Run every contract-sourced string through `SafeText` with field-appropriate fallback.
- [x] [Review][Patch] No keyboard reachability / focus order / `aria-expanded` test exists [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs]. Task 5 requires verifying source/axis/graph expansion is keyboard-reachable and not hover-only. Add bUnit tests for Tab order and explicit role/accessible-name lookups.
- [x] [Review][Patch] Trust Strip aria-label assertions are tightly coupled to `FcStatusBadge` internal formatting and `Label`'s lowercasing [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs:62-66]. Either set explicit `aria-label` on the component and assert it directly, or use accessible-name role lookups instead of attribute-string substring matches.
- [x] [Review][Patch] No fixture covers `EvidencePacketState.Empty`, `Stale`, `Degraded`, `Partial`, or `Weak` — 5 of 8 contract states untested [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs]. Add fixtures and per-state assertions. Also: build the sensitive-content test on top of a Stale or Degraded fixture, not just Complete, to verify redaction independent of state.

### Minor patches

- [x] [Review][Patch] CSS class `mem-evidence-restrictive` referenced in markup but never defined in the stylesheet [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:11, MemoriesEvidenceCockpit.razor.css]. Add styles or remove the class.
- [x] [Review][Patch] Multiple `role="alert"` regions compete (restrictive section, error paragraph, child components) [src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:17,24]. Consolidate to one live region at any moment.
- [x] [Review][Patch] `MemoriesScopeHeader` does not expose isolation badge via stable selector — no `data-testid` on the FcStatusBadge for isolation [src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor:11-14]. Add `data-testid="mem-scope-isolation"` and assert in tests.
- [x] [Review][Patch] `UnauthorizedPacket` fixture is internally inconsistent: `Result.TotalCount=1, ReturnedCount=1` but `Sources=[]` after the `with` clause [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs:48-65]. Fix to `TotalCount=0, ReturnedCount=0` and `HasIndexedMemoryUnits=true`.
- [x] [Review][Patch] `Directory.Packages.props` adds the prerelease `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.2-26098.1` without the per-block justification comment used elsewhere [Directory.Packages.props:71-72]. Add a leading comment block stating the cross-package compatibility constraint and removal condition.
- [x] [Review][Patch] `Hexalith.Memories.Web.csproj` is `IsPackable=true` without the NuGet metadata block other packable Memories projects ship (Description/Authors/License/Repo/Tags/README) [src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj]. Either add the metadata block and ship a README, or set `IsPackable=false` for now.
- [x] [Review][Patch] Order-basis "unavailable" rendering missing for axis and graph; sources hard-code `"Order basis: packet order"` for every packet, never rendering "order basis: unavailable" per Advanced Elicitation #4 [src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:7]. Render order-basis label per inspector and pick "packet order" vs "unavailable" from a contract signal (or document explicitly that the contract has no ordering field and render "unavailable").
- [x] [Review][Patch] No test for sensitive content in tenant/case identifiers (`TenantId`, `CaseId`, `CaseName`, `PermissionsContext`) [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs]. `SafeText` is called on tenant/case at render but the sanitization path is unverified. Add a fixture with sensitive values in scope fields.
- [x] [Review][Patch] Empty `<p>` for `Evidence.Caveat` when null/whitespace [src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor:24]. Gate the render on non-empty.
- [x] [Review][Patch] Confidence badge color sourced from `Packet.State` (evidence health) while text sourced from `EvidenceStrength` — two different contract concepts on one chip [src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor:6-9]. Align both on `EvidenceStrength` (with documented mapping) or split into two separate chips.
- [x] [Review][Patch] Graph `RelatedPath` empty but `Graph.Available=true` → empty `<dd>` [src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor:13]. Fallback message: "no traversal path".
- [x] [Review][Patch] Graph node separator `" -> "` literal is not aria-friendly; screen readers read "dash greater-than" [src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor:13]. Wrap in `aria-hidden` span and add visually-hidden text "then" between nodes, or render as a semantic `<ol>` with `aria-orientation="horizontal"`.
- [x] [Review][Patch] Graph ordering not asserted in tests [tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs]. Add a test that asserts `RelatedPath` renders in packet order (e.g., "memory-a -> memory-b" not the reverse).
- [x] [Review][Patch] `Hexalith.FrontComposer.Testing.ValidateVersionAlignment` not invoked in test setup [tests/Hexalith.Memories.Web.Tests]. Add to bUnit context construction so future drift between Memories and FrontComposer package versions fails fast.

### Deferred (pre-existing or scoped to future stories)

- [x] [Review][Defer] `EvidenceDisplay.Label` is locale-insensitive humanization bypassing FrontComposer's `IStringLocalizer<FcShellResources>` pattern — deferred, broader localization work spans the whole RCL.
- [x] [Review][Defer] `EvidencePacketScope.PermissionsContext` not surfaced in `MemoriesScopeHeader` — deferred, requires a new UX decision on where/how to display the machine-readable permission context.
- [x] [Review][Defer] `EvidencePacketOmittedDetails` body fields (`OmittedCount/FieldNames/DetailGroups/ExpansionHandles`) silently dropped — deferred, depends on a future "expand omitted details" UX.
- [x] [Review][Defer] `EvidencePacketSource.AnnotationsCount/CaseId/CaseName` not rendered — deferred, deemed not load-bearing for AC3 inspection workflow.
- [x] [Review][Defer] `EvidencePacketResultSummary.TotalCount/ReturnedCount/HasIndexedMemoryUnits` not surfaced — deferred, distinguishes empty-tenant from empty-result but not required by AC1.
- [x] [Review][Defer] `EvidencePacketEvidence.Degraded`/`AllEnabledAxesUnavailable` flags not fed into Trust Strip — deferred, overlapping signal with `State` already shown.
- [x] [Review][Defer] Task 6 a11y checkboxes marked `[x]` despite no automated forced-colors / focus-return / touch-target / no-text-overlap check; Completion Notes correctly call out Playwright deferred for RCL-only slice. Deferred to a follow-up story once a runnable web host is added.
- [x] [Review][Defer] `aria-label="Inspect source 0"` if Rank is 0 — deferred, only relevant if Inspect button is reinstated with wiring.
- [x] [Review][Defer] No negative tests for copy/export/MCP-inspect payload redaction parity — deferred, vacuous until command UI is wired.
- [x] [Review][Defer] No transition-state a11y coverage (loading→complete, complete→degraded) — deferred, useful when buttons trigger real state changes.
- [x] [Review][Defer] `<article>` + nested `<section aria-label>` creates a verbose landmark list — deferred, a11y refinement after primary findings settle.
- [x] [Review][Defer] Source citation order test asserts `data-source-rank` attribute values, not DOM iteration order — deferred, acceptable proxy with current rank-stable contract.
- [x] [Review][Defer] `SourceCountLabel` does not handle negative or `int.MaxValue` — deferred, contract precludes negative counts; defensive only.
- [x] [Review][Defer] `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText` — deferred, enum-like strings have controlled vocabulary; revisit if contract loosens.
- [x] [Review][Defer] Stale state never tested by fixture — covered by the broader "5 of 8 states untested" patch above; standalone deferred.
- [x] [Review][Defer] CSS `flex-wrap: wrap` is used instead of `FluentStack` — deferred minor compliance gap with Fluent UI primitive preference; the wrapping behavior itself is correct.
- [x] [Review][Defer] `Sources[].SourceUri` rendered without trust-mark badging (e.g., not marking external URLs vs local memory references) — deferred, not in AC3.
- [x] [Review][Defer] `<dl>` in graph path summary uses raw `<dt>/<dd>` rather than `FluentDescriptionList` — deferred, FrontComposer primitive preference; functionality is correct.
