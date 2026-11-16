"""Real Git and real kubectl proof that recovery re-renders the release runner's checkout bytes.

A release runner checks the tag out through `.gitattributes`, so `redis-stack.conf` reaches
kustomize as CRLF and kustomize hashes those bytes into the ConfigMap name. These fixtures
build that shape from a real tagged repository and a real clone, and render with the real
renderer, `pwsh` and `kubectl`. They grant no live release, registry or recovery credit.
"""

from contextlib import contextmanager
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import unittest
from unittest import mock

import publication_contract_test as fixtures

C = fixtures.CONTRACT
A = fixtures.ARCHIVE
RENDERER = "tools/render-production-deployment.ps1"
PRODUCTION_KUSTOMIZATION = "deploy/kubernetes/overlays/production/kustomization.yaml"
REDIS_CONFIG = "deploy/kubernetes/base/config/redis-stack.conf"
IDENTITY = {"version": fixtures.VERSION, "registry": "registry.test"}
# Fixture repositories must not depend on the developer's Git configuration (signing, eol, hooks).
GIT_ENVIRONMENT = {
    "GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull,
    "GIT_AUTHOR_NAME": "fixture", "GIT_AUTHOR_EMAIL": "fixture@example.invalid",
    "GIT_COMMITTER_NAME": "fixture", "GIT_COMMITTER_EMAIL": "fixture@example.invalid",
}


def git(directory, *arguments):
    environment = dict(os.environ, **GIT_ENVIRONMENT)
    return subprocess.run(["git", "-C", str(directory), *arguments], check=True, capture_output=True, env=environment).stdout


def render(checkout, destination):
    """Render exactly like the release job: the real renderer, from a working tree."""
    with fixtures.working_directory(checkout):
        A.render_deployment(IDENTITY, destination)
    return destination.read_bytes()


def config_map_names(deployment):
    return set(re.findall(rb"redis-stack-config-[a-z0-9]+", deployment))


class TaggedDeploymentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        missing = [tool for tool in ("git", "pwsh", "kubectl") if shutil.which(tool) is None]
        if missing:
            raise RuntimeError(f"These fixtures need the real {', '.join(missing)}; a fake renderer would hide line-ending drift.")
        temporary = tempfile.TemporaryDirectory(prefix="memories-tagged-deployment-test-")
        cls.addClassCleanup(temporary.cleanup)
        root = Path(temporary.name)
        cls.repository = root / "repository"
        cls.repository.mkdir()
        git(cls.repository, "init", "-q", "-b", "main")
        shutil.copyfile(fixtures.ROOT / ".gitattributes", cls.repository / ".gitattributes")
        (cls.repository / "tools").mkdir()
        shutil.copyfile(fixtures.ROOT / RENDERER, cls.repository / RENDERER)
        shutil.copytree(fixtures.ROOT / "deploy/kubernetes", cls.repository / "deploy/kubernetes")
        git(cls.repository, "add", "--", ".gitattributes", RENDERER, "deploy/kubernetes")
        git(cls.repository, "commit", "-q", "-m", "fixture: tagged release inputs")
        git(cls.repository, "tag", "v" + fixtures.VERSION)
        cls.tag_sha = git(cls.repository, "rev-parse", "HEAD").decode().strip()
        # A later commit holds a symlink input; a shallow clone of it lacks the tag commit.
        git(cls.repository, "switch", "-q", "-c", "later")
        os.symlink("kustomization.yaml", cls.repository / "deploy/kubernetes/base/linked-kustomization.yaml")
        git(cls.repository, "add", "--", "deploy/kubernetes")
        git(cls.repository, "commit", "-q", "-m", "fixture: symlink input")
        cls.symlink_sha = git(cls.repository, "rev-parse", "HEAD").decode().strip()
        # Commits that each drop one required renderer input.
        cls.missing_input_shas = {}
        for branch, path in (("no-renderer", RENDERER), ("no-production-kustomization", PRODUCTION_KUSTOMIZATION)):
            git(cls.repository, "switch", "-q", "-c", branch, "main")
            git(cls.repository, "rm", "-q", "--", path)
            git(cls.repository, "commit", "-q", "-m", f"fixture: drop {path}")
            cls.missing_input_shas[branch] = git(cls.repository, "rev-parse", "HEAD").decode().strip()
        git(cls.repository, "switch", "-q", "main")
        # The runner's checkout: a real clone materialized through the tree's own attributes.
        cls.clone = root / "clone"
        git(root, "clone", "-q", str(cls.repository), str(cls.clone))
        cls.shallow = root / "shallow"
        git(root, "clone", "-q", "--depth", "1", "--branch", "later", "--no-tags", cls.repository.as_uri(), str(cls.shallow))
        # The baseline's materialization: raw LF blob bytes for every tagged input.
        cls.blob_tree = root / "blob-tree"
        for name in git(cls.repository, "ls-tree", "-r", "--name-only", cls.tag_sha, "--", RENDERER, "deploy/kubernetes").decode().splitlines():
            target = cls.blob_tree / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(git(cls.repository, "cat-file", "blob", f"{cls.tag_sha}:{name}"))
        cls.crlf_deployment = root / "crlf/hexalith-memories-production.yaml"
        cls.crlf_bytes = render(cls.clone, cls.crlf_deployment)
        cls.lf_deployment = root / "lf/hexalith-memories-production.yaml"
        cls.lf_bytes = render(cls.blob_tree, cls.lf_deployment)

    @contextmanager
    def recorded_commands(self):
        original = subprocess.run
        commands = []
        def record(command, *arguments, **options):
            commands.append([str(value) for value in command])
            return original(command, *arguments, **options)
        with mock.patch.object(C.subprocess, "run", side_effect=record):
            yield commands

    def assert_not_rendered(self, commands):
        self.assertFalse([command for command in commands if Path(command[0]).name in ("pwsh", "kubectl")], commands)

    def test_positive_controls_clone_is_crlf_blob_is_lf_and_renders_differ(self):
        self.assertIn(b"\r\n", (self.clone / REDIS_CONFIG).read_bytes())
        blob = git(self.repository, "cat-file", "blob", f"{self.tag_sha}:{REDIS_CONFIG}")
        self.assertTrue(blob)
        self.assertNotIn(b"\r", blob)
        self.assertEqual(blob, (self.blob_tree / REDIS_CONFIG).read_bytes())
        self.assertNotEqual(self.crlf_bytes, self.lf_bytes)
        crlf_names, lf_names = config_map_names(self.crlf_bytes), config_map_names(self.lf_bytes)
        self.assertEqual(1, len(crlf_names))
        self.assertEqual(1, len(lf_names))
        self.assertNotEqual(crlf_names, lf_names)

    def test_runner_checkout_render_is_verified(self):
        C.verify_tagged_deployment(self.clone, self.tag_sha, IDENTITY, self.crlf_deployment)

    def test_lf_blob_render_is_refused(self):
        with self.assertRaisesRegex(ValueError, "Retained deployment differs"):
            C.verify_tagged_deployment(self.clone, self.tag_sha, IDENTITY, self.lf_deployment)

    def test_shallow_source_without_tag_commit_is_refused_before_rendering(self):
        with self.assertRaises(subprocess.CalledProcessError):
            git(self.shallow, "cat-file", "-e", self.tag_sha + "^{commit}")
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, re.escape(self.tag_sha) + ".*full tagged history"):
            C.verify_tagged_deployment(self.shallow, self.tag_sha, IDENTITY, self.crlf_deployment)
        self.assert_not_rendered(commands)

    def test_directory_inside_another_repository_is_refused_before_rendering(self):
        # The `submodules: false` shape: Git would silently fall through to the enclosing clone.
        nested = self.clone / "references/Hexalith.Memories"
        nested.mkdir(parents=True, exist_ok=True)
        self.assertEqual(self.tag_sha, git(nested, "rev-parse", "HEAD").decode().strip())
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, "not its own Git checkout"):
            C.verify_tagged_deployment(nested, self.tag_sha, IDENTITY, self.crlf_deployment)
        self.assert_not_rendered(commands)

    def test_symlink_input_is_refused_before_rendering(self):
        self.assertIn(b"120000", git(self.repository, "ls-tree", self.symlink_sha, "--", "deploy/kubernetes/base/linked-kustomization.yaml"))
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, "not a regular file"):
            C.verify_tagged_deployment(self.repository, self.symlink_sha, IDENTITY, self.crlf_deployment)
        self.assert_not_rendered(commands)

    def test_missing_renderer_is_refused_before_rendering(self):
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, "Tagged production renderer input is missing: .*pathspec"):
            C.verify_tagged_deployment(self.repository, self.missing_input_shas["no-renderer"], IDENTITY, self.crlf_deployment)
        self.assert_not_rendered(commands)

    def test_missing_production_kustomization_is_refused_before_rendering(self):
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, r"^Tagged production renderer input is missing\.$"):
            C.verify_tagged_deployment(self.repository, self.missing_input_shas["no-production-kustomization"], IDENTITY, self.crlf_deployment)
        self.assert_not_rendered(commands)

    def test_unreadable_source_reports_git_error_not_enclosing_repository(self):
        absent = self.repository.parent / "absent-source"
        with self.assertRaisesRegex(ValueError, "Git cannot read recovery source .*absent-source: .+") as refused:
            C.verify_tagged_deployment(absent, self.tag_sha, IDENTITY, self.crlf_deployment)
        self.assertNotIn("not its own Git checkout", str(refused.exception))

    def test_bundle_from_directory_inside_another_repository_is_refused_before_any_bundle_read(self):
        nested = self.clone / "references/Hexalith.Memories"
        nested.mkdir(parents=True, exist_ok=True)
        absent_bundle = self.repository.parent / "absent-release-recovery-bundle.tar.gz"
        with self.recorded_commands() as commands, self.assertRaisesRegex(ValueError, "not its own Git checkout"):
            C.verify_bundle(absent_bundle, self.repository.parent / "bundle-destination", nested, fixtures.VERSION, "304", "Hexalith/Hexalith.Memories")
        self.assertEqual([["git", "-C", str(nested), "rev-parse", "--show-toplevel"]], commands)

    def test_bundle_from_tagless_shallow_source_names_tag_and_history_requirement(self):
        absent_bundle = self.repository.parent / "absent-release-recovery-bundle.tar.gz"
        with self.assertRaisesRegex(ValueError, re.escape(f"Recovery tag v{fixtures.VERSION} cannot be resolved") + ".*full tagged history.*fetch-depth: 0"):
            C.verify_bundle(absent_bundle, self.repository.parent / "bundle-destination", self.shallow, fixtures.VERSION, "304", "Hexalith/Hexalith.Memories")

    def test_release_renderer_inputs_are_regular_files_so_recovery_can_archive_them(self):
        # Recovery refuses non-regular members; a symlink here would publish and then be unrecoverable.
        listing = subprocess.run(["git", "-C", str(fixtures.ROOT), "ls-files", "-s", "--", RENDERER, "deploy/kubernetes"], check=True, capture_output=True, text=True).stdout.splitlines()
        self.assertIn(RENDERER, [line.split("\t", 1)[1] for line in listing])
        self.assertIn(PRODUCTION_KUSTOMIZATION, [line.split("\t", 1)[1] for line in listing])
        self.assertLessEqual({line.split(" ", 1)[0] for line in listing}, {"100644", "100755"}, listing)


if __name__ == "__main__":
    unittest.main()
