# Sprint Change Proposal: Story 2.7 Evidence Packet Web Metadata Closure

Date: 2026-07-05
Project: memories
Owner: Jerome
Workflow: bmad-correct-course
Mode: Batch

## 1. Issue Summary

Story 2.7 is marked `done`, but Epic 17 action items still record a residual contract gap: web lenses can only render unavailable boundaries for chronological timestamps, benchmark NDCG@10 evidence, ingestion stage taxonomy, MCP schema/tool metadata, and freshness/last-checked metadata.

Evidence:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` has an open Epic 17 action item to drive Story 2.7 to completion for those fields.
- `LensFieldTraceability` records those fields as `NoContractSource` and defers them to Story 2.7.
- Web lens mappers hard-code unavailable states for benchmark NDCG@10, ingestion stages, timestamps, last-checked, and MCP tool name.
- `EvidencePacketViewMapping` and `EvidenceDisplay` still use a freshness unavailable sentinel.

## 2. Impact Analysis

Epic impact: Epic 2 remains semantically complete, but Story 2.7 needs a residual additive contract mapping pass. Epic 17 can then consume canonical packet fields instead of web-only placeholders.

Story impact:

- Story 2.7: add optional metadata to canonical `Contracts.V1` Evidence Packet records and keep the story done.
- Story 17.1/17.4: consume the new metadata in cockpit and lens projections.
- No backlog reordering or MVP scope reduction is required.

Artifact conflicts:

- PRD/Architecture/UX remain aligned with the Evidence Packet as the cross-surface trust envelope.
- The conflict is implementation-level only: the current contract is narrower than the web inspection stories expected.

Technical impact:

- Add optional JSON fields only; do not rename or remove existing packet fields.
- Preserve restrictive-scope behavior: unauthorized/unknown scope still suppresses sensitive details.
- Existing CLI/MCP/search producers may omit the metadata; web renders values only when present.

## 3. Recommended Approach

Direct Adjustment.

Rationale: the missing data is a contract completeness issue, not a product pivot. Additive `Contracts.V1` metadata records let current producers stay compatible while web consumers can render approved packet-provided values. Risk is low to medium because public JSON shape changes, but the changes are optional and covered by serialization and web mapper tests.

Effort: Low to medium.
Timeline impact: current sprint only.
Rollback: remove optional metadata consumption and leave prior unavailable boundaries.

## 4. Detailed Change Proposals

### Story 2.7

OLD:

- Evidence Packet exposes scope, result, sources, evidence, graph, state, omitted details, and recovery.
- Web-only unavailable boundaries remain for timestamps, benchmark evidence, ingestion taxonomy, MCP schema/tool name, and freshness.

NEW:

- Evidence Packet exposes optional metadata for:
  - packet/source freshness and last-checked timestamps
  - source activity timestamps
  - source ingestion stage taxonomy
  - benchmark NDCG@10, threshold, per-query evidence, run metadata, and evidence link
  - MCP tool/schema metadata
- Web surfaces render those values when present and keep fail-closed unavailable/unauthorized behavior when absent or restricted.

Rationale: closes the Epic 17 action item without inventing web-local DTOs.

### Web Lens Traceability

OLD:

- Trace rows for `activity.timestamp`, `ingestion.stage`, `health.lastChecked`, `benchmark.ndcg`, `benchmark.threshold`, `benchmark.perQuery`, `benchmark.evidenceLink`, and `packet.toolName` point to `NoContractSource`.

NEW:

- Those rows point to canonical Evidence Packet metadata fields.
- `health.queueBacklog` remains out of scope because it requires live probes, not packet metadata.

Rationale: keeps every rendered field tied to a named contract source.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent.

Implementation tasks:

- Add optional Evidence Packet metadata records and JSON source-generation registrations.
- Update web cockpit/lens mappers and components to render metadata when present.
- Update fixtures and focused tests for contract serialization, traceability, and web rendering.
- Mark the Epic 17 Story 2.7 action item done after verification.

Success criteria:

- Existing Evidence Packet JSON remains source-compatible.
- Web tests prove metadata renders when present and stays suppressed under restrictive scope.
- Contract serialization tests cover the new metadata types.
- Focused contract and web test suites pass.
