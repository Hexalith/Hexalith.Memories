# Addendum — Hexalith.Memories PRD

**Role:** Mechanism, topology, rejected alternatives, and change-control residue that must not live in the product-outcome PRD.  
**Updated:** 2026-09-08  
**Does not override:** `prd.md` ship gates, FR/NFR IDs, or Glossary terms.

## Why this file exists

The 2026-09-05 PRD Update moved implementation how-to out of the requirements contract: Aspire topology, OpenBao, package graphs, fusion numeric weights, Dapr Agents, and SDK pins. Architecture remains authoritative for those facts when they drift.

## EventStore — three contracts (do not collapse)

1. **Domain truth (current MVP):** Case / MemoryUnit / Tenant durable commit is Hexalith.EventStore acknowledgement. Search/vector/graph are rebuildable projections. See PRD FR13, pipeline section, architecture driver #3 / Epic 21.
2. **Product integration (Phase 1.5):** CloudEvent auto-index, dual embedding, CausationId/CorrelationId edges (FR59–FR62, Epic 9).
3. **Runtime pin (architecture / Epic 28):** Which EventStore packages or SHA this repo consumes. Not a product FR. SDK/toolchain mismatches (e.g. 10.0.302 vs 10.0.400) are repository configuration, not PRD text.

## Fusion — decision vs numbers

- **PRD decision:** Weighted reciprocal-rank fusion (NFR24). Magnitude-blend (BM25 normalization + cosine + graph proximity weighting) is a rejected alternative.
- **Architecture-owned:** default axis weights, RRF `k` (Epic 26 recorded live calibration; do not copy numbers into the PRD).
- **Permission:** Changing `k`/weights during calibration does not require a PRD rewrite if NFR24/NFR25/NFR26 still hold.

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

The 2026-09-08 validation found that every prior gate could only delay. The PRD now carries a Release decision record with a decision date per gate and a stated failure outcome (kill-switch actions; no launch). Dates were set by the assistant during the Update and are tagged `[ASSUMPTION]` until Jerome confirms; moving a date requires a sprint-change proposal, not an edit.

## Isolation mechanism

- **PRD outcome:** NFR8, FR38, FR40 — zero leaks, tenant-scoped resources, automated verify.
- **Architecture-owned:** per-tenant Redis ACL users + tenant-scoped backend resolution. Key prefixes, hash tags, and logical DBs are placement tools, not the primary security boundary (Story 24.3).
- **MVP isolation tier:** shared cluster, tenant-scoped principals and indexes, API enforcement (not separate processes/volumes per tenant unless a later approved change says so).

## Ingestion runtime

- **PRD:** DAPR Workflow owns stages, retry, compensation. Rate-limiter actor owns embedding budget.
- **Rejected:** per-tenant document-queue pipeline actor as orchestrator (pre-D23).
- **NFR17 proof:** workflow history / Durable Task persistence, not "pipeline actor state."

## Identity

- Tenant claims authorize. Case membership is metadata. Provenance binds to authenticated `sub`; allowlisted `system:*` only through an authenticated service boundary (August 2026 PRD-3).
- Epic 20 JWT on `/api/**` with anonymous health/Dapr exceptions is the current MVP ingress reality (NFR11).

## Topology and secrets (not extra product surfaces)

- .NET Aspire AppHost is the current orchestration path (`dotnet run --project Hexalith.Memories.AppHost`). Not a second product.
- Runtime secrets: DAPR Secrets API backed by OpenBao (NFR9). Kubernetes Secrets only for documented OpenBao bootstrap.
- Optional Python `ai-agent` sidecar (architecture D27/D28) is architecture-owned unless Open Question 7 promotes it to a product NFR.
- Access telemetry may use PostgreSQL (Epic 27); that is not a search-backend pivot. Search remains Redis + FalkorDB until NFR15/Qdrant work ships.

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
| Cross-case insight discovery | Blocked by FR32 single-case ownership; Open Question 5 |

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
| Cut FR1–FR74 to the active ship contract | Status register per FR/NFR chosen instead; the horizon inventory stays with explicit delivery state |
| Remove `--tenant` from the CLI | It is a shipped, required flag on `search query`; `tenant switch` (ambient state) was removed instead |
| Move NFR9 (OpenBao) / package tables / Aspire out of the PRD | August 2026 SCP amendments; they stay, with mechanism detail here |

## Downstream drift not fixed by this Update (handoff)

The 2026-09-08 validation found `architecture.md` (Requirements Overview / Coverage / PRD Deviations: C# 13, 31 NFRs, P1.5 auth, actor pipeline, atomic-write sentences) and `epics.md` (Requirements Inventory: auth `[P1.5]`, actor pipeline, indexes as isolation boundary, NFR32–NFR35 missing, single onboarding clock; Epic 9 "zero-code") still describe the pre-2026-09-05 PRD. The PRD was not edited to match them. Re-extract those sections from `prd.md` + this addendum via `bmad-correct-course`; the Story 27.4 live-producer contract stays out of the PRD.

Further handoff items recorded on 2026-09-08 (afternoon adversarial pass), none applied to code or downstream documents by this Update:

- `tests/Hexalith.Memories.Benchmarks`: rename `ThesisValidation_HybridOutperforms80Percent` (and the `ThesisValidated` flag) to a diagnostic/regression name; add the two-axis BM25+semantic control, the per-topic aggregate guards, the edge-source census, and the cached-embedding hash. A passing run of the current test is not G1 evidence.
- `README.md` line 84 says "per-tenant audit events (FR67)"; the PRD glossary bans "audit" for access telemetry. Reword downstream.
- `README.md` line 7 quotes NFR31 as "approximate"; NFR31 now defines the clean machine and the recorded-run requirement. `docs/dev/quickstart-walkthrough-log.md` has no run.
- Licence: `LICENSE`, source headers, and `PackageLicenseExpression` are MIT; the PRD decision on record is Apache 2.0. Open Question 4 owns the resolution; do not publish a README licence pledge until it closes.
- `HybridSearchService` skips the graph axis without a start node; the PRD now requires auto-seeding from top-5 syntactic + top-5 semantic (FR17). No owning story exists yet.
- MCP parameter naming (`axis` vs `axes`) is declared once in the CLI surface table (`--axis`); the MCP tool schema should be checked against it when Epic 10 hardening is next touched.

## August 2026 SCP apply log

Applied into `prd.md` on 2026-09-05 from:

- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` PRD-1…PRD-7
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` PRD-1…PRD-6

Reconciliation of the NFR33 collision is recorded in the PRD Assumptions Index.
