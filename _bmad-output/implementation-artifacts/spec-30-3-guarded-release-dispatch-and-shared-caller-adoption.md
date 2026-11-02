---
title: 'Guarded release dispatch and shared caller adoption'
type: 'chore'
created: '2026-10-31'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-3-guarded-release-dispatch-and-shared-caller-adoption'
baseline_commit: '46667b0d25c707bcdc961d59895bfe5a064bfd12'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release publishes on pushes without proving green push CI for the exact current main tip.

**Approach:** Guard intentional dispatch, use pinned shared preparation, and migrate the manifest atomically. This future story follows shared CI adoption and local Builds qualification; it is not selected for implementation in this run.

## Boundaries & Constraints

**Always:** Prove source before protected approval and again before publication. Preserve Trusted Publishing, package/image membership, historical recovery, deployment evidence and non-cancelling concurrency. Enforce the independently declared count and identical approved Builds SHA. Reuse shared helpers; avoid duplicate application tests.

**Never:** Publish, change GitHub settings/dependencies, waive predecessors, replace container publishers or claim workflow declarations prove actual environment protection.

## I/O & Edge-Case Matrix

| State | Behavior |
| --- | --- |
| Push | No publication |
| Non-main/stale dispatch; missing exact push CI | Reject before protected job |
| Source changes during approval/build | Reject before login/publication |
| Wrong count/SHA, occupied/ambiguous destination | Fail before publication |
| Historical tagged manifest | Recover without rewriting evidence |


## Approved Decisions (2026-10-31)

The user selected all recommendations: reconcile Epic 30 into six stories and implement bounded shared CI as 30.1 first; preserve ten packages and five current images; use the pinned prepare-domain-release composite with caller-owned Trusted Publishing for guarded release. Set expected-package-count explicitly to 10; preserve historical nine-package recovery and old packageId manifests at that boundary. This story is now 30.3 (historical alias: former 30.1), after 30.1 and 30.2 qualification. Approval does not establish those prerequisites or authorize publication.

</frozen-after-approval>

## Code Map

- .github/workflows/release.yml:3-209 -- push publisher; preserve OIDC, hooks, evidence; move source proof before environment.
- references/Hexalith.Builds/Github/prepare-domain-release/action.yml:52-196 -- approved SHA 81c88b34d09d34260b92d7f0928dbbc1c7446baa; checks SHA, ten id entries and live source.
- references/Hexalith.Builds/Github/publish-containers/publication_preflight.py:581-708 -- reuse source/CI and destination proofs; do not copy shared logic.
- tools/release-packages.json, tools/release-packages.schema.json, tools/validate-release-packages.ps1:229-362, tools/pack-release.ps1:58-61 -- packageId manifest/readers.
- tools/qualify-builds-publication.py:408-440 and tests -- replace obsolete transient packageId conversion; preserve approved evidence.
- tools/complete-partial-release.ps1:172-282, .github/workflows/recover-partial-release.yml:62-71 -- historical/current manifests and summary. Full current-set recovery belongs to 30.5.

## Tasks & Acceptance

**Execution:**
- [ ] .github/workflows/release.yml, tools/check-release-source.py -- dispatch main only; pre-environment tip/exact push-CI proof; protected job calls pinned composite with identical builds-execution-sha and count 10; reprove before OIDC publication. Preserve hooks, evidence, concurrency; remove duplicate tests.
- [ ] .releaserc.json, tools/release-preflight.ps1, tools/publish-nuget.ps1 -- reject occupied/ambiguous versioned destinations before publication without changing publisher mechanics.
- [ ] tools/release-packages.json, tools/release-packages.schema.json, tools/validate-release-packages.ps1, tools/pack-release.ps1, tools/qualify-builds-publication.py -- atomic packageId-to-id migration with unchanged ten IDs.
- [ ] tools/complete-partial-release.ps1, .github/workflows/recover-partial-release.yml -- read old tagged packageId and current id manifests; reject mixed shapes, stage current manifest, preserve summary fields.
- [ ] tests/tooling/release_dispatch/, tests/tooling/release_packages/, tests/tooling/builds_publication_qualification/, tests/tooling/publish_containers/, tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs -- refusal, reachability, inventory and historical recovery fixtures.
- [ ] docs/dev/release-runbook.md, CONTRIBUTING.md -- document dispatch, protection readback, pin and recovery boundary.

**Acceptance Criteria:**
- Given a non-tip SHA or missing exact-source push CI, when dispatched, then reject before production.
- Given valid dispatch, when preparing and publishing, then prove approved Builds SHA, ten IDs, live source and absent destinations; failed or ambiguous proof prevents publication.
- Given a normal push, when CI runs, then publication is unreachable and release does not repeat application tests.
- Given a tagged packageId manifest, when recovering, then consume it unchanged; reject mixed shapes.

## Checkpoint Evidence

On 2026-11-02, gh api repos/Hexalith/Hexalith.Memories/environments/production returned protection_rules: [] and deployment_branch_policy: null. GitHub settings changes are excluded.

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Non-tip dispatch rejection | Memories Release Engineer | Non-tip fixture | pending | not complete |
| Exact-source push CI proof | Memories Release Engineer | SHA-bound and missing-CI fixtures | pending | not complete |
| Protected production approval | Memories Release Engineer | Environment API readback | pending | blocked: no rules |
| Matching pinned Builds identity | Memories Release Engineer | SHA caller/mismatch fixture | pending | not complete |
| No publication on push | Memories Release Engineer | Trigger/reachability fixture | pending | not complete |
| OIDC publication retained | Memories Release Engineer | Caller/hook fixtures | pending | not complete |
| No duplicate application tests | Memories Release Engineer | Caller invocation fixture | pending | not complete |
| Atomic ten-package migration | Memories Release Engineer | Validator/unchanged-ID fixtures | pending | not complete |
| Historical manifest recovery | Memories Release Engineer | Tagged packageId fixture | pending | not complete |
| Destination absence refusal | Memories Release Engineer | Occupied/ambiguous fixtures | pending | not complete |

## Implementation Notes

## Spec Change Log

- 2026-10-31: Approved numbering, inventory and authentication; deferred 30.3 behind prerequisites.
- 2026-11-02: 30.1/30.2 done; user selected 30.3. Production protection remains external.

## Review Triage Log

## Verification

- Run unittest discovery separately for release_dispatch, release_packages, builds_publication_qualification, publish_containers; all pass.
- Run validate-release-packages.ps1, actionlint release.yml, and built Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests; all pass.
- Read back production protection and exact-source live CI before done.
