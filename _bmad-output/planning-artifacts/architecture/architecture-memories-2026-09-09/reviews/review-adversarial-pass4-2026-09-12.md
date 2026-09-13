# Adversarial Divergence — Pass 4 (Convergence Retest), 2026-09-12

**Verdict: PASS-WITH-FINDINGS — the retest is the best result this gate has produced: of the 40 items carried into this pass (17 ADV2 pairs, 7 older partial closures, 15 ADV3 pairs, erratum E1), 38 are Closed, 2 Partially closed, 0 Still open, 0 Regressed. Against the amended text I construct 20 new divergence pairs (5 critical, 9 high, 5 medium, 1 low), and 14 of the 20 live in text written by this pass. The class-level method worked on the *reported* findings and was not applied to the *new* ADs it wrote: the narrowing pattern recurred, relocated from "narrowing the fix" to "not propagating the fix into the adjacent rule".**

Target: `ARCHITECTURE-SPINE.md` (422 lines, `updated: 2026-09-12`, AD-1…AD-23).
Baseline: `reviews/review-adversarial-pass3-2026-09-12.md`.
Decisions executed: `sprint-change-proposal-2026-09-12-architecture-convergence.md` §3 (D1–D5) and §5 (class sweeps 1–5).
Code verified: `src/Hexalith.Memories.EventStore/`, `src/Hexalith.Memories.Server/`, `src/Hexalith.Memories.Contracts/V1/`.

Read-only pass. No spine, memlog, sprint-status or source file was modified.

---

## Method and discipline

1. Both units in a pair are quoted and provably compliant. A pair where one unit violates a rule is an implementation bug and is excluded.
2. Where a pass-3 quotation is byte-identical today, the verdict is Still open and the argument is not repeated.
3. The `.memlog.md` claims were treated as claims. Every closure below was verified against the spine text; every code claim against the source.
4. Every new pair states the concrete artifact — an ordering inversion, a score delta, a dropped record, a cross-tenant admission — with numbers where numbers apply.

---

## Part A — Retest of the 40 carried items

### A.1 — The 17 pass-2 pairs (ADV2-01 … ADV2-17)

The four already Closed (ADV2-01, -07, -14, -16) were re-checked for regression and none regressed. The five Partially closed and the eight Still open are retested below.

| ID | Pass-3 residue | Verdict | Evidence in the current text |
| --- | --- | --- | --- |
| **ADV2-01** | Closed; erratum E1 open | **Closed** | No regression. AD-22 L210 now carries the denominator consequence in the same decision as the state: `truncated` "is retained with its hits, contributes denominator weight, and is disclosed as degraded". E1 separately resolved (A.3). New residue at the zero-hits complement — **ADV4-02**. |
| **ADV2-02** | (b) `truncated` inverted to graph-only; (c) encoding absent; (d) no adapter reporting contract | **Closed** | (b) AD-22 L210: "**`truncated` applies to any axis in that position** — an uncompleted graph case partition, a timed-out or provider-cut-off syntactic or vector scan, `nl`, or any axis added later". (c)+(d) AD-22 L210: "**Every adapter returns, alongside its hits, an explicit machine-readable statement** … That statement is the only evidence AD-10's axis-selection step uses"; four values "reserved as a closed set in AD-12's name register". Ledger row L391 carries the missing implementation. |
| **ADV2-03** | Redirect target absent in the firing phase; MVP epoch bivalent | **Closed** | AD-14 L162 now fixes the epoch semantics ("allocates a new `embeddingConfigurationEpoch` … activates it atomically … sole writer permitted to target the new epoch") and AD-3 L96 supplies the no-staging branch ("rejected as out-of-generation and surfaced as an actionable `Failed` reprojection"). The pass-2 pair is dead. Both sentences open **ADV4-03** on the rebuild's own writes. |
| **ADV2-04** | No creator; no provisioning fail-closed; unavailable≡empty | **Closed** | AD-21 L204: "created and initialized by a **named platform-bootstrap step that runs before any tenant may be provisioned**"; "**Availability is a positive fact, never an inference from silence**"; the fail-closed list now names "**tenant provisioning**, tenant deletion, authoritative replay, EventStore restore, projection-store restore, and export re-import". |
| **ADV2-05** | No admission-boundary obligation; no issuer; "single-case" unresolved | **Closed** | AD-23 L216: "**Every trust boundary validates identifiers on read**, import included"; "Identifiers are issued rather than chosen"; "The grammar is **lowercase** ASCII — chosen, not merely single-case". |
| **ADV2-06** | Delimiter declared nowhere; non-grammar key components unconstrained | **Closed** | AD-23 L216 gives the delimiter a home ("declared in **one tracked artifact owned by this architecture and referenced by name from this decision**"), a binding-but-unenforceable status, a ledger row (L381), and the general rule: every non-grammar component "is length-bounded and either reversibly encoded into the grammar's alphabet or replaced by a fixed-length hash of itself **before** composition". The `("app\|x","7")` / `("app","x\|7")` collision is closed *as a class*. The example the same sentence uses to prove it is factually wrong — see **E2** and **ADV4-04**. |
| **ADV2-08** | "Byte-comparable" with no canonical form | **Closed** | AD-12 L150: "**Equivalence is byte-equality of one canonical document**: properties serialize in the name register's declared order, `double` values use round-trippable invariant `R` formatting, strings use minimal JSON escaping … while its own transport framing … is excluded from the comparison". Ledger row L392. Residues: **ADV4-11** (two of four state shapes unencoded) and **ADV4-12** (omission vs null). |
| **ADV2-10** | Epoch cache with no coherence contract and no mandated read path | **Closed** | AD-14 L162: "**Every consumer … resolves it through that actor and through no other path**; the cached value is invalidated synchronously as part of the AD-2 activation commit and before that commit is reported complete … a consumer that cannot resolve the value fails closed". Ledger row L393. |
| **ADV2-11** | Depth stated across surfaces only | **Closed** | AD-9 L132: "a **single deployment-wide, configuration-validated value** identical across every surface, tenant, caller, page, and load condition; admission control, backpressure, and per-tenant quotas may never shrink it"; AD-18 L186 mirrors it. Residue relocated to **ADV4-16**. |
| **ADV2-12** | Offset overrun had two compliant behaviours | **Closed** | AD-9 L132: "a request whose offset reaches or exceeds its length returns an empty page whose Evidence Packet reports the fused list's total length and states whether that length was bounded by the corpus or by the candidate depth". |
| **ADV2-13** | Checkpoint invalidation created an undefined state | **Closed** | AD-3 L96: invalidation is "**writing an explicit `reprojectionRequired` checkpoint record for the affected tuple, never by deleting it**… so an absent checkpoint record continues to mean that no projection work has ever been recorded". Ledger row L394. |
| **ADV2-15** | Digest set bound to AppHost only; Stack table records tags | **Partially closed** | Consumer binding closed: AD-19 L192 now says "**every consumer of a container default resolves images from that one digest set** — AppHost, `deploy/kubernetes`, CI, integration harnesses, and the `ContainerBaseImage` of every owned image alike". The second half **regressed into a false assertion**: the same Rule states "The Stack table records the qualified digest alongside the tag for every image the platform runs", and the Stack table at L249–L263 records **no digest for any image** — PostgreSQL says "digest-pinned" without a digest value. The rule now asserts a property of an artifact in the same document that the artifact contradicts. Recorded as **E3**, not re-argued as a pair. |
| **ADV2-17** | Four state names, three-state enumeration | **Closed** | AD-22 L210 is the single definition site for exactly four; AD-9 L132 and AD-10 L138 cite it ("**AD-22 owns the axis-state vocabulary, its denominator consequence, and its encoding, and AD-9 consumes it rather than restating it**"). |

**A.1: 16 Closed · 1 Partially closed · 0 Still open · 0 Regressed.**

### A.2 — The seven older partial closures

| ID | Pass-3 verdict | Verdict now | Evidence |
| --- | --- | --- | --- |
| **ADV-02** | Closed | **Closed** | Register now on AD-21's partition with a creator and a consult list. |
| **ADV-03** | Still open | **Closed** | AD-5 L108: "Its **membership at provisioning is the set of app IDs the operator artifact declares for that tenant** … adding or removing an app ID on a live tenant is an AD-6 grant-amendment lifecycle operation and not an edit to the tenant resource … **Revocation propagates within a stated bound** … the same bound AD-15 imposes on a rotated credential". Ledger row L387. |
| **ADV-04(b)** | Closed | **Closed** | See ADV2-01/AD-22. |
| **ADV-05** | Still open | **Closed** | AD-22 L210's adapter reporting statement plus AD-10 L138: "**Safety is decided once per query by the server's axis-selection step, never independently by each adapter, and it is decided only from the facts AD-22 obliges every adapter to report**". |
| **ADV-08** | Still open | **Closed** | See ADV2-08. |
| **ADV-09** | Closed | **Closed** | Owner named; register ledgered (L378). |
| **ADV-10(c)** | Closed for the prefix | **Closed** | Registry L241 declares `dedup:` "as shipped"; L243 adds the granularity rule ADV2-16 left open: "**a row's protected resource is named at the granularity of the durable artifact it guards** — a store, index, graph, or key family — never at the granularity of an operation". |

**A.2: 7 Closed.**

### A.3 — The 15 pass-3 pairs and erratum E1

| ID | Verdict | Evidence in the current text |
| --- | --- | --- |
| **E1** | **Closed** | AD-10 L138 no longer contains a biconditional. It reads "An axis can respond **safely** when its adapter applied the request's authoritative tenant and case scope … and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`. **Completing within configured limits is not a condition of safety**". The phrase "without truncation" has zero occurrences in the artifact. |
| **ADV3-01** | **Closed** | AD-21 L204: named bootstrap step, initialized marker, monotonic sequence, positive-fact availability, six fail-closed paths including provisioning, Health row L229 asserting the marker. Residue at the *runner's* identity — **ADV4-07**. |
| **ADV3-02** | **Closed** | AD-22 L210 generalises `truncated` to any axis and adds the uncovered-scope-unit count. Residue at zero hits — **ADV4-02**. |
| **ADV3-03** | **Closed** | AD-14 L162's ratified Phase 1 clause resolves the bivalence in favour of allocate-and-activate-atomically, and AD-3 L96 defines the no-staging branch. Residues — **ADV4-03**, **ADV4-19**. |
| **ADV3-04** | **Closed** | AD-6 L114: "**AD-6 owns the tenant lifecycle state machine and is its sole writer**", states ratified onto the shipped `Contracts.V1` `TenantStatus` enum (verified: `src/Hexalith.Memories.Contracts/V1/TenantStatus.cs:12` ships `Provisioning`, `Active`, `Deleting`, `Failed`, `CompensationFailed`), `Erased` "set **from** AD-21's register rather than beside it", and "provisioning and lifecycle repair refuse any tenant in `Deleting` or `Erased`, so no compliant repair can re-create an erased tenant's ACL principal, graph, telemetry partition, or grant". The pass-3 repair artifact is dead. Residues — **ADV4-01**, **ADV4-15**. |
| **ADV3-05** | **Closed** | AD-20 L198: "**at least one** of a current re-runnable evidence path or a dated exception recorded in the single exception register"; "Where the tracker of record is frozen, the architecture owner records the tracker obligation in the row itself with the freeze reference and a review date: the freeze suspends the tracker conjunct and no other". Ledger closing L401–L403 now attests the true state ("Every row above is `blocker`"). |
| **ADV3-06** | **Closed** | AD-20 L198: "**The architecture owner applies that predicate** … where a recorded classification and the predicate disagree, **the predicate governs** and the row is treated as active-foundation critical until the owner reclassifies it in a spine revision." The two units now converge. Residue: no row records its triggering clause, which the same sentence requires — **E4**. |
| **ADV3-07** | **Closed** | AD-5 L108: "**Exactly three exemptions from that fail-closed exist, and no other decision may add a fourth without amending this list.**" AD-17 L180 is now a pointer ("**AD-5's third and last exemption**"). |
| **ADV3-08** | **Closed** | AD-16 L174: "**Completion evidence is a content-free domain event committed through AD-2 on AD-21's platform stream** … AD-21's register holds the derived non-reuse tombstone and a reference to that event". |
| **ADV3-09** | **Closed** | AD-23 L216 (see ADV2-06) plus the AD-4 reconciliation: "That encoding is a composition step and never a normalization: AD-4 compares identities on the exact validated values". Residues — **ADV4-04**, **ADV4-14**, **ADV4-20**. |
| **ADV3-10** | **Closed** | AD-23 L216: lowercase, "RFC 1123 label rules **including the alphanumeric first and last character**", "**conformance to each listed scheme is asserted by a test in the issuance component, not by inspection**". Residue — **ADV4-14**. |
| **ADV3-11** | **Closed** | AD-12 L150: "`Hexalith.Memories.Contracts` owns **every public wire surface the platform serves under a `v1` route**, the AD-17 access-telemetry and clock plane included". Ledger row L398. |
| **ADV3-12** | **Closed** | AD-21 L204: "**The platform scope is the set of deployments that can serve one tenant population**"; Deferred L413 now states the register no longer rides on that component. Residue — **ADV4-17**. |
| **ADV3-13** | **Closed** | AD-21 L204: monotonic sequence, artifact stamping, `>=` admission, "Times recorded in the register are descriptive evidence and never an admission-control input, and no access-telemetry service participates in that decision." Residues — **ADV4-09**, **ADV4-10**. |
| **ADV3-14** | **Closed** | AD-16's Binds L172 now opens with "Tenant provisioning"; AD-6's Rule L114 carries the consult; AD-6's Binds L112 names "the AD-21 erased-tenant register consult". |
| **ADV3-15** | **Partially closed** | The preamble now states the prohibition — L352: "**A row's `Required convergence` restates what the cited Rule already requires and may not introduce a requirement absent from every Rule, nor offer a choice between architectural outcomes**" — and AD-23 L216 moved validate-on-read into the Rule, closing the normative-content half. **The choice half survives in the same table the preamble governs**: L362 still requires "move coordination to Dapr state **or** adopt an explicit `AD-n` exception" and L397 "Separate the reservation prefix from the durable-dedup prefix, **or** migrate the reservation into the durable mechanism" — two architectural outcomes, offered in the column that now forbids offering any. Recorded also as **E5**. |

**A.3: 14 Closed · 1 Partially closed · 0 Still open.** E1 Closed.

### A.4 — Retest totals

| | Closed | Partially closed | Still open | Regressed |
| --- | --- | --- | --- | --- |
| ADV2-01 … ADV2-17 (17) | 16 | 1 | 0 | 0 |
| ADV-02/03/04/05/08/09/10 (7) | 7 | 0 | 0 | 0 |
| ADV3-01 … ADV3-15 (15) | 14 | 1 | 0 | 0 |
| E1 (1) | 1 | 0 | 0 | 0 |
| **Total (40)** | **38** | **2** | **0** | **0** |

For context: pass 3 closed 8 of 24 retested items. This pass closed 38 of 40. The class-level method is the difference, and the difference is not marginal.

---

## Part B — Testing this pass's own diagnosis, fix by fix

Pass 3 diagnosed: *"three of the four highest-value fixes were scoped to the exact case the prior finding exhibited, and the narrowing un-covered the general case,"* and recommended that pass 4 *"test each proposed sentence against the complement of the exhibited case before adoption."*

**Verdict: the complement test was run on the reported findings and was not run on the new text written to close them.** Every closure in Part A holds for its complement — that is why 38 of 40 closed. But 14 of the 20 new pairs in Part C are in sentences dated 2026-09-12 by this pass, and each of them is the *adjacent* case of a fix, not the fix's own case. The failure signature moved one step outward: from **narrowing a fix** to **not propagating a fix into the rule next to it**.

| Fix | Exhibited case it was tested against | Complement it was not tested against | Escapes as |
| --- | --- | --- | --- |
| **D1 / AD-21** — write authorization: "writes are admitted only from the erasure workflow's operator-scoped principal" | The erasure workflow writing a tombstone — the one writer every finding exhibited (ADV3-01, ADV3-08, SEC3-05) | The **other three writers the same Rule's own contents list implies**: AD-6's lifecycle projection, AD-16's bundle registration at export creation, AD-17's purge-completion record | **ADV4-01 (critical)** |
| **D1 / AD-21** — "created and initialized by a **named** platform-bootstrap step" | A deployment that never created the register (the exhibited fresh-cluster case) | A deployment whose step is the **server's own startup**, which creates a marker on a fresh or misdirected partition and reports the register available-and-empty | **ADV4-07 (high)** |
| **D1 / AD-21** — the sequence stamp on "every backup, export bundle, and EventStore restore artifact" | A register restored older than the data (the exhibited ADV3-13 case) | AD-16's **bundle-registration sentence**, which still enumerates "tenant, issuance time, and content digest" and no sequence; and a **restore gap**, in which a re-advanced sequence is no longer injective | **ADV4-10 (high)**, **ADV4-09 (high)** |
| **D2 / AD-5** — the exhaustive list of ten enumerated lifecycle operations | The operations the findings named: provisioning, repair, erasure, telemetry partition, grant amendment | **Tenant deletion** (AD-6's `Deleting` state and its Binds "deletion") and the **AD-4 tenant content store**, neither of which appears in the exhaustive list | **ADV4-13 (high)** |
| **D3 / AD-14** — Phase 1 allocates a new epoch, activates atomically, and the fence's staging redirect is Phase 2+ | **Ordinary ingestion** writing under the active epoch during the rebuild — the case ADV3-03 exhibited | **The rebuild's own writes**, which are by construction non-active-epoch writes, in a phase the same sentence says declares no staging redirect; and the **syntactic index, graph and tombstone** stores, for which "the rebuild epoch" declares nothing | **ADV4-03 (critical)**, **ADV4-19 (medium)** |
| **D4 / AD-22** — `truncated` is retained with its hits and contributes denominator weight | A **truncated axis with hits** — the ADV3-02 case | A **truncated axis with zero hits**, which AD-9's normalization sentence excludes ("weights of axes that returned results") and AD-9's AD-22-citing clause includes | **ADV4-02 (critical)** |
| **D4 / AD-12** — the packet's canonical encoding: explicit `null` for unavailable, empty collection for available-with-no-hits | The **two states AD-12 already knew about** | The **two states AD-22 just added**: `truncated` and `excluded` have no declared payload shape, and AD-12's own withheld-detail clause instructs omission where the same Rule forbids omitted properties | **ADV4-11 (high)**, **ADV4-12 (high)** |
| **D5 / AD-20** — evidence path only, "at least one", tracker-freeze clause | The exhibited two-party fiction and the attestation | Passed. The only residue is two ledger rows that still offer architectural choices the new preamble forbids | **ADV3-15 partial / E5** |
| **Class sweep 2** — "a rule that names a mechanism the architecture has not yet defined carries a ledger row naming that mechanism" (L76) | The five artifacts §5.1 named, plus the six the sweep found | AD-4's "**a declared bounded character set**" for CloudEvent `source` and `id` — a mechanism named in a Rule, defined nowhere, and absent from every ledger row, so the preamble's own escape clause makes it ungoverned | **ADV4-20 (low)** |
| **Class sweep 3** — one definition site and one encoding per vocabulary | Axis states, packet states, status values, dispositions, tenant states — all verified single-site | The **payload shapes** of the newly single-sited axis vocabulary (ADV4-11) and the **`0` sentinel** of AD-22's uncovered-scope-unit count | **ADV4-11 (high)** |

Two further observations on the method.

- **The sweep verified existence, not consistency.** Class sweep 2 asked "does this mechanism have a definition or a ledger row?" and every answer is now yes. It did not ask "do the two decisions that name this mechanism agree about it?" — which is the question ADV4-01, ADV4-06, ADV4-08, ADV4-10 and ADV4-13 all answer no.
- **The pass blessed a shipped artifact it misread.** AD-23 L216, AD-4 L102 and Registry L241 all rest on the claim that the shipped reservation key hashes CloudEvent `source`. It does not; it hashes `id`, and `source` is absent from the key entirely (`EventIngestionService.cs:144`). A brownfield-reality check was performed and recorded in the memlog, and it got this one backwards. See **E2** and **ADV4-04**.

---

## Part C — New divergence pairs against the amended text

Severity-ordered. Twenty pairs: 5 critical, 9 high, 5 medium, 1 low.

---

### ADV4-01 — critical — AD-21 admits writes from exactly one principal, and three of the four things its partition holds are written by somebody else

**Unit A — Tenant lifecycle unit** (AD-6's workflows; also, in instances (b) and (c), the FR71 export unit and AD-17's retention unit).
**Unit B — Platform-partition authorization unit** (AD-21's append-only write admission; the epic closing ledger row L384).

**Compliance proof.** AD-21 L204 states what the partition holds and then admits exactly one writer:

> "It holds AD-16's erased-tenant register, the export-bundle index, erasure completion evidence, and **AD-6's content-free lifecycle-state projection**."

> "**Reading and writing it is an authorization decision:** writes are admitted only from **the erasure workflow's operator-scoped principal** and are append-only — EventStore's append-only semantics are the enforcement, not a convention, so no application principal can modify or delete a tombstone — while reads are admitted to the enumerated consult paths and to operator principals."

Three other decisions oblige three other writers to write that same partition:

> AD-6 L114: "**AD-6 owns the tenant lifecycle state machine and is its sole writer.** … a content-free projection of each tenant's identifier, current state, and transition time **is mirrored onto AD-21's platform partition**, so lifecycle state stays readable after erasure".
> AD-16 L174: "each bundle **is registered at creation in AD-21's bundle index** with its tenant, issuance time, and content digest".
> AD-17 L180: "the mapping is destroyed when the last record it addresses is purged — **at which point purge completion for that tenant is recorded in AD-21's register**."

AD-17's own exemption is additionally bounded to acts that exclude writing: "The exemption is bounded to those operations: **it permits counting and deleting records, and reading the erasure mapping solely to locate them**".

Unit A performs the three writes its own decisions require of it. Unit B refuses every write that is not from the erasure workflow's operator-scoped principal, exactly as AD-21 instructs, and its refusal is the enforcement AD-21's `Prevents` depends on ("one deleted tombstone silently re-enabling tenant-ID reuse; one spurious tombstone becoming an irreversible denial of tenant creation"). Neither unit violates the decision it implements. The write path has two owners and the partition has one admitted principal.

**The concrete incompatibility.**

**(a) The shred-surviving projection is never written, so every post-erasure authorization decision fails closed forever.** Provision `acme-legal`. AD-6 commits `Provisioning` then `Active` on `memories-tenants` and mirrors each transition to the platform partition; Unit B rejects both mirrors. AD-6 L114 says "authorization decisions read the committed state **or that projection** and never a cached copy" — so after erasure the only remaining source is the committed state on a stream whose key AD-16 destroyed. AD-17's retention exemption then cannot resolve the tenant it is exempted to service: the erased tenant's telemetry is unpurgeable, which is the precise outcome AD-5's third exemption exists to guarantee and which AD-16 L174 already declined to force ("Deletion does not wait for or force early deletion of retained opaque access telemetry"). The projection whose only purpose is to survive the shred is the one write the partition will not accept.

**(b) FR71 import is dead on arrival, in both directions.** AD-16 admits a bundle "only when its digest is present in the index and unmarked". Under Unit B no bundle-creation write is admitted, so the index is permanently empty and **no export bundle can ever be imported** — a shipped FR71 capability (ledger row L389 confirms it "has already shipped") reduced to export-only by an authorization rule in a different decision. Under the alternative, export — ordinary tenant work performed under a tenant grant — is granted the erasure workflow's operator-scoped principal, which is the widest privilege in the platform.

**(c) The remedy is worse than the defect.** The only compliant way to satisfy both units is to issue the erasure workflow's operator-scoped principal to the lifecycle workflow, the export path, and Platform Operations. That credential can append an erased-tenant tombstone, and AD-21 makes a tombstone irreversible except by "an operator procedure that **appends a recorded reversal**" which ledger row L384 records as non-existent. A bug in ordinary provisioning can then permanently deny creation of a tenant identifier — AD-21's own second `Prevents` clause, realized by the fix for its first.

**Proposed replacement — AD-21 Rule, replace the write-authorization sentence:**

> "**Reading and writing it is an authorization decision, and each of its four contents has exactly one admitted writer:** the erased-tenant register and erasure completion evidence are written only by the erasure workflow's operator-scoped principal; AD-6's lifecycle-state projection only by the lifecycle workflow's operator-scoped principal; the export-bundle index only by the export-creation path acting under the tenant's own authority, which may write a registration and may never write a tombstone; and AD-17's purge-completion record only by Platform Operations' operator-scoped principal, which is thereby permitted this one write and no other. Every write is append-only — EventStore's append-only semantics are the enforcement, not a convention — and **no writer may append a record of a kind it does not own**, so no principal but the erasure workflow can create or reverse a tombstone. A write from a principal that owns no kind is refused, and the refusal is an operator-visible condition rather than a silent drop."

---

### ADV4-02 — critical — A `truncated` axis with zero hits: AD-9's two sentences about the denominator disagree, and the disagreement is the same 1.43× divergence this gate has now carried for four passes

**Unit A — Fusion engine unit** (AD-9's weighted sum; the epic closing ledger row L363).
**Unit B — Evidence Packet and axis-health unit** (AD-12's packet; the same fused score is reported as FR63 confidence).

**Compliance proof.** AD-9 L132 states the normalization rule in one sentence and the AD-22 consequence in another, and they quantify different sets:

> "Normalize the weighted sum by top-rank contribution times **weights of axes that returned results**, then sort by descending composite score and ascending `StringComparer.Ordinal` ID."

> "**AD-22 owns the axis-state vocabulary, its denominator consequence, and its encoding, and AD-9 consumes it rather than restating it:** an axis in `available` with hits **or in `truncated`** contributes its weight to the denominator, and one in `unavailable`, `excluded`, or `available` with no hits contributes none."

AD-22 L210 confirms the unconditional form: "`truncated` … it is **retained with its hits, contributes denominator weight**, and is disclosed as degraded." AD-22 also makes the zero-hit case explicit rather than exotic: `truncated` covers "a timed-out or provider-cut-off syntactic or vector scan", and a timeout that fires before the provider's first batch returns yields a truncated axis with an empty hit list. AD-18 L186's tenant-partitioned admission and provider throttling make that the expected case under load.

Note that the enumeration of *non*-contributors is exhaustive and does not contain "truncated with no hits". So:

- Unit A implements the AD-22-citing clause literally: `truncated` is in the contributing set, unqualified; denominator includes its weight.
- Unit B implements the normalization sentence literally: an axis that returned no results is not among "axes that returned results"; denominator excludes its weight.

Both quote AD-9. Neither quotes a sentence the other can strike.

**The concrete incompatibility.** One query, one loaded tenant. Default weights `0.30 / 0.35 / 0.35`. The syntactic axis times out at zero hits with tenant and case scope verified on the work it did (`truncated`, uncovered-scope-unit count `0` with a stated cutoff reason, per AD-22). Semantic and graph both return, and both rank document `d1` first, so `d1`'s contribution on each is `1/(10+1) = 0.0909…`.

| | Unit A | Unit B |
| --- | --- | --- |
| Weighted sum for `d1` | `0.0909×0.35 + 0.0909×0.35 = 0.06364` | identical, `0.06364` |
| Denominator | `0.0909 × (0.30+0.35+0.35) = 0.09091` | `0.0909 × (0.35+0.35) = 0.06364` |
| Composite / FR63 confidence for `d1` | **0.700** | **1.000** |

Every score differs by `1.00/0.70 = 1.4286×`, and FR63's 0.0–1.0 confidence moves by 30 points of its range for an identical result set. Ordering inverts wherever axis coverage differs between documents: a document found by semantic only (`0.0909×0.35 = 0.03182`) scores `0.350` under Unit A and `0.500` under Unit B, while a document found by graph only at rank 3 (`1/13 × 0.35 = 0.02692`) scores `0.296` / `0.423` — the gaps between neighbouring documents change by 43%, which is enough to reorder any pair whose raw contributions are within that band. This is the identical divergence class recorded as ADV-04(b), ADV2-01 and ADV3-02, now surviving on the complement of the case D4 was tested against.

Secondarily, G1 becomes unmeasurable in the same way pass 3 described: the harness does not exist (ledger row L364), so whichever fusion epic ships first fixes the meaning of the gate, and under load the two units measure two different systems.

**Proposed replacement — AD-9 Rule, replace the normalization sentence and the AD-22 clause with one sentence that quantifies one set:**

> "Normalize the weighted sum by top-rank contribution times the sum of the weights of exactly the **denominator-contributing** axes, which AD-22 defines and AD-9 does not restate, then sort by descending composite score and ascending `StringComparer.Ordinal` ID."

**and — AD-22 Rule, replace the `truncated` clause:**

> "`truncated` … it is retained with whatever hits it produced, **contributes denominator weight whether or not it produced any**, and is disclosed as degraded — a truncated axis with an empty hit list is the ordinary timeout case and contributes weight exactly as one with hits does, because the axis was selected and did attempt the work."

*(Either direction closes the pair; the choice is a product decision about whether a dead-on-arrival axis should deflate scores. It must be made once, in AD-22, and cited nowhere else.)*

---

### ADV4-03 — critical — The Phase 1 rebuild's own writes are non-active-epoch writes in a phase that declares no per-store staging resource, so the AD-3 fence either admits them or rejects every one of them as `Failed`

**Unit A — FR43 degraded rebuild command unit** (the ratified MVP epic; ledger row L395).
**Unit B — Projection coordinator and write-fence unit** (AD-3's CAS checkpoints and conditional writes; ledger row L357).

**Compliance proof.** AD-14 L162, written this pass, makes the rebuild write under a **non-active** epoch:

> "**That rebuild allocates a new `embeddingConfigurationEpoch` and writes under it, and activates it atomically for the tenant when its verification succeeds** — it is the sole writer permitted to target the new epoch, **ordinary ingestion continues under the epoch that is active until the moment of activation**, and the tenant's reported ingestion state and AD-10's axis safety are evaluated against the active epoch alone throughout".

The same Rule then declares staging per store and per phase, and declares exactly one thing for Phase 1:

> "**Staging resources are declared per derived store and per phase, not assumed:** any phase in which a non-active generation or epoch may be written declares the disjoint staging resource **for every store AD-3's fence covers — syntactic index, vector index, graph, and tombstones** — and AD-3 rejects a non-active write outright wherever a phase declares none. **Phase 1's only declared staging resource is the rebuild epoch above**; the AD-3 fence's redirect to staging is therefore a **Phase 2 and Phase 3 clause by explicit statement**, not by silence."

AD-3 L96 supplies the branch that fires when a phase declares none:

> "**Where the phase declares no staging resource for the store being written, a write carrying a non-active, non-declared-staging tuple is rejected as out-of-generation and surfaced as an actionable `Failed` reprojection** — never silently dropped, and never redirected to a resource the phase has not defined."

Two literal readings, both quoting AD-14's own words:

- **Unit A** reads "Phase 1's only declared staging resource is the rebuild epoch" as the per-store declaration for all four stores: the epoch is store-agnostic, so a per-epoch resource exists for the syntactic index, the vector index, the graph and the tombstones, and the rebuild writes into them.
- **Unit B** reads "declared **per derived store**" and "the AD-3 fence's redirect to staging is a **Phase 2 and Phase 3 clause by explicit statement**": in Phase 1 there is no redirect, no store has a declared staging resource, and therefore AD-3's rejection branch is the operative one for every non-active-epoch write — including the rebuild's own.

Unit B's reading is the stronger of the two, because it is the one the sentence states explicitly rather than by implication, and because AD-3's rejection branch says in terms that a non-active write is "never redirected to a resource the phase has not defined".

**The concrete incompatibility.** Tenant `acme-legal`, 100,000 memory units, embedding model changed from `gemini-embedding-001`/768 to a 1536-dimension model. The operator runs the FR43 acknowledged degraded rebuild.

- **Unit A**: the rebuild allocates epoch 2, writes 100,000 documents into per-epoch staging resources across all four stores, verifies, and activates in a single AD-2 commit. Ordinary ingestion continues under epoch 1 throughout. Reported ingestion state and axis safety stay on epoch 1. This is exactly the behaviour D3 ratified.
- **Unit B**: the rebuild's first vector write carries epoch 2, which is neither the stored document's epoch nor the tenant's active epoch, and the phase declares no staging resource for the vector index. The fence rejects it "as out-of-generation" and surfaces "an actionable `Failed` reprojection". Repeat 100,000 times across four stores: **400,000 actionable `Failed` reprojections and an FR43 command that cannot succeed on any tenant in any phase**, because the only phase in which FR43 is permitted is the phase in which its writes are rejected.

The artifact is not a slow path or a degraded path. It is a ratified MVP command that one compliant implementation performs and another compliant implementation forbids, and the forbidding one is reading the sentence that was added to close ADV3-03.

A second, narrower instance survives even under Unit A's reading, and it is the one ADV3-03 exhibited: AD-4 L102 requires an activity to "Capture immutable non-secret configuration plus its epoch at start". An in-flight projection activity that captured epoch 1 and retries after activation carries a tuple that is neither the stored document's nor the active one. Under Unit A it lands in epoch-1 staging (a resource the retire step is about to delete); under Unit B it is an actionable `Failed`. The difference is visible to the operator as 100,000 units `Indexed` versus some thousands `Failed`.

**Proposed replacement — AD-14 Rule, replace the Phase 1 staging sentence:**

> "**Phase 1 declares one staging resource per derived store** — a per-epoch syntactic index, a per-epoch vector index, a per-epoch graph, and a per-epoch tombstone family, each disjoint from the active resource and each named by the allocated `embeddingConfigurationEpoch` — and the Phase 1 rebuild is the sole writer permitted to target them. The AD-3 fence's redirect therefore operates in Phase 1 as it does in Phase 2 and Phase 3, and what is Phase-2-and-3-only is the create-backfill-verify-switch-retire *migration contract*, not the existence of a staging target. A phase that declares no staging resource for a store admits no non-active write to that store at all, which AD-3 enforces by rejecting it as an actionable `Failed` reprojection; **no phase may permit a non-active write and decline to declare its target.**"

---

### ADV4-04 — critical — AD-23 blesses a shipped key as satisfying injective composition; the shipped key hashes the CloudEvent `id` and omits `source` entirely, and AD-4 requires both

**Unit A — Dapr pub/sub ingestion unit** (`/events/ingest`; `EventIngestionService`, the key family the spine blesses).
**Unit B — REST ingress reservation unit** (`IngestDedupReservation` / `DedupKeyBuilder`, the other shipped composer of the same prefix).

**Compliance proof.** AD-4 L102 scopes CloudEvent identity over four components and says the shipped key already satisfies AD-23:

> "scope CloudEvent identity by tenant, case, **validated `source`, and `id`** … Because neither is issued under AD-23's grammar, neither is composed into a key in raw form — AD-23's injective-composition rule governs how they enter one, **which the shipped reservation key already satisfies by hashing `source`**."

AD-23 L216 repeats the blessing as its worked example:

> "every component that is not itself issued under the grammar — CloudEvent `source` and `id`, `IdempotencyToken`, principal identifiers, handle scopes — is length-bounded and either reversibly encoded into the grammar's alphabet or replaced by a fixed-length hash of itself **before** composition, **which the shipped reservation key family `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` already does for `source`**."

Registry L241 records the same family and the same claim: "so `source` is already hashed per AD-23 before composition".

The shipped code hashes the **`id`**, not the `source`, and the `source` is not in the key at all:

> `src/Hexalith.Memories.EventStore/EventIngestionService.cs:144` — `string dedupKey = EventStoreDedupKey.Build(route.TenantId, route.CaseId, envelope.Id);`
> `src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs:17` — `=> $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";`
> `src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs:14` — documents the truth: "typically `dedup:{tenantId}:{caseId}:{sha256(cloudEventId)}`".

The parameter is *named* `sourceUri` and is *passed* `envelope.Id`. Line 152 then uses the same string as the durable workflow instance id (`string instanceId = dedupKey;`), so AD-4's "durable EventStore/workflow duplicate suppression" is keyed the same way.

Unit A composes the family the spine blesses by name and line number. Unit B composes AD-4's stated identity, which requires `source` and `id`; the shipped REST builder already distinguishes a natural-key family (`BuildKey`, over `sourceUri`) from a token family (`BuildTokenKey`, `dedup:{t}:{c}:tok:{hash}`), so Unit B has two of the four components in one family and the spine's blessed family has a different two. Neither unit violates a Rule sentence: one implements the example, the other the definition.

**The concrete incompatibility.**

**(a) Silent, success-reported loss of a real event, for 24 hours, on the MVP-active ingestion path.** The routing map is prefix→tenant and explicitly many-to-one (`TenantEventRoutingOptions.SourceToTenantMap`, longest prefix wins), and case auto-creation is keyed by `(tenantId, aggregateType)` with `AutoCreateCases` defaulting to `true`. So two publishers routed to one tenant and one case is the ordinary configuration, not an exotic one. Publisher `https://erp-a.acme/` emits `{"id":"1"}`; publisher `https://erp-b.acme/` emits `{"id":"1"}` — sequential per-publisher ids are the common case for ERP and ledger emitters.

- Unit A: both events produce `dedup:acme:events-order:sha256("1")`. The first reserves; the second returns `PreflightReservationResult.Duplicate`, and `EventIngestionService` returns `EventIngestionOutcome.Duplicate` / `EventIngestionResponse.Duplicate()` at line 155–163 — the publisher is told the event was handled. The event never reaches EventStore, never reaches a projection, and the reservation TTL is `PreflightDedupTtl = TimeSpan.FromHours(24)`. AD-4's "Redis preflight reservation is fail-open admission optimization only" does not rescue it: fail-*open* covers Redis being down, not a successful reservation collision, and the workflow instance id is the same string, so the durable layer suppresses it too.
- Unit B: the two events produce distinct identities and both are ingested.

One publisher's day of events is discarded under Unit A and ingested under Unit B, and the spine's own text points implementers at Unit A.

**(b) The blessing is load-bearing for AD-23's injectivity proof and is false.** AD-23's claim that composition is injective over all inputs rests, for the CloudEvent case, on the assertion that `source` is hashed. `source` is absent. Composition is injective over `(tenantId, caseId, id)` and is not a function of AD-4's identity at all, so the rule's only worked example demonstrates the opposite of what it claims. An implementer who checks the citation finds the code disagrees with the spine and must choose which to trust.

**(c) Two shipped composers already disagree, under one reserved prefix.** `dedup:{t}:{c}:{sha256(sourceUri)}` (REST natural key), `dedup:{t}:{c}:tok:{sha256(token)}` (REST token key) and `dedup:{t}:{c}:{sha256(id)}` (pub/sub) are three families in one prefix, of which two are byte-indistinguishable in shape while keying different things. Registry L241 declares one family and ledger row L397 records only the reservation/durable-dedup sharing, so the third family is governed by nothing.

**Proposed replacement — AD-4 Rule, replace the blessing clause; Registry L241, replace the Scope cell:**

> AD-4: "… Because neither is issued under AD-23's grammar, neither is composed into a key in raw form — AD-23's injective-composition rule governs how they enter one, and **the composed identity is a function of all four components**: tenant, case, `source` and `id` each enter the key, the two externally supplied ones as fixed-length hashes. **The shipped reservation key is a known violation of this rule, not an example of it**: `EventIngestionService.cs:144` composes `dedup:{tenantId}:{caseId}:{sha256(id)}` and omits `source`, so two publishers routed to one tenant and case collide on equal `id` values and one real event is suppressed as a duplicate; that defect carries its own ledger row and its convergence is the four-component key."

> Registry Scope cell: "Reserved key prefix `dedup:`, as shipped. Three families currently share it — `dedup:{tenantId}:{caseId}:{sha256(id)}` (`EventIngestionService.cs:144`, pub/sub reservation **and** durable workflow instance id), `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` and `dedup:{tenantId}:{caseId}:tok:{sha256(token)}` (`DedupKeyBuilder.cs:16,24`) — which is one prefix claimed by three mechanisms with two failure postures and one omitted identity component; ledgered, not approved. `tenantId` and `caseId` rely on AD-23's grammar excluding the `:` delimiter."

**and a new ledger row** naming the omitted `source` component, classified `Yes`.

---

### ADV4-05 — critical — AD-5 forbids deriving tenant authority from the envelope and then offers a routing map keyed on an envelope field, so one compliant ingress accepts a cross-tenant claim the other refuses

**Unit A — Channel-bound ingress unit** (a Dapr subscription component or topic provisioned per tenant).
**Unit B — Routing-map ingress unit** (the shipped `TenantEventRouter` plus the operator artifact's map; the epic closing ledger row L386).

**Compliance proof.** AD-5 L108 states the prohibition and the alternative in one sentence:

> "Dapr pub/sub ingress **derives tenant authority from the delivery channel and never from the envelope**: the receiving component or topic is bound to one tenant, **or** the operator artifact's routing map resolves the delivery's authenticated channel to one tenant, and the map is matched **ordinally** under AD-23's grammar rather than case-insensitively."

The map the spine means is the shipped one, and its keys are CloudEvent `source` prefixes — an envelope field. Ledger row L386 confirms both the identity of the artifact and that the convergence keeps it:

> "The shipped pub/sub tenant routing map matches CloudEvent `source` prefixes with `StringComparer.OrdinalIgnoreCase` (`TenantEventRoutingOptions.cs:21`) … **Match the routing map ordinally**, reject a delivery that resolves to no tenant or to an inactive one".

Verified: `TenantEventRoutingOptions.cs:19-21` — "Gets the map of CloudEvents `source` prefix → tenant id. Longest-prefix wins, case-insensitive."

- **Unit A** implements the first disjunct: one component or topic per tenant, authority from the authenticated channel, `source` ignored for authorization. Compliant, and it is the only reading under which "never from the envelope" is true.
- **Unit B** implements the second disjunct and the ledger row: longest-prefix match over the CloudEvent `source`, ordinal. Compliant with the sentence's second half and with the only convergence instruction the ledger gives.

Neither violates a Rule. The sentence forbids envelope-derived authority in its first clause and supplies an envelope-keyed mechanism in its second.

**The concrete incompatibility.**

**(a) Cross-tenant ingestion by a publisher that controls one envelope field.** MVP topology is "single topic per deployment" (`TenantEventRoutingOptions.cs:16`). Any workload authorized to publish to that topic sets `source` itself. Map: `https://erp.acme/` → `acme`, `https://erp.contoso/` → `contoso`. A publisher legitimately authorized for `contoso` emits an event with `source: "https://erp.acme/orders/9"`.

- Unit A: the delivery arrives on `contoso`'s channel; authority is `contoso`; the envelope's claim is irrelevant, and AD-5's "An envelope tenant the channel does not authorize is rejected" fires if the envelope names `acme`.
- Unit B: the map resolves `acme`; the content is ingested into `acme`'s case, projected into `acme`'s indexes and graph, and returned to `acme`'s users as an authentic memory unit with `acme` provenance. NFR8's isolation suite, which authenticates as tenant and operator principals, never exercises a publisher that lies about `source`, so nothing catches it.

That is cross-tenant content injection reached by a fully compliant implementation of the second disjunct — AD-5's own `Prevents` list names it ("Request fields, **envelope fields**, … from serving as authorization").

**(b) "Matched ordinally under AD-23's grammar" cannot be satisfied as written, and the two escapes differ.** AD-23's grammar is lowercase ASCII excluding every delimiter and metacharacter in URL path segments; a CloudEvent `source` prefix is a URI reference containing `:` and `/` and cannot conform. Unit B(i) reads "under AD-23's grammar" as vacuous decoration and matches raw `source` prefixes ordinally. Unit B(ii) reads it as a constraint on the map's keys, in which case no real `source` can be a key, every delivery resolves to no tenant, AD-5 requires rejection, and **Phase 1.5's L1–L3 gates become unpassable**.

**(c) Ordinal matching relocates the case-collision the grammar sweep was supposed to end.** AD-23's remediation is scoped to *issued identifiers*: "no two identifiers in the platform may differ only by case after the sweep". Map keys are not issued identifiers. Two entries `https://erp.acme/` → `t1` and `https://ERP.acme/` → `t2` are one key under the shipped `OrdinalIgnoreCase` map (the second silently overwrites or throws on insert) and two distinct keys after the ordinal convergence — and AD-4 L102 canonicalizes `source` at ingress ("scheme and host lowercased, no other change") without stating whether routing happens before or after. A delivery from `https://ERP.acme/orders` routes to `t2` if matched pre-canonicalization and to `t1` if matched post-canonicalization. Same envelope, same deployment, two tenants, decided by an ordering no rule fixes.

**Proposed replacement — AD-5 Rule, replace the pub/sub sentence:**

> "Dapr pub/sub ingress derives tenant authority from the delivery channel and never from the envelope. The channel is either a receiving component or topic bound to one tenant, or an authenticated publisher identity — the app ID or workload credential the delivery was authenticated with — that the operator artifact's routing map resolves to exactly one tenant. **A CloudEvent `source`, being a publisher-controlled envelope field, is never an input to that resolution**; it remains provenance under AD-13 and an identity component under AD-4. The map's keys are authenticated channel identities, are matched ordinally, and are issued under AD-23's grammar so that no two differ only by case. An envelope tenant the channel does not authorize is rejected rather than ingested, an envelope whose channel resolves to no tenant is rejected, and the resolved tenant must be `Active` exactly as an internal call's must. The shipped `source`-prefix map is a known violation with its own ledger row, and its convergence is replacement by channel-identity resolution rather than an ordinal comparison of the same envelope field."

---

### ADV4-06 — high — Readiness now gates on the erasure authority, so a transient platform-partition read error either removes every replica from service or does not

**Unit A — ServiceDefaults health unit** (the Health row's readiness contract).
**Unit B — Query degradation unit** (AD-10's partial-result contract, NFR18).

**Compliance proof.** The Health row L229, written this pass, adds the marker to readiness:

> "Readiness requires authentication configuration, the Dapr control boundary, EventStore command availability, **and AD-21's platform-partition initialized marker reading back**; an individual query backend reports capability degradation rather than removing a server that can still produce an AD-10 safe available-axis response."

AD-21 L204 states a per-operation posture instead:

> "While it is unavailable, **tenant provisioning, tenant deletion, authoritative replay, EventStore restore, projection-store restore, and export re-import** each fail closed and remain resumable, **as an operator-visible condition with its own recovery procedure** rather than a silent pass".

Search, annotation, ingestion and every read path are absent from that list — deliberately, since none of them can resurrect an erased tenant's content.

Unit A fails readiness when the marker does not read back, exactly as the Health row requires. Unit B keeps the server serving and fails the six enumerated operations closed, exactly as AD-21 requires, and cites the Health row's own second clause about not removing a server that can still answer safely. Both comply.

**The concrete incompatibility.** A five-minute EventStore platform-partition read error — a gateway restart, a network partition, a rolling upgrade of the external gateway the Stack table records as a Production-only dependency.

- Unit A: every replica fails `/ready`; Kubernetes removes all of them from the Service; **100% of search, retrieval, annotation and evidence traffic returns connection failures for five minutes**, and the operator-visible condition AD-21 promises is unreachable because the surface that would report it is out of service. NFR18's degradation contract and AD-10's "return a partial result when at least one axis can respond safely" are both defeated by an outage in a store neither of them reads.
- Unit B: search continues at full fidelity; `tenant create`, `tenant delete`, replay and the three restore paths return an actionable fail-closed error naming the register.

The availability difference is total, and the direction is perverse: the erasure authority is the one component whose unavailability has no effect on read safety, and it is the one this pass promoted to a readiness condition. It also creates a denial-of-service surface of the same family AD-21's `Prevents` already worries about — anything that can make the platform partition unreadable can take the whole product offline.

**Proposed replacement — Consistency Conventions, Health row:**

> "Liveness reports process viability. Readiness requires authentication configuration, the Dapr control boundary, and EventStore command availability. **AD-21's platform-partition initialized marker is asserted by a separate, named startup and periodic check whose failure is an operator-visible degraded condition and the fail-closed trigger for AD-21's six enumerated operations — it is not a readiness condition**, because a server that cannot reach the register can still produce an AD-10 safe available-axis response and removing it from service converts an erasure-authority outage into a total product outage. An individual query backend likewise reports capability degradation rather than removing such a server."

---

### ADV4-07 — high — AD-21's "named platform-bootstrap step" names nobody, so a compliant implementation makes it the server's own startup and re-creates the available-and-empty register the rule was written to abolish

**Unit A — Deployment bootstrap unit** (`deploy/kubernetes`, an operator job, alongside AD-15's bootstrap material).
**Unit B — Server startup unit** (the host that owns provisioning and readiness).

**Compliance proof.** AD-21 L204:

> "It is created and initialized by a **named platform-bootstrap step that runs before any tenant may be provisioned**, writing an explicit initialized marker that carries a monotonic register sequence; that step is a **deployment-time obligation owned alongside AD-15's bootstrap material** and is asserted by readiness under the Consistency Conventions' Health row."

No step is named; no owner is assigned beyond "alongside AD-15's bootstrap material", and AD-15's Binds L166 covers "bootstrap material" — key material, not a partition. AD-6's `Prevents` L113 forbids "Startup or ingestion hot paths implicitly creating **tenant** resources"; the platform partition is by definition not a tenant resource, so that prohibition does not reach it.

- Unit A: a deployment job creates the partition and writes the marker; the server never writes it and fails closed when it is absent.
- Unit B: the server writes the marker on startup if absent. It "runs before any tenant may be provisioned" — provisioning is served by that same server — it is a deployment-time obligation in the sense that it is part of standing the deployment up, and it makes readiness pass, which the same sentence requires. Nothing forbids it.

**The concrete incompatibility.** The register's guarantee is that availability is a positive fact. Unit B manufactures the positive fact.

Take a Production deployment whose EventStore connection string is changed during an upgrade to point at a fresh gateway instance, or a DR cluster brought up against an empty gateway, or a gateway restored from a backup that predates every erasure. Under Unit A the marker does not read back, the register is unavailable, and provisioning, replay and all three restore paths fail closed — the designed behaviour. Under Unit B the server finds no marker, writes one at sequence 0, reports ready, and the register is **available and empty**: `acme-legal`, erased on 2026-08-14, is immediately re-provisionable, and the tenant-ID-reuse artifact that ADV2-04 and ADV3-01 chased for two passes is live again with a third cause — this time produced by the very mechanism introduced to kill it.

The sequence guard does not catch it for provisioning (a tombstone lookup on an empty register simply misses) and catches restores only accidentally: an artifact stamped at sequence 41 fails against a live sequence of 0, which is correct, but the artifact stamp is itself absent from every bundle an AD-16-conformant exporter produces (**ADV4-10**).

**Proposed replacement — AD-21 Rule, replace the creation sentence:**

> "It is created and initialized by the platform-bootstrap step, a **deployment-time operator-invoked step that is never a server startup path and is never triggered by a request**, owned by the deployment assets alongside AD-15's bootstrap material, named in `deploy/` and in the operations documentation, and recorded in the ledger until it exists. The step is idempotent and **refuses to initialize a partition that is not provably empty**: it writes the initialized marker only when no marker and no register record exist, and otherwise verifies the existing marker and exits. **No other component may write the marker**, so a deployment pointed at an uninitialized, fresh, or mis-targeted partition reads no marker, is unavailable under this rule, and fails closed rather than initializing its way to an empty register."

---

### ADV4-08 — high — Two owners of one retire step: AD-14 assigns it to AD-3 and AD-3 assigns it to AD-14, so the superseded epoch's documents are nobody's to delete

**Unit A — Migration/rebuild unit** (AD-14's activation and retire).
**Unit B — Projection coordinator unit** (AD-3's checkpoint records).

**Compliance proof.** Each decision attributes the step to the other, in text written this pass.

> AD-14 L162: "Activation is a single AD-2 commit, **after which AD-3's retire step deletes the superseded epoch's documents and checkpoint records**."
> AD-3 L96: "Repair and replay use the same protocol, and **AD-14's retire step deletes the retired generation's and epoch's checkpoint records as part of its verified completion**."

Unit A reads AD-14 and waits for AD-3's coordinator to retire. Unit B reads AD-3 and waits for AD-14's migration to retire. Both are quoting the decision that binds them, and neither is in breach: each has been told the other owns it. The scopes also differ — AD-14's version deletes "documents **and** checkpoint records", AD-3's deletes checkpoint records only — so even a unit that decides to act has two different jobs to choose from.

**The concrete incompatibility.**

**(a) Nothing retires, and the failure is invisible until it degrades every query.** After the `acme-legal` rebuild activates epoch 2, the 100,000 epoch-1 documents remain in the vector and syntactic indexes and the graph. AD-10 L138 conditions safety on the adapter having "read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`". An adapter querying an index that still contains epoch-1 documents either returns some of them — in which case it must report that it did not read only the active epoch, and AD-22 makes the axis `unavailable`, nulling it — or it filters them, in which case it is doing per-document epoch filtering that AD-3's "exactly one current document per `(tenantId, caseId, MemoryUnitId)`" was written to make unnecessary. In the first case **every axis is nulled for the tenant after every successful rebuild**, and AD-10's "Fail the query only when no selected axis can produce a safe response" fails every query — permanently, with no operator signal, because the rebuild reported success.

**(b) Checkpoint records under the retired epoch keep answering.** AD-3's "exactly one checkpoint record exists per `(tenantId, caseId, memoryUnitId, schemaGeneration, embeddingConfigurationEpoch)`" leaves the epoch-1 records in place; AD-3's public-status rule reads the active epoch, so the stale records are inert but permanent, and storage grows by one full checkpoint set per rebuild forever. Ledger row L395 carries the rebuild's implementation and does not carry the retire step, so the omission has no convergence obligation either.

**Proposed replacement — AD-14 Rule, replace the activation sentence; AD-3 Rule, replace the retire clause:**

> AD-14: "Activation is a single AD-2 commit. **The retire step belongs to this decision and to the migration or rebuild that performed the activation**: as part of its verified completion it deletes the superseded epoch's and generation's documents in every derived store the AD-3 fence covers, and then the superseded checkpoint records, and reports the deletion counts per store; **the retire step is not complete until every superseded document is absent**, and AD-3's write fence rejects any write to a retired epoch thereafter. An activation whose retire step has not completed is an actionable `Failed` migration, not a successful one."

> AD-3: "Repair and replay use the same protocol, and **AD-14 owns the retire step; AD-3 only supplies the fence that rejects a write to a retired generation or epoch and the checkpoint semantics the retire step deletes under.**"

---

### ADV4-09 — high — The register's monotonic sequence is not injective across a restore gap, and "resumes at the highest sequence it can prove" has two readings that differ by every erasure in the gap

**Unit A — Register restore unit** (AD-21's reconciliation path).
**Unit B — Restore-admission unit** (AD-2's three recovery operations plus AD-16's import).

**Compliance proof.** AD-21 L204, written this pass:

> "The register carries a **monotonic sequence advanced on every write and never reset**; every backup, export bundle, and EventStore restore artifact records the sequence current at its creation, and a restore is admitted **only when the live register's sequence is greater than or equal to the artifact's**. **A register restored from backup resumes at the highest sequence it can prove**, and an artifact stamped higher fails closed until the register is reconciled."

"Can prove" is undefined, and the two available primitives give two behaviours:

- **Unit A**: proof is what the restored stream contains. The register resumes at the highest sequence present in it and advances from there on the next write. "Never reset" holds — the sequence only ever increases from the resumed value.
- **Unit B**: proof is the highest sequence the platform has ever observed stamped on any artifact. The register refuses to advance — and therefore refuses to admit any restore — until an operator reconciles it above that watermark.

**The concrete incompatibility.** The register reaches sequence 500. Tenants `t501` … `t540` are erased, advancing it to 540. The platform partition is restored from a backup taken at sequence 500; the 40 erasure records in the gap are gone.

- Unit A resumes at 500. The next erasure writes sequence 501 — **a second, different erasure now carries sequence 501**, so the sequence is monotonic and no longer injective. After 40 further erasures the live sequence is 540 again. Every backup and export bundle stamped 501–540 — created *after* the lost erasures and therefore containing erased tenants' data — now satisfies "the live register's sequence is greater than or equal to the artifact's" and is **admitted**. Forty erasures' worth of restore refusals and tenant-ID non-reuse silently evaporate, and the guard reports healthy throughout because the number went up.
- Unit B refuses every restore and every import until an operator reconciles, which is safe and also means the platform cannot restore anything during exactly the incident the backup exists for; and because no rule says where the watermark is stored, Unit B's watermark is itself lost in the same restore.

The equality test compares a **counter** where the guarantee needs **content coverage**. A sequence number proves ordering, not that the records between two numbers are present.

**Proposed replacement — AD-21 Rule, replace the sequence sentences:**

> "The register carries a monotonic sequence advanced on every write and never reset, **and every appended record carries the sequence it was written at, so the sequence is injective over records as well as monotonic**. Every backup, export bundle, and EventStore restore artifact records the sequence current at its creation. A restore is admitted only when the live register **contains an unbroken record sequence up to and including the artifact's stamp** — a live sequence number greater than or equal to the artifact's is necessary and not sufficient, because a register restored from an older backup can re-advance past a gap it no longer contains. A register restored from backup records an explicit **gap marker** naming the lowest and highest sequence it cannot account for; while any gap marker is open the register is **unavailable** for every path AD-21 lists, and it is closed only by an operator reconciliation that either recovers the missing records or records their irrecoverability, which is a one-way admission that every artifact stamped inside the gap is refused permanently."

---

### ADV4-10 — high — AD-21 requires every export bundle to carry a register sequence and AD-16 enumerates the bundle's registered fields without one, so a conformant exporter produces bundles no conformant importer can admit

**Unit A — Export/bundle-registration unit** (AD-16's FR71 path; ledger row L389).
**Unit B — Import admission unit** (AD-21's sequence rule plus AD-16's digest check).

**Compliance proof.** AD-16 L174 enumerates what registration records:

> "each bundle **is registered at creation in AD-21's bundle index with its tenant, issuance time, and content digest**, and its payload is wrapped under a per-bundle key escrowed under the tenant key".

AD-21 L204 imposes a fourth field on the artifact itself:

> "**every backup, export bundle, and EventStore restore artifact records the sequence current at its creation**, and a restore is admitted only when the live register's sequence is greater than or equal to the artifact's."

Unit A registers the three fields AD-16 enumerates. AD-16's enumeration reads as complete — it is the same closed-enumeration style the decision uses for its purge targets ("That set is **enumerated, and the enumeration is closed**"). Unit B requires the stamp AD-21 mandates and, finding none, cannot evaluate "greater than or equal to" at all.

**The concrete incompatibility.** Unit B has exactly two compliant dispositions for an unstamped bundle and they are opposite:

- Treat a missing stamp as unavailable/unknown and **fail closed**: no bundle produced by a conformant Unit A exporter is ever importable, and FR71 — a shipped capability — is export-only in perpetuity. The pre-mechanism sweep AD-16 mandates ("Bundles created before this mechanism are enumerated and re-registered or destroyed") re-registers them with tenant, issuance time and digest, so re-registration does not fix it either.
- Treat a missing stamp as sequence 0 and **admit**: every unstamped bundle passes the sequence guard unconditionally, which is the empty-register reading of the same guard AD-21 forbids elsewhere ("an absent marker … never an empty register").

Both dispositions are available because AD-16's enumeration and AD-21's requirement were written in different decisions on the same day and neither cites the other. This is the propagation failure in its purest form: the fix landed in AD-21 and the sentence it needed to amend is in AD-16.

**Proposed replacement — AD-16 Rule, replace the registration clause:**

> "each bundle is registered at creation in AD-21's bundle index with its tenant, issuance time, content digest, **and the register sequence current at its creation, which AD-21 requires of every such artifact and without which the bundle is unimportable**, and its payload is wrapped under a per-bundle key escrowed under the tenant key … Import admits a bundle only when its digest is present in the index, unmarked, **and its recorded sequence is covered by the live register under AD-21's admission rule**; a bundle carrying no sequence is refused rather than treated as sequence zero, and the pre-mechanism sweep stamps the sequence current at re-registration."

---

### ADV4-11 — high — AD-12 encodes two of AD-22's four axis states, so two compliant surfaces produce two byte sequences for one packet and G5's golden vector stays unauthorable

**Unit A — REST surface serializer unit.**
**Unit B — CLI/MCP surface serializer unit.**

**Compliance proof.** AD-12 L150 declares byte-equality and then supplies encodings for two state shapes:

> "**Equivalence is equivalence of the serialized document:** every element is always present on every surface, **an unavailable axis or absent value is an explicit JSON `null` and never an omitted property, an available-with-no-hits axis is an empty collection**, no surface enables null-omitting serialization for evidence-bearing types".

> "**Equivalence is byte-equality of one canonical document:** properties serialize in the name register's declared order, `double` values use round-trippable invariant `R` formatting, strings use minimal JSON escaping, and no surface reformats, reorders, or rounds the packet".

AD-22 L210 defines four states and reserves only the *values*, not their payload shapes:

> "`truncated` … **it is retained with its hits** … `excluded` — the axis **was not selected for this query**, which is a different fact from being unable to answer and is never encoded as the same value. … The four values and their packet shapes are reserved as a closed set in AD-12's name register".

AD-22 defers the shapes to the register; the register does not exist (ledger row L378) and AD-12's own text covers only `unavailable` and `available`-empty. So for an `excluded` axis — AD-9 L132 requires one on every query where NL is inactive, which is the default ("default-off") — the hits payload has two compliant encodings:

- **Unit A**: an excluded axis has no hits and no value, so by "an unavailable axis **or absent value** is an explicit JSON `null`" it emits `"hits": null`.
- **Unit B**: an excluded axis returned no hits, so by "an available-with-no-hits axis is an empty collection" — read as the general rule for "no hits" — it emits `"hits": []`.

The same fork applies to `truncated`'s uncovered-scope-unit count on the three axes that are not truncated: "every element is always present" obliges the property to exist, and AD-22 gives `0` a meaning ("`0` with a stated cutoff reason for a non-partitioned axis"), so Unit A emits `null` for a non-truncated axis while Unit B emits `0`.

**The concrete incompatibility.** G5's cross-surface golden vector is a **byte** comparison of the extracted packet, which AD-12 now mandates explicitly. A search with NL inactive and the graph axis truncated produces, for the identical query and identical data:

- Unit A: `…,"nl":{"state":"excluded","hits":null,"uncoveredScopeUnits":null},…`
- Unit B: `…,"nl":{"state":"excluded","hits":[],"uncoveredScopeUnits":0},…`

Byte-unequal, so the golden vector fails on every run; and the CLI's operator-observable output contract ("a text label for every state") renders `null` and `[]` differently in the human form, so an operator comparing a REST response with a CLI run sees two different answers for one query. Ledger row L392 carries the canonical serialization and ledger row L378 the register, so the obligation is ledgered — but the two rows do not name the missing shapes, which means the divergence has no convergence target: an implementer closing both rows can still ship either encoding.

**Proposed replacement — AD-12 Rule, replace the encoding sentence:**

> "**Equivalence is equivalence of the serialized document, and every state in every closed set the packet carries has exactly one declared shape:** every element is always present on every surface; a value that is absent, and the hits of an axis in AD-22's `unavailable` state, are an explicit JSON `null` and never an omitted property; the hits of an axis in `available` or `truncated` are a collection, **empty when it produced none**; the hits of an axis in `excluded` are `null`, because an unselected axis has no result rather than an empty one; AD-22's uncovered-scope-unit count is an integer on an axis in `truncated` and `null` on every other state, so `0` never means 'not truncated'; and no surface enables null-omitting serialization for evidence-bearing types. **A state whose shape is not declared here and in the name register may not be serialized**, which makes adding a fifth state a registered additive change in both places at once."

---

### ADV4-12 — high — AD-12 instructs one surface to omit withheld detail and forbids omitted properties in the same Rule, and the omission is itself the existence oracle the Conventions forbid

**Unit A — Authorization-filtering serializer unit** (implements the withheld-detail clause).
**Unit B — Canonical serializer unit** (implements the always-present clause).

**Compliance proof.** Both sentences are in AD-12 L150, and both were written this pass.

> "every element is always present on every surface, an unavailable axis or absent value is an explicit JSON `null` and **never an omitted property** … no surface enables null-omitting serialization for evidence-bearing types"

> "A handle is emitted only for detail the caller is authorized to retrieve; **detail withheld for authorization reasons is omitted with no handle and is indistinguishable from absence**"

Unit A omits the property for withheld detail, as instructed. Unit B emits `null`, as instructed. Neither can strike the other's sentence.

**The concrete incompatibility.**

**(a) Unit A leaks exactly what the clause intends to hide.** Because AD-12 encodes *authorized absence* as an explicit `null`, an *omitted* property is distinguishable from absence by construction: `"expansionHandle": null` means "nothing to expand" and a missing `expansionHandle` means "there is something and you may not have it". A caller enumerating cases in a tenant they partially share therefore obtains a reliable existence oracle over content they are not authorized to see, which the Errors row L225 forbids in terms — "authorization and existence failures on scoped resources return one indistinguishable response, same code, message, and timing class" — and which the same AD-12 sentence claims to achieve. Unit A is compliant and produces the leak; the leak exists *because* the other half of the fix made `null` meaningful.

**(b) Byte-equality fails across surfaces for every partially authorized result.** Unit A's REST payload and Unit B's CLI payload differ by a whole property, so G5's golden vector cannot be authored for any packet containing withheld detail — and a packet containing withheld detail is the normal case for a tenant-wide query under AD-7, where a principal reads every case but handles are "scoped to the issuing caller and request".

**Proposed replacement — AD-12 Rule, replace the withheld-detail sentence:**

> "A handle is emitted only for detail the caller is authorized to retrieve. **Detail withheld for authorization reasons is encoded exactly as absent detail is — the property is present with an explicit JSON `null` and no handle — so that withheld and absent are byte-identical and the packet carries no omitted property under any circumstance.** The count, score, scope and existence of unauthorized content are therefore unobservable, which is the indistinguishability the Errors convention requires; an implementation that omits the property instead reveals withholding and is non-conformant."

---

### ADV4-13 — high — AD-5's exhaustive list of enumerated lifecycle operations omits tenant deletion and the tenant content store, so the deletion workflow must pass the `Active` check on a tenant it has already moved to `Deleting`

**Unit A — Tenant deletion unit** (AD-6's deletion workflow; `TenantStatus.Deleting` ships today).
**Unit B — Authorization unit** (AD-5's three internal-call checks and its closed exemption list).

**Compliance proof.** AD-5 L108 closes the operation list with the word "exhaustively":

> "reaches **only the operations the operator artifact enumerates**, which are exhaustively: tenant provisioning, tenant verification, lifecycle repair, grant amendment, per-tenant credential rotation, tenant-resource migration setup and retire, access-telemetry partition provisioning and erasure, tenant deactivation, and AD-16 erasure. **Every other call such a workflow makes satisfies all three internal-call checks.**"

The third internal-call check is tenant-active:

> "satisfy three independent checks — an app ID present in the allowlist, an explicit grant for the requested tenant, and **that tenant being currently `Active` in AD-6's lifecycle state machine** — any one of which failing closed."

AD-6 L112/L114 requires operations that list does not contain:

> Binds: "Tenant provisioning, verification, repair, **deactivation, deletion**, the tenant lifecycle state machine … **the tenant content store**, the access-telemetry partition, and cleanup."
> Rule: states include "`Deleting`"; "the tenant content store of AD-4 and the access-telemetry store of AD-17 are tenant-isolated resources with a stated per-tenant principal or partition, **provisioned and erased with the tenant**".

`Deleting` is a distinct state from `Erased`, and "deletion" is a distinct Binds item from erasure — AD-21's own fail-closed list likewise names "tenant deletion" separately from "authoritative replay". So AD-6 has a deletion operation, and AD-5's exhaustive enumeration does not.

- Unit A implements `tenant delete` as AD-6's deletion workflow: commit `Deleting`, then release resources.
- Unit B applies AD-5 literally: deletion is not enumerated, so it is "every other call", so it must satisfy all three checks, so the tenant must be `Active`. The workflow's first act moved the tenant out of `Active`.

**The concrete incompatibility.** `tenant delete acme-legal` commits `Deleting` (AD-6 requires the transition "committed **before** the resources it authorizes are touched"). Its next activity boundary re-checks authority. Under Unit B the tenant is not `Active`, the call fails closed, and the workflow **deadlocks after its first commit with resources half-released and a tenant stuck in `Deleting`** — the precise deadlock D2 was ratified to eliminate, reintroduced for the one operation the enumeration forgot. `Deleting` is not terminal and AD-6's repair path "refuses any tenant in `Deleting`", so no compliant operation can move it. Under Unit A the operation proceeds; two builds of the same platform have a working and a permanently wedged delete.

The same hole exists for the AD-4 tenant content store on a live tenant: AD-6 must provision and erase it, AD-18 must quota it, and no enumerated operation names it — so creating it for a tenant provisioned before AD-4 landed (ledger row L383 confirms it does not exist yet) is an unenumerated call requiring an `Active` tenant, which works, while *erasing* it outside the AD-16 flow does not. And AD-6's mirroring of lifecycle transitions onto AD-21's partition is likewise not an enumerated operation (**ADV4-01**).

**Proposed replacement — AD-5 Rule, replace the enumeration:**

> "… which are exhaustively: tenant provisioning, tenant verification, lifecycle repair, grant amendment, per-tenant credential rotation, tenant-resource migration setup and retire, tenant-content-store and access-telemetry-partition provisioning and erasure, **the lifecycle-state projection write AD-6 mirrors onto AD-21's partition**, tenant deactivation, **tenant deletion**, and AD-16 erasure. **An operation that AD-6's Binds names and this list omits is a defect in this list, not an unexempt operation**: the list and AD-6's Binds are amended together, and a lifecycle operation that acts on its own subject's non-`Active` state and is absent from this list blocks the spine revision that introduced it."

---

### ADV4-14 — high — One reserved delimiter cannot be both excluded from the grammar and legal in the DNS-1123 names AD-23 requires, and the spine simultaneously blesses `:`

**Unit A — Identifier issuance and key-composition unit** (AD-23's tracked grammar artifact; ledger row L381).
**Unit B — Preflight dedup and Redis key unit** (Registry L241, "as shipped").

**Compliance proof.** AD-23 L216 requires one delimiter for every composed name, and requires the grammar to be DNS-1123-clean because per-tenant Kubernetes objects carry it:

> "every composed key, ACL pattern, index name, and state key is built from **that one delimiter**"
> Binds L214: "every composed key, **ACL pattern, index name, state key, secret path, and object name**"
> "The grammar is **lowercase** ASCII — chosen … so that identifiers are valid unchanged both as **DNS-1123 Kubernetes object names** and as unquoted PostgreSQL identifiers, where an uppercase grammar would satisfy 'single-case' and still produce **invalid Kubernetes objects for every per-tenant resource**."

DNS-1123 admits `[a-z0-9]`, `-` and `.` only. The grammar is lowercase alphanumeric with alphanumeric first and last characters, so a delimiter that is (i) excluded from the grammar and (ii) legal in a composed DNS-1123 object name must be `-` or `.` — nothing else exists. Registry L241 asserts the delimiter is `:`:

> "`tenantId` and `caseId` rely on **AD-23's grammar excluding the `:` delimiter**"

- **Unit A** declares the delimiter `-` (or `.`), because it must compose per-tenant Kubernetes object names and AD-23 makes that an explicit design driver. Every composed key in the platform is then rebuilt around `-`, and the shipped `dedup:` families are non-conformant and must migrate — including the Registry row's reserved prefix.
- **Unit B** keeps `:`, because the Registry row names it as the delimiter, the spine records the shipped family "as shipped", and AD-23's own worked example uses it.

Both quote the spine. There are now two delimiters, and AD-23's injectivity property holds within each family and is proved for neither globally.

**The concrete incompatibility.** A per-tenant resource name composed under Unit B — `memories:acme-legal:telemetry` — is rejected by the Kubernetes API server, and AD-6's provisioning workflow must compensate a tenant it could not create, which is exactly the class ADV3-10 closed for the *identifier* and this pass reopened for the *composition*. Under Unit A the shipped `dedup:` key families change shape, which invalidates every existing reservation and every durable workflow instance id derived from one (`EventIngestionService.cs:152` sets `instanceId = dedupKey`), making the migration a live-traffic identity change with no stated migration path. Neither unit can proceed without invalidating the other's artifacts, and the ledger rows that cover the grammar (L381) and the prefix (L397) do not name the delimiter conflict, so nothing forces the choice.

**Proposed replacement — AD-23 Rule, replace the composition sentence:**

> "**Composition is injective over all inputs and uses one reserved delimiter per composition scheme, each declared in the same tracked artifact as the grammar and each excluded from it.** Exactly two schemes exist: names that must satisfy DNS-1123 — every Kubernetes object, and every name composed into one — use `-`, and the grammar excludes `-` and `.` so that `-` is unambiguous there; keys and index names in Redis, RediSearch, FalkorDB and Dapr state use `:`, as shipped. **No identifier may be composed under both schemes without a stated one-to-one mapping between them**, and a component that composes a name for a system not covered by either scheme names its scheme in the artifact before use rather than choosing one."

---

### ADV4-15 — medium — `Erased` must be an AD-2-committed event on the stream the shred destroys and must be set from a register written after the shred, so AD-6's "can never disagree" holds only outside the crash window

**Unit A — Tenant lifecycle unit** (AD-6, the state machine's sole writer).
**Unit B — Erasure workflow unit** (AD-16/AD-21).

**Compliance proof.** AD-6 L114 imposes three things on the `Erased` state at once:

> "Every transition is an AD-2-committed domain event on the **`memories-tenants` stream** and is committed **before** the resources it authorizes are touched."
> "The `Erased` state is set **from** AD-21's register rather than beside it — the register remains the sole authority on erasure and the state machine reads it — **so the two can never disagree**."

`memories-tenants` is tenant-scoped by the spine's own topology — Structural Seed L314: "the **tenant-scoped** `memories-cases`, `memories-memory-units`, and `memories-tenants`, plus AD-21's `memories-platform`". AD-16 destroys the key that makes that stream readable.

- **Unit A** commits `Erased` to `memories-tenants` before the shred, satisfying "committed before the resources it authorizes are touched" and keeping the transition readable in the projection it mirrors. The register is then written afterwards by the erasure workflow, so the state was *not* set from the register.
- **Unit B** sets `Erased` only after the register tombstone exists, satisfying "set from AD-21's register". The transition event is then appended to a stream whose key is already destroyed and is unreadable, so the state machine has lost the transition and only the platform projection carries it.

Both are compliant; each violates the other's reading of one of the three constraints.

**The concrete incompatibility.** The erasure workflow crashes between the shred activity and the register write — the window AD-16 explicitly plans for ("remain resumable").

- Unit A: the machine reads `Erased`, terminal and irrevocable. AD-21's register holds no tombstone. AD-21 is "the sole authority for replay rejection, tenant-ID non-reuse, and restore admission", so an EventStore restore of `acme-legal` consults the register, finds nothing, and **is admitted** — rehydrating a tenant the state machine records as irrevocably erased. AD-6's "can never disagree" is false for the duration of the window, which is unbounded because the workflow is resumable rather than time-boxed.
- Unit B: the machine reads `Deleting`; provisioning and repair refuse it; the resumed erasure completes and writes the register; the two agree. But the `Erased` transition event is an unreadable append on a shredded stream, so AD-6's "every transition is an AD-2-committed domain event" is satisfied in form and not in evidence, and AD-16's "recorded and reproducible check" cannot cite it.

**Proposed replacement — AD-6 Rule, replace the two sentences:**

> "Every transition is an AD-2-committed domain event on the `memories-tenants` stream and is committed before the resources it authorizes are touched, **except the transition to `Erased`, which is committed on AD-21's platform partition because the tenant stream is no longer readable once the key is destroyed**. `Erased` is the projection of AD-21's register: the register's tombstone is the transition, the state machine reads it rather than recording a parallel one, and **the state between the shred and the tombstone is `Deleting`, not `Erased`** — so a crash in that window leaves a tenant that provisioning and repair refuse, that the erasure workflow may resume, and whose restore the register correctly still admits only because erasure has not completed. The register and the state machine cannot disagree because there is one record, and the only intermediate state is one both honour."

---

### ADV4-16 — medium — AD-18 forbids backpressure from changing an axis's AD-22 state and AD-22 makes a provider cutoff `truncated`, so under load one build degrades and the other rejects

**Unit A — Admission control and quota unit** (AD-18; NFR12–NFR14).
**Unit B — Retrieval adapter unit** (AD-22's reporting contract).

**Compliance proof.** AD-18 L186, written this pass:

> "**Admission control and backpressure never narrow a retrieval contract** — they reject or delay a query, and never shrink AD-9's deployment-wide candidate depth, drop an axis, **or change an axis's AD-22 state**."

AD-22 L210 makes the load-induced case a state:

> "`truncated` … did not complete the selected work within its configured **depth, result, or time limits** … **`truncated` applies to any axis in that position** — an uncompleted graph case partition, a **timed-out or provider-cut-off** syntactic or vector scan, `nl`, or any axis added later".

AD-9 L132 applies the time limit at the fan-out: "apply AD-11's time limit once to the whole tenant-wide fan-out."

A tenant at its concurrency quota issues a tenant-wide search. The fan-out contends for a throttled provider connection pool and the time limit fires with two of nine case partitions uncovered.

- **Unit B** reports `truncated` with an uncovered-scope-unit count of 2, exactly as AD-22 obliges; the query returns a degraded partial answer.
- **Unit A** must ensure backpressure never changed an axis's state; the only way to honour that is to detect the quota condition at admission and **reject or delay the query** with retry guidance, since once the query runs, the throttle changes the state.

Both comply, and the two behaviours are not variations of one contract: one returns HTTP 200 with a degraded packet and 7/9 case coverage, the other returns a rejection with retry guidance and no results.

**The concrete incompatibility.** Under load, Unit A's users see rejections and Unit B's users see partial answers; FR63 confidence and Evidence Packet state differ for identical queries, and G1's recall protocol — which "runs against a loaded tenant" — measures a rejection rate on one build and a recall degradation on the other. The rule is also unenforceable as stated: a time limit exhausted under contention is indistinguishable at the adapter from a time limit exhausted on a large corpus, so no adapter can tell whether its truncation was "caused by" backpressure.

**Proposed replacement — AD-18 Rule, replace the clause:**

> "**Admission control and backpressure never narrow a retrieval contract**: they reject or delay a query **before it is admitted**, and never shrink AD-9's deployment-wide candidate depth or drop an axis from the selected set. Once a query is admitted, resource contention is reported through AD-22's states exactly as any other limit exhaustion is — a contended axis that verified scope and did not complete is `truncated`, not `unavailable` and not omitted — because an adapter cannot distinguish contention from corpus size and must not guess. **What backpressure may never do is change an axis's state without the adapter reporting it**, so a throttle that silently reduces an axis's work is a defect."

---

### ADV4-17 — medium — One register instance across every deployment that may admit a restore makes a DR site either permanently unready or an empty-register admitter

**Unit A — Disaster-recovery topology unit.**
**Unit B — Register availability unit** (AD-21's platform scope).

**Compliance proof.** AD-21 L204:

> "**The platform scope is the set of deployments that can serve one tenant population:** every environment, replica, and **disaster-recovery site that may admit a restore, an import, or a provisioning for that population reads and writes one register instance**, and a deployment that cannot reach it is **unavailable** under this rule rather than empty."

AD-21 also provides the restored-register path: "A register restored from backup resumes at the highest sequence it can prove."

- **Unit A** stands up a DR site with its own restored copy of the platform partition, citing the restored-register sentence and the Deferred row's silence on topology.
- **Unit B** enforces one instance: the DR site reads the primary's register, and when it cannot, it is unavailable.

**The concrete incompatibility.** A regional outage — the event DR exists for. Under Unit B the DR site cannot reach the primary register, is unavailable under AD-21, and with the Health row (**ADV4-06**) fails readiness, so **failover is impossible during exactly the incident it was built for**. Under Unit A the DR site runs on a restored register whose sequence gap (**ADV4-09**) re-admits every artifact in the window, so failover succeeds and silently re-admits erased tenants' backups. "Multi-region, backend HA" is Deferred (L421), but AD-21's rule is binding now and already forecloses one of the two options a future topology decision would want.

**Proposed replacement — AD-21 Rule, add after the platform-scope sentence:**

> "A deployment may serve the population from a **replica** of the register rather than the primary instance only when the replica is synchronous with respect to register writes — a tombstone is not acknowledged until the replica holds it — in which case the replica is the register for that deployment. An **asynchronous or restored** copy is not the register: a deployment holding one is unavailable under this rule until it reconciles, and it admits no restore, import, or provisioning in the meantime. The topology that satisfies this for a disaster-recovery site is part of the Deferred multi-region decision, which may not be closed by treating a restored copy as the register."

---

### ADV4-18 — medium — The bundle index keeps a content digest forever in an append-only partition that AD-16's closed purge enumeration cannot reach and AD-21 forbids purging

**Unit A — Erasure unit** (AD-16's enumerated purge and verification list).
**Unit B — Platform-partition unit** (AD-21's never-shredded, append-only partition).

**Compliance proof.** AD-16 L174 scopes the purge by what a store can hold and names derived material explicitly:

> "Tenant deletion completes only after **every store that can hold tenant content or content-derived material** is purged … derived artifacts **including embeddings and index terms**".

The bundle index holds a digest of tenant content:

> "each bundle is registered at creation in AD-21's bundle index with its tenant, issuance time, and **content digest**"

AD-21 L204 forbids purging it: the partition is "never crypto-shredded, and **never carrying tenant content**", and writes "are append-only … so no application principal can modify or delete" a record.

Unit A classifies a digest of tenant content as content-derived material and purges it. Unit B refuses: the partition is append-only and by definition carries no tenant content, so the digest is outside the purge. Both are quoting their own decision, and the enumeration AD-16 calls closed does not list the bundle index either way — so Unit A's purge is also an addition to a closed list, which AD-16 forbids ("a store purged by obligation but missing from the enumeration is a gap").

**The concrete incompatibility.** After `acme-legal` is erased and its key destroyed, the platform retains, permanently and by design, a SHA-family digest for every export bundle the tenant ever produced. Anyone with read access to the register — AD-21 admits "operator principals" — can test any candidate document set against those digests and learn whether that exact bundle was `acme-legal`'s. That is a confirmation oracle over erased content, which is precisely the property crypto-shredding is claimed to remove, and it survives AD-16's completion evidence because the enumeration never names the store. AD-16's own `Prevents` clause ("a completion claim resting on a check that demonstrates nothing") is engaged: the sampled-ciphertext verification proves the payload no longer decrypts while the digest that identifies it is retained forever.

**Proposed replacement — AD-16 Rule, add to the bundle-index sentence:**

> "… registered at creation in AD-21's bundle index with its tenant, issuance time, **a content-free bundle identifier, and a digest bound to the bundle's per-bundle key so that the digest is unverifiable once the tenant key is destroyed** — a bare content digest is content-derived material and may not be retained on a partition erasure never reaches. Import compares the digest under the escrowed key, so an erased tenant's index entries remain refusable and no longer identify any content."

---

### ADV4-19 — medium — "Exactly one current document per `(tenantId, caseId, MemoryUnitId)`" and disjoint staging resources are jointly satisfiable two ways

**Unit A — Derived-store adapter unit** (Redis/FalkorDB writers).
**Unit B — Projection coordinator unit** (AD-3's fence).

**Compliance proof.** AD-3 L96:

> "derived stores hold **exactly one current document per `(tenantId, caseId, MemoryUnitId)`** carrying the tuple it was projected from … otherwise targets the **declared staging resources** AD-14 defines for that derived store in that phase, **so an active and a staging write can never alternate in one document**."

AD-14 L162 requires the staging resource to be "**disjoint**" from the active one.

- **Unit A** reads the invariant per resource: the active index holds one document per key and the staging index holds one document per key, which is what "disjoint" implies and what makes the backfill possible at all.
- **Unit B** reads it over the derived store as a whole — "derived stores hold exactly one current document per key" — so a backfill that creates a second document for the same key anywhere in the store breaches it, and the only compliant backfill is one that replaces the active document, which the same sentence forbids ("can never alternate").

**The concrete incompatibility.** A Phase 2 dimension migration on a 100,000-unit tenant. Unit A backfills 100,000 staging documents while the active index keeps its 100,000 — the ordinary create-backfill-verify-switch pattern AD-14 mandates. Unit B's architecture test, written from AD-3's literal sentence, fails the build the moment a second document for one key exists in the store family, so the migration cannot be implemented in a repository that carries the test. This matters concretely because ledger rows L357 and L395 both owe tests for exactly these invariants and neither states which reading the test asserts.

**Proposed replacement — AD-3 Rule, replace the fence's opening clause:**

> "each derived **resource** holds exactly one current document per `(tenantId, caseId, MemoryUnitId)` carrying the tuple it was projected from, and the active and staging resources for one store are disjoint, so one key may have one document in the active resource and one in each declared staging resource and never two in either; every projection write is conditional on the stored tuple of the resource it targets …"

---

### ADV4-20 — low — AD-4's "declared bounded character set" for `source` and `id` is declared by nobody, carries no ledger row, and the preamble's own escape clause therefore makes it ungoverned

**Compliance proof.** AD-4 L102:

> "`source` and `id` are externally supplied and are therefore validated at the ingress trust boundary **against a declared bounded character set**, length-bounded, and canonicalized there exactly once in a stated form".

AD-23 L216 expressly excludes them from the grammar ("every component that is **not itself issued under the grammar** — CloudEvent `source` and `id` …"), so the grammar is not that set. No other rule declares one, and no ledger row names it: L375 and L381 cover the issuance grammar and legacy identifiers, L386 covers the routing map's comparison. The preamble L76 then supplies the consequence: "a reader may treat an obligation absent from both the rule text and the ledger as **not yet governed**."

Two ingresses declare two sets — one the CloudEvents URI-reference production, one a restrictive `[A-Za-z0-9._~:/?#-]` subset — and a publisher's event is accepted at `/events/ingest` and rejected at the REST ingress, or vice versa. The artifact is thin (a rejection, not a corruption) because AD-23's hashing rule means a permissive set no longer produces key collisions, which is why this is low rather than high. It is recorded because it is the one case where class sweep 2's invariant — every named mechanism has a definition or a row — does not hold after the sweep.

**Proposed addition — AD-4 Rule, replace the clause:**

> "`source` and `id` are externally supplied and are therefore validated at the ingress trust boundary against **the CloudEvents production for each field, narrowed by a stated maximum length declared in AD-23's tracked artifact alongside the grammar**, and canonicalized there exactly once in a stated form (scheme and host lowercased, no other change); thereafter they are compared ordinally with no further normalization. Every ingress validates against that one declaration, and its absence carries the same ledger row as the grammar."

---

## Part D — Errata (intra-document defects, not divergence pairs)

| ID | Defect | Location | Correction |
| --- | --- | --- | --- |
| **E2** | The spine asserts three times that the shipped reservation key hashes CloudEvent `source`. It hashes `id`; `source` is not in the key. `EventIngestionService.cs:144` passes `envelope.Id` into a parameter named `sourceUri`; `IPreflightDedupStore.cs:14` documents the truth. | AD-4 L102, AD-23 L216, Registry L241 | Correct all three, and add the ledger row **ADV4-04** names. This is the same class of defect as the pass-3 `memories:preflight:` erratum — a key family described from the spine rather than read from the code — and it was introduced by the pass that corrected that one. |
| **E3** | AD-19 states "The Stack table records the qualified digest alongside the tag for every image the platform runs." The Stack table L249–L263 records no digest for any image; the PostgreSQL row says "digest-pinned" without a value. | AD-19 L192 vs Stack L249–L263 | Either record the digests or state that the digest set lives in `Hexalith.Memories.Aspire` and that the Stack table records tags plus a pointer. As written the rule asserts a false fact about an artifact in the same file. |
| **E4** | AD-20 requires the owner to record "the triggering clause in the row". No row among L356–L399 records one. | AD-20 L198 vs ledger | Add the clause to each `Yes` row, or state that the classification column carries it implicitly. |
| **E5** | The ledger preamble forbids a convergence cell that offers "a choice between architectural outcomes"; L362 and L397 each offer one. | L352 vs L362, L397 | Resolve both choices in the Rule, or state that AD-8's "any addition requires a new architecture decision" makes the pair one outcome with two routes. |
| **E6** | AD-5's exemption list is introduced as "**Exactly three exemptions**" and the enumerated operation list is introduced as "exhaustively" with nine items; the memlog records ten. Not a divergence, but the count and the list should agree so that a reader can verify closure. | AD-5 L108, memlog | Count the items or drop the numeral. |

---

## Part E — Specific probes the mandate named

| Probe | Finding |
| --- | --- |
| **Bootstrap ordering — what creates AD-21's partition, and what happens where it does not exist** | Creation is now stated and ledgered (L377), and the fresh-cluster deadlock is resolved: provisioning is in the fail-closed list and unavailability is a positive fact. Two residues: the step has no named owner or runner, so a server-startup implementation manufactures an available-and-empty register (**ADV4-07**), and readiness now converts a register outage into a total product outage (**ADV4-06**). |
| **AD-21's append-only register vs AD-16's purge** | The register itself is consistent with the purge — the tombstone is content-free. The **bundle index** is not: a bare content digest is content-derived material retained forever on a partition erasure never reaches, and AD-16's closed enumeration names neither the store nor an exception (**ADV4-18**). |
| **Can AD-6's state machine and AD-21's register still disagree about "erased"?** | Not in the steady state — `Erased` is set from the register, which closes ADV3-04. They can still disagree **across the crash window between the shred and the tombstone**, and the two compliant ways to place the `Erased` transition event differ about whether that window exists (**ADV4-15**). |
| **Do AD-22's four states and AD-12's encodings now match?** | **No.** AD-22 defers the four packet shapes to AD-12's name register; AD-12 declares shapes for `unavailable` and `available`-empty only, so `truncated` and `excluded` have two compliant encodings each and byte-equality fails (**ADV4-11**). AD-12 additionally instructs one surface to omit a property it forbids omitting (**ADV4-12**). |
| **Can AD-23's grammar and AD-4's identity comparison be jointly satisfied?** | **Yes** — AD-23's "composition step, never a normalization" sentence resolves ADV3-09 cleanly. But AD-23's worked example misstates the shipped key and AD-4 relies on that misstatement, so the joint satisfaction is proved over the wrong artifact (**ADV4-04**, **E2**), and one delimiter cannot serve both the DNS-1123 requirement AD-23 states and the `:` the Registry asserts (**ADV4-14**). |
| **Are AD-14's Phase 1 rebuild epoch and AD-3's fence consistent in every phase and for every derived store including the graph?** | **No.** They are consistent for ordinary ingestion writes during a rebuild — the case D3 was derived from. They are inconsistent for the **rebuild's own writes**, which are non-active-epoch writes in a phase whose only declared staging resource is "the rebuild epoch" and whose redirect the same sentence declares Phase-2-and-3-only; the graph, syntactic index and tombstone stores have no per-store Phase 1 declaration at all (**ADV4-03**). The retire step that would clean up after activation is attributed by each decision to the other (**ADV4-08**). |

---

## Gate disposition

**Counts.** Retest: 38 Closed · 2 Partially closed · 0 Still open · 0 Regressed, of 40 carried items. New: **20 pairs — 5 critical, 9 high, 5 medium, 1 low**. Errata: 5 new (E2–E6); E1 closed.

**Did this pass repeat the narrowing pattern? Yes — relocated.** The complement test pass 3 asked for was run on the reported findings, and the evidence is the retest: every previously-open general case is closed, including the eight ADV2 pairs that had survived two amendments verbatim. It was **not** run on the new text written to close them. 14 of the 20 new pairs are in sentences dated 2026-09-12 by this pass, and each is the adjacent case of a fix rather than the fix's own case: AD-21 authorizes the one writer the findings exhibited and not the three its own contents list implies; AD-22 generalises `truncated` for an axis with hits and not for one with none; AD-14 declares Phase 1 staging for the epoch and not per store; AD-21 stamps a sequence that AD-16's bundle sentence does not carry; AD-12 encodes the two states it already had and not the two AD-22 added. The signature changed from *narrowing a fix* to *not propagating a fix into the rule beside it* — which is a better failure, and it is the same failure.

**Why PASS-WITH-FINDINGS rather than REVISE.** No adopted decision is reopened by any of the twenty. Eighteen are closable by replacing or adding Rule text in place, and the proposed text is supplied for each. Two are not editing-pass work:

- **ADV4-01** needs a ratification, not an edit: D1 authorized one writer for the platform partition and the partition demonstrably has four. The replacement text above assigns one writer per content kind, which is a decision about privilege distribution and should be recorded as a D1 amendment.
- **ADV4-04** needs a code-or-text decision: either the shipped pub/sub key becomes the four-component identity AD-4 states, or the spine records it as a known violation with a ledger row. Today the spine records it as an example of compliance, which is the one option that is false.

**Sequencing.**

1. **E2 first.** Three sentences and a registry row rest on a misread of shipped code. Correct them before any epic cites AD-23.
2. **The five criticals before sprint selection.** ADV4-01 blocks the AD-6, AD-16, AD-17 and FR71 epics jointly and cannot be closed by any one of them. ADV4-03 blocks the ratified FR43 command. ADV4-02 and ADV4-11 jointly block the fusion, adapter and G5 evidence epics. ADV4-05 is the only one with a cross-tenant consequence and should be closed before the pub/sub ingress epic is scoped; ledger row L386's convergence currently points at the wrong fix.
3. **ADV4-06 and ADV4-07 together**, since both concern what the platform does when the register is not there and they push in opposite directions.
4. **ADV4-08, -09, -10, -12, -13, -14** with their owning epics; each is a single-sentence edit.
5. **ADV4-15 … ADV4-20** and the errata batch into the next spine revision.
6. **ADV2-15 / E3** and **ADV3-15 / E5** are the two partial closures; both are corrections to a rule that asserts something about an artifact in the same document.

**Recommendation for a fifth pass.** The complement test worked. Extend it one step: **apply it to the text the pass writes, not only to the text the pass fixes.** Concretely, for every new or amended Rule sentence, ask (a) who else does the spine oblige to perform this act, (b) which other decision enumerates a set this sentence adds a member to, and (c) does the adjacent rule that must carry this fact now carry it. All 14 of the new-text pairs above are caught by one of those three questions, and none of them required reading the codebase to find.
