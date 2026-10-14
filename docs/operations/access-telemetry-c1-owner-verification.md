# Owner-bound C1.15 verification

`tools/access_telemetry_c1_owner_c115.py` composes the existing owner-run,
retained-source, strict command and retained-stream inspections for one
non-Production `PG-ONPREM-2` capture. A matching result binds the supplied byte
materials to the same approved-plan assertions and discloses
`sole-owner/self-reviewed`. It supplies no live execution evidence, deployed
registration, technical gate pass, target/action authorization or acceptance.
Story 27.4, A41, sprint holds and the Spec 14 live tasks remain open.

The fixed policy is `sole-owner-qualification/2026-10-12`, and the owner is
`github:user:6775094`. The existing profile/workload, target, Dapr 1.18.1 and image
pins are unchanged. The owner materials have separate schemas; the legacy
fixture registration APIs retain their stronger independent-review declarations
and their existing version selection. `lookup_deployed_binding` continues to
refuse unconditionally.

## Pure API and outside approval anchors

Put the repository's `tools` directory on the Python import path. All snapshot
acquisition and status acquisition belong to the caller. The inspector has no
filesystem, process, network, configuration or clock dependencies, and it has no
filesystem wrapper or acceptance-record writer.

```python
from access_telemetry_c1_owner_c115 import inspect_owner_c115

inspection = inspect_owner_c115(
    registry=registry_snapshot,
    capture=capture_snapshot,
    sources=retained_source_tuple,
    streams=retained_command_stream_tuple,
    run=owner_run_snapshot,
    anchors=approved_run_anchors,
    statement=owner_registration_snapshot,
    command_contract=owner_command_snapshot,
    review_role_policy=owner_role_snapshot,
    approved_registry_sha256=approved_raw_registry_sha256,
    now=caller_supplied_aware_utc_datetime,
    withdrawn=caller_supplied_current_boolean,
)
```

| Input | Contract |
| --- | --- |
| `registry`, `capture`, `run`, `statement`, `command_contract`, `review_role_policy` | Exact `JsonSnapshot` instances containing immutable retained bytes. Every snapshot is reparsed and rehashed from `raw`; cached trees and cached hashes are discarded. |
| `sources` | Exact built-in tuple of nineteen exact `SourceSnapshot` instances, including both executed and normalized Git blob byte forms and all required metadata. |
| `streams` | Exact built-in tuple of exact `C115CommandStreams` instances in capture receipt order. Each contains a purpose and exact immutable stdout/stderr bytes. |
| `anchors` | Exact `OwnerRunAnchors` instance from the retained owner-approved plan, obtained outside the candidate record. It contains `record_sha256`, `policy_sha256`, `session_id`, `source_commit`, `target_sha256` and `custody_root`. |
| `approved_registry_sha256` | Canonical lowercase SHA-256 from the separately approved plan, covering the registry's exact raw bytes, including whitespace. The registry's canonical J1 inventory hash cannot substitute for it. |
| `now` | Explicit aware UTC built-in `datetime`, with a built-in zero-offset `timezone`. No ambient time is read. |
| `withdrawn` | Explicit current built-in boolean. Missing status, nonboolean values and withdrawal refuse. |

Do not derive approval anchors or the approved registry digest from candidates
being inspected. Matching caller-supplied approval assertions cannot authenticate
the owner or registration; the inspector cannot establish how the caller
obtained those assertions. Re-read the run and withdrawal state before later use.

The run must pass the existing closed `owner-run/v1` inspector for `observe`.
Its status must be approved, its policy/owner/assurance and non-Production scope
must match the adopted constants, and its profile/workload pins must match the
unchanged values. Its collection phase must be active at `now`; `fault`, `purge`,
`reclaim`, `close-out` or `publish` permission alone cannot substitute for
`observe`. Withdrawal, expiry and use outside the run's collection phase refuse.

The capture's profile ID, profile/workload digests, qualification session, target
digest and full source commit label must match the inspected run. The capture's
start and finish must lie within the run's collection window, and finish must be
at or before `now`. Capture comparisons preserve all seven fractional digits
(100 ns); the run retains its existing millisecond UTC `Z` spelling. Start is
inclusive. Although the capture endpoint comparison permits the collection end,
`observe` itself requires `now` strictly before that end.

## Closed owner declaration schemas

All schema names below are prefixed with `hexalith.access-telemetry.c1.`. Unknown
versions and fields refuse; there is no version inference or fallback to fixture
schemas. Registry entries retain the existing thirteen-field inventory shape.

`owner-registration/v1` contains exactly the existing fixture statement fields
plus four owner fields:

`schemaVersion`, `gate`, `profileId`, `registeredStory`, `producerPath`,
`helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`,
`commandContract`, `cleanupRequired`, `reviewRolePolicy`, `sourceCommit`,
`policyId`, `policySha256`, `ownerPrincipal`, `assuranceMode`.

Every inventory field matches the selected reinspected C1.15 entry; the source
commit matches the owner run, capture and all nineteen source snapshots. The
four owner fields match the run. The statement excludes `registrationReceipt`
to avoid a circular digest. `cleanupRequired` is strictly `false` in both the
entry and statement. Its `commandContract` and `reviewRolePolicy` Refs must
equal the selected entry's Refs exactly, including their paths.

`owner-command-contract/v1` contains exactly seven fields:

`schemaVersion`, `gate`, `profileId`, `producerPath`, `executable`, `purposes`,
`readOnly`.

Gate/profile/producer match the entry; executable is `kubectl`, and `readOnly`
is strictly `true`. Purposes are exactly the six ordered grammar kinds:

```json
["current-context", "lifecycle-pods", "daprd-version", "metadata", "alpha-opt-in", "lifecycle-pods-recheck"]
```

Pod suffixes and repeated kinds belong only to the capture receipts. This owner
schema always applies the strict v2 argv grammar: context, initial selection,
version/metadata/alpha for each observed Pod in its observation order, then final
selection. Command count is `3 * Pod count + 3`. Every argument, fixed probe and
Pod/container scope is compared exactly; rehashing foreign argv cannot admit it.

`owner-review-role-policy/v1` contains exactly seven fields:

`schemaVersion`, `gate`, `profileId`, `policyId`, `policySha256`,
`ownerPrincipal`, `assuranceMode`.

Gate/profile match the entry, and the four owner fields match the inspected run.
The owner schema does not imply independent review. Fixture role fields such as
`reviewerRole` and `producerExcluded` are outside this closed schema.

The selected registry's three Refs bind the exact retained registration,
command-contract and role-policy lengths and raw SHA-256 values. Their unchanged
Ref shape is exactly `path`, `sha256`, `byteLength`, using canonical relative
paths, lowercase SHA-256 and positive lengths at most 1 MiB. Ref comparison
establishes supplied byte identity and declaration consistency only.

## Source, stream and budget checks

The source comparison reuses the existing normalization and metadata rules:
strict UTF-8, `utf8-crlf-to-lf`, regular Git mode `100644` or `100755`, no clean
filter or working-tree encoding, and `ident: false`. All nineteen producer,
helper and input paths must match the registry/capture inventory exactly, with
no duplicate, missing or substituted sources. Executed SHA-256, normalized
SHA-256 and Git blob SHA-1 are recomputed and compared with every receipt.
Commit labels are compared without authenticating a repository or Git object.

Require an observed clean capture, a clean worktree label, zero failures/skips,
and an unchanged final source recheck. The existing structural reader and pinned
observation inspector remain authoritative. Stream inspection hashes every
untrimmed stdout/stderr byte sequence, applies existing secret/content checks
to every command including later Pods, and compares actual retained output with
the declared observations. The final Pod selection must preserve the initial
name, UID, label and lifecycle/sidecar image identities; reordering unchanged
Pods is allowed. Retained bytes cannot prove that any command actually ran or
that the observations came from the declared target.

Before any reparse or digest work, each supplied JSON artifact, each source byte
form and each stdout/stderr stream is capped at 1 MiB. One call is capped at
32 MiB across all six JSON artifacts, all thirty-eight source byte forms and
every stdout/stderr occurrence. Identical bytes count every time they appear;
there is no deduplication. The exact limits are allowed, and one extra byte
refuses. This aggregate rule is stricter than the legacy source inspector's
unique-byte accounting, whose API and result remain unchanged.

## Frozen result and refusals

`OwnerC115Inspection` exposes these frozen fields:

| Field | Meaning |
| --- | --- |
| `owner_run` | Reinspected `OwnerRunInspection`, with the exact retained run, action `observe`, supplied instant and self-review disclosure. |
| `registration` | Frozen `OwnerRegistrationInspection` containing `source`, `statement`, `command_contract`, and `review_role_policy`. All material snapshots preserve their original bytes. |
| `commands` | Existing `C115CommandInspection` with Pod names, command purposes and argv tuples. |
| `retained_streams` | Existing `C115StreamInspection` with a detached immutable stream tuple preserving every exact purpose/stdout/stderr value and derived Pod/command counts. |
| `sources` | Detached immutable tuple of copied retained source snapshots, with source byte pairs concealed from representations. |
| `retained_byte_count` | Total supplied byte occurrences charged by this call. |
| `owner_principal`, `assurance_mode` | `github:user:6775094` and `sole-owner/self-reviewed`. |

Source, command and stream inspections share the same reparsed capture object
and exact capture bytes. The registration's source inspection retains the
reparsed registry bytes. JSON trees are immutable mappings/tuples, source and
stream collections are tuples, and every result dataclass is frozen. Retained
snapshots, source bytes, streams and argv are concealed from the combined
result's representation. No pass, acceptance or execution flag is returned.
Source/stream records and their tuple containers are copied after preflight and
before inspection. Their immutable byte objects preserve identity and the
occurrence count; later caller field mutation cannot change the retained result.

Failures raise `OwnerC115InspectionError` with bounded content-free codes and
no original exception cause/context. A failure returns no partial combined
inspection. Unsupported inputs, unknown versions, changed approval assertions,
foreign scope, inconsistent Ref/source/argv/output and budget/time violations
all refuse before any forbidden dependency could be called.
Missing required fields on exact dataclass inputs also refuse with
`owner-c115-required-field-missing`, including missing byte fields during budget
preflight. Forged stream purposes must be exact built-in strings before any
comparison, so caller equality callbacks cannot run.

## Reproducible offline verification

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_owner_c115.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
```

The focused fixtures cover same-run positive inspection, all one/two/three-Pod
permutations, immutability, forged caches, exact budget/time boundaries, missing
or withdrawn runs, owner schema/policy changes, and source/Ref/argv/output drift.
Counted barriers deny filesystem, process, network, configuration and clocks
during every inspector case and test representative aliases bound before the
barriers across the composed modules. A counted C-call guard denies native
datetime/date clock methods before execution while preserving their class
identity for closed-type validation. Cross-scope negative evidence is named explicitly in
`test_rehashed_cross_namespace_and_tenant_targets_refuse_before_dependencies`,
`test_rehashed_foreign_session_and_source_commit_refuse`,
`test_rehashed_later_pod_argv_scope_and_fixed_probe_substitutions_refuse`, and
`test_rehashed_later_pod_output_swap_and_final_recheck_drift_refuse`.

These results remain offline verification. Approval of a run, matching owner
materials and passing fixtures supply no deployed registration, gate acceptance,
Production activation or A41 close-out credit.
