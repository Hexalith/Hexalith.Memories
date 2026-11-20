# Addendum — Hexalith.Memories PRD

**Role:** Mechanism, topology, rejected alternatives, and change-control residue that must not live in the product-outcome PRD.
**Updated 2026-11-20.**
**Does not override:** `prd.md` ship gates, FR/NFR IDs, or Glossary terms.

## Why this file exists

The 2026-09-05 PRD Update moved implementation how-to out of the requirements contract: Aspire topology, OpenBao, package graphs, fusion numeric weights, Dapr Agents, and SDK pins. Architecture remains authoritative for those facts when they drift.

**Current handoff, 2026-11-20.** The missed October 31 checkpoint leaves Phase 1 at no-go. The Administrator stewards the corpus and must secure two independent reviewers before label freeze. Story 35.7 needs enrolled McpCli operations and a cold run. AD-14 and the FR75/NFR37/G6 architecture binding are ratified. Each active gap still needs current, re-runnable evidence under AD-20. [Release decision record](prd.md#release-decision-record)

## EventStore — three contracts (do not collapse)

1. **Domain truth.** FR13 requires an EventStore commit before projection. Cases, tenants and memory-unit annotations and deletions already use it. Stories 34.1 and 34.2 own the outstanding intake commit for V1 ingestion and CloudEvents, respectively.
2. **Product integration (Phase 1.5):** CloudEvent auto-index, dual embedding, CausationId/CorrelationId edges (FR59–FR62, Epic 9).
3. **Runtime dependency (Epic 28).** DW-734 closed after the 5.4.0 EventStore package floor preserved higher selections and all 31 configuration tests passed. L2 still requires a full-stack EventStore-originated publish (DW-713). [Deferred-work register](../implementation-artifacts/deferred-work.md)

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

The eight-topic suite supplies NFR26 regression and reproducibility evidence. Stories 33.2 and 33.3 own case-local graph seeding and partitioning, 33.4 owns the two-axis control, and 33.6 owns the real corpus. G1 still requires independently reviewed labels and a qualifying scoring run.

## Release decision — why dated no-go rather than "gate must pass"

The October 5 decision set October 31 as the prerequisite checkpoint, December 1 as the G1–G6 decision and January 1 as the derived Phase 1.5 date. The checkpoint was missed. The Administrator can propose one reset when reviewer availability, corpus preparation, the target CLI delivery plan and engineering capacity support it. The PRD distinguishes a missing verdict from a measured G1 failure, which triggers the fusion kill switch. [Approved November 20 disposition](sprint-change-proposal-2026-11-20.md)

## Isolation mechanism

- **PRD outcome:** NFR8, FR38, FR40 — zero leaks, tenant-scoped resources, automated verify.
- **Backend authority.** The design requires per-tenant Redis principals. The audit found that search selected a tenant index over a shared Redis connection. Stories 34.11 and 35.6 must show that each tenant can read and write its own data, that each is denied access to the other’s data, and that the deployed backend rejects revoked credentials. Story 34.53 covers FalkorDB separately.
- **MVP isolation tier:** shared cluster, tenant-scoped principals and indexes, API enforcement (not separate processes/volumes per tenant unless a later approved change says so).

## Ingestion runtime

- **PRD:** DAPR Workflow owns stages, retry, compensation. Rate-limiter actor owns embedding budget.
- **Rejected:** per-tenant document-queue pipeline actor as orchestrator (pre-D23).
- **NFR17 proof:** workflow history / Durable Task persistence, not "pipeline actor state."
- **Projection completion contract.** The target coordinator accepts monotonic per-axis checkpoints for `(tenantId, caseId, memoryUnitId, authoritative EventStore sourceVersion, schemaGeneration, embeddingConfigurationEpoch)`. Stories 34.1, 34.3 and 34.4 cover the revision, write fencing and completion. The audit found that a missing graph acknowledgement could still yield `indexed`. Status, case activity and counts must agree on incomplete work until repair obtains all three current acknowledgements.
- **Idempotency:** V1 tokens are scoped by tenant, case, and command/operation. CloudEvent identity is tenant + case + exact validated `source` + `id`, compared ordinally with no post-validation normalization. Durable EventStore/workflow suppression owns the guarantee; projections are idempotent upserts for the completion tuple. Redis preflight reservation is fail-open admission optimization only and never durable truth.
- **Recovery.** Authoritative EventStore replay, export restore and Redis-input consistency repair are separate operations. Replay to all current projections is still outstanding. Retry a restore only after a confirmed pre-write refusal, on a clean target with the same live tenant ID and matching configuration. If writes may have occurred or verification fails, keep the tenant offline and preserve its state for the recovery owner. DW-733 requires a live-tenant drill with final counts, search on each axis and an unaffected control tenant. [Restore stop conditions](../../docs/operations/backup-restore.md#rollback-and-stop-conditions)

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
- **Erasure evidence.** Deletion fences the tenant ID against reuse. The architecture calls for tenant-key crypto-shredding and content-free tombstones. Stories 34.24 and 34.25 still need to demonstrate that an erased origin or a pre-erasure snapshot cannot rehydrate content.
- **Telemetry boundary:** tenant deletion records the durable mapping/handoff but does not force early deletion of retained opaque telemetry. NFR34 owns TTL, purge progress, and any separately adopted accelerated purge operation.

## Capacity and backpressure

- The NFR12 reference workload is 100K units per tenant. The current ingestion ledger caps retained keys at 65,536, and key count varies by workload. DW-735 must measure keys per unit, admission and deletion at load before publishing a supported capacity. Admission and concurrency are tenant-partitioned, with separate tenant and global quotas.
- Provider `Retry-After` is honored through workflow timers, not unbounded in-memory retries.
- Repair, recovery, and migration work run below interactive query/ingestion priority. PRD NFR12/NFR13/NFR22/NFR36 own measurable fairness and freshness outcomes.

## Migration phasing

- The draft architecture spine now ratifies AD-14 by phase: MVP FR43 permits an acknowledged degraded tenant-scoped rebuild; Phase 2 owns non-disruptive embedding/schema migration; Phase 3 owns backend replacement. The decision is settled, while implementation and evidence gaps remain in its alignment ledger.
- AD-14's ratification gives no G6 credit. The evidence-only AD-20 rule applies to each MVP-active requirement and architecture-critical active-foundation gap; phase-inactive work remains outside MVP scope.

## Package inventory

- Sole published-ID source: `tools/release-packages.json`.
- Non-packable hosts are listed in the PRD as a separate table. Do not invent a "9+3" slogan that disagrees with those two lists.
- Compatibility-only `Hexalith.Memories.Redis` facade: composition-root registration; Server must not take it as a transitive domain dependency.

## Language / SDK

- The repository targets .NET 11 and C# 14 in `Directory.Build.props`.
- `global.json` selects the SDK.

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

The final architecture spine ratifies AD-14 phasing and the FR75/NFR37/G6 binding. Epic 34 has a reviewed execution order. Final validation and successor sprint tracking remain on hold for the outstanding target-surface and qualification work. Epic 27 closed through the separate November 1 telemetry decision. [Implementation readiness](implementation-readiness.md)

Further handoff items recorded on 2026-09-08 (afternoon adversarial pass), none applied to code or downstream documents by this Update:

- `tests/Hexalith.Memories.Benchmarks`: rename `ThesisValidation_HybridOutperforms80Percent` (and the `ThesisValidated` flag) to a diagnostic/regression name; add the two-axis BM25+semantic control, the per-topic aggregate guards, the edge-source census, and the cached-embedding hash. A passing run of the current test is not G1 evidence.
- `README.md` line 84 says "per-tenant audit events (FR67)"; the PRD glossary bans "audit" for access telemetry. Reword downstream.
- The README labels the warm-stack quickstart as a compatibility walkthrough. Story 35.7 owns cold timing on McpCli. [Walkthrough log](../../docs/dev/quickstart-walkthrough-log.md)
- Licence: confirmed MIT by Jerome on 2026-09-08 (Open Question 4 closed); the repository already complies. Remaining downstream action: add the README sentence "Hexalith.Memories is committed to the MIT license. We will not change to a restrictive license." Any older Apache 2.0 wording in `architecture.md`/`epics.md` is drift to remove.
- `HybridSearchService` still needs case-local seeding on default hybrid search. Stories 33.2 and 33.3 own this work under FR17.
- MCP parameter naming (`axis` vs `axes`) is declared once in the CLI surface table (`--axis`); the MCP tool schema should be checked against it when Epic 10 hardening is next touched.
- The final architecture spine binds `FR1-FR75`, `NFR1-NFR37`, `G1-G6`, and `L1-L3`; its AD-14/FR75/NFR37/G6 decisions and AD-23 Identifier Grammar V1 are ratified. Implementations and qualifying evidence remain owed.

## August 2026 SCP apply log

Applied into `prd.md` on 2026-09-05 from:

- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` PRD-1…PRD-7
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` PRD-1…PRD-6

Reconciliation of the NFR33 collision is recorded in the PRD Assumptions Index.
