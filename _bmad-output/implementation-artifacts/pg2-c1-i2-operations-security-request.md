# PG-ONPREM-2 C1 I2 registration and live-evidence request

## Current handoff, adopted 2026-10-12

The owner approved sole-owner qualification. The institutional request below is
retained as historical context for the older protocol. A new Platform P1 adoption
manifest, independent human reviewer or external immutable custody service is not
required for the new mode. The owner approves the prepared target/action plan and
accepts measured results; the agent owns record preparation, collectors and
verifier implementation. See [the owner handoff](../../docs/operations/access-telemetry-owner-handoff.md).
Actual scoped run approval and complete technical evidence are still required.

**State:** request prepared 2026-10-10; no execution grant or approval recorded. This is the next separately tracked prerequisite for Story 27.4. It does not change the owner-approved P1–P4 directions, the deployed refusal, or any C1 gate state.

**Ownership clarification (2026-10-10):** `Hexalith.Platform` owns the deployment target and operational access boundary. Memories must consume authoritative Platform-produced target, namespace, custody, credential-path and grant evidence; the sole contributor is not expected to invent or manually restate those runtime facts. Jérôme Piquot is the sole project contributor and Operations/Security policy authority, so no second policy co-signer is requested. Existing distinct producer/reviewer principal rules, C1.25 author/reviewer separation and post-evidence C5/C6 separation remain unresolved unless separately corrected by an approved policy change. A single contributor or this clarification alone cannot satisfy them. Track the response in [issue #57](https://github.com/Hexalith/Hexalith.Memories/issues/57).

**Owner-selected handoff (2026-10-10):** The [delivery-route decision](../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md#owner-selected-delivery-route-2026-10-10-planning-decision-only) prefers Platform P1 v1 if operable, reuse of authentically proven Platform target/custody resources, independent named reviewers, one bounded C1.16 provenance and eligibility audit followed by fresh capture if incomplete or ineligible, and strict predecessor/v2 consumer migration. Platform should return feasibility and the versioned adoption manifest with authenticated references; an alternative receipt protocol needs a separate owner decision if P1 v1 cannot be operated. This handoff changes no `NO_*` state, registration, session, grant or gate.

Platform P1 v1 feasibility and adoption inputs are tracked in [Hexalith.Platform issue #5](https://github.com/Hexalith/Hexalith.Platform/issues/5); the complete Memories handoff remains [issue #57](https://github.com/Hexalith/Hexalith.Memories/issues/57). Both are requests for authenticated records, not grants or accepting evidence.

## Repository work already available

The [closed I2 inspector](spec-pg2-c1-i2-registration-source-provenance.md) validates an exact thirteen-field registry entry, retained registration/command/role Ref bytes, the C1.15 source inventory and a clean explicit local Git checkout. `lookup_deployed_binding` still refuses every gate. The [interchange spec](../specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md) and [decision packet](../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md) require genuine approved registration and authenticated live provenance before any accepting entry can be considered. Current source revision `25a412e75e88c44bf184b2c5b3859fda5f5f5cbe` is a candidate label only; approval must bind the actual retained source bytes and full revision used for capture. Neither a matching Git hash nor an offline fixture authenticates an owner decision.

The closed entry must supply exactly `gate`, `profileId`, `registeredStory`, `registrationReceipt`, `producerPath`, `helperPaths`, `inputPaths`, `captureSchema`, `verifierPath`, `verifierSchema`, `commandContract`, `cleanupRequired` and `reviewRolePolicy`. For C1.15 the candidate producer is `tools/verify-access-telemetry-c1.ps1`; helpers are `tools/access-telemetry-c1-component-backend.ps1` and `tools/access-telemetry-c1-profile.ps1`; capture schema is `hexalith.access-telemetry.c1.evidence/v2`; the proposed command material names `kubectl` and the six ordered purposes in the [inspection contract](../../docs/operations/c1-producer-binding-inspection-contract.md). The sixteen exact input paths are:

```text
deploy/dapr/components/access-telemetry-config.yaml
deploy/dapr/components/access-telemetry-secrets.yaml
deploy/dapr/components/access-telemetry-store.yaml
deploy/kubernetes/base/access-telemetry-deployments.yaml
deploy/kubernetes/base/access-telemetry-postgresql.yaml
deploy/kubernetes/base/dapr/access-telemetry-clock-config.yaml
deploy/kubernetes/base/dapr/access-telemetry-config-store.yaml
deploy/kubernetes/base/dapr/access-telemetry-lifecycle-config.yaml
deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml
deploy/kubernetes/base/dapr/access-telemetry-store.yaml
deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml
deploy/kubernetes/overlays/production/kustomization.yaml
deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml
deploy/openbao/service-account-hardening.yaml
deploy/openbao/smoke-test.yaml
deploy/openbao/values.yaml
```

The current [C1.15 pin inspector](../../tools/access_telemetry_c1_capture_semantics.py) checks declared observations only; it is not a complete registered semantic verifier. `registeredStory`, final `verifierPath`/`verifierSchema`, actual closed command grammar, `cleanupRequired`, the three authenticated registration/command/role Ref values and done owner evidence remain for the C1.15 owner to supply and Security to review. A fixture schema or historical PG1 story cannot fill them.

## Requested Operations and Security decisions

The paths below are **concrete proposals for review**, not existing approved resources. Operations and Security must return exact approved values or an explicit replacement in a new authenticated, immutable policy/grant record. An unfilled field remains a denial; no local label, directory or credential path can be promoted by this request.

| Item | Proposed scope or named value | Required returned evidence |
| --- | --- | --- |
| Target | Read-only C1 capture against the Kubernetes API selected by client context `jpiquot@local`; local cluster label `local` is not trusted identity. Selector `app.kubernetes.io/name=memories-access-telemetry`. | Independently verified API TLS CA/public-key fingerprint, authoritative cluster ID, exact target digest and approved workload/profile hashes. Do not derive identity from the context or selector. |
| Namespace | `hexalith-memories` for C1; C2–C4 qualification remains separately scoped. | Authenticated namespace UID and allowed resource/action list. The local kubeconfig default `default` does not select this namespace. |
| Custody root | Candidate new external root `/srv/hexalith-evidence/access-telemetry-c1`, with session material beneath `PG-ONPREM-2/<issued-session-id>/`. The path alone says nothing about its backing. | Identify the external mount or custody service, accountable service owner, approved immutable volume ID, and authenticated receipt origin/retrieval/status protocol. Prove immutable receipt creation, owner-only access enforcement, 365-day retention after session closure, cleanup and incident procedure. Historical `/home/administrator/evidence/hexalith-memories` and `/approved-evidence/...` are not approved custody. |
| Credential-file paths | Candidate `KUBECONFIG=/run/hexalith/access-telemetry-c1/kubeconfig`; C2–C4 only: `HEXALITH_STORY_27_4_BUSINESS_BEARER_FILE=/run/hexalith/access-telemetry-c1/business-bearer.jwt`; GitHub review client candidate `/run/hexalith/access-telemetry-c1/github-review-token`. | Approve or replace each absolute owner-only, non-symlink path and name the issuing principal, expiry, audience and rotation procedure. Supply paths and file metadata only, never credential values. The GitHub client currently accepts a caller-supplied token, so its proposed file path needs a separately reviewed caller boundary. Any non-GitHub receipt-reader credential path awaits the selected P1 issuer/protocol. |
| C1.15 registration and execution | Register exact PG2 C1.15 story/producer/helpers/16 inputs, v2 capture schema, implemented verifier, closed command grammar, cleanup rule, role policy, full source commit and approved registration receipt. Grant one issued session ID and immutable grant Ref for `capture`, `review`, `assemble` and `use` with distinct action windows. | Approved/done successor owner record; authenticated registration/command/role materials and source snapshots; independent producer-execution, target and custody receipts; exact tenant ID, registry revision/digest and effective policy revision/digest; exact profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`. C1.16 closed-window acceptance supplies no new session grant. |
| Fault and purge authority | **No fault or purge operation is requested for C1.15.** For later C2–C4, request a separate non-Production grant covering exact shared-system fault selectors, rollback/recovery owner, three retention cohorts, active purge, PostgreSQL physical reclamation and final disabled/empty-Lease/zero-replica cleanup. | Operations must return stable principal IDs and separate action grants for the Platform Operations fault owner, lifecycle purge owner and adapter reclamation owner; exact resource scope, action windows, safety/abort criteria and Security concurrence are required before any live mutation. Jérôme Piquot's archive/incident assignment is not mutation authority. C1 read-only authority cannot be reused. |
| Reviewers | Jérôme Piquot (`github:user:6775094`) is the recorded sole P1–P4 policy signer and may fill **both bundle roles only** with two distinct authenticated decisions/receipts if he produced no manifest capture. | Appoint the actual C1.15 producer and a different independently granted Security gate reviewer; record stable principal IDs and exclude every actual producer. Name the Operations and Security bundle-role reviewers/grants for this session. Name an authorized GitHub PR author distinct from each reviewer and bind the PR/commit/review IDs. C1.25 needs different Security author/reviewer principals; later C5 and C6 need separate named reviewers. No principal is inferred from a role label or this request. |

Security also needs to publish the operational P1 receipt issuer, trust roots, retrieval/status/revocation protocol and principal mapping; an authenticated P2 numerical-policy revision; and effective P4 gate grants/parent matrix. The [decision packet](../specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md) records owner approval of directions and numerical limits, but its `NO_*` states remain effective until these records exist. P5 still needs registrations for the other twenty-three held gates; P6 must decide C1.16 session eligibility; P7 must approve and verify strict consumer migration.

## Required response and release boundary

Operations and Security should return one versioned record with the exact target and namespace identities, tenant ID, custody root/volume/receipt origin, approved credential-file paths, session ID/grant Ref/action windows, registry and policy revision/digests, producer and reviewer principal grants, source/registration Refs, fault/purge decisions and the immutable record digest. Include an explicit Platform P1 v1 feasibility disposition and its deployment/support boundary; if Platform cannot operate it using existing approved infrastructure, return a concrete alternative for a separate owner policy decision. A reviewer must be able to retrieve and authenticate every referenced byte and current status independently. If a proposed path or person changes, the record must name the replacement explicitly.

Until that response and live evidence are verified, do not run the C1.15 collector or later fault/purge producers, add a deployed accepting registry entry, assemble a passing C1 predecessor, or claim Story 27.4/A41/Production credit. Story 27.4 stays `in-progress` under `RECHECK_ONLY`, A41 and its sprint action stay open, and Production lifecycle writes stay disabled.

## 2026-10-10 response audit for Story 27.4 spec 11

At repository commit `4167ce3fdcffd0923bc9f5d8960025492a279608`, the request and owner decision packet still expose `NO_SESSION_ID`, `NO_GRANT_REF`, and `NO_TARGET_DIGEST`. No authenticated, versioned Platform response, deployed PG2 C1.15 registration, or separately scoped C2-C4 action grant was supplied to this implementation run. This is a repository and task-input observation, not a claim about records held outside this workspace. The exact required profile and workload SHA-256 remain `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.

The proposed target, custody and credential paths above remain unapproved. Current disposition: deny target contact, C1 gate advancement, C0/C2-C4 execution, C5/C6 acceptance, and A41 publication. Platform Operations owns the missing target, custody, session and scoped action records; Security owns receipt trust, current status, policy and reviewer grants; the C1 gate owners, once registered, own their source and accepted gate evidence. Recheck only when those inputs change or immediately before authorized execution.

## 2026-10-12 P1 v1 feasibility result

Platform commit `e229df9a58ee6899f2b24ba51f652435bc36dc62`, file `docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md`, SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`, finds P1 receipt-provider v1 not currently feasible for this qualification. Memories accepts that finding. No adoption manifest was returned or created. The proposed C1.15-only signed packet is not enrolled.

The reviewable blocker record is [`c1-15-only-signed-evidence-packet-decision.md`](../specs/spec-pg2-c1-authenticated-predecessor-interchange/c1-15-only-signed-evidence-packet-decision.md), SHA-256 `d85537fbbf3a50eeff913a6c3363fbd3f0046ae112b0ce3d1f2b37cbce36eb04`. The owner has not selected that packet, an independently pinned signer, or a bounded custody replacement. No authenticated status, trusted-time, fresh read-only target/session grant, exact source and command binding, actual producer, independent reviewer, or external custody record was supplied. `NO_SESSION_ID`, `NO_GRANT_REF`, and `NO_TARGET_DIGEST` remain in force.

C1.16 eligibility, the other twenty-three gates, P5–P7, and later C2–C4 grants stay separate obligations. Disposition remains `RECHECK_ONLY`. This audit contacted no live target and did not accept a C1 binding, enable Production writes, complete Story 27.4, or mutate A41.


## 2026-10-12 current-pin response audit for Story 27.4 spec 14

At Memories HEAD `f4c7d68f619f376debca440387428139a61080cc`, the root-recorded and checked-out Platform pin is `977f2e01347226ac727b707aee4f140f5dc8b010`. Local Git-object and exact worktree readback of `docs/implementation/pg2-p1-v1-feasibility-2026-10-12.md` matches SHA-256 `b879819874934937cea19b406edd66c2657ded9527348ce4a8b626227865dd6e`. The cited P1 contract and qualification record still match `968316ab6801c7d9f84c9a5fc2bd7b85d8f905b05a1fea32b90f7a490eed3ab2` and `cf7fa72152234133e468c965ea5d87b085ae607ed056f8cd1ba189203c0c4b3e`. The updated pin contains the same infeasibility finding; the earlier dated observations remain intact.

No authenticated adoption, current status/revocation, trusted time, target/session, custody, source/command, producer/reviewer or separate fault/purge grant was supplied to this run. The signed-packet blocker remains unchanged and the proposed packet remains unenrolled. This is a local repository and task-input audit, not proof about records outside the workspace or an authenticated operational status check. Unknown current authority therefore denies execution. Platform Operations owns the missing target/session, custody and action records; Security owns receipt trust, current status, policy and principal grants. Those are provider prerequisites, with no requirement for the user to reconstruct runtime facts. The accepted same-session 25-gate predecessor and two authenticated bundle decisions remain absent; closed-window C1.16 acceptance grants no new eligibility.

Disposition remains `RECHECK_ONLY`. The [spec 14 offline verification receipt](tests/27-4-retention-verification-evidence.md#2026-10-12-spec-14-current-pin-readiness-hold) records both required passing Python suites. No live target, gate, Lease or workload was contacted or mutated; C0–C6 states, A41/action and Production disablement stay unchanged. All six live execution tasks remain open.

### Sole-owner qualification policy, adopted 2026-10-12

The owner explicitly approved `sole-owner-qualification/2026-10-12`. For
non-Production qualification the same owner may produce and accept evidence,
labelled `sole-owner/self-reviewed`. C1.25 records owner security risk acceptance;
C5/C6 and Operations/Security roles retain distinct records without independent
review claims. Owner-approved bounded run records and private local hashed/readback
evidence replace separate institutional issuers and external immutable custody
for this version. Actual target/action approval and all 25 technical gate passes
remain required. Production activation, runtime identity/security and publication
remain separately governed. Earlier institutional policy wording is retained as
history for the stronger/legacy protocol. Policy adoption does not implement a
collector, register a successor, pass a gate, complete 27.4 or close A41.

The institutional I2 request above is superseded for owner-mode qualification. The current handoff is [the owner guide](../../docs/operations/access-telemetry-owner-handoff.md). Missing collectors and verifier migration are development work; the owner supplies plan and result approval. No new Platform P1 response is required to implement this mode.
