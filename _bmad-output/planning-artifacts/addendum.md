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
- **V1 rank calculation.** Process providers in order, discard blank IDs and non-finite scores, and retain the first exact memory-unit ID. Use `Double.Equals` for finite ties and assign competition ranks (`1,1,3`). For rank `r`, the raw internal term is `1 / (10 + r)`. Hybrid output exposes the unweighted per-axis value `raw(r) / raw(1)`: rank one is `1/11` internally and `1.0` publicly, rank two is `1/12` internally and `11/12` publicly. Native single-axis scores retain their own scale.
- **V1 composite.** Sum the weighted raw terms for the unit and divide by `raw(1)` times the sum of the weights of axes that returned at least one result. Default syntactic/semantic/graph weights are `0.30/0.35/0.35`. NL has weight `0.20` when requested and configured for Phase 1.5 event units and is disabled by default. The same rank fixture applies across active presenters. [Architecture AD-9 and AD-22](architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [FusionEngine](../../src/Hexalith.Memories.Server/Search/FusionEngine.cs)
- **Tenant-wide graph merge:** partition seeds by ascending ordinal case ID, retain the maximum finite graph score per unit within its case, then merge by descending graph score, ascending ordinal case ID, and ascending ordinal memory-unit ID before rank allocation. Paths never cross cases.
- **Permission:** Changing weights during calibration does not require a PRD rewrite if NFR24/NFR25/NFR26 still hold and architecture versions the canonical contract.

## Benchmark — what the shipped suite actually is (2026-09-08 extract)

Source: `tests/Hexalith.Memories.Benchmarks` (`BenchmarkSuiteTests.ThesisValidation_HybridOutperforms80Percent`, `Data/ground-truth.json`, `Data/synthetic-corpus.json`), last recorded run 2026-07-16 (Epic 26.8).

| Aspect | Shipped suite | PRD thesis-gate protocol (G1) |
|---|---|---|
| N | 8 queries (BQ-01…BQ-08) | ≥ 50 topics |
| Query mix | Every query tagged `requiredAxes: [syntactic, semantic, graph]` | Representative mix; thesis-stress is a labelled slice |
| Corpus / labels | Synthetic corpus; `expectedResults` authored with the corpus | Real Phase 1 corpus. Jerome and two independent reviewers grade 0–3. All three unweighted pairwise κ values must reach 0.6 before median grades freeze. |
| Control | Best single active axis (`hybridNdcg > bestSingleNdcg`) | BM25+semantic two-axis RRF |
| Win rule | Any positive Δ | ΔNDCG@10 ≥ 0.02 |
| Graph edges in corpus | `references` 29, `causedBy` 9, `correlatedWith` 5 | Whatever the frozen real corpus contains under the Phase 1 population rule |
| Result | 8/8 wins, reproducible twice | Not yet run |

The eight-topic suite supplies NFR26 regression and reproducibility evidence. Stories 33.2 and 33.3 own case-local graph seeding and partitioning, 33.4 owns the two-axis control, and 33.6 owns the real corpus. G1 still requires independently reviewed labels and a qualifying scoring run.

## Gate evidence procedures

### G1 judgments

After freezing the topics, corpus, embeddings, retrieval configuration and the two arm versions, generate their top-ten candidate IDs without computing relevance scores against human judgments. For each topic pool the union of both top tens and any independently identified reference units registered before pooling. Deduplicate by unit ID. Present the pool to each reviewer in blinded order without arm or rank. Record one grade per topic, unit and reviewer. Use 0 for irrelevant, 1 for related context that cannot answer the topic, 2 for a directly relevant partial answer, and 3 for direct evidence sufficient to answer the topic. An abstention leaves the pool incomplete.

Compute unweighted Cohen’s κ for each of the three reviewer pairs over the same complete topic–unit rows. An undefined value or any pair below 0.6 blocks qualification. Resolve abstentions or relabel under a recorded new version and recalculate all three values before the first scoring run. Preserve the individual judgments. The median grade determines the gain `2^grade − 1`. For each ranking, sum gain divided by `log2(rank + 1)` over ranks 1–10. Divide by the gain of the ideal top ten from that topic’s final grades, using the same discounts. An empty rank contributes zero. If ideal gain is zero, both arms receive zero and the topic stays in N. Every returned top-ten unit must have three frozen grades. A new candidate outside the frozen pool requires a new registered pool and freeze before qualification. Pin and hash the manifest, judgments and graded scorer. The existing binary-relevance test remains diagnostic.

### L1 holdout

1. **Freeze the holdout.** The corpus steward registers the corpus snapshot and all N ≥ 10 topic IDs, prompts and budgets. Hold these topics out of G1 and all agent, prompt and retrieval tuning. For each topic, preregister a judging pool of candidate units that includes plausible relevant units and distractors. Use the G1 scale, three independent raw judgments per item, pairwise agreement check and median aggregation. Freeze the scored topic–unit file before running the reference agent. Every registered topic needs at least one reference unit graded 2 or 3. Freeze the reference agent, model, settings, tool version, budget tokenizer and scoring procedure with the manifest. Keep the labels unavailable to the agent and tuning team.
2. **Run the agent.** Run each registered topic once. Require a completed interaction with at least one budgeted MCP tool response per topic. Measure every such payload with the frozen tokenizer.
3. **Score and archive.** A topic earns one citation success if the final answer cites at least one unit that appears in both that topic’s archived MCP responses and its frozen grade-2-or-3 set. An unlisted, weak, or fabricated citation earns no credit. Report budget violations, successful topics and N. Passing requires zero budget violations and at least `ceil(0.8 × N)` successful topics. For N=12, ten successes are required. Archive the raw responses, citations and counts. The end-to-end MCP golden vectors use the same NFR25 score, ordering and Evidence Packet expectations as the Phase 1 contract.

### L2 starting state

Pin one source revision containing the EventStore starter service and the sample AppHost that runs that exact service. Record the OS, pinned SDK, Docker version, starter and host paths, package source and package version. Stage the source files and obtain the provider key off-clock. Verify empty NuGet and container-image caches, absent local tool installations and generated assets, no running sample services, and the absence of the Memories package reference, subscription and configured provider secret. At time zero begin the package-add command in the starter service. Count tool installation, restores, pulls, wiring, secret entry and boot. Capture the ID of a new committed event from that service and the first search response attributing a unit to it. Record elapsed wall time, the committed EventStore event ID and the ID of the indexed memory unit returned by search. A prewarmed or already-wired demo is diagnostic.

### L3 event denominator

In at least 20 labelled chains, freeze each eligible event’s tenant, case, event ID and one or more nonempty expected paths starting at that event. Each path lists the ordered event IDs and the required type and direction of each edge. An event with no known nonempty path can be registered as an endpoint but cannot enter the scored denominator. Register eligibility before execution. If chains overlap, use one row per distinct in-scope event and include all its labelled paths. An event fails if even one labelled path is missing, incomplete, in the wrong case or unavailable. A gap marker or timeout also counts as a failure. Report the number of successful event rows divided by the full frozen row count. A whole-chain count may be reported separately as a diagnostic. [Product brief success metric](product-brief-Hexalith.Memories-2026-03-22.md#success-metrics)

The Epic 9 fixture sources are `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs` and `tests/Hexalith.Memories.IntegrationTests/Graph/`. The labelled extension remains a Phase 1.5 prerequisite.

### NFR13 load window

Use identical hardware, dataset and query sequence for tenant A’s solo and mixed runs. Keep ten clients active and use the same fixed rotation across syntactic, semantic and hybrid calls. Measure each query type’s p95 separately. During the ten-minute mixed window, offer tenant B 120 small ingests per minute and keep tenant C supplied from an initial backlog of 300 distinct repair units. Count distinct successful completions inside the window, including zero-progress intervals. B must complete more than 1,000 ingests and C at least 250 repairs. Record offered and completed counts, per-type solo and mixed p95 values, the three ratios, maximum eligible-queue admission delay and cap-plus-one outcomes. Use the existing >100/min small-ingest NFR5 threshold and the NFR13 ratio of ≤1.10 for each query type.

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
- **Projection completion contract.** The target coordinator accepts monotonic per-axis checkpoints for `(tenantId, caseId, memoryUnitId, authoritative EventStore sourceVersion, schemaGeneration, embeddingConfigurationEpoch)`. The coordinator also binds target writes and all three confirmations to the current repair attempt under AD-3. Story 34.33 supplies the V1 register and build guard before 34.1 can add a URL retry token. Stories 34.1, 34.3 and 34.4 own durable acceptance, write fencing and completion respectively. The audit found that a missing graph acknowledgement could still yield `indexed`. Status, case activity and counts must agree on incomplete work until repair obtains all three current acknowledgements.
- **Idempotency:** V1 tokens are scoped by tenant, case, and command/operation. CloudEvent identity is tenant + case + exact validated `source` + `id`, compared ordinally with no post-validation normalization. Durable EventStore/workflow suppression owns the guarantee; projections are idempotent upserts for the completion tuple. Redis preflight reservation is fail-open admission optimization only and never durable truth.
- **Failure visibility.** Native ingestion status and case failed-unit routes exist. NFR19 still requires the enrolled CLI to display the scoped unit, stage and safe error after an intentional failure at each stage. DW-732 verified signed-URL display redaction, exact fetch and scoped deduplication on November 20. AD-4 durable-history protection remains outstanding. [DW-732 evidence](../implementation-artifacts/deferred-work.md#dw-732-keep-ingestion-source-credentials-out-of-logs-and-case-activity)
- **Recovery.** Authoritative EventStore replay, export restore and Redis-input consistency repair are separate operations. Replay to all current projections is still outstanding. Retry a restore only after a confirmed pre-write refusal, on a clean target with the same live tenant ID and matching configuration. If writes may have occurred or verification fails, keep the tenant offline and preserve its state for the recovery owner. DW-733 requires a live-tenant drill with final counts, search on each axis and an unaffected control tenant. [Restore stop conditions](../../docs/operations/backup-restore.md#rollback-and-stop-conditions)
- **Consistency repair.** The compatibility command schedules mutating Redis-input repair after confirmation or `--yes`. Story 34.30 requires an authoritative revision-bound preview, tenant-fenced apply and deletion provenance for FR74. [Repair story](epics.md#story-3430-repair-only-from-authoritative-revisions)

## Identity

- Native REST and compatibility CLI/MCP preserve validated bearer authority. External `ingested_by` uses the authenticated issuer and subject, normalized together once at ingress and compared ordinally across surfaces. Trusted internal actors use an allowlist-derived `system:*` identity, an explicit grant for the tenant and authoritative `Active` state. The shared McpCli Gateway carries workload authority only. Before enrollment, an operation needing a user must demonstrate authenticated tenant and user authorization, including denial, over the selected transport. Case membership records attribution. [AD-5 and AD-13](architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md#invariants--rules)
- Epic 20 JWT on `/api/**` with anonymous health/Dapr exceptions is the current MVP ingress reality (NFR11).

## Topology and secrets (not extra product surfaces)

- .NET Aspire AppHost is the current orchestration path (`dotnet run --project src/Hexalith.Memories.AppHost`). Not a second product.
- Runtime secrets: DAPR Secrets API backed by OpenBao (NFR9). Deployments provide only minimum documented OpenBao bootstrap material outside that path. Direct Kubernetes injection of Redis/FalkorDB credentials is a temporary alignment gap, not a second approved application-secret path.
- Optional Python `ai-agent` sidecar is architecture-owned and deferred until a selected feature requires it (Open Question 7 closed 2026-09-12).
- Access telemetry may use PostgreSQL (Epic 27); that is not a search-backend pivot. Search remains Redis + FalkorDB until NFR15/Qdrant work ships.

## Tenant erasure

- **PRD outcome:** FR39/NFR16 — verified projection purge, irreversible EventStore content inaccessibility, telemetry erasure handoff, no replay/restore resurrection, and no tenant-ID reuse.
- **Erasure evidence.** Deletion fences the tenant ID against reuse. The architecture calls for tenant-key crypto-shredding and content-free tombstones. Stories 34.24 and 34.25 still need to demonstrate that an erased origin or a pre-erasure snapshot cannot rehydrate content.
- **Portable export.** Story 8.3 delivered streamed case and tenant application JSON early. AD-16/AD-21 still require authenticated origin and payload integrity, controlled release, cleanup of source-held copies and refusal of a same-population import from a tombstoned origin. Story 34.25 owns erased-origin refusal, Story 34.26 owns bundle issuance and staging purge, and Story 34.57 owns lifecycle-bound release. Epic 26 owns operational backup and recovery.
- **Telemetry boundary:** tenant deletion records the durable mapping/handoff but does not force early deletion of retained opaque telemetry. NFR34 owns TTL, purge progress, and any separately adopted accelerated purge operation.

## Capacity and backpressure

- The NFR12 reference workload is 100K units per tenant. The current ingestion ledger caps retained keys at 65,536, and key count varies by workload. DW-735 must measure keys per unit, admission and deletion at load before publishing a supported capacity. Admission and concurrency are tenant-partitioned, with separate tenant and global quotas.
- Provider `Retry-After` is honored through workflow timers, not unbounded in-memory retries.
- Repair, recovery, and migration work run below interactive query/ingestion priority. PRD NFR12/NFR13/NFR22/NFR36 own measurable fairness and freshness outcomes.

## Migration phasing

- AD-14 permits an acknowledged, degraded tenant-scoped rebuild for MVP FR43. The native embedding-config endpoint warns on incompatible changes and accepts `forceReindex=true`. The actor sets `ReindexRequired`. Story 34.31 must demonstrate authoritative catch-up, verified parity, atomic activation and retirement for that tenant. Phase 2 owns non-disruptive embedding/schema migration and Phase 3 owns backend replacement. [Phase 1 rebuild story](epics.md#story-3431-activate-a-phase-1-embedding-rebuild-epoch)
- AD-14's ratification gives no G6 credit. The evidence-only AD-20 rule applies to each MVP-active requirement and architecture-critical active-foundation gap; phase-inactive work remains outside MVP scope.

## Package inventory

- The [release manifest](../../tools/release-packages.json) defines both published package IDs and non-packable projects. The published IDs include `Hexalith.Memories.EventStoreExtensions` for enrollment of local internal intent adapters.
- The PRD lists the non-packable `Hexalith.Memories.DomainService` SDK command/replay host and `Hexalith.Memories.Web` Razor component library. `tests/Hexalith.Memories.Web.SpecimenHost` runs the conformance specimen. [Route inventory](../../docs/operations/route-surface.md)
- Compatibility-only `Hexalith.Memories.Redis` facade: composition-root registration; Server must not take it as a transitive domain dependency.

## Language / SDK

- The repository targets .NET 11 and C# 14 in `Directory.Build.props`.
- `global.json` selects the SDK.

## Brief residue (2026-03-22) not absorbed as FRs

Qualitative ideas to re-home in UX or docs, not silent FRs:

- Brand line "Connected knowledge that understands why"
- README-as-product / 30-second demo craft
- Interactive exploration as a trust-building UX idea. The compatibility `memories explore` entry is a stub. Search, inspect and lookup are working manual steps.
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
| Bring non-disruptive migration or backend replacement into MVP | AD-14 allocates non-disruptive migration to Phase 2 and backend replacement to Phase 3. MVP FR43 permits degraded service during an acknowledged tenant rebuild. |
| Remove the compatibility `--tenant` option | `memories search query` requires the requested tenant ID. The authenticated authority controls access. McpCli enrollment owns the target grammar. |
| Move NFR9 (OpenBao) / package tables / Aspire out of the PRD | August 2026 SCP amendments; they stay, with mechanism detail here |

## Downstream drift not fixed by this Update (handoff)

The architecture spine binds FR1–FR75, NFR1–NFR37, G1–G6 and L1–L3. It ratifies AD-14 phasing, the FR75/NFR37/G6 binding and AD-23 Identifier Grammar V1. Epic 34 has a reviewed execution order. Final validation and successor sprint tracking remain on hold for the outstanding target-surface and qualification work. Epic 27 closed through the separate November 1 telemetry decision. [Implementation readiness](implementation-readiness.md)

**November 20 epic handoff.** Use the current PRD gates when updating the epic summaries, in particular the independent L1 holdout and the Phase 1 G5 surfaces. The Administrator owns the three open corrections below. [Validation findings and story references](prd-validation-2026-11-20/validation-report.md#downstream-story-corrections)

- **Stories 34.32 and 34.38.** Tenant lifecycle ends ordinary access and verifies removal of the principal-token mapping. Platform Operations receives the durable handoff, disposes of the retained partition and its locator after verified expiry, and writes the completion receipt to the separate `retention-purge` stream under AD-17 and AD-21.
- **Stories 34.23 and 34.24.** AD-16 requires positive fenced evidence for empty or never-provisioned targets, including never-issued keys and never-created backups. Missing or unreadable evidence leaves deletion pending. Exercise restart after key destruction and verify exactly one terminal tombstone.
- **Story 34.40.** The telemetry and clock routes serve authorized internal callers. Cover wire registration, V1 error mapping and the non-packable project inventory under AD-12 and AD-19. External exposure requires a subsequent architecture decision. Story 34.33 owns the separate V1 wire-name register guard.

Earlier handoff items:

- `tests/Hexalith.Memories.Benchmarks`: rename `ThesisValidation_HybridOutperforms80Percent` (and the `ThesisValidated` flag) to a diagnostic/regression name; add the two-axis BM25+semantic control, the per-topic aggregate guards, the edge-source census, and the cached-embedding hash. A passing run of the current test is not G1 evidence.
- The README calls FR67 “per-tenant audit events.” Update the CLI preview to say “access telemetry,” the term defined in the PRD. [README CLI preview](../../README.md#cli-preview)
- The README describes the warm-stack compatibility wizard. L06 was withdrawn from McpCli migration. The current `SearchQueryCommand` hints still name the `ingest` stub. FR56/FR57 require an available recovery such as native case creation, file/URL ingestion or case status. Story 35.7 owns the clean-machine run through enrolled operations. [Walkthrough log](../../docs/dev/quickstart-walkthrough-log.md) · [Compatibility hints](../../src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs) · [Native routes](../../src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs)
- Licence: confirmed MIT by Jerome on 2026-09-08 (Open Question 4 closed); the repository already complies. Remaining downstream action: add the README sentence "Hexalith.Memories is committed to the MIT license. We will not change to a restrictive license." Any older Apache 2.0 wording in `architecture.md`/`epics.md` is drift to remove.
- `HybridSearchService` still needs case-local seeding on default hybrid search. Stories 33.2 and 33.3 own this work under FR17.
- The compatibility CLI uses `--axis`, and compatibility MCP `search_memory` uses `axes`. The approved target MCP search row M01 is deferred. Check the enrolled operation’s published schema when delivery resumes.

## August 2026 SCP apply log

Applied into `prd.md` on 2026-09-05 from:

- `sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md` PRD-1…PRD-7
- `sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md` PRD-1…PRD-6

Reconciliation of the NFR33 collision is recorded in the PRD Assumptions Index.
