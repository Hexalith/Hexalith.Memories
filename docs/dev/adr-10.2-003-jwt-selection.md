# ADR 10.2-003: JWT Selection

## Status

Accepted.

## Decision

The MCP ingress uses JWT bearer authentication for Story 10.2.

## Rationale

JWT bearer validation is stateless, works with the MCP Streamable HTTP transport, fits LLM-hosted clients that cannot reliably hold client certificates, and matches the existing EventStore authentication pattern. mTLS was rejected for client-distribution complexity. API keys were rejected because they do not carry tenant claims cleanly. PASETO and HMAC-signed URLs were rejected because they would add non-standard operational machinery for this ecosystem.

Revisit if the client population can hold certificates cleanly or if an MCP authorization profile becomes mandatory for deployed clients.
