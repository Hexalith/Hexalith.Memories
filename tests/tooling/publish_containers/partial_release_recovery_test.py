"""Retained-byte recovery fixtures; these grant no live registry or NuGet credit."""
from contextlib import contextmanager
from copy import deepcopy
import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import io
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
import tarfile
from types import SimpleNamespace
import threading
import unittest
from unittest import mock
import urllib.error
import urllib.request
import zipfile

import publication_contract_test as fixtures

C = fixtures.CONTRACT
A = fixtures.ARCHIVE


def package_bytes(package_id, signed=False, content='retained'):
    data = io.BytesIO()
    with zipfile.ZipFile(data, 'w') as package:
        package.writestr(zipfile.ZipInfo(package_id + '.nuspec', (2026, 1, 1, 0, 0, 0)), f'<package><metadata><id>{package_id}</id><version>{fixtures.VERSION}</version></metadata></package>')
        package.writestr(zipfile.ZipInfo('lib/net11.0/fixture.dll', (2026, 1, 1, 0, 0, 0)), content)
        if signed:
            package.writestr(zipfile.ZipInfo('.signature.p7s', (2026, 1, 1, 0, 0, 0)), 'repository signature')
    return data.getvalue()


class NuGet:
    def __init__(self):
        self.packages, self.statuses, self.requests, self.pending, self.redirects = {}, {}, [], {}, {}
        owner = self
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                owner.requests.append(self.path)
                package_id = self.path.split('/')[1]
                expected = f'/{package_id}/{fixtures.VERSION}/{package_id}.{fixtures.VERSION}.nupkg'
                if self.path != expected or package_id != package_id.lower():
                    self.send_error(404, 'Unknown flat-container path')
                    return
                if package_id in owner.pending:
                    count, data = owner.pending[package_id]
                    if count == 0:
                        owner.packages[package_id] = data
                        del owner.pending[package_id]
                    else:
                        owner.pending[package_id] = (count - 1, data)
                status = owner.statuses.get(package_id, 200 if package_id in owner.packages else 404)
                self.send_response(status)
                if status == 302 and package_id in owner.redirects:
                    self.send_header('Location', owner.redirects[package_id])
                self.end_headers()
                if status == 200:
                    self.wfile.write(owner.packages[package_id])
            def log_message(self, *_):
                pass
        self.server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.url = f'http://127.0.0.1:{self.server.server_port}'

    def close(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()


class PartialReleaseRecoveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        fixtures.PublicationContractTests.setUpClass()

    @classmethod
    def tearDownClass(cls):
        fixtures.PublicationContractTests.tearDownClass()

    def setUp(self):
        self.f = fixtures.PublicationContractTests()
        self.f.setUp()
        self.addCleanup(self.f.doCleanups)
        self.root = self.f.root
        (self.f.shared.parents[1] / 'Props').mkdir()
        shutil.copyfile(type(self.f).shared.parents[1] / 'Props/ContainerProvenance.targets', self.f.shared.parents[1] / 'Props/ContainerProvenance.targets')
        identity = self.f.identity
        identity['helpers'] = {p.relative_to(self.f.shared.parents[1]).as_posix(): C.sha256(p) for p in [*(self.f.shared / name for name in C.SHARED_FILES), self.f.shared.parents[1] / 'Props/ContainerProvenance.targets']}
        identity['setSha256'] = hashlib.sha256(C.canonical({k: v for k, v in identity.items() if k != 'setSha256'})).hexdigest()
        C.write_json(self.f.evidence / 'identity.json', identity)
        self.f.prepare()
        package_directory = self.root / 'artifacts/packages/release'
        package_directory.mkdir(parents=True)
        self.ids = [p['id'] for p in C.read_json(self.f.manifest.with_name('release-packages.json'))['packages']]
        for package_id in self.ids:
            (package_directory / f'{package_id}.{fixtures.VERSION}.nupkg').write_bytes(package_bytes(package_id))
        C.write_package_identity(self.root, package_directory, self.f.evidence / 'identity.json')
        self.deployment = self.root / 'artifacts/deployment/hexalith-memories-production.yaml'
        self.deployment.parent.mkdir()
        shutil.copytree(fixtures.ROOT / 'deploy/kubernetes', self.root / 'deploy/kubernetes')
        with fixtures.working_directory(self.root):
            A.render_deployment(identity, self.deployment)
        self.bundle = self.root / 'release-recovery-bundle.tar.gz'
        self.rebundle()
        self.nuget = NuGet()
        self.addCleanup(self.nuget.close)
        self.output = self.root / 'recovery'
        self.arguments = SimpleNamespace(evidence_directory=self.output, version=fixtures.VERSION, registry='registry.test', release_run_id='304', recovery_bundle=self.bundle, source_root=self.root, repository='Hexalith/Hexalith.Memories', shared_directory=self.f.shared, nuget_base_uri=self.nuget.url, skopeo_command='skopeo', phase='recover', nuget_validation_timeout=0)
        self.assets, self.writes = {}, []
        self.release, self.draft, self.issue = False, False, True
        self.registry_status = None
        self.push_error = None
        self.delayed_package = None
        self.interrupted_image = None
        self.interrupted_release = False
        self.interrupted_create = False
        self.downloads = 0
        self.listed_issues = None
        self.unobservable_draft = False
        self.failed_run = {'id': 304, 'run_attempt': 1, 'head_sha': fixtures.SOURCE_SHA, 'path': '.github/workflows/release.yml', 'conclusion': 'failure', 'status': 'completed', 'repository': {'full_name': 'Hexalith/Hexalith.Memories'}}

    def rebundle(self):
        with fixtures.working_directory(self.root):
            self.manifest = A.archive_recovery_bundle(self.f.evidence, self.bundle, self.f.output, self.root / 'artifacts/packages/release', self.root / 'artifacts/release', self.deployment)
        self.assertEqual('recoverable', self.manifest['result'])

    def present_packages(self, count=10):
        for package_id in self.ids[:count]:
            path = self.root / 'artifacts/packages/release' / f'{package_id}.{fixtures.VERSION}.nupkg'
            data = io.BytesIO(path.read_bytes())
            with zipfile.ZipFile(data, 'a') as package:
                package.writestr('.signature.p7s', 'repository signature')
            self.nuget.packages[package_id.lower()] = data.getvalue()

    def present_images(self, count=5):
        for image in C.CANONICAL_IMAGES[:count]:
            self.f.remote_graphs[f'registry.test/{image["repository"]}:{fixtures.VERSION}'] = self.f.graphs[image['name']]

    def mark_write(self, command):
        plan = C.read_json(self.output / 'recovery-plan.json')
        self.assertEqual(15, len(plan['members']))
        self.assertEqual('pass', plan['result'])
        self.assertFalse(any(p['classification'] not in ('absent', 'present-identical') for p in plan['members']))
        self.writes.append([str(v) for v in command])

    def runner(self, command, log=None, **_):
        args = [str(value) for value in command]
        if args[0] == 'git':
            if 'show' in args:
                return (self.root / args[-1].split(':', 1)[1]).read_text()
            if 'ls-tree' in args:
                return '\n'.join(path.relative_to(self.root).as_posix() for path in (self.root / 'deploy/kubernetes').rglob('*') if path.is_file())
            return fixtures.SOURCE_SHA
        if args[:2] == ['gh', 'api']:
            return json.dumps(self.failed_run)
        if args[:3] == ['dotnet', 'nuget', 'push']:
            self.mark_write(args)
            self.assertNotIn('--skip-duplicate', args)
            self.assertEqual('temporary-key', args[args.index('--api-key') + 1])
            if self.push_error:
                raise ValueError(self.push_error)
            path = Path(args[3])
            package_id, _ = C.package_metadata(path)
            self.assertEqual(package_bytes(package_id), path.read_bytes())
            self.nuget.packages[package_id.lower()] = package_bytes(package_id, signed=True)
            if self.delayed_package == package_id:
                self.nuget.packages.pop(package_id.lower())
            return ''
        if args[0] == 'skopeo':
            self.mark_write(args)
            result = self.f.fake_run(command, log)
            if self.interrupted_image and args[-1].endswith('/' + self.interrupted_image + ':' + fixtures.VERSION):
                raise ValueError('fixture timeout after immutable manifest PUT')
            return result
        if args[0] == 'gh':
            if args[:2] == ['gh', 'release']:
                action = args[2]
                if action == 'download':
                    self.downloads += 1
                    for name, data in self.assets.items():
                        (Path(args[args.index('--dir') + 1]) / name).write_bytes(data)
                elif action in ('create', 'upload'):
                    self.mark_write(args)
                    self.release = True
                    if action == 'create':
                        self.assertIn('--draft', args)
                        self.assertFalse(any(Path(value).is_file() for value in args[4:]))
                        self.draft = True
                        if self.interrupted_create:
                            raise ValueError('fixture timeout after empty draft creation')
                    for value in args[4:]:
                        if Path(value).is_file():
                            self.assets[Path(value).name] = Path(value).read_bytes()
                            if self.interrupted_release is True or self.interrupted_release == Path(value).name:
                                raise ValueError('fixture timeout after immutable Release asset PUT')
                elif action == 'edit':
                    self.mark_write(args)
                    self.assertIn('--latest=false', args)
                    self.draft = False
                else:
                    raise AssertionError(args)
            elif args[:3] == ['gh', 'issue', 'list']:
                self.assertEqual('1000', args[args.index('--limit') + 1])
                self.assertEqual(f'in:title "PARTIAL PUBLISH {fixtures.VERSION} - manual reconciliation required"', args[args.index('--search') + 1])
                if self.listed_issues is not None:
                    return json.dumps(self.listed_issues)
                return json.dumps([{'number': 32, 'title': f'PARTIAL PUBLISH {fixtures.VERSION} - manual reconciliation required'}] if self.issue else [])
            elif args[:3] in (['gh', 'issue', 'comment'], ['gh', 'issue', 'close']):
                self.mark_write(args)
                if args[2] == 'close':
                    self.issue = False
            else:
                raise AssertionError(args)
            return ''
        return self.f.fake_run(command, log)

    @contextmanager
    def boundary(self):
        original_run = C.subprocess.run
        def native(command, **kwargs):
            if command[:3] == ['gh', 'release', 'view']:
                visible = self.release and not self.unobservable_draft
                return SimpleNamespace(returncode=0 if visible else 1, stdout=json.dumps({'tagName': 'v' + fixtures.VERSION, 'isDraft': self.draft, 'isPrerelease': False, 'assets': [{'name': n} for n in self.assets]}) if visible else '', stderr='' if visible else 'release not found')
            return original_run(command, **kwargs)
        def probe(_kind, repository, version):
            return self.registry_status or (200 if f'{repository}:{version}' in self.f.remote_graphs else 404)
        # The exact approved validator still parses all raw OCI objects; only the
        # authenticated registry transport, gh, dotnet and health process are fixtures.
        with self.f.execution(), mock.patch.object(C, 'run', side_effect=self.runner), mock.patch.object(C.subprocess, 'run', side_effect=native), mock.patch.object(self.f.preflight, 'destination_probe', return_value=probe), mock.patch.dict(os.environ, {'NUGET_API_KEY': 'temporary-key'}):
            yield

    def complete(self):
        with self.boundary():
            C.current_completion(self.arguments)

    def staged_powershell(self, publish=False):
        """Run the staged PS entry point and real CLI parser with controlled external I/O.

        A local process bridge keeps authenticated registry/process fixtures in this
        process; it forwards the exact staged adapter argv to its real main().
        """
        staging = self.root / 'staged'
        staging.mkdir(exist_ok=True)
        for name in ('complete-partial-release.ps1', 'container-publication-contract.py', 'release-packages.json', 'verify-container-health.py'):
            shutil.copyfile(fixtures.ROOT / 'tools' / name, staging / name)
        owner = self
        calls = []
        github_output = self.root / 'github-output.txt'
        github_output.unlink(missing_ok=True)
        class Bridge(BaseHTTPRequestHandler):
            def do_POST(self):
                argv = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
                calls.append(argv)
                owner.assertEqual(staging / 'container-publication-contract.py', Path(argv[0]))
                with mock.patch.object(sys, 'argv', argv):
                    code = C.main()
                self.send_response(200)
                self.end_headers()
                self.wfile.write(str(code).encode())
            def log_message(self, *_):
                pass
        server = ThreadingHTTPServer(('127.0.0.1', 0), Bridge)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        executable = staging / 'python3'
        executable.write_text(f'#!{sys.executable}\nimport json, sys, urllib.request\nrequest = urllib.request.Request("http://127.0.0.1:{server.server_port}", data=json.dumps(sys.argv[1:]).encode(), method="POST")\nsys.exit(int(urllib.request.urlopen(request).read()))\n')
        executable.chmod(0o755)
        command = [shutil.which('pwsh'), '-NoLogo', '-NoProfile', '-File', str(staging / 'complete-partial-release.ps1'), '-Version', fixtures.VERSION, '-SourceRoot', str(self.root), '-RecoveryBundle', str(self.bundle), '-ReleaseRunId', '304', '-SharedDirectory', str(self.f.shared), '-OutputDirectory', str(self.output), '-Repository', self.arguments.repository, '-Registry', 'registry.test', '-NuGetBaseUri', self.nuget.url]
        if publish:
            command.append('-Publish')
        try:
            with self.boundary():
                environment = dict(os.environ, PATH=str(staging) + os.pathsep + os.environ['PATH'], GITHUB_OUTPUT=str(github_output))
                # The bridge executes the real parser here and must see the same
                # output file as the staged child process.
                os.environ['GITHUB_OUTPUT'] = str(github_output)
                completed = subprocess.run(command, env=environment, capture_output=True, text=True, check=False, timeout=120)
            self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        finally:
            server.shutdown()
            server.server_close()
            thread.join()
        self.assertEqual(1, len(calls))
        self.assertEqual('recover' if publish else 'recovery-plan', calls[0][calls[0].index('--phase') + 1])
        return dict(line.split('=', 1) for line in github_output.read_text().splitlines()) if github_output.is_file() else {}

    def ordinary_packet(self):
        self.f.evidence_packet(prepared=True)
        path = self.root / 'artifacts/release' / C.PACKET_NAME
        with self.f.archive_context():
            A.archive(self.f.evidence, path, self.f.shared)
        self.rebundle()
        return path.read_bytes(), path.with_suffix(path.suffix + '.sha256.json').read_bytes()

    def rewrite_packet(self, mutate):
        with tarfile.open(fileobj=io.BytesIO(self.assets[C.PACKET_NAME])) as packet:
            files = {member.name: packet.extractfile(member).read() for member in packet}
        document = json.loads(files.pop('recovery-evidence.json'))
        mutate(document, files)
        document['files'] = [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()} for name, data in sorted(files.items())]
        files['recovery-evidence.json'] = C.canonical(document) + b'\n'
        path = self.root / 'changed-packet.tar.gz'
        fixtures.tar_bytes(path, files)
        self.assets[C.PACKET_NAME] = path.read_bytes()
        self.assets[C.PACKET_NAME + '.sha256.json'] = json.dumps({'path': C.PACKET_NAME, 'sha256': C.sha256(path), 'result': 'pass'}, indent=2, sort_keys=True).encode() + b'\n'

    def test_missing_retained_health_log_runs_fresh_health_before_package_writes(self):
        self.f.publish()
        (self.f.evidence / 'server/health-amd64.log').unlink()
        self.rebundle()
        self.f.commands.clear()
        original = self.runner
        def boundary(command, log=None, **kwargs):
            if [str(v) for v in command[:3]] == ['dotnet', 'nuget', 'push']:
                for image in C.CANONICAL_IMAGES:
                    self.assertTrue(C.passing_health(self.output / image['name'], image, self.f.graphs[image['name']], self.f.identity))
            return original(command, log=log, **kwargs)
        with mock.patch.object(self, 'runner', side_effect=boundary):
            self.complete()
        health = [command for command in self.f.commands if len(command) > 1 and command[1].endswith('verify-container-health.py')]
        self.assertEqual(1, len(health))
        self.assertEqual('server', health[0][health[0].index('--name') + 1])
        self.assertEqual(10, sum(command[:3] == ['dotnet', 'nuget', 'push'] for command in self.writes))

    def test_fresh_runner_reuses_verified_packet_records_without_health_or_destination_writes(self):
        self.complete()
        packet = self.assets[C.PACKET_NAME]
        shutil.rmtree(self.output)
        self.writes.clear()
        self.f.commands.clear()
        self.downloads = 0
        self.complete()
        self.assertEqual([], self.writes)
        self.assertFalse(any(len(command) > 1 and command[1].endswith('verify-container-health.py') for command in self.f.commands))
        self.assertEqual(packet, self.assets[C.PACKET_NAME])
        self.assertEqual(2, self.downloads)

    def test_custom_recovery_source_layout_is_explicitly_unrecoverable(self):
        custom = self.root / 'custom-packages'
        shutil.copytree(self.root / 'artifacts/packages/release', custom)
        with fixtures.working_directory(self.root):
            result = A.archive_recovery_bundle(self.f.evidence, self.bundle, self.f.output, custom, self.root / 'artifacts/release', self.deployment)
        self.assertEqual('unrecoverable', result['result'])
        self.assertTrue(any('Unsupported recovery source layout for packages' in reason for reason in result['reasons']))
        with self.assertRaisesRegex(ValueError, 'receipt'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_failed_run_attempt_drift_refuses_before_any_write(self):
        self.failed_run['run_attempt'] = 2
        with self.assertRaisesRegex(ValueError, 'frozen run attempt'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_existing_packet_enforces_environment_endpoint_and_no_lifecycle_writes(self):
        self.complete()
        original = dict(self.assets)
        for field, value in (('environment', 'Production'), ('lifecycleWritesEnabled', True), ('endpoint', '/wrong')):
            self.assets = dict(original)
            def drift(document, files):
                document['health'][0][field] = value
                files['records/server/health.json'] = C.canonical(document['health'][0])
            self.rewrite_packet(drift)
            self.writes.clear()
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, 'health proof'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_packet_retains_raw_recomputable_identity_and_refuses_record_tampering(self):
        self.complete()
        original = dict(self.assets)
        document, files = C.packet_files(self.assets[C.PACKET_NAME], 'recovery-evidence.json')
        identity = C.verify_frozen_identity(json.loads(files[C.IDENTITY_RECORD_FILES[0]]))
        self.assertEqual(document['setSha256'], identity['setSha256'])
        self.assertEqual(identity['manifestSha256'], hashlib.sha256(files['tools/release-containers.json']).hexdigest())
        self.assertEqual(10, len(json.loads(files['artifacts/release/package-identity.json'])['packages']))
        self.assertEqual(10, len(json.loads(files['tools/release-packages.json'])['packages']))
        for name in C.IDENTITY_RECORD_FILES:
            self.assets = dict(original)
            self.rewrite_packet(lambda _document, records: records.update({name: records[name] + b'\n'}))
            self.writes.clear()
            with self.subTest(record=name), self.assertRaisesRegex(ValueError, 'identity record differs'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_writer_during_archive_materialization_is_reclassified_before_copy(self):
        self.present_packages()
        self.present_images()
        image = C.CANONICAL_IMAGES[0]
        reference = f'registry.test/{image["repository"]}:{fixtures.VERSION}'
        original = C.materialize_oci_archive
        for changed in (False, True):
            self.f.remote_graphs.pop(reference, None)
            self.writes.clear()
            def writer(archive, destination):
                original(archive, destination)
                graph = deepcopy(self.f.graphs[image['name']])
                if changed:
                    raw = json.loads(graph['index_bytes'])
                    raw['annotations'] = {'fixture-writer': 'different'}
                    graph['index_bytes'] = C.canonical(raw)
                    graph['index_digest'] = 'sha256:' + hashlib.sha256(graph['index_bytes']).hexdigest()
                self.f.remote_graphs[reference] = graph
            with mock.patch.object(C, 'materialize_oci_archive', side_effect=writer):
                if changed:
                    with self.assertRaisesRegex(ValueError, 'after materialization'):
                        self.complete()
                    self.assertEqual([], self.writes)
                else:
                    self.complete()
                    self.assertFalse(any(command[0] == 'skopeo' for command in self.writes))

    def test_draft_creation_and_ordered_upload_interruptions_are_recoverable(self):
        self.present_packages()
        self.present_images()
        for interrupted in ('draft', C.PACKET_NAME, C.PACKET_NAME + '.sha256.json'):
            self.release, self.draft, self.issue = False, False, True
            self.assets = {}
            shutil.rmtree(self.output, ignore_errors=True)
            self.interrupted_create = interrupted == 'draft'
            self.interrupted_release = False if interrupted == 'draft' else interrupted
            self.writes.clear()
            with self.subTest(interrupted=interrupted), self.assertRaisesRegex(ValueError, 'timeout'):
                self.complete()
            self.assertTrue(self.draft)
            if C.PACKET_NAME + '.sha256.json' in self.assets:
                self.assertIn(C.PACKET_NAME, self.assets)
            self.interrupted_create, self.interrupted_release = False, False
            self.complete()
            self.assertEqual(13, len(self.assets))
            self.assertFalse(self.draft)
            uploads = [Path(command[-1]).name for command in self.writes if command[:3] == ['gh', 'release', 'upload']]
            self.assertLess(uploads.index(C.PACKET_NAME), uploads.index(C.PACKET_NAME + '.sha256.json'))
            self.writes.clear()
            self.complete()
            self.assertEqual([], self.writes)

    def test_staged_plan_emits_zero_absent_packages_when_all_are_present(self):
        self.present_packages()
        outputs = self.staged_powershell()
        self.assertEqual('0', outputs['absent-packages'])
        self.assertEqual([], self.writes)

    def test_staged_powershell_plans_fifteen_then_publish_completes_real_cli(self):
        outputs = self.staged_powershell()
        self.assertEqual('10', outputs['absent-packages'])
        plan = (self.output / 'recovery-plan.json').read_bytes()
        self.assertEqual([], self.writes)
        self.staged_powershell(publish=True)
        self.assertEqual(plan, (self.output / 'recovery-plan.json').read_bytes())
        summary = C.read_json(self.output / 'completion-summary.json')
        self.assertEqual('succeeded', summary['status'])
        self.assertEqual(15, len(summary['packages']) + len(summary['images']))
        self.assertEqual(13, len(self.assets))
        self.assertEqual(10, sum(w[:3] == ['dotnet', 'nuget', 'push'] for w in self.writes))

    def test_normal_packet_and_receipt_subsets_preserve_exact_ordinary_assets(self):
        packet, receipt = self.ordinary_packet()
        self.present_packages()
        for names in ((C.PACKET_NAME,), (C.PACKET_NAME + '.sha256.json',), (C.PACKET_NAME, C.PACKET_NAME + '.sha256.json')):
            with self.subTest(subset=names):
                self.release, self.draft = True, True
                expected = {C.PACKET_NAME: packet, C.PACKET_NAME + '.sha256.json': receipt}
                self.assets = {name: expected[name] for name in names}
                self.writes.clear()
                self.complete()
                self.assertEqual(packet, self.assets[C.PACKET_NAME])
                self.assertEqual(receipt, self.assets[C.PACKET_NAME + '.sha256.json'])
                self.assertEqual(13, len(self.assets))
                self.assertTrue((self.output / 'recovery-results.json').is_file())

    def test_ordinary_packet_refuses_each_missing_producer_record_after_consistent_rehash(self):
        packet, _ = self.ordinary_packet()
        with tarfile.open(fileobj=io.BytesIO(packet)) as archive:
            originals = {member.name: archive.extractfile(member).read() for member in archive}
        document = json.loads(originals['evidence-manifest.json'])
        required = {path.relative_to(self.root).as_posix() if path.is_absolute() else path.as_posix()
                    for path in A.required_files(self.f.evidence, self.f.output, self.root / 'artifacts/packages/release', self.root / 'artifacts/release', self.deployment)}
        required.add('artifacts/containers/release/production-deployment.yaml')
        self.assertEqual(129, len(required))
        authorization = 'artifacts/release/container-evidence/authorization.json'
        self.assertIn(authorization, required)
        changed = self.root / 'changed-ordinary.tar.gz'
        def without_record(name):
            files = {key: value for key, value in originals.items() if key not in (name, 'evidence-manifest.json')}
            manifest = dict(document, files=[entry for entry in document['files'] if entry['path'] != name])
            files['evidence-manifest.json'] = C.canonical(manifest) + b'\n'
            fixtures.tar_bytes(changed, files)
            return changed.read_bytes(), C.canonical({'path': C.PACKET_NAME, 'sha256': C.sha256(changed), 'result': 'pass'})
        for name in sorted(required):
            data, receipt = without_record(name)
            with self.subTest(missing=name), self.assertRaisesRegex(ValueError, 'incomplete|release identity'):
                C.validate_ordinary_packet(data, receipt, self.f.identity, self.root)
        data, receipt = without_record(authorization)
        path = self.root / 'artifacts/release' / C.PACKET_NAME
        path.write_bytes(data)
        path.with_suffix(path.suffix + '.sha256.json').write_bytes(receipt)
        self.rebundle()
        self.present_packages()
        self.release, self.draft = True, True
        self.assets = {C.PACKET_NAME: data, C.PACKET_NAME + '.sha256.json': receipt}
        with self.assertRaisesRegex(ValueError, 'incomplete'):
            self.complete()
        self.assertEqual([], self.writes)
        self.assertEqual({C.PACKET_NAME: data, C.PACKET_NAME + '.sha256.json': receipt}, self.assets)

    def test_recovery_bundle_cli_default_preserves_ordinary_packet_and_explicit_outputs(self):
        packet, receipt = self.ordinary_packet()
        ordinary = self.root / 'artifacts/release' / C.PACKET_NAME
        ordinary_receipt = ordinary.with_suffix(ordinary.suffix + '.sha256.json')
        def invoke(*arguments):
            result = subprocess.run([sys.executable, str(fixtures.ROOT / 'tools/archive-release-evidence.py'), *arguments], cwd=self.root, capture_output=True, text=True, timeout=120)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        invoke('--recovery-bundle')
        recovery = ordinary.with_name('release-recovery-bundle.tar.gz')
        recovery_receipt = recovery.with_suffix(recovery.suffix + '.sha256.json')
        self.assertEqual('recoverable', C.read_json(recovery_receipt)['result'])
        recovery_bytes = recovery.read_bytes(), recovery_receipt.read_bytes()
        explicit = self.root / 'explicit-recovery.tar.gz'
        invoke('--recovery-bundle', '--output', str(explicit))
        self.assertEqual('recoverable', C.read_json(explicit.with_suffix('.gz.sha256.json'))['result'])
        self.assertEqual((packet, receipt), (ordinary.read_bytes(), ordinary_receipt.read_bytes()))
        self.assertEqual(recovery_bytes, (recovery.read_bytes(), recovery_receipt.read_bytes()))
        with tarfile.open(recovery) as archive:
            self.assertEqual(packet, archive.extractfile('artifacts/release/' + C.PACKET_NAME).read())
            self.assertEqual(receipt, archive.extractfile('artifacts/release/' + C.PACKET_NAME + '.sha256.json').read())
        invoke('--diagnostics')
        self.assertEqual({'path': C.PACKET_NAME, 'sha256': C.sha256(ordinary), 'result': 'diagnostic-only'}, C.read_json(ordinary_receipt))
        normal_bytes = ordinary.read_bytes(), ordinary_receipt.read_bytes()
        explicit = self.root / 'explicit-ordinary.tar.gz'
        invoke('--diagnostics', '--output', str(explicit))
        self.assertEqual('diagnostic-only', C.read_json(explicit.with_suffix('.gz.sha256.json'))['result'])
        self.assertEqual(normal_bytes, (ordinary.read_bytes(), ordinary_receipt.read_bytes()))
        self.assertEqual(recovery_bytes, (recovery.read_bytes(), recovery_receipt.read_bytes()))

    def test_malformed_or_comment_only_deployment_refuses_before_destination_writes(self):
        for text in ('kind: [invalid YAML\n', '\n'.join('# registry.test/' + image['repository'] + ':' + fixtures.VERSION for image in C.CANONICAL_IMAGES[:4])):
            self.deployment.write_text(text)
            self.rebundle()
            with self.subTest(deployment=text), self.assertRaisesRegex(ValueError, 'tagged renderer'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_invalid_preparation_receipts_are_retained_as_unrecoverable(self):
        compatibility = self.f.evidence / 'archive-compatibility.json'
        package_identity = self.root / 'artifacts/release/package-identity.json'
        originals = {path: path.read_bytes() for path in (compatibility, package_identity)}
        for path, field, value in ((compatibility, 'result', 'fail'), (compatibility, 'setSha256', 'f' * 64), (package_identity, 'sourceSha', 'f' * 40)):
            document = C.read_json(path)
            document[field] = value
            C.write_json(path, document)
            with fixtures.working_directory(self.root):
                manifest = A.archive_recovery_bundle(self.f.evidence, self.bundle, self.f.output, self.root / 'artifacts/packages/release', self.root / 'artifacts/release', self.deployment)
            self.assertEqual('unrecoverable', manifest['result'])
            self.assertTrue(any('Invalid pre-publication preparation' in reason for reason in manifest['reasons']))
            with self.assertRaisesRegex(ValueError, 'receipt'):
                self.complete()
            path.write_bytes(originals[path])
        self.assertEqual([], self.writes)

    def test_success_packet_refuses_actual_package_hash_or_metadata_mismatch(self):
        self.f.evidence_packet(prepared=True)
        path = self.root / 'artifacts/packages/release' / f'{self.ids[0]}.{fixtures.VERSION}.nupkg'
        path.write_bytes(path.read_bytes() + b'changed after identity emission')
        with self.f.archive_context(), self.assertRaisesRegex(ValueError, 'package bytes or metadata'):
            A.archive(self.f.evidence, self.root / 'success.tar.gz', self.f.shared)

    def test_recovery_packet_raw_record_tampering_refuses_before_writes(self):
        self.complete()
        with tarfile.open(fileobj=io.BytesIO(self.assets[C.PACKET_NAME])) as packet:
            files = {member.name: packet.extractfile(member).read() for member in packet}
        files['records/server/health-amd64.log'] += b'tampered'
        changed = self.root / 'tampered.tar.gz'
        fixtures.tar_bytes(changed, files)
        self.assets[C.PACKET_NAME] = changed.read_bytes()
        self.writes.clear()
        with self.assertRaisesRegex(ValueError, 'record hash differs'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_failed_image_transport_preserves_initial_plan_and_actual_side_effect(self):
        self.present_packages()
        self.interrupted_image = 'memories'
        with self.assertRaisesRegex(ValueError, 'timeout'):
            self.complete()
        plan = C.read_json(self.output / 'recovery-plan.json')
        self.assertEqual('absent', plan['members'][10]['classification'])
        summary = C.read_json(self.output / 'completion-summary.json')
        self.assertEqual('failed', summary['status'])
        self.assertEqual('present-identical', summary['images'][0]['classification'])
        attempt = summary['sideEffects'][0]
        self.assertTrue(attempt['attempted'])
        self.assertTrue(attempt['verified'])
        self.assertFalse(attempt['uncertain'])
        self.assertIn('timeout', attempt['error'])

    def test_failed_package_push_preserves_partial_progress_and_uncertainty(self):
        self.present_images()
        self.present_packages(4)
        self.push_error = 'HTTP 409 fixture'
        with self.assertRaisesRegex(ValueError, '409'):
            self.complete()
        summary = C.read_json(self.output / 'completion-summary.json')
        self.assertEqual('failed', summary['status'])
        attempt = summary['sideEffects'][-1]
        self.assertEqual('package-push', attempt['operation'])
        self.assertTrue(attempt['attempted'])
        self.assertFalse(attempt['verified'])
        self.assertTrue(attempt['uncertain'])
        self.assertEqual('absent', attempt['classification'])
        self.assertIn('409', attempt['error'])
        self.push_error = 'fixture timeout during NuGet push'
        writes = len(self.writes)
        with self.assertRaisesRegex(ValueError, 'Prior NuGet package-push attempt remains unresolved'):
            self.complete()
        self.assertEqual(writes, len(self.writes))
        self.assertTrue(C.read_json(self.output / 'completion-summary.json')['sideEffects'][-1]['uncertain'])
        # Preserve the first-attempt transport-timeout control in its own bound directory.
        self.output = self.root / 'timeout-recovery'
        self.arguments.evidence_directory = self.output
        with self.assertRaisesRegex(ValueError, 'timeout during NuGet push'):
            self.complete()
        self.assertTrue(C.read_json(self.output / 'completion-summary.json')['sideEffects'][-1]['uncertain'])

    def test_failed_release_upload_reports_verified_members_and_partial_assets(self):
        self.present_packages()
        self.present_images()
        self.interrupted_release = True
        with self.assertRaisesRegex(ValueError, 'Release asset PUT'):
            self.complete()
        summary = C.read_json(self.output / 'completion-summary.json')
        self.assertEqual('failed', summary['status'])
        self.assertTrue(all(item['classification'] == 'present-identical' for item in summary['packages'] + summary['images']))
        self.assertEqual(1, len(summary['releaseAssets']))
        attempt = summary['sideEffects'][-1]
        self.assertEqual('release-asset-upload', attempt['operation'])
        self.assertEqual(1, len(attempt['verifiedAssets']))
        self.assertTrue(attempt['verified'])
        self.assertFalse(attempt['uncertain'])
        self.assertIn('timeout', attempt['error'])

    def test_partial_images_copy_only_two_retained_archives_and_completed_rerun_writes_nothing(self):
        self.present_packages()
        self.present_images(3)
        self.complete()
        self.assertEqual(2, sum(w[0] == 'skopeo' for w in self.writes))
        self.assertEqual(0, sum(w[:3] == ['dotnet', 'nuget', 'push'] for w in self.writes))
        self.assertEqual(13, len(self.assets))
        self.assertEqual('succeeded', C.read_json(self.output / 'completion-summary.json')['status'])
        self.assertEqual(5, sum(len(command) > 1 and command[1].endswith('verify-container-health.py') for command in self.f.commands))
        if os.environ.get('MEMORIES_RECOVERY_FIXTURE_EVIDENCE_DIRECTORY'):
            retained = Path(os.environ['MEMORIES_RECOVERY_FIXTURE_EVIDENCE_DIRECTORY']).resolve()
            retained.mkdir(parents=True, exist_ok=True)
            for path in (self.bundle, self.bundle.with_suffix(self.bundle.suffix + '.sha256.json'), self.output / 'recovery-plan.json', self.output / 'completion-summary.json', self.output / C.PACKET_NAME, self.output / (C.PACKET_NAME + '.sha256.json')):
                shutil.copyfile(path, retained / path.name)
        self.writes.clear()
        self.complete()
        self.assertEqual([], self.writes)

    def test_partial_packages_push_only_six_retained_nupkgs(self):
        self.present_packages(4)
        self.present_images()
        self.complete()
        self.assertEqual(6, sum(w[:3] == ['dotnet', 'nuget', 'push'] for w in self.writes))
        self.assertEqual(0, sum(w[0] == 'skopeo' for w in self.writes))

    def test_all_missing_positive_control_verifies_fifteen_and_thirteen_assets(self):
        self.complete()
        self.assertEqual(5, sum(w[0] == 'skopeo' for w in self.writes))
        self.assertEqual(10, sum(w[:3] == ['dotnet', 'nuget', 'push'] for w in self.writes))
        self.assertEqual(13, len(self.assets))

    def test_occupied_other_package_or_image_refuses_before_any_write(self):
        self.present_packages()
        self.present_images()
        self.nuget.packages[self.ids[0].lower()] = package_bytes(self.ids[0], content='other')
        with self.assertRaisesRegex(ValueError, 'present-different'):
            self.complete()
        self.assertEqual([], self.writes)
        self.nuget.packages[self.ids[0].lower()] = package_bytes(self.ids[0], signed=True)
        expected = self.f.graphs['server']
        self.f.runtime_overrides['server'] = dict(fixtures.RUNTIME, User='2000')
        self.f.fixture_archives(C.CANONICAL_IMAGES[0], self.root / 'other.tar.gz', self.root / 'other.oci.tar.gz')
        other = self.f.graphs['server']
        self.f.graphs['server'] = expected
        self.f.remote_graphs[f'registry.test/memories:{fixtures.VERSION}'] = other
        with self.assertRaisesRegex(ValueError, 'server=present-different'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_http_401_403_503_and_timeout_are_ambiguous_no_write(self):
        self.present_packages()
        self.present_images()
        for status in (302, 401, 403, 503):
            with self.subTest(registry=status):
                self.registry_status = status
                with self.assertRaisesRegex(ValueError, 'ambiguous'):
                    self.complete()
                self.assertEqual([], self.writes)
        self.registry_status = None
        target = NuGet()
        self.addCleanup(target.close)
        package_id = self.ids[0].lower()
        route = f'/{package_id}/{fixtures.VERSION}/{package_id}.{fixtures.VERSION}.nupkg'
        target.packages[package_id] = self.nuget.packages[package_id]
        self.nuget.redirects[package_id] = target.url + route
        self.nuget.statuses[package_id] = 302
        # Positive control: the Location really follows to an identical package
        # with the default opener, so accepting redirects would mask ambiguity.
        with urllib.request.urlopen(self.nuget.url + route) as response:
            self.assertEqual(target.packages[package_id], response.read())
        self.assertEqual([route], target.requests)
        target.requests.clear()
        original_packages = dict(self.nuget.packages)
        for status in (302, 401, 403, 503):
            with self.subTest(nuget=status):
                self.nuget.statuses[self.ids[0].lower()] = status
                with self.assertRaisesRegex(ValueError, 'ambiguous'):
                    self.complete()
                self.assertEqual([], self.writes)
                self.assertEqual([], target.requests)
                self.assertEqual(original_packages, self.nuget.packages)
        self.nuget.statuses.clear()
        with mock.patch.object(C, 'open_package', side_effect=TimeoutError('fixture timeout')), self.assertRaisesRegex(ValueError, 'ambiguous'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_409_push_refuses_without_release_completion_or_success_credit(self):
        self.present_images()
        self.present_packages(9)
        self.push_error = 'HTTP 409 conflict while NuGet package validates'
        with self.assertRaisesRegex(ValueError, '409'):
            self.complete()
        self.assertFalse(self.release)
        self.assertEqual('failed', C.read_json(self.output / 'completion-summary.json')['status'])
        self.assertTrue(C.read_json(self.output / 'completion-summary.json')['sideEffects'][-1]['uncertain'])

    def test_pending_flat_container_after_successful_push_has_no_completion_credit(self):
        self.present_images()
        self.delayed_package = C.read_json(self.root / 'artifacts/release/package-identity.json')['packages'][0]['id']
        with self.assertRaisesRegex(ValueError, 'NuGet validation deadline/readback'):
            self.complete()
        self.assertFalse(self.release)
        self.assertEqual('failed', C.read_json(self.output / 'completion-summary.json')['status'])
        self.assertTrue(C.read_json(self.output / 'completion-summary.json')['sideEffects'][-1]['uncertain'])
        self.assertEqual(1, sum(write[:3] == ['dotnet', 'nuget', 'push'] for write in self.writes))

    def test_unresolved_package_push_refuses_retry_until_positive_identical_readback(self):
        self.present_images()
        self.delayed_package = C.read_json(self.root / 'artifacts/release/package-identity.json')['packages'][0]['id']
        with self.assertRaisesRegex(ValueError, 'NuGet validation deadline/readback'):
            self.complete()
        self.writes.clear()
        with self.assertRaisesRegex(ValueError, 'Prior NuGet package-push attempt remains unresolved'):
            self.complete()
        self.assertEqual([], self.writes)
        self.nuget.packages[self.delayed_package.lower()] = package_bytes(self.delayed_package, signed=True)
        self.delayed_package = None
        self.complete()
        self.assertEqual(9, sum(write[:3] == ['dotnet', 'nuget', 'push'] for write in self.writes))
        attempts = C.read_json(self.output / 'completion-summary.json')['sideEffects']
        self.assertTrue(all(attempt['verified'] and not attempt['uncertain'] for attempt in attempts))

    def test_copy_failure_after_manifest_put_is_reconciled_by_read_only_graph_on_rerun(self):
        self.present_packages()
        self.interrupted_image = 'memories'
        with self.assertRaisesRegex(ValueError, 'after immutable manifest PUT'):
            self.complete()
        self.assertFalse(self.release)
        self.interrupted_image = None
        self.writes.clear()
        self.complete()
        self.assertEqual(4, sum(w[0] == 'skopeo' for w in self.writes))
        self.assertFalse(any(w[0] == 'skopeo' and '/memories:' in w[-1] for w in self.writes))

    def test_read_only_plan_stores_all_members_and_never_writes(self):
        self.present_images(3)
        self.present_packages(4)
        self.arguments.phase = 'recovery-plan'
        self.complete()
        plan = C.read_json(self.output / 'recovery-plan.json')
        self.assertEqual(15, len(plan['members']))
        self.assertEqual(6, plan['absentPackages'])
        self.assertEqual([], self.writes)

    def test_missing_nuget_temporary_login_refuses_before_writes(self):
        with self.boundary(), mock.patch.dict(os.environ, {'NUGET_API_KEY': ''}), self.assertRaisesRegex(ValueError, 'Trusted Publishing login required'):
            C.current_completion(self.arguments)
        self.assertEqual([], self.writes)

    def test_passing_retained_health_is_reused_without_new_health_process(self):
        self.f.publish()
        self.rebundle()
        self.present_packages()
        self.f.commands.clear()
        self.complete()
        self.assertFalse(any(len(command) > 1 and command[1].endswith('verify-container-health.py') for command in self.f.commands))

    def test_bundle_receipt_hash_source_run_version_inventory_and_builds_drift_refuse(self):
        self.present_packages()
        self.present_images()
        original = deepcopy(self.f.identity)
        for label, mutate in (
            ('set digest', lambda i: i.update(setSha256='f' * 64)),
            ('source', lambda i: i.update(sourceSha='f' * 40, workflowSha='f' * 40)),
            ('version', lambda i: i.update(version='9.9.9')),
            ('images', lambda i: i['images'].pop()),
            ('builds', lambda i: i.update(buildsSha='f' * 40)),
        ):
            changed = deepcopy(original)
            mutate(changed)
            if label != 'set digest':
                changed['setSha256'] = hashlib.sha256(C.canonical({k: v for k, v in changed.items() if k != 'setSha256'})).hexdigest()
            # Preserve a recoverable outer receipt and every file hash, so these
            # mutations exercise the consumer's frozen/bundle bindings.
            with tarfile.open(self.bundle) as archive:
                files = {member.name: archive.extractfile(member).read() for member in archive}
            manifest = json.loads(files.pop('recovery-manifest.json'))
            files['artifacts/release/container-evidence/identity.json'] = C.canonical(changed)
            manifest['files'] = [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()} for name, data in sorted(files.items())]
            files['recovery-manifest.json'] = C.canonical(manifest)
            fixtures.tar_bytes(self.bundle, files)
            C.write_json(self.bundle.with_suffix(self.bundle.suffix + '.sha256.json'), {'path': self.bundle.name, 'sha256': C.sha256(self.bundle), 'result': 'recoverable'})
            with self.subTest(drift=label), self.assertRaises((ValueError, FileNotFoundError)):
                self.complete()
            self.assertNotIn('receipt', C.read_json(self.output / 'completion-summary.json')['error'])
            self.assertIn('Bundle version/source/set' if label in ('source', 'version') else 'Frozen identity', C.read_json(self.output / 'completion-summary.json')['error'])
            self.assertEqual([], self.writes)
            self.rebundle()
        C.write_json(self.f.evidence / 'identity.json', original)
        self.rebundle()
        self.failed_run['head_sha'] = 'f' * 40
        with self.assertRaisesRegex(ValueError, 'Failed run'):
            self.complete()
        self.failed_run['head_sha'] = fixtures.SOURCE_SHA
        self.bundle.write_bytes(self.bundle.read_bytes() + b'tamper')
        with self.assertRaisesRegex(ValueError, 'receipt'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_workflow_metadata_and_image_phases_execute_without_package_credentials(self):
        self.present_packages(4)
        self.present_images(3)
        github_output = self.root / 'phase-output.txt'
        base = ['contract', '--version', fixtures.VERSION, '--registry', 'registry.test', '--release-run-id', '304', '--release-run-attempt', '1', '--source-root', str(self.root), '--recovery-bundle', str(self.bundle), '--shared-directory', str(self.f.shared), '--evidence-directory', str(self.output), '--nuget-base-uri', self.nuget.url]
        with self.boundary(), mock.patch.dict(os.environ, {'GITHUB_OUTPUT': str(github_output), 'NUGET_API_KEY': ''}):
            with mock.patch.object(C, 'load_shared', side_effect=AssertionError('metadata must not load helpers')), mock.patch.object(sys, 'argv', base + ['--phase', 'recovery-metadata']):
                self.assertEqual(0, C.main())
            self.assertIn('builds-sha=' + C.APPROVED_BUILDS_SHA, github_output.read_text())
            self.arguments.phase = 'recovery-plan'
            C.current_completion(self.arguments)
            plan = (self.output / 'recovery-plan.json').read_bytes()
            with mock.patch.object(sys, 'argv', base + ['--phase', 'recover-images']):
                self.assertEqual(0, C.main())
            self.assertEqual('images-verified', C.read_json(self.output / 'completion-summary.json')['status'])
            self.assertEqual(plan, (self.output / 'recovery-plan.json').read_bytes())
            self.assertEqual(2, sum(write[0] == 'skopeo' for write in self.writes))
            self.assertFalse(any(write[0] in ('dotnet', 'gh') for write in self.writes))

    def test_fresh_checkout_exposes_approved_catalog_for_real_msbuild_then_removes_alias(self):
        fresh = self.root / 'fresh-checkout'
        fresh.mkdir()
        for filename in ('global.json', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props'):
            shutil.copyfile(fixtures.ROOT / filename, fresh / filename)
        image = C.CANONICAL_IMAGES[0]
        project = fresh / image['project']
        project.parent.mkdir(parents=True)
        shutil.copyfile(fixtures.ROOT / image['project'], project)
        for target in (fixtures.ROOT / image['project']).parent.glob('*.targets'):
            shutil.copyfile(target, project.parent / target.name)
        approved = fixtures.approved_builds_checkout()
        catalog = fresh / '.hexalith/builds-source-proof'
        files = subprocess.check_output(['git', '-C', str(approved), 'ls-tree', '-r', '--name-only', C.APPROVED_BUILDS_SHA, '--', 'Props'], text=True).splitlines()
        for name in files:
            path = catalog / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(subprocess.check_output(['git', '-C', str(approved), 'show', f'{C.APPROVED_BUILDS_SHA}:{name}']))
        (fresh / 'references/Hexalith.Builds').mkdir(parents=True)  # checkout with submodules:false
        command = ['dotnet', 'msbuild', str(project), '-nologo', '-p:Configuration=Debug', '-p:UseHexalithProjectReferences=false', '-getProperty:HexalithVersionsLoaded']
        absent = subprocess.run(command, cwd=fresh, capture_output=True, text=True)
        self.assertNotEqual(0, absent.returncode)
        self.assertIn('MSB4019', absent.stdout + absent.stderr)
        self.assertIn('Hexalith.Builds/Props/Directory.Packages.props', absent.stdout + absent.stderr)
        environment = dict(os.environ, GITHUB_WORKSPACE=str(fresh))
        expose = self.workflow_run('recover-partial-release.yml', 'Expose approved Builds catalog for trusted fixtures')
        completed = subprocess.run(['bash', '-c', expose], cwd=fresh, env=environment, capture_output=True, text=True)
        self.assertEqual(0, completed.returncode, completed.stderr)
        self.assertEqual(catalog.resolve(), (fresh / 'references/Hexalith.Builds').resolve())
        present = subprocess.run(command, cwd=fresh, capture_output=True, text=True)
        self.assertEqual(0, present.returncode, present.stdout + present.stderr)
        self.assertEqual('true', present.stdout.strip())
        cleanup = self.workflow_run('recover-partial-release.yml', 'Remove trusted fixture catalog alias')
        subprocess.run(['bash', '-c', cleanup], cwd=fresh, env=environment, check=True)
        self.assertFalse((fresh / 'references/Hexalith.Builds').exists())
        workflow = (fixtures.ROOT / '.github/workflows/recover-partial-release.yml').read_text()
        self.assertLess(workflow.index('Expose approved Builds catalog'), workflow.index('Validate trusted recovery tooling'))
        self.assertLess(workflow.index('Validate trusted recovery tooling'), workflow.index('Remove trusted fixture catalog alias'))
        cleanup_step = workflow.split('      - name: Remove trusted fixture catalog alias\n', 1)[1].split('      - name:', 1)[0]
        self.assertIn("if: always() && steps.fixture-catalog.outcome == 'success'", cleanup_step)

    def test_existing_packet_plan_requires_bound_canonical_fifteen_member_identities(self):
        self.present_packages()
        self.present_images()
        self.complete()
        original = dict(self.assets)
        self.writes.clear()
        cases = [('duplicate', lambda plan: plan['members'].__setitem__(1, deepcopy(plan['members'][0]))),
                 ('unrelated', lambda plan: plan['members'][0].update(name='Other.Package')),
                 ('package file', lambda plan: plan['members'][0].update(file='Other.nupkg')),
                 ('image', lambda plan: plan['members'][10].update(image='registry.test/other:1.2.3')),
                 ('index digest', lambda plan: plan['members'][10].update(indexDigest='sha256:' + 'f' * 64))]
        cases += [(key, lambda plan, key=key: plan.update({key: 'unrelated'})) for key in ('version', 'sourceSha', 'setSha256')]
        for label, mutate in cases:
            self.assets = dict(original)
            self.rewrite_packet(lambda document, files: mutate(document['plan']))
            with self.subTest(plan=label), self.assertRaisesRegex(ValueError, 'passing complete plan'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_existing_packet_validator_metadata_must_match_recomputed_graph(self):
        self.present_packages()
        self.present_images()
        self.complete()
        original = dict(self.assets)
        self.writes.clear()
        for label, mutate in (
            ('image', lambda validation: validation.update(image='registry.test/other:1.2.3')),
            ('child platform', lambda validation: validation['children'][0].update(platform='linux/arm64')),
            ('child digest', lambda validation: validation['children'][0].update(digest='sha256:' + 'f' * 64)),
        ):
            self.assets = dict(original)
            def alter(document, files):
                name = 'records/server/oci-validation.json'
                validation = json.loads(files[name])
                mutate(validation)
                files[name] = C.canonical(validation)
                document['health'][0]['ociValidationSha256'] = hashlib.sha256(files[name]).hexdigest()
                files['records/server/health.json'] = C.canonical(document['health'][0])
            self.rewrite_packet(alter)
            with self.subTest(metadata=label), self.assertRaisesRegex(ValueError, 'metadata differs from the recomputed graph'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_nuget_validation_window_waits_read_only_and_publishes_each_package_once(self):
        self.present_packages(9)
        self.present_images()
        self.delayed_package = self.ids[-1]
        self.nuget.pending[self.ids[-1].lower()] = (12, package_bytes(self.ids[-1], signed=True))
        self.arguments.nuget_validation_timeout = 60
        with mock.patch.object(C.time, 'sleep') as waiting:
            self.complete()
        self.assertGreater(waiting.call_count, 0)
        self.assertEqual(1, sum(write[:3] == ['dotnet', 'nuget', 'push'] for write in self.writes))
        self.assertEqual('succeeded', C.read_json(self.output / 'completion-summary.json')['status'])
        for path in self.nuget.requests:
            package_id = path.split('/')[1]
            self.assertEqual(f'/{package_id}/{fixtures.VERSION}/{package_id}.{fixtures.VERSION}.nupkg', path)

    def test_flat_container_fake_rejects_wrong_version_filename_and_case(self):
        self.present_packages()
        package_id = self.ids[0].lower()
        for path in (f'/{package_id}/9.9.9/{package_id}.9.9.9.nupkg', f'/{package_id}/{fixtures.VERSION}/wrong.nupkg', f'/{self.ids[0]}/{fixtures.VERSION}/{package_id}.{fixtures.VERSION}.nupkg'):
            with self.subTest(path=path), self.assertRaises(urllib.error.HTTPError) as refused:
                urllib.request.urlopen(self.nuget.url + path).close()
            refused.exception.close()

    def test_current_incident_is_commented_then_closed_after_asset_verification(self):
        self.present_packages()
        self.present_images()
        self.complete()
        incident = [write for write in self.writes if write[:2] == ['gh', 'issue']]
        self.assertEqual(['comment', 'close'], [write[2] for write in incident])
        self.assertIn('setSha256=' + self.f.identity['setSha256'], incident[0][-1])
        self.assertEqual(13, len(self.assets))
        self.assertFalse(self.issue)

    def test_current_incident_multiple_matches_and_limit_are_named_refusals(self):
        self.present_packages()
        self.present_images()
        self.complete()
        self.writes.clear()
        title = f'PARTIAL PUBLISH {fixtures.VERSION} - manual reconciliation required'
        for count, diagnostic in ((2, 'Multiple open'), (1000, 'result limit')):
            self.listed_issues = [{'number': index, 'title': title} for index in range(count)]
            with self.subTest(count=count), self.assertRaisesRegex(ValueError, diagnostic):
                self.complete()
            self.assertEqual([], self.writes)

    def test_unobservable_created_draft_refuses_before_any_asset_upload(self):
        self.present_packages()
        self.present_images()
        self.unobservable_draft = True
        with self.assertRaisesRegex(ValueError, 'draft is not observable'):
            self.complete()
        self.assertEqual(['create'], [write[2] for write in self.writes if write[:2] == ['gh', 'release']])
        self.assertTrue(C.read_json(self.output / 'completion-summary.json')['sideEffects'][-1]['uncertain'])

    def workflow_run(self, filename, step_name):
        text = (fixtures.ROOT / '.github/workflows' / filename).read_text()
        block = text.split('      - name: ' + step_name + '\n', 1)[1].split('      - name:', 1)[0]
        match = re.search(r'^        run: (.+)$', block, re.MULTILINE)
        if match.group(1) == '|':
            return '\n'.join(line[10:] for line in block[match.end():].splitlines() if line.startswith('          '))
        return match.group(1)

    def test_failed_publish_producer_cli_workflow_download_and_recovery_complete_retained_bytes(self):
        # Execute the actual publication path and interrupt transport after a PUT.
        def interrupted(command, log=None):
            if str(command[0]) == 'skopeo' and len(self.f.remote_graphs) >= 2:
                raise ValueError('fixture release publisher lost transport before remaining PUTs')
            result = self.f.fake_run(command, log)
            if str(command[0]) == 'skopeo' and str(command[-1]).endswith('/memories-mcp:' + fixtures.VERSION):
                raise ValueError('fixture release publisher lost transport after immutable PUT')
            return result
        with self.f.execution(), mock.patch.object(C, 'run', side_effect=interrupted), self.assertRaisesRegex(ValueError, 'Container publication or verification failed'):
            C.shared_phase(self.f.shared, self.f.evidence, self.f.identity, 'publish')
            C.publish_archives(self.f.output, self.f.evidence, self.f.shared, self.f.identity, self.f.validator, 'skopeo')
        self.assertEqual(2, len(self.f.remote_graphs))
        self.assertIn(f'version {fixtures.VERSION} and release-run-id 304', C.read_json(self.f.output / 'publish-summary.json')['recovery'])
        for filename in ('archive-release-evidence.py', 'container-publication-contract.py'):
            shutil.copyfile(fixtures.ROOT / 'tools' / filename, self.root / 'tools' / filename)
        retain = self.workflow_run('release.yml', 'Retain prepared partial-release recovery bytes')
        produced = subprocess.run(shlex.split(retain), cwd=self.root, capture_output=True, text=True, timeout=120)
        self.assertEqual(0, produced.returncode, produced.stdout + produced.stderr)
        produced_bundle = self.root / 'artifacts/release/release-recovery-bundle.tar.gz'
        self.assertEqual('recoverable', C.read_json(produced_bundle.with_suffix('.gz.sha256.json'))['result'])
        upload = (fixtures.ROOT / '.github/workflows/release.yml').read_text().split('      - name: Upload immutable recovery bundle\n', 1)[1].split('      - name:', 1)[0]
        self.assertIn('if: failure()', upload)
        self.assertIn('compression-level: 0', upload)
        self.assertIn('retention-days: 90', upload)
        zipped = self.root / 'retained-artifact.zip'
        with zipfile.ZipFile(zipped, 'w') as archive:
            for path in (produced_bundle, produced_bundle.with_suffix('.gz.sha256.json')):
                archive.write(path, path.name)
        self.fake_workflow_api(zipped)
        download = self.workflow_run('recover-partial-release.yml', 'Download hash-bound failed-run recovery artifact').replace('${{ steps.recovery-tooling.outputs.path }}', str(self.root / 'tools'))
        completed = subprocess.run(['bash', '-c', download], cwd=self.root, env=self.workflow_environment, capture_output=True, text=True, timeout=120)
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.arguments.recovery_bundle = self.root / 'artifacts/recovery/bundle/release-recovery-bundle.tar.gz'
        self.present_packages(4)
        self.complete()
        self.assertEqual(3, sum(write[0] == 'skopeo' for write in self.writes))
        self.assertEqual(6, sum(write[:3] == ['dotnet', 'nuget', 'push'] for write in self.writes))
        self.assertEqual(13, len(self.assets))

    def fake_workflow_api(self, zipped=None):
        binary = self.root / 'workflow-bin'
        binary.mkdir(exist_ok=True)
        self.api = {'production': {'protection_rules': [{'type': 'required_reviewers', 'reviewers': [{'id': 1}]}], 'deployment_branch_policy': {'protected_branches': True}}, 'run': self.failed_run,
                    'artifacts': {'total_count': 1, 'artifacts': [{'id': 987, 'name': 'release-recovery-bundle', 'expired': False, 'digest': 'sha256:' + C.sha256(zipped) if zipped else ''}]}}
        self.api_path = self.root / 'workflow-api.json'
        C.write_json(self.api_path, self.api)
        gh = binary / 'gh'
        gh.write_text(f'#!{sys.executable}\nimport json, os, sys\nfrom pathlib import Path\napi=json.loads(Path(os.environ["FIXTURE_API"]).read_text())\nurl=sys.argv[-1]\nassert sys.argv[1]=="api",sys.argv\nif url.endswith("/zip"):\n sys.stdout.buffer.write(Path(os.environ["FIXTURE_ZIP"]).read_bytes())\nelif url.endswith("/production"):\n print(json.dumps(api["production"]))\nelif "/artifacts?" in url:\n print(json.dumps(api["artifacts"]))\nelse:\n print(json.dumps(api["run"]))\n')
        gh.chmod(0o755)
        git = binary / 'git'
        git.write_text(f'#!{sys.executable}\nimport sys\nif "rev-list" in sys.argv: print({fixtures.SOURCE_SHA!r})\nif "show" in sys.argv: print("env:\\n  KUBECTL_VERSION: \'v1.34.7\'")\n')
        git.chmod(0o755)
        self.workflow_environment = dict(os.environ, PATH=str(binary) + os.pathsep + os.environ['PATH'], FIXTURE_API=str(self.api_path), FIXTURE_ZIP=str(zipped or ''), RECOVERY_VERSION=fixtures.VERSION, RELEASE_RUN_ID='304', GITHUB_REPOSITORY=self.arguments.repository, GITHUB_OUTPUT=str(self.root / 'gate-output'))
        shutil.copyfile(fixtures.ROOT / 'tools/container-publication-contract.py', self.root / 'tools/container-publication-contract.py')

    def test_executed_workflow_production_failed_run_and_artifact_gates_have_named_refusals(self):
        self.fake_workflow_api()
        production = self.workflow_run('recover-partial-release.yml', 'Require production environment approval rule')
        # Execute the exact CLI used by both protection readbacks.
        result = subprocess.run(['bash', '-c', production], cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stderr)
        self.api['production']['protection_rules'] = []
        C.write_json(self.api_path, self.api)
        result = subprocess.run(['bash', '-c', production], cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Production approval rule', result.stderr)
        self.api['production']['protection_rules'] = [{'type': 'required_reviewers', 'reviewers': [{'id': 1}]}]
        for label, policy in (('missing', {}), ('null', {'deployment_branch_policy': None})):
            self.api['production'].pop('deployment_branch_policy', None)
            self.api['production'].update(policy)
            C.write_json(self.api_path, self.api)
            result = subprocess.run(['bash', '-c', production], cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
            with self.subTest(branch_policy=label):
                self.assertNotEqual(0, result.returncode)
                self.assertIn('Production approval rule or deployment branch policy missing', result.stderr)
        source_job = (fixtures.ROOT / '.github/workflows/recover-partial-release.yml').read_text().split('  release-source:\n', 1)[1].split('  recover:\n', 1)[0]
        upload = source_job.split('      - name: Upload release source refusal evidence\n', 1)[1]
        self.assertIn('if: always()', upload)
        self.assertIn('name: partial-release-source-evidence-v${{ inputs.version }}', upload)
        self.assertIn('path: artifacts/recovery/protection/**', upload)
        self.assertLess(source_job.index('Require production environment approval rule'), source_job.index('Upload release source refusal evidence'))
        command = ['python3', 'tools/container-publication-contract.py', '--phase', 'failed-run-gate', '--version', fixtures.VERSION, '--release-run-id', '304', '--tag-commit', fixtures.SOURCE_SHA, '--repository', self.arguments.repository]
        result = subprocess.run(command, cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn('kubectl-version=v1.34.7', (self.root / 'gate-output').read_text())
        for key, wrong, reason in (('path', '.github/workflows/ci.yml', 'workflow'), ('conclusion', 'cancelled', 'completed failure'), ('head_sha', 'f' * 40, 'tag head SHA'), ('run_attempt', 0, 'run attempt')):
            self.api['run'] = dict(self.failed_run, **{key: wrong})
            C.write_json(self.api_path, self.api)
            result = subprocess.run(command, cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn('Failed release run proof differs: ' + reason, result.stderr)
        download = ['python3', 'tools/container-publication-contract.py', '--phase', 'download-bundle', '--version', fixtures.VERSION, '--release-run-id', '304', '--repository', self.arguments.repository]
        for artifacts, reason in (([], 'missing/expired or ambiguous'), ([{'name': 'release-recovery-bundle', 'expired': True}], 'missing/expired or ambiguous'), ([{'name': 'release-recovery-bundle', 'expired': False}] * 2, 'missing/expired or ambiguous')):
            self.api['artifacts'] = {'artifacts': artifacts}
            C.write_json(self.api_path, self.api)
            result = subprocess.run(download, cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn(reason, result.stderr)
        zipped = self.root / 'bad-digest.zip'
        with zipfile.ZipFile(zipped, 'w') as archive:
            archive.writestr('unexpected', 'bytes')
        self.workflow_environment['FIXTURE_ZIP'] = str(zipped)
        self.api['artifacts'] = {'artifacts': [{'id': 987, 'name': 'release-recovery-bundle', 'expired': False, 'digest': 'sha256:' + 'f' * 64}]}
        C.write_json(self.api_path, self.api)
        result = subprocess.run(download, cwd=self.root, env=self.workflow_environment, capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('artifact digest mismatch', result.stderr)
        self.assertEqual([], self.writes)

    def test_missing_preparation_retains_named_unrecoverable_bundle(self):
        self.bundle.unlink()
        (self.f.output / 'server.oci.tar.gz').unlink()
        with fixtures.working_directory(self.root):
            manifest = A.archive_recovery_bundle(self.f.evidence, self.bundle, self.f.output, self.root / 'artifacts/packages/release', self.root / 'artifacts/release', self.deployment)
        self.assertEqual('unrecoverable', manifest['result'])
        self.assertTrue(any('server.oci.tar.gz' in reason for reason in manifest['reasons']))
        with self.assertRaisesRegex(ValueError, 'receipt'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_draft_hash_identical_subset_completes_then_publishes(self):
        self.present_packages()
        self.present_images()
        self.release, self.draft = True, True
        filename = f'{self.ids[0]}.{fixtures.VERSION}.nupkg'
        self.assets[filename] = package_bytes(self.ids[0])
        self.complete()
        self.assertFalse(self.draft)
        self.assertEqual(13, len(self.assets))
        self.assertEqual(1, sum(w[:3] == ['gh', 'release', 'edit'] for w in self.writes))

    def test_unexpected_or_different_draft_asset_refuses_before_writes(self):
        self.present_images()
        for name in ('unexpected.bin', f'{self.ids[0]}.{fixtures.VERSION}.nupkg'):
            self.release, self.draft = True, True
            self.assets = {name: b'other'}
            with self.subTest(asset=name), self.assertRaisesRegex(ValueError, 'unexpected|did not match'):
                self.complete()
            self.assertEqual([], self.writes)

    def test_immediate_destination_recheck_refuses_changed_member(self):
        self.present_packages()
        with self.boundary(), mock.patch.object(C, 'classify_image', wraps=C.classify_image) as probe:
            original = probe._mock_wraps
            calls = 0
            def raced(*args):
                nonlocal calls
                calls += 1
                result = original(*args)
                if calls == 6:
                    result['classification'] = 'present-different'
                return result
            probe.side_effect = raced
            with self.assertRaisesRegex(ValueError, 'Immediate image recheck'):
                C.current_completion(self.arguments)
        self.assertEqual([], self.writes)

    def test_package_destination_changes_before_push_refuse_or_skip_without_mutation(self):
        self.present_packages()
        self.present_images()
        package_id = self.ids[0]
        key = package_id.lower()
        original = C.classify_package
        for classification in ('present-different', 'ambiguous', 'present-identical'):
            with self.subTest(classification=classification):
                self.nuget.packages.pop(key, None)
                self.nuget.statuses.clear()
                self.writes.clear()
                calls, remote = 0, None
                def raced(item, *arguments):
                    nonlocal calls, remote
                    if item['id'] == package_id:
                        calls += 1
                        if calls == 2:
                            self.assertEqual('absent', C.read_json(self.output / 'recovery-plan.json')['members'][0]['classification'])
                            self.nuget.packages[key] = package_bytes(package_id, signed=True, content='retained' if classification == 'present-identical' else 'competing remote bytes')
                            if classification == 'ambiguous':
                                self.nuget.statuses[key] = 503
                            remote = dict(self.nuget.packages)
                    return original(item, *arguments)
                with self.boundary(), mock.patch.object(C, 'classify_package', side_effect=raced):
                    if classification == 'present-identical':
                        C.current_completion(self.arguments)
                    else:
                        with self.assertRaisesRegex(ValueError, 'Immediate NuGet recheck.*' + classification):
                            C.current_completion(self.arguments)
                        self.assertEqual([], self.writes)
                self.assertGreaterEqual(calls, 2)
                self.assertEqual(classification, C.read_json(self.output / 'packages' / (package_id + '.before-push.json'))['classification'])
                self.assertEqual([], [write for write in self.writes if write[:3] == ['dotnet', 'nuget', 'push']])
                self.assertEqual(remote, self.nuget.packages)

    def test_recovery_helper_gate_accepts_only_recorded_sha_and_hashes(self):
        with mock.patch.object(C.subprocess, 'run', return_value=SimpleNamespace(stdout='f' * 40)):
            with self.assertRaisesRegex(ValueError, 'approved Builds'):
                C.load_shared(self.f.shared, recovery_identity=self.f.identity)
        altered = deepcopy(self.f.identity)
        altered['helpers'] = {}
        altered['setSha256'] = hashlib.sha256(C.canonical({k: v for k, v in altered.items() if k != 'setSha256'})).hexdigest()
        with self.assertRaisesRegex(ValueError, 'helper hashes'):
            C.verify_frozen_identity(altered)

    def test_every_retained_file_hash_is_checked_even_with_valid_bundle_receipt(self):
        with tarfile.open(self.bundle) as archive:
            files = {member.name: archive.extractfile(member).read() for member in archive}
        name = next(name for name in files if name.endswith('.nupkg'))
        files[name] += b'changed retained bytes'
        fixtures.tar_bytes(self.bundle, files)
        C.write_json(self.bundle.with_suffix(self.bundle.suffix + '.sha256.json'), {'path': self.bundle.name, 'sha256': C.sha256(self.bundle), 'result': 'recoverable'})
        with self.assertRaisesRegex(ValueError, 'file hash differs'):
            self.complete()
        self.assertEqual([], self.writes)

    def test_approved_recorded_helper_bytes_load_and_changed_hash_refuses(self):
        names = ('oci_registry_validator', 'smoke_container_platforms')
        previous = {name: sys.modules.pop(name, None) for name in names}
        def restore_modules():
            for name in names:
                sys.modules.pop(name, None)
            sys.modules.update({name: module for name, module in previous.items() if module is not None})
        self.addCleanup(restore_modules)
        def git(command, **kwargs):
            if 'rev-parse' in command:
                return SimpleNamespace(stdout=C.APPROVED_BUILDS_SHA)
            relative = command[-1].split(':', 1)[1]
            return SimpleNamespace(stdout=(self.f.shared.parents[1] / relative).read_bytes())
        with mock.patch.object(C.subprocess, 'run', side_effect=git):
            loaded = C.load_shared(self.f.shared, recovery_identity=self.f.identity)
            self.assertEqual(self.f.identity['helpers'], loaded[-1])
            altered = deepcopy(self.f.identity)
            altered['helpers']['Github/publish-containers/action.yml'] = 'f' * 64
            altered['setSha256'] = hashlib.sha256(C.canonical({k: v for k, v in altered.items() if k != 'setSha256'})).hexdigest()
            with self.assertRaisesRegex(ValueError, 'helper hashes differ'):
                C.load_shared(self.f.shared, recovery_identity=altered)


if __name__ == '__main__':
    unittest.main()
