# Release Runbook

This runbook records the observed first release path (`v1.2.0`, 2026-04-30) and the repeatable
path for the next release. Package and container publishing remains CI-only. Do not publish release
artifacts from a workstation.

## Historical Release-Note Correction (2026-10-28)

Commit [358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4](https://github.com/Hexalith/Hexalith.Memories/commit/358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4)
used an untyped subject and described only test additions in its message. It also changed
`InMemoryAccessTelemetryStateStore.GetDueEntriesAsync`: entries are ordered by expiry minute,
then expiration timestamp (`ExpiresAtUtc`, ordinal), shard and record ID. The added timestamp
tie-break matches the Dapr adapter's purge ordering, making bounded batches deterministic across
the two adapters. This is a source change to the in-memory lifecycle test adapter; it does not
establish Production retention qualification or change the runtime adapter registration.

This durable note records the omitted change for release review (DW-600). Published commit and
release history remain intact. Local and CI commit validation now apply the Conventional Commit
rules to merge, revert and version-shaped messages as well as ordinary subjects.

## Shared CI Adoption And Rollback

Story 30.1 adopts the shared CI core before guarded release work. The approved registry order is
30.1 shared CI, 30.2 pinned Builds publication/recovery qualification, 30.3 guarded release,
30.4 five-image publication, 30.5 partial-release recovery and 30.6 cutover parity/rollback. Former
30.2 shared CI and former 30.1 guarded release identifiers are historical aliases. The guarded 30.3 caller is now implemented; production environment approval remains an external setting and must be confirmed by API readback before any dispatch.

The approved scope remains the ten packages in `tools/release-packages.json` and the five existing
images (Server, MCP, DomainService, access-telemetry lifecycle and clock). Guarded release uses `prepare-domain-release@81c88b34d09d34260b92d7f0928dbbc1c7446baa` with the same `builds-execution-sha`, an explicit count of ten packages, and caller-owned NuGet Trusted Publishing. Historical recovery remains compatible with tagged `packageId` manifests; complete-set recovery parity is owned by Story 30.5.

Before adopting fresh publication qualification evidence, require the dedicated
`Qualify Builds publication` workflow to succeed at the exact bound Memories source and selected
Builds revision, with the upstream fixtures and all six checkpoints passing from clean source.
Retain and verify that run's original archive, result bytes and provenance receipt before selecting
it. Future captures also retain the actual workflow SHA/ref in `workflow-context.json`; adopting one
requires an explicit new source/run/digest/member-inventory update. The existing a478 capture keeps
its original eleven-member inventory. This adoption requirement preserves the existing `build`
aggregate, required-check semantics and remote branch-protection settings.

| Previous owner | Current owner and check | Evidence / retained behavior | Rollback |
| --- | --- | --- | --- |
| Local Release portion of `build` | `shared-ci / build-and-test`, Builds `domain-ci.yml@6e7011fc754e74ac2a8864da1ce0b6d8f7178a48` | Release restore/build with warnings as errors; explicit Contracts MTP shard; `blocking-test-results`, seven days | Restore local Release steps from the baseline below |
| Local Debug/source portion of `build` | `source-debug` | Debug source graph; at least three catalog tests, none failed/errors/skipped/not-run; `source-catalog-verification` XML uploaded even after test failure | Restore the original combined build job |
| Required `build` status | Always-run local aggregate | Requires success from both shared and source jobs; unsuccessful/skipped/cancelled/unknown dependencies fail | Restore the original combined build job, preserving the `build` name |
| Unit/contract and package validation | Local `test-unit-contract` | Seven Coverlet project reports, 76.5% line coverage, package topology; `test-unit-contract-results`, 14 days | Retain unchanged |
| Tenant-negative/scope/slice/readiness, web E2E, fast integration, deployment and SQL | Existing local jobs | Existing mandatory commands and failure artifacts; no advisory conversion | Retain unchanged |
| Explicit PR-title commitlint, nightly slow integration and benchmarks | Existing workflows | Existing triggers, blocking tests and artifacts | Retain unchanged |

Shared consumer validation and coverage remain disabled; the compatible shared shard selects only
`Contracts.Tests`, with empty integration/Aspire selections and a 20-minute build timeout. The
shared workflow has no integration-exclusion input. The full local coverage lane therefore retains
Contracts intentionally until collector/report equivalence is proved. Shared Release restore relies
on `Directory.Build.props` audit policy; four local restores retain their existing explicit audit
exceptions. No new collector, package source, dependency pin or governed provenance is introduced.

To prepare a rollback, inspect the original workflow without modifying the working tree:

```bash
git show 00919bdd6fe97284da5f4f1657da4a5b336867cc:.github/workflows/ci.yml
```

Restore its original combined `build` steps through a normal PR, removing the shared caller,
source job and aggregate together. Preserve every specialized lane, the three required check names,
and the audit exceptions. Adjust adoption fixtures to the approved rollback and run actionlint,
`ci_alignment`, coverage, scope/readiness and CLI inventory checks before merge. The April branch
protection evidence below stays historical evidence; this story performs no remote settings write.

## Current Release Contract

- An operator dispatches `.github/workflows/release.yml` from the current `main` tip. Normal pushes run CI and never publish. The source job proves the selected SHA is the live main tip with a successful completed `ci.yml` push run for that exact SHA, and checks that the production environment has required reviewers and a branch deployment policy. Administrator approval bypass is permitted when enabled in the environment settings. The protected `production` job runs only after that proof succeeds; it reads back protection again before publication.
- The production job calls pinned `prepare-domain-release@81c88b34d09d34260b92d7f0928dbbc1c7446baa` with the identical `builds-execution-sha` and `expected-package-count: 10`. It restores and builds; only when publication is enabled does it then repeat the exact-source CI proof, because a frozen run exits its publish gate before that repeat. The caller validates the package manifest, checks local/remote tag and every versioned package/image destination, then reproves source immediately before OIDC login and `npx semantic-release`. Application tests are owned by exact-source CI and are not repeated in release. Semantic-release then drives the prepare, publish, and post-publish
  phases through its plugin chain —
  `pack-release.ps1` and
  `publish-release.ps1` are not invoked as separate workflow steps; they are invoked by
  `@semantic-release/exec` during the corresponding semantic-release lifecycle phase.
- Semantic-release uses `.releaserc.json`.
- Release tooling restore uses `npm ci` from the tracked root `package-lock.json`. `npm ci` is the
  release contract because it fails when `package.json` and the lockfile disagree and removes any
  existing `node_modules` tree instead of reusing workstation state. The pinned preparation action performs npm installation and signature audit before the caller reaches release preflight or semantic-release.
- `tools/release-preflight.ps1` runs after package inventory validation and before `npx
  semantic-release`. It executes the existing `release:dry-run` script to read the next version
  from semantic-release without running prepare or publish hooks, converts that version through
  `.releaserc.json` `tagFormat: "v${version}"`, and checks exact local and remote tag refs such as
  `refs/tags/v1.2.3`. It intentionally does not treat similarly prefixed tags such as
  `v1.2.30` as collisions.
  The parser accepts both the ordinary next-version record and semantic-release's
  `There is no previous release, the next release version is <version>` record. Live
  output must use the semantic-release logger prefix; malformed or conflicting terminal
  records fail preflight. A peeled-only exact remote ref also counts as a collision.
  Invalid `RepositoryPath` values name the required checkout directory. A failed local
  ref probe blocks publication and identifies the ref to inspect; failures retain the
  original exception, inner causes and PowerShell script stack for diagnosis. With `-CheckDestinations`, it uses the pinned Builds destination probe to reject any occupied or ambiguous NuGet package or image tag before OIDC login. The semantic-release verify hook repeats that probe; the prepare hook probes again after packing and before tag creation.
- Semantic-release runs the destination preflight and `tools/verify-container-registry.ps1` as its `verifyReleaseCmd`, before
  prepare creates release artifacts or a tag can be published. The verifier first requires the
  unauthenticated `/v2/` response to advertise a Basic `WWW-Authenticate` realm, then opens and
  immediately cancels authenticated OCI upload sessions in `memories`, `memories-mcp`,
  `memories-access-telemetry`, `memories-access-telemetry-clock`, and `memories-domain-service`.
  The challenge check proves challenge-driven clients can select the authfile credential; the
  upload probes exercise the preemptive Basic upload-session authorization path. They do not
  prove a blob/index push succeeded. Every opened probe session must return HTTP 204 when
  cancelled or the release fails closed. Cross-origin upload locations are refused and never
  receive credentials. `authorization.json` records the actual `preemptive-basic` probe mode,
  whether a Basic challenge was observed, the exact repositories, cleanup, owner, consequence
  and reopen trigger; it always records `pushProven: false`.
- `tools/pack-release.ps1` starts the `prepareCmd` for `@semantic-release/exec`; destination validation runs next, followed by the terminal source proof immediately before semantic-release creates the tag.
- `tools/publish-release.ps1` is the `publishCmd` for `@semantic-release/exec`. It validates the
  frozen five-image identity and all prepared archive hashes, then executes the shared publish
  preflight before either publication family writes. After authorization, it attempts both NuGet
  and container publication and writes one aggregate release summary.
- `tools/pack-release.ps1` builds five stable deployment archives, five separate two-platform OCI
  archives, and the caller-rendered production deployment before publication. The exact approved
  Builds `Props/ContainerProvenance.targets` emits source, revision, version, release URL,
  documentation URL and one shared creation instant on every child image.
- `tools/publish-containers.ps1 -PublicationContract` uses `tools/release-containers.json` and
  `container-publication-contract.py` to freeze project mappings, version, source SHA, workflow
  SHA/ref/run/attempt, the approved Builds SHA and helper hashes into `identity.json` and its
  `setSha256`. The current release path requires all five members; a missing, extra or remapped
  project, identity drift, occupied tag or ambiguous destination refuses before registry writes.
- The approved shared preflight runs once per phase: `verify` during archive preparation,
  `publish` before either artifact family writes, and `container` before the first image copy.
  The transport extracts each validated OCI archive without restoring tar ownership, then uses `skopeo copy --all --preserve-digests` from the temporary `oci:` layout. The scoped
  temporary authfile also supplies Docker's digest-bound health pulls and is removed afterward.
  The unchanged approved OCI validator checks the remote index, both child digests, configs and
  provenance against the prepared graph. Docker is never used to push.
- Current publication refuses every occupied version tag. Matching config-digest reconciliation
  remains solely on historical source trees without the current manifest; Story 30.5 owns
  missing-only recovery for current ten-package releases.
- Semantic-release does not commit generated files back to `main`. It creates the release tag,
  GitHub Release, and package assets without using `@semantic-release/git`, so the release path is
  compatible with the protected-branch rule that requires pull requests for repository changes.
- `tools/validate-release-packages.ps1` is the canonical package verifier (used both before
  semantic-release runs and inside `pack-release.ps1` after pack).
- `tools/release-packages.json` is the approved package inventory.
- The current inventory contains ten approved packages: Aspire, Contracts, Client.Rest, Redis,
  Cli, Mcp, ServiceDefaults, EventStore, Telemetry, and EventStoreExtensions. The historical `v1.2.0`
  evidence below remains a record of the seven packages published by that release.
- NuGet.org publication uses Trusted Publishing. The release job exchanges its GitHub OIDC
  token through `NuGet/login` for a one-hour key immediately before semantic-release. The
  repository variable `NUGET_USER` names the individual nuget.org account that owns the policy
  for `Hexalith` / `Hexalith.Memories` / `release.yml` / `production`. No stored NuGet API key
  is read, and the temporary key is never written to a file in the repo.

The required-check names mapping to `.github/workflows/ci.yml` job IDs is documented in
[`branch-protection.md`](./branch-protection.md).

### Five-Image Publication And Retained Evidence

`tools/release-containers.json` fixes the following complete set. Every image requires exactly
`linux/amd64` and `linux/arm64`; health pulls each child by its validated digest in Development.

| Member | Project | Repository | Workload health |
| --- | --- | --- | --- |
| Server | `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` | `memories` | `/alive` liveness |
| MCP | `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` | `memories-mcp` | `/alive` liveness |
| Lifecycle | `src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj` | `memories-access-telemetry` | `/ready` explicitly reports writes disabled |
| Clock | `src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj` | `memories-access-telemetry-clock` | `/ready` verifies a signed attestation |
| DomainService | `src/Hexalith.Memories.DomainService/Hexalith.Memories.DomainService.csproj` | `memories-domain-service` | `/alive` liveness |

The Server liveness lane starts private Redis Stack and FalkorDB dependencies using this repository's
already declared image pins, then injects both keyed connection strings. The stores are test-only
and are removed with their private network; their cleanup result is retained.

The clock health lane generates temporary P-256 signing material, serves it through an isolated
Dapr local-file secret authority, and uses the clock's three Development UTC sources. Every
workload uses its own internal Docker network. Failed container, authority or network cleanup
fails verification. Test keys/tokens are never retained. A disposable, digest-pinned BusyBox
probe shares the workload's network namespace to check health because internal networks expose
no host port. These probes establish the declared publication health contract; they do not
qualify Production clock authorities or enable lifecycle
writes. DW-17 and parked Production qualification DW-730 remain unchanged.

The stable Docker archive names remain `server.tar.gz`, `mcp.tar.gz`, `access-telemetry.tar.gz`,
`access-telemetry-clock.tar.gz`, and `domain-service.tar.gz`. All five bind the same source and
release-set identity. Separate publication files use the corresponding `.oci.tar.gz` suffix.
The renderer stays caller-owned; DomainService remains an archive/image member without adding
hosted Production activation. The ordinary archive-only deployment verifier and historical
archive format remain compatible.

Each OCI child must start exactly like its image's single-RID Docker deployment archive. The build
phase compares each child's `Env`, `Entrypoint`, `Cmd`, `WorkingDir`, `User` and `ExposedPorts` with
the deployment archive's config. The comparison runs after the read-only `verify` preflight (local
preflight evidence and read-only destination probes) and before the `publish` preflight and any
registry write. A mismatch fails the build, and `<member>/runtime-config-parity.json` records each differing platform, field and value.
`archive-compatibility.json` records the SHA-256 of the compared values for the deployment archive
and for each child. The authorize and publish phases and the evidence archiver recompute those
hashes from the archive bytes and refuse a missing or different record. The check exists because
SDK 10.0.403 multi-RID inner builds rebuild container environment variables with `Split(':')[1]`,
which cut the Server `PATH` to `/app`. The `RebindMultiRidContainerEnvironmentVariables` target in
`Directory.Build.targets` restores each declared value in those inner builds only; single-RID
publishes skip it.

`tools/archive-release-evidence.py` refuses success if any required identity, authorization,
preflight, archive compatibility, raw OCI graph, digest-bound health, family summary or aggregate
summary is absent or inconsistent. Its packet hashes every retained record and is attached to
successful GitHub Releases as `hexalith-memories-release-evidence.tar.gz` with a separate SHA-256
receipt. The always-running `release-publication-evidence` upload requests 90 days. On
2026-11-11, `GET /repos/Hexalith/Hexalith.Memories/actions/permissions/artifact-and-log-retention`
returned `days: 90` and `maximum_allowed_days: 90`. Failed Release run `43012088139` retained
artifact `13850599888` until `2027-02-09T11:27:25Z`, confirming the 90-day policy. The earlier
v3.0.24 and v3.0.25 runs below logged the then-effective 30-day cap; that is historical evidence.
Successful packets stay durable as release assets. An incomplete packet is explicitly
`diagnostic-only` and grants no publication-success credit.

The Story 30.2 approval remains bound to Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa`.
The newer CI profile grants no publication approval. Story 30.4's local fixture qualification
controls SDK, registry and process boundaries and does not prove an actual registry push.
Authorized credentialed Release runs `42108808279` (v3.0.24, source `859680b3`) and
`42134622308` (v3.0.25, source `246157a7`) published all five images through this contract. Each
attached `hexalith-memories-release-evidence.tar.gz` and its SHA-256 receipt to its GitHub release.
On 2026-11-10 both receipts matched their packets, and read-only registry reads returned the
packets' exact index and child manifest bytes. Anonymous `/v2/` and manifest reads return HTTP 401
with `Basic realm="zot-distribution"`, so registry reads need the scoped credentials.

**Do not deploy the v3.0.20 through v3.0.25 Server images.** Read-only registry reads on
2026-11-10 show that each of v3.0.20, v3.0.21, v3.0.22, v3.0.23, v3.0.24 and v3.0.25 holds all five
two-platform image indexes, and both Server children of every one carry `PATH=/app` instead of the
declared `PATH`. The Production overlay's `wget` exec startup, readiness and liveness probes
therefore exit 127 and can never pass. The v3.0.20 and v3.0.21 Server children also keep the
relative `dotnet` entrypoint, which `PATH=/app` cannot resolve, so they cannot start at all.
v3.0.19 pushed no image. No packet check compared child runtime config with the deployment archive
before this check existed. Published tags are immutable: never retag, overwrite or delete them.
The corrected [v3.0.26 release](https://github.com/Hexalith/Hexalith.Memories/releases/tag/v3.0.26)
from source `f807701e45297064f3968bcc8052e8002eac6d34` carries the fix. Authorized run
`43018206370` passed all five two-platform OCI graphs, digest-bound Development health and
six-field child/deployment-archive runtime parity checks. Its retained evidence packet has
SHA-256 `f3819e67d3f246e0a5c56dcf854ca34231cb8453753a047b89495541195d70d2`;
all 128 manifest entries re-hash, the registry metadata matches, and a fresh exact-source
checkout reproduces the deployment YAML asset byte-for-byte. Both Server children retain the
full declared PATH. All ten NuGet 3.0.26 package downloads are publicly available. These results
qualify publication; the declared health checks use Development test authorities and disabled
lifecycle writes, with no Production lifecycle qualification credit.
Owner: Jérôme Piquot. Consequence: no v3.0.20–v3.0.25 Server image may be deployed. Reopen
trigger: the owner marks the affected GitHub releases do-not-deploy and the Story 30.6 cutover
refuses those tags. Implementation does not enable the publication flag, change protected
settings, publish a release, or change Production lifecycle state.

### Release-Tooling Dependency Safety

The repository-root npm graph is development-only release tooling. Its previous lockfile reported
six low-or-higher advisories: commitlint reached vulnerable `fast-uri` and `js-yaml` versions, while
semantic-release installed its unused default `@semantic-release/npm` plugin and that plugin's
bundled npm CLI. The web E2E workspace and production .NET artifacts use separate dependency graphs
and were not affected.

`semantic-release` is intentionally pinned to `25.0.9`. The offline configuration verifier loads
that installed version's configuration and constructs its plugin lifecycle without invoking any
lifecycle hook, so the pin is a reviewed compatibility boundary rather than a general update
policy. The normal verifier command also runs fail-closed mutation self-tests. The explicit
`.releaserc.json` allowlist uses only commit analysis, release-note generation, the repository-owned
exec hooks, and GitHub publication. Because `@semantic-release/npm` is not used, the root override
scopes its dependency edge to the official `@semantic-release/error@4.0.0` package alias and
removes the vulnerable npm CLI bundle from the installed and locked graph.

The Hexalith.Memories release and CI maintainers own this exception. Pull requests fail closed when
configuration precedence changes, lifecycle keys shadow the plugin list, the actual installed
package graph differs from the reviewed alias, or any low-or-higher npm advisory returns. Validate
the contract from the repository root with:

```bash
npm ci
npm audit signatures
npm run verify:semantic-release-config
```

Remove the alias and exact semantic-release pin when upstream semantic-release no longer installs
`@semantic-release/npm` by default, or when an upstream plugin release no longer brings the
vulnerable npm bundle. The owning maintainers must confirm a clean low-level audit, migrate the
verifier away from any obsolete internal API, and keep all release configuration and workflow
contract tests green in the same pull request.

### Dispatch and Approval

Wait for successful push CI on the exact current `main` SHA, then dispatch **Release** from `main`. Read back `gh api repos/Hexalith/Hexalith.Memories/environments/production` and require a nonempty `required_reviewers` rule and a non-null `deployment_branch_policy` restricting deployments to `main` or protected branches. Administrator bypass was enabled (`can_admins_bypass: true`) on 2026-11-12 at the owner's explicit request. Administrators may bypass pending production approval through GitHub; current-main CI, destination checks and NuGet Trusted Publishing remain required. A workflow `environment: production` declaration alone is insufficient. The sole owner's self-review is an accepted exception until a second eligible reviewer is available. Set `HEXALITH_RELEASE_PUBLISH_ENABLED` to the exact lowercase value `true` at repository scope only for an authorized publication, and set `NUGET_USER` to the individual account that created the NuGet Trusted Publishing policy. Any other publication-flag value leaves the release frozen before OIDC and semantic-release.

Historical protection evidence: the 2026-11-02 API readback returned `protection_rules: []`, `can_admins_bypass: true` and `deployment_branch_policy: null`. The source job refused entry under that configuration; this readback does not describe the current environment.

On 2026-11-02, Release run `41495447050` published `v3.0.18` from commit `66822bc5`, which temporarily let the owner skip both approval readbacks for one run; `d9d61d8a` removed that bypass. The run proved source, destinations and publication end to end, but it did not exercise an environment approval.

On 2026-11-03, the authorized production configuration was verified with required reviewer `jpiquot`, admin bypass disabled and deployments restricted to branch `main`. Approved frozen Release run `41624526890` on `d7e44d01b66f0800bdb7edca8553934fe44dc145` followed successful push CI `41621265465`. Approval was submitted as `jpiquot` on the user's behalf through the normal production gate, with no bypass; publication stayed frozen.

The source job checks main tip and exact-source push CI before entering production. When publication is enabled, preparation repeats that check after the build; the caller checks once more immediately before login. If main advances or CI cannot be proved at any checkpoint, stop and dispatch again from the new tip after its push CI succeeds. A missing reviewer rule blocks the workflow before the protected job. Never treat a passing workflow declaration or local fixture as proof that the remote approval setting is active.

## Prerequisites

Before relying on the release path:

1. `main` branch protection is enabled (see [`branch-protection.md`](./branch-protection.md)).
2. Pull requests are required before merge.
3. At least one approving review is required.
4. Required checks are exactly `build`, `test-unit-contract`, and `integration-fast`.
5. The production environment has required reviewers and a deployment branch policy for `main` or protected branches. Administrator approval bypass is enabled by the current owner policy. Read it back with the GitHub environments API. Keep `HEXALITH_RELEASE_PUBLISH_ENABLED` unset for a frozen guard test; set it to exact lowercase `true` only for authorized publication.
6. Direct pushes are blocked by the protected-branch policy; the only path to producing a `push`
   event on `main` is merging a PR.
7. `NUGET_USER` names the individual Trusted Publishing policy creator; `HEXALITH_ZOT_USERNAME` and
   `HEXALITH_ZOT_API_KEY` secrets exist. The Zot principal has repository write authorization for
   five flat repositories: `memories`, `memories-mcp`, `memories-access-telemetry`,
   `memories-access-telemetry-clock`, and `memories-domain-service`. `HEXALITH_ZOT_REGISTRY` may override the
   default `registry.hexalith.com` host.
8. The runner has `skopeo`, Docker/buildx, arm64 QEMU, OpenSSL and curl. The guarded workflow
   installs missing skopeo and initializes QEMU only on an enabled publication path. Health also
   pulls four test images, each digest-pinned in `tools/verify-container-health.py`: Docker Hub
   `redis/redis-stack-server:7.4.0-v8` and `falkordb/falkordb:v4.12.0` for the Server
   dependencies, `ghcr.io/dapr/daprd:1.18.1` for the clock authority and the Server/MCP Dapr
   sidecars, and `busybox:1.37.0` for the private-network HTTP probe. Missing tools, emulation
   or image access fail closed.
9. The merge to `main` uses a conventional commit that semantic-release can classify.
10. The local working tree is clean before opening the release PR — no uncommitted edits, no
   in-flight tag drift, no orphan packages in `artifacts/`. The release job builds from the merged
   commit; clean local state is what makes the merged commit predictable.
11. `./tools/validate-release-packages.ps1` passes before release.
12. `tools/release-packages.json` still lists the intended package set.

## Branch Protection Evidence

Before Story 12.1 execution on 2026-04-30, classic branch protection was not configured for `main`.
`gh api` returned HTTP 404 with the standard "branch not protected" body:

```text
$ gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
gh: Branch not protected (HTTP 404)
{
  "message": "Branch not protected",
  "documentation_url": "https://docs.github.com/rest/branches/branch-protection#get-branch-protection",
  "status": "404"
}
```

After Story 12.1 execution on 2026-04-30, the relevant fields of the protection response were:

```text
$ gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
required_status_checks.strict: true
required_status_checks.contexts: build, test-unit-contract, integration-fast
required_pull_request_reviews.required_approving_review_count: 1
enforce_admins.enabled: true
allow_force_pushes.enabled: false
allow_deletions.enabled: false

$ gh api repos/Hexalith/Hexalith.Memories/branches/main
protected: true
```

A repository ruleset named `main` also exists in the repository configuration, but its enforcement
is `disabled` and it only contains deletion and non-fast-forward rules. (`gh api ... /rulesets`
returns no rulesets in `enforcement: active` state — the `main` ruleset is in `disabled`
enforcement state and therefore not active.) The enforced control on `main` for Story 12.1 is the
classic branch protection rule above; the disabled ruleset is left in place but unenforced.

## Observed First Release

Release `v1.2.0` proves the Epic 11 release automation ran end to end. Important sequencing note:
`v1.2.0` was published on 2026-04-30 **before** branch protection was applied later the same day.
The release succeeded against an unprotected `main`. The branch-protection-versus-release-commit
caveat at the end of this section therefore applies to the **next** release, not to `v1.2.0`.

- Source commit (last commit on `main` before the release run): `e6d9e5764582709620a7449df5459d2680619451`
- Release tag commit (the release commit semantic-release created and tagged): `f4b5038b3d495e424f266797b11d8d72b69030b1`
- Tag–commit relationship: `v1.2.0` points at `f4b5038`, whose first parent is the source commit
  `e6d9e57`. The release commit was created by semantic-release after publishing and before
  pushing the tag.
- Release commit message: `chore(release): 1.2.0 [skip ci]`
- Release commit author: `semantic-release-bot` (display name; the commit was authored by the
  semantic-release process running under `GITHUB_TOKEN` in the Actions run below)
- Release commit date: 2026-04-30 05:04:19 UTC
- GitHub Actions run: https://github.com/Hexalith/Hexalith.Memories/actions/runs/25148312032
- GitHub Release: https://github.com/Hexalith/Hexalith.Memories/releases/tag/v1.2.0
- GitHub Release published: 2026-04-30 05:04:38 UTC
- Release job completed: 2026-04-30 05:04:45 UTC

Observed release-job behaviour on 2026-04-30. The first five items below are workflow-level steps
in `.github/workflows/release.yml`. From `npx semantic-release` onward, the work is driven by
semantic-release plugins, not by additional workflow steps.

1. A `push` event on `main` triggered `.github/workflows/release.yml`.
2. The release job checked out the repository (root-declared `references/` submodules enabled;
   `submodules: true`, not `recursive`).
3. The job restored and built `Hexalith.Memories.slnx`.
4. `tools/test-release.ps1` ran the unit and non-Docker release test suite.
5. `tools/validate-release-packages.ps1` validated package inventory metadata before release.
6. `npx semantic-release` ran. Inside that single workflow step:
   - `@semantic-release/commit-analyzer` and `@semantic-release/release-notes-generator`
     calculated version `1.2.0`.
   - `@semantic-release/changelog` updated `CHANGELOG.md` in the working tree.
   - `@semantic-release/exec` invoked `tools/pack-release.ps1` as `prepareCmd` to pack the approved
     package set with the new version, then invoked `tools/publish-nuget.ps1` as `publishCmd` to
     push the resulting packages to nuget.org using `NUGET_API_KEY` from Actions secrets.
   - `@semantic-release/github` created the `v1.2.0` GitHub Release and uploaded the seven `.nupkg`
     assets.
   - `@semantic-release/git` created the release commit `chore(release): 1.2.0 [skip ci]` on `main`
     with the updated `CHANGELOG.md`, then pushed the new tag `v1.2.0`.
7. The `[skip ci]` token in the release commit message stops `release.yml` from re-triggering on
   its own commit; the same token also stops `ci.yml` from running on the release commit.

The release workflow guards against re-entry with:

```yaml
if: "!contains(github.event.head_commit.message, '[skip ci]') && !contains(github.event.head_commit.message, '[skip actions]')"
```

Story 12.1 initially identified a protected-branch compatibility risk because the original
`.releaserc.json` used `@semantic-release/git` to commit `CHANGELOG.md` back to `main`. The
post-merge release run for PR #12 confirmed that failure mode: GitHub rejected the direct push to
`main` with rule violation `GH013`. The current release configuration removes the changelog/git
commit plugins, so future releases should not attempt `HEAD:main` pushes from the release job.

## Release Identity And Forensic Anchors

Release-time writes (tag, GitHub Release, package upload) are produced under
`secrets.GITHUB_TOKEN`. In a GitHub Actions run, `GITHUB_TOKEN` is minted by the GitHub Actions
GitHub App (`github-actions`, `https://github.com/apps/github-actions`) and posts as the bot user
`github-actions[bot]` (GitHub user id `41898282`). This is the identity reviewers should pin in any
forensic comparison; the historical "semantic-release-bot" display recorded for `v1.2.0` was the
author name configured by `@semantic-release/git` for the legacy release commit and is not the
underlying token identity.

For each release, capture the following forensic anchors so a future review can confirm the
release was driven by Actions and not by a workstation:

- The Actions run URL of the `Release` workflow run that produced the tag
  (`github.com/Hexalith/Hexalith.Memories/actions/runs/<run-id>`).
- The tag commit SHA (`git rev-parse v<version>` once the tag exists locally) and its tagger
  identity. With the post-12.1 release config that no longer uses `@semantic-release/git`, the
  tag is created via the GitHub API and is attributed to `github-actions[bot]`.
- The GitHub Release "Created by" user — must be `github-actions[bot]`. Anything else (a personal
  user, a non-github-actions GitHub App) is a forensic red flag and must be investigated before
  any further publish action.
- Whether the workflow run was a `workflow_dispatch` from the current `main` tip after successful exact-source push CI. A `push` event or another source is a red flag.

If any of those checks fail, treat the release as untrusted, do not delete published packages
from nuget.org, and escalate before merging another release-eligible commit to `main`.

## Package Evidence

The approved package set published as part of `v1.2.0` is:

| Package | NuGet latest version (2026-04-30) | GitHub Release asset |
| --- | --- | --- |
| `Hexalith.Memories.Contracts` | `1.2.0` | `Hexalith.Memories.Contracts.1.2.0.nupkg` |
| `Hexalith.Memories.Client.Rest` | `1.2.0` | `Hexalith.Memories.Client.Rest.1.2.0.nupkg` |
| `Hexalith.Memories.Redis` | `1.2.0` | `Hexalith.Memories.Redis.1.2.0.nupkg` |
| `Hexalith.Memories.Cli` | `1.2.0` | `Hexalith.Memories.Cli.1.2.0.nupkg` |
| `Hexalith.Memories.Mcp` | `1.2.0` | `Hexalith.Memories.Mcp.1.2.0.nupkg` |
| `Hexalith.Memories.EventStore` | `1.2.0` | `Hexalith.Memories.EventStore.1.2.0.nupkg` |
| `Hexalith.Memories.Telemetry` | `1.2.0` | `Hexalith.Memories.Telemetry.1.2.0.nupkg` |

The NuGet flat-container API also reports earlier versions `1.0.0` and `1.1.0` for all seven
packages from prior CI publishes; `1.2.0` is the latest version observed on 2026-04-30.

Validation commands used during Story 12.1:

```powershell
./tools/validate-release-packages.ps1
./tools/validate-release-packages.ps1 -PackageDirectory <downloaded-v1.2.0-assets> -Version 1.2.0
```

Both validations passed. Keep this script canonical; do not replace it with manual package
inspection.

### Per-Release Package Audit Evidence

`tools/validate-release-packages.ps1` proves the inventory and metadata shape of the generated
packages, but it does not by itself prove byte-identity or signature provenance. For every
release starting after Story 14.2 closes, capture an explicit per-package audit record alongside
the `Package Evidence` table. Choose at least one of the following options and record the exact
commands that produced the evidence in the release Pull Request body or the GitHub Release notes:

1. **SHA-256 checksums (preferred default).** Hash every `.nupkg` produced under
   `artifacts/packages/release/` and pair the hash with the package id and version. Both
   commands below return the same SHA-256 value for the same input file, so Windows and Linux
   reviewers can compare results directly:

   ```powershell
   # Windows runners (pwsh 7+, Windows PowerShell 5.1)
   Get-ChildItem artifacts/packages/release -Filter *.nupkg |
     ForEach-Object { Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName } |
     Select-Object Hash, Path
   ```

   ```bash
   # Linux/macOS runners
   ( cd artifacts/packages/release && sha256sum *.nupkg )
   ```

   Both forms emit a deterministic `Hash  Path` (PowerShell) or `<hash>  <name>` (sha256sum)
   record. Paste the resulting block into the release evidence under the package table.

2. **`dotnet nuget verify --all` evidence.** When the release introduces or rotates package
   signing, the equivalent provenance check is signature verification. Run the command per
   `.nupkg` and record the `Successfully verified` / `Signature type` lines as audit output.
   Signature verification proves origin; it does not replace SHA-256 for byte-level identity.

   ```powershell
   Get-ChildItem artifacts/packages/release -Filter *.nupkg |
     ForEach-Object { dotnet nuget verify --all $_.FullName }
   ```

3. **`nuget verify -Signatures`.** Functional equivalent to option 2 when only the standalone
   `nuget` CLI is available; record the same successful-verification output.

The historical `v1.2.0` package set is not retroactively backfilled with SHA-256 evidence
because the CI build that produced those `.nupkg` files is no longer available locally; use the
NuGet flat-container API entries plus the existing `Package Evidence` table as the audit record
for that specific version. Future releases must produce per-release evidence before the GitHub
Release is treated as final.

Package publishing remains CI-only. Operators must not run `dotnet nuget push`, manual checksum
spot-checks, or signature spoof checks from a workstation against the canonical NuGet source —
those actions can move the published state away from the CI-recorded audit anchors above.

## Second Release Checklist

1. Merge only through a pull request targeting `main`.
2. Confirm the PR has at least one approval.
3. Confirm required checks `build`, `test-unit-contract`, and `integration-fast` passed.
4. Confirm the merge commit follows conventional-commit semantics.
5. Confirm `tools/release-packages.json` has not drifted.
6. Run `./tools/validate-release-packages.ps1` locally before merging if package files changed.
7. Confirm the final merge or squash commit message does not contain bracketed skip instructions.
8. Merge to `main`.
9. Watch the `Release` workflow run.
10. Confirm `Install release tooling` used `npm ci` from the tracked lockfile.
11. Confirm `Run release preflight` succeeded before `Run semantic-release`.
12. Confirm semantic-release's registry write-scope verification opened and cancelled upload
    sessions for both repositories before prepare/publish work began.
13. If the release preflight fails on a stale tag, confirm the exact blocked ref and resolve the
    tag only after release-owner review.
14. Confirm `Build`, `Test unit and non-Docker suite`, `Validate package inventory before release`,
   `Run release preflight`, `Run semantic-release`, and `Upload package artifacts` all succeed.
15. Confirm the new tag exists.
16. Confirm the GitHub Release contains the ten approved package assets and the versioned
    `hexalith-memories-production.yaml` deployment asset.
17. Confirm NuGet flat-container APIs show the new version for all approved packages.
18. Confirm semantic-release created the new tag and GitHub Release without attempting to push a
    release commit back to `main`.

## Failure And Recovery Notes

- If restore, build, or test fails, fix through a normal pull request and rerun release by merging
  a new conventional commit to `main`.
- If package validation fails, treat `tools/release-packages.json` and
  `tools/validate-release-packages.ps1` as the source of truth. Do not manually publish around the
  validator.
- If semantic-release fails before publishing, inspect the run logs and fix the repository state or
  release configuration through a pull request.
- If registry verification reports a missing Basic `WWW-Authenticate` realm, stop before creating
  a release. Confirm `/v2/` is routed to Zot and that no ingress or health-check sidecar replaces
  its response. Do not work around the failure by making clients send Basic credentials
  preemptively.
- If registry write-scope verification returns HTTP 401 or 403, stop before creating a release.
  An organization or Zot administrator must grant the `HEXALITH_ZOT_USERNAME` principal push
  authorization to `memories`, `memories-mcp`, `memories-access-telemetry`, and
  `memories-access-telemetry-clock`; rotating a valid credential or proving
  registry login/read access does not repair a repository ACL. Never paste the username, API key,
  or authorization header into an issue or workflow log.
- If container pushes fail with `unauthorized: authentication required` after verification passes,
  inspect Zot logs for the failed request before changing credentials. No `Authorization` header
  means either `/v2/` challenge headers were changed after preflight, the skopeo authfile path was
  bypassed, or a docker-daemon push path was reintroduced. An `Authorization` header with HTTP 401
  or 403 instead points to credential or repository-policy rejection. Preserve the skopeo path and
  compare the live `/v2/` response with the verifier evidence before choosing a repair.
- If `tools/release-preflight.ps1` reports that `refs/tags/v<version>` already exists locally or on
  `origin`, stop before publish work starts. The message names the exact conflicting ref. Do not
  delete the remote tag casually; confirm whether it came from an aborted/manual release, record the
  release-owner decision, then either remove the stale tag through the approved repository process
  or merge a new conventional commit that produces a different semantic-release version.
- If semantic-release reports `GH013` or another protected-branch rejection for `HEAD:main`, keep
  branch protection intact. The release configuration should not use plugins that commit back to
  `main`; fix the release configuration through a pull request and rerun by merging a conventional
  commit.
- If NuGet publishing partially succeeds, do not delete published packages from nuget.org. There
  are two failure shapes to keep separate:
  - **HTTP 409 (already published).** The destination preflight refuses an occupied version before tag creation. `dotnet nuget push --skip-duplicate` remains in the publisher for a race after the probe; it does not authorize rerunning a partially published release. Record the exact package state for manual NuGet reconciliation, defined below.
  - **Non-409 failures (auth, network, server error).** `dotnet nuget push` exits non-zero.
    `publish-nuget.ps1` records the failed package, exit code, and sanitized output, then continues
    to later packages where it can still safely invoke `dotnet nuget push`. If a preflight or
    unrecoverable condition prevents more attempts, the remaining packages are listed as
    not-attempted with a reason.
- `tools/publish-nuget.ps1` records a structured package-by-package summary at
  `artifacts/packages/release/publish-summary.json` before it exits non-zero. The summary lists the
  target version, package directory, source, pushed packages, failed packages with exit codes and
  sanitized output, and not-attempted packages.
- When at least one package pushed and at least one package failed or was not attempted, the
  release log includes a GitHub Actions error annotation titled
  `PARTIAL PUBLISH - manual reconciliation required`. The failed job also appends a concise
  Markdown summary to the GitHub Actions step summary when that output is available.
- The release workflow creates a GitHub Issue titled
  `PARTIAL PUBLISH <version> - manual reconciliation required`. If an open issue already exists
  for the same version, reruns add a comment to the existing issue instead of creating a duplicate.
  The issue/comment includes the run URL, version, pushed/failed/not-attempted package lists, and
  this runbook reference.
- The alert helper searches quoted title text among up to 100 open issues and verifies the exact
  title before choosing an incident. Null artifact entries are omitted; empty lists say `None`.
  GitHub transport timeouts, connection failures and HTTP 5xx responses receive at most three
  attempts with visible one- and two-second backoff. HTTP 429 and rate-limit-specific HTTP 403
  or GraphQL errors wait before any further request, including readback: the helper honors
  Retry-After/reset timing surfaced in CLI diagnostics, otherwise using 60/120-second backoff.
  All waits share a 180-second budget; a longer required wait stops the helper without another
  request. Before repeating an ambiguous create or comment, it requires valid readback JSON
  and checks the incident body or exact comment body. If it discovers an older incident, it adds
  the current run's evidence as a comment. Permanent/unclassified failures and failed readback
  stop further writes. Terminal errors retain bounded diagnostics with credentials redacted.
  This recovery does not serialize independent concurrent release runs.
- If the summary cannot be parsed, the alert helper fails before contacting GitHub and names
  the summary path. Preserve the original release failure and all summary evidence, repair or
  regenerate the summary, then rerun the alert step. A failed alert does not reconcile a release
  or justify closing its incident.
- Once a failed publish has created the exact version tag or published any NuGet package, do not
  rerun normal semantic-release for that version and do not delete or republish packages. For a
  current ten-package release, manual NuGet reconciliation means recording the exact pushed, failed
  and not-attempted packages in the PARTIAL PUBLISH issue and escalating to the release owner. Until
  Story 30.5 completes current-set recovery, nothing finishes it: the recovery workflow cannot push
  NuGet, the destination preflight refuses a Release rerun for that version, and publishing stays
  CI-only, so never push a package locally.
  **Recover Partial Release** from `main` accepts a historical nine-package tag with the existing bare version (for example, `2.6.6`).
  The workflow accepts only a semantic-version tag reachable from `origin/main` and never creates
  or moves a tag. It stages the recovery scripts and their passing fixtures from current trusted
  `main`, then checks out and builds the exact tagged source.
- Recovery first proves Zot write scope against the fixed current five repositories, even for historical tags. It then rebuilds and reconciles the tagged Server, MCP, access-telemetry lifecycle and clock immutable tags using the remote image **config digest**. Current ten-package, five-image recovery remains fail-closed until Story 30.5. A matching tag is recorded
  as already present, a missing tag is pushed, and any conflicting digest fails closed.
- Recovery never invokes a NuGet push. Its completion script reads the tagged manifest unchanged: uniform historical `packageId` or current `id` entries are recognized; mixed shapes fail before package downloads. The workflow currently accepts only the historical nine-package, two-or-four-image unit. It validates nuspec id/version, hashes downloaded bytes, and creates or completes the GitHub Release with exactly those package assets plus `hexalith-memories-production.yaml`. Existing matching assets are retained; unexpected names or mismatched bytes fail closed.
- Only after the tagged image unit, package unit, versioned deployment, and all corresponding GitHub Release assets verify does recovery attach the evidence artifact to the matching partial-publish incident and close it. A failed or interrupted run remains safely rerunnable and leaves the incident open.
- Container release members are prebuilt during prepare. On a rerun, an existing immutable image
  tag is accepted only when its remote config digest matches the prebuilt archive; a conflicting
  digest fails closed and requires release-owner reconciliation.
- `tools/publish-release.ps1` writes the cross-family audit record to
  `artifacts/release/publish-summary.json`. The release workflow uploads that aggregate summary and
  its child summaries even on failure, and creates one reconciliation issue for an aggregate
  `partial-publish` state.
- If exact-source push CI reports a test failure, investigate the failing CI lane and rerun that lane after a fix. A failed or missing exact-source `ci.yml` run blocks dispatch.
- Never print, copy, or store the `NUGET_API_KEY` value in logs, docs, screenshots, or repo files.
