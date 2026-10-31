---
title: 'Shared CI core and retained Memories verification lanes'
type: 'chore'
created: '2026-10-31'
status: 'done'
workflow_status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-1-shared-ci-core-and-module-specific-verification-lanes'
baseline_commit: '00919bdd6fe97284da5f4f1657da4a5b336867cc'
context: ['{project-root}/AGENTS.md', '{project-root}/_bmad-output/implementation-artifacts/30-1-ci-adoption-notes.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI duplicates standard build mechanics locally, while the Epic 30 registry contradicts its approved execution sequence.

**Approach:** Adopt the pinned shared Release build and compatible Contracts test shard, preserve specialized verification locally, and reconcile six-story ordering with historical aliases.

## Boundaries & Constraints

**Approved 2026-10-31:** The user selected shared CI first; preserve ten packages/five images; later guarded release uses pinned prepare-domain-release with caller-owned Trusted Publishing. Numbering reconciliation and future guarded scope are planning changes here, not completed release work.

**Always:** Keep exact build, test-unit-contract and integration-fast required-check names. Build passes only when shared-ci and source-debug both succeed. Retain seven-project Coverlet coverage at 76.5%, package topology, tenant-negative/scope gates, web E2E, fast integration, deployment, SQL, nightly benchmarks, explicit PR-title commitlint and existing failure artifacts. Use full Builds SHA 81c88b34d09d34260b92d7f0928dbbc1c7446baa. Declare Microsoft.Testing.Platform and explicit project selections. Keep Contracts coverage locally until equivalence is proved; its small test overlap is intentional.

**Never:** Introduce publication, container cutover, new collectors, source dependency changes, governed provenance machinery or remote operations. No existing validation may become advisory.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| PR/main push | Shared Release build and Contracts shard; existing local gates |
| Shared or source verification fails, skips or cancels | Stable build check fails |
| All build dependencies succeed | Stable build check succeeds |
| Wrong SHA/platform/project list or bypassed required gate | Regression fixtures reject configuration |
| Source catalog absent/empty/failed/skipped | Source lane fails; XML retained |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml`: current combined build plus specialized lanes; shared workflow has no integration-exclusion input.
- `references/Hexalith.Builds/.github/workflows/domain-ci.yml`: pinned standard Release restore/build, Contracts MTP reporting; coverage/consumer conventions do not match Memories yet.

## Tasks & Acceptance

The [canonical lane-by-lane migration and rollback table](30-1-ci-adoption-notes.md#lane-migration-and-rollback) is incorporated here as this story’s implementation evidence.

- [x] `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/epic-30-context.md` — reconcile order, aliases and approved inventory/authentication decisions without claiming later prerequisites.
- [x] `.github/workflows/ci.yml` — add shared-ci and source-debug, retain stable aggregate build check and local lanes.
- [x] `tests/tooling/ci_alignment/`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `tests/tooling/story_review_readiness/readiness_entrypoints_test.py` — verify matrix, exact pin, audit ownership and retained mandatory checks; wire fixtures into CI.
- [x] `CONTRIBUTING.md`, `docs/dev/branch-protection.md`, `docs/dev/release-runbook.md`, `tests/README.md` — record lane migration, retained coverage/consumer exceptions, check/artifact identities and rollback. Preserve historical settings evidence.
- [x] Verify focused tooling, lint and project checks.

**Acceptance Criteria:**
- Given a PR or main push, when standard CI runs, then approved immutable shared code owns Release build and the explicit compatible test shard.
- Given module-specific evidence, when adoption occurs, then every current gate remains blocking with equivalent commands and downloadable artifacts.
- Given branch protection using build, when either build dependency is unsuccessful, then the required status fails without remote settings changes.
- Given approved ordering, when plans are read, then 30.1 means shared CI; aliases and later prerequisites remain.

## Historical Context Classification

| Source | Classification | Permitted use |
| --- | --- | --- |
| Former Story 30.2 | historical-reference-only | CI outcome, corrected to new 30.1 |
| Former Story 30.1 | historical-reference-only | Guarded-release intent moved to 30.3 |
| Story 27.3 | anti-template | Preserve deployment boundary only |

## Slice Proof

One maintainer outcome: consistent shared build without losing Memories verification. Later release and recovery implementation retain separate owners.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml`
- `tests/tooling/ci_alignment/**`
- `tests/tooling/story_review_readiness/readiness_entrypoints_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `CONTRIBUTING.md`
- `docs/dev/branch-protection.md`
- `docs/dev/release-runbook.md`
- `tests/README.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/epic-30-context.md`
- `_bmad-output/implementation-artifacts/30-1-ci-adoption-notes.md`
- `_bmad-output/implementation-artifacts/30-1-shared-ci-core-and-module-specific-verification-lanes.md`
- `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`

## Cross-Tenant Negative Evidence

CI/configuration only; existing tenant-negative evidence gates remain mandatory.

## Checkpoint Evidence

Evidence verifies implementation locally; live workflow execution remains a separate adoption check.

| Gate | Owner | Evidence | Review state | Completion |
| --- | --- | --- | --- | --- |
| Immutable pin | Maintainer | ci_alignment identity cases | reviewed 2026-10-31 | complete 2026-10-31 |
| MTP selection | Maintainer | Contracts TRX: 596 passed | reviewed 2026-10-31 | complete 2026-10-31 |
| Local gates retained | Maintainer | ci_alignment gate mutations | reviewed 2026-10-31 | complete 2026-10-31 |
| Coverage retained | Maintainer | coverage_gate: 36 passed | reviewed 2026-10-31 | complete 2026-10-31 |
| Source lane retained | Maintainer | Actual source catalog: three passed; negative XML scenarios | reviewed 2026-10-31 | complete 2026-10-31 |
| Aggregate result | Maintainer | ci_alignment: 49 combinations | reviewed 2026-10-31 | complete 2026-10-31 |
| Commitlint retained | Maintainer | commitlint: 13 passed | reviewed 2026-10-31 | complete 2026-10-31 |
| Artifacts retained | Maintainer | ci_alignment upload assertions | reviewed 2026-10-31 | complete 2026-10-31 |
| Check names/rollback | Maintainer | Lane map; CLI inventory: 136 passed | reviewed 2026-10-31 | complete 2026-10-31 |

## Implementation Notes

Approved decisions applied; canonical filename supports scope tooling. Evidence: context notes. Repository story/sprint statuses are synchronized; automatic staging/committing is omitted under the user’s AGENTS.md baseline.

## Spec Change Log

## Review Triage Log

All three review layers completed. Edge-case and verification-gap reviewers returned no findings. Blind-review findings were individually verified before grouping:

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| B1 source report execution proof | medium | The new positive fixture and retained checker accept total=3 with no passed count; a contradictory passed=0 report can also pass. Patch the report check and scenario fixtures with explicit successful-count consistency. |
| B2 no-op critical commands | medium | assert_adoption checks names/conditions for tenant-negative and SQL gates but accepts run=true. Patch command assertions and replacement mutations. |
| B3 omitted mandatory fixture steps | medium | Lifecycle fixture steps are outside the new critical inventory and can become advisory undetected. Patch the retained mandatory fixture inventory and conditions. |
| B4 uploader substitution | medium | Existing artifact assertions accept a checkout action carrying upload-shaped inputs. Patch exact approved uploader identities and substitution mutations. |
| B5 trigger exclusions | medium | Adding paths-ignore=['**'] leaves the new trigger assertions green. Patch PR/push filter rejection and mutation scenarios. |
| B6 source lane execution evidence | medium | Only synthetic source-report cases were run initially. Execute actual Debug/source restore/build/catalog checks and record the result distinctly. |
| B7 Coverlet evidence overstatement | medium | Epic row cites generated reports/verdict although this run proved retained configuration and 36 validator fixtures. Correct its evidence to configuration preservation; do not claim fresh measured coverage. |
| B8 artifact download evidence | low | No new fixture downloads an artifact; it validates upload identity/configuration and local XML preservation. Directly correct the evidence label. |
| B9 future guarded checkpoints | medium | Future draft has four combined rows while its reconciled epic defines ten. Align the draft rows without granting implementation/qualification credit. |
| B10 lane table incorporation | low | The required table is loaded through frontmatter context but lacks explicit body incorporation. Add a direct canonical-table link to satisfy repository evidence policy without duplicating the table. |

## Verification

Release and Debug/source builds passed with zero warnings/errors; 990 focused cases passed (255 Python, 136 CLI, 596 Contracts, three source catalog). Actionlint and repository scope/slice/readiness gates passed. Live CI pending.

## File List

- `.github/workflows/ci.yml`
- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/30-1-ci-adoption-notes.md`
- `_bmad-output/implementation-artifacts/30-1-shared-ci-core-and-module-specific-verification-lanes.md`
- `_bmad-output/implementation-artifacts/epic-30-context.md`
- `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `docs/dev/branch-protection.md`
- `docs/dev/release-runbook.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/README.md`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `tests/tooling/story_review_readiness/readiness_entrypoints_test.py`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| --- | --- | --- | --- | --- |
| 2026-10-31 | create-story | Approved bounded CI scope; ledger initialized before review. Baseline observations were reproduced from canonical source snapshots after implementation, not claimed as earlier execution. | Phase +0, cumulative +0. Baseline Python unittest cases: 244 across the six named suites; CLI class discovery: 68 deferred-theory methods; affected action-policy theory: 15 executed cases. Exact reproducible discovery commands and snapshot build are in context notes. | matched 15/15 current cumulative paths against 00919bdd6fe97284da5f4f1657da4a5b336867cc plus untracked files, using /tmp/memories-30-1-changed-files.txt; creation/planning paths included, no exclusions. |
| 2026-10-31 | correct-course | Applied user-approved numbering, ten-package/five-image preservation and later OIDC composite choice; reconciled D17. | Phase +0 Python cases and +0 CLI methods/action-policy cases; cumulative +0 at planning boundary. Planning changes do not add tests. Same source-snapshot discovery scopes as create-story. | matched 15/15 cumulative paths from canonical baseline plus untracked files; active aliases preserve historical references. |
| 2026-10-31 | dev-story | Shared Release/Contracts caller, source-debug and stable aggregate implemented; specialized gates preserved. | Python unittest cases 244 -> 252: phase/cumulative +8 (ci_alignment 0 -> 7; readiness 104 -> 105; coverage 36, scope 71, slice 20 and commitlint 13 unchanged). CLI discovery methods 68 -> 68: phase/cumulative +0; action-policy execution cases 15 -> 19: phase/cumulative +4. Current whole CLI class: 136 passed; Contracts MTP: 596 passed. Exact commands/evidence in context notes; units are not mixed. | matched 15/15 using git diff --name-only 00919bdd6fe97284da5f4f1657da4a5b336867cc and git ls-files --others --exclude-standard; no exclusions. |
| 2026-10-31 | code-review | Three independent layers completed; verified each finding and patched commands, triggers, uploads, report integrity and evidence prose. No deferred item; all in-scope review chunks complete. | Review phase +3 Python unittest cases (ci_alignment 7 -> 10); cumulative +11: 244 -> 255. CLI methods +0/cumulative +0 (68); action-policy cases +0/cumulative +4 (15 -> 19), current whole class 136 passed. Existing Contracts scope 596 passed and newly executed existing source-catalog scope three passed; no added-test delta claimed for that scope. Evidence: final-python.log, final-cli.xml and actual source-catalog workflow command in context notes. | matched 15/15 current cumulative paths against 00919bdd6fe97284da5f4f1657da4a5b336867cc plus untracked files; no exclusions. |

### Review Resolution Evidence

All three independent layers completed; all ten blind findings were addressed with no deferred items.

| Finding | Resolution |
| --- | --- |
| B1 | Explicit counters and actual Pass records now reconcile; valid, incomplete, contradictory and failed-report scenarios pass. Actual source workflow command also passes all three real catalog tests. |
| B2 | Literal mandatory-command inventory and replacement-with-true mutations pass. |
| B3 | All retained local tooling fixture commands are in the mandatory inventory; deletion/conditional/advisory mutations pass. |
| B4 | Exact uploader identities are checked; checkout substitutions are rejected. |
| B5 | PR/main-push trigger mappings are exact; path/branch/tag/type exclusions are rejected. |
| B6 | Actual Debug/source restore and solution build passed, zero warnings/errors; exact current catalog workflow command passed three tests. |
| B7 | Epic evidence explicitly records configuration preservation and validator fixtures, without claiming a new coverage collection/verdict. |
| B8 | Epic evidence records uploader identity/retention assertions and local XML preservation, without claiming downloads. |
| B9 | Future guarded draft now carries all ten canonical checkpoint rows, still pending behind qualification. |
| B10 | This story explicitly incorporates the linked canonical lane migration/rollback table. |

