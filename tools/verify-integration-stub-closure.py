#!/usr/bin/env python3
"""Verify Story 26.3 source dispositions and xUnit TRX outcomes for all audited targets."""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path
from typing import NamedTuple


EXPECTED_TARGET_COUNT = 28
PRIORITY_ORIGINALS = {
    "Hexalith.Memories.IntegrationTests.Ingestion.IngestionRetryIntegrationTests.TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries",
    "Hexalith.Memories.IntegrationTests.Ingestion.RetryFailureIntegrationTests.IngestUrl_ProviderReturns500_ExhaustsRetriesAndPersistsFailedUnit",
    "Hexalith.Memories.IntegrationTests.Ingestion.RateLimitingIntegrationTests.TwoTenantIsolation_ShouldEnforceIndependentCeilings",
    "Hexalith.Memories.IntegrationTests.Ingestion.RateLimitingIntegrationTests.Provider429_ShouldReportToActorAndRetry",
    "Hexalith.Memories.IntegrationTests.Search.DegradationIntegrationTests.HybridSearch_FalkorDbStopped_ShouldDegradeToSyntacticAndSemantic",
}

NAMESPACE_RE = re.compile(r"\bnamespace\s+([A-Za-z_][\w.]*)\s*;")
CLASS_RE = re.compile(r"\bclass\s+([A-Za-z_]\w*)")
METHOD_RE = re.compile(
    r"(?P<attributes>(?:\s*\[[^\]]+\]\s*)+)"
    r"(?:public|internal|private|protected)\s+"
    r"(?:static\s+)?(?:async\s+)?(?:Task(?:<[^>]+>)?|ValueTask(?:<[^>]+>)?|void)\s+"
    r"(?P<method>[A-Za-z_]\w*)\s*\(",
    re.MULTILINE,
)
SKIP_REASON_RE = re.compile(r'\bSkip\s*=\s*"(?P<reason>(?:\\.|[^"])*)"')
DEFERRED_ID_RE = re.compile(r"\b\d+\.\d+-[A-Z0-9][A-Z0-9-]+\b")
TEST_ATTRIBUTE_RE = re.compile(r"\b(?:Fact|Theory)\b")
DEFERRED_FIELD_RE = re.compile(
    r"^\s*-\s+(?P<name>ID|Status|Source story|Target artifact|Re-open trigger|Rationale):\s*(?P<value>.*)\s*$",
    re.IGNORECASE,
)
LINE_COMMENT_RE = re.compile(r"//[^\r\n]*")
BLOCK_COMMENT_RE = re.compile(r"/\*.*?\*/", re.DOTALL)
WHITESPACE_RE = re.compile(r"\s+")


class VerificationError(RuntimeError):
    """Raised when source or result evidence is incomplete or contradictory."""


class TargetRow(NamedTuple):
    original: str
    final: str


class SourceDisposition(NamedTuple):
    final: str
    skipped: bool
    reason: str | None
    no_op: bool
    path: Path


class VerificationReport(NamedTuple):
    passed: int
    skipped: int
    failed: int
    rows: tuple[tuple[str, str, str], ...]


def read_targets(path: Path, expected_count: int) -> list[TargetRow]:
    rows: list[TargetRow] = []
    for line_number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = [part.strip() for part in line.split("|")]
        if len(parts) not in (1, 2) or not all(parts):
            raise VerificationError(f"{path}:{line_number}: expected original or original|replacement")
        rows.append(TargetRow(parts[0], parts[-1]))

    if len(rows) != expected_count:
        raise VerificationError(f"target manifest has {len(rows)} rows; expected {expected_count}")

    originals = [row.original for row in rows]
    duplicates = sorted({name for name in originals if originals.count(name) > 1})
    if duplicates:
        raise VerificationError(f"target manifest contains duplicate originals: {', '.join(duplicates)}")
    return rows


def discover_source_dispositions(source_root: Path) -> dict[str, list[SourceDisposition]]:
    discovered: dict[str, list[SourceDisposition]] = defaultdict(list)
    for path in sorted(source_root.rglob("*.cs")):
        source = path.read_text(encoding="utf-8-sig")
        namespace_match = NAMESPACE_RE.search(source)
        if namespace_match is None:
            continue
        namespace = namespace_match.group(1)
        classes = list(CLASS_RE.finditer(source))
        for method_match in METHOD_RE.finditer(source):
            attributes = method_match.group("attributes")
            if TEST_ATTRIBUTE_RE.search(attributes) is None:
                continue
            preceding_classes = [match for match in classes if match.start() < method_match.start()]
            if not preceding_classes:
                continue
            class_name = preceding_classes[-1].group(1)
            final = f"{namespace}.{class_name}.{method_match.group('method')}"
            skip_match = SKIP_REASON_RE.search(attributes)
            body = _extract_method_body(source, method_match.end() - 1)
            discovered[final].append(
                SourceDisposition(
                    final=final,
                    skipped=skip_match is not None,
                    reason=skip_match.group("reason") if skip_match is not None else None,
                    no_op=_is_no_op_body(body),
                    path=path,
                )
            )
    return discovered


def _extract_method_body(source: str, opening_parenthesis: int) -> str:
    closing_parenthesis = _find_matching_delimiter(source, opening_parenthesis, "(", ")")
    cursor = closing_parenthesis + 1
    while cursor < len(source):
        cursor = _skip_whitespace_and_comments(source, cursor)
        if source.startswith("=>", cursor):
            expression_start = cursor + 2
            semicolon = _find_unquoted_character(source, expression_start, ";")
            return source[expression_start:semicolon]
        if cursor < len(source) and source[cursor] == "{":
            closing_brace = _find_matching_delimiter(source, cursor, "{", "}")
            return source[cursor + 1:closing_brace]
        cursor += 1
    return ""


def _find_matching_delimiter(source: str, opening_index: int, opening: str, closing: str) -> int:
    depth = 0
    cursor = opening_index
    while cursor < len(source):
        skipped = _skip_literal_or_comment(source, cursor)
        if skipped is not None:
            cursor = skipped
            continue
        if source[cursor] == opening:
            depth += 1
        elif source[cursor] == closing:
            depth -= 1
            if depth == 0:
                return cursor
        cursor += 1
    raise VerificationError(f"unterminated method delimiter at source offset {opening_index}")


def _find_unquoted_character(source: str, start: int, value: str) -> int:
    cursor = start
    while cursor < len(source):
        skipped = _skip_literal_or_comment(source, cursor)
        if skipped is not None:
            cursor = skipped
            continue
        if source[cursor] == value:
            return cursor
        cursor += 1
    raise VerificationError(f"unterminated expression-bodied test at source offset {start}")


def _skip_whitespace_and_comments(source: str, start: int) -> int:
    cursor = start
    while cursor < len(source):
        if source[cursor].isspace():
            cursor += 1
            continue
        skipped = _skip_comment(source, cursor)
        if skipped is None:
            return cursor
        cursor = skipped
    return cursor


def _skip_literal_or_comment(source: str, start: int) -> int | None:
    comment_end = _skip_comment(source, start)
    if comment_end is not None:
        return comment_end

    quote_index = start
    while quote_index < len(source) and source[quote_index] in "$@":
        quote_index += 1
    if quote_index >= len(source) or source[quote_index] not in "\"'":
        return None

    quote = source[quote_index]
    verbatim = "@" in source[start:quote_index]
    if quote == '"':
        raw_quote_count = 0
        while quote_index + raw_quote_count < len(source) and source[quote_index + raw_quote_count] == '"':
            raw_quote_count += 1
        if raw_quote_count >= 3:
            terminator = '"' * raw_quote_count
            raw_end = source.find(terminator, quote_index + raw_quote_count)
            return len(source) if raw_end < 0 else raw_end + raw_quote_count

    cursor = quote_index + 1
    while cursor < len(source):
        if verbatim and source.startswith('""', cursor):
            cursor += 2
            continue
        if not verbatim and source[cursor] == "\\":
            cursor += 2
            continue
        if source[cursor] == quote:
            return cursor + 1
        cursor += 1
    return len(source)


def _skip_comment(source: str, start: int) -> int | None:
    if source.startswith("//", start):
        newline = source.find("\n", start + 2)
        return len(source) if newline < 0 else newline + 1
    if source.startswith("/*", start):
        end = source.find("*/", start + 2)
        return len(source) if end < 0 else end + 2
    return None


def _is_no_op_body(body: str) -> bool:
    without_comments = BLOCK_COMMENT_RE.sub("", LINE_COMMENT_RE.sub("", body))
    normalized = WHITESPACE_RE.sub("", without_comments)
    return normalized in {
        "",
        "return;",
        "Task.CompletedTask",
        "Task.CompletedTask;",
        "returnTask.CompletedTask;",
        "awaitTask.CompletedTask;",
        "_=_fixture",
        "_=_fixture;",
    }


def read_deferred_entries(path: Path) -> dict[str, list[dict[str, str]]]:
    entries: dict[str, list[dict[str, str]]] = defaultdict(list)
    current: dict[str, str] | None = None
    current_field: str | None = None
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        match = DEFERRED_FIELD_RE.match(raw_line)
        if match is not None:
            field = match.group("name").lower()
            if field == "id":
                current = {"id": match.group("value").strip()}
                entries[current["id"]].append(current)
            elif current is not None:
                current[field] = match.group("value").strip()
            current_field = field if current is not None else None
            continue

        if current is not None and current_field is not None and raw_line.startswith(("  ", "\t")):
            continuation = raw_line.strip()
            if continuation:
                current[current_field] = f"{current.get(current_field, '')} {continuation}".strip()
        elif raw_line.lstrip().startswith("- ") or raw_line.startswith("#"):
            current = None
            current_field = None
    return entries


def read_trx_outcomes(results_directory: Path) -> dict[str, list[str]]:
    outcomes: dict[str, list[str]] = defaultdict(list)
    trx_files = sorted(results_directory.rglob("*.trx"))
    if not trx_files:
        raise VerificationError(f"no TRX files found under {results_directory}")
    for trx_path in trx_files:
        root = ET.parse(trx_path).getroot()
        for element in root.iter():
            if element.tag.rsplit("}", 1)[-1] != "UnitTestResult":
                continue
            test_name = element.attrib.get("testName", "").strip()
            outcome = element.attrib.get("outcome", "").strip()
            if test_name:
                outcomes[test_name].append(outcome)
    return outcomes


def verify_closure(
    targets_path: Path,
    source_root: Path,
    deferred_work_path: Path,
    results_directory: Path,
    *,
    expected_count: int = EXPECTED_TARGET_COUNT,
    priority_originals: set[str] = PRIORITY_ORIGINALS,
) -> VerificationReport:
    rows = read_targets(targets_path, expected_count)
    sources = discover_source_dispositions(source_root)
    outcomes = read_trx_outcomes(results_directory)
    deferred_entries = read_deferred_entries(deferred_work_path) if deferred_work_path.exists() else {}

    errors: list[str] = []
    report_rows: list[tuple[str, str, str]] = []
    passed = skipped = failed = 0

    for row in rows:
        final_sources = sources.get(row.final, [])
        if len(final_sources) != 1:
            errors.append(
                f"{row.original}: final target {row.final} does not resolve exactly once in source (found {len(final_sources)})"
            )
            continue

        if row.original != row.final and sources.get(row.original):
            errors.append(
                f"{row.original}: replacement mapping is ambiguous because the original method still exists"
            )

        source = final_sources[0]
        if not source.skipped and source.no_op:
            errors.append(f"{row.final}: runnable target has an assertion-free no-op body")

        result_values = outcomes.get(row.final, [])
        if len(result_values) != 1:
            errors.append(
                f"{row.original}: final target {row.final} has {'no' if not result_values else 'multiple'} TRX result(s)"
            )
            continue
        outcome = result_values[0]

        if source.skipped:
            reason = source.reason or ""
            deferred_ids = DEFERRED_ID_RE.findall(reason)
            matching_entries = deferred_entries.get(deferred_ids[0], []) if len(deferred_ids) == 1 else []
            if len(matching_entries) != 1:
                errors.append(f"{row.final}: skip is not linked to exactly one accepted deferred-work ID")
            else:
                entry = matching_entries[0]
                if entry.get("status", "").lower() != "accepted":
                    errors.append(f"{row.final}: deferred-work entry status must be accepted")
                rationale = entry.get("rationale", "")
                if "Owner:" not in rationale:
                    errors.append(f"{row.final}: deferred-work rationale must name Owner")
                if not entry.get("re-open trigger", "").strip():
                    errors.append(f"{row.final}: deferred-work entry must define a Re-open trigger")
            if "Owner:" not in reason or "Unskip when:" not in reason:
                errors.append(f"{row.final}: skip reason must name Owner and Unskip when")
            if outcome not in {"NotExecuted", "Skipped"}:
                errors.append(f"{row.final}: source is skipped but TRX outcome is {outcome!r}")
                failed += 1
                report_rows.append((row.original, row.final, outcome or "Missing"))
            else:
                skipped += 1
                report_rows.append((row.original, row.final, "Skipped"))
            if row.original in priority_originals:
                errors.append(f"{row.original}: priority target must be Passed, not skipped")
        else:
            if outcome != "Passed":
                errors.append(f"{row.final}: runnable target outcome is {outcome!r}, expected 'Passed'")
                failed += 1
            else:
                passed += 1
            report_rows.append((row.original, row.final, outcome or "Missing"))

    if errors:
        raise VerificationError("\n".join(errors))

    return VerificationReport(passed, skipped, failed, tuple(report_rows))


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--targets", type=Path, required=True)
    parser.add_argument("--results-directory", type=Path, required=True)
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--deferred-work", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    repo_root = Path(__file__).resolve().parent.parent
    source_root = args.source_root or repo_root / "tests" / "Hexalith.Memories.IntegrationTests"
    deferred_work = args.deferred_work or repo_root / "_bmad-output" / "implementation-artifacts" / "deferred-work.md"
    try:
        report = verify_closure(
            args.targets,
            source_root,
            deferred_work,
            args.results_directory,
        )
    except (OSError, ET.ParseError, VerificationError) as error:
        print(f"integration-stub closure verification failed:\n{error}", file=sys.stderr)
        return 1

    print("original|final|outcome")
    for original, final, outcome in report.rows:
        print(f"{original}|{final}|{outcome}")
    print(f"summary: passed={report.passed} skipped={report.skipped} failed={report.failed}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
