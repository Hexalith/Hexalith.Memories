# Adversarial Divergence — Pass 6, 2026-10-05

**Initial snapshot verdict (superseded by the dated retest below): REVISE — zero surviving critical pass-5 contradictions, but six high and two medium cross-unit seams remained.** The 2026-10-05 portable-export amendment closes the permanent content-digest and key-escrow contradiction and adds an in-flight release lease. Its first-byte ordering still needs a linearization rule before the erasure and export units can agree on whether a transfer may start after `Deleting` commits.

Target: `ARCHITECTURE-SPINE.md` (448 lines, status `draft`, AD-1…AD-23). Compared with `review-adversarial-pass5-2026-09-13.md` and the appended `.memlog.md` decisions through the portable-custody-transfer ratification. Read-only review of the spine and memlog; this file is the only artifact written.

## Retest of pass-5 findings

| Pass-5 item | Current result |
| --- | --- |
| ADV5-01 late projection write after erasure | **Closed at the rule level.** AD-3:101 and AD-16:187 require generation-bound atomic target writes or confirmed revocation, durable target acknowledgements, and generation-bound completion CAS. Implementation remains blocker-ledgered at 424. |
| ADV5-02 publisher-selected `source` routing | **Closed at the rule level.** AD-5:113 gives the operator artifact an authenticated channel-identity map and requires the same three admission checks. |
| ADV5-03 tombstone reversal | **Closed by terminal semantics.** AD-21:221 forbids every reversal and permanently retires the identifier. |
| ADV5-04 restored register counter without facts | **Closed at the rule level.** AD-21:217-221 uses one dedicated stream revision, proven live continuity, and indefinite fail-closed on lineage loss. |
| ADV5-05 permanent bare export digest | **Closed.** AD-16:183 and AD-21:217 forbid content-derived authenticators and source-key escrow in the index. |
| ADV5-06 lifecycle transition graph | **Open; ADV6-02.** AD-6:119 still gives names and writer but no legal transition graph. |
| ADV5-07 bootstrap wrong empty partition | **Open; ADV6-03.** AD-21:218 still says only “named platform-bootstrap step.” |
| ADV5-08 retire-step circular owner | **Open; ADV6-04.** AD-3:99 says AD-14's retire step; AD-14:167 says AD-3's. |
| ADV5-09 authorization-null ambiguity | **Closed.** AD-12:155 makes withheld and ordinary absence byte-identical canonical nulls. |
| ADV5-10 universal delimiter | **Partly corrected in interpretation, still unbound; ADV6-05.** An alphabetic reserved separator could satisfy several schemes if all component grammars exclude it. No actual delimiter or per-scheme encoding exists, so independent builders still cannot produce the same keys. |
| ADV5-11 CloudEvent shipped-identity claim | **Closed as a contradictory claim; residual ADV6-07.** AD-4:107, AD-23:233, and registry:258 all say the shipped key hashes `id` and omits `source`. The adopted target remains different and blocker-ledgered. |
| ADV5-12 qualified digest set | **Open; ADV6-06.** AD-19:205 requires one set while Stack:264-280 and ledger:422 confirm none. |
| ADV5-13 retained telemetry principal | **Open; ADV6-08.** AD-17:193 still requires a principal without a content-free representation. |
| ADV5-14 admission versus truncation | **Closed.** AD-18:199 places rejection before fan-out; AD-22:227 labels post-admission cutoffs. |

## Findings

### ADV6-01 — high — Export release and `Deleting` have no atomic winner

**Units:** FR71 export delivery and AD-16 erasure workflow. **Rule:** AD-16:183 requires an active-generation revalidation “at the first-byte boundary,” cancellation if `Deleting` won first, and a tenant-generation-bound release lease; `Deleting` fences new leases and waits for existing ones.

- Export A acquires the lease, reads `Active`, then stalls before writing the first byte. Erasure commits `Deleting` and fences new leases. Export A resumes and sends the first byte. Its lease was admitted under the old generation and erasure waits for it, but `Deleting` won before custody transfer began.
- Export B serializes a release-intent commit against the `Deleting` transition, so either release wins and its lease may complete, or deletion wins and export cancels. It never makes a read-then-network-write check the ordering authority.

Both follow the named lease and revalidation steps; the spine does not say what operation makes the check and `Deleting` mutually exclusive. The same interleaving is classified as a completed external transfer in A and a canceled source-held bundle in B. **Correction:** define a single atomic release-intent versus deletion transition in tenant lifecycle state. State whether a committed release intent may finish after `Deleting`, and make the erasure wait/cancel rule consume that committed state rather than an unobservable physical first-byte instant. Keep staging until transfer completion or cancellation is durably acknowledged.

### ADV6-02 — high — Lifecycle operations still choose incompatible legal transitions

**Units:** tenant repair workflow and authorization/lifecycle projection consumer. AD-6:119 names seven states and one writer; ledger:400 admits transitions are undeclared. Repair A commits `Failed → Active` after verifying resources. Repair B requires `Failed → Provisioning → Active`, and treats `CompensationFailed → Deleting` as the only cleanup path. Both commit transitions before touching resources and use only the adopted enum. They expose different `Active` intervals to AD-5 and disagree on which retry a workflow may issue. **Correction:** declare the legal transition graph and operation preconditions in AD-6, including deactivation, failure recovery, deletion, and terminal `Erased`; make the ledger restate it.

### ADV6-03 — high — Bootstrap can still initialize an incorrectly empty population

**Units:** platform deployment bootstrap and Server readiness. AD-21:218 bans ordinary Server startup from creating the marker, but the exclusive “named platform-bootstrap step” still has no component, principal, one-time ceremony, or previous-lineage evidence. Bootstrap A runs as a deployment init job against a mispointed fresh partition and writes a new genesis marker with the expected configured population ID. Bootstrap B requires an operator-authorized new-population ceremony and disjoint namespace proof, and refuses it. Both run before provisioning and are distinct from Server startup; only B prevents the wrong empty authority from passing readiness at 246. **Correction:** bind the one-time creator, authenticated principal, population-ID issuance, immutable lineage anchor, and disjoint-namespace check; ordinary deployment/restart paths must verify the prior marker and never create a new one.

### ADV6-04 — high — Superseded-epoch retirement has circular ownership

**Units:** projection coordinator and rebuild/migration workflow. AD-3:99 assigns deletion of old checkpoint records to “AD-14's retire step”; AD-14:167 assigns deletion of old documents and checkpoints to “AD-3's retire step.” The coordinator can wait for the migration workflow while the migration workflow waits for the coordinator. Both implement their cited ownership; neither has a committed retry/completion state. Old query-inactive but tenant-resident records can persist indefinitely. **Correction:** name one owning workflow, its trigger, resumable state and verification; define whether activation success precedes retirement, and make the other AD reference that owner unambiguously.

### ADV6-05 — high — Issuers and key composers lack the one actual grammar/delimiter contract

**Units:** tenant issuer/SQL or Kubernetes provisioner and Redis/Dapr key composer. AD-23:233 promises a single tracked grammar artifact “referenced by name from this decision,” but names no file and declares no actual delimiter or maximum lengths. Ledger:399 confirms it does not exist. Composer A can choose a lowercase alphabetic reserved separator valid in its schemes; composer B can choose a different one. Both can exclude their choice from issued component alphabets and satisfy the abstract rule, yet produce incompatible tenant resource names and dedup keys. The shipped `dedup:` family at 258 is a separate migration obligation. **Correction:** record exact per-class productions, maximum lengths, composition encoding and separator in a named tracked artifact, then cite it from AD-23 and test every listed scheme. Treat existing keys as legacy until migrated. The earlier pass's assertion that *no* universal character could ever work was too strong; the missing concrete contract is the surviving divergence.

### ADV6-06 — high — Release lanes still have no single qualified image identity

**Units:** Aspire/integration and Kubernetes/Production owners. AD-19:205 says all consumers resolve one digest set from `Hexalith.Memories.Aspire`, and Stack:264-280 should record each digest. Ledger:422 confirms there is no shared set and Redis harness and deployment even use different image repositories. The integration owner can qualify its existing `redis/redis-stack` image, while deployment can qualify `redis/redis-stack-server`; neither can inherit a digest from the absent owner artifact. Their tests and Production run different engines while each can claim to have “pinned” a digest. **Correction:** publish the actual owner-managed digest set and make every named consumer resolve it; rerun both lane integrations against those exact images.

### ADV6-07 — medium — CloudEvent source/id validation still has no bounded production

**Units:** external event ingress and durable dedup/workflow identity. AD-4:107 requires `source` and `id` to be validated against a “declared bounded character set” and length bound, yet declares neither; AD-23:233 assigns a per-class grammar only to issued identifiers, not these externally supplied fields. Ingress A accepts percent-encoded URI path characters and 4 KB IDs; ingress B rejects those characters and caps IDs at 256 bytes. Both can honestly say they declared a bounded set locally, but the same publisher delivery is accepted in one lane and rejected in another, and a rolling key migration lacks one shared field contract. **Correction:** reserve the exact field productions, byte-length maxima, canonicalization vectors, and composed key shape in the tracked identity artifact, with a migration rule for already durable keys.

### ADV6-08 — medium — Retained access telemetry has no content-free principal representation

**Units:** authenticated provenance and retained access telemetry. AD-13:161 preserves issuer-plus-subject for actor provenance; AD-17:193 requires a `principal` in retained telemetry while calling retained fields content-free. Issuer/subject can contain an email address. Writer A records it verbatim for cross-surface identity. Writer B pseudonymizes it and must choose an unowned salt/key, collision behavior and retention period; or it drops records under AD-17's content-free rule. Both follow one side of the contract but produce different post-erasure data and auditability. **Correction:** define a separate stable telemetry principal token, its issuer/subject mapping owner and key lifecycle, and how omitted records affect qualification evidence.

## Gate disposition

The spine remains `draft`. The pass-5 critical erasure and cross-tenant pub/sub defects are closed in the current rules, and the portable export no longer retains a content-derived digest or source-key escrow. Resolve ADV6-01 through ADV6-05 before independent implementers can rely on this as a convergent build substrate; the release and telemetry findings remain current qualification blockers under AD-20. PRD G6's phase-exception wording is a separately acknowledged upstream correction owed after the user's evidence-only decision, not an architectural alternative to AD-20.


## Dated retest — 2026-10-05, after AD-6/AD-14/AD-16/AD-17/AD-21 amendments

**Current verdict: REVISE — one high design divergence, no critical design divergence.** The earlier six-high/two-medium count is a historical snapshot. Its export, transition, bootstrap, retirement, and principal findings were corrected in the current 459-line spine. AD-23's absent grammar artifact and AD-19's absent digest set remain explicit implementation/qualification blockers, not contradictory design choices. The CloudEvent bounded-character-set gap remains a medium design incompleteness.

| Earlier finding | Retest against current rule | Classification |
| --- | --- | --- |
| ADV6-01 export versus `Deleting` | **Closed.** AD-6:123 and AD-16:189 serialize release-intent begin/end/cancel and `Deleting` in the same authoritative tenant stream; a canceled sender must stop or lose egress capability before cancellation is terminal. | Design closed; implementation owed in ledger:411,418. |
| ADV6-02 legal transitions | **Closed.** AD-6:123 states the allowed graph, operation preconditions, `Deleting` write generation, and terminal `Erased`. | Design closed; code enforcement owed in ledger:411. |
| ADV6-03 bootstrap | **Closed at the safety boundary.** AD-5:115 and AD-21:226 bind the dedicated bootstrap identity, first-ever grant, expected population ID, one-time consumption, verify-only startup, and fail-closed uncertainty. | Design closed; one-time procedure/continuity implementation owed in ledger:406. |
| ADV6-04 epoch retirement | **Closed.** AD-3:99 and AD-14:171-173 name the tenant migration workflow as owner of catch-up, activation and verified resumable retire. | Design closed; implementation owed in ledger:424. |
| ADV6-05 grammar and delimiter | **Still absent, but explicitly held.** AD-23 still requires a named tracked artifact and ledger:410 blocks evidence until it exists. This is an implementation/qualification gap; the spine does not authorize independent local choices as compliant. | Implementation blocker, not current contradiction. |
| ADV6-06 image digest set | **Still absent, but explicitly held.** AD-19 and ledger:433 require one owner-managed digest set and deny correspondence/evidence until it exists. | Implementation/qualification blocker, not current contradiction. |
| ADV6-07 external CloudEvent field production | **Open, medium.** AD-4:107 still leaves the bounded character set and maximum lengths of externally supplied `source` and `id` unnamed, and AD-23's issued-ID grammar does not define them. Independent ingress and dedup owners can accept different field domains. | Design incompleteness; declare one production and migration vectors. |
| ADV6-08 retained principal | **Closed.** AD-17:201 owns stable random tenant-scoped token issuance, mapping purge, omitted-record behavior and qualification evidence. | Design closed; implementation still owed. |

### ADV6-R1 — high — Stale lifecycle projection can authorize a tenant after `Deleting` commits

**Units:** AD-6 tenant lifecycle writer and AD-5 API/query authorization reader. AD-6:121 says authorization decisions may read the committed tenant state **or** its content-free projection on AD-21's separate platform partition. It does not require that mirror to be synchronous, version-matched, or consulted only after a freshness barrier. AD-5:113 requires the tenant to be *currently* `Active`; AD-16:193 closes ordinary admission when `Deleting` commits.

- Reader A checks the authoritative `memories-tenants` stream and rejects immediately after `Active → Deleting`.
- Reader B checks the AD-21 lifecycle projection, which still says `Active` because the mirror has not caught up, and authorizes a query whose derived-store reads do not pass through AD-3's write fence.

Both use an explicitly permitted AD-6 read path, but give opposite authorization for the same committed lifecycle state. AD-16's generation fence protects target writes and deletion completion; it does not prevent stale-projection read egress. **Correction:** make the committed tenant stream the authority for all `Active` decisions, or require a monotonic lifecycle-revision freshness barrier before a platform projection may answer authorization. The `Erased` state can continue to derive solely from the AD-21 tombstone after shredding.

No other high or critical design contradiction survived this retest. The current alignment ledger continues to block Production qualification independently of this design review; closing a reviewer finding does not supply AD-20's current rerunnable evidence.


## Dated retest — 2026-10-05, authoritative lifecycle read amendment

AD-6:121 now makes the AD-21 lifecycle mirror descriptive only and requires an authoritative `memories-tenants` state read for ingress and query egress. **ADV6-R1's stale-projection pair is closed.** A reader of the mirror no longer complies with the Rule when authorizing.

**Residual high design seam — read-to-egress race.** Query A reads authoritative `Active` at revision *r*. AD-6 then commits `Deleting` at *r+1*. Query A sends its first result byte after that commit. Query B uses a tenant read-intent lease and lets deletion wait, or cancels A before egress. Both use a current authoritative read when checked, but AD-6's phrase “every ... query egress ... reads ... at the current stream revision and requires `Active`” has no atomic check-to-network-write boundary. The risk is data egress during `Deleting`; AD-3/AD-16 write fences do not cover reads.

**Minimal correction:** choose the observable boundary the product actually requires. FR39/NFR16 forbid resurrection after **erasure completion**, not every already-authorized response after the `Deleting` transition. Let authorization linearize at authoritative-stream query admission; make `Deleting` fence new admissions; maintain bounded in-flight read/egress leases (or equivalent drain/cancel acknowledgements) and require them to finish or be revoked before purge and completion. If zero bytes after the `Deleting` commit is required instead, serialize a read-intent gate with that transition in the same authoritative stream and state its egress stop acknowledgement. A plain final lifecycle read is insufficient.


## Dated retest — 2026-10-05, serialized query-admission gate

**Verdict for the query race: CLOSED; no high or critical design residual found in this retest.** AD-6:121-123 now requires every content-emitting product reader to acquire the same serialized tenant gate before its authoritative `Active` read and to hold its permit through the last response byte. The lifecycle workflow durably closes that gate, obtains completion or stop-and-no-further-egress acknowledgement for every bounded response, and only then commits `Deleting`. An acquire cannot race past a serialized close; an earlier permit must be drained or canceled before deletion commits. Direct readers are expressly forbidden. AD-16:192 includes the gate and permits in erasure's purge-and-verification set.

**Loss/recovery test:** If Dapr gate state is lost or unavailable while a sender still holds a permit, the rule requires the sender to stop before emitting another byte when renewal fails, and explicitly forbids treating missing state as proof of drain. `Deleting` therefore remains uncommitted until the sender can be positively reconciled or stopped; reopening requires an authoritative `Active` read, unchanged lifecycle generation, and confirmed permit reconciliation. A conservative implementation may remain unavailable indefinitely after irrecoverable gate loss. That is a liveness/operational qualification risk, not an unsafe permission to resume egress or complete erasure. The review does not infer that a missing permit record means its sender stopped.

The AD-6 gate is target architecture, not shipped evidence. Its durable close, permit acquisition/renewal, sender stop acknowledgements, and lost-state recovery need implementation and failure-injection evidence before AD-20 qualification.


## Final consistency pass — 2026-10-05, Identifier Grammar V1 and PRD G6

**Verdict: REVISE — one high AD-23 build divergence, no critical contradiction.** This supersedes the earlier dated query-gate verdict for the full-spine finalization review. PRD G6:192 now agrees with AD-20:225: a current rerunnable evidence path is required and a risk acceptance or product/architecture exception supplies no gate credit. AD-6's serialized query permit still closes the read-to-egress race under its fail-closed loss rule. Missing image digests, legacy identifier migration, and unenforced isolation remain explicit implementation/qualification ledger blockers, not new architecture contradictions.

### ADV6-F1 — high — Identifier Grammar V1 permits two incompatible encodings for one key family

**Units:** ingress/workflow dedup key builder and import/replay key builder. The new normative grammar at AD-23:257 lets a non-issued component be either reversibly encoded into lowercase Crockford **or** represented by a 52-character SHA-256 digest with canonical-value collision reservation. A family registration fixes its tag, arity, component class order, and destination scheme, but does not fix each component's codec or the exact reversible byte encoding.

For a single registered family `tag u tenant u source u id`, Builder A reversibly encodes validated CloudEvent `source`; Builder B uses the permitted collision-reserved digest. Both follow the family tag and the permitted component rule, yet produce different durable keys and can each admit the same event as new work. Mixed-version replay or import can miss a previously accepted idempotency record. Separately, the reversible and digest outputs occupy the same lowercase Crockford alphabet with no mode discriminator, so the collision reservation only detects digest-versus-digest collisions, not a possible digest-versus-reversible alias.

**Correction:** make the family registration bind an immutable per-component codec and exact canonical byte encoding, version it with the tag, and give reversible and digest representations disjoint encodings or distinct family tags. Collision reservation still guards digest values within that fixed codec.

**Other retests:** AD-23:253-255 now fixes per-class tenant and ULID productions, `u` delimiter, and external CloudEvent bounds; the former absent-grammar finding is closed as a design omission. The active PRD and AD-20 G6 wording match. No other high or critical cross-unit inconsistency was identified in this final pass.


## Final AD-23 codec retest — 2026-10-05

**Verdict: PASS WITH FINDINGS — ADV6-F1 closed; no remaining high or critical design contradiction found.** Identifier Grammar V1:257 now binds one immutable codec and exact canonical-byte encoding to each component position of a versioned family. A codec or byte-encoding change requires a new family tag and migration. The rendered formula places `u` between every component, and the SHA-256/Crockford and reversible encodings have exact bit, length, and pad rules plus family golden vectors at :259. The two builders from ADV6-F1 can no longer choose different codecs under the same tag; different tags cannot alias. A digest reservation continues to reject a same-codec canonical-value collision before key use.

The current spine still carries explicit implementation and qualification blockers: issuers and key builders do not yet enforce the grammar, legacy identities need migration, image digests are not shared, and G6 requires current rerunnable evidence. Those are tracked work, not inconsistent architecture decisions. PRD G6 and AD-20 remain aligned, and AD-6's serialized query gate remains fail-closed. This review raises no further finalization blocker from the adversarial divergence lens.
