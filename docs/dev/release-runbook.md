# Release Runbook

This runbook preserves the observed first release (`v1.2.0`, 2026-04-30)
and legacy release procedures for forensic comparison. Until Hexalith.CiCd
adoption, the guarded Release workflow uses the root npm tooling and retains
the package, container, and evidence checks described below. After adoption,
Hexalith.CiCd's central nightly process owns publication.
Publishing remains CI-only; do not publish release artifacts from a workstation.

## Current Dependency Maintenance (2026-11-18)

The .NET SDK is 11.0.100. The root release tooling uses Node.js 26.13.1.
Web E2E retains Node typings 26.6.6 and its own manifest and lockfile in
`tests/Hexalith.Memories.Web.E2E`.

FrontComposer retains its supported net10 project surfaces. Its shared Builds catalog view selects
OpenIdConnect 10.0.15 for the exact Shell-dependent project identities, including transitive central
package pins; Memories net11 consumers retain 11.0.0. Keep FrontComposer's catalog import-only.
The clock project and its Dapr sidecars wait for Redis readiness and rewritten component files,
so their pubsub connection uses the allocated endpoint instead of the initial port 6379 placeholder.

Current CI, guarded release, publication profiles and caller validators must all select the Builds
revision recorded by the root gitlink. After a catalog repair is committed and pushed in Builds,
refresh every current execution pin together. Keep the older `6ad5854` and `f0feeb2` fetches for
immutable qualification captures; those captures retain their original source and hashes.

Before dispatch, require successful exact-source CI and a fresh dedicated publication qualification
run at the final Memories and Builds revisions. Record those final workflow links with the release
verification, then read every package in `tools/release-packages.json` back from NuGet at the released
version. Local restore/build and offline fixture success do not replace those remote gates.

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
images (Server, MCP, DomainService, access-telemetry lifecycle and clock). Guarded release uses the exact reviewed `prepare-domain-release` SHA in `release.yml` with the same `builds-execution-sha`, an explicit count of ten packages, and caller-owned NuGet Trusted Publishing. Current-set recovery uses retained bytes for all ten packages and five images. Historical recovery remains compatible with tagged `packageId` manifests.

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
- The production job calls pinned `prepare-domain-release@38e038d1179b1c829ba47a45bfa0a4683ea5e2a5` with the identical `builds-execution-sha` and `expected-package-count: 10`. It restores and builds; only when publication is enabled does it then repeat the exact-source CI proof, because a frozen run exits its publish gate before that repeat. The caller validates the package manifest, checks local/remote tag and every versioned package/image destination, then reproves source immediately before OIDC login and `npx semantic-release`. Application tests are owned by exact-source CI and are not repeated in release. Semantic-release then drives the prepare, publish, and post-publish
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
  remains solely on historical source trees without the current manifest; Recover Partial Release
  completes current ten-package sets from retained, hash-bound bytes.
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
trigger: the owner marks the affected GitHub releases do-not-deploy. Story 30.6 documents
refusal of v3.0.19–v3.0.25 as rollback targets; the external do-not-deploy action remains open. The v3.0.26 publication above was a separately authorized guarded release.
Local implementation and review do not authorize another release, publication-flag or protected-setting
changes, or Production lifecycle writes.

### Release-Tooling Dependency Safety

The repository-root npm graph is development-only release tooling. Its previous lockfile reported
six low-or-higher advisories: commitlint reached vulnerable `fast-uri` and `js-yaml` versions, while
semantic-release installed its unused default `@semantic-release/npm` plugin and that plugin's
bundled npm CLI. The web E2E workspace and production .NET artifacts use separate dependency graphs
and were not affected.

`semantic-release` is pinned to `26.0.1` and runs on Node.js 26.13.1 in the release workflow.
The offline verifier uses the public `resolveConfig` function from the installed
`@semantic-release/core` dependency to verify the resolved configuration and plugin lifecycle
without invoking lifecycle hooks. The normal verifier command also runs fail-closed self-tests.
The explicit `.releaserc.json` allowlist uses only commit analysis, release-note generation, the repository-owned
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

Reevaluate the alias when upstream semantic-release no longer installs
`@semantic-release/npm` by default, or when an upstream plugin release no longer brings the
vulnerable npm bundle. Confirm a clean low-level audit and run the release configuration and
workflow contract tests in the same pull request.

### Release Workflow Action And Base-Image Pins

`release.yml` and `recover-partial-release.yml` publish, so every action they run is a reviewed
full-SHA identity. The same rule covers any job, in any workflow, that holds `contents: write`,
`id-token: write` or `write-all`, whether declared on the job, inherited from the workflow, or
received from the repository default token. Hexalith.Builds actions run at the approved execution
pin `38e038d1179b1c829ba47a45bfa0a4683ea5e2a5`, never `@main`. Each third-party action runs at the
commit its major tag resolved to on 2026-11-16, and the line carries that tag as a comment:

| Action | Tag | Pinned SHA |
| --- | --- | --- |
| `actions/checkout` | v7.0.1 | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
| `actions/upload-artifact` | v7.0.2 | `cf430e030ddbb5b0abf93d22962f4752f3646cd9` |
| `actions/setup-dotnet` | v6.1.0 | `0f4a354dd5547a28c70b7853dbd95d5b67886e73` |
| `azure/setup-kubectl` | v4.0.1 | `776406bce94f63e41d621b960d78ee25c8b76ede` |
| `docker/setup-qemu-action` | v3.7.0 | `c7c53464625b32c7a7e944ae62b3e17d2b600130` |
| `NuGet/login` | v1.2.0 | `8d196754b4036150537f80ac539e15c2f1028841` |

`Workflows_ExternalActions_UseAllowedRefs` in `CiTestInventoryTests.cs` enforces the rule and names
the workflow, job and reference it refuses. Unprivileged `ci.yml` jobs keep the major-tag and
Builds `@main` policy. A Dependabot `github-actions` update to one of these pins fails until the
reviewed table in that test changes in the same pull request. Resolve a replacement with
`git ls-remote https://github.com/<owner>/<repo>.git 'refs/tags/<tag>' 'refs/tags/<tag>^{}'`, taking
the peeled `^{}` commit for an annotated tag, and never move a version inside an unrelated change.
Every checkout in both workflows sets `persist-credentials: false`. semantic-release pushes the
release tag through its own `x-access-token:$GITHUB_TOKEN` URL, so no write token stays in
`.git/config` for the build and publication hooks.

An action-pin refresh changes all of these in one pull request:

- every matching `uses:` line and its `# vX.Y.Z` comment in `release.yml` and
  `recover-partial-release.yml`;
- `ReviewedPinnedActions`, and `NuGetLoginAction` for `NuGet/login`, in
  `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`;
- the `NuGet/login` pin in `tests/tooling/release_dispatch/release_dispatch_test.py`;
- the table above.

`Directory.Build.targets` digest-pins the container base image as
`mcr.microsoft.com/dotnet/aspnet:11.0-alpine@sha256:d39b72ad1534fa0faf52b488bffb0762df7ba269d120f0338713d4dda2a41bf5`,
the index (manifest list) digest resolved on 2026-11-16. Dependabot tracks no Docker ecosystem here,
so the base never moves silently. Jérôme Piquot owns the refresh. Refresh at each monthly .NET 11
servicing release, or on an Alpine or .NET base-image security advisory, whichever comes first:

```bash
docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:11.0-alpine
```

Take the top-level `Digest`, not a platform child's. In one pull request, update the
`ContainerBaseImage` value in `Directory.Build.targets`, the `ReviewedContainerBaseImage` constant in
`CiTestInventoryTests.cs` and the reference in this section. Then require green push CI (including
`ContainerBaseImage_IsDigestPinnedAndInheritedByEveryProject`) before the next dispatch.
SDK 11 keeps the full pinned reference in each child's `org.opencontainers.image.base.name` label;
the approved Builds validators compare only the provenance labels.

### Dispatch and Approval

Wait for successful push CI on the exact current `main` SHA, then dispatch **Release** from `main`. Read back `gh api repos/Hexalith/Hexalith.Memories/environments/production` and require a nonempty `required_reviewers` rule and a non-null `deployment_branch_policy` restricting deployments to `main` or protected branches. Administrator bypass was enabled (`can_admins_bypass: true`) on 2026-11-12 at the owner's explicit request. Administrators may bypass pending production approval through GitHub; current-main CI, destination checks and NuGet Trusted Publishing remain required. A workflow `environment: production` declaration alone is insufficient. The sole owner's self-review is an accepted exception until a second eligible reviewer is available. Set `HEXALITH_RELEASE_PUBLISH_ENABLED` to the exact lowercase value `true` at repository scope only for an authorized publication, and set `NUGET_USER` to the individual account that created the NuGet Trusted Publishing policy. Any other publication-flag value leaves the release frozen before OIDC and semantic-release.

Historical protection evidence: the 2026-11-02 API readback returned `protection_rules: []`, `can_admins_bypass: true` and `deployment_branch_policy: null`. The source job refused entry under that configuration; this readback does not describe the current environment.

On 2026-11-02, Release run `41495447050` published `v3.0.18` from commit `66822bc5`, which temporarily let the owner skip both approval readbacks for one run; `d9d61d8a` removed that bypass. The run proved source, destinations and publication end to end, but it did not exercise an environment approval.

On 2026-11-03, the authorized production configuration was verified with required reviewer `jpiquot`, admin bypass disabled and deployments restricted to branch `main`. Approved frozen Release run `41624526890` on `d7e44d01b66f0800bdb7edca8553934fe44dc145` followed successful push CI `41621265465`. Approval was submitted as `jpiquot` on the user's behalf through the normal production gate, with no bypass; publication stayed frozen.

The source job checks main tip and exact-source push CI before entering production. When publication is enabled, preparation repeats that check after the build; the caller checks once more immediately before login. If main advances or CI cannot be proved at any checkpoint, stop and dispatch again from the new tip after its push CI succeeds. A missing reviewer rule blocks the workflow before the protected job. Never treat a passing workflow declaration or local fixture as proof that the remote approval setting is active.

## Release Cutover Parity

Story 30.6 retroactively compares the guarded caller introduced by Story 30.3
(`8aee7a1323160cb39d7b1415256dbfdcb032939a`) with its predecessor
`46667b0d25c707bcdc961d59895bfe5a064bfd12`. D17 retains Memories' caller-owned
publisher. Adopting Builds `domain-release.yml` is outside this cutover.

The evidence lives in
[`30-6-cutover/parity-matrix.md`](../../_bmad-output/implementation-artifacts/30-6-cutover/parity-matrix.md),
[`dry-run.md`](../../_bmad-output/implementation-artifacts/30-6-cutover/dry-run.md) and
[`rollback-rehearsal.md`](../../_bmad-output/implementation-artifacts/30-6-cutover/rollback-rehearsal.md).
The inventory remains ten packages and five images. The Release asset set grows from eleven
to thirteen by adding the release-evidence archive and its receipt. Source/protection checks,
pinned preparation, destination checks and current retained-byte recovery strengthen the old path.
Duplicate-response success, external version-floor omission, terminal-tag recheck and incident
eligibility remain shared gaps with named owners and `epic-30-retrospective` triggers in the matrix.
The NuGet child-summary wording also remains an explicit hand-off. No release-path fix is implied.

The owner selected dispatched v4.0.0 run
[43432588932](https://github.com/Hexalith/Hexalith.Memories/actions/runs/43432588932)
as the controlled rehearsal. It succeeded at source
`c4ee997702ede948ada5f99eeae7b3782e92003b` with thirteen assets. It and the recorded
v3.0.19–v3.0.22 failures ran code from before Story 30.5. They do not prove the final 30.4-fix/30.5 source.
Offline fixtures prove local behavior only. Live proof for the final 30.4-fix/30.5 code remains
an owner obligation on the next authorized release.

Removal requires a reviewed matrix with zero `regression` rows and passing checkpoint evidence.
The removal record in the matrix is authoritative. The ordinary developer test command is
`./tools/test.ps1`. The 2026-11-16 removal retired `tools/test-release.ps1` after the
coordinator verified zero matrix regressions, the rollback lanes qualified, and all published
identities remained byte-identical across the complete rehearsal. The C# inventory now guards
the old runner's absence. The historical container fixture's complete-module fallback is recorded
in the rehearsal; full-discovery flakes remain retained rather than reported as passing runs.

Before the next owner-dispatched release, Jérôme Piquot records the intended version and exact
successful-CI source, then captures every published identity. These reads require `gh` authentication
and registry read credentials in the local Docker credential store. A redirect, 401/403/5xx, timeout
or unparseable member refuses the entire capture; investigate the named member before proceeding.
Historical image tags are captured as their exact top-level SHA-256 digest and media type,
including recognized OCI/Docker single-image manifests and indexes. Capture verifies the original
response bytes against the registry digest and refuses unknown formats. Existing entries come
from the before-snapshot; there is no historical allowlist or version cutoff. Only the five new
version tags in an additive release must be image indexes. Use a fresh directory so prior evidence
cannot be overwritten. Keep it in a tracked, exact-byte location: `artifacts/` is git-ignored, and
the root `text=auto eol=crlf` rule would rewrite snapshot bytes on checkout and break the receipts.
The `30-6-cutover/**` path below carries the `-text` rule in `.gitattributes`; commit both files
and their receipts:

```bash
VERSION=4.1.0 # replace with the approved semantic-release candidate (feat e201f169 follows v4.0.0)
SOURCE_SHA='REPLACE_WITH_FULL_APPROVED_CURRENT_MAIN_SHA'
EVIDENCE=_bmad-output/implementation-artifacts/30-6-cutover/next-release-$VERSION
python3 -B tools/release-identity-snapshot.py capture --output "$EVIDENCE/identity-before.json"
```

Retain this before-snapshot and its `.sha256` receipt before the separately authorized dispatch.
After the release finishes, capture again and require the additive-only check. NuGet's
flat-container and registration indexes can lag the push; an early after-capture refuses with
`flat-container/registration version sets disagree` and writes nothing, so wait and capture again.
Never edit a snapshot. A disagreement that persists after NuGet has finished validating the new
versions is an incident: investigate the named member.

```bash
python3 -B tools/release-identity-snapshot.py capture --output "$EVIDENCE/identity-after.json"
python3 -B tools/release-identity-snapshot.py compare "$EVIDENCE/identity-before.json" "$EVIDENCE/identity-after.json" --expect additive --version "$VERSION" --source-sha "$SOURCE_SHA"
```

The only permitted delta is one new tag peeled to that source, one published Release containing
exactly the thirteen expected nonempty digested assets, ten new listed package versions and five
new image-index tags. Latest may stay unchanged or move to this Release. Every prior tag object,
peeled commit, Release state/asset identity, package listed state and image digest/media type stays unchanged.
An unexpected delta leaves the checkpoint incomplete. After any publication, continuation is
retained-byte recovery or a named blocker; never rerun publication to manufacture a passing delta.

| Checkpoint | Owner | Exact command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Retrospective caller parity and removal | Memories Release Engineer | `30-6-cutover/parity-matrix.md` | reviewed 2026-11-16 | complete; zero regression rows; old runner retired 2026-11-16 |
| Next-release before-snapshot | Jérôme Piquot | `capture --output "$EVIDENCE/identity-before.json"` above | accepted 2026-11-16 (D2 obligation) | not complete; consequence: no final-code live publication credit; reopen trigger: the next owner-authorized dispatch |
| Final-code live additive proof | Jérôme Piquot | `compare ... --expect additive --version "$VERSION" --source-sha "$SOURCE_SHA"` above | accepted 2026-11-16 (D2 obligation) | not complete; consequence: no final-code live publication credit; reopen trigger: the next owner-authorized dispatch followed by this comparison |

## Release Rollback

A rollback is a normal reviewed PR restoring the release surface to the **latest fully released
guarded tag**, currently **v4.0.0**. Re-establish that selection with read-only tag, Release,
package and image evidence before preparing the PR; a tag alone never qualifies. Refuse
**v3.0.19–v3.0.25**: v3.0.19–v3.0.22 are partial tags, and the affected v3.0.20–v3.0.25 Server
images carry the do-not-deploy defect described above. The pre-30.3 caller
`46667b0d25c707bcdc961d59895bfe5a064bfd12` is never a target: it publishes on push and ignores
the publication flag. Do not restore a push-triggered caller.

1. Record the blocker, selected guarded tag, fully released evidence and owner in the PR. From
   the repository root, choose a fresh tracked exact-byte evidence directory outside any disposable
   `$TMPDIR` and capture the before-snapshot. Refuse an ambiguous live read:

   ```bash
   TARGET_TAG=v4.0.0 # the re-established latest fully released guarded tag
   ROLLBACK_EVIDENCE="$PWD/_bmad-output/implementation-artifacts/30-6-cutover/rollback-$(date -u +%Y%m%dT%H%M%SZ)"
   python3 -B tools/release-identity-snapshot.py capture --output "$ROLLBACK_EVIDENCE/identity-before.json"
   ```

2. From a clean checkout at the PR base, run preparation and all scratch validation inside one
   foreground shell job that owns a private, disposable `$TMPDIR` (on the maintainer workstation,
   `run-with-temp bash`; elsewhere, a dedicated shell after `export TMPDIR="$(mktemp -d)"`).
   Keep that job alive until every child has finished and cleanup completes; retain requested
   logs outside its private `$TMPDIR`. Materialize both the
   Memories worktree and a separate worktree of the unchanged **root-declared** Builds gitlink.
   A plain directory beneath an uninitialized gitlink lets historical fixtures skip and supplies
   no qualification. From the repository root, inside that wrapper job:

   ```bash
   SOURCE_ROOT=$PWD
   BASE_SHA=$(git rev-parse HEAD)
   BUILDS_SOURCE="$SOURCE_ROOT/references/Hexalith.Builds"
   BUILDS_SHA=$(git rev-parse "$BASE_SHA:references/Hexalith.Builds")
   SCRATCH_ROOT="$TMPDIR/memories"
   git -C "$BUILDS_SOURCE" cat-file -e "$BUILDS_SHA^{commit}"
   git -C "$BUILDS_SOURCE" cat-file -e 'ae77d5690a9411a2154fa4ed5231edc373942573^{commit}'
   git -C "$BUILDS_SOURCE" cat-file -e '81c88b34d09d34260b92d7f0928dbbc1c7446baa^{commit}'
   git worktree add --detach "$SCRATCH_ROOT" "$BASE_SHA"
   git -C "$BUILDS_SOURCE" worktree add --detach "$SCRATCH_ROOT/references/Hexalith.Builds" "$BUILDS_SHA"
   git -C "$SCRATCH_ROOT/references/Hexalith.Builds" rev-parse --show-toplevel
   cd "$SCRATCH_ROOT"
   ```

   Require every availability check to pass, and the final top-level path to equal the scratch
   Builds path. The pinned preparation/publication helper and historical recovery commits must
   already exist in the local root-declared Builds object store; stop with a named blocker if
   any is absent. Do not fetch or update a dependency, pin, allowlist or nested submodule to
   manufacture availability. The 2026-11-15 rehearsal restored the inventory in
   [`30-6-cutover/rollback-surface.txt`](../../_bmad-output/implementation-artifacts/30-6-cutover/rollback-surface.txt)
   at a base that still carried the old runner. It covers caller, publisher, recovery tools and
   their tests, including files absent at the target. After the Story 30.6 retirement, restore that
   inventory with two exclusions. Never restore `tools/test-release.ps1`: D4 retired it and no
   guarded caller invokes it. Never restore `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
   wholesale: it also carries non-release inventory facts, the retired runner's absence fact and the
   deferred-work schema guard. Restore the remaining paths with
   `git restore --source "$TARGET_TAG" --worktree -- <paths>`. Dependencies, Builds gitlinks/execution
   pins and recovery allowlists need their own authorization if a future target would require
   changes. Check the actual diff before making a PR.
3. Run `actionlint .github/workflows/release.yml .github/workflows/recover-partial-release.yml`
   and each of the following exact fixture commands in that worktree, inside the same wrapper
   job. Require exit 0 **and zero skips for every lane**, including every isolated fallback
   module; a missing Docker prerequisite or Builds checkout is a blocker, not a passing lane:

   ```bash
   python3 -B -m unittest discover -s tests/tooling/release_packages -p '*_test.py' -v
   python3 -B -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' -v
   python3 -B -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v
   NUGET_SCRATCH="$TMPDIR/pppppppppppppppppppp"
   mkdir -p "$NUGET_SCRATCH"
   TMPDIR="$NUGET_SCRATCH" TMP="$NUGET_SCRATCH" TEMP="$NUGET_SCRATCH" TERM=dumb python3 -B -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v
   python3 -B -m unittest discover -s tests/tooling/publish_containers -p '*_test.py' -v
   ```

   The twenty-character padding keeps the exact historical v4.0.0 NuGet diagnostic assertion
   intact under PowerShell line wrapping; the scratch directory remains beneath the wrapper
   TMPDIR. The passing rehearsal changed no source, fixture or output.

   Exact v4.0.0 full container discovery can hit its historical HTTP/1.0 fixture race: a
   bodyless 204 response closes the socket without an explicit `Connection: close`, and
   the next synchronous DELETE can race that teardown. Current source already fixes the
   fixture; do not patch the selected rollback target to obtain a pass. Retain the failure
   log, then use the focused-validation fallback: run each complete module in a fresh
   Python process, once, with no skipped assertions:

   ```bash
   FALLBACK_FAILED=
   for MODULE in partial_release_completion_test.py publication_contract_test.py publish_containers_test.py registry_authorization_test.py release_orchestration_test.py
   do
     python3 -B -m unittest discover -s tests/tooling/publish_containers -p "$MODULE" -v || { echo "named blocker: $MODULE failed" >&2; FALLBACK_FAILED=1; break; }
   done
   ```

   A failed module does not exit the wrapper job: still run the step-4 cleanup, then report
   the blocker.

   Require the aggregate executed IDs to equal the entire discovered set, with zero skips.
   The 30.6 rehearsal executed 104/104 exact IDs in those five processes (9/50/26/12/7).
   `rollback-d6-isolated-modules-verified.json` records that equality against unchanged raw
   passing logs. Its provenance retains the original ad hoc collector error: that collector
   duplicated method names in verbose IDs, so its equality check failed even though every
   test subprocess passed. The verified record reads the canonical parenthesized IDs; no
   test was rerun or changed for that metadata correction. Record any remaining failed
   module as a named blocker and keep the rollback checkpoint incomplete.

   A rollback PR that restores this target's `tests/tooling/publish_containers` also inherits the
   race in CI's `test-unit-contract` job, which runs the full discovery. Rerun a failed CI job only
   when the failure is this named fixture race, and require the rerun to pass in full. Otherwise
   record a named blocker. Patching the restored fixture changes the target and needs its own
   authorization.

   The C# inventory class cannot build in this scratch layout, because only the Builds gitlink is
   materialized. Run it in the PR branch checkout, with its root-declared submodules, after applying
   the same restore. Adjust only the release-surface facts it reports, record that adjustment in
   the PR, and require the class to pass there and in PR CI:

   ```bash
   dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --artifacts-path "$TMPDIR/cli-tests" -m:1
   DiffEngine_Disabled=true dotnet exec "$TMPDIR/cli-tests/bin/Hexalith.Memories.Cli.Tests/debug/Hexalith.Memories.Cli.Tests.dll" -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests
   ```

4. Return to the source checkout, capture the after-snapshot from the read-only destinations into
   the same evidence directory, then compare:

   ```bash
   cd "$SOURCE_ROOT"
   python3 -B tools/release-identity-snapshot.py capture --output "$ROLLBACK_EVIDENCE/identity-after-rollback.json"
   python3 -B tools/release-identity-snapshot.py compare "$ROLLBACK_EVIDENCE/identity-before.json" "$ROLLBACK_EVIDENCE/identity-after-rollback.json" --expect identical
   ```

   Require exit 0 and retain both receipts and test logs outside the disposable directory.
   After every foreground child has finished, remove the scratch Builds worktree **first**, then
   Memories, using their owning repositories, before the wrapper exits:

   ```bash
   cd "$SOURCE_ROOT"
   git -C "$BUILDS_SOURCE" worktree remove --force "$SCRATCH_ROOT/references/Hexalith.Builds"
   git worktree remove --force "$SCRATCH_ROOT"
   ```

   Perform that cleanup on failure too, without touching unrelated worktrees. Never push the
   rehearsal. The checked local diff becomes the reviewable PR;
   merging or dispatching it requires the ordinary owner-controlled process.

5. After the rollback PR merges and its push CI finishes, capture again and require that the
   merge published nothing:

   ```bash
   python3 -B tools/release-identity-snapshot.py capture --output "$ROLLBACK_EVIDENCE/identity-after-merge.json"
   python3 -B tools/release-identity-snapshot.py compare "$ROLLBACK_EVIDENCE/identity-before.json" "$ROLLBACK_EVIDENCE/identity-after-merge.json" --expect identical
   ```

Rolling back to v4.0.0 drops the retained-byte recovery bundles introduced in Story 30.5.
A subsequent failed release can therefore lack the current-set recovery bytes. Preserve every
available immutable artifact and record a named blocker when recovery prerequisites are absent.
After any package, image, tag or Release has been published, a rollback cannot undo that publication:
continue through qualified recovery or a named blocker. Never delete, retag, overwrite or unlist
published identities, and never move Latest as part of rollback.

| Checkpoint | Owner | Exact command or artifact | Review state | Completion state |
| --- | --- | --- | --- | --- |
| Fully released guarded target and refusals | Memories Maintainer | This rule plus `30-6-cutover/parity-matrix.md` | reviewed 2026-11-16 | documented; live target proof recorded in matrix |
| Local rollback release lanes | Platform Operations | `30-6-cutover/rollback-rehearsal.md` | reviewed 2026-11-16 | complete 2026-11-16; actionlint and five lanes pass; containers 104/104 through isolated modules after retained full-discovery fixture flakes |
| Published identities unchanged | Platform Operations | `compare identity-before.json identity-after-rollback.json --expect identical` | reviewed 2026-11-16 | complete 2026-11-16; the before, after and supplemental snapshots are byte-identical |

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
  - **HTTP 409 (already published).** The destination preflight refuses an occupied version before tag creation. `dotnet nuget push --skip-duplicate` remains in the publisher for a race after the probe; it does not authorize rerunning a partially published release. Preserve the package summary and follow the retained-byte Recover Partial Release procedure below. The child publisher still emits legacy manual-reconciliation guidance; the shared-gap parity row re-targets that guidance and duplicate-response handling to `epic-30-retrospective`.
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
- Once a failed publish has created the exact version tag or published any member, dispatch
  **Recover Partial Release** from live `main` with `version` (without `v`) and `release-run-id`
  (the failed `release.yml` run). Preserve the incident and the `release-recovery-bundle` artifact.
  Recovery never moves a tag or overwrites an occupied package, image or Release asset.
  Do not rerun the failed `release.yml` run: a rerun increments `run_attempt`, while the frozen
  bundle remains bound to its original attempt. An attempt mismatch refuses recovery; preserve
  the original run and escalate it to Jérôme Piquot rather than editing its identity.
  Never push packages or images locally to complete a partially published version. Use the
  production-approved recovery workflow so the full plan, provenance and destination checks run.
- The read-only source job proves the live `main` tip and successful exact-source push CI, then
  reads back the production reviewer and deployment-branch rules. The recovery job waits for
  `environment: production` approval and repeats those proofs before staging executable tools.
  A source-job protection refusal retains its generated readback in
  `partial-release-source-evidence-v<version>` even when the recovery job is skipped.
- Current tags with `tools/release-containers.json` require the failed run's unexpired
  recovery artifact (90 days requested; the last recorded repository cap is 30 days, so inspect expiry). The workflow verifies GitHub's artifact SHA-256, the bundle receipt and every
  file hash, recomputes `setSha256`, and binds the tag commit, failed-run head, version and tagged
  ten-package/five-image manifests. Earlier failed current releases without a bundle refuse;
  rebuilding cannot reproduce their prepared OCI/package identity.
  Retention runs only on workflow failure: cancellation, runner loss or job timeout may leave no
  recovery bundle. A missing/expired bundle is a blocker. Recovery also refuses any run whose
  conclusion is not `failure`, including `cancelled` and `timed_out`, even if bytes were retained.
- Before any registry, NuGet or Release read or write, recovery re-renders the retained deployment
  with only the tagged renderer and its tagged inputs. `git archive <tag> -- tools/render-production-deployment.ps1
  deploy/kubernetes` exports them into private scratch through the tagged tree's own
  `.gitattributes`, so they carry the bytes the release runner's checkout rendered. That matters
  because `redis-stack.conf` materializes as CRLF and kustomize hashes it into the
  `redis-stack-config-<hash>` ConfigMap name: v4.0.0 published `444ff7t4hh`, while LF blob bytes
  (the earlier `git show` path) give `tfmk6ftkcm`. The render must equal the retained
  `hexalith-memories-production.yaml` byte for byte, or recovery refuses with "Retained deployment
  differs". It refuses before rendering when `--source-root` is not its own Git checkout (such as
  an empty `submodules: false` directory, where Git would read the enclosing repository), when the
  tag commit is absent (a shallow clone: the recovery checkout needs the full tagged history,
  `fetch-depth: 0`), and when an archived input is not a regular file, such as a symlink.
- Recovery loads Builds helpers at the SHA and byte hashes recorded in the identity. Jérôme Piquot
  approved `81c88b34d09d34260b92d7f0928dbbc1c7446baa` and
  `ae77d5690a9411a2154fa4ed5231edc373942573` for recovery on 2026-11-15. The 2026-11-18
  dependency update adds `38e038d1179b1c829ba47a45bfa0a4683ea5e2a5` for new releases.
  Subsequent SHAs require recorded owner approval and an explicit allowlist edit before recovery can write.
- Before the first destination write, `artifacts/recovery/current/recovery-plan.json` records all
  fifteen members as present-identical (skip), absent (publish retained bytes), present-different
  (refuse), or ambiguous (refuse). Authenticated registry manifests and the complete OCI graph
  must match; NuGet compares every ZIP entry byte for byte, ignoring only `.signature.p7s` added
  by repository signing. A flat-container 404 is absent, including NuGet's validation window;
  after a successful push, recovery polls that package read-only for up to 600 seconds and never
  repeats the push. Only an identical package clears uncertainty. A validation timeout, subsequent
  push conflict or 401/403/5xx response fails closed and grants no completion credit. Preserve the
  progress evidence, allow NuGet validation to finish, then rerun **recovery**, not the failed release.
  An unverified push stops subsequent writes. When the same saved progress records an unresolved
  earlier push, another 404 does not authorize a retry; positive identical readback must settle it.
  The original plan stays unchanged. `recovery-progress.json` records actual attempted writes and
  their read-only rechecks, including verified side effects and uncertainty after a failed command.
- Recovery rechecks each destination immediately before its write, copies missing images with
  `--preserve-digests`, and runs digest-bound health for any image without a passing retained
  health record. Only when packages are absent does the workflow invoke `NuGet/login` (v1.2.0,
  pinned to `8d196754b4036150537f80ac539e15c2f1028841`), using `vars.NUGET_USER`, immediately before
  retained-package publication. No stored NuGet key is used.
  The owner must configure Trusted Publishing for
  `Hexalith/Hexalith.Memories/recover-partial-release.yml/production`; until then, package recovery
  fails at login with Jérôme Piquot as the named owner. Image- and asset-only recovery can proceed.
- After all fifteen members verify, recovery creates or completes the Release with exactly thirteen
  assets: ten retained nupkgs, the tagged renderer's four-workload `hexalith-memories-production.yaml`,
  an evidence packet and its receipt. An already-uploaded valid ordinary packet or its matching receipt
  is preserved from the retained bundle byte for byte; otherwise recovery emits a packet containing
  hash-bound raw OCI validation, health records and logs. DomainService remains a publication member with health proof;
  it is not invented as a hosted deployment. A draft may contain a hash-identical asset subset;
  missing assets are uploaded and verified before publishing it with `--latest=false`, preserving the
  repository's existing Latest release. Unexpected or different assets refuse.
  A completed rerun performs zero registry, NuGet or GitHub writes and reuses the verified immutable packet.
- Historical trees without the contract retain their two- or four-image/nine-package completion.
  Their inventory is derived from the tagged publisher's literal candidate table, parsed as data
  without executing tagged code, and validated before any registry mutation. Authorization and both
  staged build/push calls are restricted to that derived repository filter. Projects merely present
  in the tree do not enlarge the published set: v2.11.0–v2.15.x retain two images, while v2.16.0
  through v3.0.11 retain four. They rebuild historical archives and require all nine packages already on nuget.org.
  The precontract five-image/ten-package tags (v3.0.12–v3.0.18) are unsupported and refuse before `-Push`.
- Only after members, assets and packet verification does recovery close the matching incident.
  The always-uploaded `partial-release-completion-v<version>` artifact retains plans, diagnostics,
  health and the completion summary. Fixture success proves the refusal and orchestration logic;
  it supplies no live NuGet, registry or production authorization proof.
- Open registry follow-ups (owner: Jérôme Piquot): registry immutability/race policy remains unverified
  until authoritative policy readback or an isolated concurrent-writer registry fixture. The offline
  manifest-PUT failure fixture now proves recovery re-probes a landed tag and skips it on rerun;
  the original release-path transport summary and live interruption behavior remain unqualified.
  Preserve the packet and escalate an uncertain or conflicting tag to the owner before retrying.
- Recovery blockers and next actions (owner: Jérôme Piquot):
  - Missing/expired recovery bundle, unsupported run conclusion or attempt drift: preserve the failed
    run and incident; the owner must select an intact supported run or approve a separate release.
    Never rebuild or rebind the immutable current set to bypass the refusal.
  - Missing Trusted Publishing policy: the owner configures the named recovery policy, then reruns
    recovery. Image- and asset-only recovery does not need NuGet login.
  - Unapproved Builds SHA/helper hash, source/version/inventory or artifact digest mismatch: retain
    diagnostics; the owner reviews the recorded identity and approved helper bytes before retrying.
  - Present-different package/image/asset or ambiguous destination response: preserve plan/progress
    and the incident; the owner investigates the destination. Never overwrite or delete the member.
  - Registry immutability/race policy: obtain authoritative server-policy evidence before claiming
    protection against a concurrent writer; the terminal client probe cannot prove atomic exclusion.
  - NuGet ZIP-entry/signing compatibility: fixtures exercise the signing-entry rule; a real signed
    package-to-retained-asset comparison remains unverified. The owner retains that read-only proof
    before claiming live compatibility. These fixtures supply no live publication qualification.
- `tools/publish-release.ps1` writes the cross-family audit record to
  `artifacts/release/publish-summary.json`. The release workflow uploads that aggregate summary and
  its child summaries even on failure, and creates one reconciliation issue for an aggregate
  `partial-publish` state.
- If exact-source push CI reports a test failure, investigate the failing CI lane and rerun that lane after a fix. A failed or missing exact-source `ci.yml` run blocks dispatch.
- Never print, copy, or store the `NUGET_API_KEY` value in logs, docs, screenshots, or repo files.
