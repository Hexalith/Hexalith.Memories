---
title: 'PG2 C1 I3 offline authority boundary'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** I3 has no approved operational trust roots, receipt verification protocol, numerical time policy, authenticated target/session grant, or complete gate role policy. Structural dispositions and offline GitHub observations cannot confer C1 acceptance.

**Approach:** Add an offline terminal authority-consumption boundary that checks exact asserted capture and scope linkage, then refuses every purported acceptance until P1–P4 have approved provider inputs. Add refusal tests proving labels, hashes, fixture observations, and unavailable policy cannot trigger an adapter or target. Preserve deployed accepting entries absent, Story 27.4 RECHECK_ONLY, A41 open, and Production lifecycle writes disabled. Do not contact live targets, stage, commit, or publish.

</frozen-after-approval>

## Implementation Notes

The existing GitHub TLS adapter checks exact policy/session/gate review bodies
under caller-supplied bootstrap facts, but it returns observations only. Its
`BootstrapRoot` construction does not authenticate operational adoption, and
`GateFacts` cannot prove execution, target identity or custody. P1–P4 remain
unresolved as listed in the interchange implementation tasks; only the named
owner's separate two-role bundle exception is decided within P4.

`tools/access_telemetry_c1_authority_boundary.py` now authenticates an exact
retained C1.15 capture Ref against bytes, parses capture and disposition, checks
their asserted gate/current-profile/target/session/source linkage, rejects blocked
captures and reviews before capture completion, and ends in
`authority-prerequisites-unapproved` even for a matching accepted claim. It has
no provider import, adapter, target, output or authority-success path. This is offline refusal
preparation, not I3 completion or a deployed consumer integration.

`tests/tooling/access_telemetry_c1_interchange/test_authority_boundary.py`
exercises matching-but-unauthorized, forged labels/receipt, changed bytes,
wrong capture/gate/scope, negative decision, self-review and malformed inputs.
`docs/operations/c1-github-authority-contract.md` records the terminal API and
P1–P4 owners. No accepting producer entry or legacy C1 consumer was changed.

Focused verification: eight new boundary tests and 43 existing GitHub authority
tests passed with zero failures or skips. `git diff --check` passed. The
production disabled overlay, Story 27.4, its evidence and sprint tracking had
no diff. No live target was contacted by this new boundary or its tests.

Remaining owner decisions for I3: P1 Security must approve the full
producer/gate/session identity and receipt protocol, issuer/trust roots,
retrieval/revocation, principal mapping and owning maintainer. P2 Security with
Operations must approve numeric collection/review/session/decision/status-age
limits, authenticated time/skew and key rotation. P3 Operations with Security
must approve actual cluster identity, a scoped authenticated session grant and
immutable custody/retrieval/cleanup ownership. P4 Security, gate approvers,
Operations and Architecture must approve gate role/action grants,
delegation/quorum and acyclic C1.23–C1.25 dependencies. The authenticated
`github:user:6775094` two-bundle-role exception is partial P4 only.

## Review Triage Log

| Finding | Verdict and evidence |
| --- | --- |
| Provider import | medium, patched: `AuthorityScope` imported the Platform-backed GitHub module just to check caller labels; exact local scope fields and current profile/workload pins now keep the terminal refusal independently importable. |
| Blocked capture | medium, patched: the strict capture reader intentionally retains blocked captures; the terminal boundary now refuses blocked, failed, skipped or dirty candidates before considering disposition claims. |
| Review ordering | medium, patched: independent ordering requires a disposition after capture finish; same-instant and earlier claims now refuse before the generic prerequisite hold. |
| Provider/target sentinels | false after checking the call graph: the boundary imports only pure wire helpers and has no provider, process or network call path. The test additionally poisons socket connection, `subprocess.Popen`/`run` and `os.system`. |
| Empty notes | false: the reviewer observed the file before the implementation notes and focused results were written; the completed file records both. |

