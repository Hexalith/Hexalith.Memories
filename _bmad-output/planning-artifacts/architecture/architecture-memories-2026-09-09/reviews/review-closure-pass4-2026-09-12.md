---
lens: amendment-closure + good-spine rubric
kind: reviewer-gate-pass4
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
scope: 'the 2026-09-12 convergence pass — execution of D1-D5 and the five class sweeps of sprint-change-proposal-2026-09-12-architecture-convergence.md section 5'
reviewed: '2026-09-12'
supersedes: 'nothing — complementary to reviews/review-closure-pass3-2026-09-12.md'
mode: read-only
---

# Reviewer Gate — Pass 4, Closure + Rubric (2026-09-12)

**Verdict: REVISE.**

**Counts: 21 CLOSED / 6 PARTIAL / 5 NOT CLOSED / 1 REGRESSED** (33 judgments: the 10 PARTIAL
items of the pass-3 Part A table, plus P3-01 … P3-23).

**AD ID constraint: NOT violated.** `AD-1 … AD-23`, 23 headings, each exactly once, in sequence,
no gaps, no renumbering, no reuse, none retired. Verified mechanically.

This is the best of the four passes. It closed both of pass 3's headline reconciliations at the
definition rather than at the enumeration — AD-10's "if and only if … without truncation"
biconditional is gone (`:138`), and the erased-tenant register's two homes collapsed into one
(`:204`). Twelve of pass 3's twenty-three findings are cleanly closed, three of them by moving a
decision rather than by adding a sentence, which is what "close the class" means. The new preamble
rule at `:76` is the single most valuable edit in four passes.

It is REVISE rather than PASS-WITH-FINDINGS for one reason. **D3 — the decision the freeze existed
to settle for embedding integrity — is executed on its epoch half and left indeterminate on its
write half, and the indeterminacy is now a literal contradiction between two ADs' text with no
ledger row.** AD-14 `:162` requires a Phase 1 rebuild to write a second, non-active-epoch document
for a triple; AD-3 `:96` asserts, in the same words pass 3 quoted, that "derived stores hold
exactly one current document per `(tenantId, caseId, MemoryUnitId)`". ADV3-03 was rated critical
because "one active epoch and writes under it" was satisfiable two bad ways. After D3 it is
satisfiable two bad ways again, for a different reason. A closure lens that has watched three
passes narrow fixes to their exhibiting instance should not pass a fourth that leaves a ratified
decision unimplementable as written.

Everything else on the list is a sentence or two of editing. `status: final` (`:8`) is still
byte-identical while `:400` says no row may be cited as qualification evidence.

### Method

Read-only. Read: the convergence proposal sections 3 and 5; every AD-1 … AD-23 Rule, Binds and
Prevents; the Conventions, Registry, Stack, Seed, Boundaries, Map, ledger and Deferred sections;
`.memlog.md` entries from `:109` onward; the four pass-3 reviews; `prd.md`; and the cited files
under `src/`. Every present-fact claim the pass introduced about the codebase was re-derived. The
memlog was treated as the author's claim throughout and is contradicted in two places below.
Nothing was built, restored, staged or modified. The only file written is this review.

`lint_spine.py --workspace <run folder>` reproduces the memlog's claim: `total_findings: 0`.

Hostility standard, carried forward from pass 3: a fix is CLOSED only when the sentence it changed
and every sentence that sentence now interacts with are consistent. Where a predecessor quoted a
sentence and that sentence is byte-identical today, the verdict is NOT CLOSED and the argument is
not repeated.

---

## 1. Part A — the ten PARTIAL items of the pass-3 table

| # | Pass-3 item | Verdict | Current text and reasoning |
| --- | --- | --- | --- |
| 7 | AD-9/AD-10 contradiction, incl. depth-is-not-truncation | **CLOSED** | `:138` now reads "An axis can respond **safely** when its adapter applied the request's authoritative tenant and case scope to every result it returns and read only resources belonging to the tenant's active `(schemaGeneration, embeddingConfigurationEpoch)`. Completing within configured limits is not a condition of safety: an axis that hit a limit while keeping scope verified is safe and `truncated`". The biconditional and the words "without truncation" are gone; the three-passes-old 1.54× divergence is dead. AD-9 `:132` now delegates instead of restating: "**AD-22 owns the axis-state vocabulary, its denominator consequence, and its encoding, and AD-9 consumes it rather than restating it**". Residue is encoding-side only (**P3-15**). |
| 9 | AD-5 lifecycle exemption, grammar | **PARTIAL** | Exemption: closed and then some — see D2 below for what is enumerated. Grammar: `:216` still does not contain it. "The issuance grammar and its single reserved composition delimiter are declared in **one tracked artifact owned by this architecture and referenced by name from this decision**" — the decision names no artifact. `grep -on delimiter` finds the word five times on `:216` and the delimiter character on none of them. **P3-05.** |
| 10 | AD-17 continuing retention accounting for an erased tenant | **CLOSED** | AD-5 `:108` now reads "**Exactly three exemptions from that fail-closed exist, and no other decision may add a fourth without amending this list.**", with "(3) AD-17 retention accounting … is authorized by Platform Operations' own operator-scoped principal and is bounded in AD-17". AD-17 `:180` agrees: "**AD-5's third and last exemption**". The assertion now comes from the governing decision. This is a class fix: the closed-list clause forecloses the next AD-17-shaped claim as well as this one. |
| 11 | Erased-tenant register | **CLOSED** | Relocated wholesale. `:204`: "lives on a **platform-tenant EventStore partition** (`memories-platform`): never held under a tenant key, never encrypted under one, never crypto-shredded, and never carrying tenant content". `grep` for "Dapr state under AD-8" and "domain evidence under AD-2" returns nothing. The missing readers are supplied: AD-2 `:90` gives replay its own statement — "each of the three consults AD-21's erased-tenant register before admitting any record and fails closed when that register is unavailable — **authoritative replay included, which is not a restore path and therefore needs its own statement**" — and AD-6 `:114` supplies the provisioning consult. One residue, new: **P4-04**. |
| 13 | Erasure-scoped store defined | **CLOSED** | Named, not propertied. AD-4 `:102`: "read at execution time from the **tenant content store** … **That store is a named tenant resource, not a property**: AD-6 provisions and erases it with the tenant's other resources, its contents are written under the tenant key so AD-16 crypto-shredding reaches them, AD-16 names it in its enumerated purge targets, access to it derives tenant authority under AD-5 … and its capacity is an AD-18 tenant quota." All five SEC2-04 asks land, AD-16 `:174` carries it in the closed enumeration, and ledger `:383` exists. |
| 15 | Registry key prefix | **CLOSED** | `:241`: "Reserved key prefix `dedup:`, **as shipped** — the key family is `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` (`EventStoreDedupKey.cs:17`)". Verified: `src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs` builds exactly that, and `memories:preflight` has zero occurrences anywhere in the spine now. The promotion-in-place defect pass 3 found beside it is ledgered at `:397`. |
| 16 | AD-15's two release conditions | **CLOSED** | The "blocked until that expiry is met or extended" clause is deleted. `:168` now: "**Production qualification is blocked until per-tenant backend principals replace it, or until a dated, time-bounded security exception is recorded in the single exception register AD-20 defines** … The ledger's date on such a row is a **review checkpoint that obliges a re-decision, never a condition whose arrival lifts the block**". The second sentence closes the class, not just the instance. |
| 17 | AD-20 rewritten to oblige | **PARTIAL** | The three inconsistent statements are reconciled: `:198` and `:352` now share the vocabulary, and `:352` defines it once — "Every row's `Disposition` begins with its AD-20 state in one of exactly three words — **`blocker`** … **`evidenced`** … or **`excepted`**". All 44 rows comply (44/44 begin `` `blocker` ``). But AD-20 acquired a new obligation in the same edit — "records the triggering clause in the row" (`:198`) — and **0 of 44 rows records one**. See **P4-02**. |
| 18 | Ledger owner/date governance | **CLOSED** | `:400`: "**Every row above is `blocker`, and the owner and review date must not be read as though any row were not.**" `:402` restates the anchor honestly: the 2026-10-31 date's "owning decision — sprint-change D1 — is recorded OPEN with a recommendation to reset it, **so the date has no determinate value yet**". `:404` records the D5 drop and self-approval explicitly. This is the section pass 3 asked for, written better than it was asked for. |
| 20 | `prd.md` Open Questions 9 and 10 | **CLOSED** | `prd.md:62` now reads "architecture **resolved** AD-14 phasing and extended its binding to FR75/NFR37/G6 on 2026-09-12 (Open Questions 9 and 10 closed), but its alignment ledger carries no evidence path or approved exception". The no-go posture survives on its real grounds. |

**Part A: 8 CLOSED · 2 PARTIAL.**

---

## 2. P3-01 … P3-23 retest

**P3-01 — CLOSED.** `:138`, quoted above. The words "if and only if" and "without truncation" are
both absent from the document.

**P3-02 — CLOSED.** `:108` "Exactly three exemptions", `:180` "third and last exemption".

**P3-03 — CLOSED.** `:204`, and the two offending labels are gone from the file.

**P3-04 — CLOSED.** `:352` and `:400` no longer claim compliance with a form AD-20 does not
define. `:400` asserts the opposite: "AD-20 admits two satisfied states … **and no row is in
either**". The misattribution is corrected — `:198` requires "a tracker entry" and `:400` attributes
`sprint-status.yaml` to nothing, while AD-20 `:198` adds the freeze rule: "the freeze suspends the
tracker conjunct and no other, and **never converts an unmet evidence obligation into a satisfied
one**". New residue: **P4-02**.

**P3-05 — PARTIAL.** Three of four asks land. Validation-on-read is now normative and general:
`:216` "**Every trust boundary validates identifiers on read**, import included: 'treat issued
identifiers as validated' holds only for identifiers this platform issued, so an identifier
arriving through export re-import, a CloudEvent envelope, or a migration is validated against the
grammar before use or is rejected." The grammar's *properties* are now stated in unusual detail —
"lowercase ASCII — chosen, not merely single-case, so that identifiers are valid unchanged both as
DNS-1123 Kubernetes object names and as unquoted PostgreSQL identifiers", with a per-scheme
conformance test obligation. The ledger row `:381` was reduced to the grammar itself plus
remediation, as asked. What did not land: **the character set and the delimiter character are
still nowhere in the document**, and `:216`'s own sentence says they are "referenced by name from
this decision" when no name is given — as does `:381`, which asks to "Author the grammar and
delimiter as the tracked artifact **AD-23 names**". Two rules and one ledger row now assert the
existence of a reference that does not exist. Divergence consequence in **P4-06**.

**P3-06 — CLOSED.** All three undefined terms are derived. Operator principal, `:108`: "**An
operator principal is an ordinary authenticated identity marked operator-scoped in that
artifact** — a human operator's OIDC issuer-plus-subject or an allowlisted workload app ID … carrying
no tenant claim by construction", with the NFR8 gap closed in the same sentence: "the isolation
suite authenticates as an operator principal and asserts that every non-exempt operation it
attempts against a tenant is refused" (plus ledger `:388`). Lifecycle state machine, `:114`:
"**AD-6 owns the tenant lifecycle state machine and is its sole writer.** Its states are the
`Contracts.V1` `TenantStatus` values". Platform Operations, `:180`: "**Platform Operations is not a
new principal type:** it is an ordinary operator-scoped identity in AD-5's operator artifact".

**P3-07 — CLOSED.** `:402`, quoted in Part A item 18. The conditional is gone.

**P3-08 — CLOSED.** `:168`, quoted in Part A item 16.

**P3-09 — CLOSED.** `:76` no longer says "every": "The Current Alignment Gaps ledger is **the index
of outstanding enforcement obligations**". Both partial-coverage guards the code-reality lens
counted now have rows: the digest guard's Kubernetes-only scope at `:380` ("add it to the digest
guard, **which currently covers Kubernetes manifests only**") and the inventory gate's `src/`-only
scope at `:372` ("Extend the inventory gate beyond `src/`"). Residue, not scored: the Testing
convention `:236` now says "**every missing guard carries its own ledger row**", and the one
guard the code-reality lens rated PARTIAL for a different reason — "tests state their tier and
boundary", 118 of many classes carrying a `[Trait]` — has no row. The universal quantifier moved
from the preamble into a convention.

**P3-10 — CLOSED.** `:241`, quoted in Part A item 15, code-verified.

**P3-11 — CLOSED** by relocation. The register no longer sits on the Deferred component at all,
and Deferred `:411` says so: "the 2026-09-12 D1 decision moved AD-16's erased-tenant register onto
AD-21's EventStore platform partition, so **FR39/NFR16 no longer depend on this component's
qualification**". The rubric's one material Deferred violation is gone. The new dependency is
declared where it belongs: Stack `:266` adds "qualification of the EventStore platform partition
that AD-21 places the erased-tenant register on, which FR39/NFR16 now rest upon" to the
preconditions count, raising it from four to six in the open.

**P3-12 — CLOSED.** `:102`, quoted in Part A item 13.

**P3-13 — PARTIAL, and instance-scoped.** `:96` adds an outcome for exactly the case pass 3's
example named and leaves the complement: "Where the phase declares **no** staging resource for the
store being written, a write carrying a non-active, non-declared-staging tuple is **rejected as
out-of-generation and surfaced as an actionable `Failed` reprojection** — never silently dropped,
and never redirected to a resource the phase has not defined." In a phase that *does* declare
staging — Phase 2 and Phase 3, by AD-14 `:162` — the preceding clause still governs every
non-active write: "otherwise targets the **declared staging resources** AD-14 defines for that
derived store in that phase". A late write carrying a *retired* generation in Phase 2 is non-active
and is therefore directed at the live staging set it does not belong to, after AD-14's retire step
"deletes the retired generation's and epoch's checkpoint records" (`:96`). The fence still has
three inputs and two outcomes; only the phase in which the third input was easiest to name got the
third outcome. See §4.

**P3-14 — NOT CLOSED.** `:96` reads "derived stores hold exactly one current document per
`(tenantId, caseId, MemoryUnitId)` carrying the tuple it was projected from". The quantified clause
pass 3 quoted is byte-identical; no "within each resource set" qualification was added. This was a
MEDIUM finding in pass 3. It is not a MEDIUM finding now, because D3 made the invariant
load-bearing against AD-14's Phase 1 rebuild rather than only against Phase 2. See **P4-01**.

**P3-15 — PARTIAL.** The state half is closed and generalized: AD-22 `:210` "Exactly four axis
states exist and the set is closed", each defined, with `excluded` separated from `unavailable`
explicitly ("a different fact from being unable to answer and is never encoded as the same
value"), and depth excluded from truncation ("Reaching the configured candidate depth is **not**
truncation and never sets that state"). The encoding half is not. AD-22 discharges it by
delegation — "The four values and their packet shapes are reserved as a closed set in AD-12's name
register" — and AD-12 `:150` still encodes exactly two states inline: "an unavailable axis or absent
value is an explicit JSON `null` … an available-with-no-hits axis is an empty collection". The
register that would carry the other two is a file the ledger says does not exist (`:378`). So a
`truncated` axis's serialized shape is specified nowhere today, which is the same position
pass 3 described, reached by a better route. Pass 3's third ask also survives verbatim: `:210`
says `truncated` "is disclosed as **degraded**", while `degraded` is a packet-level state in AD-12
`:150`'s closed vocabulary. One word, two altitudes, one register.

**P3-16 — NOT CLOSED at the instance, CLOSED as a class, and re-opened by this pass.** The class
fix is real and is the right shape: AD-20 `:198` now defaults rather than asks — "where a recorded
classification and the predicate disagree, **the predicate governs** and the row is treated as
active-foundation critical until the owner reclassifies it in a spine revision". But rows `:366`
(source/package lanes lack independent contract/integration evidence) and `:372` (`tools/` outside
the inventory gate, `MigrateEmbeddingVectors` on a direct provider SDK) are still `No`, unchanged,
and still state no reason. Worse, the pass added a row with the same defect: `:378` — "`Contracts.V1`
has an owning component but **no wire-name register file and no build-failing guard**, so no closed
set the architecture now reserves there — AD-22's axis states, the packet states, the status wire
values, the error-code catalogue — is actually reserved" — is marked **No**, while `:392` (the
Evidence Packet has no canonical serialization, "so AD-12's byte-equality obligation and **G5's
golden vector** are unauthorable") is marked **Yes**. The two rows describe one artifact's absence
from opposite ends and disagree about whether it is critical. Under AD-20's fourth prong the
answer is Yes for both.

**P3-17 — CLOSED.** AD-3 `:96` defines invalidation without deleting: "AD-2's derived-store restore
invalidates a checkpoint by **writing an explicit `reprojectionRequired` checkpoint record for the
affected tuple, never by deleting it**: that record reports the public ingestion state as
`indexing` with a stated reprojection reason, does not violate `sourceVersion` monotonicity, and is
cleared only by a complete all-axis acknowledgement of the same tuple — so an absent checkpoint
record continues to mean that no projection work has ever been recorded, and the two are never
confused." Ledger `:394` carries the implementation.

**P3-18 — REGRESSED.** The split happened — AD-23 exists and AD-5 `:106` points at it ("Identifier
issuance and key composition are AD-23's") — and AD-5's Rule is **883 words**, up from 470 before a
split whose recorded purpose was to reduce it. Measured: AD-5 883, AD-16 627, AD-12 582, AD-9 491,
AD-3 445, AD-21 444, AD-23 440. AD-5 is still the longest rule in the document by 41%. The memlog
`:126` records this as "a tension rather than resolved" and flags a further split "beyond the
recorded section 5.4 decision" — honest, and the right call not to take unilaterally, but the
finding is not closed. §5 judges how many decisions the 883 words carry.

**P3-19 — PARTIAL.** `prd.md:799` is corrected: the MVP provider table now reads
`gemini-embedding-001` / 768, and `text-embedding-004` has zero occurrences in `prd.md`. The second
half is untouched. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` registers
two providers — `:20` `OllamaProviderName = "ollama"`, `:26` `OllamaModelName = "qwen3-embedding:4b"`,
`:69` `new(OllamaModelName, [2560])`, `:133` `Dimensions = 2560` — and Stack `:260` still records
one: "Google / `gemini-embedding-001` / `768`". A shipped second registration at 2560 dimensions,
under a rule (AD-14) whose *Prevents* names "mixed embedding dimensions", appears in neither the
Stack table nor any of the 44 ledger rows.

**P3-20 — CLOSED.** `prd.md:62`, quoted in Part A item 20.

**P3-21 — NOT CLOSED.** `:353` is `| Gap | Violated rule | Required convergence | Disposition |
Active-foundation critical |`. Byte-identical; classification still last, after the column that
holds the owner, against the intent recorded at `.memlog.md:87`. Flagged by RET-09, pass 3, and now
pass 4.

**P3-22 — NOT CLOSED.** `:8` is `status: final`. Byte-identical. `:400` says no row may be cited as
qualification evidence; `.memlog.md:127` says the reviewer gate is being re-run as four parallel
lenses, i.e. the document was labelled final before its review existed.

**P3-23 — NOT CLOSED.** `:268` still states as present fact that Redis Stack `7.4.0-v8` "is already
carrying roughly ten months of unpatched base-OS CVEs" on the production pin. The pass raised the
precondition count from four to six (`:266`) and Redis Stack is not among the six. It has no ledger
row among 44; its only home is Deferred `:417`, revisit "Before the next Production
support/security window". Its four siblings each have a row, an owner, a date and a "blocks
Production" disposition.

**P3 counts: 13 CLOSED · 4 PARTIAL · 5 NOT CLOSED · 1 REGRESSED.**

---

## 3. D1-D5 execution

| Decision | Verdict | Evidence |
| --- | --- | --- |
| **D1** — register on a platform-tenant EventStore stream, new AD for authorization and readers | **Implemented as recorded** | AD-21 `:200-204` delivers every named element: the `memories-platform` partition; "created and initialized by a **named platform-bootstrap step that runs before any tenant may be provisioned**"; the initialized marker and monotonic sequence; authorization in both directions ("writes are admitted only from the erasure workflow's operator-scoped principal and are append-only — EventStore's append-only semantics are the enforcement, not a convention … while reads are admitted to the enumerated consult paths and to operator principals"); and the availability rule as a positive fact ("an absent marker, an unreachable partition, a read error, or a restore older than the newest erasure it records are all **unavailable** — never an empty register"). The readers are wired at `:90` (AD-2, replay included), `:114` (AD-6 provisioning), `:174` (AD-16), `:108` (AD-5). Readiness is bound at `:232`. Bootstrap ordering is stated at `:314`. One inert element: **P4-04**. |
| **D2** — exhaustively enumerated operations, each invocation bound to exactly one tenant, no enumerated operation creating or restoring for a register tenant | **Under-implemented** | Two of the three clauses land generally and one lands only on two of the three exemptions. Enumeration, `:108`: the exempt class "reaches **only the operations the operator artifact enumerates**, which are exhaustively: tenant provisioning, tenant verification, lifecycle repair, grant amendment, per-tenant credential rotation, tenant-resource migration setup and retire, access-telemetry partition provisioning and erasure, tenant deactivation, and AD-16 erasure" — ten operations, and the inclusion of "access-telemetry partition provisioning and erasure" closes SEC3-02's workflow-versus-call gap at the class level rather than at the AccessTelemetry instance. Register clause, verbatim as decided: "**No enumerated operation may create, re-provision, or restore any resource for a tenant present in AD-21's erased-tenant register**, and an enumerated operation that cannot reach the register fails closed." Tenant binding: present and strong — "**scoped at initiation to exactly one tenant identifier recorded in its lifecycle evidence, which it may not change on resume or retry**" — but the sentence sits inside the "(1) … and (2) …" paragraph and governs "Each such workflow". Exemption (3), one sentence later, receives no tenant binding at all: "(3) AD-17 retention accounting … is authorized by Platform Operations' own operator-scoped principal and is bounded in AD-17." AD-17 `:180` bounds it by *operation* ("it permits counting and deleting records, and reading the erasure mapping **solely to locate them**") and never by tenant. D2 says "each invocation bound to exactly one tenant". One third of the exempt class is not. See §4. |
| **D3** — MVP allocates a new epoch, writes under it, activates atomically per tenant; the fence's staging clause Phase-2-only by explicit statement | **Under-implemented** | The epoch half is exact and well written. AD-14 `:162`: "**That rebuild allocates a new `embeddingConfigurationEpoch` and writes under it, and activates it atomically for the tenant when its verification succeeds** — it is the sole writer permitted to target the new epoch, ordinary ingestion continues under the epoch that is active until the moment of activation … so the rebuild never flips a tenant's whole corpus into `indexing` and never mixes two embedding spaces under one epoch." The explicit-statement half also lands: "the AD-3 fence's redirect to staging is therefore a **Phase 2 and Phase 3 clause by explicit statement**, not by silence." The write half does not. **P4-01.** |
| **D4** — a new AD owning the three axis states AND their single canonical encoding together | **States: implemented, and correctly wider than recorded. Encoding: under-implemented.** AD-22 `:210` owns four states, not the three D4 names, which is the right call — the spine already used four and P3-15 said so — and the widening is itself class-shaped: "**`truncated` applies to any axis in that position** — an uncompleted graph case partition, a timed-out or provider-cut-off syntactic or vector scan, `nl`, **or any axis added later**". The adapter reporting contract ADV-05 and ADV-08 left open for three passes is now owned and is the sole input to AD-10: "**Every adapter returns, alongside its hits, an explicit machine-readable statement** … That statement is the only evidence AD-10's axis-selection step uses — the server never infers either fact from a result set's shape — and an adapter that does not supply it is treated as scope-unverified and is nulled", with AD-10 `:138` agreeing verbatim and ledger `:391` carrying it. But D4's operative word was *together*: the AD was to own the states **and** fix one canonical serialized form so no amendment could leave an encoding behind. AD-22 hands the encoding to AD-12's name register; AD-12 `:150` encodes two of the four inline; the register is `:378`, which does not exist and which this pass classified non-critical. The encoding is once again in a different place from the states. |
| **D5** — drop AD-20's two-party language, evidence-path only | **Implements the proposal's recommendation, not the decision as recorded** | The two-party fiction is gone and replaced well: `:198` "**A dated exception is recorded by the architecture owner and reviewed by a named reviewer who is not its author where one is available; where none is, the row says so and the exception is recorded as self-approved rather than presented as two-party approval.**" That is section 3's recommended text almost verbatim, and `:404` states the drop and its reason. But the recorded decision (`.memlog.md:118`, and frontmatter `:27-28` "D5 AD-20 two-party exception route dropped; **evidence path only**") says the exception route ends. It does not: `:198` admits "**at least one** of a current re-runnable evidence path or a dated exception recorded in the single exception register", AD-15 `:168` depends on that register for its Production block, Stack `:266` offers "a dated exception" on four rows, and `:396` ledgers the register's absence. The spine's own frontmatter therefore mis-states what AD-20 now says. Two fixes are available and only one is right: amend the frontmatter, not the rule. Note also the deliberate widening from "exactly one of" to "at least one of … where both are present the evidence path governs and the exception is closed in the same change" — an improvement, and another place the frontmatter's summary no longer matches. |

---

## 4. Class-versus-instance test

This is the test the pass was convened to pass. For each significant fix, the complement of its
motivating case:

### Fixes that hold against the complement

- **AD-22's `truncated`** (`:210`). Motivated by the graph axis. Tested against a timed-out vector
  scan and an axis not yet invented: covered explicitly, including the uncovered-scope-unit report
  for a non-partitioned axis ("`0` with a stated cutoff reason").
- **AD-5's closed exemption list** (`:108`). Motivated by AD-17 claiming a third exemption.
  Tested against a fourth claim from any future AD: "no other decision may add a fourth without
  amending this list", and "Adding an operation or an exemption to this list is an architecture
  decision, not a configuration change."
- **AD-23's injective composition** (`:216`). Motivated by CloudEvent `source`. Tested against the
  complement: "every component that is not itself issued under the grammar — CloudEvent `source`
  and `id`, `IdempotencyToken`, principal identifiers, handle scopes — is length-bounded and either
  reversibly encoded … or replaced by a fixed-length hash of itself **before** composition". The
  distinction that keeps AD-4 honest is stated too: "That encoding is a composition step and never
  a normalization".
- **AD-19's digest consumers** (`:192`). Motivated by AppHost. Tested against the complement:
  "AppHost, `deploy/kubernetes`, CI, integration harnesses, and the `ContainerBaseImage` of every
  owned image alike, so no consumer declares its own and no owned image inherits a floating base
  tag."
- **AD-18's non-narrowing clause** (`:186`). Motivated by candidate depth. Tested against the
  complement: "they reject or delay a query, and never shrink AD-9's deployment-wide candidate
  depth, drop an axis, or change an axis's AD-22 state."
- **AD-16's export bundles** (`:174`). Motivated by the either/or pass 3 declined to score. Now
  "**Export bundles are erasure-scoped by one mechanism, not a choice of two**", with both halves
  required and re-keying forbidden.
- **AD-21's platform scope** (`:204`). Motivated by one deployment. Tested against DR sites and
  replicas: "every environment, replica, and disaster-recovery site that may admit a restore, an
  import, or a provisioning for that population reads and writes one register instance."
- **AD-20's predicate-governs default** (`:198`) and the **three-word disposition** (`:352`, 44/44
  rows). Both are class fixes that survive their complements.
- **The preamble rule** (`:76`) is the strongest class fix in the document: "**a rule that names an
  owner, a store, a state vocabulary, a principal, or any other mechanism the architecture has not
  yet defined carries a ledger row naming that mechanism**, and a reader may treat an obligation
  absent from both the rule text and the ledger as not yet governed." Spot-checked against seven
  mechanisms the pass introduced — grammar artifact `:381`, exception register `:396`, operator
  artifact `:385`, name register `:378`, adapter statement `:391`, bundle index `:389`, bootstrap
  step `:377` — all seven have rows. Its own complement fails twice, though: see **P4-05**.

### Fixes that are still instance-scoped

1. **AD-3's out-of-generation rejection** (`:96`) — scoped to "where the phase declares no staging
   resource", which is Phase 1. The complement (a retired tuple in a staging-declaring phase) is
   still redirected into the live staging set. **P3-13.**
2. **D2's single-tenant binding** (`:108`) — scoped to the two lifecycle exemptions that SEC3-01
   and ADV3-04 named. The complement (exemption 3, AD-17 retention accounting) has no tenant
   binding in either AD. **P4-03.**
3. **AD-21's tombstone reversal** (`:204`) — the remedy was written into the owning AD and not into
   any of the four rules that read the register. **P4-04.**
4. **P3-16's criticality mis-classification** — AD-20 got a class-level defaulting rule and the
   ledger's 44 rows were not re-swept against it; the pass then added `:378` with the identical
   defect. The class rule makes the answer derivable without amending the rows, which is why this
   is the mildest of the four, but the row set was not swept.

Score: 10 class-level fixes that hold, 4 fixes still scoped to their exhibiting instance. Pass 3
diagnosed "three of four highest-value fixes scoped to the exact case the prior finding
exhibited". This pass inverts that ratio. The pattern is broken, not gone.

---

## 5. Rubric walk

### Does every AD Rule prevent its stated Prevents?

Mostly, and more so than in any prior pass. Three exceptions, in severity order.

**P4-01 (critical-shaped) — AD-3 and AD-14 contradict each other on what a Phase 1 rebuild
writes, and nothing discloses it.** AD-3 `:96` asserts "derived stores hold exactly one current
document per `(tenantId, caseId, MemoryUnitId)`". AD-14 `:162` requires the Phase 1 rebuild to
write under a *new* epoch while "ordinary ingestion continues under the epoch that is active until
the moment of activation", and then says that after activation "AD-3's retire step deletes the
**superseded epoch's documents** and checkpoint records" — a plural that only parses if two
documents existed for one triple during the rebuild. AD-3's fence offers three dispositions and
none fits: the rebuild write does not carry the active epoch, so it does not "replace the
document"; AD-14 declares the redirect-to-staging clause "a **Phase 2 and Phase 3 clause by
explicit statement**", so it is not redirected; and it is not rejected either, because rejection is
conditioned on "a non-active, **non-declared-staging** tuple" and AD-14 says "Phase 1's only
declared staging resource is the rebuild epoch". So the write is neither replaced, redirected, nor
rejected, and the invariant it must not violate says only one document may exist. Two readings
remain available — overwrite the active document, or hold two documents per triple — which are
respectively "fails tenant-wide queries" and "the thing AD-14 exists to prevent", i.e. exactly
ADV3-03's critical pair, restored. AD-14's own first clause also indicts its second: "any phase in
which a non-active generation or epoch may be written declares the disjoint staging resource for
**every store** AD-3's fence covers — syntactic index, vector index, graph, and tombstones". Phase 1
writes a non-active epoch and declares one thing that is not a store. No ledger row states this;
`:395` ledgers the *implementation* of the rebuild epoch, which presumes the contract is decided.
**Fix:** qualify `:96`'s invariant per resource set as pass 3 asked (P3-14), and state in AD-14
whether Phase 1's per-store staging is the same physical store under a different epoch key or a
disjoint one. One sentence each, but it is a decision, not an edit.

**P4-04 (high) — AD-21's tombstone reversal is inert, so its *Prevents* is not prevented.** `:200`
undertakes to prevent "one spurious tombstone becoming an irreversible denial of tenant creation",
and `:204` supplies the remedy: "A tombstone written in error is corrected by an operator procedure
that **appends a recorded reversal**, never by a delete, so the write-once poison case has a
sanctioned remedy and the silent-deletion case has none." But every rule that consults the register
tests bare presence, and none of them mentions a reversal. `grep -on "reversal"` returns three
hits: `:204` and two ledger cells at `:384`. AD-16 `:174` "refuses to rehydrate any record whose
tenant **appears there**"; AD-5 `:108` forbids work for "a tenant **present in** AD-21's
erased-tenant register"; AD-6 `:114` "refuses any tenant in `Deleting` or `Erased`" and sets
`Erased` "**from** AD-21's register rather than beside it". A tenant with an appended reversal is
still present, still appears there, and still resolves to `Erased`. The remedy is writable and
unreadable. **Fix:** one clause in AD-21 defining the register's read semantics — a tenant is *in*
the register when its latest entry is an unreversed tombstone — which all four consult paths then
inherit without amendment.

**AD-23 (disclosed).** `:216` cannot prevent "an identifier whose characters defeat the isolation
mechanism that carries it" while the character set is unwritten. The rule says so itself — "until
that artifact exists this rule is binding and unenforceable, and its absence carries a ledger row
rather than passing silently" — and `:381` is that row. This is the honest form of an unenforceable
rule and is not scored as a rubric failure.

### Is anything under Deferred able to let two units diverge?

**No — for the first time in four passes.** The one material violation is gone (P3-11): `:411`
records that D1 detached FR39/NFR16 from the unqualified state store. Every remaining row states a
revisit trigger and pins the current position where it could otherwise leak: `:412` per-case
authorization ("until then AD-7 fixes case as partition and attribution only, **and no product
documentation may present cases as access control**", matching AD-7 `:120`), `:414` cross-case
references ("until then AD-7 forbids them"), `:415` hosted Web ("Server-derived authority remains
AD-5's"). One Deferred row still parks a live exposure rather than an option (`:417`, **P3-23**).

The divergence risk has *moved out of Deferred and into the rules*, which is a different rubric
line but the same consequence, and I record it here because it is where a reader will look:

- **P4-06 — two units can pick different delimiters.** AD-23 `:216` reserves "a single reserved
  composition delimiter" and never says which character. The Registry `:241` already assumes one:
  "`tenantId` and `caseId` rely on AD-23's grammar excluding the `:` delimiter", and the shipped key
  uses `:`. Conventions `:222` points at AD-23 as "the single definition site for the grammar, the
  delimiter, and composition". So one implementer reads `:` as settled by `:241` and another reads
  it as undeclared and picks `/` for object-storage paths, and both can cite the spine. AD-23's
  injective-composition guarantee is only as strong as the delimiter's uniqueness.
- **P4-01** — two epics can implement the Phase 1 rebuild's write target differently.
- **P3-15** — two surfaces can serialize a `truncated` axis differently.

### Does the spine ratify rather than contradict the brownfield at `src/`?

Four brownfield claims the pass introduced were re-derived and **all four are true**, which is a
better record than any prior pass:

- `:114` "Its states are the `Contracts.V1` `TenantStatus` values — today `Provisioning`, `Active`,
  `Deleting`, `Failed`, `CompensationFailed`" — exact, `TenantStatus.cs:12-27`. `Deactivated` and
  `Erased` are correctly presented as additive extensions, and `:382` ledgers the gap.
- `:150` the packet state vocabulary "is the shipped `Contracts.V1` `EvidencePacketState` enum and
  is its single definition site" — exact, all eight values in order at
  `Contracts/V1/EvidencePacket.cs:178-202`.
- `:241` `EventStoreDedupKey.cs:17` — exact, and the surrounding doc comment confirms the Server
  duplicate the row alludes to.
- `:386` "`StringComparer.OrdinalIgnoreCase` (`TenantEventRoutingOptions.cs:21`)" and
  `AutoProvisionRoutedTenants` — exact, `:21` and `:29`. Ratifying this as a *contradiction* to
  AD-5 with a row, rather than silently restating the shipped behaviour as the rule, is the right
  call.

**P4-05 (high) — but the preamble rule fails its own complement on two shipped contradictions that
this pass created new rules about.** Both are AD-12 and AD-22 obligations with no rule-text caveat
and no ledger row, so under `:76` a reader "may treat [them] as not yet governed" — which is the
opposite of what the ADs intend:

1. **Null-omitting serialization is enabled across Contracts.V1 and at the CLI surface.** AD-12
   `:150` requires "an unavailable axis or absent value is an explicit JSON `null` and never an
   omitted property, … **no surface enables null-omitting serialization for evidence-bearing
   types**". `grep -c JsonIgnoreCondition.WhenWritingNull src/Hexalith.Memories.Contracts` returns
   **84**, across 20 files that include `EvidencePacket.cs`, `SearchResult.cs`,
   `HybridSearchResult.cs`, `TraversalResult.cs`, `ScoredResult.cs`, `SearchExplanation.cs`,
   `EvidencePacketFreshness.cs` and `EvidencePacketMetadata.cs` — every evidence-bearing type named
   in the rule. `src/Hexalith.Memories.Cli/Output/Json/CliJsonContext.cs:19` and `:59` set
   `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` for the whole CLI JSON surface.
   Ledger `:392` covers property order, `R` formatting, escaping and transport framing, and does
   not mention null omission.
2. **The shipped axis-state shape cannot carry AD-22's four states.** `Contracts/V1/EvidencePacket.cs:94`
   is `IReadOnlyList<string> UnavailableAxes`, documented at `:86` as "Axes that were unavailable
   **or degraded**" — one name list collapsing two of AD-22's four states, which is precisely what
   AD-22 `:200` undertakes to prevent ("a surface collapsing 'could not answer' into 'was never
   selected'"). `:96`'s `AllEnabledAxesUnavailable` additionally carries
   `JsonIgnore(WhenWritingNull)`. The same shape repeats in `HybridSearchResult.cs:23`,
   `TraversalResult.cs:69` and `SearchResult.cs:61`. Ledger `:391` ledgers the *adapter* side of
   AD-22 and says nothing about the contract shape the packet must carry.

### Is every dimension the altitude owns decided, deferred, or an open question?

**Holds.** Operational Boundaries `:316-330` covers nine dimensions, each naming at least one AD,
and two rows were correctly re-pointed by this pass (Security now names AD-23 and the operator
artifact; Erasure now names AD-21's platform partition). The Capability → Architecture Map gained
three rows for the new decisions — platform bootstrap and erasure authority, identifier issuance
and key composition, axis state and adapter reporting — so no new AD is unreachable from the map.
The Structural Seed is unchanged and still carries a constraint per line. Licensing, accessibility
and operator-facing output remain governed as pass 3 found them.

### AD-5 at 883 words: one decision or more?

**More than one — at least three, and the two that should leave are named by other ADs.** The 883
words cover: identifier comparison; bearer and subject authority; the three internal-call checks;
the operator artifact; the operator principal's derivation and the NFR8 extension; the per-tenant
grant's membership, sole writer, amendment path and revocation bound; pub/sub ingress and the
routing map; background-work authority; and the three-exemption regime with its ten enumerated
operations. Two of those are separable decisions with their own consumers:

- **The operator artifact** (`:108`, "**One deployment-time artifact, the operator artifact, carries
  all three of the platform's non-tenant authorization facts**") is a deployment-and-secrets
  decision about a versioned artifact with one writer, held in AD-15's operator scope. AD-15 `:166`
  binds it, AD-17 `:180` enumerates itself inside it, AD-6 `:114` reads its operation list, and
  Operational Boundaries `:321` names it as "the single home for the allowlist, the enumerated
  lifecycle operations, and the pub/sub routing map". Four other sections cite an artifact that has
  no heading of its own.
- **The privileged/exempt regime** (the three exemptions, the ten operations, the register clause,
  the travels-with-the-principal rule) is the decision D2 ratified. It is cited by AD-6, AD-16,
  AD-17 and AD-21.

AD-5's *Prevents* now lists four distinct harms, two of which ("a privileged principal that
authorizes work while being derivable from nothing"; "an exempt class of work that no mechanism
binds to a tenant") belong to the privileged half alone — the clearest internal signal that the
heading carries more than one decision. The memlog `:126` reaches the same conclusion and correctly
declines to act on it without the user. Recommendation, for the user and not for an editing pass:
one further split taking the operator artifact and the exempt regime into a new AD-24, leaving AD-5
as caller-authority derivation. AD IDs stay stable either way.

---

## 6. New findings this pass

| id | Finding | Severity | Lines |
| --- | --- | --- | --- |
| **P4-01** | AD-3's one-document invariant and AD-14's Phase 1 new-epoch rebuild cannot both hold; the fence gives that write no outcome; undisclosed in the ledger. D3's purpose is defeated. | **CRITICAL-shaped** | `:96` vs `:162` |
| **P4-02** | AD-20 now requires each row to record "the triggering clause"; 0 of 44 rows records one. A pass-created obligation the same pass's ledger universally violates. | **MEDIUM** | `:198` vs `:355-398` |
| **P4-03** | D2's single-tenant binding reaches exemptions (1) and (2) and not (3); AD-17's exemption is bounded by operation only, so one third of the exempt class is bound to no tenant — the defect SEC3-01 was rated critical for. | **HIGH** | `:108`, `:180` |
| **P4-04** | AD-21's tombstone-reversal remedy is written only into AD-21; all four consult paths test bare presence, so the *Prevents* it was added for is not prevented. | **HIGH** | `:204` vs `:90`, `:108`, `:114`, `:174` |
| **P4-05** | Two shipped Contracts.V1 realities contradict AD-12's null rule (84 `WhenWritingNull` attributes plus the CLI's global default) and AD-22's four-state contract (`UnavailableAxes: string[]`, documented as "unavailable **or degraded**"), with no caveat in either rule and no ledger row — so `:76` lets a reader treat both as not yet governed. | **HIGH** | `:150`, `:210` vs `EvidencePacket.cs:86,94,96`, `CliJsonContext.cs:19,59` |
| **P4-06** | The single composition delimiter is reserved and never declared, while the Registry already relies on `:`; two units can choose differently and both cite the spine. | **MEDIUM** | `:216`, `:222`, `:241` |
| **P4-07** | Frontmatter `:27-28` records D5 as "evidence path only" while AD-20 `:198` admits "at least one of" an evidence path or a dated exception, which AD-15, Stack and `:396` all depend on. The frontmatter is wrong, not the rule. | **MEDIUM** | `:27-28` vs `:198` |
| **P4-08** | The PostgreSQL CVE count is internally inconsistent: Stack `:268` says "28 CVEs, fourteen of them at CVSS 8.8", ledger `:379` says "28 CVEs, four at CVSS 8.8". `.memlog.md:104` records the four→fourteen correction as made. | **LOW** | `:268` vs `:379` |
| **P4-09** | `:216` ends and `## Consistency Conventions` begins with no blank line between them (`:216`/`:217`). The linter does not catch it. | **LOW** | `:216-217` |

---

## 7. What a fifth pass should require

Only the first item is a decision. The rest are one or two sentences each.

1. **P4-01** — decide whether Phase 1's per-store staging is the same store under a different epoch
   key or a disjoint one, then qualify AD-3's one-document invariant per resource set (P3-14) and
   say which in AD-14. Until this is settled the epoch contract is not derivable into stories.
2. **P4-04** — one clause defining the register's read semantics with a reversal present.
3. **P4-03** — bind exemption (3) to one tenant per invocation, or state in AD-17 why a
   cross-tenant retention sweep cannot be so bound and what bounds it instead.
4. **P4-05** — a caveat plus a ledger row for each of the two shipped contradictions.
5. **P3-13** — the third fence outcome for a staging-declaring phase.
6. **P4-06, P3-05** — declare the delimiter character and name the grammar artifact, or stop saying
   `:216` and `:381` reference it by name.
7. **P3-15** — encode `truncated` and `excluded` in AD-12 alongside the other two, and rename the
   axis-level `degraded` annotation.
8. **P3-16** — reclassify `:366`, `:372` and `:378`, or state the reasoning in each Disposition;
   **P4-02** — add the triggering clause to all 44 rows, or delete the obligation from `:198`.
9. **P3-19, P3-23, P4-08** — the Ollama registration, a Redis Stack row, the CVSS count.
10. **P3-21, P3-22, P4-09** — column order, `status: final` → `final-pending-review`, blank line.

The original spine took four adversarial passes. This one has had four, and the curve did flatten:
seven highs in pass 3, one critical-shaped and three highs here, of which two are the complements
of fixes this pass made rather than new territory. One more pass should close it. This one should
not be the last.
