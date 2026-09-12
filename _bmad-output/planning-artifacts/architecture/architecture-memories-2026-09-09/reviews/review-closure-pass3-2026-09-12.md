---
lens: amendment-closure + good-spine rubric
kind: reviewer-gate-pass3
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: 'the second amendment pass (2026-09-12) — the ~30 edits made in response to the four pass-2 re-tests'
reviewed: '2026-09-12'
supersedes: 'nothing — this is the third pass, complementary to reviews/review-closure-retest-2026-09-12.md'
mode: read-only
---

# Reviewer Gate — Pass 3, Closure + Rubric (2026-09-12)

**Verdict: PASS-WITH-FINDINGS.**

The second amendment pass did real work. Every one of the four factual errors the *first*
amendment introduced is now corrected and independently re-verified against the repository and
upstream; the Stack caveat is the strongest section of the document. Ten of the twenty claimed
closures are clean. AD IDs are stable at AD-1…AD-20, no adopted decision was reversed, and the
seed and dimension coverage still hold.

But the pattern that made pass 2 necessary has repeated at reduced amplitude: **the two headline
reconciliations are closed in the prose that was added and left open in the prose that was not
edited.** AD-10 still opens with a biconditional requiring "without truncation" for safety, three
sentences before declaring AD-9's `truncated` safe. AD-17 grants itself an exemption from AD-5
that AD-5's own sentence — "AD-6 lifecycle workflows and AD-16 erasure **are the stated
exception**" — forecloses. The erased-tenant register was given a home in Dapr state and, in the
same paragraph, keeps the label "domain evidence under AD-2", which AD-2's *Prevents* forbids.
And the owner/date column, the pass's most visible change, is a constant on 24 heterogeneous rows
anchored to a checkpoint whose own change proposal recommends abandoning it.

`status: final` (`:8`) is not defensible while the security lens verdict stands at FAIL, the
memlog records a third pass as owed (`.memlog.md:103`), and the ledger itself states that no row
may be cited as satisfied qualification evidence (`:345`).

| Measure | Result |
| --- | --- |
| Claimed closures verified | **20** |
| CLOSED | **10** |
| PARTIAL | **10** |
| NOT CLOSED | **0** |
| REGRESSED | **0** adopted decisions; **1** editorial regression (AD-5 grew 379 → 470 words, against a recorded pass-3 obligation) |
| AD IDs | **stable** — AD-1…AD-20, each exactly once, sequential, none renumbered |
| New findings | **23** (P3-01 … P3-23): 7 high, 10 medium, 6 low |

### Method

Read-only. The spine, `.memlog.md`, the four pass-2 re-tests, the pass-1 validation report,
`prd.md`, `sprint-change-proposal-2026-09-12.md`, and the cited source files were read. Every
statement of present fact the second pass introduced was re-derived from the repository
(`git ls-tree HEAD`, `git submodule status --cached`, `src/`, `deploy/`). Nothing was built, run,
restored, or modified; no submodule was touched. The only file written is this review.

Hostility standard: a fix is CLOSED only when the sentence it changed *and every sentence that
sentence now interacts with* are consistent. Adopting a proposal's conclusion while dropping its
mechanism counts as PARTIAL, and so does relocating a contradiction rather than removing it.

---

## 1. Closure verdict per claimed item

| # | Claimed fix | Verdict | Current line and reasoning |
| --- | --- | --- | --- |
| 1 | Embedding model → `gemini-embedding-001` | **CLOSED** | `:226`. Matches `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:23` exactly. The added parenthetical "(a per-tenant AD-14 fact, recorded here because AD-14 binds it)" also answers VER2 item 7f's framing objection. Residue in **P3-19** (prd.md still stale; second registered provider omitted). |
| 2 | PostgreSQL flagged as a security blocker | **CLOSED** | `:224` row annotated "behind the 18.6 security release"; `:232` gives the date, the skipped 18.5, the 28-CVE count and the four CVSS 8.8 classes; `:337` is a ledger row; the headline is corrected to "Four items block Production qualification, not one" and the four enumerated at `:232` match exactly the four rows carrying "blocks Production" (`:324`, `:332`, `:333`, `:337`). This is the pass's cleanest fix. |
| 3 | Redis Stack EOL sentence corrected | **CLOSED** | `:232` — "maintenance ended December 2025 and the image has not been rebuilt since 2025-11-03 … (The 2026-11-30 date sometimes cited is Redis Software 7.4's EOL, a different product, and must not be read as headroom here.)" Deferred `:359` agrees. Residue in **P3-23** (the exposure has no ledger row). |
| 4 | FalkorDB → four released minor lines | **CLOSED** | `:232` — "four released minor lines behind `v4.20.4` — FalkorDB ships even-numbered minors, so the version-number distance overstates the gap". Correct count (4.14, 4.16, 4.18, 4.20), and the correction now explains itself rather than just restating a number. |
| 5 | `rollForward` semantics | **CLOSED** | `:232` — "floats the resolved SDK across feature bands rather than within one". Matches the documented `latestFeature` behaviour; the "within the 4xx band" error is gone. |
| 6 | "No stable alternative" claim | **CLOSED** | `:232` — scoped to "no stable release exists **in the pinned line**", with both counter-facts stated in the open ("13.0.0 is stable and 13.5.1-beta is newer than the pin"; "no 5.0.0 GA is published, though 4.x is stable"). Better than the correction that was asked for. |
| 7 | AD-9/AD-10 contradiction, incl. depth-is-not-truncation | **PARTIAL** | The two *added* sentences are right: `:116` "Reaching the configured per-axis candidate depth is **not** truncation"; `:122` "a graph fan-out that completed some but not all case partitions … is AD-9's `truncated` state, which is safe, disclosed, and contributes denominator weight". But `:122`'s **opening biconditional was not edited** and still reads "safely **if and only if** its adapter … returned within AD-11's or the axis's configured limits **without truncation**". See **P3-01**. Also unclosed: the three/four-state vocabulary and `truncated`'s missing encoding (**P3-15**). |
| 8 | AD-3 write fence scoped within one `(schemaGeneration, embeddingConfigurationEpoch)` | **CLOSED** | `:80` — "is a no-op when the incoming tuple is older **within the same `(schemaGeneration, embeddingConfigurationEpoch)`**; a write whose generation or epoch differs … is never merely older … so an active and a staging write can never alternate in one document. A Phase 1 / MVP degraded rebuild under AD-14 has one active epoch and writes under it." All three sub-parts of ADV2-03 answered. Two adjacent holes opened: **P3-13**, **P3-14**. |
| 9 | AD-5 lifecycle exemption, single-case grammar, widened surface list | **PARTIAL** | Single-case: **closed** (`:92`, "a grammar that is **single-case** — so no downstream system that folds case can collapse two distinct identifiers into one"). Surface list: **closed** — OpenBao secret paths, SQL identifiers and object-storage paths are all added. Exemption sentence: added at `:92` and reads correctly, but it is anchored to two terms the spine never defines (**P3-06**), and the grammar it depends on still does not exist and is owed by nobody (**P3-05**). |
| 10 | AD-17 continuing retention accounting for an erased tenant | **PARTIAL** | `:164` adds the clause, and the clause is the right *outcome*. It is asserted from the wrong decision: AD-5 `:92` closes its exception set with "AD-6 lifecycle workflows and AD-16 erasure **are** the stated exception", so AD-17 declares a third exception to a rule that does not admit one. **P3-02.** "Platform Operations" is also not a derivable principal (**P3-06**). |
| 11 | Erased-tenant register: store, durability posture, existence-from-provisioning, fail-closed, AD-6 read | **PARTIAL** | Store named: `:158` "It lives in Dapr state under AD-8, in a platform-scoped component outside any tenant-scoped keyspace". Existence: closed — "It exists from platform provisioning rather than from the first erasure". Unavailability: closed — "deletion and every restore path fail closed and remain resumable". Restore precedence: closed — "any restore that would return the register to a state older than the data being admitted fails closed". **Not closed:** the durability posture is asserted, not stated (**P3-11**); the AD-2 label contradicts the Dapr home (**P3-03**); AD-6's own Rule (`:98`) still carries no read obligation and the replay consumer has none at all (AD-2 `:74` / AD-3 `:80` are silent), so the register remains "the sole authority for replay rejection" with no reader. |
| 12 | `Hexalith.Memories.Contracts` named as `Contracts.V1` owner | **CLOSED** | `:134` — "`Hexalith.Memories.Contracts` is the single owning component for `Contracts.V1` and its maintainers hold change-approval authority over that wire surface". The project exists. The register's missing guard is ledgered at `:342`, and the preamble was widened to "the rule **or its ledger row** says so" (`:60`) so the unenforced-rule disclosure requirement is met. |
| 13 | Erasure-scoped store defined | **PARTIAL** | `:86` defines it by property only — "one whose tenant content is covered by AD-16's enumerated purge targets and by the tenant key, so shredding reaches it". AD-16's enumeration (`:158`) names no such store, so the property set can be satisfied by nothing; the store is still not named, provisioned by AD-6, authorized, quota'd, or ledgered. **P3-12.** |
| 14 | Export-bundle mechanisms restored | **CLOSED** | `:158` — "by one of two mechanisms — each bundle is registered at creation with its tenant and issuance time so tenant erasure invalidates every registered bundle, or bundles are wrapped under the tenant key so shredding covers them". Both mechanisms also close SEC2-08's re-import-under-a-new-tenant-ID variant, since both key on the bundle's originating tenant. Noted below: an either/or inside a Rule is a decision the spine declined to make. |
| 15 | Registry key prefix added | **PARTIAL** | `:208` declares "Reserved key prefix `memories:preflight:`". The string has **zero occurrences in `src/`**; the shipped reservation key is `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`. **P3-10.** |
| 16 | AD-15's false owner/expiry claim corrected | **PARTIAL** | Corrected in the sense that RET-01 named: `:152` now points at the ledger and `:324` carries "Owner: Jérôme Piquot; expiry 2026-10-31; blocks Production per AD-15". But the rewrite left two Production-block clauses in one sentence with **different release conditions**. **P3-08.** |
| 17 | AD-20 rewritten to oblige; "active-foundation critical" defined | **PARTIAL** | The definition landed and is usable: `:182` "violates a rule binding an MVP-active surface and its violation can produce incorrect product behaviour, cross-tenant exposure, irreversible data loss, or an unverifiable gate claim". The obligation landed: "**must carry** … exactly one of a current evidence path or a dated phase exception". But the same obligation is now stated three inconsistent ways (`:182`, `:316`, `:345`) — **P3-04** — two rows' criticality is not derivable from the new definition — **P3-16** — and ADV2-09's "no issuer of a `confirmed resolved` verdict" survives untouched. |
| 18 | Ledger grown to 24 rows, all with `Owner: Jérôme Piquot` and a 2026-10-31 expiry | **PARTIAL** | Mechanically complete and verified: 24 rows, 24 owner strings, the anchor `prd.md:1234` exists and reads exactly as quoted. As governance it is a constant column and a category error on the four security rows. Full judgment in **§3**; findings **P3-04**, **P3-07**. |
| 19 | `prd.md` second secret path removed | **CLOSED** | `prd.md:927` now reads "Kubernetes Secrets are permitted only where required for OpenBao bootstrap material. Direct Redis/FalkorDB credential injection is an alignment gap, not an approved second path (NFR9, architecture AD-15)." Matches AD-15 exactly. |
| 20 | `prd.md` Open Questions 9 and 10 closed | **PARTIAL** | Both are closed in the Open Questions list (`prd.md:1225-1226`) and OQ9 cites the spine's `ratifications` frontmatter, which closes RET-02. But `prd.md:62` still reads "architecture must still resolve AD-14 phasing and extend its binding to FR75 and NFR37" — the two things the spine has now done. **P3-20.** |

**Counts: 10 CLOSED · 10 PARTIAL · 0 NOT CLOSED · 0 REGRESSED.**

---

## 2. Consistency findings after ~30 further edits

### P3-01 — AD-10's safety biconditional still excludes the state AD-10 declares safe · **HIGH** · `:122`

`:122` opens: "An axis can respond **safely** if and only if its adapter applied the request's
authoritative tenant and case scope, returned within AD-11's or the axis's configured limits
**without truncation**, and read only resources belonging to the tenant's active
`(schemaGeneration, embeddingConfigurationEpoch)`."

Three sentences later, the same rule says: "a graph fan-out that completed some but not all case
partitions with scope verified on each is AD-9's `truncated` state, which is safe, disclosed, and
contributes denominator weight."

`truncated` is a defined term (`:116`). The biconditional is definitional and unqualified, so a
truncated axis is *by definition* not safe; the later sentence says it is. The amendment patched
the enumeration and did not touch the definition. An implementation reading the biconditional
nulls the graph axis and drops `0.35` from the denominator; one reading sentence three keeps it.
That is the ~1.54× composite-score divergence ADV-05 was written to close and ADV2-01 re-opened —
now surviving a second amendment. A careful reader can resolve it as general-rule-plus-exception,
which is why this is HIGH and not critical, but a spine should not require that reading of a
sentence that says "if and only if".

**Fix.** In the biconditional, replace "returned within AD-11's or the axis's configured limits
without truncation" with "returned within AD-11's or the axis's configured limits with the applied
scope verifiable (reaching the configured candidate depth is not a limit failure, and the
`truncated` state below is safe)".

### P3-02 — AD-17 claims an AD-5 exemption that AD-5's exhaustive sentence forecloses · **HIGH** · `:164` vs `:92`

AD-5 `:92`: "AD-6 lifecycle workflows and AD-16 erasure **are the stated exception**." Definite
article, closed set.

AD-17 `:164`: "Retention accounting for an erased tenant … continues under Platform Operations'
own authority and is **explicitly not blocked by AD-5's erased-tenant fail-closed**."

A telemetry TTL-purge sweep is "work with no human caller" under AD-5's own enumeration
("workflows, activities, actors, reminders, repair, replay, and migration"), which "fails closed
when the tenant is … erased". AD-17 requires it to proceed. No implementation satisfies both
sentences, and the exemption is asserted from the decision that benefits rather than from the
decision that governs. This is a *new* contradiction created by the fix for SEC2-03, replacing a
deadlock with a conflict.

**Fix.** In AD-5 `:92`, change to "AD-6 lifecycle workflows, AD-16 erasure, and AD-17 retention
accounting for an erased tenant are the stated exceptions" and keep AD-17's sentence as the
cross-reference.

### P3-03 — the erased-tenant register is Dapr state and "domain evidence under AD-2" in the same paragraph · **HIGH** · `:158` vs `:74`, `:353`

`:158` gives the register a home — "It lives in Dapr state under AD-8" — and then says the
completion record inside it "is domain evidence under AD-2, not access telemetry".

AD-2 `:74` *Prevents* reads: "Redis, FalkorDB, workflow state, or package topology becoming an
accidental second system of record", and its Rule requires domain mutations to be accepted
"through Hexalith.EventStore before reporting success". Deferred `:353` identifies the production
Dapr state store as "the current Redis component". So the spine places a record it labels domain
evidence, and a register it calls "the **sole authority** for replay rejection, tenant-ID
non-reuse, and restore admission", in exactly the store AD-2 forbids from becoming a system of
record. ADV2-04's "two homes for one register" is not closed — it is now stated twice in one
paragraph rather than inferred across two ADs.

**Fix.** Choose one. Either (a) the erasure-completion event is committed through EventStore under
AD-2 and the Dapr register is its derived, fail-closed read index — say so; or (b) the register is
coordination state under AD-8 and the completion record is coordination evidence, and delete
"domain evidence under AD-2".

### P3-04 — AD-20's requirement is stated three different ways, and the ledger claims compliance with a form AD-20 does not define · **HIGH** · `:182`, `:316`, `:345`

- `:182` (AD-20, normative): "**must carry** … exactly one of a current evidence path or a dated
  phase exception approved by product and architecture, **each with a named owner and a tracker
  entry**".
- `:316` (ledger intro): "must carry an owner and evidence path or an approved dated exception" —
  the tracker entry has vanished, and "owner and evidence path" changes the disjunction's shape.
- `:345` (ledger closing): "Every row above carries the named owner and **dated expiry** AD-20
  requires."

AD-20 nowhere requires a "dated expiry". It requires a *current evidence path* or an *approved
dated phase exception*. A row reading "Owner: Jérôme Piquot; evidence path **owed** by 2026-10-31"
carries neither: an owed evidence path is not a current one, and a self-assigned target date is
not an exception approved by product and architecture. So `:345`'s first sentence asserts
compliance with a requirement that does not exist while the rows fail the requirement that does.

The paragraph's *honesty* — "**Tracker entries remain owed** … no row may yet be cited as satisfied
qualification evidence and each active-foundation-critical row stays an open gate blocker" — is
correct, welcome, and the best sentence in the ledger. Two defects sit beside it. First, it
misattributes: "AD-20 also requires an entry in `sprint-status.yaml`" — AD-20 says only "a tracker
entry"; the file is named by PRD G6 (`prd.md:189`). Second, it names the tracker freeze as the
*reason* no row qualifies, when the more basic reason is that no row has an evidence path or an
approved exception at all. As written, a reader concludes that unfreezing `sprint-status.yaml`
would make the rows citable. It would not.

**Fix.** Rewrite `:345`'s first sentence: "No row yet carries either form AD-20 admits. Each row
carries a named owner and a derived target date only; the evidence path or approved dated
exception, and the tracker entry PRD G6 requires in `sprint-status.yaml`, are both outstanding."
Align `:316` to `:182`'s wording verbatim.

### P3-05 — the issuance grammar and its delimiter are load-bearing, nonexistent, and owed by nobody · **HIGH** · `:92`, `:189`, ledger `:339`

AD-5 makes issuance the entire justification for ordinal comparison ("**Issuance is what makes
ordinal comparison sufficient**"), for the per-tenant Redis ACL principal, and for unambiguous key
composition — then defers all of it to "one **documented** bounded ASCII grammar" and "a single
delimiter **declared with the grammar**". No grammar document is named, referenced, or located.
The delimiter is never declared anywhere in the spine. `:189` restates the dependency without
supplying it.

Ledger row `:339` owes *remediation of pre-grammar identifiers* — enumerate, validate on read,
re-run G2 — which presupposes the grammar exists. Nothing owes the grammar itself. G2 ("zero
cross-tenant data leaks") therefore rests on an artifact with no author, no location, and no date.

The same row is also being used to carry a *missing rule*, not a missing implementation: "there is
no rule for identifiers arriving through export re-import" is an architecture gap, and `:316`
declares the ledger to be "implementation obligations against adopted decisions, **not** open
architecture choices". The validation-on-read decision belongs in AD-5.

**Fix.** State the grammar and the delimiter inline in AD-5 (they are one sentence each), add
validation-on-read at every trust boundary including import to AD-5's Rule, and reduce `:339` to
the remediation of already-issued identifiers.

### P3-06 — the lifecycle exemption rests on two terms the spine never defines, and a third authority appears from nowhere · **HIGH** · `:92`, `:164`

The exemption added at `:92` reads: lifecycle work is "authorized by the **operator principal
below** rather than by a tenant grant, and revalidate[s] against the **tenant lifecycle state
machine** rather than against tenant-active".

- "Operator principal" appears twice, both in `:92`, and is never derived. AD-5's four authority
  paths produce no operator principal: bearer authority carries a tenant claim; the allowlist
  "grants principal identity and never tenant scope"; the per-tenant grant cannot exist before the
  tenant does; channel authentication "never replaces tenant authorization". "Operator" exists
  elsewhere only as an AD-15 *secret scope* (`:152`). SEC2-15 rated this LOW when nothing depended
  on it; the exemption now makes it the sole authorization basis for provisioning and erasure.
- "Tenant lifecycle state machine" appears exactly once, here. AD-6 (`:94-98`) defines lifecycle
  *workflows* and never enumerates states; "deactivated" appears nowhere else in the document.
- AD-17 `:164` introduces "Platform Operations" as an authority that overrides an AD-5 fail-closed
  (see P3-02) and is likewise not a derivable principal.

**Fix.** Add one sentence to AD-5 deriving the operator principal (issuer + claim, or an allowlist
entry marked operator, with the authorizing principal recorded in lifecycle evidence), and one to
AD-6 enumerating the lifecycle states the exemption revalidates against.

### P3-07 — every date is anchored to a checkpoint whose own change proposal recommends abandoning it · **HIGH** · `:345` vs `sprint-change-proposal-2026-09-12.md:68`

`:345`: "The dates are **derived** from the PRD's `[DERIVED]` 2026-10-31 G1-prerequisite checkpoint
(`prd.md:1234`) … and move with it **if** the sprint-change D1 decision resets the gate schedule."

The anchor verifies — `prd.md:1234` reads exactly as quoted. But the conditional is already
resolved in the other direction: the in-flight `sprint-change-proposal-2026-09-12.md:68` reads
"Recommendation: select `RESET-ONCE` … Retaining the date without a capacity-backed commitment
would turn the 2026-10-31 checkpoint into a **predictable no-go** rather than a useful control."

So the spine dated 24 obligations, four of them live CVE exposures, against a checkpoint its own
upstream proposes to reset and calls unachievable — and presents that reset as a hypothetical.

**Fix.** Change "if the sprint-change D1 decision resets" to "the 2026-09-12 sprint change proposes
`RESET-ONCE` on this checkpoint (`sprint-change-proposal-2026-09-12.md:68`); these dates move with
the ratified outcome". See also §3.

### P3-08 — AD-15 blocks Production twice with two different release conditions · **MEDIUM** · `:152`

"…its named owner and dated expiry are recorded in the ledger and Production qualification is
blocked **until that expiry is met or extended by an approved exception**; **Production
qualification is blocked** until per-tenant backend principals replace it or a dated, time-bounded
security exception is approved…"

Condition one makes the *passage of a date* the unblocking event. Condition two, correctly,
requires the replacement or an approved exception. Read literally, 2026-11-01 unblocks Production
with the shared credentials still in place. This is drafting debris from splicing the RET-01 fix
into the SEC-10 sentence.

**Fix.** Delete "and Production qualification is blocked until that expiry is met or extended by an
approved exception".

### P3-09 — the preamble's "every outstanding enforcement obligation" is not true · **MEDIUM** · `:60`, `:199`

`:60`: "the Current Alignment Gaps ledger carries **every** outstanding enforcement obligation."
`:199`: "six of the invariants this spine relies on have no guard today, and the ledger carries
each missing guard as an obligation."

The six check out — rows `:326` (two AD-8 guards), `:338`, `:342`, `:343`. But the code-reality
lens counted seventeen invariants with "6 … unenforced, **one … enforced backwards**, and **two …
enforced only over a subset**" (`architecture-validation-2026-09-12/reviews/review-code-reality.md:126`).
Row `:327` covers the backwards one (the `Fuse_Bm25NaN_*` tests). Neither partial-coverage guard
has a row. `:60` is the escape hatch that lets an unenforced rule stay binding without further
disclosure, so an overclaim there is not cosmetic.

**Fix.** Soften `:60` to "carries every missing guard identified by the 2026-09-12 code-reality
review", or add the two partial-coverage rows.

### P3-10 — the newly reserved Redis key prefix is not the prefix the code uses · **MEDIUM** · `:208`

`:208` now declares "Reserved key prefix `memories:preflight:`". `grep -rn "memories:preflight"
src/` returns nothing. The shipped key is built at
`src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs:16` as
`dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`, duplicated in the Server's `DedupKeyBuilder`.

Two consequences. First, the rule "Every row declares its reserved Redis key prefix, which no other
row may claim" — added to close ADV2-16/RET-05 — now protects a namespace nothing occupies, while
the namespace that *is* occupied is unreserved. Second, the row's failure-posture column says the
reservation "fails open to AD-4 durable suppression", but in code the reservation key and the
permanent dedup key are the **same key**, promoted in place by
`src/Hexalith.Memories.Server/Activities/Ingestion/SaveDedupKeyActivity.cs`. The transient/durable
separation the row asserts does not exist today. Row `:326` ledgers "permanent dedup … outside the
finite exception registry" generically; nothing ledgers the prefix change or the promotion.

**Fix.** Either declare `dedup:` as the reserved prefix, or add "migrate the shipped `dedup:` keys
to the reserved prefix and separate the reservation key from the durable dedup record" to `:326`'s
convergence column.

### P3-11 — "carries a stated durability and restore posture" states no posture, and the obligation sits under Deferred with two deadlines · **MEDIUM** · `:158`, `:341`, `:353`

`:158` requires that "that component carries a stated durability and restore posture" — a
requirement that a statement exist, with the statement absent. The only durability statement in
the document is Deferred `:353`: the production Dapr actor/workflow state store "qualify or replace
the current Redis component against durability and recovery requirements", revisit condition
"**Before Production launch or SLO approval**". Ledger `:341` covers the same dependency with
"evidence path owed **by 2026-10-31**", active-foundation critical **Yes**.

So AD-16's fail-closed erasure gate — the hard gate for tenant deletion — is load-bearing on a
Deferred item, and the same obligation carries two different deadlines depending on which table
the reader consults. This is the one clear violation of the "nothing load-bearing under Deferred"
rubric (see §4).

**Fix.** State the posture in AD-16 (minimum: durable, replicated, point-in-time restorable, and
not the unqualified actor/workflow component), and make `:353`'s revisit condition defer to
`:341`'s date for the register's share of the qualification.

### P3-12 — the "erasure-scoped store" is defined by a property no named store satisfies · **MEDIUM** · `:86` vs `:158`

AD-4 relocates all tenant content out of workflow history into "an **erasure-scoped store** — one
whose tenant content is covered by AD-16's enumerated purge targets and by the tenant key". AD-16's
enumeration is "product projections, durable workflow history and activity payloads, actor state,
caches, and derived artifacts including embeddings and index terms" — a list of things that must be
purged, not a store that may be written to. The definition is therefore satisfiable only by
something already on the list, and the list contains no store an activity could write extracted
text to. SEC2-04's other four asks — named, provisioned, authorized, quota'd — are all still open,
and there is no ledger row.

**Fix.** Name the store, add it to AD-16's enumeration explicitly, and make AD-6 provision and
erase it as a tenant resource (as `:98` already does for the telemetry partition).

### P3-13 — the write fence routes a stale-generation write instead of rejecting it · **MEDIUM** · `:80`

"a write whose generation or epoch differs from the stored document's is never merely older — it
replaces the document when it carries the tenant's active generation and epoch, and **otherwise
targets AD-14's disjoint staging resources** rather than the active document".

A late write carrying a *retired* generation is neither active nor staging: AD-14's retire step has
completed and `:80` itself says retire "deletes the retired generation's and epoch's checkpoint
records". The rule directs that write at resources that no longer exist rather than discarding it.
The fence has three inputs (active, staging, retired) and two outcomes.

**Fix.** Append: "A write carrying a generation or epoch that is neither the tenant's active pair
nor a live staging pair is discarded and recorded as a stale-generation drop."

### P3-14 — "exactly one current document" is asserted globally while AD-14 mandates two · **MEDIUM** · `:80` vs `:146`

`:80`: "derived stores hold **exactly one** current document per `(tenantId, caseId,
MemoryUnitId)`". `:146` (AD-14 Phase 2): migrations use "**disjoint active and staging resources**
with a full-tenant reindex before atomic activation". During any Phase 2 migration there are two
documents for one triple. The later staging clause in `:80` implies the invariant is per resource
set, but the invariant as stated is unqualified and is the premise of the whole fence.

**Fix.** "derived stores hold exactly one current document per `(tenantId, caseId, MemoryUnitId)`
**within each resource set (active, and each live staging set)**".

### P3-15 — four axis-state names, a three-state enumeration, and a two-state encoding · **MEDIUM** · `:116`, `:122`, `:134`

`:116` names three states and says "the Evidence Packet distinguishes all **three** under AD-12's
encoding rules", then introduces a fourth on the same line: "where NL is inactive it is reported as
an **excluded** axis rather than omitted". `:122` confirms four by listing "unavailable/excluded/
truncated axes". AD-12's encoding rules (`:134`) define exactly two: explicit `null` for
unavailable/absent, empty collection for available-with-no-hits. `truncated` and `excluded` have no
encoding, so the delegation at `:116` points at a rule that cannot express what it delegates —
which also leaves the byte-comparable golden vector (`:134`, NFR25) unauthorable for any degraded
query. `truncated` is additionally described as "named in the Evidence Packet **as degraded**",
colliding with `degraded` as a packet-level state in the same vocabulary list.

**Fix.** Enumerate four states in `:116`, give each an explicit encoding in `:134`, and use a
different word than `degraded` for the axis-level annotation.

### P3-16 — two ledger rows are non-critical on grounds AD-20's new definition does not supply · **MEDIUM** · `:330`, `:336` vs `:182`

AD-20's only carve-out is "release and operational debt on **phase-inactive surfaces**".

- `:330` (source and package lanes lack independent contract/integration evidence, AD-19) is marked
  **No**. A release lane is not a phase-inactive surface, and missing lane evidence is precisely
  AD-20's fourth prong, "an unverifiable gate claim". Its AD-19 sibling `:331` is marked **Yes**.
- `:336` (`tools/` outside the inventory gate; `MigrateEmbeddingVectors` takes a direct provider-SDK
  dependency) is marked **No**, yet it contains the same AD-8 provider-SDK violation that makes
  `:326` critical, and the inventory-gate half is again an unverifiable release claim.

Under the definition the pass just added, both should be Yes, or each row should state why the
definition does not reach it.

**Fix.** Reclassify, or add the reasoning to the Disposition cell.

### P3-17 — AD-2 requires a checkpoint operation AD-3 does not define, and AD-3's amendment made it harder · **MEDIUM** · `:74` vs `:80`

AD-2 `:74`: derived-store restore "must **invalidate** the affected AD-3 checkpoints and re-verify
through AD-3's protocol". AD-3 `:80` defines exactly three checkpoint operations — monotonic CAS
advance, stale-ack rejection, and AD-14 retire-deletion — and now asserts "**exactly one**
checkpoint record exists per `(tenantId, caseId, memoryUnitId, schemaGeneration,
embeddingConfigurationEpoch)`". Invalidation is neither an advance (monotonicity forbids lowering)
nor a deletion (the existence invariant forbids absence). ADV2-13 flagged this before the existence
invariant was tightened; the tightening sharpened it.

**Fix.** Define invalidation in AD-3: "invalidation resets the record's per-axis acknowledgements to
none while preserving the tuple, returning the unit to `Indexing`".

### P3-18 — AD-5 regressed on an obligation the memlog recorded for this pass · **LOW** · `:92`

`.memlog.md:102` lists as open and owed to a third pass: "the AD-5 heading now carrying two
decisions in **379 words**". After the second pass AD-5's Rule is **470 words** — the exemption,
the single-case clause and three more excluded surfaces were added and nothing was split out. AD-5
is now the longest rule in the document (AD-9 460, AD-16 399, AD-3 298) and covers identifier
grammar, key composition, bearer authority, workload authority, allowlist governance, grant
ownership, pub/sub ingress, background-work authority, the lifecycle exemption, and operator
authorization.

**Fix.** Split issuance and key composition into their own AD. AD IDs must stay stable, so append it
as AD-21 and leave AD-5 pointing at it.

### P3-19 — the corrected model row now contradicts a declared source, and records one of two providers · **LOW** · `:226`, `prd.md:799`

`:226` is right and `prd.md:799` still says `text-embedding-004` — a model Google decommissioned on
2026-01-14 — while `prd.md` is listed at `:21` as an active `sources:` entry. `.memlog.md:95` records
that the PRD "owes that correction"; no ledger row owns it and the PRD was edited in three other
places in the same pass. Separately, the repository registers two providers
(`EmbeddingProviderDefaults.cs:17-26,131-133`): `google`/`gemini-embedding-001`/768 and
`ollama`/`qwen3-embedding:4b`/**2560**. A table that records one 768 pin under a rule that forbids
mixed dimensions should say the second exists.

**Fix.** Add a ledger row for the `prd.md:799` correction; add the Ollama registration to `:226` or
to the caveat.

### P3-20 — `prd.md`'s release posture still asks for what the spine delivered · **LOW** · `prd.md:62`

"architecture must still resolve AD-14 phasing and extend its binding to FR75 and NFR37" — AD-14 is
ratified (`:12-14`, `:142`), the header binds FR1-FR75 and NFR1-NFR37 (`:16-17`), and Open Questions
9 and 10 were closed in the same pass. The Open Questions list was updated and the body paragraph
that summarises it was not.

**Fix.** Update `prd.md:62` to cite the closures, leaving the no-go posture intact on its remaining
grounds.

### P3-21 — the ledger's column order still contradicts the recorded intent · **LOW** · `:318`

`.memlog.md:87` records the intent as "the classification column **preceding** the owner column so
the spine does not over-scope G6". The header is `| Gap | Violated rule | Required convergence |
Disposition | Active-foundation critical |` — classification last, after the column that holds the
owner. RET-09 flagged this; the second pass rewrote every Disposition cell and left the order.

### P3-22 — `status: final` is not supportable · **LOW** · `:8`

The security re-test verdict is FAIL, the adversarial re-test carries five open criticals,
`.memlog.md:103` says "A third reviewer pass is owed before the spine is treated as clean", and
`:345` says no row may be cited as satisfied qualification evidence. A document that cannot be
quoted as evidence should not be labelled final.

**Fix.** `status: final-pending-review` until P3-01 … P3-07 close.

### P3-23 — the one live security exposure with no owner, no date and no row · **LOW** · `:232`, `:359`

`:232` states as present fact that Redis Stack `7.4.0-v8` "is already carrying roughly ten months of
unpatched base-OS CVEs" on the production-pinned image. Its four siblings (.NET, OpenBao,
PostgreSQL, per-tenant principals) each have a ledger row, an owner, a date and a "blocks
Production" disposition. This one has a Deferred row (`:359`) whose revisit condition is "Before the
next Production support/security window". Under AD-20 an unowned gap on an MVP-active surface may
not be cited as gate evidence, and Deferred is not the ledger.

**Fix.** Add a ledger row, or state in `:232` why an unmaintained image with unpatched base-OS CVEs
is not a fifth Production blocker.

**Two further notes, not scored as findings.** (a) `:158`'s export-bundle clause offers "one of two
mechanisms" — registration at creation, or wrapping under the tenant key. Both are sound; leaving
the choice open is a decision the spine declined to make, and two epics can implement different
ones. (b) AD-20's `confirmed resolved` verdict (`:182`) still names no issuer, and RET-10 is
unfixed: `:38` still lists the pre-FR71 traceability matrix as an active companion.

---

## 3. Judging the owner and date recording

### The blanket owner

**Truthful, and therefore defensible as a record. Not defensible as satisfying AD-20.**

It is not a fabrication: this repository has one accountable maintainer, the git author and the
PRD's decision-maker are the same person, and `.memlog.md:104` records that the user supplied the
name on 2026-09-12. Writing one name 24 times is more honest than inventing team labels.

But the owner field exists so that an *unowned* row is detectable, a row can be routed, and an
overdue row can be escalated. A column with 24 identical values is constant, so it discriminates
nothing: it cannot fail, it cannot be queried, and the G6 check at `prd.md:189` — "every
gap/exception has an owner and tracking entry in `sprint-status.yaml`" — passes on it without
learning anything. Filling a required field with a constant converts a control into a formatting
convention.

The sharper problem is that it collapses a separation AD-20 itself requires. AD-20 admits a "dated
phase exception **approved by product and architecture**" — a two-party control. If the owner, the
product approver, and the architecture approver are one person, the second form of AD-20 compliance
is self-approval and provides no check at all, which leaves the ledger with exactly one usable path
(a current evidence path) and no exception route. The same collapse is visible elsewhere and is
already recognised upstream: AD-5's allowlist requires changes "only through a **reviewed** and
observable procedure" (`:92`) with a single writer, and the sprint change proposal blocks its own
G1 protocol on the fact that "neither independent reviewer is named"
(`sprint-change-proposal-2026-09-12.md:68`, `:84`, `:264`).

**Recommendation.** Keep the name, and make the constraint explicit rather than implicit. Add to
`:345`: "Hexalith.Memories currently has one accountable maintainer; the per-row owner is therefore
uniform, and AD-20's second admissible form — a dated phase exception approved by product and
architecture — cannot be satisfied while owner, product and architecture are the same person. Until
a second approver is named, only the evidence-path form is available."

### The single derived date

**The derivation is sound in method, verified in fact, and wrong in application on four rows.**

What holds up: `:345` says plainly that the dates are derived and not negotiated, which is the right
disclosure; `prd.md:1234` exists and reads exactly as quoted (I re-derived the anchor rather than
trusting it); and tying architecture dates to a product checkpoint rather than inventing them is the
correct instinct for a spine that must not invent organizational facts.

Three problems.

1. **The anchor is under an active reset recommendation** (P3-07). `prd.md:1234` is `[DERIVED]`, it
   sits beside an `[ASSUMPTION]` that the 2026-12-01 gate holds "until he ratifies or resets it",
   and `sprint-change-proposal-2026-09-12.md:68` recommends `RESET-ONCE`, calling retention of
   2026-10-31 "a predictable no-go". The spine presents the reset as hypothetical.
2. **A constant date carries no scheduling information.** The same 2026-10-31 covers "bump the SDK"
   — which `:232` says "already sits in the checked-out Builds worktree, making this a one-bump
   fix" — and "Integrate and verify the full erasure contract, including the register and
   per-target verification" (`:325`). A reader cannot tell which rows are weeks of work and which
   are minutes, which is the only thing a date column is for.
3. **Category error on the four security rows.** `:324`, `:332`, `:333` and `:337` are exposures, not
   deliverables. AD-15 requires "a dated, **time-bounded security exception**"; a security expiry is
   derived from exposure and exploitability, not from a product gate. Dating six live .NET CVEs, 28
   PostgreSQL CVEs (four at CVSS 8.8) and an unqualified OpenBao to a G1 sprint-selection checkpoint
   means that if the checkpoint moves — which its own change proposal recommends — the CVE
   deadlines move with it. That is backwards, and the .NET row makes it vivid: a one-submodule-bump
   fix already sitting in the worktree is given seven weeks.

**Recommendation.** Split the column. Give the four security rows risk-derived dates recorded as
AD-15/AD-19 security exceptions with their own approval line; keep the gate-derived date for the
remaining twenty and restate the anchor as "under an unratified `RESET-ONCE` recommendation".

---

## 4. AD stability and the good-spine rubric

**AD IDs — stable.** `AD-1 … AD-20`, each heading appearing exactly once, in order, with no
renumbering, no reuse, and no gaps. AD-14 keeps its ratification suffix in place. This is the third
consecutive pass to confirm stability.

| Rubric criterion | Verdict |
| --- | --- |
| **Enforceable rules** | **Mostly holds.** The added text is overwhelmingly obligation-shaped ("must carry", "fails closed", "is a no-op when"). Four exceptions, each named above: the grammar and delimiter that do not exist (**P3-05**), the erasure-scoped store defined by an unsatisfiable property (**P3-12**), the durability posture required-but-not-stated (**P3-11**), and `confirmed resolved` with no issuer. The preamble change at `:60` — "the rule **or its ledger row** says so" — is a genuine improvement that lets unenforced rules stay honest without bloating every AD. |
| **Nothing load-bearing under Deferred** | **Violated, once, materially.** Deferred `:353` (production Dapr actor/workflow state store, "the current Redis component", unqualified) is the store AD-16 `:158` now places the erased-tenant register in — the sole authority for replay rejection, tenant-ID non-reuse and restore admission, and the thing every restore path fails closed on. Ledger `:341` names the dependency, which is why this is a finding and not a failure, but the two carry different deadlines (**P3-11**). Secondary: the Redis Stack CVE exposure is described as live and parked in Deferred with no ledger row (**P3-23**). Every other Deferred row is a genuine future option with a stated revisit trigger, and AD-7 and AD-11 correctly pin the current position for the two that could otherwise leak (per-case authorization, cross-case references). |
| **No dimension silent** | **Holds.** Operational Boundaries covers nine dimensions and each maps to at least one AD. Consistency, security, reliability, capacity, observability, erasure, deployment, gate evidence and evolvability are all present; accessibility and operator-facing output live in the Active CLI output contract convention (`:193`) with an owner in AD-12; licensing sits in Deferred with a review trigger. No dimension is asserted without a governing rule. |
| **Seed still minimal** | **Holds.** Thirteen `src/` entries plus `deploy/`, `tests/`, `tools/`, `samples/`, `docs/` and the release manifest. Every line carries a constraint rather than a description — "Compatibility facade only; not the provider-adapter owner", "Local composition only; never a consumer dependency", "asset presence is not activation". Nothing was added by this pass. |

---

## 5. What a fourth pass should require

In order. Each is a sentence or two of editing, not a redesign.

1. **P3-01** — delete "without truncation" from AD-10's biconditional. Two words; it is the last
   survivor of the 1.54× divergence three passes have now chased.
2. **P3-02** — add AD-17 retention accounting to AD-5's exception list.
3. **P3-03** — decide whether the erased-tenant register is AD-2 domain truth or AD-8 coordination
   state, and delete the other label.
4. **P3-04** — rewrite the ledger's closing sentence so it does not claim compliance with a form
   AD-20 does not define, and align `:316` to `:182`.
5. **P3-05, P3-06** — write down the grammar, the delimiter, the operator principal's derivation,
   and the lifecycle states. Four sentences on which G2, erasure and provisioning currently depend.
6. **P3-07 and §3** — restate the date anchor against the standing `RESET-ONCE` recommendation, split
   the four security rows onto risk-derived dates, and say in the ledger that AD-20's two-party
   exception route is unavailable while owner, product and architecture are one person.
7. Then drop `status: final` back to pending until 1-6 land.

The original spine took four adversarial passes to converge. This one has now had three, and the
third found seven high findings where the second found five criticals. The curve is right; it has
not flattened.
