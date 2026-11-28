"""Run locally with run-with-temp python3 -B _bmad/custom/tests/test_build_policy_wiring.py."""

import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


REPO = Path(__file__).resolve().parents[3]
CUSTOM = Path("_bmad/custom")
HOSTS = (".agents", ".claude", ".agent")
ROUTES = ("bmad-build", "bmad-build-auto")
POLICIES = (
    "story-scope-guard.md",
    "story-phase-ledger.md",
    "epic-ac-verification.md",
    "remediation-runtime-checklist.md",
)
FACTS = [f"file:{{project-root}}/{CUSTOM.as_posix()}/{policy}" for policy in POLICIES]


class BuildPolicyWiringTests(unittest.TestCase):
    def setUp(self):
        workspace = tempfile.TemporaryDirectory(prefix="build-policy-")
        self.addCleanup(workspace.cleanup)
        self.project = Path(workspace.name)
        shutil.copytree(REPO / "_bmad/scripts", self.project / "_bmad/scripts")
        shutil.copy2(REPO / "_bmad/config.toml", self.project / "_bmad/config.toml")
        (self.project / CUSTOM).mkdir()
        for name in ("config.toml", *POLICIES, *(f"{route}.toml" for route in ROUTES)):
            source = REPO / CUSTOM / name
            if source.is_file():
                shutil.copy2(source, self.project / CUSTOM / name)

    def run_script(self, script, skill, *args):
        result = subprocess.run(
            (
                "uv", "run", "--no-cache", str(self.project / "_bmad/scripts" / script),
                "--project-root", str(self.project), "--skill", str(skill), *args,
            ),
            cwd=self.project, capture_output=True, text=True, timeout=30,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result.stdout

    def assert_policy_wiring(self, host, route):
        skill = self.project / host / "skills" / route
        if not skill.exists():
            shutil.copytree(REPO / host / "skills" / route, skill)
        resolved = json.loads(self.run_script("resolve_customization.py", skill, "--key", "workflow"))["workflow"]
        result = self.run_script("render_skill.py", skill)
        prefix = "read and follow "
        self.assertTrue(result.startswith(prefix), result)
        entry = Path(result.rstrip().removeprefix(prefix))
        self.assertTrue(entry.is_relative_to(self.project))
        rendered = entry.read_text(encoding="utf-8")

        self.assertEqual(resolved["persistent_facts"], FACTS, f"{route} team policy facts did not resolve")
        for fact, policy in zip(FACTS, POLICIES, strict=True):
            self.assertIn(f"- {fact}\n", rendered)
            self.assertTrue((self.project / CUSTOM / policy).is_file())
        for directive in (
            "Carry these phase checks through routing, including direct-resume exits.",
            "delegated, add the four policy paths above to its frontmatter `context:`",
            "--changed-files0-file <cumulative-nul-paths>",
        ):
            self.assertTrue(any(directive in step for step in resolved["activation_steps_append"]))
            self.assertIn(directive, rendered)
        handoff = (entry.parent / "step-03-implement.md").read_text(encoding="utf-8")
        self.assertIn("Load every file listed in its frontmatter `context:`", handoff)

    def test_installed_routes_load_team_policy(self):
        for host in HOSTS:
            for route in ROUTES:
                with self.subTest(host=host, route=route):
                    self.assert_policy_wiring(host, route)

    def test_either_missing_team_override_fails_the_wiring_check(self):
        for route in ROUTES:
            with self.subTest(route=route):
                override = self.project / CUSTOM / f"{route}.toml"
                hidden = override.with_suffix(".disabled")
                override.rename(hidden)
                try:
                    with self.assertRaisesRegex(AssertionError, f"{route} team policy facts did not resolve"):
                        self.assert_policy_wiring(".agents", route)
                finally:
                    hidden.rename(override)


if __name__ == "__main__":
    unittest.main(verbosity=2)
