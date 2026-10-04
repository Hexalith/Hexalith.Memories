import copy
import hashlib
import json
import os
import shutil
import stat
import subprocess
import sys
import tempfile
import textwrap
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RUNNER = REPO_ROOT / 'tools/verify-access-telemetry-c1.ps1'
HELPER = REPO_ROOT / 'tools/access-telemetry-c1-component-backend.ps1'
FIXTURE = Path(__file__).parent / 'fixtures/c1_16_complete.json'
TOKEN_CANARY = 'C1_SECRET_CANARY_DO_NOT_EMIT_7429'
POD_OUTPUT = "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"


def write_fake_kubectl(directory: Path) -> None:
    fake = directory / 'fake_kubectl.py'
    fake.write_text(textwrap.dedent(r'''
        import json
        import os
        import sys
        import time
        from pathlib import Path

        args = sys.argv[1:]
        scenario = json.loads(Path(os.environ['C1_SCENARIO']).read_text())
        log = Path(os.environ['C1_KUBECTL_LOG'])
        prior = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
        with log.open('a') as stream:
            stream.write(json.dumps(args) + '\n')
        purpose = None
        after = False
        if args == ['config', 'current-context']:
            purpose = 'context'
        elif args[:4] != ['--context', 'jpiquot@local', '-n', 'hexalith-memories']:
            raise SystemExit(99)
        elif args[4:7] == ['get', 'component', 'access-telemetry-store']:
            assert len(args) == 9 and args[7] == '-o'
            template = args[-1]
            assert template.startswith('go-template=')
            assert '.metadata.uid' in template and '.metadata.resourceVersion' in template
            assert '.secretKeyRef.name' in template and '.secretKeyRef.key' in template
            # connectionString branch reports inline presence, never its value.
            branch = template.split('{{if eq .name "connectionString"}}')[1]
            assert '.value' not in branch and 'eq $key "value"' in branch
            assert '.metadata.annotations' not in template and '.spec |' not in template
            purpose = 'component'
            after = any(call[4:7] == args[4:7] for call in prior)
        elif args[4:6] == ['get', 'pods']:
            assert args[-2:] == ['-o', "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"]
            selector = args[args.index('-l') + 1]
            purpose = {'app.kubernetes.io/name=memories-access-telemetry': 'pods',
                       'app.kubernetes.io/name=access-telemetry-postgresql': 'backendPods'}[selector]
            after = any('get' in call and 'pods' in call and selector in call for call in prior)
        elif args[4] == 'exec' and args[6:8] == ['-c', 'lifecycle']:
            assert args[8:11] == ['--', '/bin/sh', '-ec']
            assert '/v1.0/metadata' in args[-1]
            assert '--header="dapr-api-token: ${DAPR_API_TOKEN}"' in args[-1]
            assert '*"$DAPR_API_TOKEN"*' in args[-1]
            assert 'required runtime credential unavailable' in args[-1]
            if not scenario.get('metadataTokenAvailable', True):
                print('required runtime credential unavailable', file=sys.stderr)
                raise SystemExit(72)
            purpose = 'metadata'
        elif args[4] == 'exec' and args[6:8] == ['-c', 'postgresql']:
            assert args[8:10] == ['--', 'env']
            for required in ['PGPASSWORD', 'PGSERVICE', 'PGSERVICEFILE', 'PGPASSFILE=/dev/null',
                             'PGCONNECT_TIMEOUT=5', 'PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=5000',
                             'psql', '-X', '--no-password', '--set=ON_ERROR_STOP=1',
                             '--host=/var/run/postgresql', '--port=5432', '--username=memories_admin',
                             '--dbname=memories_access_telemetry', '--tuples-only', '--no-align']:
                assert required in args
            assert "current_setting('server_version')" in args[-1]
            assert "current_setting('server_version_num')" in args[-1]
            assert 'current_database()' in args[-1] and 'inet_client_addr() IS NULL' in args[-1]
            assert "'systemUser', system_user" in args[-1]
            assert args[-1].startswith('SELECT ') and ';' == args[-1][-1]
            purpose = 'server'
        else:
            raise SystemExit(98)
        if scenario.get('hangPurpose') == purpose:
            time.sleep(5)
        if scenario.get('excessPurpose') == purpose:
            print('x' * (1024 * 1024 + 32), file=sys.stderr if scenario.get('excessStderr') else sys.stdout)
            raise SystemExit(0)
        if scenario.get('failPurpose') == purpose:
            print(scenario.get('stderr', 'observation refused'), file=sys.stderr)
            raise SystemExit(scenario.get('exitCode', 71))
        if scenario.get('stderrPurpose') == purpose:
            print(scenario['stderr'], file=sys.stderr)
        if purpose == 'context':
            print(scenario['context'])
        elif purpose in ('component', 'pods', 'backendPods'):
            key = purpose + ('After' if after else '')
            if key + 'Raw' in scenario:
                print(scenario[key + 'Raw'])
            else:
                value = scenario.get(key, scenario[purpose])
                if purpose == 'component':
                    print(json.dumps(value))
                else:
                    for pod in value['items']:
                        print(json.dumps([pod.get('metadata'), pod.get('status')]))
        elif purpose == 'metadata':
            pod = args[5]
            print(scenario.get('metadataRaw', {}).get(pod, json.dumps(scenario['metadata'][pod])))
        else:
            print(scenario.get('serverRaw', json.dumps(scenario['server'])))
    ''').strip() + '\n', encoding='utf-8')
    executable = directory / 'kubectl'
    executable.write_text(f'#!/usr/bin/env sh\nexec "{sys.executable}" "{fake}" "$@"\n')
    executable.chmod(0o755)


class ComponentBackendCaptureTests(unittest.TestCase):
    def setUp(self) -> None:
        self.base = json.loads(FIXTURE.read_text())
        self.pod = self.base['pods']['items'][0]['metadata']['name']

    def run_gate(self, scenario, *, opt_in=True, gate='C1.16', profile='PG-ONPREM-1', repeat=1, timeout=30, runner=RUNNER):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        root = Path(temp.name)
        fake_bin = root / 'bin'
        fake_bin.mkdir()
        write_fake_kubectl(fake_bin)
        scenario_path = root / 'scenario.json'
        scenario_path.write_text(json.dumps(scenario))
        log = root / 'calls.jsonl'
        evidence = root / 'evidence'
        env = os.environ.copy()
        env.update(PATH=str(fake_bin) + os.pathsep + env.get('PATH', ''),
                   C1_SCENARIO=str(scenario_path), C1_KUBECTL_LOG=str(log), DAPR_API_TOKEN=TOKEN_CANARY)
        command = ['pwsh', str(runner), '-Gate', gate, '-ProfileId', profile,
                   '-EvidenceDirectory', str(evidence), '-CommandTimeoutSeconds', str(timeout)]
        if opt_in:
            command.append('-AllowHistoricalProfileCapture')
        for _ in range(repeat):
            result = subprocess.run(command, cwd=REPO_ROOT, env=env, text=True, capture_output=True,
                                    check=False, timeout=max(12, timeout + 5))
            if result.returncode:
                break
        packets = [json.loads(path.read_text()) for path in sorted(evidence.glob('*.json'))]
        calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
        captured = result.stdout + result.stderr + ''.join(path.read_text() for path in evidence.glob('*.json'))
        self.assertNotIn(TOKEN_CANARY, captured)
        return result, packets, calls, evidence

    def assert_blocked(self, scenario, expected=None, *, timeout=30, secrets=(), absent_source=None, profile='PG-ONPREM-1', opt_in=True):
        result, packets, calls, evidence = self.run_gate(scenario, timeout=timeout, profile=profile, opt_in=opt_in)
        self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertEqual(1, len(packets), result.stderr)
        packet = packets[0]
        self.assertEqual('blocked', packet['producerStatus'])
        self.assertEqual('not-evaluated', packet['gateStatus'])
        self.assertFalse(packet['productionGatePassed'])
        self.assertEqual({'component': None, 'lifecyclePods': [], 'loadedComponents': [], 'backend': None},
                         packet['observations'])
        self.assertEqual(1, len(packet['blockers']))
        if expected:
            self.assertEqual([expected], packet['blockers'])
        if absent_source:
            self.assertFalse(any(source['source'] == absent_source for source in packet['sources']))
        captured = result.stdout + result.stderr + ''.join(path.read_text() for path in evidence.glob('*.json'))
        for secret in secrets:
            self.assertNotIn(secret, captured)
        return calls

    def test_complete_capture_is_separate_immutable_and_neutral(self):
        result, packets, calls, evidence = self.run_gate(self.base, repeat=2)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets))
        self.assertEqual(18, len(calls))
        for path in evidence.glob('*.json'):
            self.assertTrue(path.name.startswith('c1.16-component-backend-identity-'))
            self.assertFalse(path.stat().st_mode & stat.S_IWUSR)
        packet = packets[0]
        self.assertEqual('C1.16', packet['gate'])
        self.assertEqual('PG-ONPREM-1', packet['profileId'])
        self.assertTrue(packet['historicalProfileCapture'])
        self.assertEqual('closed-historical', packet['profileDisposition'])
        self.assertEqual('observed', packet['producerStatus'])
        self.assertEqual('not-evaluated', packet['gateStatus'])
        self.assertEqual('not-evaluated', packet['componentBehavior'])
        self.assertEqual('not-evaluated', packet['productionLifecycleWrites'])
        self.assertFalse(packet['productionGatePassed'])
        self.assertEqual([], packet['blockers'])
        observed = packet['observations']
        self.assertEqual('40', observed['component']['settings']['maxConns'])
        self.assertEqual(self.base['component']['uid'], observed['component']['uid'])
        self.assertEqual(self.base['component']['resourceVersion'], observed['component']['resourceVersion'])
        self.assertEqual({'name': 'access-telemetry-postgresql', 'key': 'connectionString'},
                         observed['component']['connectionReference'])
        self.assertEqual(['ETAG', 'KEYS_LIKE', 'TRANSACTIONAL', 'TTL'],
                         observed['loadedComponents'][0]['advertisedCapabilities'])
        self.assertEqual('1.18.1', observed['loadedComponents'][0]['runtimeVersion'])
        self.assertEqual('180004', observed['backend']['serverVersionNum'])
        self.assertEqual('memories_access_telemetry', observed['backend']['database'])
        self.assertEqual('memories_admin', observed['backend']['user'])
        self.assertEqual('peer:postgres', observed['backend']['systemUser'])
        self.assertTrue(observed['backend']['localSocket'])
        backend_sources = [entry for entry in packet['sources']
                           if entry['source'] == 'kubectl:backend-server-identity:allowlisted']
        self.assertEqual(1, len(backend_sources))
        backend_json = json.dumps(observed['backend'], separators=(',', ':'), ensure_ascii=False)
        self.assertEqual(hashlib.sha256(backend_json.encode('utf-8')).hexdigest(), backend_sources[0]['sha256'])
        for source in (RUNNER, HELPER):
            matches = [entry for entry in packet['sources'] if entry['source'] == 'tools/' + source.name]
            self.assertEqual(1, len(matches))
            self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(), matches[0]['sha256'])
        self.assertTrue(all(len(entry['sha256']) == 64 for entry in packet['commands']))
        self.assertFalse(any('secret' in call or 'secrets' in call for call in calls))

    def test_opt_in_and_exact_gate_profile_denials_precede_calls_and_directory_creation(self):
        for kwargs in ({'opt_in': False}, {'profile': 'PG-ONPREM-2'}, {'profile': 'PG-CLOUD-1'},
                       {'profile': 'pg-onprem-1'},
                       {'gate': 'C1.17'}, {'gate': 'c1.16'},
                       {'profile': 'PG-ONPREM-2', 'gate': 'C1.15', 'opt_in': False},
                       {'profile': 'pg-onprem-2', 'opt_in': False},
                       {'profile': 'PG-ONPREM-2', 'gate': 'c1.16', 'opt_in': False}):
            with self.subTest(kwargs=kwargs):
                result, packets, calls, evidence = self.run_gate(self.base, **kwargs)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], packets)
                self.assertEqual([], calls)
                self.assertFalse(evidence.exists())

    def test_authenticated_historical_linux_amd64_child_image_is_observed(self):
        scenario = copy.deepcopy(self.base)
        scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'containerd://sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf'
        )
        result, packets, _, _ = self.run_gate(scenario)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('observed', packets[0]['producerStatus'])
        self.assertEqual(scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'],
                         packets[0]['observations']['backend']['imageId'])

    def test_unreviewed_backend_digest_and_index_to_child_change_block(self):
        scenario = copy.deepcopy(self.base)
        scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'containerd://sha256:' + 'a' * 64
        )
        self.assert_blocked(scenario, 'backend-image-mismatch')
        scenario = copy.deepcopy(self.base)
        scenario['backendPodsAfter'] = copy.deepcopy(scenario['backendPods'])
        scenario['backendPodsAfter']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'containerd://sha256:d93de42662696f278fb34354b06fdaa90ad7ca3106d6f72fbd01d16da006d2cf'
        )
        self.assert_blocked(scenario, 'running-pod-changed')

    def test_component_missing_duplicate_wrong_type_and_conflicting_identity_block(self):
        for name, mutate in {
            'uid-missing': lambda c: c.pop('uid'),
            'uid-number': lambda c: c.update(uid=123),
            'resource-version-number': lambda c: c.update(resourceVersion=123),
            'type-drift': lambda c: c.update(type='state.redis'),
            'version-drift': lambda c: c.update(version='v1'),
            'api-drift': lambda c: c.update(apiVersion='dapr.io/v1'),
            'namespace-drift': lambda c: c.update(namespace='other-tenant'),
            'scope-duplicate': lambda c: c['scopes'].append(c['scopes'][0]),
            'scope-wrong-type': lambda c: c.update(scopes='memories-access-telemetry'),
            'scope-drift': lambda c: c.update(scopes=['other-tenant']),
            'missing-settings': lambda c: c['settings'].pop(0),
            'duplicate-settings': lambda c: c['settings'].__setitem__(1, copy.deepcopy(c['settings'][0])),
            'settings-number': lambda c: c['settings'][4].update(value=40),
            'settings-drift': lambda c: c['settings'][4].update(value='41'),
            'settings-ref': lambda c: c['settings'][0].update(hasSecretReference=True),
            'inline-connection': lambda c: c['connectionReferences'][0].update(hasInlineValue=True),
            'connection-missing': lambda c: c.update(connectionReferences=[None]),
            'connection-duplicate': lambda c: c['connectionReferences'].insert(0, copy.deepcopy(c['connectionReferences'][0])),
            'connection-drift': lambda c: c['connectionReferences'][0].update(secretName='other-secret'),
        }.items():
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base)
                mutate(scenario['component'])
                calls = self.assert_blocked(scenario)
                self.assertEqual(2, len(calls))

    def test_authenticated_loaded_component_missing_duplicate_and_capability_drift_block(self):
        for name, mutate in {
            'component-missing': lambda m: m.update(components=[]),
            'components-wrong-type': lambda m: m.update(components={}),
            'component-duplicate': lambda m: m['components'].append(copy.deepcopy(m['components'][0])),
            'runtime-drift': lambda m: m.update(runtimeVersion='1.18.4'),
            'runtime-wrong-type': lambda m: m.update(runtimeVersion=1181),
            'app-drift': lambda m: m.update(id='other-tenant'),
            'type-drift': lambda m: m['components'][0].update(type='state.redis'),
            'version-drift': lambda m: m['components'][0].update(version='v1'),
            'capability-missing': lambda m: m['components'][0]['capabilities'].pop(),
            'capability-duplicate': lambda m: m['components'][0]['capabilities'].append('TTL'),
            'capabilities-wrong-type': lambda m: m['components'][0].update(capabilities='ETAG'),
            'capability-wrong-type': lambda m: m['components'][0].update(capabilities=[42]),
            'capability-conflicting': lambda m: m['components'][0]['capabilities'].append('ACTOR'),
        }.items():
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base)
                mutate(scenario['metadata'][self.pod])
                calls = self.assert_blocked(scenario)
                self.assertFalse(any('psql' in call for call in calls))
        denied = copy.deepcopy(self.base)
        denied['metadataTokenAvailable'] = False
        self.assert_blocked(denied, 'kubectl-component-metadata:' + self.pod + '-exit-72')

    def test_backend_actual_server_database_and_local_peer_identity_must_match(self):
        for field, value in [('serverVersion', '18.6'), ('serverVersionNum', '180006'),
                             ('serverVersionNum', 180004), ('database', 'other_database'),
                             ('user', 'postgres'), ('localSocket', False), ('localSocket', 'true')]:
            with self.subTest(field=field, value=value):
                scenario = copy.deepcopy(self.base)
                scenario['server'][field] = value
                self.assert_blocked(scenario)

    def test_backend_system_user_rejects_trust_null_missing_wrong_identity_and_types(self):
        for value in (None, 'trust:postgres', 'peer:other', 'peer:memories_admin', 123, ['peer:postgres'], {}):
            with self.subTest(value=value):
                scenario = copy.deepcopy(self.base)
                scenario['server']['systemUser'] = value
                self.assert_blocked(scenario, 'backend-peer-system-user-invalid')
        scenario = copy.deepcopy(self.base)
        scenario['server'].pop('systemUser')
        self.assert_blocked(scenario, 'backend-peer-system-user-invalid')

    def test_nonempty_credential_fields_of_every_json_type_block_before_provenance(self):
        for name, value in (('authorization', ['Bearer-private-array']),
                            ('dapr-api-token', {'value': 'private-object-token'}),
                            ('password', 12345), ('clientSecret', True), ('access_token', ['private-access-token'])):
            with self.subTest(name=name):
                scenario = copy.deepcopy(self.base)
                scenario['metadata'][self.pod]['discarded'] = {name: value}
                calls = self.assert_blocked(scenario, 'secret-shaped-output',
                                            secrets=('Bearer-private-array', 'private-object-token', 'private-access-token'),
                                            absent_source=f'kubectl:component-metadata:{self.pod}:allowlisted')
                self.assertFalse(any('psql' in call for call in calls))
        for stderr in ('{"authorization":["private-stderr-token"]}',
                       'diagnostic: {"password":["private-array-value"]}',
                       '{"password":["private-array-value"],"password":null}'):
            with self.subTest(stderr=stderr):
                scenario = copy.deepcopy(self.base)
                scenario.update(stderrPurpose='component', stderr=stderr)
                self.assert_blocked(scenario, 'secret-shaped-output',
                                    secrets=('private-stderr-token', 'private-array-value'),
                                    absent_source='kubectl:component-identity:allowlisted')
        scenario = copy.deepcopy(self.base)
        scenario['pods']['items'][0]['metadata']['discarded'] = {'password': {'nested': 'private-pod-password'}}
        self.assert_blocked(scenario, 'secret-shaped-output', secrets=('private-pod-password',),
                            absent_source='kubectl:lifecycle-pods:allowlisted')

    def test_real_go_template_and_shell_env_boundaries_execute_offline(self):
        real_kubectl = shutil.which('kubectl')
        self.assertIsNotNone(real_kubectl, 'Offline printer execution requires the existing kubectl tool')
        result, packets, calls, _ = self.run_gate(self.base)
        self.assertEqual(0, result.returncode, result.stderr)
        template = next(call[-1] for call in calls if call[4:7] == ['get', 'component', 'access-telemetry-store'])
        metadata_command = next(call[9:] for call in calls if call[4:5] == ['exec'] and call[7] == 'lifecycle')
        backend_command = next(call[9:] for call in calls if call[4:5] == ['exec'] and call[7] == 'postgresql')
        component = self.base['component']
        raw_component = {
            'apiVersion': component['apiVersion'], 'kind': component['kind'],
            'metadata': {key: component[key] for key in ('name', 'namespace', 'uid', 'resourceVersion')},
            'spec': {'type': component['type'], 'version': component['version'],
                     'initTimeout': component['initTimeout'],
                     'metadata': [{'name': entry['name'], 'value': entry['value']}
                                  for entry in component['settings'] if entry is not None]},
            'auth': {'secretStore': component['secretStore']}, 'scopes': component['scopes'],
        }
        raw_component['metadata']['annotations'] = {'discarded': TOKEN_CANARY}
        raw_component['spec']['metadata'] += [
            {'name': 'connectionString', 'secretKeyRef': {'name': 'access-telemetry-postgresql', 'key': 'connectionString'}},
            {'name': 'unselectedCredential', 'value': TOKEN_CANARY},
        ]
        requests = []

        class DiscoveryFixture(BaseHTTPRequestHandler):
            def log_message(self, *_):
                pass

            def do_GET(self):
                path = self.path.split('?', 1)[0]
                requests.append(path)
                documents = {
                    '/api': {'kind': 'APIVersions', 'apiVersion': 'v1', 'versions': ['v1']},
                    '/api/v1': {'kind': 'APIResourceList', 'apiVersion': 'v1', 'groupVersion': 'v1', 'resources': []},
                    '/apis': {'kind': 'APIGroupList', 'apiVersion': 'v1', 'groups': [{
                        'name': 'dapr.io', 'versions': [{'groupVersion': 'dapr.io/v1alpha1', 'version': 'v1alpha1'}],
                        'preferredVersion': {'groupVersion': 'dapr.io/v1alpha1', 'version': 'v1alpha1'}}]},
                    '/apis/dapr.io/v1alpha1': {'kind': 'APIResourceList', 'apiVersion': 'v1',
                        'groupVersion': 'dapr.io/v1alpha1', 'resources': [{
                            'name': 'components', 'singularName': 'component', 'namespaced': True,
                            'kind': 'Component', 'verbs': ['get', 'create']}]},
                }
                payload = json.dumps(documents.get(path, {'error': 'unexpected offline discovery request'})).encode()
                self.send_response(200 if path in documents else 400)
                self.send_header('Content-Type', 'application/json')
                self.send_header('Content-Length', str(len(payload)))
                self.end_headers()
                self.wfile.write(payload)

        server = ThreadingHTTPServer(('127.0.0.1', 0), DiscoveryFixture)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(server.server_close)
        self.addCleanup(server.shutdown)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            config = root / 'kubeconfig.json'
            config.write_text(json.dumps({'apiVersion': 'v1', 'kind': 'Config',
                'clusters': [{'name': 'offline', 'cluster': {'server': f'http://127.0.0.1:{server.server_port}'}}],
                'contexts': [{'name': 'offline', 'context': {'cluster': 'offline', 'user': 'offline'}}],
                'users': [{'name': 'offline', 'user': {}}], 'current-context': 'offline'}))
            raw_path = root / 'component.json'
            printer_command = [real_kubectl, '--kubeconfig', str(config), '--cache-dir', str(root / 'cache'),
                               'create', '--dry-run=client', '--validate=false', '-f', str(raw_path), '-o', template]
            for inline in (False, True):
                with self.subTest(inline_connection=inline):
                    if inline:
                        raw_component['spec']['metadata'][-2]['value'] = TOKEN_CANARY
                    raw_path.write_text(json.dumps(raw_component))
                    printer_env = os.environ.copy()
                    for proxy in ('HTTP_PROXY', 'HTTPS_PROXY', 'ALL_PROXY', 'http_proxy', 'https_proxy', 'all_proxy'):
                        printer_env.pop(proxy, None)
                    printer_env['NO_PROXY'] = '127.0.0.1,localhost'
                    projected = subprocess.run(printer_command, env=printer_env, text=True, capture_output=True, check=False, timeout=15)
                    self.assertEqual(0, projected.returncode, projected.stderr)
                    self.assertNotIn(TOKEN_CANARY, projected.stdout + projected.stderr)
                    observation = json.loads(projected.stdout)
                    expected_component = copy.deepcopy(component)
                    expected_component['connectionReferences'][0]['hasInlineValue'] = inline
                    self.assertEqual(expected_component, observation)
            self.assertTrue(requests)
            self.assertTrue(all(path in ('/api', '/api/v1', '/apis', '/apis/dapr.io/v1alpha1') for path in requests))

            fake_bin = root / 'bin'
            fake_bin.mkdir()
            wget = fake_bin / 'wget'
            wget.write_text(f'#!{sys.executable}\n' + textwrap.dedent('''
                import json, os, sys
                from pathlib import Path
                expected = ['-qO-', '--timeout=5', '--header=dapr-api-token: ' + os.environ['DAPR_API_TOKEN'],
                            'http://127.0.0.1:3500/v1.0/metadata']
                if sys.argv[1:] != expected:
                    raise SystemExit(90)
                Path(os.environ['OFFLINE_WGET_MARKER']).write_text('authenticated')
                print(os.environ['OFFLINE_WGET_RESPONSE'])
            '''))
            wget.chmod(0o755)
            marker = root / 'wget-marker'
            env = os.environ.copy()
            env.update(PATH=str(fake_bin) + os.pathsep + env.get('PATH', ''), DAPR_API_TOKEN=TOKEN_CANARY,
                       OFFLINE_WGET_MARKER=str(marker), OFFLINE_WGET_RESPONSE=json.dumps(self.base['metadata'][self.pod]))
            shell = subprocess.run(metadata_command, env=env, text=True, capture_output=True, timeout=10)
            self.assertEqual(0, shell.returncode, shell.stderr)
            self.assertEqual(self.base['metadata'][self.pod], json.loads(shell.stdout))
            self.assertEqual('authenticated', marker.read_text())
            env['OFFLINE_WGET_RESPONSE'] = json.dumps({'discarded': TOKEN_CANARY})
            shell = subprocess.run(metadata_command, env=env, text=True, capture_output=True, timeout=10)
            self.assertEqual(73, shell.returncode)
            self.assertNotIn(TOKEN_CANARY, shell.stdout + shell.stderr)
            marker.unlink()
            env.pop('DAPR_API_TOKEN')
            shell = subprocess.run(metadata_command, env=env, text=True, capture_output=True, timeout=10)
            self.assertEqual(72, shell.returncode)
            self.assertFalse(marker.exists())

            psql = fake_bin / 'psql'
            psql.write_text(f'#!{sys.executable}\n' + textwrap.dedent('''
                import json, os, sys
                from pathlib import Path
                if any(name in os.environ for name in ('PGPASSWORD', 'PGSERVICE', 'PGSERVICEFILE')):
                    raise SystemExit(91)
                if os.environ.get('PGPASSFILE') != '/dev/null' or os.environ.get('PGCONNECT_TIMEOUT') != '5':
                    raise SystemExit(92)
                if os.environ.get('PGOPTIONS') != '-c default_transaction_read_only=on -c statement_timeout=5000':
                    raise SystemExit(93)
                required = ['-X', '--no-password', '--set=ON_ERROR_STOP=1', '--host=/var/run/postgresql',
                            '--username=memories_admin', '--dbname=memories_access_telemetry']
                if not all(arg in sys.argv for arg in required) or "'systemUser', system_user" not in sys.argv[-1]:
                    raise SystemExit(94)
                Path(os.environ['OFFLINE_PSQL_MARKER']).write_text('read-only-no-password')
                print(os.environ['OFFLINE_PSQL_RESPONSE'])
            '''))
            psql.chmod(0o755)
            env.update(PGPASSWORD=TOKEN_CANARY, PGSERVICE='unwanted-service', PGSERVICEFILE='unwanted-service-file',
                       PGPASSFILE='unwanted-password-file', PGOPTIONS='unwanted-options',
                       OFFLINE_PSQL_MARKER=str(root / 'psql-marker'), OFFLINE_PSQL_RESPONSE=json.dumps(self.base['server']))
            peer = subprocess.run(backend_command, env=env, text=True, capture_output=True, timeout=10)
            self.assertEqual(0, peer.returncode, peer.stderr)
            self.assertEqual('read-only-no-password', (root / 'psql-marker').read_text())
            self.assertEqual(self.base['server'], json.loads(peer.stdout))
            self.assertNotIn(TOKEN_CANARY, peer.stdout + peer.stderr)

    def test_component_and_pod_replacements_or_image_changes_block_all_observations(self):
        for field, value in [('uid', 'replacement-uid'), ('resourceVersion', '42018')]:
            with self.subTest(component=field):
                scenario = copy.deepcopy(self.base)
                scenario['componentAfter'] = copy.deepcopy(scenario['component'])
                scenario['componentAfter'][field] = value
                self.assert_blocked(scenario, 'component-changed')
        for key in ('pods', 'backendPods'):
            for mutation in ('uid', 'image', 'not-ready', 'missing', 'duplicate'):
                with self.subTest(key=key, mutation=mutation):
                    scenario = copy.deepcopy(self.base)
                    scenario[key + 'After'] = copy.deepcopy(scenario[key])
                    pods = scenario[key + 'After']['items']
                    if mutation == 'uid':
                        pods[0]['metadata']['uid'] = 'replacement-uid'
                    elif mutation == 'image':
                        pods[0]['status']['containerStatuses'][0]['imageID'] = 'containerd://sha256:' + 'b' * 64
                    elif mutation == 'not-ready':
                        pods[0]['status']['containerStatuses'][0]['ready'] = False
                    elif mutation == 'missing':
                        pods.clear()
                    else:
                        pods.append(copy.deepcopy(pods[0]))
                    self.assert_blocked(scenario)

    def test_initial_missing_duplicate_malformed_or_conflicting_pods_block(self):
        for key in ('pods', 'backendPods'):
            for mutation in ('missing', 'duplicate', 'uid-type', 'phase-type', 'condition-type',
                             'container-name-type', 'not-ready', 'image-missing', 'label-drift'):
                with self.subTest(key=key, mutation=mutation):
                    scenario = copy.deepcopy(self.base)
                    pods = scenario[key]['items']
                    if mutation == 'missing':
                        pods.clear()
                    elif mutation == 'duplicate':
                        pods.append(copy.deepcopy(pods[0]))
                    elif mutation == 'uid-type':
                        pods[0]['metadata']['uid'] = 42
                    elif mutation == 'phase-type':
                        pods[0]['status']['phase'] = True
                    elif mutation == 'condition-type':
                        pods[0]['status']['conditions'][0]['type'] = True
                    elif mutation == 'container-name-type':
                        pods[0]['status']['containerStatuses'][0]['name'] = True
                    elif mutation == 'not-ready':
                        pods[0]['status']['containerStatuses'][0]['ready'] = 'true'
                    elif mutation == 'image-missing':
                        pods[0]['status']['containerStatuses'][0].pop('imageID')
                    else:
                        pods[0]['metadata']['labels']['app.kubernetes.io/name'] = 'other-tenant'
                    self.assert_blocked(scenario)

    def test_json_duplicate_properties_wrong_roots_and_secret_fields_block_before_projection(self):
        for purpose in ('component', 'metadata', 'server', 'pods', 'backendPods'):
            baseline = self.base[purpose][self.pod] if purpose == 'metadata' else self.base[purpose]
            if purpose in ('pods', 'backendPods'):
                pod = baseline['items'][0]
                baseline = [pod['metadata'], pod['status']]
            for issue in ('duplicate-property', 'encoded-secret', 'encoded-credential-name', 'malformed', 'wrong-root'):
                with self.subTest(purpose=purpose, issue=issue):
                    scenario = copy.deepcopy(self.base)
                    raw = json.dumps(baseline)
                    if issue == 'duplicate-property':
                        raw = raw.replace('{', '{"diagnostic":"safe","diagnostic":"safe",', 1)
                    elif issue == 'encoded-secret':
                        encoded = ''.join('\\u%04x' % ord(c) for c in TOKEN_CANARY)
                        raw = raw.replace('{', '{"discarded":{"note":"' + encoded + '"},', 1)
                    elif issue == 'encoded-credential-name':
                        encoded = ''.join('\\u%04x' % ord(c) for c in 'authorization')
                        raw = raw.replace('{', '{"discarded":{"' + encoded + '":"Bearer-private"},', 1)
                    elif issue == 'malformed':
                        raw = '{invalid'
                    else:
                        raw = '[]'
                    if purpose == 'metadata':
                        scenario['metadataRaw'] = {self.pod: raw}
                    else:
                        scenario[purpose + 'Raw'] = raw
                    self.assert_blocked(scenario, secrets=('Bearer-private',))

    def test_literal_credentials_process_failures_timeouts_and_output_bounds_are_safe(self):
        for purpose in ('component', 'metadata', 'server'):
            with self.subTest(purpose=purpose):
                scenario = copy.deepcopy(self.base)
                scenario.update(failPurpose=purpose, stderr=TOKEN_CANARY)
                self.assert_blocked(scenario, 'secret-shaped-output')
        encoded = ''.join('\\u%04x' % ord(c) for c in TOKEN_CANARY)
        scenario = copy.deepcopy(self.base)
        scenario.update(stderrPurpose='component', stderr='{"discarded":"' + encoded + '"}')
        self.assert_blocked(scenario, 'secret-shaped-output')
        scenario = copy.deepcopy(self.base)
        scenario.update(stderrPurpose='component', stderr='{"password":"discarded-private-password"}')
        self.assert_blocked(scenario, 'secret-shaped-output', secrets=('discarded-private-password',))
        failed = copy.deepcopy(self.base)
        failed['failPurpose'] = 'server'
        self.assert_blocked(failed, 'kubectl-backend-server-identity-exit-71')
        for purpose, code in [('context', 'current-context'), ('component', 'component-identity'),
                              ('server', 'backend-server-identity')]:
            with self.subTest(timeout=purpose):
                scenario = copy.deepcopy(self.base)
                scenario['hangPurpose'] = purpose
                self.assert_blocked(scenario, 'kubectl-' + code + '-timeout', timeout=1)
        for stderr in (False, True):
            with self.subTest(excessStderr=stderr):
                scenario = copy.deepcopy(self.base)
                scenario.update(excessPurpose='component', excessStderr=stderr)
                self.assert_blocked(scenario, 'kubectl-component-identity-output-too-large')

    def test_multiple_matching_lifecycle_pods_are_separately_attributed_and_conflicts_block(self):
        scenario = copy.deepcopy(self.base)
        second = copy.deepcopy(scenario['pods']['items'][0])
        second['metadata'].update(name='memories-access-telemetry-second', uid='second-uid')
        scenario['pods']['items'].append(second)
        scenario['metadata'][second['metadata']['name']] = copy.deepcopy(scenario['metadata'][self.pod])
        result, packets, _, _ = self.run_gate(scenario)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, len(packets[0]['observations']['loadedComponents']))
        conflicting = copy.deepcopy(scenario)
        conflicting['pods']['items'][1]['status']['containerStatuses'][1]['imageID'] = 'containerd://sha256:' + 'c' * 64
        self.assert_blocked(conflicting, 'running-target-image-drift')
        scenario['metadata'][second['metadata']['name']]['components'][0]['capabilities'].pop()
        self.assert_blocked(scenario, 'metadata-component-capabilities-mismatch')


    def successor_scenario(self):
        scenario = copy.deepcopy(self.base)
        scenario['server']['serverVersion'] = '18.6 (Debian 18.6-1.pgdg13+1)'
        scenario['server']['serverVersionNum'] = '180006'
        scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'docker-pullable://docker.io/library/postgres@sha256:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722'
        )
        return scenario

    def test_successor_exact_index_and_child_capture_is_immutable_neutral_and_source_bound(self):
        for child in (False, True):
            with self.subTest(child=child):
                scenario = self.successor_scenario()
                if child:
                    scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
                        'containerd://sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04'
                    )
                    scenario['pods']['items'][0]['status']['containerStatuses'][1]['imageID'] = (
                        'containerd://sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b'
                    )
                result, packets, calls, evidence = self.run_gate(scenario, profile='PG-ONPREM-2', opt_in=False, repeat=2)
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(2, len(packets))
                self.assertEqual(18, len(calls))
                for packet in packets:
                    self.assertEqual('PG-ONPREM-2', packet['profileId'])
                    self.assertEqual('7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe', packet['profileSha256'])
                    self.assertFalse(packet['historicalProfileCapture'])
                    self.assertEqual('approved-current', packet['profileDisposition'])
                    self.assertEqual('observed', packet['producerStatus'])
                    for field in ('gateStatus', 'productionLifecycleWrites', 'componentBehavior', 'connectionLinkage'):
                        self.assertEqual('not-evaluated', packet[field])
                    self.assertFalse(packet['productionGatePassed'])
                    self.assertEqual('180006', packet['observations']['backend']['serverVersionNum'])
                    self.assertEqual('peer:postgres', packet['observations']['backend']['systemUser'])
                    sources = {entry['source']: entry['sha256'] for entry in packet['sources']}
                    self.assertEqual(16, len([name for name in sources if name.startswith('deploy/')]))
                    for name, digest in sources.items():
                        if name.startswith(('deploy/', 'tools/')):
                            self.assertEqual(hashlib.sha256((REPO_ROOT / name).read_bytes()).hexdigest(), digest)
                for path in evidence.glob('*.json'):
                    self.assertFalse(path.stat().st_mode & stat.S_IWUSR)

    def test_successor_bare_index_and_child_image_ids_preserve_neutral_capture_and_raw_coherence(self):
        for child in (False, True):
            with self.subTest(child=child):
                scenario = self.successor_scenario()
                for source in ('pods', 'backendPods'):
                    for pod in scenario[source]['items']:
                        for status in pod['status']['containerStatuses']:
                            status['imageID'] = 'sha256:' + status['imageID'].rsplit('sha256:', 1)[-1]
                if child:
                    scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = 'sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04'
                    scenario['pods']['items'][0]['status']['containerStatuses'][1]['imageID'] = 'sha256:edbe3fc30d7efc90869411666fd03b70bb89eafed382bb37ff9a6de2fcab914b'
                result, packets, calls, _ = self.run_gate(scenario, profile='PG-ONPREM-2', opt_in=False)
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(9, len(calls))
                packet = packets[0]
                self.assertEqual('observed', packet['producerStatus'])
                self.assertEqual(scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'],
                                 packet['observations']['backend']['imageId'])
                for field in ('gateStatus', 'productionLifecycleWrites', 'componentBehavior', 'connectionLinkage'):
                    self.assertEqual('not-evaluated', packet[field])
                self.assertFalse(packet['productionGatePassed'])
                scenario['backendPodsAfter'] = copy.deepcopy(scenario['backendPods'])
                scenario['backendPodsAfter']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
                    'containerd://' + scenario['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'])
                self.assert_blocked(scenario, 'running-pod-changed', profile='PG-ONPREM-2', opt_in=False)

    def test_successor_rejects_historical_wrong_or_mixed_image_version_and_peer_evidence(self):
        cases = (
            ('serverVersion', '18.4', 'backend-version-invalid'),
            ('serverVersionNum', '180004', 'backend-version-invalid'),
            ('systemUser', 'trust:postgres', 'backend-peer-system-user-invalid'),
            ('systemUser', 'peer:memories_admin', 'backend-peer-system-user-invalid'),
        )
        for field, value, failure in cases:
            with self.subTest(field=field, value=value):
                scenario = self.successor_scenario()
                scenario['server'][field] = value
                self.assert_blocked(scenario, failure, profile='PG-ONPREM-2', opt_in=False)
        for source, container, digest, failure in (
            ('backendPods', 0, '3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a', 'backend-image-mismatch'),
            ('backendPods', 0, 'a' * 64, 'backend-image-mismatch'),
            ('pods', 1, 'a' * 64, 'runtime-image-mismatch'),
        ):
            with self.subTest(source=source, digest=digest):
                scenario = self.successor_scenario()
                scenario[source]['items'][0]['status']['containerStatuses'][container]['imageID'] = 'containerd://sha256:' + digest
                calls = self.assert_blocked(scenario, failure, profile='PG-ONPREM-2', opt_in=False)
                self.assertFalse(any('psql' in call for call in calls))
        scenario = self.successor_scenario()
        scenario['metadata'][self.pod]['runtimeVersion'] = '1.18.2'
        self.assert_blocked(scenario, 'metadata-identity-mismatch', profile='PG-ONPREM-2', opt_in=False)
        scenario = self.successor_scenario()
        scenario['backendPodsAfter'] = copy.deepcopy(scenario['backendPods'])
        scenario['backendPodsAfter']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'containerd://sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04'
        )
        self.assert_blocked(scenario, 'running-pod-changed', profile='PG-ONPREM-2', opt_in=False)
        scenario = self.successor_scenario()
        scenario['componentAfter'] = copy.deepcopy(scenario['component'])
        scenario['componentAfter']['resourceVersion'] = '42018'
        self.assert_blocked(scenario, 'component-changed', profile='PG-ONPREM-2', opt_in=False)

    def test_successor_source_drift_refuses_before_target_calls(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            tools = root / 'tools'
            tools.mkdir()
            for source in (RUNNER, HELPER):
                shutil.copy2(source, tools / source.name)
            proposal = REPO_ROOT / '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate/candidate-profile.json'
            identity = json.loads(proposal.read_text())['manifest']['canonical_profile']['identity']
            paths = list(identity['securityPlatform']['secretAndConfigurationInputs']) + [
                'deploy/openbao/values.yaml', 'deploy/kubernetes/base/access-telemetry-postgresql.yaml',
                'deploy/kubernetes/base/access-telemetry-deployments.yaml',
                'deploy/kubernetes/overlays/qualification/physical-evidence-reporter-job.yaml',
            ]
            for relative in paths:
                target = root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(REPO_ROOT / relative, target)
            target = root / 'deploy/openbao/values.yaml'
            target.write_bytes(target.read_bytes() + b'\n# drift\n')
            result, packets, calls, _ = self.run_gate(self.successor_scenario(), profile='PG-ONPREM-2',
                                                   opt_in=False, runner=tools / RUNNER.name)
            self.assertNotEqual(0, result.returncode)
            self.assertEqual([], calls)
            self.assertEqual(['approved-profile-input-drift'], packets[0]['blockers'])
            self.assertEqual({'component': None, 'lifecyclePods': [], 'loadedComponents': [], 'backend': None},
                             packets[0]['observations'])


if __name__ == '__main__':
    unittest.main()
