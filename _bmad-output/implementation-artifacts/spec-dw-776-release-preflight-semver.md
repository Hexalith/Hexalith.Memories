---
title: 'Validate complete SemVer in release preflight'
type: 'bugfix'
created: '2026-11-28'
status: 'done'
route: 'dispatch'
baseline_commit: '453c7b0a5e41468b3e04820b2aa5fbcbd97a73df'
review_loop_iteration: 0
context: ['{project-root}/AGENTS.md', '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-776 addresses “Validate release versions consistently against the complete SemVer grammar in direct input and both semantic-release terminal marker forms.” Current preflight accepts versions with leading zeros and refuses valid prerelease plus build metadata.

## Boundaries & Constraints

Follow SemVer 2.0.0. Use one version validator across the direct parameter and both terminal records. Preserve fail-closed ambiguous/malformed marker behavior and remote tag collision checks. Run only local isolated preflight; no publication, credential use, or Git mutation outside scratch fixtures. Do not edit the ledger/spec.

</frozen-after-approval>

## Code Map

tools/release-preflight.ps1 defines SemverPattern, Get-NextReleaseVersionFromDryRun and the direct NextVersion check. tests/tooling/release_preflight/release_preflight_test.py has disposable direct and dry-run fixtures.

## Tasks & Acceptance

- [x] Implement the scoped behavior and focused regression evidence.

Given supported stable or prerelease+metadata input, all three paths accept it. Given leading zeros, empty identifiers, or extra characters, all three paths refuse it. Given malformed terminal records mixed with valid outcomes, the preflight still fails.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Run release-preflight tool fixtures and explicitly verify the original acceptance and rejection inversions are corrected on all entry paths.

The original parser failed 22 expected cases in the regression matrix. The shared validator passed all 45 direct and terminal-record cases and the entire 54-test preflight suite. The optional destination gate still enforces the separate OCI image-tag grammar when live destination checks are requested.

## File List

- `tests/tooling/release_preflight/release_preflight_test.py`
- `tools/release-preflight.ps1`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-11-28 | create-story | Scoped the verified deferred item | Focused checks planned | Recorded affected surfaces |
| 2026-11-28 | dev-story | `067b8fac4f9d82f84f4b072e8d2b6f4f55c8c576` | 54 preflight tests | Matched 2/2 |
| 2026-11-28 | code-review | `067b8fac4f9d82f84f4b072e8d2b6f4f55c8c576` | 54 preflight tests | Matched 2/2 |
