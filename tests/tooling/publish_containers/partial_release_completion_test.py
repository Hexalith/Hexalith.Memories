import json
import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import threading
import unittest
import zipfile
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "complete-partial-release.ps1"
VERSION = "1.2.3"
TAG = f"v{VERSION}"
PACKAGE_IDS = [
    "Hexalith.Memories.Aspire",
    "Hexalith.Memories.Contracts",
    "Hexalith.Memories.Client.Rest",
    "Hexalith.Memories.Redis",
    "Hexalith.Memories.Cli",
    "Hexalith.Memories.Mcp",
    "Hexalith.Memories.ServiceDefaults",
    "Hexalith.Memories.EventStore",
    "Hexalith.Memories.Telemetry",
]
IMAGE_REPOSITORIES = [
    "memories",
    "memories-mcp",
    "memories-access-telemetry",
    "memories-access-telemetry-clock",
]


def git(root: Path, *args: str) -> None:
    subprocess.run(["git", "-C", str(root), *args], check=True, capture_output=True, text=True)


def prepare_tagged_source(
    root: Path,
    image_repositories: list[str] | None = None,
) -> tuple[Path, Path]:
    repositories = list(image_repositories or IMAGE_REPOSITORIES)
    tools = root / "tools"
    tools.mkdir(parents=True)
    (tools / "release-packages.json").write_text(
        json.dumps({"packages": [{"packageId": package_id} for package_id in PACKAGE_IDS]}),
        encoding="utf-8",
    )
    marker = root / "release-source.txt"
    marker.write_text("previous\n", encoding="utf-8")
    git(root, "init", "-b", "main")
    git(root, "config", "user.email", "ci@example.test")
    git(root, "config", "user.name", "CI Fixture")
    git(root, "add", ".")
    git(root, "commit", "-m", "previous")
    git(root, "tag", "v1.2.2")
    marker.write_text("current\n", encoding="utf-8")
    git(root, "add", ".")
    git(root, "commit", "-m", "current")
    git(root, "tag", TAG)

    container_summary = root / "publish-summary.json"
    container_summary.write_text(
        json.dumps(
            {
                "status": "succeeded",
                "version": VERSION,
                "images": [
                    {
                        "image": f"registry.test/{repository}:{VERSION}",
                        "status": "succeeded",
                        "disposition": "pushed" if index % 2 == 0 else "already-present",
                    }
                    for index, repository in enumerate(repositories)
                ],
            }
        ),
        encoding="utf-8",
    )
    deployment = root / "production-deployment.yaml"
    deployment.write_text(
        "images:\n"
        + "".join(f"- registry.test/{repository}:{VERSION}\n" for repository in repositories),
        encoding="utf-8",
    )
    return container_summary, deployment


def package_bytes(package_id: str) -> bytes:
    with tempfile.SpooledTemporaryFile() as package:
        with zipfile.ZipFile(package, "w") as archive:
            archive.writestr(
                f"{package_id}.nuspec",
                (
                    '<?xml version="1.0" encoding="utf-8"?>'
                    '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">'
                    f"<metadata><id>{package_id}</id><version>{VERSION}</version></metadata>"
                    "</package>"
                ),
            )
        package.seek(0)
        return package.read()


class PackageServer:
    def __init__(self) -> None:
        packages = {
            f"/{package_id.lower()}/{VERSION}/{package_id.lower()}.{VERSION}.nupkg": package_bytes(package_id)
            for package_id in PACKAGE_IDS
        }

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
                content = packages.get(self.path)
                if content is None:
                    self.send_response(404)
                    self.end_headers()
                    return
                self.send_response(200)
                self.send_header("Content-Length", str(len(content)))
                self.end_headers()
                self.wfile.write(content)

            def log_message(self, format: str, *args: object) -> None:
                return

        self.server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)

    def __enter__(self) -> "PackageServer":
        self.thread.start()
        return self

    def __exit__(self, *_: object) -> None:
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    @property
    def origin(self) -> str:
        return f"http://127.0.0.1:{self.server.server_port}"


def write_fake_gh(directory: Path) -> None:
    script = directory / "fake_gh.py"
    script.write_text(
        textwrap.dedent(
            f"""
            import json
            import os
            import shutil
            import sys
            from pathlib import Path

            args = sys.argv[1:]
            state_path = Path(os.environ["FAKE_GH_STATE"])
            asset_dir = Path(os.environ["FAKE_GH_ASSETS"])
            log_path = Path(os.environ["FAKE_GH_LOG"])
            state = json.loads(state_path.read_text(encoding="utf-8"))
            asset_dir.mkdir(parents=True, exist_ok=True)
            with log_path.open("a", encoding="utf-8") as log:
                log.write(json.dumps(args) + "\\n")

            def save():
                state_path.write_text(json.dumps(state), encoding="utf-8")

            def copy_file_arguments(start):
                for value in args[start:]:
                    path = Path(value)
                    if path.is_file():
                        shutil.copyfile(path, asset_dir / path.name)

            if args[:2] == ["release", "view"]:
                if not state["release"]:
                    print("release not found", file=sys.stderr)
                    sys.exit(1)
                print(json.dumps({{
                    "tagName": "{TAG}",
                    "isDraft": False,
                    "isPrerelease": False,
                    "assets": [{{"name": path.name}} for path in sorted(asset_dir.iterdir())],
                }}))
            elif args[:2] == ["release", "create"]:
                if state["release"]:
                    print("release already exists", file=sys.stderr)
                    sys.exit(1)
                copy_file_arguments(3)
                state["release"] = True
                save()
            elif args[:2] == ["release", "upload"]:
                copy_file_arguments(3)
                save()
            elif args[:2] == ["release", "download"]:
                destination = Path(args[args.index("--dir") + 1])
                destination.mkdir(parents=True, exist_ok=True)
                for path in asset_dir.iterdir():
                    shutil.copyfile(path, destination / path.name)
            elif args[:2] == ["issue", "list"]:
                issues = [{{"number": 32, "title": "PARTIAL PUBLISH {VERSION} - manual reconciliation required"}}] if state["issue_open"] else []
                print(json.dumps(issues))
            elif args[:2] == ["issue", "comment"]:
                pass
            elif args[:2] == ["issue", "close"]:
                state["issue_open"] = False
                save()
            else:
                print(f"unexpected gh arguments: {{args}}", file=sys.stderr)
                sys.exit(98)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    executable = directory / "gh"
    executable.write_text(f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n', encoding="utf-8")
    executable.chmod(0o755)


def run_completion(
    source: Path,
    summary: Path,
    deployment: Path,
    nuget_origin: str,
    env: dict[str, str],
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(SCRIPT),
            "-Version",
            VERSION,
            "-SourceRoot",
            str(source),
            "-ContainerSummaryPath",
            str(summary),
            "-DeploymentPath",
            str(deployment),
            "-Repository",
            "owner/repo",
            "-Registry",
            "registry.test",
            "-OutputDirectory",
            "recovery-output",
            "-NuGetBaseUri",
            nuget_origin,
            "-Publish",
        ],
        cwd=source,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )


class PartialReleaseCompletionTests(unittest.TestCase):
    def prepare(
        self,
        root: Path,
        image_repositories: list[str] | None = None,
    ) -> tuple[Path, Path, Path, dict[str, str]]:
        source = root / "source"
        source.mkdir()
        summary, deployment = prepare_tagged_source(source, image_repositories=image_repositories)
        fake_bin = root / "bin"
        fake_bin.mkdir()
        write_fake_gh(fake_bin)
        state = root / "gh-state.json"
        state.write_text(json.dumps({"release": False, "issue_open": True}), encoding="utf-8")
        env = os.environ.copy()
        env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
        env["FAKE_GH_STATE"] = str(state)
        env["FAKE_GH_ASSETS"] = str(root / "remote-assets")
        env["FAKE_GH_LOG"] = str(root / "gh-log.jsonl")
        env["GH_TOKEN"] = "SECRET_GITHUB_TOKEN_SHOULD_NOT_LEAK"
        return source, summary, deployment, env

    def test_completion_creates_verified_release_closes_issue_and_is_idempotent(self) -> None:
        with tempfile.TemporaryDirectory() as temp, PackageServer() as nuget:
            root = Path(temp)
            source, container_summary, deployment, env = self.prepare(root)

            first = run_completion(source, container_summary, deployment, nuget.origin, env)
            second = run_completion(source, container_summary, deployment, nuget.origin, env)

            self.assertEqual(0, first.returncode, first.stdout + first.stderr)
            self.assertEqual(0, second.returncode, second.stdout + second.stderr)
            evidence = json.loads(
                (source / "recovery-output" / "completion-summary.json").read_text(encoding="utf-8-sig")
            )
            self.assertEqual("succeeded", evidence["status"])
            self.assertEqual(9, len(evidence["packages"]))
            self.assertEqual(4, len(evidence["images"]))
            self.assertEqual(10, len(evidence["releaseAssets"]))
            commands = [
                json.loads(line)
                for line in Path(env["FAKE_GH_LOG"]).read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual(1, sum(command[:2] == ["release", "create"] for command in commands))
            self.assertEqual(0, sum(command[:2] == ["release", "upload"] for command in commands))
            self.assertEqual(1, sum(command[:2] == ["issue", "close"] for command in commands))
            combined = first.stdout + first.stderr + second.stdout + second.stderr
            self.assertNotIn(env["GH_TOKEN"], combined)

    def test_historical_two_image_unit_completes_without_access_telemetry(self) -> None:
        with tempfile.TemporaryDirectory() as temp, PackageServer() as nuget:
            root = Path(temp)
            source, container_summary, deployment, env = self.prepare(
                root,
                image_repositories=["memories", "memories-mcp"],
            )

            result = run_completion(source, container_summary, deployment, nuget.origin, env)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            evidence = json.loads(
                (source / "recovery-output" / "completion-summary.json").read_text(encoding="utf-8-sig")
            )
            self.assertEqual("succeeded", evidence["status"])
            self.assertEqual(2, len(evidence["images"]))
            self.assertEqual(
                [
                    f"registry.test/memories:{VERSION}",
                    f"registry.test/memories-mcp:{VERSION}",
                ],
                [image["image"] for image in evidence["images"]],
            )
            commands = [
                json.loads(line)
                for line in Path(env["FAKE_GH_LOG"]).read_text(encoding="utf-8").splitlines()
            ]
            comment = next(command for command in commands if command[:2] == ["issue", "comment"])
            self.assertTrue(any("2 immutable image(s)" in arg for arg in comment))

    def test_existing_release_asset_conflict_fails_before_incident_closure(self) -> None:
        with tempfile.TemporaryDirectory() as temp, PackageServer() as nuget:
            root = Path(temp)
            source, container_summary, deployment, env = self.prepare(root)
            first = run_completion(source, container_summary, deployment, nuget.origin, env)
            self.assertEqual(0, first.returncode, first.stdout + first.stderr)

            remote_asset = next(Path(env["FAKE_GH_ASSETS"]).glob("*.nupkg"))
            remote_asset.write_bytes(b"conflicting immutable asset")
            state_path = Path(env["FAKE_GH_STATE"])
            state = json.loads(state_path.read_text(encoding="utf-8"))
            state["issue_open"] = True
            state_path.write_text(json.dumps(state), encoding="utf-8")
            previous_commands = len(Path(env["FAKE_GH_LOG"]).read_text(encoding="utf-8").splitlines())

            conflict = run_completion(source, container_summary, deployment, nuget.origin, env)

            self.assertNotEqual(0, conflict.returncode)
            evidence = json.loads(
                (source / "recovery-output" / "completion-summary.json").read_text(encoding="utf-8-sig")
            )
            self.assertEqual("failed", evidence["status"])
            self.assertIn("did not match", evidence["error"])
            new_commands = [
                json.loads(line)
                for line in Path(env["FAKE_GH_LOG"]).read_text(encoding="utf-8").splitlines()[previous_commands:]
            ]
            self.assertFalse(any(command[:2] == ["issue", "close"] for command in new_commands))
            self.assertNotIn(env["GH_TOKEN"], conflict.stdout + conflict.stderr)


if __name__ == "__main__":
    unittest.main()
