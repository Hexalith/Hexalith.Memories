import { expect, test } from '@playwright/test';

import { scanRouteWithAxe } from '../helpers/a11y.js';
import { writeEvidenceFile } from '../helpers/artifacts.js';
import { loadSpecimenRoutes } from '../helpers/specimen-routes.js';

test('specimen routes pass blocking axe checks with nonzero target nodes', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  const scans = [];

  for (const route of routes) {
    scans.push(await scanRouteWithAxe(page, route));
  }

  const artifactPath = await writeEvidenceFile('axe-summary.json', {
    generatedBy: 'specimen-a11y.spec.ts',
    routeCount: routes.length,
    scans,
  });

  expect(scans.every((scan) => scan.scannedNodeCount > 0)).toBe(true);
  expect(scans.every((scan) => scan.blockingViolationCount === 0)).toBe(true);
  expect(scans.every((scan) => scan.incompleteIds.every((id) => id === 'aria-prohibited-attr'))).toBe(true);
  expect(artifactPath).toBe('test-results/evidence/axe-summary.json');
});
