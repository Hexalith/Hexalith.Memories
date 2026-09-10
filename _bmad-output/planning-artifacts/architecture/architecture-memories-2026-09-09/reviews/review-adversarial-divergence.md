# Adversarial Divergence Review

**Artifact:** `ARCHITECTURE-SPINE.md`  
**Lens:** Two independently implemented units that each claim spine compliance but cannot interoperate or produce the same product behavior  
**Verdict:** **FAIL — 3 critical and 9 high-severity convergence gaps.** The spine has a strong center, but it still permits incompatible wire states, authentication behavior, degraded-query semantics, graph population, adapter ownership, and phase activation. It also contradicts the current PRD on Phase 1 graph population and the runnable Web conformance specimen.

## Method

For every finding, this review constructs two implementations one level below the feature-altitude spine. Both can make a plausible compliance argument from the current Rule text, yet their outputs, security decisions, deployment shape, or evidence differ. A finding is included only where the divergence is non-obvious, load-bearing, and not safely recoverable from ordinary code conventions.

## Critical findings

### C1 — The public ingestion-state wire vocabulary is contradictory

**Spine anchors:** AD-3 lines 65–69; AD-10 lines 107–111; Consistency Conventions line 150.  
**Source contract:** PRD Glossary and Async Ingestion Pipeline; PRD Open Question 8.

**Unit A — semantic-vocabulary serializer:** exposes `pending`, `extracting`, `embedding`, `projecting`, `indexed`, `failed` because the convention says these are the public states and the PRD calls them the one vocabulary.

**Unit B — V1-enum serializer:** exposes `Queued`, `Extracting`, `Embedding`, `Indexing`, `Indexed`, `Failed` because the same convention says internal states map “without renaming V1,” AD-10 preserves deliberate legacy JSON names, and the current V1 enum carries those spellings.

Both claim compliance. Their CLI JSON, MCP responses, REST payloads, status filters, and persisted workflow/status discriminators are incompatible. This is the exact unresolved choice in PRD Open Question 8, but the spine presents both sides as one settled convention.

**Required fix:** distinguish three things explicitly: (1) canonical product semantics, (2) exact V1 JSON enum strings, and (3) UI/CLI display labels. State which one is serialized on each V1 route and Evidence Packet field. Until a breaking-contract decision is made, preserve the exact current V1 wire spellings and record the semantic mapping without calling both “public.”

**Disposition:** discuss/resolve before finalization; not safely autofixable because it chooses a public compatibility contract.

### C2 — Internal Dapr callers can authorize tenants incompatibly

**Spine anchors:** AD-5 lines 77–81; AD-11 lines 113–117; Operational Security line 220; local/production identity statement line 213.  
**Source contract:** PRD NFR8, NFR10, NFR11, FR44, FR65; approved 2026-08-03 ARCH-2 boundary table.

**Unit A — app-token-as-system authority:** authenticates the Dapr application token, treats the calling app as an allowlisted `system:*` principal, and accepts tenant scope from the message payload.

**Unit B — mapped-claim authority:** authenticates the Dapr application token but also requires an app-id-to-system-principal allowlist and an independently derived allowed-tenant set before accepting payload tenant scope.

Both can claim an “explicit allowlist,” server-side backend resolution, and authorization before access. They disagree on whether a compromised or over-broad internal app can act in every tenant. The spine collapses REST/CLI, MCP, Dapr infrastructure callbacks, and trusted internal adapters into one rule and never names the source of the allowlist or the tenant grant for each boundary.

**Required fix:** restore the approved boundary matrix: external REST/CLI derives tenant and provenance from validated user claims/`sub`; MCP maps its authenticated context to the same server authorization contract; trusted internal adapters map authenticated app identity to a canonical finite `system:*` principal and an explicit tenant grant; case membership never authorizes. Name the single owner/configuration source for both allowlists and require deny-by-default for unknown app IDs.

**Disposition:** autofix from approved ARCH-2 language.

### C3 — Query behavior during a backend outage is not fixed

**Spine anchors:** AD-3 lines 65–69; AD-8 lines 95–99; AD-10 lines 107–111; Operational Consistency/Reliability lines 219–222.  
**Source contract:** PRD FR66 and NFR18; Evidence Packet degradation semantics.

**Unit A — completeness-first search:** fails a hybrid query when one configured backend is unavailable because AD-3 requires all three acknowledgements and AD-8 says to run enabled axes.

**Unit B — available-axis search:** returns results from healthy axes, marks the Evidence Packet degraded, and identifies the excluded axis because FR66/NFR18 require partial service.

Both can claim AD-3 compliance if Unit B treats it as ingestion-only and Unit A treats it as a general “logical outcome” rule. Clients receive mutually incompatible availability and error behavior. The spine never separates **ingestion completeness** (all three projections required before `Indexed`) from **query-time degradation** (already indexed data served on available axes).

**Required fix:** add an explicit rule: projection admission is all-three and same-version; query execution is available-axis, must return partial results when at least one selected axis remains usable, must list excluded axes and recovery guidance in the Evidence Packet, and must fail only when no selected axis can produce a safe response. Define whether readiness becomes degraded or unavailable for each backend loss.

**Disposition:** autofix from FR66/NFR18.

## High findings

### H1 — Phase 1 graph population is narrowed beyond the PRD

**Spine anchor:** AD-9 line 105: “create P1 explicit edges only.”  
**Source contract:** PRD Phase 1 graph inventory; FR46, FR50, FR51.

**Unit A:** treats any metadata-carried `CausationId`/`CorrelationId`, explicit reference, structural `contains`, and user annotation as “explicit,” but excludes AI-inferred `references`.

**Unit B:** treats ingest metadata and AI-produced similarity references as explicit-to-the-ingestion-contract and includes them in Phase 1.

Both have a plausible reading of “explicit edges,” but their G1 graph and retrieval results differ. The PRD explicitly permits Phase 1 `references` from explicit links **or AI-inferred content similarity**, plus `contains`, `annotates`, and metadata-carried causal/correlation IDs; only automatic EventStore-stream population is Phase 1.5.

**Required fix:** enumerate the Phase 1 sources exactly and reserve only EventStore-stream auto-population/dual event embedding for Phase 1.5. Do not use “explicit” as the umbrella noun.

### H2 — Weighted RRF is underspecified at precisely the interoperability seams

**Spine anchor:** AD-8 lines 95–99.  
**Source contract:** PRD FR17, FR19, FR63, NFR24, NFR25, and G1 graph-seeding rule.

**Unit A:** traverses auto-seeded graph candidates at depth 1, divides final score by the sum of configured weights including unavailable axes, and breaks equal scores by ascending ordinal ID.

**Unit B:** traverses at depth 2, renormalizes across only available axes, and breaks equal scores by descending ordinal ID.

Both use `k=10`, the named weights, union top-five seeds, weighted RRF, and `MemoryUnitId` as final tie-break. They yield different rankings and explanations. AD-8 omits the PRD’s required auto-seed depth ≤2, the normalization denominator for missing/optional axes, duplicate candidate handling, and tie-break direction/comparer. It also gives NL weight without stating that NL is Phase 1.5 event-only and not a fourth marketing axis.

**Required fix:** bind depth ≤2; candidate de-duplication by exact opaque ID; available-axis normalization against the best possible enabled contribution; ascending ordinal (`StringComparer.Ordinal`) final tie-break; and NL activation only for Phase 1.5 event units when requested/configured. If current code uses a different exact formula, surface that as a conflict rather than inventing a second compliant formula.

### H3 — Direct infrastructure-client ownership is undefined

**Spine anchors:** AD-7 lines 89–93; Structural Seed lines 176–193; Capability Map lines 228–238.

**Unit A:** puts `NRedisStack` and `NFalkorDB` implementations under `Hexalith.Memories.Server/Adapters` because the seed says Server owns adapters and AD-7 allows clients in approved adapters.

**Unit B:** puts direct clients only in the published Redis/infrastructure boundary package because the legacy accepted D30 prohibited direct infrastructure dependencies in product projects.

Both hide SDK types behind ports and inject keyed connections, yet their dependency graphs and package surfaces are incompatible. The seed omits `Hexalith.Memories.Redis` entirely even though it is a release-manifest package, and “approved Redis and FalkorDB adapters” is not an owning-project allowlist.

**Required fix:** name the exact projects permitted to reference each provider SDK and the role of the compatibility-only `Hexalith.Memories.Redis` facade. State whether Server may contain adapter composition only or provider SDK implementations. Add an enforceable architecture-test boundary.

### H4 — The EventStore project is mislabeled and can collapse the three EventStore contracts

**Spine anchors:** AD-2 line 63; Structural Seed line 179.

**Unit A:** treats `Hexalith.Memories.EventStore` as a pure domain aggregate/command/event package because the seed says exactly that.

**Unit B:** treats it as the Phase 1.5 CloudEvent product-integration package because the PRD package inventory and current source contain routing, subscription, deduplication, and ingestion-controller code as well as domain types.

Both can point to AD-2’s instruction to keep the contracts distinct. They will place adapters, domain handlers, and public dependencies differently. The “pure aggregate handlers” comment is false for the brownfield project and undermines the very three-contract distinction AD-2 is meant to preserve.

**Required fix:** describe the current mixed package honestly and define internal namespace/dependency boundaries, or decide to split it and list that split as an alignment gap. Do not imply an already-pure project.

### H5 — Evidence Packet equivalence can be weakened by “approved capability subset”

**Spine anchor:** AD-10 lines 107–111.

**Unit A:** returns the complete Evidence Packet on every evidence-bearing surface, varying only presentation.

**Unit B:** removes freshness, degradation, omitted-detail handles, or recovery fields from MCP/CLI because each surface may expose only its approved capability subset.

Both treat Contracts.V1 as the semantic source. The PRD requires one identical cross-surface trust envelope; capability subset applies to operations/tools, not to the mandatory trust semantics of a result.

**Required fix:** separate **capability subset** from **envelope invariants**. Name mandatory Evidence Packet fields/states for every search/traversal/inspection result, define allowed nullability/omission, and require semantic equivalence across REST, CLI JSON, MCP, and future Web composition.

### H6 — Workflow input capture can persist secrets or use incompatible configuration epochs

**Spine anchors:** AD-4 line 75; AD-12 lines 119–123; AD-13 lines 125–129.

**Unit A:** captures provider endpoint, model, dimensions, rate limits, and resolved API-key value in durable workflow input to satisfy “capture mutable inputs.”

**Unit B:** captures only a configuration epoch and secret name, resolving the current secret during each activity for rotation.

Both can argue deterministic replay and Dapr/OpenBao compliance; one persists secret material in workflow history, while the other can observe a rotated credential mid-run. The spine does not define which values are snapshotted, where the snapshot is owned, or how secret rotation interacts with replay.

**Required fix:** capture immutable non-secret configuration and a version/epoch at workflow start; store only secret references, never values, in workflow history; define activity-time resolution and rotation retry semantics; make source version plus config epoch observable in projection evidence.

### H7 — Web shape and activation contradict the current PRD

**Spine anchors:** Structural Seed line 184; Current Alignment Gap line 251; Deferred line 260.  
**Source contract:** PRD package/host inventory line 715 and NFR32/NFR35.

**Unit A:** builds only a non-runnable Razor class library and waits for a future host.

**Unit B:** preserves the currently specified runnable Web conformance specimen while keeping it non-activated as a product surface.

Both can claim “Web is not an activated product surface,” but only Unit B matches the PRD’s explicit “runnable web shell for accessibility/telemetry conformance.” This is a shape mismatch, not merely a phase label.

**Required fix:** distinguish the runnable conformance specimen from a future hosted product surface. State its current project output/host ownership and that NFR32/NFR35 product gates activate only when a product Web surface is selected.

### H8 — Production topology silently deploys Phase 1.5 MCP

**Spine anchors:** lines 213 and 223; Deferred line 260.  
**Source contract:** PRD Phase 1/1.5 split and L1–L3 release decision.

**Unit A:** deploys and independently scales MCP in every production environment because Operational Deployment says Kubernetes production “runs Server and MCP.”

**Unit B:** omits or marks MCP preview until the Phase 1.5 launch gates pass because the PRD makes MCP a no-launch surface on L1–L3 failure.

Both comply with different spine sections. The topology has no phase/activation qualifier and can bypass the product’s no-launch decision.

**Required fix:** make MCP, EventStore CloudEvent product ingestion, and corresponding samples/routes explicit Phase 1.5 activation units. Deployment assets may exist earlier, but ingress/publication/announcement remain disabled until L1–L3 pass.

### H9 — Known G1 blockers are missing from Current Alignment Gaps

**Spine anchors:** Current Alignment Gaps lines 240–251; AD-8.  
**Source contract:** PRD release record: FR17 graph auto-seeding and FR25 BM25+semantic control are partial; no owning story exists; G1 is not runnable.

**Unit A:** closes the six listed alignment gaps and declares the adopted architecture aligned, relying on the section as the finite obligation list.

**Unit B:** also implements graph auto-seeding, a two-axis BM25+semantic control, and the G1 protocol harness before claiming alignment.

Both can claim AD-8 because Unit A may implement union seeding but not the PRD depth/protocol, and FR25 is not mentioned anywhere in the spine Rule set. Their release evidence is incompatible; Unit A can present the N=8 diagnostic as sufficient architecture evidence.

**Required fix:** add the known FR17 and FR25 gaps, explicitly distinguish the N=8 diagnostic from G1, and state that architecture alignment does not imply G1/G3 product-gate passage.

## Medium findings

### M1 — Tenant normalization and comparison are not reproducible

AD-5 requires an “exact case-sensitive normalized tenant claim” but names no normalization algorithm. One unit can trim and Unicode-normalize NFC while preserving case; another can accept the token string byte-for-byte. Bind a canonical normalization function or declare tenant IDs opaque exact ordinal strings and normalize only at issuance. Name the comparer for route/body/claim equality.

### M2 — Per-backend physical isolation is not explicit

AD-5 and alignment gap 249 generalize “tenant-scoped backend credentials,” while the approved mechanism specifically binds Redis to per-tenant ACL users plus tenant-scoped resolution and FalkorDB to tenant-scoped databases/content-negative evidence. One unit can require per-tenant Falkor credentials; another can use a shared graph credential with separate databases. Define each backend’s security boundary, credential owner, and what evidence may transition it to verified.

### M3 — CLI configuration precedence is omitted

The PRD fixes precedence as flags → environment → config file, while secrets never use fallback resolution. The spine only says options validate at startup. Two CLI commands can invert environment/config precedence and both comply. Add the precedence convention and keep Dapr secret references outside that ordinary configuration chain.

### M4 — EventStore dependency identity is singular despite two evidence lanes

AD-15 requires distinct source and package evidence, but Stack lists only `Hexalith.EventStore 3.103.0` and then says every entry is an exact repository pin. Source mode is pinned by a gitlink/SHA, package mode by a catalog version. Split the Stack identity by lane, or call the table package-mode only and point source mode to the exact gitlink authority.

### M5 — Access-telemetry fail-open/fail-closed timing is ambiguous

AD-14 says writes are non-blocking and telemetry cannot corrupt domain outcomes, while activation gates fail closed. One unit can reject every product request when telemetry delivery is unavailable; another can buffer/drop telemetry after an admitted deployment. State explicitly: qualification/configuration fails closed before Production activation; once admitted, product requests remain non-blocking under the bounded buffering/delivery contract, with degradation surfaced and the profile disabled when its approved bound is exceeded.

### M6 — Health/readiness semantics under degradation are missing

FR72 verifies all backends while NFR18 keeps partial service available. One host can fail readiness on any backend loss and be removed from service; another can remain ready but degraded. Fix liveness, readiness, and capability-health states per dependency so orchestration behavior agrees with Evidence Packet degradation.

### M7 — The final ID tie-break direction is absent

“Use `MemoryUnitId` as final tie-break” ensures determinism inside one implementation, but two units can sort ascending versus descending or use culture-sensitive comparison. Bind ascending ordinal comparison over the exact opaque string.

### M8 — Tenant lifecycle introduces `suspension` without a product contract

AD-6 binds suspension although the PRD tenant lifecycle FRs name create/update/delete/verify and lifecycle status may exist only as implementation seed. Either define suspension’s externally visible behavior and authorization in a product/feature spec or remove it from the feature-level invariant.

## Low findings

### L1 — Managed-service licensing constraints are silent

The stack binds Redis Stack and FalkorDB, but the operational boundary omits the addendum’s SSPL/RSAL managed-service constraint and the required external-service/license boundary. Add a Deferred or operational constraint so a deployment unit cannot assume an unrestricted hosted offering.

### L2 — `/events/ingest` is an unrecorded route-versioning exception

The convention says product HTTP routes live under `/api/v1`; the EventStore Dapr subscription route is `/events/ingest`. Record it as an infrastructure adapter exception so one unit does not move it under `/api/v1` while another retains the Dapr subscription contract.

### L3 — “Nine release-manifest projects” is brittle wording

The binding source is `tools/release-packages.json`, not the prose count. Keep the manifest as authority and move the count to dated seed/verified facts, preventing a manifest update from making the spine self-contradictory.

## Recommended gate disposition

1. Resolve C1 with an explicit V1 wire-state decision; it is the only finding requiring owner choice.
2. Autofix C2, C3, H1, H7, H8, and H9 directly from the approved PRD/SCP language.
3. Tighten AD-8 for H2 and M7 against the current fusion implementation and PRD G1 protocol.
4. Reconcile H3/H4 against the brownfield project/package graph; do not invent a greenfield package split.
5. Add explicit envelope, workflow-snapshot, configuration, backend-isolation, health, and activation conventions for H5/H6 and M1–M6.
6. Put L1 in Operational Boundaries or Deferred; record L2 as a route exception; remove the prose package count per L3.

**Gate conclusion:** The spine is not yet safe as a build substrate for independent units. Its central paradigm and major ownership decisions are sound, but the critical/high findings must be corrected before `status: final`; otherwise independently built search, ingest, auth, deployment, and surface units can all claim compliance while producing incompatible behavior.

## Second Pass — Revised Spine

**Verdict:** **FAIL — one high-severity convergence blocker remains.** All three prior critical findings and eight of the nine prior high findings are closed by the revised spine. Weighted RRF still permits independently implemented engines to assign different ranks and therefore different public scores/orderings.

### Prior critical/high retest

| Prior finding / requested boundary | Second-pass result | Revised binding evidence |
| --- | --- | --- |
| C1 — status wire values | **Resolved** | The convention fixes exact lowercase V1 JSON values and separately maps product/display semantics (`queued`/`indexing` versus `pending`/`projecting`) at spine line 177. |
| C2 — internal authorization | **Resolved** | AD-5 requires deny-by-default Dapr workload policy, app-token channel protection, one operator-owned finite app-ID allowlist, a canonical `system:*` principal, and an explicit tenant grant; it also states that channel authentication is not tenant authorization (lines 80–84). |
| C3 — degraded query behavior | **Resolved** | AD-10 separates query-time available-axis service from AD-3 ingestion completeness, requires a partial response with degradation evidence when one safe axis remains, and permits failure only when none remains (lines 110–114); readiness is capability-aware (line 181). |
| H1 — Phase 1 graph population | **Resolved** | AD-11 enumerates `contains`, `annotates`, explicit/AI-inferred `references`, metadata-supplied causal/correlation edges, and reserves automatic EventStore-stream population for Phase 1.5 (lines 116–120). |
| H2 — graph population/fusion | **Partly resolved; blocker below** | Axis weights, `k`, available-axis normalization, exact-ID deduplication, final ordering, NL activation, and union auto-seeding are bound in AD-9 (lines 104–108); the current gap requires depth-two seeding and the real G1 harness (line 291). Rank construction remains ambiguous. |
| H3 — adapter boundary | **Resolved** | AD-8 confines provider SDK use to the two named physical adapter assemblies' composition roots or internal provider namespaces, keeps Dapr behind Dapr APIs, restricts direct Redis primitives, and makes `Hexalith.Memories.Redis` a compatibility facade only (lines 98–102, 211–214). Ports preserve interoperability even where either approved physical assembly composes its own internal adapter. |
| H4 — EventStore physical boundary | **Resolved** | The design and seed now describe the brownfield assembly as mixed while making only `Domain/` dependency-pure, and AD-2 keeps domain truth, CloudEvent product integration, and evidence lanes distinct (lines 39, 62–66, 211). |
| H5 — Evidence Packet | **Resolved** | AD-12 makes capability subsets operation-only and enumerates the mandatory trust-envelope semantics for every evidence-bearing result through the shared `Contracts.V1` type (lines 122–126, 178). |
| H6 — workflow secrets/configuration | **Resolved** | AD-4 snapshots immutable non-secret configuration plus its epoch and stores references only; AD-14 resolves secret references in activities so rotation retries do not put values in workflow history (lines 74–78, 134–138, 179). |
| H7 — Web shape | **Resolved** | The seed distinguishes today's RCL, the required runnable conformance specimen, and a non-activated product UI; the gap requires the conformance host, while hosted product Web remains deferred (lines 220, 296, 308). |
| H8 — MCP/EventStore activation | **Resolved** | AD-12 makes both product capabilities inactive until L1–L3, topology calls MCP gated, and the alignment gap forbids ingress/publication/announcement before passage (lines 126, 219, 225, 249, 261, 297). |
| H9 — G1 gaps | **Resolved** | The alignment ledger now names missing graph auto-seeding, graph kill switch, BM25+semantic control, representative G1 protocol, and the insufficiency of N=8; it also states that architecture alignment is not product-gate passage (lines 291, 299). |

### Remaining blocker — RRF rank construction is not deterministic across implementations

**Severity:** High  
**Spine anchor:** AD-9 line 108.  
**Bound requirements:** FR19, NFR24, NFR25, G1.

**Unit A — one-based competition ranking:** sorts an axis by raw score, assigns tied scores ranks `1, 1, 3`, calculates `1/(10+rank)`, and allocates rank positions before suppressing repeated `MemoryUnitId` rows. This matches the current `FusionEngine` behavior.

**Unit B — one-based dense ranking with early deduplication:** removes repeated IDs first, assigns the same tied scores ranks `1, 1, 2`, and applies the identical contribution formula and normalization.

Both units satisfy “tied raw scores share rank,” exact-ID deduplication, `1/(10+rank)`, the named weights, available-axis normalization, and the final ordinal tie-break. A candidate below a tie or duplicate receives a different per-axis contribution, composite score, explanation, and potentially final position. NFR24/NFR25 require a documented deterministic algorithm, so determinism inside each unit is insufficient: the spine must make the units agree.

**Required convergence:** bind rank origin and tie progression (for the current implementation, one-based competition rank: `rank = first zero-based position in the ordered axis list + 1`, yielding `1,1,3`), state whether exact-ID deduplication occurs before or after rank allocation, and require a contract test containing both a raw-score tie and a repeated ID followed by another candidate. If early deduplication is preferred instead, record that as an intentional implementation change rather than leaving both orders compliant.

### Second-pass gate conclusion

The revised spine now closes status, authentication, degradation, graph-source, adapter, EventStore, Evidence Packet, workflow-secret, phase-activation, and G1-ledger divergence. It should remain `draft` until the RRF rank/deduplication order is bound and tested; after that correction, no prior critical/high blocker from this lens remains.

## Third Pass — RRF and Query Scope

**Verdict:** **FAIL — two high-severity ranking convergence gaps remain.** The prior second-pass blocker is substantially corrected: AD-9 now filters invalid entries, retains the first exact-ID occurrence before ranking, fixes one-based competition ranks (`1,1,3`), distinguishes null from empty axes, fixes denominator participation, and records the missing implementation/golden-vector work. The rule still does not define raw-score tie equivalence or the provider order of a graph axis assembled from multiple case-isolated traversals.

### H10 — Raw-score tie equivalence is unspecified

**Spine anchor:** AD-9 line 108.  
**Bound requirements:** FR19, NFR24, NFR25, G1.

**Unit A — exact numeric ties:** treats two finite raw `double` values as tied only when they compare exactly equal. An axis with scores `0.9`, `0.8999999999999999`, `0.8` receives competition ranks `1,2,3`.

**Unit B — tolerance/provider ties:** treats scores within `1e-12` as tied, or preserves a provider's equal-rank bucket. The same axis receives ranks `1,1,3`.

Both retain provider order, drop invalid records, deduplicate before ranking, use competition ranks, and can reasonably claim that “tied raw scores share rank.” They emit different per-axis contributions and composite scores. The required golden-vector tests at line 301 do not close the architecture choice because independently built units can encode different vectors while each claiming compliance.

**Required convergence:** define the tie predicate in provider-neutral contract terms. The narrow current-code choice is exact finite `double` equality (including an explicit decision for signed zero), but a provider-supplied rank/bucket is also viable if represented in the port contract. If comparison is positional, require provider order to be monotonically non-increasing with equal-score rows contiguous; otherwise define rank grouping across the whole canonicalized axis. Add a vector where two near-equal but non-equal scores precede a third result.

### H11 — Tenant-wide graph fan-out has no canonical axis merge

**Spine anchors:** AD-7 line 96; AD-9 line 108; AD-11 line 120.  
**Bound requirements:** FR17, FR19, FR25, FR33, FR34, NFR24, NFR25, G1, G4.

For a tenant-wide hybrid query, the global syntactic/semantic seeds can belong to several cases. AD-7 correctly requires every graph traversal to remain inside its seed's authoritative case, so the graph axis is assembled from multiple case-local traversal responses. The spine does not define the single “provider order” of that assembled axis.

**Unit A — seed-order concatenation:** traverses case-local seed partitions, concatenates each response in syntactic/semantic seed order, retains the first duplicate ID, and assigns AD-9 ranks to that list.

**Unit B — score-ordered merge:** traverses the same seeds within the same cases, merges all case-local responses by descending graph proximity and ordinal ID, then retains duplicates and assigns AD-9 ranks.

Both use the global top-five seed union, never cross a case boundary, preserve mandatory case attribution, and feed one canonicalized graph list to AD-9. Their graph ranks, duplicate winner, composite scores, pagination, and Evidence Packet explanations differ. Neither AD-7's “independently ranked units” nor AD-9's “provider order” selects an aggregation rule when there are multiple provider calls.

**Required convergence:** bind the tenant-wide graph-axis assembly algorithm: seed ordering and seed deduplication; whether traversal is per seed or grouped per case; how repeated graph candidates are chosen; and the deterministic global order used before AD-9. If graph proximity is comparable across case-local calls, specify the score-then-ordinal merge. If it is not comparable, specify a nested rank fusion or another provider-neutral merge and expose that method in explain evidence. Add one golden vector with two cases, overlapping seed lists, and different traversal completion orders.

### Third-pass gate conclusion

No regression was found in status, authorization, ingestion/query degradation, phase activation, EventStore boundaries, or Evidence Packet semantics. AD-9's former rank-origin and early-dedup ambiguity is closed. The spine should remain `draft` until H10 and H11 choose one tie and tenant-wide graph-merge convention; asynchronous completion order or adapter-specific floating-point tolerance must not influence public ranking.

## Fourth Pass — Final Focused Gate

**Verdict:** **PASS — no critical or high-severity divergence remains under this lens.** The revised AD-9 uniquely resolves H10 and H11, and the surrounding patch introduces no new architecture-level incompatibility.

### Focused counterexample retest

| Prior blocker | Result | Binding reason |
| --- | --- | --- |
| H10 — raw-score tie equivalence | **Resolved** | AD-9 line 108 accepts only finite scores and defines a tie by finite `Double.Equals`, explicitly forbidding epsilon comparison. Near-equal values therefore cannot be grouped differently; signed zero behavior is also fixed by the named runtime predicate. Canonicalization retains the first exact ID before one-based competition-rank allocation. |
| H11 — tenant-wide graph fan-out merge | **Resolved** | AD-9 line 108 partitions seeds by ascending ordinal case ID, collapses repeat candidates to the maximum finite graph score within the authoritative case, and defines the global pre-rank merge as descending graph score, ascending ordinal case ID, then ascending ordinal `MemoryUnitId`. Traversal completion order, seed-call order within a case, and adapter enumeration order cannot change the graph-axis rank list. |

### Regression check

- The RRF pipeline now has one ordering: provider order; blank/non-finite filtering; first exact-ID retention; exact tie grouping; competition ranks; `1/(10+rank)` contribution; available-result-axis normalization; descending composite score; ascending ordinal ID.
- Case-scoped queries filter every candidate and relationship to one authoritative case. Tenant-wide queries retain cross-case discovery while case-partitioning graph traversal and using the new canonical graph merge (AD-7 line 96; AD-9 line 108).
- Null and empty axes remain distinct in Evidence Packet health but neither changes the denominator; AD-10 still returns safe empty/partial responses without weakening AD-3 ingestion completeness.
- The new alignment rows correctly classify missing preprocessing/golden vectors and tenant-wide graph merge as implementation obligations, not alternative decisions (lines 301–302).
- No new critical/high conflict was introduced in authorization, adapter boundaries, Evidence Packet semantics, workflow state/secrets, status wire values, phase activation, or G1 accounting.

### Fourth-pass gate conclusion

Two independently built ranking/query units following the spine now converge on tie identity, duplicate handling, competition rank, tenant-wide graph aggregation, normalization, and final ordering. This adversarial-divergence gate passes. Open alignment work and product gates remain implementation/release obligations and do not reopen an architecture choice.
