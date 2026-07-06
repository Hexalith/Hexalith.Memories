import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

import {
  evidenceDirectory,
  sanitizeText,
  validateArtifactText,
  validateEvidenceDirectory,
  writeEvidenceFile,
} from '../helpers/artifacts.js';
import { loadSpecimenRoutes } from '../helpers/specimen-routes.js';

test('redaction validator rejects restricted canaries and accepts sanitized text', async () => {
  expect(() => validateArtifactText('Bearer abc.def.ghi', 'bearer canary')).toThrow();
  expect(() => validateArtifactText('C:\\Users\\Jerome\\secret.txt', 'windows path canary')).toThrow();
  expect(() => validateArtifactText('/home/jerome/file', 'linux path canary')).toThrow();

  const sanitized = sanitizeText('Bearer abc.def.ghi C:\\Users\\Jerome\\secret.txt /home/jerome/file');
  validateArtifactText(sanitized, 'sanitized canary');
  expect(sanitized).toContain('[REDACTED]');
});

test('browser artifacts and manual checklist evidence stay bounded and redacted', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  const cockpit = routes.find((route) => route.slug === 'evidence-cockpit');
  const agentPacket = routes.find((route) => route.slug === 'agent-packet-inspector');
  expect(cockpit).toBeDefined();
  expect(agentPacket).toBeDefined();

  await mkdir(evidenceDirectory, { recursive: true });
  await page.goto(cockpit!.route);
  const screenshotPath = path.join(evidenceDirectory, 'evidence-cockpit.png');
  await page.getByTestId('mem-specimen-route').screenshot({ path: screenshotPath });

  await page.goto(agentPacket!.route);
  const copyText = await page.getByTestId('mem-packet-json').textContent();
  expect(copyText).not.toBeNull();
  validateArtifactText(copyText!, 'agent packet copy payload');

  await writeEvidenceFile('copied-text-summary.json', {
    generatedBy: 'specimen-artifacts.spec.ts',
    route: agentPacket!.route,
    copiedTextPolicy: 'Browser copied-text scan is bounded to the clean AgentPacketInspectorMapper fixture; sensitive-payload sanitization of the trust components is proven by bUnit Epic17SanitizationCanaryTests, not by browser-rendered copy here.',
    copiedTextLength: copyText!.length,
  });

  await writeEvidenceFile('manual-at-checklist.json', {
    generatedBy: 'specimen-artifacts.spec.ts',
    workflow: 'Epic 17 specimen trust-surface keyboard and AT checklist',
    viewport: '360, 768, 1024, 1440 Chromium automated; OS screen reader manual not available in unattended run',
    browser: 'Chromium via Playwright',
    os: process.platform,
    method: 'Checklist-method evidence only; no NVDA/JAWS/VoiceOver process was launched by automation',
    tester: 'BMad dev-auto automation',
    date: new Date().toISOString().slice(0, 10),
    result: 'Automated route/axe/media checks recorded; OS screen-reader dimension remains fail-closed',
    defects: [],
    severity: 'High for unresolved OS screen-reader release claim',
    owner: 'Memories web product owner + QA + accessibility tester',
    waiverState: 'Not waived for product-route release claim',
    releaseDisposition: 'Manual screen-reader pass required before product-route/full AT validation claim',
  });

  await writeEvidenceFile('trace-policy.json', {
    generatedBy: 'specimen-artifacts.spec.ts',
    tracePolicy: 'Playwright trace is retain-on-failure only and bounded under tests/Hexalith.Memories.Web.E2E/test-results',
    redactionPolicy: 'Text artifacts are scanned for bearer tokens, local paths, raw payload markers, provider diagnostics, stack traces, and restricted source details',
  });

  const artifacts = await validateEvidenceDirectory();
  await writeEvidenceFile('artifact-summary.json', {
    generatedBy: 'specimen-artifacts.spec.ts',
    artifacts,
    nonTextArtifacts: [
      {
        path: 'test-results/evidence/evidence-cockpit.png',
        source: 'Evidence Cockpit complete fixture',
        redactionProof: 'The screenshot is bounded to the complete fixture route and text artifacts independently validate no restricted canaries.',
      },
    ],
    redactionScan: 'passed',
  });

  expect(artifacts).toContain('test-results/evidence/evidence-cockpit.png');
});
