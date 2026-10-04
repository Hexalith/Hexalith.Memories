# Approved Prerequisite Handoff

**Historical proposal-time sections:** the original pending/adoption-held claims below
are superseded only for repository adoption by the dated sections at the end.
Actual live qualification remains pending.

Updated 2026-10-04 after D1-D4 repository implementation approval.

The first component/backend producer is prepared and verified on closed
historical PG-ONPREM-1. It requires explicit historical opt-in and grants no gate
credit. [Preparation spec](../../implementation-artifacts/spec-c1-16-component-backend-capture-preparation.md)
records the actual tests. Story 27.22 remains held because its proposed command
uses the successor profile, which is still rejected by current tools.

**Attribution limit:** Independently observed Component settings, authenticated
loaded-component metadata and PostgreSQL session identity do not prove that the
loaded Component actually connects to the inspected backend/database. Neither
a shared name nor a Secret reference establishes that connection linkage. The
collector does not retrieve Secret values or award behavioral credit. Actual,
secret-safe Component-to-backend attribution remains held for qualifying evidence
on the exact approved profile before C1.16 acceptance.

The [exact inactive profile package](profile-candidate/README.md) supplies verified
PostgreSQL 18.6, OpenBao 2.6.4/chart 0.29.6 and Dapr 1.18.1 identities, concrete values
and workload files, render receipts and canonical hash. Exact-byte review precedes
adoption. Fresh operational qualification follows separately authorized execution.
The [D3 contract](predecessor-interchange-contract.md) records the genuine capture,
disposition and authority requirements that an assembler must implement.

## Source-Confirmed Implementation Dependencies

| Slice | Current source finding | Owner and required reopen evidence |
| --- | --- | --- |
| C1.3 / Story 27.9 | `DaprAccessTelemetryStateStoreTests.cs::WriteRecordAndIndexAsync_ConcurrentInitialRecordWriteWithoutEtag_DoesNotClobberTheWinner` documents that raw FirstWrite without ETag is last-write-wins. Lifecycle no-clobber behavior relies on the transactional catalogue ETag fence. | Deployment Adapter Developer: observe both component semantics and actual lifecycle fence/end state. The held draft's blanket raw FirstWrite claim is corrected; no-clobber and retention requirements remain. |
| C1.9 / Story 27.15 | `AccessTelemetryQualificationWorkloadRunner.cs` sets 125 records/s per writer, two writers; its existing test asserts 250 aggregate. This does not meet the approved 500 events/s for 30 minutes. | Platform Operations / owning workload developer: a closed 500 events/s seam and exact emitted/acknowledged inventories, rate/latency/loss/cleanup evidence. Do not lower the approved target or relabel current 250 as 500. |
| C1.11 / Story 27.17 | Current component uses fixed `access-telemetry` partition and one PostgreSQL role/schema; existing tenant filtering cannot prove physical denial from independently authenticated A/B principals. | Architecture/Security/platform owners: settle AD-6/AD-15/AD-17 tenant authority and storage enforcement, then actual principal-driven negative evidence. A generic collector or application fixture cannot close this gate. |
| C1.21 / Story 27.27 | Restore non-resurrection needs the AD-21 erased-tenant register and restore filtering/replay contract. Existing backup mechanics do not supply that contract. | Architecture / recovery owner: implemented erased-tenant register, authenticated restore filtering and negative evidence that erased tenants stay erased. |
| D3 interchange | Current artifact hashing does not verify artifact content semantics or restrict source paths to implemented producers; reviewer labels do not authenticate authority. | Adapter/Security: exact versioned schemas, closed registry, genuine decision receipt contract and executed rejection fixtures before assembler registration. |

These are dependencies to be solved in their owning slices. They do not reduce
the approved C1 observations, substitute old evidence, or authorize a broad bundled
implementation. All 24 story drafts retain their approved gate/role mapping.

Next after exact-byte review: coherently prepare successor profile consumers,
then finalize and register one genuinely supported gate transaction at a time.
Apply the dated Story 27.21 prose correction at actual registration. The tracker,
epics, completed Story 27.21, Story 27.4 and A41 records remain unchanged here.

## Dated repository adoption — 2026-10-04

The exact candidate was approved and adopted by the bounded implementation spec.
See [the adoption record](adoption.md) for current selectors, retained historical receipts,
neutral capture boundaries and the single C1.16 owner. Earlier pending/adoption-held
text records proposal-time state; its original bytes and operator intent are preserved.
No target execution, security requalification, Production activation or gate pass is claimed.

## Remaining qualification prerequisites — dated 2026-10-04

Exact PG-ONPREM-2 repository adoption is approved and implemented. Proposal-time
pending exact-byte/adoption claims above are historical; live upgrade, operational
qualification and independent acceptance remain pending. Story 27.21 and its PG1
C1.15 command/accepted evidence remain historical and unchanged. No additional
story is registered by this correction.

| Prerequisite | Owner | Current consequence | Measurable reopen evidence |
| :----------- | :---- | :------------------ | :------------------------- |
| Actual execution platform, image-manifest epoch and profile qualification — held C1.17 / unregistered Story 27.23 draft | Platform Operations | Accepting an approved OCI index observes image identity, not actual platform. Even an allowlisted platform-child imageID does not supply independent node/platform qualification; C1.16 grants no C1.17 credit. | Separately authorized immutable same-PG2 observations bind selected Pods to their actual node OS/architecture, authenticated index/child identities and manifest/profile epoch, followed by independent C1.17 disposition. |
| Fresh PG2 C1.15 producer and review prerequisite, separately scoped and unregistered | Deployment Adapter Developer; independently named reviewer | `C1.15/PG-ONPREM-2` remains unsupported and is rejected before target calls/output-directory creation. Generic Python C0 observations and the historical Story 27.21 packet cannot renew C1.15 or satisfy a current predecessor. | A separate approved scope implements a literal PG2 C1.15 producer in the existing runner, exact runtime/control-plane/configuration/profile provenance, immutable neutral packets, and nonzero positive/negative fixtures rejecting drift, wrong pins, malformed/secret observations and invalid modes. Then separately authorized live capture and an independent reviewer must retain a retrievable same-PG2 archive and disposition; only that executable source/fixture/capture/review evidence reopens the prerequisite. |
| Inherited container-incarnation continuity limitation | Deployment Adapter Developer | Ready/Pod UID/imageID rechecks do not prove an unchanged process instance when a container restarts inside the same Pod. Maintenance remains pending; no fix or qualification credit is claimed. | Separately scoped stability maintenance compares initial/final containerID and restartCount and executes changed-incarnation rejection fixtures on the actual capture path. |
| Inherited authenticated remote-response buffering limitation | Deployment Adapter Developer | The metadata shell probe buffers the full HTTP response in command substitution before local capture limits apply. Remote memory bounding remains pending; no fix is claimed. | Separately scoped probe maintenance bounds the remote reader before command substitution and executes oversized actual-probe rejection evidence with secret-safe output. |

The two inherited maintenance limitations are already recorded in the
[deferred-work ledger](../../implementation-artifacts/deferred-work.md); its old
entries and historical acceptance are untouched. Their pending work does not
widen this adoption or grant Production, security, Story 27.4 or A41 credit.
