---
project: Hexalith.Memories
date: 2026-05-27
author: Jerome (with Claude / bmad-correct-course)
trigger: Hexalith.Parties consumer correct-course intake (MEM-1 … MEM-7)
scope_classification: Moderate
review_mode: Batch
input_documents:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
  - _bmad-output/implementation-artifacts/deferred-work.md
artifacts_modified:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
  - _bmad-output/implementation-artifacts/deferred-work.md
artifacts_unchanged:
  - prd.md
  - architecture.md
  - ux-design-specification.md
  - project-context.md
---

# Sprint Change Proposal — 2026-05-27 — Parties Consumer Integration Contract Hardening

## 1. Issue Summary

The `Hexalith.Parties` project ran its own `bmad-correct-course` and surfaced **seven cross-repository asks (MEM-1 … MEM-7)** that can only be resolved on the Memories side, because they are about capabilities the Memories SDK / Server must expose or guarantee for a downstream consumer. Each ask carries a Parties-side follow-up that unblocks once the Memories-side course lands.

The asks cluster into: build/clone stability (MEM-1), deployment configuration (MEM-2), ACL-verifiable route surface (MEM-3), race-safe and stable ingestion (MEM-4), exact source-URI lookup (MEM-5), `MemoryUnitId` stability semantics (MEM-6), and a stable client-mocking seam (MEM-7).

### How discovered

Raised by the Parties project's sprint-change intake while consuming Hexalith.Memories as a submodule SDK. Origins are tagged to the Parties passes that found them (7-7, 9-3, 9-6 chunk A / passes 2/3/5).

### Evidence — current Memories `main` reconciled against each ask

A grounded codebase investigation found that **three asks were partly based on stale assumptions** (the current `main` already satisfies the core of MEM-1, MEM-4, MEM-7). The stories below close only the verified residual gap.

| ID | Parties assumption | Verified current state (file:line) | Residual gap |
|----|----|----|----|
| MEM-1 | `Projects.Hexalith_Memories_Server` missing; `AddHexalithEventStore` redis-param drift | `Projects.Hexalith_Memories_Server` resolves (`AppHost/Program.cs:151`); MCP at `:226`. Wiring is `AddServerEventStoreIntegration(IConfiguration)` → `AddMemoriesEventStoreIntegration(IConfiguration, Action<…>?)` — **no redis param exists**. Drift was a stale submodule pin. | No dedicated compile-resolution guard test (only an integration test touches `Projects.Hexalith_Memories_AppHost`); name-stability not documented. |
| MEM-2 | aspirate emits real OTLP/Dapr config | **No aspirate tooling exists.** OTLP is env-gated in `ServiceDefaults/Extensions.cs:253`; Dapr ports hardcoded in AppHost (3500/50001, 3600/50101), not manifest-emitted. | No published deploy config contract for consumers to fill placeholders. |
| MEM-3 | ACL `/process` operation path needs verifying | **No `/process` endpoint exists.** Real surface: pub/sub `[HttpPost("ingest")]` → `/events/ingest` (`EventIngestionController.cs:56`) + `/api/*`. No OpenAPI. | Route/operation surface not published in an ACL-verifiable form; the Parties `/process` path is wrong. |
| MEM-4 | `IngestAsync` Obsolete; only `sourceUri` dedup → race | It is `[Experimental("HXL001")]` (Story 7.4), **not Obsolete** (`MemoriesClient.cs:404`). Server dedup exists: `dedup:{tenant}:{case}:{SHA256(sourceUri)}` (`DedupKeyBuilder.cs:14`) + `CheckIdempotencyActivity`. | Dedup is check-then-act (TOCTOU race); no client-supplied idempotency token; API still experimental. |
| MEM-5 | needs URI-keyed lookup endpoint | **No keyed lookup**; search is free-text only (no `sourceUri` param). The `dedup:` key already maps sourceUri→MemoryUnitId internally. | Internal mapping not exposed as a public exact lookup. |
| MEM-6 | guarantee stable `MemoryUnitId` | Id = workflow `InstanceId` or new GUID (`IngestionWorkflow.cs:521`); **not** derived from sourceUri. Re-ingest returns existing id **via the dedup record**. Undocumented. | Stability semantics + dedup-record-lifetime dependency not documented or guaranteed. (Parties' "decision D1" ≠ Memories D1 = FalkorDB for MVP.) |
| MEM-7 | provide mockable `IMemoriesClient` | `MemoriesClient` is **not sealed**; methods are `virtual`; **Architecture Decision D9 rejects a client interface**. Mock seam = `HttpClient`/`IHttpClientFactory` boundary. | Supported seam + non-sealed/virtual stability not documented or guaranteed. |

## 2. Impact Analysis

### Project state at time of change

All MVP and operational epics (Epic 0 → Epic 16) are `done` in `sprint-status.yaml`. Only Story 2.7 (`evidence-packet-contract-mapping`) and the non-MVP Epic 17 (future web UI) remain in flight. These seven items are **net-new downstream-consumer integration concerns**, not regressions in existing scope.

### Epic Impact

| Epic | Impact | Action |
|---|---|---|
| **Epic 18 (new)** | Created to hold all seven asks as MEM-tagged stories | Add "Epic 18: Downstream Consumer Integration Contract Hardening" (7 stories) following the Epic 14/15/16 carry-forward hardening pattern |
| Epics 0–17 | No scope change | No edits to existing epic/story bodies |

No existing epic or story is added-to, removed, or renumbered. Epic 18 slots after Epic 17 (the highest existing number) and is placed in the Engineering/Operational Readiness Track, not in active MVP accounting.

### Story Impact (new — all in Epic 18)

| Story | Origin | Summary | Release-timing |
|---|---|---|---|
| 18.1 | MEM-1 | AppHost project-resolution guard test + public-surface stability contract | None |
| 18.2 | MEM-2 | Publish canonical deployment configuration contract (defer full aspirate) | None |
| 18.3 | MEM-3 | Publish invocable route/operation surface for ACL verification; confirm no `/process` | None |
| 18.4 | MEM-4 | Stabilise ingest path + explicit idempotency token + atomic dedup | **Semantic-release sensitive (`feat`)** |
| 18.5 | MEM-5 | Source-URI-keyed memory-unit lookup endpoint | None (additive) |
| 18.6 | MEM-6 | Document/guarantee `MemoryUnitId` stability semantics | None |
| 18.7 | MEM-7 | Reaffirm D9 + document `HttpClient`-boundary mock seam + non-sealed/virtual guarantee | None |

### Artifact Conflicts

- **PRD** — no conflict. 74/74 FR coverage unchanged; no new FR introduced. The seven items reinforce existing FRs (FR6, FR24, FR59–FR62) and NFRs (tenant isolation, idempotent at-least-once handling, deployment/observability configurability). No edits.
- **Architecture** — **referenced, not changed.** MEM-7 explicitly **reaffirms** Decision D9 (concrete client, no interface). No decision is amended. No edits.
- **UX Design** — no conflict. No edits.
- **sprint-status.yaml** — one new epic + seven new stories added as `backlog`. Edited.
- **deferred-work.md** — seven traceability entries (MEM-1 … MEM-7) added as `carried-forward` into the Epic 18 stories, per the Story 14.5 schema. Edited.
- **project-context.md** — no rule change. No edits.

### Technical Impact

- **Code (deferred to Developer agent, not this proposal):** Stories 18.1, 18.4, 18.5, 18.7 carry code/test work; 18.2, 18.3, 18.6 are primarily documentation + drift-guard tests. The only public-contract change with release impact is 18.4 (additive `feat` to `Hexalith.Memories.Client.Rest`).
- **This proposal changes planning documents only.** No code, schema, or infrastructure is modified by the proposal itself.

## 3. Recommended Approach

### Path forward: **Direct Adjustment (Option 1) — add one new hardening epic**

Add **Epic 18** in the Engineering/Operational Readiness Track and record the seven asks as MEM-tagged stories that close only the verified residual gap.

### Path rationale

- The asks are net-new integration concerns from the first external consumer; the established repo pattern for "carry-forward / cross-system hardening that is not MVP product capability" is a dedicated readiness epic (Epics 14, 15, 16). Epic 18 follows that pattern exactly.
- Reopening done Epics 7/8/9/12 to insert stories was rejected: it churns completed-epic accounting and breaks the "done means done" register invariant for no benefit.
- Treating all seven as MVP-blocking was rejected: it contradicts the current PASS readiness gate and the fact that all MVP epics are complete; only MEM-4 has release-timing sensitivity, and that is captured per-story rather than by blocking the whole cut.
- **Rollback** — N/A; nothing to revert. **MVP Review** — N/A; MVP scope is complete and unaffected.

### Decisions locked with maintainer (2026-05-27)

1. **Sequencing:** New **Epic 18, post-MVP fast-follow** (not MVP-blocking; MEM-4 release-timing flagged per-story).
2. **MEM-7 / D9:** **Reaffirm D9** + document the `HttpClient`-boundary mock seam and the non-sealed/virtual stability guarantee. No `IMemoriesClient` interface added.
3. **MEM-2 / deploy:** **Document the deploy config contract now**; defer full aspirate emission to a separate future story.
4. **Mode/deliverable:** **Batch**; write the proposal and apply the `epics.md` / `sprint-status.yaml` / `deferred-work.md` edits in this run. Code work routes to the Developer agent.

### Effort / risk / timeline

- **Effort (planning edits, this run):** Low — one new epic block + register/status updates.
- **Effort (implementation, downstream):** Medium across 7 stories; 18.4 is the largest (concurrency + contract).
- **Risk:** Low for the planning edits (additive, git-reversible). Medium-localised for 18.4 implementation (concurrency correctness + additive public contract).
- **Timeline:** No impact on Story 2.7 or Epic 17. Epic 18 is fast-follow; 18.4 should be sequenced before Parties pins the stabilised SDK.

## 4. Detailed Change Proposals

### Edit 1 — `epics.md`: Epic List entry for Epic 18

**Before:** Epic List ends at the Epic 17 entry.
**After:** Adds an "Epic 18: Downstream Consumer Integration Contract Hardening" entry with lifecycle label and the FRs/NFRs it reinforces, placed after the Epic 17 entry.

### Edit 2 — `epics.md`: Implementation Readiness Boundary scope update

**Before:** "Engineering/Operational Readiness Track: Epics 11-16."
**After:** "Epics 11-16 and Epic 18," with a one-line note that Epic 18 holds the 2026-05-27 Parties consumer integration asks and is not counted toward MVP product readiness.

### Edit 3 — `epics.md`: full Epic 18 section (7 stories)

Adds the Epic 18 detail section at end of file, mirroring the Epic 16 format (lifecycle label, Origin, Preflight required, FRs/NFRs reinforced, release-timing note) with seven `Given/When/Then` stories. Each story header carries its `(MEM-n)` origin and a **Parties-side follow-up** line. Full text in Appendix A.

### Edit 4 — `sprint-status.yaml`: register Epic 18

Adds `epic-18: backlog`, seven `18-x-…: backlog` story keys, and `epic-18-retrospective: optional`.

### Edit 5 — `deferred-work.md`: traceability entries

Adds a "Parties Consumer Integration Intake (2026-05-27)" section with seven entries (IDs `MEM-1` … `MEM-7`), each `Status: carried-forward`, `Source story:` the Parties intake, `Target artifact:` the Epic 18 story, a `Re-open trigger:`, and a `Rationale:`.

## 5. PRD MVP Impact and Action Plan

### MVP impact

**None.** MVP scope (Epic 0 → Epic 8) is complete and unchanged; FR coverage stays 74/74. Epic 18 is Engineering/Operational Readiness Track, explicitly outside MVP accounting.

### Action plan

| # | Action | Owner | Status |
|---|---|---|---|
| 1 | Write this Sprint Change Proposal | Claude / bmad-correct-course | ✅ Done 2026-05-27 |
| 2 | Apply Epic 18 to `epics.md` (Edits 1–3) | Claude / bmad-correct-course | ✅ Done 2026-05-27 |
| 3 | Register Epic 18 in `sprint-status.yaml` (Edit 4) | Claude / bmad-correct-course | ✅ Done 2026-05-27 |
| 4 | Add MEM-1…7 traceability to `deferred-work.md` (Edit 5) | Claude / bmad-correct-course | ✅ Done 2026-05-27 |
| 5 | Implement Stories 18.1–18.7 (code/docs/tests) | Implementation Developer agent | 🔜 Pending sprint selection |
| 6 | Sequence Story 18.4 (`feat`) before Parties pins the stabilised SDK | Implementation Developer + Jerome | 🔜 Pending |
| 7 | Notify Parties when each Memories-side story lands so the paired follow-up unblocks | Jerome (cross-repo) | 🔜 Ongoing |

### Dependencies / sequencing

- 18.5 (keyed lookup) and 18.6 (`MemoryUnitId` stability) are coupled — both lean on the existing dedup record as the authoritative source-URI→id mapping; implement 18.5 with 18.6's lifetime guarantee in view.
- 18.4 is the only release-timing-sensitive story (additive `feat`); land and cut it before Parties pins the SDK.

## 6. Implementation Handoff

### Scope classification: **Moderate**

The proposal itself is planning-document edits (applied this run). Downstream implementation spans seven stories with real code/test work, so the overall change is backlog-affecting (Moderate), not a single-edit Minor change.

### Handoff recipients

| Role | Responsibility | Deliverable |
|---|---|---|
| **Developer agent** (this run) | Write proposal; apply Epic 18 to `epics.md`, `sprint-status.yaml`, `deferred-work.md` | ✅ Done 2026-05-27 |
| **Implementation Developer agent** | Implement Stories 18.1–18.7 honoring the Preflight re-verification and the additive-contract constraint on 18.4 | 🔜 Per sprint selection |
| **Jerome (Maintainer)** | Approve this proposal; sequence 18.4 vs the Parties SDK pin; relay each landed story to Parties | Confirmation + cross-repo coordination |

### Success criteria

- ✅ Epic 18 exists in `epics.md` with seven MEM-tagged stories and Parties-side follow-ups.
- ✅ `sprint-status.yaml` registers Epic 18 (backlog) without touching done epics.
- ✅ `deferred-work.md` carries MEM-1…7 as auditable cross-repo entries.
- ✅ No PRD/architecture/UX change; D9 reaffirmed, not amended; FR coverage unchanged.
- 🔜 On implementation: each story closes its verified residual gap with tests/docs, and each Parties-side follow-up is unblocked.

## 7. Reference

- Trigger: Hexalith.Parties consumer correct-course intake (MEM-1 … MEM-7), 2026-05-27.
- Affected documents: `epics.md`, `sprint-status.yaml`, `deferred-work.md`.
- Architecture: Decision D9 (concrete client, no interface) — reaffirmed by Story 18.7; Decision D1 (FalkorDB for MVP) — noted as unrelated to Parties' "decision D1" in Story 18.6.
- Prior precedent: `sprint-change-proposal-2026-05-19.md` (incremental epics.md edits); Epics 14/15/16 (carry-forward hardening epic pattern).
- Memory: `feedback_review_autonomy.md`, `feedback_scope_override_pattern.md`, `project_release_readiness.md`, `feedback_submodule_init.md`.

**Proposal approved:** ✅ Yes — Jerome, 2026-05-27.
**Author / Executor:** Claude Code (`bmad-correct-course` workflow).

---

## Appendix A — Epic 18 full text (as applied to `epics.md`)

See `epics.md` § "Epic 18: Downstream Consumer Integration Contract Hardening" for the authoritative copy. The seven stories are: 18.1 AppHost Project-Resolution Guard and Public-Surface Stability Contract (MEM-1); 18.2 Deployment Configuration Contract Publication (MEM-2); 18.3 Invocable Route and Operation Surface Publication (MEM-3); 18.4 Stable Ingest Contract with Explicit Idempotency Token and Atomic Dedup (MEM-4); 18.5 Source-URI-Keyed Memory-Unit Lookup Endpoint (MEM-5); 18.6 MemoryUnitId Stability Contract (MEM-6); 18.7 MemoriesClient Mockability Stability Contract (MEM-7).
