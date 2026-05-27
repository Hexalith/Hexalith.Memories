# Story 0.3: Tenant and Case Validation Guard

Status: done

## Story

As a developer,
I want one shared validation guard for tenant and case scope,
So that ingestion, indexing, search, graph traversal, CLI, and later MCP behavior enforce the same isolation rule.

## Acceptance Criteria

1. Given a request with tenant and case scope, when validation runs, then it checks tenant existence, tenant active state, case existence, case active state, and tenant-case ownership before backend mutation.
2. Given invalid scope, when validation fails, then the caller receives a structured error with a recovery suggestion and no Redis, vector, or graph write occurs.
3. Given future ingestion, indexing, search, CLI, MCP, or graph paths need scope validation, when they call the shared guard, then behavior and error codes remain consistent.

## Ownership Boundary

Story 0.3 establishes the shared minimum validation contract before Epic 1 data-writing work. Story 5.4 remains responsible for deeper tenant context enforcement and broader tenant isolation verification.

## Completion Evidence

- Planning source: `_bmad-output/planning-artifacts/epics.md`, Epic 0, Story 0.3.
- Readiness validation: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-19.md` records Story 0.3 as covered and scope-bounded.
- Current sprint status marks this story `done`.

## Change Log

- 2026-05-20: Added reconciliation artifact for the completed Epic 0 prerequisite story.
