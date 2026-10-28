---
title: 'Close five deferred release CI inventory test gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-60, DW-72, DW-146, DW-148 and DW-330 as one bounded correction to release CI inventory tests. Given harmless release command formatting, optional npm ci --ignore-scripts, renamed display steps, or an explicit pwsh invocation of the same repository script, when the release tests inspect the workflow, then recognize the required operation while preserving its ordering, shell and blocking constraints. Reject alternate operations and additional shell commands rather than using substring matches.

Given consistently space-indented workflow mappings, when inspecting jobs.release scalars, then read only immediate properties of that job regardless of indentation width; reject missing/duplicate jobs and ambiguous or unsupported syntax instead of silently reporting an absent condition. Given the real deferred register plus an in-memory synthetic release baseline, when parsing, then return exactly that baseline while the unchanged real register still has no baseline waivers. Given two independent baseline filters and filter-looking comment lines, when parsing, then preserve exact key/test pairings and enforce each filter's own nearby key. Given absent or blank RepoRoot metadata, when resolving the repository root, then report the exact project metadata configuration needed.

Resolve exactly these five deferred entries with executed evidence and reopen triggers. Independently review, commit, and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Route: small test-only footprint, no unresolved intent gaps, new public API, application configuration, dependency updates or irreversible implementation action. Commit/push on main is expressly authorized. These inventory tests inspect files without application startup; no Aspire lifecycle operation applies.
- Baseline: clean main at b997d5e6ae45a2fb6e2dac3c7c7017c05bad9410. Read root agent instructions, declared AI.Tools baseline/Git rules, project context, contributor test/scope guidance, EditorConfig/attributes and build/test configuration. Current repository configuration overrides stale contextual versions. Independent step-02 investigation confirmed all five gaps still exist and no YAML parser dependency is available.
- Code map: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs owns release workflow command assertions, GetReleaseWorkflowJobScalar, ReadOpenDeferredBaselines, ReadAcceptedReleaseFilters and GetRepoRoot. Reuse its existing structured baseline fixture/schema and workflow step record. Introduce only private helpers in this type; no extra types or dependencies. Preserve unrelated CI inventory assertions and production workflow bytes.
- Verification: build the individual CLI test project in Debug, then execute the built assembly with -class '*CiTestInventoryTests'. Add focused positive/negative helper cases. Run the complete CLI assembly if the focused suite passes. Check repository line-ending and scope gates, and validate the Conventional Commit with commitlint before and after commit. Any broad build blocker must be recorded separately from actual focused evidence.

- Initial focused baseline passed 67/67 cases. The first expanded build passed with zero warnings/errors and the focused suite passed 99/99 cases. The complete CLI assembly also executed successfully; final results will be recorded after review. Current build defaults use repository-configured package references; no project-reference, package, or other build settings were changed.

- Independent blind review reported six concrete issues. Corrected unsupported shell acceptance for explicit pwsh commands; operation lookup now accepts only default/bash/sh/pwsh execution shells and rejects Python/custom no-op shells. Located the OIDC exchange by its NuGet/login action and asserted the user input and blocking behavior. Extended baseline parsing to ignore trailing PowerShell comments while preserving quoted/backtick-escaped hashes. Quoted and spaced mapping keys now resolve correctly; merge/complex mappings fail explicitly; tab characters in script-body text no longer trigger property-indentation errors.
- Final focused command: `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class '*CiTestInventoryTests'` — 115 passed, zero errors/failures/skips. Final whole-project command: `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll` — 562 passed, zero errors/failures/skips (0.746 seconds). The final Debug build with --no-restore -m:1 passed with zero warnings/errors. No production application or service-backed topology changed.
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -q` — 4 passed. Explicit file-scope gate passed for the three allowed paths; tenant-isolation and readiness gates report no affected surface and no governed ledger. Whitespace checks and pre-commit-message commitlint validation passed. Fetched origin/main had zero divergence. Exactly DW-60, DW-72, DW-146, DW-148 and DW-330 changed in the existing deferred register. Final independent fix confirmation is pending.

- Final independent fix verification found an additional escaped-key variant of the quoted-key issue. Reject escaped double-quoted YAML keys explicitly instead of silently treating their decoded property as absent; added Unicode and hexadecimal escape fixtures. Rebuilt Debug with zero warnings/errors and reran 117 focused inventory cases and the complete 564-case CLI assembly; all passed with zero failures/errors/skips (full assembly 0.744 seconds). The reviewer then verified the escaped-key rejection and all six original fixes and confirmed no unresolved findings. No review items were deferred. The earlier 115/562 counts predate these two final negative cases.

## Review Triage Log

- Medium, patched: explicit pwsh command matching accepted a custom shell that could skip execution successfully. Restrict shells and retain Python/custom no-op negative cases; no broad substring matching is used.
- Low, patched: renamed OIDC exchange still failed the operation lookup. Select NuGet/login@v1 and assert its user and blocking properties; a renamed action fixture covers lookup.
- Low, patched: trailing filter-looking comments counted as extra directives. Strip only unquoted PowerShell comment text; assert trailing decoys, quoted # and backtick/single-quote escape controls.
- Medium, patched: quoted/spaced `if` keys appeared absent. Parse explicit mapping keys and test quoted jobs/release/if plus whitespace before colons.
- Medium, patched: YAML merges could hide an inherited job condition. Reject merge and complex-key syntax explicitly; test unquoted and quoted merge markers.
- Low, patched: a global tab scan rejected valid tab characters in run-block text. Validate property indentation while ignoring nested script text; the tab-containing script fixture passes.

- Medium, patched follow-up to quoted-key finding: escaped double-quoted keys could decode to `if` while appearing absent. Explicitly reject their escape syntax; independent reflection verifies both Unicode and hexadecimal variants fail closed.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-ci-inventory.md`
