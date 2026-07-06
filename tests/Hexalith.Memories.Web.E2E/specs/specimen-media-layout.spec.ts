import { expect, test, type Page } from '@playwright/test';

import { writeEvidenceFile } from '../helpers/artifacts.js';
import { loadSpecimenRoutes, type SpecimenRoute } from '../helpers/specimen-routes.js';

const viewports = [
  { width: 360, height: 800 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

test('specimen routes keep required trust anchors reachable across viewports and media modes', async ({ page }) => {
  const routes = await loadSpecimenRoutes(page);
  const routeEvidence = [];

  for (const route of routes) {
    const viewportEvidence = [];
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.goto(route.route);
      const reachability = await requiredAnchorReachability(page, route);

      expect(reachability.visible, `${route.surface} required anchor should be visible at ${viewport.width}px`).toBe(true);
      expect(reachability.horizontalOnly, `${route.surface} should not require horizontal-only trust access at ${viewport.width}px`).toBe(false);

      viewportEvidence.push({
        viewport,
        ...reachability,
      });
    }

    const forcedColors = await mediaModeEvidence(page, route, '(forced-colors: active)', async () => {
      await page.emulateMedia({ forcedColors: 'active' });
    });
    await page.emulateMedia({ forcedColors: 'none' });

    const reducedMotion = await mediaModeEvidence(page, route, '(prefers-reduced-motion: reduce)', async () => {
      await page.emulateMedia({ reducedMotion: 'reduce' });
    });
    await page.emulateMedia({ reducedMotion: 'no-preference' });

    const reflow = await reflowEvidence(page, route);
    const touchTargets = await touchTargetEvidence(page, route);
    const measurableTouchTargets = touchTargets.filter((target) => target.measurable);
    const unmeasurableTouchTargets = touchTargets.filter((target) => !target.measurable);
    const undersizedTouchTargets = measurableTouchTargets.filter((target) => !target.meets44By44);
    const touchFailClosed = undersizedTouchTargets.length > 0 || unmeasurableTouchTargets.length > 0;

    // Emulation efficacy is asserted from the browser's own media state, not a hardcoded flag,
    // so a media mode that fails to engage is fail-closed instead of silently passing.
    expect(forcedColors.supported, `${route.surface} forced-colors emulation should engage in Chromium`).toBe(true);
    expect(forcedColors.visible, `${route.surface} forced-colors anchor should stay visible`).toBe(true);
    expect(forcedColors.horizontalOnly, `${route.surface} forced-colors anchor should not become horizontal-only`).toBe(false);
    expect(reducedMotion.supported, `${route.surface} reduced-motion emulation should engage in Chromium`).toBe(true);
    expect(reducedMotion.visible, `${route.surface} reduced-motion anchor should stay visible`).toBe(true);
    expect(reducedMotion.horizontalOnly, `${route.surface} reduced-motion anchor should not become horizontal-only`).toBe(false);
    expect(reflow.visible, `${route.surface} reflow anchor should stay visible`).toBe(true);
    expect(reflow.horizontalOnly, `${route.surface} reflow anchor should not require horizontal-only trust access at 320px`).toBe(false);

    routeEvidence.push({
      route: route.route,
      surface: route.surface,
      selectorAnchor: route.selectorAnchor,
      viewportEvidence,
      forcedColors,
      reducedMotion,
      reflow,
      touchTargets: {
        measuredCount: measurableTouchTargets.length,
        unmeasurableCount: unmeasurableTouchTargets.length,
        undersizedCount: undersizedTouchTargets.length,
        failClosed: touchFailClosed,
        disposition: touchFailClosed
          ? 'fail-closed: measured Chromium controls below 44x44 or unmeasurable interactive controls need source-owned remediation or manual release waiver'
          : 'supported Chromium measurements meet 44x44 where controls are present',
        targets: touchTargets,
      },
    });
  }

  await writeEvidenceFile('media-layout-summary.json', {
    generatedBy: 'specimen-media-layout.spec.ts',
    routeCount: routes.length,
    unsupportedDimensions: [
      {
        dimension: 'OS screen-reader browse/forms mode',
        reason: 'Requires installed assistive technology and human tester in the target OS/browser pairing.',
        disposition: 'fail-closed manual checklist required',
      },
      {
        dimension: 'Firefox/WebKit browser matrix',
        reason: 'Story 17.7 automated lane is bounded to Chromium for deterministic CI specimen evidence.',
        disposition: 'fail-closed before product-route release claim',
      },
    ],
    routes: routeEvidence,
  });
});

async function requiredAnchorReachability(page: Page, route: SpecimenRoute) {
  const surfaceRoot = page.getByTestId('mem-specimen-surface-root');
  const target = surfaceRoot.getByTestId(route.selectorAnchor).first();
  await expect(target).toBeAttached();

  // Reset the scroll offset and measure horizontal reachability WITHOUT an auto
  // horizontal scroll, so trust content that is only reachable by scrolling
  // horizontally is caught instead of being pulled into view before measurement.
  await page.evaluate(() => window.scrollTo(0, 0));
  const rect = await target.evaluate((element) => {
    const box = element.getBoundingClientRect();
    return { width: box.width, height: box.height, left: box.left, right: box.right };
  });
  const viewport = page.viewportSize();
  const documentMetrics = await page.evaluate(() => ({
    clientWidth: document.documentElement.clientWidth,
    scrollWidth: document.documentElement.scrollWidth,
  }));
  const visible = rect.width > 0 && rect.height > 0;
  const horizontalOnly = Boolean(viewport && (rect.left < -1 || rect.right > viewport.width + 1));

  return {
    visible,
    horizontalOnly,
    documentOverflowsHorizontally: documentMetrics.scrollWidth > documentMetrics.clientWidth + 1,
    boundingBox: rect,
  };
}

async function mediaModeEvidence(page: Page, route: SpecimenRoute, mediaQuery: string, emulate: () => Promise<void>) {
  await emulate();
  await page.goto(route.route);
  const supported = await page.evaluate((query) => window.matchMedia(query).matches, mediaQuery);
  return {
    supported,
    ...await requiredAnchorReachability(page, route),
  };
}

async function reflowEvidence(page: Page, route: SpecimenRoute) {
  // WCAG 1.4.10 reflow uses a 320 CSS-pixel wide viewport rather than the
  // non-standard CSS `zoom` property, which does not model real reflow.
  await page.setViewportSize({ width: 320, height: 900 });
  await page.goto(route.route);
  return requiredAnchorReachability(page, route);
}

async function touchTargetEvidence(page: Page, route: SpecimenRoute) {
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto(route.route);

  const interactiveSelector = [
    'button',
    'a',
    'input:not([type="hidden"])',
    'select',
    'textarea',
    '[role="button"]',
    '[role="link"]',
    '[role="checkbox"]',
    '[role="switch"]',
    '[role="tab"]',
    '[role="menuitem"]',
    '[role="option"]',
    'fluent-button',
    'fluent-anchor',
    'fluent-switch',
    'fluent-checkbox',
    'fluent-tab',
    'fluent-select',
    'fluent-menu-item',
    'fluent-text-field',
  ]
    .map((part) => `main ${part}`)
    .join(', ');

  // Do not filter out zero-size / unmeasurable interactive controls: they are
  // recorded as a distinct fail-closed category so a collapsed tappable control
  // cannot silently escape the 44x44 accounting.
  return page.locator(interactiveSelector).evaluateAll((elements) =>
    elements.map((element) => {
      const rect = element.getBoundingClientRect();
      const measurable = rect.width > 0 && rect.height > 0;
      return {
        tagName: element.tagName.toLowerCase(),
        text: element.textContent?.trim().slice(0, 48) ?? '',
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        measurable,
        meets44By44: measurable && rect.width >= 44 && rect.height >= 44,
      };
    }));
}
