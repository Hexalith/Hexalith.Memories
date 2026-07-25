---
title: 'Restore a policy-compliant Release pipeline for Hexalith.Tenants'
type: 'bugfix'
created: '2026-07-25'
status: 'draft'
review_loop_iteration: 0
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

**Execution — PR 1, `references/Hexalith.Builds`:**
- [ ] `Github/publish-containers/publication_preflight.py` -- add required `--expected-package-count` (positive int, no default); replace both `!= 14` literals and the two error messages with the supplied count -- removes the EventStore-specific constant while keeping the check fail-closed.
- [ ] `Github/publish-containers/publish-containers.sh` -- read an expected-count input and forward it to the `--phase container` invocation, failing closed when absent or non-positive -- the container phase must enforce the same inventory identity as verify/publish.
- [ ] `Github/publish-containers/tests/*.py` -- parameterize the 14-package fixtures and add cases for the size-drift, duplicate-ID, missing-argument and non-positive-count rows of the I/O matrix -- these tests are the only automated guard on this tool.
- [ ] `.github/workflows/domain-release.md` -- document the new required argument and show it in the `verifyReleaseCmd`/`publishCmd` example -- callers copy this contract verbatim.

**Execution — PR 2, `references/Hexalith.Tenants`:**
- [ ] `tools/release-packages.json` -- create with the 5 published IDs and project paths (`Contracts`, `Client`, `Server`, `Testing`, `Aspire`) -- authoritative inventory the preflight freezes and proves absent.
- [ ] `scripts/pack-release-packages.py` -- read the manifest instead of the hard-coded `PACKAGE_PROJECTS` list, failing on duplicates or empty entries -- prevents packed output drifting from the inventory the preflight proves.
- [ ] `scripts/validate-publication-preflight.sh` -- create, mirroring EventStore's wrapper but asserting the Tenants identity (`Hexalith/Hexalith.Tenants`, `registry.hexalith.com/tenants`) and passing `--expected-package-count 5` -- fail-closed bridge between semantic-release and the shared preflight.
- [ ] `.github/workflows/release.yml` -- switch to `workflow_dispatch`, add the caller-owned `verify-source` job (current-`main` tip + exact-SHA green push CI), pin `uses:` to the merged Builds SHA, pass the identical `builds-execution-sha`, add `environment-name: production`, `actions: read`, and `needs: verify-source` -- restores startup and satisfies Release Gates.
- [ ] `.releaserc.json` -- add `verifyReleaseCmd` running the preflight `verify` phase, add the `publish` phase to `publishCmd`, and drop `--skip-duplicate` -- duplicate skipping is forbidden; existing identities are collisions.

**Acceptance Criteria:**
- Given the Builds unit-test suite, when `Tools/test-publish-containers.ps1` runs, then all tests pass with no test asserting a literal inventory size of 14 outside an explicitly-14 EventStore fixture.
- Given `Hexalith.Tenants` `release.yml` after PR 2, when the `uses:` revision and the `builds-execution-sha` input are compared, then both are the same 40-hex literal and neither is a branch, tag, expression or variable.
- Given a `push` to `Hexalith.Tenants` `main`, when CI completes successfully, then no Release run is triggered — release requires an explicit dispatch.
- Given `actionlint` (or equivalent YAML/workflow parse) over the changed workflow, when it runs, then `release.yml` parses with no error and declares every input the reusable workflow requires.
- Given the two PRs, when they are reviewed, then the Builds PR is mergeable on its own and the Tenants PR pins only a SHA that exists on `Hexalith.Builds` `main`.

## Spec Change Log

## Design Notes

**Why the Builds change is unavoidable.** `_load_package_identity` and `_require_absent_destinations` both reject any manifest whose ID count is not exactly 14 — EventStore's inventory, introduced in `cf04c41 fix(release): harden publication boundaries (#24)`. Tenants ships 5 packages, so its first `verifyReleaseCmd` would fail with `package-inventory-mismatch` no matter how correct its manifest is. The count is caller-supplied rather than derived from `len(packages)` so an accidental manifest edit still fails closed:

```python
parser.add_argument("--expected-package-count", required=True, type=_positive_integer)
```

**Ordering.** PR 2 cannot be finalized until PR 1 merges, because `builds-execution-sha` must name a commit reachable on `Hexalith.Builds` `main` and the action byte-compares its helpers against that exact SHA via `raw.githubusercontent.com`. Author PR 2 with a placeholder, then substitute the merged SHA in both places before pushing.

**EventStore is untouched but affected later.** Making the argument required means `Hexalith.EventStore/scripts/validate-publication-preflight.sh` must pass `--expected-package-count 14` whenever EventStore repins from `cf04c419…` to a newer Builds commit. EventStore is pinned to the old SHA, so nothing breaks now. Out of scope here — record it as follow-up work.

**Known residual gap.** `Hexalith.Tenants` currently has **zero** GitHub environments configured. Referencing `production` auto-creates it *without* protection rules, so the human approval authority the standard relies on would not exist. This is a repo-settings action requiring admin, not a code change — flag it, do not perform it silently.

## Verification

**Commands:**
- `cd references/Hexalith.Builds && pwsh -NoProfile -File ./Tools/test-publish-containers.ps1` -- expected: all tests pass (baseline before changes: 44 passed).
- `cd references/Hexalith.Builds && python3 -m unittest discover -s Github/publish-containers/tests -p 'test_*.py' -v` -- expected: same result without pwsh.
- `cd references/Hexalith.Builds && bash -n Github/publish-containers/publish-containers.sh` -- expected: no syntax errors.
- `cd references/Hexalith.Tenants && bash -n scripts/validate-publication-preflight.sh && python3 -m json.tool tools/release-packages.json >/dev/null && python3 -m json.tool .releaserc.json >/dev/null` -- expected: all exit 0.
- `cd references/Hexalith.Tenants && python3 -c "import ast,pathlib; ast.parse(pathlib.Path('scripts/pack-release-packages.py').read_text())"` -- expected: exit 0.
- `cd references/Hexalith.Tenants && git diff --check` -- expected: no whitespace or line-ending errors.

**Manual checks (if no CLI):**
- `release.yml`: the `uses:` revision and `builds-execution-sha` value are byte-identical 40-hex strings, and that commit resolves on `Hexalith.Builds` `main`.
- `verify-source` runs with `permissions: {actions: read, contents: read}` and is named in the release job's `needs:`, so it cannot be skipped.
- Only `NUGET_API_KEY`, `HEXALITH_ZOT_USERNAME` and `HEXALITH_ZOT_API_KEY` are mapped explicitly under `secrets:`; `secrets: inherit` appears nowhere.
- The 5 IDs in `tools/release-packages.json` match the projects previously listed in `PACKAGE_PROJECTS` exactly — same set, no additions.
