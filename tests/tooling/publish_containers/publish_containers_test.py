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


def write_fake_docker(directory: Path) -> None:
    script = directory / "fake_docker.py"
    script.write_text(
        textwrap.dedent(
            f"""
            import json
            import os
            import sys
            from pathlib import Path

            args = sys.argv[1:]
            state_path = Path(os.environ["FAKE_DOCKER_STATE"])
            plan_path = Path(os.environ["FAKE_DOCKER_PLAN"])
            state = json.loads(state_path.read_text(encoding="utf-8")) if state_path.exists() else {{"remote": {{}}, "attempts": {{}}, "local": {{}}}}
            plan = json.loads(plan_path.read_text(encoding="utf-8"))
            digests = {{
                "server": "sha256:server-config",
                "mcp": "sha256:mcp-config",
            }}

            with open(os.environ["FAKE_COMMAND_LOG"], "a", encoding="utf-8") as log:
                log.write(json.dumps({{"command": "docker", "args": args}}) + "\\n")

            if args and args[0] == "login":
                password = sys.stdin.read().strip()
                expected_registry = os.environ["EXPECTED_ZOT_REGISTRY"]
                expected_username = os.environ["HEXALITH_ZOT_USERNAME"]
                expected_password = os.environ["HEXALITH_ZOT_API_KEY"]
                if args != ["login", expected_registry, "--username", expected_username, "--password-stdin"]:
                    print("unexpected login arguments", file=sys.stderr)
                    sys.exit(93)
                if password != expected_password:
                    print("unexpected login password", file=sys.stderr)
                    sys.exit(92)
                outcome = plan.get("login", {{}})
                if outcome.get("stdout"):
                    print(outcome["stdout"])
                if outcome.get("stderr"):
                    print(outcome["stderr"], file=sys.stderr)
                if int(outcome.get("exitCode", 0)) != 0:
                    sys.exit(int(outcome["exitCode"]))
                print("Login Succeeded")
            elif args[:2] == ["load", "--input"]:
                image = "server" if Path(args[2]).name.startswith("server") else "mcp"
                default_reference = "memories:{VERSION}" if image == "server" else "memories-mcp:{VERSION}"
                outcome = plan.get("load", {{}}).get(image, {{}})
                reference = outcome.get("reference", default_reference)
                if reference:
                    state["local"][reference] = digests[image]
                if "stdout" in outcome:
                    print(outcome["stdout"])
                elif reference:
                    print(f"Loaded image: {{reference}}")
                if outcome.get("stderr"):
                    print(outcome["stderr"], file=sys.stderr)
                if int(outcome.get("exitCode", 0)) != 0:
                    state_path.write_text(json.dumps(state), encoding="utf-8")
                    sys.exit(int(outcome["exitCode"]))
            elif args and args[0] == "tag":
                source = args[1]
                target = args[2]
                image = "server" if target == "{SERVER_IMAGE}" else "mcp"
                outcome = plan.get("tag", {{}}).get(image, {{}})
                if outcome.get("stdout"):
                    print(outcome["stdout"])
                if outcome.get("stderr"):
                    print(outcome["stderr"], file=sys.stderr)
                if int(outcome.get("exitCode", 0)) != 0:
                    state_path.write_text(json.dumps(state), encoding="utf-8")
                    sys.exit(int(outcome["exitCode"]))
                if source not in state["local"]:
                    print("unknown tag source", file=sys.stderr)
                    sys.exit(4)
                state["local"][target] = state["local"][source]
            elif args[:2] == ["image", "inspect"]:
                reference = args[2]
                if reference not in state["local"]:
                    print("unknown local image", file=sys.stderr)
                    sys.exit(4)
                print(state["local"][reference])
            elif args[:2] == ["manifest", "inspect"]:
                if args[2] != "--verbose":
                    print("manifest inspection must request the descriptor digest", file=sys.stderr)
                    sys.exit(95)
                reference = args[3]
                digest = state["remote"].get(reference)
                if digest is None:
                    print("no such manifest", file=sys.stderr)
                    sys.exit(1)
                print(json.dumps({{
                    "Descriptor": {{"digest": "sha256:intentionally-different-manifest"}},
                    "SchemaV2Manifest": {{"config": {{"digest": digest}}}},
                }}))
            elif args and args[0] == "push":
                reference = args[1]
                image = "server" if reference == "{SERVER_IMAGE}" else "mcp"
                attempt = int(state["attempts"].get(image, 0))
                state["attempts"][image] = attempt + 1
                outcomes = plan.get(image, [{{"exitCode": 0}}])
                outcome = outcomes[min(attempt, len(outcomes) - 1)]
                if outcome.get("stdout"):
                    print(outcome["stdout"])
                if outcome.get("stderr"):
                    print(outcome["stderr"], file=sys.stderr)
                if int(outcome.get("exitCode", 0)) == 0:
                    state["remote"][reference] = state["local"][reference]
                state_path.write_text(json.dumps(state), encoding="utf-8")
                sys.exit(int(outcome.get("exitCode", 0)))
            else:
                print(f"unexpected docker arguments: {{args}}", file=sys.stderr)
                sys.exit(98)

            state_path.write_text(json.dumps(state), encoding="utf-8")
            sys.exit(0)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "docker",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )
    (directory / "docker.cmd").write_text(
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )


def prepare_environment(root: Path, plan: dict[str, object] | None = None) -> tuple[Path, dict[str, str]]:
    fake_bin = root / "bin"
    fake_bin.mkdir()
    write_fake_dotnet(fake_bin)
    write_fake_kubectl(fake_bin)
    write_fake_docker(fake_bin)

    plan_path = root / "docker-plan.json"
    plan_path.write_text(json.dumps(plan or {}), encoding="utf-8")
    env = os.environ.copy()
    env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_DOCKER_PLAN"] = str(plan_path)
    env["FAKE_DOCKER_STATE"] = str(root / "docker-state.json")
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


def run_publish(output: Path, env: dict[str, str], *, push: bool, kubectl_exit: int = 0, kubectl_warning: str = "") -> subprocess.CompletedProcess[str]:
    invocation = [
        "pwsh",
        "-NoLogo",
        "-NoProfile",
        "-File",
        str(SCRIPT),
        "-Version",
        VERSION,
        "-Registry",
        "registry.test",
        "-OutputDirectory",
        str(output),
    ]
    if push:
        invocation.append("-Push")
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


class PublishContainersTests(unittest.TestCase):
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

            logins = [
                entry for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][0] == "login"
            ]
            self.assertEqual([], logins, "build-only publication must remain credential-free")

    def test_push_authenticates_with_password_stdin_before_registry_operations(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            docker_calls = [entry["args"] for entry in command_log(root) if entry["command"] == "docker"]
            self.assertEqual(
                ["login", "registry.test", "--username", env["HEXALITH_ZOT_USERNAME"], "--password-stdin"],
                docker_calls[0],
            )
            serialized_calls = json.dumps(docker_calls)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized_calls)

    def test_login_failure_is_redacted_and_stops_before_image_operations(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "login": {
                    "exitCode": 41,
                    "stderr": "expired SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertTrue(all(image["disposition"] == "authentication-failed" for image in summary["images"]))
            docker_calls = [entry["args"] for entry in command_log(root) if entry["command"] == "docker"]
            self.assertEqual(
                [["login", "registry.test", "--username", env["HEXALITH_ZOT_USERNAME"], "--password-stdin"]],
                docker_calls,
            )
            serialized = result.stdout + result.stderr + json.dumps(summary)
            self.assertNotIn(env["HEXALITH_ZOT_USERNAME"], serialized)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized)

    def test_login_success_with_push_denial_is_reported_as_authorization_failure(self) -> None:
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

    def test_push_retags_registry_less_loaded_images_before_canonical_inspect(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            docker_calls = [entry["args"] for entry in command_log(root) if entry["command"] == "docker"]
            self.assertLess(
                docker_calls.index(["tag", f"memories:{VERSION}", SERVER_IMAGE]),
                docker_calls.index(["image", "inspect", SERVER_IMAGE, "--format={{.Id}}"]),
            )
            self.assertLess(
                docker_calls.index(["tag", f"memories-mcp:{VERSION}", MCP_IMAGE]),
                docker_calls.index(["image", "inspect", MCP_IMAGE, "--format={{.Id}}"]),
            )

    def test_push_skips_tag_when_archive_load_is_already_canonical(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "load": {
                    "server": {"reference": SERVER_IMAGE},
                    "mcp": {"reference": MCP_IMAGE},
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            tags = [
                entry for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][0] == "tag"
            ]
            self.assertEqual([], tags)

    def test_push_accepts_single_loaded_image_id_and_retags_it(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            image_id = "sha256:" + ("a" * 64)
            plan = {
                "load": {
                    "server": {
                        "reference": image_id,
                        "stdout": f"Loaded image ID: {image_id}",
                    }
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            docker_calls = [entry["args"] for entry in command_log(root) if entry["command"] == "docker"]
            self.assertIn(["tag", image_id, SERVER_IMAGE], docker_calls)

    def test_push_accepts_single_loaded_reference_reported_on_stderr(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "load": {
                    "server": {
                        "stdout": "",
                        "stderr": f"Loaded image: memories:{VERSION}",
                    }
                }
            }
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            docker_calls = [entry["args"] for entry in command_log(root) if entry["command"] == "docker"]
            self.assertIn(["tag", f"memories:{VERSION}", SERVER_IMAGE], docker_calls)

    def test_multiple_loaded_references_fail_closed_before_tag_or_inspect(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "load": {
                    "server": {
                        "stdout": (
                            f"Loaded image: memories:{VERSION}\n"
                            f"Loaded image: unrelated:{VERSION}"
                        ),
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
            self.assertEqual("load-reference-invalid", server["disposition"])
            server_operations = [
                entry["args"] for entry in command_log(root)
                if entry["command"] == "docker" and SERVER_IMAGE in entry["args"]
            ]
            self.assertEqual([], server_operations)

    def test_unrelated_loaded_reference_fails_closed_before_tag_or_inspect(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {"load": {"server": {"reference": f"unrelated:{VERSION}"}}}
            _, env = prepare_environment(root, plan)
            output = root / "artifacts"
            prepare_archives(output)

            result = run_publish(output, env, push=True)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            server = next(image for image in summary["images"] if image["name"] == "server")
            self.assertEqual("load-reference-invalid", server["disposition"])
            server_operations = [
                entry["args"] for entry in command_log(root)
                if entry["command"] == "docker" and SERVER_IMAGE in entry["args"]
            ]
            self.assertEqual([], server_operations)

    def test_unparseable_load_output_fails_member_before_inspect_or_remote_operation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "load": {
                    "server": {
                        "stdout": "archive imported SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
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
            self.assertEqual("load-reference-invalid", server["disposition"])
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], json.dumps(summary))
            server_operations = [
                entry["args"] for entry in command_log(root)
                if entry["command"] == "docker" and SERVER_IMAGE in entry["args"]
            ]
            self.assertEqual([], server_operations)

    def test_tag_failure_is_redacted_and_stops_member_before_canonical_inspect(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plan = {
                "tag": {
                    "server": {
                        "exitCode": 44,
                        "stderr": "tag rejected SECRET_ZOT_USERNAME_SHOULD_NOT_LEAK SECRET_ZOT_API_KEY_SHOULD_NOT_LEAK",
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
            self.assertEqual("tag-failed", server["disposition"])
            self.assertEqual(44, server["exitCode"])
            serialized = json.dumps(summary)
            self.assertNotIn(env["HEXALITH_ZOT_USERNAME"], serialized)
            self.assertNotIn(env["HEXALITH_ZOT_API_KEY"], serialized)
            canonical_inspects = [
                entry for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][:3] == ["image", "inspect", SERVER_IMAGE]
            ]
            self.assertEqual([], canonical_inspects)

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
            docker_calls = [entry for entry in command_log(root) if entry["command"] == "docker"]
            self.assertEqual([], docker_calls)

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
            pushes = [
                entry["args"][1]
                for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][0] == "push"
            ]
            self.assertEqual([SERVER_IMAGE, MCP_IMAGE, MCP_IMAGE], pushes)

    def test_existing_immutable_tag_with_conflicting_digest_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            _, env = prepare_environment(root)
            (root / "docker-state.json").write_text(
                json.dumps(
                    {
                        "remote": {SERVER_IMAGE: "sha256:conflicting-config"},
                        "attempts": {},
                        "local": {},
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
            pushes = [
                entry["args"][1]
                for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][0] == "push"
            ]
            self.assertEqual([MCP_IMAGE], pushes)

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
            pushes = [
                entry for entry in command_log(root)
                if entry["command"] == "docker" and entry["args"][0] == "push"
            ]
            self.assertEqual([], pushes)

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
