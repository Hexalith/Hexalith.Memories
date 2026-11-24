---
title: 'Close ten deferred release safeguards'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Tighten release publication and recovery safeguards and bring operator instructions up to date. Close ten entries with evidence from the maintained scripts and executable fixtures.

- Given a historical container publication, when authenticated inspection explicitly reports the requested manifest missing, then copying may proceed. Authentication, transport, and malformed responses must fail closed for that image. Matching and conflicting existing tags retain their current outcomes (DW-567, DW-569).
- Given a partial-release incident for the same version, when the alert is rerun, then append the evidence to its exact-title issue and reopen it if closed. Reconcile uncertain reopen responses before retrying (DW-158).
- Given a staged package directory, when publishing selects files, then it selects real `.nupkg` files by the same extension rule as validation. Symbol packages, directories, and unrelated files stay out (DW-322, DW-390).
- Given a tagged historical image table, when the pre-mutation inventory check runs, then it emits the approved two- or four-repository selection and refuses shapes that omit Server or MCP (DW-636).
- Given the current release path, when an operator consults the runbook or the replacement test runner, then they can find the current checklist, the stale-tag preflight invariant, the baseline-waiver record and the pre-publication Skopeo prerequisite (DW-66, DW-67, DW-149, DW-570).

</frozen-after-approval>

## Implementation Notes

- Baseline `46636c6cfea596b6d66b0fe3e35234c799051e26` with 309 open records. The existing guarded release probes Skopeo before destination preflight, OIDC login, or semantic-release. The historical publisher and incident helper have focused subprocess fixtures. Architecture D17 retains the guarded current publication and historical compatibility paths.
- Use the existing release tooling and fixture commands. Fixture commands exercise local scripts with fake publishers and GitHub CLI responses. The current-ten-package/five-image guarded contract remains the source of truth for current publication.
- Before the fixes, the new tests reproduced eight unsafe copy cases, three closed-issue reruns that created a duplicate, and a package validation failure on a directory ending in `.nupkg`.
- The historical publisher now confirms a reported absence over authenticated HTTPS with redirects disabled. A localhost TLS fixture exercised the production PowerShell lookup and observed the expected Basic credentials on the exact manifest GETs. It accepted HTTP 404 with `MANIFEST_UNKNOWN`, refused HTTP 200, 401 and 403 carrying the same error code, and followed no redirect. A separate localhost Skopeo fixture confirmed the ambiguous diagnostics on which the review finding was based. [OCI Distribution Specification](https://github.com/opencontainers/distribution-spec/blob/main/spec.md#error-codes)
- The incident helper uses exact-title search and a direct-list fallback before creating an issue. It reopens closed matches, prefers an existing open match, and uses one per-invocation marker to correlate comment readback with the current append. [GitHub CLI issue list](https://cli.github.com/manual/gh_issue_list) and [issue reopen](https://cli.github.com/manual/gh_issue_reopen)
- The release-runbook links resolved. A local execution of the workflow's unmodified archive-transport step stopped with exit 42 when the Skopeo probe failed. The step runs before destination preflight and NuGet login on the publish-enabled path.
- Local `run-with-temp python3 -B -m unittest discover` runs passed in the focused tooling directories. NuGet publication and incident handling passed 37 tests, package validation 28, release preflight 50, release dispatch 11, release orchestration 8, and partial-release completion 9.
- The final isolated run selected every method in `publish_containers_test.py` with `python3 -B -m unittest -q <method>`. All 29 methods passed, including the staged historical inventory gate, malformed manifest responses, the authenticated response status matrix, and the safe copy, matching-tag and conflicting-tag controls.
- Closed DW-66, DW-67, DW-149, DW-158, DW-322, DW-390, DW-567, DW-569, DW-570 and DW-636. The ledger now has 299 open records.

## Review Triage Log

- High, patched. Skopeo loses HTTP status in some error diagnostics. The publisher now checks authenticated HTTP 404 and the structured registry error before copying.
- Medium, patched. A version-specific Skopeo diagnostic can include human-readable detail or escaped quotes after the error code. The authenticated structured response determines absence even when that detail changes.
- Medium, patched. A malformed successful manifest could masquerade as the matching digest through PowerShell array coercion. The historical path explicitly rejects arrays at object boundaries and requires schema version 2, a layers array, a config object and a scalar string digest.
- Medium, patched. The issue search index can omit an existing exact match. Verify the direct listing before creating an incident.
- Medium, patched. An identical older comment could satisfy lost-response readback. The new retry marker remains stable within one append and distinct across reruns.
- Medium, patched. PowerShell file enumeration includes links. Both package validation and publishing exclude reparse points from their staged inputs.
- Low, patched. The stale-tag section gives owner-approved options for an abandoned tag and links partial publication to recovery instructions.
- Low, patched. The release-day checklist distinguishes a successful no-version run from verification of a published set.

## File Scope

Allowed files for this story:

- `tools/publish-containers.ps1`
- `tools/publish-nuget.ps1`
- `tools/validate-release-packages.ps1`
- `tools/create-partial-publish-issue.ps1`
- `tools/test.ps1`
- `tests/tooling/publish_containers/publish_containers_test.py`
- `tests/tooling/publish_nuget/publish_nuget_test.py`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`
