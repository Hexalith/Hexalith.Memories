import base64
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PACKAGE = REPO_ROOT / '_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/profile-candidate'


def replace_archived_bytes(archive, name, raw):
    archive['artifacts'][name] = {'sha256': hashlib.sha256(raw).hexdigest(), 'size_bytes': len(raw),
                                 'original_bytes_base64': base64.b64encode(raw).decode('ascii')}


class CandidateProfileVerificationTests(unittest.TestCase):
    def run_verifier(self, mutate=None, *, optimized=False):
        with tempfile.TemporaryDirectory() as temporary:
            package = Path(temporary) / 'candidate'
            shutil.copytree(PACKAGE, package)
            if mutate:
                mutate(package)
            command = [sys.executable]
            if optimized:
                command.append('-O')
            command += [str(package / 'verify_candidate.py'), '--repo-root', str(REPO_ROOT)]
            env = os.environ.copy()
            env['PYTHONDONTWRITEBYTECODE'] = '1'
            return subprocess.run(command, env=env, cwd=REPO_ROOT, text=True,
                                  capture_output=True, check=False, timeout=15)

    def assert_rejected(self, mutate, expected):
        for optimized in (False, True):
            with self.subTest(optimized=optimized):
                result = self.run_verifier(mutate, optimized=optimized)
                self.assertNotEqual(0, result.returncode, result.stdout)
                self.assertIn('FAIL: ' + expected, result.stderr)

    def test_package_replays_without_workstation_historical_packet_in_normal_and_optimized_python(self):
        def unavailable_original(package):
            path = package / 'dapr-authentication-summary.json'
            summary = json.loads(path.read_text())
            summary['historical_packet_path'] = '/unavailable-workstation/original-packet.json'
            path.write_text(json.dumps(summary))
        for optimized in (False, True):
            with self.subTest(optimized=optimized):
                result = self.run_verifier(unavailable_original, optimized=optimized)
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertIn('PASS: retained exact secret-safe historical C1.15 packet hash/runtime/image observations', result.stdout)
                self.assertIn('PASS: canonical and receipt-bound image/platform/config and chart chains', result.stdout)

    def test_envelope_versions_alias_unknown_fields_and_activation_are_rejected_under_optimization(self):
        for field, value, expected in (
            ('schema_version', 2, 'candidate-envelope-version'),
            ('schema_version', True, 'candidate-envelope-version'),
            ('profile_alias', 'PG-ONPREM-3', 'candidate-profile-alias'),
            ('extra', 'unknown', 'candidate-envelope-fields'),
            ('activation_authorized', True, 'candidate-activation-or-production-credit'),
        ):
            with self.subTest(field=field, value=value):
                def mutate(package):
                    path = package / 'candidate-profile.json'
                    proposal = json.loads(path.read_text())
                    proposal[field] = value
                    path.write_text(json.dumps(proposal))
                self.assert_rejected(mutate, expected)

    def test_duplicate_json_properties_in_envelope_and_archived_config_are_rejected(self):
        def duplicate_envelope(package):
            path = package / 'candidate-profile.json'
            path.write_text('{"schema_version":2,' + path.read_text()[1:])
        self.assert_rejected(duplicate_envelope, 'duplicate-json-property')
        def duplicate_config(package):
            path = package / 'registry-source-evidence.json'
            archive = json.loads(path.read_text())
            name = 'postgres-config.json'
            raw = base64.b64decode(archive['artifacts'][name]['original_bytes_base64'])
            duplicate = b'{"architecture":"other",' + raw.lstrip()[1:]
            replace_archived_bytes(archive, name, duplicate)
            path.write_text(json.dumps(archive))
        self.assert_rejected(duplicate_config, 'duplicate-json-property')

    def test_self_consistent_alternative_image_chains_cannot_replace_canonical_pins(self):
        for label, index_name, child_name, config_name in (
            ('postgres', 'postgres-tag.json', 'postgres-linux-amd64.json', 'postgres-config.json'),
            ('openbao', 'openbao-tag.json', 'openbao-linux-amd64.json', 'openbao-config.json'),
            ('dapr', 'dapr-1.18.1-index.json', 'dapr-1.18.1-linux-amd64-manifest.json', 'dapr-1.18.1-linux-amd64-config.json'),
        ):
            with self.subTest(image=label):
                def mutate(package):
                    path = package / 'registry-source-evidence.json'
                    archive = json.loads(path.read_text())
                    decode = lambda name: json.loads(base64.b64decode(archive['artifacts'][name]['original_bytes_base64']))
                    config = decode(config_name)
                    config['alternative_config'] = True
                    config_raw = json.dumps(config).encode()
                    child = decode(child_name)
                    child['config'].update(digest='sha256:' + hashlib.sha256(config_raw).hexdigest(), size=len(config_raw))
                    child_raw = json.dumps(child).encode()
                    index = decode(index_name)
                    selected = [d for d in index['manifests'] if d.get('platform') == {'architecture': 'amd64', 'os': 'linux'}]
                    self.assertEqual(1, len(selected))
                    selected[0].update(digest='sha256:' + hashlib.sha256(child_raw).hexdigest(), size=len(child_raw))
                    index_raw = json.dumps(index).encode()
                    self.assertEqual('sha256:' + hashlib.sha256(config_raw).hexdigest(), child['config']['digest'])
                    self.assertEqual('sha256:' + hashlib.sha256(child_raw).hexdigest(), selected[0]['digest'])
                    for name, raw in ((config_name, config_raw), (child_name, child_raw), (index_name, index_raw)):
                        replace_archived_bytes(archive, name, raw)
                    path.write_text(json.dumps(archive))
                self.assert_rejected(mutate, label + ':canonical-index-pin')

    def test_self_consistent_alternative_chart_chain_and_receipt_drift_are_rejected(self):
        def alternative_chart(package):
            path = package / 'registry-source-evidence.json'
            archive = json.loads(path.read_text())
            config = json.loads(base64.b64decode(archive['artifacts']['openbao-chart-config.json']['original_bytes_base64']))
            config['alternative_config'] = True
            config_raw = json.dumps(config).encode()
            chart = json.loads(base64.b64decode(archive['artifacts']['openbao-chart-tag.json']['original_bytes_base64']))
            chart['config'].update(digest='sha256:' + hashlib.sha256(config_raw).hexdigest(), size=len(config_raw))
            replace_archived_bytes(archive, 'openbao-chart-config.json', config_raw)
            replace_archived_bytes(archive, 'openbao-chart-tag.json', json.dumps(chart).encode())
            path.write_text(json.dumps(archive))
        self.assert_rejected(alternative_chart, 'chart:canonical-receipt-pin')
        def receipt_drift(package):
            path = package / 'artifact-records.json'
            records = json.loads(path.read_text())
            records['sources'][0]['linux_amd64_config_sha256'] = 'a' * 64
            path.write_text(json.dumps(records))
        self.assert_rejected(receipt_drift, 'postgres:receipt-chain')

    def test_retained_packet_hash_and_runtime_image_observation_links_are_verified(self):
        def bad_bytes(package):
            path = package / 'historical-c1-15-packet-bytes.json'
            retained = json.loads(path.read_text())
            raw = base64.b64decode(retained['original_bytes_base64']) + b' '
            retained['original_bytes_base64'] = base64.b64encode(raw).decode('ascii')
            retained['size_bytes'] = len(raw)
            path.write_text(json.dumps(retained))
        self.assert_rejected(bad_bytes, 'historical-packet-hash')
        for field, value in (('historical_runtime_version', '1.18.4'),
                             ('historical_sidecar_image_digest', 'sha256:' + 'a' * 64)):
            with self.subTest(field=field):
                def mismatch(package):
                    path = package / 'dapr-authentication-summary.json'
                    summary = json.loads(path.read_text())
                    summary[field] = value
                    path.write_text(json.dumps(summary))
                self.assert_rejected(mismatch, 'historical-packet-runtime-image')


if __name__ == '__main__':
    unittest.main()
