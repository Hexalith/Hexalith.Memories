---
title: 'Fix CI test inventory and production rollout probes'
type: 'bugfix'
created: '2026-07-19'
status: 'in-review'
review_loop_iteration: 0
baseline_commit: '1a557ca3c7a50c7fe0db2dedfd1af2d3b21fe83b'
context:
  - '{project-root}/.gitattributes'
  - '{project-root}/.github/workflows/ci.yml'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run `29697587488` fails in the coverage and fast-integration lanes because Bash passes carriage returns from CRLF-materialized project inventories into `dotnet`, producing invalid project paths. Its disposable production verification also cannot observe the Server health endpoint: the verifier uses an unauthenticated `nc` probe that is not the image's supported health-check contract, so the job receives no HTTP status and times out.

**Approach:** Normalize inventory records at the Bash runner boundary, preserving the repository's CRLF checkout policy. Align the production verifier with the deployment manifest by using the image-provided `wget`, forwarding the container's application token, and retaining HTTP status/body diagnostics for both healthy and intentionally unhealthy checks. Extend the existing tooling and CI contract tests to prevent either regression.

**Boundaries & Constraints**

**Always:** Keep inventory files and the repository line-ending policy unchanged; preserve exact project selection, filters, result directories, and TRX/coverage validation. Use the existing `APP_API_TOKEN`/`dapr-api-token` contract, redact no secrets into evidence, and retain status/body evidence for expected 200 and 503 responses. Keep CI fail-closed and make artifact uploads useful after test failures.

**Ask First:** Changes to deployment health semantics, authentication contracts, test inventory membership, or CI job scope beyond the failed run.

**Never:** Convert the repository globally to LF, remove the coverage/integration gates, use `|| true` to hide test failures, bypass the application token, add a new base image/tool dependency solely for probing, or weaken production verification to accept an empty/unknown HTTP response.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|----------------------------|----------------|
| CRLF inventory | Bash reads a project path materialized with `\\r\\n` | `dotnet` receives the exact LF-normalized path | Missing inventory still fails clearly |
| Healthy probe | Running Server returns authenticated HTTP 200 Healthy JSON | Verifier accepts the response and records its body | Missing status/body remains a failure |
| Dependency fault probe | Server returns authenticated HTTP 503 Unhealthy JSON | Verifier accepts the expected failure state and preserves diagnostics | Wrong status, malformed JSON, or missing dependency evidence fails |

</frozen-after-approval>

## Code Map

- `tools/test.sh` -- Bash test runner that reads the CRLF-materialized project inventories and constructs `dotnet test` arguments.
- `tools/verify-production-deployment.ps1` -- disposable Kubernetes rollout verifier and authenticated health/fault probe.
- `deploy/kubernetes/base/server-deployment.yaml` -- canonical container health probe and application-token header contract.
- `tests/tooling/coverage_gate/test_runner_contract_test.py` -- wrapper argument contract, including exact benchmark project selection.
- `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py` -- production evidence/verifier contract fixtures.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- CI and release wiring contract tests.

## Tasks & Acceptance

**Execution:**
- [x] `tools/test.sh` -- strip only terminal carriage returns while reading inventories -- make CRLF checkout records safe without changing inventory contents or filters.
- [x] `tools/verify-production-deployment.ps1` -- replace the unsupported unauthenticated raw socket probe with an authenticated image-native HTTP probe that parses status and body -- make startup and fault-injection checks match deployed behavior.
- [x] `tests/tooling/coverage_gate/test_runner_contract_test.py`, `tests/tooling/production_deployment_evidence/production_deployment_evidence_test.py`, and `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` -- add focused regression assertions -- keep the runner and production contracts executable.

**Acceptance Criteria:**
- Given a normal CRLF checkout, when the Bash runner reads any selected inventory, then every project argument is free of carriage returns and existing filters/result validation remain unchanged.
- Given a disposable deployment whose Server image exposes the manifest's authenticated `/ready` contract, when production verification runs, then healthy startup and intentional dependency-fault checks receive and validate the expected HTTP status and JSON state.
- Given either runner or verifier failure, when CI reaches its `always()` upload steps, then the expected result/evidence paths remain available for diagnostics and no gate is bypassed.

## Design Notes

The repository deliberately stores text as LF but materializes most text as CRLF. PowerShell already trims inventory lines; the Bash path must normalize at the read boundary rather than changing tracked inventories. The deployment's own probes use BusyBox `wget` with `APP_API_TOKEN`; the verifier should reuse that contract and use `--server-response`/captured output so 503 fault states remain observable instead of treating nonzero `wget` exit status as an uninformative command failure.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` -- expected: all coverage and wrapper contract fixtures pass.
- `python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py'` -- expected: all evidence and verifier contract fixtures pass.
- `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release -p:NuGetAudit=false -p:MinVerVersionOverride=0.0.264` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~CiTestInventoryTests` -- expected: CI inventory contracts pass, or invoke the built xUnit v3 assembly directly if project filtering is blocked by Microsoft.Testing.Platform.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.
