# Architecture-Coherence Retest — 2026-09-12

> **Current focused-retest status:** Critical 0 · High 2. C1 is closed by the addendum at the end of this report; H1/H2 remain external phase blockers.

## Verdict — REVISE

The update resolves the prior high findings on tracking authority, Phase 1.5 activation status, Dapr/FalkorDB topology, FR75 epoch semantics, and measurable fairness. One critical release-gate loophole and two high architecture blockers remain; G6 correctly fails today, but its criterion is not yet closed against every MVP delivery/evidence state.

| Severity | Remaining |
|---|---:|
| Critical | 1 |
| High | 2 |

Three medium tail issues remain and are intentionally not detailed in this retest.

## Critical

### C1 — G6 still permits unimplemented or unverified MVP requirements to escape the ship gate

**Affected:** `prd.md` §Phase 1 thesis go/no-go, §Release decision record, and §NFR delivery status; newly imported NFR37.

G6 covers MVP/active-foundation IDs only when they appear in a `Partial`, `hardening`, or `re-verification` row. It omits the `Implemented, verification run not recorded` and `Not started` rows. Consequently MVP NFR37 and NFR36 can remain not started, and MVP performance evidence in the implemented-but-unverified row can remain absent, while the literal G6 criterion still passes once its named architecture gaps are bound or excepted. NFR37 is especially clear: the PRD calls it an MVP active-CLI contract, records no current evidence or story, but G6 treats only its architecture binding—not implementation and verification—as a prerequisite.

**Recommended fix:** Make G6 cover every current MVP/active-foundation ID that is not verified against current wording, regardless of register row, or record an explicit product-and-architecture release exception with owner and `sprint-status.yaml` entry for each exclusion.

## High

### H1 — AD-14 phasing remains contradictory across the adopted architecture and product contract

**Affected:** `prd.md` FR43, Phase 2/3 scope, Open Question 9; `addendum.md` §Migration phasing; `ARCHITECTURE-SPINE.md` AD-14.

The retest confirms that the earlier silent exception is now honestly disclosed and fail-closed through G6, but the underlying contradiction remains: AD-14 is adopted and unphased, while FR43 permits an acknowledged degraded MVP rebuild and the product assigns staged non-disruptive migration to Phases 2/3. Until architecture ratifies that boundary or product rebaselines MVP, the PRD and architecture cannot both be authoritative.

**Recommended fix:** Amend the architecture authority to qualify AD-14 by phase and name the bounded FR43 MVP exception, or rebaseline FR43/MVP through approved sprint change; then track any implementation convergence gap.

### H2 — The final architecture does not bind or trace FR75 and the newly imported NFR37

**Affected:** `prd.md` G6, Open Question 10, FR75, NFR37; `addendum.md` §Downstream drift; `ARCHITECTURE-SPINE.md` frontmatter and capability map.

The spine still binds only FR1–FR74/NFR1–NFR36. AD-4 substantively anticipates the corrected epoch-aware FR75, but the formal binding is stale; NFR37 adds an active CLI accessibility/output contract with implications for AD-4 duplicate behavior, AD-5/AD-13 disclosure, AD-12 cross-surface semantics, and operational progress, yet has no architecture trace. The PRD correctly makes this a G6 blocker, so final coherence is not achieved until the authority is updated.

**Recommended fix:** Review NFR37 against the relevant ADs, extend the spine binding to FR1–FR75/NFR1–NFR37, add explicit capability-map/consistency trace where needed, and register any resulting convergence work in `sprint-status.yaml`.

## Addendum — G6 Scope-Loophole Retest

**Retest verdict: CLOSED.**

The latest G6 wording now covers every MVP requirement not **Verified against its current wording**, explicitly including not-started, partial, hardening, implemented-without-recorded-evidence, and re-verification rows. The release-decision rule also makes missing evidence, unowned work, or an unratified exception a G6 failure. This closes C1: NFR36, NFR37, and MVP performance-evidence obligations can no longer escape the release gate by occupying an omitted delivery-status class.

AD-14 phasing and FR75/NFR37 architecture binding remain known external phase blockers and were not re-reviewed in this addendum.
