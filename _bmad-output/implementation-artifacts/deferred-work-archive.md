### DW-13: `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story explicitly accepts or reassigns them.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/19-2-downstream-contract-artifact-generation-decisions.md:7 records Status: done.

### DW-14: `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the story explicitly accepts or reassigns them.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/19-3-release-preflight-and-baseline-evidence-residual-sweep.md:7 records Status: done.

### DW-15: `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals target Story 19.4 unless the story explicitly accepts or reassigns them.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md:7 records Status: done.

### DW-16: Other active `open` or `carried-forward` entries are classified by Story 19.1

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - Other active `open` or `carried-forward` entries are classified by Story 19.1
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md:7 records Status: done.

### DW-17: 20.5-A41-ACCESS-TELEMETRY-RETENTION: carried-forward. Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision.

origin: migrated from legacy ledger ("Story 20.5 Deferred Retention Slice (2026-07-04)"), 2026-09-01
location: docs/dev/telemetry.md`, the Story 27.1 architecture decision, the selected access-telemetry sink/storage deployment and purge implementation, and focused lifecycle/tenant-privacy tests, or this entry updated to a complete explicit accepted-debt disposition.
reason: - **20.5-A41-ACCESS-TELEMETRY-RETENTION - carried-forward.** Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision. - ID: 20.5-A41-ACCESS-TELEMETRY-RETENTION - Status: carried-forward - Source story: 20-5-inbound-rate-limiting-quotas-and-audit-completeness - Backlog home: Epic 27, registered Stories 27.1-27.4, plus the held C1 successor definitions in approved Sprint Change Proposal 2026-08-01. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification; all twenty-five C1 gates have no registered story owner; Story 27.4 owns deployment-shaped verification and close-out but remains `backlog` until compliant successor files are later registered and every C1 gate passes on its own evidence. Production lifecycle writes remain disabled and A41 remains open. Scheduling or a held proposal annex does not satisfy the resolution gate. **Corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01.** - Target artifact: `docs/dev/telemetry.md`, the Story 27.1 architecture decision, the selected access-telemetry sink/storage deployment and purge implementation, and focused lifecycle/tenant-privacy tests, or this entry updated to a complete explicit accepted-debt disposition. - Resolution gate: Keep this entry `carried-forward` and the matching sprint action `open` until bounded retention/TTL is implemented, documented, and validated, or accepted debt records a named approver and owner, affected storage/scope, rationale, risk and consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger. - Re-open trigger: Review before any claim that A41 is fully closed, before any production-retention assurance is made, and at the accepted-debt review/expiry trigger if that path is selected. - Rationale: Inbound quotas and audit completeness are implemented in Story 20.5; access telemetry retention remains unaddressed and is carried forward to avoid falsely closing the A41 retention requirement. Owner: operations maintainer / security remediation owner. **Current correction 2026-09-01:** Story 27.21 is the registered `in-progress` owner of C1.15; the remaining twenty-four C1 gates stay unowned. This dated correction changes no A41, Production-write, Story 27.4, or deferred-record status.
status: done 2026-11-01
archived: 2026-11-23
resolution: 2026-11-01 — Resolved by accepted debt; not closed as qualified (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md`). Evidence: bounded retention/TTL is implemented by Story 27.2 and functionally validated by Story 27.4 run `0673d50339f4e1058c26e3a9b2d680a2` (real Dapr 1.18.1 / PostgreSQL 18.6; native TRX 1/1; `functional-observations-passed`; verified disposal; `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`). Approver and owner: Jérôme Piquot (`github:user:6775094`). Scope of the debt: Production use of the PostgreSQL access-telemetry lifecycle store — two-writer Production shape; workload/sidecar/actor/Placement/Scheduler/backend fault recovery; backup/restore; physical tuple deletion and reclamation; capacity at the 151.2M-record / 7-day envelope. Evidence limits: one writer, Development, file secrets and development UTC, 3600-second retention, logical expiry / strong absence / due-index and reminder progress only. Rationale: no Production consumer exists and lifecycle writes are disabled. Risk and consequence: Production access telemetry exists only as JSON-console/OTLP emission (tenant, user and operation fields of `AccessTelemetryEvent`) whose retention is set by the operator's log platform; Memories does not bound Production retention while the store is disabled. Compensating controls: (1) operators configure access-telemetry retention of 7 days or less on the log/OTLP backend (`docs/operations/access-telemetry-lifecycle.md`, section "Production access-telemetry retention"); (2) the Memories Production overlay keeps `ACCESS_TELEMETRY_ENABLED=false`, pinned in CI by `tests/tooling/access_telemetry_c1` (`ci.yml:388`) and `integration-fast` (`ci.yml:545`), and lifecycle `replicas: 0`, pinned by `AccessTelemetryA41CloseOutTests` and `ProductionDeploymentArtifactsTests` in `test-unit-contract` (`ci.yml:421`); (3) the Production options validator requires Dapr-configuration retention and a 64-hex component profile hash, and the overlay ships `unproven`; (4) the PRD states access telemetry is not a retention-compliance guarantee (`prd.md:551`). Reopen trigger: any Production deployment path (the Memories overlay or Platform composition through manifest reference `accesstelemetrylifecycle-enabled`) that sets `AccessTelemetryLifecycle:Enabled=true` or runs a lifecycle Deployment above zero replicas; reopening this entry also reopens DW-730. Review date: 2027-05-01 (sprint-status Epic 27 action item).
decision: 2026-11-01 resolve-by-accepted-debt — owner Jérôme Piquot answered "yes" to D1–D4 of sprint-change-proposal-2026-11-01.md (Story 27.4 scope reset).
decision: 2026-09-01 Implement bounded retention — Define the approved period and scope, implement purge or TTL behavior, document it, and add privacy verification.
decision: 2026-09-01 Implement bounded retention — Define the approved period and scope, implement purge or TTL behavior, document it, and add privacy verification.

### DW-18: 18.4-REDIS-RACE: accepted. Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs (real-Redis / Aspire-Testcontainers concurrency lane)
reason: - **18.4-REDIS-RACE - accepted.** Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute. - ID: 18.4-REDIS-RACE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs (real-Redis / Aspire-Testcontainers concurrency lane) - Re-open trigger: before any production claim about concurrent ingest, run the real two-thread Redis race wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI. - Rationale: Story 18.4 is substitute-proven by a deterministic winner/loser reservation test and unit-proven today; the real two-thread Redis race is infra-lane-deferred because this sandbox cannot run the Docker/Aspire lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]
status: done 2026-09-01
archived: 2026-11-23
resolution: resolved by sweep bundle dw-redis-ingest-race-proof
resolution-undo: 359d6619c61ffa57f0fd81f56e167e58a181ed120faa003eb485b6f0fda64b4a 2026-09-01 7374617475733a206f70656e

### DW-19: 18.8-DAPR-SMOKE: accepted. Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests (Dapr-sidecar pub/sub smoke lane over /events/ingest)
reason: - **18.8-DAPR-SMOKE - accepted.** Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute. - ID: 18.8-DAPR-SMOKE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: tests/Hexalith.Memories.IntegrationTests (Dapr-sidecar pub/sub smoke lane over /events/ingest) - Re-open trigger: before any production claim about cross-module event delivery, run the Dapr-sidecar pub/sub smoke wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI. - Rationale: Story 18.8 is proven today by in-process HTTP E2E tests over `/events/ingest`; the Dapr-sidecar smoke is infra-lane-deferred because this sandbox cannot run the sidecar lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.8]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs:123-134

### DW-22: 12.6-RV5: resolved. The `EmbeddingInputContentKindTests` telemetry

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs
reason: - **12.6-RV5 - resolved.** The `EmbeddingInputContentKindTests` telemetry assertions now use per-test unique tenant ids, tenant-filtered captures, and a thread-safe capture queue, removing the dormant static-meter contamination risk that originally motivated the S11-FA release-lane baseline. - ID: 12.6-RV5 - Status: resolved - Source story: deferred-work-implementation-2026-05-19 - Target artifact: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs - Re-open trigger: `EmbeddingInputContentKindTests` flakes again, or another story adds a concurrent `MemoriesMeter.EmbeddingApiCalls` assertion path that could share static meter captures. - Evidence: The focused telemetry tests now call `UniqueTenantId(...)`, capture only matching `tenant_id` measurements from `MemoriesMeter.EmbeddingApiCalls`, store observations in `ConcurrentQueue<(TenantId, ContentKind, Delta)>`, and assert a single matching metric event per test case.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-23: Story-9.3-ProjectionRegistryCrossCheck: resolved. Handler mismatch

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs; src/Hexalith.Memories.EventStore/ProjectionBinding.cs; src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs; src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs; src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs; tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs
reason: - **Story-9.3-ProjectionRegistryCrossCheck - resolved.** Handler mismatch detection now has a repository-owned projection binding provider contract and emits `ProjectionBindingMissing` only when an authoritative tenant-scoped registry proves a configured route lacks a runtime projection binding. - ID: Story-9.3-ProjectionRegistryCrossCheck - Status: resolved - Source story: 16-1-projection-registry-cross-check-design - Target artifact: src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs; src/Hexalith.Memories.EventStore/ProjectionBinding.cs; src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs; src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs; src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs; tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs - Re-open trigger: A host needs automatic EventStore discovery adaptation, projection liveness or lag evidence, or authoritative registry detection beyond the host-provided tenant boundary. - Evidence: Story 16.1 adds `IProjectionBindingProvider`, default `Unknown` posture, authoritative-only `ProjectionBindingMissing` diagnostics, tenant-scoped deterministic matching, CLI/contract coverage, and operator documentation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs:178-203

### DW-24: 12.4-RV10: accepted. A parse-time warning for dropped bare-token bullets

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tools/check-story-file-scope.py
reason: - **12.4-RV10 - accepted.** A parse-time warning for dropped bare-token bullets may help story authors, but the current out-of-scope-files diagnostic already catches the issue once a changed file lands outside the parsed allow-list. - ID: 12.4-RV10 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: tools/check-story-file-scope.py - Re-open trigger: A contributor confusion incident or story-template redesign shows that pre-commit author warnings are needed before any changed-file validation runs. - Rationale: The value is low until there is evidence of author confusion, and adding parse-time stderr warnings could create noise for legitimate non-bullet prose.
status: done 2026-10-28
archived: 2026-11-23
resolution: Direct unbackticked allowed-list bullets now emit artifact/line-specific stderr warnings while remaining unauthorized; fenced, indented and read-only bullets stay silent. Reopen if warnings disappear or imply write access. Evidence: spec-five-deferred-story-scope.md and tests/tooling/story_scope/story_scope_validator_test.py; PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -q passed 71/71 methods on 2026-10-28.

### DW-25: 12.4-RV11: accepted. Local Windows absolute-path cosmetic noise remains

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tools/check-story-file-scope.py
reason: - **12.4-RV11 - accepted.** Local Windows absolute-path cosmetic noise remains intentionally accepted because CI diagnostics use repository-relative paths and do not expose maintainer-visible drive letters. - ID: 12.4-RV11 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: tools/check-story-file-scope.py - Re-open trigger: A PR review comment or release-evidence document cites a local Windows drive-letter path emitted by `tools/check-story-file-scope.py`, or a maintainer reports that pasting story-scope tooling output from a local Windows run into a shared review channel leaks a drive letter. - Rationale: The remaining issue is cosmetic and local-only; changing it now would add story-scope tooling churn without improving CI or reviewer evidence.
status: done 2026-10-28
archived: 2026-11-23
resolution: Artifact and local-input diagnostics now render relative POSIX paths; Windows cross-drive and UNC paths use an external marker without the drive/share anchor. Lookup and authorization stay independent of formatting. Reopen if a local path diagnostic emits an absolute drive prefix or names the wrong artifact. Evidence: spec-five-deferred-story-scope.md and tests/tooling/story_scope/story_scope_validator_test.py; PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -q passed 71/71 methods on 2026-10-28.

### DW-26: `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1:

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1: `S11-FC` and `12.1-RV4` are resolved, while `12.1-RV3` is accepted with a documented release-maintainer risk decision.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md:236 records the three dispositions.

### DW-27: `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled by Story 15.2 through the provider/model/dimension registry work.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md:269 records the resolved provider-registry findings.

### DW-28: `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live migration marker enforcement and accepted migration result semantics.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md:244 records the migration dispositions.

### DW-29: `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport policy enforcement and operations documentation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e68cd2e4

### DW-30: `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator`

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator` structured entry dated 2026-05-12; no new backlog item is created here.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit acfdf211

### DW-36: 15.4-RV1: Sanitization-message assertions are tautological. `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.

origin: migrated from legacy ledger ("Deferred from: code review of 15-4-token-endpoint-transport-policy (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.4-RV1 — Sanitization-message assertions are tautological.** `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.
status: done 2026-10-28
archived: 2026-11-23
resolution: 2026-10-28 — `spec-five-deferred-embedding-regressions.md`: Removed constant policy-vocabulary assertions from both sanitization helpers and added AcquireToken_RejectedEndpoint_DoesNotLeakIndividualUriSegments / Validate_RejectedOidcEndpoint_DoesNotLeakIndividualUriSegments with independent user-info, query, fragment and HTTP transport rejection rows. Both token entry points verify no logger leakage. Distinct user-info, host, encoded/decoded path, query name/value and fragment canaries must be absent; both ordinary and refresh token acquisition send zero requests. Existing parameter, host/path and credential-marker checks remain. Reopen if diagnostic formatting changes or a new URI component can leak. Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 354 cases with zero failures/errors/skips/not-run; Debug build passed with zero warnings/errors.

### DW-37: 13.2-RV4: resolved. OIDC token endpoint validation now enforces HTTPS for production and

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.4 Token Endpoint Transport Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs; src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; docs/operations/embedding-providers.md
reason: - **13.2-RV4 - resolved.** OIDC token endpoint validation now enforces HTTPS for production and permits `http://` only for literal local loopback hosts (`localhost`, `127.0.0.1`, and `[::1]`). The same policy is applied before tenant/default config persistence and before direct `IOidcTokenProvider` token acquisition, with sanitized errors that do not echo full endpoint URLs. - ID: 13.2-RV4 - Status: resolved - Source story: 15-4-token-endpoint-transport-policy - Target artifact: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs; src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; docs/operations/embedding-providers.md - Re-open trigger: Any non-loopback `http://` OIDC token endpoint reaches tenant config persistence, direct token acquisition, an outbound token HTTP request, logs, or snapshots without being rejected by the HTTPS/local-loopback policy. - Evidence: Story 15.4 added `OidcTokenProvider.ValidateTokenEndpointTransport(...)`, reused it from `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` for `OidcTokenEndpoint`, documented the production HTTPS/local loopback exception in `docs/operations/embedding-providers.md`, and added focused `OidcTokenProviderTests` plus `EmbeddingProviderDefaultsTests` coverage for accepted loopback HTTP, rejected public/private/link-local/Docker/DNS-alias/127.0.0.2 HTTP, no-request-before-rejection, and non-leaking error text.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:128-136

### DW-38: 13.6-RV1: resolved. Live migration cutover now writes a durable

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs
reason: - **13.6-RV1 - resolved.** Live migration cutover now writes a durable tenant-scoped active marker before index recreation or tenant config update, and runtime ingestion/indexing reads that marker to block stale provider/model writes for the migrating tenant. - ID: 13.6-RV1 - Status: resolved - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs - Re-open trigger: A production or test migration completes while raw or natural-language semantic hashes for the tenant contain a provider/model/dimensions tuple different from the active migration target after cutover. - Evidence: Story 15.3 added active marker writes in `RedisEmbeddingMigrationStore.StartMigrationMarkerAsync`, read/write guards in `GenerateEmbeddingActivity`, `IndexSemanticActivity`, and `IndexNaturalLanguageSemanticActivity`, plus focused tests proving old-provider generation and raw/NL semantic writes are blocked while the marker is active.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-39: 13.6-RV2: resolved. `IndexSemanticActivity.cs` now carries the standard

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs
reason: - **13.6-RV2 - resolved.** `IndexSemanticActivity.cs` now carries the standard ITANEO MIT copyright header because Story 15.3 touched the file substantively for the mandatory semantic write guard. - ID: 13.6-RV2 - Status: resolved - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs - Re-open trigger: A future hand-written C# source file touched by a story lacks the standard project copyright header. - Evidence: Story 15.3 added the missing copyright header while updating `IndexSemanticActivity` for active migration marker enforcement.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs:1

### DW-41: 13.1-RV6: resolved. Provider validation now has a shared maximum

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV6 - resolved.** Provider validation now has a shared maximum vector-dimension policy and rejects out-of-policy dimensions before any tenant state or index path can consume them. - ID: 13.1-RV6 - Status: resolved - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: A future registry entry accepts a vector dimension above the shared maximum without a story that explicitly raises the storage/memory policy, or a tenant config with `Dimensions = int.MaxValue` reaches persistence/index creation. - Evidence: Story 15.2 added `MaxSupportedDimensions = 16_384` in `EmbeddingProviderDefaults`, validates dimensions before model-specific allowlist checks, and added `EmbeddingProviderDefaultsTests.Validate_DimensionsAboveSharedMaximum_ShouldThrowAtConfigTime`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:213-259

### DW-43: 13.1-RV11: resolved. Provider/model/dimension validation now uses a

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV11 - resolved.** Provider/model/dimension validation now uses a closed provider-scoped registry, so cross-pollinated and unknown models fail by construction. - ID: 13.1-RV11 - Status: resolved - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: Any provider/model pair validates without being present in the local registry, or a provider falls back to another provider's defaults, dimensions, models, or rate-limit ceiling. - Evidence: Story 15.2 replaced scattered provider/model/dimension/rate-limit checks with a single local registry in `EmbeddingProviderDefaults` and added `EmbeddingProviderDefaultsTests.Validate_CrossProviderModelPairs_ShouldThrow`, `Validate_UnknownModelForProvider_ShouldThrowAndListProviderModels`, `Validate_SyntacticallyValidButUnregisteredModel_ShouldThrow`, and provider-scoped rate-limit tests.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:46-70

### DW-47: 15.2-RV3: accepted. Other test files still use unregistered model literals.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs, tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs
reason: - **15.2-RV3 - accepted.** Other test files still use unregistered model literals. - ID: 15.2-RV3 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs, tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs - Re-open trigger: The covered code paths add a Validate-preflight, OR a registry-wide test-fixture-hygiene sweep is scheduled. - Rationale: `HybridSearchServiceTests` (line 74) and `TenantEmbeddingConfigEndpointTests` (lines 30, 39) use `"text-embedding-004"` / `"different-model"` literals that compile only because those tests do not call `Validate`. Pre-existing; out of File Scope. Story 19.4 (2026-06-30) accepts this as fixture-hygiene debt: the literals remain harmless until those paths begin validating tenant configs or a registry-wide test-fixture sweep is scheduled. Natural future home: a registry-wide test-fixture hygiene sweep. Owner: Server test maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: 2026-10-28 — `spec-five-deferred-embedding-regressions.md`: HybridSearchServiceTests.MakeEmbeddingConfig now uses the registered Google default with its original rate/key overrides and validates it. EmbeddingConfigChangeException_ShouldContainAllFields uses validated Google-to-Ollama configs, checks the complete returned records and exact provider/model/dimensions list. Removed the two named consumers' unsupported model literals without registry or runtime changes. Reopen if fixtures introduce unsupported combinations or either consumer begins requiring different validation semantics. Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 354 cases with zero failures/errors/skips/not-run; Debug build passed with zero warnings/errors.

### DW-48: 15.2-RV4: accepted. `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
reason: - **15.2-RV4 - accepted.** `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded. - ID: 15.2-RV4 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs - Re-open trigger: Registry adds a third provider, OR an operator reports a failed identifier parse for a registered provider/model pair. - Rationale: Closed-allowlist behavior of `EmbeddingProviderDefaults` does not extend to `EmbeddingClient.ParseEmbeddingProviderIdentifier` or the dispatch site, which binary-check `IsGoogle || IsOllama`. Architectural follow-up; out of this story's File Scope. Story 19.4 (2026-06-30) verified `EmbeddingClient` still dispatches via `IsGoogle`/`IsOllama` and parses only those two providers; this is safe while the registry holds two providers and stays accepted until a third provider is added or an operator reports a failed parse for a registered pair. It is the strongest implement-now cluster with `15.2-RV5` and `15.2-RV6` (runtime dispatch, persisted identifier casing, migration target selection) and must be solved together, not in isolation. Natural future home: a provider-runtime dispatch abstraction story for a third provider. Owner: Server / ingestion maintainer.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:79-123

### DW-52: 15.2-RV8: accepted. Already-persisted invalid configs not surfaced on read.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs
reason: - **15.2-RV8 - accepted.** Already-persisted invalid configs not surfaced on read. - ID: 15.2-RV8 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs - Re-open trigger: Operator needs visibility into tenants whose persisted config no longer validates under the closed registry. - Rationale: Closed-registry validation runs on write only — tenants whose state was valid under loose rules continue to be served. Story 15.2 documents this as intentional compatibility behavior; operator-visibility design is a follow-up. Story 19.4 (2026-06-30) accepts this until operator visibility for already-persisted invalid configs is explicitly needed; per the scope boundary, no read-time tenant-config rejection is added without an approved operator remediation path. Natural future home: an operator visibility/remediation story for persisted invalid configs. Owner: Server / ingestion maintainer.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:103-116

### DW-53: 15.2-RV9: accepted. "Order-sensitive metric test passed in isolation" acknowledged.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests (test ordering)
reason: - **15.2-RV9 - accepted.** "Order-sensitive metric test passed in isolation" acknowledged. - ID: 15.2-RV9 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests (test ordering) - Re-open trigger: The order-sensitive metric test fails intermittently in CI, OR a test-isolation sweep is scheduled. - Rationale: Acknowledged in the Dev Agent Record but not fixed; not caused by this story. Story 19.4 (2026-06-30) accepts this until the order-sensitive metric test flakes in CI or a test-isolation sweep is scheduled. Natural future home: a test-isolation sweep if the metric test flakes. Owner: Server test maintainer.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-54: S11-FC: resolved. Release execution now has a repository-owned stale-tag

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **S11-FC - resolved.** Release execution now has a repository-owned stale-tag preflight before `npx semantic-release`. The script obtains the next version from semantic-release dry-run output, applies `.releaserc.json` `tagFormat: "v${version}"`, and checks exact local and remote refs before prepare or publish hooks can run. - ID: S11-FC - Status: resolved - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: semantic-release output changes so `tools/release-preflight.ps1` can no longer parse the next release version, or a release post-mortem shows a stale tag reached the publish-capable `npx semantic-release` step. - Evidence: Story 15.1 added `tools/release-preflight.ps1`, wired `.github/workflows/release.yml` to run it before `npx semantic-release`, and added `tests/tooling/release_preflight/release_preflight_test.py` coverage for no tag, local-only collision, remote-only collision, matching local/remote collision path, no-release dry-run output, and similarly prefixed non-colliding refs.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 3445e66b

### DW-55: 12.1-RV3: accepted. The repository removed its partial job-level

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV3 - accepted.** The repository removed its partial job-level `github.event.head_commit.message` skip parser and documents GitHub's native push skip handling as the release contract. The remaining edge is accepted because a workflow skipped by GitHub before job creation cannot run an in-workflow repository validator. - ID: 12.1-RV3 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: docs/dev/release-runbook.md - Re-open trigger: first silently skipped release caused by a bracketed skip instruction in a release-eligible merge/squash commit message, or GitHub exposes a pre-job policy hook that can reject such commits before native skip handling suppresses the workflow. - Rationale: Release maintainer ownership remains with the final merge/squash message author and reviewer. Story 15.1 makes the outcome predictable by removing the repository's partial parser, adding `CiTestInventoryTests.ReleaseWorkflow_ReleaseJob_DoesNotUseHeadCommitSkipCondition`, and documenting that bracketed skip instructions anywhere in the final commit message can suppress release. Accepted until 2026-08-13 unless the re-open trigger fires sooner.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Renew accepted risk — Re-accept the limitation with an owner, review control, and new review date.
resolution: closed by human decision: Re-accept the limitation with an owner, review control, and new review date.
decision: 2026-09-01 Renew accepted risk — Re-accept the limitation with an owner, review control, and new review date.

### DW-56: 12.1-RV4: resolved. The release restore contract is now explicitly

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: package-lock.json
reason: - **12.1-RV4 - resolved.** The release restore contract is now explicitly verified: `package-lock.json` is tracked, matches root `package.json` for `npm ci`, and the workflow installs release tooling through `npm ci`. - ID: 12.1-RV4 - Status: resolved - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: package-lock.json - Re-open trigger: `npm ci --ignore-scripts` fails from an isolated checkout/worktree, `package-lock.json` is removed from git tracking, or `.github/workflows/release.yml` stops using `npm ci` for release tooling restore. - Evidence: Story 15.1 confirmed `git ls-files -- package-lock.json package.json` lists both files, added `CiTestInventoryTests.ReleaseWorkflow_InstallReleaseTooling_UsesNpmCi`, documented the `npm ci` lockfile contract in `docs/dev/release-runbook.md`, and validated the fresh-clone-style restore with `npm ci --ignore-scripts` in an isolated worktree. Fresh-clone proof: `npm ci --ignore-scripts` run in working directory `D:\Hexalith.Memories` after deleting any pre-existing `node_modules/`; the command resolved the tracked `package-lock.json` against `package.json` without writing back to either file; post-run `git status -- package-lock.json package.json` reported zero changes.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: .github/workflows/release.yml:83

### DW-59: 15.1-RV3: accepted. Final `catch` block loses inner-exception and stack trace.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV3 - accepted.** Final `catch` block loses inner-exception and stack trace. - ID: 15.1-RV3 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release failure investigation requires the original stack/inner exception and the operator only has the truncated `Write-Error -Message $_.Exception.Message` output. - Rationale: `Write-Error -Message $_.Exception.Message` discards inner exception and stack. Switch to `Write-Error -ErrorRecord $_` or `$_.Exception.ToString()` when a future release post-mortem proves the loss matters. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: Final failure reporting retains Exception.ToString() (including inner causes) and the original PowerShell ScriptStackTrace. Contextual ref/repository diagnostics use bare rethrow rather than replacing the ErrorRecord. A nested-exception fixture proves both original causes and Invoke-GitCommand stack context survive. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' passed 36/36 methods (120.463s, exit 0); the subsequently strengthened diagnostic assertions also passed in the focused method (10.375s, exit 0). See spec-five-deferred-release-preflight.md. Reopen if this preflight behavior or its regression coverage is lost.

### DW-61: 15.1-RV5: accepted. Windows tempdir cleanup can raise `PermissionError`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV5 - accepted.** Windows tempdir cleanup can raise `PermissionError`. - ID: 15.1-RV5 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: CI or a Windows developer hits intermittent `PermissionError` on tempdir cleanup because git keeps an index lock open when the test ends. - Rationale: Tests currently pass locally and in CI. Switching to `tempfile.TemporaryDirectory(ignore_cleanup_errors=True)` (Py 3.10+) is a one-line hardening but Story 15.1 has no evidence the path manifests yet. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: `spec-five-deferred-preflight-harness.md` — All release-preflight fixtures now use bounded permission-error cleanup retries (three attempts with 50/100 ms delays); persistent and unrelated errors still fail. Focused regressions prove transient recovery removes the directory, persistent exhaustion propagates, unrelated failures are not retried and test-body exceptions survive successful cleanup. Reopen if a real Windows file-handle failure outlasts this bound or cleanup can leave resources without a failing run.

### DW-62: 15.1-RV6: accepted. `Path | None` union syntax requires Python 3.10+.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV6 - accepted.** `Path | None` union syntax requires Python 3.10+. - ID: 15.1-RV6 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A contributor runs the test on Python 3.9 (or the project lowers its minimum) and gets a `TypeError` on test collection. - Rationale: Current CI runs Python 3.11+. Lowering to `Optional[Path]` would broaden compatibility but is not needed today. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: `spec-five-deferred-preflight-harness.md` — Replace evaluated Python 3.10 union annotations with Optional for _git and _run_preflight. CPython 3.9.25 successfully imports the actual module and resolves all four helper signatures with typing.get_type_hints; all nine final helper regressions also pass on that interpreter. This qualifies this harness only. Reopen if module collection or annotation evaluation again needs a newer interpreter than the documented Python 3.9 minimum.

### DW-63: 15.1-RV7: accepted. Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV7 - accepted.** Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output. - ID: 15.1-RV7 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A test runner uses a non-UTF-8 codepage and `pwsh` stderr contains non-ASCII characters, raising `UnicodeDecodeError` on `subprocess.run(..., text=True)`. - Rationale: Pass `encoding='utf-8', errors='replace'` to `subprocess.run`. Deferred as low-impact hardening; CI codepages are UTF-8. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: `spec-five-deferred-preflight-harness.md` — All three subprocess capture paths explicitly decode UTF-8 with errors=replace. A real child process emits UTF-8 café and malformed 0xff on both output streams through each helper; the regression preserves Unicode, replaces malformed bytes and retains the exit code. The same regression passes on CPython 3.9.25 and 3.14.4. Reopen if another capture path relies on the ambient codepage or raises a decoding error.

### DW-64: 15.1-RV8: accepted. `git init` default branch depends on host `init.defaultBranch`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV8 - accepted.** `git init` default branch depends on host `init.defaultBranch`. - ID: 15.1-RV8 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A future test that relies on the default branch name (rather than just tags) fails on a runner with a non-standard `init.defaultBranch` value. - Rationale: Current tests only push tags, so the default-branch name is irrelevant. Adding `--initial-branch=main` would future-proof the helper. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: `spec-five-deferred-preflight-harness.md` — Working fixtures and the shared bare-fixture helper explicitly pass --initial-branch=main. An isolated real Git global configuration proves ordinary init chooses ambient-topic, then verifies both fixture kinds select main. Reopen if a new fixture bypasses these helpers or inherits host init.defaultBranch.

### DW-65: 15.1-RV9: accepted. Test runner hardcodes `pwsh` without availability guard.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV9 - accepted.** Test runner hardcodes `pwsh` without availability guard. - ID: 15.1-RV9 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A non-Windows developer environment without PowerShell 7 runs `pytest`/`unittest discover` and sees a confusing `FileNotFoundError` instead of a clear skip. - Rationale: Add `@unittest.skipUnless(shutil.which('pwsh'), 'pwsh required')`. Deferred because CI and dev environments today all have pwsh. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: `spec-five-deferred-preflight-harness.md` — Resolve pwsh with shutil.which and mark the script execution class with an explicit PowerShell 7 prerequisite skip. The isolated no-pwsh discovery regression proves one recorded skip and no fixture creation/errors/failures; helper regressions remain runnable. With pwsh installed the existing execution suite remains enabled. Reopen if absent PowerShell causes FileNotFoundError or skips are presented as execution evidence.

### DW-68: 15.1-RV12: accepted. `12.1-RV3` accepted-until 2026-08-13 has no automated reminder.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: - **15.1-RV12 - accepted.** `12.1-RV3` accepted-until 2026-08-13 has no automated reminder. - ID: 15.1-RV12 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md - Re-open trigger: Accepted-until date `2026-08-13` passes with no review surfacing the expired entry. - Rationale: No infrastructure today surfaces expired `accepted` entries. A scheduled check would close the gap. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commit fa1ecb52 records the renewed accepted-risk decision and closes the reminder target.

### DW-69: 15.1-RV13: accepted. `git show-ref --verify` allowed exit codes do not cover `128`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV13 - accepted.** `git show-ref --verify` allowed exit codes do not cover `128`. - ID: 15.1-RV13 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release attempt fails with the generic "git failed with exit code 128" wrapper because the ref store is corrupt or otherwise unreadable. - Rationale: A clearer "ref-state probe failed" diagnostic would shorten release-day investigation. Current wrapper is acceptable for the common case. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: Unexpected local show-ref failures remain fatal and now identify the queried tag ref with repository-ref recovery direction before bare rethrow preserves original Git details. The exit-128 fixture configures origin and proves the remote sentinel is never reached; 128 is not treated as missing-tag success. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' passed 36/36 methods (120.463s, exit 0); the subsequently strengthened diagnostic assertions also passed in the focused method (10.375s, exit 0). See spec-five-deferred-release-preflight.md. Reopen if this preflight behavior or its regression coverage is lost.

### DW-70: 15.1-RV14: accepted. Peeled-only-ref remote response not fixtured.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV14 - accepted.** Peeled-only-ref remote response not fixtured. - ID: 15.1-RV14 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A real remote returns only peeled refs (`refs/tags/vX.Y.Z^{}`) without the unpeeled entry and the contract is not test-fixtured. - Rationale: The script accepts either, but no fixture proves the peeled-only path. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: Retained a controlled peeled-only remote response regression: refs/tags/v1.2.3^{} rejects the requested tag while refs/tags/v1.2.30^{} does not collide. Both cases prove the ls-remote fixture executes; existing correct exact-ref production behavior is preserved. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' passed 36/36 methods (120.463s, exit 0); the subsequently strengthened diagnostic assertions also passed in the focused method (10.375s, exit 0). See spec-five-deferred-release-preflight.md. Reopen if this preflight behavior or its regression coverage is lost.

### DW-71: 15.1-RV15: accepted. `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV15 - accepted.** `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing. - ID: 15.1-RV15 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A caller invokes the script with a stale or wrong `-RepositoryPath` and gets a generic `Cannot find path` error instead of an actionable message. - Rationale: Pre-check with `Test-Path -PathType Container` and throw a script-owned message. Minor UX improvement. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: done 2026-10-28
archived: 2026-11-23
resolution: Missing paths, files and existing non-Git directories now receive actionable RepositoryPath checkout-directory guidance, including the supplied path. Original Git failure details and stack survive for the existing-directory case. The three-case regression rejects each invalid input. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' passed 36/36 methods (120.463s, exit 0); the subsequently strengthened diagnostic assertions also passed in the focused method (10.375s, exit 0). See spec-five-deferred-release-preflight.md. Reopen if this preflight behavior or its regression coverage is lost.

### DW-73: 13.7-RV4: resolved. The AppHost and Aspire integration fixture now share the

origin: migrated from legacy ledger ("Closed by: Deferred Work 13.7-RV4 Repository Root Locator Consolidation (2026-05-12)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/RepositoryRootLocator.cs
reason: - **13.7-RV4 — resolved.** The AppHost and Aspire integration fixture now share the AppHost-owned `RepositoryRootLocator` helper instead of maintaining duplicate `ResolveRepositoryRoot` implementations. The helper walks upward from the current directory and `AppContext.BaseDirectory`, fails closed when `Hexalith.Memories.slnx` is not found, and has focused unit coverage for both nested-directory discovery and missing-marker failure. - ID: 13.7-RV4 - Status: resolved - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide - Target artifact: src/Hexalith.Memories.AppHost/RepositoryRootLocator.cs - Re-open trigger: a third repository-root locator is introduced outside `RepositoryRootLocator`, or either AppHost startup or Aspire integration tests drift to a different root-discovery contract. - Evidence: Deferred-work implementation on 2026-05-12 added `RepositoryRootLocator`, replaced the AppHost and fixture helper copies with calls to it, and added `RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory` plus `RepositoryRootLocator_MissingMarker_Throws`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit acfdf211

### DW-74: 12.4-RV6: resolved. `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV6 — resolved.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` no longer classifies entries by substring scans for `baseline`, `test-release.ps1`, or `release lane`. The new `ParseStructuredDeferredEntries` reader matches anchored field labels (`ID:`, `Status:`, `Source story:`, `Target artifact:`, `Re-open trigger:`, `Evidence:` / `Rationale:`, optional `Test:`), and `ReadOpenDeferredBaselines` reports only entries whose `Target artifact` references the release-lane script with `Status: open`. - ID: 12.4-RV6 - Status: resolved - Source story: 12-4-baseline-failures-sweep - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: any future change that reintroduces prose-substring classification of baseline-related deferred entries, or a parser regression where unrelated narrative mentions of `baseline` / `release lane` are once again counted as baseline filters. - Evidence: Story 14.5 replaced the substring-driven `baselineRelated` / `HasReleaseFilter` classifier with field-aware parsing; new fixture tests prove that prose mentions of `baseline`, `release lane`, and `test-release.ps1` in non-structured entries do not trigger baseline classification.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-75: 12.4-RV19: resolved. The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV19 — resolved.** The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a literal trailing period) is replaced by reading the structured `ID:` field verbatim. The new parser accepts any ID token that the schema admits and rejects near-matches such as `12x4-RV6` or `112.4-RV6` exactly because field equality is enforced after extraction. - ID: 12.4-RV19 - Status: resolved - Source story: 12-4-baseline-failures-sweep - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: a future deferred-work format change that adds new ID shapes (lowercase, em-dash, alternate suffix punctuation) without exercising them in `CiTestInventoryTests` fixtures. - Evidence: Story 14.5 deleted `DeferredKeyRegex` and now resolves IDs from the structured `ID:` field. Fixture tests cover `12.4-RV6`, `S11-FX`, lowercase / mixed-case rejection, and exact-ID boundaries against `12x4-RV6` and `112.4-RV6`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-76: 12.6-RV2: resolved. This entry explicitly realized 12.4-RV6 and is closed

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.6-RV2 — resolved.** This entry explicitly realized 12.4-RV6 and is closed by the same parser change. The unconditional `ShouldBeEmpty` assertion now rests on the structured-field reader rather than substring heuristics, so a prose-only edit to an unrelated entry (for example renaming "release pipeline" to "release lane") cannot flip an entry's classification. - ID: 12.6-RV2 - Status: resolved - Source story: 12-6-embedding-input-content-kind-baseline-resolution - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: a parser regression that reintroduces substring-based baseline classification, or a future story that adds a new release-lane filter without a paired structured deferred-work entry. - Evidence: closed alongside 12.4-RV6 by the Story 14.5 structured-field parser; new fixture test `ReadOpenDeferredBaselines_NarrativeMentionsBaseline_NotMisclassified` proves prose mentions are no longer load-bearing for classification.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-77: 13.7-RV5: resolved. Sprint-status history hygiene is now a documented

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: CONTRIBUTING.md
reason: - **13.7-RV5 — resolved.** Sprint-status history hygiene is now a documented forward-looking convention in `CONTRIBUTING.md`. Future status entries should use short dated breadcrumbs that link to the relevant story artifact, deferred entry, run log, or review document instead of accumulating multi-sentence evidence on a single YAML line. Historical Epic 1-13 history comments are intentionally not rewritten — that cleanup remains out of scope per the Story 14.5 dev notes. - ID: 13.7-RV5 - Status: resolved - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide - Target artifact: CONTRIBUTING.md - Re-open trigger: a future parser, dashboard, or auditor that fails on the long historical YAML lines and proves a targeted edit to specific entries is required, or a contributor-process change that takes ownership of bulk sprint-status history rewriting. - Evidence: Story 14.5 added the "Sprint Status History Conventions" section to `CONTRIBUTING.md`; the Epic 14 bookkeeping rules in the same file require future Epic 14 stories to point at story artifacts and deferred IDs rather than appending narratives on the YAML status line.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: CONTRIBUTING.md:201-221

### DW-78: 13.6-RV4: closed. `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV4 — closed.** `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-79: 13.6-RV5: closed. Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV5 — closed.** Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-80: 13.7-RV1: closed. `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV1 — closed.** `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-81: 13.7-RV2: closed. Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV2 — closed.** Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:144-148

### DW-82: 13.7-RV3: closed. `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV3 — closed.** `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1819

### DW-83: 13.7-RV6: closed. `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV6 — closed.** `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs:235-247

### DW-84: 13.7-RV7: closed. The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV7 — closed.** The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:35

### DW-85: 13.6-RV1: carried forward. Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV1 — carried forward.** Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-87: 13.7-RV4: resolved 2026-05-12. Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV4 — resolved 2026-05-12.** Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit acfdf211

### DW-88: 13.2-RV1: closed. `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV1 — closed.** `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-89: 13.2-RV2: closed. `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: tools/MigrateEmbeddingVectors/Program.cs
reason: - **13.2-RV2 — closed.** `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-90: 13.2-RV3: closed. `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV3 — closed.** `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-91: 13.2-RV5: closed. `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV5 — closed.** `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-92: 13.2-RV6: closed. Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV6 — closed.** Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-93: 13.3-RV6: closed. Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV6 — closed.** Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-94: 13.3-RV7: closed. `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV7 — closed.** `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-95: 13.3-RV11: closed. `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV11 — closed.** `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-96: 13.3-RV12: closed. `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV12 — closed.** `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-97: 13.3-RV14: closed. `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV14 — closed.** `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-98: 13.3-RV15: closed. `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV15 — closed.** `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-99: 13.4-RV5: closed. `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.4-RV5 — closed.** `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-100: 13.6-RV1: Concurrent ingestion racing the migration. `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV1 — Concurrent ingestion racing the migration.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-101: 13.6-RV2: Pre-existing missing copyright header. `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs
reason: - **13.6-RV2 — Pre-existing missing copyright header.** `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs:1

### DW-103: 13.6-RV4: Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs
reason: - **13.6-RV4 — Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-104: 13.6-RV5: Redactor skips `client_secret named foo` style strings without `:` or `=` separator. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs
reason: - **13.6-RV5 — Redactor skips `client_secret named foo` style strings without `:` or `=` separator.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-105: 13.5-RV1: `Hexalith.EventStore` submodule pointer bump bundled in feat commit. Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. Process note: future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: Hexalith.Commons/_bmad-output/project-context.md:99
reason: - **13.5-RV1 — `Hexalith.EventStore` submodule pointer bump bundled in feat commit.** Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. **Process note:** future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: references/Hexalith.AI.Tools/hexalith-git-instructions.md:45-46 requires content and parent-gitlink commits separately.

### DW-106: 13.5-RV2: AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline. `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130
reason: - **13.5-RV2 — AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline.** `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs:145-168

### DW-108: 13.5-RV4: Legacy `provider="ollama"` payload with missing OIDC fields not exercised. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496
reason: - **13.5-RV4 — Legacy `provider="ollama"` payload with missing OIDC fields not exercised.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.
status: done 2026-10-28
archived: 2026-11-23
resolution: Resolved by spec-five-deferred-ollama-config. Evidence: TenantConfigurationActorTests.GetEmbeddingConfigAsync_LegacyOllamaState_ShouldValidateDefaultedAuthWithoutWriting; literal legacy JSON covers missing/empty/space/tab/valid BaseUrl with both reindex flags and proves missing authentication fields default to api-key, absent BaseUrl falls back to Google, a valid BaseUrl retains Ollama metadata, and only read-only state-manager calls occur. Both affected classes executed: 231 passed, zero failures/errors/skips. Command and build evidence: _bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md. Re-open trigger: legacy authentication defaults or provider BaseUrl requirements change.

### DW-109: 13.5-RV5: Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219
reason: - **13.5-RV5 — Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.
status: done 2026-10-28
archived: 2026-11-23
resolution: Resolved by spec-five-deferred-ollama-config. Evidence: TenantConfigurationActorTests.GetEmbeddingConfigAsync_GoogleStateWithBlankBaseUrl_ShouldPreserveStateWithoutWriting and GetEmbeddingConfigAsync_OllamaStateWithBlankBaseUrl_ShouldReturnDefaultsWithoutWriting; empty, space-only and tab-only BaseUrl cases, including omitted legacy authentication fields and both preserved Google reindex flags, prove complete Google configuration preservation and Ollama fallback with only read-only state-manager calls. Both affected classes executed: 231 passed, zero failures/errors/skips. Command and build evidence: _bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md. Re-open trigger: blank URL validation or actor read fallback changes.

### DW-110: 13.5-RV6: `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381
reason: - **13.5-RV6 — `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.
status: done 2026-10-28
archived: 2026-11-23
resolution: Resolved by spec-five-deferred-ollama-config. Evidence: TenantConfigurationActorTests.SetEmbeddingConfigAsync_FirstWrite_ShouldIgnoreClientSuppliedReindexFlag and SetEmbeddingConfigAsync_FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag; all four independent client ReindexRequired/forceReindex combinations for each provider assert exactly one complete persisted custom configuration with ReindexRequired false. Both affected classes executed: 231 passed, zero failures/errors/skips. Command and build evidence: _bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md. Re-open trigger: first-write reindex semantics or persisted metadata mapping changes.

### DW-111: 13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:39-46,54-70
reason: - **13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:39-46,54-70` — 4-arg ctor delegates to 5-arg with `null`; 5-arg also has `IOidcTokenProvider? = null` default. MS DI does not honor C# default values, so the 4-arg overload is currently necessary. Remove the redundant default on the 5-arg side at next refactor. Re-open trigger: Story 13.7 wires `IOidcTokenProvider` into DI and the constructor surface is touched again.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-112: 13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:483-495
reason: - **13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:483-495` — Short tokens or short input text could mask coincidental substrings of the upstream JSON; longer tokens with overlapping substrings get clobbered. Apply a length floor and order-by-length-descending replacement. Re-open trigger: a real-world incident where a redacted exception body becomes unreadable, or a security review.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-115: 13.3-RV10: No 429/Retry-After test on the Ollama path. `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs
reason: - **13.3-RV10 — No 429/Retry-After test on the Ollama path.** `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.
status: done 2026-10-28
archived: 2026-11-23
resolution: 2026-10-28 — `spec-five-deferred-embedding-regressions.md`: GenerateAsync_Ollama_Http429_ReportsRetryAfterWithoutCredentialRefresh covers missing, 120-second, capped 7200-second, zero, malformed, past-date and far-future-date Retry-After headers through the Ollama POST /api/embed bearer dispatch. Every case verifies tenant, expected delay, exactly one HTTP request, Dapr secret read and token acquisition with no credential refresh. Reopen if Ollama transport dispatch, retry headers or refresh policy changes. Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 354 cases with zero failures/errors/skips/not-run; Debug build passed with zero warnings/errors.

### DW-116: 13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:291-297
reason: - **13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:291-297` — Accidentally-added positional argument silently becomes a "sensitive value". Replace `params` with an explicit `IReadOnlyList<string?>` for security-critical parameters. Re-open trigger: any new caller of `HandleEmbeddingResponseAsync`, or a new sensitive value to redact.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-117: 13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:285
reason: - **13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:285` — Interface does not enforce non-blank token. Current `OidcTokenProvider` validates; future provider implementation could return whitespace and crash with `FormatException`. Re-open trigger: a third-party `IOidcTokenProvider` is added, or the interface is opened to non-Hexalith implementations.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-118: 13.3-RV13: `BaseUrl` with query string or fragment silently dropped. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261
reason: - **13.3-RV13 — `BaseUrl` with query string or fragment silently dropped.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:352-395

### DW-119: 13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:216-218
reason: - **13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:216-218` — `OidcTokenAcquisitionException`, `HttpRequestException`, `TaskCanceledException` leak past the EmbeddingClient boundary. Mirrors deferred 13.2-RV3. Re-open trigger: a 401-retry production incident where typed transport errors are needed for retry classification at higher layers.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-120: 13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:201,217
reason: - **13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:201,217` — If the DAPR `client_secret` is rotated, cached secret stays in `_apiKeyCache`; bearer-token refresh hands the IdP the stale secret. Google path evicts the secret cache symmetrically (line 176); Ollama does not. AC5 does not strictly require this. Re-open trigger: a secret-rotation runbook where Ollama tenants degrade until restart.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-123: 13.4-RV3: OIDC mode does not enforce `ApiSecretKeyName` distinctness/role. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181
reason: - **13.4-RV3 — OIDC mode does not enforce `ApiSecretKeyName` distinctness/role.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Keep names opaque — Let lookup and authentication determine role and document that contract.
resolution: closed by human decision: Let lookup and authentication determine role and document that contract.
decision: 2026-09-01 Keep names opaque — Let lookup and authentication determine role and document that contract.

### DW-124: 13.4-RV4: No assertion that endpoint paths invoke `Validate`. `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Endpoints/*
reason: - **13.4-RV4 — No assertion that endpoint paths invoke `Validate`.** `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:53

### DW-125: 13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:214-226
reason: - **13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:214-226` — Mirrors deferred 13.2-RV5 in the OIDC token provider. `Uri.TryCreate` accepts userinfo and the value is preserved; backend-configured endpoints make tenant exploitation rare, but defensive rejection is cheap and uniform across providers. Re-open trigger: any tenant config audit that finds embedded credentials, or a security review that wants the rule applied uniformly across `OidcTokenProvider` + `EmbeddingProviderDefaults`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-126: 13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.** `OidcTokenProvider.cs:117,167` — the leader's `CancellationToken` is passed straight into `_httpClient.SendAsync`. AC6 narrow text only requires waiter cancellation isolation (current test `GetAccessTokenAsync_CancelledWaiter_DoesNotCancelSharedAcquisition` covers this). Dev Notes Implementation Guidance is stricter: "a single caller cancellation must cancel that caller's wait without cancelling the in-flight fetch for remaining waiters." If the leader cancels mid-fetch, queued waiters re-enter and refire. Fix requires TCS-based detached-fetch refactor or linked-CTS where the inner SendAsync uses `CancellationToken.None` and waiters await via `Task.WaitAsync(ct)`. Re-open trigger: Story 13.3 retry integration where leader-cancel under 401 retry becomes concrete; or a production incident where IdP traffic spikes correlate with caller cancellations.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-127: 13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.** `Program.cs:110-118` and `OidcTokenProvider.cs:34-42` — the named HttpClient is resolved once at singleton activation and stored for the service lifetime. DNS changes, TLS session rotation, and `SocketsHttpHandler.PooledConnectionLifetime` rotation never apply. The same caveat exists for `EmbeddingClient` registration (line 108). Fix options: inject `IHttpClientFactory` and `CreateClient(name)` per call, or convert to typed-HttpClient + scoped lifetime. Re-open trigger: an ops incident traceable to stale TLS/DNS, or an ecosystem-wide pass to standardize HttpClient lifecycle across `EmbeddingClient` + `OidcTokenProvider`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-128: 13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.** `OidcTokenProvider.cs:167` — `HttpRequestException`, `TaskCanceledException` (timeout), `IOException` from `SendAsync` propagate raw. AC7 only requires wrapping non-2xx responses, but Story 13.3's 401-retry will distinguish recoverable vs terminal failures. Re-open trigger: Story 13.3 surfaces a need for typed transport errors during retry classification.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-129: 13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).** `OidcTokenProvider.cs:80` — `uri.Scheme is not "https" and not "http"` is the only scheme guard. Dev/local Keycloak needs `http://localhost`; production must be constrained at the operations/config layer. Re-open trigger: Story 13.7 operations docs / production hardening pass.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e68cd2e4

### DW-130: 13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.** `OidcTokenProvider.cs:79-89` — `Uri.TryCreate` accepts userinfo and it is preserved through `UriComponents.SchemeAndServer`. Backend-configured endpoints make this rare; defensive rejection is still cheap. Re-open trigger: any tenant config audit that finds embedded userinfo, or a security review.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-131: 13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.** `OidcTokenProvider.cs:65,116-119` — two concurrent forced-refresh callers both skip the cache double-check inside the guard and each issue a fresh HTTP fetch. AC5 is silent on concurrent forced refresh; AC6's "exactly one outbound HTTP request" applies to cache-miss collapse, not invalidation. Re-open trigger: a 401 storm during ingestion that hammers Keycloak via simultaneous retry-after-401 paths in Story 13.3.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 90bf620a

### DW-134: 13.2-RV9: `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot. `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412
reason: - **13.2-RV9 — `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot.** `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:817-821

### DW-135: 13.1-RV1: `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body. Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272
reason: - **13.1-RV1 — `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body.** Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)
status: done 2026-10-28
archived: 2026-11-23
resolution: 2026-10-28 — `spec-five-deferred-embedding-regressions.md`: Replaced the misleading Google-above-Ollama-ceiling test with Validate_RateAboveGoogleCeilingWithinOllamaCeiling_AppliesProviderLimit. Rates 3001, 5000 and 60000 all reject Google specifically for RateLimitPerMinute and accept Ollama. Reopen if provider ceilings or rate-validation ordering changes. Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 354 cases with zero failures/errors/skips/not-run; Debug build passed with zero warnings/errors.

### DW-136: 13.1-RV2: `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects". Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307
reason: - **13.1-RV2 — `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects".** Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)
status: done 2026-10-28
archived: 2026-11-23
resolution: Resolved by spec-five-deferred-ollama-config. Evidence: EmbeddingProviderDefaultsTests.Validate_OllamaQwen3_RejectsAnyDimensionExcept2560 and Validate_OllamaQwen3_AcceptsExactly2560; the renamed negative theory asserts Dimensions errors for five unsupported widths and a dedicated positive test explicitly validates 2560. Both affected classes executed: 231 passed, zero failures/errors/skips. Command and build evidence: _bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md. Re-open trigger: the registered Ollama model or supported dimensions change.

### DW-137: 13.1-RV3: `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google. Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284
reason: - **13.1-RV3 — `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google.** Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)
status: done 2026-10-28
archived: 2026-11-23
resolution: Resolved by spec-five-deferred-ollama-config. Evidence: EmbeddingProviderDefaultsTests.Validate_OllamaQwen3WithGoogleDimensions_ShouldThrowDimensionsError and Validate_CrossProviderModelPairs_ShouldThrow; the corrected name identifies qwen3 with unsupported 768 dimensions, asserts ParamName Dimensions, and the distinct cross-provider tests assert ParamName Model. Both affected classes executed: 231 passed, zero failures/errors/skips. Command and build evidence: _bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md. Re-open trigger: model/provider compatibility or validation error attribution changes.

### DW-139: 13.1-RV5: Per-provider rate-limit ternary fragile for future providers. `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145
reason: - **13.1-RV5 — Per-provider rate-limit ternary fragile for future providers.** `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:46-70

### DW-140: 13.1-RV6: `Dimensions = int.MaxValue` accepted. Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117
reason: - **13.1-RV6 — `Dimensions = int.MaxValue` accepted.** Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:239-247

### DW-141: 13.1-RV7: `GetBreakingChangeFields` case-sensitivity contract not pinned by tests. Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67
reason: - **13.1-RV7 — `GetBreakingChangeFields` case-sensitivity contract not pinned by tests.** Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:785-805

### DW-142: 13.1-RV8: No null-config test for `Validate(null!)`. `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs
reason: - **13.1-RV8 — No null-config test for `Validate(null!)`.** `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:170

### DW-145: 13.1-RV11: Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4). The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. Action when 13.4 lands: (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. Re-open trigger if 13.4 ships without: any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167
reason: - **13.1-RV11 — Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4).** The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. **Action when 13.4 lands:** (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. **Re-open trigger if 13.4 ships without:** any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:219-259

### DW-147: 12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377
reason: - **12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.** `ParseDeferredBaseline` classifies an entry as "baseline-related" via case-insensitive substring of `baseline` or `test-release.ps1`. A pure-prose deferred-work edit (e.g., S11-FD "release pipeline" → "release lane") could flip an entry's classification and break the inventory test with no functional change. Migrate to a structured classifier (e.g., explicit `Filter:` field per entry) before the surface grows. Realizes the 12.4-RV6 concern. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1094-1110

### DW-150: 12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs
reason: - **12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filtered captures by neither tenant nor instrument-source, while its sibling `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` used unique tenant id + `ShouldHaveSingleItem`. The flake mode that originally motivated S11-FA was dormant, not eliminated, and could re-trip under heavier xunit parallelism or any other test that emits on the static `MemoriesMeter.EmbeddingApiCalls` counter. Resolved by the 2026-05-19 deferred-work implementation: the affected telemetry tests now use unique tenant ids, tenant-filtered metric capture, `ConcurrentQueue`, and single-event assertions. Re-open trigger: any flake reappearance on `EmbeddingInputContentKindTests`, or before any future story that adds a third concurrent `EmbeddingApiCalls` test path. (`tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-152: 12.5-RV2: `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`. `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92
reason: - **12.5-RV2 — `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`.** `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.
status: done 2026-10-28
archived: 2026-11-23
resolution: `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md` — Quoted in:title search with an explicit limit of 100 retains exact-title verification; IssueHelperReliabilityTests.test_lookup_uses_quoted_title_and_explicit_limit_and_exact_match proves a match beyond the former 30-result default and ignores near matches. Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods (2026-10-28).
reopen-trigger: The corresponding retained fake-GitHub CLI regression fails, or live alert evidence contradicts the documented behavior.

### DW-154: 12.5-RV4: `Format-ListSection` does not skip `$null` items. `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:52-67
reason: - **12.5-RV4 — `Format-ListSection` does not skip `$null` items.** `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.
status: done 2026-10-28
archived: 2026-11-23
resolution: `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md` — Format-ListSection filters null members before counting/rendering; IssueHelperReliabilityTests.test_null_artifacts_are_omitted_and_all_null_lists_render_none proves mixed lists omit null bullets and all-null/missing lists say None. Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods (2026-10-28).
reopen-trigger: The corresponding retained fake-GitHub CLI regression fails, or live alert evidence contradicts the documented behavior.

### DW-155: 12.5-RV5: No retry/backoff for transient `gh` failures in the alert path. `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92-128
reason: - **12.5-RV5 — No retry/backoff for transient `gh` failures in the alert path.** `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.
status: done 2026-10-28
archived: 2026-11-23
resolution: `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md` — Three-attempt transient GitHub retries use visible one-/two-second transport backoff, rate-limit-specific pacing within a shared 180-second wait budget, retained redacted diagnostics, immediate permanent-error refusal and validated issue/comment readback before repeating an ambiguous write, preserving the current evidence when readback finds an older incident. Subprocess fixtures prove recovery, exhausted query/write budgets, applied writes with lost responses, and refusal after failed or invalid readback. Independent concurrent runs remain the existing DW-153 scope. Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods (2026-10-28).
reopen-trigger: The corresponding retained fake-GitHub CLI regression fails, or live alert evidence contradicts the documented behavior.

### DW-156: 12.5-RV6: Malformed `publish-summary.json` makes the alert step throw. `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:32
reason: - **12.5-RV6 — Malformed `publish-summary.json` makes the alert step throw.** `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.
status: done 2026-10-28
archived: 2026-11-23
resolution: `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md` — Summary parsing fails with a path-specific, actionable preserve-evidence diagnostic before GitHub invocation, without echoing malformed JSON; IssueHelperReliabilityTests.test_malformed_summary_fails_with_context_before_github proves this refusal. Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods (2026-10-28).
reopen-trigger: The corresponding retained fake-GitHub CLI regression fails, or live alert evidence contradicts the documented behavior.

### DW-157: 12.5-RV7: Empty stdout from `gh issue list` trips `ConvertFrom-Json`. `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:97
reason: - **12.5-RV7 — Empty stdout from `gh issue list` trips `ConvertFrom-Json`.** `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.
status: done 2026-10-28
archived: 2026-11-23
resolution: `_bmad-output/implementation-artifacts/spec-five-deferred-release-alerts.md` — Blank and whitespace lookup stdout explicitly normalize to an empty issue collection; IssueHelperReliabilityTests.test_empty_lookup_output_is_an_empty_collection proves incident creation. The historical crash did not reproduce on installed PowerShell (the previous null sentinel was filtered), so this closes explicit handling and compatibility coverage, not a reproduced crash. Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/publish_nuget -p '*_test.py' -v` passed 30/30 methods (2026-10-28).
reopen-trigger: The corresponding retained fake-GitHub CLI regression fails, or live alert evidence contradicts the documented behavior.

### DW-159: 12.3-RV1: CI duplicate runs on `pull_request` and `push`. Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:3-9
reason: - **12.3-RV1 — CI duplicate runs on `pull_request` and `push`.** Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion. [.github/workflows/ci.yml:3-9]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 6ef83aa6

### DW-161: 12.3-RV3: CI brand-new branch first push enumerates entire head commit. `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:51-52
reason: - **12.3-RV3 — CI brand-new branch first push enumerates entire head commit.** `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable. [.github/workflows/ci.yml:51-52]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: .github/workflows/ci.yml:95-130

### DW-164: 12.3-RV6: `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages. Edge case; clean error wrapping is a follow-up nicety.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:114-118
reason: - **12.3-RV6 — `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages.** Edge case; clean error wrapping is a follow-up nicety. [tools/check-story-file-scope.py:114-118]
status: done 2026-10-28
archived: 2026-11-23
resolution: UTF-8 input decoding failures now become actionable ValidationError diagnostics naming the commit-message file, with exit 1 and no traceback; missing input files also receive clean errors. Reopen if malformed commit-message bytes bypass validation or expose a traceback. Evidence: spec-five-deferred-story-scope.md and tests/tooling/story_scope/story_scope_validator_test.py; PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -q passed 71/71 methods on 2026-10-28.

### DW-165: 12.3-RV7: `--changed-files-file` does not strip a UTF-8 BOM. A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:246
reason: - **12.3-RV7 — `--changed-files-file` does not strip a UTF-8 BOM.** A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited. [tools/check-story-file-scope.py:246]
status: done 2026-10-28
archived: 2026-11-23
resolution: UTF-8-sig decoding removes an optional leading BOM from changed-files input before normalizing paths. BOM and BOM-free fixtures prove both first-path authorization and forbidden-default classification, including remaining list entries. Reopen if BOM-prefixed input changes scope decisions. Evidence: spec-five-deferred-story-scope.md and tests/tooling/story_scope/story_scope_validator_test.py; PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -q passed 71/71 methods on 2026-10-28.

### DW-167: 12.3-RV9: Pre-commit fails closed during rebase / cherry-pick / detached-HEAD. `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:7
reason: - **12.3-RV9 — Pre-commit fails closed during rebase / cherry-pick / detached-HEAD.** `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching. [.githooks/pre-commit:7]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tools/check-story-file-scope.py:475-486

### DW-170: 12.3-RV12: `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check. Backslashes get normalized away before the test. Pair with override-vagueness rework P6.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:286-288
reason: - **12.3-RV12 — `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check.** Backslashes get normalized away before the test. Pair with override-vagueness rework P6. [tools/check-story-file-scope.py:286-288]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tools/check-story-file-scope.py:442-449

### DW-171: 12.3-RV13: `parse_allowed_scope` does not honor `## ` subheadings inside `## File Scope`. No current story uses this shape.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:206-211
reason: - **12.3-RV13 — `parse_allowed_scope` does not honor `## ` subheadings inside `## File Scope`.** No current story uses this shape. [tools/check-story-file-scope.py:206-211]
status: done 2026-10-28
archived: 2026-11-23
resolution: Proper ATX Markdown subsections at heading levels three through six now recognize allowed/read-only/forbidden labels and reset collection at unrelated headings. Sibling level-two and parent headings end File Scope. The historical wording calling a level-two heading nested was corrected in the build trace. Reopen if subsection content leaks into writable scope or explicit allowed lists cannot resume. Evidence: spec-five-deferred-story-scope.md and tests/tooling/story_scope/story_scope_validator_test.py; PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_scope -p '*_test.py' -q passed 71/71 methods on 2026-10-28.

### DW-173: 12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:217-223
reason: - **12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.** No current story uses this shape. [tools/check-story-file-scope.py:217-223]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-174: W10: closed. AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.

origin: migrated from legacy ledger ("Closed by: course correction (2026-04-26)"), 2026-09-01
location: docs/dev/branch-protection.md
reason: - **W10 — closed.** AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md:4

### DW-175: S11-FA: closed. `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.

origin: migrated from legacy ledger ("Closed by: Story 12.6 EmbeddingInputContentKind Baseline Resolution (2026-05-02)"), 2026-09-01
location: tools/test-release.ps1
reason: - **S11-FA — closed.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-176: Story-10.2-TokenBudgetServerTruncation: closed. MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-TokenBudgetServerTruncation — closed.** MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/SearchResult.cs:33-43

### DW-177: Story-10.2-DegradedStateAnnotations: closed for MCP ingress. Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-DegradedStateAnnotations — closed for MCP ingress.** Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/SearchResult.cs:45-65

### DW-178: Story-10.2-IngressAuthentication-NFR11: closed for MCP ingress. `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-IngressAuthentication-NFR11 — closed for MCP ingress.** `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Mcp/Program.cs:20-23

### DW-184: Story-10.x-RetroLessonForwardReferenceGuards (A16). Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-RetroLessonForwardReferenceGuards (A16).** Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/epic-10-retro-2026-04-30.md:47 records the forward-reference lifecycle lesson.

### DW-186: Story-10.x-StatelessModeAuditFor10.2Auth: closed for bearer auth. Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-StatelessModeAuditFor10.2Auth — closed for bearer auth.** Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:82

### DW-190: Story-10.x-OpenTelemetryAspNetCoreAlignment. Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. Re-open trigger: AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-OpenTelemetryAspNetCoreAlignment.** Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. **Re-open trigger:** AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:266-270 aligns the OpenTelemetry packages at 1.18.0.

### DW-191: Story-9.3-MemoriesServerAuthN: resolved by Story 20.1. Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Authentication/*
reason: - **Story-9.3-MemoriesServerAuthN — resolved by Story 20.1.** Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit b48a519b

### DW-192: D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs
reason: - **D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.** Memories Server now normalizes authenticated principal tenant claims into a server-owned `memories:tenant` claim, rejects well-formed cross-tenant `/api/tenants/{tenantId}/**`, `/api/search?tenantId=...`, and ingest scheduling requests before endpoint business logic/backend access, and derives audit user identity from the authenticated principal instead of `x-user-id` or request-body attribution fields. Evidence: `src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs`, `src/Hexalith.Memories.Server/Program.cs`, `tests/Hexalith.Memories.Server.Tests/Authentication/*`, and `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs`. Residual tenantless workflow/batch status scoping remains Story 20.3 scope, not a duplicate D8/A2 entry.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit ae9558fe

### DW-194: Story-9.3-ProjectionRegistryCrossCheck. The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. Re-open trigger: operator-driven demand for this gap to be closed.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ProjectionRegistryCrossCheck.** The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. **Re-open trigger:** operator-driven demand for this gap to be closed.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs:178-203

### DW-198: Story-9.3-SubscriptionStatusConfigured. 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. Re-open trigger: operator feedback post-landing indicates the 3-state model is ambiguous.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-SubscriptionStatusConfigured.** 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. **Re-open trigger:** operator feedback post-landing indicates the 3-state model is ambiguous.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Keep three states — Retain the stable enum until concrete ambiguity justifies a break.
resolution: closed by human decision: Retain the stable enum until concrete ambiguity justifies a break.
decision: 2026-09-01 Keep three states — Retain the stable enum until concrete ambiguity justifies a break.

### DW-216: DataContract/DataMember attributes missing on V1 contracts

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **DataContract/DataMember attributes missing on V1 contracts** — Systematic gap across all V1 contracts (ExtractionInput, ExtractionResult, MemoryUnit, GraphEdge, etc.). None use DataContract/DataMember/JsonPropertyOrder/JsonConstructor attributes per project-context.md rules. Should be addressed as a batch across all V1 types.
status: done 2026-09-06
archived: 2026-11-23
resolution: closed by human decision: Document System.Text.Json as authoritative.
decision: 2026-09-06 Retain System.Text.Json — Document System.Text.Json as authoritative.

### DW-218: Large byte[] in ExtractionInput persisted to workflow state store

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **Large byte[] in ExtractionInput persisted to workflow state store** — DAPR Workflow serializes activity inputs to state store. For 1MB files, this means ~1.33MB base64 per workflow instance. Accepted per D13 (MVP payloads ≤1MB). Future work: consider streaming or external storage for larger payloads.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 906f819f

### DW-220: End-to-end embedding flow is not wired into orchestration

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **End-to-end embedding flow is not wired into orchestration** — Deferred because orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:153-187

### DW-221: Rate-limiting scope conflicts with credential scope

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Rate-limiting scope conflicts with credential scope** — Deferred because Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:82-107

### DW-223: Story status requires manual dual-write across tracking files

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Story status requires manual dual-write across tracking files** — The workflow duplicates status in both the story artifact and `sprint-status.yaml`, which is a pre-existing consistency risk whenever one file changes without the other.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tools/check-story-review-readiness.py:803-817

### DW-226: ContentBytes serialized inline in DAPR workflow state

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **ContentBytes serialized inline in DAPR workflow state** — Base64-encoded byte[] in IngestionInput causes replay amplification for large files. Accepted per D13 (MVP payloads <= 1MB). Same issue as Story 1.3 ExtractionInput. Future: external blob storage for content.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 906f819f

### DW-229: CaseId not validated for special characters

origin: migrated from legacy ledger ("Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **CaseId not validated for special characters** — `TenantId` has a strict alphanumeric+hyphen regex via `TenantIdGuard.Validate`, but `CaseId` only checks for null/empty. Not spec-required; CaseId is used as hash field values (not key names or graph names), so the blast radius is limited to potential key scan interference.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Cases/CaseValidator.cs:161-168 enforces a safe CaseId.

### DW-231: InternalsVisibleTo in packable library without strong-name key

origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
location: n/a
reason: - **InternalsVisibleTo in packable library without strong-name key** — `Hexalith.Memories.Redis.csproj` has `IsPackable=true` and `InternalsVisibleTo` for Benchmarks without a strong-name key. Any consumer assembly named `Hexalith.Memories.Benchmarks` could access internals. Pre-existing pattern across the project; low practical risk.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 01b8cad6

### DW-232: FusionEngine non-finite handling asymmetry across axes

origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
location: n/a
reason: - **FusionEngine non-finite handling asymmetry across axes** — Graph axis skips non-finite scores entirely (`continue` in FusionEngine), while syntactic/semantic axes normalize non-finite to 0.0 via ScoreNormalizer. Both paths produce safe results, but the mechanism differs: a document with a NaN graph-only score is excluded from fusion, while a NaN syntactic-only score becomes 0.0 and is included. Defensible design — graph scores bypass normalizer.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 14c19428

### DW-233: Case creation is non-atomic across Redis and FalkorDB

origin: migrated from legacy ledger ("Deferred from: code review of 3-1-create-and-list-cases (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. The story already records this as an accepted MVP gap, so it remains deferred for now.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit c350b7ab

### DW-234: Case creation is non-atomic across Redis and FalkorDB

origin: migrated from legacy ledger ("Deferred from: code review of 3-2-case-status-and-activity.md (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` still writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. This remains a pre-existing MVP gap from Story 3.1 rather than a regression introduced by Story 3.2.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit c350b7ab

### DW-235: Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key

origin: migrated from legacy ledger ("Deferred from: 3-3-case-member-management (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key** — Story 3.3 introduces a `:members` Redis Hash key per case for member storage. When Story 3.5 implements case deletion, it must also delete this key alongside the case hash and `:activity` stream to avoid orphaned data.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs:46-47

### DW-238: Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade** — Story 3.5's `DETACH DELETE` removes `annotates` edges but leaves connected annotation MU nodes intact. When Story 3.6 implements annotations, `DeleteMemoryUnitAsync` must first traverse outgoing `annotates` edges, recursively delete annotation MUs, then delete the target MU.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs:31-33

### DW-241: No input validation on caseId format before Redis key construction

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **No input validation on caseId format before Redis key construction** — `caseId` undergoes no format validation (unlike `tenantId` which has `TenantIdGuard`). A caseId containing `:` is used directly in Redis key patterns. Defense-in-depth gap, though read-only lookups limit blast radius. [Program.cs:472]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Cases/CaseValidator.cs:161

### DW-243: Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Program.cs:1829
reason: - **Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass** — Deferred by review decision. Reason: state-store key format is not empirically verified yet, so the actor fallback is the safer MVP path for now. [src/Hexalith.Memories.Server/Program.cs:1829]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/TenantEndpointHandlers.cs:76

### DW-245: Per-run Docker named volumes are never torn down

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs:175-181
reason: - **Per-run Docker named volumes are never torn down** — Fixture generates `hexalith-memories-it-<guid>` volumes for test isolation but nothing cleans them up. CI hosts accumulate disk usage over time. [src/Hexalith.Memories.AppHost/Program.cs:175-181]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1292

### DW-252: `IOutputFormatter<T>.Write` signature has no `CancellationToken`

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs
reason: - **`IOutputFormatter<T>.Write` signature has no `CancellationToken`** — Broken downstream pipe (`memories … | head -1` on a large body) surfaces as "Unexpected error contacting Memories Server" rather than a clean broken-pipe exit; Ctrl+C during a synchronous write has no effect. Signature change is architectural and out-of-scope for 7.2. [src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs]
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Retain bounded sync writes — The bounded payloads do not justify broad API churn.
resolution: closed by human decision: The bounded payloads do not justify broad API churn.
decision: 2026-09-01 Retain bounded sync writes — The bounded payloads do not justify broad API churn.

### DW-256: `IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401
reason: - **`IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`** — spec line 168 allowed "or equivalent — grep first" flexibility, but the signature divergence cascades into validation step's inability to match by `MemoryUnitId`. To be reconsidered during Group 2 (MemoriesClient) review of this story. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit c4af9b0c

### DW-262: `CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:285-289, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:341
reason: - **`CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)** — docs/dev/experimental-apis.md already lists all three HXL001 methods; the story spec should be amended (TL;DR item 5) to acknowledge `CreateCaseAsync`. Alternative: remove `CreateCaseAsync` and rely on server-side auto-create (requires server support). [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:285-289, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:341]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/dev/experimental-apis.md:7

### DW-264: `CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:451
reason: - **`CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5** (which called for `Task<TenantSummary>` and `Task<MemoryUnit>`). Both server endpoints are 202 Accepted fire-and-forget, so returning the workflow instance id is the honest contract; the spec signatures were drafted before the server surface was verified. Amend spec TL;DR items 4-5 to match and document the polling pattern (`CreateTenantAsync` → poll `GetTenantAsync` until `TenantStatus.Active`; `IngestAsync` → poll via the workflow id route) in the next spec touch. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:451]
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:260

### DW-269: Review findings from the planning validation (Review Findings block in the story file)

origin: migrated from legacy ledger ("Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)"), 2026-09-01
location: n/a
reason: - **Review findings from the planning validation (Review Findings block in the story file)** — Several \[Review\]\[Patch\] entries in the story's Review Findings section surfaced during planning iteration and were folded into Tasks 1-4 during implementation (controller `[Topic]` binding, EventStore package boundary with Server adapters, typed router outcomes, compensated hybrid dedup, response DTO with `instanceId` contract, configurable `PubSubName`, canonical middleware order, queryable subject metadata, severity-correct 9100-9129 log bank). Any remaining bullets that did not turn into task-level checkboxes are documentation refinements (e.g. AC #17 wording), not code-level deferrals, and can be cleaned up in a follow-up story pass. Touching the Review Findings block would violate the dev-story "only modify the story file in permitted areas" rule.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/9-1-eventstoredb-integration-tests.md:274 records all planning findings as dispositioned.

### DW-282: Integration tests for `NaturalLanguageSemanticSearchService`.

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Integration tests for `NaturalLanguageSemanticSearchService`.** The library class ships without being wired into `HybridSearchService` (AC #7 — staged rollout). Follow-up story wires it in behind an opt-in `axis=naturalLanguage` query parameter and adds end-to-end search tests.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:59

### DW-283: Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).** `Dapr.Workflow.WorkflowState` (SDK 1.17.6) does not expose a code-version field per active instance, so `ShouldCountWorkflow` falls back to "any in-flight IngestionWorkflow." Clean same-version redeploys wait for the drain window (up to 5 min). Follow-up: (a) investigate SDK surface across Dapr 1.18+ for a workflow-version metadata hook, (b) if unavailable, thread a `"workflowCodeVersion"` tag through `IngestionInput` and persist to workflow state so the gate can compare against `Assembly.GetEntryAssembly().GetName().Version`. AC #11 updated 2026-04-24 to document the relaxation.
status: done 2026-09-06
archived: 2026-11-23
resolution: closed by human decision: Keep the any-active-workflow gate.
decision: 2026-09-06 Retain current guard — Keep the any-active-workflow gate.

### DW-290: Task 7.1 reflection test

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 7.1 reflection test** — `GraphQueryBuilderTests.AllCallers_PassStubCreatedAt` (reflection-based enumeration of `BuildMergeStubNode` callers asserting 2-arg form). Tied to the patch-level deprecation of the 1-arg overload.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs:242

### DW-299: S6-F6. Move `MetadataField = typeof(WorkflowState).GetField(...)` from static cctor to `Lazy<FieldInfo?>` with try/catch + 9173 emission.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F6. Move `MetadataField = typeof(WorkflowState).GetField(...)` from static cctor to `Lazy<FieldInfo?>` with try/catch + 9173 emission.** A missing/version-mismatched `Dapr.Workflow` assembly currently throws `TypeInitializationException` on first hosted-service invocation, bypassing the 9173 path entirely. Follow-up: lazy + structured log.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/HostedServices/WorkflowReplaySafetyHostedService.cs:216

### DW-302: S6-F9. Add `BatchSize=1` accumulator test for the notes/discrepancies routing across batch boundaries.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F9. Add `BatchSize=1` accumulator test for the notes/discrepancies routing across batch boundaries.** The current `[10_001]` test always bumps batches in chunks of 64 (default); a `BatchSize=2` mixed-result test would catch carry-over off-by-one.
status: done 2026-11-23
archived: 2026-11-23
resolution: The mixed-result workflow test requests batch size one and verifies the exact order, totals, and completion of three batches at the supported minimum of ten. See [consistency reporting verification](spec-five-deferred-consistency-reporting.md).

### DW-303: S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: docs/dev/consistency.md
reason: - **S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.** Resolves S6-D4 (9173 EventId now overloaded with `workflow-name-reflection-null` / `sidecar-query-timeout` / `sidecar-query-exception` / `metadata-field-missing`) and S6-P15 (consistency.md still describes Discrepancies as the all-encompassing list, missing the Notes split + independent-cap behavior). One docs PR covers both: enumerate the four 9173 reasons + per-reason triage steps; update the consistency.md "Discrepancies" section to describe the structural split and the new EventId 8210 NotesListTruncated.
status: done 2026-11-23
archived: 2026-11-23
resolution: The consistency runbook documents both independent caps, per-list counts and log recovery, plus event 9173 triage for current and older hosts. See [consistency reporting verification](spec-five-deferred-consistency-reporting.md).

### DW-304: S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.** Currently the `{Recommendation}` field is overloaded with `ConsistencyNoteKind` strings (e.g., `Recommendation NaturalLanguageEmbeddingMissing`) for note-only entries. This is a wire-shape change for log consumers; should land alongside S6-FA so the operator runbook can be updated atomically.
status: done 2026-11-23
archived: 2026-11-23
resolution: Verification event 8201 includes a structured EntryKind of Note or Discrepancy alongside the existing Recommendation field. The workflow test inspects captured structured fields. See [consistency reporting verification](spec-five-deferred-consistency-reporting.md).

### DW-305: S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs
reason: - **S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.** Existing CLI JSON round-trip already exercises these properties end-to-end; the standalone source-gen contract test is helpful but not blocking. Track in `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs`.
status: done 2026-11-23
archived: 2026-11-23
resolution: ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext checks the generated resolver, wire fields and deserialized note records with distinct retained and total counts. See [consistency reporting verification](spec-five-deferred-consistency-reporting.md).

### DW-306: S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.** Constructing `WorkflowState` requires test-double scaffolding the SDK does not document; gated on Story 9.x integration-test infrastructure.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/HostedServices/WorkflowReplaySafetyHostedService.cs:216

### DW-308: S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.** Existing tests pass against the corrected D6 column semantic; this is a test-quality follow-up. <!-- End of deferred items -->
status: done 2026-11-23
archived: 2026-11-23
resolution: The CLI command test checks case-sensitive column names and their numeric contents with each list independently truncated. See [consistency reporting verification](spec-five-deferred-consistency-reporting.md).

### DW-309: W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: actions/*
reason: - **W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.** All five third-party `actions/*` references in `.github/workflows/release.yml` are now pinned to a 40-char commit SHA with a trailing `# v<x.y.z>` comment for review context. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha` enforces the SHA shape so a future bump back to a floating tag fails the test.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-310: W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: src/**/*.csproj
reason: - **W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.** The validator now iterates every `src/**/*.csproj`, requires an explicit `<IsPackable>true|false</IsPackable>` declaration, and asserts every project is in exactly one of `packages` or `nonPackableProjects`. Coverage in `tests/tooling/release_packages/release_packages_test.py` exercises missing/unexpected/duplicate inventory entries and missing/blank/unsupported `IsPackable` values.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-314: W6. `submodules: recursive` cannot fetch private submodules without PAT.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:30,52,84
reason: - **W6. `submodules: recursive` cannot fetch private submodules without PAT.** `Hexalith.Commons` and `Hexalith.EventStore` are public today; revisit if either becomes private. (`.github/workflows/ci.yml:30,52,84`; `release.yml:24`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: .github/workflows/ci.yml:43

### DW-318: W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/release-packages.json
reason: - **W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.** New `tools/release-packages.schema.json` now defines required keys, `additionalProperties: false`, and `pattern`/`minItems` constraints. The inventory references the schema via `$schema` and `validate-release-packages.ps1` invokes `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`, `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-319: W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: Cli/README.md
reason: - **W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.** Either ship the tool first, or document `--prerelease` until 1.0.0 lands. (`src/Hexalith.Memories.Cli/README.md:7-9`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/dev/release-runbook.md:280

### DW-320: W14. CI workflow `fetch-depth: 0` not set on PR checkout.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:28-30
reason: - **W14. CI workflow `fetch-depth: 0` not set on PR checkout.** commitlint isn't run in CI yet (only locally per CONTRIBUTING); add when CI adopts commit validation. (`.github/workflows/ci.yml:28-30`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: .github/workflows/ci.yml:43

### DW-321: W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/tooling/release_packages/
reason: - **W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.** `ConvertTo-NormalizedNuGetVersion` strips `+...` build metadata before equality compare, emits a `Note:` diagnostic naming both the original and NuGet-normalized form, and threads the normalized value through both the per-package version assertion and the internal cross-package dependency-version assertion. Coverage: `test_version_with_build_metadata_normalizes_with_clear_message` in `tests/tooling/release_packages/`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-325: W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/publish-nuget.ps1 --skip-duplicate
reason: - **W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.** Story 14.2 keeps `cancel-in-progress: false` deliberately because cancelling a release mid-publish would convert a recoverable partial-publish into an indeterminate half-state — `tools/publish-nuget.ps1 --skip-duplicate` rerun-and-self-heal recovery requires that the in-flight release runs to completion. The 30-minute job timeout and the partial-publish issue alert (S11-FD) bound the worst-case stuck-release window. `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces the policy so a future flag flip lands with an explicit recovery model rather than a silent edit.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-332: S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).** `tagFormat: "v${version}"` collides with stale tags from manual or aborted releases. Currently the natural `git push tag` failure is the gate. Adding a structured pre-flight requires running `npx semantic-release --dry-run` to compute `nextRelease.version` (wasteful on every release) or carrying our own version-computation logic. Story 14.2 reassessed and chose to carry forward — neither preflight option meets the cost/benefit bar without an observed collision. Re-open trigger: first stale-tag-collision incident on `main`, migration from another release tool that left tags behind, or a release-time decision that the dry-run cost is acceptable. Defer-by: 2026-08-04 (re-evaluate at the next release-pipeline hardening pass if no triggering incident occurs).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 3445e66b

### DW-333: S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/publish-nuget.ps1
reason: - **S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).** Story 14.2 audited the existing path and confirmed it satisfies the spec: `tools/publish-nuget.ps1` writes a structured `publish-summary.json` (pushed/failed/notAttempted), emits a `PARTIAL PUBLISH - manual reconciliation required` GitHub Actions error annotation, and appends a Markdown step summary; `.github/workflows/release.yml` invokes `tools/create-partial-publish-issue.ps1` on workflow failure; the helper opens or comments on a `PARTIAL PUBLISH <version>` GitHub Issue with the run URL, status, package lists, and runbook reference. Coverage in `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail (`publish-failed`), middle-package-fail (`partial-publish`), pre-push validation failure, issue creation, issue commenting on rerun, and helper skip on `publish-failed` status. Operator recovery is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes (HTTP 409 vs non-409 distinction, rerun-and-self-heal contract).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-334: 12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.** `docs/dev/release-runbook.md` now ships a `Per-Release Package Audit Evidence` subsection that requires SHA-256 capture for every future release alongside the Package Evidence table, with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands and explicit equivalents (`dotnet nuget verify --all`, `nuget verify -Signatures`) for signature-based provenance. The historical `v1.2.0` block remains as-is because the source CI artifacts are no longer available locally; the requirement applies to releases after Story 14.2.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-335: 12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.** `docs/dev/release-runbook.md` adds a `Release Identity And Forensic Anchors` section that pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`) as the canonical token identity for tag, GitHub Release, and package-asset writes; lists the four anchors reviewers should capture per release (Actions run URL, tag commit SHA + tagger identity, Release "Created by", trigger event); and treats anything else as a forensic red flag.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-336: 12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: .github/workflows/release.yml:18
reason: - **12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.** `release.yml`'s skip-CI guard checks `head_commit.message` for the substring. A merge commit whose squash body legitimately contains `[skip ci]` (quoting another commit, copying changelog text) would silently suppress the release. Branch protection now blocks direct pushes, so the only producer of merge commits is PRs. Re-open trigger: first observed silently-skipped release. (`.github/workflows/release.yml:18`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 3445e66b

### DW-337: 12.1-RV4. Verify `package-lock.json` is tracked in git.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV4. Verify `package-lock.json` is tracked in git.** Sprint-status comment dated 2026-04-26 (Epic 11 closeout, P1) flagged the file as in working tree but untracked. `v1.2.0` shipped successfully on 2026-04-30, which implies `npm ci` worked, but neither this story's runbook nor the Dev Agent Record confirms `package-lock.json` is committed. Re-open trigger: first `npm ci` failure on a fresh clone, or sweep of Epic 11 leftover P-items. (`package-lock.json`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: package-lock.json:1

### DW-338: 12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.** File Scope explicitly permits an `UPDATE only for a cross-link to the runbook`; spec intent treats the runbook as the new operational source of truth. Adds discoverability without scope creep into Story 12.2. (`CONTRIBUTING.md`)
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: CONTRIBUTING.md:541

### DW-339: 12.1-RV6 [resolved in 12.1].

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV6 [resolved in 12.1].** Cross-reference of S11-FA / Story 12.6 from release runbook recovery notes was applied as a patch during the 12.1 code-review pass instead of being deferred. Operator-context only; the resolution of S11-FA itself remains tracked under S11-FA and Story 12.6.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-340: 12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:37
reason: - **12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.** `.github/workflows/ci.yml:37` masks auth/network/repository-rename errors and silently degrades the story-scope diff to `git diff-tree -r HEAD` (every file in HEAD). Drop `|| true` so fetch failures are loud. Out of Story 12.4 file scope; should land in a CI-hardening story.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-341: 12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:39
reason: - **12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.** `.github/workflows/ci.yml:39` — `A...B` requires a reachable merge-base, which depth=1 cannot guarantee. Either fetch enough history (`--depth=50` or `--unshallow`) or switch to 2-dot semantics. Out of Story 12.4 file scope.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-342: 12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:36-46
reason: - **12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.** `.github/workflows/ci.yml:36-46` — when `origin/main` after fetch equals `head_sha`, `git diff` returns empty and the validator silently passes. A direct push to main bypasses story-scope checks entirely. Branch protection should normally prevent this, but the workflow should fail loudly when the diff is empty under push-to-main.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-343: 12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:51-55
reason: - **12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.** `.github/workflows/ci.yml:51-55` — predictable delimiter that a hostile branch name could contain. Defense-in-depth; replace with a randomized sentinel.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-344: 12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:23-27
reason: - **12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.** `.github/workflows/ci.yml:23-27` — when both `PR_HEAD_REF` and `GITHUB_REF_NAME` are empty, downstream errors blame "no story key" instead of identifying the missing env. Hard-fail at the env-binding step.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-345: 12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:189-202
reason: - **12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:189-202` — tokens `baseline`, `test-release.ps1`, `release lane` drive classification. Schema-strengthen the deferred-work entry format (e.g., a `Filter:` line per entry) and parse structure rather than prose. Follow-up to the patches landed in this review pass.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-346: 12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:170-178
reason: - **12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:170-178` — inconsistent with trailer multi-key rejection. Story 12.3 territory; reject loudly to mirror trailer behavior.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-347: 12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:183-185
reason: - **12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:183-185` — same asymmetry as 12.4-RV7. Story 12.3 territory.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-348: 12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:13-16
reason: - **12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.** `tools/check-story-file-scope.py:13-16` — single-letter third segment, trailing-hyphen rejection are not directly tested. Story 12.3 territory.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-351: 12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:20,222-228
reason: - **12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.** `tools/check-story-file-scope.py:20,222-228` — Markdown's nested-fence form is supported by parsers but breaks the toggle. Story 12.3 territory.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-352: 12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:243-247
reason: - **12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.** `tools/check-story-file-scope.py:243-247` — only known section markers should terminate the allow-list. Story 12.3 territory.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-353: 12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:133-141
reason: - **12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.** `tools/check-story-file-scope.py:133-141` — emit a clean `ValidationError` with actionable message. Story 12.3 territory.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-354: 12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1108-1120
reason: - **12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.** `tests/tooling/story_scope/story_scope_validator_test.py:1108-1120` — could mask future validator regressions where Out-of-scope sections gain a blank-line continuation. Test-helper hardening.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-355: 12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1196-1199
reason: - **12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.** `tests/tooling/story_scope/story_scope_validator_test.py:1196-1199` — passes today but would silently co-exist with a future conflict-detection regression that exits 0. Test hardening.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-356: 12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1324-1334
reason: - **12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.** `tests/tooling/story_scope/story_scope_validator_test.py:1324-1334` — `assertIn` only checks stdout; if the error path moves to stderr, the test silently breaks. Test hardening.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-357: 12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1426-1456
reason: - **12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.** `tests/tooling/story_scope/story_scope_validator_test.py:1426-1456` — a future loader-precedence bug could silently load a different file. Test hardening.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-358: 12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1041
reason: - **12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1041` — em-dash, colon, or lowercase variants are silently ignored. Today all S11-F* entries use the literal-period format, so future-resilience only. Re-open trigger: first deferred-work format change.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-360: 12.4-RV1: closed. `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: .github/workflows/ci.yml
reason: - **12.4-RV1 — closed.** `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with `|| true` and no longer falls back to `git diff-tree -r HEAD`. Fetch failures exit 1 with a `::error::story-file-scope:` diagnostic that names the failed operation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-361: 12.4-RV2: closed. The push-fallback path now resolves an explicit `base_sha=$(git rev-parse

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: actions/checkout@v6
reason: - **12.4-RV2 — closed.** The push-fallback path now resolves an explicit `base_sha=$(git rev-parse origin/main)` and runs a 2-dot `git diff --name-only "$base_sha" "$head_sha"`, on top of `actions/checkout@v6`'s `fetch-depth: 0` clone. No more 3-dot reachability against shallow history.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-362: 12.4-RV3: closed. When `origin/main` resolves to the same commit as `head_sha`, the job

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: origin/main
reason: - **12.4-RV3 — closed.** When `origin/main` resolves to the same commit as `head_sha`, the job exits 1 with a direct-push / empty-diff diagnostic instead of silently passing file-scope validation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-363: 12.4-RV4: closed. `BRANCH_NAME` heredoc delimiter is randomized per run as

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV4 — closed.** `BRANCH_NAME` heredoc delimiter is randomized per run as `STORY_SCOPE_EOF_$(date +%s%N)_${$}_${RANDOM}_${RANDOM}` so a hostile branch name cannot collide with the closer.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-364: 12.4-RV5: closed. Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV5 — closed.** Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the job at the env-binding step with a diagnostic that names the missing variable; "no story key resolved" no longer hides a missing-env case.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-365: 12.4-RV7: closed. `--story-key` rejects values containing more than one story key and lists

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV7 — closed.** `--story-key` rejects values containing more than one story key and lists every detected key. Test: `test_multiple_keys_in_explicit_story_key_value_fails_with_all_keys_reported`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-366: 12.4-RV8: closed. Branch-name parsing rejects branches whose value contains more than one

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: \w-
reason: - **12.4-RV8 — closed.** Branch-name parsing rejects branches whose value contains more than one distinct story key (separated by a non-`[\w-]` character such as `/`) and lists every detected key. Test: `test_multiple_keys_in_branch_name_fails_with_all_keys_reported`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-367: 12.4-RV9: closed. Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection,

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV9 — closed.** Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection, uppercase normalization, and single-letter title segment.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-368: 12.4-RV12: closed. `parse_allowed_scope` tracks the open fence's marker character and

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV12 — closed.** `parse_allowed_scope` tracks the open fence's marker character and length so fences longer than three backticks containing nested 3-backtick fences (and tilde fences containing nested backtick fences) both parse correctly. Tests: `test_parser_handles_fences_longer_than_three_backticks`, `test_parser_handles_tilde_fence_with_nested_backtick_fence`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-369: 12.4-RV13: closed. Allow-list collection terminates only on known section labels

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: Read/verify only:
reason: - **12.4-RV13 — closed.** Allow-list collection terminates only on known section labels (`Read/verify only:`, `Forbidden by default:`, including their `**bold:**` variants) or `## ` headings; bullets whose rationale ends with `:` are no longer dropped. Tests: `test_parser_does_not_terminate_on_bullet_with_trailing_colon_rationale`, `test_parser_terminates_on_known_section_label_only`, `test_parser_does_not_terminate_on_unrecognized_prose_with_trailing_colon`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-370: 12.4-RV14: closed. `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git`

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV14 — closed.** `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git` catch `FileNotFoundError` and raise a clean `ValidationError` naming `git interpret-trailers` with an install / `PATH` hint. No Python traceback reaches contributors. Test: `test_missing_git_interpret_trailers_reports_clean_validation_error`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-371: 12.4-RV15: closed. `section_block` helper no longer terminates on blank lines; only

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV15 — closed.** `section_block` helper no longer terminates on blank lines; only non-blank, non-bullet lines end a section.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-372: 12.4-RV16: closed. `test_branch_and_trailer_agreement_passes` asserts the diagnostic does

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV16 — closed.** `test_branch_and_trailer_agreement_passes` asserts the diagnostic does NOT contain `Conflicting story keys`, so a future regression that exits 0 while emitting the conflict diagnostic cannot silently co-exist with the test.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-373: 12.4-RV17: closed. `test_unparseable_explicit_story_key_fails_closed` matches against

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV17 — closed.** `test_unparseable_explicit_story_key_fails_closed` matches against combined stdout + stderr via the `stdio()` helper, so the test does not break silently if the error path moves between sinks.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-374: 12.4-RV18: closed. `test_fixture_test_reports_loaded_story_artifact_path` pins the full

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV18 — closed.** `test_fixture_test_reports_loaded_story_artifact_path` pins the full `Story artifact:` line under the fixture artifacts root, so a future loader-precedence bug that loads a different file fails loudly.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-375: 12.3-RV15: closed. Multi-block `Allowed files for this story:` parsing is now exercised by

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.3-RV15 — closed.** Multi-block `Allowed files for this story:` parsing is now exercised by `test_parser_merges_multiple_allowed_files_blocks`. The validator merges entries across blocks consistently; future shape drift fails the test instead of changing scope silently.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 749c4e7c

### DW-376: 12.4-RV6: out of 14.1 scope (CI test inventory parser, not the story-scope lane).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV6** — out of 14.1 scope (CI test inventory parser, not the story-scope lane).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-379: 12.4-RV19: out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV19** — out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-381: W1: closed. `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`,

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: .github/workflows/release.yml
reason: - **W1 — closed.** `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/cache`, and `actions/upload-artifact` to 40-char commit SHAs with trailing `# v<x.y.z>` comments. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha` enforces the SHA shape so a future revert to a floating major-tag fails the test.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-382: W2: closed. `tools/validate-release-packages.ps1` iterates every `src//*.csproj`, requires

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W2 — closed.** `tools/validate-release-packages.ps1` iterates every `src/**/*.csproj`, requires an explicit `<IsPackable>true|false</IsPackable>` declaration, asserts the project appears in exactly one inventory bucket, and rejects missing/blank/unsupported `IsPackable` values. New Python fixture suite at `tests/tooling/release_packages/release_packages_test.py` covers missing/unexpected/duplicate inventory entries, both bucket misuses, and the three IsPackable failure modes via temporary sentinel csproj files under `src/`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-383: W12: closed. `tools/release-packages.schema.json` defines required keys with

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/release-packages.schema.json
reason: - **W12 — closed.** `tools/release-packages.schema.json` defines required keys with `additionalProperties: false`, `pattern` constraints on IDs and project paths, and `uniqueItems`. `tools/release-packages.json` now references the schema via `$schema`, and the validator runs `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`, `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-384: W15: closed. `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **W15 — closed.** `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the NuGet-comparable `1.2.3` via `ConvertTo-NormalizedNuGetVersion`, emits a `Note:` diagnostic naming both forms, and threads the normalized value through both per-package and internal cross-package dependency-version assertions. `pack-release.ps1` is unchanged because semantic-release passes versions without build metadata in the CI path.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-385: W19: closed. `concurrency: cancel-in-progress: false` is preserved deliberately to keep

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/publish-nuget.ps1 --skip-duplicate
reason: - **W19 — closed.** `concurrency: cancel-in-progress: false` is preserved deliberately to keep rerun-and-self-heal partial-publish recovery viable (`tools/publish-nuget.ps1 --skip-duplicate`). An inline comment in `release.yml` documents the trade-off and `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces it.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-386: S11-FD: closed. Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation,

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1
reason: - **S11-FD — closed.** Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation, step-summary, and `tools/create-partial-publish-issue.ps1` issue/comment path were audited as sufficient. `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail, middle-package-fail, pre-push validation failure, issue creation, issue commenting on rerun, and helper skip on `publish-failed` status. Operator recovery (HTTP 409 vs non-409, rerun contract) is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-387: 12.1-RV1: closed. `docs/dev/release-runbook.md` Per-Release Package Audit Evidence

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV1 — closed.** `docs/dev/release-runbook.md` Per-Release Package Audit Evidence subsection now requires SHA-256 capture for every release with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands, plus `dotnet nuget verify --all` and `nuget verify -Signatures` as audit-equivalent options for signature-based provenance. Historical `v1.2.0` is not retroactively backfilled.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-388: 12.1-RV2: closed. `docs/dev/release-runbook.md` Release Identity And Forensic Anchors

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV2 — closed.** `docs/dev/release-runbook.md` Release Identity And Forensic Anchors section pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`) as the canonical token identity and lists the four anchors reviewers must capture per release.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit e4318116

### DW-392: S11-FC: carried forward with fresh defer-by 2026-08-04. Stale-tag preflight still requires

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **S11-FC — carried forward with fresh defer-by 2026-08-04.** Stale-tag preflight still requires either an `npx semantic-release --dry-run` cost on every release or carrying our own version-computation logic. Story 14.2 reassessed and confirmed neither option meets the cost/benefit bar without an observed collision; refreshed the re-open trigger and defer-by date.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 3445e66b

### DW-395: 13.7-RV3. Clean up parent temp directory in `DeleteTempDaprConfig`.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:474-487
reason: - **13.7-RV3. Clean up parent temp directory in `DeleteTempDaprConfig`.** `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:474-487` — only `config.yaml` is removed; AppHost-generated component yamls accumulate per random `daprAppId` under `%TEMP%/hexalith-memories-dapr/`. Low impact; cleanup is feasible if the AppHost-generated files are also enumerated. Re-open trigger: CI temp-space exhaustion or first complaint.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-396: 13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../AspireIngestionPipelineFixture.cs:489-501
reason: - **13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.** `tests/.../AspireIngestionPipelineFixture.cs:489-501` and `src/Hexalith.Memories.AppHost/Program.cs` — same concept, two implementations, brittle five-`..` magic count in the fixture fallback. Resolved by the AppHost-owned `RepositoryRootLocator` shared by AppHost startup and the Aspire integration fixture.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit acfdf211

### DW-397: 13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: _bmad-output/implementation-artifacts/sprint-status.yaml
reason: - **13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.** `_bmad-output/implementation-artifacts/sprint-status.yaml` — entries accumulate per-event comment blurbs into multi-thousand-character logical lines (existing 13-2..13-7 entries all exhibit this). Project-wide pattern; coordinated convention change required. Re-open trigger: a parser/tool that fails on the long lines, or readability complaint.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 2af177e2

### DW-398: 13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../OllamaOidcFakeServerTests.cs
reason: - **13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.** `tests/.../OllamaOidcFakeServerTests.cs` — fake rejects missing `Content-Type`, missing `grant_type`, missing `client_id`, missing `client_secret`, and malformed bodies at runtime, but no `[Theory]+[InlineData]` enumerates each branch. Coverage gap rather than behavior gap; AC4 spirit met via wrong-path test plus runtime guards. Re-open trigger: a regression where the fake's rejection logic was weakened without tests catching it.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-399: 13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../OllamaEmbeddingEndToEndTests.cs:116
reason: - **13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.** `tests/.../OllamaEmbeddingEndToEndTests.cs:116` — the rationale (raw + NL embeddings = 2 calls) is implicit. A named constant or comment would prevent brittleness if production legitimately changes the call count. Re-open trigger: assertion fails after a refactor and the cause is not immediately obvious.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d2510226

### DW-403: 15.3-RV13. `OrdinalIgnoreCase` provider/model comparison vs case-sensitive downstream Redis hash keys.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:92-93
reason: - **15.3-RV13. `OrdinalIgnoreCase` provider/model comparison vs case-sensitive downstream Redis hash keys.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:92-93` — requires a broader audit of downstream key generation across raw/NL/migration paths to confirm whether a case-distinct write that passes the guard can still produce mixed-metadata Redis state. Re-open trigger: an incident where a tenant ends up with mixed-case provider/model metadata after migration.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-404: 15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:172-200
reason: - **15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:172-200` — a fresh non-resume start for target B silently overwrites the active marker while a B different target A migration may still be in progress. Out of story 15.3 scope. Re-open trigger: an operator-coordination incident where two migrations are launched concurrently on the same tenant.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-405: 15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:211-223
reason: - **15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:211-223` — same root cause as 15.3-RV15: completion writes `status=completed` to the active-marker key regardless of which migration is completing. Re-open trigger: same as 15.3-RV15.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit d673a0e2

### DW-409: 15.3-RV25. Operator-docs downtime statement could be sharper about per-tenant retry disruption.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: docs/operations/embedding-providers.md
reason: - **15.3-RV25. Operator-docs downtime statement could be sharper about per-tenant retry disruption.** `docs/operations/embedding-providers.md` — "Tenant-specific ingestion downtime is not required" is correct but some readers will interpret operator-visible per-tenant retry as "effective downtime"; phrasing could be tightened. Re-open trigger: operator confusion or escalation citing the downtime statement.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/operations/embedding-providers.md:244

### DW-410: 15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:64-65
reason: - **15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:64-65` — current path stores `int` via `HashEntry(string, int)` overload which is invariant; a future refactor to a string overload could silently regress to locale-sensitive parsing → fail-open. Re-open trigger: any refactor of the marker write path away from the `int`-typed `HashEntry` overload.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:73

### DW-411: 15.3-RV27. Stale per-target marker can resume against drifted state.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:179-187
reason: - **15.3-RV27. Stale per-target marker can resume against drifted state.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:179-187` — `--resume` only checks per-target key existence; does not verify active marker still references same target. Overlaps 15.3-RV15/16. Re-open trigger: same as 15.3-RV15.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:828

### DW-422: 15.6-CR4: resolved. `ResolveAllocatedEndpoint` is no longer called before awaiting the rewrite TCS in `BeforeResourceStartedEvent`. The endpoint lookup now occurs after `WaitForRedisComponentRewriteAsync`, so an early sidecar-start event gives Redis allocation and component-file rewrite a chance to complete before the lookup runs.

origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **15.6-CR4 - resolved.** `ResolveAllocatedEndpoint` is no longer called before awaiting the rewrite TCS in `BeforeResourceStartedEvent`. The endpoint lookup now occurs after `WaitForRedisComponentRewriteAsync`, so an early sidecar-start event gives Redis allocation and component-file rewrite a chance to complete before the lookup runs. - ID: 15.6-CR4 - Status: resolved - Source story: deferred-work-implementation-2026-05-19 - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: `ResolveAllocatedEndpoint(redis.Resource, "redis")` moves back above the rewrite wait, an Aspire upgrade changes the `BeforeResourceStartedEvent`-vs-allocation contract, or the `AppHostComponentFileOrderingTests` behavioral guard reproduces an early InvalidOperationException. - Evidence: `src/Hexalith.Memories.AppHost/Program.cs` snapshots the rewrite task, awaits `WaitForRedisComponentRewriteAsync(...)`, then resolves the Redis endpoint before `WaitForRedisPingAsync(...)`; `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs` carries the Docker/Aspire behavioral guard for the ordering invariant.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.AppHost/Program.cs:191

### DW-423: 2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.** Currently `internal static class` in `Hexalith.Memories.Contracts.Tests`; only consumed by `EvidencePacketSerializationTests`. Spec Task 5 demanded cross-surface fixture reuse (CLI, MCP, server tests). Rationale: requires moving fixtures to a shared test helper assembly (cross-cutting refactor) and re-keying CLI/MCP/server tests; paired with 2.7-CR2/CR3/CR4/CR5.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.TestHelpers/EvidencePackets/EvidencePacketCanonicalFixtures.cs:18

### DW-424: 2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.** `EvidencePacketCliOutputTests.cs` has a single hybrid happy-path `[Fact]`. Spec Task 5 demanded full state coverage at the CLI surface. Rationale: significant test-scope expansion; depends on 2.7-CR1 shared fixtures.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:28

### DW-425: 2.7-CR3. No table-driven sanitization tests across the spec'd categories

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR3. No table-driven sanitization tests across the spec'd categories** (unauthorized, all-backend failure, partial degradation, token-budget compression, server diagnostics, MCP error mapping). Rationale: paired with 2.7-CR1 shared fixtures.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketSanitizationTests.cs:48

### DW-426: 2.7-CR4. No tenant/case negative isolation fixtures.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR4. No tenant/case negative isolation fixtures.** Tests use only `tenant-a`/`case-a` happy-paths. Rationale: paired with decision on 2.7-CR9 (scope-consistency policy).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketIsolationTests.cs:20

### DW-427: 2.7-CR5. No server-side `EvidencePacketMapper` tests

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR5. No server-side `EvidencePacketMapper` tests** (tenant/case scope, empty results, partial backend degradation, all-backend/unauthorized diagnostics, token-budget omitted metadata). Rationale: needs new test class scaffolding in `Hexalith.Memories.Server.Tests`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs:21

### DW-432: 2.7-CR10. CLI does not emit `EvidencePacket` on error responses

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR10. CLI does not emit `EvidencePacket` on error responses** (decision-needed). Rationale: changes CLI JSON error envelope shape (`CliErrorWriter.WriteForCommand`); needs explicit decision about CLI error envelope schema, possibly affecting ADR-7.3-002.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:238

### DW-433: 2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal** (decision-needed). Rationale: needs a server-side change to expose authorization-driven emptiness; paired with 2.7-CR15.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs:165

### DW-434: 2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.** Rationale: needs precedence design between strength and state.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Keep semantics orthogonal — State describes execution and evidenceStrength describes quality; document that split.
resolution: closed by human decision: State describes execution and evidenceStrength describes quality; document that split.
decision: 2026-09-01 Keep semantics orthogonal — State describes execution and evidenceStrength describes quality; document that split.

### DW-435: 2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".** Rationale: lower-level `ScoredResult.Score` schema would need to become nullable.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs:61

### DW-440: 2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketCanonicalParityTests.cs:21

### DW-441: 2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records** instead of an envelope wrapper (design smell). Rationale: revert would require envelope wrapping at every consumer; mitigated by `[JsonIgnore(WhenWritingNull)]`.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Ratify nullable property — The additive property is compatible and protected by serialization tests.
resolution: closed by human decision: The additive property is compatible and protected by serialization tests.
decision: 2026-09-01 Ratify nullable property — The additive property is compatible and protected by serialization tests.

### DW-444: 2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).** Rationale: borderline scope-shape oracle; minor.
status: done 2026-09-06
archived: 2026-11-23
resolution: closed by human decision: Keep TenantId required and omit CaseId when null.
decision: 2026-09-06 Retain current shape — Keep TenantId required and omit CaseId when null.

### DW-448: 2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketCanonicalParityTests.cs:21

### DW-449: 2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.** Rationale: paired with 2.7-CR2 single-axis CLI coverage.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:390

### DW-458: 17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13
reason: - **17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern** (`src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13` and all display copy across the RCL). Rationale: broader localization work spans the whole new RCL, not just Evidence Cockpit; needs a Memories resource bundle decision before retrofitting.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 16958753

### DW-465: 17.1-CR8. `aria-label="Inspect source N"` exposes raw Rank including 0

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR8. `aria-label="Inspect source N"` exposes raw Rank including 0** (`MemoriesSourceCitationStack.razor:34`). Rationale: only relevant if Inspect button is reinstated with command wiring; the dead-button removal patch closes this path.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:20

### DW-466: 17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - **17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: Task 5 mandates these tests, but they are vacuous until command primitives exist; couples to dead-button removal patch.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17SanitizationCanaryTests.cs:123

### DW-467: 17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - **17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: useful when actions trigger real state changes; trivial today since all states are rendered in isolation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitRecoveryTransitionTests.cs:29

### DW-471: 17.1-CR14. `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText`

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR14. `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText`** (`MemoriesSourceCitationStack.razor:17`, `MemoriesRetrievalAxisBreakdown.razor:17`). Rationale: enum-like strings have controlled vocabulary; revisit if contract loosens to free-form strings.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:26

### DW-472: 17.1-CR15. Stale state never tested by fixture

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR15. Stale state never tested by fixture** — covered by the broader "5 of 8 states untested" patch above; standalone deferred.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Web.Specimens/Epic17EvidencePacketFixtures.cs:216

### DW-473: 17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout** (`MemoriesEvidenceCockpit.razor.css`). Rationale: minor compliance gap with Fluent UI primitive preference; wrapping behavior itself is correct.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceRemediationTests.cs:57

### DW-476: ID: 17.1-CR1

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`; `src/Hexalith.Memories.Web/Resources/*
reason: - ID: 17.1-CR1 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`; `src/Hexalith.Memories.Web/Resources/*` - Re-open trigger: any new Evidence Cockpit visible or assistive copy bypasses `EvidenceResourceKeys` and the EN/FR resource bundle. - Rationale: Story 25.7 routed cockpit headings, banners, enum labels, counts, freshness, timestamps, scores, captions, fallbacks, and accessible names through `IStringLocalizer<MemoriesWebResources>` with EN/FR parity tests. - Evidence: `EvidenceResourceKeys.cs`, localized Evidence components/helpers, and `EvidenceCockpitTests.Localization_EveryEvidenceKeyResolvesInEnglishAndFrench` plus the French multi-state rendering test.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 16958753

### DW-490: ID: 17.1-CR15

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`; `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - ID: 17.1-CR15 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`; `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - Re-open trigger: stale-state fixture coverage is removed or a new stale-state rendering path bypasses the existing fixture and per-state assertions. - Evidence: Story 17.1 review patch added the `Stale` fixture as part of the broader empty/stale/degraded/partial/weak fixture expansion, and current tests still reference `EvidencePacketFixtures.StalePacket()` in Evidence Cockpit, recovery, filters, lenses, and responsive/accessibility suites.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs:432-443 exercises the current EvidencePacketFixtures.StalePacket degraded-state fixture.

### DW-491: ID: 17.1-CR16

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css
reason: - ID: 17.1-CR16 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css` - Re-open trigger: Trust Strip wrapping is moved out of `FluentStack Wrap="true"` into hand-authored layout CSS. - Rationale: Story 25.7 keeps wrapping in the Fluent V5 stack primitive and verifies that the cockpit stylesheet contains no raw `flex-wrap` declaration. - Evidence: `MemoriesTrustStrip.razor` and `Epic17ConformanceRemediationTests.TrustStrip_UsesFluentStackWrappingInsteadOfRawFlexWrap`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor:8-11 renders the trust strip in a wrapping FluentStack.

### DW-499: ID: MEM-1

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/dev/public-surface-stability.md (review-enforced Mcp `PackageId` stability half); _bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md
reason: - ID: MEM-1 - Status: carried-forward - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: docs/dev/public-surface-stability.md (review-enforced Mcp `PackageId` stability half); _bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md - Re-open trigger: the published `Hexalith.Memories.Mcp` NuGet `PackageId` is renamed without a semantic-release `BREAKING CHANGE:` note, or a pack-time/analyzer guard becomes available to enforce the `PackageId` half that reflection cannot cover. - Rationale: Story 18.1 (done) delivered the compile-resolution guard test (`AppHostProjectResolutionTests`) and the name-stability contract (`docs/dev/public-surface-stability.md`), test-enforcing 5 of 6 contract items (project-symbol resolution, Server/Mcp assembly name + root namespace, Aspire symbol shape); the Mcp `PackageId` is a pack-time NuGet property not reflectable from a built assembly, so it stays review-enforced only and is carried forward as the residual half. Owner: AppHost/release maintainer. Story 19.1 (2026-06-30) refreshed this entry against the now-completed Story 18.1 without reopening Epic 18.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/PublicSurfaceStabilityTests.cs:68-77 pins the exact MCP PackageId.

### DW-500: ID: MEM-2

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.2)
reason: - ID: MEM-2 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.2) - Re-open trigger: a downstream operator cannot fill deployment placeholders because the canonical env/port/OTLP config surface is undocumented or has drifted from code. - Evidence: Story 18.2 published the canonical deploy-config contract at `docs/operations/deployment-configuration.md` (OTLP env gate, Dapr sidecar ports, required runtime env, pub/sub event-intake surface, app-id reconciliation) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (bidirectional doc<->code tie on the `EventIngestionController` constants plus authoritative source-file cross-checks). Residual full aspirate manifest emission is carried forward as `MEM-2-ASPIRATE`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/operations/deployment-configuration.md and tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs publish and guard the deployment configuration contract.

### DW-502: ID: MEM-3

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.3)
reason: - ID: MEM-3 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.3) - Re-open trigger: an external Dapr ACL cannot be verified against the Memories operation surface, or the published surface drifts from the mapped endpoints. - Evidence: Story 18.3 published the invocable route/operation-surface contract at `docs/operations/route-surface.md` (full 45-route `/api/*` inventory, pub/sub `/dapr/subscribe` + `POST /events/ingest` operation surface, health and MCP probes, and the explicit `/process` refutation tied to code) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` (forward code->doc route tie deriving the list from `Program.cs`, a 45-route count tie, bidirectional pub/sub + health constant ties, an MCP source-text tie, and a code-tied `/process` negative assertion). Residual OpenAPI/Swagger document emission is carried forward as `MEM-3-OPENAPI`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/operations/route-surface.md and tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs publish and guard the current route surface.

### DW-504: ID: MEM-4

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.4)
reason: - ID: MEM-4 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.4) - Re-open trigger: concurrent same-source ingests race into duplicate/partial memory units, or consumers still require the `HXL001` suppression to ingest. - Evidence: Story 18.4 graduated `MemoriesClient.IngestAsync` out of `[Experimental("HXL001")]` (stable 8-param overload preserved + additive `idempotencyToken` overload; `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`), added the optional `IngestionInput.IdempotencyToken` contract property (`src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`), and closed the REST `/api/ingest` check-then-act race with an atomic Redis `SET … NX` preflight reservation (`src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs`, wired in `Program.cs`) keyed by idempotency-token-precedence/`sourceUri`-fallback while preserving the permanent `sourceUri → MemoryUnitId` mapping for Stories 18.5/18.6. Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` (winner/loser concurrent-ingest + fail-open), `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` (token precedence + sourceUri fallback), `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` (token-keyed duplicate short-circuit + dual permanent record), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` (stable client, token on the wire), and `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` (camelCase round-trip + back-compat). Stable contract documented at `docs/dev/ingest-contract.md`; `HXL001` ledger updated at `docs/dev/experimental-apis.md`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:59 uses IngestDedupReservation, covered by IngestDedupReservationTests.cs.

### DW-505: ID: MEM-5

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.5)
reason: - ID: MEM-5 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.5) - Re-open trigger: a consumer resolving a memory unit from a known source URI must rely on free-text search and silently degrades graph mode to local. - Evidence: Story 18.5 exposed an exact source-URI-keyed lookup that reads the permanent dedup record as the authoritative index (no parallel store): new route `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri` (`src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`, mapped in `Program.cs`) backed by the lookup seam `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` (reuses `DedupKeyBuilder.BuildKey`, excludes the transient `PreflightDedupReservation` marker, and propagates Redis failures so the endpoint returns a structured `503 LOOKUP_BACKEND_UNAVAILABLE` rather than a false `404`). Surfaced through the additive `Contracts.V1` record `MemoryUnitIdLookupResponse` (registered in `MemoriesJsonContext`), the public `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` (`string?`, 404→null; D9 concrete/virtual), and the CLI diagnostic `memories search lookup` (`src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs`, `CliExitCodes.NotFound` on miss). MCP exposure deliberately declined (operational/diagnostic resolution). Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs`, `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs` (200 / structured-404 / 400 / cross-tenant / different-case / transient-reserved / Redis-down→503 / literal-route precedence), `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs` (camelCase round-trip), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` (path/encoding, 200→id, 404→null, error→MemoriesRemoteException), and `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs`. Published route surface updated at `docs/operations/route-surface.md` (45→46) with `RouteSurfaceContractTests` green.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs:266 delegates source-URI lookup to MemoryUnitLookupEndpoint.HandleAsync, covered by MemoryUnitLookupEndpointTests.cs.

### DW-506: ID: MEM-6

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.6)
reason: - ID: MEM-6 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.6) - Re-open trigger: a consumer's `MemoryUnitId`-keyed mapping accumulates ghost ids after a Memories restart/contract change because the stability semantics are unspecified. - Evidence: Story 18.6 published the MemoryUnitId stability contract at `docs/dev/memory-unit-id-stability.md` (MemoryUnitId is an opaque id string, not derived from `sourceUri` and not guaranteed to be a ULID; same `(tenantId, caseId, sourceUri)` re-ingestion returns the same canonical id while the permanent `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record persists; TTL-less `expiry: null` dependency made explicit; Redis-eviction / manual-deletion / TTL / key-format-change / cross-environment-reindex loss modes documented; the dedup record is the id-resolution authority, not the backend index; Story 18.4 token records `dedup:{tenantId}:{caseId}:tok:{sha256(token)}` augment-never-replace the source-URI record; Story 18.5 `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` / `GET .../memory-units/by-source-uri` named the authoritative resolution path; Parties 'decision D1' clarified as unrelated to Memories Architecture Decision D1 'FalkorDB for MVP') with an authoritative-guarantee cross-link added at `docs/dev/ingest-contract.md` section 6. Guarded by `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs` (doc<->code ties on SaveDedupKeyActivity `expiry: null`, DedupKeyBuilder `dedup:{tenantId}:{caseId}:` / `:tok:` shapes, and SourceUriMemoryUnitLookup `DedupKeyBuilder.BuildKey`) and `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/SaveDedupKeyActivityTests.cs` (TTL-less expiry assertion); existing IngestionWorkflowTests / DedupKeyBuilderTests continue to prove stable-instance-id reuse, independent id for `dedup:` event workflows, duplicate short-circuit, and dual permanent records.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/dev/memory-unit-id-stability.md is guarded by MemoryUnitIdStabilityContractTests.cs.

### DW-507: ID: MEM-7

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.7)
reason: - ID: MEM-7 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.7) - Re-open trigger: `MemoriesClient` is sealed or has `virtual` members removed, breaking consumer subclass-based test fixtures. - Evidence: Story 18.7 published the MemoriesClient mockability stability contract at `docs/dev/client-mockability.md` (reaffirms Architecture Decision D9 — concrete class, avoid the abstraction tax, extract an interface only when a second implementation arrives — and explicitly declines to add `IMemoriesClient`; documents the two supported seams: the recommended `HttpClient`/`IHttpClientFactory` boundary with a worked example and subclass override; guarantees `MemoriesClient` stays public + non-sealed with `virtual` public methods; records the breaking-change rule that sealing the class or removing `virtual` requires the D9 escape hatch (extract `IMemoriesClient`) plus a sprint change; notes the non-virtual `BaseAddress` passthrough is outside the mock seam) with companion cross-links added in `docs/dev/public-surface-stability.md` and `docs/dev/experimental-apis.md`. Guarded by `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (doc mandatory-claims content ties + reflection guard asserting `MemoriesClient` is public, non-sealed, exposes no `IMemoriesClient`, and that every public declared instance method is `IsVirtual && !IsFinal`, plus worked-example `[Fact]`s for both seams). The subclass seam remains proven by `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` and the `HttpClient` seam by the `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` suite; no production code changed (`MemoriesClient` already satisfied the contract).
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/dev/client-mockability.md and its contract tests preserve the non-sealed virtual MemoriesClient seam.

### DW-511: [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs
reason: - **RESOLVED 2026-07-05 — `EvidencePacketSource` has no freshness field** [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.
status: done 2026-07-05
archived: 2026-11-23
resolution: `EvidencePacketSource` has no freshness field** [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

### DW-513: Story 24.3 graph content and honest test evidence.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md
reason: - **Story 24.3 graph content and honest test evidence.** - ID: 24.3-GRAPH-CONTENT-EVIDENCE - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md - Backlog home: Story 24.6 - Owner: Murat / Test Architect and Developer - Rationale: The graph-content evidence is implemented and verified, but its owning Story 24.6 remains in review. Keeping the entry carried forward preserves the deferred-work lifecycle contract until the story reaches `done`. - Evidence: Story 24.6 implemented `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` with identical tenant A/B node IDs, topology, insertion order, graph-scoped relationship-ID collision, authenticated dual-tenant traversal, tenant-local node/edge markers, and zero foreign markers. The assertion-sensitivity control `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` plants a tenant B edge-marker literal in tenant A and proves the edge-locality assertion rejects it; it is not cross-tenant access evidence and does not mutation-test the node-marker assertions. The final authoritative real Aspire/FalkorDB owning-class run on 2026-08-14 passed 7 total, 0 failed, 0 skipped in 254.358 seconds with `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060` (following the tenant-B PreviousConfidence obligation); it supersedes every earlier method/class duration. The current verifier/runbook/authorization result is recorded once in the target story's verification table rather than duplicated with a competing duration here. Proof boundary: `SeedCollisionGraphAsync` writes through `falkor.SelectGraph(tenantId)` directly rather than through production ingestion, so this evidence proves authenticated read-path tenant routing and graph-content locality under collisions, not production write-path tenant selection; write-path isolation remains separately deferred. Exact commands are recorded in the target artifact and its linked implementation spec. This entry remains carried forward while Story 24.6 is in review and moves to resolved only when Story 24.6 reaches `done`. - Re-open trigger: Story 24.6 reaches `done` (promote this entry to `resolved`), or any change to the graph fixture, `AssertTraversalIsFixtureLocal`, `GraphQueryBuilder`, traversal route/authorization, FalkorDB tenant selection, removal/skip/rename of either the positive or assertion-sensitivity method, any claim that `GRAPH.LIST` or unit mocks prove content isolation, or a future required lane that cannot execute the real backend.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 24.6 is done and TenantIsolationIntegrationTests.cs:76 implements the identical-graph-structure isolation proof.

### DW-514: Story 24.3 configured vector-dimension authority.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md
reason: - **Story 24.3 configured vector-dimension authority.** - ID: 24.3-VECTOR-DIMENSION-SOURCE - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md - Backlog home: Story 24.7 - Owner: Winston / Architect and Developer - Rationale: Story 24.3 compares raw and natural-language semantic dimensions only with each other, so an equally wrong pair can pass. Story 24.7 makes the requested tenant's existing `ITenantEmbeddingConfigProvider` value authoritative without recreating indexes or running migration. - Re-open trigger: Story 24.7 is selected, or any verifier assurance relies only on raw-versus-natural-language dimension equality.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 24.7 is done and TenantIsolationVerifierTests.cs:279 verifies non-default tenant-configured dimensions.

### DW-515: Story 24.3 collision-safe semantic key-family membership.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md
reason: - **Story 24.3 collision-safe semantic key-family membership.** - ID: 24.3-SEMANTIC-KEY-FAMILY - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md - Backlog home: Story 24.8 - Owner: Developer and Murat / Test Architect - Rationale: Broad `{tenantId}:vec:*` and `{tenantId}:vecnl:*` scans include markerless raw/NL migration staging hashes and legacy nested-NL hashes, causing false marker-mismatch evidence. Because memory-unit IDs are opaque, Story 24.8 requires canonical provenance and record shape rather than prefix-only shortcuts, plus collision-shaped tests and a guarded unknown-family outcome. - Re-open trigger: Story 24.8 is selected, a migration or legacy tenant reports false `SemanticIsolation`, a new semantic namespace appears, or a classifier assumes reserved-looking colon text cannot occur inside an opaque memory-unit ID.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 24.8 is done and IndexSchemaDefinitionsTests.cs:213 covers reserved-looking opaque identifiers.

### DW-516: Story 24.3 distinct and non-destructive marker remediation.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md
reason: - **Story 24.3 distinct and non-destructive marker remediation.** - ID: 24.3-MARKER-REMEDIATION - Status: resolved - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md - Backlog home: Story 24.9 - Owner: Winston / Architect, Murat / Test Architect, and Developer - Rationale: Pre-Story-24.3 active hashes can lack `tenantId`, yet current verification combines missing and foreign markers and recommends removing mismatched target-prefix hashes. Story 24.9 keeps both outcomes fail-closed but classifies missing markers as incomplete evidence, foreign values as possible contamination, and limits remediation to named-key inspection/quarantine plus tenant-scoped repair or reindex after provenance verification. - Evidence: Discharged 2026-08-31 by Story 24.9 dev-story. `TenantIsolationVerifier.ScanSemanticHashPrefixForTenantEvidenceAsync` (src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs) now carries an internal `MarkerDefectKind` (`Missing`/`Foreign`) on each `MarkerMismatchEvidence` entry, never exposed through the public V1 contract. `CheckSemanticIsolationAsync`'s non-classification-gap `Details`/`Remediation` construction branches on that kind via the new `BuildSemanticIsolationRemediation` helper: a foreign non-empty `tenantId` reports a confirmed marker mismatch/possible contamination naming the exact key, expected tenant, and observed tenant; a missing `tenantId` reports incomplete evidence, not confirmed cross-tenant leakage; a mixed check preserves both distinct diagnoses. Every marker-related `Remediation` path now directs named-key inspection/quarantine and tenant-scoped marker repair or reindex only after provenance is verified, and the anti-template "remove mismatched target-prefix hashes" phrase is removed from every marker path (the syntactic-only `ScanHashPrefixForTenantFieldMismatchesAsync` scanner, a separate non-semantic check, is untouched and still out of this story's scope). The V1 `TenantIsolationCheckResult.Details`/`Remediation` plain-string shape is unchanged. Focused evidence: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` passed 94/94, 0 failed/skipped; `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests` passed 7/7, 0 failed/skipped. New tests `TenantIsolationVerifierTests.VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance`, `VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, and `VerifyAsync_MixedMissingAndForeignActiveMarkers_PreservesBothDistinctDiagnoses` each assert the new wording and that no marker `Remediation` contains the retired blanket-deletion phrase. Full detail is in `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md`'s 2026-08-31 dev-story Change Log row; the story remains at `Status: review` pending code review, so this ledger discharge is scoped to the rationale/re-open condition below, not a claim that Story 24.9 itself is `done`. **Scope precision (2026-08-31 code review):** this discharge applies only when no classification gap co-occurs with the marker mismatch in the same `SemanticIsolation` check. When a classification gap and an active marker defect are both present, `CheckSemanticIsolationAsync`'s `Remediation` ternary still selects the classification-gap-only sentence and does not compose in the marker-specific guidance this entry describes; `TenantIsolationVerifierTests.VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesBothDiagnostics` now pins that exact behavior. That co-occurrence prioritization gap is the pre-existing, separately tracked `24.6-F8-W9` entry below, which remains open and unchanged by this discharge. - Re-open trigger: missing markers are described as confirmed leakage, operator guidance recommends broad prefix deletion, or Story 24.9's code-review phase finds the marker `Remediation`/`Details` wording regresses to the pre-24.9 shared/destructive form.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 24.9 is done and TenantIsolationVerifier.cs:673-731 preserves distinct missing/foreign marker diagnostics and non-destructive remediation.

### DW-521: 24.2-RV3: Pre-existing: two `DeleteMemoryUnitProjectionActivityTests` fail at HEAD (unrelated to Story 24.2). `RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast` and `RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry` fail on the full server slice (2 of 2441). Verified pre-existing by stashing the 24.2 review patches and re-running the class (still 2/3 failing), so NOT introduced by read-path caching — the delete-projection Redis hash/vector ordering area, likely from a later commit (24.3/24.4/CI). Flagged during the 24.2 code review for separate triage. Re-open trigger: whoever owns the delete-projection area investigates the NSubstitute in-order sequence assertion on annotation/target/syntactic-hash deletion.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)"), 2026-09-01
location: n/a
reason: - **24.2-RV3 — Pre-existing: two `DeleteMemoryUnitProjectionActivityTests` fail at HEAD (unrelated to Story 24.2).** `RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast` and `RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry` fail on the full server slice (2 of 2441). Verified pre-existing by stashing the 24.2 review patches and re-running the class (still 2/3 failing), so NOT introduced by read-path caching — the delete-projection Redis hash/vector ordering area, likely from a later commit (24.3/24.4/CI). Flagged during the 24.2 code review for separate triage. Re-open trigger: whoever owns the delete-projection area investigates the NSubstitute in-order sequence assertion on annotation/target/syntactic-hash deletion.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: At HEAD, both named DeleteMemoryUnitProjectionActivityTests pass individually in Release.

### DW-530: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting. evidence: The review traced `/api/tenants/{tenantId}/traverse`; `edgeTypes=","` produces an empty split result and assigns an empty `parsedEdgeTypes` list. This behavior existed before endpoint extraction and can return no edges where the default traversal would have applied.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:415-417 treats null or empty edge types as defaults.

### DW-537: Hexalith.Memories.Web.Tests — including the Epic 17 machine-checked inventory/over-claim guards — is in the .slnx test inventory but absent from every tools/test-projects.*.txt lane, so it never runs in CI.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` summary: Hexalith.Memories.Web.Tests — including the Epic 17 machine-checked inventory/over-claim guards — is in the .slnx test inventory but absent from every tools/test-projects.*.txt lane, so it never runs in CI. evidence: grep finds no reference to Web.Tests under `.github/` or `tools/`; the `test-unit-contract` CI job runs only the five projects listed in `tools/test-projects.unit-contract.txt` (Contracts/Server/Cli/Mcp/EventStore), yet that file's own header says "Keep in sync with Hexalith.Memories.slnx test inventory." The 476-test suite runs locally/pre-commit and passed here via `dotnet exec` (DiffEngine_Disabled=true), but no CI lane executes it, so the story's fail-closed inventory guards are not CI-enforced. bUnit is in-process/docker-free, so wiring Web.Tests into the unit-contract lane is viable but must be confirmed with a headless CI run that this sandbox cannot perform. Pre-existing drift broader than Story 17.7, surfaced because 17.7's headline machine-checked guards depend on CI execution.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tools/test-projects.unit-contract.txt:8 includes Hexalith.Memories.Web.Tests and CI executes the unit-contract lane.

### DW-540: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
location: n/a
reason: - source_spec: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/spec-gh-29158532353-release-preflight-stale-branch.md` summary: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists. evidence: Repository-pinned semantic-release 25.0.5 emits `There is no previous release, the next release version is <version>`, while the pre-existing parser expects `The next release version is <version>`; this repository already has release tags, so first-release support is outside the stale-checkout incident fix.
status: done 2026-10-28
archived: 2026-11-23
resolution: The existing terminal parser now recognizes the exact first-release version record confirmed in pinned semantic-release 25.0.8 while preserving logger provenance, collision checks and malformed/conflicting-record rejection. Fixtures cover plain/logger/ANSI records, first-release local collisions, the live 2x2 payload/logger marker matrix and payload-only failures. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/release_preflight -p '*_test.py' passed 36/36 methods (120.463s, exit 0); the subsequently strengthened diagnostic assertions also passed in the focused method (10.375s, exit 0). See spec-five-deferred-release-preflight.md. Reopen if this preflight behavior or its regression coverage is lost.

### DW-541: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md` summary: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run. evidence: `.github/workflows/ci.yml` has no step that runs `validate-release-packages.ps1 -PackageDirectory`, no `python -m unittest` over `tests/tooling/release_packages` or `tests/tooling/publish_nuget`, and no `dotnet pack`; the `release.yml` bare invocation passes no `-PackageDirectory` (skipping the whole new block), the `release_packages` fixtures run only inside `release.yml`, and the real-package throws fire only inside `semantic-release` → `pack-release.ps1`. A PR that re-adds a `Hexalith.Memories.*` ProjectReference to the Redis compatibility package, drifts the Mcp→ServiceDefaults dependency version out of lockstep, or edits the ServiceDefaults/Redis/publish tooling passes all PR checks — the only PR-lane guard, `BackendProjects_ShouldNotUseRedisCompatibilityPackageAsDependencyFacade`, asserts csproj text, not packed nuspec dependency graphs. `tests/tooling/publish_nuget/publish_nuget_test.py` is edited by this story yet is discovered by no CI lane at all. Pre-existing CI-lane architecture (release validation has always run at release time), surfaced by Story 25.8 adding substantial new post-merge-only validation; closing it means wiring the tooling fixtures (and ideally a pack plus `-PackageDirectory` validation) into the PR lane, which must be confirmed with a headless CI run this sandbox cannot perform.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: .github/workflows/ci.yml:289-293 runs release_packages and publish_nuget tests; line 330 runs pack-release.ps1 with PackageOnly.

### DW-547: [HIGH — gating, environment-blocked] The mandatory no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) has never executed; every runtime AC (AC-3/4/5 aggregate-health/degradation/fail-closed + AC-1 container-start) rests on it.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [HIGH — gating, environment-blocked] The mandatory no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) has never executed; every runtime AC (AC-3/4/5 aggregate-health/degradation/fail-closed + AC-1 container-start) rests on it. evidence: Dev sandbox lacks `docker`/`kind`; the story is correctly at `review`, not `done`. Must run green with ZERO skips on CI/an operator cluster before `done`. Prerequisite: the verifier's image-tag naming mismatch (patch finding, `verify-production-deployment.ps1:189-190` vs `publish-containers.ps1:64,72`) will likely make the first run fail at `docker tag` — fix it before relying on this gate. Re-open trigger: any change to the deploy topology, health/ACL semantics, or container publication.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 27.3 line 1633 records qualifying CI run 33400812038 at the reviewed source with all required steps.

### DW-554: [LOW] Case-scoped restore does not enforce per-record case membership.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] Case-scoped restore does not enforce per-record case membership. evidence: `RestoreDataPlaneActivity.RunAsync` (`:71-124`) restores every case/unit/edge in the envelope and never reads `input.CaseId`; `ImportRequestValidator` checks only `manifest.CaseId`. No cross-tenant impact (caller is tenant-authorized). Re-open trigger: hardening the import validator to reject records outside the route case, or a multi-case case-scoped-export defect.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: RestoreDataPlaneActivity.cs:105-116 validates case, memory-unit, and manifest targets before writes.

### DW-557: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total. evidence: `RestoreReindexUnitActivity.RunAsync` (`:85-95`) returns `RestoreReindexResult(id, 0)` when the syntactic hash is absent, and `RestoreWorkflow` (`:68-71`) reports `RestoredMemoryUnits` from the data-plane count — so a partially-failed restore could report `completed` with full counts and units that are `Indexed` but have no `:vec:` vectors. Largely unreachable in the happy path (the data-plane activity writes every hash first and fails the workflow if it can't). Re-open trigger: any change that decouples data-plane restore from re-index, or an observed partial-restore incident.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: RestoreWorkflow.cs:47-75 totals actual ProcessedMemoryUnits, and restore reindexing fails when syntactic content is missing.

### DW-572: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41. evidence: Blind review found non-canonical `Target artifacts:` and `Re-open/claim trigger:` labels, a validator that accepts incomplete debt metadata, and contradictory proposed/applied and open-action wording in the concurrent A41 artifacts; these issues predate and are outside the one-shot tracking correction.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commit e335a89b and tools/verify_access_telemetry_lifecycle.py:1949-1961 require a published resolved A41 state and full evidence.

### DW-573: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation. evidence: Blind review found that `architecture.md` describes a structured log file while `docs/dev/telemetry.md` documents console emission plus an operator-selected external pipeline; this unrelated documentation conflict predates the one-shot tracking correction.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/architecture.md:227 and docs/dev/telemetry.md:27 consistently define JSON console plus optional OTLP without an owned collector.

### DW-574: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal. evidence: Blind review found that the concurrent proposal both preserves and changes the unknown-ID error contract and omits the mixed GUID-N/GUID-D backend case that can suppress fallback; this is outside the one-shot tracking correction.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: ConsistencyEndpointTests.cs:198-222 proves opaque identifier misses return 404 without GUID syntax.

### DW-577: Align the repository's OpenTelemetry core packages with the versions imported by the current Hexalith.Builds pointer so the exact working tree restores and builds again.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-consistency-inspect-opaque-id-contract.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-consistency-inspect-opaque-id-contract.md` summary: Align the repository's OpenTelemetry core packages with the versions imported by the current Hexalith.Builds pointer so the exact working tree restores and builds again. evidence: Exact-tree restore/build fails with NU1605 because Hexalith.Builds@8e0e2da imports OTLP exporter and hosting 1.17.0 while Directory.Packages.props pins OpenTelemetry core 1.16.0; the opaque-ID change neither caused nor is authorized to alter that concurrent dependency state.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:266-273 aligns OpenTelemetry core and exporters at 1.18.0.

### DW-579: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list.

origin: migrated from legacy ledger ("Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md
reason: - source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` summary: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list. evidence: The concurrent A41-entry rewrite in commit `8bb0708a` (sprint-change-proposal scope, not Story 27.1) replaced the old `Target artifact: docs/operations/rate-limiting.md and the future access-telemetry storage/purge implementation` line with a new target list that omits rate-limiting.md entirely, leaving that file's documentation obligation without a stated disposition. The fourth code-review pass of Story 27.1 (2026-07-17) surfaced the orphaned obligation; ownership belongs to the A41/Story 27.4 close-out coordination after Story 27.3 qualification, not this decision story.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commit e335a89b and docs/operations/rate-limiting.md:161-168 restore the obligation.
decision: 2026-09-01 Restore obligation — Restore the document to A41 or Story 27.4 targets and reconcile it during close-out.
decision: 2026-09-01 Restore obligation — Restore the document to A41 or Story 27.4 targets and reconcile it during close-out.

### DW-588: Live test-count totals recorded in the 27.3 Change Log predate current HEAD.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Live test-count totals recorded in the 27.3 Change Log predate current HEAD. - ID: 27.3-CR2 - Status: resolved 2026-07-26 — the live runner recount executes in this sandbox and was run again by dev-story. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: before Story 27.3 advances to done, or when any File List assembly changes. - Evidence: recount executed 2026-07-26 by dev-story on a clean tree at HEAD `a6753c11` with fresh Release builds (0 warnings, 0 errors). Pre-development: Server.Tests 2,190 / `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`, IntegrationTests 297 / `7836151bdf59ff8712f59911ed138a2f7afc792a7c4d2415c64122695c163856`, AccessTelemetry.Tests 55 / `973244b8ebcdfd55eeaf01ba56b8f33a1836aee158a98906817b5a5b2e3e60ef`, Cli.Tests 384 / `55e179bb6678fb671b1b342eeef71876b5f2f2c6106903c36507bb16769de312`. Command: `DiffEngine_Disabled=true dotnet exec <assembly> -list methods -noLogo | grep -E '^Hexalith\.'`. - Rationale: The entry's recorded figures (Server 2,188 / Integration 297 / AccessTelemetry 43) were stale: they predated the chunk-1 and chunk-2 review patches. The correct pre-development figures at HEAD `a6753c11` are recorded in the Evidence field above, and the post-development figures are in the 2026-07-26 `dev-story` Change Log row. The original claim that a full live runner recount "could not be executed in this sandbox" is false and is corrected here.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:15 records Status: done and the corrected recount.

### DW-589: recordId charset is not validated before it is interpolated into the Dapr state key.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs
reason: - recordId charset is not validated before it is interpolated into the Dapr state key. - ID: 27.3-CR3 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs - Re-open trigger: a RecordId containing '/' or other key-delimiter characters can reach GetRecordKey/GetBucketKey. - Rationale: GetRecordKey builds `records/{shard}/{recordId}` and GetShard only guards null/whitespace; confirm the AccessTelemetryRecord contract constrains RecordId to a safe charset, otherwise add explicit validation.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: DaprAccessTelemetryStateStore.cs:40 canonicalizes before key construction and AccessTelemetryCanonicalizer enforces uppercase Crockford ULIDs.

### DW-590: Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation. - ID: 27.3-CR4 - Status: resolved 2026-07-26 — the Server story/external attribution is restated and the recompute is done. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: before Story 27.3 advances to done / at the final-chunk code-review ledger reconciliation. - Evidence: the authoritative equation is `2,157 create + 6 Story 27.3 + 1 Story 31.1 + 26 external = 2,190`, recorded by the 2026-07-26 chunk-3b `code-review` Change Log row and restated in the 2026-07-26 `dev-story` row. It supersedes both the recorded `+1/+30` and this entry's own `+5/+26` target, which was correct only at Server 2,188. The `+1` is `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`, which follows Story 31.1 with `deploy/openbao/values.yaml` per the 2026-07-26 Administrator decision. Live discovery at HEAD `a6753c11` confirms Server 2,190 at hash `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`; the 2026-07-26 dev-story phase added no Server method, so the equation is unchanged after it. - Rationale: 4 Story-27.3 C1 methods (Adr_C1SourceEventMapping, Adr_C1TypedStateAndNullableMapping, Adr_C1QueryAndErrorMappings, Adr_ProductionAdapterQualification in AccessTelemetryRetentionDecisionTests.cs 6->10; plus ProductionDeploymentArtifactsTests +2) are booked under the +30 external delta rather than the +1 story delta. Recompute with live discovery (expected Server +5 story / +26 external) when the final review chunk finalizes the ledger. Administrator approved deferring the recompute to the final chunk on 2026-07-21.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 27.3 records the reconciled Server.Tests total as 2,190.

### DW-591: Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md
reason: - Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story. - ID: 27.3-CR5 - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 30 / Story 30.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/planning-artifacts/epics.md - Re-open trigger: before Story 27.3 advances to done; the release/publish-pipeline work must own a separate story. - Evidence: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26-readiness-coherence-and-27-3-splits.md` (approved 2026-07-26) enumerates the ten transferred paths; `_bmad-output/planning-artifacts/epics.md` carries Epic 30 / Story 30.1; `_bmad-output/implementation-artifacts/sprint-status.yaml` registers `30-1-...` as `backlog`; Story 27.3's File List no longer declares any of the ten paths. - Rationale: The four-image publish/partial-recovery pipeline (CiTestInventoryTests.cs + tests/tooling/publish_containers/*) is independently demonstrable and was ledgered as an external CI/CD lane, yet is bundled into the single C1 adapter-qualification slice. Administrator approved splitting it into a new story via correct-course on 2026-07-21; 27.3's File List and ledger shrink to adapter/qualification scope.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: epics.md and sprint-status.yaml register the Epic 30 release/publish stories.

### DW-592: Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`. - ID: 27.3-CR18 - Status: resolved 2026-07-26 — chunk 3a and chunk 3b are now reviewed and the live recount ran successfully; the surviving obligations are DW 27.3-CR4 (Server attribution) and the open review action items. (`superseded` is not one of the register's four documented statuses; corrected to `resolved` on 2026-07-26 by dev-story.) - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: superseded; do not reopen under this ID. - Evidence: chunk 3a (2026-07-26, HEAD `159d7216`) and chunk 3b (2026-07-26, HEAD `c9dfb06f`) are both recorded in **Code Review Evidence** of the story file, and the live recount executed successfully at `159d7216` (Server 2,190 / IntegrationTests 297 / AccessTelemetry.Tests 55). The surviving obligation is tracked separately as DW 27.3-CR4.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 27.3 records completed chunk-3a/chunk-3b review and a successful live recount.

### DW-594: `maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: deploy/kubernetes/base/dapr/access-telemetry-store.yaml:25
reason: - ~~`maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.~~ [deploy/kubernetes/base/dapr/access-telemetry-store.yaml:25] — **resolved 2026-07-26 (dev-story)**: `maxConns` lowered to `40`, so `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93 <= max_connections 100`. The derivation is a comment on the metadata entry and is enforced by the new `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`, which failed RED at `141 > 100` before the fix.
status: done 2026-07-26
archived: 2026-11-23
resolution: story)**: `maxConns` lowered to `40`, so `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93 <= max_connections 100`. The derivation is a comment on the metadata entry and is enforced by the new `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`, which failed RED at `141 > 100` before the fix.

### DW-595: Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs
reason: - ~~Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.~~ [tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs] — **resolved 2026-07-26 (dev-story)**: all six surfaces are now bound by `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound`. Because the guard passed on first authoring, it was mutation-proven rather than RED-proven: six independent drift injections (`skipVerify:"true"`, `actorStateStore:"false"`, dropped `hostnossl ... reject`, RBAC verbs widened to `get,list`, ACL `defaultAction: allow`, and an extra secret smuggled into `allowedSecrets`) each failed the suite, and the baseline returned green after every revert.
status: done 2026-07-26
archived: 2026-11-23
resolution: story)**: all six surfaces are now bound by `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound`. Because the guard passed on first authoring, it was mutation-proven rather than RED-proven: six independent drift injections (`skipVerify:"true"`, `actorStateStore:"false"`, dropped `hostnossl ... reject`, RBAC verbs widened to `get,list`, ACL `defaultAction: allow`, and an extra secret smuggled into `allowedSecrets`) each failed the suite, and the baseline returned green after every revert.

### DW-598: 27.3-CR23: chunk 3b unreviewed; fail-closed for `done`. (Renumbered 2026-07-26 by code review, chunk 3b: this entry was minted as `27.3-CR7`, colliding with the existing `DW 27.3-CR7` create-story-verifier entry. Both were open and both were cited as `done` blockers, so the ID resolved to two unrelated obligations. Resolved 2026-07-26: chunk 3b has now been reviewed; this entry is closed by that review. Renumbered again 2026-07-27 by code review, chunk 3: a later, unrelated entry — the AC6/C2 production-deployment-verification red-run record — was independently minted as `27.3-CR17`, recreating the identical collision this entry was renumbered once already to escape. This entry, being resolved and historical, is renumbered to `27.3-CR18` rather than the active, currently-cited `27.3-CR17`. Renumbered a third time 2026-07-27 by dev-story: `27.3-CR18` was itself already taken by the resolved chunk-2 fail-closed-blockers entry above, so that renumber recreated the very collision it was performing. This entry moves to `27.3-CR23`, the first free ID; the pre-existing `27.3-CR18` above keeps the ID it held first. The recurrence is now bound by `CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`, which failed RED on this exact duplicate.) The eight governance/planning record

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - **27.3-CR23 - chunk 3b unreviewed; fail-closed for `done`.** (Renumbered 2026-07-26 by code review, chunk 3b: this entry was minted as `27.3-CR7`, colliding with the existing `DW 27.3-CR7` create-story-verifier entry. Both were open and both were cited as `done` blockers, so the ID resolved to two unrelated obligations. Resolved 2026-07-26: chunk 3b has now been reviewed; this entry is closed by that review. **Renumbered again 2026-07-27 by code review, chunk 3:** a later, unrelated entry — the AC6/C2 production-deployment-verification red-run record — was independently minted as `27.3-CR17`, recreating the identical collision this entry was renumbered once already to escape. This entry, being resolved and historical, is renumbered to `27.3-CR18` rather than the active, currently-cited `27.3-CR17`. **Renumbered a third time 2026-07-27 by dev-story:** `27.3-CR18` was itself already taken by the resolved chunk-2 fail-closed-blockers entry above, so that renumber recreated the very collision it was performing. This entry moves to `27.3-CR23`, the first free ID; the pre-existing `27.3-CR18` above keeps the ID it held first. The recurrence is now bound by `CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`, which failed RED on this exact duplicate.) The eight governance/planning record - ID: 27.3-CR23 - Status: resolved 2026-07-26 — chunk 3b reviewed; all in-scope chunks are now complete. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: resolved; a new chunk would need a new ID. - Evidence: the chunk-3b record in **Code Review Evidence** of the story file (8 governance/planning paths, 2,264 diff lines at HEAD `c9dfb06f`, manifest SHA-256 `605152e597357936680f5f171d9a87e09dfcb7887e21ecd953bfab6d550d6344`), which states that chunk 3b completes path-level review coverage. paths of Story 27.3 (story file, `epics.md`, `architecture.md`, the 2026-07-20 sprint change proposal, `deferred-work.md`, `sprint-status.yaml`, and the create-scope and adapter-profile evidence packets; 2,076 diff lines) have not been reviewed. Per `story-phase-ledger.md`, an intermediate chunk may emit findings but cannot finalize the ledger or synchronize completion status. Owner: Story 27.3 review owner. Consequence: the final `code-review` row cannot be appended and Story 27.3 cannot reach `done`. Reopen trigger: run code review over the chunk-3b path set, then append the final row carrying evidence that all three chunks are complete.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 27.3 records the completed chunk-3b review closing 27.3-CR23.

### DW-600: 27.3-CR9: commit `358bef35` bypasses the Conventional Commits contract. Its subject carries

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: .githooks / commitlint configuration
reason: - **27.3-CR9 - commit `358bef35` bypasses the Conventional Commits contract.** Its subject carries - ID: 27.3-CR9 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: .githooks / commitlint configuration - Re-open trigger: confirm the commit-msg hook rejects a missing type prefix, and decide whether the omitted product change needs a follow-up release note. - Rationale: Commit `358bef35` carries no Conventional Commits type prefix, and its body lists only the three new test files while omitting the `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. It is already published on `main`, so correcting the message needs a history rewrite; the durable fix is the commit-msg gate, not this commit. Owner: repository workflow owner. Consequence: release semantics and the changed-surface audit trail are both wrong for that one commit. no type prefix and its body enumerates only the three new test files, omitting the `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. The commit is already published on `main`, so correcting the message itself would require a history rewrite. Owner: repository workflow owner. Consequence: `feat`/`fix` release semantics and the changed-surface audit trail are both wrong for that commit. Reopen trigger: confirm the commit-msg hook rejects a missing type prefix, and record whether the omitted product change needs a follow-up release note.
status: done 2026-10-28
archived: 2026-11-23
resolution: Recorded the omitted expiry timestamp ordering change from canonical commit 358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4 in docs/dev/release-runbook.md, including its in-memory test-adapter scope. Real commit-msg fixtures reject untyped headers and preserve valid commits; published history is retained. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 13/13 methods (exit 0); see spec-five-deferred-commit-validation.md. Reopen if the gate or caller contract permits these bypasses again.
decision: 2026-09-01 Add follow-up note — Add a release-note correction and regression-test the commit-msg gate.
decision: 2026-09-01 Add follow-up note — Add a release-note correction and regression-test the commit-msg gate.

### DW-616: The local commitlint hook fails open when repository dependencies are absent.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The local commitlint hook fails open when repository dependencies are absent. evidence: `.githooks/commit-msg` prints an installation hint but exits successfully when `node_modules/.bin/commitlint` is unavailable, contrary to mandatory local validation policy.
status: done 2026-10-28
archived: 2026-11-23
resolution: .githooks/commit-msg now exits 1 with npm ci guidance if the pinned CLI is absent or non-executable. Disposable real Git commit fixtures prove no commit is created and no downstream gate is reached in either case. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 13/13 methods (exit 0); see spec-five-deferred-commit-validation.md. Reopen if the gate or caller contract permits these bypasses again.

### DW-617: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy. evidence: `commitlint.config.mjs` does not disable default ignores, so commitlint's built-in ignored message shapes are outside the configured Conventional Commit rules.
status: done 2026-10-28
archived: 2026-11-23
resolution: commitlint.config.mjs disables built-in ignores. Pinned-CLI and actual-hook fixtures reject merge, version, revert, reapply and fixup/squash subjects that lack a valid Conventional Commit header; all eleven supported types remain accepted. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 13/13 methods (exit 0); see spec-five-deferred-commit-validation.md. Reopen if the gate or caller contract permits these bypasses again.

### DW-618: Main-push commitlint runs can cancel an earlier range before it is validated.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Main-push commitlint runs can cancel an earlier range before it is validated. evidence: The workflow groups all main pushes together with `cancel-in-progress: true`, while each reusable run validates only its event-specific before-to-after range.
status: done 2026-10-28
archived: 2026-11-23
resolution: .github/workflows/commitlint.yml assigns every main push a run_id-specific group and limits cancellation to pull_request events. Parsed caller fixtures pin these expressions and preserve PR supersession; distinct groups protect queued as well as running push ranges. Actual GitHub scheduling is not executed locally. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 13/13 methods (exit 0); see spec-five-deferred-commit-validation.md. Reopen if the gate or caller contract permits these bypasses again.

### DW-619: Contributor guidance still recommends the forbidden chore branch and commit type.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Contributor guidance still recommends the forbidden chore branch and commit type. evidence: `CONTRIBUTING.md` contains `chore/<short-name>` and `chore:` examples while the shared baseline and current commitlint type enum forbid `chore`.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commit 7df55c12 records the correction; the root baseline permits the chore commit type.

### DW-620: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification. evidence: Repository tests do not execute the hook against an invalid message, reject forbidden message shapes through the pinned config, or contract-test PR-title edit wiring.
status: done 2026-10-28
archived: 2026-11-23
resolution: Retained executable policy/hook tests use the installed pinned CLI and actual git commit in disposable repositories, with only unrelated downstream gates isolated. Parsed caller checks retain PR edited triggers, exact title forwarding and mandatory shared-validator invocation. CLI and hook matrices reject invalid messages and accept valid controls; remote reusable execution is outside local evidence. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/commitlint -p '*_test.py' -v passed 13/13 methods (exit 0); see spec-five-deferred-commit-validation.md. Reopen if the gate or caller contract permits these bypasses again.

### DW-621: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests. evidence: The new header- and body-phase catches handle `OperationCanceledException`, but tests throw only the narrower `TaskCanceledException` unless caller cancellation is requested.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantsRestQueryClientTests.cs:911 and :929 cover plain non-caller OperationCanceledException at header and body reads and map it to timeout.

### DW-622: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage. evidence: REST-client tests stop at the `InvalidCursor` enum and gateway tests inject an already mapped `invalid-cursor` exception, leaving `ToReasonCode` disconnected from page-one recovery tests.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantsRestQueryClientTests.cs:468 distinguishes invalid cursors and TenantQueryGatewayTests.cs:2362 verifies typed transport mapping before recovery.

### DW-623: Independent tenant-detail and member read fault containment lacks page-level verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Independent tenant-detail and member read fault containment lacks page-level verification. evidence: The page now maps each faulted initial read to its own unavailable state, but existing tests cover pending success and cancellation rather than either read faulting independently.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantDetailSurfaceTests.cs:2378 verifies each independently faulted initial detail/member read while the sibling is still observed.

### DW-624: The Tenants member paging state machine has only happy-path coverage.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The Tenants member paging state machine has only happy-path coverage. evidence: Existing tests do not verify retained previous data, invalid-cursor recovery, failed-page state preservation, the 50-entry history cap, or navigation from an empty later page.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantDetailSurfaceTests.cs covers the 50-row cap, invalid-cursor recovery, and failed refresh retaining confirmed rows.

### DW-625: Failed refresh-subscription retry and duplicate-subscription protection lack verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Failed refresh-subscription retry and duplicate-subscription protection lack verification. evidence: Tests do not assert `Empty.IsSubscribed` is false followed by successful retry, nor exercise the audit page's in-flight setup guard under overlapping parameter passes.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantReadRefreshSubscriptionTests.cs:254-268 proves failed leases are unsubscribed and retry succeeds; TenantAuditPageTests.cs:167 covers overlapping setup.

### DW-626: The Tenants command-side URI scheme gate lacks non-HTTP composition tests.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The Tenants command-side URI scheme gate lacks non-HTTP composition tests. evidence: Existing malformed-scheme theories vary only `Tenants:BaseAddress` while retaining a valid EventStore address, so command-gateway fallback is not pinned.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantsUiCompositionTests.cs:622-640 covers non-HTTP and malformed EventStore base addresses.

### DW-628: Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
location: n/a
reason: - Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Story 27.3 line 625 explicitly records both review chunks complete.

### DW-629: `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
location: tools/check-story-review-readiness.py
reason: - `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: tools/check-story-review-readiness.py:851-855 rejects empty changed sets with a clear fail-closed message.

### DW-633: Fix integration-fast Dapr actor Connection refused failures from CI run 30990821240 (rate limiting + tenant configuration tests against 127.0.0.1:35131).

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: n/a
reason: - source_spec: none summary: Fix integration-fast Dapr actor Connection refused failures from CI run 30990821240 (rate limiting + tenant configuration tests against 127.0.0.1:35131). evidence: Split from the CI fix intent so BMAD customization fixture drift can ship independently without waiting on sidecar/harness diagnosis.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:78 records the recovered actor-proxy checkpoint as complete.

### DW-638: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test. evidence: Review found the registered 24.6 evidence contract omits GraphScopedSearchIntegrationTests, SyntacticSearchIntegrationTests, and SemanticSearchIntegrationTests that Dev Notes and Planned Verification require citing.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md:85-87 lists all three required axis-specific test classes.
decision: 2026-09-01 Add axis-specific evidence — Add three axis-specific integration classes or equivalent cases without weakening the all-axis proof.
decision: 2026-09-01 Add axis-specific evidence — Add three axis-specific integration classes or equivalent cases without weakening the all-axis proof.

### DW-639: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement. evidence: The approved proposal says “all available values agree” while the registered story and epics.md require “all three values agree,” leaving undefined behavior when one index dimension is missing.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md:24 and TenantIsolationVerifier.cs:808 show agreement validation that fails closed when dimensions are missing.
decision: 2026-09-01 Fail closed — Return incomplete verification whenever any required FT.INFO dimension is absent and test each field.
decision: 2026-09-01 Fail closed — Return incomplete verification whenever any required FT.INFO dimension is absent and test each field.

### DW-640: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics. evidence: Edge-case review showed proven-active hashes with empty/whitespace tenantId are unclassified and can be mislabeled as foreign contamination.
status: done 2026-11-23
archived: 2026-11-23
resolution: Blank and whitespace-only markers now use the missing-marker diagnosis. VerifyAsync_ActiveFamilyMarkerEvidence_MissingAndForeignFailClosed covers all three active semantic families.
decision: 2026-09-01 Treat as missing marker — Classify blank tenantId as incomplete structural evidence and use non-destructive remediation.
decision: 2026-09-01 Treat as missing marker — Classify blank tenantId as incomplete structural evidence and use non-destructive remediation.

### DW-656: The verifier class-level contract had been narrowed beyond Story 24.6.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The verifier class-level contract had been narrowed beyond Story 24.6.** - ID: 24.6-CR-W5 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Evidence: Fifth-pass Decision D2 restored the broad class-level architectural-isolation XML summary and kept the structural-only hedge local to `CheckGraphIsolationAsync`; focused verifier tests passed after the repair. - Re-open trigger: A future change again applies the graph-specific structural-only limitation to the verifier's class-wide Redis and semantic responsibilities.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:21-22 restores the broad class summary.

### DW-657: Non-passing graph-isolation branches do not repeat the structural-only label.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Non-passing graph-isolation branches do not repeat the structural-only label.** - ID: 24.6-CR-W6 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Evidence: E-P12 prefixed the missing-graph and backend-unavailable `GraphIsolation.Details` branches with the structural-only label, confirmed by inspection of `TenantIsolationVerifier.cs`. The focused verifier, runbook, and authorization gate (the same three classes as the 86-case snapshot, since revised) passed 101/101 on 2026-08-29 after the `references/Hexalith.Builds` submodule bump unblocked the build; see the twelfth-pass Change Log row on `24-6-graph-content-level-tenant-isolation-evidence.md`. - Re-open trigger: An operator or automated consumer interprets a failed or unavailable `GraphIsolation` result as graph-content proof, or those branch messages are otherwise revised.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantIsolationVerifier.cs:448 and :915-918 qualify missing/backend-unavailable results as structural-only.

### DW-658: The HTTP-visible graph detail has no explicit length or format contract.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The HTTP-visible graph detail has no explicit length or format contract.** - ID: 24.6-CR-W7 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: The roughly 330-character prose detail is intentionally operator-facing and is now pinned for its required structural-only, `GRAPH.LIST`, and proof-method tokens; introducing a new structured or maximum-length contract would expand the public API surface. - Re-open trigger: The V1 response receives a formal details-length/format requirement or an operator surface truncates the required proof citation.
status: done 2026-09-01
archived: 2026-11-23
decision: 2026-09-01 Retain prose contract — Keep the current bounded token assertions as the supported contract.
resolution: closed by human decision: Keep the current bounded token assertions as the supported contract.
decision: 2026-09-01 Retain prose contract — Keep the current bounded token assertions as the supported contract.

### DW-662: Verifier unit mocks no longer rely on unconfigured graph-query defaults.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
reason: - **Verifier unit mocks no longer rely on unconfigured graph-query defaults.** - ID: 24.6-CR-W11 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs - Evidence: Fifth-pass repair inspects all `ReceivedCalls()` arguments for every `GRAPH.*` command and requires the non-empty executed set to contain only `GRAPH.LIST`; the companion source guard scans every `TenantIsolationVerifier*.cs` file and rejects any other graph command token. - Re-open trigger: A verifier collaborator can execute a graph command without being captured by `ReceivedCalls()`, or graph-command construction moves outside the guarded source family.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: TenantIsolationVerifierTests.cs:1693-1720 and :1979 inspect graph calls and require GRAPH.LIST only.

### DW-664: `ReconnectPrimaryDaprClients` disposes before installing replacements.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **`ReconnectPrimaryDaprClients` disposes before installing replacements.** - ID: 24.6-F5-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The rewritten method constructs both actor-proxy replacements first and only then disposes `_actorHttpMessageHandler` and swaps `_actorProxyFactory`/`_actorProxyOptions`. The original dispose-before-assign window is closed; a brief null window remains during the swap. Not currently reachable: `[Collection("AspireIngestionPipeline")]` serialises the tests and the restart regression creates its proxy after the rotation. The untested allocation-failure cleanup path is recorded separately as `24.6-F8-W2`. - Re-open trigger: Any test caches an actor proxy across the OpenBao restart, or the collection gains parallel execution.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: AspireIngestionPipelineFixture.cs:1191-1206 constructs replacements before disposing old clients at :1215-1218.

### DW-675: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed. evidence: The working tree shows `references/Hexalith.Builds` dirty at `5d268c6b` with `Props/Directory.Packages.props` modified. That leftover is owned by `spec-submodule-bumps-2026-08-11.md`, not this envelope, which was required to preserve unrelated root work.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 8ed18ed6 bumps references/Hexalith.Builds; its checked-out worktree is clean on main.

### DW-676: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope. evidence: The 2026-08-05 envelope still has an unchecked superproject-push task and was not superseded or partitioned by the 2026-08-09 closeout, so a later operator can restage the same gitlinks under a second Story-Key.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-05.md:5 is done.

### DW-678: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md` summary: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11. evidence: Memories consumes its own contracts via ProjectReference, so this pin is not a direct PackageReference AC failure; bumping it locally would move Builds off origin/main unless a Builds PR lands first.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:10 pins HexalithMemoriesVersion 2.22.1 in commit 12b69515.

### DW-680: TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.** - ID: 24.7-CTOR-UNGUARDED-PARAMS - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: A future change to `TenantIsolationVerifier`'s constructor, or a dedicated null-safety hardening pass. - Rationale: Only the new `embeddingConfigProvider` parameter gained an ArgumentNullException guard; `registry`, `redis`, `falkorDb`, and `logger` predate Story 24.7 and still surface null as NullReferenceException at first use, deviating from the documented `ArgumentNullException.ThrowIfNull` boundary rule. Pre-existing behavior; only the new parameter was in Story 24.7's scope.
status: done 2026-11-22
archived: 2026-11-23
resolution: Both constructor paths validate the shared verifier dependencies by name. Evidence: `TenantIsolationVerifierTests.Constructor_NullDependency_ThrowsForThatParameter`.

### DW-682: Bound semantic-isolation mismatch evidence returned for large tenant key sets.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Bound semantic-isolation mismatch evidence returned for large tenant key sets.** - ID: 24.7-SEMANTIC-EVIDENCE-UNBOUNDED - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: The owning diagnostic-bounding slice is selected, or an operator reports an oversized `SemanticIsolation.Details` response. - Rationale: `ScanHashPrefixForTenantFieldMismatchesAsync` records every missing or foreign tenant marker and `CheckSemanticIsolationAsync` joins the full list into `Details`; this behavior predates Story 24.7 and can produce an unbounded diagnostic response when many hashes are contaminated. Pre-existing behavior; bounding it is out of Story 24.7's frozen scope.
status: done 2026-11-22
archived: 2026-11-23
resolution: Semantic verification retains up to ten examples per finding category across both prefixes, counts omitted findings and continues scanning. Timeout results preserve samples and counts collected before the failure. Evidence: `TenantIsolationVerifierTests.VerifyAsync_LargeSemanticScan_BoundsEachEvidenceCategoryAndKeepsScanning`.

### DW-683: `GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **`GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.** - ID: 24.6-F8-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: The success `Details` string reports `({graphDatabases.Count} graph database(s))` over `POST /api/v1/tenants/{tenantId}/verify`, which is a count of other tenants returned to a single tenant's caller. The count predates Story 24.6 and the endpoint is operator-facing rather than tenant-facing, so this is not a live cross-tenant data leak. It is recorded because this range rewrote that exact string for a story whose thesis is not overstating isolation evidence, and no existing entry covers it. - Re-open trigger: The verify endpoint becomes reachable by a tenant-scoped caller, or any story re-scopes `GraphIsolation` evidence semantics.
status: done 2026-11-22
archived: 2026-11-23
resolution: The structural graph and successful orphan checks omit the cluster database total. The structural result identifies the requested tenant and retains the content-proof citation. Evidence: `TenantIsolationVerifierTests.VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof`.

### DW-695: Bound tenant-isolation embedding-configuration lookup time independently of caller cancellation.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Bound tenant-isolation embedding-configuration lookup time independently of caller cancellation. evidence: `TenantIsolationVerifier.CheckSemanticIsolationAsync` waits only on the caller token, so a provider that ignores cancellation can hang verification indefinitely.
status: done 2026-11-22
archived: 2026-11-23
resolution: The verifier caps the requested tenant's configuration wait at five seconds. A stalled provider produces a failed, sanitized SemanticIsolation check while the other checks continue. Evidence: VerifyAsync_EmbeddingConfigLookupDeadline_FailsClosedAndContinuesIndependentChecks in the 81-case verifier run recorded in `spec-five-deferred-tenant-isolation-reliability.md`. Reopen if a stalled lookup blocks verification past its deadline or supplies an unchecked semantic result.

### DW-699: Convert Redis command timeouts across tenant-isolation checks into structured backend-unavailable evidence.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: Convert Redis command timeouts across tenant-isolation checks into structured backend-unavailable evidence. evidence: `TenantIsolationVerifier` catches connection and server exceptions but not `RedisTimeoutException`, so an index-info, cursor-adjacent, or hash-field timeout can escape `VerifyAsync` instead of returning the verifier's backend-unavailable result contract.
status: done 2026-11-22
archived: 2026-11-23
resolution: All five checks report structured backend-unavailable results for Redis/FalkorDB timeouts. Driver messages are omitted, including for a disconnected backlog reported as a connection exception. Evidence: VerifyAsync_CommandTimeouts_ReportEveryAffectedCheckWithoutDriverDetails and VerifyAsync_SyntacticScanAndInitialGraphFailures_AffectOnlyThoseChecks in the 81-case verifier run recorded in `spec-five-deferred-tenant-isolation-reliability.md`. Reopen if a command or cursor timeout escapes the verifier, discloses driver diagnostics, or suppresses an independent check.

### DW-700: Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan.** - ID: 24.6-F8-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Syntactic isolation verification fails on non-hash Redis keys under tenant prefix or is unable to cancel in-flight hash inspections. - Rationale: `TenantIsolationVerifier.ScanHashPrefixForTenantFieldMismatchesAsync` calls `await db.HashGetAsync(key, "tenantId")` without passing `CancellationToken ct` and does not handle `RedisServerException` on WRONGTYPE non-hash keys. Pre-existing behavior outside story scope.
status: done 2026-11-22
archived: 2026-11-23
resolution: Syntactic hash reads stop waiting on caller cancellation. WRONGTYPE identifies the affected key and still allows later keys under the requested tenant prefix to be inspected. Evidence: VerifyAsync_SyntacticWrongType_ReportsKeyAndContinuesCheckingThePrefix and VerifyAsync_PendingSyntacticHashRead_StopsWaitingWhenCancelled in the 81-case verifier run recorded in `spec-five-deferred-tenant-isolation-reliability.md`. Reopen if a non-hash key prevents later marker checks or a canceled hash read keeps verification waiting.

### DW-701: Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods.** - ID: 24.6-F8-W6 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Cancellation token propagation standards are audited across all server verification services. - Rationale: `CheckIndexExistenceAsync`, `CheckSyntacticIsolationAsync`, and `CheckOrphanedDatabasesAsync` lack entry `ct.ThrowIfCancellationRequested()` and do not bind Redis operations to `ct`. Pre-existing behavior outside story scope.
status: done 2026-11-22
archived: 2026-11-23
resolution: The verifier rejects cancellation before its first dependency read and checks cancellation before dispatching index, graph and key reads. Evidence: VerifyAsync_AlreadyCancelled_DoesNotDispatchBackendReads and VerifyAsync_PendingIndexOrGraphRead_StopsWaitingWhenCancelled in the 81-case verifier run recorded in `spec-five-deferred-tenant-isolation-reliability.md`. Reopen if a canceled invocation reaches the registry or places further Redis/FalkorDB commands.

### DW-703: Broaden IsEmbeddingConfigurationUnavailable exception filters in TenantIsolationVerifier.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Broaden IsEmbeddingConfigurationUnavailable exception filters in TenantIsolationVerifier.** - ID: 24.6-F8-W8 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.7 audits provider lookup error handling. - Rationale: `IsEmbeddingConfigurationUnavailable` filters for four specific exception types; unexpected I/O or RPC exceptions during provider lookups could surface as unhandled 500 errors instead of structured check results. Pre-existing behavior owned by Story 24.7.
status: done 2026-11-23
archived: 2026-11-23
resolution: VerifyAsync_EmbeddingConfigUnavailable_FailsClosed covers I/O and gRPC lookup failures, sanitized evidence and completion of independent checks.

### DW-704: Support dual remediation when classification gap and dimension mismatch co-occur.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Support dual remediation when classification gap and dimension mismatch co-occur.** - ID: 24.6-F8-W9 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.8 addresses dual remediation formatting. - Rationale: `TenantIsolationVerifier.CheckSemanticIsolationAsync` suppresses vector dimension remediation guidance when a key classification gap is also present. Pre-existing behavior owned by Story 24.8.
status: done 2026-11-23
archived: 2026-11-23
resolution: VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesAllRemediation verifies configured-dimension guidance alongside a classification gap.

### DW-705: Add replica endpoint filtering in GetConnectedServers for clustered Redis deployments.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Add replica endpoint filtering in GetConnectedServers for clustered Redis deployments.** - ID: 24.6-F8-W10 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Redis cluster deployment topology validation is performed. - Rationale: `TenantIsolationVerifier.GetConnectedServers` does not filter out `server.IsReplica`, which can cause duplicate scans across replicas in clustered or primary-replica topologies. Pre-existing behavior outside story scope.
status: done 2026-11-22
archived: 2026-11-23
resolution: Both Redis cursor scans select connected primaries, and key inspection requires primary routing. A replica-only view or a missing primary during the read yields failed availability evidence. Evidence: `TenantIsolationVerifierTests.VerifyAsync_RedisReplica_DisregardsReplicaKeysAndRequiresConnectedPrimary` and `TenantIsolationVerifierTests.VerifyAsync_PrimaryLostAfterEnumeration_ReportsUnavailableMarkerRead`.

### DW-706: Tenants worktree HEAD `4a3eec38` is ahead of the staged gitlink `c5fa0082`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md` summary: Tenants worktree HEAD `4a3eec38` is ahead of the staged gitlink `c5fa0082`. evidence: Pre-existing `MM` submodule state; this story must not unstage or edit Tenants, so a later parent commit of the staged SHA can miss checkout `4a3eec38`.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: commit 8ed18ed6 bumps references/Hexalith.Tenants to clean main commit aa88a037.

### DW-707: Isolate the server embedding-provider options test from parallel static options state.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-ci-cd-and-release-2026-08-30.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-cd-and-release-2026-08-30.md` summary: Isolate the server embedding-provider options test from parallel static options state. evidence: `AddMemoriesServerServices_WithHostEmbeddingProvidersConfig_SeedsCurrentOptionsAndOllama` observed the repository default endpoint during one full release-suite run, passed alone, and passed on an immediate complete rerun; the CI runner and telemetry-root changes do not touch that options surface.
status: done 2026-10-28
archived: 2026-11-23
resolution: 2026-10-28 — `spec-five-deferred-embedding-regressions.md`: Already resolved by 5b43fe2f8a0f04dc021921a077dff1a573c2ce5e: EventIngestionWorkflowSchedulerAdapterTests belongs to EmbeddingProviderDefaultsStateCollection, whose DisableParallelization=true excludes concurrent collections, and the host-options method restores previous options in finally. The existing AddMemoriesServerServices_WithHostEmbeddingProvidersConfig_SeedsCurrentOptionsAndOllama test passes alongside the embedding-default tests. The full Server.Tests assembly also passed with 3123 passing cases, zero failures/errors and one pre-existing disabled submodule-mutation test (25.692 seconds). No redundant isolation implementation added. Reopen if this class leaves that collection, parallelization is enabled, options restoration is removed or the host-config test flakes again. Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 354 cases with zero failures/errors/skips/not-run; Debug build passed with zero warnings/errors.

### DW-708: Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence. evidence: `CheckSemanticIsolationAsync` awaits the raw-prefix scan then the natural-language-prefix scan under one shared `try`/`catch (RedisConnectionException/RedisServerException)`; if the second scan throws, any marker mismatches or classification gaps the first scan already collected are discarded and the check returns a generic backend-unavailable result instead of surfacing the already-detected isolation problem. Pre-existing pattern predating Story 24.8 (the same shared-try shape already existed for `ScanHashPrefixForTenantFieldMismatchesAsync`'s raw/NL split); this diff extends it to also carry classification-gap evidence.
status: done 2026-11-22
archived: 2026-11-23
resolution: The failed semantic check retains classification and marker evidence already gathered across both scans, including findings before an interruption during the natural-language scan. Evidence: VerifyAsync_NaturalLanguageScanTimeout_RetainsAllPreviouslyCollectedEvidence in the 81-case verifier run recorded in `spec-five-deferred-tenant-isolation-reliability.md`. Reopen if a scan interruption drops previously collected findings or produces a passing semantic check.

### DW-709: `Remediation` text still does not support dual guidance when a classification gap co-occurs with another problem.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: `Remediation` text still does not support dual guidance when a classification gap co-occurs with another problem. evidence: `CheckSemanticIsolationAsync`'s `hasClassificationGap ? "Register or migrate..." : "Repair or re-provision..."` ternary always picks gap wording over marker-mismatch (or dimension-mismatch) wording when both are present in `Details`. This is the same unresolved gap as carried-forward ledger item `24.6-F8-W9` ("dual remediation when classification gap and dimension mismatch co-occur"), which already named Story 24.8 as its natural closure point; this diff did not close it, only extended the same single-priority pattern to also cover the new classification-gap case.
status: done 2026-11-23
archived: 2026-11-23
resolution: Duplicate of DW-704 and DW-714. The same mixed-finding tests verify both the dimension and marker guidance.

### DW-711: `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` still calls `AddHexalithMemoriesSearchIndexServer` with the retired `string secretStoreComponentPath` positional argument and will fail to build once it references an updated `Hexalith.Memories.Aspire` package.

origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
location: references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md` summary: `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` still calls `AddHexalithMemoriesSearchIndexServer` with the retired `string secretStoreComponentPath` positional argument and will fail to build once it references an updated `Hexalith.Memories.Aspire` package. evidence: Story 29.2 changed that parameter to `IResourceBuilder<IDaprComponentResource> secretStore` (an intentional, spec-required breaking change so the reusable extension accepts an externally-provisioned secret-store resource instead of hard-coding `secretstores.local.file`). Fixing the Tenants call site requires a coordinated change in the `Hexalith.Tenants` submodule/repo, which this story's boundaries explicitly exclude from casual editing. - ID: 29.2-TENANTS-SECRETSTORE-CALLSITE - Status: resolved - Source story: spec-29-2-provider-neutral-aspire-composition-and-secret-verification - Target artifact: references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs - Re-open trigger: a future `Hexalith.Memories.Aspire` signature change breaks the Tenants AppHost call site again, or the Tenants submodule pin is bumped without picking up commit `7453ba5b`. - Evidence: Hexalith.Tenants commit `7453ba5b` ("fix: update Memories secret-store call site for the Aspire 29.2 signature change") builds an externally-provisioned `secretstores.local.file` component via `AddDaprComponent` and passes it to `AddHexalithMemoriesSearchIndexServer`, matching the new signature; committed and pushed to `origin/main` during code review.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: Hexalith.Tenants commit 7453ba5b updates the secret-store call site; Program.cs:124-129 passes the Dapr component builder.

### DW-712: `docs/operations/openbao.md`'s new Story 29.2 passage claims a standalone Dapr self-hosted host (not just Kubernetes) can supply the `openbao-runtime-bootstrap`/`openbao-access-telemetry-bootstrap` bootstrap Secrets that `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` reference via `secretKeyRef`, but this has not been verified against Dapr's actual self-hosted secret-store resolution behavior outside Kubernetes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
location: docs/operations/openbao.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md` summary: `docs/operations/openbao.md`'s new Story 29.2 passage claims a standalone Dapr self-hosted host (not just Kubernetes) can supply the `openbao-runtime-bootstrap`/`openbao-access-telemetry-bootstrap` bootstrap Secrets that `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` reference via `secretKeyRef`, but this has not been verified against Dapr's actual self-hosted secret-store resolution behavior outside Kubernetes. evidence: `secretKeyRef` in a Dapr component's metadata typically resolves through a Kubernetes secret store in k8s-hosted Dapr; a bare self-hosted Dapr install would need its own separately configured secret-store component to resolve those references, which these standalone templates do not define or document. If that mechanism does not work outside Kubernetes as claimed, the "standalone Dapr self-hosted host" deployability statement in `docs/operations/openbao.md` is inaccurate and should be corrected or scoped to Kubernetes only. - ID: 29.2-OPENBAO-SELFHOSTED-SECRETKEYREF-CLAIM - Status: resolved - Source story: spec-29-2-provider-neutral-aspire-composition-and-secret-verification - Target artifact: docs/operations/openbao.md - Re-open trigger: a standalone (non-Kubernetes) Dapr self-hosted deployment is actually exercised and its `secretKeyRef` resolution behavior is verified, positively or negatively, which should replace this caveat with a confirmed statement. - Evidence: `docs/operations/openbao.md`'s Dapr secret boundaries section now scopes the `secretKeyRef` bootstrap resolution explicitly to Kubernetes and marks the standalone self-hosted case as unverified rather than asserting it works, resolved during code review.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: docs/operations/openbao.md:366-375 scopes secretKeyRef bootstrap to Kubernetes and states the standalone resolver requirement.

### DW-714: Classification-gap co-occurring with an active marker defect still suppresses marker-specific `Remediation` entirely.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md` summary: Classification-gap co-occurring with an active marker defect still suppresses marker-specific `Remediation` entirely. evidence: `TenantIsolationVerifier.CheckSemanticIsolationAsync`'s `Remediation` ternary (`src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:389-395`) still selects only the classification-gap sentence whenever a classification gap co-occurs with a marker mismatch in the same `SemanticIsolation` check, giving the operator no inspect/quarantine/named-key guidance for the marker defect at all in that case. Pre-existing behavior, not introduced by Story 24.9; already tracked as ledger item `24.6-F8-W9` below, and Story 24.9's own spec Boundaries ("Ask First") explicitly declined to resolve it here since it is a different axis than this story's AC. Reopen trigger: Story 24.8/24.9 follow-up work touching the same ternary, or an operator report that this suppression hid actionable marker guidance during a real co-occurring incident.
status: done 2026-11-23
archived: 2026-11-23
resolution: VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesAllRemediation and VerifyAsync_LargeSemanticScan_BoundsEachEvidenceCategoryAndKeepsScanning verify provenance guidance for sampled marker defects alongside classification gaps.

### DW-715: `CheckSyntacticIsolationAsync` still returns the anti-template blanket-deletion wording Story 24.9 removed from `SemanticIsolation`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md` summary: `CheckSyntacticIsolationAsync` still returns the anti-template blanket-deletion wording Story 24.9 removed from `SemanticIsolation`. evidence: `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:216` still reads `"Repair or re-provision the tenant RediSearch index and remove mismatched target-prefix hashes"`. Story 24.9's Boundaries explicitly forbid touching the syntactic-only `ScanHashPrefixForTenantFieldMismatchesAsync`/`CheckSyntacticIsolationAsync` path ("a separate, non-semantic check outside this AC's `SemanticIsolation` scope"), so this is pre-existing and correctly out of this story's scope — but it now leaves an inconsistent operator experience: `SemanticIsolation` failures get non-destructive, named-key guidance while `SyntacticIsolation` failures still get blanket-deletion language. Reopen trigger: a follow-up story extending the non-destructive marker-diagnostic pattern to `SyntacticIsolation`.
status: done 2026-11-22
archived: 2026-11-23
resolution: Syntactic verification identifies the affected keys and gives independent guidance for marker, metadata and wrong-type findings. Marker repair requires provenance verification. Evidence: `TenantIsolationVerifierTests.VerifyAsync_SyntacticMarkerDefect_ReportsNamedKeyAndProvenanceGuidance` and `TenantIsolationVerifierTests.VerifyAsync_SyntacticWrongType_ReportsKeyAndContinuesCheckingThePrefix`.

### DW-717: `production-deployment-verification` CI job not wired to the ephemeral EventStore local-feed provisioning script; unconfirmed whether it needs it.

origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-01
location: .github/workflows/ci.yml
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md` summary: the `production-deployment-verification` job in `ci.yml` builds container images via `tools/publish-containers.ps1`, which was not traced this session to confirm whether its own internal `dotnet publish`/`dotnet pack` calls also resolve `Hexalith.EventStore.Client` at the proof-identity version pinned in `references/Hexalith.Builds/Props/Directory.Packages.props`. Every other CI job that restores the full `Hexalith.Memories.slnx` graph was wired to `tools/ci/provision-eventstore-local-feed.sh` (see DW-716 and the spec's Task 2/CI checklist entries) because a plain `dotnet restore`/`dotnet build` against the tracked `NuGet.config` alone fails with `NU1102` for that version; `production-deployment-verification` was left unwired because its build path goes through a PowerShell script rather than a direct `dotnet restore`/`build` step in the YAML, and tracing that script's own restore graph was out of scope for the patch pass that wired the other 7 jobs. evidence: `.github/workflows/ci.yml`'s `production-deployment-verification` job (`Publish local release OCI archives` step, `run: ./tools/publish-containers.ps1 ...`) has no `Provision ephemeral EventStore local NuGet feed` step and no `--configfile`/`RestoreConfigFile` wiring, unlike `build`, `test-unit-contract`, `web-e2e-specimen`, and `integration-fast`. - Re-open trigger: `production-deployment-verification` is observed failing with `NU1102` (or an equivalent EventStore-package restore failure) in CI; or a follow-up story/review traces `tools/publish-containers.ps1`'s restore graph and confirms (or rules out) that it needs the same provisioning.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commits 2f85536d and f7fef9fb replace the ephemeral feed with published Hexalith.EventStore 3.102.0.

### DW-718: Capture and independently review the real C1.15 Production runtime/control-plane identity packet.

origin: Story 27.21 C1.15 producer identity hardening, 2026-09-01
location: artifacts/access-telemetry-c1/C1.15; _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md` summary: Capture a complete immutable C1.15 packet from the approved PG-ONPREM-1 target and obtain independent review without treating producer success as gate acceptance. evidence: Repository fixtures prove fail-closed producer behavior only. The approved target is not currently eligible while either named lifecycle Deployment (`memories-access-telemetry` and `memories-access-telemetry-clock`) remains zero-scaled or the Production inputs lack the explicit `AccessTelemetryLifecycle__ComponentIsAlpha` / `AccessTelemetryLifecycle__AllowAlphaComponent` pair; no real packet or reviewer disposition was produced during this hardening pass. - ID: 27.21-C1.15-REAL-PACKET-REVIEW - Status: open - Source story: 27-21-runtime-and-control-plane-identity - Target artifact: `artifacts/access-telemetry-c1/C1.15` and `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` - Re-open trigger: the approved `jpiquot@local` / `hexalith-memories` target has at least one Ready lifecycle pod and explicit alpha values, allowing the exact operator command to run and an independent reviewer to record the C1.15 disposition. - Rationale: Cluster mutation, scaling, rollout, and synthesized Production evidence are outside this hardening scope; C1.15 therefore remains `pending` / `not complete`, Story 27.21 remains `in-progress`, and Production lifecycle writes remain disabled. Owner: Deployment Adapter Developer for capture; independent code/planning reviewer for disposition. **2026-09-05 reconfirmation:** Fresh read-only checks reconfirmed Production lifecycle Deployments still `replicas: 0` ×2 in `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml`, explicit `AccessTelemetryLifecycle__ComponentIsAlpha` / `AccessTelemetryLifecycle__AllowAlphaComponent` still absent from `deploy/kubernetes/base/access-telemetry-deployments.yaml`, and `kubectl --context jpiquot@local -n hexalith-memories get pods -l app.kubernetes.io/name=memories-access-telemetry` returned no resources; DW-718 capture halted by human decision without cluster or manifest mutation. C1.15 remains `pending` / `not complete`; Story 27.21 remains `in-progress`; no packet was synthesized.
status: done 2026-10-04
archived: 2026-11-23

2026-10-04 authorized capture attempt: the superseding scope in `spec-27-21-runtime-control-plane-identity-6.md` prepared only live `deployment/memories-access-telemetry` at the approved context/namespace with the existing image digest, all three authorized runtime settings `false`, and desired/Ready replicas `1/1`; clock remains at zero. The exact C1.15 command exited 1 and preserved `artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T091704026Z-c37b747735b14e19a30a867e1ac31c3b.json` (SHA-256 `d2c45271e58086e57f8f551ee04363961fd388f8103a5b158d729d8ba0da6d12`, mode `0444`). `secret-shaped-output` blocked the pod-list response before producer exec/metadata/alpha collection. Boolean-only diagnostics identify the unchanged lifecycle credential-placeholder probe templates as matching the producer predicate. Capture is halted without source/manifest changes or bypass; the successfully prepared target remains Ready with writes disabled. Independent review is pending after implementation handoff. DW-718 stays open, C1.15 pending/not complete, and Story 27.21 in-progress; no other gate, write, Story 27.4, or A41 authority advances. Exact command evidence and the review handoff are appended to the execution spec and story record.

2026-10-04 successful recapture after the narrowly authorized producer projection correction: the exact C1.15 command exited 0 and preserved `artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T093016130Z-c77f2729e2e249228379aac843de4b03.json` (SHA-256 `df2fd0177b63d4a7caf69e6ea8c21ace74eabf183545b0e9c8cc82dce81ac5ab`, mode `0444`, 4,429 bytes). Discovery/recheck collect only pod metadata/status with bounded typed-pair parsing and secret scans before projection hashes; the unchanged predicate, full Dapr metadata scan, prior identity/readiness/drift checks, and first immutable blocker are preserved. All required runtime/control-plane identity families were observed; packet gate/write status remains unevaluated and `productionGatePassed: false`. Focused updated pod fixtures passed 9/9; full 30-method verification and independent review are pending. DW-718 remains open until accepted independent real-evidence disposition; story/gate/write/other-gate/Story 27.4/A41 authority is unchanged. The target remains one Ready replica with all three authorized options false. Exact artifact/command/hash evidence is in the execution spec and story record.

2026-10-04 final implementation verification: the complete updated directory-wide C1 fixture lane passed 30/30 methods in 306.537 seconds; syntax, one-story scope, and whitespace checks pass. Both local immutable packet hashes are preserved. Independent accepted disposition remains pending after implementation handoff; DW-718 stays open. Capture and fixture success alone supply no gate/write authority.

historical_resolution: 2026-10-04 — Independent reviewer `/root/review_c1_packet` accepted [the real observed C1.15 packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T093016130Z-c77f2729e2e249228379aac843de4b03.json) (SHA-256 `df2fd0177b63d4a7caf69e6ea8c21ace74eabf183545b0e9c8cc82dce81ac5ab`) through [the immutable independent packet disposition](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-packet-review-20261004T094017772000Z-cc92883d00a0416fa4da1e90f3ff5149.json) (SHA-256 `4a903a708758b2d92355af94c6f21ae1c884a74791e8be1169bd2b8798d23058`, 18,264 bytes, mode `0444`). All six command and ten source hashes and live identity/readiness/disabled settings matched independent verification. DW-718 capture/review is complete; only the C1.15 runtime/control-plane identity capture outcome is accepted/complete. Story 27.21 stays `review` pending final implementation code review. Original unavailable-target and blocker/pending-review notes are historical and retained; the initial immutable blocker is unchanged. Packets remain neutral (`gateStatus: not-evaluated`, `productionGatePassed: false`, `productionLifecycleWrites: not-evaluated`); Production writes remain disabled, other C1 gates/C1.25/human approval are not granted, Story 27.4 is unchanged, and A41 remains open. The original handoff is preserved. Low pre-existing generic source labels are nonblocking and mapped in the immutable review; no producer/cluster/Git mutation accompanies this reconciliation.

historical_field_major_resolution: 2026-10-04 — Independent reviewer `/root/review_c1_packet` accepted [the real observed C1.15 packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T100311363Z-ee01d2344f2b45d6a66b5dce519d0666.json) (SHA-256 `c0bcbb984451fbf1d14cd165016d46dead90cb7dc19341897f38dd8ffa8082ed`) through [the immutable independent packet disposition](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-final-packet-review-20261004T100712521349Z-06c438ae2dad4b7685ff270513adfb78.json) (SHA-256 `8295a00b86ec721d6ec573fc2bdbc9c5940f1b772dfef621d5c907cb2e80ed8c`, 20,021 bytes, mode `0444`). All six command and ten source hashes and live identity/readiness/disabled settings matched independent verification. DW-718 capture/review is complete; only the C1.15 runtime/control-plane identity capture outcome is accepted/complete. Story 27.21 stays `review` pending final implementation code review. Original unavailable-target and blocker/pending-review notes are historical and retained; the initial immutable blocker is unchanged. Packets remain neutral (`gateStatus: not-evaluated`, `productionGatePassed: false`, `productionLifecycleWrites: not-evaluated`); Production writes remain disabled, other C1 gates/C1.25/human approval are not granted, Story 27.4 is unchanged, and A41 remains open. The original handoff is preserved. Low pre-existing generic source labels are nonblocking and mapped in the immutable review; no producer/cluster/Git mutation accompanies this reconciliation. Final evidence binds the corrected collector 030152bd368ccf1fd8d4e577b33cddef8fd8659b2ce7b3a347f9c9638eaac8d7; its complete fixture lane passed31/31 in287.955 seconds. Earlier packet/review remains accepted historical evidence.

resolution: 2026-10-04 — Independent reviewer `/root/review_c1_packet` accepted [the real observed C1.15 packet](../../artifacts/access-telemetry-c1/C1.15/c1.15-runtime-control-plane-identity-20261004T104156676Z-af39ddb5939044f38e438e6e584c3268.json) (SHA-256 `17d7f350c3193ce6663364b0b4e6ef52d8f319f4ca4c0a25e9886a006ff0ed87`) through [the immutable independent packet disposition](../../artifacts/access-telemetry-c1/C1.15/c1.15-independent-framed-packet-review-20261004T104839838701Z-1b6be26b1d604a50bc53eefcc2203278.json) (SHA-256 `8a70077297e68962f12da991d76c37a58a2faa86697ea0e2212f590941e62c73`, 32,097 bytes, mode `0444`). All six command and ten source hashes and live identity/readiness/disabled settings matched independent verification. DW-718 capture/review is complete; only the C1.15 runtime/control-plane identity capture outcome is accepted/complete. Story 27.21 is `done` after full 33-method verification and final implementation code review. Original unavailable-target and blocker/pending-review notes are historical and retained; the initial immutable blocker is unchanged. Packets remain neutral (`gateStatus: not-evaluated`, `productionGatePassed: false`, `productionLifecycleWrites: not-evaluated`); Production writes remain disabled, other C1 gates/C1.25/human approval are not granted, Story 27.4 is unchanged, and A41 remains open. The original handoff is preserved. Low pre-existing generic source labels are nonblocking and mapped in the immutable review; no producer/cluster/Git mutation accompanies this reconciliation. Final evidence binds the per-Pod-framed collector 00e68b0f6b5d9782941b2942eede60af54cac9db8101d19e740e1b89abf138ab and records exact safe argument/output provenance for all five extra disabled/readiness queries; the complete fixture lane passed 33/33 in 497.470 seconds and final code review is complete with pre-existing follow-ups recorded. Earlier packets/reviews remain accepted historical evidence.


### DW-719: Decode and scan the complete Dapr metadata object for secret-shaped values before allowlist projection.

origin: migrated from legacy ledger (""), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md
reason: The pre-existing collector scans raw metadata text and the allowlisted projection, so a Unicode-escaped canary in an unallowlisted property can be decoded and discarded between those checks without blocking; the packet remains secret-safe, but the original secret-shaped-output fail-closed contract is not fully enforced.
status: done 2026-10-03
archived: 2026-11-23
resolution: 2026-10-03 — Implemented `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-4.md`: scan decoded metadata names and string values, including discarded nested objects/arrays, immediately after bounded parsing and before extraction, projection hashing, or the alpha probe. Reuse the unchanged secret predicate and constant `secret-shaped-output` blocker; only the allowlisted projection is hashed. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed 24/24 methods, including encoded canaries/tokens/credential names, safe packets and ledgers, benign projection/hash equality, and bounded invalid metadata. Only DW-719 closes; DW-718 and DW-645 remain open, C1.15 remains `pending` / `not complete`, Story 27.21 remains `in-progress`, the original handoff remains `awaiting-operator`, and no Production gate, lifecycle write, Story 27.4, or A41 status advances.

### DW-720: Two new `deferred-work.md` entries under `spec-24-9` (the syntactic-isolation-wording and classification-gap items added earlier in this same file, immediately preceding this section) use only the legacy free-text `source_spec`/`summary`/`evidence` shape, missing the `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` fields the file's own reformatted schema requires (as used by, e.g., the neighboring `DW-716`-`DW-718` entries and the reformatted `24.7-*` items).

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-09-01)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: Reviewer-confirmed inspection found that the two recent `spec-24-9-non-destructive-tenant-marker-diagnostics` entries used only the legacy free-text `source_spec`/`summary`/`evidence` shape and omitted the ledger schema's `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` fields. This pre-existing schema-migration drift in a shared governance file was authored by Story 24.9 rather than Story 27.3 and was deferred to that session or file owner for reformatting.
status: done 2026-09-01
archived: 2026-11-23
resolution: already resolved: deferred-work.md:4997-5008 gives DW-714 and DW-715 canonical headings, origin, location, reason, and status fields.

### DW-728: `eventstore` AppHost gateway resource cannot start under Memories' mandated SDK 10.0.400 alone — Aspire always launches it via `dotnet run` against the EventStore submodule's own project, which pins SDK 10.0.302.

origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-05, discovered by actually running a new runtime-health test this review added (`tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventStoreAppHostResourceGraphTests.cs`, `EventStoreResource_StartsAndBecomesHealthy`, currently `[Fact(Skip = ...)]`)
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStorePlatformExtensions.cs:92-99 (`AddHexalithEventStoreGatewayProject`); references/Hexalith.EventStore/global.json; src/Hexalith.Memories.AppHost/Program.cs:289-309
source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md`
severity: high
reason: `AddHexalithEventStoreGatewayProject()` unconditionally calls `builder.AddProject<EventStoreProjectMetadata>(eventStoreName)` — a project resource, which Aspire always launches via `dotnet run` against `references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj`, regardless of Memories' own `UseHexalithProjectReferences` package/source mode setting for its EventStore *client* dependency. .NET SDK resolution walks up from that project's own directory and finds `references/Hexalith.EventStore/global.json` (pinned `"version": "10.0.302"`, `"rollForward": "latestPatch"`) before it ever reaches Memories' root `global.json` (`"version": "10.0.400"`). Confirmed live: with only SDK 10.0.400 installed, starting the full AppHost topology and waiting for the `eventstore` resource to become healthy fails — the launched `dotnet run` process logs "Install the [10.0.302] .NET SDK or update [references/Hexalith.EventStore/global.json] to match an installed SDK" and the resource never starts, causing `WaitForResourceHealthyAsync("eventstore", ...)` to throw `DistributedApplicationException`. This means the `eventstore` resource Story 28.1 added exists in the Aspire resource graph (satisfying that AC's narrow letter, and the existing build-only `EventStoreAppHostResourceGraphTests.EventStoreResource_HasExpectedDaprSidecarAndComponentReferences` test) but cannot actually run in any environment following this repo's own SDK 10.0.400 mandate — which per this story's own extensive narrative is meant to be every Memories dev/CI environment. Both obvious fixes are out of this story's own scope: installing SDK 10.0.302 alongside 10.0.400 is an environment/toolchain change, not a code change; editing `references/Hexalith.EventStore/global.json` is forbidden by this spec's own "submodule content is not edited" boundary. Whether a fix exists that doesn't require either (e.g. an EventStore-side Aspire hosting change so the gateway project resource's SDK resolution isn't controlled by the submodule's own `global.json`, or Memories choosing to run the gateway some other way) needs an architecture-level decision, not a unilateral patch.
status: done 2026-09-05
archived: 2026-11-23
resolution: already resolved: Commit 86844b59 and references/Hexalith.EventStore/global.json:3 show the SDK is now pinned to 10.0.400.

