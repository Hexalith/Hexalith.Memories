---
title: 'Resolve the standalone-spec commit gate path'
type: 'bugfix'
created: '2026-07-28'
status: 'in-progress'
baseline_commit: 'fc92c4d8ac63601cbc01741bd92b91ee7e6bcdfe'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository requires story-scope validation before every commit,
but the validator recognizes only numeric story keys. Standalone `spec-*` work
therefore cannot be committed from `main` or a spec-named branch, and a commit
message trailer cannot rescue the earlier `pre-commit` gate. The current index
also mixes standalone readiness-gate work, Story 27.3 corrections, and two
submodule pointer changes, so assigning the whole set to Story 27.3 would record
false ownership and still fail its declared scope.

**Approach:** Make exact `spec-*` artifact keys first-class scope owners across
the three story-aware validators, preserving their existing source precedence
and fail-closed conflict behavior. Give each standalone spec a machine-readable
File Scope, then partition the current work into independently validated commit
groups without altering or discarding any user changes.

## Boundaries & Constraints

**Always:** Accept only normalized, full `spec-[a-z0-9][a-z0-9-]*` keys whose
artifact exists; apply the same CLI > trailer > branch precedence and conflict
checks used for numeric stories; require a parseable File Scope before a spec
can authorize changed files; keep `main` with no story/spec key fail-closed;
preserve all current index and worktree content while regrouping it; validate
Conventional Commit messages before and after every commit.

**Ask First:** Expanding any standalone spec's scope beyond its recorded Code
Map and implementation evidence; attributing the Commons or Tenants submodule
pointer changes to an owner; changing or committing files inside a submodule;
combining Story 27.3 work with the standalone readiness-gate work.

**Never:** Use `--no-verify`; invent a numeric story key; treat a partial
`spec-*` substring as a key; let `Scope-Override` authorize forbidden-default
submodule paths; reset, clean, overwrite, or drop existing changes; weaken the
existing failure when changed files have no resolved owner.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Spec branch | `fix/spec-resolve-story-gate-commit-path` | Resolve the exact spec artifact and enforce its File Scope | Missing artifact or scope exits 1 |
| Spec trailer | `Story-Key: spec-resolve-story-gate-commit-path` | Resolve from the trailer in `commit-msg` | Malformed or partial keys exit 1 |
| Conflicting sources | Numeric story branch plus spec trailer | Reject and report both sources | Exit 1 without selecting either |
| Unowned main commit | `main`, no trailer | Preserve the current fail-closed result | Actionable no-key diagnostic |
| Mixed current index | Spec, 27.3, and submodule changes staged together | Partition into coherent groups before committing | Unowned groups remain uncommitted |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `tools/check-story-file-scope.py` -- recognize exact standalone-spec owner keys.
- `tools/check-tenant-isolation-evidence.py` -- apply the same owner-key contract to sensitive changes.
- `tools/check-story-review-readiness.py` -- extend existing standalone-spec support to trailers and branches.
- `tests/tooling/story_scope/story_scope_validator_test.py` -- scope-owner resolution regressions.
- `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- evidence-owner resolution regressions.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- readiness-owner resolution regressions.
- `CONTRIBUTING.md` -- standalone-spec ownership guidance only.
- `_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md` -- add its explicit File Scope.
- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` -- this fix's scope and implementation record.

Read/verify only:

- `.githooks/pre-commit`
- `.githooks/commit-msg`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`

Forbidden by default:

- unrelated staged governance changes
- Story 27.3 artifacts and implementation
- submodule pointers and contents

## Code Map

- `tools/check-story-file-scope.py` -- canonical scope-owner resolution and File Scope enforcement.
- `tools/check-tenant-isolation-evidence.py` -- resolves the same owner when sensitive surfaces change.
- `tools/check-story-review-readiness.py` -- already supports CLI spec keys but not trailer or branch sources.
- `tests/tooling/story_scope/story_scope_validator_test.py` -- numeric/spec resolution, malformed-key, and conflict regressions.
- `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- spec-owned evidence resolution regressions.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- spec trailer/branch parity regressions.
- `CONTRIBUTING.md` -- contributor-facing spec ownership and commit examples.
- `_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md` -- completed standalone work requiring an explicit File Scope.
- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` -- this fix's owner and File Scope.

## Tasks & Acceptance

**Execution:**
- [x] `tools/check-story-file-scope.py` and its focused tests -- recognize exact spec keys from all three sources without relaxing numeric-story behavior.
- [x] `tools/check-tenant-isolation-evidence.py` and its focused tests -- use the same spec-key resolution contract for sensitive changes.
- [x] `tools/check-story-review-readiness.py` and its focused tests -- extend existing CLI-only spec support to trailer and branch parity.
- [x] `CONTRIBUTING.md` -- document spec branches/trailers, required File Scope, and the retained no-owner failure.
- [x] Both standalone spec artifacts -- declare narrow allowed-file lists sufficient for their own coherent change groups.
- [x] Git index -- unstage safely, restage by owner, run focused gates and commitlint, and leave submodule pointers or other unowned work uncommitted.

**Acceptance Criteria:**
- Given a branch, trailer, or CLI argument containing one exact existing spec key, when each validator resolves ownership, then it selects the same artifact and applies its normal checks.
- Given absent, malformed, partial, multiple, or conflicting owner keys, when validation runs with changed files, then it exits 1 with the offending sources identified.
- Given the current mixed working tree, when commit groups are prepared, then standalone-spec and Story 27.3 changes never share a commit and no unowned submodule pointer is committed.
- Given a prepared commit, when local hooks and commitlint run, then validation passes without bypass flags and the post-commit message check remains green.

## Spec Change Log

## Design Notes

Use one shared extraction contract per validator: numeric story keys and exact
`spec-*` keys are both artifact owner keys, but only a full key is accepted.
The artifact remains authoritative; recognizing its name never substitutes for
the required File Scope or evidence sections.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/story_scope -p "*_test.py"` -- 47 tests pass.
- `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"` -- 36 tests pass.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py"` -- 40 tests pass.
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- 33 tests pass.
- `.githooks/pre-commit` -- each staged owner group passes from its matching branch.
- `npx commitlint --edit <message-file> --verbose` and `npx commitlint --last --verbose` -- messages pass before and after commit.

**Commit evidence:**

- `5bf5870c` -- standalone-spec owner resolution, focused regressions, guidance, and this spec.
- `5be50c24` -- executable story review-readiness gate, policy, fixtures, and explicit File Scope.
- Commitlint-policy files and all Commons, EventStore, and Tenants submodule pointer changes remain uncommitted and unstaged because neither approved standalone spec owns them.
