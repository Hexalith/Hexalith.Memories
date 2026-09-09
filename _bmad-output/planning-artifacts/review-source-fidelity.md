# Source-Fidelity Review — Hexalith.Memories PRD

- **PRD:** `_bmad-output/planning-artifacts/prd.md`
- **Addendum:** `_bmad-output/planning-artifacts/addendum.md`
- **Canonical audit trail:** `_bmad-output/planning-artifacts/.memlog.md`
- **Original product input:** `_bmad-output/planning-artifacts/product-brief-Hexalith.Memories-2026-03-22.md`
- **Approved change inputs:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md`
- **Excluded as product truth:** prior `validation-report*` and `review-*` inputs in PRD frontmatter; they are review history only.

## Verdict

**Not yet source-safe for downstream extraction.** The current PRD/addendum faithfully carry most high-order decisions, including the dual Phase 1/1.5 ship contract, RRF, EventStore durable-commit semantics, principal-bound tenancy/provenance, MIT licensing, and the hardened G1 protocol. However, three high-severity partial applications of approved source decisions can cause incorrect implementation or duplicate planning: the backend access boundary is contradicted in two PRD locations, unowned Phase 1 CLI gaps are described as tracked, and FR71 is misclassified in the delivery register. Four medium and two low fidelity gaps remain.

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 3 |
| Medium | 4 |
| Low | 2 |

## Source handling and precedence

- Later `.memlog.md` decisions were treated as superseding the March brief where explicit: MIT over Apache 2.0 (`.memlog.md:74-75`), capability alignment over literal CLI/MCP parity (`.memlog.md:28`), Phase 1 thesis versus Phase 1.5 launch (`.memlog.md:27,34-35`), weighted RRF over magnitude blending (`.memlog.md:30`), and EventStore commit plus rebuildable projections over atomic triple-write (`.memlog.md:31`). These are not findings.
- Explicitly conscious product-brief drops in `addendum.md:88-100` were treated as set aside, not omissions.
- The rerun proposal is a delta over the remediation proposal only where it says so (`sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md:10-14`); otherwise both approved PRD amendment sets remain applicable.

## Findings

### High

#### SF-H1 — The approved direct-backend access boundary is contradicted by stale DAPR-sidecar claims

**Source decision.** The rerun SCP explicitly replaces the ambiguous server-to-backend wording: DAPR state uses the sidecar state API, while direct Redis/FalkorDB search/graph access uses approved infrastructure clients with Aspire-injected keyed connections and does not treat the sidecar as a generic proxy (`sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md:181-194`).

**Current representation.** The correct contract appears in the Service Communication Model (`prd.md:735-744`, especially `prd.md:740-741`). But two current statements contradict it:

- the licensing table says the DAPR sidecar is the FalkorDB client (`prd.md:612`);
- the deployment topology routes the Memories Server through DAPR to Redis/FalkorDB (`prd.md:724-731`).

**Impact.** This reintroduces the exact ambiguity the approved amendment was meant to remove. It can drive an architect toward the wrong connection model and makes the AGPL boundary rationale depend on a transport claim the PRD elsewhere denies.

**PRD-level remediation.** Rewrite `prd.md:612` so the licensing rationale rests on process/network separation through the approved infrastructure-boundary client, without claiming that DAPR is the client. Split `prd.md:730` into DAPR-state access versus direct Redis/FalkorDB search/graph access, matching `prd.md:740-741`. Keep the detailed client topology architecture-owned.

#### SF-H2 — Phase 1 CLI gaps are called “tracked” even though the same PRD says they have no owning stories

**Source decision.** The remediation SCP says Stories 7.6-7.10 are proposed identities, not registered (`sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md:77-81`), and their proof commands become real only in the registration change (`sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md:416-427`).

**Current representation.** The PRD correctly reports seven Phase 1 command rows as stubs (`prd.md:877-896`) and says the G1 prerequisites, including stub verbs, have no owning story in `epics.md` or `sprint-status.yaml` (`prd.md:202-208`). It then states that all stubbed Phase 1 verbs “are tracked in `epics.md` / `sprint-status.yaml`” (`prd.md:900`).

**Impact.** This is a release-critical ownership contradiction. A planner extracting only the CLI section can assume the work is already registered and omit the story-creation/sprint-selection transaction required for G1/G3.

**PRD-level remediation.** Replace `prd.md:900` with the authoritative state from the Release decision record: the stubs are identified in the PRD but unowned/unregistered as of 2026-09-08, and Jerome must create and sprint-select bounded owners by the recorded decision date. Do not imply that the August proposal itself registered Stories 7.6-7.10; the expanded current Phase 1 surface may require a refreshed slice map.

#### SF-H3 — FR71’s delivery-register note reverses the approved export/restore split

**Source decision.** The remediation SCP states that Story 8.3 delivered portable case/tenant export across server, client, and CLI, while re-import/restore remains separately owned by Epic 26 (`sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md:404-414`).

**Current representation.** The CLI table correctly marks portable JSON export shipped (`prd.md:894`), and FR71 correctly says portable case/tenant export is complete while Epic 26 owns operational backup/restore (`prd.md:1089-1092`). The delivery status register instead calls Story 8.3 “operational export/restore” and says application-facing export stays Phase 2 (`prd.md:987`).

**Impact.** The register is the brownfield status summary most likely to be extracted by planning tools. Its reversed description can cause duplicate FR71 work or falsely credit re-import/restore to Story 8.3—the exact failure the SCP correction sought to prevent.

**PRD-level remediation.** Change the FR71 register note to: “Story 8.3 delivered portable case/tenant export across server, client, and CLI as completed non-MVP Phase 2 work; re-import/restore remains separately owned by Epic 26.” Reconcile the label “Shipped early (non-MVP)” with that sentence and leave `prd.md:1091` as the canonical capability statement.

### Medium

#### SF-M1 — Journey 8 retains a Phase 1 heading after the recorded Phase 2 correction

**Source decision.** The product brief defers the Memory Explorer/Timeline and application UI to later scope (`product-brief-Hexalith.Memories-2026-03-22.md:316-330`). The memlog records “Journey 8 phase tag” among the applied triage changes (`.memlog.md:71`).

**Current representation.** Journey 8 is headed “Phase 1” (`prd.md:440`), even though it requires an application web UI and narrative composition (`prd.md:442-456`). The PRD’s own non-goal makes application-facing REST search UI Phase 2 (`prd.md:101-104`), and the journey summary correctly labels Journey 8 Phase 2 (`prd.md:496-508`).

**Impact.** A heading-level extractor can pull a future web/narrative experience into the thesis MVP despite the rest of the document’s phase split.

**PRD-level remediation.** Retitle Journey 8 to “(Phase 2)” and add the same explicit phase banner used by Journeys 1 and 4. No capability or narrative rewrite is required.

#### SF-M2 — NFR32 omits explicit accessibility acceptance modes from the approved rerun amendment

**Source decision.** The rerun SCP requires NFR32 to name reduced motion, forced colors, zoom/reflow, and responsive access to trust fundamentals, in addition to WCAG 2.2 AA, keyboard/focus, and non-color status (`sprint-change-proposal-2026-08-03-implementation-readiness-rerun.md:226-230`).

**Current representation.** NFR32 carries WCAG 2.2 AA, keyboard, focus, non-color, accessible announcements, responsive viewports, and browser/AT evidence, but does not explicitly require reduced-motion, forced-color, or zoom/reflow behavior; “responsive viewports” also does not preserve the source's requirement that trust fundamentals remain accessible at those viewports (`prd.md:1182-1187`). Neither `addendum.md` nor the memlog records these dimensions as deliberately set aside.

**Impact.** The generic WCAG target does not preserve the source’s concrete acceptance surface for downstream UX stories and browser evidence; those modes are easy to omit from a test matrix when not named.

**PRD-level remediation.** Restore the four explicit dimensions in NFR32 and require the Epic 17 evidence matrix to exercise them. Keep the existing NVDA/Edge/Chrome and responsive wording.

#### SF-M3 — The original tenant-deletion isolation check survives as a capability but not as acceptance evidence

**Source decision.** The product brief’s dedicated zero-leak test matrix includes deletion of tenant A followed by verification that all indexes and graph data are removed (`product-brief-Hexalith.Memories-2026-03-22.md:305-314`).

**Current representation.** Tenant deletion remains a product outcome (`prd.md:535-539`; FR39 at `prd.md:1036-1041`), but NFR8’s principal-driven verification matrix covers search, ingest, traversal, malformed/empty IDs, and graph collisions without a delete-and-verify scenario (`prd.md:1123-1128`). No source disposition explicitly drops this test.

**Impact.** FR39 can be judged present while its security/erasure end state remains unverified. This weakens both the original isolation matrix and the compliance-enablement claim.

**PRD-level remediation.** Add an FR39/NFR8 integration check: delete tenant A, prove all A search/vector/graph resources and committed units are removed or tombstoned per the domain contract, and prove tenant B is unaffected. If deletion verification is owned outside NFR8, name the exact NFR/test owner instead of leaving it implicit.

#### SF-M4 — The addendum still says release dates are unconfirmed after Jerome confirmed them

**Source decision.** Jerome confirmed the Phase 1.5 launch date and then the one-month release-gate rule (`.memlog.md:74-75`). The current PRD reflects 2026-12-01 and 2027-01-01 (`prd.md:202-209`, `prd.md:1197-1200`).

**Current representation.** `addendum.md:39-41` says the dates were assistant-set and remain `[ASSUMPTION]` until Jerome confirms. The same addendum later says they are confirmed (`addendum.md:122-128`).

**Impact.** The addendum does not override the PRD (`addendum.md:3-5`), but it is still a validation input and handoff artifact. The contradiction obscures whether moving the dates requires a new SCP and can confuse release planning.

**PRD-level remediation.** Update the release-rationale paragraph to distinguish confirmed dates from the still-derived 2026-10-31 sprint-selection date. Preserve the rule that moving either confirmed decision date requires sprint change.

### Low

#### SF-L1 — Several future-roadmap phase advances are not traceable to a decision or explicit set-aside

**Source decision.** The brief places embedding versioning/model migration in Phase 3 and Memory Explorer/Timeline, per-unit ACLs/redaction, geo pinning, encryption, compliance evidence, and audit trail in Phase 4 (`product-brief-Hexalith.Memories-2026-03-22.md:316-329`, `product-brief-Hexalith.Memories-2026-03-22.md:347-373`).

**Current representation.** The PRD advances embedding versioning/model migration to Phase 2 and collapses the brief’s Phase 4 enterprise/UI set into Phase 3 (`prd.md:260-277`). The addendum’s deliberate brief-drop table (`addendum.md:88-100`) does not record this phase reshaping, and the memlog has no corresponding decision.

**Impact.** These are undated future horizons, so immediate delivery risk is low, but the current roadmap presents source changes without an audit trail in a change-controlled PRD.

**PRD-level remediation.** Either log the deliberate phase compression with rationale and affected capabilities, or restore the original phase assignments. Prefer stable named outcomes over renumbering if “Phase 4” was intentionally removed.

#### SF-L2 — The deferred journey-density decision has no artifact disposition

**Source decision.** The memlog defers trimming the ten journeys, assigns Jerome, and sets the Phase 1.5 launch decision as the revisit point (`.memlog.md:63`).

**Current representation.** The PRD still contains ten journeys (`prd.md:297-519`), which is consistent with the immediate decision, but neither the PRD Open Questions (`prd.md:1197-1206`) nor the addendum handoff records the owner/revisit condition.

**Impact.** The product contract is unaffected today, but this violates the memlog-to-artifact audit rule: the decision is neither captured nor explicitly set aside outside the hidden run log.

**PRD-level remediation.** Add a short addendum handoff/open-item entry: owner Jerome; revisit at the Phase 1.5 launch decision; no current PRD change required.

## Fidelity strengths retained

- The PRD clearly records the original qualitative differentiators without overclaiming unshipped collaboration: three-axis retrieval, case-scoped memory, causal intelligence, and integrated tenant isolation (`prd.md:43-68`), while the brand/README/UX residue is preserved in `addendum.md:78-100`.
- Approved August PRD amendments for C# 14, principal-bound provenance, EventStore durable commit, NFR11 MVP authentication, phase ownership, split package/host inventory, telemetry lifecycle, and the NFR33 collision are materially represented (`prd.md:572`, `prd.md:681-720`, `prd.md:735-749`, `prd.md:814-839`, `prd.md:967-987`, `prd.md:1123-1130`, `prd.md:1182-1189`, `prd.md:1216`).
- The memlog’s later product decisions—hard G1 protocol, diagnostic-only N=8 result, graph auto-seeding requirement, deterministic kill switch, two release clocks, MIT, and confirmed dates—are traceable in the current PRD (`prd.md:153-211`, `prd.md:601-603`).
