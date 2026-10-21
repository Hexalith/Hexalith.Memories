"""Offline bounded read retries; no target calls or live retention credit."""

from contextlib import ExitStack
import io
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import Mock, patch
from urllib.error import HTTPError, URLError
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as f


class SequentialReads:
    """Keep clock assertions deterministic; separate regressions exercise all sixteen workers."""

    def __init__(self, *, max_workers):
        if max_workers != 16:
            raise AssertionError("selected read concurrency changed")

    def __enter__(self):
        return self

    def __exit__(self, *args):
        return False

    @staticmethod
    def map(action, values):
        return map(action, values)


class SelectedReadRetryTests(unittest.TestCase):
    def setUp(self):
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        self.elapsed = 0.0
        self.sleeps = []
        self.consumer = object.__new__(f._PlatformRetentionConsumer)
        self.consumer.segments = {"cohort-a": 0}
        self.consumer.check = Mock()
        self.consumer.access = {"memories-access-telemetry": {"DAPR_API_TOKEN": "offline-secret"}, "memories": {"APP_API_TOKEN": "offline-secret"}}
        self.consumer.observation = {"endpoints": {"memories-access-telemetry/dapr": "http://127.0.0.1:12345", "memories": "http://127.0.0.1:12346"}}
        self.keys = ("record/00/a", "record/00/b")
        for item in (patch.object(f.time, "monotonic", lambda: self.elapsed), patch.object(f.time, "sleep", self.sleep),
                     patch.object(f, "fixed_record_keys", lambda *args: self.keys), patch.object(f, "ThreadPoolExecutor", SequentialReads)):
            self.stack.enter_context(item)

    def sleep(self, seconds):
        self.sleeps.append(seconds)
        self.elapsed += seconds

    @staticmethod
    def key(path):
        return unquote(urlsplit(path).path).split("/access-telemetry-store/", 1)[1]

    def test_only_the_failed_key_is_retried_with_the_same_strong_read_and_shared_deadline(self):
        calls = []

        def request(resource, path, **kwargs):
            key = self.key(path)
            calls.append((resource, path, kwargs))
            self.assertTrue(path.endswith("?consistency=strong&metadata.partitionKey=access-telemetry"))
            if len(calls) == 1:
                raise f.TransportUncertain("functional-selected-transport-uncertain")
            return {"key": key}

        self.consumer.request = request
        result = self.consumer.states("offline-run", "cohort-a")

        self.assertEqual([self.keys[0], self.keys[0], self.keys[1]], [self.key(path) for _, path, _ in calls])
        self.assertEqual([2], self.sleeps)
        self.assertEqual(list(self.keys), [item["key"] for item in result])
        self.assertTrue(all(resource == "memories-access-telemetry/dapr" and kwargs == {"missing": True, "read_deadline": 120.0} for resource, _, kwargs in calls))

    def test_persistent_transport_failure_has_three_attempts_and_a_safe_static_reason(self):
        self.consumer.request = Mock(side_effect=f.TransportUncertain("offline-secret-and-private-url"))

        with self.assertRaisesRegex(f.TransportUncertain, "^functional-selected-state-read-transport-exhausted$"):
            self.consumer.states("offline-run", "cohort-a")

        self.assertEqual(3, self.consumer.request.call_count)
        self.assertEqual([2, 5], self.sleeps)
        self.assertEqual([self.keys[0]] * 3, [self.key(call.args[1]) for call in self.consumer.request.call_args_list])

    def test_all_keys_share_one_aggregate_budget_and_late_results_are_refused(self):
        def request(*args, **kwargs):
            self.elapsed += 60
            return {}

        self.consumer.request = Mock(side_effect=request)
        with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-read-aggregate-deadline"):
            self.consumer.states("offline-run", "cohort-a")

        self.assertEqual(2, self.consumer.request.call_count)
        self.assertEqual([120.0, 120.0], [call.kwargs["read_deadline"] for call in self.consumer.request.call_args_list])

    def test_failed_attempt_time_and_backoff_cannot_extend_the_aggregate_budget(self):
        def request(*args, **kwargs):
            if self.consumer.request.call_count == 1:
                self.elapsed += 116
            raise f.TransportUncertain("functional-selected-transport-uncertain")

        self.consumer.request = Mock(side_effect=request)
        with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-read-aggregate-deadline"):
            self.consumer.states("offline-run", "cohort-a")

        self.assertEqual(2, self.consumer.request.call_count)
        self.assertEqual([2], self.sleeps)
        self.assertEqual(118, self.elapsed)

    def test_withdrawal_after_failure_or_during_backoff_admits_no_retry_or_later_key(self):
        for stage in ("failure", "backoff"):
            withdrawn = False
            self.elapsed, self.sleeps = 0.0, []

            def check():
                f._require(not withdrawn, "selected-gate-withdrawn-or-expired")

            def request(*args, **kwargs):
                nonlocal withdrawn
                withdrawn = stage == "failure"
                raise f.TransportUncertain("functional-selected-transport-uncertain")

            def sleep(seconds):
                nonlocal withdrawn
                self.sleep(seconds)
                withdrawn = True

            self.consumer.check = check
            self.consumer.request = Mock(side_effect=request)
            with self.subTest(stage=stage), patch.object(f.time, "sleep", sleep):
                with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-gate-withdrawn-or-expired"):
                    self.consumer.states("offline-run", "cohort-a")
            self.assertEqual(1, self.consumer.request.call_count)
            self.assertEqual([] if stage == "failure" else [2], self.sleeps)

    def test_authentication_security_and_server_http_failures_are_never_retried(self):
        self.keys = self.keys[:1]
        for status in (401, 403, 429, 500):
            with self.subTest(status=status), HTTPError("http://127.0.0.1:12345", status, "offline", {}, None) as error:
                with patch("urllib.request.OpenerDirector.open", side_effect=error) as contact:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-request-failed"):
                        self.consumer.states("offline-run", "cohort-a")
                self.assertEqual(1, contact.call_count)
                self.assertEqual([], self.sleeps)

    def test_malformed_json_and_absence_are_not_transport_retries(self):
        self.keys = self.keys[:1]
        response = io.BytesIO(b"{")
        with patch("urllib.request.OpenerDirector.open", return_value=response) as contact:
            with self.assertRaises(json.JSONDecodeError):
                self.consumer.states("offline-run", "cohort-a")
        self.assertEqual(1, contact.call_count)
        with patch("urllib.request.OpenerDirector.open", return_value=io.BytesIO(b"")) as contact:
            self.assertEqual([{"key": self.keys[0], "value": None}], self.consumer.states("offline-run", "cohort-a"))
        self.assertEqual(1, contact.call_count)
        self.assertEqual([], self.sleeps)

    def test_http_attempt_timeout_is_capped_by_the_remaining_shared_budget(self):
        self.elapsed = 119.75
        with patch("urllib.request.OpenerDirector.open", return_value=io.BytesIO(b"{}")) as contact:
            self.assertEqual({}, self.consumer.request("memories-access-telemetry/dapr", "/fixed-read", read_deadline=120.0))
        self.assertEqual(0.25, contact.call_args.kwargs["timeout"])

    def test_write_submission_is_single_attempt_with_unchanged_timeout_and_no_read_retry(self):
        with patch("urllib.request.OpenerDirector.open", side_effect=URLError("offline-transport")) as contact:
            with self.assertRaises(f.TransportUncertain):
                self.consumer.request("memories", "/fixed-write", data=b"")
        self.assertEqual(1, contact.call_count)
        self.assertEqual(120, contact.call_args.kwargs["timeout"])
        self.assertEqual([], self.sleeps)
        with patch("urllib.request.OpenerDirector.open") as contact:
            with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-read-aggregate-deadline"):
                self.consumer.request("memories", "/fixed-write", data=b"", read_deadline=120.0)
        contact.assert_not_called()


if __name__ == "__main__":
    unittest.main()
