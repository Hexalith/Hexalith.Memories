---
title: 'Close ten deferred Evidence Packet mapping findings'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the Evidence Packet describe the authorized response accurately and keep caller-provided text within the packet's display boundary.

- Given an unrecognized upstream omission reason or a null results collection, when a search surface builds the packet, then it fails with a stable, sanitized error before serializing the result (DW-429, DW-445).
- Given an expandable result, when the mapper builds a handle, then it binds the original query, tenant, case and detail group through versioned length-prefixed hashing with at least 128 bits. Existing packets with v1 handles remain readable (DW-430).
- Given case-scoped results, when a source explicitly names a different case, then both search mappers fail before publishing results. A whitespace-only requested case maps to tenant scope (DW-431, DW-443).
- Given combined degradation and token-budget omission, when a packet recommends recovery, then the backend guidance stays first and the budget guidance is added (DW-438).
- Given query display text, when any packet is mapped, then sensitive text is suppressed and the remaining display text is bounded. Given a forbidden MCP request, the prose and message omit the requested tenant identifier while the structured scope retains its validated request context (DW-442, DW-447).
- Given a healthy result with positive total matches and an empty returned page, when the mapper creates the packet, then it describes a partial result and keeps both counts. The single-axis packet leaves the hybrid-only all-axes signal absent (DW-446, DW-509).

</frozen-after-approval>

## Implementation Notes

- Baseline `febfa8bdd3c1a91403aa3906b946a0fde221d861` has 299 open entries. Story 2.7 and `docs/dev/evidence-packet.md` define the shared contract and preserve the underlying retrieval responses. `EvidencePacketMapper` serves CLI/MCP search, and their executors already catch mapping exceptions before writing results. `McpErrorMapper` owns the external authorization envelope.
- The scoped edits cover the shared mapper, the MCP mapper, focused Contracts/CLI/MCP tests, the packet contract guide, this spec and the ledger. The wire types and retrieval requests remain compatible. Handle consumers read values as opaque strings.

- The mapper bounds display text to 1,024 UTF-16 code units and reuses the packet's sensitive-text filter. Expansion hashes bind the full original query. V1 values continue to round-trip through JSON. The compatibility retrieval payload still carries its original query field.
- Explicitly inconsistent source cases, missing collections, negative match counts and unknown omission reasons fail mapping. The existing CLI and MCP executors return an error response without the result payload. Missing source case metadata stays supported because upstream case attribution is optional.
- Debug builds with scratch outputs passed for all four projects. Direct xUnit execution passed all 613 Contracts, 460 CLI and 122 MCP tests, plus 9 Server Evidence Packet mapping tests. The isolated build placed project outputs under the job's `TMPDIR` and exposed the workspace solution marker, source and docs there for the CLI's repository-document checks.
- `run-with-temp aspire start --non-interactive --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` started the pre-change AppHost. `aspire describe --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` reported OpenBao exited with code 1 and Keycloak unhealthy. The AppHost was stopped. Full-topology verification remains unestablished.
- `git diff --check` passed and `bmad-loop sweep --dry-run` reported 289 open records after exactly ten closures.

## Review Triage Log

- Medium, patched. Explicit combined or backend omission now contributes to the degraded state even when the separate flag is clear. Both mappers have a combined-omission regression.
- Medium, patched. Partial packet recovery now suggests changing the result window or refining the query. Invalid indexed rows can consume positions in a syntactic page.
- Medium, patched. Human and table CLI output now explain that matches exist when the mapped result is a partial empty page. The existing empty-result guidance remains available for an empty search.
- Medium, patched. Display filtering covers API key and authorization assignments and whitespace before a password value. Representative credential strings are suppressed in Contracts tests.
- Low, patched for the changed path. CLI and MCP end-to-end entry-point tests exercise null results and mismatched source cases. Missing required JSON members and unknown enum tokens are handled by the existing REST decoder before the mapper runs.
- Low, covered. The independent golden digest pins the length-prefixed framing and 128-bit output. A second test verifies that identical display text does not collapse distinct full queries into the same handle.
- Low, patched. The result summary XML documentation now describes bounded display text and suppression.
- Medium, patched. Negative source totals fail with a fixed diagnostic instead of becoming partial.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-evidence-packet-mapping.md`
- `docs/dev/evidence-packet.md`
- `src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs`
- `src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs`
- `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs`
- `src/Hexalith.Memories.Mcp/McpErrorMapper.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketCanonicalParityTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketFixtures.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketIsolationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketMapperTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketSerializationTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpErrorMapperTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs`
