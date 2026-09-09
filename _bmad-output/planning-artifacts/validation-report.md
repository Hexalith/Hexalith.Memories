# Validation Report — Hexalith.Memories

- **PRD:** /home/administrator/projects/hexalith/memories/_bmad-output/planning-artifacts/prd.md
- **Rubric:** /home/administrator/projects/hexalith/memories/.agents/skills/bmad-prd/assets/prd-validation-checklist.md
- **Run at:** 2026-09-09T09:10:51+02:00
- **Grade:** Poor

## Overall verdict

This is a strategically strong, unusually candid change-controlled PRD: it states a falsifiable thesis, separates thesis and launch contracts, names hard no-go outcomes, and exposes delivery gaps rather than presenting shipped code as validated product. Its main risk is downstream extraction: several phase, parameter, and decision-state contradictions remain across the PRD and addendum, while a small set of cross-cutting requirements still describe the need for a bound rather than supplying the bound. The document is decision-ready in its strategic framing, but those inconsistencies should be reconciled before another UX, architecture, or story pass treats every sentence as canonical.

The adversarial and source-fidelity passes materially lower the gate result. Two critical contradictions make deletion guarantees and launch gate L1 impossible to prove as written; eight additional adversarial high findings leave the thesis gate, authorization, recovery, public compatibility, onboarding, degraded reads, telemetry, and launch qualification open to incompatible implementations. The source review adds three high-fidelity defects in backend access, CLI ownership, and export status. The PRD should not serve as the acceptance contract for deletion, public API compatibility, or Phase 1.5 launch until the critical findings and phase-blocking high findings are resolved.

## Dimension verdicts

- Decision-readiness — strong
- Substance over theater — adequate
- Strategic coherence — strong
- Done-ness clarity — adequate
- Scope honesty — strong
- Downstream usability — thin
- Shape fit — adequate

## Findings by severity

### Critical (2)

**[Adversarial ADV-C1] — “Delete all data” can be undone by the durability contract (§ Compliance Boundary; FR27; FR39; FR13; NFR16; NFR34; addendum § EventStore — three contracts)**

Deletion is presented as erasure, while EventStore remains the durable source of truth and lost projections must be rebuilt from it. Without purge, redaction, crypto-shredding, permanent replay suppression, backup expiry, and telemetry rules, deleted tenant data can reappear after replay.

Fix: Define a deletion contract per data class, including authoritative events, extracted content, projections, embeddings, graph edges, workflow history, telemetry, and backups; specify mechanism, replay suppression, SLA, failure recovery, and verification evidence. Bound the current erasure claim until this exists.

**[Adversarial ADV-C2] — Launch gate L1 requires labels that do not exist (§ Measurable Outcomes › Phase 1.5 launch go/no-go, L1)**

L1 requires at least ten topics excluded from G1 labelling but names frozen G1 labels as the scorer, so the held-out topics have no labels. The threshold also says 8/10 while permitting more than ten topics.

Fix: Define a separately frozen and independently graded L1 set excluded from G1 scoring and tuning, with a named relevance scale and adjudication method. Use exactly ten topics or a percentage for N ≥ 10, and identify the corpus hash, owner, scorer, reference-agent configuration, and evidence artifact.

### High (11)

**[Adversarial ADV-H1] — The G1 thesis gate lacks a scoreable ground-truth contract (§ Measurable Outcomes › Thesis-gate protocol; Release decision record; Open Question 2)**

The PRD does not define the relevance scale, reviewer-grade aggregation, agreement interpretation, tie and abstention handling, or a reproducible sampling frame for the “representative mix.” Teams can produce incompatible but facially compliant suites.

Fix: Make the protocol a gate prerequisite with the query population, sampling rule, relevance scale, reviewer workflow, agreement and adjudication rules, frozen hashes, accountable owner, and owning story.

**[Adversarial ADV-H2] — Privileged product actions have no authorization policy (Glossary › Member; FR28–FR29; FR38–FR45; FR51; FR67; FR71; FR74; NFR8; Journey 5)**

Tenant isolation is defined, but no role or permission model governs deletion, configuration, export, repair, telemetry, membership, or confidence promotion. NFR8 also allows silent tenant retargeting where FR44 and Journey 5 require rejection.

Fix: Add a capability-by-role authorization matrix for platform operator, tenant operator, contributor, reader, and service identities. Make every tenant-claim/request mismatch fail closed with one specified error.

**[Adversarial ADV-H3] — Rebuild is promised without durable rebuild inputs (FR6; FR13; FR70; § Async Ingestion Pipeline; NFR16)**

The durable commit is not required to retain immutable content, source hashes, extraction and chunking versions, metadata, or embedding-model identity. Mutable or vanished sources and retired models can make rebuild non-equivalent or impossible.

Fix: Define the minimum durable recovery payload, versioning, model-unavailability fallback, equivalence criteria, and proof that committed units can be recovered without refetching mutable sources.

**[Adversarial ADV-H4] — Access telemetry can launch before its privacy and retention contract exists (Glossary › Access telemetry; Journey 5; § Compliance Boundary; FR67; NFR34)**

The PRD does not define recorded fields, redaction, read/export permissions, deletion interaction, or a required TTL default/range, allowing telemetry to become a less-protected sensitive-data store.

Fix: Add a telemetry data dictionary, minimization and redaction rules, access roles, tenant scoping, encryption expectations, default and maximum TTL, erasure behavior, purge evidence, and an accountable owner. Gate launch on the evidence or disable telemetry by default.

**[Adversarial ADV-H5] — Public API versioning stories conflict (§ Service Communication Model; § Versioning Strategy; CLI Specification; § Evidence Packet; published packages)**

The PRD says there are no versioned endpoints while shipped routes and contracts are explicitly V1. Stability, preview status, allowed changes, and deprecation windows are undefined across REST, DAPR, NuGet, MCP, and CLI JSON.

Fix: Establish one compatibility policy covering maturity, version identifiers, additive changes, enum and required-field rules, breaking-change vehicles, deprecation/support windows, and preview versus stable surfaces.

**[Adversarial ADV-H6] — Onboarding stopwatches lack reproducible starting states (L2; NFR31; § Developer Experience & Documentation; Release decision record)**

Checkout, CLI installation, restore, caches, samples, image pulls, secret-store seeding, and external setup are inconsistently inside or outside the timed procedure, so the same target can be passed under incomparable conditions.

Fix: Publish an exact T0 fixture manifest for G3 and L2: source/sample commit, cache state, CLI state, secret-store state, allowed credentials, network rules, commands, stop event, supported OS matrix, and evidence template.

**[Adversarial ADV-H7] — Degraded reads and partial ingestion lack one eligibility rule (FR6; FR13; FR66; § Async Ingestion Pipeline; NFR18; Evidence Packet)**

The PRD requires all three projections before searchability but also requires results when one backend fails. It does not distinguish previously complete units from newly partial or version-skewed units.

Fix: Define degraded query eligibility, prior indexed-state requirements, source-version agreement, stale-version behavior, per-result degradation/freshness fields, and recovery semantics.

**[Adversarial ADV-H8] — Launch gates omit known unmet security and reliability requirements (§ Phase 1.5 launch go/no-go; NFR status; NFR6; NFR11; NFR16; NFR21; NFR34)**

L1–L3 omit known unmet freshness, negative-envelope, recovery, and telemetry-retention requirements, so the product can formally launch while those obligations remain unverified.

Fix: Add a production-qualification row naming required NFR evidence or dated debt acceptance and its authority. If Phase 1.5 is preview-only, state that and define the deferred GA requirements.

**[Source fidelity SF-H1] — Backend access is both direct-client and DAPR-sidecar mediated (PRD §§ Open-Source Licensing, Deployment Topology, Service Communication Model)**

The approved change says DAPR state uses the sidecar while direct Redis/FalkorDB search and graph access uses approved clients. The licensing and topology sections still claim that DAPR is the FalkorDB client or route.

Fix: Align those sections with the Service Communication Model and keep detailed client topology architecture-owned.

**[Source fidelity SF-H2] — Unowned Phase 1 CLI gaps are incorrectly called tracked (§ CLI Specification; Release decision record)**

The release record says the stubbed G1 prerequisites have no owning story, while the CLI section says every stub is tracked in epics and sprint status. A planner can therefore omit the required story-registration transaction.

Fix: State that the stubs are identified but unowned and unregistered as of 2026-09-08, with Jerome responsible for creating and sprint-selecting bounded owners by the recorded date.

**[Source fidelity SF-H3] — FR71’s status row reverses portable export and operational restore (§ FR delivery register; FR71; CLI export)**

The approved source and canonical FR say portable case/tenant export shipped while operational re-import/restore remains Epic 26 work. The delivery register states the reverse.

Fix: Correct the register to record shipped portable export and separately owned re-import/restore, and retain the non-MVP Phase 2 status without duplicating work.

### Medium (18)

**[Rubric: Substance over theater] — Future and non-product narratives dilute active journeys (§ User Journeys 4, 6, 8, and 10)**

Full narratives for Phase 2, Phase 3, application UI, and contributor operations make the active Phase 1/1.5 contract look broader and more settled than it is.

Fix: Keep full narratives for load-bearing Phase 1/1.5 paths and reduce later-phase or contributor material to future-scenario notes or addendum content.

**[Rubric: Done-ness clarity] — Cross-cutting requirements defer acceptance bounds (§ NFR13, NFR15, NFR33, NFR34)**

The requirements name desirable properties but omit maximum interference, migration extraction tests, freshness transitions, TTLs, and recovery bounds.

Fix: Supply explicit numbers, state transitions, and verification fixtures for active surfaces; give future items an owner and activation condition.

**[Rubric: Done-ness clarity] — Cross-tenant mismatch has two observable outcomes (§ NFR8; FR44; Journey 5)**

NFR8 permits rejection or claims-based retargeting, while FR44 and Journey 5 promise rejection.

Fix: Require rejection for explicit tenant conflicts and define claims-based scoping only for requests that omit tenant identity, if omission is allowed.

**[Rubric: Downstream usability] — Journey 8 has conflicting phase ownership (§ Non-Goals; Journey 8; Journey Requirements Summary)**

The heading says Phase 1 while the explicit non-goal and summary assign the application-facing REST UI to Phase 2.

Fix: Retag Journey 8 and all references as Phase 2.

**[Rubric: Downstream usability] — MCP search parameter drifts between axis and axes (Journeys 3 and 7; FR58 support text)**

The public schema is ambiguous because setup promises axes while examples use axis.

Fix: Choose the actual MCP field everywhere and distinguish it from CLI --axis if needed.

**[Rubric: Downstream usability] — Addendum release-date state is stale (addendum § Release decision)**

The addendum says dates remain assumed until confirmation while the PRD and later addendum handoff say Jerome confirmed them.

Fix: Preserve the history but state that the launch and release dates are confirmed and only the derived sprint-selection date remains unconfirmed.

**[Adversarial ADV-M1] — Journey 8 schedules the same web experience in Phase 1 and Phase 2 (Journey 8; Non-Goals; Journey Requirements Summary; Language & Platform Matrix)**

UX and epic authors can legitimately schedule the same narrative UI in either phase.

Fix: Retag the heading and capability beats as Phase 2 and separate existing transport/client capability from the future application experience.

**[Adversarial ADV-M2] — One ingestion vocabulary remains two public vocabularies (Glossary; FR10; § Async Ingestion Pipeline; Open Question 8)**

The PRD mandates pending/projecting while shipped contracts expose Queued/Indexing, and the temporary mapping can remain open indefinitely.

Fix: Choose canonical wire values before the next stable release or explicitly make the shipped enum canonical with a presentation mapping and version policy.

**[Adversarial ADV-M3] — FR71 is marked shipped for a different capability than it states (§ FR delivery status; FR71; CLI export)**

The status note credits operational export/restore while FR71 promises portable developer-facing export.

Fix: Split the capabilities and statuses or mark FR71 partial until its public format, completeness, versioning, and CLI behavior meet the requirement.

**[Adversarial ADV-M4] — Freshness states have no product semantics (Journey 7; Evidence Packet; NFR33)**

Current, aging, stale, and unknown lack thresholds, clock source, source policy, and recovery behavior, so surfaces can classify the same unit differently.

Fix: Define source-specific thresholds, timestamp selection, clock-skew tolerance, transitions, disclosures, and recovery behavior.

**[Adversarial ADV-M5] — CLI bearer-token handling is unsafe and unspecified (§ CLI Specification; Configuration Layering; NFR9; NFR11)**

The explicit command-line token can leak through shell history and process inspection, while acquisition, refresh, storage, redaction, and CI behavior are undefined.

Fix: Define secure credential sources and precedence, redaction and persistence prohibitions, expiry/refresh, and CI injection behavior; retain the command-line option only with explicit risk treatment.

**[Adversarial ADV-M6] — Licensing conclusions lack a decision authority (§ Open-Source Licensing; addendum § Topology)**

The PRD states categorical legal conclusions without a named legal reviewer, version-specific assessment, jurisdiction, or accepted-risk owner.

Fix: Recast them as licensing risks and required legal decisions, naming artifacts/versions, owner, review date, allowed modes, and approved public wording.

**[Adversarial ADV-M7] — PRD/addendum change-control boundary is unusable (§ Document Purpose; Technical Architecture; Deployment Topology; Async Ingestion; addendum § Why this file exists)**

The PRD assigns mechanisms to architecture/addendum but also mandates exact products and mechanics, while source precedence remains ambiguous.

Fix: Classify immutable product constraints, observable acceptance behavior, replaceable reference implementation, and architecture-owned current choices, including which changes require product change control.

**[Adversarial ADV-M8] — Availability and disaster recovery stop at component examples (NFR7; NFR16–NFR19; Health and Observability; Release decision record)**

There is no service availability objective, EventStore/Workflow/FalkorDB recovery target, maximum degradation period, backup cadence, or escalation boundary.

Fix: Define launch-level availability, RPO, and RTO envelopes or explicitly make Phase 1.5 a non-production preview.

**[Source fidelity SF-M1] — Journey 8 keeps Phase 1 after the approved Phase 2 correction (Journey 8; Non-Goals; Journey summary)**

This heading-level drift can pull future UI and narrative composition into the thesis MVP.

Fix: Retitle Journey 8 as Phase 2 and add the explicit phase banner used by other future journeys.

**[Source fidelity SF-M2] — NFR32 omits approved accessibility modes (NFR32; approved 2026-08-03 rerun amendment)**

Reduced motion, forced colors, zoom/reflow, and responsive access to trust fundamentals were approved but are no longer explicit, making them easy to omit from downstream test matrices.

Fix: Restore all four dimensions and require the Epic 17 evidence matrix to exercise them.

**[Source fidelity SF-M3] — Tenant-deletion isolation lacks acceptance evidence (§ Compliance Boundary; FR39; NFR8)**

The product brief required deleting tenant A and proving all indexes and graph data were removed without affecting tenant B; the capability remains, but the verification scenario does not.

Fix: Add an FR39/NFR8 integration check covering all data classes and the authoritative delete/tombstone contract, or name the exact alternate owner.

**[Source fidelity SF-M4] — Addendum still calls confirmed release dates assumptions (addendum § Release decision)**

The memlog and PRD confirm 2026-12-01 and 2027-01-01, while the addendum both disputes and confirms them.

Fix: Distinguish confirmed decision dates from the derived 2026-10-31 sprint-selection date and retain the sprint-change rule for moving them.

### Low (7)

**[Rubric: Decision-readiness] — Open Questions is not a clean pending-decision queue (§ Open Questions)**

Closed and settled items force decision-makers to retriage the list.

Fix: Move resolved items to a decision log and retain only active decisions.

**[Rubric: Done-ness clarity] — The primary empty-state command is not executable (Journey 9)**

The hint omits required tenant and case options.

Fix: Show complete syntax or clearly label the command as shorthand.

**[Rubric: Downstream usability] — Assumptions Index does not round-trip (§ Release decision record; Assumptions Index)**

The 2026-12-01 work-window assumption is missing from the index, while the NFR33/NFR35 collision is index-only.

Fix: Index the schedule assumption and move the historical collision to decision history, or add a matching inline tag.

**[Rubric: Downstream usability] — Journey 7 is a floating technical actor (Journey 7)**

LLM Agent is an interaction pattern, not a named protagonist with context.

Fix: Relabel it as an Integration Flow or give the agent a named application and context.

**[Rubric: Shape fit] — Outcome/mechanism boundary exceptions are undeclared (§ Document Purpose; Package Distribution; Deployment Topology; addendum)**

Package inventory and topology remain in the PRD despite its rule assigning them to architecture/addendum.

Fix: Move them or state precisely which facts remain contractual and which are architecture-owned.

**[Source fidelity SF-L1] — Future roadmap phase advances lack a traceable decision (product brief roadmap; PRD Phase 2/3)**

Embedding migration and enterprise/UI capabilities were pulled into earlier numbered phases without a memlog decision or explicit set-aside.

Fix: Log the deliberate compression with rationale or restore the source phase assignments; prefer named outcomes over unstable phase numbers.

**[Source fidelity SF-L2] — Deferred journey-density decision lacks artifact disposition (.memlog.md; User Journeys; addendum handoff)**

The memlog assigns Jerome and the Phase 1.5 launch decision as the revisit point, but neither visible artifact records it.

Fix: Add an addendum handoff entry with owner, revisit condition, and no-current-change status.

## Mechanical notes

- FR1–FR74, NFR1–NFR36, and Journey 1–10 definitions are contiguous and unique.
- Journey 8 phase drifts between Phase 1 and Phase 2.
- MCP parameter naming drifts between axis and axes.
- The Assumptions Index misses the schedule-window assumption and contains an index-only historical collision entry.
- Open Questions includes three closed items and one settled MVP constraint.
- Journey 7 lacks a named protagonist and fits an integration-flow shape better.
- Addendum release-date rationale is stale relative to confirmed decisions.

## Reviewer files

- review-rubric.md
- review-adversarial-general.md
- review-source-fidelity.md
