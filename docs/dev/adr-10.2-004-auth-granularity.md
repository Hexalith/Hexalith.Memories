# ADR 10.2-004: MCP Auth Granularity

## Status

Accepted.

## Decision

The Story 10.2 MVP authorization model is authenticated caller plus matching tenant claim. It is not per-tool scope authorization.

## Rationale

The first security boundary needed for MCP is tenant isolation. A caller with a valid token for `tenant-a` can use all four MCP tools for `tenant-a`; calls for any other tenant return `TENANT_FORBIDDEN`. Per-tool scopes such as `memories:read` or `memories:ingest` are deferred to Phase 2.

Promote per-tool scopes when the first tenant asks for read-only LLM-agent access or separate ingestion delegation.
