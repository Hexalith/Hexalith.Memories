import { expect, test } from '@playwright/test';

import { writeEvidenceFile } from '../helpers/artifacts.js';
import { loadSpecimenRoutes } from '../helpers/specimen-routes.js';

test('all specimen routes render required anchors and metadata', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  const evidence = [];

  for (const route of routes) {
    await page.goto(route.route);
    const root = page.getByTestId('mem-specimen-route');
    await expect(root).toBeVisible();
    await expect(root).toHaveAttribute('data-selector-anchor', route.selectorAnchor);
    await expect(root).toHaveAttribute('data-fixture-family', route.fixtureFamily);

    const requiredSelectorCount = await page.getByTestId(route.selectorAnchor).count();
    expect(requiredSelectorCount, `${route.surface} selector ${route.selectorAnchor}`).toBeGreaterThan(0);

    evidence.push({
      route: route.route,
      slug: route.slug,
      surface: route.surface,
      selectorAnchor: route.selectorAnchor,
      fixtureFamily: route.fixtureFamily,
      requiredSelectorCount,
    });
  }

  const artifactPath = await writeEvidenceFile('route-metadata.json', {
    generatedBy: 'specimen-smoke.spec.ts',
    routeCount: routes.length,
    routes: evidence,
  });

  expect(artifactPath).toBe('test-results/evidence/route-metadata.json');
});
