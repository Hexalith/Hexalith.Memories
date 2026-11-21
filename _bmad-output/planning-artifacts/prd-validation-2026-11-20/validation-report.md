# Hexalith.Memories PRD validation

**Poor** · 1 critical · 11 high · 9 medium · 2 low · 2026-11-20T18:21:51+01:00

[November 20 PRD](../prd.md) · [Addendum](../addendum.md) · [Quality rubric](../../../.agents/skills/bmad-prd/assets/prd-validation-checklist.md) · [HTML report](validation-report.html)

## Overall verdict

The November 20 PRD supports holding Phase 1. It states a falsifiable search thesis, keeps the eight-topic diagnostic separate from qualifying evidence and identifies when the release decision can be reopened. Its acceptance criteria still allow reviewers to reach different decisions from the same data.

The critical defect is in Phase 1.5 gate L1. Its held-out topics have no applicable relevance labels. Phase 1 also needs an explicit G1 scoring rule and a consistent G2 tenant-mismatch result. The planning sequence for Story 34.33 before 34.1 agrees with the architecture and can proceed within its existing scope.

## Dimension verdicts

- **Decision-readiness — adequate.** The November decision names the owner, the missed checkpoint and the evidence for reopening. L1 cannot yet produce a launch verdict, and G1 needs a reproducible scoring rule.
- **Substance over theater — adequate.** The benchmark section explains why the eight-topic result earns no G1 credit. The market section goes further than its evidence supports.
- **Strategic coherence — strong.** Phase 1 tests the incremental value of graph retrieval against BM25 plus semantic retrieval. Aggregate and worst-topic limits constrain that bet. A measured failure changes the default search behavior.
- **Done-ness clarity — thin.** Several tests permit opposite outcomes for the same run. The most consequential differences concern scoring, tenant rejection and which clients must supply gate evidence.
- **Scope honesty — adequate.** The phase and release tables distinguish current, preview and deferred capabilities. Some delivery rows nevertheless credit a later acceptance contract or a user-facing operation that remains unavailable.
- **Downstream usability — thin.** Stable requirement IDs support traceability. Contradictions in identity, routing and operator examples would lead implementers to write different behavior for the same request.
- **Shape fit — adequate.** Developer, operator and agent journeys suit this platform. Duplicating volatile deployment and package details across the PRD, addendum and manifest has introduced drift.

## Findings by severity

### Critical (1)

**PRD-C01 · L1 has no labels for the topics it must score** (Decision-readiness, PRD 204)

L1 requires topics excluded from G1 labelling, then uses the frozen G1 labels to decide whether a citation is relevant. It also permits more than ten topics while prescribing eight successes out of ten.

**Suggested correction** · Freeze independent L1 topic–unit judgments and a relevance cutoff before the agent run. Specify exactly ten topics or a pass fraction across every preregistered topic. Retain the requirement that every response meet its token budget. [PRD](../prd.md#measurable-outcomes) · [Approved stories](../epics.md)

### High (11)

**PRD-H01 · G1 does not define the grades used by its scorer** (Decision-readiness, PRD 170–177, 191)

Three reviewers can clear pairwise κ ≥ 0.6 and still disagree on an individual unit. Taking the mean or median of their grades can change whether a topic clears ΔNDCG@10 ≥ 0.02. The diagnostic scorer accepts binary relevance.

**Suggested correction** · Agree on the grade scale, adjudication, gain mapping and unjudged-item treatment before labelling. Freeze the resulting topic–unit file and scorer version with the corpus. [PRD](../prd.md#measurable-outcomes) · [Diagnostic scorer](../../../tests/Hexalith.Memories.Benchmarks/Scoring/NdcgScorer.cs)

**PRD-H02 · L3 alternates between events and whole chains** (Done-ness clarity, PRD 185, 206, 679)

With nineteen complete two-event chains and one missing twenty-event chain, a chain count yields 95% while an event count yields about 66%. Both denominators appear in the PRD.

**Suggested correction** · Choose the scoring unit, define the expected path for each unit, and require the fixture report to give the numerator and denominator. [PRD](../prd.md#measurable-outcomes)

**PRD-H03 · G2 permits silent tenant substitution where FR44 requires rejection** (Done-ness clarity, PRD 192, 402, 1068, 1152)

NFR8 allows a caller authenticated for A who explicitly requests B to receive A’s data. FR44 and Journey 5 require the request to fail. Both can prevent disclosure, but the caller and operator observe different outcomes.

**Suggested correction** · Require rejection with zero restricted output and side effects for an explicit tenant conflict. Test permitted scope omission separately. [PRD](../prd.md#security) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

**PRD-H04 · NFR21 endorses two ways to select a tenant for an event** (Done-ness clarity, PRD 273, 1180)

The normative row includes source-prefix routing. The correction in the same PRD and AD-5 require tenant selection from the authenticated delivery channel. The envelope source is publisher-controlled.

**Suggested correction** · Describe the prefix rule as legacy behavior. Test channel-based routing, tenant grant and active-state checks, including that changing the envelope source cannot change the authorized tenant. [PRD](../prd.md#integration) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

**PRD-H05 · G5 has two Phase 1 surface lists** (Done-ness clarity, PRD 195–198, 1188–1190)

Phase 1 explicitly excludes MCP, while NFR25 requires REST, CLI JSON and MCP golden vectors for the G5 determinism check. Reviewers could accept the same REST and CLI evidence or require an MCP run.

**Suggested correction** · Enumerate the Phase 1 participants and identify any offline contract fixture separately. Assign end-to-end MCP parity to its activation gate. [PRD](../prd.md#algorithmic-quality)

**PRD-H06 · The target Gateway cannot forward the bearer identity NFR10 promises** (Downstream usability, PRD 592, 764–769, 1085, 1154)

Native and compatibility routes can preserve a delegated bearer. The shared Gateway calls with workload authority. The blanket CLI/MCP rule leaves an enrolled operation needing a user without a defined proof of that user’s authority.

**Suggested correction** · State the identity on each transport. Require independent user and tenant authorization before enrolling an operation that needs it, with explicit denial when it cannot be demonstrated. [PRD](../prd.md#security) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [McpCli owner inventory](../../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md)

**PRD-H07 · FR65 omits the issuer from external provenance** (Downstream usability, PRD 592, 769, 1103 and addendum 63)

A subject can be unique within one issuer yet duplicated under another. The PRD’s normalized subject permits two different callers to receive one attribution, contrary to AD-13 and Story 34.27.

**Suggested correction** · Bind external attribution to the authenticated issuer and subject. Verify two issuers with the same subject produce distinct provenance. [PRD](../prd.md#trust--transparency) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Approved stories](../epics.md)

**PRD-H08 · The journeys teach callers to choose durable identifiers** (Downstream usability, PRD 394–402, 478–490)

Journey 5 supplies a descriptive tenant ID and Journey 9 supplies a case identifier. These examples can become fixtures even though AD-23 requires platform-issued opaque IDs and Story 34.52 refuses caller-selected tenant IDs.

**Suggested correction** · Use display names for human input and reuse the identifiers returned by provisioning and case creation in the rest of each journey. [PRD](../prd.md#user-journeys) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Approved stories](../epics.md)

**PRD-H09 · NFR19 marks a missing CLI failure path as verified** (Scope honesty, PRD 903–910, 1009, 1128, 1173)

The verified register includes failure visibility with stage and error details via CLI status. The compatibility command inventory exposes telemetry status and records the failed-unit operation as outstanding.

**Suggested correction** · Link a rerunnable intentional-failure test on the applicable CLI or correct the status and name the outstanding target-surface evidence. [PRD](../prd.md#reliability)

**PRD-H10 · FR43 marks the rebuild as delivered while its operator surface is unresolved** (Scope honesty, PRD 918, 1003, 1067)

FR43 requires a CLI warning, the Phase 1 inventory excludes a CLI verb, and the server currently sets a reindex-required flag after acknowledgement. The verified rebuild and cutover are still owed.

**Suggested correction** · Name the Phase 1 acknowledgement surface and credit the shipped guard separately from the warning, rebuild and cutover. [PRD](../prd.md#tenant-management) · [Current configuration guard](../../../src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

**PRD-H11 · FR74 credits the repair workflow with a dry run it does not provide** (Scope honesty, PRD 911, 1003, 1118)

The compatibility command confirms or accepts `--yes` and schedules repair. The request offers no dry run tied to an authoritative EventStore revision. An operator cannot inspect the proposed writes as the requirement promises.

**Suggested correction** · Record FR74 as partial. Credit the existing consistency operation and require Story 34.30’s revision-bound preview, fenced apply and deletion provenance before claiming delivery. [PRD](../prd.md#data-portability--system-health) · [Repair workflow](../../../src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs) · [Approved stories](../epics.md)

### Medium (9)

**PRD-M01 · L2 does not fix the stopwatch’s starting state** (Done-ness clarity, PRD 205, 208–213, 947, 1205)

The clock starts inside an existing EventStore service, but NFR31’s clean machine has only Docker and the SDK. The five steps then switch to a sample AppHost. Preparation time and the event-producing service can differ between runs.

**Suggested correction** · Version the starting service and sample, define permitted tools and caches at time zero, and require the event at the finish to come from that service. [PRD](../prd.md#measurable-outcomes)

**PRD-M02 · The NFR13 load test gives incompatible latency and throughput targets** (Done-ness clarity, PRD 1162, 1252)

“≤10% of its solo p95” asks for a tenfold speedup where the assumptions index permits 10% degradation. A 100-unit repair cannot sustain 25 units/minute throughout ten minutes.

**Suggested correction** · State the maximum mixed-to-solo latency ratio and define whether repair throughput is measured during active repair or over the entire window. Size the workload accordingly. [PRD](../prd.md#scalability)

**PRD-M03 · The public hybrid axis score has two meanings** (Done-ness clarity, PRD 572–582, 1188 and addendum 22)

The PRD describes a weighted contribution and the addendum gives a raw reciprocal rank. AD-9 and the implementation expose an unweighted, rank-one-normalized score. At rank one the public value is 1.0 and the raw term is 1/11.

**Suggested correction** · Separate public per-axis values from the weighted composite calculation and use a rank-one fixture across presenters. [PRD](../prd.md#ai-reliability-and-trust-boundaries) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Fusion engine](../../../src/Hexalith.Memories.Server/Search/FusionEngine.cs)

**PRD-M04 · The installation matrix designates the legacy CLI as its future tool** (Scope honesty, PRD 36, 705–707, 893)

The future column repeats the direct installation of `Hexalith.Memories.Cli`, even though the accepted migration names enrolled `Hexalith.McpCli` operations as the target.

**Suggested correction** · Label the installation row as compatibility guidance and describe the approved McpCli target and operation-level migration status locally. [PRD](../prd.md#technical-architecture-considerations) · [McpCli owner inventory](../../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md)

**PRD-M05 · Operator examples present unavailable commands as usable next actions** (Downstream usability, PRD 478–488, 800, 851–857, 903–911)

The empty-state flow directs the reader to a stub ingestion command. Status and verification examples also differ from the registered compatibility commands. These instructions can halt the manual journey before a user reaches search.

**Suggested correction** · Label aspirational interactions, use the native routes or working compatibility commands for current instructions, and update examples when an equivalent target operation is enrolled. [PRD](../prd.md#cli-specification) · [Command registration](../../../src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs)

**PRD-M06 · The package inventory omits a package and an executable host** (Shape fit, PRD 712–740 and addendum 90–93)

The manifest includes the EventStoreExtensions package and the non-packable DomainService host. The PRD also describes the Web component library as the runnable specimen.

**Suggested correction** · Reference or generate the inventory from the manifest. Identify DomainService’s command/replay role and the separate specimen host. [PRD](../prd.md#technical-architecture-considerations) · [Release manifest](../../../tools/release-packages.json)

**PRD-M07 · The service versioning statement contradicts its V1 routes** (Downstream usability, PRD 784–789, 918)

The document says “no versioned endpoints,” but native and Dapr-invoked methods use `/api/v1`. The Dapr application identifier and the route have different versioning rules.

**Suggested correction** · Document the unversioned application identifier, V1 HTTP method paths and additive compatibility contract separately. [PRD](../prd.md#technical-architecture-considerations) · [V1 route constants](../../../src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs)

**PRD-M08 · FR71 attributes the wrong export contract to Story 8.3** (Scope honesty, PRD 914, 995, 1010, 1115)

Story 8.3 delivered basic portable application export. Epic 26 covers operational backup and recovery. The delivery row conflates the two and can be read as credit for later origin, custody-transfer and source-cleanup requirements.

**Suggested correction** · Assign each delivery to its actual owner and identify the outstanding custody safeguards. Keep the early application feature outside MVP credit. [PRD](../prd.md#data-portability--system-health) · [Architecture decisions](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Approved stories](../epics.md)

**PRD-M09 · The FR2 and FR36 register omits the bounded DW-732 resolution** (Scope honesty, PRD 1009, 1016)

The same-day work register documents signed-URL display redaction in logs and case activity. The PRD still refers readers to the issue without recording what was verified.

**Suggested correction** · Credit that specific resolution and its evidence. Keep target CLI delivery and durable workflow-history protection listed separately. [PRD](../prd.md#functional-requirements) · [DW-732 disposition](../../../_bmad-output/implementation-artifacts/deferred-work.md)

### Low (2)

**PRD-L01 · Market defensibility is presented as an established outcome** (Substance over theater, PRD 643–669)

The competition table and causal-chain tests support technical differentiation. They do not establish the claim of a defensible product category.

**Suggested correction** · Describe defensibility as a hypothesis with an adoption signal, or omit the claim. [PRD](../prd.md#innovation--novel-patterns)

**PRD-L02 · The case membership example switches tenants mid-journey** (Downstream usability, PRD 374–384)

Journey 4 adds a member in `acme` and then queries the same case in `engineering` without establishing a tenant switch or a second case.

**Suggested correction** · Use one tenant for the successful path and a second tenant only in an explicit denial example. [PRD](../prd.md#user-journeys)

## Downstream story corrections

The following three findings apply to epic acceptance criteria. Their severity is tracked separately from the PRD grade and counts. [Epic definitions](../epics.md) · [Architecture spine](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md)

- **High** · Stories 34.32 and 34.38 assign retention cleanup to the wrong owner and stream. FR39 and NFR34 already specify the handoff. Update the story criteria so Platform Operations retires the partition and locator after verified expiry and writes the receipt to the dedicated retention-purge stream. (epics.md 7935–7948, 8216–8218)
- **High** · Stories 34.23 and 34.24 assume every tenant has a recoverable sample or an issued key. Align the tests with AD-16’s affirmative empty or never-provisioned evidence. Exercise unreadable evidence and restart after key destruction. (epics.md 7514–7521, 7561–7564)
- **Medium** · Story 34.40 still offers public exposure of telemetry and clock routes. Use AD-12’s internal caller boundary, wire registration and error mapping. Keep that inventory distinct from Story 34.33’s register guard. (epics.md 8292–8305)

## Independent reviews

- [PRD quality rubric](review-rubric.md) — Assesses seven dimensions and traces findings to the current PRD. PRD-C01, PRD-H01, PRD-H04, PRD-H05, PRD-H06, PRD-H07, PRD-H09, PRD-M02, PRD-M03, PRD-M04, PRD-M05, PRD-M06, PRD-M09, PRD-L01, PRD-L02.
- [Gate scoring and reproducibility](review-gates.md) — Walks each relevant gate as a reviewer would score it. PRD-C01, PRD-H01, PRD-H02, PRD-H03, PRD-H05, PRD-M01.
- [Source fidelity](review-source-fidelity.md) — Checks delivery claims against source, manifests, migration decisions and existing evidence. PRD-H04, PRD-H07, PRD-H10, PRD-H11, PRD-M03, PRD-M04, PRD-M05, PRD-M06, PRD-M07, PRD-M08, PRD-M09.
- [Downstream handoff](review-downstream.md) — Compares PRD instructions with the architecture, UX, epic acceptance criteria and Story 34.33. PRD-C01, PRD-H01, PRD-H03, PRD-H06, PRD-H08, PRD-M08.

## Mechanical notes and scope

- FR1–FR75 and NFR1–NFR37 each have one normative definition and one delivery-status entry. The ten journeys and nine gates have contiguous identifiers.
- All 13 original-input paths in the PRD frontmatter and the local Markdown links in the PRD and addendum were checked. The six live inline assumptions have index entries. The historical NFR identifier collision is marked index-only.
- The PRD was reviewed at repository revision 75c5b5f5, PRD SHA-256 0c535e96e18f993f10d553cd89c1ea2abf3bac20a34ba65c160a3708dbf5de6e, with working-tree architecture 3c7bf368 and the Story 34.33 spec. No runtime or release-qualification tests were run.
- September’s validation is preserved as an input to the change-controlled PRD. Severity counts here are unique current PRD/addendum findings across four reviews. Three additional epic corrections are recorded separately.
