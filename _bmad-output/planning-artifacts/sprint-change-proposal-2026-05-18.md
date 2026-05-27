---
status: approved
date: 2026-05-18
project: Hexalith.Memories
trigger_report: implementation-readiness-report-2026-05-18.md
change_scope: moderate
recommended_path: direct_adjustment
approved_by: JeromePiquot
approved_on: 2026-05-18
affected_artifacts:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-18.md
---

# Sprint Change Proposal: MVP Embedding Provider Scope Alignment

## 1. Issue Summary

The 2026-05-18 Implementation Readiness Assessment marked the artifact set as `NEEDS WORK`.

The critical blocker is a scope mismatch between the PRD and implementation stories:

- `prd.md` says all major embedding providers are supported from MVP: Google, OpenAI, and Mistral.
- `epics.md` Story 1.7 says MVP supports Google only and keeps OpenAI, Mistral, and custom providers as future extensions.
- `architecture.md` already records a PRD deviation: Google first in MVP; OpenAI/Mistral in Phase 1.5/2.

This makes FR68 appear covered at the traceability level while the detailed MVP acceptance criteria do not implement the full provider matrix promised by the PRD.

## 2. Checklist Findings

### Trigger and Context

- [x] Triggering artifact: `implementation-readiness-report-2026-05-18.md`.
- [x] Triggering story: Story 1.7, `Embedding Provider Configuration`.
- [x] Issue type: misunderstanding or drift in original requirements across artifacts.
- [x] Evidence: PRD provider table conflicts with Story 1.7 and the architecture deviation table.

### Epic Impact

- [x] Epic 1 can still be completed as originally planned if MVP is Google-only.
- [x] No new MVP epic is required.
- [x] No epic removal or resequencing is required.
- [x] Epic 13 remains the correct carrier for post-MVP provider pluggability and vector migration.
- [x] If the PRD keeps OpenAI/Mistral in MVP instead, Epic 1 would need new provider implementation stories before readiness can pass.

### Artifact Impact

- [x] PRD requires correction so MVP provider scope matches Story 1.7 and architecture.
- [x] Epics require a clarifying note that FR68 MVP coverage means configurable Google provider settings plus future-compatible provider shape, not OpenAI/Mistral runtime support.
- [x] Architecture already contains the correct position, but should be tightened to name Epic 13/Ollama and avoid leaving the PRD conflict implicit.
- [N/A] UX design requires no direct change. The provider mismatch affects operator/developer configuration behavior, not CLI/MCP trust-loop UX.
- [N/A] Sprint status requires no story additions, removals, renumbering, or status changes for the recommended path.

### Path Forward Evaluation

Option 1, Direct Adjustment: Viable.

Effort: Low to medium.

Risk: Low.

Rationale: The architecture already decided Google-first MVP. Story 1.7 is scoped to Google-only provider configuration with an extensible provider shape. Aligning the PRD and traceability language removes the blocker without expanding implementation.

Option 2, Potential Rollback: Not viable.

Effort: Medium.

Risk: Medium.

Rationale: No completed implementation needs rollback. This is a planning artifact inconsistency.

Option 3, PRD MVP Review: Viable but heavier than needed.

Effort: Medium to high if OpenAI and Mistral stay in MVP.

Risk: High.

Rationale: Keeping OpenAI and Mistral in MVP would add provider clients, secrets, model/dimension validation, rate-limit handling, tests, and operator documentation. That broadens the solo-developer MVP and conflicts with the architecture's explicit scope control.

Selected approach: Direct Adjustment.

## 3. Recommended Approach

Update the planning truth source so MVP provider support is:

1. Google `text-embedding-004` only for runtime embedding generation in MVP.
2. Tenant-level configuration still includes provider, model, dimensions, secret name, and rate-limit fields.
3. Provider shape remains extensible to OpenAI, Mistral, custom, and later Ollama without refactoring.
4. OpenAI and Mistral are deferred to a post-MVP provider expansion track unless a later sprint change explicitly pulls them forward.
5. Epic 13 remains the accepted post-MVP/provider-migration path, including the Ollama implementation already documented.

This resolves the readiness blocker while preserving the architecture decision and Story 1.7 acceptance criteria.

## 4. Detailed Change Proposals

### PRD: Embedding Provider Configuration

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: `### Embedding Provider Configuration`

OLD:

```text
All major providers supported from MVP. Configuration is per-tenant — different tenants can use different providers/models.

**Supported Providers (MVP):**

| Provider | Model (default) | Dimensions | Rate Limit (default) |
|---|---|---|---|
| Google | `text-embedding-004` | 768 | 1500 req/min |
| OpenAI | `text-embedding-3-small` | 1536 | 3000 req/min |
| Mistral | `mistral-embed` | 1024 | Varies |
```

NEW:

```text
MVP supports Google embedding generation at runtime. Configuration is per-tenant and deliberately shaped for provider expansion — different tenants can carry provider/model/rate-limit configuration, but non-Google runtime providers are post-MVP unless a later sprint change explicitly pulls them forward.

**Supported Providers (MVP):**

| Provider | Model (default) | Dimensions | Rate Limit (default) |
|---|---|---|---|
| Google | `text-embedding-004` | 768 | 1500 req/min |

**Post-MVP provider expansion candidates:**

| Provider | Model (default) | Dimensions | Notes |
|---|---|---|---|
| OpenAI | `text-embedding-3-small` | 1536 | Deferred provider implementation |
| Mistral | `mistral-embed` | 1024 | Deferred provider implementation |
| Ollama | `qwen3-embedding:4b` | 2560 | Covered by Epic 13 provider migration work |
```

Rationale: Makes the PRD match the architecture and Story 1.7 without losing the long-term provider roadmap.

### PRD: Tenant Configuration Table

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: `**Configuration per tenant:**`

OLD:

```text
| `provider` | google / openai / mistral / custom | Tenant config |
```

NEW:

```text
| `provider` | MVP: google. Post-MVP: openai / mistral / ollama / custom via provider expansion stories | Tenant config |
```

Rationale: Prevents FR68 from implying OpenAI/Mistral are already MVP runtime paths.

### Epics: FR68 Coverage Clarification

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### FR Coverage Map`

OLD:

```text
- FR68: Epic 1 — Configure embedding provider
```

NEW:

```text
- FR68: Epic 1 — Configure Google embedding provider for MVP with an extensible provider/model/dimensions/rate-limit shape. OpenAI, Mistral, Ollama, and custom runtime providers are post-MVP provider expansion work unless explicitly pulled forward by sprint change.
```

Rationale: Keeps traceability honest at the implementation-depth level.

### Epics: Story 1.7 Clarification

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### Story 1.7: Embedding Provider Configuration`

OLD:

```text
So that different tenants can use different embedding providers and the system is ready for multi-provider support.
```

NEW:

```text
So that MVP tenants can configure Google embedding settings consistently and the system is ready for multi-provider support in later provider expansion stories.
```

Rationale: The existing acceptance criteria already say MVP supports Google only. This keeps the story goal from sounding broader than the acceptance criteria.

### Architecture: PRD Deviation Tightening

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `### PRD Deviations`

OLD:

```text
| "All major [embedding] providers supported from MVP" | Google first in MVP. OpenAI/Mistral in Phase 1.5/2. | Solo developer scope. IEmbeddingProvider abstraction makes additions trivial. |
```

NEW:

```text
| "All major [embedding] providers supported from MVP" | Google runtime embedding provider only in MVP. OpenAI/Mistral are post-MVP provider expansion candidates; Ollama is covered by Epic 13 provider migration work. | Solo developer scope. The provider configuration shape and embedding provider pattern preserve extensibility without making every provider an MVP blocker. |
```

Rationale: The architecture already has the correct decision. This edit makes it the explicit cross-artifact rule.

### Readiness Report Follow-Up

Artifact: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-18.md`

Change after approval:

- Add a note near `Overall Readiness Status` linking this sprint change proposal.
- Mark the critical provider-scope blocker as resolved once the PRD/epics/architecture edits above are applied.
- Preserve the remaining major/minor/warning guardrails as implementation controls.

Rationale: Keeps the readiness report audit trail coherent without rewriting the original assessment.

## 5. Implementation Handoff

Scope classification: Moderate planning correction.

Recommended handoff:

- Product/PM responsibility: approve that Google-only is the MVP truth source.
- Architect responsibility: confirm the architecture deviation remains the governing provider-scope decision.
- Developer responsibility: apply the documentation edits above after approval; do not add OpenAI/Mistral implementation stories for MVP.
- PO/Story governance responsibility: ensure future provider work lands in Epic 13 or a later approved provider-expansion story, not by silently widening Story 1.7.

No code implementation is required for this correction.

No `sprint-status.yaml` update is required because the recommended path changes artifact wording only and does not add, remove, renumber, or status-change stories.

## 6. Success Criteria

The correction is complete when:

1. `prd.md` no longer claims OpenAI and Mistral runtime providers are MVP-supported.
2. `epics.md` FR68 and Story 1.7 clearly define MVP as Google-only runtime support with an extensible provider shape.
3. `architecture.md` names the provider phase boundary explicitly.
4. The readiness report or follow-up note references this proposal as the resolution for the critical provider mismatch.
5. A follow-up readiness check no longer reports the provider mismatch as critical.

## 7. Approval Request

Recommended decision: approve this proposal and apply the direct documentation correction.

Approval statement to record:

```text
Approved: MVP runtime embedding provider support is Google-only. OpenAI, Mistral, Ollama, and custom providers are post-MVP provider expansion work unless explicitly pulled forward by a later sprint change.
```
