import json
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CONFIG_PATH = REPO_ROOT / "tests" / "tooling" / "coverage_gate" / "line-coverage-gate.json"
RUNSETTINGS_PATH = REPO_ROOT / "tests" / "tests.runsettings"
CI_WORKFLOW_PATH = REPO_ROOT / ".github" / "workflows" / "ci.yml"
NIGHTLY_WORKFLOW_PATH = REPO_ROOT / ".github" / "workflows" / "nightly.yml"
TEST_README_PATH = REPO_ROOT / "tests" / "README.md"
CONTRIBUTING_PATH = REPO_ROOT / "CONTRIBUTING.md"


class CoverageContractTests(unittest.TestCase):
    def test_configuration_pins_threshold_scope_and_required_evidence(self):
        configuration = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))

        self.assertEqual(76.5, configuration["minimumLineCoveragePercent"])
        self.assertEqual(["src/Hexalith.Memories."], configuration["sourcePathPrefixes"])
        self.assertEqual(["**/obj/**"], configuration["excludedSourcePathPatterns"])
        self.assertEqual(
            [
                "Hexalith.Memories.AccessTelemetry",
                "Hexalith.Memories.AccessTelemetry.Clock",
                "Hexalith.Memories.AccessTelemetry.Contracts",
                "Hexalith.Memories.Cli",
                "Hexalith.Memories.Client.Rest",
                "Hexalith.Memories.Contracts",
                "Hexalith.Memories.EventStore",
                "Hexalith.Memories.Mcp",
                "Hexalith.Memories.Server",
                "Hexalith.Memories.ServiceDefaults",
                "Hexalith.Memories.Telemetry",
                "Hexalith.Memories.Web",
            ],
            configuration["requiredAssemblies"],
        )
        self.assertEqual(
            {
                "Hexalith.Memories.Cli": "src/Hexalith.Memories.Cli/Program.cs",
                "Hexalith.Memories.Mcp": "src/Hexalith.Memories.Mcp/Program.cs",
                "Hexalith.Memories.Server": "src/Hexalith.Memories.Server/Program.cs",
            },
            configuration["requiredFiles"],
        )
        self.assertEqual(
            {
                assembly: [f"src/{assembly}"]
                for assembly in configuration["requiredAssemblies"]
            },
            configuration["assemblySourcePrefixes"],
        )
        self.assertEqual(
            [
                "Hexalith.Memories.Contracts.Tests",
                "Hexalith.Memories.Server.Tests",
                # Story 27.3: registered in the Docker-free unit/contract lane on 2026-07-26, so
                # its Cobertura report is now required evidence. Keep in sync with
                # tools/test-projects.unit-contract.txt and CiTestInventoryTests.
                "Hexalith.Memories.AccessTelemetry.Tests",
                "Hexalith.Memories.Cli.Tests",
                "Hexalith.Memories.Mcp.Tests",
                "Hexalith.Memories.EventStore.Tests",
                "Hexalith.Memories.Web.Tests",
            ],
            configuration["requiredReportProjects"],
        )

    def test_configuration_is_not_excluded_from_version_control(self):
        result = subprocess.run(
            ["git", "check-ignore", "--quiet", str(CONFIG_PATH.relative_to(REPO_ROOT))],
            cwd=REPO_ROOT,
            check=False,
        )

        self.assertNotEqual(0, result.returncode)

    def test_required_report_projects_reference_the_private_coverlet_collector(self):
        configuration = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
        expected_include_assets = (
            "runtime; build; native; contentfiles; analyzers; buildtransitive"
        )

        def local_name(tag: str) -> str:
            return tag.rsplit("}", 1)[-1]

        def child_text(element: ET.Element, name: str) -> str | None:
            child = next(
                (candidate for candidate in element if local_name(candidate.tag) == name),
                None,
            )
            return None if child is None else child.text

        for project_name in configuration["requiredReportProjects"]:
            with self.subTest(project=project_name):
                project_path = REPO_ROOT / "tests" / project_name / f"{project_name}.csproj"
                root = ET.parse(project_path).getroot()
                references: list[tuple[ET.Element, tuple[str, ...]]] = []
                mutators: list[ET.Element] = []

                def collect_references(element: ET.Element, conditions: tuple[str, ...] = ()) -> None:
                    condition = (element.get("Condition") or "").strip()
                    inherited_conditions = conditions + ((condition,) if condition else ())
                    if local_name(element.tag) == "PackageReference":
                        if element.get("Include") == "coverlet.collector":
                            references.append((element, inherited_conditions))
                        if element.get("Update") == "coverlet.collector" or element.get("Remove") == "coverlet.collector":
                            mutators.append(element)
                    for child in element:
                        collect_references(child, inherited_conditions)

                collect_references(root)

                self.assertEqual(
                    1,
                    len(references),
                    f"{project_name}: expected exactly one coverlet.collector PackageReference",
                )
                self.assertEqual(
                    [],
                    mutators,
                    f"{project_name}: coverlet.collector must not be updated or removed conditionally",
                )
                reference, conditions = references[0]
                self.assertEqual(
                    (),
                    conditions,
                    f"{project_name}: coverlet.collector and its ancestors must be unconditional",
                )
                self.assertNotIn(
                    "Version",
                    reference.attrib,
                    f"{project_name}: coverlet.collector must not have a Version attribute",
                )
                self.assertNotIn(
                    "VersionOverride",
                    reference.attrib,
                    f"{project_name}: coverlet.collector must not have a VersionOverride attribute",
                )
                self.assertIsNone(
                    child_text(reference, "Version"),
                    f"{project_name}: coverlet.collector must not have a Version child",
                )
                self.assertIsNone(
                    child_text(reference, "VersionOverride"),
                    f"{project_name}: coverlet.collector must not have a VersionOverride child",
                )
                self.assertIsNone(
                    child_text(reference, "ExcludeAssets"),
                    f"{project_name}: coverlet.collector must not exclude collector assets",
                )
                self.assertEqual(
                    "all",
                    child_text(reference, "PrivateAssets"),
                    f"{project_name}: coverlet.collector PrivateAssets must be all",
                )
                self.assertEqual(
                    expected_include_assets,
                    child_text(reference, "IncludeAssets"),
                    f"{project_name}: coverlet.collector IncludeAssets must match the repository standard",
                )

    def test_runsettings_is_valid_and_collects_only_first_party_production_code(self):
        root = ET.parse(RUNSETTINGS_PATH).getroot()
        configuration = root.find(
            "./DataCollectionRunSettings/DataCollectors/DataCollector/Configuration"
        )

        self.assertIsNotNone(configuration)
        self.assertEqual("cobertura", configuration.findtext("Format"))
        self.assertEqual("[Hexalith.Memories.*]*", configuration.findtext("Include"))
        self.assertEqual(
            "[*.Tests]*,[Hexalith.Memories.TestHelpers]*,"
            "[Hexalith.Memories.Web.Specimens]*,"
            "[Hexalith.Memories.MigrateEmbeddingVectors]*",
            configuration.findtext("Exclude"),
        )
        self.assertEqual("**/obj/**", configuration.findtext("ExcludeByFile"))
        self.assertNotIn("Program.cs", RUNSETTINGS_PATH.read_text(encoding="utf-8"))

    def test_unit_contract_job_collects_validates_and_retains_coverage(self):
        job = self._read_workflow_job(CI_WORKFLOW_PATH, "test-unit-contract")
        steps = self._read_workflow_steps(job)

        timeout_match = re.search(r"^    timeout-minutes: (\d+)$", job, re.MULTILINE)
        self.assertIsNotNone(timeout_match)
        self.assertGreaterEqual(int(timeout_match.group(1)), 30)
        self.assertLessEqual(int(timeout_match.group(1)), 60)
        fixture_command = (
            'python3 -m unittest discover -s tests/tooling/coverage_gate -p "*_test.py"'
        )
        collection_command = (
            'bash ./tools/test.sh --filter "Category!=Integration" --configuration Release '
            "--no-build --coverage --results-directory TestResults/test-unit-contract"
        )
        validation_command = (
            "python3 tools/validate-coverage.py --results-directory "
            "TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json"
        )
        fixture = self._assert_blocking_run_step(
            steps,
            "Run coverage gate fixtures",
            fixture_command,
        )
        collection = self._assert_blocking_run_step(
            steps,
            "Run Docker-free unit and contract tests",
            collection_command,
        )
        validation = self._assert_blocking_run_step(
            steps,
            "Validate scoped line coverage",
            validation_command,
        )
        self.assertLess(job.index(fixture), job.index(collection))
        self.assertLess(job.index(collection), job.index(validation))
        upload = steps["Upload unit and contract test results"]
        self.assertIn("if: always()", upload)
        self.assertIn("uses: actions/upload-artifact@v7", upload)
        self.assertIn("path: TestResults/test-unit-contract/**", upload)
        self.assertIn("if-no-files-found: error", upload)
        self.assertIn("retention-days: 14", upload)

    def test_unit_contract_job_runs_real_package_topology_gate_without_publication(self):
        job = self._read_workflow_job(CI_WORKFLOW_PATH, "test-unit-contract")
        steps = self._read_workflow_steps(job)

        release_fixtures = (
            'python3 -m unittest discover -s tests/tooling/release_packages -p "*_test.py"'
        )
        publish_fixtures = (
            'python3 -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"'
        )
        pack_command = (
            "pwsh ./tools/pack-release.ps1 -Version 0.0.264 "
            "-OutputDirectory artifacts/packages/ci -PackageOnly"
        )
        release_step = self._assert_blocking_run_step(
            steps,
            "Run release package fixtures",
            release_fixtures,
        )
        publish_step = self._assert_blocking_run_step(
            steps,
            "Run NuGet publish fixtures",
            publish_fixtures,
        )
        pack_step = self._assert_blocking_run_step(
            steps,
            "Pack and validate release packages",
            pack_command,
        )
        self.assertLess(job.index(release_step), job.index(pack_step))
        self.assertLess(job.index(publish_step), job.index(pack_step))
        self.assertNotIn("dotnet nuget push", job)
        self.assertNotIn("tools/publish-nuget.ps1 -PackageDirectory", job)

    def test_nightly_benchmark_job_runs_complete_project_and_retains_both_evidence_types(self):
        workflow = NIGHTLY_WORKFLOW_PATH.read_text(encoding="utf-8")
        job = self._read_workflow_job(NIGHTLY_WORKFLOW_PATH, "benchmark")
        steps = self._read_workflow_steps(job)

        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("cron: '0 3 * * *'", workflow)
        timeout_match = re.search(r"^    timeout-minutes: (\d+)$", job, re.MULTILINE)
        self.assertIsNotNone(timeout_match)
        self.assertGreaterEqual(int(timeout_match.group(1)), 10)
        self.assertLessEqual(int(timeout_match.group(1)), 45)
        self.assertIn("global-json-file: global.json", job)
        self.assertIn("dotnet restore Hexalith.Memories.slnx -p:Configuration=Release", job)
        self.assertIn("dotnet build Hexalith.Memories.slnx --configuration Release --no-restore", job)
        self.assertIn("docker info", job)
        self._assert_blocking_run_step(
            steps,
            "Run complete NDCG benchmark project",
            'bash ./tools/test.sh --filter "Category=Benchmark" --configuration Release '
            "--no-build --results-directory TestResults/benchmark",
        )
        self.assertNotIn("continue-on-error", steps["Run complete NDCG benchmark project"])
        self.assertNotIn("dotnet test", job)
        self.assertNotIn("dapr init", job)
        uploads = [steps["Upload benchmark TRX"], steps["Upload benchmark quality result"]]
        for upload in uploads:
            self.assertIn("uses: actions/upload-artifact@v7", upload)
            self.assertIn("if: always()", upload)
            self.assertIn("if-no-files-found: error", upload)
            self.assertIn("retention-days: 14", upload)
        self.assertIn(
            "TestResults/benchmark/Hexalith.Memories.Benchmarks/Hexalith.Memories.Benchmarks.trx",
            job,
        )
        self.assertIn(
            "tests/Hexalith.Memories.Benchmarks/bin/Release/net10.0/benchmark-results.json",
            job,
        )

    def test_contributor_docs_publish_coverage_package_and_benchmark_contracts(self):
        configuration = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
        formatted_threshold = f'{configuration["minimumLineCoveragePercent"]:.1f}%'
        test_readme = TEST_README_PATH.read_text(encoding="utf-8")
        contributing = CONTRIBUTING_PATH.read_text(encoding="utf-8")
        test_readme_coverage = self._read_atx_section(test_readme, "Coverage")
        contributing_tests = self._read_atx_section(contributing, "Tests")

        self.assertIn(
            'bash ./tools/test.sh --filter "Category!=Integration" --configuration Release '
            "--no-build --coverage --results-directory TestResults/test-unit-contract",
            test_readme,
        )
        self.assertIn(
            "python3 tools/validate-coverage.py --results-directory "
            "TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json",
            test_readme,
        )
        self.assertIn(formatted_threshold, test_readme_coverage)
        self.assertIn("Category=Benchmark", test_readme)
        self.assertIn("all 17 tests", test_readme)
        self.assertIn("80%", test_readme)
        self.assertIn("requires Docker", test_readme)
        self.assertIn("TestResults/benchmark", test_readme)
        self.assertIn("benchmark-results.json", test_readme)
        self.assertIn(
            "epic-26-benchmark-remediation-evidence-2026-07-16.md",
            test_readme,
        )
        self.assertNotIn("known-red at 6/8", test_readme)
        self.assertNotIn("rather than a dedicated root CI workflow", test_readme)

        for project in (
            "Contracts.Tests",
            "Server.Tests",
            "Cli.Tests",
            "Mcp.Tests",
            "EventStore.Tests",
            "Web.Tests",
        ):
            self.assertIn(project, contributing)
        self.assertIn(formatted_threshold, contributing_tests)
        self.assertIn("all 17 tests", contributing)
        self.assertIn("test-unit-contract-results", contributing)
        self.assertIn("nightly-benchmark-trx", contributing)
        self.assertIn("nightly-benchmark-quality-result", contributing)

    @staticmethod
    def _read_workflow_job(path: Path, job_name: str) -> str:
        source = path.read_text(encoding="utf-8")
        match = re.search(
            rf"(?ms)^  {re.escape(job_name)}:\n(?P<body>.*?)(?=^  [A-Za-z0-9_-]+:\n|\Z)",
            source,
        )
        if match is None:
            raise AssertionError(f"workflow job {job_name!r} was not found in {path}")
        return f"  {job_name}:\n{match.group('body')}"

    @staticmethod
    def _read_atx_section(source: str, heading: str) -> str:
        headings = list(re.finditer(
            rf"(?m)^(?P<marks>#{{1,6}}) {re.escape(heading)}[ \t]*$",
            source,
        ))
        if len(headings) != 1:
            raise AssertionError(f"expected exactly one ATX section named {heading!r}")

        match = headings[0]
        level = len(match.group("marks"))
        remainder = source[match.end():]
        boundary = re.search(rf"(?m)^#{{1,{level}}} ", remainder)
        return remainder if boundary is None else remainder[:boundary.start()]

    @staticmethod
    def _read_workflow_steps(job: str) -> dict[str, str]:
        matches = list(re.finditer(r"(?m)^      - name: (?P<name>[^\n]+)\n", job))
        steps: dict[str, str] = {}
        for index, match in enumerate(matches):
            end = matches[index + 1].start() if index + 1 < len(matches) else len(job)
            steps[match.group("name").strip()] = job[match.start():end]
        return steps

    def _assert_blocking_run_step(
        self,
        steps: dict[str, str],
        name: str,
        command: str,
    ) -> str:
        self.assertIn(name, steps)
        step = steps[name]
        self.assertIn(f"run: {command}", step)
        self.assertNotRegex(step, r"(?m)^        continue-on-error:")
        self.assertNotRegex(step, r"(?m)^        if:")
        return step


if __name__ == "__main__":
    unittest.main()
