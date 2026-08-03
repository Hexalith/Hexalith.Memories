# Sprint Change Proposal — Implementation-Readiness Remediation Batch

**Date:** 2026-08-03  
**Mode:** Batch  
**Status:** Approved — implementation-planning handoff active  
**Trigger:** `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-03.md`  
**Change classification:** Major coordinated direct adjustment  
**Current implementation boundary:** **NOT READY for unrestricted implementation**

This proposal corrects the planning corpus after the 2026-08-03 implementation-readiness
assessment. Administrator approved it on 2026-08-03. Approval authorizes the artifact
reconciliations and bounded story-creation work listed below; it does not itself implement source
changes, register a story, change a sprint state, assign an independent reviewer, mutate a runtime,
or mark any prerequisite as passed. Each new story remains subject to normal selection and
prerequisite checks.

## 1. Issue Summary

The readiness assessment identified 28 actionable findings across requirements clarity,
functional coverage, UX/architecture alignment, and epic/story quality, plus three warnings. The
issues are concentrated in planning ownership and traceability rather than a failed product
thesis. No MVP rollback or product-scope reduction is recommended.

The portfolio cannot be treated as an unrestricted numeric implementation queue because:

1. Epic 27 names mandatory C1 prerequisites that are not registered with executable producers.
2. Epic 30's current ordering and dependency statements do not match the capabilities now pinned
   in `Hexalith.Builds`; local qualification is still required before selection.
3. Physical tenant isolation is described more strongly than its owned enforcement, migration,
   and attached negative-evidence work.
4. FR53 is only partially covered by real CLI commands; FR71, contrary to the report, is already
   implemented and tested but absent from the planning registry.
5. Epic 31 has no assigned independent reviewer for its mandatory countersignatures.
6. Epic 17's reusable execution override is circular even though its stories are historical
   `done` work.

### 1.1 Reverification ledger

Verified on 2026-08-03 at worktree HEAD
`6698144d6129731e712354b334d8257cd96ee14e`. The readiness report is a user-owned dirty file and
is not modified by this proposal.

| Assessment claim | Evidence and command | Observation | Verdict |
| :--------------- | :------------------- | :---------- | :------ |
| The assessment contains 28 findings plus three warnings. | Full report review; findings grouped as P1-P7, F1-F2, U1-U6, and E1-E13. | The count and four categories reconcile. | `confirmed` |
| Story 27.3 C0 is open or contradictory. | Story 27.3 checkpoint and phase log compared with `epics.md`. | Independent review reclosed C0 with 8/8 criteria; `epics.md` retains the stale reopened state. C2 remains blocked; C3/C4 are complete. | `corrected` |
| Epic 27 has 24 unregistered C1 owners. | Compare the approved C1.1-C1.25 map with Epic 27 headings and sprint rows. | Twenty-five gates are mapped to 27.7-27.31; only 27.21/C1.15 is registered. Twenty-four remain held. | `confirmed` |
| Required Epic 30 multi-container support is unavailable. | Inspect pinned `references/Hexalith.Builds` revision and run `pwsh -NoProfile -File ./Tools/test-publish-containers.ps1`. | The current workflow exposes `publish-containers`, newline project mappings, exact execution SHA, set identity, and evidence upload. All 62 fixture tests passed. Local Memories qualification and partial-recovery evidence remain outstanding. | `partially corrected` |
| FR53 lacks a complete CLI path. | Inspect `RootCommandFactory.Build` and command tests. | `quickstart`, `search`, `handlers`, `status telemetry`, `consistency`, and `export` are real. `ingest`, `traverse`, `case`, and `explore` are tagged stubs; directory batch ingestion is absent. | `confirmed`, but narrower than reported |
| FR71 export is reserved and unimplemented. | Inspect `8-3-data-export.md`, server/client/CLI export source, and run the focused Server export tests. | Story 8.3 is `done`; export services, REST endpoints, client methods, and CLI commands exist. `10 passed, 0 failed`. Planning and sprint registration are stale. | `corrected` |
| External authentication is deferred to Phase 1.5. | Inspect server authentication/authorization registration and middleware; run focused authentication tests. | JWT authentication and a fallback authenticated-user policy are active. Anonymous exceptions are deliberately narrow. `75 passed, 0 failed`. Identity attribution semantics still need one authoritative contract. | `corrected` for authentication; identity gap confirmed |
| Physical isolation has no complete owner. | Compare Story 24.3 scope with current connection construction and architecture follow-up text. | Story 24.3 is a decision/verifier slice. ACL lifecycle, tenant-scoped connection enforcement, existing-data cutover, and deployment-shaped negative evidence are not registered. | `confirmed` |
| Epic 31 can close predictably. | Inspect Story 31.1/31.2 checkpoint rules and sprint notes. | C4b, C5b, and C7 require an independent countersignature; reviewer and schedule are unassigned. Story 31.2 has a smaller activation subset but Epic 31 cannot close. | `confirmed` blocker |
| Epic 17 is reusable in its declared override order. | Compare the override with Story 17.6 acceptance criteria. | The override puts 17.6 before components that 17.6 consumes. Numeric order is coherent; the override is not. | `confirmed` historical-plan defect |
| The story corpus has uniform BDD blocks. | Count bold `Given`, `When`, and `Then` blocks by story. | Counts are 561/552/561. Stories 12.1-12.6, 17.6, 26.6, 26.7, and 27.3 require editorial normalization if retained as reusable plan text. | `confirmed` |

The focused checks above prove current facts only. They do not qualify an Epic 30 release, verify
physical isolation, pass an Epic 27 C1 gate, or supply Epic 31's independent approval.

## 2. Impact Analysis

### 2.1 Epic impact

| Epic or area | Impact |
| :----------- | :----- |
| Epic 7 | Reopen only the Phase 1.5 CLI-completion track. Preserve Stories 7.1-7.5 as historical `done`; add bounded command slices for the four stub groups, separating single-source from directory-batch ingestion. |
| Epic 8 | Reconcile completed Story 8.3 into the canonical epic and sprint registry as completed non-MVP Phase 2 work. |
| Epic 17 | Remove the circular execution override. Preserve all completed story keys and use numeric dependency order as historical truth. |
| Epic 20 | Add one principal-bound ingestion-provenance story if the canonical identity contract is approved. Do not reopen authentication foundation already proved by current source. |
| Epic 24 | Extend the isolation outcome with qualification, enforcement, migration, and deployment-shaped negative-evidence owners. Story 24.3 remains the narrow completed strategy/verifier record. |
| Epic 27 | Correct C0 status; keep 24 C1 gates held until their producers exist; withdraw the broad Story 27.4 backlog contract and replace it with three monotonic close-out outcomes after 27.7-27.31. |
| Epic 30 | Replace stale “unavailable future capability” text with a pinned-dependency qualification gate. Re-key unfinished stories into monotonic order and retain old keys only as historical aliases. |
| Epic 31 | Keep the epic closure blocked until Administrator assigns an independent reviewer and schedule. Story 31.2 remains conditionally selectable only when its stated activation subset passes. |
| Epics 11, 14, 15, 19, 25 | Preserve completed records; label them non-reusable enabling-work archives. Future technical work must state an operator/developer outcome or live outside the product-epic hierarchy. |
| Other completed oversized or non-monotonic stories | Preserve audit history. Add anti-template and selected-outcome notes; do not retroactively move implementation or renumber completed files. |

### 2.2 Story impact and selection boundary

- No currently `done` implementation story is reopened solely to rewrite history.
- New Stories 7.6-7.10, 20.7, and 24.6-24.9 are proposed planning identities. They are not
  registered by this document.
- Epic 27's already-approved 27.7-27.31 mapping remains authoritative. The 24 missing entries may
  be registered only one gate at a time with a real rerunnable producer.
- Story 27.4 remains unselectable and is proposed for withdrawal as an active backlog key after
  replacement Stories 27.32-27.34 are defined.
- Epic 30 remains unselectable until the current pinned Builds contract passes a Memories-owned
  qualification story. Passing the upstream 62-test fixture is necessary evidence, not local
  acceptance.
- Story 31.2 may be selected only through its existing subset gate. Story 31.1 and Epic 31 cannot
  be marked `done` without independent C4b/C5b/C7 evidence.
- Unrestricted implementation remains prohibited. A Product Owner may select a single bounded
  story only after its own prerequisites and evidence producer are present.

### 2.3 Artifact conflicts

| Artifact | Current conflict | Required reconciliation after approval |
| :------- | :--------------- | :------------------------------------- |
| `prd.md` | Phase ownership, C# version, identity trust, indexing consistency, authentication timing, web NFRs, and telemetry lifecycle are ambiguous or stale. | Add a phase register; select C# 14; define identity and consistency semantics; move authentication boundary to current MVP reality; add NFR32-NFR34. |
| `architecture.md` | Early C# 13 text conflicts with current C# 14 facts; future web and physical-isolation enforcement are under-specified; Epic 30 dependency text is stale. | Reconcile language baseline, add authoritative consistency/auth/identity contracts, add web composition map, define isolation state machine and ownership, and record the pinned Builds qualification boundary. |
| `ux-design-specification.md` | UX requirements lack numbered PRD anchors; web “Phase” labels collide with product phases; isolation and equivalence can be overclaimed. | Map to NFR32/NFR33, rename delivery waves, bind assurance to verified runtime evidence, and phase-gate surface equivalence. |
| `epics.md` | FR71 omits completed Story 8.3; FR53 gaps are unowned; Epic 17 order is circular; Epics 27/30/31 have blocked dependencies; historical defects remain reusable-looking. | Apply the exact story and policy edits in Section 5.4. |
| `sprint-status.yaml` | Story 8.3 has only a reserved override; new ownership is absent; date metadata trails authoritative 2026-08-03 changes. | Register completed 8.3, add only approved and fully defined backlog stories, update order/aliases and `last_updated`. |
| Readiness report | FR71, Story 27.3 C0, Epic 30 capability availability, and story-count assumptions are stale. | Preserve the report as the trigger record. Add a dated correction note or rerun readiness after reconciliation; do not rewrite its historical result silently. |

### 2.4 Technical and operational impact

The planning correction itself changes no source, package, workflow, manifest, cluster, secret,
tenant data, or runtime state. Approved follow-up stories would affect:

- CLI command handlers and tests;
- principal-derived provenance at external ingress;
- Redis ACL provisioning, credential lifecycle, connection routing, and migration;
- release workflow qualification and recovery evidence;
- access-telemetry Production qualification and governance evidence.

Every tenant-scope-sensitive implementation must attach a deployment-shaped cross-tenant negative
test to the same change. A passing identifier-format or unit test is not physical-isolation proof.

## 3. Finding Resolution Matrix

### 3.1 PRD clarity findings

| ID | Finding | Resolution | Disposition |
| :- | :------ | :--------- | :---------- |
| P1 | FRs are generally untagged by phase. | Add a canonical phase register. Treat FR53 as a phased requirement and FR71 as completed early but non-MVP. | `confirmed` |
| P2 | C# 13 conflicts with the repository's C# 14 baseline. | Replace stale .NET 10/C# 13 statements with .NET 10/C# 14; record SDK `10.0.302` as current build identity, not a forever pin. | `confirmed` |
| P3 | Case membership, `ingested_by`, and access telemetry lack authoritative identity semantics. | Define tenant claims as authorization; case membership as tenant-scoped metadata; external provenance as normalized `sub`; trusted adapters as allowlisted `system:*`. | `confirmed` |
| P4 | “Atomic” three-backend indexing conflicts with retry/rollback convergence. | Make EventStore the source of truth and projections retryable/rebuildable; expose pending/partial/failed states; do not claim a distributed transaction. | `confirmed` |
| P5 | External ingress authentication is labeled Phase 1.5 despite an MVP HTTP surface. | Update NFR11 to current MVP enforcement and enumerate the deliberate anonymous infrastructure exceptions. | `corrected` by current source |
| P6 | No numbered web accessibility/usability requirement exists. | Add NFR32 for WCAG 2.2 AA, keyboard/focus, status semantics, assistive-technology coverage, and responsive verification. | `confirmed` |
| P7 | Telemetry retention/TTL/ownership is not numbered. | Add NFR34 with owner, TTL/purge/erasure contract, and time-bounded debt treatment; keep telemetry explicitly non-audit. | `confirmed` |

### 3.2 Functional coverage findings

| ID | Finding | Resolution | Disposition |
| :- | :------ | :--------- | :---------- |
| F1 | FR53 has no complete CLI implementation path. | Preserve real commands and add Stories 7.6-7.10 for single-source ingest, directory batch ingest, traversal, case management, and exploration. | `confirmed`, narrowed |
| F2 | FR71 is a reserved unregistered placeholder. | Register completed Story 8.3 as `done`, Phase 2, completed non-MVP; correct the FR map to full coverage. | `corrected` |

After F2 reconciliation, strict FR story coverage is 73/74 with FR53 partial, not 72/74. It
reaches 74/74 only after the proposed FR53 slices are registered with complete acceptance paths.

### 3.3 UX and architecture alignment findings

| ID | Finding | Resolution | Disposition |
| :- | :------ | :--------- | :---------- |
| U1 | UX requirements are not first-class PRD traceability anchors. | Map UX-DRs to NFR32/NFR33 and owning components/stories. | `confirmed` |
| U2 | Future web composition is under-specified. | Add RCL/shell/rendering/state/localization/security and contract-to-component ownership decisions. | `confirmed` |
| U3 | UI performance lacks measurable budgets. | Add NFR33 and representative evidence-packet/graph fixtures. | `confirmed` |
| U4 | UX “Phase 1/2/3” can be confused with product phases. | Rename to “Web UX Wave 1/2/3” and state activation gates. | `confirmed` |
| U5 | UX may overclaim physical isolation. | Use `target`, `configured`, `verified`, `degraded`, and `unknown`; only runtime evidence may produce `verified`. | `confirmed` |
| U6 | Cross-surface equivalence can imply simultaneous delivery. | Gate equivalence by capability, surface, and active product phase; retain capability alignment rather than literal feature parity. | `confirmed` |

### 3.4 Epic and story quality findings

| ID | Finding | Resolution | Disposition |
| :- | :------ | :--------- | :---------- |
| E1 | Epic 27 depends on unregistered future C1 work and has stale C0 state. | Correct C0, retain one-gate-per-story 27.7-27.31 registration guard, and replace broad 27.4 with 27.32-27.34. | `confirmed`; C0 subclaim corrected |
| E2 | Epic 17's override is circular. | Delete the override and preserve historical numeric order 17.1 through 17.7. Future web work gets a fresh preflight rather than reusing 17.6. | `confirmed` |
| E3 | Epic 30 depends on unavailable external capabilities and later-numbered work. | Qualify the now-present pinned Builds contract locally, then re-key unfinished work into monotonic 30.1-30.6 order. | `partially corrected` |
| E4 | Several epics are technical/process containers rather than user-value epics. | Mark completed Epics 11/14/15/19/25 as non-reusable enabling-work archives; reframe active Epic 30 around recoverable release-set operator value. | `confirmed`, historical |
| E5 | Several stories are oversized or bundled. | Mark completed bundles as anti-templates; split active 27.4 and new isolation/CLI/release work into one-outcome stories. | `confirmed` |
| E6 | Several story dependencies are non-monotonic. | Preserve completed 18.x/23.x keys as audit aliases; remove Epic 17 override; re-key unfinished Epic 30 work. | `confirmed` |
| E7 | Acceptance criteria permit divergent outcomes. | Record the actual selected outcome for completed stories; separate future decision stories from implementation and never count accepted deferral as delivery. | `confirmed` |
| E8 | Physical isolation has no complete owning story. | Add Stories 24.6-24.9 for qualification, enforcement, cutover, and attached evidence. | `confirmed` blocker |
| E9 | Epic 31's independent completion authority is unassigned. | Leave closure blocked; Administrator must name an independent reviewer and schedule. Do not invent an assignee. | `confirmed` blocker |
| E10 | Ten stories have non-uniform BDD formatting. | Editorially normalize reusable current plan text without changing behavior; preserve completed story files as history where appropriate. | `confirmed` |
| E11 | Story 0.0 omits exact `dotnet new aspire` provenance. | Add a dated `unverifiable` note; record current SDK/AppHost identities without manufacturing a historical command. | `confirmed`, historical/unverifiable |
| E12 | Reserved keys, aliases, and overrides make ordering ambiguous. | Add one canonical order/alias registry and prohibit future active dependencies on historical aliases. | `confirmed` |
| E13 | Sprint status metadata predates its newest corrections. | Set `last_updated` to the date of the approved reconciliation. | `confirmed` |

### 3.5 Supporting warnings

| ID | Warning | Treatment |
| :- | :------ | :-------- |
| W1 | Accessibility and responsive rules exist only in UX/epic material. | Resolved through NFR32 and its trace map. |
| W2 | Architecture language/dependency facts are stale. | Reconcile C# 14, FrontComposer SHA `663a88ec647d6ea804dd3f4c900ff2a139488c50`, and current Fluent V5 package fact; identify these as observed dependency facts rather than permanent pins unless policy says otherwise. |
| W3 | Detailed trust/freshness/focus/recovery states remain UX-owned. | Add an architecture component/view-model map and retain UX as the detailed interaction source under NFR32/NFR33. |

## 4. Recommended Approach

Use a **coordinated Direct Adjustment**, classified as **Major** because it changes the PRD,
architecture, UX, epic ownership, and sprint registry and requires Product Manager, Architect,
Product Owner, UX, Security, Platform Operations, and Developer review. It is not a fundamental
product replan: the thesis, MVP outcome, and completed implementation remain intact.

### 4.1 Why this approach

- Source-backed corrections prevent new stories for capabilities already delivered.
- One-outcome story slices restore implementable ownership without weakening security or
  Production gates.
- Historical files remain auditable; only current canonical planning truth is corrected.
- Local qualification replaces an inaccurate assumption about an external dependency while still
  failing closed on unproved recovery behavior.
- The plan can regain bounded story selection before every long-running Production gate finishes,
  while unrestricted queue consumption remains prohibited.

### 4.2 Alternatives evaluated

| Option | Verdict | Reason |
| :----- | :------ | :----- |
| Treat the readiness report literally and create new FR71/export work. | Rejected | It would duplicate completed Story 8.3 behavior and contradict current tests/source. |
| Declare Epic 30 ready because upstream Builds tests pass. | Rejected | Memories workflows still require pinned-contract adoption and local four-image/recovery qualification. |
| Keep Epic 27.4 as one checkpoint-heavy close-out. | Rejected | It combines evidence, operations/publication, and governance closure and depends on unregistered producers. |
| Retroactively split or renumber completed stories. | Rejected | It would damage audit history without changing implemented behavior. |
| Roll back implementation or reduce MVP scope. | Rejected | No verified finding requires either action. |

### 4.3 Effort, risk, and schedule

- **Planning reconciliation:** high, because five canonical planning artifacts and the readiness
  correction trail must agree.
- **New implementation:** high, dominated by physical-isolation migration, Epic 27 Production
  qualification, and release recovery evidence.
- **Risk after reconciliation:** medium for bounded story selection; high for any attempt to claim
  Production, physical-isolation, or Epic 31 completion before the named gates pass.
- **MVP effect:** no scope reduction. Authentication is reclassified to match shipped behavior;
  FR71 stays completed non-MVP; FR53 completion stays Phase 1.5. Physical-isolation assurance stays
  `target` until enforcement evidence passes.

## 5. Detailed Change Proposals

### 5.1 PRD changes

#### PRD-1 — add canonical phase ownership

**Section:** Functional Requirements / Roadmap.

**OLD:** Most FRs have no canonical phase tag; FR53 says “all” capabilities without a phased
command inventory, and FR71 is Phase 2.

**NEW:**

> The canonical FR phase register is: MVP — FR1-FR22, FR24-FR52, FR55-FR57, FR63-FR70,
> FR72-FR74, plus the already-delivered portion of FR53; Phase 1.5 — FR23, FR54, FR58-FR62,
> and the remaining FR53 command slices; Phase 2 — FR71. A capability completed before its
> planned phase is recorded as completed non-MVP and does not silently change MVP acceptance.
>
> FR53 is satisfied per active phase. Current real CLI commands include tenant/config, search,
> quickstart, status telemetry, consistency, export, and handler diagnostics. Phase 1.5 completes
> single-source ingestion, directory-batch ingestion, traversal, case management, and interactive
> exploration. A help entry backed by `NotImplementedCommand` is not coverage.

**Rationale:** prevents broad language from pulling deferred commands into MVP acceptance while
retaining a complete final requirement.

#### PRD-2 — select the current language baseline

**OLD:** `.NET 10 / C# 13`.

**NEW:** `.NET 10 / C# 14`. Record SDK `10.0.302` as the current repository build identity and
defer long-term pin policy to repository configuration.

#### PRD-3 — define identity and provenance trust

**OLD:** tenant identity is sufficient for MVP, while case membership, `ingested_by`, and access
telemetry name users/systems without an authoritative source.

**NEW:**

> Tenant claims authorize access. Case membership is tenant-scoped domain metadata and does not
> grant authorization in the current phase. At authenticated external ingress, access telemetry
> and ingestion provenance bind to the normalized `sub` principal; caller-supplied provenance is
> rejected or ignored. Trusted internal adapters may use only allowlisted `system:*` identities
> through an explicitly authenticated service boundary. Display metadata never overrides the
> authenticated principal.

**Rationale:** closes spoofing ambiguity without pretending case membership is an authorization
system.

#### PRD-4 — define multi-store completion

**OLD:** indexing is “atomic” across EventStore, RediSearch, vector, and graph storage, while FR13
allows convergence after partial failure.

**NEW:**

> EventStore acknowledgement is the durable source-of-truth commit. Search/vector/graph writes are
> idempotent rebuildable projections coordinated by the durable workflow. No distributed
> transaction is claimed. The observable state machine distinguishes pending, projecting,
> indexed, partially failed/retrying, failed/dead-lettered, and repaired. `indexed` is emitted only
> after every required active projection acknowledges the same source version.

#### PRD-5 — reconcile authentication timing

**OLD:** NFR11 places external ingress authentication in Phase 1.5.

**NEW:** NFR11 is an MVP/current invariant for external API and CLI ingress. Anonymous access is
limited to explicitly enumerated infrastructure endpoints such as approved health/diagnostic
routes; the fallback application policy requires an authenticated principal.

#### PRD-6 — add numbered future-web NFRs

**NEW NFR32 — Future Web Accessibility and Usability:**

> When a web capability is activated, it meets WCAG 2.2 AA; supports the complete trust workflow
> by keyboard; provides visible focus; never communicates state by color alone; announces
> recovery/status changes accessibly; and is verified with the UX-defined responsive viewports
> plus NVDA on supported Edge/Chrome configurations.

**NEW NFR33 — Future Web Interaction Performance:**

> On representative Evidence Packet and graph fixtures, the activated web surface targets an
> initial usable trust packet within 2.5 seconds, p95 local interaction response within 200 ms,
> cumulative layout shift no greater than 0.1, and initial route payload no greater than 256 KiB.
> Architecture review may revise these proposed budgets before activation, but must replace them
> with explicit measured values rather than removing the gate.

#### PRD-7 — number telemetry lifecycle debt

**NEW NFR34 — Access Telemetry Lifecycle:**

> Access telemetry has an explicit Platform Operations owner, configured TTL, observable purge
> progress, tenant-erasure mapping, bounded recovery behavior, and a dated accepted-debt decision
> for any unsupported retention profile. It remains infrastructure telemetry, not a tamper-evident
> compliance audit trail. Epic 27 C1 evidence governs Production qualification.

### 5.2 Architecture changes

#### ARCH-1 — reconcile language and consistency decisions

Replace stale C# 13 statements with C# 14. Add the PRD-4 state machine as the authoritative
multi-store contract and identify EventStore as source of truth, with Dapr workflow retry and
compensation for projections. Remove “atomic across three backends” wherever it can be read as a
distributed transaction.

#### ARCH-2 — authenticate and bind principal attribution

Document the existing JWT fallback policy and middleware order. Add a boundary table:

| Boundary | Authorization source | Permitted provenance |
| :------- | :------------------- | :------------------- |
| External REST/CLI | Validated tenant claim plus normalized `sub` | Server-derived `sub` only |
| MCP | Validated MCP signing/auth context mapped to tenant/principal | Server-derived authenticated subject |
| Trusted internal adapter | Authenticated allowlisted service identity | Canonical `system:*` value |
| Case membership | None | Domain/display metadata only |

#### ARCH-3 — define physical-isolation ownership and state

Document the separation between logical tenant keying and physical enforcement. The runtime state
grammar is `target`, `configured`, `verified`, `degraded`, or `unknown`. Only a hash-bound,
deployment-shaped negative-evidence packet may transition a named deployment profile to
`verified`. Add component ownership for ACL provisioning, secret rotation, tenant-scoped
connection construction, migration/cutover, rollback, and verifier evidence.

#### ARCH-4 — make future web architecture buildable

Add a project map with these decisions:

- `Hexalith.Memories.Web` is a non-packable Razor component library composed by the
  FrontComposer shell; it is not a second standalone design system.
- Microsoft Fluent UI Blazor V5 supplies primitives. The current observed package fact is
  `5.0.0-rc.4-26180.1`; compatibility policy controls future movement.
- The specimen host may use Interactive Server for verification but is not the production shell.
- RCL components own parameter/view-model state. The consuming host owns navigation,
  authentication, global state, and render-mode selection.
- Localization uses `IStringLocalizer` and `.resx` resources.
- The host supplies verified tenant/case scope; the RCL displays scope and never authorizes.
- Map every Evidence Packet field and UX component to its contract/view-model owner, including
  freshness, health, omissions, recovery actions, focus, and accessible announcements.
- Record current FrontComposer revision
  `663a88ec647d6ea804dd3f4c900ff2a139488c50` as an observed integration fact.

#### ARCH-5 — replace the stale Epic 30 external-dependency statement

**OLD:** required multi-container mapping, exact workflow SHA, and set identity do not exist in
Hexalith.Builds.

**NEW:**

> The pinned Hexalith.Builds revision exposes the candidate multi-container contract and its
> upstream fixtures pass. Memories must still qualify that exact revision against the four-image
> set, caller syntax, immutable execution SHA, evidence upload, partial publication, recovery, and
> cutover behavior before Epic 30 implementation is selectable. Workflow references must not use
> mutable `@main` for accepted release evidence.

### 5.3 UX changes

#### UX-1 — establish traceability

Map accessibility, focus, state, responsive, and recovery requirements to NFR32; map rendering,
payload, layout-stability, and interaction budgets to NFR33. Keep the detailed UX-DR catalog as
the verification source and add owning story/component columns.

#### UX-2 — remove phase ambiguity

Replace UX component labels `Phase 1`, `Phase 2`, and `Phase 3` with `Web UX Wave 1`, `Web UX
Wave 2`, and `Web UX Wave 3`. Every wave names the product phase and activation story that permits
it.

#### UX-3 — fail closed on assurance

Replace any binary “isolated” presentation with `target`, `configured`, `verified`, `degraded`, or
`unknown`. Tenant IDs, prefixes, or graph names alone can never produce `verified`. The trust strip
must expose the evidence profile/hash or disclose that verification is unavailable.

#### UX-4 — phase-gate surface equivalence

State that Evidence Packet equivalence applies per registered capability and active surface.
CLI/MCP/web may ship in different phases; compatibility tests activate with each surface. Preserve
semantic equivalence, not identical presentation.

### 5.4 Epic and story changes

#### EPIC-1 — reconcile completed Story 8.3 / FR71

**OLD:** Story 8.3 is a reserved Phase 2 placeholder with an activation rule.

**NEW:** Add a compact canonical Story 8.3 historical record linked to
`8-3-data-export.md`, with status `done`, phase `Phase2`, and readiness class
`completed-non-mvp`. State that portable case/tenant export is implemented across server, client,
and CLI; re-import/restore remains separately owned by Epic 26. Remove the “create before
implementation” activation rule.

**Rationale:** aligns planning with delivered behavior without pulling FR71 into MVP.

#### EPIC-2 — complete FR53 through bounded CLI stories

The following are proposed identities. Proof commands are future acceptance intent and must be
made real in the same change that registers each story.

| Story | Single outcome | Owner | Prerequisite | Slice proof intent | Historical-context classification |
| :---- | :------------- | :---- | :----------- | :----------------- | :-------------------------------- |
| 7.6 | Ingest one file or URL through a real CLI handler with structured result/error output. | CLI Developer | Existing ingestion API/client | `dotnet test ...Cli.Tests --filter FullyQualifiedName~IngestSourceCommand` | Existing client/API is `current-narrow-pattern`; stub is an `anti-template`. |
| 7.7 | Submit and observe one directory-batch ingestion with bounded enumeration and partial-failure reporting. | CLI Developer | 7.6 and batch API | `dotnet test ...Cli.Tests --filter FullyQualifiedName~IngestDirectoryCommand` | Batch status API is `current-narrow-pattern`; unrelated consistency batch options are not evidence. |
| 7.8 | Traverse one causal graph from the CLI with gaps/degradation represented in all output modes. | CLI Developer | Existing traversal client/API | `dotnet test ...Cli.Tests --filter FullyQualifiedName~TraverseCommand` | Search graph axis is adjacent history, not traversal proof. |
| 7.9 | Create, list, inspect, and update case membership through one coherent CLI group. | CLI Developer | Existing case client/API | `dotnet test ...Cli.Tests --filter FullyQualifiedName~CaseCommand` | Existing case API is `current-narrow-pattern`; membership remains metadata, not auth. |
| 7.10 | Explore memories/cases interactively with deterministic non-interactive fallback and cancellation. | CLI Developer | 7.8 and 7.9 | `dotnet test ...Cli.Tests --filter FullyQualifiedName~ExploreCommand` | Current stub is an `anti-template`; quickstart interaction is useful history but not scope proof. |

Do not create replacement stories for real quickstart, handler diagnostics, status telemetry,
search, consistency, or export commands.

#### EPIC-3 — bind ingestion provenance to the authenticated principal

**Proposed Story 20.7: Principal-Bound Ingestion Provenance**

- **Single outcome:** external ingestion persists normalized authenticated `sub` as `ingested_by`
  and cannot be spoofed by request data; trusted internal adapters use only allowlisted `system:*`
  identities.
- **Owner:** Security Developer, with Security review.
- **Prerequisite:** approved PRD/architecture identity contract and completed Story 20.2.
- **Slice proof intent:** focused endpoint and integration tests for spoof rejection/ignore,
  normalized-sub attribution, missing-sub rejection, system allowlist, and attached cross-tenant
  negatives.
- **Historical context:** Story 20.2 is `current-narrow-pattern` for authorization/audit identity;
  caller-supplied `IngestedBy` is `current-baseline/problem`, not reusable precedent.

#### EPIC-4 — give physical isolation complete ownership

| Story | Single outcome | Owner | Prerequisite | Slice proof intent | Historical-context classification |
| :---- | :------------- | :---- | :----------- | :----------------- | :-------------------------------- |
| 24.6 | Qualify and select one Redis ACL/credential/routing strategy for the supported deployment profile. | Architect + Security | Story 24.3 | A checked-in capability matrix and executable spike fixture with one accepted or blocked decision. | 24.3 is `current-narrow-pattern` for decision/verifier only. |
| 24.7 | Provision/rotate per-tenant credentials and enforce tenant-scoped connections for every Redis-backed path. | Platform Developer | Accepted 24.6 | Deployment-shaped allow/deny and rotation tests, including attached cross-tenant negatives. | Shared keyed multiplexer use is `current-baseline/problem`, not enforcement proof. |
| 24.8 | Migrate existing tenants/data to enforced connections with resumable cutover and tested rollback. | Platform Operations | 24.7 | Fixture covering mixed state, resume, rollback, and no cross-tenant credential reuse. | No current migration implementation is presumed. |
| 24.9 | Produce hash-bound attached runtime evidence and publish the isolation state for a named profile. | Security + Platform Operations | 24.7 and 24.8 | Disposable deployment negative suite; packet proves allow-own/deny-foreign and binds profile/config/image identities. | Prefix/graph-ID verifier evidence is `insufficient-history` for physical enforcement. |

Until 24.9 passes for a named profile, architecture, UX, and operator output report `target`,
`configured`, `degraded`, or `unknown`, never `verified`.

#### EPIC-5 — correct Epic 27 planning truth

1. Update `epics.md` to state Story 27.3 C0 is independently reaccepted and complete; C3/C4 are
   complete and only C2 remains blocked in that story's current checkpoint contract.
2. Retain the approved one-gate-per-story C1.1-C1.25 map to Stories 27.7-27.31. Only Story
   27.21/C1.15 is currently registered. The other 24 stay held until a literal producer and focused
   fixture exist; proposal text is not producer evidence.
3. Withdraw Story 27.4 as an active backlog contract and retain its key as an `anti-template`
   historical alias.
4. Define these monotonic successors:

| Story | Single outcome | Owner | Prerequisite | Slice proof intent | Historical-context classification |
| :---- | :------------- | :---- | :----------- | :----------------- | :-------------------------------- |
| 27.32 | Verify one production-shaped lifecycle profile after all C0-C4 and C1.1-C1.25 evidence gates pass. | Platform Operations | 27.7-27.31 plus completed C0/C2/C3/C4 | Hash-bound lifecycle evidence verifier for the same named profile. | Broad 27.4 is an `anti-template`; 27.3 deployment lane is `current-narrow-pattern`. |
| 27.33 | Publish the operator, incident, recovery, decommission, and evidence-consumption contract for the verified profile. | Technical Writer + Platform Operations | 27.32 | Docs/runbook link and command verifier with no unresolved placeholder. | Existing 27.4 docs bundle is split history, not reusable scope. |
| 27.34 | Close A41 governance only after immutable evidence and required independent approvals are present. | Product Owner + independent Security reviewer | 27.32 and 27.33 | Ledger/approval verifier bound to exact evidence hashes; cannot synthesize approval. | Accepted debt remains open history until this outcome passes. |

#### EPIC-6 — make Epic 30 monotonic and locally qualifiable

No current Epic 30 story file exists, so unfinished planning keys may be re-keyed without rewriting
implemented history. Preserve old keys as aliases in prior proposal citations.

| New key | Origin | Single outcome | Owner | Prerequisite and proof intent |
| :------ | :----- | :------------- | :---- | :---------------------------- |
| 30.1 | Old 30.2 | Shared CI core invokes module-specific verification lanes at an immutable workflow identity. | Build Developer | Focused caller and exact-SHA fixtures. |
| 30.2 | New | Qualify the pinned Builds multi-container and partial-recovery contract for Memories. | Build Owner + Memories Release Engineer | `Tools/test-publish-containers.ps1` upstream evidence plus Memories four-image dry-run, set-drift, partial-publication, recovery, and upload fixtures. |
| 30.3 | Old 30.1 | Guarded release dispatch adopts the qualified shared core for every caller. | Release Engineer | 30.1 and 30.2; workflow-call fixtures prove no mutable `@main`. |
| 30.4 | Old 30.3 | Publish the canonical four-image set with exact source/workflow/set identity. | Release Engineer | 30.3; registry-fixture evidence for all-or-explicit-partial outcomes. |
| 30.5 | Old 30.4 | Recover an intentionally partial release without identity drift or silent overwrite. | Platform Operations | 30.4; rerunnable partial-recovery fixture and evidence packet. |
| 30.6 | Old 30.5 | Cut over and roll back the release path with parity proven against the prior path. | Release Engineer + Platform Operations | 30.5; cutover/rollback fixture and immutable comparison evidence. |

Epic 30's value statement becomes: “Release maintainers can publish and recover one complete,
identity-bound Memories image set without silent partial success.” Upstream fixture success alone
does not move 30.2 to `done`.

#### EPIC-7 — remove Epic 17's circular reusable sequence

**OLD:** execution override places 17.6, then 17.7, then 17.2-17.5 even though 17.6 consumes
17.2-17.5.

**NEW:** historical dependency order is 17.1 → 17.2 → 17.3 → 17.4 → 17.5 → 17.6 → 17.7.
Delete the circular override. Preserve statuses and implementation files. Any new web initiative
must create a fresh preflight story containing only harness/shell prerequisites, then component
stories, then a separate final conformance/browser gate.

#### EPIC-8 — preserve Epic 31's independent authority

Add an explicit sprint blocker:

> Epic 31 closure is outside implementation readiness until Administrator records an independent
> reviewer and scheduled C4b/C5b/C7 countersignature. The Murat persona evidence already present
> is not independent. Story 31.2 may be selected only if C1/C2/C3/C4a/C5a/C6 satisfy its existing
> activation gate; this does not close Story 31.1 or Epic 31.

No person is assigned by this proposal.

#### EPIC-9 — reconcile historical quality findings without rewriting delivery

- Label Epics 11, 14, 15, 19, and 25 as non-reusable enabling-work archives. Add the same warning
  to historical bundles 1.5, 1.6, 8.5, 15.6, 17.7, 21.9, 25.8, and 26.5.
- Link Story 25.7 to Epic 17's web outcome; future user-facing web work belongs in a product/UX
  epic, but do not move the completed file.
- Preserve completed 18.5/18.6 and 23.1/23.9 keys as historical aliases. Future reopened work gets
  fresh monotonic keys.
- Record selected implementation outcomes: 16.1 used projection-registry cross-check; 22.1 used
  semantic offset pagination; 23.4 retained supported non-URL sources and rejects only legacy or
  unavailable sources; 25.8 selected explicit topology outcomes; 26.3 delivered 20 items and
  accepted eight explicit skips, which remain deferred rather than delivered.
- For Stories 12/14/15/19, distinguish implementation, accepted risk, and deferral in a final
  disposition field. A deferral never proves the named capability.
- Normalize reusable planning BDD for Stories 12.1-12.6, 17.6, 26.6, 26.7, and 27.3 without
  changing behavior or erasing historical AC numbering.
- Add Story 0.0 note: exact historical `dotnet new aspire` command/version is unverifiable. Record
  current SDK `10.0.302` and AppHost SDK `13.4.6`; do not manufacture provenance.

### 5.5 Sprint-status changes

After the artifact edits above are approved and completed:

1. Add `8-3-data-export: done` and classify it as `track: product`, `phase: Phase2`,
   `mvpReadiness: completed-non-mvp`.
2. Add new candidate stories only when each has a complete story definition, owner, prerequisite,
   and executable acceptance producer. Proposed text in this document is not registration.
3. Replace current Epic 30 order/keys with 30.1-30.6 and retain an explicit old→new alias map.
4. Order Epic 27 successors 27.7-27.31, then 27.32, 27.33, and 27.34. Story 27.4 remains a
   withdrawn historical alias.
5. Remove Epic 17's circular override and preserve numeric historical order.
6. Keep Epic 31's independent-review blocker and Story 31.2 subset gate visible.
7. Set `last_updated` to the actual date of the approved canonical mutation.

## 6. Implementation Handoff

### 6.1 Ownership

| Recipient | Responsibility |
| :-------- | :------------- |
| Product Manager | Approve PRD phase, identity, consistency, and NFR32-NFR34 changes; confirm no MVP scope reduction. |
| Architect | Approve language, authentication/identity, consistency, web composition, isolation, and Builds qualification decisions. |
| UX Designer | Apply NFR traceability, wave naming, assurance states, performance fixtures, and phase-gated equivalence. |
| Product Owner | Reconcile epic definitions, story aliases/order, registration gates, and selection boundary. |
| Security | Review principal-bound provenance and physical-isolation acceptance; supply independent evidence only where actually independent. |
| Platform Operations | Own isolation migration/evidence, Epic 27 running-profile evidence, and release recovery/cutover fixtures. |
| Build/Release owners | Qualify the pinned Builds contract and immutable workflow identity before Epic 30 selection. |
| Administrator | Name Epic 31's independent reviewer and schedule, or retain the blocker. |
| Developer | Implement only a separately selected story whose producer and prerequisites exist; attach required negative evidence. |

### 6.2 Ordered handoff

1. Reconcile PRD, architecture, and UX decisions.
2. Reconcile completed historical facts: Story 8.3, Story 27.3 C0, selected AC outcomes, Epic 17
   order, C# version, and starter-provenance note.
3. Update `epics.md` and sprint status atomically so phase, key, alias, status, and execution order
   agree.
4. Rerun implementation readiness. The target for planning readiness is no unresolved ownership,
   undefined predecessor, contradictory status, or overclaimed assurance.
5. Select bounded stories individually. Suggested earliest candidates are documentation-only
   reconciliation, Story 20.7 after identity approval, Story 24.6, Story 30.2 qualification after
   30.1, or an FR53 slice with an existing server/client capability.
6. Keep Epic 27 Production closure and Epic 31 closure fail-closed until their external evidence
   and independent approvals exist.

### 6.3 Success criteria

The correction is complete only when:

- all 28 findings have a canonical resolution or explicit accepted blocker;
- the readiness correction records FR71 as full completed non-MVP coverage and FR53 as the sole
  partial FR until its slices register;
- no active story depends on an undefined, later-numbered, or mutable external producer;
- physical isolation is not shown as `verified` without enforcement, migration, and attached
  negative evidence for the same deployment profile;
- Story 27.3 current status agrees across planning and implementation artifacts;
- every C1 registration has one real producer and one independently demonstrable outcome;
- Epic 30 references an immutable qualified Builds identity and proves partial recovery locally;
- Epic 31 names a genuinely independent reviewer or stays blocked;
- all canonical artifact edits pass `git diff --check` and the narrow relevant validation; and
- a rerun readiness assessment identifies no critical ownership or dependency blocker before the
  boundary is changed to unrestricted implementation.

## 7. Change Navigation Checklist Result

### Section 1 — Trigger and context

- [x] Trigger is specific and source-backed.
- [x] The issue is planning/traceability drift, not failure of the MVP thesis.
- [x] Batch mode selected by Administrator.

### Section 2 — Epic impact

- [x] Current and future epic impact assessed.
- [x] Blocked dependencies and external authorities identified.
- [!] Epics 27, 30, and 31 remain gated after proposal creation.

### Section 3 — Artifact conflict analysis

- [x] PRD, architecture, UX, epics, sprint registry, and readiness report compared.
- [x] Source/implementation evidence used to correct stale planning claims.
- [!] Canonical artifacts still require approved reconciliation.

### Section 4 — Path evaluation

- [x] Direct adjustment, rollback, and MVP review evaluated.
- [x] Coordinated direct adjustment selected.
- [N/A] Rollback is not justified.
- [N/A] MVP scope reduction is not justified.

### Section 5 — Proposal components

- [x] Old→new artifact edits specified.
- [x] Candidate stories have one outcome, owner, prerequisite, proof intent, and historical-context
  classification.
- [x] Selection and fail-closed boundaries are explicit.

### Section 6 — Final review and handoff

- [x] Change classified as Major.
- [x] Cross-functional owners and success criteria identified.
- [x] Administrator completed Continue/Edit review and explicitly approved the proposal.

## 8. Approval State and Workflow Log

**Current state:** Approved. Major-change implementation-planning handoff is active for the
Product Manager and Solution Architect, with the Product Owner, UX Designer, Security, Platform
Operations, Build/Release owners, Technical Writer, and Developer responsibilities defined in
Section 6. Approval does not waive any story-registration, evidence, independent-review, or
selection gate.

- 2026-08-03: Administrator supplied the implementation-readiness result and selected Batch mode.
- 2026-08-03: PRD, architecture, UX, epics, sprint status, implementation evidence, current source,
  repository guidance, and the readiness report were fully reviewed.
- 2026-08-03: Reverification corrected FR71, Story 27.3 C0, authentication timing, and the extent of
  Epic 30's external-capability claim; FR53 was narrowed to its actual stubbed command groups.
- 2026-08-03: Comprehensive proposal drafted for Continue/Edit review.
- 2026-08-03: Administrator selected Continue and explicitly approved the complete proposal.
- 2026-08-03: Proposal finalized as a Major coordinated direct adjustment and routed to Product
  Manager and Solution Architect for canonical artifact reconciliation and cross-functional
  handoff.

The next delivery action is the ordered handoff in Section 6.2. Unrestricted implementation remains
blocked until canonical reconciliation and a successful readiness rerun remove the named ownership
and dependency blockers.
