import json
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CONFIG_PATH = REPO_ROOT / "tests" / "tooling" / "coverage_gate" / "line-coverage-gate.json"
RUNSETTINGS_PATH = REPO_ROOT / "tests" / "tests.runsettings"
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
                "Hexalith.Memories.Aspire",
                "Hexalith.Memories.Cli",
                "Hexalith.Memories.Client.Rest",
                "Hexalith.Memories.Contracts",
                "Hexalith.Memories.EventStore",
                "Hexalith.Memories.EventStoreExtensions",
                "Hexalith.Memories.Mcp",
                "Hexalith.Memories.Server",
                "Hexalith.Memories.ServiceDefaults",
                "Hexalith.Memories.Telemetry",
                "Hexalith.Memories.Web",
                "Hexalith.Memories.DomainService",
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
                "Hexalith.Memories.DomainService.Tests",
            ],
            configuration["requiredReportProjects"],
        )

    def test_required_reports_match_the_executable_unit_contract_inventory(self):
        projects = [
            Path(line.strip())
            for line in (REPO_ROOT / "tools" / "test-projects.unit-contract.txt")
            .read_text(encoding="utf-8")
            .splitlines()
            if line.strip() and not line.lstrip().startswith("#")
        ]
        self.assertTrue(projects)
        self.assertEqual(len(projects), len(set(projects)))
        for project in projects:
            with self.subTest(project=project):
                self.assertEqual(Path("tests") / project.stem / f"{project.stem}.csproj", project)
                self.assertTrue((REPO_ROOT / project).is_file())
        configuration = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
        self.assertEqual([project.stem for project in projects], configuration["requiredReportProjects"])

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
                if project_name == "Hexalith.Memories.DomainService.Tests":
                    # This MTP project inherits the repository coverage extension; the
                    # original seven projects retain their direct collector contract below.
                    self._assert_inherited_mtp_reference(root)
                    continue
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

    def _assert_inherited_mtp_reference(self, project: ET.Element) -> None:
        for reference in project.iter("PackageReference"):
            self.assertFalse(
                any(reference.get(operation) == "coverlet.MTP" for operation in ("Include", "Update", "Remove")),
                "DomainService must inherit the central MTP reference without a local override",
            )
        shared = ET.parse(REPO_ROOT / "tests" / "Directory.Build.props").getroot()
        parents = {child: parent for parent in shared.iter() for child in parent}
        references = [
            element
            for element in shared.iter("PackageReference")
            if any(element.get(operation) == "coverlet.MTP" for operation in ("Include", "Update", "Remove"))
        ]
        self.assertEqual(1, len(references), "Expected one inherited MTP reference without mutators")
        reference = references[0]
        self.assertEqual({"Include": "coverlet.MTP"}, reference.attrib)
        self.assertEqual("all", reference.findtext("PrivateAssets"))
        self.assertEqual(
            "compile; runtime; build; native; contentfiles; analyzers; buildtransitive",
            reference.findtext("IncludeAssets"),
        )
        self.assertEqual({"PrivateAssets", "IncludeAssets"}, {child.tag for child in reference})
        current = reference
        while current is not None:
            self.assertFalse(current.get("Condition"), "The inherited MTP reference must be unconditional")
            self.assertNotIn(current.tag, {"Choose", "When", "Otherwise"})
            current = parents.get(current)
        self._assert_unconditional_mtp_pin(
            ET.parse(REPO_ROOT / "tests" / "Directory.Packages.props").getroot()
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

        directory_build_props = (REPO_ROOT / "tests" / "Directory.Build.props").read_text(
            encoding="utf-8"
        )
        self.assertIn("coverlet.MTP", directory_build_props)
        self.assertNotIn("Microsoft.Testing.Extensions.CodeCoverage", directory_build_props)
        test_packages = ET.parse(REPO_ROOT / "tests" / "Directory.Packages.props").getroot()
        self._assert_unconditional_mtp_pin(test_packages)

    def test_central_mtp_pin_rejects_conditional_xml(self):
        pin = '<PackageVersion Include="coverlet.MTP" Version="10.1.0" />'
        fixtures = {
            "conditioned item group": f'<Project><ItemGroup Condition="false">{pin}</ItemGroup></Project>',
            "when branch": f'<Project><Choose><When Condition="false"><ItemGroup>{pin}</ItemGroup></When></Choose></Project>',
            "inactive otherwise": (
                '<Project><Choose><When Condition="true"><PropertyGroup><Selected>true</Selected></PropertyGroup></When>'
                f'<Otherwise><ItemGroup>{pin}</ItemGroup></Otherwise></Choose></Project>'
            ),
        }
        for name, xml in fixtures.items():
            with self.subTest(name=name):
                with self.assertRaisesRegex(AssertionError, "unconditional"):
                    self._assert_unconditional_mtp_pin(ET.fromstring(xml))

    def _assert_unconditional_mtp_pin(self, test_packages: ET.Element) -> None:
        parents = {child: parent for parent in test_packages.iter() for child in parent}
        collector_pins = [
            element
            for element in test_packages.iter("PackageVersion")
            if any(element.get(operation) == "coverlet.MTP" for operation in ("Include", "Update", "Remove"))
        ]
        self.assertEqual(1, len(collector_pins), "Expected exactly one central MTP collector pin, without mutators")
        collector_pin = collector_pins[0]
        self.assertEqual("coverlet.MTP", collector_pin.get("Include"))
        version = collector_pin.get("Version", "")
        self.assertRegex(version, r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
        self.assertGreaterEqual(tuple(int(part) for part in version.split(".")), (10, 0, 1))
        current = collector_pin
        while current is not None:
            self.assertFalse(current.get("Condition"), "The central MTP collector pin must be unconditional")
            self.assertNotIn(
                current.tag.rsplit("}", 1)[-1],
                {"Choose", "When", "Otherwise"},
                "The central MTP collector pin must be unconditional, outside branch-control elements",
            )
            current = parents.get(current)










if __name__ == "__main__":
    unittest.main()
