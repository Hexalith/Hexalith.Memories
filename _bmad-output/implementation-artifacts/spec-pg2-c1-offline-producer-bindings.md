---
title: 'PG2 C1 offline producer registry and source-byte inspection'
type: 'feature'
created: '2026-10-08'
status: 'done'
route: 'dispatch'
baseline_commit: '906bc07ad6a8e4912a7222d9d097da434148266a'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/producer-bindings.md'
  - '{project-root}/_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/schemas.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Prepare the separately authorized offline I2 producer-binding library: inspect closed registry entries and prove that supplied retained source bytes match neutral C1.15 capture receipts. The owner approved this recommended scope with “apply recommended” on 2026-10-08. Return immutable inspection results, never acceptance or execution authority.

## Boundaries & Constraints

**Always:** Reuse I1 snapshots/J1/Refs and the unchanged C1.15 reader. Keep deployed accepting entries absent. Require explicit immutable byte snapshots and a full canonical expected source commit. Compare supplied commit labels only: this slice cannot authenticate a Git repository/commit, real attributes, source custody or producer execution. Registration, command grammar and role-policy interpretation remain unsupported; any deployed-binding request refuses. Preserve existing workspace changes and all Story 27.4/A41/Production/sprint/history holds. Existing preparation: `/tmp/pg2-c1-i2-receipt-path` identifies baseline/protected receipts.

**Never:** Read files, invoke Git/processes/collectors, contact networks/targets, load ambient configuration or execute a verifier from library functions. Do not add accepting entries, gate semantics, authority integration, consumer migration, accepted artifacts, operational defaults, dependencies or Git mutations. Positive tests use visibly isolated fixtures; fixture code/source bytes supply no registration.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected result | Refusal |
| --- | --- | --- | --- |
| Registry | Exact numerically ordered entries | Frozen inventory plus entry/registry J1 hashes | Unknown fields/types, malformed Refs, invalid gate/profile, unsafe/duplicate/unordered/overlapping paths |
| Sources | Neutral observed clean C1.15 capture and complete retained byte pairs | Recomputed executed/blob SHA-256 and Git blob SHA-1 agree; CRLF executed bytes may differ from LF blobs | Missing/extra/substituted sources, changed commit/hash/OID, dirty/blocked/recheck drift |
| Bounds | Immutable UTF-8 bytes and explicit supported normalization metadata | Per-snapshot 1 MiB and shared unique-retained 32 MiB bounds include JSON and both byte forms | Mutable/oversized/invalid bytes, unsupported transforms/filter/encoding/ident or nonregular mode |
| Eligibility | Empty registry or successful fixture inspection | Deployed lookup refuses before dependencies; no accepted output | No fixture/label/Ref can authorize deployment |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py` — reuse `JsonSnapshot`, `parse_capture`, `EvidenceReference`, `j1_bytes` and strict lexical/schema helpers. No edits required.
- `tools/access-telemetry-c1-profile.ps1:Get-C1SourceState` — independent reference for UTF-8 CRLF-to-LF and `blob <length>\0` Git SHA-1; do not invoke it.
- `tests/tooling/access_telemetry_c1_interchange/test_artifact_readers.py` — independent `capture_fixture` and exact nineteen-source inventory. Keep existing cases unchanged.
- The frontmatter contracts define thirteen entry fields and unresolved P1/P4/P5 eligibility. Ordinary `done`/path labels do not authenticate registration.

## Tasks & Acceptance

- [x] `tools/access_telemetry_c1_producer_bindings.py` — add frozen registry/source inspection APIs, importing existing primitives; no deployed integration.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_producer_bindings.py` — independently authored registry/capture/source fixtures, literal J1/Git-OID vectors, matrix negatives, immutable outputs, budget boundaries/deduplication and zero-dependency checks.
- [x] `docs/operations/c1-producer-binding-inspection-contract.md` — document actual APIs, input/proof limits and reproducible offline command.
- [x] `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md` — append scoped I2 preparation/result citation; preserve prior text and incomplete I2–I6/P1–P7 state.

**Acceptance Criteria:**
- Given a registry snapshot, when inspected, then the exact thirteen fields, canonical paths, ordered unique helper/input sets disjoint from the producer and each other, three strict Refs and numeric unique gate ordering are enforced; J1 hashes preserve retained bytes separately. Empty inventories are valid inspection.
- Given supported C1.15 retained source pairs, when compared, then registry producer/helpers/inputs exactly match capture membership, full commit labels agree, every receipt's clean state and byte/Git identity is checked, and no caller pass flag establishes success.
- Given UTF-8 CRLF-to-LF normalization, when comparison runs, then exact normalized bytes equal supplied blob bytes; independently recomputed digests/OID match. Explicit clean filter/working-tree encoding/ident transforms and symlink/submodule modes refuse. Metadata itself remains untrusted inspection input.
- Given any positive or negative input, when a library API runs, then filesystem/process/network call counters remain zero, refusal contains no source content, outputs/containers remain immutable and deployed eligibility stays unavailable.
- Given the completed slice, when regression and preservation checks run, then all focused tests pass without skips, existing sources/consumers/protected bytes remain identical, and no checkpoint or whole-story status advances.

## Design Notes

Use a JSON array for registry inventory, hashed as J1 ordered entries. Source validation supports only the existing C1.15 capture schema; other schema strings can be inventoried without eligibility. Byte records explicitly name path, commit, executed/blob bytes, supported normalization and regular mode/transform metadata. Bound both byte forms before decoding/hashing and deduplicate identical retained bytes. Hash or shape agreement cannot authenticate command/role/registration Refs. Repository resolution, safe custody traversal and semantic verifier execution remain later prerequisites.

## Implementation Notes

User approval covers this concrete bounded scope; routine API details are implementation decisions. No further scope approval is required to implement and review this offline slice.

Implementation completed in the four task paths. Parent read the unified diff
and checked every AC/matrix row against the executed 24 new tests. Registry:
`test_all_thirteen_fields_missing_unknown_duplicate_and_wrong_type_refuse`;
sources: `test_each_source_receipt_digest_and_oid_are_recomputed`; bounds:
`test_exact_32_mib_shared_budget_includes_json_and_both_forms`; eligibility:
`test_deployed_lookup_always_refuses_for_empty_fixture_and_opaque_inputs`.
All ran and passed. Existing I1/consumer/source/protected bytes and nine gitlinks
are unchanged. Source normalization metadata and commit labels remain untrusted.

## Spec Change Log

## Review Triage Log

All three independent layers reported before classification. Each finding below
received its own verdict before grouping. Local reproductions are retained at
`/tmp/pg2-c1-i2-2z7veqau/review-reproductions.json`.

| Finding | Verdict | Evidence |
| --- | --- | --- |
| BH1: empty-byte source cardinality | medium | A supplied tuple of 256 distinct paths and empty byte pairs passed 256 shape checks before source-set refusal; the byte budget charged no source bytes. Offline callers can incur unnecessary dictionary growth and work before the fixed nineteen-record contract is checked. |
| BH2: UTF-8 exception context retains contents | medium | Invalid supplied UTF-8 raised the bounded format error with a `UnicodeDecodeError` context whose `object` retained the exact source bytes. Suppressing display does not remove that accessible content. |
| BH3: Git SHA-1 provider refusal | medium | A SHA-1 provider raising `ValueError` escaped the inspection API as that raw exception. Python's [hashlib contract](https://docs.python.org/3/library/hashlib.html) supports `usedforsecurity=False` for nonsecurity checksums in restricted environments; the Git object checksum must either compare or refuse through the bounded API. |
| BH4: registry cardinality/order checked after hashing | low | A 26-entry repeated inventory caused 26 entry hashes before duplicate/order refusal, although only twenty-five unique gate labels exist. The registry inspector can reject the demonstrated invalid inventory before digest work. |
| BH5: returned source retention unclear | low | `SourceInspection` retains the registry/capture JSON and source identities, but neither the supplied byte pairs nor transform/mode metadata. The API document does not say callers must retain those original snapshots to reproduce the comparison. |
| BH6: dependency denial coverage incomplete | medium | The denial test wraps selected positive/negative calls, while the UTF-8, transform, malformed-path and budget refusal cases execute through `refuse` without the same barriers. Those refusal paths therefore have no regression assertion for the approved zero-I/O contract. |
| BH7: selected entry/source permutation coverage | low | The successful source comparison uses a single-entry registry and one supplied order. The library permits numerically ordered multi-entry inventories and order-independent source membership; a direct positive case can pin the selected C1.15 entry and stable identities. |
| BH8: preservation manifest only temporary | low | The twelve protected-file comparisons and hashes live only under `/tmp`; the published offline contract cannot reproduce that claimed preservation check after the temporary receipt disappears. Record the path/hash manifest and verification command in that existing document. |
| BH9: parent draft code-map wording | low | The parent 27.4 draft still says registry/semantic assembly is absent and does not name this new inspector. This is a spec-only editorial finding; reject under Step 4's rule against findings whose fix edits this build's spec. The current slice's operations contract already distinguishes inventory inspection from absent deployed eligibility. |
| EC1: empty-byte source cardinality | medium | Independently reported the same reachable unbounded tuple/dictionary work as BH1; the 256-record reproduction confirms membership refusal occurs after accumulating every record. |
| VG1: returned digest names unpinned | medium | Pre-verified by the verification-gap layer: swapping executed and blob digests in `SourceByteInspection` left all twenty-four tests green. The positive assertion only checks inequality; pin each returned named digest to an independently computed digest of its corresponding supplied bytes. |

BH9 is rejected by the explicit spec-edit rule. BH1 and EC1 share one cardinality
root cause and form one patch entry. All other surviving findings are separate
patch entries: each has a direct correction, adds no public API, and guards only
reproduced states or strengthens existing verification/documentation. No intent
change, spec loopback or deferred-work entry is required.

## Verification

`env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` must pass with zero skips. Run the narrow new test file first. Recheck `/tmp/pg2-c1-i2-receipt-path` protected hashes, existing canonical history and nonrecursive gitlinks; `git diff --check` must pass. Record actual commands/counts and named negatives after execution.

Executed final verification on 2026-10-08 from the repository root:

- Focused command above with `-p 'test_producer_bindings.py'`: 28 tests passed,
  zero failures/errors/skips (1.119 seconds).
- Full command above with `-p 'test_*.py'`: 195 tests passed, zero
  failures/errors/skips (45.241 seconds), including all 167 pre-existing cases.
- Three independent review layers returned eleven individually triaged findings.
  Ten findings were addressed in nine patch entries; the spec-only editorial
  finding was rejected under the workflow rule. Nothing was deferred.
- Added executed cases: `test_source_count_refuses_before_any_source_shape_or_digest_work`,
  `test_registry_count_order_and_uniqueness_refuse_before_entry_hashing`,
  `test_git_sha1_provider_failure_is_a_content_free_checksum_refusal` and
  `test_middle_registry_entry_and_reordered_sources_keep_selected_identities`.
  The existing UTF-8 case asserts no exception context/cause; every shared
  refusal now installs zero-I/O/configuration barriers.
- An in-memory constructor mutation swapping the returned executed/normalized
  digests produced the expected positive-test assertion failure, with no source
  edits. This closes VG1's demonstrated verification gap.
- The durable operations-document manifest exactly matched the twelve
  pre-implementation path/hash pairs; its published command executed successfully
  and reported `12 protected paths unchanged (offline validation only)`.
- Preservation check passed: twelve exact working-file hashes, two existing
  readiness-document hashes, two canonical history prefixes after Git newline
  normalization, approved frozen intent, unchanged HEAD and nine root gitlinks.
  All eight changed paths are expected; Markdown CRLF, Python LF, final newlines,
  trailing whitespace and `git diff --check` passed. The checker initially
  compared LF Git blobs directly to CRLF working files; normalizing only that
  history-prefix comparison resolved the checker mismatch without repository edits.

Detailed local receipts: `/tmp/pg2-c1-i2-2z7veqau/final-tests.json`,
`final-mutation-probe.json`, `final-manifest.json` and `final-preservation.json`.
The durable [inspection contract](../../docs/operations/c1-producer-binding-inspection-contract.md)
retains the proof limits, commands and protected hash manifest. This separately
tracked preparation is complete; integrated I2–I6/P1–P7 and Story 27.4 remain
incomplete, A41 remains open and Production remains disabled. No Git mutation
is authorized by the frozen intent, so no commit or push was created.
