# Story 0.2: Minimal Case Bootstrap

Status: done

## Story

As a developer,
I want to create and list a minimal case inside an active tenant before ingestion begins,
So that every memory unit has a valid single-case owner from its first write.

## Acceptance Criteria

1. Given an active tenant, when I create a minimal case with an ID and display name, then the case is persisted in the tenant scope and can be listed.
2. Given that case, when it is created, then the tenant's graph database contains the case node required for later `contains` edges.
3. Given a missing, inactive, or cross-tenant case, when ingestion or search requests use that case, then validation fails with a structured error and recovery suggestion before backend mutation.

## Ownership Boundary

Story 0.2 is the minimum executable prerequisite proving an active case exists before Epic 1 data-writing work. It delivers only minimal case creation, listing, and the case-node-in-graph requirement.

Case status, activity history, member management, single-case ownership enforcement, case-scoped graph edges, cross-case search, deletion, and annotation work remain owned by Epic 3 and Story 5.4.

## Completion Evidence

- Planning source: `_bmad-output/planning-artifacts/epics.md`, Epic 0, Story 0.2.
- Deep implementation artifact: `_bmad-output/implementation-artifacts/3-1-create-and-list-cases.md`.
- Readiness validation: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-19.md` records Story 0.2 as covered and recently strengthened with an ownership boundary.
- Current sprint status marks this story `done`.

## Change Log

- 2026-05-20: Added reconciliation artifact for the completed Epic 0 prerequisite story.
