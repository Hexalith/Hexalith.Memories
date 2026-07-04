# Hexalith.Memories.Mcp

MCP Server surface for Hexalith.Memories — exposes memory operations as typed Model Context Protocol
tools so LLM agents (Claude Desktop, custom MCP clients, etc.) can search, ingest, traverse, and query
case information programmatically.

The `/mcp` endpoint requires JWT bearer authentication. Configure `Authentication:JwtBearer`
with OIDC `Authority` metadata in production; development and test hosts may use a symmetric
`SigningKey`. Production startup validation rejects `SigningKey` and requires HTTPS metadata
discovery for authority mode. Each tool also checks its `tenantId` argument against the token's
normalized tenant claims before calling the Memories Server.

The server runs as its own ASP.NET Core / DAPR-sided service (app-id `memories-mcp`) and reaches the
Memories Server exclusively via DAPR service invocation — no direct Redis, FalkorDB, or secret-store
access.

See `docs/dev/mcp-server.md` in the [Hexalith.Memories repository](https://github.com/Hexalith/Hexalith.Memories)
for the four registered tools, their parameter schemas, the DAPR-sidecar local-dev workflow, and the
operator rollback procedure.
