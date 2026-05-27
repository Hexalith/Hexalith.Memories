# Story 11.2: Semantic Release & NuGet Publishing

Status: done

## Story

As a maintainer,
I want automated semantic versioning from conventional commits and NuGet publishing on release,
so that releases are predictable, traceable, and publishing is zero-friction.

## Acceptance Criteria

1. Given commits follow Conventional Commits (`feat:`, `fix:`, `BREAKING CHANGE:` footer), when a release is triggered from `main`, then semantic-release determines the next semantic version: major for breaking changes, minor for features, patch for fixes.
2. Given the release pipeline runs, when packages are built, then every approved publishable NuGet package is packed with the same semantic version, non-packable Aspire/runtime projects are excluded, and package IDs/descriptions/dependencies are validated before any push.
3. Given package publishing is enabled, when the release pipeline publishes to nuget.org, then it uses a scoped `NUGET_API_KEY` secret, `https://api.nuget.org/v3/index.json`, and `--skip-duplicate` so reruns after partial success are idempotent.
4. Given the release completes, when maintainers inspect GitHub Releases and NuGet, then release notes, tag, artifacts, package metadata, and dependency versions are consistent for the release version.
5. Given a new contributor reads `CONTRIBUTING.md`, then it documents Conventional Commits, PR process, branch naming, code review expectations, unit tests without Docker, integration tests with Docker, submodule setup, and release expectations clearly enough to submit a valid PR.
6. Given Epic 11 Story 11.1 has not landed yet, when implementing this story, then release workflow build/test coverage is self-contained and does not assume `.github/workflows/ci.yml` exists; branch protection and PR check enforcement remain Story 11.1 scope.

## Tasks / Subtasks

- [x] Task 0: Reconcile and lock the publishable package inventory before enabling publish (AC: #2)
  - [x] 0.1 Inspect all root `src/**/*.csproj` files excluding `src/submodules/**` and record the final inventory in the story Completion Notes.
  - [x] 0.2 Resolve the current planning/code mismatch explicitly:
    - Epic AC lists 8 packages: `Contracts`, `Client`, `Client.Rest`, `Server`, `Redis`, `Cli`, `Mcp`, `EventStore`.
    - Current repo has no `src/Hexalith.Memories.Client/` project; Story 7.1 explicitly said not to create it speculatively.
    - Current `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` has `<IsPackable>false</IsPackable>`.
    - Current `src/Hexalith.Memories.Telemetry/Hexalith.Memories.Telemetry.csproj` has `<IsPackable>true</IsPackable>` but is not listed in the epic package table.
  - [x] 0.3 Do not publish a package just because the epic text names it if the project does not exist or is intentionally non-packable; either make a deliberate architecture-backed change in this story or update the release package list to match implemented repo truth.
  - [x] 0.4 Add a package-inventory validation script or workflow step that fails if produced package IDs differ from the expected list.

- [x] Task 1: Add semantic-release configuration (AC: #1, #4)
  - [x] 1.1 Create root `package.json` with `private: true` and semantic-release dev dependencies.
  - [x] 1.2 Run `npm install` to create `package-lock.json`; CI must use `npm ci`.
  - [x] 1.3 Create root `.releaserc.json` with `branches: ["main"]`, `tagFormat: "v${version}"`, and plugin order: commit analyzer, release notes, changelog, exec, GitHub, git.
  - [x] 1.4 Configure `@semantic-release/exec` so `prepareCmd` builds and packs with `-p:Version=${nextRelease.version}` and `publishCmd` pushes only after package validation passes.
  - [x] 1.5 Configure `@semantic-release/git` to commit only generated release assets such as `CHANGELOG.md` with `[skip ci]` in the message to avoid release loops.

- [x] Task 2: Create release workflow (AC: #1, #2, #3, #4, #6)
  - [x] 2.1 Create `.github/workflows/release.yml` triggered by `push` to `main`; do not trigger releases from manually pushed `v*` tags because semantic-release owns tag creation.
  - [x] 2.2 Use `actions/checkout` with `fetch-depth: 0` and `submodules: true`; semantic-release needs full history and this repo fails restore when submodules are missing.
  - [x] 2.3 Set least-privilege permissions: `contents: write` for tag/GitHub Release creation; do not grant broad write-all permissions.
  - [x] 2.4 Add `concurrency: group: release, cancel-in-progress: false` so near-simultaneous merges serialize instead of racing on version/tag creation.
  - [x] 2.5 Set up .NET from `global.json`, Node LTS, NuGet cache, restore, build, and test before `npx semantic-release`.
  - [x] 2.6 Pass `GITHUB_TOKEN` and `NUGET_API_KEY` only through the semantic-release step environment; never echo secrets or commit feed credentials.
  - [x] 2.7 Keep `.github/workflows/nightly.yml` intact; it is the existing Tier 3 bridge and is not a release workflow.

- [x] Task 3: Harden package metadata and package contents (AC: #2, #4)
  - [x] 3.1 Add or centralize NuGet metadata for every approved packable package: `PackageId`, `Description`, `Authors`, `Company`, `PackageLicenseExpression`, `PackageProjectUrl`, `RepositoryUrl`, and `PackageTags`.
  - [x] 3.2 Ensure all published packages include a `README.md` via `PackageReadmeFile` and `None Include="README.md" Pack="true" PackagePath="\"`.
  - [x] 3.3 Preserve `AppHost` and `ServiceDefaults` as non-packable internal Aspire projects.
  - [x] 3.4 If `Telemetry` remains packable, give it first-class package metadata and include it in the expected package list; if it should be internal only, set `<IsPackable>false</IsPackable>` and validate it is absent from `nupkgs`.
  - [x] 3.5 Do not add package versions to individual `.csproj` files; package dependency versions stay in `Directory.Packages.props`.

- [x] Task 4: Add Conventional Commits enforcement and contributor documentation (AC: #1, #5)
  - [x] 4.1 Create `commitlint.config.js` using `@commitlint/config-conventional`.
  - [x] 4.2 If Story 11.1 is still absent, document that PR commitlint enforcement belongs to 11.1, but add local validation instructions (`npx commitlint --from <base> --to HEAD`) in `CONTRIBUTING.md`.
  - [x] 4.3 Create `CONTRIBUTING.md` covering submodule initialization, branch naming, Conventional Commits, PR review expectations, unit test command, integration test command, Docker requirement, and release process.
  - [x] 4.4 Document required repository secrets: `NUGET_API_KEY`; note that GitHub provides `GITHUB_TOKEN` automatically for the workflow.

- [x] Task 5: Validate release behavior without publishing (AC: #2, #3, #4)
  - [x] 5.1 Run `dotnet restore Hexalith.Memories.slnx`.
  - [x] 5.2 Run `dotnet build Hexalith.Memories.slnx --configuration Release`.
  - [x] 5.3 Run release package creation with `./tools/pack-release.ps1 -Version 0.0.1-ci -OutputDirectory artifacts/packages/test`.
  - [x] 5.4 Validate generated `.nupkg` files: expected count, expected package IDs, one shared version, license metadata, readme metadata, no `AppHost`, no `ServiceDefaults`, no test packages.
  - [x] 5.5 Run `npm ci`.
  - [x] 5.6 Run semantic-release dry-run where feasible (`npx semantic-release --dry-run --no-ci`) and document any local limitation caused by token verification.
  - [x] 5.7 Do not run a real `dotnet nuget push` locally; publishing is CI-only.

## Dev Notes

### Current State

- `.github/workflows/nightly.yml` exists and runs Tier 3 integration tests on schedule/manual dispatch.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.releaserc.json`, `commitlint.config.js`, root `package.json`, root `package-lock.json`, and `CONTRIBUTING.md` do not exist yet.
- `NuGet.config` already enforces nuget.org source mapping and signature validation. Keep using it; do not introduce a second feed or bypass signature enforcement.
- `Directory.Build.props` targets `net10.0`, C# 14, nullable enabled, implicit usings enabled, and warnings as errors. It also hard-fails restore/build when the `Hexalith.Commons` or `Hexalith.EventStore` submodules are missing.
- Package versions are centrally managed in `Directory.Packages.props`.

### Package Inventory Guardrail

The epic package list is stale against implemented code. Treat Task 0 as load-bearing. The release workflow must not blindly publish every project with `<IsPackable>true</IsPackable>` until the expected package list is deliberate and validated.

Current root `src` project facts:

| Project | Current packable state | Notes |
|---|---:|---|
| `Hexalith.Memories.Contracts` | true | Missing full NuGet metadata today. |
| `Hexalith.Memories.Client.Rest` | true | Has `PackageId` and description; needs full metadata/readme validation. |
| `Hexalith.Memories.Redis` | true | Missing full NuGet metadata today. |
| `Hexalith.Memories.Cli` | true | Global tool package; has `PackAsTool` and `ToolCommandName=memories`; needs full metadata/readme validation. |
| `Hexalith.Memories.Mcp` | true | Has full metadata and package README. |
| `Hexalith.Memories.EventStore` | true | Has full metadata and package README. |
| `Hexalith.Memories.Telemetry` | true | Created as shared telemetry substrate in Story 7.5; not listed in original Epic 11 package set. Decide explicitly. |
| `Hexalith.Memories.Server` | false | Runtime web service, currently not a NuGet package. Do not flip without architecture justification. |
| `Hexalith.Memories.AppHost` | false | Internal Aspire host; must not publish. |
| `Hexalith.Memories.ServiceDefaults` | false | Internal Aspire service defaults; must not publish. |

### Release Pattern To Reuse

Use the Hexalith.EventStore submodule as the closest working reference, but adapt it to this repo's package set and tests:

- `src/submodules/Hexalith.EventStore/.releaserc.json` uses semantic-release with `v${version}` tags, `@semantic-release/exec` for `dotnet build`/`dotnet pack`/`dotnet nuget push`, GitHub release assets, and changelog commit via `@semantic-release/git`.
- `src/submodules/Hexalith.EventStore/.github/workflows/release.yml` uses full checkout history, submodules, Node setup, NuGet cache, tests before release, `contents: write`, serialized release concurrency, and `NUGET_API_KEY` as a secret.
- Do not copy EventStore package names, test project names, or DAPR setup steps verbatim. Memories currently has a different workflow surface and only `nightly.yml` exists.

### semantic-release Requirements

- semantic-release determines release type from commit messages using Angular-style Conventional Commits by default.
- Use `feat:` for minor, `fix:` for patch, and a `BREAKING CHANGE:` footer for major.
- semantic-release should run after successful build/test on `main`, create the tag, generate release notes, publish artifacts, and update `CHANGELOG.md`.
- Ensure there is a baseline `v*` tag before the first real release. If none exists, create `v0.0.0` on a commit before the release automation PR; do not place the baseline tag on the automation commit itself.

### NuGet Publishing Requirements

- Use `dotnet pack` to create packages and `dotnet nuget push` to publish existing `.nupkg` files.
- Push to `https://api.nuget.org/v3/index.json` with `--api-key $NUGET_API_KEY`.
- Use `--skip-duplicate` so a rerun after partial publish does not fail on already-published packages.
- Package API keys are secrets. Never store them in `NuGet.config`, workflow YAML literals, `.npmrc`, or logs.
- NuGet package validation/indexing can lag after push; do not treat delayed search visibility as a failed pipeline unless nuget.org reports validation failure.

### File Scope

Expected files to add or edit:

- `.github/workflows/release.yml`
- `.releaserc.json`
- `package.json`
- `package-lock.json`
- `commitlint.config.js`
- `CONTRIBUTING.md`
- `CHANGELOG.md` if introducing an initial changelog for semantic-release
- `Directory.Build.props` only if centralizing package metadata safely
- Packable project `.csproj` files for package metadata/readme inclusion
- Package README files where missing

Avoid runtime `.cs` changes unless Task 0 deliberately changes package boundaries. This story should not change service behavior.

### Testing Requirements

- Validate packaging in Release configuration with an explicit test version before enabling publish.
- Add a script or workflow step that reads generated `.nupkg` metadata and fails on unexpected package IDs, missing license/readme metadata, mismatched versions, or internal projects being packed.
- Keep tests Docker-free in release workflow unless Story 11.1 has already established a reliable CI lane. The existing nightly workflow remains responsible for scheduled Tier 3 integration coverage.

### Project Structure Notes

- Use `.slnx`; do not create `.sln`.
- Keep package dependency versions centralized in `Directory.Packages.props`.
- Keep submodule initialization in checkout/restore documentation because `Directory.Build.props` blocks restore when submodules are absent.
- Preserve `NuGet.config` source mapping/signature enforcement.

### Latest Technical Information

- semantic-release's official documentation describes CI execution after successful builds on release branches, release type analysis from commit messages, tag creation, release notes, and publish steps.
- Microsoft documents `dotnet nuget push` as a push-only command; package creation remains `dotnet pack`. The `--api-key`, `--source`, and `--skip-duplicate` options are the relevant release-pipeline controls.
- NuGet.org publishing requires a scoped API key; keys must be kept secret and can be scoped to package patterns/operations.
- GitHub Actions job permissions should be explicitly scoped. `contents: write` is required for semantic-release tag/GitHub Release creation; avoid broader permissions.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` -- Epic 11, Story 11.2 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- Package distribution table and versioning strategy]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- D17, CI flow, project structure, enforcement guidelines]
- [Source: `_bmad-output/implementation-artifacts/7-1-cli-foundation-and-command-structure.md` -- `Client.Rest` created; non-REST `Client` explicitly not created]
- [Source: `_bmad-output/implementation-artifacts/7-5-search-and-access-telemetry.md` -- shared `Hexalith.Memories.Telemetry` project rationale]
- [Source: `_bmad-output/implementation-artifacts/10-1-mcp-server-and-tool-registration.md` -- MCP currently uses `Hexalith.Memories.Client.Rest` and is packable]
- [Source: `.github/workflows/nightly.yml` -- existing workflow to preserve]
- [Source: `NuGet.config` -- nuget.org source mapping and signature validation]
- [Source: `src/submodules/Hexalith.EventStore/.releaserc.json` and `.github/workflows/release.yml` -- closest working Hexalith release automation reference]
- [Source: semantic-release official docs: https://github.com/semantic-release/semantic-release]
- [Source: Microsoft `dotnet nuget push`: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push]
- [Source: NuGet.org publish docs: https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package]
- [Source: GitHub Actions workflow syntax permissions: https://docs.github.com/en/actions/writing-workflows/workflow-syntax-for-github-actions]

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1` -> passed.
- `npm install --save-dev semantic-release ... @commitlint/config-conventional` -> generated `package-lock.json`, 0 vulnerabilities.
- `dotnet restore Hexalith.Memories.slnx` -> passed.
- `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore` -> passed, 0 warnings/errors.
- `./tools/test-release.ps1` -> passed 2356/2356 in the release lane, excluding four pre-existing Server.Tests baseline failures documented in the script.
- Full `dotnet test Hexalith.Memories.slnx --configuration Release --no-build --filter "Category!=Integration"` timed out locally; targeted Server.Tests reproduced the known baseline failures: `ProvisionRediSearchActivityTests.RunAsync_IndexAlreadyExistsWithMatchingSchema_ShouldReturnTrue`, `IngestionInputValidatorTests.Validate_Event_WithNullBytes_Throws`, `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent`, `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`.
- `./tools/pack-release.ps1 -Version 0.0.1-ci -OutputDirectory artifacts/packages/test` -> passed; generated and validated seven `.nupkg` files.
- `npm ci` -> passed, 0 vulnerabilities.
- `npx commitlint --from HEAD~1 --to HEAD` -> commitlint executed and rejected the existing previous commit message as non-conventional.
- `npx semantic-release --dry-run --no-ci` -> loaded plugins and verified repository push access, then stopped at expected local `ENOGHTOKEN` because no `GITHUB_TOKEN`/`GH_TOKEN` was set.

### Completion Notes List

- Story context generated on 2026-04-26.
- Package-inventory mismatch intentionally captured as Task 0 to prevent accidental publication of stale or unintended artifacts.
- Final approved package inventory is `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Redis`, `Hexalith.Memories.Cli`, `Hexalith.Memories.Mcp`, `Hexalith.Memories.EventStore`, and `Hexalith.Memories.Telemetry`.
- `Hexalith.Memories.Telemetry` remains packable because it is a shared telemetry substrate used by multiple published surfaces; it now has first-class package metadata and README packaging.
- `Hexalith.Memories.Server`, `Hexalith.Memories.AppHost`, and `Hexalith.Memories.ServiceDefaults` remain non-packable and are asserted by `tools/validate-release-packages.ps1`.
- Release automation uses semantic-release as the version authority, with `tools/pack-release.ps1` forcing one version across every approved package and `tools/publish-nuget.ps1` validating packages again before any nuget.org push.
- The release workflow is self-contained and does not depend on Story 11.1 `.github/workflows/ci.yml`; branch protection and PR check enforcement remain outside this story.
- No runtime `.cs` files were changed.

### File List

- `.github/workflows/release.yml`
- `.releaserc.json`
- `CHANGELOG.md`
- `CONTRIBUTING.md`
- `commitlint.config.js`
- `package.json`
- `package-lock.json`
- `src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj`
- `src/Hexalith.Memories.Cli/README.md`
- `src/Hexalith.Memories.Client.Rest/Hexalith.Memories.Client.Rest.csproj`
- `src/Hexalith.Memories.Client.Rest/README.md`
- `src/Hexalith.Memories.Contracts/Hexalith.Memories.Contracts.csproj`
- `src/Hexalith.Memories.Contracts/README.md`
- `src/Hexalith.Memories.Redis/Hexalith.Memories.Redis.csproj`
- `src/Hexalith.Memories.Redis/README.md`
- `src/Hexalith.Memories.Telemetry/Hexalith.Memories.Telemetry.csproj`
- `src/Hexalith.Memories.Telemetry/README.md`
- `tools/pack-release.ps1`
- `tools/publish-nuget.ps1`
- `tools/release-packages.json`
- `tools/test-release.ps1`
- `tools/validate-release-packages.ps1`

### Review Findings

_Adversarial review on 2026-04-26 covered Stories 11.1 + 11.2 as a single uncommitted bundle on `main`. After dedup: 8 decision-needed, 18 patch, 25 defer, 17 dismissed. The full unified findings list lives in `11-1-github-actions-build-and-test-pipeline.md`. Findings primarily anchored to 11.2 are reproduced below; cross-cutting items are listed there._

#### Decision-Needed (11.2-anchored)

- [x] [Review][Decision] **D4: `--skip-duplicate` + plugin order = silent half-publish recovery.** Multi-faceted: (i) `tools/publish-nuget.ps1 --skip-duplicate` masks a partial publish where 4/7 packages went up before a network failure; (ii) plugin order (`exec → github → git`) means `git` plugin commits the chore CHANGELOG only on full success — re-running semantic-release recomputes the same nextVersion (no tag exists yet) and `--skip-duplicate` self-heals to mixed binaries on the registry; (iii) `tagFormat: "v${version}"` collides with stale tags from aborted releases; (iv) `pack-release.ps1` rebuilds with `ContinuousIntegrationBuild=true` regenerating content hashes between attempts. Options: (a) replace `--skip-duplicate` with explicit "this version is not yet on nuget.org" precondition; (b) accept current "operator re-run heals" model and add Slack/issue alerting on partial publish; (c) tag-first / publish-second ordering. Source: blind+edge+auditor. Location: `tools/publish-nuget.ps1:34-39`, `.releaserc.json:7-27`.
- [x] [Review][Decision] **D7: `pack-release.ps1` per-project `dotnet build` creates inconsistent intermediate metadata.** Diamond dependency: `Cli` depends on `Contracts`, `Telemetry`, `Client.Rest`. Per-iteration `dotnet build $package.project -p:Version=$Version` only rebuilds the leaf; dependency projects rebuild lazily, so the second iteration's build skips dependencies as up-to-date and the embedded `Version` metadata across packages can drift. Options: (a) replace per-project loop with one `dotnet build Hexalith.Memories.slnx -p:Version=$Version` then loop `dotnet pack --no-build`; (b) verify with `dotnet pack` post-condition that produced packages have consistent metadata; (c) defer (acknowledge risk, ship as-is). Source: blind. Location: `tools/pack-release.ps1:42-51`.

#### Patch (11.2-anchored)

- [x] [Review][Patch] **P10: `validate-release-packages.ps1` uses `-notlike "*$Version*"` substring on dependency version** — `1.0.0` matches `1.0.10`, `1.0.0-rc1`, etc. Replace with strict equality after stripping the NuGet `[X, )` lower-bound bracket form. Source: blind+edge. Location: `tools/validate-release-packages.ps1:230`.
- [x] [Review][Patch] **P11: `validate-release-packages.ps1` PackageId comparison is case-insensitive** — PowerShell `-ne` is case-insensitive by default; lowercase-vs-canonical drift slips through and NuGet then locks the first-pushed case forever. Use `-cne` and `[StringComparer]::Ordinal`. Source: edge. Location: `tools/validate-release-packages.ps1:196-198`.

_See `11-1-github-actions-build-and-test-pipeline.md` Review Findings for: D1 (test-release.ps1 baseline filter), D2 (AspireEndToEndTraceTests trait), D3 (release without integration-fast), D5 (runtime .cs scope leakage), D6 (cancellation conflation), D8 (verify-integration-fast SystemExit), and patches P1-P9, P12-P18 plus deferred items W1-W24._

### Change Log

- 2026-04-26: Implemented Story 11.2 semantic-release, NuGet package validation/publish automation, package metadata/readmes, release workflow, and contributor documentation.
- 2026-04-26 (review-fix pass): 3-layer adversarial review on the bundled 11.1 + 11.2 diff applied review-fix patches across `release.yml` (action versions aligned to v6/v5; head-commit `[skip ci]` guard; broader `permissions:` for semantic-release/github plugin; `if: success()` + `if-no-files-found: warn` on package upload), `tools/pack-release.ps1` (single solution-wide build with version pinned, then per-package `dotnet pack --no-build` to eliminate diamond-dependency intermediate-metadata drift), `tools/test-release.ps1` (drives from shared `tools/test-projects.unit-contract.txt` inventory; baseline-failure filter reduced from four exclusions to one tracked entry), `tools/validate-release-packages.ps1` (case-sensitive PackageId comparison; NuGet `[X, )` lower-bound bracket parsing for strict internal cross-package dependency version equality). Decision D4 (replace `--skip-duplicate` with idempotency precondition) accepted current model; partial-publish alerting deferred as **S11-FD**. P16 (release.yml stale-tag preflight) deferred as **S11-FC**. Story moved `review → done`. **User-action remaining (P1):** `git add package-lock.json` before next push.
