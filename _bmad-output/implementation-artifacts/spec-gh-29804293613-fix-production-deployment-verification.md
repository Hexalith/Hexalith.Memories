---
title: 'Fix OpenBao staging in production deployment verification'
type: 'bugfix'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: '2411c03c497133f48ec4ad42be9b333f8fc157c4'
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/operations/openbao.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions job `88551584287` fails because the disposable production verifier applies OpenBao-backed Dapr components without staging their TLS endpoint or bootstrap Secrets. Both Server sidecars exit while loading `access-telemetry-secrets`, so aggregate readiness remains HTTP 503 and `statestore` is unreachable.

**Approach:** Make the verifier provision a pinned, TLS-verified disposable OpenBao service, initialize and seed its two isolated prefixes, publish narrowly scoped token/CA bootstrap Secrets, and prove the unchanged production Dapr components work before application startup timing begins.

## Boundaries & Constraints

**Always:** Preserve OpenBao `2.6.0`, TLS verification, distinct runtime/access-telemetry policies and prefixes, the existing Dapr component names/scopes, the 60-second application startup contract, zero-skip kind verification, final Server/MCP restoration, and redacted always-uploaded evidence. Keep direct pod-input Kubernetes Secrets unchanged; bootstrap Secrets may contain only scoped token and CA material.

**Ask First:** Changing production OpenBao manifests, secret names or fields, Dapr authorization/scopes, the CI toolchain, or the production verification job's runtime budget.

**Never:** Replace either production Dapr store with `secretstores.kubernetes`, use OpenBao dev mode, disable TLS, seed literal credentials into tracked files, expose root/unseal/scoped tokens in commands or evidence, extend health timeouts, or bypass existing rollout/fault/recovery gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| OpenBao bootstrap | Fresh kind cluster and rendered production manifest | TLS OpenBao is initialized, unsealed, seeded, and exposes two read-only identities before Server/MCP scale-up | Fail at a named bootstrap stage with secret-safe diagnostics |
| Dapr secret access | Sidecar uses either production secret-store component | Allowed reads succeed; cross-prefix and non-allow-listed reads fail | Any unexpected allow or unavailable component fails closed |
| Application startup | OpenBao, Dapr, Redis, and FalkorDB are ready | Server and MCP reach authenticated aggregate `Healthy` within the existing 60 seconds | Preserve HTTP status/body plus OpenBao/Dapr diagnostics without secret values |

</frozen-after-approval>

## Code Map

- `tools/verify-production-deployment.ps1` -- owns disposable infrastructure staging, health/fault checks, recovery, diagnostics, and teardown.
- `deploy/kubernetes/base/dapr/secretstore.yaml` and `access-telemetry-secrets.yaml` -- authoritative TLS OpenBao component contract that must run unchanged.
- `deploy/openbao/values.yaml` and `docs/operations/openbao.md` -- pinned image, endpoint, TLS, policy, and bootstrap boundaries.
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` -- executable source-contract and redaction fixtures for the verifier.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- static CI/verifier wiring contract.
- `.github/workflows/ci.yml` -- production verification job and any explicitly pinned prerequisite setup.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `tools/production-deployment-openbao.ps1`
- `tools/verify-production-deployment.ps1`
- `tools/validate-production-deployment-evidence.ps1`
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `.github/workflows/ci.yml`
- `docs/operations/deployment-configuration.md`

## Tasks & Acceptance

**Execution:**
- [x] `tools/verify-production-deployment.ps1` -- stage pinned TLS OpenBao, initialize KV v2, seed required runtime/access-telemetry maps, create isolated read-only identities and token/CA-only bootstrap Secrets, verify allowed/denied access, order application scale-up after dependency readiness, redact diagnostics, and clean up owned temporary material.
- [x] `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` -- cover bootstrap ordering, both stores/prefixes, fail-closed access, unchanged 60-second timing, and generated-secret redaction without accepting a Kubernetes-store fallback.
- [x] `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` and `.github/workflows/ci.yml` -- pin any required verifier prerequisite/version and preserve the existing publish, validate, and always-upload contract.
- [x] `docs/operations/deployment-configuration.md` -- document that the disposable verifier stages the external OpenBao dependency while exercising the unchanged production components.

**Acceptance Criteria:**
- Given the four CI-built OCI archives and a fresh kind cluster, when production verification runs, then both unchanged OpenBao-backed Dapr components initialize and Server/MCP complete every existing health, fault, and restoration stage with final stage `required-server-mcp-restored`.
- Given runtime and access-telemetry scoped identities, when verification attempts permitted and cross-prefix reads, then permitted values resolve through Dapr while both cross-prefix directions fail without revealing values.
- Given bootstrap or component failure, when CI captures evidence, then the failing stage and provider/component state are actionable while root, unseal, scoped-token, CA-private-key, and seeded values are absent.

## Spec Change Log

- 2026-08-08 (present): Added an exact `## File Scope` allow-list so the commit-msg story-scope gate can accept the staged OpenBao verifier paths. Avoids a story-scope rejection after implementation completed. KEEP: disposable TLS OpenBao staging, immutable digest pin, unmodified hashicorp.vault stores, dual-store Dapr allow/deny probes, and strengthened bootstrap evidence validation.

The failed run proves the authenticated readiness probe is working: its 503 body correctly isolates the dead Dapr sidecar. The repair must stage the newly required external provider before starting application timing, as the verifier already does for other required infrastructure. Patching components back to Kubernetes would make CI green while bypassing architecture decision D31 and would not validate the shipped production topology.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py'` -- expected: all verifier/evidence fixtures pass.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` followed by direct xUnit v3 `-class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` execution -- expected: zero build errors and all CI contracts pass.
- Run the publish plus `tools/verify-production-deployment.ps1` commands from `.github/workflows/ci.yml` -- expected: zero skips, TLS OpenBao/Dapr access evidence, aggregate health/fault recovery, and terminal stage `required-server-mcp-restored`.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.

## Suggested Review Order

**Disposable OpenBao staging**

- Immutable digest pin and bootstrap orchestration entry for the disposable provider.
  [`production-deployment-openbao.ps1:5`](../../tools/production-deployment-openbao.ps1#L5)

- TLS service deploy refuses any image other than the hardcoded pin.
  [`production-deployment-openbao.ps1:95`](../../tools/production-deployment-openbao.ps1#L95)

- Prefix isolation requires permission-shaped denies, not any nonzero exit.
  [`production-deployment-openbao.ps1:401`](../../tools/production-deployment-openbao.ps1#L401)

- Full init/seed/token/bootstrap/root-revoke sequence before apps start.
  [`production-deployment-openbao.ps1:481`](../../tools/production-deployment-openbao.ps1#L481)

**Verifier wiring (unchanged production stores)**

- OPENBAO_IMAGE may only restate the pin; never overwrite it.
  [`verify-production-deployment.ps1:36`](../../tools/verify-production-deployment.ps1#L36)

- Bootstrap before apply; confirm vault components unmodified (D31).
  [`verify-production-deployment.ps1:1066`](../../tools/verify-production-deployment.ps1#L1066)

- Dapr allow/deny probes for both secretstore and access-telemetry-secrets.
  [`verify-production-deployment.ps1:1118`](../../tools/verify-production-deployment.ps1#L1118)

- Positive unmodified disclosure via verifiedVaultComponents.
  [`verify-production-deployment.ps1:135`](../../tools/verify-production-deployment.ps1#L135)

**Evidence validation**

- Succeeded packets must prove bootstrap stages, isolation, and pinned image.
  [`validate-production-deployment-evidence.ps1:368`](../../tools/validate-production-deployment-evidence.ps1#L368)

- Unmodified runs must name both vault stores; kubernetes substitution rejected.
  [`validate-production-deployment-evidence.ps1:245`](../../tools/validate-production-deployment-evidence.ps1#L245)

- Access-telemetry marker included in secret canaries.
  [`validate-production-deployment-evidence.ps1:445`](../../tools/validate-production-deployment-evidence.ps1#L445)

**Peripherals**

- Source-contract pin for staging order, TLS, and no kubernetes fallback.
  [`production_deployment_evidence_test.py:908`](../../tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py#L908)

- Regression pin for PowerShell `--from-file=` + comma precedence.
  [`production_deployment_evidence_test.py:956`](../../tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py#L956)

- CI inventory pins OpenBao helper, digest, and immutable-pin contract.
  [`CiTestInventoryTests.cs:738`](../../tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs#L738)

- Workflow pins OPENBAO_IMAGE digest for the verification job.
  [`ci.yml:18`](../../.github/workflows/ci.yml#L18)

- Ops doc: disposable verifier stages OpenBao, leaves production stores unchanged.
  [`deployment-configuration.md:66`](../../docs/operations/deployment-configuration.md#L66)
