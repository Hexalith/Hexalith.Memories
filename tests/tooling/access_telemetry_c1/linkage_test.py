import copy
import hashlib
import json
import os
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import textwrap
import time
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RUNNER = REPO_ROOT / 'tools/verify-access-telemetry-c1-linkage.ps1'
HELPER = REPO_ROOT / 'tools/access-telemetry-c1-component-backend.ps1'
FIXTURE = Path(__file__).parent / 'fixtures/c1_16_complete.json'
PROFILE_HASH = '7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe'
CANARY = 'C1_SECRET_CANARY_DO_NOT_EMIT_7429'


def utc(value):
    return value.isoformat(timespec='microseconds').replace('+00:00', 'Z')


def write_fake_kubectl(directory):
    fake = directory / 'fake_kubectl.py'
    fake.write_text(textwrap.dedent(r'''
        import json
        import os
        import subprocess
        import sys
        import time
        from datetime import datetime, timedelta, timezone
        from pathlib import Path

        args = sys.argv[1:]
        scenario = json.loads(Path(os.environ['C1_SCENARIO']).read_text())
        probe_environment = os.environ.copy()
        if scenario.get('missingGrep'):
            probe_environment['PATH'] = str(Path(__file__).parent)
        log = Path(os.environ['C1_KUBECTL_LOG'])
        prior = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
        with log.open('a') as stream:
            stream.write(json.dumps(args) + '\n')
        after = False
        if args == ['config', 'current-context']:
            purpose = 'context'
        else:
            assert args[:4] == ['--context', 'jpiquot@local', '-n', 'hexalith-memories']
            if args[4:7] == ['get', 'component', 'access-telemetry-store']:
                purpose = 'component'
                after = any(call[4:7] == args[4:7] for call in prior)
                branch = args[-1].split('{{if eq .name "connectionString"}}')[1]
                assert '.value' not in branch
            elif args[4:6] == ['get', 'pods']:
                selector = args[args.index('-l') + 1]
                purpose = 'pods' if selector.endswith('=memories-access-telemetry') else 'backendPods'
                after = any('get' in call and selector in call for call in prior)
                assert args[-2:] == ['-o', "jsonpath-as-json={range .items[*]}{['metadata','status']}{end}"]
                if purpose == 'pods':
                    assert args[args.index('--field-selector') + 1] == 'metadata.name=' + scenario['pods']['items'][0]['metadata']['name']
            elif args[4] == 'exec' and args[7] == 'postgresql':
                purpose = 'sessionsAfter' if any('pg_stat_activity' in call[-1] for call in prior) else 'sessionsBefore'
                sql = args[-1]
                assert sql.startswith('WITH sessions AS MATERIALIZED (')
                for required in ['PGPASSFILE=/dev/null', '--no-password', '-X', '--host=/var/run/postgresql',
                                 'PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=5000']:
                    assert required in args
                assert args[args.index('PGHOSTADDR') - 1] == '-u'
                assert 'pg_stat_activity' in sql and 'pg_stat_ssl' in sql and 'clock_timestamp()' in sql
                assert 'LIMIT 41' in sql and "a.client_addr = '10.42.0.12'::inet" in sql
                assert 'a.query,' not in sql and 'application_name' not in sql and 'client_dn' not in sql
                assert not any(word in sql.upper() for word in ['INSERT ', 'UPDATE ', 'DELETE ', 'ALTER ', 'CREATE ', 'DROP '])
                if scenario.get('runPsqlEnvironment'):
                    environment_command = args[9:args.index('psql')] + [sys.executable, '-c', 'import os; assert "PGHOSTADDR" not in os.environ']
                    subprocess.run(environment_command, env=os.environ, check=True)
            elif args[4] == 'exec' and args[7] == 'lifecycle':
                purpose = 'read' if '/v1.0/state/' in args[-1] else 'metadata'
                assert args[8:11] == ['--', '/bin/sh', '-ec']
                if purpose == 'read':
                    assert 'nc -w 5 127.0.0.1 3500' in args[-1] and '?consistency=strong' in args[-1]
                    assert 'Connection: close' in args[-1] and 'validation=$?' in args[-1]
                    assert 'C1_NC_EXIT' not in args[-1]
                    Path(os.environ['C1_READ_TIME']).write_text(datetime.now(timezone.utc).isoformat())
                else:
                    assert '/v1.0/metadata' in args[-1]
                    assert 'nc -w 5 127.0.0.1 3500' in args[-1] and 'wget' not in args[-1]
            else:
                raise SystemExit(98)
        if scenario.get('hangPurpose') == purpose:
            Path(os.environ['C1_HANG_STARTED']).write_text(json.dumps({'pid': os.getpid(), 'started': time.time()}))
            time.sleep(5)
            Path(os.environ['C1_HANG_COMPLETED']).write_text('not terminated')
        if purpose == 'sessionsAfter' and scenario.get('delayAfterRead'):
            time.sleep(scenario['delayAfterRead'])
        if scenario.get('excessPurpose') == purpose:
            print('x' * (1024 * 1024 + 32), file=sys.stderr if scenario.get('excessStderr') else sys.stdout)
            raise SystemExit(0)
        if scenario.get('failPurpose') == purpose:
            print(scenario.get('stderr', 'observation refused'), file=sys.stderr)
            raise SystemExit(71)
        if scenario.get('stderrPurpose') == purpose:
            print(scenario['stderr'], file=sys.stderr)
        raw_key = purpose + ('After' if after else '') + 'Raw'
        if raw_key in scenario:
            print(scenario[raw_key])
        elif purpose == 'context':
            print(scenario['context'])
        elif purpose == 'component':
            print(json.dumps(scenario.get('componentAfter', scenario['component']) if after else scenario['component']))
        elif purpose in ('pods', 'backendPods'):
            payload = scenario.get(purpose + 'After', scenario[purpose]) if after else scenario[purpose]
            for pod in payload['items']:
                print(json.dumps([pod['metadata'], pod['status']]))
        elif purpose == 'metadata':
            if scenario.get('runMetadataProbe'):
                result = subprocess.run(args[9:], env=probe_environment, capture_output=True)
                sys.stdout.buffer.write(result.stdout)
                sys.stderr.buffer.write(result.stderr)
                raise SystemExit(result.returncode)
            body = json.dumps(scenario['metadata'][args[5]])
            sys.stdout.write('HTTP/1.1 200 OK\r\nContent-Length: ' + str(len(body.encode())) + '\r\n\r\n' + body)
        elif purpose == 'read':
            if scenario.get('runReadProbe'):
                result = subprocess.run(args[9:], env=probe_environment, capture_output=True)
                sys.stdout.buffer.write(result.stdout)
                sys.stderr.buffer.write(result.stderr)
                raise SystemExit(result.returncode)
            sys.stdout.write('HTTP/1.1 204 No Content\r\n\r\n')
        else:
            now = datetime.now(timezone.utc)
            read_time = datetime.fromisoformat(Path(os.environ['C1_READ_TIME']).read_text()) if purpose == 'sessionsAfter' else now - timedelta(seconds=10)
            if purpose == 'sessionsAfter':
                read_time += timedelta(seconds=scenario.get('activityOffsetSeconds', 0))
            def utc(value):
                return value.isoformat(timespec='microseconds').replace('+00:00', 'Z')
            session = {'pid': 137, 'usename': 'memories_access_telemetry_runtime', 'datname': 'memories_access_telemetry',
                       'clientAddr': '10.42.0.12', 'clientPort': 42318, 'backendStartUtc': '2026-01-01T00:00:00.000000Z',
                       'queryStartUtc': utc(read_time), 'stateChangeUtc': utc(read_time + timedelta(milliseconds=1)),
                       'state': 'idle', 'ssl': True, 'tlsVersion': 'TLSv1.3'}
            session.update(scenario.get(purpose + 'Session', {}))
            if scenario.get('reuseSession') and purpose == 'sessionsAfter':
                session.update(queryStartUtc=utc(now - timedelta(seconds=10)), stateChangeUtc=utc(now - timedelta(seconds=9)))
            value = {**scenario['server'], 'observedAtUtc': utc(now), 'sessions': [session]}
            value.update(scenario.get(purpose, {}))
            if scenario.get('duplicateSession') == purpose:
                value['sessions'].append({**session, 'pid': 138, 'clientPort': 42319})
            print(json.dumps(value))
    ''').strip() + '\n')
    executable = directory / 'kubectl'
    executable.write_text(f'#!/usr/bin/env sh\nexec "{sys.executable}" "{fake}" "$@"\n')
    executable.chmod(0o755)
    netcat = directory / 'nc'
    netcat.write_text(f'#!{sys.executable}\n' + textwrap.dedent('''
        import json
        import os
        import sys
        from pathlib import Path
        scenario = json.loads(Path(os.environ['C1_SCENARIO']).read_text())
        assert sys.argv[1:] == ['-w', '5', '127.0.0.1', '3500']
        request = sys.stdin.read()
        metadata = request.startswith('GET /v1.0/metadata ')
        assert metadata or request.startswith('GET /v1.0/state/access-telemetry-store/c1-linkage-absent-')
        assert 'dapr-api-token: ' + os.environ['DAPR_API_TOKEN'] in request
        with open(os.environ['C1_NC_CALLS'], 'a') as stream:
            stream.write(json.dumps({'mode': 'metadata' if metadata else 'state', 'destination': sys.argv[1:]}) + '\\n')
        if metadata:
            body = scenario.get('metadataHttpBody', json.dumps(next(iter(scenario['metadata'].values()))))
            headers = scenario.get('metadataHttpHeaders', 'HTTP/1.1 200 OK\\r\\nContent-Length: ' + str(len(body.encode())))
        else:
            body = scenario.get('httpBody', '')
            headers = scenario.get('httpHeaders', 'HTTP/1.1 204 No Content')
        sys.stdout.write(scenario.get('httpRaw', headers + '\\r\\n\\r\\n' + body))
        raise SystemExit(scenario.get('transportExit', 0))
    ''').strip() + '\n')
    netcat.chmod(0o755)


class ConnectionLinkageTests(unittest.TestCase):
    def setUp(self):
        self.base = json.loads(FIXTURE.read_text())
        for metadata in self.base['metadata'].values():
            metadata['components'][0]['capabilities'].append('ACTOR')
        self.pod = self.base['pods']['items'][0]['metadata']['name']
        self.base['server'].update(serverVersion='18.6 (Debian 18.6-1.pgdg13+1)', serverVersionNum='180006')
        self.base['backendPods']['items'][0]['status']['containerStatuses'][0]['imageID'] = (
            'containerd://sha256:5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722')
        for kind, ip in [('pods', '10.42.0.12'), ('backendPods', '10.42.0.15')]:
            pod = self.base[kind]['items'][0]
            pod['status']['podIP'] = ip
            for status in pod['status']['containerStatuses']:
                status.update(containerID='containerd://' + status['name'] + '-incarnation1', restartCount=0,
                              state={'running': {'startedAt': '2026-01-01T00:00:00Z'}})

    def run_collector(self, scenario=None, *, scope_changes=None, mode='Observe', gate='C1.16',
                      profile='PG-ONPREM-2', repeat=1, timeout=10, runner=RUNNER,
                      archive_file=False, expiry_seconds=None):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        binary = root / 'bin'
        binary.mkdir()
        write_fake_kubectl(binary)
        scenario_path = root / 'scenario.json'
        scenario_path.write_text(json.dumps(scenario or self.base))
        evidence = root / 'external-evidence'
        if archive_file:
            evidence.write_text('existing ordinary file')
        if scenario and scenario.get('failingGrep'):
            (binary / 'grep').write_text('#!/bin/sh\nexit 2\n')
            (binary / 'grep').chmod(0o755)
        now = datetime.now(timezone.utc)
        scope = {'schemaVersion': 'hexalith.access-telemetry.c1.linkage.scope/v1', 'gate': 'C1.16',
                 'profileId': 'PG-ONPREM-2', 'profileSha256': PROFILE_HASH, 'context': 'jpiquot@local',
                 'namespace': 'hexalith-memories', 'lifecyclePod': self.pod, 'evidenceDirectory': str(evidence),
                 'authorizedFromUtc': utc(now - timedelta(minutes=1)), 'authorizedUntilUtc': utc(now + timedelta(minutes=4)),
                 'authorizationReference': 'synthetic-offline-authorization', 'independentReviewer': 'synthetic-reviewer',
                 'exclusiveReadWindow': True, 'exclusiveReadWindowEvidenceSha256': '1' * 64,
                 'collectorSha256': hashlib.sha256(runner.read_bytes()).hexdigest(),
                 'identityHelperSha256': hashlib.sha256(HELPER.read_bytes()).hexdigest(),
                 'lifecycleImageSha256': 'a' * 64}
        scope.update(scope_changes or {})
        if expiry_seconds is not None:
            scope['authorizedUntilUtc'] = utc(now + timedelta(seconds=expiry_seconds))
        scope_path = root / 'scope.json'
        scope_path.write_text(json.dumps(scope))
        calls_path = root / 'calls.jsonl'
        env = os.environ.copy()
        env.update(PATH=str(binary) + os.pathsep + env.get('PATH', ''), C1_SCENARIO=str(scenario_path),
                   C1_KUBECTL_LOG=str(calls_path), C1_READ_TIME=str(root / 'read-time'), DAPR_API_TOKEN=CANARY,
                   C1_NC_CALLS=str(root / 'nc-calls'), C1_HANG_STARTED=str(root / 'hang-started'),
                   C1_HANG_COMPLETED=str(root / 'hang-completed'), PGHOSTADDR='192.0.2.99',
                   http_proxy='http://192.0.2.99:8080', HTTP_PROXY='http://192.0.2.99:8080')
        command = [shutil.which('pwsh'), '-NoProfile', str(runner), '-Gate', gate, '-ProfileId', profile, '-Mode', mode,
                   '-LifecyclePod', self.pod, '-ScopeFile', str(scope_path), '-EvidenceDirectory', str(evidence),
                   '-CommandTimeoutSeconds', str(timeout)]
        for _ in range(repeat):
            # Each run is an independent synthetic observation; preserve prior immutable packets.
            calls_path.unlink(missing_ok=True)
            started = time.monotonic()
            result = subprocess.run(command, cwd=REPO_ROOT, env=env, text=True, capture_output=True, timeout=20)
            self.last_elapsed = time.monotonic() - started
            self.last_root = root
            if result.returncode:
                break
        paths = sorted(evidence.glob('*.json'))
        packets = [json.loads(path.read_text()) for path in paths]
        calls = [json.loads(line) for line in calls_path.read_text().splitlines()] if calls_path.exists() else []
        captured = result.stdout + result.stderr + ''.join(path.read_text() for path in paths)
        self.assertNotIn(CANARY, captured)
        self.assertNotRegex(captured, r'c1-linkage-absent-[0-9a-f]{64}')
        return result, packets, calls, evidence, captured

    def assert_blocked(self, scenario, expected=None, **kwargs):
        result, packets, calls, _, captured = self.run_collector(scenario, **kwargs)
        self.assertNotEqual(0, result.returncode, captured)
        self.assertEqual(1, len(packets), captured)
        packet = packets[0]
        self.assertEqual('blocked', packet['collectorStatus'])
        self.assertIsNone(packet['observations'])
        self.assertEqual('not-evaluated', packet['connectionLinkage'])
        self.assertEqual('not-evaluated', packet['gateStatus'])
        self.assertFalse(packet['productionGatePassed'])
        if expected:
            self.assertEqual([expected], packet['blockers'], captured)
        return calls, captured

    def test_complete_attribution_emits_unique_immutable_neutral_packets(self):
        result, packets, calls, evidence, captured = self.run_collector(repeat=2)
        self.assertEqual(0, result.returncode, captured)
        self.assertEqual(2, len(packets))
        for path in evidence.glob('*.json'):
            self.assertTrue(path.name.startswith('c1.16-connection-linkage-candidate-'))
            self.assertFalse(path.stat().st_mode & stat.S_IWUSR)
        packet = packets[0]
        self.assertEqual('observed', packet['collectorStatus'])
        self.assertEqual(PROFILE_HASH, packet['profileSha256'])
        for field in ['gateStatus', 'connectionLinkage', 'componentBehavior', 'productionLifecycleWrites']:
            self.assertEqual('not-evaluated', packet[field])
        self.assertEqual('pending', packet['independentDisposition'])
        self.assertFalse(packet['productionGatePassed'])
        observed = packet['observations']
        self.assertEqual(137, observed['after']['sessions'][0]['pid'])
        self.assertEqual('10.42.0.12', observed['lifecyclePod']['podIp'])
        self.assertTrue(observed['stateRead']['syntheticKeyAbsent'])
        self.assertEqual('candidate-session-correlation-requires-independent-exclusive-window-verification', observed['attribution'])
        for source in [RUNNER, HELPER]:
            entry = next(item for item in packet['sources'] if item['source'] == 'tools/' + source.name)
            self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(), entry['sha256'])
        self.assertTrue(all(re.fullmatch('[0-9a-f]{64}', item['sha256']) for item in packet['sources'] + packet['commands']))
        self.assertEqual(11, len(calls))
        self.assertEqual(1, sum('/v1.0/state/' in call[-1] for call in calls))
        self.assertFalse(any('secret' in call or 'secrets' in call for call in calls))

    def test_invalid_literals_precede_calls_and_directory_creation(self):
        for changes in [{'mode': 'observe'}, {'mode': 'Accept'}, {'profile': 'PG-ONPREM-1'},
                        {'profile': 'pg-onprem-2'}, {'gate': 'C1.15'}, {'gate': 'c1.16'}]:
            with self.subTest(changes=changes):
                result, packets, calls, evidence, _ = self.run_collector(**changes)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], calls)
                self.assertEqual([], packets)
                self.assertFalse(evidence.exists())

    def test_scope_and_source_mismatches_precede_calls_and_directory_creation(self):
        for changes in [{'profileSha256': 'a' * 64}, {'context': 'other'}, {'namespace': 'another-tenant'},
                        {'lifecyclePod': 'another-pod'}, {'exclusiveReadWindow': False},
                        {'collectorSha256': 'a' * 64}, {'identityHelperSha256': 'a' * 64},
                        {'authorizedUntilUtc': '2026-01-01T00:00:00Z'}, {'evidenceDirectory': '/different'},
                        {'password': CANARY}, {'authorizedFromUtc': '2020-01-01T00:00:00Z'}]:
            with self.subTest(changes=changes):
                result, packets, calls, evidence, _ = self.run_collector(scope_changes=changes)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], calls)
                self.assertEqual([], packets)
                self.assertFalse(evidence.exists())

    def test_wrong_context_blocks_before_pod_or_backend_calls(self):
        scenario = copy.deepcopy(self.base)
        scenario['context'] = 'wrong-context'
        calls, _ = self.assert_blocked(scenario, 'profile-context-mismatch')
        self.assertEqual([['config', 'current-context']], calls)

    def test_approved_configuration_drift_precedes_calls_and_directory_creation(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        repository = Path(temporary.name)
        for source in [RUNNER, HELPER]:
            target = repository / 'tools' / source.name
            target.parent.mkdir(exist_ok=True)
            shutil.copyfile(source, target)
        inputs = re.findall(r"'(deploy/[^']+)' = '[0-9a-f]{64}'", RUNNER.read_text())
        self.assertEqual(16, len(inputs))
        for relative in inputs:
            target = repository / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO_ROOT / relative, target)
        (repository / inputs[0]).write_bytes(b'drift')
        result, packets, calls, evidence, captured = self.run_collector(runner=repository / 'tools' / RUNNER.name)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('approved-profile-input-drift', captured)
        self.assertEqual([], calls)
        self.assertEqual([], packets)
        self.assertFalse(evidence.exists())

    def test_drifted_helper_is_rejected_before_loading_its_top_level_code(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        repository = Path(temporary.name)
        tools = repository / 'tools'
        tools.mkdir()
        shutil.copyfile(RUNNER, tools / RUNNER.name)
        helper = tools / HELPER.name
        shutil.copyfile(HELPER, helper)
        marker = repository / 'helper-must-not-execute'
        with helper.open('a') as stream:
            stream.write("\n[IO.File]::WriteAllText('" + str(marker) + "', 'unauthorized helper execution')\n")
        result, packets, calls, evidence, captured = self.run_collector(runner=tools / RUNNER.name)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('linkage-approved-source-drift', captured)
        self.assertEqual([], calls)
        self.assertEqual([], packets)
        self.assertFalse(evidence.exists())
        self.assertFalse(marker.exists())

    def test_helper_replacement_after_verification_cannot_execute_replacement_bytes(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        repository = Path(temporary.name)
        tools = repository / 'tools'
        tools.mkdir()
        helper = tools / HELPER.name
        shutil.copyfile(HELPER, helper)
        marker = repository / 'replacement-must-not-execute'
        replacement = "[IO.File]::WriteAllText('" + str(marker) + "', 'unapproved execution')"
        mutation = "[IO.File]::AppendAllText('" + str(helper) + "', \"`n" + replacement + "`n\")\n. $approvedHelper"
        collector = tools / RUNNER.name
        collector.write_text(RUNNER.read_text().replace('. $approvedHelper', mutation))
        result, packets, calls, evidence, captured = self.run_collector(runner=collector)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('linkage-approved-source-drift', captured)
        self.assertFalse(marker.exists())
        self.assertEqual([], calls)
        self.assertEqual([], packets)
        self.assertFalse(evidence.exists())

    def test_archive_file_is_rejected_before_contact(self):
        result, packets, calls, evidence, captured = self.run_collector(archive_file=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('linkage-archive-is-file', captured)
        self.assertEqual([], packets)
        self.assertEqual([], calls)
        self.assertTrue(evidence.is_file())
        self.assertEqual('existing ordinary file', evidence.read_text())

    def test_session_predating_selected_container_incarnation_blocks(self):
        for kind, container in [('pods', 'daprd'), ('backendPods', 'postgresql')]:
            with self.subTest(kind=kind):
                scenario = copy.deepcopy(self.base)
                status = next(item for item in scenario[kind]['items'][0]['status']['containerStatuses'] if item['name'] == container)
                status['state']['running']['startedAt'] = '2026-01-02T00:00:00Z'
                calls, _ = self.assert_blocked(scenario, 'session-predates-container-incarnation')
                self.assertFalse(any('/v1.0/state/' in call[-1] for call in calls))

    def test_private_challenge_command_hash_is_reconstructable(self):
        result, packets, calls, _, captured = self.run_collector()
        self.assertEqual(0, result.returncode, captured)
        command = next(call for call in calls if '/v1.0/state/' in call[-1])
        normalized = [re.sub(r'c1-linkage-absent-[0-9a-f]{64}', '__KEY__', argument) for argument in command]
        identity = 'kubectl ' + '\x1f'.join(normalized)
        ledger = next(item for item in packets[0]['commands'] if item['purpose'] == 'linkage-absent-state-get')
        self.assertEqual({'purpose', 'sha256'}, set(ledger))
        self.assertEqual(hashlib.sha256(identity.encode('utf-8')).hexdigest(), ledger['sha256'])

    def test_inherited_pghostaddr_is_removed_from_executed_environment(self):
        result, packets, calls, _, captured = self.run_collector({**self.base, 'runPsqlEnvironment': True})
        self.assertEqual(0, result.returncode, captured)
        self.assertTrue(packets[0]['observations']['before']['localSocket'])
        self.assertNotIn('192.0.2.99', captured)
        sql_calls = [call for call in calls if 'pg_stat_activity' in call[-1]]
        self.assertEqual(2, len(sql_calls))
        self.assertTrue(all(call[call.index('PGHOSTADDR') - 1] == '-u' for call in sql_calls))

    def test_advancing_activity_outside_get_interval_blocks(self):
        for change in [{'activityOffsetSeconds': -2}, {'activityOffsetSeconds': 1.5, 'delayAfterRead': 2}]:
            with self.subTest(change=change):
                self.assert_blocked({**self.base, **change}, 'session-activity-not-attributable')

    def test_running_command_is_terminated_at_authorization_deadline(self):
        self.assert_blocked({**self.base, 'hangPurpose': 'context'}, 'kubectl-current-context-timeout', expiry_seconds=3)
        started = self.last_root / 'hang-started'
        self.assertTrue(started.exists(), 'The authorized command must have started before its deadline.')
        self.assertFalse((self.last_root / 'hang-completed').exists())
        self.assertLess(self.last_elapsed, 4.5, 'The five-second command must be terminated at the three-second scope deadline.')
        pid = json.loads(started.read_text())['pid']
        status = Path('/proc') / str(pid) / 'status'
        if status.exists():
            self.assertRegex(status.read_text(), r'State:\s+Z', 'The command must no longer be running.')

    def test_missing_duplicate_or_concurrent_sessions_block(self):
        for purpose in ['sessionsBefore', 'sessionsAfter']:
            for change in [{purpose: {'sessions': []}}, {'duplicateSession': purpose}]:
                with self.subTest(change=change):
                    self.assert_blocked({**self.base, **change}, 'session-attribution-ambiguous')

    def test_stale_malformed_and_duplicate_json_block(self):
        for change, blocker in [({'sessionsBefore': {'observedAtUtc': '2026-01-01T00:00:00Z'}}, 'session-observation-stale'),
                                ({'sessionsBeforeRaw': '{'}, 'malformed-session-json'),
                                ({'sessionsBeforeRaw': '{"sessions":[],"sessions":[]}'}, 'malformed-session-json'),
                                ({'readRaw': '{"httpStatus":204,"httpStatus":200}'}, 'invalid-loopback-http-response')]:
            with self.subTest(change=change):
                self.assert_blocked({**self.base, **change}, blocker)

    def test_wrong_ip_role_database_tls_active_and_timestamp_fields_block(self):
        for changes in [{'clientAddr': '10.42.0.99'}, {'usename': 'other-tenant-role'}, {'datname': 'other-tenant-db'},
                        {'ssl': False}, {'tlsVersion': 'TLSv1.1'}, {'state': 'active'}, {'pid': '137'},
                        {'clientPort': 0}, {'queryStartUtc': None}, {'query': 'tenant-query-must-not-export'},
                        {'usename': ['memories_access_telemetry_runtime']}, {'state': ['idle']}]:
            with self.subTest(changes=changes):
                self.assert_blocked({**self.base, 'sessionsBeforeSession': changes})

    def test_no_activity_or_replaced_pool_session_blocks(self):
        self.assert_blocked({**self.base, 'reuseSession': True}, 'session-activity-not-attributable')
        for changes in [{'pid': 138}, {'clientPort': 42319}, {'backendStartUtc': '2026-01-02T00:00:00Z'}]:
            with self.subTest(changes=changes):
                self.assert_blocked({**self.base, 'sessionsAfterSession': changes}, 'session-replaced-or-reused')

    def test_wrong_version_or_peer_is_rejected_before_state_get(self):
        for changes in [{'serverVersionNum': '180004'}, {'serverVersion': '18.4'}, {'systemUser': 'scram:runtime'}, {'localSocket': False}]:
            with self.subTest(changes=changes):
                scenario = copy.deepcopy(self.base)
                scenario['server'].update(changes)
                calls, _ = self.assert_blocked(scenario)
                self.assertFalse(any('/v1.0/state/' in call[-1] for call in calls))

    def test_wrong_images_and_component_scope_block_before_challenge(self):
        for kind in ['pods', 'backendPods']:
            scenario = copy.deepcopy(self.base)
            statuses = scenario[kind]['items'][0]['status']['containerStatuses']
            statuses[-1]['imageID'] = 'containerd://sha256:' + 'f' * 64
            calls, _ = self.assert_blocked(scenario)
            self.assertFalse(any('/v1.0/state/' in call[-1] for call in calls))
        scenario = copy.deepcopy(self.base)
        scenario['component']['scopes'] = ['another-tenant-app']
        self.assert_blocked(scenario, 'component-scopes-mismatch')

    def test_changed_pods_and_container_incarnations_cannot_publish_partial_observations(self):
        for kind in ['pods', 'backendPods']:
            for field in ['uid', 'podIP', 'containerID', 'restartCount']:
                with self.subTest(kind=kind, field=field):
                    scenario = copy.deepcopy(self.base)
                    scenario[kind + 'After'] = copy.deepcopy(scenario[kind])
                    pod = scenario[kind + 'After']['items'][0]
                    if field == 'uid':
                        pod['metadata']['uid'] = 'changed-uid'
                    elif field == 'podIP':
                        pod['status']['podIP'] = '10.42.0.99'
                    else:
                        status = pod['status']['containerStatuses'][0]
                        status[field] = 'containerd://replaced' if field == 'containerID' else 1
                    self.assert_blocked(scenario, 'selected-identity-changed')

    def test_timeout_output_bounds_and_denial_are_secret_safe(self):
        for change in [{'failPurpose': 'sessionsBefore'}, {'hangPurpose': 'read'},
                       {'excessPurpose': 'sessionsBefore'}, {'excessPurpose': 'sessionsBefore', 'excessStderr': True}]:
            with self.subTest(change=change):
                self.assert_blocked({**self.base, **change}, timeout=1)

    def test_secret_shaped_stdout_stderr_and_decoded_fields_are_not_retained(self):
        for change in [{'sessionsBeforeRaw': json.dumps({'password': CANARY})},
                       {'stderrPurpose': 'sessionsBefore', 'stderr': json.dumps({'connectionString': CANARY})},
                       {'failPurpose': 'read', 'stderr': '{"to\\u006ben":"' + CANARY + '"}'}]:
            with self.subTest(change=change):
                calls, captured = self.assert_blocked({**self.base, **change}, 'secret-shaped-output')
                self.assertNotIn(CANARY, captured)

    def test_probe_discards_headers_body_key_and_denies_non_absence_or_redirects(self):
        scenario = {**self.base, 'runReadProbe': True}
        result, packets, _, _, captured = self.run_collector(scenario)
        self.assertEqual(0, result.returncode, captured)
        self.assertEqual('observed', packets[0]['collectorStatus'])
        for headers in ['HTTP/1.1 200 OK', 'HTTP/1.1 401 Unauthorized',
                        'HTTP/1.1 302 Found\r\nHTTP/1.1 204 No Content',
                        'HTTP/1.1 204 No Content\r\nsecret: ' + CANARY]:
            with self.subTest(headers=headers):
                self.assert_blocked({**self.base, 'runReadProbe': True, 'httpHeaders': headers})
        for change in [{'transportExit': 1}, {'httpBody': CANARY}, {'httpHeaders': 'HTTP/1.1 204 No Content\r\nX-Test: ' + 'x' * 20000}]:
            with self.subTest(change=change):
                self.assert_blocked({**self.base, 'runReadProbe': True, **change})

    def test_actual_probe_cannot_spoof_transport_status_or_hide_body_bytes(self):
        for change in [
            {'transportExit': 1, 'httpBody': '\nC1_NC_EXIT 0\n' + 'x' * 20000},
            {'httpHeaders': 'HTTP/1.1 204 No Content\r\nC1_NC_EXIT 0', 'transportExit': 1},
            {'httpBody': '\n'}, {'httpBody': '\r\n'}, {'httpBody': '\n\n'}, {'httpBody': '\x00'},
            {'httpRaw': 'HTTP/1.1 204 No Content\r\nX-Test: truncated'},
            {'httpHeaders': 'HTTP/1.1 204 No Content\r\nContent-Length: 1'}
        ]:
            with self.subTest(change=change):
                self.assert_blocked({**self.base, 'runReadProbe': True, **change})

    def test_actual_probe_validator_errors_precede_requests(self):
        for phase in ['runReadProbe', 'runMetadataProbe']:
            for change in [{'missingGrep': True}, {'failingGrep': True}]:
                with self.subTest(phase=phase, change=change):
                    self.assert_blocked({**self.base, phase: True, **change})
                    self.assertFalse((self.last_root / 'nc-calls').exists())

    def test_actual_metadata_client_refuses_redirects_and_ignores_proxies(self):
        result, packets, _, _, captured = self.run_collector({**self.base, 'runMetadataProbe': True})
        self.assertEqual(0, result.returncode, captured)
        self.assertEqual('observed', packets[0]['collectorStatus'])
        trace = [json.loads(line) for line in (self.last_root / 'nc-calls').read_text().splitlines()]
        self.assertEqual([{'mode': 'metadata', 'destination': ['-w', '5', '127.0.0.1', '3500']}], trace)
        calls, _ = self.assert_blocked({**self.base, 'runMetadataProbe': True,
                                       'metadataHttpHeaders': 'HTTP/1.1 302 Found\r\nLocation: http://192.0.2.99/token-sink'},
                                      'loopback-http-status-refused')
        trace = [json.loads(line) for line in (self.last_root / 'nc-calls').read_text().splitlines()]
        self.assertEqual(1, len(trace))
        self.assertEqual(['-w', '5', '127.0.0.1', '3500'], trace[0]['destination'])
        self.assertFalse(any('/v1.0/state/' in call[-1] for call in calls))


if __name__ == '__main__':
    unittest.main()
