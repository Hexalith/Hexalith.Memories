# Epic 25 Context: Architecture Factorization & Code Health

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic turns already-delivered behavior into a cleaner, safer implementation surface for maintainers. It extracts inline endpoint code, centralizes repeated error, tenant-validation, telemetry, routing, client, CLI, and MCP idioms, separates public contracts from backend persistence details, brings the evidence cockpit into the mandated FrontComposer and Fluent UI model, and removes dead topology so future changes remain testable and backend-migration ready without changing product behavior.

## Stories

- Story 25.1: Program.cs Decomposition
- Story 25.2: Error & Telemetry Centralization
- Story 25.3: Shared Route Table & Client Consolidation
- Story 25.4: Contract/Persistence Separation & Route Versioning
- Story 25.5: CLI Consolidation
- Story 25.6: MCP Tool Executor
- Story 25.7: Evidence Cockpit UX Conformance
- Story 25.8: Dead-Code & Topology Cleanup

## Requirements & Constraints

Epic 25 is a code-health and operational-readiness epic. It must not remove or alter existing product capabilities; integration, contract, output-format, exit-code, MCP tool-contract, and web conformance coverage remain the behavioral safety net.

Public REST behavior must remain consumable by external clients, CLI, MCP, and third-party HTTP callers. Route definitions should become single-source so server endpoints and REST clients cannot drift. Public HTTP paths should move under a versioned API shape while preserving compatibility intentionally.

Errors must keep structured envelopes across REST, CLI, MCP, and DAPR-facing paths. Internal DAPR or backend failures must retain enough component and recovery context for CLI and agent consumers to show actionable diagnostics rather than collapsing into generic gateway failures. Unhandled exceptions must still map to the shared envelope.

Tenant identity and tenant-active validation must be enforced consistently through endpoint filters or equivalent shared boundaries. This epic must not weaken existing authentication, authorization, or physical tenant-isolation behavior; it should make those checks easier to reuse after endpoint extraction.

Contracts exposed through the public package must avoid Redis, RediSearch, FalkorDB, or persistence-specific naming. Backend-specific stored-state DTOs and serialization contexts belong outside the public contract surface. Domain and client contracts should stay axis-oriented and implementation-neutral so backend swaps do not leak into URLs, response models, or package consumers.

CLI and MCP remain first-class product surfaces, not thin afterthoughts. CLI commands must keep multiple output formats and actionable errors. MCP tools must keep typed schemas, bounded/token-budget-aware responses, structured errors, tenant scoping, and source/evidence semantics that agents can consume without parsing prose.

Dead-code and topology cleanup should delete only code or boundaries that are proven unused, duplicated, or intentionally superseded. Project dependency boundaries should be fixed or explicitly documented when they are intentionally unusual; do not hide unresolved topology questions behind incidental cleanup.

## Technical Decisions

Hexalith.Memories is a DAPR-native .NET/Aspire system. External consumers use REST through ingress, MCP reaches the Memories Server through DAPR service invocation, and the server coordinates Redis/RediSearch/Redis Vector/FalkorDB through the established backend and sidecar boundaries.

The Server package should depend on public contracts, not directly on Redis implementation details. Backend implementations are registered at composition roots, preserving the future extraction path for memory index and graph backends.

Use versioned contract namespaces for public models and keep backward-compatible additions as the default evolution path. Public HTTP routes should be versioned through a shared route table so server, REST client, CLI, and MCP callers converge on the same path definitions.

Endpoint extraction should preserve the Minimal API route-group shape while moving resource-specific mappings into focused endpoint classes. The composition root should retain orchestration and registration responsibilities only.

Error handling, tenant validation, tenant-active checks, endpoint telemetry, and exception handling should be reusable infrastructure instead of repeated endpoint-local code. Shared result factories, endpoint filters, telemetry filters, and exception handlers should be easy to apply to extracted route groups.

OpenTelemetry traces should propagate across CLI, ingress, server, MCP, and backend hops. Structured JSON logging should carry trace correlation, and custom metrics should use stable, low-cardinality dimensions suitable for Aspire dashboard visibility.

The REST client should consolidate HTTP send/decode behavior behind a generic path so individual client methods express request intent rather than repeating transport, deserialization, and error-mapping ceremony.

The MCP server should centralize validate/authorize/catch/error-mapping behavior in one executor. Tenant authorization must come from one source per tool invocation to avoid drift or redundant checks.

## UX & Interaction Patterns

The evidence packet is the shared trust object across CLI JSON output, MCP responses, and future web UI. It should consistently carry tenant and case scope, source attribution, evidence strength, retrieval-axis explanation, freshness, degraded-backend state, omitted details, and recovery guidance.

The evidence cockpit must use FrontComposer and Fluent UI Blazor V5 components with Fluent 2 tokens. Raw interactive HTML controls, custom primitives, legacy Fluent tokens, and hardcoded user-facing strings are not acceptable when a FrontComposer or Fluent component or localized resource should own the behavior.

Web evidence sections should use accessible Fluent/FrontComposer structure and progressive disclosure rather than ad hoc headings and hand-built layouts. Trust strips, recovery action panels, agent packet inspection, and status feedback should preserve the same evidence semantics used by CLI and MCP. Empty, unavailable, stale, degraded, weak-evidence, unauthorized, and out-of-scope states must remain visibly distinct and support safe next actions.

## Review Checklist — Epic 23 Ingestion Invariants

| Invariant | Required review evidence | Status |
|---|---|---|
| Claim-check workflow payloads | Inspect scheduler/claim-check/workflow inputs and tests; prove raw source bytes and large intermediate values are replaced by scoped references. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |
| Captured workflow configuration | Inspect scheduler capture and orchestrator reads; prove retry/NL settings come from durable workflow input. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |
| Chunked semantic vectors | Inspect chunk key construction/index writes, base-ID parsing, and semantic-result deduplication tests; prove `{tenant}:vec:{memoryUnitId}:{sequence}` while hashes and parsed/results-facing identity retain the base `MemoryUnitId`. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |
| Source-payload retention | Inspect failed-unit persistence/cleanup/re-ingestion and negative tests; prove a retained pointer or actionable rejection. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |
| Tenant index readiness | Inspect all four indexing activities, readiness verification, maintenance adapter, provisioning activities/workflow, and ownership guards; prove no on-demand index creation, fail-closed missing-index behavior, sole provisioning ownership, and only Story 23.7-approved in-place upgrades for known additive TAG fields before readiness is cached. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |
| Single-operation admission | Inspect Story 23.5 ingestion embedding activities, captured configuration, and actor/logic assertions; prove one admission for `GenerateEmbeddingActivity`'s single provider operation and one per bounded batch in `GenerateChunkEmbeddingsActivity`. | Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md). |

Every applicable verdict must record a rerunnable evidence command or artifact, reviewer, date, and pass/fail/blocked result. `N/A` requires diff evidence that the reviewed change cannot affect the invariant. The statuses above are dated corrective verification of the current tree, not evidence that this checklist ran during every original Epic 25 story review.

## Cross-Story Dependencies

Endpoint decomposition depends on reusable authentication, tenant authorization, and tenant-active validation boundaries because extracted route groups need consistent security behavior.

The shared route table and route versioning work should be coordinated before CLI, REST client, or MCP code is updated broadly; otherwise consumers may chase transient route names.

Contract/persistence separation affects server serialization, REST client models, CLI JSON output, MCP schemas, and the evidence cockpit. Treat public wire-shape compatibility as a cross-surface constraint while moving stored-state DTOs out of the public package.

Evidence cockpit conformance depends on the shared evidence packet semantics and unavailable-state mapper so the web surface does not invent a divergent representation of failure or degraded evidence.
