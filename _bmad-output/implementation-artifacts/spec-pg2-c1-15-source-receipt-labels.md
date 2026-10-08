---
title: 'PG2 C1.15: correct source receipt labels and identity digests'
type: 'bugfix'
created: '2026-10-08'
status: 'done'
route: 'dispatch'
baseline_commit: 'dbe4ce0a97c6a3a883af800f7873a99b79434d44'
review_loop_iteration: 0
context:
  - 'AGENTS.md'
  - '.editorconfig'
  - '.gitattributes'
  - 'project-context.md'
---

<frozen-after-approval reason="owner explicitly selected the recommended separate producer correction">

## Intent

Correct the shared collector's PowerShell source-label interpolation so actual
C1.15 captures retain distinct `kubectl:<purpose>:stdout` and `:stderr` receipts,
including per-Pod purposes. Change only the two label expressions in
`tools/verify-access-telemetry-c1.ps1`; preserve stream digests, sanitized
projections and neutral gate/disposition fields. Add regression tests in
`tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
that inspect actual observed one/two-Pod and blocked fixture captures through
I1's unchanged strict reader, and exercise legacy shared-call compatibility.

This is an offline producer prerequisite. Preserve existing edits, PG2 profile,
all authority/acceptance/consumer/deployment/status and historical evidence bytes.
Do not issue acceptance, operate a real target, register successors, change
policy, stage/commit/push, or update dependencies. Story 27.4 remains incomplete,
A41 open and Production writes disabled. Existing tests may establish their
commitlint-validated baselines inside disposable fixture repositories only.

**Owner decision 2026-10-08:** “apply recommendation” authorizes the PG2-only
identity-digest extension, superseding the two-label-expression restriction.
At the initial Pod query and final recheck, store the matching validated command's
existing `stdoutSha256` directly in the identity receipt. Keep parsing trimmed
text, v1 identity hashes, stream digests, metadata projections and strict readers
unchanged. Clarify these PG2 semantics in
`docs/operations/access-telemetry-c1-pg2-c1-15-contract.md`. Add successful
one/two-Pod, safe nonzero per-Pod command, mismatched digest rejection and legacy
compatibility checks. No new API/helper, authority policy or live target work.

</frozen-after-approval>

## Implementation Notes

The collector retains the two `${Purpose}` label fixes. The approved PG2-only
extension copies the initial/recheck Pod query's matching validated command
`stdoutSha256` directly into its identity source receipt at the existing
validation-before-publication boundary. Legacy v1 still hashes trimmed identity
text; parsing, stream digests, sanitized metadata projections and neutral fields
are unchanged. Actual one/two-Pod, wrong-context and safe nonzero per-Pod captures
exercise unchanged I1 parsing, exact source/stream digests, identity digest
mutation rejection and legacy compatibility. PowerShell CRLF is preserved. No
reader or fixture issuer is relaxed. This non-epic prerequisite has no
sprint-status synchronization.

Initial regression setup had a wrong expected blocker spelling, corrected to the
existing `profile-context-mismatch`; its observed cases also hit the 38-second
harness timeout while host load was high. The new label tests select the existing
supported 90-second collector parameter without changing production defaults or
the shared harness. Corrected pre-fix blocked regression then failed solely on
collapsed labels (`3 != 2`), exit 1, in 4.733s; receipt `red-labels.*` at
`/tmp/pg2-c1-15-source-labels-oqw2ywf1`. Later verification controls completion.

No staging/commit/publication is authorized by the parent scope or this selected
prerequisite; those constraints override the one-shot workflow's generic commit
step. Tests may use their existing disposable repositories and commitlint gates.

## Code Map

- `tools/verify-access-telemetry-c1.ps1:353` — corrected stream labels; retain `${Purpose}`.
- Same file: command stdout digest is computed before `return $stdout.Trim()`;
  lifecycle-pods identity source receipts at lines 701/957 now copy the matching
  validated command receipt in PG2 only, preserving v1 trimmed identity hashes,
  observations and all stream digests.
- `tools/access_telemetry_c1_interchange.py:748` — unchanged strict reader requires
  identity source and command stdout digests to agree; never relax it.
- `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
  — actual one/two-Pod, wrong-context and safe nonzero per-Pod captures now
  pass unchanged parsing, with independent byte digests, digest mutation refusal
  and legacy trimmed-hash compatibility.

## Tasks & Acceptance

- [x] `tools/verify-access-telemetry-c1.ps1` — retain label fix; use exact
  corresponding validated stdout digests for PG2 initial/recheck identity sources.
  Both existing validation-before-source-publication boundaries remain intact;
  preserve v1 hashes and all neutral/secret-safety/stream behavior.
- [x] `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
  — actual successful one/two-Pod and blocked/nonzero captures parse unchanged;
  independently assert initial/recheck identity digests against fake output bytes
  including trailing newlines, mutated digest rejection, and existing v1 behavior.
- [x] `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md` — document exact
  PG2 identity-byte hashing and distinct stream labels; historical packets stay
  immutable and receive no retroactive acceptance.
- [x] This spec — record verification/review and close only this prerequisite.

Given a successful PG2 fixture capture, when inspected by the unchanged I1 reader,
then unique source labels and exact matching stream/identity hashes admit the
same immutable snapshot with neutral gate/disposition fields.
Given safe failed observations, when inspected, then actual exit/failure facts
and source digests survive with no acceptance; malformed/secret/changed hashes
still refuse. Given legacy v1 capture, when produced, then its established trimmed
identity hashes remain. Given completion, then user edits, reader/authority/consumer,
profile/deployment/history/status and index bytes remain untouched.

## Review Triage Log

- Blind B1 — medium / replan: actual one/two-Pod parser calls fail with
  `artifact-literal-mismatch` at `_source_receipts:748`; existing trimmed identity
  source hashes disagree with exact stdout receipts. Scope currently locks the
  producer correction to two labels, so owner selection is required to extend it.
- Blind B2 — medium / planned patch: current blocked case is successful command
  plus wrong context; it does not exercise safe nonzero per-Pod stream receipts.
  Add that actual failed-observation case once the identity-digest scope resolves.
- Approved-extension implementation disposition — B1 is corrected by the PG2-only
  initial/recheck digest branches. B2 is covered by
  `test_safe_nonzero_per_pod_command_retains_exact_streams_and_strict_reader_blocker`:
  actual exit 71, zero results, one failure and exact empty stdout/nonempty safe
  stderr digests survive unchanged strict parsing without acceptance. Workflow
  review remains the parent's final completion step.

### Approved-extension independent review

All three layers reported before triage. The edge-case layer returned `[]`;
the verification-gap layer reported no gaps after four changed tests passed
in 17.437s. The blind layer supplied seven findings, each assessed below.
Its floor arithmetic is recorded with its report, not counted as a finding.

- Blind extension B1 — false / rejected: the complete workspace diff includes
  the untracked prerequisite. Step 4 deliberately keeps this change's own account
  private to the edge-case layer, which read and checked the supplied spec;
  omitting it from the blind/verification diff is required, not missing review.
- Blind extension B2 — low / rejected: the Platform gitlink is unrelated existing
  user work, already present in the approved baseline/status. It appears because
  the workflow requires the full tracked diff; this build changes no dependency.
  The explicit intent requires preserving it, so no split or Git mutation applies.
- Blind extension B3 — low / patch: the contract's unqualified C1.16 statement
  obscures the shared current-context label correction. Existing C1.16 complete
  tests assert sanitized/source hashes but not those two stream labels/digests.
  Clarify shared labels versus unchanged C1.16 projection hashes and add direct
  assertions to its existing complete-capture test; no fixture/helper change.
- Blind extension B4 — low / patch: PG2 rejection tests do not assert absent
  identity sources at both malformed query boundaries or inspect their blocked
  bytes with I1; the explicit malformed-projection publication matrix is v1.
  Existing PG2 guards reject the queries, so the gap is regression proof rather
  than a current publication defect. Add two actual PG2 malformed-query cases.
- Blind extension B5 — low / rejected as outside intent: Story 34.7's criterion
  lists codec/arity/class-order changes but omits explicit byte-encoding changes.
  AD-23 still mandates exact encoding and a new tag (spine lines 257/259), and
  its story retains that normative dependency. Any planning wording correction
  belongs to the user's unrelated Epic 34 work, which this intent must preserve.
- Blind extension B6 — false / rejected: the reservation criterion is not limited
  to sequential inputs; it rejects a second different canonical value before
  any key use, while AD-23 mandates no admitted inputs alias. Concurrent admission
  of distinct colliding values would already violate that rule. There is no
  changed reservation implementation here establishing the alleged outcome.
- Blind extension B7 — medium / rejected as outside intent: the Epic 34.10 row's
  universal middleware claim overstates token-enabled enforcement. The middleware
  passes through on an empty resolved token (lines 47–52), including an explicit
  host override (lines 79–91/111); production startup checks environment tokens.
  This is unrelated user planning work, excluded by the explicit preservation
  intent; this producer correction must not alter its evidence or token policy.

B3 and B4 have distinct roots and each routes to a trivial patch. No intent/spec
loopback or deferred-work entry is needed. The additional existing C1.16 test
file has a before-byte snapshot at the verification receipt directory; editing
only its compatibility assertions follows the approved shared-call intent.

- B3 patch verified — contract now explicitly names the C1.16 shared stream-label
  correction, while its projection/identity hashes remain unchanged. The existing
  complete-capture test asserts the exact two stream labels and their independently
  computed hashes for both emitted captures; projection receipts are preserved.
- B4 patch verified — the new initial/recheck malformed-query subcases retain
  exit-zero validated command facts but publish no rejected identity source,
  remain neutral/blocked, and pass exact immutable bytes through unchanged I1.

Review complete: all three layers ran, both direct follow-ups are fixed, and
nothing is deferred within this prerequisite. Parent 27.4 remains incomplete.

## Verification

`focused-green.*` at `/tmp/pg2-c1-15-source-labels-oqw2ywf1`: exit 1;
2 unittest cases, observed subcases error on the independent identity digest
mismatch; blocked exact-byte capture passes. Source bytes stayed unchanged.
All new source-label/digest assertions preceding the observed parser calls pass.
This is a scope discovery, not qualification or permission to operate a target.

Owner approved and continued this concrete extension after the options review;
no further approval is needed for the specified offline work. Scope is non-epic,
so `story_key` is unset and sprint bytes must not be synchronized. Use existing
fake-kubectl fixtures only. Avoid a new helper for two straightforward branches.
Retain exact test commands/logs/source hashes at
`/tmp/pg2-c1-15-source-labels-oqw2ywf1`; its `run_lane.py` can log lanes. Run new
focused cases first, then the full PG2 module plus relevant existing legacy v1
and C1.16 shared-call checks and the 32-test artifact-reader lane. Runtime test
fixtures may create commitlint-validated disposable repository baselines only.
CI already discovers this test file. No application/runtime code changes warrant
an Aspire launch or .NET build for this producer-only task.


### 2026-10-08 approved producer extension verification

All lanes ran through `/tmp/pg2-c1-15-source-labels-oqw2ywf1/run_lane.py` with
`PYTHONDONTWRITEBYTECODE=1`, `PYTHONHASHSEED=0` and
`PYTHONPATH=tests/tooling/access_telemetry_c1`. Each named `.json` retains the
exact command arguments, timestamps and before/after producer, test and unchanged
strict-reader source hashes; each matching `.log` retains the test result.

| Lane receipt | Command/test scope | Result |
| --- | --- | --- |
| `extension-red.*` | New observed one/two-Pod and safe nonzero regressions plus legacy compatibility, before the PG2 digest fix | Expected exit 1; 3 methods, 3 identity-digest assertion failures across observed/failed subcases; legacy passes; 13.911s |
| `extension-focused-green.*` | Source receipts, wrong context, safe nonzero per-Pod command and legacy v1 methods | Exit 0; 4 tests; 18.005s |
| `extension-pg2-module.*` | `python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p pg2_runtime_control_plane_identity_test.py -v` | Exit 0; 25 tests; 499.766s |
| `extension-shared-legacy.*` | Existing legacy complete capture, Pod template exclusions, sanitized metadata, lowercase mode, C1.16 historical/current captures and secret/failure/timeout bounds | Exit 0; 7 tests; 71.018s |
| `extension-artifact-readers.*` | `python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p test_artifact_readers.py -v` | Exit 0; 32 tests; 11.619s |

All lane receipts record unchanged producer/test/reader bytes during execution.
The new successful test independently hashes the fake initial/recheck projection
stdout including trailing newlines, varies the recheck bytes and reverses the
two-Pod order. It rejects mutations to either identity source digest or its
corresponding command stdout digest through unchanged I1. Legacy v1 independently
retains trimmed identity hashes for both queries.

Affected surface: collector observation-source attribution only. Focused
fail-closed evidence is
`test_blocked_capture_source_labels_remain_distinct_and_structurally_inspectable`
(wrong-context denial), the mutated identity/command digest subcases in
`test_source_receipt_labels_bind_each_stream_and_strict_reader_inspects_actual_capture`,
and the safe exit-71 test named above. The full PG2 lane also passes malformed
UTF-8/JSON, secret-shaped/encoded credentials, wrong runtime identities,
Pod/source drift and invalid input/session refusals. No live target was used.

Implementation self-review confirms that each identity entry still follows its
existing projection validation and has no intervening kubectl call before copying
the matching query receipt. Legacy branches, command stream hash/safety logic,
sanitized metadata, neutral fields and strict readers are unchanged. The reader
SHA-256 remains
`1ac1a117a23c326974b3dac9d61b9638c0bac9279d11c975d048931c57aec9de`.
PowerShell and contract bytes retain CRLF; Python retains LF.
`extension-implementation.diff`, `extension-scope-hashes.json` and
`extension-byte-checks.json` retain the implementation delta and byte checks
against the starting user-edited files. No repository staging, commits, branch
changes, dependency updates or publication occurred. Independent workflow review
and the final prerequisite-only status/task closure remain with the parent;
Story 27.4, A41, Production writes and all historical dispositions stay unchanged.

### Final review-patch verification and closure

`parent-final-focused.*`: exit 0; six affected methods passed in 31.705s:
observed one/two-Pod capture, wrong context, safe nonzero per-Pod command,
legacy v1 identity hashes, malformed initial/recheck queries, and C1.16 shared
stream receipts. Exact command and all four producer/reader/test hashes are
retained in the JSON receipt. `parent_final_cases.py` only copies emitted bytes
before fixture cleanup; assertions and strict parsing are unchanged. Actual
snapshots are retained in `parent-final-packets/`.

The earlier 25-test PG2, 32-test artifact-reader and seven-test compatibility
lanes remain valid for their tested behaviors. Review changed only documentation
and test assertions, adding one PG2 method; no production/reader/helper/fixture
bytes changed, so final verification reran the six affected methods rather than
the unchanged broad negative matrix. CI discovery includes the new method.

Intermediate patch receipts are preserved: `review-fixes-focused.*` first found
that uniqueness must apply to the changed stream labels, since C1.16 retains its
existing repeated projection receipts. `review-fixes-focused-green.*` then
passed C1.16 but saw an `artifact-command-chronology` rejection in source Git
receipts for the initial malformed PG2 subcase. That emitted packet was removed
by fixture cleanup, so the exact interval and cause are unconfirmed. Later
`review-fixes-malformed-retained-run.*` passed the same method in 7.355s and
retained exact packets/timestamp sidecars; the final six-method run also passed.
The temporary retained runner's initial import-path setup failure remains in
`review-fixes-malformed-retained.*`. No retry logic, timestamp normalization,
assertion weakening or parser relaxation was added. The single intermittent
chronology failure remains a verification limitation, not qualification credit.

Only this prerequisite is done. No sprint synchronization, staging, commit,
push, branch, dependency update, live operation or historical acceptance was
performed. The explicit no-Git-mutation intent overrides Step 5's generic commit
instruction. Parent handoff records completion without clearing its live gates.
