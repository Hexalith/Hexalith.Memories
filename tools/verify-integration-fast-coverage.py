#!/usr/bin/env python3
"""Verify integration-fast TRX files contain the required acceptance surfaces."""

from __future__ import annotations

import argparse
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--results-directory", required=True)
    parser.add_argument(
        "--requirements",
        default="tools/integration-fast-required-surfaces.txt",
        help="Pipe-delimited surface requirements file.",
    )
    return parser.parse_args()


def load_requirements(path: Path) -> dict[str, str]:
    requirements: dict[str, str] = {}
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue

        try:
            surface, class_name = [part.strip() for part in line.split("|", 1)]
        except ValueError as exc:
            raise SystemExit(f"Invalid requirement line in {path}: {raw_line}") from exc

        if not surface or not class_name:
            raise SystemExit(f"Invalid requirement line in {path}: {raw_line}")

        requirements[surface] = class_name

    if not requirements:
        raise SystemExit(f"No integration-fast requirements found in {path}.")

    return requirements


def executed_classes(results_directory: Path) -> set[str]:
    classes: set[str] = set()
    trx_files = sorted(results_directory.rglob("*.trx"))
    if not trx_files:
        raise SystemExit(f"No TRX files found under {results_directory}.")

    # Aggregate executed counts across all TRX files. A single project with zero
    # filter-matched tests is informational, not fatal: as integration projects grow,
    # filtering may legitimately leave some TRX files empty. Failure fires only when
    # the *total* across the lane is zero, or when no TestMethod className tags survive.
    total_executed = 0
    empty_trx_files: list[Path] = []
    for trx_file in trx_files:
        root = ET.parse(trx_file).getroot()
        counters = root.find(".//{*}Counters")
        executed = int(counters.attrib.get("executed", "0")) if counters is not None else 0
        total_executed += executed
        if executed <= 0:
            empty_trx_files.append(trx_file)

        for unit_test in root.findall(".//{*}UnitTest"):
            method = unit_test.find(".//{*}TestMethod")
            if method is not None and method.attrib.get("className"):
                classes.add(method.attrib["className"])

    if total_executed <= 0:
        raise SystemExit(
            f"All {len(trx_files)} TRX files under {results_directory} report zero executed tests."
        )

    if empty_trx_files:
        # Informational; doesn't fail the verifier so long as the aggregate is non-zero
        # and the required-surfaces check (below) still passes.
        print(
            f"integration-fast: {len(empty_trx_files)} TRX file(s) executed zero tests "
            f"(aggregate executed={total_executed} across {len(trx_files)} TRX files):",
            file=sys.stderr,
        )
        for trx_file in empty_trx_files:
            print(f"  {trx_file}", file=sys.stderr)

    if not classes:
        raise SystemExit(f"No executed test classes were discovered under {results_directory}.")

    return classes


def main() -> int:
    args = parse_args()
    results_directory = Path(args.results_directory)
    requirements = load_requirements(Path(args.requirements))
    classes = executed_classes(results_directory)

    print("integration-fast executed classes:")
    for class_name in sorted(classes):
        print(f"  {class_name}")

    missing = {
        surface: class_name
        for surface, class_name in requirements.items()
        if class_name not in classes
    }
    if missing:
        print("integration-fast missing required surfaces:", file=sys.stderr)
        for surface, class_name in missing.items():
            print(f"  {surface}: {class_name}", file=sys.stderr)
        return 1

    print("integration-fast required surfaces satisfied:")
    for surface, class_name in requirements.items():
        print(f"  {surface}: {class_name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
