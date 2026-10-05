# Pass 6 architecture spine rubric review — 2026-10-05

**Verdict: REVISE.** The feature-level boundaries, erasure authority, projection cutover, export custody, and operational envelope are now governed, but the spine still asserts stale repository facts and is too long to function reliably as a terse build substrate. This is a review of the spine, not a claim that the implementation meets it.

**Snapshot:** `ARCHITECTURE-SPINE.md` SHA-256 `e26a95469d860f9e9ab3e9e4635c171b6422b954cd3ccc57c0714ac52934dbbb` (read after the latest AD-6, AD-14, AD-16, and AD-17 amendments). The deterministic linter returned `ok: true`, `total_findings: 0`.

## High

1. **The Stack and alignment ledger contradict committed HEAD.** AD-19 says every fact-source gitlink is re-derived at each spine revision, yet the Stack claims Builds `bf8adb73` and EventStore `4502913c`, while `git submodule status --cached` reports Builds `11ae79eb` and EventStore `7dcc4756`. The Stack still lists SDK `10.0.400`, Aspire `13.5.3`, Dapr SDK `1.18.7`, EventStore package `3.103.0`, and Fluent UI prerelease, while committed `global.json` is `10.0.401`, AppHost SDK is `13.6.0`, and the current Builds catalog has Dapr `1.18.10`, EventStore package `3.112.0`, and Fluent UI `5.0.0`. The ledger still describes the SDK bump to `10.0.401` as owed. This makes the brownfield diagnosis and some Production classifications unreliable. **Autofix:** re-derive each pin and gitlink from committed HEAD, recheck every fact-dependent ledger row, and retain an evidence obligation only where it remains unmet. External release/security assertions need a separate current-source check.

## Medium

2. **The spine exceeds its declared build-substrate altitude.** It is 19,112 words; AD-5 and AD-16 each contain several contracts, implementation notes, historical code diagnoses, and migration remedies, while the Stack contains a long version advisory and the alignment ledger spans more than fifty rows. The invariant is often difficult to locate without reading an entire paragraph. The detailed ledger is useful, but its placement makes a small implementation intent consume the whole platform review. **Autofix:** keep the binding `AD` rules, capability map, and short operational envelope in the spine; move historical evidence and the gap register to a linked companion that retains the same `AD` references. Do not delete the unresolved obligations.

3. **AD-20's classification audit rule is not represented in the ledger.** AD-20 says an owner records the triggering clause of the active-foundation-critical predicate for each row. The ledger offers only `Yes` or `No`, without the reason, so another reviewer cannot reproduce a classification or tell whether a phase-inactive item was assessed. **Autofix:** add a terse predicate reason to each row or amend AD-20 to point to an explicit review record containing those reasons.

4. **The source contract remains contradictory pending the planning correction.** The spine's AD-20 allows gate credit only from current rerunnable evidence; PRD G6 still allows a product-and-architecture phase exception. The spine acknowledges the upstream correction, and the 2026-10-05 sprint proposal records a decision to align the PRD, but the PRD has not been changed or approved as a complete proposal. **Discuss/route:** keep the spine draft and the release no-go posture until the approved planning correction updates the PRD/addendum. This is an upstream handoff, not a new architecture decision.

## Low

5. **Frontmatter dates lag the current decision text.** `updated: '2026-09-12'` and most ratification entries predate the 2026-10-05 export, bootstrap, lifecycle, and cutover decisions now represented in the rules. **Autofix at close:** update the date and decision record when the spine is finalized.

## Rubric walk

| Criterion | Judgment |
| --- | --- |
| Feature-altitude divergence points | Covered for ownership, authority, idempotency, projection epochs, query-axis states, migration, erasure, export, and gate evidence. The latest revisions resolve the earlier dual-store export-release CAS and tenant-stream `Erased` contradictions. |
| AD enforceability | Binding rules are mostly testable; many remain explicitly unenforced in code and correctly appear as ledger blockers. AD-20's ledger-classification evidence is the remaining self-enforcement gap above. |
| Deferred safety | No current Deferred item silently licenses an incompatible MVP implementation. Production state-store durability, Redis migration, and full register recovery have revisit conditions and fail-closed interim rules. |
| Brownfield fidelity | Explicit target-versus-current labeling is good for provider adapters, tenant IDs, dedup, packet serialization, and erasure. The Stack and SDK ledger facts are stale, so this criterion does not pass. |
| Source coverage | The current spine binds FR75, NFR37, G6, and FR71 in named decisions and the capability map. PRD G6 is still inconsistent upstream. |
| Operational/environmental envelope | Deployment, environments, provider strategy, security, availability, recovery, capacity, observation, testing, packaging, and release evidence are each decided or deferred. |
| Terse build substrate | Fails at 19,112 words. The decisions are substantially clearer than earlier passes, but the rendered artifact is too large for its stated use. |

**Review boundary:** I checked local committed pins and the architecture/source documents. I did not independently verify current external vendor releases, security advisories, or run implementation tests; those belong to the parallel version/security lenses and later implementation evidence.

## Retest — 2026-10-05, latest spine

**Snapshot:** SHA-256 `a302836b7e6e817becde61a003eb8cf86264a92eff0994632efb177fb4d4ebf7`; deterministic lint again reports zero findings. The original high stale-pin finding is **closed**: committed Builds/EventStore/FrontComposer gitlinks, `global.json` `10.0.401`, AppHost `13.6.0`, EventStore package `3.112.0`, Dapr `1.18.10`, and Fluent UI `5.0.0` now match the Stack. The revised ledger distinguishes upgraded pins from still-owed current qualification evidence. No remaining high blocker was found by this rubric lens in the latest bytes; implementation gaps in the ledger remain open by design.

**AD-20 classification audit remains open (medium).** The Rule requires each row to record the triggering predicate clause, yet the last column remains bare `Yes`/`No`. The smallest durable repair is to retain the existing five-column table and replace each bare value with a short basis. Examples: `Yes — MVP; cross-tenant exposure`, `Yes — MVP; unverifiable gate claim`, `No — Phase 1.5 only`, or `No — release debt; no MVP behavior or gate effect`. Use the Rule's consequence terms, not a new scoring system. Review every `No` while making the edit: the source/package contract-and-integration lane is currently `No`, although the Rule's “unverifiable gate claim” clause could apply if the lane is needed for G6. The classification should follow the stated predicate rather than the existing flag.

**Build-substrate length remains open (medium).** The revised spine is 18,982 words. The 130-word reduction does not materially change the navigation cost. A concise core plus linked gap/evidence companion would preserve obligations while letting implementers find the invariants. PRD G6 alignment and the frontmatter date are also still pending as described above.

## Final delta retest — 2026-10-05

**Verdict for latest bytes: PASS WITH FINDINGS.** Snapshot SHA-256 `aa75b8811b39ee6246fdbb126126f9d55326c1c67aa91c4fca642c81539e3cdf`; deterministic lint reports zero findings. I found no remaining high rubric issue or internal architecture contradiction in this delta. This supersedes the earlier REVISE verdicts for stale repository facts and unaudited ledger classification; it does not claim that the implementation blockers have closed.

AD-20's classification audit is now represented in all 51 ledger rows: each final cell gives a Yes/No decision and a short predicate reason. The source/package evidence lane and Contracts.V1 guard are correctly marked MVP-active, and the served access-telemetry `/v1` contract is marked `Yes — MVP-active served /v1 contract; unverifiable gate claim`. The CloudEvent duplicate row now requires AD-4's canonical `source` and AD-23's digest-collision reservation, without offering an AD-4 rewrite as convergence. The unregistered coordination row converges to Dapr state under AD-8, and the preflight dedup row requires a distinct reserved prefix and separate failure-posture tests; neither offers an unapproved architectural alternative. Frontmatter `updated` is 2026-10-05, and the ledger date note distinguishes the retained 2026-10-31 checkpoint from the September proposal's historical OPEN field.

The spine remains long at 19,443 words, a **medium** build-substrate navigation concern. PRD G6 wording still awaits the separately approved planning correction. These are handoff findings, not a reason to reopen the now-consistent architecture decisions in this pass.

## Source and grammar finality retest — 2026-10-05

**Rubric verdict: PASS WITH FINDINGS; no critical or high architecture issue in the latest spine.** Snapshot SHA-256 `a8fc426d72b0a72a53a6c0e48b022fe07b922bfebb98d43dc77f20eadebe0084`; deterministic lint again reports zero findings. This is a design/handoff verdict, not evidence that the 51 implementation blockers are resolved or that G1–G6 pass.

The PRD G6 criterion now matches AD-20's evidence-only rule, while FR75 and NFR37 remain explicitly bound by AD-3/AD-4 and AD-12. PRD and addendum metadata dates are both 2026-10-05. Identifier Grammar V1 is normative in the tracked spine: class-specific fixed forms, lowercase `u` between every component, immutable per-position codecs and byte encodings, destination tests, ingress bounds, and collision-checked digest reservations are defined. V1 expressly forbids a replacement tenant population after register-lineage loss; a future population requires a separately ratified disjoint namespace and grammar amendment. That closes the random-ID/non-rollback conflict found during this retest.

The alignment ledger now has **51/51** rows marked `blocker`, each with a predicate reason, owner, review date, evidence owed, tracker owed, and a per-row September sprint-change freeze reference. It treats Identifier Grammar V1 as specified but unimplemented, and upgraded dependency pins as distinct from current qualification evidence. No row claims gate credit from a date, risk acceptance, or completed code without rerunnable evidence. The operational dimensions and Deferred revisit conditions remain covered.

The spine remains `status: draft`. This rubric lens supports changing it to `final` after the configured independent reviewer retests are reconciled and the architecture run logs its finalization event. Its 20,455-word length remains a medium navigation concern for a build substrate; the gap ledger accounts for much of that size. Named G1 human reviewers and implementation evidence remain separate product/release prerequisites.
