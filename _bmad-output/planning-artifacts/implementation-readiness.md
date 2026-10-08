---
project: memories
date: '2026-10-08'
intent: sprint-planning
gate: FAIL
baseline: aac6d905
tracking_updated: false
---

# Implementation readiness — 2026-10-08

The full plan is not ready for sprint tracking generation. The approved handoff
stops before final validation, with missing target-surface stories, held
qualification work, and an unfinished dependency review. Approval of the 64
successor story definitions does not resolve those gaps.

This is a planning-readiness verdict. It does not reopen completed history or
infer release qualification from historical story status. No tracker rows or
statuses were changed, and the tracking generator was not run.

## Current artifact inventory

| Artifact | Readiness relevance |
| :--- | :--- |
| [PRD](prd.md) and [addendum](addendum.md) | Current FR1–FR75, NFR1–NFR37, G1–G6, phase boundaries, and release decisions. |
| [Architecture spine](architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) | `status: final`; binds the current requirements and gates. Its implementation gaps remain obligations. |
| [DESIGN](ux-designs/ux-memories-2026-09-12/DESIGN.md) and [EXPERIENCE](ux-designs/ux-memories-2026-09-12/EXPERIENCE.md) | Current UX contracts include the approved shared McpCli presentation boundary. |
| [Epics](epics.md) | Historical Epics 0–31; approved successor outcomes 32–35; 8 Story 33 definitions, 40 Story 34 definitions, and 16 Story 35 definitions. Epic 32 has no operation story. |
| [Approved implementation-readiness correction](sprint-change-proposal-2026-10-05-implementation-readiness.md) | Sections 10–11 record the remaining ownership and final-validation holds. |
| [Sprint tracking](../implementation-artifacts/sprint-status.yaml) | Existing progress and accounting are preserved. No Epic/Story 32–35 tracking rows exist. |

The 64 successor definitions all contain acceptance criteria, Historical
Context Classification, Slice Proof, and Epic AC Verification sections.
Section presence is a structural check, not independent proof of every claim
or of slice independence. A missing architecture document is not a finding:
the authoritative spine is final; older draft observations in the proposal
are explicitly historical.

## Findings, ordered by severity

| ID | Severity | Gap and consequence | Owner and resolution route |
| :--- | :--- | :--- | :--- |
| R1 | High | **Shared-surface coverage is held.** Epic 32 has no registered operation story. The McpCli owner's Stories 4.4 and 4.5 remain `backlog`; Gateway eligibility, per-user identity, migration dispositions, and the approved coverage gate are unresolved. Story 35.7 consumes that enrollment. A developer cannot invent the operation inventory or substitute the legacy Memories grammar. Sources: `epics.md:858–868`, proposal sections 10–11, and the owner tracker. | McpCli owner and Administrator. Complete the owner-repository inventory/coverage handoff; use `bmad-correct-course` for cross-repository decisions, then `bmad-create-epics-and-stories` for verified operation slices. Reopen when the inventory, identity dispositions, and coverage gate are approved. |
| R2 | High | **The successor dependency review is incomplete.** The approved Epic 34 prefix is 34.33 → 34.1 → 34.8. Story 34.9's clarified criteria and placement await review, and the remaining execution sequence is unapproved. Story 34.2 must consume collision-safe composition and authenticated routing; numeric adjacency supplies neither. Source: `epics.md:6066–6094`. | Administrator. Resume `bmad-create-epics-and-stories` at Story 34.9, verify current contracts and prerequisites, and record the reviewed order. Use `bmad-correct-course` if a dependency correction changes approved scope. Preserve the approved prefix and stable IDs. |
| R3 | High | **Qualifying gate work lacks its required handoff.** Two independent external human G1 reviewers remain unnamed. Label-freeze and qualifying-run slices, target CLI NFR37 qualification, and the final G1–G6 verdict remain held or unregistered. The proposal explicitly says final validation cannot certify current-surface coverage or development readiness. Sources: `prd.md:1223` and proposal section 11. | Administrator, with the McpCli owner for target-surface qualification. Record reviewers and independence checks; use `bmad-correct-course` for the cross-cutting qualification handoff and `bmad-create-epics-and-stories` for its compliant slices. Diagnostic preparation may proceed only within its existing boundary and earns no G1 credit. |

These are explicit planning and ownership gaps. Missing future implementation
or release-test results alone were not treated as a planning failure. The
recorded release posture remains no-go; the 2026-10-31 prerequisite checkpoint
and 2026-12-01 decision date are unchanged.

## Evidence checked on the current worktree

Run these commands from the Memories repository root:

```bash
rg -n -e '4-4-approve-chatbot-and-memories-migration-inventories' -e '4-5-gate-module-coverage-against-the-approved-inventory' references/Hexalith.McpCli/_bmad-output/implementation-artifacts/sprint-status.yaml
```

Exit 0: the two owner rows at lines 73–74 both remain `backlog`.

```bash
sed -n '68,90p' references/Hexalith.McpCli/src/Hexalith.McpCli/Cli/CliRunner.cs
```

Exit 0: the root adds generic `modules`, `operations`, `describe`, `send`,
`query`, `config`, and `mcp` commands. This confirms the planning boundary;
it does not prove migration parity.

```bash
rg -n '^### Story 32\.' _bmad-output/planning-artifacts/epics.md
rg -n '^  (epic-(32|33|34|35)(:|-)|(32|33|34|35)-)' _bmad-output/implementation-artifacts/sprint-status.yaml
```

Each exits 1 with no matches: no Epic 32 operation story and no successor
tracking rows. These are expected absence checks, not tool failures.

```bash
rg -n 'EventStoreDedupKey.Build\(route.TenantId, route.CaseId, envelope.Id\)' src/Hexalith.Memories.EventStore/EventIngestionService.cs
rg -n 'MatchTenant\(envelope.Source' src/Hexalith.Memories.EventStore/TenantEventRouter.cs
```

Each exits 0: the current dedup call at line 144 passes the event ID without
source, and routing at line 67 uses publisher-provided source. These source
observations support the recorded Story 34.2 dependency hold; they are not
runtime qualification evidence.

## Next action

Finish the story/dependency revalidation beginning at Story 34.9 and resolve
the owner-inventory and qualification handoffs. Then complete epic final
validation and rerun `bmad-sprint-planning`. Until that gate passes, preserve
the current tracker and the separate Story 27.22/C1 workflow; its other 23
held gate stories are not imported into the successor epics.
