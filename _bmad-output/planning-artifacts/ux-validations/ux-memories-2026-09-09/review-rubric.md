# Spine Pair Review — memories

## Overall verdict

**Broken as a downstream UX contract.** The legacy specification contains a coherent trust-oriented product direction and several useful component dossiers, but it is not a `DESIGN.md` / `EXPERIENCE.md` spine pair, cannot be mechanically consumed as one, covers only five of the current PRD's ten named journeys, and has drifted from current phase, CLI, authorization, scoring, and state contracts. Converting it is not a filename-only migration: source reconciliation, IA closure, tokenization, and explicit decisions on the open explain contract are required first.

**Finding counts:** critical 4 · high 15 · medium 9 · low 2.

## 1. Flow coverage — broken

Checked the current PRD's ten named journeys and journey coverage summary against all five Mermaid flows in the legacy UX specification, including protagonist naming, step structure, climax, and bad paths.

### Findings

- **critical** The current source defines J1–J10, but the UX has only five flows. J6 (Kenji scaling), J8 (Priya application experience), and J10 (Dani contributor experience) are absent; J1 and J9 are blended into one generic Alex onboarding flow, J2 is reduced to weak-result recovery rather than the handler/replay debug path, J3 and J7 are blended into one agent flow, and J4 is renamed/genericized as Marcus briefing (PRD `prd.md:496`; UX `ux-design-specification.md:614`). *Fix:* create one traceable Key Flow per current journey name, or explicitly mark a journey as non-UX/infrastructure and record why it is excluded.
- **high** None of the five UX flows uses the required numbered-step Key Flow form or labels a climax beat. The Mermaid graphs convey sequence and include useful branches, but downstream consumers cannot extract a stable step list and climax assertion from them (UX `ux-design-specification.md:620`, `:654`, `:688`, `:718`, `:749`). *Fix:* retain diagrams as optional illustrations and add numbered steps, an explicit **Climax**, and an applicable failure/recovery path beneath every Key Flow.
- **medium** Flow titles and phase identity do not preserve source names verbatim. Most importantly, `Alex: Zero to First Evidence Packet` does not distinguish the PRD's Phase 1.5 EventStore Journey 1 from the Phase 1 file/URL/CLI Journey 9, whose clocks and evidence are deliberately different (PRD `prd.md:301`, `:460`; UX `ux-design-specification.md:616`). *Fix:* use the exact source journey names and phase labels, with source identifiers alongside each flow.

## 2. Token completeness — broken

Checked for a `DESIGN.md` YAML token tree, token types, prose token references, UI-system inheritance, light/dark handling, and contrast targets. Because no `DESIGN.md` exists, the legacy visual prose and the showcase CSS were treated as the candidate visual contract rather than as if token files existed.

### Findings

- **critical** There is no `DESIGN.md` and therefore no machine-readable `name`, `description`, `colors`, `typography`, `rounded`, `spacing`, or `components` token contract. The legacy document states Fluent inheritance and an 8px rhythm only in prose, which is not resolvable by downstream code or AI consumers (UX `ux-design-specification.md:438`; `:458`; `:468`). *Fix:* create a real `DESIGN.md` frontmatter contract that inherits Fluent UI Blazor V5 / Fluent 2 by name and records only justified Memories deltas and component token mappings.
- **high** The only renderable visual reference creates a parallel bespoke variable palette (`--neutral-*`, `--brand`, `--success`, `--warning`, `--danger`, `--info`, `--graph`) plus literal state colors, despite the UX contract requiring Fluent 2 tokens and prohibiting recreated theme primitives. Its planning-only disclaimer does not define how those values map back to the inherited system (HTML `ux-design-directions.html:8`, `:103`, `:225`; UX `ux-design-specification.md:338`). *Fix:* replace artifact-local style names with Fluent 2 semantic-role references or document the showcase as non-normative and map every retained visual decision to a valid DESIGN token.
- **medium** WCAG 2.2 AA and forced-colors support are stated, but no load-bearing foreground/background combinations have named contrast targets or verified values; the showcase declares light-only color scheme with no dark-mode decision (UX `ux-design-specification.md:482`, `:1104`; HTML `ux-design-directions.html:8`). *Fix:* name the critical scope, warning, error, evidence, link, focus, and selected-state combinations and record their contrast requirements under inherited Fluent tokens, including the intended theme modes.

## 3. Component coverage — thin

Extracted the named Fluent/FrontComposer primitives, nine detailed custom component sections, three additional roadmap components, recurring pattern components, and all component-like names used in flows and the showcase.

### Findings

- **high** Component rules are co-mingled in one legacy section; no component has both a `DESIGN.md.Components` visual row and an `EXPERIENCE.md.Component Patterns` behavioral row. The nine custom dossiers are useful, but consumers must parse prose fields and guess which statements are visual versus behavioral (UX `ux-design-specification.md:804`, `:818`). *Fix:* split each retained custom component into identically named visual and behavioral entries in the peer contracts.
- **high** `Ingestion Lifecycle Tracker`, `Operator Health Matrix`, and `Benchmark Result Comparator` are declared as custom roadmap components without the purpose/anatomy/state/interaction specifications given to the preceding components. The broad Fluent component inventory is likewise only a name list rather than per-component delta rules (UX `ux-design-specification.md:810`, `:927`). *Fix:* either supply real two-spine rows for these components or mark them as inherited/future concepts and remove them from the implementation-ready inventory.
- **medium** Component aliases are not normalized: `Scope strip` versus `Trust Strip`, `Recovery footer` versus `Recovery Action Panel`, and `evidence state` versus `evidence health` may denote the same concepts but are not declared as aliases (UX `ux-design-specification.md:545`, `:567`, `:818`, `:829`, `:884`). *Fix:* choose one canonical name for every component and field, then use it identically in anatomy, states, patterns, journeys, and sources.

## 4. State coverage — broken

Walked every named task view and showcase direction against the state material for cold load, empty, focus, validation/error, degradation, permission denial, long-running work, and recovery. Offline was not treated as required because the legacy strategy explicitly says there is no offline requirement.

### Findings

- **high** There is no Information Architecture surface table or closure proof. The selected direction names Evidence Cockpit, Case Activity Trail, Agent Packet Inspector, and Operator Console, while the showcase adds Briefing, Command First, Graph Studio, and Onboarding, but none records `reached from`, route/entry, purpose, or the journey that lands there (UX `ux-design-specification.md:496`, `:513`; HTML `ux-design-directions.html:471`). *Fix:* build an IA table and prove every stated need lands on a surface and every surface is exercised by a Key Flow.
- **high** State guidance is rich but generic. The one catch-all section lists no-result, pending ingestion, weak evidence, inaccessible scope, degradation, and graph gaps, yet it does not map cold-load, empty, focus, validation/error, permission-denied, and recovery states across each IA surface (UX `ux-design-specification.md:996`). *Fix:* add a surface-by-state matrix with only applicable states, explicit entry/exit behavior, microcopy, and recovery action.
- **medium** Ingestion state labels are stale and internally inconsistent. The onboarding flow uses `queued, extracting, embedding, indexed`, and the generic state section uses `queued, extracting, embedding, indexing`; the current source contract is exactly `pending, extracting, embedding, projecting, indexed, failed`, with retry/dead-letter/repair as detail rather than states (UX `ux-design-specification.md:630`, `:1002`; PRD `prd.md:93`, `:1001`). *Fix:* adopt the current source vocabulary verbatim and show the shipped enum mapping only where implementation consumers need it.

## 5. Visual reference coverage — adequate

Inventoried visual artifacts in and around the supplied UX: there are no `imports/`, `mockups/`, or `wireframes/` directories; the sole visual artifact is the root-level `ux-design-directions.html`, containing eight static directions.

### Findings

- **medium** The specification links the showcase once and explains all eight directions, so the artifact is not orphaned; however, it is not linked inline from the relevant component, state, flow, and responsive sections, and the contract never states that the textual spine wins on conflict (UX `ux-design-specification.md:500`; HTML `ux-design-directions.html:780`). *Fix:* classify the artifact under `mockups/` or `wireframes/`, link it where its layouts are specified, name exactly what each screen illustrates, and state precedence once.
- **low** The HTML remains an exploration set rather than a final visual reference for the chosen blended direction. No promoted key-screen artifact demonstrates the combined Evidence Cockpit + activity + agent + operator model selected in prose (UX `ux-design-specification.md:513`; HTML `ux-design-directions.html:470`). *Fix:* promote or render the minimum chosen key screens and label unmocked IA surfaces as spine-only.

## 6. Bloat & overspecification — thin

Checked the 1,164-line legacy specification for source restatement, duplicated decisions, narrative not tied to downstream choices, repeated prose, and pixel detail that should be tokenized.

### Findings

- **high** The artifact restates large portions of the product vision, target users, emotional objectives, and trust thesis, then repeats `Core User Experience` in two separate sections. This both bloats extraction and preserves a May snapshot of source content that has since changed (UX `ux-design-specification.md:25`, `:81`, `:187`, `:366`). *Fix:* keep personas, requirements, phase definitions, and success gates in sources; retain only their UX consequences and source identifiers in the new spines.
- **medium** All eight exploratory directions remain described after a chosen blended direction is committed, leaving downstream consumers to distinguish alternatives from the contract (UX `ux-design-specification.md:496`, `:513`). *Fix:* move rejected/exploratory directions into a non-normative artifact note and keep only adopted/rejected decisions in the spine.
- **medium** Scope, evidence, state, recovery, progressive disclosure, and accessibility rules recur across packet invariants, implementation approach, component dossiers, consistency patterns, and responsive guidance (UX `ux-design-specification.md:539`, `:578`, `:804`, `:950`, `:1068`). *Fix:* establish each invariant once, reference it by name, and use tables for component- or surface-specific deltas.

## 7. Inheritance discipline — broken

Resolved every legacy `inputDocuments` entry, compared source journey/glossary/phase/CLI/authorization/scoring/state names with the current PRD, and checked whether downstream UX requirement identifiers resolve back to the UX artifact.

### Findings

- **critical** Tenant safety semantics are not fail-closed. The UX allows an unauthorized or out-of-scope request to “block or warn” and describes confirmation before expanding cross-tenant scope, whereas the current contract requires requests against another tenant to be rejected based on authenticated tenant claims (UX `ux-design-specification.md:426`, `:658`, `:565`; PRD `prd.md:1127`, `:1130`). *Fix:* reserve warning/confirmation for non-security case/filter changes; unauthorized or tenant-mismatched operations must refuse and expose a safe, non-leaking recovery path.
- **high** Source inheritance is stale and partially broken. Legacy `inputDocuments` is not the current `sources:` contract, omits current `addendum.md`/change-control context, and references a missing `references/Hexalith.Commons/_bmad-output/project-context.md` (UX `ux-design-specification.md:1`). *Fix:* create `sources:` in both spines, resolve paths from the workspace, remove or replace the missing Commons reference, and re-extract the current PRD/addendum decisions.
- **high** CLI grammar is inconsistent with the current source of truth. The UX uses `memories search "…" --case … --explain`, while the showcase uses a pseudo-command `search claims-q1: …`; the current grammar is `memories search query --tenant <t> --query "<q>" [--case <c>] [--axis <a>] [--explain]` (UX `ux-design-specification.md:424`; HTML `ux-design-directions.html:655`; PRD `prd.md:299`, `:881`). *Fix:* mirror the canonical CLI grammar verbatim in flows, examples, empty-state copy, and mocks.
- **high** Phase inheritance is not extract-safe. The UX's onboarding flow merges “install package or run local AppHost,” content ingestion, and causal evidence into one unphased path, while the current PRD deliberately separates the Phase 1 file/URL CLI thesis path (J9) from Phase 1.5 EventStore package/causal onboarding (J1); MCP and browser work also have distinct later activation gates (UX `ux-design-specification.md:622`; PRD `prd.md:301`, `:460`, `:1186`). *Fix:* phase-tag every flow, surface, and component and keep the two onboarding clocks separate.
- **high** The automatic-explain rule is a live contradiction, not a committed inherited decision. UX requires explain breakdown and graph context automatically on the first response, while the current PRD retains `--explain` as an option and explicitly leaves UX-DR7 versus FR19 open for UX resolution (UX `ux-design-specification.md:117`, `:404`; PRD `prd.md:1201`). *Fix:* resolve the open question, then specify exactly which trust fields are always present and which require `--explain` per surface.
- **high** Scoring semantics drift from the current weighted-RRF contract. The UX describes normalized axis scores and the HTML presents 0.82/0.91/0.76 without saying whether they are raw single-axis values or hybrid rank contributions; current source says hybrid per-axis scores are weighted reciprocal-rank contributions and never raw magnitudes (UX `ux-design-specification.md:428`, `:855`; HTML `ux-design-directions.html:540`; PRD `prd.md:560`). *Fix:* use the PRD's exact single-axis versus hybrid score semantics and label every displayed number with its meaning.
- **high** Confidence language is not glossary-safe. `evidence strength`, unqualified `confidence`, component state labels, and edge confidence are interleaved, while current sources distinguish relevance confidence, metadata confidence, and edge confidence and require the relevance-not-factual-accuracy caveat (UX `ux-design-specification.md:418`, `:558`, `:866`, `:877`; PRD `prd.md:82`, `:549`). *Fix:* import the glossary nouns verbatim and bind each field/state to exactly one confidence type.
- **medium** The current PRD and epics refer to `UX-DR1`–`UX-DR40`, including an open decision named `UX-DR7`, but no `UX-DR*` identifier exists in the UX source itself (PRD `prd.md:1201`; epics `epics.md:231`; UX `ux-design-specification.md`, no matches). *Fix:* either establish durable requirement IDs in `EXPERIENCE.md` or replace downstream references with stable section anchors.
- **medium** The glossary is not explicitly inherited, so nouns such as `Evidence Packet`, `Case`, `Tenant`, `Axis`, and ingestion state are repeatedly redefined rather than referenced; this enabled the phase, state, and confidence drift above (PRD `prd.md:78`; UX `ux-design-specification.md:25`, `:539`, `:1043`). *Fix:* cite the source glossary and record only UX-specific presentation or behavior deltas.

## 8. Shape fit — broken

Compared the supplied single legacy UX file against the canonical `DESIGN.md` section order and all required `EXPERIENCE.md` defaults, including triggered Inspiration and Responsive sections.

### Findings

- **critical** No peer `DESIGN.md` / `EXPERIENCE.md` contract exists. The single legacy file mixes visual identity, behavior, source narrative, journeys, and component implementation, so downstream consumers cannot select the visual or behavioral spine independently and there are no `{path.to.token}` cross-contract references (workspace artifact inventory; UX `ux-design-specification.md:16`). *Fix:* reconcile sources and distill two peer contracts with shared sources and stable canonical names.
- **high** The candidate visual material does not fit the DESIGN spine: no YAML token frontmatter and no canonical Brand & Style, Elevation & Depth, Shapes, Components, or Do's and Don'ts sequence. Analogous Color, Typography, and Layout prose exists but is not machine-readable (UX `ux-design-specification.md:438`, `:804`). *Fix:* move visual decisions into canonical DESIGN order and omit only sections that are genuinely inapplicable under Fluent inheritance.
- **high** The candidate behavior material lacks the required Foundation, Information Architecture, Voice and Tone, Component Patterns, State Patterns, Interaction Primitives, Accessibility Floor, and Key Flows shape. Inspiration and responsive/accessibility material is present and applicable, but it cannot compensate for the missing defaults (UX `ux-design-specification.md:252`, `:614`, `:950`, `:1068`). *Fix:* distill the behavior into the required EXPERIENCE sections, adding product-specific trust/evidence sections only where they earn their place.
- **low** Legacy frontmatter records workflow completion but has no `name`, `status: final`, `sources`, or current `updated` contract fields; its completion timestamp predates the current PRD by nearly four months (UX `ux-design-specification.md:1`; PRD `prd.md:1`). *Fix:* give both peer files complete current frontmatter and treat legacy workflow metadata as migration provenance, not active status.

## Mechanical notes

- **Severity totals:** critical 4 · high 15 · medium 9 · low 2.
- **Artifact inventory:** `ux-design-specification.md` and one linked root-level `ux-design-directions.html`; no `DESIGN.md`, `EXPERIENCE.md`, `.memlog.md` in the validation workspace, `imports/`, `mockups/`, or `wireframes/` artifacts were found for this legacy contract.
- **Broken source reference:** `references/Hexalith.Commons/_bmad-output/project-context.md` from legacy frontmatter does not exist. The other six listed input paths resolve.
- **Name inconsistencies:** `Scope strip` / `Trust Strip`; `Recovery footer` / `Recovery Action Panel`; `evidence state` / `evidence health`; unqualified `confidence` / `evidence strength` / `relevance confidence`; `queued` / `pending`; `indexing` / `projecting`.
- **Cross-references:** no `{path.to.token}` references exist because there is no DESIGN token tree. `UX-DR1`–`UX-DR40` are downstream-only labels and cannot be resolved in the UX source.
- **Frontmatter:** legacy workflow metadata is syntactically valid YAML, but it is not the DESIGN/EXPERIENCE frontmatter contract and one input path is broken.
- **Mermaid:** five `flowchart TD` blocks have balanced fences and no obvious syntax defect. Their mechanical syntax is adequate; their contract shape still lacks numbered steps and explicit climax labels.
