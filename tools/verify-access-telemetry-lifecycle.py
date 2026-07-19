#!/usr/bin/env python3
"""CLI entry point for fail-closed access-telemetry lifecycle evidence."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import sys


TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from verify_access_telemetry_lifecycle import (  # noqa: E402
    EnvironmentIdentity,
    EnvironmentIdentityError,
    run_adapter_profile_checkpoint,
)


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", required=True)
    parser.add_argument("--kube-context", required=True)
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--deployment-id", required=True)
    parser.add_argument("--profile-id", required=True)
    parser.add_argument("--workload-profile", required=True)
    parser.add_argument("--steady-state-minutes", type=int, required=True)
    parser.add_argument("--purge-backlog-records", type=int, required=True)
    parser.add_argument("--evidence", required=True)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    if args.checkpoint != "adapter-profile":
        print(f"unsupported checkpoint: {args.checkpoint}", file=sys.stderr)
        return 2
    try:
        identity = EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": args.kube_context,
                "KUBE_NAMESPACE": args.namespace,
                "DEPLOYMENT_ID": args.deployment_id,
                "PROFILE_ID": args.profile_id,
                "EVIDENCE_ROOT": str(Path(args.evidence).resolve().parent),
                "DECLARED_SINGLE_COMPONENT_FAULT": os.environ.get(
                    "DECLARED_SINGLE_COMPONENT_FAULT",
                    "unconfigured",
                ),
            }
        )
    except EnvironmentIdentityError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    return run_adapter_profile_checkpoint(
        identity=identity,
        workload_profile=args.workload_profile,
        steady_state_minutes=args.steady_state_minutes,
        purge_backlog_records=args.purge_backlog_records,
        evidence_path=Path(args.evidence),
    )


if __name__ == "__main__":
    raise SystemExit(main())
