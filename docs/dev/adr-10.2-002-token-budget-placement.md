# ADR 10.2-002: Token Budget Placement

## Status

Accepted.

## Decision

Token budget is a request DTO/query parameter, not an HTTP header.

## Rationale

The budget is part of the semantic request made by an LLM tool caller. Keeping it beside `maxResults`, `axes`, `caseId`, and traversal depth makes replay, logging, tests, and generated MCP schemas straightforward. A header would hide a behavior-changing parameter from the tool contract.

Reconsider this if a future Phase 2 tool has no request DTO or if a cross-cutting gateway needs to enforce a single budget across multiple downstream calls.
