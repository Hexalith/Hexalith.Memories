---
change_trigger: "Keep cross-tenant negative validation evidence attached to future scope-sensitive changes"
mode: batch
status: approved-and-applied
approved_by: Administrator
project: Hexalith.Memories
date: 2026-07-16
scope_classification: minor
---

# Sprint Change Proposal: Durable Cross-Tenant Negative-Evidence Gate

Date: 2026-07-16  
Project: Hexalith.Memories  
Scope: Minor planning and project-governance correction affecting future story/spec creation, implementation records, review evidence, and sprint corrections.

## 1. Issue Summary

The project already has the right safety intent and strong canonical evidence:

- PRD FR44 and NFR8 require tenant context enforcement at every layer and make zero cross-tenant leakage a hard gate.
- Story 20.2 proves cross-tenant denial before tenant state, search, graph, actor, workflow, registry, Redis, FalkorDB, or dedup dependencies are invoked.
- Story 24.3 proves verifier checks fail closed from target-prefix tenant-marker evidence and backend/metadata failures.
- `_bmad-output/project-context.md` requires negative coverage when tenant routing, index names, graph selection, actor IDs, MCP authorization, or UI scope display changes.
- The approved 2026-07-06 sprint correction added a cross-tenant negative-evidence carry-forward guard to `epics.md` and kept the Epic 0 retrospective action in progress.

The remaining gap is durability and attachment quality. The current `epics.md` guard applies only to "future scope-sensitive Epic 20-26" stories, so it expires when Epic 26 closes even though tenant-sensitive fixes, refactors, review patches, and new epics will continue. Its requirements also live separately from the shorter root project-context rule, allowing a future change to run general auth or isolation suites without recording which negative boundary proof the change preserves.

An artifact-level review of later work confirms that this is an evidence-governance gap, not a claim of a known product leak:

| Later scope-sensitive record | Attachment observed |
|---|---|
| Story 25.6 MCP tool executor | Strong: cites the 2026-07-06 proposal, names denial-before-client behavior, and records all-four cross-tenant denial results. |
| Stories 26.2 and 26.3 | Strong or targeted: require cross-tenant denial/fail-closed checks when route/auth/state surfaces change and record tenant-independent backing-state evidence. |
| Stories 25.1, 25.2, 25.3, 25.7, and 25.8 | Partial at the artifact level: preserve general authorization, restrictive-scope, or tenant-isolation behavior, but do not consistently link the canonical prior evidence and a focused negative-validation result in the change record. |

Completed stories are not reopened by this proposal. They are evidence that a prose guard scoped to one epic range is not sufficiently self-carrying for future work.

## 2. Impact Analysis

### Epic impact

- Epic 0 remains done; its retrospective action remains an active carry-forward obligation.
- Epics 20 and 24 remain done and continue to supply canonical evidence anchors.
- Epics 25 and 26 remain complete or in their current states; no completed story is reopened.
- Every future epic inherits the guard when work touches tenant or case scope, regardless of epic number.
- No new epic, resequencing, or product-scope change is required.

### Story and change impact

The guard applies conditionally to any future story, spec, refactor, fix, review patch, sprint correction, or implementation change that touches a scope-sensitive surface, including:

- tenant/case route grouping, route tables, or versioning;
- endpoint filters, middleware, auth/claim normalization, tenant status, or principal-derived scope;
- MCP tool execution or client calls;
- evidence-packet scope metadata, restrictive states, or web scope rendering;
- verifier logic, tenant markers, key/index routing, graph selection, actor IDs, storage selectors, or query builders;
- search, graph, case attribution, export/import, backup/restore, or other data movement across tenant-scoped stores; and
- a refactor that moves any of those paths without intending a behavior change.

For an applicable change, its story/spec and completion or review record must attach:

1. the impacted scope-sensitive surfaces;
2. the applicable prior evidence anchor, normally Story 20.2 for denial-before-dependency boundaries and Story 24.3 for verifier/tenant-marker fail-closed behavior, or a linked newer canonical replacement;
3. focused negative test names plus the command and result proving cross-tenant denial or fail-closed behavior appropriate to the changed surface; and
4. if proof cannot run, an explicit accepted blocker with owner, consequence, and reopen trigger.

A scope-sensitive change cannot close on happy-path, broad-suite, build-only, or refactor-green evidence alone.

### Artifact conflicts

- **PRD:** No conflict and no modification. The correction reinforces FR44 and NFR8.
- **Architecture:** No conflict and no modification. The correction reinforces the D8 authorization boundary, D29 physical-isolation direction, tenant-scoped resources, and parameterized graph access.
- **UX:** No conflict and no modification. UI changes use fail-closed wrong-scope/restrictive-state evidence rather than backend denial tests when that is the relevant surface.
- **Epics:** The existing range-limited carry-forward paragraph must become a repository-lifetime guard.
- **Project context:** The existing negative-coverage rule must require evidence attachment and a close-out gate, not only test presence.
- **Sprint status:** The existing action remains `in-progress`; its comment must point to the durable refresh.

### Technical and operational impact

- No immediate product code, public contract, schema, persistence, deployment, package, or submodule change.
- Future scope-sensitive work carries a small verification and documentation cost proportional to risk.
- The rule permits a focused accepted blocker, so unavailable infrastructure is visible rather than converted into an unsupported completion claim.

## 3. Recommended Approach

Use **Direct Adjustment** to make the existing safeguard repository-lifetime and auditable.

Rationale:

- The product boundary and canonical negative tests already exist.
- Rollback would weaken a hard security requirement.
- A new epic would overstate a small governance correction.
- PRD/MVP review is unnecessary because scope and acceptance intent do not change.
- Updating both `epics.md` and the root project context closes the expiration gap and makes the rule visible to future planning and implementation workflows.

Estimate and risk:

- Effort: low, less than half a working day for the approved artifact edits.
- Product risk: low; no runtime behavior changes.
- Process risk: low after both guard locations agree.
- Timeline impact: none now; future affected changes must budget focused negative validation.

## 4. Detailed Change Proposals

### Proposal A: Make the epics guard repository-lifetime

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: `Cross-tenant negative-evidence carry-forward (2026-07-06)`

OLD:

```markdown
**Cross-tenant negative-evidence carry-forward (2026-07-06):** Any future scope-sensitive Epic 20-26 story must keep cross-tenant negative validation evidence attached to the change instead of treating it as historical proof. Scope-sensitive includes tenant/case route grouping or versioning, endpoint filters, auth or claim normalization, tenant status guards, MCP tool executors, evidence-packet scope metadata, web evidence rendering, tenant verifier logic, key or index routing, graph/search/case attribution, and any refactor that moves those paths. Story files and Dev Agent Records must cite the prior evidence they preserve, including Story 20.2 denial-before-dependency tests and Story 24.3 verifier fail-closed/tenant-marker evidence when those areas are affected, list the impacted surfaces, and include focused negative tests or an explicit accepted blocker. A scope-sensitive story cannot close on happy-path or refactor-green tests alone.
```

NEW:

```markdown
**Cross-tenant negative-evidence carry-forward (2026-07-06; broadened 2026-07-16):** Any future scope-sensitive story, spec, refactor, fix, review patch, sprint correction, or implementation change—regardless of epic number—must keep cross-tenant negative validation evidence attached to the change instead of treating it as historical proof. Scope-sensitive includes tenant/case route grouping or versioning; endpoint filters or middleware; auth or claim normalization; tenant status guards; MCP tool executors or client calls; evidence-packet scope metadata or restrictive web rendering; tenant verifier logic or tenant markers; key/index/graph routing, actor IDs, storage selectors, or query builders; search/graph/case attribution; export/import or backup/restore; and any refactor that moves those paths. The story/spec and completion or review record must list the impacted surfaces, cite Story 20.2 denial-before-dependency and Story 24.3 verifier fail-closed/tenant-marker evidence when applicable (or link a newer canonical replacement), and record focused negative test names, command, and result. If proof cannot run, record an explicit accepted blocker with owner, consequence, and reopen trigger. A scope-sensitive change cannot close on happy-path, broad-suite, build-only, or refactor-green evidence alone.
```

Rationale: removes the Epic 26 expiration and applies the same gate to fixes and review patches that may bypass new-story creation.

### Proposal B: Strengthen the root project-context rule

Artifact: `_bmad-output/project-context.md`  
Section: `Testing Rules`

OLD:

```markdown
- **Tenant isolation requires negative coverage** - changes to tenant routing, index names, graph database selection, actor IDs, MCP authorization, or UI scope display need cross-tenant denial tests.
```

NEW:

```markdown
- **Tenant isolation requires attached negative evidence** - any change to tenant/case routing, endpoint filters or auth claims, tenant status, index/key/graph selection, actor IDs, storage/query selectors, MCP authorization/execution, evidence scope display, verifier markers, attribution, or tenant-scoped data movement must name the affected surfaces and attach focused cross-tenant denial or fail-closed test names, command, and result to its story/spec plus completion or review record. Cite Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker evidence when applicable, or link the newer canonical replacement. If proof cannot run, record an accepted blocker with owner, consequence, and reopen trigger. Do not close on happy-path, broad-suite, build-only, or refactor-green evidence alone.
```

Rationale: turns a general test expectation into a persistent evidence-attachment and close-out rule loaded by future implementation workflows.

### Proposal C: Keep the retrospective action active and point it to the durable guard

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`  
Action: `Keep cross-tenant negative validation evidence attached to future scope-sensitive changes`

OLD:

```yaml
status: in-progress  # 2026-07-06: Cross-tenant negative-evidence carry-forward guard added to epics.md and captured in sprint-change-proposal-2026-07-06-cross-tenant-negative-evidence.md; remains active for future scope-sensitive stories.
```

NEW:

```yaml
status: in-progress  # 2026-07-16: Guard broadened beyond Epics 20-26 and mirrored in project-context.md by sprint-change-proposal-2026-07-16-cross-tenant-negative-evidence-refresh.md; remains active for every future scope-sensitive change.
```

Rationale: the obligation is intentionally ongoing, so `done` would falsely imply that future attachment is no longer required.

## 5. Implementation Handoff

Classification: **Minor**.

Recipients and responsibilities after approval:

- **Product owner / planning agent:** preserve the universal guard in epics and any future sprint correction that touches scope-sensitive work.
- **Developer agent:** include the four-part evidence attachment in the story/spec and Dev Agent Record or implementation record.
- **Test architect / reviewer:** reject scope-sensitive close-out that lacks focused negative proof or a fully recorded accepted blocker.
- **Architect:** identify a newer canonical evidence anchor when architecture changes supersede Story 20.2 or Story 24.3, without dropping traceability to the replaced proof.

Success criteria:

- The guard no longer expires after Epic 26.
- Future planning and implementation workflows receive the same attachment rule.
- Every applicable change identifies its surfaces, evidence anchor, focused negative result, and blocker disposition.
- Completed stories remain closed; no historical artifact is rewritten to claim evidence it did not record.
- The Epic 0 action remains visibly active.

## 6. Checklist Record

### 1. Understand the trigger and context

- [N/A] 1.1 No failing story triggered the correction; the trigger is Epic 0 retrospective action item 4.
- [x] 1.2 Core problem classified as evidence-governance drift and a guard that expires after Epic 26.
- [x] 1.3 Supporting evidence collected from PRD FR44/NFR8, root project context, Stories 20.2 and 24.3, the approved 2026-07-06 proposal, current epics/status, and later scope-sensitive records.

### 2. Epic impact assessment

- [x] 2.1 Current and completed epics remain valid.
- [x] 2.2 The current range-limited epics guard requires broadening.
- [x] 2.3 Future epics are affected only when a change is scope-sensitive.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No priority or sequencing change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict or modification required.
- [x] 3.2 Architecture reviewed; no conflict or modification required.
- [x] 3.3 UX reviewed; wrong-scope and restrictive-state validation remain the relevant fail-closed UI evidence.
- [x] 3.4 Epics, project context, sprint status, and later story/spec records reviewed.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable with low effort and low risk.
- [N/A] 4.2 Rollback is inappropriate because the existing safety evidence remains required.
- [N/A] 4.3 PRD/MVP review is unnecessary.
- [x] 4.4 Direct Adjustment selected.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact and detailed artifact edits documented.
- [x] 5.5 Minor-scope handoff documented.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked against current repository evidence.
- [x] 6.3 Explicit approval received from Administrator on 2026-07-16.
- [x] 6.4 Approved `epics.md`, `project-context.md`, and `sprint-status.yaml` edits applied.
- [x] 6.5 Minor-scope implementation handoff completed through the durable planning and project-context guards.

## 7. Approval

- [x] Approved by Administrator — 2026-07-16
- [x] `epics.md` guard broadened beyond Epics 20-26
- [x] root `project-context.md` rule strengthened to require attached evidence
- [x] `sprint-status.yaml` comment updated while status remains `in-progress`
