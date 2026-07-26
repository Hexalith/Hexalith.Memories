ok---
title: 'Restore a policy-compliant Release pipeline for Hexalith.Tenants'
type: 'bugfix'
created: '2026-07-25'
status: 'done'
review_loop_iteration: 1
baseline_commit:
  memories: 'c7765d395d511b571cc69bee7e99ac5a65eea2dd'
  Hexalith.Builds: '0d8e4652b978d71d5e1ee33181b1515f5dd5413e'
  Hexalith.Tenants: 'ec7ec8c51eb2ccaa093ae008d2f69c1006220d5a'
context:
  - '{project-root}/references/Hexalith.Builds/.github/workflows/ci-cd-standards.md'
  - '{project-root}/references/Hexalith.Builds/.github/workflows/domain-release.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Every `Hexalith.Tenants` Release run fails with `startup_failure` in 0s (latest: run 30146368778) because `release.yml` calls `domain-release.yml@main` without the now-required `builds-execution-sha` input. The caller has also drifted from the shared Release Gates policy: it auto-releases on `workflow_run` instead of `workflow_dispatch`, has no source preflight, no protected environment, no publication preflight, no package manifest, and still uses forbidden `--skip-duplicate`.

**Approach:** Two coupled PRs. First generalize the shared publication preflight in `Hexalith.Builds` so it is no longer hard-coded to EventStore's 14-package inventory; then bring the `Hexalith.Tenants` release caller to full parity with the documented contract, pinned to the merged Builds commit.

## Boundaries & Constraints

**Always:**
- Pin the reusable release workflow to one exact 40-hex Builds commit and pass that identical literal as `builds-execution-sha` — `job.workflow_sha` must equal it or the job fails closed.
- Keep every gate fail-closed. A generalized inventory check must still reject an inventory whose size or contents changed unexpectedly.
- Keep the source-proof job outside the protected release job so an invalid dispatch cannot request approval or touch release secrets.
- Conventional Commits in both repos; never bypass commit validation.
- Work from the repository that owns each change; commit the two repos separately.
- Honour `.gitattributes`: shell, Python and YAML files stay LF.

**Ask First:**
- Creating or configuring the `production` GitHub environment on `Hexalith.Tenants` (repo settings, not code).
- Any change to `Hexalith.EventStore`.
- Changing the published package inventory of either module.

**Never:**
- Do not use `@main`, tags, expressions or variables for the release-workflow `uses:` revision.
- Do not give `--expected-package-count` a default value — a default silently reintroduces the bug for the next module.
- Do not use `secrets: inherit`, and do not add `--skip-duplicate` back: duplicate skipping is forbidden.
- Do not modify the release pipelines of other modules, or `references/Hexalith.Builds` gitlinks, in these PRs.
- Do not push a release or dispatch the workflow as part of this work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Matching inventory | manifest with 5 unique IDs, `--expected-package-count 5` | preflight proceeds to destination-absence checks | N/A |
| EventStore inventory | manifest with 14 unique IDs, `--expected-package-count 14` | preflight proceeds — existing behavior preserved | N/A |
| Size drift | manifest with 4 IDs, `--expected-package-count 5` | fail closed | `package-inventory-mismatch` |
| Duplicate IDs | manifest with 5 entries, 2 sharing an ID case-insensitively | fail closed | `package-inventory-mismatch` |
| Missing count argument | preflight invoked without `--expected-package-count` | argparse rejects the invocation | non-zero exit, no publication |
| Non-positive count | `--expected-package-count 0` or `-1` | fail closed before reading the manifest | argument validation error |
| Dispatch off main | release dispatched from a non-`main` ref | `verify-source` fails before the protected job | job fails, no approval requested |
| Stale dispatch | dispatched SHA is no longer the live `main` tip | `verify-source` fails | job fails, no approval requested |
| No green CI | dispatched SHA has no successful exact-SHA push CI run | `verify-source` fails | job fails, no approval requested |

</frozen-after-approval>

## Code Map

**`references/Hexalith.Builds`** (PR 1 — merge first)
- `Github/publish-containers/publication_preflight.py` -- hard-codes `!= 14` at two sites (`_require_absent_destinations` ~L330-336, `_load_package_identity` ~L426-442); add the CLI argument (~L499-510) and thread it through.
- `Github/publish-containers/publish-containers.sh` -- invokes the preflight with `--phase container` (~L83-95); must forward the count.
- `Github/publish-containers/action.yml` -- byte-compares the five installed helper files against the approved Builds commit; unchanged, but its file list constrains what may be added.
- `Github/publish-containers/tests/test_publication_preflight.py`, `test_publish_script_contract.py`, `test_oci_registry_validator.py` -- fixtures build 14-package manifests; parameterize.
- `Tools/test-publish-containers.ps1` -- test entry point used by Builds CI (`.github/workflows/build-release.yml:119`).
- `.github/workflows/domain-release.md` -- documents the caller contract and the `verifyReleaseCmd`/`publishCmd` shape.

**`references/Hexalith.Tenants`** (PR 2 — after PR 1 merges)
- `.github/workflows/release.yml` -- the broken caller: `@main` + missing `builds-execution-sha`.
- `.releaserc.json` -- no `verifyReleaseCmd`; `publishCmd` uses `--skip-duplicate`.
- `scripts/pack-release-packages.py` -- hard-codes the 5 package projects in `PACKAGE_PROJECTS`; becomes manifest-driven.
- `scripts/validate-release-secrets.sh` -- existing secrets gate, reused unchanged.
- `tools/release-packages.json` -- **new**, authoritative manifest (default `package-manifest` input path).
- `scripts/validate-publication-preflight.sh` -- **new**, caller wrapper over the action-installed `.hexalith/release/publication_preflight.py`.

**Reference implementations (read, do not edit):** `references/Hexalith.EventStore/.github/workflows/release.yml`, `.releaserc.json`, `scripts/validate-publication-preflight.sh`, `tools/release-packages.json`, `tools/pack-release-packages.py`.

## Tasks & Acceptance

**Execution — PR 1, `references/Hexalith.Builds`** (branch `fix/publication-preflight-package-count`, commit `bbfa283`):
- [x] `Github/publish-containers/publication_preflight.py` -- add required `--expected-package-count` (positive int, no default); replace both `!= 14` literals and the two error messages with the supplied count -- removes the EventStore-specific constant while keeping the check fail-closed.
- [x] `Github/publish-containers/publish-containers.sh` -- read an expected-count input and forward it to the `--phase container` invocation, failing closed when absent or non-positive -- the container phase must enforce the same inventory identity as verify/publish.
- [x] `Github/publish-containers/tests/*.py` -- parameterize the 14-package fixtures and add cases for the size-drift, duplicate-ID, missing-argument and non-positive-count rows of the I/O matrix -- these tests are the only automated guard on this tool.
- [x] `.github/workflows/domain-release.md` -- document the new required argument and show it in the `verifyReleaseCmd`/`publishCmd` example -- callers copy this contract verbatim.
- [x] `.github/workflows/domain-release.yml` -- **added during implementation**: `publish-containers.sh` reads the count from the environment, so the reusable workflow needed an `expected-package-count` input exported as `HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT`. Without it the container phase had no source for the value.

**Execution — PR 2, `references/Hexalith.Tenants`** (branch `fix/release-workflow-builds-execution-sha`, commit `f1eeeaa`):
- [x] `tools/release-packages.json` -- create with the 5 published IDs and project paths (`Contracts`, `Client`, `Server`, `Testing`, `Aspire`) -- authoritative inventory the preflight freezes and proves absent.
- [x] `scripts/pack-release-packages.py` -- read the manifest instead of the hard-coded `PACKAGE_PROJECTS` list, failing on duplicates or empty entries -- prevents packed output drifting from the inventory the preflight proves.
- [x] `scripts/validate-publication-preflight.sh` -- create, mirroring EventStore's wrapper but asserting the Tenants identity (`Hexalith/Hexalith.Tenants`, `registry.hexalith.com/tenants`) and passing `--expected-package-count 5` -- fail-closed bridge between semantic-release and the shared preflight.
- [x] `.github/workflows/release.yml` -- switch to `workflow_dispatch`, add the caller-owned `verify-source` job (current-`main` tip + exact-SHA green push CI), pin `uses:` to the merged Builds SHA, pass the identical `builds-execution-sha`, add `environment-name: production`, `actions: read`, and `needs: verify-source` -- restores startup and satisfies Release Gates. **SHA is `BUILDS_EXECUTION_SHA_PLACEHOLDER` in both places pending the PR 1 merge.**
- [x] `.releaserc.json` -- add `verifyReleaseCmd` running the preflight `verify` phase, add the `publish` phase to `publishCmd`, and drop `--skip-duplicate` -- duplicate skipping is forbidden; existing identities are collisions.

**Execution — iteration 1, review loopback:**
- [x] `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- rewrite the release-wiring assertions to pin the **new** contract (`workflow_dispatch`, `verify-source` job + `needs:`, `environment-name`, `expected-package-count`, `test-projects: ''`, `--skip-duplicate` absent, both preflight phases, publish-before-push ordering, no `changelog`/`git` plugins) and require an exact 40-hex Builds pin whose value equals `builds-execution-sha` -- the old assertions pinned the superseded contract and would have redded CI, deadlocking the release gate.
- [x] `Hexalith.Tenants/tests/.../PackageGovernanceTests.cs` -- add `Release_package_manifest_matches_every_other_copy_of_the_inventory` -- ties `tools/release-packages.json` to `ExpectedPackageIds`, `PublishablePackageProjects`, the workflow input and the wrapper constant, and proves each project file exists.
- [x] `Hexalith.Tenants/.releaserc.json` -- remove `@semantic-release/changelog` and `@semantic-release/git` -- the `git` plugin pushes a CHANGELOG commit to `main` during `prepare`, which makes the publish-phase source proof fail after the tag is pushed and strands the version. Matches EventStore.
- [x] `Hexalith.Tenants/scripts/validate-publication-preflight.sh` -- replace `${VAR:-default}` with `${VAR-}` -- `:-` substitutes on set-but-empty, so the cross-check passed vacuously in exactly the case its message named.
- [x] `Hexalith.Tenants/scripts/pack-release-packages.py` -- reject non-dict roots and non-string `id`/`project`, case-fold project dedup, refuse option-like and non-confined paths, require an existing `.csproj`, and narrow the exception handler -- manifest data reaches `dotnet pack` argv, so it must be treated as untrusted input.
- [x] `Hexalith.Builds/.github/workflows/domain-release.yml` -- add a `Validate declared package count` step gated on `publish-containers` -- previously an invalid count was rejected only inside `publish-containers.sh`, i.e. *after* `dotnet nuget push`, leaving a half-published release.
- [x] `Hexalith.Builds/Github/publish-containers/tests/test_publish_script_contract.py` -- assert `--expected-package-count` and its value are forwarded -- deleting the forwarding line previously kept all 49 tests green.
- [x] `Hexalith.Builds/.github/workflows/domain-release.md` -- correct the input table: the workflow, not just caller wrappers, now rejects `0`.
- [x] `Hexalith.Builds/.github/workflows/ci-cd-standards.md` -- add `expected-package-count` to the canonical caller example.

**Acceptance Criteria:**
- Given the Builds unit-test suite, when `Tools/test-publish-containers.ps1` runs, then all tests pass with no test asserting a literal inventory size of 14 outside an explicitly-14 EventStore fixture.
- Given `Hexalith.Tenants` `release.yml` after PR 2, when the `uses:` revision and the `builds-execution-sha` input are compared, then both are the same 40-hex literal and neither is a branch, tag, expression or variable.
- Given a `push` to `Hexalith.Tenants` `main`, when CI completes successfully, then no Release run is triggered — release requires an explicit dispatch.
- Given `actionlint` (or equivalent YAML/workflow parse) over the changed workflow, when it runs, then `release.yml` parses with no error and declares every input the reusable workflow requires.
- Given the two PRs, when they are reviewed, then the Builds PR is mergeable on its own and the Tenants PR pins only a SHA that exists on `Hexalith.Builds` `main`.

## Spec Change Log

### 2026-07-25 — iteration 1 (adversarial review loopback)

**Triggering findings.** Three parallel review layers independently confirmed two defects the spec should have prevented:

1. `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` asserts the *superseded* release contract (`workflow_run`, `domain-release.yml@main`, `--skip-duplicate` present, `test-projects` forbidden, Builds refs must equal `main`). It runs in Tenants CI as a unit-test project. The change turns CI red, and because the new `verify-source` job requires a successful exact-SHA push CI run, the release becomes permanently undispatchable — the change disables the path it exists to fix.
2. Tenants' `.releaserc.json` carries `@semantic-release/changelog` and `@semantic-release/git`, which EventStore deliberately omits. The `git` plugin commits `CHANGELOG.md` and pushes to `main` during `prepare`, before `publishCmd`. The publish-phase preflight then re-proves "source is still the live main tip" and fails — after the tag is pushed — stranding the version with nothing published.

**What was amended.** The Code Map gained the governance test, the four other copies of the package inventory, and the `.releaserc.json` plugin set. The task list gained: rewriting the governance test to pin the *new* invariants, a manifest-consistency test, removal of the two semantic-release plugins, hardening of the manifest loader, an early `expected-package-count` guard in `domain-release.yml`, the missing forwarding assertion, and two doc corrections.

**Known-bad state avoided.** Shipping a change that (a) reds CI and deadlocks its own release gate, and (b) deterministically strands the first release version after a tag push. Also avoided: "fixing" the governance test by mechanically refreshing its literals, which would leave every new invariant unverified.

**KEEP — must survive re-derivation.**
- The caller-declared `--expected-package-count` design: required, no default, validated by `^[1-9][0-9]*$`, *declared* rather than derived from the manifest so inventory drift fails closed. Do not replace with `len(packages)`.
- The two `!= 14` call sites in `publication_preflight.py` must both be parameterized; the container phase builds the frozen identity too, so it needs the count.
- `HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT` as the env transport from the reusable workflow to `publish-containers.sh`.
- The `verify-source` job shape copied from EventStore, and `test-projects: ''` (CI already proved the same head).
- Dropping `--skip-duplicate` — collisions are intended behavior per the standard. Do not restore it.
- The 5-package manifest and the manifest-driven pack script.
- Builds test suite structure: `FIXTURE_PACKAGE_COUNT` constant, and the five added test methods.

## Design Notes

**Why the Builds change is unavoidable.** `_load_package_identity` and `_require_absent_destinations` both reject any manifest whose ID count is not exactly 14 — EventStore's inventory, introduced in `cf04c41 fix(release): harden publication boundaries (#24)`. Tenants ships 5 packages, so its first `verifyReleaseCmd` would fail with `package-inventory-mismatch` no matter how correct its manifest is. The count is caller-supplied rather than derived from `len(packages)` so an accidental manifest edit still fails closed:

```python
parser.add_argument("--expected-package-count", required=True, type=_positive_integer)
```

**Ordering.** PR 2 cannot be finalized until PR 1 merges, because `builds-execution-sha` must name a commit reachable on `Hexalith.Builds` `main` and the action byte-compares its helpers against that exact SHA via `raw.githubusercontent.com`. Author PR 2 with a placeholder, then substitute the merged SHA in both places before pushing.

**EventStore is untouched but affected later.** Making the argument required means `Hexalith.EventStore/scripts/validate-publication-preflight.sh` must pass `--expected-package-count 14` whenever EventStore repins from `cf04c419…` to a newer Builds commit. EventStore is pinned to the old SHA, so nothing breaks now. Out of scope here — record it as follow-up work.

**Protected environment — RESOLVED 2026-07-25.** `Hexalith.Tenants` had **zero** GitHub environments, so referencing `production` would have auto-created it *without* protection rules and the human approval authority the standard relies on would not have existed. Created on human request, mirroring `Hexalith.EventStore/environments/production` exactly:

```text
required_reviewers: jpiquot     prevent_self_review: false
branch_policy:      main only   can_admins_bypass:   false
```

`prevent_self_review` stays `false` deliberately: with a single reviewer who is also the dispatching operator, `true` would make every release unapprovable. Revisit when a second reviewer exists.

## Verification

**Commands:**
- `cd references/Hexalith.Builds && pwsh -NoProfile -File ./Tools/test-publish-containers.ps1` -- expected: all tests pass (baseline before changes: 44 passed).
- `cd references/Hexalith.Builds && python3 -m unittest discover -s Github/publish-containers/tests -p 'test_*.py' -v` -- expected: same result without pwsh.
- `cd references/Hexalith.Builds && bash -n Github/publish-containers/publish-containers.sh` -- expected: no syntax errors.
- `cd references/Hexalith.Tenants && bash -n scripts/validate-publication-preflight.sh && python3 -m json.tool tools/release-packages.json >/dev/null && python3 -m json.tool .releaserc.json >/dev/null` -- expected: all exit 0.
- `cd references/Hexalith.Tenants && python3 -c "import ast,pathlib; ast.parse(pathlib.Path('scripts/pack-release-packages.py').read_text())"` -- expected: exit 0.
- `cd references/Hexalith.Tenants && git diff --check` -- expected: no whitespace or line-ending errors.

**Results (2026-07-25):**
- `pwsh -NoProfile -File ./Tools/test-publish-containers.ps1` -- PASS, "Container publisher fixture tests passed."
- `python3 -m unittest discover -s Github/publish-containers/tests -p 'test_*.py'` -- PASS, **49 tests** (baseline before changes: 44; +5 new test methods).
- `bash -n Github/publish-containers/publish-containers.sh` -- PASS.
- `bash -n scripts/validate-publication-preflight.sh`; `python3 -m json.tool` on `tools/release-packages.json` and `.releaserc.json`; `ast.parse` on `scripts/pack-release-packages.py`; `yaml.safe_load` on `.github/workflows/release.yml` (jobs: `verify-source`, `release`) -- all PASS.
- `git diff --check` in both repos -- clean; `git ls-files --eol` confirms LF for every changed/added file.

**Results after iteration 1 (2026-07-25):**
- `python3 -m unittest discover -s Github/publish-containers/tests -p 'test_*.py'` -- PASS, 49 tests.
- `dotnet build tests/Hexalith.Tenants.Contracts.Tests/...csproj -v:m` -- **Build succeeded, 0 Warning(s), 0 Error(s)** (`TreatWarningsAsErrors` is on).
- Full `Hexalith.Tenants.Contracts.Tests` assembly -- **113 total, 1 failed**. The single failure is `Release_workflow_packs_validates_and_publishes_only_expected_packages`, asserting `IsFullCommitSha('BUILDS_EXECUTION_SHA_PLACEHOLDER')`. **This is the new guard working**, not a regression.
- Same assembly with a valid 40-hex SHA temporarily substituted -- **11/11 `PackageGovernanceTests` pass, 0 failed**, proving every other rewritten assertion and the new manifest test hold. `release.yml` was restored to its committed state immediately after (`git diff --quiet` confirmed).
- Verifying this also caught a bug in the new test itself: `"@semantic-release/github"` contains the substring `"@semantic-release/git"`, so the plugin-absence assertions now match quoted tokens.

**Matrix audit disposition:**
- Rows 1-6 (inventory size, duplicate IDs, missing/non-positive count) -- covered by `test_declared_package_count_gates_the_inventory_for_any_module_size`, `test_duplicate_ids_fail_closed_at_a_small_module_inventory`, `test_manifest_identity_honours_the_declared_count_and_rejects_drift`, `test_expected_package_count_argument_is_required_and_must_be_positive`, `test_absent_or_invalid_expected_package_count_blocks_publication`. All ran and passed in the 49-test run above.
- Rows 7-9 (dispatch off `main`, stale dispatch, no green CI) -- **no automated coverage.** Enforced by the caller-owned `verify-source` job and proven only when GitHub runs a dispatch. `Hexalith.Tenants` CI calls `domain-ci.yml`, which has no hook able to execute a workflow-contract test, and `Hexalith.EventStore` ships the identical guards with the same absence of tests. Accepted as workflow-proven by human decision on 2026-07-25.
- `scripts/pack-release-packages.py` is additionally exercised on every push/PR: `domain-ci.yml:168` runs it under `run-consumer-validation: true`, which Tenants sets, so a missing or malformed `tools/release-packages.json` fails CI.

**Manual checks (if no CLI):**
- `release.yml`: the `uses:` revision and `builds-execution-sha` value are byte-identical 40-hex strings, and that commit resolves on `Hexalith.Builds` `main`.
- `verify-source` runs with `permissions: {actions: read, contents: read}` and is named in the release job's `needs:`, so it cannot be skipped.
- Only `NUGET_API_KEY`, `HEXALITH_ZOT_USERNAME` and `HEXALITH_ZOT_API_KEY` are mapped explicitly under `secrets:`; `secrets: inherit` appears nowhere.
- The 5 IDs in `tools/release-packages.json` match the projects previously listed in `PACKAGE_PROJECTS` exactly — same set, no additions.

## Suggested Review Order

**The startup failure itself**

- Entry point: the two literals that must be one identical 40-hex Builds commit.
  [`release.yml:86`](../../references/Hexalith.Tenants/.github/workflows/release.yml#L86)
- The caller-owned source proof, kept outside the protected job so bad dispatches never reach secrets.
  [`release.yml:18`](../../references/Hexalith.Tenants/.github/workflows/release.yml#L18)
- Release becomes an operator action; `needs:` makes the proof unskippable.
  [`release.yml:74`](../../references/Hexalith.Tenants/.github/workflows/release.yml#L74)

**Why Builds had to change first**

- The required argument replacing EventStore's hard-coded 14; no default, by design.
  [`publication_preflight.py:523`](../../references/Hexalith.Builds/Github/publish-containers/publication_preflight.py#L523)
- Rejects zero, negatives, padding and non-digits before the count is trusted.
  [`publication_preflight.py:503`](../../references/Hexalith.Builds/Github/publish-containers/publication_preflight.py#L503)
- Both former `!= 14` sites; the container phase builds the frozen identity too.
  [`publication_preflight.py:431`](../../references/Hexalith.Builds/Github/publish-containers/publication_preflight.py#L431)
- Fails closed early — rejecting later would strand a half-published release.
  [`domain-release.yml:158`](../../references/Hexalith.Builds/.github/workflows/domain-release.yml#L158)
- Env transport from the reusable workflow into the container phase.
  [`publish-containers.sh:96`](../../references/Hexalith.Builds/Github/publish-containers/publish-containers.sh#L96)

**Publication gates**

- Preflight now runs before the first NuGet write; `--skip-duplicate` is gone.
  [`.releaserc.json:12`](../../references/Hexalith.Tenants/.releaserc.json#L12)
- `${VAR-}` not `${VAR:-}`: a set-but-empty value must not pass vacuously.
  [`validate-publication-preflight.sh:47`](../../references/Hexalith.Tenants/scripts/validate-publication-preflight.sh#L47)
- Manifest data reaches `dotnet pack` argv, so it is confined like untrusted input.
  [`pack-release-packages.py:60`](../../references/Hexalith.Tenants/scripts/pack-release-packages.py#L60)

**What now enforces all of it**

- The pin guard: fails today on the placeholder, by design.
  [`PackageGovernanceTests.cs:323`](../../references/Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#L323)
- Ties the manifest to the four other copies of the inventory.
  [`PackageGovernanceTests.cs:433`](../../references/Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#L433)
- Quoted tokens — `"@semantic-release/github"` contains `"@semantic-release/git"`.
  [`PackageGovernanceTests.cs:379`](../../references/Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#L379)
