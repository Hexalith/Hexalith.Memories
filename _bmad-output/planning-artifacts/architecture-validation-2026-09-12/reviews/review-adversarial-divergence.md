# Adversarial Divergence Review — Validation Pass 2026-09-12

**Verdict: PASS-WITH-FINDINGS — 4 critical, 6 high, 1 low divergence pairs; no finding reopens an adopted decision, every one is a missing or under-tightened Rule clause.**

**Artifact (read-only):** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (324 lines, `status: final`, updated 2026-09-09)
**Lens:** two implementation units one level down that each obey every AD to the letter and still build incompatibly
**Prior gate:** `architecture-memories-2026-09-09/reviews/review-adversarial-divergence.md` reached PASS at its fourth pass. This review does not restate it. Its closed findings (status vocabulary, internal Dapr auth, degrade-vs-fail, Phase 1 graph population, RRF rank origin/dedup/tie equivalence/tenant-wide merge order/tie-break direction, adapter assembly ownership, Evidence Packet field removal, workflow secret capture, Web shape, MCP production activation, G1 gap accounting, tenant normalization, backend isolation, CLI precedence, EventStore lane identity, telemetry fail-open timing, health/readiness, `suspension`, licensing, `/events/ingest`, package count) were re-tested and remain closed.

## Method and discipline

Each finding names two units that could plausibly be built by different epics or teams, quotes the exact Rule text and line number that each unit relies on, shows that **neither unit violates any AD**, then shows the concrete incompatible artifact — a clashing serialized shape, two writers of one key, two conflicting orderings, or an unowned seam. Where a candidate turned out to require one unit to break a Rule, it was discarded as an implementation bug rather than a spine hole; three such candidates were dropped (an axis adapter constructing its own Redis connection outside `Adapters.Redis`, a surface omitting freshness from the Evidence Packet, and a consumer package referencing `Server` — all three are AD violations, not divergences).

The prior gate's four passes hardened AD-9's *ranking function*. What this pass attacks instead is the layer around it: what feeds the ranking function, what the six-field tuple is ordered by, and who owns four seams the spine names without assigning.

---

## ADV-01 — critical — The AD-3 projection tuple has no ordering relation and no designated reported member

**Unit A — Ingestion projection coordinator** (the epic that implements AD-3's Dapr-state checkpoint for ordinary file/URL/CloudEvent ingest).
**Unit B — Provider/schema migration backfill unit** (the epic that implements AD-14's create-backfill-verify-switch-retire reindex).

### Compliance proof

Both units are governed by the same sentence, AD-3 line 72:

> "Identify projection work by `(tenantId, caseId, memoryUnitId, authoritative EventStore sourceVersion, schemaGeneration, embeddingConfigurationEpoch)`; one Dapr-state projection coordinator advances per-axis checkpoints monotonically with ETag/CAS and rejects stale acknowledgements. Mark `Indexed` only after syntactic, vector, and graph projections acknowledge that tuple; otherwise retain `Indexing`/product-semantic `projecting` or enter actionable `Failed`. Repair and replay use the same protocol."

Unit A implements one checkpoint **record per `(tenantId, caseId, memoryUnitId, schemaGeneration, embeddingConfigurationEpoch)`**, each advancing monotonically in `sourceVersion`. It complies: there is one coordinator, advancement is monotonic in the only field the spine states is versioned by EventStore, CAS is used, and acknowledgements carrying a lower `sourceVersion` for the same generation/epoch are rejected as stale.

Unit B implements one checkpoint **record per `(tenantId, caseId, memoryUnitId)`** whose stored value is the whole tuple, ordered lexicographically with `embeddingConfigurationEpoch` most significant, then `schemaGeneration`, then `sourceVersion`. It also complies, and it can point at AD-14 line 138 for why epoch must dominate:

> "Use versioned create-backfill-verify-switch-retire migrations with disjoint active and staging resources, reindex the full tenant corpus before atomic activation, and keep embedding and LLM implementations behind strategy ports; **projection evidence carries the active configuration epoch** and activities resolve secret references at execution time so rotation can retry safely."

AD-4 line 78 binds both identically — "make every projection an idempotent upsert for AD-3's tuple" — and says nothing about ordering. The spine never states a comparison relation over the six fields, and it never says how many checkpoint records may exist for one memory unit at one instant. (PRD FR75 does disambiguate this — "a legitimate reprojection under a new epoch is not suppressed" — but the spine frontmatter binds only `FR1-FR74`, so FR75 is not binding architecture; PRD Open Question 10 records exactly this.)

### The concrete incompatibility

**(a) Two conflicting staleness verdicts on the same acknowledgement.** During a backfill, staging is being written at epoch 2 while the tenant still serves epoch 1. A user ingests a new revision of unit `U`, producing `(…, sourceVersion 7, gen 3, epoch 1)`. The syntactic activity acknowledges it *after* the backfill acknowledged `(…, sourceVersion 6, gen 3, epoch 2)`.

- Unit A: two independent records; the epoch-1 record advances 6 → 7 and `U` completes on the active surface.
- Unit B: one record already at `(epoch 2, …)`; the epoch-1 acknowledgement is lexicographically lower, so it is **rejected as stale**. `U` is written into the active Redis index by the activity but its checkpoint never completes, so `U` remains `Indexing` on the active surface for the entire backfill window and no retry can ever clear it — every retry produces the same "stale" verdict. This is a permanent stall produced by full compliance.

**(b) Two conflicting public statuses for the same tenant.** AD-3 line 72 forbids marking `Indexed` without acknowledgements for "that tuple", but never says *which* tuple's completion the reported ingestion state reflects when more than one is live.

- Unit A reports the active-epoch tuple: during a migration the tenant stays `indexed`, and a reindex that silently fails for 40% of the corpus is invisible to `memories status` — AD-14's "verify" step has no state to read.
- Unit B reports the highest tuple: the instant a backfill starts, **every unit in the tenant flips from `indexed` to `indexing`**, blowing NFR36's `pending` → `indexed` budget tenant-wide, corrupting FR10 per-state counts and FR31 case status, and making "migration running" indistinguishable from "ingestion broken".

Both readings satisfy the V1 status wire contract at line 177 and neither promotes an incomplete revision, so AD-10 line 114's guard does not discriminate between them.

**(c) No retirement owner.** AD-14's "retire" step is silent on checkpoint records. Unit A accumulates one record per unit per historical epoch forever; Unit B keeps one. Neither is wrong.

### Proposed tightening — AD-3 Rule, replacement sentence

> "Order projection work by the strict tuple comparison `(embeddingConfigurationEpoch, schemaGeneration)` as the identity discriminator and `authoritative EventStore sourceVersion` as the only monotonic dimension: one checkpoint record exists per `(tenantId, caseId, memoryUnitId, schemaGeneration, embeddingConfigurationEpoch)`, an acknowledgement is stale only when its `sourceVersion` is lower than the recorded value for its own generation and epoch, an acknowledgement for a different generation or epoch is never stale, the reported public ingestion state is always that of the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)` while the staging tuple's completion is reported only as migration-backfill progress, and AD-14's retire step deletes the retired generation's and epoch's checkpoint records as part of its verified completion."

---

## ADV-02 — critical — The tenant-erasure mapping has two claimed owners, no store, and no stated blocking semantics

**Unit A — Tenant erasure workflow** (the AD-6/AD-16 lifecycle epic in `Hexalith.Memories.Server`).
**Unit B — Access-telemetry service** (`Hexalith.Memories.AccessTelemetry*` — the spine's Structural Seed line 226 calls it "Independent contracts, service, and clock authority").

### Compliance proof

AD-16 line 150 makes the mapping a *precondition of completion* but uses the passive voice and names no owner:

> "Tenant deletion completes after product projections are purged, **a durable access-telemetry erasure mapping/handoff is recorded**, and the Hexalith.EventStore tenant-key crypto-shredding workflow irreversibly invalidates or deletes content access with verification."

AD-17 line 156 assigns ownership of the same object to the other side:

> "Platform Operations owns the configured TTL, observable purge progress, **tenant-erasure mapping**, bounded recovery, and dated accepted debt."

Unit A additionally has AD-8 line 194 on its side, which names this exact class of object and puts it in Dapr state:

> "Registries, leases, fences, **mappings**, and migration coordination use Dapr state unless a later `AD-n` adds a row with its primitive, failure posture, tenant/key scope, and test evidence."

Unit B additionally has AD-17's whole premise on its side — telemetry must not depend on product truth and must not become it — plus the fact that AD-17 names Platform Operations, the telemetry owner, as the owner of the mapping. Neither unit violates anything. Operational Boundaries line 270 makes it worse rather than better: "AD-16 joins projection purge, access-telemetry mapping, EventStore crypto-shredding, backup admission, non-reuse, and completion evidence into **one outcome**" — one outcome assembled from two independently owned stores, with no reconciliation rule.

### The concrete incompatibility

**(a) Two records, two stores, one required outcome.** Unit A writes the mapping to Dapr state (citing line 194) and treats its own record as the AD-16 completion evidence, sending the handoff as a notification. Unit B maintains the authoritative mapping in the telemetry service's own store (PostgreSQL per `addendum.md` › Topology). After an erasure there are two mappings, they can disagree, and no AD says which one FR39's operator-facing "verified erasure" evidence is read from.

**(b) Opposite blocking semantics, both compliant.** AD-16 makes recording the mapping a precondition of *completion*, so Unit A must block tenant deletion on it. If Unit B owns the record, that block becomes a synchronous product→telemetry dependency — which AD-17 line 156 constrains only for a different class of write: "once admitted, **sanitized product writes** remain non-blocking within the approved delivery bound". An erasure handoff is not a sanitized product write, so blocking on it is compliant; and returning `202` and landing it later is also compliant, because AD-16 does not say the handoff must be acknowledged. Two units therefore build, respectively, a tenant deletion that hangs when the telemetry service is degraded, and a tenant deletion that reports "erasure complete" for a mapping that was never received. Both satisfy AD-16 line 150 as written.

**(c) The non-reuse tombstone can inherit a TTL.** AD-16 line 150 continues: "Retain only content-free deletion evidence/tombstones, **reject replay and reuse of the tenant ID**". If the tombstone is realised as part of Unit B's mapping record, it sits inside the store whose defining property (AD-17 line 156) is a "configured TTL, observable purge progress". After the TTL elapses, the erased tenant ID is reusable — an irreversible-data-protection outcome silently undone by a retention policy, with neither unit having broken a rule. AD-16 explicitly exempts *retained opaque telemetry* from erasure timing but never exempts the *tombstone* from telemetry retention.

### Proposed tightening — AD-16 Rule, replacement sentence

> "The tenant erasure workflow is the sole owner and writer of the durable erasure record in Dapr state under AD-8 line 194; that record — not any telemetry-side copy — is the authoritative completion evidence, carries the content-free non-reuse tombstone with no TTL and no purge path, and deletion completes only when the workflow has durably recorded an acknowledged handoff identifier returned by the access-telemetry service, where an unavailable telemetry service leaves the deletion in a resumable incomplete state and never in a completed one; the access-telemetry service holds only a derived, TTL-eligible copy of the mapping for its own purge accounting and its copy can never satisfy, delay, or revoke AD-16 completion."

---

## ADV-03 — critical — The `system:*` allowlist and tenant grant have two possible writers with opposite failure directions

**Unit A — Tenant lifecycle workflow** (AD-6, the epic that provisions Redis ACL principals, Falkor graphs and indexes).
**Unit B — Internal authorization unit** (the epic that implements AD-5's trusted-internal path for the Dapr subscriber, MCP service, and repair service).

### Compliance proof

AD-5 line 84:

> "…trusted internal calls additionally require deny-by-default Dapr mTLS/access-control policy and app-token channel protection, then map authenticated app ID through **one operator-owned finite allowlist** to a canonical `system:*` principal and **explicit tenant grant**. Dapr channel authentication never replaces tenant authorization."

AD-6 line 88 Binds — "Tenant provisioning, verification, repair, deletion, indexes, graphs, **backend identities**, and cleanup" — and line 90:

> "Perform tenant resource changes only in explicit, idempotent, observable, bounded lifecycle workflows with verification, compensation, and resumable cleanup. … Other paths require an active verified tenant and never create infrastructure implicitly."

**Unit A complies.** A per-tenant `system:*` grant is the thing that makes a newly provisioned tenant reachable on the internal ingestion path; under AD-6 line 88 it is a tenant resource ("backend identities") and therefore *only* the lifecycle workflow may change it. The grant table is one document in Dapr state (AD-8 line 194 puts "mappings" there), finite at every instant, and mutated only by an operator-invoked workflow — so it remains "one operator-owned finite allowlist".

**Unit B complies.** AD-5 says *operator*-owned; AD-15 line 144 keeps "bootstrap, application, data-plane, and **operator** scopes … separate and least-privileged"; Consistency Conventions line 179 says "Options validate at startup". Unit B therefore ships the allowlist as a deployment artifact loaded and validated at startup, and reads "finite" as statically finite — a table a runtime workflow can append to is not finite by construction. Tenant creation does not touch it.

Neither unit violates an AD. The spine assigns ownership explicitly for tenant resources (AD-6), adapters (AD-8), telemetry (AD-17) and quotas (AD-18 line 162, "separate owners"), and does not assign it here.

### The concrete incompatibility

**Two writers of one authorization decision, failing in opposite directions at the two moments that matter most.**

- **At create:** Unit A grants the new tenant to the ingestion subscriber as part of provisioning, so internal CloudEvent ingest works the moment `tenant create` returns. Unit B's static artifact does not contain the new tenant, so the same call **fails closed** — correctly, by AD-5's own doctrine — and an operator must edit a file and redeploy. This is the L2 launch stopwatch path (publish a domain event → `search query` finds it, PRD Measurable Outcomes › Launch stopwatch step 5): whether that gate is passable at all depends on which unit owns the grant, and both answers are compliant.
- **At delete:** Unit A revokes the grant as AD-6 "resumable cleanup" and AD-16 completion work. Unit B's artifact still grants `system:*` on the erased tenant ID. AD-16 line 150 requires the system to "reject replay and reuse of the tenant ID", but that rejection lives on the domain-command/tombstone path while the grant lives on the channel-authorization path, and **no Rule conjoins them**. A retained CloudEvent replayed onto the deleted tenant therefore passes AD-5's authorization check under Unit B, and the spine's only defence is a tombstone check nobody was told to place behind that grant.
- **At neither:** AD-6 line 90's "Other paths require an active verified tenant" is written about *infrastructure creation*, not about authorization. Neither unit is required to validate a grant against tenant-active state, so a grant naming an unprovisioned or erased tenant is admissible under both.

### Proposed tightening — AD-5 Rule, replacement sentence

> "The operator-owned app-ID-to-`system:*` allowlist is a deployment-time artifact with exactly one writer — the operator, never a workflow — and it grants only principal identity, never tenant scope; the per-tenant grant that pairs with it is a tenant resource whose sole writer is AD-6's lifecycle workflow, is created only by verified tenant provisioning and revoked as a completion condition of AD-16 erasure, and every internal call must satisfy allowlisted app ID, an explicit grant for the requested tenant, **and** that tenant being currently active and verified, with any one of the three absent failing closed."

---

## ADV-04 — critical — The graph axis's seeds, limits, and truncation state are unbound, so a compliant graph axis can be silently incomplete and non-reproducible

**Unit A — Tenant-wide search fusion coordinator** (owns AD-9's canonicalization, seed assembly, merge and rank allocation).
**Unit B — FalkorDB graph traversal adapter** (`Adapters.FalkorDb`; owns AD-11's server-side limits and kill switch).

### Compliance proof

AD-9 line 108, the two clauses that meet here:

> "For tenant-wide graph search, **partition seeds by ascending ordinal case ID**, retain each unit's maximum finite graph score across seeds within its case, and merge by descending graph score, ascending ordinal case ID, then ascending ordinal `MemoryUnitId` before rank allocation. … Without an explicit graph start, traverse to depth at most two from the union of **the top five syntactic and top five semantic hits**."

AD-11 line 120:

> "Parameterize values, restrict labels to the contract enum, enforce tenant and case scope over every node and edge, and apply **server-owned depth/result/time limits** plus a graph kill switch."

AD-7 line 96 makes the case partition the isolation unit: "partitions graph seeding/traversal by each seed's authoritative case and never crosses case boundaries."

Both units comply throughout. The prior gate's fourth pass proved the *merge* is order-deterministic given a fixed candidate set. Nothing in the spine fixes the candidate set.

### The concrete incompatibility

**(a) The result limit's application point is unassigned, and it changes rank membership.** Unit B applies AD-11's result limit per traversal call, because a traversal call is the adapter's unit of work and AD-7 makes the case partition the traversal boundary. Unit A applies it to the merged list, because AD-9 produces "the graph axis" as one list and AD-18 line 162 budgets per tenant, not per case. For a tenant with six cases and a limit of 50, Unit B feeds up to 300 candidates into the merge and Unit A feeds 50. The merged list's *membership* differs, so competition ranks differ, so `1 / (10 + rank)` contributions differ, so composite scores and final ordering differ. NFR25 ("identical results across runs and surfaces") and gate G5 fail with no bug on either side.

**(b) There is no axis state for "truncated", so silent incompleteness is the compliant outcome.** Whoever owns AD-11's *time* limit, a fan-out across case partitions can expire with some cases traversed and some not. AD-9 line 108 offers exactly two states: "A null axis is unavailable and an empty axis is available with no hits". A partially-traversed axis is neither. Unit B therefore reports it as an ordinary available axis with hits, and the Evidence Packet says the graph axis is healthy while entire cases are silently missing — which is verbatim the outcome AD-10 line 113 exists to prevent ("a partial outage returning deceptively complete results"). Unit A nulls the whole axis on any partition timeout, losing all graph contribution and shifting the AD-9 denominator. And because *which* partitions complete depends on wall-clock, Unit B's answer is not reproducible, defeating NFR25's "100 repeated queries with zero score or ordering variance" and gate G4's requirement that tenant-wide fixtures behave.

**(c) "Top five hits" is ambiguous in exactly the sentence that has just defined ties.** AD-9 line 108 defines competition ranks `1,1,3` and then says "the top five syntactic and top five semantic hits". Unit A reads it as the first five surviving entries in provider order; Unit B reads it as every entry with rank ≤ 5, which under ties is six or more. It is also unstated whether "hits" means the raw provider list or the canonicalized list (post blank/non-finite drop, post first-occurrence retention). Different seed sets produce different graph candidates, hence different graph ranks, hence different composite scores — again with both units compliant.

### Proposed tightening — AD-9 Rule, replacement sentence (with AD-11 kept as the limit's source)

> "Auto-seed from exactly the first five entries of the canonicalized syntactic list and the first five of the canonicalized semantic list in the post-canonicalization order defined above, taking five entries and not five ranks; apply AD-11's result limit once, to the merged graph list after the per-case merge and before rank allocation, never per case partition; apply AD-11's time limit once to the whole tenant-wide graph fan-out; and when any selected case partition does not complete within depth, result, or time limits, the graph axis is reported as a third state, `truncated` — available, carrying its hits, contributing denominator weight, and named in the Evidence Packet as degraded with the count of uncompleted case partitions — so that an incomplete traversal is never indistinguishable from a complete one."

---

## ADV-05 — high — "Safely" is undefined, and it selects AD-9's normalization denominator

**Unit A — Redis search/vector adapter epic** (`Adapters.Redis`).
**Unit B — FalkorDB graph adapter epic** (`Adapters.FalkorDb`).

### Compliance proof

AD-10 line 114 hangs the entire degradation contract on an undefined adjective, used twice:

> "Query every selected usable axis and return a partial result when at least one can respond **safely**; list unavailable/excluded axes, degradation, freshness impact, and recovery guidance in the Evidence Packet. Fail the query only when no selected axis can produce a **safe response**."

AD-9 line 108 then makes that judgment numerically load-bearing:

> "Normalize the weighted sum by top-rank contribution times **weights of axes that returned results** … A null axis is unavailable and an empty axis is available with no hits; neither adds denominator weight".

Consistency Conventions line 181 uses the same undefined term again — "a server that can still produce a **safe available-axis** response" — without defining it. Each adapter epic must decide, for its own backend, whether it can respond safely, and both readings are defensible from the spine's own vocabulary:

- Unit A reads safe as *the data-plane call succeeded and the tenant scope was applied* — AD-10 line 112's Binds names "freshness impact", implying stale-but-scoped answers are safe and are disclosed rather than withheld.
- Unit B reads safe as *scope provably verified and the answer provably complete* — AD-7 line 96 makes case scope absolute over "every candidate, result, node, and edge", AD-6 line 90 requires FalkorDB to "select a separate tenant database/graph", and for a traversal an incomplete path is a wrong answer rather than a partial one.

Neither unit violates a Rule. The spine defines the tie predicate (`Double.Equals`, no epsilon), the sort comparer (`StringComparer.Ordinal`), and the contribution constant (`10`) to the character — and leaves the predicate that selects the denominator undefined.

### The concrete incompatibility

With FalkorDB alive but slow, and Redis alive and serving:

- Unit B's doctrine nulls the graph axis (it cannot prove completeness within the limit). Denominator = 0.30 + 0.35 = 0.65.
- Unit A's doctrine keeps it (the call returned). Denominator = 0.30 + 0.35 + 0.35 = 1.00.

Every result's composite score therefore differs by a factor of ~1.54 for the same query against the same data at the same instant, and the ordering inverts wherever the graph axis was the discriminator. FR63 makes that number product-visible ("relevance confidence (0.0–1.0)"), NFR24 requires per-axis contributions to be preserved on every evidence-bearing surface, and NFR25 requires cross-surface identity — so the product's headline trust number moves by half its range according to which adapter team's private safety doctrine happened to apply, with no implementation defect on either side. The same ambiguity also lets the Evidence Packet's axis health and the capability-aware health endpoint (line 181) disagree about the same backend, contradicting FR56's requirement that the CLI "name the unavailable/excluded axes".

### Proposed tightening — AD-10 Rule, replacement sentence

> "An axis can respond safely if and only if its adapter applied the request's authoritative tenant and case scope, returned within AD-11's or the axis's configured limits without truncation, and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`; staleness, low recall, and empty results are safe and are disclosed as freshness or emptiness, whereas unverified scope, an exhausted limit, and a generation or epoch mismatch are unsafe and null the axis; safety is decided once per query by the server's axis-selection step against this definition, never independently by each adapter, and the resulting availability vector is the single input to both AD-9's denominator and the Evidence Packet's axis health."

---

## ADV-06 — high — Pagination is unbound relative to rank allocation, so the same result has two confidence values

**Unit A — REST search API unit** (FR21/FR22 metadata filter and pagination on `GET /api/v1/search`, recorded as product-active in the PRD delivery register).
**Unit B — Axis adapter performance unit** (the epic that meets NFR1–NFR3 p95 budgets and NFR12's ten-tenant scaling on 100K-unit tenants).

### Compliance proof

The spine contains no occurrence of "page", "offset", or "pagination" — verified across all 324 lines. The two Rules that touch the question are AD-9 line 108, which describes fusion over complete axis lists and never mentions a window, and AD-11 line 120, which grants "server-owned depth/**result**/time limits" without saying where in the pipeline the result limit and any offset apply. AD-18 line 162 supplies the motive for the second unit: "use tenant-partitioned admission/concurrency and bounded durable queues".

Unit A fuses full axis lists and windows the fused output — compliant, and the natural reading. Unit B pushes the caller's offset and limit down into each axis so that a page-20 request does not pull 200 candidates per axis per query — also compliant, required by no Rule to do otherwise, and the standard way to hold a p95 budget at scale.

### The concrete incompatibility

Under Unit B, competition ranks restart at 1 inside every page. Page 2's top entry receives contribution `1 / (10 + 1) = 0.0909` and, after AD-9's normalization "by top-rank contribution times weights of axes that returned results", normalizes to a composite of 1.0 — **the identical score the page-1 top result carries**. Under Unit A it carries the rank it earned globally. Consequences, all with both units compliant:

- One memory unit has two different `confidence` values (FR63, NFR24) depending on which page it is fetched on.
- The concatenation of Unit B's pages is not the descending-composite list AD-9 line 108 mandates; it is a sequence of independently normalized blocks. AD-9's final ordering guarantee holds only within a page.
- Where an axis's provider order is not stable under offset, units can be repeated or skipped across page boundaries, which FR22 does not contemplate and no Rule forbids.
- NFR25's "Golden-vector contract tests across REST, CLI JSON, and MCP" cannot be authored: a golden vector is page-shaped under B and page-independent under A.

The prior gate's four passes proved AD-9 deterministic; every one of those proofs silently assumed unpaged axis lists.

### Proposed tightening — AD-9 Rule, replacement sentence

> "Fusion is defined over each axis's complete server-limited candidate list and produces one ranked list per query; AD-11's result limit and any caller offset are applied only to the fused output after rank allocation, normalization, and final ordering, never pushed into an axis provider, so that a given `MemoryUnitId`'s rank, per-axis contributions, and composite score for a query are identical whatever page it is returned on, and the concatenation of consecutive pages is exactly the head of the single fused ordering."

---

## ADV-07 — high — `nl` is simultaneously a weighted axis and a post-fusion rerank

**Unit A — Phase 1.5 EventStore dual-embedding unit** (FR60: raw payload plus natural-language description embeddings).
**Unit B — Fusion and explain unit** (FR19/NFR24, the owner of `--explain` and the Evidence Packet's per-axis contributions).

### Compliance proof

AD-9 line 108 names `nl` inside the weight sentence, in the same grammatical slot as the three axes:

> "Default weights are `0.30/0.35/0.35`; **NL weight is `0.20`**, default-off, and limited to requested/configured Phase 1.5 event units."

AD-9 line 106 names the same thing as a rerank, not an axis:

> "**Binds:** G1, syntactic, semantic, graph, **natural-language reranking**, result ordering, degradation, and explainability."

The PRD Glossary, which the spine does not contradict, sides with line 106: "`nl` is an optional extra semantic score on the natural-language-description embedding, **not a fourth marketing axis**."

Unit A takes line 108 literally: a stated weight is a term in a weighted sum, so `nl` is a fourth ranked axis whose 0.20 enters the AD-9 denominator when it returns results, appears in the Evidence Packet's axis-health list, and is selectable through FR18's axis control. Unit B takes line 106 and the Glossary literally: `nl` is a post-fusion reorder over the already-fused three-axis list, never a denominator term, never an Evidence Packet axis, never an `--axis` value. Both are complying with AD-9.

### The concrete incompatibility

With NL enabled on Phase 1.5 event units:

- **Denominator:** Unit A normalizes over `0.30 + 0.35 + 0.35 + 0.20 = 1.20`; Unit B over `1.00`. Every composite score differs by a factor of 0.833.
- **Result-set membership, not just scores:** under Unit A a unit that ranks only on the NL embedding enters the fused list and can outrank three-axis results; under Unit B a post-fusion rerank can only permute units the three axes already produced, so that unit **can never appear**. The two surfaces return different sets, not different orderings of one set.
- **Contract shape:** the Evidence Packet carries four axis entries under A and three under B, and `--axis nl` is a valid value under A and an error under B — a divergence in the CLI/MCP grammar that AD-12 line 126 requires to be equivalent across surfaces. (`addendum.md` already records a live instance of exactly this class of drift: "MCP parameter naming (`axis` vs `axes`) is declared once in the CLI surface table".)
- **Explain:** FR19 requires "per-axis score breakdown … including normalization method applied". Under B there is no NL axis to break down, so a 0.20-weighted influence on the public ordering is undisclosed while still being compliant.

Default-off and Phase 1.5 gating keep this out of G1/G5, which is why it is high and not critical — but it is a numeric and set-membership divergence in the product's headline algorithm that survived four prior passes over AD-9.

### Proposed tightening — AD-9 Rule, replacement sentence

> "`nl` is a fourth ranked axis and not a rerank: when requested and configured for Phase 1.5 event units it is canonicalized, competition-ranked, and contributes `1 / (10 + rank)` at weight `0.20` inside the same weighted sum, its weight enters the denominator only when it returned results, it appears in the Evidence Packet's axis-health list and in `--explain` on every surface under the wire name `nl`, and it is a selectable value of the axis-control parameter; on a surface or phase where NL is not active it is reported as an excluded axis rather than omitted, so the axis set is identical across surfaces."

---

## ADV-08 — high — "Equivalent null/omission meaning" is satisfied by two mutually unreadable encodings

**Unit A — REST/Contracts.V1 serializer unit.**
**Unit B — MCP tool-result unit** (FR58: "typed parameter schemas with descriptions for LLM agent consumption").

### Compliance proof

AD-12 line 126:

> "Capability subsets may vary operations, never Evidence Packet semantics: every evidence-bearing result carries tenant/case scope, source/origin, confidence and per-axis contribution, degradation/excluded axes, omitted-detail handles, freshness, and recovery **with equivalent null/omission meaning**."

The prior gate's H5 closed the *field-removal* failure with this sentence. What the sentence constrains is *meaning*; it does not constrain *encoding*, and the two are separable.

Unit A configures `System.Text.Json` with `DefaultIgnoreCondition = WhenWritingNull`, the conventional .NET posture: an unavailable axis is **absent** from the axis-health collection, an empty axis is **present and empty**. The distinction AD-9 line 108 demands ("the Evidence Packet distinguishes them") is fully preserved. Unit B emits every property declared in its MCP tool schema, using explicit `null` for unavailable and `[]` for empty; absence never occurs. Also fully preserved. Both units carry every required element with equivalent meaning. Neither violates anything.

### The concrete incompatibility

- One deserializer cannot read both. On surface A, "field absent" means *axis unavailable*; on surface B, "field absent" cannot happen and would mean a protocol error. A generic consumer — `Hexalith.Memories.Client.Rest`, the PRD's promised cross-language clients, or the CLI's `--format json` which NFR37 requires to "preserve the same semantics" as the human form — must be written twice, against one contract.
- NFR25's gate obligation is unauthorable: "Golden-vector contract tests across REST, CLI JSON, and MCP" require one expected document per vector, and there is no compliant single document. G5 cannot be closed without an arbitrary tiebreak the spine does not supply.
- Under Unit A's encoding, absence is overloaded: an unrecognised axis (`nl` on a Phase 1 server, see ADV-07), an unavailable axis, and an absent omitted-detail handle are all "field not present". Unit A therefore cannot distinguish two of AD-12's own mandatory elements from each other, while remaining compliant with the sentence that mandates them.
- The same gap covers the packet's *state cardinality*. The PRD Glossary fixes an eight-value vocabulary (`complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, `pendingExpansion`) and then licenses divergence — "current mappers need not emit every target state yet" — and the spine never mentions the vocabulary at all, nor whether the state is one value or a set. One unit emits the single most severe state (`degraded`); another emits `["partial","degraded","stale"]`. Both carry "degradation" per AD-12.

### Proposed tightening — AD-12 Rule, replacement sentence

> "Evidence Packet equivalence is equivalence of the serialized document, not only of intent: every element listed above is always present on every surface, an unavailable axis or absent value is encoded as an explicit JSON `null` and never by omitting the property, an available-with-no-hits axis is encoded as an empty collection, no surface may enable null-omitting serialization for evidence-bearing types, the packet state is a single value drawn from the versioned state vocabulary with an accompanying set-valued reason list, and one golden-vector document per case is byte-comparable across REST, Dapr invocation, CLI JSON, and MCP."

---

## ADV-09 — high — `Contracts.V1` has no owner, so two additive changes can collide on one wire name and become unresolvable

**Unit A — Tenant-wide search and attribution unit** (FR34: "mandatory case attribution").
**Unit B — Explain unit** (FR19/NFR24: per-axis rank contributions and weights).

### Compliance proof

AD-12 line 126: "**Evolve Contracts.V1 additively** and preserve deliberate wire names until a versioned break."

Both units are *required* by AD-12's own element list to add to the same envelope — Unit A supplies "degradation/excluded axes" and case scope, Unit B supplies "confidence and per-axis contribution". Both add a property named `axes`:

- Unit A: `"axes": {"syntactic": "available", "semantic": "unavailable", "graph": "empty"}` — a map from axis to health.
- Unit B: `"axes": [{"axis": "syntactic", "rank": 3, "contribution": 0.0769, "weight": 0.30}, …]` — an array of contributions.

Neither removes a field. Neither renames a deliberate wire name. Neither breaks a consumer of anything that existed before. Both are purely additive, which is the only evolution constraint AD-12 states. Both comply completely.

### The concrete incompatibility

The second unit to merge either overwrites the first's shape or is forced to rename — **and AD-12 forbids the rename outside "a versioned break"**, so the spine's additive rule can trap the contract in a state where the only compliant exits are shipping a collision or opening a V2 window. The clash is not hypothetical for this repository: `addendum.md` › Downstream drift records a live instance — "MCP parameter naming (`axis` vs `axes`) is declared once in the CLI surface table (`--axis`); the MCP tool schema should be checked against it when Epic 10 hardening is next touched."

The root hole is ownership. The spine's central device is naming an owner: AD-6 owns tenant resources, AD-8 owns the adapter boundary, AD-17 owns telemetry, AD-18 line 162 says quotas have "separate owners", and AD-19 makes `tools/release-packages.json` the sole packaging authority. For the cross-surface contract — the one artifact AD-12 exists to keep singular — the spine supplies only a *location*: Consistency Conventions line 174, "Public APIs live in `Contracts/V1`", and Capability map line 281, "Contracts.V1 Evidence Packet". A location does not arbitrate two epics adding the same name in the same sprint, and no Rule requires a name to be reserved before it is used.

### Proposed tightening — AD-12 Rule, added sentence

> "`Contracts.V1` has one named owning component with change-approval authority over its wire surface, and additive evolution is admissible only after the new wire name, its JSON shape, and its nullability are reserved in the contract's versioned name register in the same change; a name already reserved may not be reused with a different shape by any surface or epic, a collision is resolved before merge and never by a post-hoc rename, and architecture tests fail the build when a public evidence-bearing type carries a wire name absent from the register."

---

## ADV-10 — high — The Direct Redis Exception Registry's amendment process permits two lock stores for one resource

**Unit A — Repair-and-replay unit** (the epic closing the alignment gaps at lines 296 and 300).
**Unit B — Import and migration unit** (the epic owning import leases and AD-14 migration state).

### Compliance proof

AD-8 line 102: "Only the finite Direct Redis Exception Registry below may bypass Dapr state, and any addition requires a new architecture decision."
Line 194: "Registries, leases, fences, mappings, and migration coordination use Dapr state **unless a later `AD-n` adds a row** with its primitive, failure posture, tenant/key scope, and test evidence."
Alignment gap line 300 states the convergence as a genuine either/or: "Move data-plane code behind named adapters; **move coordination to Dapr state or adopt an explicit `AD-n` exception**; enforce both boundaries with architecture tests."

Unit A takes the first branch: the failed-unit registry and the derived-store fence move to Dapr state with ETag CAS. Unit B takes the second: it authors `AD-20` and adds a registry row for import leases and migration coordination, using `SET NX PX` plus a Lua compare-and-delete. Both branches are explicitly offered by the same sentence. Neither unit violates a Rule, and the amendment process is followed exactly.

### The concrete incompatibility

**(a) Two locks, one resource, no mutual exclusion.** Tenant `T`'s derived Redis index has a Dapr-state fence held by repair (Unit A) and a Redis lease held by import (Unit B). Neither primitive can observe the other. Both proceed. AD-3's ETag/CAS protects the *projection checkpoint*, not the index contents, and AD-4's "idempotent upsert" protects per-tuple writes, not a repair's delete-and-rebuild sweep. AD-18 line 162 orders "repair/migration below interactive priority" but never mutually excludes repair from migration. Result: repair deletes index entries the import just wrote, both report success, and FR74's guarantee that repair "cannot invent edges the current authoritative EventStore revision does not support" is untouched — the corruption is deletion, not invention.

**(b) The registry's only precedent is fail-open.** The single existing row (line 192) states "Reservation fails open to AD-4 durable suppression; release is best-effort and TTL is the final cleanup". A new row's author has one documented example and no rule, so choosing fail-open for a *lease* is the well-precedented choice — and a fail-open lease during a Redis blip admits two concurrent importers to one tenant's derived stores. The spine never states that mutual-exclusion primitives must fail closed while admission optimizations may fail open, even though that distinction is the entire reason the existing row is safe.

**(c) No owner of the Redis key namespace.** The existing row constrains its own key ("Key includes tenant/case and canonical request identity") and nothing constrains the next one. The Consistency Conventions govern project names, routes, statuses, identifiers and packaging, but never Redis key layout — AD-5 line 83 mentions key prefixes only to deny them authorization force. Two rows can therefore claim the same prefix.

### Proposed tightening — Direct Redis Exception Registry, replacement closing sentence

> "Registries, leases, fences, mappings, and migration coordination use Dapr state, and a later `AD-n` may add a row only when it also states the single coordination store for the protected resource — no resource may be protected by primitives in two stores, and any new row that guards a resource already guarded elsewhere must migrate the existing guard in the same decision; mutual-exclusion primitives (leases, fences, locks) fail closed, only admission optimizations with a durable fallback may fail open, and every row declares its reserved Redis key prefix, which no other row may claim."

---

## ADV-11 — low — Two container-default surfaces, one pinning obligation

AD-19 line 168 requires implementers to "pin qualified container defaults" and Alignment Gap line 305 names two surfaces at once — "AppHost **and** Aspire defaults use floating Redis/Falkor images" — without saying which one the Stack table's qualified version belongs to. The AppHost composition unit pins for the source lane; the `Hexalith.Memories.Aspire` integration-package unit pins for consumers. Both comply and can qualify against different Redis Stack builds while each truthfully reports "pinned qualified defaults" — a live risk given the `7.4.0-v8` end-of-maintenance note at line 214. Suggested clause for AD-19: *"`Hexalith.Memories.Aspire` is the single owner of qualified container image digests; AppHost consumes those same defaults rather than declaring its own, and both release lanes produce their integration evidence against the one digest set recorded in the Stack table."*

---

## Retest of the prior gate's closed findings

| Prior finding | Still closed? | Basis |
| --- | --- | --- |
| C1 status wire vocabulary | Yes | Line 177 fixes the six lowercase V1 values and the two product mappings; PRD Open Question 8 closed 2026-09-12 to the same values. |
| C2 internal Dapr authorization | Yes as a *mechanism*; reopened as an *ownership* question | Line 84's layered check stands. ADV-03 attacks who writes the allowlist and grant, not whether they are required. |
| C3 degrade vs fail | Yes as a *direction*; reopened as a *predicate* | Line 114 fixes that partial service is returned. ADV-05 attacks what "safely" means. |
| H1 Phase 1 graph population | Yes | Line 120 enumerates Phase 1 and Phase 1.5 sources and matches the PRD's Phase 1 population decision. |
| H2/H10/H11 RRF construction | Yes for the ranking function | Line 108's canonicalization, tie predicate, competition ranks, per-case merge and final ordering are unique. ADV-04, ADV-06 and ADV-07 attack the function's inputs and its axis set, not the function. |
| H3/H4 adapter and EventStore assembly ownership | Yes | Lines 102, 39 and the Structural Seed distinguish the mixed assembly, the pure `Domain/` subtree, and the compatibility facade. |
| H5 Evidence Packet field removal | Yes for *presence*; reopened for *encoding* | Line 126 forbids varying semantics. ADV-08 attacks the serialization that carries them. |
| H6 workflow secrets and epoch | Yes | Lines 78 and 138 require secret references only and execution-time resolution. |
| H7/H8 Web shape and MCP activation | Yes | Lines 126, 307 and 308 gate activation on L1–L3 and keep Web a non-activated conformance specimen. |
| H9 G1 alignment accounting | Yes | Lines 301–302 carry auto-seeding, the per-case merge, the kill switch and the G1 harness as obligations. |
| M1–M8, L1–L3 | Yes | Lines 175, 90, 179, 204–205, 156, 181 and 176 carry the respective conventions; the package count is gone. |

## Gate disposition

The spine remains a sound build substrate and none of these findings reopens an adopted decision — every one is closable by adding or tightening a Rule sentence in place. Four are ownership or ordering holes that two independently staffed epics will hit on contact (ADV-01 through ADV-04) and should be closed before any of the affected epics is sprint-selected: the projection coordinator, the erasure workflow, the internal-authorization path, and tenant-wide graph search. Three more (ADV-05, ADV-06, ADV-07) each defeat NFR25's cross-surface determinism and therefore block gate G5 independently of implementation quality; ADV-04(b) and ADV-06 also make golden-vector authorship impossible, so G5's evidence cannot be produced until they are decided. ADV-08 and ADV-09 should be closed alongside the FR34/FR19 contract work rather than after it, since ADV-09's failure mode is unresolvable once both changes have shipped.

**Recommendation:** PASS-WITH-FINDINGS. Adopt the eleven proposed clauses as amendments to AD-3, AD-5, AD-9 (four separate clauses), AD-10, AD-12 (two clauses), AD-16, AD-19 and the Direct Redis Exception Registry, then re-run this lens against the amended text.
