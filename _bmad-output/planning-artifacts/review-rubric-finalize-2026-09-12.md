# PRD Quality Review — Hexalith.Memories

## Overall verdict

This is a strategically coherent, unusually candid brownfield PRD: the Phase 1 thesis, Phase 1.5 launch bet, hard gates, kill switch, FR1–FR75 inventory, and architecture-derived contract gaps are all substantive and largely testable. It is not yet Finalize-ready, however, because its delivery-status precedence contradicts the canonical decision that `sprint-status.yaml` wins, the dated G1 critical path still has no owning stories, and FR75 is not yet bound by the supposedly reconciled final architecture spine. Correct those high-impact governance and traceability defects; the remaining issues are bounded polish and test-acceptance work.

## Decision-readiness — thin

The document does the hard product thinking well. It states the Phase 1 bet, separates thesis and launch clocks, defines five hard thesis gates and three hard launch gates, and gives the fusion kill switch an actual consequence rather than a delay-only escape (§ Executive Summary; § Measurable Outcomes; § Release decision record). It also names what has been given up: MCP and EventStore product integration are not allowed to accordion into MVP, causal completeness is not claimed for file/URL ingest, and a failed G1 removes graph from default hybrid.

The weakness is execution authority. A decision-maker cannot safely use a delivery register whose precedence rule conflicts with the run's canonical decision, nor approve a fixed gate date while all named prerequisites remain unowned. These are not hidden risks—the PRD surfaces them—but Finalize requires converting them into an internally consistent decision contract.

### Findings

- **high** Delivery status reverses the recorded authority (§ Functional Requirements › Delivery status register; `.memlog.md` decision 67; Addendum § Downstream drift not fixed by this Update) — The PRD says, “On conflict, current implementation evidence wins for delivery status,” while the canonical memlog decision says “register conflict rule = sprint-status.yaml wins.” The addendum also admits that `epics.md` and `sprint-status.yaml` still need change control after architecture reconciliation. Architecture evidence may legitimately downgrade *current-contract satisfaction* or *verification confidence*, but it cannot silently replace the workflow tracker as delivery authority. *Fix:* State that `sprint-status.yaml` remains authoritative for epic/story delivery state; place architecture-ledger contradictions in a separate current-contract/conformance column, and route tracker corrections through the declared change-control path.
- **high** The fixed G1 date has an unowned critical path (§ Measurable Outcomes › Release decision record; § Open Questions, item 2; § CLI Specification) — The record fixes G1–G5 for 2026-12-01 while stating that the stub CLI verbs, two-axis control, graph seeding, and N ≥ 50 corpus have “No owning story.” It then says the stub verbs are “tracked in `epics.md` / `sprint-status.yaml`,” although the existing completed Epic 7 rows do not own the newly acknowledged residual work. The reviewer/corpus decision is still open. *Fix:* Before Finalize, register and sprint-select explicit successor stories for every prerequisite, including corpus/reviewer ownership, or invoke the PRD's own unreachable-date/sprint-change outcome now.
- **medium** One register conflates delivery, contract satisfaction, and evidence (§ Functional Requirements › Delivery status register; § Non-Functional Requirements › NFR delivery status) — Categories such as “Shipped,” “Partial (architecture contract incomplete),” “Shipped against previous wording,” “hardening in progress,” and “verification owed” are not values on one status axis. For example, a story may remain `done` in tracking while the strengthened 2026-09-12 wording is only partially satisfied. *Fix:* Use independent fields for planned phase, tracker state, current-contract assessment, and verification state, each with its authority and as-of date.

## Substance over theater — strong

The document earns its length. The vision is falsifiable; competitive claims are narrowed instead of inflated; the N=8 benchmark is explicitly demoted to diagnostic evidence; confidence, telemetry, EventStore, and ingestion-state terms are carefully separated; and future journeys are phase-labelled rather than marketed as shipped. The ten journeys each expose a distinct product, operator, agent, end-user, or contributor concern, and the later-phase journeys are explicitly scoped away from thesis acceptance.

No substantive finding is warranted in this dimension. The architecture material that remains in `prd.md` is mostly real rather than decorative, although its placement is addressed under Shape fit.

## Strategic coherence — strong

The PRD has a clear thesis: case-scoped, tenant-isolated, explainable three-axis retrieval must beat a BM25+semantic control before the EventStore/MCP launch bet proceeds. Feature sequencing, the G1 control, the graph-population caveat, the five-step failure response, onboarding clocks, and Phase 1.5 launch gates all follow from that thesis. Success metrics include concern thresholds and hard counter-actions, so activity signals cannot masquerade as thesis validation.

No substantive finding is warranted in this dimension. The product arc remains coherent through the 2026-09-12 architecture reconciliation: projection completion, idempotency, authorization, tenant-wide case partitioning, erasure, telemetry posture, and fairness strengthen the non-retrofittable foundation rather than opening unrelated scope.

## Done-ness clarity — adequate

Most high-risk requirements have an observable consequence and a named test: G1 has population, control, delta, aggregate guards, and label agreement; FR6/FR13 define current-revision completion; FR39/NFR16 cover replay and restore non-resurrection; NFR8 is principal-driven; NFR18 defines the no-safe-axis boundary; and NFR24–NFR26 define deterministic fusion evidence. The status registers also distinguish mechanism from recorded verification, which is useful once their authority model is repaired.

The weakest acceptance language is concentrated in the newly strengthened operational contracts. Fairness, freshness-state classification, and telemetry recovery use boundedness words without defining the bound that makes a test pass.

### Findings

- **high** Workload fairness has a scenario but no pass threshold (§ Functional Requirements, FR8; § Scalability, NFR12–NFR13; § File/URL ingest freshness, NFR36; Addendum § Capacity and backpressure) — “Cannot starve,” “queues are bounded,” and “interactive ... responsive” are important outcomes, but NFR13's target only names a three-tenant noisy-neighbor test. It gives no queue limit, admission-wait bound, latency/throughput degradation limit, or finite progress criterion for interactive work. NFR12's 5% tenant-scaling target does not fully specify the batch/repair/recovery/migration contention case. *Fix:* Define the admitted workload profile and explicit pass bounds—preferably maximum degradation against NFR1–NFR3/NFR36, queue/admission limits, and a progress/fairness invariant—then link re-verification to those values.
- **medium** Freshness and telemetry contracts defer the values that define success (§ NFR33–NFR34) — NFR33 requires “authoritative” `current`/`aging`/`stale`/`unknown` thresholds but supplies none. NFR34 requires a configured TTL, bounded recovery, and an “approved bound” for non-blocking telemetry failure without naming defaults, maximums, or the authoritative configuration artifact. Contract tests can prove fields exist but cannot decide whether a deployment meets the product requirement. *Fix:* Put the product-level defaults/limits in the PRD or name a versioned contract/configuration source and require Production admission to reject absent or out-of-policy values.

## Scope honesty — adequate

Scope is mostly exemplary. MVP, Phase 1.5, Phase 2, and Phase 3 are explicit; non-goals name plausible assumptions that readers might otherwise import; early delivery does not silently alter MVP acceptance; and known architecture gaps are recorded instead of being wished away. The open G1 corpus/reviewer question has an owner and revisit condition, though it remains a Finalize blocker as noted above.

Two editorial defects still make scope extraction less reliable: one journey carries contradictory phase labels, and the Open Questions section mixes live questions with resolved history.

### Findings

- **medium** Journey 8 has two incompatible phase assignments (§ User Journeys › Journey 8; § Journey Requirements Summary; § Non-Goals) — Its heading says “(Phase 1),” while the summary and the explicit application-facing REST UI non-goal place Priya's surface in Phase 2. That is a direct scope contradiction for downstream UX/story generation. *Fix:* Relabel Journey 8 as Phase 2 everywhere and retain the distinction between MVP transport endpoints and the Phase 2 application-facing surface.
- **low** The Open Questions register obscures the live-item count (§ Open Questions) — Items 1, 4, 6, 7, and 8 are closed; item 5 reads as a settled prohibition with a reopen condition rather than a question. Only items 2 and 3 are clearly active questions, yet the section presents eight numbered entries. *Fix:* Split Active Open Questions from Resolved Decisions/History and state whether item 5 is closed or still awaiting a decision.

## Downstream usability — adequate

The core mechanics are sound: there are exactly 75 unique FR definitions covering FR1–FR75 and 36 unique NFR definitions covering NFR1–NFR36; the glossary stabilizes difficult nouns; journeys have named protagonists except the deliberately technical agent journey; and the phase/status registers provide a usable extraction surface once authority is corrected. Cross-links among gates, FRs, NFRs, CLI rows, and addendum mechanism notes are unusually specific.

The main downstream gap is at the PRD-to-architecture boundary: FR75 was introduced by reconciliation but is not yet included in the final architecture spine's binding. The assumptions index also fails its promised roundtrip.

### Findings

- **high** FR75 is outside the final architecture spine's declared binding (§ Functional Requirements, FR75; Addendum § Downstream drift not fixed by this Update) — The PRD says it was reconciled to the “final architecture spine” and declares FR1–FR75 as the product contract, while the addendum admits that the spine still binds only `FR1-FR74`. FR75's durable idempotency guarantee is itself marked partial and architecture-sensitive, so this is not a cosmetic frontmatter lag. *Fix:* Add FR75 to the architecture spine's requirement binding and trace its implementation gap/decision, or mark the PRD's architecture reconciliation incomplete until that downstream update is accepted.
- **low** The Assumptions Index does not roundtrip (§ Measurable Outcomes › Release decision record; § Assumptions Index) — The inline schedule-window assumption for reaching 2026-12-01 is not indexed, while the NFR33/NFR35 collision entry is explicitly “index-only by design” and has no inline `[ASSUMPTION]`. This contradicts § Document Purpose's promise that assumptions are tagged and indexed. *Fix:* Add the schedule assumption to the index; move the historical NFR-ID collision to a resolved-decisions note or add a genuine inline assumption if it still affects interpretation.

## Shape fit — adequate

A chain-top, brownfield developer-platform PRD appropriately needs more rigor than a lightweight product brief, and the capability-spec shape is a good fit. Named journeys remain valuable because the product spans developer, operator, agent, application end-user, and contributor workflows. The addendum is also the right mechanism for architecture residue and rejected alternatives.

The boundary is not yet consistently enforced. The PRD says topology and mechanism detail live in `addendum.md`/architecture, but then repeats enough architecture detail to create another drift surface.

### Findings

- **medium** Mechanism detail still leaks across the declared PRD/addendum boundary (§ Developer Tool / API Backend Specific Requirements; Addendum § Why this file exists) — `prd.md` retains published/non-packable package inventory, concrete service topology, communication mechanisms, provider model IDs and vector dimensions, secret/configuration layering, and orchestration specifics even though the addendum says these were moved out and architecture is authoritative. Some of this constrains product behavior, but the implementation-level duplication weakens the promised document roles. *Fix:* Keep externally observable constraints and compatibility commitments in the PRD; move canonical topology, package graph, provider defaults/dimensions, and configuration mechanism to the addendum/architecture with stable cross-references.

## Mechanical notes

- FR continuity is complete and unique: 75 definitions cover FR1–FR75. FR75 is placed between FR13 and FR14, so the IDs are complete but not in monotonic document order; move it to a clearly named cross-cutting subsection or explain the insertion convention for downstream parsers.
- NFR continuity is complete and unique: 36 definitions cover NFR1–NFR36.
- Journey 2 uses `memories handlers --list`, while the canonical CLI table uses `memories handlers list/mismatches`; normalize to the declared command grammar.
- The release record says `docs/dev/quickstart-walkthrough-log.md` is empty, but the referenced file is absent. “No recorded run/file yet” is the accurate evidence statement.
- Open Question 3 cites the older `ux-design-specification.md`, while the frontmatter also names the 2026-09-12 `DESIGN.md`/`EXPERIENCE.md` pair. The old file exists, but the question should name which UX artifact is authoritative before Epic 17 activation.
- Addendum cross-references otherwise preserve the key architecture/product boundary: RRF numbers, current-revision projection completion, durable idempotency, tenant authority, erasure, telemetry, and fairness all map back to named FR/NFR contracts.
