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

## Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :-------------- | :------------ |
| Story 29.1 (OpenBao-Backed AppHost Secret Topology) | `historical-reference-only` | Dependency/sequencing context only. Story 29.1 built the OpenBao resource, bootstrap, and isolation contract this story consumes, and its own Slice Proof pre-declared "changing reusable APIs/templates" as a second independently shippable slice already assigned to Story 29.2. This story preserves Story 29.1's AppHost resource/initializer/generation-gate behavior and its prefixes/allow-lists unchanged rather than reusing its shape as a template. |
| `stateStore`/`pubSub` externally-provisioned-component parameter pattern (pre-existing in `Hexalith.Memories.Aspire`) | `current-narrow-pattern` | Re-verified against current source (`HexalithMemoriesServerExtensions.cs`). The new `secretStore` parameter mirrors this exact existing pattern; only the pattern is reused, not a whole-story shape. |

## Slice Proof

This story closes exactly one independently shippable slice: generalizing both `Hexalith.Memories.Aspire` extensions' secret-store parameter to an externally-provisioned resource, migrating the standalone `deploy/dapr/components` templates off `secretstores.local.file`/`secretstores.kubernetes`, and aligning docs/tests to match. Story 29.1 itself pre-declared and assigned this exact slice to Story 29.2. It does not touch Story 29.1's AppHost resource, initializer, or generation-gate behavior, and it does not rewire the root AppHost (`Program.cs`) or `deploy/kubernetes/**` (both explicitly out of scope per Boundaries & Constraints). Externally observable proof: `HexalithMemoriesAspireSecretStoreTests`/`DaprComponentTemplateTests` structurally pin the generalized surface, and this story's `OpenBaoTopologyIntegrationTests` extension ties it to the fixture's existing live evidence that the component shape resolves OpenBao values through Dapr without disclosure and fails closed on cross-prefix reads.

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

### Review Findings

- [x] [Review][Decision] Breaking `Hexalith.Memories.Aspire` API change shipped without a `BREAKING CHANGE:` trailer and left the one known real consumer broken — both extension signatures replace `secretStoreComponentPath` (string) with `secretStore` (`IResourceBuilder<IDaprComponentResource>`), an intentional breaking change to a packable (`IsPackable=true`) NuGet package, and commit `09a98f1d` is a plain `feat:` commit with no `BREAKING CHANGE:` footer, so semantic-release will publish it as a minor (semver-compatible) bump. `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs:120-127` called the retired 3-arg positional overload and would have failed to compile the moment the Tenants submodule bumped its `Hexalith.Memories.Aspire` package reference. **Resolved:** the Administrator chose to fix Hexalith.Tenants now. Fixed and pushed as its own submodule commit — `references/Hexalith.Tenants@7453ba5b` ("fix: update Memories secret-store call site for the Aspire 29.2 signature change") — which builds and passes an externally-provisioned `secretstores.local.file` component via `AddDaprComponent`, mirroring the extension's new contract and preserving prior dev behavior. The `BREAKING CHANGE:` trailer gap on `09a98f1d` itself was not corrected (would require rewriting a published commit) and is accepted as-is since the one real consumer is now fixed. [src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:67]
- [x] [Review][Patch] Missing required `Historical Context Classification` / `Slice Proof` sections — `_bmad/custom/story-scope-guard.md`'s Creation gate requires these sections whenever a prior story materially influences scope; this spec repeatedly cites and is constrained by Story 29.1 (preserve its prefixes/allow-lists; never change its AppHost resource/initializer/generation-gate behavior) yet carries neither section. This is an established spec-kernel convention present in sibling specs (e.g. `spec-24-7-fourth-pass-action-item-closure.md`) and a fail-closed "Critical Miss" per policy — it should have blocked the `backlog`→`review` sprint-status update already made in this same diff. [_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md]
- [x] [Review][Patch] `docs/operations/openbao.md` ships an unverified "standalone Dapr self-hosted host" `secretKeyRef`-resolution claim — the new passage asserts a bare self-hosted Dapr install (not just Kubernetes) can resolve the templates' `secretKeyRef` bootstrap-Secret references the same way, but `secretKeyRef` normally resolves through a configured secret-store backend that Kubernetes-hosted Dapr supplies automatically and a self-hosted install does not; this diff's own new `deferred-work.md` entry admits the claim is unverified and possibly inaccurate, yet the shipped doc carries no such caveat. [docs/operations/openbao.md:366]
- [x] [Review][Patch] New Aspire wiring tests assert aggregate reference counts, not per-resource attachment — `ServerExtension_WiresTheSuppliedSecretStoreToTheServerSidecarAndProjectAndWaitsForIt` / `AccessTelemetryExtension_WiresTheSuppliedSecretStoreToServerLifecycleAndClockAndWaitsForIt` count whole-file occurrences of `.WithReference(secretStore)`/`.WaitFor(secretStore)` rather than verifying which resource each call attaches to; a regression that drops one wiring call while adding a compensating duplicate elsewhere keeps the aggregate count unchanged and would pass undetected. Current production wiring is correct (verified by reading `HexalithMemoriesAccessTelemetryExtensions.cs:61-106`), so this is a coverage gap, not a live defect. The new `OpenBaoTopologyIntegrationTests` guard's doc comment additionally claims this "inherits the same proof" as the fixture's live OpenBao evidence, but no test actually exercises the generalized extensions against a live secret-store resource. [tests/Hexalith.Memories.Server.Tests/Deployment/HexalithMemoriesAspireSecretStoreTests.cs:104]
- [x] [Review][Patch] Access-telemetry template test doesn't guard against a reintroduced `nestedSeparator` key — `AccessTelemetrySecretsTemplate_UsesOpenBaoVaultStoreScopedToAccessTelemetryApps` checks `ShouldNotContain("secretsFile")` (the retired local-file key) but has no equivalent check for the paired retired key `nestedSeparator`; a regression reintroducing it alongside the vault fields would pass every existing assertion undetected. [tests/Hexalith.Memories.Server.Tests/NaturalLanguage/DaprComponentTemplateTests.cs:72]
- [x] [Review][Patch] New `deferred-work.md` entries omit the Story-14.5 structured field block — both entries added by this diff (Tenants call-site break; unverified self-hosted-Dapr claim) use only the legacy `source_spec:`/`summary:`/`evidence:` prose shape and omit the `ID:`/`Status:`/`Source story:`/`Target artifact:`/`Re-open trigger:` block the file's own schema section says active entries "should carry" so tooling/reviewers don't have to infer status from prose. [_bmad-output/implementation-artifacts/deferred-work.md:3484]
- [x] [Review][Patch] Integration-fixture test duplicates unit-test source-text assertions — `OpenBaoTopologyIntegrationTests.GeneralizedAspireExtensions_RequireTheExternallyProvisionedComponentShapeThisFixtureProvesResolvesOpenBaoWithoutDisclosure` repeats the same three `ShouldNotContain`/`ShouldContain` source-grep assertions already present in `HexalithMemoriesAspireSecretStoreTests`, in a fixture class otherwise reserved for genuine live/negative OpenBao evidence per the spec's own Code Map — risking a reviewer crediting it as broader proof than it is, and risking silent drift between the two copies. [tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs:666]
- [x] [Review][Patch] Spec's cited Verification command only proves discovery, not "passing" — `dotnet exec ... Hexalith.Memories.IntegrationTests.dll -list methods -noLogo` only lists/discovers test methods; it cannot itself demonstrate the extended `OpenBaoTopologyIntegrationTests` methods "passing" as the Verification section claims. CI's `Category=Integration` lane does actually execute them, so this is a documentation-accuracy gap in the spec, not a missing safety net. [_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md:178]
- [x] [Review][Patch] Test's repo-root resolution silently falls back instead of failing loudly — `ResolveRepoRoot()`'s 8-level walk-up returns `AppContext.BaseDirectory` if it never finds `Hexalith.Memories.slnx`, instead of throwing; `ReadRepoFile` has its own explicit `File.Exists` assertion that would catch this, but `TestProjectMetadata.ProjectPath` (used to resolve the test project's own `.csproj` for `AddProject<TestProjectMetadata>`) has no equivalent guard, so a failed walk-up there would surface as a confusing downstream Aspire error instead of a clear message. [tests/Hexalith.Memories.Server.Tests/Deployment/HexalithMemoriesAspireSecretStoreTests.cs:173]

## Spec Change Log

<!-- Empty until the first bad_spec loopback. -->

## Design Notes

The root AppHost (`Program.cs`) doesn't call either `Hexalith.Memories.Aspire` extension today — it hand-rolls its own vault-backed topology from Story 29.1. This story generalizes the package for *external* consumers; it does not require rewiring the root AppHost (flagged under Ask First).

## Verification

**Commands:**
- `dotnet build src/Hexalith.Memories.Aspire/Hexalith.Memories.Aspire.csproj --configuration Release --no-restore -m:1` -- expected: 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore -m:1` -- expected: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -list methods -noLogo` -- expected: extended `OpenBaoTopologyIntegrationTests` methods present (`-list methods` only discovers; it does not itself prove a passing run). CI's `Category=Integration` lane executes these methods and is the actual pass/fail evidence.

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
