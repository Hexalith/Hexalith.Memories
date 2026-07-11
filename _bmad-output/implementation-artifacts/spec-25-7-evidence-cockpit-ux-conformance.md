---
title: 'Story 25.7: Evidence Cockpit UX Conformance'
type: 'refactor'
created: '2026-07-11'
status: 'done'
baseline_revision: '160d8244e71a4d99b1777b2630fe038855d5656b'
final_revision: '211f229695c46c2a785622452500306bea737481'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** The flagship Evidence Cockpit still presents sibling titled regions outside the mandated Fluent accordion pattern, embeds English throughout its evidence components and display helpers, and hand-builds a synthetic unavailable packet that can drift from the canonical mapper.

**Approach:** Compose real multi-section packet states with Fluent UI Blazor V5 accordion and label primitives, route Evidence-area copy through EN/FR resource keys, and add one canonical `EvidencePacketMapper.Unavailable(...)` factory consumed by the cockpit.

## Boundaries & Constraints

**Always:** Preserve scope-before-content ordering; stable `data-testid` and accessible-name contracts; restrictive-state precedence and message intents; unknown/unauthorized fail-closed suppression; source/axis/graph ordering; sensitive-text redaction; recovery callback and live-region behavior; and the existing packet shape for loading/error placeholders. Use the centrally pinned Fluent V5 API (`Header`, not removed V4 members), Fluent 2 tokens, and whole localized strings with placeholders.

**Block If:** Conformance requires a nonexistent Fluent/FrontComposer primitive, changes canonical Evidence Packet meanings or public wire shape, or cannot preserve restrictive-scope and redaction tests. The pinned Fluent package has no description-list primitive, so retain the justified semantic `<dl>` fallback rather than blocking on `FluentDescriptionList`.

**Never:** Add evidence metadata, omitted-detail expansion, command wiring, backend calls, ranking/retrieval changes, new trust semantics, raw interactive controls, legacy tokens, hand-authored theme primitives, or browser/assistive-technology claims owned by other stories.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Complete packet | Authorized packet with result, recovery, sources, axes, and graph | Scope/trust precede one multi-expand Fluent accordion; the primary evidence item is expanded and all headings/copy resolve in the active culture | No error expected |
| Loading or error | No packet plus tenant/case and loading or sanitized error state | The sole primary result stays visible outside an accordion; canonical unavailable packet supplies scope/trust placeholders; detail children stay absent | Unsafe error text is redacted and localized fallback copy is used |
| Restrictive packet | Unknown or unauthorized scope, including populated secret details | Localized restrictive banner and recovery remain, while source/axis/graph details are not rendered or discoverable | Fail closed before detail composition |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs` -- canonical evidence-packet construction boundary.
- `src/Hexalith.Memories.Web/Components/Evidence/` -- cockpit, trust, scope, source, axis, graph, and display-copy surface.
- `src/Hexalith.Memories.Web/Resources/MemoriesWebResources*.resx` -- existing EN/FR RCL resource bundle.
- `tests/Hexalith.Memories.Web.Tests/Components/{Evidence,Validation}/` -- rendered trust, accessibility, Fluent conformance, and governance evidence.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- disposition ledger for `17.1-CR1`, `17.1-CR16`, and `17.1-CR18`.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs` and `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketMapperTests.cs` -- add and pin `Unavailable(...)` with validated tenant scope, optional case scope, unknown isolation, empty result/details, graph unavailable, no recovery, and explicit degraded/error semantics.
- `src/Hexalith.Memories.Web/Components/Evidence/EvidenceResourceKeys.cs`, `EvidenceDisplay.cs`, `EvidencePacketViewMapping.cs`, and `Resources/MemoriesWebResources{,.fr}.resx` -- define stable keys and localized formatting/fallbacks for every visible and assistive Evidence-area string, including enum states, counts, freshness, timestamps, scores, banners, headings, field captions, and unavailable text; maintain EN/FR parity.
- `src/Hexalith.Memories.Web/Components/Evidence/{MemoriesEvidenceCockpit,MemoriesScopeHeader,MemoriesTrustStrip,MemoriesSourceCitationStack,MemoriesRetrievalAxisBreakdown,MemoriesGraphPathSummary}.razor` -- inject the localizer; use Fluent labels for owned typography; render real multi-section states through one `FluentAccordion` with `ExpandMode.Multi` and the primary item expanded; keep single loading/error content visible; consume `EvidencePacketMapper.Unavailable(...)`; retain semantic lists only where the pinned design system has no equivalent.
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` and `tests/Hexalith.Memories.Web.Tests/Components/Validation/{Epic17ConformanceAllowlist,Epic17ConformanceRemediationTests,Epic17AccessibilitySweepTests}.cs` -- prove rendered accordion structure/default expansion, no sole-content collapse, localized EN/FR output and key resolution, current Fluent V5 members, updated allowlist entries, stable selectors/heading outline, and all existing fail-closed/redaction/recovery behaviors.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- mark CR1/CR16 resolved with implementation evidence and CR18 accepted/resolved by verified absence of a pinned Fluent description-list primitive while retaining its explicit semantic allowlist.

**Acceptance Criteria:**
- Given an authorized packet with multiple titled evidence regions, when the cockpit renders, then the user encounters scope and trust first followed by one multi-expand Fluent accordion whose primary evidence item is expanded and whose visible headers come from localized resources.
- Given loading or error is the only primary content, when the cockpit renders, then that content remains visible without an accordion interaction and no source, axis, or graph detail is present.
- Given French culture, when complete, loading, error, degraded, or unauthorized evidence renders, then all cockpit-owned visible and assistive copy resolves from `EvidenceResourceKeys` in French without missing-resource key leakage.
- Given unknown or unauthorized scope containing secret source, axis, and graph values, when the localized cockpit renders, then no restricted value or sensitive diagnostic appears and existing recovery/live-region behavior remains intact.
- Given tenant/case loading or error inputs, when the cockpit builds its effective packet, then it uses `EvidencePacketMapper.Unavailable(...)` and produces the canonical empty, unknown-isolation, graph-unavailable shape with degradation set only for an error.

## Spec Change Log

## Review Triage Log

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 15: (high 1, medium 11, low 3)
- defer: 1: (high 0, medium 1, low 0)
- reject: 3: (high 0, medium 1, low 2)
- addressed_findings:
  - `[medium]` `[patch]` Restored a neutral no-packet idle state so absence of input is not announced as an error.
  - `[medium]` `[patch]` Replaced delimiter-concatenated unavailable-packet cache signatures with ordinal field comparisons.
  - `[high]` `[patch]` Made unauthorized packet state hide source counts even when a producer supplies a contradictory authorized isolation flag.
  - `[medium]` `[patch]` Restored the recovery section for every real packet instead of dropping mapper-derived recovery states.
  - `[medium]` `[patch]` Expanded recovery initially so the accordion conversion preserves visible recovery and live-region behavior.
  - `[medium]` `[patch]` Kept the synthetic unknown tenant identity culture-stable while localizing only its rendered fallback.
  - `[medium]` `[patch]` Preserved round-trip timestamp precision and UTC offsets in localized templates.
  - `[medium]` `[patch]` Replaced invalid `FluentLabel Typo` usage with `FluentText` and valid V5 label parameters, with a source guard.
  - `[medium]` `[patch]` Added fail-closed localized fallback handling for undefined or missing enum resources.
  - `[medium]` `[patch]` Bound accordion expansion state so a parent rerender does not reopen a user-collapsed primary item.
  - `[medium]` `[patch]` Corrected French retrieval-axis and pending-expansion terminology.
  - `[low]` `[patch]` Rendered negative freshness ages as unavailable instead of impossible negative values.
  - `[low]` `[patch]` Made every traceability-table fallback consistently resource-keyed and documented that contract.
  - `[medium]` `[patch]` Added French rendered assertions for source type and leaf detail captions.
  - `[low]` `[patch]` Added localized null, NaN, and infinity score coverage.

### 2026-07-11 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 0, low 3)
- defer: 1: (high 0, medium 1, low 0)
- reject: 4: (high 0, medium 0, low 4)
- addressed_findings:
  - `[low]` `[patch]` Added a bUnit test pinning the localized "unknown tenant"/"locataire inconnu" scope rendering in the idle/empty state, so the two independent `"unknown"` sentinel literals (cockpit sentinel and scope-header special case) can no longer drift and leak an untranslated tenant.
  - `[low]` `[patch]` Added a symmetric recovery-item collapse-persistence test proving `@bind-Expanded="_isRecoveryExpanded"` survives a parent rerender, matching the existing primary-item guard.
  - `[low]` `[patch]` Added EN/FR assertions for the localized positive-age freshness branch (`{state}; age {n} s`), the only freshness path with no direct output assertion.
- deferred: `[medium]` trust strip renders confidence/freshness for unauthorized/unknown-isolation packets while hiding source count — a pre-existing coarse inference surface; appended as a new deferred-work entry (out of scope: intent forbids new trust semantics).
- rejected: `[low]` loading/error/idle title changed from `<h2>` to `<FluentText>` span (section retains its localized accessible name; heading-outline validation intentionally retired; no clean Fluent V5 heading primitive); `[low]` degraded banner not lowercased ("Evidence is Partial…", locale-fragile to "fix"); `[low]` localized freshness age applies culture group separators (cosmetic); `[low]` token-budget slot intent unpinned (styling only; the change already fixed a latent fr comparison bug).

## Design Notes

`FluentAccordion` and `FluentAccordionItem` are present in the pinned `5.0.0-rc.4-26180.1` package with `ExpandMode`, `Header`, and `Expanded`. No `FluentDescriptionList` type exists. For real packets, the accordion owns section headers; for loading/error, rendering the lone result directly avoids hiding the sole primary region. Localization should keep sanitization and data values separate from resource-owned templates so translated copy cannot bypass redaction.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --configuration Release -m:1 /nr:false` then `dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Release/net10.0/Hexalith.Memories.Contracts.Tests.dll -parallel none -noLogo` -- expected: mapper contract suite passes.
- `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --configuration Release -m:1 /nr:false` then `dotnet exec tests/Hexalith.Memories.Web.Tests/bin/Release/net10.0/Hexalith.Memories.Web.Tests.dll -parallel none -noLogo` -- expected: all bUnit, localization, accessibility, and conformance tests pass.
- `dotnet build Hexalith.Memories.slnx --configuration Release -m:1 /nr:false` -- expected: succeeds with zero warnings/errors.
- `git diff --check -- src/Hexalith.Memories.Contracts src/Hexalith.Memories.Web tests/Hexalith.Memories.Contracts.Tests tests/Hexalith.Memories.Web.Tests _bmad-output/implementation-artifacts` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Reworked the Evidence Cockpit into a localized Fluent UI V5 multi-expand accordion, kept sole loading/error/idle content directly visible, centralized unavailable packet construction in `EvidencePacketMapper`, preserved restrictive/recovery/redaction behavior, and closed the scoped Evidence conformance backlog with EN/FR and fail-closed tests.

Files changed:
- `src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs` -- adds the validated canonical unavailable-packet factory.
- `src/Hexalith.Memories.Web/Components/Evidence/EvidenceResourceKeys.cs` -- defines the Evidence localization key surface.
- `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs` -- localizes Evidence formatting with fail-closed enum, freshness, timestamp, and score behavior.
- `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacket{FieldMapping,ViewMapping}.cs` -- records consistent resource-key fallbacks for every rendered field.
- `src/Hexalith.Memories.Web/Components/Evidence/Memories{EvidenceCockpit,ScopeHeader,TrustStrip,SourceCitationStack,RetrievalAxisBreakdown,GraphPathSummary}.razor` -- composes correct V5 accordion/text/label primitives and localized fail-closed evidence states.
- `src/Hexalith.Memories.Web/Resources/MemoriesWebResources{,.fr}.resx` -- adds matching English and French Evidence resources.
- `tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketMapperTests.cs` -- pins unavailable-packet validation and canonical shape.
- `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` -- covers accordion state, localization, idle/loading/error behavior, cache isolation, restrictive scope, and formatting edges.
- `tests/Hexalith.Memories.Web.Tests/Components/Validation/{Epic17AccessibilitySweepTests,Epic17ConformanceAllowlist,Epic17ConformanceRemediationTests}.cs` -- updates the Fluent conformance, allowlist, semantic fallback, and component accessibility gates.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- resolves CR1/CR16, accepts CR18 based on pinned API evidence, and records remaining non-cockpit localization debt.
- `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md` -- captures the implementation contract, review triage, verification, and result.

Review findings breakdown: patched 15 findings (1 high, 11 medium, 3 low), deferred 1 pre-existing medium non-cockpit localization item, and rejected 3 findings (1 medium, 2 low). Follow-up review recommended: true because review changed fail-closed information disclosure, recovery visibility, state persistence, identity handling, localization fallbacks, and multiple verification surfaces.

Verification performed:
- Contracts Release build passed with zero warnings/errors; 575/575 tests passed with zero skipped or not run.
- Web Release build passed with zero warnings/errors; 489/489 tests passed with zero skipped or not run.
- All three I/O matrix rows are covered by passing tests for authorized multi-section, loading/error canonical placeholder, and restrictive fail-closed behavior.
- Full `Hexalith.Memories.slnx` Release build passed with zero warnings/errors.
- Scoped `git diff --check` passed.

Residual risks: Hydrated browser/assistive-technology behavior remains owned by Story 17.7 and was not claimed here. Non-cockpit grid/lens callers still use pre-existing invariant `EvidenceDisplay` overloads and are recorded in deferred work. The graph description list remains an intentional semantic HTML fallback because the pinned Fluent V5 package has no equivalent primitive.

### Follow-up review (2026-07-11)

A fresh independent four-layer review pass (adversarial, edge-case, verification-gap, intent-alignment) ran against the full since-baseline diff. No intent gaps, spec defects, or high/medium correctness, security, redaction, or data findings survived: reviewers independently confirmed the fail-closed source/axis/graph suppression, EN/FR resource parity, the unavailable-packet cache-aliasing fix, and format-argument injection safety are all sound. Eight distinct findings were triaged into 3 patches, 1 defer, and 4 rejects.

Patches (test-only, no production behavior change) — added to `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`:
- `MemoriesEvidenceCockpit_UnavailableState_ShouldLocalizeUnknownTenantScope` — pins the localized unknown-tenant rendering (EN + FR) so the two independent `"unknown"` sentinel literals cannot drift silently.
- `MemoriesEvidenceCockpit_UserCollapsedRecovery_ShouldStayCollapsedAfterParentRerender` — pins recovery-item `@bind-Expanded` persistence, symmetric to the existing primary-item guard.
- `EvidenceDisplay_FreshnessLabel_ShouldLocalizePositiveAgeWithoutTimestamp` — pins the previously unasserted localized positive-age freshness branch.

Deferred (1, new ledger entry): trust strip still surfaces confidence/freshness for unauthorized/unknown-isolation packets while hiding source count — a pre-existing coarse inference surface whose remediation would introduce new trust semantics the intent forbids.

Rejected (4, all low): `<h2>`→`<FluentText>` title in loading/error/idle (accessible name retained; heading-outline validation intentionally retired; no clean Fluent V5 heading primitive); degraded-banner capitalization (cosmetic, locale-fragile fix); localized freshness-age culture grouping (cosmetic); token-budget slot styling unpinned (the change already fixed a latent French comparison bug).

Verification: Web.Tests 492/492 pass (489 prior + 3 new), Contracts.Tests 575/575 pass, full `Hexalith.Memories.slnx` Release build clean (0 warnings/0 errors), scoped `git diff --check` clean.

Follow-up review recommended: false. This pass added only three localized, low-consequence, test-only assertions and changed no production behavior, API, security, or data surface.
