#!/usr/bin/env python3
"""Validate unioned first-party Cobertura line coverage fail closed."""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path, PurePosixPath
from typing import NamedTuple


WINDOWS_ABSOLUTE_PATH = re.compile(r"^[A-Za-z]:/")


class CoverageValidationError(RuntimeError):
    """Raised when coverage evidence is absent, malformed, incomplete, or below threshold."""


class AssemblyCoverage(NamedTuple):
    """Line coverage totals for one production assembly."""

    covered_lines: int
    valid_lines: int

    @property
    def coverage_percent(self) -> float:
        """Return the assembly line coverage percentage."""

        return self.covered_lines * 100.0 / self.valid_lines


class CoverageSummary(NamedTuple):
    """Validated union coverage totals and diagnostic detail."""

    covered_lines: int
    valid_lines: int
    coverage_percent: float
    threshold_percent: float
    report_count: int
    duplicate_report_count: int
    assemblies: dict[str, AssemblyCoverage]
    required_file_line_counts: dict[str, int]


class CoverageConfiguration(NamedTuple):
    """Parsed coverage scope and fail-closed requirements."""

    threshold_percent: float
    source_path_prefixes: tuple[str, ...]
    assembly_name_prefix: str
    excluded_assembly_patterns: tuple[str, ...]
    excluded_source_path_patterns: tuple[str, ...]
    required_assemblies: tuple[str, ...]
    required_files: dict[str, str]


def _local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _children(element: ET.Element, name: str) -> list[ET.Element]:
    return [child for child in element if _local_name(child.tag) == name]


def _first_child(element: ET.Element, name: str) -> ET.Element | None:
    return next((child for child in element if _local_name(child.tag) == name), None)


def _normalize_relative_path(raw_path: str) -> str:
    normalized = raw_path.strip().replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]
    parts = PurePosixPath(normalized).parts
    if not normalized or normalized.startswith("/") or WINDOWS_ABSOLUTE_PATH.match(normalized):
        raise CoverageValidationError(f"unsafe source path {raw_path!r}")
    if any(part in {"", ".", ".."} for part in parts):
        raise CoverageValidationError(f"unsafe source path {raw_path!r}")
    return "/".join(parts)


def _strip_absolute_root(path: str, root: str) -> str | None:
    case_insensitive = bool(WINDOWS_ABSOLUTE_PATH.match(path) or WINDOWS_ABSOLUTE_PATH.match(root))
    candidate_path = path.casefold() if case_insensitive else path
    candidate_root = root.casefold().rstrip("/") if case_insensitive else root.rstrip("/")
    if candidate_path == candidate_root:
        return ""
    prefix = f"{candidate_root}/"
    if candidate_path.startswith(prefix):
        return path[len(root.rstrip("/")) + 1 :]
    return None


def normalize_source_path(raw_path: str, source_roots: tuple[str, ...], repo_root: Path) -> str:
    """Map a Cobertura source identity to a safe repository-relative POSIX path."""

    normalized = raw_path.strip().replace("\\", "/")
    if "\x00" in normalized:
        raise CoverageValidationError(f"unsafe source path {raw_path!r}")

    is_absolute = normalized.startswith("/") or bool(WINDOWS_ABSOLUTE_PATH.match(normalized))
    if not is_absolute:
        relative_path = _normalize_relative_path(normalized)
        repository_root = repo_root.resolve().as_posix()
        for source_root in source_roots:
            normalized_root = source_root.replace("\\", "/").rstrip("/")
            if normalized_root.startswith("/") or WINDOWS_ABSOLUTE_PATH.match(normalized_root):
                root_relative = _strip_absolute_root(normalized_root, repository_root)
                if root_relative is None:
                    continue
            else:
                root_relative = _normalize_relative_path(normalized_root)
            if root_relative:
                return _normalize_relative_path(f"{root_relative}/{relative_path}")
        return relative_path

    possible_roots = [repo_root.resolve().as_posix()]
    possible_roots.extend(source_roots)
    for source_root in possible_roots:
        relative = _strip_absolute_root(normalized, source_root.replace("\\", "/"))
        if relative:
            return _normalize_relative_path(relative)
    raise CoverageValidationError(f"unsafe source path {raw_path!r}: outside repository roots")


def load_configuration(path: Path) -> CoverageConfiguration:
    """Load and validate the checked-in coverage contract."""

    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise CoverageValidationError(f"cannot read coverage configuration {path}: {error}") from error

    try:
        threshold = float(raw["minimumLineCoveragePercent"])
        prefixes = tuple(raw["sourcePathPrefixes"])
        assembly_prefix = raw["assemblyNamePrefix"]
        excluded_assemblies = tuple(raw["excludedAssemblyPatterns"])
        excluded_paths = tuple(raw.get("excludedSourcePathPatterns", []))
        required_assemblies = tuple(raw["requiredAssemblies"])
        required_files = dict(raw["requiredFiles"])
    except (KeyError, TypeError, ValueError) as error:
        raise CoverageValidationError(f"invalid coverage configuration {path}: {error}") from error

    string_collections = {
        "sourcePathPrefixes": prefixes,
        "excludedAssemblyPatterns": excluded_assemblies,
        "excludedSourcePathPatterns": excluded_paths,
        "requiredAssemblies": required_assemblies,
    }
    if threshold < 0.0 or threshold > 100.0:
        raise CoverageValidationError("minimumLineCoveragePercent must be between 0 and 100")
    if not isinstance(assembly_prefix, str) or not assembly_prefix:
        raise CoverageValidationError("assemblyNamePrefix must be a non-empty string")
    for name, values in string_collections.items():
        if not isinstance(values, tuple) or any(not isinstance(value, str) or not value for value in values):
            raise CoverageValidationError(f"{name} must contain only non-empty strings")
    if not prefixes or not required_assemblies:
        raise CoverageValidationError("sourcePathPrefixes and requiredAssemblies must not be empty")
    if len(set(required_assemblies)) != len(required_assemblies):
        raise CoverageValidationError("requiredAssemblies must not contain duplicates")
    if any(not isinstance(key, str) or not isinstance(value, str) for key, value in required_files.items()):
        raise CoverageValidationError("requiredFiles must map assembly names to source paths")
    if any(assembly not in required_assemblies for assembly in required_files):
        raise CoverageValidationError("requiredFiles assemblies must also appear in requiredAssemblies")

    normalized_prefixes = tuple(_normalize_relative_path(prefix) for prefix in prefixes)
    normalized_required_files = {
        assembly: _normalize_relative_path(source_path)
        for assembly, source_path in required_files.items()
    }
    return CoverageConfiguration(
        threshold,
        normalized_prefixes,
        assembly_prefix,
        excluded_assemblies,
        excluded_paths,
        required_assemblies,
        normalized_required_files,
    )


def _assembly_is_in_scope(assembly: str, configuration: CoverageConfiguration) -> bool:
    return assembly.startswith(configuration.assembly_name_prefix) and not any(
        fnmatch.fnmatchcase(assembly, pattern)
        for pattern in configuration.excluded_assembly_patterns
    )


def _source_is_in_scope(source_path: str, configuration: CoverageConfiguration) -> bool:
    return any(source_path.startswith(prefix) for prefix in configuration.source_path_prefixes) and not any(
        fnmatch.fnmatchcase(source_path, pattern)
        for pattern in configuration.excluded_source_path_patterns
    )


def _parse_report(
    report_path: Path,
    configuration: CoverageConfiguration,
    repo_root: Path,
    union_hits: dict[tuple[str, int], int],
    assembly_hits: dict[str, dict[tuple[str, int], int]],
) -> None:
    try:
        root = ET.parse(report_path).getroot()
    except (OSError, ET.ParseError) as error:
        raise CoverageValidationError(f"malformed Cobertura report {report_path}: {error}") from error
    if _local_name(root.tag) != "coverage":
        raise CoverageValidationError(f"malformed Cobertura report {report_path}: root is not coverage")

    sources_element = _first_child(root, "sources")
    source_roots = tuple(
        (element.text or "").strip().replace("\\", "/").rstrip("/")
        for element in (_children(sources_element, "source") if sources_element is not None else [])
        if (element.text or "").strip()
    )
    packages_element = _first_child(root, "packages")
    packages = _children(packages_element, "package") if packages_element is not None else []
    for package in packages:
        assembly = package.attrib.get("name", "").strip()
        if not assembly or not _assembly_is_in_scope(assembly, configuration):
            continue
        classes_element = _first_child(package, "classes")
        classes = _children(classes_element, "class") if classes_element is not None else []
        for class_element in classes:
            raw_filename = class_element.attrib.get("filename", "")
            source_path = normalize_source_path(raw_filename, source_roots, repo_root)
            if not _source_is_in_scope(source_path, configuration):
                continue
            lines_element = _first_child(class_element, "lines")
            lines = _children(lines_element, "line") if lines_element is not None else []
            for line in lines:
                try:
                    line_number = int(line.attrib["number"])
                    hits = int(line.attrib["hits"])
                except (KeyError, ValueError) as error:
                    raise CoverageValidationError(
                        f"malformed Cobertura report {report_path}: invalid line evidence"
                    ) from error
                if line_number <= 0 or hits < 0:
                    raise CoverageValidationError(
                        f"malformed Cobertura report {report_path}: invalid line evidence"
                    )
                key = (source_path, line_number)
                union_hits[key] = max(union_hits.get(key, 0), hits)
                assembly_hits[assembly][key] = max(assembly_hits[assembly].get(key, 0), hits)


def validate_coverage(results_directory: Path, configuration_path: Path, repo_root: Path) -> CoverageSummary:
    """Validate recursively discovered Cobertura reports against the checked-in contract."""

    configuration = load_configuration(configuration_path)
    report_paths = sorted(results_directory.rglob("coverage.cobertura.xml"))
    if not report_paths:
        raise CoverageValidationError(f"no Cobertura reports found under {results_directory}")

    unique_reports: list[Path] = []
    hashes: set[str] = set()
    duplicate_count = 0
    for report_path in report_paths:
        try:
            digest = hashlib.sha256(report_path.read_bytes()).hexdigest()
        except OSError as error:
            raise CoverageValidationError(f"cannot read Cobertura report {report_path}: {error}") from error
        if digest in hashes:
            duplicate_count += 1
            continue
        hashes.add(digest)
        unique_reports.append(report_path)

    union_hits: dict[tuple[str, int], int] = {}
    assembly_hits: dict[str, dict[tuple[str, int], int]] = defaultdict(dict)
    for report_path in unique_reports:
        _parse_report(report_path, configuration, repo_root, union_hits, assembly_hits)

    if not union_hits:
        raise CoverageValidationError("Cobertura reports contain no in-scope executable lines")
    missing_assemblies = [
        assembly
        for assembly in configuration.required_assemblies
        if not assembly_hits.get(assembly)
    ]
    if missing_assemblies:
        raise CoverageValidationError(
            "required assemblies have no executable lines: " + ", ".join(missing_assemblies)
        )

    required_file_counts: dict[str, int] = {}
    missing_files: list[str] = []
    for assembly, source_path in configuration.required_files.items():
        count = sum(
            1
            for path, _line_number in assembly_hits.get(assembly, {})
            if path == source_path
        )
        required_file_counts[source_path] = count
        if count == 0:
            missing_files.append(f"{assembly}:{source_path}")
    if missing_files:
        raise CoverageValidationError(
            "required files have no executable lines: " + ", ".join(missing_files)
        )

    valid_lines = len(union_hits)
    covered_lines = sum(1 for hits in union_hits.values() if hits > 0)
    percentage = covered_lines * 100.0 / valid_lines
    if percentage < configuration.threshold_percent:
        raise CoverageValidationError(
            f"line coverage {percentage:.2f}% is below required {configuration.threshold_percent:.2f}% "
            f"({covered_lines}/{valid_lines})"
        )

    assembly_summaries = {
        assembly: AssemblyCoverage(
            sum(1 for hits in hits_by_line.values() if hits > 0),
            len(hits_by_line),
        )
        for assembly, hits_by_line in sorted(assembly_hits.items())
    }
    return CoverageSummary(
        covered_lines,
        valid_lines,
        percentage,
        configuration.threshold_percent,
        len(unique_reports),
        duplicate_count,
        assembly_summaries,
        required_file_counts,
    )


def format_console_summary(summary: CoverageSummary) -> str:
    """Format concise human-readable coverage diagnostics."""

    rows = [
        f"Coverage reports: {summary.report_count} unique, {summary.duplicate_report_count} duplicate attachment(s)",
    ]
    for assembly, coverage in summary.assemblies.items():
        rows.append(
            f"  {assembly}: {coverage.covered_lines}/{coverage.valid_lines} "
            f"({coverage.coverage_percent:.2f}%)"
        )
    for source_path, line_count in sorted(summary.required_file_line_counts.items()):
        rows.append(f"  required source {source_path}: {line_count} executable line(s)")
    rows.append(
        f"Union line coverage: {summary.covered_lines}/{summary.valid_lines} "
        f"({summary.coverage_percent:.2f}%), required >= {summary.threshold_percent:.2f}%"
    )
    return "\n".join(rows)


def append_github_summary(summary: CoverageSummary, path: Path) -> None:
    """Append validated coverage evidence to the GitHub job summary."""

    lines = [
        "## Scoped line coverage",
        "",
        "| Assembly | Covered | Valid | Coverage |",
        "| --- | ---: | ---: | ---: |",
    ]
    for assembly, coverage in summary.assemblies.items():
        lines.append(
            f"| `{assembly}` | {coverage.covered_lines} | {coverage.valid_lines} | "
            f"{coverage.coverage_percent:.2f}% |"
        )
    lines.extend(
        [
            "",
            f"**Union:** {summary.covered_lines}/{summary.valid_lines} "
            f"({summary.coverage_percent:.2f}%), required ≥ {summary.threshold_percent:.2f}%.",
            "",
        ]
    )
    with path.open("a", encoding="utf-8") as summary_file:
        summary_file.write("\n".join(lines))


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--results-directory", type=Path, required=True)
    parser.add_argument("--config", type=Path)
    parser.add_argument("--repo-root", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    default_repo_root = Path(__file__).resolve().parent.parent
    repo_root = (args.repo_root or default_repo_root).resolve()
    configuration_path = args.config or repo_root / "tests" / "tooling" / "coverage_gate" / "line-coverage-gate.json"
    try:
        summary = validate_coverage(args.results_directory, configuration_path, repo_root)
    except CoverageValidationError as error:
        print(f"coverage validation failed:\n{error}", file=sys.stderr)
        return 1

    print(format_console_summary(summary))
    github_summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if github_summary:
        try:
            append_github_summary(summary, Path(github_summary))
        except OSError as error:
            print(f"coverage validation failed to write GitHub summary: {error}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
