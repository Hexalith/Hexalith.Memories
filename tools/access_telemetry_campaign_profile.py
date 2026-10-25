"""Explicit finite-campaign profile selection for existing Memories consumers.

An unbound Platform candidate cannot be selected. The outer digest is supplied
outside the proposed C1 predecessor, and the full source/measurement/reservation
binding is reopened on every selection. This is capacity admission, not C1
authority or adoption of any historical evidence.
"""
from __future__ import annotations

import hashlib
import importlib.util
import os
from pathlib import Path
from contextlib import ExitStack


def _original_admission():
    """Only the private outside-approved owner plan appoints an issuer/key/ref."""
    from access_telemetry_c1_owner_dispatch import _validated_plan, _open_held_file, _canonical_path
    from access_telemetry_c1_interchange import _artifact_ref
    plan_path = os.environ.get("HEXALITH_C1_OWNER_PLAN")
    if not plan_path:
        return None
    root = _canonical_path(os.environ["HEXALITH_C1_CUSTODY_ROOT"])
    held = []
    with ExitStack() as stack:
        def read(path):
            path = _canonical_path(path)
            if not path.startswith(root + "/"):
                raise ValueError("initial admission lies outside approved custody")
            file = _open_held_file(path, stack, custody_root=root, effective_uid=os.geteuid())
            raw = file.read(); held.append((file, raw))
            return raw
        plan = _validated_plan(read(plan_path), os.environ["HEXALITH_C1_APPROVED_PLAN_SHA256"]).value
        if "initialCapacityAdmission" not in plan:
            return None
        ref = _artifact_ref(plan["initialCapacityAdmission"])
        admission = read(root + "/" + ref.path)
        if len(admission) != ref.byte_length or hashlib.sha256(admission).hexdigest() != ref.sha256:
            raise ValueError("initial admission reference differs")
        key = read(plan["capacityAdmissionKeyPath"])
        result = dict(admission=admission, admission_sha256=ref.sha256,
                      issuer=plan["capacityAdmissionIssuer"], key=key, key_sha256=plan["capacityAdmissionKeySha256"])
        for file, raw in held:
            os.lseek(file.descriptor, 0, os.SEEK_SET)
            if file.read() != raw:
                raise ValueError("original admission custody drifted")
        return result

PATH_ENV = "HEXALITH_CAMPAIGN_PROFILE"
SHA_ENV = "HEXALITH_CAMPAIGN_APPROVED_PROFILE_SHA256"


def selected_campaign_profile(repository_root=None, *, now_ms=None, launch=False, require_original=False):
    path, expected = os.environ.get(PATH_ENV), os.environ.get(SHA_ENV)
    if not path and not expected:
        return None
    from verify_access_telemetry_lifecycle import EvidenceValidationError
    if not path or not expected:
        raise EvidenceValidationError("finite campaign requires its outside-approved profile digest")
    root = Path(repository_root or os.environ.get("HEXALITH_C1_REPOSITORY_ROOT") or Path.cwd()).resolve()
    module_path = root / "references/Hexalith.Platform/eng/helm-qualification/campaign_capacity.py"
    try:
        # Authenticate the external implementation before executing its module
        # body. Its own later source check cannot protect an import-time effect.
        from verify_access_telemetry_lifecycle import _json_without_duplicates
        if Path(path).stat().st_size > 16 * 1024 * 1024 or module_path.stat().st_size > 16 * 1024 * 1024:
            raise ValueError("capacity profile or source exceeds the input bound")
        raw_profile = Path(path).read_bytes()
        if hashlib.sha256(raw_profile).hexdigest() != expected:
            raise ValueError("capacity profile differs from outside approval")
        proposed = _json_without_duplicates(raw_profile.decode("utf-8"), "finite campaign profile")
        sources = [ref for ref in proposed["inputs"]["source"] if ref["path"] == str(module_path.resolve())]
        module_bytes = module_path.read_bytes()
        if len(sources) != 1 or hashlib.sha256(module_bytes).hexdigest() != sources[0]["sha256"]:
            raise ValueError("capacity producer source differs from outside approval")
        spec = importlib.util.spec_from_file_location("hexalith_platform_campaign_capacity", module_path)
        module = importlib.util.module_from_spec(spec)
        exec(compile(module_bytes, str(module_path), "exec"), module.__dict__)
        profile = module.read_bound(path, expected, now_ms=now_ms, check_freshness=False)
        expected_source = {ref["path"]: ref["sha256"] for ref in profile["inputs"]["source"]}
        if expected_source.get(str(module_path.resolve())) != hashlib.sha256(module_path.read_bytes()).hexdigest():
            raise ValueError("capacity producer source is not bound")
        original = _original_admission()
        import time
        current_ms = int(time.time() * 1000) if now_ms is None else now_ms
        if original is not None:
            module.verify_initial_admission(profile, now_ms=current_ms, **original)
        elif require_original or not 0 <= current_ms - profile["measurements"]["observedUtcMs"] <= 86400000:
            raise ValueError("authenticated original fresh admission is required")
        if launch:
            current_path = os.environ.get("HEXALITH_CAMPAIGN_CAPACITY_RECEIPT")
            current_sha = os.environ.get("HEXALITH_CAMPAIGN_APPROVED_CAPACITY_SHA256")
            if not current_path or not current_sha:
                raise ValueError("fresh outside-approved capacity receipt is required")
            module.admit_launch(profile, current_path, current_sha, now_ms=now_ms,
                                original_admission=original, require_original=require_original)
            validate_campaign_environment_binding(profile)
        return profile
    except (OSError, ValueError, TypeError, KeyError, AttributeError, ImportError) as error:
        raise EvidenceValidationError("finite campaign profile admission refused") from error


def active_profile(repository_root=None):
    from verify_access_telemetry_lifecycle import (
        AdapterProfile, ADR_TWO_WRITER_WORKLOAD, canonical_pg_onprem_2_profile,
    )
    selected = selected_campaign_profile(repository_root)
    if selected is None:
        return canonical_pg_onprem_2_profile()
    # Exact file bytes were authenticated by Platform's admission above.
    import json
    def retained(group):
        ref = selected["inputs"][group][0]
        raw = Path(ref["path"]).read_bytes()
        if hashlib.sha256(raw).hexdigest() != ref["sha256"]:
            from verify_access_telemetry_lifecycle import EvidenceValidationError
            raise EvidenceValidationError("finite campaign input changed during selection")
        return json.loads(raw)
    runtime, backend = retained("runtimeImages"), retained("backendImages")
    return AdapterProfile(
        identity={"profileId": selected["profileId"], "kubeContext": selected["target"]["context"],
                  "kubeNamespace": selected["target"]["namespace"], "componentType": "state.postgresql/v2",
                  "maxConns": "40", "runtimeImage": runtime["image"],
                  "runtimeLinuxAmd64Manifest": runtime["linuxAmd64Manifest"],
                  "runtimeVersion": runtime["version"], "postgresqlImage": backend["postgresql"],
                  "postgresqlLinuxAmd64Manifest": backend["postgresqlLinuxAmd64Manifest"],
                  "finiteCampaign": selected},
        capabilities=canonical_pg_onprem_2_profile().capabilities.copy(),
        workload=ADR_TWO_WRITER_WORKLOAD.to_dict(),
    )


def active_profile_sha256(repository_root=None):
    return active_profile(repository_root).manifest()["profile_sha256"]


def active_profile_id(repository_root=None):
    return active_profile(repository_root).identity["profileId"]


def active_runtime_image():
    return active_profile().identity["runtimeImage"]


def active_runtime_manifest():
    return active_profile().identity["runtimeLinuxAmd64Manifest"]


def active_runtime_version():
    return active_profile().identity.get("runtimeVersion", "1.18.1")


def validate_campaign_runtime_binding(value):
    """Bind the observed late-bound ConfigMap to the exact canonical identity."""
    from verify_access_telemetry_lifecycle import EvidenceValidationError, _canonical_json
    campaign = selected_campaign_profile()
    if campaign is None:
        raise EvidenceValidationError("finite campaign runtime binding requires its selected profile")
    data = {"profileId": campaign["profileId"], "profileSha256": active_profile_sha256(),
            "capacityProfileSha256": hashlib.sha256(_canonical_json(campaign).encode()).hexdigest()}
    if (not isinstance(value, dict) or value.get("kind") != "ConfigMap" or value.get("apiVersion") != "v1"
            or value.get("metadata", {}).get("name") != "access-telemetry-campaign-identity"
            or value.get("metadata", {}).get("namespace") != campaign["target"]["namespace"]
            or value.get("data") != data):
        raise EvidenceValidationError("finite campaign runtime binding differs from approved profile")
    return data


def validate_campaign_environment_binding(campaign):
    """Require the supported runner's ready descriptor and exact physical readback.

    These additional outside-approved anchors grant no C1 or producer permission.
    The existing owner authority separately verifies the same physical target,
    source, session, current status and all 25 gates before any mutation.
    """
    import json
    import subprocess
    import time
    from verify_access_telemetry_lifecycle import EvidenceValidationError, _json_without_duplicates
    try:
        pair = None
        pair_path = os.environ.get('HEXALITH_CAMPAIGN_OBSERVATION')
        pair_sha = os.environ.get('HEXALITH_CAMPAIGN_APPROVED_OBSERVATION_SHA256')
        if pair_path or pair_sha:
            path = Path(pair_path or '')
            if not pair_sha or not path.is_absolute() or path.is_symlink() or path.resolve() != path or path.stat().st_size > 1048576:
                raise ValueError('invalid observation pair')
            raw = path.read_bytes()
            if hashlib.sha256(raw).hexdigest() != pair_sha:
                raise ValueError('observation pair changed')
            pair = _json_without_duplicates(raw.decode(), 'campaign observation pair')
            if set(pair) != {'schema', 'runId', 'observedUtcMs', 'descriptor', 'readback'} or pair['schema'] != 'hexalith.platform.campaign-observation/v1':
                raise ValueError('invalid observation pair shape')
        def read(name):
            path = Path(pair[name.lower()]['path'] if pair else os.environ['HEXALITH_CAMPAIGN_' + name])
            expected = pair[name.lower()]['sha256'] if pair else os.environ['HEXALITH_CAMPAIGN_APPROVED_' + name + '_SHA256']
            if path.is_symlink() or path.resolve() != path or path.stat().st_size > 1048576:
                raise ValueError('invalid lifecycle artifact')
            raw = path.read_bytes()
            if hashlib.sha256(raw).hexdigest() != expected:
                raise ValueError('lifecycle artifact changed')
            return path, _json_without_duplicates(raw.decode(), 'campaign lifecycle artifact'), expected
        descriptor_path, descriptor, descriptor_sha = read('DESCRIPTOR')
        _, readback, _ = read('READBACK')
        if pair and (pair['runId'] != readback['runId'] or pair['observedUtcMs'] != readback['observedUtcMs']):
            raise ValueError('observation pair binding differs')
        bindings = descriptor['bindings']
        if (readback['schema'] != 'hexalith.platform.campaign-readback/v1' or readback['qualified'] is not False
                or readback['descriptorSha256'] != descriptor_sha
                or readback['profileSha256'] != active_profile_sha256()
                or readback['capacityProfileSha256'] != hashlib.sha256(json.dumps(campaign, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()
                or readback['planSha256'] != os.environ['HEXALITH_CAMPAIGN_APPROVED_LIFECYCLE_PLAN_SHA256']
                or bindings['run']['runId'] != readback['runId'] or bindings['run']['ownerRunId'] != readback['runId']
                or bindings['environment']['composition']['contentIdentity'] != 'sha256:' + readback['planSha256']
                or bindings['environment']['readiness']['status'] != 'ready'
                or not 0 <= int(time.time() * 1000) - readback['observedUtcMs'] <= 60000):
            raise ValueError('lifecycle binding differs')
        # The already outside-approved owner plan binds actual API/CA/cluster and
        # namespace identities. A proposed descriptor cannot appoint its target.
        from access_telemetry_c1_owner_dispatch import _validated_plan
        plan = _validated_plan(Path(os.environ['HEXALITH_C1_OWNER_PLAN']).read_bytes(), os.environ['HEXALITH_C1_APPROVED_PLAN_SHA256']).value
        if any(readback['target'][key] != plan['target'][key] for key in ('apiServer', 'apiCaSha256', 'clusterUid', 'namespaceUid')):
            raise ValueError('physical target differs')
        ref = campaign['inputs']['tool'][0]
        raw = Path(ref['path']).read_bytes()
        if hashlib.sha256(raw).hexdigest() != ref['sha256']:
            raise ValueError('tool input changed')
        tool = json.loads(raw)
        for selected in [*tool['tools'].values(), *tool['toolFiles']]:
            with Path(selected['path']).open('rb') as stream:
                if hashlib.file_digest(stream, 'sha256').hexdigest() != selected['sha256']:
                    raise ValueError('descriptor validator changed')
        result = subprocess.run([tool['tools']['dotnet']['path'], tool['tools']['buildsCli']['path'], 'validate', '--environment', str(descriptor_path), '--output', 'json'],
                                stdin=subprocess.DEVNULL, capture_output=True, timeout=30, check=False)
        if result.returncode != 0 or len(result.stdout) > 1048576 or json.loads(result.stdout).get('status') != 'validated':
            raise ValueError('supported descriptor reader refused')
        return readback
    except (OSError, ValueError, TypeError, KeyError, subprocess.SubprocessError) as error:
        raise EvidenceValidationError('finite campaign environment admission refused') from error
