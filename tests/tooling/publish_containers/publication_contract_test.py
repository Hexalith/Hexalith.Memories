"""Offline qualification of the skopeo adapter against unchanged approved Builds APIs.

Registry transport, SDK builds and process health are controlled fixture boundaries;
these tests grant no live registry or workload qualification credit.
"""

from contextlib import contextmanager, redirect_stderr
from copy import deepcopy
import base64
import hashlib
from html import escape
import importlib.util
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
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
# The runtime config every fixture deployment archive declares, shaped like the Server image.
RUNTIME = {"Env": ["PATH=/app:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin", "ASPNETCORE_HTTP_PORTS=8080"],
           "Entrypoint": ["/usr/share/dotnet/dotnet", "/app/Hexalith.Memories.Server.dll"], "WorkingDir": "/app/", "User": "1654",
           "ExposedPorts": {"8080/tcp": {}}}


BUILDS = ROOT / "references/Hexalith.Builds"


def approved_builds_checkout():
    """Return the Builds checkout holding the approved commit; fetch exactly it into a shallow clone."""
    top = subprocess.run(["git", "-C", str(BUILDS), "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=False)
    if top.returncode or Path(top.stdout.strip()).resolve() != BUILDS.resolve():
        raise unittest.SkipTest(f"references/Hexalith.Builds is not its own git checkout, so approved Builds {CONTRACT.APPROVED_BUILDS_SHA} helper bytes are unavailable.")
    commit = ["git", "-C", str(BUILDS), "cat-file", "-e", CONTRACT.APPROVED_BUILDS_SHA + "^{commit}"]
    if subprocess.run(commit, capture_output=True, check=False).returncode:
        fetch = subprocess.run(["git", "-C", str(BUILDS), "fetch", "--no-tags", "--depth=1", "origin", CONTRACT.APPROVED_BUILDS_SHA], capture_output=True, text=True, check=False)
        if fetch.returncode or subprocess.run(commit, capture_output=True, check=False).returncode:
            raise RuntimeError(f"Could not fetch approved Builds commit {CONTRACT.APPROVED_BUILDS_SHA} (exit {fetch.returncode}): {fetch.stderr.strip()}")
    return BUILDS


def write_approved_helpers(shared):
    """Materialize the approved helper bytes under `<builds>/Github/publish-containers`."""
    checkout = approved_builds_checkout()
    def show(relative):
        return subprocess.run(["git", "-C", str(checkout), "show", f"{CONTRACT.APPROVED_BUILDS_SHA}:{relative}"], check=True, capture_output=True).stdout
    shared.mkdir(parents=True)
    for name in CONTRACT.SHARED_FILES:
        (shared / name).write_bytes(show(f"Github/publish-containers/{name}"))
    provenance = shared.parents[1] / "Props/ContainerProvenance.targets"
    provenance.parent.mkdir()
    provenance.write_bytes(show("Props/ContainerProvenance.targets"))


def load_with_validator(path, name, validator):
    """Load an approved helper that imports `oci_registry_validator` without leaking that module."""
    previous = sys.modules.get("oci_registry_validator")
    sys.modules["oci_registry_validator"] = validator
    try:
        return load(path, name)
    finally:
        if previous is None:
            sys.modules.pop("oci_registry_validator", None)
        else:
            sys.modules["oci_registry_validator"] = previous


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
        approved_builds_checkout()
        cls.shared_temp = tempfile.TemporaryDirectory()
        cls.shared = Path(cls.shared_temp.name) / "builds/Github/publish-containers"
        write_approved_helpers(cls.shared)
        cls.validator = load(cls.shared / "oci_registry_validator.py", "approved_adapter_oci")
        cls.preflight = load_with_validator(cls.shared / "publication_preflight.py", "approved_adapter_preflight", cls.validator)
        cls.smoke = load_with_validator(cls.shared / "smoke_container_platforms.py", "approved_adapter_smoke", cls.validator)

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
        # Image name -> runtime config its OCI children carry instead of their archive's RUNTIME.
        self.runtime_overrides = {}
        # Platform -> outcome function; when set, the real verify-container-health CLI runs.
        self.real_health = None

    def fixture_archives(self, image, legacy, oci, labels_override=None, identity=None):
        identity = self.identity if identity is None else identity
        labels = CONTRACT.labels_for(self.validator, identity) if labels_override is None else labels_override
        files = {"oci-layout": b'{"imageLayoutVersion":"1.0.0"}'}
        children = []
        amd64_config = None
        for platform in CONTRACT.PLATFORMS:
            config = bytes_json({"architecture": platform.split("/")[1], "os": "linux", "config": {"Labels": labels, **self.runtime_overrides.get(image["name"], RUNTIME)}})
            if platform == "linux/amd64":
                amd64_config = bytes_json({"architecture": "amd64", "os": "linux", "config": {"Labels": labels, **RUNTIME}})
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
        self.graphs[image["name"]] = CONTRACT.archive_graph(oci, self.validator, CONTRACT.labels_for(self.validator, identity))

    def fake_run(self, arguments, log=None):
        arguments = [str(value) for value in arguments]
        self.commands.append(arguments)
        if log:
            Path(log).parent.mkdir(parents=True, exist_ok=True)
            Path(log).write_text("controlled fixture boundary\n")
        if arguments[0] == "git":
            # main() reads the fixture source revision; the fixture tree is clean.
            return SOURCE_SHA if "rev-parse" in arguments else ""
        if arguments[0] == "dotnet":
            image = next(image for image in CONTRACT.CANONICAL_IMAGES if image["project"] == arguments[2])
            if "-p:ContainerImageFormat=OCI" in arguments:
                # Like the SDK, label the archive from the provenance properties of this command.
                properties = dict(value[3:].split("=", 1) for value in arguments if value.startswith("-p:"))
                built = {"repository": properties["ContainerProvenanceRepositoryUrl"].removeprefix("https://github.com/"),
                         "sourceSha": properties["ContainerProvenanceSourceSha"], "version": properties["ContainerProvenanceReleaseVersion"],
                         "created": properties["ContainerProvenanceCreated"]}
                self.fixture_archives(image, self.output / (image["name"] + ".tar.gz"), self.output / (image["name"] + ".oci.tar.gz"), identity=built)
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
        elif arguments[1].endswith("verify-container-health.py") and self.real_health:
            self.run_real_health(arguments)
        elif arguments[1].endswith("verify-container-health.py"):
            name = arguments[arguments.index("--name") + 1]
            graph = self.graphs[name]
            directory = Path(arguments[arguments.index("--evidence-directory") + 1])
            image = next(image for image in CONTRACT.CANONICAL_IMAGES if image["name"] == name)
            for architecture in ("amd64", "arm64"):
                (directory / f"health-{architecture}.log").write_text("controlled workload fixture\n")
            (directory / "smoke-preflight.log").write_text("controlled emulator fixture\n")
            CONTRACT.write_json(directory / "health.json", {"result": "pass", "image": arguments[arguments.index("--image") + 1], "setSha256": arguments[arguments.index("--set-sha256") + 1], "ociValidationSha256": CONTRACT.sha256(directory / "oci-validation.json"), "indexDigest": graph["index_digest"], "environment": "Development", "endpoint": "/alive" if image["health"] == "liveness" else "/ready", "contract": image["health"], "lifecycleWritesEnabled": False, "platforms": [{"platform": child["platform"], "digest": child["digest"], "outcome": "pass", "cleanup": "pass"} for child in graph["children"]]})
            if name == self.fail_health or self.fail_health == "all":
                raise ValueError("controlled fixture health failure")
        return ""

    def run_real_health(self, arguments):
        """Run the real health CLI; only the approved helpers' external process boundary is controlled."""
        def smoke_process(command, timeout=30):
            if command[:3] == ["docker", "buildx", "inspect"]:
                return SimpleNamespace(returncode=0, stdout="Platforms: linux/amd64, linux/arm64\n", stderr="")
            if "/bin/uname" in command:
                return SimpleNamespace(returncode=0, stdout="aarch64\n", stderr="")
            return SimpleNamespace(returncode=0, stdout="", stderr="")
        def platform_run(name, repository, platform, digest, directory, smoke, timeout):
            self.assertIs(type(self).smoke, smoke)
            outcome = self.real_health(platform)
            (directory / f"health-{platform.split('/')[1]}.log").write_text(f"controlled workload boundary\noutcome={outcome}\n")
            return {"platform": platform, "digest": digest, "outcome": outcome, "cleanup": "pass"}
        with mock.patch.object(sys, "argv", [str(value) for value in arguments[1:]]), mock.patch.object(HEALTH.contract, "load_shared", return_value=(self.preflight, self.validator, type(self).smoke, {})), mock.patch.object(type(self).smoke, "_run", side_effect=smoke_process), mock.patch.object(HEALTH, "command", return_value=SimpleNamespace(returncode=0, stdout="", stderr="")), mock.patch.object(HEALTH, "run_platform", side_effect=platform_run), redirect_stderr(io.StringIO()):
            code = HEALTH.main()
        if code:
            raise ValueError(f"verify-container-health.py failed ({code})")

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

    def run_main(self, phase, evidence):
        arguments = ["container-publication-contract.py", "--phase", phase, "--version", VERSION, "--registry", "registry.test",
                     "--output-directory", str(self.output), "--evidence-directory", str(evidence), "--shared-directory", str(self.shared)]
        with self.execution(), mock.patch.object(sys, "argv", arguments), redirect_stderr(io.StringIO()):
            return CONTRACT.main()

    def test_main_freezes_identity_only_in_build_and_authorize_runs_shared_publish_phase(self):
        evidence = self.root / "artifacts/release/main-evidence"
        for phase in ("authorize", "publish"):
            with self.subTest(before_build=phase):
                self.assertEqual(1, self.run_main(phase, evidence))
                self.assertFalse((evidence / "identity.json").exists())
        self.assertEqual(0, self.run_main("build", evidence))
        frozen = (evidence / "identity.json").read_bytes()
        self.assertFalse((evidence / "preflight/publication-preflight.publish.json").exists())
        CONTRACT.write_json(evidence / "authorization.json", {"result": "pass", "mode": "preemptive-basic", "pushProven": False, "cleanup": "pass", "registry": "registry.test", "repositories": [image["repository"] for image in CONTRACT.CANONICAL_IMAGES]})
        self.assertEqual(0, self.run_main("authorize", evidence))
        self.assertEqual("pass", CONTRACT.read_json(evidence / "preflight/publication-preflight.publish.json")["result"])
        self.assertEqual(0, self.run_main("publish", evidence))
        self.assertEqual(frozen, (evidence / "identity.json").read_bytes())
        self.assertEqual(["verify", "publish", "container"], [command[command.index("--phase") + 1] for command in self.commands if "--phase" in command])
        self.assertEqual("succeeded", CONTRACT.read_json(self.output / "publish-summary.json")["status"])

    def test_ambiguous_destination_through_main_records_accountable_refusal_before_writes(self):
        evidence = self.root / "artifacts/release/ambiguous-evidence"
        self.status = 503
        self.assertEqual(1, self.run_main("build", evidence))
        failure = CONTRACT.read_json(evidence / "failure-build.json")
        self.assertEqual("fail", failure["result"])
        self.assertEqual("Jérôme Piquot", failure["owner"])
        self.assertTrue(failure["consequence"])
        self.assertTrue(failure["reopenTrigger"])
        self.assertIn("destination absence could not be proved", failure["error"])
        self.assertFalse(any(command[0] in ("dotnet", "skopeo") for command in self.commands))

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

    def test_repository_swap_and_msbuild_true_project_are_refused(self):
        CONTRACT.validate_manifest(self.root, self.manifest)
        server, mcp = (self.root / image["project"] for image in CONTRACT.CANONICAL_IMAGES[:2])
        originals = server.read_text(), mcp.read_text()
        server.write_text(originals[0].replace(">memories<", ">memories-mcp<"))
        mcp.write_text(originals[1].replace(">memories-mcp<", ">memories<"))
        with self.subTest(change="swap keeps the five names"), self.assertRaisesRegex(ValueError, "inventory"):
            CONTRACT.validate_manifest(self.root, self.manifest)
        server.write_text(originals[0])
        mcp.write_text(originals[1])
        added = self.root / "src/Extra/Extra.csproj"
        added.parent.mkdir(parents=True)
        added.write_text("<Project><PropertyGroup><EnableContainer> True </EnableContainer><ContainerRepository>extra</ContainerRepository></PropertyGroup></Project>")
        with self.subTest(change="added True project"), self.assertRaisesRegex(ValueError, "inventory"):
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
        with mock.patch.dict(os.environ, {"GITHUB_SHA": source, "GITHUB_WORKFLOW_SHA": source}):
            # Positive control: the unmodified committed tree is accepted by the real git commands.
            self.assertEqual(source, CONTRACT.current_identity(self.root, self.manifest, VERSION, "registry.test", {})["sourceSha"])
        refusals = {"Directory.Build.props": r"^git failed \(1\)", "Directory.Packages.props": r"^git failed \(1\)", "global.json": r"^git failed \(1\)",
                    "NuGet.Config": r"^git failed \(1\)", "src/Hexalith.Memories.Server/Injected.cs": "Untracked release build inputs",
                    "src/Hexalith.Memories.Server/Ignored.cs": "Untracked SDK-glob compiled source"}
        for filename, refusal in refusals.items():
            path = self.root / filename
            original = path.read_bytes() if path.exists() else None
            path.write_text("changed input")
            with self.subTest(filename=filename), mock.patch.dict(os.environ, {"GITHUB_SHA": source, "GITHUB_WORKFLOW_SHA": source}), self.assertRaisesRegex(ValueError, refusal):
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

    def test_generated_mcp_description_labels_survive_the_sdk_multi_rid_parser(self):
        self.prepare()
        image = next(image for image in CONTRACT.CANONICAL_IMAGES if image["name"] == "mcp")
        call = next(call for call in self.commands if call[0] == "dotnet" and call[2] == image["project"] and "-p:ContainerImageFormat=OCI" in call)
        target = type(self).shared.parents[1] / "Props/ContainerProvenance.targets"
        properties = [value for value in call if value.startswith("-p:") and not value.startswith("-p:MemoriesApprovedProvenanceTargets=")]
        properties.append(f"-p:MemoriesApprovedProvenanceTargets={target}")
        command = ["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", *properties]
        generated = subprocess.run([*command, "-target:ComputeContainerConfig", "-getProperty:Description", "-getItem:ContainerLabel"],
                                   check=True, capture_output=True, text=True)
        document = json.loads(generated.stdout)
        description = document["Properties"]["Description"]
        labels = document["Items"]["ContainerLabel"]
        description_labels = ("org.opencontainers.artifact.description", "org.opencontainers.image.description")
        self.assertEqual(dict.fromkeys(description_labels, description), {item["Identity"]: item["Value"] for item in labels if item["Identity"] in description_labels})
        for rid in ("linux-musl-x64", "linux-musl-arm64"):
            for old_semicolon in (False, True):
                # Match _PublishMultiArchContainers' Identity:Value serialization. A temporary
                # property file preserves its literal punctuation without command-line quoting.
                carried = [(item["Identity"], item["Value"]) for item in labels]
                if old_semicolon:
                    carried = [(name, value.replace("not retired. See", "not retired; see", 1) if name in description_labels else value) for name, value in carried]
                transported = ";".join(f"{name}:{value}" for name, value in carried)
                handoff = self.root / "mcp-label-handoff.targets"
                handoff.write_text(f"<Project><PropertyGroup><_ContainerLabel>{escape(transported)}</_ContainerLabel></PropertyGroup></Project>")
                result = subprocess.run([*command, f"-p:RuntimeIdentifier={rid}", f"-p:ContainerRuntimeIdentifier={rid}", "-p:_IsMultiRIDBuild=false",
                                         "-p:_IsSingleRIDBuild=true", f"-p:AfterMicrosoftNETSdkTargets={handoff}",
                                         "-target:_ParseItemsForPublishingSingleContainer", "-getItem:ContainerLabel"], capture_output=True, text=True)
                with self.subTest(rid=rid, old_semicolon=old_semicolon):
                    if old_semicolon:
                        self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
                        self.assertIn("MSB4184", result.stdout + result.stderr)
                        self.assertIn("Microsoft.NET.Build.Containers.targets", result.stdout + result.stderr)
                        self.assertIn("Index was outside the bounds", result.stdout + result.stderr)
                    else:
                        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                        parsed = json.loads(result.stdout)["Items"]["ContainerLabel"]
                        self.assertEqual(dict.fromkeys(description_labels, description), {item["Identity"]: item["Value"] for item in parsed if item["Identity"] in description_labels})

    def test_inner_multi_rid_build_rebinds_the_full_declared_server_path(self):
        self.prepare()
        image = CONTRACT.CANONICAL_IMAGES[0]
        call = next(call for call in self.commands if call[0] == "dotnet" and call[2] == image["project"] and "-p:ContainerImageFormat=OCI" in call)
        target = type(self).shared.parents[1] / "Props/ContainerProvenance.targets"
        properties = [value for value in call if value.startswith("-p:") and not value.startswith("-p:MemoriesApprovedProvenanceTargets=")]
        properties.append(f"-p:MemoriesApprovedProvenanceTargets={target}")
        def evaluate(*arguments):
            result = subprocess.run(["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", *properties, *arguments], check=True, capture_output=True, text=True)
            return json.loads(result.stdout)["Items"]
        declared = evaluate("-getItem:ContainerEnvironmentVariable")["ContainerEnvironmentVariable"]
        declared_path = next(item["Value"] for item in declared if item["Identity"] == "PATH")
        self.assertEqual("/app:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin", declared_path)
        expected = [(item["Identity"], item["Value"]) for item in declared]
        # _PublishMultiArchContainers hands each inner RID build these exact global properties.
        transported = ";".join(f"{item['Identity']}:{item['Value']}" for item in declared)
        # A single -getProperty prints the bare value, which is the SDK's own container targets directory.
        sdk_targets = Path(subprocess.run(["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", *properties, "-getProperty:_ContainersTargetsDir"],
                                          check=True, capture_output=True, text=True).stdout.strip()) / "Microsoft.NET.Build.Containers.targets"
        self.assertTrue(sdk_targets.is_file(), sdk_targets)
        def image_boundary(inner):
            """Run the SDK's exact inner target list with _PublishSingleContainer replaced by a no-op stub.

            Like the approved Builds provenance-target test, the stub records the items reaching image
            creation, so only the BeforeTargets hook (never a named target) can rebind them.
            """
            with tempfile.TemporaryDirectory(prefix="memories-image-boundary-") as temporary:
                observed = Path(temporary) / "environment.txt"
                hook = Path(temporary) / "stub.targets"
                # Suppressing the SDK's own import lets this hook import the same file and then win the target name.
                hook.write_text(f'''<Project>
  <Import Project="{escape(str(sdk_targets), quote=True)}" />
  <Target Name="_PublishSingleContainer">
    <WriteLinesToFile File="{escape(str(observed), quote=True)}" Lines="@(ContainerEnvironmentVariables-&gt;'%(Identity)=%(Value)')" Overwrite="true" />
  </Target>
</Project>''')
                subprocess.run(["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", *properties, *inner, f"-p:_ContainersTargetsDir={temporary}/",
                                f"-p:AfterMicrosoftNETSdkTargets={hook}", "-target:_ParseItemsForPublishingSingleContainer;_ComputeContainerExecutionArgs;_PublishSingleContainer"],
                               check=True, capture_output=True, text=True)
                return [tuple(line.split("=", 1)) for line in observed.read_text().splitlines()]
        for rid in ("linux-musl-x64", "linux-musl-arm64"):
            inner = [f"-p:RuntimeIdentifier={rid}", f"-p:ContainerRuntimeIdentifier={rid}", "-p:_IsMultiRIDBuild=false", "-p:_IsSingleRIDBuild=true", f'-p:_ContainerEnvironmentVariables="{transported}"']
            for rebind in (True, False):
                targets = "_ParseItemsForPublishingSingleContainer;_ComputeContainerExecutionArgs" + (";RebindMultiRidContainerEnvironmentVariables" if rebind else "")
                items = evaluate(*inner, f"-target:{targets}", "-getItem:ContainerEnvironmentVariables", "-getItem:ContainerAppCommand")
                environment = [(item["Identity"], item["Value"]) for item in items["ContainerEnvironmentVariables"]]
                with self.subTest(rid=rid, rebind=rebind):
                    self.assertEqual(["/usr/share/dotnet/dotnet", "/app/Hexalith.Memories.Server.dll"], [item["Identity"] for item in items["ContainerAppCommand"]])
                    if rebind:
                        self.assertEqual(expected, environment)
                    else:
                        # Negative control: the SDK's Split(':')[1] reconstruction alone cuts PATH.
                        self.assertIn(("PATH", "/app"), environment)
            with self.subTest(rid=rid, boundary="_PublishSingleContainer stub"):
                self.assertEqual(expected, image_boundary(inner))
        # A single-RID publish never receives the transported list, so its items stay as declared.
        single = [value for value in next(call for call in self.commands if call[0] == "dotnet" and call[2] == image["project"] and "-p:ContainerImageFormat=Docker" in call) if value.startswith("-p:") and not value.startswith("-p:MemoriesApprovedProvenanceTargets=")]
        result = subprocess.run(["dotnet", "msbuild", str(ROOT / image["project"]), "-nologo", *single, f"-p:MemoriesApprovedProvenanceTargets={target}",
                                 "-target:ComputeContainerConfig;RebindMultiRidContainerEnvironmentVariables", "-getItem:ContainerEnvironmentVariables"], check=True, capture_output=True, text=True)
        self.assertEqual(expected, [(item["Identity"], item["Value"]) for item in json.loads(result.stdout)["Items"]["ContainerEnvironmentVariables"]])

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

    def test_child_runtime_config_mismatch_refuses_build_before_any_release_write_or_transport(self):
        # The v3.0.23-v3.0.25 Server children carried PATH=/app while their deployment archive kept the full PATH.
        truncated = dict(RUNTIME, Env=["PATH=/app", "ASPNETCORE_HTTP_PORTS=8080"])
        self.runtime_overrides = {"server": truncated}
        evidence = self.root / "artifacts/release/parity-evidence"
        self.assertEqual(1, self.run_main("build", evidence))
        parity = CONTRACT.read_json(evidence / "server/runtime-config-parity.json")
        self.assertEqual("fail", parity["result"])
        self.assertEqual([("linux/amd64", "Env", RUNTIME["Env"], truncated["Env"]), ("linux/arm64", "Env", RUNTIME["Env"], truncated["Env"])],
                         [(item["platform"], item["field"], item["deployment"], item["child"]) for item in parity["mismatches"]])
        outcomes = {item["name"]: item for item in CONTRACT.read_json(self.output / "build-summary.json")["images"]}
        self.assertEqual("build-failed", outcomes["server"]["disposition"])
        self.assertIn("linux/arm64 Env", outcomes["server"]["error"])
        self.assertIn("archive builds failed", CONTRACT.read_json(evidence / "failure-build.json")["error"])
        self.assertEqual("fail", CONTRACT.read_json(evidence / "archive-compatibility.json")["result"])
        # Matching positive control in the same build: the other children equal their archives.
        for image in CONTRACT.CANONICAL_IMAGES[1:]:
            with self.subTest(member=image["name"]):
                self.assertEqual("built", outcomes[image["name"]]["disposition"])
                self.assertEqual(("pass", []), (lambda record: (record["result"], record["mismatches"]))(CONTRACT.read_json(evidence / image["name"] / "runtime-config-parity.json")))
        CONTRACT.write_json(evidence / "authorization.json", {"result": "pass", "mode": "preemptive-basic", "pushProven": False, "cleanup": "pass", "registry": "registry.test", "repositories": [image["repository"] for image in CONTRACT.CANONICAL_IMAGES]})
        for phase in ("authorize", "publish"):
            with self.subTest(phase=phase):
                self.assertEqual(1, self.run_main(phase, evidence))
                self.assertIn("same-source", CONTRACT.read_json(evidence / f"failure-{phase}.json")["error"])
        self.assertFalse((evidence / "preflight/publication-preflight.publish.json").exists())
        self.assertFalse((evidence / "preflight/publication-preflight.container.json").exists())
        self.assertFalse(any(command[0] == "skopeo" for command in self.commands))

    def test_every_runtime_config_field_mismatch_refuses_the_member_on_both_platforms(self):
        self.assertEqual(("Env", "Entrypoint", "Cmd", "WorkingDir", "User", "ExposedPorts"), CONTRACT.RUNTIME_CONFIG_FIELDS)
        # Env drift is covered by the PATH=/app case; each remaining field drifts alone on both children.
        drift = {"Entrypoint": ["dotnet", "/app/Hexalith.Memories.Server.dll"], "Cmd": ["--urls", "http://+:8080"],
                 "WorkingDir": "/", "User": "0", "ExposedPorts": {"80/tcp": {}}}
        for field, value in drift.items():
            with self.subTest(field=field):
                shutil.rmtree(self.output)
                self.output.mkdir()
                self.runtime_overrides = {"server": dict(RUNTIME, **{field: value})}
                evidence = self.root / f"artifacts/release/{field.lower()}-evidence"
                self.assertEqual(1, self.run_main("build", evidence))
                parity = CONTRACT.read_json(evidence / "server/runtime-config-parity.json")
                self.assertEqual([("linux/amd64", field), ("linux/arm64", field)], [(item["platform"], item["field"]) for item in parity["mismatches"]])
                outcomes = {item["name"]: item for item in CONTRACT.read_json(self.output / "build-summary.json")["images"]}
                self.assertEqual("build-failed", outcomes["server"]["disposition"])
                self.assertIn(f"linux/arm64 {field}", outcomes["server"]["error"])
                self.assertEqual(["built"] * 4, [outcomes[image["name"]]["disposition"] for image in CONTRACT.CANONICAL_IMAGES[1:]])

    def test_matching_runtime_config_is_hashed_per_archive_and_required_by_the_archiver(self):
        self.evidence_packet()
        expected = hashlib.sha256(CONTRACT.canonical({field: RUNTIME.get(field) for field in CONTRACT.RUNTIME_CONFIG_FIELDS})).hexdigest()
        receipt_path = self.evidence / "archive-compatibility.json"
        receipt = CONTRACT.read_json(receipt_path)
        for record in receipt["archives"]:
            self.assertEqual({"fields": list(CONTRACT.RUNTIME_CONFIG_FIELDS), "deploymentSha256": expected, "children": dict.fromkeys(CONTRACT.PLATFORMS, expected)}, record["runtimeConfig"])
        with self.archive_context():
            ARCHIVE.verify(self.evidence, self.shared)
        original = receipt_path.read_bytes()
        for label, mutate in (("record removed", lambda document: document["archives"][0].pop("runtimeConfig")),
                              ("child hash changed", lambda document: document["archives"][1]["runtimeConfig"]["children"].update({"linux/arm64": "f" * 64}))):
            changed = deepcopy(receipt)
            mutate(changed)
            CONTRACT.write_json(receipt_path, changed)
            # The authorize and publish phases gate on the same recomputed record before any write.
            with self.subTest(receipt=label, consumer="validate_archives"), self.execution(), self.assertRaisesRegex(ValueError, "Runtime config parity record"):
                CONTRACT.validate_archives(self.output, self.evidence, self.identity, self.validator)
            with self.subTest(receipt=label, consumer="archiver"), self.archive_context(), self.assertRaisesRegex(ValueError, "Runtime config parity"):
                ARCHIVE.verify(self.evidence, self.shared)
        receipt_path.write_bytes(original)
        diagnostics_path = self.evidence / "server/runtime-config-parity.json"
        diagnostics = diagnostics_path.read_bytes()
        CONTRACT.write_json(diagnostics_path, dict(json.loads(diagnostics), result="fail"))
        with self.subTest(diagnostics="failed"), self.archive_context(), self.assertRaisesRegex(ValueError, "Runtime config parity"):
            ARCHIVE.verify(self.evidence, self.shared)
        diagnostics_path.unlink()
        with self.subTest(diagnostics="missing"), self.archive_context(), self.assertRaisesRegex(ValueError, "incomplete"):
            ARCHIVE.verify(self.evidence, self.shared)

    def test_runtime_config_diagnostics_cannot_contradict_the_accepted_hash(self):
        self.evidence_packet()
        path = self.evidence / "server/runtime-config-parity.json"
        original = CONTRACT.read_json(path)
        corruptions = (
            ("fields", lambda document: document.update(fields=["Env"])),
            ("deployment", lambda document: document["deployment"].update(Env=["PATH=/app"])),
            ("child", lambda document: document["children"]["linux/arm64"].update(User="0")),
            ("child removed", lambda document: document["children"].pop("linux/amd64")),
        )
        for label, mutate in corruptions:
            changed = deepcopy(original)
            mutate(changed)
            self.assertEqual(original["sha256"], changed["sha256"])
            CONTRACT.write_json(path, changed)
            with self.subTest(corruption=label), self.archive_context(), self.assertRaisesRegex(ValueError, "Runtime config parity"):
                ARCHIVE.verify(self.evidence, self.shared)
        CONTRACT.write_json(path, original)
        with self.archive_context():
            ARCHIVE.verify(self.evidence, self.shared)

    def test_occupied_and_ambiguous_destinations_refuse_before_builds_or_copy(self):
        for status, code, message in ((200, "version-collision", "already exists"), (401, "destination-probe-failure", "could not be proved"), (503, "destination-probe-failure", "could not be proved")):
            self.status = status
            self.commands.clear()
            with self.subTest(status=status):
                with self.execution(), self.assertRaisesRegex(self.preflight.PreflightError, message) as refusal:
                    CONTRACT.build_archives(self.root, self.output, self.evidence, self.shared, self.identity, self.validator)
                self.assertEqual(code, refusal.exception.code)
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

    def test_real_health_cli_record_satisfies_the_archiver_and_a_failed_platform_exits_nonzero(self):
        self.real_health = lambda platform: "pass"
        self.evidence_packet()
        health = CONTRACT.read_json(self.evidence / "mcp/health.json")
        # Written by the real verify(), which the fixture health stub never produces.
        self.assertEqual(("mcp", "Development only"), (health["name"], health["authorities"]))
        with self.archive_context():
            ARCHIVE.verify(self.evidence, self.shared)
        self.real_health = lambda platform: "liveness-timeout" if platform == "linux/arm64" else "pass"
        member = self.evidence / "mcp"
        arguments = [sys.executable, ROOT / "tools/verify-container-health.py", "--name", "mcp", "--image", f"registry.test/memories-mcp:{VERSION}",
                     "--shared-directory", self.shared, "--evidence-directory", member, "--set-sha256", self.identity["setSha256"]]
        with working_directory(self.root), self.assertRaisesRegex(ValueError, r"failed \(1\)"):
            self.run_real_health(arguments)
        failed = CONTRACT.read_json(member / "health.json")
        self.assertEqual("workload-health-failure", failed["result"])
        self.assertEqual(["pass", "liveness-timeout"], [item["outcome"] for item in failed["platforms"]])
        with self.archive_context(), self.assertRaisesRegex(ValueError, "health"):
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

    def test_real_powershell_aggregation_round_trip_keeps_family_summaries_verifiable(self):
        self.evidence_packet()
        package_path = self.root / "artifacts/packages/release/publish-summary.json"
        packages = CONTRACT.read_json(package_path)
        # publish-nuget.ps1 writes `.ToString("o")` timestamps, which carry seven fractional digits.
        nuget = {"schemaVersion": 1, "status": packages["status"], "version": VERSION, "packageDirectory": packages["packageDirectory"],
                 "source": packages["source"], "startedAt": "2026-11-03T12:00:00.1234560Z", "completedAt": "2026-11-03T12:00:01.0000000Z",
                 "pushed": packages["pushed"], "failed": [], "notAttempted": [], "recovery": "Do not republish or delete NuGet packages."}
        package_path.write_text(json.dumps(nuget, indent=2) + "\n", encoding="utf-8")
        shutil.copyfile(ROOT / "tools/publish-release.ps1", self.root / "tools/publish-release.ps1")
        fake = self.root / "round-trip-bin"
        fake.mkdir()
        for name in ("python3", "child-pwsh"):
            (fake / name).write_text("#!/bin/sh\nexit 0\n")
            (fake / name).chmod(0o755)
        pwsh = shutil.which("pwsh")
        result = subprocess.run([pwsh, "-NoLogo", "-NoProfile", "-File", str(self.root / "tools/publish-release.ps1"), "-Version", VERSION, "-PowerShellExecutable", str(fake / "child-pwsh")],
                                cwd=self.root, env=dict(os.environ, PATH=str(fake) + os.pathsep + os.environ["PATH"]), capture_output=True, text=True, check=False)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        aggregate_path = self.root / "artifacts/release/publish-summary.json"
        aggregate = CONTRACT.read_json(aggregate_path)
        embedded = aggregate["families"][0]["summary"]
        self.assertEqual(("2026-11-03T12:00:00.123456Z", "2026-11-03T12:00:01Z"), (embedded["startedAt"], embedded["completedAt"]), "pwsh no longer trims; the round-trip case is not exercised")
        with working_directory(self.root):
            ARCHIVE.validate_summaries(self.identity, Path("artifacts/containers/release"), Path("artifacts/packages/release"), Path("artifacts/release"))
        for field, value in (("recovery", "Different recovery guidance."), ("startedAt", "2026-11-03T12:00:00.1234561Z")):
            changed = deepcopy(aggregate)
            changed["families"][0]["summary"][field] = value
            CONTRACT.write_json(aggregate_path, changed)
            with self.subTest(field=field), working_directory(self.root), self.assertRaisesRegex(ValueError, "Aggregate family summary contradicts"):
                ARCHIVE.validate_summaries(self.identity, Path("artifacts/containers/release"), Path("artifacts/packages/release"), Path("artifacts/release"))

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


class CommandRunnerTests(unittest.TestCase):
    def test_timed_out_command_keeps_its_partial_redacted_log(self):
        with tempfile.TemporaryDirectory() as temporary, mock.patch.dict(os.environ, {"HEXALITH_ZOT_API_KEY": "fixture-timeout-secret"}):
            log = Path(temporary) / "server/transport.log"
            script = "echo partial transport output; echo using fixture-timeout-secret >&2; exec sleep 30"
            with self.assertRaisesRegex(ValueError, r"^sh timed out after 1 s") as refusal:
                CONTRACT.run(["sh", "-c", script], log, timeout=1)
            text = log.read_text(encoding="utf-8")
        self.assertIn("partial transport output", text)
        self.assertIn("using [redacted]", text)
        self.assertIn("[timeout] sh did not finish within 1 s.", text)
        self.assertIn("partial transport output", str(refusal.exception))
        self.assertNotIn("fixture-timeout-secret", text + str(refusal.exception))


class WorkloadHealthTests(unittest.TestCase):
    def test_server_dependency_failures_keep_bounded_redacted_process_output(self):
        for stage in ("pull", "start", "readiness"):
            def command(arguments):
                failed = ((stage == "pull" and arguments[:2] == ["docker", "pull"])
                          or (stage == "start" and arguments[:2] == ["docker", "run"])
                          or (stage == "readiness" and arguments[:2] == ["docker", "exec"]))
                return SimpleNamespace(returncode=int(failed), stdout="x" * 5000 + "dependency diagnostic\n" if failed else "",
                                       stderr="connection refused with fixture-zot-user:fixture-zot-secret" if failed else "")
            smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass")
            with self.subTest(stage=stage), tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command), \
                    mock.patch.object(HEALTH.time, "monotonic", side_effect=[0, 31]), \
                    mock.patch.dict(os.environ, {"HEXALITH_ZOT_USERNAME": "fixture-zot-user", "HEXALITH_ZOT_API_KEY": "fixture-zot-secret"}):
                result = HEALTH.run_platform("server", "registry.test/memories", "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
                log = (Path(temporary) / "health-amd64.log").read_text()
            self.assertEqual("workload-health-failure", result["outcome"])
            self.assertIn("dependency diagnostic", log)
            self.assertIn("connection refused", log)
            self.assertNotIn("fixture-zot-user", log)
            self.assertNotIn("fixture-zot-secret", log)
            self.assertLess(len(log), 4500)

    def test_timeout_keeps_safe_partial_output_without_the_generated_dapr_token(self):
        token = "generated-dapr-token-never-retain"
        def command(arguments):
            if arguments[:2] == ["docker", "run"]:
                raise subprocess.TimeoutExpired(arguments, 150, output=("x" * 5000 + "partial timeout output " + token).encode(), stderr=b"authority did not start")
            return SimpleNamespace(returncode=0, stdout="", stderr="")
        smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass")
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command), mock.patch.object(HEALTH.secrets, "token_hex", return_value=token):
            result = HEALTH.run_platform("mcp", "registry.test/memories-mcp", "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
            log = (Path(temporary) / "health-amd64.log").read_text()
        self.assertEqual("workload-health-failure", result["outcome"])
        self.assertIn("timed out after 150 s", log)
        self.assertIn("partial timeout output [redacted]", log)
        self.assertIn("authority did not start", log)
        self.assertNotIn(token, log)
        self.assertNotIn("--env", log)
        self.assertNotIn("DAPR_API_TOKEN", log)
        self.assertLess(len(log), 4500)

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
                    self.assertTrue(any(value.startswith("DAPR_GRPC_ENDPOINT=http://memories-dapr-authority-") for value in environment))
                return SimpleNamespace(returncode=0, stdout="", stderr="")
            smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _poll_liveness=lambda *_: ("pass", 1), _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "pass", SMOKE_JWT_ISSUER="issuer", SMOKE_JWT_AUDIENCE="audience", SMOKE_JWT_SIGNING_KEY="development-key")
            with self.subTest(cleanup_failure=cleanup_failure), tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command):
                result = HEALTH.run_platform("server", "registry.test/memories", "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
                self.assertEqual("cleanup-failure" if cleanup_failure else "pass", result["outcome"])
                log = (Path(temporary) / "health-amd64.log").read_text()
                self.assertIn("dependency=redis;image=", log)
                self.assertIn("dependency=falkordb;image=", log)
                self.assertEqual(3, len([call for call in calls if call[:3] == ["docker", "rm", "--force"]]))
                self.assertTrue(any(call[:4] == ["docker", "network", "create", "--internal"] for call in calls))
                workload = next(call for call in calls if call[:2] == ["docker", "run"] and call[-1].startswith("registry.test/memories@"))
                self.assertNotIn("--publish", workload)
                probe = next(call for call in calls if call[:3] == ["docker", "run", "--rm"])
                self.assertIn("container:memories-publication-server-amd64-", " ".join(probe))
                self.assertIn(HEALTH.PROBE_IMAGE, probe)
                self.assertTrue(probe[-1].endswith("/alive"))
                if cleanup_failure:
                    self.assertIn("error=CleanupFailure: Isolated Server dependency cleanup failed.", log)

    def test_network_creation_failure_is_a_workload_failure_with_its_cause_retained(self):
        calls = []
        def command(arguments):
            calls.append(arguments)
            return SimpleNamespace(returncode=1 if arguments[:3] == ["docker", "network", "create"] else 0, stdout="", stderr="")
        smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "pass")
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command):
            result = HEALTH.run_platform("mcp", "registry.test/memories-mcp", "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
            log = (Path(temporary) / "health-amd64.log").read_text()
        self.assertEqual(("workload-health-failure", "pass"), (result["outcome"], result["cleanup"]))
        self.assertIn("error=ValueError: Isolated test network could not be created.", log)
        self.assertFalse(any(call[:3] == ["docker", "network", "rm"] for call in calls))
        create = next(call for call in calls if call[:3] == ["docker", "network", "create"])
        self.assertIn("--internal", create)

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
                    return SimpleNamespace(returncode=0, stdout=json.dumps(body) if arguments[:3] == ["docker", "run", "--rm"] and arguments[-1].endswith("/ready") else "alive passes", stderr="")
                smoke = SimpleNamespace(_pull_immutable=lambda *_: "pass", _poll_liveness=mock.Mock(return_value=("pass", 1)), _append_runtime_evidence=lambda *_: None, _remove_container=lambda *_: "pass", _run_or_none=lambda *_: SimpleNamespace(returncode=0, stdout="127.0.0.1:12345"), _container_state=lambda *_: "running|0", SMOKE_JWT_ISSUER="issuer", SMOKE_JWT_AUDIENCE="audience", SMOKE_JWT_SIGNING_KEY="development-key")
                with self.subTest(name=name, valid=valid), tempfile.TemporaryDirectory() as temporary, mock.patch.object(HEALTH, "command", side_effect=command), mock.patch.object(HEALTH, "clock_authority", authority), mock.patch.object(HEALTH.time, "monotonic", side_effect=[0, 2]):
                    result = HEALTH.run_platform(name, "registry.test/" + name, "linux/amd64", "sha256:" + "c" * 64, Path(temporary), smoke, 1)
                self.assertEqual("pass" if valid else "readiness-timeout", result["outcome"])
                self.assertTrue(any(call[:3] == ["docker", "run", "--rm"] and call[-1].endswith("/ready") for call in calls))
                smoke._poll_liveness.assert_not_called()

    def test_clock_authority_is_ephemeral_grpc_and_cleanup_failure_is_terminal(self):
        original = HEALTH.command
        calls = []
        secret_paths = []
        fail_cleanup = False
        def controlled(arguments):
            calls.append(arguments)
            if arguments[0] != "docker":
                return original(arguments)
            return SimpleNamespace(returncode=1 if arguments[:3] == ["docker", "rm", "--force"] and fail_cleanup else 0, stdout="", stderr="")
        with mock.patch.object(HEALTH, "command", side_effect=controlled), self.assertRaises(HEALTH.CleanupFailure):
            with HEALTH.clock_authority("fixture-isolated-network") as settings:
                self.assertIn(":50001", settings["DAPR_GRPC_ENDPOINT"])
                self.assertLessEqual(len(settings["DAPR_GRPC_ENDPOINT"].split("://")[1].split(":")[0]), 63)
                self.assertEqual("access-telemetry-secrets", settings["Clock__SecretStoreName"])
                self.assertEqual("true", settings["Clock__AllowDevelopmentSources"])
                start = next(call for call in calls if call[:2] == ["docker", "run"])
                mount = start[start.index("--mount") + 1]
                mount_fields = dict(field.split("=", 1) for field in mount.split(",") if "=" in field)
                source = Path(mount_fields["source"])
                destination = PurePosixPath(mount_fields["destination"])
                resources = PurePosixPath(start[start.index("--resources-path") + 1])
                component = source / resources.relative_to(destination) / "secretstore.yaml"
                secret_file = re.search(r"- name: secretsFile\s*\n\s+value:\s*(\S+)", component.read_text()).group(1)
                mounted_secret = (source / PurePosixPath(secret_file).relative_to(destination)).resolve()
                self.assertEqual((source / "secrets.json").resolve(), mounted_secret)
                secret_paths.append(source)
                self.assertTrue(mounted_secret.is_file())
                self.assertEqual(0o600, mounted_secret.stat().st_mode & 0o777)
                # A flat secret survives Dapr's local-file flattening; the clock reads it through its secret-name fallback.
                signing_key = CONTRACT.read_json(mounted_secret)["access-telemetry-clock-key"]
                public = subprocess.run(["openssl", "pkey", "-inform", "DER", "-pubout", "-outform", "DER"],
                                        input=base64.b64decode(signing_key, validate=True), capture_output=True, check=True).stdout
                self.assertEqual(settings["AccessTelemetryLifecycle__AttestationVerificationKey"], base64.b64encode(public).decode())
                self.assertEqual("readonly", mount.split(",")[-1])
                self.assertEqual("fixture-isolated-network", start[start.index("--network") + 1])
                # Only inject cleanup failure after the assertions, so it cannot hide a bad secret path/key.
                fail_cleanup = True
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


class DockerWorkloadHealthTests(unittest.TestCase):
    def test_real_docker_internal_network_probe_reaches_liveness_and_cleans_up(self):
        if shutil.which("docker") is None:
            self.skipTest("The docker CLI is unavailable, so real Docker networking cannot be exercised.")
        try:
            daemon = subprocess.run(["docker", "info"], capture_output=True, check=False, timeout=60).returncode
        except subprocess.TimeoutExpired:
            daemon = 1
        if daemon:
            self.skipTest("The Docker daemon is unavailable, so real Docker networking cannot be exercised.")
        with tempfile.TemporaryDirectory() as temporary:
            shared = Path(temporary) / "builds/Github/publish-containers"
            write_approved_helpers(shared)
            validator = load(shared / "oci_registry_validator.py", "approved_docker_oci")
            smoke = load_with_validator(shared / "smoke_container_platforms.py", "approved_docker_smoke", validator)
            evidence = Path(temporary) / "evidence"
            evidence.mkdir()
            # verify() pulls the digest-pinned probe before any platform run; the probe uses --pull=never.
            pulled = HEALTH.command(["docker", "pull", "--platform", "linux/amd64", HEALTH.PROBE_IMAGE])
            self.assertEqual(0, pulled.returncode, pulled.stdout + pulled.stderr)
            # About 10 MB; listens on 8080 and answers 200 on /alive. DomainService liveness needs no sidecar.
            result = HEALTH.run_platform("domain-service", "docker.io/jmalloc/echo-server", "linux/amd64",
                                         "sha256:c87f80de8dbb976a59b228fc9ecf257e0574c6f760c2f1c5f05f64c7ac7fbc1e", evidence, smoke, 60)
            log = (evidence / "health-amd64.log").read_text()
        self.assertEqual("pass", result["outcome"], log)
        self.assertEqual("pass", result["cleanup"], log)


if __name__ == "__main__":
    unittest.main()
