---
title: 'Align MCP dependencies after branch merge'
type: 'chore'
created: '2026-07-10'
status: 'done'
baseline_commit: 'dbcd3ceb4242880ef9e0f4f3b05d35f5bcd06cf1'
review_loop_iteration: 5
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The merged Dependabot branch upgrades `ModelContextProtocol.AspNetCore` to 1.4.1 but leaves the directly referenced `ModelContextProtocol` package at 1.4.0. NuGet therefore reports `NU1605` as an error for the MCP and integration test projects, preventing restore and validation of the merged branch.

**Approach:** Align the centrally managed core MCP package with the merged ASP.NET Core package at 1.4.1, validate the affected build and test surface, then complete the requested branch pruning without rewriting published `main` history.

## Boundaries & Constraints

**Always:** Keep both MCP packages on the same version in `Directory.Packages.props`; preserve central package management; use the `.slnx` solution for restore/build and run tests by project; use a Conventional Commit validated by the repository's commitlint configuration; preserve all existing remote history.

**Ask First:** Any package version other than 1.4.1; source-code changes required to accommodate the package update; force-pushing or rewriting published history; deleting any branch not proven incorporated by ancestry or equivalent patch content.

**Never:** Add versions to project files; modify submodule contents or pointers; initialize nested submodules; suppress `NU1605` or warnings-as-errors; broaden the change to unrelated dependency updates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Aligned dependency resolution | Both MCP packages are centrally pinned to 1.4.1 | Solution restore resolves MCP packages without a downgrade | Build and focused tests proceed |
| Unexpected compatibility regression | Restore succeeds but build or MCP tests fail against 1.4.1 | Do not mask or suppress the failure | Stop before pruning and report the exact failing command |
| Published merge history | Remote `main` already contains the Dependabot merge tip | Add a forward fix without rewriting history | Force-push is forbidden |

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` -- central versions for `ModelContextProtocol` and `ModelContextProtocol.AspNetCore`.
- `_bmad-output/project-context.md` -- durable MCP SDK version and alignment invariant used by implementation agents.
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` -- production consumer of the ASP.NET Core MCP package.
- `tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj` -- focused consumer of both MCP packages and primary compatibility test lane.
- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` -- direct core MCP consumer that exposed the downgrade during solution restore.

## Tasks & Acceptance

**Execution:**
- [x] `Directory.Packages.props` -- update `ModelContextProtocol` from 1.4.0 to 1.4.1 and update the adjacent SDK-version comment from 1.2.0 to 1.4.1 so package metadata remains internally consistent.
- [x] `_bmad-output/project-context.md` -- update the authoritative MCP SDK version from 1.4.0 to 1.4.1 while preserving the package-alignment invariant and its existing CRLF convention.
- [x] This spec -- preserve repository CRLF formatting before staging and verify it at byte level.
- [x] `Hexalith.Memories.slnx`, `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`, and `tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj` -- attempt package-mode solution validation, record the committed-pointer blocker, run focused Release restore/build fallback for the affected production/test projects, run 90 MCP unit tests, and run the two named MCP integration classes.

**Acceptance Criteria:**
- Given the merged branch on `main`, when the solution restores, then no `NU1605` downgrade is reported for any MCP consumer.
- Given the clean-checkout solution blocker, when the affected MCP production and test projects restore/build in Release package mode, then both complete with warnings treated as errors.
- Given the aligned MCP packages, when the MCP test project runs, then all tests pass.
- Given the aligned MCP packages, when `McpAuthenticationIntegrationTests` and `McpServerIntegrationTests` run, then all focused MCP client/server compatibility tests pass.
- Given repository version guidance, when the dependency change is inspected, then both the adjacent package comment and project context identify MCP SDK 1.4.1.

## Post-Review Delivery

- [ ] Staging -- assert current branch `main`; hash the three reviewed worktree files; stage only them; mechanically assert the exact cached path set, cached blob equality, no same-file unstaged remainder, byte-level CRLF compliance, and a clean CRLF-aware cached diff.
- [ ] Commit -- snapshot the expected index tree; validate a temporary Conventional Commit message; commit; assert the committed tree, path set, and blobs equal the reviewed index; validate the created commit; fetch `origin/main`; and validate the full outgoing range.
- [ ] Git refs -- define the exact Dependabot branch literal and capture the exact forward-fix OID; query the server directly. If the branch is absent, publish that exact OID to `main`; if present, prove its captured OID is incorporated and atomically publish the exact fix OID plus lease-delete only that captured tip. The lease option is authorized solely as a deletion guard and must never force-update `main`. Prune and assert synchronized `main` plus branch absence at local, tracking, and server scopes.
- Given validation and a compliant forward-fix commit, when branch cleanup completes, then local `main` matches `origin/main`, the incorporated Dependabot branch is absent locally and remotely, and any unrelated branch remains untouched.

## Spec Change Log

- Review iteration 1: Edge Case Hunter found that the integration-test project directly consumes `ModelContextProtocol` but the verification plan exercised only the MCP unit project. Added the integration test lane and its acceptance criterion to avoid a runtime compatibility regression that still compiles. KEEP the exact one-line central package alignment, the already successful restore/Release build/90-test MCP lane, strict staging isolation, and the prohibition on touching concurrent loop-skill or FrontComposer changes. Also narrowed post-review deletion wording to the proven-incorporated Dependabot branch.
- Review iteration 2: Blind Hunter found stale MCP version guidance and an incomplete Git delivery checklist. Added the adjacent comment and project-context updates, exact staging assertions, pre/post/full-range commitlint checks, ancestry proof, explicit remote deletion, and post-delete pruning. This avoids publishing misleading version instructions, accidentally staging concurrent work, or claiming pruning without deleting the remote branch. KEEP the 1.4.1 package alignment, successful restore/build/90-unit/6-integration evidence, the recorded broad-suite blocker, exact Dependabot-only deletion, and all unrelated-work prohibitions. This entry supersedes iteration 1's exact-one-line KEEP only for the two version-documentation updates required to keep authoritative guidance accurate.
- Review iteration 3: Reviewers found same-file staging gaps and a remote-ref check/use race. Added CRLF preservation, cached-content and no-unstaged-hunk checks, a fresh captured branch OID, atomic main publication plus lease-protected deletion, and explicit final ref assertions. This avoids committing concurrent hunks, deleting a branch that advanced after review, deleting before the fix is published, or claiming cleanup without verifying every relevant ref. KEEP all validated 1.4.1 behavior/evidence, accurate version guidance, the broad-suite blocker, exact three-file scope, and untouched concurrent work.
- Review iteration 4: Reviewers observed that the remote Dependabot branch had already disappeared and that prior checks were still partly prose. Added an idempotent server-absent path, literal branch assignment, direct `ls-remote` capture, exact worktree/index/commit blob and tree binding, executable path-set and final-ref assertions, exact-OID publication, local-branch safety, and byte-level CRLF checks. Narrowed integration acceptance to the two focused MCP classes and recorded the broad timeout as unresolved rather than proven unrelated. This avoids blocking publication on already-complete cleanup, stale tracking-ref reads, concurrent index/ref drift, or overstating broad-suite evidence. KEEP all prior validated package behavior, focused tests, accurate guidance, strict three-file scope, and unrelated-work isolation.
- Review iteration 5: Reviewers requested literal fail-fast commands, exact commit-OID validation, explicit handling of `ls-remote` failures and stale tracking refs, and isolation from the concurrent FrontComposer checkout. Added the executable delivery script below, package-mode restore/build, exact reviewed-blob handoff semantics, commit-tree verification, and stale-ref ancestry guards. Reclassified the broad slow-test attempt as non-gating diagnostic evidence because it did not use the repository's 60-minute `IntegrationSlow` lane. This avoids treating a network failure as branch absence, publishing a later concurrent commit, pruning an unincorporated stale tip, or claiming evidence from an unintended submodule checkout. KEEP the validated 1.4.1 content, 90 unit and 6 focused integration passes, CRLF preservation, exact three-file scope, and unrelated-work isolation.

## Verification

**Commands:**
- Package-mode solution restore/build, plus an isolated committed-FrontComposer-pointer restore -- expected: record broad validation status without masking baseline failures.
- Package-mode Release restore/build of `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` and `tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj` -- expected: both affected projects succeed with zero warnings and errors.
- `dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --configuration Release --no-build` -- expected: focused MCP tests pass.
- `tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests -class "Hexalith.Memories.IntegrationTests.Mcp.McpAuthenticationIntegrationTests" -class "Hexalith.Memories.IntegrationTests.Mcp.McpServerIntegrationTests"` -- expected: direct MCP integration coverage passes via the repository's xUnit v3 focused-lane fallback.
- `test "$(git symbolic-ref --short HEAD)" = main`; capture `git hash-object` for each reviewed file; stage the three files; compare sorted `git diff --cached --name-only` output to the three literal expected paths; compare each `git rev-parse :<path>` to its reviewed hash; run `git diff --quiet -- <paths>`; byte-check CRLF Markdown; inspect `git diff --cached -- <paths>`; and run `git -c core.whitespace=cr-at-eol diff --cached --check` -- expected: exact approved bytes and paths are staged with no remainder.
- `npx commitlint --edit "$COMMIT_MSG" --verbose` -- expected: the proposed forward-fix message passes before commit.
- `npx commitlint --last --verbose` -- expected: the forward-fix commit satisfies repository commit policy.
- `git fetch origin main --quiet` followed by `npx commitlint --from "$(git merge-base origin/main HEAD)" --to HEAD --verbose` -- expected: the outgoing commit range passes.
- Capture `EXPECTED_TREE=$(git write-tree)` before commit; after commit assert `HEAD^{tree}` equals it, the commit path set is the exact three paths, and each `HEAD:<path>` blob equals the reviewed hash -- expected: hooks/concurrency introduced no content.
- Set and literal-check `BRANCH=dependabot/nuget/ModelContextProtocol.AspNetCore-1.4.1`; capture `FIX_OID=$(git rev-parse HEAD)`; capture `REMOTE_LINE=$(git ls-remote --heads origin "refs/heads/$BRANCH")`. If empty, push `"$FIX_OID:refs/heads/main"`. If present, parse `BRANCH_OID`, prove it is an ancestor of `FIX_OID`, then run `git push --atomic --force-with-lease="refs/heads/$BRANCH:$BRANCH_OID" origin "$FIX_OID:refs/heads/main" ":refs/heads/$BRANCH"` -- expected: exact-OID idempotent publication and safe deletion.
- If a same-named local branch exists, prove it is an ancestor of `FIX_OID` and delete it normally; run `git fetch origin --prune`; assert `main` and `origin/main` both equal `FIX_OID`; assert local and tracking refs are absent; assert `test -z "$(git ls-remote --heads origin "refs/heads/$BRANCH")"` -- expected: synchronized main and complete cleanup.

**Observed results:**
- Package-mode solution restore/build passed in the current checkout with no `NU1605`, 0 warnings, and 0 errors. An isolated restore against the committed FrontComposer pointer `479101a` failed before MCP with pre-existing `NU1010` missing central package versions in FrontComposer Contracts/Shell/Testing; therefore broad clean-checkout solution evidence is blocked.
- Focused package-mode Release restore/build passed for both the MCP production project and MCP test project with 0 warnings and 0 errors.
- MCP unit project passed 90/90 tests; focused MCP integration classes passed 6/6 tests.
- A separate 10-minute whole-project diagnostic attempt timed out in the `IntegrationSlow` test `PipelinePersistenceIntegrationTests.RestartTopology_ShouldKeepFailedUnitVisibleAndAllowReingestionAfterRecovery` after 7 minutes 37 seconds. This was not the repository's 60-minute slow-lane command and is recorded as non-gating diagnostic evidence, not as proof of either an MCP regression or a clean broad gate.

**Delivery script:** The final no-finding review hands off `REVIEWED_PACKAGE_BLOB`, `REVIEWED_CONTEXT_BLOB`, and `REVIEWED_SPEC_BLOB`. Run the following with `set -e`; do not recalculate those three values after review.

```bash
set -e
BASE_OID=dbcd3ceb4242880ef9e0f4f3b05d35f5bcd06cf1
PACKAGE=Directory.Packages.props
CONTEXT=_bmad-output/project-context.md
SPEC=_bmad-output/implementation-artifacts/spec-align-mcp-dependencies-after-branch-merge.md
BRANCH=dependabot/nuget/ModelContextProtocol.AspNetCore-1.4.1
test "$BRANCH" = dependabot/nuget/ModelContextProtocol.AspNetCore-1.4.1
test "$(git symbolic-ref --short HEAD)" = main
test "$(git rev-parse HEAD)" = "$BASE_OID"
test "$(git hash-object "$PACKAGE")" = "$REVIEWED_PACKAGE_BLOB"
test "$(git hash-object "$CONTEXT")" = "$REVIEWED_CONTEXT_BLOB"
test "$(git hash-object "$SPEC")" = "$REVIEWED_SPEC_BLOB"
perl -e 'local $/; open my $h, "<:raw", $ARGV[0] or die $!; my $s=<$h>; die "spec has non-CRLF newline" if $s =~ /(?<!\r)\n|\r(?!\n)/' "$SPEC"
perl -ne 'if (/MCP SDK 1\.4\.1/) { die "context MCP line is not CRLF" unless /\r\n$/; $n++ } END { die "context MCP line count" unless $n == 1 }' "$CONTEXT"
git add -- "$PACKAGE" "$CONTEXT" "$SPEC"
EXPECTED_PATHS=$(printf '%s\n' "$PACKAGE" "$CONTEXT" "$SPEC" | LC_ALL=C sort)
ACTUAL_PATHS=$(git diff --cached --name-only | LC_ALL=C sort)
test "$ACTUAL_PATHS" = "$EXPECTED_PATHS"
test "$(git rev-parse ":$PACKAGE")" = "$REVIEWED_PACKAGE_BLOB"
test "$(git rev-parse ":$CONTEXT")" = "$REVIEWED_CONTEXT_BLOB"
test "$(git rev-parse ":$SPEC")" = "$REVIEWED_SPEC_BLOB"
git diff --quiet -- "$PACKAGE" "$CONTEXT" "$SPEC"
git -c core.whitespace=cr-at-eol diff --cached --check
EXPECTED_TREE=$(git write-tree)
COMMIT_MSG=$(mktemp)
printf '%s\n\n%s\n' 'chore(deps): align MCP packages at 1.4.1' 'Keep the core and ASP.NET Core MCP packages aligned.' > "$COMMIT_MSG"
npx commitlint --edit "$COMMIT_MSG" --verbose
git commit -F "$COMMIT_MSG"
FIX_OID=$(git rev-parse HEAD)
test "$(git rev-parse "$FIX_OID^")" = "$BASE_OID"
test "$(git rev-parse "$FIX_OID^{tree}")" = "$EXPECTED_TREE"
COMMIT_PATHS=$(git diff-tree --no-commit-id --name-only -r "$FIX_OID" | LC_ALL=C sort)
test "$COMMIT_PATHS" = "$EXPECTED_PATHS"
test "$(git rev-parse "$FIX_OID:$PACKAGE")" = "$REVIEWED_PACKAGE_BLOB"
test "$(git rev-parse "$FIX_OID:$CONTEXT")" = "$REVIEWED_CONTEXT_BLOB"
test "$(git rev-parse "$FIX_OID:$SPEC")" = "$REVIEWED_SPEC_BLOB"
npx commitlint --last --verbose
git fetch origin main --quiet
npx commitlint --from "$(git merge-base origin/main "$FIX_OID")" --to "$FIX_OID" --verbose
TRACK_REF=refs/remotes/origin/$BRANCH
if git show-ref --verify --quiet "$TRACK_REF"; then git merge-base --is-ancestor "$TRACK_REF" "$FIX_OID"; fi
if ! REMOTE_LINE=$(git ls-remote --heads origin "refs/heads/$BRANCH"); then exit 1; fi
if test -n "$REMOTE_LINE"; then
  BRANCH_OID=${REMOTE_LINE%%[[:space:]]*}
  git merge-base --is-ancestor "$BRANCH_OID" "$FIX_OID"
  git push --atomic --force-with-lease="refs/heads/$BRANCH:$BRANCH_OID" origin "$FIX_OID:refs/heads/main" ":refs/heads/$BRANCH"
else
  git push origin "$FIX_OID:refs/heads/main"
fi
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then git merge-base --is-ancestor "$BRANCH" "$FIX_OID" && git branch -d "$BRANCH"; fi
git fetch origin --prune
test "$(git rev-parse main)" = "$FIX_OID"
test "$(git rev-parse origin/main)" = "$FIX_OID"
! git show-ref --verify --quiet "refs/heads/$BRANCH"
! git show-ref --verify --quiet "refs/remotes/origin/$BRANCH"
if ! FINAL_REMOTE=$(git ls-remote --heads origin "refs/heads/$BRANCH"); then exit 1; fi
test -z "$FINAL_REMOTE"
rm "$COMMIT_MSG"
```

## Suggested Review Order

**Dependency alignment**

- Aligns both MCP packages and their adjacent version context at 1.4.1.
  [Directory.Packages.props:23](../../Directory.Packages.props#L23)

- Preserves the durable rule that core and ASP.NET Core MCP packages stay aligned.
  [project-context.md:25](../project-context.md#L25)

**Validation and delivery safety**

- Records focused green evidence and separates pre-existing broad validation blockers.
  [spec:77](./spec-align-mcp-dependencies-after-branch-merge.md#L77)

- Binds staging, commit, publication, and pruning to reviewed bytes and exact OIDs.
  [spec:98](./spec-align-mcp-dependencies-after-branch-merge.md#L98)
