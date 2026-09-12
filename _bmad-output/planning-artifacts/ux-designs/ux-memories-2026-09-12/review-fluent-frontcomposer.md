---
review_lens: fluent-ui-v5-frontcomposer-conformance
reviewed: 2026-09-12
targets:
  - DESIGN.md
  - EXPERIENCE.md
verdict: conformant-with-targeted-revisions-before-web-activation
severity_counts:
  critical: 0
  high: 1
  medium: 1
  low: 1
---

# Fluent UI V5 / FrontComposer Conformance Review

## Overall verdict

**Conformant for the current non-product specimen and active CLI/MCP scope; make three targeted documentation corrections before a downstream web surface treats the spines as an implementation contract.** The migration closes both high-severity defects in the 2026-09-09 legacy review: it now binds the experience to `FrontComposerShell`/Fluent UI Blazor V5 with explicit ownership, and it states the required single-accordion rule. It also correctly refuses to turn the specimen into product-route evidence. (`DESIGN.md:12-32`, `DESIGN.md:171-181`, `DESIGN.md:203-241`; `EXPERIENCE.md:20-36`, `EXPERIENCE.md:228-232`, `EXPERIENCE.md:243-268`; `../../ux-validations/ux-memories-2026-09-09/review-fluent-conformance.md:41-88`)

The remaining high finding is contract metadata drift: the normative `primitive` values neither match current component source nor consistently identify a real component type. The medium finding makes the destructive-dialog launch/focus owner precise. Neither finding invalidates a current product UI, because none exists and the route-level activation gate remains closed.

## Method and scope

- Read both current spines, the repository UX baseline, the migration's current implementation extract, the 19-component RCL/specimen inventory, the fail-closed conformance allowlist/tests, accessibility/focus/responsive inventories, and the relevant FrontComposer shell/page/toolbar/dialog/status sources. (`../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-51`; `.working/extract-ui-implementation.md:3-107`)
- Checked inheritance discipline; shell/host/domain ownership; accordion structure; named V5/FrontComposer primitives; theme/token policy; forbidden v4/FAST/raw styling; canonical concept/type naming; component anatomy; specimen-versus-product claims; overlay/focus/provider ownership; and web-activation evidence gates.
- Rebuilt `Hexalith.Memories.Web.Tests` with 0 warnings/errors and ran the freshly built xUnit v3 executable: **492 total, 492 passed, 0 failed/skipped**. The repository's `dotnet test` path selected zero tests under Microsoft.Testing.Platform, so it was not treated as evidence; the documented in-process runner was used.
- This is a UX-contract conformance lens, not a general code, product, or accessibility certification. Existing browser/AT gaps remain activation evidence, not defects in the inactive product surface.

## Findings by severity

### Critical — 0

No finding can cause current product UI harm because the spines and implementation both classify the Web RCL and its routes as conformance specimens, not an activated product surface. (`EXPERIENCE.md:24-36`, `EXPERIENCE.md:91-95`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17InventoryTests.cs:47-73`; `../../../../tests/Hexalith.Memories.Web.Specimens/Epic17SpecimenManifest.cs:8-17`)

### High — 1

#### FC-01 — Normative primitive bindings drift from the named current components

`DESIGN.md` makes its machine-readable inheritance/component leaves normative, says exact component types already exist, and dereferences the `primitive` leaves as the visual contract. Yet many primitive values are not the current source composition and some are conceptual labels rather than exact types. (`DESIGN.md:33-166`, `DESIGN.md:173-175`, `DESIGN.md:203-229`)

Representative mismatches:

- Source Citation Stack declares `FluentStack and FluentButton`, while current `MemoriesSourceCitationStack` uses `FluentText` plus allowlisted semantic section/ordered-list/description-list markup and has no button. (`DESIGN.md:55-61`; `../../../../src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:4-43`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs:103-104`, `:259-265`)
- Retrieval Axis Breakdown and Graph Path Summary declare `FluentStack`/badge-oriented compositions that current source does not use; both rely on `FluentText` and registered semantic markup. (`DESIGN.md:62-75`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs:97-100`, `:242-254`)
- Command Surface declares `FluentText` although current source uses `FluentLabel`; Shared Lens Shell declares conceptual `Context Navigation` rather than its actual `FluentButton`; and several lens entries use conceptual `Shared Lens Shell` rather than exact `MemoriesLensShell`. (`DESIGN.md:104-109`, `DESIGN.md:125-165`; `../../../../src/Hexalith.Memories.Web/Components/Interaction/MemoriesCommandSurface.razor:9-31`; `../../../../src/Hexalith.Memories.Web/Components/Lenses/MemoriesLensShell.razor:22-98`)
- Ingestion Lifecycle Tracker declares `FluentProgressBar`/`FluentText`, and Operator Health Matrix declares `FluentDataGrid`; current implementations instead compose `MemoriesLensShell`, `FluentStack`, `FluentLabel`, `FcStatusBadge`, and `FluentButton`. (`DESIGN.md:139-152`; `../../../../src/Hexalith.Memories.Web/Components/Lenses/Ingestion/MemoriesIngestionLifecycleTracker.razor:21-98`; `../../../../src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/MemoriesOperatorHealthMatrix.razor:21-91`)

**Downstream impact:** a generator or implementer can reasonably treat these normative leaves as exact implementation evidence, recompose a component with controls it does not currently own, miss registered semantic exceptions, or mistake a future desired anatomy for an already conforming one.

**Fix:** choose and label one meaning. Prefer separate `implemented-primitives` and `activation-target-primitives` leaves. Populate the former from the fail-closed conformance register using exact symbols (`MemoriesLensShell`, `FluentLabel`, etc.) and name registered semantic exceptions. Keep future behavioral additions in `EXPERIENCE.md`, or explicitly mark every target-only primitive. If `primitive` is intended to be exhaustive current evidence, align all 19 rows with the current allowlist/source.

### Medium — 1

#### FC-02 — Destructive-dialog focus ownership omits the required service/provider launch boundary

The Action Confirmation row says `FcDestructiveConfirmationDialog` owns focus, Escape, confirm/cancel, and plain-text rendering, while the general ownership rule assigns live focus movement to the product host. (`EXPERIENCE.md:122`, `EXPERIENCE.md:228`) The current Memories wrapper renders the FrontComposer dialog body directly and the specimen mounts that wrapper inline; neither proves a dialog frame, trap, or return. (`../../../../src/Hexalith.Memories.Web/Components/Interaction/MemoriesActionConfirmation.razor:8-16`; `../../../../tests/Hexalith.Memories.Web.SpecimenHost/Components/Pages/Epic17SpecimenSurface.razor:72-74`)

FrontComposer documents that `FcDestructiveConfirmationDialog` is opened through `IDialogService`; the service/provider lifecycle supplies the real dialog context, while the component supplies safe autofocus, Escape cancellation, callbacks, and plain-text content. (`../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor:5-28`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs:10-28`, `:42-92`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:185`)

**Downstream impact:** a host can render the specimen-shaped wrapper inline and still believe the spine's focus-trap/return promise is satisfied for a destructive action.

**Fix:** state at Action Confirmation and overlay rules that the product host launches the confirmation through the shell-provided Fluent `IDialogService`/provider lifecycle; `MemoriesActionConfirmation` maps domain copy and callbacks; `FcDestructiveConfirmationDialog` supplies the dialog body/safe controls; and only route-level browser evidence closes initial focus, trap, Escape, and focus return. Preserve the existing activation matrix requirement. (`EXPERIENCE.md:254-260`)

### Low — 1

#### FC-03 — The activation gate does not anchor its required dispositions to the existing gap registry

The future-web evidence matrix is strong and already demands route/state/browser/AT evidence plus a defect-or-waiver owner and release disposition. (`EXPERIENCE.md:254-260`) The current inventory, however, already records six named unresolved dimensions: axe incomplete triage, measured sub-44px targets, data-heavy horizontal overflow, product-route validation, non-Chromium validation, and live screen-reader/focus testing. (`../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs:155-210`) The spine does not point activation work to that fail-closed registry or its durable browser evidence artifact.

**Downstream impact:** low while the product horizon remains inactive, but a later activation team can repeat broad checks without explicitly closing or waiving each known debt.

**Fix:** add one activation-traceability sentence: activation must consume the current `Epic17ValidationInventory.Gaps`/browser summary and give every still-applicable row a dated closure or named waiver; specimen evidence never transfers automatically to a product route. Do not duplicate the registry's changing contents in the spine.

## Verified strengths

- **Inheritance and token discipline are explicit.** The spines inherit `FrontComposerShell`, FC-A11Y, centrally pinned Fluent UI Blazor V5, Fluent 2, shell theme/density modes, and declare no Memories color/type/spacing/radius/elevation delta. Exact package version ownership correctly stays in the central catalogue rather than being copied into UX prose. (`DESIGN.md:12-32`, `DESIGN.md:171-201`; `../../../../references/Hexalith.Builds/Props/Directory.Packages.props:226-227`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs:32-90`)
- **No forbidden styling vocabulary leaked into the spines.** Historical v4/FAST tokens, hard-coded colors, raw UI implementation, fixed mock breakpoints, and theme recreation are explicitly rejected; numeric CSS-pixel references are accessibility/performance test conditions rather than owned tokens. (`DESIGN.md:177-201`, `DESIGN.md:231-241`; `EXPERIENCE.md:258-268`, `EXPERIENCE.md:402-408`; `../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md:10-39`)
- **The ownership boundary is unusually clear.** FrontComposer owns shell, navigation, landmarks, global commands, providers, page composition, and shell accessibility; Memories owns packet projection, validation, and intents; the product host owns routes, authorization lifecycle, retrieval, dispatch, notifications, and live focus. (`EXPERIENCE.md:93-95`, `EXPERIENCE.md:107-129`, `EXPERIENCE.md:228`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:16-22`, `:31-185`)
- **The accordion rule is fully migrated.** Evidence Cockpit keeps Scope Header/Trust Strip outside one multi-expand accordion, uses five items with Evidence and Recovery initially expanded, suppresses empty subordinate regions for loading/error, and confines the Inspector's native disclosure to one registered exception. (`DESIGN.md:205-229`; `EXPERIENCE.md:230`; `../../../../src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:4-9`, `:27-98`, `:131-132`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs:273-297`)
- **Canonical naming is stable.** All 19 conceptual names pair with current Memories-prefixed types; Shared Lens Shell intentionally maps to `MemoriesLensShell` while the implementation inventory's shorter “Lens Shell” is not promoted to a second code symbol. (`DESIGN.md:33-166`; `EXPERIENCE.md:105-129`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs:90-112`)
- **The specimen/product boundary is explicit and evidence-backed.** The RCL is non-packable, specimen routes use `/__memories/specimens`, callbacks only update a test log, inventory rejects product-route claims, and the spines require separate host/route/browser/AT/performance evidence before activation. (`EXPERIENCE.md:24-36`, `EXPERIENCE.md:91-95`, `EXPERIENCE.md:254-260`; `../../../../src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:4-10`; `../../../../tests/Hexalith.Memories.Web.Specimens/Epic17SpecimenManifest.cs:8-41`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17InventoryTests.cs:47-73`)

## Severity counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 1 |
| Low | 1 |

## Resolution check

**Checked 2026-09-12 against the patched spines.** Narrow verification compared every machine-readable component binding with the current Razor composition and fail-closed allowlist, counted 19 `implementation` leaves and 19 `implemented-primitives` leaves with no legacy `primitive` leaf, and rechecked the dialog/provider and activation-registry statements. No build was rerun because the patch changes documentation only and the original 492/492 component run remains the implementation baseline.

- **FC-01 — Resolved.** `DESIGN.md` now separates implementation evidence from product activation, gives every one of the 19 components an exact `implemented-primitives` leaf, names registered semantic exceptions, and states that the leaves record current conformance-tested specimen composition. The representative mismatches are corrected: Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary use `FluentText` plus their actual semantic fallbacks; Command Surface uses `FluentLabel`; the lens bodies name `MemoriesLensShell`; and the ingestion/operator compositions match current source. (`DESIGN.md:15-19`, `:41-174`, `:183`, `:211-237`; `../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs:90-146`, `:235-316`)
- **FC-02 — Resolved in the normative spines; one low-severity evidence-comment residue remains open.** The visual binding now records the generic wrapper and product activation's Fluent dialog-service/provider dependency, while the behavioral row assigns copy/callbacks to Memories, body/autofocus/Escape/callbacks to the FrontComposer component, service launch to the host, and trap/return proof to the activated route. (`DESIGN.md:119-125`; `EXPERIENCE.md:119-139`) The current conformance allowlist summary still says the component “owns focus trap/return,” which is broader than both the patched spine and FrontComposer's `IDialogService` contract. This does not reopen the normative ownership defect, but that source comment should be corrected before the allowlist is cited independently. (`../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs:115-116`; `../../../../references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs:10-28`)
- **FC-03 — Resolved.** Both frontmatter blocks now identify the current UI/protocol extracts and conformance/gap registries as implementation evidence. The future-web gate must consume the then-current `Epic17ValidationInventory.Gaps` and durable browser summary, disposition every applicable row, and never transfer specimen evidence automatically to a product route. (`DESIGN.md:12-19`; `EXPERIENCE.md:12-19`, `:266-272`)

### Revised open-severity counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 1 |
