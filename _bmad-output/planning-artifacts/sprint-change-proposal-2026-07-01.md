# Sprint Change Proposal: Centralize Sandbox Test Runner Workaround

Date: 2026-07-01
Project: Hexalith.Memories

## 1. Issue Summary

Epic 17 and Epic 18 story artifacts repeatedly documented the same local sandbox test workaround:
`dotnet test` can fail in this environment because VSTest opens a local TCP listener and receives
`SocketException (13): Permission denied`. The working validation path is to build the test project,
then run the xUnit v3 assembly directly with `DiffEngine_Disabled=true dotnet exec ...`.

The workaround had been copied into individual story notes and test summaries, but not centralized in
contributor guidance. Sprint status recorded this as Epic 17 action item 4 and carried it forward as an
Epic 18 action item.

## 2. Impact Analysis

Epic impact: Epic 17 and Epic 18 are affected only through retrospective/process follow-up. Product scope,
MVP scope, architecture, UX behavior, and runtime code are unchanged.

Story impact: No new implementation story is required. The open/in-progress retrospective action items can
be closed by adding canonical guidance to `CONTRIBUTING.md` and updating `sprint-status.yaml`.

Artifact impact: `CONTRIBUTING.md` needs the canonical workaround under Tests. `sprint-status.yaml` needs
dated breadcrumbs marking the Epic 17 and Epic 18 action items done. PRD, Architecture, and UX Design need
no changes.

Technical impact: Documentation-only. No source, test, package, CI, or submodule behavior changes.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The change is a narrow contributor-process correction with existing evidence in Epic 17, Epic 18,
and Epic 19 artifacts. Centralizing the workaround reduces repeated story-local instructions while preserving
the normal `tools/test.*` commands as the supported local/CI path.

Effort: Low.
Risk: Low.
Timeline impact: None.

## 4. Detailed Change Proposals

### `CONTRIBUTING.md`

Section: `Tests`.

OLD:

The Tests section documented the standard Docker-free and integration lanes, but did not explain what to do
when sandboxed VSTest execution fails before test discovery with `SocketException (13)`.

NEW:

Add `### Sandbox test runner workaround` documenting:

- Keep `tools/test.*` as the normal local and CI parity path.
- Use the workaround only when `dotnet test` is blocked by the VSTest TCP-listener permission failure.
- Build the target test project first.
- Run the xUnit v3 assembly directly with `DiffEngine_Disabled=true dotnet exec ...`.
- Prefer narrow xUnit v3 filters such as `-class`, `-method`, or `-namespace`.
- Record the command, test count, and limitation in story/review artifacts.
- Do not treat this workaround as evidence for Docker, DAPR sidecar, Aspire, browser, or other infrastructure lanes.

Rationale: Makes the workaround discoverable in the canonical contributor guide without weakening CI parity.

### `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `action_items`.

OLD:

```yaml
status: in-progress  # Epic 18 follow-through: workaround re-applied and noted in every Epic 18 story, but still not centralized in a contributing doc
...
status: open
```

NEW:

```yaml
status: done  # 2026-07-01: centralized in CONTRIBUTING.md "Sandbox test runner workaround"; closes the Epic 18 carry-forward item.
...
status: done  # 2026-07-01: canonical guidance added to CONTRIBUTING.md "Sandbox test runner workaround".
```

Rationale: Closes the original Epic 17 retrospective action and the Epic 18 carry-forward item with a durable
artifact pointer.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation.

Success criteria:

- `CONTRIBUTING.md` contains canonical sandbox workaround guidance under Tests.
- Epic 17 action item 4 and the Epic 18 carry-forward action item are marked done with dated breadcrumbs.
- `git diff --check` passes.

## Checklist Status

- [x] 1.1 Triggering story/item identified: Epic 17 action item 4, carried forward by Epic 18.
- [x] 1.2 Core problem defined: repeated story-local workaround guidance without a canonical contributor doc.
- [x] 1.3 Evidence gathered: sprint status action items and Epic 17/18/19 story artifacts reference the same workaround.
- [x] 2.1 Current epic impact assessed: retrospective/process only.
- [x] 2.2 Required epic-level changes assessed: no epic scope changes.
- [x] 2.3 Remaining epics reviewed for impact: no product or sequencing impact.
- [x] 2.4 New/obsolete epic assessment complete: none needed.
- [x] 2.5 Priority/order assessment complete: no resequencing.
- [x] 3.1 PRD conflict check complete: no PRD change.
- [x] 3.2 Architecture conflict check complete: no architecture change.
- [x] 3.3 UX conflict check complete: no UX change.
- [x] 3.4 Other artifacts checked: `CONTRIBUTING.md` and `sprint-status.yaml` updated.
- [x] 4.1 Direct Adjustment viable: selected.
- [N/A] 4.2 Rollback not viable: no implementation rollback.
- [N/A] 4.3 MVP review not required: MVP scope unchanged.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1 Issue summary created.
- [x] 5.2 Impact and artifact needs documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist completion reviewed.
- [x] 6.2 Proposal accuracy reviewed.
- [x] 6.3 User approval treated as direct instruction in batch mode.
- [x] 6.4 Sprint status updated.
- [x] 6.5 Next steps and handoff plan documented.
