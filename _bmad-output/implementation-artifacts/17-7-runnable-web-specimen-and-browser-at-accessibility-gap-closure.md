# Story 17.7 Handoff: Runnable Web Specimen and Browser/AT Gap Closure

Status: done

## Implementation Summary

Story 17.7 added a test-only Memories web specimen lane. It closes the automated non-product Chromium specimen coverage gap and keeps product-route, full WCAG/AT, broad browser, and source-owned touch-target claims fail-closed:

- Shared fixture and route manifest library under `tests/Hexalith.Memories.Web.Specimens`.
- Non-product Blazor host under `tests/Hexalith.Memories.Web.SpecimenHost` serving `/__memories/specimens`.
- Playwright + axe workspace under `tests/Hexalith.Memories.Web.E2E`.
- Existing bUnit fixture classes now delegate to the shared specimen library.
- `Epic17ValidationInventory` now distinguishes browser-specimen evidence from fail-closed product-route, axe-incomplete, horizontal-overflow, touch-target, non-Chromium, touch-device, and screen-reader gaps.

## Evidence

See `tests/test-summary-17-7-browser-at-gap-closure.md`.

## Residual Fail-Closed Items

- Product-route validation.
- Full axe/WCAG clearance for known `aria-prohibited-attr` incomplete findings.
- Non-Chromium browser matrix.
- Source-owned remediation or waiver for data-heavy horizontal overflow.
- Source-owned remediation or waiver for measured under-44px touch targets.
- Manual touch-device target confirmation.
- OS screen-reader pass and live AT focus behavior.
- Existing RCL benchmark happy-state progress-bar axe remediation.
