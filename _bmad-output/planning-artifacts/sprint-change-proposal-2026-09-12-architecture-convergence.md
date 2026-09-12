# Sprint Change Proposal — 2026-09-12 Architecture Convergence

**Date:** 2026-09-12
**Mode:** Batch
**Status:** Draft — approval and the human decisions in section 3 are required before any further spine edit
**Trigger:** Three reviewer-gate passes over the architecture spine on 2026-09-12. The first found 10 critical and 17 high findings; two amendment passes closed 24 of those 27 and corrected every factual error, but the security lens returned FAIL on all three passes and the third pass still recorded 9 NOT CLOSED, 14 PARTIAL, and 31 new findings across lenses. The residue is not editorial.
**Recommended path:** Freeze spine editing; resolve five structural decisions as scoped design work; then one final convergence pass and gate re-run.
**Change size:** Architecture-only and cross-cutting. This proposal changes no product requirement, no epic, and no tracking state.

## Non-negotiable boundaries

- Do not modify `_bmad-output/implementation-artifacts/sprint-status.yaml` in this correction.
- Do not create implementation story files, start implementation, update dependencies, stage, commit, push, or alter submodule pointers.
- Do not renumber, retire, or reuse any `AD` id. AD-1…AD-20 are stable and downstream may cite them.
- No further edit to `ARCHITECTURE-SPINE.md` until section 3's decisions are recorded. Two amendment passes have now each closed findings while introducing new ones; a third unguided pass is the failure mode this proposal exists to stop.
- Preserve every reviewer-gate artifact under `architecture-validation-2026-09-12/` and `architecture-memories-2026-09-09/reviews/` as evidence.

## 1. Authoritative baseline

Pinned intake hashes:

| Artifact | SHA-256 |
| :------- | :------ |
| `ARCHITECTURE-SPINE.md` (post-pass-3) | `f5516a701c438cad1224a53b21b90558dcb94004db2b2e50c0ac0fd200b5f79c` |
| Spine `.memlog.md` | `4ec80c2c957688ca85200a41b4ec30174b6932e39c1fa15209a710ea43965c89` |
| `prd.md` (post-correction) | `71de5deb478978e68c321325c37ab3e22cbba027d9ba68017bb79d31a28aeff4` |
| `addendum.md` | `009f7684b40e6a873056eeb3c35bca8e22112170019c24e75f9124d934ac1e01` |
| `sprint-change-proposal-2026-09-12.md` | `01a21f18c2033c7ca5f338731b9b637e93b638df07442a2c3adb68c09005e9c3` |

**Relationship to the 2026-09-12 Sprint-Readiness Recovery proposal.** That proposal's §5 asked architecture for three things. All three are now done: §5.1 phase-qualified AD-14 (ratified by Jerome, 2026-09-12), §5.2 extended the binding to FR75/NFR37/G6, §5.3 reconciled the alignment ledger. **This proposal is its successor, not its duplicate.** It covers only what the reviewer gate exposed that §5 could not have anticipated, because §5 was written before any gate ran.

## 2. Issue and impact summary

The spine improved materially: 324 → 369 lines, 19 → 20 ADs, ledger 15 → 29 rows, deterministic linter clean, every factual error corrected against source, PRD reconciled in three places and Open Questions 9 and 10 closed.

It has not converged. Two diagnoses from the gate itself, both accepted:

1. **Narrowing.** The adversarial lens found that the second amendment "scoped three of its four highest-value fixes to the exact case the prior finding exhibited, and the narrowing un-covered the general case." Scoping the `truncated` axis state to graph partitions relocated an identical composite-score divergence onto the syntactic axis. Fixes written against a reported instance do not close the class.
2. **Obligations without homes.** Each amendment added rules that named an owner, a store, or a state machine that does not exist. The spine now mandates an issuance grammar, a reserved delimiter, a tenant lifecycle state machine, an erasure-scoped store, and an erased-tenant register — five artifacts referenced as though real. Ledger rows were added for all five in pass 3, which makes them visible but does not decide them.

The impact is confined to architecture. No product requirement changed. But **G6 cannot pass**: AD-20 admits a current evidence path or an approved dated exception, and all 29 ledger rows (23 classified active-foundation critical) are in neither state.

## 3. Required human decisions

**Owner for all five: Jerome.** None can be closed by an editing pass.

### D1 — The erased-tenant register: owner, creation point, authorization, and store

**Why it blocks.** AD-16 made this register the sole authority for replay rejection, tenant-ID non-reuse, and restore admission. The gate then found: no AD owns the platform provisioning that must create it (ADV3-01, critical); nothing authorizes reads or writes of it, so one deleted tombstone silently re-enables reuse and one spurious tombstone is an irreversible tenant-creation denial of service (SEC3-05); its fail-closed clause names "deletion and every restore path" and omits tenant provisioning and replay, two of the three duties it is authoritative for (SEC3-04); and it is described in one paragraph as both "Dapr state under AD-8" and "domain evidence under AD-2", where the Dapr component in question is the Redis one AD-2's *Prevents* forbids as a system of record and Deferred calls unqualified (P3-03, ADV3-08).

**Choose one:**

| Choice | Consequence |
| :----- | :---------- |
| **`EVENTSTORE-REGISTER` (recommended)** — the register is a platform-tenant EventStore stream, created at platform bootstrap, never encrypted under any tenant key, with an explicit AD covering its authorization and its readers. | Resolves the AD-2 contradiction outright, inherits durability and replay semantics already adopted, and survives the Redis state-store qualification question in Deferred. Costs a new AD-21 and a bootstrap ordering rule. |
| `DAPR-REGISTER` — keep it in Dapr state, qualify the component, and amend AD-2's *Prevents* to carve out content-free platform evidence. | Cheaper text change, but weakens AD-2's central claim and inherits the unqualified-state-store risk the gate flagged twice. |

### D2 — Scope of AD-5's lifecycle exemption

**Why it blocks.** The exemption was added to stop erasure deadlocking on its own subject, and it works for that. But the gate found it "binds its holder to no tenant by any mechanism, is the sole writer of the per-tenant grants every other path consumes, widens as AD-6 is honoured — because AD-6 forces all tenant-resource work into the exempt class — and its principal has no tenant claim, so NFR8's principal-driven verification method cannot reach it" (SEC3-01, critical). It is written about workflows while the three checks are written about calls, so the provisioning deadlock survives at the AccessTelemetry call (SEC3-02). A compliant AD-6 *repair* can re-provision an erased tenant's ACL principal, graph, telemetry partition, and grant, none of which the register guards (ADV3-04).

**Decide:** the exemption's exact membership (which operations), its binding to a tenant (how an exempt principal is scoped to the one tenant it may touch), and whether NFR8's verification method must be extended to cover principals with no tenant claim. Recommended: enumerate the exempt operations exhaustively, bind each invocation to exactly one tenant recorded in the lifecycle evidence, and make "no enumerated operation may create or restore resources for a tenant present in the register" an explicit clause.

### D3 — MVP epoch and staging semantics

**Why it blocks.** AD-14's ratified Phase 1 permits a degraded rebuild; AD-3's write fence says a differing-epoch write "targets AD-14's disjoint staging resources." The gate found AD-14 defines staging only for Phase 2 and never for the graph store, and that "a Phase 1/MVP degraded rebuild has one active epoch and writes under it" is satisfiable two ways, both bad: bump-and-activate flips every unit in the tenant to `indexing` and fails tenant-wide queries; keep-epoch produces mixed embedding dimensions under one epoch, which is precisely what AD-14 exists to prevent (ADV3-03, critical).

**Decide:** what an MVP degraded rebuild does to the epoch, and what the fence means when no staging resources exist. Recommended: MVP allocates a new epoch, writes under it, and activates atomically per tenant with the rebuild's acknowledged degradation covering the query gap — making the fence's staging clause Phase-2-only by explicit statement rather than by silence.

### D4 — The axis-state contract

**Why it blocks.** Three states (`unavailable`, `available`, `truncated`) now span AD-9, AD-10, and AD-12, and pass 3 generalised `truncated` beyond the graph axis. AD-12 still defines two encodings while the spine names three states (ADV2-17, aggravated), and the adapter reporting contract — who decides an axis's state, and how a surface serializes it — remains open (ADV-05, ADV-08 still open after three passes).

**Decide:** whether the axis-state contract belongs in AD-9 (ranking), AD-10 (degradation), or its own AD, and fix one canonical serialized form. Recommended: a new `AD-22` owning the three-state contract and its encoding, so no future amendment can scope one state in one AD and leave another AD's encoding behind.

### D5 — AD-20's approval route and a second approver

**Why it blocks.** AD-20 admits a dated phase exception "approved by product and architecture." You hold owner, product, and architecture. That route is self-approval, so only the evidence-path route is real, and 23 active-foundation-critical rows currently have neither. Separately, every recorded expiry anchors to the PRD's `[DERIVED]` 2026-10-31 checkpoint, whose owning decision — D1 of the Sprint-Readiness Recovery proposal — is itself `OPEN` with a recommendation to reset (SEC3-07, P3-07).

**Choose one:** name a second approver for architecture exceptions; or drop the two-party language from AD-20 and rely solely on evidence paths; or accept self-approval explicitly and record it as a known governance limitation. Recommended: drop the two-party fiction, keep the evidence-path route as the only one, and let exceptions require a dated entry plus a named reviewer who is not the author where one is available.

## 4. Recommended path and rejected alternatives

**Selected: freeze, decide, one convergence pass.** Record D1–D5, then execute a single amendment pass that closes the decided items *as classes rather than instances*, then re-run the full gate. Convergence is judged by the security lens reaching PASS-WITH-FINDINGS with no critical, not by finding count alone.

**Rejected — continue iterative amendment.** Three passes produced a stable pattern: each closes real findings and generates comparable new ones because the open items require decisions the editor cannot make alone. A fourth unguided pass would repeat it.

**Rejected — accept the spine as-is and proceed to epics.** Defensible for the 24 closed findings, but D1, D2, and D3 each describe a scenario a fully compliant implementation permits, in erasure, tenant isolation, and embedding integrity respectively. Deriving epics over them propagates the ambiguity into stories.

**Rejected — revert to the pre-update spine.** It is strictly worse: it carries the original 10 critical findings plus a false Web gap row, an unphased AD-14, no G6, and a gitlink that resolves to nothing.

## 5. Scope of the convergence pass, once D1–D5 are recorded

Class-level, not instance-level. Each item closes a category:

1. **Artifacts that must exist before the rules referencing them are enforceable:** the issuance grammar and its reserved delimiter, the tenant lifecycle state machine, the erasure-scoped store, the erased-tenant register, the `Contracts.V1` name register. Each gets a definition or an explicit deferral with a revisit condition — never a bare mention.
2. **Every rule that names an owner, store, or state:** sweep all 20 ADs for the pattern rather than fixing reported instances.
3. **Every state vocabulary:** axis states, packet states, status wire values, ledger dispositions — one definition site each, one encoding each.
4. **AD-5 split.** At 470 words it carries two decisions under one heading and concentrates the most findings of any AD. Split into identity/authority and issuance/grammar, taking the next free id; AD-5 keeps its number and its first decision.
5. **The eight pass-2 adversarial pairs whose text is still literally unchanged** (ADV2-06, -08, -10, -11, -12, -13, -15, -17) and the nine NOT CLOSED security items, which were not reached rather than judged closed.

## 6. Ledger scheduling — the actual G6 blocker

29 rows, 23 active-foundation critical, all carrying owner and date, **none carrying an evidence path or an approved exception**. Under AD-20 that is 23 open gate blockers. Converting them is ordinary planning work and is the precondition for G6, independent of D1–D5.

Recommended sequencing, subject to the Sprint-Readiness Recovery proposal's own epic structure rather than duplicating it:

| Band | Rows | Handling |
| :--- | :--- | :--- |
| Production security blockers | .NET CVEs, OpenBao, PostgreSQL, absent per-tenant backend principals, floating base image | Each needs a dated exception or a fix; four of five are version bumps with a known target. |
| Erasure and isolation contract | register, erasure-scoped store, AD-4 content enforcement, pre-grammar identifiers, lifecycle state machine | Blocked on D1 and D2; schedule after. |
| Projection integrity | AD-3 checkpoint, replay path, status persistence, ingestion command boundary | Blocked on D3 for the epoch half. |
| Retrieval determinism | fusion canonicalization, graph seeding, kill switch, G1 harness | Blocked on D4 for the axis-state half. |
| Boundary enforcement | AD-8 namespaces and guards, unguarded AD-1 invariants, `Contracts.V1` register, `tools/` inventory | Independent; schedulable now. |

## 7. Approval conditions

This proposal is approved when Jerome records D1 through D5 with a one-line rationale each. Partial approval is acceptable and unblocks the corresponding band in section 6; D5 may be recorded independently of D1–D4.

No spine edit, story registration, or tracker change may proceed on the strength of this document alone.

## 8. Exact ordered handoff

1. Jerome records D1–D5.
2. Architecture executes one convergence pass scoped to section 5, logging each decision to the spine memlog before editing.
3. Full reviewer gate re-run: rubric/closure, adversarial divergence, technology reality, security and isolation. Convergence criterion: security reaches PASS-WITH-FINDINGS with zero critical, and no lens reports a contradiction introduced by the pass.
4. Ledger rows acquire evidence paths or dated exceptions under section 6; tracker entries follow once `sprint-status.yaml` is unfrozen by the Sprint-Readiness Recovery correction.
5. Only then does epic derivation resume against the spine.
