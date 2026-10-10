---
title: 'PG2 C1 I2 operational registration handoff'
type: 'chore'
created: '2026-10-10'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md'
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Closed I2 registration and local Git provenance inspection is implemented, but no approved PG2 C1.15 successor registration or authenticated live evidence permits a deployed binding. Story 27.4 needs a concrete Operations/Security handoff before the next C1 prerequisite can progress.

**Approach:** Record the exact I2 proof boundary and a scoped request for registration, target and namespace identity, new custody, credential-file paths, authority, and reviewers. Treat proposed filesystem paths as choices requiring Operations/Security approval. Keep deployed C1 lookup refusing, A41 open, Story 27.4 in progress, and Production lifecycle writes disabled. Do not run the prior offline recheck, access a live target, or claim acceptance.

</frozen-after-approval>

## Implementation Notes

The closed I2 inspector and deployed refusal already exist, so this slice changes no verifier or producer code. Added `pg2-c1-i2-operations-security-request.md` and linked it from `implementation-tasks.md`. The request gives concrete **candidate** external custody and credential-file paths, identifies the local client context and namespace without promoting them to authenticated identity, and asks for exact registration/source receipts, target/session grants, fault/purge scope and named independent reviewers. Jérôme Piquot's recorded bundle-only exception is preserved; the C1.15 gate reviewer remains unassigned until Security grants a different principal from the actual producer. No operational acceptance or live call is implied.

The independent blind review found five concrete omissions. All were corrected in the request: tenant/registry/policy scope, the thirteen-field and nineteen-source registration inventory, stable fault/purge principal IDs, a distinct GitHub PR author, and custody backing/immutability details. No finding was deferred. Only request and task-ledger documentation changed; the prior Story 27.4 offline recheck was not repeated.

## Review Triage Log

| Finding | Verdict and evidence |
| --- | --- |
| Missing tenant, registry and policy scope in session grant | medium, patched: P3 requires each; the request now asks for exact identities/revisions and authenticated Refs. |
| Incomplete reviewable registration inventory | medium, patched: the request now lists all thirteen fields, three producer/helper paths and sixteen exact input paths, and marks the unimplemented/unauthorized owner and verifier values missing. |
| Fault and purge owners only described by role | medium, patched: Operations must return stable principal IDs and separate grants for each fault/lifecycle-purge/adapter-reclamation action. |
| GitHub PR author not separated from reviewer | medium, patched: the request requires an authorized distinct PR author and bound PR, commit and review IDs. |
| Custody path lacks backing/immutability | medium, patched: Operations must identify external mount/service, owner, volume ID, receipt origin, immutable creation and access enforcement. |

