---
project: memories
date: 2026-07-16
status: approved
review_mode: batch
change_scope: minor
change_trigger: Preserve TenantProvisioningWorkflow as the sole owner of tenant infrastructure creation during Epic 1 and Epic 5 rework
---

# Sprint Change Proposal — Preserve Tenant Provisioning Ownership

## 1. Issue Summary

Epic 0 retrospective action item 2 requires future Epic 1 and Epic 5 rework to preserve `TenantProvisioningWorkflow` as the sole owner of RediSearch, Redis Vector, and FalkorDB tenant infrastructure creation. The action remains `open` in `sprint-status.yaml`.

The product and architecture direction is already correct:

- The PRD implementation sequence states that `TenantProvisioningWorkflow` owns physical tenant infrastructure creation and that ingestion/indexing fail before backend writes when tenant or case context is invalid.
- `architecture.md` identifies `TenantProvisioningWorkflow` as the sole tenant index/database creation owner and requires ingestion, search, graph, CLI, and MCP paths to validate existing active infrastructure rather than create it.
- `epics.md` already gives Story 1.5 and Story 5.1 explicit rework ownership gates.

The remaining problem is artifact drift in the completed implementation story records. Story 1.5 still contains historical tasks and constraints instructing indexing activities to run `FT.CREATE` on demand. Story 5.1 still records intermediate refactoring tasks that routed indexing activities through `FT.CREATE`. Those statements accurately describe an earlier implementation stage, but without an explicit supersession notice they remain dangerous templates for future Epic 1 or Epic 5 rework.

Story 23.7 subsequently removed on-demand creation from indexing paths. Current implementation evidence confirms:

- `IndexSyntacticActivity`, `IndexSemanticActivity`, `IndexSemanticChunksActivity`, `IndexNaturalLanguageSemanticActivity`, and `RedisSearchIndexMaintenanceAdapter` call `ITenantIndexReadinessVerifier` and do not create indexes.
- `ITenantIndexReadinessVerifier` documents that missing indexes are provisioning inconsistencies and throws `TenantIndexNotProvisionedException` instead of creating resources.
- `IndexingHotPathGuardTests.IndexWriteHotPaths_DoNotCreateIndexesOnDemand` prevents per-document `FT.CREATE` from returning to the covered production paths.
- Normal tenant index creation remains in `ProvisionRediSearchActivity` and `ProvisionRedisVectorActivity`, orchestrated by `TenantProvisioningWorkflow`; tenant graph initialization remains in `ProvisionFalkorDbActivity`.

This course correction therefore closes a documentation/governance gap, not a current runtime defect.

## 2. Impact Analysis

### Epic impact

- **Epic 0:** Direction remains valid. Retrospective action item 2 can close after the approved story-record clarifications and planning guard are applied.
- **Epic 1:** Scope and sequencing remain unchanged. Future indexing rework must be split into vertical stories and may verify existing tenant infrastructure only. It must never restore on-demand resource creation from historical Story 1.5 instructions.
- **Epic 5:** Scope remains the full tenant lifecycle. Future rework may modify creation semantics only inside `TenantProvisioningWorkflow` and its provisioning/verification/compensation activities; it must extend the same workflow introduced by the foundation slice, not introduce another creator.
- **Epic 23 / Story 23.7:** Completed behavior already enforces the desired ownership. Its compact acceptance criteria in `epics.md` should be strengthened so the planning source records the fail-closed ownership rule already present in the implementation story and code.
- **Other epics:** No epic becomes obsolete, no new epic is needed, and no resequencing is required. Migration-specific creation of versioned or staging indexes remains governed by approved migration stories and must not be generalized into initial tenant provisioning or feature-path create-if-missing behavior.

### Story impact

- Clarify completed Story 1.5 as historical evidence whose create-on-demand instructions are superseded.
- Clarify completed Story 5.1 so its intermediate indexing-activity `FT.CREATE` tasks cannot be reused during tenant-lifecycle rework.
- Expand the planning acceptance criteria for Story 23.7 to name the no-create and fail-closed behavior explicitly.
- Do not reopen or change the completion status of Stories 1.5, 5.1, or 23.7.

### Artifact conflicts

- **PRD:** No modification required. The invariant is already explicit and MVP scope remains achievable.
- **Architecture:** No modification required. The workflow ownership, consumer behavior, and compensation model are already explicit.
- **UX Design:** Not applicable. No interaction, accessibility, or presentation behavior changes.
- **Epics/stories:** Action required to reconcile concise current planning with stale completed-story implementation instructions.
- **Sprint tracking:** After approval and application, update only the Epic 0 retrospective action item from `open` to `done`. No epic/story status, number, or execution order changes.
- **Testing:** Preserve and run the existing ownership guard and focused readiness/provisioning tests. No new runtime implementation is required unless verification finds drift.

### Technical impact

The current runtime already follows the target architecture. The adjustment prevents future regressions by making the authoritative current rule visible wherever a developer might begin Epic 1 or Epic 5 rework.

Production feature paths must remain consumers of active tenant infrastructure:

- Allowed: validate active tenant status; inspect index/database existence and schema; write tenant data; fail with a structured provisioning or schema inconsistency.
- Allowed only within the tenant lifecycle owner: create tenant indexes/databases; verify creation; compensate partially created resources; transition provisioning status.
- Forbidden in ingestion, indexing, search, graph, CLI, MCP, or ordinary endpoint paths: create-if-missing, implicit `FT.CREATE`, implicit tenant graph/database initialization, or fallback creation after readiness failure.

## 3. Recommended Approach

Select **Direct Adjustment**.

- **Effort:** Low — approximately 1–2 hours for story/planning edits, focused validation, and action-item closure.
- **Risk:** Low — documentation and governance alignment only; runtime behavior should remain unchanged.
- **Timeline impact:** None expected.
- **MVP impact:** None. The change preserves an existing hard-gate invariant and does not add capability or scope.
- **Release impact:** None.

Alternatives considered:

- **Potential rollback:** Not viable. Reintroducing per-document creation would weaken tenant lifecycle consistency, compensation, and isolation.
- **PRD/MVP review:** Not needed. The PRD and architecture already state the desired ownership and remain achievable.
- **New story or epic:** Not needed unless focused verification discovers that the current runtime has drifted from Story 23.7. Current repository evidence shows no such drift.

## 4. Detailed Change Proposals

### A. Completed Story 1.5 — add a supersession notice

**Artifact:** `_bmad-output/implementation-artifacts/1-5-three-backend-indexing.md`

**Section:** Immediately after `Status: done`

**OLD:**

```markdown
Status: done

## Story
```

**NEW:**

```markdown
Status: done

## Rework Supersession Notice

This file preserves the historical implementation record for Story 1.5. Its tasks and constraints that instruct indexing activities to call `FT.CREATE`, create indexes on demand, or treat create-if-exists as an indexing concern are superseded by Story 5.1 and Story 23.7.

For any Epic 1 rework, `TenantProvisioningWorkflow` remains the sole owner of RediSearch, Redis Vector, and FalkorDB tenant infrastructure creation. Ingestion/indexing activities may verify existing active infrastructure and write tenant data only. Missing or incompatible infrastructure must fail before writes with `TENANT_NOT_PROVISIONED`, schema-mismatch, or equivalent structured evidence; it must never trigger implicit creation. Graph node/edge writes inside an already-provisioned tenant database are data writes, not permission to create the tenant database lifecycle resource.

Do not reuse this completed broad story as a rework template. Use newly numbered vertical stories and the rework ownership gate in `epics.md`.

## Story
```

**Rationale:** Keeps historical traceability while preventing obsolete create-on-indexing instructions in Tasks 4/5 and Critical Constraint 7 from becoming current implementation guidance.

### B. Completed Story 5.1 — add a reconciliation notice

**Artifact:** `_bmad-output/implementation-artifacts/5-1-tenant-provisioning-workflow.md`

**Section:** Immediately after `Status: done`

**OLD:**

```markdown
Status: done

## Story
```

**NEW:**

```markdown
Status: done

## Rework Reconciliation Notice

This file preserves the historical transition from indexing-owned `FT.CREATE` calls to tenant-lifecycle ownership. Completed intermediate tasks that mention refactoring `IndexSyntacticActivity` or `IndexSemanticActivity` to issue `FT.CREATE` describe the state at the time and are superseded by Story 23.7.

For any Epic 5 rework, extend the existing `TenantProvisioningWorkflow` and its provisioning, verification, compensation, and status activities. Do not introduce a replacement workflow, direct endpoint creator, indexing/search/graph creator, or create-if-missing fallback. `ProvisionRediSearchActivity`, `ProvisionRedisVectorActivity`, and `ProvisionFalkorDbActivity` remain the tenant infrastructure creation activities orchestrated by that workflow; feature paths validate and consume their output.

Do not reopen Story 5.1 to duplicate the Epic 0 minimum slice. Verify or extend the same lifecycle implementation and preserve rollback plus focused cross-tenant negative evidence.

## Story
```

**Rationale:** Makes the canonical lifecycle owner unmistakable while preserving the completed story's historical sequence and evidence.

### C. Epic planning — strengthen Story 23.7 ownership acceptance criteria

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**Story:** 23.7, `Index-Provisioning Ownership`

**Section:** Acceptance Criteria

**OLD:**

```markdown
**Given** each indexed document attempts `FT.CREATE` with exception-as-control-flow and `Thread.Sleep` (`IndexSyntacticActivity.cs:55-66,205`),
**When** documents are indexed,
**Then** index verification is memoized per tenant per process, `Thread.Sleep` is replaced with `Task.Delay`, and the per-ingest warning is removed. Closes A34.
```

**NEW:**

```markdown
**Given** each indexed document attempts `FT.CREATE` with exception-as-control-flow and `Thread.Sleep` (`IndexSyntacticActivity.cs:55-66,205`),
**When** documents are indexed,
**Then** index verification is memoized per tenant, index family, and expected schema per process, `Thread.Sleep` is replaced with `Task.Delay`, and the per-ingest warning is removed
**And** the readiness verifier inspects existing infrastructure only and never issues `FT.CREATE` or initializes a tenant graph/database.

**Given** an active tenant is missing a required index/database or has incompatible schema,
**When** an ingestion, indexing, search, graph, CLI, or MCP path checks readiness,
**Then** it fails before data access with `TENANT_NOT_PROVISIONED`, schema-mismatch, or equivalent structured evidence
**And** it does not create or repair tenant lifecycle resources on demand.

**Given** Epic 1 or Epic 5 rework is reviewed,
**When** ownership evidence is collected,
**Then** `TenantProvisioningWorkflow` remains the sole owner of tenant infrastructure creation
**And** focused source/architecture guards prove feature paths do not contain create-if-missing behavior. Closes A34 and preserves the Epic 0 ownership invariant.
```

**Rationale:** Aligns the compact epic source with the full Story 23.7 artifact and current implementation, preventing future readers from interpreting “verify once” as “create once if missing.”

### D. Sprint tracking — close the retrospective action after application

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Section:** `action_items`, Epic 0 ownership action

**OLD:**

```yaml
  - epic: 0
    action: "Preserve TenantProvisioningWorkflow as the sole owner of tenant infrastructure creation during Epic 1 and Epic 5 rework"
    owner: "Winston, Amelia"
    status: open
```

**NEW:**

```yaml
  - epic: 0
    action: "Preserve TenantProvisioningWorkflow as the sole owner of tenant infrastructure creation during Epic 1 and Epic 5 rework"
    owner: "Winston, Amelia"
    status: done  # 2026-07-16: Historical Story 1.5/5.1 rework notices and Story 23.7 planning AC now pin workflow-only creation; existing ownership guard retained.
```

**Rationale:** Closes the retrospective commitment only after the approved safeguards are present. No epic or story delivery status changes.

### E. PRD, Architecture, and UX

No edits proposed.

- PRD implementation sequencing already states the invariant.
- Architecture already assigns sole ownership and fail-closed consumer behavior.
- UX is unaffected.

## 5. Implementation Handoff

**Classification:** Minor — direct planning/story-record adjustment with focused verification; no backlog reorganization or product replan.

**Primary recipient:** Developer agent (Amelia).

**Architect reviewer:** Winston, for ownership-boundary confirmation.

### Responsibilities

Developer agent:

1. Apply the approved edits to Story 1.5, Story 5.1, Story 23.7 in `epics.md`, and the Epic 0 action item in `sprint-status.yaml`.
2. Preserve completed story statuses and historical content; add clarification rather than rewriting history as though the old implementation never existed.
3. Run the existing ownership/readiness evidence:
   - `IndexingHotPathGuardTests.IndexWriteHotPaths_DoNotCreateIndexesOnDemand`
   - `TenantIndexReadinessVerifierTests` focused class
   - provisioning workflow/activity tests covering RediSearch, raw/NL Redis Vector, FalkorDB initialization, verification, and compensation
4. Confirm no production ingestion, indexing, search, graph, CLI, or MCP path creates tenant lifecycle resources on demand.
5. Attach focused cross-tenant negative evidence if any implementation or scope-sensitive test code changes are needed; a planning-only edit does not authorize unrelated runtime changes.

Architect reviewer:

1. Confirm the wording distinguishes tenant infrastructure lifecycle creation from ordinary data writes inside already-provisioned indexes/databases.
2. Confirm migration-specific staging/versioned-index work remains separately governed and is not turned into a generic feature-path creation exception.

### Success criteria

- Historical Story 1.5 and Story 5.1 each carry an explicit current rework notice.
- Story 23.7 planning AC states inspection-only readiness and fail-closed missing-infrastructure behavior.
- `TenantProvisioningWorkflow` remains the only normal tenant lifecycle creation orchestrator.
- Feature paths contain no create-if-missing behavior for tenant indexes/databases.
- Existing ownership, readiness, provisioning, compensation, and cross-tenant tests pass or an accepted blocker names owner, consequence, and reopen trigger.
- The Epic 0 retrospective action is marked `done` only after the approved edits and evidence are complete.
- PRD, MVP scope, epic ordering, and completed story statuses remain unchanged.

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified: Epic 0 retrospective action item 2; Story 23.7 provides the implementation correction that exposed stale historical guidance.
- [x] 1.2 Core problem defined: technical implementation evolution plus artifact drift could reintroduce a second tenant infrastructure creation path during rework.
- [x] 1.3 Evidence collected from PRD, epics, architecture, UX, sprint status, Stories 1.5/5.1/23.7, current workflow/readiness code, and ownership guard tests.

### 2. Epic impact assessment

- [x] 2.1 Epic 0, Epic 1, Epic 5, and Epic 23 remain completable/completed as planned.
- [x] 2.2 Existing epic/story wording needs direct clarification; no epic scope redefinition.
- [x] 2.3 Remaining epics reviewed; no dependency invalidation or resequencing required.
- [N/A] 2.4 No epic is obsolete and no new epic is needed.
- [N/A] 2.5 Priorities remain unchanged.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; already aligned, no edit required.
- [x] 3.2 Architecture reviewed; already aligned, no edit required.
- [N/A] 3.3 UX has no affected flows or components.
- [x] 3.4 Story history, sprint tracking, source guards, migration boundary, and tests reviewed; explicit story/planning clarification required.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable; effort low, risk low, timeline unchanged.
- [N/A] 4.2 Rollback is not justified and would weaken the ownership model.
- [N/A] 4.3 PRD/MVP review is unnecessary; core goals and scope remain valid.
- [x] 4.4 Direct Adjustment selected because current code already conforms and the residual gap is governance/documentation drift.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact and implementation sequence documented.
- [x] 5.5 Minor-scope Developer handoff with Architect review defined.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items completed; approval-dependent items remain identified.
- [x] 6.2 Proposal checked for consistency with current planning and implementation evidence.
- [!] 6.3 Explicit user approval pending.
- [!] 6.4 Sprint status action-item closure pending approval and application; no epic/story row changes required.
- [!] 6.5 Final handoff confirmation pending approval.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | Trigger confirmed from Epic 0 retrospective action item 2 | Complete |
| 2026-07-16 | Batch review mode selected | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, project context, and checklist loaded | Complete |
| 2026-07-16 | Stories 1.5, 5.1, 23.7, sprint status, current workflow/readiness code, and guard tests inspected | Complete |
| 2026-07-16 | Direct Adjustment selected | Proposed |
| 2026-07-16 | Sprint Change Proposal written | Awaiting approval |
| 2026-07-16 | User approval received | Approved |
