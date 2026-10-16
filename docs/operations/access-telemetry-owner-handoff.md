# Access-telemetry qualification: owner handoff

**Platform ownership correction, approved 2026-10-16:** Platform owns the local environment and Builds its existing runner lifecycle; Memories owns retention assertions. Functional preparation currently refuses before input reads/writes, spawn or contact. VM boot/disks and fixed 40/56-GiB requirements are historical. Follow [Spec 14](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) and [the report](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md) for owning gaps and reopen checks. No operational approval is ready; full/Production gates and old budgets remain unchanged. Earlier VM wording below is historical for this functional route.


The owner approved the sole-owner policy on 2026-10-12 and the simplified delivery plan on 2026-10-16. Use `github:user:6775094` and disclose `sole-owner/self-reviewed`. No repeat policy or presented-offline-result approval is needed.

- **Current specification and execution plan:** [existing Story 27.4 Spec 14](../../_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md).
- **One consolidated results report:** [retention verification evidence](../../_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md).
- **Operating procedures:** [lifecycle runbook](access-telemetry-lifecycle.md) and [PostgreSQL appendix](access-telemetry-adapter-production.md).

The agent implements and verifies the fixed functional path, prepares its exact source/image/setup bindings and finite resource/action/window/retry/restoration plan, then presents **one operational approval**. That approval covers the enumerated setup, test and cleanup, including permitted transient retries. A changed or exhausted scope requires a new decision; an unchanged step does not. Do not approve an unfinished setup or fixture as if it were a runnable target plan.

The first run uses one writer and two 125-record cohorts at real one-hour retention. It deliberately uses the existing development UTC sources and source-built Debug image digests; signed attestation validation and real Dapr/PostgreSQL/actor behavior remain. These are disclosed functional limitations, not independent UTC, full image provenance, capacity, fault, seven-day, Production or A41 qualification.

After a run, accept its measured report. For later full qualification, accept the completed C1 batch before downstream tests, then the final C0–C6/cleanup result set. Tools generate distinct gate/role/C5/C6 records from those actual decisions in dependency order. The owner does not manually write gate packets or pre-accept future outcomes.

The original and recovery attempts remain closed. The separate boot-only proposal is unapproved; consolidation does not execute it. Existing policy/profile decisions persist. Current implementation approval grants no workload/VM start, active permission/status change, storage mutation, A41/sprint transition, Production action or Git/publication authority.

<details>
<summary>Historical owner guide and inspection contracts, retained in place</summary>

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

The offline [owner-plan dispatcher](../../tools/access_telemetry_c1_owner_dispatch.py)
checks the retained plan's exact bytes against its outside approved digest and
binds the current inspection record. Its seven required plan fields are
`recordSha256`, `policySha256`, `sessionId`, `sourceCommit`, `targetSha256`,
`custodyRoot` and `statusPath`. The status path must already be canonical and
strictly inside the custody root. Other plan fields grant no authority in this
slice. The caller re-reads the current record before each dispatch; the dispatcher
receives the record bytes and cannot establish their freshness from a record path.

The fresh status document contains exactly `planSha256`, `recordSha256`,
`sessionId` and boolean `withdrawn`. Its three keys must match this plan, record
and session. Missing, unreadable, malformed or mismatched status refuses; no
cached `withdrawn: false` substitutes for a current read. A matching false yields
only the requested action's `OwnerRunInspection` after its window checks, with
the `sole-owner/self-reviewed` disclosure. It grants no execution, gate acceptance,
checkpoint, publication or A41 close-out.

The dispatcher requires a filesystem supporting POSIX directory descriptors,
non-following opens and descriptor-relative metadata checks, and nonblocking
opens of a regular status file. Symlink components and unsupported operations
refuse. It bounds the read, checks stable file metadata and revalidates the named
file and ancestor chain; concurrent replacement, removal, in-place rewrite or
ancestor movement refuses without following an outside target. Status writers
write a complete separate file in the same directory as `statusPath`, under the
private custody root, and atomically replace `statusPath`. A concurrent dispatch may refuse and must retry with a
fresh read; never rewrite the current status in place. The full contract is in
[owner run records](../../_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md#offline-owner-plan-dispatcher).

The offline [file wrapper](../../tools/access_telemetry_c1_owner_custody.py)
provides `dispatch_owner_run_files(plan_path, approved_plan_sha256, *,
custody_root, now, action)`. It reads the plan, its current record and its current
status anew for each call. The plan adds `recordPath`; the existing byte API
keeps its original seven required bindings. The caller's custody root must
exactly equal the approved plan's `custodyRoot`. Plan, record and status must be
distinct canonical paths strictly below that root; the outside approval digest
pins exact plan bytes before the other locations are read.

The wrapper checks effective OS-user ownership and no group/other permissions on
the three named artifacts and their directory chains at/below the custody root,
including that root. It checks regular single-link files and distinct file
device/inode identities; it does not enumerate other custody descendants. Private
directory/file modes such as
`0700`/`0600` satisfy the permission condition. All ancestors are traversed
without following symlinks; public ancestors above custody retain identity checks
without the private-mode requirement. OS ownership proves local custody only;
it does not authenticate the GitHub policy principal. The initial read and final
readback each consume at most `MAX_ARTIFACT_BYTES + 1` per artifact; oversized
documents refuse. File modification/change times and private-directory change
times must be available as integer nanoseconds; missing/wrong-type values refuse.

All file and ancestor descriptors remain held through dispatch. Before returning,
the wrapper seeks and rereads the held plan and record, then status last, and
compares each bounded readback with its originally inspected exact bytes. Each
readback also revalidates named identities and required metadata. This detects
same-size rewrites even when timestamps repeat; withdrawal during the plan or
record final checks is included in the final status readback. Observed replacement,
removal, rewrite, owner/mode changes or ancestor movement refuses with no
inspection. Unrelated file/directory creation or removal above custody does not
invalidate unchanged private inputs; directory identity excludes link-count churn.
File/parse/unsupported-filesystem failures expose only a
content-free `OwnerRunCustodyError` code after internal handlers and cleanup;
existing `OwnerRunInspectionError` refusals keep their type and code. Write
complete private replacement files and atomically replace the named files;
never rewrite them in place. After any refusal, retry only with a fresh complete
call and current approved bindings. These sequential checks establish local
point-in-time evidence ending with status readback/validation. They do not provide
a simultaneous filesystem snapshot, immutability or a continuing lock after return.
The [file-wrapper contract](../../_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/owner-run-records.md#offline-current-record-and-private-custody-wrapper)
documents the remaining execution requirements. The result remains only the
requested `OwnerRunInspection`, labelled `sole-owner/self-reviewed`; it grants
no execution, gate, checkpoint, publication or A41 authority.

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

For a run, approve the exact prepared plan after the agent shows it. The adopted
context is `owner@memories-qualification-27-4`; C1 uses `hexalith-memories`, while
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

</details>
