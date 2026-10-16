# Historical VM implementation retained for evidence and offline regressions.
# The current functional-retention CLI never imports or dispatches this module.
"""Fixed local KVM/SSH target for the existing functional-retention checkpoint.

Preparation creates inert private inputs only. The live constructor is selected
by the verifier after admission; callers cannot supply commands or destinations.
"""

from __future__ import annotations

import base64
from datetime import datetime, timedelta, timezone
import hashlib
import json
import os
from pathlib import Path
import selectors
import shlex
import shutil
import signal
import socket
import stat
import subprocess
import sys
import time

NS = 'hexalith-memories-qualification'
GUEST = '/var/tmp/memories-27-4-functional'
SCHEMA = 'hexalith.access-telemetry.functional-transport/v1'
TOOLS = ('/usr/bin/qemu-system-x86_64', '/usr/bin/timeout', '/usr/bin/ssh', '/usr/bin/scp',
         '/usr/bin/sudo', '/usr/bin/setpriv', '/usr/bin/ssh-keygen', '/usr/bin/cloud-localds',
         '/usr/bin/openssl', '/usr/local/bin/kubectl', '/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', str(Path(sys.executable).resolve()))
RETAINED_OS_SHA256 = '074502ca51d9cb20a79cdabd715f5e06afbbbec298e19b84a6770104da6361de'
_NATIVE_OS_PATH = r'C:\Users\JeromePiquot\AppData\Local\Hexalith\Qualification\memories-27-4-2fe71a5c\os.raw'
_NATIVE_OS_HASH_ARGV = (
    '/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', '-NoProfile', '-NonInteractive', '-Command',
    r'''$ErrorActionPreference='Stop';
$p='C:\Users\JeromePiquot\AppData\Local\Hexalith\Qualification\memories-27-4-2fe71a5c\os.raw';
$metadata={ $i=[IO.FileInfo]::new($p); $i.Refresh(); [ordered]@{fullName=$i.FullName;length=$i.Length;creationUtcTicks=$i.CreationTimeUtc.Ticks;lastWriteUtcTicks=$i.LastWriteTimeUtc.Ticks;attributes=[int]$i.Attributes} };
$before=& $metadata;
if ([Environment]::MachineName -cne 'DESKTOP-VIOG240' -or $before.length -ne 42949672960 -or ($before.attributes -band 3584) -ne 0) { throw 'OS identity refused' };
$stream=[IO.File]::Open($p,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);
try {
  $start=$stream.Position; $lengthBefore=$stream.Length; $readOnly=$stream.CanRead -and -not $stream.CanWrite;
  $hasher=[Security.Cryptography.SHA256]::Create();
  try { $digest=[BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','').ToLowerInvariant() } finally { $hasher.Dispose() };
  $end=$stream.Position; $lengthAfter=$stream.Length; $after=& $metadata;
  $unchanged=(($before | ConvertTo-Json -Compress) -ceq ($after | ConvertTo-Json -Compress)) -and $lengthBefore -eq $lengthAfter;
  [ordered]@{machine=[Environment]::MachineName;sha256=$digest;bytesHashed=($end-$start);startPosition=$start;endPosition=$end;streamLengthBefore=$lengthBefore;streamLengthAfter=$lengthAfter;readOnly=$readOnly;unchanged=$unchanged;before=$before;after=$after} | ConvertTo-Json -Depth 4 -Compress
} finally { $stream.Dispose() }''',
)
OPENBAO = 'quay.io/openbao/openbao:2.6.4@sha256:cf2340fc9a22cb9358ca0defd1f39b65673836bd23fe2bb8984a07e11fe13ef4'
FILES = {'clientKey': 'ssh-client', 'knownHosts': 'known_hosts', 'seed': 'functional-seed.iso',
         'guestHelper': 'access_telemetry_functional_guest.py', 'guestConfig': 'guest-config.json',
         'daprValues': 'dapr-values.json'}


def _core():
    import access_telemetry_functional_retention as core
    return core


def _require(condition, code):
    _core()._require(condition, 'target-' + code)


def _digest(path):
    return _core()._hash_file(Path(path))[0]


def _write(path, value):
    raw = value if isinstance(value, bytes) else json.dumps(value, sort_keys=True, separators=(',', ':')).encode()
    _guard_generated_path(Path(path), len(raw))
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(descriptor, 'wb') as stream:
        stream.write(raw)
        stream.flush()
        os.fsync(stream.fileno())


def _local(argv, seconds=120):
    # Preparation commands are all local key/seed/render operations. No kube
    # context, server, registry, QEMU or privileged invocation occurs here.
    for value in argv:
        if isinstance(value, str) and value.startswith('/'):
            _guard_generated_path(Path(value), 2 * 1024**2)
    result = subprocess.run(argv, stdin=subprocess.DEVNULL, capture_output=True, timeout=seconds)
    _require(result.returncode == 0 and len(result.stdout) <= 1048576 and len(result.stderr) <= 1048576, 'local-preparation')
    return result.stdout


def _guard_generated_path(path, count):
    for ancestor in path.parents:
        if ancestor.parent == _core().PRIVATE_TARGET_ROOT / 'evidence' and ancestor.name.startswith('functional-preparation-'):
            if path.is_relative_to(ancestor / 'transport'):
                _core()._preparation_write_guard(ancestor, count)
            break


def _secret(name, values, namespace=NS):
    return {'apiVersion': 'v1', 'kind': 'Secret', 'metadata': {'name': name, 'namespace': namespace}, 'stringData': values}


def _certificate(root, name, common_name, sans, *, verify_existing=False):
    key, cert, csr, extension = (root / (name + suffix) for suffix in ('.key', '.crt', '.csr', '.ext'))
    if verify_existing:
        return {'tls.crt': cert.read_text(), 'tls.key': key.read_text(), 'ca.crt': (root / 'ca.crt').read_text()}
    _local(['/usr/bin/openssl', 'genpkey', '-algorithm', 'RSA', '-pkeyopt', 'rsa_keygen_bits:2048', '-out', str(key)])
    os.chmod(key, 0o600)
    _local(['/usr/bin/openssl', 'req', '-new', '-key', str(key), '-subj', '/CN=' + common_name, '-out', str(csr)])
    _write(extension, ('subjectAltName=' + ','.join('DNS:' + value for value in sans) + '\nextendedKeyUsage=serverAuth\n').encode())
    _local(['/usr/bin/openssl', 'x509', '-req', '-in', str(csr), '-CA', str(root / 'ca.crt'), '-CAkey', str(root / 'ca.key'),
            '-CAserial', str(root / (name + '.srl')), '-CAcreateserial', '-days', '3', '-extfile', str(extension), '-out', str(cert)])
    return {'tls.crt': cert.read_text(), 'tls.key': key.read_text(), 'ca.crt': (root / 'ca.crt').read_text()}


def _render(root, source, application_images, *, verify_existing=False):
    import yaml
    raw = _local(['/usr/local/bin/kubectl', 'kustomize', str(source / 'deploy/kubernetes/overlays/qualification')])
    objects = list(yaml.safe_load_all(raw))
    apps = {'memories', 'memories-access-telemetry', 'memories-access-telemetry-clock'}
    excluded = {'memories-mcp', 'eventstore', 'memories-mcp-token-reader', 'eventstore-dapr-secret-reader',
                'memories-mcp-config', 'memories-access-telemetry-physical-evidence-reporter',
                'access-telemetry-physical-evidence-report', 'llm-openai', 'memories-events'}
    allowed_components = {'secretstore', 'statestore', 'pubsub', 'access-telemetry-secrets', 'access-telemetry-config', 'access-telemetry-store'}
    selected = []
    if verify_existing:
        public_key = (root / 'clock-public.der').read_bytes()
        private_key = (root / 'clock-private.der').read_bytes()
        material = json.loads((root / 'functional-material.json').read_text())
    else:
        public_key = _local(['/usr/bin/openssl', 'pkey', '-in', str(root / 'clock.key'), '-pubout', '-outform', 'DER'])
        private_key = _local(['/usr/bin/openssl', 'pkcs8', '-topk8', '-nocrypt', '-in', str(root / 'clock.key'), '-outform', 'DER'])
        material = {'passwords': {name: os.urandom(32).hex() for name in ('redis', 'falkor', 'pg-admin', 'pg-runtime', 'app', 'dapr')},
                    'markerKey': base64.b64encode(os.urandom(32)).decode()}
        _write(root / 'clock-public.der', public_key)
        _write(root / 'clock-private.der', private_key)
        _write(root / 'functional-material.json', material)
    _require(set(material) == {'passwords', 'markerKey'} and set(material['passwords']) == {'redis', 'falkor', 'pg-admin', 'pg-runtime', 'app', 'dapr'}, 'material-schema')
    _require(all(isinstance(value, str) and _core()._HEX.fullmatch(value) for value in material['passwords'].values()), 'material-password')
    _require(len(base64.b64decode(material['markerKey'], validate=True)) == 32 and 64 <= len(public_key) <= 256 and 64 <= len(private_key) <= 512, 'material-key')
    encoded_public = base64.b64encode(public_key).decode()
    common = {
        'AccessTelemetryLifecycle__DeploymentId': 'memories-27-4-functional',
        'AccessTelemetryLifecycle__ConfigurationEpoch': '01K7P000000000000000000000',
        'AccessTelemetryLifecycle__ClockSignerKeyEpoch': 'functional-clock-1',
        'AccessTelemetryLifecycle__MarkerKeyGeneration': 'functional-marker-1',
        'AccessTelemetryLifecycle__MarkerKeyReference': 'access-telemetry-marker-key',
        'AccessTelemetryLifecycle__CapacityEvidenceId': 'functional-small-cohorts-only',
        'AccessTelemetryLifecycle__PhysicalReclamationEvidenceId': 'pending-full-c3',
        # Reserved to the source lifecycle image; this slice deploys no adapter
        # or reporter and cannot issue physical-reclamation evidence.
        'AccessTelemetryLifecycle__PhysicalReclamationReporterImageDigest': application_images['memories-access-telemetry']['manifestDigest'].removeprefix('sha256:'),
    }
    for obj in objects:
        if not obj:
            continue
        kind, name = obj['kind'], obj['metadata']['name']
        if name in excluded or 'operator' in name or 'reporter' in name:
            continue
        if kind == 'Component' and name not in allowed_components:
            continue
        if kind in {'ClusterRole', 'ClusterRoleBinding', 'Job'}:
            continue
        if kind == 'ServiceAccount':
            if name not in apps | {'access-telemetry-postgresql'}:
                continue
            obj.pop('imagePullSecrets', None)
        if kind == 'Deployment':
            if name not in apps:
                continue
            obj['spec']['replicas'] = 0
            template = obj['spec']['template']
            template['metadata']['annotations']['dapr.io/sidecar-image'] = (
                'ghcr.io/dapr/daprd:1.18.1@sha256:b7f7d296f01f0b4b82bf3c5f087ecf26165ce08caf3e87f94b8c72b9e11873f8')
            container = template['spec']['containers'][0]
            container['image'] = 'docker.io/library/' + name + '@' + application_images[name]['manifestDigest']
            environment = {item['name']: item for item in container['env'] if not item['name'].startswith('Clock__Sources__')}
            for key, value in common.items():
                environment[key] = {'name': key, 'value': value}
            environment['AccessTelemetryLifecycle__AttestationVerificationKey'] = {'name': 'AccessTelemetryLifecycle__AttestationVerificationKey', 'value': encoded_public}
            if name == 'memories-access-telemetry-clock':
                for key, value in {'Clock__AllowDevelopmentSources': 'true', 'Clock__SignerKeyEpoch': 'functional-clock-1'}.items():
                    environment[key] = {'name': key, 'value': value}
            container['env'] = list(environment.values())
        if kind == 'StatefulSet':
            if name in {'redis-stack', 'falkordb'}:
                # Static local PV directories begin root-owned. Let kubelet
                # grant their pods group write access without privileged init.
                security = obj['spec']['template']['spec'].setdefault('securityContext', {})
                security.update({'fsGroup': 1000, 'fsGroupChangePolicy': 'OnRootMismatch'})
            for claim in obj['spec'].get('volumeClaimTemplates', []):
                claim['spec']['storageClassName'] = 'memories-functional-retain'
                claim['spec']['volumeName'] = 'memories-functional-' + {'access-telemetry-postgresql': 'postgresql', 'redis-stack': 'redis', 'falkordb': 'falkordb'}[name]
        if kind in {'Role', 'RoleBinding'} and obj['metadata'].get('namespace') == 'dapr-system':
            # Existing exact cross-namespace Dapr permission is retained.
            pass
        selected.append(obj)
    bootstrap = [obj for obj in selected if obj['kind'] in {'Namespace', 'ConfigMap', 'Lease'} and obj['metadata']['name'] == 'access-telemetry-qualification']
    bootstrap = [{'apiVersion': 'v1', 'kind': 'Namespace', 'metadata': {'name': name}} for name in (NS, 'openbao', 'dapr-system')]
    bootstrap.append({'apiVersion': 'storage.k8s.io/v1', 'kind': 'StorageClass', 'metadata': {'name': 'memories-functional-retain'},
                      'provisioner': 'kubernetes.io/no-provisioner', 'volumeBindingMode': 'WaitForFirstConsumer', 'reclaimPolicy': 'Retain'})
    volumes = [('postgresql', 400, NS, 'data-access-telemetry-postgresql-0'), ('redis', 20, NS, 'data-redis-stack-0'),
               ('falkordb', 10, NS, 'data-falkordb-0'), ('openbao', 60, 'openbao', 'memories-functional-openbao')]
    volumes.extend(('scheduler-' + str(index), 17, 'dapr-system', 'dapr-scheduler-data-dir-dapr-scheduler-server-' + str(index)) for index in range(3))
    for name, size, namespace, claim in volumes:
        bootstrap.append({'apiVersion': 'v1', 'kind': 'PersistentVolume', 'metadata': {'name': 'memories-functional-' + name}, 'spec': {
            'capacity': {'storage': str(size) + 'Gi'}, 'accessModes': ['ReadWriteOnce'], 'persistentVolumeReclaimPolicy': 'Retain',
            'storageClassName': 'memories-functional-retain', 'local': {'path': '/srv/memories-qualification/' + name},
            'claimRef': {'name': claim, 'namespace': namespace},
            'nodeAffinity': {'required': {'nodeSelectorTerms': [{'matchExpressions': [{'key': 'kubernetes.io/hostname', 'operator': 'In', 'values': ['memories-qualification-27-4']}]}]}}}})
    # Both dedicated scopes remain in OpenBao, using the same TLS secret-store
    # components and real PostgreSQL SCRAM+verify-full runtime connection.
    passwords = material['passwords']
    pg_tls = _certificate(root, 'postgresql', 'access-telemetry-postgresql',
                          ['access-telemetry-postgresql', 'access-telemetry-postgresql.' + NS + '.svc.cluster.local'], verify_existing=verify_existing)
    bao_tls = _certificate(root, 'openbao', 'hexalith-keys.openbao.svc.cluster.local',
                           ['hexalith-keys', 'hexalith-keys.openbao.svc.cluster.local', 'localhost'], verify_existing=verify_existing)
    # Include loopback IP explicitly for the CLI's verified HTTPS connection.
    # BAO_ADDR below uses the certificate's DNS identity through its service.
    application_secrets = [
        _secret('redis-secret', {'password': passwords['redis'], 'falkordb-password': passwords['falkor']}),
        _secret('app-api-token', {'token': passwords['app']}), _secret('dapr-api-token', {'token': passwords['dapr']}),
        _secret('access-telemetry-postgresql-bootstrap', {'admin-password': passwords['pg-admin'], 'runtime-password': passwords['pg-runtime']}),
        _secret('access-telemetry-postgresql-tls', pg_tls),
        _secret('access-telemetry-clock-key', {'verification-public-key': encoded_public}),
    ]
    runtime_values = {
        'redis-secret': {'password': passwords['redis'], 'falkordb-password': passwords['falkor']},
        'llm-secret': {'key': 'functional-offline-unused', 'endpoint': 'https://unused.example.invalid', 'model': 'unused'},
        'google-embedding-api-key': {'google-embedding-api-key': 'functional-offline-unused'},
        'memories-embedding-client-secret': {'memories-embedding-client-secret': 'functional-offline-unused'},
    }
    access_values = {
        'redis-secret': {'password': passwords['redis']},
        'access-telemetry-marker-key': {'access-telemetry-marker-key': material['markerKey']},
        'access-telemetry-clock-key': {'signing-key-pkcs8': base64.b64encode(private_key).decode()},
        'access-telemetry-postgresql': {'connectionString': 'host=access-telemetry-postgresql.' + NS + '.svc.cluster.local port=5432 user=memories_access_telemetry_runtime password=' + passwords['pg-runtime'] + ' dbname=memories_access_telemetry sslmode=verify-full sslrootcert=/mnt/access-telemetry-postgresql/ca.crt'},
    }
    bao_objects = [_secret('openbao-server-tls', bao_tls, 'openbao'),
        {'apiVersion': 'v1', 'kind': 'PersistentVolumeClaim', 'metadata': {'name': 'memories-functional-openbao', 'namespace': 'openbao'}, 'spec': {
            'accessModes': ['ReadWriteOnce'], 'storageClassName': 'memories-functional-retain', 'volumeName': 'memories-functional-openbao', 'resources': {'requests': {'storage': '60Gi'}}}},
        {'apiVersion': 'v1', 'kind': 'ConfigMap', 'metadata': {'name': 'memories-functional-openbao', 'namespace': 'openbao'}, 'data': {'config.hcl': 'ui = false\ndisable_mlock = true\nstorage "file" { path = "/openbao/data" }\nlistener "tcp" { address = "0.0.0.0:8200" tls_cert_file = "/tls/tls.crt" tls_key_file = "/tls/tls.key" }\n'}},
        {'apiVersion': 'v1', 'kind': 'Service', 'metadata': {'name': 'hexalith-keys', 'namespace': 'openbao'}, 'spec': {'selector': {'app': 'hexalith-keys'}, 'ports': [{'port': 8200, 'targetPort': 8200}]}},
        {'apiVersion': 'apps/v1', 'kind': 'Deployment', 'metadata': {'name': 'hexalith-keys', 'namespace': 'openbao'}, 'spec': {
            'replicas': 1, 'selector': {'matchLabels': {'app': 'hexalith-keys'}}, 'template': {'metadata': {'labels': {'app': 'hexalith-keys'}}, 'spec': {
                'securityContext': {'fsGroup': 1000}, 'containers': [{'name': 'openbao', 'image': OPENBAO, 'imagePullPolicy': 'IfNotPresent',
                    'command': ['bao', 'server', '-config=/config/config.hcl'], 'env': [{'name': 'BAO_ADDR', 'value': 'https://hexalith-keys.openbao.svc.cluster.local:8200'}, {'name': 'BAO_CACERT', 'value': '/tls/ca.crt'}],
                    'resources': {'requests': {'cpu': '100m', 'memory': '128Mi'}, 'limits': {'cpu': '1', 'memory': '512Mi'}},
                    'volumeMounts': [{'name': 'tls', 'mountPath': '/tls', 'readOnly': True}, {'name': 'config', 'mountPath': '/config', 'readOnly': True}, {'name': 'data', 'mountPath': '/openbao/data'}],
                    'readinessProbe': {'tcpSocket': {'port': 8200}, 'periodSeconds': 2}}],
                'volumes': [{'name': 'tls', 'secret': {'secretName': 'openbao-server-tls'}}, {'name': 'config', 'configMap': {'name': 'memories-functional-openbao'}}, {'name': 'data', 'persistentVolumeClaim': {'claimName': 'memories-functional-openbao'}}]}}}}]
    return {'bootstrapObjects': bootstrap, 'openbaoObjects': bao_objects, 'applicationObjects': selected,
            'applicationSecretObjects': application_secrets, 'vaultValues': {'hexalith/memories/runtime': runtime_values, 'hexalith/memories/access-telemetry': access_values},
            'openbaoCa': bao_tls['ca.crt'], 'redisPassword': passwords['redis']}


def prepare_transport_inputs(bundle_root: Path, source_root: Path, application_images: dict):
    """Local-only render, new inactive keys/seed, tool and retained disk binding."""
    root = bundle_root / 'transport'
    root.mkdir(mode=0o700)
    blockers = []
    for name in TOOLS:
        if not Path(name).is_file():
            blockers.append('missing-local-' + Path(name).name)
    if blockers:
        return {}, blockers
    expires = (datetime.now(timezone.utc) + timedelta(hours=48)).replace(microsecond=0)
    for name in ('ssh-client', 'ssh-host'):
        _local(['/usr/bin/ssh-keygen', '-q', '-t', 'ed25519', '-N', '', '-C', 'memories-27-4-functional', '-f', str(root / name)])
    _write(root / 'known_hosts', ('[127.0.0.1]:12274 ' + (root / 'ssh-host.pub').read_text()).encode())
    cloud = {'hostname': 'memories-qualification-27-4', 'preserve_hostname': False, 'manage_etc_hosts': True,
             'disable_root': True, 'ssh_pwauth': False, 'package_update': False, 'package_upgrade': False, 'ssh_deletekeys': True,
             'ssh_keys': {'ed25519_private': (root / 'ssh-host').read_text(), 'ed25519_public': (root / 'ssh-host.pub').read_text().strip()},
             'users': [{'name': 'qualifier', 'lock_passwd': True, 'shell': '/bin/bash', 'sudo': ['ALL=(ALL) NOPASSWD:ALL'],
                        'ssh_authorized_keys': ['restrict,expiry-time="' + expires.strftime('%Y%m%d%H%M%SZ') + '" ' + (root / 'ssh-client.pub').read_text().strip()]}],
             'write_files': [{'path': '/etc/memories-qualification-target', 'permissions': '0644', 'content': 'environment=non-Production\npurpose=story-27-4-qualification\ninstance=' + _core().TARGET['uuid'] + '\n'}]}
    _write(root / 'user-data', b'#cloud-config\n' + json.dumps(cloud).encode() + b'\n')
    _write(root / 'meta-data', ('instance-id: memories-27-4-functional-' + expires.strftime('%Y%m%dT%H%M%SZ') + '\nlocal-hostname: memories-qualification-27-4\n').encode())
    _local(['/usr/bin/cloud-localds', str(root / 'functional-seed.iso'), str(root / 'user-data'), str(root / 'meta-data')])
    _local(['/usr/bin/openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '3', '-subj', '/CN=memories-functional-ca', '-keyout', str(root / 'ca.key'), '-out', str(root / 'ca.crt')])
    _local(['/usr/bin/openssl', 'genpkey', '-algorithm', 'EC', '-pkeyopt', 'ec_paramgen_curve:P-256', '-out', str(root / 'clock.key')])
    for path in root.iterdir():
        os.chmod(path, 0o600)
    config = _render(root, source_root, application_images)
    helper = source_root / 'tools/access_telemetry_functional_guest.py'
    _write(root / helper.name, helper.read_bytes())
    config, values = _complete_config(config, bundle_root, application_images, _digest(root / helper.name))
    _write(root / 'dapr-values.json', values)
    _write(root / 'guest-config.json', config)
    for path in root.iterdir():
        os.chmod(path, 0o600)
    disks = {}
    for name in ('os.raw', 'data.raw'):
        path = Path(_core().TARGET['diskDirectory']) / name
        info = path.lstat()
        _require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_blocks * 512 >= info.st_size, 'retained-disk-allocation')
        disks[name] = {key: getattr(info, attr) for key, attr in (('device', 'st_dev'), ('inode', 'st_ino'), ('uid', 'st_uid'), ('size', 'st_size'), ('mtimeNs', 'st_mtime_ns'), ('ctimeNs', 'st_ctime_ns'))}
    transport = {'schemaVersion': SCHEMA, 'root': str(root), 'credentialExpiresUtc': expires.isoformat().replace('+00:00', 'Z'),
                 'diskIdentities': disks, 'hostTools': {path: _digest(Path(path).resolve()) for path in TOOLS},
                 'files': {name: str(root / filename) for name, filename in FILES.items()},
                 'applicationImages': application_images, 'renderedObjectsSha256': _digest(root / 'guest-config.json')}
    validate_transport(transport, custody_root=bundle_root)
    return transport, blockers


def _complete_config(config, bundle_root, application_images, helper_digest):
    catalog = _core().artifact_catalog()
    # Catalog paths and digest values are closed and validated by the core.
    # Preserve relative asset paths in the guest to avoid an alternate mapper.
    download_roles = {'assets/k3s': 'k3s', 'assets/k3s-airgap-images-amd64.tar.zst': 'k3s-airgap',
                      'assets/helm.tar.gz': 'helm', 'assets/dapr.tgz': 'dapr-chart'}
    config['assets'] = {download_roles[item['path']]: {'name': item['path']} for item in catalog['downloads'] if item['path'] in download_roles}
    config['imageArchives'] = [item['path'] for item in catalog['images']]
    config['imageIdentities'] = catalog['images']
    config['fileDigests'] = {'access_telemetry_functional_guest.py': helper_digest,
                           **{item['path']: item['sha256'] for item in catalog['downloads']}}
    for name, item in application_images.items():
        relative = Path(item['archivePath']).relative_to(bundle_root).as_posix()
        config['imageArchives'].append(relative)
        config['fileDigests'][relative] = item['archiveSha256']
    images = {item['id']: item['reference'] for item in catalog['images']}
    values = {'global': {'mtls': {'enabled': True}, 'ha': {'enabled': False}, 'logAsJson': True},
              'dapr_operator': {'image': {'name': images['operator']}},
              'dapr_sentry': {'image': {'name': images['sentry']}},
              'dapr_placement': {'image': {'name': images['placement']}},
              'dapr_scheduler': {'image': {'name': images['scheduler']}, 'cluster': {'storageSize': '17Gi', 'storageClassName': 'memories-functional-retain'}},
              'dapr_sidecar_injector': {'injectorImage': {'name': images['injector']}, 'image': {'name': images['daprd']}}}
    config['fileDigests']['dapr-values.json'] = hashlib.sha256(json.dumps(values, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    return config, values


def validate_transport(transport, *, custody_root):
    _require(type(transport) is dict and set(transport) == {'schemaVersion', 'root', 'credentialExpiresUtc', 'diskIdentities', 'hostTools', 'files', 'applicationImages', 'renderedObjectsSha256'}, 'schema')
    _require(transport['schemaVersion'] == SCHEMA and transport['root'] == str(custody_root / 'transport'), 'root')
    _require(transport['files'] == {name: str(custody_root / 'transport' / filename) for name, filename in FILES.items()}, 'file-paths')
    _require(set(transport['hostTools']) == set(TOOLS) and all(_core()._HEX.fullmatch(value) for value in transport['hostTools'].values()), 'tool-schema')
    _core()._utc(transport['credentialExpiresUtc'])
    _require(set(transport['diskIdentities']) == {'os.raw', 'data.raw'}, 'disk-schema')
    for name, item in transport['diskIdentities'].items():
        _require(set(item) == {'device', 'inode', 'uid', 'size', 'mtimeNs', 'ctimeNs'} and all(type(value) is int and value >= 0 for value in item.values()), 'disk-fields')
        _require(item['size'] == (40 if name == 'os.raw' else 600) * 1024**3, 'disk-size')
    _require(set(transport['applicationImages']) == {'memories', 'memories-access-telemetry', 'memories-access-telemetry-clock'}, 'images')
    for item in transport['applicationImages'].values():
        _require(set(item) == {'archivePath', 'archiveSha256', 'manifestDigest', 'bytes'} and isinstance(item['archivePath'], str) and Path(item['archivePath']).is_relative_to(custody_root / 'assets'), 'image-schema')
        _require(_core()._HEX.fullmatch(item['archiveSha256']) and item['manifestDigest'].startswith('sha256:') and _core()._HEX.fullmatch(item['manifestDigest'][7:]), 'image-digest')
    _require(_core()._HEX.fullmatch(transport['renderedObjectsSha256']) is not None, 'render-digest')


class FixedFunctionalTarget:
    """The only live transport. There is no injectable executor or URL field."""

    offline_only = False

    def __init__(self, plan, authority_callback):
        self.plan, self.authority = plan, authority_callback
        self.root = Path(plan['custodyRoot'])
        self.transport = plan['transport']
        validate_transport(self.transport, custody_root=self.root)
        self.output = self.root / 'run'
        self.output.mkdir(mode=0o700)
        self.process = None
        self.vm_buffers = [bytearray(), bytearray()]
        self.identity = None
        self.argv = None
        self.begin = time.monotonic()
        self.counter = 0
        self.output_bytes = 0
        self.lease = None
        self.gate = None
        self.pods = {}
        self.deployments = {}
        self.segments = {}
        self.ttl_absent = set()
        self.before_sequence = None
        self.setup_complete = False
        self.assets_staged = False
        self.ssh_ready = False
        self._resource_check_active = False
        self._last_resource_check = self.begin
        self.released_lease_version = None
        self.retry_disk_metadata = None
        self.kubeconfig = self.root / 'run/kubeconfig'
        self.holder = 'functional-' + hashlib.sha256(str(self.root).encode()).hexdigest()[:24]

    def _record(self, label, data):
        self.counter += 1
        encoded = data if isinstance(data, bytes) else json.dumps(data, sort_keys=True).encode()
        _require(len(encoded) <= 1048576, 'stream-bound')
        self.output_bytes += len(encoded)
        _require(self.output_bytes <= 256 * 1024**2, 'evidence-bound')
        _write(self.output / (str(self.counter).zfill(5) + '-' + label), encoded)

    def _run(self, argv, *, data=None, seconds=120, restoring=False, sensitive=False):
        self._pump_vm(restoring=restoring)
        started_utc = datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')
        failure = None
        if not restoring:
            self.authority()
            _require(self.setup_complete or time.monotonic() - self.begin < 5400, 'setup-deadline')
            self._monitor_floors()
        _require(argv[0] in self.transport['hostTools'] and _digest(Path(argv[0]).resolve()) == self.transport['hostTools'][argv[0]], 'tool-drift')
        _require(time.monotonic() - self.begin < 15300, 'restoration-deadline')
        child = subprocess.Popen(argv, stdin=subprocess.PIPE if data is not None else subprocess.DEVNULL,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True, close_fds=True)
        buffers = [bytearray(), bytearray()]
        deadline = time.monotonic() + seconds
        try:
            with selectors.DefaultSelector() as selector:
                for index, stream in enumerate((child.stdout, child.stderr)):
                    os.set_blocking(stream.fileno(), False)
                    selector.register(stream, selectors.EVENT_READ, index)
                if data is not None:
                    # Inputs are bounded small manifests / stdin secrets; bulk
                    # archives use scp with no retained file content.
                    _require(len(data) <= 1048576, 'stdin-bound')
                    child.stdin.write(data)
                    child.stdin.close()
                while selector.get_map() or child.poll() is None:
                    self._pump_vm(restoring=restoring)
                    if not restoring:
                        getattr(self.authority, 'tick', self.authority)()
                        _require(self.setup_complete or time.monotonic() - self.begin < 5400, 'setup-deadline')
                        self._monitor_floors()
                    _require(time.monotonic() < deadline and time.monotonic() - self.begin < 15300, 'command-deadline')
                    for key, _ in selector.select(.25):
                        chunk = os.read(key.fileobj.fileno(), 65536)
                        if not chunk:
                            selector.unregister(key.fileobj)
                        else:
                            buffers[key.data].extend(chunk)
                            _require(len(buffers[key.data]) <= 1048576, 'stream-bound')
            code = child.wait(timeout=2)
        except BaseException as error:
            failure = type(error).__name__
            raise
        finally:
            if child.poll() is None:
                os.killpg(child.pid, signal.SIGTERM)
                try:
                    child.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(child.pid, signal.SIGKILL)
                    child.wait(timeout=5)
            for stream in (child.stdout, child.stderr):
                stream.close()
            if child.stdin is not None and not child.stdin.closed:
                child.stdin.close()
            self._record('command.json', {'argv': argv, 'startedAtUtc': started_utc, 'finishedAtUtc': datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z'),
                                           'exitCode': child.returncode, 'failure': failure, 'stdoutSha256': hashlib.sha256(buffers[0]).hexdigest(),
                                           'stderrSha256': hashlib.sha256(buffers[1]).hexdigest(), 'stdoutBytes': len(buffers[0]), 'stderrBytes': len(buffers[1]),
                                           'streamPolicy': 'hashed-private-streams-with-separate-safe-semantic-projections'})
        # Never retain raw command streams: stdout can contain credentials or
        # whole canonical records. Semantic projections are recorded separately.
        if code == 255:
            diagnostic = bytes(buffers[1]).lower()
            transient = any(value in diagnostic for value in (b'connection refused', b'connection timed out', b'connection reset', b'connection closed', b'broken pipe', b'operation timed out'))
            semantic = any(value in diagnostic for value in (b'host key verification failed', b'host identification has changed', b'permission denied', b'bad permissions', b'invalid format'))
            if transient and not semantic:
                raise _core().TransportUncertain('functional-target-transport-uncertain')
            raise _core().FunctionalRetentionError('functional-target-ssh-identity-or-command-refused')
        _require(code == 0, 'command-failed')
        return bytes(buffers[0])

    def _pump_vm(self, *, restoring=False):
        if self.process is None:
            return
        for index, stream in enumerate((self.process.stdout, self.process.stderr)):
            if stream is None or stream.closed:
                continue
            while True:
                try:
                    block = os.read(stream.fileno(), 65536)
                except BlockingIOError:
                    break
                if not block:
                    break
                room = 1048576 - len(self.vm_buffers[index])
                self.vm_buffers[index].extend(block[:max(0, room)])
                if not restoring:
                    _require(len(block) <= room, 'qemu-output-bound')

    def _ssh_args(self):
        return ['-o', 'BatchMode=yes', '-o', 'IdentitiesOnly=yes', '-o', 'StrictHostKeyChecking=yes',
                '-o', 'UserKnownHostsFile=' + self.transport['files']['knownHosts'], '-o', 'GlobalKnownHostsFile=/dev/null',
                '-o', 'ConnectTimeout=5', '-o', 'ConnectionAttempts=1', '-i', self.transport['files']['clientKey']]

    def _ssh(self, arguments, **kwargs):
        _require(self._same_vm(), 'ssh-target-identity')
        return self._run(['/usr/bin/ssh', *self._ssh_args(), '-p', '12274', 'qualifier@127.0.0.1', shlex.join(arguments)], **kwargs)

    def _kubectl(self, *arguments, data=None, seconds=120, restoring=False):
        _require(self._same_vm(), 'api-target-identity')
        return self._run(['/usr/local/bin/kubectl', '--kubeconfig', str(self.kubeconfig), '--context', _core().TARGET['context'],
                          '--request-timeout=110s', '-n', NS, *arguments], data=data, seconds=seconds, restoring=restoring)

    def _get(self, kind, name, *, restoring=False):
        return json.loads(self._kubectl('get', kind, name, '-o', 'json', restoring=restoring))

    def _pod(self, app):
        obj = json.loads(self._kubectl('get', 'pods', '-l', 'app.kubernetes.io/name=' + app, '-o', 'json'))
        _require(len(obj['items']) == 1, 'single-pod-required')
        item = obj['items'][0]
        _require(not item['metadata'].get('deletionTimestamp') and item['status']['phase'] == 'Running', 'pod-not-running')
        identity = (item['metadata']['name'], item['metadata']['uid'])
        _require(app not in self.pods or self.pods[app] == identity, 'pod-replaced')
        self.pods[app] = identity
        return identity[0]

    def _exec(self, app, script, *, seconds=120):
        container = {'memories': 'memories', 'memories-access-telemetry': 'lifecycle', 'memories-access-telemetry-clock': 'clock'}[app]
        return self._kubectl('exec', self._pod(app), '-c', container, '--', '/bin/sh', '-ec', script, seconds=seconds)

    def _monitor_floors(self):
        if not self._resource_check_active and time.monotonic() - self._last_resource_check >= 30:
            self.resource_floors()

    def resource_floors(self, *, starting=False):
        self._resource_check_active = True
        try:
            return self._resource_floors(starting=starting)
        finally:
            self._last_resource_check = time.monotonic()
            self._resource_check_active = False

    def _resource_floors(self, *, starting=False):
        values = dict(line.split(':', 1) for line in Path('/proc/meminfo').read_text().splitlines())
        available = int(values['MemAvailable'].split()[0]) * 1024
        floor = _core().LIMITS['hostStartAvailableBytes'] if starting or self.process is None else _core().LIMITS['hostRunAvailableBytes']
        free = shutil.disk_usage(_core().TARGET['diskDirectory']).free
        _require(available >= floor and free >= _core().LIMITS['hostDiskFreeBytes'], 'resource-floor')
        _require(Path('/proc/sys/kernel/hostname').read_text().strip() == _core().TARGET['host'], 'host-name')
        # WSL allocation and Linux stat alone do not prove native Windows
        # physical memory, NTFS allocation or C: headroom.
        script = r'''$ErrorActionPreference='Stop'; $p='C:\Users\JeromePiquot\AppData\Local\Hexalith\Qualification\memories-27-4-2fe71a5c'; Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class FunctionalAllocation { [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern uint GetCompressedFileSizeW(string path, out uint high); }'; $files=@(Get-ChildItem -LiteralPath $p -Force | ForEach-Object { if($_.PSIsContainer -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unexpected entry' }; [uint32]$high=0; $low=[FunctionalAllocation]::GetCompressedFileSizeW($_.FullName,[ref]$high); if($low -eq [uint32]::MaxValue -and [Runtime.InteropServices.Marshal]::GetLastWin32Error() -ne 0) { throw 'Allocation read failed' }; @{name=$_.Name;size=$_.Length;allocatedBytes=([uint64]$high*4294967296+[uint64]$low);attributes=$_.Attributes.ToString()} }); $drive=[IO.DriveInfo]::new('C:\'); [pscustomobject]@{machine=[Environment]::MachineName;format=$drive.DriveFormat;freeBytes=$drive.AvailableFreeSpace;availableBytes=([uint64](Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory*1024);files=$files} | ConvertTo-Json -Depth 5 -Compress'''
        native = json.loads(self._run(['/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe', '-NoProfile', '-NonInteractive', '-Command', script], seconds=25).decode('utf-8-sig'))
        _require(native['machine'] == _core().TARGET['host'] and native['format'] == 'NTFS' and native['freeBytes'] >= _core().LIMITS['hostDiskFreeBytes'] and native['availableBytes'] >= floor, 'native-resource-floor')
        _require(len(native['files']) == 2 and {item['name'] for item in native['files']} == {'os.raw', 'data.raw'}, 'native-disk-set')
        for item in native['files']:
            _require(item['size'] == (40 if item['name'] == 'os.raw' else 600) * 1024**3 and item['allocatedBytes'] >= item['size'] and not any(attribute in item['attributes'] for attribute in ('SparseFile', 'Compressed', 'ReparsePoint')), 'native-allocation')
        self._record('native-resources.json', native)
        self._record('host-resources.json', {'availableBytes': available, 'diskFreeBytes': free})
        if self.ssh_ready:
            disk = json.loads(self._ssh(['python3', '-c', 'import os,json; p="/srv/memories-qualification"; m=os.path.ismount(p); s=os.statvfs(p) if m else None; print(json.dumps({"mounted":m,"free":s.f_bavail*s.f_frsize if m else 0,"size":s.f_blocks*s.f_frsize if m else 0}))']))
            # A static local PV is a declared capacity, not a hard quota. Both
            # the PVC budget and actual filesystem headroom are observed.
            _require(disk['mounted'] or not self.kubeconfig.exists(), 'guest-data-unmounted')
            if disk['mounted']:
                _require(disk['free'] >= 150 * 1024**3 and disk['size'] - disk['free'] < 280 * 1024**3, 'guest-storage-admission')
            self._record('guest-storage.json', disk)
        return True

    def preflight(self):
        preflight_started = time.monotonic()
        self.authority()
        for path, digest in self.transport['hostTools'].items():
            _require(_digest(Path(path).resolve()) == digest, 'tool-drift')
        transport_root = Path(self.transport['root'])
        helper = Path(self.plan['sourceRoot']) / 'tools/access_telemetry_functional_guest.py'
        _require((transport_root / helper.name).read_bytes() == helper.read_bytes(), 'guest-helper-drift')
        expected = _render(transport_root, Path(self.plan['sourceRoot']), self.transport['applicationImages'], verify_existing=True)
        expected, values = _complete_config(expected, self.root, self.transport['applicationImages'], _digest(helper))
        _require(json.loads((transport_root / 'guest-config.json').read_text()) == expected and json.loads((transport_root / 'dapr-values.json').read_text()) == values, 'source-render-drift')
        self.authority()
        self._retained_disks()
        for port in (12274, 16443):
            with socket.socket() as probe:
                probe.bind(('127.0.0.1', port))
        _require(not Path('/home/administrator/.local/state/hexalith/q27-2fe71a5c/functional.sock').exists(), 'socket-exists')
        # Retain the independently verified 40-GiB OS content identity from
        # the closed recovery; never boot a merely same-sized replacement.
        _require(time.monotonic() - preflight_started < 120, 'preflight-deadline')
        self._retained_os_content()
        _require(time.monotonic() - preflight_started < 120, 'preflight-deadline')
        result = self.resource_floors()
        _require(time.monotonic() - preflight_started <= 120, 'preflight-deadline')
        return result

    def _retained_os_content(self):
        """Read the fixed OS through native .NET while retaining Linux identity."""
        self.authority()
        path = Path(_core().TARGET['diskDirectory']) / 'os.raw'
        descriptor = os.open(path, os.O_RDONLY | os.O_NOFOLLOW)
        try:
            before = os.fstat(descriptor)
            identity = _core()._file_metadata(before)
            size = 40 * 1024**3
            _require(stat.S_ISREG(before.st_mode) and before.st_nlink == 1 and before.st_size == size and _core()._file_metadata(path.lstat()) == identity, 'retained-os-identity')
            started = time.monotonic()
            raw = self._run(_NATIVE_OS_HASH_ARGV, seconds=90)
            _require(time.monotonic() - started <= 90, 'os-read-deadline')
            after = os.fstat(descriptor)
            _require(_core()._file_metadata(after) == identity and _core()._file_metadata(path.lstat()) == identity, 'retained-os-identity')
            self.authority()
            native = _core()._json(raw.decode('utf-8-sig').encode())
            _require(type(native) is dict and set(native) == {'machine', 'sha256', 'bytesHashed', 'startPosition', 'endPosition', 'streamLengthBefore', 'streamLengthAfter', 'readOnly', 'unchanged', 'before', 'after'}, 'native-os-schema')
            _require(native['machine'] == 'DESKTOP-VIOG240' and native['sha256'] == RETAINED_OS_SHA256 and native['readOnly'] is True and native['unchanged'] is True, 'retained-os-content')
            _require(all(type(native[name]) is int and native[name] == expected for name, expected in (('bytesHashed', size), ('startPosition', 0), ('endPosition', size), ('streamLengthBefore', size), ('streamLengthAfter', size))), 'native-os-full-read')
            for item in (native['before'], native['after']):
                _require(type(item) is dict and set(item) == {'fullName', 'length', 'creationUtcTicks', 'lastWriteUtcTicks', 'attributes'} and item['fullName'] == _NATIVE_OS_PATH, 'native-os-identity')
                _require(all(type(item[name]) is int and item[name] >= 0 for name in ('length', 'creationUtcTicks', 'lastWriteUtcTicks', 'attributes')) and item['length'] == size and item['attributes'] & 3584 == 0, 'native-os-metadata')
            _require(native['before'] == native['after'], 'native-os-drift')
            self._record('retained-os-content.json', native)
        finally:
            os.close(descriptor)
        return True

    def _retained_disks(self):
        for name, expected in (self.retry_disk_metadata or self.transport['diskIdentities']).items():
            info = (Path(_core().TARGET['diskDirectory']) / name).lstat()
            actual = {key: getattr(info, attr) for key, attr in (('device', 'st_dev'), ('inode', 'st_ino'), ('uid', 'st_uid'), ('size', 'st_size'), ('mtimeNs', 'st_mtime_ns'), ('ctimeNs', 'st_ctime_ns'))}
            _require(actual == expected and stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_blocks * 512 >= info.st_size, 'disk-drift')
        return True

    def claim_single_run_budget(self):
        return self.authority.claim()

    def _vm_identity(self):
        pidfile = _core().PRIVATE_TARGET_ROOT / 'control/functional-qemu.pid'
        pid = int(pidfile.read_text().strip())
        proc = Path('/proc') / str(pid)
        cmdline = (proc / 'cmdline').read_bytes().rstrip(b'\0').split(b'\0')
        expected = [item.encode() for item in self.argv[self.argv.index('/usr/bin/qemu-system-x86_64'):]]
        _require(proc.stat().st_uid == 1000 and cmdline == expected, 'qemu-identity')
        return {'pid': pid, 'startTicks': (proc / 'stat').read_text().rsplit(')', 1)[1].split()[19],
                'bootId': Path('/proc/sys/kernel/random/boot_id').read_text().strip(), 'uuid': _core().TARGET['uuid']}

    def _same_vm(self):
        try:
            return self.identity is not None and self._vm_identity() == self.identity
        except (OSError, ValueError, _core().FunctionalRetentionError):
            return False

    def _qmp(self, command):
        _require(command in {'query-uuid', 'query-status', 'system_powerdown', 'quit'} and self._same_vm(), 'qmp-identity')
        with socket.socket(socket.AF_UNIX) as channel:
            channel.settimeout(2)
            channel.connect('/home/administrator/.local/state/hexalith/q27-2fe71a5c/functional.sock')
            stream = channel.makefile('rb')
            def receive():
                count = 0
                while True:
                    raw = stream.readline(1048577)
                    count += len(raw)
                    _require(raw and count <= 1048576, 'qmp-output')
                    value = json.loads(raw)
                    if 'event' not in value:
                        return value
            _require('QMP' in receive(), 'qmp-greeting')
            channel.sendall(b'{"execute":"qmp_capabilities"}\r\n')
            _require('return' in receive(), 'qmp-capabilities')
            channel.sendall((json.dumps({'execute': command}) + '\r\n').encode())
            result = receive()
            _require('return' in result, 'qmp-command')
            return result['return']

    def launch(self, attempt, readiness_seconds=600):
        _require(attempt in (1, 2) and readiness_seconds == 600 and self.identity is None, 'launch-bound')
        self.authority()
        self.resource_floors(starting=True)
        self._retained_disks()
        if not self.assets_staged:
            _core().stage_transport_assets(self.root, self.authority)
            self.assets_staged = True
        self.resource_floors(starting=True)
        argv = list(_core().fixed_boot_argv())
        argv[argv.index('15300s')] = str(max(1, int(15300 - (time.monotonic() - self.begin)))) + 's'
        position = argv.index('-drive', argv.index('virtio-blk-pci,drive=qual-data,serial=memories-qual-data')) + 1
        argv[position] = 'file=' + self.transport['files']['seed'] + ',media=cdrom,readonly=on'
        self.argv = ['/usr/bin/sudo', '-n', '/usr/bin/setpriv', '--reuid=1000', '--regid=991', '--init-groups', *argv]
        # A host timeout survives the agent process; bounded output goes to
        # private files checked at each readiness step and at restoration.
        self.vm_buffers = [bytearray(), bytearray()]
        self.authority()
        for executable in ('/usr/bin/sudo', '/usr/bin/setpriv', '/usr/bin/timeout', '/usr/bin/qemu-system-x86_64'):
            _require(_digest(Path(executable).resolve()) == self.transport['hostTools'][executable], 'tool-drift')
        self._retained_disks()
        self.process = subprocess.Popen(self.argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True, close_fds=True)
        for stream in (self.process.stdout, self.process.stderr):
            os.set_blocking(stream.fileno(), False)
        deadline = time.monotonic() + readiness_seconds
        while time.monotonic() < deadline:
            self.authority()
            self._pump_vm()
            _require(self.process.poll() is None, 'qemu-launch-failed')
            try:
                self.identity = self._vm_identity()
                _require(self._qmp('query-uuid')['UUID'].lower() == _core().TARGET['uuid'] and self._qmp('query-status')['running'] is True, 'qmp-target')
                raw = self._ssh(['true'], seconds=10)
                self.ssh_ready = True
                self._record('qemu-identity.json', self.identity)
                return True
            except (FileNotFoundError, ConnectionRefusedError, ConnectionResetError, BrokenPipeError, TimeoutError, _core().TransportUncertain):
                time.sleep(2)
        raise _core().TransportUncertain('functional-target-readiness-timeout')

    def guest_identity(self):
        script = 'import pathlib,json; print(json.dumps({"uuid":pathlib.Path("/sys/class/dmi/id/product_uuid").read_text().strip(),"hostname":pathlib.Path("/etc/hostname").read_text().strip(),"marker":pathlib.Path("/etc/memories-qualification-target").read_text()}))'
        identity = json.loads(self._ssh(['python3', '-c', script]))
        _require(identity['uuid'].lower() == _core().TARGET['uuid'] and identity['hostname'] == _core().TARGET['guest'] and 'environment=non-Production' in identity['marker'] and 'instance=' + _core().TARGET['uuid'] in identity['marker'], 'guest-identity')
        self._record('guest-identity.json', identity)
        return True

    def setup(self):
        self._ssh(['mkdir', '-m', '700', GUEST])
        for relative in ('assets', 'assets/images'):
            self._ssh(['mkdir', '-p', '-m', '700', GUEST + '/' + relative])
        paths = [(Path(self.transport['root']) / name, name) for name in ('access_telemetry_functional_guest.py', 'guest-config.json', 'dapr-values.json')]
        catalog = _core().artifact_catalog()
        for item in catalog['downloads'] + catalog['images']:
            paths.append((self.root / item['path'], item['path']))
        for item in self.transport['applicationImages'].values():
            paths.append((Path(item['archivePath']), Path(item['archivePath']).relative_to(self.root).as_posix()))
        for source, destination in paths:
            _require(self._same_vm(), 'copy-target-identity')
            self._run(['/usr/bin/scp', *self._ssh_args(), '-P', '12274', str(source), 'qualifier@127.0.0.1:' + GUEST + '/' + destination], seconds=300)
        for stage in ('bootstrap', 'applications'):
            observation = json.loads(self._ssh(['sudo', '-n', 'python3', GUEST + '/access_telemetry_functional_guest.py', stage, self.transport['renderedObjectsSha256']], seconds=1800))
            self._record('setup-' + stage + '.json', observation)
            if stage == 'bootstrap':
                raw = self._ssh(['sudo', '-n', 'cat', '/etc/rancher/k3s/k3s.yaml'], sensitive=True)
                import yaml
                config = yaml.safe_load(raw)
                _require(len(config['clusters']) == len(config['contexts']) == len(config['users']) == 1, 'kubeconfig-shape')
                config['clusters'][0]['cluster']['server'] = 'https://127.0.0.1:16443'
                config['contexts'][0]['name'] = _core().TARGET['context']
                config['current-context'] = _core().TARGET['context']
                _require(not any('exec' in user['user'] or 'auth-provider' in user['user'] for user in config['users']), 'kubeconfig-plugin')
                _write(self.kubeconfig, yaml.safe_dump(config).encode())
        return True

    def _patch(self, kind, name, obj, operations, *, restoring=False):
        tests = [{'op': 'test', 'path': '/metadata/uid', 'value': obj['metadata']['uid']}, {'op': 'test', 'path': '/metadata/resourceVersion', 'value': obj['metadata']['resourceVersion']}]
        self._kubectl('patch', kind, name, '--type=json', '-p', json.dumps(tests + operations, separators=(',', ':')), restoring=restoring)

    def startup_probes(self):
        # Ownership is recorded before the first mutable gate/scale operation,
        # including uncertain transport so restoration can reconcile it.
        for name in ('memories-access-telemetry-clock', 'memories-access-telemetry', 'memories'):
            obj = self._get('deployment', name)
            _require(obj['spec']['replicas'] == 0, 'unexpected-active-deployment')
            self.deployments[name] = obj['metadata']['uid']
        for name in self.deployments:
            self.renew_owned_gate()
            obj = self._get('deployment', name)
            _require(obj['metadata']['uid'] == self.deployments[name], 'startup-deployment-owner')
            self._patch('deployment', name, obj, [{'op': 'replace', 'path': '/spec/replicas', 'value': 1}])
            self._kubectl('rollout', 'status', 'deployment/' + name, '--timeout=300s', seconds=310)
            self._pod(name)
            self.renew_owned_gate()
        deadline = time.monotonic() + 120
        for observation in range(3):
            state = self._actor_state(allow_missing=True)
            _require(time.monotonic() <= deadline, 'actor-startup-deadline')
            if state is not None:
                self.before_sequence = state['ReminderSequence']
                break
            _require(observation < 2 and time.monotonic() + 30 < deadline, 'actor-startup-deadline')
            time.sleep(30)
        self.setup_complete = True
        return True

    def acquire_owned_gate(self):
        self.lease = self._get('lease', 'access-telemetry-qualification')
        self.gate = self._get('configmap', 'access-telemetry-qualification-gate')
        obj = self._get('lease', 'access-telemetry-qualification')
        _require(obj['metadata']['uid'] == self.lease['metadata']['uid'] and obj['spec'].get('holderIdentity', '') == '', 'lease-acquisition')
        self._patch('lease', 'access-telemetry-qualification', obj, [{'op': 'test', 'path': '/spec/holderIdentity', 'value': ''}, {'op': 'replace', 'path': '/spec/holderIdentity', 'value': self.holder}, {'op': 'replace', 'path': '/spec/leaseDurationSeconds', 'value': 900}])
        self.renew_owned_gate()
        return True

    def renew_owned_gate(self):
        lease = self._get('lease', 'access-telemetry-qualification')
        _require(lease['metadata']['uid'] == self.lease['metadata']['uid'] and lease['spec']['holderIdentity'] == self.holder, 'lease-owner')
        now = datetime.now(timezone.utc)
        self._patch('lease', 'access-telemetry-qualification', lease, [{'op': 'test', 'path': '/spec/holderIdentity', 'value': self.holder}, {'op': 'add', 'path': '/spec/renewTime', 'value': now.isoformat().replace('+00:00', 'Z')}])
        gate = self._get('configmap', 'access-telemetry-qualification-gate')
        _require(gate['metadata']['uid'] == self.gate['metadata']['uid'], 'gate-owner')
        value = {'schemaVersion': 1, 'state': 'enabled', 'profileSha256': _core().STORY_27_4_PROFILE_SHA256, 'expiresUtcMs': int(now.timestamp() * 1000) + 600000}
        self._patch('configmap', 'access-telemetry-qualification-gate', gate, [{'op': 'replace', 'path': '/data/gate.json', 'value': json.dumps(value, separators=(',', ':'))}])
        readback = self._get('lease', 'access-telemetry-qualification')
        _require(readback['spec']['holderIdentity'] == self.holder, 'lease-readback')
        return True

    def emit(self, run_id, segment, emitted_ms):
        self.segments[segment] = emitted_ms
        result = json.loads(self._exec('memories', _core().fixed_workload_script(run_id, segment, emitted_ms)))
        self._record(segment + '-accounting.json', result)
        return result

    def _states(self, run_id, segment, *, buckets=False):
        return json.loads(self._exec('memories-access-telemetry', _core().fixed_state_probe_script(run_id, segment, self.segments[segment], buckets=buckets)))

    def read_cohort(self, run_id, segment):
        records = []
        for item in self._states(run_id, segment):
            record = item['value']
            _require(type(record) is dict, 'missing-record')
            def milliseconds(value):
                return (_core()._utc(value) - datetime(1970, 1, 1, tzinfo=timezone.utc)) // timedelta(milliseconds=1)
            records.append({'recordId': record['recordId'], 'envelopeHash': record['envelopeHash'],
                            'emittedUtcMs': milliseconds(record['emittedAtUtc']),
                            'expiresUtcMs': milliseconds(record['expiresAtUtc']),
                            'acceptedUtcMs': milliseconds(record['acceptedAtUtc'])})
        result = {'consistency': 'strong', 'records': records}
        self._record(segment + '-strong-records.json', result)
        return result

    def _actor_state(self, *, allow_missing=False):
        raw = self._exec('memories-access-telemetry', 'wget -qO- -T 5 -t 1 --header="dapr-api-token: $DAPR_API_TOKEN" http://127.0.0.1:3500/v1.0/actors/AccessTelemetryLifecycleActor/global/state/lifecycle-control')
        obj = json.loads(raw)
        if obj is None and allow_missing:
            return None  # Successful readiness observation; never replay startup.
        _require(type(obj) is dict, 'actor-state')
        normalized = {}
        for key in ('ReminderSequence', 'RetainedRecordCount', 'LastPurgeUnixMilliseconds'):
            choices = [name for name in (key, key[0].lower() + key[1:]) if name in obj]
            _require(len(choices) == 1, 'actor-state-field')
            normalized[key] = obj[choices[0]]
        _require(type(normalized['ReminderSequence']) is int and type(normalized['RetainedRecordCount']) is int and (normalized['LastPurgeUnixMilliseconds'] is None or type(normalized['LastPurgeUnixMilliseconds']) is int), 'actor-state')
        return normalized

    def observe_purge(self, run_id, segment):
        from access_telemetry_producer_common import _qualification_record_ids
        ids = _qualification_record_ids(run_id, segment)
        records = self._states(run_id, segment)
        # Positive physical tuple proof distinguishes TTL-filtered absence
        # from a claimed actor delete. A missing physical row alone never
        # proves which process deleted it. Unknown dispositions cannot pass.
        query = _core().fixed_expired_physical_query(run_id, segment)
        pods = json.loads(self._kubectl('get', 'pods', '-l', 'app.kubernetes.io/name=access-telemetry-postgresql', '-o', 'json'))['items']
        _require(len(pods) == 1, 'postgresql-pod')
        expired = json.loads(self._kubectl('exec', pods[0]['metadata']['name'], '-c', 'postgresql', '--',
                            'psql', '-X', '-A', '-t', '-v', 'ON_ERROR_STOP=1', '-U', 'memories_admin', '-d', 'memories_access_telemetry', '-c', query))['expiredRecordIds']
        _require(type(expired) is list and all(type(value) is str and value in ids for value in expired), 'physical-corroboration')
        absent = [record_id for record_id, item in zip(ids, records) if item['value'] is None]
        self.ttl_absent.update(set(absent) & set(expired))
        buckets = self._states(run_id, segment, buckets=True)
        present_index = {entry['recordId'] for bucket in buckets if bucket['value'] is not None for entry in bucket['value']['entries']}
        actor = self._actor_state()
        result = {'consistency': 'strong', 'absentRecordIds': absent,
                  'removedIndexIds': [record_id for record_id in ids if record_id not in present_index],
                  'reminderSequenceBefore': self.before_sequence, 'reminderSequenceAfter': actor['ReminderSequence'],
                  'progressObservedUtcMs': actor['LastPurgeUnixMilliseconds'] or 0,
                  'actorDeletedRecordIds': [], 'ttlAbsentRecordIds': [record_id for record_id in ids if record_id in self.ttl_absent]}
        self._record(segment + '-purge.json', result)
        return result

    def disable_owned_gate(self):
        if self.gate is None:
            return not self.deployments
        self._restore_owner()
        obj = self._get('configmap', 'access-telemetry-qualification-gate', restoring=True)
        _require(obj['metadata']['uid'] == self.gate['metadata']['uid'], 'restore-gate-owner')
        value = {'schemaVersion': 1, 'state': 'disabled', 'profileSha256': _core().STORY_27_4_PROFILE_SHA256, 'expiresUtcMs': 0}
        self._patch('configmap', 'access-telemetry-qualification-gate', obj, [{'op': 'replace', 'path': '/data/gate.json', 'value': json.dumps(value, separators=(',', ':'))}], restoring=True)
        return True

    def release_owned_lease(self):
        if self.lease is None:
            return not self.deployments
        self._restore_owner()
        obj = self._get('lease', 'access-telemetry-qualification', restoring=True)
        _require(obj['metadata']['uid'] == self.lease['metadata']['uid'] and obj['spec'].get('holderIdentity', '') == self.holder, 'restore-lease-owner')
        try:
            self._patch('lease', 'access-telemetry-qualification', obj, [{'op': 'test', 'path': '/spec/holderIdentity', 'value': self.holder}, {'op': 'replace', 'path': '/spec/holderIdentity', 'value': ''}, {'op': 'replace', 'path': '/spec/leaseDurationSeconds', 'value': 0}], restoring=True)
        except _core().TransportUncertain:
            pass  # Reconcile the same intended result, without replaying it.
        current = self._get('lease', 'access-telemetry-qualification', restoring=True)
        _require(current['metadata']['uid'] == self.lease['metadata']['uid'] and current['spec'].get('holderIdentity', '') == '' and current['spec']['leaseDurationSeconds'] == 0, 'lease-release-readback')
        self.released_lease_version = current['metadata']['resourceVersion']
        return True

    def _restore_owner(self):
        _require(self.lease is not None, 'restore-owner-unbound')
        lease = self._get('lease', 'access-telemetry-qualification', restoring=True)
        _require(lease['metadata']['uid'] == self.lease['metadata']['uid'], 'restore-lease-identity')
        if self.released_lease_version is None:
            _require(lease['spec'].get('holderIdentity', '') == self.holder, 'restore-foreign-owner')
        else:
            _require(lease['spec'].get('holderIdentity', '') == '' and lease['metadata']['resourceVersion'] == self.released_lease_version, 'restore-release-changed')
        return True

    def scale_owned_services_zero(self):
        failed = False
        for name, uid in self.deployments.items():
            try:
                self._restore_owner()
                obj = self._get('deployment', name, restoring=True)
                _require(obj['metadata']['uid'] == uid, 'restore-deployment-owner')
                self._patch('deployment', name, obj, [{'op': 'replace', 'path': '/spec/replicas', 'value': 0}], restoring=True)
            except _core()._SEMANTIC_FAILURES:
                failed = True
        _require(not failed, 'scale-restoration-incomplete')
        return True

    def terminal_api_readback(self):
        _require(self.gate is not None and self.lease is not None and len(self.deployments) == 3, 'terminal-identity-incomplete')
        deadline = time.monotonic() + 120
        while True:
            gate = self._get('configmap', 'access-telemetry-qualification-gate', restoring=True)
            lease = self._get('lease', 'access-telemetry-qualification', restoring=True)
            _require(gate['metadata']['uid'] == self.gate['metadata']['uid'] and json.loads(gate['data']['gate.json'])['state'] == 'disabled' and lease['metadata']['uid'] == self.lease['metadata']['uid'] and lease['spec'].get('holderIdentity', '') == '' and lease['spec']['leaseDurationSeconds'] == 0, 'terminal-gate-lease')
            pending = False
            for name, uid in self.deployments.items():
                obj = self._get('deployment', name, restoring=True)
                _require(obj['metadata']['uid'] == uid and obj['spec']['replicas'] == 0, 'terminal-deployment')
                pods = json.loads(self._kubectl('get', 'pods', '-l', 'app.kubernetes.io/name=' + name, '-o', 'json', restoring=True))['items']
                pending |= bool(pods) or obj.get('status', {}).get('readyReplicas', 0) != 0 or obj.get('status', {}).get('replicas', 0) != 0
            if not pending:
                self._record('terminal-api.json', {'gateDisabled': True, 'leaseEmpty': True, 'desiredReadyAndPodsZero': True, 'observedBeforeGuestStop': True})
                return True
            _require(time.monotonic() < deadline, 'terminal-pods-pending')
            time.sleep(2)

    def stop_owned_guest(self):
        if self.process is None:
            return True
        if self.identity is None and self.process.poll() is None:
            try:
                self.identity = self._vm_identity()
            except (OSError, ValueError):
                pass
        if self._same_vm():
            try:
                self._qmp('system_powerdown')
            except (OSError, ValueError):
                pass
            deadline = time.monotonic() + 30
            while self._same_vm() and time.monotonic() < deadline:
                time.sleep(.25)
            if self._same_vm():
                try:
                    self._qmp('quit')
                except (OSError, ValueError):
                    pass
        try:
            self.process.wait(timeout=70)
        except subprocess.TimeoutExpired:
            # A process group is owned only by this Popen handle. Never kill a
            # process found by name or a PID file without the argv/start match.
            _require(self._same_vm(), 'stop-process-identity')
            os.kill(self.identity['pid'], signal.SIGTERM)
            try:
                self.process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                _require(self._same_vm(), 'kill-process-identity')
                os.kill(self.identity['pid'], signal.SIGKILL)
                self.process.wait(timeout=10)
        self._pump_vm(restoring=True)
        for suffix, value in zip(('stdout', 'stderr'), self.vm_buffers):
            self._record('qemu.' + suffix, bytes(value))
        for stream in (self.process.stdout, self.process.stderr):
            stream.close()
        stopped = self.stopped_readback()
        if self.identity is not None:
            snapshot = {}
            for name, initial in self.transport['diskIdentities'].items():
                info = (Path(_core().TARGET['diskDirectory']) / name).lstat()
                observed = {key: getattr(info, attr) for key, attr in (('device', 'st_dev'), ('inode', 'st_ino'), ('uid', 'st_uid'), ('size', 'st_size'), ('mtimeNs', 'st_mtime_ns'), ('ctimeNs', 'st_ctime_ns'))}
                _require(all(observed[key] == initial[key] for key in ('device', 'inode', 'uid', 'size')) and stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_blocks * 512 >= info.st_size, 'stopped-disk-drift')
                snapshot[name] = observed
            self.retry_disk_metadata = snapshot
            self._record('stopped-disk-identities.json', snapshot)
        self.identity = None
        self.ssh_ready = False
        return stopped

    def stopped_readback(self):
        _require(self.process is None or self.process.poll() is not None, 'guest-still-running')
        _require(not self._same_vm(), 'guest-still-running')
        for port in (12274, 16443):
            with socket.socket() as probe:
                _require(probe.connect_ex(('127.0.0.1', port)) != 0, 'forward-still-listening')
        self._record('guest-stopped.json', {'ownedProcessStopped': True, 'forwardsClosed': True, 'terminalApiProofIsSeparate': True})
        return True
