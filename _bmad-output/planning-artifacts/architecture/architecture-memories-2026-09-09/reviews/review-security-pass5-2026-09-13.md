# Architecture Spine — Security, Tenant Isolation & Data Integrity — Pass 5

**VERDICT: FAIL — 2 critical, 5 high, 5 medium, 3 low architecture findings.**

The correction round improved the spine materially: the projection document identity now includes generation and epoch; the denominator rule is correct in AD-9 and AD-22; the pub/sub target rule now explicitly rejects publisher-selected tenant scope; platform-partition writes are divided by record type; issued identifiers are a declared target with the brownfield mismatch ledgered; and the six newly verified implementation mismatches are recorded honestly. Those changes do not yet converge. Two architecture defects still permit a fully rule-conforming implementation to defeat FR39/NFR16: an erased tenant can become issuable after register rollback or reversal, and AD-16's deliberately closed purge list still excludes tenant-keyed coordination state (and does not resolve operational backups). Five additional high findings leave tenant routing/privileged authority, lifecycle work, export registration, and concurrent migration behavior divergent.

This verdict is about the architecture rules, not the 50 disclosed implementation blockers. The code sweep was used to distinguish the two: where the spine states one coherent target and the repository has not reached it, this review records an **alignment gap** and does not count a new architecture finding. Where two independently built units can obey different sentences and produce incompatible security or data-integrity behavior, this review records an **architecture defect**.

## Scope and method

- Read the complete current `ARCHITECTURE-SPINE.md` (428 lines), its complete `.memlog.md`, and pass 4's security/data-integrity review.
- Re-tested SEC4-01 through SEC4-14 against the current bytes, then applied the complement test to the correction text.
- Traced the material claims into the current repository, especially tenant routing, lifecycle initiation, tenant-id acceptance, duplicate suppression, export/import, restore, Evidence Packet encoding, and tenant deletion.
- Severity follows the established gate: **critical** defeats an MVP hard gate (NFR8 or FR39/NFR16) in a fully compliant implementation; **high** permits cross-tenant write/disclosure, content survival, unauthorized privileged action, or resurrection/loss; **medium** leaves an integrity/security mechanism materially divergent; **low** weakens verification or controlled disclosure.
- No spine, memlog, source, configuration, or submodule was edited. This review file is the only write.

## Pass-4 re-test

**Result: 2 CLOSED · 3 PARTIAL · 9 NOT CLOSED · 0 REGRESSED.**

| ID | Status | Current result |
| --- | --- | --- |
| SEC4-01 | **PARTIAL** | AD-5's operative pub/sub clause now correctly binds tenant authority to authenticated channel identity and explicitly forbids `source`; however the same Rule still defines the operator artifact's third authorization fact as a CloudEvent-`source`-prefix-to-tenant map. The contradiction remains security-significant (SEC5-03). |
| SEC4-02 | **PARTIAL** | AD-21 now assigns ordinary record writers by record type. It still requires every write to use an operator-scoped principal, while AD-5 refuses every non-exempt operator action; export-index writes and tombstone reversal are not admitted exemptions. The initial four-content inventory also omits the purge-completion record later assigned a writer (SEC5-04). |
| SEC4-03 | **NOT CLOSED** | A ledger row was added, but AD-16's Rule still calls a shorter purge enumeration closed. A ledger obligation to amend the Rule is not the amendment itself (SEC5-02). |
| SEC4-04 | **CLOSED (architecture)** | AD-23 now makes platform issuance and non-descriptive identifiers binding and explicitly calls caller-selected tenant IDs a brownfield mismatch. The current `TenantProvisioningInput` remains an accurately disclosed alignment gap, not a rule defect. |
| SEC4-05 | **NOT CLOSED** | The receiver still verifies only that an exempt call names one tenant, not that it equals the tenant pinned in the workflow's lifecycle evidence. Grant amendment remains unconstrained to app IDs declared for that tenant, while the artifact is also said never to grant tenant scope (SEC5-05). |
| SEC4-06 | **NOT CLOSED** | Register rollback still compares the restored register only with facts it can prove from itself; provisioning has no external high-water comparand (SEC5-01). |
| SEC4-07 | **NOT CLOSED** | Reads remain admitted at partition/role granularity despite records of different sensitivity, and the bundle index still retains a bare digest of tenant content forever (SEC5-07). |
| SEC4-08 | **NOT CLOSED** | AD-23 still has no reserved-name set excluding `memories-platform`, the other platform partitions, principal prefixes, and platform-owned resource names from tenant issuance (SEC5-12). |
| SEC4-09 | **NOT CLOSED** | The grammar artifact is still not named, the delimiter value is still outside AD-23, and AD-4's bounded CloudEvent `source`/`id` production has no owner or declaration (SEC5-11). |
| SEC4-10 | **NOT CLOSED** | Adapter self-attestation remains the only input to the safety decision; AD-7 does not assign an independent, named enforcement point that invalidates an axis when returned records contradict the attestation (SEC5-09). |
| SEC4-11 | **NOT CLOSED** | AD-6 still enumerates states without an exhaustive transition relation. The ledger still asks implementation to "declare" architecture that the Rule does not contain (SEC5-06). |
| SEC4-12 | **CLOSED** | AD-23 now limits its statement to injective encoding of the value the shipped key hashes and immediately discloses that the value is `id`, not `source`, with the identity defect ledgered. Prefix aliasing remains an AD-8 implementation blocker, not a defect in AD-23's composition rule. |
| SEC4-13 | **NOT CLOSED** | The isolation method still names operator-principal negative cases but not authenticated pub/sub publisher cases (wrong-tenant envelope, unmapped channel, inactive tenant) (SEC5-14). |
| SEC4-14 | **PARTIAL** | Per-class grammar fixes the erroneous demand to lowercase shipped ULIDs. Re-issuing a colliding legacy tenant identifier still contradicts stable identity without a mandatory old-to-new mapping and rewrite/retention contract (SEC5-13). |

The carried SEC2-17 residue is also still open: AD-5 and AD-15 require revocation "within a stated bound," but neither states the value nor names its authoritative configuration/evidence home. The ledger accurately says implementation must state it; that means the spine has not fixed the convergence value (SEC5-08).

## Critical findings

### SEC5-01 — critical — Register rollback or reversal can make a completed erasure issuable again

**Rules:** AD-6 `:114`; AD-16 `:174`; AD-21 `:204`.

AD-6 says `Erased` is terminal and irrevocable and is set from AD-21's register. AD-16 requires permanent tenant-ID non-reuse. AD-21 simultaneously defines effective erasure as the latest tombstone/reversal record and says a reversal makes the identifier not erased, explicitly restoring issuability. It does not restrict reversal to a demonstrably spurious tombstone written before erasure completion, and its writer is not coherently authorized. A compliant operator procedure can therefore reverse a genuine completed erasure and provision the identifier again.

The rollback path independently reaches the same result. A restored register "resumes at the highest sequence it can prove" and is unavailable only when it is older than the newest erasure it records. A copy rolled back from sequence 9 to 5 cannot observe tombstones 6–9; its initialized marker reads back, its own highest provable sequence is 5, and provisioning has no high-water mark outside the restored partition against which to reject it. The new tenant can then inherit names, ACL patterns, key families, graphs, or coordination artifacts left by the erased tenant.

This is not an implementation gap: both unsafe paths are permitted by the Rule. Convergence requires (1) reversal to be legal only for a tombstone proven not to reference completion evidence and never to undo a completed `Erased` state, and (2) a restored/asynchronous register to remain unavailable until reconciled against an external durable high-water witness or a synchronous replica whose acknowledgement was part of every register write.

### SEC5-02 — critical — The closed erasure enumeration still excludes state that the architecture itself inventories

**Rules:** AD-8 `:126`; AD-16 `:174`; Direct Redis Exception Registry `:241-243`; ledger `:362`, `:397`, `:404`.

AD-16 says every store containing tenant content or content-derived material is purged and then deliberately closes the enumeration over product projections, the tenant content store, workflow/activity history, actor state, caches, derived artifacts, and application export bundles. The same spine inventories permanent `dedup:` records, failed-unit registries, import leases, derived-store fences, and migration state keyed by tenant; none is in the closed list. The correction adds row `:404`, but that row says the Rule must later be amended. It cannot make an omitted target part of a list the Rule calls closed.

Operational projection backups remain unresolved as well. AD-16 binds backups/restores and AD-2 blocks rehydration through the register, but a readable backup is itself a store of projection content. It is not explicitly in the purge list, has no adopted bounded-retention/encryption exception, and may remain readable forever even if restore admission is correctly refused.

A fully compliant erasure can therefore emit demonstrative completion evidence while durable tenant-correlated/content-derived state survives outside the demonstrated target set, defeating FR39/NFR16. Amend the Rule's enumeration (not only the ledger), and either enumerate operational backups or adopt an explicit encryption-and-retention exception whose key destruction makes their tenant payloads unreadable.

## High findings

### SEC5-03 — high — AD-5 still defines two incompatible pub/sub authorization inputs

**Rule:** AD-5 `:108`.

The operator artifact is first defined as holding a CloudEvent-`source`-prefix-to-tenant routing map. Later in the same Rule, `source` is correctly identified as publisher-set and any `source`-keyed routing map is explicitly forbidden; the accepted alternative is authenticated channel identity `(subscriber component, topic, authenticated publisher)`. Two teams implementing different halves are both quoting AD-5, and one lets a publisher select a tenant by changing an envelope field.

The repository confirms why this is not theoretical: `TenantEventRoutingOptions.cs:19-21` and `TenantEventRouter.cs:67,278-289` route by case-insensitive `source` prefix. Row `:386` correctly calls that code an alignment gap, but the architecture cannot converge the gap while its artifact definition still mandates the unsafe key. Replace the artifact's first enumeration with the authenticated-channel key and remove prefix/longest-match language unless the channel key itself has a separately defined hierarchical grammar.

### SEC5-04 — high — Platform-partition bootstrap and export registration have no admissible authority path

**Rules:** AD-5 `:108`; AD-16 `:174`; AD-21 `:204`.

AD-5 closes privileged work to exactly three exemptions and an exhaustive lifecycle-operation list. AD-21's platform bootstrap acts before any tenant exists, so it cannot satisfy the ordinary checks for an explicit tenant grant and `Active` tenant, but platform bootstrap/marker initialization is not an exemption or enumerated operation. The authority store that all provisioning and restore admission depend on therefore has no conforming creator.

The corrected per-record writer table has the same mismatch for exports. AD-21 requires every platform-partition write to come from an operator-scoped principal and assigns bundle-index records to the export operation. AD-5 requires a claimless operator principal to be refused for every non-exempt tenant operation, and export is not exempt. Either a normal tenant export cannot register its bundle, or it is given operator authority that AD-5 forbids. Tombstone reversal is likewise neither an AD-5 operation nor an AD-21 record-type writer.

Define platform bootstrap as a narrowly scoped deployment exemption with no tenant-data access; let the export-registration path write only its tenant-authorized record type without acquiring tombstone authority; and explicitly authorize or remove the correction procedure. Record-type enforcement must use distinct capabilities, not a shared operator credential.

### SEC5-05 — high — Lifecycle exemption scope is asserted by the caller, and the operation list omits lifecycle work AD-6 requires

**Rules:** AD-5 `:108`; AD-6 `:112-114`.

An exempt workflow pins one tenant in lifecycle evidence, but the receiver checks only that the call names a single tenant; it is not required to verify equality with the tenant recorded for that workflow instance. The NFR8 extension checks non-exempt operations and erased tenants, not an enumerated operation aimed at a different live tenant. A compromised or buggy lifecycle workflow can therefore invoke its privileged operation against another live tenant without violating the receiver rule.

The exhaustive operation list also omits tenant deletion as named by AD-6 and tenant-content-store provisioning/erasure. Reading "AD-16 erasure" as all deletion conflicts with AD-5's deliberate distinction between AD-6 lifecycle workflows and AD-16 erasure. A literal implementation moves the tenant to `Deleting`, then fails the next activity's ordinary `Active` check and cannot use lifecycle repair because AD-6 refuses repair in `Deleting`.

Finally, the grant definition says provisioning materializes app IDs the operator artifact declares "for that tenant" while the same paragraph says the artifact never grants tenant scope; live amendment does not constrain additions to any declared set. Resolve that ownership contradiction, require a verifiable workflow-instance scope reference at every receiver, and make the lifecycle list exhaustive against AD-6's Binds.

### SEC5-06 — high — Concurrent deletion during a rebuild can be resurrected by epoch activation

**Rules:** AD-3 `:96`; AD-14 `:162`.

The correction gives the Phase-1 rebuild epoch a valid document identity and lets ordinary ingestion continue only under the active epoch until activation. That creates an uncovered complement: a delete committed after a unit was copied into the rebuild epoch writes a fenced tombstone under the ordinary active pair, while the copied document remains live in the declared rebuild pair. Neither Rule requires the rebuild to consume a tail through a fixed EventStore high-water mark, replicate every concurrent tombstone into the rebuild pair, or prove source-version parity immediately before activation. Atomic activation can therefore make the deleted document queryable again.

The cleanup owner is also circular: AD-3 calls it "AD-14's retire step" while AD-14 calls it "AD-3's retire step." Assign one workflow as owner and require create/backfill/catch-up/verify/switch/retire, with verification covering every authoritative source version and deletion through a recorded high-water mark. Activation must fail closed if the tail is not caught up.

### SEC5-07 — high — Export erasure control conflicts with portability and retains a permanent content oracle

**Rules:** AD-16 `:174`; AD-21 `:204`; FR71 (`prd.md:1104`).

AD-16 says the payload is wrapped under a per-bundle key escrowed under the tenant key and, in the same sentence, says FR71 portability survives because the bundle format is unchanged. Ciphertext wrapping changes the artifact a portable JSON consumer receives unless a versioned transport envelope and key-transfer contract are defined; escrow only under the source deployment's tenant key also makes the file non-portable to an independent deployment. A streaming response further needs a fail-closed ordering contract for final digest/key escrow/index append versus release of the last bytes, which the Rule does not supply.

The index stores a bare content digest forever on a never-shredded, append-only partition. That digest is content-derived material under AD-16's own scope and is a confirmation oracle for anyone who can read the index and possesses a candidate export. Read admission remains broad (`enumerated consult paths and operator principals`) rather than per record class. Use an authenticated/keyed digest that becomes unverifiable after tenant-key destruction, define record-class readers, and specify a portable encrypted envelope plus atomic/fail-closed issuance-registration order.

## Medium and low findings

### SEC5-08 — medium — Revocation has no convergence value

AD-5 and AD-15 both defer to "a stated bound" without stating it or naming its authoritative validated-configuration location. A one-second bound and a one-year bound both conform. Since it covers cached sessions, connections, and resumed durable work under withdrawn operator authority, this is architecture, not merely implementation. Route one numeric/parametric bound and its evidence to a named configuration contract.

### SEC5-09 — medium — Safety still trusts the adapter that enforces isolation

AD-10/AD-22 make the adapter statement the only evidence used to admit an axis. AD-7 says scope must be enforced but does not assign an independent server check that can contradict a false `scopeApplied=true`. This is especially weak while the ledger acknowledges shared backend credentials. Require server-side verification of tenant/case/generation on every returned item; a mismatch must discard the item and null the axis regardless of adapter attestation.

### SEC5-10 — medium — Axis denominator and truncation are still contradictory across Rules

AD-9 and AD-22 now correctly say denominator weight is included iff the axis returned a result. AD-10 still says a scope-verified truncated axis "contributes denominator weight" without the zero-hit qualifier. AD-22 also defines truncation as failure to complete within a configured "depth" limit and later says reaching configured candidate depth is not truncation. These readings produce incompatible scores and health states. Make AD-10 cite the AD-22 result-dependent rule without restating it, and distinguish traversal/cutoff limits from the candidate-depth contract by name.

### SEC5-11 — medium — Identifier and CloudEvent validation artifacts remain unnamed

AD-23 promises a tracked artifact "referenced by name" but supplies no path/name and no delimiter value; the only concrete `:` appears in the Redis registry. AD-4's source/id bounded production is separately unnamed, while AD-23 explicitly says those fields are outside the issuance grammar. This lets teams adopt incompatible canonicalization, delimiter, and length contracts. Name the artifact, reserve the delimiter(s) by target naming scheme, and include CloudEvent productions and bounds in it.

### SEC5-12 — medium — Platform-owned names remain issuable

The grammar constrains alphabets but reserves no platform names. `memories-platform`, `memories-tenants`, `memories-cases`, and `memories-memory-units` all satisfy the target tenant grammar; the current `TenantIdGuard` does not reserve them. Platform issuance rather than caller choice reduces intent, not collision possibility. The grammar artifact must own a tested reserved-name/prefix set for every composed resource space.

### SEC5-13 — low — Legacy re-issuance has no referential-integrity migration

AD-23 requires colliding legacy tenants to be re-issued, while AD-13 requires stable opaque identities. No rule mandates an old-to-new map or rewriting EventStore references, ACLs, keys, graph names, provenance, telemetry, exports, and register records. Define migration and mapping retention before re-issuance can be implemented safely.

### SEC5-14 — low — NFR8 verification omits pub/sub principals and scoped exempt calls

Add negative tests for an authenticated publisher delivering an envelope naming tenant B over a channel bound to A, an unmapped channel, an inactive tenant, and an exempt lifecycle call naming a tenant different from the workflow evidence. Current row `:388` covers neither class.

### SEC5-15 — low — Erasure refusal remains distinguishable at provisioning

The Conventions make authorization/existence failures indistinguishable only for scoped resources. Provisioning refusal for a permanently retained erased identifier is operator-visible and can disclose historical tenant existence indefinitely. If this disclosure is intentional operator evidence, say so explicitly and scope the reader; otherwise use the same indistinguishability contract.

## Implementation alignment — confirmed, not counted as architecture findings

The following current-code mismatches are already represented in the ledger and should not be mistaken for new rule defects:

- `TenantEventRoutingOptions.cs:19-21` and `TenantEventRouter.cs:67,278-289` use publisher-controlled, case-insensitive `source` prefixes. `RoutedTenantProvisioningStartupService.cs:98-117` schedules the lifecycle workflow but carries no operator authorization. Row `:386` accurately records both facts; `AutoProvisionRoutedTenants` does **not** bypass the lifecycle workflow itself.
- `EventIngestionService.cs:144` passes `envelope.Id` to `EventStoreDedupKey.Build`; `EventStoreDedupKey.cs:16-20` hashes that value. The shipped identity omits `source`, as rows `:400` and the Direct Redis registry now say. AD-4 `:102` still contains the stale phrase "already satisfies by hashing `source`"; correct that factual sentence while retaining the implementation blocker.
- `TenantProvisioningInput.cs:9` accepts a caller-provided tenant ID, and the active guards admit mixed case/descriptive identifiers. Row `:401` accurately captures the gap from AD-23.
- No `memories-platform` source implementation, initialized marker, bundle index, erasure register, or register consult exists. Rows `:377`, `:384`, and `:389-390` accurately block qualification.
- Current export is streaming plaintext JSON (`ExportEndpoints.cs:111-118,167-174`; `ExportWriter.cs:36-52`) and the restore path validates same-tenant targeting but no register/index/key escrow. Row `:389` accurately records the target gap.
- The current Evidence Packet uses `AxesUsed`/`UnavailableAxes`, and null omission remains enabled. Rows `:402-403` accurately capture implementation misalignment; SEC5-10 is separate because the target Rules themselves still conflict.

## Gate conclusion

Pass 5 does **not** satisfy the Reviewer Gate. The ledger is more honest than in pass 4, but a ledger entry is not a substitute for correcting a contradictory or incomplete invariant. Before another pass, fix SEC5-01 and SEC5-02 first, then make AD-5/AD-21's privilege model executable (SEC5-03 through SEC5-05), add the rebuild catch-up/deletion invariant (SEC5-06), and reconcile export erasure with portability (SEC5-07). The remaining medium/low findings can then be resolved as one consistency sweep over authority bounds, state encodings, grammar artifacts, and evidence cases.
