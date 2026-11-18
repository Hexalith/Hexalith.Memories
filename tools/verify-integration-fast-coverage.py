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


def load_requirements(path: Path) -> dict[str, tuple[str, str | None]]:
    requirements: dict[str, tuple[str, str | None]] = {}
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue

        parts = [part.strip() for part in line.split("|")]
        if len(parts) not in (2, 3):
            raise SystemExit(f"Invalid requirement line in {path}: {raw_line}")

        surface, class_name = parts[:2]
        method_name = parts[2] if len(parts) == 3 else None
        if not surface or not class_name or (len(parts) == 3 and not method_name):
            raise SystemExit(f"Invalid requirement line in {path}: {raw_line}")
        if surface in requirements:
            raise SystemExit(f"Duplicate requirement surface in {path}: {surface}")

        requirements[surface] = (class_name, method_name)

    if not requirements:
        raise SystemExit(f"No integration-fast requirements found in {path}.")

    return requirements


def passed_tests(results_directory: Path) -> set[tuple[str, str]]:
    passed: set[tuple[str, str]] = set()
    trx_files = sorted(results_directory.rglob("*.trx"))
    if not trx_files:
        raise SystemExit(f"No TRX files found under {results_directory}.")

    total_passed_results = 0
    for trx_file in trx_files:
        root = ET.parse(trx_file).getroot()
        definitions: dict[str, tuple[str, str]] = {}
        for unit_test in root.findall(".//{*}UnitTest"):
            method = unit_test.find(".//{*}TestMethod")
            test_id = unit_test.attrib.get("id")
            class_name = method.attrib.get("className") if method is not None else None
            method_name = method.attrib.get("name") if method is not None else None
            if test_id and class_name and method_name:
                definitions[test_id] = (class_name, method_name)

        for result in root.findall(".//{*}UnitTestResult"):
            if result.attrib.get("outcome") != "Passed":
                continue

            total_passed_results += 1
            identity = definitions.get(result.attrib.get("testId", ""))
            if identity is not None:
                passed.add(identity)

    if total_passed_results <= 0:
        raise SystemExit(
            f"No UnitTestResult with outcome=Passed was found under {results_directory}."
        )

    if not passed:
        raise SystemExit(
            f"Passed TRX results under {results_directory} did not map to TestMethod definitions."
        )

    return passed


def missing_requirements(
    requirements: dict[str, tuple[str, str | None]],
    passed: set[tuple[str, str]],
) -> dict[str, tuple[str, str | None]]:
    passed_classes = {class_name for class_name, _ in passed}
    return {
        surface: (class_name, method_name)
        for surface, (class_name, method_name) in requirements.items()
        if (
            class_name not in passed_classes
            if method_name is None
            else (class_name, method_name) not in passed
        )
    }


def format_requirement(class_name: str, method_name: str | None) -> str:
    return class_name if method_name is None else f"{class_name}.{method_name}"


def main() -> int:
    args = parse_args()
    results_directory = Path(args.results_directory)
    requirements = load_requirements(Path(args.requirements))
    passed = passed_tests(results_directory)
    classes = {class_name for class_name, _ in passed}

    print("integration-fast classes with passed tests:")
    for class_name in sorted(classes):
        print(f"  {class_name}")

    missing = missing_requirements(requirements, passed)
    if missing:
        print("integration-fast missing required surfaces:", file=sys.stderr)
        for surface, (class_name, method_name) in missing.items():
            print(f"  {surface}: {format_requirement(class_name, method_name)}", file=sys.stderr)
        return 1

    print("integration-fast required surfaces satisfied:")
    for surface, (class_name, method_name) in requirements.items():
        print(f"  {surface}: {format_requirement(class_name, method_name)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
