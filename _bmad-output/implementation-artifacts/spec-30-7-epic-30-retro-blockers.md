---
title: 'Epic 30 retro blockers: runner-faithful recovery render and pinned privileged actions'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-7-epic-30-retro-blockers'
baseline_commit: '7b6f82690cb5d5b4f52643d27159d0e5b7c1909b'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current-set recovery rebuilds the tagged renderer inputs with `git show` plus `write_text`, which writes LF bytes. Release runners materialize `redis-stack.conf` as CRLF through `.gitattributes`, so kustomize names the ConfigMap `redis-stack-config-444ff7t4hh` on the runner (the published v4.0.0 name) but `…-tfmk6ftkcm` in recovery, and every real recovery fails closed (retro F1). Separately, the privileged production recovery job runs `initialize-dotnet@main` and `initialize-build@main`, third-party actions float on major tags in both release workflows, the release checkout persists its write token, the base image floats, and the inventory test codifies `@main` (retro F2, F13).

**Approach:** Materialize the tagged inputs with `git archive <tag> -- tools/render-production-deployment.ps1 deploy/kubernetes` into private scratch, and fail closed with a clear message when the source is not its own checkout or lacks the tag commit. Prove this with real-git, real-kubectl fixtures that are red on today's code. Pin every action in `release.yml` and `recover-partial-release.yml` to a full SHA, set `persist-credentials: false` on the release checkout, digest-pin `ContainerBaseImage`, record the curl provenance, and replace the `@main` allow-list with a job-aware rule.

## Boundaries & Constraints

**Always:**
- Builds actions pin exactly `ae77d5690a9411a2154fa4ed5231edc373942573`, the SHA at `release.yml:97`. Each pinned path is verified at that SHA locally and through the GitHub contents API.
- A third-party SHA is the commit the currently used major tag resolves to (`git ls-remote`), with a `# vX.Y.Z` comment. No version moves.
- Recovery stays byte-strict and fail-closed: a retained deployment rendered from LF bytes must still be refused.
- The new fixtures are run red against the baseline contract before the fix, and that red run is recorded.
- Story 30.7 is registered in `epics.md` and `sprint-status.yaml` with minimal additive diffs and no reflow.

**Never:**
- Dispatch the release or recovery workflow, or change GitHub, environment or registry settings.
- Change `ci.yml` or other workflows' refs.
- Choose a new Builds pin or edit the recovery Builds allowlist (A6). Change the production approval posture (A5).
- Work on A3–A11 (`epic-30-retro-item-93..101`).
- Rewrite historical evidence, or flip the retro verdict. The flip is an owner decision once this story lands.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Runner checkout | The retained deployment was rendered from an attribute-applied checkout with CRLF `redis-stack.conf` | `verify_tagged_deployment` passes | N/A |
| LF retained render | The retained deployment was rendered from LF blob bytes | Refused: "Retained deployment differs" | Fail closed |
| Tag commit absent | A shallow clone that lacks the tag commit | Refused before rendering, naming the SHA and the fix (full tagged history) | Fail closed |
| Not its own checkout | The `submodules: false` shape: an empty directory inside another repository | Refused before rendering: "not its own Git checkout" | Fail closed |
| Non-regular member | A symlink under `deploy/kubernetes` | Refused: not a regular file | Fail closed |
| Moving ref in a privileged job | `@main` or `@vN` in a job holding `id-token: write` or `contents: write` | The inventory test fails and names the workflow, job and ref | N/A |

</frozen-after-approval>

## Code Map

Anchors were derived on 2026-11-16 at `7b6f8269`. Re-derive them before editing.

- **`tools/container-publication-contract.py:578-598` (`verify_tagged_deployment`)**
  - Today it calls `ls-tree` and `git show` through `run()`. `run()` uses `text=True`, which normalizes newlines, and redacts output, so it cannot carry file bytes.
  - Use `subprocess.run` with bytes for `git rev-parse --show-toplevel`, `cat-file -e <sha>^{commit}` and `archive --format=tar`.
  - Extract with `tarfile`. The pax global header is not a member. Directories are skipped. Keep the unsafe-path refusal.
  - Keep the render command and the byte compare unchanged.
  - The caller is `verify_bundle:656`. The tag must equal `HEAD` (`:636-637`), and the workflow uses `fetch-depth: 0`.
- **Why fixtures missed F1**
  - `publication_contract_test.py:155-157` fakes `kubectl` with static YAML that ignores its inputs.
  - `partial_release_recovery_test.py:156-161` fakes `git show` and `ls-tree` from LF local files.
  - Both sides then render the same bytes. The local working tree is LF (stale checkout), while a fresh worktree is CRLF.
- **`partial_release_recovery_test.py`**
  - `boundary()` patches `C.subprocess.run` with `native` (pass-through).
  - The real git calls will hit `self.root`, which is not a repository today.
  - Make `self.root` a real repository holding the tagged inputs, and translate `fixtures.SOURCE_SHA` to its commit only in the `cat-file`/`archive` argv.
  - Drop the dead `ls-tree` fake.
- **Workflows**
  - `recover-partial-release.yml` uses `@main` at `:120`, `:167`, `:250` and `:253`.
  - Third-party actions:
    - `actions/checkout@v7`: `:35,40,78,84,185`
    - `actions/upload-artifact@v7`: `:57,295`
    - `azure/setup-kubectl@v4`: `:172`
    - `docker/setup-qemu-action@v3`: `:194`
    - `NuGet/login@v1`: `:232`
  - `release.yml`:
    - `actions/checkout@v7`: `:43,49,79`. Add `persist-credentials: false` at `:79`.
    - `azure/setup-kubectl@v4`: `:85`
    - `actions/setup-dotnet@v6`: `:91`
    - `docker/setup-qemu-action@v3`: `:118`
    - `NuGet/login@v1`: `:172`
    - `actions/upload-artifact@v7`: `:230,242,257,265`
- **Resolved SHAs (2026-11-16)**

  | Action | Tag | Pinned SHA |
  | --- | --- | --- |
  | `actions/checkout` | v7.0.1 | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
  | `actions/upload-artifact` | v7.0.2 | `cf430e030ddbb5b0abf93d22962f4752f3646cd9` |
  | `actions/setup-dotnet` | v6.1.0 | `0f4a354dd5547a28c70b7853dbd95d5b67886e73` |
  | `azure/setup-kubectl` | v4.0.1 | `776406bce94f63e41d621b960d78ee25c8b76ede` (peeled) |
  | `docker/setup-qemu-action` | v3.7.0 | `c7c53464625b32c7a7e944ae62b3e17d2b600130` |
  | `NuGet/login` | v1.2.0 | `8d196754b4036150537f80ac539e15c2f1028841` (peeled) |

  Builds `ae77d569` already pins `checkout` and `setup-dotnet` to the same SHAs. All three Builds action paths exist at `ae77d569` (contents API checked).
- **`persist-credentials: false` is safe for semantic-release 25.0.9.** `get-git-auth-url.js` falls back to `x-access-token:$GITHUB_TOKEN`, the same token the checkout persists today.
- **`CiTestInventoryTests.cs`**
  - `:1093-1191` holds `Workflows_ExternalActions_UseAllowedRefs`, the matrix and `IsAllowedExternalActionReference`. The policy is line-based: Builds must use `@main` or listed SHAs; third-party must use `@vN` or the one reviewed upload-artifact SHA.
  - Text pins `NuGet/login@v1` at `:547`, `:624`, `:1454` and `:1621` (OIDC step locator).
  - Reuse the parser helpers: `GetWorkflowJobNames:1760`, `GetBlockMappingBody:2069`, `GetImmediateMappingLines:2094`, `ReadMappingProperty:2108`.
  - No workflow outside these two has a job with `contents: write` or `id-token: write`.
  - `ci.yml` keeps `initialize-build@main` in unprivileged jobs.
- **Other text pins and docs**
  - `release_dispatch_test.py:82,87` pins `NuGet/login@v1`.
  - `docs/dev/release-runbook.md:916` cites `NuGet/login@v1`. The section around `:922` describes the tagged render.
- **Curl provenance (retro F13; deferred-work R3-B10 at `deferred-work.md:6151`)**
  - The pins live at `tools/stage-qualification-curl.py:26-35`.
  - The `49c913da` hashes equal the GitHub asset digests of `stunnel/static-curl` 8.20.0. Both assets were uploaded 2026-04-29 and are unchanged since, and fresh downloads hash to the same values.
  - The `72af4eaf` values were the release notes' inner-`curl` binary checksums, so they could never match the `.tar.xz` download.
  - No tool hash-pins the stager file.
- **`ContainerBaseImage`**
  - It is declared only at `Directory.Build.targets:10`. Nothing hashes that file or the base reference.
  - Builds validators compare only provenance labels.
  - SDK 11 splits `ContainerBaseImage` into tag plus digest, and the label parser is `Split(':', 2)`.
  - Index digest (manifest list), resolved 2026-11-16: `sha256:d39b72ad1534fa0faf52b488bffb0762df7ba269d120f0338713d4dda2a41bf5`.
  - Dependabot has no Docker ecosystem, so the pin needs a documented refresh rule.

## Tasks & Acceptance

**Execution:**
- [x] `tools/container-publication-contract.py` -- replace the input materialization in `verify_tagged_deployment` with a `git archive` export that checks top-level, commit presence and regular-file members -- F1.
- [x] `tests/tooling/publish_containers/tagged_deployment_test.py` (new) -- cover every matrix row except the last. Use a temporary repository with the real `.gitattributes`, renderer and `deploy/kubernetes`, a tag, and a CRLF clone. Render through `archive-release-evidence.py` `render_deployment` with real `pwsh` and `kubectl`. Add positive controls: the clone is CRLF, the blob is LF, and the CRLF and LF renders differ. Run it red on the baseline first -- red-first proof.
- [x] `tests/tooling/publish_containers/partial_release_recovery_test.py` -- make the fixture root a real tagged repository and translate the synthetic SHA in the `archive`/`cat-file` argv -- keeps the existing suite on the real Git path.
- [x] `.github/workflows/recover-partial-release.yml`, `.github/workflows/release.yml` -- pin as mapped, and add `persist-credentials: false` on the release checkout -- F2, F13.
- [x] `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
  - Make the ref policy job-aware. A job is privileged when it has `id-token: write`, `contents: write` or `write-all`, including through inherited or default permissions. In privileged jobs, and in every job of the two release workflows, only reviewed `owner/repo@<40-hex>` identities are allowed, and `@main` is rejected.
  - Invert the `:1132` and `:1140` rows for privileged context.
  - Assert that both workflows contain privileged jobs, so the rule is never vacuous.
  - Add facts that every release-workflow checkout sets `persist-credentials: false` and that the base image is digest-pinned.
  - Update the `NuGet/login` pins and the locator.
  -- This is the A2 guard.
- [x] `tests/tooling/release_dispatch/release_dispatch_test.py` -- update the `NuGet/login` pin -- it follows the workflow.
- [x] `tools/stage-qualification-curl.py` -- add a provenance comment with no hex values -- F13.
- [x] `Directory.Build.targets` -- set `aspnet:11.0-alpine@sha256:d39b72ad…41bf5` and a comment with the refresh command -- F13.
- [x] `docs/dev/release-runbook.md` -- describe the `git archive` render and its refusals, update the `NuGet/login` text, and add the base-image digest refresh rule -- operator truth.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- resolve R3-B10 with evidence.
- [x] `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `epic-30-context.md` -- register Story 30.7 (row and `story_execution_order`). After green CI on the exact final commit, set items 91 and 92 to `done` with minimal diffs.

**Acceptance Criteria:**
- Given the new fixtures, when they run against the baseline contract, then the CRLF, LF-refusal, absent-commit and enclosing-repository cases fail. When they run against the fixed contract, all of them pass.
- Given both release workflows, when `CiTestInventoryTests` runs, then every external action there is a reviewed full SHA, `@main` is rejected in privileged context, and unprivileged `ci.yml` keeps its current policy.
- Given the final commit, when the tooling suites, `CiTestInventoryTests` and push CI run, then all are green. Items 91 and 92 move to `done` only after that.

## Implementation Notes

Implemented 2026-11-16 on top of `7b6f8269`. Anchors were re-derived before editing, and all of them held.

**Red-first run (recorded before the contract changed).** With `tools/container-publication-contract.py` byte-identical to `7b6f8269`, `PATH=~/.dotnet/tools:$PATH TERM=dumb python3 -B -m unittest discover -s tests/tooling/publish_containers -p 'tagged_deployment_test.py' -v` ran 6 tests and reported `FAILED (failures=4, errors=1)`. Only the positive control passed.

| Case | Baseline result |
| --- | --- |
| Runner checkout (CRLF render) | ERROR: `Retained deployment differs` (the baseline re-rendered LF blob bytes) |
| LF blob render | FAIL: `ValueError not raised` (the baseline accepted LF bytes) |
| Shallow clone without the tag commit | FAIL: got `git failed (128): fatal: not a tree object`, with no SHA and no fix |
| Empty directory inside another repository | FAIL: got `Tagged production renderer input is missing.` (Git fell through to the enclosing clone and resolved the pathspec under the subdirectory) |
| Symlink input | FAIL: got `Retained deployment differs` (the baseline wrote the link text as a regular file) |

The positive controls held on both sides. The clone's `redis-stack.conf` is CRLF and its blob is LF. The CRLF render names `redis-stack-config-444ff7t4hh`, the published v4.0.0 name, and the LF render names `redis-stack-config-tfmk6ftkcm`. After the fix, the same command passes 6/6.

**What changed.**
- `verify_tagged_deployment` now runs the following through byte-mode `subprocess.run`, never through `run()`:
  - `rev-parse --show-toplevel`, which must equal the source;
  - `cat-file -e <sha>^{commit}`;
  - `archive --format=tar <sha> -- tools/render-production-deployment.ps1 deploy/kubernetes`.
- `tarfile` consumes the pax global header. Directories are skipped, unsafe paths and non-regular members are refused, and the render command and byte compare are unchanged.
- The recovery fixture root is now a real repository holding `.gitattributes`, the renderer and `deploy/kubernetes`. Only the `cat-file`/`archive` argv translates the synthetic SHA. The dead `ls-tree` fake is gone.
- Every action in both release workflows is pinned as mapped, and the release checkout sets `persist-credentials: false`.
  - The three Builds paths exist at `ae77d569` locally and through the contents API (blob SHAs `10e5dc67`, `417525eb`, `04f1bfa3`).
  - Every third-party SHA was re-resolved with `git ls-remote` and equals the Code Map table.
  - semantic-release 25.0.9 sees only `GITHUB_TOKEN` in its step, so it pushes through `x-access-token:$GITHUB_TOKEN`. The repository is public, so the preflight's anonymous `ls-remote` still works.
- The inventory guard is now job-aware:
  - A job is privileged through its own, inherited or default permissions. Privileged jobs and every release-workflow job need a reviewed `owner/repo@<40-hex>` identity plus a matching `# vX.Y.Z` comment for third-party actions.
  - Every `uses:` line must be attributed to a job.
  - New facts: a privileged-matrix theory that inverts the `initialize-build@main` and `@ae77d569` rows, non-vacuity, privilege-detection cases, a checkout `persist-credentials` check and a digest-pinned base-image check.
  - The OIDC locator now matches `NuGet/login@` by action.
- `ContainerBaseImage` is `aspnet:11.0-alpine@sha256:d39b72ad…41bf5`. `docker buildx imagetools inspect` re-confirmed that index digest on 2026-11-16.
- The curl provenance comment has no hex values. R3-B10 is resolved with upstream asset-digest evidence.

**Negative controls.** Each control mutated one real input in place, ran the inventory class, and restored the input byte-identical (SHA-256 checked). Each produced exactly one failure, which named the workflow, job and reference:
- `initialize-build@main` in `recover`;
- `actions/checkout@v7` in the read-only `release-source` job of `release.yml`;
- a `# v4` comment on the pinned kubectl action;
- the release checkout without `persist-credentials: false`;
- an undigested base image.

**Real SDK proof of the base pin.** `dotnet publish` of `Hexalith.Memories.AccessTelemetry.Clock` succeeded as the single-RID Docker archive and as the two-RID OCI archive, with the release's arguments, the approved `ae77d569` provenance targets and a scratch `--artifacts-path`. Both builds ran "on top of base image `mcr.microsoft.com/dotnet/aspnet:11.0-alpine@sha256:d39b72ad…`". The contract's `archive_graph` with the approved `ae77d569` validator accepted both children. Each child's `org.opencontainers.image.base.name` keeps the full pinned reference.

**Results on the final working tree.**

| Check | Result |
| --- | --- |
| `publish_containers` | 171/171, zero skips (165 before + 6 new) |
| `release_dispatch` | 17/17 |
| `release_cutover` | 34/34 |
| `release_packages` | 28/28 |
| `release_preflight` | 50/50 |
| `publish_nuget` | 32/32 |
| `ci_alignment` | 15/15 |
| `builds_publication_qualification` | 70/70 |
| `line_endings` | 4/4 |
| `access_telemetry_functional_retention` | 162/162 |
| Lifecycle stager methods | 2/2 |
| `CiTestInventoryTests` | 161/161 (118 before + 43 new cases in 5 new methods); 166/166 after the review fixes |
| `actionlint` 1.7.12 | clean |
| Runner-shaped tree (`git archive HEAD` CRLF materialization, no submodules, not a checkout) | `tagged_deployment_test` 6/6; `partial_release_recovery_test` 52/52 |

**Review fixes (2026-11-16).** The table above is the first pass. After the coordinator review:
- **Contract:** `require_own_checkout` now runs at the start of `verify_bundle` and in `verify_tagged_deployment`. A failing `rev-parse` gets its own refusal carrying Git's redacted stderr, and the absent-commit refusal now appends that stderr. An unresolvable `refs/tags/v<version>` names the tag and the `fetch-depth: 0` requirement before any bundle read.
- **Fixtures:** new cases for a missing renderer, a missing production kustomization, an unreadable source, the two `verify_bundle` refusals and a mode fact (only `100644`/`100755`) over the real renderer inputs. The recovery fixture root now carries the `v` tag.
- **Inventory guard:** the per-workflow body is a helper. A synthetic `ci.yml` theory proves the privilege term and the local-action refusal. `*.yaml` files are scanned. Reviewed pins are also allowed unprivileged, so the old `initialize-build@ae77d569` row is now true. The semantic-release `GITHUB_TOKEN` is asserted, and the base image equals the reviewed constant.
- **Reruns:** `tagged_deployment_test` 12/12, `partial_release_recovery_test` 52/52 (the only modules that exercise the edited functions) and `CiTestInventoryTests` 166/166. Dropping the privilege term turns 3 theory cases red. The wider lanes were not rerun; the coordinator runs full verification.

**Closed (2026-11-16).** The code landed in `e4b2b08edacf1d10337258ffb6949711cc5987ca`. On that exact commit, push CI `43862752990` (CI), `43862752951` (CodeQL), `43862752937` (Commitlint) and `43862752591` (Qualify Builds publication) all `success` on `e4b2b08edacf1d10337258ffb6949711cc5987ca`. The CI test-unit-contract job ran `publish_containers` 177/177 with no skips, and Cli.Tests 618/618. A follow-up records commit then moved items 91 and 92 and the Story 30.7 row to `done`. No workflow was dispatched, and no GitHub, environment or registry setting changed.

## Spec Change Log

## Review Triage Log

Review pass 1 (2026-11-16): Blind Hunter (BH), Edge Case Hunter (ECH) and Verification Gap (VG) layers ran on the diff from `7b6f8269`. Verdicts were set by the parent session after checking each finding at its location.

| # | Layer | Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- | --- | --- |
| 1 | VG | The enforcement loop never consults `IsPrivilegedWorkflowJob` for a real job: release files short-circuit on `releaseWorkflow`, and no other job is privileged, so dropping that term passes every test | medium | Pre-verified by the layer. The helper tests call it in isolation only | patch (P5) |
| 2 | BH, ECH | `rev-parse` and `cat-file` failures drop git's stderr, and any `rev-parse` failure (dubious ownership, corrupt `.git`) is reported as "not its own Git checkout" | low | `contract:594-598`: the `returncode` branch shares the mismatch message, and stderr is not read | patch (P1) |
| 3 | BH | `verify_bundle` reads `rev-list`, `HEAD` and `git show` from `source` before the new checks run. A shallow or tagless clone fails there with a generic `git failed (128)` | low | `contract:663-674` precede `verify_tagged_deployment`. Recovery still fails closed before any destination write | patch (P2) |
| 4 | BH | No fixture covers "Tagged production renderer input is missing" (the archive-error branch or the post-extraction branch) | low | No assertion in `tests/tooling/publish_containers` | patch (P3) |
| 5 | BH | Recovery now refuses non-regular members, while a release renders through symlinks. A future symlink would publish, then be unrecoverable | low | `git ls-files -s deploy/kubernetes` has no `120000` entries today. This change introduced the asymmetry | patch (P4) |
| 6 | BH | The unprivileged policy refuses the reviewed SHA pins, so hardening `ci.yml` would fail | low | `IsAllowedExternalActionReference` accepts only `@vN`, Builds `@main` and listed identities; row `:1140` still says `false` | patch (P6) |
| 7 | ECH | Workflows named `*.yaml` are never scanned | low | `Directory.GetFiles(..., "*.yml")`; none exist today | patch (P7) |
| 8 | ECH | A local `./` action in a pinned job skips the reviewed-identity check, so its nested actions run unreviewed | low | The local-skip `continue` precedes the pinned check. No workflow uses a local action today | patch (P8) |
| 9 | BH | Nothing asserts that the semantic-release step carries `GITHUB_TOKEN`, which `persist-credentials: false` now depends on | low | `ReleaseWorkflow_RunSemanticReleaseStep_UsesCanonicalCommand` asserts only `NUGET_API_KEY` | patch (P9) |
| 10 | BH | The runbook's refresh steps name only the inventory table. The pin copies in `release_dispatch_test.py`, the `NuGetLoginAction` constant and the runbook table go unlisted | low | Runbook section "Release Workflow Action And Base-Image Pins" | patch (P10) |
| 11 | BH | The base-image fact checks format only, so a platform-child digest passes. The refresh rule names no owner or trigger | low | The regex accepts any `@sha256:<64 hex>` | patch (P11) |
| 12 | BH | The regenerated `epic-30-context.md` still says "seven-project" Coverlet union | low | `tools/test-projects.unit-contract.txt` lists eight (retro F8). The "never `@main`" claim is scoped to `domain-ci.yml`/`prepare-domain-release`, which are pinned, so that half is false | patch (P12) |
| 13 | BH | The context rewrite dropped Epic 31's runtime secret-store migration and the Story 27.3 four-archive input rule | low | Compared with the 637eaa1b text | patch (P13) |
| 14 | BH | The `epics.md` 30.7 entry lacks the Epic 30 checkpoint table, and its ACs omit `persist-credentials`, the base-image pin and curl provenance | low | Epic 30 evidence rule; Stories 30.1–30.6 carry tables | patch (P14) |
| 15 | ECH | The spec says the recovery fixture root is a "tagged" repository, but it creates no tag | low | `partial_release_recovery_test.py:110-115`. Tag resolution stays a mocked `run` call | patch (P15) |
| 16 | BH | The prepareCmd build and publication hooks still run with `NUGET_API_KEY`, `HEXALITH_ZOT_API_KEY` and `GITHUB_TOKEN` in env. `persist-credentials` removes only the `.git/config` copy | medium | `release.yml` "Run semantic-release" env. Pre-existing half of F13, not caused by this story | defer (DW) |
| 17 | BH | A moving action in an unprivileged main-branch job could seed a `nuget-*` Actions cache entry that the pinned prepare action restores | maybe-false (medium if true) | Settle by reading `prepare-domain-release@ae77d569` cache key and restore-keys, and checking whether `ci.yml` jobs on `main` save `nuget-*` keys | defer (DW) |
| 18 | BH | The real-kubectl fixture runs before CI installs the pinned kubectl | low; rejected | `ubuntu-latest` ships kubectl. The fixture compares two renders from one binary, so the version does not matter, and a missing binary fails loudly. The fix needs a `ci.yml` step reorder plus inventory edits | reject |
| 19 | BH, ECH | Privilege ignores `secrets.*`, `environment:`, and `packages`/`actions`/`security-events` write | low; rejected | No non-release job references a non-`GITHUB_TOKEN` secret or an environment (grep). The intent defines privilege as `id-token`/`contents` write. Broadening would add detection branches and pull in `codeql.yml` | reject |
| 20 | BH, ECH | The base-image override scan misses command-line `-p:ContainerBaseImage` | false | The only command-line override is the Story 27.4 functional-retention qualification build (`Debug`, `LocalSourceContainerArchive`). No release tool passes `ContainerBaseImage` | reject |
| 21 | BH | The authoritative spec is absent from the change set, and it lacks File Scope/File List | false | The spec is in the working tree, is committed with the change, and was excluded from the review diff by design as the claims file. Commits are trailerless, so the scope gates no-op by design | reject |
| 22 | ECH | A gitlink under `deploy/kubernetes` is archived as an empty directory, giving a misleading refusal | false | The release checkout uses `submodules: false` and materializes the same empty directory, so both renders see identical inputs | reject |
| 23 | ECH | `export-ignore` or `export-subst` in the tagged attributes would make the archive diverge | low; rejected | The tagged `.gitattributes` sets neither. A divergence fails closed (refusal). A guard needs attribute probing | reject |
| 24 | ECH | `run:` block text starting with `uses:` is misread as an action | low; rejected | This regex behavior predates the change, and no such text exists today. A fix needs block-scalar parsing | reject |

## Design Notes

`git archive` reads the archived tree's own `.gitattributes`, so its bytes are exactly what the release runner's checkout of that commit produced. `kubectl kustomize` hashes those bytes into ConfigMap names. That makes the archive both necessary and sufficient for byte-equal re-rendering. A real checkout of the tag would also produce the right bytes, but it would make recovery depend on mutable working-tree state.

## Verification

**Commands:**
- `PATH=~/.dotnet/tools:$PATH TERM=dumb python3 -B -m unittest discover -s tests/tooling/publish_containers -p '*_test.py'` -- expected: OK. Run the same command for `release_dispatch`, `release_cutover`, `release_packages`, `release_preflight`, `publish_nuget`, `ci_alignment` and `builds_publication_qualification`.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net11.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: 0 failed.
- `actionlint .github/workflows/release.yml .github/workflows/recover-partial-release.yml` -- expected: clean.
- `gh run list --commit $(git rev-parse HEAD)` -- expected: push CI `success`.
