"""Offline fixed-target render and bounded transport tests; no live credit."""

from datetime import datetime, timezone
from contextlib import ExitStack
from copy import deepcopy
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import signal
import shutil
import subprocess
import tarfile
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools'))
import access_telemetry_functional_guest as guest
import access_telemetry_functional_retention as functional
import access_telemetry_functional_target as target


class RenderTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix='functional-render-offline-')
        cls.directory = Path(cls.temp.name)
        original_local = target._local
        def local(argv, seconds=120):
            if argv[0] == '/usr/local/bin/kubectl':
                installed = shutil.which('kubectl')
                if installed is None:
                    raise RuntimeError('offline-render-requires-kubectl')
                argv = [installed, *argv[1:]]
            return original_local(argv, seconds)
        cls.local_patch = patch.object(target, '_local', side_effect=local)
        cls.local_patch.start()
        target._local(['/usr/bin/openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '3',
                       '-subj', '/CN=memories-functional-ca', '-keyout', str(cls.directory / 'ca.key'), '-out', str(cls.directory / 'ca.crt')])
        target._local(['/usr/bin/openssl', 'genpkey', '-algorithm', 'EC', '-pkeyopt', 'ec_paramgen_curve:P-256', '-out', str(cls.directory / 'clock.key')])
        images = {name: {'manifestDigest': 'sha256:' + '1' * 64} for name in ('memories', 'memories-access-telemetry', 'memories-access-telemetry-clock')}
        cls.config = target._render(cls.directory, ROOT, images)

    @classmethod
    def tearDownClass(cls):
        cls.local_patch.stop()
        cls.temp.cleanup()

    def test_only_three_inert_application_deployments_and_real_dependencies(self):
        apps = [item for item in self.config['applicationObjects'] if item['kind'] == 'Deployment']
        self.assertEqual({'memories', 'memories-access-telemetry', 'memories-access-telemetry-clock'}, {item['metadata']['name'] for item in apps})
        self.assertTrue(all(item['spec']['replicas'] == 0 for item in apps))
        components = {item['metadata']['name'] for item in self.config['applicationObjects'] if item['kind'] == 'Component'}
        self.assertTrue({'statestore', 'pubsub', 'access-telemetry-store', 'access-telemetry-config', 'access-telemetry-secrets'} <= components)
        self.assertFalse(any(item['kind'] == 'Job' for item in self.config['applicationObjects']))

    def test_development_clock_removes_invalid_external_sources_and_keeps_key_validation(self):
        apps = {item['metadata']['name']: item for item in self.config['applicationObjects'] if item['kind'] == 'Deployment'}
        clock_env = {item['name']: item.get('value') for item in apps['memories-access-telemetry-clock']['spec']['template']['spec']['containers'][0]['env']}
        self.assertEqual(clock_env['Clock__AllowDevelopmentSources'], 'true')
        self.assertFalse(any(name.startswith('Clock__Sources__') for name in clock_env))
        for app in apps.values():
            env = {item['name']: item.get('value') for item in app['spec']['template']['spec']['containers'][0]['env']}
            self.assertEqual(env['DOTNET_ENVIRONMENT'], 'Qualification')
            self.assertEqual(env['AccessTelemetryLifecycle__ConfigurationEpoch'], '01K7P000000000000000000000')
            self.assertTrue(env['AccessTelemetryLifecycle__AttestationVerificationKey'])

    def test_pvc_capacity_is_one_400_gib_claim_within_existing_data_disk(self):
        volumes = [item for item in self.config['bootstrapObjects'] if item['kind'] == 'PersistentVolume']
        self.assertEqual(sum(int(item['spec']['capacity']['storage'][:-2]) for item in volumes), 541)
        scheduler = [item for item in volumes if item['spec']['claimRef']['namespace'] == 'dapr-system']
        self.assertEqual({item['spec']['claimRef']['name'] for item in scheduler}, {'dapr-scheduler-data-dir-dapr-scheduler-server-' + str(index) for index in range(3)})
        self.assertTrue(all(item['spec']['capacity']['storage'] == '17Gi' for item in scheduler))
        self.assertEqual(sum(item['spec']['capacity']['storage'] == '400Gi' for item in volumes), 1)
        self.assertTrue(all(item['spec']['persistentVolumeReclaimPolicy'] == 'Retain' for item in volumes))
        for item in self.config['applicationObjects']:
            if item['kind'] == 'StatefulSet':
                if item['metadata']['name'] in {'redis-stack', 'falkordb'}:
                    security = item['spec']['template']['spec']['securityContext']
                    self.assertEqual(security['fsGroup'], 1000)
                    self.assertEqual(security['fsGroupChangePolicy'], 'OnRootMismatch')
                    self.assertEqual(security['seccompProfile']['type'], 'RuntimeDefault')
                for claim in item['spec']['volumeClaimTemplates']:
                    self.assertEqual(claim['spec']['storageClassName'], 'memories-functional-retain')
                    self.assertTrue(claim['spec']['volumeName'].startswith('memories-functional-'))

    def test_postgresql_tls_certificate_has_real_full_service_san(self):
        output = target._local(['/usr/bin/openssl', 'x509', '-in', str(self.directory / 'postgresql.crt'), '-noout', '-ext', 'subjectAltName'])
        self.assertIn(('DNS:access-telemetry-postgresql.' + target.NS + '.svc.cluster.local').encode(), output)
        connection = self.config['vaultValues']['hexalith/memories/access-telemetry']['access-telemetry-postgresql']['connectionString']
        self.assertIn('sslmode=verify-full', connection)
        self.assertIn('sslrootcert=/mnt/access-telemetry-postgresql/ca.crt', connection)

    def test_source_render_is_reproducible_without_generating_new_private_material(self):
        images = {name: {'manifestDigest': 'sha256:' + '1' * 64} for name in ('memories', 'memories-access-telemetry', 'memories-access-telemetry-clock')}
        before = {path.name: path.read_bytes() for path in self.directory.iterdir()}
        rendered = target._render(self.directory, ROOT, images, verify_existing=True)
        self.assertEqual(rendered, self.config)
        self.assertEqual(before, {path.name: path.read_bytes() for path in self.directory.iterdir()})

    def test_guest_input_bindings_cover_downloads_values_and_each_infrastructure_image(self):
        images = {name: {'archivePath': str(self.directory / 'assets' / (name + '.tar')), 'archiveSha256': '2' * 64} for name in ('memories', 'memories-access-telemetry', 'memories-access-telemetry-clock')}
        config, values = target._complete_config(deepcopy(self.config), self.directory, images, '3' * 64)
        catalog = functional.artifact_catalog()
        for item in catalog['downloads']:
            self.assertEqual(config['fileDigests'][item['path']], item['sha256'])
        self.assertEqual(config['imageIdentities'], catalog['images'])
        raw = json.dumps(values, sort_keys=True, separators=(',', ':')).encode()
        self.assertEqual(config['fileDigests']['dapr-values.json'], hashlib.sha256(raw).hexdigest())


class ObservationTests(unittest.TestCase):
    def driver(self):
        instance = target.FixedFunctionalTarget.__new__(target.FixedFunctionalTarget)
        instance.segments = {'cohort-a': 1800000000000}
        instance.ttl_absent = set()
        instance.before_sequence = 1
        instance._record = lambda *_: None
        return instance

    def test_ttl_attribution_requires_positive_physical_tuple_and_strong_absence(self):
        instance = self.driver()
        ids = functional.fixed_record_keys('offline-run', 'cohort-a')
        record_ids = [item.rsplit('/', 1)[1] for item in ids]
        instance._states = lambda *_args, **kwargs: [] if kwargs.get('buckets') else [{'value': None} for _ in ids]
        instance._actor_state = lambda: {'ReminderSequence': 2, 'LastPurgeUnixMilliseconds': 1800003600500}
        def kubectl(*args):
            return json.dumps({'items': [{'metadata': {'name': 'postgresql-offline'}}]} if args[0] == 'get' else {'expiredRecordIds': record_ids[:1]}).encode()
        instance._kubectl = kubectl
        result = instance.observe_purge('offline-run', 'cohort-a')
        self.assertEqual(result['ttlAbsentRecordIds'], record_ids[:1])
        self.assertEqual(result['actorDeletedRecordIds'], [])
        self.assertEqual(len(result['absentRecordIds']), 125)

    def test_record_time_conversion_does_not_round_milliseconds(self):
        instance = self.driver()
        instance._states = lambda *_: [{'value': {'recordId': 'offline-record', 'envelopeHash': '1' * 64,
            'emittedAtUtc': '2026-10-16T12:00:00.123Z', 'expiresAtUtc': '2026-10-16T13:00:00.123Z', 'acceptedAtUtc': '2026-10-16T12:00:01.124Z'}}]
        result = instance.read_cohort('offline-run', 'cohort-a')['records'][0]
        self.assertEqual(result['expiresUtcMs'] - result['emittedUtcMs'], 3600000)
        self.assertEqual(result['acceptedUtcMs'] - result['emittedUtcMs'], 1001)

    def test_malformed_actor_state_cannot_supply_duplicate_case_identity(self):
        instance = self.driver()
        instance._exec = lambda *_: json.dumps({'ReminderSequence': 1, 'reminderSequence': 2, 'RetainedRecordCount': 125, 'LastPurgeUnixMilliseconds': None}).encode()
        with self.assertRaises(functional.FunctionalRetentionError):
            instance._actor_state()

    def test_guest_child_output_and_time_are_bounded(self):
        with self.assertRaisesRegex(RuntimeError, 'stream-bound'):
            guest.run([sys.executable, '-c', 'import sys; sys.stdout.write("x"*2097152)'])
        with self.assertRaisesRegex(RuntimeError, 'command-deadline'):
            guest.run([sys.executable, '-c', 'import time; time.sleep(10)'], seconds=.05)

    def test_foreign_lease_prevents_each_restoration_mutation(self):
        instance = self.driver()
        instance.lease = {'metadata': {'uid': 'owned-lease'}}
        instance.gate = {'metadata': {'uid': 'owned-gate'}}
        instance.holder = 'owned-run'
        instance.released_lease_version = None
        instance.deployments = {'memories': 'writer', 'memories-access-telemetry': 'lifecycle', 'memories-access-telemetry-clock': 'clock'}
        instance._get = lambda *_args, **_kwargs: {'metadata': {'uid': 'owned-lease', 'resourceVersion': '9'}, 'spec': {'holderIdentity': 'foreign-run'}}
        instance._patch = unittest.mock.Mock()
        for name in ('disable_owned_gate', 'release_owned_lease', 'scale_owned_services_zero'):
            with self.subTest(method=name), self.assertRaises(functional.FunctionalRetentionError):
                getattr(instance, name)()
        instance._patch.assert_not_called()
        instance.released_lease_version = '8'
        instance._get = lambda *_args, **_kwargs: {'metadata': {'uid': 'owned-lease', 'resourceVersion': '9'}, 'spec': {'holderIdentity': ''}}
        with self.assertRaises(functional.FunctionalRetentionError):
            instance._restore_owner()

    def test_unavailable_qmp_reaches_identity_checked_process_termination(self):
        instance = self.driver()
        alive = [True]
        instance.identity = {'pid': 123456789}
        instance.process = unittest.mock.Mock()
        instance.process.poll.side_effect = lambda: None if alive[0] else 0
        instance.process.wait.side_effect = [subprocess.TimeoutExpired('owned-test-wrapper', 70), 0]
        instance.process.stdout = io.BytesIO()
        instance.process.stderr = io.BytesIO()
        instance._same_vm = lambda: alive[0]
        instance._qmp = unittest.mock.Mock(side_effect=OSError('offline-qmp-unavailable'))
        instance._pump_vm = lambda **_kwargs: None
        instance.vm_buffers = [bytearray(), bytearray()]
        instance.transport = {'diskIdentities': {}}
        instance.stopped_readback = lambda: True
        times = iter((0, 31))
        with patch.object(target.time, 'monotonic', side_effect=lambda: next(times)), patch.object(target.os, 'kill', side_effect=lambda *_: alive.__setitem__(0, False)) as kill:
            self.assertTrue(instance.stop_owned_guest())
        kill.assert_called_once_with(123456789, signal.SIGTERM)
        self.assertEqual([call.args[0] for call in instance._qmp.call_args_list], ['system_powerdown', 'quit'])

    def test_setup_resource_monitor_checks_floors_at_bounded_intervals(self):
        instance = self.driver()
        instance._resource_check_active = False
        instance._last_resource_check = 0
        instance.resource_floors = unittest.mock.Mock(return_value=True)
        with patch.object(target.time, 'monotonic', return_value=31):
            instance._monitor_floors()
        instance.resource_floors.assert_called_once()
        instance._resource_check_active = True
        with patch.object(target.time, 'monotonic', return_value=62):
            instance._monitor_floors()
        instance.resource_floors.assert_called_once()

    def test_unseal_material_is_stdin_and_absent_from_exec_arguments(self):
        initialized = {'root_token': 'offline-root-material', 'unseal_keys_b64': ['offline-unseal-material']}
        with patch.object(guest, 'bao', side_effect=[json.dumps(initialized).encode(), RuntimeError('offline-stop-after-unseal')]) as bao:
            with self.assertRaisesRegex(RuntimeError, 'offline-stop-after-unseal'):
                guest.secrets_and_apps({})
        args, kwargs = bao.call_args_list[1]
        self.assertEqual(args, ('write', '-format=json', 'sys/unseal', '-'))
        self.assertEqual(json.loads(kwargs['data']), {'key': initialized['unseal_keys_b64'][0]})


class NativeOsHashTests(unittest.TestCase):
    def setUp(self):
        self.instance = target.FixedFunctionalTarget.__new__(target.FixedFunctionalTarget)
        self.instance.authority = unittest.mock.Mock()
        self.instance._record = unittest.mock.Mock()
        self.size = 40 * 1024**3
        self.linux = SimpleNamespace(st_dev=1, st_ino=2, st_mode=0o100600, st_nlink=1,
                                     st_uid=1000, st_gid=1000, st_size=self.size,
                                     st_mtime_ns=100, st_ctime_ns=200)
        metadata = {'fullName': target._NATIVE_OS_PATH, 'length': self.size,
                    'creationUtcTicks': 100, 'lastWriteUtcTicks': 200, 'attributes': 32}
        self.native = {'machine': 'DESKTOP-VIOG240', 'sha256': target.RETAINED_OS_SHA256,
                       'bytesHashed': self.size, 'startPosition': 0, 'endPosition': self.size,
                       'streamLengthBefore': self.size, 'streamLengthAfter': self.size,
                       'readOnly': True, 'unchanged': True, 'before': metadata, 'after': deepcopy(metadata)}
        self.instance._run = unittest.mock.Mock(return_value=json.dumps(self.native).encode())
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        self.open = self.stack.enter_context(patch.object(target.os, 'open', return_value=731))
        self.close = self.stack.enter_context(patch.object(target.os, 'close'))
        self.fstat = self.stack.enter_context(patch.object(target.os, 'fstat', side_effect=[self.linux, self.linux]))
        self.lstat = self.stack.enter_context(patch.object(target.Path, 'lstat', side_effect=[self.linux, self.linux]))
        self.stack.enter_context(patch.object(target.time, 'monotonic', return_value=0))

    def test_exact_full_native_read_keeps_linux_pin_and_records_verified_observations(self):
        self.instance._run.return_value = b'\xef\xbb\xbf' + json.dumps(self.native).encode() + b'\r\n'
        self.assertTrue(self.instance._retained_os_content())
        self.open.assert_called_once_with(Path(functional.TARGET['diskDirectory']) / 'os.raw', os.O_RDONLY | os.O_NOFOLLOW)
        self.close.assert_called_once_with(731)
        self.assertEqual(self.fstat.call_count, 2)
        self.assertEqual(self.lstat.call_count, 2)
        self.assertEqual(self.instance.authority.call_count, 2)
        self.instance._record.assert_called_once_with('retained-os-content.json', self.native)

    def test_native_invocation_is_fixed_read_only_and_bounded_to_90_seconds(self):
        self.assertTrue(self.instance._retained_os_content())
        self.instance._run.assert_called_once_with(target._NATIVE_OS_HASH_ARGV, seconds=90)
        argv = target._NATIVE_OS_HASH_ARGV
        self.assertIsInstance(argv, tuple)
        self.assertEqual(argv[:4], ('/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', '-NoProfile', '-NonInteractive', '-Command'))
        self.assertEqual(len(argv), 5)
        script = argv[4]
        for fixed in ("$p='" + target._NATIVE_OS_PATH + "'", '[IO.File]::Open($p,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)', '[Security.Cryptography.SHA256]::Create()', 'ComputeHash($stream)', 'bytesHashed=($end-$start)', '$stream.Dispose()'):
            self.assertIn(fixed, script)
        for forbidden in ('Get-FileHash', '[IO.FileAccess]::Write', '[IO.FileMode]::Create', 'Invoke-Expression'):
            self.assertNotIn(forbidden, script)
        with self.assertRaises(TypeError):
            argv[0] = 'another-executable'

    def test_wrong_partial_or_truncated_hash_and_read_observations_refuse(self):
        changes = ({'sha256': 'f' * 64}, {'sha256': target.RETAINED_OS_SHA256[:-1]},
                   {'bytesHashed': self.size - 1}, {'startPosition': 1}, {'endPosition': self.size - 1},
                   {'streamLengthBefore': self.size - 1}, {'streamLengthAfter': self.size - 1},
                   {'bytesHashed': True}, {'readOnly': False}, {'readOnly': 1}, {'unchanged': False})
        for change in changes:
            with self.subTest(change=change):
                self.fstat.side_effect = [self.linux, self.linux]
                self.lstat.side_effect = [self.linux, self.linux]
                self.instance._run.return_value = json.dumps({**self.native, **change}).encode()
                with self.assertRaises(functional.FunctionalRetentionError):
                    self.instance._retained_os_content()
        self.fstat.side_effect = [self.linux, self.linux]
        self.lstat.side_effect = [self.linux, self.linux]
        self.instance._run.return_value = json.dumps(self.native).encode()[:-2]
        with self.assertRaises(functional.FunctionalRetentionError):
            self.instance._retained_os_content()
        self.instance._record.assert_not_called()

    def test_native_machine_identity_and_metadata_drift_refuse(self):
        observations = [{**self.native, 'machine': 'OTHER-HOST'}]
        for key, value in (('fullName', target._NATIVE_OS_PATH + '.other'), ('length', self.size - 1),
                           ('creationUtcTicks', 101), ('lastWriteUtcTicks', 201), ('attributes', 32 | 1024)):
            observation = deepcopy(self.native)
            observation['after'][key] = value
            observations.append(observation)
        for observation in observations:
            with self.subTest(observation=observation):
                self.fstat.side_effect = [self.linux, self.linux]
                self.lstat.side_effect = [self.linux, self.linux]
                self.instance._run.return_value = json.dumps(observation).encode()
                with self.assertRaises(functional.FunctionalRetentionError):
                    self.instance._retained_os_content()
        self.instance._record.assert_not_called()

    def test_linux_descriptor_and_named_path_identity_drift_refuse_and_close(self):
        for location in ('descriptor', 'name'):
            for field in ('st_dev', 'st_ino', 'st_size', 'st_mtime_ns', 'st_ctime_ns', 'st_uid', 'st_mode'):
                with self.subTest(location=location, field=field):
                    changed = deepcopy(self.linux)
                    setattr(changed, field, getattr(changed, field) + 1)
                    self.fstat.side_effect = [self.linux, changed if location == 'descriptor' else self.linux]
                    self.lstat.side_effect = [self.linux, changed if location == 'name' else self.linux]
                    with self.assertRaises(functional.FunctionalRetentionError):
                        self.instance._retained_os_content()
        self.instance._record.assert_not_called()
        self.assertEqual(self.close.call_count, 14)

    def test_authority_is_fresh_before_and_after_native_read_and_timeout_rejects(self):
        self.instance.authority.side_effect = functional.FunctionalRetentionError('offline-withdrawal')
        with self.assertRaises(functional.FunctionalRetentionError):
            self.instance._retained_os_content()
        self.open.assert_not_called()
        self.instance._run.assert_not_called()
        self.instance.authority.side_effect = [None, functional.FunctionalRetentionError('offline-withdrawal')]
        with self.assertRaises(functional.FunctionalRetentionError):
            self.instance._retained_os_content()
        self.close.assert_called_once_with(731)
        self.instance._record.assert_not_called()

    def test_native_hash_and_whole_preflight_keep_existing_deadlines(self):
        with patch.object(target.time, 'monotonic', side_effect=[0, 90.001]):
            with self.assertRaisesRegex(functional.FunctionalRetentionError, 'os-read-deadline'):
                self.instance._retained_os_content()
        self.close.assert_called_once_with(731)
        self.stack.close()
        with tempfile.TemporaryDirectory(prefix='functional-preflight-deadline-offline-') as temporary:
            root = Path(temporary)
            (root / 'tools').mkdir()
            (root / 'transport').mkdir()
            for path in (root / 'tools/access_telemetry_functional_guest.py', root / 'transport/access_telemetry_functional_guest.py'):
                path.write_text('offline-only fixture')
            for name in ('guest-config.json', 'dapr-values.json'):
                (root / 'transport' / name).write_text('{}')
            instance = target.FixedFunctionalTarget.__new__(target.FixedFunctionalTarget)
            instance.authority = unittest.mock.Mock()
            instance.plan = {'sourceRoot': str(root)}
            instance.root = root
            instance.transport = {'hostTools': {}, 'root': str(root / 'transport'), 'applicationImages': {}}
            instance._retained_disks = unittest.mock.Mock(return_value=True)
            elapsed = [0]
            instance._retained_os_content = lambda: elapsed.__setitem__(0, 120.001)
            instance.resource_floors = unittest.mock.Mock(return_value=True)
            with patch.object(target.time, 'monotonic', side_effect=lambda: elapsed[0]), patch.object(target, '_render', return_value={}), patch.object(target, '_complete_config', return_value=({}, {})), patch.object(target, '_digest', return_value='1' * 64), patch.object(target.socket, 'socket'), patch.object(target.Path, 'exists', return_value=False):
                with self.assertRaisesRegex(functional.FunctionalRetentionError, 'preflight-deadline'):
                    instance.preflight()
            instance.resource_floors.assert_not_called()


class DiskInitializationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='functional-guest-disk-offline-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.marker = self.root / 'format-attempt'
        self.backing = self.root / 'backing-evidence'
        self.backing.write_bytes(b'preserved-evidence')
        self.data = self.root / 'data'
        self.disks = [
            {'name': 'vda', 'path': '/dev/vda', 'size': 40 * 1024**3, 'serial': 'memories-qual-os', 'type': 'disk', 'fstype': None, 'mountpoints': [None]},
            {'name': 'vdb', 'path': '/dev/vdb', 'size': 600 * 1024**3, 'serial': 'memories-qual-data', 'type': 'disk', 'fstype': None, 'mountpoints': [None]},
        ]
        self.signatures, self.mounted, self.formats, self.fail_format = [], False, 0, False
        paths = {}
        for name, value in (('/sys/class/dmi/id/product_uuid', guest.UUID), ('/etc/hostname', 'memories-qualification-27-4'),
                            ('/etc/memories-qualification-target', 'environment=non-Production\ninstance=' + guest.UUID)):
            path = self.root / ('identity-' + str(len(paths)))
            path.write_text(value)
            paths[name] = path
        paths['/var/lib/memories-27-4-data-initialized'] = self.marker
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        self.stack.enter_context(patch.object(guest, 'Path', side_effect=lambda value: paths.get(str(value), Path(value))))
        self.stack.enter_context(patch.object(guest, 'DATA', self.data))
        self.stack.enter_context(patch.object(guest.os.path, 'ismount', side_effect=lambda _path: self.mounted))
        self.stack.enter_context(patch.object(guest.os, 'statvfs', return_value=SimpleNamespace(f_bavail=550 * 1024**3 // 4096, f_frsize=4096, f_blocks=590 * 1024**3 // 4096)))
        self.stack.enter_context(patch.object(guest.os, 'chown'))
        self.stack.enter_context(patch.object(guest, 'run', side_effect=self.command))

    def command(self, argv, **_kwargs):
        if argv[0] == 'lsblk':
            return json.dumps({'blockdevices': self.disks}).encode()
        if argv[0] == 'wipefs':
            return json.dumps({'signatures': self.signatures}).encode()
        if argv[0] == 'mkfs.ext4':
            self.formats += 1
            self.assertEqual(self.marker.read_text(), guest.UUID)
            if self.fail_format:
                raise RuntimeError('offline-format-interrupted')
            self.backing.write_bytes(b'formatted-offline-fixture')
            self.disks[1]['fstype'] = 'ext4'
        elif argv[0] == 'mount':
            self.mounted = True
        else:
            self.fail('unexpected command')
        return b''

    def test_nonblank_signature_filesystem_mount_identity_size_and_path_refuse_before_format(self):
        pristine = deepcopy(self.disks)
        for mutation in ('signature', 'filesystem', 'mount', 'serial', 'size', 'path', 'child'):
            with self.subTest(mutation=mutation):
                self.disks = deepcopy(pristine)
                self.signatures = []
                if mutation == 'signature':
                    self.signatures = [{'type': 'existing-data'}]
                elif mutation == 'filesystem':
                    self.disks[1]['fstype'] = 'ext4'
                elif mutation == 'mount':
                    self.disks[1]['mountpoints'] = ['/foreign']
                elif mutation == 'serial':
                    self.disks[1]['serial'] = 'foreign-disk'
                elif mutation == 'size':
                    self.disks[1]['size'] -= 4096
                elif mutation == 'path':
                    self.disks[1]['path'] = '/dev/vdc'
                else:
                    self.disks[1]['children'] = [{'name': 'partition'}]
                with self.assertRaises(RuntimeError):
                    guest.mount_data()
                self.assertEqual(self.formats, 0)
                self.assertFalse(self.marker.exists())
                self.assertEqual(self.backing.read_bytes(), b'preserved-evidence')

    def test_format_attempt_is_recorded_before_write_and_interruption_never_retries(self):
        self.fail_format = True
        with self.assertRaisesRegex(RuntimeError, 'offline-format-interrupted'):
            guest.mount_data()
        self.assertEqual(self.formats, 1)
        self.assertEqual(self.marker.read_text(), guest.UUID)
        self.fail_format = False
        with self.assertRaisesRegex(RuntimeError, 'existing-data'):
            guest.mount_data()
        self.assertEqual(self.formats, 1)

    def test_successful_initialization_reuses_only_its_marked_ext4_mount(self):
        observed = guest.mount_data()
        self.assertEqual(observed['dataBytes'], 600 * 1024**3)
        self.assertGreaterEqual(observed['filesystemFreeBytes'], 500 * 1024**3)
        self.assertEqual(self.formats, 1)
        guest.mount_data()
        self.assertEqual(self.formats, 1)


class OciIntegrityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='functional-oci-offline-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def archive(self, *, corrupt=False):
        reference = 'example.invalid/offline-fixture@sha256:'
        config = json.dumps({'os': 'linux', 'architecture': 'amd64'}).encode()
        layer = b'fixed-offline-layer'
        def descriptor(raw):
            return {'digest': 'sha256:' + hashlib.sha256(raw).hexdigest(), 'size': len(raw)}
        manifest = json.dumps({'schemaVersion': 2, 'mediaType': 'application/vnd.oci.image.manifest.v1+json', 'config': descriptor(config), 'layers': [descriptor(layer)]}).encode()
        root = {**descriptor(manifest), 'annotations': {'io.containerd.image.name': reference + hashlib.sha256(manifest).hexdigest()}}
        identity = {'indexSha256': hashlib.sha256(manifest).hexdigest(), 'platformManifestSha256': None, 'reference': root['annotations']['io.containerd.image.name']}
        values = {'oci-layout': b'{"imageLayoutVersion":"1.0.0"}', 'index.json': json.dumps({'schemaVersion': 2, 'manifests': [root]}).encode(),
                  'blobs/sha256/' + hashlib.sha256(manifest).hexdigest(): manifest,
                  'blobs/sha256/' + hashlib.sha256(config).hexdigest(): config,
                  'blobs/sha256/' + hashlib.sha256(layer).hexdigest(): b'x' * len(layer) if corrupt else layer}
        path = self.root / ('corrupt.tar' if corrupt else 'verified.tar')
        with tarfile.open(path, 'w') as archive:
            for name, value in values.items():
                info = tarfile.TarInfo(name)
                info.size = len(value)
                archive.addfile(info, io.BytesIO(value))
        return path, identity

    def test_each_pinned_manifest_and_blob_is_checked_before_guest_import(self):
        path, identity = self.archive()
        guest._verify_oci(path, identity)
        path, identity = self.archive(corrupt=True)
        with self.assertRaisesRegex(RuntimeError, 'oci-blob-drift'):
            guest._verify_oci(path, identity)

    def test_staged_download_change_refuses_at_consumption(self):
        path = self.root / 'k3s-offline-fixture'
        path.write_bytes(b'pinned-executable-fixture')
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        with patch.object(guest, 'ROOT', self.root):
            guest._verify_file(path.name, digest)
            path.write_bytes(b'changed-executable-fixture')
            with self.assertRaisesRegex(RuntimeError, 'input-drift'):
                guest._verify_file(path.name, digest)


if __name__ == '__main__':
    unittest.main()
