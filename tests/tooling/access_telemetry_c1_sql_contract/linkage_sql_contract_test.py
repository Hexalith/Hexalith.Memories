"""Disposable, real PostgreSQL verification of the collector's emitted SQL.

Requires Docker, PowerShell >=7.5, OpenSSL and the exact approved image locally.
No Kubernetes client, live credentials, tenant tables or published ports are used.
"""

import hashlib
import json
import os
import re
import selectors
import signal
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
import uuid
from types import SimpleNamespace
from unittest import mock
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
COLLECTOR = ROOT / 'tools/verify-access-telemetry-c1-linkage.ps1'
HELPER = ROOT / 'tools/access-telemetry-c1-component-backend.ps1'
DEPLOYMENT = ROOT / 'deploy/kubernetes/base/access-telemetry-postgresql.yaml'
IMAGE = 'docker.io/library/postgres@sha256:0377e72c5289ed2f98cf61b1a9c2db9eb9d300317fe14244492fbc94343b3d04'
HOST = 'access-telemetry-postgresql.hexalith-memories.svc.cluster.local'
DATABASE = 'memories_access_telemetry'
ROLE = 'memories_access_telemetry_runtime'
LABEL = 'hexalith.c1-sql-contract-run'
LIMIT = 65536
LANE_RESULTS = []


def sha256(data):
    return hashlib.sha256(data).hexdigest()


class CommandFailure(RuntimeError):
    def __init__(self, result):
        super().__init__(result['failure'])
        self.result = result


def bounded(command, *, input_bytes=None, timeout=15, check=True):
    """Retain bounded diagnostics for exits, timeouts and output-limit failures."""
    result = {'command': command, 'exitCode': None, 'stdout': '', 'stderr': ''}
    streams = {'stdout': bytearray(), 'stderr': bytearray()}
    process = None
    deadline = time.monotonic() + timeout
    try:
        process = subprocess.Popen(command, stdin=subprocess.PIPE if input_bytes is not None else subprocess.DEVNULL,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
        if input_bytes is not None:
            process.stdin.write(input_bytes)
            process.stdin.close()
        with selectors.DefaultSelector() as selector:
            selector.register(process.stdout, selectors.EVENT_READ, 'stdout')
            selector.register(process.stderr, selectors.EVENT_READ, 'stderr')
            while selector.get_map():
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    raise RuntimeError('local-command-timeout')
                for key, _ in selector.select(min(remaining, 0.2)):
                    chunk = os.read(key.fileobj.fileno(), 4096)
                    if not chunk:
                        selector.unregister(key.fileobj)
                    else:
                        buffer = streams[key.data]
                        overflow = len(buffer) + len(chunk) > LIMIT
                        buffer.extend(chunk[:LIMIT - len(buffer)])
                        if overflow:
                            raise RuntimeError('local-command-output-limit')
        process.wait(timeout=max(0.01, deadline - time.monotonic()))
        if check and process.returncode:
            result['failure'] = 'local-command-failed'
    except subprocess.TimeoutExpired:
        result['failure'] = 'local-command-timeout'
    except Exception as error:
        result['failure'] = str(error) if isinstance(error, RuntimeError) else 'local-command-' + type(error).__name__
    finally:
        if process is not None:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=5)
            result['exitCode'] = process.returncode
            for stream in (process.stdin, process.stdout, process.stderr):
                if stream:
                    stream.close()
        result.update({name: bytes(value).decode('utf-8', errors='replace') for name, value in streams.items()})
    if 'failure' in result:
        raise CommandFailure(result)
    return result


def recorded_command(state, command, **kwargs):
    try:
        result = bounded(command, **kwargs)
    except CommandFailure as error:
        state.commands.append(error.result)
        raise
    state.commands.append(result)
    return result


def read_session_response(stream, timeout=8):
    response = bytearray()
    deadline = time.monotonic() + timeout
    with selectors.DefaultSelector() as selector:
        selector.register(stream, selectors.EVENT_READ)
        while b'\n' not in response:
            remaining = deadline - time.monotonic()
            if remaining <= 0 or not selector.select(remaining):
                raise RuntimeError('runtime-session-readiness-timeout')
            chunk = os.read(stream.fileno(), 128)
            if not chunk or len(response) + len(chunk) > 128:
                raise RuntimeError('runtime-session-query-failed')
            response.extend(chunk)
    if response != b'1\n':
        raise RuntimeError('runtime-session-query-failed')
    return bytes(response)


def assert_manifest_contract(path=DEPLOYMENT):
    text = path.read_text()
    markers = list(re.finditer(r'^        - name: postgresql\n', text, re.MULTILINE))
    if len(markers) != 1:
        raise RuntimeError('deployment-container-contract-invalid')
    container = text[markers[0].end():].split('      volumes:', 1)[0]
    expected_env = {
        'POSTGRES_USER': 'memories_admin', 'POSTGRES_DB': DATABASE,
        'POSTGRES_PASSWORD_FILE': '/run/secrets/postgresql/admin-password',
        'POSTGRES_INITDB_ARGS': '--auth-host=scram-sha-256 --auth-local=peer --data-checksums',
        'PGDATA': '/var/lib/postgresql/18/docker',
    }
    for name, expected in expected_env.items():
        values = re.findall(r'^            - name: ' + re.escape(name) +
                            r'\n              value: (.+)$', container, re.MULTILINE)
        if values != [expected]:
            raise RuntimeError('deployment-identity-contract-invalid: ' + name)
    arguments = container.split('          args:\n', 1)[1].split('          env:', 1)[0]
    expected_args = {
        'hba_file': '/etc/postgresql/pg_hba.conf', 'ident_file': '/etc/postgresql/pg_ident.conf',
        'ssl': 'on', 'ssl_min_protocol_version': 'TLSv1.2',
        'ssl_cert_file': '/run/postgresql-tls/tls.crt', 'ssl_key_file': '/run/postgresql-tls/tls.key',
        'ssl_ca_file': '/run/postgresql-tls/ca.crt', 'password_encryption': 'scram-sha-256',
        'log_statement': 'none',
    }
    for name, expected in expected_args.items():
        values = re.findall(r'^            - ' + re.escape(name) + r'=(.+)$', arguments, re.MULTILINE)
        if values != [expected]:
            raise RuntimeError('deployment-tls-or-auth-contract-invalid: ' + name)


def inspect_owned_resource(state, kind, name):
    result = state.run_command([state.docker, kind, 'inspect', name, '--format',
                               '{{index .Labels "' + LABEL + '"}}' if kind == 'network' else
                               '{{index .Config.Labels "' + LABEL + '"}}'], check=False)
    if result['exitCode']:
        absent = {'Error response from daemon: No such container: ' + name,
                  'Error: No such container: ' + name} if kind == 'container' else {
                  'Error response from daemon: network ' + name + ' not found',
                  'Error: No such network: ' + name}
        if result['exitCode'] == 1 and not result['stdout'].strip() and result['stderr'].strip() in absent:
            return False
        raise RuntimeError('cleanup-inspection-failed: ' + name)
    if result['stdout'].strip() != state.run_id:
        raise RuntimeError('cleanup-ownership-mismatch: ' + name)
    return True


def remove_owned_resource(state, kind, name):
    if inspect_owned_resource(state, kind, name):
        command = [state.docker, kind, 'rm'] + (['--force', '--volumes'] if kind == 'container' else []) + [name]
        state.cleanup_results.append(state.run_command(command))


def receipt_status(state, errors):
    expected = set(state.expected_tests)
    actual = [test['test'] for test in state.test_results]
    complete = len(actual) == len(expected) and set(actual) == expected
    return 'passed' if expected and complete and all(test['passed'] for test in state.test_results) and state.observations and not errors else 'failed'


def finish_receipt(state):
    errors = []
    try:
        try:
            state.stop_sessions()
        except Exception as error:
            errors.append('session-cleanup-failed: ' + type(error).__name__)
        for kind, name in reversed(state.owned_resources):
            try:
                remove_owned_resource(state, kind, name)
            except Exception as error:
                errors.append(str(error))
        for relative, expected in state.receipt['sources'].items():
            try:
                if sha256((ROOT / relative).read_bytes()) != expected:
                    errors.append('tracked-source-changed: ' + relative)
            except Exception:
                errors.append('tracked-source-unreadable: ' + relative)
    finally:
        # Credential deletion precedes serialization and every receipt-writing path.
        try:
            shutil.rmtree(state.work)
        except Exception as error:
            errors.append('private-workspace-cleanup-failed: ' + type(error).__name__)
    state.receipt.update(commands=state.commands, observations=state.observations, cleanup=state.cleanup_results,
                         cleanupErrors=errors, expectedTests=state.expected_tests, testResults=state.test_results,
                         privateWorkspaceRemoved=not state.work.exists(), status=receipt_status(state, errors))
    receipt_path = state.evidence / 'receipt.json'
    try:
        receipt_text = json.dumps(state.receipt, indent=2) + '\n'
        for secret in getattr(state, 'secrets', []):
            if secret in receipt_text or any(secret in path.read_text() for path in state.evidence.glob('*.json')):
                for path in state.evidence.glob('*.json'):
                    path.unlink()
                raise RuntimeError('receipt-secret-leak')
        receipt_path.write_text(receipt_text)
        for path in state.evidence.glob('*.json'):
            path.chmod(0o400)
    except Exception as error:
        errors.append('receipt-finalization-failed: ' + type(error).__name__)
        # No source values, exception text or credentials enter the fallback.
        fallback = ('{"status":"failed","reason":"receipt-finalization-failed","privateWorkspaceRemoved":' +
                    ('true' if not state.work.exists() else 'false') + '}\n').encode()
        if receipt_path.exists():
            receipt_path.chmod(0o600)
        descriptor = os.open(receipt_path, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
        try:
            os.write(descriptor, fallback)
        finally:
            os.close(descriptor)
    for path in state.evidence.glob('*.json'):
        path.chmod(0o400)
    print('SQL contract receipt: ' + str(receipt_path) + ' sha256=' + sha256(receipt_path.read_bytes()), flush=True)
    if errors:
        raise RuntimeError('; '.join(errors))


class ReceiptOutcomeCase(unittest.TestCase):
    test_results = LANE_RESULTS

    def run(self, result=None):
        result = result if result is not None else self.defaultTestResult()
        try:
            return super().run(result)
        finally:
            def owns(test):
                return test is self or getattr(test, 'test_case', None) is self
            failed = any(owns(test) for test, _ in result.failures + result.errors + result.skipped + result.expectedFailures)
            failed = failed or any(owns(test) for test in result.unexpectedSuccesses)
            self.test_results.append({'test': self.id(), 'passed': not failed})


def manifest_block(name):
    """Extract a literal ConfigMap block without copying its configuration."""
    text = DEPLOYMENT.read_text()
    prefix = '  ' + name + ': |\n'
    if text.count(prefix) != 1:
        raise RuntimeError('deployment-config-block-missing-or-duplicate: ' + name)
    lines = []
    for line in text.split(prefix, 1)[1].splitlines():
        if line and not line.startswith('    '):
            break
        lines.append(line[4:] if line else '')
    return '\n'.join(lines).rstrip() + '\n'


# Only these function AST extents are evaluated. Neither source's top-level
# statements, approval loading, deployment probes nor live collector are run.
DRIVER = r'''
param([string]$ConfigPath)
$ErrorActionPreference = 'Stop'
$config = Get-Content -Raw $ConfigPath | ConvertFrom-Json -DateKind String
if ($PSVersionTable.PSVersion -lt [version]'7.5') { throw 'powershell-7.5-required' }
function Import-Definitions([string]$Path, [string[]]$Names) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors)
    if ($errors.Count) { throw 'source-parse-failed' }
    foreach ($name in $Names) {
        $matches = @($ast.FindAll({ param($node)
            $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name
        }, $true))
        if ($matches.Count -ne 1) { throw 'function-missing-or-duplicate' }
        . ([scriptblock]::Create(($matches[0].Extent.Text -replace '^function\s+', 'function script:')))
    }
}
Import-Definitions $config.collector @('Get-TextSha256', 'Add-SourceHash', 'Assert-SecretSafeOutput',
    'Assert-SecretSafeMetadata', 'Assert-UniqueMetadataJsonProperties', 'Assert-MetadataJsonShape',
    'Get-RequiredProperty', 'Get-CollectionIdentity', 'Read-LinkageJson', 'Assert-LinkageFields',
    'Read-LinkageTime', 'Get-LinkageSessions')
Import-Definitions $config.helper @('Assert-C1CredentialFields', 'Get-C1RequiredString')
$script:sourceLedger = [Collections.Generic.List[object]]::new()
$expectedContext = 'jpiquot@local'; $namespace = 'hexalith-memories'
$lifecycle = @{ podIp = $config.clientIp; containers = @{ daprd = @{ startedAtUtc = $config.startedAtUtc } } }
$backend = @{ pod = $config.server; containers = @{ postgresql = @{ startedAtUtc = $config.startedAtUtc } } }
function Invoke-KubectlObservation {
    param([string]$Purpose, [string[]]$Arguments, [switch]$SkipSourceHash)
    [IO.File]::WriteAllText($config.arguments, (ConvertTo-Json -InputObject $Arguments -Compress))
    $raw = & $config.python $config.test '--execute-emitted-command' $ConfigPath
    if ($LASTEXITCODE -ne 0) { throw 'local-psql-execution-failed' }
    $text = $raw -join "`n"
    Assert-SecretSafeOutput $text
    return $text
}
try {
    $value = Get-LinkageSessions 'isolated-sql-contract'
    @{ valid = $true; value = $value; sources = @($script:sourceLedger) } | ConvertTo-Json -Depth 12 -Compress
}
catch {
    @{ valid = $false; denial = $_.Exception.Message; sources = @($script:sourceLedger) } | ConvertTo-Json -Depth 12 -Compress
}
'''


def execute_emitted_command(config_path):
    """Local adapter: preserve the emitted env/psql vector after exec routing."""
    config = json.loads(Path(config_path).read_text())
    arguments = json.loads(Path(config['arguments']).read_text())
    expected = ['--context', 'jpiquot@local', '-n', 'hexalith-memories',
                'exec', config['server'], '-c', 'postgresql', '--']
    if arguments[:9] != expected:
        raise RuntimeError('unexpected-collector-transport')
    local = arguments[9:]
    if local[:1] != ['env'] or 'psql' not in local or local[-2] != '--command':
        raise RuntimeError('unexpected-emitted-psql-contract')
    # Adverse observer identity uses real psql flags, never manufactured JSON.
    for flag, value in config.get('observerOverrides', {}).items():
        index = next(i for i, arg in enumerate(local) if arg.startswith(flag + '='))
        local[index] = flag + '=' + value
    failure = None
    try:
        result = bounded([config['docker'], 'exec', '--user', 'postgres', config['server'], *local])
    except CommandFailure as error:
        result = error.result
        failure = error
    Path(config['rawLog']).write_text(json.dumps(result, indent=2) + '\n')
    if failure is not None:
        raise failure
    print(result['stdout'], end='')


class LinkageSqlContractTests(ReceiptOutcomeCase):
    @classmethod
    def setUpClass(cls):
        cls.run_id = uuid.uuid4().hex
        cls.work = Path(tempfile.mkdtemp(prefix='c1-sql-private-'))
        cls.evidence = Path(tempfile.mkdtemp(prefix='c1-sql-receipt-'))
        cls.server = 'c1-sql-server-' + cls.run_id
        cls.client = 'c1-sql-client-' + cls.run_id
        cls.network = 'c1-sql-network-' + cls.run_id
        cls.sessions = []
        cls.commands = []
        cls.observations = []
        cls.test_results = LANE_RESULTS
        cls.cleanup_results = []
        cls.owned_resources = []
        cls.docker = None
        cls.expected_tests = [case.__module__ + '.' + case.__qualname__ + '.' + name
                              for case in (cls, HarnessFailureTests)
                              for name in unittest.TestLoader().getTestCaseNames(case)]
        cls.receipt = {'schemaVersion': 'hexalith.access-telemetry.c1.sql-contract/v1',
                       'scope': 'disposable-local-sql-only', 'runId': cls.run_id, 'status': 'running',
                       'connectionLinkage': 'not-evaluated', 'gateStatus': 'not-evaluated',
                       'componentBehavior': 'not-evaluated', 'productionLifecycleWrites': 'not-evaluated',
                       'productionGatePassed': False, 'independentDisposition': 'pending',
                       'sources': {}}
        cls.addClassCleanup(cls.finish)
        for source in (COLLECTOR, HELPER, DEPLOYMENT, Path(__file__)):
            cls.receipt['sources'][str(source.relative_to(ROOT))] = sha256(source.read_bytes())
        assert_manifest_contract()
        cls.docker = shutil.which('docker')
        cls.pwsh = shutil.which('pwsh')
        cls.openssl = shutil.which('openssl')
        if not all((cls.docker, cls.pwsh, cls.openssl)):
            raise RuntimeError('missing-required-docker-pwsh-or-openssl')
        version = cls.run_command([cls.docker, 'version', '--format', '{{.Server.Os}}/{{.Server.Arch}}'])
        if version['stdout'].strip() != 'linux/amd64':
            raise RuntimeError('docker-linux-amd64-required')
        identity = json.loads(cls.run_command([cls.docker, 'image', 'inspect', IMAGE, '--format',
                                      '{{json .}}'])['stdout'])
        if (identity['Os'], identity['Architecture']) != ('linux', 'amd64') or not any(value in identity['RepoDigests'] for value in
                (IMAGE, IMAGE.removeprefix('docker.io/library/'))):
            raise RuntimeError('approved-child-image-required')
        cls.receipt['image'] = {'requested': IMAGE, 'id': identity['Id'], 'repoDigests': identity['RepoDigests'],
                                'platform': 'linux/amd64'}
        cls.started_at = datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')
        cls.prepare_files()
        cls.owned_resources.append(('network', cls.network))
        cls.run_command([cls.docker, 'network', 'create', '--internal', '--label', LABEL + '=' + cls.run_id, cls.network])
        server_start = ('mkdir -p /run/postgresql-tls /run/secrets/postgresql /etc/postgresql; '
                        'cp /contract/tls/ca.crt /contract/tls/tls.crt /contract/tls/tls.key /run/postgresql-tls/; '
                        'cp /contract/*-password /run/secrets/postgresql/; '
                        'cp /contract/pg_ident.conf /contract/pg_hba.conf /etc/postgresql/; '
                        'chown -R postgres:postgres /run/postgresql-tls /run/secrets/postgresql; '
                        'chmod 600 /run/postgresql-tls/tls.key /run/secrets/postgresql/*; '
                        'exec docker-entrypoint.sh postgres -c hba_file=/etc/postgresql/pg_hba.conf '
                        '-c ident_file=/etc/postgresql/pg_ident.conf -c ssl=on -c ssl_min_protocol_version=TLSv1.2 '
                        '-c ssl_cert_file=/run/postgresql-tls/tls.crt -c ssl_key_file=/run/postgresql-tls/tls.key '
                        '-c ssl_ca_file=/run/postgresql-tls/ca.crt -c password_encryption=scram-sha-256 '
                        '-c log_statement=none')
        cls.owned_resources.append(('container', cls.server))
        cls.run_command([cls.docker, 'run', '--detach', '--pull=never', '--platform=linux/amd64',
                 '--name', cls.server, '--label', LABEL + '=' + cls.run_id, '--network', cls.network,
                 '--network-alias', HOST, '--network-alias', 'wrong.' + HOST,
                 '--mount', 'type=bind,src=' + str(cls.work) + ',dst=/contract,readonly',
                 '--mount', 'type=bind,src=' + str(cls.work / '010-access-telemetry.sh') +
                 ',dst=/docker-entrypoint-initdb.d/010-access-telemetry.sh,readonly',
                 '--tmpfs', '/var/lib/postgresql:rw,size=256m', '--tmpfs', '/run/postgresql-tls:rw,size=4m',
                 '--tmpfs', '/run/secrets/postgresql:rw,size=4m', '--env', 'POSTGRES_USER=memories_admin',
                 '--env', 'POSTGRES_DB=' + DATABASE, '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/postgresql/admin-password',
                 '--env', 'POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256 --auth-local=peer --data-checksums',
                 '--entrypoint', '/bin/sh', IMAGE, '-ec', server_start])
        deadline = time.monotonic() + 45
        while True:
            result = cls.run_command([cls.docker, 'exec', '--user', 'postgres', cls.server,
                              'pg_isready', '--quiet', '--host=/var/run/postgresql',
                              '--username=memories_admin', '--dbname=' + DATABASE], check=False)
            if result['exitCode'] == 0:
                # initdb's temporary server can be ready; wait for final entrypoint completion.
                logs = cls.run_command([cls.docker, 'logs', cls.server])
                if 'PostgreSQL init process complete; ready for start up.' in logs['stdout']:
                    break
            if time.monotonic() >= deadline:
                raise RuntimeError('postgres-startup-timeout')
            time.sleep(0.2)
        client_start = ('mkdir -p /run/c1; cp /contract/client.pgpass /run/c1/pgpass; '
                        'cp /contract/tls/ca.crt /run/c1/ca.crt; chown -R postgres:postgres /run/c1; '
                        'chmod 600 /run/c1/pgpass; exec sleep 600')
        cls.owned_resources.append(('container', cls.client))
        cls.run_command([cls.docker, 'run', '--detach', '--pull=never', '--platform=linux/amd64',
                 '--name', cls.client, '--label', LABEL + '=' + cls.run_id, '--network', cls.network,
                 '--mount', 'type=bind,src=' + str(cls.work) + ',dst=/contract,readonly',
                 '--tmpfs', '/var/lib/postgresql:rw,size=4m', '--tmpfs', '/run/c1:rw,size=4m', '--entrypoint', '/bin/sh', IMAGE, '-ec', client_start])
        cls.client_ip = cls.run_command([cls.docker, 'inspect', cls.client, '--format',
                                '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}'])['stdout'].strip()
        for container in (cls.server, cls.client):
            inspection = json.loads(cls.run_command([cls.docker, 'inspect', container, '--format',
                                            '{{json .HostConfig.PortBindings}}'])['stdout'])
            if inspection:
                raise RuntimeError('published-ports-prohibited')
        cls.driver = cls.work / 'driver.ps1'
        cls.driver.write_text(DRIVER)

    @classmethod
    def run_command(cls, command, **kwargs):
        return recorded_command(cls, command, **kwargs)

    @classmethod
    def prepare_files(cls):
        # Secrets stay in the private disposable directory and container tmpfs.
        cls.secrets = [uuid.uuid4().hex + uuid.uuid4().hex for _ in range(2)]
        for name, value in zip(('admin-password', 'runtime-password'), cls.secrets):
            (cls.work / name).write_text(value)
            (cls.work / name).chmod(0o600)
        cls.hba = manifest_block('pg_hba.conf')
        (cls.work / 'pg_hba.conf').write_text(cls.hba)
        (cls.work / 'pg_hba.conf').chmod(0o644)
        (cls.work / 'pg_ident.conf').write_text(manifest_block('pg_ident.conf'))
        (cls.work / '010-access-telemetry.sh').write_text(manifest_block('010-access-telemetry.sh'))
        (cls.work / '010-access-telemetry.sh').chmod(0o644)
        tls = cls.work / 'tls'
        tls.mkdir()
        cls.run_command([cls.openssl, 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '1',
                 '-subj', '/CN=C1 disposable local CA', '-keyout', str(tls / 'ca.key'), '-out', str(tls / 'ca.crt')])
        cls.run_command([cls.openssl, 'req', '-newkey', 'rsa:2048', '-nodes', '-subj', '/CN=' + HOST,
                 '-keyout', str(tls / 'tls.key'), '-out', str(tls / 'server.csr')])
        (tls / 'extensions').write_text('subjectAltName=DNS:' + HOST + '\nextendedKeyUsage=serverAuth\n')
        cls.run_command([cls.openssl, 'x509', '-req', '-in', str(tls / 'server.csr'), '-CA', str(tls / 'ca.crt'),
                 '-CAkey', str(tls / 'ca.key'), '-CAcreateserial', '-days', '1', '-extfile', str(tls / 'extensions'),
                 '-out', str(tls / 'tls.crt')])
        cls.run_command([cls.openssl, 'verify', '-CAfile', str(tls / 'ca.crt'), '-verify_hostname', HOST, str(tls / 'tls.crt')])
        cls.receipt['tls'] = {'hostname': HOST, 'caSha256': sha256((tls / 'ca.crt').read_bytes()),
                              'certificateSha256': sha256((tls / 'tls.crt').read_bytes()), 'sslmode': 'verify-full'}
        pgpass = '\n'.join('*:5432:*:' + role + ':' + secret for role, secret in
                           zip(('memories_admin', ROLE), cls.secrets)) + '\n'
        (cls.work / 'client.pgpass').write_text(pgpass)
        (cls.work / 'client.pgpass').chmod(0o600)

    def setUp(self):
        self.addCleanup(self.stop_sessions)

    @classmethod
    def stop_sessions(cls):
        for process in cls.sessions:
            if process.poll() is None:
                try:
                    process.stdin.write(b'\\q\n')
                    process.stdin.flush()
                    process.wait(timeout=5)
                finally:
                    if process.poll() is None:
                        process.kill()
                        process.wait(timeout=5)
            if hasattr(process, 'contract_record'):
                process.contract_record['exitCode'] = process.returncode
                process.contract_record['stderr'] = process.stderr.read(LIMIT).decode(errors='replace')
            for stream in (process.stdin, process.stdout, process.stderr):
                stream.close()
        cls.sessions.clear()

    def client_command(self, role=ROLE, database=DATABASE, sslmode='verify-full', host=HOST):
        return [self.docker, 'exec', '-i', '--user', 'postgres', self.client, 'env',
                'PGPASSFILE=/run/c1/pgpass', 'PGCONNECT_TIMEOUT=5', 'PGSSLROOTCERT=/run/c1/ca.crt',
                'PGSSLMODE=' + sslmode, 'PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=5000',
                'psql', '-X', '--no-password', '--set=ON_ERROR_STOP=1', '--host=' + host,
                '--port=5432', '--username=' + role, '--dbname=' + database, '--tuples-only', '--no-align']

    def start_session(self, **kwargs):
        command = self.client_command(**kwargs)
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.sessions.append(process)
        process.stdin.write(b'SELECT 1;\n')
        process.stdin.flush()
        record = {'command': command, 'exitCode': None, 'stdout': '', 'stderr': '', 'result': 'session-readiness-pending'}
        self.commands.append(record)
        process.contract_record = record
        try:
            record['stdout'] = read_session_response(process.stdout).decode()
            record['result'] = 'idle-session-open-until-test-cleanup'
        except Exception as error:
            record['result'] = str(error)
            raise

    def observe(self, *, source=COLLECTOR, overrides=None):
        number = len(self.observations)
        config = {'collector': str(source), 'helper': str(HELPER), 'test': str(Path(__file__).resolve()),
                  'python': sys.executable, 'docker': self.docker, 'server': self.server, 'clientIp': self.client_ip,
                  'startedAtUtc': self.started_at, 'arguments': str(self.work / 'arguments.json'),
                  'rawLog': str(self.evidence / ('psql-' + str(number) + '.json')),
                  'observerOverrides': overrides or {}}
        config_path = self.work / 'adapter.json'
        config_path.write_text(json.dumps(config))
        result = self.run_command([self.pwsh, '-NoProfile', '-File', str(self.driver), str(config_path)])
        value = json.loads(result['stdout'])
        arguments = json.loads(Path(config['arguments']).read_text())
        raw = json.loads(Path(config['rawLog']).read_text())
        self.observations.append({'test': self.id(), 'collectorSha256': sha256(source.read_bytes()),
                                  'querySha256': sha256(arguments[-1].encode()), 'emittedArguments': arguments,
                                  'emittedCommandSha256': sha256(('kubectl ' + '\x1f'.join(arguments)).encode()),
                                  'rawLog': Path(config['rawLog']).name,
                                  'rawLogSha256': sha256(Path(config['rawLog']).read_bytes()),
                                  'validation': value})
        self.assertEqual(0, raw['exitCode'])
        parsed = json.loads(raw['stdout'])
        if parsed['database'] == DATABASE:
            self.receipt['server'] = {key: parsed[key] for key in
                                      ('serverVersion', 'serverVersionNum', 'database', 'user', 'systemUser', 'localSocket')}
        if not value['valid']:
            self.assertEqual([], value['sources'])
        return value, parsed

    def assert_denial(self, denial, **kwargs):
        result, _ = self.observe(**kwargs)
        self.assertFalse(result['valid'], result)
        self.assertEqual(denial, result['denial'])

    def test_valid_idle_tls_emitted_contract(self):
        self.start_session()
        result, raw = self.observe()
        self.assertTrue(result['valid'], result)
        self.assertEqual(raw, result['value'])
        self.assertEqual('180006', raw['serverVersionNum'])
        self.assertTrue(raw['serverVersion'].startswith('18.6'))
        self.assertEqual((DATABASE, 'memories_admin', 'peer:postgres', True),
                         tuple(raw[key] for key in ('database', 'user', 'systemUser', 'localSocket')))
        self.receipt['server'] = {key: raw[key] for key in ('serverVersion', 'serverVersionNum', 'database', 'user', 'systemUser', 'localSocket')}
        session = raw['sessions'][0]
        self.assertIs(type(session['pid']), int)
        self.assertIs(type(session['clientPort']), int)
        self.assertIs(type(session['ssl']), bool)
        self.assertEqual((ROLE, DATABASE, self.client_ip, 'idle', True),
                         tuple(session[key] for key in ('usename', 'datname', 'clientAddr', 'state', 'ssl')))
        self.assertIn(session['tlsVersion'], ('TLSv1.2', 'TLSv1.3'))
        times = [session[key] for key in ('backendStartUtc', 'queryStartUtc', 'stateChangeUtc')] + [raw['observedAtUtc']]
        self.assertTrue(all(value.endswith('Z') for value in times))
        self.assertEqual(sorted(datetime.fromisoformat(value.replace('Z', '+00:00')) for value in times),
                         [datetime.fromisoformat(value.replace('Z', '+00:00')) for value in times])
        self.assertEqual(1, len(result['sources']))

    def test_missing_and_duplicate_sessions_are_denied(self):
        self.assert_denial('session-attribution-ambiguous')
        self.start_session()
        self.start_session()
        self.assert_denial('session-attribution-ambiguous')

    def test_wrong_runtime_role_and_database_are_denied(self):
        for kwargs in ({'role': 'memories_admin'}, {'database': 'postgres'}):
            with self.subTest(**kwargs):
                self.start_session(**kwargs)
                self.assert_denial('session-identity-or-tls-invalid')
                self.stop_sessions()

    def test_wrong_observer_database_is_denied(self):
        self.start_session()
        self.assert_denial('backend-peer-identity-invalid', overrides={'--dbname': 'postgres'})

    def test_non_tls_session_is_denied(self):
        # Only this owned disposable server admits the deliberately adverse session.
        hba = self.work / 'pg_hba.conf'
        try:
            hba.write_text('hostnossl all all ' + self.client_ip + '/32 scram-sha-256\n' + self.hba)
            self.run_command([self.docker, 'exec', '--user', 'root', self.server, 'cp',
                      '/contract/pg_hba.conf', '/etc/postgresql/pg_hba.conf'])
            self.run_command([self.docker, 'exec', '--user', 'postgres', self.server, 'pg_ctl', 'reload',
                      '-D', '/var/lib/postgresql/18/docker'])
            self.start_session(sslmode='disable')
            self.assert_denial('session-identity-or-tls-invalid')
        finally:
            self.stop_sessions()
            hba.write_text(self.hba)
            self.run_command([self.docker, 'exec', '--user', 'root', self.server, 'cp',
                      '/contract/pg_hba.conf', '/etc/postgresql/pg_hba.conf'])
            self.run_command([self.docker, 'exec', '--user', 'postgres', self.server, 'pg_ctl', 'reload',
                      '-D', '/var/lib/postgresql/18/docker'])

    def test_tls_requires_matching_service_dns(self):
        command = self.client_command(host='wrong.' + HOST) + ['--command', 'SELECT 1;']
        result = self.run_command(command, check=False)
        self.assertNotEqual(0, result['exitCode'])
        self.assertIn('does not match host name', result['stderr'])
        self.assert_denial('session-attribution-ambiguous')

    def test_emitted_alias_mutation_is_detected_without_source_changes(self):
        self.start_session()
        source = COLLECTOR.read_bytes()
        original = b'AS "clientPort"'
        self.assertEqual(1, source.count(original))
        mutant = self.work / 'mutated-collector.ps1'
        mutant.write_bytes(source.replace(original, b'AS "brokenClientPort"'))
        self.assert_denial('session-fields-invalid', source=mutant)
        self.assertEqual(source, COLLECTOR.read_bytes())

    def test_old_inet_text_projection_is_rejected_without_source_changes(self):
        # Reintroduce the real /32 defect only in temporary source bytes.
        self.start_session()
        source = COLLECTOR.read_bytes()
        original = b'host(a.client_addr) AS "clientAddr"'
        self.assertEqual(1, source.count(original))
        diagnostic = self.work / 'diagnostic-collector.ps1'
        diagnostic.write_bytes(source.replace(original, b'a.client_addr::text AS "clientAddr"'))
        result, raw = self.observe(source=diagnostic)
        self.observations[-1]['regressionMutation'] = 'old-inet-text-projection'
        self.assertFalse(result['valid'], result)
        self.assertEqual('session-identity-or-tls-invalid', result['denial'])
        self.assertEqual(self.client_ip + '/32', raw['sessions'][0]['clientAddr'])
        self.assertEqual(source, COLLECTOR.read_bytes())

    def test_missing_prerequisite_fails_without_skips_and_cleans_private_setup(self):
        result = self.run_command([sys.executable, str(Path(__file__).resolve()), '--probe-missing-prerequisite'],
                                  check=False)
        self.assertNotEqual(0, result['exitCode'])
        self.assertIn('missing-required-docker-pwsh-or-openssl', result['stderr'])
        self.assertNotIn('skipped', result['stderr'])
        receipt_path = Path(result['stdout'].split('SQL contract receipt: ', 1)[1].split(' sha256=', 1)[0])
        receipt = json.loads(receipt_path.read_text())
        self.assertEqual('failed', receipt['status'])
        self.assertEqual([], receipt['cleanupErrors'])
        self.assertTrue(receipt['privateWorkspaceRemoved'])
        self.receipt['missingPrerequisiteReceipt'] = {'path': str(receipt_path),
                                                     'sha256': sha256(receipt_path.read_bytes())}

    def test_timeout_fails_and_owned_disposable_container_is_removed(self):
        name = 'c1-sql-timeout-' + self.run_id
        self.owned_resources.append(('container', name))
        try:
            self.run_command([self.docker, 'run', '--detach', '--pull=never', '--name', name,
                              '--network', self.network, '--label', LABEL + '=' + self.run_id,
                              '--tmpfs', '/var/lib/postgresql:rw,size=4m',
                              '--entrypoint', 'sleep', IMAGE, '60'])
            with self.assertRaisesRegex(RuntimeError, '^local-command-timeout$'):
                self.run_command([self.docker, 'exec', name, 'sleep', '30'], timeout=0.2)
        finally:
            remove_owned_resource(self, 'container', name)
        self.assertFalse(inspect_owned_resource(self, 'container', name))

    @classmethod
    def finish(cls):
        finish_receipt(cls)


class HarnessFailureTests(ReceiptOutcomeCase):
    def state(self):
        temporary = tempfile.TemporaryDirectory(prefix='c1-sql-harness-unit-')
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        work = root / 'private'
        evidence = root / 'evidence'
        work.mkdir()
        evidence.mkdir()
        secret = uuid.uuid4().hex
        (work / 'disposable-credential').write_text(secret)
        return SimpleNamespace(work=work, evidence=evidence, secrets=[secret], commands=[], observations=[{}],
                               cleanup_results=[], owned_resources=[], run_id=uuid.uuid4().hex, docker='docker',
                               expected_tests=['probe.test'], test_results=[{'test': 'probe.test', 'passed': True}],
                               receipt={'sources': {}}, stop_sessions=lambda: None)

    def read_receipt(self, state):
        self.assertFalse(state.work.exists())
        receipt = json.loads((state.evidence / 'receipt.json').read_text())
        self.assertTrue(receipt['privateWorkspaceRemoved'])
        self.assertNotIn(state.secrets[0], (state.evidence / 'receipt.json').read_text())
        return receipt

    def test_cleanup_errors_are_recorded_after_actual_test_cleanup(self):
        class Probe(ReceiptOutcomeCase):
            test_results = []

            def test_probe(self):
                self.addCleanup(lambda: (_ for _ in ()).throw(RuntimeError('cleanup-probe-failure')))

        probe = Probe('test_probe')
        result = unittest.TestResult()
        probe.run(result)
        self.assertEqual(1, len(result.errors))
        state = self.state()
        state.expected_tests = [probe.id()]
        state.test_results = Probe.test_results
        finish_receipt(state)
        self.assertEqual('failed', self.read_receipt(state)['status'])

    def test_skips_and_partial_suites_cannot_produce_passed_receipts(self):
        for mode in ('skip', 'partial'):
            with self.subTest(mode=mode):
                class Probe(ReceiptOutcomeCase):
                    test_results = []

                    def test_one(self):
                        if mode == 'skip':
                            self.skipTest('intentional-outcome-probe')

                    def test_two(self):
                        pass

                first, second = Probe('test_one'), Probe('test_two')
                result = unittest.TestResult()
                first.run(result)
                if mode == 'skip':
                    second.run(result)
                    self.assertEqual(1, len(result.skipped))
                state = self.state()
                state.expected_tests = [first.id(), second.id()]
                state.test_results = Probe.test_results
                finish_receipt(state)
                self.assertEqual('failed', self.read_receipt(state)['status'])

    def test_failed_inspection_fails_receipt_and_only_exact_absence_is_accepted(self):
        for absent in (False, True):
            with self.subTest(absent=absent):
                state = self.state()
                name = 'c1-sql-inspect-' + state.run_id
                state.owned_resources.append(('container', name))
                result = {'exitCode': 1, 'stdout': '', 'stderr':
                          'Error response from daemon: No such container: ' + name if absent else
                          'Cannot connect to the Docker daemon'}
                state.run_command = lambda *args, **kwargs: result
                if absent:
                    finish_receipt(state)
                else:
                    with self.assertRaisesRegex(RuntimeError, 'cleanup-inspection-failed'):
                        finish_receipt(state)
                self.assertEqual('passed' if absent else 'failed', self.read_receipt(state)['status'])

    def test_missing_source_retains_failed_receipt_and_removes_private_credentials(self):
        state = self.state()
        state.receipt['sources'] = {'missing-c1-source-' + uuid.uuid4().hex: '0' * 64}
        with self.assertRaisesRegex(RuntimeError, 'tracked-source-unreadable'):
            finish_receipt(state)
        self.assertEqual('failed', self.read_receipt(state)['status'])

    def test_serialization_and_write_errors_still_delete_credentials_and_retain_safe_failure(self):
        for target, name, error in ((json, 'dumps', TypeError('serialization-probe')),
                                    (Path, 'write_text', OSError('writing-probe'))):
            with self.subTest(name=name):
                state = self.state()
                with mock.patch.object(target, name, side_effect=error):
                    with self.assertRaisesRegex(RuntimeError, 'receipt-finalization-failed'):
                        finish_receipt(state)
                self.assertEqual('failed', self.read_receipt(state)['status'])

    def test_failed_exit_timeout_and_output_limit_retain_bounded_diagnostics(self):
        state = self.state()
        probes = [
            ("import sys; print('partial'); print('refused', file=sys.stderr); sys.exit(7)",
             'local-command-failed', 2),
            ("import time; print('started', flush=True); time.sleep(5)", 'local-command-timeout', 0.2),
            ("import sys; sys.stdout.write('x' * 65537)", 'local-command-output-limit', 2),
        ]
        for code, failure, timeout in probes:
            with self.subTest(failure=failure):
                with self.assertRaisesRegex(CommandFailure, '^' + failure + '$'):
                    recorded_command(state, [sys.executable, '-c', code], timeout=timeout)
                record = state.commands[-1]
                self.assertEqual(failure, record['failure'])
                self.assertIsInstance(record['exitCode'], int)
                self.assertLessEqual(len(record['stdout'].encode()), LIMIT)
                self.assertLessEqual(len(record['stderr'].encode()), LIMIT)
        self.assertEqual(7, state.commands[0]['exitCode'])
        self.assertIn('refused', state.commands[0]['stderr'])
        self.assertIn('started', state.commands[1]['stdout'])

    def test_adapter_retains_raw_log_before_propagating_psql_failure(self):
        state = self.state()
        arguments = state.work / 'arguments.json'
        config_path = state.work / 'config.json'
        raw = state.evidence / 'failed-psql.json'
        config_path.write_text(json.dumps({'arguments': str(arguments), 'server': 'owned-server',
                                          'docker': 'docker', 'rawLog': str(raw)}))
        arguments.write_text(json.dumps(['--context', 'jpiquot@local', '-n', 'hexalith-memories',
                                         'exec', 'owned-server', '-c', 'postgresql', '--',
                                         'env', 'psql', '--command', 'query-not-executed']))
        failure = {'command': ['docker', 'exec'], 'exitCode': 2, 'stdout': '',
                   'stderr': 'psql: database refused', 'failure': 'local-command-failed'}
        with mock.patch(__name__ + '.bounded', side_effect=CommandFailure(failure)):
            with self.assertRaises(CommandFailure):
                execute_emitted_command(config_path)
        self.assertEqual(failure, json.loads(raw.read_text()))

    def test_split_valid_session_response_succeeds(self):
        read_descriptor, write_descriptor = os.pipe()
        try:
            os.write(write_descriptor, b'1')
            original_read = os.read

            def split_read(descriptor, size):
                data = original_read(descriptor, size)
                if data == b'1':
                    os.write(write_descriptor, b'\n')
                return data

            with os.fdopen(read_descriptor, 'rb', buffering=0) as stream:
                with mock.patch.object(os, 'read', side_effect=split_read) as reader:
                    self.assertEqual(b'1\n', read_session_response(stream, timeout=1))
                    self.assertEqual(2, reader.call_count)
        finally:
            os.close(write_descriptor)

    def test_temporary_manifest_identity_auth_and_tls_drift_is_denied(self):
        source = DEPLOYMENT.read_bytes()
        assert_manifest_contract()
        state = self.state()
        manifest = state.work / 'deployment.yaml'
        mutations = [
            (b'value: memories_admin', b'value: another_admin'),
            (b'value: memories_access_telemetry', b'value: another_database'),
            (b'--auth-host=scram-sha-256 --auth-local=peer', b'--auth-host=trust --auth-local=trust'),
            (b'- ssl=on', b'- ssl=off'),
            (b'- ssl_min_protocol_version=TLSv1.2', b'- ssl_min_protocol_version=TLSv1.1'),
        ]
        for before, after in mutations:
            with self.subTest(before=before):
                self.assertEqual(1, source.count(before))
                manifest.write_bytes(source.replace(before, after))
                with self.assertRaisesRegex(RuntimeError, '^deployment-'):
                    assert_manifest_contract(manifest)
        self.assertEqual(source, DEPLOYMENT.read_bytes())


if __name__ == '__main__':
    if len(sys.argv) == 3 and sys.argv[1] == '--execute-emitted-command':
        execute_emitted_command(sys.argv[2])
    elif sys.argv[1:] == ['--probe-missing-prerequisite']:
        os.environ['PATH'] = ''
        unittest.main(argv=[sys.argv[0], 'LinkageSqlContractTests.test_valid_idle_tls_emitted_contract'])
    else:
        unittest.main()
