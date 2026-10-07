# PG2 C1.15 runtime and control-plane capture contract

Repository producer preparation: **2026-10-07**. This contract describes the
implemented offline-testable producer and the required future independent
disposition. Live capture, archive custody and independent acceptance are pending.
It does not register a successor or implement an authority verifier, all-gate
assembler, C1.17 qualification or aggregate C1 credit. Production stays disabled;
Story 27.4/A41 and historical Story 27.21/27.22 dispositions retain their states.

## Invocation and identity

On a separately authorized eligible target, using PowerShell 7.5 or later:

```powershell
pwsh ./tools/verify-access-telemetry-c1.ps1 -Gate C1.15 -ProfileId PG-ONPREM-2 -QualificationSessionId pg2-c1-15-20261007-session01 -EvidenceDirectory /approved-evidence/access-telemetry-c1/PG-ONPREM-2/C1.15 -CommandTimeoutSeconds 30
```

The example session and external archive path are placeholders for independently
assigned operator inputs; they grant no authorization. The session must be an
explicit 1–128 character ASCII identifier matching
`\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z`, with no credential-shaped names or content.
It is an opaque correlation label, not authenticated session authority. Exact
`C1.15` and `PG-ONPREM-2` literals are required. Historical opt-in is refused.
Session arguments are refused in other modes; PG1 C1.15 keeps its legacy v1
behavior, including its existing case handling. C1.16 retains its v1 envelope and
historical/current mode semantics.

The v2 capture binds these immutable identities:

| Field | Value |
| --- | --- |
| `profileId` | `PG-ONPREM-2` |
| `profileIdentity` | `postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2` |
| `profileSha256` | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| `workloadId` | `adr-27.1-two-writer-500eps` |
| `workloadSha256` | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |
| Target context / namespace | `jpiquot@local` / `hexalith-memories` |
| Selector / app ID | `app.kubernetes.io/name=memories-access-telemetry` / `memories-access-telemetry` |
| Actor type | `AccessTelemetryLifecycleActor` |
| Runtime | Dapr `1.18.1` in `/daprd --version` and authenticated metadata |
| Dapr OCI index | `sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8` |
| Authenticated linux/amd64 child | `sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b` |

The workload hash is SHA-256 of the ADR workload object's canonical compact JSON
with sorted keys: two writers, each at search 200, ingest 6, traverse 10,
case_access 16, delete 2, tenant_lifecycle 0.2, tenant_config 0.8, case_member 4,
annotation 11 events/second; total 500. It binds the approved envelope; this
identity capture executes no workload. Profile hash rules remain those of the
approved canonical PG2 profile; live observations do not redefine that hash.

The shared `tools/access-telemetry-c1-profile.ps1` preflight checks all sixteen
approved deployment/security/workload input byte hashes before PG2 C1.15 makes
any target call or creates the output directory. Missing or symlink inputs,
configuration drift, invalid invocation/session and unavailable source Git
provenance refuse without a packet. C1.16 uses the same preflight and retains its
existing neutral blocker packet behavior. Deployment inputs are never changed.

## Exact v2 capture fields

The producer emits `hexalith.access-telemetry.c1.evidence/v2` only for PG2 C1.15.
The complete top-level field set is:

`schemaVersion`, `gate`, `profileId`, `capturedAtUtc`, `context`, `namespace`,
`targetSelector`, `producerStatus`, `gateStatus`, `productionGatePassed`,
`productionLifecycleWrites`, `observations`, `blockers`, `sources`, `commands`,
`profileIdentity`, `profileSha256`, `workloadId`, `workloadSha256`,
`qualificationSessionId`, `target`, `targetSha256`, `producer`, `sourceCommands`,
`sourceCommit`, `sourceDisposition`, `worktreeDirty`, `producerSources`,
`finalSourceRecheck`, `resultCount`, `failureCount`, `skipCount`,
`independentDisposition`.

`target` contains exactly `context`, `namespace`, `selector`, `appId`, `actorType`
in that order. Its fixed expected context remains recorded on a wrong-context
blocker; the top-level `context` records the observed client context. Context
names do not authenticate cluster identity. `capturedAtUtc` starts producer
collection, including provenance queries. Timestamps use UTC ISO-8601 round-trip
strings, with explicit `+00:00`. Source and command intervals must fit the producer
interval; a future reviewer must check freshness against scoped session authority.

`producer` contains exactly `path`, `identity`, `identityAuthentication`,
`arguments`, `argumentsSha256`, `startedAtUtc`, `finishedAtUtc`, `exitCode`.
Its path is `tools/verify-access-telemetry-c1.ps1`; `identity: repository-collector`
identifies the software, and `identityAuthentication: not-evaluated` makes no
claim about the person executing it. `arguments` records the effective parameter
list, including the resolved evidence directory, explicit session and effective
timeout, even when the timeout was defaulted. The producer exit is 0 for observed
and 1 for blocked. Packet-writer failure refuses publication and can cause a
process failure without a packet.

`sourceCommit` is the full canonical 40-character source HEAD obtained from Git.
`producerSources` contains each of the three used PowerShell sources and all
sixteen approved inputs, exactly once. Each entry contains exactly `source`,
`gitNormalizedSha256`, `executedBytesSha256`, `gitBlobOid`,
`headGitNormalizedSha256`, `trackedAtHead`, `modified`, `matchesHead`.
`source` is a closed repository-relative path. `gitNormalizedSha256` hashes
working-file UTF-8 bytes after Git's CRLF-to-LF normalization, authenticated
against `git hash-object --path`; unsupported clean filters fail preflight.
`executedBytesSha256` hashes the exact bytes executed or read. Helpers are loaded
from one strict UTF-8 byte snapshot parsed with their original file paths; main
script bytes come from its actual parsed source text. Initial provenance compares
these loaded/read snapshots with the current files and refuses changes before any
target call.
`gitBlobOid` is that normalized Git blob identity. `headGitNormalizedSha256`
hashes exact bytes returned by `git show <full-HEAD>:<path>`; it is null when the
file is absent at HEAD. `modified` records whether Git reports staged, unstaged or
untracked state for that source; `matchesHead` compares normalized working bytes
to the HEAD bytes. Symlink source files are refused.

`worktreeDirty` records whether the repository has any Git-reported changes,
refreshed during the final source recheck.
`sourceDisposition` is `clean` or `dirty-development`; any dirty worktree or
modified/untracked/nonmatching used source produces the latter label. A dirty
capture can report observations but cannot satisfy future accepted-source checks.
Before publication the producer rechecks full HEAD and every used source's byte
hashes and modification state. Drift blocks the capture and clears observations;
`finalSourceRecheck` is `unchanged` or `changed-or-unavailable`. Changes after this
final check require independent retained-source validation; a packet is not a
lock on the source repository.

`commands` records each actual kubectl child with exactly `purpose`, `sha256`,
`executable`, `arguments`, `argumentsSha256`, `startedAtUtc`, `finishedAtUtc`,
`exitCode`, `stdoutSha256`, `stderrSha256`, `streamSafety`, `resultCount`,
`failureCount`, `skipCount`. `sha256` retains the legacy command identity hash of
UTF-8 `kubectl ` plus arguments joined by U+001F. `arguments` is the actual process
argument array, including the fixed credential-variable metadata probe, never a
credential value. Safe complete stdout/stderr byte hashes are retained, with
`streamSafety: validated`; unsafe, oversized or incomplete timed-out streams have
null hashes and `streamSafety: not-validated`. Exit status is the actual child
status, or null if no started process has an available exit. Start/finish receipts
also exist for failures. A successful nonempty safe observation has `resultCount:
1`, `failureCount: 0`; other commands have 0 results and 1 failure. No commands
are skipped.

`sourceCommands` separately records every actual Git provenance child with the
same fields except `purpose` and legacy `sha256`. These local provenance queries
retain exact stdout/stderr byte hashes with
`streamSafety: hash-only-source-provenance`; raw source/status/error text is never
copied into the packet. A successful Git query has one completed query result,
including a query establishing that a source is untracked; failed or incomplete
queries have zero results and one failure. These receipts describe source
provenance, not target observations or authorization. Each Git stdout/stderr
stream is capped at 1 MiB, and one five-second deadline covers both pipe reads
and process completion. Oversized or incomplete streams retain null hashes and
`streamSafety: not-validated`; the producer closes streams and attempts cleanup
of its owned process tree before refusing.

Except for workload's established canonical hash, JSON hashes use SHA-256 over
UTF-8 compact JSON emitted by `ConvertTo-Json -InputObject -Compress -Depth 14`
with default escaping, preserving documented object insertion order and array
order. Arguments are JSON arrays even with one member. Byte hashes are lowercase
64-character hexadecimal SHA-256; exact packet bytes, including its final newline,
are hashed externally for disposition binding. No self-referential capture hash
is embedded in the packet. Future validators must reject duplicate/unknown fields,
wrong types, wrong hashes and contradictory facts, rather than reinterpret them.

`sources` retains the existing `{source, sha256}` allowlisted observation hashes
and collector byte hash. `observations` retains exactly `pods`, `runtimeVersions`,
`sidecarImageIds`, `sidecarImageDigests`, `appIds`, `schedulerConnectedAddresses`,
`actorTypes`, `enabledFeatures`, `alphaOptIn`. Each pod contains `pod`, `podUid`,
`runtimeVersion`, `sidecarImageId`, `sidecarImageDigest`, `appId`,
`schedulerConnectedAddresses`, `actorTypes`, `enabledFeatures`, `alphaOptIn`;
alpha pairs contain `componentIsAlpha` and `allowAlphaComponent`. Collection shape,
strict UTF-8 and JSON, authentication token presence, secret safety and exact runtime/image pins are
checked before selected observations are admitted. Every selected Pod is rechecked
for drift. Index, authenticated child and bare digest representations are supported;
the approved index and authenticated child can coexist across stable Pods.
Rechecks compare qualification identities deterministically by Pod name: UID, app
label, deletion state, phase, Ready condition and lifecycle/daprd container names,
readiness and raw image IDs. Incidental resource versions, annotations and status
timestamps do not redefine those identities. Raw per-Pod image identity changes
during capture still block. An observed capture has a
nonzero overall `resultCount` equal to observed Pod count, zero failures/skips and
no blockers. A blocked capture has zero overall results, empty observation arrays,
null alpha values and neutral stable blockers. No partial observations are published.

Packets use a fresh exclusive file, UTF-8 without BOM, and read-only finalization.
They never overwrite existing packets. All captures keep `gateStatus:
not-evaluated`, `productionGatePassed: false`, `productionLifecycleWrites:
not-evaluated` and `independentDisposition: pending`. Fixture success demonstrates
producer behavior only. An observed packet is not accepted evidence.

## Required independent disposition (documented, not implemented)

A future separate immutable disposition must have exactly `schemaVersion`,
`capture`, `gate`, `profileId`, `profileSha256`, `workloadSha256`,
`qualificationSessionId`, `targetSha256`, `producerPrincipal`,
`reviewerPrincipal`, `reviewerRole`, `independence`, `decision`, `reasons`,
`decidedAtUtc`, `expiresAtUtc`, `authorityReceipt`.

Its schema is `hexalith.access-telemetry.c1.disposition/v1`.
All scalar identity, hash, URI, role, decision and timestamp fields are strings;
`capture.byteLength` is a positive integer, `independence` is a boolean, and
`reasons` is a nonempty array of strings. Accepted decisions require
`independence: true`. Hashes use lowercase SHA-256 hex; the receipt hash binds
exact retained receipt bytes. Disposition JSON must be bounded to 1 MiB, UTF-8
without BOM, with no duplicate/unknown fields or credential-shaped content.
`capture` has exactly `path`, `sha256`, `byteLength`, binding retrievable immutable
capture bytes in the approved custody root. It must reject symlink/path escapes.
Gate/profile/workload/session/target hashes must agree with the parsed v2 capture.
`producerPrincipal` and `reviewerPrincipal` are authenticated authority identities,
not labels inferred from `repository-collector`, local usernames or Git commits.
`reviewerRole` is the independently authorized C1.15 review role;
`independence` must establish different producer and reviewer principals.
`decision` is one of `accepted`, `rejected`, `needs-evidence`; `reasons` is a
nonempty list of secret-safe substantive reasons. Decision/expiry timestamps must
be ordered UTC instants and independently checked against the actual authorized
session; receipt revocation invalidates acceptance even before expiry.

`authorityReceipt` has exactly `uri`, `sha256`, `issuer`, `decisionId` and must
identify a retrievable, authenticated receipt binding all those facts and the
capture hash. URI/labels/hashes alone do not establish authority. The owning
security slice must select and implement the supported authentication and receipt
mechanism; this producer implements none and invents no approvals. Future accepted
source checks require exact registered producer/helper set, clean matching source
at full HEAD, complete successful command receipts, positive observations, zero
failures/skips, permitted freshness, genuine custody, authorized independent
review and valid unexpired/unrevoked receipt. Historical v1 captures cannot acquire
missing provenance by relabelling. Registration and all-gate assembly remain
separate work under the [predecessor interchange preparation](../../_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md).
