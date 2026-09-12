# Latest UX Reconciliation — 2026-09-12

## Sources

- `ux-designs/ux-memories-2026-09-12/DESIGN.md`
- `ux-designs/ux-memories-2026-09-12/EXPERIENCE.md`

## Verdict

The completed UX spines are primarily downstream presentation and interaction contracts, but they contain five product-contract deltas that belong in the PRD. The earlier “migration scaffolding / no product delta” assessment is superseded.

## Imported into the PRD

1. **Phase accuracy:** Priya/Journey 8 is an authoritative Phase 2 downstream application journey, despite its stale source heading.
2. **Current CLI delivery evidence:** only `ingest`, `traverse`, `case`, and `explore` are current top-level `NotImplementedCommand` placeholders. Target `tenant create/delete/verify` and `status --case/--failed` commands are absent, not individually registered stubs. Current diagnostics use `status telemetry`, `handlers list`, and `handlers mismatches`.
3. **Evidence semantics:** the versioned packet states are `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, and `pendingExpansion`. “No results” must distinguish true absence, empty scope, wrong scope, incomplete or delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation; unauthorized responses disclose no restricted evidence.
4. **Active CLI accessibility:** stable linear reading order, text-equivalent state and recovery semantics, bounded/wrappable human output, deterministic redirected output, durable progress/failure lines, keyboard-only interaction, and protection of secrets/restricted identifiers become NFR37.
5. **Future-web activation evidence:** NFR32 now names 200% text resize, 400% zoom/320-CSS-pixel reflow, focus-not-obscured, theme/forced-colors/reduced-motion/input-mode coverage, supported browser/assistive-technology checks, and manual evidence. WCAG 2.2 SC 2.5.8 is the author-sized pointer-target floor.

## Preserved as an Open Decision

Compact scope, source, relevance caveat, freshness/degradation, omission, and recovery remain always visible. Detailed ranks, weights, matched terms, and graph diagnostics remain opt-in until PRD Open Question 3 is resolved before web activation.

## Downstream-Only Detail Not Imported

FrontComposer/Fluent component ownership, composition anatomy, inherited design roles, page modes, visual sequencing, and future-host route/layout choices remain in the UX spines. They do not activate a web product surface or change product phase.

## Delivery Caveat

The UX contract records target behavior separately from current delivery evidence. It does not promote absent CLI grammar, incomplete Evidence Packet mappers, conformance-specimen routes, or preview MCP capabilities to shipped product status.
