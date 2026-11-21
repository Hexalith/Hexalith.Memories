---
id: SPEC-34-33-v1-wire-name-register
story: '34.33'
companions:
  - acceptance-and-evidence.md
  - ../../planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md
sources: []
---

# Story 34.33 — Reserve V1 wire names in one build guard

**Story owner** Administrator · **Ledger owner** Jérôme Piquot · **Decisions** AD-12 and AD-22 · **Traceability** FR19, FR44 and G6

## Why

Story 34.1 must add an optional retry token to the dedicated URL ingestion request. AD-12 requires same-change reservation of new V1 fields. The generic ingestion input already has a token, but the dedicated request has none, and Contracts lacks the register and build guard. The approved execution order therefore starts with 34.33. Reliable AD-3 projection repair depends downstream on 34.1 establishing durable acceptance before scheduling. [Dependency evidence and affected components](acceptance-and-evidence.md#selection-and-affected-components)

## Capabilities

- **CAP-1**
  - **Intent** Contract maintainers can extend V1 while protecting the wire contract that existing consumers read.
  - **Success** The required build check accepts the registered baseline, identifies each unregistered or incompatible change, and accepts an approved addition once it and its reservation land together.

## Constraints

- Keep one versioned register in `src/Hexalith.Memories.Contracts/`. Its maintainers approve reservations. Record each evidence-bearing field’s case-sensitive wire name, owning object, JSON shape, nullable/absent behavior and property order, together with the values of each closed vocabulary.
- Preserve prior V1 reservations. A known difference between the shipped representation and an adopted target must identify the individual difference and its owning work. A successful guard run establishes only the compatibility covered by this story.
- The successor sprint tracker is frozen. Story 34.33 remains an approved backlog item pending implementation and evidence. The Phase 1 release decision remains **no-go**. [November release decision](../../planning-artifacts/prd.md#release-decision-record)

## Non-goals

Story 34.1 owns the URL token and EventStore-first intake. AD-3 write fencing, acknowledgements and repair, runtime tenant isolation, and release qualification remain separate work. Story 34.34 owns explicit-null emission, Story 33.8 owns canonical packet bytes, and Story 34.40 owns the wider telemetry and clock contract inventory.

## Success signal

A reviewer can reproduce a passing baseline, make the normal build-validation lane fail on one incompatible field, and demonstrate a compatible additive change using the same guard. The architecture owner can then evaluate the evidence for the AD-12/AD-22 register row. [Acceptance criteria and closure checks](acceptance-and-evidence.md#closure-checks)
