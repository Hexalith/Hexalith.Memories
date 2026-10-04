# C1 and Security Prerequisite Planning Verification

**Date:** 2026-10-04  
**Source revision:** `47027d35a1f4a2c6986bec85b5cae601ce1b201e`  
**Evidence scope:** Local planning/source/fixture checks only; no target contacted.

## Current-State Claim Audit (refreshed after approved handoff)

Planning baseline results below are historical. Current source recheck is against
`5b43fe2f8a0f04dc021921a077dff1a573c2ce5e` and the changed runner.
Future successor producer behavior and profile adoption remain intent.
They are not asserted as current implementation, acceptance or deployed state.

| Quoted claim | Class | Re-runnable command / evidence | Observed | Verdict |
| :----------- | :---- | :---------------------------- | :------- | :------ |
| "The remaining twenty-four C1 gates stay held without a registered owner." | Quantitative/existence | Allocation audit below; exact narrative in `_bmad-output/implementation-artifacts/epic-27-context.md` | Approved allocation has 25 distinct rows; only its existing identity story has a canonical implementation file. | confirmed |
| "The runner accepts C1.15 and historical-opt-in C1.16 on PG-ONPREM-1" | Behavioral | `rg -n -e "ValidateSet" -e "AllowHistoricalProfileCapture" tools/verify-access-telemetry-c1.ps1`; preparation spec fixture results | Approved historical preparation is implemented; missing opt-in and successor mode are rejected before calls/writes. | confirmed |
| "PG-ONPREM-1" is the runner's sole supported profile | Existence/behavior | `rg -n -F "ValidateSet('PG-ONPREM-1')" tools/verify-access-telemetry-c1.ps1`; unsupported-profile fixture below | The proposed successor is not accepted today. | confirmed |
| "Story 27.21 is done" | Existence/state | `rg -n '^  27-21-runtime-and-control-plane-identity: done$' _bmad-output/implementation-artifacts/sprint-status.yaml`; current story and compiled context | Tracker and current completion record agree. Historical prose is not used as current status authority. | confirmed |
| "gateStatus: not-evaluated" and "productionGatePassed: false" | Behavior | `rg -n -e "gateStatus = 'not-evaluated'" -e 'productionGatePassed = \$false' tools/verify-access-telemetry-c1.ps1` | Capture success grants no automatic Production gate credit. | confirmed |
| "PostgreSQL 18.4 is part of the immutable PG-ONPREM-1 identity" | Existence/behavior | Canonical profile read below; `rg -n 'postgresql-18.4|postgres:18.4' tools/verify_access_telemetry_lifecycle.py` | The current profile contains 18.4 image/version material and has the recorded hash. | confirmed |
| Rejects "C1 predecessor profile differs from PG-ONPREM-1" | Behavioral contract | Read `_validate_predecessor` in `tools/verify_access_telemetry_lifecycle.py` and its caller `run_story_27_4_producer_checkpoint` | The validator requires the current exact profile hash, unique artifact path/hash, canonical source identities, two different reviewer labels and gate freshness when requested. It does not authenticate reviewer authority or artifact semantics; see the D3 contract. | confirmed |
| "OpenBao 2.6.0 / chart 0.28.5" | Existence/configuration | `rg -n -e '0.28.5' -e '2.6.0@sha256' deploy/openbao/values.yaml`; pinned image in `tools/production-deployment-openbao.ps1` | Both repository image consumers use 2.6.0; the chart header is 0.28.5. This is repository input, not a new live observation. | confirmed |
| "ACCESS_TELEMETRY_ENABLED=false" | Configuration | `rg -n -F 'ACCESS_TELEMETRY_ENABLED=false' deploy/kubernetes/overlays/production/kustomization.yaml` | Production provider remains statically disabled. | confirmed |
| "replicas: 0" | Configuration/count | `rg -c '^  replicas: 0$' deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` | Both lifecycle/clock Deployment patches specify zero; no live replica state inferred. | confirmed |
| "state disabled" and "holderIdentity empty" | Configuration | Read `deploy/kubernetes/overlays/qualification/qualification-gate.yaml` | Gate is disabled and Lease holder empty in source; no current-cluster cleanup receipt inferred. | confirmed |
| "NFR34" requires fail-closed qualification and non-blocking admitted delivery | Requirement location | `sed -n '1198,1208p' _bmad-output/planning-artifacts/prd.md` | Current requirement preserves ownership, TTL, purge, erasure, recovery, sanitization and degradation without changing domain truth or audit assurance. | confirmed |

No `corrected` claim is used to justify an authored slice without correcting its
source. The proposal separately identifies historical/stale epic wording and
provides the future dated correction; current facts above use the current tracker,
source and completed story instead.

## Re-runnable Allocation, Draft and Link Audit

Run from the repository root:

```bash
python3 - <<'PY'
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path

root = Path.cwd()
base = root / '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04'
source = (root / '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md').read_text()
approved = {}
for line in source.splitlines():
    match = re.match(r'^\| (27\.\d+) \| (C1\.\d+) — ([^|]+) \| ([^|]+) \| ([^|]+) \|$', line)
    if match:
        story, gate, outcome, owner, observation = [part.strip() for part in match.groups()]
        assert gate not in approved
        approved[gate] = (story, outcome, owner, observation)
assert set(approved) == {f'C1.{i}' for i in range(1, 26)}
rows = json.loads((base / 'candidate-stories.json').read_text())
assert len(rows) == 24
assert {row['gate_id'] for row in rows} == set(approved) - {'C1.15'}
assert len({row['story_key'] for row in rows}) == 24
assert len(list((base / 'stories').glob('*.md'))) == 24
index = (base / 'README.md').read_text()
with tempfile.TemporaryDirectory(prefix='c1-story-record-check-') as temporary:
    preview = Path(temporary)
    paths = []
    for row in rows:
        gate = row['gate_id']
        assert (row['story_id'], row['outcome'], row['owner'], row['observation']) == approved[gate]
        path = base / row['draft_path']
        text = path.read_text()
        assert 'status: draft\n' in text and 'registration: held\n' in text
        assert f"gate_id: '{gate}'" in text
        assert f"accountable_role: '{row['owner']}'" in text
        assert '**Given**' in text and '**when**' in text and '**then**' in text
        assert '### Epic AC Verification' in text
        assert '### Historical Context Classification' in text and '### Slice Proof' in text
        assert f"{row['draft_path']})" in index
        key = row['story_id'].replace('.', '-')
        assert not list((root / '_bmad-output/implementation-artifacts').glob(key + '-*.md'))
        assert (root / row['planned_fixture']).exists() == (gate == 'C1.16')
        relative = '_bmad-output/implementation-artifacts/' + row['story_key'] + '.md'
        target = preview / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
        paths.append(relative)
    changed = preview / 'changed-stories.txt'
    changed.write_text('\n'.join(paths) + '\n')
    command = ['python3', str(root / 'tools/check-story-slice-scope.py'),
               '--repo-root', str(preview), '--require-record',
               '--changed-files-file', str(changed)]
    result = subprocess.run(command, check=True, capture_output=True, text=True)
    assert '24 story file(s) checked' in result.stdout
    print(result.stdout)
for path in base.rglob('*.md'):
    data = path.read_bytes()
    assert b'\n' not in data.replace(b'\r\n', b'')
    for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', path.read_text()):
        if not target.startswith(('https://', 'http://')):
            assert (path.parent / target.split('#')[0]).exists(), (path, target)
print('PASS: 24 exact allocation drafts, 24 required records, local links and CRLF.')
PY
```

The temporary preview gives the existing CLI its canonical story paths without
creating any registered files in the workspace. `--require-record` is enabled;
the output must say 24 files checked, never no-op. This validates the mechanically
checkable records only. It deliberately does not prove that proposed modes exist,
that classifiers are semantically sufficient, or that any live gate passed.

## Canonical Profile Audit

```bash
PYTHONDONTWRITEBYTECODE=1 python3 - <<'PY'
import sys
from pathlib import Path
sys.path.insert(0, str(Path('tools').resolve()))
import verify_access_telemetry_lifecycle as lifecycle
assert lifecycle.STORY_27_4_PROFILE_SHA256 == 'dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14'
assert lifecycle.STORY_27_4_WORKLOAD_SHA256 == '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
assert (lifecycle.PROFILE_CAPACITY_BYTES, lifecycle.STEADY_STATE_CAPACITY_BYTES,
        lifecycle.CRITICAL_CAPACITY_BYTES, lifecycle.UNHEALTHY_CAPACITY_BYTES) == (
    429496729600, 300647710720, 343597383680, 386547056640)
print('PASS: current profile/workload hashes and exact capacity thresholds.')
PY
```

## Initial Planning Results (before implementation handoff)

| Check | Command / result |
| :---- | :--------------- |
| Unsupported gate/profile denial | `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'runtime_control_plane_identity_test.py' -k unsupported_ -v` — exit 0; 2/2 passed in 5.097 seconds. Both assert no producer calls, no packets and no evidence directory. |
| Candidate allocation and records | The allocation/draft/link audit above — exit 0; all 24 exact approved allocations and required story records passed. All local Markdown links resolved and CRLF checks passed. |
| Profile and admission identity | Canonical profile audit above — exit 0; exact current profile/workload hashes and all four byte thresholds confirmed. |
| Whitespace | `git diff --check` — exit 0. Separate `git diff --no-index --check /dev/null <path>` inspection of each of the 29 new planning files reported no whitespace errors. |
| Existing work preservation | SHA-256 comparison of all 6,024 tracked regular files against the initial planning worktree snapshot — zero content changes. Registration, Story 27.4, producer and profile/security inputs remained byte-identical. |

The baseline revision identifies the source at planning start. Concurrent work
advanced HEAD while this plan was authored; the content audit compares the observed
initial and final worktree bytes, including pre-existing user edits. This effort
created only the 29 new planning files and performed no Git publication.

## Primary Release Evidence

- [OpenBao 2.6.4](https://github.com/openbao/openbao/releases/tag/v2.6.4): a current 2.6.x patch containing security fixes; proposed, not locally qualified.
- [OpenBao chart 0.29.6](https://github.com/openbao/openbao-helm/releases/tag/openbao-0.29.6): published chart default is OpenBao 2.6.3; candidate server override must be rendered and tested.
- [OpenBao chart 0.30.0](https://github.com/openbao/openbao-helm/releases/tag/openbao-0.30.0): available alternative with a documented standalone-storage default change, not asserted to affect this Raft deployment.
- [PostgreSQL 18.6](https://www.postgresql.org/docs/release/18.6/): published minor upgrade from 18.4, with release-specific configuration/data/extension applicability to assess.

These observations support version choices, not a claim of deployed posture,
qualified compatibility or complete security remediation. Exact public artifact identities and inactive render bytes are now retained in
[the candidate package](profile-candidate/README.md); target qualification remains
required before acceptance. No target, credential value, tenant payload or raw backend secret
was read while authoring the plan.

## Approved Handoff Recheck

[Implementation handoff](implementation-handoff.md), [exact profile verification](profile-candidate/verification.md)
and [capture preparation spec](../../implementation-artifacts/spec-c1-16-component-backend-capture-preparation.md)
record the actual current work. The allocation audit now expects the C1.16 fixture
to exist and the other 23 planned fixtures to remain absent; all 24 canonical story
files remain absent. The final suite passed all 55 methods in 833.295 seconds:
34 C1.15, 15 C1.16 and 6 candidate-verifier methods, with zero failures/errors/skips.
The separate required five-method compatibility/denial check passed in 76.303
seconds. The independent review patched 13 finding reports and deferred two
inherited shared-probe limitations; each verdict and resolution remains in the
preparation spec. No actual gate pass, registration or security requalification
was inferred.

[Final verification evidence](final-verification-evidence.json) retains five exact
raw logs in base64, their hashes and executed method names, final source hashes,
normal/optimized candidate-check receipts and the preservation result. Decode
original bytes before hashing. All 6,021 protected tracked files and the previous
ledger prefix remain byte-identical to the initial worktree; only the two owned
collector/regression files and the appended deferred entries changed.
All 24 draft preview records, the one actual freeform preparation record, local
links and repository line endings passed. No Git publication was performed.
