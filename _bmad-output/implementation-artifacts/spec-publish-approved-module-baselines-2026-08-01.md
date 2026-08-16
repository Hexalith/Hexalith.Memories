---
title: 'Authorize approved module baseline publication'
type: 'build'
created: '2026-08-01'
status: 'done'
baseline_commit: 'b9ae7b9d11a11cd239ff5aef7c552cd393da7b99'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md'
---

<frozen-after-approval reason="human-owned publication intent — do not modify unless the Administrator renegotiates">

## Intent

**Problem:** The Administrator-approved correct-course proposal and the already-published Builds and
FrontComposer root gitlink revisions form one pending parent-repository snapshot, but the repository
commit gate requires one exact standalone owner for the aggregate changed-file set.

**Approach:** Authorize exactly one parent commit containing this ownership envelope, the approved
proposal, and the two root gitlink updates. This spec owns only the publication envelope; it does not
claim completion of Epic 28, Story 28.1, Story 28.2, or the FrontComposer verification route.

## Boundaries & Constraints

**Always:** Keep the original publication envelope equal to exactly its four paths: this spec, the
approved proposal, `references/Hexalith.Builds`, and `references/Hexalith.FrontComposer`. Keep Builds
at `10af541e7b2a5a4664be37c9495930844e0954a8` and FrontComposer at
`a746cde4bd128399522f895a7ac7f077c4ee64da`; require both commits to be reachable from their fetched
`origin/main`; preserve all other root gitlinks; validate the message before and after commit; push
only a fast-forward update of root `main`. Keep the later validator implementation within this spec,
`tools/check-approved-publication.py`, `tests/tooling/approved_publication/approved_publication_test.py`,
`.github/workflows/ci.yml`, and the workflow-owned `_bmad-output/implementation-artifacts/deferred-work.md`
review entries. The expanded eight-path File Scope is an implementation allow-list, not a redefinition
of the historical four-path publication envelope. The validator must be read-only and must not fetch,
stage, commit, push, or rewrite refs.

**Ask First:** Any path addition, removal, or rename beyond the eight paths in File Scope; any approved
OID change; any remote divergence outside the validator's fail-closed test fixtures; any merge,
rebase, force push, remote mutation, or broader ownership claim.

**Never:** Edit submodule content, initialize or update nested submodules, bypass hooks, use a scope
override, rewrite history, merge or prune branches, move the EventStore gitlink, mutate a real
remote while validating, or represent this publication as implementation evidence for the approved
Epic 28 split.

## Ownership Partition

- The correct-course workflow retains ownership of the approved planning proposal.
- Story 28.2 retains implementation and evidence ownership for EventStore `3.89.0` package adoption.
- The independent FrontComposer route retains its focused final-tree evidence obligation.
- This spec owns only the four-path commit and fast-forward push envelope.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md`
- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`
- `tools/check-approved-publication.py`
- `tests/tooling/approved_publication/approved_publication_test.py`
- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Exact approved snapshot | The four publication paths are staged at the approved content and OIDs | Scope, evidence, readiness, whitespace, commitlint, exact-gitlink, root-fast-forward, and submodule-reachability gates pass | Stop before commit or push on any failure |
| Snapshot drift | A staged path or gitlink OID differs | Scope or publication preflight validation fails closed | Preserve the index and request renewed authorization |
| Root remote movement | `origin/main` advances before push | Publication preflight rejects the stale or divergent candidate; no push is attempted | Stop and report the divergence |
| Missing submodule commit | A gitlink OID is missing or is not reachable from its fetched `origin/main` | Publication preflight rejects the unsafe parent snapshot | Stop without committing or pushing the parent snapshot |

</frozen-after-approval>

## Tasks & Acceptance

**Execution:**

- [x] Record the Administrator's authorization for the exact four-file publication envelope.
- [x] Pass repository scope, evidence, readiness, whitespace, and commit-message gates.
- [x] Commit the exact snapshot and push root `main` as a fast-forward update.
- [x] Add and verify the read-only publication preflight validator and its durable matrix tests.

**Acceptance Criteria:**

- Given the exact staged snapshot, when the story-file-scope gate runs, then all four original
  publication-envelope paths resolve to this standalone owner without an override.
- Given the two gitlink revisions, when reachability is checked after fetching their remotes, then
  each revision is an ancestor of its corresponding `origin/main`.
- Given a validated local commit and unchanged fetched root remote, when `main` is pushed, then local
  and remote heads match the new commit without force.
- Given any gate failure or remote movement, when publication cannot proceed safely, then no bypass,
  history rewrite, merge, or branch pruning occurs.

## Design Notes

The commit message uses this owner trailer:

```text
build(deps): adopt approved module baselines

Record the correct-course proposal for EventStore source and package
identities.

Advance Hexalith.Builds to expose EventStore 3.89.0 and synchronize
Hexalith.FrontComposer with its completed governance update.

Story-Key: spec-publish-approved-module-baselines-2026-08-01
```

## Verification

Historical four-path publication envelope, reconstructed at baseline
`b9ae7b9d11a11cd239ff5aef7c552cd393da7b99` when necessary:

- `git diff --cached --name-only` equals exactly this spec, the approved proposal,
  `references/Hexalith.Builds`, and `references/Hexalith.FrontComposer`; it is not compared with the
  later expanded File Scope.
- `git diff --cached --check`, the story-scope, tenant-evidence, and review-readiness staged gates,
  `npx commitlint --edit <message-file> --verbose`, and `.githooks/commit-msg <message-file>` pass.
- Pre-commit validation runs while `HEAD` and fetched `origin/main` both equal the approved baseline:
  `python3 tools/check-approved-publication.py --snapshot index --root-remote-ref origin/main
  --expected-root-remote-oid b9ae7b9d11a11cd239ff5aef7c552cd393da7b99 --candidate-ref HEAD
  --expected-path _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
  --expected-path _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md
  --expected-gitlink references/Hexalith.Builds=10af541e7b2a5a4664be37c9495930844e0954a8
  --expected-gitlink references/Hexalith.FrontComposer=a746cde4bd128399522f895a7ac7f077c4ee64da`.
- Post-commit validation runs before push with unchanged fetched `origin/main`, `HEAD` at the new
  commit, and `--snapshot HEAD`; the remaining arguments are identical to the pre-commit invocation.
- `npx commitlint --from b9ae7b9d11a11cd239ff5aef7c552cd393da7b99
  --to e7e4045fd8ad2a25c3f3bf7bde3cd88403a96273 --verbose` passes.

Expanded validator implementation scope:

- `python3 -m py_compile tools/check-approved-publication.py
  tests/tooling/approved_publication/approved_publication_test.py` passes.
- `python3 tests/tooling/approved_publication/approved_publication_test.py -v` passes every staged,
  committed-tree, exact-delta, root-safety, registration, remote-identity, missing-commit, and
  reachability fixture.
- `python3 tests/tooling/story_scope/story_scope_validator_test.py -v` passes.
- `dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll
  -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.CiWorkflow_RunsEveryToolingFixtureSuiteThatGuardsAShippedTool`
  passes after `.github/workflows/ci.yml` wires the suite.
- Story-scope validation uses the current changed-path set: this spec,
  `tools/check-approved-publication.py`, `tests/tooling/approved_publication/approved_publication_test.py`,
  `.github/workflows/ci.yml`, and `_bmad-output/implementation-artifacts/deferred-work.md`.
- The tenant-evidence gate, review-readiness gate, and `git diff --check` pass for that same current
  changed-path set.

## Suggested Review Order

**Publication validation**

- Central orchestration binds exact deltas, root safety, gitlinks, and registered submodules.
  [`check-approved-publication.py:436`](../../tools/check-approved-publication.py#L436)

- Root checks distinguish staged pre-commit safety from committed fast-forward publication.
  [`check-approved-publication.py:262`](../../tools/check-approved-publication.py#L262)

- Exact-set checks reject missing, extra, or unrelated publication paths.
  [`check-approved-publication.py:249`](../../tools/check-approved-publication.py#L249)

- Submodule identity couples registered remotes with reachable approved commits.
  [`check-approved-publication.py:358`](../../tools/check-approved-publication.py#L358)

**Contract and evidence**

- Frozen constraints separate the historical envelope from its later validator implementation.
  [`spec-publish-approved-module-baselines-2026-08-01.md:24`](spec-publish-approved-module-baselines-2026-08-01.md#L24)

- Verification records reproducible pre-commit, post-commit, and implementation checks.
  [`spec-publish-approved-module-baselines-2026-08-01.md:114`](spec-publish-approved-module-baselines-2026-08-01.md#L114)

- Confirmed historical review findings remain explicitly deferred from this implementation.
  [`deferred-work.md:3349`](deferred-work.md#L3349)

**Regression coverage**

- Staged success proves the validator preserves index, worktree, HEAD, and refs.
  [`approved_publication_test.py:232`](../../tests/tooling/approved_publication/approved_publication_test.py#L232)

- Post-commit success proves a local descendant remains safely publishable.
  [`approved_publication_test.py:244`](../../tests/tooling/approved_publication/approved_publication_test.py#L244)

- CI discovery keeps publication fixtures in the required tooling lane.
  [`ci.yml:279`](../../.github/workflows/ci.yml#L279)
