# Architecture Spine Review — Rubric Walker

**Verdict: PASS-WITH-FINDINGS** — the spine is a genuine spine (it decides the hard cross-unit questions, it is enforceable in most places, and its gap ledger is honest), but two high-severity invariants are incomplete at exactly the seams they exist to close, and four dimensions of the operational envelope are decided only by implication.

- **Target:** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (324 lines, `status: final`, altitude: feature)
- **Reviewed:** 2026-09-12, under the Validate intent (read-only)
- **Lens:** rubric walker — good-spine checklist, semantic half. The deterministic linter (0 findings) owns the mechanical half; separate lenses own the deep web-currency, brownfield-code, and PRD-drift checks.
- **Counts:** 0 critical, 2 high, 7 medium, 4 low.

---

## Dimension 1 — Does it fix the real divergence points for epics/implementation units, and miss none?

**Verdict: PASS-WITH-FINDINGS.**

The spine identifies the divergence points that matter for this system and mostly nails them. The set that a hybrid-retrieval, event-sourced, multi-tenant platform actually diverges on — projection completion identity (AD-3), duplicate suppression identity (AD-4), authority derivation (AD-5), case scoping across both query shapes (AD-7), adapter ownership (AD-8), fusion arithmetic (AD-9), degradation vs. ingestion gating (AD-10/AD-3), evidence envelope equivalence (AD-12), erasure completeness (AD-16) — is present and, in most cases, specified to the level where two teams cannot silently disagree. The alignment-gap ledger (`:288-308`) is unusually strong: it names the divergences that already exist in code as obligations rather than pretending they are decisions, and the 2026-09-12 sprint change proposal derives its story set almost row-for-row from it, which is direct evidence the ledger is at the right granularity.

Three divergence points are missed:

1. **Projection write ordering** — AD-3 orders *acknowledgements*, not the *writes* they acknowledge (RUB-01).
2. **Per-axis candidate depth** — AD-9 fixes the arithmetic of fusion but not its inputs (RUB-02).
3. **The error-code catalogue** — three surfaces must map the same code and nothing says where codes are declared or that they are immutable (RUB-09).

A fourth, smaller one: nothing fixes write-side concurrency on a single MemoryUnit (no expected-version / conflict contract anywhere in the spine; `ExpectedVersion` does not appear in `src/` or in the `Hexalith.EventStore` submodule either). AD-3's monotonic checkpoint absorbs most of the risk, so I fold this into RUB-01's fix rather than raising it separately.

## Dimension 2 — Is every Rule enforceable, and does it actually prevent its stated "Prevents"?

**Verdict: PASS-WITH-FINDINGS.**

Rule quality here is well above average: AD-3, AD-4, AD-5, AD-7, AD-9, AD-12, AD-16 and AD-19 are written as testable constraints, several of them to golden-vector precision. Ordinal comparison, `1/(10+rank)`, competition ranks `1,1,3`, `Double.Equals` with no epsilon, depth-at-most-two seeding, and the finite exception registry are all falsifiable by a test that could be written today.

The exceptions:

- **AD-9 (`:108`)** — the most precisely written rule in the document does not prevent its own "Prevents" (ranking implementations diverging), because per-axis candidate depth is unbound (RUB-02).
- **AD-10 (`:114`)** — "respond safely" / "a safe response" is the load-bearing term and is undefined (RUB-10).
- **AD-13 (`:132`)** — "normalized subject claims" is undefined while AD-5 explicitly forbids post-issuance normalization for the adjacent identifiers (RUB-03).
- **AD-14 (`:138`)** — enforceable, but unphased against a PRD that places its content in Phase 2/3, so conformance is currently undecidable for an MVP story (RUB-08).
- **AD-17 (`:156`)** — half the rule is governance assignment ("Platform Operations owns…", "dated accepted debt"). That is legitimate at this altitude, but "within the approved delivery bound" names a bound with no value, no location, and no owner, unlike AD-18 which explicitly points at "the PRD and validated configuration" (`:162`). PRD NFR34 has the same wording, so the bound exists nowhere. Enough to make "surface degradation when exceeded" unfalsifiable; recorded under RUB-10's family but not separately raised, since the fix is one cross-reference.
- **AD-6 (`:90`)** — "explicit, idempotent, observable, bounded" is adjective-stacking, but the second half ("Redis uses a per-tenant ACL principal resolved server-side; FalkorDB selects a separate tenant database/graph… never create infrastructure implicitly") is concrete and does the preventing. Sound as written.

## Dimension 3 — Could anything under "Deferred" let two independently built units diverge?

**Verdict: PASS-WITH-FINDINGS (one low).**

Eight of the nine Deferred rows (`:312-325`) are correctly deferred: each names a revisit condition, and — the part that usually goes wrong — each says what remains binding in the meantime. "Cross-case references" is explicitly forbidden by AD-7 until adopted (`:96`, `:318`); "Alternate search or graph backend" keeps adapter, isolation, migration and Evidence Packet rules binding (`:322`). That is how a Deferred row should be written.

The one problem is the hosted-Web row (`:319`), which assigns *authorization* to a future host — a live contradiction of AD-5 rather than a deferral (RUB-11).

Two rows I checked and cleared: the production actor/workflow state store (`:316`) does not create divergence because AD-8 forces all coordination through the portable Dapr state API, though the row would be stronger if it recorded AD-3's ETag/CAS requirement as a selection constraint; and "Generated OpenAPI" (`:320`) is documentation, not semantics, with Contracts.V1 already authoritative under AD-12.

## Dimension 4 — Is named technology verified-current?

**Verdict: PASS (flagged, not duplicated).**

The Stack section is handled better than most: `:214` explicitly states the pins are repository facts rather than currency claims, and names the three known problems (OpenBao `2.6.0`/chart `0.28.5` below the security-fixed line, Redis Stack `7.4.0-v8` past end of maintenance, FalkorDB `4.12.0` qualified-but-behind), each with a matching gap or Deferred row. I did not re-run the upstream currency check — the version lens owns it.

Two things for that lens:

- The `Hexalith.EventStore source lane gitlink` (`:205`) is not merely possibly-stale, it is verifiably stale in this working copy: the spine pins `b1c00a79…`, the superproject's committed gitlink is `6b0247ac…` (commit `e18f51a9`, 2026-09-12) and the checked-out submodule is `a568af4e…`. `b1c00a79…` was accurate on 2026-09-09 and has moved twice since. Raised as RUB-07 because the rubric problem is structural (a decaying value in a table of invariants with no refresh owner), not the version number itself.
- Two pins are prerelease — CommunityToolkit Aspire Dapr hosting `13.5.0-preview.1.260825-0345` (`:202`) and Fluent UI Blazor `5.0.0-rc.5-26219.1` (`:211`) — and the Stack prose flags neither, while it does flag three stable-but-behind pins. AD-19 requires pinning, not stability, so no rule is violated; whether a Production-qualified surface may depend on a preview package is a currency question I am leaving to that lens.

## Dimension 5 — Does it ratify rather than contradict the brownfield codebase?

**Verdict: PASS (flagged, not duplicated).**

This is the spine's strongest dimension. The paradigm section states the ratification policy outright (`:52`: "Where current code differs, the rule remains binding and the difference is implementation work, not a competing decision"), the Structural Seed describes the repository as it actually is rather than as it should be (`:221` mixed EventStore assembly with a pure `Domain/` subtree; `:224` Redis as compatibility facade; `:230` Web as a non-runnable RCL), and fifteen alignment gaps carry a violated-rule reference and a convergence obligation. Nothing in the spine claims a capability the ledger does not already contradict where it needs to.

I did not re-verify the gap rows against code — that lens owns it. The only structural note is RUB-05: the three-way recovery distinction (authoritative replay / export restore / Redis-input repair) exists *only* in gap row `:296`, i.e. it is recorded as an implementation obligation but never adopted as a rule.

## Dimension 6 — Does it cover the driving spec's capabilities?

**Verdict: PASS-WITH-FINDINGS (flagged, not duplicated).**

The Capability → Architecture Map (`:274-286`) covers every PRD capability area, and each row's governing ADs are plausible. Per the known context, I am not re-deriving the FR/NFR drift; I note only where it lands on dimensions I own:

- Frontmatter binds `FR1-FR74 / NFR1-NFR36 / G1-G5` (`:11-15`) against a PRD that now carries FR75, NFR37 and G6. This is a dimension-7 problem as much as a drift problem: NFR37 makes the CLI's semantic parity across human/table/JSON/stderr/exit-code forms a *contract*, and no AD or convention row owns exit codes or output-form equivalence today (the Configuration row at `:179` covers only precedence). SCP §5.2 proposes binding NFR37 to AD-12 and AD-19, which is the right home.
- AD-14's missing phase qualification is a rubric-2 defect in its own right, not only drift: as written, an MVP story cannot tell whether a degraded tenant rebuild conforms (RUB-08).

## Dimension 7 — Is every dimension the altitude owns decided, deferred, or an open question?

**Verdict: PASS-WITH-FINDINGS — the operational envelope is the weakest half of the document.**

Walking the envelope explicitly:

| Envelope dimension | State | Note |
| --- | --- | --- |
| Deployment | Decided | `:259`, `:271`; local AppHost vs Kubernetes vs Production, with the missing EventStore workload recorded as a gap (`:303`). |
| Infra/provider strategy | Decided | AD-8 adapters, AD-14 strategy ports, Deferred backend replacement. |
| Operations | Decided | AD-17 ownership, AD-18 quotas, Health convention (`:181`). |
| CI/CD | Decided | AD-19 two evidence lanes; CI in Binds (`:166`) and the capability map (`:286`). |
| Testing strategy | **Partial** | `:183` requires tests to "state tier and boundary" but never defines the tiers (RUB-04). |
| Cost | **Silent** | AD-18 Binds NFR14 but its Rule has no sizing clause (RUB-14). |
| Developer experience | **Silent** | No AD binds NFR30/NFR31; `samples/` and `docs/` absent from the seed (RUB-13). |
| Local/dev-prod parity | **Partial** | Discussed at `:259` and via the floating-image gap (`:305`), but there is no environment taxonomy and no parity invariant (RUB-12). |
| Disaster recovery | **Partial** | Replay is decided (AD-2/AD-3), RPO/RTO correctly deferred (`:323`), but backup/restore of derived stores has no rule (RUB-05). |

No dimension is wholly unowned in the way that usually earns a critical, but "testing strategy" and "backup/restore" are the two where a story team would have to invent a contract, and both interact with adopted invariants (AD-19's evidence lanes; AD-3's checkpoints).

## Dimension 8 — Structural balance: minimal seed, no bloat, nothing load-bearing missing

**Verdict: PASS.**

The seed (`:216-239`) is a directory listing plus one clause per entry, and every clause is a boundary statement rather than a design (`AppHost — never a consumer dependency`, `Mcp — asset presence is not activation`). That is the right shape; it is structure code already owns, annotated with the invariant that would otherwise be lost. Minor duplication — those two clauses restate AD-1 and AD-12 — which creates two places to update but no ambiguity.

On bloat: the obvious candidate is AD-9's rule, which reads like an algorithm specification. It earns its length. Cross-surface score and order equality is a real invariant (G1 is measured on it, and the Evidence Packet publishes per-axis contribution), and the fourth adversarial pass on 2026-09-09 shows exactly how much of it was needed to stop implementations diverging. Nothing else is over-specified; AD-17 is the only rule carrying material that is governance rather than architecture, and it is short.

On missing load-bearing content, see RUB-01 (projection write ordering), RUB-02 (retrieval depth), RUB-05 (recovery taxonomy), RUB-09 (error catalogue).

## Dimension 9 — AD IDs, Binds/Prevents/Rule quality, cross-reference consistency

**Verdict: PASS-WITH-FINDINGS (two contradictions, two uncovered Binds terms).**

IDs AD-1…AD-19 are stable, sequential and referenced consistently by the Capability Map, Operational Boundaries and gap ledger; every citation I checked resolves to an AD that actually governs the cited concern. Two contradictions and two coverage gaps:

- **Operational Boundaries Security row vs. AD-5** — `:266` scopes app tokens to "local channels"; AD-5 (`:84`) and the deployment prose (`:259`) require app-token channel protection on all trusted internal calls (RUB-06).
- **Identifiers convention vs. AD-9** — `:175` says "only the final fusion tie-break imposes ascending ordinal order", but AD-9 (`:108`) imposes ascending ordinal case ID at two *intermediate* stages of the tenant-wide graph merge. The convention as written licenses an implementation to skip the intermediate ordering, which is precisely what the third adversarial pass added (RUB-06 covers both; recorded separately in the findings table).
- **Binds without a Rule clause** — AD-18 Binds NFR14 (per-unit memory footprint predictable and documented) and its Rule never mentions sizing; AD-13 Binds "correlation" and its Rule never mentions correlation or causation identifiers, which AD-11 depends on for `caused_by`/`correlated_with` (RUB-14).

Everything else lines up: the V1 status wire contract (`:177`) agrees with AD-3's `Indexing`/`projecting` mapping; the Health row (`:181`) agrees with AD-10; the Direct Redis Exception Registry (`:186-194`) agrees with AD-8 and its violations are ledgered at `:300`; the Deferred upgrade row agrees with the Stack prose.

---

## Findings

### RUB-01 — high — `ARCHITECTURE-SPINE.md:72`, `:78`

**What is wrong.** AD-3 makes *acknowledgements* monotonic ("advances per-axis checkpoints monotonically with ETag/CAS and rejects stale acknowledgements") and AD-4 makes each projection "an idempotent upsert for AD-3's tuple". Neither orders the projection **write** itself, and "idempotent upsert for AD-3's tuple" is ambiguous about the store key: if the Redis/FalkorDB document is keyed by `MemoryUnitId`, a late write from an older attempt overwrites newer projected content; if it is keyed by the full tuple, stale documents for superseded versions remain searchable. Both readings conform to the rule as written, and they are mutually incompatible.

**Divergence permitted.** Activity A (sourceVersion N) is retried after activity B (N+1) has completed and the checkpoint has advanced. The coordinator correctly rejects A's stale acknowledgement, so status stays `Indexed` at N+1 — while the syntactic/vector index now holds N's content, or holds both. The same hole lets a deleted or re-ingested unit reappear in a projection after its delete/re-ingest revision was acknowledged. Two teams implementing the syntactic and vector axes will resolve this differently, and the difference is invisible in the status contract that AD-3 exists to protect.

**Fix.** Extend AD-3's Rule: derived stores hold exactly one current document per `(tenantId, caseId, MemoryUnitId)` carrying the tuple it was projected from; every projection write is conditional on that stored tuple and is a no-op when the incoming tuple is older (version fencing at the write, not only at the acknowledgement); deletion writes a fenced tombstone under the same rule. While there, state the write-side concurrency contract for concurrent mutations of one unit (expected-version append and the conflict outcome) — no artifact in the repository defines one today.

### RUB-02 — high — `ARCHITECTURE-SPINE.md:108`

**What is wrong.** AD-9 fixes every step of fusion *after* candidates arrive — preprocessing, tie semantics, competition ranks, the `1/(10+rank)` constant, the normalization denominator, the total order — but never fixes how many candidates each axis contributes. The only depth constant in the rule ("top five syntactic and top five semantic hits") governs graph seeding, not retrieval. AD-11's "server-owned depth/result/time limits" is scoped to graph traversal. Nothing binds the requested result count or pagination either, although FR21–FR22 pagination is already shipped on `GET /api/v1/search`.

**Divergence permitted.** Two conforming implementations — or the same implementation before and after a refactor — fetch top-20 vs. top-100 syntactic candidates and produce different composite result sets *and* different ranks for the identical corpus and query, because rank position feeds `1/(10+rank)` and the truncation point decides which units are ranked at all. This defeats the golden-vector tests AD-9 exists to enable (gap row `:301` calls for exactly those tests) and makes the G1 benchmark non-reproducible across runs of different builds — the gate the whole rule serves.

**Fix.** Add to AD-9: a fixed, configuration-validated per-axis candidate depth (a named default, server-owned, identical across surfaces), a statement that composite normalization and golden vectors are defined at that depth, and page/limit semantics expressed over AD-9's deterministic total order so pagination cannot reorder results.

### RUB-03 — medium — `ARCHITECTURE-SPINE.md:132` (with `:84`, `:297`)

**What is wrong.** AD-13 requires binding actor provenance to "normalized subject claims" without naming the claim, the normalization form, or the comparison rule — while AD-5, two rules earlier, goes out of its way to forbid post-issuance normalization for tenant and case IDs and to mandate `StringComparison.Ordinal`. The gap row that remediates caller-controlled `IngestedBy` (`:297`) inherits the same undefined term.

**Divergence permitted.** The REST path records `sub`, the MCP path records `oid` or `email`, one lowercases and another does not — for the same human. Provenance equality then fails across surfaces, actor attribution in the case activity feed splits one principal into several, and any later authorization or filtering keyed on the recorded actor is unsound. Cross-issuer collisions are possible if the subject is recorded without its issuer.

**Fix.** Name the actor identity explicitly (issuer + subject claim, or the equivalent pair the platform standardizes on), state that normalization happens exactly once at the authentication boundary and specify the form, and state that recorded provenance is compared ordinally thereafter — mirroring AD-5's wording so the two rules cannot be read as contradicting each other.

### RUB-04 — medium — `ARCHITECTURE-SPINE.md:183` (with `:168`)

**What is wrong.** The Testing convention requires tests to "state tier and boundary" but the spine never enumerates the tiers or their constraints, and AD-19's four evidence artifacts ("restore, build, contract, and integration") are never mapped onto them. The PRD's Test Infrastructure Strategy carries a hard constraint the spine drops entirely: unit tests must run with a mocked `DaprClient` and no sidecar, so contributors can run them without Docker — a Journey 10 / contributor-onboarding requirement, not a preference.

**Divergence permitted.** Story teams classify the same test differently and land Docker-dependent tests in the unit tier, silently breaking the no-Docker contributor path; and because the tier vocabulary is open, AD-19's "contract lane" and "integration lane" evidence cannot be checked for completeness — any suite can claim to be either.

**Fix.** Enumerate the tiers in the convention row with their binding constraints (unit: no sidecar/no container; contract: serialization and route/error-envelope round-trips, cross-surface golden vectors; integration: Aspire/testcontainers with the observable end states already listed; E2E), and state which tier produces each AD-19 evidence artifact per lane.

### RUB-05 — medium — `ARCHITECTURE-SPINE.md:150`, `:265`, `:296`

**What is wrong.** Backup and restore appear only inside AD-16's erasure clause (quarantine unreadable payloads) and in the Deferred upgrade row. No rule governs restoring a derived store in normal operations, and the three distinct recovery operations — authoritative EventStore replay, application export restore, Redis-input consistency repair — are distinguished only in the *gap ledger* (`:296`), which is an implementation obligation table, not an adopted decision. Epic 26 (operational backup/restore) therefore has no binding contract.

**Divergence permitted.** An operator restores a Redis or FalkorDB snapshot taken before the current checkpoints. Projections rewind while the Dapr-state checkpoints stay ahead, so AD-3's monotonic protocol actively prevents recovery: the units are missing from the index but permanently reported `Indexed`, and no rule requires the restore to invalidate or rebuild the affected checkpoints. Independently, one team may treat an export restore as a legitimate source of truth for projections while another treats replay as the only authority — the exact confusion the addendum warns about.

**Fix.** Promote the three-operation taxonomy into a Rule (AD-2 or AD-3): authoritative replay is the only rebuild path that may set projection truth; derived-store restore is an availability optimization that must invalidate the affected checkpoints and re-verify through AD-3's protocol; export restore is an application-level import subject to normal ingestion rules. Keep AD-16's erasure quarantine as the overriding constraint on all three.

### RUB-06 — medium — `ARCHITECTURE-SPINE.md:266` (with `:84`, `:259`) and `:175` (with `:108`)

**What is wrong.** Two cross-reference contradictions:

1. The Operational Boundaries Security row says "app tokens protect **local** channels". AD-5 (`:84`) requires trusted internal calls to have deny-by-default Dapr mTLS/access-control policy *and* app-token channel protection with no local qualifier, and the deployment prose (`:259`) says internal traffic uses channel tokens generally.
2. The Identifiers convention says "**only** the final fusion tie-break imposes ascending ordinal order", but AD-9 (`:108`) imposes ascending ordinal case ID twice at intermediate stages of the tenant-wide graph merge.

**Divergence permitted.** (1) A Kubernetes deployment omits app-token channel protection, citing the boundary table, and still passes an architecture conformance read — removing one of the two layers AD-5 requires between an authenticated workload and a tenant grant. (2) An implementer skips the intermediate case-ID ordering as "not the final tie-break", reintroducing the non-deterministic tenant-wide graph merge that the third adversarial pass closed.

**Fix.** Change `:266` to "app tokens protect service-to-service channels" (or reference AD-5 rather than restating it), and change `:175` to "ordinal ordering is imposed at AD-9's specified merge and tie-break points and nowhere else".

### RUB-07 — medium — `ARCHITECTURE-SPINE.md:205`

**What is wrong.** The Stack table pins the EventStore source-lane gitlink as a literal commit SHA (`b1c00a79d1d34aa7ba3f58046a7844e8b3d57fd6`). That value was correct on 2026-09-09 (superproject commit `4e564e43`) but the gitlink has moved twice since — `2dd7ebfb` on 2026-09-10 and `6b0247ac` on 2026-09-12 (`e18f51a9`), with the working copy currently at `a568af4e`. A `final` spine now pins a source lane the repository no longer references, and no rule says who refreshes the value or what happens when it drifts.

**Divergence permitted.** The story that closes gap row `:304` ("add isolated source-mode contract/integration evidence") produces evidence against `b1c00a79`, while every other build in the repository uses the recorded gitlink — so the two release lanes AD-19 requires are compared at different source revisions, which is exactly the substitution AD-19's "Prevents" forbids.

**Fix.** Replace the literal SHA with a pointer to the authoritative source (the superproject gitlink for `references/Hexalith.EventStore`, plus the package-lane version it must correspond to) and add one clause to AD-19 making gitlink/package-version correspondence part of lane evidence. The version lens owns whether the specific revisions are otherwise acceptable.

### RUB-08 — medium — `ARCHITECTURE-SPINE.md:138`

**What is wrong.** AD-14 is the only capability-bearing AD with no phase qualification. AD-11 phases graph population (`:120`), AD-12 phases MCP and CloudEvent capabilities (`:126`) — AD-14 states a single create-backfill-verify-switch-retire contract that the PRD places in Phase 2 (embedding/schema) and Phase 3 (backend replacement), while the MVP contract for FR43 is a degraded, acknowledged tenant rebuild. PRD Open Question 9 records this as a G6 phase-blocker, and SCP §5.1 already proposes the corrected wording.

**Divergence permitted.** An MVP story either over-builds staged migration infrastructure that the product does not need yet, or ships the degraded rebuild and is judged non-conformant against AD-14; a reviewer cannot decide which is correct from the spine. AD-14's "Prevents" (destructive in-place changes) is also unachievable in Phase 1 as currently shipped, so the rule is violated by design until it is phased.

**Fix.** Adopt SCP §5.1's phase-qualified AD-14 verbatim, preserving its adapter and secret-resolution clauses. The drift lens owns the wider FR75/NFR37/G6 binding refresh.

### RUB-09 — medium — `ARCHITECTURE-SPINE.md:178`

**What is wrong.** The Errors convention requires "stable code, human message, and actionable suggestion" but never says where codes are declared, who owns the namespace, that they are unique, or that they are immutable once published. AD-12's additive-evolution rule covers Contracts.V1 types, not the code catalogue. The PRD requires the same code to survive three mappings (server → Dapr envelope, MCP error response, HTTP + JSON envelope to CLI/third parties).

**Divergence permitted.** REST returns `MEM-404`, MCP maps an unrelated string, and the CLI invents a third for the same condition; a code is silently renamed in a patch release because nothing declares it part of the wire contract; recovery suggestions become surface-specific, which contradicts AD-12's equivalence requirement for recovery guidance in evidence-bearing results.

**Fix.** State that the error-code catalogue is declared in `Contracts/V1`, is append-only under AD-12's additive rule, that every surface maps from it rather than defining codes, and that a code's meaning may not change without a versioned break.

### RUB-10 — low — `ARCHITECTURE-SPINE.md:114` (with `:156`, `:181`)

**What is wrong.** AD-10's decision hinges on "respond safely" / "a safe response" and neither is defined. The Health convention inherits the ambiguity ("an individual query backend reports capability degradation rather than removing a server that can still produce a safe available-axis response"). AD-17 has the same shape of hole in "within the approved delivery bound", a bound that exists in neither the spine nor PRD NFR34.

**Divergence permitted.** One axis adapter reports a backend serving stale-but-consistent data as available (degraded), another reports it unavailable; the query then either degrades with an Evidence Packet note or fails outright for the same backend state. Readiness reporting diverges the same way.

**Fix.** Define "safe" once — a response that is authorization- and case/tenant-correctly scoped and whose freshness is knowable and labelled — and state explicitly that staleness is degradation, not unavailability. Give AD-17's delivery bound a location and owner in the same pass (PRD or validated configuration, as AD-18 does).

### RUB-11 — low — `ARCHITECTURE-SPINE.md:319`

**What is wrong.** The Deferred hosted-Web row says the future host "owns navigation, authentication, authorization, global state, render mode, and activation of the conformance components". Authorization is not deferrable: AD-5 makes tenant, case and caller authority server-derived and says no channel or surface mechanism substitutes for tenant/case checks.

**Divergence permitted.** A Phase 2 host team reads this row as a licence to implement its own authorization layer, and either duplicates or weakens AD-5's server-side checks — the outcome AD-5 exists to prevent — while believing it is inside a deferred boundary.

**Fix.** Reword to "authentication and session integration, navigation, global state, render mode, and activation of the conformance components; server-derived authority remains AD-5's".

### RUB-12 — low — `ARCHITECTURE-SPINE.md:259`, `:271`

**What is wrong.** Only two environments exist in the spine: local AppHost composition and Production Kubernetes. There is no environment taxonomy (CI, integration/test, staging/pre-production) and no parity invariant, even though AD-19 requires isolated lane evidence that must run *somewhere*, and AD-14 uses "staging" to mean staging *resources* inside a tenant migration — a collision with the usual environment meaning.

**Divergence permitted.** Lane evidence is produced in environments whose component wiring differs from Production (already visible as the floating-image gap at `:305`), so a green lane proves less than it appears to; and nothing forbids a test environment from substituting a different Dapr component contract for the one Production uses.

**Fix.** Name the environments the platform recognizes and add one parity invariant: every environment uses the same Dapr component contracts and pinned image digests, differing only in scale and in the documented Production-only dependencies (external EventStore gateway, OpenBao topology). Disambiguate AD-14's "staging resources" wording while there.

### RUB-13 — low — `ARCHITECTURE-SPINE.md:216-239` (with `:305`)

**What is wrong.** The developer-experience dimension has no architectural home. No AD binds NFR30 (`--help` with an example per command) or NFR31 (README quickstart under 30 minutes on a clean machine), and the timed G3 path — a Must-pass ship gate that runs AppHost boot → `tenant create` → `case create` → `ingest` → `search query` — is not identified anywhere in the spine even though gap row `:305` (floating AppHost images) sits directly on it. The Structural Seed also omits `samples/`, which the PRD makes a launch prerequisite for L1 and L2, and `docs/`, which holds the required walkthrough log.

**Divergence permitted.** Sample and documentation assets land in inconsistent locations (or nowhere), and the quickstart composition — the artifact G3 is timed against — has no owner, so a change to AppHost defaults can break the gate path without violating any recorded rule.

**Fix.** Add `samples/` and `docs/` rows to the seed with one clause each, and one sentence (AD-19 is the natural home) binding the quickstart composition to pinned qualified defaults and naming it as the G3-evidence path.

### RUB-14 — low — `ARCHITECTURE-SPINE.md:160-162`, `:130-132`

**What is wrong.** Two Binds terms have no corresponding Rule clause. AD-18 Binds NFR14 — per-memory-unit Redis footprint predictable and documented, so an operator can estimate infrastructure cost before provisioning — and its Rule covers quotas, queues, `Retry-After` and priority but never sizing or its evidence; cost is otherwise absent from the entire spine. AD-13 Binds "correlation" and its Rule never mentions correlation or causation identifiers, which AD-11 depends on for Phase 1 `caused_by`/`correlated_with` population.

**Divergence permitted.** No unit owns the sizing model, so capacity evidence ships without the per-unit figures operators need and the cost dimension stays undecided by default. For correlation: nothing states that supplied `CausationId`/`CorrelationId` are opaque, preserved through projection, and distinct from W3C trace identifiers, so ingestion and graph population can treat them inconsistently.

**Fix.** Add a sizing/documentation clause to AD-18's evidence sentence (per-unit memory by vector dimension and metadata size, published with capacity evidence), and add correlation/causation identifiers to AD-13's Rule as opaque preserved provenance, distinct from trace context.

---

## Dimension verdict summary

| # | Dimension | Verdict | Findings |
| --- | --- | --- | --- |
| 1 | Fixes the real divergence points, misses none | PASS-WITH-FINDINGS | RUB-01, RUB-02, RUB-09 |
| 2 | Rules enforceable and actually preventing | PASS-WITH-FINDINGS | RUB-02, RUB-03, RUB-08, RUB-10 |
| 3 | Deferred hides no live divergence | PASS-WITH-FINDINGS | RUB-11 |
| 4 | Named technology verified-current | PASS (flagged) | RUB-07 |
| 5 | Ratifies the brownfield codebase | PASS (flagged) | RUB-05 |
| 6 | Covers the driving spec | PASS-WITH-FINDINGS (flagged) | RUB-08 |
| 7 | Every owned dimension decided/deferred/open | PASS-WITH-FINDINGS | RUB-04, RUB-05, RUB-12, RUB-13, RUB-14 |
| 8 | Structural balance: minimal seed, no bloat | PASS | — |
| 9 | ID stability and cross-reference consistency | PASS-WITH-FINDINGS | RUB-06, RUB-14 |
