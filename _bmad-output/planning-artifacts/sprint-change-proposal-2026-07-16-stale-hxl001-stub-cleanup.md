# Sprint Change Proposal — Stale HXL001 Stub Suppression Cleanup

**Date:** 2026-07-16  
**Project:** memories  
**Mode:** Batch  
**Approval:** The user's explicit action to remove the suppression is treated as approval to execute this minor cleanup.

## 1. Issue Summary

Epic 18 retrospective action 8 carried forward Story 18.7 review finding L2: remove the `HXL001` warning suppression around `StubMemoriesClient.IngestAsync` after Story 18.4 graduated `MemoriesClient.IngestAsync` from the experimental surface.

Preflight found that the code cleanup was already completed by commit `528fb235` on 2026-07-01. That commit removed both the file-level `#pragma warning disable HXL001` and matching restore directive. The remaining issue was tracking drift: the retrospective action still read `status: open`.

## 2. Impact Analysis

- **Epic impact:** None. Epic 18 and Stories 18.4/18.7 remain `done`.
- **Story impact:** None. This closes a post-story review cleanup action without changing acceptance criteria or completed history.
- **PRD impact:** None. Product scope, MVP gates, and requirements are unchanged.
- **Architecture impact:** None. The supported non-sealed/virtual `MemoriesClient` mockability seam remains unchanged.
- **UX impact:** None. No user-facing behavior or interface changes.
- **Technical impact:** No current source edit is required. The MCP test stub already compiles without the obsolete suppression.
- **Release impact:** None. This is test/tracking maintenance and does not alter a published contract.

## 3. Recommended Approach

Use a **Direct Adjustment** and close the stale retrospective ledger entry based on repository and build evidence.

- **Effort:** Low
- **Risk:** Low
- **Timeline impact:** None
- **Rollback:** Not warranted; it would restore an obsolete suppression.
- **MVP review:** Not applicable; Epic 18 is an operational-readiness track excluded from MVP accounting.

## 4. Detailed Change Proposals

### Test stub cleanup

Historical state before commit `528fb235`:

```csharp
#pragma warning disable HXL001 // MemoriesClient.IngestAsync is HXL001-experimental.
// StubMemoriesClient implementation
#pragma warning restore HXL001
```

Current state:

```csharp
// StubMemoriesClient implementation; no HXL001 suppression around IngestAsync.
```

Rationale: Story 18.4 removed `[Experimental("HXL001")]` from both stable `IngestAsync` overloads, so the suppression no longer expresses a real compiler requirement.

### Sprint tracking

OLD:

```yaml
status: open
```

NEW:

```yaml
status: done  # 2026-07-16: already removed by 528fb235; focused Release build passes with 0 warnings and 0 errors.
```

Rationale: Align the Epic 18 retrospective ledger with the repository's implemented and verified state.

## 5. Implementation Handoff

**Scope classification:** Minor  
**Recipient:** Developer agent  
**Result:** Complete

Success criteria:

- `StubMemoriesClient.cs` contains no `HXL001` suppression.
- The focused Release build succeeds with warnings treated as errors.
- The MCP test assembly passes.
- The Epic 18 retrospective action is marked `done` with evidence.

Validation evidence:

- `dotnet build tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --configuration Release` — succeeded, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Mcp.Tests/bin/Release/net10.0/Hexalith.Memories.Mcp.Tests.dll` — 105 passed, 0 failed.

## Change Analysis Checklist

- [x] 1.1–1.3 Trigger, problem, and evidence confirmed from Stories 18.4/18.7, the Epic 18 retrospective, source history, and live code.
- [x] 2.1–2.5 No epic scope, ordering, priority, or dependency changes required.
- [x] 3.1 PRD has no conflict or required edit.
- [x] 3.2 Architecture has no conflict or required edit.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Test/tracking impact is limited to focused verification and closing the retrospective action.
- [x] 4.1 Direct Adjustment is viable with low effort and low risk.
- [N/A] 4.2 Rollback is not justified.
- [N/A] 4.3 MVP review is not required.
- [x] 4.4 Direct Adjustment selected.
- [x] 5.1–5.5 Proposal, impacts, rationale, action plan, and Developer handoff documented.
- [x] 6.1–6.2 Proposal reviewed against live repository evidence.
- [x] 6.3 Explicit action approval supplied by the user.
- [N/A] 6.4 No epic or story status changes are required; only the retrospective action status is reconciled.
- [x] 6.5 Handoff and success criteria are complete.
