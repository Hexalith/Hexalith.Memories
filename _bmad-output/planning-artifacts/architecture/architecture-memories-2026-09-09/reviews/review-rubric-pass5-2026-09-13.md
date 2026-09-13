---
lens: good-spine-rubric
kind: reviewer-gate-pass5
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: 'pass-5 review after the pass-4 correction round; pass-4 closure plus fresh rubric findings'
reviewed: '2026-09-13'
mode: read-only
---

# Reviewer Gate — Pass 5, Good-Spine Rubric

**Verdict: REVISE.** The correction round closed the Phase-1 epoch contradiction and made the
alignment ledger materially more honest, but the spine still contains two security-critical
rule contradictions, does not execute ratified D5, and treats a current unsupported backend as a
future upgrade choice.

The spine and memlog were not modified by this lens. Mechanical lint is clean:
`lint_spine.py` reports `total_findings: 0`.

## Review basis

Read in full or at the load-bearing sections:

- the current `ARCHITECTURE-SPINE.md` and its `.memlog.md` through the 2026-09-13 resume event;
- the pass-4 closure/rubric review, with its pass-4 adversarial, security, and version reports used
  to distinguish carried findings from new ones;
- `prd.md`, especially FR39, FR44, FR66, FR71, FR75, NFR8-NFR10, NFR16, NFR18, and G6;
- the ratified D1-D5 decisions in
  `sprint-change-proposal-2026-09-12-architecture-convergence.md`;
- `addendum.md`, the current UX `DESIGN.md` and `EXPERIENCE.md`, and the structural/operational
  evidence named by the spine;
- the committed gitlinks and the specific brownfield files cited by the amended rules, including
  `EventIngestionService.cs`, `EventStoreDedupKey.cs`, `TenantEventRoutingOptions.cs`,
  `TenantProvisioningInput.cs`, `EvidencePacket.cs`, and `CliJsonContext.cs`.

Judgment follows the good-spine test: a decision is closed only when two implementation units one
level down cannot choose incompatible outcomes while both citing the spine. A disclosed
implementation gap is not scored as a contradiction unless the Rule, ledger, source requirement,
or current-reality claim disagrees with another binding statement.

## Pass-4 closure retest

| Pass-4 finding | Pass-5 verdict | Evidence |
| --- | --- | --- |
| P4-01 — AD-3/AD-14 Phase-1 rebuild target | **CLOSED** | AD-3 now makes generation and epoch part of each derived document's identity (`:96`), while AD-14 explicitly says Phase 1 uses the same physical store under a different epoch identity and reserves disjoint staging *resources* for Phase 2/3 (`:162`). |
| P4-02 — no ledger row records AD-20's triggering clause | **NOT CLOSED** | AD-20 still requires the clause in each row (`:198`); the 50-row ledger still has no field or disposition text that records it (`:354-405`). See R5-09. |
| P4-03 — exemption 3 lacks D2's one-tenant binding | **NOT CLOSED** | AD-5 binds only lifecycle and erasure workflows to exactly one tenant (`:108`). AD-17 limits retention accounting by operation, but still does not bind each invocation to exactly one tenant (`:180`). See R5-05. |
| P4-04 — tombstone reversal is inert | **PARTIAL, with a new contradiction** | AD-21 now defines latest-record effective-state semantics (`:204`), so consult readers can observe a reversal. That remedy conflicts with AD-6's terminal, irrevocable `Erased` state and has no unambiguous authorized writer. See R5-02. |
| P4-05 — null omission and four-state packet shape absent from the ledger | **CLOSED as disclosure** | Separate ledger rows now name both shipped contradictions (`:402-403`). They remain implementation blockers, correctly represented as such. |
| P4-06 — delimiter undeclared and artifact unnamed | **NOT CLOSED** | AD-23 still refers to one tracked artifact without naming it and never selects the delimiter character (`:216`); only the Redis registry happens to use `:` (`:241`). See R5-10. |
| P4-07 — D5 frontmatter conflicts with AD-20 | **NOT CLOSED and broader than frontmatter** | The frontmatter now says both “evidence path is the only satisfied state” and that exceptions are recorded (`:27`); AD-20 treats a dated exception as satisfied (`:198`), while ratified D5 says evidence-only (`.memlog.md:114`). See R5-03. |
| P4-08 — PostgreSQL CVE count | **CLOSED** | Stack prose and ledger both say 28 CVEs, fourteen at 8.8 and seventeen at 8.0+ (`:265`, `:373`). |
| P4-09 — missing blank line before Conventions | **CLOSED** | A blank line now separates AD-23 and `## Consistency Conventions` (`:216-217`). |

**Pass-4 total: 4 closed, 1 partial, 4 not closed.** The most important pass-4 decision fix,
P4-01, is genuinely closed. The remaining items are not merely implementation work: P4-03,
P4-06, and P4-07 still allow incompatible implementations or qualification decisions.

## Critical findings

### R5-01 — AD-5 mandates both envelope-derived and authenticated-channel-derived tenant routing

**Severity: CRITICAL · Disposition: autofix the stale clause, then re-review the whole artifact.**

AD-5 first defines the operator artifact as containing a
“CloudEvent-`source`-prefix-to-tenant routing map” (`:108`). The same Rule later says no
publisher-set field may select the tenant, identifies `source` as publisher-set, declares a
source-keyed map forbidden by AD-5's own *Prevents*, and requires the operator artifact to map the
authenticated subscribing component/topic/publisher identity instead (`:108`). The alignment row
correctly requires re-keying to authenticated channel identity (`:386`), but the owning Rule still
mandates the vulnerable source-prefix form in its artifact inventory.

Two teams can therefore ship opposite authorization boundaries while claiming compliance. This
can direct one publisher's content into another tenant, violating FR44 and NFR8-NFR10. It is also
the exact adjacent-clause propagation failure the correction round said it had fixed.

### R5-02 — reversal restores issuability while `Erased` is terminal and irrevocable

**Severity: CRITICAL · Disposition: discuss and decide the state transition and reversal writer.**

AD-6 says `Erased` is terminal and irrevocable, is derived from AD-21's register, and can never
disagree with it (`:114`). AD-21 now says a later reversal makes the identifier not erased and
“actually restores issuability” (`:204`). A reversal therefore either transitions a terminal
`Erased` tenant back to an issuable state, violating AD-6, or leaves the state `Erased`, making the
AD-21 remedy ineffective. The correction closed bare-presence reads but opened a direct state
machine contradiction.

Authorization is also incomplete: AD-21 exhaustively assigns its four record-type writers, but a
reversal is assigned only to an “operator procedure,” not to one of those authorized record-type
writers (`:204`). Before finalization, the spine must decide whether reversal is permitted only
before erasure completion, what lifecycle state it produces, and which principal/record type may
append it.

## High findings

### R5-03 — ratified D5 and PRD G6 have three incompatible exception regimes

**Severity: HIGH · Disposition: discuss; reconcile the ratified decision, spine, and PRD together.**

The authority record says D5 was ratified as “the evidence path is the only route”
(`.memlog.md:114`). AD-20 instead qualifies a row with either evidence or a dated exception and
permits explicit self-approval when no independent reviewer exists (`:198`). Frontmatter combines
both positions in one sentence (`:27`). PRD G6 still requires a phase-specific exception approved
by product and architecture (`prd.md:189` in the gate section), and the memlog already records this
upstream divergence as unresolved (`.memlog.md:128`).

The result is not an editorial nuance: one release unit can accept a self-approved exception,
another can require two-role approval, and a third can reject exceptions entirely. Because AD-20
binds G6, the spine cannot be final while its only qualification escape route has three meanings.

### R5-04 — AD-4 still falsely claims the shipped dedup key hashes `source`

**Severity: HIGH · Disposition: autofix the fact; retain the existing blocker row.**

AD-4 says the shipped reservation key satisfies composition by hashing `source` (`:102`). The
current code passes `envelope.Id` into `EventStoreDedupKey.Build`
(`EventIngestionService.cs:144`), and the builder hashes that argument
(`EventStoreDedupKey.cs:16-20`). AD-23, the Redis registry, and the ledger now state the correct
reality: the key hashes `id`, omits `source`, and therefore does not implement AD-4 (`:216`,
`:241`, `:400`).

Leaving the false blessing in the owning Rule makes the brownfield posture self-contradictory and
can suppress two real events from different publishers that share an ID. This is one of the three
locations pass-4 erratum E2 required to be corrected; two were fixed and AD-4 was missed.

### R5-05 — D2's one-tenant invariant still excludes retention accounting

**Severity: HIGH · Disposition: autofix by binding each AD-17 exempt invocation to one tenant, or
record a contrary architecture decision.**

D2 requires every exempt invocation to be bound to exactly one tenant. AD-5 applies that rule to
exemptions (1) and (2), then introduces exemption (3) without the same binding (`:108`). AD-17
enumerates what retention accounting may do but only says it operates “for an erased tenant”; it
does not prohibit a cross-tenant sweep under one invocation (`:180`). This is P4-03 unchanged and
leaves the claimless operator class outside the scope shape that NFR8 verifies.

### R5-06 — the permanent bundle index retains content-derived material outside the closed purge set

**Severity: HIGH · Disposition: discuss; either remove/content-harden the digest or include its
lifecycle in the erasure contract.**

AD-16 requires every store holding tenant content **or content-derived material** to be in one
closed purge-and-verification list (`:174`). It then requires a content digest for every export
bundle in AD-21's platform partition. AD-21 makes that partition append-only, never
crypto-shredded, and outside the enumerated purge targets (`:204`). A digest of an exported payload
is content-derived material and can be a membership oracle for low-entropy exports.

The rule thus closes erasure over a smaller set than its own store inventory. The ledger contains
the bundle-index implementation gap (`:389`) but no row for this architectural retention conflict.

### R5-07 — a restored register cannot prove its own staleness on provisioning

**Severity: HIGH · Disposition: discuss an external monotonic witness or a no-restore durability
contract.**

AD-21 says a register restored older than its newest erasure is unavailable and that a restored
register resumes at the highest sequence it can prove (`:204`). If the register itself lost later
entries, it cannot know that a newer sequence or tombstone ever existed. Artifact admission can
detect an artifact stamped above the restored value, but tenant provisioning has no such artifact
and may issue an identifier erased only in the lost suffix. This defeats AD-21's non-reuse
*Prevents* and FR39 under exactly the disaster-recovery condition the sequence is meant to cover.

### R5-08 — the Redis Stack security exposure is deferred although it is already current

**Severity: HIGH · Disposition: move to the active ledger or record a qualified replacement before
Production.**

The Stack section says Redis Stack `7.4.0-v8` is on an ended-maintenance line, has not been rebuilt
since 2025-11-03, and is already carrying roughly ten months of unpatched base-OS exposure
(`:255`, `:265`). Deferred nevertheless postpones Redis 8 qualification until the “next
Production support/security window” (`:425`), and no ledger row classifies the currently pinned
unsupported runtime itself. This is not a future option: active projection and query stores
already bind it. The deferment lets one release unit ship it and another block Production, both
citing the spine.

## Medium findings

### R5-09 — AD-20's required triggering clause is absent from every row

**Severity: MEDIUM · Disposition: autofix all rows or remove the requirement.**

AD-20 requires the architecture owner to record the criticality predicate's triggering clause in
each row (`:198`). The ledger schema has only a Yes/No column and none of its 50 rows names the
triggering clause (`:354-405`). This is P4-02 unchanged after the ledger grew by six rows.

### R5-10 — the grammar artifact, delimiter, and external-ID validation alphabet remain undecided

**Severity: MEDIUM · Disposition: discuss, then put the selected names/values in the owning Rule.**

AD-23 still names no tracked grammar artifact and does not select its “single reserved
composition delimiter” (`:216`); `:` appears only as an assumption of the shipped Redis family
(`:241`). AD-4 separately requires a “declared bounded character set” and canonical form for
CloudEvent `source` and `id` but names neither their definition site nor separate treatment for
URI-reference `source` versus opaque string `id` (`:102`). The ledger records that artifacts must
be authored, but the build substrate still lets independent units choose incompatible alphabets,
delimiters, and canonicalization.

### R5-11 — legal lifecycle transitions exist only as a ledger demand

**Severity: MEDIUM · Disposition: autofix by defining the transition contract in AD-6 or removing
it from Required convergence.**

AD-6 enumerates states and says transitions are event-committed, but never declares which state
transitions are legal (`:114`). The ledger then requires implementation to “declare the legal
transitions” (`:382`), despite its preamble forbidding Required convergence from introducing a
requirement absent from every Rule (`:352`). Two lifecycle units can therefore implement different
transition graphs without violating AD-6 as written.

### R5-12 — canonical nulls and authorization-withheld omission still overlap ambiguously

**Severity: MEDIUM · Disposition: autofix by separating property presence from collection-item or
detail visibility.**

AD-12 says every element is present, absent values are explicit JSON `null`, and no evidence
property is omitted. The same Rule says authorization-withheld detail is “omitted with no handle”
and indistinguishable from absence (`:150`). It does not say whether the containing property stays
present as `null`, whether an item is removed from a collection, or how byte equivalence remains
stable. This is pass-4 ADV4-12 still semantically open even though the current implementation's
general null omission is now correctly ledgered.

## Rubric result

| Rubric dimension | Result | Judgment |
| --- | --- | --- |
| Real divergence points complete | **FAIL** | Routing authority, reversal semantics, exception governance, exempt-tenant binding, identifier grammar, and lifecycle transitions still permit incompatible choices. |
| Rules enforce their Prevents | **FAIL** | AD-5 contradicts itself on tenant-routing input; AD-6 and AD-21 contradict on reversibility; AD-21 cannot prove a restored register current. |
| Deferred safety | **FAIL** | Most Deferred rows safely preserve the current rule, but the already-unsupported Redis Stack runtime is treated as a future upgrade choice. |
| Named technology current and accurately pinned | **FAIL** | The correction re-derived the current Builds and EventStore gitlinks, but the target production stack still defers an already-ended Redis Stack line and carries separately ledgered .NET, OpenBao, PostgreSQL, and image-digest blockers. |
| Brownfield fit | **FAIL** | The ledger is unusually candid, but AD-4 still blesses a key the code and three other spine locations say is non-compliant. |
| Requirements coverage | **FAIL** | Coverage is broad and the capability map reaches every major area, but FR44/NFR8 fail at the routing contradiction, FR39 fails at reversal/register recovery, FR75 fails at AD-4's false identity claim, and G6 has three exception regimes. |
| Operational/environmental envelope | **PARTIAL** | Deployment, environments, reliability, capacity, observability, erasure, release evidence, and evolution are all present; register disaster recovery and current backend security qualification remain unsafe. |
| Introduced contradictions | **FAIL** | R5-01, R5-02, and R5-04 are adjacent-rule propagation failures introduced or left by the correction round. |

## Gate conclusion

Do not set `status: final`. Resolve R5-01 and R5-02 first; reconcile R5-03 before any G6 or release
qualification claim; then apply the clear factual/ledger fixes and rerun all gate lenses. The
Phase-1 epoch correction is sound and should be preserved unchanged.
