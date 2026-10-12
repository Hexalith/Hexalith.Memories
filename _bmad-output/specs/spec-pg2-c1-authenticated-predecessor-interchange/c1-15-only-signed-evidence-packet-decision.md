# PG-ONPREM-2 C1.15-only signed evidence packet

**Review state:** prepared 2026-10-12 for Memories owner review. The owner choice and the operational records below are absent, so this file is the blocker record.

**Disposition:** `RECHECK_ONLY`.

**Effect:** P1 receipt-provider v1 is accepted as not currently feasible for this qualification. The proposed C1.15-only packet is not enrolled. No adoption manifest is created. No signer, session, grant, custody root, registration, or accepting C1 binding is created.

**Exact scope:** profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`; workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.

## Accepted Platform finding

Memories accepts the finding in Platform `docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md` at commit `e229df9a58ee6899f2b24ba51f652435bc36dc62`. Independent retrieval of that blob through the Platform git object and through the GitHub contents API at the same ref both produced SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`. The Memories Platform submodule pin `3217e6329ec015532061f262c55de1f3956fdd46` does not contain that file.

The same two retrievals re-checked the records the assessment cites, both at Platform `fa5dfc23dfaa3a36407a1e653f91415c5bfee7b5`:

| Record | SHA-256 |
| --- | --- |
| `docs/contracts/p1-receipt-provider-v1.md` | `968316ab6801c7d9f84c9a5fc2bd7b85d8f905b05a1fea32b90f7a490eed3ab2` |
| `eng/cluster-management/QUALIFICATION.md` | `cf7fa72152234133e468c965ea5d87b085ae607ed056f8cd1ba189203c0c4b3e` |

Those two files are offline contract and historical qualification evidence. Their digests match the assessment. They do not enroll an issuer, target, grant, or archive. Issue #57 and Platform issue #5 remain open requests. Their comments are mutable tracking text, not the operational records this decision requires.

## Exact blocker

Both of the following are absent. Either absence refuses the packet. Supplying one leaves the other in force.

1. **Owner choice absent.** No authenticated owner record selects the qualification-specific signed packet as a P1 replacement for this single C1.15 predecessor, names an independently pinned signer and trust pin, states how packet and session withdrawal are observed, or discloses sole-owner risk. The [sole-owner draft](sole-owner-policy-revision-draft.md) remains unadopted. Jérôme Piquot (`github:user:6775094`) remains the recorded sole P1–P4 policy signer in the [decision packet](p1-p4-security-operations-decision-packet.md). That appointment does not pin a packet signer, name the actual producer, or name an independent C1.15 reviewer.
2. **Operational records absent.** For every fact below, the Platform assessment supplied no authoritative record URI, record SHA-256, authenticated issuer, or current withdrawal route. The [I2 request](../../implementation-artifacts/pg2-c1-i2-operations-security-request.md), the P1–P4 packet, and the [Story 27.4 evidence matrix](../../implementation-artifacts/tests/27-4-retention-verification-evidence.md) likewise contain no such record. Their current states remain the listed `NO_*` denials.

## Facts required before any later enrollment

A later owner decision can enroll this packet for C1.15 only after each row has an authenticated record a reviewer can retrieve independently of this file. A chat approval, issue comment, Git author, role label, fixture, or this proposal cannot fill a row.

| Fact | Required binding | Current state | Authenticated record that establishes it |
| --- | --- | --- | --- |
| Independently pinned signer | One signer and trust pin established outside the packet: algorithm, verification-key fingerprint or equivalent service anchor, audience, and current key inventory. Signature checks use that pin. The packet body cannot appoint its own signer. | `NO_INDEPENDENTLY_PINNED_SIGNER`. The sole P1–P4 policy signer is not this pin. | Immutable out-of-band enrollment naming the signer, key fingerprint, purpose, validity, and current inventory digest, with a withdrawal method that has zero grace. |
| Current status and revocation | Immediately before use, re-fetch current signer/key, policy, principal, grant, session, and target status. Unknown, missing, or withdrawn status denies. Kubernetes RBAC removal and packet/session withdrawal must be observable. A later signature cannot prove an earlier execution. | `NO_RETRIEVAL_OR_STATUS_ENDPOINT`; `NO_CURRENT_STATUS_PROOF`. No withdrawal route was supplied. | Versioned status origin and proof format, independent of the packet, plus a current inventory whose digest a reviewer can recompute. The record names the exact RBAC-removal and packet/session-withdrawal observations. |
| Trusted time | Independently authenticated UTC, advanced with same-boot Linux `CLOCK_BOOTTIME`, sampled no more than 60 seconds before context construction. The owner-approved P2 numerical limits apply only through an immutable policy revision. Local wall time and an old sample reused as a fresh anchor refuse. | `NO_AUTHENTICATED_TIME_SOURCE`; `NO_AUTHENTICATED_P2_POLICY_REVISION`. The P2 table is an approved numerical direction, not that revision. | Time-source identity, sample instant, same-boot binding, and the policy revision digest that adopts the approved P2 numbers, each with current withdrawal status. |
| Fresh read-only target and session grant | One newly issued session and immutable grant Ref for this capture: exact profile and workload hashes, tenant, independently verified API TLS anchor, cluster ID, namespace UID, target digest, registered C1.15 read-only Kubernetes API actions and resource selectors, and separate `capture`, `review`, `assemble`, and `use` windows. | `NO_SESSION_ID`; `NO_GRANT_REF`; `NO_TARGET_DIGEST`; `NO_APPROVED_TARGET_IDENTITY`; `NO_AUTHENTICATED_SESSION_GRANT`. | Platform-produced grant whose issuer, scope, windows, and current status are independently checkable. The historical recovered client and closed-window C1.16 acceptance do not issue it. |
| Exact source and command binding | Retained source bytes and the full revision actually used for capture; the thirteen-field registry entry; implemented verifier path and schema; closed command grammar; `cleanupRequired`; and the registration, command, and role Ref digests. | Candidate label `25a412e75e88c44bf184b2c5b3859fda5f5f5cbe` is not an approved binding. Fixture command schema `hexalith.access-telemetry.c1.fixture-command-contract/v1` names `kubectl` and six purpose kinds and is a format check only. `registeredStory`, verifier path and schema, closed grammar, cleanup rule, and the three Ref values remain unsupplied. | Approved registration receipt binding those exact bytes, digests, and the command grammar used for the capture. A matching Git hash or fixture does not authenticate it. |
| Actual producer and independent reviewer | Stable principal ID of the person or workload that executes the capture, and a different independently granted Security reviewer for C1.15. Every actual producer is excluded from that review. | `NO_PRODUCER_EXECUTION_GRANTS`; `NO_GATE_REVIEWER_GRANTS`. The candidate producer path `tools/verify-access-telemetry-c1.ps1` appoints no principal. The bundle-role exception for `github:user:6775094` applies only to the two bundle roles and only with two receipts and no capture production. | Separate principal grants for the actual producer and the C1.15 Security reviewer, each with scope, window, and current status. Producer overlap requires an adopted policy revision that discloses it. The unadopted sole-owner draft is not that revision. |
| Proven external custody | An existing external archive with immutable object identity, exact-byte readback, enforced owner-only access, 365-day retention after session closure, off-site custody, and an incident and retrieval process. | `NO_APPROVED_CUSTODY_ROOT`; `NO_APPROVED_VOLUME_ID`; `NO_IMMUTABLE_RECEIPT_ORIGIN`. The historical archive under `/home/administrator/evidence/hexalith-memories` stays excluded. Candidate paths in the I2 request are unapproved and are not repeated here. | Platform or custody-owner record identifying the service or volume, accountable owner, retention enforcement, and readback proof. A Git commit, encrypted export pointer, or proposed directory does not establish it. If no store can be authenticated, a separate bounded custody decision is still required; that decision is absent too. |

## Obligations kept separate

This review is limited to whether a C1.15-only packet could later replace P1 for that one predecessor. It does not decide, grant, or advance any of the following:

| Obligation | Remains required |
| --- | --- |
| C1.16 eligibility | One provenance and eligibility audit of the closed-window acceptance. Missing, expired, revoked, or over-96-hour facts require a fresh authorized capture and independent disposition. Historical acceptance stays historical. |
| Other twenty-three gates | P5 registrations with actual owners, producers, semantic verifiers, and same-session accepted evidence. |
| P5–P7 | Accepting producer registrations, the C1.16 eligibility result, and strict versioned predecessor/v2 consumer migration, including denial-before-dependency tests and current-profile runtime verification. |
| Later C2–C4 grants | Separate non-Production fault and purge grants. C1 read-only authority cannot be reused. No fault or purge operation is requested for C1.15. |
| C5, C6, Story 27.4, A41, Production | Independent post-evidence decisions, terminal proof, and the existing close-out chain. |

## Release boundary

Story 27.4 stays `in-progress` under `RECHECK_ONLY`. A41 and its sprint action stay open. Production lifecycle writes stay disabled. Deployed accepting C1 lookup continues to refuse. No live target was contacted while this record was prepared. No C1 binding was accepted. No Production write, Story 27.4 completion, or A41 mutation was performed.

The public issue reply, when an owner chooses to post it, is limited to record locations and digests. It is not prepared as an adoption manifest and it is not posted by this record.
