import json
import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "publish-containers.ps1"
VERSION = "1.2.3-test.1"
SERVER_IMAGE = f"registry.test/memories:{VERSION}"
MCP_IMAGE = f"registry.test/memories-mcp:{VERSION}"


def write_executable(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
    os.chmod(path, 0o755)


def write_fake_dotnet(directory: Path) -> None:
    script = directory / "fake_dotnet.py"
    script.write_text(
        textwrap.dedent(
            """
            import json
            import os
            import sys
            from pathlib import Path

            args = sys.argv[1:]
            if args and args[0].endswith("pwsh.dll"):
                real_dotnet = os.environ["REAL_DOTNET"]
                os.execv(real_dotnet, [real_dotnet] + args)

            project = args[1] if len(args) >= 2 and args[0] == "publish" else ""
            if project.endswith("Hexalith.Memories.Server.csproj"):
                image = "server"
            elif project.endswith("Hexalith.Memories.Mcp.csproj"):
                image = "mcp"
            else:
                print(f"unexpected dotnet arguments: {args}", file=sys.stderr)
                sys.exit(97)

            with open(os.environ["FAKE_COMMAND_LOG"], "a", encoding="utf-8") as log:
                log.write(json.dumps({"command": "dotnet", "image": image, "args": args}) + "\\n")

            archive_argument = next(
                (arg for arg in args if arg.startswith("-p:ContainerArchiveOutputPath=")),
                None,
            )
            if archive_argument is None:
                print("missing ContainerArchiveOutputPath", file=sys.stderr)
                sys.exit(96)
            archive = Path(archive_argument.split("=", 1)[1])
            archive.parent.mkdir(parents=True, exist_ok=True)
            archive.write_bytes((image + "-archive").encode("utf-8"))
            sys.exit(0)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "dotnet",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )
    (directory / "dotnet.cmd").write_text(
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )


def write_fake_kubectl(directory: Path) -> None:
    script = directory / "fake_kubectl.py"
    script.write_text(
        textwrap.dedent(
            f"""
            import os
            import sys

            warning = os.environ.get("FAKE_KUBECTL_WARNING", "")
            if warning:
                print(warning, file=sys.stderr)
            exit_code = int(os.environ.get("FAKE_KUBECTL_EXIT", "0"))
            if exit_code:
                print("synthetic kustomize failure", file=sys.stderr)
                sys.exit(exit_code)

            print("apiVersion: v1")
            print("kind: List")
            print("items:")
            print("  - image: registry.hexalith.com/memories:0.0.0")
            print("  - image: registry.hexalith.com/memories-mcp:0.0.0")
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "kubectl",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )
    (directory / "kubectl.cmd").write_text(
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )


def write_fake_skopeo(directory: Path) -> None:
    script = directory / "fake_skopeo.py"
    script.write_text(
        textwrap.dedent(
            f"""
            import base64
            import json
            import os
            import sys
            from pathlib import Path

            args = sys.argv[1:]
            state_path = Path(os.environ["FAKE_SKOPEO_STATE"])
            plan_path = Path(os.environ["FAKE_SKOPEO_PLAN"])
            state = json.loads(state_path.read_text(encoding="utf-8")) if state_path.exists() else {{"remote": {{}}, "attempts": {{}}}}
            plan = json.loads(plan_path.read_text(encoding="utf-8"))
            digests = {{
                "server": "sha256:server-config",
                "mcp": "sha256:mcp-config",
            }}

            def image_for_archive(path: str) -> str:
                return "server" if Path(path).name.startswith("server") else "mcp"

            def image_for_reference(reference: str) -> str:
                return "server" if reference == "{SERVER_IMAGE}" else "mcp"

            entry = {{"command": "skopeo", "args": args}}
            if "--authfile" in args:
                authfile = Path(args[args.index("--authfile") + 1])
                entry["authfile_path"] = str(authfile)
                entry["authfile_mode"] = oct(authfile.stat().st_mode & 0o777) if authfile.exists() else "missing"
                expected_user = os.environ.get("HEXALITH_ZOT_USERNAME", "")
                expected_key = os.environ.get("HEXALITH_ZOT_API_KEY", "")
                expected_auth = base64.b64encode(f"{{expected_user}}:{{expected_key}}".encode()).decode()
                try:
                    # Strict utf-8: a BOM must break parsing exactly like Go's JSON decoder in real skopeo.
                    document = json.loads(authfile.read_text(encoding="utf-8"))
                    recorded = document["auths"][os.environ["EXPECTED_ZOT_REGISTRY"]]["auth"]
                    entry["authfile_valid"] = recorded == expected_auth
                except (OSError, KeyError, ValueError):
                    entry["authfile_valid"] = False

            with open(os.environ["FAKE_COMMAND_LOG"], "a", encoding="utf-8") as log:
                log.write(json.dumps(entry) + "\\n")

            forbidden_flags = {{"--creds", "--src-creds", "--dest-creds", "--dest-password", "--dest-username"}}
            if forbidden_flags.intersection(args):
                print(f"forbidden credential argument in: {{args}}", file=sys.stderr)
                sys.exit(99)

            def emit(outcome: dict) -> None:
                if outcome.get("stdout"):
                    print(outcome["stdout"])
                if outcome.get("stderr"):
                    print(outcome["stderr"], file=sys.stderr)

            if args[:2] == ["inspect", "--raw"] and len(args) == 3 and args[2].startswith("docker-archive:"):
                image = image_for_archive(args[2].removeprefix("docker-archive:"))
                outcome = plan.get("archiveInspect", {{}}).get(image, {{}})
                emit(outcome)
                exit_code = int(outcome.get("exitCode", 0))
                if exit_code == 0 and not outcome.get("stdout"):
                    print(json.dumps({{"schemaVersion": 2, "config": {{"digest": digests[image]}}}}))
                state_path.write_text(json.dumps(state), encoding="utf-8")
                sys.exit(exit_code)
            elif args[0] == "inspect" and len(args) == 2 and args[1].startswith("docker-archive:"):
                image = image_for_archive(args[1].removeprefix("docker-archive:"))
                outcome = plan.get("archiveReferences", {{}}).get(image, {{}})
                emit(outcome)
                exit_code = int(outcome.get("exitCode", 0))
                if exit_code == 0 and not outcome.get("stdout"):
                    repository = "memories" if image == "server" else "memories-mcp"
                    print(json.dumps({{"RepoTags": [f"{{repository}}:{VERSION}"]}}))
                state_path.write_text(json.dumps(state), encoding="utf-8")
                sys.exit(exit_code)
            elif args[:2] == ["inspect", "--raw"] and "--authfile" in args and args[-1].startswith("docker://"):
                reference = args[-1].removeprefix("docker://")
                image = image_for_reference(reference)
                outcome = plan.get("remoteInspect", {{}}).get(image)
                if outcome is not None:
                    emit(outcome)
                    state_path.write_text(json.dumps(state), encoding="utf-8")
                    sys.exit(int(outcome.get("exitCode", 0)))
                digest = state["remote"].get(reference)
                state_path.write_text(json.dumps(state), encoding="utf-8")
                if digest is None:
                    print(f"reading manifest {{reference}}: manifest unknown", file=sys.stderr)
                    sys.exit(1)
                print(json.dumps({{"schemaVersion": 2, "config": {{"digest": digest}}}}))
                sys.exit(0)
            elif args and args[0] == "copy" and "--authfile" in args:
                source = next(arg for arg in args if arg.startswith("docker-archive:"))
                reference = next(arg for arg in args if arg.startswith("docker://")).removeprefix("docker://")
                image = image_for_reference(reference)
                attempt = int(state["attempts"].get(image, 0))
                state["attempts"][image] = attempt + 1
                outcomes = plan.get(image, [{{"exitCode": 0}}])
                outcome = outcomes[min(attempt, len(outcomes) - 1)]
                emit(outcome)
                if int(outcome.get("exitCode", 0)) == 0:
                    state["remote"][reference] = digests[image_for_archive(source.removeprefix("docker-archive:"))]
                state_path.write_text(json.dumps(state), encoding="utf-8")
                sys.exit(int(outcome.get("exitCode", 0)))
            else:
                print(f"unexpected skopeo arguments: {{args}}", file=sys.stderr)
                sys.exit(98)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "skopeo",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )
    (directory / "skopeo.cmd").write_text(
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )


def prepare_environment(root: Path, plan: dict[str, object] | None = None) -> tuple[Path, dict[str, str]]:
    fake_bin = root / "bin"
    fake_bin.mkdir()
    write_fake_dotnet(fake_bin)
    write_fake_kubectl(fake_bin)
    write_fake_skopeo(fake_bin)

    plan_path = root / "skopeo-plan.json"
    plan_path.write_text(json.dumps(plan or {}), encoding="utf-8")
    env = os.environ.copy()
    env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_SKOPEO_PLAN"] = str(plan_path)
    env["FAKE_SKOPEO_STATE"] = str(root / "skopeo-state.json")
    env["FAKE_COMMAND_LOG"] = str(root / "commands.jsonl")
    env["GITHUB_ACTIONS"] = "true"
    env["GH_TOKEN"] = "SECRET_GITHUB_TOKEN_SHOULD_NOT_LEAK"
    env["NUGET_API_KEY"] = "SECRET_NUGET_TOKEN_SHOULD_NOT_LEAK"
    env["EXPECTED_ZOT_REGISTRY"] = "registry.test"
    env["HEXALITH_ZOT_USERNAME"] = "SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK"
    env["HEXALITH_ZOT_API_KEY"] = "SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK"
    return fake_bin, env


def prepare_archives(output: Path) -> None:
    output.mkdir(parents=True, exist_ok=True)
    (output / "server.tar.gz").write_bytes(b"server-archive")
    (output / "mcp.tar.gz").write_bytes(b"mcp-archive")


def run_publish(
    output: Path,
    env: dict[str, str],
    *,
    push: bool,
    registry: str | None = "registry.test",
    kubectl_exit: int = 0,
    kubectl_warning: str = "",
    extra_arguments: list[str] | None = None,
) -> subprocess.CompletedProcess[str]:
    invocation = [
        "pwsh",
        "-NoLogo",
        "-NoProfile",
        "-File",
        str(SCRIPT),
        "-Version",
        VERSION,
        "-OutputDirectory",
        str(output),
    ]
    if registry is not None:
        invocation.extend(["-Registry", registry])
    if push:
        invocation.append("-Push")
    if extra_arguments:
        invocation.extend(extra_arguments)
    run_env = env.copy()
    run_env["FAKE_KUBECTL_EXIT"] = str(kubectl_exit)
    run_env["FAKE_KUBECTL_WARNING"] = kubectl_warning
    return subprocess.run(
        invocation,
        cwd=REPO_ROOT,
        env=run_env,
        capture_output=True,
        text=True,
        check=False,
    )


def command_log(root: Path) -> list[dict[str, object]]:
    path = root / "commands.jsonl"
    if not path.exists():
        return []
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]


def skopeo_calls(root: Path) -> list[dict[str, object]]:
    return [entry for entry in command_log(root) if entry["command"] == "skopeo"]


def copy_references(root: Path) -> list[str]:
    return [
        next(arg for arg in entry["args"] if arg.startswith("docker://")).removeprefix("docker://")
        for entry in skopeo_calls(root)
        if entry["args"][0] == "copy"
    ]


def assert_authenticated_calls_use_scoped_authfile(test: unittest.TestCase, root: Path) -> None:
    authenticated = [entry for entry in skopeo_calls(root) if "--authfile" in entry["args"]]
    test.assertGreater(len(authenticated), 0)
    for entry in authenticated:
        test.assertTrue(entry["authfile_valid"], entry)
        test.assertEqual("0o600", entry["authfile_mode"], entry)


class PublishContainersTests(unittest.TestCase):
    def test_registry_environment_override_drives_push_and_render_targets(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            registry = "registry.env.test:5443"
            env["HEXALITH_ZOT_REGISTRY"] = registry
            env["EXPECTED_ZOT_REGISTRY"] = registry
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True, registry=None)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            expected_images = [
                f"{registry}/memories:{VERSION}",
                f"{registry}/memories-mcp:{VERSION}",
            ]
            self.assertEqual(expected_images, copy_references(root))
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual(registry, summary["source"])
            self.assertEqual(expected_images, [image["image"] for image in summary["images"]])
            deployment = (output / "production-deployment.yaml").read_text(encoding="utf-8-sig")
            for image in expected_images:
                self.assertIn(image, deployment)
            assert_authenticated_calls_use_scoped_authfile(self, root)

    def test_invalid_registry_override_fails_before_build_or_push(self) -> None:
        invalid_registries = [
            "https://registry.test",
            "registry.test/path",
            "registry.test/",
            "registry test",
            "registry.test:70000",
            "user@registry.test",
        ]
        for registry in invalid_registries:
            with self.subTest(registry=registry), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                _, env = prepare_environment(root)
                output = root / "artifacts"

                result = run_publish(output, env, push=False, registry=registry)

                self.assertNotEqual(0, result.returncode)
                combined = result.stdout + result.stderr
                self.assertIn("HEXALITH_ZOT_REGISTRY", combined)
                self.assertIn("registry host with an optional port", combined)
                self.assertEqual([], command_log(root))

    def test_build_creates_both_archives_with_exact_publish_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"

            result = run_publish(output, env, push=False)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertGreater((output / "server.tar.gz").stat().st_size, 0)
            self.assertGreater((output / "mcp.tar.gz").stat().st_size, 0)
            summary = json.loads((output / "build-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])

            calls = [entry for entry in command_log(root) if entry["command"] == "dotnet"]
            self.assertEqual(["server", "mcp"], [entry["image"] for entry in calls])
            expected_repositories = ["memories", "memories-mcp"]
            for call, repository in zip(calls, expected_repositories, strict=True):
                args = call["args"]
                self.assertIn("-t:PublishContainer", args)
                self.assertIn("-p:ContainerRegistry=registry.test", args)
                self.assertIn(f"-p:ContainerRepository={repository}", args)
                self.assertIn(f"-p:ContainerImageTag={VERSION}", args)
                self.assertTrue(any(arg.startswith("-p:ContainerArchiveOutputPath=") for arg in args))

            self.assertEqual([], skopeo_calls(root), "build-only publication must remain credential-free")

    def test_push_publishes_with_scoped_authfile_and_no_secret_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])
            self.assertTrue(all(image["disposition"] == "pushed" for image in summary["images"]))
            self.assertEqual([SERVER_IMAGE, MCP_IMAGE], copy_references(root))

            copies = [entry for entry in skopeo_calls(root) if entry["args"][0] == "copy"]
            for entry in copies:
                self.assertTrue(any(arg.startswith("docker-archive:") for arg in entry["args"]))
                self.assertTrue(any(arg.startswith("docker://") for arg in entry["args"]))
                self.assertIn("--authfile", entry["args"])

            assert_authenticated_calls_use_scoped_authfile(self, root)
            authenticated = [entry for entry in skopeo_calls(root) if "--authfile" in entry["args"]]
            for entry in authenticated:
                self.assertFalse(Path(str(entry["authfile_path"])).exists(), "authfile must be deleted after publication")

            serialized_calls = json.dumps([entry["args"] for entry in skopeo_calls(root)])
            self.assertNotIn("--dest-creds", serialized_calls)
            self.assertNotIn(env["HEXALITH_ZOT_USERNAME"], serialized_calls)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized_calls)

    def test_push_denial_is_reported_as_authorization_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            denial = {
                "exitCode": 1,
                "stderr": "unauthorized: authentication required SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
            }
            plan = {"server": [denial], "mcp": [denial]}
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertTrue(all(image["disposition"] == "authorization-failed" for image in summary["images"]))
            self.assertTrue(all("grant push access" in image["error"] for image in summary["images"]))
            serialized = result.stdout + result.stderr + json.dumps(summary)
            self.assertNotIn(env["HEXALITH_ZOT_USERNAME"], serialized)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized)

    def test_invalid_credentials_stderr_is_classified_as_authorization_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {"server": [{"exitCode": 1, "stderr": "copying image: invalid username/password"}]}
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("authorization-failed", server["disposition"])
            self.assertIn("grant push access", server["error"])

    def test_forbidden_stderr_is_classified_as_authorization_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {"server": [{"exitCode": 1, "stderr": "writing manifest: forbidden"}]}
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("authorization-failed", server["disposition"])
            self.assertIn("grant push access", server["error"])

    def test_archive_reference_mismatch_fails_closed_before_copy(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "archiveReferences": {
                    "server": {"stdout": json.dumps({"RepoTags": [f"wrong-repo:{VERSION}"]})}
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("archive-reference-invalid", server["disposition"])
            self.assertIn("wrong-repo", server["error"])
            self.assertEqual([MCP_IMAGE], copy_references(root))

    def test_archive_without_embedded_references_still_publishes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "archiveReferences": {
                    "server": {"stdout": json.dumps({"RepoTags": []})}
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])
            self.assertEqual([SERVER_IMAGE, MCP_IMAGE], copy_references(root))

    def test_push_without_standard_credentials_fails_at_publish_boundary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            env.pop("HEXALITH_ZOT_USERNAME")
            env.pop("HEXALITH_ZOT_API_KEY")
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertEqual(2, len(summary["notAttempted"]))
            self.assertTrue(all(image["disposition"] == "authentication-failed" for image in summary["images"]))
            self.assertIn("HEXALITH_ZOT_USERNAME", result.stdout + result.stderr)
            self.assertEqual([], skopeo_calls(root))

    def test_legacy_credentials_do_not_satisfy_push_contract(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            env.pop("HEXALITH_ZOT_USERNAME")
            env.pop("HEXALITH_ZOT_API_KEY")
            env["CONTAINER_REGISTRY_USERNAME"] = "LEGACY_USERNAME_SHOULD_NOT_BE_USED"
            env["CONTAINER_REGISTRY_PASSWORD"] = "LEGACY_PASSWORD_SHOULD_NOT_BE_USED"
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertTrue(all(image["disposition"] == "authentication-failed" for image in summary["images"]))
            self.assertEqual([], skopeo_calls(root))
            serialized = result.stdout + result.stderr + json.dumps(summary)
            self.assertNotIn(env["CONTAINER_REGISTRY_USERNAME"], serialized)
            self.assertNotIn(env["CONTAINER_REGISTRY_PASSWORD"], serialized)

    def test_redaction_handles_overlapping_zot_credentials(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            username = "OVERLAPPING_ZOT_SECRET"
            api_key = f"{username}_API_KEY_REMAINDER_SHOULD_NOT_LEAK"
            denial = {
                "exitCode": 1,
                "stderr": f"unauthorized credentials {username} {api_key}",
            }
            _, env = prepare_environment(root, {"server": [denial], "mcp": [denial]})
            env["HEXALITH_ZOT_USERNAME"] = username
            env["HEXALITH_ZOT_API_KEY"] = api_key
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            serialized = result.stdout + result.stderr + json.dumps(summary)
            self.assertNotIn(username, serialized)
            self.assertNotIn(api_key, serialized)
            self.assertNotIn("API_KEY_REMAINDER_SHOULD_NOT_LEAK", serialized)

    def test_push_without_skopeo_fails_before_any_push_work(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(
                output,
                env,
                push=True,
                extra_arguments=["-SkopeoCommand", "skopeo-missing-for-tests"],
            )

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertEqual(2, len(summary["notAttempted"]))
            self.assertTrue(all(image["disposition"] == "tooling-missing" for image in summary["images"]))
            self.assertIn("skopeo-missing-for-tests", result.stdout + result.stderr)
            self.assertEqual([], skopeo_calls(root))

    def test_missing_archive_fails_member_before_any_skopeo_call(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            output.mkdir(parents=True, exist_ok=True)
            (output / "mcp.tar.gz").write_bytes(b"mcp-archive")

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("archive-missing", server["disposition"])
            server_calls = [
                entry for entry in skopeo_calls(root)
                if any(arg.endswith("server.tar.gz") or SERVER_IMAGE in arg for arg in entry["args"])
            ]
            self.assertEqual([], server_calls)
            self.assertEqual([MCP_IMAGE], copy_references(root))

    def test_archive_inspect_failure_fails_member_closed_and_is_redacted(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "archiveInspect": {
                    "server": {
                        "exitCode": 3,
                        "stderr": "invalid archive SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
                    }
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("archive-inspect-failed", server["disposition"])
            self.assertEqual(3, server["exitCode"])
            serialized = json.dumps(summary)
            self.assertNotIn(env["HEXALITH_ZOT_USERNAME"], serialized)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized)
            self.assertEqual([MCP_IMAGE], copy_references(root))

    def test_remote_inspect_error_still_attempts_push(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "remoteInspect": {
                    "server": {"exitCode": 2, "stderr": "connection reset by registry"},
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("pushed", server["disposition"])
            self.assertEqual([SERVER_IMAGE, MCP_IMAGE], copy_references(root))

    def test_remote_manifest_without_config_digest_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "remoteInspect": {
                    "server": {
                        "exitCode": 0,
                        "stdout": json.dumps({"schemaVersion": 2, "manifests": [{"digest": "sha256:list-entry"}]}),
                    }
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("remote-inspect-failed", server["disposition"])
            self.assertEqual([MCP_IMAGE], copy_references(root))

    def test_partial_publish_writes_redacted_summary_and_returns_nonzero(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "server": [{"exitCode": 0, "stdout": "server published"}],
                "mcp": [
                    {
                        "exitCode": 42,
                        "stderr": "registry rejected SECRET_GITHUB_TOKEN_SHOULD_NOT_LEAK SECRET_NUGET_TOKEN_SHOULD_NOT_LEAK SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
                    }
                ],
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])
            self.assertEqual([SERVER_IMAGE], summary["pushed"])
            self.assertEqual(42, summary["failed"][0]["exitCode"])
            serialized = json.dumps(summary)
            self.assertNotIn("SECRET_GITHUB_TOKEN_SHOULD_NOT_LEAK", serialized)
            self.assertNotIn("SECRET_NUGET_TOKEN_SHOULD_NOT_LEAK", serialized)
            self.assertNotIn("SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK", serialized)
            self.assertNotIn("SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK", serialized)
            self.assertIn("PARTIAL CONTAINER PUBLISH", result.stdout + result.stderr)

    def test_rerun_skips_matching_remote_digest_and_retries_only_missing_member(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "server": [{"exitCode": 0}],
                "mcp": [{"exitCode": 42, "stderr": "transient"}, {"exitCode": 0}],
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            first = run_publish(output, env, push=True)
            second = run_publish(output, env, push=True)

            self.assertNotEqual(0, first.returncode)
            self.assertEqual(0, second.returncode, second.stdout + second.stderr)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])
            dispositions = {image["name"]: image["disposition"] for image in summary["images"]}
            self.assertEqual("already-present", dispositions["server"])
            self.assertEqual("pushed", dispositions["mcp"])
            self.assertEqual([SERVER_IMAGE, MCP_IMAGE, MCP_IMAGE], copy_references(root))
            assert_authenticated_calls_use_scoped_authfile(self, root)

    def test_existing_immutable_tag_with_conflicting_digest_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            (root / "skopeo-state.json").write_text(
                json.dumps(
                    {
                        "remote": {SERVER_IMAGE: "sha256:conflicting-config"},
                        "attempts": {},
                    }
                ),
                encoding="utf-8",
            )
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("digest-conflict", server["disposition"])
            self.assertEqual([MCP_IMAGE], copy_references(root))
            assert_authenticated_calls_use_scoped_authfile(self, root)

    def test_render_failure_happens_before_push_and_writes_current_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            prepare_archives(output)
            (output / "publish-summary.json").write_text('{"status":"stale"}', encoding="utf-8")

            result = run_publish(output, env, push=True, kubectl_exit=42)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertEqual(2, len(summary["notAttempted"]))
            self.assertEqual([], copy_references(root))

    def test_successful_kubectl_warning_is_not_written_to_deployment(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"

            result = run_publish(output, env, push=False, kubectl_warning="synthetic warning")

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            deployment = (output / "production-deployment.yaml").read_text(encoding="utf-8-sig")
            self.assertNotIn("synthetic warning", deployment)

    def test_total_push_failure_uses_non_partial_annotation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "server": [{"exitCode": 11, "stderr": "server failed"}],
                "mcp": [{"exitCode": 12, "stderr": "mcp failed"}],
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            combined = result.stdout + result.stderr
            self.assertIn("CONTAINER PUBLISH FAILED", combined)
            self.assertNotIn("PARTIAL CONTAINER PUBLISH", combined)


if __name__ == "__main__":
    unittest.main()
