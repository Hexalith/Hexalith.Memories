# Adversarial Review (finalize pass) — Hexalith.Memories PRD, 2026-09-08 afternoon

**Reviewed:** `prd.md` (updated 2026-09-08), `addendum.md` (updated 2026-09-08).
**Checked against:** `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`, `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`, `src/Hexalith.Memories.Server/Search/HybridSearchService.cs`, `tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs`, `tools/release-packages.json`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md`, `LICENSE`, `docs/dev/quickstart-walkthrough-log.md`, `README.md`.
**Prior review:** `review-adversarial-general.md` (2026-09-08 morning). Settled product-owner decisions (FR46–FR52 MVP with a Phase 1 population rule; N=8 as diagnostic and G1 as an unrun protocol with numbers; dated release record with `[ASSUMPTION]` dates; NFR9/packages/Aspire in the PRD; downstream drift as handoff) are **not** re-opened here. What is attacked is whether the text implements those decisions coherently and whether the brownfield claims survive contact with the repository.

## Verdict

Refuse to sign. The afternoon Update genuinely closed the three morning criticals as written: G1 now has N ≥ 50, a two-axis control, ΔNDCG@10 ≥ 0.02 and κ ≥ 0.6; FR46–FR52 have one phase and a population rule; there is a dated release record whose failure column says "no launch". But the document still cannot be handed to architecture, UX, and story authors, for three reasons that are each sufficient. First, the thesis gate is not executable as specified: the shipped hybrid search skips the graph axis whenever no start node is supplied, the Phase 1 CLI `search query` has no way to supply one, and the protocol never says how a topic acquires a graph seed — so G1 as written is either "control vs itself" (automatic kill) or "control vs label-seeded treatment" (automatic pass). Second, the release record declares the thesis increment "Delivered 2026-07-16" while the same PRD lists seven Phase 1 verbs as `NotImplementedCommand` stubs, and neither the stubs nor the N ≥ 50 benchmark nor the two-axis control has an owning story anywhere in `epics.md` or `sprint-status.yaml`; a gate whose prerequisites have no owner and whose "not run by the date" case has no consequence is a gate that cannot fail. Third, the PRD records "Apache 2.0 (decision, not a recommendation)" and instructs the README to publish a no-relicense pledge, while `LICENSE`, every source header, and every published package's `PackageLicenseExpression` say MIT. Fix those three and the remaining Highs, and this becomes signable.

## Findings

### Critical

#### C1. G1 has no graph-seeding rule, and the shipped hybrid drops the graph axis without one

- **PRD location:** § Executive Summary (lead paragraph); § Glossary "Hybrid / three-axis"; § Measurable Outcomes › Thesis-gate protocol (Population, Control); § CLI Specification › Phase 1 CLI surface (`search query` row); MVP Feature Set #3.
- **Evidence (PRD):** "runs one search that fuses syntactic (BM25), semantic (embedding), and graph retrieval and explains every score." Protocol: "Population: N ≥ 50 topics. A representative mix of developer/agent tasks … Control: … hybrid NDCG@10 exceeds the BM25+semantic (two-axis RRF) control by at least … 0.02." Nothing in the protocol says where the graph axis gets its entry point for a text topic. CLI row: "`memories search query --tenant --query [--case] [--axis] [--max-results] [--explain]` | 1 | Shipped (Story 7.2)".
- **Evidence (code):** `HybridSearchService.SearchAsync(... string? graphStartNodeId ...)`: "Graph traversal start node (required for graph axis, null to skip)"; runtime log "Graph axis skipped for tenant {TenantId}: graphStartNodeId is null". `SearchQueryCommand.Build` exposes exactly `--tenant --case --query --axis --max-results --explain`; no start-node option. `SearchEndpoints` accepts `graphStartNodeId` as a query parameter the CLI never sends. In the shipped suite, `ExecuteGraphTraversalAsync(tenantId, bq.GraphStartNodeId)` takes the seed from `ground-truth.json` — the same file that holds `expectedResults`.
- **Why it matters:** Three consequences. (a) The Executive Summary's first product sentence is false for the shipped Phase 1 CLI: a plain `search query` is BM25+semantic(+nl); the graph axis never runs. (b) G1 is unspecified at the only point that decides its outcome. If the protocol inherits the CLI behaviour, hybrid ≡ control and G1 fails by construction. If it inherits the benchmark behaviour, the treatment arm receives a seed chosen by the people who wrote the labels — the one thing the morning review called "label leakage" and the Update said it removed by demoting N=8. (c) Story authors for the "Epic 26 successor" will pick whichever seeding makes the number come out, because the PRD gave them no rule.
- **Fix:** Add a **Graph seeding** bullet to the protocol: the graph axis entry set for a topic is derived only from the topic text (e.g., seeds = top-k of the *control's own* BM25+semantic result list, k pre-registered; or a query-driven graph lookup) — never from labels or hand-picked node ids. Add a corresponding requirement to the `search query` row (or a new FR) so the Phase 1 CLI hybrid actually engages the graph axis, with delivery status "Not started". Rewrite the Executive Summary sentence to say what the shipped CLI does today and what G1 will test.

#### C2. "Thesis increment delivered" is contradicted by the PRD's own CLI table, and none of the remaining G1 prerequisites has an owner

- **PRD location:** § Measurable Outcomes › Release decision record (row 1, row 2); § MVP Feature Set #6 and note; § CLI Specification › Phase 1 CLI surface; § Functional Requirements › Delivery status register; addendum § Benchmark ("Expanding it into the G1 protocol is engineering work to be sprint-selected (Epic 26 successor)").
- **Evidence (PRD):** Release record row 1: "Thesis increment delivered (Epics 0–8) | Delivered 2026-07-16 per `sprint-status.yaml`". CLI table: `traverse`, `ingest`, `case create/delete/list`, `case add-member/remove-member/activity`, `tenant create/delete/verify`, `status --case/--failed` — all "**Not started** — `NotImplementedCommand` stub". Immediately below: "this PRD only records that the thesis surface is not complete until they are real" and "They are tracked in `epics.md` / `sprint-status.yaml`." Row 2 failure column: "Execute the kill-switch actions" — conditioned on *fails*, with no clause for *not run by 2026-10-31*.
- **Evidence (repo):** `RootCommandFactory.CommandGroups` stubs `ingest`, `traverse`, `case`, `explore` with `StoryId "7.2"`; `tenant` has only `list`; `status` has only `telemetry`. `sprint-status.yaml`: `epic-7: done`, all 7.x stories `done`; no story in any epic names these verbs. `epics.md` contains no story for the CLI stubs, no "Epic 26 successor", no N ≥ 50 corpus, no two-axis control (grep for `NotImplementedCommand|memories traverse|memories ingest|two-axis|BM25+semantic|N ≥ 50` returns nothing relevant). Epic 30 (the only backlog epic) is CI/CD. `sprint-status.yaml` has no 2026-07-16 delivery date for Epics 0–8; that date is the Epic 26 benchmark note.
- **Why it matters:** The release record is the instrument the morning review demanded. Its first row asserts a state the CLI table denies; its second row can be satisfied forever by not running G1, because "fails" ≠ "was not run" and nobody owns the work that would make it runnable (real corpus, two reviewers, two-axis control, seeding rule, the seven verbs the thesis journeys use). "Tracked in epics.md" is a false pointer. A story author reading this PRD will conclude the thesis increment is done and G1 is someone else's problem.
- **Fix:** Row 1 → "Thesis increment: server capabilities delivered (Epics 0–8); Phase 1 CLI surface incomplete — see FR53 stub list". Add to row 2: "**Not run by the decision date counts as a fail** unless a sprint-change proposal moves the date before it." Add a **Prerequisites** column or bullet listing, with story/epic IDs or the word *unowned*: (i) FR53 stub verbs, (ii) frozen real corpus N ≥ 50 + labelling, (iii) two-axis control implementation, (iv) graph seeding rule (C1). Replace "They are tracked in `epics.md`" with the truth until the stories exist.

#### C3. The license decision is contradicted by the repository the PRD is describing

- **PRD location:** § Project Classification ("License"); § Open-Source Licensing; Open Question 4.
- **Evidence (PRD):** "**License:** Apache 2.0 (decision, not a recommendation). Public README commitment: the project will not switch to a restrictive license." "The README must include a public commitment: *'Hexalith.Memories is committed to the Apache 2.0 license. We will not change to a restrictive license.'*" Open Question 4: "Confirm Apache 2.0 no-relicense README sentence with Jerome if it has not been published yet."
- **Evidence (repo):** `LICENSE` line 1: "MIT License / Copyright (c) 2026 Hexalith". Every reviewed source file header: "Licensed under the MIT license." `references/Hexalith.Builds/Hexalith.Package.props`: `<PackageLicenseExpression>MIT</PackageLicenseExpression>`; `Hexalith.Memories.Cli.csproj`, `.Mcp`, `.EventStore`, `.Aspire`, `.Telemetry`, `.ServiceDefaults` csproj files likewise. `README.md` contains no license statement at all. Packages have been published under semantic-release since Epic 11/12.
- **Why it matters:** A brownfield PRD that states a legal fact about shipped artifacts must state the true one. As written, a documentation story will publish a pledge to a license the packages do not carry, and the "Licensing de-risk strategy" reasons from the wrong baseline (SSPL/AGPL compatibility analysis is different for MIT vs Apache 2.0 only in patent terms, but the *pledge* is the problem). Open Question 4 asks whether the sentence was published; the real question is whether the project is relicensing.
- **Fix:** One of: (a) record a dated relicense decision (MIT → Apache 2.0) with an owning story that changes `LICENSE`, headers, `PackageLicenseExpression`, and the README, and mark the current packages as MIT until that ships; or (b) change the PRD to "MIT (as shipped)" and rewrite the README commitment as "will not move to a restrictive license" without naming Apache. Retire Open Question 4 either way.

### High

#### H1. FR25 still describes the diagnostic control and is marked Shipped; the gate's control has no FR and no code

- **PRD location:** FR25; § Delivery status register (Shipped row); § Measurable Outcomes › Control.
- **Evidence:** FR25: "Developer can run automated benchmark comparisons of hybrid vs **single-axis** search results with scored output." Status register: "Shipped | FR4–FR9, FR12–**FR25** …". Protocol: "Single-axis runs are diagnostics only." Code: `bestSingleNdcg = Math.Max(syntacticNdcg, semanticNdcg[, graphNdcg]); hybridOutperforms = hybridNdcg > bestSingleNdcg;` — no two-axis fusion path exists in the suite, and `--axis` in the CLI is single-valued (`syntactic|semantic|nl|graph|hybrid`), so BM25+semantic-only fusion is not producible from any product surface.
- **Why it matters:** The morning review asked that FR25 name the same control as the gate. It still names the other one and is stamped Shipped, which means the FR the gate depends on will never generate a story. Feature #7 says "Benchmark Suite (thesis-gate protocol …)" as an MVP must-have — and it is marked done via FR25.
- **Fix:** FR25 → "Developer can run the thesis-gate benchmark: hybrid vs BM25+semantic two-axis RRF control, NDCG@10 per topic, with single-axis results reported as diagnostics." Status → Partial (diagnostic suite shipped; protocol control not started). Add FR18-level control for a two-axis selection if the product surface is expected to expose it.

#### H2. L3's fixture of record and the entire `samples/` folder do not exist; L2's path has recorded high-severity residual gaps the record hides

- **PRD location:** § Measurable Outcomes › Causal Chain Completeness; § Phase 1.5 launch go/no-go (L2, L3); § Launch stopwatch step 4; § Developer Experience & Documentation › In-Repo Examples; Release record row 3.
- **Evidence (PRD):** "Validated by automated tests against a named set of known event chains (the Phase 1.5 sample, `samples/02-eventstore-integration/`, is the fixture of record)." Samples table, present tense: "`samples/01-quickstart/` … `samples/02-eventstore-integration/` … `samples/03-mcp-agent/`". Release record: "Epics 9–10 code delivered; gates not evaluated".
- **Evidence (repo):** `ls samples*` → "No such file or directory"; no directory matching `*sample*` outside `references/`. `sprint-status.yaml` 28-1 note: "Task 4's DW-713 deferral (real EventStore-originated Dapr publish full-stack proof) … DW-728 (… eventstore resource cannot start under SDK 10.0.400-only environments — a real, high-severity residual gap, accepted as story-closing …)".
- **Why it matters:** L3 is "Must pass" against a fixture that is not in the repository. L2 step 4 boots "the sample stack" that does not exist, and the repo's own ledger says the EventStore-originated publish proof was deferred and the eventstore resource has a known start failure. "Code delivered" is technically true and materially misleading for a launch decision dated 2026-11-30.
- **Fix:** Either point L3 at a fixture that exists today (name the Epic 9 test class and data file) or mark `samples/02-eventstore-integration/` "Not started — owner, story". Change the samples table header to "Planned in-repo examples" with a status column. Add DW-713 and DW-728 to the release record as L2 prerequisites.

#### H3. The dispute-resolution line still lets labels change after the score is known

- **PRD location:** § Measurable Outcomes › Thesis-gate protocol (Ground truth; Dispute resolution).
- **Evidence:** "Ground truth: Graded labels collected *after* queries exist, by Jerome plus 2 independent reviewers … Below that, relabel before scoring." Then: "Dispute resolution: Human review where automated score and reviewer judgment diverge; the human decision is logged with the run."
- **Why it matters:** The automated score *is* computed from the reviewers' labels. The only way "automated score and reviewer judgment diverge" is that someone looks at a per-topic NDCG they dislike and revisits the labels. Logging the override does not make it a gate; it makes it a documented waiver. The morning review said "delete … or confine it to label QA before lock"; the line survived intact.
- **Fix:** Replace with: "Labels are frozen (hash recorded) before any scored run. Disagreements are resolved during labelling, before freeze. Any post-freeze label change invalidates the run; a new run is scheduled and both are recorded."

#### H4. NFR16's 5-minute target is arithmetically impossible in the branch NFR16 exists to cover

- **PRD location:** NFR16; NFR5; § Embedding Provider Configuration (Google 1500 req/min); FR13.
- **Evidence:** NFR16: "every unit that reached `pending` or later is searchable again after restart — either because the projections survived (Redis AOF) or because they were **rebuilt from the EventStore commit** … all N are `indexed` within **5 minutes for 10K units/tenant**". NFR5: "Ingestion throughput | >100 memory units/min (payloads ≤10KB)". Google default rate limit: "1500 req/min". Rebuilding vectors for 10K units at 100–1500 units/min takes 7–100 minutes, not 5, unless vectors are part of the durable commit — which the PRD never says (Story 26.2 note: "export omits embeddings/NL descriptions → restore must re-embed").
- **Why it matters:** NFR16 is listed under "Key hard gates" in Technical Success and is "Verified" via Epic 26.7. The verified case is almost certainly the AOF-survived branch. The clause "AOF is an optimisation, not the durability contract" then promises a rebuild-branch outcome that the stated numbers cannot deliver.
- **Fix:** Split the target: "AOF-survived: `indexed` within 5 min for 10K units. Rebuilt-from-commit: `indexed` within ⌈N / NFR5 throughput⌉ + 5 min, or within 5 min if vectors are persisted with the commit (state which)." Say which branch Epic 26.7 exercised.

#### H5. What a thesis kill does to Phase 1.5 is undefined in both duration and logic

- **PRD location:** § Measurable Outcomes › kill-switch actions; Release record row 2 ("If it fails"); § Phase 1.5 — Fast-Follow heading and "Hard commitment"; § Risk Mitigation (Market Risks).
- **Evidence:** Kill switch: "remove graph from default `axes=hybrid`; freeze FalkorDB for causal traversal **or** cut it from general search; change README positioning *before* any Phase 1.5 launch decision". Row 2: "no Phase 1.5 launch decision is taken" — no end condition. Heading: "Phase 1.5 — Fast-Follow (committed: within 4 weeks of thesis validation)". Row 3 has its own date, 2026-11-30, independent of whether row 2 passed.
- **Why it matters:** Causal traversal (L3) does not need fusion; MCP (L1) does not need the graph axis. So after a thesis kill, L1–L3 remain logically passable, and the "or" in the kill switch permits keeping FalkorDB for exactly that. The record says "no launch decision is taken" but not for how long or under what re-entry rule, while the L-row keeps its date. Two readers will reach opposite conclusions: "1.5 is dead" and "1.5 launches on 11-30 as a causal product". The morning review's "thesis fail then a four-week 1.5 commitment" is still structurally present.
- **Fix:** State one rule. Either: "Thesis fail blocks the L1–L3 decision until a re-run passes or an SCP re-baselines the thesis; the 2026-11-30 date is void on a thesis fail." Or: "Thesis fail demotes hybrid; Phase 1.5 may still be decided on 2026-11-30 as a causal-traversal + MCP product with graph out of default fusion." Delete "committed: within 4 weeks of thesis validation" — the dated record supersedes it.

### Medium

#### M1. "Verified" is granted to NFRs whose verification text was rewritten in this Update

- **PRD location:** § NFR delivery status (Verified row); NFR8; NFR21; § Delivery status register (FR44 row); Release record row 2 ("G2/G4/G5 have automated suites").
- **Evidence:** NFR8 verification (new today): "Automated suite driven by *principals*, not ids … Graph-specific test: identical graph structures in A and B, traverse as A, zero B nodes even if edge ids collide. Re-run when Epic 24 (tenant-scoped principals) closes." Status: "Verified | NFR8 (Epics 5, 20, **24-in-progress** isolation suites)". FR44: "NFR8 evidence must be re-run when Epic 24 closes." NFR21 (new today): "a documented negative test showing what a non-conforming envelope does" — status Verified via "Epics 9–10 conformance tests".
- **Why it matters:** A verification restated at noon cannot have been verified by a suite recorded in July unless the PRD cites the test that already matches. "Have automated suites" is not "passed the restated NFR8". G2 will be stamped on the old test.
- **Fix:** For NFR8 and NFR21 either cite the test class that implements the restated verification (colliding-edge-id traversal; non-conforming envelope negative test) or move both to "Implemented, verification run not recorded". Mark G2 "suite exists; re-run against restated NFR8 pending Epic 24".

#### M2. G3 rests on a walkthrough with zero recorded runs, and "clean machine" is undefined

- **PRD location:** Release record row 2 ("G3 rests on the Story 7.4 quickstart walkthrough"); NFR31; § NFR delivery status (NFR31 row); Journey 9.
- **Evidence:** `docs/dev/quickstart-walkthrough-log.md`: "| _pending_ | _pending_ | _pending_ | No walkthrough has been recorded yet." `README.md` line 7: "under 30 minutes on a clean machine (NFR31 — approximate …)". PRD NFR31: "clean machine with Docker installed (AppHost → first CLI search)". MVP embedding: Google `text-embedding-004` only; nothing says whether obtaining a Google API key is on the clock. .NET SDK, Aspire CLI, and Docker image pulls (NFR7 excludes pulls; NFR31 does not say) are unspecified.
- **Why it matters:** G3 is "Must pass" and the record implies it has a basis. It has a procedure and no measurement, and the procedure's start state is whatever the runner had installed.
- **Fix:** In NFR31 define the clean-machine spec (OS class, preinstalled: Docker only / Docker + .NET SDK; network assumed; embedding key acquisition on or off the clock — or a documented no-network embedder for the quickstart). Record row 2: "G3: procedure exists; no timed run recorded."

#### M3. The Phase 1 thesis journey uses two CLI syntaxes, one of which does not exist

- **PRD location:** Journey 9 (Opening Scene vs Climax; case-create hint); Journey 2; Journey 10; § CLI Specification.
- **Evidence:** Journey 9 opening: "`memories search query --tenant pilot --query "anything"`" (correct). Journey 9 climax: "`memories search "water damage" --case claims-pilot`" (no `query` subcommand, no `--tenant`, positional query — not a shipped shape; `--tenant` is `Required = true`). Journey 9 case-create response: "Start building knowledge: ingest documents, **subscribe to event topics**, or add files from a directory" — one paragraph after "Event auto-indexing is not offered in this hint until Phase 1.5 ships." Journey 2: "`memories search "Henderson" --case claims-q1`". Journey 10: "`memories search --explain`".
- **Why it matters:** Journey 9 is declared "the Phase 1 thesis success path"; UX and story authors copy journey commands into acceptance criteria. The PRD forbade a third ingestion vocabulary and then keeps two CLI grammars.
- **Fix:** One grammar everywhere: `memories search query --tenant <t> --query "<q>" [--case <c>] [--explain]`. Remove "subscribe to event topics" from the Phase 1 case-create hint.

#### M4. The non-packable table disagrees with the "sole release inventory", and the Web project is unmentioned

- **PRD location:** § Package Distribution › Non-packable hosts; Non-Goals (REST search UI Phase 2); NFR32/NFR35 status ("Epic 17 delivered components").
- **Evidence:** PRD table: `Hexalith.Memories.Server`, `Hexalith.Memories.AppHost` (2 rows). `tools/release-packages.json` `nonPackableProjects`: Server, AppHost, **Web**, **AccessTelemetry**, **AccessTelemetry.Clock**, **AccessTelemetry.Contracts** (6). `src/Hexalith.Memories.Web` exists; `sprint-status.yaml` `epic-17: done` including "17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure". Addendum: "Do not invent a '9+3' slogan that disagrees with those two lists."
- **Why it matters:** The PRD names the JSON as the source of truth and then publishes a different list. A shipped Web specimen is a product-shaped artifact the PRD calls "Future web" without saying what it is or is not.
- **Fix:** Mirror all six `nonPackableProjects` rows. Add one sentence: "`Hexalith.Memories.Web` is an Epic 17 conformance specimen, not an activated surface; NFR32/NFR35 activate when it is."

#### M5. NFR25 permits what NFR26 forbids, and NFR26 is demanded on non-deterministic real embeddings

- **PRD location:** NFR25; NFR26; G5; § Thesis-gate protocol (Automated scoring; Population "real, not synthetic, content").
- **Evidence:** NFR25: "Result ordering within the same score tier may vary." NFR26: "running benchmarks twice … yields identical NDCG@10 scores." Protocol: "NDCG@10 per topic, reproducible under NFR26" on a corpus with real content — i.e., live embedding calls, whereas the shipped suite is reproducible only because "Results use synthetic pre-computed vectors, not real embeddings" (`BenchmarkSuiteTests.Caveat`).
- **Why it matters:** NDCG@10 depends on the order of tied items when a tie straddles rank 10 or when tied items carry different graded labels, so NFR25's permitted variance can violate NFR26. Separately, real embeddings from a hosted provider are not bit-stable across model updates; NFR26 on G1 is unachievable unless vectors are frozen with the corpus.
- **Fix:** NFR25 → deterministic tie-break by memory-unit id (the benchmark already does this for the graph axis). Protocol → "embedding vectors for the frozen corpus and topics are computed once, cached, and hashed with the corpus; scoring runs use the cache."

#### M6. G1 has no aggregate or regression guard, and the "thesis-stress slice" has no size

- **PRD location:** § Thesis-gate protocol (Population, Control); User Success row "Hybrid beats the realistic alternative".
- **Evidence:** "A topic counts as a hybrid win when hybrid NDCG@10 exceeds the … control by at least … 0.02." "80% of topics is the hard line." "Thesis-stress queries are a labelled diagnostic slice inside N, not the whole suite." No bound on the other 20 %; no proportion for the slice.
- **Why it matters:** 40 topics winning by 0.02 and 10 topics losing by 0.5 is a pass with a large net regression. A slice with no size can be 2 topics or 48.
- **Fix:** Add "mean ΔNDCG@10 over all N ≥ 0.00 and no topic regresses by more than 0.10" (numbers are yours to set; the PRD must own them, as it says). Set the stress slice to a fixed share (e.g., ≤ 20 % of N) and pre-register it.

#### M7. L1 defines the budget rule and not the task rule

- **PRD location:** L1; User Success "LLM Agent | Respects token budget"; Journey 7 success criteria.
- **Evidence:** L1: "MCP end-to-end: agent task on a held-out query set (topics not used in G1 labelling, ≥10) within token budget (FR23/FR54/FR58) | Must pass." Journey 7: "Agent produces responses that are sourced, attributed, within token budget, and causally grounded when graph data is available."
- **Why it matters:** Budget compliance is measurable (100 %). "Agent task" success is not defined: which agent, which scorer, what counts as a correct answer. L1 can be passed by any agent that returns short output.
- **Fix:** "Pass = 100 % budget compliance **and** ≥ X/10 topics where the agent's answer cites at least one memory unit graded relevant for that topic (scorer: the G1 labels; agent: named reference configuration in `samples/03-mcp-agent/` once it exists)."

#### M8. The Phase 1 graph population is mostly derived from the semantic axis, and the protocol does not require otherwise

- **PRD location:** § MVP Feature Set › Phase 1 graph inventory; § Edge Type Taxonomy (`references` row); § Thesis-gate protocol (Population).
- **Evidence:** "`references` (explicit link or **AI-inferred similarity**)"; `contains` is case membership (already a `--case` filter); `caused_by`/`correlated_with` "only when ingested metadata already carries CausationId/CorrelationId". Protocol: "the thesis-gate corpus must reflect the Phase 1 population, not a synthetic causal graph" — with no requirement to report or bound the share of derived edges.
- **Why it matters:** If the frozen corpus's non-`contains` edges are overwhelmingly AI-inferred similarity, the graph axis is a function of the semantic axis and G1 tests "semantic-neighbour rerank vs semantic". That is a legitimate result to obtain, but the PRD should say it is what is being tested, or require explicit edges.
- **Fix:** Add to Population: "The frozen corpus records edge counts by type and source (explicit / metadata-carried / AI-inferred). If explicit + metadata-carried edges are < Y % of non-`contains` edges, the run is recorded as testing similarity-graph fusion, and the Executive Summary thesis sentence must say so."

#### M9. Three lists of the thesis verbs, and "CLI is the operational superset" is false today

- **PRD location:** MVP Feature Set #6; § Phase 1 CLI surface table; § Phase 1.5 — Fast-Follow #3; § Executive Summary ("CLI is the operational superset"); § Interface Capability Parity Matrix.
- **Evidence:** Feature #6: "`search query --explain`, `traverse`, `ingest`, `case create/delete/add-member/activity`, `tenant create/delete/verify/list`, `status`, `quickstart`, `consistency verify`" — omits `case list`, `case remove-member`, `search inspect/lookup`, `status telemetry`, `config show`, `consistency inspect/repair` that the CLI table marks Phase 1. Phase 1.5 #3: "CLI expansion: `explore`, **`handlers`**" — CLI table: "`memories handlers list/mismatches` | 1.5 | **Shipped (9.3)**". Parity matrix: CLI `memories ingest` vs MCP `ingest_content`; CLI `ingest` is a stub, MCP ingest shipped (Epic 10 done).
- **Why it matters:** The Update said the CLI table is "the FR53 source of truth" and then kept a second, shorter list in Feature #6 and a third in Phase 1.5. The morning finding "the CLI contract is two lists" is now three.
- **Fix:** Feature #6 → "Phase 1 verbs exactly per the CLI surface table." Drop `handlers` from Phase 1.5 expansion. "CLI is the operational superset (target; today MCP exposes ingest while the CLI verb is a stub)."

#### M10. The Interpretation layer is made responsible for "complete edge graphs" the PRD says Phase 1 cannot promise

- **PRD location:** § Compliance Boundary › Three-tier responsibility model; § What Makes This Special; § Responsibility boundary.
- **Evidence:** "Interpretation (Memories) | Accurate embeddings, correct causal chains, documented score semantics, **complete edge graphs**". Versus: "what Phase 1 cannot promise is that the graph *contains* causal edges for a folder of files" and "Phase 1 does not claim causal completeness."
- **Why it matters:** Compliance readers take the responsibility table literally. "Complete edge graphs" is a completeness warranty the rest of the document explicitly withholds.
- **Fix:** "complete traversal of indexed edges, with explicit gap markers for missing nodes (FR49)".

### Low

#### L1. Front matter still `status: draft`
- **Location:** YAML front matter; § 0. Document Purpose.
- **Evidence:** `status: draft` after a second "validation Update" and `step-12-complete`.
- **Fix:** Set to the control state the body claims (`change-controlled`).

#### L2. Two names for the decision owner
- **Location:** Release record (`[ASSUMPTION: … pending Administrator confirmation]`); Open Question 1 ("Owner: Jerome").
- **Fix:** Pick one noun and add it to the Glossary if it is a role.

#### L3. The "benchmark README" does not exist; the shipped test is still named as a thesis gate
- **Location:** § Measurable Outcomes ("the benchmark README restates them"); addendum § Benchmark.
- **Evidence:** `tests/Hexalith.Memories.Benchmarks/` has no README. The CI test is `ThesisValidation_HybridOutperforms80Percent` asserting `ThesisValidated` (`winRate >= 0.80` vs best single axis) and passes.
- **Fix:** Add a handoff line: "the shipped test must be re-tagged/renamed *diagnostic*; a passing `ThesisValidation_*` test is not G1 evidence."

#### L4. Licensing de-risk pins FalkorDB "in the default docker-compose.yml"
- **Location:** § Licensing de-risk strategy #2.
- **Evidence:** No `docker-compose*` in the repo; the addendum says Aspire AppHost is the orchestration path.
- **Fix:** "in the AppHost resource definition".

#### L5. Journey 7 asserts a 90-day staleness threshold that NFR33 says the contract owns
- **Location:** Journey 7 ("not updated in >90 days"); NFR33.
- **Fix:** "per the NFR33 thresholds".

#### L6. Narrative numbers presented as measurements
- **Location:** Journey 1 ("14 minutes", "under a minute"); Journey 2 ("15-minute fix"); Journey 5 ("8 seconds", "under 10 minutes").
- **Fix:** Add one line to § User Journeys: "Durations in journeys are illustrative; gates cite NFR/L numbers only."

#### L7. Business Success thresholds have no measurement source
- **Location:** § Business Success ("GitHub stars", "NuGet downloads", "EventStore integration users | 5+ projects").
- **Evidence:** No named counter for "projects referencing `Hexalith.Memories.EventStore`" (User Success says "Tracked via NuGet dependency graph", which does not exist as a public API for private projects).
- **Fix:** Name the source per row or label the table "signals, not gates".

## Mechanical notes

- Journey 7 uses `axes="hybrid"` / `axes` while the CLI table and code use `--axis`; MCP parameter naming should be declared in one place.
- § Phase 1 CLI surface is titled "Phase 1 CLI surface (thesis…)" but contains Phase 2 (`export`) and 1.5 (`handlers`, `explore`) rows; retitle "CLI surface with phase and delivery status".
- Release record row 1 cites "Delivered 2026-07-16 per `sprint-status.yaml`"; the file has no such date for Epics 0–8 (the date is the Epic 26 benchmark note).
- README line 84 says "per-tenant audit events (FR67)"; the Glossary bans "audit" for this capability *in the PRD*. Downstream, but worth a handoff line since the PRD's own FR67 text is now clean.
- Feature #6 says `consistency verify`; the CLI table says `consistency verify/inspect/repair` shipped — align.
- Journey 4 is tagged Phase 2 in its heading; Journey 8 heading still carries no phase tag (the summary table does).
- `[ASSUMPTION]` tags in the Release record say "Administrator confirmation"; the Assumptions Index repeats it — keep both or neither once L2 is resolved.
- Open Question 2 revisit date 2026-09-30 precedes G1 decision date 2026-10-31 by 31 days; recruiting reviewers, freezing a real corpus, labelling ≥ 50 topics with κ ≥ 0.6, and implementing a two-axis control plus a seeding rule in that window is a schedule assumption that should be tagged `[ASSUMPTION]`.

## What this pass confirms as closed (from the morning review)

- Kill switch now has N, Δ, κ, unit of the 80 %, and a two-axis control in the PRD; the N=8 run is labelled diagnostic in the PRD and the addendum.
- FR46–FR52 have one phase and a stated Phase 1 population rule; the taxonomy heading no longer says "MVP minimum" without qualification.
- "Soft gate" tier deleted; five hard gates.
- Launch stopwatch names `Hexalith.Memories.EventStore`, a published ID; `Hexalith.Memories.Client` is gone from Journey 1.
- Compliance chapter no longer says "physical"; NFR8 verification is principal-driven in text.
- FR67 no longer says "audit"; Member has a Phase 1 outcome; NFR21 is narrowed to EventStore conventions; score-table graph row is confined to single-axis; `tenant switch` removed; NFR36 exists; one ingestion-state vocabulary with the `Contracts.V1` mapping declared.
