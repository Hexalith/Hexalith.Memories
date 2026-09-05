"""Closed, source-bound Story 27.4 qualification scenario producer."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
from typing import Any, Mapping, Sequence
from urllib.parse import quote

from verify_access_telemetry_lifecycle import (
    REQUIRED_FAILURE_SCENARIOS,
    REQUIRED_REPLACEMENTS,
    STORY_27_4_PROFILE_SHA256,
    STORY_27_4_WORKLOAD_SHA256,
    EvidenceValidationError,
    _canonical_json,
    _json_without_duplicates,
    _require_bool,
    _require_exact_fields,
    _require_integer,
    _require_mapping,
    _require_nonempty_string,
    _require_nonzero_integer,
    _run_bounded_process,
    _sha256,
    _utc_now_milliseconds,
    _validate_secret_safe,
)


_TARGET_FIELDS = frozenset({"kind", "kube_context", "namespace", "profile_sha256"})
_SAFE_TARGET = re.compile(r"\A[a-zA-Z0-9][a-zA-Z0-9._@/-]{0,127}\Z")


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scenario-input", required=True)
    parser.add_argument("--disable-only", action="store_true")
    return parser


def _load_target(path: Path) -> Mapping[str, str]:
    if path.is_symlink() or not path.is_file() or path.stat().st_size > 16_384:
        raise EvidenceValidationError("scenario input must be one small regular file")
    payload = path.read_bytes()
    if payload.startswith(b"\xef\xbb\xbf"):
        raise EvidenceValidationError("scenario input must not contain a UTF-8 BOM")
    try:
        parsed = _require_mapping(
            _json_without_duplicates(payload.decode("utf-8", errors="strict"), str(path)),
            "scenario input",
        )
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError("scenario input is not canonical UTF-8") from exc
    _validate_secret_safe(parsed, "scenario input")
    _require_exact_fields(parsed, frozenset({"schema_version", "target"}), "scenario input")
    if _require_integer(parsed["schema_version"], "scenario input.schema_version", minimum=1, maximum=1) != 1:
        raise EvidenceValidationError("scenario input schema must be 1")
    target = _require_mapping(parsed["target"], "scenario input.target")
    _require_exact_fields(target, _TARGET_FIELDS, "scenario input.target")
    normalized = {
        key: _require_nonempty_string(target[key], f"target.{key}", maximum=128)
        for key in _TARGET_FIELDS
    }
    if normalized["kind"] != "non-production-qualification":
        raise EvidenceValidationError("Story 27.4 producers run only in non-Production qualification")
    if normalized["profile_sha256"] != STORY_27_4_PROFILE_SHA256:
        raise EvidenceValidationError("scenario target profile differs from PG-ONPREM-1")
    if normalized["namespace"] == "hexalith-memories" or not normalized["namespace"].endswith("-qualification"):
        raise EvidenceValidationError("scenario namespace is not the isolated qualification namespace")
    for name in ("kube_context", "namespace"):
        if _SAFE_TARGET.fullmatch(normalized[name]) is None:
            raise EvidenceValidationError(f"target.{name} is not a bounded Kubernetes identity")
    return normalized


def _operation_path(namespace: str, checkpoint: str, command_id: str) -> str:
    del namespace
    return (
        "http://127.0.0.1:3500/v1.0/invoke/memories-access-telemetry/method/"
        f"operations/qualification/{quote(checkpoint, safe='-')}/{quote(command_id, safe='-')}"
    )


def _run_operation(
    target: Mapping[str, str],
    checkpoint: str,
    command_id: str,
) -> tuple[Mapping[str, Any], dict[str, Any]]:
    arguments = {
        "kube_context": target["kube_context"],
        "namespace": target["namespace"],
        "operation": command_id,
    }
    started = _utc_now_milliseconds()
    return_code, stdout, stderr = _run_bounded_process(
        (
            "kubectl",
            "--context",
            target["kube_context"],
            "--namespace",
            target["namespace"],
            "exec",
            "deployment/memories",
            "-c",
            "memories",
            "--",
            "sh",
            "-ec",
            (
                "wget -qO- --header=\"dapr-api-token: ${DAPR_API_TOKEN}\" "
                "--post-data='' " + _operation_path(target["namespace"], checkpoint, command_id)
            ),
        ),
        cwd=Path.cwd(),
        timeout_seconds=300,
    )
    finished = _utc_now_milliseconds()
    if return_code != 0:
        raise EvidenceValidationError(
            f"command {command_id} exited {return_code}; stderr_sha256={hashlib.sha256(stderr).hexdigest()}"
        )
    try:
        result = _require_mapping(
            _json_without_duplicates(stdout.decode("utf-8", errors="strict"), command_id),
            command_id,
        )
    except UnicodeDecodeError as exc:
        raise EvidenceValidationError(f"command {command_id} returned invalid UTF-8") from exc
    _validate_secret_safe(result, command_id)
    result_count = _require_nonzero_integer(result.get("result_count"), f"{command_id}.result_count")
    observation = {
        "command_id": command_id,
        "arguments": arguments,
        "arguments_sha256": _sha256(_canonical_json(arguments)),
        "started_utc_ms": started,
        "finished_utc_ms": finished,
        "exit_code": return_code,
        "stdout_sha256": hashlib.sha256(stdout).hexdigest(),
        "stderr_sha256": hashlib.sha256(stderr).hexdigest(),
        "result_count": result_count,
    }
    return result, observation


def _result_observation(command: Mapping[str, Any]) -> dict[str, Any]:
    return {
        "command_id": command["command_id"],
        "output_sha256": command["stdout_sha256"],
        "result_count": command["result_count"],
    }


def _without_result_count(value: Mapping[str, Any]) -> dict[str, Any]:
    result = dict(value)
    result.pop("result_count", None)
    return result


def _transition(
    enable: Mapping[str, Any],
    disable: Mapping[str, Any],
    final: Mapping[str, Any],
) -> dict[str, Any]:
    if enable.get("state") != "enabled" or disable.get("state") != "disabled" or final.get("state") != "disabled":
        raise EvidenceValidationError("qualification transition did not finish disabled")
    return {
        "non_production": True,
        "enable_observation": _result_observation(enable["_command"]),
        "disable_observation": _result_observation(disable["_command"]),
        "final_observation": _result_observation(final["_command"]),
        "final_writes_state": "disabled",
    }


def _execute(
    target: Mapping[str, str],
    checkpoint: str,
    command_ids: Sequence[str],
) -> tuple[dict[str, Mapping[str, Any]], list[dict[str, Any]]]:
    results: dict[str, Mapping[str, Any]] = {}
    commands: list[dict[str, Any]] = []
    for command_id in command_ids:
        result, command = _run_operation(target, checkpoint, command_id)
        mutable = dict(result)
        mutable["_command"] = command
        results[command_id] = mutable
        commands.append(command)
    return results, commands


def _execute_qualification(
    target: Mapping[str, str],
    checkpoint: str,
    command_ids: Sequence[str],
) -> tuple[dict[str, Mapping[str, Any]], list[dict[str, Any]]]:
    """Run one qualification session and always restore/verify its disabled state."""

    results: dict[str, Mapping[str, Any]] = {}
    commands: list[dict[str, Any]] = []
    try:
        # Enter the protected region before invoking enable.  The target may
        # apply the transition and then return malformed/truncated evidence; a
        # parse failure in that window must still trigger disable restoration.
        enable, enable_command = _run_operation(target, checkpoint, "qualification-enable")
        enable = {**enable, "_command": enable_command}
        results["qualification-enable"] = enable
        commands.append(enable_command)
        if enable.get("state") != "enabled":
            raise EvidenceValidationError("qualification target did not enter the enabled state")
        body_results, body_commands = _execute(target, checkpoint, command_ids)
        results.update(body_results)
        commands.extend(body_commands)
    finally:
        # The confirmation is attempted even if the disable command itself fails. The
        # outer verifier performs one further fail-safe disable attempt when this
        # producer exits nonzero, but producer-owned cleanup does not depend on it.
        try:
            disable, disable_command = _run_operation(target, checkpoint, "qualification-disable")
            results["qualification-disable"] = {**disable, "_command": disable_command}
            commands.append(disable_command)
        finally:
            final, final_command = _run_operation(target, checkpoint, "qualification-final-state")
            results["qualification-final-state"] = {**final, "_command": final_command}
            commands.append(final_command)
    return results, commands


def _c2(target: Mapping[str, str]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = [
        "writer-1",
        "writer-2",
        *[f"replace-{name}" for name in REQUIRED_REPLACEMENTS],
        "approved-adapter-fault",
        "continuity",
    ]
    results, commands = _execute_qualification(target, "c2-production-replacement", command_ids)
    writers = []
    for index in (1, 2):
        result = _without_result_count(results[f"writer-{index}"])
        result.pop("_command", None)
        result["observation"] = _result_observation(results[f"writer-{index}"]["_command"])
        writers.append(result)
    replacements = {}
    for name in REQUIRED_REPLACEMENTS:
        value = _without_result_count(results[f"replace-{name}"])
        value.pop("_command", None)
        value["observation"] = _result_observation(results[f"replace-{name}"]["_command"])
        replacements[name] = value
    adapter = _without_result_count(results["approved-adapter-fault"])
    adapter.pop("_command", None)
    adapter["observation"] = _result_observation(results["approved-adapter-fault"]["_command"])
    continuity = results["continuity"]
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    return {
        "writers": {
            "steady_state_minutes": 30,
            "cluster_accepted_records_per_second": 250,
            "component_operations_per_second": continuity.get("component_operations_per_second"),
            "writer_results": writers,
            "acknowledged_loss": continuity.get("acknowledged_loss"),
            "actor_serialized": continuity.get("actor_serialized"),
            "idempotent_retry": continuity.get("idempotent_retry"),
            "conflict_rejected": continuity.get("conflict_rejected"),
            "transaction_acknowledged": continuity.get("transaction_acknowledged"),
            "reconstructed": continuity.get("reconstructed"),
            "reconnected": continuity.get("reconnected"),
            "direct_backend_dependencies": continuity.get("direct_backend_dependencies"),
        },
        "replacements": replacements,
        "adapter_fault": adapter,
        "console_continuity": continuity.get("console_continuity"),
        "otlp_configured": continuity.get("otlp_configured"),
        "otlp_continuity": continuity.get("otlp_continuity"),
        "continuity_observation": _result_observation(continuity["_command"]),
        "qualification_transition": _transition(enable, disable, final),
    }, commands


def _c3(target: Mapping[str, str]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = ["retention-controls"]
    for hours in (1, 24, 168):
        command_ids.extend((f"cohort-{hours}h-expiry", f"cohort-{hours}h-purge", f"cohort-{hours}h-reclamation"))
    results, commands = _execute_qualification(target, "c3-retention-reclamation", command_ids)
    cohorts = []
    for hours in (1, 24, 168):
        merged: dict[str, Any] = {}
        for stage in ("expiry", "purge", "reclamation"):
            command_id = f"cohort-{hours}h-{stage}"
            partial = _without_result_count(results[command_id])
            partial.pop("_command", None)
            for key, value in partial.items():
                if key in merged and merged[key] != value:
                    raise EvidenceValidationError(f"cohort {hours}h returned inconsistent {key}")
                merged[key] = value
            merged[f"{stage}_observation"] = _result_observation(results[command_id]["_command"])
        cohorts.append(merged)
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    retention = _without_result_count(results["retention-controls"])
    retention.pop("_command", None)
    return {
        "retention": retention,
        "retention_observation": _result_observation(results["retention-controls"]["_command"]),
        "cohorts": cohorts,
        "qualification_transition": _transition(enable, disable, final),
    }, commands


def _c4(target: Mapping[str, str]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    command_ids = [
        *[f"failure-{name}" for name in REQUIRED_FAILURE_SCENARIOS],
        "observability",
        "privacy-denial",
    ]
    results, commands = _execute_qualification(target, "c4-failure-privacy-observability", command_ids)
    failures = {}
    for name in REQUIRED_FAILURE_SCENARIOS:
        command_id = f"failure-{name}"
        value = _without_result_count(results[command_id])
        value.pop("_command", None)
        value["observation"] = _result_observation(results[command_id]["_command"])
        failures[name] = value
    observability = _without_result_count(results["observability"])
    observability.pop("_command", None)
    observability["observation"] = _result_observation(results["observability"]["_command"])
    privacy = _without_result_count(results["privacy-denial"])
    privacy.pop("_command", None)
    privacy["observation"] = _result_observation(results["privacy-denial"]["_command"])
    enable = results["qualification-enable"]
    disable = results["qualification-disable"]
    final = results["qualification-final-state"]
    return {
        "failure_scenarios": failures,
        "observability": observability,
        "privacy": privacy,
        "qualification_transition": _transition(enable, disable, final),
    }, commands


def run(checkpoint: str, argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        target = _load_target(Path(args.scenario_input))
        if args.disable_only:
            disable: Mapping[str, Any] | None = None
            final: Mapping[str, Any] | None = None
            try:
                disable, _ = _run_operation(target, checkpoint, "qualification-disable")
            finally:
                final, _ = _run_operation(target, checkpoint, "qualification-final-state")
            return 0 if disable.get("state") == "disabled" and final.get("state") == "disabled" else 1
        factory = {
            "c2-production-replacement": _c2,
            "c3-retention-reclamation": _c3,
            "c4-failure-privacy-observability": _c4,
        }[checkpoint]
        results, commands = factory(target)
        payload = {
            "schema_version": 1,
            "checkpoint": checkpoint,
            "profile_sha256": STORY_27_4_PROFILE_SHA256,
            "workload_sha256": STORY_27_4_WORKLOAD_SHA256,
            "owner": "hexalith-platform-operations",
            "failure_count": 0,
            "skip_count": 0,
            "failures": [],
            "skipped": [],
            "commands": commands,
            "result_count": sum(command["result_count"] for command in commands),
            "results": results,
        }
        print(_canonical_json(payload))
        return 0
    except (EvidenceValidationError, OSError, ValueError, KeyError) as exc:
        print(str(exc)[:512], file=sys.stderr)
        return 1
