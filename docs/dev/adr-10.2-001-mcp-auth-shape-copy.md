# ADR 10.2-001: MCP Auth Shape Copy

## Status

Accepted.

## Decision

`Hexalith.Memories.Mcp` copies the JWT bearer authentication shape from the EventStore submodule instead of adding a project reference to EventStore infrastructure classes.

## Rationale

Memories and EventStore are separate products. Sharing auth implementation by reference would couple release cadence, configuration names, logging templates, and public infrastructure types across that boundary. Copying the shape keeps the important invariants aligned while allowing MCP-specific challenge text and tenant-claim handling.

The invariants to keep aligned are strict JWT validation, sanitized 401 `ProblemDetails`, RFC 6750 `WWW-Authenticate` headers, no token logging, OIDC-or-symmetric-key configuration, and startup validation. Allowed divergence includes logger event wording, realm names, tenant claim normalization, and MCP tool-level authorization errors.
