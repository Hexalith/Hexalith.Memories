# Legacy UX evidence extract

## Source authority

- **Primary legacy decision source:** `../../../ux-design-specification.md`. It records a completed UX workflow and explicitly selects a direction, component contracts, state grammar, interaction patterns, responsive behavior, and accessibility target.
- **Illustrative source only:** `../../../ux-design-directions.html`. It is an eight-direction static showcase and labels itself a planning artifact made from HTML/CSS approximations rather than production component code (`ux-design-directions.html:470-478`, `ux-design-directions.html:780`).
- **Controlling repository baseline:** `../../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md`. Production UI must use FrontComposer and Blazor Fluent UI V5; existing components win over raw HTML/CSS/JavaScript; colors, typography, and spacing must come from Fluent component parameters or Fluent 2 tokens (`hexalith-ux-instructions.md:5-39`). Page-like surfaces with two or more sibling titled sections must use a single `FluentAccordion` while titles, navigation, toolbars, and a lone primary region stay outside it (`hexalith-ux-instructions.md:41-51`).

The extract below therefore treats the Markdown specification as normative legacy evidence, the repository baseline as the implementation constraint, and the HTML only as a non-normative visual reference.

## Product and visual identity decisions

- **Promise / north star:** “recoverable trust.” Users must be able to verify scope, source, freshness, retrieval path, confidence limits, and a recovery action without leaving the workflow. Every answer should make the system more inspectable (`ux-design-specification.md:27-37`, `ux-design-specification.md:77-79`).
- **Signature experience:** ingest a case, ask a why-oriented question, receive an evidence packet, inspect why it appeared and where it came from, then recover safely if incomplete. Search is the input; verified understanding is the outcome (`ux-design-specification.md:31-35`, `ux-design-specification.md:83-97`).
- **Character:** calm, technical, utilitarian, professional, compact, work-focused, accessible, and operationally honest. It should resemble dependable infrastructure, not an experimental or mystical AI interface (`ux-design-specification.md:260-262`, `ux-design-specification.md:352-364`, `ux-design-specification.md:482-494`).
- **System inheritance:** FrontComposer is the application-composition boundary and Microsoft Fluent UI Blazor V5 is the component-primitive boundary. Use contract-first, tenant-aware, command-driven composition; do not create a bespoke design system (`ux-design-specification.md:312-342`).
- **Color:** use Fluent theme roles / Fluent 2 tokens, neutral surfaces, restrained accent, and explicit semantic states. Success covers verified/healthy/strong; warning covers stale/weak/partial/pending/truncated; error covers unauthorized/failed/unavailable; info covers explanations/metadata/graph/recovery; neutral covers routine evidence and history. Avoid decorative gradients and novelty palettes (`ux-design-specification.md:440-456`).
- **Typography:** inherit the Fluent scale; optimize for scanning rather than display drama. Reserve monospace for commands, identifiers, paths, event or graph IDs, JSON fields, and CLI/MCP examples (`ux-design-specification.md:458-466`).
- **Layout and density:** compact professional density with an 8px rhythm as a mental model, expressed through Fluent parameters/tokens. Recurrent structures are scope-first workflow, stable evidence-packet anatomy, and list/grid plus inspection detail. Data-heavy surfaces favor Fluent data-grid patterns (`ux-design-specification.md:468-480`).
- **Emphasis:** color clarifies state rather than mood; primary emphasis is sparse and reserved for the next safe action. Trust-critical status must also use text and semantic indicators, never color alone (`ux-design-specification.md:444-456`, `ux-design-specification.md:484-492`).
- **Anti-patterns:** chat-only answers, decorative AI mystique, dashboard sprawl, generic enterprise heaviness, different trust semantics per surface, silent partial failure, and confident claims without inspectable evidence (`ux-design-specification.md:282-294`, `ux-design-specification.md:527-537`).

## Canonical component and alias map

Use the component-section headings as canonical names. Other phrases below are prose aliases or variants, not confirmed C# component type names.

| Canonical name | Legacy aliases / variants | Extracted contract |
| --- | --- | --- |
| **Evidence Packet** | evidence object; packet; compact packet; detailed inspection; MCP schema preview | Product unit for an answer, ranked result, briefing, or diagnostic. Stable anatomy: trust strip, claim/result, evidence summary, sources, reasoning, confidence/freshness/health, optional graph context, recovery. States: supported, partial, disputed, insufficient, degraded, unauthorized, pending expansion (`ux-design-specification.md:539-576`, `ux-design-specification.md:818-827`). |
| **Trust Strip** | scope strip; badge row; high-risk warning strip | Mandatory before the answer. Shows tenant, case, confidence state, freshness state, source count, evidence health, and optional token-budget status. `scope strip` in the anatomy is a naming inconsistency; normalize it to **Trust Strip** (`ux-design-specification.md:543-554`, `ux-design-specification.md:829-838`). |
| **Scope Header** | persistent page header; locked context rail; compact mobile header; containment frame | Keeps tenant, case, permission, isolation, and recent scope visible. Scope expansion needs deliberate confirmation (`ux-design-specification.md:565-567`, `ux-design-specification.md:840-849`). |
| **Retrieval Axis Breakdown** | explain breakdown; axis breakdown; score stack; detailed axis table | Explains syntactic, semantic, and graph contributions, including active/excluded/unavailable/degraded/low/conflicting states. Bars or scores require text equivalents (`ux-design-specification.md:851-860`). |
| **Source Citation Stack** | source list; source stack; citation list; source table; source viewer | Bounded, attributed sources with type, snippet, timestamp, freshness, confidence, and actions. Supports available, missing, stale, redacted, unauthorized, and conflicting states (`ux-design-specification.md:862-871`). |
| **Graph Path Summary** | graph summary; graph context; graph detail; path summary | Linear inspectable causal/relational chain with nodes, relationships, gaps, per-edge confidence, and depth. A text narration is required for accessibility (`ux-design-specification.md:873-882`). |
| **Recovery Action Panel** | recovery footer; Recovery Footer Pattern; inline empty-state panel; critical-action panel | Cause, safest next action, secondary actions, and diagnostic context for every incomplete, empty, stale, degraded, compressed, or unauthorized packet. **Recovery footer** is a placement/pattern alias, not a separate component (`ux-design-specification.md:567`, `ux-design-specification.md:884-893`, `ux-design-specification.md:1039-1041`). |
| **Agent Packet Inspector** | developer inspector; agent preview; MCP schema inspector | Both a task-specific view and a component composition for request/response schema, budget, omitted fields, expansion handles, and structured errors (`ux-design-specification.md:521`, `ux-design-specification.md:895-904`). |
| **Case Activity Trail** | activity trail; timeline; grouped activity table; briefing history | Both a task-specific continuity view and a component composition for memory evolution, ingestion, changes, annotations, actors, sources, and evidence impact (`ux-design-specification.md:519`, `ux-design-specification.md:906-915`). |

Roadmap-only component names without full legacy contracts are **Ingestion Lifecycle Tracker**, **Operator Health Matrix**, and **Benchmark Result Comparator**. They must remain placeholders until their anatomy, states, behavior, and accessibility are specified (`ux-design-specification.md:927-948`).

## Information architecture and surfaces

### Cross-surface foundation

- CLI, MCP, and web are first-class presentation surfaces over one evidence and error model. Their differences are density, formatting/schema, and exploration depth—not semantics (`ux-design-specification.md:99-113`).
- Delivery horizon matters: MVP is CLI-first; the shared packet/state grammar comes first; MCP/EventStore follows in Phase 1.5; FrontComposer/Fluent web composition is future work unless scope is explicitly changed (`ux-design-specification.md:101-103`, `ux-design-specification.md:578-582`).
- CLI optimizes for keyboard use, compact explain output, diagnostics, scope visibility, and scriptable formats. MCP optimizes for typed bounded packets, deterministic expansion, source attribution, budget visibility, and structured recovery. Web optimizes for human verification, source review, causal exploration, case context, and confidence cues (`ux-design-specification.md:105-111`).
- There is no offline requirement (`ux-design-specification.md:113`).

### Web/task views

The selected IA is one model with task-specific views, not eight separate products:

1. **Evidence Cockpit** — primary/default search and verification view; answers “Can I trust this answer and act?” (`ux-design-specification.md:515-517`, `ux-design-specification.md:569-574`).
2. **Case Activity Trail** — adopted continuity/history view for “How did this memory evolve?” (`ux-design-specification.md:519`, `ux-design-specification.md:572`).
3. **Agent Packet Inspector** — adopted MCP/schema diagnostic view for “Did the tool contract behave correctly?” (`ux-design-specification.md:521`, `ux-design-specification.md:573`).
4. **Operator Console** — specialized tenant, isolation, health, ingestion, backend, blast-radius, and repair view (`ux-design-specification.md:523`, `ux-design-specification.md:574`).

Supporting lenses are **Case Briefing** for Marcus/Priya and onboarding continuity, and **Command First** for Alex’s expert search/explain/debug flow (`ux-design-specification.md:604-610`). **Graph Evidence Studio** and **Onboarding Proof Path** were explored but not selected as peer top-level products; retain their graph-exploration and guided-proof ideas as patterns until explicitly promoted (`ux-design-specification.md:498-525`).

### Responsive form factor

- Desktop is the richest multi-panel inspection surface. Tablet reduces simultaneous density and shifts detail to drawers/panels. Mobile prioritizes review, trust inspection, and recovery; advanced configuration, graph work, operator repair, and schema inspection move to focused/full-screen panels (`ux-design-specification.md:1068-1080`).
- Legacy breakpoints: mobile 320–767px, tablet 768–1023px, desktop 1024px+, wide desktop 1440px+. These remain provisional where FrontComposer conventions differ (`ux-design-specification.md:1082-1091`).
- Collapse order: columns to one; secondary inspection to overlays; data grids to fewer columns/expandable rows; secondary commands to overflow; Trust Strip wraps but remains before the answer; Recovery Action Panel stays with the affected state (`ux-design-specification.md:1093-1102`).

## State patterns

### Shared trust grammar

- **Confidence:** supported, partial, disputed, insufficient.
- **Freshness:** current, aging, stale, unknown.
- **Evidence health:** complete, degraded, missing source, schema mismatch.
- **Scope:** verified, inferred, cross-case, unauthorized, out-of-scope.

These text states are canonical and must accompany—not be replaced by—percentages (`ux-design-specification.md:558-563`, `ux-design-specification.md:1043-1050`). Confidence describes evidence strength and retrieval quality, not factual certainty (`ux-design-specification.md:85-89`).

### Operation and failure states

- Long-running command lifecycle: pending, running, succeeded, failed, degraded, recoverable; onboarding ingestion stages additionally use queued, extracting, embedding, indexed (`ux-design-specification.md:344-348`, `ux-design-specification.md:616-649`).
- Search/absence must distinguish no match, low evidence, missing/delayed ingestion, stale memory, inaccessible tenant/case, backend degradation, and wrong scope. Conflicting signals or sources stay visible rather than being smoothed away (`ux-design-specification.md:127-139`, `ux-design-specification.md:650-683`).
- Designed bad paths include missing sources, conflicting evidence, partial graph loading, unavailable MCP schema, failed recovery, unauthorized scope, graph gaps, token-budget truncation, and backend degradation (`ux-design-specification.md:576`, `ux-design-specification.md:996-1005`).
- Dynamic feedback answers: what happened, what it affects, severity, and next action. Trust-critical feedback stays attached to the packet/object, not only in a global notification (`ux-design-specification.md:963-972`).

## Interaction rules

- **Scope first:** establish and validate tenant, case, authorization, and isolation before retrieval/action. Missing, ambiguous, unauthorized, or inconsistent scope blocks or warns; visual scope is not the enforcement mechanism (`ux-design-specification.md:131-133`, `ux-design-specification.md:166-167`, `ux-design-specification.md:422-436`).
- **One-query trust loop:** automatically perform source lookup, evidence-strength assessment, axis explanation, freshness assessment, and relevant graph traversal. The first response is bounded but sufficient to trust, challenge, refine, or recover (`ux-design-specification.md:115-129`).
- **Progressive disclosure:** trust essentials come first; source details, scoring, graph paths, activity, token-budget details, and backend diagnostics expand on demand (`ux-design-specification.md:97`, `ux-design-specification.md:600-602`, `ux-design-specification.md:1031-1033`).
- **Recovery in context:** every weak, empty, stale, degraded, unauthorized, or compressed packet ends with one safest next action plus optional secondary actions (`ux-design-specification.md:127-133`, `ux-design-specification.md:1039-1041`).
- **Navigation continuity:** preserve tenant/case and search context, and keep a clear return path from sources, graph nodes, activity items, and packet details (`ux-design-specification.md:985-994`).
- **Confirmation:** deliberately confirm destructive, permission-sensitive, trust-sensitive, or scope-expanding actions and name tenant, case, target, and consequence (`ux-design-specification.md:952-961`, `ux-design-specification.md:1018-1027`, `ux-design-specification.md:1060-1062`).
- **Overlays:** use side panels/drawers for focused inspection and dialogs for confirmation; do not replace navigable core structure. Move focus into an overlay and return it to the invoker on close (`ux-design-specification.md:1018-1027`).
- **Search/filter effects:** show when filters narrow/broaden scope, exclude axes, or change confidence; active filters remain visible, with advanced mobile filters in a panel/drawer (`ux-design-specification.md:1007-1016`).
- **Cross-surface invariance:** scope, source, reasoning, state, recovery, omitted details, and degraded behavior mean the same thing in CLI, MCP, and web (`ux-design-specification.md:917-925`, `ux-design-specification.md:1064-1066`).

## Accessibility floor

- Target WCAG 2.2 AA for web, treating trust-state accessibility as correctness (`ux-design-specification.md:1104-1108`).
- Full keyboard path: scope selection, query, search, packet inspection, source/reasoning expansion, graph context, recovery, and return. Do not use pointer- or hover-only interactions (`ux-design-specification.md:1108-1116`, `ux-design-specification.md:1158-1162`).
- Status uses text plus an accessible icon/indicator; bars and percentages need text equivalents. Packets are labelled regions, grids have headers/row actions, and meaningful async transitions use restrained live regions (`ux-design-specification.md:484-492`, `ux-design-specification.md:1108-1114`).
- Predictable focus, visible focus, reduced motion, forced-colors/high contrast, readable selected states, and preserved borders are mandatory (`ux-design-specification.md:1114-1118`, `ux-design-specification.md:1156-1160`).
- Primary touch targets should reach 44px where practical; primary actions remain available as secondary actions move to overflow (`ux-design-specification.md:952-961`, `ux-design-specification.md:1156`).
- Accessible names, tooltips, announcements, and copied text must not expose secrets or restricted tenant diagnostics (`ux-design-specification.md:1162-1164`).

## Named personas and journeys

| Protagonist | Need / journey | Climax beat evidenced in the legacy flow |
| --- | --- | --- |
| **Alex**, senior .NET/DAPR developer | Zero to First Evidence Packet: prerequisites → tenant/case → ingest → searchable → why-search → inspect trust (`ux-design-specification.md:41`, `ux-design-specification.md:616-649`). | Alex reaches “trust sufficient” after inspecting answer, sources, reasoning, freshness, and recovery, then decides Memories can replace fragile plumbing. |
| **Alex** | Weak/Empty Result Recovery: validate scope → classify strong/weak/none → diagnose cause → choose recovery (`ux-design-specification.md:650-683`). | A blank or weak result becomes actionable when Alex understands the cause and safely refines, fixes ingestion/scope, inspects health, or exports/escalates. |
| **LLM Agent** | MCP Evidence Packet Consumption: send scoped bounded request → receive full/compressed typed packet → evaluate trust → expand/refine/escalate (`ux-design-specification.md:43`, `ux-design-specification.md:684-713`). | The agent can answer with sourced context—or follows an explicit structured recovery—without guessing or parsing prose. |
| **Kenji**, operator | Tenant Verification and Degraded Backend Recovery: checks → issue class/blast radius → deliberate recovery → verify outcome (`ux-design-specification.md:47`, `ux-design-specification.md:714-744`). | A successful repair returns Kenji to a timestamped verified state; failed recovery produces an exportable diagnostic packet. |
| **Marcus**, team lead | Case Briefing and Activity Continuity: inspect health/activity → repair stale/sparse/conflicting state → generate sourced briefing → inspect or share (`ux-design-specification.md:45`, `ux-design-specification.md:745-770`). | Marcus deems the briefing sufficient and shares/uses it for onboarding or decisions, with sources still inspectable. |
| **Priya**, downstream beneficiary | Needs understandable, trustworthy case context without infrastructure concepts; only a downstream Case Briefing pattern is assigned (`ux-design-specification.md:49`, `ux-design-specification.md:604-610`). | **Gap:** no complete named Priya journey or climax was captured. |

## Unresolved decisions and conflicts to carry into migration

1. **HTML palette vs design-system inheritance:** the HTML hard-codes neutrals, brand/status colors, graph purple, shadow, radius, spacing, Segoe UI, and Cascadia Mono (`ux-design-directions.html:8-28`). This conflicts with both the legacy Markdown’s “no separate custom palette” decision and the repository’s no-theme-redefinition rule (`ux-design-specification.md:440-464`; `hexalith-ux-instructions.md:18-36`). Do not migrate the literal values.
2. **HTML implementation vs production boundary:** the showcase uses raw controls, custom CSS, fixed layout dimensions, and JavaScript tab switching (`ux-design-directions.html:48-456`, `ux-design-directions.html:790-800`). The file itself says these are static approximations (`ux-design-directions.html:780`), and the repository requires FrontComposer / Fluent UI V5 first. Preserve information hierarchy only.
3. **Eight explored directions vs selected IA:** the HTML makes all eight directions navigable (`ux-design-directions.html:470-478`), but the Markdown selects Evidence Cockpit plus Activity Trail and Agent Inspector, with Operator Console specialized (`ux-design-specification.md:513-525`). Graph Studio and Onboarding Proof Path are inspiration, not confirmed top-level navigation.
4. **State labels vs numeric-only mocks:** HTML cards show `Evidence 0.91`, `Evidence 0.68`, and `Confidence 0.87` (`ux-design-directions.html:521-531`, `ux-design-directions.html:698-702`). The normative grammar requires supported/partial/disputed/insufficient and says percentages cannot stand alone (`ux-design-specification.md:558-563`). Keep numerical detail only as a subordinate signal.
5. **Naming:** packet anatomy says “Scope strip” while the custom component is **Trust Strip** (`ux-design-specification.md:543-554`, `ux-design-specification.md:829-838`). Normalize to Trust Strip; retain Scope Header as the persistent workspace context.
6. **Component code names:** the legacy document names conceptual domain compositions but never confirms actual FrontComposer or Fluent component APIs. Mapping canonical UX concepts to existing components remains implementation discovery; do not infer new low-level primitives (`ux-design-specification.md:806-814`, `ux-design-specification.md:917-925`).
7. **Brand gaps:** no logo, iconography system, brand asset, product-specific typeface, dark-mode token delta, elevation scale, or final shape system is selected. HTML shadow/radius values are mock-only. Motion is constrained by reduced-motion requirements but otherwise unspecified.
8. **Journey gap:** Priya has a need and a supporting view but no narrated end-to-end journey. The other journeys are role-based system flows rather than rich situational narratives; personal context beyond job role is absent.
9. **Web timing:** “CLI, MCP, and web are first-class” is full-horizon guidance, while web is explicitly deferred beyond MVP (`ux-design-specification.md:99-113`). Keep both statements so downstream planning does not mistake design parity for current delivery scope.
10. **Page composition delta:** the legacy mock panels do not encode the repository’s newer `FluentAccordion` rule for multiple sibling titled sections. The migrated EXPERIENCE contract must add that inherited behavior even though the HTML directions do not show it (`hexalith-ux-instructions.md:41-51`).

## Reusable HTML ideas — non-normative only

- **Evidence Cockpit composition:** persistent scope/status row, query near the top, evidence list beside inspection details, axis breakdown, and nearby next action (`ux-design-directions.html:496-552`).
- **Continuity and briefing compositions:** activity timeline with status/sidebar, and narrative briefing beside a bounded evidence stack (`ux-design-directions.html:554-622`).
- **Operator and expert compositions:** tenant health matrix with row actions, and compact command/result/shortcut layout (`ux-design-directions.html:624-679`).
- **Graph and agent compositions:** relationship workspace beside selected-edge detail, and payload preview beside budget/omission/safety states (`ux-design-directions.html:680-744`). The graph drawing itself is decorative positioning, not an accessible graph specification.
- **Onboarding proof composition:** staged progress, first packet, and recovery guidance shown together (`ux-design-directions.html:746-779`).
- **Responsive idea:** collapse rail and multi-column layouts into one column and stack toolbar/header controls (`ux-design-directions.html:440-456`). Treat the exact 980px breakpoint as mock-specific.
- **Visual mood:** neutral surfaces, thin boundaries, compact panels, restrained status emphasis, and a modest information density are useful directional references. Exact hex colors, sizes, radius, shadows, type choices, and grid widths are not reusable tokens (`ux-design-directions.html:8-28`, `ux-design-directions.html:48-435`).
- **Accessibility caveat:** the mock’s `aria-label`/`aria-selected` examples show intent (`ux-design-directions.html:470-478`, `ux-design-directions.html:509`, `ux-design-directions.html:655`), but its tab-switching script is not a production navigation pattern and does not establish complete keyboard, focus, live-region, or graph semantics (`ux-design-directions.html:784-800`).

No source file was modified. This extract is evidence for distillation into `DESIGN.md` and `EXPERIENCE.md`; those spines must remain the final authority.
