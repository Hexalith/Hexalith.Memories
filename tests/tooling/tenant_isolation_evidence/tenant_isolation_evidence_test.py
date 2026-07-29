"""Contract tests for tools/check-tenant-isolation-evidence.py.

Every scenario in the spec I/O & Edge-Case Matrix is exercised here; the
fail-closed cases are this change's own attached cross-tenant negative evidence.
"""

import os
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-tenant-isolation-evidence.py"
REAL_SURFACES = REPO_ROOT / "tools" / "tenant-isolation-surfaces.txt"

# A real tenant-isolation surface file; used to prove the shipped globs are live.
REAL_SURFACE_PATH = "src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs"

SPEC_HEADER = textwrap.dedent(
    """\
    ---
    title: 'fixture'
    status: 'in-progress'
    ---

    ## Intent

    Fixture spec body.
    """
)

PROOF_SECTION = textwrap.dedent(
    """\
    ## Cross-Tenant Negative Evidence

    **Surfaces:** tenant authorization filter, actor routing
    **Tests:** `TenantIsolationVerifierTests`, `IndexSemanticActivityTests`
    **Command:** `dotnet test --filter TenantIsolationVerifierTests`
    **Result:** 35 passed; cross-tenant denial precedes dependencies.
    """
)

BLOCKER_SECTION = textwrap.dedent(
    """\
    ## Cross-Tenant Negative Evidence

    **Accepted blocker:** integration harness cannot build (CS0234).
    **Owner:** Jerome
    **Consequence:** cross-tenant denial unproven at the integration layer.
    **Reopen trigger:** the integration project compiles again.
    """
)

NOT_TRIGGERED_SECTION = textwrap.dedent(
    """\
    ## Cross-Tenant Negative Evidence

    **Not triggered:** the matched file only edits an XML doc comment; no live isolation path changes.
    """
)


def build_spec(section: str) -> str:
    return SPEC_HEADER + "\n" + section if section else SPEC_HEADER


class TenantIsolationEvidenceTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.tmp = Path(self._tmp.name)
        self.artifacts = self.tmp / "artifacts"
        self.artifacts.mkdir()
        self.story_key = "9-9-evidence-fixture"

    # --- fixtures -----------------------------------------------------------

    def write_spec(self, section: str, story_key: str | None = None) -> None:
        key = story_key or self.story_key
        (self.artifacts / f"{key}.md").write_text(build_spec(section), encoding="utf-8")

    def write_lines(self, name: str, lines: list[str]) -> Path:
        path = self.tmp / name
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return path

    def write_surfaces(self, globs: list[str]) -> Path:
        return self.write_lines("surfaces.txt", ["# test surfaces", *globs])

    def write_message(self, body: str) -> Path:
        path = self.tmp / "message.txt"
        path.write_text(textwrap.dedent(body), encoding="utf-8")
        return path

    def run_tool(self, args: list[str], env: dict | None = None) -> subprocess.CompletedProcess:
        run_env = {**os.environ, **(env or {})}
        return subprocess.run(
            [sys.executable, str(TOOL), *args],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
            env=run_env,
        )

    def base_args(self, changed: list[str], surfaces: Path, **extra) -> list[str]:
        changed_file = self.write_lines("changed.txt", changed)
        args = [
            "--surfaces-file", str(surfaces),
            "--changed-files-file", str(changed_file),
            "--artifacts-root", str(self.artifacts),
        ]
        for flag, value in extra.items():
            args.extend([f"--{flag.replace('_', '-')}", value])
        return args

    # --- matrix rows --------------------------------------------------------

    def test_no_changed_files_is_noop(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        empty = self.write_lines("changed.txt", [])
        result = self.run_tool(
            ["--surfaces-file", str(surfaces), "--changed-files-file", str(empty),
             "--artifacts-root", str(self.artifacts)]
        )
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("no-op", result.stdout)

    def test_non_sensitive_change_is_noop(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["README.md", "docs/dev/x.md"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("No tenant-isolation surface changed", result.stdout)

    def test_sensitive_with_valid_proof_passes(self):
        self.write_spec(PROOF_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/TenantAuthorizationMiddleware.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("validation passed", result.stdout)

    def test_sensitive_with_accepted_blocker_passes(self):
        self.write_spec(BLOCKER_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_sensitive_with_not_triggered_passes(self):
        self.write_spec(NOT_TRIGGERED_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_sensitive_missing_section_fails(self):
        self.write_spec("")  # spec without the evidence section
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Cross-Tenant Negative Evidence", result.stdout)

    def test_sensitive_empty_result_field_fails(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            **Surfaces:** tenant filter
            **Tests:** `SomeTest`
            **Command:** `dotnet test`
            **Result:**
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Result", result.stdout)

    def test_sensitive_tests_without_backtick_fails(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            **Surfaces:** tenant filter
            **Tests:** we ran the isolation tests
            **Command:** `dotnet test`
            **Result:** all green
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Tests", result.stdout)

    def test_accepted_blocker_missing_owner_fails(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            **Accepted blocker:** cannot run integration proof
            **Consequence:** denial unproven
            **Reopen trigger:** harness builds
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Owner", result.stdout)

    def test_not_triggered_without_reason_fails(self):
        section = "## Cross-Tenant Negative Evidence\n\n**Not triggered:**\n"
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Not triggered", result.stdout)

    def test_sensitive_no_story_resolvable_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        # Branch name carries no story key, no --story-key, no message trailer.
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main")
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("no story key resolved", result.stdout.lower())

    def test_bypass_trailer_with_reason_passes(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Tenant-Isolation-Evidence: not-applicable — pure rename, no isolation path changed
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("Bypass accepted", result.stdout)

    def test_bypass_trailer_without_reason_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Tenant-Isolation-Evidence: not-applicable
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("non-empty reason", result.stdout)

    def test_story_key_from_trailer_passes(self):
        self.write_spec(PROOF_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            f"""\
            chore: touch a tenant surface

            Story: {self.story_key}
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_story_key_from_branch_passes(self):
        self.write_spec(PROOF_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces,
                              branch_name=f"feature/{self.story_key}")
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_spec_key_from_cli_trailer_and_branch_passes(self):
        spec_key = "spec-tenant-evidence-fixture"
        self.write_spec(PROOF_SECTION, story_key=spec_key)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            f"""\
            test: verify standalone spec evidence

            Story-Key: {spec_key}
            """
        )
        cases = (
            ("cli", {"story_key": spec_key}),
            ("trailer", {"branch_name": "main", "commit_message_file": str(message)}),
            ("branch", {"branch_name": f"fix/{spec_key}"}),
        )

        for expected_source, source_args in cases:
            with self.subTest(source=expected_source):
                args = self.base_args(["src/Foo/Tenant.cs"], surfaces, **source_args)
                result = self.run_tool(args)

                self.assertEqual(result.returncode, 0, result.stdout)
                self.assertIn(f"{spec_key}.md (source: {expected_source})", result.stdout)

    def test_spec_trailer_conflicting_with_numeric_story_branch_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            test: verify standalone spec evidence

            Story-Key: spec-tenant-evidence-fixture
            """
        )
        args = self.base_args(
            ["src/Foo/Tenant.cs"],
            surfaces,
            branch_name="feature/9-9-evidence-fixture",
            commit_message_file=str(message),
        )

        result = self.run_tool(args)

        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Conflicting story keys", result.stdout)
        self.assertIn("trailer=spec-tenant-evidence-fixture", result.stdout)
        self.assertIn("branch=9-9-evidence-fixture", result.stdout)

    def test_malformed_spec_key_fails_from_cli_trailer_and_branch(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            test: reject malformed standalone spec key

            Story-Key: spec-tenant-evidence.extra
            """
        )
        cases = (
            ("cli", {"story_key": "spec-tenant-evidence.extra"}),
            ("trailer", {"commit_message_file": str(message)}),
            ("branch", {"branch_name": "fix/spec-tenant-evidence.extra"}),
        )

        for source, source_args in cases:
            with self.subTest(source=source):
                args = self.base_args(["src/Foo/Tenant.cs"], surfaces, **source_args)
                result = self.run_tool(args)

                self.assertEqual(result.returncode, 1, result.stdout)
                self.assertIn("valid standalone spec key", result.stdout)

    def test_incomplete_spec_key_fails_from_cli_trailer_and_branch(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        for malformed in ("spec-", "spec-tenant-evidence-"):
            message = self.write_message(
                f"test: reject incomplete standalone spec key\n\nStory-Key: {malformed}\n"
            )
            cases = (
                ("cli", {"story_key": malformed}),
                ("trailer", {"commit_message_file": str(message)}),
                ("branch", {"branch_name": f"fix/{malformed}"}),
            )

            for source, source_args in cases:
                with self.subTest(malformed=malformed, source=source):
                    args = self.base_args(["src/Foo/Tenant.cs"], surfaces, **source_args)
                    result = self.run_tool(args)

                    self.assertEqual(result.returncode, 1, result.stdout)
                    self.assertIn("valid standalone spec key", result.stdout)

    def test_duplicate_spec_key_fails_from_cli_trailer_and_branch(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        duplicate = "spec-tenant-evidence/spec-tenant-evidence"
        message = self.write_message(
            f"""\
            test: reject duplicate standalone spec key

            Story-Key: {duplicate}
            """
        )
        cases = (
            ("cli", {"story_key": duplicate}),
            ("trailer", {"commit_message_file": str(message)}),
            ("branch", {"branch_name": f"fix/{duplicate}"}),
        )

        for source, source_args in cases:
            with self.subTest(source=source):
                args = self.base_args(["src/Foo/Tenant.cs"], surfaces, **source_args)
                result = self.run_tool(args)

                self.assertEqual(result.returncode, 1, result.stdout)
                self.assertIn("multiple story keys", result.stdout.lower())

    def test_unicode_spec_key_does_not_alias_ascii_artifact(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(
            ["src/Foo/Tenant.cs"],
            surfaces,
            story_key="spec-Key",
        )

        result = self.run_tool(args)

        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("valid standalone spec key", result.stdout)

    def test_bypass_does_not_hide_owner_conflict(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            test: reject conflicting owner under bypass

            Story-Key: 9-9-evidence-fixture
            Tenant-Isolation-Evidence: not-applicable - verifier-only change
            """
        )
        args = self.base_args(
            ["src/Foo/Tenant.cs"],
            surfaces,
            branch_name="fix/spec-tenant-evidence-fixture",
            commit_message_file=str(message),
        )

        result = self.run_tool(args)

        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Conflicting story keys", result.stdout)

    def test_heading_inside_code_fence_is_not_a_section(self):
        section = textwrap.dedent(
            """\
            ## Design Notes

            ```markdown
            ## Cross-Tenant Negative Evidence
            **Surfaces:** looks real but is fenced
            ```
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("missing required", result.stdout)

    def test_story_artifact_not_found_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key="1-1-does-not-exist")
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("not found", result.stdout)

    def test_missing_changed_files_input_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        result = self.run_tool(
            ["--surfaces-file", str(surfaces),
             "--changed-files-file", str(self.tmp / "nope.txt"),
             "--artifacts-root", str(self.artifacts)]
        )
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("does not exist", result.stdout)

    def test_missing_git_reports_clean_error(self):
        # Trailer parsing needs git; simulate its absence with an empty PATH.
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Story: 9-9-evidence-fixture
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args, env={"PATH": ""})
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("interpret-trailers", result.stdout)

    def test_conflicting_story_keys_fail(self):
        self.write_spec(PROOF_SECTION)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Story: 9-9-evidence-fixture
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces,
                              branch_name="feature/1-2-other-thing",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Conflicting story keys", result.stdout)

    # --- shipped-config guards (no dead / typo'd globs) ----------------------

    def test_shipped_surfaces_match_a_real_surface_file(self):
        self.write_spec(PROOF_SECTION)
        args = [
            "--surfaces-file", str(REAL_SURFACES),
            "--changed-files-file", str(self.write_lines("changed.txt", [REAL_SURFACE_PATH])),
            "--artifacts-root", str(self.artifacts),
            "--story-key", self.story_key,
        ]
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("Tenant-isolation surface(s) changed", result.stdout)

    def test_shipped_surfaces_ignore_non_surface_file(self):
        args = [
            "--surfaces-file", str(REAL_SURFACES),
            "--changed-files-file", str(self.write_lines("changed.txt", ["README.md", "tools/test.sh"])),
            "--artifacts-root", str(self.artifacts),
        ]
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("No tenant-isolation surface changed", result.stdout)

    # --- fail-open regression guards (review loop 1) -------------------------

    def test_fenced_fields_inside_section_do_not_count(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            ```markdown
            **Surfaces:** placeholder
            **Tests:** `Placeholder`
            **Command:** `dotnet test`
            **Result:** example only
            ```
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Surfaces", result.stdout)

    def test_html_comment_fields_do_not_count(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            <!--
            **Surfaces:** hidden placeholder
            **Tests:** `Hidden`
            **Command:** `dotnet test`
            **Result:** hidden
            -->
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Surfaces", result.stdout)

    def test_surfaces_file_missing_fails(self):
        args = self.base_args(["src/Foo/Tenant.cs"], self.tmp / "no-surfaces.txt",
                              story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("does not exist", result.stdout)

    def test_surfaces_file_comment_only_fails(self):
        surfaces = self.write_lines("surfaces.txt", ["# only comments", "   ", "# nothing here"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("no glob entries", result.stdout)

    def test_bypass_wrong_disposition_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Tenant-Isolation-Evidence: applicable - I decided it is fine
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Invalid Tenant-Isolation-Evidence trailer", result.stdout)

    def test_bypass_token_without_boundary_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Tenant-Isolation-Evidence: not-applicableXYZ sneaky reason
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Invalid Tenant-Isolation-Evidence trailer", result.stdout)

    def test_bulleted_proof_fields_pass(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            - **Surfaces:** tenant authorization filter
            - **Tests:** `TenantIsolationVerifierTests`
            - **Command:** `dotnet test --filter TenantIsolationVerifierTests`
            - **Result:** 12 passed; denial precedes dependencies.
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_proof_empty_surfaces_fails(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            **Surfaces:**
            **Tests:** `SomeTest`
            **Command:** `dotnet test`
            **Result:** all green
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Surfaces", result.stdout)

    def test_proof_command_without_backtick_fails(self):
        section = textwrap.dedent(
            """\
            ## Cross-Tenant Negative Evidence

            **Surfaces:** tenant filter
            **Tests:** `SomeTest`
            **Command:** just run the tests
            **Result:** all green
            """
        )
        self.write_spec(section)
        surfaces = self.write_surfaces(["src/**/*.cs"])
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, story_key=self.story_key)
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Command", result.stdout)

    def test_malformed_story_trailer_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Story: not-a-valid-key
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Malformed Story trailer", result.stdout)

    def test_multi_key_single_story_trailer_fails(self):
        surfaces = self.write_surfaces(["src/**/*.cs"])
        message = self.write_message(
            """\
            chore: touch a tenant surface

            Story: 1-1-alpha-and-2-2-beta
            """
        )
        args = self.base_args(["src/Foo/Tenant.cs"], surfaces, branch_name="main",
                              commit_message_file=str(message))
        result = self.run_tool(args)
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Multiple story keys", result.stdout)

    def test_shipped_surfaces_have_no_dead_globs(self):
        import importlib.util

        spec = importlib.util.spec_from_file_location("evidence_gate", TOOL)
        module = importlib.util.module_from_spec(spec)
        sys.modules[spec.name] = module  # frozen dataclasses resolve via sys.modules
        self.addCleanup(lambda: sys.modules.pop(spec.name, None))
        spec.loader.exec_module(module)

        tracked = subprocess.run(
            ["git", "ls-files", "src"],
            cwd=REPO_ROOT, text=True, capture_output=True, check=True,
        ).stdout.splitlines()

        globs = module.load_surface_globs(REAL_SURFACES)
        self.assertGreater(len(globs), 0)
        for glob in globs:
            self.assertTrue(
                any(module.matches_glob(path, glob) for path in tracked),
                f"shipped surface glob matches no tracked file (dead/typo'd): {glob}",
            )


if __name__ == "__main__":
    unittest.main()
