"""Offline bounded read retries; no target calls or live retention credit."""

from contextlib import ExitStack
from concurrent.futures import ThreadPoolExecutor
import http.client
import io
import json
from pathlib import Path
import sys
import tempfile
from threading import Barrier
import types
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
        directory = self.stack.enter_context(tempfile.TemporaryDirectory())
        self.consumer.root = Path(directory)
        (self.consumer.root / "native-tests").mkdir(mode=0o700)
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
        root = self.consumer.root
        for stage in ("failure", "backoff"):
            self.consumer.root = root / stage
            (self.consumer.root / "native-tests").mkdir(parents=True, mode=0o700)
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

    def test_response_body_transport_failures_retry_the_same_strong_read(self):
        failures = (
            TimeoutError("offline-secret-timeout"),
            ConnectionResetError("offline-secret-reset"),
            ConnectionAbortedError("offline-secret-aborted"),
            http.client.IncompleteRead(b"offline-secret-partial"),
            http.client.RemoteDisconnected("offline-secret-disconnected"),
            http.client.BadStatusLine("offline-secret-status"),
        )
        for failure in failures:
            self.elapsed, self.sleeps = 0.0, []
            response = io.BytesIO()
            response.read = Mock(side_effect=failure)
            with self.subTest(failure=type(failure).__name__), patch(
                "urllib.request.OpenerDirector.open",
                side_effect=(response, io.BytesIO(b'{"record": "first"}'), io.BytesIO(b'{"record": "second"}')),
            ) as contact:
                observed = self.consumer.states("offline-run", "cohort-a")
            urls = [call.args[0].full_url for call in contact.call_args_list]
            self.assertEqual([self.keys[0], self.keys[0], self.keys[1]], [self.key(url) for url in urls])
            self.assertTrue(all(url.endswith("?consistency=strong&metadata.partitionKey=access-telemetry") for url in urls))
            self.assertEqual([{"record": "first"}, {"record": "second"}], [item["value"] for item in observed])
            self.assertEqual([2], self.sleeps)
            self.assertTrue(response.closed)
            response.read.assert_called_once_with(1048577)
            self.assertEqual([5, 5, 5], [call.kwargs["timeout"] for call in contact.call_args_list])

    def test_persistent_response_body_failure_exhausts_read_retries_with_sanitized_reason(self):
        responses = [io.BytesIO() for _ in range(3)]
        for response in responses:
            response.read = Mock(side_effect=TimeoutError("offline-secret-and-private-url"))
        with patch("urllib.request.OpenerDirector.open", side_effect=responses) as contact:
            with self.assertRaisesRegex(f.TransportUncertain, "^functional-selected-state-read-transport-exhausted$") as rejected:
                self.consumer.states("offline-run", "cohort-a")
        self.assertTrue(rejected.exception.__suppress_context__)
        self.assertEqual([self.keys[0]] * 3, [self.key(call.args[0].full_url) for call in contact.call_args_list])
        self.assertEqual([2, 5], self.sleeps)
        self.assertTrue(all(response.closed for response in responses))

    def test_failed_read_retains_only_bounded_private_transport_categories(self):
        failure = URLError(ConnectionRefusedError("offline-secret-url-key-payload"))
        with patch("urllib.request.OpenerDirector.open", side_effect=failure) as contact:
            with self.assertRaisesRegex(f.TransportUncertain, "^functional-selected-state-read-transport-exhausted$"):
                self.consumer.states("offline-run", "cohort-a")
        path = self.consumer.root / "native-tests/state-read-transport-failure.json"
        raw = path.read_text()
        observed = json.loads(raw)
        self.assertEqual(0o600, path.stat().st_mode & 0o777)
        self.assertLess(len(raw), 1048576)
        self.assertEqual({"runId", "segmentId", "kind", "attemptLimit", "aggregateLimitSeconds", "failures"}, set(observed))
        self.assertEqual(("offline-run", "cohort-a", "record", 3, 120),
                         tuple(observed[name] for name in ("runId", "segmentId", "kind", "attemptLimit", "aggregateLimitSeconds")))
        self.assertEqual([1, 2, 3], [item["attempt"] for item in observed["failures"]])
        self.assertEqual([0, 2000, 7000], [item["elapsedMilliseconds"] for item in observed["failures"]])
        for item in observed["failures"]:
            self.assertEqual({"ordinal", "attempt", "observedUtcMs", "elapsedMilliseconds", "category", "phase"}, set(item))
            self.assertEqual((1, "connection-refused", "open"), (item["ordinal"], item["category"], item["phase"]))
        for private in ("offline-secret", "12345", self.keys[0], "dapr-api-token", "payload", "http://"):
            self.assertNotIn(private, raw)
        self.assertEqual(3, contact.call_count)
        self.assertEqual([2, 5], self.sleeps)

    def test_body_failure_diagnostic_does_not_retain_partial_response_or_exception_values(self):
        responses = [io.BytesIO() for _ in range(3)]
        for response in responses:
            response.read = Mock(side_effect=http.client.IncompleteRead(b"offline-secret-partial"))
        with patch("urllib.request.OpenerDirector.open", side_effect=responses):
            with self.assertRaises(f.TransportUncertain):
                self.consumer.states("offline-run", "cohort-a")
        raw = (self.consumer.root / "native-tests/state-read-transport-failure.json").read_text()
        self.assertNotIn("offline-secret", raw)
        self.assertEqual({("incomplete-read", "body")}, {(item["category"], item["phase"]) for item in json.loads(raw)["failures"]})

    def test_recovered_read_creates_no_failure_artifact(self):
        with patch("urllib.request.OpenerDirector.open", side_effect=(URLError("private-reason"), io.BytesIO(b"{}"), io.BytesIO(b"{}"))):
            self.consumer.states("offline-run", "cohort-a")
        self.assertFalse((self.consumer.root / "native-tests/state-read-transport-failure.json").exists())

    def test_phase_timing_retains_failed_and_unobserved_ordinals_without_exception_text(self):
        self.consumer.request = Mock(side_effect=f.TransportUncertain("offline-secret-url-key-payload"))
        with self.assertRaisesRegex(f.TransportUncertain, "^functional-selected-state-read-transport-exhausted$"):
            self.consumer.states("offline-run", "cohort-a", phase="initial")
        path = self.consumer.root / "native-tests/state-read-initial-cohort-a-record-01.json"
        raw = path.read_text()
        observed = json.loads(raw)
        self.assertEqual(("failed", 2, 0, 0), tuple(observed[name] for name in ("status", "keyCount", "missingCount", "nonObjectCount")))
        failed, pending = observed["reads"]
        self.assertEqual((1, "failed", 3, 0, 7), tuple(failed[name] for name in ("ordinal", "status", "attempts", "startedMonotonicSeconds", "completedMonotonicSeconds")))
        self.assertIsNone(failed["responseObservedUtcMs"])
        self.assertIsNone(failed["responseObservedMonotonicSeconds"])
        self.assertEqual((2, "unobserved", 0), tuple(pending[name] for name in ("ordinal", "status", "attempts")))
        self.assertIsNone(pending["startedUtcMs"])
        self.assertIsNone(pending["completedUtcMs"])
        self.assertEqual(0o600, path.stat().st_mode & 0o777)
        for private in ("offline-secret", "http://", *self.keys):
            self.assertNotIn(private, raw)

    def test_diagnostic_phase_and_poll_scope_refuse_unbounded_or_untrusted_names(self):
        self.consumer.request = Mock()
        for phase, observation in (("offline-secret", 1), ("pre-expiry-3540", 1), ("pre-expiry-3590", 1), ("initial", 0), ("initial", 33), ("initial", True)):
            with self.subTest(phase=phase, observation=observation), self.assertRaisesRegex(
                    f.FunctionalRetentionError, "^functional-selected-read-observation-scope$"):
                self.consumer.states("offline-run", "cohort-a", phase=phase, observation=observation)
        self.consumer.request.assert_not_called()
        self.assertEqual([], list((self.consumer.root / "native-tests").iterdir()))

    def preexpiry(self, label, expiry=10):
        self.consumer.root = self.consumer.root / label
        (self.consumer.root / "native-tests").mkdir(parents=True, mode=0o700)
        self.keys = self.keys[:1]
        self.elapsed, self.sleeps, self.utc = 0.0, [], 0.0
        self.consumer.validated_expiries = {"cohort-a": {1: int(expiry * 1000)}}
        self.consumer.segment_monotonic = {"cohort-a": 0.0}
        self.stack.enter_context(patch.object(f.time, "time", lambda: self.utc))

    def test_preexpiry_response_boundary_checks_both_utc_and_monotonic(self):
        for label, utc, monotonic, accepted in (("before", 9.999, 9.999, True),
                                               ("utc-at", 10, 9.999, False), ("mono-at", 9.999, 10, False),
                                               ("both-after", 10.001, 10.001, False)):
            self.preexpiry(label)
            def request(*args, **kwargs):
                self.assertEqual(10, kwargs["read_deadline"])
                self.utc, self.elapsed = utc, monotonic
                return {}
            self.consumer.request = Mock(side_effect=request)
            with self.subTest(label=label):
                if accepted:
                    self.assertEqual({}, self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")[0]["value"])
                else:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-preexpiry-window$"):
                        self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")
            packet = json.loads((self.consumer.root / "native-tests/state-read-pre-expiry-3450-cohort-a-record-01.json").read_text())
            self.assertEqual("completed" if accepted else "failed", packet["status"])
            self.assertEqual((int(utc * 1000), monotonic), (packet["reads"][0]["responseObservedUtcMs"], packet["reads"][0]["responseObservedMonotonicSeconds"]))
            self.assertEqual("object", packet["reads"][0]["status"])

    def test_preexpiry_late_dispatch_or_scheduler_delay_refuses_before_any_request(self):
        for label, utc, monotonic, scheduler in (("utc", 10, 0, False), ("mono", 0, 10, False), ("scheduler", 0, 0, True)):
            self.preexpiry(label)
            self.utc, self.elapsed = utc, monotonic
            self.consumer.request = Mock()
            class DelayedReads(SequentialReads):
                @staticmethod
                def map(action, values):
                    self.elapsed = 10
                    return map(action, values)
            with self.subTest(label=label), patch.object(f, "ThreadPoolExecutor", DelayedReads if scheduler else SequentialReads):
                with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-preexpiry-window$"):
                    self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")
            self.consumer.request.assert_not_called()
            packet = json.loads((self.consumer.root / "native-tests/state-read-pre-expiry-3450-cohort-a-record-01.json").read_text())
            self.assertEqual("failed", packet["status"])
            self.assertEqual(0, packet["reads"][0]["attempts"])

    def test_preexpiry_retries_share_the_clamped_expiry_and_backoff_cannot_cross_it(self):
        self.preexpiry("retry", expiry=6)
        def request(*args, **kwargs):
            self.assertEqual(6, kwargs["read_deadline"])
            self.elapsed += 4
            self.utc = self.elapsed
            raise f.TransportUncertain("private-response-secret")
        self.consumer.request = Mock(side_effect=request)
        with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-preexpiry-window$"):
            self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")
        self.assertEqual(1, self.consumer.request.call_count)
        self.assertEqual([], self.sleeps)
        raw = (self.consumer.root / "native-tests/state-read-transport-failure.json").read_text()
        self.assertNotIn("private-response-secret", raw)

    def test_preexpiry_http_deadline_uses_the_smaller_utc_remaining_window(self):
        self.preexpiry("utc-remaining")
        self.utc = 9.75
        self.consumer.request = Mock(return_value={})
        self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")
        self.assertEqual(0.25, self.consumer.request.call_args.kwargs["read_deadline"])

    def test_preexpiry_sql_is_clamped_refuses_late_completion_and_retains_private_timing(self):
        for label, utc, monotonic, dispatch, accepted in (("before", 9.999, 9.999, 9.75, True),
                                                         ("utc-at", 10, 9.999, 9.75, False),
                                                         ("mono-at", 9.999, 10, 9.75, False),
                                                         ("dispatch-at", 10, 10, 10, False)):
            self.preexpiry(label)
            self.utc = self.elapsed = dispatch
            self.consumer.observation["postgresqlContainerId"] = "d" * 64
            def sql(*args, **kwargs):
                self.assertEqual(0.25, kwargs["timeout"])
                self.utc, self.elapsed = utc, monotonic
                return types.SimpleNamespace(stdout=json.dumps({"serverVersionNum": "180006", "ssl": True, "tlsVersion": "TLSv1.3",
                    "role": "telemetry_observer", "readOnly": "on", "observedUtcMs": int(utc * 1000), "rows": []}).encode(), stderr=b"")
            with self.subTest(label=label), patch.object(f.subprocess, "run", side_effect=sql) as contact:
                if accepted:
                    self.assertEqual([], self.consumer.physical("offline-run", pre_expiry_segment="cohort-a")["rows"])
                else:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-preexpiry-window$"):
                        self.consumer.physical("offline-run", pre_expiry_segment="cohort-a")
                self.assertEqual(0 if label == "dispatch-at" else 1, contact.call_count)
            path = self.consumer.root / "native-tests/pre-expiry-physical-cohort-a.json"
            packet = json.loads(path.read_text())
            self.assertEqual("completed" if accepted else "failed", packet["status"])
            self.assertEqual(0o600, path.stat().st_mode & 0o777)
            self.assertEqual({"segmentId", "status", "startedUtcMs", "startedMonotonicSeconds", "completedUtcMs", "completedMonotonicSeconds",
                              "expectedExpiryUtcMs", "expectedExpiryMonotonicSeconds"}, set(packet))
            for private in ("offline-secret", "docker", "psql", "127.0.0.1", "telemetry_observer"):
                self.assertNotIn(private, path.read_text())

    def test_preexpiry_scope_refuses_other_segments_and_buckets_before_requests(self):
        self.preexpiry("scope")
        self.consumer.segments["cohort-b"] = 0
        self.consumer.request = Mock()
        for segment, buckets in (("cohort-b", False), ("cohort-a", True)):
            with self.subTest(segment=segment, buckets=buckets), self.assertRaisesRegex(
                    f.FunctionalRetentionError, "^functional-selected-read-observation-scope$"):
                self.consumer.states("offline-run", segment, buckets=buckets, phase="pre-expiry-3450")
        self.consumer.request.assert_not_called()
        self.assertEqual([], list((self.consumer.root / "native-tests").iterdir()))

    def test_preexpiry_early_response_survives_slow_guard_but_shared_budget_still_applies(self):
        for label, value, guard_delay, reason in (("object", {}, 2, None), ("missing", None, 2, "record-missing"),
                                                  ("aggregate", None, 121, "read-aggregate-deadline")):
            self.preexpiry(label)
            returned = False
            def request(*args, **kwargs):
                nonlocal returned
                self.utc = self.elapsed = 9
                returned = True
                return value
            def check():
                nonlocal returned
                if returned:
                    returned = False
                    self.elapsed += guard_delay
                    self.utc = self.elapsed
            self.consumer.check = check
            self.consumer.request = Mock(side_effect=request)
            with self.subTest(label=label):
                if reason:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-" + reason + "$"):
                        self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")
                else:
                    self.assertEqual({}, self.consumer.states("offline-run", "cohort-a", phase="pre-expiry-3450")[0]["value"])
            packet = json.loads((self.consumer.root / "native-tests/state-read-pre-expiry-3450-cohort-a-record-01.json").read_text())
            self.assertEqual(9000, packet["reads"][0]["responseObservedUtcMs"])
            self.assertGreater(packet["reads"][0]["completedUtcMs"], 10000)

    def test_failure_artifact_is_exclusive_and_does_not_overwrite_previous_evidence(self):
        path = self.consumer.root / "native-tests/state-read-transport-failure.json"
        path.write_bytes(b"retained-authentic-evidence")
        self.consumer.request = Mock(side_effect=f.TransportUncertain("private-reason"))
        with self.assertRaises(FileExistsError):
            self.consumer.states("offline-run", "cohort-a")
        self.assertEqual(b"retained-authentic-evidence", path.read_bytes())
        self.assertEqual(3, self.consumer.request.call_count)

    def test_untrusted_exception_metadata_is_replaced_with_fixed_categories(self):
        error = f.TransportUncertain("private-reason")
        error.category = "http://private-token-and-key"
        error.phase = "private-response"
        self.consumer.request = Mock(side_effect=error)
        with self.assertRaises(f.TransportUncertain):
            self.consumer.states("offline-run", "cohort-a")
        raw = (self.consumer.root / "native-tests/state-read-transport-failure.json").read_text()
        self.assertNotIn("private", raw)
        self.assertEqual({("unclassified", "unknown")}, {(item["category"], item["phase"]) for item in json.loads(raw)["failures"]})

    def test_parallel_failure_is_bounded_to_the_existing_batch_and_sorted_without_keys(self):
        self.keys = tuple("record/00/private-" + str(index) for index in range(125))
        barrier = Barrier(16)
        def request(*args, **kwargs):
            barrier.wait(timeout=5)
            raise f.TransportUncertain("private-reason")
        self.consumer.request = Mock(side_effect=request)
        with patch.object(f, "ThreadPoolExecutor", ThreadPoolExecutor), patch.object(f.time, "sleep", lambda seconds: None):
            with self.assertRaises(f.TransportUncertain):
                self.consumer.states("offline-run", "cohort-a")
        raw = (self.consumer.root / "native-tests/state-read-transport-failure.json").read_text()
        observed = json.loads(raw)
        self.assertNotIn("private", raw)
        self.assertEqual(48, self.consumer.request.call_count)
        self.assertEqual([(ordinal, attempt) for ordinal in range(1, 17) for attempt in range(1, 4)],
                         [(item["ordinal"], item["attempt"]) for item in observed["failures"]])
        self.assertLess(len(raw), 1048576)

    def test_response_output_limit_is_refused_without_retry(self):
        with patch("urllib.request.OpenerDirector.open", return_value=io.BytesIO(b"x" * 1048577)) as contact:
            with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-output-limit$"):
                self.consumer.states("offline-run", "cohort-a")
        self.assertEqual(1, contact.call_count)
        self.assertEqual([], self.sleeps)

    def test_write_response_body_failure_is_sanitized_and_never_resubmitted(self):
        response = io.BytesIO()
        response.read = Mock(side_effect=http.client.IncompleteRead(b"offline-secret-partial"))
        with patch("urllib.request.OpenerDirector.open", return_value=response) as contact:
            with self.assertRaisesRegex(f.TransportUncertain, "^functional-selected-transport-uncertain$") as rejected:
                self.consumer.request("memories", "/fixed-write", data=b"")
        self.assertTrue(rejected.exception.__suppress_context__)
        self.assertEqual(1, contact.call_count)
        self.assertEqual(120, contact.call_args.kwargs["timeout"])
        self.assertEqual([], self.sleeps)
        self.assertTrue(response.closed)

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
