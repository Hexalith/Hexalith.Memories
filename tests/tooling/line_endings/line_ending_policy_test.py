import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RAZOR_PATTERNS = ("*.razor", "*.razor.css")
LF_PATTERNS = (
    ".gitattributes",
    "*.sh",
    "*.bash",
    "*.py",
    "*.yml",
    "*.yaml",
    "Dockerfile",
    "*.dockerfile",
    ".githooks/*",
)
LF_ATTRIBUTE_PROBES = (
    ".gitattributes",
    "policy-probe.sh",
    "policy-probe.bash",
    "policy-probe.py",
    "policy-probe.yml",
    "policy-probe.yaml",
    "Dockerfile",
    "policy-probe.dockerfile",
    ".githooks/policy-probe",
)
BINARY_PATTERNS = ("*.gif", "*.ico", "*.jpeg", "*.jpg", "*.pdf", "*.png", "*.webp", "*.zip")


def run_git(*args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        raise AssertionError(
            f"git {' '.join(args)} failed with {result.returncode}\n"
            f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}"
        )
    return result.stdout


def run_git_bytes(*args: str) -> bytes:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        raise AssertionError(
            f"git {' '.join(args)} failed with {result.returncode}\n"
            f"stdout:\n{result.stdout!r}\nstderr:\n{result.stderr!r}"
        )
    return result.stdout


def tracked_paths(*patterns: str) -> list[str]:
    output = run_git("ls-files", "--", *patterns)
    return [line for line in output.splitlines() if line]


def attributes_for(paths: list[str]) -> dict[str, dict[str, str]]:
    output = run_git("check-attr", "text", "eol", "--", *paths)
    attributes: dict[str, dict[str, str]] = {}
    for line in output.splitlines():
        path, attribute, value = line.split(": ", 2)
        attributes.setdefault(path, {})[attribute] = value
    return attributes


def index_eols_for(paths: list[str]) -> dict[str, str]:
    output = run_git("ls-files", "--eol", "--", *paths)
    eols: dict[str, str] = {}
    for line in output.splitlines():
        metadata, path = line.split("\t", 1)
        eols[path] = metadata.split()[0]
    return eols


class LineEndingPolicyTests(unittest.TestCase):
    def test_razor_paths_are_normalized_and_materialize_as_crlf(self) -> None:
        paths = tracked_paths(*RAZOR_PATTERNS)
        self.assertTrue(paths, "expected tracked Razor paths")

        attributes = attributes_for(paths)
        index_eols = index_eols_for(paths)

        for path in paths:
            self.assertEqual("set", attributes[path]["text"], path)
            self.assertEqual("crlf", attributes[path]["eol"], path)
            self.assertEqual("i/lf", index_eols[path], path)

    def test_unix_tooling_and_extensionless_hooks_stay_lf(self) -> None:
        paths = tracked_paths(*LF_PATTERNS)
        self.assertIn(".githooks/pre-commit", paths)
        self.assertIn(".githooks/commit-msg", paths)

        attributes = attributes_for(paths)
        index_eols = index_eols_for(paths)

        for path in paths:
            self.assertEqual("set", attributes[path]["text"], path)
            self.assertEqual("lf", attributes[path]["eol"], path)
            self.assertEqual("i/lf", index_eols[path], path)

        probe_attributes = attributes_for(list(LF_ATTRIBUTE_PROBES))
        for path in LF_ATTRIBUTE_PROBES:
            self.assertEqual("set", probe_attributes[path]["text"], path)
            self.assertEqual("lf", probe_attributes[path]["eol"], path)

    def test_declared_binary_payloads_disable_text_normalization(self) -> None:
        paths = tracked_paths(*BINARY_PATTERNS)
        self.assertTrue(paths, "expected at least one tracked declared binary")

        attributes = attributes_for(paths)

        for path in paths:
            self.assertEqual("unset", attributes[path]["text"], path)

    def test_conflicting_git_settings_preserve_checkout_contract(self) -> None:
        razor_paths = tracked_paths(*RAZOR_PATTERNS)
        lf_paths = tracked_paths(*LF_PATTERNS)
        binary_paths = tracked_paths(*BINARY_PATTERNS)
        paths = [*razor_paths, *lf_paths, *binary_paths]

        for autocrlf, eol in (("false", "lf"), ("true", "native")):
            with self.subTest(autocrlf=autocrlf, eol=eol):
                with tempfile.TemporaryDirectory() as temp:
                    checkout_root = Path(temp)
                    run_git(
                        "-c",
                        f"core.autocrlf={autocrlf}",
                        "-c",
                        f"core.eol={eol}",
                        "checkout-index",
                        f"--prefix={checkout_root}/",
                        "--",
                        *paths,
                    )

                    for path in razor_paths:
                        data = (checkout_root / path).read_bytes()
                        self.assertIn(b"\r\n", data, path)
                        normalized = data.replace(b"\r\n", b"")
                        self.assertNotIn(b"\r", normalized, path)
                        self.assertNotIn(b"\n", normalized, path)

                    for path in lf_paths:
                        data = (checkout_root / path).read_bytes()
                        self.assertNotIn(b"\r", data, path)

                    for path in binary_paths:
                        data = (checkout_root / path).read_bytes()
                        self.assertEqual(run_git_bytes("show", f":{path}"), data, path)

                    self.assert_localized_razor_diff(checkout_root / razor_paths[0])

    def assert_localized_razor_diff(self, razor_path: Path) -> None:
        before = razor_path.read_bytes()
        after = before.replace(
            b"\r\n",
            b" <!-- line-ending-policy-probe -->\r\n",
            1,
        )
        self.assertNotEqual(before, after)

        before_path = razor_path.with_suffix(".before.razor")
        after_path = razor_path.with_suffix(".after.razor")
        before_path.write_bytes(before)
        after_path.write_bytes(after)

        result = subprocess.run(
            [
                "git",
                "diff",
                "--no-index",
                "--unified=3",
                "--",
                str(before_path),
                str(after_path),
            ],
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(1, result.returncode, result.stderr)
        lines = result.stdout.splitlines()
        self.assertEqual(1, sum(line.startswith("@@") for line in lines), result.stdout)
        self.assertEqual(
            1,
            sum(line.startswith("-") and not line.startswith("---") for line in lines),
            result.stdout,
        )
        self.assertEqual(
            1,
            sum(line.startswith("+") and not line.startswith("+++") for line in lines),
            result.stdout,
        )


if __name__ == "__main__":
    unittest.main()
