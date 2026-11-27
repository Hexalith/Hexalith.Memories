"""Verify Windows publishes staged reports without merging into existing output."""

import os
from pathlib import Path
import shutil
import stat
import subprocess
import tempfile
import unittest


HELPER = Path(__file__).resolve().parents[3] / "tools" / "prepare-test-results.ps1"
PROJECT = "Example.Tests"
LAUNCHER = r"""
param([string]$Helper, [string]$Repository, [string]$UnexpectedChild)
$ErrorActionPreference = 'Stop'
$results = Join-Path $Repository 'reports'
$unrelated = Join-Path $Repository 'unrelated'
$project = 'Example.Tests'

try {
    & $Helper $Repository $results @('reports') {
        param([string]$Staging)
        $projectStaging = Join-Path $Staging $project
        [void][IO.Directory]::CreateDirectory((Join-Path $projectStaging 'attachments'))
        [IO.File]::WriteAllText((Join-Path $projectStaging 'Example.Tests.trx'), 'staged report')
        [IO.File]::WriteAllText((Join-Path $projectStaging 'attachments/trace.txt'), 'staged attachment')

        # Introduce the unexpected child after ownership validation and cleanup.
        $destination = Join-Path $results $project
        if ($UnexpectedChild -eq 'Junction') {
            & $env:ComSpec /d /c mklink /j $destination $unrelated
            if ($LASTEXITCODE -ne 0) { throw 'Could not create the test junction.' }
        }
        elseif ($UnexpectedChild -eq 'Directory') {
            [void][IO.Directory]::CreateDirectory($destination)
            [IO.File]::WriteAllText((Join-Path $destination 'Example.Tests.trx'), 'existing report')
        }
        elseif ($UnexpectedChild -eq 'File') {
            [IO.File]::WriteAllText($destination, 'existing file')
        }
    }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
"""


@unittest.skipUnless(os.name == "nt" and shutil.which("pwsh"), "requires Windows PowerShell 7")
class WindowsReportPublicationTests(unittest.TestCase):
    def setUp(self):
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.workspace = Path(scratch.name)
        self.repository = self.workspace / "checkout"
        self.results = self.repository / "reports"
        self.unrelated = self.repository / "unrelated"
        self.unrelated.mkdir(parents=True)
        self.receipt = self.unrelated / f"{PROJECT}.trx"
        self.receipt.write_text("unrelated report", encoding="utf-8")
        self.helper = self.workspace / HELPER.name
        shutil.copy2(HELPER, self.helper)
        self.launcher = self.workspace / "publish.ps1"
        self.launcher.write_text(LAUNCHER, encoding="utf-8")

    def publish(self, unexpected="Absent"):
        return subprocess.run(
            [shutil.which("pwsh"), "-NoProfile", "-File", str(self.launcher), str(self.helper),
             str(self.repository), unexpected],
            capture_output=True, timeout=120,
        )

    def test_publication_preserves_project_layout(self):
        published = self.publish()
        self.assertEqual(0, published.returncode, published.stderr)
        project = self.results / PROJECT
        self.assertEqual("staged report", (project / f"{PROJECT}.trx").read_text(encoding="utf-8"))
        self.assertEqual("staged attachment", (project / "attachments" / "trace.txt").read_text(encoding="utf-8"))

    def test_publication_refuses_a_project_junction(self):
        refused = self.publish("Junction")
        self.assertEqual("unrelated report", self.receipt.read_text(encoding="utf-8"))
        self.assertFalse((self.unrelated / "attachments").exists())
        self.assertEqual(1, refused.returncode, refused.stderr)
        self.assertIn(b"could not publish", refused.stderr)
        self.assertTrue((self.results / PROJECT).lstat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT)

    def test_publication_refuses_existing_project_output(self):
        target = self.results / PROJECT
        for kind in ("Directory", "File"):
            with self.subTest(kind=kind):
                refused = self.publish(kind)
                self.assertEqual(1, refused.returncode, refused.stderr)
                self.assertIn(b"could not publish", refused.stderr)
                if kind == "Directory":
                    self.assertEqual("existing report", (target / f"{PROJECT}.trx").read_text(encoding="utf-8"))
                    self.assertFalse((target / "attachments").exists())
                else:
                    self.assertEqual("existing file", target.read_text(encoding="utf-8"))
