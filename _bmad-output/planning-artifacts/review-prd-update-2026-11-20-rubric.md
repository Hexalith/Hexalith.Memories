# PRD Quality Review — Hexalith.Memories

Reviewed 2026-11-20 against the repository’s [PRD quality rubric](../../.agents/skills/bmad-prd/assets/prd-validation-checklist.md). The reviewed working copies have SHA-256 prefixes `247cb8e5d382` for the PRD and `9a00cfd82997` for the addendum.

## Overall verdict

**Excellent.** The PRD defines a falsifiable retrieval thesis, reproducible release tests and a clear decision for a maintainer facing the missed checkpoint. Phase 1 remains **no-go**, Phase 1.5 remains on hold and the Administrator has three downstream story corrections to make. Those are recorded delivery and planning obligations. [Release decision](prd.md#release-decision-record) · [Epic handoff](addendum.md#downstream-drift-not-fixed-by-this-update-handoff)

## Decision-readiness — strong

The November 20 record identifies who owns each decision and what would reopen it. G1 lacks independent reviewers and a qualifying run. December 1 is unreachable and January 1 was derived from that earlier date. The Administrator owns one reset once the reviewer, corpus, delivery and capacity prerequisites allow it. The PRD prescribes the six fusion changes only for a measured G1 failure. Misses on G2–G6 block release pending remediation or approved rescoping. This agrees with the approved sprint change. [Decision record](prd.md#release-decision-record) · [Open questions](prd.md#open-questions) · [November decision](sprint-change-proposal-2026-11-20.md)

## Substance over theater — strong

The benchmark’s eight synthetic topics supply regression evidence. The actual comparison in the test is `hybridNdcg > bestSingleNdcg`, whereas G1 asks whether hybrid clears a fixed improvement over BM25 plus semantic search on independently graded real topics. The market section frames integration depth as a hypothesis and records the lack of durability and replication-cost measurements. Alex’s empty-case search and Kenji’s tenant provisioning have different, concrete implications for adoption and isolation. [Benchmark test](../../tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs) · [Benchmark comparison](addendum.md#benchmark--what-the-shipped-suite-actually-is-2026-09-08-extract) · [Market comparison](prd.md#market-context--competitive-landscape) · [Journey coverage](prd.md#journey-requirements-summary)

## Strategic coherence — strong

Phase 1 asks whether case-local graph evidence improves file/URL search enough to justify including the graph in default ranking. The baseline, per-topic win margin, average-gain guard and worst-topic regression guard test that choice directly. The Phase 1.5 proposition has its own tests for grounded agent citations, the effort to integrate EventStore and the completeness of returned causal paths. Later web and collaboration work does not enter the thesis decision. [Executive summary](prd.md#executive-summary) · [Measurable outcomes](prd.md#measurable-outcomes) · [Validation approach](prd.md#validation-approach)

## Done-ness clarity — strong

G1 specifies blinded pooling, a 0–3 scale, three pairwise agreement thresholds, median judgments and an exact ranking formula. L1 freezes a separate holdout and requires a budgeted MCP response for every topic. A successful citation must identify a unit both returned in that interaction and graded at least two for the topic. All topics enter the denominator. L2 follows a new committed event through to a search result bearing its ID, with restores and image pulls on the clock. L3 counts distinct eligible events and requires every registered path for each successful event. These procedures identify what to capture and decide in failure cases. [G1 judgments](addendum.md#g1-judgments) · [L1 holdout](addendum.md#l1-holdout) · [L2 starting state](addendum.md#l2-starting-state) · [L3 denominator](addendum.md#l3-event-denominator)

NFR13 gives interactive, ingestion and repair workloads simultaneous completion and latency obligations. G2 requires rejection of an explicit tenant mismatch before restricted output or side effects. G5 defines REST and enrolled CLI parity over 100 trials and assigns end-to-end MCP verification to L1. G6 requires current evidence for active obligations, including previously shipped behavior under strengthened wording. [Load window](addendum.md#nfr13-load-window) · [Gate contracts](prd.md#measurable-outcomes)

## Scope honesty — strong

Phase and delivery are independently recorded. FR71 can have a shipped basic export while its product activation sits in Phase 2 and deployed export safeguards still matter to erasure. FR19 and FR56–FR57 record partial explanation and recovery behavior, consistent with the compatibility hints that still send users to a stub. G3 and Story 35.7 require the enrolled CLI and a cold run. The existing compatibility walkthrough cannot qualify that test. [FR phase and delivery registers](prd.md#functional-requirements) · [Compatibility hints](../../src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs) · [Walkthrough record](../../docs/dev/quickstart-walkthrough-log.md)

## Downstream usability — strong

Architecture and story owners can extract the operation, owner and evidence for the next step. The 34.33 specification confirms that the V1 wire-name register and guard precede 34.1’s additive URL retry token. The epic handoff identifies the actual story pairs and decisions for retained telemetry, positive erasure evidence in empty or never-provisioned cases, and internal telemetry/clock routes. Those epic edits remain open and their acceptance evidence remains owed. [Story 34.33 specification](../specs/spec-34-33-v1-wire-name-register/SPEC.md) · [Story definitions](epics.md) · [Downstream handoff](addendum.md#downstream-drift-not-fixed-by-this-update-handoff)

## Shape fit — strong

The brownfield product serves a CLI developer, a tenant operator, API consumers and agents. Journey coverage traces their work to its phase and marks the contributor journey as ecosystem scope. The PRD carries product outcomes and cutoffs. The addendum carries the scorer, workload fixtures, mechanism and migration details needed by implementers. The current repository host command matches the README, and the launch journey instead names the pinned EventStore sample host required for L2. [Journey coverage](prd.md#journey-requirements-summary) · [Gate procedures](addendum.md#gate-evidence-procedures) · [Implementation considerations](prd.md#implementation-considerations) · [Repository quick start](../../README.md#quick-start)

## Mechanical notes

- The 75 FR definitions and 37 NFR definitions each cover their full numeric range once. The requirements group FR75 with ingestion, so document order differs from ID order. All ten numbered journeys, G1–G6 and L1–L3 resolve. [Functional requirements](prd.md#functional-requirements) · [Non-functional requirements](prd.md#non-functional-requirements) · [Journey coverage](prd.md#journey-requirements-summary)
- The assumption index traces the tagged live assumptions, records the NFR13 reference values for recalibration and explicitly identifies the historical NFR33/NFR35 collision. The glossary distinguishes access telemetry, the Evidence Packet and exact ingestion wire values. Local Markdown destinations and heading links in the PRD and addendum resolved in this checkout. [Assumptions index](prd.md#assumptions-index) · [Glossary](prd.md#glossary)

**Current document findings** — 0 critical, 0 high, 0 medium, 0 low.
