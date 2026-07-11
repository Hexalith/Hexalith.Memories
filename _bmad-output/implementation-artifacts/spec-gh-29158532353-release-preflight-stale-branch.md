---
title: 'Handle stale release checkouts in release preflight'
type: 'bugfix'
created: '2026-07-11'
status: 'done'
review_loop_iteration: 4
baseline_commit: 'c5a999e19531e6b73eba3f059a55c099c200ef17'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions release run `29158532353` failed after all build, test, and package-validation gates passed because a later push advanced remote `main` while the run was active. Semantic-release correctly returned a successful, non-publishing stale-branch result, but `tools/release-preflight.ps1` did not recognize that terminal output and converted it into a parser failure.

**Approach:** Treat semantic-release's exact branch-behind result as a safe no-publish outcome, while retaining fail-closed behavior for unknown successful output. Add focused parser regression coverage and run the release-preflight fixture suite in the release workflow before invoking the real preflight.

## Boundaries & Constraints

**Always:** Preserve exact next-version extraction, stale-tag collision checks, ordinary no-release handling, and failure on unrecognized or ambiguous dry-run output. A stale checkout must skip tag checks because semantic-release will not publish from it. Keep the fix compatible with repository-pinned semantic-release `25.0.5` and PowerShell on GitHub's Linux runner.

**Ask First:** Any change to workflow concurrency, cancellation policy, semantic-release configuration, package publishing, tag deletion, or commit history requires separate approval.

**Never:** Do not weaken failed semantic-release command handling, accept arbitrary output as no-release, rewrite invalid historical commit subjects, rerun or publish from the superseded commit, modify application code, or disturb the pre-existing dirty submodules and story 26-1 artifacts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Stale checkout | Dry-run output says the local `main` branch is behind remote | Preflight exits successfully, reports that no release/tag check is required, and performs no tag collision check | N/A |
| Ordinary no release | Dry-run output says there are no relevant changes | Existing successful no-release behavior remains unchanged | N/A |
| Releasable commit | Dry-run output contains one next semantic version | Existing exact local and remote tag collision checks run for that version | Existing collision failures remain unchanged |
| Unknown terminal output | Dry-run exits zero without a recognized version or non-publish message | Preflight rejects the output | Preserve the actionable parser error and non-zero exit |
| Conflicting versions | Dry-run output contains multiple distinct versions | No version is selected | Preserve the explicit ambiguity failure |

</frozen-after-approval>

## Code Map

- `tools/release-preflight.ps1` -- captures semantic-release dry-run output, classifies terminal outcomes, and gates exact stale-tag checks.
- `tests/tooling/release_preflight/release_preflight_test.py` -- isolated PowerShell preflight fixtures using temporary Git repositories and supplied dry-run output.
- `.github/workflows/release.yml` -- release job ordering for repository-owned validation, preflight, and publish-capable semantic-release work.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- executable contract for required release workflow steps and their ordering/commands.

## Tasks & Acceptance

**Execution:**
- [x] `tools/release-preflight.ps1` -- normalize ANSI once, parse terminal outcomes line-by-line only from exact bare fixture lines or recognized semantic-release logger records, ignore release-note/plugin payload text, and preserve exact `main` stale handling, malformed recognized version rejection, mixed/ambiguous outcome failures, diagnostics, and stale-checkout skip.
- [x] `tests/tooling/release_preflight/release_preflight_test.py` -- preserve all prior coverage and prove release-note/plugin payload lines containing version/stale phrases do not become terminal markers when one genuine semantic-release outcome exists; embedded/quoted text without a genuine outcome must still fail as unknown.
- [x] `.github/workflows/release.yml` -- execute the release-preflight Python fixture suite before the live `Run release preflight` step.
- [x] `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- assert exactly one fixture step uses the canonical command, is ordered before live preflight and semantic-release, has no condition, and cannot suppress fixture failures with `continue-on-error`.

**Acceptance Criteria:**
- Given a release checkout becomes stale because a later push advances remote `main`, when semantic-release dry-run returns its branch-behind message, then preflight succeeds without checking or publishing a tag.
- Given semantic-release returns an ordinary no-release result or exactly one next version, when preflight parses it, then the existing no-release or exact tag-collision behavior is preserved.
- Given semantic-release exits successfully with an unknown message or conflicting versions, when preflight parses it, then the release remains blocked with an actionable error.
- Given the release workflow definition, when its validation inventory is inspected, then the release-preflight fixture suite runs before the live preflight and before semantic-release.

## Spec Change Log

- Review loop 1: adversarial, edge-case, and verification-gap review found that checking the stale marker before version extraction allowed mixed terminal output to bypass fail-closed ambiguity handling, while the stale fixture did not make remote probing observable. The implementation tasks now require full terminal-outcome classification before return, exact case-sensitive stale matching, mixed-outcome rejection, complete parser diagnostics, an origin that fails if contacted, and workflow-step uniqueness/unconditional execution. Avoid the known-bad early-return classifier. KEEP the clear stale-only success message, ordinary version/no-release and tag-collision behavior, unknown-output rejection, canonical workflow ordering, and focused Python/C# verification.
- Review loop 2: edge and adversarial review found that a stale marker plus a malformed next-version marker was still accepted, and that substring matching allowed embedded quotes or non-`main` branch messages to masquerade as the approved terminal result. Tasks now require complete-line `main` matching after recognized semantic-release logger-prefix handling and explicit rejection of every malformed next-version marker. Avoid the known-bad valid-version-only classifier and unanchored branch wildcard. KEEP review-loop-1 outcome cardinality, mixed-valid-outcome failures, observable unreachable-origin coverage, complete diagnostics, workflow gate, and green focused suites.
- Review loop 3: adversarial review found ANSI normalization was stale-only, causing colored valid-version output to regress into malformed classification, while verification review found the workflow contract could not detect `continue-on-error`. Tasks now require one normalized stream for every terminal classifier, colored version/no-release fixtures, remaining mixed-outcome combinations, and explicit failure-propagation parsing/assertion. Avoid the known-bad per-branch ANSI normalization and incomplete workflow-step model. KEEP complete-line `main` matching, malformed-marker detection, all prior negative fixtures, exact tag checks, workflow ordering, and green focused suites.
- Review loop 4: adversarial review found whole-stream marker scanning could interpret generated release-note or plugin payload text as malformed or conflicting terminal output. Tasks now require line-oriented classification with provenance: exact bare fixture records or recognized semantic-release logger records only. Payload text containing terminal phrases is ignored when a genuine outcome exists and remains unknown when no genuine outcome exists. Avoid the known-bad substring scanner. KEEP global ANSI normalization, exact `main` matching, malformed genuine-record rejection, all mixed-outcome guards, blocking workflow contract, exact tag checks, and the 23-fixture/446-test green baseline.

## Design Notes

The branch-behind line is a deliberate semantic-release early-return condition, not a command failure. Strip ANSI control sequences, then inspect complete lines: test fixtures may supply a bare terminal sentence, while production output must carry the repository-pinned semantic-release logger prefix. Generated notes and plugin payloads are not terminal records even when they quote similar text. Only one recognized, fully parseable outcome may bypass version/tag processing, and any mixture of genuine stale, no-release, malformed-version, or valid-version records is rejected. The later `main` run remains responsible for evaluating and publishing any release.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/release_preflight -p "*_test.py"` -- expected: all release-preflight parser and tag-collision fixtures pass.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release` -- expected: release workflow inventory and the remaining CLI test project pass.
- `git diff --check` -- expected: the scoped CI/tooling patch has no whitespace errors.

## Suggested Review Order

**Terminal classification**

- Provenance-aware parsing separates semantic-release outcomes from payload text.
  [`release-preflight.ps1:95`](../../tools/release-preflight.ps1#L95)

- Live execution disables bare fixture records before tag checks.
  [`release-preflight.ps1:245`](../../tools/release-preflight.ps1#L245)

**Release gate**

- Release jobs run parser fixtures before live or publish-capable work.
  [`release.yml:81`](../../.github/workflows/release.yml#L81)

- Workflow contracts require one unconditional, failure-blocking fixture step.
  [`CiTestInventoryTests.cs:194`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L194)

**Regression evidence**

- Stale checkout succeeds without probing remote tags.
  [`release_preflight_test.py:116`](../../tests/tooling/release_preflight/release_preflight_test.py#L116)

- Fake npm exercises live provenance rather than permissive fixture mode.
  [`release_preflight_test.py:400`](../../tests/tooling/release_preflight/release_preflight_test.py#L400)

- First-release parsing remains explicitly deferred outside this incident.
  [`deferred-work.md:2020`](./deferred-work.md#L2020)
