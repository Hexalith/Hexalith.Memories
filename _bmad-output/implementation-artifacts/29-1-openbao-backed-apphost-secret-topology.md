---
baseline_commit: 4d2e4e2f3188e57143c6290df8ff47e360ff3e27
creation_sprint_status_sha256: 201e054312a206b2933fa8fdb0dd650743670c4623347647193ea01eb8dfac04
---

# Story 29.1: OpenBao-Backed AppHost Secret Topology

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer and operator,
I want the Aspire AppHost to provision and initialize OpenBao-backed Dapr secret stores,
so that local and deployed application code use the same provider-neutral secret-access boundary.

## Acceptance Criteria

1. **Given** the Aspire AppHost starts the Memories topology,
   **When** secret infrastructure is composed,
   **Then** AppHost adds a pinned, health-checked OpenBao resource with a safe development profile that cannot silently become a production deployment,
   **And** `secretstore` and `access-telemetry-secrets` use `secretstores.hashicorp.vault`,
   **And** the stores use separate least-privilege policies and secret prefixes.

2. **Given** a service consumes an application secret,
   **When** its Dapr sidecar starts,
   **Then** it waits for OpenBao initialization and receives only its required Dapr component,
   **And** application secret payloads are not stored in local-file or Kubernetes secret-store components.

3. **Given** OpenBao requires bootstrap or one-time seeding material,
   **When** AppHost supplies it,
   **Then** local bootstrap uses Aspire secret parameters or protected temporary files,
   **And** Kubernetes Secrets are allowed only for required deployed bootstrap tokens and CA certificates or direct pod inputs Dapr cannot provide,
   **And** secrets never appear in source control, configuration, logs, diagnostics, or Aspire model output.

4. **Given** the OpenBao-backed topology is running,
   **When** integration verification executes,
   **Then** successful Dapr secret reads, cross-prefix denial, health, and restart recovery are proven without disclosing secret values,
   **And** an environment-controlled cold-start measurement proves the topology is fully operational within NFR7's 60-second target after containers are running, excluding image pull time.

## Tasks / Subtasks

- [x] Task 1 - Replace the AppHost local-file secret topology with an explicit run-only OpenBao resource (AC: 1, 3)
  - [x] In `src/Hexalith.Memories.AppHost`, add a normal OpenBao server resource using the repository's exact production image pin `quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653`; do not use `latest`, change package pins, or introduce an OpenBao SDK.
  - [x] Use a generated non-secret HCL configuration with `storage "inmem" {}` and a TCP listener on container port 8200. Do not use `bao server -dev`: dev mode emits the initial root token, which violates the no-disclosure contract.
  - [x] Give the endpoint a stable explicit name, bind its plain-HTTP host/proxy endpoint to loopback only, prohibit any external endpoint, and attach `/v1/sys/health` to that endpoint. Preserve OpenBao's uninitialized/sealed failures rather than remapping 501/503 to healthy.
  - [x] Mark the container as run-session-only with the pinned Aspire 13.4.6 stable `.WithLifetime(ContainerLifetime.Session)` API and `ExcludeFromManifest`. Retain both `ExecutionContext.IsRunMode` and Development-environment guards; publication or selection outside explicit Development/run mode must fail closed. Do not use the experimental shared `WithSessionLifetime` API unless a narrowly scoped `ASPIREPERSISTENCE001` suppression is justified.
  - [x] Keep the local topology honest: `inmem` is disposable, single-node development storage. Do not claim local persistence, HA, or production durability; the existing Raft-based production topology remains authoritative.

- [x] Task 2 - Initialize, unseal, isolate, and seed OpenBao without exposing credentials (AC: 1, 3)
  - [x] Add an AppHost-owned, cancellation-aware initialization state machine using OpenBao's HTTP API. Wait for the resource's running/start state and allocated endpoint via the pinned Aspire notification/start APIs, initialize only when required, unseal, enable/verify KV v2 at `secret`, create policies, seed values, create scoped tokens, and complete the readiness gate idempotently.
  - [x] Avoid an initialization deadlock: `/v1/sys/health` is intentionally non-healthy before initialization, so initialization must begin from resource-start state rather than `ResourceReadyEvent`, `WaitFor` health readiness, or the final health check. Consumers may wait for healthy OpenBao and initializer completion afterward.
  - [x] For OpenBao 2.6 initialization, post only `secret_shares` and `secret_threshold`; do not send the removed/ignored `stored_shares` field.
  - [x] Use distinct read-only policies for `secret/data/hexalith/memories/runtime/*` and `secret/data/hexalith/memories/access-telemetry/*`. Each token receives only its matching policy, no default policy, and no `list`, `create`, `update`, `delete`, `sudo`, or root capability. Prefer orphan service tokens so revoking/discarding bootstrap credentials does not revoke the two runtime identities.
  - [x] Seed each requested secret name under KV v2 paths `secret/hexalith/memories/runtime/<secretName>` or `secret/hexalith/memories/access-telemetry/<secretName>` (HTTP `/v1/secret/data/<prefix>/<secretName>`), using the request body `{ "data": { "<field>": "<value>" } }`. The runtime provider field equals the requested secret name, the lifecycle marker field is `access-telemetry-marker-key`, and the clock field is `signing-key-pkcs8`; preserve exactly what the existing `DaprClient.GetSecretAsync` consumers index.
  - [x] Obtain seed values through Aspire parameters declared with `secret: true` or through a narrowly scoped protected test input. Never put a root token, unseal key, scoped token, or secret payload in container arguments, plain environment literals, appsettings, source, committed YAML, exception text, logging, resource annotations, or dashboard/model output.
  - [x] Hold bootstrap responses only in memory. If any seed or token must cross a process boundary, use a per-AppHost-run temporary directory with restrictive ownership/permissions (0700 directory and 0600 files on Unix; equivalently restricted ACLs on Windows), fail closed when required protection cannot be established, and delete/stale-sweep only AppHost-owned paths.
  - [x] Give scoped tokens an explicit session-compatible lifetime or AppHost-owned renewal strategy. Dapr does not renew the mounted token: either publish and enforce a supported maximum session shorter than token expiry, or renew safely and restart affected sidecars after any credential rotation; verify expiry/renewal behavior.
  - [x] Verify that the two runtime identities are orphan tokens with `no_default_policy` before revoking the bootstrap root token through `revoke-self`; then prove a root-token request fails. Merely discarding the local root-token copy is insufficient. Never expose raw API response bodies on success or failure.
  - [x] Make re-initialization generation-aware. Because `inmem` loses all state on replacement, a controlled OpenBao or full-topology restart must deterministically reinitialize, reseed, recreate tokens, rewrite dependent component files, and reopen the sidecar gates without treating data loss as persistence.

- [x] Task 3 - Generate two OpenBao-backed Dapr components and enforce startup ordering (AC: 1, 2, 3)
  - [x] Replace only the root AppHost's generated `secretstores.local.file` documents for `secretstore` and `access-telemetry-secrets` with Dapr `secretstores.hashicorp.vault` v1 components. Keep `enginePath: secret`, `vaultKVUsePrefix: "true"`, `vaultValueType: map`, and distinct `vaultKVPrefix` values `hexalith/memories/runtime` and `hexalith/memories/access-telemetry`.
  - [x] Resolve `vaultAddr` from the allocated host/proxy endpoint because self-hosted `daprd` runs on the host; do not hard-code port 8200 or use the container DNS name. Local HTTP is allowed only inside the guarded disposable profile; do not add `skipVerify: true` or weaken production TLS.
  - [x] Prefer `vaultTokenMountPath` pointing to a protected per-run token file so generated YAML contains no token. Write a mode-0600 temporary file and atomically rename it before the sidecar starts; ensure the host Dapr process can read it while no broader user/group can, retain it until every consumer terminates, and never give either component the root token.
  - [x] Reuse the existing Redis component's generation/barrier concept, not its health-ready trigger: a refreshable completion source represents the current OpenBao initialization generation, generated component and token files are atomically installed after endpoint allocation and successful initialization, and every dependent `BeforeResourceStartedEvent` waits on that generation. Because Dapr reads and caches the mounted token when initializing the component, start or restart every consuming sidecar after each OpenBao generation. Do not regress Redis endpoint rewrite, Redis ping readiness, or sidecar restart behavior.
  - [x] Keep generated paths process-unique, YAML-safe, and cleaned in `finally`; preserve stale-directory cleanup and never delete a path not owned and positively identified by this AppHost.
  - [x] Scope `secretstore` to `memories`. Scope `access-telemetry-secrets` to `memories`, `memories-access-telemetry`, and `memories-access-telemetry-clock`. The `memories-mcp` sidecar receives neither component.
  - [x] Preserve the current consumer matrix: Memories Server receives both secret components; lifecycle receives only `access-telemetry-secrets`; clock receives only `access-telemetry-secrets`; MCP receives neither. Preserve the pinned beta toolkit's sidecar-level and project-level component-reference pattern under the existing warning suppression.
  - [x] Extend the Memories Server Dapr configuration in `deploy/dapr/config.yaml` with a deny-by-default scope for `access-telemetry-secrets` allowing only `access-telemetry-marker-key`. Preserve the runtime store allow-list. Keep the lifecycle marker-only and clock signing-key-only configuration files unchanged unless a focused test demonstrates a contract defect.

- [x] Task 4 - Remove AppHost and fixture dependence on repository-root `secrets.json` (AC: 2, 3)
  - [x] Remove `EnsureSecretsFile`, local-file `secretsFile` metadata, and the code path that creates or mutates root `secrets.json`. Preserve development access-telemetry key generation, but pass its marker/signing material through protected OpenBao seed inputs rather than a shared repository file.
  - [x] Update `AspireIngestionPipelineFixture` so provider-specific tests for the root AppHost topology supply stable protected OpenBao seed inputs. Delete its snapshot/mutate/restore behavior for a user's `secrets.json`; a test must never overwrite or delete a pre-existing user secret file.
  - [x] Update `EmbeddingProviderSecret` documentation to describe a provider-neutral protected seed input rather than a value written to a local test secret store.
  - [x] Preserve the same seed set across `RestartTopologyAsync` so a full disposable topology can be rebuilt and permitted Dapr reads recover. Update the fixture's generated Dapr configuration allow-list for custom provider keys without placing their values in the config.
  - [x] Continue to keep product projects provider-neutral: `EmbeddingSecretStore`, lifecycle bootstrap, and clock must still use Dapr `GetSecretAsync`; no OpenBao URL, client, credential, or HTTP code may enter product services.

- [x] Task 5 - Add structural, ordering, live isolation, restart, and leakage verification (AC: 1, 2, 3, 4)
  - [x] Update `AppHostSecurityConfigurationTests` or add a focused `AppHostOpenBaoConfigurationTests` model suite for the exact image+digest, normal in-memory server configuration, run-only/non-publish guard, loopback named health endpoint, two distinct secret parameters, two Vault components, exact prefixes/map semantics/scopes, and absence of `secretstores.local.file`, `secretstores.kubernetes`, `secretsFile`, dev-mode arguments, root-token literals, and repository-root `secrets.json` writes.
  - [x] Strengthen `AppHostComponentFileOrderingTests` to prove no relevant Dapr sidecar starts until the current OpenBao generation is initialized, unsealed, policy/seed/token-ready, and its component/token files are atomically and completely installed. Give the test a unique `MEMORIES_DAPR_APP_ID`, derive its exact owned temporary directory rather than selecting the globally newest file, cover Server/lifecycle/clock, prove MCP has no OpenBao reference or gate, and retain the existing Redis ordering assertion.
  - [x] Add live Dapr verification in the root AppHost topology using high-entropy canaries: permitted runtime and access-telemetry reads return the expected result through their Dapr components, while runtime-token-to-access-prefix and access-token-to-runtime-prefix requests both fail closed. Prove bulk/list secret operations fail because the policies deliberately grant no `list` capability. Never include raw expected/actual secret values in assertion messages.
  - [x] Prove `/v1/sys/health` reports initialized and unsealed after bootstrap, then exercise a full topology restart (or an equivalent explicit OpenBao replacement plus dependent-sidecar restart) and prove reinitialization and permitted Dapr reads recover.
  - [x] Scan AppHost/container/sidecar stdout and stderr, captured resource logs, exceptions/diagnostics, the Aspire resource model/manifest/environment snapshots, and generated component/config files for every canary, root token, scoped token, and unseal key. Report only boolean/status/fingerprint-safe evidence.
  - [x] Add provider-boundary negative evidence naming every affected surface: Dapr component scopes, Dapr per-key scopes, both OpenBao policies/prefixes, Server, lifecycle, clock, MCP, generated files, model/diagnostics, and restart. Prove cross-prefix and non-consumer denial before secret resolution. Tenant routing and allow-listed secret-name selection are unchanged, so do not claim that shared runtime OpenBao paths create per-tenant Vault isolation or represent this evidence as a new tenant partition.
  - [x] Measure cold start in an environment-controlled lane from containers running until the topology accepts queries; prove NFR7's 60-second target excluding image pull, and report the measured duration and environment rather than redefining the target through a larger initializer timeout.
  - [x] Preserve production OpenBao deployment tests as regression evidence; do not edit production manifests merely to make AppHost tests pass.

- [x] Task 6 - Validate the vertical slice and record the implementation phase truthfully (AC: 1, 2, 3, 4)
  - [x] Run the narrow Release builds and runner-derived discovery commands recorded under **Testing Baseline and Planned Delta**, then run focused AppHost structural/ordering tests and live fixture tests with a real OpenBao container and Dapr sidecars.
  - [x] Re-run the existing production OpenBao artifact class, AppHost security/configuration tests, and the relevant integration fixture regression set. A mock-only or YAML-only pass cannot satisfy AC 4.
  - [x] Run `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` if the focused lanes pass and no external environment blocker prevents it; record the exact command/result for any blocker.
  - [x] Run `git diff --check`, reconcile the cumulative File List against `baseline_commit`, and append a `dev-story` Change Log row with runner-derived actual deltas. Do not rewrite the create-story row or count planned tests as actual.

### Review Findings

- [x] [Review][Patch] [High] Prove the retained Dapr hot-reload hardening is inseparable from Story 29.1 [deploy/dapr/access-telemetry-lifecycle-config.yaml:8] — Decision: retain `HotReload: false` in the lifecycle and clock configurations and add focused executable evidence that it is required for the OpenBao topology.
- [x] [Review][Patch] [Medium] Record named ownership and reasons for the five excluded baseline paths [29-1-openbao-backed-apphost-secret-topology.md:318] — Decision: Jérôme Piquot owns the concurrent external work; the four gitlinks are dependency-pointer maintenance and the Web test is separate Epic 17 hardening.
- [x] [Review][Patch] [High] Make OpenBao generation leases exclusive and reject stale-generation completion [src/Hexalith.Memories.AppHost/OpenBaoGenerationGate.cs:18]
- [x] [Review][Patch] [High] Surface replacement-generation consumer restart failures instead of leaving readiness successful [src/Hexalith.Memories.AppHost/Program.cs:1184]
- [x] [Review][Patch] [High] Tear down or replace OpenBao safely after partial bootstrap failure [src/Hexalith.Memories.AppHost/OpenBaoInitializer.cs:62]
- [x] [Review][Patch] [High] Run deterministic OpenBao initializer contract tests in a blocking CI lane [tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoInitializerTests.cs:15]
- [x] [Review][Patch] [High] Exercise an in-place same-AppHost OpenBao replacement and dependent-sidecar recovery [tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs:62]
- [x] [Review][Patch] [High] Make the ordering test preserve every sidecar observation instead of overwriting failures [tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs:49]
- [x] [Review][Patch] [High] Prove the complete Server, lifecycle, clock, and MCP Dapr allow/deny matrix with exact failure evidence [tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs:39]
- [x] [Review][Patch] [High] Scan concrete Aspire manifest, resolved environment, model, diagnostic, and custom-provider secret surfaces [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:519]
- [x] [Review][Patch] [High] Fix bootstrap and unseal fingerprint matching so labeled token leaks cannot evade detection [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:90]
- [x] [Review][Patch] [Medium] Reject `.` and `..` seed names before URI normalization escapes the declared prefix [src/Hexalith.Memories.AppHost/OpenBaoSeedInputs.cs:100]
- [x] [Review][Patch] [Medium] Require the NFR7 timer to stop after a real query and preserve the initial cold-start measurement [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:130]
- [x] [Review][Patch] [Medium] Replace source-text-only run-mode safety evidence with executable guard tests [tests/Hexalith.Memories.Server.Tests/Deployment/AppHostOpenBaoConfigurationTests.cs:47]
- [x] [Review][Patch] [Medium] Verify effective owner-only directory and token-file permissions on supported platforms [src/Hexalith.Memories.AppHost/OpenBaoProtectedFileSystem.cs:16]
- [x] [Review][Patch] [Medium] Verify session-expiry shutdown behavior rather than only the lifetime constant [src/Hexalith.Memories.AppHost/OpenBaoSessionLifetimeGuard.cs:14]
- [x] [Review][Patch] [Medium] Add fail-closed tests for every invalid scoped-token lookup property [src/Hexalith.Memories.AppHost/OpenBaoInitializer.cs:246]
- [x] [Review][Patch] [Medium] Record both literal discovery commands in each governed phase ledger cell [29-1-openbao-backed-apphost-secret-topology.md:318]
- [x] [Review][Patch] [Medium] Record the authoritative create-story File List comparison command and exclusions [29-1-openbao-backed-apphost-secret-topology.md:318]

## Dev Notes

### Developer Context

The current tracked production OpenBao manifests and focused deployment tests are the source of truth for image identity, KV prefixes, policy separation, component metadata, TLS posture, and restart expectations. Story 29.1 closes the remaining local AppHost mismatch: `Program.cs` currently creates root `secrets.json`, generates two `secretstores.local.file` components, and lets tests temporarily mutate that user-owned file.

This story is one executable root-AppHost slice. The developer should build on the existing Redis readiness pattern rather than replacing the surrounding topology. Redis/FalkorDB, Dapr placement and scheduler addressing, JWT/Keycloak, Dapr API tokens, conversation, access-telemetry lifecycle, process-unique component directories, YAML escaping, cleanup, and MCP isolation are preservation requirements.

### Scope Boundary and 29.2 Handoff

**In scope:** the root `Hexalith.Memories.AppHost` resource and internal helper seams, its generated local Dapr component files, the self-hosted Memories Dapr secret allow-list, `AspireIngestionPipelineFixture`, and focused structural/live tests proving the AppHost contract.

**Out of scope and reserved for Story 29.2:** public/reusable APIs in `src/Hexalith.Memories.Aspire`, standalone templates under `deploy/dapr/components`, `deploy/dapr/secrets.json.example`, provider operations documentation, `README.md`, and reusable/template conformance tests. Do not broaden Story 29.1 to make the whole repository provider-neutral. Product retrieval code is also unchanged unless a separately demonstrated defect requires course correction.

Story 29.2 may consume only the executable resource, initialization, isolation, and readiness contract established here; it owns externally provisioned component composition and repository-wide standalone-template/documentation alignment.

### Slice Proof

**Thin vertical slice:** start the root Aspire topology; allocate and health-check one disposable OpenBao resource; initialize two isolated KV/policy/token lanes; generate the two Vault-backed Dapr components; gate their exact consumers; perform allowed and denied reads through live Dapr; replace/restart the disposable topology; and prove recovery plus absence of disclosure.

This is the minimum coherent slice because resource startup without live Dapr reads would not establish the provider-neutral boundary, while changing reusable APIs/templates would be a second independently shippable slice already assigned to Story 29.2.

### Security and Architecture Guardrails

- D31 is invariant: product services use Dapr Secrets API only. Provider-aware HTTP belongs exclusively to AppHost orchestration.
- The runtime and access-telemetry prefixes are shared application scopes, not tenant partitions. Existing opaque/allow-listed secret names remain the tenant-safety seam; this story must not claim per-tenant Vault isolation.
- OpenBao policy isolation and Dapr secret scopes are complementary. Both directions of prefix denial and every non-consumer component denial must be executable negative evidence.
- `secret: true` on an Aspire parameter is necessary metadata, not proof of redaction. Model/log/file scans must independently prove that values are absent.
- OpenBao's `/v1/sys/health` is a post-bootstrap readiness signal, not the bootstrap trigger. The initializer must observe running/start state independently so the required 501/503 pre-bootstrap health results cannot deadlock the topology.
- Normal OpenBao with `inmem` avoids dev-server root-token output, but it is still development-only and uses local HTTP. The runtime/profile guard and manifest exclusion are mandatory safety boundaries.
- `WaitForCompletion` has no deployment-time effect and cannot be the production guard. Use it only if it fits a run-mode initializer resource; retain the runtime environment check and manifest exclusion.
- Never log HTTP response bodies from init, unseal, token, policy, or KV endpoints. Exceptions must identify operation/status only.
- OpenBao 2.6 runs as non-root by default. Any bind mount must have deliberate ownership and the smallest necessary access; host-side Dapr token files must not become container-readable unless required.
- Keep one C# type per file for new types and include the repository copyright header. Do not perform an unrelated refactor of the existing top-level `Program.cs` types.

### Pinned APIs and Versions

| Asset | Required identity / usage |
| :---- | :------------------------ |
| .NET SDK | `10.0.302`; C# 14 / `net10.0` |
| Aspire AppHost SDK | `13.4.6`; verified APIs include `AddParameter(..., secret: true)`, `WithEnvironment(name, ParameterResource)`, `WithHttpEndpoint`, `WithHttpHealthCheck`, container `.WithLifetime(ContainerLifetime.Session)`, `ExcludeFromManifest`, `WaitFor`, and `WaitForCompletion` |
| Dapr Aspire toolkit | `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.1-beta.686`; preserve current dual sidecar/project reference workaround |
| Dapr .NET packages | `1.18.4`; local CLI/runtime evidence currently spans 1.18.0/1.18.1 and production uses 1.18.1 |
| OpenBao | Exact `2.6.0` image and digest above; normal server with `inmem`, never `-dev` |
| Test stack | xUnit v3 `3.2.2`, Shouldly, NSubstitute, and `Aspire.Hosting.Testing` already present |

No dependency update is part of Story 29.1. Use `dotnet-inspect` against the pinned assemblies again if an API signature is uncertain rather than guessing or copying a newer Aspire example.

The canonical project-context inventory currently lags the tracked build configuration for Aspire (`13.3.3` versus `13.4.6`) and the Dapr toolkit (an older preview versus `13.4.1-beta.686`). For this implementation, the tracked project and central package files above are authoritative; refreshing canonical context is separate maintenance, not Story 29.1 scope.

### Expected File Ownership

| Disposition | Path / area | Story 29.1 expectation |
| :---------- | :---------- | :--------------------- |
| Update | `src/Hexalith.Memories.AppHost/Program.cs` | Compose guarded OpenBao, remove local-file/root-secrets flow, gate component rewrites and consumers |
| New | `src/Hexalith.Memories.AppHost/*.cs` | Small internal resource/profile, initializer, policy/token, renderer, and protected-file lifecycle types; exact names follow existing conventions |
| Update | `deploy/dapr/config.yaml` | Add Server's access-telemetry marker-only deny-by-default scope |
| Update | `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` | Protected OpenBao seeding, restart stability, no user file mutation |
| Update | `tests/Hexalith.Memories.IntegrationTests/Fixtures/EmbeddingProviderSecret.cs` | Replace the obsolete local-test-store wording with provider-neutral protected seed terminology |
| Update | `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs` | Preserve Redis check and add OpenBao generation/barrier ordering |
| Update/New | Focused Server/Integration test files | Structural/model, fixture lifecycle, live allow/deny/health/restart/redaction evidence |
| Preserve | `src/Hexalith.Memories.Aspire/**` | Story 29.2 public composition surface |
| Preserve | `deploy/dapr/components/**`, `deploy/dapr/secrets.json.example` | Story 29.2 standalone/template surface |
| Preserve | `src/**/EmbeddingSecretStore.cs`, access lifecycle bootstrap, clock secret retrieval | Provider-neutral Dapr client behavior |
| Preserve | `deploy/kubernetes/**`, `deploy/openbao/**`, `docs/operations/openbao.md` | Existing production implementation/reference unless a real regression is found |

Treat this table as guidance, not permission to overwrite concurrent work. Re-read every path immediately before editing and reconcile actual paths in the phase ledger.

### Testing Baseline and Planned Delta

Creation used fresh Release builds with `--no-restore -m:1`; both completed with 0 warnings and 0 errors:

```text
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore -m:1
dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Release --no-restore -m:1
```

Runner-derived baseline commands use the named unit **xUnit test method**:

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo
```

| Lane | Creation baseline | Method-set SHA-256 | Planned Story 29.1 delta |
| :--- | ----------------: | :---------------- | -----------------------: |
| Server.Tests, all xUnit methods | 2,182 | `09e2ba5020d90738228d3bb47c811e6d37dde66e636eb03b9a866c9764db6989` | +3..5 structural/deployment methods |
| Server.Tests, `Deployment` namespace | 41 | derived from the same inventory | included in +3..5 above |
| IntegrationTests, all xUnit methods | 279 | `93585931fcab9cca04689a874feef72bdf7060a35c1d19f087e2e872746398e1` | +7..10 live/model/fixture methods |
| IntegrationTests, `Fixtures` namespace | 21 | derived from the same inventory | includes fixture/order work above |
| IntegrationTests, `AppHostComponentFileOrderingTests` | 1 | runner-derived focused baseline | strengthen existing method; +0 unless split intentionally |
| Server.Tests, production OpenBao regression class | 7 | runner-derived focused baseline | preserve at 7; +0 expected |

Creation assembly SHA-256 values were Server.Tests `0a663324e96b2bfb146f0eca9a41a32ccb94c6d8da30b0f311e97fc44964c1d2` and IntegrationTests `7e9e092574d6be861cd756876e52fb4b223c61c76457bc14a8ad062918bcd89b`. Planned ranges are estimates, never evidence. At implementation time, rebuild first, capture pre/post runner inventories and hashes, state external deltas separately, and record only observed comparable changes.

### Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Current tracked production OpenBao manifests and focused deployment tests | current-narrow-pattern | Exact image pin, Vault component metadata, prefixes, policies, and focused health/read/deny/restart mechanics |
| Story 27.3 trigger/provenance | historical-reference-only | Explains why Epic 29 exists; it does not define this story's shape |
| Story 27.3 whole checkpoint shape | anti-template | Do not inherit its retention gates, task breadth, or cumulative file shape |
| Story 1.4 | historical-reference-only | Original secret-access contract; the story remains done and is not reopened |
| Current `EmbeddingSecretStore`, lifecycle, and clock `DaprClient.GetSecretAsync` seams | current-narrow-pattern | Provider-neutral consumer and secret-name behavior to preserve |
| Story 7.1 | historical-reference-only | Non-secret CLI constraints only; no CLI work is in this slice |
| Story 15.6 whole story | anti-template | Its broad checkpoint scaffolding and local-file provider are superseded; do not copy its tasks, file list, or old green evidence |
| Current AppHost Redis generation/readiness barrier | current-narrow-pattern | Focused ordering pattern to adapt without copying its health-ready trigger literally |
| Story 0.0 | historical-reference-only | Original single-command/topology intent, without reopening historical work |
| Alias Story 1.1 | anti-template | Alias-only broad/outdated source; do not use it to shape this story |
| Epic 26 production-deployment foundation | historical-reference-only | Production deployment provenance only; Epic 26 did not deliver OpenBao |
| Epic 26 / Stories 26.1 and 26.5 whole-story shapes | anti-template | Broad deployment and runbook slices must not shape this AppHost story |
| Commits `4d2e4e2f` and `c7c2ca21` | historical-reference-only | Commit provenance only; reusable behavior comes from the current tracked files and tests named above |

### Git Intelligence

- Creation baseline is `4d2e4e2f3188e57143c6290df8ff47e360ff3e27` (`feat(openbao): Implement OpenBao-first secret management in Hexalith`). That commit delivered production Kubernetes/OpenBao manifests, documentation, and tests; it did not replace the root AppHost local-file topology.
- The next relevant earlier commit, `c7c2ca21` (`feat: add Access Telemetry components and Dapr integration`), explains the current AppHost access-telemetry resource/component relationships. Preserve those relationships while changing only the secret provider and startup gate.
- After creation, concurrent/external commit `445a85d3` bundled the two intended story/status paths with unrelated gitlink updates for `references/Hexalith.Builds`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants`. Those gitlinks are not Story 29.1 implementation files or credit; do not reset or absorb them into this story's File List.

### Latest Technical Information

- OpenBao dev mode is in-memory and unsealed but prints the initial root token; `-dev-no-store-token` only avoids token-helper persistence. Use normal server mode with the documented in-memory backend instead.
- Normal `inmem` starts sealed and loses all state on process restart. Initialization uses `/v1/sys/init`, `/v1/sys/unseal`, and `/v1/sys/health`; restart evidence must demonstrate deterministic rebuild, not persistence.
- OpenBao KV v2 ACL paths include the `data` segment. Dapr's prefix metadata excludes the engine name and must use `vaultValueType: map` for current consumers.
- Dapr documents OpenBao compatibility through `secretstores.hashicorp.vault`; there is no separate OpenBao component type. Component scopes and secret scopes each independently restrict access and should both be used for defense in depth.
- The official Dapr Vault component accepts token value or token mount path. Prefer the mount path and protected files so component YAML is credential-free.
- OpenBao 2.6.0 is the repository pin and includes security fixes; retain the digest and do not use the deprecated file storage backend.

### Project Structure Notes

- Work in the root repository and use `Hexalith.Memories.slnx`; do not use or create a legacy `.sln`.
- `.editorconfig` and `Directory.Build.props` enforce nullable, analyzers, warnings as errors, implicit usings, and one type per file. Root-repository C# files require the ITANEO copyright header.
- Preserve CRLF for ordinary tracked files as normalized by `.gitattributes`; YAML remains LF.
- Do not initialize/update nested submodules, update dependencies, stage, commit, push, or clean the worktree for this story unless separately requested.

### References

- [Epic 29 and Story 29.1](../planning-artifacts/epics.md#story-291-openbao-backed-apphost-secret-topology)
- [Architecture D31](../planning-artifacts/architecture.md#d31--openbao-first-dapr-secret-provider)
- [Approved sprint change proposal](../planning-artifacts/sprint-change-proposal-2026-07-19.md#41-add-epic-29)
- [PRD embedding API-key reference contract](../planning-artifacts/prd.md#embedding-provider-configuration)
- [PRD sensitive configuration boundary](../planning-artifacts/prd.md#cli-specification)
- [PRD NFR7 and NFR9 evidence](../planning-artifacts/prd.md#non-functional-requirements)
- [Canonical project context](../project-context.md)
- [OpenBao server command](https://openbao.org/docs/commands/server/)
- [OpenBao in-memory storage](https://openbao.org/docs/configuration/storage/in-memory/)
- [OpenBao system initialization API](https://openbao.org/api-docs/system/init/)
- [OpenBao unseal API](https://openbao.org/api-docs/system/unseal/)
- [OpenBao health API](https://openbao.org/api-docs/system/health/)
- [OpenBao KV v2](https://openbao.org/docs/secrets/kv/kv-v2/)
- [OpenBao token API](https://openbao.org/api-docs/auth/token/)
- [OpenBao 2.6.0 release notes](https://openbao.org/community/release-notes/2-6-0/)
- [Dapr OpenBao guidance](https://docs.dapr.io/reference/components-reference/supported-secret-stores/openbao/)
- [Dapr HashiCorp Vault component](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)
- [Dapr Vault v1.18 implementation](https://github.com/dapr/components-contrib/blob/v1.18.0/secretstores/hashicorp/vault/vault.go)
- [Dapr secret scopes](https://docs.dapr.io/developing-applications/building-blocks/secrets/secrets-scopes/)
- [Dapr component scopes](https://docs.dapr.io/operations/components/component-scopes/)
- [Aspire external parameters](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/external-parameters)
- [Aspire resource lifetimes](https://aspire.dev/app-host/resource-lifetimes/)
- [Aspire dashboard security](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/security-considerations)
- [Aspire `WithHttpHealthCheck`](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.resourcebuilderextensions.withhttphealthcheck?view=dotnet-aspire-13.0)
- [Aspire `WaitForCompletion`](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.resourcebuilderextensions.waitforcompletion?view=dotnet-aspire-13.0)
- [Aspire Dapr integration](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-19: Loaded the complete epic, PRD, architecture, UX, approved change proposal, sprint status, canonical project context, applicable reference contexts, story-scope guard, phase ledger, creation lessons, repository configuration, current AppHost/tests, production OpenBao reference, and official primary documentation.
- 2026-07-19: Verified pinned Aspire 13.4.6 API surfaces with `dotnet-inspect`; no dependency change is required.
- 2026-07-19: Captured fresh Release build, runner-discovery, method-set, and assembly-hash baselines recorded above.
- 2026-07-19: Revalidated the committed draft against the scope guard, current code/tests, Git history, and official OpenBao, Dapr, and Aspire behavior; corrected historical classifications, cold-start evidence, bootstrap ordering, token lifecycle, and provider-boundary denial requirements.
- 2026-07-19: Implemented the guarded normal-server OpenBao resource, cancellation-aware initialize/unseal/seed/policy/token/revoke state machine, refreshable generation gate, protected files, Vault component generation, exact consumer scopes, and bounded session lifetime without changing package pins.
- 2026-07-19: Live Aspire/Dapr diagnosis found and corrected endpoint-event reentrancy, non-root HCL access, `no_default_policy` token verification, dynamic Dapr app-id scopes, pre-composition secret-parameter binding, host inotify saturation, and fixture-owned Docker teardown convergence. Raw secret values and OpenBao response bodies were never emitted during diagnosis.
- 2026-07-19: Re-ran runner-derived discovery, focused structural/initializer/ordering/live topology tests, production and fixture regressions, the full Server lane, the canonical Release solution build, and whitespace/file-list checks. The extra unfiltered Integration lane was cancelled at a bounded 30-minute ceiling with a healthy topology, three declared skips, and no failure output; focused live acceptance evidence completed independently.
- 2026-07-19: Final security audit strengthened scoped-token lookup verification to require orphan service identities, exact policies, non-renewability, the 168-hour explicit maximum TTL, and a remaining lifetime beyond the 144-hour AppHost session guard; deterministic initializer tests and the pinned live OpenBao topology passed afterward.
- 2026-07-19: Code review applied all 19 accepted patches. Exclusive generation leases now reject stale completion, replacement failures stop the disposable AppHost, duplicate stop notifications preserve readiness, and same-AppHost OpenBao replacement rotates credentials and recovers dependent sidecars.
- 2026-07-19: Post-review Aspire verification reached `Running`/`Healthy` for every Story 29.1 project, sidecar, and backing container in 19.5 seconds; an `error OR critical` log query returned no entries and the AppHost stopped cleanly.

### Completion Notes List

- 2026-07-19: Created Story 29.1 as the root-AppHost OpenBao vertical slice, with Story 29.2's reusable/template surface explicitly excluded.
- 2026-07-19: Reconciled current local-file behavior against D31, the existing production OpenBao implementation, pinned Aspire/Dapr/OpenBao APIs, security isolation, restart semantics, and test fixture ownership.
- 2026-07-19: Ultimate context engine analysis completed - comprehensive developer guide created.
- 2026-07-19: Replaced both root AppHost local-file stores with distinct Vault KV v2 lanes backed by the exact OpenBao 2.6.0 image digest, loopback-only endpoint, normal in-memory server, strict health check, least-privilege orphan identities, root revocation proof, protected token mounts, and generation-aware consumer gates.
- 2026-07-19: Removed all fixture/AppHost reads, writes, snapshots, and restoration of repository-root `secrets.json`; stable protected Aspire seed parameters now survive disposable topology reconstruction while product services remain provider-neutral Dapr Secrets consumers.
- 2026-07-19: Added five Server structural methods and seven Integration methods. Live evidence proves permitted reads, both cross-prefix denials, list/bulk denial, initialized/unsealed health, full-topology recovery, no sensitive disclosure, and a 3.245-second NFR7 cold start; an earlier identical run measured 3.743 seconds.
- 2026-07-19: Verification passed: solution Release build 0 warnings/errors; focused OpenBao Server 5/5; initializer plus real ordering 4/4; final live topology 4/4; production/security regression 17/17; fixture/public-surface regression 9/9; full Server 2,741 passed, 0 failed, 1 pre-existing skip. The non-required unfiltered Integration run remained incomplete at the 30-minute diagnostic ceiling, so its whole-lane result is not claimed.
- 2026-07-19: Post-audit verification passed again: initializer contract 3/3, live OpenBao topology 4/4, and solution Release build with 0 warnings/errors. The final topology restart recovered in 179 seconds; the independently measured NFR7 cold-start and disclosure checks remained green.
- 2026-07-19: Post-review verification passed: story-owned Release builds with 0 warnings/errors; deterministic OpenBao contracts 23/23; AppHost structural tests 5/5; executable ordering/hot-reload proof 1/1; combined live OpenBao topology 6/6, including full reconstruction in 2m27s and same-AppHost rotation in 9s. The umbrella `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` is externally blocked by 73 Razor errors in Jérôme Piquot's concurrently maintained `references/Hexalith.FrontComposer` checkout; no Story 29.1 project failed.

### File List

- `_bmad-output/implementation-artifacts/29-1-openbao-backed-apphost-secret-topology.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `deploy/dapr/access-telemetry-clock-config.yaml`
- `deploy/dapr/access-telemetry-lifecycle-config.yaml`
- `deploy/dapr/config.yaml`
- `src/Hexalith.Memories.AppHost/AccessTelemetryDevelopmentSecrets.cs`
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`
- `src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoGenerationGate.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoGenerationLease.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoGenerationLogger.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoInitializationResult.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoInitializer.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoProtectedFileSystem.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoSeedInputs.cs`
- `src/Hexalith.Memories.AppHost/OpenBaoSessionLifetimeGuard.cs`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/EmbeddingProviderSecret.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoCapturedRequest.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoGenerationGateTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoInitializerTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoRecordingHandler.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoSafetyContractTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostOpenBaoConfigurationTests.cs`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-19 | create-story | Created context-ready Story 29.1; moved Epic 29 from `backlog` to `in-progress` and Story 29.1 from `backlog` to `ready-for-dev`; left Story 29.2 at `backlog`. No implementation or dependency change occurred. | Actual phase delta +0 and cumulative +0. Fresh Release builds passed with 0 warnings/errors. Runner-derived xUnit baselines: Server.Tests 2,182 methods (Deployment 41), IntegrationTests 279 methods (Fixtures 21), ordering class 1, and production OpenBao regression class 7. Planned deltas: Server.Tests +3..5 and IntegrationTests +7..10 methods; planned values are not actual evidence. Exact discoveries: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo`. | Matched 2/2 intended creation paths against baseline `4d2e4e2f3188e57143c6290df8ff47e360ff3e27` with authoritative command `git diff --name-only 4d2e4e2f3188e57143c6290df8ff47e360ff3e27 82c92c52b95ecfe50df5b3dc4094f352975fec0d -- _bmad-output/implementation-artifacts/29-1-openbao-backed-apphost-secret-topology.md _bmad-output/implementation-artifacts/sprint-status.yaml`; sprint status changed from pre-create SHA-256 `201e054312a206b2933fa8fdb0dd650743670c4623347647193ea01eb8dfac04` to post-create SHA-256 `c7464bf870b8eb90cc4f0199d9be772a19279fd065e268c8854811967bbff79e`. Jérôme Piquot owns the named concurrent exclusions: dependency-pointer maintenance in `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants`, plus separate Epic 17 hardening in `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs`. |
| 2026-07-19 | dev-story | Implemented the executable root-AppHost OpenBao slice: guarded normal server, one-time bootstrap and root revocation, two isolated Vault KV v2 identities/components, protected seed/token boundaries, generation ordering/recovery, exact consumer and key scopes, fixture migration away from `secrets.json`, disclosure checks, and live NFR7 evidence. No package version changed. | Runner-derived xUnit **methods**: Server.Tests 2,182 -> 2,187, phase/cumulative **+5**; Deployment 41 -> 46, with new `AppHostOpenBaoConfigurationTests` 0 -> 5 and production `ProductionDeploymentArtifactsTests` 7 -> 7 (**+0**). IntegrationTests 279 -> 286, phase/cumulative **+7**; Fixtures 21 -> 28, comprising `OpenBaoInitializerTests` 0 -> 3 and `OpenBaoTopologyIntegrationTests` 0 -> 4; `AppHostComponentFileOrderingTests` remains 1 -> 1 (**+0**, behavior strengthened). External same-lane delta: none. Exact discoveries: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo`. Nonblank method-set SHA-256: Server `9fe81bd35f7ef7bc5217df9692e67f78b4988b67e3b33c44dc0a5c716866c299`; Integration `1bca7f9c82dbcc579eccb68069d31cd395ac8519159a32135e79f93b5df78151`. Built assembly SHA-256: Server `ac191da1902db1c4ae159a5f0b0c3fa6ccc07172b18c9917fa7c49619f6d08cf`; Integration `7b2d09ad7795bcf15459081996fe7242c4c9450cda4b94862833c0e7ac7bb057`. | Matched **23/23** cumulative story paths against declared baseline `4d2e4e2f3188e57143c6290df8ff47e360ff3e27`, using `git diff --name-status 4d2e4e2f3188e57143c6290df8ff47e360ff3e27` plus `git status --porcelain` for added working-tree files. Jérôme Piquot owns the five named exclusions: dependency-pointer maintenance in `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants`, plus separate Epic 17 hardening in `tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs`; none was included in the File List. |
| 2026-07-19 | code-review | Applied every accepted review patch: hardened exclusive generation ownership and failure teardown, made same-AppHost recovery executable, completed the exact Dapr authorization matrix, expanded concrete disclosure/NFR and platform safety evidence, preserved all ordering observations, and made the initializer contracts CI-blocking. No dependency or production deployment change occurred. | Runner-derived xUnit **methods**: Server.Tests remains 2,187, review delta **+0**, cumulative **+5**; Deployment remains 46, `AppHostOpenBaoConfigurationTests` remains 5, and `ProductionDeploymentArtifactsTests` remains 7. IntegrationTests 286 -> 297, review delta **+11**, cumulative **+18**; Fixtures 28 -> 39, comprising `OpenBaoInitializerTests` 3 -> 5, `OpenBaoTopologyIntegrationTests` 4 -> 6, new `OpenBaoGenerationGateTests` 0 -> 3, new `OpenBaoSafetyContractTests` 0 -> 4, and `AppHostComponentFileOrderingTests` remains 1. Exact discoveries: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` and `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo`. Nonblank method-set SHA-256: Server `9fe81bd35f7ef7bc5217df9692e67f78b4988b67e3b33c44dc0a5c716866c299`; Integration `654f280ac3ae54856f9d3e446ec5fb8b0e36c0cbab25c07ec2b5ccec5598c945`. Built assembly SHA-256: Server `7a3fff958e513b63014660c2f88ed0101d40647e91caf79b5ded12a49ec1f282`; Integration `e596cc1d600af50fcfcd27c52fde5ee017c40170739d63172f23921041ef9281`. Executed evidence: story-owned Release builds 0 warnings/errors; deterministic contracts 23/23; AppHost structural 5/5; executable ordering/hot-reload 1/1; combined live topology 6/6; Aspire resources all `Running`/`Healthy` in 19.5s with no error/critical logs. The umbrella solution command was blocked only by 73 Razor errors in the excluded FrontComposer checkout. | Matched **27/27** cumulative story paths. Authoritative comparison command `comm -3 <({ git diff --name-only 4d2e4e2f3188e57143c6290df8ff47e360ff3e27; git ls-files --others --exclude-standard; } \| rg -v '^(references/Hexalith\.(Builds\|EventStore\|FrontComposer\|Tenants)\|tests/Hexalith\.Memories\.Web\.Tests/Components/Validation/Epic17ConformanceHardeningTests\.cs)$' \| sort -u) <(sed -n '/^### File List/,/^## Change Log/p' _bmad-output/implementation-artifacts/29-1-openbao-backed-apphost-secret-topology.md \| tr -d '\r' \| sed -n 's/^- `\(.*\)`$/\1/p' \| sort -u)` returned empty. Jérôme Piquot owns the excluded four dependency-pointer paths and the separate Epic 17 Web hardening path; they were neither edited nor credited by this review. |
