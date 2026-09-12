# Source and Implementation Drift Review — Hexalith.Memories UX Spines

**Run:** 2026-09-12  
**Lens:** source and implementation drift  
**Reviewed:** `DESIGN.md`, `EXPERIENCE.md`, all five frontmatter sources, the live PRD, `.memlog.md`, the current protocol/UI extracts, the PRD/legacy/validation reconciliations, and the current CLI, MCP, `Contracts.V1`, and Web/RCL source.

## Overall verdict

**REVISE — the pair is not yet source-safe for downstream implementation.** The migration successfully establishes the two-spine shape, keeps the active product CLI-first, preserves fail-closed tenant semantics, uses the correct four MCP names and current CLI registrations, inherits FrontComposer/Fluent V5, and quarantines the historical HTML. However, the live PRD advanced after the extraction/reconciliation that declared the pair complete. The result is one wrong journey identifier, an omitted no-go/G6/NFR37 posture, a resolved graph-start decision still presented as open, machine-readable component mappings that do not match current Razor, and current-component behaviors that the versioned packet cannot carry.

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 6 |
| Medium | 3 |
| Low | 1 |

Downstream implementation should wait until the six High findings are corrected and the PRD reconciliation is rerun against a pinned source revision.

## Method and scope

- Read both spines in full and resolved every frontmatter source: `../../prd.md`, `../../ux-design-specification.md`, `../../ux-design-directions.html`, `../../ux-validations/ux-memories-2026-09-09/validation-report.md`, and `../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md` (`DESIGN.md:5-10`; `EXPERIENCE.md:5-10`).
- Treated the 1,243-line, change-controlled PRD as product/phase authority; repository UX instructions as UI-system authority; `Contracts.V1` and direct source inspection as serialized-name/current-delivery authority; and the May specification/HTML plus September 9 validation as subordinate historical evidence (`../../prd.md:1-42`; `EXPERIENCE.md:18-22`).
- Compared the 49-entry process log and all current working evidence: `.working/extract-prd.md`, `.working/extract-protocol-implementation.md`, `.working/extract-ui-implementation.md`, `.working/distill-open-items.md`, `.working/reconcile-prd.md`, `.working/reconcile-validation.md`, and `.working/reconcile-legacy-ux.md`.
- Rechecked the four MCP tools, CLI registrations/placeholders, `EvidencePacket` graph/metadata types, the 19 Razor components, their conformance allowlist, and the lens/recovery mappers rather than accepting the extracts as current proof.
- Severity reflects the likely downstream impact of consuming the spine as a build contract, not the amount of prose needed to fix it.
- Location convention: planning-artifact paths are relative to this review; implementation basenames are unique files under `src/Hexalith.Memories.*` or `tests/Hexalith.Memories.*` and are paired with exact line locations.

## Findings

### Critical

None.

### High

#### H1 — J8 no longer mirrors the exact authoritative journey name

**Evidence.** The live PRD now names Journey 8 **`Priya — "I Need to Understand This Case" (Phase 2 downstream application)`** (`../../prd.md:447`). `EXPERIENCE.md` still names it **`(Phase 1)`** and says that stale phrase is the exact source heading (`EXPERIENCE.md:363-365`). The other nine journey headings match the current J1–J10 headings (`../../prd.md:307-487`; `EXPERIENCE.md:272-389`). The false title is propagated by `.working/extract-prd.md:76`, `.working/reconcile-prd.md:41`, and the earlier process assertion in `.memlog.md:17`.

**Impact.** Consumers asked to key requirements or tests by exact journey identifier will bind J8 to a nonexistent current title and may reintroduce a Phase 1 web scope the PRD explicitly removed.

**Fix.** Change the J8 heading and phase sentence to the current PRD wording, remove the stale-heading explanation, then regenerate the extraction and reconciliation. Count the current result as 9/10 exact until that is done.

#### H2 — The current no-go/G6/NFR37 posture is missing, and semantic parity is phrased as current fact

**Evidence.** The PRD declares the release **no-go**, expands the thesis gate to hard gates G1–G6, identifies absent evidence/unowned work, and leaves the 2026-12-01 decision date as an assumption (`../../prd.md:62`, `:180-215`). It also records NFR37 as not started/current-contract evidence absent (`../../prd.md:1113-1123`) and makes AD-14 phasing plus architecture binding of FR75/NFR37 explicit G6 blockers (`../../prd.md:1219`, `:1227-1228`). The phase matrix lists incomplete/preview states but never states the overall no-go, G6, or those architecture blockers (`EXPERIENCE.md:24-36`). In a section titled “Exact current CLI grammar and status,” it says human/table/stderr/JSON/exit-code forms preserve the same meaning (`EXPERIENCE.md:38`, `:61`), although that cross-form assertion is the unverified target of NFR37 (`../../prd.md:1211-1215`). The later accessibility section repeats the desired target without restoring its current status (`EXPERIENCE.md:243-252`). J1 only warns against launch before L1–L3 (`EXPERIENCE.md:283`), not the prerequisite G1–G6 release decision.

**Impact.** Release reviewers can read a phase-incomplete but generally active posture where the authority says no-go, and implementers can treat the NFR37 parity requirement as shipped instead of unverified work.

**Fix.** Add one explicit release-governance row/note: current posture no-go; all G1–G6 are hard; the date is an assumption; G6 prerequisites lack successor-story ownership/current evidence and await AD-14 plus FR75/NFR37 architecture binding. Separate observed exit-code/envelope behavior from the unverified NFR37 semantic-parity target, and include NFR37 in the point-of-use maturity register.

#### H3 — A closed search-start decision is still “unresolved,” and graph-start provenance has no V1 field

**Evidence.** The live PRD is now unambiguous: public search has no start-node input; hybrid auto-seeds; explicit starts belong to `traverse` / `traverse_relations`, not search `--from` (`../../prd.md:167`, `:1024`). The spine still calls search `--from` placement unresolved and says evidence labels a graph start as “explicit or auto-seeded” (`EXPERIENCE.md:214-216`). The current `EvidencePacketGraphSummary` has only `available`, `relatedPath`, `edgeTypes`, and `gapMarkers`; it has no start-provenance field (`src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs:110-119`; `.working/extract-protocol-implementation.md:137`). The stale rule also survives in `.working/distill-open-items.md:10` and `.working/reconcile-prd.md:97`. It originated in the older validation’s request for graph-start provenance (`../../ux-validations/ux-memories-2026-09-09/validation-report.md:92-96`), but the current PRD wins by the spine’s own precedence rule.

**Impact.** A CLI designer can reopen a settled grammar decision or invent a serialized provenance field, breaking both current search grammar and the versioned Evidence Packet.

**Fix.** Remove the unresolved search `--from` statement. State that search is auto-seeded and explicit starts are traversal inputs. If a future presentation labels provenance from known invocation context, call it presentation-derived context and not a `Contracts.V1` wire field; otherwise show no provenance label until the contract carries one.

#### H4 — Nine machine-readable primitive mappings do not match the current components they claim to bind

**Evidence.** `DESIGN.md` calls its 19 entries the complete **current** RCL conformance inventory and says the exact types already exist (`DESIGN.md:203-205`), so the `primitive` leaves are actionable current bindings. At least nine are inaccurate:

- Source Citation Stack names `FluentStack`/`FluentButton`, but the component uses `FluentText` plus semantic lists and exposes no button (`DESIGN.md:55-61`; `MemoriesSourceCitationStack.razor:4-43`; `Epic17ConformanceAllowlist.cs:103-104`).
- Retrieval Axis Breakdown names `FluentStack`/`FcStatusBadge`, and Graph Path Summary names `FluentStack`; neither uses those primitives (`DESIGN.md:62-75`; `MemoriesRetrievalAxisBreakdown.razor:4-45`; `MemoriesGraphPathSummary.razor:4-46`; `Epic17ConformanceAllowlist.cs:97-100`).
- Command Surface names `FluentText` rather than its actual `FluentLabel`; Shared Lens Shell names `FluentText` and Context Navigation rather than its actual `FluentLabel`/`FluentButton` composition (`DESIGN.md:104-110`, `:125-131`; `Epic17ConformanceAllowlist.cs:117-120`, `:130-131`; `MemoriesLensShell.razor:31-97`).
- Ingestion Lifecycle Tracker names `FluentProgressBar`/`FluentText`, Operator Health Matrix names `FluentDataGrid`, Benchmark Result Comparator names `FluentText`, and Agent Packet Inspector names `FluentText`; those elements are absent from the respective current Razor compositions (`DESIGN.md:139-166`; `Epic17ConformanceAllowlist.cs:122-133`).

**Impact.** This is machine-readable drift in `DESIGN.md`, not editorial shorthand. A downstream generator or implementer will select nonexistent composition requirements and can regress the conformance-tested Fluent/FrontComposer boundary.

**Fix.** Replace every `primitive` leaf with the exact current allowlisted composition, or introduce an explicit `delivery: target`/`current-primitive` split if the desired future binding intentionally differs. Revalidate all 19 entries mechanically against the Razor/allowlist before calling the inventory current.

#### H5 — “Current RCL” behavioral rows promise actions and data the packet/mappers cannot provide

**Evidence.** The component table introduces its rows as behavioral peers of the current conformance components (`EXPERIENCE.md:105-109`) but mixes target product anatomy into that current framing:

- Source Citation Stack promises an inspect action and explicit conflict state, while the component is read-only and has no callback/button (`EXPERIENCE.md:114`; `MemoriesSourceCitationStack.razor:4-60`).
- Graph Path Summary promises directional edges, chronology, case attribution, and confidence, while its V1 input contains only a path-id list, edge-type list, and gap-marker list (`EXPERIENCE.md:116`; `EvidencePacket.cs:110-119`; `MemoriesGraphPathSummary.razor:20-44`).
- Case Activity Trail promises time-ordered ingestion/search/membership/change events with actor/evidence impact, while its mapper can emit only packet sources, annotation counts, graph relations/gaps, trust state, and recovery (`EXPERIENCE.md:125`; `CaseActivityTrailMapper.cs:18-21`, `:42-161`).
- Ingestion Lifecycle Tracker promises acknowledgement, revision/config completion, queue delay, last update, retry count, and failure stage, but its mapper consumes only optional source stage metadata and drops `UpdatedAt`/`RetryCount` from its row (`EXPERIENCE.md:126`; `EvidencePacketIngestionMetadata.cs:10-19`; `IngestionLifecycleMapper.cs:59-140`).
- Operator Health Matrix promises liveness, readiness, fairness, telemetry, and repair, but its fixed checks are isolation, authorization, retrieval backend, axis availability, graph context, and detail completeness (`EXPERIENCE.md:127`; `OperatorHealthMatrixMapper.cs:14-22`, `:39-64`).
- Benchmark Result Comparator promises the BM25+semantic control, delta, κ, label freeze, and aggregate guards, but the packet metadata has only four axis NDCGs, one threshold/pass value, run/corpus metadata, per-query hybrid values, and an evidence URI (`EXPERIENCE.md:128`; `EvidencePacketBenchmarkEvidence.cs:10-33`; `BenchmarkResultComparatorMapper.cs:77-97`, `:100-132`).

The global target qualifier at `EXPERIENCE.md:36` does not adequately disambiguate these point-of-use claims because the table explicitly invokes the current implementations.

**Impact.** Teams can accept the specimen as evidence for features its DTO and mapper cannot express, or add ad hoc fields/actions outside `Contracts.V1` to satisfy the spine.

**Fix.** Split each affected row into “current specimen projection” and “target on product activation,” with absent data/actions named explicitly. Keep exact current mapper behavior in the first column and phase-/contract-gate the richer target behavior.

#### H6 — Current Web conflict mapping contradicts the spine’s trust vocabulary and is not carried as live-code divergence

**Evidence.** The spine correctly says conflicting sources are a separate evidence condition rather than packet state or relevance strength (`EXPERIENCE.md:146`). Current Web code cannot observe a source-conflict field. Instead, it maps any answer with `evidence.Degraded` to `Conflicting` and maps a `Complete` packet with any unavailable axis to `Conflicting` (`RecoveryStateMapper.cs:85-90`, `:141-160`). The presentation enum itself defines conflict as sources, backend health, **or** axes disagreeing (`RecoveryStateKind.cs:37-41`), and tests lock this behavior (`RecoveryStateMapperGapTests.cs:113-120`; `RecoveryStateMapperTests.cs:28-29`). Neither `.working/extract-ui-implementation.md` nor the “15/16” divergence claim records this contradiction (`.working/reconcile-prd.md:101-109`).

**Impact.** Users can be told sources conflict when only a backend or retrieval axis is unavailable. That changes the meaning of evidence and can drive the wrong recovery action in a trust-critical surface.

**Fix.** Mark this as a current implementation divergence at the component/recovery point of use. Do not claim current conflict conformance. A later code change should reserve conflict for a contract-backed conflict signal and classify backend/axis loss as degradation; until then, the UX contract must describe the current limitation explicitly.

### Medium

#### M1 — The working evidence and memlog still certify a superseded PRD snapshot

**Evidence.** The extraction says the PRD is 1,225 lines, NFR1–NFR36, G1–G5, and retains a stale J8 heading (`.working/extract-prd.md:3`, `:9`, `:20`, `:76`); the live file is 1,243 lines and now contains NFR37/G6/current J8 (`../../prd.md:40`, `:180-191`, `:447`, `:1211-1215`). The reconciliation still reports PASS, 10/10 exact, 0 blockers, NFR1–NFR36, and an unresolved search `--from` (`.working/reconcile-prd.md:1-23`, `:41`, `:79-81`, `:97`). The process log likewise says J8 is reconciled, `--from` is unresolved, and the current PRD reconciliation has zero blockers (`.memlog.md:17-18`, `:40`, `:47`).

**Impact.** Reviewers can cite a green reconciliation that demonstrably predates the authoritative content despite sharing the same date.

**Fix.** Regenerate the PRD extraction/reconciliation from the 1,243-line revision, invalidate the old counts, and append a supersession entry to the memlog. Do not rewrite prior log history.

#### M2 — Source precedence is circular without a revision identity

**Evidence.** Both spines cite the live PRD and say it takes precedence (`DESIGN.md:5-10`; `EXPERIENCE.md:5-10`, `:20`). The current PRD lists these same two spines as its own inputs and says it incorporated their final changes (`../../prd.md:7-18`, `:36`). All three are dated only 2026-09-12, with no revision hash or snapshot identifier.

**Impact.** A later edit can make either direction appear authoritative, and same-date reconciliations cannot prove which text was actually compared.

**Fix.** Record an immutable PRD revision/hash in the spine source metadata and a distinct derived-from revision in the PRD, or establish a one-way lineage rule for this migration cycle. Reconcile only after that revision is pinned.

#### M3 — The “Exact MCP tool contract map” omits schema constraints needed for exact reuse

**Evidence.** Tool/type names and delivery profiles are correct (`EXPERIENCE.md:63-74`), but `search_memory.maxResults` omits its 1..100 clamp, and `traverse_relations.depth` omits its 0..10 clamp and exact `edgeType` literals (`EXPERIENCE.md:67-72`). Those constraints are present in the implementation extract and code (`.working/extract-protocol-implementation.md:18-28`, `:61-74`; `SearchMemoryTool.cs:22-26`, `:65-66`; `TraverseRelationsTool.cs:20-24`, `:58-65`).

**Impact.** Schema/help examples derived only from the “exact” spine can expose invalid or misleading input guidance, although the server clamps/rejects safely.

**Fix.** Add the ranges and literals, or rename the section to a selected UX contract map and require the versioned schema/extract for validation details.

### Low

#### L1 — Validation-reconciliation links are mechanically broken from `.working/`

**Evidence.** `.working/reconcile-validation.md:5-7` links to `../../ux-validations/...`, `.working/extract-validation.md`, `DESIGN.md`, and `EXPERIENCE.md` as though the file lived one directory higher; the same wrong bases recur throughout (`:21-67`, `:73-82`). From `.working/`, the correct bases are `../../../ux-validations/...`, `extract-validation.md`, `../DESIGN.md`, and `../EXPERIENCE.md`.

**Impact.** The disposition evidence cannot be followed in rendered review tooling, weakening auditability but not changing product behavior.

**Fix.** Correct the relative links when the reconciliation is regenerated.

## Mechanical notes

- Both frontmatters carry the same five source entries, and all five resolve to existing files from the spine directory (`DESIGN.md:5-10`; `EXPERIENCE.md:5-10`).
- The pair contains 19 machine-readable component keys, 19 visual rows, 19 behavioral peer rows, and 10 journey sections. Exact live journey-name parity is 9/10 because of H1.
- The exact CLI command names, global options, shipped/placeholder/absent classifications, output envelope, and exit codes agree with current source (`EXPERIENCE.md:38-61`; `.working/extract-protocol-implementation.md:208-263`). The only current `NotImplementedCommand` groups remain `ingest`, `traverse`, `case`, and `explore`.
- The four MCP names, request-field names, `CaseStatus` values, success profile split, `ErrorResponse { code, message, suggestion }`, and current delivery caveats agree with source (`EXPERIENCE.md:63-74`; `.working/extract-protocol-implementation.md:5-108`). H3 and M3 concern an invented presentation concept and omitted constraints, not renamed tools.
- FrontComposer/Fluent V5 inheritance, no-theme-delta posture, the one-accordion rule, and the Evidence Cockpit exception/application are retained (`DESIGN.md:12-31`, `:229`; `EXPERIENCE.md:228-232`; `../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-51`). No forbidden legacy FAST token spelling appears in the spines.
- The May HTML is explicitly non-normative and its palette, fixed breakpoints, markup, scripts, ARIA, scores, and component names are not copied (`DESIGN.md:241`; `EXPERIENCE.md:402-408`). No files exist in this workspace’s `mockups/`, `wireframes/`, or `imports/` directories.
- J7 remains protocol-first with no required screen and J10 remains repository/CI infrastructure-only, as required (`EXPERIENCE.md:350-361`, `:389-400`).
- This review does not edit either spine, any source, any extract/reconciliation, or `.memlog.md`.

## Resolution check

**Checked:** 2026-09-12, after reviewer remediation and evidence refresh.  
**Pinned input:** the PRD still has 1,243 lines and SHA-256 `12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1`; both spines pin that exact identity and the one-way lineage rule. ([PRD](../../prd.md#L1), [DESIGN lineage](DESIGN.md#L12), [EXPERIENCE lineage](EXPERIENCE.md#L12))  
**Resolution verdict:** **SOURCE-SAFE FOR DOWNSTREAM SPINE BINDING.** Every original H1–H6, M1–M3, and L1 finding is resolved in the source contract. Product release, runtime conformance, and implementation-remediation gates remain separately open and must not be inferred from this documentation verdict.

### Revised residual severity counts

| Severity | Original findings still open | New residual notes |
|---|---:|---:|
| Critical | 0 | 0 |
| High | 0 | 0 |
| Medium | 0 | 0 |
| Low | 0 | 1 |

The one new Low note is mechanical and does not affect either spine: the refreshed PRD reconciliation still says the protocol extract’s item 14 is stale ([reconciliation lines 23 and 94](.working/reconcile-prd.md#L23)), but corrected item 14 now accurately says current search has no `--from` and matches the pinned PRD’s automatic-seeding boundary ([protocol extract](.working/extract-protocol-implementation.md#L282), [PRD](../../prd.md#L167)). Remove that historical note on the next reconciliation refresh; no spine change is required.

### Original finding dispositions

| ID | Disposition | Resolution evidence |
|---|---|---|
| H1 | **Resolved** | J8 now exactly matches **`Priya — "I Need to Understand This Case" (Phase 2 downstream application)`** in the PRD, spine, refreshed extraction, and reconciliation. ([PRD J8](../../prd.md#L447), [EXPERIENCE J8](EXPERIENCE.md#L375), [extraction](.working/extract-prd.md#L60), [reconciliation](.working/reconcile-prd.md#L51)) The memlog explicitly supersedes the old Phase 1 title. ([memlog](.memlog.md#L52)) |
| H2 | **Resolved** | The spine states the current no-go, all-hard G1–G6 posture, assumed 2026-12-01 date, G6 ownership/AD-14/FR75/NFR37 blockers, and unevaluated L1–L3. It separately labels current envelope/exit behavior, raw-export/bodyless-cancellation exceptions, and NFR37 semantic parity as an unverified target. ([release governance](EXPERIENCE.md#L44), [maturity](EXPERIENCE.md#L46), [CLI status](EXPERIENCE.md#L71), [CLI accessibility](EXPERIENCE.md#L253), [PRD release posture](../../prd.md#L62)) |
| H3 | **Resolved** | Public search has no start-node input; per-case graph work auto-seeds from top-five syntactic plus top-five semantic candidates at depth≤2; explicit starts belong only to `traverse` / `traverse_relations`. The spine says any auto-seeded label is invocation-derived and that `Contracts.V1` has no provenance field. Corrected protocol item 14 agrees. ([PRD rule](../../prd.md#L167), [spine rule](EXPERIENCE.md#L224), [protocol item 14](.working/extract-protocol-implementation.md#L282), [open-item closure](.working/distill-open-items.md#L10)) |
| H4 | **Resolved** | All 19 DESIGN leaves now use `implemented-primitives` and bind to the actual Razor/allowlist composition. The nine originally incorrect entries now name their present Fluent/FrontComposer and registered semantic primitives rather than invented ones. ([DESIGN component map](DESIGN.md#L41), [DESIGN inventory status](DESIGN.md#L211), [allowlist](../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs#L95)) |
| H5 | **Resolved** | Each affected behavioral row is split at point of use into **Current specimen** and **Activation target**: Source Citation Stack, Graph Path Summary, Case Activity Trail, Ingestion Lifecycle Tracker, Operator Health Matrix, and Benchmark Result Comparator. Unsupported actions/data are explicitly contract- or host-gated. ([component peers](EXPERIENCE.md#L117), [source/graph rows](EXPERIENCE.md#L124), [lens rows](EXPERIENCE.md#L135)) |
| H6 | **Resolved for spine binding; implementation debt retained** | The spine now explicitly says current Web lacks a contract-backed conflict field and may misclassify degraded/unavailable-axis packets as `Conflicting`; it forbids treating that as current conformance and reserves conflict for a future versioned signal. ([spine divergence](EXPERIENCE.md#L156), [current mapper](../../../../src/Hexalith.Memories.Web/Components/Recovery/RecoveryStateMapper.cs#L85)) Runtime code still needs its separate fix, but a downstream spine consumer can no longer mistake it for desired semantics. |
| M1 | **Resolved** | The extraction and reconciliation now identify the exact 1,243-line pinned PRD, cover G1–G6, FR1–FR75 and NFR1–NFR37, preserve the corrected J8 title and settled graph rule, and report zero PRD-to-spine blockers without claiming product go. The memlog records immutable lineage and supersession rather than rewriting history. ([extraction](.working/extract-prd.md#L1), [reconciliation](.working/reconcile-prd.md#L1), [memlog lineage](.memlog.md#L51)) |
| M2 | **Resolved** | Both spine frontmatters carry the exact authoritative PRD SHA and state that same-day reciprocal PRD links do not reverse authority for this run. ([DESIGN](DESIGN.md#L12), [EXPERIENCE](EXPERIENCE.md#L12)) |
| M3 | **Resolved** | The exact MCP map now includes `search_memory.maxResults` default 10/clamp 1..100, `traverse_relations.depth` default 3/clamp 0..10, and the exact `edgeType` literals `causedBy`, `correlatedWith`, `references`, `contains`, and `annotates`. ([spine MCP map](EXPERIENCE.md#L73), [protocol schemas](.working/extract-protocol-implementation.md#L18)) |
| L1 | **Resolved** | The validation reconciliation now uses paths relative to `.working/`: `../../../ux-validations/...`, sibling `extract-validation.md`, and `../DESIGN.md` / `../EXPERIENCE.md`. The referenced files resolve. ([corrected links](.working/reconcile-validation.md#L5)) |

### Binding boundary after resolution

- The current spine-binding residual is **0 Critical / 0 High / 0 Medium / 0 Low**; the Low count above is a working-evidence wording cleanup, not a DESIGN/EXPERIENCE defect.
- Source safety does not mean release approval. The PRD and spine still require G1–G6 evidence/ownership and L1–L3 evaluation, keep NFR37 unverified, and preserve partial/hardening/re-verification statuses. ([PRD release decision](../../prd.md#L208), [spine governance](EXPERIENCE.md#L44), [refreshed reconciliation](.working/reconcile-prd.md#L120))
- H6’s conflict mapper, the shipped CLI 401 suggestion’s wrong environment-variable name, and the conformance allowlist’s dialog-focus comment remain implementation/documentation debt outside this spine-only resolution; the spines carry the safe target and current limitation without blessing those behaviors. ([conflict divergence](EXPERIENCE.md#L156), [other implementation residues](.working/distill-open-items.md#L27))
