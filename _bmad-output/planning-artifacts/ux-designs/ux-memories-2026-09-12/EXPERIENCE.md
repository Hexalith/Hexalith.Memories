---
name: Hexalith.Memories
description: Behavioral contract for trustworthy, inspectable memory infrastructure.
status: draft
sources:
  - ../../prd.md
  - ../../ux-design-specification.md
  - ../../ux-design-directions.html
  - ../../ux-validations/ux-memories-2026-09-09/validation-report.md
  - ../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md
updated: 2026-09-12
lineage:
  authoritative-prd-sha256: 12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1
  rule: This migration derives from the pinned PRD revision; same-day reciprocal PRD links do not reverse authority for this run.
implementation-evidence:
  - .working/extract-ui-implementation.md
  - .working/extract-protocol-implementation.md
  - ../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs
  - ../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs
---

# EXPERIENCE — Hexalith.Memories

## Foundation

The experience promise is recoverable trust: establish authorized tenant and case scope before work; expose result, source/origin, relevance meaning, freshness and degradation, omission, and the safest recovery without forcing the user to infer system state. Relevance confidence, metadata confidence, edge confidence, freshness, evidence health, projection completion, and authorization are separate concepts.

Source precedence is: the live change-controlled PRD and repository UX instructions; then the 2026-09-09 validation; then the May legacy specification; then its historical HTML study. Contracts.V1 and current implementation evidence own serialized names and delivery facts. Architecture, the central package catalogue, and the repository instructions own mechanisms and component availability. Deferred product values stay deferred.

Where a capability exists on more than one surface, CLI, MCP, and future web preserve the same meanings for scope, source, relevance, freshness, omission, axis availability, degradation, and recovery. Presentation density and interaction differ by surface; capability does not. CLI remains the operational superset, so semantic equivalence never implies feature parity with the MCP subset or an inactive web horizon.

### Phase, surface, and delivery matrix

| Phase | Surface | Delivery on 2026-09-12 | Binding boundary |
|---|---|---|---|
| Phase 1 | CLI developer and operator experience | Active, but target surface incomplete | Shipped search, inspect/lookup, tenant list, telemetry, consistency, quickstart, configuration, and early export coexist with missing Phase 1 verbs. This is the thesis surface. |
| Phase 1 | REST <code>/api/v1</code> and <code>Hexalith.Memories.Client.Rest</code> | Shipped programmatic transport for capabilities without CLI verbs | Transport is not a Memories application UI. |
| Phase 1.5 | MCP agent subset | Four tools implemented as preview; L1–L3 are unevaluated, so not launch-approved | Agent search, ingest, traversal, and case info only; no operator parity. |
| Phase 1.5 | EventStore product integration and handler diagnostics | Code delivered; launch gates pending | EventStore-convention CloudEvents, dual embeddings, causal metadata, and handler evolution. |
| Phase 2 | Downstream application Evidence View and Case Briefing | Inactive product horizon; portable export was delivered early without changing phase | J4/J8 composition over REST/MCP; the downstream app owns its own product decisions and chrome. |
| Phase 3 | Memory Explorer, Timeline, and Backend Migration Operations | Inactive product horizon | Requires later discovery; do not infer routes, controls, or current capability. |
| No product phase | <code>Hexalith.Memories.Web</code> specimen host | Implemented conformance evidence only | The 19 RCL components and fixture routes prove component structure, not production routing, authorization, data loading, focus integration, notification behavior, or product activation. |

**Release governance:** the current release posture is no-go. G1–G6 are all hard gates; the retained 2026-12-01 Phase 1 decision date is an explicit assumption. G6 cannot pass while missing work/evidence lacks successor-story ownership, AD-14 phasing is unratified, or architecture has not bound FR75 and NFR37. Phase 1.5 L1–L3 are not evaluated until the Phase 1 gate passes.

Point-of-use maturity matters. Current-revision projection completion (FR6/FR13), case-partitioned tenant-wide graph merge (FR34), verified erasure (FR39), server-derived provenance (FR65), and durable duplicate suppression (FR75) are partial. Tenant authority hardening (FR44) remains in progress. Fairness (FR8), capability-aware degradation (FR66), readiness (FR72), and the active CLI accessibility contract (NFR37) require current-contract evidence or re-verification; NFR37 is not started and has no current-contract evidence. Normative behavior below is target contract unless a current-status table says it is already delivered.

### Exact current CLI grammar and status

All current commands inherit <code>--endpoint &lt;uri&gt;</code>, <code>--token &lt;value&gt;</code>, <code>--verbose</code>, <code>--format human|json|table</code>, and <code>--telemetry</code>. The supported <code>--token</code> value is visible in process arguments and may enter shell history; prefer the exact noninteractive source <code>HEXALITH_MEMORIES_API_TOKEN</code>, and never place a literal secret in examples or artifacts. The current 401 recovery suggestion incorrectly names <code>HEXALITH_MEMORIES_TOKEN</code>; treat that as implementation debt, not valid guidance. There is no ambient tenant switch. A <code>--tenant</code> value requests scope; authenticated claims authorize it.

| Current grammar | Current status and important behavior |
|---|---|
| <code>memories tenant list</code> | Shipped. |
| <code>memories config show</code> | Shipped. |
| <code>memories search query --tenant &lt;id&gt; [--case &lt;id&gt;] [--query &lt;text&gt;] [--axis syntactic|semantic|nl|graph|hybrid] [--max-results &lt;1..1000&gt;] [--explain]</code> | Shipped; <code>hybrid</code> and 10 are defaults. Query is optional only for graph. There is no current search <code>--from</code> or <code>--token-budget</code>. |
| <code>memories search inspect --tenant &lt;id&gt; --case &lt;id&gt; --id &lt;memoryUnitId&gt;</code> | Shipped. |
| <code>memories search lookup --tenant &lt;id&gt; --case &lt;id&gt; --source-uri &lt;uri&gt;</code> | Shipped. |
| <code>memories quickstart [--tenant &lt;id&gt;] [--skip-boot-check] [--skip-prereq-check] [--dry-run] [--tenant-timeout-seconds &lt;positive-int&gt;]</code> | Shipped; supplements but does not replace the manual NFR31 path. |
| <code>memories status telemetry --tenant &lt;id&gt;</code> | Shipped; no current <code>status --case</code> or <code>status --failed</code> registration. |
| <code>memories consistency verify --tenant &lt;id&gt; [--batch-size &lt;10..5000&gt;] [--wait]</code> | Shipped. |
| <code>memories consistency inspect --tenant &lt;id&gt; --id &lt;memoryUnitId&gt;</code> | Shipped. |
| <code>memories consistency repair --tenant &lt;id&gt; [--batch-size &lt;10..5000&gt;] [--include-unrepairable] [--wait] [--yes]</code> | Shipped; UX preserves dry-run/apply intent, tenant boundary, provenance, and current-revision limits. |
| <code>memories export case --tenant &lt;id&gt; --case &lt;id&gt; [--output &lt;path&gt;] [--force] [--allow-absolute-path]</code> | Shipped early against Phase 2 FR71. Success is the portable raw-JSON artifact; it ignores non-human <code>--format</code> with a stderr warning rather than wrapping the artifact in a CLI envelope. |
| <code>memories export tenant --tenant &lt;id&gt; [--output &lt;path&gt;] [--force] [--allow-absolute-path]</code> | Shipped early against Phase 2 FR71. Success is the portable raw-JSON artifact; it ignores non-human <code>--format</code> with a stderr warning rather than wrapping the artifact in a CLI envelope. |
| <code>memories handlers list</code> | Shipped Phase 1.5 diagnostic; replaces stale journey wording <code>handlers --list</code>. |
| <code>memories handlers mismatches --tenant &lt;id&gt; [--severity info|warning] [--only-warning] [--exclude-stale]</code> | Shipped Phase 1.5 diagnostic; JSON intentionally remains unfiltered. |
| <code>memories ingest</code>, <code>memories traverse</code>, <code>memories case</code>, <code>memories explore</code> | These four top-level groups are the only current <code>NotImplementedCommand</code> placeholders. Each writes “Not yet implemented — tracked in Story 7.2.” to stderr and exits 2. Their target subcommand grammars are not current registrations. |
| PRD targets <code>tenant create/delete/verify</code> and <code>status --case/--failed</code> | Absent from the current command tree, not individually registered stubs. |

Current exit codes are success 0, domain error 1, plumbing 2, not found 4, and cancelled 130. Formatter-routed JSON success emits <code>{ schemaVersion, command, data }</code>; formatter-routed JSON error emits <code>{ schemaVersion, command, error }</code>, with exactly one of <code>data</code> and <code>error</code>, and JSON errors go to stdout. Export success is the raw portable artifact described above. Cancellation currently emits <code>Cancelled.</code> to stderr with exit 130 and no JSON body. Cross-form semantic parity is the binding NFR37 target, not a claim that every current byte shape is identical or that current evidence closes the requirement.

### Exact MCP tool contract map

MCP is an implemented Phase 1.5 preview. Public contract names below are version-bound; delivery gaps do not redefine the desired PRD outcome.

| Tool | Current inputs | Current success contract | UX limits |
|---|---|---|---|
| <code>search_memory</code> | Required <code>tenantId</code>, <code>query</code>; optional <code>case</code>, <code>axes</code> = <code>Hybrid</code>, <code>maxResults</code> = 10 clamped to 1..100, <code>tokenBudget</code>, <code>explain</code> = false. <code>SearchAxis</code> is <code>Syntactic</code>, <code>Semantic</code>, <code>Nl</code>, or <code>Hybrid</code>. | <code>SearchResult</code> or <code>HybridSearchResult</code>, currently with <code>Hexalith.Memories.Contracts.V1.EvidencePacket</code>. | Graph work uses <code>traverse_relations</code>. Current packets do not guarantee populated freshness, source-ingestion, MCP-schema, graph-summary, or complete NL-axis metadata. |
| <code>ingest_content</code> | Required <code>tenantId</code>, <code>caseId</code>, <code>content</code>; optional <code>sourceType</code>, <code>sourceUri</code>, <code>contentType</code>, and current implementation-only <code>ingestedBy</code>. | Wire field <code>{ workflowInstanceId }</code>; its C# response type is not public Contracts.V1. | Current behavior accepts only File despite advertising URL/Event, and UTF-8 encodes the input. Do not bless caller-controlled <code>ingestedBy</code>: FR65 requires server-derived provenance. |
| <code>traverse_relations</code> | Required <code>tenantId</code>, <code>from</code>; optional <code>depth</code> = 3 clamped to 0..10, comma-separated <code>edgeType</code> literals <code>causedBy</code>, <code>correlatedWith</code>, <code>references</code>, <code>contains</code>, or <code>annotates</code>, <code>tokenBudget</code>, <code>caseId</code>. | <code>TraversalResult</code> with start, depth, nodes, gaps, omissions, degradation, unavailable axes, and primary-path state. | Paths remain case-local. Nodes and edges preserve direction, type, chronology, confidence, and gap markers; do not invent a <code>graphScope</code> object. |
| <code>get_case_info</code> | Required <code>tenantId</code>, <code>caseId</code>. | <code>Case</code>: <code>id</code>, <code>tenantId</code>, <code>name</code>, optional <code>description</code>, <code>status</code>, <code>createdAt</code>, <code>lastUpdated</code>, <code>memoryUnitCount</code>. | <code>CaseStatus</code> is <code>active</code>, <code>closed</code>, or <code>deleting</code>. Do not promise member count or richer activity than the response contains. |

Every tool validates its request and exact tenant claim before calling upstream. Success uses text JSON plus matching structured content. The public server error is <code>ErrorResponse { code, message, suggestion }</code>; MCP error wire content adds <code>service</code>, <code>tool</code>, and optional <code>evidencePacket</code> without promoting its private C# type name. Authorization errors disclose no evidence and offer authorization recovery.

## Information Architecture

The product IA is surface-led, not one browser dashboard. The table closes every PRD journey onto a delivery-aware surface.

| Canonical surface | Form factor, audience, and entry | Purpose and source coverage | Components or representation | Required states and exit/recovery |
|---|---|---|---|---|
| CLI Search & Evidence | Terminal; developer; <code>search query</code>, <code>inspect</code>, <code>lookup</code> | Retrieve and verify evidence. J1, J2, J9; FR14–FR25, FR34, FR63–FR66. | Linear Evidence Packet projection; no browser component required. | Cold request, focus/input, results, empty, validation, auth denial, degraded, no-safe-axis, omission, transport error; exit to inspect, refine, traversal target, or recovery. |
| CLI Onboarding & Ingestion | Terminal; developer; README/manual path or <code>quickstart</code> | Reach first real-data search and observe durable progress. J9; FR1–FR13, FR53, NFR31, NFR36. | Linear progress and receipts; future semantics align with Ingestion Lifecycle Tracker. | Prerequisite, healthy empty, accepted, queued/admission delay, each lifecycle state, rate limit, failed stage, retry/replay, indexed; exit to search. |
| CLI Case | Terminal; developer/team lead; target <code>case</code> group | Create, list, delete, membership metadata, activity. J4, J9; FR26–FR37. | Target behavior aligns with Case Activity Trail and Action Confirmation. | Empty, validation, permission denial, deleting, failure, activity empty/populated; exit to ingest/search or safe cancellation. Current group is a placeholder. |
| CLI Tenant & Operations | Terminal; operator; <code>tenant</code>, <code>status telemetry</code>, <code>config show</code>, <code>export</code> | Provision, isolate, configure, export, inspect telemetry, and verify erasure. J5; FR38–FR45, FR67–FR72. | Text/table/JSON; future semantics align with Operator Health Matrix. | Cold check, configured/unconfigured, auth denial, mismatch, liveness/readiness, capability degradation, queue pressure, erasing, verified, failed, quarantined restore; exit to recovery or signed-off receipt. |
| CLI Graph Traversal | Terminal; developer; target <code>traverse</code> group | Inspect ordered case-local relations. J1/J2; FR16–FR18, FR46–FR52. | Ordered textual path equivalent to Graph Path Summary. | Start validation, no path, complete, gaps, excluded edge types, degradation, authorization failure, omission; exit to inspect a node or recover. Current group is a placeholder. |
| CLI Diagnostics & Repair | Terminal; developer/operator; <code>handlers</code>, <code>consistency</code>, telemetry | Diagnose handlers, divergence, and safe repair. J2/J5; FR62, FR72–FR74. | Text/table/JSON; future lens semantics use Operator Health Matrix. | No issues, mismatch, divergence, dry run, confirmation, queued, partial, failed, completed; exit to re-check. |
| MCP Agent Protocol | Machine protocol; configured LLM agent/developer; four registered tools | Bounded, sourced agent retrieval and recovery. J3/J7; FR23, FR54, FR58–FR66, NFR20. | Versioned JSON/structured content; Agent Packet Inspector is a future human diagnostic lens. | Schema validation, authorized success, empty, omitted, degraded, unavailable, timeout, unauthorized, structured recovery; exit through tool follow-up or explicit no-hallucination fallback. |
| Downstream Evidence View & Case Briefing | Future application UI; case worker/team member; downstream route or assistant | Verify chronological, causal, sourced case understanding. J4/J8; Phase 2 briefing/annotations/search UI. | Evidence Cockpit, Evidence Grid, Case Activity Trail, and Shared Lens Shell compositions inside the downstream host. | Cold load, focus, empty, weak/partial, stale, source unavailable, discrepancy, correction, auth denial, degraded, recovery; exit to source, annotation, or work task. |
| Backend Migration Operations | Future terminal/operator surface; assessment then migration | Plan and execute controlled backend change. J6; Phase 3 migration. | Future operator representation; no committed component anatomy beyond inherited patterns. | Assessment, unsupported, dry run, confirmation, queue/admission, migrating, rollback, failed, verified cutover; exit to operations. |
| Contribution Pipeline | Repository, GitHub, build, test, and CI; contributor | Complete first contribution. J10; infrastructure requirements only. | No Memories product component or screen. | Reproduction, build/test failure, review changes, CI failure/success, merge; exit through contributor workflow. |

The Web conformance specimen is excluded from product IA: it is a fixture-backed validation surface, not an entry point. AppHost, DAPR, OpenBao, Redis, FalkorDB, and projections are mechanisms exposed only when their prerequisite, authority, readiness, health, or recovery meaning affects a user.

The future downstream host owns query entry, filter placement, and command orchestration. Evidence Cockpit remains the packet renderer; at Phase 2 activation the host must deliberately place query/filter/command entry through real FrontComposer page/toolbar and Memories interaction patterns. The current spine does not prescribe the downstream application’s chrome or invent a product route.

## Voice and Tone

Use calm, concise, technical language. Lead with outcome, then scope and evidence, then the next safe action. Name the affected tenant/case only when authorized. Prefer “unavailable,” “excluded,” “no hits,” “pending,” “rate-limited,” “stale,” or “failed at embedding” over vague “something went wrong.”

Never say a relevance score proves truth; say it measures query-result relevance. Use “access telemetry,” never “audit trail.” Describe membership as attribution metadata, not permission. Do not call a partial result complete, a queued operation active, a retry a new unit, a conformance specimen a product UI, or preview MCP launch-approved.

Errors follow <code>code → message → suggestion</code>. Recovery copy names one safest action first and preserves diagnostic detail without exposing secrets, restricted sources, other-tenant identifiers, stack traces, or unsanitized payloads.

## Component Patterns

These rows are the behavioral peers of the 19 canonical visual entries in <code>DESIGN.md</code>. Their current RCL implementations are conformance components; product-host activation remains separate.

| Canonical component | Purpose, anatomy, states, and interaction | Ownership and accessibility | Visual binding |
|---|---|---|---|
| Evidence Cockpit | Projects one Evidence Packet as scope, trust, result, evidence, recovery, sources, axes, and graph. Loading/error suppress empty subordinate detail; packet states preserve complete, partial, weak, empty, stale, degraded, unauthorized, and pending expansion. Recovery emits an intent. | <code>MemoriesEvidenceCockpit</code> owns packet composition; host owns data, routing, dispatch, focus, and notifications. Its five titled regions use one multi-expand accordion. | {components.evidence-cockpit.surface-role}; {components.evidence-cockpit.status-treatment}. |
| Scope Header | Shows tenant, optional case, permissions context, and isolation before results. Missing, malformed, mismatch, or unauthorized scope blocks retrieval; authorized same-tenant expansion requires deliberate confirmation. | <code>MemoriesScopeHeader</code> presents verified contract data; the server enforces authority and FrontComposer provides semantics/layout. | {components.scope-header.surface-role}; {components.scope-header.status-treatment}. |
| Trust Strip | Shows relevance label/caveat, freshness, source count, evidence health, and token-budget state without collapsing them. It precedes the answer and wraps rather than disappears. | <code>MemoriesTrustStrip</code> derives text from the packet; <code>FcStatusBadge</code> owns semantic appearance and accessible naming. | {components.trust-strip.surface-role}; {components.trust-strip.status-treatment}. |
| Source Citation Stack | **Current specimen:** read-only packet sources and missing-state text; no inspect callback or contract-backed conflict signal. **Activation target:** authorized ranked source identity, type, snippet, case attribution, timestamp/freshness when populated, plus host-owned inspect action and separate conflict condition when the contract carries it. | <code>MemoriesSourceCitationStack</code> exposes only packet sources. The future host labels actions by target; restricted scope produces no source disclosure. | {components.source-citation-stack.surface-role}; {components.source-citation-stack.status-treatment}. |
| Retrieval Axis Breakdown | Presents participating axes in deterministic order with contribution semantics, normalization text, and distinct available, excluded, unavailable, and no-hits states. Detailed ranks/weights remain explain detail. | <code>MemoriesRetrievalAxisBreakdown</code> reads the packet; it never derives factual certainty from a score and provides text equivalents for any bar or number. | {components.retrieval-axis-breakdown.surface-role}; {components.retrieval-axis-breakdown.status-treatment}. |
| Graph Path Summary | **Current specimen:** V1 graph-summary related path identifiers, edge-type list, and gap markers only. **Activation target:** ordered nodes, typed/directional edges, chronology, case attribution, confidence, literal gaps, and host-owned inspect/recovery when a versioned contract supplies them. No graph and unavailable graph stay distinct. | <code>MemoriesGraphPathSummary</code> remains case-local. Any future visual graph selection is synchronized with an ordered list/table equivalent; classified semantic HTML is allowed only where Fluent lacks a primitive. | {components.graph-path-summary.surface-role}; {components.graph-path-summary.status-treatment}. |
| Recovery Action Panel | Attaches cause, affected capability, safest action, secondary actions, and diagnostic context to weak, empty, stale, degraded, omitted, failed, or unauthorized state. Failed recovery stays actionable. | <code>MemoriesRecoveryActionPanel</code> maps contract recovery and emits intents; host executes. Focus remains stable unless an overlay or navigation warrants movement. | {components.recovery-action-panel.surface-role}; {components.recovery-action-panel.status-treatment}. |
| Evidence Grid | Provides ranked/case-attributed rows, accessible headers, sort state, target-qualified actions, and compact detail parity. Restricted scope yields no rows; empty and degraded states remain outside false “zero” data. | <code>MemoriesEvidenceGrid</code> owns column planning and row intent; Fluent owns grid primitives. It requires an accessible name/caption and keyboard-operable actions. | {components.evidence-grid.surface-role}; {components.evidence-grid.status-treatment}. |
| Filter Summary | Makes active filters, sort, scope effect, excluded axes, and result impact visible; supports remove/reset without silently broadening unauthorized scope. | <code>MemoriesFilterSummary</code> owns domain chips/intents; <code>FcFilterSummary</code> owns the localized summary. Changes announce the new result context without stealing focus. | {components.filter-summary.surface-role}; {components.filter-summary.status-treatment}. |
| Interaction Form | Collects contract-backed values, labels every field, exposes inline validation, prevents unsafe submit, and preserves entered values after recoverable failure. | <code>MemoriesInteractionForm</code> validates and emits submit only; host owns execution. Labels and described-by references resolve, and summary/detail errors are associated programmatically. | {components.interaction-form.surface-role}; {components.interaction-form.status-treatment}. |
| Command Surface | Shows only safe, context-valid actions with a readable disabled reason. Primary action is sparse; secondary actions may move to overflow without disappearing from keyboard access. | <code>MemoriesCommandSurface</code> derives commands and emits intents; Fluent owns buttons. It never claims permission from visual enabled state alone. | {components.command-surface.surface-role}; {components.command-surface.status-treatment}. |
| Action Confirmation | Names tenant, case, target, consequence, and irreversible boundary for destructive, scope-expanding, or trust-sensitive actions. Cancel is safe default; retry does not duplicate work. | <code>MemoriesActionConfirmation</code> maps domain copy and callbacks; <code>FcDestructiveConfirmationDialog</code> supplies the body, safe autofocus, Escape, callbacks, and plain-text rendering. The product host must launch it through the shell-provided Fluent <code>IDialogService</code>/provider lifecycle and prove initial focus, trap, cancellation, and return on the activated route. | {components.action-confirmation.surface-role}; {components.action-confirmation.status-treatment}. |
| Context Navigation | Opens a source/node/lens and returns with tenant, case, query, filters, and focus continuity. Invalid or stale context blocks the transition with recovery. | <code>MemoriesContextNavigation</code> validates context and emits open/return intents; host owns route changes and focus restoration. | {components.context-navigation.surface-role}; {components.context-navigation.status-treatment}. |
| Shared Lens Shell | Gives each activity, ingestion, operator, benchmark, or agent lens the same packet-derived scope/trust/body/return anatomy. It does not execute domain work. | <code>MemoriesLensShell</code> owns composition and return intent; host owns data and navigation. Route heading/main focus is inherited from FrontComposer. | {components.shared-lens-shell.surface-role}; {components.shared-lens-shell.status-treatment}. |
| Case Activity Trail | **Current specimen:** maps packet sources, annotation count, graph relations/gaps, trust state, and recovery. **Activation target:** time-orders contract-backed ingestion, search, membership-metadata, annotation, and change events with actor, source, evidence impact, and inspect path; no unavailable event is inferred. | <code>MemoriesCaseActivityTrail</code> is a lens body; member events never imply authorization. A linear reading order and timestamp text replace purely spatial timelines. | {components.case-activity-trail.surface-role}; {components.case-activity-trail.status-treatment}. |
| Ingestion Lifecycle Tracker | **Current specimen:** maps optional packet source-stage metadata and does not surface every available updated/retry field. **Activation target:** shows pending → extracting → embedding → projecting → indexed or failed, plus contract-backed acknowledgement, revision/config completion, queue delay, last update, retry count, failure stage, and recovery. | <code>MemoriesIngestionLifecycleTracker</code> is a lens body. Target progress has text; unknown duration is stated; retries preserve one operation identity and announcements are durable, not spinner-only. | {components.ingestion-lifecycle-tracker.surface-role}; {components.ingestion-lifecycle-tracker.status-treatment}. |
| Operator Health Matrix | **Current specimen:** fixed checks for isolation, authorization, retrieval backend, axis availability, graph context, and detail completeness. **Activation target:** separately presents liveness, readiness, capability degradation, queue/fairness pressure, tenant blast radius, telemetry posture, and repair only when supplied by an activated contract. | <code>MemoriesOperatorHealthMatrix</code> is a lens body. Every status has text, and future actions name the tenant and consequence. | {components.operator-health-matrix.surface-role}; {components.operator-health-matrix.status-treatment}. |
| Benchmark Result Comparator | **Current specimen:** packet-backed axis NDCGs, threshold/pass value, run/corpus metadata, per-query hybrid values, and evidence URI. **Activation target:** compares hybrid with the BM25+semantic control and diagnostics under the complete G1 protocol: N≥50, at least 80% topic wins, per-topic ΔNDCG@10≥0.02, κ≥0.6, PRD aggregate guards, and frozen labels; the shipped N=8 run remains diagnostic only. | <code>MemoriesBenchmarkResultComparator</code> is a lens body. Every current or future metric has a textual equivalent; benchmark evidence never becomes product-truth confidence. | {components.benchmark-result-comparator.surface-role}; {components.benchmark-result-comparator.status-treatment}. |
| Agent Packet Inspector | Inspects real tool name, request/schema version, supported fields, token budget, omissions, expansion handles, structured error, and sanitized raw JSON. Unpopulated metadata is shown as unavailable, not fabricated. | <code>MemoriesAgentPacketInspector</code> is a lens body. Its single sanitized-JSON native disclosure is a narrow tested exception, with keyboard and accessible-name coverage. | {components.agent-packet-inspector.surface-role}; {components.agent-packet-inspector.status-treatment}. |

## State Patterns

### Evidence and absence

The Contracts.V1 Evidence Packet state vocabulary is <code>complete</code>, <code>partial</code>, <code>weak</code>, <code>empty</code>, <code>stale</code>, <code>degraded</code>, <code>unauthorized</code>, and <code>pendingExpansion</code>. Current search mappers do not yet emit every defined state, including <code>partial</code> and <code>stale</code>; unavailable states appear as target contract, not fabricated runtime data. A presentation must separately state:

- selected and available axis with hits;
- selected and available axis with no hits;
- selected but unavailable axis;
- deliberately excluded axis;
- all selected axes unavailable, which is an error rather than a partial success;
- token or density omission, with count/detail groups and deterministic expansion/recovery where the current contract supports it.

“No results” is not enough. Distinguish true absence from empty tenant/case, wrong scope, incomplete/delayed ingestion, filters, stale evidence, authorization refusal, and backend degradation. Unauthorized responses contain no restricted evidence.

Human evidence-strength labels mirror the versioned values <code>none</code>, <code>unknown</code>, <code>weak</code>, <code>moderate</code>, and <code>strong</code>, always with the relevance-not-fact caveat. Conflicting sources are a separate evidence condition with source comparison and recovery; they do not create an unversioned “disputed” packet state or alter the relevance score. Current Web code has no contract-backed conflict field and can map packet degradation or unavailable axes to <code>Conflicting</code>; this is a delivery divergence, not current conformance. Until a versioned conflict signal exists, activated UX must classify backend/axis loss as degradation and must not tell users that sources conflict.

### Ingestion lifecycle and current revision

| Human product label | V1 JSON wire value | Completion meaning |
|---|---|---|
| pending | <code>queued</code> | Accepted; EventStore acknowledgement is the durable commit. |
| extracting | <code>extracting</code> | Text extraction is active. |
| embedding | <code>embedding</code> | Provider work is active, queued, or visibly rate-limited. |
| projecting | <code>indexing</code> | Search, vector, and graph projections are converging. |
| indexed | <code>indexed</code> | All required projections acknowledge the current authoritative revision under the active schema generation and embedding configuration. |
| failed | <code>failed</code> | Terminal after configured retry limits; stage, error, attempts, and recovery remain visible. |

Retrying, dead-letter, repair, replay, attempt count, last error, and diagnostic stages are details, not extra product states. A stale/incompatible or two-of-three acknowledgement never appears indexed. Under FR75, duplicate V1 command identity means the same tenant, case, and operation-scoped idempotency token; duplicate CloudEvent identity means the same tenant, case, exact validated source, and event ID. Either appears as one operation, one durable mutation, and one projection outcome. No public CLI or MCP idempotency option is invented, and the current MCP ingest path does not yet forward one.

Long-running ingest, repair, recovery, erasure, and migration disclose admission, queue delay, current state, last update, expected or unknown duration, affected capability, cancellation/dismissal limits, timeout meaning, completion/failure, and safe retry. Provider <code>Retry-After</code> uses durable timing. Bounded tenant/global admission preserves interactive fairness during batch work.

### Freshness, health, erasure, and telemetry

Human freshness labels are <code>current</code>, <code>aging</code>, <code>stale</code>, and <code>unknown</code>. Thresholds and transitions remain contract-owned; current implementation enum differences are delivery debt. Freshness never modifies relevance confidence.

Liveness means process viability. Readiness covers authentication configuration, the DAPR control boundary, and EventStore command availability. Search backend failure is capability degradation while a safe selected axis remains; no-safe-axis failure is explicit.

Verified tenant erasure progresses through purge, irreversible EventStore inaccessibility verification, and durable access-telemetry handoff. It is incomplete until replay/restart/restore cannot resurrect content; unsafe restored payloads are quarantined and the tenant ID cannot be reused.

Production telemetry qualification fails closed. After admission, access-telemetry delivery is bounded and non-blocking for accepted writes, and degradation is surfaced; telemetry failure never rolls back domain truth. Telemetry is sanitized and is not tamper-evident.

Platform Operations owns the configured telemetry TTL, purge progress, bounded recovery, erasure mapping, and dated accepted debt for an unsupported retention profile. Retained opaque telemetry follows that TTL after tenant erasure and is never presented as tenant content or legal-audit evidence.

### Per-surface state closure

| Surface | Applicable state closure |
|---|---|
| CLI Search & Evidence | Input focus; validation; cold request; success; empty classifications; omission; partial/degraded; no-safe-axis; tenant denial; timeout/network; recovery. Offline mode is out of scope; unreachable service is an error. |
| CLI Onboarding & Ingestion | Prerequisite/boot; healthy empty; accepted; admission delay; six lifecycle states; rate limit; restart/resume; failed stage; retry/replay; current-revision indexed. |
| CLI Case | Empty/list; create validation; active/closed/deleting; membership/activity empty; permission denial; destructive confirmation; failure/recovery. |
| CLI Tenant & Operations | Cold health check; liveness/readiness; configured/unconfigured; mismatch/denial; capability degradation; fairness pressure; erase progress/failure/verification; restore quarantine. |
| CLI Graph Traversal | Start validation; no path; complete path; gap; excluded edge type; unavailable graph; omission; authorization; inspect/recover. |
| CLI Diagnostics & Repair | Healthy; mismatch/divergence; dry run; confirmation; queued; partial; failed; completed; re-verification. |
| MCP Agent Protocol | Schema/validation; authorized success; empty; omission/expansion; degradation; no-safe-axis; timeout; unauthorized; structured recovery/no-hallucination fallback. |
| Downstream Evidence View & Case Briefing | Cold load; focus; empty; weak/partial; stale; missing/unauthorized source; discrepancy/correction; degradation; recovery; responsive transformation. |
| Backend Migration Operations | Assessment; unsupported; dry run; confirmation; queued/admitted; migrating; rollback; failure; verified cutover. |
| Contribution Pipeline | Reproduction; build/test failure; review change; CI failure/success; merge. No product loading/permission state. |

## Timing and Responsiveness

These source-owned budgets determine when the experience must show loading, queueing, delay, timeout, or degradation; the spine does not redefine them.

| Context | Binding product budget and UX consequence |
|---|---|
| Search and traversal | At 10 concurrent queries and 10K units per tenant, p95 syntactic is under 200 ms, semantic under 500 ms, hybrid under 1 second, and graph traversal under 2 seconds at depth no greater than five. When a budget is exceeded, keep scope/result context stable and expose delay or capability degradation rather than a spinner-only wait. |
| Phase 1.5 event freshness | EventStore-convention publication reaches searchable state within 5 seconds under normal conditions; rate limiting, queueing, or incomplete current-revision projection is disclosed. |
| Service cold start | Running containers accept queries within 60 seconds; before readiness, distinguish process liveness from authentication, DAPR-control, and EventStore-command prerequisites. |
| Phase 1 onboarding | The defined clean-machine README/AppHost path reaches the first CLI search in under 30 minutes. <code>quickstart</code> supplements but does not replace this G3 measurement; incomplete CLI verbs currently block the gate. |
| File/URL ingestion | Under normal admitted load, a unit no larger than 10 KB reaches <code>indexed</code> within 60 seconds and one no larger than 1 MB within 5 minutes. Queueing, provider throttling, and fairness delays remain visible; NFR36 evidence is not yet recorded. |
| Recovery/rebuild | With AOF intact, a 10K-unit tenant returns to current-revision <code>indexed</code> within 5 minutes. A full rebuild proceeds no worse than NFR5 throughput, shows progress, restores every non-erased unit, and never resurrects erased content. |
| Future web activation | Representative Evidence Packet and graph fixtures target first usable trust content within 2.5 seconds, p95 local interaction within 200 ms, cumulative layout shift no greater than 0.1, and initial route payload no greater than 256 KiB. Explicit measured replacements are allowed at activation; removing the gate is not. |

## Interaction Primitives

### Scope and trust

Resolve authenticated tenant authority before retrieval or mutation. Missing scope, malformed tenant, claim mismatch, unknown internal app, ungranted tenant, unauthorized source, and cross-tenant request fail closed before upstream work and reveal no restricted evidence. External bearer identity survives hops. Internal calls additionally require protected DAPR channels, deny-by-default workload authorization, operator allowlist mapping to one canonical <code>system:*</code> principal, and an explicit tenant grant. App identity, channel credentials, request fields, display metadata, and case membership never authorize by themselves.

Tenant-wide search may rank results across cases only with mandatory case attribution. Every graph seed, node, edge, and path remains in its authoritative case partition. There is no ambient tenant switch; deliberate confirmation applies only to authorized same-tenant scope expansion.

### Retrieval, explanation, and graph

Hybrid means deterministic weighted reciprocal-rank fusion over available axes. Hybrid explain numbers are per-axis rank contributions; single-axis values retain axis-specific meaning. Numeric weights and RRF <code>k</code> are architecture/contract decisions and are not invented here. Public search has no start-node input; explicit starts belong to CLI <code>traverse</code> or MCP <code>traverse_relations</code>.

The intended hybrid search contract auto-seeds each case-local graph contribution from the union of the top five syntactic and top five semantic candidates, then traverses to depth no greater than two. A presentation may label that start as auto-seeded only from known invocation context; <code>Contracts.V1</code> carries no graph-start-provenance field. Current implementation skips graph when no start is available and does not perform the tenant-wide case-partitioned merge, so current output must disclose the omitted graph contribution instead of presenting the intended behavior as delivered.

Keep compact scope, source, relevance caveat, freshness/degradation, omission, and recovery visible independently of detailed explanation. Detailed ranks, weights, matched terms, and graph diagnostics remain opt-in explain content until the PRD resolves default behavior before web activation.

Graph interaction preserves typed/directional edges, timestamps, case attribution, confidence, and literal gaps. <code>caused_by</code> is not <code>correlated_with</code>. Phase 1 file/URL data does not promise a complete causal graph. Any graphical navigation has a synchronized ordered list/table with equivalent selection, inspection, and recovery.

### Actions, progress, and overlays

Destructive, scope-expanding, reindexing, repair-apply, migration-apply, and erasure actions name tenant, case, target, consequence, and rollback boundary. Cancel is the safe default. Dry run precedes repair or migration apply when supported. Ordinary query refinement, filtering, source opening, and inspection do not trigger confirmation when they remain inside already authorized scope.

Accepted writes stay accepted if non-blocking telemetry later degrades. Retries and duplicate delivery retain one operation identity. Asynchronous updates append or replace a labelled status predictably; spinner, motion, or cursor position never carries the only progress signal.

FrontComposer owns application shell, navigation, skip targets, main landmark, global commands, theme/density/settings/account providers, route page composition, and shell accessibility. Memories owns domain packet projection, validation, and emitted intents. The product host owns routes, authorization lifecycle, retrieval, dispatch, notification integration, and live focus movement.

On a page, dialog, or detail panel with two or more sibling titled content regions, use exactly one <code>FluentAccordion</code> and one <code>FluentAccordionItem</code> per region; the primary region starts expanded. Keep page title, breadcrumb, toolbar, Scope Header, Trust Strip, and a lone primary grid/form/chart outside. Never hide the only primary region in disclosure. Evidence Cockpit uses five items, with Evidence and Recovery initially expanded. The Agent Packet Inspector’s one native sanitized-JSON disclosure is not a reusable section pattern.

Use available FrontComposer or centrally pinned Fluent V5 primitives for actions, feedback, data, layout, and overlays. A genuine primitive gap requires semantic, focus, forced-colors, and conformance-test evidence; no nonexistent drawer/panel API is named.

### Legacy action disposition

| Legacy action set | Migrated disposition |
|---|---|
| Source compare, copy, mark conflict, request permission | Source opening/inspection and conflict visibility are retained. Compare/annotation may activate with Phase 2 capabilities; copy and permission-request workflows belong to the downstream host and are not current Memories actions. |
| Agent expand, copy, retry, inspect error | Omission expansion and structured error inspection remain contract-backed. Retry is offered only through a returned recovery action; copy is host-owned and must sanitize secrets and restricted fields. |
| Activity filter, compare, open packet | Filter visibility and packet navigation are retained. Cross-item comparison is Phase 2 host work and is not implied by the current Case Activity Trail specimen. |
| Graph neighborhood, node, and edge actions | Case-local traversal and inspectable nodes/edges are retained through <code>traverse_relations</code> and Graph Path Summary. Interactive neighborhood editing or promotion requires an activated capability and host; it is not a current visual control. |

## Accessibility Floor

### Active CLI

NFR37 is binding for the active CLI but unverified as of 2026-09-12. Before claiming conformance, record a dated artifact, owner, and disposition covering narrow-terminal, redirected-output, no-color, duplication, timeout/cancellation, secret-sanitization, cross-form semantic-parity automation, and a keyboard-only manual walkthrough across the Phase 1 command tree.

The target reading order is scope → result → sources → reasoning/state → recovery. Human, table, JSON, stderr, and exit-code forms must preserve semantics subject to the explicitly documented current export and cancellation wire exceptions.

- Every state, axis, score meaning, omission, progress stage, and recovery has a text label; color, glyph, animation, cursor position, and screen location are supplementary only.
- Human output is bounded and wrappable. Wide tables have a linear alternative; redirected output is deterministic and does not depend on terminal control sequences.
- Progress emits durable stage/failure lines with last update and delay reason. Cancellation and timeout are explicit. Repeated delivery never prints a second created unit.
- Prompts, confirmations, help, and error recovery are fully operable by keyboard alone without excluding other supported input or assistive technologies. Secrets and restricted identifiers never enter copied output, accessible names, diagnostics, or suggestions.

### Future web activation

Inherit <code>FrontComposerShell</code> and FC-A11Y: skip targets, one main landmark, route heading/main focus, logical DOM/tab order, visible focus, non-disruptive async updates, warranted focus movement into navigation/overlays, and focus return to a still-present invoker. Do not recreate shell mechanics in Memories.

WCAG 2.2 AA applies when a web capability activates. All trust work is keyboard operable; status is not color-only; labels/descriptions resolve; grids have names, headers, sort state, and target-qualified actions; graph meaning has an equivalent ordered representation; reduced motion and forced colors retain boundaries and selection. Author-sized pointer targets meet WCAG 2.2 SC 2.5.8, with 44×44 CSS pixels preferred for primary/high-risk touch actions.

Activation requires a dated evidence matrix by approved route and representative state: viewport, 200% text resize, 400% zoom/320-CSS-pixel reflow, focus-not-obscured result, theme, forced colors, reduced motion, input mode, supported browser/AT including NVDA on supported Edge/Chrome, expected accessible name/announcement, keyboard start/end focus, artifact, tester/date, defect or waiver owner, and release disposition. It must consume the then-current <code>Epic17ValidationInventory.Gaps</code> and durable browser summary, giving every applicable row a dated closure or named waiver; specimen evidence never transfers automatically to a product route. Component/axe checks do not substitute for manual browser/AT evidence. NFR32/NFR35 are inactive gates until a product web surface is activated.

## Responsive & Platform

CLI is the active platform. It must remain readable in narrow terminals through wrapping and linear output; no minimum terminal width is invented. Table mode may be dense, but human mode provides the complete linear alternative and JSON remains stable for automation.

Future web runs inside FrontComposer and inherits its breakpoints, page modes, theme, density, providers, and input behavior. Do not migrate the historical 320/768/1024/1440 or 980px breakpoints as product tokens. At 320 CSS pixels and at 400% zoom, trust essentials and primary action remain in one logical column without two-dimensional scrolling except genuinely tabular/graph content with an equivalent alternative.

Desktop may keep a full-width Evidence Grid with adjacent inspection only when reading and focus order remain logical. Tablet reduces simultaneous detail; mobile promotes review, trust inspection, and recovery while advanced graph, operator, schema, or filter work moves into an accessible focused route or overlay chosen from real current primitives. Scope Header and Trust Strip remain before the answer at every width. The product has no offline requirement; loss of service is an explicit recoverable error.

## Key Flows

### J1 — Alex — "Zero to First Search" (Phase 1.5 launch path)

**Protagonist:** Alex, an EventStore developer replacing a failed ad-hoc memory stack. **Phase/surface:** Phase 1.5 EventStore integration plus CLI; preview until launch gates pass.

1. Alex installs the EventStore package, configures the DAPR subscription, supplies the embedding secret through the approved secret boundary, and boots AppHost.
2. Alex publishes an EventStore-convention test event and sees acceptance/projection progress without equating publish with searchable completion.
3. Alex runs <code>memories search query --tenant claims --query "claim denied"</code>.
4. The result names scope, sources, case-local causal evidence, state, and recovery in the CLI reading order.
5. Alex reruns with <code>--explain</code> and sees syntactic, semantic, and graph rank contributions plus the relevance-not-fact caveat.
6. **Climax:** the sourced CausationId chain answers why the claim was denied inside the separate L2 onboarding clock.

**Failure/recovery:** Missing subscription, secret, handler, or current-revision projection acknowledgement prevents a success-looking result. Name the failed prerequisite/stage and route Alex to handler diagnostics, replay/retry, or boot guidance; do not claim launch before L1–L3.

### J2 — Alex — "Something's Wrong" (Debug Path)

**Protagonist:** Alex, diagnosing a claim that should be searchable. **Phase/surface:** Phase 1.5 CLI diagnostics plus logs; some source-narrative verbs are not current registrations.

1. Alex repeats the scoped query and receives an empty classification, not a generic zero.
2. Current CLI exposes <code>memories status telemetry --tenant claims</code>; target case/failed status is absent, so per-case evidence must not be fabricated.
3. Alex runs <code>memories handlers list</code> and <code>memories handlers mismatches --tenant claims</code>, then inspects DAPR logs for the named schema failure.
4. The diagnostics identify the unregistered event version and state its effect on ingestion/searchability.
5. Alex registers the handler and uses the available replay/recovery path outside invented CLI grammar.
6. **Climax:** the same query returns the Henderson claim with its case-local chain intact.

**Failure/recovery:** If replay, projection, or a selected backend remains unavailable, keep the failed stage, unavailable axes, last update, and next safe action visible. The stale source phrase <code>handlers --list</code> is never emitted.

### J3 — Alex — "Wiring Up the AI Assistant" (MCP Integration, Phase 1.5)

**Protagonist:** Alex, configuring a team assistant. **Phase/surface:** Phase 1.5 MCP preview and its documentation.

1. Alex registers the exact four tools: <code>search_memory</code>, <code>ingest_content</code>, <code>traverse_relations</code>, and <code>get_case_info</code>.
2. The assistant calls <code>search_memory</code> with authorized <code>tenantId</code>, query, optional <code>case</code>, and <code>axes</code>.
3. An oversized result discloses omitted detail rather than silently truncating.
4. Alex supplies <code>tokenBudget=2000</code> and retries the same query.
5. The assistant uses the returned sources and, when graph detail is required, calls <code>traverse_relations</code>.
6. **Climax:** it produces a concise, sourced answer and a case-local causal chain within budget.

**Failure/recovery:** Schema validation, tenant denial, timeout, unavailable axes, and omission return structured recovery. If memory is unavailable, the assistant tells the user and does not hallucinate organizational context.

### J4 — Marcus — "Brief the New Person" (Phase 2)

**Protagonist:** Marcus, a team lead onboarding Tomás. **Phase/surface:** Phase 2 downstream Case Briefing and Case Activity Trail.

1. Marcus arranges Tomás’s real tenant authorization; case membership is recorded only as attribution metadata.
2. Tomás requests a project briefing in the downstream assistant/application.
3. The Phase 2 composition returns chronology, key decisions, sources, and case-local causal detail.
4. Tomás opens sources and distinguishes the February 12 approval from the February 14 implementation.
5. Tomás submits a correction annotation with source and timeline context.
6. **Climax:** Tomás can explain why the dual-provider decision exists and begin useful work with verifiable evidence.

**Failure/recovery:** Sparse, stale, conflicting, unauthorized, or missing sources downgrade the briefing and attach recovery; membership never opens access.

### J5 — Kenji — "New Tenant, No Drama" (MVP)

**Protagonist:** Kenji, the platform operator for multiple business units. **Phase/surface:** Phase 1 target CLI tenant operations; only <code>tenant list</code> is currently registered.

1. Kenji requests tenant creation through the target Phase 1 operation, which provisions tenant-scoped principals and indexes before data writes.
2. The operation reports admission/progress and an isolation-verification next step.
3. Kenji runs the target principal-driven verification; it tests search, ingest, traversal, malformed IDs, and colliding graph IDs as tenant A against tenant B.
4. **Climax:** the new tenant is usable only after the isolation outcome is explicit and verified.
5. Later, an intern requests <code>--tenant bu-operations</code> with a bu-compliance claim.
6. The request fails before retrieval, names the authenticated tenant safely, returns no other-tenant evidence, and records sanitized access telemetry.

**Failure/recovery:** Current create/verify verbs are absent, so the CLI must identify them as unavailable rather than simulate success. Provisioning, verification, erasure, or pre-Production telemetry qualification failures fail closed with operator-owned recovery. After Production admission, bounded telemetry delivery failure surfaces degradation but does not reject or roll back an accepted domain write.

### J6 — Kenji — "Time to Scale" (Phase 3)

**Protagonist:** Kenji, operating a two-million-unit tenant. **Phase/surface:** Phase 3 Backend Migration Operations; no current command grammar is committed.

1. Kenji reviews capacity, growth, tenant impact, and supported target evidence.
2. He requests a dry-run plan with data volume, expected/unknown duration, queue impact, cutover, rollback, and verification.
3. The system checks authorization, tenant isolation, workload-fair admission, and rollback readiness.
4. Kenji confirms the named tenant/target and begins migration.
5. Queries remain on the safe backend until the target is ready; progress and degradation stay visible.
6. **Climax:** verified cutover completes without a silent availability or tenant-boundary break.

**Failure/recovery:** Unsupported target, queue starvation, failed copy, validation mismatch, or cutover failure keeps the old safe path or executes the documented rollback. Source-narrative <code>backend assess/migrate</code> text is illustrative, not a current registration.

### J7 — LLM Agent — Technical Integration Path

**Protagonist:** the source-named LLM Agent, a non-human protocol actor. **Phase/surface:** Phase 1.5 MCP only; this flow explicitly requires no human screen.

1. The application registers the exact four tool schemas.
2. A prompt requiring organizational context triggers an authorized <code>search_memory</code> call with a bounded token budget.
3. The server searches selected available axes, returns typed sources and Evidence Packet state, and distinguishes exclusion, unavailability, and no hits.
4. The agent evaluates scope, freshness, relevance caveat, omission, and degradation before composing.
5. It follows an expansion/retry/traversal recovery when supported.
6. **Climax:** the agent answers with attributed evidence and causal grounding when available, inside budget.

**Failure/recovery:** Empty suggests safe refinement/case inspection; stale adds caveat; timeout uses retry guidance; partial names unavailable axes; unauthorized/no-safe-axis returns no inferred answer. Agent Packet Inspector may aid developers later but is not required for the protocol journey.

### J8 — Priya — "I Need to Understand This Case" (Phase 2 downstream application)

**Protagonist:** Priya, a claims adjuster using Alex’s downstream application. **Phase/surface:** Phase 2 downstream Evidence View.

1. Priya opens claim 7293 and asks what happened.
2. The application presents an ordered narrative with Scope Header and compact trust essentials before detail.
3. Priya asks why partial coverage was approved and receives a case-local causal explanation.
4. She opens Source Citation Stack entries for the assessments, policy clause, and approval note.
5. Relevance is labelled as relevance rather than verified fact; she reads the primary source.
6. **Climax:** Priya answers the claimant accurately and verifies follow-up evidence during the call.

**Failure/recovery:** Missing, stale, conflicting, degraded, or unauthorized evidence is named beside the affected claim, with a safe source/refine/retry action; the application never invents completeness.

### J9 — Alex — "The First Case" (Empty State)

**Protagonist:** Alex, a developer with a healthy but empty Phase 1 stack. **Phase/surface:** Phase 1 README/manual onboarding plus CLI; case/ingest target verbs currently block G3.

1. Alex’s first scoped query returns a true empty-tenant explanation and only implemented next actions.
2. The manual target path requires AppHost boot → tenant create → case create → ingest → search; <code>quickstart</code> may supplement it.
3. Target case creation reports the new empty case and next ingest action.
4. Target directory ingest reports acceptance, admission delay, each human lifecycle state, counts, and current-revision completion.
5. Alex runs the first real-data hybrid query with explicit tenant and case.
6. **Climax:** ranked, sourced results replace the empty state and expose inspect/explain recovery.

**Failure/recovery:** Current <code>case</code> and <code>ingest</code> groups are placeholders, so the live experience must say unavailable and exit 2; it must not print the target success copy. Event auto-indexing is not offered as the Phase 1 empty-state path.

### J10 — The Contributor — "From Bug Report to First PR"

**Protagonist:** Dani, an external .NET contributor. **Phase/surface:** repository/GitHub/build/test/CI infrastructure; this flow explicitly warrants no Memories product screen.

1. Dani files the explain-output issue with a concrete use case.
2. Maintainer guidance points to the relevant source and contribution rules.
3. Dani clones, builds, runs tests, changes the contract/formatter safely, and adds coverage.
4. CI checks the contribution and reports actionable failures.
5. Review requests one naming correction; Dani updates it.
6. **Climax:** CI passes and the first PR merges.

**Failure/recovery:** Build, test, contract, or CI failures link to the owning evidence and remediation. Product components, routes, or fake in-app contribution states are not created.

## Inspiration & Anti-patterns

**Historical-reference warning:** the [May visual-direction study](../../ux-design-directions.html) is non-normative composition evidence, not implementation, accessibility, component, token, command, or contract evidence. Do not copy its HTML, CSS, JavaScript, ARIA, hard-coded colors, fixed dimensions/breakpoints, sample scores, or legacy component names. The two spines win on conflict with any retained visual reference.

Useful inspiration is limited to hierarchy: persistent scope/trust before evidence; evidence beside inspectable detail; activity as an ordered continuity view; agent payload/budget/omission inspection; operator capability/blast-radius inspection; and onboarding progress beside first proof and recovery. There are currently no files in <code>mockups/</code>, <code>wireframes/</code>, or <code>imports/</code>; every surface is spine-only and the current specimen is code conformance evidence.

Avoid chat-only answers, decorative AI mystique, generic dashboard sprawl, universal browser parity, silent partial failure, numeric-only trust, ambiguous confidence, cross-case graph paths, ambient tenant switching, membership-as-authorization, spinner-only progress, invented MCP fields, target CLI commands presented as shipped, legal-audit claims, and treating fixture routes as product IA.
