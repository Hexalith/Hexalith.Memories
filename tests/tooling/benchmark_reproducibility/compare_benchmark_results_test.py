import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[3] / "tools" / "compare-benchmark-results.py"
RESULT = {
    "runTimestamp": "2026-11-26T12:00:00+00:00",
    "totalQueries": 1,
    "hybridWins": 1,
    "hybridWinRate": 1,
    "thesisValidated": True,
    "caveat": "Synthetic vectors",
    "queryResults": [{
        "queryId": "BQ-01",
        "queryDescription": "Example benchmark query",
        "hybridNdcg10": 0.9,
        "syntacticNdcg10": 0.6,
        "semanticNdcg10": 0.7,
        "graphNdcg10": 0.4,
        "graphAxisActive": True,
        "hybridPrecisionAt3": 1,
        "bestSingleAxisPrecisionAt3": 0.5,
        "hybridOutperforms": True,
    }],
}


class CompareBenchmarkResultsTests(unittest.TestCase):
    def compare(self, first, second):
        with tempfile.TemporaryDirectory() as folder:
            paths = [Path(folder) / "first.json", Path(folder) / "second.json"]
            for path, value in zip(paths, (first, second)):
                if value is not None:
                    path.write_text(value, encoding="utf-8")
            return subprocess.run(
                [sys.executable, str(SCRIPT), *map(str, paths)],
                capture_output=True, text=True, check=False,
            )

    def test_normalizes_object_order_and_timestamp(self):
        later = dict(reversed(list(RESULT.items())))
        later["runTimestamp"] = "2026-11-26T12:05:00+00:00"
        self.assertEqual(self.compare(json.dumps(RESULT), json.dumps(later, indent=2)).returncode, 0)

    def test_fails_if_a_per_query_measure_changes(self):
        changed = copy.deepcopy(RESULT)
        changed["queryResults"][0]["hybridPrecisionAt3"] = 0.7
        result = self.compare(json.dumps(RESULT), json.dumps(changed))
        self.assertEqual(result.returncode, 1)
        self.assertIn("independent benchmark results differ", result.stderr)

    def test_fails_on_matching_results_that_misrepresent_the_quality_gate(self):
        incorrect_count = dict(RESULT, totalQueries=True)
        below_threshold = copy.deepcopy(RESULT)
        below_threshold["hybridWins"] = 0
        below_threshold["hybridWinRate"] = 0
        below_threshold["queryResults"][0]["hybridOutperforms"] = False
        for invalid in (incorrect_count, below_threshold):
            with self.subTest(invalid=invalid):
                payload = json.dumps(invalid)
                self.assertEqual(self.compare(payload, payload).returncode, 1)

    def test_fails_on_missing_duplicate_or_partial_data(self):
        incomplete = dict(RESULT, queryResults=[{"queryId": "BQ-01"}])
        duplicate = json.dumps(RESULT).replace('"totalQueries": 1', '"totalQueries": 1, "totalQueries": 1')
        for invalid in (None, "garbage", json.dumps(incomplete), duplicate):
            with self.subTest(invalid=invalid):
                self.assertEqual(self.compare(json.dumps(RESULT), invalid).returncode, 1)


if __name__ == "__main__":
    unittest.main()
