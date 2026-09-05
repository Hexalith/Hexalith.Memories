# Addendum — Hexalith.Memories PRD

**Role:** Mechanism, topology, rejected alternatives, and change-control residue that must not live in the product-outcome PRD.  
**Updated:** 2026-09-05  
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

## Rejected alternatives (this Update)

| Alternative | Why rejected |
|---|---|
| Keep a single "MVP go/no-go" mixing MCP, EventStore product integration, and CLI thesis | Two incompatible ship contracts (validation critical) |
| Pull MCP into thesis MVP if Phase 1.5 slips | Scope accordion; delays those surfaces instead |
| Defer cases/tenant isolation under resource pressure | Contradicts non-retrofittable foundation and NFR8 |
| Atomic triple-write across Redis backends | Architecture EventStore-commit + projections |
| C# 13 / SDK number in the PRD | Repository `global.json` + C# 14 |
| One NFR33 id for both web performance and freshness | Split: NFR33 freshness, NFR35 web performance |

## August 2026 SCP apply log

Applied into `prd.md` on 2026-09-05 from:

- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` PRD-1…PRD-7
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` PRD-1…PRD-6

Reconciliation of the NFR33 collision is recorded in the PRD Assumptions Index.
