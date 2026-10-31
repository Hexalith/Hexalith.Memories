"""Guard the real shared caller and execute its retained fail-closed shell gates."""

from __future__ import annotations

import copy
import itertools
import json
import os
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SHA = "81c88b34d09d34260b92d7f0928dbbc1c7446baa"
CALLER = f"Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@{SHA}"
CONTRACTS = "tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj"
AUDIT = "-p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904"
SOURCE_COMMAND = (
    "dotnet restore Hexalith.Memories.slnx -p:Configuration=Debug "
    f"-p:UseHexalithProjectReferences=true {AUDIT}"
)
CRITICAL_STEPS = {
    "story-file-scope": (
        "Collect story scope inputs", "Validate story file scope",
        "Validate cross-tenant negative evidence", "Validate story slice scope",
        "Validate story review readiness",
    ),
    "source-debug": (
        "Restore solution (Debug/source)", "Build solution (Debug/source)",
        "Verify source-selected owner catalog contracts", "Upload source catalog verification",
    ),
    "test-unit-contract": (
        "Run shared CI alignment fixtures", "Run coverage gate fixtures",
        "Run tenant-isolation evidence gate fixtures", "Run commitlint configuration fixtures",
        "Run story file-scope gate fixtures", "Run story slice-scope gate fixtures",
        "Run story review-readiness gate fixtures", "Pack and validate release packages",
        "Run Docker-free unit and contract tests", "Validate scoped line coverage",
        "Upload unit and contract test results",
    ),
    "web-e2e-specimen": (
        "Build Memories web specimen host", "Run Memories web specimen browser checks",
        "Validate Memories web browser artifacts", "Upload Memories web E2E artifacts",
    ),
    "integration-fast": (
        "Run fast Docker-backed integration tests", "Verify fast integration coverage evidence",
        "Upload fast integration test results",
    ),
    "production-deployment-verification": (
        "Publish local release OCI archives", "Verify disposable production rollout",
        "Validate production deployment evidence", "Upload production deployment evidence",
    ),
    "access-telemetry-sql-contract": (
        "Pull approved PostgreSQL contract image", "Run access-telemetry C1 SQL contract fixtures",
    ),
}


# These are the mandatory commands retained from the approved caller. The literal
# inventory also keeps every tooling fixture blocking when new job layouts change.
MANDATORY_COMMANDS = {
    ('story-file-scope', 'Validate story file scope'): 'python3 tools/check-story-file-scope.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"',
    ('story-file-scope', 'Validate cross-tenant negative evidence'): 'python3 tools/check-tenant-isolation-evidence.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"',
    ('story-file-scope', 'Validate story slice scope'): 'python3 tools/check-story-slice-scope.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"',
    ('story-file-scope', 'Validate story review readiness'): 'python3 tools/check-story-review-readiness.py --branch-name "$BRANCH_NAME" --commit-message-file "$COMMIT_MESSAGE_FILE" --changed-files-file "$CHANGED_FILES_FILE"',
    ('source-debug', 'Restore solution (Debug/source)'): 'dotnet restore Hexalith.Memories.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904',
    ('source-debug', 'Build solution (Debug/source)'): 'dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore',
    ('test-unit-contract', 'Install root release tooling'): 'npm ci',
    ('test-unit-contract', 'Audit root release tooling'): 'npm audit signatures',
    ('test-unit-contract', 'Verify semantic-release configuration offline'): 'npm run verify:semantic-release-config',
    ('test-unit-contract', 'Run commitlint configuration fixtures'): 'python3 -m unittest discover -s tests/tooling/commitlint -p "*_test.py"',
    ('test-unit-contract', 'Restore solution'): 'dotnet restore Hexalith.Memories.slnx -p:Configuration=Release -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904',
    ('test-unit-contract', 'Build solution'): 'dotnet build Hexalith.Memories.slnx --configuration Release --no-restore',
    ('test-unit-contract', 'Run BMad customization fixtures'): 'python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"',
    ('test-unit-contract', 'Run tenant-isolation evidence gate fixtures'): 'python3 -m unittest discover -s tests/tooling/tenant_isolation_evidence -p "*_test.py"',
    ('test-unit-contract', 'Verify repository line-ending policy'): 'python3 -m unittest discover -s tests/tooling/line_endings -p "*_test.py"',
    ('test-unit-contract', 'Run shared CI alignment fixtures'): 'python3 -m unittest discover -s tests/tooling/ci_alignment -p "*_test.py"',
    ('test-unit-contract', 'Run coverage gate fixtures'): 'python3 -m unittest discover -s tests/tooling/coverage_gate -p "*_test.py"',
    ('test-unit-contract', 'Run published host profile fixtures'): 'python3 -m unittest discover -s tests/tooling/published_host_profile -p "*_test.py"',
    ('test-unit-contract', 'Run release package fixtures'): 'python3 -m unittest discover -s tests/tooling/release_packages -p "*_test.py"',
    ('test-unit-contract', 'Run NuGet publish fixtures'): 'python3 -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"',
    ('test-unit-contract', 'Run container publish fixtures'): 'python3 -m unittest discover -s tests/tooling/publish_containers -p "*_test.py"',
    ('test-unit-contract', 'Run production deployment evidence fixtures'): 'python3 -m unittest discover -s tests/tooling/production_deployment_evidence -p "*_test.py"',
    ('test-unit-contract', 'Publish pinned kubectl at the owner-discovery path'): 'installed="$(command -v kubectl)"\ntest -n "$installed"\nif [ "$installed" != "/usr/local/bin/kubectl" ]; then\n  sudo ln -sfn "$installed" /usr/local/bin/kubectl\nfi\ntest -x /usr/local/bin/kubectl\n',
    ('test-unit-contract', 'Run access-telemetry owner discovery fixtures'): 'PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_owner_discovery -p "test_*.py"',
    ('test-unit-contract', 'Build source Debug descriptor reader'): 'dotnet build references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -p:RunAnalyzers=false -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904',
    ('test-unit-contract', 'Run access-telemetry lifecycle C1 fixtures'): 'PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p "test_*.py"',
    ('test-unit-contract', 'Run access-telemetry functional-retention safety fixtures'): 'PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_functional_retention -p "test_*.py"',
    ('test-unit-contract', 'Run access-telemetry C1 producer fixtures'): 'PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p "*_test.py"',
    ('test-unit-contract', 'Initialize root Platform review-reader dependency'): 'git submodule update --init references/Hexalith.Platform',
    ('test-unit-contract', 'Run access-telemetry C1 interchange and HTTPS review fixtures'): 'PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p "test_*.py"',
    ('test-unit-contract', 'Run story file-scope gate fixtures'): 'python3 -m unittest discover -s tests/tooling/story_scope -p "*_test.py"',
    ('test-unit-contract', 'Run approved publication preflight fixtures'): 'python3 -m unittest discover -s tests/tooling/approved_publication -p "*_test.py"',
    ('test-unit-contract', 'Run story slice-scope gate fixtures'): 'python3 -m unittest discover -s tests/tooling/story_slice_scope -p "*_test.py"',
    ('test-unit-contract', 'Run story review-readiness gate fixtures'): 'python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py"',
    ('test-unit-contract', 'Run integration stub closure fixtures'): 'python3 -m unittest discover -s tests/tooling/integration_stub_closure -p "*_test.py"',
    ('test-unit-contract', 'Run integration-fast coverage verifier fixtures'): 'python3 -m unittest discover -s tests/tooling/integration_fast_coverage -p "*_test.py"',
    ('test-unit-contract', 'Pack and validate release packages'): 'pwsh ./tools/pack-release.ps1 -Version 0.0.264 -OutputDirectory artifacts/packages/ci -PackageOnly',
    ('test-unit-contract', 'Run Docker-free unit and contract tests'): 'bash ./tools/test.sh --filter "Category!=Integration" --configuration Release --no-build --coverage --results-directory TestResults/test-unit-contract',
    ('test-unit-contract', 'Validate scoped line coverage'): 'python3 tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json',
    ('web-e2e-specimen', 'Restore solution'): 'dotnet restore Hexalith.Memories.slnx -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904',
    ('web-e2e-specimen', 'Build Memories web specimen host'): 'dotnet build tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj --configuration Debug --no-restore -m:1',
    ('web-e2e-specimen', 'Install Memories web E2E dependencies'): 'npm ci',
    ('web-e2e-specimen', 'Install Chromium browser'): 'npx playwright install --with-deps chromium',
    ('web-e2e-specimen', 'Type-check Memories web E2E workspace'): 'npm run typecheck',
    ('web-e2e-specimen', 'Run Memories web specimen browser checks'): 'npm run test',
    ('web-e2e-specimen', 'Validate Memories web browser artifacts'): 'npm run validate:artifacts',
    ('integration-fast', 'Initialize Dapr runtime'): 'bash ./tools/init-dapr-runtime.sh',
    ('integration-fast', 'Verify Docker availability'): 'docker version\ndocker info\n',
    ('integration-fast', 'Restore solution'): 'dotnet restore Hexalith.Memories.slnx -p:Configuration=Release -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904',
    ('integration-fast', 'Build solution'): 'dotnet build Hexalith.Memories.slnx --configuration Release --no-restore',
    ('integration-fast', 'Run fast Docker-backed integration tests'): 'bash ./tools/test.sh --filter "Category=Integration&Category!=IntegrationSlow&Category!=Performance" --configuration Release --no-build --results-directory TestResults/integration-fast',
    ('integration-fast', 'Verify fast integration coverage evidence'): 'python3 tools/verify-integration-fast-coverage.py --results-directory TestResults/integration-fast',
    ('production-deployment-verification', 'Publish local release OCI archives'): './tools/publish-containers.ps1 -Version "0.0.0-ci.${{ github.run_number }}"',
    ('production-deployment-verification', 'Verify disposable production rollout'): './tools/verify-production-deployment.ps1 `\n  -ServerArchive ./artifacts/containers/release/server.tar.gz `\n  -McpArchive ./artifacts/containers/release/mcp.tar.gz `\n  -AccessTelemetryArchive ./artifacts/containers/release/access-telemetry.tar.gz `\n  -AccessTelemetryClockArchive ./artifacts/containers/release/access-telemetry-clock.tar.gz `\n  -Version "0.0.0-ci.${{ github.run_number }}" `\n  -DaprRuntimeVersion $env:DAPR_RUNTIME_VERSION `\n  -KindNodeImage $env:KIND_NODE_IMAGE `\n  -EvidenceDirectory ./artifacts/production-deployment-verification\n',
    ('production-deployment-verification', 'Validate production deployment evidence'): './tools/validate-production-deployment-evidence.ps1 -EvidenceDirectory ./artifacts/production-deployment-verification',
    ('access-telemetry-sql-contract', 'Pull approved PostgreSQL contract image'): 'docker pull docker.io/library/postgres@sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04',
    ('access-telemetry-sql-contract', 'Run access-telemetry C1 SQL contract fixtures'): 'PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p "*_test.py"',
}
CRITICAL_STEPS = {
    owner: tuple(dict.fromkeys((
        *titles,
        *(title for lane, title in MANDATORY_COMMANDS if lane == owner),
    )))
    for owner, titles in CRITICAL_STEPS.items()
}



def read_workflow(name="ci.yml"):
    # Reuse the pinned js-yaml contract used by the commitlint entrypoint fixtures.
    result = subprocess.run(
        ["node", "--input-type=module", "-e",
         "import fs from 'node:fs'; import yaml from 'js-yaml'; "
         "console.log(JSON.stringify(yaml.load(fs.readFileSync(process.argv[1], 'utf8'))));",
         str(REPO_ROOT / ".github/workflows" / name)],
        cwd=REPO_ROOT, text=True, capture_output=True, check=True, timeout=20,
    )
    return json.loads(result.stdout)


def step(job, name):
    matches = [entry for entry in job["steps"] if entry.get("name") == name]
    if len(matches) != 1:
        raise AssertionError(f"Expected exactly one step {name!r}")
    return matches[0]


class SharedCiAlignmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = read_workflow()

    def assert_adoption(self, workflow):
        jobs = workflow["jobs"]
        self.assertEqual({"contents": "read"}, workflow["permissions"])
        for event in ("pull_request", "push"):
            self.assertEqual({"branches": ["main"]}, workflow["on"][event])
        shared = jobs["shared-ci"]
        self.assertEqual(CALLER, shared["uses"])
        self.assertEqual({
            "solution": "Hexalith.Memories.slnx", "dotnet-global-json": "global.json",
            "builds-execution-sha": SHA, "test-platform": "microsoft-testing-platform",
            "unit-test-projects": CONTRACTS, "integration-test-projects": "",
            "aspire-test-project": "", "run-consumer-validation": False,
            "run-coverage-gate": False, "build-timeout-minutes": 20,
        }, shared["with"])
        for key in ("if", "continue-on-error", "needs", "secrets"):
            self.assertNotIn(key, shared)
        aggregate = jobs["build"]
        self.assertEqual("build", aggregate["name"])
        self.assertEqual("${{ always() }}", aggregate["if"])
        self.assertEqual(["shared-ci", "source-debug"], aggregate["needs"])
        self.assertNotIn("continue-on-error", aggregate)
        gate = step(aggregate, "Require shared Release and source Debug success")
        self.assertNotIn("if", gate)
        self.assertNotIn("continue-on-error", gate)
        self.assertEqual({
            "SHARED_CI_RESULT": "${{ needs.shared-ci.result }}",
            "SOURCE_DEBUG_RESULT": "${{ needs.source-debug.result }}",
        }, gate["env"])
        self.assertNotIn("${{", gate["run"])
        for name, names in CRITICAL_STEPS.items():
            job = jobs[name]
            self.assertEqual(name, job["name"])
            self.assertNotIn("continue-on-error", job)
            if name == "story-file-scope":
                self.assertEqual("${{ github.event_name != 'push' || github.ref_name != 'main' }}", job["if"])
            else:
                self.assertNotIn("if", job)
            self.assertNotIn("needs", job)
            for title in names:
                entry = step(job, title)
                self.assertNotIn("continue-on-error", entry)
                expected_if = (
                    "always()" if title.startswith("Upload ") or title == "Validate production deployment evidence"
                    else "${{ !cancelled() }}" if title == "Verify fast integration coverage evidence"
                    else None
                )
                self.assertEqual(expected_if, entry.get("if"), title)
        self.assertEqual(SOURCE_COMMAND, step(jobs["source-debug"], "Restore solution (Debug/source)")["run"])
        self.assertEqual(
            "dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore",
            step(jobs["source-debug"], "Build solution (Debug/source)")["run"],
        )
        for (owner, title), command in MANDATORY_COMMANDS.items():
            self.assertEqual(command, step(jobs[owner], title)["run"], f"{owner}: {title}")
        self.assertIn(
            'git -c core.quotepath=false diff --no-renames --name-only',
            step(jobs["story-file-scope"], "Collect story scope inputs")["run"],
        )
        self.assertIn(
            "dotnet tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll "
            "-class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests -xml TestResults/source-catalog.xml",
            step(jobs["source-debug"], "Verify source-selected owner catalog contracts")["run"],
        )
        for owner in ("source-debug", "test-unit-contract", "web-e2e-specimen", "integration-fast"):
            restores = [entry["run"] for entry in jobs[owner]["steps"] if "dotnet restore " in entry.get("run", "")]
            self.assertEqual(1, len(restores), owner)
            self.assertIn(AUDIT, restores[0])
        for owner, title, artifact, path, days, missing in (
            ("source-debug", "Upload source catalog verification", "source-catalog-verification", "TestResults/source-catalog.xml", None, "error"),
            ("test-unit-contract", "Upload unit and contract test results", "test-unit-contract-results", "TestResults/test-unit-contract/**", 14, "error"),
            ("integration-fast", "Upload fast integration test results", "integration-fast-results", "TestResults/integration-fast/**", 14, "error"),
            ("web-e2e-specimen", "Upload Memories web E2E artifacts", "memories-web-e2e-artifacts", "tests/Hexalith.Memories.Web.E2E/test-results/**\ntests/Hexalith.Memories.Web.E2E/playwright-report/**\n", 14, "warn"),
            ("production-deployment-verification", "Upload production deployment evidence", "production-deployment-evidence", "artifacts/containers/release/**\nartifacts/production-deployment-verification/**\n", 14, "error"),
        ):
            upload = step(jobs[owner], title)
            self.assertEqual(
                "actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9"
                if owner == "source-debug" else "actions/upload-artifact@v7",
                upload["uses"],
            )
            self.assertEqual(artifact, upload["with"]["name"])
            self.assertEqual(path, upload["with"]["path"])
            self.assertEqual(days, upload["with"].get("retention-days"))
            self.assertEqual(missing, upload["with"]["if-no-files-found"])
        source = json.dumps(workflow)
        self.assertNotRegex(source, r"dotnet nuget push|npx semantic-release|publish-containers\.ps1[^\n]* -Push")

    def test_real_pr_push_caller_preserves_pinned_shared_and_mandatory_local_gates(self):
        self.assert_adoption(self.workflow)

    def test_wrong_identity_platform_or_project_selections_are_rejected(self):
        mutations = [("uses", CALLER.replace(SHA, "main")), ("uses", CALLER.replace(SHA, SHA[:-1] + "b"))]
        for key, value in mutations:
            workflow = copy.deepcopy(self.workflow)
            workflow["jobs"]["shared-ci"][key] = value
            with self.subTest(key=key, value=value), self.assertRaises(AssertionError):
                self.assert_adoption(workflow)
        for key, value in (
            ("solution", "wrong.slnx"), ("builds-execution-sha", "0" * 40),
            ("test-platform", "vstest"), ("unit-test-projects", ""),
            ("unit-test-projects", CONTRACTS + "\ntests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj"),
            ("integration-test-projects", CONTRACTS), ("aspire-test-project", CONTRACTS),
            ("run-coverage-gate", True), ("run-consumer-validation", True),
            ("module-manifest", "module.yaml"), ("governed-ci", True),
            ("build-timeout-minutes", 0),
        ):
            workflow = copy.deepcopy(self.workflow)
            workflow["jobs"]["shared-ci"]["with"][key] = value
            with self.subTest(key=key, value=value), self.assertRaises(AssertionError):
                self.assert_adoption(workflow)

    def test_missing_or_bypassed_required_jobs_and_steps_are_rejected(self):
        for name in (*CRITICAL_STEPS, "shared-ci", "build"):
            for mutation in ("remove", "continue-on-error", "if"):
                workflow = copy.deepcopy(self.workflow)
                if mutation == "remove":
                    del workflow["jobs"][name]
                else:
                    workflow["jobs"][name][mutation] = True if mutation == "continue-on-error" else "${{ false }}"
                with self.subTest(job=name, mutation=mutation), self.assertRaises((AssertionError, KeyError)):
                    self.assert_adoption(workflow)
        for owner, titles in CRITICAL_STEPS.items():
            for title, mutation in itertools.product(titles, ("remove", "if", "continue-on-error")):
                workflow = copy.deepcopy(self.workflow)
                entry = step(workflow["jobs"][owner], title)
                if mutation == "remove":
                    workflow["jobs"][owner]["steps"].remove(entry)
                else:
                    entry[mutation] = True if mutation == "continue-on-error" else "${{ false }}"
                with self.subTest(owner=owner, title=title, mutation=mutation), self.assertRaises(AssertionError):
                    self.assert_adoption(workflow)

    def test_replacing_any_mandatory_command_with_success_is_rejected(self):
        for owner, titles in CRITICAL_STEPS.items():
            for title in titles:
                workflow = copy.deepcopy(self.workflow)
                entry = step(workflow["jobs"][owner], title)
                if "run" not in entry:
                    continue
                entry["run"] = "true"
                with self.subTest(owner=owner, title=title), self.assertRaises(AssertionError):
                    self.assert_adoption(workflow)

    def test_substituting_artifact_upload_actions_is_rejected(self):
        for owner, titles in CRITICAL_STEPS.items():
            for title in titles:
                if not title.startswith("Upload "):
                    continue
                workflow = copy.deepcopy(self.workflow)
                step(workflow["jobs"][owner], title)["uses"] = "actions/checkout@v7"
                with self.subTest(owner=owner, title=title), self.assertRaises(AssertionError):
                    self.assert_adoption(workflow)

    def test_trigger_path_branch_tag_and_event_exclusions_are_rejected(self):
        for event in ("pull_request", "push"):
            for key, value in (
                ("paths-ignore", ["**"]), ("paths", ["docs/**"]),
                ("branches-ignore", ["main"]), ("branches", ["main", "!main"]),
                ("types", ["edited"]), ("tags-ignore", ["**"]), ("tags", ["v*"]),
            ):
                workflow = copy.deepcopy(self.workflow)
                workflow["on"][event][key] = value
                with self.subTest(event=event, key=key), self.assertRaises(AssertionError):
                    self.assert_adoption(workflow)

    def test_dropped_local_audit_exceptions_and_publication_commands_are_rejected(self):
        for owner in ("source-debug", "test-unit-contract", "web-e2e-specimen", "integration-fast"):
            workflow = copy.deepcopy(self.workflow)
            restore = next(entry for entry in workflow["jobs"][owner]["steps"]
                           if "dotnet restore " in entry.get("run", ""))
            restore["run"] = restore["run"].replace(AUDIT, "")
            with self.subTest(owner=owner), self.assertRaises(AssertionError):
                self.assert_adoption(workflow)
        for command in ("dotnet nuget push package.nupkg", "npx semantic-release",
                        "pwsh ./tools/publish-containers.ps1 -Push"):
            workflow = copy.deepcopy(self.workflow)
            workflow["jobs"]["test-unit-contract"]["steps"].append({"name": "Publish", "run": command})
            with self.subTest(command=command), self.assertRaises(AssertionError):
                self.assert_adoption(workflow)

    def test_aggregate_executes_all_dependency_result_combinations(self):
        gate = step(self.workflow["jobs"]["build"], "Require shared Release and source Debug success")
        results = ("success", "failure", "cancelled", "skipped", "unknown", "", "$(exit 0)")
        for shared, source in itertools.product(results, repeat=2):
            with self.subTest(shared=shared, source=source):
                result = subprocess.run(
                    ["bash", "-e", "-c", gate["run"]], capture_output=True, text=True, timeout=10,
                    env={**os.environ, "SHARED_CI_RESULT": shared, "SOURCE_DEBUG_RESULT": source},
                )
                self.assertEqual(0 if shared == source == "success" else 1, result.returncode, result.stderr)

    def test_source_catalog_command_requires_nonempty_executed_success_and_retains_failure_xml(self):
        command = step(self.workflow["jobs"]["source-debug"], "Verify source-selected owner catalog contracts")["run"]
        self.assertIn("-class Hexalith.Memories.EventStore.Tests.MemoriesCatalogTests", command)
        counts = {"total": "3", "passed": "3", "failed": "0", "errors": "0", "skipped": "0", "not-run": "0"}

        def report(summary=counts, results=("Pass", "Pass", "Pass")):
            root = ET.Element("assemblies")
            assembly = ET.SubElement(root, "assembly", {"name": "SourceCatalog.Tests", **summary})
            collection = ET.SubElement(assembly, "collection", {"name": "SourceCatalog"})
            for index, result in enumerate(results):
                ET.SubElement(collection, "test", {
                    "name": f"SourceCatalog.Test{index}", "type": "SourceCatalog",
                    "method": f"Test{index}", "result": result, "time": "0.001",
                })
            return ET.tostring(root, encoding="unicode")

        valid = report()
        cases = [
            ("passed", valid, 0, True),
            ("missing", "__missing__", 0, False), ("empty", "<assemblies />", 0, False),
            ("zero", report({**counts, "total": "0", "passed": "0"}, ()), 0, False),
            ("underselected", report({**counts, "total": "2", "passed": "2"}, ("Pass", "Pass")), 0, False),
            ("malformed", "<assemblies", 0, False),
            ("test-process-failed", valid, 1, False),
            ("contradictory-passed", report({**counts, "passed": "0"}), 0, False),
            ("contradictory-total", report({**counts, "total": "4"}), 0, False),
            ("missing-test-records", report(results=()), 0, False),
            ("contradictory-test-results", report(results=("Pass", "Pass", "Skip")), 0, False),
            ("negative-counts", report({**counts, "total": "-1", "passed": "-1"}, ()), 0, False),
            ("nonnumeric-count", report({**counts, "passed": "invalid"}), 0, False),
        ]
        cases += [(f"missing-{key}", report({name: value for name, value in counts.items() if name != key}), 0, False)
                  for key in counts]
        cases += [(key, report({**counts, key: "1"}), 0, False)
                  for key in ("failed", "errors", "skipped", "not-run")]
        with tempfile.TemporaryDirectory(prefix="ci alignment ") as directory:
            root = Path(directory)
            binary = root / "dotnet"
            binary.write_text(
                "#!/usr/bin/env python3\nimport os, pathlib\n"
                "if os.environ['REPORT_XML'] != '__missing__':\n"
                "    pathlib.Path('TestResults/source-catalog.xml').write_text(os.environ['REPORT_XML'])\n"
                "raise SystemExit(int(os.environ['TEST_EXIT']))\n", encoding="utf-8",
            )
            binary.chmod(0o755)
            report = root / "TestResults/source-catalog.xml"
            for name, xml, exit_code, success in cases:
                with self.subTest(case=name):
                    report.unlink(missing_ok=True)
                    result = subprocess.run(
                        ["bash", "-e", "-c", command], cwd=root, text=True, capture_output=True, timeout=10,
                        env={**os.environ, "PATH": f"{root}{os.pathsep}{os.environ['PATH']}",
                             "REPORT_XML": xml, "TEST_EXIT": str(exit_code)},
                    )
                    self.assertEqual(success, result.returncode == 0, result.stdout + result.stderr)
                    if xml != "__missing__":
                        self.assertEqual(xml, report.read_text(), "Failure XML must remain downloadable")

    def test_nightly_benchmarks_and_explicit_pr_title_commitlint_remain_blocking(self):
        nightly = read_workflow("nightly.yml")
        self.assertIn("schedule", nightly["on"])
        self.assertIn("workflow_dispatch", nightly["on"])
        for name in ("integration-slow", "benchmark"):
            job = nightly["jobs"][name]
            for key in ("if", "continue-on-error", "needs"):
                self.assertNotIn(key, job)
        commitlint = read_workflow("commitlint.yml")
        job = commitlint["jobs"]["commitlint"]
        self.assertEqual("${{ github.event.pull_request.title }}", job["with"]["pull-request-title"])
        self.assertIn("edited", commitlint["on"]["pull_request"]["types"])
        for key in ("if", "continue-on-error"):
            self.assertNotIn(key, job)


if __name__ == "__main__":
    unittest.main()
