---
project: Hexalith.Memories
date: 2026-06-24
workflow: bmad-correct-course
change_trigger: UX should use only FrontComposer and Fluent UI Blazor components V5.
mode: Batch
status: approved
prepared_for: Jerome
prepared_at: 2026-06-24T15:03:12+02:00
approved_by: Jerome
approved_at: 2026-06-24T15:12:36+02:00
---

# Sprint Change Proposal - FrontComposer and Fluent UI Blazor V5 UX Boundary

## 1. Issue Summary

The requested course correction is that all Hexalith.Memories UX work must use only Hexalith.FrontComposer and Fluent UI Blazor V5 components. The existing repository rules already state this direction, but the current planning and implementation artifacts still leave too much room for interpretation:

- The UX specification repeatedly says FrontComposer and Fluent UI are the future web foundation, but some sections still use softer language such as "default" or "where practical."
- Epic 17 requires FrontComposer and Fluent UI Blazor, but it does not yet define a hard conformance gate for raw HTML/CSS, legacy tokens, or parallel UI primitives.
- Story 17.1 is marked done and created `src/Hexalith.Memories.Web`, but the current RCL contains raw Razor markup and scoped CSS that should be audited under the stricter rule:
  - `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor` uses raw `article`, `section`, `p`, `ul`, `li`, `strong`, and `span` markup.
  - `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor` uses raw `section`, `ol`, `li`, `div`, `span`, `p`, `dl`, `dt`, and `dd` markup.
  - `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css` and related scoped CSS use direct CSS styling, direct typography rules, and non-Fluent-2 token names such as `--warning-foreground-rest`, `--danger-stroke-rest`, and `--neutral-stroke-rest`.
- The package is already pinned to Fluent UI Blazor V5: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`, aligned with the `Hexalith.FrontComposer` submodule. The available Fluent UI MCP documentation targets `5.0.0.26139`, so local package/submodule code is the stronger source when APIs differ.

This is not a PRD scope expansion for MVP. It is an Epic 17 web UX governance correction and a completed-slice conformance cleanup.

## 2. Impact Analysis

### Epic Impact

- Epic 17 is directly affected. It is already `in-progress`; Story 17.1 is `done`, and Stories 17.2 through 17.5 are `ready-for-dev`.
- Epic 17 should gain an explicit FrontComposer/Fluent UI V5 conformance boundary that applies to all current and future Memories web surfaces.
- Add Story 17.6 to audit and harden the existing Story 17.1 RCL plus any Epic 17 follow-on work.
- No MVP product epics need resequencing. Epic 17 is future web UI scope unless separately pulled into product readiness.

### Story Impact

- Story 17.1 should receive a completion amendment pointing to Story 17.6 for stricter conformance cleanup rather than being silently treated as fully compliant with the new rule.
- Stories 17.2, 17.3, 17.4, and 17.5 should update their preflight/package-version text from the old `5.0.0-rc.2-26098.1` notes to the current `5.0.0-rc.3-26138.1` pin.
- Stories 17.2 through 17.5 should explicitly prohibit raw controls, raw layout/status primitives, third-party UI controls, legacy Fluent v4/FAST tokens, and handcrafted theme primitives when a FrontComposer or Fluent UI Blazor V5 component/token exists.
- Story 17.6 should be added as a backlog story under Epic 17.

### Artifact Conflicts

- PRD: no MVP scope conflict. Optional clarification can be added to the future web UI sentence so PRD readers understand the binding implementation boundary.
- Architecture: should document `Hexalith.Memories.Web` as a FrontComposer/Fluent UI V5 RCL boundary and explicitly forbid a standalone UI framework or design-system fork.
- UX Design Specification: should replace soft "default/where practical" language with a hard composition rule.
- Epics/story artifacts: require updates as described above.
- Sprint status: add `17-6-frontcomposer-fluent-ui-v5-conformance-hardening: backlog` after approval.

### Technical Impact

- No new package is required. The correct V5 package is already centrally pinned.
- Existing `src/Hexalith.Memories.Web` components require a focused conformance pass:
  - Replace raw UI mechanics with FrontComposer or Fluent UI Blazor components where equivalents exist.
  - Replace legacy/non-Fluent-2 CSS tokens and direct typography/color styling with Fluent UI V5 parameters or Fluent 2 tokens.
  - Keep only explicitly justified semantic/container markup where Blazor or Fluent has no suitable component, and track each exception in tests or a local allowlist.
  - Add conformance tests scanning Memories web `.razor` and `.razor.css` files.

## 3. Recommended Approach

Use Direct Adjustment with moderate scope.

Rationale:

- The product direction is already consistent with FrontComposer and Fluent UI Blazor V5; the gap is enforcement and cleanup.
- The current package pin is already V5 and aligned with FrontComposer.
- The implemented Story 17.1 RCL has concrete conformance risk, but the issue is localized to future web UX surfaces and does not require PRD/MVP replanning.
- A new Story 17.6 avoids rewriting done-story history while creating a clear implementation path.

Effort estimate: 2-4 working days, depending on how many raw semantic containers can be safely replaced by Fluent/FrontComposer primitives without weakening accessibility.

Risk level: Medium. The work touches UI component markup, accessibility semantics, tests, and possibly FrontComposer reuse boundaries, but it should not affect backend, CLI, MCP, storage, or tenant isolation behavior.

## 4. Detailed Change Proposals

### UX Design Specification Amendment

Section: `Design System Foundation / Implementation Approach`.

OLD:

```markdown
Use Fluent UI components as the default for future web UI surfaces. Favor standard components before introducing custom UI, especially for forms, command surfaces, navigation, dialogs, status indicators, and tabular data.

Use Hexalith.FrontComposer-style typed descriptors to drive UI composition where practical.
```

NEW:

```markdown
All Hexalith.Memories web UX implementation must be composed from Hexalith.FrontComposer and Microsoft Fluent UI Blazor V5 components. FrontComposer is the application composition boundary; Fluent UI Blazor V5 is the component primitive boundary.

Raw HTML controls, custom component primitives, JavaScript UI behavior, and third-party UI components are not allowed when a FrontComposer or Fluent UI Blazor V5 component exists. Hand-authored HTML or CSS is allowed only for unavoidable semantic/container structure or layout gaps that neither FrontComposer nor Fluent UI V5 owns, and each exception must be justified and covered by conformance tests.

Use Fluent UI V5 component parameters and Fluent 2 tokens for color, typography, spacing, status, and focus treatment. Do not use legacy Fluent v4/FAST tokens or recreate theme primitives in scoped CSS.
```

Justification: Converts existing design direction into an implementation invariant matching the repository instructions and the user's explicit correction.

### Epics Amendment

Section: `UX Design Requirements`.

OLD:

```markdown
- UX-DR15: Future web UI composition must use Hexalith.FrontComposer patterns and Microsoft Fluent UI Blazor primitives for controls, navigation, forms, grids, dialogs, drawers, tabs, menus, status feedback, layout, and focus behavior.
```

NEW:

```markdown
- UX-DR15: All Memories web UI and UX implementation must use only Hexalith.FrontComposer and Microsoft Fluent UI Blazor V5 components for controls, navigation, forms, grids, dialogs, drawers, tabs, menus, status feedback, layout, focus behavior, and command surfaces. Raw HTML/CSS/JavaScript or third-party UI components are allowed only as explicitly justified gaps when no FrontComposer or Fluent UI V5 component/token exists, and those exceptions must be tracked by conformance tests.
```

### Epic 17 Amendment

Section: `Epic 17: Future Web UX Composition & Accessibility`.

ADD after the scope note:

```markdown
**UX implementation boundary:** Epic 17 web work is FrontComposer-first and Fluent UI Blazor V5-only. Components must consume FrontComposer shell/composition primitives and Fluent UI Blazor V5 primitives before creating Memories-specific wrappers. Raw semantic markup and scoped CSS may be used only for unavoidable container/layout gaps with no component equivalent, and must not recreate Fluent theme primitives, controls, status treatments, typography ramps, color roles, or spacing systems. Any exception requires an explicit conformance-test allowlist entry and removal condition.
```

### Story 17.1 Amendment

Section: Completion Notes.

ADD:

```markdown
Post-completion course correction: Story 17.1 predates the stricter "FrontComposer and Fluent UI Blazor V5 only" UX boundary. The delivered RCL remains a useful contract-bound Evidence Cockpit slice, but raw markup/scoped CSS and non-Fluent-2 token usage must be audited and remediated under Story 17.6. Do not extend the current raw markup/CSS pattern into Stories 17.2-17.5.
```

Justification: Preserves implementation history while preventing the completed story from becoming precedent for weaker UI composition.

### Add Story 17.6

```markdown
### Story 17.6: FrontComposer and Fluent UI Blazor V5 Conformance Hardening

As a maintainer of the Memories web UX,
I want all Memories web components to use only FrontComposer and Fluent UI Blazor V5 components and tokens,
So that Epic 17 cannot drift into a parallel design system or raw HTML/CSS implementation.

Acceptance Criteria:

1. Given the existing `Hexalith.Memories.Web` RCL from Story 17.1, when conformance is audited, then every `.razor` and `.razor.css` file is classified as FrontComposer component usage, Fluent UI Blazor V5 component usage, unavoidable semantic/container markup, or a violation requiring remediation.
2. Given a FrontComposer or Fluent UI Blazor V5 component exists for a control, status indicator, message, badge, stack/layout, grid/list, dialog/drawer, menu, tooltip, input, command surface, tab, or data display, when the Memories web component renders that function, then it uses the component rather than raw HTML or a custom UI primitive.
3. Given hand-authored CSS remains, when it is reviewed, then it contains only layout the design system does not own, uses Fluent 2 tokens where tokens are needed, and does not define theme primitives, direct typography ramps, direct foreground roles, legacy Fluent v4/FAST tokens, or one-off status color systems.
4. Given an exception is unavoidable, when it remains in source, then a conformance allowlist names the file, selector or markup pattern, reason, missing FrontComposer/Fluent primitive, owner story, and removal condition.
5. Given Stories 17.2 through 17.5 are implemented, when their code is reviewed, then they reuse the same conformance tests and cannot add new raw UI/CSS exceptions without an explicit allowlist entry.
6. Given the Fluent UI Blazor package version is checked, when component APIs are selected, then implementation follows the centrally pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` and the aligned `Hexalith.FrontComposer` submodule; incompatible MCP documentation examples are not copied blindly.
7. Given focused validation runs, then `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`, the new conformance tests, and `git diff --check` pass.

Target artifacts:

- `src/Hexalith.Memories.Web/**/*.razor`
- `src/Hexalith.Memories.Web/**/*.razor.css`
- `tests/Hexalith.Memories.Web.Tests/**`
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md`
- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md`
- `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md`
- `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- `_bmad-output/implementation-artifacts/17-5-responsive-and-accessible-web-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Out of scope:

- Broad FrontComposer framework redesign
- Fluent UI package upgrade beyond the current pinned V5 prerelease
- New Evidence Packet semantics
- Backend, CLI, MCP, storage, ingestion, search, or tenant-isolation behavior
- Recursive submodule initialization or casual submodule changes
```

### Story 17.2-17.5 Preflight Amendment

Section: Task 0 package/version bullet in each story.

OLD:

```markdown
Verify the local Fluent UI Blazor package in `references/Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.
```

NEW:

```markdown
Verify the local Fluent UI Blazor package in `Directory.Packages.props` and `references/Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The current aligned package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the available Fluent UI MCP documentation targets `5.0.0.26139` and is incompatible, so local package/submodule code and tests are authoritative when signatures differ.
```

ADD near each Story 17.2-17.5 Task 0:

```markdown
Apply the Epic 17 UX implementation boundary: use FrontComposer and Fluent UI Blazor V5 components/tokens only, and do not add raw HTML/CSS/JavaScript, third-party UI components, legacy Fluent v4/FAST tokens, or handcrafted UI primitives unless the conformance allowlist records an unavoidable gap.
```

### Architecture Amendment

Section: `Interface Philosophy` or `Project Structure & Boundaries`.

ADD:

```markdown
- **Web UI / RCL (Epic 17)** — future web composition surface. `Hexalith.Memories.Web` is a FrontComposer-aligned Razor component library over `Contracts.V1` Evidence Packet semantics. It uses FrontComposer shell/composition primitives and Microsoft Fluent UI Blazor V5 only; it must not become a standalone design system, raw HTML control library, or CSS theme fork. Custom markup/CSS is allowed only for explicitly justified semantic/container gaps and is guarded by conformance tests.
```

## 5. Change Analysis Checklist

- [x] 1.1 Triggering story identified: Epic 17 / Story 17.1 and future Stories 17.2-17.5.
- [x] 1.2 Core problem defined: UX implementation boundary is too soft; existing Story 17.1 code demonstrates raw markup/CSS drift risk.
- [x] 1.3 Evidence gathered: repository instructions, project context, UX spec, epics, Story 17.1 artifact, package pins, Fluent UI MCP version check, and current RCL code scan.
- [x] 2.1 Current epic can still be completed: Epic 17 remains viable.
- [x] 2.2 Epic-level change needed: add hard UX boundary and Story 17.6.
- [x] 2.3 Remaining planned epics reviewed: no MVP/backend/CLI/MCP epics require change.
- [x] 2.4 No epic invalidation: no planned epic becomes obsolete.
- [x] 2.5 Priority: Story 17.6 should run before extending Stories 17.2-17.5 implementation so the raw markup/CSS pattern does not spread.
- [N/A] 3.1 PRD conflict: no MVP requirement conflict; optional PRD clarification only.
- [x] 3.2 Architecture conflict: architecture should document the web RCL boundary and conformance rule.
- [x] 3.3 UI/UX conflict: UX spec must move from "default/where practical" to mandatory FrontComposer/Fluent UI V5-only composition.
- [x] 3.4 Secondary artifacts: story files, sprint status, conformance tests, package-version notes, and existing web RCL files need updates.
- [x] 4.1 Direct Adjustment viable: recommended.
- [N/A] 4.2 Rollback not recommended: Story 17.1 code can be hardened rather than reverted.
- [N/A] 4.3 MVP review not required: future web UI governance only.
- [x] 4.4 Recommended path selected: Direct Adjustment, moderate scope.
- [x] 5.1 Issue summary included.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives included.
- [x] 5.4 PRD/MVP impact stated.
- [x] 5.5 Handoff plan defined.
- [x] 6.3 User approval received from Jerome on 2026-06-24.
- [x] 6.4 Sprint status update approved for Story 17.6 backlog entry.

## 6. Implementation Handoff

Scope classification: Moderate.

Route to: Product Owner / Developer agents.

Responsibilities:

- Product Owner: approve the proposal, add Story 17.6 to Epic 17, update Stories 17.1-17.5 wording, and add the backlog entry to `sprint-status.yaml`.
- Developer: implement Story 17.6 by auditing and refactoring the Memories web RCL to FrontComposer/Fluent UI Blazor V5 conformance, adding conformance tests, and keeping package versions centralized.
- Architect/UX reviewer: review any proposed exception allowlist so exceptions do not become a parallel design system.

Success criteria:

- Planning artifacts state unambiguously that Memories web UX uses only FrontComposer and Fluent UI Blazor V5 components/tokens.
- `src/Hexalith.Memories.Web` no longer contains raw UI/control/status/layout primitives where FrontComposer or Fluent UI V5 equivalents exist.
- Scoped CSS no longer recreates theme primitives or uses legacy Fluent v4/FAST tokens.
- Any unavoidable raw markup/CSS exception is documented, tested, and has a removal condition.
- Focused web tests and conformance scans pass.

## 7. Approval

Decision: approved by Jerome on 2026-06-24.

Approved Direct Adjustment: add Story 17.6, amend Epic 17 UX boundary wording, update Story 17.1 completion notes, update Stories 17.2-17.5 preflight/conformance wording, and add the Story 17.6 backlog entry to sprint status.
