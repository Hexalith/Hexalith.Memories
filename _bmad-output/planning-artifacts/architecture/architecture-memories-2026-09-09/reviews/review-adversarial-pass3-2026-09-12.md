# Adversarial Divergence — Pass 3 (Second-Amendment Retest), 2026-09-12

**Verdict: PASS-WITH-FINDINGS — of the 17 pass-2 pairs, 4 closed, 5 partially closed, 8 still open; of the 7 pass-2 partial closures, 4 closed and 3 still open. 15 new divergence pairs (4 critical, 5 high, 4 medium, 2 low). The second amendment repeated the pass-2 residue pattern and added a second signature: three of its four highest-value fixes are scoped so narrowly that they un-cover the general case they were derived from.**

**Artifact (read-only):** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` — 363 lines, 20 ADs, `status: final`, `updated: 2026-09-12`. Every line number below is a line of that file at this revision.
**Baseline compared against:** the 353-line first-amendment spine and `reviews/review-adversarial-retest-2026-09-12.md` (pass 2).
**Lens:** two implementation units one level down that each obey every AD to the letter and still build incompatibly.

## Method and discipline

Unchanged from pass 2. Every finding names two units that different epics could plausibly staff, quotes the exact Rule text and line number each relies on, proves that **neither unit violates the AD it implements**, exhibits the concrete incompatible artifact, and proposes replacement Rule text. Candidates requiring one unit to break a Rule were discarded as implementation bugs. Nine were dropped this pass because the second amendment now forbids them outright: a truncated axis being nulled in the graph case (L122 now states it is safe and denominator-weighted); an axis reporting depth exhaustion as truncation (L116 "Reaching the configured per-axis candidate depth is **not** truncation"); an active and a staging write alternating in one document (L80); a `Contracts.V1` owner going unnamed (L134); a registry row without a reserved prefix (L208); the erased-tenant register living nowhere (L158); a register that materializes on first erasure (L158); an AD-20 reading that blocks ordinary development builds (L182 now scopes the obligation to "before it may be cited as qualification evidence"); and telemetry retention accounting being deadlocked by its own tenant's erasure (L164).

One finding is reported as an **erratum** rather than a divergence, because the contradiction is internal to a single AD and therefore admits no compliant implementation of that AD alone — it is not a two-unit pair.

The amendment is again materially good. It closed the two errata pass 2 raised as blocking, named the register's store, named the contracts owner, gave AD-20 a criticality definition and a scope, gave every ledger row an owner and a date, and added three ledger rows that did not exist. It is also the second consecutive amendment in which the fix for one pair created a pair in the adjacent AD.

---

## Part A — Retest of the pass-2 findings

### A.1 — The 17 new pairs from pass 2 (ADV2-01 … ADV2-17)

| ID | Pass-2 failure | Verdict | Evidence in the amended text | Surviving variant |
| --- | --- | --- | --- | --- |
| **ADV2-01** | `truncated` carries denominator weight in AD-9 and is nulled by AD-10 | **Closed** (erratum E1 flagged) | L116 keeps `truncated` as "available, carrying its hits, **contributing denominator weight**". L122 now agrees explicitly: "a graph fan-out that completed some but not all case partitions with scope verified on each is AD-9's `truncated` state, **which is safe, disclosed, and contributes denominator weight**." The 1.538× denominator divergence is dead for the graph axis. | None as a divergence. **E1:** AD-10's biconditional at L122 still reads "safely **if and only if** its adapter … returned within AD-11's or the axis's configured limits **without truncation**", which the same Rule then contradicts three clauses later. No unit can implement AD-10 as written; this is an intra-AD drafting defect, not a two-unit pair. Strike "without truncation" from the biconditional. |
| **ADV2-02** | Candidate depth is both "the complete list" and "an exhausted limit"; `truncated` has no non-graph meaning; no adapter reporting contract | **Partially closed** | (a) closed outright and in both ADs: L116 "Reaching the configured per-axis candidate depth is **not** truncation: the depth-limited list is the complete candidate list for that axis"; L122 "reaching a configured candidate depth **are safe** and are disclosed as freshness, emptiness, or depth". The total-query-failure artifact for large tenants is dead. | (b) **inverted, not closed.** L116 now restricts the state — "`truncated` applies **only** when a selected graph case partition did not complete" — so the syntactic, semantic and `nl` axes now have *no* state for a scope-verified limit exhaustion. See **ADV3-02**, which is the same 1.4–1.5× denominator divergence relocated to the syntactic axis. (c) **encoding still absent**, and now falsely cross-referenced: L116 says the packet "distinguishes all three **under AD-12's encoding rules**", while L134 defines exactly two encodings (`null`, empty collection). (d) **the adapter→server reporting contract is still unstated**: L122 requires the server to decide safety from scope-applied and truncation facts that only the adapter observes, and obliges no adapter to report either. |
| **ADV2-03** | MVP degraded rebuild has no epoch, no staging tuple, and no fence comparison across epochs | **Partially closed** | The fence relation is now supplied at L80: "a write whose generation or epoch differs from the stored document's is **never merely older** — it replaces the document when it carries the tenant's active generation and epoch, and otherwise targets AD-14's disjoint staging resources rather than the active document, **so an active and a staging write can never alternate in one document**." The flip-flop artifact and the write-nothing artifact are both dead. | **The redirect target does not exist in the phase where the fence fires, and the MVP epoch semantics remain bivalent.** L80's patch sentence — "A Phase 1 / MVP degraded rebuild under AD-14 has one active epoch and writes under it" — is satisfiable two incompatible ways, and AD-14 defines staging resources only for Phase 2 embedding/index-schema changes, never for the graph store and never for Phase 1. See **ADV3-03**. |
| **ADV2-04** | Register had one writer, two possible stores, and a read obligation on one of three consumers | **Partially closed** | L158 names the store — "It **lives in Dapr state under AD-8**, in a platform-scoped component outside any tenant-scoped keyspace" — adds the durability posture requirement, the restore-rollback fail-closed, existence "from platform provisioning rather than from the first erasure", indefinite retention, unavailability fail-closed for deletion and restore, and the missing provisioning read: "**AD-6 tenant provisioning consults it before issuing a tenant ID**, so an erased ID cannot be re-provisioned." Ledger row L341 now carries the register with an owner, a date, and `Yes`. The tenant-ID-reuse artifact is dead. | **Nobody creates it, provisioning has no failure posture, and "unavailable" is indistinguishable from "empty"** (**ADV3-01**). **The completion record still has two homes** — "in the register" (Dapr state) and "domain evidence under AD-2" (EventStore) (**ADV3-08**). **"Platform-scoped" has no scope** across deployments, DR clusters, or a replacement of the very state component the Deferred table says must be replaced (**ADV3-12**). **"A state older than the data being admitted"** has no age primitive (**ADV3-13**). The **replay** consumer still carries authority without an explicit read obligation — only AD-2 L74's generic "AD-16's erasure constraints override all three" reaches it. |
| **ADV2-05** | Grammar names no issuer, no validation point, no grandfathering, and does not reach already-issued identifiers | **Partially closed** | The grammar is strengthened at L92 — now **single-case** ("so no downstream system that folds case can collapse two distinct identifiers into one") and extended to exclude characters significant in **SQL identifiers, OpenBao secret paths and object-storage paths** as well. A tenant-ID issuer is implied for the first time by L158 ("before **issuing** a tenant ID" is assigned to AD-6 provisioning). Ledger row L339 now carries the legacy/import gap with an owner, a date, and `Yes`. | **No Rule-level admission-boundary obligation exists**; the requirement to "validate on read at every trust boundary including import" appears only in a ledger *convergence* cell (**ADV3-15**). Case IDs and `MemoryUnitId` still have no named issuer. **"Single-case" does not say which case**, and the grammar and its delimiter are still not written down anywhere in the spine or located in any document (**ADV3-09**, **ADV3-10**). |
| **ADV2-06** | Delimiter required but never declared; non-grammar key components unconstrained | **Still open** | L92 tightened the wording only — "using a **single delimiter declared with the grammar and reserved out of it**" (was "a delimiter reserved out of that grammar"). AD-4 L86 is unchanged: "exact validated `source`, and `id` … no post-validation normalization". | The whole pair survives, sharpened by the new wording: the delimiter is declared "with the grammar" and the grammar is nowhere in the artifact. The `("app\|x","7")` / `("app","x\|7")` dedup collision that silently suppresses a real ingest is unchanged and live. See **ADV3-09**. |
| **ADV2-07** | `Contracts.V1` owner required to be named and never named; register homeless and unenforced | **Closed** | L134 names it: "**`Hexalith.Memories.Contracts` is the single owning component for `Contracts.V1`** and its maintainers hold change-approval authority over that wire surface". Ledger row L342 now carries the missing register and guard, satisfying L60's "the rule **or its ledger row** says so" convention (L60 was itself amended to permit ledger-side disclosure). The two-registers artifact is dead. | Two residues, both promoted to new pairs rather than counted here: the row is classified `No` against AD-20's own new definition (**ADV3-06**), and a **second** `/v1/...` contract surface with independent, non-packable contracts sits outside the owner and the register (**ADV3-11**). |
| **ADV2-08** | "Byte-comparable" mandated without a canonical serialization | **Still open** | L134 is unchanged: "one golden-vector document per case is **byte-comparable** across REST, Dapr invocation, CLI JSON, and MCP." No property order, no `double` format, no escaping rule, no envelope-exclusion rule was added. | The entire pair survives verbatim. Two conformant serializers still differ in bytes while agreeing in every constrained respect, and the MCP content-block envelope still makes literal response-level byte equality impossible. G5's golden vector remains unauthorable. |
| **ADV2-09** | AD-20 has no classifier, no verdict issuer, and an unscoped Rule | **Partially closed** | (a) closed: L182 now scopes the obligation — "must carry, **before it may be cited as qualification evidence**, exactly one of …" — and adds the missing definition: "A gap is **active-foundation critical** when it violates a rule binding an MVP-active surface and its violation can produce incorrect product behaviour, cross-tenant exposure, irreversible data loss, or an unverifiable gate claim". L345 now states which rows are in the blocking state. The block-every-release reading is dead. | (b) **survives and is now demonstrable**: a predicate exists, nobody is named to apply it, and applying it mechanically contradicts the authored column on at least three rows (**ADV3-06**). (c) **two exception vocabularies survive unchanged**: L182's "dated phase exception approved by product and architecture" versus L152's "dated, time-bounded **security** exception", with ledger dispositions that name neither. The `confirmed resolved` verdict still has no issuer. And L345's closing attestation now conflicts with L182's conjuncts (**ADV3-05**). |
| **ADV2-10** | Active `(schemaGeneration, embeddingConfigurationEpoch)` has a cache, no coherence contract, no single read path | **Still open** | L146 is unchanged: "the tenant configuration actor caches and serializes access to them and **is never their source of truth**." No invalidation point, no staleness bound, no mandated read path. | The entire pair survives verbatim, and **ADV3-03** compounds it: an MVP rebuild that activates a new epoch now propagates through this uninvalidated cache. |
| **ADV2-11** | Candidate depth "server-owned" but not stated deployment-wide | **Still open** | L116 unchanged: "a fixed, server-owned, configuration-validated candidate depth **identical across every surface**." The invariant is still stated across surfaces only, while AD-18 L170 still makes tenant partitioning the norm. | Survives verbatim. |
| **ADV2-12** | Deep paging past the fused list's end has two compliant behaviours | **Still open** | No sentence was added to AD-9 or AD-12 about offset overrun or fused-list length. | Survives verbatim, and is now slightly worse: with `truncated` restricted to the graph axis, the Unit-B disclosure route the pass-2 finding assumed is no longer available for a depth-bounded syntactic list. |
| **ADV2-13** | AD-2's checkpoint invalidation creates a state AD-3 does not define | **Still open** | L74 unchanged: derived-store restore "must invalidate the affected AD-3 checkpoints and re-verify through AD-3's protocol". L80 still requires "Persist the status rather than inferring it from artifact presence" and defines no state for a deliberately removed record. | Survives verbatim — the 100K-units-`queued` versus 100K-units-`indexing` artifact is unchanged. |
| **ADV2-14** | Injective tenant→telemetry mapping is both a purge target and a retention requirement | **Closed** | L164 adds the missing authority: "Retention accounting for an erased tenant — TTL expiry, purge progress, and the derived erasure mapping — **continues under Platform Operations' own authority**", and AD-16's purge scope is bounded to stores holding "tenant **content or content-derived material**", which a content-free mapping is not. Retention is now the single answer. | None for that pair. The fix imports a new one: L164's exemption is not among AD-5's two stated exceptions (**ADV3-07**). |
| **ADV2-15** | Aspire owns the digests; `deploy/kubernetes` and the Stack table are bound to nothing | **Still open** | L176 unchanged: "`Hexalith.Memories.Aspire` is the single owner of qualified container image digests and **AppHost** consumes those same defaults". The Stack table still records tags, not digests, for every row but PostgreSQL. | Survives verbatim. |
| **ADV2-16** | The only registry row declares no reserved prefix | **Closed** | L208's Scope column now reads "Reserved key prefix `memories:preflight:`; the key includes tenant/case and canonical request identity". The prefix rule now governs a non-empty set. | Residue only: "the protected resource" still has no granularity rule, so two rows can describe one Redis artifact at different altitudes and never trip the already-guarded test. Low; folded into **ADV3-15**'s ledger observation and not re-argued. |
| **ADV2-17** | Four axis-state names, a three-state enumeration | **Still open, aggravated** | L116 unchanged at "the Evidence Packet distinguishes **all three**" while still introducing `excluded` two sentences later; L122 unchanged at "list unavailable/**excluded**/truncated axes". | Survives, and the amendment made it worse by appending "**under AD-12's encoding rules**" to the three-state clause: AD-12 L134 supplies two encodings for what the spine names as four states, so the closed set the Contracts owner must reserve and the CLI presenter must label is stated three ways in three places. |

**A.1 counts: 4 Closed · 5 Partially closed · 8 Still open.**

### A.2 — The seven pass-2 partial closures (ADV-02 · 03 · 04 · 05 · 08 · 09 · 10)

| ID | Residue carried into this pass | Verdict | Evidence |
| --- | --- | --- | --- |
| **ADV-02** | Register's store unnamed; read obligation on restore only | **Closed** | Store named at L158 ("lives in Dapr state under AD-8"); provisioning read obligation added at L158. Replay's obligation remains implicit via AD-2 L74's blanket override — thin, folded into **ADV3-08**. |
| **ADV-03** | Grant membership at provisioning unspecified; no writer for a grant on a live tenant; revocation has no propagation bound | **Still open** | L92 is unchanged word for word: "The per-tenant grant that pairs with it is a tenant resource whose sole writer is AD-6's lifecycle workflow, **created only by verified provisioning** and revoked as a completion condition of AD-16 erasure." Read strictly this still forbids adding an app ID to an existing tenant, and the allowlist — now a secret under AD-15's operator scope — still gets none of the propagation bound AD-15 L152 gives rotated credentials. |
| **ADV-04(b)** | `truncated` contradicted across AD-9/AD-10 | **Closed** | See ADV2-01. |
| **ADV-05** | Safety predicate evaluated at an unstated time from facts only the adapter holds; no reporting contract | **Still open** | L122 still places axis-selection before the query ("Query every selected usable axis") while conditioning safety on post-hoc facts, and still obliges no adapter to report scope-applied, truncation, or the uncompleted-partition count AD-9 requires in the packet. |
| **ADV-08** | "Byte-comparable" with no canonical form | **Still open** | See ADV2-08. |
| **ADV-09** | Owner required and never named; register homeless and unenforced | **Closed** | Owner named at L134; register and guard ledgered at L342, which L60's amended convention accepts. |
| **ADV-10(c)** | Only registry row declares no prefix; "protected resource" has no granularity | **Closed** for the prefix | Prefix declared at L208; granularity residue is low and unchanged. |

**A.2 counts: 4 Closed · 3 Still open.**

### A.3 — Did the second pass repeat the pass-2 residue pattern?

**Yes, and it added a second signature.**

The pass-2 pattern — *the amendment fixes the behaviour and under-specifies the mechanism* — recurs in four places, each time in text written on 2026-09-12:

- It named the register's **store** and not its **creator** (ADV3-01).
- It named the fence's **redirect target** and not the **resource** that target denotes in the phase where the fence fires (ADV3-03).
- It named an **exception** ("revalidate against the tenant lifecycle state machine") and did not define the **machine** (ADV3-04).
- It defined the **criticality predicate** and named nobody to **apply** it (ADV3-06).

The new signature is sharper and is the more instructive one: **three of the four highest-value fixes were scoped to the exact case the prior finding exhibited, and the narrowing un-covered the general case.**

- ADV2-02 exhibited the depth-versus-truncation conflict on the syntactic axis. The fix declared depth safe *and* restricted `truncated` to graph case partitions — leaving the syntactic axis with no state at all for a genuine limit exhaustion. The divergence did not close; it moved (**ADV3-02**).
- ADV2-03 exhibited an epoch flip-flop in one document. The fix supplied the cross-epoch relation *and* routed the non-active write to "AD-14's disjoint staging resources", which AD-14 defines only for Phase 2 and only for embedding and index-schema changes. The flip-flop closed; the redirect now points at nothing in Phase 1 and nothing at all for the graph store (**ADV3-03**).
- ADV2-04 exhibited a reused tenant ID. The fix added the provisioning read *and* a fail-closed clause enumerating "deletion and every restore path" — omitting the one consumer it had just added (**ADV3-01**).

A fourth pass should therefore test each proposed sentence against the *complement* of the exhibited case before adoption, not only against the case itself.

---

## Part B — New divergence pairs against the second-amendment text

Severity-ordered. Fifteen pairs: 4 critical, 5 high, 4 medium, 2 low.

---

### ADV3-01 — critical — Nobody creates the erased-tenant register, provisioning has no failure posture when it is missing, and an unavailable register is indistinguishable from an empty one

**Unit A — Platform deployment and bootstrap unit** (`deploy/kubernetes`, Dapr components, AppHost composition; the epic that stands the platform up).
**Unit B — Tenant provisioning unit** (AD-6's lifecycle workflow, now obliged by AD-16 to consult the register).

**Compliance proof.** AD-16 line 158 makes the register pre-exist every erasure and assigns its writes, but never its creation:

> "**It exists from platform provisioning rather than from the first erasure**, carries the content-free non-reuse tombstone with no TTL and no purge path, is retained indefinitely, and is **written and owned by the erasure workflow**".

"Platform provisioning" appears nowhere else in the spine. `bootstrap` appears only at lines 150 and 152, in AD-15, and there it means secret material ("deployments provide only minimum **bootstrap material**"). AD-6 line 96 owns "**Tenant** provisioning, verification, repair, deletion, indexes, graphs, backend identities and credentials, per-tenant grants, the access-telemetry partition, and cleanup" — a tenant-scoped list that does not include a platform-scoped register. AD-19 owns packaging and release. No AD owns platform provisioning, and the one workflow that is named as the register's owner is expressly not the thing that brings it into existence.

The failure posture is then enumerated without the consumer the same amendment added, in two consecutive sentences:

> "**When the register is unavailable, deletion and every restore path fail closed** and remain resumable rather than proceeding. **AD-6 tenant provisioning consults it before issuing a tenant ID**, so an erased ID cannot be re-provisioned."

Unit A complies: no Rule instructs it to create a Dapr state key, and Dapr state stores have no create-time schema — a state component with no keys written is a fully provisioned component. Unit B complies: it consults the register before issuing a tenant ID exactly as instructed, and AD-6 line 98 requires its workflow to be "explicit, idempotent, observable, **bounded**" — bounded forbids blocking indefinitely on an absent dependency, and no sentence assigns provisioning a fail-closed posture.

**The concrete incompatibility.**

**(a) First-ever provisioning deadlocks, or the fail-closed rule is defeated — and both are compliant.** On a fresh cluster the register has never been written. Unit B issues `GET` against the platform-scoped state key and receives an empty response. Two literal dispositions:

- *Fail-closed reading* (by analogy with the deletion/restore sentence and with AD-5's "an absent app ID fails closed with no default principal"): an absent register is an unavailable register, so provisioning fails closed. Nothing creates the register — the erasure workflow that owns its writes cannot run before a tenant exists — so **no tenant can ever be provisioned on a new deployment**. Local AppHost, CI, integration and Production all fail identically at their first `tenant create`, and the failure is a compliant implementation of two rules.
- *Empty reading*: an empty register contains no tombstones, so no tenant is erased, so provisioning proceeds. This is also the behaviour when the state component is **down**, misconfigured, pointed at the wrong Redis database, or restored from a backup that predates every erasure — because Dapr state returns "no value" for all of these. AD-16's "when the register is unavailable … fail closed" then protects deletion and restore and silently exempts the exact path the amendment added it for, and the tenant-ID-reuse artifact ADV2-04(a) exhibited is live again with a different cause.

**(b) The two readings cannot coexist in one deployment.** If Unit A ships the empty reading in the provisioning path and Unit B ships fail-closed in the restore path (both compliant), the platform admits `acme-legal` as a new tenant and simultaneously refuses to restore `acme-legal`'s export bundle — the same identifier is both free and erased, decided by two paths reading one store. AD-16's "sole authority" is satisfied literally: one store answered both, and the two callers disagreed about what its silence means.

**Proposed tightening — AD-16 Rule, replacement sentences:**

> "The erased-tenant register is created and initialized by a named platform-provisioning step that runs before any tenant may be provisioned and writes an explicit initialized marker carrying a monotonic register sequence; that step is a deployment-time obligation owned alongside AD-15's bootstrap material and is verified by readiness under the Consistency Conventions' Health row. The register is **available** only when that marker reads back; an absent marker, an unreachable component, and any read error are all **unavailable** and are never interpreted as an empty register. When the register is unavailable, **tenant provisioning, deletion, and every restore path** fail closed and remain resumable rather than proceeding."

---

### ADV3-02 — critical — `truncated` is now graph-only, so a syntactic, semantic or `nl` axis that exhausts a limit with scope verified has no state, and the two literal dispositions differ by the full axis weight

**Unit A — Redis syntactic/vector adapter and axis-selection integration unit** (`Adapters.Redis`; the epic closing the L326 ledger row).
**Unit B — Fusion and Evidence Packet unit** (AD-9's weighted sum and AD-12's packet).

**Compliance proof.** AD-9 line 116, amended this pass, closes `truncated` to the graph axis:

> "`truncated` applies **only when a selected graph case partition did not complete** within AD-11's depth, result, or time limits while tenant and case scope were verified on the partitions that did complete; where scope itself could not be verified the axis is unsafe under AD-10 and is nulled instead."

AD-10 line 122 enumerates a safe set and an unsafe set, and its unsafe set is qualified:

> "**Staleness, low recall, empty results, and reaching a configured candidate depth are safe** and are disclosed as freshness, emptiness, or depth; **unverified scope and a generation or epoch mismatch are unsafe** and null the axis. **A limit exhausted such that the applied scope cannot be verified is unsafe**; a graph fan-out that completed some but not all case partitions with scope verified on each is AD-9's `truncated` state, which is safe, disclosed, and contributes denominator weight."

AD-10 line 122's opening biconditional also contains, unamended, the conjunct "returned within AD-11's **or the axis's configured limits** without truncation" — establishing that non-graph axes have configured limits that can be exhausted. They do in practice: RediSearch honours a query `TIMEOUT` and returns a partial result set with scope correctly applied, and AD-18 line 170's tenant-partitioned admission and provider throttling make partial returns under load the expected case, not an exotic one.

Now take a syntactic axis that applied the tenant and case scope correctly, read only active-epoch resources, and hit its configured time limit after scanning 60% of the tenant's index, returning 40 of a possible 100 candidates. Classify it:

- It is not `unavailable`: it applied scope and returned hits.
- It cannot be `truncated`: line 116 says the state applies **only** to an uncompleted graph case partition.
- Under the *enumerated* safe/unsafe sets it is safe: it is not "unverified scope", not a "generation or epoch mismatch", and not "a limit exhausted **such that the applied scope cannot be verified**" — the scope *was* verified. It is "low recall", which line 122 names safe.
- Under the *biconditional* it is unsafe: it did not return "within … the axis's configured limits without truncation".

Unit A implements the enumerated sets, which are the operative sentences and the ones the amendment wrote: the axis is `available`, carries its 40 hits, and takes full denominator weight. Unit B implements the biconditional, which is the definitional "if and only if": the axis is nulled. Neither unit violates the AD it implements; the amendment removed the only state that could have carried the third answer.

**The concrete incompatibility.**

**(a) The exact denominator divergence ADV-05 and ADV2-01 closed, relocated to the syntactic axis.** One query, one tenant under load, syntactic axis times out with scope verified:

- Unit A: syntactic `available` with 40 hits. Denominator `0.30 + 0.35 + 0.35 = 1.00`. Packet state `complete`.
- Unit B: syntactic `unavailable`, nulled. Denominator `0.35 + 0.35 = 0.70`. Packet state `degraded`.

Every composite score differs by `1/0.70 = 1.429×`, ordering inverts wherever the syntactic axis is the discriminator, and FR63's 0.0–1.0 confidence moves by 43% of its range. Under Unit A the partial scan is **undisclosed**: the packet says `complete` for a result set built from 60% of the index, which is precisely the outcome AD-10 line 121 exists to prevent — "a partial outage returning deceptively complete results". Under Unit B a recoverable slow query becomes a nulled axis, and if the semantic axis times out in the same request AD-10's "Fail the query only when no selected axis can produce a safe response" fails the whole query.

**(b) G1 measures two different systems.** G1's recall protocol runs against a loaded tenant. Under Unit A a timed-out syntactic axis silently contributes 40% of its candidates and the measured recall is a property of the timeout; under Unit B the axis vanishes and the measured recall is a property of the semantic axis alone. The L328 ledger row makes this concrete: the G1 harness does not exist yet, so whichever adapter epic ships first fixes the meaning of the gate.

**(c) The packet has no shape for the answer either way.** Unit A must disclose "low recall" — AD-12 line 134's mandated elements include "degradation/excluded axes" and "freshness", neither of which encodes "this axis scanned 60% of the corpus". Unit B nulls an axis that returned data, which AD-12 encodes as `null` and which is indistinguishable from a dead backend.

**Proposed tightening — AD-9 Rule, replacement sentence (and strike "without truncation" from AD-10's biconditional per erratum E1):**

> "`truncated` applies to **any** axis that verified the request's tenant and case scope and its active generation and epoch but did not complete the work those scopes select within AD-11's or the axis's configured depth, result, or time limits — an uncompleted graph case partition, an aborted or timed-out scan, or a provider-side cutoff — and the axis is then retained with its hits, carries denominator weight, and is named in the Evidence Packet as degraded with an uncovered-scope-unit count that is the number of uncompleted case partitions for the graph axis and `0` with a stated cutoff reason for a non-partitioned axis. Reaching the configured per-axis candidate depth is not truncation and never sets this state. Every adapter returns, alongside its hits, an explicit machine-readable statement of whether it applied the request's tenant and case scope, whether it truncated in this sense, and the scope units it did not cover; that statement is the only evidence AD-10's axis-selection step uses, and an adapter that does not supply it is treated as scope-unverified and nulled."

---

### ADV3-03 — critical — "One active epoch and writes under it" is satisfiable two incompatible ways, and the fence's staging redirect names a resource AD-14 does not define for Phase 1 or for the graph store

**Unit A — FR43 degraded rebuild command unit** (the MVP epic implementing AD-14's ratified Phase 1 clause).
**Unit B — Projection coordinator and write-fence unit** (AD-3's Dapr-state checkpoints and conditional writes).

**Compliance proof.** AD-3 line 80, amended this pass, supplies the cross-epoch relation and then patches Phase 1 with one sentence:

> "a write whose generation or epoch differs from the stored document's is never merely older — it **replaces the document when it carries the tenant's active generation and epoch**, and **otherwise targets AD-14's disjoint staging resources** rather than the active document, so an active and a staging write can never alternate in one document. **A Phase 1 / MVP degraded rebuild under AD-14 has one active epoch and writes under it.**"

AD-14 line 146 defines staging exactly once, for one phase and two change classes:

> "In **Phase 2**, embedding provider/model/dimension and index-schema changes use versioned create-backfill-verify-switch-retire migrations with **disjoint active and staging resources** … In **Phase 1 / MVP**, FR43 permits only a tenant-scoped, explicitly acknowledged degraded rebuild of rebuildable projections; the command … **carries the AD-3 configuration epoch** … The active schema generation and embedding-configuration epoch are tenant-lifecycle domain facts committed through AD-2 **before any projection may cite them**."

**Divergence 1 — what "one active epoch and writes under it" means.** FR43 is a change of embedding provider, model or dimension. Two literal readings:

- **Unit A** allocates a new `embeddingConfigurationEpoch`, commits it through AD-2 as the tenant's active epoch — required, because AD-3 admits a differing-epoch write into the active document **only** when it "carries the tenant's active generation and epoch", and AD-14 requires the epoch be committed "before any projection may cite" it — and then rebuilds. "One active epoch, writes under it" holds exactly.
- **Unit B** does not allocate a new epoch; the rebuild writes under the epoch that is already active. "One active epoch, writes under it" holds exactly, and AD-14's "carries the AD-3 configuration epoch" is satisfied by carrying the current one.

**The concrete incompatibility.**

Unit A, at the instant it commits epoch 2 as active: every stored document in the tenant carries epoch 1, so AD-10 line 122's conjunct — "read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`" — makes **every axis unsafe**, and AD-10's "Fail the query only when no selected axis can produce a safe response" **fails every query for the tenant for the whole rebuild**. Simultaneously AD-3's "The reported public ingestion state is always that of the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`" finds no checkpoint under epoch 2, so **all 100K units report `queued` or `indexing`**. Defensible under "never claims zero downtime", and a total tenant outage of unbounded duration for what FR43 presents as a maintenance command.

Unit B: the new model's vectors land in the same Redis vector index, under the same epoch, alongside the old model's — a 768-dimension index receiving 1536-dimension vectors, or a same-dimension index holding two incomparable embedding spaces whose cosine distances are meaningless against each other. This is verbatim the failure AD-14 line 145 exists to prevent ("**mixed embedding dimensions**"), reached without violating any Rule sentence, because Phase 2's dimension-change contract is expressly a Phase 2 obligation and line 146 states that "no phase's evidence satisfies another's". Worse, the epoch has now stopped being a function of the embedding configuration, which blinds AD-10's epoch-mismatch safety check and AD-3's identity discriminator to exactly the change they were introduced for. AD-10 reports `complete` throughout.

**Divergence 2 — the redirect target does not exist.** The "otherwise" branch of L80's fence is reachable in Phase 1 even under Unit A's reading. AD-4 line 86 requires an activity to "Capture immutable non-secret configuration **plus its epoch at start**"; an in-flight projection activity that captured epoch 1 and retries after activation carries a tuple that is neither the stored document's nor the tenant's active one, so the fence sends it to "AD-14's disjoint staging resources" — which Phase 1 does not have. Unit A drops the write (no target exists) and the unit silently never reprojects; Unit B creates an ad-hoc staging key family, which is unregistered coordination state under AD-8 line 110 and claims a Redis key prefix no registry row reserved, breaching line 210's "Every row declares its reserved Redis key prefix". The same gap exists across phases for the **graph** store: AD-14 defines staging for "embedding provider/model/dimension and index-schema changes" (Phase 2) and for "backend replacement" (Phase 3), never for a `schemaGeneration` bump to FalkorDB node or edge shape — yet AD-3's fence covers every derived store and its tombstones.

**Proposed tightening — AD-14 Rule, replacement sentence, plus one clause in AD-3:**

> "A Phase 1 / MVP degraded rebuild allocates a new `embeddingConfigurationEpoch` and commits it through AD-2 as a **staging** epoch that is never the tenant's active epoch until the rebuild's verification succeeds; it is the sole writer permitted to target that staging epoch, ordinary ingestion continues at the active epoch throughout, the tenant's reported ingestion state and AD-10's axis safety are evaluated against the active epoch alone, and activation is a single AD-2 commit after which AD-3's retire step deletes the superseded epoch's documents and checkpoint records. **Staging resources** are defined for every derived store the AD-3 fence covers — syntactic index, vector index, graph, and tombstones — in every phase in which a non-active generation or epoch can be written; where a phase declares none, a write carrying a non-active, non-declared-staging tuple is **rejected as out-of-generation and surfaced as an actionable `Failed` reprojection**, never dropped and never redirected to a resource the phase has not defined."

---

### ADV3-04 — critical — AD-5's lifecycle exemption revalidates against a "tenant lifecycle state machine" the spine never defines, never places, and never assigns an owner, and AD-16's own purge destroys the two candidate homes

**Unit A — Tenant lifecycle unit** (AD-6: provisioning, verification, **repair**, deletion).
**Unit B — Tenant erasure workflow unit** (AD-16).

**Compliance proof.** AD-5 line 92, amended this pass, creates the exemption and delegates to an undefined artifact:

> "Work with no human caller — workflows, activities, actors, reminders, repair, replay, and migration — carries an explicit tenant authority captured at initiation, **revalidates it against active tenant state on every resume and at each activity boundary**, and fails closed when the tenant is inactive, erased, or absent from the initiating principal's grant … **AD-6 lifecycle workflows and AD-16 erasure are the stated exception**: they act on tenants that are being created, deactivated, or erased, are authorized by the operator principal below rather than by a tenant grant, and **revalidate against the tenant lifecycle state machine rather than against tenant-active** — so erasure and lifecycle repair are never deadlocked by their own subject's state."

`state machine` occurs exactly once in the artifact, at line 92. No AD enumerates its states, names its store, or assigns its owner. The available fragments are mutually inconsistent as a vocabulary: AD-5 offers "inactive, erased, absent", "currently active and verified", and "being created, deactivated, or erased"; AD-6 line 98 offers "an active verified tenant"; AD-16 offers an erased-register tombstone. That is at least three overlapping vocabularies and no machine.

AD-14 line 146 places tenant-lifecycle facts in EventStore: "The active schema generation and embedding-configuration epoch are **tenant-lifecycle domain facts committed through AD-2**." AD-2 line 74 makes EventStore domain truth. So Unit A puts the machine in the tenant's EventStore stream — the compliant, obvious home.

AD-16 line 158 then destroys it: the erasure workflow purges "**every store that can hold tenant content or content-derived material**" and runs "the Hexalith.EventStore **tenant-key crypto-shredding** workflow" that "irreversibly invalidates or deletes content access". Unit B must revalidate "at each activity boundary" — including the boundaries *after* the shred activity. Its own irreversible step has made the state machine unreadable, and AD-5 says an absent subject fails closed, so the erasure workflow deadlocks after the point of no return, in direct contradiction of AD-16's requirement that erasure "remain resumable" and of the exemption's own stated purpose. Unit B therefore places the machine in a platform-scoped store outside the tenant key, alongside the register — also compliant, since no Rule places it anywhere.

Neither unit violates a Rule. There are now two state machines.

**The concrete incompatibility.**

**(a) A compliant lifecycle repair re-provisions an erased tenant.** AD-6's Binds line 96 names "repair", and AD-5's exemption explicitly covers "lifecycle repair". Unit A's EventStore machine still shows `acme-legal` as `Active` — its post-shred events are unreadable, so the last readable state is the pre-erasure one, and AD-5 line 92 forbids Unit A from inferring anything from unreadable payloads. An operator runs `tenant repair acme-legal`. Unit A is authorized (operator principal), is exempt from tenant-active, revalidates against its own machine, finds `Active`, and does exactly what AD-6 line 98 requires of it: it re-creates the per-tenant Redis ACL principal, re-selects the FalkorDB tenant graph, re-provisions the access-telemetry partition, and — because AD-5 line 92 makes it the grant's "sole writer" — **re-creates the per-tenant grant that AD-16 revoked "as a completion condition of erasure"**. The erased-tenant register does not stop this: line 158 makes it the "sole authority for replay rejection, tenant-ID non-reuse, and restore admission", and repair is none of the three. AD-16's irreversibility claim survives for *content* and fails for *identity and infrastructure*.

**(b) Erasure is resumable in one unit and not the other, for the same tenant.** If Unit B ships against the EventStore machine (Unit A's home), a crash after the shred activity leaves an erasure that cannot resume and cannot be reported complete — AD-16 requires "verification means a recorded, reproducible check per enumerated target" and there is no readable subject to record against. If Unit B ships against a platform machine, resumption works but Unit A's verification path (AD-6: "with verification") reads a different machine and can certify a tenant `Active` that Unit B has recorded `Erased`. Two workflows, two truths, one tenant.

**(c) The state machine is the third store with an unowned lifecycle.** It joins the erased-tenant register (ADV3-01) and the `Contracts.V1` name register (ADV2-07 residue) as an artifact the spine requires by name and assigns to nobody.

**Proposed tightening — AD-6 Rule, added sentences (with a pointer inserted at AD-5 line 92):**

> "AD-6 owns the tenant lifecycle state machine and is its sole writer. Its states are enumerated in this decision — `Provisioning`, `Active`, `Deactivated`, `Erasing`, `Erased` — with `Erased` terminal and irrevocable; every transition is an AD-2-committed domain fact. The machine is held in a **platform-scoped, content-free** resource outside any tenant-scoped keyspace and outside the tenant crypto-shredding boundary, colocated with AD-16's erased-tenant register and sharing its durability posture, so that a tenant's lifecycle state remains readable after its content key is destroyed. Provisioning, verification, repair, deletion and erasure all revalidate against this machine and no other source; **repair and provisioning refuse any tenant in `Erasing` or `Erased`**, and AD-5's `erased` disposition resolves from this machine and AD-16's register alone."

---

### ADV3-05 — high — The ledger's closing attestation asserts AD-20 satisfaction on conjuncts AD-20 does not state, while AD-20's tracker conjunct is unsatisfiable by construction

**Unit A — Release-gate CI unit** (mechanizes AD-19/AD-20 on protected releases and on gate-qualification claims).
**Unit B — Architecture ledger maintainer unit** (authors and closes the Current Alignment Gaps rows).

**Compliance proof.** AD-20 line 182, amended this pass, states three conjuncts and one disjunction:

> "Every governed requirement and every gap so classified **must carry, before it may be cited as qualification evidence, exactly one of a current evidence path or a dated phase exception approved by product and architecture, each with a named owner and a tracker entry**; a row lacking both is an open gate blocker, not a satisfied one, and the ledger states which rows are currently in that state."

Line 345, the ledger's closing paragraph, attests to something adjacent and different:

> "**Every row above carries the named owner and dated expiry AD-20 requires.** The dates are **derived** from the PRD's `[DERIVED]` 2026-10-31 G1-prerequisite checkpoint … **Tracker entries remain owed:** AD-20 also requires an entry in `sprint-status.yaml`, **which the 2026-09-12 sprint-change correction freezes**, so no row may yet be cited as satisfied qualification evidence and each active-foundation-critical row stays an open gate blocker until its tracker entry and evidence land."

Three defects, each load-bearing:

1. **AD-20 does not require a "dated expiry".** It requires a *dated phase exception approved by product and architecture*. Line 345 attests satisfaction of a conjunct AD-20 does not state, and is silent on the one it does.
2. **No row carries either disjunct.** Every Disposition cell at lines 320–343 reads "evidence path **owed** by 2026-10-31" or "bump or dated exception by 2026-10-31". An *owed* evidence path is not "a current evidence path"; a *deadline for obtaining* an exception is not an approved exception. Several cells additionally offer a **disjunction** ("upgrade or dated exception"), which cannot satisfy "**exactly one** of".
3. **The tracker conjunct is unsatisfiable.** AD-20 makes a `sprint-status.yaml` entry a precondition for citing any evidence; line 345 records that the file is frozen by a governance process the spine does not own and gives no fallback, no exemption, and no expiry for the freeze.

Unit A reads line 345's first sentence as the architecture owner's attestation that the ledger meets AD-20's owner-and-date obligations, treats the freeze as an external condition suspending the tracker conjunct (AD-20 cannot bind a file it does not control), and permits a G1–G6 qualification claim on 2026-10-31 once evidence lands. Unit B reads AD-20 literally: zero rows carry a current evidence path, zero carry an approved exception, and the tracker conjunct is unsatisfiable, so **no qualification claim of any kind may be made and no protected release asserting a governed gate may be cut, indefinitely**. Both readings are literal; line 345 supports the first and AD-20 supports the second.

**The concrete incompatibility.** The 2026-10-31 checkpoint. Unit A's gate job evaluates twelve `Yes` rows against their delivered evidence and passes or fails on the evidence. Unit B's gate job fails unconditionally, because `sprint-status.yaml` is frozen and AD-20 admits no evidence without it. The same repository, the same date, the same rows, and the release either ships or is structurally unshippable. Neither unit can be told it is wrong by a sentence in the spine.

Secondarily, "exactly one of" makes a row *worse* by acquiring both: a row that obtains an evidence path while a dated exception is still in force carries two and satisfies neither, which is the expected state for the three Production blockers at lines 332, 333 and 337 during any grace period.

**Proposed tightening — AD-20 Rule, replacement sentence, plus a corrected attestation at line 345:**

> "Before a governed requirement or an active-foundation-critical gap may be cited as qualification evidence it must carry **at least one** of a current re-runnable evidence path or a dated exception recorded in the single exception register defined below, together with a named owner and a tracker entry; where both are present the evidence path governs and the exception is closed in the same change. A row whose disposition states only a future date, an owed evidence path, or a choice between remedies carries neither and is an open gate blocker. Where the tracker of record is frozen or otherwise unavailable, the architecture owner records the tracker obligation in the ledger row itself with the freeze reference and a review date; the freeze suspends the tracker conjunct and no other, and never converts an unmet evidence obligation into a satisfied one."

---

### ADV3-06 — high — `active-foundation critical` now has a definition and still no classifier, and applying the definition mechanically contradicts the authored column

**Unit A — Release-gate CI unit** (derives the gate set from AD-20's predicate).
**Unit B — Architecture ledger maintainer unit** (maintains the authored `Active-foundation critical` column).

**Compliance proof.** AD-20 line 182 supplies the predicate the pass-2 finding asked for, and assigns nobody to evaluate it:

> "A gap is **active-foundation critical** when it violates a rule binding an MVP-active surface and its violation can produce **incorrect product behaviour, cross-tenant exposure, irreversible data loss, or an unverifiable gate claim**; release and operational debt on phase-inactive surfaces is not."

Line 316 restates it in the passive voice — "a row **classified** active-foundation critical must carry …" — and the ledger's header column carries an authored `Yes`/`No`. Nothing in the spine says who classifies, at what moment, or whether the authored column or the predicate governs when they disagree.

Unit A evaluates the predicate. Unit B honours the column. Both comply.

**The concrete incompatibility.** The predicate and the column disagree on at least three rows, and the disagreement is not marginal:

- **Line 342** — "`Contracts.V1` has an owning component but no wire-name register and no build-failing guard for unregistered names." AD-12 binds REST and the CLI, both MVP-active. Two epics reserving one wire name with different shapes produces incorrect product behaviour on a shipped surface, and it makes G5's cross-surface golden-vector claim unverifiable — two of the predicate's four triggers. Authored: **No**.
- **Line 336** — "`tools/` utilities sit outside the release-inventory gate, and `MigrateEmbeddingVectors` takes a direct provider-SDK dependency." AD-19 binds the publish inventory; an inventory gate that does not cover a surface cannot evidence what was published, which is an unverifiable gate claim. Authored: **No**.
- **Line 330** — "Source and package modes have separate restore/build but not independent contract/integration lanes." AD-19 line 175's stated Prevents is "one successful topology standing in for another" — the definition of an unverifiable gate claim. Authored: **No**.

Line 343 is the control that shows the predicate is applicable rather than vacuous: "AD-1's two dependency invariants … are **true today** but unguarded" — no violation, therefore correctly `No` under both readings.

Unit A therefore gates on fifteen rows; Unit B gates on twelve. Under ADV3-05's Unit A reading — where the gate actually decides a release — the two units cut different releases from the same commit. Under Unit B's reading the three rows above can be cited as satisfied qualification evidence for MVP-active surfaces on the strength of an authored `No` that the spine's own predicate contradicts.

**Proposed tightening — AD-20 Rule, added sentences:**

> "The architecture owner classifies every ledger row against this predicate at the revision that adds or amends the row, records the triggering clause in the row itself, and re-evaluates every row at each spine revision; where the recorded classification and the predicate disagree, the predicate governs and the row is treated as active-foundation critical until the owner reclassifies it in a spine revision. A `confirmed resolved` verdict is issued by that same owner, is backed by re-runnable evidence named in the row, and is recorded with the date and the evidence reference."

---

### ADV3-07 — high — AD-17 grants itself a third exemption from AD-5's fail-closed, and AD-5's exception list is closed

**Unit A — Authorization unit** (AD-5's single authority-derivation component; AD-5 line 91 Prevents "two writers of one authorization decision").
**Unit B — Access-telemetry retention and purge unit** (AD-17, Platform Operations).

**Compliance proof.** AD-5 line 92 states the erased-tenant fail-closed and then closes the exception list with the definite article:

> "Work with no human caller — workflows, activities, actors, reminders, repair, replay, and migration — … **fails closed when the tenant is inactive, erased, or absent** from the initiating principal's grant … **AD-6 lifecycle workflows and AD-16 erasure are the stated exception**".

AD-17 line 164, added this pass, asserts a third:

> "Access-telemetry reads and writes **derive tenant authority under AD-5 like any other surface**, authorized independently of the calling workload's channel identity … Retention accounting for an erased tenant — TTL expiry, purge progress, and the derived erasure mapping — continues under Platform Operations' own authority and is **explicitly not blocked by AD-5's erased-tenant fail-closed**, so an erased tenant's retained telemetry stays purgeable and observable."

The two sentences in AD-17 are themselves in tension ("like any other surface" versus "not blocked"), and AD-5's list is grammatically closed — "**the** stated exception", enumerating two.

Unit A implements AD-5 as the single writer of the authorization decision that AD-5's Prevents clause demands, enumerates the two stated exceptions, and denies the telemetry purge reminder for an erased tenant. Unit B implements AD-17's explicit carve-out and runs it. Neither violates the AD it implements; AD-5 does not know AD-17's exemption exists, and AD-17 cannot amend AD-5 from outside it.

**The concrete incompatibility.**

**(a) An erased tenant's telemetry never purges, or AD-5's fail-closed has an unenumerable set of exceptions.** Under Unit A, the TTL reminder for `acme-legal` fails closed at every activity boundary after erasure. AD-16 line 158 has already declined to force the purge ("Deletion does not wait for or force early deletion of retained opaque access telemetry: those records follow AD-17's bounded TTL/purge contract"), so the records are retained past their configured TTL with no path to removal, breaching AD-17's own "Platform Operations owns the configured TTL, **observable purge progress**" and defeating the very outcome line 164's sentence was added to guarantee. Under Unit B, AD-5's fail-closed acquires an exception written in another AD, which means Unit A cannot enumerate the exception set from AD-5 and must scan every other AD for self-granted carve-outs — and AD-5's "two writers of one authorization decision" Prevents is realized: AD-17 is now the second writer.

**(b) Two deployments, two retention outcomes, one rule.** The divergence is silent — nothing in the ledger, the health surface, or the packet distinguishes "purge blocked by fail-closed" from "purge not yet due" — so the discrepancy surfaces only when an operator asks why an erased tenant's telemetry is still present after its TTL, which is exactly the FR39 evidence question AD-16 exists to answer.

**Proposed tightening — AD-5 Rule, replacement clause (AD-17's sentence then becomes a pointer rather than a grant):**

> "Three exemptions from the erased-and-inactive fail-closed exist and no other AD may add a fourth without amending this list: AD-6 lifecycle workflows and AD-16 erasure, which act on their own subject's lifecycle state and revalidate against AD-6's tenant lifecycle state machine; and AD-17 retention accounting — TTL expiry, purge progress, and the derived erasure mapping — which is authorized by Platform Operations' own principal, is restricted to content-free records, may delete but never read or export retained telemetry for an erased tenant, and never creates, restores, or re-activates any tenant resource."

---

### ADV3-08 — high — Erasure completion evidence is "a content-free record in the register" that "is domain evidence under AD-2", and the amendment has now placed the register somewhere AD-2 is not

**Unit A — Tenant erasure workflow unit** (AD-16).
**Unit B — Erasure evidence and operator-reporting unit** (FR39; the CLI/REST surface that must prove a deletion completed).

**Compliance proof.** AD-16 line 158 now fixes the register's store and, in the same Rule, assigns the completion record to a different system of record:

> "It **lives in Dapr state under AD-8**, in a platform-scoped component … Verification means a recorded, reproducible check per enumerated target, and completion evidence is a content-free record **in the register** carrying the target list, per-target outcome, and completion time; **it is domain evidence under AD-2**, not access telemetry."

AD-2 line 74 defines what domain evidence under AD-2 means, and the Consistency Conventions line 195 restate it as an ordering obligation:

> AD-2: "**Accept domain mutations through Hexalith.EventStore before reporting success**, then rebuild derived stores from authoritative events".
> L195: "**EventStore acceptance precedes domain success**; … actor/workflow state is durable coordination, **not domain truth**."

Unit A writes the completion record only to the register, exactly as line 158's "in the register" instructs. Unit B refuses to report an erasure complete until an EventStore acceptance exists, because line 195 forbids treating durable coordination state as domain truth and AD-2 forbids reporting success before EventStore acceptance — and line 158 has just called the record domain evidence under AD-2. Neither violates its own AD; the amendment named the register's store without reconciling that store with the classification the same sentence gives its contents.

**The concrete incompatibility.**

**(a) FR39 evidence is queryable from two different systems, and one of them is coordination state.** Unit A's evidence lives in the Dapr state component that the Deferred table line 353 says must still be "qualified or replaced … against durability and recovery requirements" — the spine's own ledger row at line 341 concedes this ("the Dapr state component that must hold it is the one whose production durability is unqualified"). Unit B's evidence lives in EventStore and survives that component's replacement. An auditor asking "prove `acme-legal` was erased on 2026-08-14" gets an answer from one and nothing from the other.

**(b) There is no compliant EventStore location.** If Unit B writes to the tenant's stream, its own workflow crypto-shreds the key and the evidence becomes unreadable, defeating "recorded, reproducible". A platform-scoped, never-shredded stream is the only workable home and the spine does not authorize one — AD-16 says the *register* is outside tenant keyspace and says nothing about an EventStore counterpart. Unit B therefore either invents an unratified stream family or cannot comply.

**(c) The "single" register becomes two records of one fact.** If both units ship — Unit A's register write and Unit B's EventStore commit — the two can diverge across the window between an EventStore acceptance and its projection, and AD-16's "sole authority" no longer identifies which one answers.

**Proposed tightening — AD-16 Rule, replacement sentence:**

> "Erasure completion evidence is committed through AD-2 as a content-free domain event on a **platform-scoped EventStore stream that is never encrypted under any tenant key and is never crypto-shredded**, carrying the target list, per-target outcome, and completion time; the erased-tenant register holds the derived non-reuse tombstone and a reference to that event, is the sole authority for provisioning, replay, and restore admission, and is rebuildable from the platform stream, so that no operator evidence depends on the durability of the unqualified coordination component and the register and the evidence can never disagree about whether an erasure completed."

---

### ADV3-09 — high — The composition delimiter is "declared with the grammar" and neither is written down, while AD-4 forbids the encoding that would make composition injective over its own non-grammar components

**Unit A — Dapr pub/sub ingestion subscriber unit** (AD-4's CloudEvent identity; the `/events/ingest` adapter at line 190).
**Unit B — Preflight dedup and key-composition unit** (the Direct Redis Exception Registry row at line 208).

**Compliance proof.** AD-5 line 92, amended this pass, tightened the wording and declared nothing:

> "restrict tenant, case, and `MemoryUnitId` issuance to **one documented bounded ASCII grammar** that is single-case … **Compose** every derived key, ACL pattern, index name, and state key **unambiguously using a single delimiter declared with the grammar and reserved out of it.**"

The grammar is not in the artifact, no document is named, no owner is assigned, and no ledger row covers its absence — line 339's row is about *legacy identifiers*, not about the missing grammar. The delimiter is defined only by reference to that absent document.

AD-4 line 86, unchanged, composes an identity from four components of which the grammar governs two, and forbids transforming the other two:

> "scope CloudEvent identity by tenant, case, **exact validated `source`, and `id`** with ordinal comparison and **no post-validation normalization**."

Line 208 requires the composite in a Redis key: "the key includes tenant/case and canonical request identity". CloudEvents `source` is a URI-reference and legitimately contains `:`, `/`, `#`, `?`, `%` and, on any plausible delimiter choice, the delimiter itself.

Unit A composes the raw components with the delimiter: AD-4's "exact" and "no post-validation normalization" are satisfied exactly, and AD-5's "unambiguously" is satisfied in intent because the delimiter is reserved out of the grammar — which is all AD-5 reserves it out of. Unit B percent-encodes `source` and `id` into the grammar's alphabet before composing: composition becomes injective and AD-5's "unambiguously" is satisfied in fact, but the stored and compared identity is no longer the *exact* source, and an encode applied after validation is defensibly the "post-validation normalization" AD-4 prohibits. Neither unit violates the AD it implements, and each violates the other's reading of a rule it is not implementing.

**The concrete incompatibility.**

**(a) A silent data-loss path, unchanged from pass 2 and still live.** With Unit A's composition and any delimiter `D`, the events `(t1, c1, source="app" + D + "x", id="7")` and `(t1, c1, source="app", id="x" + D + "7")` produce one dedup key and one AD-4 durable suppression identity. The second event is suppressed as a duplicate, never reaches EventStore, and the publisher is told it succeeded. AD-5 line 91's Prevents names the mechanism — "an identifier whose characters defeat the isolation mechanism that carries it" — and AD-5's reservation does not reach the two components that carry the collision.

**(b) Phase 1.5 either ingests standards-conformant CloudEvents or rejects all of them.** If a unit reads "exact **validated** `source`" as validated against AD-5's grammar — the only validation grammar the spine has — then a single-case ASCII alphabet that excludes every character significant in URL path segments admits roughly `[a-z0-9]` and rejects every real CloudEvent source, making L1–L3 unpassable. If it reads it as CloudEvents URI-reference validation, (a) applies. AD-4 says "validated" and never says against what.

**(c) Two grammars.** Because the grammar document does not exist, the ingestion epic and the provisioning epic each write one. The delimiter is "declared with the grammar", so two grammars produce two delimiters, and every composed key, ACL pattern and index name in the platform is built by two incompatible rules.

**Proposed tightening — AD-5 Rule, replacement sentences:**

> "The issuance grammar and its single reserved composition delimiter are declared in one tracked document owned by this architecture and referenced by name from this decision; until that document exists this rule is binding and unenforceable, and its absence is a ledgered obligation with a named owner. Every composed key, ACL pattern, index name, and state key is built from that delimiter alone, and every component of a composed key that is not itself issued under the grammar — CloudEvent `source` and `id`, `IdempotencyToken`, principal identifiers, and handle scopes — is length-bounded and **reversibly encoded into the grammar's alphabet before composition**, so that composition is injective over all inputs. That encoding is a composition step and not a normalization: AD-4's identity comparison is performed on the exact validated values, and the encoded form is used only to build keys."

---

### ADV3-10 — medium — "Single-case" does not say which case, and a compliant uppercase grammar cannot name the Kubernetes objects AD-5 itself enumerates

**Unit A — Identifier issuance unit** (the grammar's implementation).
**Unit B — Tenant resource provisioning unit** (AD-6: per-tenant Redis ACL principal, FalkorDB database/graph, telemetry partition, and the Kubernetes objects that carry them).

**Compliance proof.** AD-5 line 92, amended this pass:

> "restrict tenant, case, and `MemoryUnitId` issuance to one documented bounded ASCII grammar that is **single-case** — so no downstream system that folds case can collapse two distinct identifiers into one — and that **excludes every character significant as a metacharacter, delimiter, or wildcard** in Redis ACL key patterns, Redis/RediSearch/FalkorDB key and index names, Dapr key and component names, **URL path segments, Kubernetes object names**, OpenBao secret paths, SQL identifiers, and object-storage paths."

The exclusion criterion is *significance as a metacharacter, delimiter, or wildcard*. An uppercase ASCII letter is none of those in any of the listed systems — it is simply **invalid** in a Kubernetes object name (RFC 1123 requires lowercase alphanumerics, `-`, and `.`). The rule therefore does not exclude uppercase, and "single-case" is satisfied by either case.

Unit A ships `[A-Z0-9-]{1,63}` — single-case, bounded, ASCII, and containing no metacharacter, delimiter, or wildcard from any listed system. Fully compliant. Unit B must derive a Kubernetes object name for the tenant's resources and cannot: the API server rejects it. Unit B lowercases, which the same Rule's first clause forbids ("compare them with `StringComparison.Ordinal`, **without case folding**") and which reintroduces exactly the collapse single-case was added to prevent.

A second, subtler instance of the same defect: `-` is not a metacharacter, delimiter, or wildcard in the listed systems, so a compliant lowercase grammar `[a-z0-9-]+` admits `-acme-`. RFC 1123 labels must begin and end alphanumeric, and OpenBao and object-storage paths have their own leading/trailing rules. Unit A issues `-acme-`; Unit B's provisioning fails at object creation for a validly issued tenant ID, and AD-6's workflow must compensate a tenant it was never able to create.

**The concrete incompatibility.** A tenant ID that the issuance unit certifies as valid cannot be provisioned. AD-6 line 98 requires provisioning to be "explicit, idempotent, observable, bounded … with verification, **compensation**, and resumable cleanup", so the failure is handled — but the platform now has an issuance grammar that can produce unprovisionable identifiers, and the isolation guarantee AD-5's Prevents promises ("an identifier whose characters defeat the isolation mechanism that carries it") is unmet by construction rather than by a bug. Because the grammar document does not exist (ADV3-09), nothing catches the mismatch before the first failed provisioning.

**Proposed tightening — AD-5 Rule, replacement clause:**

> "The grammar is **lowercase** ASCII, is a strict subset of every naming scheme the platform composes identifiers into — it satisfies RFC 1123 label rules including the alphanumeric first and last character, Redis and RediSearch key and index names, Dapr key and component names, OpenBao path segments, SQL identifier rules, and object-storage key segments — is bounded to a stated maximum length that leaves room for every prefix, suffix, and delimiter the platform composes around it, and excludes every character significant as a metacharacter, delimiter, or wildcard in any of them. Conformance to each listed scheme is asserted by a test in the issuance component, not by inspection."

---

### ADV3-11 — medium — Two `/v1` contract owners: `Hexalith.Memories.Contracts` owns `Contracts.V1`, and the access-telemetry plane has independent, non-packable contracts outside the owner, the register, and AD-1's published-contracts rule

**Unit A — `Hexalith.Memories.Contracts` owner unit** (AD-12's named owning component and the versioned name register).
**Unit B — Access-telemetry contracts and service unit** (`Hexalith.Memories.AccessTelemetry.Contracts`, AD-17).

**Compliance proof.** AD-12 line 134, amended this pass:

> "**`Hexalith.Memories.Contracts` is the single owning component for `Contracts.V1`** and its maintainers hold change-approval authority over that wire surface, and an additive change is admissible only once its new wire name, JSON shape, and nullability are **reserved in the contract's versioned name register** in the same change".

The Routes convention at line 190 places a second `/v1` surface outside the product surface entirely:

> "`MemoriesRoutes` owns the **product** surface, under `/api/v1`. Three exception families sit outside it: … and the AD-17 access-telemetry and clock plane **`/v1/access-telemetry/*`, `/v1/time/attest`**."

The Structural Seed at line 244 makes its contracts independent — "`Hexalith.Memories.AccessTelemetry*/` # **Independent contracts**, service, and clock authority" — and `tools/release-packages.json` lists `src/Hexalith.Memories.AccessTelemetry.Contracts` under **`nonPackableProjects`**, so it is never published.

Meanwhile the error convention at line 192 binds every surface to one catalogue:

> "The error-code catalogue is declared in `Contracts/V1`, is append-only under AD-12, and **every surface maps from it** rather than defining codes".

Unit A reserves names for `Contracts.V1` and reads AD-12's authority as covering the `Contracts.V1` wire surface only — the access-telemetry plane is a different component with independent contracts, named as such by the spine. Unit B evolves its own wire names in its own package with no register, because AD-12's register obligation attaches to `Contracts.V1` and AD-17 is silent about registers. Neither violates the AD it implements.

**The concrete incompatibility.**

**(a) A `/v1` wire surface evolves with no register, no owner, and no collision rule** — precisely the state AD-12's amendment was written to end, surviving on a second surface that carries a `v1` in its route. Two components can reserve `state`, `tenant`, `outcome` or `freshness` with different JSON shapes on two `/v1` routes of one platform, and neither register knows about the other.

**(b) An unpublished contract on a surface AD-1 requires consumers to reach through published contracts.** AD-1 line 68: "**Consumers use published contracts, clients, and Aspire integration packages** and never reference AppHost or Server." The access-telemetry plane is a real surface with real routes and AD-17 line 164 says its "reads and writes derive tenant authority under AD-5 **like any other surface**" — yet its only contract assembly is non-packable, so a consumer can reach the surface and cannot reach a published contract for it, and its only compliant remaining path is a reference AD-1 forbids.

**(c) The error catalogue is either violated or unreachable.** Line 192 obliges the telemetry surface to map from the `Contracts/V1` catalogue, which means the non-packable telemetry contracts must reference the packable `Hexalith.Memories.Contracts` — a dependency the Structural Seed's word "independent" invites Unit B to avoid, and which nothing requires.

**Proposed tightening — AD-12 Rule, added sentence:**

> "`Hexalith.Memories.Contracts` owns every public wire surface the platform serves under a `v1` route, including the AD-17 access-telemetry and clock plane; a component that carries its own contract assembly reserves its wire names, shapes and nullability in that one register, maps its errors from the `Contracts/V1` catalogue, and is published under AD-19 whenever any consumer outside this repository may call its surface — or the surface is declared internal, removed from `tools/release-packages.json` deliberately, and documented as unavailable to consumers."

---

### ADV3-12 — medium — "Platform-scoped component" has no scope, and the Deferred state-store replacement the spine mandates silently discards an indefinitely-retained register

**Unit A — Deployment topology unit** (`deploy/kubernetes`, Dapr components, DR planning).
**Unit B — Deferred state-store qualification unit** (the Deferred row at line 353, whose remedy is to "qualify **or replace**" the component).

**Compliance proof.** AD-16 line 158 places the register in "a **platform-scoped** component outside any tenant-scoped keyspace" that "**carries a stated durability and restore posture**" and is "**retained indefinitely**". The Consistency Conventions line 198 defines environment parity by *contract*, not by instance:

> "Every environment uses the **same Dapr component contracts** and the same pinned image digests, differing only in scale and in documented Production-only dependencies".

The Deferred table line 353 mandates a change to the store the register now lives in:

> "Production Dapr actor/workflow state store — Before Production launch or SLO approval, **qualify or replace** the current Redis component against durability and recovery requirements."

Unit A reads "platform-scoped" as per-deployment: one register per Dapr state component, so local AppHost, CI, integration and Production each hold their own — consistent with line 198's contract-level parity and with the fact that a Dapr component is a deployment-scoped resource. Unit B reads it as scoped to the Hexalith.Memories platform as a whole. Neither is contradicted.

**The concrete incompatibility.**

**(a) A DR failover admits every erased tenant.** Under Unit A, a second cluster stood up for disaster recovery has its own state component and therefore an empty register. AD-16's "regardless of whether the payload is readable" restore refusal reads an empty register and admits every erased tenant's EventStore restore and export re-import. The register's own guard — "any restore that would return the register to a state older than the data being admitted fails closed" — does not fire, because the register was never restored; it was never populated.

**(b) The mandated component replacement is an unaddressed data migration.** The Deferred row's remedy is scoped to "actor/workflow state". A unit executing it migrates actor and workflow state to the replacement component and has no instruction to migrate a register that lives in the same component, is "retained indefinitely", and has "no purge path". After a compliant, spine-mandated infrastructure change, every erased tenant ID becomes re-provisionable and every erased tenant's backup becomes restorable — an irreversible loss of an irreversibility guarantee, produced by executing a Deferred item exactly as written.

**Proposed tightening — AD-16 Rule, added sentence (with the Deferred row amended to name the register):**

> "The register's platform scope is the set of deployments that can serve one tenant population: every environment, replica, and disaster-recovery site that may admit a restore, an import, or a tenant provisioning for that population reads and writes one register instance, and a deployment that cannot reach it is unavailable under this rule rather than empty. The register's stated durability and restore posture includes its migration obligation: any qualification or replacement of the component that holds it migrates the register with verified completeness before the old component is retired, and that migration is part of the Deferred state-store item rather than separate from it."

---

### ADV3-13 — medium — "A state older than the data being admitted" has no age primitive, and the only clock authority in the platform is the one AD-16 forbids from gating completion

**Unit A — Register restore-admission unit** (AD-16's new rollback guard).
**Unit B — Backup and restore operations unit** (AD-2's three recovery operations).

**Compliance proof.** AD-16 line 158, added this pass:

> "that component carries a stated durability and restore posture, and **any restore that would return the register to a state older than the data being admitted fails closed**."

Nothing defines "older" for a register. The register holds tombstones with no TTL; it has no stated version, sequence, watermark, or timestamp field — line 158's enumerated contents are "the content-free non-reuse tombstone" and "a content-free record … carrying the target list, per-target outcome, and completion time". "Completion time" is the only temporal value, and it belongs to individual erasure records, not to the register's state as a whole.

The platform's only attested clock is the access-telemetry plane's: line 190 routes `/v1/time/attest` and line 245 calls `Hexalith.Memories.AccessTelemetry.Clock` the "clock authority". AD-16 line 158 forbids the telemetry side from participating in erasure decisions: "any **telemetry-side copy is derived** and can never satisfy, delay, or revoke completion."

Unit A compares wall-clock timestamps: the newest completion time in the restored register versus the creation time of the data being admitted. Unit B maintains a monotonic register sequence advanced on every erasure and compares sequence numbers against a sequence stamped into backups and export bundles. Both are compliant; neither is named.

**The concrete incompatibility.** A restore that Unit A admits, Unit B refuses, and vice versa. Wall-clock comparison is defeated by the ordinary case the guard exists for: a register restored from a backup taken *before* an erasure has a *newer* newest-completion-time than a tenant erased minutes later if the erasure record is the one missing — the comparison is against the wrong quantity, and Unit A admits the restore. Sequence comparison requires every export bundle and every EventStore backup to carry a register sequence, which AD-16 does not require: line 158 says "each bundle is registered at creation with its **tenant and issuance time**". So Unit B's primitive is not obtainable from the artifacts the same Rule specifies, and Unit B must fail closed on every restore of a bundle created before it shipped. If Unit A uses the attested clock to obtain a trustworthy time, it has made a telemetry-plane service a participant in an erasure admission decision, which line 158 forbids.

**Proposed tightening — AD-16 Rule, replacement sentence:**

> "The register carries a monotonic sequence advanced on every write and never reset; every backup, export bundle, and EventStore restore artifact records the register sequence current at its creation, and a restore is admitted only when the live register's sequence is greater than or equal to the artifact's. A register restored from backup resumes at the highest sequence it can prove, and any artifact stamped with a higher sequence fails closed until the register is reconciled. Times recorded in the register are descriptive evidence and never an admission control input, and no access-telemetry service participates in this decision."

---

### ADV3-14 — low — The register-consult obligation sits in an AD whose Binds omits provisioning, and in an AD whose Binds names provisioning but whose Rule is silent

AD-16's Binds at line 156 enumerates "Tenant deletion, EventStore payloads, all projections, durable workflow history and actor state, caches and derived artifacts, application export bundles, access telemetry, backups/restores, replay, per-tenant grants, tombstones, and tenant-ID reuse" — **tenant provisioning is not in the list**, yet AD-16's Rule now imposes an obligation on it ("AD-6 tenant provisioning consults it before issuing a tenant ID"). AD-6's Binds at line 96 *does* name "Tenant provisioning", and AD-6's Rule at line 98 says nothing about the register: "Other paths require an active verified tenant and never create infrastructure implicitly."

A unit that derives its obligations from the AD whose Binds names its concern — the mechanical reading the Capability → Architecture Map at lines 299–312 encourages, and the reading a story-scoping tool would take — implements AD-6 alone and never learns of the consult. A unit that reads every AD end to end finds it. This is thin as a divergence, because AD-16's Rule is binding wherever it is written, but the Binds lists are the spine's own index and this obligation is missing from both ends of it.

**Proposed tightening — AD-6 Rule, added clause, and AD-16 Binds, added item:** add "consults AD-16's erased-tenant register before issuing any tenant identifier and fails closed when it is unavailable" to AD-6's Rule, and add "tenant provisioning" to AD-16's Binds.

---

### ADV3-15 — low — The ledger's "Required convergence" column now carries normative content that no Rule states, and the ledger denies containing choices while offering one

Line 316 states what the ledger is: "These are **implementation obligations against adopted decisions, not open architecture choices**." Two rows contradict it.

Line 339's convergence requires "**validate on read at every trust boundary including import**" — an obligation no AD states. AD-5 line 92 binds *issuance* and instructs every consumer to "**Treat issued tenant and case IDs as validated** opaque tokens", which is the opposite instruction. So the pass-2 ADV2-05 hole is closed in the ledger and open in the Rule.

Line 326's convergence offers a choice: "move coordination to Dapr state **or adopt an explicit `AD-n` exception**" — an open architecture choice, in the column that says it holds none.

Unit A (an implementation epic) treats the ledger's convergence column as binding specification and validates identifiers on read at import. Unit B treats only ADs as binding, cites AD-5's "treat issued … as validated", and admits imported identifiers unvalidated — reproducing ADV2-05(a)'s foreign-identifier artifact in full. The spine does not say whether the ledger binds implementation or only records obligations, and AD-20 line 180 scopes the ledger to gate evidence, not to implementation.

**Proposed tightening — Current Alignment Gaps preamble, replacement sentence:**

> "Each row records an obligation against an adopted decision and its required convergence restates what the cited Rule already requires; a convergence cell may not introduce a requirement absent from every Rule, and may not offer a choice between architectural outcomes. Where a row's convergence would need either, the Rule is amended in the same spine revision and the row cites the amended text."

---

## Gate disposition

The second amendment is the smaller and better-targeted of the two. It closed four of the seventeen pass-2 pairs outright, took five more down to residues, closed four of the seven older partial closures, resolved both errata pass 2 raised as blocking, and gave the ledger the owners, dates and criticality definition AD-20 needed. Nothing in this pass reopens an adopted decision; every one of the fifteen findings is closable by replacing or adding Rule text in place, and eleven are single-sentence edits to text written on 2026-09-12.

**Erratum to correct before anything else.**

- **E1** — AD-10 line 122's biconditional retains the conjunct "returned within AD-11's or the axis's configured limits **without truncation**" while the same Rule declares a truncated graph fan-out safe two clauses later. AD-10 as written admits no compliant implementation. Strike the conjunct. This is also the sentence that keeps **ADV3-02** alive on the non-graph axes.

**Sequencing.**

- **ADV3-01, ADV3-02, ADV3-03, ADV3-04** are the four criticals and each is a *bootstrap or state-definition* defect rather than a behavioural one, so each blocks the epic that would first exercise it: ADV3-01 blocks any deployment (the first `tenant create` on a fresh cluster), ADV3-02 blocks the adapter and fusion epics jointly, ADV3-03 blocks the ratified FR43 MVP command, ADV3-04 blocks both the AD-6 lifecycle epic and the AD-16 erasure epic and cannot be closed by either alone. Close all four before sprint selection.
- **ADV3-05 and ADV3-06** decide whether the 2026-10-31 checkpoint can be evaluated at all and disagree by three ledger rows; close them together, before the checkpoint, not after.
- **ADV3-07, ADV3-08, ADV3-09** should close with their owning epics: the authorization component, the erasure evidence path, and the identifier/composition boundary respectively. ADV3-09 additionally has an artifact that does not exist — the grammar document — and should acquire a ledger row this revision whether or not the Rule is amended.
- **ADV3-10 … ADV3-15** batch into the next spine revision.
- The eight pass-2 findings still open (**ADV2-06, ADV2-08, ADV2-10, ADV2-11, ADV2-12, ADV2-13, ADV2-15, ADV2-17**) carry their pass-2 proposed text unchanged. ADV2-08 and ADV2-17 block G5 evidence independently of implementation quality; ADV2-10 defeats the AD-3 checkpoint epic's own guarantee through the epoch cache and is compounded by ADV3-03.

**Recommendation: PASS-WITH-FINDINGS.** Adopt E1 and the four criticals immediately, ADV3-05/06 before the next qualification claim, and re-run this lens once more against the corrected sentences. Three consecutive amendments have now produced a pair in the adjacent AD; the specific mechanism this round was **narrowing a fix to the exhibited case**, so the fourth pass should test each proposed sentence against the complement of the case that motivated it — the non-graph axis, the phase without staging, the consumer the enumeration omitted — before adoption.
