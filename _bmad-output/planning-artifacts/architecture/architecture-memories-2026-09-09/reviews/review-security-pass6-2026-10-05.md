# Architecture Spine — Security, Tenant Isolation and Data Integrity — Pass 6

**VERDICT: PASS WITH FINDINGS — zero critical, zero high, one medium, two low (final 2026-10-05 retest).** The pass-five critical erasure paths are closed in the current spine. The register tombstone is terminal, continuity failure closes admission, the purge set includes coordination state and all projection epochs, and the operational-backup exception requires tenant-key unreadability. The October export revision also makes export release intent and deletion serialize through the authoritative tenant-lifecycle stream and recognizes that a failed transfer can leave an external prefix. AD-23 now reserves platform-owned names and prefixes from issuance and import; the Phase 1 rebuild catch-up rule and exhaustive lifecycle transition relation were also added during this review. The now-normative Identifier Grammar V1 closes that earlier handoff gap; AD-5 and AD-15 now share a 60-second revocation bound in the operator artifact.

## Scope

Reviewed the current `ARCHITECTURE-SPINE.md` and `.memlog.md` on 2026-10-05, including the in-turn corrections to AD-16 export release, AD-14 rebuild cutover, AD-6 legal transitions, and AD-21 bootstrap authority and combined erasure completion/tombstone authority, and re-tested the fifteen findings in `review-security-pass5-2026-09-13.md`. Source-code mismatches already recorded in the Current Alignment Gaps ledger are implementation blockers, not counted again as architecture findings. No spine, memlog, source, configuration, or submodule was edited by this review.

## Pass-five retest

| Finding | Result in current Rule |
| --- | --- |
| SEC5-01 register rollback/reversal | **Closed.** AD-21 requires a proven live lineage and makes any tombstone irreversible; `Erased` is derived from it. |
| SEC5-02 closed erasure set | **Closed.** AD-16 covers active, staging, retired, Dapr and direct-Redis coordination records and gives backups one verified tenant-key exception. |
| SEC5-03 pub/sub source routing | **Closed.** AD-5 keys the operator map by authenticated channel tuple and expressly forbids `source` routing. The shipped map remains ledgered implementation work. |
| SEC5-04 platform authority | **Closed.** AD-21 now assigns genesis creation only to an AD-15-scoped deployment-bootstrap identity, and export registration to its tenant-authorized record-class writer. |
| SEC5-05 lifecycle exemption | **Closed.** AD-5 binds the receiver to authenticated workflow-instance evidence and lists deletion, content-store work and grant amendment. |
| SEC5-06 concurrent rebuild/deletion | **Closed in-turn.** AD-14 now requires authoritative-tail catch-up, tombstone/source-version parity, a final command fence and an activation high-water check. |
| SEC5-07 export portability/oracle | **Closed.** AD-16 uses portable custody transfer, excludes bare digest and escrow from the permanent index, registers before release, and makes the first-byte race part of the lifecycle CAS. |
| SEC5-08 revocation bound | **Closed in final retest.** AD-5 fixes `revocationMaxAgeSeconds = 60` in the versioned operator artifact; AD-15 uses the same limit. |
| SEC5-09 independent result-scope check | **Open.** AD-10/22 still use adapter self-report as the server's only safety evidence (SEC6-06). |
| SEC5-10 denominator/truncation | **Closed.** AD-9/10/22 consistently make returned results determine denominator weight and distinguish candidate depth from incomplete work. |
| SEC5-11 grammar/CloudEvent artifacts | **Closed in final retest.** Identifier Grammar V1 fixes issued forms, delimiter, external-field bounds, key-family codec registration, and validate-on-read. |
| SEC5-12 reserved platform names | **Closed in retest.** AD-23:242 rejects the platform-owned names and prefixes at issuance, validate-on-read, and re-import, before the final grammar exists. |
| SEC5-13 legacy re-issuance mapping | **Open.** Re-issuance has no referential-integrity migration (SEC6-10). |
| SEC5-14 isolation negative tests | **Partial.** Operator-principal tests are required; publisher and wrong-workflow-scope cases are not enumerated (SEC6-11). |
| SEC5-15 erased-identifier disclosure | **Open.** The intended visibility of provisioning refusal is not declared (low). |

## In-turn closures

AD-14 now owns create/backfill/catch-up/verify/activate/retire and checks tombstone and source-version parity through an activation high-water mark. AD-6 now closes the lifecycle transition graph: `Deleting` never returns to an authorizing state, and retries preserve the fence generation. These in-turn amendments close the original SEC6-01 and SEC6-02 risks.

## Closed high finding — dated retest 2026-10-05

### SEC6-03 — Platform-owned tenant names now reserved

**Closed.** AD-23:242 rejects IDs equal to `memories-platform`, `memories-tenants`, `memories-cases`, and `memories-memory-units`, or under the `memories-`, `system-`, `operator-`, `dapr-`, and `kube-` prefixes, at issuance and validate-on-read. New platform-owned names must enter this set before use. The architecture no longer permits a compliant issuer to allocate the authority partition to a tenant. The rule is still unimplemented, as the ledger and missing exact grammar make clear.

## Medium findings and closures

### SEC6-05 — Authorization revocation bound closed in final retest

**Closed.** AD-5:119 fixes `revocationMaxAgeSeconds = 60` in the versioned operator artifact and makes freshness failure fail closed; AD-15:185 applies the same 60-second limit to provider credentials and superseded clients. Changing it requires an architecture decision. Implementation evidence remains owed.

### SEC6-06 — Adapter attestation is the sole server-side isolation evidence

AD-10 and AD-22 make an adapter's own `scopeApplied` and active-generation statement the only input to axis safety. A faulty adapter returning a cross-tenant record while claiming scope applied is not caught at the server boundary. This is defense in depth, not a fully compliant breach because the adapter would itself violate AD-10. Require the server to verify authoritative tenant, case, and generation on each returned result and null the axis on mismatch.

### SEC6-07 — Identity productions closed in final retest

**Closed.** AD-23 now names Identifier Grammar V1 in the spine as normative. It specifies 20-character lowercase tenant IDs, canonical uppercase ULIDs, the lowercase `u` delimiter, per-family arity and immutable codec registration, destination limits and conformance tests, and CloudEvent `source`/`id` byte bounds. Issuance, validation, key builders, and legacy migration remain implementation ledger blockers, not an unresolved architecture grammar.

AD-16 now puts completion evidence and the irreversible tombstone into one idempotent register-stream event, conditionally appended under its revision CAS after rechecking the terminal `Deleting` generation. It also names live EventStore payload as a sampled cryptographic-unreadability target. These in-turn amendments close the original SEC6-08 and SEC6-09 concerns.

## Low findings

### SEC6-10 — Legacy tenant re-issuance lacks a reference migration

AD-23 requires colliding legacy tenant identifiers to be re-issued without specifying rewrites or permanent mapping of EventStore references, grants, keys, projections, telemetry, bundles, and tombstones. Define the migration before using re-issuance as a remediation.

### SEC6-11 — Negative isolation evidence remains incomplete

AD-5 requires operator-principal negatives but does not specifically bind tests for authenticated pub/sub wrong-tenant envelopes, unmapped channels, inactive tenants, or an exempt call whose request tenant differs from its workflow evidence. Add those to the NFR8 isolation suite. Provisioning refusal for a tombstoned ID is also operator-visible; declare that as intentional scoped disclosure or make it indistinguishable from other refused identifiers.

## Implementation alignment, not counted above

The current-code gaps disclosed in the ledger remain blockers: caller-selected mixed-case tenant IDs, source-keyed pub/sub routing, absent per-tenant backend principals, absent EventStore-backed register and generation fence, incomplete erasure/backup verification, missing export staging/index/lease/signature, and incomplete purge of direct-Redis coordination state. AD-20 correctly gives none of these gate credit from a date or risk acceptance. The PRD's G6 exception wording still conflicts with AD-20's evidence-only rule; the user has chosen to align the PRD in the planning correction, so this is an upstream edit owed rather than a new spine security finding.

## Intermediate retest — 2026-10-05 (superseded below)

The current spine closes the high platform-name collision in AD-23:242. AD-6:123 serializes export release and `Deleting`, requires a stopped sender before cancel is terminal, and makes `Deleting` non-returning. AD-16:187 and AD-21:228 combine completion evidence with the irreversible tombstone in one register-stream event. AD-14:173 prevents epoch activation before authoritative deletion parity. No new fully compliant NFR8 or FR39/NFR16 breach was found in these amendments. **At this intermediate retest: zero critical, zero high, three medium, two low; see the final review below.**

**Handoff status at this intermediate retest:** SEC6-07 was still open. The final review below supersedes that assessment after Identifier Grammar V1 was adopted. Implementation ledger rows remain open until current rerunnable evidence closes them.

## Final delta retest — 2026-10-05, identifier collision and read egress

**No new critical or high contradiction.** AD-23:244 now permits a non-grammar component in a composed key only through reversible encoding or a tenant-keyed digest reservation that compares canonical values and rejects a collision before key use. AD-16:195 explicitly includes that reservation in its tenant-erasure purge and verification set. The Direct Redis Exception Registry initially described the shipped bare hash as conforming; its current row and the dedup ledger now correctly identify the missing collision reservation and omitted CloudEvent `source` as implementation gaps.

AD-6:124,126 now makes the committed tenant stream the `Active` authorization source, acquires a serialized permit before each tenant-content read, and keeps it through the last response byte. The transition to `Deleting` closes the gate and waits for completion or confirmed cessation of egress; gate loss fails closed and cannot itself prove drainage. AD-16:195 includes gate state and permits in the purge set. A compliant reader cannot continue emitting tenant content across a completed erasure transition under these rules.

This delta found **zero critical, zero high** at that time. The final review below supersedes its then-open grammar handoff finding.

## Final security and PRD alignment — 2026-10-05

**Final review verdict: zero critical, zero high; one medium and two low residuals.** Identifier Grammar V1 (AD-23:249–261) closes the prior SEC6-07 handoff gap. The tenant alphabet excludes delimiter `u`; IDs are 20 lowercase Crockford characters beginning with a letter, issued randomly with collision and tombstone checks; case and MemoryUnit IDs are canonical uppercase ULIDs. Platform names/prefixes remain reserved. Each key family fixes tag, arity, class order, destination and exact per-position codec before use. Digest collisions are rejected by a tenant-keyed canonical-value reservation, which AD-16 purges. The 42-byte affix budget around a 20-byte tenant ID fits the named 63-byte destinations, while longer/multipart names must pass the destination's actual limit without truncation. CloudEvent `source` and `id` now have ASCII syntax, byte limits, specified canonicalization, and pre-admission rejection. A short regex/length sanity check confirmed delimiter exclusion, platform-name rejection, ULID leading-digit bounds, and 20+42 < 63. Current code does not yet enforce these rules and remains blocker-ledgered.

PRD G6:192 and AD-20:225 both require current rerunnable evidence for MVP-active requirements and critical architecture gaps; risk acceptance gives no gate credit, and phase-inactive work earns none. PRD FR71:1107 asks for portable export, while AD-16:195 gives self-contained/recipient-encrypted custody transfer, source staging purge, and signed-origin rejection after erasure. These contracts are aligned. AD-16's live EventStore unreadability, closed purge set, release-intent ordering, and combined completion/tombstone event remain coherent with FR39/NFR16. No new fully compliant cross-tenant or erased-content resurrection path was found.

**Status-final assessment:** No security/isolation architecture blocker remains after the 60-second AD-5/AD-15 revocation decision. SEC6-06 (adapter self-attestation) is a medium defense-in-depth finding, not a fully compliant breach; SEC6-10/11 are low. The implementation ledger's open rows block product qualification under G6 but do not claim this document is unfinished.
