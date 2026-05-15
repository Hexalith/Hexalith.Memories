---
stepsCompleted: [1, 2, 3]
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md
  - _bmad-output/planning-artifacts/research/technical-kreuzberg-ocr-research-2026-03-28.md
  - _bmad-output/project-context.md
  - Hexalith.Commons/_bmad-output/project-context.md
  - Hexalith.FrontComposer/_bmad-output/project-context.md
  - Hexalith.EventStore/_bmad-output/project-context.md
---

# UX Design Specification Hexalith.Memories

**Author:** JeromePiquot
**Date:** 2026-05-15

---

<!-- UX design content will be appended sequentially through collaborative workflow steps -->

## Executive Summary

### Project Vision

Hexalith.Memories is an open-source relational memory server for AI-enabled .NET/DAPR systems. Its UX promise is recoverable trust: users can discover what knowledge exists, understand how it is connected, see why a result appeared, verify the tenant and case boundary, and know what to do when something is missing, stale, or degraded.

The product should make organizational and event-sourced knowledge feel searchable, explainable, and causally connected. Its signature experience is answering questions like "why did this happen?" or "what led to this decision?" with sourced, tenant-scoped, token-aware narratives that expose the evidence path behind the answer.

The first memorable journey should be simple: ingest a small case, ask a why-oriented question, receive a sourced answer with visible retrieval evidence, and understand the next step if the answer is incomplete.

This is not simply a search UX. It is a state-and-trust UX for memory infrastructure: the user must always be able to tell which tenant and case they are operating in, whether isolation is physically enforced, what has been ingested or is still pending, which retrieval axes contributed, what sources support the answer, and which next action will recover from an empty result, stale memory, degraded backend, permission boundary, or evidence gap.

Trustworthy means the user can verify scope, source, freshness, retrieval path, confidence, and recovery action without leaving the current workflow.

### Target Users

The primary user is Alex, a senior .NET/DAPR developer who wants reliable memory infrastructure without weeks of custom RAG plumbing. Alex's first trust moment is not installation alone; it is ingesting real or sample case data, seeing a first sourced result, understanding why it ranked, and feeling confident enough to remove fragile custom infrastructure. His adoption decision depends on whether Memories can replace custom RAG plumbing without becoming another opaque dependency.

The non-human user is the LLM agent, which needs predictable MCP tools, typed schemas, bounded responses, source attribution, confidence signals, token-budget discipline, and structured errors it can act on. For the agent, UX is response shape: stable contracts, bounded payloads, source-backed answers, and failures that explain whether to retry, narrow scope, request ingestion, or escalate. MCP responses should behave like decision-ready packets: typed, bounded, attributed, confidence-aware, and useful even when token budget forces compression.

Marcus, the team lead, needs case-level continuity: briefings, activity, memory health, and eventually change-over-time views that help new team members become productive faster. Marcus should not need retrieval theory to understand whether a case has useful memory.

Kenji, the operator, needs tenant provisioning, isolation verification, health, rate limits, failure visibility, and backend degradation surfaced as clear operational states. Kenji's trust comes from knowing the system can prove isolation and expose risk before it becomes an incident.

Priya, the end beneficiary, experiences the value indirectly through applications built on top of Memories. For her, the winning UX is not technical explain output; it is being able to find, understand, and trust case context without knowing the infrastructure exists.

### Key Design Challenges

The central UX challenge is making a technically dense system feel trustworthy quickly. Three-axis retrieval, causal graph traversal, tenant isolation, asynchronous ingestion, MCP/CLI surfaces, and backend degradation must be visible enough to build confidence without overwhelming the user.

Trust also depends on failure design. Empty states, stale memory warnings, partial retrieval, degraded graph/vector backends, and tenant or permission boundaries must be expressed as understandable states with recovery paths, not as generic errors.

Onboarding has a hard success gate: under 30 minutes from package install to first trustworthy search result. The experience must therefore reduce setup ambiguity, surface prerequisites, show ingestion progress, provide strong empty states, and make the next action obvious when something fails.

Memory lifecycle visibility is another core challenge. Users need to distinguish absent knowledge from pending ingestion, stale memory from conflicting memory, and degraded retrieval from a true no-result state. These states must be surfaced consistently so users and agents can recover without guessing.

Another challenge is designing for multiple surfaces with different mental models and maturity horizons. CLI and MCP experiences must be reliable first-class surfaces from the start, while future application experiences should compose the same evidence model into case briefings, narratives, and activity views.

The product also needs consistent trust signals across surfaces. Tenant, case, isolation status, ingestion status, source attribution, confidence, retrieval axes, graph relationships, freshness, and token budget should feel like one coherent evidence model rather than unrelated implementation details.

### Design Opportunities

The strongest UX opportunity is explainability as a product feature. Every result can show syntactic, semantic, and graph-based reasons in a way that helps users trust, debug, and tune the system.

A second opportunity is debug-first developer and operator experience. Commands such as search, status, handlers, tenant verify, quickstart, and explain can form a developer/operator cockpit where every degraded state includes cause, impact, and next action.

A third opportunity is case-centered memory. Case briefings, memory diffing, causal chain narratives, and activity views can make Memories feel distinct from ordinary vector search by helping teams understand the story of a case, not just retrieve matching fragments.

A fourth opportunity is progressive disclosure across audiences. Alex and Kenji can inspect scores, tenant boundaries, pipeline stages, and structured errors, while Marcus and Priya can receive higher-level narratives with sources and confidence indicators. The UX should preserve one shared evidence model underneath while adapting the level of detail shown by surface and audience.

A fifth opportunity is trustworthy absence. Memories can distinguish between "nothing found," "not ingested yet," "not accessible in this tenant," "retrieval degraded," and "answerable only with low confidence," giving users and agents a safer way to proceed when memory is incomplete.

The executive design goal is therefore measurable: within 30 minutes, a new developer should be able to install Memories, ingest sample or local knowledge, run a first tenant-scoped search, inspect why the result appeared, and understand the next recovery action if the result is empty, stale, or degraded.

The design north star is simple: every answer should make the system more inspectable, not more mysterious.

## Core User Experience

### Defining Experience

The core experience is the trust loop: search, verify the result, and inspect where the result came from. Users should be able to ask once and receive an evidence packet: ranked results, synthesized answer, or both; source attribution; evidence strength scoring; explain breakdown; tenant and case context; freshness signals; and graph relationships.

At minimum, every evidence packet should identify the result or answer, tenant and case scope, top source references, evidence strength, freshness status, retrieval axes used, explain summary, graph relationship summary when relevant, and the next recovery action when evidence is weak, incomplete, absent, or out of scope. If details are omitted for compactness or token budget, the response must say what was omitted and how to expand it.

Confidence should represent evidence strength and retrieval quality, not an unsupported claim of factual certainty.

The primary user action is not merely submitting a query. Search is the input; verified understanding is the outcome. A successful search should answer three questions immediately: what did Memories find, why should I trust it, and where did it come from?

Verification means the user or agent can identify the selected tenant and case, inspect the cited source, see the reason the result ranked, understand confidence limits, and choose a next action without leaving the current surface.

The simplest product promise is: search returns evidence. Follow-up commands, tool calls, or UI exploration may deepen that evidence, but they should not be required for first trust.

The evidence packet must be compact by default and expandable by design. Users should see enough to proceed safely without being forced through every score, graph edge, or diagnostic detail.

### Platform Strategy

CLI, MCP, and web UI are all first-class surfaces. None should be treated as a secondary wrapper around another experience. Each surface must support the same core trust loop and expose the same evidence model, while adapting density, formatting, and interaction controls to its audience and context.

The CLI should be optimized for keyboard-driven developer and operator workflows, including compact explain output, actionable diagnostics, tenant/case visibility, and scriptable formats.

The MCP surface should be optimized for LLM agents, with typed schemas, bounded payloads, deterministic expansion handles, source attribution, evidence strength signals, token-budget awareness, and structured failure semantics. MCP responses must treat evidence as structured data, not prose-only explanation, so agents can inspect confidence, source references, omitted fields, recovery actions, and structured errors without parsing natural language.

The web UI should support human verification and exploration, especially result inspection, source review, causal chain browsing, case context, and confidence cues.

All three surfaces should use the same underlying evidence model and error semantics. Differences should be presentational: terminal density, MCP schema shape, and browser exploration depth.

Verification must work in both terminal and browser contexts. There is no offline requirement.

### Effortless Interactions

After a search, Memories should automatically perform source lookup, evidence strength scoring, explain breakdown, and relevant graph traversal. The user should not need separate commands or manual correlation to understand why a result appeared.

The effortless promise is: ask once, receive the evidence needed to trust, challenge, or continue. Memories should eliminate the common RAG burden of manually stitching together vector matches, source documents, graph context, tenant scope, and confidence judgment.

Automatic explanation should be bounded and predictable. Memories should not hide retrieval work, but it also should not flood the user with every candidate, score, or graph edge unless the user expands the evidence.

The UX must answer recurring support questions directly: why did I get this result, why did I get nothing, can I use this answer safely, and is this from the right tenant and case?

Deeper inspection should remain available through commands, MCP fields, or UI panels, but the initial response should already contain enough evidence for a user or agent to decide whether to trust, refine, or recover.

The system should also make absence effortless to interpret. Empty or weak results should distinguish between no match, pending ingestion, stale memory, inaccessible tenant or case, degraded backend, and genuinely low evidence strength.

Every empty, weak, or degraded result should include the most likely cause, the diagnostic signal behind that cause, and the next safest action. Recovery actions should be specific, such as refine query, correct tenant or case selection, broaden case scope, inspect ingestion status, inspect degraded services, refresh stale memory, request more sources, or traverse related graph nodes.

When evidence conflicts, Memories should show the conflict instead of smoothing it away: competing sources, stale versus fresh memory, high lexical match with weak graph support, or strong graph context with weak source confidence.

Wrong-scope evidence is a critical failure state. If tenant or case scope is ambiguous, unavailable, or inconsistent with the result, Memories should degrade to a warning or refusal rather than present the answer as verified.

### Testable Experience Criteria

A core search experience is successful when a user or agent can determine, from the first response, the answer or result found, the tenant and case scope used, the primary sources, the evidence strength, the freshness of the evidence, why the result ranked or appeared, and the next action if trust is incomplete.

A no-result experience is successful when the response distinguishes between no match, low evidence strength, missing or delayed ingestion, stale memory, inaccessible tenant or case, and backend degradation.

A cross-surface experience is successful when the same query in CLI, MCP, and web UI exposes equivalent evidence fields, even when each surface presents them at different density.

### Critical Success Moments

The first major success moment is a developer running a search and seeing not just relevant output, but a traceable evidence path back to source material and graph relationships.

A second critical success moment is a weak or empty search that still feels useful because Memories explains what failed, what evidence was checked, and what recovery action is safest.

The make-or-break onboarding moment happens within 30 minutes: install or run Memories, ingest sample or local content, execute a tenant-scoped search, inspect why the result appeared, and understand the next action if the result is incomplete.

The strongest differentiation moment is when a why-oriented question returns a sourced, tenant-safe, token-aware narrative with visible retrieval evidence and causal context.

The fastest failure mode is opacity: a confident-looking answer with unclear source, hidden tenant/case scope, missing freshness signals, no explanation of confidence, or no recovery path.

### Experience Principles

1. Search Must Return Evidence
Every search should produce enough source, evidence strength, explain, freshness, and graph context for the user or agent to verify the result.

2. Evidence Should Be Progressive
The first response should be compact enough to scan, but expandable into scores, source details, graph paths, freshness, and diagnostics.

3. Trust State Must Be Visible
Tenant, case, source, freshness, evidence strength rationale, retrieval axes, and backend degradation should be visible as part of the experience, not hidden in logs.

4. Tenant Safety Is Non-Negotiable
Tenant and case scope must be enforced before retrieval and visible after retrieval. UX should expose scope clearly, but never rely on user inspection as the safety mechanism.

5. One Query Should Start the Full Trust Loop
Users should not have to manually run separate commands to discover why a result appeared or where it came from.

6. Deeper Inspection Should Be Optional, Not Required
Follow-up commands, MCP fields, and UI panels should enrich evidence, not compensate for an opaque first response.

7. Absence Must Be Actionable
No-result and low-confidence states should explain the likely cause and provide the next recovery action.

8. Conflicts Must Be Exposed
When sources, scores, freshness, or graph context disagree, the experience should reveal the disagreement clearly enough for the user or agent to make a safe judgment.

9. Scope Errors Are Safety Errors
Tenant and case ambiguity, mismatch, or inaccessible evidence should be treated as trust-blocking conditions, not minor diagnostics.

10. Same Evidence, Different Density
CLI, MCP, and web UI should share the same evidence model while presenting it at the right level of detail for developers, operators, agents, and application users.
