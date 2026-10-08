---
title: 'PG2 C1 I1: strict structural artifact readers'
type: 'feature'
created: '2026-10-08'
status: 'done'
route: 'dispatch'
baseline_commit: '3e18d0dcdceb387eff89862c382637da89ad7e47'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned implementation intent supplied explicitly in this build invocation">

## Intent

Implement the remaining I1 closed structural readers and focused rejection tests
as a separately tracked library prerequisite for Story 27.4. Adopt the six wire
artifacts in the interchange schemas and the unchanged C1.15 v2 operations
contract, reusing immutable snapshots, references, J1 and existing parser bounds.

## Boundaries & Constraints

Preserve existing user edits and the named-owner approval policy. No accepting
registration, acceptance issuance, execution authority, consumer migration,
target contact, dependency change, staging, commit or push. Story 27.4 remains
incomplete, A41 open, Production writes disabled. A structural reader admits
shape and local consistency only; asserted approvals and hashes prove no grant.
Record unresolved contract choices without selecting operational defaults.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior |
| --- | --- | --- |
| Structural inspection | Well-formed artifact, including dirty/blocked capture | Original immutable snapshot retained; no acceptance verdict |
| Closed fields | Unknown/missing/duplicate fields at any nested object | Content-free refusal |
| Malformed/types/bounds | Invalid JSON, UTF-8/BOM, bad scalars/collections, excess bounds | Existing bounded refusal; no partial result |
| Contradictions | Counts/status/interval/cleanup/references disagree | Refusal without external calls |
| Existing hash format | Target and argument hashes over PowerShell compact JSON | Exact actual PowerShell encoding; J1 remains separate |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py`: reuse `JsonSnapshot`, `SnapshotReader`,
  `parse_ref`, `authenticate_snapshot`, content-free errors and J1 without changing
  existing behavior. Add six snapshot-based parsers plus strict schema dispatch.
- `tests/tooling/access_telemetry_c1_interchange/test_wire_primitives.py`: existing
  common bounds, original bytes, immutable trees and independent J1 vectors.
- `tests/tooling/access_telemetry_c1_interchange/test_artifact_readers.py`: new
  independent valid fixtures, nested mutation/rejection cases and PowerShell vectors.
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/schemas.md`
  and `implementation-tasks.md`: proposed exact fields, remaining I1-I6/P1-P7.
- `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md`: adopted v2 nested
  fields. Ordinary source files `tools/verify-access-telemetry-c1.ps1` and
  `tools/access-telemetry-c1-profile.ps1` settle emitted shapes; do not change them.
- Existing approval policy/authority modules and all consumers remain untouched.
  Existing CI discovery already includes new `test_*.py` files in this directory.

## Tasks & Acceptance

- [x] `tools/access_telemetry_c1_interchange.py`: add pure parsers for capture/v2,
  disposition/v1, accepted-gate/v1, manifest/v1, bundle-approval/v1, predecessor/v2.
  Require `JsonSnapshot`, return the same snapshot after validation, use exact
  version dispatch, reject other roots/versions and validate every nested object.
- [x] Same module: enforce canonical digests/commits/session labels; identities
  and roles bounded to 256; correct integer/bool/null/array distinctions;
  substantive secret-safe reasons; lexical credential-free absolute receipt URIs.
  No URI retrieval, origin allowlist or implicit trust. Preserve neutral capture fields.
- [x] Same module: check local producer/source/command receipts, ordered UTC
  intervals contained in the producer interval, count/status/blocker/observation
  summaries, tracked/head/matching facts and clean/dirty consistency. Known producer
  source paths are schema shape, never registered authority; byte provenance,
  command grammar/runtime eligibility and retained receipts remain future I2/I4 work.
- [x] Same module: check accepted disposition independence and unequal principals;
  positive accepted-gate counts, read-only versus required cleanup tuples; exact
  numeric C1.1–C1.25 manifest order and distinct referenced gate artifacts;
  two distinct predecessor approval references. Do not invent cross-artifact
  role checks or require different bundle reviewers; keep the owner's exception.
- [x] Same module/tests: recompute target/argument and legacy command hashes using
  documented order and actual PowerShell escaping; preserve original bytes and J1.
- [x] `test_artifact_readers.py`: cover matrix, every object field set, malformed
  types, contradictory facts, exact PowerShell vectors, nonzero valid observations
  and zero filesystem/network/process calls in parsing. Run existing wire and
  owner-policy regressions and the focused interchange suite; retain exact logs.

Given a valid snapshot, when a reader runs, then its exact bytes/digest and nested
immutability survive. Given a malformed or internally contradictory artifact,
when parsed, then it refuses with no external effects or authority output.
Given the completed library prerequisite, when records are checked, then existing
story/A41/Production, policy, consumer and user-edit bytes remain unchanged.

## Unresolved Contract Choices

P2 disposition precision and all numeric freshness/lifetime/skew/status windows
remain owner decisions. Inspection accepts only lexical UTC ISO instants with
explicit Z or +00:00 and at most seven fractional digits, comparing them exactly;
new proposed schemas use exactly three digits and Z. This is timestamp parsing,
not issuance policy. No current-time check is performed.

P1 receipt protocols/schemes/origins/trust roots and P3 custody/supporting streams
remain unresolved: lexical URIs and Refs establish no authenticated retrieval,
symlink safety, retained source or command replay. P4 gate review roles and acyclic
approval topology, P5 actual registrations/verifiers, P6 legacy renewal and P7
consumer dispatch remain outstanding. Cleanup receipt semantics belong to each
owning gate. None blocks structural inspection or authorizes acceptance.

Semantic adequacy of review reasons has no owner-approved length or word-count
threshold. Structural nonempty/substantive-text checks do not establish a justified
decision; future review/authority verification must establish that independently.
P2 must also reconcile exact seven-digit capture instants with the proposed
millisecond accepted-gate representation before deriving cross-artifact intervals.

## Implementation Notes

The invocation explicitly authorizes implementation and preservation of existing
dirty-tree changes. The build records unresolved operational choices as requested;
they are outside this library scope. No additional approval gate or story/sprint
registration is introduced. Protected-byte and index baselines are retained at
`/tmp/pg2-c1-i1-xvv0k0nk`.

Producer investigation: failed receipts can have zero or null exit; successful
Git queries can return empty streams. `modified` and `matchesHead` are independent;
executed and normalized hashes can differ for CRLF. `dirty-development` is sticky
from initial worktree state, so enforce clean implications only. Blocked capture
context can be null or differ from fixed target context. Compare the seventh
fractional time digit exactly. Actual PowerShell default JSON escapes U+0085,
U+2028 and U+2029 while preserving ordinary Unicode. The old PG2 producer test
fixture stages and commits disposable repositories; do not run that lane under
this invocation's no-stage/no-commit constraint.

Content checks must inspect decoded strings and credential-bearing URI query/user
info, including percent-encoded forms, with stable errors that never echo inputs.
The exact documented metadata shell probe legitimately names `DAPR_API_TOKEN`
and its variable expansion; any field-aware exception must match that entire
known probe, not broadly exempt arguments containing a credential name. Session
labels additionally reject credential-shaped names as the producer does.

Capture/producer/child timestamps specifically retain the existing exact
seven-digit `+00:00` round-trip spelling. The flexible lexical inspection rule
above applies only to the unresolved adopted disposition timestamps; new
bundle-approval and accepted-gate timestamps use their specified millisecond Z.

Source inspection found an existing producer discrepancy: PowerShell expands
`"kubectl:$Purpose:stdout"` (and stderr) to `kubectl:`. A local expression probe
confirmed this without running the collector. Ambiguous/repeated source labels
must refuse in the strict reader; do not relax admission or change the producer
in this library-only task. A separately scoped producer correction and source
binding/semantic verification remain necessary before actual capture admission.

Implementation completed in the two scoped code/test files. All six parsers and
`parse_artifact` return the exact supplied `JsonSnapshot`; only structural/local
checks are added. Artifact-specific Ref content checks reuse the unchanged generic
wire Ref implementation. Effective producer timeout bounds reuse its existing
1–300-second parameter contract and choose no operational time policy. Review
fixes preserve UTC precision, J1, the full metadata-probe exception and owner policy.

## Spec Change Log

## Review Triage Log

Three independent review layers read the authored code/test diff at
`/tmp/pg2-c1-i1-xvv0k0nk/review.diff`; the full workspace diff separately retains
the pre-existing user edits. No layers were skipped. Findings below were checked
against implementation, actual producer source and the invocation's library scope.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| Blind B1: quoted credential keys | medium / patch | Root reproduced admission of a reason containing a nonempty quoted password field; detect decoded structured bindings with content-free errors. |
| Blind B2: Ref content guard | medium / patch | New readers call the narrower existing Ref guard, admitting token bindings/private-token shapes rejected in other artifact fields. Add artifact-level content checks after existing lexical Ref validation; preserve generic wire behavior. |
| Blind B3: URI component syntax | medium / patch | Nonnumeric ports and raw path brackets admit. Check authority/port and path/query/fragment syntax without selecting schemes, origins or trust. |
| Blind B4: fragment credentials | medium / patch | Signature-shaped fragment bindings evade the query-key guard. Inspect decoded query and fragment content alike. |
| Blind B5: target actor omission | medium / patch | With consistent summaries and recomputed metadata hashes, every Pod can omit the declared target actor. Require that local target fact in each observed Pod. |
| Blind B6: whitespace process arguments | medium / patch | Typed argv strings are not substantive prose. Preserve empty/whitespace arguments with correct hashes; do not choose command grammar. |
| Blind B7: NUL process arguments | medium / patch | Root independently reproduced admission after recomputing both argument and legacy hashes. NUL cannot represent an actual OS argv member; reject it. |
| Blind B8: serial receipt chronology | medium / patch | The collector invokes children synchronously, but reversed or overlapping receipt sequences admit. Check chronology/nonoverlap and use realistic fixture intervals. |
| Blind B9: stronger reason threshold | maybe-false / rejected outside scope | Placeholder text is structurally admitted but establishes no justified review. No approved minimum length/word count exists; inventing such a policy conflicts with the invocation. Semantic adequacy remains an explicit unresolved contract choice. |
| Edge E1: missing target actor | medium / patch | Confirmed; shared cause and fix with B5. |
| Edge E2: nonnumeric URI port | medium / patch | Confirmed; shared cause and fix with B3. |
| Edge E3: form-encoded bearer query | medium / patch | Query values retain plus characters, hiding the common form-encoded bearer shape. Check its decoded content without rewriting retained URI bytes. |
| Verification V1: metadata digest regression | medium / patch | Reviewer disabled only the metadata projection hash check in memory: a changed canonical digest admitted and all 25 tests still passed. Add a direct metadata-source hash mismatch rejection assertion. |
| Root R1: credential name/value object | medium / patch | Root reproduced a reason embedding name=authorization/value=credential JSON. Include this structured representation alongside B1's direct quoted bindings. |

All in-scope review patches were applied and their regression cases executed.
No requested library work was deferred. Operational choices and the pre-existing
producer source-label defect remain outside this prerequisite, as recorded above.
No commit, staging, push, registration, story/status update or target call occurred.

## Verification

Final command:

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

Exact result: exit **0**, `Ran 167 tests in 59.866s`, `OK`; **zero failures,
errors or skips**. This includes 32 artifact-reader tests and all 135 existing
wire, owner-policy, GitHub review and authority regressions. Source/test bytes
remained unchanged throughout the final run. Python 3.14.4 and PowerShell 7.6.2
were used; actual PowerShell encoding vectors and local TLS fixtures executed.
These fixtures supply no live grant, custody, target eligibility or acceptance.

Receipts: `/tmp/pg2-c1-i1-xvv0k0nk/interchange-final.json` records exact argv,
environment, timestamps, exits and source digests; `interchange-final.log` retains
all individual results. Earlier baseline, artifact and implementation/review-fix
runs remain separately retained there, including 32 wire and 12 owner-policy
baseline passes and the preliminary 160-test run; the final 167-test result is
controlling evidence for the final source bytes.

The five matrix rows are covered by the executed snapshot identity/immutability,
every nested field/type mutation, malformed/common-bounds, contradiction and
actual PowerShell-vector tests. Additional regressions cover quoted/named JSON
credentials, artifact reference content, URI components/fragment/form encoding,
target actors, exact whitespace/NUL argv, serial child intervals and metadata
digest recomputation. Every reader's valid/refused path makes zero file, network
or process calls under the exercised dependency traps.

`git diff --check -- tools/access_telemetry_c1_interchange.py`: exit **0**, no
output. Python files retain LF. `preservation-final.json` confirms all **94**
protected files match their baseline, all seven pre-existing Git diffs match,
and both index entries and exact index bytes are unchanged. Story 27.4 remains
incomplete, A41 open, Production writes disabled; the named-owner policy,
consumer/authority modules, producer and deployment files remain unchanged.

Final tested source SHA-256:

- `tools/access_telemetry_c1_interchange.py`:
  `1ac1a117a23c326974b3dac9d61b9638c0bac9279d11c975d048931c57aec9de`
- `tests/tooling/access_telemetry_c1_interchange/test_artifact_readers.py`:
  `1869f3abbff92b395da7efcccbcc77cb335ee34f077e922511e13e3db1272be7`
