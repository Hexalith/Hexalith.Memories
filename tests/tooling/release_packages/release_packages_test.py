import json
import shutil
import subprocess
import tempfile
import unittest
import zipfile
from contextlib import contextmanager
from pathlib import Path
from typing import Iterator, Optional


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "validate-release-packages.ps1"
SCHEMA = REPO_ROOT / "tools" / "release-packages.schema.json"
CANONICAL_INVENTORY = REPO_ROOT / "tools" / "release-packages.json"

# The validator computes $repoRoot from its own location ($PSScriptRoot/..), so when the
# inventory under test is written to a temp file, the SchemaPath / InventoryPath must be
# absolute paths and the script execution must use cwd=REPO_ROOT for the src/ scan.


def has_pwsh() -> bool:
    return shutil.which("pwsh") is not None


def load_canonical_inventory() -> dict:
    return json.loads(CANONICAL_INVENTORY.read_text(encoding="utf-8"))


def write_inventory(directory: Path, inventory: dict) -> Path:
    path = directory / "release-packages.json"
    path.write_text(json.dumps(inventory, indent=2), encoding="utf-8")
    return path


@contextmanager
def temporary_src_project(project_name: str, content: str) -> Iterator[None]:
    sentinel_dir = REPO_ROOT / "src" / project_name
    sentinel_csproj = sentinel_dir / f"{project_name}.csproj"
    if sentinel_dir.exists():
        raise AssertionError(f"refusing to reuse existing src sentinel directory: {sentinel_dir}")

    sentinel_dir.mkdir()
    try:
        sentinel_csproj.write_text(content, encoding="utf-8")
        yield
    finally:
        if sentinel_csproj.exists():
            sentinel_csproj.unlink()
        if sentinel_dir.exists():
            sentinel_dir.rmdir()


def run_validator(
    *,
    inventory_path: Optional[Path] = None,
    package_directory: Optional[Path] = None,
    version: Optional[str] = None,
) -> "subprocess.CompletedProcess[str]":
    args: list[str] = [
        "pwsh",
        "-NoLogo",
        "-NoProfile",
        "-File",
        str(SCRIPT),
    ]
    if inventory_path is not None:
        args += ["-InventoryPath", str(inventory_path)]
        # Schema lives next to the canonical inventory; pin it explicitly so the override
        # inventory in a temp directory still resolves the schema.
        args += ["-SchemaPath", str(SCHEMA)]
    if package_directory is not None:
        args += ["-PackageDirectory", str(package_directory)]
    if version is not None:
        args += ["-Version", version]

    return subprocess.run(
        args,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )


def create_package_set(
    directory: Path,
    *,
    version: str = "1.2.3",
    mcp_dependency_id: Optional[str] = "Hexalith.Memories.ServiceDefaults",
    mcp_dependency_version: Optional[str] = None,
    mcp_dependency_range: Optional[str] = None,
    redis_dependency_id: Optional[str] = None,
    service_defaults_prerelease_dependency_id: Optional[str] = None,
) -> None:
    inventory = load_canonical_inventory()
    package_ids = [item["packageId"] for item in inventory["packages"]]

    for package_id in package_ids:
        dependency_rows: list[str] = []
        if package_id == "Hexalith.Memories.Mcp" and mcp_dependency_id is not None:
            dependency_version = mcp_dependency_version or version
            dependency_range = mcp_dependency_range or f"[{dependency_version}, )"
            dependency_rows.append(
                f'        <dependency id="{mcp_dependency_id}" version="{dependency_range}" />'
            )
        if package_id == "Hexalith.Memories.Redis" and redis_dependency_id is not None:
            dependency_rows.append(
                f'        <dependency id="{redis_dependency_id}" version="[{version}, )" />'
            )
        if (
            package_id == "Hexalith.Memories.ServiceDefaults"
            and service_defaults_prerelease_dependency_id is not None
        ):
            dependency_rows.append(
                f'        <dependency id="{service_defaults_prerelease_dependency_id}" version="[1.0.0-preview.1, )" />'
            )

        dependencies = ""
        if dependency_rows:
            dependencies = f"""
    <dependencies>
      <group targetFramework="net10.0">
{chr(10).join(dependency_rows)}
      </group>
    </dependencies>"""

        nuspec = f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>{package_id}</id>
    <version>{version}</version>
    <authors>ITANEO</authors>
    <license type="expression">MIT</license>
    <projectUrl>https://github.com/Hexalith/Hexalith.Memories</projectUrl>
    <description>Test package</description>
    <tags>hexalith memories test</tags>
    <repository type="git" url="https://github.com/Hexalith/Hexalith.Memories" />
    <readme>README.md</readme>{dependencies}
  </metadata>
</package>
"""
        package_path = directory / f"{package_id}.{version}.nupkg"
        with zipfile.ZipFile(package_path, "w") as archive:
            archive.writestr(f"{package_id}.nuspec", nuspec)
            archive.writestr("README.md", "# Test package\n")


@unittest.skipUnless(has_pwsh(), "pwsh (PowerShell 7+) is required to exercise validate-release-packages.ps1")
class ValidateReleasePackagesTests(unittest.TestCase):
    def assertValidatorFailsWith(
        self,
        result: "subprocess.CompletedProcess[str]",
        *expected_substrings: str,
    ) -> None:
        if result.returncode == 0:
            self.fail(
                f"validator unexpectedly succeeded.\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}"
            )
        combined = (result.stdout or "") + "\n" + (result.stderr or "")
        for needle in expected_substrings:
            self.assertIn(
                needle,
                combined,
                f"expected diagnostic to mention '{needle}' but it did not.\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}",
            )

    def test_canonical_inventory_validates_without_overrides(self) -> None:
        result = run_validator()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("Release package validation passed.", result.stdout)

    def test_mcp_service_defaults_dependency_at_release_version_passes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(package_directory)

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertEqual(0, result.returncode, result.stderr)

    def test_mcp_service_defaults_dependency_must_match_release_version(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(package_directory, mcp_dependency_version="1.2.2")

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.Mcp",
                "Hexalith.Memories.ServiceDefaults",
                "expected",
                "exact version '1.2.3'",
                "release range '[1.2.3, )'",
            )

    def test_internal_dependency_range_must_not_have_an_upper_bound(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(
                package_directory,
                mcp_dependency_range="[1.2.3, 9.0.0)",
            )

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.Mcp",
                "Hexalith.Memories.ServiceDefaults",
                "expected",
                "exact version '1.2.3'",
                "release range '[1.2.3, )'",
            )

    def test_mcp_package_must_reference_service_defaults(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(package_directory, mcp_dependency_id=None)

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.Mcp",
                "must reference",
                "Hexalith.Memories.ServiceDefaults",
            )

    def test_unapproved_internal_dependency_fails_loudly(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(
                package_directory,
                mcp_dependency_id="Hexalith.Memories.Unpublished",
            )

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.Mcp",
                "unexpected internal",
                "dependency",
                "Hexalith.Memories.Unpublished",
            )

    def test_generated_package_directory_rejects_duplicate_package_ids(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(package_directory)
            source = package_directory / "Hexalith.Memories.Contracts.1.2.3.nupkg"
            shutil.copyfile(source, package_directory / "duplicate-contracts.1.2.3.nupkg")

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "duplicate package IDs",
                "Hexalith.Memories.Contracts",
            )

    def test_redis_compatibility_package_rejects_internal_dependencies(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(
                package_directory,
                redis_dependency_id="Hexalith.Memories.Contracts",
            )

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.Redis",
                "must not reference",
                "internal Hexalith.Memories packages",
                "Hexalith.Memories.Contracts",
            )

    def test_service_defaults_rejects_unexpected_prerelease_dependencies(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(
                package_directory,
                service_defaults_prerelease_dependency_id="Example.Unexpected.Prerelease",
            )

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.ServiceDefaults",
                "contains unexpected",
                "prerelease dependencies",
                "Example.Unexpected.Prerelease",
            )

    def test_service_defaults_allows_whitelisted_prerelease_dependency(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp)
            create_package_set(
                package_directory,
                service_defaults_prerelease_dependency_id="OpenTelemetry.Instrumentation.StackExchangeRedis",
            )

            result = run_validator(package_directory=package_directory, version="1.2.3")

            self.assertEqual(0, result.returncode, result.stderr)

    @unittest.skipUnless(shutil.which("dotnet"), "dotnet is required to compile the Redis compatibility consumer")
    def test_redis_compatibility_surface_compiles_with_warnings_as_errors(self) -> None:
        redis_project = REPO_ROOT / "src" / "Hexalith.Memories.Redis" / "Hexalith.Memories.Redis.csproj"
        with tempfile.TemporaryDirectory() as temp:
            consumer_dir = Path(temp)
            (consumer_dir / "CompatibilityConsumer.csproj").write_text(
                f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="{redis_project.as_posix()}" />
  </ItemGroup>
</Project>
""",
                encoding="utf-8",
            )
            (consumer_dir / "CompatibilityConsumer.cs").write_text(
                """using System.Threading.Tasks;

using Hexalith.Memories.Redis;
using NFalkorDB;

public static class CompatibilityConsumer
{
    public static Task<ResultSet> QueryAsync(FalkorDB client)
        => client.QueryAsync("consumer-graph", "RETURN 1");

    public static string Ports
        => RedisPlaceholder.DefaultRedisPort + ":" + RedisPlaceholder.DefaultFalkorDbPort;
}
""",
                encoding="utf-8",
            )

            result = subprocess.run(
                [
                    "dotnet",
                    "build",
                    str(consumer_dir / "CompatibilityConsumer.csproj"),
                    "--configuration",
                    "Release",
                    "-m:1",
                    "/nr:false",
                    "-p:NuGetAudit=false",
                ],
                cwd=REPO_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, result.returncode, result.stdout + "\n" + result.stderr)

    def test_misspelled_top_level_field_is_rejected_by_schema(self) -> None:
        canonical = load_canonical_inventory()
        canonical["nonPackableProject"] = canonical.pop("nonPackableProjects")
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "release-packages.json",
                "schema",
            )

    def test_misspelled_package_field_is_rejected_by_schema(self) -> None:
        canonical = load_canonical_inventory()
        first = canonical["packages"][0]
        # Replace the canonical key with the common misspelling 'packageID'.
        first["packageID"] = first.pop("packageId")
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "release-packages.json",
                "schema",
            )

    def test_misspelled_project_field_is_rejected_by_schema(self) -> None:
        canonical = load_canonical_inventory()
        first = canonical["packages"][0]
        first["projectPath"] = first.pop("project")
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "release-packages.json",
                "schema",
            )

    def test_duplicate_package_id_with_different_paths_fails_loudly(self) -> None:
        canonical = load_canonical_inventory()
        duplicate = dict(canonical["packages"][0])
        # Different project path so the schema's uniqueItems (object-equality) does not catch
        # this; the explicit duplicate-ID guard inside the validator must.
        duplicate["project"] = "src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj"
        canonical["packages"].append(duplicate)
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "duplicate packageId",
                duplicate["packageId"],
            )

    def test_duplicate_project_path_with_different_ids_fails_loudly(self) -> None:
        canonical = load_canonical_inventory()
        duplicate = dict(canonical["packages"][0])
        duplicate["packageId"] = "Hexalith.Memories.DuplicatePathSentinel"
        canonical["packages"].append(duplicate)
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "duplicate project paths",
            )

    def test_path_in_both_buckets_fails_loudly(self) -> None:
        canonical = load_canonical_inventory()
        clashing = canonical["packages"][0]["project"]
        canonical["nonPackableProjects"].append(clashing)
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "declared in both packages and nonPackableProjects",
            )

    def test_missing_non_packable_project_fails_loudly(self) -> None:
        canonical = load_canonical_inventory()
        # Drop one of the canonical non-packable projects so the validator has to detect
        # an unlisted real src/**/*.csproj — this is the W2 silent-bypass closure under test.
        dropped = canonical["nonPackableProjects"].pop()
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "Non-packable project inventory mismatch",
                dropped,
            )

    def test_extra_non_packable_project_pointing_to_nonexistent_csproj_fails(self) -> None:
        canonical = load_canonical_inventory()
        canonical["nonPackableProjects"].append("src/Hexalith.Memories.Phantom/Hexalith.Memories.Phantom.csproj")
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "Non-packable project inventory mismatch",
            )

    def test_extra_packable_project_pointing_to_nonexistent_csproj_fails(self) -> None:
        canonical = load_canonical_inventory()
        canonical["packages"].append(
            {
                "packageId": "Hexalith.Memories.Phantom",
                "project": "src/Hexalith.Memories.Phantom/Hexalith.Memories.Phantom.csproj",
            }
        )
        with tempfile.TemporaryDirectory() as temp:
            inventory_path = write_inventory(Path(temp), canonical)
            result = run_validator(inventory_path=inventory_path)
            self.assertValidatorFailsWith(
                result,
                "Packable",
                "inventory mismatch",
            )

    def test_version_with_build_metadata_normalizes_with_clear_message(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            package_directory = Path(temp) / "no-packages"
            package_directory.mkdir()
            # Provide a directory so the validator reaches the version branch even though
            # the directory is empty (it will fail on missing nupkg). The test asserts the
            # normalization note appears even on the failure path so operators see both the
            # original and normalized form regardless of which downstream check fails.
            result = run_validator(
                package_directory=package_directory,
                version="1.2.3+local",
            )
            self.assertNotEqual(0, result.returncode)
            combined = (result.stdout or "") + "\n" + (result.stderr or "")
            self.assertIn("1.2.3+local", combined)
            self.assertIn("1.2.3", combined)
            self.assertIn("NuGet pack strips '+...'", combined)

    def test_version_with_empty_build_metadata_fails_loudly(self) -> None:
        result = run_validator(version="1.2.3+")
        self.assertValidatorFailsWith(
            result,
            "Version '1.2.3+' is not a valid semantic version.",
        )

    def test_version_without_base_version_fails_loudly(self) -> None:
        result = run_validator(version="+local")
        self.assertValidatorFailsWith(
            result,
            "Version '+local' is not a valid semantic version.",
        )

    def test_invalid_isPackable_value_fails_loudly(self) -> None:
        with temporary_src_project(
            "Hexalith.Memories.IsPackableSentinel",
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <IsPackable>maybe</IsPackable>
  </PropertyGroup>
</Project>
""",
        ):
            result = run_validator()
            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.IsPackableSentinel",
                "unsupported",
                "<IsPackable> value",
            )

    def test_blank_isPackable_value_fails_loudly(self) -> None:
        with temporary_src_project(
            "Hexalith.Memories.BlankPackableSentinel",
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <IsPackable>   </IsPackable>
  </PropertyGroup>
</Project>
""",
        ):
            result = run_validator()
            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.BlankPackableSentinel",
                "blank <IsPackable>",
            )

    def test_missing_isPackable_value_fails_loudly(self) -> None:
        with temporary_src_project(
            "Hexalith.Memories.NoIsPackableSentinel",
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
""",
        ):
            result = run_validator()
            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.NoIsPackableSentinel",
                "missing <IsPackable>",
            )

    def test_multiple_isPackable_values_fail_loudly(self) -> None:
        with temporary_src_project(
            "Hexalith.Memories.MultipleIsPackableSentinel",
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
""",
        ):
            result = run_validator()
            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.MultipleIsPackableSentinel",
                "declares <IsPackable> multiple times",
            )

    def test_conditional_isPackable_value_fails_loudly(self) -> None:
        with temporary_src_project(
            "Hexalith.Memories.ConditionalIsPackableSentinel",
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup Condition=\"'$(Configuration)' == 'Release'\">
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
""",
        ):
            result = run_validator()
            self.assertValidatorFailsWith(
                result,
                "Hexalith.Memories.ConditionalIsPackableSentinel",
                "declares conditional <IsPackable>",
            )


if __name__ == "__main__":
    unittest.main()
