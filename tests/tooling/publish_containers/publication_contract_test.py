"""Offline qualification of the skopeo adapter against unchanged approved Builds APIs.

Registry transport, SDK builds and process health are controlled fixture boundaries;
these tests grant no live registry or workload qualification credit.
"""

from contextlib import contextmanager
from copy import deepcopy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[3]


def load(path, name):
    specification = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


CONTRACT = load(ROOT / "tools/container-publication-contract.py", "five_image_contract")
HEALTH = load(ROOT / "tools/verify-container-health.py", "five_image_health")
ARCHIVE = load(ROOT / "tools/archive-release-evidence.py", "five_image_archive")
SOURCE_SHA = "a" * 40
SET_SHA = "b" * 64
VERSION = "1.2.3"
ENVIRONMENT = {
    "GITHUB_REPOSITORY": "Hexalith/Hexalith.Memories", "GITHUB_SHA": SOURCE_SHA,
    "GITHUB_WORKFLOW_SHA": SOURCE_SHA, "GITHUB_REF": "refs/heads/main",
    "GITHUB_WORKFLOW_REF": "Hexalith/Hexalith.Memories/.github/workflows/release.yml@refs/heads/main",
    "GITHUB_RUN_ID": "304", "GITHUB_RUN_ATTEMPT": "1", "GITHUB_RUN_NUMBER": "30",
    "GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_ACTOR": "fixture", "GITHUB_TRIGGERING_ACTOR": "fixture",
    "HEXALITH_ZOT_USERNAME": "fixture-user", "HEXALITH_ZOT_API_KEY": "fixture-secret", "GITHUB_TOKEN": "fixture-github-token",
}


def bytes_json(document):
    return json.dumps(document, sort_keys=True, separators=(",", ":")).encode()


def descriptor(data, media):
    return {"mediaType": media, "digest": "sha256:" + hashlib.sha256(data).hexdigest(), "size": len(data)}


def tar_bytes(path, files):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tarfile.open(path, "w:gz") as archive:
        for name, data in files.items():
            header = tarfile.TarInfo(name)
            header.size = len(data)
            archive.addfile(header, io.BytesIO(data))


@contextmanager
def working_directory(path):
    previous = Path.cwd()
    os.chdir(path)
    try:
        yield
    finally:
        os.chdir(previous)


class PublicationContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.shared_temp = tempfile.TemporaryDirectory()
        cls.shared = Path(cls.shared_temp.name) / "builds/Github/publish-containers"
        cls.shared.mkdir(parents=True)
        for name in CONTRACT.SHARED_FILES:
            data = subprocess.run(["git", "-C", str(ROOT / "references/Hexalith.Builds"), "show", f"{CONTRACT.APPROVED_BUILDS_SHA}:Github/publish-containers/{name}"], check=True, capture_output=True).stdout
            (cls.shared / name).write_bytes(data)
        provenance = cls.shared.parents[1] / "Props/ContainerProvenance.targets"
        provenance.parent.mkdir()
        provenance.write_bytes(subprocess.run(["git", "-C", str(ROOT / "references/Hexalith.Builds"), "show", f"{CONTRACT.APPROVED_BUILDS_SHA}:Props/ContainerProvenance.targets"], check=True, capture_output=True).stdout)
        cls.validator = load(cls.shared / "oci_registry_validator.py", "approved_adapter_oci")
        previous = sys.modules.get("oci_registry_validator")
        sys.modules["oci_registry_validator"] = cls.validator
        try:
            cls.preflight = load(cls.shared / "publication_preflight.py", "approved_adapter_preflight")
        finally:
            if previous is None:
                sys.modules.pop("oci_registry_validator", None)
            else:
                sys.modules["oci_registry_validator"] = previous

    @classmethod
    def tearDownClass(cls):
        cls.shared_temp.cleanup()

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.shared = self.root / ".hexalith/builds-execution/Github/publish-containers"
        shutil.copytree(type(self).shared, self.shared)
        self.output = self.root / "artifacts/containers/release"
        self.evidence = self.root / "artifacts/release/container-evidence"
        self.manifest = self.root / "tools/release-containers.json"
        self.manifest.parent.mkdir()
        shutil.copyfile(ROOT / "tools/render-production-deployment.ps1", self.manifest.parent / "render-production-deployment.ps1")
        fake_bin = self.root / "bin"
        fake_bin.mkdir()
        kubectl = fake_bin / "kubectl"
        kubectl.write_text("#!/bin/sh\ncat <<'YAML'\napiVersion: v1\nkind: List\nitems:\n" + "".join(f"  - image: registry.hexalith.com/{image['repository']}:0.0.0\n" for image in CONTRACT.CANONICAL_IMAGES[:4]) + "YAML\n")
        kubectl.chmod(0o755)
        self.fake_path = str(fake_bin) + os.pathsep + os.environ["PATH"]
        shutil.copyfile(ROOT / "tools/release-containers.json", self.manifest)
        shutil.copyfile(ROOT / "tools/release-packages.json", self.manifest.with_name("release-packages.json"))
        for image in CONTRACT.CANONICAL_IMAGES:
            path = self.root / image["project"]
            path.parent.mkdir(parents=True)
            path.write_text(f"<Project><PropertyGroup><EnableContainer>true</EnableContainer><ContainerRepository>{image['repository']}</ContainerRepository></PropertyGroup></Project>")
        self.output.mkdir(parents=True)
        self.env = mock.patch.dict(os.environ, dict(ENVIRONMENT, PATH=self.fake_path))
        self.env.start()
        self.addCleanup(self.env.stop)
        with mock.patch.object(CONTRACT, "run", side_effect=lambda args, **_: SOURCE_SHA if "rev-parse" in args else ""):
            current = CONTRACT.current_identity(self.root, self.manifest, VERSION, "registry.test", {})
        self.identity = CONTRACT.freeze_identity(self.evidence / "identity.json", current, create=True)
        CONTRACT.write_json(self.evidence / "authorization.json", {"result": "pass", "mode": "preemptive-basic", "pushProven": False, "cleanup": "pass", "registry": "registry.test", "repositories": [image["repository"] for image in CONTRACT.CANONICAL_IMAGES]})
        self.commands = []
        self.graphs = {}
        self.remote_graphs = {}
        self.status = 404
        self.fail_copy = None
        self.fail_health = None

    def fixture_archives(self, image, legacy, oci, labels_override=None):
        labels = CONTRACT.labels_for(self.validator, self.identity) if labels_override is None else labels_override
        files = {"oci-layout": b'{"imageLayoutVersion":"1.0.0"}'}
        children = []
        amd64_config = None
        for platform in CONTRACT.PLATFORMS:
            config = bytes_json({"architecture": platform.split("/")[1], "os": "linux", "config": {"Labels": labels}})
            if platform == "linux/amd64":
                amd64_config = config
            config_descriptor = descriptor(config, self.validator.OCI_CONFIG_MEDIA_TYPE)
            manifest = bytes_json({"schemaVersion": 2, "mediaType": self.validator.OCI_MANIFEST_MEDIA_TYPE, "config": config_descriptor, "layers": []})
            child = descriptor(manifest, self.validator.OCI_MANIFEST_MEDIA_TYPE)
            child["platform"] = {"os": "linux", "architecture": platform.split("/")[1]}
            children.append(child)
            files["blobs/sha256/" + child["digest"].split(":")[1]] = manifest
            files["blobs/sha256/" + config_descriptor["digest"].split(":")[1]] = config
        files["index.json"] = bytes_json({"schemaVersion": 2, "mediaType": self.validator.OCI_INDEX_MEDIA_TYPE, "manifests": children})
        tar_bytes(oci, files)
        tar_bytes(legacy, {"manifest.json": bytes_json([{"Config": "config.json", "RepoTags": [f"registry.test/{image['repository']}:{VERSION}"], "Layers": []}]), "config.json": amd64_config})
        self.graphs[image["name"]] = CONTRACT.archive_graph(oci, self.validator, CONTRACT.labels_for(self.validator, self.identity))

    def fake_run(self, arguments, log=None):
        arguments = [str(value) for value in arguments]
        self.commands.append(arguments)
        if log:
            Path(log).parent.mkdir(parents=True, exist_ok=True)
            Path(log).write_text("controlled fixture boundary\n")
        if arguments[0] == "dotnet":
            image = next(image for image in CONTRACT.CANONICAL_IMAGES if image["project"] == arguments[2])
            if "-p:ContainerImageFormat=OCI" in arguments:
                self.fixture_archives(image, self.output / (image["name"] + ".tar.gz"), self.output / (image["name"] + ".oci.tar.gz"))
            else:
                (self.output / (image["name"] + ".tar.gz")).write_bytes(b"pending SDK fixture")
        elif arguments[1].endswith("publication_preflight.py"):
            # Execute the unchanged approved preflight, controlling only its external reads.
            with mock.patch.object(sys, "argv", arguments[1:]), mock.patch.object(self.preflight, "prove_current_green_source", return_value={"live_sha": SOURCE_SHA, "ci_run": {"head_sha": SOURCE_SHA, "id": 1}}), mock.patch.object(self.preflight, "read_external_version_observations", return_value=[{"kind": "fixture", "identity": "offline", "versions": []}]), mock.patch.object(self.preflight, "destination_probe", return_value=lambda *_: self.status):
                parsed = self.preflight._parse_arguments()
                self.preflight._validate_publication(parsed)
        elif arguments[0] == "skopeo":
            name = next(image["name"] for image in CONTRACT.CANONICAL_IMAGES if arguments[-1] == f"docker://registry.test/{image['repository']}:{VERSION}")
            self.assertTrue(arguments[-2].startswith("oci:"))
            layout = Path(arguments[-2].removeprefix("oci:"))
            self.assertEqual(("index.json", "oci-layout"), tuple(sorted(path.name for path in layout.iterdir() if path.is_file())))
            self.assertTrue((layout / "blobs/sha256").is_dir())
            if name == self.fail_copy:
                raise ValueError("controlled fixture authorization denial")
            self.remote_graphs[arguments[-1].removeprefix("docker://")] = self.graphs[name]
            auth = Path(arguments[arguments.index("--authfile") + 1])
            self.assertEqual(0o600, auth.stat().st_mode & 0o777)
        elif arguments[1].endswith("verify-container-health.py"):
            name = arguments[arguments.index("--name") + 1]
            graph = self.graphs[name]
            directory = self.evidence / name
            image = next(image for image in CONTRACT.CANONICAL_IMAGES if image["name"] == name)
            for architecture in ("amd64", "arm64"):
                (directory / f"health-{architecture}.log").write_text("controlled workload fixture\n")
            (directory / "smoke-preflight.log").write_text("controlled emulator fixture\n")
            CONTRACT.write_json(directory / "health.json", {"result": "pass", "image": arguments[arguments.index("--image") + 1], "setSha256": self.identity["setSha256"], "ociValidationSha256": CONTRACT.sha256(directory / "oci-validation.json"), "indexDigest": graph["index_digest"], "environment": "Development", "endpoint": "/alive" if image["health"] == "liveness" else "/ready", "contract": image["health"], "lifecycleWritesEnabled": False, "platforms": [{"platform": child["platform"], "digest": child["digest"], "outcome": "pass", "cleanup": "pass"} for child in graph["children"]]})
            if name == self.fail_health or self.fail_health == "all":
                raise ValueError("controlled fixture health failure")
        return ""

    @contextmanager
    def execution(self):
        owner = self
        class FixtureRegistryClient:
            def __init__(self, registry, repository, username, api_key):
                owner.assertEqual((ENVIRONMENT["HEXALITH_ZOT_USERNAME"], ENVIRONMENT["HEXALITH_ZOT_API_KEY"]), (username, api_key))
                self.graph = owner.remote_graphs[f"{registry}/{repository}:{VERSION}"]

            def response(self, body):
                return {"body_bytes": body, "docker_content_digest": "sha256:" + hashlib.sha256(body).hexdigest(), "content_type": json.loads(body).get("mediaType")}

            def manifest(self, reference, _code):
                if reference in (VERSION, self.graph["index_digest"]):
                    return self.response(self.graph["index_bytes"])
                return self.response(next(child["manifest_bytes"] for child in self.graph["children"] if child["digest"] == reference))

            def blob(self, digest):
                return self.response(next(child["config_bytes"] for child in self.graph["children"] if child["config_digest"] == digest))
        with working_directory(self.root), mock.patch.object(CONTRACT, "run", side_effect=self.fake_run), mock.patch.object(CONTRACT, "load_shared", return_value=(self.preflight, self.validator, None, {})), mock.patch.object(self.preflight, "destination_probe", return_value=lambda *_: self.status), mock.patch.object(self.validator, "RegistryClient", FixtureRegistryClient):
            yield

    def prepare(self):
        with self.execution():
            CONTRACT.build_archives(self.root, self.output, self.evidence, self.shared, self.identity, self.validator)

    def publish(self):
        with self.execution():
            CONTRACT.shared_phase(self.shared, self.evidence, self.identity, "publish")
            CONTRACT.publish_archives(self.output, self.evidence, self.shared, self.identity, self.validator, "skopeo")

    def test_manifest_requires_exact_five_mappings_health_platforms_and_approved_sha(self):
        valid = CONTRACT.read_json(self.manifest)
        mutations = [lambda doc: doc["images"].pop(), lambda doc: doc["images"].append(deepcopy(doc["images"][0])), lambda doc: doc["images"][0].update(repository="remapped"), lambda doc: doc.update(buildsSha="c" * 40), lambda doc: doc["platforms"].pop(), lambda doc: doc["images"][2].update(health="liveness")]
        for mutate in mutations:
            document = deepcopy(valid)
            mutate(document)
            CONTRACT.write_json(self.manifest, document)
            with self.subTest(document=document), self.assertRaisesRegex(ValueError, "exact five"):
                CONTRACT.validate_manifest(self.root, self.manifest)

    def test_missing_added_or_remapped_project_is_refused_before_sdk_or_transport(self):
        project = self.root / CONTRACT.CANONICAL_IMAGES[0]["project"]
        project.unlink()
        with self.assertRaisesRegex(ValueError, "inventory"):
            CONTRACT.validate_manifest(self.root, self.manifest)
        self.assertEqual([], self.commands)

    def test_nested_container_project_is_refused(self):
        nested = self.root / "src/nested/container/extra.csproj"
        nested.parent.mkdir(parents=True)
        nested.write_text("<Project><PropertyGroup><EnableContainer>true</EnableContainer><ContainerRepository>extra</ContainerRepository></PropertyGroup></Project>")
        with self.assertRaisesRegex(ValueError, "inventory"):
            CONTRACT.validate_manifest(self.root, self.manifest)

    def test_modified_top_level_shared_helper_cannot_execute(self):
        sentinel = self.root / "executed"
        path = self.shared / "publication_preflight.py"
        path.write_text(f"from pathlib import Path\nPath({str(sentinel)!r}).touch()\n" + path.read_text())
        def git(arguments, **kwargs):
            if "rev-parse" in arguments:
                return SimpleNamespace(stdout=CONTRACT.APPROVED_BUILDS_SHA)
            relative = arguments[-1].split(":", 1)[1]
            return SimpleNamespace(stdout=(type(self).shared.parents[1] / relative).read_bytes())
        with mock.patch.object(CONTRACT.subprocess, "run", side_effect=git), mock.patch.object(CONTRACT, "load_local") as loader, self.assertRaisesRegex(ValueError, "file changed"):
            CONTRACT.load_shared(self.shared)
        self.assertFalse(sentinel.exists())
        loader.assert_not_called()

    def test_modified_root_build_inputs_and_untracked_source_refuse_before_freeze(self):
        (self.root / ".gitignore").write_text("Ignored.cs\n")
        for filename in ("Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config"):
            (self.root / filename).write_text("baseline")
        for arguments in (("init", "--initial-branch=main"), ("add", "tools", "src", ".gitignore", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config"), ("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-m", "chore: create source proof fixture")):
            subprocess.run(["git", "-C", str(self.root), *arguments], check=True, capture_output=True)
        source = subprocess.run(["git", "-C", str(self.root), "rev-parse", "HEAD"], check=True, capture_output=True, text=True).stdout.strip()
        for filename in ("Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", "src/Hexalith.Memories.Server/Injected.cs", "src/Hexalith.Memories.Server/Ignored.cs"):
            path = self.root / filename
            original = path.read_bytes() if path.exists() else None
            path.write_text("changed input")
            with self.subTest(filename=filename), mock.patch.dict(os.environ, {"GITHUB_SHA": source, "GITHUB_WORKFLOW_SHA": source}), self.assertRaises(ValueError):
                CONTRACT.current_identity(self.root, self.manifest, VERSION, "registry.test", {})
            if original is None:
                path.unlink()
            else:
                path.write_bytes(original)

    def test_approved_target_evaluates_canonical_provenance_labels_for_all_projects(self):
        self.prepare()
        for image in CONTRACT.CANONICAL_IMAGES:
            call = next(call for call in self.commands if call[0] == "dotnet" and call[2] == image["project"])
            properties = [value for value in call if value.startswith("-p:")]
            target = type(self).shared.parents[1] / "Props/ContainerProvenance.targets"
            properties = [value for value in properties if not value.startswith("-p:MemoriesApprovedProvenanceTargets=")]
            command = ["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", "-target:RebindContainerProvenanceLabels", "-getItem:ContainerLabel", *properties, f"-p:MemoriesApprovedProvenanceTargets={target}"]
            result = subprocess.run(command, check=True, capture_output=True, text=True)
            labels = {item["Identity"]: item["Value"] for item in json.loads(result.stdout)["Items"]["ContainerLabel"]}
            for key, value in CONTRACT.labels_for(self.validator, self.identity).items():
                self.assertEqual(value, labels[key], (image["name"], key))

    def test_workflow_run_source_and_semver_identity_fail_closed(self):
        for key, value in (("GITHUB_WORKFLOW_REF", "other"), ("GITHUB_SHA", "c" * 40), ("GITHUB_WORKFLOW_SHA", "c" * 40), ("GITHUB_RUN_ID", ""), ("GITHUB_RUN_ATTEMPT", "0"), ("GITHUB_EVENT_NAME", "push")):
            with self.subTest(key=key), mock.patch.dict(os.environ, {key: value}), mock.patch.object(CONTRACT, "run", return_value=SOURCE_SHA), self.assertRaises(ValueError):
                CONTRACT.current_identity(self.root, self.manifest, VERSION, "registry.test", {})
        for version in ("01.2.3", "1.2.3+build", "1.2.3-01"):
            with self.subTest(version=version), self.assertRaises(ValueError):
                CONTRACT.current_identity(self.root, self.manifest, version, "registry.test", {})

    def test_frozen_identity_rejects_mapping_version_run_and_digest_changes(self):
        current = {key: value for key, value in self.identity.items() if key not in ("created", "setSha256")}
        self.assertEqual(self.identity, CONTRACT.freeze_identity(self.evidence / "identity.json", current))
        for key, value in (("version", "1.2.4"), ("sourceSha", "c" * 40), ("registry", "other.test"), ("run", {}), ("images", [])):
            with self.subTest(key=key), self.assertRaisesRegex(ValueError, "identity changed"):
                CONTRACT.freeze_identity(self.evidence / "identity.json", dict(current, **{key: value}))

    def test_full_adapter_preserves_five_stable_archives_and_all_child_digests(self):
        self.prepare()
        self.publish()
        phases = [command[command.index("--phase") + 1] for command in self.commands if "--phase" in command]
        self.assertEqual(["verify", "publish", "container"], phases)
        copies = [command for command in self.commands if command[0] == "skopeo"]
        self.assertEqual(5, len(copies))
        for command in copies:
            self.assertEqual(["copy", "--all", "--preserve-digests"], command[1:4])
            self.assertTrue(command[-2].startswith("oci:"))
        self.assertEqual(10, len([command for command in self.commands if command[0] == "dotnet"]))
        receipt = CONTRACT.read_json(self.evidence / "archive-compatibility.json")
        self.assertEqual([image["name"] + ".tar.gz" for image in CONTRACT.CANONICAL_IMAGES], [record["filename"] for record in receipt["archives"]])
        self.assertTrue(all(record["sourceSha"] == SOURCE_SHA for record in receipt["archives"]))
        self.assertEqual("succeeded", CONTRACT.read_json(self.output / "publish-summary.json")["status"])

    def test_occupied_and_ambiguous_destinations_refuse_before_builds_or_copy(self):
        for status in (200, 401, 503):
            self.status = status
            self.commands.clear()
            with self.subTest(status=status), self.execution(), self.assertRaises(Exception):
                CONTRACT.build_archives(self.root, self.output, self.evidence, self.shared, self.identity, self.validator)
            self.assertFalse(any(command[0] in ("dotnet", "skopeo") for command in self.commands))

    def test_archive_drift_or_missing_member_blocks_before_container_phase_and_writes(self):
        self.prepare()
        (self.output / "mcp.oci.tar.gz").write_bytes(b"changed")
        before = len(self.commands)
        with self.execution(), self.assertRaisesRegex(ValueError, "changed"):
            CONTRACT.publish_archives(self.output, self.evidence, self.shared, self.identity, self.validator, "skopeo")
        self.assertEqual(before, len(self.commands))

    def test_archive_materialization_rejects_paths_and_links_outside_layout(self):
        for name, link in (("../escape", False), ("blobs/sha256/link", True)):
            with self.subTest(name=name):
                archive_path = self.root / "unsafe.tar.gz"
                with tarfile.open(archive_path, "w:gz") as archive:
                    header = tarfile.TarInfo(name)
                    if link:
                        header.type = tarfile.SYMTYPE
                        header.linkname = "../../escape"
                        archive.addfile(header)
                    else:
                        header.size = 1
                        archive.addfile(header, io.BytesIO(b"x"))
                with self.assertRaisesRegex(ValueError, "unsafe member"):
                    CONTRACT.materialize_oci_archive(archive_path, self.root / "unpacked")
                shutil.rmtree(self.root / "unpacked")

    def test_transport_denial_and_health_failure_retain_aggregate_partial_outcomes(self):
        self.prepare()
        self.fail_copy = "mcp"
        self.fail_health = "access-telemetry-clock"
        with self.assertRaisesRegex(ValueError, "Jérôme Piquot"):
            self.publish()
        summary = CONTRACT.read_json(self.output / "publish-summary.json")
        self.assertEqual("partial-publish", summary["status"])
        self.assertEqual(2, len(summary["failed"]))
        self.assertEqual(5, len(summary["images"]))

    def test_all_successful_copies_remain_pushed_when_every_health_check_fails(self):
        self.prepare()
        self.fail_health = "all"
        with self.assertRaises(ValueError):
            self.publish()
        summary = CONTRACT.read_json(self.output / "publish-summary.json")
        self.assertEqual("partial-publish", summary["status"])
        self.assertEqual(5, len(summary["pushed"]))
        self.assertEqual(5, len(summary["failed"]))
        self.assertTrue(all(item["transported"] and item["status"] == "failed" for item in summary["images"]))

    def test_shared_phase_collision_cannot_replay_publication(self):
        self.prepare()
        with self.execution():
            CONTRACT.shared_phase(self.shared, self.evidence, self.identity, "publish")
            with self.assertRaisesRegex(Exception, "already exists"):
                CONTRACT.shared_phase(self.shared, self.evidence, self.identity, "publish")

    def test_missing_platform_and_provenance_are_rejected_by_approved_validator(self):
        self.prepare()
        image = CONTRACT.CANONICAL_IMAGES[0]
        path = self.output / "server.oci.tar.gz"
        with tarfile.open(path) as archive:
            files = {member.name: archive.extractfile(member).read() for member in archive if member.isfile()}
        index = json.loads(files["index.json"])
        index["manifests"].pop()
        files["index.json"] = bytes_json(index)
        tar_bytes(path, files)
        with self.assertRaisesRegex(Exception, "exactly|platform|two"):
            CONTRACT.archive_graph(path, self.validator, CONTRACT.labels_for(self.validator, self.identity))

    def test_missing_source_provenance_is_rejected_by_approved_validator(self):
        image = CONTRACT.CANONICAL_IMAGES[0]
        labels = CONTRACT.labels_for(self.validator, self.identity)
        labels.pop("org.opencontainers.image.revision")
        with self.assertRaisesRegex(Exception, "provenance|label"):
            self.fixture_archives(image, self.output / "server.tar.gz", self.output / "server.oci.tar.gz", labels)

    def test_destination_that_becomes_occupied_after_set_preflight_is_never_copied(self):
        self.prepare()
        original = self.fake_run
        def after_first_copy(arguments, log=None):
            result = original(arguments, log)
            if str(arguments[0]) == "skopeo":
                self.status = 200
            return result
        with self.execution(), mock.patch.object(CONTRACT, "run", side_effect=after_first_copy), self.assertRaises(ValueError):
            CONTRACT.shared_phase(self.shared, self.evidence, self.identity, "publish")
            CONTRACT.publish_archives(self.output, self.evidence, self.shared, self.identity, self.validator, "skopeo")
        self.assertEqual(1, len([command for command in self.commands if command[0] == "skopeo"]))
        self.assertEqual("partial-publish", CONTRACT.read_json(self.output / "publish-summary.json")["status"])

    def evidence_packet(self):
        self.prepare()
        self.publish()
        CONTRACT.write_json(self.evidence / "authorization.json", {"result": "pass", "mode": "preemptive-basic", "pushProven": False, "cleanup": "pass", "registry": "registry.test", "repositories": [image["repository"] for image in CONTRACT.CANONICAL_IMAGES]})
        packages = {"schemaVersion": 1, "status": "succeeded", "version": VERSION, "source": "https://api.nuget.org/v3/index.json", "pushed": sorted(f"{package['id']}.{VERSION}.nupkg" for package in CONTRACT.read_json(self.manifest.with_name("release-packages.json"))["packages"]), "failed": [], "notAttempted": []}
        package_path = self.root / "artifacts/packages/release/publish-summary.json"
        packages["packageDirectory"] = package_path.parent.relative_to(self.root).as_posix()
        CONTRACT.write_json(package_path, packages)
        container = CONTRACT.read_json(self.output / "publish-summary.json")
        aggregate = {"schemaVersion": 1, "artifactKind": "release-artifacts", "status": "succeeded", "version": VERSION, "sourceSha": SOURCE_SHA, "setSha256": self.identity["setSha256"], "families": [], "pushed": [], "failed": [], "notAttempted": []}
        aggregate["source"] = "NuGet and registry.test"
        aggregate["packageDirectory"] = "artifacts/release"
        for name, summary, path in (("NuGet packages", packages, package_path), ("container images", container, self.output / "publish-summary.json")):
            aggregate["families"].append({"name": name, "status": "succeeded", "exitCode": 0, "summaryPath": path.relative_to(self.root).as_posix(), "summary": summary})
            aggregate["pushed"] += [f"{name}: {item}" for item in summary["pushed"]]
        CONTRACT.write_json(self.root / "artifacts/release/publish-summary.json", aggregate)
        with working_directory(self.root):
            ARCHIVE.render_deployment(self.identity, self.output / "production-deployment.yaml")
        deployment = self.root / "artifacts/deployment/hexalith-memories-production.yaml"
        deployment.parent.mkdir(parents=True)
        shutil.copyfile(self.output / "production-deployment.yaml", deployment)

    @contextmanager
    def archive_context(self):
        original = ARCHIVE.contract.run
        def controlled(args, **kwargs):
            if str(args[0]) == "git":
                return SOURCE_SHA if "rev-parse" in args else ""
            return original(args, **kwargs)
        with working_directory(self.root), mock.patch.object(ARCHIVE.contract, "load_shared", return_value=(self.preflight, self.validator, None, {})), mock.patch.object(ARCHIVE.contract, "run", side_effect=controlled):
            yield

    def test_successful_evidence_asset_hashes_every_identity_auth_oci_and_health_record(self):
        self.evidence_packet()
        output = self.root / "evidence.tar.gz"
        with self.archive_context():
            manifest = ARCHIVE.archive(self.evidence, output, self.shared)
        self.assertEqual("pass", manifest["result"])
        with tarfile.open(output) as archive:
            for entry in manifest["files"]:
                self.assertEqual(entry["sha256"], hashlib.sha256(archive.extractfile(entry["path"]).read()).hexdigest())
        self.assertGreater(len(manifest["files"]), 100)

    def test_missing_health_auth_or_platform_evidence_rejects_success_asset(self):
        self.evidence_packet()
        for relative in ("authorization.json", "mcp/health.json", "server/child-linux-arm64.manifest.raw"):
            path = self.evidence / relative
            original = path.read_bytes()
            path.unlink()
            with self.subTest(path=relative), self.archive_context(), self.assertRaisesRegex(ValueError, "incomplete"):
                ARCHIVE.verify(self.evidence, self.shared)
            path.write_bytes(original)

    def test_health_digest_cleanup_or_auth_push_claim_cannot_be_substituted(self):
        self.evidence_packet()
        path = self.evidence / "mcp/health.json"
        original = CONTRACT.read_json(path)
        for mutation in (lambda health: health["platforms"][0].update(digest="sha256:" + "f" * 64), lambda health: health["platforms"][1].update(cleanup="failure"), lambda health: health.update(ociValidationSha256="f" * 64)):
            health = deepcopy(original)
            mutation(health)
            CONTRACT.write_json(path, health)
            with self.archive_context(), self.assertRaisesRegex(ValueError, "health"):
                ARCHIVE.verify(self.evidence, self.shared)
        CONTRACT.write_json(path, original)
        auth = CONTRACT.read_json(self.evidence / "authorization.json")
        auth["pushProven"] = True
        CONTRACT.write_json(self.evidence / "authorization.json", auth)
        with self.archive_context(), self.assertRaisesRegex(ValueError, "authorization"):
            ARCHIVE.verify(self.evidence, self.shared)

    def test_empty_wrong_set_and_contradictory_summaries_are_refused(self):
        self.evidence_packet()
        cases = (
            (self.output / "build-summary.json", lambda summary: summary.update(images=[])),
            (self.output / "publish-summary.json", lambda summary: summary.update(sourceSha="f" * 40)),
            (self.output / "publish-summary.json", lambda summary: summary.update(setSha256="f" * 64)),
            (self.output / "publish-summary.json", lambda summary: summary.update(failed=[{"package": "failed"}])),
            (self.output / "publish-summary.json", lambda summary: summary["images"][0].update(transported=False)),
            (self.root / "artifacts/packages/release/publish-summary.json", lambda summary: summary["pushed"].pop()),
            (self.root / "artifacts/release/publish-summary.json", lambda summary: summary.update(families=[])),
            (self.root / "artifacts/release/publish-summary.json", lambda summary: summary.update(setSha256="f" * 64)),
            (self.root / "artifacts/release/publish-summary.json", lambda summary: summary["families"][0].update(exitCode=42)),
            (self.root / "artifacts/release/publish-summary.json", lambda summary: summary["families"][1]["summary"].update(pushed=[])),
            (self.root / "artifacts/release/publish-summary.json", lambda summary: summary.update(pushed=[])),
        )
        for path, mutate in cases:
            original = path.read_bytes()
            summary = CONTRACT.read_json(path)
            mutate(summary)
            CONTRACT.write_json(path, summary)
            with self.subTest(path=path, mutation=summary), self.archive_context(), self.assertRaises(ValueError):
                ARCHIVE.verify(self.evidence, self.shared)
            path.write_bytes(original)

    def test_destination_receipts_and_local_raw_graphs_cannot_be_substituted(self):
        self.evidence_packet()
        path = self.evidence / "mcp/destination-before-copy.json"
        original = path.read_bytes()
        for change in ({"result": "fail"}, {"version": "9.9.9"}, {"container_repository": "registry.test/other"}):
            CONTRACT.write_json(path, dict(json.loads(original), **change))
            with self.subTest(change=change), self.archive_context(), self.assertRaisesRegex(ValueError, "Destination"):
                ARCHIVE.verify(self.evidence, self.shared)
        path.write_bytes(original)
        for filename in ("index.raw", "child-linux-amd64.manifest.raw", "child-linux-arm64.config.raw"):
            path = self.evidence / "mcp/local" / filename
            original = path.read_bytes()
            path.write_bytes(b"corrupt local raw evidence")
            with self.subTest(raw=filename), self.archive_context(), self.assertRaisesRegex(ValueError, "OCI"):
                ARCHIVE.verify(self.evidence, self.shared)
            path.write_bytes(original)

    def test_deployment_requires_exact_caller_rendering_and_no_hosted_domain_service(self):
        self.evidence_packet()
        deployment = self.root / "artifacts/deployment/hexalith-memories-production.yaml"
        original = deployment.read_bytes()
        for replacement in (b"arbitrary deployment bytes", original.replace(b"memories-mcp:1.2.3", b"memories-domain-service:1.2.3"), original + b"---\nkind: Deployment\nmetadata:\n  name: memories-domain-service\n"):
            deployment.write_bytes(replacement)
            (self.output / "production-deployment.yaml").write_bytes(replacement)
            with self.subTest(replacement=replacement), self.archive_context(), self.assertRaisesRegex(ValueError, "deployment"):
                ARCHIVE.verify(self.evidence, self.shared)
        self.assertNotIn(b"memories-domain-service", original)

    def test_custom_directories_never_consume_stale_default_summaries(self):
        self.evidence_packet()
        containers, packages, summaries = (self.root / name for name in ("custom-containers", "custom-packages", "custom-summaries"))
        shutil.copytree(self.output, containers)
        shutil.copytree(self.root / "artifacts/packages/release", packages)
        package_summary = CONTRACT.read_json(packages / "publish-summary.json")
        package_summary["packageDirectory"] = str(packages)
        CONTRACT.write_json(packages / "publish-summary.json", package_summary)
        summaries.mkdir()
        aggregate = CONTRACT.read_json(self.root / "artifacts/release/publish-summary.json")
        for family, directory in zip(aggregate["families"], (packages, containers)):
            family["summaryPath"] = str(directory / "publish-summary.json")
            family["summary"] = CONTRACT.read_json(directory / "publish-summary.json")
        aggregate["packageDirectory"] = str(summaries)
        CONTRACT.write_json(summaries / "publish-summary.json", aggregate)
        CONTRACT.write_json(self.output / "publish-summary.json", {"status": "stale-default"})
        CONTRACT.write_json(self.root / "artifacts/release/publish-summary.json", {"status": "stale-default"})
        directories = dict(container_directory=containers, package_directory=packages, summary_directory=summaries)
        with self.archive_context():
            result = ARCHIVE.archive(self.evidence, summaries / "evidence.tar.gz", self.shared, **directories)
        self.assertEqual("pass", result["result"])
        (summaries / "publish-summary.json").unlink()
        with self.archive_context(), self.assertRaisesRegex(ValueError, "incomplete"):
            ARCHIVE.verify(self.evidence, self.shared, **directories)

    def test_malformed_identity_bytes_are_retained_in_diagnostic_only_archive(self):
        identity_path = self.evidence / "identity.json"
        for raw in (b"{broken JSON", b"[]"):
            identity_path.write_bytes(raw)
            packet = self.root / "diagnostics.tar.gz"
            with working_directory(self.root):
                manifest = ARCHIVE.archive(self.evidence, packet, self.shared, diagnostics=True)
            self.assertEqual("diagnostic-only", manifest["result"])
            self.assertFalse(manifest["identityReadable"])
            self.assertIsNone(manifest["sourceSha"])
            with tarfile.open(packet) as archive:
                self.assertEqual(raw, archive.extractfile(identity_path.relative_to(self.root).as_posix()).read())

    def test_diagnostics_archive_remains_explicitly_non_success_with_missing_evidence(self):
        with working_directory(self.root):
            manifest = ARCHIVE.archive(self.evidence, self.root / "diagnostics.tar.gz", self.shared, diagnostics=True)
        self.assertEqual("diagnostic-only", manifest["result"])

    def test_diagnostic_archive_rejects_symbolic_link_without_retaining_private_target(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "private-fixture.txt"
            target.write_bytes(b"PRIVATE_FIXTURE_MUST_NOT_BE_RETAINED")
            link = self.evidence / "unsafe-link.log"
            link.symlink_to(target)
            packet = self.root / "diagnostics.tar.gz"
            for dangling in (False, True):
                if dangling:
                    target.unlink()
                with self.subTest(dangling=dangling), working_directory(self.root), self.assertRaisesRegex(ValueError, "symbolic link"):
                    ARCHIVE.archive(self.evidence, packet, self.shared, diagnostics=True)
                self.assertFalse(packet.exists(), "Refusal must precede packet creation and cannot retain the link target.")
                self.assertFalse(packet.with_suffix(packet.suffix + ".sha256.json").exists())


class WorkloadHealthTests(unittest.TestCase):
    def test_server_caller_starts_both_keyed_dependencies_and_records_their_cleanup(self):
        for cleanup_failure in (False, True):
            calls = []
            def command(arguments):
                calls.append(arguments)
                if arguments[:2] == ["docker", "exec"]:
                    return SimpleNamespace(returncode=0, stdout="PONG", stderr="")
                if arguments[:3] == ["docker", "rm", "--force"] and arguments[-1].endswith("-redis") and cleanup_failure:
                    return SimpleNamespace(returncode=1, stdout="", stderr="cleanup refused")
                if arguments[:2] == ["docker", "run"] and arguments[-1].startswith("registry.test/memories@"):
                    environment = [arguments[index + 1] for index, value in enumerate(arguments[:-1]) if value == "--env"]
                    self.assertTrue(any(value.startswith("ConnectionStrings__redis=memories-publication-server-") for value in environment))
                    self.assertTrue(any(value.startswith("ConnectionStrings__falkordb=memories-publication-server-") for value in environment))
                    self.assertIn("AccessTelemetryLifecycle__Enabled=false", environment)
                return SimpleNamespace(returncode=0, stdout="", stderr="")
            smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _poll_liveness=lambda *_: ("pass", 1), _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "pass", SMOKE_JWT_ISSUER="issuer", SMOKE_JWT_AUDIENCE="audience", SMOKE_JWT_SIGNING_KEY="development-key")
            with self.subTest(cleanup_failure=cleanup_failure), tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command):
                result = HEALTH.run_platform("server", "registry.test/memories", "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
                self.assertEqual("cleanup-failure" if cleanup_failure else "pass", result["outcome"])
                log = (Path(temporary) / "health-amd64.log").read_text()
                self.assertIn("dependency=redis;image=", log)
                self.assertIn("dependency=falkordb;image=", log)
                self.assertEqual(2, len([call for call in calls if call[:3] == ["docker", "rm", "--force"]]))

    def test_run_platform_lifecycle_and_clock_require_ready_even_when_alive_would_pass(self):
        @contextmanager
        def authority(_network):
            yield {"Clock__AllowDevelopmentSources": "true"}
        for name in ("access-telemetry", "access-telemetry-clock"):
            for valid in (False, True):
                calls = []
                entry = "access-telemetry-lifecycle" if name == "access-telemetry" else "clock-attestation"
                body = {"status": "Healthy", "entries": {entry: {"status": "Healthy", "description": "Access telemetry lifecycle writes are disabled."}} if valid else {}}
                def command(arguments):
                    calls.append(arguments)
                    return SimpleNamespace(returncode=0, stdout=json.dumps(body) if arguments[0] == "curl" and arguments[-1].endswith("/ready") else "alive passes", stderr="")
                smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _poll_liveness=mock.Mock(return_value=("pass", 1)), _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "pass", _run_or_none=lambda *_: SimpleNamespace(returncode=0, stdout="127.0.0.1:12345"), _container_state=lambda *_: "running|0", SMOKE_JWT_ISSUER="issuer", SMOKE_JWT_AUDIENCE="audience", SMOKE_JWT_SIGNING_KEY="development-key")
                with self.subTest(name=name, valid=valid), tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command), mock.patch.object(HEALTH, "clock_authority", authority), mock.patch.object(HEALTH.time, "monotonic", side_effect=[0, 2]):
                    result = HEALTH.run_platform(name, "registry.test/" + name, "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
                self.assertEqual("pass" if valid else "readiness-timeout", result["outcome"])
                self.assertTrue(any(call[0] == "curl" and call[-1].endswith("/ready") for call in calls))
                smoke._poll_liveness.assert_not_called()

    def test_clock_authority_is_ephemeral_grpc_and_cleanup_failure_is_terminal(self):
        original = HEALTH.command
        calls = []
        secret_paths = []
        def controlled(arguments):
            calls.append(arguments)
            if arguments[0] != "docker":
                return original(arguments)
            return SimpleNamespace(returncode=1 if arguments[:3] == ["docker", "rm", "--force"] else 0, stdout="", stderr="")
        with mock.patch.object(HEALTH, "command", side_effect=controlled), self.assertRaises(HEALTH.CleanupFailure):
            with HEALTH.clock_authority("fixture-isolated-network") as settings:
                self.assertIn(":50001", settings["DAPR_GRPC_ENDPOINT"])
                self.assertLessEqual(len(settings["DAPR_GRPC_ENDPOINT"].split("://")[1].split(":")[0]), 63)
                self.assertEqual("access-telemetry-secrets", settings["Clock__SecretStoreName"])
                self.assertEqual("true", settings["Clock__AllowDevelopmentSources"])
                start = next(call for call in calls if call[:2] == ["docker", "run"])
                mount = start[start.index("--mount") + 1]
                source = Path(mount.split("source=", 1)[1].split(",", 1)[0])
                secret_paths.append(source)
                self.assertTrue((source / "secrets.json").exists())
                self.assertEqual(0o600, (source / "secrets.json").stat().st_mode & 0o777)
                self.assertEqual("readonly", mount.split(",")[-1])
                self.assertEqual("fixture-isolated-network", start[start.index("--network") + 1])
        self.assertTrue(all(not path.exists() for path in secret_paths))

    def test_disabled_readiness_requires_explicit_disabled_description(self):
        body = {"status": "Healthy", "entries": {"access-telemetry-lifecycle": {"status": "Healthy", "description": "Access telemetry lifecycle writes are disabled."}}}
        self.assertTrue(HEALTH.health_response_valid("access-telemetry", json.dumps(body)))
        body["entries"]["access-telemetry-lifecycle"]["description"] = "Enabled and healthy"
        self.assertFalse(HEALTH.health_response_valid("access-telemetry", json.dumps(body)))

    def test_clock_requires_signed_readiness_entry_and_healthy_status(self):
        for body, expected in (({"status": "Healthy", "entries": {}}, False), ({"status": "Healthy", "entries": {"clock-attestation": {"status": "Healthy"}}}, True), ({"status": "Unhealthy", "entries": {"clock-attestation": {"status": "Unhealthy"}}}, False)):
            with self.subTest(body=body):
                self.assertEqual(expected, HEALTH.health_response_valid("access-telemetry-clock", json.dumps(body)))

    def test_cleanup_failure_fails_workload_and_keeps_digest_diagnostics(self):
        smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _poll_liveness=lambda *_: ("pass", 1), _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "failure", SMOKE_JWT_ISSUER="issuer", SMOKE_JWT_AUDIENCE="audience", SMOKE_JWT_SIGNING_KEY="development-key")
        def command(arguments):
            return SimpleNamespace(returncode=0, stdout="172.20.0.1" if "inspect" in arguments else "", stderr="")
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command):
            result = HEALTH.run_platform("mcp", "registry.test/memories-mcp", "linux/arm64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
            self.assertEqual("cleanup-failure", result["outcome"])
            self.assertEqual("failure", result["cleanup"])
            self.assertIn("sha256:" + "c" * 64, (Path(temporary) / "health-arm64.log").read_text())


if __name__ == "__main__":
    unittest.main()
