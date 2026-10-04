# PG-ONPREM-2 repository adoption — 2026-10-04

The Administrator approved the exact candidate and coherent repository consumer
adoption in [the implementation spec](../../implementation-artifacts/spec-pg-onprem-2-approved-profile-adoption.md).
Current canonical profile SHA-256 is
`7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe`;
mutation manifest SHA-256 is
`ff75ddd004a475420070f07cd82bbb5e74379f12ddebca7693dcd3616475d984`.

The original candidate receipt envelope's `pending-exact-byte-approval` state
records its proposal-time disposition and remains unchanged. This dated adoption
record supersedes that state for repository consumer selection only. All registry,
render, acquisition, historical profile and historical C1.15 packet bytes remain
immutable. No original operator intent or independently reviewed capture is rewritten.

## Adopted identities and bytes

The embedded `canonical_pg_onprem_2_profile()` reproduces all approved manifest
fields exactly. `canonical_pg_onprem_profile()` retains historical SHA-256
`dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`.
Four approved YAML inputs are copied byte-for-byte to their owning deployment
surfaces. All twelve bound configuration/security source inputs remain unchanged,
including the CA-only OpenBao 2.6.0 smoke client. Current server tooling and CI
select OpenBao 2.6.4 / chart 0.29.6; AppHost development defaults are unchanged.
Current PostgreSQL is 18.6 / 180006; Dapr remains the exact authenticated 1.18.1
index and authenticated platform-child identities. Actual execution-platform qualification
remains held under C1.17; an accepted OCI index does not observe the running platform.
Candidate canonical identity/configuration bytes are untouched.

The qualification overlay derives PG2 configuration, disabled gate and reporter
hashes without modifying the bound reporter file. Production remains disabled,
lifecycle/clock replicas remain zero, the reporter stays suspended, the qualification
gate stays disabled and its Lease is released. Capacity, workload and fault limits
remain unchanged. The current capacity appendix is corrected to the retained executable
admission multiplier `2` (one durable copy plus its WAL/snapshot copy); its former
multiplier `1` did not describe the executable rule. Historical PG1 tables remain
historical and no capacity relaxation is granted. The 168-hour (`7d`) horizon
remains evidence-only and is never admitted on the exact 400-GiB profile; a changed
admission rule requires a new approved profile. Repository preflights reject any of the sixteen source/configuration
input drifts before target calls.

## Capture and ownership boundaries

Literal C1.16/PG-ONPREM-2 is callable. It captures stable selected Component,
loaded capability advertisements, PostgreSQL/Dapr image identity, and the actual
read-only local peer server identity. All packets are immutable, secret-safe and
neutral: `gateStatus`, `componentBehavior`, `productionLifecycleWrites` and
`connectionLinkage` stay `not-evaluated`, with `productionGatePassed: false`.
A local `peer:postgres` query and a secret reference do not independently prove
Dapr's connection uses the observed backend. Historical C1.16 requires explicit
PG1 opt-in and retains its own version/pins. Legacy C1.15/PG1 behavior is preserved.
Current downstream predecessor, approval, C0 and terminal evidence requires PG2;
old/mixed evidence is refused. No historical packet earns successor credit.

Story 27.22 is the one C1.16 registration transaction, at backlog after focused
checks and the one-file scope guard. The remaining twenty-three gates remain held
and unregistered. The dated planning correction preserves Story 27.21's completed
historical capture and original literal command. Actual eligible-target authority,
external archive, independent reviewer, connection linkage and acceptance remain
operator prerequisites; no target was contacted by this adoption.

## Verification

Executed offline on 2026-10-04. All commands exited 0; suites were nonzero with
zero failures/errors/skips. Exact commands, working directories, exit codes, counts,
skips and raw log bytes are durably archived in the separate
[adoption verification receipt](adoption-verification-evidence.json). The prior
`final-verification-evidence.json` and historical capture receipts are unchanged.
The receipt labels implementation-stage broad and focused checks separately. It is
retained unchanged as pre-review evidence. The review-patched current source is
covered by the separate final review receipt below.

| Check | Command / result | Raw log |
| :---- | :--------------- | :------ |
| Full C1 tooling | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py'` — 58 passed | `/tmp/pg2-c1-tests.log` |
| Verbose C1 matrix | `python3 -m unittest -v` with six exact successor/invalid/historical/legacy methods from `tests/tooling/access_telemetry_c1` — 6 passed | `/tmp/pg2-c1-matrix-tests.log` |
| Lifecycle tooling | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — 75 passed; added selector refusal test has a separate focused receipt | `/tmp/pg2-lifecycle-tests.log`, `/tmp/pg2-lifecycle-adoption-tests.log` |
| Production evidence tooling | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p '*_test.py' -v` — 123 passed | `/tmp/pg2-production-tests.log` |
| Server build | `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — 0 warnings/errors | `/tmp/pg2-server-build.log` |
| Earlier Server focused classes | `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class 'Hexalith.Memories.Server.Tests.Deployment.*' -class 'Hexalith.Memories.Server.Tests.Architecture.AccessTelemetryRetentionDecisionTests'` — 91 passed, 0 skipped/not run | `/tmp/pg2-server-tests.log` |
| CLI build | `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — 0 warnings/errors | `/tmp/pg2-cli-build.log` |
| Direct CI caller class | `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -class 'Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests'` — 66 passed, 0 skipped/not run | `/tmp/pg2-cli-tests.log` |
| Candidate integrity | `PYTHONDONTWRITEBYTECODE=1 python3 [-O] _bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/verify_candidate.py --repo-root .` — normal and optimized modes passed | `/tmp/pg2-candidate-normal.log`, `/tmp/pg2-candidate-optimized.log` |
| Offline overlays | `kubectl kustomize deploy/kubernetes/overlays/production`; `kubectl kustomize deploy/kubernetes/overlays/qualification` — PG2 derived hashes and disabled-state audit passed | `/tmp/pg2-production-render.yaml`, `/tmp/pg2-qualification-render.yaml` |
| Story slice | `python3 tools/check-story-slice-scope.py --require-record --story-key 27-22-component-and-backend-identity` — exactly 1 story checked | `/tmp/pg2-story-scope.log` |
| Final capacity/profile fixtures | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_adapter_profile.py' -v` — 32 passed, including multiplier-1 refusal | `/tmp/pg2-capacity-final-tests.log` |
| Final Server build | Same Release Server build command above — 0 warnings/errors | `/tmp/pg2-server-final-build.log` |
| Final affected Server classes | OpenBaoPlatformDocumentationTests, AccessTelemetryOperationsContractTests and AccessTelemetryRetentionDecisionTests — 28 passed, 0 skipped/not run; exact command in receipt | `/tmp/pg2-server-final-tests.log` |
| Final 168-hour admission wording guard | Same Release Server build, then only AccessTelemetryOperationsContractTests — 3 passed, 0 skipped/not run | `/tmp/pg2-server-admission-build.log`, `/tmp/pg2-server-admission-tests.log` |
| Final candidate / slice guards | Normal and optimized candidate checker and exactly-one-story scope guard — all passed | `/tmp/pg2-candidate-final-normal.log`, `/tmp/pg2-candidate-final-optimized.log`, `/tmp/pg2-story-final-scope.log` |
| Preservation / whitespace | Four exact copies, twelve fixed inputs, both profile hashes/manifests and archived C1.15 bytes checked; `git diff --check` passed | Candidate and lifecycle receipts above |

The temporary logs and their durably archived exact bytes are offline verification
output; no external qualification archive or live evidence is claimed. Matrix coverage is named in the focused C1 log; wrong PG/Dapr
running/declared pins and missing/renamed/duplicate named statuses are executed in
`test_downstream_producer_refuses_wrong_declared_and_running_pg_or_dapr_before_enablement`
and `test_c0_refuses_wrong_dapr_digest_without_adapter_or_wrapper_credit`.

This record grants no security requalification, C1 approval, Production activation,
Story 27.4 completion, A41 closure or Git publication.

## Remaining qualification prerequisites — dated 2026-10-04

Exact PG2 repository adoption is implemented; earlier pending exact-byte/adoption
claims in the earlier handoff and security plan are historical. An accepted OCI index does
not observe actual execution platform; that qualification remains held under
C1.17 / the unregistered Story 27.23 draft and earns no credit from C1.16.

Deployment Adapter Developer owns a separately scoped, unregistered future PG2
C1.15 producer/review prerequisite. Current `C1.15/PG-ONPREM-2` remains rejected
before target calls/output-directory creation; Story 27.21 and its accepted PG1
packet remain historical. Reopen requires approved executable producer/source
changes, nonzero positive/negative fixtures, separately authorized same-PG2 live
capture and a retrievable independently reviewed disposition. Generic C0 runtime
observations cannot substitute for that renewal.

Inherited in-Pod restart/container-incarnation and remote metadata-response
buffering limitations remain pending maintenance, owned by Deployment Adapter
Developer. No fixes or changes to historical ledger entries are claimed. The
[current handoff](implementation-handoff.md#remaining-qualification-prerequisites--dated-2026-10-04)
records concrete owner/consequence/reopen evidence for all four prerequisites.
No new story registration, live qualification, Production or A41 credit is granted.

## Final review verification — 2026-10-04

Review fixes now check running PostgreSQL and every observed profile workload in C0,
accept exact approved bare digests without widening allowlists, verify both Python
configuration preflight callers and authenticated child success, and install pinned
kubectl before its CI consumers. The approved C1.17 accountable role remains
Platform Operations; fresh PG2 C1.15 renewal remains separately owned and unsupported
by the current dispatcher. All eight patch groups are resolved. Three inherited
restart, remote-buffer and selected-Pod schema groups remain deferred with owners
and measurable reopen evidence; historical ledger entries are unchanged.

Parent final checks passed: lifecycle **79**, C1.16 **19**, and CI inventory **67**,
with zero failures/errors/skips. Candidate checks in normal/optimized modes, the
exactly-one-story scope guard, preservation audit and whitespace check passed.
[Final review verification receipt](parent-final-review-verification-evidence.json)
retains exact commands, raw log bytes, current source hashes, the original reviewed
diff and the final audit script. Earlier broad Server/production-evidence results
remain attributed to their unchanged implementation-stage sources; they are not
claimed as reruns after review.

The final byte audit matches all four adopted copies and twelve fixed inputs, both
profile identities, the original 4,429-byte C1.15 packet and 46 protected files.
Exactly one C1.16 backlog registration and twenty-three held allocations remain.
No target contact, live qualification, security acceptance, Production activation,
Story 27.4 advancement, A41 closure or Git publication occurred.
