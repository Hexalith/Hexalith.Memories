# Sprint Change Proposal — Implementation Readiness Recovery

**Date:** 2026-10-05  
**Mode:** Batch  
**Status:** Approved by Administrator on 2026-10-05; PRD/addendum and architecture handoff complete; Epics 32–35 approved with 64 approved backlog story definitions with story-local source checks in Epics 33–35; Epic 32 and qualifying G1 work held; G1 human reviewers unassigned  
**Scope:** Major planning correction and architecture handoff. Approval authorizes the listed planning edits and verified successor-story registration; implementation and sprint-tracker refresh remain separate decisions.  
**Predecessors:** [Sprint-readiness proposal](sprint-change-proposal-2026-09-12.md), [architecture-convergence proposal](sprint-change-proposal-2026-09-12-architecture-convergence.md), and [approved C1 prerequisite correction](sprint-change-proposal-2026-10-04-c1-security-prerequisites.md).

## 1. Issue summary

At intake, the current PRD required FR75, NFR37, and G6, while `epics.md` still listed the superseded `architecture.md` and `ux-design-specification.md` as active inputs and lacked their inventory and Story 32–35 definitions. The epic inventory, current UX/NFR/gate coverage overlays, and approved successor epic list are now recorded. Sixty-four narrow approved backlog definitions in Epics 33–35 have story-local source checks; Epic 32 operation slices and qualifying G1 work remain held on the owner-repository inventory and named humans. The architecture spine binds FR75/NFR37/G6 and is now `final` after pass-with-findings reviewer gates. The approved AD-23 grammar is rendered and PRD G6 is aligned with AD-20; implementation evidence remains owed. The 2026-09-12 sprint proposal still has open corpus/reviewer assignments; its gate date has now been chosen in D1 below. Its verification rows V07–V08 describe architecture gaps that were subsequently corrected and must be treated as dated history, not current findings.

The later approved McpCli course correction changes the target CLI/MCP presentation: `Hexalith.McpCli` owns eligible operations, while the Memories CLI/MCP packages are compatibility sources. The September proposed Epic 32 command slices predate that change and cannot be registered verbatim. Separately, the approved C1 work has registered only Story 27.22 as the new C1.16 backlog owner; the other 23 gates remain held. This correction must not turn either held group into apparent sprint progress.

## 2. Impact analysis

Sections 2–8 preserve the pre-approval impact snapshot, proposed edits, and intake observations. Completed corrections and remaining work are tracked in sections 7 and 9 and in the current source artifacts.

| Area | Current observation | Required correction |
| :--- | :--- | :--- |
| Product | PRD covers FR1–FR75, NFR1–NFR37, and G1–G6. Its implemented FR14–FR22 range overlaps its explicit partial FR17 row. PRD G6 still offers a product/architecture phase exception, while ratified AD-20 allows only current rerunnable evidence as gate credit. | Correct the overlapping status range. Resolve the G6/AD-20 policy conflict by an explicit product decision; preserve the no-go release posture. |
| Architecture | Spine frontmatter is `draft`, binds FR75/NFR37/G6, and contains AD-20 through AD-23. The memlog now records the selected portable export custody boundary and later ratifications on tombstone irreversibility, tenant write fencing, non-rollback register lineage, and backup erasure. Pass-six retests found no high/critical design issue in those amendments. Administrator approved exact AD-23 Identifier Grammar V1, now rendered in the spine; final retests are in progress. | Resolve that decision, reconcile upstream PRD G6, rerun lint and reviewer gate on the resulting bytes, then mark the spine `final` only if it passes. |
| Epics and UX | `epics.md` active inputs and requirement maps are stale. The current DESIGN and EXPERIENCE spines and the later McpCli decision are not reflected in its derivation. | Re-derive the current inventory, UX trace, coverage, and narrow successor scope. Preserve completed Epics 0–31 and their evidence. |
| C1 and operational tracks | Story 27.22 is registered; 23 other C1 gate stories remain held. Epic 27 and the approved C1 producer transactions have their own registration gates. | Keep that work separate. Reference its dependencies and evidence status without registering its held gates through this correction. |
| Tracking | No Story 32–35 entry exists. `sprint-status.yaml` is not an implementation-readiness verdict. | Keep the file unchanged until an approved architecture and compliant epics handoff support a later sprint-planning refresh. |

No completed MVP evidence is rolled back. The MVP outcome and hard gates remain; schedule feasibility is a separate decision.

## 3. Human decisions and approval record

| ID | Decision | Current record | Consequence |
| :--- | :--- | :--- | :--- |
| D1 | Expanded G1–G6 gate date | **Retain 2026-12-01**, chosen by Administrator on 2026-10-05. Prerequisite checkpoint remains 2026-10-31; derived Phase 1.5 launch date remains 2027-01-01. Capacity and named-reviewer commitment must be checked by the prerequisite checkpoint; their absence does not relax the gate. | A missed prerequisite makes the retained date unreachable; no verdict means no launch. |
| D2 | Real Phase 1 corpus and two independent human reviewers | Administrator states they are the only project contributor. **Approved:** Administrator owns corpus stewardship and acquisition boundaries as the sole project contributor; recruit two named external humans who did not curate the corpus or implement the evaluated behaviour, then record their independence checks. Their identities and availability remain **OPEN**. | G1 diagnostic work may be prepared; a qualifying G1 gate cannot pass without the required reviewers and corpus. No agent review substitutes for them. |
| D3 | PRD G6 versus ratified AD-20 | **Align to evidence-only**, chosen by Administrator on 2026-10-05. Genuinely phase-inactive surfaces stay outside MVP scope. | PRD/addendum text must be corrected in the approved planning change; risk acceptance gives no G6 credit. |
| D4 | FR71 export after tenant erasure | **Portable custody transfer**, chosen by Administrator on 2026-10-05 and appended to the architecture memlog. Source-held staging is purged; same-population restore checks tombstoned origin; external copies are outside source custody. | Spine was amended and remains `draft` until the reviewer gate passes. |
| D4a | AD-23 issued-identifier grammar | **Approved and rendered in the spine:** opaque, random 20-character lowercase Crockford tenant IDs beginning with a letter; retain canonical 26-character uppercase ULIDs for cases and memory units; reserve lowercase `u` as a component delimiter. Exact ingress bounds and composition rules are below. | Render the named grammar in the spine, prove destination-name bounds, and retain current implementation gaps in the ledger. |
| D5 | Complete planning correction | **Approved by Administrator on 2026-10-05**, including the McpCli re-derivation and the story-creation gates below. | Registration remains subject to every story-local gate. |

The earlier architecture-convergence proposal's D1–D5 are already ratified in the spine frontmatter and memlog. They are not reopened by these new IDs.

## 4. Recommended approach

**Selected: architecture completion, then backlog re-derivation, then sprint planning.** Finish the identifier-grammar decision and review first. Re-derive the existing September Epic 32–35 reservations against the corrected spine, current PRD/UX, McpCli ownership, current code, and the approved C1 boundary. Register only independently demonstrable stories that pass both the Historical Slice Scope Guard and Epic AC Verification policy. Refresh sprint tracking only after that handoff is reviewed.

**Effort:** high planning and architecture effort, with a separate substantial implementation critical path. **Risk:** high while erasure implementation evidence and G1 human assignments are absent. **Timeline:** the retained 2026-10-31 checkpoint is a hard feasibility test, not evidence that the 2026-12-01 gate is achievable. Directly reopening completed broad stories, silently copying the old Epic 32 CLI allocation, and relying on historical `done` status are rejected by the current contracts. A rollback would discard evidence without resolving the new requirements. No requirement reduction is proposed.

## 5. Detailed edit proposals

### PRD

1. **Delivery status register:** old `FR14–FR22` in the “Implemented and product-active” row; new `FR14–FR16, FR18–FR22`. Keep the separate partial FR17 row. This removes an internal status contradiction without changing FR17's desired outcome.
2. **Release record and Open Question 1:** old `[ASSUMPTION]` that the expanded G1–G6 gate retains 2026-12-01; new dated D1 ratification with the retained 2026-10-31 prerequisite checkpoint and 2027-01-01 derived launch date, once the full proposal is approved. Keep no-verdict/no-launch and the one-reset rule.
3. **G6 policy, D3 chosen:** old “current evidence or a phase-specific exception approved by product and architecture”; proposed new rule: every MVP-active requirement and architecture-critical active-foundation gap needs current rerunnable evidence for gate credit. A phase-inactive surface is classified outside MVP scope explicitly and earns no gate credit. A risk acceptance records accountability only. Align the release record and addendum in the same approved change; do not silently weaken a ratified gate.
4. **Exact stale-release-record corrections:** in the current-release posture and Release decision record, replace claims that AD-14 phasing or FR75/NFR37/G6 binding is still unratified with the current spine's ratified status, while saying plainly that its implementation ledger remains unevidenced. In the 2026-10-31 prerequisite row, replace “evidence or a named story/owner or approved exception” with “current evidence or a registered, independently verified owner story for each missing prerequisite”; a story satisfies the *planning checkpoint* only, never G6. Record Story 27.22 as the separately approved C1 owner and the other 23 C1 proposals as held. In the G1–G6 row, remove exception language and retain no-verdict/no-launch. Close Open Question 1 with the 2026-10-05 retained-date decision, keep Open Question 2 open for named reviewers/corpus, and update the matching Assumptions Index entries.

### Addendum

Replace the stale handoff statement at line 11, the date assumptions at lines 44 and 152, the unratified AD-14 claim at lines 87–88, and the FR74/NFR36 spine claim at line 155. The new text cites the draft spine's ratified AD-14/FR75/NFR37/G6 *decisions*, the retained 2026-12-01 gate and 2026-10-31 checkpoint, and the remaining evidence and human-review blockers. It must not call a draft spine final or imply that a ratified decision, owner story, or risk acceptance passes G6.

### Architecture spine and memlog

1. The existing architecture workspace was resumed from `.memlog.md`; D4 and the subsequent reviewer corrections were appended and the spine amended with stable AD IDs. Old and current status: `draft`; proposed final status: `final` only after lint, rubric, adversarial, technology-reality, and security/isolation review finds no critical contradiction and the exact AD-23 grammar is settled.
2. Reconcile the current AD-16/AD-21 export wording with the chosen custody boundary, the FR71 portability requirement, erasure target list, export-index record class and readers, and issuance/restore ordering. Explicitly state what the source platform can and cannot erase after custody transfer.
3. Re-test the published pass-5 and pass-6 findings against current bytes. The later memlog ratifications are decisions, not a substitute for a passing reviewer verdict. Carry any unresolved implementation mismatch in the alignment ledger with owner and rerunnable evidence obligation; do not present owner/date as qualification.
4. Keep AD-20's ratified evidence-only G6 rule and correct upstream product wording in the same approved planning handoff.

**D4a approved contract:** Tenant IDs are issued by the platform from a cryptographic random source and collision-checked; their grammar is `[abcdefghjkmnpqrstvwxyz][0123456789abcdefghjkmnpqrstvwxyz]{19}`. Case and `MemoryUnitId` values remain canonical 26-character uppercase Crockford ULIDs matching `[0-7][0-9A-HJKMNP-TV-Z]{25}`, with no accepted alternate case. Lowercase `u` is absent from every issued identifier class and separates encoded components. Each composite key family has a fixed, versioned family tag without `u` and a stated arity; components outside the issued grammars are length-bounded and either reversibly encoded into lowercase Crockford without `u` or represented by a fixed-width digest with a tenant-keyed canonical-value reservation that rejects a collision before key use. A digest alone does not prove injectivity. A family that enters a 63-character resource-name scheme reserves at most 42 characters for its fixed tag, suffix, and separator around the 20-character tenant ID; longer names use a shorter registered tag rather than truncation. Issuance and every import/read boundary reject reserved platform names/prefixes as AD-23 now states. For CloudEvents, propose an ASCII URI-reference `source` of 1–2048 UTF-8 bytes with valid percent escapes and lowercase scheme/host only, and an opaque visible-ASCII `id` of 1–256 bytes; both are preserved otherwise and hashed from their validated canonical bytes before key composition. The tracked grammar artifact and conformance tests must prove each actual destination naming scheme and every legacy-ID remediation rule. This is the approved architecture decision, not a claim that existing identifiers or keys conform.

### Epics and UX derivation

1. Old `inputDocuments`: PRD plus superseded architecture/UX and older readiness reports. New active inputs: current PRD, addendum, final architecture spine, current DESIGN/EXPERIENCE, and approved change control. Keep older architecture/UX and prior proposals as typed historical or change-control references. Update the current UX spines' source metadata consistently without rewriting historical design evidence.
2. Old inventory and coverage end at FR74 and omit NFR37/G6. New inventory and maps cover FR1–FR75, NFR1–NFR37, G1–G6, L1–L3, relevant ADs, and current UX obligations, distinguishing MVP-active, later-phase, verified, gap, and held work.
3. Reassess proposed Epics 32–35 from the September proposal. In particular, the old Epic 32 text assigns `memories ingest`, `traverse`, `case`, and other commands as though the Memories CLI were the target. New stories must place target operations in the approved McpCli contract/enrollment path, label old CLI behavior as compatibility evidence, and prove identity and semantic parity before retirement. Exact story IDs, count, and acceptance criteria are determined only after owner-repo and current-code verification. The other Epic 33–35 reservations require the same current-source verification; none is registered by this proposal.
4. Preserve Epics 0–31, completed story artifacts, historical evidence, and Story 27.22's separate approved scope. Do not register the other 23 held C1 gates. Every newly authored story needs a `Historical Context Classification`, one independently demonstrable `Slice Proof` (or an approved per-gate checkpoint table), and quoted, rerunnable Epic AC Verification verdicts before it appears in `epics.md` at any status.

### Sprint status

**Old and new in this correction:** no edit. Later sprint planning may add only approved, verified story entries and must resolve Epic 24 tracking drift separately. Tracking completion is not G1–G6 qualification.

## 6. Implementation handoff and success criteria

**Classification:** Major. **Route:** Jerome/Product Owner for D1–D3 and approval; Winston/Architecture for D4, spine reconciliation, and reviewer gate; Murat plus named independent reviewers for corpus protocol and gate evidence; Sally for active CLI accessibility/UX; Amelia and the relevant McpCli owner for owner-repository story decomposition. The C1 producer owners retain their separate approved workflow.

Approved handoff sequence:

1. Record D1–D4a and the corpus steward; start external reviewer recruitment and record names and independence before any qualifying G1 run. Correct PRD/addendum policy wording as decided. If reviewers are unavailable, retain G1 no-go.
2. Run `$bmad-architecture` update to a reviewed final spine with stable AD IDs and a current alignment ledger.
3. Run `$bmad-create-epics-and-stories` from that final baseline. Apply story-scope and claim-verification gates before any registration. Do not edit `sprint-status.yaml` in this handoff.
4. Run sprint-readiness validation over the resulting coverage/evidence matrix, capacity, people, dependencies, Epic 24 drift, and 2026-10-31 checkpoint. Only then use sprint planning to update tracking and select work. Keep release no-go until G1–G6 and relevant L gates actually pass.

Success is a consistent product/architecture rule, a reviewer-gated final spine, current requirement and UX coverage, compliant registered successor stories with named owners, and a separate evidence-backed sprint plan. A story count or green tracker alone is insufficient.

## 7. Change-navigation checklist

| Item | Status | Finding or next action |
| :--- | :--- | :--- |
| 1.1–1.3 Trigger/context/evidence | [x] | Failed readiness assessment, current PRD/epics mismatch, draft spine, held C1 gates, and the 2026-09-27 McpCli decision. |
| 2.1–2.5 Epic impact/sequence | [x] | Preserve completed history; re-derive proposed Epics 32–35 after architecture and current owner-repo checks. |
| 3.1–3.4 Artifact conflicts | [x] | PRD status and G6 conflict, spine reviewer gate, stale epic/UX inputs, McpCli ownership, and later tracking. |
| 4.1–4.4 Path evaluation | [x] | Hybrid architecture-first correction selected; rollback and requirement reduction do not close the gaps. |
| 5.1–5.5 Proposal and handoff | [x] | Sections 1–6 specify changes, risks, owners, and order. |
| 6.1–6.2 Review | [x] | Approved grammar and PRD G6 correction are rendered; security, adversarial, rubric, and technology-reality gates passed with findings; lint has zero findings. |
| 6.3 Approval | [x] | Administrator approved the complete proposal, corpus stewardship, and AD-23 grammar on 2026-10-05. Two external human reviewers remain unnamed, so G1 remains no-go. |
| 6.4 Sprint tracking | [N/A] | Intentionally frozen for this correction. |
| 6.5 Handoff | [~] | PRD/addendum correction, final architecture spine, approved epic list, and 64 Epic 33–35 approved backlog definitions are recorded after Administrator’s collective story approval on 2026-10-05. Epic 32 and qualifying G1/final-verdict slices remain held; sprint tracking is frozen. |

## 8. Intake evidence snapshot before correction

These rows preserve the 2026-10-05 intake findings. They are not current-state claims after the edits in sections 9–10; rerun the commands before using any row for implementation or gate credit.

| Claim | Re-runnable command | Observation, 2026-10-05 | Verdict |
| :--- | :--- | :--- | :--- |
| Epics active inputs are stale | `sed -n '1,20p' _bmad-output/planning-artifacts/epics.md` | Lists `architecture.md` and `ux-design-specification.md` as active inputs. | confirmed |
| No new requirement IDs or successor stories are registered in `epics.md` | `rg -n 'FR75|NFR37|G6|^### Story (32|33|34|35)\\.' _bmad-output/planning-artifacts/epics.md` | No match (exit 1). | confirmed |
| No Story 32–35 tracker rows exist | `rg -n '^  (32|33|34|35)-' _bmad-output/implementation-artifacts/sprint-status.yaml` | No match (exit 1). | confirmed |
| C1 has one newly registered gate owner | `rg -n '^### Story 27\\.22|remaining twenty-three' _bmad-output/planning-artifacts/epics.md` | Story 27.22 exists; the other 23 are described as held. | confirmed |
| Spine has current bindings but remains draft | `rg -n '^status:|FR1-FR75|NFR1-NFR37|G1-G6' _bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` | `status: draft`; all three current bindings present. | confirmed |
| Mechanical spine shape | `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09` | `ok: true`, zero findings. This is not a semantic approval. | confirmed |
| PRD FR17 status conflict | `sed -n '988,1000p' _bmad-output/planning-artifacts/prd.md` | FR17 appears in both the broad implemented range and the explicit partial row. | corrected in proposal; source edit pending approval |

No acceptance criterion or story is authored by this proposal. All existing September story reservations remain provisional and require fresh story-local verification before registration.

## 9. Architecture review position for approval

The pass-six [security](architecture/architecture-memories-2026-09-09/reviews/review-security-pass6-2026-10-05.md), [adversarial](architecture/architecture-memories-2026-09-09/reviews/review-adversarial-pass6-2026-10-05.md), [version](architecture/architecture-memories-2026-09-09/reviews/review-version-pass6-2026-10-05.md), and [rubric](architecture/architecture-memories-2026-09-09/reviews/review-rubric-pass6-2026-10-05.md) reviews and dated retests close the high design and stale-pin findings on the current draft. Lint has zero findings. AD-23 Identifier Grammar V1 is now ratified and rendered in the spine, and PRD G6 is aligned to AD-20. The spine is now `final` after the reviewer gate; this is design handoff status, not implementation or G1–G6 qualification. The ledger's implementation blockers remain open even after the design reviews; no G1–G6 gate has passed.

## 10. Backlog handoff status after owner-repository verification

The approved 2026-10-05 correction now has a current FR1–FR75, NFR1–NFR37, G1–G6, L1–L3, AD, and UX-DR1–UX-DR47 inventory and coverage overlay in `epics.md`. Its historical Epics 0–31 and separate Story 27.22/C1 boundary remain intact. The new entries are approved planning backlog definitions only; `sprint-status.yaml` has not been changed.

| Approved successor | Current planning state | Gate impact |
| :--- | :--- | :--- |
| Epic 32, shared CLI/MCP | Epic outcome approved; no operation story registered. McpCli owner-repository Stories 4.4 and 4.5 are still `backlog`, so the approved Memories replace/withdraw/defer inventory, Gateway eligibility, per-user identity path, and coverage gate do not yet exist. The current generic `hexalith` verbs cannot be treated as the old `memories` command tree. | G3 and NFR37 target-surface qualification remain blocked; old Memories CLI/MCP output is compatibility evidence only. |
| Epic 33, scoped hybrid | Stories 33.1–33.8 approved in `epics.md` as narrow backlog planning units; label-freeze and qualifying G1-run slices are held until two named independent human reviewers pass the independence check. | G1/G4/G5 evidence remains absent; a synthetic diagnostic run cannot pass G1. |
| Epic 34, durable isolated memory | Stories 34.1–34.40 approved in `epics.md` as narrow backlog planning units with source checks. C1’s 23 held gates were not imported. | G2 and active-foundation G6 remain unqualified. |
| Epic 35, release evidence | Stories 35.1–35.16 approved in `epics.md` as separate evidence/profile outcomes; target CLI NFR37 qualification and the final G1–G6 release-verdict umbrella are held. | No release gate is credited by these story definitions or by the G6 matrix story. |

**Verified owner-repository boundary:** `rg -n '4-4-approve-chatbot-and-memories-migration-inventories|4-5-gate-module-coverage-against-the-approved-inventory' references/Hexalith.McpCli/_bmad-output/implementation-artifacts/sprint-status.yaml` returns both `backlog`; `sed -n '68,90p' references/Hexalith.McpCli/src/Hexalith.McpCli/Cli/CliRunner.cs` shows the generic verb tree. This finding changes the old September Epic 32 command assumption and is the concrete reopen trigger for operation-story registration.

**Remaining decisions:** Administrator must secure two independent human G1 reviewers and check their independence, and the McpCli owner must approve the Memories migration inventory and Gateway/identity dispositions. Until then the 2026-10-31 prerequisite checkpoint must treat these as open, and the 2026-12-01 G1–G6 posture remains no-go.

## 11. Story-set approval and final-validation hold (2026-10-05)

Administrator approved the 64 Epic 33–35 story definitions together on 2026-10-05. They are backlog planning units with story-local source checks, not selected sprint rows or implementation evidence. The current inventory contains FR1–FR75, NFR1–NFR37, G1–G6, L1–L3, and UX-DR1–UX-DR47; all 64 successor story numbers are contiguous and have acceptance criteria.

The `bmad-create-epics-and-stories` final validation cannot certify complete current-surface FR coverage or development readiness. Epic 32 has no registered operation story because the McpCli owner inventory and generic coverage gate are still `backlog`; G1 label-freeze and qualifying-run slices lack two named independent humans; Story 35.7 depends on the held target CLI enrollment; target CLI NFR37 qualification and the final G1–G6 verdict remain unregistered. The existing stories remain approved as backlog definitions, while the correction workflow stops before a final-validation pass. The 2026-10-31 prerequisite checkpoint remains open, sprint tracking remains frozen, and no G1–G6 gate receives credit from story approval.
