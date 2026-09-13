# Adversarial Divergence — Pass 5 (Post-correction Retest), 2026-09-13

**Verdict: REVISE — the correction round closes the projection-identity, zero-hit denominator, per-record writer, and Phase 1 epoch-target contradictions, but the spine still contains two critical incompatibilities and twelve material residuals. Most are carried pass-4 adjacent-rule defects; the most serious fresh defect is that erasure has no tenant-wide quiescence/fence, so a compliant already-started projection activity can recreate readable derived content after the erasure workflow has purged and verified it.**

Target: `ARCHITECTURE-SPINE.md` (428 lines, `updated: 2026-09-12`, AD-1…AD-23).
Baseline: `reviews/review-adversarial-pass4-2026-09-12.md`.
Working memory: `.memlog.md` through the 2026-09-13 resume event.

Read-only review. The spine and memlog were not modified.

## Method

For each finding, the two units are one level below this feature-altitude spine. Each follows the rule assigned to it literally; the incompatibility is caused by a missing cross-unit protocol or by two Rule clauses that cannot both be honored. A simple code defect against an unambiguous Rule is not counted as an adversarial divergence. Existing alignment gaps are cited only when they prove that the missing authority is real or make qualification evidence impossible.

The post-pass-4 text was tested especially for shared-state ownership, tenant authority, erasure/replay, epoch transitions, ranking/axis state, telemetry retention, and release-lane evidence.

## Pass-4 contradiction retest

| Pass-4 item | Pass-5 verdict | Current evidence |
| --- | --- | --- |
| ADV4-01 — platform-partition writers | **Closed for the four ordinary writers** | AD-21 L204 now confines writes per record type. A new adjacent hole remains for reversals: ADV5-03. |
| ADV4-02 — zero-hit `truncated` denominator | **Closed** | AD-9 L132 and AD-22 L210 both make returned hits, not state name, the denominator test. |
| ADV4-03 — Phase 1 rebuild target | **Closed** | AD-3 L96 makes generation/epoch part of document identity; AD-14 L162 declares the rebuild epoch as the Phase 1 target and confines separate-resource machinery to Phase 2/3. |
| ADV4-04 — shipped key misread | **Partially closed** | AD-23 L216, the Redis registry L241, and ledger L400 now record that the shipped key hashes `id` and omits `source`; AD-4 L102 still says the opposite. See ADV5-11. |
| ADV4-05 — publisher-selected tenant | **Still open / contradictory** | AD-5's opening artifact schema still requires a `source`-prefix map, while its ingress clause forbids it. See ADV5-02. |
| ADV4-06 — readiness on register outage | **Closed by choosing fail-unready** | Health L229 requires the marker to read back; AD-21 L204 makes a read error unavailable. This is an availability trade-off, but no longer a two-reading contract. |
| ADV4-07 — unnamed bootstrap runner | **Still open** | “Named platform-bootstrap step” still names no component, principal, one-time condition, or continuity check. See ADV5-07. |
| ADV4-08 — circular retire ownership | **Still open** | AD-14 calls it AD-3's retire step; AD-3 calls it AD-14's. See ADV5-08. |
| ADV4-09 — sequence after a restore gap | **Still open** | “Highest sequence it can prove” and “reconciled” remain undefined. See ADV5-04. |
| ADV4-10 — export bundle sequence field | **Closed by AD-21's stronger general rule** | AD-21 L204 requires every export bundle to record the current sequence; AD-16's shorter index-field list is not expressly closed. |
| ADV4-11 — four axis-state encodings | **Partially closed** | AD-22 requires all four shapes in one name register, but that register and its shape remain absent and blocker-ledgered at L378/L403. No implementation may claim the canonical packet evidence yet. |
| ADV4-12 — omission versus explicit null | **Still open** | Both instructions remain in AD-12 L150. See ADV5-09. |
| ADV4-13 — deletion/content-store lifecycle operations | **Closed narrowly** | The exhaustive AD-5 list includes tenant deactivation and AD-16 erasure; AD-16's closed target list includes the tenant content store. |
| ADV4-14 — universal delimiter | **Still open** | AD-23 still requires one delimiter across mutually incompatible naming schemes. See ADV5-10. |
| ADV4-15 — `Erased` transition crash window | **Still open** | AD-6 still places every transition on the tenant-keyed stream while deriving `Erased` from a later platform tombstone. See ADV5-01 and ADV5-06. |
| ADV4-16 — backpressure versus truncation | **Still open** | AD-18 still forbids backpressure changing axis state while AD-22 requires a provider cutoff to report `truncated`. See ADV5-14. |
| ADV4-17 — DR register topology | **Deferred, not closed** | AD-21 still requires one reachable instance; Deferred L427 postpones the topology. It is not an active-surface blocker until multi-region/DR is selected. |
| ADV4-18 — permanent content digest | **Still open** | AD-16 retains a bare content digest in AD-21's non-purgeable append-only partition. See ADV5-05. |
| ADV4-19 — document identity across resources | **Closed** | AD-3 now keys identity by generation and epoch, allowing one disjoint document per pair. |
| ADV4-20 — undeclared CloudEvent character set | **Still open** | AD-4 still names a declaration owned nowhere and absent from the ledger. Folded into ADV5-11. |

## Findings

### ADV5-01 — critical — Erasure has no tenant-wide quiescence or write fence, so a valid in-flight projection can resurrect content after verified deletion

**Units.** Projection activity/write-fence unit (AD-3/AD-4/AD-5) and tenant-erasure unit (AD-16/AD-21).

**Literal compliance.** AD-5 L108 makes background work revalidate tenant authority “at each activity boundary.” AD-4 L102 makes an activity a bounded idempotent I/O operation, and AD-3 L96 admits a write for an active or declared pair when its own `sourceVersion` is not older. None of those clauses requires the derived-store write itself to retest lifecycle state atomically with the write. AD-16 L174 lets erasure complete after it purges each enumerated target and performs a sampled, reproducible verification; it declares no tenant-wide generation fence, drain barrier, or final CAS that excludes an activity which crossed its boundary while the tenant was still `Active`.

**Pair.**

- Build A's vector activity revalidates `Active`, reads content, and begins a bounded provider/write operation. The erasure workflow then moves the tenant to `Deleting`, purges and samples Redis/FalkorDB, shreds the tenant key, writes its completion evidence and tombstone, and returns success. The already-authorized activity finishes afterwards and writes its captured derived content under AD-3's still-declared tuple.
- Build B installs a tenant-wide erasure fence before purge, drains or invalidates every admitted projection, then purges and verifies. No late write is admitted.

Both implement every boundary the spine names; only Build B implements a boundary the spine never names. Build A leaves readable text, embeddings, or graph material after the operation has emitted authoritative erasure completion evidence. Replay is correctly refused by AD-21, so there is then no authoritative recovery path that naturally discovers and removes the resurrected projection.

**Required correction.** Assign one tenant-wide erasure generation/fence to AD-16, acquired after the lifecycle transition to `Deleting` and checked atomically by every AD-3 write. Erasure must drain or fence all work admitted under the previous lifecycle generation before its final purge, and completion must CAS against that fence so a late write either fails or keeps deletion incomplete.

### ADV5-02 — critical — AD-5 still both requires and forbids publisher-controlled `source` routing

**Units.** Operator-artifact schema unit and Dapr pub/sub authorization unit.

**Hard contradiction.** AD-5 L108 first defines the single operator artifact as carrying the “CloudEvent-`source`-prefix-to-tenant routing map.” Later in the same Rule it says `source` is publisher-set, a source-keyed routing map lets the publisher choose its tenant, and such a map “is forbidden by this decision's own *Prevents*”; the permitted map is instead keyed by the authenticated channel identity (subscriber component, topic, and Dapr-authenticated publisher).

The artifact-schema unit cannot remove the source map without violating the exhaustive three-content declaration. The ingress unit cannot consume it without violating the explicit prohibition. Keeping both does not help: a source claim and an authenticated-channel claim can resolve to different tenants, and the Rule gives no precedence because one of them is forbidden while simultaneously required.

**Concrete incompatibility.** Publisher `p1`, authenticated on a channel mapped to tenant `alpha`, emits a CloudEvent whose `source` matches tenant `beta`. One compliant implementation follows the artifact schema and routes to `beta`; the other follows the ingress prohibition and routes to `alpha` or rejects. This is the cross-tenant admission ADV4-05 identified and the correction round did not propagate to AD-5's artifact definition or Operational Boundaries L321.

**Required correction.** Replace every source-prefix artifact reference with the authenticated channel-identity tuple, and state whether the resulting tenant is also required to appear in the publisher app ID's ordinary per-tenant grant. The answer must be identical in the three-check internal-call rule, pub/sub clause, artifact schema, Operational Boundaries, and ledger L385/L386.

### ADV5-03 — high — The new per-record authorization has no writer for a tombstone reversal

**Units.** Platform-partition authorizer and operator recovery procedure.

AD-21 L204 now confines each write kind: AD-16 may append a non-reuse tombstone or erasure completion evidence; AD-6 a lifecycle projection; export a bundle-index record; AD-17 a purge-completion record. It then requires a tombstone written in error to be corrected by an operator procedure that appends a reversal. A reversal is none of the admitted record kinds, and no principal owns it.

- A strict authorizer rejects the reversal because “no principal may append outside its record type.”
- A recovery implementation admits any operator principal because “an operator procedure” must be able to append it.

The first makes a spurious tombstone permanently poison tenant issuance; the second gives a broad operator the ability to reverse any genuine non-reuse tombstone. Both are literal readings of adjacent sentences.

**Required correction.** Define reversal as its own record kind, assign exactly one principal/procedure, require a reason and linkage to the reversed record, and state the review/evidence gate. The effective-state rule must consume only a reversal authorized by that path.

### ADV5-04 — high — A restored register can regain a high sequence without regaining the erased-tenant facts that sequence is supposed to prove

**Units.** Platform-partition restore unit and restore/import admission unit.

AD-21 L204 admits an artifact when live register sequence is at least the artifact's sequence. A restored register “resumes at the highest sequence it can prove,” and a higher-stamped artifact fails only until the register is “reconciled.” Neither `prove` nor `reconciled` requires reconstruction of every missing effective-state record. The partition also has several independent writers, while the Rule does not say whether the sequence is a single EventStore stream position, a CAS counter, or a per-record sequence.

After backup at sequence 100, sequence 101 records tenant `alpha` erased. The platform backup is restored at 100 while an export artifact proves that sequence 101 existed.

- Build A advances the sequence/counter to 101 from the stamped artifact and calls the register reconciled. The missing tombstone is still absent, but artifact 101 is now admitted and `alpha` can be restored or reissued.
- Build B remains unavailable until it reconstructs record 101 and its effective state, regardless of the counter it can prove existed.

Both satisfy the current numerical language. Only Build B preserves what the number is meant to attest.

**Required correction.** Define one globally ordered append position for the effective register stream and define reconciliation as restoration of every record through the proven high-water mark, with continuity evidence. A counter or unrelated platform-partition write must never close a gap. Admission must verify continuity plus effective state, not `>=` alone.

### ADV5-05 — high — The append-only bundle index permanently retains content-derived confirmation material that erasure says must be purged

**Units.** Export/bundle-index unit and erasure verification unit.

AD-16 L174 registers each export bundle on AD-21's partition with a bare “content digest.” The same Rule requires every store holding tenant content or content-derived material to be in its closed purge-and-verification list. AD-21 L204 makes its partition non-shredded and append-only, and its per-record authorization defines no purge or redaction of bundle-index records.

- The export unit retains the digest forever, obeying the append-only index and preserving import admission.
- The erasure unit treats a digest of tenant content as content-derived material and must purge it before completing, but the index is absent from its closed list and the partition forbids the purge.

A retained bare digest is a confirmation oracle: an admitted operator reader can hash a candidate bundle and learn that it was the erased tenant's content even after ciphertext no longer decrypts.

**Required correction.** Store only a content-free bundle identifier plus a keyed authenticator whose verification key is destroyed with the tenant, or explicitly classify and govern the retained digest as an erasure exception. Add the index to AD-16's closed enumeration either as a purge target or as an expressly non-verifiable post-shred record.

### ADV5-06 — high — AD-6 owns a state vocabulary but not the legal transition graph that independently built lifecycle operations share

**Units.** Provision/repair workflow and authorization/lifecycle projection consumer.

AD-6 L114 fixes seven state names and says AD-6 is the sole state-machine writer, but it never declares legal transitions, transition preconditions, or which failed state a given operation may repair. Ledger L382 itself admits “the transitions are not declared” and asks implementation to declare them, although the ledger preamble L352 forbids `Required convergence` from introducing a requirement absent from the Rule.

- Repair A allows `Failed -> Active` after resource verification.
- Repair B requires `Failed -> Provisioning -> Active`, and permits `CompensationFailed -> Deleting` but not `-> Provisioning`.

Both use only the closed vocabulary, commit every transition before touching authorized resources, and preserve AD-6 as sole writer. An authorization consumer sees different periods of `Active`, while an independently built compensation workflow cannot know which retry path is legal. The differences affect whether tenant traffic is authorized, not merely UI presentation.

**Required correction.** Put the legal transition graph and operation preconditions in AD-6 (a diagram is appropriate), including `Deactivated`, `Deleting`, `Erased`, `Failed`, and `CompensationFailed`; then make L382 restate that graph rather than invent it.

### ADV5-07 — high — “Named platform-bootstrap step” still permits server auto-initialization of a wrong empty partition

**Units.** Deployment bootstrap unit and Server readiness/startup unit.

AD-21 L204 calls for a “named platform-bootstrap step” but supplies no name, owning component, authenticated principal, one-time rule, or continuity input. It only says the step runs before provisioning and writes an initialized marker. Health L229 treats that marker reading back as sufficient readiness evidence.

- Bootstrap A is Server startup: if the configured EventStore address points at a fresh partition, each replica idempotently writes the marker, reads it back, becomes ready, and sees an empty erased-tenant set.
- Bootstrap B is an operator-controlled deployment job that refuses initialization unless it can prove this is a new tenant population or has imported the prior register lineage.

Both are deployment-time steps before provisioning and can be “named.” In the misdirected-store case, Build A turns a configuration mistake into an available empty authority and permits reuse/replay; Build B stays unready.

**Required correction.** Name the component and principal, make initialization one-time and exclusive, and require a population/lineage identifier plus an explicit new-population ceremony. Ordinary Server startup may verify the marker but may never create it.

### ADV5-08 — high — The retired epoch has circular ownership and can survive indefinitely

**Units.** Projection coordinator and migration/rebuild workflow.

AD-14 L162 says that after activation “AD-3's retire step deletes” superseded documents and checkpoints. AD-3 L96 says “AD-14's retire step deletes” them. No decision names the caller, retry state, completion evidence, or authority for that step.

- The coordinator implements acknowledgement and fencing and waits for the AD-14 migration owner to retire.
- The migration workflow activates and waits for the AD-3 coordinator to retire.

Both follow the ownership pointer assigned to the other. Old embeddings, graph data, tombstones, and checkpoint records remain query-inactive but tenant-resident, inflating quota and becoming additional erasure targets that the closed AD-16 enumeration does not name by epoch.

**Required correction.** Assign retire to one workflow, define its resumable state and verification, and say whether activation success is reported before or after retirement. The other AD should cite that owner without possessive ambiguity.

### ADV5-09 — high — AD-12 still makes unauthorized omission distinguishable from ordinary absence and breaks byte-equality

**Units.** Evidence Packet canonical serializer and authorization-aware detail presenter.

AD-12 L150 requires every packet element to be present, absent values to serialize as explicit JSON `null`, and no evidence-bearing serializer to omit nulls. The same Rule says detail withheld for authorization is “omitted with no handle” and must be indistinguishable from absence.

- Serializer A emits `"detail": null, "handle": null` for both ordinary absence and authorization withholding.
- Presenter B omits both properties when withholding, following “omitted with no handle,” while ordinary absence uses explicit nulls.

Both follow a direct sentence in the Rule. Their extracted packet bytes differ, so AD-12's cross-surface golden vector cannot pass; worse, omitted versus null is the existence oracle the indistinguishability clause forbids.

**Required correction.** State that “omitted detail” means a present property whose value is canonical null, with the same bytes as ordinary absence. Reserve the complete four-state axis shape in the name register before claiming canonical packet evidence.

### ADV5-10 — high — One composition delimiter cannot satisfy every naming scheme AD-23 binds

**Units.** Kubernetes/Dapr resource provisioner and Redis/Dapr-state key composer.

AD-23 L216 requires one reserved delimiter for every composed key, ACL pattern, index name, state key, secret path, and object name. The delimiter must be excluded from each identifier grammar and from every significant-character set. Tenant identifiers are RFC 1123 labels and are composed into Kubernetes and Dapr object names; the shipped Redis family at L241 uses `:`.

- Composer A selects `:` to preserve `dedup:{tenant}:{case}:{hash}` and other key families. A Kubernetes name such as `memories:alpha:index` is invalid.
- Composer B selects `-` for DNS-1123 names. `-` is legal inside the tenant grammar, so composition is not injective (`a-b` + `c` versus `a` + `b-c`) and existing colon-key/workflow identities change.

No single character simultaneously meets all the declared alphabets and naming schemes. The missing grammar artifact cannot implement an impossible invariant.

**Required correction.** Define an injective encoding per composition scheme (for example length-prefixing or reversible base encoding) and name the schemes in the tracked artifact. Do not require one literal delimiter across DNS names, secret paths, SQL identifiers, and arbitrary data-store keys.

### ADV5-11 — high — CloudEvent identity is still described two incompatible ways inside the corrected spine

**Units.** Durable duplicate-suppression unit and preflight Redis reservation unit.

AD-4 L102 requires identity `(tenant, case, canonical source, id)` and still asserts that “the shipped reservation key already satisfies” composition “by hashing `source`.” AD-23 L216 and Registry L241 now state the verified opposite: the shipped key hashes `id`, omits `source`, and is an alignment gap; ledger L400 requires adding source or amending AD-4.

The target identity is clear, but the current-compliance assertion is not. An ingestion epic can retain the shipped key based on AD-4's explicit blessing while a dedup epic changes it based on AD-23 and L400. Their workflow instance IDs differ, so the same delivery can schedule twice across a rolling deployment; two publishers using the same `id` can also suppress one another in the old lane.

The undeclared “bounded character set” for `source` and `id` in AD-4 remains a second hole: it is neither AD-23's per-class issuance grammar nor a named field in that artifact, and no ledger row owns it despite the preamble L76 requiring one.

**Required correction.** Remove the stale AD-4 blessing; make the tracked identity/encoding artifact own the CloudEvents field productions, maximum lengths, canonicalization, and the new key shape; require a rolling migration/dual-read rule because the dedup key is also a durable workflow instance ID.

### ADV5-12 — high — The release contract requires one digest set and simultaneously provides no digest set from which any consumer can resolve

**Units.** Kubernetes/Production lane and Aspire/integration lane.

AD-19 L192 states that `Hexalith.Memories.Aspire` owns the qualified image digest set, every consumer resolves from it, and the Stack table records each digest beside its tag. The Stack table L249-L263 records no actual digest. Ledger L405 confirms that Kubernetes pins one set while integration uses a different Redis repository/tag/digest and that no consumer shares an owner-provided set.

- The Production lane treats the existing Kubernetes digests as the seed of the owner set.
- The integration lane treats its existing tested digests as the qualified seed and expects deployment to move.

Both are plausible attempts to instantiate the missing owner, but the engine repository itself differs (`redis-stack` versus `redis-stack-server`). Until one is selected, neither lane can produce the source/package and integration/Production correspondence AD-19 says qualification requires, and evidence from one lane cannot establish behavior in the other.

**Required correction.** Record the actual digest set in one tracked owner artifact now, point the Stack table to exact values, and make all consumers mechanically resolve it. Qualification must rerun after convergence; existing integration evidence is not transferable.

### ADV5-13 — medium — Access telemetry requires a principal while its post-erasure retention premise does not make principals content-free

**Units.** Authentication/provenance unit and access-telemetry writer/retention unit.

AD-13 L156 binds human actor provenance to normalized issuer plus subject. AD-17 L180 requires retained telemetry records to carry a principal while declaring every retained value content-free. AD-23 makes principal identifiers safe only when composed into keys by encoding or hashing them; it does not say the raw issuer/subject is content-free, nor does AD-16 enumerate a principal pseudonymization mapping for purge. OIDC subjects may be opaque, but the architecture does not require that and may receive an email or other descriptive stable subject.

- Telemetry A preserves issuer-plus-subject exactly, satisfying cross-surface provenance but retaining personal/descriptive data after tenant erasure.
- Telemetry B hashes or tokenizes the principal, satisfying the content-free posture but creating a new mapping/rotation/collision contract that no decision owns; alternatively it drops the record under AD-17's “not written” clause.

**Required correction.** Define the telemetry principal representation separately from domain provenance: keyed, stable for the retention window, non-reversible after its mapping/key is purged, and explicitly owned and erased. State whether a dropped record affects qualification evidence.

### ADV5-14 — medium — The same capacity event can be either a rejected query or a successful `truncated` query

**Units.** Admission/backpressure unit and retrieval adapter/axis-state unit.

AD-18 L186 says backpressure rejects or delays and may never change an axis's AD-22 state. AD-22 L210 says a provider cutoff or timeout after scope verification is `truncated`, and AD-10 returns it as a safe degraded result. The spine does not define the admission point or distinguish provider throttling caused by shared backpressure from ordinary provider cutoff.

- Build A detects the saturated provider pool before fan-out and rejects with retry guidance.
- Build B admits the query, the same pool cutoff occurs during fan-out, and the adapter returns `truncated` with hits.

Both satisfy the current temporal wording; users and G1 see different contracts under the same load.

**Required correction.** Define admission as the only point where capacity may reject/delay. Once admitted, every limit or contention cutoff must be represented through AD-22 and never silently shrink work; AD-18 should forbid state mutation without adapter evidence, not forbid a truthful `truncated` state.

## Gate disposition

The pass-4 correction round materially improved convergence. The core projection identity now works across active and rebuild epochs, zero-hit axis normalization has one meaning, and the four ordinary platform-partition writers are no longer mutually excluded. Those closures should be retained.

The spine is not ready to finalize because:

1. **Erasure is not closed under concurrency** (ADV5-01), so completion evidence can be true at the sampled instant and false immediately afterwards.
2. **The pub/sub authority contract directly contradicts itself** (ADV5-02), preserving the only current cross-tenant routing ambiguity.
3. **The register's exceptional mutation and recovery semantics remain incomplete** (ADV5-03/04/07), so both poison recovery and stale-register recovery diverge.
4. **Deletion, canonical evidence, and release evidence still claim properties their own stores/contracts do not supply** (ADV5-05/09/12).

The next correction should apply one class sweep to all mutation protocols: for every authoritative or append-only state, enumerate normal writers, exceptional writers, ordering/fencing, crash recovery, and the evidence that proves continuity. A second sweep should replace every “single artifact will define it” placeholder with either the actual seed needed by independent builders or a Deferred row that blocks the dependent evidence claim.
