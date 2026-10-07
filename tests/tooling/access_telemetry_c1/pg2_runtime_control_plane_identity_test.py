"""Offline PG2 C1.15 capture tests; fake observations grant no gate credit."""
import copy
import hashlib
import json
import os
import re
import shutil
import stat
import subprocess
import tempfile
import time
import unittest
from datetime import datetime
from pathlib import Path

from runtime_control_plane_identity_test import FIXTURE, REPO_ROOT, TOKEN_CANARY, write_fake_kubectl

RUNNER = REPO_ROOT / 'tools/verify-access-telemetry-c1.ps1'
PROFILE_HELPER = REPO_ROOT / 'tools/access-telemetry-c1-profile.ps1'
SOURCE_PATHS = ('tools/verify-access-telemetry-c1.ps1', 'tools/access-telemetry-c1-profile.ps1',
                'tools/access-telemetry-c1-component-backend.ps1')
INPUT_PATHS = re.findall(r"'([^']+)' = '[0-9a-f]{64}'", PROFILE_HELPER.read_text())
WORKLOAD_ID = 'adr-27.1-two-writer-500eps'
WORKLOAD_HASH = '71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f'
PROFILE_IDENTITY = 'postgresql-v2-dapr-1.18.1-postgresql-18.6-onprem-k8s1-openebs-local-retain-400g-v2'
TARGET = {'context': 'jpiquot@local', 'namespace': 'hexalith-memories',
          'selector': 'app.kubernetes.io/name=memories-access-telemetry',
          'appId': 'memories-access-telemetry', 'actorType': 'AccessTelemetryLifecycleActor'}
PROFILE_HASH = '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
INDEX = 'b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8'
CHILD = 'edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b'
SESSION = 'pg2-offline-20261007'


def digest(value):
    return hashlib.sha256(value).hexdigest()


def json_digest(value):
    return digest(json.dumps(value, separators=(',', ':'), ensure_ascii=False).encode())


class Pg2RuntimeControlPlaneIdentityTests(unittest.TestCase):
    def setUp(self):
        self.base = json.loads(FIXTURE.read_text())
        self.pod = self.base['pods']['items'][0]['metadata']['name']

    def temporary_repository(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        for relative in (*SOURCE_PATHS, *INPUT_PATHS, '.gitattributes'):
            target = root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(REPO_ROOT / relative, target)
        # Only this disposable fixture gets a baseline; the workspace is never staged or committed.
        message = 'test: establish disposable source fixture\n'
        message_file = root / 'fixture-message.txt'
        message_file.write_text(message)
        lint_command = [str(REPO_ROOT / 'node_modules/.bin/commitlint'),
                        '--config', str(REPO_ROOT / 'commitlint.config.mjs'), '--edit', str(message_file)]
        subprocess.run(lint_command, cwd=REPO_ROOT, check=True, capture_output=True)
        message_file.unlink()
        for command in (['git', 'init', '-q'], ['git', 'add', '.'],
                        ['git', '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid',
                         '-c', 'commit.gpgsign=false', 'commit', '-qm', message.strip()]):
            subprocess.run(command, cwd=root, check=True, capture_output=True)
        message_file.write_bytes(subprocess.check_output(['git', 'log', '-1', '--format=%B'], cwd=root))
        subprocess.run(lint_command, cwd=REPO_ROOT, check=True, capture_output=True)
        message_file.unlink()
        return root

    def run_gate(self, scenario=None, *, session=SESSION, gate='C1.15', profile='PG-ONPREM-2',
                 opt_in=False, timeout=30, runner=RUNNER, repeat=1, extra=(),
                 cwd=REPO_ROOT, evidence_argument=None, git_hook=None):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        fake_bin = root / 'bin'
        fake_bin.mkdir()
        write_fake_kubectl(fake_bin)
        fake = fake_bin / 'fake_kubectl.py'
        source = fake.read_text()
        # Reuse the existing exact argument checks and add bounded transport/source-race controls.
        hook = '''
if args == ["config", "current-context"]:
    purpose = "current-context"
elif "get" in args:
    purpose = "pods-recheck" if any("get" in call for call in prior_calls) else "pods"
elif "/daprd" in args:
    purpose = "version"
elif "/v1.0/metadata" in args[-1]:
    purpose = "metadata"
else:
    purpose = "alpha"
if scenario.get("mutateSource") and purpose == "pods-recheck":
    changed = Path(scenario["mutateSource"])
    changed.write_bytes(changed.read_bytes() + b"\\n# mutation during capture\\n")
if scenario.get("hangPurpose") == purpose:
    time.sleep(5)
if scenario.get("rawBytesPurpose") == purpose:
    sys.stdout.buffer.write(b"\\xff")
    raise SystemExit(0)
if scenario.get("excessPurpose") == purpose:
    print("x" * (1024 * 1024 + 32))
    raise SystemExit(0)
if scenario.get("stderrPurpose") == purpose:
    print(scenario["stderr"], file=sys.stderr)
if scenario.get("failPurpose") == purpose:
    print(scenario.get("stderr", "observation refused"), file=sys.stderr)
    raise SystemExit(71)
'''
        source = source.replace('if args == ["config", "current-context"]:', hook + '\nif args == ["config", "current-context"]:', 1)
        fake.write_text(source)
        if git_hook is not None:
            real_git = shutil.which('git')
            fake_git = fake_bin / 'git'
            fake_git.write_text('#!/usr/bin/env python3\n'
                                'import os, subprocess, sys, time\nfrom pathlib import Path\n'
                                + git_hook + '\nos.execv(' + repr(real_git)
                                + ', [' + repr(real_git) + ', *sys.argv[1:]])\n')
            fake_git.chmod(0o755)
        scenario_path = root / 'scenario.json'
        scenario_path.write_text(json.dumps(self.base if scenario is None else scenario))
        log = root / 'calls.jsonl'
        evidence = root / 'evidence'
        environment = os.environ | {'PATH': str(fake_bin) + os.pathsep + os.environ.get('PATH', ''),
                                    'C1_SCENARIO': str(scenario_path), 'C1_KUBECTL_LOG': str(log),
                                    'DAPR_API_TOKEN': TOKEN_CANARY}
        command = ['pwsh', str(runner), '-Gate', gate, '-ProfileId', profile,
                   '-EvidenceDirectory', str(evidence) if evidence_argument is None else evidence_argument,
                   '-CommandTimeoutSeconds', str(timeout)]
        if session is not None:
            command.extend(['-QualificationSessionId', session])
        if opt_in:
            command.append('-AllowHistoricalProfileCapture')
        command.extend(extra)
        for _ in range(repeat):
            result = subprocess.run(command, cwd=cwd, env=environment, text=True, capture_output=True,
                                    check=False, timeout=max(15, timeout + 8))
            if result.returncode:
                break
        paths = sorted(evidence.glob('*.json'))
        packets = [json.loads(path.read_text()) for path in paths]
        calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
        retained = result.stdout + result.stderr + ''.join(path.read_text() for path in paths)
        self.assertNotIn(TOKEN_CANARY, retained)
        return result, packets, calls, evidence

    def assert_blocked(self, scenario, failure, **kwargs):
        result, packets, calls, evidence = self.run_gate(scenario, **kwargs)
        self.assertNotEqual(0, result.returncode, result.stderr)
        self.assertEqual(1, len(packets), result.stderr)
        packet = packets[0]
        self.assertEqual('blocked', packet['producerStatus'])
        self.assertIn(failure, packet['blockers'])
        self.assertEqual([], packet['observations']['pods'])
        self.assertEqual([], packet['observations']['runtimeVersions'])
        self.assertEqual({'componentIsAlpha': None, 'allowAlphaComponent': None}, packet['observations']['alphaOptIn'])
        self.assertEqual(0, packet['resultCount'])
        self.assertGreater(packet['failureCount'], 0)
        self.assertEqual(0, packet['skipCount'])
        self.assertEqual('not-evaluated', packet['gateStatus'])
        self.assertFalse(packet['productionGatePassed'])
        self.assertEqual('pending', packet['independentDisposition'])
        self.assertEqual(1, packet['producer']['exitCode'])
        self.assertTrue(all(not path.stat().st_mode & stat.S_IWUSR for path in evidence.glob('*.json')))
        return packet, calls

    def test_index_child_bare_and_two_pods_are_immutable_neutral_v2_captures(self):
        for image in ('docker-pullable://ghcr.io/dapr/daprd@sha256:' + INDEX,
                      'containerd://sha256:' + CHILD, 'sha256:' + INDEX, 'sha256:' + CHILD):
            for count in (1, 2):
                with self.subTest(image=image, count=count):
                    scenario = copy.deepcopy(self.base)
                    scenario['pods']['items'][0]['status']['containerStatuses'][1]['imageID'] = image
                    if count == 2:
                        second = copy.deepcopy(scenario['pods']['items'][0])
                        second['metadata'].update(name=self.pod + '-second', uid='other-safe-pod-uid')
                        scenario['pods']['items'].append(second)
                        for field in ('metadata', 'daprdVersions', 'alphaOptIn'):
                            scenario[field][second['metadata']['name']] = copy.deepcopy(scenario[field][self.pod])
                    result, packets, calls, evidence = self.run_gate(scenario)
                    self.assertEqual(0, result.returncode, result.stderr)
                    packet = packets[0]
                    self.assertEqual('hexalith.access-telemetry.c1.evidence/v2', packet['schemaVersion'])
                    self.assertEqual('observed', packet['producerStatus'])
                    self.assertEqual(PROFILE_IDENTITY, packet['profileIdentity'])
                    self.assertEqual(TARGET, packet['target'])
                    self.assertEqual(PROFILE_HASH, packet['profileSha256'])
                    self.assertEqual(json_digest(TARGET), packet['targetSha256'])
                    self.assertEqual(WORKLOAD_ID, packet['workloadId'])
                    self.assertEqual(WORKLOAD_HASH, packet['workloadSha256'])
                    self.assertEqual(SESSION, packet['qualificationSessionId'])
                    self.assertEqual(count, packet['resultCount'])
                    self.assertEqual(count, len(packet['observations']['pods']))
                    self.assertEqual(['1.18.1'], packet['observations']['runtimeVersions'])
                    self.assertEqual([image], packet['observations']['sidecarImageIds'])
                    self.assertEqual(0, packet['failureCount'])
                    self.assertEqual(0, packet['skipCount'])
                    self.assertEqual([], packet['blockers'])
                    self.assertEqual('not-evaluated', packet['gateStatus'])
                    self.assertFalse(packet['productionGatePassed'])
                    self.assertEqual('not-evaluated', packet['productionLifecycleWrites'])
                    self.assertEqual('pending', packet['independentDisposition'])
                    self.assertEqual('unchanged', packet['finalSourceRecheck'])
                    self.assertEqual(3 + count * 3, len(calls))
                    self.assertFalse(next(evidence.glob('*.json')).stat().st_mode & stat.S_IWUSR)

    def test_full_source_and_effective_invocation_and_child_receipts_are_recomputable(self):
        result, packets, calls, evidence = self.run_gate(repeat=2)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets))
        self.assertEqual(2, len({path.name for path in evidence.glob('*.json')}))
        packet = packets[0]
        self.assertEqual(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=REPO_ROOT, text=True).strip(), packet['sourceCommit'])
        expected_dirty = bool(subprocess.check_output(
            ['git', 'status', '--porcelain=v1', '--untracked-files=all'], cwd=REPO_ROOT))
        self.assertEqual(expected_dirty, packet['worktreeDirty'])
        expected_disposition = 'dirty-development' if expected_dirty or any(
            source['modified'] or not source['matchesHead'] for source in packet['producerSources']) else 'clean'
        self.assertEqual(expected_disposition, packet['sourceDisposition'])
        self.assertEqual(19, len(packet['producerSources']))
        self.assertEqual(set((*SOURCE_PATHS, *INPUT_PATHS)), {entry['source'] for entry in packet['producerSources']})
        self.assertEqual(WORKLOAD_ID, packet['workloadId'])
        self.assertEqual(WORKLOAD_HASH, packet['workloadSha256'])
        for source in packet['producerSources']:
            raw = (REPO_ROOT / source['source']).read_bytes()
            self.assertEqual(digest(raw), source['executedBytesSha256'])
            self.assertEqual(digest(raw.replace(b'\r\n', b'\n')), source['gitNormalizedSha256'])
            self.assertIsInstance(source['modified'], bool)
            self.assertIsInstance(source['matchesHead'], bool)
        self.assertEqual(PROFILE_IDENTITY, packet['profileIdentity'])
        self.assertEqual(TARGET, packet['target'])
        self.assertEqual(PROFILE_HASH, packet['profileSha256'])
        self.assertEqual(json_digest(TARGET), packet['targetSha256'])
        self.assertEqual(json_digest(packet['producer']['arguments']), packet['producer']['argumentsSha256'])
        self.assertIn(str(evidence), packet['producer']['arguments'])
        self.assertIn(SESSION, packet['producer']['arguments'])
        for entry, call in zip(packet['commands'], calls[:6], strict=True):
            self.assertEqual(call, entry['arguments'])
            self.assertEqual(json_digest(call), entry['argumentsSha256'])
            self.assertEqual(digest(('kubectl ' + '\x1f'.join(call)).encode()), entry['sha256'])
        for entry in packet['commands'] + packet['sourceCommands']:
            self.assertLessEqual(datetime.fromisoformat(packet['producer']['startedAtUtc']), datetime.fromisoformat(entry['startedAtUtc']))
            self.assertLessEqual(datetime.fromisoformat(entry['finishedAtUtc']), datetime.fromisoformat(packet['producer']['finishedAtUtc']))
            self.assertLessEqual(datetime.fromisoformat(entry['startedAtUtc']), datetime.fromisoformat(entry['finishedAtUtc']))
            self.assertEqual(0, entry['exitCode'])
            self.assertEqual(1, entry['resultCount'])
            self.assertEqual(0, entry['failureCount'])
            self.assertEqual(0, entry['skipCount'])
            self.assertRegex(entry['stdoutSha256'], r'^[0-9a-f]{64}$')
            self.assertEqual(digest(b''), entry['stderrSha256'])
            self.assertEqual(json_digest(entry['arguments']), entry['argumentsSha256'])
        self.assertEqual(digest((self.base['context'] + '\n').encode()), packet['commands'][0]['stdoutSha256'])
        self.assertEqual(digest(b'1.18.1\n'), packet['commands'][2]['stdoutSha256'])
        self.assertGreater(len(packet['sourceCommands']), 0)
        for entry in packet['sourceCommands']:
            if entry['arguments'][0] == 'show':
                exact = subprocess.check_output(['git', *entry['arguments']], cwd=REPO_ROOT)
                self.assertEqual(digest(exact), entry['stdoutSha256'])

        self.assertEqual({'validated'}, {entry['streamSafety'] for entry in packet['commands']})

    def test_clean_and_dirty_source_states_are_labelled_without_acceptance(self):
        root = self.temporary_repository()
        runner = root / SOURCE_PATHS[0]
        result, packets, _, _ = self.run_gate(runner=runner)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('clean', packets[0]['sourceDisposition'])
        self.assertFalse(packets[0]['worktreeDirty'])
        self.assertTrue(all(source['matchesHead'] and not source['modified'] for source in packets[0]['producerSources']))
        helper = root / SOURCE_PATHS[1]
        helper.write_bytes(helper.read_bytes() + b'\r\n# dirty development source\r\n')
        result, packets, _, _ = self.run_gate(runner=runner)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('dirty-development', packets[0]['sourceDisposition'])
        source = next(source for source in packets[0]['producerSources'] if source['source'] == SOURCE_PATHS[1])
        self.assertTrue(source['modified'])
        self.assertFalse(source['matchesHead'])
        self.assertEqual('pending', packets[0]['independentDisposition'])
        root = self.temporary_repository()
        scenario = copy.deepcopy(self.base)
        scenario['mutateSource'] = str(root / '.gitattributes')
        result, packets, _, _ = self.run_gate(scenario, runner=root / SOURCE_PATHS[0])
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertTrue(packets[0]['worktreeDirty'])
        self.assertEqual('dirty-development', packets[0]['sourceDisposition'])
        self.assertEqual('unchanged', packets[0]['finalSourceRecheck'])

    def test_all_invalid_modes_and_sessions_refuse_before_target_and_output(self):
        for options in ({'session': None}, {'session': ''}, {'session': 'space session'},
                        {'session': '../escape'}, {'session': SESSION + '\n'}, {'session': SESSION + '\r\n'}, {'session': 'a' * 129}, {'session': 'token-value'},
                        {'session': TOKEN_CANARY}, {'gate': 'c1.15'}, {'profile': 'pg-onprem-2'},
                        {'gate': 'C1.17'}, {'opt_in': True}, {'profile': 'PG-ONPREM-1'},
                        {'gate': 'C1.16'}, {'extra': ('-CommandTimeoutSeconds', '0')}):
            with self.subTest(options=options):
                result, packets, calls, evidence = self.run_gate(**options)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], packets)
                self.assertEqual([], calls)
                self.assertFalse(evidence.exists())

    def test_every_approved_input_drift_refuses_before_target_and_output(self):
        self.assertEqual(16, len(INPUT_PATHS))
        root = self.temporary_repository()
        for relative in INPUT_PATHS:
            with self.subTest(relative=relative):
                path = root / relative
                original = path.read_bytes()
                path.write_bytes(original + b'\n# unauthorized drift\n')
                try:
                    result, packets, calls, evidence = self.run_gate(runner=root / SOURCE_PATHS[0])
                    self.assertNotEqual(0, result.returncode)
                    self.assertIn('approved-profile-input-drift', result.stderr)
                    self.assertEqual([], packets)
                    self.assertEqual([], calls)
                    self.assertFalse(evidence.exists())
                finally:
                    path.write_bytes(original)

    def test_missing_symlink_input_and_missing_repository_refuse_before_target(self):
        for mode in ('missing', 'symlink', 'git-unavailable'):
            with self.subTest(mode=mode):
                root = self.temporary_repository()
                path = root / INPUT_PATHS[0]
                if mode == 'missing':
                    path.unlink()
                elif mode == 'symlink':
                    path.unlink()
                    path.symlink_to(REPO_ROOT / INPUT_PATHS[0])
                else:
                    shutil.rmtree(root / '.git')
                result, packets, calls, evidence = self.run_gate(runner=root / SOURCE_PATHS[0])
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], packets)
                self.assertEqual([], calls)
                self.assertFalse(evidence.exists())

    def test_wrong_runtime_image_and_runtime_pins_block_without_partial_observations(self):
        for target, value, failure in (('image', 'sha256:' + 'a' * 64, 'runtime-image-mismatch'),
                                      ('version', '1.18.2', 'runtime-version-pin-mismatch'),
                                      ('metadata', '1.18.2', 'runtime-version-pin-mismatch'),
                                      ('metadata', 1181, 'runtime-version-pin-mismatch')):
            with self.subTest(target=target, value=value):
                scenario = copy.deepcopy(self.base)
                if target == 'image':
                    scenario['pods']['items'][0]['status']['containerStatuses'][1]['imageID'] = value
                elif target == 'version':
                    scenario['daprdVersions'][self.pod] = value
                else:
                    scenario['metadata'][self.pod]['runtimeVersion'] = value
                self.assert_blocked(scenario, failure)

    def test_missing_token_wrong_target_and_malformed_metadata_block(self):
        for kind, failure in (('token', 'kubectl-metadata:' + self.pod + '-exit-72'),
                              ('context', 'profile-context-mismatch'), ('malformed', 'malformed-metadata-json'),
                              ('duplicate', 'malformed-metadata-json')):
            with self.subTest(kind=kind):
                scenario = copy.deepcopy(self.base)
                if kind == 'token':
                    scenario['metadataTokenAvailable'] = False
                elif kind == 'context':
                    scenario['context'] = 'wrong-context'
                else:
                    scenario['metadataRaw'] = {self.pod: '{' if kind == 'malformed' else '{"id":"x","id":"y"}'}
                self.assert_blocked(scenario, failure)

    def test_secret_fields_and_encoded_or_stderr_credentials_have_no_unsafe_hashes(self):
        for kind in ('field', 'encoded', 'stderr', 'quoted-double', 'quoted-single', 'url',
                     'escaped-url', 'escaped-stderr-url', 'escaped-assignment', 'authorization',
                     'dapr-api-token', 'client-secret', 'access-token'):
            with self.subTest(kind=kind):
                scenario = copy.deepcopy(self.base)
                if kind == 'field':
                    scenario['metadata'][self.pod]['discarded'] = {'password': 'private-value'}
                elif kind == 'encoded':
                    raw = json.dumps(scenario['metadata'][self.pod])
                    scenario['metadataRaw'] = {self.pod: raw[:-1] + ',"pass\\u0077ord":"private-value"}'}
                elif kind in ('escaped-url', 'escaped-assignment'):
                    raw = json.dumps(scenario['metadata'][self.pod])
                    unsafe = ('postgresql:\\/\\/username:private-value@localhost/db' if kind == 'escaped-url'
                              else 'password=\\"private-value\\"')
                    scenario['metadataRaw'] = {self.pod: raw[:-1] + ',"discarded":"' + unsafe + '"}'}
                else:
                    diagnostics = {'stderr': 'password=private-value', 'quoted-double': 'password="private-value"',
                                   'quoted-single': "password='private-value'",
                                   'authorization': 'authorization="private-value"',
                                   'dapr-api-token': "dapr-api-token='private-value'",
                                   'client-secret': 'client_secret="private-value"',
                                   'access-token': "access-token='private-value'",
                                   'url': 'postgresql://username:private-value@localhost/db',
                                   'escaped-stderr-url': '{"discarded":"postgresql:\\/\\/username:private-value@localhost/db"}'}
                    scenario.update(stderrPurpose='metadata', stderr=diagnostics[kind])
                packet, _ = self.assert_blocked(scenario, 'secret-shaped-output')
                failed = packet['commands'][-1]
                self.assertIsNone(failed['stdoutSha256'])
                self.assertIsNone(failed['stderrSha256'])
                self.assertEqual(0, failed['resultCount'])
                self.assertNotIn('private-value', json.dumps(packet))

    def test_pod_and_source_mutations_before_publication_block(self):
        for kind in ('uid', 'image', 'lifecycle-image', 'helper', 'component-helper', 'input', 'producer'):
            with self.subTest(kind=kind):
                scenario = copy.deepcopy(self.base)
                options = {}
                if kind in ('uid', 'image', 'lifecycle-image'):
                    scenario['podsAfter'] = copy.deepcopy(scenario['pods'])
                    pod = scenario['podsAfter']['items'][0]
                    if kind == 'uid':
                        pod['metadata']['uid'] = 'changed-uid'
                    else:
                        pod['status']['containerStatuses'][1 if kind == 'image' else 0]['imageID'] = 'sha256:' + CHILD
                    failure = 'running-pod-changed'
                else:
                    root = self.temporary_repository()
                    relative = {'helper': SOURCE_PATHS[1], 'component-helper': SOURCE_PATHS[2],
                                'producer': SOURCE_PATHS[0], 'input': INPUT_PATHS[0]}[kind]
                    scenario['mutateSource'] = str(root / relative)
                    options['runner'] = root / SOURCE_PATHS[0]
                    failure = 'producer-source-changed'
                packet, _ = self.assert_blocked(scenario, failure, **options)
                if kind in ('helper', 'component-helper', 'input', 'producer'):
                    self.assertEqual('changed-or-unavailable', packet['finalSourceRecheck'])

    def test_timeout_oversized_and_child_failure_are_bounded_and_neutral(self):
        for kind, failure in (('timeout', 'kubectl-metadata:' + self.pod + '-timeout'),
                              ('excess', 'kubectl-metadata:' + self.pod + '-output-too-large'),
                              ('failed', 'kubectl-metadata:' + self.pod + '-exit-71')):
            with self.subTest(kind=kind):
                scenario = copy.deepcopy(self.base)
                scenario[{'timeout': 'hangPurpose', 'excess': 'excessPurpose', 'failed': 'failPurpose'}[kind]] = 'metadata'
                packet, _ = self.assert_blocked(scenario, failure, timeout=1 if kind == 'timeout' else 30)
                command = packet['commands'][-1]
                self.assertEqual(0, command['resultCount'])
                self.assertEqual(1, command['failureCount'])
                self.assertIsNotNone(command['exitCode'])

    def test_malformed_utf8_stream_has_no_safe_stream_hash_or_observations(self):
        scenario = copy.deepcopy(self.base)
        scenario['rawBytesPurpose'] = 'metadata'
        packet, _ = self.assert_blocked(scenario, 'kubectl-metadata:' + self.pod + '-invalid-utf8')
        self.assertIsNone(packet['commands'][-1]['stdoutSha256'])
        self.assertIsNone(packet['commands'][-1]['stderrSha256'])


    def test_scalar_metadata_app_id_is_required(self):
        for value in ([self.base['metadata'][self.pod]['id']], 123, True, None, {'id': self.base['metadata'][self.pod]['id']}):
            with self.subTest(value=value):
                scenario = copy.deepcopy(self.base)
                scenario['metadata'][self.pod]['id'] = value
                self.assert_blocked(scenario, 'metadata-app-id-mismatch')

    def test_reordered_stable_pods_with_approved_index_and_child_are_observed(self):
        scenario = copy.deepcopy(self.base)
        second = copy.deepcopy(scenario['pods']['items'][0])
        second['metadata'].update(name=self.pod + '-second', uid='other-safe-pod-uid')
        second['status']['containerStatuses'][1]['imageID'] = 'containerd://sha256:' + CHILD
        scenario['pods']['items'].append(second)
        for field in ('metadata', 'daprdVersions', 'alphaOptIn'):
            scenario[field][second['metadata']['name']] = copy.deepcopy(scenario[field][self.pod])
        scenario['podsAfter'] = copy.deepcopy(scenario['pods'])
        scenario['podsAfter']['items'].reverse()
        for pod in scenario['podsAfter']['items']:
            pod['metadata'].update(resourceVersion='123456', annotations={'safe-note': 'benign update'})
            pod['status'].update(hostIP='127.0.0.1', podIP='10.1.2.3')
            pod['status']['conditions'][0]['lastTransitionTime'] = '2026-10-07T00:00:00Z'
            for container in pod['status']['containerStatuses']:
                container['restartCount'] = 1
        result, packets, _, _ = self.run_gate(scenario)
        self.assertEqual(0, result.returncode, result.stderr)
        packet = packets[0]
        self.assertEqual('observed', packet['producerStatus'])
        self.assertEqual(2, packet['resultCount'])
        self.assertEqual({'sha256:' + INDEX, 'sha256:' + CHILD}, set(packet['observations']['sidecarImageDigests']))
        self.assertEqual({pod['status']['containerStatuses'][1]['imageID'] for pod in scenario['pods']['items']},
                         set(packet['observations']['sidecarImageIds']))
        self.assertEqual(WORKLOAD_ID, packet['workloadId'])
        self.assertEqual(WORKLOAD_HASH, packet['workloadSha256'])

    def test_unsafe_resolved_evidence_path_refuses_before_provenance_target_and_output(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        unsafe = Path(temporary.name) / TOKEN_CANARY
        unsafe.mkdir()
        result, packets, calls, evidence = self.run_gate(cwd=unsafe, evidence_argument='evidence',
                                                       git_hook='raise SystemExit("unexpected provenance call")')
        self.assertNotEqual(0, result.returncode)
        self.assertIn('secret-shaped-output', result.stderr)
        self.assertNotIn('unexpected provenance call', result.stderr)
        self.assertEqual([], packets)
        self.assertEqual([], calls)
        self.assertFalse(evidence.exists())
        self.assertFalse((unsafe / 'evidence').exists())
        fixture = self.temporary_repository()
        unsafe_root = fixture / TOKEN_CANARY
        unsafe_root.mkdir()
        for path in list(fixture.iterdir()):
            if path != unsafe_root:
                shutil.move(str(path), unsafe_root / path.name)
        result, packets, calls, evidence = self.run_gate(runner=unsafe_root / SOURCE_PATHS[0],
                                                       git_hook='raise SystemExit("unexpected provenance call")')
        self.assertNotEqual(0, result.returncode)
        self.assertEqual('secret-shaped-output', result.stderr.strip())
        self.assertEqual([], packets)
        self.assertEqual([], calls)
        self.assertFalse(evidence.exists())

    def test_loaded_source_mutations_refuse_before_target_and_output(self):
        for relative in SOURCE_PATHS:
            for phase in ('git-query', 'script-load'):
                with self.subTest(relative=relative, phase=phase):
                    root = self.temporary_repository()
                    path = root / relative
                    hook = None
                    if phase == 'git-query':
                        hook = ('changed = Path(' + repr(str(path)) + ')\n'
                                'changed.write_bytes(changed.read_bytes() + b"\\n# provenance mutation\\n")')
                    else:
                        # Mutate the currently parsed main/helper from its own file-backed AST.
                        mutation = '[System.IO.File]::AppendAllText($PSCommandPath, "`n# load mutation`n")'
                        text = path.read_text()
                        if relative == SOURCE_PATHS[0]:
                            text = text.replace("$ErrorActionPreference = 'Stop'", "$ErrorActionPreference = 'Stop'\n" + mutation, 1)
                        else:
                            text += '\n' + mutation + '\n'
                        path.write_bytes(text.replace('\n', '\r\n').encode())
                    result, packets, calls, evidence = self.run_gate(runner=root / SOURCE_PATHS[0], git_hook=hook)
                    self.assertNotEqual(0, result.returncode, result.stderr)
                    self.assertIn('producer-source-changed', result.stderr)
                    self.assertEqual([], packets)
                    self.assertEqual([], calls)
                    self.assertFalse(evidence.exists())

    def test_git_output_caps_and_one_deadline_refuse_before_target_and_output(self):
        for mode in ('stdout', 'stderr', 'parent-running', 'inherited-pipe'):
            with self.subTest(mode=mode):
                root = self.temporary_repository()
                hook = ('sys.' + mode + '.buffer.write(b"x" * (1024 * 1024 + 1))\nraise SystemExit(0)' if mode in ('stdout', 'stderr')
                        else 'time.sleep(8)' if mode == 'parent-running'
                        else 'child = os.fork()\nif child == 0:\n    time.sleep(8)\n    os._exit(0)\nraise SystemExit(0)')
                hook = ('Path(os.environ["C1_SCENARIO"]).with_name("git-started").write_text(str(time.monotonic()))\n' + hook)
                result, packets, calls, evidence = self.run_gate(runner=root / SOURCE_PATHS[0], git_hook=hook)
                elapsed = time.monotonic() - float((evidence.parent / 'git-started').read_text())
                self.assertNotEqual(0, result.returncode, result.stderr)
                self.assertIn('source-git-output-too-large' if mode in ('stdout', 'stderr') else 'source-git-timeout', result.stderr)
                self.assertLess(elapsed, 6, 'Five-second Git deadline must include inherited pipe drains')
                self.assertEqual([], packets)
                self.assertEqual([], calls)
                self.assertFalse(evidence.exists())

    def test_late_git_queries_recheck_earlier_bytes_and_refresh_unrelated_dirt(self):
        for kind in ('earlier-source', 'unrelated-dirt'):
            with self.subTest(kind=kind):
                root = self.temporary_repository()
                changed = root / (SOURCE_PATHS[0] if kind == 'earlier-source' else '.gitattributes')
                arguments = ['status', '--porcelain=v1', '--untracked-files=all', '--', INPUT_PATHS[-1]]
                hook = ('if Path(os.environ["C1_KUBECTL_LOG"]).exists() and sys.argv[1:] == ' + repr(arguments) + ':\n'
                        '    changed = Path(' + repr(str(changed)) + ')\n'
                        '    changed.write_bytes(changed.read_bytes() + b"\\n# late-query mutation\\n")')
                if kind == 'earlier-source':
                    packet, _ = self.assert_blocked(self.base, 'producer-source-changed', runner=root / SOURCE_PATHS[0], git_hook=hook)
                    self.assertEqual('changed-or-unavailable', packet['finalSourceRecheck'])
                else:
                    result, packets, _, _ = self.run_gate(runner=root / SOURCE_PATHS[0], git_hook=hook)
                    self.assertEqual(0, result.returncode, result.stderr)
                    packet = packets[0]
                    self.assertEqual('observed', packet['producerStatus'])
                    self.assertEqual('unchanged', packet['finalSourceRecheck'])
                self.assertTrue(packet['worktreeDirty'])
                self.assertEqual('dirty-development', packet['sourceDisposition'])
                self.assertEqual(['rev-parse', '--verify', 'HEAD'], packet['sourceCommands'][-2]['arguments'])
                self.assertEqual(['status', '--porcelain=v1', '--untracked-files=all'], packet['sourceCommands'][-1]['arguments'])

    def test_embedded_password_and_named_credentials_have_no_validated_stream_hashes(self):
        for stream in ('stdout', 'stderr'):
            for content in ({'password': 'private-value'}, {'name': 'DAPR_API_TOKEN', 'value': 'private-value'}):
                for embedded in (False, True):
                    with self.subTest(stream=stream, content=content, embedded=embedded):
                        scenario = copy.deepcopy(self.base)
                        credential = json.dumps(content) if embedded else content
                        if stream == 'stdout':
                            scenario['metadata'][self.pod]['discarded'] = credential
                        else:
                            scenario.update(stderrPurpose='metadata', stderr=json.dumps({'diagnostic': credential}))
                        packet, _ = self.assert_blocked(scenario, 'secret-shaped-output')
                        command = packet['commands'][-1]
                        self.assertEqual('not-validated', command['streamSafety'])
                        self.assertIsNone(command['stdoutSha256'])
                        self.assertIsNone(command['stderrSha256'])
                        self.assertNotIn('private-value', json.dumps(packet))
        scenario = copy.deepcopy(self.base)
        scenario['metadata'][self.pod]['discarded'] = [
            {'name': 'connectionString', 'secretName': 'access-telemetry-postgresql', 'secretKey': 'connectionString'},
            {'name': 'DAPR_API_TOKEN', 'valueFrom': {'secretKeyRef': {'name': 'safe-runtime-reference', 'key': 'token'}}}]
        result, packets, _, _ = self.run_gate(scenario)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('observed', packets[0]['producerStatus'])

    def test_selected_pod_string_arrays_refuse_initially_and_on_recheck(self):
        paths = (
            ('metadata', 'labels', 'app.kubernetes.io/name'),
            ('status', 'conditions', 0, 'type'), ('status', 'conditions', 0, 'status'),
            ('status', 'containerStatuses', 0, 'name'), ('status', 'containerStatuses', 1, 'name'),
            ('status', 'containerStatuses', 0, 'imageID'), ('status', 'containerStatuses', 1, 'imageID'))
        for stage in ('initial', 'recheck'):
            for path in paths:
                with self.subTest(stage=stage, path=path):
                    scenario = copy.deepcopy(self.base)
                    if stage == 'recheck':
                        scenario['podsAfter'] = copy.deepcopy(scenario['pods'])
                    pod = scenario['pods' if stage == 'initial' else 'podsAfter']['items'][0]
                    selected = pod
                    for key in path[:-1]:
                        selected = selected[key]
                    selected[path[-1]] = [selected[path[-1]]]
                    result, packets, _, _ = self.run_gate(scenario)
                    self.assertNotEqual(0, result.returncode, result.stderr)
                    self.assertEqual('blocked', packets[0]['producerStatus'])
                    self.assertEqual([], packets[0]['observations']['pods'])
                    self.assertEqual(0, packets[0]['resultCount'])
                    if stage == 'recheck':
                        self.assertIn('running-pod-changed', packets[0]['blockers'])

    def test_pg2_identity_families_refuse_lf_and_crlf(self):
        pod_path = ('pods', 'items', 0)
        metadata_path = ('metadata', self.pod)
        fields = (
            (pod_path + ('metadata', 'name'), 'running-pod-identity-invalid'),
            (pod_path + ('metadata', 'uid'), 'running-pod-identity-invalid'),
            (pod_path + ('metadata', 'labels', 'app.kubernetes.io/name'), 'running-pod-label-mismatch'),
            (pod_path + ('status', 'phase'), 'running-pod-phase-invalid'),
            (pod_path + ('status', 'conditions', 0, 'type'), 'running-pod-not-stable'),
            (pod_path + ('status', 'conditions', 0, 'status'), 'running-pod-not-stable'),
            (pod_path + ('status', 'containerStatuses', 0, 'name'), 'running-pod-containers-not-ready'),
            (pod_path + ('status', 'containerStatuses', 1, 'imageID'), 'runtime-image-mismatch'),
            (pod_path + ('status', 'containerStatuses', 0, 'imageID'), 'runtime-image-mismatch'),
            (metadata_path + ('id',), 'metadata-app-id-mismatch'),
            (metadata_path + ('runtimeVersion',), 'runtime-version-pin-mismatch'),
            (metadata_path + ('scheduler', 'connectedAddresses', 0), 'metadata-scheduler-addresses-invalid'),
            (metadata_path + ('actors', 0, 'type'), 'metadata-actor-type-invalid'),
            (metadata_path + ('enabledFeatures', 0), 'metadata-enabled-features-invalid'))
        for newline in ('\n', '\r\n'):
            for path, failure in fields:
                with self.subTest(newline=newline, path=path):
                    scenario = copy.deepcopy(self.base)
                    selected = scenario
                    for key in path[:-1]:
                        selected = selected[key]
                    selected[path[-1]] += newline
                    self.assert_blocked(scenario, failure)

    def test_legacy_c1_15_stays_v1_without_session_and_cannot_consume_v2_session(self):
        result, packets, _, _ = self.run_gate(profile='PG-ONPREM-1', session=None)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('hexalith.access-telemetry.c1.evidence/v1', packets[0]['schemaVersion'])
        self.assertNotIn('qualificationSessionId', packets[0])
        self.assertNotIn('producerSources', packets[0])
        self.assertEqual({'purpose', 'sha256'}, set(packets[0]['commands'][0]))


if __name__ == '__main__':
    unittest.main()
