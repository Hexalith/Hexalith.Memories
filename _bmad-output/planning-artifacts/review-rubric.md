# PRD Quality Review — Hexalith.Memories

## Overall verdict

This is a strategically strong, unusually candid change-controlled PRD: it states a falsifiable thesis, separates thesis and launch contracts, names hard no-go outcomes, and exposes delivery gaps rather than presenting shipped code as validated product. Its main risk is downstream extraction: several phase, parameter, and decision-state contradictions remain across the PRD and addendum, while a small set of cross-cutting requirements still describe the need for a bound rather than supplying the bound. The document is decision-ready, but those inconsistencies should be reconciled before another UX/architecture/story pass treats every sentence as canonical.

## Decision-readiness — strong

The core choices are explicit and consequential. The Executive Summary separates “Phase 1 (thesis)” from “Phase 1.5 (launch)” (§ Executive Summary), the thesis protocol states the control and numeric win rule (§ Success Criteria › Measurable Outcomes), and the six-step failure path says that graph is removed from default hybrid and the launch decision is cancelled rather than merely delayed. The Release decision record (§ Success Criteria › Measurable Outcomes) names owners, dates, current evidence, and real no-go outcomes, including the uncomfortable fact that “no owning story exists” for G1 prerequisites.

Trade-offs are also recorded as decisions: weighted RRF displaces magnitude blending, EventStore conventions remain the beachhead if generic DAPR needs custom code, and isolation/case bootstrap cannot be traded away under resource pressure (§ MVP Strategy; § Risk Mitigation Strategy). These are usable constraints, not neutral “considerations.”

### Findings

- **low** The Open Questions section is not a clean pending-decision queue (§ Open Questions) — Items 1, 4, and 6 are explicitly “Closed,” and item 5 is a settled MVP rule with a future re-open condition. This makes a decision-maker re-triage the list before seeing the genuinely open items 2, 3, 7, and 8. *Fix:* move closed items to a short Decision Log/Resolved Questions tail and retain only decisions that can still change the active contract under Open Questions.

## Substance over theater — adequate

The central content is earned. The product is named specifically as DAPR-native, case-scoped, tenant-isolated memory with a three-axis retrieval thesis (§ Executive Summary); innovation claims explicitly concede that hybrid search and graph RAG already exist and narrow the differentiated claim to the integrated EventStore/DAPR causal path (§ Innovation & Novel Patterns). The NFR set mostly supplies concrete thresholds, conditions, and verification methods instead of generic “secure/scalable/reliable” prose (§ Non-Functional Requirements).

The weakest part is the journey portfolio. The phase labels and scope notes prevent outright scope deception, but ten journeys—including a Phase 3 backend migration and a contributor workflow explicitly marked “not product scope”—create more persona furniture than the active Phase 1/1.5 decision contract needs.

### Findings

- **medium** Future and non-product narratives dilute the load-bearing journeys (§ User Journeys › Journeys 4, 6, 8, and 10) — the document spends full narrative arcs on Phase 2 onboarding, Phase 3 migration, a future application UI, and contributor operations, while Journey 10 concedes its capabilities are “not product features.” The phase labels are honest, but the volume makes the product look broader and more settled than the active ship contract. *Fix:* keep full narratives for the Phase 1 and Phase 1.5 paths that drive requirements; reduce later-phase and contributor material to concise future-scenario notes or move it to the addendum.

## Strategic coherence — strong

The PRD has a clear thesis: three-axis RRF must outperform the realistic BM25+semantic alternative while tenant isolation remains non-negotiable (§ Executive Summary; § MVP Strategy & Philosophy). Phase 1 features directly support G1–G5, Phase 1.5 features support L1–L3, and the kill switch changes the default product proposition if the graph axis fails (§ Measurable Outcomes; § Project Scoping & Phased Development). Success is not reduced to activity: G1 measures retrieval quality, G2/G4 isolation, G3 onboarding, and G5 explain determinism; the aggregate regression guards and business “Concern Thresholds” act as counter-metrics (§ Success Criteria).

The document also keeps adoption signals subordinate to the thesis: GitHub/NuGet/community measures are explicitly “signals, not gates,” and the N=8 result is correctly demoted to a diagnostic (§ Business Success; § Measurable Outcomes). The feature arc therefore reads as a product bet rather than a backlog with a vision paragraph attached.

## Done-ness clarity — adequate

The most consequential outcomes are highly testable. G1 defines population, controls, delta, agreement, reproducibility, aggregate guards, and label-freeze rules; G2–G5 and L1–L3 have named pass conditions (§ Measurable Outcomes). The CLI surface table pairs each command with phase, delivery state, and acceptance output (§ CLI Specification), while the FR/NFR delivery registers distinguish “shipped,” “partial,” “implemented,” and “verified” (§ Functional Requirements; § Non-Functional Requirements).

Clarity is uneven outside those gates. Several ongoing/cross-cutting requirements say that a limit or behavior must exist without giving a pass/fail bound, and the tenant-mismatch response is not stated consistently across the hard security gate and functional contract.

### Findings

- **medium** Several cross-cutting requirements defer the actual acceptance bound (§ NFR13, NFR15, NFR33, NFR34) — “does not block,” “must not preclude backend migration,” “authoritative … thresholds,” “configured TTL,” and “bounded recovery behavior” name desirable properties but not the maximum permitted interference, the extraction-point acceptance test, the freshness transitions, the TTL, or the recovery bound. This is especially material for FR8/NFR13 and the shared Evidence Packet contract. *Fix:* supply explicit numbers/state transitions and named verification fixtures for active surfaces; for genuinely future requirements, name the decision owner and activation condition rather than presenting an unchosen bound as a complete NFR.
- **medium** Cross-tenant mismatch behavior has two acceptable outcomes in the security gate but only one in the functional contract (§ NFR8; FR44; Journey 5) — NFR8 allows every cross-tenant call to be “rejected or return only A’s data,” while FR44 requires rejection and Journey 5 promises a “tenant-mismatch error.” Both prevent leakage, but they produce observably different APIs and tests. *Fix:* require rejection when an explicit requested tenant conflicts with authenticated claims; define claims-based scoping only for requests that omit a tenant identifier, if omission is allowed.
- **low** The primary Phase 1 empty-state hint is not executable as written (§ User Journeys › Journey 9) — it suggests `memories ingest <file>` even though the canonical command requires `--tenant` and `--case`; the complete syntax appears only after case creation. *Fix:* make every quoted empty-state command include the required options or explicitly label it as shorthand.

## Scope honesty — strong

Omissions and sequencing are unusually explicit. The Non-Goals section calls out MCP, EventStore auto-index, application-facing UI, discussions, compliance-product controls, additional ingestion sources, and per-user memory (§ Non-Goals). The canonical phase and delivery registers distinguish product horizon from active increment and say that “shipped” is not thesis validation (§ Functional Requirements). The PRD also records that the G1 protocol has not run, that required CLI verbs are stubs, that three samples do not exist, and that Phase 1.5 cannot launch on a missed gate (§ Release decision record; § CLI Specification; § Developer Experience & Documentation).

Assumptions and tensions are generally visible rather than silently resolved: the corpus mix, schedule window, provider expansion, ingest-freshness budgets, and G1 reviewer/corpus ownership are tagged or called out with owners and revisit conditions (§ Measurable Outcomes; § Open Questions; § Assumptions Index). Open-item density is material for a chain-top PRD, but the document correctly identifies the G1 items as phase blockers instead of calling the product complete.

## Downstream usability — thin

The underlying machinery is strong: FR1–FR74 and NFR1–NFR36 are contiguous and uniquely defined, Journeys 1–10 are uniquely numbered, the Glossary resolves critical distinctions such as relevance versus metadata confidence and the three EventStore meanings, and phase/delivery registers provide canonical extraction points (§ Glossary; § Functional Requirements; § Non-Functional Requirements). Those features make most sections independently reusable.

For a document that explicitly feeds UX, architecture, stories, and change control (§ Document Purpose), however, three live contradictions require a downstream reader to choose which source to trust: Journey 8 has two phases, MCP uses both singular and plural parameter names, and the addendum simultaneously treats release dates as assumed and confirmed. The assumptions round-trip and one non-named technical journey add smaller extraction hazards.

### Findings

- **medium** Journey 8 has conflicting phase ownership (§ Non-Goals; § User Journeys › Journey 8; § Journey Requirements Summary) — the heading says “Phase 1,” while the explicit non-goal and summary both assign Priya’s application-facing REST UI to Phase 2. *Fix:* change the Journey 8 heading to Phase 2 and ensure any Journey 8 references use that phase.
- **medium** The MCP search parameter is both `axis` and `axes` (§ User Journeys › Journeys 3 and 7) — Journey 7’s setup promises a typed `axes` parameter, but its example call and Journey 3 use `axis`; the addendum itself records this as a downstream check. This is a public schema ambiguity, not harmless prose variation. *Fix:* choose the actual MCP schema field, use it in both journeys and FR58 support text, and explicitly distinguish it from the CLI `--axis` spelling if necessary.
- **medium** The addendum carries a stale release-date decision state (addendum § Release decision — why dated no-go) — it says dates “were set by the assistant” and remain `[ASSUMPTION]` “until Jerome confirms,” while the PRD Release decision record and addendum handoff both say Jerome confirmed the 2026-12-01/2027-01-01 dates. *Fix:* rewrite the addendum rationale as historical context and state that only the 2026-10-31 sprint-selection date remains derived/unconfirmed.
- **low** The Assumptions Index does not round-trip exactly (§ Measurable Outcomes › Release decision record; § Assumptions Index) — the inline assumption that the work window to 2026-12-01 is a schedule assumption is not indexed, while the NFR33/NFR35 historical-collision entry is “index-only by design” and has no inline assumption tag. *Fix:* add the schedule-window assumption to the index and move the historical collision note to a decision/change-history note, or add a matching inline tag if it is truly still assumed.
- **low** Journey 7 is a floating technical actor rather than a named protagonist (§ User Journeys › Journey 7) — “LLM Agent” describes a system role, unlike Alex, Kenji, Priya, or Dani, and the section explicitly says it is an interaction pattern rather than a narrative. *Fix:* relabel it as an Integration Flow rather than a User Journey, or give the agent a named application/context and keep that context inline.

## Shape fit — adequate

The chosen shape broadly fits a brownfield developer tool/API backend with multiple human and machine consumers. Capability sections, contract IDs, explicit release gates, CLI acceptance outputs, and technical NFRs carry more weight than persona prose, while the operations and agent journeys surface requirements that a pure feature list would miss (§ Project Classification; § User Journeys; § Developer Tool / API Backend Specific Requirements). The phase/status registers are justified by the document’s change-controlled purpose.

The PRD/addendum boundary is not applied consistently, though the conflict is mild because the PRD explicitly declares its exceptions. Document Purpose says “SDK pins, package counts, fusion weights, and host topology belong in architecture or `addendum.md`,” but the PRD still contains published/non-packable package inventories and a concrete deployment topology (§ Package Distribution; § Deployment Topology). That is useful brownfield context, yet it increases duplicate-source drift.

### Findings

- **low** The declared outcome-versus-mechanism boundary has exceptions that are not named as exceptions (§ Document Purpose; § Package Distribution; § Deployment Topology; addendum § Why this file exists) — package inventories and host topology remain in the PRD even though its opening rule assigns them to architecture/addendum. *Fix:* either move the duplicated mechanism tables, or amend Document Purpose to state precisely which inventory/topology facts remain contractual in the PRD and which details are architecture-owned.

## Mechanical notes

- FR definitions are contiguous and unique from FR1 through FR74; NFR definitions are contiguous and unique from NFR1 through NFR36; Journey headings are contiguous and unique from Journey 1 through Journey 10.
- Journey 8 phase drift: heading says Phase 1; Non-Goals and Journey Requirements Summary say Phase 2.
- MCP parameter drift: `axes` in Journey 7 setup versus `axis` in Journey 3 and the Journey 7 call example.
- Assumptions Index round-trip: the 2026-12-01 work-window assumption is missing from the index; the NFR33/NFR35 collision is index-only.
- Open Questions contains three closed items and one settled MVP constraint with a future revisit condition.
- Journey 7 has no named protagonist and is better shaped as an integration flow.
- Addendum release-date rationale is stale relative to the confirmed decisions in the PRD and later addendum handoff.
