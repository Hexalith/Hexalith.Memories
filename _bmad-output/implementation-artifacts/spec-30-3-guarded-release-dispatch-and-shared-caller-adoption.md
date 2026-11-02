---
title: 'Guarded release dispatch and shared caller adoption'
type: 'chore'
created: '2026-10-31'
status: 'in-review'
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
- [x] .github/workflows/release.yml, tools/check-release-source.py -- dispatch main only; pre-environment tip/exact push-CI proof; protected job calls pinned composite with identical builds-execution-sha and count 10; reprove before OIDC publication. Preserve hooks, evidence, concurrency; remove duplicate tests.
- [x] .releaserc.json, tools/release-preflight.ps1, tools/publish-nuget.ps1 -- reject occupied/ambiguous versioned destinations before publication without changing publisher mechanics.
- [x] tools/release-packages.json, tools/release-packages.schema.json, tools/validate-release-packages.ps1, tools/pack-release.ps1, tools/qualify-builds-publication.py -- atomic packageId-to-id migration with unchanged ten IDs.
- [x] tools/complete-partial-release.ps1, .github/workflows/recover-partial-release.yml -- read old tagged packageId and current id manifests; reject mixed shapes, stage current manifest, preserve summary fields.
- [x] tests/tooling/release_dispatch/, tests/tooling/release_packages/, tests/tooling/builds_publication_qualification/, tests/tooling/publish_containers/, tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs -- refusal, reachability, inventory and historical recovery fixtures.
- [x] docs/dev/release-runbook.md, CONTRIBUTING.md -- document dispatch, protection readback, pin and recovery boundary.

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

The release job remains caller owned and retains NuGet OIDC, semantic-release hooks, evidence upload and non-cancelling concurrency. The pinned Builds action prepares the release and checks exact-source push CI; local wrappers reuse its source and destination helpers. Publication remains fail closed while production lacks required reviewers or the current tip lacks successful exact-source push CI. No publication or GitHub settings change was made.

The previously approved 30.2 evidence stays bound to its historical source and helper bytes. The qualification profile now binds the tracked Builds gitlink `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`; the historical capture remains bound to `f0feeb234cbc16f88c2886a6ffae7122e05dd64b`.

## Spec Change Log

- 2026-10-31: Approved numbering, inventory and authentication; deferred 30.3 behind prerequisites.
- 2026-11-02: 30.1/30.2 done; user selected 30.3. Production protection remains external.
- 2026-11-02: Integrated the remote Builds qualification pin and historical-byte corrections; retained the current release and historical recovery assertions.

## Review Triage Log

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| Blind 1: full qualification blocks push CI | high | Defer. `ci.yml` runs the full suite and its existing profile requires Builds `f0feeb…` while the tracked gitlink is `6e701…`; the focused 30.3 tests pass, but the CI gate cannot pass at this checkout. |
| Blind 2: production has no reviewers | high | Defer. Live API readback returned an empty protection rule list; the workflow correctly refuses entry, and the approved intent excludes settings changes. |
| Blind 3: removed restore exception breaks release | false | Reject. The pinned action's exact `dotnet restore ... -p:Configuration=Release` and `dotnet build ... --no-restore --configuration Release -warnaserror` both passed locally with zero warnings. |
| Blind 4: approval rule removal after source job | medium | Patch. `release-source` checks the rule, but the protected job has no second readback before publication; removal during the wait could bypass the observed protection. |
| Blind 5: qualification hashes normalized manifest | medium | Patch. The current tracked manifest has 2150 raw bytes and the qualification serializer yields 2096 bytes, so its identity does not bind the bytes consumed by the release action. |
| Blind 6: current nine-package `id` tag enters legacy recovery | medium | Patch. Recovery removes EventStoreExtensions from the expected set whenever it is absent, even when the tagged manifest uses the new `id` shape. |
| Blind 7: future catalog evolution breaks legacy recovery | low | Reject. No current historical tag fails; adding or removing packages would require a new catalog and recovery contract, and a versioned historical inventory is beyond this migration. |
| Blind 8: no real pinned source-helper integration fixture | low | Reject. The wrapper call signature matches the inspected approved helper and the 30.2 pinned-helper qualification covers its API parsing; adding a second Git-object-dependent network simulation would duplicate that lane. |
| Blind 9: destination wrapper tests only a count | low | Patch. A wrong five-member repository set would leave the current assertion green; capture the exact repositories passed by the wrapper. |
| Blind 10: no preflight output fixture | low | Patch. The workflow reads `next-version`, but existing preflight tests do not inspect `GITHUB_OUTPUT`. |
| Blind 11: checkpoint evidence is vague | low | Reject. The finding's fix edits this build's spec; the Verification section records exact commands, results and live blockers for review. |
| Blind 12: frozen intent calls 30.3 future work | false | Reject. That sentence records the 2026-10-31 run; the user's new 30.3 selection and later spec change log establish this run's intent without editing frozen text. |
| Edge 1: approval rule removed while waiting | medium | Patch. Same reachable gap as Blind 4: no rule readback in the protected job before publication. |
| Edge 2: pinned restore lacks NU1902 override | false | Reject. The exact restore and warning-as-error build both passed with zero warnings in this checkout. |
| Edge 3: ten-package recovery accepts four images | false | Reject. `complete-partial-release.ps1` requires the DomainService image whenever EventStoreExtensions is in the tagged package set. |
| Edge 4: frozen intent says future work | false | Reject. The 2026-10-31 historical statement is superseded by the user's current selection. |
| Verification 1: CI workflow argument unasserted | low | Patch. The successful fake-helper test ignores the requested workflow; a wrong argument could ask for a weaker push workflow before approval. |
| Verification 2: destination repository set unasserted | low | Patch. The current fixture checks only count; an exact-set assertion will protect all five publisher repositories. |
| Verification 3: next-version output unasserted | low | Patch. Renaming the output key would skip all publish-capable workflow steps without failing the current preflight fixtures. |

After this review, commits `7e2e87e80bccc3c6f4f6ceb27a09a649a5be7f71` and `ad9a5ff649a31341f02522c31f36a86ff4b414ea` resolved Blind 1. The integrated full qualification suite passed 70/70.

## Verification

- Passed after review fixes: `release_dispatch` (7/7); `release_packages` (28/28); `release_preflight` (48/48); `publish_nuget` (32/32); `publish_containers` (46/46); qualification `ReleaseSetTests` (6/6) and `CommittedEvidenceTests` (12/12); `actionlint` on the three changed workflows; `pwsh -NoLogo -NoProfile -File tools/validate-release-packages.ps1`; `npm run verify:semantic-release-config --silent`; built `CiTestInventoryTests` (137/137). The pinned preparation commands `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release` and `dotnet build Hexalith.Memories.slnx --no-restore --configuration Release -warnaserror` passed, with zero build warnings.
- After integrating remote qualification corrections, `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py'` passed 70/70; `ci_alignment` passed 12/12, and the built `CiTestInventoryTests` passed 137/137. The earlier 60-test run failed on the then-current profile mismatch and is preserved in review history above.
- Live readback on 2026-11-02: `gh api repos/Hexalith/Hexalith.Memories/environments/production` returned `protection_rules: []`, `deployment_branch_policy: null`; the then-main SHA `637eaa1b14b9251d45b7b44ed8644f2b2a9a0e55` had no completed successful push `ci.yml` run. Production approval and successful exact-source CI for a new tip are required before live publication acceptance.
