# Sprint Change Proposal — Story 27.4 scope reset: finish on what is proven

**Status:** **approved and applied 2026-11-01.** The owner answered **"yes"** to D1–D4 as recommended, after advanced elicitation (§8). One ordinary review followed; §9 records the implementation, the review fixes and the verification.
**Mode:** batch (same as the 2026-10-21 correction).
**Trigger:** the owner asked: *"we made many iterations on story 27.4 without being able to complete. Is this story too complex or over-engineered? can we simplify with a pragmatic solution?"*
**Scope class:** Moderate. It changes a ratified decision (the A41 closure bar), so it needs explicit owner approval and is not absorbed silently.

## 1. Issue summary

**Short answer: yes. Story 27.4 is over-scoped and its process is over-engineered.** The product behaviour it exists to prove has already been proven. What remains is a Production qualification programme for a store that Production keeps switched off.

- **The retention behaviour is proven, within stated limits.** Run `0673d50339f4e1058c26e3a9b2d680a2` passed on 2026-10-25 against real Dapr 1.18.1 and PostgreSQL 18.6.
  - It wrote two cohorts of 125 records, 30 minutes apart.
  - Cohort A was strongly readable before its one-hour expiry, then strongly absent, with due-index cleanup and reminder progress. Cohort B was unchanged.
  - The native TRX shows 1/1 passed, and disposal was verified.
  - The evidence covers one writer, Development, one-hour retention and logical expiry only. The report itself says it does not prove physical tuple deletion or reclamation (`tests/27-4-retention-verification-evidence.md:3144`).
- **The remainder is a programme, not a story.** It has 34 checkpoints: 25 C1 gates, C0, the C1 aggregate, C2–C6, a terminal check and A41 publication. **All 34 are `pending`/`open`.** The recommended schedule needs a **336-hour** reservation containing a 216-hour C3 run, plus an environment Platform does not yet provide.
- **The target environment is being built inside the story.** Since 2026-10-26 every continuation has built qualification infrastructure rather than Memories behaviour: host capture, S3 calibration, a "smaller campaign profile", and now a rootless Docker engine on node1. That engine needs UID-mapping helpers, a private AppArmor exception and a disk-quota decision. Each is new host-level scope outside the existing approval.
- **No Production consumer exists.** The Memories Production overlay sets `ACCESS_TELEMETRY_ENABLED=false`, both lifecycle Deployments are `replicas: 0`, and CI pins that state.
- **The ceremony is sole-owner role-play.** C1.23–C1.25, C5 and C6 are "distinct" Operations and Security acceptance records that the same person writes about their own evidence (`sole-owner/self-reviewed`). They add records, not assurance.
- **The cost so far:** 91 commits mention 27.4, there are 18 numbered sub-specs, and the active Spec 14 (1,979 lines), results report (3,173) and qualification inventory (987) total about 6,100 lines. Since the functional pass, 15 more commits have produced zero executed gates.

### Root cause (5 Whys)

1. **Why did work continue after the pass?** The story stayed `in-progress`, and the full campaign was its only remaining work.
2. **Why was the campaign still in the story?** The 2026-10-21 correction split off the functional track but deliberately kept the completion bar: "Overall Story 27.4 remains in progress".
3. **Why was the campaign pursued straight away?** The owner said "I dont know, do recommended" to target/window selection. With an open story and one remaining checkpoint, "recommended" meant "continue".
4. **Why did it grow into host infrastructure?** The missing Platform capability was built inside 27.4 continuations. Yet `epic-27-context.md` itself says "Missing Platform capability is a blocker, not a new Memories environment".
5. **Why did nothing stop it?** The existing gates govern story shape and claims. None asks whether the remaining work is worth its cost.

**Root cause:** the completion bar was never revisited, and a missing-capability blocker had no stop rule.

The bar itself drifted. A41's own closure rule (`epics.md:4376`, `sprint-status.yaml:631-633`, DW-17) is: *"until either bounded retention/TTL is implemented and validated or an explicit accepted-debt disposition records a named approver/owner, scope, rationale, risk/consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger."* Later corrections raised it to "all 25 C1 gates plus C0–C6 plus terminal plus publication on one immutable Production profile". That raised bar appears in the Epic 27 header, `architecture.md:230` and Story 27.4's third Full-campaign AC.

## 2. Impact analysis

| Area | Impact |
| :--- | :--- |
| **Epic 27** | Can complete. 27.1, 27.2, 27.3, 27.21 and 27.22 are `done`. 27.4 closes on its achieved outcome once A41 is dispositioned. |
| **Story 27.4** | Narrowed to one outcome: retention verified and A41 dispositioned. The full Production qualification campaign leaves the story. |
| **Full campaign (C0–C6, 25 C1, terminal, publication)** | Moves to one deferred-work entry that reopens **only** on a measurable trigger. **No story is registered**, because a 34-gate story would break the story-scope guard. |
| **A41 / DW-17 / Epic 20 sprint action** | **Resolved by accepted debt, not closed as qualified.** The mechanism is implemented (27.2) and functionally validated within stated limits. Memories does not bound Production access-telemetry retention while the store stays disabled, and the debt record states that risk explicitly. |
| **Project context** | `_bmad-output/project-context.md:128` ("Never hide or over-close…") is rewritten so the guard survives in its resolved form. Every future agent reads this rule. |
| **PRD** | No requirement changes. NFR34 already includes "dated accepted debt for unsupported retention profiles" (`prd.md:1205`). The NFR evidence-status row (`prd.md:1123`) gets a dated note. |
| **Architecture** | Dated correction at `architecture.md:230`. No component, schema or API change. |
| **Code** | No product code changes. The document-contract guard tests listed in §4.8 change their pinned wording, and one CI-run test pins the A41 sprint action as `open`. |
| **CI** | `tests/tooling/access_telemetry_c1` runs in CI (`ci.yml:388`). Missing its update would break the build on `main`. The five access-telemetry tooling lanes (`ci.yml:368-397`) keep running for parked work; moving them to nightly or retiring them is a noted follow-up, not part of this change. |
| **Hexalith.Platform** | The in-flight node1 rootless-Docker and independent-calibration work stops. Committed Platform code stays in place, reusable if the campaign is ever triggered. Nothing is rolled back. |
| **UX** | Not applicable. There is no interface change. |
| **Epics 28–35 and release** | No dependency on 27.4's full campaign. Epics 31 and 35 keep their own gates. Documentation and test commits trigger no release. |

## 3. Recommended approach

**Direct adjustment with scope reduction (Option A). Recommended.**

1. **Close Story 27.4** on three checkpoints: the proven functional retention run, the existing operations runbook (`docs/operations/access-telemetry-lifecycle.md`), and the A41 disposition.
2. **Resolve A41 by accepted debt.** project-context, DW-17 and `epics.md:4376` already permit this route. The record carries every required field:
   - **Approver and owner:** Jérôme Piquot (`github:user:6775094`).
   - **Scope:** Production use of the PostgreSQL lifecycle store. That covers the two-writer Production shape, workload/sidecar/actor/Placement/Scheduler/backend fault recovery, backup/restore, physical tuple deletion and reclamation, and capacity at the 151.2M-record / 7-day envelope.
   - **Rationale:** there is no Production consumer, and lifecycle writes are disabled. The mechanism is implemented and functionally validated within its stated limits.
   - **Evidence limits:**
     - one writer, `Development`, file secrets and development UTC;
     - 3600-second retention;
     - logical expiry, strong absence and due-index/reminder progress only, with no physical-deletion proof (report `:3144`).
   - **Risk and consequence:** in Production, access telemetry exists only as the JSON-console/OTLP emission. **Its retention is set by the operator's log platform, not by Memories.** Memories does not bound Production retention while the store is disabled.
   - **Compensating controls:**
     1. **New runbook instruction:** operators must configure access-telemetry retention of **7 days or less** on their log/OTLP backend, the top of the ratified 1 hour–7 day range (`AccessTelemetryOptionsValidator.cs:95`). Today no such guidance exists.
     2. The Memories Production overlay sets `ACCESS_TELEMETRY_ENABLED=false` and lifecycle `replicas: 0`. CI pins both: the `access_telemetry_c1` Python lane (`ci.yml:388`) and the `integration-fast` lane (`ci.yml:545`). **[Corrected 2026-11-01 by review: those two lanes pin only `ACCESS_TELEMETRY_ENABLED=false`. `replicas: 0` is pinned by `AccessTelemetryA41CloseOutTests` and `ProductionDeploymentArtifactsTests` in `test-unit-contract` (`ci.yml:421`); DW-17 cites the corrected lanes.]**
     3. In Production the options validator rejects enablement unless retention comes from Dapr configuration (`:88`) and the component profile hash is a 64-hex value (`:111`). The overlay ships `unproven`, so flipping the switch alone fails closed. This is a deliberate second step, not a qualification check.
     4. The PRD states that access telemetry is not a retention-compliance guarantee (`prd.md:551`).
   - **Reopen trigger (measurable):** any Production deployment path that sets `AccessTelemetryLifecycle:Enabled=true` or runs a lifecycle Deployment above zero replicas. This covers the Memories overlay and Platform composition, which supplies the value through manifest reference `accesstelemetrylifecycle-enabled` (`module/hexalith.module-manifest.v2.json:272`). The CI pins cover **only** the Memories overlay.
   - **Review date:** 2027-05-01, tracked as a sprint-status action item (§4.6) so a retrospective sees it.
   - **Wording rule:** use "resolved by accepted debt". Never "closed" or "qualified", and never the literal `A41 status | \`closed\``, which `AccessTelemetryA41CloseOutTests.cs:89` forbids.
3. **Park the full campaign** in one deferred-work entry with the same trigger:
   - It reopens **only** on that trigger. "Do recommended", a free schedule slot, or new Platform capability does not reopen it.
   - When it is triggered, re-plan from scratch against the real Production target. The prior synthetic-target preparation is anti-template.
   - Put a "PARKED" banner on `docs/operations/access-telemetry-full-qualification.md`, which is kept as the historical inventory.
4. **Stop the in-flight work**: node1 rootless Docker, calibration, host capture and S3 preparation.
5. **Retained resources:** list them in the report's close-out section with owner Jérôme Piquot. Record either "cleanup done" or "explicitly left in place", rather than leaving the state implicit.
6. **Close Spec 14** with a short closing section. No further continuation sections.

**Options rejected:**

- **Keep pursuing the full campaign (trimmed again).** Tried on 2026-10-25 as the "smaller finite campaign profile". It still needs a 336-hour window and an environment nobody owns yet. Running it on a synthetic target would not reduce the Production risk either; only enabling the store does, and that is the reopen trigger.
- **Close 27.4 but keep A41 `carried-forward`** (fallback for D2). This is honest, but it leaves a permanently open residual for later stories to orbit, and the pinning tests keep the raised bar alive. With the corrected wording and the added compensating control, resolving by debt does not over-close.
- **Rollback.** There is no retention defect.
- **MVP/PRD reduction.** Not needed; NFR34 already allows dated debt.

**Effort and risk:** 1–2 days of documentation and test-guard edits.
- **Risk is low.** Production stays fail-closed, and the reopen trigger is CI-pinned for the Memories overlay.
- **Residual risk, stated in the debt record:** Production access-telemetry retention depends on the operator's log platform until the store is enabled.

## 4. Detailed change proposals (batch)

### 4.1 `epics.md` — Story 27.4 (section starting `epics.md:5329`)

**OLD (summary):** a two-track contract. Local functional retention completes "independently", but overall 27.4 stays in progress until the full campaign passes: all 25 C1 gates, C0–C6, terminal, publication, the A41 publication chain and the "transferred scope" from 27.3 Tasks 2–8.

**NEW (replacement text):**

> #### Scope reset — approved <date> ([proposal](sprint-change-proposal-2026-11-01.md))
>
> Story 27.4's outcome is **retention verified and A41 dispositioned**.
>
> | Checkpoint | Owner | Evidence | Review state | Completion state |
> | :--- | :--- | :--- | :--- | :--- |
> | Local functional retention | Jérôme Piquot | Run `0673d50339f4e1058c26e3a9b2d680a2`: native TRX 1/1, `functional-observations-passed`, verified disposal ([report](../implementation-artifacts/tests/27-4-retention-verification-evidence.md)). Limits: one writer, Development, logical expiry only. | One ordinary review complete | done |
> | Operations runbook | Jérôme Piquot | `docs/operations/access-telemetry-lifecycle.md`, plus the new operator log/OTLP retention instruction (≤ 7 days) | One ordinary review | pending → done on merge |
> | A41 disposition | Jérôme Piquot | DW-17 resolved by accepted debt with every required field; Epic 20 sprint action done; review action due 2027-05-01; guard tests updated | One ordinary review | pending → done on merge |
>
> The full Production qualification campaign (C0–C6, C1.1–C1.25, terminal, publication) and the transferred former 27.3 Tasks 2–8 are **removed from this story** and parked in deferred-work entry DW-<n>. That entry reopens only when a Production deployment path enables lifecycle writes. Earlier full-campaign text, the sole-owner role records and the A41 publication chain are historical.

The As a/I want/So that statement changes from "so that A41 closes only after the policy works in the deployment shape" to "so that A41 is resolved on validated behaviour, with Production enablement held as explicit, trigger-bound debt". The three Full-campaign ACs move verbatim into the deferred entry.

### 4.2 `epics.md` — Epic 27 header (`epics.md:5231`+)

Add a dated correction paragraph: "Story 27.4 completion and A41 no longer require the 25 C1 gates or C0–C6; A41 is resolved by accepted debt; see the 2026-11-01 scope reset." The 2026-08-03 and 2026-09-06 text is retained as history.

### 4.3 `epics.md:4376` — A41 residual guard

Append: "**Resolved by accepted debt <date>** (DW-17). The mechanism is implemented (27.2) and functionally validated within stated limits (27.4 run `0673d503…`). Memories does not bound Production retention while the store is disabled; reopen trigger and review date are in DW-17."

### 4.4 `architecture.md:230`

Append a dated correction: the "Production-shaped evidence" closure bar is replaced by the 2026-11-01 accepted-debt disposition. Production lifecycle writes stay disabled until the parked campaign is triggered and passes.

### 4.5 `deferred-work.md`

- **DW-17:** set `status: resolved`. Record the accepted-debt fields from §3 item 2 verbatim, in an `Evidence:` field; CI rejects `Resolution:`.
- **New DW entry, "Production access-telemetry lifecycle qualification":** status `deferred`. It holds:
  - the three Full-campaign ACs;
  - a pointer to the 34-row inventory;
  - the behavioural reopen trigger from §3 item 2, with the note "only this trigger reopens it";
  - owner Jérôme Piquot;
  - the note "re-plan against the real Production target; prior synthetic-target preparation is anti-template";
  - a follow-up note that the five access-telemetry CI lanes (`ci.yml:368-397`) could move to nightly or be retired.

### 4.6 `sprint-status.yaml`

- `27-4-retention-verification-operations-runbook-and-a41-close-out: done`, with a comment pointing to this proposal.
- Epic 20 action "Keep 20.5-A41-ACCESS-TELEMETRY-RETENTION visible…": `status: done`, with a comment citing DW-17's accepted-debt resolution.
- **New action:** "Review the A41 accepted-debt disposition (DW-17) and the parked Production qualification entry by 2027-05-01 or on its reopen trigger". Epic 27, owner Jérôme Piquot, `status: open`.
- `epic-27: done`. Change `epic-27-retrospective` from `optional` to a recommended short retro: this was the costliest story in the project.
- Replace `story_execution_order.epic-27.reason` with a one-line pointer to this proposal.
- Add one dated line to the comment block at `:467-487`, and supersede the `:475` comment "short Spec 14 governs…".

### 4.7 Spec 14, report, epic context, handoff, full-qualification inventory

- **Spec 14:** add a short "Closed <date> — scope reset" section at the top and set `status: 'done'`. Mark the node1, calibration and setup task boxes `[-] withdrawn by scope reset`; do not tick them as complete.
- **Report:** add a final "Story close-out" section. It covers the functional pass with its limits, the A41 accepted-debt disposition, and the retained-resource list with owner and an explicit "cleaned" or "left in place" disposition.
- **`epic-27-context.md`:**
  - Replace the "Current execution direction" and "Next preparation" paragraphs with the reset.
  - Explicitly supersede `:49`'s "Debt acceptance passes no technical gate and completes neither Story 27.4 nor full qualification" with a dated note: debt acceptance resolves A41 and completes Story 27.4; it still passes no technical gate and grants no full-qualification credit.
  - Preserve the strings pinned by `runtime_control_plane_identity_test.py` (the twenty-three-gates sentence and the C1.15 review path/hashes) as historical text.
- **`docs/operations/access-telemetry-owner-handoff.md`:** rewrite the "next work" paragraphs. It currently says "The next work is the separately scheduled full qualification campaign". New content: the story is closed, the campaign is parked behind the DW trigger, and the operator log-retention instruction applies.
- **`docs/operations/access-telemetry-full-qualification.md`:** add a top banner: "PARKED <date> — historical inventory. Do not execute. Reopens only by DW-<n>'s trigger."
- **`docs/operations/access-telemetry-lifecycle.md`:** add the operator log/OTLP retention instruction (≤ 7 days). Update the "A41 close-out chain" section and the sentence "A41 is open until remote publish verification succeeds" to the accepted-debt disposition.
- **`docs/dev/telemetry.md:221`:** update its A41 sentence to the canonical "resolved by accepted debt" wording.

### 4.8 `_bmad-output/project-context.md:128`

Rewrite the rule as:

> **Never re-open Production lifecycle writes silently.** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is resolved by accepted debt (DW-17, <date>). Never enable `AccessTelemetryLifecycle:Enabled` or scale a lifecycle Deployment above zero on any Production path without reopening the parked qualification entry. Never describe A41 as closed, qualified or Production-bounded.

### 4.9 Document-contract guard tests (complete list)

**[Corrected 2026-11-01 by review: the list below was not complete. It missed `runtime_control_plane_identity_test.py:1495` (DW-17 `status: open`, CI-run) and the report's Evidence-posture pin `AccessTelemetryA41CloseOutTests.cs:44`. Both are handled in §9.]**

Update the pinned wording only; no assertion is weakened.

| File | Lines | Change |
| :--- | :--- | :--- |
| `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryRetentionDecisionTests.cs` | `:453`, `:492`, `:575`, `:656` | "remains open / carried forward" → canonical accepted-debt wording |
| `tests/Hexalith.Memories.Server.Tests/Deployment/AccessTelemetryOperationsContractTests.cs` | `:89` (also review the `:130` test name) | "A41 is open until remote publish verification succeeds" → accepted-debt wording |
| `tests/Hexalith.Memories.Server.Tests/Architecture/AccessTelemetryA41CloseOutTests.cs` | `:89` | No change; it keeps forbidding the "closed" literal, and the wording rule respects it |
| **`tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py`** | **`:1466-1471`** | **Runs in CI (`ci.yml:388`).** It asserts the Epic 20 A41 action is `status: open`. Change it to assert `status: done` together with the DW-17 accepted-debt comment |

**Unchanged, and now carrying the reopen trigger:**
- `runtime_control_plane_identity_test.py:1353` (Python lane in CI)
- `AccessTelemetryLifecycleIntegrationCheckpointTests.cs:594` (`integration-fast`)
- the `replicas: 0` assertion in `AccessTelemetryA41CloseOutTests.cs`

**Left alone:** the A41 mutation framework in `tools/verify_access_telemetry_lifecycle.py` (`:2165`+) and its fixture tests (`test_retention_verification.py`, `test_owner_review_fixes.py`). CI exercises them on fixtures only; this ordinary reviewed change does not use the framework.

### 4.10 Lessons and memory

- **`_bmad-output/process-notes/story-creation-lessons.md`, add L15, "Finish on proven behaviour; park the rest behind a trigger":**
  - Apply it when a story's remaining work qualifies a capability that is disabled in Production, or needs an environment its owner lacks.
  - Finish the story on the proven behaviour and park the remainder in a deferred entry that reopens only on a measurable trigger.
  - Three continuation rounds without a newly passing checkpoint means running correct-course.
  - Reference failure: Story 27.4, 2026-10-25 → 2026-11-01.
  - This is advisory. No new executable gate is added; more governance is the anti-pattern this lesson records.
- **Assistant memory** (outside the repository): on approval, retire `spec14-continuation-bmad-build-deviations`, which tells `/bmad-build 27.4` to resume Spec 14, and record the reset.

## 5. Implementation handoff

**Developer (Amelia):** apply §4.1–4.10 in one change, then run these focused checks:

```bash
# Built-assembly invocation (repo memory: dotnet test is sandbox-blocked)
DiffEngine_Disabled=true dotnet exec <Server.Tests dll> -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests \
  -class Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryA41CloseOutTests \
  -class Hexalith.Memories.Server.Tests.Deployment.AccessTelemetryOperationsContractTests
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p "*_test.py"
PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p "test_*.py"
```

Then run one ordinary review.

**Owner (Jérôme):** approve or veto D1–D4. Stop any node1 setup sessions. Decide the retained-resource disposition: clean up, or explicitly leave in place.

**Success criteria:**
- Story 27.4 and Epic 27 are `done`.
- DW-17 is `resolved` with every accepted-debt field and an `Evidence:` field.
- The parked DW entry exists with its behavioural trigger.
- The review action is open with due date 2027-05-01.
- The runbook contains the operator retention instruction.
- project-context `:128` has been rewritten.
- The handoff and full-qualification documents point to the parked entry.
- The focused guards are green, including the CI-run `access_telemetry_c1` lane.
- The Memories Production overlay still has `ACCESS_TELEMETRY_ENABLED=false`.
- No new 27.4 continuation spec is created.

### Decisions requiring the owner

| # | Decision | Recommendation |
| :--- | :--- | :--- |
| D1 | Remove the full Production qualification campaign from 27.4 and park it as a trigger-bound deferred entry | Yes |
| D2 | Resolve A41 by accepted debt now, using the corrected wording and the new operator-retention control, rather than closing 27.4 and keeping A41 `carried-forward` | Resolve now |
| D3 | Debt review date | 2027-05-01, tracked as a sprint-status action, plus the behavioural trigger |
| D4 | Stop node1 rootless-Docker and calibration work; leave Platform code in place | Yes |

## 6. Policy records

### Historical Context Classification

| Prior artifact | Classification | Permitted use |
| :--- | :--- | :--- |
| `sprint-change-proposal-2026-10-21.md` (one outcome, run once, one short report) | current-narrow-pattern | Re-verified: it produced the 2026-10-25 pass. Only its "one outcome / one review" rule is reused. |
| Spec 14 continuation sections dated 2026-10-25 → 2026-10-31; specs 15–26; the 34-row full-campaign inventory | anti-template | Checkpoint-heavy, bundled infrastructure scope that shapes no story. The inventory is kept only as a parked historical pointer. |
| Stories 27.21 / 27.22 | historical-reference-only | Gate-identity provenance for the parked entry. |
| Story 27.4 transferred scope (former 27.3 Tasks 2–8) | anti-template | Moves verbatim into the parked entry; its shape is not reused. |

### Slice Proof

This correction creates no story. Narrowed 27.4 has one outcome, "retention verified and A41 dispositioned", tracked by an approved three-row checkpoint table. Each row carries owner, evidence, review state and completion state. The 34-gate campaign is not registered as a story at any status. It is a deferred entry, so the split does not reproduce the umbrella shape it removes.

### Epic AC Verification

Verified 2026-11-01 against `848f0234` (main).

| Claim (quoted) | Class | Command / evidence | Observed | Verdict |
| :--- | :--- | :--- | :--- | :--- |
| "Local functional completion is proved. Authorized run `0673d50339f4e1058c26e3a9b2d680a2`…" | Behavioral | `grep -n 0673d50339f4e1058c26e3a9b2d680a2 _bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md` | `:1555` completed/Success/0/HXI005, TRX 1/1; `:1498` re-read 1/1 passed | confirmed |
| "Strong TTL absence and due-index cleanup do not establish physical tuple deletion or reclamation." | Behavioral | `sed -n 3144p <report>` | Present | confirmed (limit recorded in debt scope) |
| A41 "remains `carried-forward`… until either bounded retention/TTL is implemented and validated or an explicit accepted-debt disposition records…" | Existence | `sed -n 4376p _bmad-output/planning-artifacts/epics.md`; `sed -n 629,633p _bmad-output/implementation-artifacts/sprint-status.yaml`; `grep -n "DW-17" _bmad-output/implementation-artifacts/deferred-work.md` | Present at all three; DW-17 status `open`, resolution gate same rule | confirmed |
| `architecture.md:230`: A41 "remains open until implementation and Production-shaped evidence pass or an explicit accepted-debt disposition…" | Existence | `sed -n 230p _bmad-output/planning-artifacts/architecture.md` | Present. The raised bar changes a ratified decision → escalated as D1/D2 | confirmed (escalated) |
| "Debt acceptance passes no technical gate and completes neither Story 27.4 nor full qualification." | Existence | `grep -n "Debt acceptance passes no technical gate" _bmad-output/implementation-artifacts/epic-27-context.md` | `:49`. Contradicts D2 → explicitly superseded in §4.7 | confirmed (escalated) |
| project-context A41 rule "Never hide or over-close…" | Location | `grep -n "Never hide or over-close" _bmad-output/project-context.md` | `:128` | confirmed |
| NFR34 includes "dated accepted debt for unsupported retention profiles" | Existence | `sed -n 1205p _bmad-output/planning-artifacts/prd.md` | Present | confirmed |
| Access telemetry "does not guarantee append-only storage, integrity verification, or retention compliance" | Existence | `sed -n 551p _bmad-output/planning-artifacts/prd.md` | Present | confirmed |
| "Production writes remain disabled" | Behavioral | `grep -rn ACCESS_TELEMETRY_ENABLED deploy/`; `grep -n replicas deploy/kubernetes/base/access-telemetry-deployments.yaml` | Production overlay `:13` `=false`; base `:32` `=false`; qualification overlay `:74` `=true`; lifecycle `replicas: 0` (`:9`, `:149`) | confirmed (Memories overlay) |
| Disabled Production state is CI-pinned | Existence | `grep -rn ACCESS_TELEMETRY_ENABLED tests`; `grep -n "access_telemetry_c1\|Category=Integration&" .github/workflows/ci.yml` | Python pin `:1353` runs at `ci.yml:388`; integration pin `:594` (`Category=Integration`) runs at `ci.yml:545` | corrected (review): these pin `ENABLED=false` only; `replicas: 0` is pinned at `ci.yml:421` (`test-unit-contract`) |
| Platform composition supplies `AccessTelemetryLifecycle:Enabled` | Location | `sed -n 268,273p module/hexalith.module-manifest.v2.json` | Reference `accesstelemetrylifecycle-enabled` at `:272`. Not covered by the Memories overlay pins | confirmed (trigger widened) |
| Production validator requires Dapr-configuration retention and a 64-hex profile hash | Behavioral | `sed -n 86,112p src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryOptionsValidator.cs` | `:88-90`, `:111`; hash not checked against an approved profile | confirmed (stated as a deliberate step, not qualification) |
| Retention range is one hour to seven days | Behavioral | `sed -n 95p <validator>` | Present | confirmed |
| No operator log/OTLP retention guidance exists | Absence | `grep -n -i "log platform\|log backend\|collector retention\|OTLP.*retention\|retention.*OTLP" docs/operations/access-telemetry-lifecycle.md docs/dev/telemetry.md` | No match | confirmed (control added in §4.7) |
| Full campaign has 34 checkpoints, all pending/open | Quantitative | `sed -n '/^## Full campaign gates/,$p' docs/operations/access-telemetry-full-qualification.md \| grep -cE '\| pending \| open \|$'` | 34 of 34 | confirmed |
| "a separate 336-hour reservation containing the existing 216-hour C3 ceiling" | Quantitative | `grep -n 336-hour <spec 14>` | `:1244` | confirmed |
| node1 setup needs new host scope ("Installing privileged mapping helpers, adding a private AppArmor exception, a new disk facility…") | Existence | `sed -n 69,85p <spec 14>` | `:69` quota decision open; `:78-80` outside approval | confirmed |
| "The next work is the separately scheduled full qualification campaign" | Existence | `grep -n "next work" docs/operations/access-telemetry-owner-handoff.md` | Present | confirmed (rewritten in §4.7) |
| A CI-run test pins the Epic 20 A41 action to `status: open` | Behavioral | `sed -n 1466,1471p tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` | Asserts `^    status: open`; lane at `ci.yml:388` | confirmed (updated in §4.9) |
| Other guard tests pin the "open / carried forward" wording | Location | `grep -n "A41" tests/Hexalith.Memories.Server.Tests/Architecture/*.cs tests/Hexalith.Memories.Server.Tests/Deployment/AccessTelemetryOperationsContractTests.cs` | RetentionDecisionTests `:453,492,575,656`; OperationsContractTests `:89`; A41CloseOutTests `:89` forbids "closed" | confirmed |
| CI does not invoke the A41 mutation framework itself | Absence | `grep -rn "verify_access_telemetry_lifecycle\|verify-access-telemetry-lifecycle" .github/workflows` | No match; its fixture tests run at `ci.yml:380` | confirmed |
| 91 commits mention 27.4; 15 since the local pass | Quantitative | `git log -i --grep='27.4' --oneline \| wc -l`; same with `--since=2026-10-25T08:00` | 91; 15 | confirmed |
| Spec 14 / report / inventory sizes 1,979 / 3,173 / 987 lines | Quantitative | `wc -l` on the three files | 1979 / 3173 / 987 | confirmed |
| Stories 27.1, 27.2, 27.3, 27.21, 27.22 are `done` | Existence | `sed -n 470,476p _bmad-output/implementation-artifacts/sprint-status.yaml` | All `done`; 27.4 `in-progress` | confirmed |
| No other open DW entry depends on 27.4 completion | Existence | `awk '/^### DW-/{h=$0} /27\.4/{print h}' _bmad-output/implementation-artifacts/deferred-work.md \| sort -u` | DW-17 open (target); DW-729 open but concerns the 31.1 NetworkPolicy; the others are done | confirmed |

## 7. Checklist disposition

| Item | Status |
| :--- | :--- |
| 1.1–1.3 trigger, problem, evidence | [x] Story 27.4. Scope creep and process over-engineering. Evidence in §1 and §6. |
| 2.1–2.5 epic impact | [x] Epic 27 can complete. No future epic depends on the full campaign. No resequencing. |
| 3.1 PRD | [x] No requirement change; NFR34 already allows dated debt. |
| 3.2 Architecture | [!] Dated correction at `:230` (§4.4). |
| 3.3 UX | [N/A] |
| 3.4 Other artifacts | [!] deferred-work, sprint-status, Spec 14, report, epic context, handoff, inventory banner, runbook, telemetry.md, project-context, guard tests, lessons (§4.5–4.10). |
| 4.1–4.4 options | [x] Option A selected; alternatives rejected in §3. |
| 5.1–5.5 proposal and handoff | [x] §1–§5. |
| 6.1–6.2 review | [x] Including advanced elicitation (§8). |
| 6.3 owner approval | [x] Owner "yes" to D1–D4, 2026-11-01 |
| 6.4 sprint-status update | [x] Applied (§9) |
| 6.5 handoff confirmation | [x] §9; commit left to the owner |

## 8. Elicitation record — 2026-11-01

The owner ran Steelmanning, Pre-mortem, Assumption Audit, Second-Order Thinking and 5 Whys on the first draft and said **"apply"** to every finding.

| Method | Finding | Applied in |
| :--- | :--- | :--- |
| Steelmanning | "Validated" wording over-closed A41. Production retention is not bounded by Memories, and no operator retention control existed. | §1 limits, §3 item 2 (wording rule, risk, control 1), §4.7 runbook |
| Pre-mortem | The review date could lapse silently. Production could be enabled through Platform, bypassing the overlay pins. Stale pointers (handoff document, memory note, sprint comment) could restart the campaign. Retained resources had no owner. | §3 item 2 (review action, behavioural trigger), §3 item 5, §4.6, §4.7, §4.10 |
| Assumption Audit | The guard-test list was incomplete (a CI-run test pins the A41 action to `open`). project-context `:128` and `epic-27-context.md:49` were missed. | §4.7, §4.8, §4.9 |
| Second-Order Thinking | Missing the CI pin would break the build on `main`. The five parked CI lanes carry ongoing cost. A retro is warranted. | §2 CI row, §4.5 follow-up note, §4.6 retro |
| 5 Whys | Root cause: the completion bar was never revisited, and a missing-capability blocker had no stop rule. | §1 root cause, §3 item 3 ("only this trigger"), §4.10 L15 |

## 9. Implementation record — 2026-11-01

Applied the same day as approval. Nothing was staged or committed; the owner commits through `/pushall`.

### Changed files

- **Planning:** `epics.md` (Story 27.4 scope-reset block and checkpoint table, historical labels, Epic 27 header,
  A41 guard at `:4376`), `architecture.md` (dated correction), `prd.md` (NFR evidence-status note).
- **Tracking:** `deferred-work.md`:
  - DW-17 set to `status: done 2026-11-01`, with a `resolution:` line carrying every accepted-debt field and a `decision:` line;
  - new DW-730 (parked campaign, `decision: keep-open`);
  - the legacy S3 calibration items marked as parked.

  `sprint-status.yaml`:
  - 27.4 and Epic 27 set to `done`;
  - the Epic 20 A41 action set to `done`;
  - a new Epic 27 review action due 2027-05-01;
  - the execution-order reason superseded, and the retro recommended.
- **Story artifacts:** Spec 14 (`status: 'done'`, closing section, eight boxes marked `[-]` withdrawn), the results
  report (close-out section; Evidence posture marked historical), `epic-27-context.md`.
- **Documentation:**
  - `docs/operations/access-telemetry-lifecycle.md`: the new "Production access-telemetry retention" section (the operator ≤ 7-day control) and a dated correction to the A41 chain;
  - `access-telemetry-owner-handoff.md`: rewritten;
  - `access-telemetry-full-qualification.md`: PARKED banner;
  - `access-telemetry-adapter-production.md` and `access-telemetry-c1-owner-verification.md`: dated pointers;
  - `rate-limiting.md` and `docs/dev/telemetry.md`: A41 wording updated;
  - `_bmad-output/project-context.md`: rule rewritten;
  - `story-creation-lessons.md`: L15 added.
- **Guard tests** (wording updated; each removed pin replaced by an equal or stronger pin of the new state):
  - `AccessTelemetryRetentionDecisionTests.cs`: also pins the "Memories does not bound" statement and the ≤ 7-day control;
  - `AccessTelemetryOperationsContractTests.cs`: also pins the new section and keeps a "not closed as qualified" guard;
  - `AccessTelemetryA41CloseOutTests.cs`: pins the historical marker on Evidence posture;
  - `runtime_control_plane_identity_test.py`: pins 27.4 `done`, the Epic 20 action `done`, and DW-17 `status: done 2026-11-01` with its resolution line.

### Review (one ordinary review)

The reviewer's verdict was approve with fixes: 1 high, 3 medium and 7 low findings. All were fixed in this change.

| Finding | Fix |
| :--- | :--- |
| High: CI-run `runtime_control_plane_identity_test.py:1495` still required DW-17 `status: open` | Pins `status: done 2026-11-01` plus the resolution line |
| Medium: report Evidence posture still current-tense | Dated historical marker, pinned in `AccessTelemetryA41CloseOutTests` |
| Medium: `epic-27-context.md` Goal and dependencies still current-tense | Banner widened; Goal qualified |
| Medium: this proposal still read "awaiting decision" | Status, checklist and this record |
| Low: wrong CI lane cited for the `replicas: 0` pin | DW-17 and §3 corrected (`ci.yml:421`) |
| Low: two operations pages still pointed to Spec 14 as current | Dated pointers added |
| Low: test pin accepted both the old and the new 27.4 state | Pins `done` exactly |
| Low: rate-limiting no-closure guard lost | `not closed as qualified` pinned (whitespace-normalised) |
| Low: three pre-existing LF lines in `docs/dev/telemetry.md` | File normalised to CRLF |
| Low: runbook overstated the routing recipe | Reworded to "names the category" |
| Low: legacy S3 calibration items and the stale memory note | Items marked parked; memory note retired |

### Verification (re-runnable)

| Check | Command | Result |
| :--- | :--- | :--- |
| .NET guard classes, assembly rebuilt and confirmed fresh by `strings -el` | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class …AccessTelemetryRetentionDecisionTests -class …AccessTelemetryA41CloseOutTests -class …AccessTelemetryOperationsContractTests -class …ConsistencyModelDecisionTests -class …InstrumentationInventoryTests -class …ProductionDeploymentArtifactsTests` | 39/39 passed |
| Deferred-work field contract and CI inventory, rebuilt at `f80a7e58` | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` | 136/136 passed |
| Python tooling lanes | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/<lane> -p '<pattern>'` for `access_telemetry_c1`, `access_telemetry_lifecycle`, `bmad_customization`, `story_review_readiness`, `story_scope`, `story_slice_scope`, `integration_stub_closure` | 126, 141, 33, 105, 71, 20 and 7 tests: all OK |
| YAML validity | `python3 -c "import yaml; yaml.safe_load(open('_bmad-output/implementation-artifacts/sprint-status.yaml'))"` | parses; 27.4 `done`, epic-27 `done`, A41 action `done` |
| Line endings | `grep -c $'\r$' <file>` vs `wc -l` | CRLF files fully CRLF; `sprint-status.yaml` and the `.py` stay LF |

### Notes

- **Concurrent commits:** commits `187c8e2c`…`f80a7e58` from another session landed during this work. They touch none of these files.
  The three `CiTestInventoryTests` failures seen on the first run came from a stale assembly. They were fixed by concurrent
  commit `a4788c89` and are unrelated to this change.
- **C1 lane runtime:** the `access_telemetry_c1` lane took 2,312 s locally. That is close to the 2,400 s local timeout used here; worth
  watching in CI.
- **Still open for the owner:**
  - commit through `/pushall`;
  - clean up the retained resources, or confirm they stay in place (the report lists them as left in place);
  - the recommended Epic 27 retrospective;
  - the 2027-05-01 debt review.
