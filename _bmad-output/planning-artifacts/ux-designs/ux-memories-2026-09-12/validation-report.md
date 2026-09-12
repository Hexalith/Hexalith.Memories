# Validation Report — Hexalith.Memories

- **DESIGN.md:** `DESIGN.md`
- **EXPERIENCE.md:** `EXPERIENCE.md`
- **Run at:** 2026-09-12T11:19:30+02:00

## Overall verdict

The rubric walker rates the pair adequate and close to strong: tokens, component coverage, states, visual-reference handling, and document shape are mechanically coherent. The additional lenses materially shift the binding decision to **revise before downstream implementation** because the live PRD and current code advanced beyond parts of the migration evidence.

Across the four raw reviews there were 0 critical, 9 high, 7 medium, and 4 low findings. After consolidating duplicate observations, this report carries 0 critical, 7 high, 5 medium, and 4 low findings.

## Resolution status

All original spine findings are resolved. The post-fix rubric has seven strong categories and one adequate category (bloat/overspecification); source binding is safe against the pinned PRD; Fluent/FrontComposer and accessibility rechecks found no remaining spine defect. Two implementation-only follow-ups remain recorded in the working ledger: the HTTP 401 suggestion names the wrong token environment variable (medium), and a conformance allowlist comment overstates dialog focus ownership (low). Neither changes the final UX contract.

## Category verdicts

- Flow coverage — adequate
- Token completeness — strong
- Component coverage — strong
- State coverage — strong
- Visual reference coverage — strong
- Bloat & overspecification — adequate
- Inheritance discipline — adequate
- Shape fit — strong

## Findings by severity

### Critical (0)

None.

### High (7)

**[Flow coverage / Source drift] — J8 uses the superseded Phase 1 journey name (§ EXPERIENCE.md Key Flows / J8)**  
The live PRD names J8 `Priya — "I Need to Understand This Case" (Phase 2 downstream application)`, while the spine retains `(Phase 1)` and incorrectly calls it the exact source heading.  
Fix: copy the current PRD name verbatim, remove the stale-heading explanation, and refresh the derived PRD evidence.

**[Source drift] — Release no-go, G6, and unverified NFR37 are missing at point of use (§ EXPERIENCE.md Foundation and Accessibility Floor)**  
The spine can be read as an active posture and describes cross-format semantic parity as current fact, while the PRD says the release is no-go, all G1–G6 are hard, G6 architecture/ownership evidence is absent, and NFR37 is not started.  
Fix: add explicit release governance and distinguish observed output behavior from the binding but unverified NFR37 target.

**[Source drift] — Search start-node handling is settled but still presented as unresolved (§ EXPERIENCE.md Interaction Primitives)**  
Search has no public start input and hybrid auto-seeds; explicit starts belong to traversal. `Contracts.V1` has no graph-start provenance field.  
Fix: remove the open `--from` question and label invocation-derived context as presentation context, never as a V1 field.

**[Source drift / Fluent–FrontComposer] — Normative primitive bindings drift from the current Razor inventory (§ DESIGN.md components)**  
At least nine `primitive` leaves name controls or conceptual compositions that the current conformance-tested components do not use.  
Fix: separate exact `implemented-primitives` from future `activation-target-primitives`, or align all current mappings with the allowlist and source.

**[Source drift] — Current component rows promise target actions and data (§ EXPERIENCE.md Component Patterns)**  
Several rows mix current specimen behavior with future source actions, rich graph attributes, lifecycle telemetry, operational health, and benchmark evidence that current DTOs/mappers cannot carry.  
Fix: give affected rows explicit current-specimen and activation-target clauses, naming absent fields and gates.

**[Source drift] — Current conflict mapping contradicts the trust vocabulary (§ EXPERIENCE.md evidence and recovery states)**  
Current Web mapping can label backend or axis degradation as `Conflicting`, although the spine correctly reserves conflict for source disagreement.  
Fix: disclose this live-code divergence and require a contract-backed conflict signal before claiming conformance.

**[Accessibility / Recovery] — Blanket CLI JSON-envelope statement is false for export and cancellation (§ EXPERIENCE.md current CLI contract)**  
Export success is a raw JSON artifact that ignores `--format` with a stderr warning; cancellation emits stderr plus exit 130 with no JSON body.  
Fix: document formatter-routed behavior and both versioned exceptions without equating byte shape with semantic equivalence.

### Medium (5)

**[Source drift] — Working evidence certifies a superseded PRD snapshot (§ .working extracts and reconciliation)**  
The extract and reconciliation still cite the older line count, G1–G5/NFR1–NFR36, stale J8 name, and open `--from` question.  
Fix: regenerate from the live PRD and append a supersession event without rewriting history.

**[Source drift] — Source precedence lacks an immutable revision identity (§ spine frontmatter)**  
The PRD and spines cite one another on the same date, so the exact compared revision is ambiguous.  
Fix: record a PRD content hash or one-way migration lineage marker.

**[Source drift] — The “Exact MCP” map omits validation constraints (§ EXPERIENCE.md MCP map)**  
The map omits `maxResults` 1..100, traversal `depth` 0..10, and exact edge-type literals.  
Fix: include those constraints or rename the section as a selected UX summary.

**[Accessibility / Recovery] — Credential guidance can normalize argv exposure and the implementation suggests the wrong variable (§ EXPERIENCE.md CLI options and recovery)**  
The supported `--token` can enter history; the correct preferred source is `HEXALITH_MEMORIES_API_TOKEN`, while current 401 copy names a different variable.  
Fix: state the safe source in the spine and track the implementation copy/test correction.

**[Fluent–FrontComposer / Accessibility] — Destructive-dialog focus ownership omits the service/provider boundary (§ EXPERIENCE.md Action Confirmation)**  
The wrapper and dialog body do not alone prove a dialog frame, trap, or focus return.  
Fix: require host launch through the shell-provided `IDialogService`/provider lifecycle and route-level focus evidence.

### Low (4)

**[Source drift] — Validation-reconciliation links are broken from `.working/` (§ .working/reconcile-validation.md)**  
Fix: correct link bases during evidence refresh.

**[Fluent–FrontComposer] — Future activation is not anchored to the current fail-closed gap registry (§ EXPERIENCE.md Future web activation)**  
Fix: require disposition of every applicable `Epic17ValidationInventory.Gaps` row and its browser summary.

**[Accessibility / Recovery] — Frontmatter omits stable implementation-evidence traceability (§ spine sources)**  
Fix: add a stable implementation evidence dependency without elevating it above product authority.

**[Accessibility / Recovery] — “Keyboard-only” wrongly implies input exclusivity (§ EXPERIENCE.md Active CLI)**  
Fix: say “fully operable by keyboard alone.”

## Reviewer files

- `review-rubric.md`
- `review-source-drift.md`
- `review-fluent-frontcomposer.md`
- `review-accessibility-recovery.md`
