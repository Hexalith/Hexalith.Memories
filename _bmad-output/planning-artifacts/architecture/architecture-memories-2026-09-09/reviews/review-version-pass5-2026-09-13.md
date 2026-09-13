---
lens: technology-version-reality
kind: reviewer-gate-pass5
target: '_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md'
reviewed: '2026-09-13'
supersedes: 'reviews/review-version-pass4-2026-09-12.md'
mode: read-only
---

# Reviewer Gate Pass 5 — Technology / Version Reality

**Verdict: REVISE.**

The pass-4 high-severity corrections were made, but AD-19's mandatory re-derivation failed again in the
very commit that carried those corrections. The committed Builds and EventStore gitlinks both moved while
the spine kept the prior hashes. The EventStore source lane is now the released `v3.104.0` commit while the
package catalog remains on `3.103.0`; the spine instead reports the previous source revision as 43 commits
past `v3.103.0`. This is a current repository contradiction in an adopted release-evidence rule, so the gate
cannot pass.

No new upstream release invalidates the spine's security or obsolescence conclusions. The .NET, OpenBao,
PostgreSQL, Redis Stack, FalkorDB, Gemini embedding, MCP, and prerelease-package conclusions still check out
against official primary sources as of this review. Two low-severity pass-4 wording defects also remain.

## Evidence and method

The full current spine and memlog were read. Repository facts were re-derived from `HEAD` (`e532936d`), the
cached gitlinks, the checked-out submodule commits, central package catalog, build pins, manifests, CI, and
the cited source files. The pass-4 version review was used only to identify claims requiring closure retest.
No spine, memlog, source, dependency, or submodule was changed.

Commands used for the decisive repository checks included:

```text
git submodule status --cached
git ls-tree HEAD references/Hexalith.Builds references/Hexalith.EventStore
git -C references/Hexalith.Builds describe --tags bf8adb73...
git -C references/Hexalith.EventStore describe --tags 4502913c...
git -C references/Hexalith.EventStore rev-list --count v3.103.0..4502913c...
git diff --submodule=short e532936d^ e532936d -- references/Hexalith.Builds references/Hexalith.EventStore
```

The official sources used for live reality checks were the [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0),
[OpenBao releases](https://github.com/openbao/openbao/releases), [OpenBao Helm releases](https://github.com/openbao/openbao-helm/releases),
[PostgreSQL security list](https://www.postgresql.org/support/security/), [Redis Stack repository](https://github.com/redis-stack/redis-stack),
[Redis Stack releases](https://github.com/redis-stack/redis-stack/releases), [FalkorDB releases](https://github.com/FalkorDB/FalkorDB/releases),
[Gemini embedding model documentation](https://ai.google.dev/gemini-api/docs/models/gemini-embedding-001), and the official NuGet registration
indexes (for example [MCP](https://api.nuget.org/v3-flatcontainer/modelcontextprotocol/index.json),
[Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), and
[CommunityToolkit Aspire Dapr](https://api.nuget.org/v3-flatcontainer/communitytoolkit.aspire.hosting.dapr/index.json)).

## Repository reality

| Claim area | Spine | Current committed repository | Verdict |
| --- | --- | --- | --- |
| Builds fact source | `cf52f74c...` / `v4.27.3-3-gcf52f74` | `bf8adb73...` / `v4.27.3-9-gbf8adb7` | **STALE** |
| EventStore source lane | `fe05a796...` / `v3.103.0-43-gfe05a796` | `4502913c...` / exact tag `v3.104.0`; 47 commits after `v3.103.0` | **STALE** |
| EventStore package lane | `3.103.0` | `HexalithEventStoreVersion=3.103.0` in the committed Builds catalog | **CONFIRMED** |
| Lane correspondence | Not met by 43 source commits | Not met: source is the `3.104.0` release while package lane remains `3.103.0` | **STALE DETAIL; CONCLUSION HOLDS** |
| Catalog package pins | Dapr `1.18.7`; CommunityToolkit `13.5.1-beta.751`; Kreuzberg `4.10.3`; StackExchange.Redis `3.2.0`; other Stack rows as listed | Same values at `bf8adb73`; the six intervening Builds commits change workflow action pins, not the catalog | **CONFIRMED VALUES, WRONG SOURCE HASH** |
| Root SDK / target / language | `10.0.400` / `net10.0` / C# 14 | `global.json`, `Directory.Build.props` match | **CONFIRMED** |
| Aspire AppHost | `13.5.3` | AppHost project SDK is `13.5.3` | **CONFIRMED** |
| Dapr runtime | `1.18.2` | CI and nightly variables are `1.18.2` | **CONFIRMED** |
| Redis / FalkorDB images | `7.4.0-v8` / `4.12.0` | Kubernetes manifests carry those tags and the stated digests | **CONFIRMED** |
| PostgreSQL | `18.4-trixie` digest-pinned | Deployment manifest matches | **CONFIRMED** |
| OpenBao | image `2.6.0`, chart `0.28.5` | `deploy/openbao/values.yaml` matches, including the image and chart digests | **CONFIRMED** |
| Embedding configuration | Google `gemini-embedding-001`, dimension 768 | `EmbeddingProviderDefaults.cs` and `TenantProvisioningInput` match | **CONFIRMED** |
| Container-default divergence | AppHost/Aspire floats Redis/FalkorDB; harnesses use a different digest set | Current code and fixtures still do exactly this | **CONFIRMED AND LEDGERED** |

`e532936d` moved Builds from `cf52f74c` to `bf8adb73` and EventStore from `fe05a796` to `4502913c` in the
same commit that introduced the present spine. The memlog's latest version correction also stops at the old
hashes, so there is no newer decision-log entry from which the rendered values could legitimately be distilled.

The EventStore package registry now contains `3.104.0`, and the checked-out source commit is exactly that tag.
That does not change the repository's package pin — it remains `3.103.0` — but it makes the current lane
divergence a released-version difference rather than merely an ahead-of-tag commit count.

## Live upstream and technology-fit retest

| Assertion | Result |
| --- | --- |
| .NET `10.0.12` / SDK `10.0.401` is the 2026-09-08 security patch above the root `10.0.400` pin | **Confirmed** by Microsoft's current .NET 10 page. The root SDK/base-image remediation remains justified; the catalog's ASP.NET Core `10.0.12` pins remain correctly described as already remediated. |
| OpenBao `2.6.2` is the current security-fixed release and chart `0.29.4` is current | **Confirmed** from the two upstream release feeds. The pin/qualification blocker remains valid. |
| PostgreSQL `18.6` is the current supported 18 release and fixes the stated 2026 security set | **Confirmed** from PostgreSQL's security list; the spine's corrected counts (14 at 8.8, 17 at least 8.0) are internally consistent now. |
| Redis Stack `7.4.0-v8` is terminal and Redis Stack maintenance ended in December 2025 | **Confirmed** from Redis Stack's repository and release feed; Docker Hub still reports the tag/digest and a 2025-11-03 last update. Redis Software 7.4's separate 2026-11-30 lifecycle is not applicable. |
| FalkorDB `4.12.0` is four even-minor release lines behind current `v4.20.4` | **Confirmed** from upstream releases. It remains an upgrade/qualification concern, not evidence that the pinned server cannot perform its assigned graph role. |
| `gemini-embedding-001` supports dimension 768 for retrieval embeddings | **Confirmed**; Google's model page lists 128-3072 output dimensions and recommends 768, 1536, or 3072. |
| MCP `2.2.0`, Aspire `13.5.3`, NRedisStack `1.7.4`, StackExchange.Redis `3.2.0`, NFalkorDB `1.2.0`, OpenTelemetry `1.18.0`, and Fluent UI `5.0.0-rc.5-26219.1` are published and compatible with the roles the spine assigns | **Confirmed** from official NuGet indexes. The CommunityToolkit `13.5.1-beta.751` pin exists; `.752` remains the newer prerelease and no stable 13.5.x exists. |

No named technology has been withdrawn, renamed, or made incompatible with the architecture role assigned to
it. Redis Stack remains the deliberate exception already captured as an ended-maintenance line whose functions
moved into Redis 8.

## Pass-4 closure retest

| Pass-4 finding | Pass-5 result |
| --- | --- |
| VER4-01 Builds gitlink and dependent facts | **REGRESSED.** The corrected pin values remain right, but both fact-source gitlinks moved again and were not re-derived. |
| VER4-02 PostgreSQL CVSS count mismatch | **CLOSED.** Both Stack prose and ledger now say 14 at 8.8 and 17 at at least 8.0. |
| VER4-03 digest rule contradicted table / missing harness ledger row | **CLOSED.** AD-19 now explicitly says the digest table obligation is unmet and the ledger contains the shared-digest-set row. |
| VER4-04 target fourth partition stated as current | **CLOSED.** Structural Seed says three exist today and the fourth does not yet exist. |
| VER4-05 drifting Production-precondition count | **CLOSED.** The caveat no longer maintains a count and delegates classification to the ledger. |
| VER4-06 smaller overstatements | **PARTIAL.** The material .NET conclusion is correct. The digest-guard scope remains overstated; see VER5-02. |
| VER4-07 auto-provision mechanism | **OPEN.** The ledger still says the startup flag creates tenants outside AD-6, although it schedules AD-6's provisioning workflow; see VER5-03. |

## Findings

### VER5-01 — Both committed gitlinks are stale in the spine, and the source/package lane description no longer matches `HEAD`. **HIGH**

AD-19 requires every fact-source gitlink to be re-derived at each spine revision. Current `HEAD` records
Builds `bf8adb73cec1ddb04b7d74b113cbfbb726cccf8c` (`v4.27.3-9-gbf8adb7`) and EventStore
`4502913cafbb6151a17544923b5b1ba76ea5e5ec` (exact tag `v3.104.0`). The spine records `cf52f74c...` and
`fe05a796...` instead. The EventStore package lane remains `3.103.0`, so lane correspondence is still unmet,
but the precise evidence is now “source at released `3.104.0`, package at `3.103.0`” (or 47 commits after the
`v3.103.0` tag), not “43 commits past `3.103.0`.”

**Required correction:** re-derive and record both current hashes/descriptions, update the source-lane mismatch,
and append the new reality check to the memlog before re-distilling. The Stack values need no package-value
changes because the Builds catalog did not change between the two hashes.

### VER5-02 — The digest guard is broader than “Kubernetes manifests only.” **LOW**

The base-image ledger row says the existing guard “currently covers Kubernetes manifests only.” Repository
tests also bind the OpenBao digest in `deploy/openbao/values.yaml` and the CI `OPENBAO_IMAGE` literal
(`ProductionDeploymentArtifactsTests.cs:329`, `CiTestInventoryTests.cs:842`). The missing `ContainerBaseImage`
coverage is real; the characterization of the existing guard is not.

**Required correction:** say that the guard covers Kubernetes/OpenBao deployment literals but not the owned
image `ContainerBaseImage` (nor the one qualified cross-consumer digest set AD-19 requires).

### VER5-03 — `AutoProvisionRoutedTenants` is an authorization defect, not a bypass of AD-6's workflow. **LOW**

The routing ledger row still says the flag “can create tenants outside AD-6” and asks that it not create a
resource “outside a lifecycle workflow.” `RoutedTenantProvisioningStartupService.cs:98-116` constructs a
`TenantProvisioningInput` and schedules `TenantProvisioningWorkflow`; it therefore uses the lifecycle mechanism.
The actual contradiction is that a startup hot path initiates the workflow without the operator principal AD-5
and AD-6 require. `EventStoreRoutingConfigValidator.cs:113` also defers unknown tenants on any Development host,
not only when the auto-provision flag is enabled.

**Required correction:** describe and converge the missing operator authorization on startup-initiated
provisioning; do not describe it as bypassing the lifecycle workflow.

## Gate conclusion

One high repository-reality mismatch blocks convergence. The remaining upstream/security conclusions and all
Stack package values are supported; two low wording defects should be corrected in the same distillation.
