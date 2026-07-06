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

    const forcedColors = await mediaModeEvidence(page, route, async () => {
      await page.emulateMedia({ forcedColors: 'active' });
    });
    await page.emulateMedia({ forcedColors: 'none' });

    const reducedMotion = await mediaModeEvidence(page, route, async () => {
      await page.emulateMedia({ reducedMotion: 'reduce' });
    });
    await page.emulateMedia({ reducedMotion: 'no-preference' });

    const reflow = await reflowEvidence(page, route);
    const touchTargets = await touchTargetEvidence(page, route);
    const undersizedTouchTargets = touchTargets.filter((target) => !target.meets44By44);

    expect(forcedColors.supported, `${route.surface} forced-colors emulation should run in Chromium`).toBe(true);
    expect(forcedColors.visible, `${route.surface} forced-colors anchor should stay visible`).toBe(true);
    expect(forcedColors.horizontalOnly, `${route.surface} forced-colors anchor should not become horizontal-only`).toBe(false);
    expect(reducedMotion.supported, `${route.surface} reduced-motion emulation should run in Chromium`).toBe(true);
    expect(reducedMotion.visible, `${route.surface} reduced-motion anchor should stay visible`).toBe(true);
    expect(reducedMotion.horizontalOnly, `${route.surface} reduced-motion anchor should not become horizontal-only`).toBe(false);
    expect(reflow.visible, `${route.surface} reflow anchor should stay visible`).toBe(true);
    expect(reflow.horizontalOnly, `${route.surface} reflow anchor should not become horizontal-only`).toBe(false);
    expect(touchTargets.every((target) => target.measurable), `${route.surface} touch target evidence should only include measurable controls`).toBe(true);

    routeEvidence.push({
      route: route.route,
      surface: route.surface,
      selectorAnchor: route.selectorAnchor,
      viewportEvidence,
      forcedColors,
      reducedMotion,
      reflow,
      touchTargets: {
        measuredCount: touchTargets.length,
        undersizedCount: undersizedTouchTargets.length,
        failClosed: undersizedTouchTargets.length > 0,
        disposition: undersizedTouchTargets.length > 0
          ? 'fail-closed: measured Chromium controls below 44x44 need source-owned remediation or manual release waiver'
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
  await target.scrollIntoViewIfNeeded();
  const box = await target.boundingBox();
  const viewport = page.viewportSize();
  const documentMetrics = await page.evaluate(() => ({
    clientWidth: document.documentElement.clientWidth,
    scrollWidth: document.documentElement.scrollWidth,
  }));
  const visible = box !== null && box.width > 0 && box.height > 0;
  const horizontalOnly = Boolean(box && viewport && (box.x < -1 || box.x + box.width > viewport.width + 1));

  return {
    visible,
    horizontalOnly,
    documentOverflowsHorizontally: documentMetrics.scrollWidth > documentMetrics.clientWidth + 1,
    boundingBox: box,
  };
}

async function mediaModeEvidence(page: Page, route: SpecimenRoute, emulate: () => Promise<void>) {
  await emulate();
  await page.goto(route.route);
  return {
    supported: true,
    ...await requiredAnchorReachability(page, route),
  };
}

async function reflowEvidence(page: Page, route: SpecimenRoute) {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(route.route);
  await page.addStyleTag({ content: 'html { zoom: 400%; }' });
  return requiredAnchorReachability(page, route);
}

async function touchTargetEvidence(page: Page, route: SpecimenRoute) {
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto(route.route);

  return page.locator('main button, main a, main input, main [role="button"], main fluent-button').evaluateAll((elements) =>
    elements
      .map((element) => {
        const rect = element.getBoundingClientRect();
        return {
          tagName: element.tagName.toLowerCase(),
          text: element.textContent?.trim().slice(0, 48) ?? '',
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          meets44By44: rect.width >= 44 && rect.height >= 44,
          measurable: rect.width > 0 && rect.height > 0,
        };
      })
      .filter((target) => target.measurable));
}
