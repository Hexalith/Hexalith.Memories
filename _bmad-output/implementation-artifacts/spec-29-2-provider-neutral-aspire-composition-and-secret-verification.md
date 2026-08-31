---
title: 'Provider-Neutral Aspire Composition and Secret Verification'
type: 'feature'
created: '2026-08-31'
status: 'done'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-29-context.md']
baseline_commit: 'bcfd84012f346efc83fa1f13b1dbe3413ef6f52a'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `Hexalith.Memories.Aspire` still hard-codes `secretstores.local.file` for both secret-store components, `deploy/dapr/components` templates still declare `secretstores.kubernetes`/`local.file`, and docs/README still describe the pre-Story-29.1 local-file topology.

**Approach:** Generalize the two `Hexalith.Memories.Aspire` extensions to accept an externally-provisioned secret-store resource (mirroring their existing `stateStore`/`pubSub` pattern); migrate the standalone templates to `secretstores.hashicorp.vault`; retire the local-file `secrets.json.example`; align docs/README; add structural + live evidence.

## Boundaries & Constraints

**Always:** Product code (`EmbeddingSecretStore`, access-telemetry lifecycle bootstrap, clock) stays provider-neutral — `DaprClient.GetSecretAsync` only. The two `Hexalith.Memories.Aspire` extensions take an externally-provisioned `IResourceBuilder<IDaprComponentResource>` per secret-store component, validated with `ArgumentNullException.ThrowIfNull()`, exactly like their existing `stateStore`/`pubSub` params. `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` use `secretstores.hashicorp.vault`, matching `deploy/kubernetes/base/dapr/*`. Every remaining Kubernetes Secret exception (`redis-secret`, `llm-secret`, OpenBao bootstrap token/CA, `app-api-token`, `dapr-api-token`, clock keys) stays named and documented. Preserve Story 29.1's prefixes and per-key allow-lists.

**Ask First:** Whether to also refactor the root AppHost's `Program.cs` (a parallel hand-rolled topology that doesn't call these extensions) onto the generalized methods — treat as out of scope unless approved. Whether to delete `deploy/dapr/secrets.json.example` outright vs. replace it with an OpenBao pointer.

**Never:** Change Story 29.1's AppHost resource/initializer/generation-gate behavior. Add OpenBao SDK/HTTP/credential code to any product project. Edit `deploy/kubernetes/**` (already vault-backed). Broaden Kubernetes Secret usage beyond the documented exceptions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Consumer supplies a vault-backed secret-store resource | `secretStore` param targets `secretstores.hashicorp.vault` | Sidecars reference it; no `local.file` component generated | N/A |
| Consumer omits the secret-store resource | `secretStore` is `null` | Extension throws at composition time | `ArgumentNullException`, fails closed before AppHost starts |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:60-90` -- `AddHexalithMemoriesSearchIndexServer`; lines 87-90 hard-code the `memories-secretstore` local-file component. Replace `secretStoreComponentPath` (string) with `IResourceBuilder<IDaprComponentResource> secretStore`, mirroring `stateStore`/`pubSub` (lines 62-63).
- `src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryExtensions.cs:17-96` (hard-code at 37-40) -- same generalization for `access-telemetry-secrets`.
- Return records `HexalithMemoriesAccessTelemetryResources.cs:14-20`, `HexalithMemoriesSearchIndexServerResources.cs:22-26` already expose `SecretStore` as `IResourceBuilder<IDaprComponentResource>` -- no shape change.
- `src/Hexalith.Memories.Aspire/README.md:20-26` -- update sample usage to the new signature.
- Provider-neutral consumers, preserve as-is: `EmbeddingSecretStore.cs:17,44-46`; `AccessTelemetryLifecycleBootstrapService.cs:54-57`; `Hexalith.Memories.AccessTelemetry.Clock/Program.cs:21-25` (all `DaprClient.GetSecretAsync`-only).
- `deploy/dapr/components/secretstore.yaml:6` -- `secretstores.kubernetes` → `hashicorp.vault`, matching `deploy/kubernetes/base/dapr/secretstore.yaml:6`.
- `deploy/dapr/components/access-telemetry-secrets.yaml:7,11` -- `local.file`/`secretsFile` → `hashicorp.vault`, matching the Kubernetes base equivalent.
- `deploy/dapr/secrets.json.example` -- remove or replace once the local-file component it backs is retired.
- `docs/operations/embedding-providers.md:178-179` -- rewrite stale "AppHost uses `local.file`" claim.
- `docs/operations/openbao.md:341-364` (exception table 350-353) -- extend for `deploy/dapr/components` exceptions.
- `README.md:74` -- update `deploy/dapr/components/` description.
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostOpenBaoConfigurationTests.cs:78-82` -- `ShouldNotContain("secretstores.local.file")` pattern to extend for templates and the generalized extensions; `OpenBaoPlatformDocumentationTests.cs:511` -- doc-content assertion pattern to extend for updated docs/README.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs`; `OpenBaoSafetyContractTests.cs:102` (`ContainsSensitiveValue` helper) -- live/negative-evidence patterns to extend for the generalized surface. No `Hexalith.Memories.Aspire`-specific test project exists; `Deployment/` under `Server.Tests` is the de facto home.

## Tasks & Acceptance

**Execution:**
- [x] `HexalithMemoriesServerExtensions.cs` -- accept externally-provisioned `secretStore` param; drop internal local-file component -- provider-neutral Server composition.
- [x] `HexalithMemoriesAccessTelemetryExtensions.cs` -- same generalization -- removes hard-coded `access-telemetry-secrets`; also wires the previously-unreferenced clock sidecar to `secretStore` (sidecar-level `WithReference`, `WaitFor`, and the CS0618 project-level reference), matching lifecycle/server, so the clock satisfies the "clock sidecars reference their required components" AC.
- [x] `Hexalith.Memories.Aspire/README.md` -- update sample to new signature.
- [x] `deploy/dapr/components/secretstore.yaml` -- `type: secretstores.hashicorp.vault`.
- [x] `deploy/dapr/components/access-telemetry-secrets.yaml` -- `type: secretstores.hashicorp.vault`, remove `secretsFile`.
- [x] `deploy/dapr/secrets.json.example` -- deleted (Ask First resolved during code review: an initial "replace with a pointer notice" attempt kept the misleading `.example` extension on a file that no longer holds an example config shape; deletion is cleaner since `docs/operations/openbao.md` and `README.md` already document the retirement, and nothing in the repo depends on the file's existence).
- [x] `docs/operations/embedding-providers.md` -- rewrite stale local-file passage.
- [x] `docs/operations/openbao.md` -- extend exception section for `deploy/dapr/components`.
- [x] `README.md` -- update `deploy/dapr/components/` description.
- [x] `tests/.../Deployment/` -- structural tests: templates are vault-typed, extensions build no local-file component, clock sidecar references/waits for the secret store.
- [x] `OpenBaoTopologyIntegrationTests.cs` -- extend to prove both components resolve OpenBao values through the generalized call shape without exposure.
- [x] Unit test: extensions throw `ArgumentNullException` when `secretStore` is `null`.

**Acceptance Criteria:**
- Given a consumer composes Memories through `Hexalith.Memories.Aspire` and supplies a Dapr secret-store resource, when the extensions run, then no `secretstores.local.file` component is generated and Server/lifecycle/clock sidecars reference their required components.
- Given embedding, lifecycle bootstrap, or clock code resolves a secret, when it executes, then it uses only `DaprClient.GetSecretAsync` and no product project contains OpenBao SDK/HTTP/credential code.
- Given standalone templates, tests, and operations docs are reviewed, when Story 29.2 completes, then they follow OpenBao-first, every remaining Kubernetes Secret exception is documented, and automated tests prove both components resolve OpenBao values without disclosing them.

## Spec Change Log

<!-- Empty until the first bad_spec loopback. -->

## Design Notes

The root AppHost (`Program.cs`) doesn't call either `Hexalith.Memories.Aspire` extension today — it hand-rolls its own vault-backed topology from Story 29.1. This story generalizes the package for *external* consumers; it does not require rewiring the root AppHost (flagged under Ask First).

## Verification

**Commands:**
- `dotnet build src/Hexalith.Memories.Aspire/Hexalith.Memories.Aspire.csproj --configuration Release --no-restore -m:1` -- expected: 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore -m:1` -- expected: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo` -- expected: extended `OpenBaoTopologyIntegrationTests` methods present and passing.

## Suggested Review Order

**Reusable Aspire composition surface (entry point)**

- Both secret-store params switch from a local-file-building string path to an externally-provisioned resource.
  [`HexalithMemoriesServerExtensions.cs:67-71`](../../src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs#L67)
  [`HexalithMemoriesAccessTelemetryExtensions.cs:33`](../../src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryExtensions.cs#L33)

- Server sidecar- and project-level `secretStore` wiring, mirroring the existing `stateStore`/`pubSub` pattern.
  [`HexalithMemoriesServerExtensions.cs:135`](../../src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs#L135)

- Review-round fix: the clock sidecar never referenced the secret store at all before this patch.
  [`HexalithMemoriesAccessTelemetryExtensions.cs:97`](../../src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryExtensions.cs#L97)

**Standalone Dapr templates**

- `secretstore.yaml` moves from `secretstores.kubernetes` to `secretstores.hashicorp.vault`, byte-identical to the Kubernetes base manifest.
  [`secretstore.yaml:10`](../../deploy/dapr/components/secretstore.yaml#L10)

- `access-telemetry-secrets.yaml` moves from `secretstores.local.file` to vault, same treatment.
  [`access-telemetry-secrets.yaml:10`](../../deploy/dapr/components/access-telemetry-secrets.yaml#L10)

- Review-round fix: `secrets.json.example` deleted rather than repurposed, since its `.example` extension implied a copyable local-file config that no longer exists.

**Documentation**

- Canonical Kubernetes-Secret-exception doc extended to cover the standalone templates and the generalized extensions.
  [`openbao.md:366`](../../docs/operations/openbao.md#L366)

- Stale "AppHost uses `secretstores.local.file`" claim rewritten to match Story 29.1's vault-backed reality.
  [`embedding-providers.md:178`](../../docs/operations/embedding-providers.md#L178)

- Top-level quick-start and topology description updated for the vault-backed local dev flow.
  [`README.md:40`](../../README.md#L40)
  [`README.md:74`](../../README.md#L74)

- Package README sample usage updated to the new resource-based signature, plus an `AddHexalithMemoriesAccessTelemetry` example added during review.
  [`Aspire README.md:21`](../../src/Hexalith.Memories.Aspire/README.md#L21)
  [`Aspire README.md:44`](../../src/Hexalith.Memories.Aspire/README.md#L44)

**Tests**

- Review-round replacement for an initial brittle text-slice test: exact occurrence-count guards on `.WithReference(secretStore)`/`.WaitFor(secretStore)` call sites, since the extensions' hard-coded cross-repo `AddProject<T>(..., launchProfileName: "http")` calls make full Aspire-model execution infeasible inside this repo's own test suite.
  [`HexalithMemoriesAspireSecretStoreTests.cs:104`](../../tests/Hexalith.Memories.Server.Tests/Deployment/HexalithMemoriesAspireSecretStoreTests.cs#L104)
  [`HexalithMemoriesAspireSecretStoreTests.cs:117`](../../tests/Hexalith.Memories.Server.Tests/Deployment/HexalithMemoriesAspireSecretStoreTests.cs#L117)

- Template conformance strengthened from key-presence to exact-value assertions (e.g. `skipVerify: "false"`) so a silent regression can't pass.
  [`DaprComponentTemplateTests.cs:35`](../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/DaprComponentTemplateTests.cs#L35)
  [`DaprComponentTemplateTests.cs:72`](../../tests/Hexalith.Memories.Server.Tests/NaturalLanguage/DaprComponentTemplateTests.cs#L72)

- Live-fixture guard tying the generalized extension surface to this file's existing OpenBao evidence.
  [`OpenBaoTopologyIntegrationTests.cs:208`](../../tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs#L208)
