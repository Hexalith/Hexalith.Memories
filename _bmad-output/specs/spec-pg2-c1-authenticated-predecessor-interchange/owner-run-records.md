# Owner run records: sole-owner qualification

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
| `profileSha256` | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
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

The later local-status reader must use the private current-state location selected
by the approved plan, keyed to the exact plan digest, record digest and session.
Re-read it immediately before every use; no cached `False` is current proof.
Missing, mismatched or unavailable state is unknown and must refuse, rather than
being passed as `withdrawn=False`. The pure inspector accepts an already checked
caller fact; it does not claim to have performed this filesystem/status verification.

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
