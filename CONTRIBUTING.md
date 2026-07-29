# Contributing

## Setup

Clone with root-declared submodules under `references/` or initialize them before restoring:

```powershell
git submodule update --init
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

## Story File Scope

Every implementation story that is used for development should include a `## File Scope` section.
Use this shape for new stories:

```markdown
## File Scope

Allowed files for this story:

- `path/or/glob` - short reason

Read/verify only:

- `path/to/context.md`

Forbidden by default:

- `src/**/*.cs`
```

The writable contract is the direct bullet list under `Allowed files for this story:`. The validator
extracts only backtick-wrapped paths or globs from that list. `Read/verify only:` and
`Forbidden by default:` are reviewer guidance and diagnostic context; they do not grant write access.
Changed-file inputs are normalized to repository-relative POSIX-style paths. Added, modified, copied,
renamed, and deleted paths are validated; for renames, Git's `--name-only` output supplies the
destination path.

The story scope check discovers the artifact owner key in the same order locally and in CI:

1. explicit CLI argument, such as `--story-key 12-3-story-file-scope-enforcement`
2. a commit trailer named `Story:` or `Story-Key:`
3. a branch name containing a full story key, such as `feature/12-3-story-file-scope-enforcement`

An owner key can be a numeric sprint story key or the exact filename stem of a standalone
`spec-*` artifact. A standalone spec is eligible only when its artifact exists under
`_bmad-output/implementation-artifacts/` and carries the same parseable `## File Scope` contract as
a story. Keep the full spec key aligned across sources, for example:

```text
branch: fix/spec-resolve-story-gate-commit-path
Story-Key: spec-resolve-story-gate-commit-path
```

An extended key does not fall back to a shorter artifact: `spec-example-extra` selects only
`spec-example-extra.md`, never `spec-example.md`. A changed set on `main` with no numeric story or
standalone-spec owner still fails closed.

If two non-empty sources disagree, the check fails closed and reports each `source=key` pair so you
know which input to fix. Keep the branch name and commit trailer aligned when both are present.

The validator also fails closed if any single source contains more than one story key — for
example, a `--story-key "<key-a> and <key-b>"` value, a single `Story:` or `Story-Key:` trailer
that lists two keys, or a branch name with two distinct keys separated by a non-`-`/`_` character
such as `/`. The diagnostic enumerates every detected key so the contributor does not have to
guess which one was preferred.

If `git interpret-trailers` is unavailable (Git missing or not on `PATH`), the validator surfaces
a clean validation error that names the missing tool and points at the install/`PATH` fix; it does
not emit a Python `FileNotFoundError` traceback.

Install the repo-managed hooks per clone:

```powershell
git config core.hooksPath .githooks
```

Do not use `--global`; this repository's hooks are intentionally project-local. The `pre-commit` hook
checks the staged file list using branch or caller-provided story context. The `commit-msg` hook reads
the proposed commit message and validates `Story:`, `Story-Key:`, and `Scope-Override:` trailers
against the same staged file list. CI runs the same Python validator against PR and non-`main` push
diffs, using the PR source branch and the real head commit message rather than the synthetic merge
commit.

Local hook and direct CLI runs treat an empty changed-file list as a successful no-op (e.g., a
`pre-commit` invocation with nothing staged). CI does not — the `story-file-scope` workflow job
fails loudly when its computed changed-file list is empty, when `origin/main` resolves to the same
commit as `HEAD`, or when the comparison base cannot be fetched. Fetch failures name the failed
operation in the diagnostic instead of degrading to a "every file in HEAD" fallback.

Use `Scope-Override:` only for narrow, audited exceptions:

```text
Scope-Override: docs/dev/story-scope.md - companion operator note discovered during implementation
```

Valid overrides name an exact repository-relative path or a narrow glob plus a short rationale.
Invalid examples include `*`, `.`, `src`, `src/**`, and prose-only values such as `repo-wide cleanup`.
An exact override authorizes only that path; it does not authorize siblings, children, suffixes, or
partial-string matches. Overrides also do not bypass forbidden-default areas such as submodule
contents, release scripts, `package-lock.json`, or runtime/source paths without a separate human,
product, or architecture decision.

Forbidden-default paths can still appear in a story's `Allowed files for this story:` block when the
story author has made a deliberate decision to touch them. Listing the path in the allow-list IS the
"separate human/product/architecture decision" that authorizes the change; reviewers should treat any
allow-list entry that intersects the forbidden-default list as a load-bearing scope decision and call
it out explicitly in story review.

The forbidden-default list (one source of truth, in `tools/check-story-file-scope.py`) currently
covers: `src/**/*.cs`, `tests/**/*.cs`, `tools/publish-nuget.ps1`, `tools/pack-release.ps1`,
`tools/test-release.ps1`, `package-lock.json`, and the submodule trees under `references/`
(`references/Hexalith.AI.Tools/**`, `references/Hexalith.Builds/**`,
`references/Hexalith.Commons/**`, `references/Hexalith.EventStore/**`,
`references/Hexalith.FrontComposer/**`, `references/Hexalith.PolymorphicSerializations/**`,
and `references/Hexalith.Tenants/**`). Submodule pointer changes (a tree entry like the bare
`references/Hexalith.EventStore` path with no children) are also detected and treated as
forbidden-default — the matcher uses recursive `**` semantics so a `<submodule>/**` glob matches
both the bare submodule path and any path beneath it.

To run the check directly:

```powershell
python tools/check-story-file-scope.py --story-key 12-3-story-file-scope-enforcement --changed-file CONTRIBUTING.md
python tools/check-story-file-scope.py --branch-name feature/12-3-story-file-scope-enforcement --changed-files-file changed-files.txt --commit-message-file commit-message.txt
```

This guardrail has narrow non-goals: it does not change runtime behavior, release tooling, package
metadata, or submodule contents. Do not initialize nested submodules recursively for story-scope work.

## Cross-Tenant Negative Evidence

The repository rule *"Tenant isolation requires attached negative evidence"*
(`_bmad-output/project-context.md`, `### Testing Rules`) is enforced by
`tools/check-tenant-isolation-evidence.py`. When a changed file matches a
tenant-isolation surface glob listed in `tools/tenant-isolation-surfaces.txt`
(tenant/case routing, auth claims and authorization filters, tenant status,
index/key/graph selection, actor IDs, MCP authorization/execution, attribution),
the resolving story/spec must carry a `## Cross-Tenant Negative Evidence`
section. A change that touches no surface is a silent no-op.

The section must contain exactly one of these dispositions (`**Label:**` lines,
optionally bulleted):

```markdown
## Cross-Tenant Negative Evidence

**Surfaces:** <named affected surfaces>
**Tests:** `Xxx.CrossTenantDenialTests` (one or more backticked test names)
**Command:** `dotnet test ... --filter "..."`
**Result:** N passed; cross-tenant denial precedes dependencies.
```

If the proof cannot run, record an accepted blocker instead — `**Accepted
blocker:**`, `**Owner:**`, `**Consequence:**`, `**Reopen trigger:**` (all
required). If a matched file provably changes no live isolation path, state
`**Not triggered:** <reason>`. Cite Story 20.2 (denial-before-dependency) and
Story 24.3 (verifier/tenant-marker) when applicable, or the newer canonical
replacement.

Story resolution and precedence match the File Scope check (`--story-key` >
`Story:`/`Story-Key:` trailer > branch name). The gate runs in the `commit-msg`
hook (not `pre-commit`, which cannot see the trailers) and as a step of the
`story-file-scope` CI job. As a last resort, a change with no story/spec may
carry a `Tenant-Isolation-Evidence: not-applicable - <reason>` commit trailer;
the non-empty reason is logged.

To run the check directly:

```powershell
python tools/check-tenant-isolation-evidence.py --story-key <your-story-key> --changed-file src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
python tools/check-tenant-isolation-evidence.py --branch-name feature/<your-story-key> --changed-files-file changed-files.txt --commit-message-file commit-message.txt
```

## Sprint Status History Conventions

`_bmad-output/implementation-artifacts/sprint-status.yaml` records story state
transitions on a single YAML line per story. To keep that file readable and to
keep tooling free of multi-thousand-character one-liners, status updates must
follow these rules.

- Use a short dated breadcrumb when a status changes. The breadcrumb names what
  changed and points at the durable artifact that holds the detail. Example:
  `done # 2026-05-04 review close-out; see 14-3 story artifact and deferred-work.md 13.x-RV* close-outs.`
- Put detailed evidence in the story artifact under
  `_bmad-output/implementation-artifacts/<story-key>.md` (Dev Agent Record,
  Change Log, Completion Notes), in the deferred-work entry, in the run log, or
  in a dedicated review or retrospective document. Then link or name that
  artifact from `sprint-status.yaml`.
- Do not append multi-sentence narratives, full validation transcripts, or
  multi-line Markdown to a single YAML status line. If a recap is worth keeping,
  put it in the story artifact and reference it.
- Historical Epic 1-13 status lines are intentionally not rewritten. The rules
  above apply forward, not retroactively. A targeted edit is only justified when
  a parser, dashboard, or auditor proves it is required.

## Epic 14 Story Close-out Rules

Epic 14 stories exist to take ownership of deferred review findings. The
following bookkeeping rules apply to every Epic 14 story before it is moved to
review.

- List every targeted deferred ID in the story's Dev Agent Record completion
  notes and mark each one `resolved`, `accepted`, or `carried-forward` using the
  schema documented at the top of
  `_bmad-output/implementation-artifacts/deferred-work.md`.
- Discussing an item is not closure. Closure requires code, test, documentation,
  or explicit acceptance evidence captured in the entry's `Evidence:` or
  `Rationale:` field.
- An entry intentionally left open after the story completes must carry a fresh
  `Re-open trigger:` value that names the future condition under which it should
  be revisited. Do not silently delete or hide such entries.
- Root-declared submodule pointer changes under `references/` are forbidden by
  default. An Epic 14 story must not initialize, update, or bump nested
  submodule pointers as part of its scope.

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

Published contract-document guards must assert authoritative structure, not whole-document vocabulary.
For Markdown tables, bind guarantees to normalized complete rows or exact adjacent cells and add row-count
or uniqueness checks where omission, duplication, or phantom rows could otherwise pass. Bind narrative-only
claims to their exact owning ATX section; a bare whole-document `ShouldContain` requires an explicit reason.

Every published contract-document guard must also reject raw leaked tool-call markup through the shared
`ContractDocumentGuard` helper. The shared scanner covers opening, closing, attributed, incomplete, and
mixed-case `content`, `invoke`, `parameter`, and `tool_call` tags while allowing ordinary Markdown, HTML,
code spans, and fenced examples.

Run the fast local confidence path before opening a PR. This path does not require Docker or DAPR
sidecars:

```powershell
dotnet restore Hexalith.Memories.slnx
dotnet build Hexalith.Memories.slnx --configuration Release --no-restore
./tools/test.ps1 -Filter "Category!=Integration" -Configuration Release -NoBuild -Coverage -ResultsDirectory "TestResults/test-unit-contract"
python tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json
```

The checked-in coverage contract enforces at least 78.0% unioned line coverage across the required
first-party production assemblies and requires executable evidence for the Server, CLI, and MCP
composition roots plus a nonempty Cobertura report from every inventoried test project. The wrappers
clean the requested repository-local results directory before collection so old attachments cannot
mask a regression.

The Docker-free lane maps `Category!=Integration` to
`Category!=Integration&Category!=Benchmark` and runs the shared inventory in
`tools/test-projects.unit-contract.txt`:

```text
Contracts.Tests
Server.Tests
Cli.Tests
Mcp.Tests
EventStore.Tests
Web.Tests
```

`Hexalith.Memories.TestHelpers` builds through the solution but is not a direct test target.
`Hexalith.Memories.Benchmarks` is intentionally excluded from PR CI. Exact `Category=Benchmark`
selects its one-project inventory but clears the effective trait filter so all 17 tests run, including
the four untraited seeder tests. The wrappers require the resulting TRX to report exactly 17 executed
tests and zero skipped tests, even when the test process returns the known thesis failure. The lane requires Docker and preserves the blocking 80% hybrid-win
and identical-NDCG@10 reproducibility assertions.

### Sandbox test runner workaround

Use the standard `tools/test.*` commands above for normal local development and CI parity. In locked-down
sandbox or WSL environments, `dotnet test` can abort before running tests because VSTest opens a local
TCP listener and receives `SocketException (13): Permission denied`. When that happens, build the
target test project and run the xUnit v3 assembly directly with `DiffEngine_Disabled=true`:

```powershell
dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --configuration Debug
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Web.Tests/bin/Debug/net10.0/Hexalith.Memories.Web.Tests.dll -class Hexalith.Memories.Web.Tests.Components.Validation.Epic17ConformanceTests
```

`DiffEngine_Disabled=true` prevents snapshot/diff tooling from launching an external diff tool in the
sandbox. Prefer the narrowest useful xUnit v3 filter (`-class`, `-method`, or `-namespace`) and record
the command, test count, and the VSTest socket limitation in the story or review artifact. Do not use
this workaround as evidence for Docker, DAPR sidecar, Aspire, browser, or other infrastructure lanes;
when those lanes cannot run in the sandbox, record the limitation and run them later in an environment
that provides the required infrastructure.

Docker-required tests are isolated in the integration lane. The standard skip/diagnostic wording for
Docker-only paths is:

```text
Requires Docker - see CONTRIBUTING.md
```

Run the fast Docker-backed integration gate when Docker is available:

```powershell
./tools/test.ps1 -Filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" -Configuration Release -ResultsDirectory "TestResults/integration-fast"
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
| `story-file-scope` | Validate changed files against the originating story's `File Scope`, and require attached cross-tenant negative evidence when a tenant-isolation surface changed. |
| `build` | Restore and build `Hexalith.Memories.slnx` in Release. |
| `test-unit-contract` | Run Docker-free unit/contract coverage, enforce the scoped line gate, and pack/validate the approved NuGet topology before merge. |
| `integration-fast` | Run Docker-backed `Category=Integration&Category!=IntegrationSlow&Category!=Performance` tests and verify required surface evidence. |

All jobs checkout root-declared submodules under `references/` and use the SDK from `global.json`. The
`test-unit-contract` check uploads TRX plus per-project Cobertura reports as the 14-day
`test-unit-contract-results` artifact. The scheduled/manual benchmark job uploads its TRX and exact
quality JSON separately as `nightly-benchmark-trx` and `nightly-benchmark-quality-result`. Other test
lanes write TRX files under `TestResults/<lane>` and upload those folders as workflow artifacts. The test scripts fail if a
selected project executes zero tests **when `--results-directory` (or `-ResultsDirectory`) is passed**;
local invocations without the flag skip TRX emission and therefore skip the zero-test guard. Pass the
flag locally if you want the same protection CI uses.

The same required job runs the `release_packages` and `publish_nuget` fixtures, then exercises the
real inventory-driven package path without publishing:

```powershell
./tools/pack-release.ps1 -Version 0.0.264 -OutputDirectory artifacts/packages/ci -PackageOnly
```

Package-only mode builds and packs exactly the nine projects in `tools/release-packages.json`, runs
`validate-release-packages.ps1` against the generated `.nupkg` directory and version, and stops before
container/deployment preparation. It does not invoke semantic-release, contact NuGet, or require a
publishing credential.

Linux and macOS contributors should use `./tools/test.sh` (the script CI runs) for full parity with the
PR lanes; `./tools/test.ps1` is the Windows-first variant and is used in `CONTRIBUTING.md` examples for
brevity.

Required branch protection for `main` is documented in `docs/dev/branch-protection.md`. Maintainers
must select the exact checks `story-file-scope`, `build`, `test-unit-contract`, and
`integration-fast` after the workflow has run at least once and GitHub exposes the check names.

Package publication, semantic-release, and `NUGET_API_KEY` remain release automation scope. PR CI
packs and validates local artifacts only; it never publishes them.

## Code Review

### Forbidden Default Tolerances

When reviewing infrastructure, scripts, workflow YAML, release automation, or aggregate verification
changes, treat tolerant defaults as suspect by default. Block them unless the PR includes an explicit
idempotency proof, a clear recovery path, or an operator-visible warning, failure, or alert.
Otherwise, request fail-fast behavior.

Until Story 12.5 codifies the canonical mechanism, "operator-visible" means a non-zero CI exit, a
structured log line tagged for operator scan, or an auto-opened issue on failure. This rule is
reviewer judgment today, not automated enforcement.

Scan for these warning patterns. Patterns marked Bash apply to `bash` shell scripts; equivalent
PowerShell suppression idioms appear in the `catch` bullet:

- **Process-substitution or pipeline exit-code swallowing** (Bash), such as `mapfile -t X < <(cmd)`,
  where the consumer command can succeed after the producer command failed. Note: `set -o pipefail`
  and `set -e` do NOT propagate the substituted command's exit status — capture status explicitly,
  e.g. via a temp file or by reading `$?` after the consumer.
- **`actions/upload-artifact` with `if-no-files-found: ignore`**, which can make missing build,
  pack, or test output look successful. Prefer `error`; use `warn` only when absence is explicitly
  non-blocking and documented.
- **`dotnet nuget push --skip-duplicate`** used without an idempotency precondition and
  operator-visible signal. Acceptable for 409-conflict rerun recovery; not a substitute for
  partial-publish detection. Story 12.5 (currently backlog) owns the executable partial-publish
  alert — until it ships, this bullet is reviewer guidance only with no automated enforcement.
- **Per-row or per-iteration zero-count checks** that allow aggregate verifiers to pass when every
  selected row, project, or package verified nothing — and zero-input cases where the inventory
  selected zero rows, projects, or packages should fail loudly rather than report success on an
  empty set.
- **Silent error suppression**: empty `catch { }` blocks in C#; empty `catch { }`,
  `-ErrorAction SilentlyContinue` / `Ignore`, or `$ErrorActionPreference = 'SilentlyContinue'` in
  PowerShell, that swallow exceptions without a written recovery path or alternate signal.
- **`|| true` and equivalent Bash idioms** (`set +e` blocks, `cmd 2>/dev/null`,
  `cmd && true || true`) that discard non-zero exit codes without making the tolerated failure
  visible.
- **Default-empty or sentinel-fallback inventories** (Bash), such as `PROJECTS=()`,
  `PROJECTS=("")`, or `${PATTERN:-*}`, that turn "no inventory" into "match everything" or
  "verify nothing."

Reviewer question: does this code intentionally tolerate a missing artifact, duplicate package,
zero-count result, swallowed exception, missing inventory, or failed command? If yes, the PR must
show why that tolerance is safe and how an operator or reviewer will notice when the tolerated path
happens. Record the rationale in the PR description under a `Tolerance Justification:` section,
with a link to the operator alert mechanism or recovery path.

Canonical source: `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` (refreshed
Epic 11 carry-forward finding "Tolerant defaults repeatedly hid failure"). The original record is
`_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` Pattern 3. The auto-memory
name `feedback_tolerance_idioms.md` is referenced by planning artifacts but is not present as a
repository file at the time of this commit.

## Release Packages

The approved NuGet package inventory is checked in at `tools/release-packages.json`.

| Package | Publish approved | Notes |
| --- | --- | --- |
| `Hexalith.Memories.Aspire` | Yes | Aspire hosting extensions |
| `Hexalith.Memories.Contracts` | Yes | Shared API contracts |
| `Hexalith.Memories.Client.Rest` | Yes | Typed REST client |
| `Hexalith.Memories.Redis` | Yes | Compatibility-only Redis and FalkorDB surface |
| `Hexalith.Memories.Cli` | Yes | Global .NET tool |
| `Hexalith.Memories.Mcp` | Yes | MCP server package |
| `Hexalith.Memories.ServiceDefaults` | Yes | Shared health, resilience, discovery, and telemetry defaults |
| `Hexalith.Memories.EventStore` | Yes | EventStore integration package |
| `Hexalith.Memories.Telemetry` | Yes | Shared telemetry constants |
| `Hexalith.Memories.Server` | No | Runtime service |
| `Hexalith.Memories.AppHost` | No | Aspire host |
| `Hexalith.Memories.Web` | No | Web application host |

Do not add a project to the release package set by changing only `<IsPackable>true</IsPackable>`.
Update `tools/release-packages.json`, package metadata, README packaging, and validation evidence in
the same change.

The inventory currently contains nine approved packages. Every approved package must declare package
ID, description, authors, company, license, project URL,
repository URL, tags, and `PackageReadmeFile`. Package dependency versions stay in
`Directory.Packages.props`; do not add dependency versions to individual project files.

## Release Process

Releases are automated from pushes to `main` by `.github/workflows/release.yml`. The observed
release path and operator checklist are maintained in `docs/dev/release-runbook.md`.

The workflow restores, builds, runs Docker-free tests, validates the package inventory, and then runs
semantic-release. semantic-release creates the `v${version}` tag, creates the GitHub Release, packs
every approved package with the same version, and publishes packages to nuget.org. Release jobs do
not commit back to `main`; `main` remains protected and all repository changes still go through pull
requests.

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
./tools/pack-release.ps1 -Version 0.0.1-ci -OutputDirectory artifacts/packages/test -PackageOnly
npm ci
npx semantic-release --dry-run --no-ci
```

Do not run a real `dotnet nuget push` locally. Publishing is CI-only.
