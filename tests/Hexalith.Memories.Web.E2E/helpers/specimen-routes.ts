import { expect, type Page } from '@playwright/test';

export const specimenIndexRoute = '/__memories/specimens';

const expectedSlugs = [
  'action-confirmation',
  'agent-packet-inspector',
  'benchmark-result-comparator',
  'case-activity-trail',
  'command-surface',
  'context-navigation',
  'evidence-cockpit',
  'evidence-grid',
  'filter-summary',
  'graph-path-summary',
  'ingestion-lifecycle-tracker',
  'interaction-form',
  'lens-shell',
  'operator-health-matrix',
  'recovery-action-panel',
  'retrieval-axis-breakdown',
  'scope-header',
  'source-citation-stack',
  'trust-strip',
] as const;

export interface SpecimenRoute {
  readonly surface: string;
  readonly slug: string;
  readonly route: string;
  readonly selectorAnchor: string;
  readonly fixtureFamily: string;
}

export async function loadSpecimenRoutes(page: Page): Promise<SpecimenRoute[]> {
  await page.goto(specimenIndexRoute);
  await expect(page.getByTestId('mem-specimen-index')).toBeVisible();

  const routes = await page.getByTestId('mem-specimen-index-item').evaluateAll((items) =>
    items.map((item) => {
      const anchor = item.querySelector('a');
      return {
        surface: anchor?.textContent?.trim() ?? '',
        slug: item.getAttribute('data-slug') ?? '',
        route: anchor?.getAttribute('href') ?? '',
        selectorAnchor: item.getAttribute('data-selector-anchor') ?? '',
        fixtureFamily: item.getAttribute('data-fixture-family') ?? '',
      };
    }));

  expect(routes.length).toBeGreaterThan(0);
  expect(new Set(routes.map((route) => route.slug)).size).toBe(routes.length);
  expect(routes.map((route) => route.slug).sort()).toEqual([...expectedSlugs]);
  for (const route of routes) {
    expect(route.surface).not.toBe('');
    expect(route.slug).not.toBe('');
    expect(route.route).toMatch(/^\/__memories\/specimens\/[a-z0-9-]+$/);
    expect(route.selectorAnchor).not.toBe('');
    expect(route.fixtureFamily).toMatch(/^Epic17/);
  }

  return routes;
}
