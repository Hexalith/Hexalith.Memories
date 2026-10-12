# Sprint Change Proposal: Sole-owner access-telemetry qualification

Date: 2026-10-12. Mode: Batch. Status: Approved for implementation by the owner.
Baseline: `f4c7d68f619f376debca440387428139a61080cc`.

## 1. Issue summary

The owner stated: “I am the owner and only contributor. make requirement simple
to resolve.” Story 27.4 currently requires independently authenticated grants,
external custody and reviewers excluded from producing evidence. These project
policy choices make the owner depend on parties and services that have not been
established. The deployed authority boundary also refuses every request; filling
in a document cannot make the current code accept a session.

The proposed correction gives the owner three responsibilities: approve the
sole-owner policy, approve a concrete bounded test plan, and accept its measured
results. Developers and tools collect and verify the evidence.

## 2. Impact analysis

Epic 27 keeps its retention, recovery, capacity, privacy and cleanup outcomes.
Story 27.4 changes its approval and evidence-custody policy, including its frozen
intent. The twenty-five C1 checks keep their identifiers and technical meanings;
C1.25 becomes **owner security risk acceptance**. Existing gate registrations
remain development work, with the sole owner accountable for every role.

PRD FR67/NFR34 remain unchanged: bounded infrastructure telemetry, fail-closed
Production admission, and no certified audit claim. The ADR, current architecture
spine's AD-17 telemetry boundary, interchange contracts, operations runbook and
Epic 27 acceptance wording must disclose sole-owner review. AD-19's dependency
and release-evidence requirements remain unchanged. Epic 34/35 release and telemetry work
must consume that disclosure without receiving qualification credit. Other
epics, product scope and UX behavior are unaffected.

Current source permits only C1.15/C1.16 capture modes. Missing collectors and
semantic verifiers are implementation work; the owner is not asked to manufacture
their evidence. Historical captures retain their original meaning.

## 3. Recommended approach

Use Direct Adjustment with an explicitly versioned **sole-owner qualification**
policy. The owner may act as developer, operator and reviewer. Reports state
`sole-owner/self-reviewed`; they never claim an independent Security review.
An agent operating for the owner is not a second human reviewer.

Replace qualification-time P1 receipt-service enrollment and separate
Operations/Security grant chains with one owner-approved run record. Anchor the
policy and plan in retained repository bytes; bind each run to its exact source,
profile, workload, observed target, resource list, allowed actions and expiry.
Use strict version dispatch. A local owner record does not satisfy the older
independently authenticated protocol and must not be accepted by that path.

For this qualification mode, use a private owner-controlled evidence directory,
hash manifests, exclusive per-run files and exact-byte readback. These provide
reproducibility and drift detection, not independent immutability or protection
against the owner rewriting evidence. External immutable custody and enforced
365-day/off-site retention cease to be prerequisites for this non-Production
qualification. Production custody and activation remain a separate explicit
owner decision. Lifecycle clock, secret, tenant and data-plane security checks
remain technical requirements.

Effort: medium for policy/consumer migration, with additional work for missing
collectors. Risk: reduced independent assurance, explicitly accepted by the
owner. No live completion date is promised before the tools and target exist.
Rollback of working runtime code and MVP reduction are unnecessary.

## 4. Detailed changes

| Artifact / section | Old requirement | Proposed replacement |
| :--- | :--- | :--- |
| Story 27.4 spec 14, Boundaries and Tasks | “independent C5/C6 decisions”; authenticated operational grants before execution | Named sole-owner decisions after automated verification; one bounded run approval with distinct read, fault/purge and close-out scopes. |
| Interchange authority/session contracts and P1–P4 packet | Independently enrolled issuer, status/time service, producer/reviewer separation and external custody | Versioned owner policy and run record; exact target observations; current local approval/revocation checks and bounded session; private hashed evidence with disclosed local trust. |
| C1 gate and bundle decisions | Separate human reviewer and two authenticated bundle-role receipts | One owner accepts the C1 result summary. Tools retain distinct gate and role records referencing that acceptance, in dependency order, with explicit self-review disclosure. |
| C1.25, C5 and C6 | Independent Security acceptance and separate reviewers | Owner security risk acceptance; distinct C5/C6 result records accepted by the same owner. No independence claim. |
| Epic 27 / ADR / architecture / runbook | Institutional role and external-evidence handoff | Owner is accountable for Operations and Security; agent/tool owns collection, verification and retained records. |

After adoption, amend spec 14 in place and record the renegotiation. Re-derive
the interchange kernel through `bmad-spec` and append its canonical memory;
do not hand-edit `SPEC.md`. Preserve old policies and evidence as history. No new
story or gate is registered by this proposal, and no completed history is reopened.

The operator flow is specified in the [owner handoff](../../docs/operations/access-telemetry-owner-handoff.md).

### Acceptance for implementation

- Given the adopted sole-owner revision and a complete verified C1 result set,
  when the named owner accepts it, then the new consumer accepts the scoped
  predecessor and labels every owner decision as self-reviewed.
- Given a missing, withdrawn, expired or changed approval, wrong target/profile,
  changed evidence, missing collector or failed check, when a consumer runs,
  then it refuses before target mutation. Legacy inputs cannot select this mode.
- Given an approved bounded non-Production test plan, when tests execute, then
  the tools retain actual command/results and verify disabled gate, empty Lease
  and zero lifecycle/clock replicas at finish, including on failure.
- Given passing same-profile C0–C6, terminal and publication evidence, when the
  owner approves close-out, then only the registered A41 paths change. Production
  enablement requires its own later approval.

## 5. Implementation handoff

Scope: Moderate policy and consumer adjustment. Owner: Administrator/Jérôme
Piquot. Implementer: development agent, working in the owning repository.

1. Record owner approval of this exact proposal, including disclosed self-review
   and qualification-only local custody. Update the affected policy/architecture
   and spec wording together; retain the previous versions.
2. Implement the owner-record verifier and strict consumer version dispatch in
   the existing tooling. Test the positive owner path and refusal of absent,
   changed, expired, revoked, cross-session, cyclic or incomplete evidence.
3. Implement/register missing gate collectors as focused development slices.
   Produce a readable test plan and discover target facts only under an approved
   read-only scope. The owner approves concrete resource/action lists before faults.
4. Collect and machine-verify C1 evidence, obtain owner result acceptance, then
   run scoped C2–C4. Prepare C5/C6 and terminal evidence for final owner acceptance.
5. Perform governed A41 close-out and publication verification under explicit
   publication authorization. Keep Production writes disabled throughout.

This proposal authorizes no live access, fault, purge, publication or gate pass.
The existing refusal stays active until migration and its tests pass.

## Analysis and verification record

Checklist: trigger/evidence, Epic 27 and successor impact, PRD/architecture/UX,
alternatives, before/after edits and handoff assessed. Owner approval was explicitly received on 2026-10-12.
Sprint mutation is not applicable: no story is added, removed or completed here.
Repository docs and full-document section inventories were inspected; current
architecture AD-17/AD-19 are additional sources for the implementation handoff.

Prior influences: spec 14 and the sole-owner policy draft are
`historical-reference-only` policy evidence. The current refusal functions are
`current-narrow-pattern` denial tests. The broad twenty-five-gate delivery plan
is an `anti-template` for implementation scope; use it only as a requirement
inventory, not as one bundled implementation story.

| Quoted current claim | Re-runnable inspection | Verdict |
| :--- | :--- | :--- |
| “The named owner may hold both reviewer roles, but never a producer role.” | `cat tools/access_telemetry_c1_approval_policy.py` — producer overlap raises `bundle-producer-self-approval`. | confirmed |
| “The proposed C1.15-only packet is not enrolled.” | `cat tools/access_telemetry_c1_authority_boundary.py` — ends with `authority-prerequisites-unapproved`. | confirmed for the deployed boundary |
| “Current source permits only C1.15/C1.16 capture modes.” | `rg -n 'ValidateSet' tools/verify-access-telemetry-c1.ps1` | confirmed |
| “Production lifecycle writes remain disabled” | `cat deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` — both lifecycle Deployments have `replicas: 0`. | confirmed for repository configuration; live target not inspected |

These policy replacements are proposed future intent, not claims of implemented
behavior. No unverifiable external fact is used to grant access. Source inspection
and pre-existing-diff receipt: `/tmp/story-27-4-sole-owner-proposal-xnfneite`.

## Owner approval, 2026-10-12

The owner answered **“yes”** to: “Approve this sole-owner policy, including self-review and private local evidence?” The approved revision is `sole-owner-qualification/2026-10-12`. This is the project owner’s policy decision, recorded from the conversation; it is not an independently authenticated third-party receipt. It authorizes repository policy and verifier implementation. Each live run still needs its concrete target/action/window approval; publication and Production activation remain separately scoped.
