---
id: SPEC-pg2-c1-authenticated-predecessor-interchange
companions:
  - schemas.md
  - authority-and-sessions.md
  - producer-bindings.md
  - implementation-tasks.md
  - failure-modes.md
  - brownfield.md
  - ../../planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md
  - ../../implementation-artifacts/epic-27-context.md
  - ../../../docs/operations/access-telemetry-c1-pg2-c1-15-contract.md
  - ../../implementation-artifacts/27-22-component-and-backend-identity.md
sources: []
---

# Authenticated PG2 C1 predecessor interchange prerequisite

Review state: **proposed; unresolved prerequisites; not implementation-ready**.
Authored 2026-10-07 against `14bb1c17`. This folder tracks the prerequisite
separately; it grants no successor registration, reviewer authority or gate credit.
This kernel and its companions are the proposed implementation contract, derived
from the append-only memory and cited source contracts.

## Why

The current predecessor validator can accept hash-correct artifacts without
reading their meaning and treats different reviewer labels as independent
approvals. PG2 C1.15 now supplies a neutral provenance capture, but no implemented
authenticated disposition contract connects it to the twenty-five-gate C1
predecessor. Operations and Security need a verifiable interchange that refuses
unsupported producers, ineligible sessions and unauthenticated decisions before
downstream qualification can use them.

## Capabilities

- **CAP-1**
  - **intent:** Validate each current-profile capture as attributable evidence for its literal gate.
  - **success:** The capture matches its registered schema, complete source/helper set, command ledger, observations and required cleanup; changed, unrelated or unsupported artifacts refuse.
- **CAP-2**
  - **intent:** Establish that an independent authorized reviewer decided on the exact retained capture.
  - **success:** Verified authority binds producer/reviewer identities, role, scope, decision, capture bytes and validity; labels, self-approval, expired or revoked authority refuse.
- **CAP-3**
  - **intent:** Establish one complete C1 predecessor eligible for its approved session.
  - **success:** Exactly twenty-five separately accepted gates and genuine Operations/Security bundle decisions bind one approved profile, workload, target and eligible session; every missing prerequisite refuses.
- **CAP-4**
  - **intent:** Prevent invalid interchange from reaching downstream qualification.
  - **success:** Negative fixtures refuse before downstream launch or target dependencies, publish no accepted artifact, and leave protected lifecycle and tracking states unchanged.

## Constraints

- Bind exact `PG-ONPREM-2` profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.
- Preserve Story 27.4 `in-progress` and incomplete, A41 and its sprint action open, and Production lifecycle writes disabled.
- Captures, fixtures, advertised capabilities, Git identities and reviewer labels supply no acceptance or authority. Preserve historical PG1 decisions and the C1.16 closed-window acceptance without enlarging their scope.
- Require fresh versioned evidence where facts are missing. Never synthesize provenance, re-label v1 packets, default missing authority policy, or treat a context name as authenticated cluster identity.
- Require approved implemented producer bindings and genuine independent receipts; missing bindings or unresolved authority/session decisions fail closed.
- C1.23, C1.24 and C1.25 stay individually attributable; aggregate approvals and post-evidence C5/C6 cannot substitute for them.
- This slice consumes evidence and authorization; it does not execute collectors or targets. Its dependency-injection test seam cannot authorize a deployed consumer.

## Non-goals

- Registering held successors, renewing other gate producers, provisioning an identity/receipt service, or selecting a trust root on an owner's behalf.
- Running live targets or C0/C2-C6, enabling qualification or Production, publishing close-out, or completing Story 27.4/A41.
- Extending product authentication into reviewer authority, changing deployment inputs, or claiming tamper-evident/compliant audit retention or node/site HA.

## Success signal

One offline verifier accepts a complete, independently authenticated fixture under
an isolated test trust policy, and rejects every specified negative before any
downstream dependency call. With today's missing bindings and authority policy,
real assembly remains refused. Fixture success creates no operational acceptance.

## Assumptions

- The new wire schemas, J1 encoding, parser budgets and proposed implementation paths are engineering proposals for review. Their approval does not appoint reviewers or adopt a receipt mechanism.
- Capabilities are predicates of one interchange verdict. Independently shippable authority-provider, producer-renewal and registration work remains outside this slice.

## Open Questions

- **P1:** Security must name the supported producer/reviewer identity mechanism, receipt issuer, trust roots, verification/retrieval/revocation adapter and accountable maintainer.
- **P2:** Security with Operations must approve numeric capture, review, approval, session and revocation-status windows, trusted time and key-rotation rules.
- **P3:** Operations must provide authenticated target/session authority and approved archive custody, retention, access and cleanup policy.
- **P4:** Security and the gate approvers must settle role authorization, delegation/quorum and the C1.23/C1.24/C1.25 evidence dependencies without approval cycles.
- **P5:** The Deployment Adapter Developer and gate owners must supply exact approved producer/schema/verifier registrations and done owner evidence for all twenty-five gates.
- **P6:** Architecture, Security and Operations must decide C1.16 closed-window eligibility or require fresh versioned evidence, preserving its existing acceptance and limitations.
- **P7:** The interchange and Story 27.4 owners must approve consumer migration and separately resolve the legacy runtime qualification-gate profile pin.

Ownership, consequences and objective reopen evidence are in
[implementation-tasks.md](implementation-tasks.md). No named assignee or approval
is inferred from a role label.
