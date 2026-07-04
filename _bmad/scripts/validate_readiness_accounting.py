#!/usr/bin/env python3
"""Validate sprint readiness metadata without third-party dependencies."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


DONE = "done"
NOT_SELECTED = {"backlog", "optional"}
SELECTED = {"ready-for-dev", "in-progress", "review", "done"}
EXPECTED_ACTIVE_MVP = [f"epic-{index}" for index in range(0, 9)]
ORDERED_STORIES = {
    "epic-17": [
        "17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening",
        "17-2-recovery-and-feedback-state-grammar",
        "17-3-contract-aware-web-interaction-patterns",
        "17-4-role-specific-web-inspection-lenses",
        "17-5-responsive-and-accessible-web-validation",
    ],
    "epic-18": [
        "18-6-memory-unit-id-stability-contract",
        "18-5-source-uri-keyed-memory-unit-lookup-endpoint",
    ],
    "epic-23": [
        "23-9-embeddingclient-provider-strategy",
        "23-1-content-chunking-and-batch-embedding",
    ],
}


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except OSError as error:
        raise SystemExit(f"error: failed to read {path}: {error}") from error


def parse_development_status(text: str) -> dict[str, str]:
    match = re.search(r"(?ms)^development_status:\n(?P<body>.*?)(?:\n\S|\Z)", text)
    if not match:
        raise SystemExit("error: development_status section not found")

    statuses: dict[str, str] = {}
    for line in match.group("body").splitlines():
        row = re.match(r"^  (?P<key>[0-9a-z][0-9a-z-]*):\s+(?P<status>[A-Za-z0-9_-]+)", line)
        if row:
            statuses[row.group("key")] = row.group("status")
    return statuses


def parse_epic_metadata(text: str) -> set[str]:
    match = re.search(r"(?ms)^  epic_metadata:\n(?P<body>.*?)(?:^  [a-zA-Z_].*?:\n|^\S|\Z)", text)
    if not match:
        raise SystemExit("error: readiness_accounting.epic_metadata section not found")
    return set(re.findall(r"^    (epic-\d+):", match.group("body"), flags=re.MULTILINE))


def parse_active_mvp_epics(text: str) -> list[str]:
    match = re.search(r"(?ms)^  active_mvp_epics:\n(?P<body>(?:    - epic-\d+\n)+)", text)
    if not match:
        raise SystemExit("error: readiness_accounting.active_mvp_epics section not found")
    return re.findall(r"epic-\d+", match.group("body"))


def parse_order_block(text: str, epic: str) -> list[str]:
    pattern = rf"(?ms)^  {re.escape(epic)}:\n.*?^    order:\n(?P<body>(?:      - [0-9a-z][0-9a-z-]*\n)+)"
    match = re.search(pattern, text)
    if not match:
        raise SystemExit(f"error: story_execution_order.{epic}.order section not found")
    return [line.strip()[2:] for line in match.group("body").splitlines()]


def active_mvp_text(epics_text: str) -> str:
    start = re.search(r"(?m)^## Epic 0:", epics_text)
    end = re.search(r"(?m)^## Phase 2 Backlog Placeholders", epics_text)
    if not start or not end or end.start() <= start.start():
        raise SystemExit("error: could not isolate active MVP epic text")
    return epics_text[start.start():end.start()]


def validate_status(sprint_text: str) -> list[str]:
    errors: list[str] = []
    statuses = parse_development_status(sprint_text)
    metadata_epics = parse_epic_metadata(sprint_text)
    active_epics = parse_active_mvp_epics(sprint_text)

    if active_epics != EXPECTED_ACTIVE_MVP:
        errors.append(
            "active_mvp_epics must be exactly "
            + ", ".join(EXPECTED_ACTIVE_MVP)
            + f"; found {', '.join(active_epics)}"
        )

    status_epics = {
        key
        for key in statuses
        if re.fullmatch(r"epic-\d+", key)
    }
    missing_metadata = sorted(status_epics - metadata_epics)
    if missing_metadata:
        errors.append("missing epic_metadata rows: " + ", ".join(missing_metadata))

    if "8-3-data-export" not in sprint_text or "reserved-non-mvp" not in sprint_text:
        errors.append("Story 8.3 reserved non-MVP override is missing")

    for epic, expected_order in ORDERED_STORIES.items():
        actual_order = parse_order_block(sprint_text, epic)
        for story in expected_order:
            if story not in actual_order:
                errors.append(f"{epic} order is missing {story}")
        expected_positions = [actual_order.index(story) for story in expected_order if story in actual_order]
        if expected_positions != sorted(expected_positions):
            errors.append(f"{epic} story_execution_order does not preserve required prerequisite order")

        previous_story = None
        for story in actual_order:
            status = statuses.get(story)
            if previous_story and status in SELECTED and statuses.get(previous_story) != DONE:
                errors.append(
                    f"{story} is {status}, but prerequisite {previous_story} is "
                    f"{statuses.get(previous_story, 'missing')}"
                )
            if status not in NOT_SELECTED:
                previous_story = story

    return errors


def validate_epics(epics_text: str) -> list[str]:
    errors: list[str] = []
    mvp_text = active_mvp_text(epics_text).lower()
    forbidden_patterns = [
        r"implemented,\s+documented,\s+accepted,\s+or\s+carried\s+forward",
        r"accepted\s+or\s+carried\s+forward",
    ]
    for pattern in forbidden_patterns:
        if re.search(pattern, mvp_text):
            errors.append(f"forbidden product-story acceptance phrase found: {pattern}")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--sprint-status",
        default="_bmad-output/implementation-artifacts/sprint-status.yaml",
        help="Path to sprint-status.yaml.",
    )
    parser.add_argument(
        "--epics",
        default="_bmad-output/planning-artifacts/epics.md",
        help="Path to epics.md.",
    )
    args = parser.parse_args(argv)

    sprint_text = read_text(Path(args.sprint_status))
    epics_text = read_text(Path(args.epics))

    errors = validate_status(sprint_text) + validate_epics(epics_text)
    if errors:
        for error in errors:
            print(f"error: {error}", file=sys.stderr)
        return 1

    print("Readiness accounting validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
