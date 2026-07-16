# PII Acknowledgment — Story 9.2 Dual-Embedding Pipeline

**Status:** Draft awaiting countersignature
**Applies to:** Hexalith.Memories 9.2 and later
**Owner:** Memories product team
**Review cadence:** Annually, or on any material change to the DAPR Conversation component
configuration, whichever comes first.

---

## 1. Purpose

Story 9.2 introduces a dual-embedding pipeline for `SourceType.Event` memory units. For each event,
the server authors a single-sentence natural-language description of the payload via the DAPR
Conversation API and indexes a second embedding derived from that description. This document is the
code-adjacent record of the privacy posture that governs the new surface. A signed-off copy of this
artifact — or an equivalent recorded in the organization's governance system — MUST be on file
before 9.2 is deployed to any tenant whose event stream may contain Personally Identifiable
Information (PII), Protected Health Information (PHI), or other regulated data subjects.

## 2. Known behaviors

1. **NL description MAY contain PII.** The LLM summarizes event payloads; when a payload carries a
   customer name, account number, email, phone, or similar attribute, the resulting sentence MAY
   reproduce any of those attributes. The DAPR Conversation component ships with
   `piiScrubbing: false` as the MVP default. Test
   `GenerateNaturalLanguageDescriptionActivityTests.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior`
   documents this behavior in code.
2. **Within-tenant PII propagation.** The NL description is persisted in the tenant's own Redis
   Vector store (`{tenant}:vecnl:*`). It does NOT leak across tenants via the store itself — tenant
   isolation boundaries from Epic 5 apply unchanged.
3. **Cross-tenant response cache at the sidecar level.** DAPR's response cache is shared across
   tenants. `deploy/dapr/components/conversation-llm.yaml` ships with `responseCacheTTL: 0s` and the
   server refuses to start if a non-zero TTL is configured without an explicit opt-in acknowledgment
   (`NaturalLanguage:AcceptCrossTenantCacheSharing = true` or env var
   `HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING=1`). Event ID `9164` is logged Critical if the
   operator tries to bypass the gate. The `memories.conversation.cache.hits{tenant_id,cache_status}`
   metric schema is reserved for future cache-status emission, but the current Dapr.AI SDK surface
   does not expose a live hit/miss signal; today the real protections are `responseCacheTTL: 0s`
   by default plus the startup validator.
4. **Third-party LLM provider egress.** When the DAPR Conversation component is configured against
   a hosted LLM (e.g., `conversation.openai`, `conversation.anthropic`), every event payload
   egresses to that provider for summarization. The provider's data-retention, training-data, and
   sub-processor commitments become load-bearing for this deployment.

## 3. Operator controls

The following per-deployment controls gate PII exposure. The governance-signatory operator
acknowledges these controls exist and commits to the posture chosen per deployment.

| Control                                               | Location                                                                    | Default                   | Mechanism                                                                    |
| ----------------------------------------------------- | --------------------------------------------------------------------------- | ------------------------- | ---------------------------------------------------------------------------- |
| PII scrubbing by DAPR                                 | `deploy/dapr/components/conversation-llm.yaml` `metadata[piiScrubbing]`     | `false`                   | YAML edit → sidecar restart                                                  |
| Response cache TTL                                    | `deploy/dapr/components/conversation-llm.yaml` `metadata[responseCacheTTL]` | `0s`                      | YAML edit, gated by startup validator                                        |
| Cross-tenant cache acknowledgment                     | `NaturalLanguage:AcceptCrossTenantCacheSharing` or env var                  | `false` / unset           | Config edit; required when cache TTL > 0                                     |
| LLM provider swap                                     | `deploy/dapr/components/conversation-llm.yaml` `type`                       | `conversation.echo` (dev) | YAML edit + secrets; no code change                                          |
| Disable duplicate metadata copy of the NL description | `NaturalLanguage:PersistInMetadata`                                         | `false`                   | Config edit; does **not** remove the NL description from `{tenant}:vecnl:*` |
| Per-tenant LLM provider                               | _Not shipped in 9.2_                                                        | —                         | Phase 2 follow-up                                                            |

## 4. Operator checklist — before enabling NL on a PII-bearing tenant

- [ ] The chosen LLM provider's data-handling agreement covers the tenant's data-subject categories
      (e.g., GDPR DPA in place for EU subjects, HIPAA BAA for PHI). Provider agreement reference: **\_**
- [ ] Response cache is either disabled (`responseCacheTTL: 0s`) OR cross-tenant sharing has been
      acknowledged AND the tenant population is known to be data-isolation-compatible. Posture: **\_**
- [ ] `piiScrubbing: true` has been evaluated against the target LLM provider; if enabled, the
      regression in summary quality has been accepted. Posture: **\_**
- [ ] Operators have a documented procedure for responding to a correction request
      ("please remove this description / re-ingest with scrubbing"). Link to runbook: **\_**
- [ ] Retention policy for the NL semantic store matches the tenant's data-retention commitment
      (NL hashes are retained for the lifetime of the memory unit; deleting the memory unit deletes
      both raw + NL hashes). Confirmed: **\_**

## 5. Re-open triggers

Revisit this artifact when ANY of the following occurs:

- A new LLM provider is added to the deployment.
- `responseCacheTTL` is changed from zero to non-zero.
- `piiScrubbing` is toggled.
- Per-tenant LLM provider configuration (Phase 2) lands — the control surface expands.
- A new regulated data class is ingested by any tenant.
- A provider's data-handling agreement is renegotiated.

## 6. Signatures

The undersigned acknowledge the posture recorded above and authorize 9.2 deployment to the
PII-bearing tenants listed in the attached scope sheet.

| Role                             | Name | Signature | Date |
| -------------------------------- | ---- | --------- | ---- |
| Product Owner                    |      |           |      |
| Legal / Compliance               |      |           |      |
| Engineering (Memories lead)      |      |           |      |
| Security / Privacy (if separate) |      |           |      |

## 7. Artifact traceability

- Story: `_bmad-output/implementation-artifacts/9-2-dual-embedding-and-causal-chain-indexing.md`
- Review finding: Story 9.2 Adversarial Code Review — D10 (Improvement W)
- Companion documentation: `docs/dev/eventstore-integration.md` §"PII scrubbing posture" and
  §"LLM hallucination posture"
- Code-level acknowledgment: `GenerateNaturalLanguageDescriptionActivityTests.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior`
- Configuration gate: `NaturalLanguageDescriptionOptionsValidator` event `9164`
