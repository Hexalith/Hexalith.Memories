# Adversarial Divergence — Amendment Retest, 2026-09-12

**Verdict: PASS-WITH-FINDINGS — 4 of 11 prior pairs fully closed, 7 partially closed, 0 still open; 17 new divergence pairs (5 critical, 5 high, 5 medium, 2 low). Two of the new critical pairs are direct contradictions introduced by the amendment itself, and one of them re-creates the exact numeric divergence ADV-05 was amended to close.**

**Artifact (read-only):** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` — 353 lines, 20 ADs, `status: final`, `updated: 2026-09-12`. All line numbers below are lines of that file.
**Baseline compared against:** the pre-amendment 324-line spine and `_bmad-output/planning-artifacts/architecture-validation-2026-09-12/reviews/review-adversarial-divergence.md`.
**Lens:** two implementation units one level down that each obey every AD to the letter and still build incompatibly.

## Method and discipline

Every finding names two units that different epics could plausibly staff, quotes the exact Rule text and line number each relies on, shows that **neither unit violates the AD it implements**, exhibits the concrete incompatible artifact, and proposes a replacement sentence. Candidates that required one unit to break a Rule were discarded as implementation bugs, not spine holes; six were dropped this pass (an adapter deciding its own safety verdict — now explicitly forbidden at line 122; a surface omitting an Evidence Packet element — forbidden at line 134; a workflow persisting extracted text — forbidden at line 86; a per-tenant Redis ACL created on the ingestion hot path — forbidden at line 98; a second registry row failing open on a lease — forbidden at line 210; AppHost declaring its own image defaults — forbidden at line 176).

Two findings below (ADV2-01 and ADV2-03) are a stronger class than a divergence: the two units each satisfy one binding sentence and cannot satisfy both, so **no compliant implementation exists**. They are reported as spine holes rather than bugs precisely because compliance is unreachable, not because either unit is careless.

The amendment was materially good: it added roughly 3,400 words of Rule text and closed every failure mode the prior pass demonstrated. It also grew the attack surface proportionally, and the new text is where 15 of the 17 new pairs live.

---

## Part A — Retest of ADV-01 … ADV-11

| ID | Prior failure | Verdict | Evidence in the amended text | Surviving variant |
| --- | --- | --- | --- | --- |
| **ADV-01** | AD-3 tuple had no ordering relation, no designated reported member, no retirement owner | **Closed** | L80 adopts all three: `(embeddingConfigurationEpoch, schemaGeneration)` as identity discriminator with `sourceVersion` "the only monotonic dimension"; "exactly one checkpoint record exists per `(tenantId, caseId, memoryUnitId, schemaGeneration, embeddingConfigurationEpoch)`"; "an acknowledgement for a different generation or epoch is never stale"; "The reported public ingestion state is always that of the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`"; "AD-14's retire step deletes the retired generation's and epoch's checkpoint records". Variants (a), (b) and (c) are each individually answered. | None of the three. The same clause opened two new holes — the write fence's "older" is undefined across epochs (**ADV2-03**) and an invalidated checkpoint has no state (**ADV2-13**) — but neither is a survival of ADV-01. |
| **ADV-02** | Erasure mapping had two claimed owners, no store, no blocking semantics, TTL-inheritable tombstone | **Partially closed** | L158 creates the "single **durable erased-tenant register** … sole authority for replay rejection, tenant-ID non-reuse, and restore admission", "written and owned by the erasure workflow", carrying the tombstone "with no TTL and no purge path"; "any telemetry-side copy is derived and can never satisfy, delay, or revoke completion". Handoff strengthened from "recorded" to "**acknowledged**", so the complete-without-ack variant is dead. L164 demotes AD-17 to "**its derived copy** of the tenant-erasure mapping". | **The store is still unnamed.** The proposed clause said "in Dapr state under AD-8"; the adopted text says only "held outside any tenant-scoped keyspace and never encrypted under a destroyed tenant key", then calls the completion record "domain evidence under AD-2". AD-2 (L74) makes EventStore domain truth and L210 makes registries Dapr state — two homes for one "single" register. **The read path is stated for restore only.** "Every restore path … consults that register" names three restore consumers; the non-reuse consumer (AD-6 provisioning, L98) and the replay consumer (AD-2/AD-3 replay, L74/L80) are given the register's *authority* but no *obligation to read it*. See **ADV2-04**. |
| **ADV-03** | Allowlist and per-tenant grant had two possible writers with opposite failure directions | **Partially closed** | L92 adopts nearly the whole proposal: allowlist is "a deployment-time artifact with exactly one writer, the operator", "grants principal identity and never tenant scope", "an absent app ID fails closed with no default principal"; the per-tenant grant's "sole writer is AD-6's lifecycle workflow, created only by verified provisioning and revoked as a completion condition of AD-16 erasure"; the three independent checks are enumerated, "any one of which failing closed". The at-create and at-delete divergences and the missing tenant-active check are all answered. | **What the grant contains at provisioning is unspecified, and no writer exists for a grant on an existing tenant.** "created only by verified provisioning" names the moment but not the membership, and read strictly it forbids adding an app ID to a live tenant at all. See **ADV2-05**. **Revocation has no propagation bound** — AD-15 (L152) gives rotated credentials one ("no connection or client established under a superseded credential outlives that bound") and the allowlist, now a secret under L92, gets none. See **ADV2-09**. |
| **ADV-04** | Graph seeds, limits and truncation unbound | **Partially closed** | (a) closed: L116 "Apply AD-11's result limit once to the merged graph list after the per-case merge and before rank allocation, never per case partition, and apply AD-11's time limit once to the whole tenant-wide fan-out." (c) closed: "the union of the first five entries of the canonicalized syntactic list and the first five of the canonicalized semantic list — five entries, not five ranks." | (b) **the `truncated` state was added in AD-9 and contradicted in AD-10 in the same amendment** — L116 gives it denominator weight, L122 nulls it. See **ADV2-01**, which restores ADV-05's ~1.54× score divergence under a new name. `truncated` is also defined only in graph vocabulary ("the count of uncompleted case partitions") with no meaning for the syntactic, semantic or `nl` axes. See **ADV2-02**. |
| **ADV-05** | "Safely" undefined and it selects AD-9's denominator | **Partially closed** | L122 defines it as a biconditional, enumerates the safe set (staleness, low recall, empty) and the unsafe set (unverified scope, exhausted limit, generation/epoch mismatch), and centralizes the decision: "Safety is decided once per query by the server's axis-selection step against this definition, never independently by each adapter, and the resulting availability vector is the single input to both AD-9's denominator and the Evidence Packet's axis health." Two adapters can no longer hold private doctrines. | The predicate is **evaluated at an unstated time from facts only the adapter holds**. "an axis-selection step" and "Query every selected usable axis" place selection *before* the query; "returned within … limits without truncation" is knowable only *after* it. No adapter→server reporting contract for scope-applied and truncated is stated. See **ADV2-02**. And the predicate's "exhausted limit" clause is the half that contradicts AD-9 (**ADV2-01**). |
| **ADV-06** | Pagination unbound relative to rank allocation | **Closed** | L116: "The caller's result limit and offset apply only to the fused output after rank allocation, normalization, and final ordering and are never pushed into an axis provider, so a unit's rank, per-axis contributions, and composite score are identical whatever page returns it and consecutive pages concatenate to exactly the head of the single fused ordering." Both the double-confidence and the non-concatenating-pages artifacts are dead. | None. A new adjacent hole appears where the closed paging clause meets the new candidate-depth clause: paging past the fused list's depth-bounded end. See **ADV2-12**. |
| **ADV-07** | `nl` simultaneously a weighted axis and a rerank | **Closed** | L114 Binds changed from "natural-language reranking" to "natural-language **ranking**", removing the contradicting half. L116: "`nl` is a fourth ranked axis and not a rerank … contributes at weight `0.20` inside the same weighted sum, default-off, entering the denominator only when it returned results, appearing in Evidence Packet axis health and in explain output under the wire name `nl`, and selectable through the axis-control parameter; where NL is inactive it is reported as an excluded axis rather than omitted." Denominator, set membership, contract shape and explain are all resolved one way. | None as stated. It does leave a fourth axis-state name, `excluded`, outside AD-9's "the Evidence Packet distinguishes all **three**" enumeration. See **ADV2-17**. |
| **ADV-08** | Equivalent meaning satisfied by two unreadable encodings | **Partially closed** | L134: "**Equivalence is equivalence of the serialized document:** every element is always present on every surface, an unavailable axis or absent value is an explicit JSON `null` and never an omitted property, an available-with-no-hits axis is an empty collection, no surface enables null-omitting serialization for evidence-bearing types, the packet state is a single value drawn from the versioned vocabulary (`complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, `pendingExpansion`) with an accompanying set-valued reason list, and one golden-vector document per case is byte-comparable across REST, Dapr invocation, CLI JSON, and MCP." The `WhenWritingNull` divergence and the state-cardinality divergence are both dead. | **"Byte-comparable" is asserted without a canonical serialization.** Property order, `double` formatting, escaping and the transport envelope are all unconstrained, so two compliant serializers still produce non-identical bytes and NFR25's golden vector remains unauthorable. See **ADV2-08**. `truncated` also has no stated encoding: the two-way rule (null / empty collection) covers `unavailable` and `available` only. See **ADV2-02(b)**. |
| **ADV-09** | `Contracts.V1` had no owner; additive changes collide on a wire name | **Partially closed** | L134: "`Contracts.V1` has one named owning component with change-approval authority over its wire surface, and an additive change is admissible only once its new wire name, JSON shape, and nullability are reserved in the contract's versioned name register in the same change; a reserved name may not be reused with a different shape, and a collision is resolved before merge rather than by a later rename." The `axes`-map-versus-`axes`-array collision is now inadmissible. | **The owning component is required to be named and is never named** — no component appears anywhere in the spine as the owner of `Contracts.V1`, unlike AD-19 which names `Hexalith.Memories.Aspire` outright at L176. **The register has no home, no enforcement statement and no ledger row.** L60 requires an unenforced rule to say so ("Where a rule is not yet mechanically enforced, it says so"); AD-12 does not, and the proposal's architecture-test sentence was dropped. See **ADV2-07**. |
| **ADV-10** | Registry amendment permits two lock stores for one resource | **Partially closed** | L210 adopts the whole clause: single coordination store per protected resource, "a new row guarding an already-guarded resource must migrate the existing guard in the same decision", "Mutual-exclusion primitives (leases, fences, locks) **fail closed**; only admission optimizations with a durable fallback may fail open", "Every row declares its reserved Redis key prefix, which no other row may claim." (a) and (b) are dead. | (c) **the only existing row declares no prefix.** L208's Scope column says "Key includes tenant/case and canonical request identity" and names no prefix, so the rule that protects the key namespace protects an empty set, while L326's ledger records five unregistered coordination key families already in the store. **"The protected resource" also has no granularity rule**, so two rows can describe the same resource at different altitudes and never trip the already-guarded test. See **ADV2-16**. |
| **ADV-11** | Two container-default surfaces, one pinning obligation | **Closed** | L176: "`Hexalith.Memories.Aspire` is the single owner of qualified container image digests and AppHost consumes those same defaults rather than declaring its own." The AppHost-versus-Aspire pair is resolved by name. | None for that pair. The proposal's second half — "both release lanes produce their integration evidence against the one digest set recorded in the Stack table" — was dropped, and a third surface (`deploy/kubernetes`) is bound by neither. See **ADV2-15**. |

**Part A counts: 4 Closed · 7 Partially closed · 0 Still open.**

No surviving variant reproduces its original artifact unchanged; every one is a narrower residue. The pattern across the seven partial closures is consistent and worth naming: **the amendment reliably fixed the behaviour and reliably under-specified the mechanism** — it named an owner without naming the store (ADV-02), named a writer without naming the payload (ADV-03), named a state without reconciling it with the predicate that consumes it (ADV-04/05), required a named owner without naming one (ADV-09), and mandated byte-comparability without a canonical form (ADV-08).

---

## Part B — New divergence pairs against the amended text

### ADV2-01 — critical — A `truncated` axis both carries denominator weight and is nulled, by two binding sentences added in the same amendment

**Unit A — Query degradation and axis-selection unit** (implements AD-10; owns the availability vector and the Evidence Packet's axis health).
**Unit B — Fusion engine unit** (implements AD-9; owns canonicalization, rank allocation, the weighted sum and the denominator).

**Compliance proof.** AD-9 line 116 assigns denominator weight per axis state, and is the only sentence in the spine that does so:

> "An axis is `unavailable` (null, no denominator weight), `available` (may be empty, no denominator weight when empty), or `truncated` — available, carrying its hits, **contributing denominator weight**, and named in the Evidence Packet as degraded with the count of uncompleted case partitions; the Evidence Packet distinguishes all three."

AD-10 line 122 defines the safety biconditional and disposes of a truncated axis in the opposite direction:

> "An axis can respond **safely** if and only if its adapter applied the request's authoritative tenant and case scope, **returned within AD-11's or the axis's configured limits without truncation**, and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`. Staleness, low recall, and empty results are safe …; unverified scope, **an exhausted limit**, and a generation or epoch mismatch are **unsafe and null the axis**. Safety is decided once per query by the server's axis-selection step against this definition … and the resulting availability vector is **the single input to both AD-9's denominator and the Evidence Packet's axis health**."

Unit A complies with AD-10: an axis that truncated did not return "without truncation", so it is not safe, so it is nulled, so the availability vector — which AD-10 declares the *single* input to the denominator — records it as unavailable. Unit B complies with AD-9: `truncated` is defined as available and denominator-weighted, and AD-9 is the sentence that owns the weighted sum. Each unit implements the AD it owns to the letter; the two sentences cannot both be honoured for the same axis, so no implementation is compliant with both. AD-10 itself acknowledges the state it erases three clauses later — "list unavailable/**excluded**/**truncated** axes" — proving the erasure is unintended, not a deliberate override.

**The concrete incompatibility.** Tenant-wide search over six cases; the FalkorDB fan-out completes four case partitions inside AD-11's time limit and two do not.

- Unit A: graph → `unavailable`, null. Denominator `0.30 + 0.35 = 0.65`. Packet axis health: `graph: null`. No degradation count.
- Unit B: graph → `truncated`, hits retained. Denominator `0.30 + 0.35 + 0.35 = 1.00`. Packet: graph degraded, `uncompletedCasePartitions: 2`.

Every composite score differs by a factor of `1/0.65 = 1.538`, ordering inverts wherever graph is the discriminator, FR63's 0.0–1.0 confidence moves by half its range, and the packet's axis health disagrees between the two units for the same query at the same instant. This is numerically and semantically the identical failure ADV-05 demonstrated and the amendment claimed to close; the fix for ADV-04(b) reintroduced it under a new state name.

**Proposed tightening — AD-10 Rule, replacement sentence:**

> "An axis can respond safely if and only if its adapter applied the request's authoritative tenant and case scope and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`; unverified scope and a generation or epoch mismatch are unsafe and null the axis to AD-9's `unavailable`, whereas an axis that applied scope and epoch but did not complete every selected partition or its own configured work within the depth, result, or time limit is **safe and `truncated`** in AD-9's sense — retained with its hits, carrying denominator weight, and disclosed as degraded — so that AD-9's three states and this predicate assign exactly one disposition to every axis, and the availability vector this step produces is the single input to both AD-9's denominator and the Evidence Packet's axis health."

---

### ADV2-02 — critical — Reaching the fixed candidate depth is both "the complete candidate list" and "an exhausted limit", and no adapter is required to report which

**Unit A — Redis syntactic/vector adapter and axis-selection integration unit** (`Adapters.Redis`, the epic closing the L326 ledger row).
**Unit B — FalkorDB graph adapter unit** (`Adapters.FalkorDb`, the epic closing the L328 ledger row).

**Compliance proof.** AD-9 line 116 makes the depth-limited list the axis's *complete* list:

> "Each axis contributes a fixed, server-owned, configuration-validated candidate depth identical across every surface, and **fusion is defined over each axis's complete server-limited candidate list**."

AD-10 line 122 makes an exhausted limit an unsafety condition:

> "returned within AD-11's or the axis's configured limits **without truncation** … **an exhausted limit** … unsafe and null the axis."

Unit A reads the two together as: the candidate depth is the axis's contract, so returning exactly `candidateDepth` hits is a complete, safe, `available` response — "server-limited" and "complete" are the same list by AD-9's own words. Unit B reads them as: a limit that was reached is by definition exhausted, so an axis that returned exactly `candidateDepth` hits truncated and is unsafe under AD-10, or at best `truncated` under AD-9. Neither unit violates the AD it cites. The spine never states which limits are "the axis's configured limits" for AD-10's purposes and which are AD-9's ordinary candidate depth, and it never obliges an adapter to report the fact either way — yet AD-10 requires the *server* to decide safety from facts (scope applied, truncation occurred) that only the adapter observes, with no reporting contract.

**The concrete incompatibility.**

**(a) Every large tenant degrades to total query failure, or none does.** With `candidateDepth = 100` and a 100K-unit tenant, essentially every query saturates the depth on the syntactic and semantic axes. Under Unit B's reading all three axes are unsafe on every query, every axis is nulled, and AD-10's "Fail the query only when no selected axis can produce a safe response" makes **search unusable for exactly the tenants NFR12 sizes for** — while remaining fully compliant. Under Unit A's reading the same query is `complete`. G1's recall protocol measures two different systems depending on which adapter epic's reading shipped.

**(b) `truncated` has no meaning or encoding outside the graph axis.** AD-9's definition of the state is written entirely in graph vocabulary — "named in the Evidence Packet as degraded with **the count of uncompleted case partitions**". A case-scoped query has one partition; the syntactic axis has none. Unit A therefore cannot emit the state at all for a syntactic axis and reports `available`; Unit B emits it with a count of `0` or `null`. AD-12 line 134's encoding rule covers only two dispositions — "an unavailable axis or absent value is an explicit JSON `null`", "an available-with-no-hits axis is an empty collection" — and says nothing about how a third state serializes, so the golden vector that L134 requires to be "byte-comparable across REST, Dapr invocation, CLI JSON, and MCP" has two compliant shapes.

**Proposed tightening — AD-9 Rule, replacement sentence:**

> "The server-owned candidate depth is the axis's complete contracted list and reaching it is never truncation; truncation is the failure to cover the request's full authoritative scope within the depth, result, or time limit — an uncompleted case partition, an aborted scan, or a provider-side cutoff — and every adapter returns, alongside its hits, an explicit machine-readable statement of whether it applied the request's tenant and case scope, whether it truncated, and the enumerated scope units it did not cover, which is the only evidence the AD-10 axis-selection step uses; the `truncated` state applies uniformly to every axis, its Evidence Packet shape carries an uncovered-scope-unit count that is `0` for a non-partitioned axis, and that shape is reserved in AD-12's name register before first use."

---

### ADV2-03 — critical — An MVP degraded rebuild has no epoch, no staging tuple, and no defined write-fence comparison across epochs

**Unit A — FR43 degraded rebuild command unit** (the MVP epic implementing AD-14's Phase 1 clause and the CLI acknowledgment flag).
**Unit B — Projection coordinator unit** (AD-3's Dapr-state checkpoint and write fence).

**Compliance proof.** AD-14 line 146, ratified 2026-09-12, newly permits an in-place MVP rebuild:

> "In **Phase 1 / MVP**, FR43 permits only a tenant-scoped, explicitly acknowledged degraded rebuild of rebuildable projections; the command discloses impact and progress, fails closed on incomplete verification, preserves authoritative EventStore truth, **carries the AD-3 configuration epoch**, and never claims zero downtime."

and reserves staging for Phase 2 — "In **Phase 2** … versioned create-backfill-verify-switch-retire migrations with **disjoint active and staging resources**" — a boundary the Consistency Conventions restate at line 198: "AD-14's 'staging resources' are migration resources inside a tenant". AD-14 also fixes the source of the epoch: "The active schema generation and embedding-configuration epoch are tenant-lifecycle domain facts committed through AD-2 **before any projection may cite them**."

AD-3 line 80 supplies the write fence and, critically, the ordering:

> "Order that tuple explicitly: `(embeddingConfigurationEpoch, schemaGeneration)` is the **identity discriminator** and `sourceVersion` **the only monotonic dimension** … Fence the write as well as the acknowledgement: derived stores hold **exactly one current document per `(tenantId, caseId, MemoryUnitId)`** carrying the tuple it was projected from, every projection write is conditional on that stored tuple and **is a no-op when the incoming tuple is older**."

FR43 is a change of embedding provider, model or dimension, so the rebuild necessarily runs under a new `embeddingConfigurationEpoch`, and MVP has no staging resource to write it into — the one current document per `(tenantId, caseId, MemoryUnitId)` is the target. Now: `sourceVersion` is the only ordered dimension, and epoch is explicitly *not* ordered ("identity discriminator"). So for two writes that differ only in epoch, "older" is **undefined**, and the fence has no verdict.

Unit A resolves the undefined comparison permissively: a differing epoch is not "older", the guard does not fire, the rebuild write lands. Unit B resolves it conservatively: the fence is conditional on the stored tuple and cannot establish that the incoming one is not older, so it no-ops. Both are literal readings of a sentence that supplies no relation.

**The concrete incompatibility.**

**(a) A write-write flip-flop, or a rebuild that writes nothing.** During the rebuild, ordinary ingestion continues at the pre-rebuild epoch (there is no second resource to divert it to). For unit `U`: the rebuild writes `(sv 7, gen 3, epoch 2)`; a concurrent ingest writes `(sv 8, gen 3, epoch 1)`; the rebuild's retry writes `(sv 7, gen 3, epoch 2)` again. Under Unit A each write clears the fence and the single stored document alternates epochs indefinitely, so the tenant's derived store holds a mixture of `768`- and new-dimension vectors in one index — precisely the "mixed embedding dimensions" AD-14 line 145 exists to prevent, reached without violating a rule. Under Unit B the rebuild writes nothing at all and then "fails closed on incomplete verification" forever.

**(b) Two opposite public postures for the same window.** AD-14 requires the new epoch be committed as a domain fact "before any projection may cite them", and AD-3 requires the reported public state to be "that of the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`". Unit A commits epoch 2 as active at the start: from that instant AD-10's safety predicate nulls every axis reading epoch-1 resources ("read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`"), so *every query for the tenant fails* for the whole rebuild, and every unit reports `indexing` — defensible as "never claims zero downtime". Unit B keeps epoch 1 active until verification, so search keeps serving, but the rebuild's writes cite an epoch that is not active, which AD-14 forbids unless there is a staging tuple — and AD-3's staging clause ("a staging tuple's completion is reported only as migration-backfill progress") presupposes the Phase 2 concept that line 146 withholds from MVP. Both units are compliant; one delivers a tenant-wide outage and the other delivers a rebuild that cannot cite its own epoch.

**Proposed tightening — AD-14 Rule, replacement sentence (with the fence relation added to AD-3):**

> "A Phase 1 / MVP degraded rebuild allocates a new `embeddingConfigurationEpoch` as an AD-2-committed **staging** epoch that is never the tenant's active epoch until the rebuild's verification succeeds, and it is the sole writer permitted to target a staging epoch inside the active resource set; ordinary ingestion continues at the active epoch throughout, the tenant's reported ingestion state and AD-10 axis safety are evaluated against the active epoch alone, and activation is a single AD-2 commit after which the retire step deletes the superseded epoch's documents and checkpoint records. AD-3's write fence compares only `sourceVersion` within one `(schemaGeneration, embeddingConfigurationEpoch)`: a write whose generation or epoch differs from the stored document's is never 'older', is admitted only when its epoch is the tenant's active epoch or the one staging epoch declared by the running rebuild, and otherwise is rejected as out-of-generation rather than treated as stale."

---

### ADV2-04 — critical — The erased-tenant register has one writer, two possible stores, and a read obligation on only one of its three declared consumers

**Unit A — Tenant erasure workflow unit** (AD-16; writes and owns the register).
**Unit B — Tenant provisioning unit** (AD-6's lifecycle workflow, which under AD-5 line 92 is also the sole writer of the per-tenant grant).

**Compliance proof.** AD-16 line 158 declares the register's scope of authority and its read obligation asymmetrically:

> "A single **durable erased-tenant register**, held **outside any tenant-scoped keyspace** and never encrypted under a destroyed tenant key, is the **sole authority for replay rejection, tenant-ID non-reuse, and restore admission** … is written and owned by the erasure workflow … **Every restore path** — EventStore restore, projection-store restore, and application export re-import — **consults that register before admission** … Verification means a recorded, reproducible check per enumerated target, and completion evidence is a content-free record in the register …; it is **domain evidence under AD-2**, not access telemetry."

Three consumers are given authority; exactly one — restore — is given an obligation to read. Unit A complies: it writes the register, and it is not the tenant-creation path. Unit B complies: AD-6 line 98 requires it to "Perform tenant resource changes only in explicit, idempotent, observable, bounded lifecycle workflows with verification, compensation, and resumable cleanup" and says nothing about the register; AD-5 line 92 makes tenant creation "authorized by an operator principal" and likewise says nothing. Unit B therefore validates a requested tenant ID against its own tenant directory — the only store AD-6 gives it — and that directory was purged by AD-16's own instruction to purge "**every store that can hold tenant content or content-derived material**".

The **store** is unnamed and two candidates are each supported by binding text. Unit A can put the register in EventStore, citing "it is domain evidence under AD-2" and AD-2 line 74's "Accept domain mutations through Hexalith.EventStore before reporting success". A restore-admission unit can read it from Dapr state, citing line 210's "Registries, leases, fences, mappings, and migration coordination use Dapr state" — a register is the archetype of that list, and AD-8 line 110 makes the only exception path the Direct Redis Exception Registry, which has no such row. Neither is wrong; the register is then not single.

**The concrete incompatibility.**

**(a) A tenant ID is reused after erasure, with no unit at fault.** Operator erases `acme-legal`; the erasure workflow writes the no-TTL tombstone to the register and purges every tenant-scoped store including the tenant directory row. Six weeks later `tenant create acme-legal` is issued. Unit B's directory shows the name free, AD-6 gives it no other check, and provisioning succeeds — creating a fresh Redis ACL principal, FalkorDB graph and per-tenant grant on an erased identifier. AD-16's "reject … reuse of the tenant ID" is asserted at line 158 and enforced by nobody. Retained access telemetry from the erased tenant, keyed by the same tenant token under the Consistency Conventions line 197 "stable, injective mapping", now attributes to the new tenant — silently defeating the injectivity that line requires.

**(b) Two registers, one "single" register.** Unit A's EventStore-resident register and the restore path's Dapr-state lookup return different answers during the window between an EventStore commit and any projection of it, and the restore path's answer is authoritative for admission. "Regardless of whether the payload is readable" then admits a restore whose tenant *is* erased, because it consulted an empty register.

**(c) Indefinite retention in a store the spine itself has not qualified.** If the register is Dapr state, it lands on the component the Deferred table line 344 flags: "Production Dapr actor/workflow state store — Before Production launch or SLO approval, qualify or replace the current Redis component against durability and recovery requirements." A register that must be "retained indefinitely" with "no TTL and no purge path" is placed by omission in the one store the spine says is not yet qualified for durability.

**Proposed tightening — AD-16 Rule, replacement sentence:**

> "The erased-tenant register is a single durable non-tenant-scoped resource in one named store — Hexalith.EventStore under a deployment-scoped, never-crypto-shredded stream — written only by the erasure workflow and readable by every consumer of its authority; AD-6's tenant provisioning workflow consults it before admitting any tenant identifier and fails closed on a hit, AD-2's replay and repair paths consult it before reading or projecting any tenant's events, AD-5's tenant-active revalidation resolves 'erased' from it and from no other source, and every restore path consults it before admission, so that no store other than the register may answer whether a tenant was erased and no path may act on a tenant identifier it has not asked."

---

### ADV2-05 — critical — AD-5's issuance grammar names no issuer, no validation point, no grandfathering, and does not reach identifiers that arrive already issued

**Unit A — Ingestion and tenant-provisioning unit** (issues `MemoryUnitId` server-side and validates tenant/case identifiers at the API boundary).
**Unit B — Export re-import unit** (AD-2 line 74: "application export restore is an import subject to normal ingestion rules"; AD-16 line 158: "export bundles are erasure-scoped artifacts").

**Compliance proof.** AD-5 line 92 constrains issuance and, separately, comparison:

> "**Treat issued tenant and case IDs as validated opaque tokens** and compare them with `StringComparison.Ordinal`, without case folding or Unicode normalization after issuance. **Issuance is what makes ordinal comparison sufficient:** restrict tenant, case, and `MemoryUnitId` **issuance** to one documented bounded ASCII grammar that excludes every character significant as a metacharacter, delimiter, or wildcard in Redis ACL key patterns, Redis/RediSearch/FalkorDB key and index names, Dapr key and component names, URL path segments, or Kubernetes object names, and compose every derived key, ACL pattern, index name, and state key unambiguously with a delimiter reserved out of that grammar."

Three things the sentence does not do: it never says **who** issues (AD-6 provisions tenant resources but AD-5 line 92 says tenant creation is "authorized by an operator principal", and AD-13 line 140 only forbids parsing `MemoryUnitId`, never saying who mints it); it never states a **validation point** other than issuance, and its first clause instructs every consumer to *treat issued IDs as already validated*; and it says nothing about identifiers that predate the grammar or were issued by another deployment.

Unit A complies: it mints `MemoryUnitId` as a 32-character hex GUID and validates operator-supplied tenant and case identifiers against the grammar at creation. Unit B complies: re-importing an export bundle is not issuance — the identifiers in the bundle were issued, by definition, and AD-5's first clause directs it to treat them as validated opaque tokens; AD-2's "subject to normal ingestion rules" imposes idempotency and authorization, not re-issuance; and AD-13 line 140 forbids it from parsing `MemoryUnitId` for meaning.

**The concrete incompatibility.**

**(a) A foreign identifier defeats the isolation mechanism AD-5 exists to protect.** A bundle exported from a pre-grammar deployment carries `MemoryUnitId = "unit:2026/07*draft"`. Unit B admits it as an already-issued opaque token. AD-5 requires every derived key, ACL pattern and index name to be composed with a delimiter reserved out of the grammar — but the reserved delimiter is only reserved against *grammar-conformant* identifiers, and this one contains `:` and `*`. The Redis ACL key pattern for the tenant becomes a pattern that matches keys outside the tenant, and the Direct Redis dedup key at line 208 ("Key includes tenant/case and canonical request identity") becomes ambiguous. AD-5's own Prevents clause at line 91 names exactly this outcome — "an identifier whose characters defeat the isolation mechanism that carries it" — and the Rule that is meant to prevent it does not reach the path that produces it.

**(b) The grammar bricks the existing corpus, or does not apply to it.** Every tenant, case and unit in the running system was issued before the grammar existed. Unit A ships grammar validation on the read path (a compliant, arguably required reading of "validated opaque tokens"); every legacy identifier now fails and the tenants become unreachable. Unit B validates nothing on read, since issuance already happened; legacy identifiers keep working and the grammar guarantees nothing about the deployed corpus. The spine has no migration, re-issue, or grandfathering clause and no ledger row for the transition, so both units are compliant and only one of them leaves the system operable.

**Proposed tightening — AD-5 Rule, replacement sentence:**

> "One named server-side component issues every tenant, case, and `MemoryUnitId` under the documented grammar and is the only place an identifier enters the system: any path that receives an identifier it did not issue — export re-import, EventStore restore, CloudEvent ingress, or a migrated deployment — validates it against the grammar at the admission boundary and rejects or re-issues it there, never admitting it as an already-validated token; identifiers issued before this grammar was adopted are a ledgered migration obligation with a named owner that must be re-issued or grammar-verified before the ACL-pattern and key-composition guarantees of this rule may be claimed."

---

### ADV2-06 — high — The reserved delimiter is required but never declared, and the non-grammar components composed into the same keys are unconstrained

**Unit A — Dapr pub/sub ingestion subscriber unit** (AD-4's CloudEvent identity; the `/events/ingest` adapter at line 190).
**Unit B — Preflight dedup unit** (the sole Direct Redis Exception Registry row at line 208).

**Compliance proof.** AD-4 line 86 fixes the CloudEvent identity from four components, only two of which AD-5's grammar governs:

> "scope CloudEvent identity by tenant, case, **exact validated `source`, and `id`** with ordinal comparison and no post-validation normalization."

AD-5 line 92 requires the composition but supplies neither the delimiter nor any constraint on `source` and `id`:

> "compose every derived key, ACL pattern, index name, and state key unambiguously with **a delimiter reserved out of that grammar**."

Line 208 requires the key to contain the composite: "Key includes tenant/case and canonical request identity". "Exact validated `source`" does not say validated against what; CloudEvents `source` is a URI-reference, so it legitimately contains `:`, `/`, `#`, `?` and `%`.

Unit A validates `source` as a CloudEvents URI-reference and composes `tenant|case|source|id`. Unit B, reading AD-5's exclusion list literally, validates `source` against AD-5's grammar — which must exclude every character significant in "URL path segments" and "Kubernetes object names", leaving roughly `[a-z0-9]` — and rejects every standards-conformant CloudEvent source. Neither violates a Rule; AD-4 says "validated" and never says by what.

**The concrete incompatibility.**

**(a) Phase 1.5 CloudEvent ingestion works on one unit and rejects everything on the other.** Unit B's reading makes L1–L3 unpassable: the launch-prerequisite samples publish domain events whose `source` is a URI. Unit A's reading admits them.
**(b) A duplicate-suppression collision that silently drops a real ingest.** With Unit A's `|` delimiter and unconstrained `source`, the events `(t1, c1, source="app|x", id="7")` and `(t1, c1, source="app", id="x|7")` compose the identical dedup key and the identical AD-4 durable suppression identity. The second is suppressed as a duplicate of the first, is never accepted into EventStore, and reports success — a silent data-loss path reachable by a publisher that has done nothing wrong. AD-5's "unambiguously" is the word that fails, and it fails because the delimiter is reserved out of a grammar that two of the four components never had to satisfy.

**Proposed tightening — AD-5 Rule, replacement sentence:**

> "One reserved composition delimiter is declared once for the whole platform and is excluded from the issuance grammar; every composed key, ACL pattern, index name, and state key is built from that delimiter alone, and every component of a composed key that is not itself issued under the grammar — CloudEvent `source` and `id`, `IdempotencyToken`, principal identifiers, and handle scopes — is length-bounded and reversibly encoded into the grammar's alphabet before composition, so that composition is injective over all inputs and no externally supplied value can produce the key of a different input."

---

### ADV2-07 — high — AD-12 requires a named owning component for `Contracts.V1` and the spine names none; the name register has no home, no enforcement statement, and no ledger row

**Unit A — Tenant-wide search and attribution epic** (FR34 case attribution; adds to the Evidence Packet).
**Unit B — Explain and per-axis contribution epic** (FR19/NFR24; adds to the same envelope).

**Compliance proof.** AD-12 line 134:

> "`Contracts.V1` has **one named owning component** with change-approval authority over its wire surface, and an additive change is admissible only once its new wire name, JSON shape, and nullability are **reserved in the contract's versioned name register** in the same change."

The spine names owners explicitly wherever it means to: `Hexalith.Memories.Aspire` at line 176, `tools/release-packages.json` at line 176, the erasure workflow at line 158, the operator at line 92, AD-6's lifecycle workflow at line 92, `MemoriesRoutes` at line 190. For `Contracts.V1` it supplies a *requirement* to name and no *name*: the Structural Seed line 238 gives a project, the Capability map line 305 gives a location, and Operational Boundaries line 295 says only "Contracts evolve additively under a name register". Both units therefore proceed under the ordinary repository process, each believing its own epic's approval satisfies "change-approval authority", and both reserve their name in whatever register they created — because the register's artifact, format, location and packaging are unstated. AD-19 line 176 packages "only entries declared in `tools/release-packages.json`", which is a package inventory and not a wire-name register, so the register has no declared home in the repository.

Neither unit violates AD-12: each reserved its name in a register, in the same change, with shape and nullability.

**The concrete incompatibility.** Two registers exist — say `src/Hexalith.Memories.Contracts/V1/wire-names.json` and `docs/contracts/name-register.md`. Unit A reserves `axes` as an object map in the first; Unit B reserves `axes` as an array of contributions in the second. Both changes are admissible under their own register, the collision is discovered at merge, and AD-12's own remedy — "a collision is resolved before merge rather than by a later rename" — is unavailable because both were separately reserved and neither has authority over the other. The escape hatches AD-12 explicitly closes (later rename; versioned break) are the only ones left. This is ADV-09's terminal state, reached through the clause that was written to prevent it.

Secondarily, the register is a rule with no enforcement and no obligation. Line 60 states the spine's own convention — "Where a rule is not yet mechanically enforced, **it says so**; an unenforced rule is still binding but is a weaker guarantee, and the Current Alignment Gaps ledger carries the enforcement obligation" — which AD-8 line 110 honours verbatim ("enforced by an architecture test **still to be added** … its convergence is ledgered"). AD-12 does not say so, and no ledger row at lines 320–336 carries the register. Under AD-20 line 182 the register is therefore a governed requirement with neither an evidence path nor a dated exception.

**Proposed tightening — AD-12 Rule, replacement sentence:**

> "`Hexalith.Memories.Contracts` is the named owning component of `Contracts.V1` and holds sole change-approval authority over its wire surface; the versioned name register is a single tracked file inside that project, every public evidence-bearing wire name, JSON shape and nullability is reserved there in the change that introduces it, and an architecture test — **still to be added**, with its convergence ledgered under AD-20 — fails the build when a serialized wire name is absent from the register or is reserved with a different shape."

---

### ADV2-08 — high — "Byte-comparable across four surfaces" is mandated without a canonical serialization, so G5's golden vector remains unauthorable

**Unit A — REST/Contracts.V1 serializer unit** (`System.Text.Json`, camelCase, declaration-order properties).
**Unit B — CLI JSON formatter unit** (line 193's "machine-readable envelope and exit-code map", `--format json`).

**Compliance proof.** AD-12 line 134 closes the *semantic* hole ADV-08 found and asserts a *syntactic* property it does not define:

> "every element is always present on every surface, an unavailable axis or absent value is an explicit JSON `null` and never an omitted property, an available-with-no-hits axis is an empty collection, no surface enables null-omitting serialization for evidence-bearing types … and **one golden-vector document per case is byte-comparable across REST, Dapr invocation, CLI JSON, and MCP**."

Byte comparison is sensitive to four things the spine never fixes: property order, floating-point rendering, string escaping, and the envelope the document sits inside. Unit A emits `"contribution":0.07692307692307693` in declaration order inside the HTTP response body. Unit B, which must also satisfy line 193's "stable presentation order" for human output and typically shares one formatter, emits `"contribution":0.076923076923076927` (or a rounded display value) with keys sorted, inside a CLI envelope carrying `exitCode` and `command`. Both units carry every mandated element with the mandated null/empty encoding and a single state value with a reason list. Neither violates a sentence.

**The concrete incompatibility.** NFR25's obligation — one golden-vector document per case, byte-comparable across four surfaces — cannot be authored, because there is no compliant single document: two conformant serializers differ in bytes while agreeing in every constrained respect. G5 cannot be closed without an arbitrary tiebreak the spine does not supply, which is precisely the gate-blocking outcome ADV-08 identified; the amendment moved the blockage from *semantics* to *bytes* without removing it. MCP compounds it: a tool result is a typed content block, so the packet is necessarily nested inside a transport envelope that REST does not have, making literal byte-equality of the *response* impossible and leaving unstated which sub-document is compared.

**Proposed tightening — AD-12 Rule, replacement sentence:**

> "Evidence Packet equivalence is byte-equality of one canonical document: properties serialize in the register's declared order, `double` values use round-trippable invariant `R` formatting, strings use minimal JSON escaping, no surface reformats, reorders, or rounds the packet, and every surface embeds that exact byte sequence as its packet payload while its own transport envelope — HTTP body framing, CLI exit-code envelope, MCP content block — is excluded from the comparison; the golden-vector test compares the extracted packet bytes across REST, Dapr invocation, CLI JSON, and MCP."

---

### ADV2-09 — high — AD-20 names no classifier for "active-foundation critical", no issuer of a `confirmed resolved` verdict, and no scope for its own unconditional Rule

**Unit A — Release-gate CI unit** (the epic that mechanizes AD-19/AD-20 on protected releases).
**Unit B — Architecture ledger maintainer unit** (the epic that authors and closes the Current Alignment Gaps rows).

**Compliance proof.** AD-20 line 182, the entire Rule:

> "Every governed requirement and **every gap classified active-foundation critical** carries **exactly one** of a current evidence path or a dated phase exception approved by product and architecture, each with a named owner and a tracker entry. Phase-inactive surfaces earn no gate credit even when their assets exist. A row leaves the ledger only on a **`confirmed resolved` verdict** backed by re-runnable evidence."

Three unowned decisions: *who classifies* a gap as active-foundation critical, *who issues* a `confirmed resolved` verdict, and *when* the Rule binds. Binds at line 180 scopes it to "every **claim** of current qualification"; the Rule sentence is unconditional. Unit A reads the Rule: it is a gate, so it enumerates the ledger and refuses to release while any `Yes` row lacks both an evidence path and an approved dated exception. Unit B reads the Binds: the ledger records obligations and the gate applies only to a qualification claim, so ordinary releases proceed. Both are literal.

**The concrete incompatibility.**

**(a) Every release is blocked, or none is.** Twelve of the seventeen ledger rows (lines 320–336) are marked `Yes` under "Active-foundation critical", and their Disposition column reads "Owner and evidence path owed" — meaning neither an evidence path nor an approved dated exception exists today. Under Unit A no release can be cut at all until twelve rows are resolved; under Unit B releases proceed unchanged. The spine gives no basis to prefer either, and the two units cannot both be shipped.

**(b) The criticality column is a judgment nobody owns.** The `Yes`/`No` split is authored, not derived: line 335's MCP-replicas row is `No` although it is a live workload with no launch-gate flag under an AD-12 phase gate, and line 334's Web row is `No` although its evidence re-run is owed. If Unit A derives criticality mechanically — for instance, any row whose Violated rule names an AD bound to G1–G6 — it reclassifies several rows and blocks on them; if Unit B keeps the authored column, it does not. Both comply with a Rule that presupposes a classification and assigns nobody to make it.

**(c) Two exception vocabularies with different approvers.** AD-20 requires "a dated phase exception approved by **product and architecture**". AD-15 line 152 requires, for the same blocking rows at lines 324, 332 and 333, "a dated, time-bounded **security** exception … on the same footing as the OpenBao version exception". A security exception approved by security and the operator closes AD-15 and not AD-20; a phase exception approved by product and architecture closes AD-20 and not AD-15. The ledger's Disposition strings ("Dated exception or bump owed; blocks Production") do not distinguish them, so one unit closes a row the other still holds open.

**Proposed tightening — AD-20 Rule, replacement sentence:**

> "The architecture owner classifies each ledger row's active-foundation criticality at the revision that adds it and records the classification basis in the row; a row leaves the ledger only on a `confirmed resolved` verdict issued by that owner and backed by re-runnable evidence named in the row. This Rule gates any claim of current qualification and any protected release that asserts a governed gate, not ordinary development builds: a protected release requires that every active-foundation-critical row carry either a current evidence path or a dated exception, and a single dated exception register records both AD-15 security exceptions and AD-20 phase exceptions with their distinct approvers, expiry, and the rows each one closes."

---

### ADV2-10 — high — The active `(schemaGeneration, embeddingConfigurationEpoch)` has a source of truth, a cache, and no coherence contract or single read path

**Unit A — Query axis-selection unit** (AD-10's epoch conjunct at line 122).
**Unit B — Projection coordinator and status read path** (AD-3's "reported public ingestion state" at line 80).

**Compliance proof.** AD-14 line 146 names the fact, its commit path, and a cache — and explicitly declines to make the cache authoritative:

> "The active schema generation and embedding-configuration epoch are tenant-lifecycle domain facts committed through AD-2 before any projection may cite them; **the tenant configuration actor caches and serializes access to them and is never their source of truth**."

Two consumers must read the same fact on every request. AD-10 line 122: "read only resources belonging to **the tenant's active** `(schemaGeneration, embeddingConfigurationEpoch)`". AD-3 line 80: "The reported public ingestion state is always that of **the tenant's active** `(schemaGeneration, embeddingConfigurationEpoch)`."

Unit A reads through the actor, citing AD-4 line 86's rule that "non-reentrant actors serialize only tenant or global stateful concerns" and the actor's stated caching role — the hot path cannot hit EventStore per query. Unit B reads the committed domain fact directly, citing "is never their source of truth" as a prohibition on trusting the cache for a value that gates a public status. Neither violates anything: the spine states that a cache exists and that it is not the truth, and never says which path a consumer must use, when the cache is invalidated, or what staleness bound applies.

**The concrete incompatibility.** At the instant AD-14's activation commits epoch 2, Unit A's actor still holds epoch 1 for up to its cache lifetime while Unit B reads epoch 2 immediately.

- Unit B's status path reports the tenant's public state against epoch 2, for which no unit has yet acknowledged: **every unit in the tenant flips to `indexing`**, the outcome ADV-01(b) was amended to prevent, now reached through the cache rather than through the tuple.
- Unit A's axis-selection continues to treat epoch-1 resources as active, so queries keep serving from resources that are no longer the tenant's active epoch — precisely the condition AD-10 line 122 declares "unsafe" — while reporting `complete`.

Reverse the two units' choices and the failure inverts: search fails closed on every axis for the whole cache window while ingestion status says `indexed`. Both configurations are compliant, and the two are mutually unreadable in one deployment.

**Proposed tightening — AD-14 Rule, replacement sentence:**

> "Every consumer of the active `(schemaGeneration, embeddingConfigurationEpoch)` — AD-3's status projection, AD-10's axis-selection, and every projection activity — resolves it through the tenant configuration actor and through no other path; the actor's cached value is invalidated synchronously as part of the AD-2 activation commit and before that commit is reported complete, so that no two consumers observe different active epochs for one tenant at one instant, and a consumer that cannot resolve the value fails closed rather than assuming the previous epoch."

---

### ADV2-11 — medium — The candidate depth is "server-owned" but not stated to be deployment-wide, and AD-18 invites a per-tenant value

**Unit A — Search configuration unit** (owns the AD-9 candidate depth as validated options).
**Unit B — Capacity and admission unit** (AD-18's tenant partitioning).

**Compliance proof.** AD-9 line 116: "Each axis contributes a **fixed, server-owned, configuration-validated candidate depth identical across every surface**." The invariant is stated across *surfaces*, not across *tenants* or *time*. AD-18 line 170 makes per-tenant differentiation the norm: "Give tenant and global quotas separate owners, use **tenant-partitioned** admission/concurrency and bounded durable queues … Numeric budgets live in the PRD and validated configuration."

Unit A ships one deployment-wide value. Unit B ships a per-tenant validated option, which is still server-owned (the caller cannot set it), still configuration-validated, and still identical across REST, CLI, MCP and Dapr invocation for a given tenant — every stated conjunct holds.

**The concrete incompatibility.** Two tenants with byte-identical corpora and the same query return different composite scores and different result sets, because a shallower depth changes competition-rank membership and therefore `1/(10+rank)` for every surviving unit. G1's recall protocol becomes a property of the tenant it runs in, and NFR25's "identical results across runs and surfaces" is satisfied while the underlying determinism claim is not. The depth is also absent from AD-12's mandated Evidence Packet elements, so a caller cannot tell which regime produced the answer.

**Proposed tightening — AD-9 Rule, replacement sentence:** "The candidate depth is a single deployment-wide validated value that is identical across every surface, tenant, caller, page, and load condition, is never reduced by admission control or backpressure — AD-18 rejects or delays work rather than shrinking the candidate set — and is reported in the Evidence Packet so that any two answers produced under different depths are distinguishable."

---

### ADV2-12 — medium — Deep paging past the fused list's end has two compliant behaviours

**Unit A — REST search API unit** (FR22 pagination).
**Unit B — Evidence Packet degradation unit.**

**Compliance proof.** AD-9 line 116 bounds the fused list by candidate depth ("fusion is defined over each axis's complete server-limited candidate list") and applies the caller's offset only afterwards ("The caller's result limit and offset apply only to the fused output after rank allocation"). Nothing states what happens when the offset exceeds the fused list's length. Unit A returns an empty page, which is truthfully "the head of the single fused ordering" exhausted. Unit B reports the axes as `truncated` because the caller reached the depth boundary, and discloses degradation. Both comply.

**The concrete incompatibility.** For a 100K-unit tenant with depth 100, page 4 is empty under Unit A with an Evidence Packet state of `complete` and `empty` — indistinguishable from "no more matches exist" — while Unit B returns the same empty page with state `degraded` and a truncation disclosure. A client paging to the end therefore cannot tell "you have seen everything" from "the server stopped looking", which is the deception AD-10 line 121 exists to prevent, reached without violating a rule.

**Proposed tightening — AD-9 Rule, added sentence:** "The fused ordering is finite and bounded by the candidate depth; a request whose offset reaches or exceeds its length returns an empty page whose Evidence Packet distinguishes exhaustion of the corpus from exhaustion of the candidate depth, and the packet always reports the fused list's total length and whether that length was depth-bounded."

---

### ADV2-13 — medium — AD-2's new checkpoint invalidation creates a checkpoint state AD-3 does not define

**Unit A — Derived-store restore unit** (AD-2's second recovery operation).
**Unit B — Ingestion status read path** (AD-3's persisted status).

**Compliance proof.** AD-2 line 74, new in this amendment: "derived-store restore is an availability optimization that **must invalidate the affected AD-3 checkpoints** and re-verify through AD-3's protocol". AD-3 line 80 requires monotonic advancement and persistence, and forbids the fallback the ledger row at line 321 records as today's bug: "**Persist the status rather than inferring it from artifact presence.**" It defines no state for a checkpoint record that was deliberately removed, and its only other deletion — AD-14's retire step — removes records for a retired epoch, not for the active one.

Unit A deletes the checkpoint records for the restored key range, as instructed. Unit B must then answer `GET /memory-units/{id}` with no persisted status and is forbidden to infer one from the presence of the restored artifact. Unit B reports `queued` (no projection work is recorded for this tuple); an equally compliant Unit B' reports `indexing` (re-verification is pending). Neither infers from artifact presence; neither violates monotonicity.

**The concrete incompatibility.** After a derived-store restore of a 100K-unit tenant, the tenant reports 100K units `queued` under one reading and 100K units `indexing` under the other. FR10's per-state counts and FR31's case status differ by the whole corpus; NFR36's `pending`→`indexed` budget is breached tenant-wide under the first reading and not the second; and an operator cannot distinguish "restore in progress" from "ingestion never ran".

**Proposed tightening — AD-3 Rule, added sentence:** "Invalidation writes an explicit `reprojectionRequired` checkpoint record for the affected tuple rather than deleting it; that record reports the public ingestion state as `indexing` with a stated reprojection reason, does not violate `sourceVersion` monotonicity, and is cleared only by a complete all-axis acknowledgement of the same tuple — an absent checkpoint record continues to mean no projection work has ever been recorded."

---

### ADV2-14 — medium — The injective tenant→telemetry mapping is both a purge target and a retention requirement

**Unit A — Tenant erasure workflow** (AD-16).
**Unit B — Telemetry purge-accounting unit** (AD-17).

**Compliance proof.** Consistency Conventions line 197, rewritten in this amendment: "The approved tenant representation in metrics and logs is a documented, stable, **injective mapping recorded at issuance**; a collision is a provisioning error, not an accepted cardinality trade-off." AD-16 line 158 requires purging "**every store that can hold tenant content or content-derived material** … and derived artifacts". AD-16 also holds telemetry back: "Deletion does not wait for or force early deletion of retained opaque access telemetry: those records follow AD-17's bounded TTL/purge contract."

Unit A treats the mapping as a per-tenant derived artifact created at issuance and purges it — a tenant identifier is tenant-identifying, and AD-6 line 96 puts "the access-telemetry partition" under lifecycle ownership. Unit B retains it, because AD-17 line 164 requires "observable purge progress" over records that survive the deletion by design, and a purge-progress report keyed to an unresolvable token is not observable. Both readings are supported.

**The concrete incompatibility.** Under Unit A, retained telemetry rows for the erased tenant carry a token with no mapping: AD-17's observable purge progress cannot be reported per tenant, and the operator evidence FR39 expects for verified erasure cannot name what it purged. Under Unit B, the mapping survives indefinitely alongside the register, so the erased tenant remains resolvable from metrics and logs after an erasure that AD-16 line 157 promises is irreversible. One unit breaks telemetry accounting and the other weakens the erasure claim, with no rule choosing.

**Proposed tightening — AD-16 Rule, added sentence:** "The tenant-to-telemetry-representation mapping is content-free and is retained in the erased-tenant register for as long as any record bearing that representation is retained, then purged with the last such record; it is never purged ahead of them and never retained beyond them, and its retention is disclosed as part of erasure completion evidence."

---

### ADV2-15 — medium — Aspire owns the digests; the Kubernetes manifests and the Stack table are bound to nothing

**Unit A — `Hexalith.Memories.Aspire` integration unit.**
**Unit B — `deploy/kubernetes` manifest unit.**

**Compliance proof.** AD-19 line 176: "`Hexalith.Memories.Aspire` is the single owner of qualified container image digests and **AppHost** consumes those same defaults rather than declaring its own." The obligation names exactly one consumer. Consistency Conventions line 198 requires "the same pinned image digests" across environments but names no source. The Stack table at lines 214–231 records *tags and versions* — `7.4.0-v8`, `4.12.0` — not digests, and only the PostgreSQL row says "digest-pinned"; line 232 says the entries are "exact repository pins read from the **committed** `references/Hexalith.Builds` gitlink".

Unit A pins digest `sha256:A` for `redis-stack-server:7.4.0-v8`. Unit B pins `sha256:B` for the same mutable tag in `deploy/kubernetes`, having resolved it at a different date. Both are "pinned qualified container defaults"; AD-19 binds only AppHost to Aspire's set, and line 198's "same pinned image digests" is satisfied across *environments* by Unit B's own manifests being identical to each other.

**The concrete incompatibility.** Integration evidence for both AD-19 lanes is produced against `sha256:A` while Production runs `sha256:B`. Line 232's warning makes this concrete rather than theoretical: `7.4.0-v8` "is the terminal tag of its ended-maintenance line, unrebuilt since 2025-11-03" — so the two digests differ only if the registry re-pushed the tag, which is exactly the event an unrebuilt terminal tag makes silent. Every AD-19 qualification claim then describes a build Production is not running.

**Proposed tightening — AD-19 Rule, replacement sentence:** "`Hexalith.Memories.Aspire` is the single owner of qualified container image digests, and every consumer of a container default — AppHost, `deploy/kubernetes`, CI, and integration harnesses — resolves images from that one digest set; the Stack table records the digest alongside the tag for every qualified image, and both release lanes produce their integration evidence against that recorded digest set."

---

### ADV2-16 — low — The Direct Redis Exception Registry's only row declares no reserved key prefix, so the new prefix rule governs an empty set

Line 210 now ends: "**Every row declares its reserved Redis key prefix, which no other row may claim.**" The single row at line 208 declares none — its Scope column reads "Key includes tenant/case and canonical request identity" — so the rule protecting the Redis key namespace protects nothing today, while line 326's ledger records five coordination families ("permanent dedup, failed-unit registries, import leases, derived-store fences, and migration state") already occupying prefixes no row reserved. A future `AD-n` author who obeys the new rule to the letter can still claim a prefix the preflight store is using. The clause also never fixes the granularity of "the protected resource", so two rows can describe the same Redis index at different altitudes ("tenant `T`'s derived index" versus "the import operation for case `C`") and never trip the already-guarded test that ADV-10's fix installed.

**Proposed tightening — Registry closing paragraph, added sentence:** "The `IPreflightDedupStore` row reserves the prefix `memories:preflight:`; a row's protected resource is named at the granularity of the durable artifact it guards — a store, index, graph, or key family — never at the granularity of an operation, and the unregistered coordination key families recorded in the alignment ledger are migrated or given rows with reserved prefixes as part of closing that row."

---

### ADV2-17 — low — The axis-state vocabulary has four names and a three-state enumeration

AD-9 line 116 closes the set at three — "An axis is `unavailable` …, `available` …, or `truncated` … the Evidence Packet distinguishes **all three**" — and then, two sentences later, introduces a fourth for an inactive NL axis: "where NL is inactive it is reported as an **excluded** axis rather than omitted". AD-10 line 122 uses the four-name form: "list unavailable/**excluded**/truncated axes". Under AD-12 line 134 the axis state is a wire value that must be reserved with its shape in the name register, so the closed set matters: the Contracts owner reserving a three-value enum and the CLI/MCP presenter obliged by line 193 to publish "a text label for every state" need the same closed set, and the spine states two. A surface that maps `excluded` onto `unavailable` (null) also loses the distinction line 116 demands between an axis that could not answer and one that was never selected.

**Proposed tightening — AD-9 Rule, replacement clause:** "An axis is in exactly one of four states — `unavailable`, `available`, `truncated`, or `excluded` — the Evidence Packet distinguishes all four on every surface, and the four values with their packet shapes are reserved in AD-12's name register as a closed set that a new axis state may extend only by an additive registered change."

---

## Gate disposition

The amendment is a net large improvement: it closed all eleven demonstrated artifacts, four of them completely and seven down to narrower residues, and it added the ownership language the spine was missing in five places. Nothing here reopens an adopted decision; all seventeen are closable by replacing or adding a Rule sentence in place, and eleven of the seventeen are single-sentence edits to text written on 2026-09-12.

Two findings should be treated as errata rather than review findings, because the current text cannot be implemented at all:

- **ADV2-01** — AD-9 line 116 and AD-10 line 122 give a truncated axis opposite dispositions. Until one is corrected, every fusion implementation is non-compliant with one of them, and the ~1.54× composite-score divergence ADV-05 closed is live again.
- **ADV2-03** — AD-14's newly ratified MVP degraded rebuild has no epoch that AD-3's write fence can compare, and MVP has no staging tuple for it to occupy. The ratified decision (frontmatter lines 11–14) is sound; its interaction with AD-3's simultaneous amendment is not.

Sequencing for the rest: **ADV2-02, ADV2-04, ADV2-05, ADV2-06** should close before the affected epics are sprint-selected — they are the axis-safety contract, the erasure register's store and read paths, the identifier admission boundary, and key composition, each of which two independently staffed epics will hit on contact. **ADV2-07 and ADV2-08** block G5 evidence independently of implementation quality and should close alongside the FR34/FR19 contract work, not after it. **ADV2-09** should close before the next protected release, since the two readings of AD-20 differ on whether any release may be cut at all. **ADV2-10** should close with the AD-3 checkpoint epic, since it defeats that epic's own guarantee through the cache. The five medium and two low findings are safe to batch into the next spine revision.

**Recommendation: PASS-WITH-FINDINGS.** Adopt the two errata corrections (AD-9/AD-10 and AD-14/AD-3) immediately, the six critical/high clauses before the corresponding epics are selected, and re-run this lens against the corrected text — the amendment history now shows two rounds in which a fix for one pair created a pair in the adjacent AD, so a third adversarial pass on the corrected sentences is warranted rather than optional.
