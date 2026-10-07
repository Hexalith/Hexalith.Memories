# Wire schemas and validation contract

Existing C1.15 capture/disposition shapes are adopted from the operations contract.
The accepted-gate, manifest, bundle-approval and predecessor shapes below are
**proposed**, with no registered producer or implemented authority implied.

## Common types and encoding

| Type / rule | Exact proposed requirement |
| --- | --- |
| `Ref` | Object with exactly `path`, `sha256`, `byteLength`; canonical custody-root-relative POSIX path, lowercase 64-hex SHA-256, positive integer byte length. No absolute paths, backslashes, empty/dot/dot-dot segments, aliases or symlink components. |
| Digest | Lowercase 64-hex SHA-256; do not trim, case-fold or accept prefixed equivalents. |
| Commit | Full canonical lowercase 40-hex commit for this SHA-1 repository, resolved to that exact commit in the approved source repository. A Git object from another repository does not establish source ownership. |
| Session label | Exact ASCII `\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z`, secret-safe; a correlation identifier, never authorization by itself. |
| Identity / role | Bounded string mapped to an opaque stable authority principal or approved role by P1/P4. No display-name, email, Git-author or local-user fallback. |
| New-schema timestamp | UTC `YYYY-MM-DDTHH:mm:ss.fffZ`, parsed and ordered as an instant. Existing v2 capture retains documented round-trip `+00:00`; convert instants only for comparisons, never alter its retained bytes. |
| Counts | Integers, never booleans, floats or number strings; nonnegative signed-64-bit range. Successful overall results and required observation results are positive; failures/skips are zero. |
| Bounds | Propose 1 MiB per artifact, maximum JSON depth 14, maximum ordinary string 4096 characters, path 512, identity/role 256. Aggregate unique retained JSON bytes read per validation: at most 32 MiB. Refuse before exceeding budgets. Larger approved evidence requires a reviewed schema/budget revision. |
| Strict parse | UTF-8 without BOM; reject invalid encoding, duplicate/unknown/missing fields at every object, nonfinite numbers, comments, trailing commas, incorrect types and extra JSON values. Apply field-aware decoded secret/content checks before admission. |

Retained-artifact digests hash **exact bytes**, including newline and original
encoding. Receipt digests do the same. Read, hash and parse one bounded snapshot;
do not hash one path read and validate another.

For new semantic hashes only, **J1** means UTF-8 compact JSON, recursively
lexicographically sorted ASCII field names, preserved array order, no whitespace,
literal non-ASCII text, standard JSON escaping with lowercase `\u00xx` control
escapes, and integers/booleans/null/strings/objects/arrays only. No Unicode
normalization or float formatting is performed. Cross-language golden vectors
must cover escaping, non-ASCII strings, arrays and integer bounds.

Existing workload hashing retains sorted compact canonical JSON. Existing v2
capture argument/target hashes retain the documented PowerShell compact JSON,
default escaping and insertion order. Implement its exact encoding and verify
golden vectors; do not substitute J1 or a Python serializer silently.

## Capture: `hexalith.access-telemetry.c1.evidence/v2`

For **C1.15/PG2 only**, the closed top-level set remains:

`schemaVersion`, `gate`, `profileId`, `capturedAtUtc`, `context`, `namespace`,
`targetSelector`, `producerStatus`, `gateStatus`, `productionGatePassed`,
`productionLifecycleWrites`, `observations`, `blockers`, `sources`, `commands`,
`profileIdentity`, `profileSha256`, `workloadId`, `workloadSha256`,
`qualificationSessionId`, `target`, `targetSha256`, `producer`, `sourceCommands`,
`sourceCommit`, `sourceDisposition`, `worktreeDirty`, `producerSources`,
`finalSourceRecheck`, `resultCount`, `failureCount`, `skipCount`,
`independentDisposition`.

The adopted operations contract defines every nested field, observation shape,
source byte/Git-normalization rule and command hash. No fields are added to its
capture to imply operator authentication. The verifier requires:

- Exact approved profile/workload/runtime/image/target pins, positive Pod observations, `producerStatus: observed`, no blockers, zero failures/skips, successful complete command and source receipts, and stable final source recheck.
- `sourceDisposition: clean`, `worktreeDirty: false`, `finalSourceRecheck: unchanged`, and each of the three PowerShell sources plus sixteen approved inputs exactly once, tracked and matching approved full HEAD. Match executed bytes separately from Git-normalized bytes; CRLF working bytes need not equal LF blob bytes.
- Exact producer and child argument arrays, documented hashes, intervals within the producer interval, successful exits, complete safe stream hashes and semantically justified nonzero results. Git query counts cannot substitute for target observation counts.
- The actual selected Pod/runtime/image observations satisfy the gate-specific verifier, rather than trusting a `resultCount` or asserted pass. Missing stdout bytes limit what can be independently recomputed; retained stream hashes alone are not replay proof. P3 must settle custody of supporting source/command evidence.

All captures retain `gateStatus: not-evaluated`, `productionGatePassed: false`,
`productionLifecycleWrites: not-evaluated`, `independentDisposition: pending` and
`producer.identityAuthentication: not-evaluated`. Those expected neutral fields do
not invalidate an otherwise sound capture, but cannot make it accepted. Acceptance
requires a separate verified disposition and authorized session/producer identity.

Other gates require their own approved versioned capture and semantic verifier.
Do not apply C1.15 Pod observations to another gate or extend v2 by accepting
arbitrary `observations`. Legacy C1.16 identity/linkage evidence stays retained;
its missing interchange facts require P6 resolution or fresh evidence.

## Disposition: `hexalith.access-telemetry.c1.disposition/v1`

Closed top-level set, unchanged from the adopted contract:

`schemaVersion`, `capture`, `gate`, `profileId`, `profileSha256`,
`workloadSha256`, `qualificationSessionId`, `targetSha256`, `producerPrincipal`,
`reviewerPrincipal`, `reviewerRole`, `independence`, `decision`, `reasons`,
`decidedAtUtc`, `expiresAtUtc`, `authorityReceipt`.

`capture` is `Ref`. `authorityReceipt` has exactly `uri`, `sha256`, `issuer`,
`decisionId`, all bounded strings, with a digest in `sha256`. The URI is retrievable
only through the selected authority adapter and approved origin/custody policy;
it must contain no credentials or credential-bearing query. It is not a trust
root. P1 must fix receipt wire format and URI rules before implementation approval.

`decision` is exactly `accepted`, `rejected` or `needs-evidence`; `reasons` is a
nonempty array of bounded secret-safe substantive strings. For acceptance,
`independence` is boolean true and producer/reviewer are independently authenticated
different principals. The actual authorized review role is required, without a
default role invented here. Decision and expiry are ordered UTC instants under
the adopted contract; P2 fixes permitted precision and windows for disposition
issuance. Authentication verifies every bound fact, including reasons, against
the retained receipt. A supplied `independence: true` is not proof.

## Accepted gate: `hexalith.access-telemetry.c1.accepted-gate/v1`

Proposed exact fields:

`schemaVersion`, `gate`, `status`, `profileId`, `profileSha256`, `workloadSha256`,
`qualificationSessionId`, `targetSha256`, `registryEntrySha256`, `capture`,
`disposition`, `sourceCommit`, `producerPath`, `startedAtUtc`, `finishedAtUtc`,
`resultCount`, `failureCount`, `skipCount`, `cleanup`.

`capture` and `disposition` are `Ref`; `status` is exactly `passed`. Identity,
source, interval and count fields are derived only after validating those
artifacts and must agree with them. The record is not a caller's new assertion of
acceptance. `registryEntrySha256` hashes J1 of the approved entry described in
[producer-bindings.md](producer-bindings.md).

`cleanup` has exactly `required`, `receipts`, `finalState`: a boolean, array of
distinct `Ref`, and one of `read-only-no-owned-mutation` or
`restored-approved-baseline`. The registry and verified command grammar decide
whether cleanup is required. Read-only captures have false, an empty array and
the first state. Mutating gates require true, nonempty successfully verified
receipts, and the second state. Receipt schemas and approved initial/final
resource states come from each gate's owning contract; absence refuses. A claimed
restore, attempted cleanup or unrelated final state is insufficient.

The verifier reads each capture and disposition, revalidates embedded provenance,
checks approval and cleanup, then constructs this record. Gates cannot reuse
another gate's accepted artifact, capture or disposition path/hash.

## Manifest: `hexalith.access-telemetry.c1.manifest/v1`

Proposed exact fields:

`schemaVersion`, `checkpoint`, `profileId`, `profileSha256`, `workloadSha256`,
`qualificationSessionId`, `targetSha256`, `registrySha256`, `policySha256`,
`sessionAuthority`, `gates`.

`checkpoint` is exactly `C1`. `sessionAuthority` is `Ref` to retained authority
evidence interpreted by the selected adapter; its issuer-specific wire schema is
P1/P3, not invented here. `gates` is an ordered array with exactly C1.1 through
C1.25, numerically ordered, each object exactly `gate`, `artifact`, where
`artifact` is `Ref` to one accepted-gate record. Registry/policy semantic hashes
use J1. All gate scope fields match the manifest; each approved producer may have
its own explicitly authorized full source commit. No requirement to fabricate a
single shared commit across captures.

Freeze manifest bytes before requesting aggregate approvals. There is no
embedded manifest hash, approval or predecessor reference, avoiding hash cycles.

## Bundle approval: `hexalith.access-telemetry.c1.bundle-approval/v1`

Proposed exact fields:

`schemaVersion`, `manifest`, `profileId`, `profileSha256`, `workloadSha256`,
`qualificationSessionId`, `targetSha256`, `reviewerPrincipal`, `reviewerRole`,
`decision`, `reasons`, `decidedAtUtc`, `expiresAtUtc`, `authorityReceipt`.

`manifest` is `Ref`; decision/reasons/receipt types follow disposition. Accepted
bundle roles are exactly `platform-operations` and `security`, each once, with
valid separate receipts binding exact manifest bytes. Review principals must
be different except for the owner-approved exception recorded on 2026-10-07:
Jérôme Piquot may provide both roles as authenticated GitHub account `jpiquot`,
canonical principal `github:user:6775094`. The exception removes separation
between the two review roles only; it does not remove either decision, receipt,
role authorization or producer/reviewer separation. Neither reviewer can
self-approve a capture it produced. P4 must
resolve how approval-gate authors participate without circular or self-approval.
An aggregate approval cannot double as C1.23/C1.24/C1.25 or C5/C6.

## Predecessor: `hexalith.access-telemetry.c1.predecessor/v2`

Proposed exact fields:

`schemaVersion`, `manifest`, `approvals`, `status`, `productionLifecycleWrites`,
`qualificationAuthorized`.

`manifest` is `Ref`; `approvals` contains exactly two distinct bundle-approval
`Ref`, ordered Operations then Security. `status: passed`,
`productionLifecycleWrites: disabled`, and boolean `qualificationAuthorized:
true` may be emitted only after the complete current authorization verdict. They
are summaries to revalidate at use, never freestanding permission. An archived
bundle may retain those original bytes after expiry; it cannot authorize again.

Never down-convert into the legacy unversioned predecessor and then bypass
verification. P7 must approve strict version dispatch at every consumer; legacy
parsing may remain for historical inspection without execution authority.
