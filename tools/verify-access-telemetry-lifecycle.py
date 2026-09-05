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
    STORY_27_4_CHECKPOINTS,
    EnvironmentIdentity,
    EnvironmentIdentityError,
    run_adapter_profile_checkpoint,
    run_close_out_postflight,
    run_close_out_preflight,
    run_publish_verification,
    run_story_27_4_checkpoint,
    run_story_27_4_producer_checkpoint,
    write_a41_inventory,
)


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", required=True)
    parser.add_argument("--kube-context")
    parser.add_argument("--namespace")
    parser.add_argument("--deployment-id")
    parser.add_argument("--profile-id")
    parser.add_argument("--workload-profile")
    parser.add_argument("--steady-state-minutes", type=int)
    parser.add_argument("--purge-backlog-records", type=int)
    parser.add_argument("--evidence")
    parser.add_argument(
        "--c0-wrapper",
        help="Optional immutable C0 wrapper written with a passing JSON adapter-profile packet.",
    )
    # Both values are part of the reviewed Production-Shaped Execution Contract and must be
    # supplied, never synthesized. DECLARED_SINGLE_COMPONENT_FAULT previously defaulted to
    # the literal "unconfigured", which is non-empty and therefore passed the required-field
    # check and was stamped into the immutable packet as a reviewed declaration; EVIDENCE_ROOT
    # was derived from the evidence path's parent instead of the reviewed value.
    parser.add_argument(
        "--declared-single-component-fault",
        default=os.environ.get("DECLARED_SINGLE_COMPONENT_FAULT", ""),
    )
    parser.add_argument("--evidence-root", default=os.environ.get("EVIDENCE_ROOT", ""))
    parser.add_argument("--repository-root", default=str(TOOLS_DIR.parent))
    parser.add_argument("--input")
    parser.add_argument("--predecessor")
    parser.add_argument("--owner")
    parser.add_argument("--scenario-input", help="Allowlisted target identity for the registered producer.")
    parser.add_argument("--bundle")
    parser.add_argument("--mutation-manifest")
    parser.add_argument("--snapshot")
    parser.add_argument("--preflight")
    parser.add_argument("--postflight")
    parser.add_argument("--commit")
    parser.add_argument("--remote")
    parser.add_argument("--branch")
    return parser


def _required(args: argparse.Namespace, *names: str) -> bool:
    missing = [name for name in names if getattr(args, name.replace("-", "_"), None) in {None, ""}]
    if missing:
        print("missing required arguments: " + ", ".join(f"--{name}" for name in missing), file=sys.stderr)
        return False
    return True


def main(argv: list[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    repository_root = Path(args.repository_root)
    if args.checkpoint in STORY_27_4_CHECKPOINTS:
        if not _required(args, "predecessor", "evidence", "evidence-root", "owner"):
            return 2
        if bool(args.input) == bool(args.scenario_input):
            print(
                "supply exactly one of --input (offline validation) or --scenario-input (registered producer)",
                file=sys.stderr,
            )
            return 2
        if args.input:
            result = run_story_27_4_checkpoint(
                checkpoint=args.checkpoint,
                input_path=Path(args.input),
                predecessor_path=Path(args.predecessor),
                evidence_path=Path(args.evidence),
                owner=args.owner,
                repository_root=repository_root,
                evidence_root=Path(args.evidence_root),
            )
            if result == 0:
                print(
                    "offline packet validation passed; this is not running-target evidence and does not advance A41",
                    file=sys.stderr,
                )
            return result
        return run_story_27_4_producer_checkpoint(
            checkpoint=args.checkpoint,
            scenario_input_path=Path(args.scenario_input),
            predecessor_path=Path(args.predecessor),
            evidence_path=Path(args.evidence),
            owner=args.owner,
            repository_root=repository_root,
            evidence_root=Path(args.evidence_root),
        )

    if args.checkpoint == "a41-inventory":
        if not _required(args, "evidence", "evidence-root"):
            return 2
        return write_a41_inventory(repository_root, Path(args.evidence), Path(args.evidence_root))

    if args.checkpoint == "close-out-preflight":
        if not _required(args, "bundle", "mutation-manifest", "snapshot", "evidence", "evidence-root", "remote", "branch"):
            return 2
        return run_close_out_preflight(
            repository_root=repository_root,
            bundle_path=Path(args.bundle),
            mutation_manifest_path=Path(args.mutation_manifest),
            snapshot_path=Path(args.snapshot),
            evidence_path=Path(args.evidence),
            evidence_root=Path(args.evidence_root),
            remote=args.remote,
            branch=args.branch,
        )

    if args.checkpoint == "close-out-postflight":
        if not _required(args, "preflight", "mutation-manifest", "snapshot", "evidence", "evidence-root"):
            return 2
        return run_close_out_postflight(
            repository_root=repository_root,
            preflight_path=Path(args.preflight),
            mutation_manifest_path=Path(args.mutation_manifest),
            snapshot_path=Path(args.snapshot),
            evidence_path=Path(args.evidence),
            evidence_root=Path(args.evidence_root),
        )

    if args.checkpoint == "publish-verification":
        if not _required(
            args,
            "preflight",
            "postflight",
            "mutation-manifest",
            "snapshot",
            "commit",
            "remote",
            "branch",
            "evidence",
            "evidence-root",
        ):
            return 2
        return run_publish_verification(
            repository_root=repository_root,
            commit=args.commit,
            mutation_manifest_path=Path(args.mutation_manifest),
            evidence_path=Path(args.evidence),
            preflight_path=Path(args.preflight),
            postflight_path=Path(args.postflight),
            snapshot_path=Path(args.snapshot),
            remote=args.remote,
            branch=args.branch,
            evidence_root=Path(args.evidence_root),
        )

    if args.checkpoint != "adapter-profile":
        print(f"unsupported checkpoint: {args.checkpoint}", file=sys.stderr)
        return 2
    if not _required(
        args,
        "kube-context",
        "namespace",
        "deployment-id",
        "profile-id",
        "workload-profile",
        "steady-state-minutes",
        "purge-backlog-records",
        "evidence",
    ):
        return 2
    try:
        identity = EnvironmentIdentity.from_mapping(
            {
                "KUBE_CONTEXT": args.kube_context,
                "KUBE_NAMESPACE": args.namespace,
                "DEPLOYMENT_ID": args.deployment_id,
                "PROFILE_ID": args.profile_id,
                "EVIDENCE_ROOT": args.evidence_root,
                "DECLARED_SINGLE_COMPONENT_FAULT": args.declared_single_component_fault,
            }
        )
    except EnvironmentIdentityError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    if identity.declared_single_component_fault.lower() in {"unconfigured", "unknown", "tbd", "none"}:
        print(
            "DECLARED_SINGLE_COMPONENT_FAULT must name the reviewed fault, not a placeholder",
            file=sys.stderr,
        )
        return 2
    return run_adapter_profile_checkpoint(
        identity=identity,
        workload_profile=args.workload_profile,
        steady_state_minutes=args.steady_state_minutes,
        purge_backlog_records=args.purge_backlog_records,
        evidence_path=Path(args.evidence),
        repository_root=repository_root,
        c0_wrapper_path=Path(args.c0_wrapper) if args.c0_wrapper else None,
    )


if __name__ == "__main__":
    raise SystemExit(main())
