# Test Summary: Story 17.7 Browser/AT Gap Closure

Date: 2026-07-06

## Environment

- .NET SDK: 10.0.301
- Node.js: v26.4.0
- npm: 11.18.0
- Playwright: 1.61.1
- Browser lane: Chromium project via Playwright
- OS: Linux 6.6.87.2-microsoft-standard-WSL2 x86_64 GNU/Linux

## Route Coverage

- Specimen host route prefix: `/__memories/specimens`
- Route count: 19
- Covered surfaces: Evidence Cockpit, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, Graph Path Summary, Recovery Action Panel, Case Activity Trail, Ingestion Lifecycle Tracker, Operator Health Matrix, Benchmark Result Comparator, Agent Packet Inspector, Evidence Grid, Command Surface, Action Confirmation, Context Navigation, Interaction Form, Filter Summary, Lens Shell.
- Route metadata artifact: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/route-metadata.json`

## Automated Results

- Host build: passed, 0 warnings / 0 errors.
- Web test build: passed, 0 warnings / 0 errors.
- Web xUnit in-process suite: passed.
- E2E install from lockfile: passed.
- TypeScript check: passed.
- Playwright browser lane: passed, 5/5.
- Artifact validator: passed, 9 bounded evidence artifacts scanned (including required transient browser artifacts and this committed summary), plus AC4 required-field schema validation of the manual AT checklist.
- Whitespace check: passed.

## Axe And Layout Evidence

- Axe artifact: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/axe-summary.json`
- Axe result: 19 routes scanned, nonzero scanned nodes on every route, 0 blocking or unknown-impact violations.
- Axe manual-review notes: some routes record known `aria-prohibited-attr` incomplete/needs-review evidence. This remains fail-closed for full axe/WCAG release clearance.
- Media/layout artifact: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/media-layout-summary.json`
- Viewports checked: 360x800, 768x1024, 1024x768, 1440x900.
- Media modes checked: forced-colors active, reduced-motion reduce. Emulation engagement is asserted from the browser's own `matchMedia` state, so a mode that fails to engage is fail-closed rather than a silent pass.
- Reflow check: 320 CSS-pixel viewport (WCAG 1.4.10 reflow) specimen pass; horizontal reachability is measured without auto horizontal-scroll, so horizontal-scroll-only trust access fails.
- Horizontal overflow evidence: required trust anchors remain visible and not horizontal-only; data-heavy route page-level overflow is recorded fail-closed for source-owned responsive remediation.
- Touch-target evidence: a broadened interactive-control selector is measured at 360px; controls below 44 CSS pixels and zero-size/unmeasurable interactive controls are both recorded fail-closed, so source-owned remediation, manual target-device confirmation, or explicit release waiver remains required.

## Artifact Redaction

- Artifact summary: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/artifact-summary.json`
- Copied text summary: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/copied-text-summary.json`
- Screenshot: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/evidence-cockpit.png`
- Trace policy: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/trace-policy.json`
- Redaction scan result: passed. The Playwright helper and the standalone `validate-artifacts.mjs` validator share one restricted-pattern list, scanning text artifacts for authorization secrets (including bare `eyJ…` JWTs), broad Windows/Linux local absolute paths, sensitive payload markers, tenant-sensitive diagnostics, provider diagnostics, JavaScript and .NET stack frames, and restricted source details. The browser copied-text scan is bounded to the clean agent-packet fixture; sensitive-payload sanitization of the trust components is proven by bUnit `Epic17SanitizationCanaryTests`. The only non-text artifact is the bounded complete-fixture cockpit screenshot named in `artifact-summary.json`.

## Manual Checklist / Fail-Closed Gaps

- Manual AT checklist artifact: `tests/Hexalith.Memories.Web.E2E/test-results/evidence/manual-at-checklist.json`
- Method: checklist evidence only; no OS screen reader was launched by automation.
- Result: automated browser evidence recorded; OS screen-reader validation remains fail-closed.
- Owner: Memories web product owner + QA + accessibility tester.
- Waiver state: not waived for product-route/full AT release claim.
- Release disposition: manual screen-reader pass required before release claim.

## Residual Risks

- Product-route validation remains fail-closed because this story adds only a non-product specimen host.
- Non-Chromium browser validation remains fail-closed.
- Manual touch-device 44x44 target confirmation remains fail-closed.
- OS screen-reader validation remains fail-closed.
- Existing RCL benchmark happy-state progress-bar rendering produced an axe issue during implementation. The browser specimen uses the existing empty-state fixture so the route shell is covered without claiming the progress-bar state is browser-cleared.
- Known `aria-prohibited-attr` axe incomplete findings remain fail-closed for full axe/WCAG release clearance.
- Measured under-44px Fluent button/custom-element touch targets remain fail-closed for product-route release clearance.
- Data-heavy route horizontal overflow remains fail-closed for full product-route responsive clearance.
- The Memories web bUnit suite (including the Epic 17 machine-checked inventory guards) currently runs locally / pre-commit but is not yet wired into a CI test lane; see the deferred-work ledger. The browser E2E specimen lane, typecheck, and artifact validator do run in CI.
