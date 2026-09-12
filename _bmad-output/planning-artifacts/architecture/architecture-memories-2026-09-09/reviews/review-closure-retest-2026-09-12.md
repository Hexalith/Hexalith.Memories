# Closure Re-test — Amendment Closure + Good-Spine Rubric

**Lens:** amendment closure + rubric walk (read-only re-test of the 2026-09-12 six-lens gate)
**Target:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` — 353 lines, 20 ADs, `updated: 2026-09-12`
**Baseline for diff:** pre-update snapshot, 325 lines, 19 ADs, `updated: 2026-09-09`
**Authority for intent:** `.memlog.md` entries after "Update run started 2026-09-12" (30 decision/constraint entries)
**Findings under re-test:** the 10 critical and 17 high in `architecture-validation-2026-09-12/validation-report.md`
**Mode:** read-only. No spine, PRD, source, or configuration file was changed. No build, test, or restore was run. No submodule was initialized or updated. The only file written is this review.

---

## Verdict

**PASS-WITH-FINDINGS** — all 10 criticals and 14 of 17 highs are fully closed, 3 highs/criticals are PARTIAL on a stated sub-part, none is NOT CLOSED, and no adopted decision was reversed or renumbered; but the amendments introduced one false statement of present fact, left the ratification they record unreflected in the PRD that declares itself authoritative on it, and added a normative classifier that nothing defines.

| Measure | Result |
| --- | --- |
| CLOSED | **24** of 27 |
| PARTIAL | **3** — C2, H4, H10 |
| NOT CLOSED | **0** |
| REGRESSED | **0** (two amendments created new inconsistencies without failing their own finding — see RET-01, RET-05) |
| Adopted decisions reversed | **0** |
| AD IDs renumbered | **0** — AD-1…AD-19 identical, AD-14 retitled in place, AD-20 appended |
| New findings | **15** (RET-01 … RET-15): 2 high, 7 medium, 6 low |

The amendment pass is substantively strong. Every amendment that closes a *divergence* finding — AD-3's tuple relation and write fencing, AD-5's issuance grammar and allowlist/grant split, AD-9's candidate depth, pagination, `truncated` state and `nl` resolution, AD-10's `safely` predicate, AD-12's serialized-document equivalence, AD-16's erased-tenant register — is stated as a predicate a reviewer can apply to a diff or a golden vector, and each admits exactly one implementation where two were previously compliant. That is the hard part and it was done well.

What holds the verdict below PASS is that the pass repeated, in three new places, the defect class that produced C9: **a sentence asserting as present fact a mechanism or record that does not exist.** A `final` document is quotable only if its statements of fact are true, and the previous gate failed this document for exactly that.

---

## 1. Closure table

| Finding | Status | Evidence (amended spine) | Note |
| --- | --- | --- | --- |
| **C1** erasure target set under-enumerated | **CLOSED** | `:158` "Tenant deletion completes only after **every store that can hold tenant content or content-derived material** is purged — product projections, durable workflow history and activity payloads, actor state, caches, and derived artifacts including embeddings and index terms"; `:156` Binds adds "durable workflow history and actor state, caches and derived artifacts, application export bundles"; `:86` (AD-4) "tenant content and content-derived material — extracted text, embeddings, snippets — are read from an erasure-scoped store at execution time and never persisted into workflow history or actor state" | All four holes closed, plus the structural AD-4 fix the finding recommended as "cheaper". Cross-tenant limitation recorded at `:158`. |
| **C2** erasure record ownership/durability | **PARTIAL** | `:158` "A single **durable erased-tenant register**, held outside any tenant-scoped keyspace and never encrypted under a destroyed tenant key, is the sole authority for replay rejection, tenant-ID non-reuse, and restore admission; it carries the content-free non-reuse tombstone with no TTL and no purge path, is retained indefinitely, and is written and owned by the erasure workflow — any telemetry-side copy is derived and can never satisfy, delay, or revoke completion"; `:158` "Verification means a recorded, reproducible check per enumerated target"; `:164` AD-17 now says "its **derived** copy of the tenant-erasure mapping" | Ownership, TTL-freedom, self-shredding, derived-copy subordination and the definition of verification are all closed. **Remaining sub-part:** the fix required the register be named "with a stated durability and restore posture". No store is named and no durability or restore posture is stated. `:210` still routes "mappings" to Dapr state, and the Deferred table `:344` says the production Dapr state store's durability is itself unqualified — so the sole authority for an irreversible data-protection outcome may sit in the one store the spine declines to qualify. |
| **C3** AD-3 tuple ordering + write fencing | **CLOSED** | `:80` "Order that tuple explicitly: `(embeddingConfigurationEpoch, schemaGeneration)` is the identity discriminator and `sourceVersion` the only monotonic dimension … an acknowledgement for a different generation or epoch is never stale"; "The reported public ingestion state is always that of the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`, while a staging tuple's completion is reported only as migration-backfill progress"; "every projection write is conditional on that stored tuple and is a no-op when the incoming tuple is older, and deletion writes a fenced tombstone under the same rule"; "AD-14's retire step deletes the retired generation's and epoch's checkpoint records"; `:146` "The active schema generation and embedding-configuration epoch are tenant-lifecycle domain facts committed through AD-2 before any projection may cite them; the tenant configuration actor caches and serializes access to them and is never their source of truth" | All five sub-parts closed, including the epoch-owner ambiguity. |
| **C4** allowlist/grant two writers | **CLOSED** | `:92` "The app-ID-to-`system:*` allowlist is a deployment-time artifact with exactly one writer, the operator, held in AD-15's operator secret scope … it grants principal identity and never tenant scope, an absent app ID fails closed with no default principal, and grants are enumerated with any wildcard requiring its own architecture decision. The per-tenant grant … is a tenant resource whose sole writer is AD-6's lifecycle workflow, created only by verified provisioning and revoked as a completion condition of AD-16 erasure"; "three independent checks … any one of which failing closed" | All five sub-parts closed in AD-5. But AD-16 never states the revocation as one of its completion conditions — see **RET-04**. |
| **C5** graph seeds/limits/truncation | **CLOSED** | `:116` "the first five entries of the canonicalized syntactic list and the first five of the canonicalized semantic list — five entries, not five ranks"; "Apply AD-11's result limit once to the merged graph list after the per-case merge and before rank allocation, never per case partition, and apply AD-11's time limit once to the whole tenant-wide fan-out"; "or `truncated` — available, carrying its hits, contributing denominator weight, and named in the Evidence Packet as degraded with the count of uncompleted case partitions" | All three closed. `truncated` also propagated to AD-9 *Prevents* `:115` and AD-10 `:122`, but not to AD-12's element list — see **RET-13**. |
| **C6** identifier issuance grammar | **CLOSED** | `:92` "**Issuance is what makes ordinal comparison sufficient:** restrict tenant, case, and `MemoryUnitId` issuance to one documented bounded ASCII grammar that excludes every character significant as a metacharacter, delimiter, or wildcard in Redis ACL key patterns, Redis/RediSearch/FalkorDB key and index names, Dapr key and component names, URL path segments, or Kubernetes object names, and compose every derived key, ACL pattern, index name, and state key unambiguously with a delimiter reserved out of that grammar"; "Tenant creation, deletion, and lifecycle repair are authorized by an operator principal, never by a tenant claim"; convention row `:189` restates | Closed against the fix as worded, including SEC-19's tenant-creation-authority enabler. The finding's third bullet (case-variant collapse to DNS-1123) survives the grammar as written — new finding **RET-08**. |
| **C7** AD-14 phasing (ratification) | **CLOSED** | `:11-14` frontmatter `ratifications: - decision: 'AD-14 phase qualification (PRD Open Question 9)' by: 'Jérôme Piquot' on: '2026-09-12'`; `:142` heading "Evolve schemas and providers with **phase-qualified** tenant migrations [ADOPTED · ratified 2026-09-12]"; `:146` "Migration obligations are phase-qualified and no phase's evidence satisfies another's. In **Phase 1 / MVP**, FR43 permits only a tenant-scoped, explicitly acknowledged degraded rebuild … never claims zero downtime. In **Phase 2**, … In **Phase 3**, … No MVP implementation or completed historical migration is retroactively claimed to meet the Phase 2/3 contract." | Resolution (a) taken, as the product side had already indicated. The *Prevents* line `:145` was updated in step ("an unphased rule silently rebaselining MVP onto a contract the product has not funded"). **But** `prd.md:1225` still carries Open Question 9 as an open `[PHASE-BLOCKER / G6]` and `prd.md:62` declares Open Questions 9 and 10 authoritative — see **RET-02**. |
| **C8** G6 + ledger columns | **CLOSED** | `:178-182` AD-20 added; `:316` "Under AD-20, a row classified **active-foundation critical** must carry an owner and evidence path or an approved dated exception"; `:318` header `\| Gap \| Violated rule \| Required convergence \| Disposition \| Active-foundation critical \|`; `:338` "G1-G6 or L1-L3"; `:294` Operational Boundaries "Gate evidence" row | Both columns added, the scoping restriction is carried by AD-20's Rule and the ledger preamble so the spine does not over-scope G6, and G1-G5 → G1-G6 landed. Two residues: the classification column sits **after** Disposition, contrary to the fix wording and the memlog's own account (**RET-09**), and "active-foundation" is nowhere defined (**RET-03**). |
| **C9** AD-8 enforcement tense | **CLOSED** | `:110` "Provider SDK references **must be** confined … enforced by an architecture test **still to be added**: neither namespace exists today and no guard enforces the boundary, so this rule is binding but currently unenforced and its convergence is ledgered"; `:188` "provider adapters **will use** the AD-8 namespaces (`Adapters.Redis` / `Adapters.FalkorDb`), which do not exist today"; `:326` ledger row states "105 files under `Hexalith.Memories.Server`" and adds namespace creation to convergence | All three places corrected and the obligation's size made visible, exactly as the fix asked. The generalisation of this fix into the preamble `:60` over-reaches — see **RET-06**. |
| **C10** .NET security pin | **CLOSED** | `:232` "Two security blockers stand before Production qualification, not one. **.NET** `10.0.400` carries runtime `10.0.11`, which predates the 2026-09-08 security release `10.0.12` / SDK `10.0.401` fixing six CVEs; `rollForward: latestFeature` means the resolved SDK floats within the 10.0.4xx band, but container base images and the pinned `Microsoft.AspNetCore.*` packages do not roll forward"; ledger row `:332` "Bump to SDK `10.0.401` / runtime `10.0.12`, or record a dated exception naming the CVEs \| Dated exception or bump owed; blocks Production \| Yes" | Closed including the `rollForward` disclosure the fix asked for explicitly. |
| **H1** `/events/ingest` authority | **CLOSED** | `:92` "Dapr pub/sub ingress derives tenant authority from the delivery channel and never from the envelope: the receiving component or topic is bound to one tenant, or the publishing app ID's grant must contain the envelope's tenant, and an unauthorized envelope tenant is rejected rather than ingested"; `:90` Binds now reads "Dapr ingress and pub/sub delivery" | Both admissible bindings offered, rejection stated. |
| **H2** telemetry authz + PostgreSQL | **CLOSED** | `:164` "Access-telemetry reads and writes derive tenant authority under AD-5 like any other surface, authorized independently of the calling workload's channel identity, and the telemetry store is a tenant-isolated resource under AD-6"; `:98` AD-6 Rule "the access-telemetry store is a tenant-isolated resource with a stated per-tenant principal or partition"; `:310` capability row now "AD-5, AD-6, AD-15, AD-17, AD-18"; `:224` Stack "PostgreSQL (access-telemetry store) \| `18.4-trixie`, digest-pinned"; `:253` seed gloss; `:281` "access telemetry with its PostgreSQL store" | Both halves closed in all four places the finding named. |
| **H3** case authority | **CLOSED** | `:104` "**Case is a data-partition and attribution boundary, not an authorization boundary:** a principal with tenant authority may read every case in that tenant, and case-scoped queries constrain results rather than access. Per-case authorization is deferred, and until it is adopted no product documentation may present cases as an access-control mechanism"; `:103` *Prevents* extended; Deferred row `:345` added | The position is stated once, either-way ambiguity removed, and the documentation constraint is a genuinely useful addition beyond the fix. |
| **H4** K8s-secret gap bounded | **PARTIAL** | `:152` "Direct Kubernetes-secret injection of Redis/FalkorDB credentials is a temporary alignment gap with a named owner and a dated expiry recorded in the ledger, not a second approved application secret path; **Production qualification is blocked** until per-tenant backend principals replace it or a dated, time-bounded security exception is approved, on the same footing as the OpenBao version exception"; ledger row `:324` rewritten with the NFR8 consequence and "blocks Production per AD-15"; upstream half landed — `prd.md:927` now reads "Direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path (NFR9, architecture AD-15)" | The production-blocking parity with OpenBao is closed and the NFR8 consequence is now stated in the row. **Remaining sub-part:** the fix required "named owner, dated expiry". Neither exists — the ledger's Disposition cell says "Named owner and dated expiry **owed**", and no date appears anywhere in the spine. AD-15 asserts them as recorded. See **RET-01**. |
| **H5** sanitized/opaque defined | **CLOSED** | `:164` "**Sanitized means content-free:** records carry tenant, case, principal, operation, outcome, and timing as opaque or enumerated values and never carry query text, snippets, extracted content, memory-unit content fields, or any value derived from them, and a record that cannot be written content-free is not written" | Field enumeration and the not-written fallback both present, closing AD-16's carve-out. |
| **H6** "safely" defined | **CLOSED** | `:122` "An axis can respond **safely** if and only if its adapter applied the request's authoritative tenant and case scope, returned within AD-11's or the axis's configured limits without truncation, and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`. Staleness, low recall, and empty results are safe … unverified scope, an exhausted limit, and a generation or epoch mismatch are unsafe and null the axis. Safety is decided once per query by the server's axis-selection step against this definition, never independently by each adapter, and the resulting availability vector is the single input to both AD-9's denominator and the Evidence Packet's axis health" | Iff-form, server-owned, single input to both consumers. The ~1.54× score divergence is closed. |
| **H7** candidate depth + pagination | **CLOSED** | `:116` "Each axis contributes a fixed, server-owned, configuration-validated candidate depth identical across every surface, and fusion is defined over each axis's complete server-limited candidate list"; "The caller's result limit and offset apply only to the fused output after rank allocation, normalization, and final ordering and are never pushed into an axis provider, so a unit's rank, per-axis contributions, and composite score are identical whatever page returns it and consecutive pages concatenate to exactly the head of the single fused ordering"; `:114` Binds adds "candidate depth, pagination" | Golden-vector authorship is now possible; the page-independence invariant is directly testable. |
| **H8** nl axis vs rerank | **CLOSED** | `:116` "`nl` is a fourth ranked axis and not a rerank: … canonicalized, competition-ranked, and contributes at weight `0.20` inside the same weighted sum, default-off, entering the denominator only when it returned results, appearing in Evidence Packet axis health and in explain output under the wire name `nl`, and selectable through the axis-control parameter; where NL is inactive it is reported as an excluded axis rather than omitted"; `:114` Binds changed from "natural-language reranking" to "natural-language ranking" | The Binds/Rule contradiction is gone; "rerank" now appears in the spine exactly once, in the negation. |
| **H9** Evidence Packet encoding | **CLOSED** | `:134` "**Equivalence is equivalence of the serialized document:** every element is always present on every surface, an unavailable axis or absent value is an explicit JSON `null` and never an omitted property, an available-with-no-hits axis is an empty collection, no surface enables null-omitting serialization for evidence-bearing types, the packet state is a single value drawn from the versioned vocabulary (`complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized`, `pendingExpansion`) with an accompanying set-valued reason list, and one golden-vector document per case is byte-comparable across REST, Dapr invocation, CLI JSON, and MCP" | Encoding fixed, PRD's eight-value vocabulary named, single-value-plus-reason-set settled. |
| **H10** Contracts.V1 owner | **PARTIAL** | `:134` "`Contracts.V1` has one named owning component with change-approval authority over its wire surface, and an additive change is admissible only once its new wire name, JSON shape, and nullability are reserved in the contract's versioned name register in the same change; a reserved name may not be reused with a different shape, and a collision is resolved before merge rather than by a later rename" | The name register and the collision-before-merge rule fully close the unresolvable-collision mechanism. **Remaining sub-parts:** (a) the fix said "**name** an owning component"; the spine requires that one be named but never names it, unlike AD-6/AD-17/AD-19 which name their owners — the finding's own argument was that the spine's central device is naming owners; (b) `.memlog.md` records the intended clause "architecture tests fail the build when a public evidence-bearing type carries an unregistered wire name", which is absent from the spine and has no ledger row, so the register has no enforcement path at all. |
| **H11** registry amendment rules | **CLOSED** | `:210` "A later `AD-n` may add a row only when it also names the **single** coordination store for the protected resource — no resource may be guarded by primitives in two stores, and a new row guarding an already-guarded resource must migrate the existing guard in the same decision — and states its primitive, failure posture, tenant/key scope, and test evidence. Mutual-exclusion primitives (leases, fences, locks) **fail closed**; only admission optimizations with a durable fallback may fail open. Every row declares its reserved Redis key prefix, which no other row may claim." | All three sub-parts stated. The table's only row does not comply with the third — **RET-05**. |
| **H12** binding header | **CLOSED** | `:16-19` `binds: - 'FR1-FR75' - 'NFR1-NFR37' - 'G1-G6' - 'L1-L3'`; FR75 added to Binds of AD-2 `:72`, AD-3 `:78`, AD-4 `:84`; AD-4 `:86` "a legitimate reprojection under a new schema generation or embedding-configuration epoch is not a duplicate and is never suppressed"; capability-map rows added at `:303`, `:306`, `:312` | Including the finding's qualification — the implicit new-epoch non-suppression is now explicit, and capability-map rows were added in place of the trace map the spine does not have. |
| **H13** NFR37 home | **CLOSED** | `:132` AD-12 Binds now leads with "NFR37 … exit codes"; `:134` "Each active surface additionally publishes an operator-observable output contract: a stable presentation order, a text label for every state, axis, score meaning, omission, progress stage and recovery, a machine-readable envelope and exit-code map, and identical semantics across every emitted form, with colour, glyph, motion, and screen position supplementary only"; `:193` new "Active CLI output contract" convention row; `:306` capability-map row | Every one of the eleven unowned obligations the finding enumerated is covered by `:193`, down to "duplicate delivery never emits a second created-unit line" and "`CliExitCodes` and the JSON envelope are versioned contract surface under AD-12". The best-executed amendment in the pass. |
| **H14** Web gap row | **CLOSED** | `:334` "The Web conformance evidence is hosted from `tests/Hexalith.Memories.Web.SpecimenHost` while `src/Hexalith.Memories.Web` remains a non-packable RCL, and that project's 'no runnable host yet' comment is stale. \| AD-12 \| Resolved for host existence (2026-07-06, CI job `web-e2e-specimen`). Re-run the browser/AT specimen evidence before recording `confirmed resolved`; correct the stale comment"; seed `:248` corrected to "Non-packable RCL; runnable specimen hosted from tests/, not product UI" | Row replaced with what actually remains, and correctly kept in the ledger under AD-20 rather than deleted. |
| **H15** fusion gap row | **CLOSED** | `:327` "Fusion allocates competition ranks correctly but skips AD-9's blank-ID and non-finite drops and lets duplicate IDs consume rank slots; `FusionEngine.ScoresTie` additionally treats `NaN == NaN` as a tie, a behavior currently locked by passing tests. \| AD-9 \| … retire or invert the `Fuse_Bm25NaN_*` / `Fuse_Bm25Infinity_*` tests as part of the change" | Both halves — the precision correction and the green-tests-must-be-inverted work item. |
| **H16** OpenBao chart target | **CLOSED** | `:232` "**OpenBao** must be upgraded and requalified to at least the security-fixed `2.6.2` line, with the current chart line (`0.29.x`, presently `0.29.4`) assessed rather than the superseded `0.28.6`"; ledger `:333` "Upgrade to at least `2.6.2`, assess the current `0.29.x` chart line, sweep GO-2026-5970" | Chart line corrected and the open upstream tracker swept in, as the fix asked. |
| **H17** EventStore gitlink | **CLOSED** | `:221` "Hexalith.EventStore source lane gitlink \| `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20`"; `:232` "the source-lane gitlink above is re-derived from `git ls-tree HEAD references/Hexalith.EventStore` at each revision"; AD-19 `:176` "The source-lane gitlink is re-derived from the superproject at every spine revision and must correspond to the package-lane version; that correspondence is part of lane evidence"; ledger `:330` convergence adds "gitlink/package-version correspondence" | **Independently verified in this pass:** `git ls-tree HEAD references/Hexalith.EventStore` → `6b0247acc0b3ef60eb00c0f9ac9cbd367f85da20`, exact match. `references/Hexalith.Builds` → `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, matching the "committed gitlink `a32cb422`" the caveat now cites. Both dirty worktree SHAs (`a568af4e`, `fa647278`) differ, and the spine now says explicitly which side it read. All three sub-parts closed. |

---

## 2. Rubric walk over the amended text

### 2.1 Do the new and changed Rules prevent their stated divergence, or are they aspirational?

**Mostly yes, and by construction.** Almost every amendment was generated from a two-compliant-implementations proof, and the replacement text closes the fork by naming the single admissible reading rather than by exhortation. Concretely testable now, where they were not before:

- AD-3 `:80` — an acknowledgement is stale **iff** `sourceVersion < recorded value for the same generation and epoch`. One predicate, mechanically checkable, and the permanent-stall scenario is arithmetically impossible under it.
- AD-9 `:116` — page-independence stated as an equality ("consecutive pages concatenate to exactly the head of the single fused ordering") that a golden vector can assert directly.
- AD-10 `:122` — `safely` given as an iff over three conjuncts, with the decision point named ("the server's axis-selection step … never independently by each adapter").
- AD-12 `:134` — equivalence re-keyed from meaning to bytes ("one golden-vector document per case is byte-comparable across REST, Dapr invocation, CLI JSON, and MCP"). That is a test, not a principle.
- AD-5 `:92` — three named checks, each with a stated failure direction ("any one of which failing closed"), and one named writer per artifact.
- AD-16 `:158` — restore admission re-keyed from a payload property to a register lookup, "regardless of whether the payload is readable". The fix to a predicate that could never fire.

**Aspirational or unfalsifiable as written** — four:

1. **AD-20** `:182` states that every critical-classified gap "carries exactly one of a current evidence path or a dated phase exception … each with a named owner and a tracker entry", while 11 of the 12 rows it governs carry neither. See **RET-14**.
2. **AD-15** `:152` asserts a named owner and dated expiry that do not exist. See **RET-01**.
3. **Preamble** `:60` + **Testing convention** `:199` claim that every unenforced rule says so and that the ledger carries the missing guards. Neither is true of AD-1. See **RET-06**.
4. **AD-12** `:134` requires "one named owning component" but names none, and its name register has no enforcement path. See H10 PARTIAL.

### 2.2 Internal consistency after the edits — the highest-yield check

I walked every amended section against every other. Cross-references, convention rows, capability-map rows, Operational-Boundary rows and the ledger were each checked against the AD they restate.

**Verified consistent** (no finding):

- All 20 ADs carry Binds / Prevents / Rule (20/20/20). No duplicate or missing ID.
- Every `AD-n` cross-reference resolves to an AD that still governs the cited concern: AD-2→AD-3/AD-16; AD-3→AD-14 retire; AD-5→AD-6/AD-15/AD-16; AD-8→the registry; AD-9→AD-11 depth/result/time; AD-10→AD-9 denominator + AD-11 limits; AD-14→AD-2/AD-3; AD-16→AD-2/AD-17; AD-17→AD-5/AD-6.
- The two RUB-06 cross-reference contradictions are gone: `:288` now reads "app tokens protect **service-to-service** channels" (was "local channels", contradicting AD-5) and `:189` now reads "Ordinal ordering is imposed at **AD-9's specified merge and tie-break points** and nowhere else" (was "only the final fusion tie-break", contradicting AD-9's two intermediate orderings).
- The AD-8 tense correction is consistent across all three surfaces (`:110`, `:188`, `:240`) and the ledger row.
- `truncated` propagates correctly through AD-9 *Prevents* → AD-9 Rule → AD-10 Rule.
- The Stack caveat, the Deferred "Redis 8 and FalkorDB upgrades" row and ledger rows 13/14 agree on every version and date.
- The Structural Seed, the `kubernetes/` gloss and the composition sentence at `:281` all now carry PostgreSQL, `tools/`, `samples/`, `docs/` and the corrected Web and `Client*` entries.
- Deferred `:345` (per-case authorization) and `:348` (hosted Web, authority retained by AD-5) both match their governing ADs.

**Inconsistencies found** — RET-04, RET-05, RET-06, RET-09, RET-11, RET-12, RET-13, RET-15 below. The two that matter most:

- **A rule stated in one place and contradicted by the artifact it governs, twice.** AD-5 `:92` says the per-tenant grant is "revoked as a completion condition of AD-16 erasure"; AD-16 `:158` enumerates its completion conditions exhaustively and grants are not among them, nor in its Binds `:156`, nor in its completion evidence "target list". This is C4's "no Rule conjoins them" seam, recreated in the opposite direction (RET-04). Likewise `:210` "Every row declares its reserved Redis key prefix" against a table whose only row declares none (RET-05).
- **Binds terms with no Rule clause.** AD-6 `:96` binds "per-tenant grants" and AD-15 `:150` binds "the AD-5 allowlist artifact"; neither Rule mentions them. Both obligations live only in AD-5, so the two ADs that Bind them govern nothing (RET-15).

### 2.3 Over-specification: does the added length earn itself?

Word counts, Rule text only, before → after:

| AD | Before | After | Factor | Verdict |
| --- | --- | --- | --- | --- |
| AD-3 | 55 | 223 | 4.1× | **Earns it.** Every clause closes a C3 sub-fork. No implementation detail. |
| AD-5 | 74 | 379 | 5.1× | **Over-loaded — see RET-07.** Not over-*specified*: every clause is architectural. But AD-5 is now two decisions under one heading. |
| AD-9 | 172 | 387 | 2.3× | **Earns it, at the readability limit.** No data structures, no APIs, no config keys — every added clause resolves a proven divergence (C5, H7, H8). But it is now one 387-word paragraph carrying nine distinct obligations, and the per-axis state vocabulary (contract surface) is buried mid-paragraph. Recommend a small axis-state table, not a cut. |
| AD-12 | 64 | 313 | 4.9× | **Earns it, borderline.** The output-contract clause and the `:193` convention row layer correctly (AD-12 states the requirement, the row specifies it) rather than duplicating. |
| AD-14 | 49 | 181 | 3.7× | Earns it — the three phases are the finding. |
| AD-16 | 83 | 284 | 3.4× | **Earns it.** The target enumeration is load-bearing. Correctly declined to import the memlog's implementation-level "a sampled ciphertext no longer decrypts", keeping "a recorded, reproducible check per enumerated target" at spine altitude. One vestigial clause — RET-12. |
| AD-17 | 48 | 129 | 2.7× | Earns it. |

**Nothing in the pass imports implementation detail that belongs in code or the addendum.** No type names, no method signatures, no configuration keys, no wire payload shapes beyond the vocabularies that are genuinely contract surface. Judged against the standard the previous gate applied to AD-9 ("its unusual length earns itself"), all seven grown ADs pass. The problem is *structure*, not altitude: AD-5 and AD-9 have outgrown the single-paragraph Rule format.

### 2.4 Ledger restructure — obligation preservation and classification defensibility

**Obligation preservation: complete.** 15 pre-update rows → 17 rows. Every one of the 15 survives; 5 were corrected as the memlog intended; 2 are new (.NET CVE from C10, `tools/` from COD-06); the AD-8 namespace/architecture-test obligation was folded into row 7 rather than made a separate row (memlog says "a new row"; substance is identical). No obligation was silently dropped, and no row that the code has closed was deleted — the Web row correctly stays in with a narrowed obligation, which is AD-20 working as designed on its first day.

Two corrections worth naming, because both *added* obligation rather than removing it: row 2 now carries "the read path defaults an unpersisted status to `Indexed`" (COD-12, the more dangerous half), and row 8 now carries the green-tests-must-be-inverted work (H15).

**Classification defensibility: 15 of 17 sound, 1 arguable, 1 inconsistent.** 12 Yes / 5 No.

Assessed against `prd.md:189` — "Every architecture-critical active-foundation gap" — read as *a gap in the platform foundation that Phase-1 work runs on and that an architecture-governed hard gate names*. The term itself is undefined (RET-03), so this reading is my inference, not the spine's.

| Row | Class | Assessment |
| --- | --- | --- |
| 1 ingestion before EventStore acceptance | Yes | Sound — defeats AD-2 truth on every ingest path. |
| 2 `Indexed` without all axes / defaulted status | Yes | Sound. |
| 3 no EventStore replay path | Yes | Sound — recovery foundation. |
| 4 caller-controlled `IngestedBy` | Yes | Sound — G2-adjacent provenance forgery. |
| 5 shared Redis/Falkor credentials | Yes | Sound — G2/NFR8 hard gate names the missing mechanism. |
| 6 erasure incomplete | Yes | Sound — NFR16/FR39. |
| 7 provider SDK spread, no adapters, no guard | Yes | Sound. |
| 8 fusion canonicalization | Yes | Sound — G5/NFR25. |
| 9 graph seeding, kill switch, G1 harness | Yes | Sound — G1 cannot be run. |
| 10 no K8s EventStore gateway | Yes | **Conservative but defensible.** A Production deployment gap; G1-G5 run against AppHost/CI, not Production Kubernetes. Yes over-includes rather than under-includes, which is the right error direction. |
| 11 source/package independent lanes | No | Sound — release-evidence, not foundation. |
| 12 floating Redis/Falkor images + FalkorDB AGPL pin | **No** | **Inconsistent.** The Stack caveat `:232` says "Image digests remain mandatory in production", and rows 13 and 14 are classified Yes precisely because they block Production. This row blocks the same thing by the same reasoning and is classified No. It also now carries a licensing obligation (the AGPL pin duty) that nothing else covers. Either reclassify to Yes or state why Production-blocking is not sufficient for the classification. |
| 13 .NET CVE | Yes | Defensible — a dependency-currency gap rather than an architecture gap, but consistent with row 14 and correctly conservative. |
| 14 OpenBao | Yes | Sound. |
| 15 Web specimen evidence re-run | No | Sound — product Web is Phase 2. |
| 16 MCP `replicas: 2` ungated | No | Defensible — no ingress exists and Dapr policy is deny-by-default; the compensating control is AD-5-mandated though unguarded. |
| 17 `tools/` outside inventory gate | No | Sound — operator utility, outside the product runtime. |

### 2.5 AD ID stability

**Confirmed stable.** Headings diffed line-for-line against the pre-update snapshot:

- AD-1 … AD-13 — titles byte-identical.
- AD-14 — title changed "staged tenant migrations" → "phase-qualified tenant migrations", with `[ADOPTED · ratified 2026-09-12]`. **ID unchanged**, and the retitle is the C7 resolution, not a renumber.
- AD-15 … AD-19 — titles byte-identical.
- AD-20 — appended, not inserted.

No AD was split, merged, withdrawn, or reordered. Section headings are the same ten, in the same order. The `AD-n` reference graph therefore has no dangling edge from the update itself.

---

## 3. New findings

### RET-01 — AD-15 asserts a named owner and dated expiry that exist nowhere · **HIGH** · `:152`, `:324`

> `:152` "Direct Kubernetes-secret injection of Redis/FalkorDB credentials is a temporary alignment gap **with a named owner and a dated expiry recorded in the ledger**, not a second approved application secret path"

The ledger's Disposition cell for that gap, `:324`, reads "**Named owner and dated expiry owed**; blocks Production per AD-15". No owner name and no date appear anywhere in the 353 lines. The memlog's own account of this amendment says the expiry should be "recorded in the Stack note" — the Stack note `:232` does not mention it either.

This is the C9 defect class exactly: a statement of present fact about a mechanism that does not exist, in a `final` document. It is worse than C9 in one respect — C9's AD-8 claim was at least a claim about code, checkable by grep; this one is a claim about the *same document*, contradicted three sentences later by a table on the same page.

Related, lower-confidence: `:198` "Every environment uses the same Dapr component contracts and **the same pinned image digests**" is contradicted by `:232` and ledger row 12 ("floating AppHost/Aspire Redis and Falkor defaults are alignment gaps"). Borderline, because the preamble `:60` frames conventions as target architecture; AD-15's is not a target statement but a factual one about the ledger's contents.

**Fix.** Either record an owner and a date, or restate as an obligation: "…is a temporary alignment gap that **must** carry a named owner and a dated expiry in the ledger; neither is recorded today."

### RET-02 — the ratification the spine records is not reflected upstream, and the PRD declares itself authoritative on it · **HIGH** · `:11-14` vs `prd.md:62`, `:1225-1226`

The spine now carries `ratifications: AD-14 phase qualification (PRD Open Question 9) by Jérôme Piquot on 2026-09-12` and a binding header of FR1-FR75 / NFR1-NFR37 / G1-G6. The PRD, modified in the same working tree, still carries:

- `prd.md:1225` — "9. `[PHASE-BLOCKER / G6]` Architecture AD-14 is unphased … Owner: Architecture + Jerome. Resolve by qualifying AD-14 or approving an MVP rebaseline before the 2026-10-31 prerequisite checkpoint."
- `prd.md:1226` — "10. `[PHASE-BLOCKER / G6]` The final architecture spine binds only FR1–FR74/NFR1–NFR36; it must bind and trace FR75's … and NFR37 before G6."
- `prd.md:62` — "**Current release posture:** no-go … architecture must still resolve AD-14 phasing and extend its binding to FR75 and NFR37. The Release decision record and **Open Questions 1, 2, 9, and 10 are authoritative**."

So the PRD, which the spine lists as its first `source:`, states that both things the spine claims to have done remain undone, and names itself authoritative on the point. A reader arriving from the PRD concludes AD-14 is still unphased and the spine still stops at FR74.

The update pass demonstrably knew how to handle this: the memlog entry "Upstream correction owed and offered in the same pass" produced the `prd.md:927` fix (verified landed). The same treatment was not applied to Open Questions 9 and 10 — the two the PRD itself flags as the blockers this update exists to clear.

Secondary: outside the architecture folder, nothing witnesses the ratification. Its only evidence is the spine's frontmatter and its own memlog. C7 required "a recorded human ratification" from an owner the PRD names as "Architecture + Jerome"; a self-recorded entry in the artifact being ratified is the weakest admissible form.

**Fix.** Close Open Questions 9 and 10 in `prd.md` citing the spine's `ratifications` block and `:16-19`, and update `prd.md:62`. If the human ratification has not in fact been obtained, downgrade the frontmatter entry to `status: recorded pending PRD closure` rather than leaving two artifacts in contradiction.

### RET-03 — "active-foundation" is the ledger's normative classifier and nothing defines it · **MEDIUM** · `:316`, `:318`, `:182`

`prd.md:189` is the only occurrence of the term in the PRD, and it is undefined there. The addendum does not carry it. The spine adopts it verbatim as the ledger's fifth column and makes AD-20's entire obligation turn on it ("every gap classified active-foundation critical carries…"), also without definition.

The highest-consequence classification in the new ledger therefore has no stated criterion, so two reviewers can classify the same row differently and both comply — which is the divergence class this whole gate exists to close. It also makes row-by-row defensibility unassessable in principle; §2.4 above had to infer a criterion in order to review the column at all.

**Fix.** Define it once in the ledger preamble at `:316`, e.g. "*Active-foundation critical* means the gap sits in the platform foundation that Phase-1 work already runs on **and** an architecture-governed hard gate (G1-G6) names the mechanism it is missing." Then re-check rows 10, 12 and 16 against it.

### RET-04 — AD-16's completion conditions omit the grant revocation AD-5 assigns to them · **MEDIUM** · `:158` vs `:92`

AD-5 `:92`: "The per-tenant grant … is a tenant resource whose sole writer is AD-6's lifecycle workflow, created only by verified provisioning and **revoked as a completion condition of AD-16 erasure**."

AD-16 `:158` enumerates its completion conditions exhaustively — "completes only after every store … is purged, a durable access-telemetry erasure mapping/handoff is acknowledged, and the … crypto-shredding workflow irreversibly invalidates or deletes content access with verification" — and grant revocation is not among them. It is absent from AD-16's Binds `:156` and from the completion evidence's "target list, per-target outcome". AD-6's Rule `:98` does not mention grants either.

C4's own diagnosis was that "that rejection lives on the domain-command path while the grant lives on the channel-authorization path, and **no Rule conjoins them**". AD-5 now conjoins them from one side only. An implementer building the erasure workflow reads AD-16, finds a closed enumeration, and leaves `system:*` granted on the erased tenant ID — the exact outcome C4 described.

**Fix.** Add grant revocation to AD-16's completion clause and to its Binds; add "per-tenant grant revocation" to AD-6's Rule.

### RET-05 — the registry's new key-prefix rule is violated by the table's only row · **MEDIUM** · `:208`, `:210`

`:210` "Every row declares its reserved Redis key prefix, which no other row may claim."
`:208` the sole row's Scope cell: "Key includes tenant/case and canonical request identity; race, duplicate, timeout, outage, and expiry tests are mandatory." No prefix.

A new rule whose only governed instance does not satisfy it teaches the next author that the requirement is decorative, and the precedent row is the template a new author copies — which was H11's own argument about fail-open.

**Fix.** Add the reserved prefix to the `IPreflightDedupStore` row's Scope cell.

### RET-06 — the preamble and Testing convention generalise the C9 fix into two false claims · **MEDIUM** · `:60`, `:199`

> `:60` "Where a rule is not yet mechanically enforced, **it says so**; an unenforced rule is still binding but is a weaker guarantee, and **the Current Alignment Gaps ledger carries the enforcement obligation**."
> `:199` "Architecture guards enforce dependency and literal ownership where they exist; **the ledger carries the guards that do not**."

Both are false. `reviews/review-code-reality.md` §B.4 inventories 17 invariants: 6 unenforced, 1 enforced backwards, 2 partial. The ledger carries the AD-8 pair (row 7) and the AD-9 one (row 8). It does **not** carry AD-1's two — "dependency-pure domain namespaces contain no orchestration, host, or provider-SDK concerns" `:68` and "consumers … never reference AppHost or Server" `:68` — and AD-1's Rule does not say it is unenforced. Nor does AD-3's all-axis gate guard have a row (row 2's convergence asks for the *feature*, not a guard). AD-12's new name register `:134` likewise has no enforcement and no row.

The C9 fix was correct and precisely scoped to AD-8. Promoting it to a universal claim about the document re-creates the defect at document scale.

**Fix.** Soften both sentences to the obligation ("an unenforced rule **must** say so, and its enforcement obligation **belongs in** the ledger"), and add the AD-1 and AD-12-name-register guard rows.

### RET-07 — AD-5 has become a compound decision under a heading that does not announce it · **MEDIUM** · `:88-92`

74 → 379 words, now the second-longest Rule. It carries at least seven separable obligations: opaque-token comparison; the **identifier issuance grammar**; **unambiguous key composition**; bearer-authority preservation; the three-check internal-call gate; allowlist governance; grant ownership; pub/sub ingress authority; no-human-caller authority; tenant-lifecycle authorization.

The issuance grammar and key composition are a lexical/naming decision, not an authority-derivation decision. The heading ("Derive tenant, case, and caller authority server-side") does not announce them, the *Binds* line has to stretch to "identifier issuance", and the Consistency Conventions "Identifiers and time" row `:189` is where a reader would look for them. This is the one place where the pass traded structure for speed.

**Fix.** Extract issuance + composition as its own AD (the next free ID), keeping AD-5's "issuance is what makes ordinal comparison sufficient" sentence as the cross-reference. No text needs rewriting, only relocating.

### RET-08 — the issuance grammar does not close C6's case-collapse bullet · **MEDIUM** · `:92`

The grammar excludes "every character significant as a **metacharacter, delimiter, or wildcard**" in the six listed namespaces. Uppercase ASCII is none of those three, so it survives the grammar. C6's third bullet stands unchanged: Kubernetes object names are DNS-1123 lowercase, so tenants `Acme` and `acme` remain two ordinal-distinct tenants to the spine that collapse to one Kubernetes object name — and, by the same argument, to one lowercased credential or component name outside .NET.

Also unresolved at the same clause: the grammar is "one **documented** bounded ASCII grammar" with no statement of where it is documented or who owns it, in a Rule that otherwise names an owner for every artifact it creates.

**Fix.** State a single case convention at issuance (lowercase-only is the conventional choice given DNS-1123), and name the grammar's home — `Contracts.V1` alongside the wire-name register is the natural owner.

### RET-09 — the classification column does not precede the owner column · **LOW** · `:318`

Header order is `Gap | Violated rule | Required convergence | Disposition | Active-foundation critical`. C8's fix said "The classification column must come first"; the memlog's own account claims it landed as "the classification column preceding the owner column".

Substance is unaffected — AD-20 `:182` and the ledger preamble `:316` both scope the owner duty to critical rows, which is what actually prevents over-scoping G6. But the artifact does not match its own recorded intent, and a reader scanning the table meets the Disposition before knowing whether the row is in scope for it.

**Fix.** Swap the last two columns.

### RET-10 — the traceability matrix stayed an active companion · **LOW** · `:36-38`

The memlog records "architecture.md and the pre-FR71 traceability matrix move from active inputs to historical reference". `historicalSources:` received `architecture.md` and `ux-design-specification.md`; `companions:` still lists `_bmad-output/test-artifacts/traceability/traceability-matrix.md`, which DRF-14 identified as pre-FR71 and stale. Half of the stated amendment did not land.

**Fix.** Move it to `historicalSources`, or state in the companions list that it predates FR71.

### RET-11 — a capability-map row cites an artifact no AD defines · **LOW** · `:312`

> "Release-gate evidence closure | Gap ledger, **exception register**, evidence artifacts, tracker entries | AD-19, AD-20"

AD-20 speaks only of "a dated phase exception approved by product and architecture, each with a named owner and a tracker entry". No AD creates, names, locates or assigns an owner to an "exception register", and the spine contains no other occurrence of the term. The "Lives in" column of a capability-map row is supposed to point at something that exists or is being built.

Related: AD-20 says "a tracker entry" where `prd.md:189` says `sprint-status.yaml`. Naming it would make the obligation checkable.

**Fix.** Either give AD-20 the exception register as a named artifact with an owner, or drop it from the row and cite `sprint-status.yaml`.

### RET-12 — AD-16 retains the superseded payload-readability predicate · **LOW** · `:158`

The Rule now re-keys restore admission to the register — "refuses to rehydrate any record whose tenant appears there, **regardless of whether the payload is readable**" — and then closes, two sentences later, with the original clause: "…and **quarantine unreadable payloads during restore rather than rehydrating them**."

The two are compatible (the second is a subset), but the retained clause is verbatim the predicate C1 identified as the bug, sitting in the Rule's terminal position where an implementer skimming for the restore behaviour will find it. Vestigial text that re-opens the reading the amendment closed.

**Fix.** Delete it, or subordinate it explicitly ("in addition to the register check, an unreadable payload is quarantined and never rehydrated").

### RET-13 — AD-12's element list does not carry the per-axis state vocabulary it owns · **LOW** · `:134`

AD-12 owns the wire surface, mandates that "every element is always present on every surface", and requires byte-comparable golden vectors. Its mandatory element list still reads "degradation/excluded axes" and does not name the four-value per-axis vocabulary that AD-9 `:116` and AD-10 `:122` now fix (`unavailable` / `available` / `truncated` / `excluded`), nor the uncompleted-case-partition count AD-9 requires the packet to carry. AD-12 named the packet-state vocabulary in this pass; the axis-state vocabulary is the same kind of contract surface and was not.

**Fix.** Add the axis-state vocabulary and the partition count to AD-12's element list and to the name register's scope.

### RET-14 — AD-20 describes a state its own ledger does not occupy · **MEDIUM** · `:182`, `:320-336`

> `:182` "Every governed requirement and every gap classified active-foundation critical **carries** exactly one of a current evidence path or a dated phase exception approved by product and architecture, each with a named owner and a tracker entry."

Of the 12 rows classified critical, 11 carry "Owner and evidence path **owed**" and one carries "Named owner and dated expiry **owed**". That is, none carries either of the two things AD-20 says such a row carries. AD-20 also states no date, names no owner for the act of assigning owners, and does not name the tracker — while `prd.md:189` names `sprint-status.yaml` and the Release decision record sets 2026-10-31 for exactly this work.

As written the rule cannot be violated: it describes rather than obliges, and it describes something untrue. The ledger's honest "owed" is the right recording; AD-20's grammar is the problem.

**Fix.** Restate as an obligation with the PRD's checkpoint: "…**must carry**, by the 2026-10-31 prerequisite checkpoint, exactly one of …, each with a named owner and a `sprint-status.yaml` entry; a critical row with neither is an unmet G6 obligation."

### RET-15 — two Binds terms with no governing Rule clause · **LOW** · `:96`, `:150`

- AD-6 `:96` binds "per-tenant grants"; AD-6's Rule `:98` never mentions grants.
- AD-15 `:150` binds "the AD-5 allowlist artifact"; AD-15's Rule `:152` never mentions the allowlist.

Both obligations are stated only in AD-5 `:92`, which points *at* AD-6 and AD-15 for ownership and storage. The pointers are one-way: the ADs that Bind these terms govern nothing about them. Same seam as RET-04, at lower stakes.

**Fix.** Add one clause each — AD-6: the lifecycle workflow creates and revokes the per-tenant grant as a verified lifecycle resource; AD-15: the allowlist artifact is held in the operator scope and is versioned and reviewed like a secret, though it is not one.

---

## 4. What this pass did well, recorded because a re-test that lists only residue misrepresents the work

- **Zero decisions reversed, zero IDs renumbered, zero sections reordered.** The single hardest constraint on an update pass, met exactly.
- **The three findings that could only be closed by a *choice*** — H8 (`nl` axis vs rerank), H3 (case as partition vs authorization), C7 (AD-14 phasing) — were each closed by picking one side and saying so, not by adding hedged text that keeps both readings alive.
- **H13 is the best-executed amendment in the pass.** Every one of the eleven obligations DRF-04 enumerated is covered by `:193`, at the right altitude, including the ones easiest to skip (deterministic redirected output, the linear alternative for wide tables, no second created-unit line on duplicate delivery).
- **The ledger corrections added obligation rather than removing it.** Rows 2 and 8 each grew a harder work item than they carried before (the defaulted-`Indexed` read path; inverting green tests). An update pass under pressure to close findings usually drifts the other way.
- **The Stack caveat and gitlink are now independently verifiable, and I verified them.** `git ls-tree HEAD` matches both recorded SHAs, and the caveat states which side it read — the exact remedy H17 asked for.
- **Altitude was held.** Seven Rules grew 2.3×-5.1× and not one imported a type name, method signature, configuration key, or payload shape. The memlog's more implementation-flavoured drafts (the ciphertext-decryption check, the build-failing wire-name test) were left out of the spine — the first correctly, the second at the cost of H10's enforcement path.

---

## 5. Recommended disposition

**The spine is now safe to cite as current architecture authority for epic derivation, with two caveats that must be cleared first.**

**Clear before citing:**
- **RET-02** — close `prd.md` Open Questions 9 and 10 and update `prd.md:62`, or downgrade the spine's ratification entry. Two authoritative artifacts currently contradict each other on whether the update happened.
- **RET-01** — remove the false statement of present fact at `:152`.

**Clear before the erasure epic is sprint-selected:** C2's residual (name the register's store and state its durability/restore posture — it may not silently inherit the Deferred Redis state store) and **RET-04** (grant revocation absent from AD-16's completion conditions).

**Clear before the ledger is used as G6 evidence:** **RET-03** (define "active-foundation"), **RET-14** (make AD-20 an obligation with a date), **RET-09**, and re-check ledger row 12's classification.

**Authoring cleanups, no urgency:** RET-05, RET-06, RET-07, RET-08, RET-10, RET-11, RET-12, RET-13, RET-15, and H10's two residual sub-parts.

**Do not re-open:** all 10 criticals and all 17 highs, other than the three PARTIAL sub-parts named above. Every closed finding was verified against new spine text quoted in §1, not against the memlog's account of it.
