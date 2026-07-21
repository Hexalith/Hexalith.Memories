---
title: 'Enforce attached cross-tenant negative evidence on scope-sensitive changes'
type: 'chore'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'ae591ce7a0f1f6aca54ccdaea303eb63980dfa25'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md'
  - '{project-root}/tools/check-story-file-scope.py'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository-lifetime rule *"Tenant isolation requires attached negative evidence"* (`_bmad-output/project-context.md` `### Testing Rules`) exists only as agent guidance. Nothing mechanically enforces it, so a scope-sensitive change can merge with no attached cross-tenant denial / fail-closed evidence. The prior `spec-cross-tenant-negative-evidence-carry-forward.md` (`done`) explicitly gated *"add a general evidence-attachment validator"* as **Ask First**; the human has now approved building it.

**Approach:** Add a self-contained, fail-closed verifier `tools/check-tenant-isolation-evidence.py` that no-ops unless a changed file matches a committed tenant-isolation surface-glob list, and when triggered requires the resolved story/spec to carry a new canonical `## Cross-Tenant Negative Evidence` marker. Wire it into `.githooks/pre-commit` and a CI gate, with unit tests and author docs. Enforcement is prospective; the gate inspects only the resolved story/spec and never rewrites history or canonical policy.

## Boundaries & Constraints

**Always:** Fail closed. Mirror `check-story-file-scope.py` conventions exactly — stdlib-only Python, `def main(argv) -> int`, exit `0` (pass/no-op) or `1` (violation), all output to stdout, a local `ValidationError`, and the same story resolution precedence (`--story-key` > `Story:`/`Story-Key:` trailer > `--branch-name`) reading `<artifacts-root>/<key>.md`. When **no** changed file matches a surface glob, exit 0 as a no-op (non-scope-sensitive change needs no evidence). Provide two author escape hatches so no one is ever hard-blocked with no recourse: an in-spec `**Not triggered:**` reviewed opt-out, and a `Tenant-Isolation-Evidence: not-applicable — <reason>` commit trailer (parsed like the existing `Scope-Override` trailer, logged to stdout). Seed the surface list from the enumerated surfaces in `_bmad-output/project-context.md` and `_bmad-output/planning-artifacts/epics.md` mapped to real repo paths. Commit as `ci`/`chore`, never `feat`. Attach this change's own cross-tenant negative evidence (the verifier's fail-closed unit tests) under a `## Cross-Tenant Negative Evidence` section in this spec.

**Ask First:** The breadth of the surface-glob seed list (over-broad blocks unrelated work; over-narrow misses real surfaces). The exact `## Cross-Tenant Negative Evidence` field labels/schema. Whether pre-commit is blocking or warn-only (CI PR gate stays blocking either way). The exact bypass-trailer key name.

**Never:** Edit `_bmad-output/project-context.md`, the root `project-context.md` bridge, generated `.agents/`/`.claude/` skill files, or any canonical policy body — the gate enforces the existing rule, it does not restate or move it. Touch product runtime, tenant routing, authentication, storage, query builders, submodules, or `Directory.Packages.props`. Claim Story 24.3 integration proof passed. Retro-apply the rule to historical records. Introduce nondeterminism or network calls in the verifier.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Non-sensitive change | No changed file matches a surface glob | Prints no-op notice, exit 0 | — |
| Sensitive + valid proof | Surface touched; resolved story/spec has a `## Cross-Tenant Negative Evidence` section with non-empty Surfaces + ≥1 backticked Test + Command + Result | Prints pass, exit 0 | — |
| Sensitive + accepted blocker | Section present with `**Accepted blocker:**` naming owner, consequence, reopen trigger | Pass, exit 0 | — |
| Sensitive + reviewed opt-out | Section present with `**Not triggered:**` + reason | Pass (logged), exit 0 | — |
| Sensitive + missing/empty evidence | Surface touched; section absent or a required field empty | Prints which fields are missing, exit 1 | Actionable message names the story/spec and required schema |
| Sensitive + no story resolvable | Surface touched; no key from CLI/trailer/branch and no bypass trailer | Exit 1 instructing to add a story/spec or the bypass trailer | — |
| Bypass trailer present | `Tenant-Isolation-Evidence: not-applicable — <reason>` in commit message | Pass with logged bypass + reason, exit 0 | Empty/whitespace reason → exit 1 |
| Missing git / bad input | `git`/`interpret-trailers` absent, unreadable changed-files file | Exit 1 with a clean ValidationError naming the missing tool/input | No raw traceback |

</frozen-after-approval>

## Code Map

- `tools/check-tenant-isolation-evidence.py` -- NEW verifier; sibling of `tools/check-story-file-scope.py` (mirror its argparse, story resolution, path normalization, exit-code and stdout conventions).
- `tools/tenant-isolation-surfaces.txt` -- NEW committed glob list (one per line, `#` comments; style of `tools/integration-fast-required-surfaces.txt`). "Balanced" seed: `src/Hexalith.Memories.Server/Authentication/**`, `src/Hexalith.Memories.Mcp/Authentication/**`, `.../Mcp/McpToolExecutor.cs`, `.../Server/Tenants/**`, `.../Server/Endpoints/Tenant*.cs`, `.../Server/Activities/Tenants/**`, `.../Server/Graph/*GraphQueryBuilder.cs`, `.../Server/Actors/*TenantConfigurationActor.cs`, tenant/isolation contracts, and tenant-lifecycle EventStore domain. No blanket `*Actor*.cs`.
- `tools/check-story-file-scope.py` -- reference implementation to copy conventions from; do not modify.
- `.githooks/commit-msg` -- append a second invocation after the scope check (see Design Notes: commit-msg, not pre-commit, so the bypass/Story trailers are visible).
- `.github/workflows/ci.yml` -- add a "Validate cross-tenant negative evidence" step to the existing `story-file-scope` job (reusing its computed diff inputs), plus a `discover -s tests/tooling/tenant_isolation_evidence` step in `test-unit-contract`.
- `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- NEW subprocess-driven unit tests (`_test.py` suffix, stdlib `unittest`, `REPO_ROOT = Path(__file__).resolve().parents[3]`).
- `CONTRIBUTING.md` -- document the canonical `## Cross-Tenant Negative Evidence` section, when it is required, and the bypass trailer; reference the project-context rule and Story 20.2 / 24.3 anchors.

## Tasks & Acceptance

**Execution:**
- [x] `tools/tenant-isolation-surfaces.txt` -- added the Balanced seed glob list mapped to real repo paths -- drives the trigger; reviewable and narrow.
- [x] `tools/check-tenant-isolation-evidence.py` -- implemented the verifier: collect changed files, match against surfaces, resolve story/spec, validate the `## Cross-Tenant Negative Evidence` marker (proof | accepted-blocker | not-triggered), honor the bypass trailer, fail closed.
- [x] `tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py` -- 22 subprocess tests cover every I/O Matrix row plus shipped-glob liveness -- the change's own attached negative evidence.
- [x] `.githooks/commit-msg` -- wired the verifier after the scope check (commit-msg, not pre-commit; see Design Notes) -- attaches the gate to local commits.
- [x] `.github/workflows/ci.yml` -- added the PR gate step to `story-file-scope` and the tooling-test discover step to `test-unit-contract`.
- [x] `CONTRIBUTING.md` -- documented the marker schema, trigger conditions, bypass trailer, and updated the CI check table.

**Acceptance Criteria:**
- Given a diff that touches no configured surface, when the verifier runs, then it prints a no-op notice and exits 0 regardless of story resolution.
- Given a diff that touches a configured surface and a resolved story/spec with a complete `## Cross-Tenant Negative Evidence` section, when the verifier runs, then it exits 0.
- Given a surface-touching diff whose story/spec lacks the section or a required field, when the verifier runs, then it exits 1 and names the missing fields.
- Given a surface-touching diff with the `Tenant-Isolation-Evidence: not-applicable — <reason>` trailer, when the verifier runs, then it exits 0 and logs the bypass and reason; an empty reason exits 1.
- Given the tooling test suite, when `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"` runs, then all cases pass and are wired into `ci.yml`.
- Given the change is complete, when owned scope is reviewed, then canonical policy files, generated skill files, product runtime, and submodules are untouched.

## Verification

**Commands:**
- `python3 tools/check-tenant-isolation-evidence.py --branch-name main --changed-files-file /dev/null` -- expected: no-op pass, exit 0.
- `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"` -- expected: all fail-closed cases pass.
- `python3 tools/check-story-file-scope.py --branch-name "$(git branch --show-current)" --staged` -- expected: unchanged behavior (no regression to the sibling gate).

**Results (2026-07-21, after review loop 1):**
- `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"` -- 34 tests, 0 failures. Every I/O Matrix row is covered, plus review-loop-1 regression guards (fenced-field & HTML-comment spoof, surfaces-file missing/comment-only, bad bypass disposition, non-boundary bypass token, bulleted labels, empty-Surfaces, non-backtick Command, malformed/multi-key Story trailer, and a dead-glob liveness check over every shipped surface glob).
- Sibling regression: `tests/tooling/story_scope` -- 42 tests, 0 failures.
- CLI smoke on a real surface (`src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`): no story -> exit 1 with actionable guidance; `Tenant-Isolation-Evidence: not-applicable — …` trailer -> exit 0 (bypass logged). Empty diff -> exit 0 no-op.
- `.github/workflows/ci.yml` parses as valid YAML; line endings verified (`.py`/`.yml`/`.githooks` LF, `.txt`/`.md` CRLF per `.gitattributes`).

## Design Notes

- **Local hook = `commit-msg`, not `pre-commit`.** The frozen Approach named `pre-commit`, but pre-commit runs before the commit message exists, so the `Tenant-Isolation-Evidence:` bypass and `Story:` trailers would be invisible and an author intending to bypass would be blocked with no recourse. The sibling scope gate already runs its trailer-aware check in `commit-msg` for the same reason. The intent (block locally) is preserved; only the hook stage changed.
- **CI = a step in `story-file-scope`, not a new job.** The evidence step reuses that job's hardened PR/push diff-collection (`BRANCH_NAME`/`COMMIT_MESSAGE_FILE`/`CHANGED_FILES_FILE`) instead of duplicating ~120 lines of fail-loud diff logic. Same blocking guarantee, one source of truth for the diff.
- **Free-form corpus → mandated marker.** Existing specs attach evidence as free-form prose with no reliable structure, so the verifier cannot parse the back-catalogue; it requires a new canonical `## Cross-Tenant Negative Evidence` marker prospectively (matching the rule's own "attach … or record an accepted blocker" wording). Detection stays structural (bold labels + backtick presence), never semantic.

## Cross-Tenant Negative Evidence

**Surfaces:** Enforcement tooling only — this change adds a governance gate and touches no runtime tenant-isolation surface (no tenant/case route, authorization filter, index/key/graph selector, actor ID, MCP authorization, or attribution path). The negative evidence here is the gate's own fail-closed denial behavior.
**Tests:** `test_sensitive_missing_section_fails`, `test_sensitive_empty_result_field_fails`, `test_sensitive_tests_without_backtick_fails`, `test_accepted_blocker_missing_owner_fails`, `test_not_triggered_without_reason_fails`, `test_sensitive_no_story_resolvable_fails`, `test_bypass_trailer_without_reason_fails`, `test_heading_inside_code_fence_is_not_a_section`, `test_fenced_fields_inside_section_do_not_count`, `test_html_comment_fields_do_not_count`, `test_conflicting_story_keys_fail`, `test_missing_git_reports_clean_error`
**Command:** `python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"`
**Result:** 34 passed, 0 failed. The gate denies (exit 1) on every missing/incomplete-evidence path — including evidence hidden in a code fence or HTML comment — and only passes on valid visible proof, accepted-blocker, reviewed not-triggered, or a reasoned bypass trailer.

## Review Notes (loop 1)

Three parallel adversarial/edge-case/verification-gap reviewers ran against the owned diff. Outcome: no intent_gap or bad_spec (no code re-derivation loopback); patches applied in place.

- **Fixed (fail-open):** evidence labels hidden inside a code fence or HTML comment inside the section were counted as valid. `parse_evidence_fields` is now fence-and-comment aware; regression tests added.
- **Fixed (robustness):** unreadable/non-UTF-8 inputs now raise a clean `ValidationError` (no raw traceback); the bypass token now requires a word boundary (`not-applicableXYZ` rejected); the CONTRIBUTING example uses `<your-story-key>` placeholders.
- **Fixed (coverage):** surface globs widened to `Server/Endpoints/**` and `Server/Actors/**` (architecture confirms all Server endpoints are tenant-scoped and all Server actors are per-tenant singletons — still "Balanced", not a repo-wide blanket); comments corrected; a dead-glob liveness test now asserts every shipped glob matches a real tracked file.
- **Disclosed limitations (by design, structural-not-semantic per the frozen approach):** the gate checks that the resolved story carries evidence but does not bind the free-text `**Surfaces:**` to the exact triggered files — in the wired pipeline the co-located `story-file-scope` gate already ties changed files to the story via File Scope. The `commit-msg` hook and CI wiring have no automated end-to-end test (only the Python tool and manual smoke). Both are candidate follow-ons, surfaced to the human, not silently deferred.
- **Open human decisions (see handoff):** (1) local hook stage `commit-msg` vs the frozen/asked `pre-commit`; (2) whether to widen surface breadth further.

## Suggested Review Order

**Enforcement logic (start here)**

- Entry point: trigger → resolve → validate; no-op unless a surface changed.
  [`check-tenant-isolation-evidence.py:460`](../../tools/check-tenant-isolation-evidence.py#L460)

- The three accepted dispositions (proof | accepted-blocker | not-triggered).
  [`check-tenant-isolation-evidence.py:420`](../../tools/check-tenant-isolation-evidence.py#L420)

- Fail-open fix: fields hidden in a fence or HTML comment no longer count.
  [`check-tenant-isolation-evidence.py:380`](../../tools/check-tenant-isolation-evidence.py#L380)

**Trigger detection**

- Balanced surface globs; every glob is liveness-tested against the tree.
  [`tenant-isolation-surfaces.txt:18`](../../tools/tenant-isolation-surfaces.txt#L18)

- Glob matching (recursive `**`, mirrors the sibling scope gate).
  [`check-tenant-isolation-evidence.py:287`](../../tools/check-tenant-isolation-evidence.py#L287)

**Story resolution and the escape hatch**

- Story key precedence (cli > trailer > branch); conflicts fail closed.
  [`check-tenant-isolation-evidence.py:241`](../../tools/check-tenant-isolation-evidence.py#L241)

- Bypass trailer parsing; requires the exact `not-applicable` token + reason.
  [`check-tenant-isolation-evidence.py:175`](../../tools/check-tenant-isolation-evidence.py#L175)

**Wiring**

- Local gate in the commit-msg hook (not pre-commit — trailers not yet written).
  [`commit-msg:25`](../../.githooks/commit-msg#L25)

- CI PR gate, reusing the story-file-scope job's diff inputs.
  [`ci.yml:152`](../../.github/workflows/ci.yml#L152)

- CI runs the gate's own unit tests.
  [`ci.yml:214`](../../.github/workflows/ci.yml#L214)

**Docs and tests**

- Author contract: the marker schema, trigger conditions, bypass trailer.
  [`CONTRIBUTING.md:134`](../../CONTRIBUTING.md#L134)

- 34 subprocess tests: every matrix row plus the fail-open regression guards.
  [`tenant_isolation_evidence_test.py:70`](../../tests/tooling/tenant_isolation_evidence/tenant_isolation_evidence_test.py#L70)
