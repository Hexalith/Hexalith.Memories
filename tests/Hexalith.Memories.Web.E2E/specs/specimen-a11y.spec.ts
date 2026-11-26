import { expect, test } from '@playwright/test';

import { scanRouteWithAxe } from '../helpers/a11y.js';
import { writeEvidenceFile } from '../helpers/artifacts.js';
import { loadSpecimenRoutes } from '../helpers/specimen-routes.js';

test('specimen routes pass blocking axe checks with nonzero target nodes', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  expect(routes.filter((route) => route.slug === 'operator-health-matrix')).toHaveLength(1);
  const scans = [];

  for (const route of routes) {
    const scan = await scanRouteWithAxe(page, route);
    scans.push(scan);
    if (route.slug === 'benchmark-result-comparator') {
      const comparator = page.getByTestId('mem-benchmark-comparator');
      const scoredAxes = comparator.getByTestId('mem-benchmark-axis')
        .and(comparator.locator('[data-has-score="true"]'));
      expect(await scoredAxes.count()).toBeGreaterThan(0);
      for (const axis of await scoredAxes.all()) {
        const bar = axis.getByTestId('mem-benchmark-axis-bar');
        await expect(bar).toBeVisible();
        await expect(bar).toHaveAttribute('aria-hidden', 'true');
        await expect(bar).not.toHaveAttribute('aria-label');
        const score = axis.getByTestId('mem-benchmark-axis-score');
        await expect(score).toBeVisible();
        await expect(score).toHaveText(/Score:\s*\d/);
      }
      const semanticAxis = comparator.locator('[data-axis="semantic"]');
      const accessibleAxis = await semanticAxis.ariaSnapshot();
      expect(accessibleAxis).toContain('Axis: semantic · Score: 0.91 · Normalization: cosine');
      expect(accessibleAxis).not.toContain('progressbar');
      expect(scan.violationCount).toBe(0);
      expect(scan.incompleteIds).toEqual([]);
    }
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

test('cockpit exposes loading, degradation and recovery during keyboard interaction', async ({ page }) => {
  await page.goto('/__memories/specimens/evidence-cockpit?transitions=true');
  const cockpit = page.getByTestId('mem-evidence-cockpit');
  const loading = cockpit.getByRole('status').and(cockpit.getByTestId('mem-evidence-loading'));
  await expect(loading).toContainText('Loading');
  await expect(cockpit.getByTestId('mem-source-stack')).toHaveCount(0);

  const advance = page.getByTestId('mem-specimen-advance-cockpit');
  await expect(advance).toHaveText('Complete retrieval');
  await expect(advance).toHaveAttribute('tabindex', '0');
  await page.keyboard.press('Tab');
  await expect(advance).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(loading).toHaveCount(0);
  await expect(cockpit.getByRole('status', { name: 'Evidence health: Complete' })).toBeVisible();
  await expect(cockpit.getByTestId('mem-evidence-result-counts')).toBeVisible();
  await expect(cockpit.getByTestId('mem-evidence-recovery')).toHaveCount(0);

  await expect(advance).toHaveText('Degrade retrieval');
  await expect(advance).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(cockpit.getByRole('status', { name: 'Evidence health: Degraded' })).toBeVisible();
  const recovery = cockpit.getByRole('status').and(cockpit.getByTestId('mem-evidence-recovery'));
  await expect(recovery).toHaveAttribute('aria-live', 'polite');
  const action = recovery.getByTestId('mem-recovery-action-button');
  await expect(action).toHaveText('Inspect backend health');
  await expect(action).toHaveAttribute('tabindex', '0');
  for (let step = 0; step < 10 && !await action.evaluate(button => button === document.activeElement); step++) {
    await page.keyboard.press('Tab');
  }
  await expect(action).toBeFocused();
  await page.keyboard.press('Enter');

  await expect(recovery).toHaveCount(0);
  await expect(cockpit.getByRole('status', { name: 'Evidence health: Complete' })).toBeVisible();
  await expect(cockpit.getByTestId('mem-evidence-result-counts')).toBeVisible();
  await expect(page.getByTestId('mem-specimen-callback-log')).toContainText('recovery:InspectBackendHealth:health');
});
