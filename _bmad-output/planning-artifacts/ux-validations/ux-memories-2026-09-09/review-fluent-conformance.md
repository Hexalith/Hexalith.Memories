---
review_lens: fluent-ui-v5-frontcomposer-conformance
reviewed: 2026-09-09
targets:
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/ux-design-directions.html
verdict: revise-before-binding-web-implementation
severity_counts:
  critical: 0
  high: 2
  medium: 2
  low: 1
---

# Fluent UI V5 / FrontComposer Conformance Review

## Overall Verdict

**Revise before using this legacy UX package as a binding web implementation contract.** The UX prose has the right architectural intent: FrontComposer is the application-composition boundary, Fluent UI Blazor V5 is the component boundary, and web work is activation-gated. Two high-severity contract gaps remain: the component vocabulary does not map conceptual patterns to the exact pinned V5/FrontComposer owners, and the required `FluentAccordion` rule is absent.

The HTML showcase is a historical, illustrative planning artifact, not production implementation guidance. Its raw HTML/CSS/JavaScript and legacy-shaped tokens therefore do **not** constitute a product-code conformance failure. They remain a downstream copy hazard because the no-copy disclaimer appears only after all eight directions.

No critical finding is raised because the canonical product plan still says the current web assembly is an Epic 17 conformance specimen rather than an activated product surface, and the UX prose independently gates browser implementation on an approved phase.

## Scope and Authorities

Reviewed:

- [Legacy UX specification](../../ux-design-specification.md), including design-system choice, implementation rules, component strategy, accessibility, and phase language.
- [Historical HTML directions](../../ux-design-directions.html), including its disclaimer, raw control/CSS/JavaScript inventory, selection behavior, and likely copy-forward patterns.
- [Hexalith UX instructions](../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md), especially component ownership, reuse, Fluent 2 tokens, and accordion rules.
- [Memories project context](../../../project-context.md) and the [FrontComposer project context](../../../../references/Hexalith.FrontComposer/_bmad-output/project-context.md).
- [FrontComposerShell contract](../../../../references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md) and the current Memories Epic 17 conformance specimen/tests, used to bound current implementation risk rather than to validate the legacy HTML as production code.

This is a UX-contract conformance review, not a complete source-code review of `Hexalith.Memories.Web`.

API verification used the centrally selected package `Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.5-26219.1`. `dotnet-inspect` confirmed `FluentAccordion`, `FluentAccordionItem`, `FluentDialog`, `FluentMenu*`, `FluentMessageBar`, `FluentBadge`, `FluentProgressBar`, `FluentTabs`, `FluentTreeView`, `FluentLayout`, `FluentStack`, and `FluentDataGrid<T>`. It found no `FluentCommandBar`, `FluentToolbar`, `FluentDrawer`, or `FluentPanel` type.

## Findings

### HIGH FC-01 — Conceptual component names are presented as a Fluent vocabulary without an exact FrontComposer/V5 ownership map

**Evidence**

- The specification correctly establishes that all web implementation must use FrontComposer and Fluent UI Blazor V5, with FrontComposer as the application boundary and Fluent as the primitive boundary (`ux-design-specification.md:338-342`; reinforced at `919-923`).
- Elsewhere it says Fluent supplies “command bars” and “drawers” as component vocabulary (`320` and `810`), recommends “Fluent UI command/menu patterns” (`480`), says commands are backed by “buttons, menus, and command bars” (`954`), and requests side panels/drawers through “Fluent UI overlay behavior” (`1020-1025`). These are useful UX concepts, but several are not types in the pinned V5 package.
- The specification contains no occurrence of `FrontComposerShell`, although the framework contract says it owns `FluentLayout`, header/navigation/content/footer, providers, skip links, theme/settings/account controls, the command-palette trigger, and global shortcuts (`FrontComposer project-context.md:135-138`; `front-composer-shell.md:17-24, 56-79, 102-126`).
- The pinned V5 API inspection found no `FluentCommandBar`, `FluentToolbar`, `FluentDrawer`, or `FluentPanel`. Exact available primitives include `FluentButton`, `FluentMenuButton`/`FluentMenu`/`FluentMenuItem`, `FluentDialog`/`IDialogService`, `FluentMessageBar`, `FluentAccordion`, `FluentTabs`, `FluentDataGrid<T>`, `FluentStack`, and `FluentLayout`.

**Impact**

An implementer can reasonably interpret the generic nouns as real Fluent controls, select an incompatible sample/API, introduce a third-party or hand-rolled substitute, or duplicate shell-owned chrome and keyboard behavior. “FrontComposer-aligned” and “FrontComposer-style” are weaker than an explicit inheritance and ownership contract.

**Required fix**

Add a binding component/ownership table before the custom-component catalogue:

- Application frame, navigation, providers, skip links, theme/settings/account UI, global command palette, and global shortcuts: inherit `FrontComposerShell`; do not recreate them in Memories.
- Domain page content and copy: Memories-owned, composed inside the shell.
- Actions: FrontComposer command registration/lifecycle where applicable; otherwise exact V5 `FluentButton` and `FluentMenu*` primitives. Do not name a nonexistent `FluentCommandBar`.
- Feedback: `FluentMessageBar`, `FcStatusBadge`/`FluentBadge`, and `FluentProgressBar`, selected by state semantics.
- Data: `FluentDataGrid<T>`; layout: `FluentStack`/`FluentGrid` inside the shell-owned layout.
- Overlays: `FluentDialog`/`IDialogService` where the dialog behavior fits. If a drawer/side-panel remains necessary and no owned primitive exists, mark it as a justified composition gap with semantic, focus, forced-colors, and conformance-test requirements rather than implying a V5 drawer type.

State explicitly that unspecified visual and interaction behavior inherits the current FrontComposer and centrally pinned Fluent UI Blazor V5 contracts; UX mockups cannot override them.

### HIGH FC-02 — The mandatory accordion rule is absent from the binding UX behavior

**Evidence**

- Hexalith requires every page/dialog/detail panel with two or more sibling titled content sections to use one `FluentAccordion`, one `FluentAccordionItem` per section, with the primary item expanded by default (`hexalith-ux-instructions.md:41-51`; FrontComposer project context `160-167`).
- The Evidence Packet defines sibling titled regions—summary, evidence, sources, reasoning, graph context, state, and recovery (`ux-design-specification.md:545-554`, `818-915`) and calls for progressive disclosure (`1031-1033`), but the document contains zero occurrences of `FluentAccordion` or `FluentAccordionItem`.
- The pinned package exposes `FluentAccordion.ExpandMode` and `HeadingLevel`; `FluentAccordionItem` exposes the current V5 `Header`, `HeaderTemplate`, `Expanded`, and `HeadingLevel` members.
- The current non-activated Epic 17 specimen already demonstrates the expected pattern with one `FluentAccordion` and multiple `FluentAccordionItem` children (`src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor:53-98`). The legacy UX contract should describe that invariant instead of leaving it to implementation discovery.

**Impact**

Future implementations can satisfy the prose with independently collapsible panels, custom disclosure controls, tabs, or always-visible titled card stacks while violating the project-wide page-section rule and creating inconsistent heading/keyboard semantics.

**Required fix**

Add a behavioral rule to the Evidence Packet and page-pattern sections:

- Keep the page title, breadcrumb, toolbar, scope header/trust strip, and a sole primary grid/form/chart outside the accordion.
- When two or more sibling titled content regions remain, use one `FluentAccordion` with a `FluentAccordionItem` per region.
- Set the primary item `Expanded="true"`; do not hide the only primary content behind disclosure.
- Use the pinned V5 `Header`/`HeaderTemplate`, `Expanded`, and heading-level surfaces; preserve accessible labels and state text.

### MEDIUM FC-03 — The historical HTML’s no-copy warning is too late to neutralize nonconformant patterns

**Evidence**

- The specification points implementers to the showcase and says the directions use Fluent UI Blazor-aligned patterns (`ux-design-specification.md:500-511`).
- The showcase defines its own theme and typography in CSS (`ux-design-directions.html:7-29`), including `--neutral-*` legacy-shaped variables and hard-coded hex colors. It restyles headings, text, buttons, pills, inputs, focus-adjacent states, shadows, and radii (`35-42`, `70-80`, `88-108`, `143-155`, `225-240`, `250-256`, `281-311`).
- It contains 23 raw `<button>` elements, two raw `<input>` elements, 29 hard-coded hex-color occurrences, 47 `--neutral-*` occurrences, and custom JavaScript state switching (`471-478`, `509-510`, `638-640`, `655-656`, `784-800`). Those patterns are prohibited in module UI when FrontComposer/Fluent equivalents exist (`hexalith-ux-instructions.md:7-20, 22-39`).
- The design-direction selector uses `aria-selected` on ordinary buttons and only click handlers; it does not establish a tablist/tab relationship, `aria-controls`, arrow-key behavior, or managed focus (`470-478`, `784-800`). There is no `:focus-visible`, `forced-colors`, or `prefers-reduced-motion` treatment in the artifact.
- The document finally says it is a static HTML/CSS approximation rather than production component code, but only after all screens (`780`).

**Impact**

The artifact is legitimate for visual comparison, but a developer or generator can copy its first-screen markup/tokens before reaching the footer disclaimer. That would fail the current raw-control, raw-hex, theme-primitive, and legacy-token governance guards and would carry incomplete keyboard/forced-colors behavior into a product surface.

**Required fix**

Put a prominent banner and source comment at the top of the HTML and next to the specification’s link: “Historical visual reference only. Do not copy markup, CSS, JavaScript, tokens, ARIA, or component names. Re-express the selected layout through `FrontComposerShell` and the centrally pinned Fluent UI Blazor V5 components; the UX contract and repository conformance tests win.” Retain the footer, and label the navigation behavior illustrative rather than accessibility-reference behavior.

### MEDIUM FC-04 — The component “Phase 1 / 2 / 3” roadmap can be mistaken for canonical product-phase activation

**Evidence**

- The UX document correctly states that MVP is CLI-first, FrontComposer/Fluent web composition is future work, and a later sprint change must activate it (`ux-design-specification.md:99-109`). It repeats that browser composition and visual accessibility become binding only in an approved implementation phase (`578-582`).
- The component roadmap then uses unqualified headings “Phase 1 - Core Trust Components,” “Phase 2 - Inspection Components,” and “Phase 3 - Continuity and Operations Components” (`927-948`).
- The canonical PRD says `Hexalith.Memories.Web` is an Epic 17 conformance specimen and “not an activated product surface” (`prd.md:709-716`); its web NFR gates remain not started until a web surface ships (`1098-1107`). Sprint status classifies Epic 17 as `future-ui`, `future-web-ui`, and excluded from MVP readiness (`sprint-status.yaml:68-92, 111`).

**Impact**

The roadmap can be read as authorizing Phase 1 product web implementation even though the surrounding prose and canonical phase register do not. It also overloads “Phase 2” with both a product phase and an internal component tranche.

**Required fix**

Rename these headings to “Web composition wave 1 / 2 / 3 (ordering only; inactive until sprint-selected)” and add the canonical activation condition immediately above the list. State that completing the conformance specimen does not activate the product web surface or its NFR gates.

### LOW FC-05 — A referenced foundational context still advertises the superseded Fluent RC

**Evidence**

- The UX frontmatter lists `_bmad-output/project-context.md` as an input (`ux-design-specification.md:6-13`).
- That context says Fluent UI is pinned to `5.0.0-rc.3-26138.1` (`_bmad-output/project-context.md:30`).
- The central package catalogue now selects `5.0.0-rc.5-26219.1` (`references/Hexalith.Builds/Props/Directory.Packages.props:226`), matching FrontComposer context (`references/Hexalith.FrontComposer/_bmad-output/project-context.md:41-42`) and the Memories conformance pin (`tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs:32-65`).

**Impact**

The UX specification itself wisely says only V5, so this is not an immediate document contradiction. An implementer following the referenced source stack can still consult the wrong release-candidate API and copy obsolete member names.

**Required fix**

Refresh the Memories project context from the central catalogue, or replace the point-version sentence with a statement that the effective version is resolved from the centrally selected `Hexalith.Builds` package catalogue. Keep exact API checks tied to the evaluated package, not a copied planning-doc pin.

## Compliant Strengths

- The core implementation boundary is stated clearly and correctly (`ux-design-specification.md:338-342`, `919-923`): FrontComposer first, Fluent UI Blazor V5 primitives, no raw controls when an owned component exists, no legacy v4/FAST tokens, and no theme recreation.
- The document explicitly treats the HTML as static approximation rather than production code (`ux-design-directions.html:780`). That disclaimer is why the mock’s raw controls and CSS are a copy-risk finding rather than a product-code failure.
- Phase intent is mostly disciplined: future web status is explicit (`ux-design-specification.md:103`, `580-582`), and the Evidence Packet contract is correctly prioritized ahead of browser composition.
- Accessibility coverage is strong: textual state semantics, complete keyboard trust loop, overlay focus entry/return, reduced motion, high contrast/forced colors, visible focus, live-region restraint, and no hover-only actions are all required (`ux-design-specification.md:1104-1118`, `1131-1142`, `1154-1162`).
- Status is not color-only (`ux-design-specification.md:484-492`, `1106-1108`), graph paths require a linear text alternative (`880`), and errors/loading/recovery are explicitly screen-reader reachable (`996-1005`).
- The current Epic 17 specimen has already moved beyond several legacy-contract gaps: it uses `FluentAccordion`, `FluentStack`, `FluentMessageBar`, `FluentDataGrid`, and FrontComposer status components; governance tests reject raw controls, raw hex colors, legacy tokens, and recreated theme primitives (`Epic17ConformanceTests.cs:74-115`). This evidence reduces current code risk but does not repair the legacy UX contract for future reuse.

## Severity Counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 2 |
| Medium | 2 |
| Low | 1 |

## Recommended Resolution Order

1. Add the binding FrontComposer/V5 ownership and exact-component mapping, including `FrontComposerShell` inheritance.
2. Add the mandatory `FluentAccordion` page-section rule to Evidence Packet and page behaviors.
3. Put an unmistakable no-copy warning at the top of the historical HTML and beside its specification link.
4. Rename component phases to non-activating web composition waves.
5. Refresh the stale Fluent package reference in Memories project context.
