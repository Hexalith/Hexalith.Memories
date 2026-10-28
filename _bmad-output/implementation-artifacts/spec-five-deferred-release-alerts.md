---
title: 'Close five deferred partial-release alert gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-152, DW-154, DW-155, DW-156 and DW-157 by hardening the existing partial-release issue helper. Preserve the deterministic issue title, exact-title verification, existing-issue comments, summary evidence and recovery instructions. Given a valid partial summary, when the helper looks up an incident, then it searches quoted title text with an explicit larger limit. Given null artifact entries, when lists render, then nulls are omitted and an empty list says None. Given transient GitHub transport, throttling or server failures, when an operation fails, then bounded retries with visible backoff recover or fail explicitly; ambiguous issue/comment writes are reconciled before another write. Permanent errors stop immediately. Given malformed summary JSON, when the helper runs, then it fails with an actionable path-specific diagnostic before invoking GitHub. Given empty or whitespace lookup output, when no issue is returned, then it explicitly normalizes to an empty collection and creates the incident.

Update the five deferred dispositions with actual verification evidence, independently review the change, commit and push to main as requested. This work changes local script behavior and fake-CLI fixtures; it does not execute a live alert, publish artifacts or change submodules.

</frozen-after-approval>

## Implementation Notes

- Route assessment: no unresolved user intent; bounded changes to one helper, existing subprocess fixtures, the release runbook, this trace and five register entries. No new public API or dependency, migration, deployment or runtime data mutation. Commit and push are explicitly authorized.
- Baseline revision: `06662fa7cfe0c36c1195bfda2630955bea310971`; clean main. Repository baseline, Git instructions, architecture D17 and contributor/testing guidance were inspected. No application or AppHost code is affected, so an application launch is not relevant.
- Code map: `tools/create-partial-publish-issue.ps1` owns summary parsing, list rendering and issue operations; reuse deterministic title and body-file cleanup. `tests/tooling/publish_nuget/publish_nuget_test.py` supplies fake gh, controlled summaries, captured arguments/body files and existing create/comment/skip controls. `docs/dev/release-runbook.md` documents incident behavior. Existing CI already runs the fixture directory.
- Baseline verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 8/8 methods in 28.856s, exit 0. Extend the fake with sequential outcomes and remote write state to prove retries, terminal failures and lost-response reconciliation without live service calls.
- Investigation nuance: DW-157's claimed crash does not reproduce on installed PowerShell; blank JSON output currently produces a null sentinel excluded by title filtering. Close it as explicit normalization and retained compatibility coverage, not a reproduced crash.
- CLI options checked against official [issue list](https://cli.github.com/manual/gh_issue_list) and [issue view](https://cli.github.com/manual/gh_issue_view) documentation. Search remains bounded; concurrent independent release runs and closed-issue lifecycle remain separate existing debt.
- Repository scope tooling requires an explicit File Scope for standalone specs, so the exact five-file contract below supplements the minimal oneshot template.
- Original-helper regression run: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -k IssueHelperReliabilityTests -v` ran 13 methods in 41.267s; 10 failed on the intended search, null formatting, malformed-summary and retry/readback gaps, while blank-output compatibility, permanent-error behavior and malformed-lookup refusal passed. No runner errors occurred.
- Implemented contextual summary failure, quoted title search with limit 100, null filtering and an internal three-attempt GitHub wrapper. Retryable failures emit backoff warnings; ambiguous writes require successful readback before another write. Create readback uses a direct issue listing to avoid GitHub search-index lag; comment readback compares the exact body. Temporary body/diagnostic files are removed in finally blocks. The runbook documents bounded recovery and preserves the release failure.
- First full implementation run: 20/21 methods passed; the new search fixture accidentally included impossible issue number zero, correctly refused by the helper's response validation. Corrected that fixture to positive issue numbers and added malformed-readback and write-budget controls before the final run.
- Final implementation verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 23/23 methods in 123.928s, exit 0. Comparable method discovery increased from 8 to 23 (+15). All original publish, create, comment and skip controls pass. Exactly the five selected deferred entries now carry dated resolutions and reopen triggers.
- Exact five-file scope validation passed. Tenant-isolation evidence is a documented no-op because no tenant surface changed; story readiness is a documented no-op because this minimal standalone oneshot has no governed ledger/File List/evidence table. Neither no-op is claimed as runtime or phase-ledger proof. `git diff --check` passes. `git fetch --no-recurse-submodules origin` confirmed HEAD and origin/main at the same baseline before committing.
- Independent blind review returned six findings. All were verified and patched within the planned helper/test scope. Rate-limit pacing now follows the official [GitHub guidance](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api#exceeding-the-rate-limit): explicit retry/reset timing when surfaced by gh, otherwise exponential 60/120-second waits before any further request. A shared 180-second wait budget bounds nested readback/retry work. Tests replace Start-Sleep only inside an isolated fixture-driver process, recording requested delays and their position among GitHub calls without real waits.

## Review Triage Log

- Medium, patched: blank create readback was accepted as absence and could repeat a remotely applied write. Initial lookup still accepts blank output; ambiguous-write readback now requires valid JSON, with lost-response/blank-output regressions.
- Medium, patched: HTTP 403 secondary throttling and GraphQL API-limit diagnostics were classified as permanent. The wrapper recognizes those specific rate messages; ordinary 403 permission failures still stop immediately.
- Medium, patched: one-/two-second backoff and immediate readback ignored rate windows. Rate failures now wait before queries/readback, honor surfaced Retry-After/reset timing and stop if the shared wait budget cannot accommodate the required delay. Fake-clock fixtures verify pacing and exhausted budgets.
- Medium, patched: captured native stderr vanished on terminal failure. Terminal exceptions now retain a bounded diagnostic after replacing known token values, authorization headers and URL credentials. A regression preserves the actionable GraphQL reason while rejecting secret values.
- Low, patched: pipeline unwrapping accepted a singleton root array as a summary object. NoEnumerate parsing now retains root shape; array, null, primitive, missing-status and blank fixtures fail before GitHub.
- Medium, patched: title-only create readback could find a preexisting incident and omit the current evidence. Readback now requests body data, compares the current body, and comments the current run evidence onto an older incident. A sequential search-lag regression proves one incident plus the current evidence comment.

- Review verification exposed PowerShell-specific edges: NoEnumerate arrays can still satisfy `-is [pscustomobject]`, so both summary and comment-readback guards explicitly reject arrays. A distant reset timestamp selected Math.Max's Int32 overload; both operands now force Int64 before checking the wait budget. The isolated fixture driver uses ErrorActionPreference Stop so helper invocation errors cannot masquerade as successful test-process exits, and root-shape subcases reset fake state before each attempt. Targeted summary-root verification passed all six subcases (one method, 19.413s).
- Final post-review verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods in 144.401s, exit 0. Comparable discovery: 8 -> 23 during implementation (+15), then 23 -> 30 during review (+7), cumulative +22 methods. Targeted timing-budget verification also passed (one method, 11.241s). The independent reviewer confirmed the six original fixes and the final PowerShell corrections, with no unresolved issue in those paths; no review finding was deferred. All five register resolutions cite the final 30-method result. Commit message passed repository commitlint before staging; managed hooks and post-commit commitlint remain enabled.

## File Scope

Allowed files for this story:

- `tools/create-partial-publish-issue.ps1`
- `tests/tooling/publish_nuget/publish_nuget_test.py`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md`
