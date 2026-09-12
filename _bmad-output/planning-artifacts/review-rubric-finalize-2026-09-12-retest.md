# PRD Quality Rubric Retest — Hexalith.Memories

> **Current focused-retest status:** Critical 0 · High 2. H3 is closed by the addendum at the end of this report; H1/H2 remain external governance/architecture phase blockers.

**Retested:** 2026-09-12
**Artifacts:** `prd.md`, `addendum.md`
**Baseline review:** `review-rubric-finalize-2026-09-12.md`
**Rubric:** BMad PRD Quality Rubric

## Overall verdict

**Fair; not Finalize-ready.** The architecture and UX reconciliation materially improved the PRD: delivery authority is now explicit, workload fairness is measurable, CLI accessibility has a testable contract, Journey 8 is phase-correct, and CLI placeholder versus absent-registration wording is accurate. No Critical finding remains, but three High findings still prevent a clean chain-top handoff: the release critical path is unowned and partly unratified, architecture binding remains incomplete, and public CLI/MCP examples still conflict with the reconciled interface contracts.

## Rubric status

| Dimension | Retest status | Change from baseline |
|---|---|---|
| Decision-readiness | thin | Delivery-authority contradiction resolved; release-governance blocker remains. |
| Substance over theater | strong | Unchanged. |
| Strategic coherence | strong | Unchanged. |
| Done-ness clarity | adequate | Prior fairness High resolved by explicit NFR13 workload, admission, queue, latency, and progress bounds. |
| Scope honesty | adequate | Journey 8 phase contradiction resolved; live phase blockers are now explicit. |
| Downstream usability | thin | CLI delivery semantics improved, but architecture binding and public-interface examples remain unsafe to extract. |
| Shape fit | adequate | Unchanged at High/Critical severity. |

**Severity:** Critical 0 · High 3. A medium/low editorial and document-boundary tail remains but is intentionally not enumerated in this retest.

### Baseline High-finding disposition

| Baseline High | Retest |
|---|---|
| Delivery status reverses recorded authority | **Resolved.** `sprint-status.yaml` is explicitly authoritative; architecture evidence is a separate contract-conformance assessment. |
| Fixed G1 date has an unowned critical path | **Remains High**, broadened by the unratified G6 date assumption. |
| Workload fairness has no pass threshold | **Resolved.** NFR13 now supplies a reproducible mixed-load profile and explicit pass bounds. |
| FR75 is outside final architecture binding | **Remains High**, now joined by NFR37 and the AD-14 phase conflict. |

## Remaining Critical findings

None.

## Remaining High findings

### H1 — The dated release contract still has no owned, ratified execution path

**Dimension:** Decision-readiness
**Location:** `prd.md` § Measurable Outcomes › Release decision record; § Open Questions 1–2; `addendum.md` § Release decision

The PRD retains 2026-12-01 for the expanded G1–G6 decision while explicitly marking that expansion as an unratified assumption. It also states that no successor stories own the missing CLI operations, FR17 graph auto-seeding, FR25 two-axis control, the N ≥ 50 corpus/reviewer work, or every G6 exception. Naming a 2026-10-31 checkpoint and a no-go consequence is honest, but it does not make the critical path executable.

**Fix:** Before Finalize, either ratify/reset the expanded gate date and create/sprint-select an owning story or approved phase exception for every prerequisite, including corpus and reviewer ownership, or invoke the PRD's unreachable-date sprint-change outcome now.

### H2 — “Final architecture reconciliation” is still incomplete at the binding boundary

**Dimension:** Downstream usability
**Location:** `prd.md` § Open Questions 9–10; `addendum.md` § Migration phasing; § Downstream drift not fixed by this Update

The PRD correctly preserves Phase 2 embedding/schema migration and Phase 3 backend migration, but architecture AD-14 remains unphased. The final architecture spine also still binds only FR1–FR74/NFR1–NFR36, excluding new MVP contracts FR75 and NFR37. These are explicitly G6 blockers, so the document cannot simultaneously be treated as fully reconciled and ready for architecture/story extraction.

**Fix:** Qualify AD-14 by phase or approve a product rebaseline, and extend the final architecture binding and gap trace through FR75 and NFR37. Until accepted, describe the architecture reconciliation as partial in handoff language.

### H3 — Public interface examples still contradict the reconciled CLI/MCP contracts

**Dimension:** Downstream usability / Done-ness clarity
**Location:** `prd.md` § Measurable Outcomes › Graph seeding; § User Journeys 3 and 7; § CLI Specification; UX `EXPERIENCE.md` § Exact current CLI grammar and status; § Exact MCP tool contract map

The PRD says `search query --from <unit>` is optional even though the current CLI grammar and its authoritative surface table contain no search `--from`. Journey 3 and Journey 7 call `search_memory` without required `tenantId`, use stale `axis`/`token_budget` names instead of current `axes`/`tokenBudget`, and include graph as a `SearchAxis` even though graph work is exposed through `traverse_relations`. These examples would generate invalid client configuration and contaminate L1/sample extraction.

**Fix:** Remove `--from` as claimed current CLI syntax until its surface is decided; keep FR17 auto-seeding as the product outcome. Rewrite J3/J7 with required `tenantId`, current camelCase field names, and `traverse_relations` for graph detail, while leaving exact serialized ownership in `Contracts.V1`.

## H3 focused retest addendum — 2026-09-12

**Verdict: Closed.** This focused retest does not reassess H1 or H2; they remain known external governance/architecture phase blockers.

The latest PRD now:

- states that the current public search grammar has no start-node input and assigns explicit graph starts to `traverse` / `traverse_relations`, without presenting `search query --from` as current syntax;
- gives Journeys 3 and 7 the required `tenantId` and current `axes` / `tokenBudget` MCP field names; and
- limits the MCP `SearchAxis` examples to `Syntactic`, `Semantic`, `Nl`, and `Hybrid`, with graph-detail work shown through `traverse_relations` using its required `tenantId` and `from` inputs.

H3 is therefore removed from the remaining High count. Focused severity after this addendum: **Critical 0 · High 2** (H1/H2 unchanged).
