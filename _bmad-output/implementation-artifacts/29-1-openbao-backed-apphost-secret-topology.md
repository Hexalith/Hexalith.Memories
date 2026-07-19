---
baseline_commit: 4d2e4e2f3188e57143c6290df8ff47e360ff3e27
creation_sprint_status_sha256: 201e054312a206b2933fa8fdb0dd650743670c4623347647193ea01eb8dfac04
---

# Story 29.1: OpenBao-Backed AppHost Secret Topology

Status: ready-for-dev

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
   **Then** successful Dapr secret reads, cross-prefix denial, health, and restart recovery are proven without disclosing secret values.

## Tasks / Subtasks

- [ ] Task 1 - Replace the AppHost local-file secret topology with an explicit run-only OpenBao resource (AC: 1, 3)
  - [ ] In `src/Hexalith.Memories.AppHost`, add a normal OpenBao server resource using the repository's exact production image pin `quay.io/openbao/openbao:2.6.0@sha256:900bb64d0671cd1d82b693c56206f7263b582445f3a3bb6ba6e5213f524a6653`; do not use `latest`, change package pins, or introduce an OpenBao SDK.
  - [ ] Use a generated non-secret HCL configuration with `storage "inmem" {}` and a TCP listener on container port 8200. Do not use `bao server -dev`: dev mode emits the initial root token, which violates the no-disclosure contract.
  - [ ] Give the endpoint a stable explicit name, publish it through an Aspire-allocated host/proxy port, and attach `/v1/sys/health` to that endpoint. Preserve OpenBao's uninitialized/sealed failures rather than remapping 501/503 to healthy.
  - [ ] Mark the in-memory topology as run-session-only with the pinned Aspire 13.4.6 `WithSessionLifetime` and `ExcludeFromManifest` APIs. Fail closed unless the AppHost is executing the explicit Development/run profile; publishing or selecting this profile outside Development must not silently produce deployable infrastructure.
  - [ ] Keep the local topology honest: `inmem` is disposable, single-node development storage. Do not claim local persistence, HA, or production durability; the existing Raft-based production topology remains authoritative.

- [ ] Task 2 - Initialize, unseal, isolate, and seed OpenBao without exposing credentials (AC: 1, 3)
  - [ ] Add an AppHost-owned, cancellation-aware initialization state machine using OpenBao's HTTP API. Wait for the allocated endpoint to accept requests, initialize only when required, unseal, enable/verify KV v2 at `secret`, create policies, seed values, create scoped tokens, and complete the readiness gate idempotently.
  - [ ] Avoid an initialization deadlock: `/v1/sys/health` is intentionally non-healthy before initialization, so initialization must begin from endpoint/resource-start state rather than waiting for the final health check to pass.
  - [ ] Use distinct read-only policies for `secret/data/hexalith/memories/runtime/*` and `secret/data/hexalith/memories/access-telemetry/*`. Each token receives only its matching policy, no default policy, and no `list`, `create`, `update`, `delete`, `sudo`, or root capability. Prefer orphan service tokens so revoking/discarding bootstrap credentials does not revoke the two runtime identities.
  - [ ] Seed KV v2 maps at the architecture prefixes `secret/hexalith/memories/runtime` and `secret/hexalith/memories/access-telemetry`. Preserve the existing Dapr consumer field contract: each secret value is a map whose field name matches what `DaprClient.GetSecretAsync` consumers index.
  - [ ] Obtain seed values through Aspire parameters declared with `secret: true` or through a narrowly scoped protected test input. Never put a root token, unseal key, scoped token, or secret payload in container arguments, plain environment literals, appsettings, source, committed YAML, exception text, logging, resource annotations, or dashboard/model output.
  - [ ] Hold bootstrap responses only in memory. If any seed or token must cross a process boundary, use a per-AppHost-run temporary directory with restrictive ownership/permissions (0700 directory and 0600 files on Unix; equivalently restricted ACLs on Windows), fail closed when required protection cannot be established, and delete/stale-sweep only AppHost-owned paths.
  - [ ] Give scoped tokens an explicit session-compatible lifetime or renewal strategy. Revoke or discard the root credential after policies, data, and scoped identities are ready; never expose raw API response bodies on success or failure.
  - [ ] Make re-initialization generation-aware. Because `inmem` loses all state on replacement, a controlled OpenBao or full-topology restart must deterministically reinitialize, reseed, recreate tokens, rewrite dependent component files, and reopen the sidecar gates without treating data loss as persistence.

- [ ] Task 3 - Generate two OpenBao-backed Dapr components and enforce startup ordering (AC: 1, 2, 3)
  - [ ] Replace only the root AppHost's generated `secretstores.local.file` documents for `secretstore` and `access-telemetry-secrets` with Dapr `secretstores.hashicorp.vault` v1 components. Keep `enginePath: secret`, `vaultKVUsePrefix: "true"`, `vaultValueType: map`, and distinct `vaultKVPrefix` values `hexalith/memories/runtime` and `hexalith/memories/access-telemetry`.
  - [ ] Resolve `vaultAddr` from the allocated host/proxy endpoint because self-hosted `daprd` runs on the host; do not hard-code port 8200 or use the container DNS name. Local HTTP is allowed only inside the guarded disposable profile; do not add `skipVerify: true` or weaken production TLS.
  - [ ] Prefer `vaultTokenMountPath` pointing to a protected per-run token file so generated YAML contains no token. Ensure the host Dapr process can read the file while no broader user/group can; never give either component the root token.
  - [ ] Reuse the existing Redis component pattern: a refreshable completion source represents the current OpenBao initialization generation, generated component files are rewritten after endpoint allocation and successful initialization, and every dependent `BeforeResourceStartedEvent` waits on that generation. Do not regress Redis endpoint rewrite, Redis ping readiness, or sidecar restart behavior.
  - [ ] Keep generated paths process-unique, YAML-safe, and cleaned in `finally`; preserve stale-directory cleanup and never delete a path not owned and positively identified by this AppHost.
  - [ ] Scope `secretstore` to `memories`. Scope `access-telemetry-secrets` to `memories`, `memories-access-telemetry`, and `memories-access-telemetry-clock`. The `memories-mcp` sidecar receives neither component.
  - [ ] Preserve the current consumer matrix: Memories Server receives both secret components; lifecycle receives only `access-telemetry-secrets`; clock receives only `access-telemetry-secrets`; MCP receives neither. Preserve the pinned beta toolkit's sidecar-level and project-level component-reference pattern under the existing warning suppression.
  - [ ] Extend the Memories Server Dapr configuration in `deploy/dapr/config.yaml` with a deny-by-default scope for `access-telemetry-secrets` allowing only `access-telemetry-marker-key`. Preserve the runtime store allow-list. Keep the lifecycle marker-only and clock signing-key-only configuration files unchanged unless a focused test demonstrates a contract defect.

- [ ] Task 4 - Remove AppHost and fixture dependence on repository-root `secrets.json` (AC: 2, 3)
  - [ ] Remove `EnsureSecretsFile`, local-file `secretsFile` metadata, and the code path that creates or mutates root `secrets.json`. Preserve development access-telemetry key generation, but pass its marker/signing material through protected OpenBao seed inputs rather than a shared repository file.
  - [ ] Update `AspireIngestionPipelineFixture` so provider-specific tests supply stable protected OpenBao seed inputs. Delete its snapshot/mutate/restore behavior for a user's `secrets.json`; a test must never overwrite or delete a pre-existing user secret file.
  - [ ] Preserve the same seed set across `RestartTopologyAsync` so a full disposable topology can be rebuilt and permitted Dapr reads recover. Update the fixture's generated Dapr configuration allow-list for custom provider keys without placing their values in the config.
  - [ ] Continue to keep product projects provider-neutral: `EmbeddingSecretStore`, lifecycle bootstrap, and clock must still use Dapr `GetSecretAsync`; no OpenBao URL, client, credential, or HTTP code may enter product services.

- [ ] Task 5 - Add structural, ordering, live isolation, restart, and leakage verification (AC: 1, 2, 3, 4)
  - [ ] Add focused AppHost structural/model tests for the exact image+digest, normal in-memory server configuration, run-only/non-publish guard, named health endpoint, two Vault components, exact prefixes/map semantics/scopes, and absence of `secretstores.local.file`, `secretstores.kubernetes`, `secretsFile`, dev-mode arguments, root-token literals, and repository-root `secrets.json` writes.
  - [ ] Strengthen `AppHostComponentFileOrderingTests` to prove no relevant Dapr sidecar starts until the current OpenBao generation is initialized, unsealed, policy/seed/token-ready, and its component/token files are completely rewritten. Retain the existing Redis ordering assertion.
  - [ ] Add live Dapr verification using high-entropy canaries: permitted runtime and access-telemetry reads return the expected result through their Dapr components, while runtime-token-to-access-prefix and access-token-to-runtime-prefix requests both fail closed. Never include raw expected/actual secret values in assertion messages.
  - [ ] Prove `/v1/sys/health` reports initialized and unsealed after bootstrap, then exercise a full topology restart (or an equivalent explicit OpenBao replacement plus dependent-sidecar restart) and prove reinitialization and permitted Dapr reads recover.
  - [ ] Scan AppHost/container/sidecar stdout and stderr, captured resource logs, exceptions/diagnostics, the Aspire resource model/manifest/environment snapshots, and generated component/config files for every canary, root token, scoped token, and unseal key. Report only boolean/status/fingerprint-safe evidence.
  - [ ] Add tenant/isolation negative evidence naming every affected surface: Dapr component scopes, Dapr per-key scopes, both OpenBao policies/prefixes, Server, lifecycle, clock, MCP, generated files, model/diagnostics, and restart. Prove cross-prefix and non-consumer denial before secret resolution; do not claim that shared runtime OpenBao paths create per-tenant Vault isolation.
  - [ ] Preserve production OpenBao deployment tests as regression evidence; do not edit production manifests merely to make AppHost tests pass.

- [ ] Task 6 - Validate the vertical slice and record the implementation phase truthfully (AC: 1, 2, 3, 4)
  - [ ] Run the narrow Release builds and runner-derived discovery commands recorded under **Testing Baseline and Planned Delta**, then run focused AppHost structural/ordering tests and live fixture tests with a real OpenBao container and Dapr sidecars.
  - [ ] Re-run the existing production OpenBao artifact class, AppHost security/configuration tests, and the relevant integration fixture regression set. A mock-only or YAML-only pass cannot satisfy AC 4.
  - [ ] Run `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` if the focused lanes pass and no external environment blocker prevents it; record the exact command/result for any blocker.
  - [ ] Run `git diff --check`, reconcile the cumulative File List against `baseline_commit`, and append a `dev-story` Change Log row with runner-derived actual deltas. Do not rewrite the create-story row or count planned tests as actual.

## Dev Notes

### Developer Context

The production OpenBao boundary already exists and is the source of truth for image identity, KV prefixes, policy separation, component metadata, TLS posture, and restart expectations. Story 29.1 closes the remaining local AppHost mismatch: `Program.cs` currently creates root `secrets.json`, generates two `secretstores.local.file` components, and lets tests temporarily mutate that user-owned file.

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
- Normal OpenBao with `inmem` avoids dev-server root-token output, but it is still development-only and uses local HTTP. The runtime/profile guard and manifest exclusion are mandatory safety boundaries.
- `WaitForCompletion` has no deployment-time effect and cannot be the production guard. Use it only if it fits a run-mode initializer resource; retain the runtime environment check and manifest exclusion.
- Never log HTTP response bodies from init, unseal, token, policy, or KV endpoints. Exceptions must identify operation/status only.
- OpenBao 2.6 runs as non-root by default. Any bind mount must have deliberate ownership and the smallest necessary access; host-side Dapr token files must not become container-readable unless required.
- Keep one C# type per file for new types and include the repository copyright header. Do not perform an unrelated refactor of the existing top-level `Program.cs` types.

### Pinned APIs and Versions

| Asset | Required identity / usage |
| :---- | :------------------------ |
| .NET SDK | `10.0.302`; C# 14 / `net10.0` |
| Aspire AppHost SDK | `13.4.6`; verified APIs include `AddParameter(..., secret: true)`, `WithEnvironment(name, ParameterResource)`, `WithHttpEndpoint`, `WithHttpHealthCheck`, `WithSessionLifetime`, `ExcludeFromManifest`, `WaitFor`, and `WaitForCompletion` |
| Dapr Aspire toolkit | `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.1-beta.686`; preserve current dual sidecar/project reference workaround |
| Dapr .NET packages | `1.18.4`; local CLI/runtime evidence currently spans 1.18.0/1.18.1 and production uses 1.18.1 |
| OpenBao | Exact `2.6.0` image and digest above; normal server with `inmem`, never `-dev` |
| Test stack | xUnit v3 `3.2.2`, Shouldly, NSubstitute, `Microsoft.Extensions.Hosting.Testing` / Aspire testing already present |

No dependency update is part of Story 29.1. Use `dotnet-inspect` against the pinned assemblies again if an API signature is uncertain rather than guessing or copying a newer Aspire example.

### Expected File Ownership

| Disposition | Path / area | Story 29.1 expectation |
| :---------- | :---------- | :--------------------- |
| Update | `src/Hexalith.Memories.AppHost/Program.cs` | Compose guarded OpenBao, remove local-file/root-secrets flow, gate component rewrites and consumers |
| New | `src/Hexalith.Memories.AppHost/*.cs` | Small internal resource/profile, initializer, policy/token, renderer, and protected-file lifecycle types; exact names follow existing conventions |
| Update | `deploy/dapr/config.yaml` | Add Server's access-telemetry marker-only deny-by-default scope |
| Update | `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` | Protected OpenBao seeding, restart stability, no user file mutation |
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
| Story 27.3 | current narrow pattern | Exact production OpenBao pin, component/policy separation, allow/deny/health/restart evidence; do not inherit its retention gates, task breadth, or file shape |
| Story 1.4 | historical-reference-only | Existing Dapr `GetSecretAsync` seam and secret-name contract; story remains done and is not reopened |
| Story 7.1 | historical-reference-only | Non-secret CLI constraints only; no CLI work is in this slice |
| Story 15.6 | anti-template for this slice | Its broad checkpoint scaffolding and local-file provider are superseded; do not copy its tasks, file list, or old green evidence |
| Story 0.0 / alias 1.1 | historical-reference-only | Original single-command/topology intent, without reopening historical work |
| Epic 26 production OpenBao work | historical production context | Immutable reference for production identity and topology; not a source of local implementation credit |

### Git Intelligence

- Creation baseline is `4d2e4e2f3188e57143c6290df8ff47e360ff3e27` (`feat(openbao): Implement OpenBao-first secret management in Hexalith`). That commit delivered production Kubernetes/OpenBao manifests, documentation, and tests; it did not replace the root AppHost local-file topology.
- The next relevant earlier commit, `c7c2ca21` (`feat: add Access Telemetry components and Dapr integration`), explains the current AppHost access-telemetry resource/component relationships. Preserve those relationships while changing only the secret provider and startup gate.
- Creation began from a clean root worktree. During read-only research, root-declared submodule gitlinks for `references/Hexalith.Builds`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Tenants` appeared modified by concurrent/external checkout state. They are not Story 29.1 files; do not reset, stage, or include them.

### Latest Technical Information

- OpenBao dev mode is in-memory and unsealed but prints the initial root token; `-dev-no-store-token` only avoids token-helper persistence. Use normal server mode with the documented in-memory backend instead.
- Normal `inmem` starts sealed and loses all state on process restart. Initialization uses `/v1/sys/init`, `/v1/sys/unseal`, and `/v1/sys/health`; restart evidence must demonstrate deterministic rebuild, not persistence.
- OpenBao KV v2 ACL paths include the `data` segment. Dapr's prefix metadata excludes the engine name and must use `vaultValueType: map` for current consumers.
- Dapr documents OpenBao compatibility through `secretstores.hashicorp.vault`; there is no separate OpenBao component type. Dapr secret access is permissive unless both component scopes and secret scopes are configured.
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
- [Dapr secret scopes](https://docs.dapr.io/developing-applications/building-blocks/secrets/secrets-scopes/)
- [Dapr component scopes](https://docs.dapr.io/operations/components/component-scopes/)
- [Aspire external parameters](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/external-parameters)
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

### Completion Notes List

- 2026-07-19: Created Story 29.1 as the root-AppHost OpenBao vertical slice, with Story 29.2's reusable/template surface explicitly excluded.
- 2026-07-19: Reconciled current local-file behavior against D31, the existing production OpenBao implementation, pinned Aspire/Dapr/OpenBao APIs, security isolation, restart semantics, and test fixture ownership.
- 2026-07-19: Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

- `_bmad-output/implementation-artifacts/29-1-openbao-backed-apphost-secret-topology.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-19 | create-story | Created context-ready Story 29.1; moved Epic 29 from `backlog` to `in-progress` and Story 29.1 from `backlog` to `ready-for-dev`; left Story 29.2 at `backlog`. No implementation or dependency change occurred. | Actual phase delta +0 and cumulative +0. Fresh Release builds passed with 0 warnings/errors. Runner-derived xUnit baselines: Server.Tests 2,182 methods (Deployment 41), IntegrationTests 279 methods (Fixtures 21), ordering class 1, and production OpenBao regression class 7. Planned deltas: Server.Tests +3..5 and IntegrationTests +7..10 methods; planned values are not actual evidence. | matched 2/2 intended creation paths against baseline `4d2e4e2f3188e57143c6290df8ff47e360ff3e27`; sprint status changed from pre-create SHA-256 `201e054312a206b2933fa8fdb0dd650743670c4623347647193ea01eb8dfac04` to post-create SHA-256 `c7464bf870b8eb90cc4f0199d9be772a19279fd065e268c8854811967bbff79e`; root-declared submodule checkout changes are concurrent/external and excluded. |
