"""Lifecycle handoff checks use the actual Builds reader, with no live target."""
from copy import deepcopy
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import time
from types import SimpleNamespace
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools'))
sys.path.insert(0, str(ROOT / 'references/Hexalith.Platform/eng/helm-qualification'))
import access_telemetry_campaign_profile as profiles
import campaign_capacity as capacity
import campaign_lifecycle as lifecycle
import test_campaign_lifecycle as platform_test
from verify_access_telemetry_lifecycle import EvidenceValidationError


class CampaignEnvironmentBindingTests(unittest.TestCase):
    def setUp(self):
        self.fixture = platform_test.CampaignLifecycleTests('runTest')
        self.fixture.setUp(); self.addCleanup(self.fixture.doCleanups)
        self.q = self.fixture.runner(); self.fixture.started(self.q)
        self.root = self.fixture.root
        self.descriptor = self.q.descriptor('ready', [{'id': 'StatefulSet/unit/postgres', 'owner': 'Hexalith.Platform'}])
        self.target = dict(apiServer='https://127.0.0.1:6443', apiCaSha256='b' * 64, clusterUid='test-cluster', namespaceUid='test-namespace')
        def ref(path):
            path = Path(path).resolve()
            return dict(path=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        tool = {'tools': {'dotnet': ref(shutil.which('dotnet')), 'buildsCli': ref(platform_test.CLI)}, 'toolFiles': []}
        lifecycle.atomic(self.root / 'tools.json', tool)
        self.profile = {'inputs': {'tool': [ref(self.root / 'tools.json')]}}
        self.readback = dict(schema='hexalith.platform.campaign-readback/v1', runId=self.q.record['runId'], planSha256=self.q.record['planSha256'],
            profileSha256='a' * 64, capacityProfileSha256=capacity.digest(self.profile), target=self.target, observedUtcMs=int(time.time() * 1000), qualified=False)
        lifecycle.atomic(self.root / 'owner-plan.json', {})
        self.env = {'HEXALITH_CAMPAIGN_APPROVED_LIFECYCLE_PLAN_SHA256': self.q.record['planSha256'],
            'HEXALITH_C1_OWNER_PLAN': str(self.root / 'owner-plan.json'), 'HEXALITH_C1_APPROVED_PLAN_SHA256': 'c' * 64}
        self.publish()
        p = patch.object(profiles, 'active_profile_sha256', return_value='a' * 64); p.start(); self.addCleanup(p.stop)
        # Existing authority owns plan validation; isolate this new consumer seam.
        p = patch('access_telemetry_c1_owner_dispatch._validated_plan', return_value=SimpleNamespace(value={'target': self.target})); p.start(); self.addCleanup(p.stop)

    def publish(self):
        lifecycle.atomic(self.root / 'descriptor.json', self.descriptor)
        self.readback['descriptorSha256'] = lifecycle.sha(self.root / 'descriptor.json')
        lifecycle.atomic(self.root / 'readback.json', self.readback)
        for name in ('descriptor', 'readback'):
            path = self.root / (name + '.json')
            self.env['HEXALITH_CAMPAIGN_' + name.upper()] = str(path)
            self.env['HEXALITH_CAMPAIGN_APPROVED_' + name.upper() + '_SHA256'] = lifecycle.sha(path)

    def test_real_supported_reader_and_exact_physical_handoff(self):
        with patch.dict(os.environ, self.env):
            self.assertEqual(self.readback, profiles.validate_campaign_environment_binding(self.profile))
        self.assertFalse(self.readback['qualified'])

    def test_missing_stale_foreign_or_not_ready_handoff_refuses(self):
        original = deepcopy(self.readback)
        for change in ({'observedUtcMs': int(time.time() * 1000) - 61000}, {'runId': 'foreign'},
                       {'target': {**self.target, 'clusterUid': 'foreign'}}, {'qualified': True}):
            self.readback = {**original, **change}; self.publish()
            with patch.dict(os.environ, self.env), self.assertRaises(EvidenceValidationError):
                profiles.validate_campaign_environment_binding(self.profile)
        self.readback = original; self.descriptor['bindings']['environment']['readiness']['status'] = 'pending'; self.publish()
        with patch.dict(os.environ, self.env), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)
        with patch.dict(os.environ, {}, clear=True), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)

    def test_actual_reader_rejects_document_local_binding_despite_matching_file_hash(self):
        self.descriptor['bindings']['run']['environmentId'] = None
        self.publish()
        with patch.dict(os.environ, self.env), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)

    def test_held_immutable_pair_survives_later_monitor_publication(self):
        self.q.record.update(runtimeBinding={'data': {'capacityProfileSha256': capacity.digest(self.profile)}}, physicalTarget=self.target)
        tool = json.loads((self.root / 'tools.json').read_bytes())
        self.q.plan['tools'].update(tool['tools'])
        first = self.q.publish_observation('ready', [{'id': 'StatefulSet/unit/postgres', 'owner': 'Hexalith.Platform', 'readiness': 'ready'}])
        held = dict(self.q.record['latestPair'])
        first_bytes = Path(first['descriptor']['path']).read_bytes()
        self.q.publish_observation('pending', [{'id': 'StatefulSet/unit/postgres', 'owner': 'Hexalith.Platform', 'readiness': 'pending'}])
        self.assertNotEqual(held, self.q.record['latestPair'])
        self.assertEqual(first_bytes, Path(first['descriptor']['path']).read_bytes())
        environment = {**self.env, 'HEXALITH_CAMPAIGN_OBSERVATION': held['path'], 'HEXALITH_CAMPAIGN_APPROVED_OBSERVATION_SHA256': held['sha256']}
        with patch.dict(os.environ, environment):
            accepted = profiles.validate_campaign_environment_binding(self.profile)
        self.assertEqual(first['descriptor']['sha256'], accepted['descriptorSha256'])
        self.assertEqual(self.q.record['runId'], accepted['runId'])
        with patch.dict(os.environ, {**environment, 'HEXALITH_CAMPAIGN_OBSERVATION': self.q.record['latestPair']['path'],
                'HEXALITH_CAMPAIGN_APPROVED_OBSERVATION_SHA256': self.q.record['latestPair']['sha256']}), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)

    def test_changed_descriptor_or_validator_bytes_refuse(self):
        (self.root / 'descriptor.json').write_bytes(b'{}')
        with patch.dict(os.environ, self.env), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)
        self.publish(); (self.root / 'tools.json').write_bytes(b'{}')
        with patch.dict(os.environ, self.env), self.assertRaises(EvidenceValidationError):
            profiles.validate_campaign_environment_binding(self.profile)


if __name__ == '__main__':
    unittest.main()
