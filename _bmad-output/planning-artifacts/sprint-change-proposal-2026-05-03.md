# Sprint Change Proposal - Deferred Work Hardening Backlog

**Date:** 2026-05-03
**Author:** Jerome (with BMad correct-course workflow)
**Status:** APPROVED 2026-05-03 - Epic 14 added to `epics.md` and `sprint-status.yaml`
**Scope classification:** MODERATE - backlog reorganization and new story creation are needed; completed epics remain valid and should not be reopened.

---

## 1. Issue Summary

The project has completed all currently tracked work through Epic 13, including the Epic 13 retrospective. Sprint tracking now has no `backlog`, `ready-for-dev`, or `in-progress` stories, while `_bmad-output/implementation-artifacts/deferred-work.md` contains 266 deferred review items.

The trigger is not a failed implementation or a strategic pivot. The issue is that deferred work has accumulated across completed stories and now needs a planned carrier so `bmad-create-story` and `bmad-dev-story` can continue operating through normal sprint tracking.

Evidence:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` marks Epics 1-13 and all listed stories as `done`.
- `_bmad-output/implementation-artifacts/deferred-work.md` contains 266 deferred bullet items.
- Recent high-density clusters include Story 12.3, Story 12.4, Stories 13.1-13.7, and Stories 11.1/11.2.
- The latest completed Epic 13 retrospective explicitly records deferred hardening and no Epic 14 plan update.

## 2. Checklist Findings

| Checklist item | Status | Finding |
|---|---|---|
| 1.1 Triggering story | [x] Done | Trigger is cross-story deferred work after Epic 13 closeout, not a single failed story. |
| 1.2 Core problem | [x] Done | Technical debt and review-hardening items are tracked but not actionable through sprint workflow. |
| 1.3 Supporting evidence | [x] Done | Deferred register has 266 entries; sprint status has no backlog stories. |
| 2.1 Current epic impact | [x] Done | Completed epics remain valid; no rollback needed. |
| 2.2 Epic changes | [!] Action-needed | Add a new Epic 14 as the carrier for deferred hardening. |
| 2.3 Future epics | [x] Done | No future epic exists; Epic 14 is needed before story creation can resume. |
| 2.4 New epic need | [x] Done | New epic is required to avoid reopening completed epics. |
| 2.5 Priority/order | [x] Done | Prioritize CI/story-scope and release hardening before lower-risk test cleanup. |
| 3.1 PRD conflicts | [x] Done | No PRD goal conflict. Work supports NFRs for security, reliability, observability, and documentation quality. |
| 3.2 Architecture conflicts | [x] Done | No architecture conflict. Work reinforces existing decisions around DAPR, CI/CD, tenant isolation, and secret discipline. |
| 3.3 UX conflicts | [N/A] Skip | No UI/UX artifact exists; project is API/backend/CLI oriented. |
| 3.4 Other artifacts | [x] Done | `epics.md`, `sprint-status.yaml`, CI workflows, tooling tests, release docs, and deferred register are impacted. |
| 4.1 Direct adjustment | [x] Viable | Add a new epic and planned backlog stories. Effort medium, risk low-medium. |
| 4.2 Rollback | [x] Not viable | Reverting completed stories would add risk and does not reduce the deferred-work problem. |
| 4.3 MVP review | [x] Not viable | MVP goals remain valid; this is post-MVP hardening. |
| 4.4 Recommended path | [x] Done | Direct adjustment through a new Epic 14. |
| 5.1 Issue summary | [x] Done | Included in this proposal. |
| 5.2 Impact summary | [x] Done | Included below. |
| 5.3 Path forward | [x] Done | Add Epic 14 and create first story from it. |
| 5.4 MVP impact | [x] Done | MVP scope unchanged. |
| 5.5 Handoff plan | [x] Done | Product/backlog update first, then dev-story implementation. |
| 6.1 Final checklist | [x] Done | All applicable sections addressed. |
| 6.2 Proposal accuracy | [x] Done | Proposal is specific and references existing deferred IDs. |
| 6.3 User approval | [x] Done | Jerome approved the proposal on 2026-05-03. |
| 6.4 Sprint status update | [x] Done | Epic 14 and five backlog stories added to `sprint-status.yaml`. |
| 6.5 Handoff confirmation | [x] Done | Next handoff is `bmad-create-story` for Story 14.1. |

## 3. Impact Analysis

### 3.1 Epic Impact

| Epic | Status | Impact |
|---|---|---|
| Epics 1-10 | done | Keep closed. Older deferred items remain available but should be pulled only when they align with a new hardening story. |
| Epic 11 | done | Release and CI hardening deferred from 11.1/11.2 should move into Epic 14 rather than reopening Epic 11. |
| Epic 12 | done | Story-scope enforcement and release-lane follow-ups should move into Epic 14. |
| Epic 13 | done | OIDC, embedding-provider, migration, and integration-test hardening should move into Epic 14. |
| Epic 14 | new | Add a post-MVP hardening epic dedicated to deferred work closure. |

### 3.2 Story Impact

No completed story should be reopened. New stories should reference the original deferred IDs and mark closures in `deferred-work.md` as part of their acceptance criteria.

Recommended story groups:

| New story | Main deferred IDs | Purpose |
|---|---|---|
| 14.1 CI Story-Scope Enforcement Hardening | 12.4-RV1..RV5, 12.4-RV7..RV18, related 12.3 parser issues | Make story-scope CI fail loudly, harden branch/story-key parsing, improve diagnostics and tests. |
| 14.2 Release Pipeline Audit Hardening | W1, W2, W11, W12, W15, W17, W23, S11-FC, S11-FD, 12.1-RV1..RV5 | Tighten release workflow integrity, package inventory validation, stale-tag detection, and release-runbook evidence. |
| 14.3 OIDC and Embedding Security Hardening | 13.2-RV1..RV8, 13.3-RV7, 13.3-RV11, 13.3-RV14, 13.3-RV15, 13.4-RV5 | Resolve highest-value token, secret, HttpClient lifecycle, userinfo URL, and error-wrapping gaps. |
| 14.4 Migration and Integration Test Hardening | 13.6-RV1, 13.6-RV3..RV5, 13.7-RV1..RV7 | Reduce migration and Aspire fixture risk, replace brittle test waits, expand malformed-token branch coverage. |
| 14.5 Deferred Register Governance and Long-Line Cleanup | 13.7-RV5, 12.4-RV6, 12.4-RV19, broad deferred-work taxonomy | Make deferred entries machine-parseable enough for future audits and stop sprint-status comment lines from becoming unmaintainable. |

### 3.3 Artifact Conflicts

| Artifact | Current state | Required change |
|---|---|---|
| `_bmad-output/planning-artifacts/epics.md` | Ends with Epic 13 as the latest planned epic. | Append Epic 14 with the five hardening stories above. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | All stories done; no backlog entries. | Add `epic-14: in-progress`, `14-1` through `14-5` as `backlog`, and `epic-14-retrospective: optional`. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Contains 266 unstructured deferred entries. | Each Epic 14 story should close, resolve, or explicitly carry forward its target deferred IDs. |
| PRD | MVP and NFRs already cover the quality goals. | No direct PRD edit required. |
| Architecture | Existing architecture supports the work. | No direct architecture edit required unless a story changes a pattern such as HttpClient lifecycle or release governance. |
| UX | No UX artifact. | Not applicable. |

### 3.4 Technical Impact

The first implementation slice should avoid broad code churn. Start with CI/story-scope hardening because it protects all later deferred-work implementation:

- `.github/workflows/ci.yml`
- `tools/check-story-file-scope.py`
- `tests/tooling/story_scope/story_scope_validator_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

Subsequent slices touch release tooling, OIDC/embedding code, integration fixtures, and documentation.

## 4. Recommended Approach

Selected path: **Direct Adjustment**.

Rationale:

- The project has finished its tracked roadmap through Epic 13. The plan, not the implementation, is now the bottleneck.
- Deferred items are already reviewed, categorized, and traceable. They do not require a product pivot.
- Reopening completed epics would blur completion history and conflict with sprint-status workflow notes.
- A new Epic 14 preserves the existing acceptance trail while giving deferred work a normal story lifecycle.

Effort estimate: medium.

Risk level: low-medium. The highest risk is accidentally grouping unrelated deferred work into overly large stories. The mitigation is to start with Story 14.1 and keep each follow-up story file-scoped.

## 5. Detailed Change Proposals

### 5.1 `epics.md` addition

OLD:

```markdown
### Story 13.7: Integration Tests, Aspire Fixtures & Operator Deployment Guide
...
---
```

NEW:

```markdown
### Epic 14: Deferred Work Hardening and Operational Readiness

Developer and operator can close the highest-value deferred review findings without reopening completed epics, improving CI correctness, release integrity, OIDC/embedding security, migration reliability, and deferred-work governance.

**FRs reinforced:** FR43, FR56, FR57, FR67, FR68, FR69, FR70, FR72, FR73, FR74
**NFRs reinforced:** NFR8, NFR9, NFR10, NFR11, NFR17, NFR18, NFR19, NFR22, NFR27, NFR28, NFR30, NFR31

### Story 14.1: CI Story-Scope Enforcement Hardening

As a maintainer,
I want story-scope validation and CI diff discovery to fail loudly and parse story keys consistently,
So that future feature work cannot bypass file-scope enforcement through shallow fetches, malformed story keys, or ambiguous branch metadata.

Acceptance criteria:

- CI fetch failures are no longer swallowed by `|| true`.
- Push-to-main and empty-diff cases fail with clear diagnostics instead of silently passing story-scope validation.
- Branch and explicit `--story-key` values containing multiple story keys are rejected consistently.
- `git interpret-trailers` absence produces a clean validation error.
- Existing and new story-scope tests pass, including boundary cases for `STORY_KEY_PATTERN`, code fences, allow-list termination, and diagnostics.
- Target deferred IDs are either removed from `deferred-work.md` or marked resolved with evidence.

### Story 14.2: Release Pipeline Audit Hardening

As a release maintainer,
I want release workflow and package validation guardrails strengthened,
So that package publication, stale tags, release evidence, and package inventory drift are caught before they can create ambiguous release states.

Acceptance criteria:

- Release workflow action pinning, stale-tag handling, and partial-publish signal behavior are explicitly decided and implemented or documented with a new defer-by date.
- `tools/validate-release-packages.ps1` validates both packable and non-packable project inventory.
- Release-runbook package evidence includes checksum or equivalent audit evidence for newly validated packages.
- `tools/release-packages.json` schema validation or schema reference is added.
- CI inventory tests use stricter parsing where feasible instead of broad substring matching.

### Story 14.3: OIDC and Embedding Security Hardening

As an operator,
I want OIDC token acquisition and embedding-client error handling hardened,
So that cancellation, credential rotation, malformed URLs, token refresh storms, and transport errors do not leak secrets or produce avoidable outages.

Acceptance criteria:

- OIDC leader cancellation cannot cancel the shared fetch for remaining waiters.
- OIDC and embedding HttpClient lifecycle follows the chosen `IHttpClientFactory`/typed-client pattern without singleton-captured stale handlers.
- Token endpoints and provider URLs reject embedded userinfo.
- Concurrent forced refreshes collapse where practical or are explicitly bounded and tested.
- Network and timeout errors are wrapped in the project-specific typed exceptions expected by callers.
- Ollama 401 retry evicts stale client_secret cache as symmetrically as the Google path.
- Sensitive-value redaction is length-aware and order-stable.

### Story 14.4: Migration and Integration Test Hardening

As an operator and maintainer,
I want migration and Aspire integration tests hardened,
So that provider migration evidence remains stable under CI pressure and malformed fake-server input cannot weaken coverage silently.

Acceptance criteria:

- Migration service avoids ad-hoc string result surfaces where `ValueOrError<T>` is appropriate, or the exception is documented with a focused reason.
- Migration redaction covers the approved credential shapes from the story scope.
- Integration tests avoid Redis `KEYS` polling where a bounded targeted alternative exists.
- Temporary DAPR configuration directories are cleaned up.
- Ollama OIDC fake-server malformed-token branches have dedicated tests.
- Magic constants in provider integration tests are replaced with named constants or clear assertions.

### Story 14.5: Deferred Register Governance and Sprint-Status Hygiene

As a maintainer,
I want deferred-work entries and sprint-status history to stay auditable,
So that future planning can distinguish open risk, resolved risk, accepted risk, and stale historical noise without manual archaeology.

Acceptance criteria:

- Deferred-work entries gain a minimal consistent structure for ID, status, source story, target artifact, and re-open trigger.
- Existing open deferred entries targeted by Epic 14 stories are updated as `resolved`, `accepted`, or `carried-forward`.
- Sprint-status update guidance avoids unbounded one-line history comments.
- Tests or scripts that parse deferred-work entries are updated to the new structure.
- The proposal avoids changing submodule pointers and follows root-declared `references/` submodule discipline.
```

### 5.2 `sprint-status.yaml` addition

OLD:

```yaml
  epic-13-retrospective: done
```

NEW:

```yaml
  epic-13-retrospective: done

  # --- Phase: Post-MVP - Deferred Work Hardening ---

  # Epic 14: Deferred Work Hardening and Operational Readiness
  epic-14: in-progress # Added by approved Sprint Change Proposal 2026-05-03 to turn deferred review findings into tracked backlog work.
  14-1-ci-story-scope-enforcement-hardening: backlog
  14-2-release-pipeline-audit-hardening: backlog
  14-3-oidc-and-embedding-security-hardening: backlog
  14-4-migration-and-integration-test-hardening: backlog
  14-5-deferred-register-governance-and-sprint-status-hygiene: backlog
  epic-14-retrospective: optional
```

### 5.3 PRD edits

No PRD edit is proposed. The deferred work reinforces already-stated NFRs and operational expectations.

### 5.4 Architecture edits

No architecture edit is proposed at proposal time. Individual Epic 14 stories may amend architecture if implementation changes a durable pattern, especially HttpClient lifecycle or release governance.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff:

| Role | Responsibility |
|---|---|
| Product/backlog owner | Approve this proposal, append Epic 14 to `epics.md`, and update `sprint-status.yaml`. |
| Story context engine | Run `bmad-create-story` to create Story 14.1 first. |
| Developer agent | Run `bmad-dev-story` on Story 14.1, then continue story-by-story. |
| Reviewer | Run code review after each story, using a different LLM if possible. |

Recommended sequence:

1. Approve this proposal.
2. Apply the `epics.md` and `sprint-status.yaml` changes.
3. Create Story 14.1.
4. Implement Story 14.1 before other Epic 14 stories, because it improves enforcement for all later work.
5. Reassess remaining deferred count after each Epic 14 story.

Success criteria:

- `bmad-create-story` discovers `14-1-ci-story-scope-enforcement-hardening` as the next backlog story.
- Story 14.1 reaches `done` with tests proving the CI/story-scope hardening.
- Deferred entries targeted by each story are resolved, accepted, or carried forward explicitly.
- No completed Epic 1-13 story is reopened.

## 7. Approval

- [x] Jerome approves adding Epic 14 and the five backlog stories. Approved 2026-05-03.
- [x] Jerome approves keeping Epics 1-13 closed. Approved 2026-05-03.
- [x] Jerome approves Story 14.1 as the first implementation target. Approved 2026-05-03.

Applied updates:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Next handoff:

- Run `bmad-create-story` to create `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md`.
