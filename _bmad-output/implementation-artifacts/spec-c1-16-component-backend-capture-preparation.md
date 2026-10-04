---
title: 'C1.16 component/backend capture preparation'
type: 'feature'
created: '2026-10-04'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5b43fe2f8a0f04dc021921a077dff1a573c2ce5e'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad/custom/story-scope-guard.md'
  - '{project-root}/_bmad/custom/epic-ac-verification.md'
---

<frozen-after-approval reason="D1-D4 repository implementation explicitly approved 2026-10-04">

## Intent

**Problem:** C1.16 lacks a component/backend collector. Approved D1-D4 permits offline preparation before successor ratification.

**Approach:** Add literal C1.16 and process fixtures on closed historical PG-ONPREM-1. Hold Story 27.22 registration/successor activation until exact PG-ONPREM-2 bytes are ratified.

## Boundaries & Constraints

**Always:** Preserve C1.15 and neutral immutable evidence. Read selected Component fields, authenticated metadata and actual server version/database via local peer authorization. Stable pod UID/image and Component UID/resourceVersion before/after. Advertisements/capture grant no acceptance.

**Never:** Contact targets; adopt pins/profile; register stories; change27.4/A41/history; accept PG-ONPREM-2; retrieve Secret values; add product clients; infer server/approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Complete | Opt-in; matching stable Component/metadata/backend | Immutable observed packet; gate not-evaluated/Production false | Nonzero results |
| Drift | Missing/duplicate/malformed/conflicting identities/capabilities | Blocked; no positive partials | Safe blocker |
| Replacement | Component UID/resourceVersion or pod UID/image changes | Blocked | Drift blocker |
| Unsafe output | Literal/encoded secrets, duplicate JSON, wrong types, excessive output/timeout | Blocked; no secrets | Bounded/decoded checks |
| Denied | No opt-in or unsupported gate/profile including PG-ONPREM-2 | Nonzero; zero calls/no directory | Refuse before dispatch |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1`: reuse bounded process/JSON/secret/pod/immutable helpers; preserve C1.15.
- `tools/verify_access_telemetry_lifecycle.py`: reuse closed image identity, state.postgresql/v2, maxConns40, runtime1.18.1; unchanged.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`: fake-process pattern and regressions.
- `deploy/kubernetes/base/dapr/access-telemetry-store.yaml`: exact Component name/settings. `deploy/kubernetes/base/access-telemetry-postgresql.yaml`: backend selector/image and memories_admin peer map.

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-access-telemetry-c1.ps1` -- add C1.16 and `-AllowHistoricalProfileCapture`; deny absent opt-in before calls/writes.
- [x] `tools/access-telemetry-c1-component-backend.ps1` -- optional focused helper: selected Component API/type/version/scopes/safe settings/reference names; authenticated loaded capabilities ETAG/TRANSACTIONAL/TTL/KEYS_LIKE; actual PostgreSQL18.4/database; stable before/after identities. No Secret values or behavioral credit.
- [x] `tests/tooling/access_telemetry_c1/gate_c1_16_test.py`, `fixtures/c1_16_complete.json` -- every matrix row via temporary-PATH processes.
- [x] This spec -- exact names/results and held registration/profile record.

**Acceptance Criteria:**
- Given matching actual observation fixtures, when the C1.16 command executes, then it captures one separately attributable component/backend packet without granting gate or Production acceptance.

## Dev Notes

### Historical Context Classification

| Influence | Classification | Permitted use |
| --- | --- | --- |
| Approved 2026-08-03 allocation and approved D1-D4 proposal | historical-reference-only | Gate/staging boundary. |
| Broad27.3, withdrawn27.5/27.6, broad31.1 | anti-template | Decision history only. |
| Current27.21 helpers | current-narrow-pattern | Dispatch/bounded neutral capture only. |

### Slice Proof

One C1.16 preparation outcome; role Deployment Adapter Developer; commands below; implementation and independent review complete; 55 tests passed. No registered gate is discharged.

### Epic AC Verification

Creation preflight verified 2026-10-04 against 5b43fe2f8a0f04dc021921a077dff1a573c2ce5e and its observed worktree. The creation-baseline gap below is now intentionally implemented; it is retained as creation-time evidence, not a claim about the final runner.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| "C1.16 — component/backend identity" | Location | `rg -n -F 'C1.16 — component/backend identity' _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md` | One allocated gate,27.22. | confirmed |
| "The runner accepts only literal C1.15" | Behavior | `git show 5b43fe2f8a0f04dc021921a077dff1a573c2ce5e:tools/verify-access-telemetry-c1.ps1 | rg -n -F "ValidateSet('C1.15')"` | Creation baseline accepts only15; desired missing16 is implementation gap. | confirmed |
| "PG-ONPREM-1" is the only supported profile | Behavior | `rg -n -F "ValidateSet('PG-ONPREM-1')" tools/verify-access-telemetry-c1.ps1` | Exact successor bytes await ratification. | confirmed |

Implementation recheck 2026-10-04 against the changed worktree:

| Epic claim | Class | Command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| C1.16 permits offline historical preparation only | Behavior | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p gate_c1_16_test.py -k opt_in -v` | Literal C1.16 requires the historical opt-in; successor/unsupported modes make zero calls and create no directory. | confirmed |
| C1.16 advertisements/capture grant no acceptance | Behavior | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p gate_c1_16_test.py -k complete -v` | Separate immutable observed packets retain not-evaluated gate/behavior and Production false. | confirmed |

## Implementation Notes

Implemented 2026-10-04 as offline preparation on closed historical `PG-ONPREM-1`.
The explicit invocation is:

```powershell
pwsh tools/verify-access-telemetry-c1.ps1 -Gate C1.16 -ProfileId PG-ONPREM-1 -AllowHistoricalProfileCapture -EvidenceDirectory artifacts/access-telemetry-c1/C1.16
```

This command is documented for later separately authorized execution; implementation
and verification used temporary-PATH processes. The command-boundary test also
executes the real kubectl Go template against a loopback-only fixture API, with an
explicit isolated kubeconfig/cache, and runs the actual shell/env probes against
fake wget/psql. No real qualification target or user kubeconfig is used. Missing opt-in, unsupported
gates, and `PG-ONPREM-2` are denied before external calls or directory creation.

- `tools/verify-access-telemetry-c1.ps1` dispatches the literal C1.16, adds the
  historical opt-in, and retains the C1.15 packet naming, capture path, and legacy
  case-insensitive gate/profile inputs. Literal gate/profile validation is confined
  to C1.16 before opt-in or dispatch.
- `tools/access-telemetry-c1-component-backend.ps1` projects Component API/kind,
  namespace/name, UID/resourceVersion, state type/version, init timeout, scope,
  seven known safe settings and reference names/key names. It never projects an
  inline connection string or any Secret resource. It reads authenticated loaded
  `ETAG`, `TRANSACTIONAL`, `TTL`, `KEYS_LIKE` advertisements and actual
  `server_version`, `server_version_num`, `current_database()`, `current_user` and
  `system_user` through read-only `psql` on the local socket with password inputs
  disabled. It requires `systemUser: peer:postgres`; a socket alone cannot prove
  peer authentication.
  Stable Component and lifecycle/backend pod identities are rechecked before any
  observation becomes positive. Unicode-escaped credentials on either process
  stream, discarded decoded fields, duplicate JSON names, wrong types, excessive
  output and timeouts remain blockers.
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py` and
  `fixtures/c1_16_complete.json` use temporary-PATH processes to exercise every
  matrix row, multi-pod attribution/conflicts and index/child image representation.
  `runtime_control_plane_identity_test.py` adds the legacy lowercase C1.15 input
  regression against its original fixture. Selected Pod phase, condition type,
  and container name require raw strings before any PowerShell comparison can
  coerce booleans into positive matches.
- Each immutable `c1.16-component-backend-identity-<timestamp>-<capture-id>.json`
  packet retains producer/source/command attribution, historical disposition,
  `gateStatus: not-evaluated`, `productionGatePassed: false`, and
  `componentBehavior: not-evaluated`. Blocked packets contain no positive partial
  Component, pod, loaded-capability or server observations.

The closed PostgreSQL image index SHA-256
`3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a`
has exactly one `linux/amd64` child descriptor,
`sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf`.
Both are authenticated representations of the existing closed image, recorded in
[retained registry response bytes](../planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/registry-source-evidence.json).
Acquisition provenance was independently rechecked from
`/tmp/c1-security-artifact-research-x3j5njvt/postgres-current.json` by hashing the
raw response bytes and selecting its unique exact Linux/amd64 platform descriptor.
The collector accepts these two digests only; a raw `imageID` change between
snapshots still blocks, including an index-to-child change. This does not adopt a
new image or profile.

**Held:** Story 27.22 remains unregistered. Successor activation and acceptance
remain held until the exact authenticated `PG-ONPREM-2` bytes are ratified.
No C1.16 gate, Production acceptance, Story 27.4 completion, A41 disposition,
or historical C1.15 evidence was granted or changed.

## Spec Change Log

- 2026-10-04: Recorded the implementation and closed-index/child identity
  representation; no frozen intent or approval boundary changed.
- 2026-10-04: Corrected the scope verification command because the CLI discovers
  numeric story filenames only. The requested original CLI invocation with this
  actual spec path returned `story-slice-scope: no governed story file changed.`
  Its exit 0 checked zero files and is not a pass. Direct `check_story` below
  validates the same creation record against this freeform preparation spec; it
  passed with `story-slice-scope: OK - 1 file checked` on 2026-10-04.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | Matching selected Component/metadata/backend identities do not establish their actual connection linkage; record that attribution limit in the planning handoff, without inferring gate credit. |
| B2 | medium | patch | Array/object credential fields survive the raw string-only regex and recursive scan; the demonstrated discarded password/token shapes must block before provenance. |
| B3 | medium | defer | The shared existing pod identity pattern records UID/image but not container incarnation; a restart can span captures. The pre-existing C1.15 pattern has the same limit; retain this as future qualification coherence work, with no acceptance claimed here. |
| B4 | medium | defer | The metadata shell buffer is unbounded before local capture; the exact same wget command substitution exists in unchanged C1.15 at runner line628. Future shared-probe hardening must bound the remote allocation. |
| B5 | medium | patch | Temporary-PATH fake kubectl checks substrings but does not execute the Component template or shell/env probes; add isolated offline command-boundary fixtures. |
| B6 | medium | patch | All candidate checks are asserts, so optimization removes them and corrupted bytes can print PASS. Replace them with explicit failures. |
| B7 | medium | patch | Archive-internal descriptor consistency is not bound to the canonical image/chart identities; demonstrated recomputed alternative chains pass. Compare hashes with canonical pins and receipt identities. |
| B8 | medium | patch | Default JSON decoding accepts duplicate package/registry fields, including contradictory activation flags. Decode with duplicate rejection. |
| B9 | medium | patch | Unsupported proposal version and wrong profile alias pass because wrapper fields are unchecked; enforce the exact envelope/version/alias. |
| B10 | medium | patch | Historical Dapr claims depend on an unretained local packet. Its 4429 bytes match the cited hash, but package replay cannot read them; retain exact secret-safe bytes separately and verify their observed identities. |
| E1 | medium | patch | Confirmed same credential array/object gap as B2, from the edge fixtures; raw values reach observed capture without a blocker. |
| E2 | medium | patch | Confirmed same optimized-assert bypass as B6 using corrupted size; checks must survive -O and PYTHONOPTIMIZE. |
| E3 | medium | patch | Local socket/current_user do not distinguish trust from peer. PostgreSQL 18 system_user exposes auth method/identity; require actual peer:postgres and reject missing/trust/other identities. |
| V1 | medium | patch | Pre-verified mutation of maxConns selector still passes the complete test because the actual Go template never executes. Add raw-Component offline projection verification. |
| V2 | medium | patch | Pre-verified deletion of backend observation Add-SourceHash still passes complete capture; assert exactly one backend entry and its independently computed content hash. |

### Review Resolution

Every finding above retains its independent verdict and route. After triage,
shared root causes were patched together. The final 55-method suite passed every
regression below. Exact raw logs and source hashes are retained in
[final verification evidence](../planning-artifacts/c1-security-prerequisites-2026-10-04/final-verification-evidence.json).

| Findings | Resolution and executed evidence |
| --- | --- |
| B1 | The implementation handoff explicitly records that independent Component/metadata/backend observations do not prove actual connection linkage; no inferred behavioral or gate credit. |
| B2, E1 | Nonempty credential-named fields of all JSON types block before provenance, including prefixed stderr and overwritten duplicates. `test_nonempty_credential_fields_of_every_json_type_block_before_provenance` passed. |
| B3 | Deferred as an inherited shared C1.15/C1.16 limitation; appended the container-incarnation entry to the ledger without changing prior entries. Current neutral capture grants no qualification continuity. |
| B4 | Deferred as an inherited shared remote-buffer limit; appended the bounded-remote-reader entry without changing prior entries. No remote size qualification is claimed. |
| B5, V1 | The actual Go template executes against a loopback-only raw Component fixture and its complete projection is compared. Actual shell/env probes execute with fake wget/psql. `test_real_go_template_and_shell_env_boundaries_execute_offline` passed. |
| B6, E2 | Explicit verification failures survive normal and optimized Python. Six candidate-verifier methods and both standalone checker modes passed. |
| B7 | Archived descriptor chains bind to canonical pins and acquisition receipts. Self-consistent alternative image/chart chains and receipt drift were rejected by the corresponding candidate-verifier methods. |
| B8 | Strict JSON decoding rejects duplicate properties in package and archived config bytes; the duplicate-properties candidate-verifier method passed. |
| B9 | Exact envelope/version/alias and inactive state are required; the malformed-envelope candidate-verifier method passed under optimization too. |
| B10 | Exact 4,429-byte historical packet is durably retained in base64 and verified without the workstation path. Replay and retained-packet integrity/observation methods passed; historical bytes are unchanged. |
| E3 | Actual PostgreSQL `system_user` must equal `peer:postgres`. `test_backend_system_user_rejects_trust_null_missing_wrong_identity_and_types` passed. |
| V2 | Complete capture asserts exactly one backend source entry and independently recomputes its SHA-256; `test_complete_capture_is_separate_immutable_and_neutral` passed. |

## Verification

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` -- 55 tests passed in 833.295s after every review patch, with zero failures/errors/skips: 34 C1.15 methods, 15 C1.16 methods and 6 candidate-verifier methods. [Exact final raw logs and source hashes](../planning-artifacts/c1-security-prerequisites-2026-10-04/final-verification-evidence.json) are retained. Repository line endings were then normalized for the C1.16 Python test and earlier text audit; content lines and the Python AST are identical, with both hashes recorded.
The creation-record check for this freeform preparation spec is:

```bash
python3 - <<'PYCHECK'
import runpy
from pathlib import Path

path = '_bmad-output/implementation-artifacts/spec-c1-16-component-backend-capture-preparation.md'
checker = runpy.run_path('tools/check-story-slice-scope.py')
violations = checker['check_story'](
    path, Path(path).read_text(encoding='utf-8'),
    'c1-16-component-backend-capture-preparation', registered=True,
)
assert not violations, '\n'.join(v.render() for v in violations)
print(f'story-slice-scope: OK - 1 file checked: {path}')
PYCHECK
```

- `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -k opt_in -k legacy_lowercase -k complete_fixture -k complete_capture -k initial_missing -v` -- 5 tests passed in 76.303s, no failures/errors/skips, after every review patch. This includes both complete captures, zero-call C1.16 denials, raw-type Pod negatives and preserved lowercase C1.15 behavior.
- `git diff --check` -- clean whitespace.

### Matrix Audit

The [final verification evidence](../planning-artifacts/c1-security-prerequisites-2026-10-04/final-verification-evidence.json)
retains exact original raw logs in base64, hashes, all 55 executed method names
and the separate final five-method check. Decode before hashing. The earlier
[post-execution audit](../planning-artifacts/c1-security-prerequisites-2026-10-04/capture-verification-audit.txt)
remains an explicitly reconstructed historical report, not a raw process log.
The parent read the actual code/fixtures and matched every matrix row below to
the final passing C1.16 methods; all 15 ran without skips.

| Matrix row | Passing C1.16 method coverage |
| --- | --- |
| Complete | `test_complete_capture_is_separate_immutable_and_neutral` (also final focused check) |
| Drift | `test_component_missing_duplicate_wrong_type_and_conflicting_identity_block`, `test_authenticated_loaded_component_missing_duplicate_and_capability_drift_block`, `test_backend_actual_server_database_and_local_peer_identity_must_match`, `test_backend_system_user_rejects_trust_null_missing_wrong_identity_and_types`, initial-pod and multi-pod methods |
| Replacement | `test_component_and_pod_replacements_or_image_changes_block_all_observations`, `test_unreviewed_backend_digest_and_index_to_child_change_block` |
| Unsafe output | `test_json_duplicate_properties_wrong_roots_and_secret_fields_block_before_projection`, `test_literal_credentials_process_failures_timeouts_and_output_bounds_are_safe`; `test_nonempty_credential_fields_of_every_json_type_block_before_provenance`; initial-pod method covers boolean discriminator negatives |
| Denied | `test_opt_in_and_exact_gate_profile_denials_precede_calls_and_directory_creation` (also final focused check) |

`test_real_go_template_and_shell_env_boundaries_execute_offline` executes the
actual selected Component template and shell/env boundaries. The authenticated
historical child-image method verifies the reviewed alternative image identity.

All execution tasks are complete. This audit grants preparation coverage only;
registration and actual successor gate evidence remain held.

### Completion Boundary

This freeform preparation spec is done; its story key is empty. No registered
story or sprint-status row is advanced. Story 27.22 remains held and the frozen
Story 27.4 operator spec remains awaiting-operator. No stage, commit, push or
branch operation was performed: the user-authorized repository preparation
retains the explicit Git-publication boundary.
