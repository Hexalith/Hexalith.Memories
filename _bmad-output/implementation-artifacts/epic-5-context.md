# Epic 5 Context: Tenant Isolation & Multi-Tenancy

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give operators a verifiable tenant lifecycle and give every product path a trustworthy tenant boundary. Provisioned tenants have isolated backend resources and configuration; deletion reaches verified, irreversible erasure. Search and ingestion remain useful during recoverable backend failures without hiding missing capabilities or weakening tenant isolation. Zero cross-tenant data leakage is a release gate.

## Stories

- Story 5.1: Tenant Provisioning Workflow
- Story 5.2: Tenant Deletion Workflow
- Story 5.3: Tenant Isolation Verification
- Story 5.4: Tenant Context Enforcement
- Story 5.5: Tenant Configuration & Listing
- Story 5.6: Graceful Degradation on Backend Failure

## Requirements & Constraints

- Tenant creation is one operator command and should complete within five minutes under the stated onboarding conditions. Provisioning verifies usable, empty resources before activation. Missing or inactive resources cause clear validation failures; product request paths do not create them.
- Isolation covers search, ingestion, graph traversal, and internal calls. Automated negative checks authenticate as one tenant and attempt access to another using swapped, malformed, and empty identifiers, colliding graph edge IDs, and foreign index names. A report identifies each check and any failure. A tenant-isolation failure is a safety error.
- Tenant listing and configuration expose lifecycle status, backend/index health, activity and usage, embedding settings, and rate limits. Non-breaking updates take effect promptly; changes requiring reindex need explicit acknowledgment and visible impact. Embedding throttling uses the tenant ceiling, and vector records retain provider/model provenance.
- Deletion closes ordinary admission, prevents late writes from recreating data, and completes only after verified purge of source-controlled projections, content stores, coordination state, and derived artifacts plus irreversible loss of access to tenant-key-protected EventStore payloads. The permanent erased-tenant register blocks ID reuse, replay, restore, and import. Incomplete cleanup remains visible and resumable.
- During query backend failure, return a partial result only if at least one selected axis safely verifies tenant/case scope and the active configuration. Disclose unavailable, excluded, available-with-no-hits, and scope-verified truncated axes distinctly, with freshness impact and recovery guidance. Fail clearly if no selected axis can answer safely. A query-time partial result never makes an incompletely projected unit `indexed`.
- Errors carry a stable code, understandable message, and safe recovery suggestion. Unknown, mismatched, deleting, or unauthorized tenant requests fail before work or evidence disclosure; responses never reveal another tenant's data. Readiness distinguishes control-plane prerequisites from a degraded search capability.

## Technical Decisions

- The tenant lifecycle workflow is the sole writer of lifecycle state and tenant resources: RediSearch/vector indexes, a separate FalkorDB database, tenant-scoped Redis principal and credentials, tenant content storage, grants, and access-telemetry partition. Dapr workflows use bounded, idempotent activities, verification, compensation, and resumable cleanup. Domain lifecycle transitions are accepted through Hexalith.EventStore; backend records are rebuildable projections.
- Ordinary admission requires the authoritative committed lifecycle state to be `Active`. Lifecycle and erasure work use only explicitly authorized, single-tenant operator exemptions; `Deleting` advances a lifecycle write generation and fences concurrent writers. The platform-scoped erased-tenant register is the sole permanent retirement authority and fails closed when its lineage cannot be verified.
- Derive external tenant and caller authority from validated bearer identity, not request fields. Internal calls additionally require protected Dapr channels, deny-by-default workload authorization, an operator-owned app allowlist, an explicit tenant grant, and current `Active` status. Revalidate captured authority when durable work resumes. Tenant IDs and composed resource keys follow the issued identifier grammar; imported identifiers are validated before use.
- Route Redis operations through tenant-scoped backend authority and FalkorDB operations to the tenant's dedicated database. Build parameterized Cypher through the graph-query boundary. Case scope partitions graph results but is not a separate authorization boundary. Product services obtain application secrets through Dapr Secrets backed by OpenBao.
- The server chooses safe search axes once from explicit adapter reports of scope, active generation/epoch, and truncation. It publishes the resulting closed axis states in the shared Evidence Packet; ranking weights use only axes that returned hits. Ingestion retries and projection completion remain governed by their durable, all-axis current-revision contract.

## UX & Interaction Patterns

- Operator commands show tenant scope, lifecycle progress, verification evidence, backend impact, and the next safe action. Destructive and reindexing actions name the tenant and consequence, with a safe cancellation path. An erasure receipt claims completion only when verification is recorded.
- CLI, MCP, and later web presentations share the same scope, axis, degradation, and recovery meanings. Use precise text for unauthorized scope, partial service, empty results, and unavailable backends. Authorization failures contain no restricted evidence; status must not rely on color alone.

## Cross-Story Dependencies

- Epic 0 consumes tenant provisioning before ingestion, indexing, or search; lifecycle work here extends or verifies that owner rather than creating another provisioning path. Context enforcement and active-tenant checks underlie every subsequent tenant operation.
- Deletion depends on lifecycle ownership and the platform erasure register. Isolation verification must be repeated after the tenant-scoped backend-principal hardening in Epic 24. Search consumers use the shared degradation decision and Evidence Packet rather than defining their own backend-health policy.
- `Hexalith.McpCli` is the target Hexalith-owned CLI/MCP presentation; existing module-specific surfaces remain migration sources until isolation and behavior gates pass.
