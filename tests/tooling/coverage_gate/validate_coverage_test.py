import importlib.util
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path, PureWindowsPath


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT_PATH = REPO_ROOT / "tools" / "validate-coverage.py"
SPEC = importlib.util.spec_from_file_location("validate_coverage", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class ValidateCoverageTests(unittest.TestCase):
    def test_validator_unions_overlapping_reports_and_reports_per_assembly(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(
                root,
                threshold=60.0,
                required_report_projects=["one", "two"],
            )
            results = root / "results"
            self._write_report(
                results / "one" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(10, 0)],
                        "src/Hexalith.Memories.Server/Other.cs": [(5, 1)],
                    }
                },
            )
            self._write_report(
                results / "two" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src\\Hexalith.Memories.Server\\Program.cs": [(10, 3)],
                        "src/Hexalith.Memories.Server/Other.cs": [(6, 0)],
                    }
                },
            )

            summary = MODULE.validate_coverage(results, config, root)

            self.assertEqual(3, summary.valid_lines)
            self.assertEqual(2, summary.covered_lines)
            self.assertAlmostEqual(66.6667, summary.coverage_percent, places=3)
            self.assertEqual(3, summary.assemblies["Hexalith.Memories.Server"].valid_lines)

    def test_validator_accepts_exact_threshold_and_rejects_below_threshold(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root, threshold=78.0)
            passing = root / "passing"
            failing = root / "failing"
            passing_lines = [(number, 1 if number <= 78 else 0) for number in range(1, 101)]
            failing_lines = [(number, 1 if number <= 77 else 0) for number in range(1, 101)]
            self._write_report(
                passing / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Program.cs": passing_lines}},
            )
            self._write_report(
                failing / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Program.cs": failing_lines}},
            )

            self.assertEqual(78.0, MODULE.validate_coverage(passing, config, root).coverage_percent)
            with self.assertRaisesRegex(MODULE.CoverageValidationError, "below required 78.00%"):
                MODULE.validate_coverage(failing, config, root)

    def test_validator_rejects_missing_reports(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            results = root / "results"
            results.mkdir()

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "no Cobertura reports"):
                MODULE.validate_coverage(results, config, root)

    def test_validator_rejects_malformed_report(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            report = root / "results" / "coverage.cobertura.xml"
            report.parent.mkdir()
            report.write_text("<coverage>", encoding="utf-8")

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "malformed Cobertura report"):
                MODULE.validate_coverage(report.parent, config, root)

    def test_validator_content_deduplicates_identical_attachments(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(
                root,
                required_report_projects=["Hexalith.Memories.Server.Tests"],
            )
            first = (
                root
                / "results"
                / "Hexalith.Memories.Server.Tests"
                / "uuid"
                / "coverage.cobertura.xml"
            )
            second = (
                root
                / "results"
                / "Hexalith.Memories.Server.Tests"
                / "host"
                / "In"
                / "machine"
                / "coverage.cobertura.xml"
            )
            self._write_report(
                first,
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Program.cs": [(1, 1)]}},
            )
            second.parent.mkdir(parents=True)
            shutil.copyfile(first, second)

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.report_count)
            self.assertEqual(1, summary.duplicate_report_count)
            self.assertEqual(1, summary.valid_lines)

    def test_validator_excludes_out_of_scope_dependencies(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(1, 1)]
                    },
                    "Hexalith.FrontComposer.Shell": {
                        "references/Hexalith.FrontComposer/src/Shell.cs": [(1, 0), (2, 0)]
                    },
                },
            )

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.valid_lines)
            self.assertNotIn("Hexalith.FrontComposer.Shell", summary.assemblies)

    def test_validator_rejects_missing_required_assembly(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(
                root,
                required_assemblies=["Hexalith.Memories.Server", "Hexalith.Memories.Cli"],
            )
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Program.cs": [(1, 1)]}},
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "required assemblies have no executable lines.*Cli"):
                MODULE.validate_coverage(root / "results", config, root)

    def test_validator_unions_windows_and_posix_paths(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(
                root,
                required_report_projects=["one", "two"],
            )
            self._write_report(
                root / "results" / "one" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src\\Hexalith.Memories.Server\\Program.cs": [(1, 0)]}},
            )
            self._write_report(
                root / "results" / "two" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Program.cs": [(1, 2)]}},
            )

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.valid_lines)
            self.assertEqual(1, summary.covered_lines)

    def test_validator_resolves_filename_against_repo_relative_source_root(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"Hexalith.Memories.Server/Program.cs": [(1, 1)]}},
                source_roots=[root / "src"],
            )

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.valid_lines)
            self.assertEqual(1, summary.required_file_line_counts["src/Hexalith.Memories.Server/Program.cs"])

    def test_validator_rejects_missing_composition_root(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"src/Hexalith.Memories.Server/Other.cs": [(1, 1)]}},
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "required files have no executable lines.*Program.cs"):
                MODULE.validate_coverage(root / "results", config, root)

    def test_validator_rejects_traversal_and_unmapped_absolute_paths(self):
        for filename in (
            "../src/Hexalith.Memories.Server/Program.cs",
            "/outside/repository/src/Hexalith.Memories.Server/Program.cs",
        ):
            with self.subTest(filename=filename), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                config = self._write_config(root)
                self._write_report(
                    root / "results" / "coverage.cobertura.xml",
                    {"Hexalith.Memories.Server": {filename: [(1, 1)]}},
                )

                with self.assertRaisesRegex(MODULE.CoverageValidationError, "unsafe source path"):
                    MODULE.validate_coverage(root / "results", config, root)

    def test_validator_rejects_external_source_root_with_relative_filename(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(1, 1)]
                    }
                },
                source_roots=[Path("/outside/repository/src")],
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "outside the repository"):
                MODULE.validate_coverage(root / "results", config, root)

    def test_validator_uses_the_matching_repository_source_root(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source_file = root / "src" / "Hexalith.Memories.Server" / "Program.cs"
            source_file.parent.mkdir(parents=True)
            source_file.write_text("// fixture\n", encoding="utf-8")
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {"Hexalith.Memories.Server/Program.cs": [(1, 1)]}},
                source_roots=[root / "wrong", root / "src"],
            )

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.valid_lines)
            self.assertEqual(
                1,
                summary.required_file_line_counts["src/Hexalith.Memories.Server/Program.cs"],
            )

    def test_validator_rejects_malformed_configuration_types_and_nonfinite_thresholds(self):
        mutations = {
            "boolean threshold": ("minimumLineCoveragePercent", True),
            "NaN threshold": ("minimumLineCoveragePercent", float("nan")),
            "string collection": ("requiredAssemblies", "Hexalith.Memories.Server"),
        }
        for name, (field, value) in mutations.items():
            with self.subTest(name=name), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                config = self._write_config(root)
                raw = json.loads(config.read_text(encoding="utf-8"))
                raw[field] = value
                config.write_text(json.dumps(raw), encoding="utf-8")

                with self.assertRaisesRegex(MODULE.CoverageValidationError, "finite number|array"):
                    MODULE.load_configuration(config)

    def test_validator_rejects_assembly_source_scope_mismatch(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Cli/Program.cs": [(1, 1)]
                    }
                },
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "outside its declared scope"):
                MODULE.validate_coverage(root / "results", config, root)

    def test_validator_requires_a_nonempty_report_from_every_inventory_project(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(
                root,
                required_report_projects=["present", "missing"],
            )
            self._write_report(
                root / "results" / "present" / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(1, 1)]
                    }
                },
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "missing"):
                MODULE.validate_coverage(root / "results", config, root)

            config = self._write_config(root, required_report_projects=["empty"])
            empty_results = root / "empty-results"
            self._write_report(
                empty_results / "empty" / "coverage.cobertura.xml",
                {},
            )

            with self.assertRaisesRegex(MODULE.CoverageValidationError, "no in-scope executable lines"):
                MODULE.validate_coverage(empty_results, config, root)

    def test_validator_accepts_mapped_absolute_posix_and_windows_paths(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = self._write_config(root)
            absolute_source = root / "src" / "Hexalith.Memories.Server" / "Program.cs"
            self._write_report(
                root / "results" / "coverage.cobertura.xml",
                {"Hexalith.Memories.Server": {str(absolute_source): [(1, 1)]}},
            )

            summary = MODULE.validate_coverage(root / "results", config, root)

            self.assertEqual(1, summary.valid_lines)

        windows_path = MODULE.normalize_source_path(
            r"C:\repo\src\Hexalith.Memories.Server\Program.cs",
            (),
            PureWindowsPath(r"C:\repo"),
        )
        self.assertEqual("src/Hexalith.Memories.Server/Program.cs", windows_path)

    def test_cli_preserves_success_failure_and_failure_diagnostics(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            passing_config = self._write_config(root, threshold=0.0)
            passing = root / "passing"
            self._write_report(
                passing / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(1, 1)]
                    }
                },
            )
            passing_result = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_PATH),
                    "--results-directory",
                    str(passing),
                    "--config",
                    str(passing_config),
                    "--repo-root",
                    str(root),
                ],
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(0, passing_result.returncode, passing_result.stderr)

            failing_config = self._write_config(root, threshold=100.0)
            failing = root / "failing"
            self._write_report(
                failing / "coverage.cobertura.xml",
                {
                    "Hexalith.Memories.Server": {
                        "src/Hexalith.Memories.Server/Program.cs": [(1, 0)]
                    }
                },
            )
            summary_path = root / "github-summary.md"
            environment = os.environ.copy()
            environment["GITHUB_STEP_SUMMARY"] = str(summary_path)
            failing_result = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_PATH),
                    "--results-directory",
                    str(failing),
                    "--config",
                    str(failing_config),
                    "--repo-root",
                    str(root),
                ],
                capture_output=True,
                text=True,
                check=False,
                env=environment,
            )
            self.assertNotEqual(0, failing_result.returncode)
            self.assertIn("Hexalith.Memories.Server", failing_result.stdout)
            self.assertIn("below required", failing_result.stderr)
            self.assertIn("Hexalith.Memories.Server", summary_path.read_text(encoding="utf-8"))

    @staticmethod
    def _write_config(
        root: Path,
        *,
        threshold: float = 0.0,
        required_assemblies: list[str] | None = None,
        required_report_projects: list[str] | None = None,
    ) -> Path:
        required = required_assemblies or ["Hexalith.Memories.Server"]
        required_files = (
            {"Hexalith.Memories.Server": "src/Hexalith.Memories.Server/Program.cs"}
            if "Hexalith.Memories.Server" in required
            else {}
        )
        path = root / "coverage-thresholds.json"
        path.write_text(
            json.dumps(
                {
                    "minimumLineCoveragePercent": threshold,
                    "sourcePathPrefixes": ["src/Hexalith.Memories."],
                    "assemblyNamePrefix": "Hexalith.Memories.",
                    "excludedAssemblyPatterns": ["Hexalith.Memories.*.Tests"],
                    "requiredAssemblies": required,
                    "requiredFiles": required_files,
                    "assemblySourcePrefixes": {
                        assembly: [f"src/{assembly}"] for assembly in required
                    },
                    "requiredReportProjects": required_report_projects or ["."],
                }
            ),
            encoding="utf-8",
        )
        return path

    @staticmethod
    def _write_report(
        path: Path,
        packages: dict[str, dict[str, list[tuple[int, int]]]],
        *,
        source_roots: list[Path | str] | None = None,
    ) -> None:
        coverage = ET.Element("coverage")
        sources = ET.SubElement(coverage, "sources")
        for source_root in source_roots or []:
            ET.SubElement(sources, "source").text = str(source_root)
        package_elements = ET.SubElement(coverage, "packages")
        for assembly, files in packages.items():
            package = ET.SubElement(package_elements, "package", {"name": assembly})
            classes = ET.SubElement(package, "classes")
            for index, (filename, lines) in enumerate(files.items()):
                class_element = ET.SubElement(
                    classes,
                    "class",
                    {"name": f"Fixture{index}", "filename": filename},
                )
                line_elements = ET.SubElement(class_element, "lines")
                for number, hits in lines:
                    ET.SubElement(
                        line_elements,
                        "line",
                        {"number": str(number), "hits": str(hits)},
                    )
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.ElementTree(coverage).write(path, encoding="utf-8", xml_declaration=True)


if __name__ == "__main__":
    unittest.main()
