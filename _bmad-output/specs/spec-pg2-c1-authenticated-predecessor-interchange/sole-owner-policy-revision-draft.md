## Current Platform ownership applicability — approved 2026-10-16

The owner approved Platform-owned local functional infrastructure and the existing Builds runner boundary in [Spec 14](../../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md); its [single report](../../implementation-artifacts/tests/27-4-retention-verification-evidence.md) records current blockers. No supported functional descriptor/result consumer exists yet. No replacement schema, VM/hosted fallback or KVM-hash reuse is admitted. Full/legacy wire contracts below remain strict; local functional evidence supplies no C1/C0–C6, full terminal or A41 credit. Prior approvals/budgets remain unchanged history.

# Historical draft: sole-owner PG2/C1 review policy revision

**Historical institutional decision material:** The sole-owner policy is already adopted. The [current execution plan](../../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) implements the owner-approved 2026-10-16 consolidation; do not reopen receipt-service, independent-reviewer or policy-adoption cycles from the dated text below. Technical gate meanings/dependencies and original decisions remain evidence; this document grants no action.

## Current policy: sole-owner qualification, adopted 2026-10-12

The owner approved `sole-owner-qualification/2026-10-12`, including disclosed
self-review and private local hashed/readback evidence for non-Production
qualification. The owner is accountable for development, Operations and Security;
producer/reviewer overlap is permitted for this revision. C1.25 means owner
security risk acceptance; C5/C6 and both bundle roles retain distinct records
without claiming independent human review. A separate P1 receipt service,
external immutable archive, 365-day/off-site custody and independent grant issuers
are not prerequisites for this mode. The older protocol material below remains
history or the stronger version's contract; it cannot override this revision or
be used as a permissive fallback.

A concrete owner-approved run still binds the actual observed target, exact PG2
profile/workload, source, actions, window and evidence root. All 25 technical
checks, source/command verification, privacy, runtime clock and cleanup remain.
No collector is implemented or gate passed by this policy change. Existing
execution consumers remain closed until the new verifier/dispatch and missing
registrations are implemented. Production activation and publication need their
own approved scopes. See [owner handoff](../../../docs/operations/access-telemetry-owner-handoff.md)
and [approved proposal](../../planning-artifacts/sprint-change-proposal-2026-10-12-sole-owner-qualification.md).

**Historical status:** The earlier external-provider/issue-comment transport proposal below was not adopted. The approved 2026-10-12 revision instead uses local owner-controlled qualification records.  
**Scope:** Story 27.4 review decisions for the exact PG-ONPREM-2 profile SHA-256 `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` and workload SHA-256 `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.  
**Proposed accountable owner:** Jérôme Piquot, authenticated as `github:user:6775094` at each decision. This identifier is selected by the existing policy; this draft does not authenticate a future decision.

## Decision for review

Allow the sole project owner to produce a capture and later accept its evidence in a **disclosed sole-owner mode**. Replace the human principal-separation requirements for C1 gate dispositions, C1.23/C1.24 authors and reviewers, C1.25, the two manifest roles, and C5/C6 with distinct, ordered, attributable owner decisions plus mandatory machine verification of independently observed facts. A second username, role label, receipt ID, or agent acting for the owner must never be described as an independent human reviewer.

This reduces assurance against one person's mistake or misconduct. It does not establish an independent Security approval. Preserve gate ID `C1.25` for compatibility, but label its revised meaning **owner security risk acceptance** in every policy, report, runbook and output. Report C5 and C6 as separate owner decisions, without claiming separate reviewers. If an external requirement mandates independent human approval, this mode cannot satisfy it and must refuse activation under that requirement.

The existing [P1–P4 decision packet](p1-p4-security-operations-decision-packet.md), [authority/session contract](authority-and-sessions.md), [wire schemas](schemas.md), accepted [ADR](../../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md), and [Story 27.4 spec](../../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-11.md) remain authoritative until a separately authenticated owner decision adopts a versioned replacement and the consumers are migrated. This draft itself grants no target access, reviewer role, session, C1 gate, Production write, or A41 close-out.

## Proposed decision and evidence rules

| Area | Proposed rule |
| :--- | :------------ |
| Policy identity | A versioned, immutable owner policy record selects the sole-owner mode, exact owner principal, profile/workload, target, session scope, gate graph, effective and exclusive expiry, revocation method, and the reduced-assurance statement above. Authenticate the owner and current policy status independently of the record body. A chat message, Git author, role label or repository fixture is not the adoption record. |
| Capture | Each gate retains its separately registered producer, literal command/source revision, bounded action grant, authenticated actual execution receipt, exact capture bytes and immutable external custody. The owner may be the producer only when the policy explicitly records that overlap. A later approval cannot reconstruct missing execution or target facts. |
| Gate decision | After the capture is frozen and machine checks pass, the owner issues one decision per gate bound to the gate ID, capture Ref/hash/length, actual producer, source revision, profile/workload/target/session/grant, review role, dependency decisions, outcome, reasons, time and expiry. Each decision discloses any producer/reviewer overlap. No decision can approve itself or a manifest containing itself. |
| C1.23–C1.25 | Keep three separate artifacts and the existing acyclic dependency order. C1.23 records operations acceptance, C1.24 records the non-HA acknowledgement, and C1.25 records owner security risk acceptance of their exact upstream evidence and the sole-owner conflict. The same owner may author and decide each only through separate bound decisions after its parents pass. |
| Manifest | Freeze the numeric-order 25-gate manifest only after all gates have passing machine verification and current owner decisions. Issue two distinct later decisions for Operations and Security roles with different decision IDs and retained bytes. The same owner may fill both and may have produced captures; the conflict is explicit in both decisions. Neither role decision is counted as a gate decision. |
| C5/C6 | Record two separate, sequenced owner decisions over the completed C0–C4 evidence, operations runbook, remaining risk and exact cleanup state. Revalidate all grants and evidence at each decision. Neither decision claims an independent reviewer. |
| Activation | A41 and Production remain held until exact-profile C0–C6, terminal checks, final disabled/empty/zero cleanup, publication authorization and remote verification pass under the adopted policy. Retain the older policy and historical PG1/closed-window decisions without relabeling them. |

## Decision transport proposed for implementation

[GitHub does not allow a pull-request author to approve that pull request](https://docs.github.com/en/pull-requests/how-tos/review-pull-requests/approving-a-pull-request-with-required-reviews). Do not reuse the existing PR-review adapter by assigning the owner an alias or treating an issue comment as a PR review. Add a **versioned GitHub issue-comment decision adapter** for owner policy and review decisions only. The selected repository and numeric owner ID must come from a separately authenticated policy bootstrap, not from the comment itself. A decision body needs an exact versioned schema covering the bound fields above and a unique decision ID. Retain its exact UTF-8 bytes and GitHub response under the approved custody root.

At assembly and immediately before use, perform a fresh authenticated GitHub API GET for the exact comment ID and issue, with the fixed origin, TLS/hostname validation, no redirects or cached response, and the existing P2 request, clock, age and whole-chain bounds. Require the returned numeric author ID, body bytes, issue URL, creation/update state, selected decision ID and approved scope to match the retained record exactly. A separately authenticated current policy inventory must name the active decision IDs and revocations. Edited, deleted, replaced, superseded, unavailable, expired or revoked decisions refuse. The adapter must authenticate GitHub identity and current comment state; it does not authenticate producer execution, a Kubernetes target, archive custody or a Platform grant. A comment's URL or digest alone is not authority.

Platform P1 v1, or a separately approved replacement, still has to supply independently adopted roots, actual issuer/status service, target and custody observations, credential-to-principal mapping, trusted time and scoped capture/use grants. The Platform [P1 v1 contract](../../../references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md) currently describes an offline verifier and `NO_*` operational enrollment; this draft neither deploys it nor supplies its missing facts. The exact PG2 target, namespace UID, custody origin, session and C2–C4 fault/purge grants must come from authenticated Platform-produced records. Never infer them from this repository's manifests or the owner's statement.

Inspection basis: Memories `499b7a838a36a4cd500ab517d86acf07b1d74ad4`; the locally available Platform `origin/main` is `78e47a4c8ca853adaf7aa476c11bce92ad7f42d0`. A search of that Platform revision found no exact PG2 profile hash in its documentation, implementation artifacts, source or tests. Its P1 contract still records the `NO_*` enrollment states. This is a repository observation, not proof that no external deployment exists.

## Required implementation and acceptance before adoption can be used

1. Obtain the owner's authenticated, versioned adoption decision with the reduced-assurance statement and an effective policy revision. Update the P1–P4 packet, authority/session and schema contracts, ADR, Story 27.4 frozen intent through explicit renegotiation, runbooks and public assurance language. Retain the previous versions and their evidence.
2. Implement strict version dispatch and the issue-comment decision adapter. Keep the current PR-review and legacy predecessor paths unchanged for their existing policy versions. No permissive fallback from failed new verification to old labels or fixtures.
3. Update the role and graph verifier so each gate, C1.23–C1.25, the two bundle roles, C5 and C6 require their distinct bound decisions, while the named owner overlap is permitted only under the adopted sole-owner revision. Require explicit conflict disclosure and automated fact verification before each decision can pass.
4. Test the positive sole-owner path and refusal of alias accounts, missing disclosure, reused decision IDs, PR self-review substitution, edited or unavailable comments, wrong gate/role/scope, cross-session evidence, failed machine checks, absent execution/custody/target receipts, stale or revoked authority, dependency cycles, and missing cleanup. Run the full affected offline suites and the live qualification only after separate action grants.
5. Requalify the exact PG2 profile and all twenty-five C1 gates in one eligible session. Obtain separately scoped C2–C4 grants, perform C0–C6 and terminal/publication checks, and retain actual receipts. If any fact is absent, record a denial and leave A41 and Production held.

## Open decision

Adoption requires the owner to accept the loss of independent human review explicitly. If that risk is unacceptable, retain the existing policy and obtain independent reviewers. Neither choice supplies the missing Platform deployment and live evidence; those are separate prerequisites.
