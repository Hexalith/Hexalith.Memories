# Brownfield evidence, scope and preservation

Verified **2026-10-07**, root `main` at
`14bb1c17` (full revision obtainable with `git rev-parse HEAD`). Initial working
tree was clean. Investigation was local/read-only apart from this new spec
folder and disposable test/snapshot files; no live targets were contacted.

## Epic AC Verification

Source readings below verify repository facts and recorded decisions, not the
authenticity of external operational evidence. Verdicts apply only to the quoted
claim and recorded scope. Future requirements in this spec are proposed intent,
not assertions that their implementation already exists.

| Inherited claim / investigated mechanism | Class | Re-runnable command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| D3: "It does not parse the referenced artifact's contents or restrict its source path to a registered producer." | Behavior | `sed -n '2642,2800p' tools/verify_access_telemetry_lifecycle.py`; local reproduction below. | Gate bytes are hashed, not parsed; gate source path is verified as a Git blob without C1 registration. Non-JSON artifacts from `.editorconfig` pass. | confirmed |
| D3: "Its two approval records contain only role, reviewer, state and profile hash." | Existence/behavior | `sed -n '2772,2800p' tools/verify_access_telemetry_lifecycle.py` | Exact four-field approval set; role/profile/state and different reviewer strings checked; no principal/receipt/time/revocation checks. | confirmed |
| D3: validator "verifies 25 unique gate records, exact profile, artifact bytes, canonical Git source hashes, command ledgers and gate freshness when requested." | Count/behavior | `sed -n '2638,2800p' tools/verify_access_telemetry_lifecycle.py`; two C1 tests below. | 25 canonical IDs; unique paths/hashes; artifact/source checks when repository/evidence roots supplied; optional authorization freshness; structural ledger validation. | confirmed |
| PG2 contract: "The producer emits hexalith.access-telemetry.c1.evidence/v2 only for PG2 C1.15." | Existence/behavior | `sed -n '1,60p' tools/verify-access-telemetry-c1.ps1`; `sed -n '1078,1122p' tools/verify-access-telemetry-c1.ps1`; PG2 provenance test below. | PG2-only dispatcher and v2 emission; explicit session label; identity authentication not evaluated; independent disposition pending. | confirmed |
| PG2 contract: "producerSources contains each of the three used PowerShell sources and all sixteen approved inputs, exactly once." | Count/behavior | `sed -n '1,47p' tools/access-telemetry-c1-profile.ps1`; PG2 provenance test below. | Test recomputes full source/effective arguments/child receipts and verifies complete unique set. | confirmed |
| Context: current profile "PG-ONPREM-2, canonical SHA-256 7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe". | Existence/identity | `rg -n 'CURRENT_PROFILE_SHA256\|STORY_27_4_PROFILE_SHA256\|STORY_27_4_WORKLOAD_SHA256' tools/verify_access_telemetry_lifecycle.py`; canonical identity command below. | Current canonical profile/hash and approved two-writer workload/hash match; profile-specific inputs checked locally. | confirmed |
| Context: "Twenty-three other gates remain held and unregistered"; PG2 C1.15 is separately scoped/unregistered and Story 27.22 registered/done. | Count/status | `sed -n '465,474p' _bmad-output/implementation-artifacts/sprint-status.yaml`; `sed -n '42,70p' _bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`; `cat _bmad-output/implementation-artifacts/epic-27-context.md` | Tracker contains historical 27.21 and current 27.22 done, 27.4 in-progress; current context/evidence identifies PG2 renewal and other held owners. No registration created here. | confirmed |
| Context: Story 27.22 has "independently accepted captured C1.16 component/backend identity and full-set connection linkage" for its recorded window. | Recorded decision | `sed -n '18,23p' _bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`; `sed -n '82,91p' _bmad-output/implementation-artifacts/27-22-component-and-backend-identity.md`; current evidence section above. | Recorded historical acceptance and retained canonical refusal confirmed as repository records; not re-authenticated here and no new session eligibility inferred. | confirmed |
| Production writes remain disabled; Story 27.4 incomplete and A41 action open. | Configuration/status | `cat deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`; `sed -n '465,467p' _bmad-output/implementation-artifacts/sprint-status.yaml`; `sed -n '617,623p' _bmad-output/implementation-artifacts/sprint-status.yaml` | Two Production lifecycle deployments remain zero; disabled annotation retained; 27.4 in-progress, A41 action open. This is repository state, not a live observation. | confirmed |
| Existing Server JWT/OIDC could provide identity authentication, but has no C1 decision-receipt contract. | Existence/scope | `cat src/Hexalith.Memories.Server/Authentication/ConfigureServerJwtBearerOptions.cs`; `cat src/Hexalith.Memories.Server/Authentication/ValidateServerAuthenticationOptions.cs` | Signature, issuer, audience, lifetime, signed-token/expiry and configured algorithms; one-minute JWT clock skew. Tenant ingress scope, no C1 reviewer role/receipt/revocation mechanism in these classes. | confirmed |
| Existing access-telemetry authorities and physical receipt are not C1 reviewer decisions. | Existence/scope | `cat src/Hexalith.Memories.AccessTelemetry/Capability/AccessTelemetryAuthorityPolicy.cs`; `sed -n '131,173p' src/Hexalith.Memories.AccessTelemetry/Program.cs`; `cat src/Hexalith.Memories.AccessTelemetry.Contracts/AccessTelemetryPhysicalReclamationEvidenceReceipt.cs` | Write/read/delete/clock/inspection/physical-evidence action matrix; Dapr-only physical receipt binds component/artifact/reporter/time, not reviewer role/session/decision expiry/revocation. | confirmed |
| Clock signatures are time attestations, not approval receipts. | Existence/scope | `cat src/Hexalith.Memories.AccessTelemetry.Clock/EcdsaClockAttestationSigner.cs`; `cat src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs` | ECDSA clock signer with Dapr-secret material and authenticated UTC sources; no C1 review decision issuer. Do not reuse its signing key as review authority. | confirmed |
| Qualification bearer/Lease checks do not authenticate C1 reviewers. | Behavior/scope | `sed -n '175,267p' tools/access_telemetry_producer_common.py`; `rg -n 'qualification-enable\|qualification-renew\|qualification-disable\|_lease_holder' tools/access_telemetry_producer_common.py` | Local bearer checks decode lifetime/one-tenant claims and owner-only file; Server verifies product tokens. Lease ownership/renewal bounds scenario execution, not review authority or decision receipts. | confirmed |
| Existing C5/C6 label/hash checks do not close the authority gap. | Behavior | `sed -n '4776,4815p' tools/verify_access_telemetry_lifecycle.py`; `sed -n '2050,2067p' tools/verify_access_telemetry_lifecycle.py` | Post-evidence approver labels, time ordering and C0-C4 hashes; no C1 receipt adapter. Existing registry is for downstream checkpoints, not all C1 producers. | confirmed |
| A repository-supported C1 reviewer/decision adapter was not found in inspected source. | Scoped absence | `rg -n -i 'decision.?receipt\|authority.?receipt\|reviewer.?principal\|reviewer.?auth' src tools docs tests -g '!**/obj/**' -g '!**/bin/**'`; same pattern over `references/Hexalith.Platform/src references/Hexalith.McpCli/src references/Hexalith.Commons/src -g '*.cs'` | Root matches are the proposed operations disposition documentation; no implemented adapter in inspected source. This does not rule out an external identity service. No reference-directory skills loaded. | confirmed |
| Runtime qualification gate retains a historical profile pin, unlike the current Python profile consumer. | Identity comparison, investigated rather than inherited | `rg -n 'ApprovedProfileSha256\|MaximumGateLifetime' src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLifecycle/AccessTelemetryQualificationGate.cs`; current canonical identity command. | C# runtime gate still pins historical `dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14`; Python consumers pin current `7f9f…`. No current-runtime compatibility claim is made; P7 records a separate prerequisite. | confirmed |
| Genuine live target identity, current session grant/custody, authorized reviewer receipt and revocation status are available. | Operational evidence | No permitted command in this offline task can authenticate these external facts; do not invoke collectors. | Blocker P1/P3/P6; Security/Operations supply authentic inputs. They are **not** load-bearing facts or acceptance justification. Reopen on independently supplied authority/custody/receipt evidence. | unverifiable |

The runtime profile mismatch is a newly verified source fact. No inherited
source claim was corrected or historical decision overwritten.

## Executed focused checks

```bash
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -k test_c1_ -v
PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p pg2_runtime_control_plane_identity_test.py -k test_full_source_and_effective_invocation_and_child_receipts_are_recomputable -v
```

Results: **2 passed** in the C1 structural/freshness lane and **1 passed** in the
PG2 provenance lane, zero failures/skips. These existing tests do not prove the
new authentication/verifier contract. The new negative matrix was not executed
because its implementation does not yet exist.

Canonical identity check, read-only:

```bash
PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import sys
from pathlib import Path
sys.path.insert(0, str(Path.cwd() / 'tools'))
import verify_access_telemetry_lifecycle as v
v.validate_current_profile_inputs(Path.cwd())
assert v.canonical_pg_onprem_2_profile().manifest()['profile_sha256'] == '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
assert v.STORY_27_4_WORKLOAD_SHA256 == '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
print('Current profile, sixteen bound inputs and workload hashes match.')
PY
```

## Local gap reproduction

Executed successfully with synthetic temporary artifacts; no target access,
Git mutation, review authority or acceptance created. This command deliberately
demonstrates the current validator deficiency, not valid production evidence:

```bash
PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import hashlib, subprocess, sys, tempfile
from pathlib import Path
root = Path.cwd()
sys.path[:0] = [str(root / 'tools'), str(root / 'tests/tooling/access_telemetry_lifecycle')]
import verify_access_telemetry_lifecycle as v
from test_retention_verification import predecessor
p = predecessor()
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
source = subprocess.check_output(['git', 'show', f'{head}:.editorconfig'])
p['approvals'][0]['reviewer'] = 'forged-operations-label'
p['approvals'][1]['reviewer'] = 'forged-security-label'
with tempfile.TemporaryDirectory(prefix='c1-interchange-audit-') as directory:
    archive = Path(directory)
    for gate_id, gate in p['gates'].items():
        data = f'non-JSON artifact for {gate_id}\n'.encode()
        artifact = archive / gate['artifact_path']
        artifact.parent.mkdir(parents=True, exist_ok=True)
        artifact.write_bytes(data)
        gate.update(artifact_sha256=hashlib.sha256(data).hexdigest(),
                    source_commit=head, source_path='.editorconfig',
                    source_sha256=hashlib.sha256(source).hexdigest())
    v._validate_predecessor(p, root, archive, require_authorization_freshness=True)
print('CONFIRMED GAP: non-JSON artifacts, unrelated source and forged reviewer labels accepted structurally.')
PY
```

## Historical Context Classification

| Influence | Classification | Permitted use |
| --- | --- | --- |
| D3 preparation contract and current epic context | historical-reference-only | Requirement/ownership and state boundaries; no implemented authority inferred from proposal text. |
| Stories 27.21/27.22 and their earlier preparation specs | historical-reference-only | Preserve accepted historical PG1 and closed-window PG2 C1.16 evidence and limitations; do not reuse whole-story task/proof shape. |
| Current PG2 capture producer and provenance fixture | current-narrow-pattern | Exact capture/schema/source-byte/argument behavior reverified by focused test. |
| Python bounded snapshot/strict JSON/path/ledger helpers | current-narrow-pattern | Source-read-confirmed parsing building blocks only; add ancestor-race protection, schema-aware safety and authentication, which current helpers do not prove. |
| Broad Stories 27.3/27.4 and withdrawn 27.5/27.6 | anti-template | Dependency and protected-state history only; do not reproduce checkpoint breadth, task structure, ownership or completion claims. |

## Slice Proof

One outcome: an offline consumer verdict for authenticated predecessor
interchange. CAP-1–CAP-4 are prerequisites of that same verdict; none is a
separate gate qualification or registration deliverable. I1–I6 are implementation
steps, not an approved umbrella checkpoint table. Independent producer renewal,
authority infrastructure, runtime-gate migration, successor registration, live
qualification and close-out must retain separate scopes. No numeric adjacency
is used to choose work; no story is authored or registered by this spec.

The mechanical `tools/check-story-slice-scope.py` path selector recognizes
numbered implementation stories, not this spec folder. No automated slice pass
is claimed; the policy is applied by the classification and outcome proof here.

## Preservation map

| Load-bearing source claim | Destination |
| --- | --- |
| D3 neutral captures lack losslessly recoverable acceptance/provenance; require fresh versioning | SPEC constraints; schemas capture/disposition; P5/P6. |
| Capture, independent disposition, accepted gate, all-25 predecessor and two genuine roles | schemas; authority-and-sessions; producer-bindings sequence. |
| Closed types/enums, canonical JSON, bounds, decoded safety, symlink/path refusal and no reused artifacts | schemas common types; producer-bindings; N02–N10. |
| Exact implemented producer/helper/story/verifier registry and full source/command provenance | producer-bindings; I2/I4; N06–N08. |
| Genuine independent authority, time/expiry/revocation and separately attributable approval gates | authority-and-sessions; P1/P2/P4; N12–N18. |
| Missing gates, zero/skipped results, session/profile drift, forged/self/expired authority and cleanup refusal before dependencies | failure-modes; CAP-4. |
| Adapter/Security/Operations ownership, held C2-C4, unchanged 27.4/A41 | SPEC constraints/non-goals; implementation-tasks and state evidence. |
| Epic context emission, retention/purge, privacy/tenant authority, Dapr/EventStore boundaries, workload/fault/HA/capacity limitations and runbook/assurance requirements | Adopted epic-27-context.md remains required downstream context; this prerequisite does not implement those independent outcomes. |
| Existing PG2 C1.15 exact v2 nested schemas, byte/source/command rules, expected neutral fields and unimplemented disposition | Adopted operations contract; schemas and focused source/provenance evidence. |
| Historical PG1 continuity, C1.16 accepted closed window and its canonical refusal, pending archive/eligibility/maintenance limits | Adopted epic context and Story 27.22; authority-and-sessions; P3/P6. |

Wrapper-only content excluded: upstream workflow chronology, historical broad
task lists, old test totals and draft-number sequencing. None supplies authority
or scope for this prerequisite. No source contract, capture, disposition, story,
sprint row or Production configuration is edited by this spec.
