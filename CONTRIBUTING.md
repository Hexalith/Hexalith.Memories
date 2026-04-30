# Contributing

## Setup

Clone with submodules or initialize them before restoring:

```powershell
git submodule update --init --recursive
dotnet restore Hexalith.Memories.slnx
```

The repository uses `Hexalith.Memories.slnx`. Do not create or rely on a stale `.sln` file.

## Branches and Pull Requests

Use short topic branches:

```text
feat/<short-name>
fix/<short-name>
docs/<short-name>
chore/<short-name>
```

Pull requests should describe the user-visible change, list affected packages when package behavior
or public API changes, and include the validation commands you ran. Reviews must pass before merge.

## Conventional Commits

Release versions are computed from commit messages on `main`.

| Commit type | Release effect |
| --- | --- |
| `fix: correct search timeout handling` | Patch release |
| `feat: add handler mismatch endpoint` | Minor release |
| `feat!: change search result shape` | Major release |
| `feat: change API` with `BREAKING CHANGE:` footer | Major release |
| `docs:`, `test:`, `chore:` | No package release by default |

For local validation:

```powershell
npx commitlint --from <base> --to HEAD
```

Story 11.1 owns PR check enforcement and branch protection. Until it lands, treat local commitlint
validation as required release hygiene.

## Tests

Run the fast local confidence path before opening a PR. This path does not require Docker or DAPR
sidecars:

```powershell
dotnet restore Hexalith.Memories.slnx
dotnet build Hexalith.Memories.slnx --configuration Release --no-restore
./tools/test.ps1 -Filter "Category!=Integration" -Configuration Release -NoBuild -ResultsDirectory "TestResults/test-unit-contract"
```

The Docker-free lane maps `Category!=Integration` to
`Category!=Integration&Category!=Benchmark` and runs the shared inventory in
`tools/test-projects.unit-contract.txt`:

```text
Contracts.Tests
Server.Tests
Cli.Tests
Mcp.Tests
EventStore.Tests
```

`Hexalith.Memories.TestHelpers` builds through the solution but is not a direct test target.
`Hexalith.Memories.Benchmarks` is intentionally excluded from PR CI; run benchmark tests explicitly
with `Category=Benchmark` when needed.

Docker-required tests are isolated in the integration lane. The standard skip/diagnostic wording for
Docker-only paths is:

```text
Requires Docker - see CONTRIBUTING.md
```

Run the fast Docker-backed integration gate when Docker is available:

```powershell
./tools/test.ps1 -Filter "Category=Integration&Category!=IntegrationSlow" -Configuration Release -ResultsDirectory "TestResults/integration-fast"
python tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast
```

The scheduled `.github/workflows/nightly.yml` workflow remains the Tier 3 slow integration bridge:

```powershell
./tools/test.ps1 -Filter "Category=IntegrationSlow" -Configuration Release -ResultsDirectory "TestResults/integration-slow"
```

`Category=IntegrationSlow` must stay out of PR CI unless the team explicitly accepts the added runtime.

## Pull Request CI

`.github/workflows/ci.yml` publishes these stable check names:

| Check | Purpose |
| --- | --- |
| `build` | Restore and build `Hexalith.Memories.slnx` in Release. |
| `test-unit-contract` | Run Docker-free unit/contract tests from `tools/test-projects.unit-contract.txt`. |
| `integration-fast` | Run Docker-backed `Category=Integration&Category!=IntegrationSlow` tests and verify required surface evidence. |

All jobs checkout submodules recursively and use the SDK from `global.json`. Test lanes write TRX files
under `TestResults/<lane>` and upload those folders as workflow artifacts. The test scripts fail if a
selected project executes zero tests **when `--results-directory` (or `-ResultsDirectory`) is passed**;
local invocations without the flag skip TRX emission and therefore skip the zero-test guard. Pass the
flag locally if you want the same protection CI uses.

Linux and macOS contributors should use `./tools/test.sh` (the script CI runs) for full parity with the
PR lanes; `./tools/test.ps1` is the Windows-first variant and is used in `CONTRIBUTING.md` examples for
brevity.

Required branch protection for `main` is documented in `docs/dev/branch-protection.md`. Maintainers
must select the exact checks `build`, `test-unit-contract`, and `integration-fast` after the workflow
has run at least once and GitHub exposes the check names.

Package publishing, semantic-release, and `NUGET_API_KEY` are Story 11.2 release automation scope, not
PR CI scope.

## Release Packages

The approved NuGet package inventory is checked in at `tools/release-packages.json`.

| Package | Publish approved | Notes |
| --- | --- | --- |
| `Hexalith.Memories.Contracts` | Yes | Shared API contracts |
| `Hexalith.Memories.Client.Rest` | Yes | Typed REST client |
| `Hexalith.Memories.Redis` | Yes | Redis, RediSearch, and FalkorDB adapters |
| `Hexalith.Memories.Cli` | Yes | Global .NET tool |
| `Hexalith.Memories.Mcp` | Yes | MCP server package |
| `Hexalith.Memories.EventStore` | Yes | EventStore integration package |
| `Hexalith.Memories.Telemetry` | Yes | Shared telemetry constants |
| `Hexalith.Memories.Server` | No | Runtime service |
| `Hexalith.Memories.AppHost` | No | Aspire host |
| `Hexalith.Memories.ServiceDefaults` | No | Aspire service defaults |

Do not add a project to the release package set by changing only `<IsPackable>true</IsPackable>`.
Update `tools/release-packages.json`, package metadata, README packaging, and validation evidence in
the same change.

Every approved package must declare package ID, description, authors, company, license, project URL,
repository URL, tags, and `PackageReadmeFile`. Package dependency versions stay in
`Directory.Packages.props`; do not add dependency versions to individual project files.

## Release Process

Releases are automated from pushes to `main` by `.github/workflows/release.yml`. The observed
release path and operator checklist are maintained in `docs/dev/release-runbook.md`.

The workflow restores, builds, runs Docker-free tests, validates the package inventory, and then runs
semantic-release. semantic-release creates the `v${version}` tag, updates `CHANGELOG.md`, creates the
GitHub Release, packs every approved package with the same version, and publishes packages to
nuget.org.

Required repository secret:

| Secret | Purpose |
| --- | --- |
| `NUGET_API_KEY` | Scoped nuget.org API key used only by the release job |

GitHub provides `GITHUB_TOKEN` automatically. Do not store NuGet API keys in `NuGet.config`, `.npmrc`,
workflow literals, logs, or local documentation.

Before changing release automation, run:

```powershell
./tools/validate-release-packages.ps1
./tools/test-release.ps1
./tools/pack-release.ps1 -Version 0.0.1-ci -OutputDirectory artifacts/packages/test
npm ci
npx semantic-release --dry-run --no-ci
```

Do not run a real `dotnet nuget push` locally. Publishing is CI-only.
