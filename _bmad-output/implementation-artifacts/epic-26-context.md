# Epic 26 Context: Test, Deployment & Operational Readiness

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make Hexalith.Memories production-operable from the repository: operators can build and deploy the complete DAPR-based topology with secure, non-placeholder configuration; back up and faithfully restore tenant or case data; trust that failure-mode and retrieval-quality regressions are caught by automated gates; and follow a complete, cross-linked runbook set during routine lifecycle work and incidents.

## Stories

- Story 26.1: Production Deployment Artifacts
- Story 26.2: Backup & Restore
- Story 26.3: Integration Stub Closure
- Story 26.4: Coverage Gating & Benchmark Lane
- Story 26.5: Operational Runbook Set

## Requirements & Constraints

- Production artifacts must build deployable container images and define the complete runtime topology, including the Memories services, DAPR sidecars, Redis Stack, FalkorDB, state store, pub/sub, secrets, service invocation, health probes, and infrastructure-managed ingress. Deployment configuration must include resource limits and usable component values; fake LLM providers, empty passwords, and committed production secrets are forbidden.
- A deployed service must become ready within 60 seconds after containers are running, excluding image-pull time. Readiness and liveness checks must reflect backend availability rather than only process health.
- Sensitive provider keys and service credentials must come from deployed secret management. External access is authenticated at ingress, internal service communication uses DAPR authentication, and production configuration must not weaken tenant isolation.
- Backup and restore must preserve the portable case-or-tenant representation across every memory unit, metadata record, Redis hash, and FalkorDB edge. Restore fidelity must be proved end to end, and Redis durability must retain zero memory units across restart with AOF enabled and verified.
- Failure-mode integration tests must exercise real retry, rate-limit, degradation, persistence, and consistency outcomes. Tests must assert the final state in the backing stores; a scenario may be skipped only with an explicit reason and must never pass through an assertion-free stub.
- CI must collect coverage and enforce a threshold without excluding composition-root behavior merely to improve the number. The established NDCG@10 benchmark suite must run in a scheduled lane and remain reproducible for the same dataset.
- Operator documentation must cover capacity planning, incident response, backup/restore and disaster recovery, index rebuild, tenant onboarding/offboarding, upgrades and migrations, and monitoring/alert thresholds. Procedures must include prerequisites, verification, rollback or recovery actions, and links between deployment and failure-recovery guidance.
- This epic implements the backup/restore and disaster-recovery slice of data portability. It does not expand scope to a broader application-facing export feature.

## Technical Decisions

- Use .NET SDK container publishing; do not introduce Dockerfiles. Production orchestration is derived from the Aspire AppHost model and delivered as a container-orchestrator manifest/overlay or Helm deployment rather than as a standalone application-host deployment.
- Keep external routing in infrastructure ingress. The Memories REST surface serves CLI and third-party traffic, MCP and server-to-server traffic use DAPR service invocation, and EventStore intake uses DAPR pub/sub with CloudEvents.
- Redis is both a retrieval backend and the durable state store for workflows and actors; its deployed state-store component must be actor-enabled and persistent. Workflow history must survive sidecar or process restart so in-progress operations resume.
- Deployed secrets flow through the DAPR Secrets API. Health, structured logs, traces, and metrics use the shared Aspire ServiceDefaults/OpenTelemetry conventions; operational thresholds should cover ingestion throughput, per-axis search latency, tenant index size, and pipeline queue depth.
- Integration verification uses Aspire distributed-application testing or DAPR-capable test containers. Unit and contract suites remain Docker-free, while the scheduled integration/benchmark lane may require the full topology.
- Deployment sizing starts from the documented service topology and must be refined into an operator-facing model, especially predictable Redis memory per memory unit by vector dimension and metadata size.

## Cross-Story Dependencies

- Production deployment artifacts establish the topology, configuration, probes, persistence, and resource model that the backup/restore procedure and operational runbooks must reference.
- Backup/restore supplies the executable recovery path and fidelity evidence required by the disaster-recovery runbook; the operational runbook story owns the final cross-linking and incident-facing presentation.
- Integration-stub closure provides trustworthy failure-path tests before coverage gating can be treated as meaningful. Coverage and nightly benchmark automation then become the continuing regression gate for the deployment and recovery work.
