# Sprint Change Proposal - .NET SDK 10.0.302 Pin

**Project:** Hexalith.Memories
**Date:** 2026-05-18
**Requested by:** JeromePiquot
**Workflow:** bmad-correct-course
**Change trigger:** User directive to use .NET SDK `10.0.302`.
**Mode:** Batch
**Status:** Implemented as a minor direct adjustment

## 1. Issue Summary

The repository targets `net10.0`, but the executable SDK pin and onboarding guidance were not aligned with the requested SDK feature band:

- `global.json` pinned SDK `10.0.201`.
- `_bmad-output/project-context.md` told agents to expect `10.0.201`.
- README and quickstart docs still referenced `.NET 9`.
- The quickstart prerequisite check accepted any `.NET 9+` SDK, which could pass environments that cannot honor a `10.0.302` repo pin.

The triggering issue is a tooling/runtime alignment correction, not a product scope change.

## 2. Impact Analysis

### Checklist Findings

| Item | Status | Finding |
|---|---|---|
| 1.1 Triggering story | [N/A] | No single story revealed the issue; the trigger was an explicit user directive. |
| 1.2 Core problem | [x] | Technical/tooling alignment drift: repo SDK pin and docs did not reflect `10.0.302`. |
| 1.3 Supporting evidence | [x] | `global.json`, README, quickstart docs, CLI prerequisite code, and project context contained older SDK expectations. |
| 2.1 Current epic impact | [x] | Epic 15 remains viable; Story 15.6 can proceed with the newer SDK. |
| 2.2 Epic-level changes | [N/A] | No epic scope or acceptance criteria need to change. |
| 2.3 Remaining epics | [x] | No backlog reordering required. SDK pin affects execution environment only. |
| 2.4 New/obsolete epics | [N/A] | No new epic is required. |
| 2.5 Priority/order | [N/A] | No priority changes required. |
| 3.1 PRD conflicts | [x] | No PRD goal or MVP scope conflict. The PRD already assumes .NET/Aspire developer tooling. |
| 3.2 Architecture conflicts | [x] | Architecture remains .NET 10 aligned. The older "C# 13" note is historical architecture text; active repo config uses C# 14. |
| 3.3 UX conflicts | [x] | UX onboarding promise is reinforced because prerequisite checks now match the repo SDK pin. |
| 3.4 Other artifacts | [x] | `global.json`, README, quickstart docs, CLI prerequisite code/tests, CLI error suggestion, and project context needed updates. |
| 4.1 Direct adjustment | [x] | Viable. Effort low, risk low. |
| 4.2 Rollback | [N/A] | Rollback would not simplify the issue. |
| 4.3 MVP review | [N/A] | MVP scope unaffected. |
| 4.4 Recommended path | [x] | Direct adjustment. |
| 5.1-5.5 Proposal components | [x] | Captured in this document. |
| 6.1-6.2 Review | [x] | Focused tests and full solution build passed. |
| 6.3 User approval | [x] | Treated as approved by direct instruction: "use .NET 10.0.302". |
| 6.4 sprint-status.yaml | [N/A] | No epics or stories were added, removed, renumbered, or status-changed. |
| 6.5 Handoff | [x] | Developer handoff complete; future agents should use SDK `10.0.302` or newer. |

### Epic Impact

Epic 15 remains in progress with Story 15.6 ready for development. This change improves execution consistency for Story 15.6 and future work without changing story scope.

### Artifact Conflicts

The active conflict was between repository runtime configuration and onboarding/runtime guidance. The project documents remain valid after updating the SDK feature band.

### Technical Impact

The required SDK is now explicit:

- `global.json` pins `10.0.302` with `rollForward=latestFeature`.
- Quickstart prerequisite logic requires SDK `10.0.302` or newer.
- Documentation and agent context now point to .NET 10 instead of .NET 9.

No package version, TFM, API contract, tenant-isolation behavior, DAPR topology, or release scope changed.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The project already targets `net10.0`; the requested change is an SDK feature-band correction.
- The affected surface is small and testable.
- No completed work needs to be reverted.
- No PRD, epic, architecture, or UX replan is required.

Effort estimate: Low.
Risk level: Low.
Timeline impact: None expected.

## 4. Detailed Change Proposals

### Runtime Configuration

File: `global.json`

OLD:

```json
"version": "10.0.201"
```

NEW:

```json
"version": "10.0.302"
```

Rationale: Make the repository SDK pin match the requested feature band.

### CLI Quickstart Prerequisite Check

File: `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs`

OLD:

```text
Accept any SDK with major version >= 9.
```

NEW:

```text
Accept SDK version 10.0.302 or newer.
```

Rationale: Prevent the quickstart from passing an environment that cannot satisfy the repository SDK pin.

### CLI Error Suggestion

File: `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs`

OLD:

```text
Install a .NET 9 SDK (or newer) and retry.
```

NEW:

```text
Install .NET SDK 10.0.302 or newer and retry.
```

Rationale: Keep actionable CLI guidance aligned with the actual runtime requirement.

### Onboarding Documentation

Files: `README.md`, `docs/dev/quickstart.md`

OLD:

```text
.NET 9 SDK or newer
```

NEW:

```text
.NET SDK 10.0.302 or newer
```

Rationale: Keep contributor onboarding and quickstart instructions accurate.

### Agent Project Context

File: `_bmad-output/project-context.md`

OLD:

```text
SDK pinned by global.json to 10.0.201
```

NEW:

```text
SDK pinned by global.json to 10.0.302
```

Rationale: Ensure future agents use the correct local build expectation.

## 5. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent for direct implementation.

Implementation tasks:

1. Pin `global.json` to `10.0.302`.
2. Tighten quickstart SDK prerequisite checks and test coverage.
3. Update README, quickstart docs, CLI error guidance, and project context.
4. Verify with focused CLI tests and full solution build.

Success criteria:

- `dotnet --version` resolves to `10.0.302` at the repository root.
- Focused quickstart prerequisite tests pass.
- `dotnet build Hexalith.Memories.slnx --configuration Release` succeeds with zero warnings and zero errors.

## 6. Validation Evidence

Completed locally on 2026-05-18:

- `dotnet --version` -> `10.0.302`
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~QuickstartPrerequisiteTests"` -> 13 passed, 0 failed, 0 skipped
- `dotnet build Hexalith.Memories.slnx --configuration Release` -> build succeeded, 0 warnings, 0 errors

## 7. Final Routing

Correct course workflow outcome: complete.

Next step: future implementation and validation work should run from the repository root with SDK `10.0.302` or a later compatible .NET 10 feature band.
