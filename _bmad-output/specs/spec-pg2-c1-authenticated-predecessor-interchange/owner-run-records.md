## Current Platform ownership applicability — approved 2026-10-16

The owner approved Platform-owned local functional infrastructure and the existing Builds runner boundary in [Spec 14](../../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md); its [single report](../../implementation-artifacts/tests/27-4-retention-verification-evidence.md) records current blockers. No supported functional descriptor/result consumer exists yet. No replacement schema, VM/hosted fallback or KVM-hash reuse is admitted. Full/legacy wire contracts below remain strict; local functional evidence supplies no C1/C0–C6, full terminal or A41 credit. Prior approvals/budgets remain unchanged history.

# Owner run records: sole-owner qualification

**Current execution boundary, 2026-10-16:** [Spec 14](../../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) owns the approved plan and checkpoint table. Further per-gate story files are not required; implemented bindings, semantic observations, distinct evidence and review still are. This technical contract creates no execution permission. Functional packets are outside full C1 admission; the stronger/historical protocol below remains strict for its own version.

Policy: `sole-owner-qualification/2026-10-12`, approved by the owner on
2026-10-12. This document defines the new qualification mode. It does not issue a
run approval or establish that a live target or private directory exists.

## Trust and scope

The owner is the selected policy principal `github:user:6775094`. This is a
project policy identifier, not a new GitHub-authentication observation. Retain the
owner's explicit approval of the exact policy and run plan in project records.
Record producer/reviewer overlap as `sole-owner/self-reviewed`; never substitute
an alias, agent or fabricated independent principal.

The caller obtains the approved record/policy digest and target/session/source/
custody anchors from the retained owner-approved plan, outside the untrusted
record being inspected. Re-read the current record and withdrawal state before
each use. A matching hash detects drift; it does not supply independent authority.
This owner-controlled boundary is intentionally weaker than the old P1 provider.

The first implementation slice inspects a bounded owner record only. It performs
no filesystem, network, process or target calls. Its immutable result is not an
accepted gate, predecessor or execution handle. Complete registry/semantic/
custody checks and consumer migration remain required before execution.

## Closed record shape

`schemaVersion` is exactly `hexalith.access-telemetry.c1.owner-run/v1`.
All these fields are required; no extra field is allowed:

| Field | Required value |
| :--- | :--- |
| `policyId` | `sole-owner-qualification/2026-10-12` |
| `policySha256` | Exact approved policy SHA-256, lower-case 64 hex |
| `ownerPrincipal` | `github:user:6775094` |
| `assuranceMode` | `sole-owner/self-reviewed` |
| `environment` | `non-production` |
| `profileId` | `PG-ONPREM-2` |
| `profileSha256` | `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb` |
| `workloadSha256` | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| `sessionId` | Non-secret ASCII identifier, 1–128 characters from letters, digits, dot, underscore or hyphen, first character alphanumeric; equals the approved plan |
| `sourceCommit` | Exact full lower-case 40-hex commit in the approved plan |
| `targetSha256` | Exact lower-case 64-hex observed-target digest in the approved plan |
| `custodyRoot` | Exact approved absolute canonical POSIX path; no empty, dot or dot-dot segments, trailing slash, backslash or NUL; not filesystem root |
| `actions` | Nonempty unique array from `observe`, `fault`, `purge`, `reclaim`, `close-out`, `publish`; contains the requested action |
| `notBeforeUtc` | UTC millisecond timestamp `YYYY-MM-DDTHH:mm:ss.fffZ` |
| `collectionEndsAtUtc` | Same timestamp format; after `notBeforeUtc`, collection window at most 72 hours |
| `reviewEndsAtUtc` | Same timestamp format; after collection end, review/use extension at most 24 hours |
| `expiresAtUtc` | Same timestamp format; at or after review end and at most 7 days after issue/start |
| `status` | `approved`; rejected/pending/withdrawn records refuse |

Reuse `JsonSnapshot` parsing/bounds and decoded secret-safety checks. Inspect exact
raw bytes against the caller's approved record digest. Reject booleans where
strings or time values are required, unknown/missing fields, malformed digests,
noncanonical scope and secret-shaped values, without echoing untrusted content.

Caller supplies aware UTC `now` using the immutable built-in `datetime.timezone`
with zero UTC offset, a real boolean `withdrawn` current status and
the requested action. Unknown/non-boolean withdrawal status refuses. Use must be
at or after `notBeforeUtc` and strictly before the earlier review end/expiry.
`observe`, `fault`, `purge` and `reclaim` also require current time strictly before
collection end. `close-out` and `publish` require current time at or after collection
end. An action is permitted only if explicitly included in the approved record;
none implies another. Local approval time does not replace qualified lifecycle
clock evidence. Runtime gate/Lease bounds remain unchanged.

The full plan also names concrete resource/action selectors, impact, budgets and
cleanup. Those facts are not established by this inspection-only record. Do not
expand this first slice into a permissive wildcard resource or gate authorizer.

## Later gate and result acceptance

Keep all 25 gate-specific artifacts and actual semantic checks. The owner accepts
the C1 result summary; tools retain separately bound gate and role records in
acyclic order. C1.25 records owner security risk acceptance. C5/C6 remain distinct
result records accepted by the same owner. These records disclose self-review;
their detailed wire format and complete assembly are later implementation work.

Private custody must be owner-controlled and safely read below the approved root;
retain exclusive per-run files, exact-byte readback and hash manifests. Missing
permission/traversal/readback evidence refuses complete assembly. This mode makes
no independent immutability, off-site or certified-retention claim.

## Plan, policy and local status binding

The canonical owner policy source is
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-12-sole-owner-qualification.md`.
At plan preparation, the agent retains its exact approved bytes and hashes those
bytes, including their CRLF line endings, as `policySha256`. Do not hash the
LF-normalized Git blob or choose another document with the same policy ID. A later
source edit cannot replace a retained policy snapshot under the existing hash.

The full approved plan and the closed inspection record are separate artifacts.
The full plan names actual resources, allowed verbs/actions, cleanup and evidence
retention and binds the exact inspection record SHA-256. Owner approval pins the
full plan's exact bytes and SHA-256 outside the inspection record. Later consumers
must verify that plan approval and its record reference before deriving the six
`OwnerRunAnchors` fields; never derive them from the candidate alone. The current
pure inspector does not implement that plan reader or complete execution admission.

## Offline owner-plan dispatcher

[`dispatch_owner_run`](../../../tools/access_telemetry_c1_owner_dispatch.py) accepts
the current record `JsonSnapshot`, exact retained `plan_bytes`, the outside
`approved_plan_sha256`, aware UTC `now` and one `action`. The caller must re-read
the current record before every dispatch; the dispatcher receives bytes and has
no record-file location from which to establish their freshness. It recomputes
the record digest and derives `OwnerRunAnchors` only after the outside plan digest
matches the retained plan bytes.

The plan requires seven bindings: `recordSha256`, `policySha256`, `sessionId`,
`sourceCommit`, `targetSha256`, `custodyRoot` and `statusPath`. The first six use
the closed types and canonical spellings defined above. `statusPath` must already
be an absolute canonical POSIX path strictly below `custodyRoot`; resolving to a
different path is refused. Other plan fields, including resource selectors,
verbs and cleanup, are ignored as non-authority by this dispatcher.

The status object has exactly four fields: `planSha256`, `recordSha256`,
`sessionId` and `withdrawn`. Its first three values must match the retained plan
digest, bound record digest and session; `withdrawn` must be a real JSON boolean.
Every dispatch reads this file anew immediately before inspection. Missing,
unreadable, malformed, mismatched or non-boolean state refuses with no inspection
and no cached `False`. A matching `true` preserves the inspector's withdrawal
refusal; a matching `false` still requires that action's record and time checks.

The filesystem must support POSIX directory descriptors, non-following opens,
non-following descriptor-relative metadata checks and nonblocking file opens.
Unavailable or unsupported operations refuse. Every ancestor and the status file
must be free of symlinks, and the status file must be regular. Reads consume at
most `MAX_ARTIFACT_BYTES + 1` bytes. The reader checks stable file metadata before
and after reading, then revalidates the named file and its ancestor chain without
following an outside target. Removal, replacement, in-place modification or
ancestor movement observed during reading refuses. Refusals expose only a code,
without internal filesystem/parser exception cause or context data.

Status writers must write a complete document to a separate file in the same
directory as `statusPath`, under the private custody root, and atomically replace
`statusPath`; do not truncate or rewrite the current file in place. A replacement overlapping a dispatch refuses;
the next dispatch reads the new named file. These checks produce only the existing
immutable `OwnerRunInspection`, labelled `sole-owner/self-reviewed`. They create
no execution handle, accepted gate, checkpoint, publication or A41 close-out.
The pure `inspect_owner_run` remains free of I/O, and live consumers remain closed.

## Offline current-record and private-custody wrapper

[`dispatch_owner_run_files`](../../../tools/access_telemetry_c1_owner_custody.py)
has the API `dispatch_owner_run_files(plan_path, approved_plan_sha256, *,
custody_root, now, action)`. Supply absolute canonical POSIX strings for
`plan_path` and `custody_root`, the exact plan digest retained outside the files,
and the same aware UTC `now` and single `action` used by the byte dispatcher.
This wrapper freshly reads all three files on every call. It returns only the
existing immutable `OwnerRunInspection`; it adds no execution flag or authority.

The wrapper requires the plan's additional `recordPath` binding. The existing
`dispatch_owner_run` byte API still consumes its original seven bindings and
requires callers to provide current record bytes. The wrapper's plan path and
its approved `recordPath` and `statusPath` must be distinct canonical paths
strictly below the caller's custody root. The retained plan's `custodyRoot` must
match that caller root exactly. The wrapper pins the plan's exact file bytes to
`approved_plan_sha256` before reading its record or status locations, recomputes
the record digest, and reuses the byte dispatcher's plan/status, policy, PG2,
self-review, action and window checks. Other full-plan fields still grant nothing.

The three named artifacts and their directory chains at/below the custody root,
including that root, must belong to the effective OS user and have no group/other
permission bits. The wrapper does not enumerate other custody descendants. Typical
private modes are `0700` for directories and `0600` for files. The named files must
be regular and single-link and have distinct device/inode identities. Every path
component, including ancestors above custody, is opened without following
symlinks. Ancestors above custody need not be private, but their named identities
are still held and checked. OS ownership establishes local custody only; it does
not authenticate `github:user:6775094` or replace retained owner approval.

The initial read and final readback each consume at most `MAX_ARTIFACT_BYTES + 1`
bytes per artifact; only documents of at most `MAX_ARTIFACT_BYTES` are accepted.
The wrapper checks stable size, ownership, permissions and file link count; file
modification/change times and private-directory change times must be available as
integer nanoseconds. Missing/wrong-type required timestamps refuse. It keeps the
plan, record, status and ancestor descriptors through dispatch. Before returning,
it seeks and rereads the held plan, record and finally status, comparing each
bounded exact-byte readback to its originally inspected bytes while revalidating
the named file and ancestor metadata. The byte comparison detects same-size
rewrites even when timestamps repeat; checking status last detects withdrawal
during the plan or record final checks. Replacement, removal,
rewrite, custody permission/owner changes or ancestor movement observed during
reading or dispatch refuses with no inspection. Unrelated file/directory creation
or removal above custody does not invalidate unchanged custody inputs. Directory
identity checks compare device/inode, mode and UID/GID without directory link-count
churn; the named files still require a single link. Filesystem
operations that cannot provide these checks refuse. `OwnerRunCustodyError`
exposes only a content-free `owner-custody-...` code after cleanup and internal
handlers finish, without an internal cause/context chain. Existing inspector
refusals retain their `OwnerRunInspectionError` type and code with the same
sanitization.

Writers prepare complete private files separately and atomically replace the
named file; never rewrite current files in place. A concurrent dispatch may
refuse, and any retry must perform a new complete call. Changed plan or record
bytes require their updated exact approved plan binding and matching status.
No bytes or withdrawal value survive between wrapper calls; a prior inspection
cannot authorize a later use. These sequential byte and metadata checks establish
local point-in-time evidence ending with status readback/validation. They do not
provide a simultaneous filesystem snapshot, filesystem immutability or a
continuing lock after return.

Complete gate-specific registry/semantic acceptance, collectors, consumer
migration, approved target/resource actions, lifecycle/Lease/clock evidence,
bounded failure restoration, result acceptance and terminal/publication checks
remain required before live admission. This wrapper contacts no target,
registers no collector, completes no gate or checkpoint, publishes nothing and
does not close Story 27.4 or A41. Production configuration and live consumers
remain unchanged.

## Ordered owner acceptance

Preserve the already selected acyclic parent sets from the P4 decision packet:
C1.24 follows C1.17, C1.18, C1.19, C1.21 and C1.22; C1.23 follows C1.1–C1.22
plus C1.24; C1.25 follows C1.1–C1.24. The owner reviews the technical evidence and
explicit non-HA, Operations and security-risk statements. Tools retain separately
bound owner records in that order. Only then freeze the numeric-order 25-gate
manifest and derive its distinct role records under the approved deterministic
policy. Neither the approval-gate records nor their parent subjects may include
their own decision or the later manifest. Role records must disclose their source
owner acceptance and bind exact manifest bytes; they do not claim independent
human review. Detailed decision/assembly wire and execution remain later I3/I5 work.

## Failure restoration and evidence availability

Expiry or withdrawal stops qualification measurements and prevents new fault,
purge or reclamation work. The full plan must also preapprove a separate bounded
abort/restoration scope for owned resources: disable the gate, release only the
run's Lease and restore only its lifecycle/clock replica state. That restoration
is a failure-finalization obligation, not a continuation authorized by this record
inspector. It cannot delete retained telemetry, extend qualification, act on a
replacement owner's resources or be inferred from an `observe` approval. The
collector/consumer must implement and test that finalization before live admission.

Retain private evidence through owner acceptance, terminal checks and remote
publication verification. Resource rollback/cleanup must not remove evidence.
After publication, keep the run in the owner's project evidence archive; removing
that archive is an intentional decommission/archive decision, with its evidence
availability consequence disclosed. No external immutability, 365-day retention
or off-site guarantee is asserted by this local policy.
