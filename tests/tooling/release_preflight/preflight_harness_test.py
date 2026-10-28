import importlib.util
import subprocess
import sys
import tempfile
import typing
import unittest
from pathlib import Path
from unittest import mock

import release_preflight_test as preflight


class PreflightHarnessTests(unittest.TestCase):
    def test_cleanup_retries_transient_permission_errors_and_removes_directory(self):
        with mock.patch.object(tempfile.TemporaryDirectory, "cleanup", autospec=True) as cleanup:
            original_cleanup = ORIGINAL_CLEANUP
            attempts = []

            def transient(directory):
                attempts.append(directory.name)
                if len(attempts) < 3:
                    raise PermissionError("fixture still locked")
                return original_cleanup(directory)

            cleanup.side_effect = transient
            with mock.patch.object(preflight.time, "sleep") as sleep:
                with preflight.temporary_directory() as directory:
                    path = Path(directory)
                    self.assertTrue(path.is_dir())
                    (path / "owned.txt").write_text("fixture", encoding="utf-8")

                self.assertFalse(path.exists())
                self.assertEqual(3, cleanup.call_count)
                self.assertEqual([mock.call(0.05), mock.call(0.1)], sleep.call_args_list)

    def test_cleanup_propagates_permission_error_after_bounded_retries(self):
        original_cleanup = ORIGINAL_CLEANUP
        owned = []

        def locked(directory):
            owned.append(directory)
            raise PermissionError("persistent fixture lock")

        try:
            with mock.patch.object(tempfile.TemporaryDirectory, "cleanup", autospec=True,
                                   side_effect=locked) as cleanup:
                with mock.patch.object(preflight.time, "sleep") as sleep:
                    with self.assertRaisesRegex(PermissionError, "persistent fixture lock"):
                        with preflight.temporary_directory() as directory:
                            path = Path(directory)

                    self.assertTrue(path.is_dir())
                    self.assertEqual(3, cleanup.call_count)
                    self.assertEqual([mock.call(0.05), mock.call(0.1)], sleep.call_args_list)
        finally:
            for directory in owned:
                original_cleanup(directory)

    def test_cleanup_does_not_retry_unrelated_failures(self):
        original_cleanup = ORIGINAL_CLEANUP
        owned = []

        def failed(directory):
            owned.append(directory)
            raise OSError("unrelated cleanup failure")

        try:
            with mock.patch.object(tempfile.TemporaryDirectory, "cleanup", autospec=True,
                                   side_effect=failed) as cleanup:
                with mock.patch.object(preflight.time, "sleep") as sleep:
                    with self.assertRaisesRegex(OSError, "unrelated cleanup failure"):
                        with preflight.temporary_directory():
                            pass
                    self.assertEqual(1, cleanup.call_count)
                    sleep.assert_not_called()
        finally:
            for directory in owned:
                original_cleanup(directory)

    def test_cleanup_preserves_body_exception(self):
        with self.assertRaisesRegex(ValueError, "test assertion failed"):
            with preflight.temporary_directory() as directory:
                path = Path(directory)
                raise ValueError("test assertion failed")
        self.assertFalse(path.exists())

    def test_each_capture_path_decodes_utf8_and_replaces_invalid_bytes(self):
        real_run = subprocess.run
        harness = preflight.ReleasePreflightTests()

        def emit_bytes(command, **kwargs):
            return real_run(
                [sys.executable, "-c",
                 "import os; os.write(1, b'caf\\xc3\\xa9 \\xff'); os.write(2, b'err \\xff')"],
                **{key: value for key, value in kwargs.items()
                   if key in ("text", "capture_output", "encoding", "errors", "check")},
            )

        with preflight.temporary_directory() as directory:
            for method in ("_git", "_run_preflight", "_run_git_fixture"):
                with self.subTest(method=method):
                    with mock.patch.object(preflight.subprocess, "run", side_effect=emit_bytes):
                        result = getattr(harness, method)(Path(directory), "fixture")
                    self.assertEqual(0, result.returncode)
                    self.assertEqual("caf\u00e9 \ufffd", result.stdout)
                    self.assertEqual("err \ufffd", result.stderr)

    def test_working_and_bare_fixture_branches_ignore_ambient_git_default(self):
        harness = preflight.ReleasePreflightTests()
        with preflight.temporary_directory() as directory:
            root = Path(directory)
            config = root / "gitconfig"
            config.write_text("[init]\n\tdefaultBranch = ambient-topic\n", encoding="utf-8")
            env = harness._environment()
            env["GIT_CONFIG_GLOBAL"] = str(config)
            with mock.patch.object(harness, "_environment", side_effect=lambda *args: dict(env)):
                # Prove that the isolated global setting actually affects ordinary Git init.
                control = root / "control"
                control.mkdir()
                harness._git(control, "init")
                self.assertEqual("ambient-topic", harness._git(control, "symbolic-ref", "--short", "HEAD").stdout.strip())

                working = root / "work"
                harness._init_repo(working)
                bare = root / "remote.git"
                harness._init_bare_repo(bare)
                for repo in (working, bare):
                    self.assertEqual("main", harness._git(repo, "symbolic-ref", "--short", "HEAD").stdout.strip())

    def test_missing_powershell_skips_execution_before_fixture_creation(self):
        spec = importlib.util.spec_from_file_location("preflight_without_pwsh", preflight.__file__)
        module = importlib.util.module_from_spec(spec)
        with mock.patch("shutil.which", return_value=None):
            spec.loader.exec_module(module)

        result = unittest.TestResult()
        with mock.patch.object(module.tempfile, "TemporaryDirectory",
                               side_effect=AssertionError("fixture must not be created")):
            unittest.TestSuite([module.ReleasePreflightTests("test_no_existing_tag_passes")]).run(result)

        self.assertEqual(1, result.testsRun)
        self.assertEqual([], result.errors)
        self.assertEqual([], result.failures)
        self.assertEqual(1, len(result.skipped))
        self.assertIn("PowerShell 7", result.skipped[0][1])

    def test_available_powershell_keeps_execution_enabled(self):
        spec = importlib.util.spec_from_file_location("preflight_with_pwsh", preflight.__file__)
        module = importlib.util.module_from_spec(spec)
        with mock.patch("shutil.which", return_value=sys.executable):
            spec.loader.exec_module(module)

        result = unittest.TestResult()
        with mock.patch.object(module.ReleasePreflightTests, "test_no_existing_tag_passes", return_value=None) as body:
            unittest.TestSuite([module.ReleasePreflightTests("test_no_existing_tag_passes")]).run(result)

        self.assertEqual(1, result.testsRun)
        self.assertEqual([], result.errors)
        self.assertEqual([], result.failures)
        self.assertEqual([], result.skipped)
        body.assert_called_once_with()

    def test_helper_annotations_resolve_on_the_running_interpreter(self):
        # Run this module on Python 3.9 as well to qualify its minimum runtime.
        for name in ("_git", "_run_preflight", "_run_git_fixture", "_run_npm_fixture"):
            with self.subTest(helper=name):
                hints = typing.get_type_hints(getattr(preflight.ReleasePreflightTests, name))
                self.assertIn("return", hints)


ORIGINAL_CLEANUP = tempfile.TemporaryDirectory.cleanup


if __name__ == "__main__":
    unittest.main()
