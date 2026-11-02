---
title: 'Guarded release dispatch and shared caller adoption'
type: 'chore'
created: '2026-10-31'
status: 'in-progress'
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
| Round 2 Blind 1: five-image recovery checks deployment for DomainService | high | Defer. `complete-partial-release.ps1` requires every summary image in the deployment, while the existing renderer has no DomainService parameter; current-set recovery fails. Full five-image recovery is Story 30.5's declared scope. |
| Round 2 Blind 2: recovery fixture invents a DomainService deployment reference | medium | Defer with Blind 1. The existing fixture writes every summary image into synthetic deployment text, so it misses the renderer mismatch. Story 30.5 must use the actual four-image render shape. |
| Round 2 Blind 3: source proof can fail after tag push without alert | high | Patch. Installed semantic-release pushes the tag before `publishCmd`; the new proof was at the start of that command, before any aggregate summary. Moved the proof to the end of `prepareCmd`, which runs before tag creation. |
| Round 2 Blind 4: stale dry-run can finish guarded dispatch green | medium | Patch. The pre-existing stale outcome returned success with no `next-version`, and the new workflow would skip its later source proof. `-CheckDestinations` now rejects that outcome; ordinary no-release remains successful. |
| Round 2 Blind 5: NuGet skip-duplicate can hide a post-probe collision | high | Defer. `publish-nuget.ps1` already used `--skip-duplicate` before this story; a writer racing the absence probe can yield zero exit and a false pushed result. Story 30.5 owns immutable partial-release reconciliation. |
| Round 2 Blind 6: recovery has no production approval gate | high | Defer. The recovery workflow already publishes through a job without `environment: production`; Story 30.5 owns recovery authorization. |
| Round 2 Blind 7: recovery tooling lacks exact-source CI proof | high | Defer. Recovery stages tools from its dispatched main checkout without a current-tip/exact-push-CI check; this predates this release-dispatch change and belongs to Story 30.5. |
| Round 2 Blind 8: historical recovery probes unused current repositories | medium | Defer. `verify-container-registry.ps1` probes the current five-member set before a historical tag is checked out, so lost access to an unused repository can block old recovery. Story 30.5 owns the tagged-set scope. |
| Round 2 Blind 9: destination set does not bind projects to repositories | medium | Defer. The new preflight proves absence for five names, but the existing container publisher owns the project mapping; Story 30.4 explicitly owns five mapping verification. |
| Round 2 Blind 10: runbook describes old hook and four images | low | Patch. The changed verify hook now includes destination proof and the current publisher has five archives; corrected the operator text. |
| Round 2 Blind 11: checkpoint table remains pending | low | Reject. The proposed correction edits this build's spec, which this review step excludes from finding fixes; the Verification section records fixture results and the external approval blocker. |
| Round 2 Blind 12: production has no required reviewers | high | Carried defer from Blind 2: the 2026-11-02 API readback had no protection rules, and approved intent excludes GitHub settings changes. |
| Round 2 Edge 1: current tag can complete with four of five images | high | Defer. The pre-existing recovery guard requires DomainService with ten packages but does not require the clock; Story 30.5 owns exact five-image recovery membership. |
| Round 2 Edge 2: pinned restore lacks advisory override | false | Carried reject from Edge 2: the exact pinned restore and warning-as-error build passed locally with zero warnings. |
| Round 2 Verification 1: publication flag binding untested | medium | Patch. The pinned action defaults an omitted input to frozen publication; the dispatch fixture now pins the workflow's repository-variable binding and exercises a failing source proof in the prepare command. |
| Round 2 Verification 2: late reviewer check lacked executable negative proof | medium | Patch. The previous fixture checked only step placement; it now runs the workflow command against empty and nonempty reviewer-rule responses. |

After this review, commits `7e2e87e80bccc3c6f4f6ceb27a09a649a5be7f71` and `ad9a5ff649a31341f02522c31f36a86ff4b414ea` resolved Blind 1. The integrated full qualification suite passed 70/70.

### Review Findings

Code review 2026-11-02 (`bmad-code-review`, full mode, unchunked) of `46667b0d25c707bcdc961d59895bfe5a064bfd12..e964fb33` (5 commits, 31 paths). Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor, Historical Slice Guard, Story Phase Ledger; none failed. Readiness gate: `python3 tools/check-story-review-readiness.py --story-key 30-3-guarded-release-dispatch-and-shared-caller-adoption --changed-files-file <git diff --name-only 46667b0d..HEAD>` exited 1 with C4 (spec `done` vs sprint-status `review`) and C2 (no phase ledger); without `--changed-files-file` it adds 10× C6 (pending checkpoint rows). C1 was skipped because there is no File List. `python3 tools/check-story-slice-scope.py --changed-files-file <same>` exited 1: `Historical Context Classification` and `Slice Proof` are missing. Fail closed: the story cannot be `done`. Reviewer re-runs at HEAD all passed: `release_dispatch` 9, `release_packages` 28, `release_preflight` 49, `publish_nuget` 32, `publish_containers` 45, `ci_alignment` 12, `builds_publication_qualification` 70 test methods, and `CiTestInventoryTests` 137 test cases (runner Total). Live readback 2026-11-02: `production` has `protection_rules: []`, `deployment_branch_policy: null`, `can_admins_bypass: true`; push CI run `41457446005` for `e964fb33` was `in_progress`.

- [x] [Review][Decision] Three Builds revisions are in play, and nothing binds them together — `release.yml` and both wrappers pin the approved 30.2 revision `81c88b34` (`30-2-qualification/approval.md`). Commit `7e2e87e8` re-pinned `shared-ci` and `tools/builds-publication-qualification.json` to `6e7011fc`, which has no owner approval; qualification run `41452783423` only passed offline. Commit `ad9a5ff6` then rebound `CommittedEvidenceTests` to the historical `f0feeb2` bytes and dropped the `profile.buildsSha == capture` check. Neither commit is in Tasks. Implementation Notes call the `f0feeb2` capture "previously approved", but its `approval.md` reads `Status: pending`. Blind Hunter's premise that the release runs "never-qualified" bytes is false; the missing binding is real. Choose one: keep the release on `81c88b34`, claim the realignment in 30.3 and test the divergence; record approval of `6e7011fc` and move the release pin; or exclude the three realignment-only paths with a named owner. (historical-slice-guard+acceptance-auditor+verification-gap+edge-case-hunter+blind-hunter+story-phase-ledger) — resolved 2026-11-02 by the owner: keep the release on approved `81c88b34`; claim the CI/profile realignment as 30.3 scope (see the resolved patch below).
- [x] [Review][Decision] Recovery widening imports Story 30.5 scope and lets a current release complete with four of five images — `recover-partial-release.yml:140,185-187` now accepts 9 or 10 packages and 2, 4 or 5 images, with no binding between the package and image counts. A ten-package summary that omits the clock image therefore passes. The baseline rejected every ten-package summary, so Round 2 Edge 1 is caused by this change, not pre-existing, and the Edge 3 rejection is wrong. The runbook (`release-runbook.md:503`) claims current-release recovery works and that the Zot check is tagged-set scoped; it actually uses the fixed five repositories. `publish-nuget.ps1:46,91,106` sends NuGet failures to a recovery path that never pushes NuGet. Choose one: revert the widening so current-set recovery stays fail-closed until 30.5, keeping shape reading, staging and mixed-shape refusal; or own full current-set recovery in 30.3. (historical-slice-guard+acceptance-auditor+blind-hunter+edge-case-hunter) — resolved 2026-11-02 by the owner: revert the widening; current-set recovery stays fail-closed until 30.5 (see the resolved patch below).
- [x] [Review][Decision] The publish-time destination re-probe runs after the tag and can strand it — `publish-nuget.ps1:159` re-probes all 15 destinations inside `publishCmd`, after semantic-release has pushed the tag. A transient probe failure refuses every NuGet push, while `publish-release.ps1` still pushes the images. Recovery cannot push NuGet. The result is an orphaned tag with images and no packages. Choose one: move the second probe into `.releaserc.json` `prepareCmd`, before the tag (the Round 2 Blind 3 precedent); make a NuGet pre-push refusal also stop containers; or accept the risk and document it. (blind-hunter+acceptance-auditor+edge-case-hunter) — resolved 2026-11-02 by the owner: move the second probe into `prepareCmd` before tag creation (see the resolved patch below).
- [x] [Review][Decision] How strict the production approval readback must be — the jq check at `release.yml:68,140` only requires a non-empty `required_reviewers` rule. The live environment has `can_admins_bypass: true` and `deployment_branch_policy: null`. Requiring `prevent_self_review` would lock out the sole owner. Choose one: also require `can_admins_bypass == false` and a non-null branch policy, and record self-review as accepted for the sole owner; keep the reviewers-only check and record the residual risk; or require `prevent_self_review`, which needs a second reviewer. (blind-hunter+edge-case-hunter+acceptance-auditor) — resolved 2026-11-02 by the owner: require reviewers, `can_admins_bypass == false` and a non-null branch policy; record sole-owner self-review as an accepted exception (see the resolved patch below).
- [x] [Review][Decision] How 30.3 closes — AC1's protected `production` environment does not exist live (`protection_rules: []`). Exact-source push CI has not yet succeeded for any 30.3 tip, and no guarded dispatch trace exists. The frozen intent excludes GitHub settings changes. Choose one: keep the story open until the owner configures protection and one frozen guarded dispatch (publication flag not `true`) proves source job → approval → preparation without publishing; or close on the fixture-proven behaviour, with the live rows carried forward as accepted debt in one DW entry that has an owner, consequence and reopen trigger (L15). (historical-slice-guard+acceptance-auditor+story-phase-ledger) — resolved 2026-11-02 by the owner: keep the story open; close the live rows with a production protection readback, green exact-source push CI, and one frozen guarded dispatch trace (see the owner action below).
- [ ] [Review][Patch] (Resolved decision: Builds pin.) Claim the realignment-only paths `tests/README.md`, `tests/tooling/ci_alignment/ci_alignment_test.py` and `tools/builds-publication-qualification.json` in the File List, with ledger rows. Add a test asserting that the `release.yml` pin equals the approved 30.2 revision `81c88b34` and that the profile `buildsSha` equals the tracked gitlink. Change "previously approved 30.2 evidence" in Implementation Notes to say that the `f0feeb2` capture approval is pending. [tools/builds-publication-qualification.json:3]
- [ ] [Review][Patch] (Resolved decision: recovery.) Revert the count widening in `recover-partial-release.yml` to the baseline nine-package, two-or-four-image unit. Keep the manifest staging (line 71) and the shape reading and mixed-shape refusal in `complete-partial-release.ps1`. Restore the `recovery-workflow-superseded-set` assertions owned by 30-5 in the qualification test. Correct the runbook: current releases are not recoverable until 30.5, and the Zot check uses the fixed five repositories. Correct the `publish-nuget.ps1` guidance: NuGet failures need the manual reconciliation in the runbook. Mark the Edge 3 rejection superseded, and Round 2 Edge 1 as caused by this change and moot after the revert. [.github/workflows/recover-partial-release.yml:140]
- [ ] [Review][Patch] (Resolved decision: destination probe.) Move the publish-time destination proof from `publish-nuget.ps1:155-163` into `.releaserc.json` `prepareCmd`, after pack and beside the source proof, before tag creation. Update `verify-semantic-release-config.mjs`, the `publish_nuget` and `release_dispatch` fixtures (add a refusing destination proof in the prepare hook), and the runbook. [tools/publish-nuget.ps1:159]
- [ ] [Review][Patch] (Resolved decision: approval readback.) Extend both readbacks to require a non-empty `required_reviewers` rule, `can_admins_bypass == false` and a non-null `deployment_branch_policy`. Record sole-owner self-review as an accepted exception in this spec and the runbook. Add fixtures for bypass enabled and for a null branch policy. [.github/workflows/release.yml:68]
- [ ] [Review][Action] (Resolved decision: closure; owner action.)
  1. In the `production` environment: add yourself as required reviewer, disable admin bypass, and restrict deployments to `main` or protected branches.
  2. Record `gh api repos/Hexalith/Hexalith.Memories/environments/production`.
  3. Wait for a successful exact-source push `ci.yml` run on the current `main` tip.
  4. Dispatch Release once with `HEXALITH_RELEASE_PUBLISH_ENABLED` unset. Record the run proving: source job pass → approval → `publish-enabled=false` → no login or publication.
  5. Then complete the live checkpoint rows. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:69]
- [ ] [Review][Patch] Restore the deleted `Historical Context Classification` and `Slice Proof` sections and correct their rows. Former 30.1 umbrella: anti-template. Current 30.1/30.2: historical-reference-only. 30.4–30.6: ownership boundaries. 27.3: anti-template (deployment boundary only). Pinned helper: current-narrow-pattern. Write the slice proof after the decisions above. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:1]
- [ ] [Review][Patch] Add the canonical phase ledger, the cumulative File List and the checkpoint reconciliation; sync status to `review`. Ledger rows: create-story adoption baseline at `46667b0d`, then dev-story, then code-review. The dev-story row records the delta combined with review round 1: no post-dev snapshot exists, so it carries an explicit note. Map per-lane counts by test method and capture the `CiTestInventoryTests` baseline at `46667b0d`. Add a File List matching the cumulative name-status set (31 paths). Give each checkpoint row an exact command and date, or a blocked record with owner, consequence and reopen trigger. Repair the stale Verification counts (7/48/46 → 9/49/45, and the "60-test run"). Record the readiness gate result. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:5]
- [ ] [Review][Patch] Add the missing `Epic AC Verification` table. The inherited claims are ten IDs including EventStoreExtensions, five images, `packages[].id`, the approved 40-character SHA, `cancel-in-progress: false`, no publish on push, and the protected `production` environment. The last is a confirmed implementation gap, not a corrected claim. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:53]
- [ ] [Review][Patch] Record remediation-runtime-checklist applicability. Category 3 applies: partial-release compensation and NuGet `--skip-duplicate` dedup of shared release state. The dedup false-success sub-defect needs its accepted blocker (owner, consequence, trigger). Categories 1, 2 and 4 are not applicable. Category 5 is covered by the ledger. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:86]
- [ ] [Review][Patch] The completion record wrongly says nothing was published. The then-active push trigger published `v3.0.16` from `637eaa1b` (run `41440624004`) and `v3.0.17` from `7e2e87e8` (run `41446831529`). `v3.0.16` came from a spec-only `fix:` commit. Correct Implementation Notes and Verification; published versions are immutable. [spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md:88]
- [ ] [Review][Patch] The release gate-step contract is under-asserted. Parse `id:`. Assert the exact shared `if:` on Reprove, Recheck, login, semantic-release and Detect, and that the ids `prepare` and `release-preflight` exist. Restore `ContinueOnError` null on gate steps. Assert the `on:` set is exactly `{workflow_dispatch}`. Make step parsing job-aware, and assert `needs: release-source` and `environment: production` on `release`. Ban `dotnet test` in the release job. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:2373]
- [ ] [Review][Patch] The reviewer-rule fixture cannot tell `any` from `all`. Add failing cases for `{"protection_rules": []}` and for wait-timer-only rules, and run the release-source copy as well as the late copy. [tests/tooling/release_dispatch/release_dispatch_test.py:127]
- [ ] [Review][Patch] The destination wrapper's pinned-revision and helper-presence guards never run in a test. Add a wrong-SHA refusal, a missing-helper refusal and a rejected-version case. [tools/check-release-destinations.py:24]
- [ ] [Review][Patch] No test runs recovery from the staged tooling directory. Add a case that copies only the workflow-staged files into a temporary directory and runs from there, with the manifest absent and present. [tests/tooling/publish_containers/partial_release_completion_test.py:16]
- [ ] [Review][Patch] A non-main dispatch is skipped instead of refused. Remove the job-level `if:` so `check-release-source.py` fails the run red, and update the inventory assertion. [.github/workflows/release.yml:35]
- [ ] [Review][Patch] A guarded dispatch with a computed `next-version` can finish green without a tag. Make Detect fail instead of writing `published=false`. [.github/workflows/release.yml:169]
- [ ] [Review][Patch] Docs went stale after the dispatch migration. The concurrency comment still says "next push to main". The runbook Prerequisites omit the reviewer rule and `HEXALITH_RELEASE_PUBLISH_ENABLED`. "The script filters" now has no referent. The CONTRIBUTING table does not separate variables from secrets and omits `HEXALITH_ZOT_REGISTRY`. The CONTRIBUTING checklist lacks the `release_dispatch` and `release_preflight` fixtures and a dry run that works without the pinned checkout. [docs/dev/release-runbook.md:178]
- [ ] [Review][Patch] The new deferred-work entries have no `## Deferred from` heading of their own. They use an absolute `source_spec` path and have no owner or reopen trigger. [_bmad-output/implementation-artifacts/deferred-work.md:5966]
- [ ] [Review][Patch] The approved Builds SHA appears in two wrapper constants and three workflow sites, and no consistency test covers them. Add an assertion that both constants equal the `release.yml` pin. [tools/check-release-source.py:13]
- [ ] [Review][Patch] Three dead release step-name mappings remain: "Audit root release tooling", "Test unit and non-Docker suite" and "Run release package validator fixtures". [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1825]
- [x] [Review][Defer] The rewrite of `epic-30-context.md` dropped binding constraints, including the Zot 401 re-check, one immutable Builds revision, never `@main`, `cancel-in-progress: false` and runbook section ownership [_bmad-output/implementation-artifacts/epic-30-context.md:1] — deferred: agent-context file; regenerate it from `epics.md`.
- [x] [Review][Defer] The NuGet absence probe is eventually consistent (flat-container 404 while a package is still validating), and `--skip-duplicate` treats HTTP 409 as pushed [tools/publish-nuget.ps1:159] — deferred: pre-existing pinned-helper and publisher semantics, already tracked by Round 2 Blind 5 for Story 30.5.

Code-review phase-ledger evidence (2026-11-02). The canonical `code-review` row cannot be appended yet. No `## Change Log` table exists, and rows are chronological and append-only, so the create-story and dev-story rows must come first. That is the ledger action item above.

- **Review-patch phase delta:** +0 in every lane; no patch was applied.
- **Test methods at `46667b0d` → HEAD:**

  | Lane | Before | After | Change |
  | :--- | -----: | ----: | -----: |
  | `release_dispatch` | 0 (scope absent) | 9 | +9 |
  | `release_preflight` | 45 | 49 | +4 |
  | `publish_nuget` | 31 | 32 | +1 |
  | `publish_containers` | 43 | 45 | +2 |
  | `release_packages` | 28 | 28 | +0 |
  | `builds_publication_qualification` | 70 | 70 | +0 |
  | `ci_alignment` | 12 | 12 | +0 |

- **Baseline command:** `git archive 46667b0d tests/tooling tools`, then `unittest` loader discovery with `-p '*_test.py'` per lane.
- **HEAD command:** `python3 -m unittest discover -s tests/tooling/<lane> -p '*_test.py'`; every lane passed.
- **`CiTestInventoryTests`:**
  - HEAD: 137 test cases (runner Total), from `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` after a Debug `--no-restore -m:1` build.
  - Baseline: blocked. It needs a build at `46667b0d` in a detached worktree.
  - Owner: Memories Release Engineer.
  - Consequence: the cumulative delta for this unit is unproven.
  - Reopen trigger: the ledger action item.
- **File List reconciliation:** not matched, 0/31 against `git diff --name-status 46667b0d..HEAD` (28 M, 3 A). `tools/publish-release.ps1` and `release_orchestration_test.py` were restored to baseline and leave the set.
- **Chunks:** the review was not chunked, so every in-scope chunk is complete.
- **Status:** `in-progress`. Fail closed: patches are action items, and the readiness and slice-scope gates exit 1.
- **Post-sync gate re-run:** the same `--changed-files-file` invocation exited 1 with `C1: a non-empty File List is required for this governed artifact.` C4 cleared with the status sync, and C6 passes now that the story is not `review` or `done`.

Rejected:

- `false` — the NU1901–1904 override removal makes release diverge from CI. Shared CI (`domain-ci.yml@6e7011fc:422-426`) restores without the override and builds `-warnaserror`, exactly like `prepare-domain-release`.
- `false` — a frozen dispatch validates nothing. Exact-source CI runs `verify:semantic-release-config` (`ci.yml:300`), and `release_packages` validates the canonical inventory for that SHA.
- `false` — the `historical_bytes()` CRLF special case. Its inputs are fixed historical revisions, so the blob bytes cannot change.
- `false` — a dirty Builds checkout. The job checks it out fresh, and only trusted `main` code runs before the wrappers.
- `false` — a `HEXALITH_RELEASE_CLASSIFICATION_ONLY` leak through `-ceq`/`-eq`. Only `release-preflight.ps1:90-101` sets the variable, for its child dry-run, and it is restored afterwards.
- `low` — fixtures for the classification early exit and for the wrapper `live_sha` mismatch. These are unlikely regressions behind the pinned helper, and fixing them means additive tests rather than a correction.
- `low` — source fixtures stub the pinned helper. `81c88b34` is approved with 158 upstream fixtures, and a real-helper fixture needs Builds objects that are not in CI.
- `false`/`low` — the destination wrapper calls private `_load_package_identity` (the pinned bytes are immutable). The new wrappers are not in `FORBIDDEN_DEFAULT_GLOBS`, but changing that would be a governance-policy change across all stories.
- `low` — the five-image runbook text sits in a section Story 30.4 owns. The text matches the current five-archive publisher, so only the ownership label is affected.

## Verification

- Passed after review fixes: `release_dispatch` (7/7); `release_packages` (28/28); `release_preflight` (48/48); `publish_nuget` (32/32); `publish_containers` (46/46); qualification `ReleaseSetTests` (6/6) and `CommittedEvidenceTests` (12/12); `actionlint` on the three changed workflows; `pwsh -NoLogo -NoProfile -File tools/validate-release-packages.ps1`; `npm run verify:semantic-release-config --silent`; built `CiTestInventoryTests` (137/137). The pinned preparation commands `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release` and `dotnet build Hexalith.Memories.slnx --no-restore --configuration Release -warnaserror` passed, with zero build warnings.
- After integrating remote qualification corrections, `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py'` passed 70/70; `ci_alignment` passed 12/12, and the built `CiTestInventoryTests` passed 137/137. The earlier 60-test run failed on the then-current profile mismatch and is preserved in review history above.
- Live readback on 2026-11-02: `gh api repos/Hexalith/Hexalith.Memories/environments/production` returned `protection_rules: []`, `deployment_branch_policy: null`; the then-main SHA `637eaa1b14b9251d45b7b44ed8644f2b2a9a0e55` had no completed successful push `ci.yml` run. Production approval and successful exact-source CI for a new tip are required before live publication acceptance.
- Follow-up review passed: `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py'` (9/9), `python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py'` (49/49), `python3 -m unittest discover -s tests/tooling/publish_containers -p 'release_orchestration_test.py'` (5/5), `npm run verify:semantic-release-config --silent`, `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug --no-restore -m:1` (zero warnings), and `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` (137/137). `git diff --check` passed.
- Follow-up live readback on 2026-11-02: `production` still has `protection_rules: []` and `deployment_branch_policy: null`. Local and remote main both point to `8aee7a1323160cb39d7b1415256dbfdcb032939a`; its push `ci.yml` run `41452784260` was queued, with no completed successful exact-source CI at readback. No release was dispatched.
