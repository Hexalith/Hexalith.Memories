# PRD-to-Spines Reconciliation — Pinned Current Pass

**Snapshot:** complete 1,243-line PRD, SHA-256 `12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1`, compared with the current patched `DESIGN.md` and `EXPERIENCE.md` on 2026-09-12. [PRD lineage](../../../prd.md#L1) [DESIGN lineage](../DESIGN.md#L12) [EXPERIENCE lineage](../EXPERIENCE.md#L12)

**Verdict:** **PASS for source reconciliation.** There are no unresolved PRD-to-spine blockers. The product itself remains **no-go**: all six Phase 1 gates remain hard and unpassed, L1–L3 are unevaluated, and the exact delivery/evidence gaps remain external blockers rather than UX-document defects. [Release posture](../../../prd.md#L62) [Spine release governance](../EXPERIENCE.md#L44)

## Counted result

| Measure | Current count | Disposition |
|---|---:|---|
| Pinned-authority declarations | **2/2 spines** | Exact SHA and same-day reciprocal-link rule present. |
| Phase/surface matrix rows | **7/7** | Phase 1 CLI, Phase 1 REST, Phase 1.5 MCP, Phase 1.5 EventStore, Phase 2 downstream app, Phase 3, and non-product Web specimen retained. |
| Exact journeys | **10/10** | Exact current titles, protagonists, phases, climaxes, and applicable recovery retained. |
| Phase 1 gates | **6/6** | All explicitly hard; none claimed passed. |
| Phase 1.5 launch gates | **3/3** | L1–L3 retained as unevaluated/no-launch. |
| Functional requirements accounted for | **75/75** | Every FR group has a surface/state consequence; FR75 is explicit. |
| Non-functional requirements accounted for | **37/37** | Every NFR group has an experience consequence; NFR37 is explicit and unverified. |
| PRD open-question dispositions captured in extraction | **10/10** | UX-relevant open items are reflected or explicitly deferred; closed non-UX decisions are not inflated into UI. |
| Current non-product component mappings | **19/19** | DESIGN names each implemented specimen composition without activating a product route. |
| Unresolved PRD-to-spine blockers | **0** | Pass. |
| Current product release gates not passed/evaluated | **6/6 G gates; 3/3 L gates** | Product no-go remains. |
| Accepted deferral classes | **7** | Explicit and phase/owner bounded below. |
| Non-blocking stale evidence notes | **0** | Protocol-extract item 14 now matches the pinned PRD and current search grammar. |

## Authority and source coverage

- Both spines pin the exact PRD hash and state that reciprocal same-day PRD links do not reverse authority for this migration. [DESIGN frontmatter](../DESIGN.md#L5) [EXPERIENCE frontmatter](../EXPERIENCE.md#L5)
- The behavioral spine correctly separates product authority, serialized contract/current delivery authority, and architecture/component authority. It preserves shared semantics without claiming feature parity. [EXPERIENCE foundation](../EXPERIENCE.md#L24)
- The visual spine is a delta over FrontComposer/FC-A11Y/Fluent, introduces no independent theme scale, and marks the 19 components as non-product conformance inventory. This does not overreach the PRD’s terminal-first phase plan. [DESIGN foundation](../DESIGN.md#L179) [DESIGN component status](../DESIGN.md#L211)
- The regenerated [PRD extraction](extract-prd.md#L1) covers the full pinned source, including gates, journeys, CLI, MCP, authority, trust, lifecycle, FR1–FR75, NFR1–NFR37, open questions, and surface consequences.

## Phase, release, and gate retention

The spine’s seven-row matrix exactly retains the PRD phase boundary: active-but-incomplete Phase 1 CLI; shipped REST transport that is not application UI; Phase 1.5 MCP and EventStore preview; Phase 2 downstream application; Phase 3 discovery; and the non-product Web specimen. [PRD scope](../../../prd.md#L219) [Spine matrix](../EXPERIENCE.md#L32)

The current no-go decision is explicit. The spine correctly preserves G1–G6 as hard, the 2026-12-01 date as an assumption, G6’s missing-owner/AD-14/FR75/NFR37 blockers, and the rule that L1–L3 wait for Phase 1. [PRD release decision](../../../prd.md#L208) [Spine governance](../EXPERIENCE.md#L44)

Point-of-use maturity is also retained: FR6/FR13/FR34/FR39/FR65/FR75 are partial; FR44 is hardening; FR8/FR66/FR72 require re-verification; NFR37 is not started. Normative target behavior is not presented as current delivery. [PRD FR status](../../../prd.md#L986) [PRD NFR status](../../../prd.md#L1109) [Spine maturity qualifier](../EXPERIENCE.md#L46)

## Exact J1–J10 coverage

| ID | Exact current PRD title | Spine disposition |
|---|---|---|
| J1 | **Alex — "Zero to First Search" (Phase 1.5 launch path)** | Exact title; EventStore/CLI preview, CausationId climax, separate L2 clock, and prerequisite/replay recovery retained. [PRD J1](../../../prd.md#L307) [Spine J1](../EXPERIENCE.md#L284) |
| J2 | **Alex — "Something's Wrong" (Debug Path)** | Exact title; empty classification, real `handlers list/mismatches`, absent case/failed status, and replay recovery retained. [PRD J2](../../../prd.md#L327) [Spine J2](../EXPERIENCE.md#L297) |
| J3 | **Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)** | Exact title; four tools, token budget, deterministic omission, traversal, structured recovery, and no-hallucination fallback retained. [PRD J3](../../../prd.md#L347) [Spine J3](../EXPERIENCE.md#L310) |
| J4 | **Marcus — "Brief the New Person" (Phase 2)** | Exact title; downstream briefing, membership-as-metadata, source/date discrepancy, annotation, and sparse/stale/conflict recovery retained. [PRD J4](../../../prd.md#L363) [Spine J4](../EXPERIENCE.md#L323) |
| J5 | **Kenji — "New Tenant, No Drama" (MVP)** | Exact title; target operator CLI, principal-driven verification, current absent verbs, fail-closed mismatch, and telemetry split retained. [PRD J5](../../../prd.md#L383) [Spine J5](../EXPERIENCE.md#L336) |
| J6 | **Kenji — "Time to Scale" (Phase 3)** | Exact title; assessment, dry run, fairness, rollback, verified cutover, and no invented grammar retained. [PRD J6](../../../prd.md#L399) [Spine J6](../EXPERIENCE.md#L349) |
| J7 | **LLM Agent — Technical Integration Path** | Exact title; non-human MCP flow, evidence states, omission, retry, freshness, no-safe-axis, and no-screen disposition retained. [PRD J7](../../../prd.md#L417) [Spine J7](../EXPERIENCE.md#L362) |
| J8 | **Priya — "I Need to Understand This Case" (Phase 2 downstream application)** | Exact corrected current title and Phase 2 surface; chronological evidence, source verification, and relevance-not-fact climax retained. [PRD J8](../../../prd.md#L447) [Spine J8](../EXPERIENCE.md#L375) |
| J9 | **Alex — "The First Case" (Empty State)** | Exact title; Phase 1 manual path, target/current distinction, six-state progress, G3 blocker, and no Event auto-index shortcut retained. [PRD J9](../../../prd.md#L467) [Spine J9](../EXPERIENCE.md#L388) |
| J10 | **The Contributor — "From Bug Report to First PR"** | Exact title; build/test/CI/review recovery and explicit infrastructure-only/no-screen disposition retained. [PRD J10](../../../prd.md#L487) [Spine J10](../EXPERIENCE.md#L401) |

The phase roll-up remains J5/J9 Phase 1, J1/J2/J3/J7 Phase 1.5, J4/J8 Phase 2, J6 Phase 3, and J10 ecosystem infrastructure. [PRD journey summary](../../../prd.md#L505)

## FR1–FR75 consequence coverage

| FR set | PRD consequence retained in the spines |
|---|---|
| FR1–FR13 + FR75 | Six-state human lifecycle mapped to V1 values, current-revision/configuration-aware three-projection completion, visible failure/recovery, restart safety, and one-operation duplicate suppression without inventing a public idempotency input. [PRD ingestion FRs](../../../prd.md#L1004) [Spine lifecycle](../EXPERIENCE.md#L158) |
| FR14–FR25 | Three search axes, deterministic RRF, selection/explain/inspection/omission, graph auto-seeding, BM25+semantic control, and present implementation gaps. [PRD retrieval FRs](../../../prd.md#L1021) [Spine retrieval](../EXPERIENCE.md#L222) |
| FR26–FR37 | Case lifecycle/activity/annotation/member attribution, one-case ownership, case-local traversal, and attributed tenant-wide retrieval. [PRD organization FRs](../../../prd.md#L1033) [Spine scope](../EXPERIENCE.md#L216) |
| FR38–FR45 | Tenant provisioning/configuration/verification/deletion, principal-driven isolation, layered internal authority, and verified erasure without membership-as-auth. [PRD tenancy FRs](../../../prd.md#L1044) [Spine scope and erasure](../EXPERIENCE.md#L173) |
| FR46–FR52 | Typed/directed graph paths, edge confidence, literal gaps, filter/depth constraints, and case boundary. [PRD graph FRs](../../../prd.md#L1054) [Spine graph interaction](../EXPERIENCE.md#L230) |
| FR53–FR58 | CLI as operational superset, exact current grammar/status, exactly four MCP tools, machine-readable outcomes, and current exceptions. [PRD interfaces](../../../prd.md#L1065) [Spine CLI](../EXPERIENCE.md#L48) [Spine MCP](../EXPERIENCE.md#L73) |
| FR59–FR62 | EventStore CloudEvents, dual embeddings, causal metadata, handler evolution, and Phase 1.5 preview status. [PRD EventStore FRs](../../../prd.md#L1073) [Spine phase matrix](../EXPERIENCE.md#L38) |
| FR63–FR67 | Source/provenance, relevance caveat, omission, freshness, axis availability, capability-aware degradation, no-safe-axis failure, and current provenance gap. [PRD trust FRs](../../../prd.md#L1081) [Spine evidence states](../EXPERIENCE.md#L141) |
| FR68–FR70 | Provider selection/configuration and visible rate-limit/retry-after behavior without exposing secrets. [PRD provider FRs](../../../prd.md#L1089) [Spine long-running states](../EXPERIENCE.md#L169) |
| FR71–FR74 | Early portable export without phase change, liveness/readiness split, sanitized telemetry, and bounded truth-preserving repair. [PRD operations FRs](../../../prd.md#L1095) [Spine health/telemetry](../EXPERIENCE.md#L173) |

## NFR1–NFR37 consequence coverage

| NFR set | Retained disposition |
|---|---|
| NFR1–NFR7 | Exact search/traversal, throughput, EventStore freshness, and cold-start budgets drive loading/delay/degradation states. [PRD](../../../prd.md#L1125) [Spine timing](../EXPERIENCE.md#L200) |
| NFR8–NFR11 | Principal-driven isolation, ingress auth, retry-after, versioning, and preserved structured errors fail closed. [PRD](../../../prd.md#L1137) [Spine scope](../EXPERIENCE.md#L216) |
| NFR12–NFR15 | Scale, fairness/noisy-neighbor behavior, migration, and backend/provider boundaries remain visible and phase-qualified. [PRD](../../../prd.md#L1146) [Spine progress](../EXPERIENCE.md#L169) |
| NFR16–NFR19 | Restart-safe current-revision convergence, erasure/non-resurrection, safe degradation, and reproducible setup become explicit state/recovery requirements. [PRD](../../../prd.md#L1155) [Spine lifecycle/health](../EXPERIENCE.md#L158) |
| NFR20–NFR23 | MCP interoperability, contract compatibility, event handling, and projection convergence preserve cross-surface semantics without parity claims. [PRD](../../../prd.md#L1164) [Spine foundation/MCP](../EXPERIENCE.md#L28) |
| NFR24–NFR26 | Deterministic fusion/ties, score semantics, and null/empty meaning constrain explanation and benchmark presentation. [PRD](../../../prd.md#L1173) [Spine retrieval/benchmark](../EXPERIENCE.md#L138) |
| NFR27–NFR29 | Sanitized observability, attribution, access telemetry, queue/failure visibility, and retention remain separate from a legal/tamper-evident audit claim. [PRD](../../../prd.md#L1181) [Spine telemetry](../EXPERIENCE.md#L181) |
| NFR30–NFR31 | Example help and the manual clean-machine <30-minute path are retained; `quickstart` cannot substitute for G3 evidence. [PRD](../../../prd.md#L1189) [Spine timing](../EXPERIENCE.md#L209) |
| NFR32–NFR35 | Future-web WCAG, performance, freshness, reflow, and dated browser/AT gates remain conditional on product activation; specimen evidence does not transfer. [PRD](../../../prd.md#L1196) [Spine future web](../EXPERIENCE.md#L266) |
| NFR36 | Small/large file time-to-index budgets and visible provider/fairness delay are explicit and evidence-qualified. [PRD](../../../prd.md#L1205) [Spine timing](../EXPERIENCE.md#L210) |
| NFR37 | Active CLI text labelling, narrow/redirected/no-color behavior, bounded linear output, explicit progress/timeout/cancellation, secret safety, cross-form semantic parity, automation, and keyboard-only walkthrough are explicit; current status remains not started/unverified. [PRD](../../../prd.md#L1211) [Spine CLI accessibility](../EXPERIENCE.md#L253) |

## Settled search rule and live-code deferral

The pinned PRD is unambiguous: public search has **no start-node input**. FR17 must auto-seed the graph contribution from the union of top-five syntactic and top-five semantic candidates and traverse depth≤2 in each case partition. Explicit start nodes belong only to CLI `traverse` and MCP `traverse_relations`. Current skip-without-start behavior is a delivery gap. [PRD decision](../../../prd.md#L167)

The spine repeats this precisely, does not invent search `--from`, does not invent a V1 graph-start-provenance field, and marks both missing auto-seeding and missing tenant-wide case merge as current gaps. [Spine rule](../EXPERIENCE.md#L224)

The working protocol extraction now records the same boundary: current public search has no `--from`, matching the pinned PRD, while explicit starts belong to traversal. The protocol extract remains implementation evidence rather than phase authority. [Protocol evidence](extract-protocol-implementation.md#L282)

## Current implementation names/status retained without overreach

- The spine carries exact current CLI globals, registrations, bounds, placeholders, absent target verbs, exit codes, JSON envelopes, raw-export exception, cancellation exception, and token-source debt. [Spine CLI table](../EXPERIENCE.md#L48)
- The four MCP names and public inputs are exact; search `maxResults` is 1..100, traversal `depth` is 0..10, and edge literals are constrained. The spine distinguishes current accepted inputs/payloads from PRD target breadth. [Spine MCP map](../EXPERIENCE.md#L73)
- Serialized Evidence Packet names and enum values remain `Contracts.V1`-owned. Current per-tool coverage, null metadata, freshness/lifecycle vocabulary differences, NL/graph summary gaps, and narrow case-info payload are presented as current implementation gaps rather than product decisions. [Spine current-vs-activation components](../EXPERIENCE.md#L115) [Protocol evidence](extract-protocol-implementation.md#L265)
- Current Web code’s lack of a versioned source-conflict field, and its unsafe mapping of backend/axis loss to `Conflicting`, is explicitly called a delivery divergence. UX does not invent a `disputed` packet state. [Spine conflict disposition](../EXPERIENCE.md#L156)
- DESIGN maps all 19 exact implemented component compositions and labels them conformance-tested specimen evidence, not a future-product contract. [DESIGN component map](../DESIGN.md#L41) [DESIGN status](../DESIGN.md#L211)

## Overreach and omission audit

### Genuine unresolved blocking gaps: 0

No journey is missing or misphased; no target CLI verb is called shipped; no placeholder subtree is invented; no MCP operator parity or extra wire field is claimed; no Web specimen is called a product route; no relevance score is called truth; no two-of-three projection is called indexed; and no weak authorization/erasure/telemetry shortcut is blessed.

### Explicitly accepted deferral classes: 7

1. J4/J8 downstream application composition and chrome remain Phase 2 and host-owned. [Phase boundary](../EXPERIENCE.md#L40)
2. J6, Memory Explorer, Timeline, and migration operations remain Phase 3 discovery with no invented command grammar. [Phase boundary](../EXPERIENCE.md#L41)
3. The Web/RCL host remains a non-product specimen; routing, authorization, live data, focus, and notifications require future activation evidence. [Specimen boundary](../EXPERIENCE.md#L42)
4. NFR32/NFR35 remain future-web activation gates; current specimen evidence does not transfer. [Future-web gate](../EXPERIENCE.md#L266)
5. Open Question 3 keeps compact trust visible while detailed explain default remains undecided until web activation. [Explain disposition](../EXPERIENCE.md#L228)
6. Cross-case associations, additional ingestion sources, and the Python sidecar remain PRD-owned later/architecture decisions rather than present UX. [Open Questions](../../../prd.md#L1217)
7. Legacy compare/copy/permission/neighborhood editing and downstream query/filter/chrome actions remain host- or capability-owned. [Legacy disposition](../EXPERIENCE.md#L244)

## Remaining product blockers—not reconciliation defects

- **G1:** auto-seeding/case merge, BM25+semantic control, and qualified corpus/label evidence remain incomplete. [PRD G1](../../../prd.md#L158) [Spine benchmark](../EXPERIENCE.md#L138)
- **G2/G4:** zero-leak and case-partition tests still require current gate evidence. [PRD gates](../../../prd.md#L184)
- **G3:** `tenant create`, `case`, and `ingest` prevent the required manual onboarding stopwatch. [PRD release record](../../../prd.md#L212) [Spine CLI status](../EXPERIENCE.md#L68)
- **G5:** deterministic fusion/current-wording evidence remains a gate; it is not inferred from UI formatting. [PRD G5](../../../prd.md#L188)
- **G6:** missing successor-story ownership, AD-14 ratification, strengthened FR/NFR evidence, architecture binding for FR75/NFR37, and NFR37’s absent evidence remain blockers. [PRD G6](../../../prd.md#L189) [PRD Open Questions](../../../prd.md#L1217)
- **L1–L3:** MCP relevance/budget, EventStore stopwatch, and causal-chain fixture gates remain unevaluated. [PRD launch gates](../../../prd.md#L193)

## Final disposition

The patched spines retain the pinned PRD’s exact journeys, phase/status boundaries, G1–G6/L1–L3 governance, all FR1–FR75 and NFR1–NFR37 UX consequences, the no-search-start/automatic-seeding rule, and current implementation caveats. Documentation blocker count is **0**. The only remaining source note is the superseded sentence in the non-authoritative working protocol extract; the spines already defer correctly to the pinned PRD.
