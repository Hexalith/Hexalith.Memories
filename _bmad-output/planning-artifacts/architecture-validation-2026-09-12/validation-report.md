**FAIL — `ARCHITECTURE-SPINE.md` (`status: final`, 2026-09-09) cannot be cited as the current architecture authority without an Update pass. Its paradigm and its nineteen adopted decisions survive the gate intact; what fails is completeness, accuracy, and currency.**

# Architecture Spine Validation Report

**Validation date:** 2026-09-12
**Target:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md`
**Target sha256:** `e37070e372884925087b869bf8c96741849046b5d474f344dee8f32db77c918e` — byte-identical to the "Final architecture spine" hash pinned in `sprint-change-proposal-2026-09-12.md`
**Mode:** read-only validation. No spine, PRD, source, or configuration file was changed. No build, test, or restore was run. No submodule was initialized or updated.
**Next action:** **Update** — apply the amendments, resolve the one genuine conflict with a human decision, then re-run this gate.

## Result

| Measure | Result |
| --- | --- |
| Overall gate | **FAIL** |
| Lens verdicts | 2 FAIL (security/isolation, upstream drift) · 4 PASS-WITH-FINDINGS (rubric, technology, adversarial, code reality) |
| Deterministic linter | **PASS** — `lint_spine.py` → `ok: true`, 0 findings |
| Raw reviewer observations | **85** |
| Consolidated findings | **68** — 10 critical, 17 high, 41 medium/low |
| Adopted decisions requiring reversal | **0** |
| Approval | Do not cite as current authority for epic derivation or sprint selection until the criticals close |

The spine remains a genuinely good build substrate. Every one of the 85 observations is closable by adding or tightening a Rule sentence, correcting a stale fact, or recording a decision — **not one of them reopens an adopted decision or challenges the paradigm.** The adversarial lens discarded three candidate findings precisely because they required a unit to *violate* an AD rather than comply with it. The rubric lens rated structural balance a clean PASS: the seed is minimal, and AD-9's unusual length earns itself.

What produces the FAIL is the conjunction of four things a `final` document may not have:

1. **Three erasure holes and one tenant-isolation hole that a fully compliant implementation permits** (C1, C2, C6), each defeating an MVP hard gate as written (NFR16/FR39, NFR8/G2).
2. **One unresolved conflict with the approved product contract** (C7, AD-14) that only a human ratification can close.
3. **A ratified hard gate the spine cannot see** (C8, G6) plus a binding header that stops at FR74/NFR36/G5 while the 2026-09-12 contract runs to FR75/NFR37/G6.
4. **Statements of present fact that are wrong** (C9, H14, H15, H17) — an enforcement mechanism that does not exist, a gap row describing a component that shipped two months before the spine was finalized, and a pinned commit SHA that resolves to nothing.

## Scope and method

Six independent lenses ran as parallel subagents against the spine. Each wrote a full review and returned only a summary; this report merges those six files and adds no new research.

| Lens | Verdict | Raw findings | Review file |
| --- | --- | --- | --- |
| Deterministic linter (`lint_spine.py`) | PASS | 0 | — |
| Rubric walker (good-spine checklist, 9 dimensions) | PASS-WITH-FINDINGS | 14 | [`reviews/review-rubric.md`](reviews/review-rubric.md) |
| Technology / version reality *(configured floor)* | PASS-WITH-FINDINGS | 9 | [`reviews/review-technology-currentness.md`](reviews/review-technology-currentness.md) |
| Adversarial divergence *(configured floor)* | PASS-WITH-FINDINGS | 11 | [`reviews/review-adversarial-divergence.md`](reviews/review-adversarial-divergence.md) |
| Upstream source drift | **FAIL** | 20 | [`reviews/review-upstream-drift.md`](reviews/review-upstream-drift.md) |
| Security, tenant isolation & data integrity | **FAIL** | 19 | [`reviews/review-security-isolation.md`](reviews/review-security-isolation.md) |
| Brownfield code reality | PASS-WITH-FINDINGS | 12 | [`reviews/review-code-reality.md`](reviews/review-code-reality.md) |

Duplicates were merged by underlying decision failure; consolidated severity is the highest justified by any source lens. Every raw observation appears in the crosswalk.

### Two controlling facts

**The code has not moved.** `git diff --name-only 3644ef63..HEAD -- src/ tests/ deploy/ tools/` is empty: not one file under `src/`, `tests/`, `deploy/`, or `tools/` has changed since the commit that added the spine. No alignment-gap row can therefore be "stale by drift" — a row that does not match the code **was wrong on arrival**, falsifiable against the same tree the spine was written from. This raises the severity of the three misstated rows rather than excusing them.

**The product contract has moved.** Commit `42dfa26b` (2026-09-12) reconciled the PRD, addendum, and UX specifications against this spine. The PRD now carries FR75, NFR37, and G6; `DESIGN.md` and `EXPERIENCE.md` are the current UX contract. The spine has not been touched since 2026-09-09.

### Cross-lens corroboration

Findings confirmed independently by more than one lens carry extra weight and are marked **[corroborated]** below. Three notable convergences: the stale EventStore gitlink (technology + rubric + code reality, three separate derivations); the erasure-mapping ownership vacuum (adversarial + security, reached from opposite directions); and the false "Web is a non-runnable RCL" gap row (upstream drift + code reality).

## What the gate did not find

Recorded because a validation report that lists only failures misrepresents the artifact.

- **No reversed decisions.** Zero of 19 ADs need to be withdrawn or replaced.
- **Mechanically clean.** No placeholders, no duplicate AD IDs, every AD carries Binds/Prevents/Rule, every Stack version pinned.
- **The gap table's direction is sound.** 12 of 15 rows are real obligations today; **none** has been silently closed while remaining in the table.
- **Prior gate findings stayed closed.** The adversarial lens re-tested all 24 findings closed at the 2026-09-09 finalize gate; every one remains closed. Its new pairs attack a different layer — what *feeds* the ranking function rather than the function itself.
- **The Stack pins are honest.** Every pin verifies exactly against the *committed* `Hexalith.Builds` catalog `a32cb422`, and the caveat paragraph's opening disclaimer ("exact repository pins, not claims that every pin is the newest safe release") is doing real work. Nothing in it overstates risk.
- **Verified-accurate claims** that an adversarial read could have broken: the EventStore `Domain/` purity claim; `Hexalith.Memories.Redis` as a facade; the V1 status wire contract down to integer-token rejection; `MemoriesRoutes` and the `/api/v1` boundary; AD-9's fusion algorithm, AD-3's tuple, and AD-4's idempotency semantics all matching `addendum.md` clause for clause; graph case-partitioning and the cross-case path prohibition; digest pinning on every production-path image.
- **`IngestionWorkflowDeterminismGuardTests`** is a genuinely strong AD-4 guard — it pins four exact workflow-start sites by file:line and forbids ambient trace capture in orchestration code.

---

# Critical findings

## C1 — AD-16's erasure target set is under-enumerated; erased tenant content survives in four stores [corroborated]

**Sources:** SEC-02, SEC-03, DRF-10 · **Spine:** `:148-150`, `:74-78`, `:180`, `:270`, `:316`

A fully compliant implementation leaves an erased tenant's content readable in at least four places AD-16 never names.

- **Workflow history and actor state.** AD-4 excludes exactly one class of data from workflow history — *secrets*. An implementation that passes extracted text and embedding vectors as activity inputs/outputs (the natural shape) has the Durable Task runtime persist them to workflow history by construction. Crypto-shredding never touched them: they are not a product projection, not an EventStore payload, and were never encrypted under the tenant key.
- **Projection-store backups.** AD-16's quarantine predicate is a property of the *payload* ("quarantine unreadable payloads during restore"), true only of shredded EventStore records. A day-0 Redis/FalkorDB snapshot restored on day 30 is plaintext, so the rule never fires and the full syntactic index, vector index, and graph rehydrate.
- **FR71 export bundles.** Plaintext by design, covered by no rule; re-importable under a *new* tenant ID, which non-reuse does not catch.
- **Cross-tenant references.** `prd.md:546` makes references held in *other* tenants' units an application responsibility documented as a limitation; AD-16 is silent and therefore over-claims "complete tenant erasure".

**Consequence.** NFR16 names this outcome explicitly and forbids it. An embedding is invertible toward its source text, so the vector index is content, not metadata.

**Fix.** Extend AD-16's first clause to enumerate every store that can hold tenant content or content-derived material (workflow history, activity payloads, actor state, caches, embeddings, index terms), add "application export bundles and projection-store backups" to its *Binds*, and re-key the restore predicate from payload readability to the erased-tenant register (C2). Cheaper and structural: add to AD-4 that workflow history carries identifiers and references only, never content.

## C2 — The erasure record has two claimed owners, no store, and no durability posture [corroborated]

**Sources:** ADV-02, SEC-09, SEC-11 · **Spine:** `:150`, `:156`, `:194`, `:270`, `:316`

AD-16 makes the erasure mapping a precondition of completion in the passive voice with no owner; AD-17 assigns "tenant-erasure mapping" to Platform Operations; AD-8 puts "mappings" in Dapr state. Three sentences, three owners.

- **Two records, two stores, one required outcome.** The erasure workflow writes to Dapr state citing `:194`; the telemetry service holds the authoritative mapping in its own store (PostgreSQL per the addendum). They can disagree, and no AD says which one FR39's operator-facing evidence is read from.
- **Opposite blocking semantics, both compliant.** AD-17 constrains only *sanitized product writes* to be non-blocking. An erasure handoff is not one — so blocking on it is compliant, and returning `202` and landing it later is also compliant. Two teams build a deletion that hangs when telemetry is degraded, and a deletion that reports "erasure complete" for a handoff never received.
- **The non-reuse tombstone can inherit a TTL.** If realized inside the telemetry record, it sits in the one store whose defining property is a configured TTL and purge progress. After expiry the erased tenant ID is reusable — an irreversible data-protection outcome silently undone by a retention policy, with no rule broken.
- **Self-shredding enforcement.** If the tombstone is written to the tenant's own EventStore stream and the tenant key is then destroyed, the non-reuse check queries an unreadable record and permits re-creation — inheriting the erased tenant's key prefix, ACL principal name, and graph name.
- **"Verification" is undefined.** `:150` says the shredding workflow completes "with verification" and `:270` claims "completion evidence"; the term appears in no AD Rule. Any check satisfies it — a 200 from the key-destruction API, or a log line. Meanwhile the only durable per-tenant evidence surface is access telemetry, which AD-17 forbids certifying as tamper-evident.

**Fix.** Name a single durable erased-tenant register, held outside any tenant-scoped keyspace and not encrypted under a destroyed tenant key, as the sole authority for replay rejection, ID non-reuse, and restore admission — retained indefinitely as content-free evidence, with a stated durability and restore posture. Define verification as a recorded, reproducible per-target check.

## C3 — AD-3's six-field tuple has no ordering relation and no designated reported member [corroborated]

**Sources:** ADV-01, RUB-01, SEC-18 · **Spine:** `:72`, `:78`, `:138`, `:282`

Two units both comply with `:72` and build incompatibly. The ingestion coordinator keys one checkpoint per `(tenant, case, unit, schemaGeneration, epoch)` advancing monotonically in `sourceVersion`. The AD-14 migration backfill keys one record per `(tenant, case, unit)` holding the whole tuple, ordered lexicographically with epoch most significant — and can cite AD-14's "projection evidence carries the active configuration epoch" for why.

- **Permanent stall.** During a backfill at epoch 2, a user ingests a new revision at epoch 1. Under the second reading the epoch-1 acknowledgement is lexicographically lower and is rejected as stale — *forever*. Every retry produces the same verdict. Full compliance produces a unit stuck in `Indexing` for the whole backfill window.
- **Two conflicting public statuses.** Reporting the active-epoch tuple hides a reindex that silently failed for 40% of the corpus; reporting the highest tuple flips every unit in the tenant to `indexing` the instant a backfill starts, blowing NFR36's budget tenant-wide.
- **Writes are never fenced.** AD-3 makes *acknowledgements* monotonic and AD-4 requires an "idempotent upsert for AD-3's tuple", but neither orders the projection **write**. If the derived document is keyed by `MemoryUnitId`, a late write from an older attempt overwrites newer content; if keyed by the full tuple, stale documents for superseded versions stay searchable. Both readings conform.
- **No retirement owner.** AD-14's "retire" step is silent on checkpoint records.
- **The epoch itself has two candidate owners** — an EventStore-committed tenant-lifecycle fact, or the "tenant configuration actor" the capability map names, which AD-4 then says is not truth.

**Fix.** State the comparison relation explicitly: epoch and generation are the identity discriminator, `sourceVersion` the only monotonic dimension; an acknowledgement for a different generation or epoch is never stale; the reported public state is always the active tuple's; retire deletes retired records. Add write-side version fencing so a projection write is a no-op when its tuple is older. Commit the epoch through AD-2 before any projection may cite it.

## C4 — The `system:*` allowlist and tenant grant have two possible writers and no governance [corroborated]

**Sources:** ADV-03, SEC-07 · **Spine:** `:84`, `:88-90`, `:144`, `:179`

AD-5 requires "one operator-owned finite allowlist" mapping app ID to a `system:*` principal plus an "explicit tenant grant", and stops there.

- **Two compliant writers, opposite failure directions.** The tenant lifecycle workflow owns it (a grant is a "backend identity" under AD-6, so *only* the lifecycle workflow may change it) — a new tenant is ingestible the moment `tenant create` returns. Or it is a deployment artifact validated at startup (AD-5 says *operator*-owned; a table a workflow can append to is not finite by construction) — the same call fails closed and an operator must edit a file and redeploy. **This is the L2 launch-stopwatch path**, and whether that gate is passable at all depends on which reading wins.
- **At delete.** One reading revokes the grant as AD-16 completion work; the other leaves `system:*` granted on the erased tenant ID. AD-16 requires rejecting replay, but that rejection lives on the domain-command path while the grant lives on the channel-authorization path, and **no Rule conjoins them**.
- **Miss behavior unstated.** AD-5 says what to do on a hit and nothing on a miss, so a default principal is compliant — while NFR10 requires unknown apps to fail closed.
- **Grant cardinality unstated.** The projection coordinator, migration workflows, and repair work all legitimately span tenants, so the practical grant is "all tenants"; nothing forbids a wildcard, and "explicit tenant grant" degrades to `*`.
- **Integrity unstated.** The allowlist is authorization data, not a secret, so AD-15 does not cover it and the Configuration convention governs only secret references. Anyone who can edit ordinary configuration can grant any app any tenant, unreviewed.

**Fix.** Split ownership explicitly: the allowlist is a deployment-time artifact with exactly one writer (the operator), granting principal identity only, held in the operator secret scope, versioned, changed only through a reviewed observable procedure; the per-tenant grant is a tenant resource whose sole writer is AD-6's lifecycle workflow, created by verified provisioning and revoked as an AD-16 completion condition. Every internal call must satisfy allowlisted app ID **and** an explicit grant **and** the tenant being currently active — any one absent fails closed. Enumerate grants; a wildcard requires its own AD.

## C5 — The graph axis's seeds, limits, and truncation state are unbound

**Source:** ADV-04 · **Spine:** `:96`, `:108`, `:120`

The prior gate's four passes proved AD-9's merge order-deterministic *given a fixed candidate set*. Nothing in the spine fixes the candidate set.

- **The result limit's application point is unassigned, and it changes rank membership.** The adapter applies AD-11's limit per traversal call (a traversal is its unit of work, and AD-7 makes the case partition the traversal boundary); the fusion coordinator applies it to the merged list (AD-9 produces "the graph axis" as one list). For six cases at a limit of 50, one feeds 300 candidates into the merge and the other 50. Membership differs → competition ranks differ → contributions differ → composite scores and final ordering differ. **NFR25 and G5 fail with no bug on either side.**
- **There is no `truncated` axis state, so silent incompleteness is the compliant outcome.** A fan-out across case partitions can expire with some cases traversed and some not. AD-9 offers exactly two states: null (unavailable) and empty (available, no hits). A partially-traversed axis is neither — so it is reported as healthy while entire cases are missing. That is verbatim the outcome AD-10 exists to prevent. And because *which* partitions complete depends on wall-clock, the result is not reproducible.
- **"Top five hits" is ambiguous in the very sentence that defines ties.** First five surviving entries, or every entry with rank ≤ 5 (six or more under ties)? Raw provider list or canonicalized list? Different seeds → different graph candidates → different composite scores.

**Fix.** Take five *entries*, not five ranks, from the canonicalized lists; apply AD-11's result limit once to the merged graph list before rank allocation, never per partition; apply the time limit once to the whole fan-out; and add a third axis state, `truncated` — available, carrying its hits, contributing denominator weight, named in the Evidence Packet with the count of uncompleted partitions.

## C6 — No issuance-time grammar for identifiers makes ordinal comparison inert outside .NET

**Source:** SEC-01 (with SEC-19 as enabler) · **Spine:** `:84`, `:90`, `:175`, `:192`

The spine constrains how an identifier is *compared* (`StringComparison.Ordinal`, no case folding, no post-issuance normalization) and never constrains what one may *contain*.

- **Redis ACL globs.** AD-6 mandates a per-tenant Redis ACL principal resolved server-side. A tenant created with ID `a*` composes a key permission such as `~a*:*`; Redis evaluates ACL key patterns as globs, so that principal reads and writes **every key of every tenant whose ID starts with `a`**. The isolation boundary the addendum calls primary is bypassed with no rule broken. **NFR8/G2 — "zero cross-tenant leaks", an MVP hard gate — fails.**
- **Ambiguous composition.** With no key-composition rule, tenant `a` + case `b:c` and tenant `a:b` + case `c` produce the same composed key — so one tenant's preflight reservation and, more seriously, one tenant's AD-3 projection checkpoint can be consumed by another tenant's acknowledgement.
- **Non-.NET comparison.** Kubernetes object names are DNS-1123 lowercase. Tenants `Acme` and `acme` are ordinal-distinct — two tenants to the spine — and collapse to one credential outside .NET.
- **Enabling step:** tenant-creation authority is itself unstated (SEC-19). AD-5's one authorization primitive is a tenant claim, which cannot exist before the tenant does, so every implementation must invent something and nothing says what.

**Fix.** Add an issuance rule to AD-5: a single documented ASCII grammar with bounded length, excluding every character that is a metacharacter, delimiter, or wildcard in Redis ACL patterns, Redis/RediSearch/FalkorDB key and index names, Dapr key and component names, URL path segments, or Kubernetes object names; unambiguous composition (length-prefixed or escaped delimiter reserved out of the grammar) for every composed key. Comparison stays ordinal — *issuance is what makes ordinal comparison sufficient*. State that lifecycle operations are authorized by an operator principal, never a tenant claim.

## C7 — AD-14 is unphased and rebaselines MVP by silence — GENUINE CONFLICT [corroborated]

**Sources:** DRF-01, RUB-08 · **Spine:** `:134-138` · **Upstream:** `prd.md:273`, `:280`, `:1056`, `:1227`; `addendum.md:87-88`, `:138`; `.memlog.md:77`

AD-14 is the only capability-bearing AD with no phase qualifier — AD-11 phases graph population, AD-12 phases MCP and CloudEvent capabilities. It states one create-backfill-verify-switch-retire contract, while the PRD places embedding/schema migration in Phase 2, backend migration in Phase 3, and keeps MVP FR43 at an explicitly acknowledged *degraded* rebuild. Combined with the spine's own preamble ("the rule remains binding"), the literal reading is that staged non-destructive migration binds MVP today.

An MVP story therefore either over-builds staged migration infrastructure the product has not funded, or ships the degraded rebuild and is judged non-conformant — and a reviewer cannot tell which is correct from the spine. AD-14's *Prevents* clause is also unachievable in Phase 1 as shipped, so the rule is violated by design until it is phased.

**This is the one finding in the report that a document edit cannot close.** The product side has already decided (`.memlog.md:77` — "architecture AD-14 is the Phase 2/3 non-disruptive migration target; it does not silently rebaseline MVP FR43"), but `prd.md:1227` names two admissible resolutions and assigns the owner as "Architecture + Jerome". Only a recorded human ratification closes it.

**Decision required — Jerome + Architecture record exactly one:**
**(a)** Phase-qualify AD-14 as an MVP / Phase 2 / Phase 3 contract — the product-decided direction, with replacement Rule text already drafted in `reviews/review-upstream-drift.md` and in SCP §5.1; or
**(b)** Approve an MVP rebaseline, which expands FR43, FR68-FR70, the delivery register, the release gates, and the epic breakdown.

## C8 — G6 is invisible to the spine, and the gap ledger cannot satisfy it

**Source:** DRF-02 · **Spine:** `:288-308`, `:310` · **Upstream:** `prd.md:189`, `:191`, `:213`; `EXPERIENCE.md:44`

G6 is a ratified hard gate: every MVP requirement without current evidence, and every architecture-critical active-foundation gap, must carry evidence or an approved phase exception, each with a named owner and a `sprint-status.yaml` tracker entry. `prd.md:191` — "All six gates are hard gates; there is no soft tier."

The spine's gap table has exactly three columns — Gap / Violated rule / Required convergence — across all rows, with no owner, no evidence path, no verdict, and no exception id anywhere. `:310` closes the section with "G1-G5"; G6 appears nowhere in the document. A reviewer cannot read the spine and determine, for any gap, who owns it, what would close it, or whether an exception was approved. **The architecture side of a hard gate has no artifact.**

**Fix.** Change `:310` to G1-G6. Re-issue the gap table with two added columns — Disposition (owner / evidence path / resolved verdict / approved exception id) and Active-foundation critical (yes/no). The classification column must come first: `prd.md:189` scopes the owner obligation to *architecture-critical active-foundation* gaps, not literally every row, so without it the spine over-scopes G6. Then add a normative decision making gate evidence architecture-governed rather than a table convention — `reviews/review-upstream-drift.md` drafts it as an AD-20, "Treat release gates as evidence contracts".

## C9 — AD-8's enforcement mechanism does not exist, and two of its three statements are present-tense

**Source:** COD-01 · **Spine:** `:102`, `:174`, `:222`, gap row 7

AD-8's Rule: provider SDK references "are confined to composition roots and named internal `Adapters.Redis` or `Adapters.FalkorDb` namespaces **and enforced by architecture tests**." The Consistency Conventions row restates it as settled practice.

Neither is true. `grep -rn "namespace .*Adapters" src/` and `find src -type d -name "Adapters*"` both return **zero**. There is no `NetArchTest` or `ArchUnitNET` dependency anywhere in the repository; every guard is a hand-rolled source-text or reflection test, and none forbids the provider SDKs. Meanwhile **105 files** under `src/Hexalith.Memories.Server/` reference `NRedisStack` / `StackExchange.Redis` / `NFalkorDB` / `IConnectionMultiplexer` — Activities 42, Endpoints 11, Search 7, Workflows 5, and more; 121 files repo-wide under `src/`.

Only the Structural Seed hedges ("**target** internal Adapters.Redis/FalkorDb"). AD-8's Rule and the conventions table do not, and gap row 7 asks to "enforce both boundaries with architecture tests" as future work — three parts of one document disagreeing about whether the boundary is present, target, or pending.

This is the clearest instance of a general problem the code-reality lens quantified: **of 17 invariants the spine leans on architecture tests to enforce, 6 do not exist, 1 is enforced backwards (see H15), and 2 cover only a subset.** An AD enforceable only by a test that does not exist is a weaker invariant than the spine implies.

**Fix.** Make AD-8's Rule and the conventions row future-tense and explicit that no such namespace exists today; add the namespace creation itself to gap row 7's convergence; state the current spread ("105 files under `src/Hexalith.Memories.Server/`") so the size of the obligation is visible.

## C10 — The .NET pin predates a same-week six-CVE security release, unflagged

**Source:** VER-01 · **Spine:** `:196-214`

`global.json:3` pins SDK `10.0.400` (runtime 10.0.11). On **2026-09-08 — one day before the spine was finalized** — Microsoft shipped 10.0.12 / SDK 10.0.401, flagged `"security": true`, fixing six CVEs (CVE-2026-69439, -71328, -69522, -69304, -58649, -69806). The committed Builds gitlink correspondingly pins the whole `Microsoft.AspNetCore.*` family at 10.0.11.

The Stack row carries no annotation, and the caveat paragraph — the spine's designated place for version risk — enumerates exactly three concerns (OpenBao, Redis Stack, FalkorDB) and asserts production is blocked on OpenBao. A reader is entitled to conclude OpenBao is the *only* security blocker. It is not.

Mitigating but not exculpating: `rollForward: latestFeature` means a machine with 10.0.401 installed silently uses it — which makes the recorded pin misleading in the other direction, neither guaranteeing 10.0.400 nor documenting that the resolved version floats. Container base images and the NuGet `Microsoft.AspNetCore.*` pins do **not** roll forward, so the exposure is real for the committed tree.

**Fix.** Add .NET to the caveat's security list. Bump `global.json` to `10.0.401` and land the 10.0.11 → 10.0.12 package bump, or record a time-bounded exception naming the six CVEs on the same footing as the OpenBao exception. State explicitly whether `rollForward` is the intended patch mechanism; if so, the Stack table should say the SDK floats within the 10.0.4xx band.

---

# High findings

## H1 — `/events/ingest` has no stated tenant-authority source
**SEC-04** · `:176`, `:84`, `:78`. The route appears only in the Conventions table. AD-5 enumerates REST/CLI, MCP, and "trusted internal calls" — not pub/sub delivery — while AD-4 tells an implementer to read a tenant off the CloudEvent *for identity*. Taking the envelope's tenant as authoritative violates no Rule while contradicting AD-5's *Prevents*; the Rule text wins. Any workload able to publish to that pubsub component can inject provenance-bearing content into any tenant's corpus — a cross-tenant **write**, whose downstream effect is worse than disclosure: an agent answers from injected content carrying the platform's own provenance markers. AD-12's L1-L3 gate does not close this; it gates product capability, not the endpoint's authorization model. **Fix:** bind the receiving component/topic to exactly one tenant, or require the publishing app ID's grant to contain the envelope's tenant; an envelope tenant the channel does not authorize is rejected.

## H2 — Access telemetry is a second per-tenant datastore with no authorization rule — and PostgreSQL is absent from the spine entirely [corroborated]
**SEC-05, COD-04** · `:152-156`, `:285`, `:226-227`, `:259`. AD-17's Rule names TTL, purge, mapping, recovery, debt, activation, sanitization, and delivery bounds — and contains **no authorization or tenant-scoping clause**. The Capability map governs telemetry by AD-15/AD-17/AD-18 with **AD-5 and AD-6 affirmatively absent**. `memories status telemetry --tenant claims` is shipped; the Server is on the allowlist, the channel is authorized, and AD-17 asks the telemetry service for nothing further — so it answers for whatever tenant the request names, returning who searched tenant `claims`, when, and what was attempted. Being outside AD-6, the store has no per-tenant principal and no partition: one shared credential reads every tenant's records. Separately, `deploy/kubernetes/base/access-telemetry-postgresql.yaml:105` runs a PostgreSQL 18.4 StatefulSet in production that the spine mentions **nowhere** — not in the Stack table, not in the seed's `kubernetes/` gloss, not in the composition sentence. **Fix:** add AD-5 and AD-6 to the telemetry capability row; require telemetry to derive tenant authority independently of the calling workload's channel identity and to be a tenant-isolated resource under AD-6; add PostgreSQL to the Stack table and deployment narrative.

## H3 — No rule derives case authority, while three ADs read as though one exists
**SEC-08** · `:83`, `:84`, `:96`, `:132`. AD-5's *Prevents* lists "case membership" as something that may not authorize, and its Rule derives tenant and subject authority with case absent. AD-7 resolves which case owns a *unit*, not which caller may reach a *case*. AD-13 says "authorize before explaining evidence" without naming subject or granularity. The addendum resolves it ("Tenant claims authorize. Case membership is metadata") — but the addendum is not the architecture, and the spine's own headings point the other way. One implementation lets any tenant principal read any case; another enforces per-case membership; both comply. A principal confined to `general` issuing a tenant-wide search legitimately receives units from `hr-investigations` with full attribution and confidence. If any deployment treats cases as a confidentiality partition — and the PRD's team-scoped-case journeys encourage that — this is a disclosure the architecture neither prevents nor warns about. The reverse is equally live: adding per-case checks silently breaks FR34. **Fix:** state the position once, either way. Silence does not close it.

## H4 — The Kubernetes-secret gap is unbounded, and the gap table understates its NFR8 consequence [corroborated]
**SEC-10, DRF-06** · `:144`, `:214`, `:271`, gap rows `:298`/`:300`; `prd.md:927` vs `:1142`. AD-15 calls direct Kubernetes-secret injection "a temporary alignment gap" with **no owner, no expiry, and no gate** — while the OpenBao version gap two paragraphs away *does* block production qualification. Row `:298` is filed as ordinary convergence work between a fusion row and a floating-image row. What it does not say is what is true while it stands: **one** shared Redis credential with access to every tenant's keyspace, so the addendum's stated primary boundary ("per-tenant Redis ACL users") does not exist and the actual boundary is application-layer key-prefix filtering — which the same addendum calls "a placement tool, not the primary security boundary". Row `:300` compounds it: five coordination paths reach Redis directly, outside the named adapters where server-side principal resolution would apply. G2/NFR8 is an MVP hard gate, and the architecture gates production on an OpenBao patch level but not on the absence of the isolation mechanism that gate names. Upstream half: `prd.md:927` still permits Kubernetes Secrets for "direct pod inputs that DAPR cannot provide", contradicting its own tightened NFR9 — **the spine is right and the PRD must change** (the 2026-09-12 reconciliation landed in NFR9 and the addendum but missed `:927`). **Fix:** bind the exception the way the OpenBao one is bound — named owner, dated expiry, production blocked until per-tenant principals replace it or a dated exception is approved.

## H5 — "Retained opaque access telemetry" is an assertion AD-17 never obliges
**SEC-06** · `:150`, `:155-156`. AD-16 carves retained telemetry out of erasure on the strength of the word *opaque*; AD-17 requires "sanitized product writes" and never defines *sanitized*. One implementation stores a hashed query fingerprint, another the raw query string; both are "sanitized" by their own definition. A search for `"Henderson settlement amount above 2.4M"` survives its tenant's erasure for the configured TTL — and the erasure mapping AD-16 requires guarantees it stays attributable to the erased tenant. **Fix:** define sanitized as content-free and enumerate the permitted fields; a record that cannot be written content-free is not written.

## H6 — "Safely" is undefined, and it selects AD-9's normalization denominator
**ADV-05** · `:114`, `:108`, `:181`. AD-10 hangs the degradation contract on an undefined adjective used twice, and AD-9 then makes that judgment numerically load-bearing. With FalkorDB alive but slow: one adapter's doctrine nulls the graph axis (cannot prove completeness) for a denominator of 0.65; the other keeps it (the call returned) for 1.00. **Every composite score differs by ~1.54× for the same query against the same data at the same instant**, and ordering inverts wherever graph was the discriminator. FR63 makes that number product-visible as relevance confidence. **Fix:** define safe once, server-side, as a property decided by the axis-selection step and not independently by each adapter — scope applied, within limits, no truncation, active generation/epoch; staleness and emptiness are safe and disclosed, unverified scope and exhausted limits are not.

## H7 — Per-axis candidate depth and pagination are unbound relative to rank allocation [corroborated]
**ADV-06, RUB-02** · `:108`, `:120`. AD-9 fixes every step *after* candidates arrive and never fixes how many candidates each axis contributes; the only depth constant governs graph seeding. The spine contains no occurrence of "page", "offset", or "pagination" across all 324 lines, though FR21-FR22 pagination already ships on `GET /api/v1/search`. Fusing full lists and windowing the output is compliant; pushing the caller's offset into each axis (the standard way to hold a p95 budget) is equally compliant — and under the latter, competition ranks restart at 1 inside every page, so **page 2's top entry normalizes to a composite of 1.0, identical to page 1's**. One unit carries two different confidence values depending on which page fetched it, and the concatenation of pages is not the descending-composite list AD-9 mandates. NFR25's golden vectors become unauthorable: page-shaped under one reading, page-independent under the other. The prior gate's four determinism proofs all silently assumed unpaged axis lists. **Fix:** fix a server-owned per-axis candidate depth; apply limit and offset only to the fused output after rank allocation and normalization.

## H8 — `nl` is simultaneously a weighted axis and a post-fusion rerank
**ADV-07** · `:106`, `:108`. AD-9's *Binds* line calls it "natural-language reranking"; its Rule states "NL weight is `0.20`" in the same grammatical slot as the three axes. The PRD Glossary sides with the Binds line ("not a fourth marketing axis"). Read as an axis: denominator 1.20, `nl` appears in Evidence Packet axis health, `--axis nl` is valid, and **a unit ranking only on the NL embedding enters the fused list**. Read as a rerank: denominator 1.00, three axis entries, `--axis nl` is an error, and that unit **can never appear**. The two surfaces return different *sets*, not different orderings — plus a 0.833 score factor and an undisclosed 0.20-weighted influence on public ordering under the rerank reading, which FR19's per-axis breakdown then cannot explain. Default-off and Phase 1.5 gating keep it out of G1/G5, which is why it is high rather than critical. **Fix:** pick one and say so; the review drafts the axis reading.

## H9 — Evidence Packet "equivalent null/omission meaning" is satisfied by two mutually unreadable encodings [corroborated]
**ADV-08, DRF-08** · `:126`; `prd.md:92`, `:571`; `EXPERIENCE.md:145`. AD-12 constrains *meaning*, not *encoding*, and the two are separable. `DefaultIgnoreCondition = WhenWritingNull` (the conventional .NET posture) makes an unavailable axis **absent** and an empty axis **present and empty** — the AD-9 distinction fully preserved. An MCP tool schema emitting every declared property uses explicit `null` and `[]`; absence never occurs — also fully preserved. **One deserializer cannot read both**, so the promised cross-language clients must be written twice against one contract, and NFR25's golden-vector obligation ("one expected document per vector") has no compliant single answer. Under the omitting encoding, absence is additionally overloaded across an unrecognized axis, an unavailable axis, and an absent omitted-detail handle. Separately, the PRD fixes an eight-value packet state vocabulary (`complete/partial/weak/empty/stale/degraded/unauthorized/pendingExpansion`) that the spine never mentions, nor whether the state is one value or a set. **Fix:** make equivalence apply to the serialized document — every element always present, explicit `null` never omission, no null-omitting serialization for evidence-bearing types, one byte-comparable golden vector per case; name the versioned state vocabulary.

## H10 — `Contracts.V1` has no owner, so two additive changes collide unresolvably
**ADV-09** · `:126`, `:174`, `:281`. Both a tenant-wide-attribution unit and an explain unit are *required* by AD-12's own element list to add to the same envelope, and both naturally add a property named `axes` — one a map of axis→health, one an array of contributions. Neither removes a field, neither renames a deliberate wire name; both are purely additive, the only evolution constraint AD-12 states. The second to merge either overwrites the first's shape or must rename — **and AD-12 forbids the rename outside a versioned break**, so the additive rule can trap the contract in a state whose only compliant exits are shipping a collision or opening V2. The addendum already records a live instance of the class (`axis` vs `axes` between the CLI table and the MCP schema). The spine's central device is naming an owner — AD-6 for tenant resources, AD-8 for adapters, AD-17 for telemetry, AD-19 for packaging — and for the one artifact AD-12 exists to keep singular it supplies only a *location*. **Fix:** name an owning component with change-approval authority and require a wire name, shape, and nullability to be reserved in a versioned name register before use.

## H11 — The Direct Redis Exception Registry's amendment process permits two lock stores for one resource
**ADV-10** · `:102`, `:186-194`, gap row `:300`. Gap row `:300` offers a genuine either/or ("move coordination to Dapr state **or** adopt an explicit `AD-n` exception"), so one epic can move the derived-store fence to Dapr state while another authors an `AD-20` registry row for import leases using `SET NX PX`. Both follow the amendment process exactly. Neither primitive can observe the other: repair holds a Dapr fence, import holds a Redis lease, both proceed, and repair deletes index entries the import just wrote — both reporting success. Compounding: the registry's **only precedent row is fail-open**, so a new author's well-precedented choice for a *lease* admits two concurrent importers during a Redis blip; the spine never states that mutual-exclusion primitives must fail closed while admission optimizations may fail open, though that distinction is the entire reason the existing row is safe. And nothing owns the Redis key namespace, so two rows can claim one prefix. **Fix:** require a new row to name the single coordination store for the protected resource and migrate any existing guard; mutual-exclusion primitives fail closed; every row declares a reserved key prefix.

## H12 — The binding header stops at FR74 / NFR36 / G5
**DRF-03** · `:11-15`; `prd.md:40`, `:1228`. A whole-file grep returns **zero** occurrences of `FR75`, `NFR37`, or `G6`. For FR75 specifically the header is the *only* gap: AD-4 already states operation-scoped token identity, CloudEvent identity with ordinal comparison, and durable suppression, and AD-3 supplies the tuple for one idempotent projection outcome — only the non-suppression of a legitimate *new-epoch* reprojection is implicit rather than stated. NFR37 and G6 are different (H13, C8): a header refresh alone would assert coverage that does not exist. Note the qualification the sprint-change proposal misses — **there is no trace map to extend.** The spine has no FR→AD or gate→AD table; traceability exists only as per-AD `Binds:` lines and the Capability map. **Fix:** update the header to FR1-FR75 / NFR1-NFR37 / G1-G6, add FR75 to the `Binds:` of AD-2, AD-3, AD-4, and add capability-map rows.

## H13 — NFR37 has no architectural home anywhere in the spine
**DRF-04** · `prd.md:1215`; `EXPERIENCE.md:71`, `:255-264`. Case-insensitive greps for `exit code`, `accessib`, `keyboard`, `WCAG`, `reading order`, and `color` return **no matches** in the spine. Unowned obligations include: the stable reading order scope → result → sources → reasoning/state → recovery; a text label for every state, axis, score meaning, omission, progress stage, and recovery with colour/glyph/motion supplementary only; bounded wrappable output and a linear alternative for wide tables; deterministic redirected output free of terminal control sequences; durable progress/failure lines; explicit cancellation and timeout; no second "created" line on duplicate delivery (the surface half of FR75); keyboard-only operability; suppression of secrets **and restricted identifiers**; and semantic parity across human, table, JSON, stderr, and exit code — with `EXPERIENCE.md:71` pinning exit codes `0/1/2/4/130` and the `{schemaVersion, command, data|error}` envelope. **An exit-code map, a JSON envelope shape, and a cross-format parity guarantee are public contract surface — exactly what AD-12 exists to own** — yet AD-12's Rule reaches only Evidence Packet semantics. **Fix:** extend AD-12 with an operator-observable output contract sentence and add an "Active CLI output contract" Consistency Convention row.

## H14 — The "Web is a non-runnable RCL" gap row is factually wrong [corroborated]
**DRF-05, COD-02** · `:307`, `:230`. A runnable conformance host exists and is CI-gated: `tests/Hexalith.Memories.Web.SpecimenHost/…csproj:1` is `Microsoft.NET.Sdk.Web`, references the Web RCL, and `Program.cs:10-27` builds a real `WebApplication` with interactive server components and `app.Run()`, serving `/__memories/specimens/{SurfaceSlug}`. Playwright launches it on `:5177` across four spec files as CI job `web-e2e-specimen`. It landed **2026-07-06** — two months before the spine was finalized — and `prd.md:724`, `EXPERIENCE.md:42`, and the 2026-09-12 reconciliation all call it implemented. Since `src/`, `tests/`, and CI are byte-identical to the spine commit, this was falsifiable on the day. **The spine understates its own project's state, and a team reading this row would rebuild something that exists.** (The likely source is a stale comment at `src/Hexalith.Memories.Web/…csproj:4-6` saying "has no runnable host yet" — source-side debt that should not be re-cited as evidence.) **Fix:** replace the row with what actually remains — the host lives under `tests/`, no product host consumes the RCL, product Web stays future.

## H15 — Gap row 8 misstates fusion, and passing tests lock the anti-AD-9 behavior
**COD-03** · gap row `:301`; `Search/FusionEngine.cs`. Three of four sub-requirements are indeed violated, but **competition ranks 1,1,3 ARE implemented** (`:144-150`) with the correct `1/(10+rank)` (`:246`), so the row as phrased is imprecise. More importantly it treats this as a pure omission. It is not: `FusionEngine.cs:253` deliberately encodes `double.IsNaN(left) && double.IsNaN(right)` as a **tie**, and `tests/…/Search/FusionEngineTests.cs:358-370` and `:373-385` are **passing tests asserting that a NaN or Infinity input is retained and ranked #1** — the exact opposite of AD-9's "drop non-finite raw scores" and "ties only when finite `Double.Equals`". Converging therefore requires deleting or inverting green tests, which is materially different work from adding a missing preprocessing pass. This is the one invariant in the repository enforced *backwards*. **Fix:** restate the row precisely and add "retire or invert `FusionEngineTests.Fuse_Bm25NaN_*` / `Fuse_Bm25Infinity_*`" to its convergence.

## H16 — The OpenBao remediation target was already stale at authoring
**VER-02** · `:214`, gap row `:306`. The substance verifies: OpenBao 2.6.0 is confirmed affected by GHSA-rh46-vc3j-w2w3 (Critical, CVSS 9.2, patched 2.6.2), and calling production blocked is proportionate. The **chart** half is not: the spine names "the `0.28.6` chart" as the assessment target, but 0.28.6 shipped 2026-07-22 and the 0.29 line opened 2026-08-10, with **0.29.4 shipping 2026-09-03 — six days before the spine was finalized**. The security *judgment* was researched; the *remediation target* was not re-checked, and re-pinning to 0.28.6 would land two minor lines behind on arrival. An open upstream dependency-vulnerability tracker against the 2.6.x branch (GO-2026-5970) should be swept into the same requalification. **Fix:** restate as "upgrade to OpenBao ≥ 2.6.2 and assess the current chart line (0.29.x, presently 0.29.4)".

## H17 — The EventStore source-lane gitlink resolves to nothing [corroborated]
**VER-03, RUB-07, COD-05** · `:205`. The Stack table pins `b1c00a79d1d34aa7ba3f58046a7844e8b3d57fd6`. `git ls-tree HEAD references/Hexalith.EventStore` returns `6b0247ac…`; the worktree is at `a568af4e…`. **Three distinct SHAs; the spine's value is neither.** Unlike the package drifts in M-tier findings, this cannot be explained by the dirty-worktree delta — the value appears nowhere in the history the code lens traced: `4e564e43` (09-09) moved it *to* `b1c00a79`, then `3644ef63` — **the very commit that published the spine** — moved it to `2dd7ebfb`, then `e18f51a9` (09-12) to `6b0247ac`. So the pin was superseded by its own publishing commit and is now two moves behind. A source-lane SHA whose entire purpose is byte-exact reproducibility is worthless if it does not resolve, and AD-19's two release lanes can be evidenced at different source revisions — the exact substitution AD-19's *Prevents* forbids. **Fix:** re-derive with `git ls-tree HEAD references/Hexalith.EventStore`, state which side is authoritative, and add gitlink/package-version correspondence to AD-19's lane evidence.

---

# Medium and low findings

Forty-one medium and low findings are recorded in full in the six review files. Grouped by theme:

**Undefined terms that select behavior** — "safe" also drives readiness reporting and AD-17's unstated "approved delivery bound" (RUB-10); "normalized subject claims" in AD-13 names no claim, no normalization form, and no comparison rule, directly contradicting AD-5's no-normalization posture two rules earlier (RUB-03); "approved low-cardinality tenant representations" has no injectivity rule, so two tenants can share a metric label and an isolation incident becomes unattributable (SEC-17).

**Missing dimensions the altitude owns** — backup/restore of derived stores in normal operations has no rule at all, and restoring a snapshot behind the checkpoints leaves units permanently reported `Indexed` because AD-3's monotonicity prevents healing (RUB-05); the three-operation recovery taxonomy lives only in a gap-table cell, not in a Rule; test tiers are never enumerated, dropping the PRD's no-Docker unit-test contract (RUB-04); there is no environment taxonomy or parity invariant beyond local AppHost and production (RUB-12); developer experience, the timed G3 quickstart path, `samples/`, and `docs/` have no home (RUB-13); cost and per-unit sizing appear nowhere despite AD-18 binding NFR14 (RUB-14); the error-code catalogue has no declared owner, uniqueness rule, or immutability guarantee across three mappings (RUB-09).

**Authorization and disclosure gaps** — mandatory "omitted-detail handles" are an unconstrained existence oracle (SEC-12); no rule requires not-found and not-authorized to be indistinguishable, so an "actionable suggestion" naming the owner case turns opaque IDs into a probe oracle (SEC-13); background, repair, replay, and migration work has no tenant-authority source and can write projections for an erased tenant (SEC-14); nothing obliges revocation of a superseded credential, and no rule bounds a connection's lifetime against its credential's (SEC-15); the L1-L3 MCP gate disables publication, not in-cluster reachability (SEC-16, softened by COD-08 — there is no ingress to disable; the real residue is `replicas: 2` ungated in base).

**Upstream drift not yet landed** — AD-18 does not carry NFR13's admission and queue-cap semantics (DRF-07); freshness vocabulary and thresholds have no owner (DRF-09); the Stack table omits the embedding provider/model/dimension that AD-14 explicitly binds (DRF-11); FR49/FR51/FR52 graph data-accuracy invariants have no AD home (DRF-12); NFR32/NFR35 sit inside the bound range with zero coverage (DRF-13); the spine still lists superseded `architecture.md` and a stale traceability companion as active sources (DRF-14); NFR11, NFR31/G3, NFR21, and the FalkorDB AGPL pin duty are uncovered (DRF-15 to DRF-18); the spine exposes no requirement→AD surface, which is why `epics.md` contains zero `AD-n` citations (DRF-20).

**Accuracy and completeness of record** — FalkorDB `4.12.0` versus upstream `v4.20.4` is eight minor lines on a stateful graph store, not "qualified-but-behind" (VER-04); a production-bound preview (CommunityToolkit Aspire Dapr — with **no stable 13.5.x existing upstream at all**) and an RC (Fluent UI 5.0.0-rc.5, no GA) get no verdict (VER-05); the Dapr CI runtime is two patch releases behind, and 1.18.3 carries a CloudEvent trace-field fix directly relevant to this architecture (VER-06); four Stack rows are true of `HEAD` but false of the dirty working tree (VER-07); Redis Stack's EOL claim is if anything understated — `7.4.0-v8` is the terminal tag, unrebuilt since 2025-11-03, with a hard 2026-11-30 EOL date (VER-08); two cross-reference contradictions, app tokens scoped to "local channels" against AD-5 and "only the final fusion tie-break" against AD-9's two intermediate orderings (RUB-06); the Deferred hosted-Web row hands authorization to a future host, which AD-5 forbids (RUB-11); the Structural Seed omits `tools/` projects — one of which `ProjectReference`s Server and takes a direct `StackExchange.Redis` dependency outside the release-inventory gate (COD-06); the routes convention names one exception where the code has three families (COD-07); gap row 5's convergence asks for separate Falkor graphs that already exist (COD-09); "tests state tier and boundary" is aspirational at 118 traited classes with no guard (COD-10); `Client*/` "Consumer ports" names an abstraction that does not exist (COD-11); gap row 2 omits the more dangerous half — status is never persisted and `CaseService.cs:995-997` **defaults it to `Indexed`**, so a `GET` reports `Indexed` from syntactic-hash presence alone (COD-12); AD-19's pinning obligation names two container-default surfaces without saying which owns the qualified version (ADV-11).

---

# The sprint-change proposal's three assertions

`sprint-change-proposal-2026-09-12.md` (untracked draft) makes three claims about this spine. Each was independently verified against the actual spine and PRD text.

| # | Assertion | Verdict | Note |
| --- | --- | --- | --- |
| A1 | AD-14 is unphased and conflicts with the approved MVP/Phase 2/Phase 3 migration boundary | **CONFIRMED** | See C7. Needs a human ratification, not an edit. |
| A2 | The binding header and trace maps must extend through FR75/NFR37/G6 | **CONFIRMED, qualified** | Two qualifications the proposal misses: there is **no trace map to extend** (no FR→AD or gate→AD table exists — only per-AD `Binds:` lines and the capability map), and **FR75's substance is already fully adopted** in AD-4 + AD-3, so for FR75 the header is the only gap. NFR37 and G6 need new normative text, not an ID refresh. See H12, H13, C8. |
| A3 | Every alignment gap must acquire an owner, evidence path, resolved verdict, or approved phase exception | **CONFIRMED, qualified** | The table genuinely has none of those columns. But G6 scopes the duty to *architecture-critical active-foundation* gaps, not literally every row — the proposal's own EX-OP-01 concedes this — so a **classification column must precede the owner column** or the spine over-scopes G6. See C8. |

---

# Recommended disposition

**One human decision blocks the rest: C7 (AD-14 phasing).** Everything else is an authoring change.

**Before any affected epic is sprint-selected.** C3, C2, C4, C5 are ownership and ordering holes that two independently staffed epics hit on contact — the projection coordinator, the erasure workflow, the internal-authorization path, and tenant-wide graph search. C1 and C6 defeat MVP hard gates as written and are AD text changes, not new decisions.

**Before the spine is cited as current authority.** C7 (decision), C8 (G6 + ledger columns), H12/H13 (binding header and NFR37's home).

**Corrections of record, applicable immediately.** C9, C10, H14, H15, H16, H17 — plus the medium-tier Stack and Seed completeness items. These are the cheapest and they are what currently make a `final` document unsafe to quote.

**Independently blocking G5.** H6, H7, H8, and C5(b) each defeat NFR25's cross-surface determinism regardless of implementation quality; H7 and C5(b) additionally make golden-vector authorship impossible, so G5's evidence cannot be produced until they are decided.

**Close alongside the contract work, not after it.** H9 and H10 — H10's failure mode becomes unresolvable once both FR34 and FR19 changes have shipped.

Every proposed amendment is drafted as replacement Rule text in the six review files; the adversarial and security lenses supply quotable sentences for all of theirs.

**Next step:** run `/bmad-architecture update` against `architecture-memories-2026-09-09`, resuming from its `.memlog.md`. Keep AD IDs stable — amend Rules in place, add `AD-20` for the gate-evidence contract, never renumber. Where an amendment overrides a source input (notably H4's `prd.md:927`), offer the upstream correction in the same pass so the PRD and spine do not diverge again.
