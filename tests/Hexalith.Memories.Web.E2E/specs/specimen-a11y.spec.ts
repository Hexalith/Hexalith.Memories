import { expect, test } from '@playwright/test';

import { scanRouteWithAxe } from '../helpers/a11y.js';
import { writeEvidenceFile } from '../helpers/artifacts.js';
import { loadSpecimenRoutes } from '../helpers/specimen-routes.js';

test('specimen routes pass blocking axe checks with nonzero target nodes', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  expect(routes.filter((route) => route.slug === 'operator-health-matrix')).toHaveLength(1);
  const scans = [];

  for (const route of routes) {
    scans.push(await scanRouteWithAxe(page, route));
    if (route.slug === 'operator-health-matrix') {
      const action = page.getByTestId('mem-health-action-button')
        .filter({ hasText: 'Next action: Use graph traversal' });
      await expect(action).toHaveCount(1);
      await expect(action).toHaveAttribute('disabled');
      await expect(action).toHaveAccessibleName('Next action: Use graph traversal');
      await expect(action).toHaveAccessibleDescription('The action is unavailable for the current scope.');
      await expect(action).not.toHaveAttribute('aria-label');
    }
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
