---
title: 'Guarded release dispatch and shared caller adoption'
type: 'chore'
created: '2026-10-31'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-3-guarded-release-dispatch-and-shared-caller-adoption'
baseline_commit: '00919bdd6fe97284da5f4f1657da4a5b336867cc'
context: []
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

- `.github/workflows/ci.yml`: shared CI adoption is implemented by 30.1; local validation is recorded there and live workflow execution remains separate.
- `.github/workflows/release.yml`: protected push publisher with OIDC and retained evidence.
- `tools/release-packages.json`: ten entries; current schema/readers use packageId.
- `.releaserc.json`: local pack/publish hooks; current NuGet skip-duplicate cannot prove destination absence.
- `references/Hexalith.Builds/Github/prepare-domain-release/README.md`: supported OIDC-preserving seam at 81c88b34d09d34260b92d7f0928dbbc1c7446baa; reuse publication_preflight.py without migrating container ownership.

## Tasks & Acceptance

Contingent on resolved scope and prerequisites:

- [ ] `.github/workflows/release.yml`, `tools/check-release-source.py` — dispatch/source gate, approved pinned preparation, protected production job, final source/identity/destination proof; retain evidence/hooks.
- [ ] `tools/release-packages.json`, `tools/release-packages.schema.json`, `tools/validate-release-packages.ps1`, `tools/pack-release.ps1` — migrate current inventory to id without membership changes.
- [ ] `tools/complete-partial-release.ps1`, `.github/workflows/recover-partial-release.yml` — preserve historical packageId input; stage required approved manifest beside recovery script; retain summary DTO fields.
- [ ] `.releaserc.json`, `tools/publish-nuget.ps1` — wire shared prepublication proofs without replacing publishers.
- [ ] `tests/tooling/release_dispatch/`, `tests/tooling/release_packages/`, `tests/tooling/publish_containers/`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` — cover refusal branches, migration, recovery, and replace legacy workflow assertions deliberately.
- [ ] `docs/dev/release-runbook.md`, `CONTRIBUTING.md` — document dispatch, protection evidence, inventory, pin and rollback.

**Acceptance:** Given approved scope and prerequisites, when dispatch passes all proofs, then only protected production can publish. Given green exact-source push CI, when release prepares, then tests are not duplicated. Given manifest migration, when packing/recovery runs, then identities and historical recovery remain intact.

## Checkpoint Evidence

Planned artifacts; none completed.

| Checkpoint | Owner | Evidence command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Non-tip dispatch rejection | Memories Release Engineer | Guarded-dispatch non-tip fixture and preflight result | pending | not complete |
| Exact-source push CI proof | Memories Release Engineer | Candidate SHA-bound CI proof and missing-CI refusal fixture | pending | not complete |
| Protected production approval | Memories Release Engineer | Environment protection readback and caller configuration | pending | not complete |
| Matching pinned Builds identity | Memories Release Engineer | Full-SHA composite caller and mismatch-refusal fixture | pending | not complete |
| No publication on push | Memories Release Engineer | Trigger and publication-reachability fixtures | pending | not complete |
| OIDC publication retained | Memories Release Engineer | Trusted Publishing caller and hook-preservation fixtures | pending | not complete |
| No duplicate application tests | Memories Release Engineer | Preparation/caller test-invocation fixtures | pending | not complete |
| Atomic ten-package migration | Memories Release Engineer | Release-package validator and unchanged-membership fixtures | pending | not complete |
| Historical manifest recovery | Memories Release Engineer | Tagged `packageId` recovery compatibility fixtures | pending | not complete |
| Destination absence refusal | Memories Release Engineer | Occupied and ambiguous destination negative fixtures | pending | not complete |

## Historical Context Classification

| Source | Classification | Permitted use |
| --- | --- | --- |
| Story 30.1 and Story 30.2 | historical-reference-only | Shared CI and local qualification prerequisites |
| Story 27.3 | anti-template | Deployment boundary only; no scope copying |

## Slice Proof

One outcome: verified intentional publication. CI alignment, container qualification, recovery redesign and cutover keep separate ownership.

## Implementation Notes

## Spec Change Log

- 2026-10-31: User approved the recommended numbering, inventory and authentication decisions. Moved former 30.1 planning to 30.3; implementation remains deferred behind its prerequisites.

## Review Triage Log

## Verification

Python unittest discovery (`-p '*_test.py'`) individually under tests/tooling/release_dispatch, release_packages, release_preflight, publish_nuget and publish_containers; canonical inventory validator; npm run verify:semantic-release-config; actionlint on changed workflows; built CLI assembly with -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests. Verify external protection separately.

Baseline: 28 package and 45 preflight fixtures passed; slice-scope validator passed. No application code or workflow changes.
