import { AxeBuilder } from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';

import type { SpecimenRoute } from './specimen-routes.js';

const knownIncompleteRules = new Set(['aria-prohibited-attr']);

export interface AxeEvidence {
  readonly route: string;
  readonly surface: string;
  readonly scannedNodeCount: number;
  readonly violationCount: number;
  readonly blockingViolationCount: number;
  readonly reportOnlyViolationCount: number;
  readonly incompleteCount: number;
  readonly blockingViolationIds: readonly string[];
  readonly reportOnlyViolationIds: readonly string[];
  readonly incompleteIds: readonly string[];
}

export async function scanRouteWithAxe(page: Page, route: SpecimenRoute): Promise<AxeEvidence> {
  await page.goto(route.route);
  const surfaceRoot = page.getByTestId('mem-specimen-surface-root');
  await expect(surfaceRoot).toBeVisible();
  const selectorCount = await surfaceRoot.getByTestId(route.selectorAnchor).count();
  expect(selectorCount, `${route.surface} should render ${route.selectorAnchor}`).toBeGreaterThan(0);

  const result = await new AxeBuilder({ page })
    .include('[data-testid="mem-specimen-surface-root"]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();

  const scannedNodeCount = result.passes
    .concat(result.violations, result.incomplete)
    .reduce((count, check) => count + check.nodes.length, 0);

  expect(scannedNodeCount, `${route.surface} axe scan should include target nodes`).toBeGreaterThan(0);

  const blocking = result.violations.filter((violation) =>
    violation.impact === 'serious' || violation.impact === 'critical' || violation.impact == null);
  const reportOnly = result.violations.filter((violation) =>
    violation.impact === 'minor' || violation.impact === 'moderate');
  const unknownIncomplete = result.incomplete.filter((item) => !knownIncompleteRules.has(item.id));

  expect(blocking, `${route.surface} has blocking or unknown axe violations`).toEqual([]);
  expect(unknownIncomplete, `${route.surface} has untriaged axe incomplete checks`).toEqual([]);
  return {
    route: route.route,
    surface: route.surface,
    scannedNodeCount,
    violationCount: result.violations.length,
    blockingViolationCount: blocking.length,
    reportOnlyViolationCount: reportOnly.length,
    incompleteCount: result.incomplete.length,
    blockingViolationIds: blocking.map((violation) => violation.id),
    reportOnlyViolationIds: reportOnly.map((violation) => violation.id),
    incompleteIds: result.incomplete.map((item) => item.id),
  };
}
