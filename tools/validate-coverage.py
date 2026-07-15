#!/usr/bin/env python3
"""Validate unioned first-party Cobertura line coverage fail closed."""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import math
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path, PurePath, PurePosixPath
from typing import NamedTuple


WINDOWS_ABSOLUTE_PATH = re.compile(r"^[A-Za-z]:/")


class CoverageValidationError(RuntimeError):
    """Raised when coverage evidence is absent, malformed, incomplete, or below threshold."""

    def __init__(self, message: str, summary: CoverageSummary | None = None) -> None:
        super().__init__(message)
        self.summary = summary


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
    assembly_source_prefixes: dict[str, tuple[str, ...]]
    required_report_projects: tuple[str, ...]


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


def _repository_root_text(repo_root: Path | PurePath) -> str:
    if isinstance(repo_root, Path):
        return repo_root.resolve().as_posix().rstrip("/")
    return repo_root.as_posix().rstrip("/")


def _mapped_source_roots(source_roots: tuple[str, ...], repository_root: str) -> tuple[str, ...]:
    mapped: list[str] = []
    absolute_root_seen = False
    mapped_absolute_root_seen = False
    for source_root in source_roots:
        normalized_root = source_root.replace("\\", "/").rstrip("/")
        if normalized_root.startswith("/") or WINDOWS_ABSOLUTE_PATH.match(normalized_root):
            absolute_root_seen = True
            root_relative = _strip_absolute_root(normalized_root, repository_root)
            if root_relative is None:
                continue
            mapped_absolute_root_seen = True
            mapped.append(_normalize_relative_path(root_relative) if root_relative else "")
        else:
            mapped.append(_normalize_relative_path(normalized_root))

    if absolute_root_seen and not mapped_absolute_root_seen:
        raise CoverageValidationError("Cobertura source roots are outside the repository")
    return tuple(dict.fromkeys(mapped))


def normalize_source_path(
    raw_path: str,
    source_roots: tuple[str, ...],
    repo_root: Path | PurePath,
) -> str:
    """Map a Cobertura source identity to a safe repository-relative POSIX path."""

    normalized = raw_path.strip().replace("\\", "/")
    if "\x00" in normalized:
        raise CoverageValidationError(f"unsafe source path {raw_path!r}")

    repository_root = _repository_root_text(repo_root)
    is_absolute = normalized.startswith("/") or bool(WINDOWS_ABSOLUTE_PATH.match(normalized))
    if not is_absolute:
        relative_path = _normalize_relative_path(normalized)
        mapped_roots = _mapped_source_roots(source_roots, repository_root)
        candidates = [
            _normalize_relative_path(f"{root}/{relative_path}") if root else relative_path
            for root in mapped_roots
        ]
        candidates.append(relative_path)
        candidates = list(dict.fromkeys(candidates))

        if isinstance(repo_root, Path):
            existing = [candidate for candidate in candidates if (repo_root / candidate).is_file()]
            if len(existing) == 1:
                return existing[0]
            if len(existing) > 1:
                raise CoverageValidationError(
                    f"ambiguous source path {raw_path!r}: matches multiple repository files"
                )

        if relative_path.startswith("src/") or not mapped_roots:
            return relative_path
        mapped_candidates = list(dict.fromkeys(candidates[:-1]))
        if len(mapped_candidates) == 1:
            return mapped_candidates[0]
        raise CoverageValidationError(
            f"ambiguous source path {raw_path!r}: multiple repository source roots match"
        )

    relative = _strip_absolute_root(normalized, repository_root)
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
        threshold_value = raw["minimumLineCoveragePercent"]
        raw_prefixes = raw["sourcePathPrefixes"]
        assembly_prefix = raw["assemblyNamePrefix"]
        raw_excluded_assemblies = raw["excludedAssemblyPatterns"]
        raw_excluded_paths = raw.get("excludedSourcePathPatterns", [])
        raw_required_assemblies = raw["requiredAssemblies"]
        required_files = raw["requiredFiles"]
        raw_assembly_source_prefixes = raw["assemblySourcePrefixes"]
        raw_required_report_projects = raw["requiredReportProjects"]
    except (KeyError, TypeError) as error:
        raise CoverageValidationError(f"invalid coverage configuration {path}: {error}") from error

    collection_values = {
        "sourcePathPrefixes": raw_prefixes,
        "excludedAssemblyPatterns": raw_excluded_assemblies,
        "excludedSourcePathPatterns": raw_excluded_paths,
        "requiredAssemblies": raw_required_assemblies,
        "requiredReportProjects": raw_required_report_projects,
    }
    for name, values in collection_values.items():
        if not isinstance(values, list) or any(
            not isinstance(value, str) or not value for value in values
        ):
            raise CoverageValidationError(f"{name} must be an array of non-empty strings")

    if isinstance(threshold_value, bool) or not isinstance(threshold_value, (int, float)):
        raise CoverageValidationError("minimumLineCoveragePercent must be a finite number")
    threshold = float(threshold_value)
    if not math.isfinite(threshold):
        raise CoverageValidationError("minimumLineCoveragePercent must be a finite number")
    if not isinstance(required_files, dict):
        raise CoverageValidationError("requiredFiles must be an object")
    if not isinstance(raw_assembly_source_prefixes, dict):
        raise CoverageValidationError("assemblySourcePrefixes must be an object")

    prefixes = tuple(raw_prefixes)
    excluded_assemblies = tuple(raw_excluded_assemblies)
    excluded_paths = tuple(raw_excluded_paths)
    required_assemblies = tuple(raw_required_assemblies)
    required_report_projects = tuple(raw_required_report_projects)

    if threshold < 0.0 or threshold > 100.0:
        raise CoverageValidationError("minimumLineCoveragePercent must be between 0 and 100")
    if not isinstance(assembly_prefix, str) or not assembly_prefix:
        raise CoverageValidationError("assemblyNamePrefix must be a non-empty string")
    if not prefixes or not required_assemblies or not required_report_projects:
        raise CoverageValidationError(
            "sourcePathPrefixes, requiredAssemblies, and requiredReportProjects must not be empty"
        )
    if len(set(required_assemblies)) != len(required_assemblies):
        raise CoverageValidationError("requiredAssemblies must not contain duplicates")
    if any(not isinstance(key, str) or not isinstance(value, str) for key, value in required_files.items()):
        raise CoverageValidationError("requiredFiles must map assembly names to source paths")
    if any(assembly not in required_assemblies for assembly in required_files):
        raise CoverageValidationError("requiredFiles assemblies must also appear in requiredAssemblies")
    if set(raw_assembly_source_prefixes) != set(required_assemblies):
        raise CoverageValidationError(
            "assemblySourcePrefixes must define every required assembly and no others"
        )
    for assembly, values in raw_assembly_source_prefixes.items():
        if not isinstance(assembly, str) or not isinstance(values, list) or not values or any(
            not isinstance(value, str) or not value for value in values
        ):
            raise CoverageValidationError(
                "assemblySourcePrefixes must map assembly names to non-empty string arrays"
            )
    if len(set(required_report_projects)) != len(required_report_projects):
        raise CoverageValidationError("requiredReportProjects must not contain duplicates")

    normalized_prefixes = tuple(_normalize_relative_path(prefix) for prefix in prefixes)
    normalized_required_files = {
        assembly: _normalize_relative_path(source_path)
        for assembly, source_path in required_files.items()
    }
    normalized_assembly_prefixes = {
        assembly: tuple(_normalize_relative_path(prefix) for prefix in values)
        for assembly, values in raw_assembly_source_prefixes.items()
    }
    for assembly, source_path in normalized_required_files.items():
        if not any(
            source_path == prefix or source_path.startswith(f"{prefix}/")
            for prefix in normalized_assembly_prefixes[assembly]
        ):
            raise CoverageValidationError(
                f"required file {source_path!r} is outside {assembly}'s declared source scope"
            )
    return CoverageConfiguration(
        threshold,
        normalized_prefixes,
        assembly_prefix,
        excluded_assemblies,
        excluded_paths,
        required_assemblies,
        normalized_required_files,
        normalized_assembly_prefixes,
        required_report_projects,
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


def _source_matches_assembly(
    assembly: str,
    source_path: str,
    configuration: CoverageConfiguration,
) -> bool:
    prefixes = configuration.assembly_source_prefixes.get(assembly)
    if prefixes is None:
        raise CoverageValidationError(
            f"in-scope assembly {assembly!r} has no declared source scope"
        )
    return any(source_path == prefix or source_path.startswith(f"{prefix}/") for prefix in prefixes)


def _parse_report(
    report_path: Path,
    configuration: CoverageConfiguration,
    repo_root: Path,
    union_hits: dict[tuple[str, int], int],
    assembly_hits: dict[str, dict[tuple[str, int], int]],
) -> int:
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
    report_lines: set[tuple[str, int]] = set()
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
            if not _source_matches_assembly(assembly, source_path, configuration):
                raise CoverageValidationError(
                    f"Cobertura report {report_path} maps assembly {assembly!r} "
                    f"to source outside its declared scope: {source_path}"
                )
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
                report_lines.add(key)
                union_hits[key] = max(union_hits.get(key, 0), hits)
                assembly_hits[assembly][key] = max(assembly_hits[assembly].get(key, 0), hits)
    return len(report_lines)


def _report_project(report_path: Path, results_directory: Path) -> str:
    try:
        relative = report_path.relative_to(results_directory)
    except ValueError as error:
        raise CoverageValidationError(
            f"coverage report {report_path} is outside results directory {results_directory}"
        ) from error
    return relative.parts[0] if len(relative.parts) > 1 else "."


def validate_coverage(results_directory: Path, configuration_path: Path, repo_root: Path) -> CoverageSummary:
    """Validate recursively discovered Cobertura reports against the checked-in contract."""

    configuration = load_configuration(configuration_path)
    report_paths = sorted(results_directory.rglob("coverage.cobertura.xml"))
    if not report_paths:
        raise CoverageValidationError(f"no Cobertura reports found under {results_directory}")

    expected_projects = set(configuration.required_report_projects)
    discovered_projects = {_report_project(path, results_directory) for path in report_paths}
    unexpected_projects = sorted(discovered_projects - expected_projects)
    if unexpected_projects:
        raise CoverageValidationError(
            "Cobertura reports found for unexpected result projects: " + ", ".join(unexpected_projects)
        )
    missing_projects = sorted(expected_projects - discovered_projects)
    if missing_projects:
        raise CoverageValidationError(
            "required test projects produced no Cobertura report: " + ", ".join(missing_projects)
        )

    unique_reports: list[tuple[Path, str]] = []
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
        unique_reports.append((report_path, _report_project(report_path, results_directory)))

    union_hits: dict[tuple[str, int], int] = {}
    assembly_hits: dict[str, dict[tuple[str, int], int]] = defaultdict(dict)
    project_line_evidence: dict[str, int] = defaultdict(int)
    for report_path, project in unique_reports:
        report_line_count = _parse_report(
            report_path,
            configuration,
            repo_root,
            union_hits,
            assembly_hits,
        )
        if report_line_count == 0:
            raise CoverageValidationError(
                f"Cobertura report {report_path} contains no in-scope executable lines"
            )
        project_line_evidence[project] += report_line_count

    projects_without_evidence = sorted(
        project for project in expected_projects if project_line_evidence.get(project, 0) == 0
    )
    if projects_without_evidence:
        raise CoverageValidationError(
            "required test projects produced no in-scope coverage evidence: "
            + ", ".join(projects_without_evidence)
        )

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
    assembly_summaries = {
        assembly: AssemblyCoverage(
            sum(1 for hits in hits_by_line.values() if hits > 0),
            len(hits_by_line),
        )
        for assembly, hits_by_line in sorted(assembly_hits.items())
    }
    summary = CoverageSummary(
        covered_lines,
        valid_lines,
        percentage,
        configuration.threshold_percent,
        len(unique_reports),
        duplicate_count,
        assembly_summaries,
        required_file_counts,
    )
    if percentage < configuration.threshold_percent:
        raise CoverageValidationError(
            f"line coverage {percentage:.2f}% is below required {configuration.threshold_percent:.2f}% "
            f"({covered_lines}/{valid_lines})",
            summary,
        )
    return summary


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
        if error.summary is not None:
            print(format_console_summary(error.summary))
            github_summary = os.environ.get("GITHUB_STEP_SUMMARY")
            if github_summary:
                try:
                    append_github_summary(error.summary, Path(github_summary))
                except OSError as summary_error:
                    print(
                        f"coverage validation failed to write GitHub summary: {summary_error}",
                        file=sys.stderr,
                    )
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
