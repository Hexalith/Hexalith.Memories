---
title: 'Record Story 27.4 prerequisite decisions'
type: 'chore'
created: '2026-10-10'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The owner has chosen a pragmatic path for Story 27.4 prerequisites, but the decision packet and handoff still present several of those choices as undecided. That obscures what Platform must return and what remains blocked.

**Approach:** Record the owner's selected route in the existing P1–P7 decision and request artifacts: use Platform's P1 v1 contract if it can be operated, reuse only authenticated target/custody resources, assign genuinely independent reviewers, audit C1.16 original facts once then recapture if incomplete, and approve strict versioned predecessor dispatch. Preserve every `NO_*` denial, live gate, Production and A41 hold until independently verifiable operational evidence exists.

</frozen-after-approval>

## Implementation Notes

The owner selected a bounded documentation handoff. Operational enrollment, reviewer appointment, target contact, issue publication, and P7 code migration remain separate evidence-bearing work. Existing working-tree edits are preserved.

Updated the P1–P4 decision packet, the I2 Operations/Security request, the interchange implementation handoff, and the Story 27.4 implementation notes. The decision fixes the completion route without changing the current deny states or any executable consumer. The existing public issue was not edited.

## Review Triage Log

- `medium`, patched: The existing Story 27.4 predecessor wording could be read as blocking C1 capture before C1 acceptance. The delivery route now states that C1 capture uses its own grant and session; C0/C2–C4 require accepted C1.
- `medium`, patched: The existing checklist obscured A41 execution order. The delivery route now spells out terminal validation, preflight, mutation, postflight, publication and remote verification.
- `medium`, patched: A 15-minute authorization verdict could obscure P2's shorter status limit. P7 now requires independent nonce-bound status within the 300-second whole-chain limit and revalidation before use.
- `medium`, patched: C1.16 provenance completeness alone could overlook expiry, revocation or the 96-hour age limit. Each now triggers fresh authorized capture.
- `low`, patched: The frozen 27.4 summary lists the profile digest but not the workload digest. An implementation note now states the required workload digest without changing the frozen intent.
- `medium`, patched: The conditional P1 v1 route needed an explicit Platform feasibility result. The I2 required response now requests that disposition and an alternative proposal if it cannot be operated.
