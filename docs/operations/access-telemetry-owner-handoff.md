# Access-telemetry qualification for the sole owner

Status: Owner-approved policy, 2026-10-12, under the
[sole-owner change proposal](../../_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-12-sole-owner-qualification.md).
The current tools still refuse live qualification. This guide issues no grant.

The owner subsequently said “I approve and accept”: the policy and the reported
25 focused / 267 full offline test results are accepted. The
[retained acceptance record](../../_bmad-output/implementation-artifacts/27-4-owner-approval-and-acceptance-2026-10-12.md)
binds that measured work and the subsequent read-only readiness observations.
No repeat policy or offline-result approval is needed; future live results
remain subject to actual execution and review.

## What you provide

1. **Policy approved.** You are the accountable owner for development,
   Operations and Security. You may accept evidence you produced. Results say
   “sole-owner/self-reviewed,” with no independent-review claim. For these
   non-Production tests, evidence stays in a private owner-controlled directory
   with hash manifests and readback checks; no separate receipt service is needed.
2. **Approve the prepared test plan.** Confirm the intended test cluster and
   permitted resources/actions. The agent discovers and presents identities,
   credentials-file metadata, evidence path, test duration and cleanup plan.
   Read-only discovery, faults/purge and publication have explicit separate
   scopes in the prepared plan. Shared Dapr faults name the affected resources
   and impact before approval. Production enablement is a later decision.
3. **Accept the results.** Review a short C1 summary before downstream tests,
   then the C0–C6 and cleanup summary before close-out. The agent generates the
   bound gate, role and checkpoint records from your approvals. You do not
   manually write twenty-five gate packets or invent evidence of passing tests.

## What the agent and tools provide

The agent prepares a full approved plan plus a separate closed inspection record.
The full plan contains the exact policy,
source revision, PG2 profile/workload hashes, observed cluster/namespace/resource
identities, allowed read and mutation actions, session/window, local withdrawal
state, private evidence path and final cleanup requirements. You review its
plain-language plan; machine fields are generated from actual observations. The
inspection record carries only its documented subset. The plan binds its exact
bytes, and later execution consumers must verify both artifacts.
Context labels alone never prove target identity.

Tools implement and run the collectors, retain secret-safe command/results and
SHA-256 manifests, verify each of the twenty-five technical gates, and report
failures. Missing collectors are development tasks assigned to the agent. Old
captures and offline fixtures do not establish new live passes.

The agent stops on scope drift, failed checks, expired/withdrawn approval or
missing cleanup. Final state must show disabled gate, empty Lease and zero
lifecycle/clock replicas. A41 closes only after required behavioral, terminal
and publication verification; the policy approval alone completes no gate.

## How to give approval

The owner approved this policy on 2026-10-12 by answering “yes”. The recorded
approval covers sole-owner review and private local evidence. No repeat policy
approval is needed. The approved direction is:

> Approve the sole-owner qualification policy. I accept self-review and private
> hashed local evidence for non-Production qualification. Keep Production
> activation separate.

The agent retains that decision in a versioned project record. This replaces the
qualification-specific institutional approval requirement only after the policy
and consumers are migrated. It does not impersonate an authenticated third-party
receipt, prove external immutable custody or grant unspecified target actions.

For a run, approve the exact prepared plan after the agent shows it. The candidate
context is `jpiquot@local`; C1's historical namespace is `hexalith-memories`, while
C2–C4 require an isolated namespace ending `-qualification`. These are candidate
labels to resolve and approve, not permission to contact or mutate them.

## First target access and failure handling

First, the agent prepares a bounded read-only discovery plan from local client
metadata. You approve its selected context/API identity, allowed namespace/resource
reads and time window before any target contact. This permits discovery only.
After observing actual cluster/namespace identities, the agent prepares the concrete
qualification and mutation plan for your approval. No opaque identity or digest
needs to be supplied manually by you.

The qualification plan includes bounded failure restoration separately from test
actions. If approval expires or is withdrawn, tests stop; only previously approved
restoration of resources still owned by that run may finish. Cleanup retains the
private evidence archive through result, terminal and publication checks.
