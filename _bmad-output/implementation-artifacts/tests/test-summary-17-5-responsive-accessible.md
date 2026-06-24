# Validation Evidence — Story 17.5 (Responsive and Accessible Web Validation)

- **Story:** `17-5-responsive-and-accessible-web-validation`
- **Workflow:** `bmad-dev-story`
- **Date:** 2026-06-24
- **Scope:** Validation/quality-gate story for the Epic 17 future web surfaces built by Stories 17.1–17.4.
  No product workflows, contracts, public APIs, package versions, or FrontComposer framework behavior
  were added or changed. No product or submodule files were modified — only test files were added.

## Dependency gate (Task 0)

| Dependency | Sprint status at dev time | Action taken |
|---|---|---|
| Story 2.7 (Evidence Packet contract) | `review` (not `done`) | Used approved `Contracts.V1` fixtures only; no Story 2.7 source/test/CLI/MCP/mapper files touched. |
| Story 17.1 (Evidence Cockpit + trust components) | `done` | Validated as component-specimen. |
| Story 17.2 (Recovery + feedback state grammar) | `done` | Validated as component-specimen. |
| Story 17.3 (Contract-aware interaction patterns) | `done` | Validated as component-specimen. |
| Story 17.4 (Role-specific lenses) | `done` | Validated as component-specimen. |

## Runnable-surface reality

`Hexalith.Memories.Web` is a **host-less Razor Class Library** (`IsPackable=false`, no `Program.cs`,
no `App.razor`, no `@page`; see `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj`). There is **no
runnable Memories web route, Playwright host, or axe/forced-colors/reduced-motion/screen-reader harness**
for these components (the FrontComposer Playwright workspace targets the Counter specimen only). Therefore:

- **Validation level for every Epic 17 surface = `component-specimen` (bUnit) backed by `contract-fixture`
  packets.** Nothing is claimed as `product-route` validation.
- Browser and assistive-technology dimensions are recorded as **deferred gaps** (fail-closed), not silently
  passed. Building a runnable host/specimen to enable them is out of this validation story's scope.

The machine-checked inventory, gap register, and AC→test map live in code under
`tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs` and are guarded by
`Epic17InventoryTests`.

## Surface inventory (component-specimen)

| Surface | Story | Implementation | Specimen / fixture | Anchor | Level |
|---|---|---|---|---|---|
| Evidence Cockpit | 17.1 | `MemoriesEvidenceCockpit` | `EvidencePacketFixtures` | `mem-evidence-cockpit` | component-specimen |
| Trust Strip | 17.1 | `MemoriesTrustStrip` | `EvidencePacketFixtures` | `mem-trust-strip` | component-specimen |
| Scope Header | 17.1 | `MemoriesScopeHeader` | via cockpit | `mem-evidence-scope` | component-specimen |
| Source Citation Stack | 17.1 | `MemoriesSourceCitationStack` | via cockpit | `mem-source-stack` | component-specimen |
| Retrieval Axis Breakdown | 17.1 | `MemoriesRetrievalAxisBreakdown` | via cockpit | `mem-axis-breakdown` | component-specimen |
| Graph Path Summary | 17.1 | `MemoriesGraphPathSummary` | via cockpit | `mem-graph-summary` | component-specimen |
| Recovery Action Panel | 17.2 | `MemoriesRecoveryActionPanel` | `RecoveryPacketFixtures` | `mem-evidence-recovery` | component-specimen |
| Case Activity Trail | 17.4 | `MemoriesCaseActivityTrail` | `LensPacketFixtures` | `mem-activity-trail` | component-specimen |
| Ingestion Lifecycle Tracker | 17.4 | `MemoriesIngestionLifecycleTracker` | `LensPacketFixtures` | `mem-ingestion-tracker` | component-specimen |
| Operator Health Matrix | 17.4 | `MemoriesOperatorHealthMatrix` | `LensPacketFixtures` | `mem-health-matrix` | component-specimen |
| Benchmark Result Comparator | 17.4 | `MemoriesBenchmarkResultComparator` | `LensPacketFixtures` | `mem-benchmark-comparator` | component-specimen |
| Agent Packet Inspector | 17.4 | `MemoriesAgentPacketInspector` | `LensPacketFixtures` | `mem-packet-inspector` | component-specimen |
| Evidence Grid | 17.3 | `MemoriesEvidenceGrid` | `EvidencePacketFixtures` | `mem-evidence-grid` | component-specimen |
| Command Surface | 17.3 | `MemoriesCommandSurface` | `EvidencePacketFixtures` | `mem-command-surface` | component-specimen |
| Action Confirmation | 17.3 | `MemoriesActionConfirmation` | `EvidencePacketFixtures` | `fc-destructive-dialog` | component-specimen |
| Context Navigation | 17.3 | `MemoriesContextNavigation` | `EvidencePacketFixtures` | `mem-context-navigation` | component-specimen |
| Interaction Form | 17.3 | `MemoriesInteractionForm` | `FormFixtures` | `mem-interaction-form` | component-specimen |
| Filter Summary | 17.3 | `MemoriesFilterSummary` | `EvidencePacketFixtures` | `mem-filter-summary` | component-specimen |
| Lens Shell | 17.4 | `MemoriesLensShell` | `LensPacketFixtures` | `mem-lens-shell` | component-specimen |

## AC → evidence matrix

| AC | Requirement | bUnit evidence (runnable here) | Playwright/browser/AT (deferred) |
|---|---|---|---|
| AC1 | Responsive parity across 360/768/1024/1440; trust fields reachable, no horizontal-scroll-only | `Epic17ResponsiveParityTests` — grid planner never collapses trust-critical columns across the full max-visible width budget (compact + non-compact); compact grid keeps trust-critical cells un-collapsed and exposes overflow via `mem-grid-more` disclosure; cockpit keeps scope/confidence/freshness/source-count/evidence-health reachable; role/density changes preserve canonical contract-backed fields (information parity) | Pixel-width layout & reflow @400% require a browser viewport — **deferred** |
| AC2 | Automated a11y: contrast, accessible names, form labels, ARIA validity, heading order, focusable controls, zero-target-node guard | `Epic17AccessibilitySweepTests` — per-surface root-anchor node-count ≥ 1 (zero-node guard); focusable controls (no negative tabindex, no `aria-hidden`); accessible names on trust badges; existing per-surface label/role/`aria-describedby` tests | `@axe-core/playwright` WCAG 2.2 AA scan + automated **contrast** require a browser — **deferred**; contrast covered by Fluent UI v5 token usage |
| AC3 | Human a11y: keyboard-only, focus order, no-color-only, reduced motion, high contrast, ≥1 screen-reader pass | `Epic17AccessibilitySweepTests` (no hover-only handlers; live-region validity; non-color text equivalents for confidence/freshness/evidence-health/severity/benchmark/operator/ingestion) + `Epic17FocusContractTests` focus contracts | Live keyboard focus order, reduced-motion/forced-colors emulation, and a **screen-reader pass** require a browser + AT — **deferred** with required manual pass |
| AC4 | Overlay focus enters and returns to invoking control | `Epic17FocusContractTests` — documented focus contract per overlay; destructive confirmation routed through FrontComposer `fc-destructive-dialog` (owns trap/return); navigation preserves return path when forward action disabled; overlay controls focusable | Live focus-trap/return movement requires a browser — **deferred** |
| AC5 | No hover-only trust behavior | `Epic17AccessibilitySweepTests.Surface_DoesNotDependOnHoverOnlyInteraction` (no `onmouse*` handlers across surfaces × states); existing keyboard-reachability tests | `:hover`-enhancement-only visual checks require a browser — **deferred** |
| AC6 | No secret/tenant/raw-payload leakage in labels, tooltips, copy/export, diagnostics | `Epic17SanitizationCanaryTests` — seeded redaction canaries (bearer/path/connection-string) across every packet-driven surface in markup **and** accessible names; restricted-source existence suppressed under unauthorized scope (tenant isolation); copy payload sanitized and equals JSON view; cross-tenant repartition leaves no active-tenant residue | n/a — sanitization is fully validatable at component level |

## Reproducibility metadata

- **Working directory:** repo root (`Hexalith.Memories`).
- **Build:** `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj -m:1`
  → `0 Warning(s), 0 Error(s)` under `TreatWarningsAsErrors=true`.
- **Run (sandbox-safe):** `dotnet test` aborts in this sandbox on a VSTest local-TCP listener
  (`SocketException (13): Permission denied`), so the xUnit v3 in-process runner was executed directly:
  `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll`.
- **Frameworks:** xUnit v3 `3.2.2` + bUnit `2.8.4-preview` + Shouldly `4.3.0` + `Hexalith.FrontComposer.Testing`
  (`FrontComposerTestBase`, `AddFluentUIComponents` host); Fluent UI Blazor pinned `5.0.0-rc.3-26138.1`.
- **Fixtures:** Story 2.7-aligned `EvidencePacketFixtures` / `LensPacketFixtures` / `RecoveryPacketFixtures` /
  `FormFixtures`; loose JS interop via the test host.
- **Culture:** invariant; no machine-local paths emitted by tests; no committed browser artifacts.

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (after Story 17.4) | 291 | 0 | 0 | 0 |
| After Story 17.5 validation sweeps (dev-story) | 407 | 0 | 0 | 0 |
| **After QA gap-closure pass (qa-generate-e2e-tests)** | **453** | **0** | **0** | **0** |
| Story 17.5 `Components/Validation` namespace only | 162 | 0 | 0 | 0 |

`git diff --check` is clean. No product (`src/**`) or submodule (`Hexalith.FrontComposer/**`) files were changed.

## QA gap-closure pass (bmad-qa-generate-e2e-tests, 2026-06-24)

A QA automation pass audited the six ACs against the 116 dev-story validation tests and auto-applied
**46 additional bUnit tests** (116 → 162 in the `Components/Validation` namespace) to close coverage gaps
for AC requirements named but not yet asserted. Validation-only: still no `src/**` or
`Hexalith.FrontComposer/**` changes — only the four existing validation test files were extended.

| Gap closed | AC | Added coverage |
|---|---|---|
| Heading outline order | AC2 | `Epic17AccessibilitySweepTests.Cockpit_ComposedHeadings_FollowValidOutlineOrder_NoSkippedLevels` (×5 states) — the composed cockpit opens at its shallowest heading (`<h2>Evidence`), has a single top heading, and never skips a level over the `<h3>` source/axis/graph sections. |
| Form labels / accessible names | AC2 | `InteractionForm_Controls_ExposeAccessibleNames_NotColorOrPlacementAlone` — `role="form"` + non-empty `aria-label`, a text label per field row, and a readable submit name. |
| ARIA reference validity | AC2 | `InteractionForm_EveryAriaDescribedbyReference_ResolvesToARenderedId` — every `aria-describedby` idref resolves to exactly one rendered element (no dangling/duplicated reference). |
| Recovery reachability in cockpit | AC1 | `Epic17ResponsiveParityTests.Cockpit_SafestRecoveryAction_RemainsReachableAcrossRecoveryStates` (×4 recovery fixtures) — the safest recovery action (or an explicit no-action explanation) stays composed in the cockpit, not `aria-hidden`, and has no negative tabindex. |
| Task 5 fixture families (degraded/stale/redacted) | AC6 | `Epic17SanitizationCanaryTests.Surface_AdditionalCanonicalState_NeverLeaksCanariesIntoMarkup` (11 surfaces × 3 states = 33) — extends the canary sweep beyond happy/compressed/unauthorized. |
| Task 5 invalid/schema-mismatch + missing-source | AC6 | `AgentPacketInspector_InvalidOrMissingSourceState_KeepsCopyAndJsonCanarySafe` (×2) — copy payload equals the JSON view and stays canary-free on the highest-risk diagnostics surface. |

These additions strengthen the AC2 (`heading order`, `form labels`, `ARIA validity`), AC1 (`safest recovery
action remain reachable`), and AC6/Task 5 (`degraded, stale, redacted, invalid/schema-mismatch,
missing-source` fixture families) requirements that the original sweeps named but did not directly assert.
The browser/assistive-technology dimensions in the deferred register below are unchanged and remain
fail-closed gaps requiring a runnable host and a manual screen-reader pass before a full product-surface claim.

## Deferred / blocked register (fail-closed)

These browser and assistive-technology dimensions cannot run against a host-less RCL and are tracked in
`Epic17ValidationInventory.Gaps` (each carries owner, severity, waiver state, and release disposition):

1. **Playwright product-route smoke + `@axe-core/playwright` WCAG 2.2 AA scan** — High — blocked until a
   runnable Memories web host/specimen exists.
2. **Automated color-contrast** — Medium — covered by Fluent UI v5 tokens (no legacy v4/FAST, no hand-rolled
   color); re-verify in browser when a host exists.
3. **Forced-colors (high contrast) emulation** — Medium — non-color comprehension proven at component level;
   manual high-contrast pass required.
4. **Reduced-motion emulation** — Low — no component depends on animation for trust comprehension; manual pass
   required.
5. **Zoom/reflow to 400% and 44×44px touch-target sizing** — Medium — manual pass required against a host.
6. **Screen-reader pass + live keyboard focus-trap/return** — High — focus contracts documented and ARIA
   semantics validated; at least one manual screen-reader pass required before release.

### Deferred product/architecture decisions (named, not resolved here)

- Release-blocking threshold for manual screen-reader defects.
- Final artifact-retention policy for accessibility evidence.
- Mobile grid-to-card transformation strategy.
- Unsupported browser/assistive-technology matrix beyond the initial validation set.
