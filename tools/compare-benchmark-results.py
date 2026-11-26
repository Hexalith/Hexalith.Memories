"""Compare independent benchmark results, excluding the run timestamp."""

from datetime import datetime
import json
import math
from pathlib import Path
import sys


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON property: {key}")
        result[key] = value
    return result


def validate_query(query):
    return (
        isinstance(query, dict)
        and all(isinstance(query.get(field), str) and query[field] for field in ("queryId", "queryDescription"))
        and all(type(query.get(field)) is bool for field in ("graphAxisActive", "hybridOutperforms"))
        and all(
            type(query.get(field)) in (int, float) and 0 <= query[field] <= 1
            for field in (
                "hybridNdcg10", "syntacticNdcg10", "semanticNdcg10", "graphNdcg10",
                "hybridPrecisionAt3", "bestSingleAxisPrecisionAt3",
            )
        )
    )


def read_result(path):
    with path.open(encoding="utf-8") as stream:
        result = json.load(stream, object_pairs_hook=unique_object)
    if not isinstance(result, dict):
        raise ValueError(f"{path}: expected a benchmark result object")
    timestamp = result.pop("runTimestamp", None)
    queries = result.get("queryResults")
    if (
        not isinstance(timestamp, str)
        or not timestamp
        or not isinstance(queries, list)
        or not queries
        or result.get("totalQueries") != len(queries)
        or not all(validate_query(query) for query in queries)
        or len({query["queryId"] for query in queries}) != len(queries)
        or type(result.get("hybridWins")) is not int
        or result["hybridWins"] != sum(query["hybridOutperforms"] for query in queries)
        or type(result.get("hybridWinRate")) not in (int, float)
        or not math.isclose(result["hybridWinRate"], result["hybridWins"] / len(queries))
        or not isinstance(result.get("caveat"), str)
        or result.get("thesisValidated") is not True
    ):
        raise ValueError(f"{path}: expected a complete, passing benchmark result")
    if datetime.fromisoformat(timestamp.replace("Z", "+00:00")).tzinfo is None:
        raise ValueError(f"{path}: expected a benchmark timestamp with a time zone")
    return json.dumps(result, sort_keys=True, allow_nan=False)


def main(first, second):
    try:
        if read_result(first) != read_result(second):
            raise ValueError("independent benchmark results differ after removing runTimestamp")
    except (OSError, ValueError) as error:
        print(f"Benchmark reproducibility failed: {error}", file=sys.stderr)
        return 1

    print("Independent benchmark results match.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: compare-benchmark-results.py FIRST_RESULT SECOND_RESULT")
    raise SystemExit(main(Path(sys.argv[1]), Path(sys.argv[2])))
