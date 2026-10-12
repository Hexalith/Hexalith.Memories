---
title: 'PG2 C1 sole-owner run-record inspection'
type: 'feature'
created: '2026-10-12'
status: 'done'
route: 'dispatch'
baseline_commit: 'f4c7d68f619f376debca440387428139a61080cc'
context:
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md'
  - 'docs/operations/access-telemetry-owner-handoff.md'
---

<frozen-after-approval reason="owner approved sole-owner policy and verifier implementation on 2026-10-12">

## Intent

Implement one pure, bounded inspection of an owner-approved non-Production PG2
run record. Reuse current wire snapshots. A valid record may select the named
owner with disclosed self-review; it never passes a C1 gate or launches a target.
This is the first narrow verifier slice under the approved policy correction.

## Boundaries & Constraints

Follow `owner-run-records.md` exactly. Keep old authority/principal predicates,
deployed accepting registry and lifecycle consumers unchanged: new inspected
records must not bypass their current refusal. No target, process, network or
filesystem operation belongs in this pure module. No code or tests elsewhere
may be changed. Parent owns policy/document adoption and edits to this spec.

## I/O & Edge-Case Matrix

| Input | Expected result |
| :--- | :--- |
| Exact owner record with matching external anchors, current nonwithdrawn scope and permitted action | Immutable inspection result with self-review disclosure and exact record identity |
| Wrong owner/policy/environment/profile/workload/session/source/target/custody anchor; changed raw bytes | Content-free refusal |
| Missing/unknown fields, bad type/digest/path/time, duplicate/unknown actions or missing action | Content-free refusal |
| Unknown/withdrawn status, before start, at collection/review/expiry boundary or excessive window | Content-free refusal |
| Positive or negative inspection | Zero target/network/process/filesystem calls; no accepted gate or execution handle |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py`: reuse immutable `JsonSnapshot`, strict parsing, SHA identity and existing secret-safe predicates; inspect available signatures before use.
- `tools/access_telemetry_c1_approval_policy.py`: existing owner constant and older self-review refusal remain unchanged.
- `tools/access_telemetry_c1_authority_boundary.py`: deployed authority refusal remains unchanged.
- `tools/access_telemetry_c1_owner_run.py`: new pure record inspector, immutable result and content-free error.
- `tests/tooling/access_telemetry_c1_interchange/test_owner_run.py`: focused positive and negative boundaries.

## Tasks & Acceptance

- [x] Implement the closed owner-run shape, exact outside-record anchors, window/action predicates and immutable result in `tools/access_telemetry_c1_owner_run.py`.
- [x] Add meaningful positive, type/scope/time/withdrawal/secret/immutability and zero-dependency tests in `tests/tooling/access_telemetry_c1_interchange/test_owner_run.py`.
- [x] Run the new focused tests and affected full interchange suite with `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0`; retain exact output/counts. Report any blocker without changing old expectations.

Given exact current owner approval and matching anchors, when inspected, then a
bounded immutable inspection is returned with explicit self-review disclosure.
Given any invalid or unapproved scope/status, when inspected, then it refuses
without dependencies or accepted evidence. Existing deployed consumer behavior
must remain unchanged.

## Implementation Notes

Approved parent policy: `sprint-change-proposal-2026-10-12-sole-owner-qualification.md`.
The user approved repository verifier implementation with “yes”; no repeated
policy approval is needed. This pure first slice does not complete I3/I5 or 27.4.
Only the two new code/test files above are delegated to the implementation agent.
The matrix’s collection-end refusal is action-aware: it applies to collecting
actions; close-out/publish are allowed at collection end. Review end and expiry
refuse every action, as the precise context contract specifies.

Historical classification: existing wire primitives are `current-narrow-pattern`;
the approved policy and spec 14 are `historical-reference-only`; full 25-gate
delivery is `anti-template`, requirement inventory only. Slice proof: one
demonstrable inspection verdict, with no collector, registry or consumer migration.

## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p test_owner_run.py -v` — 23 tests in 0.231s; OK, exit 0.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — 265 tests in 66.415s; OK, exit 0, zero failures/errors/skips.
- Retained receipt: `/tmp/pg2-owner-run-im6zd85m`; 19 existing code/test hashes unchanged.
- Matrix audit: positive/exact-byte and immutable-result tests; fixed/external scope and type/secret tests; action/window/withdrawal tests; zero-dependency sentinel test all ran and passed. Collection-end behavior follows the action-aware context contract.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| :--- | :--- | :--- |
| BH1 timezone callbacks and retained drift | medium | Reproduced RuntimeError escape and changed isoformat after mutating custom tzinfo. Patch: immutable built-in UTC timezone guard, normalized retained instant and focused regressions. |
| BH2 expiry does not independently shorten review | false | The contract intentionally requires expiry at/after review end and ends use at the earlier window. Current code refuses after review and rejects invalid expiry ordering; no later use is accepted. |
| BH3 cleanup after expiry/withdrawal ambiguous | medium | Handoff promised failure cleanup without explaining its separate scope. Direct documentation clarification assigns bounded preapproved restoration to later finalizers; this inspector grants no cleanup or execution. |
| BH4 canonical policy bytes unspecified | medium | Multiple policy documents and CRLF/Git representations could diverge. Direct clarification selects exact retained approved proposal bytes and forbids substituting its Git-normalized blob. |
| BH5 full plan conflated with closed record | medium | Guide attributed resource/status/cleanup facts to the smaller closed shape. Corrected the guide to distinguish full plan and inspection record and describe their exact-byte reference binding. |
| BH6 discovery/approval sequence circular | medium | The guide required observations before approval while denying initial target access. Clarified explicit bounded read-only discovery approval before observations, followed by observed-target mutation approval. |
| BH7 no status lookup in pure inspector | false | API explicitly receives an already checked current caller fact and returns no authority. No consumer or filesystem status reader is claimed. Added binding/fresh-read guidance for the later I3 reader, with unknown/mismatch refusal. |
| BH8 approval graph absent | medium | Existing P4 parents were retained but their applicability to owner mode was indirect. Copied the existing acyclic parent sets and freeze/role order into the active owner contract; no new graph or gate was introduced. |
| BH9 owner-mode matrix absent | low | Current 23-case owner inspection matrix exists in its spec/tests, while the companion only showed the older full pipeline. Added an active section that separates executed inspection from required future I3/I5 cases. |
| BH10 local availability rule implicit | low | Verification requires the bytes, but archive lifetime was implicit. Direct clarification retains evidence through acceptance/terminal/publication and keeps resource cleanup separate from archive removal. |
| EC1 arbitrary tzinfo exception | medium | Independently reproduced; same root cause and patch as BH1. |
| EC2 apparent retained instant changes | medium | Independently reproduced custom timezone mutation; same root cause and patch as BH1. |
| Verification-gap layer | false | Reviewer found no gap and independently passed all 23 focused cases; no finding was filed. |
| Parent historical digest preservation | medium | Prepending current policy to the old signed-packet blocker changed referenced bytes. Removed that notice and verified its original digest; supersession remains in active documents. |

No intent_gap or bad_spec remains: code corrections preserve the specified pure,
content-free immutable inspection; document patches clarify approved plan/owner
policy mechanics and do not add a public code surface or change technical gate
expectations. Full I3/I5 and live 27.4 remain outside this inspection slice.

## Final reviewed verification, 2026-10-12

- The implementation agent's post-review focused command above passed 25 tests in 0.092s, exit 0, bare `OK`. Receipt: `/tmp/pg2-owner-run-review-fix-64r9byuy`. Both timezone regressions ran.
- Parent reran the full affected interchange command above: 267 tests in 84.285s, exit 0, bare `OK`, zero failures/errors/skips. Receipt: `/tmp/pg2-owner-run-final-58o8_ync`.
- Nineteen existing code/test files, five protected status/A41/Production-related files and the historical blocker packet match their original raw-byte hashes. Existing deployed authority/consumer refusal remains unchanged.
- All three independent review layers were triaged; concrete code/document findings were corrected, with no deferred finding. Required full I3/I5 and live qualification are remaining parent work, not review deferrals or completion of 27.4.
- The completed slice's story key is empty; no sprint state is advanced. No local commit is created because the owner's repository instructions prohibit staging/committing unless the task explicitly requires it.

Final interchange stderr SHA-256: `431e213837cb8a158561b3bd461cc6ce0184f496e97dc788e0ac4315f23364e2`. New module SHA-256: `f72e798f9cc466f36c84bfd632c323e7ecd0b901eca93ae44344ef898b27ffea`; focused-test source SHA-256: `765a2b46e97d5d485f95f0e8950c468d3769eaa12095b576b0a9037f32c92387`. Retained hashes identify offline execution and confer no authority or C1 pass.
