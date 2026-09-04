---
title: 'Allow chore in the repository-wide commit policy'
type: 'bugfix'
created: '2026-09-02'
status: 'in-review'
baseline_commit: '7fa095591bf9c9479aea7280319216959eaaa25f'
review_loop_iteration: 1
context:
  - '{project-root}/references/Hexalith.AI.Tools/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository-wide commitlint configuration removes `chore` from the Conventional
Commit type allowlist, so the otherwise valid subject `chore: update subproject references for
Hexalith.FrontComposer and Hexalith.Tenants` is rejected locally and in CI. The adjacent “No story
key resolved” diagnostic is already a successful no-op and is not the source of the failure.

**Approach:** Restore `chore` in the single repository commitlint policy used by local hooks and CI,
synchronize the three root assistant entry points, and update the authoritative Git guidance in the
separate `Hexalith.AI.Tools` submodule. Retain all story-scope, review-readiness, tenant-evidence,
and Conventional Commit format checks.

## Boundaries & Constraints

**Always:** Keep one repository-wide type policy for local and CI validation; preserve all other
commitlint rules; keep each repository's `AGENTS.md`, `CLAUDE.md`, and
`.github/copilot-instructions.md` normalized-text equivalent; edit `Hexalith.AI.Tools` from its own
repository root; preserve the user's staged artifact and existing submodule changes.

**Ask First:** Updating the parent `Hexalith.AI.Tools` gitlink, propagating copied entry-point changes
to other repositories, changing semantic-release behavior, or committing any files.

**Never:** Disable commitlint or Git hooks; remove story-key tooling; bypass validation; stage,
reset, rewrite, or combine the user's existing changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Maintenance commit | `chore: update subproject references for Hexalith.FrontComposer and Hexalith.Tenants` | Local and CI commitlint accept the subject | Any unrelated hook may still report its own actionable failure |
| Unsupported type | `unknown: update dependencies` | Commitlint still rejects the subject | Report `type-enum`; do not weaken other rules |
| Unkeyed commit | No story key in branch or message | Existing story checks remain no-op where currently designed | Do not turn the no-op diagnostic into a failure |
| Automated contributor | Agent loads root and `Hexalith.AI.Tools` guidance | Both permit `chore` subject to the owning repository's effective commitlint policy | No contradictory prohibition remains |

</frozen-after-approval>

## Code Map

- `commitlint.config.mjs:7-19` -- actual rejection source used by local and CI commitlint.
- `AGENTS.md:47-53`, `CLAUDE.md`, `.github/copilot-instructions.md` -- synchronized local policy.
- `references/Hexalith.AI.Tools/hexalith-{llm,git}-instructions.md` -- authoritative prohibition;
  edit from the clean submodule root. Its three entry points already defer to repository policy.
- `tests/tooling/commitlint/commitlint_config_test.py`, `.github/workflows/ci.yml:250-325` -- new
  pinned-CLI fixture and its existing post-`npm ci` execution lane.
- `.githooks/commit-msg:15-34`, `tools/check-story-file-scope.py:474-486` -- read-only proof that
  commitlint is independent and unresolved story keys already exit successfully.

## Tasks & Acceptance

**Execution:**
- [x] `commitlint.config.mjs` -- add `chore` to the explicit allowlist; preserve all other rules.
- [x] Root `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` -- permit `chore` identically.
- [x] `references/Hexalith.AI.Tools/hexalith-{llm,git}-instructions.md` -- remove the global
  prohibition and align the type table, advice, and examples.
- [x] `tests/tooling/commitlint/commitlint_config_test.py`, `.github/workflows/ci.yml` -- add a
  pinned-CLI fixture for accepted, unsupported, and malformed messages and run it in CI.
- [x] This spec -- record evidence and the final file list without staging user work.

**Acceptance Criteria:**
- Given the reported `chore:` subject, when pinned commitlint runs locally or in CI, then it passes;
  unsupported types and malformed headers still fail.
- Given an agent loads local and authoritative guidance, when it selects a type, then neither source
  prohibits `chore`, and each entry-point triplet remains identical.
- Given the CI tooling lane, when fixtures run, then they exercise the real config and all matrix
  behaviors; the story-key no-op remains unchanged.
- Given the pre-existing index, when work finishes, then its three staged objects remain unchanged.

## Spec Change Log

- 2026-09-02 -- Added `chore` to the repository commitlint allowlist and synchronized the three
  root assistant entry points. Focused positive, negative, malformed-header, story-scope no-op,
  normalized-entry-point, whitespace, and staged-index preservation checks passed.
- 2026-09-03 -- Review loop 1 found contradictory authoritative guidance and no durable behavioral
  test. Human authorized the submodule edit; added both guides and a CI fixture to scope. Avoid:
  simultaneously permitting/forbidding `chore`. KEEP: explicit allowlist, other rules, story gates,
  synchronized entry points, and unchanged index. Do not commit or update the gitlink.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v` -- all matrix cases pass.
- `python3 tools/check-story-file-scope.py --branch-name main --changed-file commitlint.config.mjs` -- exit 0 no-op.
- Compare both entry-point triplets and search changed AI.Tools guides for remaining prohibitions.
- Run `git diff --check` in both repositories and compare the root index's three object IDs.

**Results (2026-09-04):**
- Repository-pinned `@commitlint/cli@21.1.0` fixture: 3 tests ran and passed, covering the accepted
  maintenance subject plus unsupported-type and malformed-header rejection.
- Story-scope direct check exited 0 with `No story key resolved; story-scope check is a no-op.`;
  all 53 focused story-scope tests passed.
- Both root and AI.Tools entry-point triplets compared byte-identical. The changed AI.Tools guides
  contained no remaining `chore` prohibition.
- Root tracked and no-index whitespace checks passed. AI.Tools' canonical CRLF files passed
  `git -c core.whitespace=cr-at-eol diff --check`; plain `git diff --check` treats their CR bytes as
  trailing whitespace because that repository has no line-ending attributes.
- The focused CI inventory test proving every tooling fixture is workflow-registered passed. Its
  66-test class has one unrelated baseline failure: the baseline CI already has five restore steps
  while the guard expects four.
- The three pre-existing staged object IDs remained exactly
  `aa6ea831d81ad897b7392e76ff73749ed2afe337`,
  `15a00e8d1999892a4bfd6a7f2f355d328576b446`, and
  `d2b7ede359830c27934ac9f577e3073955c3e2c2`.

## Final File List

- `commitlint.config.mjs`
- `AGENTS.md`
- `CLAUDE.md`
- `.github/copilot-instructions.md`
- `.github/workflows/ci.yml`
- `tests/tooling/commitlint/commitlint_config_test.py`
- `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`
- `references/Hexalith.AI.Tools/hexalith-git-instructions.md`
- `spec-allow-chore-in-global-commit-policy.md`
