import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
BENCHMARK_PROJECT = "tests/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.csproj"


class TestRunnerContractTests(unittest.TestCase):
    def test_benchmark_inventory_represents_all_seventeen_tests(self):
        inventory = [
            line.strip()
            for line in (REPO_ROOT / "tools" / "test-projects.benchmark.txt")
            .read_text(encoding="utf-8")
            .splitlines()
            if line.strip() and not line.lstrip().startswith("#")
        ]
        expected_counts = {
            "BenchmarkSuiteTests.cs": 3,
            "NdcgScorerTests.cs": 10,
            "BenchmarkSeederTests.cs": 4,
        }
        actual_counts = {}
        for path in (REPO_ROOT / "tests" / "Hexalith.Memories.Benchmarks").rglob("*Tests.cs"):
            count = len(re.findall(r"^\s*\[Fact(?:\([^\n]*\))?\]", path.read_text(encoding="utf-8"), re.MULTILINE))
            if count:
                actual_counts[path.name] = count

        self.assertEqual([BENCHMARK_PROJECT], inventory)
        self.assertEqual(expected_counts, actual_counts)
        self.assertEqual(17, sum(actual_counts.values()))
        suite_source = (
            REPO_ROOT / "tests" / "Hexalith.Memories.Benchmarks" / "BenchmarkSuiteTests.cs"
        ).read_text(encoding="utf-8")
        self.assertIn("ThesisValidation_HybridOutperforms80Percent", suite_source)
        self.assertIn("ReproducibilityTest_SameDatasetProducesIdenticalScores", suite_source)
        self.assertIn("ThesisValidated = winRate >= 0.80", suite_source)
        self.assertIn("result.ThesisValidated.ShouldBeTrue", suite_source)
        for score in (
            "HybridNdcg10",
            "SyntacticNdcg10",
            "SemanticNdcg10",
            "GraphNdcg10",
        ):
            self.assertIn(f"r1.{score}.ShouldBe(r2.{score}", suite_source)

    def test_exact_benchmark_selector_is_inventory_only_in_both_wrappers(self):
        wrappers = [
            ["bash", "./tools/test.sh"],
            [shutil.which("pwsh") or "pwsh", "-NoLogo", "-NoProfile", "-File", "./tools/test.ps1"],
        ]
        for wrapper in wrappers:
            with self.subTest(wrapper=wrapper[0]), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                env, log_path = self._fake_dotnet_environment(root)
                if wrapper[0].endswith("pwsh"):
                    command = wrapper + ["-Filter", "Category=Benchmark", "-Configuration", "Release", "-NoBuild"]
                else:
                    command = wrapper + ["--filter", "Category=Benchmark", "--configuration", "Release", "--no-build"]

                result = subprocess.run(
                    command,
                    cwd=REPO_ROOT,
                    env=env,
                    capture_output=True,
                    text=True,
                    check=False,
                )

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                arguments = json.loads(log_path.read_text(encoding="utf-8"))
                self.assertEqual("test", arguments[0])
                self.assertTrue(all("\r" not in argument for argument in arguments))
                self.assertIn(BENCHMARK_PROJECT, arguments)
                self.assertIn("--no-build", arguments)
                self.assertNotIn("--filter", arguments)

    def test_bash_runner_normalizes_crlf_inventory_records(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            tools_dir = root / "tools"
            tools_dir.mkdir()
            runner = tools_dir / "test.sh"
            shutil.copy2(REPO_ROOT / "tools" / "test.sh", runner)
            os.chmod(runner, 0o755)
            (tools_dir / "test-projects.benchmark.txt").write_bytes(
                b"# benchmark projects\r\n"
                + BENCHMARK_PROJECT.encode("utf-8")
                + b"\r\n"
            )
            fake_root = root / "fake-dotnet"
            fake_root.mkdir()
            env, log_path = self._fake_dotnet_environment(fake_root)

            result = subprocess.run(
                [
                    "bash",
                    str(runner),
                    "--filter",
                    "Category=Benchmark",
                    "--configuration",
                    "Release",
                    "--no-build",
                ],
                cwd=root,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            arguments = json.loads(log_path.read_text(encoding="utf-8"))
            self.assertIn(BENCHMARK_PROJECT, arguments)
            self.assertTrue(all("\r" not in argument for argument in arguments))

    def test_non_exact_benchmark_expression_preserves_trait_filter(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, log_path = self._fake_dotnet_environment(root)
            expression = "Category=Benchmark&Category!=Integration"

            result = subprocess.run(
                [
                    "bash",
                    "./tools/test.sh",
                    "--filter",
                    expression,
                    "--configuration",
                    "Release",
                    "--no-build",
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            arguments = json.loads(log_path.read_text(encoding="utf-8"))
            self.assertNotIn(BENCHMARK_PROJECT, arguments)
            filter_index = arguments.index("--filter")
            self.assertEqual(expression, arguments[filter_index + 1])

    def test_benchmark_wrappers_require_exactly_seventeen_executed_tests(self):
        wrappers = [
            ["bash", "./tools/test.sh"],
            [shutil.which("pwsh") or "pwsh", "-NoLogo", "-NoProfile", "-File", "./tools/test.ps1"],
        ]
        test_results_root = REPO_ROOT / "TestResults"
        test_results_root.mkdir(exist_ok=True)
        for wrapper in wrappers:
            with self.subTest(wrapper=wrapper[0]), tempfile.TemporaryDirectory(
                dir=test_results_root,
            ) as temp, tempfile.TemporaryDirectory() as fake_temp:
                root = Path(temp)
                stale = root / "stale" / "coverage.cobertura.xml"
                stale.parent.mkdir()
                stale.write_text("stale", encoding="utf-8")
                env, _ = self._fake_dotnet_environment(
                    Path(fake_temp),
                    executed=16,
                    not_executed=1,
                    exit_code=1,
                )
                relative_results = str(root.relative_to(REPO_ROOT))
                if wrapper[0].endswith("pwsh"):
                    command = wrapper + [
                        "-Filter",
                        "Category=Benchmark",
                        "-Configuration",
                        "Release",
                        "-NoBuild",
                        "-ResultsDirectory",
                        relative_results,
                    ]
                else:
                    command = wrapper + [
                        "--filter",
                        "Category=Benchmark",
                        "--configuration",
                        "Release",
                        "--no-build",
                        "--results-directory",
                        relative_results,
                    ]

                result = subprocess.run(
                    command,
                    cwd=REPO_ROOT,
                    env=env,
                    capture_output=True,
                    text=True,
                    check=False,
                )

                self.assertNotEqual(0, result.returncode)
                self.assertIn("exactly 17", result.stdout + result.stderr)
                self.assertFalse(stale.exists())

    def test_benchmark_wrappers_accept_complete_trx_evidence(self):
        wrappers = [
            ["bash", "./tools/test.sh"],
            [shutil.which("pwsh") or "pwsh", "-NoLogo", "-NoProfile", "-File", "./tools/test.ps1"],
        ]
        test_results_root = REPO_ROOT / "TestResults"
        test_results_root.mkdir(exist_ok=True)
        for wrapper in wrappers:
            with self.subTest(wrapper=wrapper[0]), tempfile.TemporaryDirectory(
                dir=test_results_root,
            ) as temp, tempfile.TemporaryDirectory() as fake_temp:
                root = Path(temp)
                env, _ = self._fake_dotnet_environment(Path(fake_temp), executed=17)
                relative_results = str(root.relative_to(REPO_ROOT))
                if wrapper[0].endswith("pwsh"):
                    command = wrapper + [
                        "-Filter", "Category=Benchmark",
                        "-ResultsDirectory", relative_results,
                    ]
                else:
                    command = wrapper + [
                        "--filter", "Category=Benchmark",
                        "--results-directory", relative_results,
                    ]

                result = subprocess.run(
                    command,
                    cwd=REPO_ROOT,
                    env=env,
                    capture_output=True,
                    text=True,
                    check=False,
                )

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    @staticmethod
    def _fake_dotnet_environment(
        root: Path,
        *,
        executed: int = 1,
        not_executed: int = 0,
        exit_code: int = 0,
    ) -> tuple[dict[str, str], Path]:
        log_path = root / "dotnet-arguments.json"
        script = root / "fake_dotnet.py"
        script.write_text(
            textwrap.dedent(
                """
                import json
                import os
                import sys
                from pathlib import Path

                if len(sys.argv) > 1 and sys.argv[1].endswith("pwsh.dll"):
                    real_dotnet = os.environ["REAL_DOTNET"]
                    os.execv(real_dotnet, [real_dotnet] + sys.argv[1:])

                Path(os.environ["FAKE_DOTNET_LOG"]).write_text(
                    json.dumps(sys.argv[1:]),
                    encoding="utf-8",
                )
                if "--results-directory" in sys.argv:
                    results = Path(sys.argv[sys.argv.index("--results-directory") + 1])
                    logger = sys.argv[sys.argv.index("--logger") + 1]
                    log_name = logger.split("LogFileName=", 1)[1]
                    results.mkdir(parents=True, exist_ok=True)
                    (results / log_name).write_text(
                        '<TestRun><ResultSummary><Counters executed="{executed}" '
                        'notExecuted="{not_executed}" /></ResultSummary></TestRun>',
                        encoding="utf-8",
                    )
                raise SystemExit({exit_code})
                """
            ).format(
                executed=executed,
                not_executed=not_executed,
                exit_code=exit_code,
            ).strip()
            + "\n",
            encoding="utf-8",
        )
        shim = root / "dotnet"
        shim.write_text(f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n', encoding="utf-8")
        os.chmod(shim, 0o755)
        env = os.environ.copy()
        env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
        env["PATH"] = str(root) + os.pathsep + env["PATH"]
        env["FAKE_DOTNET_LOG"] = str(log_path)
        return env, log_path


if __name__ == "__main__":
    unittest.main()
