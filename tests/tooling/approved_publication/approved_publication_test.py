import importlib.util
import io
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stderr
from dataclasses import dataclass
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-approved-publication.py"
SPEC_PATH = "_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md"
PROPOSAL_PATH = (
    "_bmad-output/planning-artifacts/"
    "sprint-change-proposal-2026-08-01-eventstore-source-and-3-89-package-identities.md"
)


def run(*args: str, cwd: Path | None = None, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        args,
        cwd=cwd,
        text=True,
        capture_output=True,
        check=check,
    )


def configure_identity(repository: Path) -> None:
    run("git", "config", "user.name", "Approved Publication Tests", cwd=repository)
    run("git", "config", "user.email", "publication-tests@example.invalid", cwd=repository)


def load_tool_module():
    spec = importlib.util.spec_from_file_location("check_approved_publication", TOOL)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


@dataclass(frozen=True)
class GitlinkFixture:
    path: str
    baseline_object_id: str
    approved_object_id: str
    remote: Path


class PublicationFixture:
    def __init__(
        self,
        test_case: unittest.TestCase,
        *,
        omitted_registration: str | None = None,
        include_extra_gitlink: bool = False,
    ) -> None:
        self._temporary_directory = tempfile.TemporaryDirectory(prefix="approved-publication-")
        test_case.addCleanup(self._temporary_directory.cleanup)
        self.base = Path(self._temporary_directory.name)
        self.root_remote = self.base / "root-remote.git"
        self.root = self.base / "root"

        run("git", "init", "--bare", "--initial-branch=main", str(self.root_remote))
        run("git", "clone", str(self.root_remote), str(self.root))
        configure_identity(self.root)
        run("git", "switch", "-c", "main", cwd=self.root)

        primary_gitlinks = (
            self._create_gitlink("Hexalith.Builds"),
            self._create_gitlink("Hexalith.FrontComposer"),
        )
        extra_gitlinks = (self._create_gitlink("Hexalith.Tenants"),) if include_extra_gitlink else ()
        self.gitlinks = primary_gitlinks
        self.all_gitlinks = primary_gitlinks + extra_gitlinks
        self._write_gitmodules(omitted_registration)
        for gitlink in self.all_gitlinks:
            self.stage_gitlink(gitlink.path, gitlink.baseline_object_id)
        run("git", "add", ".gitmodules", cwd=self.root)
        run("git", "commit", "-m", "build(deps): seed publication baseline", cwd=self.root)
        run("git", "push", "-u", "origin", "main", cwd=self.root)
        self.root_baseline_object_id = self.resolve(self.root, "HEAD")
        self.stage_approved_publication()

    def _create_gitlink(self, name: str) -> GitlinkFixture:
        remote = self.base / f"{name}-remote.git"
        author = self.base / f"{name}-author"
        relative_path = f"references/{name}"
        checkout = self.root / relative_path

        run("git", "init", "--bare", "--initial-branch=main", str(remote))
        run("git", "clone", str(remote), str(author))
        configure_identity(author)
        run("git", "switch", "-c", "main", cwd=author)
        run("git", "commit", "--allow-empty", "-m", f"build: seed {name}", cwd=author)
        run("git", "push", "-u", "origin", "main", cwd=author)
        baseline_object_id = self.resolve(author, "HEAD")

        run("git", "clone", str(remote), str(checkout))
        configure_identity(checkout)

        run("git", "commit", "--allow-empty", "-m", f"build: approve {name}", cwd=author)
        run("git", "push", "origin", "main", cwd=author)
        approved_object_id = self.resolve(author, "HEAD")
        run("git", "fetch", "origin", "main", cwd=checkout)
        run("git", "switch", "--detach", approved_object_id, cwd=checkout)
        return GitlinkFixture(relative_path, baseline_object_id, approved_object_id, remote)

    def _write_gitmodules(self, omitted_registration: str | None) -> None:
        sections: list[str] = []
        for gitlink in self.all_gitlinks:
            if gitlink.path == omitted_registration:
                continue
            name = gitlink.path.removeprefix("references/")
            sections.extend(
                (
                    f'[submodule "{name}"]',
                    f"\tpath = {gitlink.path}",
                    f"\turl = {gitlink.remote}",
                ),
            )
        (self.root / ".gitmodules").write_text("\n".join(sections) + "\n", encoding="utf-8")

    @staticmethod
    def resolve(repository: Path, revision: str) -> str:
        return run("git", "rev-parse", revision, cwd=repository).stdout.strip()

    def stage_gitlink(self, path: str, object_id: str) -> None:
        run(
            "git",
            "update-index",
            "--add",
            "--cacheinfo",
            f"160000,{object_id},{path}",
            cwd=self.root,
        )

    def stage_approved_publication(self) -> None:
        for gitlink in self.gitlinks:
            self.stage_gitlink(gitlink.path, gitlink.approved_object_id)
        for path, content in (
            (SPEC_PATH, "approved publication specification\n"),
            (PROPOSAL_PATH, "approved correct-course proposal\n"),
        ):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(content, encoding="utf-8")
            run("git", "add", path, cwd=self.root)

    def validator_arguments(
        self,
        *,
        snapshot: str = "index",
        candidate_ref: str = "HEAD",
        expected_paths: tuple[str, ...] = (SPEC_PATH, PROPOSAL_PATH),
        expected_gitlinks: tuple[tuple[str, str], ...] | None = None,
    ) -> list[str]:
        gitlinks = expected_gitlinks or tuple(
            (gitlink.path, gitlink.approved_object_id) for gitlink in self.gitlinks
        )
        arguments = [
            "--repository",
            str(self.root),
            "--snapshot",
            snapshot,
            "--root-remote-ref",
            "origin/main",
            "--expected-root-remote-oid",
            self.root_baseline_object_id,
            "--candidate-ref",
            candidate_ref,
        ]
        for path in expected_paths:
            arguments.extend(("--expected-path", path))
        for path, object_id in gitlinks:
            arguments.extend(("--expected-gitlink", f"{path}={object_id}"))
        return arguments

    def run_validator(self, **kwargs) -> subprocess.CompletedProcess[str]:
        return run(
            sys.executable,
            str(TOOL),
            *self.validator_arguments(**kwargs),
            cwd=self.root,
            check=False,
        )

    def snapshot_observable_state(self) -> tuple[str, str, str, str, str]:
        return (
            run("git", "diff", "--cached", "--binary", cwd=self.root).stdout,
            run("git", "status", "--porcelain=v2", "--untracked-files=all", cwd=self.root).stdout,
            self.resolve(self.root, "HEAD"),
            run("git", "for-each-ref", "--format=%(refname) %(objectname)", "refs/heads", "refs/tags", cwd=self.root).stdout,
            run("git", "for-each-ref", "--format=%(refname) %(objectname)", "refs/remotes", cwd=self.root).stdout,
        )

    def commit_publication(self) -> str:
        run("git", "commit", "-m", "build(deps): publish approved snapshot", cwd=self.root)
        return self.resolve(self.root, "HEAD")

    def advance_root_remote(self) -> str:
        publisher = self.base / "root-publisher"
        run("git", "clone", str(self.root_remote), str(publisher))
        configure_identity(publisher)
        run("git", "commit", "--allow-empty", "-m", "build: advance root remote", cwd=publisher)
        run("git", "push", "origin", "main", cwd=publisher)
        run("git", "fetch", "origin", "main", cwd=self.root)
        return self.resolve(publisher, "HEAD")

    def create_non_fast_forward_candidate(self) -> str:
        tree_object_id = run("git", "write-tree", cwd=self.root).stdout.strip()
        return run(
            "git",
            "commit-tree",
            tree_object_id,
            "-m",
            "build: create divergent candidate",
            cwd=self.root,
        ).stdout.strip()

    def create_unpublished_submodule_commit(self, gitlink: GitlinkFixture) -> str:
        checkout = self.root / gitlink.path
        run("git", "commit", "--allow-empty", "-m", "build: create unpublished commit", cwd=checkout)
        return self.resolve(checkout, "HEAD")


class ApprovedPublicationTests(unittest.TestCase):
    def test_staged_exact_approved_snapshot_passes_without_mutating_git_state(self) -> None:
        fixture = PublicationFixture(self)
        before = fixture.snapshot_observable_state()

        result = fixture.run_validator()

        after = fixture.snapshot_observable_state()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Publication mode: pre-commit index", result.stdout)
        self.assertIn("Approved publication preflight passed.", result.stdout)
        self.assertEqual(after, before)

    def test_post_commit_fast_forward_snapshot_passes(self) -> None:
        fixture = PublicationFixture(self)
        candidate_object_id = fixture.commit_publication()

        result = fixture.run_validator(snapshot="HEAD", candidate_ref="HEAD")

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Publication mode: post-commit tree", result.stdout)
        self.assertIn(candidate_object_id, result.stdout)
        self.assertEqual(fixture.resolve(fixture.root, "origin/main"), fixture.root_baseline_object_id)

    def test_gitlink_oid_drift_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        builds = fixture.gitlinks[0]
        drift_object_id = fixture.create_unpublished_submodule_commit(builds)
        expected_gitlinks = (
            (builds.path, drift_object_id),
            (fixture.gitlinks[1].path, fixture.gitlinks[1].approved_object_id),
        )

        result = fixture.run_validator(expected_gitlinks=expected_gitlinks)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Gitlink OID drift", result.stdout)

    def test_omitted_expected_non_gitlink_path_fails_closed(self) -> None:
        fixture = PublicationFixture(self)

        result = fixture.run_validator(expected_paths=(SPEC_PATH,))

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unexpected changed paths", result.stdout)
        self.assertIn(PROPOSAL_PATH, result.stdout)

    def test_omitted_expected_gitlink_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        builds = fixture.gitlinks[0]

        result = fixture.run_validator(
            expected_gitlinks=((builds.path, builds.approved_object_id),),
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unexpected root gitlinks changed", result.stdout)
        self.assertIn(fixture.gitlinks[1].path, result.stdout)

    def test_extra_changed_non_gitlink_path_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        extra = fixture.root / "unexpected.txt"
        extra.write_text("unexpected\n", encoding="utf-8")
        run("git", "add", "unexpected.txt", cwd=fixture.root)

        result = fixture.run_validator()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unexpected changed paths", result.stdout)
        self.assertIn("unexpected.txt", result.stdout)

    def test_unrelated_root_gitlink_drift_fails_closed(self) -> None:
        fixture = PublicationFixture(self, include_extra_gitlink=True)
        unrelated = fixture.all_gitlinks[2]
        fixture.stage_gitlink(unrelated.path, unrelated.approved_object_id)

        result = fixture.run_validator()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unexpected root gitlinks changed", result.stdout)
        self.assertIn(unrelated.path, result.stdout)

    def test_root_remote_movement_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        advanced_object_id = fixture.advance_root_remote()

        result = fixture.run_validator()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Root remote moved", result.stdout)
        self.assertIn(advanced_object_id, result.stdout)

    def test_pre_commit_candidate_must_equal_expected_remote(self) -> None:
        fixture = PublicationFixture(self)
        fixture.commit_publication()

        result = fixture.run_validator(snapshot="index", candidate_ref="HEAD")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Pre-commit candidate baseline must equal", result.stdout)

    def test_non_fast_forward_candidate_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        divergent_object_id = fixture.create_non_fast_forward_candidate()

        result = fixture.run_validator(
            snapshot=divergent_object_id,
            candidate_ref=divergent_object_id,
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("is not a fast-forward", result.stdout)
        self.assertIn(divergent_object_id, result.stdout)

    def test_post_commit_snapshot_tree_must_match_candidate_tree(self) -> None:
        fixture = PublicationFixture(self)
        publication_object_id = fixture.commit_publication()
        extra = fixture.root / "candidate-only.txt"
        extra.write_text("different tree\n", encoding="utf-8")
        run("git", "add", "candidate-only.txt", cwd=fixture.root)
        run("git", "commit", "-m", "test: change candidate tree", cwd=fixture.root)

        result = fixture.run_validator(snapshot=publication_object_id, candidate_ref="HEAD")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("does not match candidate tree", result.stdout)

    def test_invalid_post_commit_treeish_fails_closed_without_traceback(self) -> None:
        fixture = PublicationFixture(self)
        fixture.commit_publication()

        result = fixture.run_validator(snapshot="not-a-tree-ish", candidate_ref="HEAD")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Approved publication preflight failed:", result.stdout)
        self.assertNotIn("Traceback", result.stdout + result.stderr)

    def test_malformed_tree_entry_fails_closed(self) -> None:
        module = load_tool_module()
        malformed = subprocess.CompletedProcess(
            args=["git"],
            returncode=0,
            stdout="malformed-tree-entry\0",
            stderr="",
        )

        with mock.patch.object(module, "run_git", return_value=malformed):
            with self.assertRaises(module.ValidationError) as raised:
                module.parse_tree_entries(Path("."), "HEAD")

        self.assertIn("Unexpected tree entry", str(raised.exception))

    def test_unregistered_submodule_path_fails_closed(self) -> None:
        front_composer_path = "references/Hexalith.FrontComposer"
        fixture = PublicationFixture(self, omitted_registration=front_composer_path)

        result = fixture.run_validator()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not registered by root .gitmodules", result.stdout)
        self.assertIn(front_composer_path, result.stdout)

    def test_submodule_remote_identity_mismatch_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        builds = fixture.gitlinks[0]
        different_remote = fixture.base / "different-remote.git"
        run("git", "init", "--bare", "--initial-branch=main", str(different_remote))
        run("git", "remote", "set-url", "origin", str(different_remote), cwd=fixture.root / builds.path)

        result = fixture.run_validator()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Submodule origin does not match root .gitmodules", result.stdout)

    def test_equivalent_local_and_network_remote_urls_have_stable_identity(self) -> None:
        module = load_tool_module()
        with tempfile.TemporaryDirectory(prefix="approved-remote-identity-") as root:
            repository = Path(root)
            local_remote = repository / "remote.git"
            local_remote.mkdir()

            self.assertEqual(
                module.canonical_remote_identity(str(local_remote), repository),
                module.canonical_remote_identity(local_remote.as_uri(), repository),
            )
            self.assertEqual(
                module.canonical_remote_identity(
                    "https://github.com/Hexalith/Hexalith.Builds.git",
                    repository,
                ),
                module.canonical_remote_identity(
                    "git@github.com:Hexalith/Hexalith.Builds.git",
                    repository,
                ),
            )

    def test_missing_submodule_commit_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        builds = fixture.gitlinks[0]
        missing_object_id = "f" * 40
        fixture.stage_gitlink(builds.path, missing_object_id)
        expected_gitlinks = (
            (builds.path, missing_object_id),
            (fixture.gitlinks[1].path, fixture.gitlinks[1].approved_object_id),
        )

        result = fixture.run_validator(expected_gitlinks=expected_gitlinks)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Approved submodule commit is missing", result.stdout)

    def test_unreachable_submodule_commit_fails_closed(self) -> None:
        fixture = PublicationFixture(self)
        builds = fixture.gitlinks[0]
        unpublished_object_id = fixture.create_unpublished_submodule_commit(builds)
        fixture.stage_gitlink(builds.path, unpublished_object_id)
        expected_gitlinks = (
            (builds.path, unpublished_object_id),
            (fixture.gitlinks[1].path, fixture.gitlinks[1].approved_object_id),
        )

        result = fixture.run_validator(expected_gitlinks=expected_gitlinks)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not reachable from origin/main", result.stdout)

    def test_main_empty_arguments_do_not_fall_back_to_process_arguments(self) -> None:
        fixture = PublicationFixture(self)
        module = load_tool_module()
        process_arguments = [str(TOOL), *fixture.validator_arguments()]

        with mock.patch.object(sys, "argv", process_arguments):
            with redirect_stderr(io.StringIO()):
                with self.assertRaises(SystemExit) as raised:
                    module.main([])

        self.assertEqual(raised.exception.code, 2)


if __name__ == "__main__":
    unittest.main()
