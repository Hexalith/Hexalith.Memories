# Approved Platform ownership correction — Story 27.4

**Status: approved for repository implementation and offline preparation.** The owner replied **“approve”** on 2026-10-16 to the Platform-ownership proposal preserved verbatim below (SHA-256 `95e690608f25e550778588459cc1a46cd02a46904afd2e2f1708c4b42e4dc8db`). This explicitly renegotiates the earlier frozen environment intent. Hexalith.Platform owns composition and environment lifecycle; Memories owns retention assertions. Existing sole-owner policy persists.

No operational execution, server contact, storage allocation/formatting, credential issuance, active permission change, dependency update, Git mutation, A41/sprint closure or Production activation is authorized by this decision. The selected Platform path must be concrete and verified before its one operational request. Any discovered implementation gap remains a named blocker rather than a substitute environment or fabricated readiness claim.

[Existing current story](../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md) and [one results report](../implementation-artifacts/tests/27-4-retention-verification-evidence.md) retain authority. Proposal checklist 6.3 is now satisfied by this actual response; proposal-time status below remains historical.

<details>
<summary>Platform proposal exactly as presented for approval</summary>

# Sprint Change Proposal: Story 27.4 consumes Hexalith.Platform

Date: 2026-10-16. Mode: **Batch**. Status: **Proposed — implementation approval pending**.
This is a revision of the existing proposal, not another story or numbered implementation specification.
Memories baseline: `61074e981bd92019488db19da0c8572bd4bf8b97`; inspected Platform submodule: `55bee680e6901a45946dd8bf1e4a4060952bd7ea`.
Owner: Jérôme Piquot / `github:user:6775094`; existing non-Production assurance remains `sole-owner/self-reviewed`.

## 1. Issue summary and decision requested

**Make Hexalith.Platform own the test environment; make Memories own the retention test.** Amend existing Story 27.4/Spec 14, retain its one verifier entry point and one results report, and retire the Memories-owned VM/bootstrap route from the functional test's prerequisites.

The owner clarified that infrastructure is managed by the Platform module. The functional slice nevertheless inherited a 40-GiB VM, a 56-GiB host-memory admission floor, raw-disk verification, QEMU/SSH bootstrap, Kubernetes installation and private infrastructure provisioning. The approved run rejected before boot on the OS-hash deadline. Its native-read correction passed local checks, but the replacement remained held for RAM. Neither produced live retention evidence. These are environment/preparation outcomes, not failures of the retention implementation.

Platform already declares the Memories server, clock and lifecycle service. Its contract also settles two points that the earlier container/server advice omitted:

- **FR-4 / AD-4–AD-5:** Platform owns local/CI environment lifecycle and supplies identified endpoints/composition. Memories source runs as Platform evidence from the Platform workspace. Tests do not create their own AppHost or infrastructure.
- **FR-4 / FR-10:** hosted attachment is refused. The designated hosted installation is `192.168.1.30`, through Platform's staging/release controls. A manually created test namespace is not an automatic exception.

Approve one direct adjustment: prepare the first real retention test through **Platform's local, run-owned minimum environment**. Preserve real PostgreSQL for this slice; the current Platform Redis telemetry configuration is not equivalent evidence. Keep hosted staging on `192.168.1.30` as the later Platform-controlled deployment path. If hosted execution is preferred first, revise this choice explicitly rather than silently changing Platform's attachment policy.

This request approves repository implementation and offline preparation only. It grants no workload start, server contact, infrastructure/storage mutation, credential issuance, active permission change, Git mutation, Production activation or A41 closure. Standing owner decisions are reused. A later single operational approval must cover the complete concrete Platform-owned run after its inputs and limits are ready.

## 2. Impact analysis

| Area | Impact |
| :--- | :--- |
| Memories Epic 27 / Story 27.4 | Keep the current story, checkpoint ownership and technical outcomes. Replace environment ownership and the functional execution route. The current story remains `in-progress`; A41 remains open. |
| Memories PRD FR67/NFR34 | No product requirement, MVP scope, retention range or fail-closed Production-admission requirement changes. |
| Memories architecture | Clarify that “technical platform module” does not assign shared infrastructure lifecycle to Memories. Preserve its runtime services, application declarations, backend semantics and reusable Aspire integration package. |
| Platform Epics 1/2 | Reuse declaration/composition, workspace mapping, descriptor, provisioning and outcome/cleanup contracts, including Stories 1.5/1.6/1.10 and 2.2/2.4/2.5. Verify their current implementation; story text is not readiness proof. No new story is registered by this proposal. |
| Platform Epics 4/5 and FR-10/NFR-3 | Hosted staging and exact-release E2E remain separately gated. Existing cluster-management/staging blockers are not waived by this correction or Memories' sole-owner policy. |
| Memories Epics 31 and 35 | Consume Platform-owned infrastructure qualification where applicable; retain module-specific integration/security checks and exact PostgreSQL/OpenBao/image evidence. Do not reopen completed stories or count this functional test as their qualification. |
| UX / public APIs | No screen, product flow, wire contract or accessibility change. Operator handoff becomes shorter: Platform supplies the environment; Memories consumes it and reports behavior. |

Current preparation gaps are explicit:

1. Platform's reference telemetry component is `state.redis`; the selected PostgreSQL retention behavior has not been demonstrated there. Extend the existing Platform-owned composition/profile, reusing qualified module integration seams, rather than accepting Redis as PostgreSQL evidence.
2. A standalone Platform workspace exists at `/home/administrator/projects/hexalith/platform`, HEAD `c85198310635b91166df70eba25ba400260b020b`. It differs from the inspected submodule. The active Platform revision, tool version and directly declared Memories candidate must be selected and bound before execution. No sibling-source fallback, nested-submodule initialization or automatic dependency update is authorized.
3. The inspected Platform checkout has no `.config/dotnet-tools.json`. Tool-pin/descriptor readiness must be established through the existing Platform/Builds workflow; do not invent a replacement runner or claim that an AppHost declaration alone is ready integration evidence.
4. Hosted readiness is unverified. Platform's dated staging handoff reports outstanding gates; this turn performs no cluster observation. Missing hosted readiness cannot be bypassed with a qualification namespace.
5. The approved native-read VM bundle remains held, unused and unclaimed. Its source map now differs in three dependency `.mcp.json` files. Preserve those user changes and the original approval; do not replay or silently rebind it.

## 3. Recommended approach and assurance boundary

**Direct adjustment; moderate cross-repository scope.** Reuse the existing Platform composition/runner and existing Memories behavioral collector. Do not roll back completed stories, transplant the VM framework into Platform, introduce kind/QEMU infrastructure, create a new receipt service, or add another 27.4 specification.

The smallest implementation slice is one Platform-owned local composition containing the declared Memories server/lifecycle/clock and their required real dependencies, with run-owned PostgreSQL configured for the chosen Dapr component and the existing test connected through the Platform descriptor. Keep other services only where the declared dependency/readiness contract requires them. A separate VM and Kubernetes are not prerequisites for that local integration route.

Preserve the currently selected **Dapr 1.18.1 / PostgreSQL 18.6 immutable image identities** for the PostgreSQL experiment; the existing functional asset catalog and approved KVM base record supply those references. If Platform cannot support those inputs, report the concrete incompatibility for an owner decision rather than silently changing versions. Derive a truthful **local functional profile identity** through Platform; do not reuse the KVM target/profile hash or manufacture C1 eligibility for the new environment. Full Production admission remains closed.

Use Platform's active-root mapping and local Debug/source rules. Bind what actually ran. Development evidence does not become CI candidate acceptance, package qualification or hosted release evidence; those retain their committed candidate, Release/NuGet and artifact-identity rules. Use Platform/Builds' existing descriptor/result schemas and lifecycle ownership, not a new Memories environment format or command executor.

| Change | Technical assurance effect |
| :--- | :--- |
| Platform owns provisioning, resource admission, credentials and teardown | Ownership/procedural correction. Retention assertions remain required; infrastructure observations are referenced from Platform evidence. |
| Remove VM boot, raw-disk hashing and fixed 40/56-GiB requirements from the first functional route | Removes the VM-specific test environment. It earns no OS, hypervisor, dedicated-disk or capacity qualification. Actual Platform resource limits/readiness still must pass. |
| Use Platform local Dapr and local secret-store conventions | Explicit functional coverage change: this run will not exercise Kubernetes injection/RBAC/Lease objects or OpenBao secret delivery. Preserve signing-key custody, real ECDSA/nonce/expiry validation and the declared data-plane security settings. No full security gate is discharged; OpenBao/Kubernetes coverage remains in hosted/full qualification. |
| Keep PostgreSQL/Dapr backend, startup capability checks, strong reads and real time | No retention/backend coverage reduction. Do not fall back to Redis, mocks, SQL writes/deletes, fake clocks, backdating, public test endpoints or capability bypasses. Preserve PostgreSQL TLS/identity requirements in the selected fixture; inability to provide them is a preparation gap. |
| Adopt Platform's outcome-aware cleanup | Success requires observed cleanup of owned resources. Failed local runs retain surviving resources and diagnostics according to FR-4, with test traffic quiescent and retained state reported; CI cleanup and withdrawn/cancelled outcomes follow Platform. Retention is not a cleanup pass. Full Kubernetes terminal predicates remain mandatory where applicable. |
| Defer full capacity, long-duration, fault and Production campaigns | Scheduling separation only. Keep all 25 C1 gates, full C0–C6, two-writer C2, real 1/24/168-hour C3, attributable allocator reuse, recovery/privacy checks and exact-profile adoption/publication obligations. |

Resource sizing must come from the selected Platform profile's actual requests, startup observations and aggregate headroom. Do not invent a smaller RAM number or merely lower the old admission floor. Record container limits, ephemeral-storage/data limits, shared-capacity protection and measured peak use before requesting execution.

Retain the existing four-hour upper bound for the first functional run and 15 minutes for bounded stopping/diagnostic capture; use Platform's default ten-minute startup deadline unless its configuration carries a justified finite override. The operational plan must bind an absolute window, Platform run/owner identities and sufficient run lifetime for real expiry. Reads remain finite; uncertain cohort submission reconciles the same run/segment/time identity with at most two submissions. No blind mutation retry or budget/deadline reset is permitted. Platform controls resource retention and any startup retry, with the exact allowance disclosed in that one run plan.

Estimate: medium implementation effort across the existing composition/profile and consumer boundary; no credible live-test date until descriptor, PostgreSQL profile and resource readiness are observed. Main risks are source/artifact mismatch, quietly substituting Redis, and accidentally claiming local evidence as hosted/full qualification. Avoiding another infrastructure bootstrap reduces setup work; it does not establish readiness by itself.

## 4. Specific edits and file disposition

These are proposed edits for approval, not already-applied implementation changes.

| Affected artifact | Before → proposed after | Reason / disposition |
| :--- | :--- | :--- |
| [Existing Spec 14](../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md), Intent/Code Map/operational envelope | “One current plan … fixed local KVM/SSH target” and 40/56-GiB prerequisites → Platform-owned local minimum environment; Memories consumes the existing descriptor and owns behavioral assertions. Keep the old target/approval sections explicitly historical. | **Keep and amend** the one authoritative story; explicitly renegotiate its frozen environment intent on approval. |
| [Epics](epics.md), Story 27.4 current guidance | “under the current approved KVM profile” as the universal route → functional evidence uses the declared Platform local profile; full qualification needs a separately adopted exact Platform target/profile. | **Amend** source planning with the same ownership decision; keep checkpoint IDs and historical acceptance text. |
| [Current architecture spine](architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md), Design Paradigm/AD-1/AD-17/Environments | “AppHost, ServiceDefaults, deployment manifests, and reusable hosting integrations … platform responsibilities” → distinguish module-owned services/extensions/declarations from **Hexalith.Platform-owned composition and infrastructure lifecycle**; local/CI/hosted evidence follows Platform contracts. | **Clarify** ownership; preserve AD-19/AD-20 dependency and release evidence requirements. |
| `tools/verify-access-telemetry-lifecycle.py`; `tools/access_telemetry_functional_retention.py`; existing functional tests | `functional-retention` compiles/launches a VM and owns setup → same entry/checkpoint consumes the existing Platform descriptor and lifecycle result, verifies exact app/backend identities, and evaluates the retained behavioral assertions. | **Keep/merge** semantic checks; remove infrastructure authority and reject the old functional VM input schema as a current route. Full validators remain strict. |
| `tools/access_telemetry_functional_target.py`; `tools/access_telemetry_functional_guest.py`; VM-specific tests and private bundles | Active QEMU/SSH/guest installer route → historical implementation/evidence after the Platform consumer works. | **Retire from active execution; archive in place.** Do not migrate the bootstrap framework wholesale into Platform or delete disks, keys, approvals, failure receipts or source snapshots. |
| `Directory.Build.props`, `Directory.Build.targets`, Server qualification-curl target, CI workflow | Existing guarded local archive lane and registered tests → retain safety fixes where still consumed; update only affected functional test discovery for the Platform consumer. | **Keep** source/package/Release/publication refusal boundaries; no broad revert or dependency upgrade. |
| Platform `src/Hexalith.Platform.Composition/{PlatformAppModel.cs,PlatformCompositionOptions.cs,PlatformLocalProfile.cs}`, `Declarations/{memories.json,local-profile.json}`, `DaprComponents/reference/`, composition tests | Existing declared three Memories services and Redis telemetry profile → the smallest declared, run-owned PostgreSQL functional profile and verified startup/resource/result bindings, reusing existing options/extensions. | **Extend in the owning Platform repository only where required.** Do not edit generated declarations without their owner/provenance workflow or change unrelated consumers' default backend. |
| Platform/Builds runner/descriptor implementation under existing Stories 2.2/2.4/2.5 | Required existing lifecycle/result contract → demonstrate current tool pin, descriptor validity, ownership, readiness, lifetime and outcome rules; repair a demonstrated gap in its owner repository. | **Reuse** the existing contract; an implementation gap blocks readiness instead of creating a Memories fallback. |
| [ADR](../../docs/dev/adr-27.1-001-access-telemetry-lifecycle.md), lifecycle/adapter runbooks, owner handoff, interchange kernel/companions | Current KVM route and Memories-owned setup guidance → Platform environment boundary and local-versus-hosted/full evidence labels, linking the same story/report. | **Merge** current instructions; retain old target/profile/policy decisions as history. Re-derive kernel changes through its existing skill only after approval; stable IDs/memory remain. |
| [Consolidated report](../implementation-artifacts/tests/27-4-retention-verification-evidence.md), two dated execution/target plans, bootstrap/recovery records, all older numbered specs | Current VM resource hold → adoption note redirects future functional preparation through Platform; VM results/approval/budget states remain dated facts. | **Keep** one human report and historical bytes. No new report, replacement story, numbered spec, A41 or sprint transition. |
| Platform PRD/architecture/epics; Memories PRD and DESIGN/EXPERIENCE | Existing product and environment contracts → unchanged requirements; implementation handoff uses their existing owners/work. | **Keep**; no product/UX replan, hosted-policy waiver or new epic is needed. |

## 5. Implementation order, acceptance and approval boundary

1. **Adopt this correction in the existing Memories planning artifacts.** Preserve the earlier approved proposal and raw evidence, record the ownership/profile-intent change, and mark VM boot as outside the functional critical path. The unused VM budget is not consumed, withdrawn or transferred by rewriting history.
2. **Platform maintainer establishes one supported local run.** Work from the selected Platform root, bind its direct Memories source mapping/tool identity, and extend only its existing minimum environment for real PostgreSQL and the required lifecycle/clock configuration. No nested initialization, shared-cluster change or separate bootstrap framework.
3. **Memories developer adapts the existing functional consumer.** Consume Platform endpoints/identities and test-scoped observation access. Keep normal logging/acknowledgement, strong Dapr reads, safe fixed read-only PostgreSQL corroboration, index/reminder progress and unchanged newer-cohort comparison. No caller-supplied shell/SQL/executor/URL bypass.
4. **Validate and prepare the one operational request.** Test missing/foreign/stale descriptors, wrong candidate/backend, insufficient run lifetime, ownership conflicts, partial startup, withdrawal, bounded retry, cleanup failure and correct local retention outcomes. Bind actual sources, artifacts, resources, commands and absolute window. Complete preparation before asking for its operational approval.
5. **After that operational approval, execute and report observed behavior.** Import Platform lifecycle/diagnostic references into the existing results report; retain all failures. Retire active VM dispatch only with the replacement consumer and refusal tests in place. Full campaign, Production and A41 remain separate.

Acceptance for the implementation slice:

- [ ] One existing Story 27.4 and one existing report govern the work; no new numbered specification, duplicate runner, provisioning script or acceptance ledger is introduced.
- [ ] Platform is the only environment lifecycle owner. The test consumes its existing descriptor, proves the intended Memories/PostgreSQL/Dapr identities and readiness, and reports local development evidence honestly.
- [ ] No missing PostgreSQL/profile/descriptor capability is satisfied by a Redis substitution, a VM fallback or hosted attachment. Workspace/tool/source gaps are named preparation blockers.
- [ ] One writer produces cohorts A/B of 125 acknowledged records each, at least 1,800 real seconds apart. A is readable before its 3,600-second expiry, absent by expiry plus 900 seconds, with required due-index cleanup and durable reminder/purge progress; B's IDs/content hashes/accepted times/expiry remain unchanged. Both monotonic elapsed time and UTC observations are required.
- [ ] Expiry attribution is supported by positive evidence: strong absence with the exact expired physical tuple still present, or an explicit actor deletion receipt. A disappeared tuple or ambiguous/missing evidence cannot pass. No synthetic time or direct backend mutation drives the result.
- [ ] Success includes observed Platform-owned cleanup and stopped test traffic. Failure/withdrawal follows Platform's actual retention/result policy, records leftovers and cannot become a pass. Full Kubernetes gate/Lease/replica terminal proof remains required for the full hosted campaign.
- [ ] The final report records measured resource use, exact limits and approvals, target/source/artifact identities, all observations and outcome. It grants no full capacity, long-duration, independent UTC, OpenBao/Kubernetes security, CI candidate, Production or A41 credit.

**Handoff:** moderate coordination between the Platform maintainer (environment/profile/runner readiness) and Memories developer (module integration, behavioral test, runbook/report). The same owner may fill both roles under the already adopted non-Production policy; no independence claim or repeated standing-policy decision is needed. Builds receives only a demonstrated gap in its existing runner implementation. No live handoff message or remote action is sent by this proposal.

### Verified claims and unresolved readiness

Verified 2026-10-16 against the worktrees and private local records; commands are read-only. Requirement/acceptance statements above are desired end states, not claims of completed behavior.

| Quoted claim / source | Re-runnable check | Observation / verdict |
| :--- | :--- | :--- |
| “Platform owns the integration environment lifecycle” — Platform FR-4 | `sed -n '114,149p' references/Hexalith.Platform/_bmad-output/planning-artifacts/prds/prd-platform-2026-09-27/prd.md` | **confirmed** requirement; no live lifecycle pass inferred. |
| “Attachment to hosted environments is refused” — same source | Same FR-4 read | **confirmed** policy; ad hoc use of the server is not the local integration route. |
| “Memories … qualified as Platform evidence only from the Platform workspace” — Platform AD-4 | `sed -n '95,113p' references/Hexalith.Platform/_bmad-output/planning-artifacts/architecture/architecture-platform-2026-09-27/ARCHITECTURE-SPINE.md` | **confirmed** source/mode and evidence boundary. |
| “designated installation at 192.168.1.30” — Platform FR-10 | `sed -n '288,302p' references/Hexalith.Platform/_bmad-output/planning-artifacts/prds/prd-platform-2026-09-27/prd.md` | **confirmed** intended hosted target, not current readiness. |
| “declares the Memories server, clock and lifecycle service” | Read `references/Hexalith.Platform/src/Hexalith.Platform.Composition/Declarations/memories.json`, `modules[0].identity.servers` | **confirmed** three declared services; declaration is not an executed test. |
| “reference telemetry component is state.redis” and local secrets are file-backed | Read Platform `DaprComponents/reference/{telemetry-state,secretstore}.yaml`; `rg -n 'state.redis|secretstores.local.file' references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformAppModel.cs` | **confirmed** current local defaults; PostgreSQL functional readiness is not demonstrated. |
| “no .config/dotnet-tools.json” in the inspected Platform checkout | `test -f references/Hexalith.Platform/.config/dotnet-tools.json` | **confirmed** absent at inspection; do not infer an installed/supported runner from documentation. |
| Standalone and submodule Platform identities differ | `git -C /home/administrator/projects/hexalith/platform rev-parse HEAD`; `git -C references/Hexalith.Platform rev-parse HEAD` | **confirmed** `c85198310635b91166df70eba25ba400260b020b` versus `55bee680e6901a45946dd8bf1e4a4060952bd7ea`; alignment is preparation work. |
| “Dapr 1.18.1 / PostgreSQL 18.6 immutable image identities” | Read `tools/access_telemetry_functional_retention.py:artifact_catalog` and `docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json` | **confirmed** selected references; compatibility and execution in the proposed Platform local profile remain preparation gaps. |
| “56-GiB host-memory admission floor” and “40-GiB VM” | `rg -n 'memoryBytes|hostStartAvailableBytes' tools/access_telemetry_functional_retention.py` | **confirmed** configured limits, not measured functional need. |
| “replacement remained held … unused and unclaimed” | Read native bundle `status.json`, `actual-owner-decision.json`; check absence of `run.claim` under the retained path in the current report | **confirmed** approval exists, executable authority false, budget unconsumed; no VM/retention pass. |
| Three dependency `.mcp.json` source-map differences | Compare `source_files(Path.cwd())` from the existing functional helper with that bundle's `plan.json:sourceFiles` | **confirmed** EventStore/FrontComposer/Tenants local configuration drift; preserve user changes. |
| “Story 27.4 remains in-progress” / “A41 remains open” | `rg -n '27-4-retention|20.5-A41' _bmad-output/implementation-artifacts/sprint-status.yaml`; read current consolidated report | **confirmed** tracking/report posture, not technical completion. |
| “FR67/NFR34” product outcomes | Read Memories PRD FR67/NFR34 and current spine AD-17 | **confirmed** normative requirements retained; no implementation-complete assertion. |
| Hosted capacity, classification, compatible runtime, isolation and staging readiness | Fresh Platform-owned bounded inventory and readiness evidence, after its execution is authorized | **unverifiable here:** no server contact in this correction. Platform owner; blocks hosted execution; reopen on fresh Platform observations. Dated staging handoff is context only. |
| Live 250-record PostgreSQL retention, resource fit and cleanup on the proposed Platform route | The later approved Platform run and its descriptor/result plus module observations | **unverifiable here:** not executed. Platform/Memories owners; blocks functional acceptance; reopen after preparation and the actual bounded run. |

### Scope guard and checklist disposition

Historical influence: the current three-service declaration and Platform composition/descriptor are `current-narrow-pattern` for ownership/configuration only; their live PostgreSQL readiness is unverified. Prior approvals, VM allocation/read failures and profile hashes are `historical-reference-only`. Accumulated numbered specs, the Memories QEMU/guest-install route and any whole infrastructure programme copied into Story 27.4 are `anti-template`; retain evidence without copying their execution shape. **Slice proof:** one real retention experiment consuming a Platform-owned environment; full qualification remains the existing explicitly owned checkpoint inventory.

- [x] 1.1–1.3: Story 27.4 trigger, ownership misunderstanding/failed environment approach and measured evidence identified.
- [x] 2.1–2.5: current/future epic effects assessed; direct adjustment within existing stories, no new epic, story or resequencing of full technical gates.
- [x] 3.1–3.4: PRD unchanged; architecture/operations/environment-consumer changes identified; CI/evidence ownership retained.
- [N/A] 3.3 product UX edits: no screen, accessibility or product interaction changes; only operator routing changes.
- [x] 4.1/4.4: direct adjustment selected, moderate effort/risk, concrete single-slice rationale.
- [N/A] 4.2/4.3: no Git rollback, reopening of completed work or MVP reduction proposed.
- [x] 5.1–5.5 and 6.1–6.2/6.5: issue, before/after edits, assurance differences, acceptance and owner handoff prepared; readiness unknowns explicitly assigned.
- [!] 6.3: owner approval of this completed proposal is pending; the requested skill requires explicit approval before implementation.
- [N/A] 6.4: no sprint registration/status change; Story 27.4 and A41 remain open.

Only this proposal revision is written in this turn. The earlier approved proposal follows unchanged, with original bytes SHA-256 `3e93b40536c01a63e40365df89ef4acd97a9c36b57e744f9614a11e36943b10c`. Source, story, report, Platform files, private decisions/status, disks, workloads and Git state are not changed by this proposal.

## Earlier 2026-10-16 proposal and approval — preserved verbatim

<details>
<summary>Earlier approved VM-based correction and its original proposal</summary>


# Approved course correction — Story 27.4

**Current status: approved for implementation.** The owner answered “yes” on 2026-10-16. The [current execution specification is existing Spec 14](../implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md). Operational execution, active permissions, storage mutation, A41/sprint closure, Production and Git/publication remain separately held.

The proposal as presented is preserved verbatim below, including its proposal-time status and observations. Its exact approved-byte digest is recorded in the owner decision after it.

<details>
<summary>Original proposal as presented for approval</summary>

# Sprint Change Proposal: Simplify Story 27.4 execution

Date: 2026-10-16. Mode: Batch, as requested. Status: **Proposed; awaiting owner approval.**
Investigation baseline: `61074e981bd92019488db19da0c8572bd4bf8b97` on `main`.
Owner: Jérôme Piquot, `github:user:6775094`. Assurance: `sole-owner/self-reviewed`.

## 1. Issue summary and decision requested

**Keep Story 27.4, amend its existing Spec 14 in place, use one execution entry point and the existing canonical evidence report, and deliver one small live retention test first.** Preserve full qualification and A41 closure as later checkpoints of that same story. Do not create Spec 27, a replacement story, a receipt service, or another qualification framework.

The delivery process has accumulated overlapping specifications and inspections without an accepting live predecessor path. The latest operational failure was narrower: QEMU rejected `serial` on a raw-drive backend. Correcting that argument is necessary, but another boot-only approval would still produce no retention evidence.

Verified starting position:

- The owner approved sole-owner qualification and private local evidence on 2026-10-12, then accepted the presented offline results. These decisions persist; there is no request to approve that policy again.
- The owner approved the isolated KVM target and profile `95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb`. Its full workload hash remains `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f`.
- The recovery record reports physically allocated 40-GiB OS and 600-GiB data disks, exit 1 before an observed running guest, zero guest metadata commands, and stopped-process checks. Those are dated measurements, not fresh resource reservations or application cleanup proof.
- The original and recovery attempts are closed. Private `boot-only-status.json` has no actual decision and `executableAuthority: false`. The corrected boot-only plan is **unapproved**, even though policy and profile approvals persist.
- Spec 14 has six unchecked live tasks; the sprint row is `in-progress`; A41/DW-17 and its retrospective action are open. No current live C0–C6 completion is established.

Primary evidence: [bootstrap recovery result](../implementation-artifacts/27-4-bootstrap-recovery-result-2026-10-16.md), [target approval](../implementation-artifacts/27-4-isolated-target-approval-2026-10-16.md), [owner acceptance](../implementation-artifacts/27-4-owner-approval-and-acceptance-2026-10-12.md), and [approved sole-owner correction](sprint-change-proposal-2026-10-12-sole-owner-qualification.md).

Approval of **this proposal** authorizes the documentation and implementation changes below. It does not launch anything or grant operational authority. After implementation, the agent presents **one complete, executable functional-run plan** for one operational approval. The missing exact executable/image/setup bindings are agent work, not fields the owner must manufacture. A boot-only approval is not requested separately.

## 2. Impact and recommended approach

Use **Direct Adjustment**. Do not roll back working lifecycle code or reduce the PRD MVP. The moderate scope is a change to delivery sequencing, administrative gate ownership and qualification tooling, with a small first implementation slice.

| Artifact or dependency | Impact |
| :--- | :--- |
| Epic 27 / Story 27.4 | Retain its outcome and open status. Add a functional checkpoint before full qualification. Replace the requirement to register separate story files for every missing C1 gate with an explicitly approved checkpoint table inside this existing story. Preserve each technical gate, attributable evidence and review state. |
| PRD FR67 / NFR34 | No requirement change: bounded, sanitized infrastructure telemetry; Production admission fails closed; telemetry failure does not change domain truth. Functional success supplies no complete NFR34 or G6 verdict. |
| ADR 27.1-001 / current architecture spine AD-17, AD-19, AD-20 | Retain Dapr-only application persistence, privacy, release-lane separation, evidence-only qualification and the non-HA boundary. Add a qualification-tier clarification to the ADR; preserve historical architecture and decisions. |
| Stories 27.1–27.3, 27.21, 27.22 | Preserve their scope, completed history and existing remediation obligations. Historical PG1 and closed-window C1.16 evidence do not become eligible for the KVM session. No completed story is reopened. |
| Epic 34 telemetry/erasure work; Epic 35, especially 35.15 | Consume the same report and its assurance limits. Reuse matching observations only after validating source, target and configuration; no automatic release, erasure or PostgreSQL qualification credit. Other epics need no reordering. |
| Current and legacy UX specifications | No product screen, route, accessibility or interaction change. Simplify the operator documentation and terminal invocation only. |
| CI and runtime | Add focused verification of the selected path and its refusal boundary. Preserve existing required checks and Production defaults. No broad build, dependency or pipeline redesign. |

### Procedural simplification versus technical coverage

| Change | Classification and limit |
| :--- | :--- |
| One current story plan; one report; one top-level verifier | Procedural consolidation. Internal helpers, raw logs and per-gate packets remain when they serve a distinct check. |
| One run approval covering setup, test and restoration; bounded retries already included | Procedural simplification. No approval per command, transient failure, generated packet or unchanged policy. Scope drift still stops execution. |
| One batch C1 acceptance and one final result acceptance for full qualification | Procedural simplification. Tools retain distinct gate, Operations/Security and C5/C6 records, with acyclic evidence dependencies and explicit self-review. One owner response can approve an enumerated batch of existing results; it cannot pre-accept future results. |
| Existing story owns the missing C1 checklist instead of requiring more story registrations | Proposed administrative-policy amendment requiring this proposal's approval. Every gate still needs an owner, executable evidence, review state and completion state. Registration itself earns no pass. |
| One writer, two 125-record cohorts, actual one-hour retention, no injected recovery faults in the first run | **Deliberately smaller first-run coverage.** It proves functional plumbing only. Two-writer throughput, capacity, replacement, backup/restore, fault matrix and long-duration evidence remain required later. |
| Use the existing non-Production development UTC sources for the first isolated functional run | **Explicit first-run assurance reduction.** Keep real signed attestations, nonce/profile/expiry checks and real elapsed time, but do not claim independent UTC-source qualification. This avoids building new UTC services or adding guest egress just to test retention. Full qualification still requires the independent authenticated UTC contract. |
| Use source-built Debug application images identified by digest for the functional run | **Explicit first-run evidence limitation.** These are source-lane functional assets, not signed CI/package/Production evidence. Full image provenance, signing, scanning and AD-19 lanes remain required. |

The approved base target/profile remains the reference. Functional clock, retention and image settings are explicit, hash-bound execution deltas; their outputs are **not** an exact-profile qualification pass. Never label a modified effective deployment as fully qualified under the base hash, relax full-profile equality, or feed functional packets into C1/C0–C6 or A41 consumers.

## 3. One current specification, path and report

### Authority and document disposition

In this table, `S` means `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out` and `I` means `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange`.

| Keep, merge, archive or retire | Affected files | Reason and proposed action after approval |
| :--- | :--- | :--- |
| **Keep and amend as sole current execution specification** | `S-14.md` | Put current intent, functional acceptance, full checkpoint table, implementation order, exact run-plan link and approval state here. Retain its historical implementation/verification notes. No new numbered implementation spec. |
| **Merge, then retire as active instructions** | `docs/operations/story-27-4-execution-plan-2026-10-15.md`; `docs/operations/story-27-4-isolated-target-plan-2026-10-16.md` | Bring still-valid topology, resource limits, corrected boot approach and restoration into Spec 14. Leave dated documents at their existing paths; use a short status notice linking the current plan rather than relocating or deleting evidence. |
| **Archive in place as historical requirements/results** | `S.md`, `S-2.md` through `S-13.md`, `S-15.md` through `S-26.md`; `spec-27-4-{current-readiness-handoff-reconciliation,prerequisite-decision-adoption,strict-predecessor-consumption}.md` | Preserve exact historical bodies, original statuses, reviews and receipts. Index their disposition in Spec 14; an old `in-progress` or `done` supporting spec must not compete with the parent. Do not mass-edit historical metadata. |
| **Keep decision and incident evidence unchanged** | `27-4-owner-approval-and-acceptance-2026-10-12.md`; `27-4-isolated-target-approval-2026-10-16.md`; `27-4-bootstrap-recovery-result-2026-10-16.md`, all under implementation artifacts | Reuse policy, target and offline-result decisions. Preserve both failures, the misleading `vmStarted` field and its explicit correction, hashes and private archive references. |
| **Keep; correct current report front matter, merge status** | `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` | This remains the **only consolidated human results report**. Put current profile, functional result, C0–C6, terminal state, limits and next action first. Preserve dated receipts below; label superseded front-section policy as historical rather than silently erasing it. |
| **Merge operator instructions** | `docs/operations/access-telemetry-owner-handoff.md`; `docs/operations/access-telemetry-lifecycle.md`; `docs/operations/access-telemetry-adapter-production.md` | Owner handoff becomes a short link to the current plan and report. Lifecycle runbook keeps operational procedures; adapter appendix keeps backend-specific procedures. Remove duplicated approval walkthroughs from active guidance. |
| **Keep technical contracts; retire competing qualification routes** | `I/SPEC.md`, `I/owner-run-records.md`, `I/authority-and-sessions.md`, `I/producer-bindings.md`, `I/implementation-tasks.md`, `I/schemas.md`; `I/sole-owner-policy-revision-draft.md`; `I/p1-p4-security-operations-decision-packet.md` | Current owner-mode constraints need the approved KVM pin and checkpoint ownership. Legacy P1 issuer/GitHub review/institutional custody protocols remain separate historical or stronger contracts. They are not prerequisites for the adopted local owner mode. Do not restart a policy-adoption exercise from the old draft. Use the existing spec maintenance route for kernel changes; no new specification family. |
| **Keep one executable entry point; compose existing helpers** | `tools/verify-access-telemetry-lifecycle.py`; `tools/verify_access_telemetry_lifecycle.py`; `tools/access_telemetry_producer_common.py`; `tools/access_telemetry_c{2,3,4}_producer.py` | Add the bounded functional checkpoint to the existing verifier. Full qualification remains its other mode and reuses the existing scenario producers. No new public CLI, plugin, generic runner or receipt framework. |
| **Keep and call internally** | `tools/access_telemetry_c1_owner_{run,dispatch,custody,c115,c115_plan_files,c115_provenance,c115_retained}.py`; `tools/access_telemetry_c1_capture_streams.py`; `tools/access_telemetry_c1_retained_files.py`; `tools/verify-access-telemetry-c1.ps1`; `tools/verify-access-telemetry-c1-linkage.ps1` | Reuse ownership/status checks, bounded file/stream retention, source corroboration and real identity collectors. Stop exposing each inspector as a separate owner approval milestone. Preserve useful negative tests. |
| **Retire from the current target route; retain historical bytes** | `tools/access_telemetry_owner_discovery.py`; its dated discovery plan/receipts; private original/recovery/boot-only proposals and supervisors | D0 selected the excluded source cluster and is now historical. Reuse only safe implementation details after checking them. Fold the corrected drive syntax into the combined run; never reopen a consumed budget or execute the pending boot-only plan by implication. |
| **Keep full admission fail-closed; implement only when needed** | `tools/access_telemetry_c1_authority_boundary.py`; `tools/access_telemetry_c1_producer_bindings.py`; full predecessor/terminal/publication consumers in `tools/verify_access_telemetry_lifecycle.py` | Their current blanket refusals are real unfinished integration, not evidence that more approvals are needed. Add an accepting owner path only with real gate semantics and strict version dispatch. Do not return success from a stub or weaken legacy refusal. This is later than the functional slice. |
| **Keep runtime and deployment assurance** | `src/Hexalith.Memories.AccessTelemetry*/`; `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/`; `deploy/kubernetes/overlays/qualification/`; `docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json` | Reuse the actual actor, Dapr store, configuration, clock signer, fixed workload and inert qualification overlay. No runtime gate bypass, fabricated capability evidence or arbitrary workload endpoint. Bind rendered functional deltas separately. |
| **Keep protected close-out inputs unchanged during initial work** | `deferred-work.md`, `_bmad-output/project-context.md`, `docs/dev/telemetry.md`, `sprint-status.yaml`, Epic 20/Story 20.5 records | No A41 closure or sprint transition follows from simplification or a functional pass. The report can record preparation/results without asserting a close-out transition. |

No implementation or archival edits in this table have been applied by this proposal.

### Executable path to implement

Extend the existing command with `--checkpoint functional-retention`, a retained run-plan path and its outside approved SHA-256. Exact invocation is generated from the implemented parser and bound source before operational approval; the checkpoint does not exist today. The command owns this fixed sequence:

1. Validate current approval/status, source/tool/input hashes, disk identities, physical allocation, native Windows DACL and host limits. Refuse before spawn on missing authority or drift.
2. Boot the retained guest with the corrected raw backend plus `virtio-blk-pci` frontend. Observe UUID, block identities, SSH host identity and actual guest readiness. A spawn flag is insufficient.
3. Initialize the previously unformatted **data disk only**, after signature/serial/empty-state checks; install the pinned Kubernetes/Dapr/storage/OpenBao inputs and load pinned application images using host-staged artifacts. Use the existing qualification overlay and a new private kubeconfig. Record resource UIDs and TLS CA/API identity before enabling the test.
4. Run existing startup/capability probes and record what they actually checked. Reference genuine functional configuration/headroom observations with their limited scope; do not invent capacity/reclamation receipts or turn startup probe labels into C1 passes.
5. Acquire the run-owned Lease, open the expiring qualification gate, and execute the functional scenario below through the existing Server fixed-workload endpoint and normal typed logger → provider → Dapr → lifecycle actor → PostgreSQL path.
6. Disable the gate, release only the owned Lease, scale qualification lifecycle/clock to zero, obtain actual final API readback, then stop the owned guest and verify its process/ports. Retain disks and evidence.
7. Validate retained observations and update the existing report with the result and explicit coverage limits. Internal packets/logs and a hash manifest are evidence inputs, not additional owner-facing reports.

The retained correction puts the disk serial on the device frontend, rather than the raw backend. Reuse this syntax in the new source-bound command; do not execute the old failed vector:

```text
-drive file=<retained-os.raw>,if=none,id=qual-os,format=raw,cache=none,aio=threads,discard=ignore,detect-zeroes=off
-device virtio-blk-pci,drive=qual-os,serial=memories-qual-os
-drive file=<retained-data.raw>,if=none,id=qual-data,format=raw,cache=none,aio=threads,discard=ignore,detect-zeroes=off
-device virtio-blk-pci,drive=qual-data,serial=memories-qual-data
```

This is the already-prepared correction, not evidence of successful QEMU acceptance or guest boot. Preserve the existing UUID, seed and network bindings unless the new exact plan explicitly changes them. The new supervisor must enforce the whole-run deadline independently of the agent, attempt approved restoration on failure, and bound the guest even when restoration cannot complete; such a run cannot claim terminal success.

Use the existing private root for control and raw evidence, with exclusive per-attempt outputs. Keep command IDs/arguments, start/end UTC, exit codes, result counts, bounded secret-safe streams, source/image/configuration identity and exact-byte readback. Each required full gate later retains its own attributable packet; a common report does not allow one packet to discharge multiple gates.

### Smallest live functional scenario

Use **one Server writer**, the existing one-second fixed segment of **125 records**, and retention **3,600 seconds**. Emit cohort A at actual time T0. Confirm all 125 acknowledgements and persisted records through Dapr. At approximately T0 + 30 minutes emit cohort B, again 125 records with the same one-hour retention. The command must retain actual emission, acceptance and absolute expiry times; expiry in current source is based on emission time, not acceptance time.

Observe A before expiry, at expiry, and through the actor's purge deadline. By each A record's expiry + 15 minutes require strong Dapr absence, its expiry-index removal and actual reminder/progress observations. B must still be readable with unchanged record identity/hash/expiry. Distinguish an already-TTL-absent record from an actor-issued deletion; both still require index/progress proof. Aggregate counters or HTTP 200 alone cannot pass this test.

Do not backdate events, advance a fake clock, shorten the runtime minimum, insert records directly with SQL, delete them with a test SQL command, or replace PostgreSQL/Dapr with in-memory doubles. Fixed adapter-side queries may corroborate sanitized record/index identities without becoming an application backend dependency. Use only synthetic qualification data. Privacy/tenant refusal regressions remain required offline; this small run does not claim the full live isolation/fault matrix.

## 4. Operational approval envelope

**Proposed terms, not active permission.** After the implementation is reviewable, one approval names the exact plan/runner/manifest hashes and covers all following steps. No additional approval is sought for an unchanged in-budget step or allowed transient retry.

| Scope | Proposed bound |
| :--- | :--- |
| Target | Host `DESKTOP-VIOG240`; only guest `memories-qualification-27-4`, UUID `2fe71a5c-bdda-4767-8780-8f27f5bcd520`; private context `owner@memories-qualification-27-4`; namespace `hexalith-memories-qualification` and explicitly listed guest-only infrastructure namespaces. No contact with `192.168.1.30` or ambient kubeconfig. |
| Compute/storage | At most 8 vCPUs, 40 GiB guest RAM; reuse only existing 40/600-GiB raw disks in `/mnt/c/Users/JeromePiquot/AppData/Local/Hexalith/Qualification/memories-27-4-2fe71a5c/`. No new disk, resize or replacement allocation. One 400-GiB PostgreSQL PVC within the existing data disk; total existing proposed volume allowances stay within 600 GiB. Requests are not proof of free space. |
| Resource floors | Before guest start: at least 56 GiB host available RAM and 150 GiB C: free. During run: at least 16 GiB host available RAM and 150 GiB C: free; retain the 400-GiB 70/80/90-percent boundaries. Stop admission on insufficient observed guest filesystem/volume headroom or aggregate resource limits. Never resize to make a failed check pass. |
| Data initialization | Permit one initial filesystem creation on the exact blank 600-GiB data disk, then mount/install only task-owned guest paths. Refuse an existing filesystem, unknown signature or changed disk identity; no format retry. OS boot/setup writes are explicit; never reformat the retained OS or discard existing evidence. |
| Installation and credentials | Exact hash-bound package/image/chart list staged on the host, transferred over the fixed private SSH channel, installed only in the guest. Permit fresh guest-only service accounts, Roles, Secrets, config and TLS/marker/clock material named in the manifest. No host permission changes, external account grants, source-cluster credentials, package-version updates or public publishing. |
| Network | Retain `restrict=on` and loopback-only SSH/API forwards `12274`/`16443`; no bridge/TAP or guest external egress. Functional mode uses the existing development UTC sources explicitly. Images/packages are host-staged; their downloads/cache, if needed, have an exact allowlist and a 20-GiB total staging cap within the host free-space floor. No general guest Internet enablement. |
| Workload/actions | Guest bootstrap/setup, read-only identity observations, configuration of one-hour retention, two fixed 125-record segments from one writer, existing startup probes, record/index reads, normal actor reminder purge, short gate renewals and restoration. No injected backend/Placement/Scheduler faults, capacity filling, backup/restore or allocator reclamation job in this first run. |
| Window | One operational approval expires 48 hours after its actual recorded UTC time. One execution may start within that period and must finish tests/setup within **4 hours of first spawn**, with **15 minutes reserved for restoration**. Compile absolute `notBefore`, `latestStart` and `expires` values so the entire 4h15 fits before approval expiry. Setup must finish within 90 minutes; otherwise stop. Existing 30-minute guest auto-poweroff/timeout settings must be deliberately replaced in the new plan; the old bootstrap supervisor cannot run this scenario unchanged. |
| Retry budget | At most **two VM launch attempts total**, each with a 10-minute readiness limit; a second only for a transport/readiness transient after verified stop. Same QEMU syntax/device/resource error stops the run. Read-only commands: at most three attempts, 2/5-second delays, 120-second aggregate command bound. Downloads: at most two attempts within the staging cap. Mutations: no blind retry; re-read UID/resourceVersion and reconcile the same intended object. Cohort emit: at most two requests with the same run/segment/time identity, only for transport uncertainty, and require exact accounting. No second cohort experiment or reset of elapsed time on semantic failure. |
| Observation/outputs | Poll at most every 30 seconds with per-query deadlines. At most 1 MiB per retained stdout/stderr stream and 256 MiB total run evidence, plus the separate 20-GiB staging cap. Exceeded bounds fail with a retained reason. |
| Stop and restoration | Stop tests on denial, drift, withdrawal, expiry, resource floor violation or failed semantic assertion. Separately preapprove up to 15 minutes of restoration after a stop: disable only the run-owned gate, release only its Lease, scale only its lifecycle/clock to zero, retain evidence/storage, then stop only the UUID/PID/start-time/argv-matching guest. Never delete namespaces/PVCs/disks as cleanup. Unverifiable cleanup makes the run incomplete and blocks another launch. |

Suggested single operational decision, filled with concrete hashes and UTC values after preparation:

> Approve the exact Story 27.4 functional-run plan, including retained-disk boot, one-time blank-data-disk initialization, guest-only setup, the disclosed development-clock/source-image limitations, two 125-record cohorts at real one-hour retention, the listed resource/window/retry limits and bounded restoration. Reuse my existing sole-owner and KVM target decisions. This grants no full qualification, Production, A41 close-out or Git/publication authority.

If source, tools, permissions, endpoints or actions cannot be concretely enumerated, the agent finishes that preparation before asking for this decision. Renew approval only for a material scope/resource/time change or an exhausted/expired budget; an execution error alone is not owner withdrawal of the standing policy.

## 5. Full qualification and precise completion gates

### Coverage retained for later execution

Full mode keeps exact-source/profile/workload equality, all 25 distinct C1 checks, C0–C6, the complete replacement/failure inventories, independent authenticated UTC behavior, physical reclamation, privacy, backup/restore and terminal/publication validation. Functional output is refused as a predecessor or close-out artifact.

For C2, distinguish the 250 accepted records/s cluster workload from the canonical 500-events/s qualification workload label and at least 500 component operations/s throughput assertion; do not merge these quantities. Keep two concurrent writers, the exact 30-minute interval and 150,000-record backlog, with no acknowledgement loss across required replacements.

C3 still requires **actual 1/24/168-hour cohorts** and attributable allocator-reuse proof within **24 hours after active purge**, inside its **216-hour** producer bound. A one-hour functional pass, a 600-GiB allocated image, logical absence or OS free space cannot discharge these checks.

Before full mode is executable, resolve the known mismatch between the 72-hour owner collection record and multi-day C3. Prepare one approved campaign plan containing bounded phase scopes/records and same-session continuation checks; implement and verify those in the existing owner tooling. Do not silently extend v1 windows, refresh old C1 evidence, relabel sessions, reset cohort age or waive freshness. If existing freshness rules prevent the campaign, that exact policy change must be presented explicitly in the full-run plan before execution. Do not build campaign renewal machinery in the first functional slice.

Also reconcile C1 collectors' fixed `hexalith-memories` namespace with C2–C4's `-qualification` requirement before full execution. Bind the actual database/store and namespace relationships in the full plan; do not duplicate two 400-GiB stores on the 600-GiB disk or treat a namespace substitution as identity-preserving proof.

### Proposed checkpoint ownership in the existing story

Each row below is proposed future intent. `Owner` means Jérôme Piquot; agent/tools implement and collect. Review state `pending` means awaiting the owner's decision on actual evidence, under the standing self-review policy. Current live completion state is `open`. These columns must remain explicit when this table is copied into Spec 14. Evidence paths are relative to one future approved private run root; a path is not evidence that a collector exists.

| Gate | Required observed evidence / artifact | Owner | Review state | Completion state |
| :--- | :--- | :--- | :--- | :--- |
| C1.1 | `c1/C1.1.json`: real CRUD round trip | Jérôme Piquot | pending | open |
| C1.2 | `c1/C1.2.json`: post-write strong read | Jérôme Piquot | pending | open |
| C1.3 | `c1/C1.3.json`: matched/stale ETags and FirstWrite | Jérôme Piquot | pending | open |
| C1.4 | `c1/C1.4.json`: later-operation failure with no partial transaction | Jérôme Piquot | pending | open |
| C1.5 | `c1/C1.5.json`: effective component TTL | Jérôme Piquot | pending | open |
| C1.6 | `c1/C1.6.json`: actor state survives reactivation | Jérôme Piquot | pending | open |
| C1.7 | `c1/C1.7.json`: Placement/Scheduler disruption and reminder recovery | Jérôme Piquot | pending | open |
| C1.8 | `c1/C1.8.json`: actual record/request limits | Jérôme Piquot | pending | open |
| C1.9 | `c1/C1.9.json`: full two-writer throughput and latency evidence | Jérôme Piquot | pending | open |
| C1.10 | `c1/C1.10.json`: 150,000-record catch-up and drain bounds | Jérôme Piquot | pending | open |
| C1.11 | `c1/C1.11.json`: tenant/isolation denials before dependencies | Jérôme Piquot | pending | open |
| C1.12 | `c1/C1.12.json`: TLS verify-full and at-rest posture | Jérôme Piquot | pending | open |
| C1.13 | `c1/C1.13.json`: measured amplification/headroom and 1/24/168-hour admission | Jérôme Piquot | pending | open |
| C1.14 | `c1/C1.14.json`: cohort-attributable allocator reuse and deadline | Jérôme Piquot | pending | open |
| C1.15 | `c1/C1.15.json`: fresh current-session runtime/control-plane capture | Jérôme Piquot | pending | open |
| C1.16 | `c1/C1.16.json`: fresh component/backend identity and actual connection linkage | Jérôme Piquot | pending | open |
| C1.17 | `c1/C1.17.json`: deployed image/manifest/epoch/source identity | Jérôme Piquot | pending | open |
| C1.18 | `c1/C1.18.json`: node/storage reservation, headroom and operating cost | Jérôme Piquot | pending | open |
| C1.19 | `c1/C1.19.json`: declared PostgreSQL process/pod fault, zero acknowledged loss | Jérôme Piquot | pending | open |
| C1.20 | `c1/C1.20.json`: exact out-of-profile exclusions | Jérôme Piquot | pending | open |
| C1.21 | `c1/C1.21.json`: real backup destination and successful restore | Jérôme Piquot | pending | open |
| C1.22 | `c1/C1.22.json`: measured RPO/RTO and no-HA claim | Jérôme Piquot | pending | open |
| C1.23 | `c1/C1.23.json`: owner Operations acceptance of required parents | Jérôme Piquot | pending | open |
| C1.24 | `c1/C1.24.json`: owner non-HA acknowledgement | Jérôme Piquot | pending | open |
| C1.25 | `c1/C1.25.json`: owner security risk acceptance after required parents | Jérôme Piquot | pending | open |
| C0 | `c0/adapter.json`: exact approved adapter/source/input evidence | Jérôme Piquot | pending | open |
| C1 aggregate | 25 verified same-session packets plus distinct owner role records | Jérôme Piquot | pending | open |
| C2 | Existing `c2-production-replacement` producer packet, all fixed replacements/accounting | Jérôme Piquot | pending | open |
| C3 | Existing `c3-retention-reclamation` packet and complete journal, all three cohorts | Jérôme Piquot | pending | open |
| C4 | Existing `c4-failure-privacy-observability` packet, every fault/privacy/continuity lane | Jérôme Piquot | pending | open |
| C5 | Operations result record over actual C0–C4, runbooks, risk and cleanup | Jérôme Piquot | pending | open |
| C6 | Distinct Security result record over the same evidence, accepted with C5 in one final owner response | Jérôme Piquot | pending | open |
| Terminal | Actual disabled gate, empty Lease, zero desired/ready lifecycle/clock and terminated Pods, bound to the result set | Jérôme Piquot | pending | open |
| A41 publication | Existing inventory → recoverable preflight → exact mutation → postflight → authorized publication → remote containment proof | Jérôme Piquot | pending | open |

Gate meanings come from the existing requirement inventory in the 2026-08-01 correction, updated by the adopted sole-owner policy; its old story-registration shape is not reused. Keep the existing dependency graph, including C1.24 before C1.23 and C1.25. The owner does not manually author these packets. Failed, skipped, zero-result, stale, missing or mismatched observations never become passes.

### Completion checklist based on behavior

**First implementation slice is accepted only when all functional items pass:**

- [ ] The implemented existing entry point rejects missing/expired/withdrawn approval, source/target drift, wrong profile, exceeded budget and unsafe evidence paths before the affected action. Offline denial tests are recorded separately from live observations.
- [ ] Live QMP/SSH/OS observations identify the approved guest and both retained disks; installation resolves all images/configuration to retained exact bytes. Placeholder images/epochs/keys do not pass readiness.
- [ ] A's 125 records and B's 125 records traverse the actual Server delivery pipeline and are acknowledged and strongly read back; rejected/conflicted/lost accounting is zero. Real emission and acceptance timestamps are retained.
- [ ] A exists before expiry; after its actual one-hour expiry it is unavailable through the strong Dapr check, with due-index removal and reminder/progress proof by expiry + 15 minutes. B remains readable with unchanged expiry and hash.
- [ ] UTC signing/nonce/profile/expiry validation and normal startup probes execute, with development-source and source-image limits disclosed. No check is satisfied by a fabricated receipt or substituted in-memory backend.
- [ ] Console telemetry and bounded purge progress are observed; no secrets/raw content enter retained results. The current tenant/privacy denial regressions pass, without claiming a full live C4 result.
- [ ] Actual API readback proves disabled gate, empty owned Lease, zero desired/ready lifecycle/clock replicas and Pod termination **before** guest shutdown; stopped guest/process/ports are then observed separately. Incomplete cleanup is reported as a failure.
- [ ] One report contains exact command/result references, source/configuration/image hashes, failed attempts, measured timing, `functional passed/rejected/incomplete`, `sole-owner/self-reviewed`, and all unperformed full checks. Story 27.4 and A41 remain open.

**Whole Story 27.4 completion remains a separate, unchanged assurance bar:**

- [ ] All applicable predecessor/remediation requirements, 25 C1 technical gates and C0–C4 pass on eligible exact-profile live evidence; functional packets are rejected by those consumers.
- [ ] Full capacity, independent clock, replacement/fault/tenant/privacy, backup/restore and actual 1/24/168-hour reclamation requirements are proven, or the relevant gate remains open. This proposal accepts no debt in place of a technical pass.
- [ ] The owner accepts the completed C1 batch before downstream full qualification and later accepts final results; distinct bound C5/C6 records disclose self-review.
- [ ] Terminal validation passes. Only the verifier's four registered A41 paths participate in a separately authorized close-out/publication transaction; historical Epic 20/Story 20.5, epics and sprint bytes stay protected in that transaction.
- [ ] Remote containment is verified for the exact authorized close-out commit. Any orchestrator-owned sprint/action synchronization is a subsequent authorized action, never an inferred side effect of a functional pass.

Production activation remains a separate decision after qualification, with Production target/security/custody/configuration evidence. Neither story completion nor non-Production results automatically enable it.

## 6. Explicit before → after edits and implementation order

These are proposed edits, not applied changes to frozen intent.

| File / section | Before | After, subject to approval | Why |
| :--- | :--- | :--- | :--- |
| Spec 14 / Approach and Execution | “prepare one bounded run record for owner approval, collect and verify C1 and qualification evidence” plus six broad live tasks and overlapping D0/target plans | “Deliver the bounded functional-retention checkpoint first through the existing verifier; retain the full C1/C0–C6 and A41 checkpoints below. This file owns current execution instructions.” Add the operational envelope and observable checklist above. | Enables direct live feedback without pretending the full predecessor is ready. |
| `epics.md` / Story 27.4 Predecessor Gate | “Actual Story 27.7 through Story 27.31 files must exist … be registered … and be done.” | “Full completion requires the same technical evidence for C1.1–C1.25, with owner, exact evidence, review state and completion state in Story 27.4. Separate story-file registration is not a qualification predicate. The functional checkpoint earns no predecessor or closure credit.” Preserve old wording as dated history. | Removes administrative expansion while preserving per-gate assurance. This is an explicit ratified-policy change, not an editorial correction. |
| Canonical report / Immutable decision identity and current matrix guidance | Former PG2 `7f9f…` identity and independent-review instructions appear before the later sole-owner addendum | Put approved KVM `95aa…` identity and adopted self-review policy first; identify older paragraphs as historical. Add a separate functional summary without advancing C0–C6. | Readers should not have to reconstruct current authority from many appendices. |
| `I/SPEC.md` / Constraints and companions | Current constraints still pin `7f9f…`; scope delegates live readiness across many documents | Point current owner-mode constraints to the adopted manifest and Spec 14. Keep technical wire contracts subordinate, historical protocol strict and full admission closed until implemented. | Aligns the technical contract with the existing owner decision without creating a new numbered implementation specification. |
| ADR / qualification guidance; owner handoff | Full admission obligations and multiple approval walkthroughs obscure how a functional test can run | State the three evidence tiers and the two disclosed functional limitations; preserve every full/Production technical predicate. One owner action approves an enumerated operational plan; result acceptance follows actual results. | Separates fast feedback from operational assurance. |

Implementation order after proposal approval:

1. **Consolidate current instructions in place.** Amend Spec 14, its checkpoint ownership and active links, the report's current-policy/identity front section, and the named planning/contract clarifications together. Preserve history and all live statuses. No new story registration or numbered spec. Owner approves this umbrella checkpoint structure through this proposal.
2. **Prepare one runnable functional slice.** Extend the existing verifier and shared producer code. Reuse corrected QEMU arguments, existing private custody checks, inert overlay, fixed workload and real record/index checks. Bind guest setup, exact staged inputs and source/image bytes. Use the existing development-clock option only in the explicitly rendered functional configuration; keep full-mode checks unchanged. No C# feature or public API is planned; a demonstrated runtime defect is fixed narrowly and reported.
3. **Verify offline boundaries.** Add meaningful tests for denial before spawn, exact command/resource selection, bounded retry reconciliation, real-versus-fixture result separation and inability to use a functional result in full/terminal/A41 consumers. Run only affected Python lanes and relevant individual .NET test projects if runtime changes require them. Local builds use Debug/source references; CI/package checks retain their own lane. Do not rerun every historical suite after documentation-only changes.
4. **Present the completed operational bundle once.** It contains exact runnable command, source/tool/image/manifest hashes, generated current decision/status bindings, absolute UTC window, actions, budgets and restoration. Obtain the missing operational approval; create no active permission before it.
5. **Execute, observe, restore and report.** Run the functional checkpoint inside that envelope. Keep failed attempts. Present its one measured report for result acceptance; do not mark the story done or close A41.
6. **Complete full-mode gaps in place.** Implement actual missing gate collectors/semantic admission, same-session owner dispatch and long-run continuation only as needed for the existing full checkpoint inventory. Replace future per-gate story-registration ceremonies with the approved table. Prepare one full campaign operational plan when concrete; retain C1 batch and final result decisions at their causal boundaries. Reuse observations within matching source/session/profile limits, not by copying a pass from a different scenario.
7. **Close out only on full evidence and separate authority.** Follow existing terminal/A41 publication verification. Production rollout is outside this proposal's operational scope.

Planning estimate, not a completion promise: documentation consolidation about half a day; functional command/setup/verification approximately 1–3 engineering days if pinned local inputs are available; one functional execution at most 4h15. Missing images, setup incompatibility, resource admission or Dapr/reminder behavior can block that run. Full qualification remains several engineering days plus at least seven days of actual observation; its missing collectors and continuation cannot be estimated as a boot fix.

Handoff: **Moderate — owner/PO and Developer.** Jérôme owns scope, operational approval and acceptance; the development agent owns consolidation, implementation, evidence collection and reporting. No extra human reviewer or external service is introduced. No handoff message or external publication has been sent.

## 7. Investigation, claim verification and workflow record

Repository guidance read: `AGENTS.md`, the permitted root-declared `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, Git instructions, build/format configuration, the correct-course checklist and resolved customizations. Planning document inventories and the impacted PRD, Epic 27/34/35, architecture/ADR and UX passages were examined against current source, tests and owner records. No repository skill under `references/` was loaded.

Historical Context Classification:

- Spec 14, owner approvals, target plan and bootstrap results: `historical-reference-only` for decisions, failures and protected evidence; the current story's intent is amended only on approval.
- The broad accumulated Spec 1–26 delivery/review shape, old one-story-per-gate registration programme and institutional P1 prerequisites: `anti-template`; use only their requirement/evidence inventory.
- Existing custody/stream helpers, qualification workload and Dapr actor/store: `current-narrow-pattern`, rechecked in current source; reuse their specific implementation, not historical whole-story task density.

### Epic AC Verification

Inspection date: 2026-10-16, baseline above. Commands below are local, re-runnable inspections; no assertion of live qualification follows. Proposed end states and limits are design intent, not claims that their implementation exists.

| Quoted claim | Re-runnable command / evidence | Observed / verdict |
| :--- | :--- | :--- |
| “All six … live tasks remain open.” | `sed -n '60,83p' _bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md` | Six unchecked Execution rows. **confirmed**. |
| “The owner explicitly approved sole-owner-qualification/2026-10-12.” | `cat _bmad-output/planning-artifacts/sprint-change-proposal-2026-10-12-sole-owner-qualification.md` and `cat _bmad-output/implementation-artifacts/27-4-owner-approval-and-acceptance-2026-10-12.md` | Actual policy and offline acceptance decisions recorded. **confirmed** as conversation decisions, not third-party receipts. |
| “Both 40-GiB OS and 600-GiB data disks are allocated.” | `cat _bmad-output/implementation-artifacts/27-4-bootstrap-recovery-result-2026-10-16.md` | **confirmed** as a dated recorded measurement. Fresh disk/headroom/ACL truth is **unverifiable without the later preflight**; agent owns that check immediately before launch; failure stops execution. No full disk read was repeated here. |
| “Block format 'raw' does not support the option 'serial'.” | Same recovery record; retained QEMU/result/postflight references | Exact error and no observed running guest recorded. **confirmed**. The result and independent-postflight bytes were read locally and match SHA-256 `422dd84accde80bd1f181e4f56ae5a5078863a6d097dcc1196028f868f19856a` / `0492c220206609d5e48e33d1a7d3ee61f1d70c907fcbad2634d51a3b7ddaba5d`. |
| “The corrected boot-only proposal remains unapproved.” | Read private `control/boot-only-status.json` and `evidence/proposal/bootstrap-boot-only-plan.json` under the recovery record's private root | No actual decision, no executable authority, unused budget. Plan bytes match `66f2ce2ffc32cee4538a9ebf9b1bdcbb4baef20609bd0002291ea214805f8778`; commands and supervisor match the recovery record. **confirmed** at inspection. |
| “The approved current profile is … 7f9f…” in current report/kernel constraints | `sed -n '19,29p' _bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`; `sed -n '44,52p' _bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/SPEC.md`; `cat docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json` | Former pin is stale for the current KVM target. **corrected**, escalated in the exact before/after edits above; source documents remain untouched pending this requested approval. |
| “Post-evidence C5/C6 still require separate reviewers.” in report current guidance | `sed -n '68,79p' _bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`; read its `2026-10-12 owner policy adoption` addendum | Superseded by adopted sole-owner mode. **corrected** for this mode; preserve stronger legacy protocol. Source correction proposed above, not silently applied. |
| “Current consumers remain closed.” | `rg -n -e 'current authority unavailable' -e 'authority-prerequisites-unapproved' -e 'deployed-producer-binding-unavailable' tools/verify_access_telemetry_lifecycle.py tools/access_telemetry_c1_authority_boundary.py tools/access_telemetry_c1_producer_bindings.py` | Explicit terminal refusals remain. **confirmed implementation gap**, not justification to weaken full gates. |
| “Retention must be finite and between one hour and seven days inclusive.” | `cat src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidator.cs`; `sed -n '78,102p' src/Hexalith.Memories.AccessTelemetry/Lifecycle/AccessTelemetryLifecycleProcessor.cs` | Bounds and emission-based expiry present. **confirmed** in source; live enforcement is a future observation. |
| “one fixed one-second segment” and “125” | `sed -n '17,34p' src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationWorkloadRunner.cs` | Existing segment constants and normal logger path. **confirmed** in source. No need for a new workload endpoint. |
| “72” hour collection versus “1, 24, and 168” hour cohorts | `sed -n '177,196p' tools/access_telemetry_c1_owner_run.py`; `rg -n -e '1, 24, and 168' -e '777_600_000' tools/verify_access_telemetry_lifecycle.py` | Existing incompatible time envelopes require continuation work before full execution. **confirmed**. No clock or freshness bound was changed. |
| “Production lifecycle writes remain disabled” | `cat deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`; `cat deploy/kubernetes/overlays/qualification/qualification-gate.yaml` | Zero lifecycle/clock Production replicas and disabled/empty qualification defaults in repository bytes. **confirmed for configuration**. Live Production state was not inspected and receives no claim. |

Checklist outcome: [x] 1.1–1.3 trigger/evidence; [x] 2.1–2.5 epic/dependency impact; [x] 3.1–3.4 PRD/architecture/UX/secondary artifacts; [x] 4.1 direct adjustment selected, medium effort/risk; [N/A] 4.2 rollback and 4.3 MVP reduction rejected as remedies; [x] 4.4 rationale; [x] 5.1–5.5 proposal and handoff; [x] 6.1–6.2 drafting/verification; [!] 6.3 explicit approval pending; [N/A] 6.4 no sprint registration/renumbering is proposed; [x] 6.5 next steps and handoff specified, awaiting approval to execute. No scope change or corrected planning claim is treated as adopted here.

Document validation passed: local Markdown links resolve; tables have consistent columns; all 25 C1 rows and the remaining checkpoint rows name the owner; Markdown uses the required CRLF. `GIT_OPTIONAL_LOCKS=0 git diff --check` exited 0. SHA-256 comparison against the read-only baseline of 6,228 pre-existing tracked files found no changes, and the pre-existing untracked bootstrap result is byte-identical. These are document/preservation checks; no runtime test suite or qualification workload was run for this proposal.

No workload, guest, cluster query, allocation, permission/status activation, implementation change, A41/sprint transition or Git mutation was performed. Only this proposal was created. Existing untracked bootstrap evidence is preserved. The scope and tier amendments above require owner approval under the invoked correct-course workflow and the user's explicit instruction to stop before implementation.

</details>


## Owner approval and implementation handoff

On 2026-10-16 the owner answered **“yes”** to the complete proposal's implementation approval question. This records that actual conversation decision under `sole-owner-qualification/2026-10-12`, principal `github:user:6775094`, assurance `sole-owner/self-reviewed`. The proposal bytes presented for that decision have SHA-256 `c27e345dde05d07d63474f080cf1ab2d102a0551b410ba78094ca8d0369dcb13`. Recorded at `2026-10-16T11:25:03Z`; this is the record time, not a fabricated decision timestamp or external receipt.

Status: **Approved for implementation.** Scope and administrative checkpoint amendments above are adopted. Proceed with the existing Spec 14, consolidating current instructions and implementing/verifying the bounded functional path before presenting its one concrete operational plan. No live run, active permission/status, storage initialization/allocation, A41/sprint transition, publication or Git mutation is authorized by this decision. Full qualification remains later work. The earlier proposal-time statements and verification record above are retained as dated history.

</details>

</details>
