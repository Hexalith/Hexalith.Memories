# Historical VM implementation retained for evidence and offline regressions.
# The current functional-retention CLI never imports or dispatches this module.
"""Fixed guest-only setup for the functional checkpoint; never a public CLI.

Copied, hashed and invoked only by access_telemetry_functional_target after
operational admission. All paths, namespaces and commands are fixed here.
Secret output is kept in memory and never included in the observation stream.
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import selectors
import signal
import subprocess
import sys
import tarfile
import time

ROOT = Path('/var/tmp/memories-27-4-functional')
DATA = Path('/srv/memories-qualification')
NS = 'hexalith-memories-qualification'
UUID = '2fe71a5c-bdda-4767-8780-8f27f5bcd520'
K3S = '/usr/local/bin/memories-27-4-k3s'


def require(value, code):
    if not value:
        raise RuntimeError('functional-guest-' + code)


def run(argv, *, data=None, seconds=120):
    child = subprocess.Popen(argv, stdin=subprocess.PIPE if data is not None else subprocess.DEVNULL,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True,
                             env={**os.environ, 'KUBECONFIG': '/etc/rancher/k3s/k3s.yaml'})
    buffers, offset, deadline = [bytearray(), bytearray()], 0, time.monotonic() + seconds
    try:
        with selectors.DefaultSelector() as selector:
            for index, stream in enumerate((child.stdout, child.stderr)):
                os.set_blocking(stream.fileno(), False)
                selector.register(stream, selectors.EVENT_READ, index)
            if data is not None:
                require(len(data) <= 1048576, 'stdin-bound')
                os.set_blocking(child.stdin.fileno(), False)
                selector.register(child.stdin, selectors.EVENT_WRITE, 2)
            while selector.get_map() or child.poll() is None:
                require(time.monotonic() < deadline, 'command-deadline')
                for key, _ in selector.select(.2):
                    if key.data == 2:
                        if offset < len(data):
                            offset += os.write(child.stdin.fileno(), data[offset:offset + 65536])
                        if offset == len(data):
                            selector.unregister(child.stdin)
                            child.stdin.close()
                    else:
                        block = os.read(key.fileobj.fileno(), 65536)
                        if not block:
                            selector.unregister(key.fileobj)
                        else:
                            buffers[key.data].extend(block)
                            require(len(buffers[key.data]) <= 1048576, 'stream-bound')
        require(child.wait(timeout=2) == 0, 'command-failed')
        return bytes(buffers[0])
    finally:
        if child.poll() is None:
            os.killpg(child.pid, signal.SIGTERM)
            try:
                child.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(child.pid, signal.SIGKILL)
                child.wait(timeout=5)
        for stream in (child.stdout, child.stderr, child.stdin):
            if stream is not None and not stream.closed:
                stream.close()


def kubectl(*args, data=None, seconds=120):
    return run([K3S, 'kubectl', '--request-timeout=110s', *args], data=data, seconds=seconds)


def apply(objects):
    kubectl('apply', '-f', '-', data=json.dumps({'apiVersion': 'v1', 'kind': 'List', 'items': objects}).encode())


def wait_deployment(name, namespace=NS):
    kubectl('-n', namespace, 'rollout', 'status', 'deployment/' + name, '--timeout=300s', seconds=310)


def mount_data():
    require(Path('/sys/class/dmi/id/product_uuid').read_text().strip().lower() == UUID, 'uuid')
    require(Path('/etc/hostname').read_text().strip() == 'memories-qualification-27-4', 'hostname')
    marker = Path('/etc/memories-qualification-target').read_text()
    require('environment=non-Production' in marker and 'instance=' + UUID in marker, 'marker')
    ls = json.loads(run(['lsblk', '--json', '--bytes', '--output', 'NAME,PATH,SIZE,SERIAL,TYPE,FSTYPE,MOUNTPOINTS']))
    disks = {disk.get('serial'): disk for disk in ls['blockdevices'] if disk['type'] == 'disk'}
    require(set(disks) == {'memories-qual-os', 'memories-qual-data'}, 'disk-set')
    disk = disks['memories-qual-data']
    require(disk['size'] == 600 * 1024**3 and disks['memories-qual-os']['size'] == 40 * 1024**3, 'disk-size')
    require(disk['path'] == '/dev/vdb' and not disk.get('children'), 'data-device')
    claim = Path('/var/lib/memories-27-4-data-initialized')
    DATA.mkdir(mode=0o700, exist_ok=True)
    if claim.exists():
        require(claim.read_text() == UUID and disk.get('fstype') == 'ext4', 'existing-data')
        if not os.path.ismount(DATA):
            run(['mount', '-o', 'nodev,nosuid', '/dev/vdb', str(DATA)])
    else:
        signatures = json.loads(run(['wipefs', '--json', '--no-act', '/dev/vdb']))
        require(not signatures.get('signatures') and not disk.get('fstype') and not any(disk.get('mountpoints', [])), 'nonblank-data')
        # O_EXCL records this single destructive attempt before starting mkfs.
        # A failed/interrupted attempt is deliberately not automatically retried.
        with claim.open('x') as stream:
            stream.write(UUID)
            stream.flush()
            os.fsync(stream.fileno())
        run(['mkfs.ext4', '-F', '-m', '0', '-L', 'memories-qual-data', '/dev/vdb'], seconds=300)
        run(['mount', '-o', 'nodev,nosuid', '/dev/vdb', str(DATA)])
    require(os.path.ismount(DATA), 'data-not-mounted')
    free = os.statvfs(DATA).f_bavail * os.statvfs(DATA).f_frsize
    size = os.statvfs(DATA).f_blocks * os.statvfs(DATA).f_frsize
    require(free >= 500 * 1024**3, 'data-headroom')
    for name in ('k3s', 'postgresql', 'redis', 'falkordb', 'openbao', 'scheduler-0', 'scheduler-1', 'scheduler-2'):
        (DATA / name).mkdir(mode=0o750, exist_ok=True)
    os.chown(DATA / 'postgresql', 999, 999)
    return {'filesystemFreeBytes': free, 'filesystemSizeBytes': size, 'dataSerial': 'memories-qual-data', 'dataBytes': disk['size']}


def bootstrap(config):
    storage = mount_data()
    require(not Path('/etc/systemd/system/memories-27-4-k3s.service').exists(), 'cluster-already-exists')
    require(not Path(K3S).exists() and not Path('/etc/rancher/k3s/k3s.yaml').exists(), 'cluster-artifact-already-exists')
    asset = ROOT / config['assets']['k3s']['name']
    _verify_file(config['assets']['k3s']['name'], config['fileDigests'][config['assets']['k3s']['name']])
    shutil.copyfile(asset, K3S)
    os.chmod(K3S, 0o755)
    images = DATA / 'k3s/agent/images'
    images.mkdir(parents=True, exist_ok=True)
    _verify_file(config['assets']['k3s-airgap']['name'], config['fileDigests'][config['assets']['k3s-airgap']['name']])
    shutil.copyfile(ROOT / config['assets']['k3s-airgap']['name'], images / 'k3s-airgap-images-amd64.tar.zst')
    unit = '''[Unit]
Description=Story 27.4 isolated functional cluster
After=network.target
[Service]
Type=notify
ExecStart=/usr/local/bin/memories-27-4-k3s server --data-dir=/srv/memories-qualification/k3s --node-name=memories-qualification-27-4 --tls-san=127.0.0.1 --disable=traefik --disable=servicelb --disable=metrics-server --disable=local-storage --prefer-bundled-bin --write-kubeconfig-mode=600
KillMode=control-group
Delegate=yes
LimitNOFILE=1048576
Restart=no
TimeoutStartSec=300
[Install]
WantedBy=multi-user.target
'''
    Path('/etc/systemd/system/memories-27-4-k3s.service').write_text(unit)
    run(['systemctl', 'daemon-reload'])
    run(['systemctl', 'start', 'memories-27-4-k3s.service'], seconds=300)
    kubectl('wait', '--for=condition=Ready', 'node/memories-qualification-27-4', '--timeout=110s', seconds=120)
    # Import only reviewed staged archives. Registry pulls are impossible with
    # guest external networking restricted; every app uses IfNotPresent.
    for name in config['imageArchives']:
        identity = next((item for item in config['imageIdentities'] if item['path'] == name), None)
        if identity is None:
            _verify_file(name, config['fileDigests'][name])
        else:
            _verify_oci(ROOT / name, identity)
        run([K3S, 'ctr', 'images', 'import', '--platform', 'linux/amd64', '--digests', str(ROOT / name)], seconds=300)
    apply(config['bootstrapObjects'])
    # The immutable chart and executable are staged, not downloaded in guest.
    helm = ROOT / config['assets']['helm']['name']
    for name in (config['assets']['helm']['name'], config['assets']['dapr-chart']['name'], 'dapr-values.json'):
        _verify_file(name, config['fileDigests'][name])
    run(['tar', '-xzf', str(helm), '-C', str(ROOT), 'linux-amd64/helm'])
    run([str(ROOT / 'linux-amd64/helm'), 'install', 'dapr', str(ROOT / config['assets']['dapr-chart']['name']),
         '--namespace', 'dapr-system', '--create-namespace', '--values', str(ROOT / 'dapr-values.json'),
         '--wait', '--timeout', '300s'], seconds=310)
    apply(config['openbaoObjects'])
    wait_deployment('hexalith-keys', 'openbao')
    # The private kubeconfig is returned only over the pinned SSH channel.
    print(json.dumps({'stage': 'cluster-ready', 'node': 'memories-qualification-27-4', 'storage': storage, 'capacityCredit': False}))


def bao(*args, data=None):
    pods = json.loads(kubectl('-n', 'openbao', 'get', 'pods', '-l', 'app=hexalith-keys', '-o', 'json'))['items']
    require(len(pods) == 1, 'openbao-pod')
    return kubectl('-n', 'openbao', 'exec', '-i', pods[0]['metadata']['name'], '--', 'bao', *args, data=data)


def secrets_and_apps(config):
    initialized = json.loads(bao('operator', 'init', '-key-shares=1', '-key-threshold=1', '-format=json'))
    root = initialized['root_token']
    bao('write', '-format=json', 'sys/unseal', '-', data=json.dumps({'key': initialized['unseal_keys_b64'][0]}).encode())
    # Use the pod's stdin for sensitive payloads. No tokens in argv or reports.
    pods = json.loads(kubectl('-n', 'openbao', 'get', 'pods', '-l', 'app=hexalith-keys', '-o', 'json'))['items']
    pod = pods[0]['metadata']['name']

    def authenticated(arguments, value=None):
        script = 'IFS= read -r BAO_TOKEN; export BAO_TOKEN; exec bao "$@"'
        raw = root.encode() + b'\n' + (json.dumps(value).encode() if value is not None else b'')
        return kubectl('-n', 'openbao', 'exec', '-i', pod, '--', 'sh', '-ec', script, 'bao', *arguments, data=raw)

    authenticated(['secrets', 'enable', '-path=secret', 'kv-v2'])
    for prefix, values in config['vaultValues'].items():
        for name, fields in values.items():
            authenticated(['kv', 'put', '-mount=secret', prefix + '/' + name, '-'], fields)
    tokens = {}
    for lane in ('runtime', 'access-telemetry'):
        prefix = 'hexalith/memories/' + lane
        policy = 'memories-functional-' + lane
        text = 'path "secret/data/' + prefix + '/*" { capabilities = ["read"] }'
        # Policy text travels via stdin; it is fixed from the lane enumeration.
        script = 'IFS= read -r BAO_TOKEN; export BAO_TOKEN; exec bao policy write "$1" -'
        kubectl('-n', 'openbao', 'exec', '-i', pod, '--', 'sh', '-ec', script, 'bao', policy,
                data=root.encode() + b'\n' + text.encode())
        created = json.loads(authenticated(['token', 'create', '-policy=' + policy, '-orphan', '-no-default-policy', '-ttl=5h', '-explicit-max-ttl=5h', '-format=json']))
        tokens[lane] = created['auth']['client_token']
    objects = list(config['applicationSecretObjects'])
    for lane, token in tokens.items():
        objects.append({'apiVersion': 'v1', 'kind': 'Secret', 'metadata': {'name': 'openbao-' + lane + '-bootstrap', 'namespace': NS},
                        'stringData': {'token': token, 'ca.pem': config['openbaoCa']}})
    apply(objects)
    apply(config['applicationObjects'])
    kubectl('-n', NS, 'rollout', 'status', 'statefulset/access-telemetry-postgresql', '--timeout=300s', seconds=310)
    kubectl('-n', NS, 'rollout', 'status', 'statefulset/redis-stack', '--timeout=300s', seconds=310)
    kubectl('-n', NS, 'rollout', 'status', 'statefulset/falkordb', '--timeout=300s', seconds=310)
    # The real Redis configuration component's input, not a Dapr-state/SQL
    # record shortcut. Use its actual configuration.redis JSON wire value.
    password = config['redisPassword']
    pods = json.loads(kubectl('-n', NS, 'get', 'pods', '-l', 'app.kubernetes.io/name=redis-stack', '-o', 'json'))['items']
    require(len(pods) == 1, 'redis-pod')
    script = 'IFS= read -r REDISCLI_AUTH; export REDISCLI_AUTH; exec redis-cli SET retentionSeconds 3600'
    kubectl('-n', NS, 'exec', '-i', pods[0]['metadata']['name'], '-c', 'redis', '--', 'sh', '-ec', script, data=password.encode() + b'\n')
    # Revoke bootstrap root authority after scoped application tokens exist.
    authenticated(['token', 'revoke', '-self'])
    print(json.dumps({'stage': 'applications-installed', 'retentionSeconds': 3600}))


def _verify_oci(path, identity):
    """Verify the preserved root and every referenced amd64 blob before import."""
    descriptor = os.open(path, os.O_RDONLY | os.O_NOFOLLOW)
    with os.fdopen(descriptor, 'rb') as stream, tarfile.open(fileobj=stream) as archive:
        members = archive.getmembers()
        require(0 < len(members) <= 512 and len({item.name for item in members}) == len(members), 'oci-member-count')
        require(all(item.isfile() and (item.name in {'oci-layout', 'index.json'} or re.fullmatch(r'blobs/sha256/[0-9a-f]{64}', item.name)) and item.size <= 20 * 1024**3 for item in members), 'oci-member-shape')
        require(sum(item.size for item in members) <= 20 * 1024**3 and archive.extractfile('oci-layout').read(256) == b'{"imageLayoutVersion":"1.0.0"}', 'oci-layout')
        index_raw = archive.extractfile('index.json').read(1048577)
        require(len(index_raw) <= 1048576, 'oci-index-bound')
        index = json.loads(index_raw)
        require(index.get('schemaVersion') == 2 and len(index.get('manifests', [])) == 1, 'oci-index')
        root = index['manifests'][0]
        require(root['digest'] == 'sha256:' + identity['indexSha256'] and root.get('annotations', {}).get('io.containerd.image.name') == identity['reference'], 'oci-root')
        expected = {'oci-layout', 'index.json'}

        def read_blob(digest, length, *, parse=False):
            require(re.fullmatch(r'sha256:[0-9a-f]{64}', digest) and type(length) is int and 0 < length <= 20 * 1024**3, 'oci-blob-binding')
            name = 'blobs/sha256/' + digest[7:]
            expected.add(name)
            member = archive.getmember(name)
            require(member.size == length and (not parse or length <= 1048576), 'oci-blob-size')
            calculated = hashlib.sha256()
            content = bytearray()
            with archive.extractfile(member) as body:
                while block := body.read(1024**2):
                    calculated.update(block)
                    if parse:
                        content.extend(block)
            require(calculated.hexdigest() == digest[7:], 'oci-blob-drift')
            return json.loads(content) if parse else None

        manifest = read_blob(root['digest'], root['size'], parse=True)
        if 'manifests' in manifest:
            selected = [value for value in manifest['manifests'] if value.get('platform', {}).get('os') == 'linux' and value.get('platform', {}).get('architecture') == 'amd64']
            require(len(selected) == 1 and identity['platformManifestSha256'] in {None, selected[0]['digest'][7:]}, 'oci-platform')
            manifest = read_blob(selected[0]['digest'], selected[0]['size'], parse=True)
        config = read_blob(manifest['config']['digest'], manifest['config']['size'], parse=True)
        require(config.get('os') == 'linux' and config.get('architecture') == 'amd64', 'oci-config-platform')
        for blob in manifest['layers']:
            read_blob(blob['digest'], blob['size'])
        require({item.name for item in members} == expected, 'oci-extra-content')


def _verify_file(name, expected):
    path = ROOT / name
    require(path.is_relative_to(ROOT) and '..' not in path.parts and not path.is_symlink() and re.fullmatch(r'[0-9a-f]{64}', expected), 'input-path')
    descriptor = os.open(path, os.O_RDONLY | os.O_NOFOLLOW)
    digest = hashlib.sha256()
    with os.fdopen(descriptor, 'rb') as stream:
        before = os.fstat(stream.fileno())
        require(before.st_size <= 20 * 1024**3, 'input-size')
        while block := stream.read(1024**2):
            digest.update(block)
        after = os.fstat(stream.fileno())
        named = path.lstat()
        require(all((getattr(before, key) == getattr(after, key) == getattr(named, key)) for key in ('st_dev', 'st_ino', 'st_size', 'st_mtime_ns', 'st_ctime_ns')), 'input-metadata-drift')
    require(digest.hexdigest() == expected, 'input-drift')


def verify_inputs(config):
    for name, expected in config['fileDigests'].items():
        _verify_file(name, expected)
    for identity in config['imageIdentities']:
        _verify_oci(ROOT / identity['path'], identity)


def main():
    require(os.geteuid() == 0 and len(sys.argv) == 3 and sys.argv[1] in {'bootstrap', 'applications'} and re.fullmatch(r'[0-9a-f]{64}', sys.argv[2]), 'invocation')
    raw = (ROOT / 'guest-config.json').read_bytes()
    require(len(raw) <= 1048576 and hashlib.sha256(raw).hexdigest() == sys.argv[2], 'configuration-drift')
    config = json.loads(raw)
    verify_inputs(config)
    (bootstrap if sys.argv[1] == 'bootstrap' else secrets_and_apps)(config)


if __name__ == '__main__':
    try:
        main()
    except (RuntimeError, OSError, ValueError, KeyError, subprocess.SubprocessError):
        print('{"status":"rejected","reason":"functional-guest-setup-failed"}', file=sys.stderr)
        sys.exit(1)
