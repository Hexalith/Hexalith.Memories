---
title: 'Resolve the standalone-spec commit gate path'
type: 'bugfix'
created: '2026-07-28'
status: 'done'
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
message trailer cannot rescue the earlier `pre-commit` gate. The original index
also mixed standalone readiness-gate work, Story 27.3 corrections, and
submodule pointer changes, so assigning the whole set to Story 27.3 would have
recorded false ownership and still failed its declared scope. On 2026-07-29,
the human owner separately approved the exact remaining policy and root
submodule-pointer paths listed in this spec for the `/pushall` sync commit.

**Approach:** Make exact `spec-*` artifact keys first-class scope owners across
the three story-aware validators, preserving their existing source precedence
and fail-closed conflict behavior. Give each standalone spec a machine-readable
File Scope, partition the original work into independently validated commit
groups, and record the later human-approved `/pushall` sync group without
altering or discarding any user changes.

## Boundaries & Constraints

**Always:** Accept only normalized, full `spec-[a-z0-9][a-z0-9-]*` keys whose
artifact exists; apply the same CLI > trailer > branch precedence and conflict
checks used for numeric stories; require a parseable File Scope before a spec
can authorize changed files; keep `main` with no story/spec key fail-closed;
preserve all current index and worktree content while regrouping it; validate
Conventional Commit messages before and after every commit.

**Ask First:** Expanding any standalone spec's scope beyond its recorded Code
Map and implementation evidence; attributing root submodule pointer changes to
an owner; changing or committing files inside a submodule; combining Story
27.3 work with the standalone readiness-gate work. The 2026-07-29 `/pushall`
approval satisfies this requirement only for the exact paths listed below.

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
| Mixed current index | Spec, policy, and submodule changes staged together | Partition by owner, or use the exact human-approved `/pushall` sync group recorded in File Scope | Unapproved groups remain uncommitted |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `tools/check-story-file-scope.py` -- recognize exact standalone-spec owner keys.
- `tools/check-tenant-isolation-evidence.py` -- apply the same owner-key contract to sensitive changes.
- `tools/check-story-review-readiness.py` -- extend existing standalone-spec support to trailers and branches.
- `tests/tooling/story_scope/story_scope_validator_test.py` -- scope-owner resolution regressions.
- `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- evidence-owner resolution regressions.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- readiness-owner resolution regressions.
- `.githooks/commit-msg` -- install the repository-local commitlint gate before story-aware trailer checks.
- `.github/workflows/commitlint.yml` -- align CI commit-message validation with the repository-local configuration.
- `commitlint.config.mjs` -- retain the approved Conventional Commit rules in the Node module format.
- `AGENTS.md` -- synchronize the shared AI-assistant baseline.
- `CLAUDE.md` -- synchronize the shared AI-assistant baseline.
- `.github/copilot-instructions.md` -- synchronize the shared AI-assistant baseline.
- `CONTRIBUTING.md` -- standalone-spec ownership guidance only.
- `_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md` -- add its explicit File Scope.
- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` -- this fix's scope and implementation record.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- append confirmed pre-existing review findings.
- `references/Hexalith.AI.Tools` -- record the root-declared submodule pointer synchronized by `/pushall`.
- `references/Hexalith.Builds` -- record the root-declared submodule pointer synchronized by `/pushall`.
- `references/Hexalith.Commons` -- record the root-declared submodule pointer synchronized by `/pushall`.
- `references/Hexalith.EventStore` -- record the root-declared submodule pointer synchronized by `/pushall`.
- `references/Hexalith.Tenants` -- record the root-declared submodule pointer synchronized by `/pushall`.

Read/verify only:

- `.githooks/pre-commit`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`

Forbidden by default:

- governance changes other than the exact approved paths above
- Story 27.3 artifacts and implementation
- submodule contents and pointers other than the exact approved paths above

## Code Map

- `tools/check-story-file-scope.py` -- canonical scope-owner resolution and File Scope enforcement.
- `tools/check-tenant-isolation-evidence.py` -- resolves the same owner when sensitive surfaces change.
- `tools/check-story-review-readiness.py` -- already supports CLI spec keys but not trailer or branch sources.
- `tests/tooling/story_scope/story_scope_validator_test.py` -- numeric/spec resolution, malformed-key, and conflict regressions.
- `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- spec-owned evidence resolution regressions.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- spec trailer/branch parity regressions.
- `.githooks/commit-msg`, `.github/workflows/commitlint.yml`, and `commitlint.config.mjs` -- local and CI Conventional Commit enforcement.
- `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` -- synchronized shared assistant baseline.
- `CONTRIBUTING.md` -- contributor-facing spec ownership and commit examples.
- `_bmad-output/implementation-artifacts/spec-executable-pre-review-story-gate.md` -- completed standalone work requiring an explicit File Scope.
- `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` -- this fix's owner and File Scope.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review ledger for findings outside this fix's ownership.
- The five exact `references/Hexalith.*` entries in File Scope -- root submodule pointers approved for the `/pushall` sync commit; no submodule contents are owned here.

## Tasks & Acceptance

**Execution:**
- [x] `tools/check-story-file-scope.py` and its focused tests -- recognize exact spec keys from all three sources without relaxing numeric-story behavior.
- [x] `tools/check-tenant-isolation-evidence.py` and its focused tests -- use the same spec-key resolution contract for sensitive changes.
- [x] `tools/check-story-review-readiness.py` and its focused tests -- extend existing CLI-only spec support to trailer and branch parity.
- [x] `CONTRIBUTING.md` -- document spec branches/trailers, required File Scope, and the retained no-owner failure.
- [x] Both standalone spec artifacts -- declare narrow allowed-file lists sufficient for their own coherent change groups.
- [x] Git index -- preserve the original owner partition, then stage the exact later policy and submodule-pointer set only after explicit human approval for the `/pushall` sync commit.

**Acceptance Criteria:**
- Given a branch, trailer, or CLI argument containing one exact existing spec key, when each validator resolves ownership, then it selects the same artifact and applies its normal checks.
- Given absent, malformed, partial, multiple, or conflicting owner keys, when validation runs with changed files, then it exits 1 with the offending sources identified.
- Given the mixed working tree, when commit groups are prepared, then standalone-spec and Story 27.3 changes never share a commit, and only the exact human-approved root submodule pointers may join the `/pushall` sync commit.
- Given a prepared commit, when local hooks and commitlint run, then validation passes without bypass flags and the post-commit message check remains green.

## Spec Change Log

- 2026-07-29 -- Human approved the exact remaining commitlint-policy, synchronized assistant-baseline, and five root submodule-pointer paths for the `/pushall` sync commit; submodule contents and all other forbidden-default paths remain excluded.
- 2026-07-29 -- Human approved adding the deferred-work ledger to File Scope so the mandatory review could record verified pre-existing findings; the frozen intent is unchanged.

## Design Notes

Use one shared extraction contract per validator: numeric story keys and exact
`spec-*` keys are both artifact owner keys, but only a full key is accepted.
The artifact remains authoritative; recognizing its name never substitutes for
the required File Scope or evidence sections.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/story_scope -p "*_test.py"` -- 51 tests pass.
- `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"` -- 41 tests pass.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py"` -- 45 tests pass.
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- 33 tests pass.
- `.githooks/pre-commit` -- each staged owner group passes from its matching branch.
- `npx commitlint --edit <message-file> --verbose` and `npx commitlint --last --verbose` -- messages pass before and after commit.

**Commit evidence:**

- `5bf5870c` -- standalone-spec owner resolution, focused regressions, guidance, and this spec.
- `5be50c24` -- executable story review-readiness gate, policy, fixtures, and explicit File Scope.
- `5edfb8d5` -- scope-boundary regressions for standalone spec owners.
- `e13e986e` -- exact ASCII owner parsing, duplicate-key rejection, bypass conflict checks, and review regressions.
- The original commitlint-policy and submodule-pointer exclusions remained uncommitted until the human's 2026-07-29 `/pushall` approval; the exact paths now appear in this spec's File Scope for the validated sync commit.

## Suggested Review Order

**Standalone owner resolution**

- Start with exact ASCII spec-token validation at the canonical scope gate.
  [`check-story-file-scope.py:122`](../../tools/check-story-file-scope.py#L122)

- Follow CLI, trailer, and branch precedence through fail-closed conflict handling.
  [`check-story-file-scope.py:246`](../../tools/check-story-file-scope.py#L246)

- Confirm tenant-evidence ownership is resolved before bypass acceptance.
  [`check-tenant-isolation-evidence.py:516`](../../tools/check-tenant-isolation-evidence.py#L516)

- Confirm readiness ownership conflicts are likewise checked before bypass.
  [`check-story-review-readiness.py:759`](../../tools/check-story-review-readiness.py#L759)

**Executable readiness gate**

- Review the central readiness orchestration and five mechanically checked conditions.
  [`check-story-review-readiness.py:755`](../../tools/check-story-review-readiness.py#L755)

- Trace local commit integration with cumulative branch-diff derivation.
  [`commit-msg:43`](../../.githooks/commit-msg#L43)

- Trace CI integration using its already prepared pull-request changed set.
  [`ci.yml:165`](../../.github/workflows/ci.yml#L165)

- Read the policy boundary that defines what a green gate proves.
  [`story-phase-ledger.md:148`](../../_bmad/custom/story-phase-ledger.md#L148)

**Verification and records**

- Inspect scope-owner success, conflict, partial, duplicate, and Unicode regressions.
  [`story_scope_validator_test.py:145`](../../tests/tooling/story_scope/story_scope_validator_test.py#L145)

- Inspect sensitive-surface ownership and bypass-conflict regressions.
  [`tenant_isolation_evidence_test.py:292`](../../tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py#L292)

- Inspect readiness source parity and malformed-owner regressions.
  [`story_review_readiness_test.py:144`](../../tests/tooling/story_review_readiness/story_review_readiness_test.py#L144)

- Review contributor guidance for standalone spec ownership and required File Scope.
  [`CONTRIBUTING.md:63`](../../CONTRIBUTING.md#L63)

- Verify the separately committed readiness spec's explicit ownership boundary.
  [`spec-executable-pre-review-story-gate.md:85`](spec-executable-pre-review-story-gate.md#L85)

- Finish with pre-existing findings intentionally routed outside this fix.
  [`deferred-work.md:2590`](deferred-work.md#L2590)
