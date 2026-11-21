# PRD Quality Review — Hexalith.Memories

## Overall verdict

The November PRD supports the decision to hold Phase 1 and identifies what would reopen it. Its thesis, non-goals and separation of diagnostic from qualifying evidence are clear. Revise the gate specifications, tenant authority rules and delivery register before using them as acceptance instructions: independent teams can currently reach different verdicts on the same evidence.

## Decision-readiness — adequate

The release table assigns ownership, distinguishes a missing G1 run from a measured failure and identifies the single permitted schedule reset. Those are actionable decisions even while reviewers and target operations are outstanding (lines 167–182 and 215–228). L1 has a defect in its scoring protocol that recruiting reviewers or building the sample cannot fix.

### Findings

- **[high]** L1 cannot score its held-out topics (§ PRD lines 200–205) — The topics must be “not used in G1 labelling,” but answer relevance is scored using “the frozen G1 labels.” Topic-specific relevance judgments for these topics therefore do not exist in the named scoring set. The acceptance denominator “≥ 8/10” is also ambiguous when the set exceeds ten. *Fix:* Before the agent run, independently label and freeze the L1 topic–unit pairs, specify the minimum relevant grade, and select either exactly ten topics or a success proportion across the entire preregistered set. Preserve the 100% token-budget condition.
- **[medium]** G1 has no shared rule for reconciling three reviewers’ grades (§ PRD lines 170–177) — “Graded labels” and pairwise Cohen’s κ establish who judges and the minimum agreement, but residual disagreement can remain after κ passes. Two scorers could pool the same three grades differently and move a topic across ΔNDCG@10 of 0.02. *Fix:* Bind or link the pre-freeze grade scale, treatment of unjudged units and adjudication or aggregation rule, then freeze the resulting topic–unit judgment file and scoring parameters before comparing systems.

## Substance over theater — adequate

The eight-topic diagnostic is reported with its comparator and corpus limitations (lines 167–180). The responsibility boundary between retrieval relevance and factual correctness drives explanations for users (lines 569–590). The competitive section identifies relevant hybrid search and graph alternatives, but it concludes more about the market than the proposed experiments establish.

### Findings

- **[low]** The positioning claim goes beyond the stated evidence (§ PRD lines 643–669) — “This framing creates a defensible product category” describes a market outcome; the competitor table and L3 test cover technical differences and causal-chain retrieval only. *Fix:* Keep the three-tier responsibility model as an operating boundary and present defensibility as a hypothesis with an adoption signal, or remove that claim.

## Strategic coherence — strong

Phase 1 tests the incremental value of graph retrieval over BM25 plus semantic retrieval. The mean and worst-topic guards prevent an aggregate improvement from masking substantial regressions (lines 167–182). The explicit actions after a measured failure change the default product and defer launch. Phase 1.5 separately tests agent utility and EventStore integration (lines 198–228). Business adoption and community measures remain diagnostic signals (lines 140–159). The defects below concern the execution of that strategy.

## Done-ness clarity — thin

FR2 precisely describes which surfaces must omit sensitive URL material. FR13, NFR16 and NFR31 specify observable outcomes, failure behavior and test conditions. Other normative rows conflict with their own phase rules or with ratified security and wire contracts. These conflicts affect which tests may pass, even if every missing feature is delivered.

### Findings

- **[high]** NFR21 still makes publisher-selected routing part of acceptance (§ PRD lines 1177–1182) — NFR21 includes “source-prefix routing” in the accepted EventStore convention. Line 273 explicitly identifies it as legacy behavior and says AD-5 forbids selecting a tenant from a publisher-set field. A conformance test written from the NFR can reward a violation of the isolation boundary. *Fix:* Specify authenticated component/topic/publisher routing, grant and Active checks in NFR21. Keep envelope source validation for identity and require a negative case in which changing the envelope source cannot change the authorized tenant.
- **[high]** G5 can require an explicitly excluded launch surface (§ PRD lines 187–198) — G5 invokes NFR25, whose MVP verification at line 1189 requires golden vectors “across REST, CLI JSON, and MCP.” The gate table says “Phase 1 does not require MCP.” With identical REST and CLI evidence, two reviewers could reach opposite Phase 1 verdicts. *Fix:* Name the G5 surface set for Phase 1 and require the MCP parity vectors at their own activation gate. If the intended check is an offline adapter fixture, define its scope and the evidence still due on the enrolled target.
- **[high]** NFR19 claims verification for a CLI failure-status path the PRD says is unavailable (§ PRD lines 1124–1132) — The verified group includes NFR19, which requires “all failures visible via CLI status with error details and failure stage” (line 1173). Lines 903–910 say only telemetry status is wired; the absent failed-unit operation is also listed as partial under FR11. Downstream G6 accounting can omit the user-visible failure path. *Fix:* Cite a rerunnable end-to-end failure-stage check on an identified applicable CLI surface or reclassify NFR19, preserve the server-side evidence and record what remains to be shown.
- **[medium]** The NFR13 fairness formula and repair workload produce incompatible pass conditions (§ PRD lines 1159–1163) — “≤10% of its solo p95” literally requires a latency at most one tenth of baseline, whereas line 1252 describes 10% degradation. Repairing 100 units in a ten-minute window cannot sustain “≥25 units/min” over the full window. *Fix:* Define the allowable ratio to solo p95 explicitly and either measure repair throughput over active repair time with a completion deadline or supply sufficient work for the stated sustained rate.
- **[medium]** Hybrid explain uses two meanings of per-axis contribution (§ PRD lines 572–582) — The display table calls the exposed values “weighted reciprocal-rank contribution.” Addendum line 22 gives the raw term, while AD-9 and `FusionEngine` assign an unweighted, rank-one-normalized public per-axis value. A consumer could multiply the displayed axis value by its weight before comparing it to the canonical packet. *Fix:* Distinguish the public per-axis value from the internal weighted fusion calculation. Use the rank-one fixture and point all surfaces to AD-9 for the formula.

## Scope honesty — adequate

The PRD explicitly excludes the MCP launch, automatic EventStore ingestion, application Web UI and compliance claims from MVP (lines 111–120). The updated decision record and FR53 recognize the approved inventory and pending enrolled CLI. Some tables still export a different commitment when extracted on their own.

### Findings

- **[high]** The target CLI and transport are described as the legacy distribution (§ PRD lines 695–707) — The language/platform matrix specifies “dotnet tool install -g Hexalith.Memories.Cli” with “Same” for the future, and lines 746–766 route external CLI/agent consumers through legacy REST/MCP hops with bearer authority preserved. Lines 36, 258 and 893 instead designate McpCli as the target; the approved inventory documents workload Gateway calls that forward no user credentials. Reading the matrix alone directs work toward the wrong tool or an inadmissible transport. *Fix:* Mark the legacy install and topology locally as compatibility, reference the enrolled target contract for future coverage and require independent user/tenant authorization proof on any target route that needs it.
- **[medium]** The November status register leaves completed URL-display remediation looking pending (§ PRD lines 999–1009) — “DW-732 tracks redaction and regression tests” follows the historical finding without its disposition. The work register records resolution with focused and live coverage for logs and case activity. The target CLI portions of FR2 and FR36 remain partial. *Fix:* Record the scoped DW-732 closure and its evidence reference separately from the remaining enrolled CLI and durable-workflow-history obligations.

## Downstream usability — thin

Stable FR, NFR, journey and gate IDs support extraction, and the glossary distinguishes domain truth from EventStore launch integration. A few literal examples and technical summaries still encode incompatible identity or next-action behavior for downstream teams.

### Findings

- **[high]** External actor provenance loses its issuer (§ PRD lines 1101–1105) — FR65 says “normalized authenticated subject,” and lines 592 and 769 and addendum line 63 refer only to `sub`. The ratified AD-13 binds the authenticated issuer plus subject. Equal subjects from different accepted issuers can otherwise collapse to one audit attribution. *Fix:* Specify issuer-qualified subject identity for authenticated external callers and add a two-issuer/same-subject fixture. Keep workload-derived system principals and tenant authorization separate.
- **[medium]** The empty-state example recommends an unavailable action (§ PRD lines 478–488) — Journey 9 demands an “action they can complete” then prints “memories ingest <file>” and a guided wizard. The compatibility ingest command is a stub (line 904), and L06 is withdrawn from target coverage (line 893). FR56–57 forbid suggesting unimplemented actions. *Fix:* Separate target acceptance copy from a current compatibility transcript; show an available manual path until enrolled ingestion exists, and identify when the target copy can switch.
- **[low]** The membership example changes tenants between setup and retrieval (§ PRD lines 374–384) — Membership is added under “acme” and the same case is subsequently queried under “engineering.” Neither an authorized tenant switch nor a second case setup is described. A copied fixture would exercise a denial rather than the stated onboarding experience. *Fix:* Keep one tenant throughout the happy path and reserve a different tenant for an explicit denial case.

## Shape fit — adequate

The document combines decision gates, person-centered journeys and technical requirements in a form appropriate to a shared developer tool. The separate operational and agent journeys expose different authorization and response constraints. The addendum is well suited to volatile implementation facts, yet inventories duplicated in the PRD have already diverged.

### Findings

- **[medium]** The “sole release inventory” and its duplicated tables disagree (§ PRD lines 713–740) — The text claims “The six rows mirror `nonPackableProjects`.” The manifest contains seven, including DomainService, and ten packable projects, including EventStoreExtensions, versus nine in the PRD. The Web project is a Razor library; its runnable specimen is hosted in tests. *Fix:* Point to the manifest for the complete inventory or generate the two mandated tables from it and retain only the product-facing purpose and phase in prose.

## Mechanical notes

All 75 FRs, 37 NFRs, ten journeys, six thesis gates and three launch gates have unique definitions with no numerical gaps; FR75 is intentionally grouped with ingestion. All ten journeys identify a person or actor, including Dani and the LLM Agent. The six live inline assumption tags have matching index entries; line 1250 explicitly labels the historical NFR33/NFR35 collision as index-only. The Markdown file targets in the PRD and addendum resolve locally. The absent sample directories are identified as qualification prerequisites. Open Questions 1–3 have release or activation consequences, while items 4–10 include resolved decisions retained for change control.

Reviewed the complete 1,254-line PRD and 163-line addendum. Cross-checks used the current architecture, McpCli owner inventory, release manifest, scoped DW-732 resolution and fusion implementation. No product gate was rerun in this document review. [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [McpCli inventory](../../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md) · [Release manifest](../../../tools/release-packages.json) · [Deferred work](../../implementation-artifacts/deferred-work.md) · [Fusion engine](../../../src/Hexalith.Memories.Server/Search/FusionEngine.cs)
