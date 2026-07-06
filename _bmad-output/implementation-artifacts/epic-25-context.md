# Epic 25 Context: Architecture Factorization & Code Health

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

This epic turns already-delivered behavior into a cleaner, safer implementation surface for maintainers. It reduces the oversized composition root, centralizes duplicated error, tenant-validation, telemetry, routing, client, CLI, and MCP idioms, separates public contracts from backend persistence details, brings the evidence cockpit into the mandated FrontComposer/Fluent UX model, and removes dead topology. The purpose is maintainability and backend-migration readiness without changing product behavior.

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

Epic 25 is a code-health and operational-readiness epic. It must not remove or alter existing product capabilities; existing integration, contract, output-format, exit-code, MCP tool-contract, and web conformance coverage remains the behavioral safety net.

Public REST behavior must remain consumable by external clients, CLI, MCP, and third-party HTTP callers. Route definitions should become single-source so server endpoints and REST clients cannot drift. Route versioning for public HTTP paths is required for this epic; preserve compatibility intentionally while moving routes under the versioned API shape.

Errors must keep Hexalith-style structured envelopes across REST, CLI, MCP, and DAPR-facing paths. Internal DAPR or backend failures must retain enough component and recovery context for CLI and agent consumers to show actionable diagnostics rather than collapsing into generic gateway failures. Unhandled exceptions must still map to the shared envelope.

Tenant identity and tenant-active validation must be enforced consistently through endpoint filters or equivalent shared boundaries. This epic must not weaken the security and tenant-authorization work from Epic 20; it should make those checks easier to reuse after endpoint extraction.

Contracts exposed through the public package must avoid Redis, RediSearch, FalkorDB, or persistence-specific naming. Backend-specific stored-state DTOs and serialization contexts belong outside the public contract surface. This supports the backend-migration NFR by keeping domain and client contracts axis-oriented rather than implementation-oriented.

CLI and MCP remain first-class product surfaces, not thin afterthoughts. CLI commands must keep multiple output formats and actionable errors. MCP tools must keep typed schemas, bounded/token-budget-aware responses, structured errors, tenant scoping, and source/evidence semantics that agents can consume without parsing prose.

Dead-code and topology cleanup should delete only code or boundaries that are proven unused, duplicated, or intentionally superseded. Project dependency boundaries should be fixed or explicitly documented when they are intentionally unusual; do not hide unresolved topology questions behind incidental cleanup.

## Technical Decisions

Hexalith.Memories is a DAPR-native .NET/Aspire system. External consumers use REST through ingress, MCP reaches the Memories Server through DAPR service invocation, and the server coordinates Redis/RediSearch/Redis Vector/FalkorDB through the established backend and sidecar boundaries.

The Server package should depend on public contracts, not directly on Redis implementation details. Backend implementations are registered at composition roots, preserving the future extraction path for memory index and graph backends.

Use versioned contract namespaces for public models and keep backward-compatible additions as the default evolution path. For this epic, public HTTP routes must also be versioned with the shared route table so server, REST client, CLI, and MCP callers converge on the same path definitions.

Endpoint extraction should preserve the Minimal API route-group shape while moving resource-specific mappings into focused endpoint classes. The composition root should retain orchestration and registration responsibilities only.

Error handling, tenant validation, tenant-active checks, endpoint telemetry, and exception handling should be reusable infrastructure instead of repeated endpoint-local code. Shared filters and factories should be easy to apply to extracted route groups.

The REST client should consolidate HTTP send/decode behavior behind a generic path so individual client methods express request intent rather than repeating transport, deserialization, and error-mapping ceremony.

The MCP server should centralize validate/authorize/catch/error-mapping behavior in one executor. Tenant authorization must come from one source per tool invocation to avoid drift or redundant checks.

## UX & Interaction Patterns

The evidence packet is the shared trust object across CLI JSON output, MCP responses, and future web UI. It should consistently carry tenant and case scope, source attribution, evidence strength, retrieval-axis explanation, freshness, degraded-backend state, omitted details, and recovery guidance.

The evidence cockpit must use FrontComposer and Fluent UI Blazor V5 components and Fluent 2 tokens. Raw interactive HTML controls, custom primitives, legacy Fluent tokens, and hardcoded user-facing strings are not acceptable when a FrontComposer or Fluent component/localized resource should own the behavior.

Web evidence sections should use accessible Fluent/FrontComposer structure and progressive disclosure rather than ad hoc headings and hand-built layouts. Empty, unavailable, stale, degraded, weak-evidence, unauthorized, and out-of-scope states must remain visibly distinct and support safe next actions.

## Cross-Story Dependencies

Endpoint decomposition is safer after Epic 20's authentication and tenant-authorization filters exist, because extracted route groups need a reusable security boundary.

The shared route table and route versioning work should be coordinated before CLI, REST client, or MCP code is updated broadly; otherwise consumers may chase transient route names.

Contract/persistence separation affects server serialization, REST client models, CLI JSON output, MCP schemas, and the evidence cockpit. Treat public wire-shape compatibility as a cross-surface constraint while moving stored-state DTOs out of the public package.

Evidence cockpit conformance depends on the shared evidence packet semantics and unavailable-state mapper so the web surface does not invent a divergent representation of failure or degraded evidence.
