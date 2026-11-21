# Brownfield source-fidelity review

Reviewed the November 20 PRD and its addendum at Memories `75c5b5f5` against the current architecture, local source, release inventory and dated evidence. Evidence locations below are repository-relative. This review used source inspection and did not repeat operational qualification runs.

**[high]** NFR21 still requires publisher-controlled source-prefix routing (§ PRD 273, 1180) — A team implementing the normative row could pass its envelope test while selecting a tenant from publisher-controlled input. The October correction in the same PRD explicitly rejects that selection for the target. *Fix:* Keep `source` as an envelope/identity field and require candidate-tenant selection from the authenticated delivery channel plus the operator allowlist, tenant grant and active lifecycle check. Retain source-prefix matching only as a description of current code.

Evidence: `src/Hexalith.Memories.EventStore/TenantEventRouter.cs:67,284` reads and matches the envelope source. `_bmad-output/planning-artifacts/epics.md:6923` assigns authenticated channel routing to Story 34.10.

**[high]** FR65 drops the issuer from external provenance (§ PRD 769, 1103 and addendum 63) — A normalized OIDC subject alone can represent distinct people under two trusted issuers. AD-13 and the accepted Story 34.27 criterion bind external provenance to the normalized issuer and subject together. The FR65 implementation gap is already disclosed, but the written acceptance condition still permits a collision. *Fix:* Use the issuer–subject pair consistently and include equal subjects under different issuers in the provenance acceptance case.

Evidence: `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md:198`; `_bmad-output/planning-artifacts/epics.md:7698,7710`.

**[high]** FR43 is classified as delivered under mutually exclusive CLI obligations (§ PRD 918, 1003, 1067) — FR43 requires the CLI to warn that vectors will be rebuilt, while the Phase 1 surface explicitly excludes a CLI verb for FR43. The server rejects unacknowledged changes and records a reindex-required flag after acknowledgment. The ratified MVP rebuild and atomic cutover still have an open story. *Fix:* Identify the Phase 1 operator surface for acknowledgment, credit the existing guard, and classify remaining warning, rebuild and cutover work separately in the delivery register.

Evidence: `src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:63–79`; `_bmad-output/planning-artifacts/epics.md:7886,7915`; architecture spine AD-14 and its rebuild gap.

**[high]** FR74 credits a mutating repair with an authoritative dry run (§ PRD 911, 1003, 1118) — The compatibility command prompts for confirmation or accepts `--yes`, then schedules repair from a fresh consistency comparison. The request has no dry-run selector or authoritative EventStore revision. An operator following the delivery row could mistake confirmation for a preview of proposed writes. *Fix:* Credit the shipped consistency check and presence-based repair separately. Mark FR74 partial until Story 34.30 supplies a revision-bound preview, fenced apply and deletion provenance.

Evidence: `src/Hexalith.Memories.Cli/Commands/ConsistencyRepairCommand.cs:19–31,65–77`; `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs:210–216`; `src/Hexalith.Memories.Server/Workflows/ConsistencyRepairWorkflow.cs:17–24`; `_bmad-output/planning-artifacts/epics.md:7839–7856,7869`.

**[medium]** The distribution tables omit a package and a required host (§ PRD 712–740 and addendum 90–93) — The declared source of truth includes the `Hexalith.Memories.EventStoreExtensions` package and the non-packable `Hexalith.Memories.DomainService` that serves authenticated commands and replay. The Web entry is a Razor component library. The executable specimen lives in the tests tree. *Fix:* Reconcile both PRD lists with the manifest, distinguish the native Server and DomainService, and identify the actual runnable specimen.

Evidence: `tools/release-packages.json:41–52`; `docs/operations/route-surface.md:153–157`; `src/Hexalith.Memories.Web/Hexalith.Memories.Web.csproj:1–7`; `tests/Hexalith.Memories.Web.SpecimenHost/Hexalith.Memories.Web.SpecimenHost.csproj:1–11`.

**[medium]** The platform matrix assigns the obsolete CLI to future releases (§ PRD 36, 705–707, 893) — The MVP column installs `Hexalith.Memories.Cli` and the future column says “Same.” This conflicts with the approved target of enrolled McpCli operations. Its adjacent client description also overlooks the legacy CLI’s existing reference to `Hexalith.Memories.Client.Rest`. *Fix:* Label and describe the current compatibility transport in one row and identify the shared McpCli target in the phase columns.

Evidence: `src/Hexalith.Memories.Cli/Hexalith.Memories.Cli.csproj:33`; `references/Hexalith.McpCli/_bmad-output/implementation-artifacts/memories-migration-inventory-v1.1.0.md:29–49`.

**[medium]** Operator guidance sends readers to unavailable commands (§ PRD 800, 851–857, 907–911) — The consistency check is implemented as `memories consistency verify`. The compatibility CLI offers only `tenant list` and `status telemetry` under those parents. The earlier sections present `tenant verify`, `status --case` and `status --failed` as working commands despite the later command inventory. *Fix:* Use the working consistency command, cite native status/failed-unit routes where applicable, and label unavailable commands as desired behavior.

Evidence: `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:105–138`; `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs:53,62,121`.

**[medium]** The versioning rule contradicts the shipped V1 API (§ PRD 784–789, 918) — “No versioned endpoints” conflicts with the published `/api/v1` route constants and the invocation ACL contract. The unversioned Dapr app ID and the versioned method route address different parts of a request. *Fix:* Describe the V1 path, additive compatibility rules and unversioned app ID separately.

Evidence: `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs:9–17,29–33`; `docs/operations/route-surface.md:15–20`.

**[medium]** FR71 completion conceals outstanding custody-transfer requirements (§ PRD 995, 999–1010, 1115) — The delivery register claims the current obligation shipped early. Story 8.3 delivered streamed JSON. The subsequently approved FR71 architecture binds verifiable origin, controlled handoff and source-held staging cleanup, and the successor story is still open. *Fix:* Keep the historical Story 8.3 delivery and Phase 2 scope. Distinguish it from the outstanding FR71 custody contract and link the existing successor work.

Evidence: `src/Hexalith.Memories.Server/Endpoints/ExportEndpoints.cs:111–118`; `_bmad-output/planning-artifacts/epics.md:7646–7648,7682`; architecture spine AD-16 and FR71 gap at line 505.

**[medium]** The November delivery register misses the signed-URL resolution (§ PRD 1009, 1016) — The register points to DW-732 as tracking remediation. The same-day resolution records safe logs and case activity on success, duplicate and failure, with 159 focused tests and two live integration cases. The exact fetch/dedup identity remains intact. *Fix:* Credit DW-732 for that bounded behavior, retain incomplete target CLI delivery for FR2/FR36, and keep the durable workflow-history obligation separate.

Evidence: `_bmad-output/implementation-artifacts/deferred-work.md:6366–6375,6392`; superproject fix `75c5b5f5`.

**[medium]** The addendum leaves the public hybrid score on the wrong scale (§ addendum 19–24 and PRD 1188) — Its canonical contribution formula yields 1/11 for a first-ranked axis. The public hybrid score is normalized against that raw maximum, so rank one is 1.0 and rank two is 11/12. A presenter following the addendum alone could report a different confidence breakdown. *Fix:* Qualify the formula as the internal raw term and reference AD-9 for public V1 output, weights and normalization.

Evidence: `src/Hexalith.Memories.Server/Search/FusionEngine.cs:18,239–250`; architecture spine AD-9, line 170.
