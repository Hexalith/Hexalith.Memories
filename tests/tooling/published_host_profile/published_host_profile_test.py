"""Evaluate the real native host edges for the explicit published qualification profile."""
import json
import os
import subprocess
import unittest
from pathlib import Path

ROOT = Path(os.environ.get("MEMORIES_HOST_PROFILE_ROOT", Path(__file__).resolve().parents[3]))
PROJECTS = {
    "Server": {"Hexalith.Memories.Contracts", "Hexalith.Memories.EventStore", "Hexalith.Memories.ServiceDefaults", "Hexalith.Memories.Telemetry"},
    "DomainService": {"Hexalith.Memories.EventStore"},
}


class PublishedHostProfileTests(unittest.TestCase):
    def evaluate(self, name, profile=None, publishing=False, source=False, version="3.0.13", validate=False):
        project = ROOT / "src" / ("Hexalith.Memories." + name) / ("Hexalith.Memories." + name + ".csproj")
        args = ["dotnet", "msbuild", str(project), "-nologo", "-p:HexalithEventStoreVersion=3.129.0",
                "-p:HexalithMemoriesVersion=" + version, "-p:IsPublishing=" + str(publishing).lower(),
                "-p:HexalithEventStoreFromSource=" + str(source).lower(), "-p:HexalithEventStoreRoot=/uninitialized-sdk-owner",
                "-getProperty:MemoriesPublishedHostedTestProfile", "-getItem:ProjectReference,PackageReference,PackageVersion"]
        if profile is not None: args.append("-p:HexalithUsePublishedSdkForHostedTests=" + str(profile).lower())
        if validate: args.append("-target:ValidatePublishedHostedTestProfile")
        result = subprocess.run(args, cwd=ROOT, capture_output=True, text=True, timeout=30)
        return result

    def test_actual_source_package_and_publish_edges_are_explicit(self):
        for name, own in PROJECTS.items():
            for profile, publishing, source in [(None,False,False),(False,False,False),(True,False,False),(True,True,False),(None,False,True),(True,False,True)]:
                with self.subTest(name=name, profile=profile, publishing=publishing, source=source):
                    result = self.evaluate(name, profile, publishing, source)
                    self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                    document = json.loads(result.stdout)
                    references = document["Items"]
                    projects = {Path(item["Identity"].replace("\\", "/")).stem for item in references.get("ProjectReference", [])}
                    packages = {item["Identity"] for item in references.get("PackageReference", [])}
                    selected = profile is True and not publishing
                    if selected:
                        self.assertFalse(projects & own)
                        self.assertTrue(own.issubset(packages))
                        self.assertFalse(any(item.startswith("Hexalith.EventStore.") for item in projects))
                        self.assertIn("Hexalith.EventStore.DomainService", packages)
                        versions = {item["Identity"]: item["Version"] for item in references["PackageVersion"]}
                        self.assertTrue(all(versions[package] == "3.0.13" for package in own))
                        if name == "DomainService": self.assertIn("Hexalith.Memories.EventStoreExtensions", packages)
                    else:
                        self.assertTrue(own.issubset(projects))
                        self.assertFalse(packages & own)
                        if source: self.assertIn("Hexalith.EventStore.DomainService", projects)
                    if name == "Server": self.assertIn("Hexalith.Memories.AccessTelemetry.Contracts", projects)

    def test_actual_version_guard_refuses_unavailable_owner_cohort(self):
        for name in PROJECTS:
            for version in ("3.0.12", "0.0.0", ""):
                with self.subTest(name=name, version=version):
                    result = self.evaluate(name, True, version=version, validate=True)
                    self.assertNotEqual(0, result.returncode)
            result = self.evaluate(name, True, version="3.0.13", validate=True)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            result = self.evaluate(name, True, publishing=True, version="3.0.12", validate=True)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
