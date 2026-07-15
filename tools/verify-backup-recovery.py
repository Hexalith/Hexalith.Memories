#!/usr/bin/env python3
"""Verify Redis/FalkorDB recovery evidence against a consolidated tenant export."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from collections.abc import Callable, Sequence
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


CommandRunner = Callable[[Sequence[str]], str]


class VerificationError(RuntimeError):
    """Raised when recovery evidence is incomplete or contradicts the export."""


def run_command(command: Sequence[str]) -> str:
    """Run one evidence command and return stdout, failing closed on any error."""

    completed = subprocess.run(
        list(command),
        check=False,
        capture_output=True,
        text=True,
    )
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip() or "no diagnostic output"
        raise VerificationError(f"command failed ({' '.join(command[:4])} ...): {detail}")
    return completed.stdout.strip()


def export_summary(export_path: Path, runner: CommandRunner = run_command) -> dict[str, Any]:
    """Read only the manifest/statistics projection through jq."""

    if not export_path.is_file():
        raise VerificationError(f"export file does not exist: {export_path}")

    expression = (
        "[.manifest.tenantId,.manifest.scope,.statistics.memoryUnitCount,"
        ".statistics.edgeCount,.statistics.caseCount] | @tsv"
    )
    raw = runner(["jq", "-er", expression, str(export_path)])
    fields = raw.split("\t")
    if len(fields) != 5:
        raise VerificationError("export manifest/statistics projection was unreadable")

    tenant_id, scope, memory_units_text, exported_edges_text, cases_text = fields
    if not tenant_id or scope != "tenant":
        raise VerificationError("verification requires a consolidated tenant-scope export")
    try:
        memory_units = int(memory_units_text)
        exported_edges = int(exported_edges_text)
        cases = int(cases_text)
    except ValueError as error:
        raise VerificationError("export statistics must be integers") from error
    if min(memory_units, exported_edges, cases) < 0:
        raise VerificationError("export statistics cannot be negative")

    return {
        "tenantId": tenant_id,
        "scope": scope,
        "memoryUnitCount": memory_units,
        "exportedEdgeCount": exported_edges,
        "caseCount": cases,
    }


def sha256_file(path: Path) -> str:
    """Hash an export without materializing it in memory."""

    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest().upper()


def parse_persistence_info(raw: str, workload: str) -> dict[str, str]:
    """Require exact healthy persistence fields from Redis INFO output."""

    fields: dict[str, str] = {}
    for line in raw.replace("\r", "").splitlines():
        if ":" in line and not line.startswith("#"):
            key, value = line.split(":", 1)
            fields[key] = value

    expected = {
        "loading": "0",
        "aof_enabled": "1",
        "aof_last_write_status": "ok",
        "aof_last_bgrewrite_status": "ok",
    }
    mismatches = [f"{key}={fields.get(key)!r}, expected {value!r}" for key, value in expected.items() if fields.get(key) != value]
    if mismatches:
        raise VerificationError(f"{workload} persistence is not healthy: {', '.join(mismatches)}")
    return {key: fields[key] for key in expected}


def parse_count(raw: str, label: str) -> int:
    """Parse a non-negative integer result, tolerating RedisGraph headers."""

    for line in raw.replace("\r", "").splitlines():
        candidate = line.strip().strip('"')
        if candidate.isdigit():
            return int(candidate)
    raise VerificationError(f"{label} did not return a non-negative integer")


def verify_recovery(
    *,
    namespace: str,
    tenant_id: str,
    export_path: Path,
    kubectl: str = "kubectl",
    runner: CommandRunner = run_command,
) -> dict[str, Any]:
    """Collect and validate one tenant's physical recovery evidence."""

    if not namespace.strip() or not tenant_id.strip():
        raise VerificationError("namespace and tenant id are required")

    summary = export_summary(export_path, runner)
    if summary["tenantId"] != tenant_id:
        raise VerificationError(
            f"export tenant {summary['tenantId']!r} does not match requested tenant {tenant_id!r}"
        )

    def kube(*arguments: str) -> str:
        return runner([kubectl, "-n", namespace, *arguments])

    redis_pvc = kube("get", "pvc", "data-redis-stack-0", "-o", "jsonpath={.status.phase}")
    falkor_pvc = kube("get", "pvc", "data-falkordb-0", "-o", "jsonpath={.status.phase}")
    if redis_pvc != "Bound" or falkor_pvc != "Bound":
        raise VerificationError(
            f"recovery PVCs must both be Bound (redis={redis_pvc!r}, falkordb={falkor_pvc!r})"
        )

    redis_info = kube(
        "exec",
        "redis-stack-0",
        "--",
        "sh",
        "-ec",
        'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning --raw INFO persistence',
    )
    falkor_info = kube(
        "exec",
        "falkordb-0",
        "--",
        "sh",
        "-ec",
        'export REDISCLI_AUTH="$FALKORDB_PASSWORD"; redis-cli --no-auth-warning --raw INFO persistence',
    )
    redis_health = parse_persistence_info(redis_info, "Redis")
    falkor_health = parse_persistence_info(falkor_info, "FalkorDB")

    memory_units = parse_count(
        kube(
            "exec",
            "redis-stack-0",
            "--",
            "sh",
            "-ec",
            'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning --scan --pattern "$1:mu:*" | wc -l',
            "--",
            tenant_id,
        ),
        "memory-unit count",
    )
    semantic_chunks = parse_count(
        kube(
            "exec",
            "redis-stack-0",
            "--",
            "sh",
            "-ec",
            'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning --scan --pattern "$1:vec:*" | wc -l',
            "--",
            tenant_id,
        ),
        "semantic-chunk count",
    )
    cases = parse_count(
        kube(
            "exec",
            "redis-stack-0",
            "--",
            "sh",
            "-ec",
            'export REDISCLI_AUTH="$REDIS_PASSWORD"; redis-cli --no-auth-warning --scan --pattern "$1:case:*" | awk -F: \'NF == 3 { count++ } END { print count + 0 }\'',
            "--",
            tenant_id,
        ),
        "case count",
    )
    graph_edges = parse_count(
        kube(
            "exec",
            "falkordb-0",
            "--",
            "sh",
            "-ec",
            'export REDISCLI_AUTH="$FALKORDB_PASSWORD"; redis-cli --no-auth-warning --raw GRAPH.QUERY "$1" "MATCH ()-[r]->() RETURN count(r)" --compact',
            "--",
            tenant_id,
        ),
        "graph-edge count",
    )

    expected_memory_units = summary["memoryUnitCount"]
    expected_cases = summary["caseCount"]
    expected_graph_edges = summary["exportedEdgeCount"] + expected_memory_units
    if memory_units != expected_memory_units:
        raise VerificationError(
            f"memory-unit count mismatch: actual {memory_units}, expected {expected_memory_units}"
        )
    if cases != expected_cases:
        raise VerificationError(f"case count mismatch: actual {cases}, expected {expected_cases}")
    if semantic_chunks < expected_memory_units:
        raise VerificationError(
            f"semantic-chunk count {semantic_chunks} is below memory-unit count {expected_memory_units}"
        )
    if graph_edges != expected_graph_edges:
        raise VerificationError(
            f"graph-edge count mismatch: actual {graph_edges}, expected {expected_graph_edges} "
            "(exported edges plus rebuilt CONTAINS)"
        )

    return {
        "schemaVersion": 1,
        "verifiedAt": datetime.now(timezone.utc).isoformat(),
        "tenantId": tenant_id,
        "namespace": namespace,
        "exportSha256": sha256_file(export_path),
        "expected": {
            "memoryUnits": expected_memory_units,
            "cases": expected_cases,
            "exportedEdges": summary["exportedEdgeCount"],
            "totalGraphEdges": expected_graph_edges,
        },
        "actual": {
            "memoryUnits": memory_units,
            "cases": cases,
            "semanticChunks": semantic_chunks,
            "totalGraphEdges": graph_edges,
        },
        "persistence": {"redis": redis_health, "falkorDb": falkor_health},
        "status": "verified",
    }


def write_evidence(path: Path, evidence: dict[str, Any]) -> None:
    """Write evidence atomically."""

    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".part")
    temporary.write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    temporary.replace(path)


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--tenant", required=True)
    parser.add_argument("--export", type=Path, required=True)
    parser.add_argument("--evidence-output", type=Path, required=True)
    parser.add_argument("--kubectl", default="kubectl")
    return parser.parse_args(argv)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        evidence = verify_recovery(
            namespace=args.namespace,
            tenant_id=args.tenant,
            export_path=args.export,
            kubectl=args.kubectl,
        )
        write_evidence(args.evidence_output, evidence)
    except (OSError, VerificationError) as error:
        print(f"backup recovery verification failed: {error}", file=sys.stderr)
        return 1
    print(f"backup recovery verified: {args.evidence_output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
