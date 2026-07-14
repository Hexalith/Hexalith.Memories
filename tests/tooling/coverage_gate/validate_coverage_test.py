import importlib.util
import json
import shutil
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


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
            config = self._write_config(root, threshold=60.0)
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
            config = self._write_config(root)
            first = root / "results" / "uuid" / "coverage.cobertura.xml"
            second = root / "results" / "host" / "In" / "machine" / "coverage.cobertura.xml"
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
            config = self._write_config(root)
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
                source_root=root / "src",
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

    @staticmethod
    def _write_config(
        root: Path,
        *,
        threshold: float = 0.0,
        required_assemblies: list[str] | None = None,
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
        source_root: Path | None = None,
    ) -> None:
        coverage = ET.Element("coverage")
        sources = ET.SubElement(coverage, "sources")
        ET.SubElement(sources, "source").text = str(source_root or path.parents[2])
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
