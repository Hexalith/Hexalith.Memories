# Addendum — Hexalith.Memories PRD

**Role:** Mechanism, topology, rejected alternatives, and change-control residue that must not live in the product-outcome PRD.
**Updated:** 2026-09-12
**Does not override:** `prd.md` ship gates, FR/NFR IDs, or Glossary terms.

## Why this file exists

The 2026-09-05 PRD Update moved implementation how-to out of the requirements contract: Aspire topology, OpenBao, package graphs, fusion numeric weights, Dapr Agents, and SDK pins. Architecture remains authoritative for those facts when they drift.

**Current handoff:** G6 cannot pass until architecture qualifies AD-14 by phase and extends its binding to FR75 and NFR37. Every MVP requirement that lacks current-wording verification must also have current evidence or an approved exception with an owner in `sprint-status.yaml`. See the PRD Release decision record and Open Questions 1, 2, 9, and 10.

## EventStore — three contracts (do not collapse)

1. **Domain truth (current MVP):** Case / MemoryUnit / Tenant durable commit is Hexalith.EventStore acknowledgement. Search/vector/graph are rebuildable projections. See PRD FR13, pipeline section, architecture driver #3 / Epic 21.
2. **Product integration (Phase 1.5):** CloudEvent auto-index, dual embedding, CausationId/CorrelationId edges (FR59–FR62, Epic 9).
3. **Runtime pin (architecture / Epic 28):** Which EventStore packages or SHA this repo consumes. Not a product FR. SDK/toolchain mismatches (e.g. 10.0.302 vs 10.0.400) are repository configuration, not PRD text.

## Fusion — decision vs numbers

- **PRD decision:** Weighted reciprocal-rank fusion (NFR24). Magnitude-blend (BM25 normalization + cosine + graph proximity weighting) is a rejected alternative.
- **Architecture-owned canonical V1 behavior:** Process providers in order. Discard blank IDs and non-finite scores. Retain the first exact memory-unit ID. Use `Double.Equals` for finite-score ties and assign competition ranks (`1,1,3`). Calculate each contribution as `1 / (10 + rank)`. Default syntactic/semantic/graph weights are `0.30/0.35/0.35`; NL is `0.20`, disabled by default, and limited to requested/configured Phase 1.5 event units.
- **Tenant-wide graph merge:** partition seeds by ascending ordinal case ID, retain the maximum finite graph score per unit within its case, then merge by descending graph score, ascending ordinal case ID, and ascending ordinal memory-unit ID before rank allocation. Paths never cross cases.
- **Permission:** Changing weights during calibration does not require a PRD rewrite if NFR24/NFR25/NFR26 still hold and architecture versions the canonical contract.

## Benchmark — what the shipped suite actually is (2026-09-08 extract)

Source: `tests/Hexalith.Memories.Benchmarks` (`BenchmarkSuiteTests.ThesisValidation_HybridOutperforms80Percent`, `Data/ground-truth.json`, `Data/synthetic-corpus.json`), last recorded run 2026-07-16 (Epic 26.8).

| Aspect | Shipped suite | PRD thesis-gate protocol (G1) |
|---|---|---|
| N | 8 queries (BQ-01…BQ-08) | ≥ 50 topics |
| Query mix | Every query tagged `requiredAxes: [syntactic, semantic, graph]` | Representative mix; thesis-stress is a labelled slice |
| Corpus / labels | Synthetic corpus; `expectedResults` authored with the corpus | Real Phase 1 corpus; graded labels by Jerome + 2 reviewers, κ ≥ 0.6 |
| Control | Best single active axis (`hybridNdcg > bestSingleNdcg`) | BM25+semantic two-axis RRF |
| Win rule | Any positive Δ | ΔNDCG@10 ≥ 0.02 |
| Graph edges in corpus | `references` 29, `causedBy` 9, `correlatedWith` 5 | Whatever the frozen real corpus contains under the Phase 1 population rule |
| Result | 8/8 wins, reproducible twice | Not yet run |

The suite remains the NFR26 reproducibility gate and a regression check. Expanding it into the G1 protocol is engineering work to be sprint-selected (Epic 26 successor); the PRD owns the numbers, the README restates them.

## Release decision — why dated no-go rather than "gate must pass"

The 2026-09-08 validation found that every prior gate could only delay. The PRD now carries a Release decision record with a decision date per gate and a stated failure outcome. Jerome confirmed **2026-12-01** for the former G1–G5 thesis gate and **2027-01-01** for Phase 1.5 launch; the 2026-09-12 architecture reconciliation added G6. Two points remain explicit assumptions pending Jerome's ratification: retaining 2026-12-01 for the expanded G1–G6 gate and using **2026-10-31** as the prerequisite checkpoint. Moving a confirmed date requires a sprint-change proposal, not an edit.

## Isolation mechanism

- **PRD outcome:** NFR8, FR38, FR40 — zero leaks, tenant-scoped resources, automated verify.
- **Architecture-owned:** per-tenant Redis ACL users + tenant-scoped backend resolution. Key prefixes, hash tags, and logical DBs are placement tools, not the primary security boundary (Story 24.3).
- **MVP isolation tier:** shared cluster, tenant-scoped principals and indexes, API enforcement (not separate processes/volumes per tenant unless a later approved change says so).

## Ingestion runtime

- **PRD:** DAPR Workflow owns stages, retry, compensation. Rate-limiter actor owns embedding budget.
- **Rejected:** per-tenant document-queue pipeline actor as orchestrator (pre-D23).
- **NFR17 proof:** workflow history / Durable Task persistence, not "pipeline actor state."
- **Projection completion:** work is keyed by `(tenantId, caseId, memoryUnitId, authoritative EventStore sourceVersion, schemaGeneration, embeddingConfigurationEpoch)`. One DAPR-state coordinator advances per-axis checkpoints monotonically with ETag/CAS, rejects stale acknowledgements, and shares the protocol across ingest, repair, and replay.
- **Idempotency:** V1 tokens are scoped by tenant, case, and command/operation. CloudEvent identity is tenant + case + exact validated `source` + `id`, compared ordinally with no post-validation normalization. Durable EventStore/workflow suppression owns the guarantee; projections are idempotent upserts for the completion tuple. Redis preflight reservation is fail-open admission optimization only and never durable truth.
- **Recovery distinction:** authoritative EventStore replay, application export restore, and the current Redis-input consistency repair are different operations. The final architecture spine records replay-to-all-current-projections as an implementation gap.

## Identity

- Tenant claims authorize. Case membership is metadata. Provenance binds to authenticated `sub`; internal provenance maps an authenticated app ID through one operator-owned finite allowlist to a canonical `system:*` principal and an explicit tenant grant. App identity or channel credentials alone never authorize.
- Epic 20 JWT on `/api/**` with anonymous health/Dapr exceptions is the current MVP ingress reality (NFR11).

## Topology and secrets (not extra product surfaces)

- .NET Aspire AppHost is the current orchestration path (`dotnet run --project Hexalith.Memories.AppHost`). Not a second product.
- Runtime secrets: DAPR Secrets API backed by OpenBao (NFR9). Deployments provide only minimum documented OpenBao bootstrap material outside that path. Direct Kubernetes injection of Redis/FalkorDB credentials is a temporary alignment gap, not a second approved application-secret path.
- Optional Python `ai-agent` sidecar is architecture-owned and deferred until a selected feature requires it (Open Question 7 closed 2026-09-12).
- Access telemetry may use PostgreSQL (Epic 27); that is not a search-backend pivot. Search remains Redis + FalkorDB until NFR15/Qdrant work ships.

## Tenant erasure

- **PRD outcome:** FR39/NFR16 — verified projection purge, irreversible EventStore content inaccessibility, telemetry erasure handoff, no replay/restore resurrection, and no tenant-ID reuse.
- **Architecture-owned mechanism:** Hexalith.EventStore tenant-key crypto-shredding; content-free deletion evidence/tombstones; unreadable restored payload quarantine rather than rehydration.
- **Telemetry boundary:** tenant deletion records the durable mapping/handoff but does not force early deletion of retained opaque telemetry. NFR34 owns TTL, purge progress, and any separately adopted accelerated purge operation.

## Capacity and backpressure

- Tenant and global quotas have separate owners; admission and concurrency are tenant-partitioned and queues are bounded/durable.
- Provider `Retry-After` is honored through workflow timers, not unbounded in-memory retries.
- Repair, recovery, and migration work run below interactive query/ingestion priority. PRD NFR12/NFR13/NFR22/NFR36 own measurable fairness and freshness outcomes.

## Migration phasing

- The final architecture spine states an unphased create-backfill-verify-switch-retire rule using disjoint active/staging resources and atomic activation. The pre-existing product contract places that experience in Phase 2 for embedding/schema migration and Phase 3 for backend migration; MVP FR43 permits an acknowledged degraded rebuild.
- That phase boundary is **not yet ratified in the final architecture authority**. G6 cannot pass until architecture either qualifies AD-14 by phase or product explicitly rebaselines MVP through sprint change. This addendum records the conflict; it does not manufacture an exception.

## Package inventory

- Sole published-ID source: `tools/release-packages.json`.
- Non-packable hosts are listed in the PRD as a separate table. Do not invent a "9+3" slogan that disagrees with those two lists.
- Compatibility-only `Hexalith.Memories.Redis` facade: composition-root registration; Server must not take it as a transitive domain dependency.

## Language / SDK

- Language baseline in the PRD: .NET 10 / C# 14.
- SDK pin: `global.json` only. Dated SCPs that mention 10.0.302 or 10.0.400 are historical verified facts, not PRD pins.

## Brief residue (2026-03-22) not absorbed as FRs

Qualitative ideas to re-home in UX or docs, not silent FRs:

- Brand line "Connected knowledge that understands why"
- README-as-product / 30-second demo craft
- `explore` as trust-building (Phase 1.5 CLI)
- Priya as landing-page screenshot (Phase 2 UX)
- "Not ChatGPT memory" / not per-user silos (now a Non-Goal)

Brief claims deliberately **not** carried into the PRD (2026-09-08 product-brief reconciliation; each is a conscious drop, re-open by sprint change):

| Brief item | Disposition |
|---|---|
| Custom extraction phrases | Phase 2 "extraction phrase templates" — already listed; no FR until Phase 2 planning |
| Second embedding provider within 6 months | Post-MVP provider table (OpenAI/Mistral/Ollama); no dated commitment in the PRD |
| "Works with all DAPR state stores" | Dropped. Search backend is Redis + FalkorDB; NFR15 only preserves the migration path |
| "5× productivity" | Dropped as unmeasurable; Marcus/Priya journeys carry the qualitative version |
| "Why now / 12–18-month window" | Positioning context for README/marketing, not a requirement |
| Priya's search-success metric | Phase 2 web surface; measure when Journey 8 has a surface (Epic 17 activation) |
| Motivating benchmark scene | Replaced by the thesis-gate protocol; the anecdote does not belong in a contract |
| Cross-case insight discovery | Tenant-wide discovery already exists under FR34 with mandatory case attribution; only cross-case references and graph paths are blocked by FR32/FR33 and deferred under Open Question 5 |

## Rejected alternatives (this Update)

| Alternative | Why rejected |
|---|---|
| Keep a single "MVP go/no-go" mixing MCP, EventStore product integration, and CLI thesis | Two incompatible ship contracts (validation critical) |
| Pull MCP into thesis MVP if Phase 1.5 slips | Scope accordion; delays those surfaces instead |
| Defer cases/tenant isolation under resource pressure | Contradicts non-retrofittable foundation and NFR8 |
| Atomic triple-write across Redis backends | Architecture EventStore-commit + projections |
| C# 13 / SDK number in the PRD | Repository `global.json` + C# 14 |
| One NFR33 id for both web performance and freshness | Split: NFR33 freshness, NFR35 web performance |
| Move FR46–FR52 + NFR4 to Phase 1.5 (2026-09-08 reviewers) | Epics 1 and 4 shipped them as MVP graph mechanics; the PRD instead states the Phase 1 population rule and keeps causal *completeness* as gate L3 |
| Accept the N=8 benchmark as the thesis gate | Control, N, labels, and win rule do not meet the protocol; recorded as diagnostic |
| Retitle the 80% line as diagnostic only | Would leave the thesis with no falsifier; protocol numbers were put in the PRD instead |
| Cut FR1–FR75 to the active ship contract | Status register per FR/NFR chosen instead; the horizon inventory stays with explicit delivery state |
| Pull architecture AD-14 into MVP without a scope decision | Contradicts approved Phase 2/3 migration sequencing; the target is preserved above without silently expanding FR43 |
| Remove `--tenant` from the CLI | It is a shipped, required flag on `search query`; `tenant switch` (ambient state) was removed instead |
| Move NFR9 (OpenBao) / package tables / Aspire out of the PRD | August 2026 SCP amendments; they stay, with mechanism detail here |

## Downstream drift not fixed by this Update (handoff)

The final 2026-09-09 architecture spine supersedes legacy `architecture.md` for architecture decisions. The PRD incorporated its product-observable guarantees on 2026-09-12, but reconciliation remains incomplete until AD-14's phase boundary is ratified and the spine binds FR75/NFR37. `epics.md` and `sprint-status.yaml` also need a change-control pass for the strengthened contracts and corrected status claims. The Story 27.4 live-producer contract stays out of the PRD.

Further handoff items recorded on 2026-09-08 (afternoon adversarial pass), none applied to code or downstream documents by this Update:

- `tests/Hexalith.Memories.Benchmarks`: rename `ThesisValidation_HybridOutperforms80Percent` (and the `ThesisValidated` flag) to a diagnostic/regression name; add the two-axis BM25+semantic control, the per-topic aggregate guards, the edge-source census, and the cached-embedding hash. A passing run of the current test is not G1 evidence.
- `README.md` line 84 says "per-tenant audit events (FR67)"; the PRD glossary bans "audit" for access telemetry. Reword downstream.
- `README.md` line 7 quotes NFR31 as "approximate"; NFR31 now defines the clean machine and the recorded-run requirement. `docs/dev/quickstart-walkthrough-log.md` has no run.
- Licence: confirmed MIT by Jerome on 2026-09-08 (Open Question 4 closed); the repository already complies. Remaining downstream action: add the README sentence "Hexalith.Memories is committed to the MIT license. We will not change to a restrictive license." Any older Apache 2.0 wording in `architecture.md`/`epics.md` is drift to remove.
- Dates: Phase 1.5 launch decision 2027-01-01 and the rule "release (thesis-gate) decision = launch − 1 month" (→ 2026-12-01) are confirmed by Jerome; the prerequisite sprint-selection date 2026-10-31 is derived and should be confirmed at the next sprint planning.
- `HybridSearchService` skips the graph axis without a start node; the PRD now requires auto-seeding from top-5 syntactic + top-5 semantic (FR17). No owning story exists yet.
- MCP parameter naming (`axis` vs `axes`) is declared once in the CLI surface table (`--axis`); the MCP tool schema should be checked against it when Epic 10 hardening is next touched.
- The final architecture spine frontmatter still binds `FR1-FR74` and `NFR1-NFR36`; refresh it to include new MVP idempotency requirement FR75 and active-CLI accessibility NFR37 after this PRD Update is accepted.

## August 2026 SCP apply log

Applied into `prd.md` on 2026-09-05 from:

- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` PRD-1…PRD-7
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` PRD-1…PRD-6

Reconciliation of the NFR33 collision is recorded in the PRD Assumptions Index.
