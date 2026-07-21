---
title: 'Fix OpenBao staging in production deployment verification'
type: 'bugfix'
created: '2026-07-21'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'bf911d5ca1dbab8c763e4df1c3e3d74773493117'
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

## Tasks & Acceptance

**Execution:**
- [ ] `tools/verify-production-deployment.ps1` -- stage pinned TLS OpenBao, initialize KV v2, seed required runtime/access-telemetry maps, create isolated read-only identities and token/CA-only bootstrap Secrets, verify allowed/denied access, order application scale-up after dependency readiness, redact diagnostics, and clean up owned temporary material.
- [ ] `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` -- cover bootstrap ordering, both stores/prefixes, fail-closed access, unchanged 60-second timing, and generated-secret redaction without accepting a Kubernetes-store fallback.
- [ ] `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` and `.github/workflows/ci.yml` -- pin any required verifier prerequisite/version and preserve the existing publish, validate, and always-upload contract.
- [ ] `docs/operations/deployment-configuration.md` -- document that the disposable verifier stages the external OpenBao dependency while exercising the unchanged production components.

**Acceptance Criteria:**
- Given the four CI-built OCI archives and a fresh kind cluster, when production verification runs, then both unchanged OpenBao-backed Dapr components initialize and Server/MCP complete every existing health, fault, and restoration stage with final stage `required-server-mcp-restored`.
- Given runtime and access-telemetry scoped identities, when verification attempts permitted and cross-prefix reads, then permitted values resolve through Dapr while both cross-prefix directions fail without revealing values.
- Given bootstrap or component failure, when CI captures evidence, then the failing stage and provider/component state are actionable while root, unseal, scoped-token, CA-private-key, and seeded values are absent.

## Spec Change Log

## Design Notes

The failed run proves the authenticated readiness probe is working: its 503 body correctly isolates the dead Dapr sidecar. The repair must stage the newly required external provider before starting application timing, as the verifier already does for other required infrastructure. Patching components back to Kubernetes would make CI green while bypassing architecture decision D31 and would not validate the shipped production topology.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py'` -- expected: all verifier/evidence fixtures pass.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` followed by direct xUnit v3 `-class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` execution -- expected: zero build errors and all CI contracts pass.
- Run the publish plus `tools/verify-production-deployment.ps1` commands from `.github/workflows/ci.yml` -- expected: zero skips, TLS OpenBao/Dapr access evidence, aggregate health/fault recovery, and terminal stage `required-server-mcp-restored`.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.
