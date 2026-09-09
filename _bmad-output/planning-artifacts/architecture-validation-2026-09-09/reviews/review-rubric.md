# Architecture Reviewer Gate — Rubric Walker

**Candidate:** `../../architecture.md` (legacy architecture document)  
**Review date:** 2026-09-09  
**Intent:** Validate only; the candidate was not changed.  
**Verdict:** **FAIL — unsafe as the current architecture spine.** The document contains valuable design history, but it is not a convergent build substrate: load-bearing rules conflict with one another and with the brownfield code, the current PRD is not covered, and current deployment/operational reality is represented only by scattered amendments. Replace it through an architecture Update that distils a short, named-paradigm spine; do not certify or lightly patch this legacy document as-is.

## Evidence reviewed

- Full candidate: `_bmad-output/planning-artifacts/architecture.md` (1,769 lines).
- Current product inputs: `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/planning-artifacts/addendum.md`, `_bmad-output/planning-artifacts/.memlog.md`, and `_bmad-output/planning-artifacts/review-downstream-drift.md`.
- Brownfield anchors: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `Hexalith.Memories.slnx`, `tools/release-packages.json`, the Server/AppHost project and composition code, representative workflow/search/actor code, deployment manifests, and operations runbooks.
- Mechanical pass: the candidate was copied to a temporary `ARCHITECTURE-SPINE.md` and checked with `lint_spine.py`; the candidate itself was not modified. The linter reported six low possible-template-token hits, all intentional naming examples (`{tenant}` / `{model-version}`). More importantly, it could not check decision integrity because the legacy document has no `AD-n` blocks with `Binds` / `Prevents` / `Rule` fields.
- Version check: the repository pins were compared with the official NuGet registry. The current repository pins for [Aspire.Hosting 13.5.3](https://www.nuget.org/packages/Aspire.Hosting/13.5.3), [Dapr.Client 1.18.5](https://www.nuget.org/packages/Dapr.Client/1.18.5), [NRedisStack 1.7.4](https://www.nuget.org/packages/NRedisStack/1.7.4), and [StackExchange.Redis 3.1.31](https://www.nuget.org/packages/StackExchange.Redis/3.1.31) are published versions; the candidate's March table is not current.

## Good-spine checklist

| Checklist item | Judgment | Evidence and consequence |
|---|---|---|
| Fixes the real divergence points for the level below and misses none | **Fail** | It leaves incompatible actor identities (`architecture.md:557` vs `architecture.md:598`), contradicts its own infrastructure boundary (`architecture.md:604-648`), and omits the current graph auto-seeding decision (`prd.md:162`, `prd.md:1011`) even though the code still skips graph without a start node (`src/Hexalith.Memories.Server/Search/HybridSearchService.cs:168-186`). |
| Every decision Rule is enforceable and prevents its stated divergence | **Fail** | The 31 decisions are registry/table entries, not `AD-n` contracts, and have no explicit `Binds`, `Prevents`, or `Rule`. `architecture.md:535-536` calls them implementation-blocking and `architecture.md:1750-1759` orders agents to follow them all, but the document supplies no unambiguous precedence when they conflict. |
| Nothing under Deferred can let two units diverge | **Fail** | The document still defers/opens work that is implemented: index migration (`architecture.md:296-301`) versus blue/green migration (`docs/operations/index-rebuild.md:10-17`, `:69-76`, `:167-177`); REST and export (`architecture.md:538-542`, `architecture.md:1670-1674`) versus the current REST/export surfaces (`src/Hexalith.Memories.Server/Program.cs:88-96`, `prd.md:987`, `prd.md:1091`). A new unit cannot know whether to extend, defer, or replace these paths. |
| Named technology is verified-current | **Fail** | The candidate labels a March 2026 table “Current Verified Versions” (`architecture.md:464-480`) and later repeats those pins as handoff instructions (`architecture.md:1761-1766`). Current pins are SDK 10.0.400 (`global.json:1-8`), Aspire 13.5.3 (`src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:1`), Dapr 1.18.5, NRedisStack 1.7.4, StackExchange.Redis 3.1.31, and NFalkorDB 1.2.0 (`references/Hexalith.Builds/Props/Directory.Packages.props:113-120`, `:139-146`, `:188`, `:257-259`). |
| Ratifies rather than contradicts the brownfield codebase | **Fail** | The candidate describes a Python `ai-agent` service and makes it part of topology/data flow (`architecture.md:267-268`, `:571`, `:601-602`, `:1157-1232`, `:1514-1533`, `:1608-1614`), but no such service exists. The current AppHost wires Conversation directly to Server (`src/Hexalith.Memories.AppHost/Program.cs:263-274`, `:315-343`) and adds EventStore, MCP, and access-telemetry resources instead (`:283-303`, `:403-476`, `:480-500`). The project/package inventory is likewise obsolete. |
| Covers the driving specification's capabilities | **Fail** | The candidate validates “31/31” NFRs (`architecture.md:1660-1666`), while the current PRD owns NFR1-NFR36 (`prd.md:33-37`, `prd.md:1096-1107`). It misclassifies MVP auth (`architecture.md:315`, `:1664-1666` vs `prd.md:1123-1130`), omits the hard graph auto-seed/two-axis-control gaps (`prd.md:161-162`, `:984`, `:1011`, `:1019`), retains the old benchmark gap (`architecture.md:370-377`), and retains a pre-update status vocabulary (`architecture.md:100-119` vs `prd.md:78-93`). |
| Does not weaken or contradict an inherited parent spine | **N/A** | No parent spine is declared. The candidate is initiative/system altitude and is itself the legacy source being validated. |
| Every owned dimension is decided, deferred, or an open question, especially deployment/environments/infra/operations | **Fail** | Local topology, CI, and isolated operational amendments exist, so the dimension is not wholly silent; however, there is no coherent current environment contract. The candidate's topology lists Server, a nonexistent AI agent, Redis, FalkorDB, and Dashboard (`architecture.md:566-574`), while current base/production/qualification manifests add MCP, EventStore-related wiring, OpenBao, access telemetry, PostgreSQL, explicit disabled/enabled profiles, RBAC, and qualification evidence (`deploy/kubernetes/base/kustomization.yaml:1-41`, `deploy/kubernetes/overlays/production/kustomization.yaml:1-27`, `deploy/kubernetes/overlays/qualification/kustomization.yaml:1-58`). Ownership, promotion gates, supported failure envelopes, and what remains unqualified are not distilled into one enforceable contract. |

## Critical findings

### C1 — The purported binding contract is internally contradictory and has no enforceable decision form

**Evidence**

- `architecture.md:535-536` says D1-D31 are critical and block implementation; `architecture.md:1750-1759` instructs agents to follow all 31 exactly.
- D15 requires actor IDs shaped as `{actorType}-{tenantId}` (`architecture.md:557`, reinforced at `:775-778`), while the actor table and D24 say “Actor ID = tenant ID” (`architecture.md:342-347`, `:598`). Current rate-limiter code uses `new ActorId(input.TenantId)` with actor type passed separately (`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:102-105`); other current actors use tenant-only, tenant/case, or fixed-global identities according to ownership.
- D30 says direct `NRedisStack` / `NFalkorDB` usage is allowed only in Redis/EventStore boundary projects (`architecture.md:604`, `:619-648`), but the Server directly references NFalkorDB, NRedisStack, and StackExchange.Redis (`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:43-59`) and directly uses those APIs (`src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:16-54`; `src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:16-59`). `Hexalith.Memories.Redis` is now explicitly a compatibility facade whose description tells new code to reference those packages directly (`src/Hexalith.Memories.Redis/Hexalith.Memories.Redis.csproj:3-20`).
- None of these registry decisions has the spine's required `Binds` / `Prevents` / `Rule` contract or a precedence rule.

**Why critical:** Two independently built units can choose different actor state keys and therefore read/write different durable state. Likewise, one unit can move direct storage code behind a boundary while another legitimately follows the current Server pattern. Both choices claim support from the same “mandatory” document.

**Recommended disposition: `discuss` (then Update).** Decide and ratify the actor-identity grammar per actor ownership, and decide whether D30 governs connection construction only or also backend API consumption. Distil both as separate `AD-n` rules with explicit scope and automated enforcement. Do not mechanically rewrite the code or declare either legacy sentence authoritative without that decision.

### C2 — The candidate no longer covers the current product contract, including security and the thesis gate

**Evidence**

- The candidate starts from 31 NFRs and C# 13 (`architecture.md:32-37`, `:61`) and certifies 31/31 coverage (`architecture.md:1660-1666`); the change-controlled PRD is explicitly NFR1-NFR36 and .NET 10/C# 14 (`prd.md:33-37`, `prd.md:679-685`, `prd.md:1096-1107`). The PRD memlog explicitly overrides C# 13 and the old NFR set (`.memlog.md:31-38`, `:59-62`).
- The candidate puts `TenantAuthorizationMiddleware` outside the gate and in Phase 1.5 (`architecture.md:303-320`) and repeats NFR11 as Phase 1.5 (`architecture.md:1664-1666`), even though another candidate section says auth is implemented (`architecture.md:217-220`, D8 at `:404`) and current code makes authentication/authorization/tenant middleware part of the active Server pipeline (`src/Hexalith.Memories.Server/Program.cs:38-63`). The current PRD says unauthenticated product ingress is not a Phase 1.5 allowance (`prd.md:1123-1130`).
- The current thesis contract requires graph auto-seeding from the top syntactic/semantic candidates and a BM25+semantic control (`prd.md:159-162`, `prd.md:979-986`, `prd.md:1006-1019`). The candidate instead presents the old 3-5-query benchmark gap (`architecture.md:370-377`) and never fixes the auto-seeding rule. Current code confirms the gap by skipping graph when no start node is supplied (`src/Hexalith.Memories.Server/Search/HybridSearchService.cs:168-186`).
- The current PRD/addendum explicitly warns that architecture Requirements Overview/Coverage/PRD Deviations are stale (`addendum.md:118-130`; `review-downstream-drift.md:147-154`, `:212-224`).

**Why critical:** Following the candidate can defer an active security invariant and validate the core product thesis against the wrong experiment. These are release/safety decisions, not cosmetic document drift.

**Recommended disposition: `discuss` (then Update).** Re-extract only current product constraints from `prd.md` plus `addendum.md`; carry current open gaps as explicit open items with owner/revisit condition. The architecture Update must not edit or weaken the PRD to match these fossils.

## High findings

### H1 — The topology and ownership model invent a current Python service and omit the services that actually exist

**Evidence:** D27/D28 make the Python `ai-agent` a chosen MVP/Phase 1.5 service (`architecture.md:601-602`); the deployment table, sample AppHost code, directory tree, service boundary, and ingest flow all treat it as real (`architecture.md:566-574`, `:1199-1232`, `:1514-1533`, `:1562-1569`, `:1596-1620`). There is no `services/ai-agent` directory or `CallAiAgentActivity`/`AiEnrichmentWorkflow`. Current AppHost composes EventStore, Server, MCP, access-telemetry lifecycle, and clock services (`src/Hexalith.Memories.AppHost/Program.cs:283-343`, `:403-500`) and wires the DAPR Conversation component directly to Server (`:263-274`). The current PRD keeps the optional sidecar as an architecture-owned open question (`prd.md:1197-1206`), not established brownfield topology.

**Divergence:** A feature builder could add or mirror Python contracts and service invocation, while another extends the shipped Server-side Conversation path.

**Recommended disposition: `discuss`.** Resolve PRD Open Question 7. Until promoted, move the Python service to Deferred/Open with an owner and revisit condition. Ratify the current AppHost graph as seed; do not present speculative source trees as current structure.

### H2 — D30 contradicts the current storage boundary and therefore cannot prevent dependency drift

**Evidence:** D30's registry and invariant say product projects reach infrastructure through DAPR/Aspire and direct clients live only in boundary projects (`architecture.md:603-605`, `:619-648`). Yet current Server composition intentionally consumes Aspire-created keyed multiplexers (`src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:276-330`), and Server feature code directly uses Redis/FalkorDB APIs (examples above). The Redis package is compatibility-only (`src/Hexalith.Memories.Redis/Hexalith.Memories.Redis.csproj:3-20`).

**Divergence:** “Connection construction belongs at the composition boundary” and “all backend API use belongs outside Server” are materially different rules; the text slides between them.

**Recommended disposition: `discuss`.** Preserve the current, narrower invariant if intended: endpoint/credential/connection construction is boundary-owned; Server may consume keyed connections and backend libraries in infrastructure-facing feature adapters. Otherwise create and fund a real relocation decision. Add a structural test matching the chosen rule.

### H3 — Operational and environmental breadth is fragmented and materially incomplete

**Evidence:** The candidate's “updated” topology still has only five containers and includes the nonexistent AI service (`architecture.md:566-574`); its main environment story is local Aspire (`architecture.md:244-270`). D31 and the long access-telemetry paragraph (`architecture.md:227`, `:650-677`) append mechanisms but do not reconcile a deployment model. Current deployment has explicit base, Production, and qualification overlays; MCP; access-telemetry/clock; PostgreSQL; Redis/FalkorDB stateful sets; DAPR access-control resources; OpenBao artifacts; and a Production-disabled versus qualification-enabled telemetry posture (`deploy/kubernetes/base/kustomization.yaml:1-41`, `deploy/kubernetes/overlays/production/kustomization.yaml:1-27`, `deploy/kubernetes/overlays/qualification/kustomization.yaml:1-58`). Current recovery ownership and supported paths are also explicit (`docs/operations/index-rebuild.md:1-32`, `:48-76`, `:129-177`) but absent from the supposed spine.

**Divergence:** Units can choose different deployment profiles, readiness expectations, access-telemetry activation rules, and recovery ownership because no concise environment invariant binds them.

**Recommended disposition: `autofix` in the Update.** Distil an environment matrix and operational ownership rules from current manifests/runbooks. Mark exact Production access-telemetry qualification as open/deferred with its existing gate; keep deployment manifests and runbooks as seed authorities rather than copying their full contents.

### H4 — Deferred/open sections contain resolved brownfield decisions

**Evidence:** Index rebuild remains an “Open Decision” accepting degradation (`architecture.md:296-301`), but current implementation and runbook support blue/green migration and atomic alias switching (`tools/MigrateEmbeddingVectors/Program.cs:44-76`; `docs/operations/index-rebuild.md:10-17`, `:69-76`, `:167-177`). Full REST remains deferred (`architecture.md:538-542`), while current Server maps ingestion, tenants, export/import, consistency, cases, search, graph, and derived-store endpoints (`src/Hexalith.Memories.Server/Program.cs:88-97`) and the PRD identifies shipped REST/Client.Rest surfaces (`prd.md:871-900`). FR71 is called missing/deferred (`architecture.md:1670-1674`) even though the current PRD records it shipped early (`prd.md:979-987`, `:1089-1094`). `status` and `quickstart` are also parked in Phase 1.5 (`architecture.md:137-146`) despite the current Phase 1 CLI contract (`prd.md:871-900`).

**Divergence:** Teams can schedule already-built capabilities again or build incompatible alternate mechanisms.

**Recommended disposition: `autofix` in the Update.** Convert implemented choices to adopted rules or code-owned seed; retain only genuine limitations (for example, no generic rebuild and incomplete semantic repair) under Deferred with explicit revisit triggers.

### H5 — The candidate does not lead with a paradigm or isolate the few durable invariants

**Evidence:** The document identifies itself as an append-built “Architecture Decision Document” (`architecture.md:19-23`), then mixes requirements summary, starter evaluation, source-code examples, a ~65-file speculative tree, validation self-certification, and implementation steps (`architecture.md:409-529`, `:731-1310`, `:1312-1550`, `:1638-1769`). It never names a single governing paradigm. The most important durable pattern—EventStore-acknowledged domain truth with workflow-orchestrated rebuildable projections—is buried in one very long concern (`architecture.md:98`) and D3 (`architecture.md:582`).

**Divergence:** Readers cannot distinguish invariant from historical rationale or code-owned seed, and later amendments have equal visual weight with superseded scaffolding. Different units will select different “authoritative” paragraphs.

**Recommended disposition: `autofix` through replacement/distillation, not prose trimming.** Lead with a named paradigm such as **event-sourced, workflow-orchestrated vertical slices with rebuildable search projections** (final wording to be confirmed), then retain only non-obvious trade-off decisions that prevent sibling units from diverging. Move rationale/history to the memlog and let code/config own project tree and API shape.

## Medium findings

### M1 — Version pins are stale and the handoff would regress the repository

**Evidence:** `architecture.md:464-480` pins Aspire 13.1.3, Dapr 1.17.6, NRedisStack 1.3.0, StackExchange.Redis 2.12.4, and NFalkorDB 1.0.0; `architecture.md:1761-1766` tells implementers to use the old Dapr pins. Current pins are newer as listed in the checklist, and the official registry confirms the current Aspire/Dapr/Redis package versions.

**Recommended disposition: `autofix`.** A new spine should identify `global.json`, AppHost SDK, and central package props as authorities and record only compatibility constraints that are architectural. Avoid copying volatile package catalogs into the spine.

### M2 — The package/project inventory is false seed

**Evidence:** The candidate says seven published plus three non-packable components (`architecture.md:51-55`) and presents an obsolete project tree (`architecture.md:793-815`, `:1312-1550`). Current `tools/release-packages.json:1-48` lists nine published and six non-packable projects, while `Hexalith.Memories.slnx:1-36` contains 15 source projects and 13 test projects, including Aspire, Web, Telemetry, and three access-telemetry projects omitted by the tree.

**Recommended disposition: `autofix`.** Replace counts/tree with one seed pointer to `tools/release-packages.json` and `Hexalith.Memories.slnx`; retain only dependency direction/ownership that cannot be inferred from project references.

### M3 — Stored and operator-facing ingestion states are not reconciled

**Evidence:** The candidate's draft field inventory fixes `queued/extracting/embedding/indexing/indexed/failed` (`architecture.md:100-119`), while the current PRD defines operator states and an explicit mapping to the shipped enum (`prd.md:78-93`, `prd.md:997-1004`). The candidate still describes a “Pipeline Actor” in one failure row (`architecture.md:180-188`) despite later Workflow ownership.

**Recommended disposition: `autofix`.** Adopt the PRD mapping as the contract, name the enum as code-owned seed, and remove the pipeline-actor fossil. Revisit only at the next breaking contract window already recorded in `prd.md:1205-1206`.

### M4 — The license statement is stale

**Evidence:** The candidate tree says Apache 2.0 (`architecture.md:1327-1329`). The repository `LICENSE:1-20` and all packable project metadata use MIT, and the current product decision closes license as MIT (`prd.md:1197-1203`; `.memlog.md:74-75`).

**Recommended disposition: `autofix`.** Remove the obsolete Apache statement. The spine may record the MIT no-restrictive-relicense invariant if it is judged architecture-level; otherwise point to repository license metadata.

## Low / mechanical tail

- The duplicate `workflowType` key in frontmatter (`architecture.md:3`, `:13`) and “status: complete” self-certification (`architecture.md:5`) are legacy-format residue. **Disposition: `ignore` in this file; replacement removes them.**
- The linter's six low `{tenant}` / `{model-version}` hits are intentional naming tokens rather than placeholders. **Disposition: `ignore`.** Their presence does not offset the fact that no `AD-n` decision blocks were available for deterministic field checks.

## Recommended gate action

1. **Do not approve this candidate as a spine.** Keep it read-only as historical input until the Update has preserved useful decisions.
2. **Discuss C1/C2 and H1/H2:** actor identity, infrastructure boundary scope, current PRD/security/thesis constraints, and Python-sidecar disposition are load-bearing choices.
3. **Autofix through a fresh distillation:** ratify current code/config, make the paradigm explicit, produce stable `AD-n` entries with `Binds` / `Prevents` / `Rule`, and cover environment/operations explicitly.
4. **Move history and rationale to the architecture memlog.** Treat solution/project trees, package inventories, endpoint lists, and deployment manifests as code-owned seed referenced by authority.
5. Re-run the deterministic linter and all configured reviewer lenses against the resulting `ARCHITECTURE-SPINE.md` before handoff.
