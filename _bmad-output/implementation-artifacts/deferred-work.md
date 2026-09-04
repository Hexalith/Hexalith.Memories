# Deferred Work

### DW-1: `ID:` — unique entry id, exactly as referenced elsewhere (e.g. `12.4-RV6`,

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `ID:` — unique entry id, exactly as referenced elsewhere (e.g. `12.4-RV6`, `S11-FX`, `13.6-RV1`). Tests match the field value as a verbatim token; partial prose mentions and near-matches such as `12x4-RV6` or `112.4-RV6` do not count.
status: open

### DW-2: `Status:` — one of `open`, `resolved`, `accepted`, or `carried-forward`. The

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Status:` — one of `open`, `resolved`, `accepted`, or `carried-forward`. The vocabulary is closed and lowercase. Synonyms such as `done`, `closed`, `fixed`, or `deferred-again` are not allowed and will fail validation.
status: open

### DW-3: `Source story:` — the story key, retro key, or review pass that produced the

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Source story:` — the story key, retro key, or review pass that produced the entry (for example `12-4-baseline-failures-sweep`).
status: open

### DW-4: `Target artifact:` — the repository-relative path or planning artifact the entry

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: tools/test-release.ps1
reason: - `Target artifact:` — the repository-relative path or planning artifact the entry targets. For release-lane baseline entries, this points at the consumer that owns the release filter (typically `tools/test-release.ps1` or a parser test).
status: open

### DW-5: `Re-open trigger:` — one sentence describing the event or evidence that would

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Re-open trigger:` — one sentence describing the event or evidence that would re-open the entry. Required even for `resolved` and `accepted` so a future reviewer knows when the disposition no longer applies.
status: open

### DW-6: One of `Evidence:` or `Rationale:` — `Evidence:` is required when `Status` is

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - One of `Evidence:` or `Rationale:` — `Evidence:` is required when `Status` is `resolved` and names the change (story, commit, test, or doc) that closes the risk; `Rationale:` is required when `Status` is `accepted` or `carried-forward` and explains why the risk remains intentionally.
status: open

### DW-7: `open` — planned action is still needed.

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `open` — planned action is still needed.
status: open

### DW-8: `resolved` — code, test, or documentation evidence shows the risk no longer

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `resolved` — code, test, or documentation evidence shows the risk no longer applies.
status: open

### DW-9: `accepted` — the risk remains but is intentionally accepted with a written

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `accepted` — the risk remains but is intentionally accepted with a written rationale.
status: open

### DW-10: `carried-forward` — the risk remains and has been moved to a named future

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `carried-forward` — the risk remains and has been moved to a named future artifact, story, or trigger.
status: open

### DW-11: `Test:` — fully-qualified `Class.Method` name when the entry is paired with a

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: tools/test-release.ps1
reason: - `Test:` — fully-qualified `Class.Method` name when the entry is paired with a release-lane filter in `tools/test-release.ps1`. Required when `Target artifact` references the release-lane test script.
status: open

### DW-12: Case activity stream append and summary-hash update should be made atomic if duplicate retry side effects become observable.

origin: migrated from legacy ledger ("Story 24.5 Review Deferred Items (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: Case activity stream append and summary-hash update should be made atomic if duplicate retry side effects become observable. evidence: Story 24.5 bounded the stream and added summary backfill for missing legacy summaries, but `XADD` and summary hash updates remain separate Redis operations, so a failure between them can leave a read-model summary temporarily stale until rebuild/backfill. - ID: 24.5-CASE-ACTIVITY-ATOMIC-SUMMARY - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs - Re-open trigger: duplicate case activity appends, stale failed-count/last-activity summaries, or Redis partial-write telemetry are observed after Story 24.5 ships. - Evidence: Review pass found `RecordEventAsync` writes the stream and summary hash separately; Story 24.5 mitigated missing legacy summaries with backfill but did not introduce Lua/transactional atomicity for this projection summary.
status: open

### DW-13: `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story explicitly accepts or reassigns them.
status: open

### DW-14: `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the story explicitly accepts or reassigns them.
status: open

### DW-15: `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals target Story 19.4 unless the story explicitly accepts or reassigns them.
status: open

### DW-16: Other active `open` or `carried-forward` entries are classified by Story 19.1

origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
location: n/a
reason: - Other active `open` or `carried-forward` entries are classified by Story 19.1
status: open

### DW-17: 20.5-A41-ACCESS-TELEMETRY-RETENTION: carried-forward. Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision.

origin: migrated from legacy ledger ("Story 20.5 Deferred Retention Slice (2026-07-04)"), 2026-09-01
location: docs/dev/telemetry.md`, the Story 27.1 architecture decision, the selected access-telemetry sink/storage deployment and purge implementation, and focused lifecycle/tenant-privacy tests, or this entry updated to a complete explicit accepted-debt disposition.
reason: - **20.5-A41-ACCESS-TELEMETRY-RETENTION - carried-forward.** Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision. - ID: 20.5-A41-ACCESS-TELEMETRY-RETENTION - Status: carried-forward - Source story: 20-5-inbound-rate-limiting-quotas-and-audit-completeness - Backlog home: Epic 27, registered Stories 27.1-27.4, plus the held C1 successor definitions in approved Sprint Change Proposal 2026-08-01. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification; all twenty-five C1 gates have no registered story owner; Story 27.4 owns deployment-shaped verification and close-out but remains `backlog` until compliant successor files are later registered and every C1 gate passes on its own evidence. Production lifecycle writes remain disabled and A41 remains open. Scheduling or a held proposal annex does not satisfy the resolution gate. **Corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01.** - Target artifact: `docs/dev/telemetry.md`, the Story 27.1 architecture decision, the selected access-telemetry sink/storage deployment and purge implementation, and focused lifecycle/tenant-privacy tests, or this entry updated to a complete explicit accepted-debt disposition. - Resolution gate: Keep this entry `carried-forward` and the matching sprint action `open` until bounded retention/TTL is implemented, documented, and validated, or accepted debt records a named approver and owner, affected storage/scope, rationale, risk and consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger. - Re-open trigger: Review before any claim that A41 is fully closed, before any production-retention assurance is made, and at the accepted-debt review/expiry trigger if that path is selected. - Rationale: Inbound quotas and audit completeness are implemented in Story 20.5; access telemetry retention remains unaddressed and is carried forward to avoid falsely closing the A41 retention requirement. Owner: operations maintainer / security remediation owner. **Current correction 2026-09-01:** Story 27.21 is the registered `in-progress` owner of C1.15; the remaining twenty-four C1 gates stay unowned. This dated correction changes no A41, Production-write, Story 27.4, or deferred-record status.
status: open
decision: 2026-09-01 Implement bounded retention — Define the approved period and scope, implement purge or TTL behavior, document it, and add privacy verification.
decision: 2026-09-01 Implement bounded retention — Define the approved period and scope, implement purge or TTL behavior, document it, and add privacy verification.

### DW-18: 18.4-REDIS-RACE: accepted. Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs (real-Redis / Aspire-Testcontainers concurrency lane)
reason: - **18.4-REDIS-RACE - accepted.** Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute. - ID: 18.4-REDIS-RACE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs (real-Redis / Aspire-Testcontainers concurrency lane) - Re-open trigger: before any production claim about concurrent ingest, run the real two-thread Redis race wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI. - Rationale: Story 18.4 is substitute-proven by a deterministic winner/loser reservation test and unit-proven today; the real two-thread Redis race is infra-lane-deferred because this sandbox cannot run the Docker/Aspire lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]
status: done 2026-09-01
resolution: resolved by sweep bundle dw-redis-ingest-race-proof
resolution-undo: 359d6619c61ffa57f0fd81f56e167e58a181ed120faa003eb485b6f0fda64b4a 2026-09-01 7374617475733a206f70656e

### DW-19: 18.8-DAPR-SMOKE: accepted. Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests (Dapr-sidecar pub/sub smoke lane over /events/ingest)
reason: - **18.8-DAPR-SMOKE - accepted.** Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute. - ID: 18.8-DAPR-SMOKE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: tests/Hexalith.Memories.IntegrationTests (Dapr-sidecar pub/sub smoke lane over /events/ingest) - Re-open trigger: before any production claim about cross-module event delivery, run the Dapr-sidecar pub/sub smoke wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI. - Rationale: Story 18.8 is proven today by in-process HTTP E2E tests over `/events/ingest`; the Dapr-sidecar smoke is infra-lane-deferred because this sandbox cannot run the sidecar lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.8]
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs:123-134

### DW-20: 18.4-TOKEN-EDGE: accepted. Story 18.4 token-anchoring edge: a token whose first use falls back to a pre-existing `sourceUri` unit relies on the 24h reservation key rather than the permanent dedup record.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs (idempotency-token anchoring path)
reason: - **18.4-TOKEN-EDGE - accepted.** Story 18.4 token-anchoring edge: a token whose first use falls back to a pre-existing `sourceUri` unit relies on the 24h reservation key rather than the permanent dedup record. - ID: 18.4-TOKEN-EDGE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs (idempotency-token anchoring path) - Re-open trigger: a token whose first use falls back to a pre-existing `sourceUri` unit relying on the 24h reservation key (not a permanent record) causes a real dedup/idempotency defect, or a hardening story is scheduled. - Rationale: tokens augment and never replace the permanent source-URI dedup record, so the edge is a known narrow case accepted until it produces a real defect or a hardening story is scheduled. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]
status: open

### DW-21: 12.4-RV20: accepted. Strict literal per-SHA replay evidence is a

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV20 - accepted.** Strict literal per-SHA replay evidence is a release-quality proof candidate, not a runtime defect. Ancestry-based HEAD-inheritance proof remains acceptable for existing close-out evidence; Story 19.3 declines to schedule a strict replay evidence story now. - ID: 12.4-RV20 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A release post-mortem traces a regression to a test that existed at one of the named anchor SHAs but was silently fixed before HEAD, or a release-quality story explicitly requests strict literal replay evidence over ancestry-based proof. - Rationale: Story 19.3 (2026-06-30) reviewed the release-evidence need (AC1) and accepts ancestry-based HEAD-inheritance proof as sufficient for current close-out evidence, declining to create the proposed "Strict Release Baseline Replay Evidence" story until the re-open trigger fires. This governance sweep does not run historical checkout/build/test lanes or mutate release tooling. Owner: release maintainer.
status: open

### DW-22: 12.6-RV5: resolved. The `EmbeddingInputContentKindTests` telemetry

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs
reason: - **12.6-RV5 - resolved.** The `EmbeddingInputContentKindTests` telemetry assertions now use per-test unique tenant ids, tenant-filtered captures, and a thread-safe capture queue, removing the dormant static-meter contamination risk that originally motivated the S11-FA release-lane baseline. - ID: 12.6-RV5 - Status: resolved - Source story: deferred-work-implementation-2026-05-19 - Target artifact: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs - Re-open trigger: `EmbeddingInputContentKindTests` flakes again, or another story adds a concurrent `MemoriesMeter.EmbeddingApiCalls` assertion path that could share static meter captures. - Evidence: The focused telemetry tests now call `UniqueTenantId(...)`, capture only matching `tenant_id` measurements from `MemoriesMeter.EmbeddingApiCalls`, store observations in `ConcurrentQueue<(TenantId, ContentKind, Delta)>`, and assert a single matching metric event per test case.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-23: Story-9.3-ProjectionRegistryCrossCheck: resolved. Handler mismatch

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs; src/Hexalith.Memories.EventStore/ProjectionBinding.cs; src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs; src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs; src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs; tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs
reason: - **Story-9.3-ProjectionRegistryCrossCheck - resolved.** Handler mismatch detection now has a repository-owned projection binding provider contract and emits `ProjectionBindingMissing` only when an authoritative tenant-scoped registry proves a configured route lacks a runtime projection binding. - ID: Story-9.3-ProjectionRegistryCrossCheck - Status: resolved - Source story: 16-1-projection-registry-cross-check-design - Target artifact: src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs; src/Hexalith.Memories.EventStore/ProjectionBinding.cs; src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs; src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs; src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs; tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs - Re-open trigger: A host needs automatic EventStore discovery adaptation, projection liveness or lag evidence, or authoritative registry detection beyond the host-provided tenant boundary. - Evidence: Story 16.1 adds `IProjectionBindingProvider`, default `Unknown` posture, authoritative-only `ProjectionBindingMissing` diagnostics, tenant-scoped deterministic matching, CLI/contract coverage, and operator documentation.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs:178-203

### DW-24: 12.4-RV10: accepted. A parse-time warning for dropped bare-token bullets

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tools/check-story-file-scope.py
reason: - **12.4-RV10 - accepted.** A parse-time warning for dropped bare-token bullets may help story authors, but the current out-of-scope-files diagnostic already catches the issue once a changed file lands outside the parsed allow-list. - ID: 12.4-RV10 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: tools/check-story-file-scope.py - Re-open trigger: A contributor confusion incident or story-template redesign shows that pre-commit author warnings are needed before any changed-file validation runs. - Rationale: The value is low until there is evidence of author confusion, and adding parse-time stderr warnings could create noise for legitimate non-bullet prose.
status: open

### DW-25: 12.4-RV11: accepted. Local Windows absolute-path cosmetic noise remains

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: tools/check-story-file-scope.py
reason: - **12.4-RV11 - accepted.** Local Windows absolute-path cosmetic noise remains intentionally accepted because CI diagnostics use repository-relative paths and do not expose maintainer-visible drive letters. - ID: 12.4-RV11 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: tools/check-story-file-scope.py - Re-open trigger: A PR review comment or release-evidence document cites a local Windows drive-letter path emitted by `tools/check-story-file-scope.py`, or a maintainer reports that pasting story-scope tooling output from a local Windows run into a shared review channel leaks a drive letter. - Rationale: The remaining issue is cosmetic and local-only; changing it now would add story-scope tooling churn without improving CI or reviewer evidence.
status: open

### DW-26: `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1:

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1: `S11-FC` and `12.1-RV4` are resolved, while `12.1-RV3` is accepted with a documented release-maintainer risk decision.
status: open

### DW-27: `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled by Story 15.2 through the provider/model/dimension registry work.
status: open

### DW-28: `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live migration marker enforcement and accepted migration result semantics.
status: open

### DW-29: `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport policy enforcement and operations documentation.
status: done 2026-09-01
resolution: already resolved: commit e68cd2e4

### DW-30: `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator`

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator` structured entry dated 2026-05-12; no new backlog item is created here.
status: done 2026-09-01
resolution: already resolved: commit acfdf211

### DW-31: The Epic 14 retrospective's "Preparation For The Next Work" note is stale

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - The Epic 14 retrospective's "Preparation For The Next Work" note is stale because Epic 15 now exists. This rollup records the reconciliation instead of rewriting retrospective history.
status: open

### DW-32: 15.5-RV1: `git diff --check` validation claim is inaccurate. Story 15.5 Dev Agent Record states `git diff --check ... passed with only expected LF-to-CRLF working-copy warnings`, but actual `git diff --check 9042c17..c2e575c` reports trailing-whitespace errors on `_15-5-review-diff.patch`. Re-open trigger: any future story's validation block reuses the same tolerance wording without verifying `--check` output is genuinely error-free.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV1 — `git diff --check` validation claim is inaccurate.** Story 15.5 Dev Agent Record states `git diff --check ... passed with only expected LF-to-CRLF working-copy warnings`, but actual `git diff --check 9042c17..c2e575c` reports trailing-whitespace errors on `_15-5-review-diff.patch`. Re-open trigger: any future story's validation block reuses the same tolerance wording without verifying `--check` output is genuinely error-free.
status: open

### DW-33: 15.5-RV2: `sprint-status.yaml:last_updated` not advanced when post-implementation commit `c2e575c` landed. Cosmetic drift; the dev-story timestamp `2026-05-15T12:45:15+02:00` predates the 15:55 follow-on commit. Re-open trigger: a tool starts treating `last_updated` as a freshness proxy across all commits on a story.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV2 — `sprint-status.yaml:last_updated` not advanced when post-implementation commit `c2e575c` landed.** Cosmetic drift; the dev-story timestamp `2026-05-15T12:45:15+02:00` predates the 15:55 follow-on commit. Re-open trigger: a tool starts treating `last_updated` as a freshness proxy across all commits on a story.
status: open

### DW-34: 15.5-RV3: Task 3 prose "`13.1-RV6` and related provider work" is looser than the rollup's explicit 4-ID enumeration. `15-5-deferred-register-triage-sweep.md:79` lists one ID; `deferred-work.md:119` enumerates `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, `13.3-RV8`. Rollup is accurate; task description was provisional. Re-open trigger: a future story re-reads Task 3 prose as a complete ownership list and misses one of the four IDs.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV3 — Task 3 prose "`13.1-RV6` and related provider work" is looser than the rollup's explicit 4-ID enumeration.** `15-5-deferred-register-triage-sweep.md:79` lists one ID; `deferred-work.md:119` enumerates `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, `13.3-RV8`. Rollup is accurate; task description was provisional. Re-open trigger: a future story re-reads Task 3 prose as a complete ownership list and misses one of the four IDs.
status: open

### DW-35: 15.5-RV4: `Target artifact:` field uses `;`-joined multi-paths in `12.4-RV20` and `Story-9.3-ProjectionRegistryCrossCheck` blocks. `deferred-work.md:68,92` — the Story 14.5 schema describes `Target artifact:` as singular, but Story 15.4's `13.2-RV4` (line 178) and Story 15.2's HybridSearch entry (line 293) already use multi-path joined formats and the `CiTestInventoryTests` parser tolerates them (48/48 PASS). Pre-existing pattern; a Story-14.5-owned schema-cleanliness pass should either tighten the parser to require single-path values or formalize a multi-path separator. Re-open trigger: a parser regression where the joined string is treated as one literal path and a target-artifact filter misses a real consumer.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV4 — `Target artifact:` field uses `;`-joined multi-paths in `12.4-RV20` and `Story-9.3-ProjectionRegistryCrossCheck` blocks.** `deferred-work.md:68,92` — the Story 14.5 schema describes `Target artifact:` as singular, but Story 15.4's `13.2-RV4` (line 178) and Story 15.2's HybridSearch entry (line 293) already use multi-path joined formats and the `CiTestInventoryTests` parser tolerates them (48/48 PASS). Pre-existing pattern; a Story-14.5-owned schema-cleanliness pass should either tighten the parser to require single-path values or formalize a multi-path separator. Re-open trigger: a parser regression where the joined string is treated as one literal path and a target-artifact filter misses a real consumer.
status: open

### DW-36: 15.4-RV1: Sanitization-message assertions are tautological. `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.

origin: migrated from legacy ledger ("Deferred from: code review of 15-4-token-endpoint-transport-policy (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.4-RV1 — Sanitization-message assertions are tautological.** `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.
status: open

### DW-37: 13.2-RV4: resolved. OIDC token endpoint validation now enforces HTTPS for production and

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.4 Token Endpoint Transport Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs; src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; docs/operations/embedding-providers.md
reason: - **13.2-RV4 - resolved.** OIDC token endpoint validation now enforces HTTPS for production and permits `http://` only for literal local loopback hosts (`localhost`, `127.0.0.1`, and `[::1]`). The same policy is applied before tenant/default config persistence and before direct `IOidcTokenProvider` token acquisition, with sanitized errors that do not echo full endpoint URLs. - ID: 13.2-RV4 - Status: resolved - Source story: 15-4-token-endpoint-transport-policy - Target artifact: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs; src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; docs/operations/embedding-providers.md - Re-open trigger: Any non-loopback `http://` OIDC token endpoint reaches tenant config persistence, direct token acquisition, an outbound token HTTP request, logs, or snapshots without being rejected by the HTTPS/local-loopback policy. - Evidence: Story 15.4 added `OidcTokenProvider.ValidateTokenEndpointTransport(...)`, reused it from `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` for `OidcTokenEndpoint`, documented the production HTTPS/local loopback exception in `docs/operations/embedding-providers.md`, and added focused `OidcTokenProviderTests` plus `EmbeddingProviderDefaultsTests` coverage for accepted loopback HTTP, rejected public/private/link-local/Docker/DNS-alias/127.0.0.2 HTTP, no-request-before-rejection, and non-leaking error text.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs:128-136

### DW-38: 13.6-RV1: resolved. Live migration cutover now writes a durable

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs
reason: - **13.6-RV1 - resolved.** Live migration cutover now writes a durable tenant-scoped active marker before index recreation or tenant config update, and runtime ingestion/indexing reads that marker to block stale provider/model writes for the migrating tenant. - ID: 13.6-RV1 - Status: resolved - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs - Re-open trigger: A production or test migration completes while raw or natural-language semantic hashes for the tenant contain a provider/model/dimensions tuple different from the active migration target after cutover. - Evidence: Story 15.3 added active marker writes in `RedisEmbeddingMigrationStore.StartMigrationMarkerAsync`, read/write guards in `GenerateEmbeddingActivity`, `IndexSemanticActivity`, and `IndexNaturalLanguageSemanticActivity`, plus focused tests proving old-provider generation and raw/NL semantic writes are blocked while the marker is active.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-39: 13.6-RV2: resolved. `IndexSemanticActivity.cs` now carries the standard

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs
reason: - **13.6-RV2 - resolved.** `IndexSemanticActivity.cs` now carries the standard ITANEO MIT copyright header because Story 15.3 touched the file substantively for the mandatory semantic write guard. - ID: 13.6-RV2 - Status: resolved - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs - Re-open trigger: A future hand-written C# source file touched by a story lacks the standard project copyright header. - Evidence: Story 15.3 added the missing copyright header while updating `IndexSemanticActivity` for active migration marker enforcement.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs:1

### DW-40: 13.6-RV3: accepted. The migration command keeps its local nullable

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV3 - accepted.** The migration command keeps its local nullable string/tuple helper shape for `ValidateOptions(...)` and `TryBuildTargetConfig(...)`, with `EmbeddingMigrationResult` plus stable exit codes as the project-approved equivalent for this command surface. - ID: 13.6-RV3 - Status: accepted - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - Re-open trigger: Migration errors need `ApplicationError` metadata beyond a flat operator message, or `Hexalith.Memories.Server` adopts `Hexalith.Commons.ValueOrError<T>` as an approved dependency across this boundary. - Rationale: The helper results are private to `EmbeddingVectorMigrationService`, immediately converted into `EmbeddingMigrationResult`, and already produce automation-readable `Plumbing`, `DomainError`, and `Cancelled` exit codes with sanitized messages. Introducing `Hexalith.Commons.ValueOrError<T>` here would add cross-project reference churn without improving the public migration command contract.
status: open

### DW-41: 13.1-RV6: resolved. Provider validation now has a shared maximum

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV6 - resolved.** Provider validation now has a shared maximum vector-dimension policy and rejects out-of-policy dimensions before any tenant state or index path can consume them. - ID: 13.1-RV6 - Status: resolved - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: A future registry entry accepts a vector dimension above the shared maximum without a story that explicitly raises the storage/memory policy, or a tenant config with `Dimensions = int.MaxValue` reaches persistence/index creation. - Evidence: Story 15.2 added `MaxSupportedDimensions = 16_384` in `EmbeddingProviderDefaults`, validates dimensions before model-specific allowlist checks, and added `EmbeddingProviderDefaultsTests.Validate_DimensionsAboveSharedMaximum_ShouldThrowAtConfigTime`.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:213-259

### DW-42: 13.1-RV10: accepted. Provider and model validation remains

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV10 - accepted.** Provider and model validation remains case-insensitive for compatibility, and caller-provided casing is preserved rather than normalized at validation time. - ID: 13.1-RV10 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: A persisted mixed-case provider/model value causes runtime dispatch, reindex detection, search metadata, or migration state comparisons to diverge. - Rationale: Ollama model tags may be case-sensitive outside the committed `qwen3-embedding:4b` model, so Story 15.2 keeps validation case-insensitive while preserving original values. Evidence is pinned by `EmbeddingProviderDefaultsTests.Validate_MixedCaseProviderAndModel_ShouldUseCaseInsensitiveRegistryLookup`; compatibility consumers continue to use `OrdinalIgnoreCase` where provider/model equality matters.
status: open

### DW-43: 13.1-RV11: resolved. Provider/model/dimension validation now uses a

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV11 - resolved.** Provider/model/dimension validation now uses a closed provider-scoped registry, so cross-pollinated and unknown models fail by construction. - ID: 13.1-RV11 - Status: resolved - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: Any provider/model pair validates without being present in the local registry, or a provider falls back to another provider's defaults, dimensions, models, or rate-limit ceiling. - Evidence: Story 15.2 replaced scattered provider/model/dimension/rate-limit checks with a single local registry in `EmbeddingProviderDefaults` and added `EmbeddingProviderDefaultsTests.Validate_CrossProviderModelPairs_ShouldThrow`, `Validate_UnknownModelForProvider_ShouldThrowAndListProviderModels`, `Validate_SyntacticallyValidButUnregisteredModel_ShouldThrow`, and provider-scoped rate-limit tests.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:46-70

### DW-44: 13.3-RV8: accepted. The persisted provider/model parser continues to

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
reason: - **13.3-RV8 - accepted.** The persisted provider/model parser continues to lowercase the provider and preserve the model string after the first colon. - ID: 13.3-RV8 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs - Re-open trigger: A persisted provider/model identifier with mixed casing fails a real runtime path, migration path, or equality comparison because provider and model casing are handled asymmetrically. - Rationale: Provider names are registry keys and safe to normalize for dispatch, while model tags can contain embedded colons and may be case-sensitive in provider-specific APIs. Story 15.2 pins the behavior with `EmbeddingClientTests.ParseEmbeddingProvider_NormalizesProviderAndPreservesModelAfterFirstColon` and leaves runtime parsing unchanged.
status: open

### DW-45: 15.2-RV1: accepted. AC5 contract-tier serialization test not updated.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs
reason: - **15.2-RV1 - accepted.** AC5 contract-tier serialization test not updated. - ID: 15.2-RV1 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs - Re-open trigger: A casing/canonicalization change at the contract boundary is later considered. - Rationale: Task 2 chose `accepted` for casing semantics, so contract-tier serialization is intentionally unchanged. Recorded for traceability against AC5's "contract/server tests cover ... deferred-work dispositions" wording. Story 19.4 (2026-06-30) re-reviewed the current contract serializer against live code and accepts this until the contract boundary changes: the contract tests still preserve JSON shape and value round-trip, and provider/model validation lives in server/provider paths, not the contract serializer, so no contract-tier change is warranted now. Natural future home: a contract casing/canonicalization story, only if the contract boundary changes. Owner: Contracts / Server maintainer.
status: open

### DW-46: 15.2-RV2: accepted. Actor reindex tests lost same-Provider/different-Model isolation.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs
reason: - **15.2-RV2 - accepted.** Actor reindex tests lost same-Provider/different-Model isolation. - ID: 15.2-RV2 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs - Re-open trigger: Registry adds a second model under Google or Ollama, OR `GetBreakingChangeFields` regresses to flag only provider changes. - Rationale: Tests now switch Provider AND Model (Google→Ollama) instead of Model-only; same-provider/model-change reindex-trigger coverage cannot be restored inside this story because the closed registry currently lists exactly one model per provider. Story 19.4 (2026-06-30) confirmed the closed registry still lists exactly one model per provider (Google + Ollama only), so same-provider/different-model reindex coverage stays trigger-bound and accepted until a supported provider gains a second model or `GetBreakingChangeFields(...)` regresses. Natural future home: provider-registry model-expansion tests when a second model lands under one provider. Owner: Server test maintainer.
status: open

### DW-47: 15.2-RV3: accepted. Other test files still use unregistered model literals.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs, tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs
reason: - **15.2-RV3 - accepted.** Other test files still use unregistered model literals. - ID: 15.2-RV3 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs, tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs - Re-open trigger: The covered code paths add a Validate-preflight, OR a registry-wide test-fixture-hygiene sweep is scheduled. - Rationale: `HybridSearchServiceTests` (line 74) and `TenantEmbeddingConfigEndpointTests` (lines 30, 39) use `"text-embedding-004"` / `"different-model"` literals that compile only because those tests do not call `Validate`. Pre-existing; out of File Scope. Story 19.4 (2026-06-30) accepts this as fixture-hygiene debt: the literals remain harmless until those paths begin validating tenant configs or a registry-wide test-fixture sweep is scheduled. Natural future home: a registry-wide test-fixture hygiene sweep. Owner: Server test maintainer.
status: open

### DW-48: 15.2-RV4: accepted. `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
reason: - **15.2-RV4 - accepted.** `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded. - ID: 15.2-RV4 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs - Re-open trigger: Registry adds a third provider, OR an operator reports a failed identifier parse for a registered provider/model pair. - Rationale: Closed-allowlist behavior of `EmbeddingProviderDefaults` does not extend to `EmbeddingClient.ParseEmbeddingProviderIdentifier` or the dispatch site, which binary-check `IsGoogle || IsOllama`. Architectural follow-up; out of this story's File Scope. Story 19.4 (2026-06-30) verified `EmbeddingClient` still dispatches via `IsGoogle`/`IsOllama` and parses only those two providers; this is safe while the registry holds two providers and stays accepted until a third provider is added or an operator reports a failed parse for a registered pair. It is the strongest implement-now cluster with `15.2-RV5` and `15.2-RV6` (runtime dispatch, persisted identifier casing, migration target selection) and must be solved together, not in isolation. Natural future home: a provider-runtime dispatch abstraction story for a third provider. Owner: Server / ingestion maintainer.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:79-123

### DW-49: 15.2-RV5: accepted. `GenerateEmbeddingActivity` may emit mixed-case provider in persisted identifier.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs
reason: - **15.2-RV5 - accepted.** `GenerateEmbeddingActivity` may emit mixed-case provider in persisted identifier. - ID: 15.2-RV5 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs - Re-open trigger: A tenant persists `Provider = "Google"` (mixed case, now accepted by Validate) and an equality comparison or migration state diverges from the lowercased parsed form. - Rationale: Tenant-persisted casing is preserved, but `ParseEmbeddingProviderIdentifier` lowercases the provider on read — write and read forms can diverge. Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `GenerateEmbeddingActivity` writes the raw `$"{config.Provider}:{config.Model}"` form while the parser lowercases the provider, and the migration marker guard compares with `OrdinalIgnoreCase`, so this is accepted compatibility today, not a current defect. Accepted until a casing-sensitive equality or migration-state divergence is observed. Natural future home: a provider identifier canonicalization story covering write/read/migration equality (bundled with `15.2-RV4`/`15.2-RV6`). Owner: Server / ingestion maintainer.
status: open

### DW-50: 15.2-RV6: accepted. Migration tool uses binary Google/Ollama coin-flip, not registry.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **15.2-RV6 - accepted.** Migration tool uses binary Google/Ollama coin-flip, not registry. - ID: 15.2-RV6 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - Re-open trigger: Registry adds a third provider, OR a migration plan needs to target a new provider. - Rationale: `TargetProvider` defaults to `Ollama()` if not Google (lines 125-149). Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `EmbeddingVectorMigrationService.TryBuildTargetConfig(...)` still chooses Google defaults for a Google target and Ollama defaults otherwise, then relies on `EmbeddingProviderDefaults.Validate(...)` to reject unsupported providers — safe for two providers but a binary defaulting path, accepted until a third provider or a new migration target lands. Natural future home: a migration target factory/registry story for a third provider (bundled with `15.2-RV4`/`15.2-RV5`). Owner: Server / migration maintainer.
status: open

### DW-51: 15.2-RV7: accepted. Whitespace-prefixed provider not trimmed before registry lookup.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **15.2-RV7 - accepted.** Whitespace-prefixed provider not trimmed before registry lookup. - ID: 15.2-RV7 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: Operator reports an unhelpful "Provider ' google' is not supported" error and the registry path needs to suggest the whitespace cause. - Rationale: `ArgumentException.ThrowIfNullOrWhiteSpace` accepts `" google"`, then `FindProvider` misses (no trim). Already family-deferred as `13.1-RV4`; surfaces again in the registry path. Story 19.4 (2026-06-30) accepts this until an operator UX issue justifies whitespace-specific diagnostics in `EmbeddingProviderDefaults.Validate(...)`. Natural future home: a provider validation UX cleanup if whitespace diagnostics become operator-visible. Owner: Server / ingestion maintainer.
status: open

### DW-52: 15.2-RV8: accepted. Already-persisted invalid configs not surfaced on read.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs
reason: - **15.2-RV8 - accepted.** Already-persisted invalid configs not surfaced on read. - ID: 15.2-RV8 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs - Re-open trigger: Operator needs visibility into tenants whose persisted config no longer validates under the closed registry. - Rationale: Closed-registry validation runs on write only — tenants whose state was valid under loose rules continue to be served. Story 15.2 documents this as intentional compatibility behavior; operator-visibility design is a follow-up. Story 19.4 (2026-06-30) accepts this until operator visibility for already-persisted invalid configs is explicitly needed; per the scope boundary, no read-time tenant-config rejection is added without an approved operator remediation path. Natural future home: an operator visibility/remediation story for persisted invalid configs. Owner: Server / ingestion maintainer.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:103-116

### DW-53: 15.2-RV9: accepted. "Order-sensitive metric test passed in isolation" acknowledged.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests (test ordering)
reason: - **15.2-RV9 - accepted.** "Order-sensitive metric test passed in isolation" acknowledged. - ID: 15.2-RV9 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests (test ordering) - Re-open trigger: The order-sensitive metric test fails intermittently in CI, OR a test-isolation sweep is scheduled. - Rationale: Acknowledged in the Dev Agent Record but not fixed; not caused by this story. Story 19.4 (2026-06-30) accepts this until the order-sensitive metric test flakes in CI or a test-isolation sweep is scheduled. Natural future home: a test-isolation sweep if the metric test flakes. Owner: Server test maintainer.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-54: S11-FC: resolved. Release execution now has a repository-owned stale-tag

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **S11-FC - resolved.** Release execution now has a repository-owned stale-tag preflight before `npx semantic-release`. The script obtains the next version from semantic-release dry-run output, applies `.releaserc.json` `tagFormat: "v${version}"`, and checks exact local and remote refs before prepare or publish hooks can run. - ID: S11-FC - Status: resolved - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: semantic-release output changes so `tools/release-preflight.ps1` can no longer parse the next release version, or a release post-mortem shows a stale tag reached the publish-capable `npx semantic-release` step. - Evidence: Story 15.1 added `tools/release-preflight.ps1`, wired `.github/workflows/release.yml` to run it before `npx semantic-release`, and added `tests/tooling/release_preflight/release_preflight_test.py` coverage for no tag, local-only collision, remote-only collision, matching local/remote collision path, no-release dry-run output, and similarly prefixed non-colliding refs.
status: done 2026-09-01
resolution: already resolved: commit 3445e66b

### DW-55: 12.1-RV3: accepted. The repository removed its partial job-level

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV3 - accepted.** The repository removed its partial job-level `github.event.head_commit.message` skip parser and documents GitHub's native push skip handling as the release contract. The remaining edge is accepted because a workflow skipped by GitHub before job creation cannot run an in-workflow repository validator. - ID: 12.1-RV3 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: docs/dev/release-runbook.md - Re-open trigger: first silently skipped release caused by a bracketed skip instruction in a release-eligible merge/squash commit message, or GitHub exposes a pre-job policy hook that can reject such commits before native skip handling suppresses the workflow. - Rationale: Release maintainer ownership remains with the final merge/squash message author and reviewer. Story 15.1 makes the outcome predictable by removing the repository's partial parser, adding `CiTestInventoryTests.ReleaseWorkflow_ReleaseJob_DoesNotUseHeadCommitSkipCondition`, and documenting that bracketed skip instructions anywhere in the final commit message can suppress release. Accepted until 2026-08-13 unless the re-open trigger fires sooner.
status: done 2026-09-01
decision: 2026-09-01 Renew accepted risk — Re-accept the limitation with an owner, review control, and new review date.
resolution: closed by human decision: Re-accept the limitation with an owner, review control, and new review date.
decision: 2026-09-01 Renew accepted risk — Re-accept the limitation with an owner, review control, and new review date.

### DW-56: 12.1-RV4: resolved. The release restore contract is now explicitly

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
location: package-lock.json
reason: - **12.1-RV4 - resolved.** The release restore contract is now explicitly verified: `package-lock.json` is tracked, matches root `package.json` for `npm ci`, and the workflow installs release tooling through `npm ci`. - ID: 12.1-RV4 - Status: resolved - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: package-lock.json - Re-open trigger: `npm ci --ignore-scripts` fails from an isolated checkout/worktree, `package-lock.json` is removed from git tracking, or `.github/workflows/release.yml` stops using `npm ci` for release tooling restore. - Evidence: Story 15.1 confirmed `git ls-files -- package-lock.json package.json` lists both files, added `CiTestInventoryTests.ReleaseWorkflow_InstallReleaseTooling_UsesNpmCi`, documented the `npm ci` lockfile contract in `docs/dev/release-runbook.md`, and validated the fresh-clone-style restore with `npm ci --ignore-scripts` in an isolated worktree. Fresh-clone proof: `npm ci --ignore-scripts` run in working directory `D:\Hexalith.Memories` after deleting any pre-existing `node_modules/`; the command resolved the tracked `package-lock.json` against `package.json` without writing back to either file; post-run `git status -- package-lock.json package.json` reported zero changes.
status: done 2026-09-01
resolution: already resolved: .github/workflows/release.yml:83

### DW-57: 15.1-RV1: accepted. Transient network failure in `Test-RemoteTagCollision` aborts the release lane.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV1 - accepted.** Transient network failure in `Test-RemoteTagCollision` aborts the release lane. - ID: 15.1-RV1 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release attempt fails because `git ls-remote` returns a transient network/DNS error and the preflight has no retry/backoff. - Rationale: The preflight currently has no retry. A DNS hiccup turns a recoverable error into a hard abort. Deferred because the right policy (number of retries, backoff window, idempotency boundary) is a release-owner decision rather than a clear patch. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no release has failed on it; the retry policy is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open

### DW-58: 15.1-RV2: accepted. Dry-run version regex hard-codes English semantic-release output.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV2 - accepted.** Dry-run version regex hard-codes English semantic-release output. - ID: 15.1-RV2 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: semantic-release ever rewords `The next release version is X.Y.Z` or ships i18n output that no longer matches the regex, and the preflight starts mis-detecting the next version. - Rationale: Bound to current semantic-release output. A more stable contract would be `semantic-release --dry-run --debug` JSON or a plugin hook, but switching is out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open

### DW-59: 15.1-RV3: accepted. Final `catch` block loses inner-exception and stack trace.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV3 - accepted.** Final `catch` block loses inner-exception and stack trace. - ID: 15.1-RV3 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release failure investigation requires the original stack/inner exception and the operator only has the truncated `Write-Error -Message $_.Exception.Message` output. - Rationale: `Write-Error -Message $_.Exception.Message` discards inner exception and stack. Switch to `Write-Error -ErrorRecord $_` or `$_.Exception.ToString()` when a future release post-mortem proves the loss matters. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open

### DW-60: 15.1-RV4: accepted. `CiTestInventoryTests` workflow-string assertions are brittle to cosmetic edits.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **15.1-RV4 - accepted.** `CiTestInventoryTests` workflow-string assertions are brittle to cosmetic edits. - ID: 15.1-RV4 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A future workflow edit needs `npm ci --ignore-scripts`, a renamed step name, or `pwsh ./tools/release-preflight.ps1` invocation form, and the existing strict `ShouldBe` assertions fail without a real contract violation. - Rationale: Exact-match `ShouldBe` for `Run`, `Name`, and `Shell` is consistent with the rest of `CiTestInventoryTests`. Loosening to `ShouldContain`/`ShouldStartWith` should be done as a sweep across the file, not in isolation. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-61: 15.1-RV5: accepted. Windows tempdir cleanup can raise `PermissionError`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV5 - accepted.** Windows tempdir cleanup can raise `PermissionError`. - ID: 15.1-RV5 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: CI or a Windows developer hits intermittent `PermissionError` on tempdir cleanup because git keeps an index lock open when the test ends. - Rationale: Tests currently pass locally and in CI. Switching to `tempfile.TemporaryDirectory(ignore_cleanup_errors=True)` (Py 3.10+) is a one-line hardening but Story 15.1 has no evidence the path manifests yet. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-62: 15.1-RV6: accepted. `Path | None` union syntax requires Python 3.10+.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV6 - accepted.** `Path | None` union syntax requires Python 3.10+. - ID: 15.1-RV6 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A contributor runs the test on Python 3.9 (or the project lowers its minimum) and gets a `TypeError` on test collection. - Rationale: Current CI runs Python 3.11+. Lowering to `Optional[Path]` would broaden compatibility but is not needed today. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-63: 15.1-RV7: accepted. Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV7 - accepted.** Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output. - ID: 15.1-RV7 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A test runner uses a non-UTF-8 codepage and `pwsh` stderr contains non-ASCII characters, raising `UnicodeDecodeError` on `subprocess.run(..., text=True)`. - Rationale: Pass `encoding='utf-8', errors='replace'` to `subprocess.run`. Deferred as low-impact hardening; CI codepages are UTF-8. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-64: 15.1-RV8: accepted. `git init` default branch depends on host `init.defaultBranch`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV8 - accepted.** `git init` default branch depends on host `init.defaultBranch`. - ID: 15.1-RV8 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A future test that relies on the default branch name (rather than just tags) fails on a runner with a non-standard `init.defaultBranch` value. - Rationale: Current tests only push tags, so the default-branch name is irrelevant. Adding `--initial-branch=main` would future-proof the helper. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-65: 15.1-RV9: accepted. Test runner hardcodes `pwsh` without availability guard.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV9 - accepted.** Test runner hardcodes `pwsh` without availability guard. - ID: 15.1-RV9 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A non-Windows developer environment without PowerShell 7 runs `pytest`/`unittest discover` and sees a confusing `FileNotFoundError` instead of a clear skip. - Rationale: Add `@unittest.skipUnless(shutil.which('pwsh'), 'pwsh required')`. Deferred because CI and dev environments today all have pwsh. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-66: 15.1-RV10: accepted. Runbook release-day checklist renumbered to 17 items; other docs may reference old step numbers.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **15.1-RV10 - accepted.** Runbook release-day checklist renumbered to 17 items; other docs may reference old step numbers. - ID: 15.1-RV10 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: docs/dev/release-runbook.md - Re-open trigger: A maintainer follows a stale "see step 7" cross-reference in CONTRIBUTING.md or another doc that no longer matches the renumbered checklist. - Rationale: A repo-wide `rg "step 7|step 8|step 9" docs/` sweep is warranted but out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: open

### DW-67: 15.1-RV11: accepted. `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: - **15.1-RV11 - accepted.** `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path. - ID: 15.1-RV11 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md - Re-open trigger: The preflight script is renamed or relocated and the `S11-FC` re-open trigger silently fails to reference the right artifact. - Rationale: Use a more stable artifact phrasing like "the repository-owned release preflight script in `tools/`" if the script ever moves. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: open

### DW-68: 15.1-RV12: accepted. `12.1-RV3` accepted-until 2026-08-13 has no automated reminder.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: - **15.1-RV12 - accepted.** `12.1-RV3` accepted-until 2026-08-13 has no automated reminder. - ID: 15.1-RV12 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md - Re-open trigger: Accepted-until date `2026-08-13` passes with no review surfacing the expired entry. - Rationale: No infrastructure today surfaces expired `accepted` entries. A scheduled check would close the gap. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: open

### DW-69: 15.1-RV13: accepted. `git show-ref --verify` allowed exit codes do not cover `128`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV13 - accepted.** `git show-ref --verify` allowed exit codes do not cover `128`. - ID: 15.1-RV13 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release attempt fails with the generic "git failed with exit code 128" wrapper because the ref store is corrupt or otherwise unreadable. - Rationale: A clearer "ref-state probe failed" diagnostic would shorten release-day investigation. Current wrapper is acceptable for the common case. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open

### DW-70: 15.1-RV14: accepted. Peeled-only-ref remote response not fixtured.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/tooling/release_preflight/release_preflight_test.py
reason: - **15.1-RV14 - accepted.** Peeled-only-ref remote response not fixtured. - ID: 15.1-RV14 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/tooling/release_preflight/release_preflight_test.py - Re-open trigger: A real remote returns only peeled refs (`refs/tags/vX.Y.Z^{}`) without the unpeeled entry and the contract is not test-fixtured. - Rationale: The script accepts either, but no fixture proves the peeled-only path. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-71: 15.1-RV15: accepted. `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV15 - accepted.** `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing. - ID: 15.1-RV15 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A caller invokes the script with a stale or wrong `-RepositoryPath` and gets a generic `Cannot find path` error instead of an actionable message. - Rationale: Pre-check with `Test-Path -PathType Container` and throw a script-owned message. Minor UX improvement. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open

### DW-72: 15.1-RV16: accepted. `GetReleaseWorkflowJobScalar` depends on 4-space indentation.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **15.1-RV16 - accepted.** `GetReleaseWorkflowJobScalar` depends on 4-space indentation. - ID: 15.1-RV16 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A future `release.yml` reformat (2-space, tabs) silently passes the job-scalar contract test without actually inspecting the right scope. - Rationale: Structural YAML parsing would be ideal, but a hand-rolled prefix parser is consistent with the rest of `CiTestInventoryTests`. A broader test-helper sweep is the natural home. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: open

### DW-73: 13.7-RV4: resolved. The AppHost and Aspire integration fixture now share the

origin: migrated from legacy ledger ("Closed by: Deferred Work 13.7-RV4 Repository Root Locator Consolidation (2026-05-12)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/RepositoryRootLocator.cs
reason: - **13.7-RV4 — resolved.** The AppHost and Aspire integration fixture now share the AppHost-owned `RepositoryRootLocator` helper instead of maintaining duplicate `ResolveRepositoryRoot` implementations. The helper walks upward from the current directory and `AppContext.BaseDirectory`, fails closed when `Hexalith.Memories.slnx` is not found, and has focused unit coverage for both nested-directory discovery and missing-marker failure. - ID: 13.7-RV4 - Status: resolved - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide - Target artifact: src/Hexalith.Memories.AppHost/RepositoryRootLocator.cs - Re-open trigger: a third repository-root locator is introduced outside `RepositoryRootLocator`, or either AppHost startup or Aspire integration tests drift to a different root-discovery contract. - Evidence: Deferred-work implementation on 2026-05-12 added `RepositoryRootLocator`, replaced the AppHost and fixture helper copies with calls to it, and added `RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory` plus `RepositoryRootLocator_MissingMarker_Throws`.
status: done 2026-09-01
resolution: already resolved: commit acfdf211

### DW-74: 12.4-RV6: resolved. `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV6 — resolved.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` no longer classifies entries by substring scans for `baseline`, `test-release.ps1`, or `release lane`. The new `ParseStructuredDeferredEntries` reader matches anchored field labels (`ID:`, `Status:`, `Source story:`, `Target artifact:`, `Re-open trigger:`, `Evidence:` / `Rationale:`, optional `Test:`), and `ReadOpenDeferredBaselines` reports only entries whose `Target artifact` references the release-lane script with `Status: open`. - ID: 12.4-RV6 - Status: resolved - Source story: 12-4-baseline-failures-sweep - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: any future change that reintroduces prose-substring classification of baseline-related deferred entries, or a parser regression where unrelated narrative mentions of `baseline` / `release lane` are once again counted as baseline filters. - Evidence: Story 14.5 replaced the substring-driven `baselineRelated` / `HasReleaseFilter` classifier with field-aware parsing; new fixture tests prove that prose mentions of `baseline`, `release lane`, and `test-release.ps1` in non-structured entries do not trigger baseline classification.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-75: 12.4-RV19: resolved. The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV19 — resolved.** The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a literal trailing period) is replaced by reading the structured `ID:` field verbatim. The new parser accepts any ID token that the schema admits and rejects near-matches such as `12x4-RV6` or `112.4-RV6` exactly because field equality is enforced after extraction. - ID: 12.4-RV19 - Status: resolved - Source story: 12-4-baseline-failures-sweep - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: a future deferred-work format change that adds new ID shapes (lowercase, em-dash, alternate suffix punctuation) without exercising them in `CiTestInventoryTests` fixtures. - Evidence: Story 14.5 deleted `DeferredKeyRegex` and now resolves IDs from the structured `ID:` field. Fixture tests cover `12.4-RV6`, `S11-FX`, lowercase / mixed-case rejection, and exact-ID boundaries against `12x4-RV6` and `112.4-RV6`.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-76: 12.6-RV2: resolved. This entry explicitly realized 12.4-RV6 and is closed

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.6-RV2 — resolved.** This entry explicitly realized 12.4-RV6 and is closed by the same parser change. The unconditional `ShouldBeEmpty` assertion now rests on the structured-field reader rather than substring heuristics, so a prose-only edit to an unrelated entry (for example renaming "release pipeline" to "release lane") cannot flip an entry's classification. - ID: 12.6-RV2 - Status: resolved - Source story: 12-6-embedding-input-content-kind-baseline-resolution - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: a parser regression that reintroduces substring-based baseline classification, or a future story that adds a new release-lane filter without a paired structured deferred-work entry. - Evidence: closed alongside 12.4-RV6 by the Story 14.5 structured-field parser; new fixture test `ReadOpenDeferredBaselines_NarrativeMentionsBaseline_NotMisclassified` proves prose mentions are no longer load-bearing for classification.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-77: 13.7-RV5: resolved. Sprint-status history hygiene is now a documented

origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
location: CONTRIBUTING.md
reason: - **13.7-RV5 — resolved.** Sprint-status history hygiene is now a documented forward-looking convention in `CONTRIBUTING.md`. Future status entries should use short dated breadcrumbs that link to the relevant story artifact, deferred entry, run log, or review document instead of accumulating multi-sentence evidence on a single YAML line. Historical Epic 1-13 history comments are intentionally not rewritten — that cleanup remains out of scope per the Story 14.5 dev notes. - ID: 13.7-RV5 - Status: resolved - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide - Target artifact: CONTRIBUTING.md - Re-open trigger: a future parser, dashboard, or auditor that fails on the long historical YAML lines and proves a targeted edit to specific entries is required, or a contributor-process change that takes ownership of bulk sprint-status history rewriting. - Evidence: Story 14.5 added the "Sprint Status History Conventions" section to `CONTRIBUTING.md`; the Epic 14 bookkeeping rules in the same file require future Epic 14 stories to point at story artifacts and deferred IDs rather than appending narratives on the YAML status line.
status: done 2026-09-01
resolution: already resolved: CONTRIBUTING.md:201-221

### DW-78: 13.6-RV4: closed. `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV4 — closed.** `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-79: 13.6-RV5: closed. Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV5 — closed.** Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-80: 13.7-RV1: closed. `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV1 — closed.** `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-81: 13.7-RV2: closed. Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV2 — closed.** Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:144-148

### DW-82: 13.7-RV3: closed. `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV3 — closed.** `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1819

### DW-83: 13.7-RV6: closed. `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV6 — closed.** `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs:235-247

### DW-84: 13.7-RV7: closed. The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.

origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV7 — closed.** The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:35

### DW-85: 13.6-RV1: carried forward. Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.6-RV1 — carried forward.** Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-86: 13.6-RV3: carried forward. `EmbeddingVectorMigrationService` retains string-shaped error returns from `ValidateOptions` and `TryBuildTargetConfig` and routes all operator-visible failures through the structured `EmbeddingMigrationResult` surface. Adopting Hexalith's `ValueOrError<T>` convention requires a project reference to `Hexalith.Commons` (`src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` + `ApplicationError.cs`), which is in this story's forbidden-by-default file scope and would cascade through `Hexalith.Memories.Server`'s reference graph. The internal helpers feed exactly one consumer (the orchestrator) which immediately wraps each message into the public `EmbeddingMigrationResult`, so the local string shape is structurally equivalent to a `ValueOrError<T>` for this surface. Re-open trigger: a Hexalith-wide audit of result-pattern adoption that drops the `Hexalith.Commons` cross-project boundary, or any feature that needs to surface migration errors with `ApplicationError`'s richer shape (Title/Detail/TechnicalDetail/Arguments/Category) rather than a flat operator sentence.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs
reason: - **13.6-RV3 — carried forward.** `EmbeddingVectorMigrationService` retains string-shaped error returns from `ValidateOptions` and `TryBuildTargetConfig` and routes all operator-visible failures through the structured `EmbeddingMigrationResult` surface. Adopting Hexalith's `ValueOrError<T>` convention requires a project reference to `Hexalith.Commons` (`src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` + `ApplicationError.cs`), which is in this story's forbidden-by-default file scope and would cascade through `Hexalith.Memories.Server`'s reference graph. The internal helpers feed exactly one consumer (the orchestrator) which immediately wraps each message into the public `EmbeddingMigrationResult`, so the local string shape is structurally equivalent to a `ValueOrError<T>` for this surface. Re-open trigger: a Hexalith-wide audit of result-pattern adoption that drops the `Hexalith.Commons` cross-project boundary, or any feature that needs to surface migration errors with `ApplicationError`'s richer shape (Title/Detail/TechnicalDetail/Arguments/Category) rather than a flat operator sentence.
status: open

### DW-87: 13.7-RV4: resolved 2026-05-12. Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.7-RV4 — resolved 2026-05-12.** Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.
status: done 2026-09-01
resolution: already resolved: commit acfdf211

### DW-88: 13.2-RV1: closed. `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV1 — closed.** `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-89: 13.2-RV2: closed. `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: tools/MigrateEmbeddingVectors/Program.cs
reason: - **13.2-RV2 — closed.** `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-90: 13.2-RV3: closed. `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV3 — closed.** `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-91: 13.2-RV5: closed. `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV5 — closed.** `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-92: 13.2-RV6: closed. Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.2-RV6 — closed.** Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-93: 13.3-RV6: closed. Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV6 — closed.** Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-94: 13.3-RV7: closed. `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV7 — closed.** `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-95: 13.3-RV11: closed. `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV11 — closed.** `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-96: 13.3-RV12: closed. `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV12 — closed.** `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-97: 13.3-RV14: closed. `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV14 — closed.** `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-98: 13.3-RV15: closed. `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.3-RV15 — closed.** `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-99: 13.4-RV5: closed. `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.

origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **13.4-RV5 — closed.** `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-100: 13.6-RV1: Concurrent ingestion racing the migration. `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV1 — Concurrent ingestion racing the migration.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-101: 13.6-RV2: Pre-existing missing copyright header. `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs
reason: - **13.6-RV2 — Pre-existing missing copyright header.** `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs:1

### DW-102: 13.6-RV3: Migration tool surfaces use ad-hoc string error returns + exit codes rather than `ValueOrError<T>`. `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — `ValidateOptions` returns `string?`; result construction is manual. Hexalith convention prefers `ValueOrError<T>` for expected business failures. Re-open trigger: a refactor of the migration service surface, or a Hexalith-wide audit of result-pattern adoption.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV3 — Migration tool surfaces use ad-hoc string error returns + exit codes rather than `ValueOrError<T>`.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — `ValidateOptions` returns `string?`; result construction is manual. Hexalith convention prefers `ValueOrError<T>` for expected business failures. Re-open trigger: a refactor of the migration service surface, or a Hexalith-wide audit of result-pattern adoption.
status: open

### DW-103: 13.6-RV4: Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs
reason: - **13.6-RV4 — Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-104: 13.6-RV5: Redactor skips `client_secret named foo` style strings without `:` or `=` separator. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs
reason: - **13.6-RV5 — Redactor skips `client_secret named foo` style strings without `:` or `=` separator.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-105: 13.5-RV1: `Hexalith.EventStore` submodule pointer bump bundled in feat commit. Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. Process note: future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: Hexalith.Commons/_bmad-output/project-context.md:99
reason: - **13.5-RV1 — `Hexalith.EventStore` submodule pointer bump bundled in feat commit.** Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. **Process note:** future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.
status: open

### DW-106: 13.5-RV2: AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline. `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130
reason: - **13.5-RV2 — AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline.** `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs:145-168

### DW-107: 13.5-RV3: No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119` — existing Model/Dimensions breaking-change coverage uses `EmbeddingProviderDefaults.Google()` only; Ollama-specific `Validate(...)` ceilings (qwen3 dim lock at 2560, rate-limit ceiling 60_000) are exercised in `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dim/provider breaking-change matrix grows.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119
reason: - **13.5-RV3 — No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119` — existing Model/Dimensions breaking-change coverage uses `EmbeddingProviderDefaults.Google()` only; Ollama-specific `Validate(...)` ceilings (qwen3 dim lock at 2560, rate-limit ceiling 60_000) are exercised in `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dim/provider breaking-change matrix grows.
status: open

### DW-108: 13.5-RV4: Legacy `provider="ollama"` payload with missing OIDC fields not exercised. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496
reason: - **13.5-RV4 — Legacy `provider="ollama"` payload with missing OIDC fields not exercised.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.
status: open

### DW-109: 13.5-RV5: Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219
reason: - **13.5-RV5 — Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.
status: open

### DW-110: 13.5-RV6: `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381
reason: - **13.5-RV6 — `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.
status: open

### DW-111: 13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:39-46,54-70
reason: - **13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:39-46,54-70` — 4-arg ctor delegates to 5-arg with `null`; 5-arg also has `IOidcTokenProvider? = null` default. MS DI does not honor C# default values, so the 4-arg overload is currently necessary. Remove the redundant default on the 5-arg side at next refactor. Re-open trigger: Story 13.7 wires `IOidcTokenProvider` into DI and the constructor surface is touched again.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-112: 13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:483-495
reason: - **13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:483-495` — Short tokens or short input text could mask coincidental substrings of the upstream JSON; longer tokens with overlapping substrings get clobbered. Apply a length floor and order-by-length-descending replacement. Re-open trigger: a real-world incident where a redacted exception body becomes unreadable, or a security review.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-113: 13.3-RV8: Asymmetric provider/model casing in parser output. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146` — Provider lowercased, model preserved verbatim. May be intentional (Ollama tags can be case-sensitive). Re-open trigger: Story 13.4 / 13.5 introduces a persisted-config consumer that needs round-trip equality.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146
reason: - **13.3-RV8 — Asymmetric provider/model casing in parser output.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146` — Provider lowercased, model preserved verbatim. May be intentional (Ollama tags can be case-sensitive). Re-open trigger: Story 13.4 / 13.5 introduces a persisted-config consumer that needs round-trip equality.
status: open

### DW-114: 13.3-RV9: No per-tenant circuit-breaker on persistent OIDC 401s. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234` — AC5 mandates "exactly once" per request. Across many requests with a misconfigured client, each call still hits the IdP. Re-open trigger: a production incident where Keycloak traffic spikes correlate with embedding 401 storms.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234
reason: - **13.3-RV9 — No per-tenant circuit-breaker on persistent OIDC 401s.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234` — AC5 mandates "exactly once" per request. Across many requests with a misconfigured client, each call still hits the IdP. Re-open trigger: a production incident where Keycloak traffic spikes correlate with embedding 401 storms.
status: open

### DW-115: 13.3-RV10: No 429/Retry-After test on the Ollama path. `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs
reason: - **13.3-RV10 — No 429/Retry-After test on the Ollama path.** `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.
status: open

### DW-116: 13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:291-297
reason: - **13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:291-297` — Accidentally-added positional argument silently becomes a "sensitive value". Replace `params` with an explicit `IReadOnlyList<string?>` for security-critical parameters. Re-open trigger: any new caller of `HandleEmbeddingResponseAsync`, or a new sensitive value to redact.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-117: 13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:285
reason: - **13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:285` — Interface does not enforce non-blank token. Current `OidcTokenProvider` validates; future provider implementation could return whitespace and crash with `FormatException`. Re-open trigger: a third-party `IOidcTokenProvider` is added, or the interface is opened to non-Hexalith implementations.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-118: 13.3-RV13: `BaseUrl` with query string or fragment silently dropped. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261
reason: - **13.3-RV13 — `BaseUrl` with query string or fragment silently dropped.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:352-395

### DW-119: 13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:216-218
reason: - **13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:216-218` — `OidcTokenAcquisitionException`, `HttpRequestException`, `TaskCanceledException` leak past the EmbeddingClient boundary. Mirrors deferred 13.2-RV3. Re-open trigger: a 401-retry production incident where typed transport errors are needed for retry classification at higher layers.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-120: 13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:201,217
reason: - **13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:201,217` — If the DAPR `client_secret` is rotated, cached secret stays in `_apiKeyCache`; bearer-token refresh hands the IdP the stale secret. Google path evicts the secret cache symmetrically (line 176); Ollama does not. AC5 does not strictly require this. Re-open trigger: a secret-rotation runbook where Ollama tenants degrade until restart.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-121: 13.4-RV1: `RateLimitPerMinute` boundary / arithmetic overflow concerns. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161` — Validator caps the value but downstream arithmetic on it is not audited. Pre-existing. Re-open trigger: any throughput refactor that multiplies rate by a window size or uses it in token-bucket math.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161
reason: - **13.4-RV1 — `RateLimitPerMinute` boundary / arithmetic overflow concerns.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161` — Validator caps the value but downstream arithmetic on it is not audited. Pre-existing. Re-open trigger: any throughput refactor that multiplies rate by a window size or uses it in token-bucket math.
status: open

### DW-122: 13.4-RV2: `OidcScope` whitespace-only not validated. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — Spec leaves `OidcScope` optional and unvalidated; a non-null whitespace-only value would silently flow into the token request and surface as `invalid_scope` from Keycloak. Out of story scope. Re-open trigger: Story 13.2 / 13.3 surfaces an IdP-side regression caused by malformed scope.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181
reason: - **13.4-RV2 — `OidcScope` whitespace-only not validated.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — Spec leaves `OidcScope` optional and unvalidated; a non-null whitespace-only value would silently flow into the token request and surface as `invalid_scope` from Keycloak. Out of story scope. Re-open trigger: Story 13.2 / 13.3 surfaces an IdP-side regression caused by malformed scope.
status: open

### DW-123: 13.4-RV3: OIDC mode does not enforce `ApiSecretKeyName` distinctness/role. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181
reason: - **13.4-RV3 — OIDC mode does not enforce `ApiSecretKeyName` distinctness/role.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.
status: done 2026-09-01
decision: 2026-09-01 Keep names opaque — Let lookup and authentication determine role and document that contract.
resolution: closed by human decision: Let lookup and authentication determine role and document that contract.
decision: 2026-09-01 Keep names opaque — Let lookup and authentication determine role and document that contract.

### DW-124: 13.4-RV4: No assertion that endpoint paths invoke `Validate`. `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Endpoints/*
reason: - **13.4-RV4 — No assertion that endpoint paths invoke `Validate`.** `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs:53

### DW-125: 13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:214-226
reason: - **13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:214-226` — Mirrors deferred 13.2-RV5 in the OIDC token provider. `Uri.TryCreate` accepts userinfo and the value is preserved; backend-configured endpoints make tenant exploitation rare, but defensive rejection is cheap and uniform across providers. Re-open trigger: any tenant config audit that finds embedded credentials, or a security review that wants the rule applied uniformly across `OidcTokenProvider` + `EmbeddingProviderDefaults`.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-126: 13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.** `OidcTokenProvider.cs:117,167` — the leader's `CancellationToken` is passed straight into `_httpClient.SendAsync`. AC6 narrow text only requires waiter cancellation isolation (current test `GetAccessTokenAsync_CancelledWaiter_DoesNotCancelSharedAcquisition` covers this). Dev Notes Implementation Guidance is stricter: "a single caller cancellation must cancel that caller's wait without cancelling the in-flight fetch for remaining waiters." If the leader cancels mid-fetch, queued waiters re-enter and refire. Fix requires TCS-based detached-fetch refactor or linked-CTS where the inner SendAsync uses `CancellationToken.None` and waiters await via `Task.WaitAsync(ct)`. Re-open trigger: Story 13.3 retry integration where leader-cancel under 401 retry becomes concrete; or a production incident where IdP traffic spikes correlate with caller cancellations.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-127: 13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.** `Program.cs:110-118` and `OidcTokenProvider.cs:34-42` — the named HttpClient is resolved once at singleton activation and stored for the service lifetime. DNS changes, TLS session rotation, and `SocketsHttpHandler.PooledConnectionLifetime` rotation never apply. The same caveat exists for `EmbeddingClient` registration (line 108). Fix options: inject `IHttpClientFactory` and `CreateClient(name)` per call, or convert to typed-HttpClient + scoped lifetime. Re-open trigger: an ops incident traceable to stale TLS/DNS, or an ecosystem-wide pass to standardize HttpClient lifecycle across `EmbeddingClient` + `OidcTokenProvider`.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-128: 13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.** `OidcTokenProvider.cs:167` — `HttpRequestException`, `TaskCanceledException` (timeout), `IOException` from `SendAsync` propagate raw. AC7 only requires wrapping non-2xx responses, but Story 13.3's 401-retry will distinguish recoverable vs terminal failures. Re-open trigger: Story 13.3 surfaces a need for typed transport errors during retry classification.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-129: 13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).** `OidcTokenProvider.cs:80` — `uri.Scheme is not "https" and not "http"` is the only scheme guard. Dev/local Keycloak needs `http://localhost`; production must be constrained at the operations/config layer. Re-open trigger: Story 13.7 operations docs / production hardening pass.
status: done 2026-09-01
resolution: already resolved: commit e68cd2e4

### DW-130: 13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.** `OidcTokenProvider.cs:79-89` — `Uri.TryCreate` accepts userinfo and it is preserved through `UriComponents.SchemeAndServer`. Backend-configured endpoints make this rare; defensive rejection is still cheap. Re-open trigger: any tenant config audit that finds embedded userinfo, or a security review.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-131: 13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.** `OidcTokenProvider.cs:65,116-119` — two concurrent forced-refresh callers both skip the cache double-check inside the guard and each issue a fresh HTTP fetch. AC5 is silent on concurrent forced refresh; AC6's "exactly one outbound HTTP request" applies to cache-miss collapse, not invalidation. Re-open trigger: a 401 storm during ingestion that hammers Keycloak via simultaneous retry-after-401 paths in Story 13.3.
status: done 2026-09-01
resolution: already resolved: commit 90bf620a

### DW-132: 13.2-RV7: Unbounded `_cache` and `_guards` growth + undisposed `SemaphoreSlim`. `OidcTokenProvider.cs:24-25` — singleton dictionary growth is bounded by unique `(endpoint, clientId, scope)` tuples but never evicted; semaphores in `_guards` are never `Dispose()`'d. Re-open trigger: a long-running tenant churn scenario or a leak diagnostic that traces growth to this provider.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV7 — Unbounded `_cache` and `_guards` growth + undisposed `SemaphoreSlim`.** `OidcTokenProvider.cs:24-25` — singleton dictionary growth is bounded by unique `(endpoint, clientId, scope)` tuples but never evicted; semaphores in `_guards` are never `Dispose()`'d. Re-open trigger: a long-running tenant churn scenario or a leak diagnostic that traces growth to this provider.
status: open

### DW-133: 13.2-RV8: `JsonDocument.Parse` materializes adversarial large bodies; `InvalidOperationException.Message` may pass provider-controlled text. `OidcTokenProvider.cs:188,217` — a 200 OK with an outsized body is read fully before parsing; provider-supplied `tokenType`/`propertyName` values appear inside the malformed-response reason string (then in the typed exception message). Modern HttpClient defaults bound the buffer at 2 MiB so practical risk is low. Re-open trigger: an SLA pass that wants explicit `MaxResponseContentBufferSize` or a sanitization audit on exception text.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV8 — `JsonDocument.Parse` materializes adversarial large bodies; `InvalidOperationException.Message` may pass provider-controlled text.** `OidcTokenProvider.cs:188,217` — a 200 OK with an outsized body is read fully before parsing; provider-supplied `tokenType`/`propertyName` values appear inside the malformed-response reason string (then in the typed exception message). Modern HttpClient defaults bound the buffer at 2 MiB so practical risk is low. Re-open trigger: an SLA pass that wants explicit `MaxResponseContentBufferSize` or a sanitization audit on exception text.
status: open

### DW-134: 13.2-RV9: `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot. `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412
reason: - **13.2-RV9 — `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot.** `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:817-821

### DW-135: 13.1-RV1: `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body. Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272
reason: - **13.1-RV1 — `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body.** Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)
status: open

### DW-136: 13.1-RV2: `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects". Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307
reason: - **13.1-RV2 — `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects".** Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)
status: open

### DW-137: 13.1-RV3: `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google. Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284
reason: - **13.1-RV3 — `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google.** Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)
status: open

### DW-138: 13.1-RV4: Provider whitespace UX gap. `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but obscures the leading-whitespace root cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104
reason: - **13.1-RV4 — Provider whitespace UX gap.** `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but obscures the leading-whitespace root cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
status: open

### DW-139: 13.1-RV5: Per-provider rate-limit ternary fragile for future providers. `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145
reason: - **13.1-RV5 — Per-provider rate-limit ternary fragile for future providers.** `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:46-70

### DW-140: 13.1-RV6: `Dimensions = int.MaxValue` accepted. Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117
reason: - **13.1-RV6 — `Dimensions = int.MaxValue` accepted.** Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:239-247

### DW-141: 13.1-RV7: `GetBreakingChangeFields` case-sensitivity contract not pinned by tests. Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67
reason: - **13.1-RV7 — `GetBreakingChangeFields` case-sensitivity contract not pinned by tests.** Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:785-805

### DW-142: 13.1-RV8: No null-config test for `Validate(null!)`. `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs
reason: - **13.1-RV8 — No null-config test for `Validate(null!)`.** `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:170

### DW-143: 13.1-RV9: Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site. Spec rationale exists but the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at `Ollama()`'s `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57
reason: - **13.1-RV9 — Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site.** Spec rationale exists but the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at `Ollama()`'s `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)
status: open

### DW-144: 13.1-RV10: Mixed-case provider/model strings persisted verbatim. `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104
reason: - **13.1-RV10 — Mixed-case provider/model strings persisted verbatim.** `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
status: open

### DW-145: 13.1-RV11: Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4). The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. Action when 13.4 lands: (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. Re-open trigger if 13.4 ships without: any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167
reason: - **13.1-RV11 — Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4).** The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. **Action when 13.4 lands:** (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. **Re-open trigger if 13.4 ships without:** any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:219-259

### DW-146: 12.6-RV1: Real-repo positive parser canary lost. `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` and `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` both now expect empty against the real repo files; only fixture-shaped tests prove `ReadOpenDeferredBaselines` parses anything. Add a smoke test that exercises the parser against a fixture mirroring the current `deferred-work.md` (or a separate non-baseline open `S11-F*` entry) so future structural drift in the file format is caught loudly. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224
reason: - **12.6-RV1 — Real-repo positive parser canary lost.** `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` and `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` both now expect empty against the real repo files; only fixture-shaped tests prove `ReadOpenDeferredBaselines` parses anything. Add a smoke test that exercises the parser against a fixture mirroring the current `deferred-work.md` (or a separate non-baseline open `S11-F*` entry) so future structural drift in the file format is caught loudly. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224`)
status: open

### DW-147: 12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377
reason: - **12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.** `ParseDeferredBaseline` classifies an entry as "baseline-related" via case-insensitive substring of `baseline` or `test-release.ps1`. A pure-prose deferred-work edit (e.g., S11-FD "release pipeline" → "release lane") could flip an entry's classification and break the inventory test with no functional change. Migrate to a structured classifier (e.g., explicit `Filter:` field per entry) before the surface grows. Realizes the 12.4-RV6 concern. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377`)
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1094-1110

### DW-148: 12.6-RV3: Single-item parser fixture masks over-matching. `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` exercises exactly one comment + one filter line and uses `ShouldHaveSingleItem()`, which would pass even if the parser is matching on the wrong line and dedupes. Strengthen with a 2-filter fixture or one with a comment-line that resembles a filter, to verify the proximity guard and uniqueness logic. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214
reason: - **12.6-RV3 — Single-item parser fixture masks over-matching.** `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` exercises exactly one comment + one filter line and uses `ShouldHaveSingleItem()`, which would pass even if the parser is matching on the wrong line and dedupes. Strengthen with a 2-filter fixture or one with a comment-line that resembles a filter, to verify the proximity guard and uniqueness logic. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214`)
status: open

### DW-149: 12.6-RV4: Discoverability breadcrumb removed from `tools/test-release.ps1`. Along with the `$projectFilters` block went the only on-script reference to deferred-work bookkeeping. Consider a one-line trailing comment such as `# No per-project baseline waivers; if one becomes necessary, register it in _bmad-output/implementation-artifacts/deferred-work.md and pair it here.` so a future maintainer searching the script for "baseline" finds the policy. (`tools/test-release.ps1:25`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tools/test-release.ps1
reason: - **12.6-RV4 — Discoverability breadcrumb removed from `tools/test-release.ps1`.** Along with the `$projectFilters` block went the only on-script reference to deferred-work bookkeeping. Consider a one-line trailing comment such as `# No per-project baseline waivers; if one becomes necessary, register it in _bmad-output/implementation-artifacts/deferred-work.md and pair it here.` so a future maintainer searching the script for "baseline" finds the policy. (`tools/test-release.ps1:25`)
status: open

### DW-150: 12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs
reason: - **12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filtered captures by neither tenant nor instrument-source, while its sibling `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` used unique tenant id + `ShouldHaveSingleItem`. The flake mode that originally motivated S11-FA was dormant, not eliminated, and could re-trip under heavier xunit parallelism or any other test that emits on the static `MemoriesMeter.EmbeddingApiCalls` counter. Resolved by the 2026-05-19 deferred-work implementation: the affected telemetry tests now use unique tenant ids, tenant-filtered metric capture, `ConcurrentQueue`, and single-event assertions. Re-open trigger: any flake reappearance on `EmbeddingInputContentKindTests`, or before any future story that adds a third concurrent `EmbeddingApiCalls` test path. (`tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs`)
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-151: 12.5-RV1: Workflow hardcodes summary path. `.github/workflows/release.yml:75` literal `artifacts/packages/release/publish-summary.json` is a duplicate of the path computed by `tools/publish-nuget.ps1` from its `-PackageDirectory` parameter. Aligned today via `.releaserc.json` invocation; if either ever changes the alert silently no-ops.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: .github/workflows/release.yml:75
reason: - **12.5-RV1 — Workflow hardcodes summary path.** `.github/workflows/release.yml:75` literal `artifacts/packages/release/publish-summary.json` is a duplicate of the path computed by `tools/publish-nuget.ps1` from its `-PackageDirectory` parameter. Aligned today via `.releaserc.json` invocation; if either ever changes the alert silently no-ops.
status: open

### DW-152: 12.5-RV2: `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`. `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92
reason: - **12.5-RV2 — `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`.** `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.
status: open

### DW-153: 12.5-RV3: Race on concurrent partial-publish runs. Two workflow runs hitting partial-publish for the same version simultaneously can each create a new issue. `tools/create-partial-publish-issue.ps1:92-119`. Workflow `concurrency: release` reduces but does not eliminate the window. Needs server-side dedupe (search-with-create-or-comment idempotency) before becoming critical.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92-119
reason: - **12.5-RV3 — Race on concurrent partial-publish runs.** Two workflow runs hitting partial-publish for the same version simultaneously can each create a new issue. `tools/create-partial-publish-issue.ps1:92-119`. Workflow `concurrency: release` reduces but does not eliminate the window. Needs server-side dedupe (search-with-create-or-comment idempotency) before becoming critical.
status: open

### DW-154: 12.5-RV4: `Format-ListSection` does not skip `$null` items. `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:52-67
reason: - **12.5-RV4 — `Format-ListSection` does not skip `$null` items.** `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.
status: open

### DW-155: 12.5-RV5: No retry/backoff for transient `gh` failures in the alert path. `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92-128
reason: - **12.5-RV5 — No retry/backoff for transient `gh` failures in the alert path.** `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.
status: open

### DW-156: 12.5-RV6: Malformed `publish-summary.json` makes the alert step throw. `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:32
reason: - **12.5-RV6 — Malformed `publish-summary.json` makes the alert step throw.** `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.
status: open

### DW-157: 12.5-RV7: Empty stdout from `gh issue list` trips `ConvertFrom-Json`. `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:97
reason: - **12.5-RV7 — Empty stdout from `gh issue list` trips `ConvertFrom-Json`.** `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.
status: open

### DW-158: 12.5-RV8: Closed-then-reopened partial-publish issue creates a duplicate. `tools/create-partial-publish-issue.ps1:92` filters `--state open` only. After a maintainer manually reconciles and closes the issue, a same-version rerun creates a new issue rather than reopening or commenting. Spec is silent; semantics may need refinement after first real reconciliation cycle.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92
reason: - **12.5-RV8 — Closed-then-reopened partial-publish issue creates a duplicate.** `tools/create-partial-publish-issue.ps1:92` filters `--state open` only. After a maintainer manually reconciles and closes the issue, a same-version rerun creates a new issue rather than reopening or commenting. Spec is silent; semantics may need refinement after first real reconciliation cycle.
status: open
decision: 2026-09-01 Reopen and comment — Search open and closed exact-title matches, reopen the prior incident, append evidence, and test reruns.
decision: 2026-09-01 Reopen and comment — Search open and closed exact-title matches, reopen the prior incident, append evidence, and test reruns.

### DW-159: 12.3-RV1: CI duplicate runs on `pull_request` and `push`. Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:3-9
reason: - **12.3-RV1 — CI duplicate runs on `pull_request` and `push`.** Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion. [.github/workflows/ci.yml:3-9]
status: done 2026-09-01
resolution: already resolved: commit 6ef83aa6

### DW-160: 12.3-RV2: CI fork-PR base SHA reachability. `actions/checkout@v6` with `fetch-depth: 0` on the head ref does not fetch the base repo's history. `git diff "$base_sha" "$head_sha"` may fail on PRs from forks. Deferred until fork PRs are accepted; if needed, add an explicit `git fetch origin "$base_sha"` step.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:43-46
reason: - **12.3-RV2 — CI fork-PR base SHA reachability.** `actions/checkout@v6` with `fetch-depth: 0` on the head ref does not fetch the base repo's history. `git diff "$base_sha" "$head_sha"` may fail on PRs from forks. Deferred until fork PRs are accepted; if needed, add an explicit `git fetch origin "$base_sha"` step. [.github/workflows/ci.yml:43-46]
status: open

### DW-161: 12.3-RV3: CI brand-new branch first push enumerates entire head commit. `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:51-52
reason: - **12.3-RV3 — CI brand-new branch first push enumerates entire head commit.** `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable. [.github/workflows/ci.yml:51-52]
status: done 2026-09-01
resolution: already resolved: .github/workflows/ci.yml:95-130

### DW-162: 12.3-RV4: Branch with no story key + no `Story:` trailer fails closed across all CI. No allowlist for automation branches (dependabot, renovate). Defer until automation PRs are configured for this repo.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:168-172
reason: - **12.3-RV4 — Branch with no story key + no `Story:` trailer fails closed across all CI.** No allowlist for automation branches (dependabot, renovate). Defer until automation PRs are configured for this repo. [tools/check-story-file-scope.py:168-172]
status: open

### DW-163: 12.3-RV5: `commit-msg` re-reads index after pre-commit may have modified it. File set seen by the two hooks can differ in environments that auto-format during pre-commit. Low impact in this repo; reconsider if formatters are introduced.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/commit-msg:12
reason: - **12.3-RV5 — `commit-msg` re-reads index after pre-commit may have modified it.** File set seen by the two hooks can differ in environments that auto-format during pre-commit. Low impact in this repo; reconsider if formatters are introduced. [.githooks/commit-msg:12]
status: open

### DW-164: 12.3-RV6: `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages. Edge case; clean error wrapping is a follow-up nicety.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:114-118
reason: - **12.3-RV6 — `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages.** Edge case; clean error wrapping is a follow-up nicety. [tools/check-story-file-scope.py:114-118]
status: open

### DW-165: 12.3-RV7: `--changed-files-file` does not strip a UTF-8 BOM. A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:246
reason: - **12.3-RV7 — `--changed-files-file` does not strip a UTF-8 BOM.** A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited. [tools/check-story-file-scope.py:246]
status: open

### DW-166: 12.3-RV8: `collect_changed_files` silently drops paths normalizing to empty. Theoretical; `..`-only inputs are not produced by `git diff --name-only`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:250
reason: - **12.3-RV8 — `collect_changed_files` silently drops paths normalizing to empty.** Theoretical; `..`-only inputs are not produced by `git diff --name-only`. [tools/check-story-file-scope.py:250]
status: open

### DW-167: 12.3-RV9: Pre-commit fails closed during rebase / cherry-pick / detached-HEAD. `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:7
reason: - **12.3-RV9 — Pre-commit fails closed during rebase / cherry-pick / detached-HEAD.** `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching. [.githooks/pre-commit:7]
status: done 2026-09-01
resolution: already resolved: tools/check-story-file-scope.py:475-486

### DW-168: 12.3-RV10: `python` fallback may land on Python 2 on legacy systems. Not a target environment today.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:13-17
reason: - **12.3-RV10 — `python` fallback may land on Python 2 on legacy systems.** Not a target environment today. [.githooks/pre-commit:13-17]
status: open

### DW-169: 12.3-RV11: Hooks consume newline-separated `git diff --name-only` output. Filenames containing newlines (legal POSIX) mishandled. No such filenames in repo.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:12
reason: - **12.3-RV11 — Hooks consume newline-separated `git diff --name-only` output.** Filenames containing newlines (legal POSIX) mishandled. No such filenames in repo. [.githooks/pre-commit:12]
status: open

### DW-170: 12.3-RV12: `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check. Backslashes get normalized away before the test. Pair with override-vagueness rework P6.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:286-288
reason: - **12.3-RV12 — `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check.** Backslashes get normalized away before the test. Pair with override-vagueness rework P6. [tools/check-story-file-scope.py:286-288]
status: done 2026-09-01
resolution: already resolved: tools/check-story-file-scope.py:442-449

### DW-171: 12.3-RV13: `parse_allowed_scope` does not honor `## ` subheadings inside `## File Scope`. No current story uses this shape.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:206-211
reason: - **12.3-RV13 — `parse_allowed_scope` does not honor `## ` subheadings inside `## File Scope`.** No current story uses this shape. [tools/check-story-file-scope.py:206-211]
status: open

### DW-172: 12.3-RV14: `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not in CONTRIBUTING.md. Either remove the aliases or document them in a follow-up.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:19-23
reason: - **12.3-RV14 — `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not in CONTRIBUTING.md.** Either remove the aliases or document them in a follow-up. [tools/check-story-file-scope.py:19-23]
status: open

### DW-173: 12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:217-223
reason: - **12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.** No current story uses this shape. [tools/check-story-file-scope.py:217-223]
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-174: W10: closed. AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.

origin: migrated from legacy ledger ("Closed by: course correction (2026-04-26)"), 2026-09-01
location: docs/dev/branch-protection.md
reason: - **W10 — closed.** AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.
status: done 2026-09-01
resolution: already resolved: _bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md:4

### DW-175: S11-FA: closed. `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.

origin: migrated from legacy ledger ("Closed by: Story 12.6 EmbeddingInputContentKind Baseline Resolution (2026-05-02)"), 2026-09-01
location: tools/test-release.ps1
reason: - **S11-FA — closed.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:87-89

### DW-176: Story-10.2-TokenBudgetServerTruncation: closed. MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-TokenBudgetServerTruncation — closed.** MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/SearchResult.cs:33-43

### DW-177: Story-10.2-DegradedStateAnnotations: closed for MCP ingress. Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-DegradedStateAnnotations — closed for MCP ingress.** Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/SearchResult.cs:45-65

### DW-178: Story-10.2-IngressAuthentication-NFR11: closed for MCP ingress. `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.

origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.2-IngressAuthentication-NFR11 — closed for MCP ingress.** `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Mcp/Program.cs:20-23

### DW-179: Story-10.x-McpTraceHopAspire (closes AC #12).

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs::TraceHop_McpToServer_PreservesTraceparent
reason: - **Story-10.x-McpTraceHopAspire (closes AC #12).** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs::TraceHop_McpToServer_PreservesTraceparent` — assert MCP→Memories Server parent/child trace span relationship at Tier-3. Implementation requires a parallel breadcrumb path through the MCP Aspire process (the existing `AspireEndToEndTraceTests` uses an in-process collector for the CLI side and audit-log JSON breadcrumbs for the Server side; the MCP service emits ActivitySource spans into its own process and lacks an in-test exporter). 10.2 ships the prerequisite Tier-3 auth bearer-minting fixture (`AspireIngestionPipelineFixture.MintDevBearer`) so the trace-hop work can attach to it directly. Re-open trigger: nightly Tier-3 lane stabilizes against the new auth-required surface and an in-test exporter for the MCP process becomes affordable; or first observation of MCP→Server trace breakage in production OTel data.
status: open

### DW-180: Story-10.x-McpAuthTier3ExtendedSuite. Additional Tier-3 cases beyond the four critical scenarios shipped in 10.2 (`PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge`, `GetHealth_AllowsAnonymous`, `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`, `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden`): expired-bearer test, clock-skew tolerance test (M3), cross-request alternating-tenants leak test (P2), tool-class lifetime convention test (P1). Tier-2 already covers expiry / clock-skew at unit level (`ConfigureJwtBearerOptionsTests`); cross-request leak is structurally prevented by `TenantClaimAuthorizationFilter`'s `InvalidOperationException` guard (P3). Re-open trigger: first nightly Tier-3 regression against the auth path, or first cross-request leakage report.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAuthTier3ExtendedSuite.** Additional Tier-3 cases beyond the four critical scenarios shipped in 10.2 (`PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge`, `GetHealth_AllowsAnonymous`, `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`, `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden`): expired-bearer test, clock-skew tolerance test (M3), cross-request alternating-tenants leak test (P2), tool-class lifetime convention test (P1). Tier-2 already covers expiry / clock-skew at unit level (`ConfigureJwtBearerOptionsTests`); cross-request leak is structurally prevented by `TenantClaimAuthorizationFilter`'s `InvalidOperationException` guard (P3). Re-open trigger: first nightly Tier-3 regression against the auth path, or first cross-request leakage report.
status: open

### DW-181: Story-10.x-McpStatelessTripwireTest (W3). `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — assert `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Requires an MCP-builder introspection surface that the SDK does not currently expose. Re-open trigger: SDK exposes a public reader for `WithHttpTransport` configuration, or any follow-up story discusses OAuth-PKCE / sampling / elicitation.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpStatelessTripwireTest (W3).** `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — assert `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Requires an MCP-builder introspection surface that the SDK does not currently expose. Re-open trigger: SDK exposes a public reader for `WithHttpTransport` configuration, or any follow-up story discusses OAuth-PKCE / sampling / elicitation.
status: open

### DW-182: Story-10.x-McpAuthAnonymousDevBindAddress (A2). Bind-address invariant for the anonymous-dev gate — refuse to run when `Authentication:JwtBearer:Authority` AND `SigningKey` are both unset and the process binds a non-loopback address. The 10.2 wiring requires `Authority` OR `SigningKey` at startup (validator hard-fails otherwise), so the anonymous-dev path no longer exists; this defense-in-depth check is N/A unless the anonymous-dev path is reintroduced. Re-open trigger: any future story re-introducing an anonymous mode for development.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAuthAnonymousDevBindAddress (A2).** Bind-address invariant for the anonymous-dev gate — refuse to run when `Authentication:JwtBearer:Authority` AND `SigningKey` are both unset and the process binds a non-loopback address. The 10.2 wiring requires `Authority` OR `SigningKey` at startup (validator hard-fails otherwise), so the anonymous-dev path no longer exists; this defense-in-depth check is N/A unless the anonymous-dev path is reintroduced. Re-open trigger: any future story re-introducing an anonymous mode for development.
status: open

### DW-183: Story-10.x-McpAuthDriftDetectorCi (A14). Monthly GitHub Actions workflow that diffs `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` against `src/Hexalith.Memories.Mcp/Authentication/` and posts structural drift summaries to an ops issue. Allowed-divergence list mirrors ADR-10.2-001 invariants. Effort > 45 min for a robust diff + allowlist + ops-issue integration; deferred to keep the 10.2 envelope on track. Re-open trigger: first observed drift incident, or quarterly review.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/
reason: - **Story-10.x-McpAuthDriftDetectorCi (A14).** Monthly GitHub Actions workflow that diffs `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` against `src/Hexalith.Memories.Mcp/Authentication/` and posts structural drift summaries to an ops issue. Allowed-divergence list mirrors ADR-10.2-001 invariants. Effort > 45 min for a robust diff + allowlist + ops-issue integration; deferred to keep the 10.2 envelope on track. Re-open trigger: first observed drift incident, or quarterly review.
status: open

### DW-184: Story-10.x-RetroLessonForwardReferenceGuards (A16). Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-RetroLessonForwardReferenceGuards (A16).** Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.
status: open

### DW-185: Story-10.x-McpTraceHopAssertion. `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` does not yet include an MCP-hop assertion — adding one is out of scope for 10.1 and tempting-but-fragile until 10.2 auth is wired (so the test does not need to mint bearer tokens). The MCP integration test (`McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`) covers a happy-path execution, but does NOT yet assert that the outbound trace contains a span resolving to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). Re-open trigger: post-10.2 follow-up story or first observation that direct-HTTP regressions slip through.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs
reason: - **Story-10.x-McpTraceHopAssertion.** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` does not yet include an MCP-hop assertion — adding one is out of scope for 10.1 and tempting-but-fragile until 10.2 auth is wired (so the test does not need to mint bearer tokens). The MCP integration test (`McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`) covers a happy-path execution, but does NOT yet assert that the outbound trace contains a span resolving to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). **Re-open trigger:** post-10.2 follow-up story or first observation that direct-HTTP regressions slip through.
status: open

### DW-186: Story-10.x-StatelessModeAuditFor10.2Auth: closed for bearer auth. Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-StatelessModeAuditFor10.2Auth — closed for bearer auth.** Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:82

### DW-187: Story-10.x-McpAotCompatibility. ModelContextProtocol 1.2.0 uses reflection for tool-schema generation. Setting `<PublishAot>true</PublishAot>` on `Hexalith.Memories.Mcp.csproj` will likely surface trim/reflection warnings. 10.1 deliberately leaves AOT off — same default as `Contracts`, `Client.Rest`, `EventStore`, `Cli`. Re-open trigger: an explicit AOT-publishing requirement, or upstream MCP SDK release that ships a source-generator-based schema path.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAotCompatibility.** ModelContextProtocol 1.2.0 uses reflection for tool-schema generation. Setting `<PublishAot>true</PublishAot>` on `Hexalith.Memories.Mcp.csproj` will likely surface trim/reflection warnings. 10.1 deliberately leaves AOT off — same default as `Contracts`, `Client.Rest`, `EventStore`, `Cli`. **Re-open trigger:** an explicit AOT-publishing requirement, or upstream MCP SDK release that ships a source-generator-based schema path.
status: open

### DW-188: Story-10.x-McpTokenizerAccurateBudget. Story 10.2 replaced the 10.1 per-result soft clamp with server-side `contentSnippet.Length / 4 + overhead` estimation. This is still heuristic and can over-prune non-ASCII content. Phase 2 Sprint 1 candidate; escalate on first non-ASCII tenant onboarding or quantitative observation that the estimate causes >2x under-utilization on real workloads.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpTokenizerAccurateBudget.** Story 10.2 replaced the 10.1 per-result soft clamp with server-side `contentSnippet.Length / 4 + overhead` estimation. This is still heuristic and can over-prune non-ASCII content. Phase 2 Sprint 1 candidate; escalate on first non-ASCII tenant onboarding or quantitative observation that the estimate causes >2x under-utilization on real workloads.
status: open

### DW-189: Story-10.x-TraverseSemanticPrimaryPath. Story 10.2 traversal truncation preserves a BFS shortest path to the deepest node before pruning branches. A semantically weighted primary path (`causedBy` > `correlatedWith`, etc.) may produce better narratives under tight budgets, but edge weights are not exposed today. Re-open when traversal edge weights become available.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-TraverseSemanticPrimaryPath.** Story 10.2 traversal truncation preserves a BFS shortest path to the deepest node before pruning branches. A semantically weighted primary path (`causedBy` > `correlatedWith`, etc.) may produce better narratives under tight budgets, but edge weights are not exposed today. Re-open when traversal edge weights become available.
status: open

### DW-190: Story-10.x-OpenTelemetryAspNetCoreAlignment. Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. Re-open trigger: AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-OpenTelemetryAspNetCoreAlignment.** Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. **Re-open trigger:** AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.
status: open

### DW-191: Story-9.3-MemoriesServerAuthN: resolved by Story 20.1. Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Authentication/*
reason: - **Story-9.3-MemoriesServerAuthN — resolved by Story 20.1.** Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.
status: done 2026-09-01
resolution: already resolved: commit b48a519b

### DW-192: D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs
reason: - **D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.** Memories Server now normalizes authenticated principal tenant claims into a server-owned `memories:tenant` claim, rejects well-formed cross-tenant `/api/tenants/{tenantId}/**`, `/api/search?tenantId=...`, and ingest scheduling requests before endpoint business logic/backend access, and derives audit user identity from the authenticated principal instead of `x-user-id` or request-body attribution fields. Evidence: `src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs`, `src/Hexalith.Memories.Server/Program.cs`, `tests/Hexalith.Memories.Server.Tests/Authentication/*`, and `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs`. Residual tenantless workflow/batch status scoping remains Story 20.3 scope, not a duplicate D8/A2 entry.
status: done 2026-09-01
resolution: already resolved: commit ae9558fe

### DW-193: Story-9.3-ObservationWindowConfig. The 24h observation window is hardcoded in `HandlerRegistryService.ObservationWindow`. Making it configurable per-tenant complicates Redis TTL (TTL must exceed the widest possible window) and global-config awkwardness. Re-open trigger: first operator explicitly requests a non-24h window in a real deployment.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ObservationWindowConfig.** The 24h observation window is hardcoded in `HandlerRegistryService.ObservationWindow`. Making it configurable per-tenant complicates Redis TTL (TTL must exceed the widest possible window) and global-config awkwardness. **Re-open trigger:** first operator explicitly requests a non-24h window in a real deployment.
status: open

### DW-194: Story-9.3-ProjectionRegistryCrossCheck. The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. Re-open trigger: operator-driven demand for this gap to be closed.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ProjectionRegistryCrossCheck.** The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. **Re-open trigger:** operator-driven demand for this gap to be closed.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs:178-203

### DW-195: Story-9.3-SinceFlagForLowVolume. `--since <duration>` CLI flag on `memories handlers mismatches` to widen the observation window for low-volume tenants, reducing `StaleHandler` Info noise on weekly-publishing patterns. Requires the observation store TTL to be widened to `2 × max(window)` globally, or a dedicated expanded-window store. Material cost, deferred.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-SinceFlagForLowVolume.** `--since <duration>` CLI flag on `memories handlers mismatches` to widen the observation window for low-volume tenants, reducing `StaleHandler` Info noise on weekly-publishing patterns. Requires the observation store TTL to be widened to `2 × max(window)` globally, or a dedicated expanded-window store. Material cost, deferred.
status: open

### DW-196: Story-9.3-TenantCardinalityBucketing. Switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0 / 1-10 / 10-100 / 100+ tenants) when N ≥ 1000 tenants approaches. Not an issue in current deployments. Re-open trigger: tenant count crosses 1000 in any real environment.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-TenantCardinalityBucketing.** Switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0 / 1-10 / 10-100 / 100+ tenants) when N ≥ 1000 tenants approaches. Not an issue in current deployments. **Re-open trigger:** tenant count crosses 1000 in any real environment.
status: open

### DW-197: Story-9.3-VersionMismatchAttributeApproach. Replace regex-based `VersionMismatch` with a publisher-declared `[EventType("ClaimSubmitted", Version=2)]` attribute, surfaced via `ReflectionTypeLoader` at startup. Becomes an O(1) dictionary lookup with no ReDoS surface, no length cap, no regex-timeout event id 9141. Deferred: requires coordinating a convention change with every publisher repo.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-VersionMismatchAttributeApproach.** Replace regex-based `VersionMismatch` with a publisher-declared `[EventType("ClaimSubmitted", Version=2)]` attribute, surfaced via `ReflectionTypeLoader` at startup. Becomes an O(1) dictionary lookup with no ReDoS surface, no length cap, no regex-timeout event id 9141. Deferred: requires coordinating a convention change with every publisher repo.
status: open
decision: 2026-09-01 Await publisher program
decision: 2026-09-01 Await publisher program

### DW-198: Story-9.3-SubscriptionStatusConfigured. 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. Re-open trigger: operator feedback post-landing indicates the 3-state model is ambiguous.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-SubscriptionStatusConfigured.** 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. **Re-open trigger:** operator feedback post-landing indicates the 3-state model is ambiguous.
status: done 2026-09-01
decision: 2026-09-01 Keep three states — Retain the stable enum until concrete ambiguity justifies a break.
resolution: closed by human decision: Retain the stable enum until concrete ambiguity justifies a break.
decision: 2026-09-01 Keep three states — Retain the stable enum until concrete ambiguity justifies a break.

### DW-199: Story-9.3-ObservationStoreRebuildFromAuditLog. Rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss (Risk #8 degraded mode). Out of scope for 9.3.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ObservationStoreRebuildFromAuditLog.** Rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss (Risk #8 degraded mode). Out of scope for 9.3.
status: open

### DW-200: Story-9.3-PostgresObservationStoreAlternative. Investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-PostgresObservationStoreAlternative.** Investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.
status: open
decision: 2026-09-01 Benchmark Postgres view — Build a representative view prototype, measure query latency and write amplification, and recommend a substrate.
decision: 2026-09-01 Benchmark Postgres view — Build a representative view prototype, measure query latency and write amplification, and recommend a substrate.

### DW-201: Story-9.3-CrossTenantVersionConsumerLookup. A dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside 9.3; Epic 5 tenant-isolation invariant prevents a naive implementation.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-CrossTenantVersionConsumerLookup.** A dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside 9.3; Epic 5 tenant-isolation invariant prevents a naive implementation.
status: open
decision: 2026-09-01 Await operator auth model
decision: 2026-09-01 Await operator auth model

### DW-202: Story-9.3-PostLaunchCategoryReview. Measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator acknowledgement or >95% false-positive rate. Target review: 2026-09 or later. The three-category decision is explicitly revisitable based on measured telemetry, not speculation.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-PostLaunchCategoryReview.** Measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator acknowledgement or >95% false-positive rate. Target review: 2026-09 or later. The three-category decision is explicitly revisitable based on measured telemetry, not speculation.
status: open

### DW-203: Story-9.3-Tier2IntegrationTests. 9.3's Task 10 (Tier-2 Aspire-AppHost integration tests 10.0–10.12) was deferred during the initial landing pass. Unit coverage (45 new tests) pins the per-component invariants; the Tier-2 tests would add cross-component proof against a real Redis + DAPR sidecar. Specific deferred tests: `HandlersFixtureSmokeTests`, `HandlersListIntegrationTests`, `HandlersMismatchIntegrationTests` (VersionMismatch + healthy + StaleHandler), `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (property-based, dropProbability ∈ {0.0, 0.1, 0.3}), `HandlerEndpointLatencyNfrTests` (N=100 tenants, p95 500ms/200ms), `HandlerObservationKillSwitchIntegrationTests` (AC #21), `EventIngestionTelemetryAdapterSlowRedisTests` (AC #22 — bounded-FAF contract), `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` (AC #26 — Finding N), `HandlerMetricsCardinalitySmokeTests`, `EndpointRoutingTests` + `MemoriesClientPathConstantTests` (AC #29 — Findings U, V), `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis` + operator-polish tests (AC #30 — Findings B, X). Re-open trigger: first post-landing Tier-2 regression run or any cross-component bug report on 9.3 surface. Each test ~30s startup via Aspire fixture; budget ~1d to close the set.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-Tier2IntegrationTests.** 9.3's Task 10 (Tier-2 Aspire-AppHost integration tests 10.0–10.12) was deferred during the initial landing pass. Unit coverage (45 new tests) pins the per-component invariants; the Tier-2 tests would add cross-component proof against a real Redis + DAPR sidecar. Specific deferred tests: `HandlersFixtureSmokeTests`, `HandlersListIntegrationTests`, `HandlersMismatchIntegrationTests` (VersionMismatch + healthy + StaleHandler), `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (property-based, dropProbability ∈ {0.0, 0.1, 0.3}), `HandlerEndpointLatencyNfrTests` (N=100 tenants, p95 500ms/200ms), `HandlerObservationKillSwitchIntegrationTests` (AC #21), `EventIngestionTelemetryAdapterSlowRedisTests` (AC #22 — bounded-FAF contract), `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` (AC #26 — Finding N), `HandlerMetricsCardinalitySmokeTests`, `EndpointRoutingTests` + `MemoriesClientPathConstantTests` (AC #29 — Findings U, V), `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis` + operator-polish tests (AC #30 — Findings B, X). Re-open trigger: first post-landing Tier-2 regression run or any cross-component bug report on 9.3 surface. Each test ~30s startup via Aspire fixture; budget ~1d to close the set.
status: open

### DW-204: Source-prefix observation granularity / true per-handler fidelity.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-handler-registration-and-mismatch-detection (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Source-prefix observation granularity / true per-handler fidelity.** The current observation store records tenant + aggregateType + eventType only; it does not persist the CloudEvent `source` or matched `SourceToTenantMap` prefix. As a result, `HandlerRegistryService` duplicates one tenant-wide observation set across every row, `StaleHandler` can only mean "tenant saw nothing" rather than "this configured sourcePrefix saw nothing", and `UnhandledEventType` remains heuristic versus the real longest-prefix router on `source`. Re-open trigger: first operator/reporting need for true per-`sourcePrefix` fidelity, or any follow-up story that revisits the observation-store write model.
status: open

### DW-205: Rolling 24h counts in `RedisObservedEventTypeStore`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-handler-registration-and-mismatch-detection (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Rolling 24h counts in `RedisObservedEventTypeStore`.** `GetObservedTypesAsync` currently reads last-seen membership from the zset and counts from a lifetime `HINCRBY` hash, so `eventsProcessedCount` and per-type counts are not truly window-bounded. Accurate rolling counts require a different write model (per-occurrence events, rolling buckets, or another substrate) rather than a small read-side patch. Re-open trigger: when the observation-store model is next revised — ideally in the same change as the source-prefix-fidelity fix so the write/read contracts only churn once.
status: open

### DW-206: F1: Retry backpressure + 9174 + exponential backoff (Task 8.5 / D2). `NaturalLanguageEmbeddingRetryHostedService.TickAsync` has no rate-limiter utilization check, no skip counter, no `9174` LoggerMessage, and no interval multiplier when `backlog > 1000`. Re-open trigger: rate-limiter consistently >80% utilization with retry queue contributing >20% of total calls.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F1 — Retry backpressure + 9174 + exponential backoff** (Task 8.5 / D2). `NaturalLanguageEmbeddingRetryHostedService.TickAsync` has no rate-limiter utilization check, no skip counter, no `9174` LoggerMessage, and no interval multiplier when `backlog > 1000`. Re-open trigger: rate-limiter consistently >80% utilization with retry queue contributing >20% of total calls.
status: open

### DW-207: F2: Tier-2 / Tier-3 integration test suite for AC #14/#15/#16 (Task 9.1–9.6). `DualEmbeddingRoundTripTests`, `OutOfOrderEventTests`, `DegradedNaturalLanguageEmbeddingTests`, `CorrelationRootEdgeTests`, `IngestionWorkflowReplaySafetyTests`, consistency verification NL cases. Deferred per Task 9 header; re-open when Tier-2 environment is stable.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F2 — Tier-2 / Tier-3 integration test suite for AC #14/#15/#16** (Task 9.1–9.6). `DualEmbeddingRoundTripTests`, `OutOfOrderEventTests`, `DegradedNaturalLanguageEmbeddingTests`, `CorrelationRootEdgeTests`, `IngestionWorkflowReplaySafetyTests`, consistency verification NL cases. Deferred per Task 9 header; re-open when Tier-2 environment is stable.
status: open

### DW-208: F3: `RateLimiterSizingValidator` + event 9163 (Task 8.7). Needed when first `SourceType.Event` ingest hits an under-sized tenant configuration. Re-open trigger: first production `9162` warning or tenant NL-pipeline SLO breach.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F3 — `RateLimiterSizingValidator` + event 9163** (Task 8.7). Needed when first `SourceType.Event` ingest hits an under-sized tenant configuration. Re-open trigger: first production `9162` warning or tenant NL-pipeline SLO breach.
status: open

### DW-209: F4: `retry-nl-embeddings` CLI dead-letter surface (Task 8.8). Re-open when dead-letter volume > 0 in any tenant for >24h.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F4 — `retry-nl-embeddings` CLI dead-letter surface** (Task 8.8). Re-open when dead-letter volume > 0 in any tenant for >24h.
status: open

### DW-210: F5: Logprobs extraction for confidence promotion (Task 2.5 / D1). Blocked on Dapr.AI 1.17.6 SDK surface. Re-open when `ConversationClient` exposes `logprobs` or the equivalent shape on `ConversationResponse`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F5 — Logprobs extraction for confidence promotion** (Task 2.5 / D1). Blocked on Dapr.AI 1.17.6 SDK surface. Re-open when `ConversationClient` exposes `logprobs` or the equivalent shape on `ConversationResponse`.
status: open

### DW-211: F6: Per-tenant LLM configuration. Phase 2. MVP is single system-wide `conversation.openai` component; operators swap via YAML.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F6 — Per-tenant LLM configuration.** Phase 2. MVP is single system-wide `conversation.openai` component; operators swap via YAML.
status: open

### DW-212: F7: `NaturalLanguageEmbeddingRetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter. When `ScheduleNewWorkflowAsync` + `WaitForWorkflowCompletionAsync` loop times out repeatedly for the same record, move to dead-letter after N ticks rather than leaving stuck instances in queue.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F7 — `NaturalLanguageEmbeddingRetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter.** When `ScheduleNewWorkflowAsync` + `WaitForWorkflowCompletionAsync` loop times out repeatedly for the same record, move to dead-letter after N ticks rather than leaving stuck instances in queue.
status: open

### DW-213: F8: Redis cluster multi-node enumeration in `FailedNaturalLanguageEmbeddingRegistry.ListTenantsWithBacklogAsync`. Current `GetFirstConnectedServer()` covers single-node / replicated deployments only. Cluster deployment re-open trigger: moving to Redis Cluster in production infrastructure.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F8 — Redis cluster multi-node enumeration in `FailedNaturalLanguageEmbeddingRegistry.ListTenantsWithBacklogAsync`.** Current `GetFirstConnectedServer()` covers single-node / replicated deployments only. Cluster deployment re-open trigger: moving to Redis Cluster in production infrastructure.
status: open

### DW-214: F9: `OrphanSemanticIndexReconciler` interval-based re-run. Currently one-shot startup sweep only (per D3 decision pending). Re-open if post-startup SIGKILL-during-provisioning produces orphan NL indexes in production.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F9 — `OrphanSemanticIndexReconciler` interval-based re-run.** Currently one-shot startup sweep only (per D3 decision pending). Re-open if post-startup SIGKILL-during-provisioning produces orphan NL indexes in production.
status: open

### DW-215: F10: `IsStubBackfillMigration` atomic gate-write + backfill safety. Partial-commit risk after backfill if `MERGE SchemaMigration` throws. Defer to ops runbook monitoring; re-open if migration re-runs cause operator friction.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F10 — `IsStubBackfillMigration` atomic gate-write + backfill safety.** Partial-commit risk after backfill if `MERGE SchemaMigration` throws. Defer to ops runbook monitoring; re-open if migration re-runs cause operator friction.
status: open

### DW-216: DataContract/DataMember attributes missing on V1 contracts

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **DataContract/DataMember attributes missing on V1 contracts** — Systematic gap across all V1 contracts (ExtractionInput, ExtractionResult, MemoryUnit, GraphEdge, etc.). None use DataContract/DataMember/JsonPropertyOrder/JsonConstructor attributes per project-context.md rules. Should be addressed as a batch across all V1 types.
status: open

### DW-217: No transient/permanent exception classification for Kreuzberg errors

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **No transient/permanent exception classification for Kreuzberg errors** — AC4 is met (exceptions propagate for DAPR Workflow retry). However, permanent failures (corrupt files) will be retried indefinitely. Future work: classify KreuzbergValidationException as non-retriable, KreuzbergOcrException as retriable.
status: open

### DW-218: Large byte[] in ExtractionInput persisted to workflow state store

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **Large byte[] in ExtractionInput persisted to workflow state store** — DAPR Workflow serializes activity inputs to state store. For 1MB files, this means ~1.33MB base64 per workflow instance. Accepted per D13 (MVP payloads ≤1MB). Future work: consider streaming or external storage for larger payloads.
status: done 2026-09-01
resolution: already resolved: commit 906f819f

### DW-219: byte[] mutable on immutable record

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **byte[] mutable on immutable record** — ExtractionInput uses byte[] which is mutable, breaking record immutability semantics and reference-based equality. No practical alternative exists in .NET for JSON-serializable binary data (ImmutableArray/ReadOnlyMemory don't serialize well).
status: open

### DW-220: End-to-end embedding flow is not wired into orchestration

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **End-to-end embedding flow is not wired into orchestration** — Deferred because orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:153-187

### DW-221: Rate-limiting scope conflicts with credential scope

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Rate-limiting scope conflicts with credential scope** — Deferred because Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:82-107

### DW-222: Story transition rationale is comment-only and not machine-readable

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Story transition rationale is comment-only and not machine-readable** — The sprint tracking update relies on a free-form YAML comment for rationale, so tooling cannot query or validate why a story moved between workflow states.
status: open

### DW-223: Story status requires manual dual-write across tracking files

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Story status requires manual dual-write across tracking files** — The workflow duplicates status in both the story artifact and `sprint-status.yaml`, which is a pre-existing consistency risk whenever one file changes without the other.
status: done 2026-09-01
resolution: already resolved: tools/check-story-review-readiness.py:803-817

### DW-224: ValidateResult.IsValid/ErrorMessage is dead code on failure path

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **ValidateResult.IsValid/ErrorMessage is dead code on failure path** — ValidateContentActivity throws exceptions for invalid input; the ValidateResult record is only used for the success path. Spec mandates this contract shape, so keeping as-is.
status: open

### DW-225: SaveDedupKeyActivity: no TTL on dedup keys

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **SaveDedupKeyActivity: no TTL on dedup keys** — Dedup keys persist forever in DAPR state store, preventing re-ingestion of deleted content. Cleanup mechanism belongs to Epic 8 (Story 8.2 consistency verification).
status: open

### DW-226: ContentBytes serialized inline in DAPR workflow state

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **ContentBytes serialized inline in DAPR workflow state** — Base64-encoded byte[] in IngestionInput causes replay amplification for large files. Accepted per D13 (MVP payloads <= 1MB). Same issue as Story 1.3 ExtractionInput. Future: external blob storage for content.
status: done 2026-09-01
resolution: already resolved: commit 906f819f

### DW-227: Duplicate dedup entries are returned without confirming the referenced memory unit still exists

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **Duplicate dedup entries are returned without confirming the referenced memory unit still exists** — The workflow fast-returns on a dedup hit without verifying that the stored `MemoryUnitId` is still present in the indexed backends, so manual cleanup or backend drift can leave callers with a duplicate response that points at missing data.
status: open

### DW-228: indexedAt set to ingestedAt in GraphQueryBuilder

origin: migrated from legacy ledger ("Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **indexedAt set to ingestedAt in GraphQueryBuilder** — `BuildMergeMemoryUnitNode` sets the FalkorDB `indexedAt` property to the workflow's `ingestedAt` timestamp. These are semantically different (when ingestion started vs when the graph write happened). Fixing requires adding a separate `indexedAt` parameter to `IndexInput`, which is a cross-story contract change (Story 1.5).
status: open

### DW-229: CaseId not validated for special characters

origin: migrated from legacy ledger ("Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **CaseId not validated for special characters** — `TenantId` has a strict alphanumeric+hyphen regex via `TenantIdGuard.Validate`, but `CaseId` only checks for null/empty. Not spec-required; CaseId is used as hash field values (not key names or graph names), so the blast radius is limited to potential key scan interference.
status: open

### DW-230: Return `offset` and `maxResults` pagination metadata in search response envelopes

origin: migrated from legacy ledger ("Deferred from: code review of 2-6-explain-mode-and-confidence-scores (2026-04-02)"), 2026-09-01
location: n/a
reason: - **Return `offset` and `maxResults` pagination metadata in search response envelopes** — AC 3 still calls for `offset`, `maxResults`, and `totalCount` in paginated responses, but the response contracts still expose only `TotalCount`. This appears to predate the explain-mode change and would require a broader response-contract update across `SearchResult` and `HybridSearchResult`.
status: open
decision: 2026-09-01 Add pagination metadata — Add fields to paginated envelopes, populate them consistently, update consumers, and test compatibility.
decision: 2026-09-01 Add pagination metadata — Add fields to paginated envelopes, populate them consistently, update consumers, and test compatibility.

### DW-231: InternalsVisibleTo in packable library without strong-name key

origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
location: n/a
reason: - **InternalsVisibleTo in packable library without strong-name key** — `Hexalith.Memories.Redis.csproj` has `IsPackable=true` and `InternalsVisibleTo` for Benchmarks without a strong-name key. Any consumer assembly named `Hexalith.Memories.Benchmarks` could access internals. Pre-existing pattern across the project; low practical risk.
status: done 2026-09-01
resolution: already resolved: commit 01b8cad6

### DW-232: FusionEngine non-finite handling asymmetry across axes

origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
location: n/a
reason: - **FusionEngine non-finite handling asymmetry across axes** — Graph axis skips non-finite scores entirely (`continue` in FusionEngine), while syntactic/semantic axes normalize non-finite to 0.0 via ScoreNormalizer. Both paths produce safe results, but the mechanism differs: a document with a NaN graph-only score is excluded from fusion, while a NaN syntactic-only score becomes 0.0 and is included. Defensible design — graph scores bypass normalizer.
status: done 2026-09-01
resolution: already resolved: commit 14c19428

### DW-233: Case creation is non-atomic across Redis and FalkorDB

origin: migrated from legacy ledger ("Deferred from: code review of 3-1-create-and-list-cases (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. The story already records this as an accepted MVP gap, so it remains deferred for now.
status: done 2026-09-01
resolution: already resolved: commit c350b7ab

### DW-234: Case creation is non-atomic across Redis and FalkorDB

origin: migrated from legacy ledger ("Deferred from: code review of 3-2-case-status-and-activity.md (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` still writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. This remains a pre-existing MVP gap from Story 3.1 rather than a regression introduced by Story 3.2.
status: done 2026-09-01
resolution: already resolved: commit c350b7ab

### DW-235: Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key

origin: migrated from legacy ledger ("Deferred from: 3-3-case-member-management (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key** — Story 3.3 introduces a `:members` Redis Hash key per case for member storage. When Story 3.5 implements case deletion, it must also delete this key alongside the case hash and `:activity` stream to avoid orphaned data.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Cases/DeleteCaseProjectionActivity.cs:46-47

### DW-236: Dedup key orphaning after MU deletion

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Dedup key orphaning after MU deletion** — Deleting a memory unit removes it from RediSearch, Redis Vector, and FalkorDB, but the DAPR state store dedup key persists. Re-ingesting identical content is silently blocked by dedup detection, returning a stale MU ID. Fix: add dedup key TTL or explicit dedup key deletion during MU deletion. Belongs in Epic 8 (Story 8.2 consistency verification).
status: open

### DW-237: Ingestion workflow must check `CaseStatus.Deleting`

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Ingestion workflow must check `CaseStatus.Deleting`** — Story 3.5 sets case status to `Deleting` during case deletion, but the ingestion workflow (`ValidateContentActivity`) does not yet check this status before creating CONTAINS edges. A concurrent ingestion during case deletion could create orphaned MUs. Wire the status check into ingestion validation.
status: open

### DW-238: Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade** — Story 3.5's `DETACH DELETE` removes `annotates` edges but leaves connected annotation MU nodes intact. When Story 3.6 implements annotations, `DeleteMemoryUnitAsync` must first traverse outgoing `annotates` edges, recursively delete annotation MUs, then delete the target MU.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs:31-33

### DW-239: metadataQuery no length/content validation

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **metadataQuery no length/content validation** — The `metadataQuery` query parameter has no length limit or format validation at the endpoint level, unlike `sourceType` (enum-validated) and `caseId` (existence-checked). General input validation concern across all query parameters. [Program.cs:436]
status: open

### DW-240: cancellationToken not propagated in ResolveNamesAsync

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **cancellationToken not propagated in ResolveNamesAsync** — `CaseService.ResolveNamesAsync` accepts a CancellationToken but never passes it to Redis batch operations or Task.WhenAll. StackExchange.Redis batch ops have limited cancellation support; pre-existing pattern in other batch methods. [CaseService.cs:321]
status: open

### DW-241: No input validation on caseId format before Redis key construction

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **No input validation on caseId format before Redis key construction** — `caseId` undergoes no format validation (unlike `tenantId` which has `TenantIdGuard`). A caseId containing `:` is used directly in Redis key patterns. Defense-in-depth gap, though read-only lookups limit blast radius. [Program.cs:472]
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Cases/CaseValidator.cs:161

### DW-242: No error handling for Redis failure in case name enrichment

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **No error handling for Redis failure in case name enrichment** — If Redis fails during the optional `ResolveNamesAsync` call, the entire search request returns 500 even though core search results are already available. Should degrade gracefully by returning results without case names. [Program.cs:988]
status: open

### DW-243: Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Program.cs:1829
reason: - **Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass** — Deferred by review decision. Reason: state-store key format is not empirically verified yet, so the actor fallback is the safer MVP path for now. [src/Hexalith.Memories.Server/Program.cs:1829]
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/TenantEndpointHandlers.cs:76

### DW-244: Breaking-change conflict response still returns the wrong error contract

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Program.cs:1888
reason: - **Breaking-change conflict response still returns the wrong error contract** — `CreateEmbeddingConfigConflictResponse` still emits `error = "EmbeddingConfigChangeRequired"` instead of the pinned `EMBEDDING_CONFIG_BREAKING_CHANGE` response contract. This predates Story 5.5 and was not introduced by the current diff, so it remains deferred here. [src/Hexalith.Memories.Server/Program.cs:1888]
status: open

### DW-245: Per-run Docker named volumes are never torn down

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs:175-181
reason: - **Per-run Docker named volumes are never torn down** — Fixture generates `hexalith-memories-it-<guid>` volumes for test isolation but nothing cleans them up. CI hosts accumulate disk usage over time. [src/Hexalith.Memories.AppHost/Program.cs:175-181]
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1292

### DW-246: `_logProvider` in the fixture accumulates entries across restart lifetimes

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **`_logProvider` in the fixture accumulates entries across restart lifetimes** — `RestartTopologyAsync` does not reset the shared log provider, so any future test code that captures a pre-restart index and inspects post-restart entries will see mixed lifetimes. Latent trap rather than a current bug. [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs]
status: open

### DW-247: `[DataMember]` attributes omit explicit `Name`

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs
reason: - **`[DataMember]` attributes omit explicit `Name`** — Property renames on `CorpusStatistics`, `RateLimitState`, and `CaseIngestionCounts` will silently break wire format for existing persisted actor state. Set `[DataMember(Name = "...")]` explicitly before the next rename. [src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs]
status: open

### DW-248: `BuildDedupKey` duplicates server-side hash logic in the test

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774
reason: - **`BuildDedupKey` duplicates server-side hash logic in the test** — `PipelinePersistenceIntegrationTests.BuildDedupKey` recomputes `SHA256(sourceUri)` exactly the way the server does today. Any future change to URI normalization on the server will be invisible to the test. Replace with a server-side dedup-inspection query or an exposed helper. [tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774]
status: open

### DW-249: AppHost token propagation uses process-env side effects

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs:183-198
reason: - **AppHost token propagation uses process-env side effects** — `ApplyProcessEnvironmentTokens` seeds `APP_API_TOKEN` / `DAPR_API_TOKEN` into the AppHost process environment because CommunityToolkit.Aspire.Hosting.Dapr 9.7 does not expose a sidecar-scoped env API. Tokens leak to every child container/subprocess and are never unset. Revisit when the toolkit exposes a sidecar-env builder. [src/Hexalith.Memories.AppHost/Program.cs:183-198]
status: open

### DW-250: Dictionary iteration order in `AxisDetails` / `Metadata` is not sorted

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, SearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs, MemoryUnitTableFormatter.cs
reason: - **Dictionary iteration order in `AxisDetails` / `Metadata` is not sorted** — `SearchExplanation.AxisDetails` and `MemoryUnit.Metadata` formatters enumerate the underlying `Dictionary<,>` in server-insertion order. Test-fragility for golden snapshots if the server ever reorders its JSON payload; no AC broken today. Sort keys at emit time when that becomes load-bearing. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, SearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs, MemoryUnitTableFormatter.cs]
status: open

### DW-251: `NaN` / `Infinity` confidence or composite scores poison the JSON envelope

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs
reason: - **`NaN` / `Infinity` confidence or composite scores poison the JSON envelope** — If the server ever emits non-finite floats, human/table prints `NaN`/`Infinity` and the JSON envelope emits bare `NaN` tokens that strict parsers (jq, `JSON.parse`, Python `allow_nan=False`) reject. Contracts don't currently enable `AllowNamedFloatingPointLiterals`; treat as contract-boundary work if non-finite scores ever become legitimate. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs]
status: open

### DW-252: `IOutputFormatter<T>.Write` signature has no `CancellationToken`

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs
reason: - **`IOutputFormatter<T>.Write` signature has no `CancellationToken`** — Broken downstream pipe (`memories … | head -1` on a large body) surfaces as "Unexpected error contacting Memories Server" rather than a clean broken-pipe exit; Ctrl+C during a synchronous write has no effect. Signature change is architectural and out-of-scope for 7.2. [src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs]
status: done 2026-09-01
decision: 2026-09-01 Retain bounded sync writes — The bounded payloads do not justify broad API churn.
resolution: closed by human decision: The bounded payloads do not justify broad API churn.
decision: 2026-09-01 Retain bounded sync writes — The bounded payloads do not justify broad API churn.

### DW-253: `Uri.EscapeDataString` on path-segment IDs produces `%2F` for embedded slashes

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810
reason: - **`Uri.EscapeDataString` on path-segment IDs produces `%2F` for embedded slashes** — ASP.NET Core rejects `%2F` in path segments by default (404 Not Found), surfacing as an opaque "not found" error to the user. IDs containing `/` are unusual; the clean fix is CLI-side rejection or server-side `AllowEncodedSlashes`. Server concern. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810]
status: open

### DW-254: `BuildSearchPath` drops subpath when `--endpoint` has one

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath
reason: - **`BuildSearchPath` drops subpath when `--endpoint` has one** — Constructing `"api/search?..."` as a relative URI against an `HttpClient.BaseAddress` like `http://host:5000/v1` drops `/v1` per `Uri` resolution rules. No 7.2 AC exercises subpath endpoints; Story 7.1 owns endpoint normalization. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath]
status: open

### DW-255: `--max-results abc` exits via System.CommandLine default error

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs Options
reason: - **`--max-results abc` exits via System.CommandLine default error** — Non-integer input bypasses the 7.2 "plumbing = 2" exit-code contract because System.CommandLine's built-in parser emits its own error format. Parser-level concern consistent with the Story 7.1 baseline; neither story tests this edge. [src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs Options]
status: open

### DW-256: `IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401
reason: - **`IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`** — spec line 168 allowed "or equivalent — grep first" flexibility, but the signature divergence cascades into validation step's inability to match by `MemoryUnitId`. To be reconsidered during Group 2 (MemoriesClient) review of this story. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]
status: done 2026-09-01
resolution: already resolved: commit c4af9b0c

### DW-257: Port availability check binds `IPAddress.Loopback` only; IPv6-only services missed, and bind-success is TOCTOU-advisory on Windows port reservations

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:182
reason: - **Port availability check binds `IPAddress.Loopback` only; IPv6-only services missed, and bind-success is TOCTOU-advisory on Windows port reservations** — platform-specific caveat is already acknowledged in spec Task 2.4; upgrade to dual-stack check and/or `IpGlobalProperties.GetActiveTcpListeners` lookup when touched next. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:182]
status: open

### DW-258: `EnsureSampleCaseAsync` picks first match of `DefaultCaseName` without stable ordering

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:276-283
reason: - **`EnsureSampleCaseAsync` picks first match of `DefaultCaseName` without stable ordering** — only manifests after repeated failed runs leave duplicate cases in the tenant. Low likelihood; will be neutralized if `CreateCaseAsync` is removed per the open decision. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:276-283]
status: open

### DW-259: `NegativeCanaryQuery` is a literal constant — any future fixture copying the token into a sample body silently breaks the canary invariant

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:31
reason: - **`NegativeCanaryQuery` is a literal constant — any future fixture copying the token into a sample body silently breaks the canary invariant** — add a startup self-check (assert `SampleDocumentText` does not contain the canary token) when touched next. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:31]
status: open

### DW-260: `HealthStep` failure suggestion interpolates `result.LastError` raw

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs:349-354
reason: - **`HealthStep` failure suggestion interpolates `result.LastError` raw** — may surface exception messages with proxy URLs or paths in CI logs. Low sensitivity for a dev wizard; sanitize when the wizard grows a non-local-dev use case. [src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs:349-354]
status: open

### DW-261: `DurationMs` uses `checked((int)Math.Round(...))` — overflows at ~24.85 days and would throw mid-serialization

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs:89
reason: - **`DurationMs` uses `checked((int)Math.Round(...))` — overflows at ~24.85 days and would throw mid-serialization** — real-world risk negligible given the wizard's bounded 60s probe + 30s provisioning. Switch to `long` or clamp if the wizard ever grows unbounded polls. [src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs:89]
status: open

### DW-262: `CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:285-289, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:341
reason: - **`CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)** — docs/dev/experimental-apis.md already lists all three HXL001 methods; the story spec should be amended (TL;DR item 5) to acknowledge `CreateCaseAsync`. Alternative: remove `CreateCaseAsync` and rely on server-side auto-create (requires server support). [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:285-289, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:341]
status: done 2026-09-01
resolution: already resolved: docs/dev/experimental-apis.md:7

### DW-263: `PrerequisiteCheckResult.IsSkipped` is a 7.4 refinement beyond the spec-pinned 3-field signature

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs:18
reason: - **`PrerequisiteCheckResult.IsSkipped` is a 7.4 refinement beyond the spec-pinned 3-field signature** — provides clearer "advisory pass" UX (SKIP vs. OK-with-advisory). Spec Task 2.1 should be amended to acknowledge the 4-field record and the "SKIP for soft-fail" rendering convention. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs:18]
status: open

### DW-264: `CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:451
reason: - **`CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5** (which called for `Task<TenantSummary>` and `Task<MemoryUnit>`). Both server endpoints are 202 Accepted fire-and-forget, so returning the workflow instance id is the honest contract; the spec signatures were drafted before the server surface was verified. Amend spec TL;DR items 4-5 to match and document the polling pattern (`CreateTenantAsync` → poll `GetTenantAsync` until `TenantStatus.Active`; `IngestAsync` → poll via the workflow id route) in the next spec touch. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:451]
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:260

### DW-265: `IngestAsync` has no client-side content-size guard

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401
reason: - **`IngestAsync` has no client-side content-size guard** — server rejects >1 MiB via `IngestionInputValidator` (returns `INVALID_INPUT`) and >2 MiB via `RequestSizeLimitAttribute` (returns 413 with no `ErrorResponse` body, so `ErrorResponseDecoder.DecodeAsync` yields a terse diagnostic). UX polish, not correctness — the quickstart sample is well under 1 MiB. Revisit if `IngestAsync` grows beyond quickstart scope. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]
status: open

### DW-266: Semantic re-index remains intentionally unsupported in `SemanticIndexer`

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-consistency-verification-and-repair (2026-04-20)"), 2026-09-01
location: src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84
reason: - **Semantic re-index remains intentionally unsupported in `SemanticIndexer`** — `SemanticIndexer.ReIndexFromSyntacticAsync` still throws `NotSupportedException`, so `ReIndexSemantic` / `ReIndexSemanticAndGraph` remain documented follow-up work rather than live repair paths. [src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84]
status: open

### DW-267: Classification metadata is not persisted into the syntactic export source of truth

origin: migrated from legacy ledger ("Deferred from: code review fix pass of 8-3-data-export (2026-04-20)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/IndexInput.cs, src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, src/Hexalith.Memories.Server/Cases/CaseService.cs
reason: - **Classification metadata is not persisted into the syntactic export source of truth** — `MemoryUnit.Classification` exists on the contract/export surface, but the ingestion/indexing path (`IndexInput`, `IndexSyntacticActivity`) does not write classification into the Redis memory-unit hash. `CaseService.ParseMemoryUnitFromHash` and `TenantExportService` therefore cannot recover it during export. Fix in a future story by persisting classification at ingest/index time, then plumb it through export. [src/Hexalith.Memories.Contracts/V1/IndexInput.cs, src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, src/Hexalith.Memories.Server/Cases/CaseService.cs]
status: open

### DW-268: Tier-3 Aspire end-to-end integration tests for EventStore subscription

origin: migrated from legacy ledger ("Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)"), 2026-09-01
location: tests/Hexalith.Memories.EventStore.Tests/
reason: - **Tier-3 Aspire end-to-end integration tests for EventStore subscription** — Task 6 guard tests that require a running DAPR sidecar + Redis + FalkorDB are deferred to a follow-up Tier-3 / nightly harness. This covers: `EventIngestionRoundTripTests` (publish via DAPR → search within 5 s, NFR6), `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic` + `Startup_FailsFast_WhenSubscribeEndpointEmpty`, `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency`, and `EventIngestionLatencyTests.SingleEvent_P50Under3s_Enforcement` / `SingleEvent_P95Under5s_Observation`. The existing Tier-1 (65 tests, `tests/Hexalith.Memories.EventStore.Tests/`) + Tier-2 (10 tests, `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/` — outcome mapping, middleware-order, documentation completeness) coverage pins every non-end-to-end guard test from the story's risk table. Rationale: standing up the full Aspire topology for one subscription round-trip is disproportionate to the risk the Tier-2 tests already cover at the controller / outcome-mapping level; the replay-after-restore case is behaviorally correct as long as `CheckIdempotencyActivity` runs (a Tier-2 concern covered by `CheckIdempotencyActivityTests`). The nightly Aspire harness is the right place to catch sidecar / broker wiring regressions.
status: open

### DW-269: Review findings from the planning validation (Review Findings block in the story file)

origin: migrated from legacy ledger ("Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)"), 2026-09-01
location: n/a
reason: - **Review findings from the planning validation (Review Findings block in the story file)** — Several \[Review\]\[Patch\] entries in the story's Review Findings section surfaced during planning iteration and were folded into Tasks 1-4 during implementation (controller `[Topic]` binding, EventStore package boundary with Server adapters, typed router outcomes, compensated hybrid dedup, response DTO with `instanceId` contract, configurable `PubSubName`, canonical middleware order, queryable subject metadata, severity-correct 9100-9129 log bank). Any remaining bullets that did not turn into task-level checkboxes are documentation refinements (e.g. AC #17 wording), not code-level deferrals, and can be cleaned up in a follow-up story pass. Touching the Review Findings block would violate the dev-story "only modify the story file in permitted areas" rule.
status: open

### DW-270: Activity / HttpResponseMessage using-scope pattern inconsistency in retry test

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-end-to-end-telemetry-integration-tests (2026-04-22)"), 2026-09-01
location: n/a
reason: - **Activity / HttpResponseMessage using-scope pattern inconsistency in retry test** — `SearchOperation_RetrySequence_EmitsDistinctAuditEventsPerStatus` uses mixed `using` statement vs `using` statement scopes for `Activity retryRoot` vs `Activity secondAttempt`; the pattern is correct, just inconsistent. Pre-existing style nit, not caused by 8.4 scope boundary.
status: open

### DW-271: `OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease pin — upgrade-on-GA trigger.

origin: migrated from legacy ledger ("Deferred from: 8-5-redis-otel-instrumentation (2026-04-23)"), 2026-09-01
location: Directory.Packages.props, NuGet.config, docs/dev/telemetry.md ADR-8.5-001
reason: - **`OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease pin — upgrade-on-GA trigger.** Package pinned at `1.15.1-beta.1` in `Directory.Packages.props` per ADR-8.5-001 (b). Revisit this pin within **14 days of `1.15.0`** (non-prerelease) shipping on nuget.org, **OR by 2026-09-30**, whichever comes first. Owner: Memories release-manager rotation. Review-by: **2026-09-30**. On review, either (a) bump to the GA version and remove the `-beta.N` tag from `Directory.Packages.props` + update `packageSourceMapping` comment, or (b) file a new deferred-work entry with a fresh review-by if GA is still not shipped. Tracking: ADR-8.5-001 (g). [Directory.Packages.props, NuGet.config, docs/dev/telemetry.md ADR-8.5-001]
status: open

### DW-272: Malformed or truncated Redis breadcrumbs are still silently dropped by `ServerActivityStreamReader`.

origin: migrated from legacy ledger ("Deferred from: code review of 8-5-redis-otel-instrumentation (2026-04-23)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs
reason: - **Malformed or truncated Redis breadcrumbs are still silently dropped by `ServerActivityStreamReader`.** `TryParse(...)` catches `JsonException` and returns `null`, so the Story 8.5 hard Redis-span assertion can report a missing span when the real failure is capture corruption / truncation. Deferred as pre-existing review debt in the existing stderr-breadcrumb reader path. [tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs]
status: open

### DW-273: Task 8.7 — `RateLimiterSizingValidator` (Winston promoted, Improvement AB).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 8.7 — `RateLimiterSizingValidator` (Winston promoted, Improvement AB).** Validator emits `9163 RateLimiterUnderSizedForEvents` at Warning when a tenant's `EmbeddingRateLimiterActor` ceiling is below `sustainedUsage * 2` over a 15-min sliding window. Core dual-embedding path ships without the validator; the degraded-state queue path already protects against cascade failure under doubled API volume. Follow-up: add `RateLimiterSizingValidator.cs` that reuses the retry hosted service's scheduling slot, plus unit tests `.SustainedUnderSizing_Emits9163 / .TransientBurst_DoesNotEmit / .CeilingSufficient_DoesNotEmit`.
status: open

### DW-274: Task 8.8 — `memories retry-nl-embeddings` CLI surface (Improvement F).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 8.8 — `memories retry-nl-embeddings` CLI surface (Improvement F).** Dead-letter inspection + re-enqueue is interim via `redis-cli` commands (documented in operator runbook section 10.1.3). Follow-up: add the sub-command to `Hexalith.Memories.Cli` once that project surfaces — current story scope ships no CLI project. Interim commands: `redis-cli ZCARD nl-embedding-retry-dead:{tenant}` + `redis-cli ZRANGEBYSCORE ... | xargs redis-cli ZADD nl-embedding-retry:{tenant} ...`.
status: open

### DW-275: Task 9.1 — `DualEmbeddingRoundTripTests` (Tier 2/3).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/
reason: - **Task 9.1 — `DualEmbeddingRoundTripTests` (Tier 2/3).** Publishes a test CloudEvent via `DaprClient.PublishEventAsync` and polls both raw + NL hashes for dual indexing within 7s. Requires Aspire `DistributedApplicationTestingBuilder` + DAPR slim + Redis + FalkorDB + `conversation.echo` component. Follow-up: add under `tests/Hexalith.Memories.IntegrationTests/Ingestion/`.
status: open

### DW-276: Task 9.2 — `OutOfOrderEventTests` (Tier 2/3).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.2 — `OutOfOrderEventTests` (Tier 2/3).** Publishes event B (with `causationid = A_id`) before event A; asserts stub node is created with `isStub = true` + `stubCreatedAt` set; publishes A; asserts stub promoted (`isStub = false`, `9154 StubNodeResolved` emitted). Requires FalkorDB integration fixture + replay window.
status: open

### DW-277: Task 9.3 — `DegradedNaturalLanguageEmbeddingTests` (Tier 2, 3 scenarios).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.3 — `DegradedNaturalLanguageEmbeddingTests` (Tier 2, 3 scenarios).** Scenario A: LLM transient failure → `Queued` + retry completes on next tick. Scenario B: index-side partial failure → workflow-level retry recovers. Scenario C: index-side terminal failure → compensation drops both hashes. Requires NSubstitute-replaceable DAPR Conversation client + fault injector for `IndexNaturalLanguageSemanticActivity`.
status: open

### DW-278: Task 9.4 — `CorrelationRootEdgeTests` (Tier 2 FalkorDB fixture).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.4 — `CorrelationRootEdgeTests` (Tier 2 FalkorDB fixture).** Publishes 1 root + 3 correlated events; asserts root has no self-edge, each correlated event has exactly one edge from root, no edges between correlated events, `9155` emitted once. Guard for Risk #3 at the integration level — the unit test already covers the activity-level behavior via `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `.CorrelationIdEqualsMemoryUnitId_NoSelfEdge_LogsDebug`.
status: open

### DW-279: Task 9.5 — `IngestionWorkflowReplaySafetyTests` (Tier 1).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.5 — `IngestionWorkflowReplaySafetyTests` (Tier 1).** Simulates a 9.1-shape history replaying under 9.2 code. Requires fabricating a `durable-task` state snapshot — the SDK surface for this is undocumented. Document the failure mode as deterministic (it is) and rely on the runbook quiesce (AC #11) + `WorkflowReplaySafetyHostedService` startup gate (Task 5.9) as the combined mitigation.
status: open

### DW-280: Per-tenant LLM provider configuration (Phase 2 per architecture L1254).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: deploy/dapr/components/conversation-llm.yaml
reason: - **Per-tenant LLM provider configuration (Phase 2 per architecture L1254).** MVP ships one system-wide `llm` DAPR Conversation component. Operators swap providers by editing `deploy/dapr/components/conversation-llm.yaml` (no code change required). Per-tenant LLM is tracked as a Phase 2 follow-up.
status: open

### DW-281: Content-absent fallback in `GraphTraversalService`

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:~95-100
reason: - **Content-absent fallback in `GraphTraversalService`** (`src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:~95-100`). Retire after the Task 7.6 `IsStubBackfillMigration` has been executed against all production databases. Until then, the fallback MUST be kept — pre-9.2 stubs have neither the `isStub` flag nor content.
status: open

### DW-282: Integration tests for `NaturalLanguageSemanticSearchService`.

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Integration tests for `NaturalLanguageSemanticSearchService`.** The library class ships without being wired into `HybridSearchService` (AC #7 — staged rollout). Follow-up story wires it in behind an opt-in `axis=naturalLanguage` query parameter and adds end-to-end search tests.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:59

### DW-283: Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).** `Dapr.Workflow.WorkflowState` (SDK 1.17.6) does not expose a code-version field per active instance, so `ShouldCountWorkflow` falls back to "any in-flight IngestionWorkflow." Clean same-version redeploys wait for the drain window (up to 5 min). Follow-up: (a) investigate SDK surface across Dapr 1.18+ for a workflow-version metadata hook, (b) if unavailable, thread a `"workflowCodeVersion"` tag through `IngestionInput` and persist to workflow state so the gate can compare against `Assembly.GetEntryAssembly().GetName().Version`. AC #11 updated 2026-04-24 to document the relaxation.
status: open

### DW-284: Risk-mapped guard tests that depend on integration topology

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk-mapped guard tests that depend on integration topology** — `DualEmbeddingLatencyTests` (Risk #2, P95 benchmark), `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` (Risk #11), `GraphQueryBuilderTests.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (Risk #12 Tier-2 FalkorDB fixture), `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` (Risk #17). All bound to the Task 9.x integration-test deferral above.
status: open

### DW-285: Risk #3 coverage gap at unit level

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #3 coverage gap at unit level** — `IndexGraphActivityTests.MultipleEventsSameCorrelationId_NoFanOut` and `GraphTraversalServiceTests.CorrelatedWith_InboundDirection_ReturnsCorrelatedSiblings` — the fan-out-prevention and inbound-traversal tests name-listed in Risk #3 that go beyond the two shipped tests. Tied to Task 9.4 `CorrelationRootEdgeTests`.
status: open

### DW-286: Risk #4 gap-marker unit tests

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #4 gap-marker unit tests** — `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes`. Tied to Task 9.2 `OutOfOrderEventTests`.
status: open

### DW-287: Risk #6 unit tests

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #6 unit tests** — `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` + `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget`. Tied to Task 8.7 `RateLimiterSizingValidator` deferral (Risk #6 guard + follow-up).
status: open

### DW-288: Risk #9 follow-up tests

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #9 follow-up tests** — `MultipleTenantsWithBacklog_FairlyDequeuesAcrossTenants`, `RestartMidIteration_DoesNotDoubleScheduleSameRecord`, `NaturalLanguageEmbeddingRetryWorkflowTests.Idempotency_DuplicateScheduling_DoesNotDoubleIndex`. Tied to Task 9.3 `DegradedNaturalLanguageEmbeddingTests`.
status: open

### DW-289: Orphan stub operator surface

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Orphan stub operator surface** — `OrphanStubQuery_ReturnsStubsOlderThanThreshold` test + `memories_graph_orphan_stub_count{tenant}` gauge + `memories graph orphan-stubs --tenant X --age 24h` CLI sub-command (Dev Notes "Orphan stub detection"). Tied to Task 8.8 CLI project deferral.
status: open

### DW-290: Task 7.1 reflection test

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 7.1 reflection test** — `GraphQueryBuilderTests.AllCallers_PassStubCreatedAt` (reflection-based enumeration of `BuildMergeStubNode` callers asserting 2-arg form). Tied to the patch-level deprecation of the 1-arg overload.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs:242

### DW-291: Task 1.9 Improvement AD dynamic-compilation `ProjectCompilationTests`

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 1.9 Improvement AD dynamic-compilation `ProjectCompilationTests`** — diff ships the weaker file-content-string form (`File.ReadAllText` + regex assertions). The stronger dynamic-compilation variant that builds a throwaway project and asserts zero `DAPR_CONVERSATION` diagnostics is deferred; current form still catches the regression the Improvement AD cared about.
status: open

### DW-292: D1: Logprobs-based confidence extraction in `GenerateNaturalLanguageDescriptionActivity`. Dapr.AI 1.17.6 `ConversationResultChoice` exposes only `FinishReason`, `Index`, and `Message` (verified against `C:/Users/.nuget/packages/dapr.ai/1.17.6/lib/net9.0/Dapr.AI.xml`). The SDK does not surface per-token `logprobs` from the underlying provider response, so the spec-documented `exp(avg(logprob))` computation has no upstream to pull from. Task 2.5 permanently documents `ConfidenceSource = Constant` + `EstimatedConfidence = null` as the MVP behavior. Re-open triggers: (a) Dapr.AI exposes logprobs on `ConversationResultChoice` or an extension surface, OR (b) a follow-up story ships a direct-provider client path (bypassing DAPR) that exposes logprobs — only relevant if operator UX research confirms users want measured confidence numbers. Evidence in comments at `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:~206`.

origin: migrated from legacy ledger ("Deferred from: Session 5 — 9-2 review follow-up (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:~206
reason: - **D1 — Logprobs-based confidence extraction in `GenerateNaturalLanguageDescriptionActivity`.** Dapr.AI 1.17.6 `ConversationResultChoice` exposes only `FinishReason`, `Index`, and `Message` (verified against `C:/Users/.nuget/packages/dapr.ai/1.17.6/lib/net9.0/Dapr.AI.xml`). The SDK does not surface per-token `logprobs` from the underlying provider response, so the spec-documented `exp(avg(logprob))` computation has no upstream to pull from. Task 2.5 permanently documents `ConfidenceSource = Constant` + `EstimatedConfidence = null` as the MVP behavior. Re-open triggers: (a) Dapr.AI exposes logprobs on `ConversationResultChoice` or an extension surface, OR (b) a follow-up story ships a direct-provider client path (bypassing DAPR) that exposes logprobs — only relevant if operator UX research confirms users want measured confidence numbers. Evidence in comments at `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:~206`.
status: open

### DW-293: Retry backpressure `9174 RetryBackpressureOverride` deferred.

origin: migrated from legacy ledger ("Deferred from: Session 5 — 9-2 review follow-up (2026-04-24)"), 2026-09-01
location: 9170/9179
reason: - **Retry backpressure `9174 RetryBackpressureOverride` deferred.** Decision D2 was originally resolved "implement now" but on inspection the `EmbeddingRateLimiterActor` public surface does not currently expose a read-side "budget utilization %" API that the retry hosted service can consume without a new actor method + DI wiring. Shipping a partial implementation risks either (a) always-dequeue (false-negative backpressure) or (b) always-skip until manual bypass (stampede when LLM recovers). Re-open trigger: add `EmbeddingRateLimiterActor.GetCurrentUtilizationAsync()` as part of a follow-up Task 8.5.1, then layer the skip-counter + 9174 override + exponential backoff over the top. The current hosted service's `9170/9179` backlog Warning/Error + the underlying rate-limiter's throttling on the LLM calls themselves cover the acute failure mode (doubled API volume rate-limited at the embedding layer).
status: open

### DW-294: S6-F1. Comparer-rebuild guard test/analyzer for `new Dictionary<string, MetadataField>(...)` without explicit comparer.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F1. Comparer-rebuild guard test/analyzer for `new Dictionary<string, MetadataField>(...)` without explicit comparer.** D6 normalizes Ordinal at the contract boundary (`IngestionInput.Metadata` / `IndexInput.Metadata` `init` accessors), but intermediate copies that build a fresh `Dictionary` without passing the comparer escape the safety net. Follow-up: add a unit test that scans the server project sources for `new Dictionary<string, MetadataField>` calls and asserts each one passes a comparer, OR introduce a Roslyn analyzer.
status: open

### DW-295: S6-F2. Replace `LogGateFailedOpen` side-comment contract with explicit outer-caller log on null return.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F2. Replace `LogGateFailedOpen` side-comment contract with explicit outer-caller log on null return.** `WorkflowReplaySafetyHostedService.cs:75-77` currently relies on a code comment that "TryCountInFlightAsync already logged" — fragile to inner-method refactors. Follow-up: add an outer "gate-bypassed" Critical log whenever the count is null, regardless of inner-method logging.
status: open

### DW-296: S6-F3. Replace reflection-on-private-static in `BuildIndexMetadata_*` test with `internal` + `InternalsVisibleTo`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F3. Replace reflection-on-private-static in `BuildIndexMetadata_*` test with `internal` + `InternalsVisibleTo`.** Test currently uses `BindingFlags.NonPublic | BindingFlags.Static` to reach `IngestionWorkflow.BuildIndexMetadata`. Follow-up: mark the method `internal`, add `InternalsVisibleTo` to `Hexalith.Memories.Server.Tests`, and rewrite the test to call directly.
status: open

### DW-297: S6-F4. Split `LogDiscrepancyDetected` emission for note-only vs. true-discrepancy cases.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F4. Split `LogDiscrepancyDetected` emission for note-only vs. true-discrepancy cases.** Notes now flow through the same Warning logger event as discrepancies, inflating volume for healthy tenants during NL rollout. Follow-up: split into `LogDiscrepancyDetected` (Warn) + `LogConsistencyNoteObserved` (Info/Debug).
status: open

### DW-298: S6-F5. Tighten OCE `when` filter at `WorkflowReplaySafetyHostedService.cs:177-181` to log per-call timeouts even during host shutdown.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F5. Tighten OCE `when` filter at `WorkflowReplaySafetyHostedService.cs:177-181` to log per-call timeouts even during host shutdown.** Currently `when (!cancellationToken.IsCancellationRequested)` swallows the per-call timeout when outer cancellation is concurrent. Follow-up: track the per-call CTS source explicitly and log if it was the cause regardless of outer state.
status: open

### DW-299: S6-F6. Move `MetadataField = typeof(WorkflowState).GetField(...)` from static cctor to `Lazy<FieldInfo?>` with try/catch + 9173 emission.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F6. Move `MetadataField = typeof(WorkflowState).GetField(...)` from static cctor to `Lazy<FieldInfo?>` with try/catch + 9173 emission.** A missing/version-mismatched `Dapr.Workflow` assembly currently throws `TypeInitializationException` on first hosted-service invocation, bypassing the 9173 path entirely. Follow-up: lazy + structured log.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/HostedServices/WorkflowReplaySafetyHostedService.cs:216

### DW-300: S6-F7. Map free-text-only `ConsistencyNote` (kind=None, note≠empty) to a typed sentinel in `BuildConsistencyNoteKind`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F7. Map free-text-only `ConsistencyNote` (kind=None, note≠empty) to a typed sentinel in `BuildConsistencyNoteKind`.** Consumers cannot pattern-match on `None`. Follow-up: extend `BuildConsistencyNoteKind` to return `ConsistencyNoteKind.Other` (or similar) when free-text is present without a kind.
status: open

### DW-301: S6-F8. Add inner-loop deadline check to `WorkflowReplaySafetyHostedService.TryCountInFlightAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F8. Add inner-loop deadline check to `WorkflowReplaySafetyHostedService.TryCountInFlightAsync`.** With 100-instance pages and 10s per-query timeout, hundreds of active instances can outlive the documented 5-min `TotalTimeout`. Follow-up: thread the outer deadline through the inner enumeration.
status: open

### DW-302: S6-F9. Add `BatchSize=1` accumulator test for the notes/discrepancies routing across batch boundaries.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-F9. Add `BatchSize=1` accumulator test for the notes/discrepancies routing across batch boundaries.** The current `[10_001]` test always bumps batches in chunks of 64 (default); a `BatchSize=2` mixed-result test would catch carry-over off-by-one.
status: open

### DW-303: S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: docs/dev/consistency.md
reason: - **S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.** Resolves S6-D4 (9173 EventId now overloaded with `workflow-name-reflection-null` / `sidecar-query-timeout` / `sidecar-query-exception` / `metadata-field-missing`) and S6-P15 (consistency.md still describes Discrepancies as the all-encompassing list, missing the Notes split + independent-cap behavior). One docs PR covers both: enumerate the four 9173 reasons + per-reason triage steps; update the consistency.md "Discrepancies" section to describe the structural split and the new EventId 8210 NotesListTruncated.
status: open

### DW-304: S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.** Currently the `{Recommendation}` field is overloaded with `ConsistencyNoteKind` strings (e.g., `Recommendation NaturalLanguageEmbeddingMissing`) for note-only entries. This is a wire-shape change for log consumers; should land alongside S6-FA so the operator runbook can be updated atomically.
status: open

### DW-305: S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs
reason: - **S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.** Existing CLI JSON round-trip already exercises these properties end-to-end; the standalone source-gen contract test is helpful but not blocking. Track in `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs`.
status: open

### DW-306: S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.** Constructing `WorkflowState` requires test-double scaffolding the SDK does not document; gated on Story 9.x integration-test infrastructure.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/HostedServices/WorkflowReplaySafetyHostedService.cs:216

### DW-307: S6-FE. Normalize the `ConsistencyVerificationResultHumanFormatter` "notes: none" / "Notes:" header layout across the discrepancy-empty / discrepancy-populated branches.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FE. Normalize the `ConsistencyVerificationResultHumanFormatter` "notes: none" / "Notes:" header layout across the discrepancy-empty / discrepancy-populated branches.** Cosmetic; minimal operator impact.
status: open

### DW-308: S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.** Existing tests pass against the corrected D6 column semantic; this is a test-quality follow-up. <!-- End of deferred items -->
status: open

### DW-309: W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: actions/*
reason: - **W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.** All five third-party `actions/*` references in `.github/workflows/release.yml` are now pinned to a 40-char commit SHA with a trailing `# v<x.y.z>` comment for review context. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha` enforces the SHA shape so a future bump back to a floating tag fails the test.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-310: W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: src/**/*.csproj
reason: - **W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.** The validator now iterates every `src/**/*.csproj`, requires an explicit `<IsPackable>true|false</IsPackable>` declaration, and asserts every project is in exactly one of `packages` or `nonPackableProjects`. Coverage in `tests/tooling/release_packages/release_packages_test.py` exercises missing/unexpected/duplicate inventory entries and missing/blank/unsupported `IsPackable` values.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-311: W3. `tools/test.sh` Python heredoc has no error path if `python3` is missing.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh
reason: - **W3. `tools/test.sh` Python heredoc has no error path if `python3` is missing.** Linux/macOS runners always have it; Windows uses `test.ps1`. Add `command -v python3` guard with a clear error if Linux/macOS distros are added later that omit it. (`tools/test.sh:134`)
status: open

### DW-312: W4. Python `{*}Counters` namespace XPath requires Python ≥ 3.8.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh:140
reason: - **W4. Python `{*}Counters` namespace XPath requires Python ≥ 3.8.** Current ubuntu-latest ships 3.10+; document as a runner-version floor or hardcode the TRX namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`. (`tools/test.sh:140`, `tools/verify-integration-fast-coverage.py:54`)
status: open

### DW-313: W5. `if-no-files-found: error` + `if: always()` upgrades a build failure to two red checks.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:67-74,108-115
reason: - **W5. `if-no-files-found: error` + `if: always()` upgrades a build failure to two red checks.** Noisy but correct; reviewers must read the build step first. Resolve by gating artifact upload on `success() || failure()` against the test step specifically. (`.github/workflows/ci.yml:67-74,108-115`)
status: open

### DW-314: W6. `submodules: recursive` cannot fetch private submodules without PAT.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:30,52,84
reason: - **W6. `submodules: recursive` cannot fetch private submodules without PAT.** `Hexalith.Commons` and `Hexalith.EventStore` are public today; revisit if either becomes private. (`.github/workflows/ci.yml:30,52,84`; `release.yml:24`)
status: done 2026-09-01
resolution: already resolved: .github/workflows/ci.yml:43

### DW-315: W7. `Substitute.For<WorkflowActivityContext>()` may fail if Dapr.Workflow seals the type in a future SDK.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs:330
reason: - **W7. `Substitute.For<WorkflowActivityContext>()` may fail if Dapr.Workflow seals the type in a future SDK.** Works against 1.17.6; failure mode is loud (NSubstitute throws at instantiation). (`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs:330`)
status: open

### DW-316: W9. CONTRIBUTING.md skip wording (`Requires Docker - see CONTRIBUTING.md`) is documented but not wired into any test SkipAttribute.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **W9. CONTRIBUTING.md skip wording (`Requires Docker - see CONTRIBUTING.md`) is documented but not wired into any test SkipAttribute.** Spec text is aspirational, not enforced as a contract. Wire it via a custom SkipAttribute when Docker-required local skips are needed. (`CONTRIBUTING.md:76-81`)
status: open

### DW-317: W11. `branch-protection.md` is a manual checklist with no automation.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/rulesets/main.json
reason: - **W11. `branch-protection.md` is a manual checklist with no automation.** Commit a `.github/rulesets/main.json` and a daily audit workflow. Out of scope for 11.x. (`docs/dev/branch-protection.md`)
status: open

### DW-318: W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/release-packages.json
reason: - **W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.** New `tools/release-packages.schema.json` now defines required keys, `additionalProperties: false`, and `pattern`/`minItems` constraints. The inventory references the schema via `$schema` and `validate-release-packages.ps1` invokes `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`, `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-319: W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: Cli/README.md
reason: - **W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.** Either ship the tool first, or document `--prerelease` until 1.0.0 lands. (`src/Hexalith.Memories.Cli/README.md:7-9`)
status: done 2026-09-01
resolution: already resolved: docs/dev/release-runbook.md:280

### DW-320: W14. CI workflow `fetch-depth: 0` not set on PR checkout.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:28-30
reason: - **W14. CI workflow `fetch-depth: 0` not set on PR checkout.** commitlint isn't run in CI yet (only locally per CONTRIBUTING); add when CI adopts commit validation. (`.github/workflows/ci.yml:28-30`)
status: done 2026-09-01
resolution: already resolved: .github/workflows/ci.yml:43

### DW-321: W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/tooling/release_packages/
reason: - **W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.** `ConvertTo-NormalizedNuGetVersion` strips `+...` build metadata before equality compare, emits a `Note:` diagnostic naming both the original and NuGet-normalized form, and threads the normalized value through both the per-package version assertion and the internal cross-package dependency-version assertion. Coverage: `test_version_with_build_metadata_normalizes_with_clear_message` in `tests/tooling/release_packages/`.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-322: W16 [partially resolved in 14.2]. `Where-Object {-notlike *.snupkg}` masks regression risk.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W16 [partially resolved in 14.2]. `Where-Object {-notlike *.snupkg}` masks regression risk.** `tools/validate-release-packages.ps1` now uses `Where-Object { $_.Extension -ieq '.nupkg' }` for explicit extension matching. The mirror in `tools/publish-nuget.ps1:40-42` is intentionally not touched in 14.2 because the story's file scope only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic alignment alone does not meet that bar. Re-open trigger: a partial-publish recovery story that already touches `publish-nuget.ps1`, or first .snupkg-symbol introduction.
status: open

### DW-323: W17. `verify-integration-fast-coverage.py` exit codes don't distinguish "missing surface" from "tool error".

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/verify-integration-fast-coverage.py
reason: - **W17. `verify-integration-fast-coverage.py` exit codes don't distinguish "missing surface" from "tool error".** Both yield exit 1; use distinct codes (e.g., 2 for parse error, 3 for empty results, 1 for missing surfaces). (`tools/verify-integration-fast-coverage.py`)
status: open

### DW-324: W18. CI `runs-on: ubuntu-latest` is unpinned.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml
reason: - **W18. CI `runs-on: ubuntu-latest` is unpinned.** Works today; pin to `ubuntu-22.04` if Docker engine version drift causes Testcontainers regression. (`.github/workflows/ci.yml`)
status: open

### DW-325: W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/publish-nuget.ps1 --skip-duplicate
reason: - **W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.** Story 14.2 keeps `cancel-in-progress: false` deliberately because cancelling a release mid-publish would convert a recoverable partial-publish into an indeterminate half-state — `tools/publish-nuget.ps1 --skip-duplicate` rerun-and-self-heal recovery requires that the in-flight release runs to completion. The 30-minute job timeout and the partial-publish issue alert (S11-FD) bound the worst-case stuck-release window. `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces the policy so a future flag flip lands with an explicit recovery model rather than a silent edit.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-326: W20. Release workflow runs build+restore+test+pack twice.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **W20. Release workflow runs build+restore+test+pack twice.** Pre-release validation + semantic-release internal pack pipeline duplicate work; optimize when CI minutes become a constraint.
status: open

### DW-327: W21. `tools/test.sh` Slow/Integration arms collapsed via `|`.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh
reason: - **W21. `tools/test.sh` Slow/Integration arms collapsed via `|`.** Functionally correct today (same project list); diverges from `test.ps1`. Resync if Slow ever gets its own list. (`tools/test.sh:79`)
status: open

### DW-328: W22. `PublicContractSerializationCoverageTests` uses name-suffix filter (`Validator`/`Defaults`/`Taxonomy`).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/Hexalith.Memories.Contracts.Tests/V1/PublicContractSerializationCoverageTests.cs:54-58
reason: - **W22. `PublicContractSerializationCoverageTests` uses name-suffix filter (`Validator`/`Defaults`/`Taxonomy`).** Fragile but works today; replace with `[ExcludeFromContractCoverage]` attribute when a false-positive is observed. (`tests/Hexalith.Memories.Contracts.Tests/V1/PublicContractSerializationCoverageTests.cs:54-58`)
status: open

### DW-329: W23. `CiTestInventoryTests` uses `Contains` for workflow text.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:66-68
reason: - **W23. `CiTestInventoryTests` uses `Contains` for workflow text.** Too permissive; a future workflow refactor adding another `dotnet test` step with the wrong arguments still passes the assertion. Replace with structural YAML parsing. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:66-68`)
status: open

### DW-330: W24. `CiTestInventoryTests` opaque error if `RepoRoot` AssemblyMetadata missing.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:79-84
reason: - **W24. `CiTestInventoryTests` opaque error if `RepoRoot` AssemblyMetadata missing.** Minor diagnostic improvement; emit a wire-up hint message. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:79-84`)
status: open

### DW-331: S11-FB. Compile-time symbol verification for `tools/integration-fast-required-surfaces.txt` (review patch P9).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/integration-fast-required-surfaces.txt
reason: - **S11-FB. Compile-time symbol verification for `tools/integration-fast-required-surfaces.txt` (review patch P9).** Currently the verifier surfaces missing classes only after CI runs the lane. Promoting the check to compile-time requires either (a) a `ProjectReference` to `Hexalith.Memories.IntegrationTests` from a Docker-free test project (pulls integration deps), (b) a refactor of the surfaces file into a typed C# inventory consumed by both the verifier (after build) and a unit test, or (c) a `dotnet test --list-tests` step in CI before the test run. Re-open trigger: first surface drift incident, or the next time the surfaces list grows past ~10 entries.
status: open

### DW-332: S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).** `tagFormat: "v${version}"` collides with stale tags from manual or aborted releases. Currently the natural `git push tag` failure is the gate. Adding a structured pre-flight requires running `npx semantic-release --dry-run` to compute `nextRelease.version` (wasteful on every release) or carrying our own version-computation logic. Story 14.2 reassessed and chose to carry forward — neither preflight option meets the cost/benefit bar without an observed collision. Re-open trigger: first stale-tag-collision incident on `main`, migration from another release tool that left tags behind, or a release-time decision that the dry-run cost is acceptable. Defer-by: 2026-08-04 (re-evaluate at the next release-pipeline hardening pass if no triggering incident occurs).
status: done 2026-09-01
resolution: already resolved: commit 3445e66b

### DW-333: S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/publish-nuget.ps1
reason: - **S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).** Story 14.2 audited the existing path and confirmed it satisfies the spec: `tools/publish-nuget.ps1` writes a structured `publish-summary.json` (pushed/failed/notAttempted), emits a `PARTIAL PUBLISH - manual reconciliation required` GitHub Actions error annotation, and appends a Markdown step summary; `.github/workflows/release.yml` invokes `tools/create-partial-publish-issue.ps1` on workflow failure; the helper opens or comments on a `PARTIAL PUBLISH <version>` GitHub Issue with the run URL, status, package lists, and runbook reference. Coverage in `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail (`publish-failed`), middle-package-fail (`partial-publish`), pre-push validation failure, issue creation, issue commenting on rerun, and helper skip on `publish-failed` status. Operator recovery is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes (HTTP 409 vs non-409 distinction, rerun-and-self-heal contract).
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-334: 12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.** `docs/dev/release-runbook.md` now ships a `Per-Release Package Audit Evidence` subsection that requires SHA-256 capture for every future release alongside the Package Evidence table, with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands and explicit equivalents (`dotnet nuget verify --all`, `nuget verify -Signatures`) for signature-based provenance. The historical `v1.2.0` block remains as-is because the source CI artifacts are no longer available locally; the requirement applies to releases after Story 14.2.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-335: 12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.** `docs/dev/release-runbook.md` adds a `Release Identity And Forensic Anchors` section that pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`) as the canonical token identity for tag, GitHub Release, and package-asset writes; lists the four anchors reviewers should capture per release (Actions run URL, tag commit SHA + tagger identity, Release "Created by", trigger event); and treats anything else as a forensic red flag.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-336: 12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: .github/workflows/release.yml:18
reason: - **12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.** `release.yml`'s skip-CI guard checks `head_commit.message` for the substring. A merge commit whose squash body legitimately contains `[skip ci]` (quoting another commit, copying changelog text) would silently suppress the release. Branch protection now blocks direct pushes, so the only producer of merge commits is PRs. Re-open trigger: first observed silently-skipped release. (`.github/workflows/release.yml:18`)
status: done 2026-09-01
resolution: already resolved: commit 3445e66b

### DW-337: 12.1-RV4. Verify `package-lock.json` is tracked in git.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV4. Verify `package-lock.json` is tracked in git.** Sprint-status comment dated 2026-04-26 (Epic 11 closeout, P1) flagged the file as in working tree but untracked. `v1.2.0` shipped successfully on 2026-04-30, which implies `npm ci` worked, but neither this story's runbook nor the Dev Agent Record confirms `package-lock.json` is committed. Re-open trigger: first `npm ci` failure on a fresh clone, or sweep of Epic 11 leftover P-items. (`package-lock.json`)
status: done 2026-09-01
resolution: already resolved: package-lock.json:1

### DW-338: 12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.** File Scope explicitly permits an `UPDATE only for a cross-link to the runbook`; spec intent treats the runbook as the new operational source of truth. Adds discoverability without scope creep into Story 12.2. (`CONTRIBUTING.md`)
status: done 2026-09-01
resolution: already resolved: CONTRIBUTING.md:541

### DW-339: 12.1-RV6 [resolved in 12.1].

origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
location: n/a
reason: - **12.1-RV6 [resolved in 12.1].** Cross-reference of S11-FA / Story 12.6 from release runbook recovery notes was applied as a patch during the 12.1 code-review pass instead of being deferred. Operator-context only; the resolution of S11-FA itself remains tracked under S11-FA and Story 12.6.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-340: 12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:37
reason: - **12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.** `.github/workflows/ci.yml:37` masks auth/network/repository-rename errors and silently degrades the story-scope diff to `git diff-tree -r HEAD` (every file in HEAD). Drop `|| true` so fetch failures are loud. Out of Story 12.4 file scope; should land in a CI-hardening story.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-341: 12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:39
reason: - **12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.** `.github/workflows/ci.yml:39` — `A...B` requires a reachable merge-base, which depth=1 cannot guarantee. Either fetch enough history (`--depth=50` or `--unshallow`) or switch to 2-dot semantics. Out of Story 12.4 file scope.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-342: 12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:36-46
reason: - **12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.** `.github/workflows/ci.yml:36-46` — when `origin/main` after fetch equals `head_sha`, `git diff` returns empty and the validator silently passes. A direct push to main bypasses story-scope checks entirely. Branch protection should normally prevent this, but the workflow should fail loudly when the diff is empty under push-to-main.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-343: 12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:51-55
reason: - **12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.** `.github/workflows/ci.yml:51-55` — predictable delimiter that a hostile branch name could contain. Defense-in-depth; replace with a randomized sentinel.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-344: 12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:23-27
reason: - **12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.** `.github/workflows/ci.yml:23-27` — when both `PR_HEAD_REF` and `GITHUB_REF_NAME` are empty, downstream errors blame "no story key" instead of identifying the missing env. Hard-fail at the env-binding step.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-345: 12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:189-202
reason: - **12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:189-202` — tokens `baseline`, `test-release.ps1`, `release lane` drive classification. Schema-strengthen the deferred-work entry format (e.g., a `Filter:` line per entry) and parse structure rather than prose. Follow-up to the patches landed in this review pass.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-346: 12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:170-178
reason: - **12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:170-178` — inconsistent with trailer multi-key rejection. Story 12.3 territory; reject loudly to mirror trailer behavior.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-347: 12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:183-185
reason: - **12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:183-185` — same asymmetry as 12.4-RV7. Story 12.3 territory.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-348: 12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:13-16
reason: - **12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.** `tools/check-story-file-scope.py:13-16` — single-letter third segment, trailing-hyphen rejection are not directly tested. Story 12.3 territory.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-349: 12.4-RV10. `extract_backtick_path` silently drops bare-token bullets without an author-facing diagnostic.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:204-212
reason: - **12.4-RV10. `extract_backtick_path` silently drops bare-token bullets without an author-facing diagnostic.** `tools/check-story-file-scope.py:204-212` — author who forgets backticks gets no warning. Story 12.3 author UX.
status: open

### DW-350: 12.4-RV11. `to_posix(path)` embeds Windows drive letter in diagnostic header.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:347-348
reason: - **12.4-RV11. `to_posix(path)` embeds Windows drive letter in diagnostic header.** `tools/check-story-file-scope.py:347-348` — emit `story_path.relative_to(REPO_ROOT).as_posix()` instead. Story 12.3 territory; cosmetic.
status: open

### DW-351: 12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:20,222-228
reason: - **12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.** `tools/check-story-file-scope.py:20,222-228` — Markdown's nested-fence form is supported by parsers but breaks the toggle. Story 12.3 territory.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-352: 12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:243-247
reason: - **12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.** `tools/check-story-file-scope.py:243-247` — only known section markers should terminate the allow-list. Story 12.3 territory.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-353: 12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:133-141
reason: - **12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.** `tools/check-story-file-scope.py:133-141` — emit a clean `ValidationError` with actionable message. Story 12.3 territory.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-354: 12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1108-1120
reason: - **12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.** `tests/tooling/story_scope/story_scope_validator_test.py:1108-1120` — could mask future validator regressions where Out-of-scope sections gain a blank-line continuation. Test-helper hardening.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-355: 12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1196-1199
reason: - **12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.** `tests/tooling/story_scope/story_scope_validator_test.py:1196-1199` — passes today but would silently co-exist with a future conflict-detection regression that exits 0. Test hardening.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-356: 12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1324-1334
reason: - **12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.** `tests/tooling/story_scope/story_scope_validator_test.py:1324-1334` — `assertIn` only checks stdout; if the error path moves to stderr, the test silently breaks. Test hardening.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-357: 12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/tooling/story_scope/story_scope_validator_test.py:1426-1456
reason: - **12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.** `tests/tooling/story_scope/story_scope_validator_test.py:1426-1456` — a future loader-precedence bug could silently load a different file. Test hardening.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-358: 12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1041
reason: - **12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1041` — em-dash, colon, or lowercase variants are silently ignored. Today all S11-F* entries use the literal-period format, so future-resilience only. Re-open trigger: first deferred-work format change.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-359: 12.4-RV20. AC #1 strict literal per-SHA replay drill.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: n/a
reason: - **12.4-RV20. AC #1 strict literal per-SHA replay drill.** Story 12.4 satisfied AC #1 via HEAD-replay coverage (HEAD strictly includes Epic 8.x SHA `d7495a3`, Epic 9.x SHA `bc4d5cc`, and Epic 10.x SHA `8207b54` in its ancestry, and the surviving test inventory at HEAD is a superset of those completion states — see Story 12.4 Decision Resolutions D3). A literal interpretation of AC #1 would also exercise each anchor SHA via `git checkout`, restore, build, and run both authoritative lanes against that exact tree. Re-open trigger: a release post-mortem that traces a regression to a test that existed at one of the named SHAs and was silently fixed before HEAD; or a future quality-discipline story that prefers strict literal AC #1 evidence over inheritance argumentation.
status: open

### DW-360: 12.4-RV1: closed. `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: .github/workflows/ci.yml
reason: - **12.4-RV1 — closed.** `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with `|| true` and no longer falls back to `git diff-tree -r HEAD`. Fetch failures exit 1 with a `::error::story-file-scope:` diagnostic that names the failed operation.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-361: 12.4-RV2: closed. The push-fallback path now resolves an explicit `base_sha=$(git rev-parse

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: actions/checkout@v6
reason: - **12.4-RV2 — closed.** The push-fallback path now resolves an explicit `base_sha=$(git rev-parse origin/main)` and runs a 2-dot `git diff --name-only "$base_sha" "$head_sha"`, on top of `actions/checkout@v6`'s `fetch-depth: 0` clone. No more 3-dot reachability against shallow history.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-362: 12.4-RV3: closed. When `origin/main` resolves to the same commit as `head_sha`, the job

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: origin/main
reason: - **12.4-RV3 — closed.** When `origin/main` resolves to the same commit as `head_sha`, the job exits 1 with a direct-push / empty-diff diagnostic instead of silently passing file-scope validation.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-363: 12.4-RV4: closed. `BRANCH_NAME` heredoc delimiter is randomized per run as

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV4 — closed.** `BRANCH_NAME` heredoc delimiter is randomized per run as `STORY_SCOPE_EOF_$(date +%s%N)_${$}_${RANDOM}_${RANDOM}` so a hostile branch name cannot collide with the closer.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-364: 12.4-RV5: closed. Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV5 — closed.** Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the job at the env-binding step with a diagnostic that names the missing variable; "no story key resolved" no longer hides a missing-env case.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-365: 12.4-RV7: closed. `--story-key` rejects values containing more than one story key and lists

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV7 — closed.** `--story-key` rejects values containing more than one story key and lists every detected key. Test: `test_multiple_keys_in_explicit_story_key_value_fails_with_all_keys_reported`.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-366: 12.4-RV8: closed. Branch-name parsing rejects branches whose value contains more than one

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: \w-
reason: - **12.4-RV8 — closed.** Branch-name parsing rejects branches whose value contains more than one distinct story key (separated by a non-`[\w-]` character such as `/`) and lists every detected key. Test: `test_multiple_keys_in_branch_name_fails_with_all_keys_reported`.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-367: 12.4-RV9: closed. Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection,

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV9 — closed.** Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection, uppercase normalization, and single-letter title segment.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-368: 12.4-RV12: closed. `parse_allowed_scope` tracks the open fence's marker character and

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV12 — closed.** `parse_allowed_scope` tracks the open fence's marker character and length so fences longer than three backticks containing nested 3-backtick fences (and tilde fences containing nested backtick fences) both parse correctly. Tests: `test_parser_handles_fences_longer_than_three_backticks`, `test_parser_handles_tilde_fence_with_nested_backtick_fence`.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-369: 12.4-RV13: closed. Allow-list collection terminates only on known section labels

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: Read/verify only:
reason: - **12.4-RV13 — closed.** Allow-list collection terminates only on known section labels (`Read/verify only:`, `Forbidden by default:`, including their `**bold:**` variants) or `## ` headings; bullets whose rationale ends with `:` are no longer dropped. Tests: `test_parser_does_not_terminate_on_bullet_with_trailing_colon_rationale`, `test_parser_terminates_on_known_section_label_only`, `test_parser_does_not_terminate_on_unrecognized_prose_with_trailing_colon`.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-370: 12.4-RV14: closed. `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git`

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV14 — closed.** `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git` catch `FileNotFoundError` and raise a clean `ValidationError` naming `git interpret-trailers` with an install / `PATH` hint. No Python traceback reaches contributors. Test: `test_missing_git_interpret_trailers_reports_clean_validation_error`.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-371: 12.4-RV15: closed. `section_block` helper no longer terminates on blank lines; only

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV15 — closed.** `section_block` helper no longer terminates on blank lines; only non-blank, non-bullet lines end a section.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-372: 12.4-RV16: closed. `test_branch_and_trailer_agreement_passes` asserts the diagnostic does

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV16 — closed.** `test_branch_and_trailer_agreement_passes` asserts the diagnostic does NOT contain `Conflicting story keys`, so a future regression that exits 0 while emitting the conflict diagnostic cannot silently co-exist with the test.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-373: 12.4-RV17: closed. `test_unparseable_explicit_story_key_fails_closed` matches against

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV17 — closed.** `test_unparseable_explicit_story_key_fails_closed` matches against combined stdout + stderr via the `stdio()` helper, so the test does not break silently if the error path moves between sinks.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-374: 12.4-RV18: closed. `test_fixture_test_reports_loaded_story_artifact_path` pins the full

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV18 — closed.** `test_fixture_test_reports_loaded_story_artifact_path` pins the full `Story artifact:` line under the fixture artifacts root, so a future loader-precedence bug that loads a different file fails loudly.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-375: 12.3-RV15: closed. Multi-block `Allowed files for this story:` parsing is now exercised by

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.3-RV15 — closed.** Multi-block `Allowed files for this story:` parsing is now exercised by `test_parser_merges_multiple_allowed_files_blocks`. The validator merges entries across blocks consistently; future shape drift fails the test instead of changing scope silently.
status: done 2026-09-01
resolution: already resolved: commit 749c4e7c

### DW-376: 12.4-RV6: out of 14.1 scope (CI test inventory parser, not the story-scope lane).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV6** — out of 14.1 scope (CI test inventory parser, not the story-scope lane).
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-377: 12.4-RV10: the existing `Out-of-scope files:` diagnostic surfaces dropped bare-token bullets

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV10** — the existing `Out-of-scope files:` diagnostic surfaces dropped bare-token bullets whenever a contributor's changed-file landing references one. A separate parse-time stderr warning would help story authors before any commit, but adding it was not part of 14.1's ACs and risks noisy false positives on legitimate non-bullet prose. Re-open trigger: an author-confusion incident or a story template redesign that needs pre-commit author warnings.
status: open

### DW-378: 12.4-RV11: cosmetic only. CI uses a repo-relative `_bmad-output/implementation-artifacts`

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts
reason: - **12.4-RV11** — cosmetic only. CI uses a repo-relative `_bmad-output/implementation-artifacts` artifacts root, so production diagnostics never embed a drive letter; the issue surfaces only in local Windows runs. Re-open trigger: a maintainer-visible diagnostic that exposes a local Windows path in a maintainer-facing channel.
status: open

### DW-379: 12.4-RV19: out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV19** — out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-380: 12.4-RV20: out of 14.1 scope (Story 12.4 strict-literal AC #1 evidence drill).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV20** — out of 14.1 scope (Story 12.4 strict-literal AC #1 evidence drill).
status: open

### DW-381: W1: closed. `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`,

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: .github/workflows/release.yml
reason: - **W1 — closed.** `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/cache`, and `actions/upload-artifact` to 40-char commit SHAs with trailing `# v<x.y.z>` comments. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha` enforces the SHA shape so a future revert to a floating major-tag fails the test.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-382: W2: closed. `tools/validate-release-packages.ps1` iterates every `src//*.csproj`, requires

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W2 — closed.** `tools/validate-release-packages.ps1` iterates every `src/**/*.csproj`, requires an explicit `<IsPackable>true|false</IsPackable>` declaration, asserts the project appears in exactly one inventory bucket, and rejects missing/blank/unsupported `IsPackable` values. New Python fixture suite at `tests/tooling/release_packages/release_packages_test.py` covers missing/unexpected/duplicate inventory entries, both bucket misuses, and the three IsPackable failure modes via temporary sentinel csproj files under `src/`.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-383: W12: closed. `tools/release-packages.schema.json` defines required keys with

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/release-packages.schema.json
reason: - **W12 — closed.** `tools/release-packages.schema.json` defines required keys with `additionalProperties: false`, `pattern` constraints on IDs and project paths, and `uniqueItems`. `tools/release-packages.json` now references the schema via `$schema`, and the validator runs `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`, `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-384: W15: closed. `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **W15 — closed.** `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the NuGet-comparable `1.2.3` via `ConvertTo-NormalizedNuGetVersion`, emits a `Note:` diagnostic naming both forms, and threads the normalized value through both per-package and internal cross-package dependency-version assertions. `pack-release.ps1` is unchanged because semantic-release passes versions without build metadata in the CI path.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-385: W19: closed. `concurrency: cancel-in-progress: false` is preserved deliberately to keep

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/publish-nuget.ps1 --skip-duplicate
reason: - **W19 — closed.** `concurrency: cancel-in-progress: false` is preserved deliberately to keep rerun-and-self-heal partial-publish recovery viable (`tools/publish-nuget.ps1 --skip-duplicate`). An inline comment in `release.yml` documents the trade-off and `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces it.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-386: S11-FD: closed. Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation,

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1
reason: - **S11-FD — closed.** Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation, step-summary, and `tools/create-partial-publish-issue.ps1` issue/comment path were audited as sufficient. `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail, middle-package-fail, pre-push validation failure, issue creation, issue commenting on rerun, and helper skip on `publish-failed` status. Operator recovery (HTTP 409 vs non-409, rerun contract) is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-387: 12.1-RV1: closed. `docs/dev/release-runbook.md` Per-Release Package Audit Evidence

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV1 — closed.** `docs/dev/release-runbook.md` Per-Release Package Audit Evidence subsection now requires SHA-256 capture for every release with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands, plus `dotnet nuget verify --all` and `nuget verify -Signatures` as audit-equivalent options for signature-based provenance. Historical `v1.2.0` is not retroactively backfilled.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-388: 12.1-RV2: closed. `docs/dev/release-runbook.md` Release Identity And Forensic Anchors

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **12.1-RV2 — closed.** `docs/dev/release-runbook.md` Release Identity And Forensic Anchors section pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`) as the canonical token identity and lists the four anchors reviewers must capture per release.
status: done 2026-09-01
resolution: already resolved: commit e4318116

### DW-389: W3..W11, W13, W14, W17, W18, W20..W24

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **W3..W11, W13, W14, W17, W18, W20..W24** — out of 14.2 scope (CI workflow, test infra, and contracts/CLI hardening unrelated to the release-lane audit). 14.2 limits its file scope to release-pipeline artifacts.
status: open

### DW-390: W16: partially closed. Cleaned up in `tools/validate-release-packages.ps1`. The mirror in

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W16 — partially closed.** Cleaned up in `tools/validate-release-packages.ps1`. The mirror in `tools/publish-nuget.ps1` is intentionally not touched in 14.2 because the story's file scope only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic alignment alone does not meet that bar.
status: open

### DW-391: S11-FB: out of 14.2 scope (compile-time symbol verification for integration-fast surfaces).

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **S11-FB** — out of 14.2 scope (compile-time symbol verification for integration-fast surfaces).
status: open

### DW-392: S11-FC: carried forward with fresh defer-by 2026-08-04. Stale-tag preflight still requires

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **S11-FC — carried forward with fresh defer-by 2026-08-04.** Stale-tag preflight still requires either an `npx semantic-release --dry-run` cost on every release or carrying our own version-computation logic. Story 14.2 reassessed and confirmed neither option meets the cost/benefit bar without an observed collision; refreshed the re-open trigger and defer-by date.
status: done 2026-09-01
resolution: already resolved: commit 3445e66b

### DW-393: 13.7-RV1. End-to-end uses Redis `KEYS` in 3-minute polling loop.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:192
reason: - **13.7-RV1. End-to-end uses Redis `KEYS` in 3-minute polling loop.** `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:192` — `KEYS` is O(n) and prod-banned but acceptable in tests with bounded data and a non-parallel collection; the 3-min budget masks slow CI. Re-open trigger: any flake report attributing to this wait, or a need to scale the integration suite.
status: open

### DW-394: 13.7-RV2. URL-escape `tenantId`/`canary` in search query interpolation.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:107
reason: - **13.7-RV2. URL-escape `tenantId`/`canary` in search query interpolation.** `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:107` — defensive only; both values are `Guid.NewGuid().ToString("N")` hex without URL-reserved characters. Re-open trigger: generator change that introduces non-hex chars.
status: open

### DW-395: 13.7-RV3. Clean up parent temp directory in `DeleteTempDaprConfig`.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:474-487
reason: - **13.7-RV3. Clean up parent temp directory in `DeleteTempDaprConfig`.** `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:474-487` — only `config.yaml` is removed; AppHost-generated component yamls accumulate per random `daprAppId` under `%TEMP%/hexalith-memories-dapr/`. Low impact; cleanup is feasible if the AppHost-generated files are also enumerated. Re-open trigger: CI temp-space exhaustion or first complaint.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-396: 13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../AspireIngestionPipelineFixture.cs:489-501
reason: - **13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.** `tests/.../AspireIngestionPipelineFixture.cs:489-501` and `src/Hexalith.Memories.AppHost/Program.cs` — same concept, two implementations, brittle five-`..` magic count in the fixture fallback. Resolved by the AppHost-owned `RepositoryRootLocator` shared by AppHost startup and the Aspire integration fixture.
status: done 2026-09-01
resolution: already resolved: commit acfdf211

### DW-397: 13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: _bmad-output/implementation-artifacts/sprint-status.yaml
reason: - **13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.** `_bmad-output/implementation-artifacts/sprint-status.yaml` — entries accumulate per-event comment blurbs into multi-thousand-character logical lines (existing 13-2..13-7 entries all exhibit this). Project-wide pattern; coordinated convention change required. Re-open trigger: a parser/tool that fails on the long lines, or readability complaint.
status: done 2026-09-01
resolution: already resolved: commit 2af177e2

### DW-398: 13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../OllamaOidcFakeServerTests.cs
reason: - **13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.** `tests/.../OllamaOidcFakeServerTests.cs` — fake rejects missing `Content-Type`, missing `grant_type`, missing `client_id`, missing `client_secret`, and malformed bodies at runtime, but no `[Theory]+[InlineData]` enumerates each branch. Coverage gap rather than behavior gap; AC4 spirit met via wrong-path test plus runtime guards. Re-open trigger: a regression where the fake's rejection logic was weakened without tests catching it.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-399: 13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.

origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
location: tests/.../OllamaEmbeddingEndToEndTests.cs:116
reason: - **13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.** `tests/.../OllamaEmbeddingEndToEndTests.cs:116` — the rationale (raw + NL embeddings = 2 calls) is implicit. A named constant or comment would prevent brittleness if production legitimately changes the call count. Re-open trigger: assertion fails after a refactor and the cause is not immediately obvious.
status: done 2026-09-01
resolution: already resolved: commit d2510226

### DW-400: 15.3-RV6. `GenerateEmbeddingActivity._redis is not null` silent no-op when keyed Redis service is missing.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:81-91
reason: - **15.3-RV6. `GenerateEmbeddingActivity._redis is not null` silent no-op when keyed Redis service is missing.** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:81-91` — File Scope L88 wording "if the runtime marker reader can be injected cleanly" treats this guard as intentionally optional; the mandatory correctness gate is at both indexing activities. Follow-up: either make `IConnectionMultiplexer` required at this site or emit a startup warning when the keyed registration is absent. Re-open trigger: any production deployment where DI omits the `"redis"` keyed `IConnectionMultiplexer` registration but the indexing activities also become optional, or a missed-write incident attributed to this site.
status: open

### DW-401: 15.3-RV8. `WaitAsync(ct)` cancels the await but not the underlying Redis command.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:49
reason: - **15.3-RV8. `WaitAsync(ct)` cancels the await but not the underlying Redis command.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:49`, `RedisEmbeddingMigrationStore.cs:181,198-199,222-223` — repo-wide pattern; cancelling the await leaks pending Redis work but does not stop the call. Re-open trigger: a Redis connection-exhaustion or pile-up incident traced to migration marker read/write paths.
status: open

### DW-402: 15.3-RV10. `CompleteMigrationMarkerAsync` leaves stale `targetProvider/Model/Dimensions` on the active-marker key after `status=completed`.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: targetProvider/Model/Dimensions
reason: - **15.3-RV10. `CompleteMigrationMarkerAsync` leaves stale `targetProvider/Model/Dimensions` on the active-marker key after `status=completed`.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:217-223` — reader short-circuits on `status == completed` so no functional issue today; debugging hygiene only. Re-open trigger: a future code path reading the active-marker hash without checking `status` first, or an operator complaint that completed markers show contradictory target metadata.
status: open

### DW-403: 15.3-RV13. `OrdinalIgnoreCase` provider/model comparison vs case-sensitive downstream Redis hash keys.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:92-93
reason: - **15.3-RV13. `OrdinalIgnoreCase` provider/model comparison vs case-sensitive downstream Redis hash keys.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:92-93` — requires a broader audit of downstream key generation across raw/NL/migration paths to confirm whether a case-distinct write that passes the guard can still produce mixed-metadata Redis state. Re-open trigger: an incident where a tenant ends up with mixed-case provider/model metadata after migration.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-404: 15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:172-200
reason: - **15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:172-200` — a fresh non-resume start for target B silently overwrites the active marker while a B different target A migration may still be in progress. Out of story 15.3 scope. Re-open trigger: an operator-coordination incident where two migrations are launched concurrently on the same tenant.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-405: 15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:211-223
reason: - **15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:211-223` — same root cause as 15.3-RV15: completion writes `status=completed` to the active-marker key regardless of which migration is completing. Re-open trigger: same as 15.3-RV15.
status: done 2026-09-01
resolution: already resolved: commit d673a0e2

### DW-406: 15.3-RV18. Active-marker hash has no TTL; orphaned markers block tenant ingestion until manual cleanup.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:198-199
reason: - **15.3-RV18. Active-marker hash has no TTL; orphaned markers block tenant ingestion until manual cleanup.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:198-199` — spec explicitly says marker is retained until clean completion. Operator alerting / manual-clearance command is a follow-up. Re-open trigger: an operator escalation where a crashed migration left a tenant blocked with no automated alert.
status: open

### DW-407: 15.3-RV22. `13.6-RV2` swept in based on "Story 15.3 touched the file substantively, gained copyright header" rationale.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md:210-219
reason: - **15.3-RV22. `13.6-RV2` swept in based on "Story 15.3 touched the file substantively, gained copyright header" rationale.** `_bmad-output/implementation-artifacts/deferred-work.md:210-219` — borderline-compliant with the spec's "records why they became in scope" clause; rationale could be sharper about *why* file-touch resolves the deferred risk. Re-open trigger: another `13.6-RV*` ID is closed with the same weak rationale and triggers a governance question.
status: open

### DW-408: 15.3-RV24. Story status moved `ready-for-dev` → `review` without an `in-progress` step.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: n/a
reason: - **15.3-RV24. Story status moved `ready-for-dev` → `review` without an `in-progress` step.** `sprint-status.yaml` — process flag only, not code; create-story → dev-story workflow could record an explicit `in-progress` transition. Re-open trigger: any tooling that breaks on missing `in-progress` history.
status: open

### DW-409: 15.3-RV25. Operator-docs downtime statement could be sharper about per-tenant retry disruption.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: docs/operations/embedding-providers.md
reason: - **15.3-RV25. Operator-docs downtime statement could be sharper about per-tenant retry disruption.** `docs/operations/embedding-providers.md` — "Tenant-specific ingestion downtime is not required" is correct but some readers will interpret operator-visible per-tenant retry as "effective downtime"; phrasing could be tightened. Re-open trigger: operator confusion or escalation citing the downtime statement.
status: done 2026-09-01
resolution: already resolved: docs/operations/embedding-providers.md:244

### DW-410: 15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:64-65
reason: - **15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:64-65` — current path stores `int` via `HashEntry(string, int)` overload which is invariant; a future refactor to a string overload could silently regress to locale-sensitive parsing → fail-open. Re-open trigger: any refactor of the marker write path away from the `int`-typed `HashEntry` overload.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:73

### DW-411: 15.3-RV27. Stale per-target marker can resume against drifted state.

origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:179-187
reason: - **15.3-RV27. Stale per-target marker can resume against drifted state.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:179-187` — `--resume` only checks per-target key existence; does not verify active marker still references same target. Overlaps 15.3-RV15/16. Re-open trigger: same as 15.3-RV15.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:828

### DW-412: 1.1-RR1. Process-wide environment mutation when wiring DAPR API tokens.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **1.1-RR1. Process-wide environment mutation when wiring DAPR API tokens.** `ApplyProcessEnvironmentTokens` sets `APP_API_TOKEN`/`DAPR_API_TOKEN` on the AppHost process so spawned daprd sidecars inherit them, but the variables persist for every child process the AppHost spawns afterwards. - ID: 1.1-RR1 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: A child-process leak surfaces in audit (token visible in unrelated process env), or CommunityToolkit.Aspire.Hosting.Dapr exposes a per-sidecar env API that replaces the global mutation. - Rationale: CommunityToolkit.Aspire.Hosting.Dapr 9.7 has no sidecar-specific env-builder API; the documented workaround is process env inheritance, and the AppHost only runs in development/CI/staging where the surface area is tightly scoped.
status: open

### DW-413: 1.1-RR2. `DAPR_API_TOKEN_MODE` default silently disables token authentication.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **1.1-RR2. `DAPR_API_TOKEN_MODE` default silently disables token authentication.** A missing or typo'd `DAPR_API_TOKEN_MODE` value yields `(null, null)` from `ResolveDaprApiTokens` and skips both sidecar and application token wiring with no log entry. - ID: 1.1-RR2 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: A production incident traces a missing-token deployment back to a `DAPR_API_TOKEN_MODE` typo or omission. - Rationale: Default-disabled is the intentional posture for local dev and the Aspire integration-test fixture; production runs ship `DAPR_API_TOKEN_MODE=enabled` via secret manifest and never go through this branch silently.
status: open

### DW-414: 1.1-RR3. Obsolete `WithReference` (CS0618) suppression hides upstream Aspire migration.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs; Directory.Packages.props
reason: - **1.1-RR3. Obsolete `WithReference` (CS0618) suppression hides upstream Aspire migration.** `#pragma warning disable CS0618` wraps the project-level component references; Aspire 14.x will remove the API. - ID: 1.1-RR3 - Status: carried-forward - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs; Directory.Packages.props - Re-open trigger: Aspire 14.x package bump turns the warning into an error, or CommunityToolkit.Aspire.Hosting.Dapr releases a non-obsolete component-binding API. - Rationale: CommunityToolkit.Aspire.Hosting.Dapr 9.7 still reads project-level component references; removing the suppression now would break sidecar wiring with no upstream replacement. Owner: the AppHost/release maintainer carries the CS0618 suppression and removes it when the trigger fires (the Aspire 14.x bump, or a non-obsolete CommunityToolkit.Aspire.Hosting.Dapr component-binding API). Re-confirmed carried-forward by Story 19.1 (2026-06-30).
status: open

### DW-415: 1.1-RR4. `RepositoryRootLocator.Resolve()` failure is unhandled in AppHost helpers.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **1.1-RR4. `RepositoryRootLocator.Resolve()` failure is unhandled in AppHost helpers.** `EnsureTestDataRoot`, `EnsureSecretsFile`, `ResolveDaprConfigPath`, and `ResolveRedisConfigPath` propagate raw `InvalidOperationException` if the AppHost runs from outside a recognizable repo layout. - ID: 1.1-RR4 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: A user runs the AppHost from a packaged distribution or detached workspace and files an issue about the cryptic "repository root not found" error. - Rationale: AppHost is dev/CI-side only and always launched from within the repo today; the locator's own exception message names the lookup keys and is debuggable.
status: open

### DW-416: 1.1-RR5. `test-data/README.md` write race between parallel AppHosts.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **1.1-RR5. `test-data/README.md` write race between parallel AppHosts.** `EnsureTestDataRoot` uses non-atomic `File.Exists` then `File.WriteAllText`; two simultaneous AppHost runs can collide on the README. - ID: 1.1-RR5 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: A developer reports a transient `IOException: file in use` on AppHost startup, or CI begins running multiple parallel AppHosts in a single sandbox. - Rationale: The README is created once per workspace lifetime; subsequent AppHost runs short-circuit on `File.Exists`. Collision window is sub-millisecond and only on the first ever run.
status: open

### DW-417: 1.1-RR6. `AddJsonConsole` plus OTEL logger create dual log sinks.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.ServiceDefaults/Extensions.cs
reason: - **1.1-RR6. `AddJsonConsole` plus OTEL logger create dual log sinks.** ServiceDefaults registers OpenTelemetry logging (with scopes + formatted message) and `AddJsonConsole` simultaneously, producing two log records per emission when the OTLP exporter is also active. - ID: 1.1-RR6 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.ServiceDefaults/Extensions.cs - Re-open trigger: A log-volume budget overrun in production is traced to dual-sink output, or downstream log shipping fails because of duplicated records. - Rationale: AC #3 explicitly calls for structured JSON logging via ServiceDefaults; the JSON console is the local-dev/Aspire dashboard surface, the OTEL exporter is the production sink. Both running side-by-side is the intended design for visibility parity.
status: open

### DW-418: 1.1-RR7. `ResolveAllocatedEndpoint` `Single()` failure lacks context.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **1.1-RR7. `ResolveAllocatedEndpoint` `Single()` failure lacks context.** A missing or duplicated endpoint name surfaces as a bare `InvalidOperationException` with no message naming the resource or endpoint. - ID: 1.1-RR7 - Status: accepted - Source story: 1-1-project-scaffolding-and-single-command-boot - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: An Aspire upgrade renames endpoint keys and the unmatched lookup produces a triage ticket that costs more than the wrapping cost would have saved. - Rationale: AppHost-internal helper used for two known endpoint names (`redis`, `falkordb`); the surrounding `OnResourceReady`/`BeforeResourceStartedEvent` callbacks would themselves fail in a debuggable way if the endpoint contract drifts.
status: open

### DW-419: 15.6-CR1. Tight Redis PING reconnect loop without exponential backoff.

origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **15.6-CR1. Tight Redis PING reconnect loop without exponential backoff.** `WaitForRedisPingAsync` reconnects every 500 ms for up to 2 minutes against a Redis that may already be struggling; no backoff, no jitter. - ID: 15.6-CR1 - Status: accepted - Source story: 15-6-scaffolding-hardening-sweep - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: A developer reports AppHost log spam or a Redis backpressure incident traces back to the readiness probe loop. - Rationale: Cosmetic vs functional under current single-developer / CI load profile; the 500 ms cadence is below SE.Redis's own retry intervals and never has more than one in-flight connection.
status: open

### DW-420: 15.6-CR2. Submodule guard `.git`-existence check does not detect partially-cloned submodules.

origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
location: Directory.Build.props
reason: - **15.6-CR2. Submodule guard `.git`-existence check does not detect partially-cloned submodules.** `Exists('{path}/.git')` passes as long as a `.git` file or directory is present at that path; it does not verify `HEAD` validity or that `git submodule update --init` actually populated content. - ID: 15.6-CR2 - Status: accepted - Source story: 15-6-scaffolding-hardening-sweep - Target artifact: Directory.Build.props - Re-open trigger: A developer reports a fresh clone that "passes the submodule guard" but actually has missing content, traced to a network failure mid `git submodule update`. - Rationale: Story 15.6 only expanded the *count* of checked submodules (Story 1.1's pre-existing guard pattern); tightening the predicate to verify `HEAD` validity is a separate, broader scope and would require shelling out to git.
status: open

### DW-421: 15.6-CR3. `File.WriteAllText` on DAPR component files is not atomic.

origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **15.6-CR3. `File.WriteAllText` on DAPR component files is not atomic.** AppHost writes component YAMLs via `File.WriteAllText` which truncates-then-writes; a hot-reload watcher could read a partial file. - ID: 15.6-CR3 - Status: accepted - Source story: 15-6-scaffolding-hardening-sweep - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: `DAPR_COMPONENT_RELOAD_INTERVAL` gets enabled for local dev, or a developer reports a transient "invalid YAML" sidecar error during AppHost restart. - Rationale: Per-PID directory isolation means no other daprd process watches the same path; the local daprd does not have hot-reload enabled by default. Switching to write-temp-then-rename is a standalone hardening, not a Story 15.6 regression.
status: open

### DW-422: 15.6-CR4: resolved. `ResolveAllocatedEndpoint` is no longer called before awaiting the rewrite TCS in `BeforeResourceStartedEvent`. The endpoint lookup now occurs after `WaitForRedisComponentRewriteAsync`, so an early sidecar-start event gives Redis allocation and component-file rewrite a chance to complete before the lookup runs.

origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs
reason: - **15.6-CR4 - resolved.** `ResolveAllocatedEndpoint` is no longer called before awaiting the rewrite TCS in `BeforeResourceStartedEvent`. The endpoint lookup now occurs after `WaitForRedisComponentRewriteAsync`, so an early sidecar-start event gives Redis allocation and component-file rewrite a chance to complete before the lookup runs. - ID: 15.6-CR4 - Status: resolved - Source story: deferred-work-implementation-2026-05-19 - Target artifact: src/Hexalith.Memories.AppHost/Program.cs - Re-open trigger: `ResolveAllocatedEndpoint(redis.Resource, "redis")` moves back above the rewrite wait, an Aspire upgrade changes the `BeforeResourceStartedEvent`-vs-allocation contract, or the `AppHostComponentFileOrderingTests` behavioral guard reproduces an early InvalidOperationException. - Evidence: `src/Hexalith.Memories.AppHost/Program.cs` snapshots the rewrite task, awaits `WaitForRedisComponentRewriteAsync(...)`, then resolves the Redis endpoint before `WaitForRedisPingAsync(...)`; `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs` carries the Docker/Aspire behavioral guard for the ordering invariant.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.AppHost/Program.cs:191

### DW-423: 2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.** Currently `internal static class` in `Hexalith.Memories.Contracts.Tests`; only consumed by `EvidencePacketSerializationTests`. Spec Task 5 demanded cross-surface fixture reuse (CLI, MCP, server tests). Rationale: requires moving fixtures to a shared test helper assembly (cross-cutting refactor) and re-keying CLI/MCP/server tests; paired with 2.7-CR2/CR3/CR4/CR5.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.TestHelpers/EvidencePackets/EvidencePacketCanonicalFixtures.cs:18

### DW-424: 2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.** `EvidencePacketCliOutputTests.cs` has a single hybrid happy-path `[Fact]`. Spec Task 5 demanded full state coverage at the CLI surface. Rationale: significant test-scope expansion; depends on 2.7-CR1 shared fixtures.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:28

### DW-425: 2.7-CR3. No table-driven sanitization tests across the spec'd categories

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR3. No table-driven sanitization tests across the spec'd categories** (unauthorized, all-backend failure, partial degradation, token-budget compression, server diagnostics, MCP error mapping). Rationale: paired with 2.7-CR1 shared fixtures.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketSanitizationTests.cs:48

### DW-426: 2.7-CR4. No tenant/case negative isolation fixtures.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR4. No tenant/case negative isolation fixtures.** Tests use only `tenant-a`/`case-a` happy-paths. Rationale: paired with decision on 2.7-CR9 (scope-consistency policy).
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketIsolationTests.cs:20

### DW-427: 2.7-CR5. No server-side `EvidencePacketMapper` tests

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR5. No server-side `EvidencePacketMapper` tests** (tenant/case scope, empty results, partial backend degradation, all-backend/unauthorized diagnostics, token-budget omitted metadata). Rationale: needs new test class scaffolding in `Hexalith.Memories.Server.Tests`.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs:21

### DW-428: 2.7-CR6. MCP error path uses `UnknownScope()` for known-tenant errors.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR6. MCP error path uses `UnknownScope()` for known-tenant errors.** `McpErrorMapper.cs:61, 86, 105, 132` — only the forbidden branch passes real scope. Rationale: requires plumbing `requestedTenantId`/`@case` through `Map`/`MapGeneric`/`MapValidation` from `SearchMemoryTool`; touches every MCP tool that hits the error mapper.
status: open

### DW-429: 2.7-CR7. `MapOmittedReason` falls to default `None` for future enum values

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR7. `MapOmittedReason` falls to default `None` for future enum values** (Density, Redaction, Policy, Authorization, TrueAbsence). Rationale: lower-level `OmittedReason` enum doesn't expose those today; reopens when the server starts emitting them.
status: open

### DW-430: 2.7-CR8. SHA-256 expansion handle truncated to 16 hex (64 bits) + `|`-delimited material allows injection collisions.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR8. SHA-256 expansion handle truncated to 16 hex (64 bits) + `|`-delimited material allows injection collisions.** `EvidencePacketMapper.cs:508-513`. Rationale: needs handle-format decision (length, delimiter, length-prefix vs delimiter).
status: open
decision: 2026-09-01 Versioned length-prefixed handle — Emit a v2 length-prefixed handle with at least 128 hash bits and accept v1 during migration.
decision: 2026-09-01 Versioned length-prefixed handle — Emit a v2 length-prefixed handle with at least 128 hash bits and accept v1 during migration.

### DW-431: 2.7-CR9. `source.CaseId` copied verbatim from upstream with no scope-consistency check

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR9. `source.CaseId` copied verbatim from upstream with no scope-consistency check** (decision-needed). Rationale: needs design call between trust-upstream / overwrite / skip / throw.
status: open

### DW-432: 2.7-CR10. CLI does not emit `EvidencePacket` on error responses

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR10. CLI does not emit `EvidencePacket` on error responses** (decision-needed). Rationale: changes CLI JSON error envelope shape (`CliErrorWriter.WriteForCommand`); needs explicit decision about CLI error envelope schema, possibly affecting ADR-7.3-002.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:238

### DW-433: 2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal** (decision-needed). Rationale: needs a server-side change to expose authorization-driven emptiness; paired with 2.7-CR15.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Server.Tests/Search/EvidencePacketServerMappingTests.cs:165

### DW-434: 2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.** Rationale: needs precedence design between strength and state.
status: done 2026-09-01
decision: 2026-09-01 Keep semantics orthogonal — State describes execution and evidenceStrength describes quality; document that split.
resolution: closed by human decision: State describes execution and evidenceStrength describes quality; document that split.
decision: 2026-09-01 Keep semantics orthogonal — State describes execution and evidenceStrength describes quality; document that split.

### DW-435: 2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".** Rationale: lower-level `ScoredResult.Score` schema would need to become nullable.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs:61

### DW-436: 2.7-CR14. Inconsistent `permissionsContext` values across surfaces

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR14. Inconsistent `permissionsContext` values across surfaces** (`tenant`/`tenant-case`/`mcp-auth`/`mcp-error`). Rationale: needs a single source-of-truth constant list.
status: open

### DW-437: 2.7-CR15. State precedence does not model `Redaction`/`Policy` states

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR15. State precedence does not model `Redaction`/`Policy` states** (Party-Mode Hardening). Rationale: no upstream signal exists today; paired with 2.7-CR11.
status: open

### DW-438: 2.7-CR16. `Combined` omission reason on degraded result strips token-budget hint from recovery.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR16. `Combined` omission reason on degraded result strips token-budget hint from recovery.** Rationale: paired with 2.7-CR11/CR12 precedence redesign.
status: open
decision: 2026-09-01 Emit additive recoveries — Keep backend recovery primary and add deterministic IncreaseTokenBudget when Combined includes budget omission.
decision: 2026-09-01 Emit additive recoveries — Keep backend recovery primary and add deterministic IncreaseTokenBudget when Combined includes budget omission.

### DW-439: 2.7-CR17. `McpErrorPayload` not registered in source-gen `MemoriesJsonContext`

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR17. `McpErrorPayload` not registered in source-gen `MemoriesJsonContext`** (AOT-only risk; works today via reflection fallback). Rationale: needs Mcp-side source-gen context.
status: open

### DW-440: 2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketCanonicalParityTests.cs:21

### DW-441: 2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records** instead of an envelope wrapper (design smell). Rationale: revert would require envelope wrapping at every consumer; mitigated by `[JsonIgnore(WhenWritingNull)]`.
status: done 2026-09-01
decision: 2026-09-01 Ratify nullable property — The additive property is compatible and protected by serialization tests.
resolution: closed by human decision: The additive property is compatible and protected by serialization tests.
decision: 2026-09-01 Ratify nullable property — The additive property is compatible and protected by serialization tests.

### DW-442: 2.7-CR20. `EvidencePacketResultSummary.Query` echoes raw caller query verbatim

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR20. `EvidencePacketResultSummary.Query` echoes raw caller query verbatim** (defense-in-depth length cap / sanitize). Rationale: caller-supplied, not a leak.
status: open

### DW-443: 2.7-CR21. Whitespace-only `caseId` inconsistency

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR21. Whitespace-only `caseId` inconsistency** (`permissionsContext: "tenant"` while `scope.caseId: " "`). Rationale: input-validation polish.
status: open

### DW-444: 2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).** Rationale: borderline scope-shape oracle; minor.
status: open

### DW-445: 2.7-CR23. `HybridSearchResult.Results` not null-guarded in mapper.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR23. `HybridSearchResult.Results` not null-guarded in mapper.** Rationale: contract guarantees non-null via `required`; defensive-only.
status: open

### DW-446: 2.7-CR24. `FromSearchResult` passes `null` for `AllEnabledAxesUnavailable` vs hybrid passing actual value.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR24. `FromSearchResult` passes `null` for `AllEnabledAxesUnavailable` vs hybrid passing actual value.** Rationale: cosmetic; semantically correct since single-axis has no multi-axis concept.
status: open

### DW-447: 2.7-CR25. `McpErrorMapper.MapAuthorization` forbidden message echoes `requestedTenantId`.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR25. `McpErrorMapper.MapAuthorization` forbidden message echoes `requestedTenantId`.** Rationale: caller-supplied input echoed back, not a confirmation of alternate tenant existence.
status: open

### DW-448: 2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Contracts.Tests/V1/EvidencePacketCanonicalParityTests.cs:21

### DW-449: 2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.** Rationale: paired with 2.7-CR2 single-axis CLI coverage.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Cli.Tests/Cli/EvidencePacketCliOutputTests.cs:390

### DW-450: 16.1-CR1. Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*`

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140
reason: - **16.1-CR1. Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*`** (`src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140`). Rationale: operator-error edge; promote to wildcard is consistent with empty-string semantics documented in `ProjectionBinding.SupportedEventTypePatterns` XML doc. Sweep later if any host reports surprise.
status: open

### DW-451: 16.1-CR2. Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR2. Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split** (`ProjectionBindingMatcher.cs:155-159, 183-187`). Rationale: not produced by current callers; defensive normalization deferred to a normalization sweep.
status: open

### DW-452: 16.1-CR3. Embedded `\` in event names not normalized to `.` or `/`

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR3. Embedded `\` in event names not normalized to `.` or `/`** (`ProjectionBindingMatcher.cs:135`). Rationale: serializers in this stack emit `.`-separated event types; backslash variant is theoretical.
status: open

### DW-453: 16.1-CR4. Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR4. Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes** (`ProjectionBindingMatcher.cs:132, 167`). Rationale: current tenants/aggregates are ASCII; revisit when non-ASCII tenant ids are introduced.
status: open

### DW-454: 16.1-CR5. Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: tenant/source//
reason: - **16.1-CR5. Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key** (`ProjectionBindingMatcher.cs:161`). Rationale: cosmetic — comparison key is internal, only surfaces in Subject if a bare `V2` event reaches the detector; unlikely.
status: open

### DW-455: 16.1-CR6. Tenant-leakage assertion absent on structured log/telemetry payload

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs
reason: - **16.1-CR6. Tenant-leakage assertion absent on structured log/telemetry payload** (`tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`). Rationale: currently no foreign-tenant fields are emitted on the warning; defensive assertion to add when telemetry shape expands.
status: open

### DW-456: 16.1-CR7. Multi-slash collapse (`while (... "//")`) has no direct test

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR7. Multi-slash collapse (`while (... "//")`) has no direct test** (`ProjectionBindingMatcher.cs:127-130`). Rationale: covered transitively by slash-normalization tests; explicit regression test is low priority.
status: open

### DW-457: 16.1-CR8. Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR8. Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring** (`ProjectionBindingMatcher.cs:108-111`). Rationale: add a targeted test when wildcard semantics is widened.
status: open

### DW-458: 17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13
reason: - **17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern** (`src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13` and all display copy across the RCL). Rationale: broader localization work spans the whole new RCL, not just Evidence Cockpit; needs a Memories resource bundle decision before retrofitting.
status: done 2026-09-01
resolution: already resolved: commit 16958753

### DW-459: 17.1-CR2. `EvidencePacketScope.PermissionsContext` not surfaced anywhere in `MemoriesScopeHeader` or mapping

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor
reason: - **17.1-CR2. `EvidencePacketScope.PermissionsContext` not surfaced anywhere in `MemoriesScopeHeader` or mapping** (`src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`, `EvidencePacketViewMapping.cs`). Rationale: needs a UX call on where/how to display the machine-readable permission context (chip? expandable detail?); not blocking AC1.
status: open

### DW-460: 17.1-CR3. `EvidencePacketOmittedDetails` body fields (`OmittedCount/FieldNames/DetailGroups/ExpansionHandles`) silently dropped beyond the binary token-budget indicator

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: OmittedCount/FieldNames/DetailGroups/ExpansionHandles
reason: - **17.1-CR3. `EvidencePacketOmittedDetails` body fields (`OmittedCount/FieldNames/DetailGroups/ExpansionHandles`) silently dropped beyond the binary token-budget indicator** (`MemoriesTrustStrip.razor`, `MemoriesEvidenceCockpit.razor`). Rationale: requires an "expand omitted details" UX (drawer? inline disclosure?) that is itself a separate story.
status: open

### DW-461: 17.1-CR4. `EvidencePacketSource.AnnotationsCount/CaseId/CaseName` not rendered in Source Citation Stack

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: EvidencePacketSource.AnnotationsCount/CaseId/CaseName
reason: - **17.1-CR4. `EvidencePacketSource.AnnotationsCount/CaseId/CaseName` not rendered in Source Citation Stack** (`MemoriesSourceCitationStack.razor`). Rationale: deemed not load-bearing for AC3 inspection workflow; revisit when annotations or cross-case visibility surfaces are designed.
status: open

### DW-462: 17.1-CR5. `EvidencePacketResultSummary.TotalCount/ReturnedCount/HasIndexedMemoryUnits` not surfaced

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: EvidencePacketResultSummary.TotalCount/ReturnedCount/HasIndexedMemoryUnits
reason: - **17.1-CR5. `EvidencePacketResultSummary.TotalCount/ReturnedCount/HasIndexedMemoryUnits` not surfaced** (`MemoriesEvidenceCockpit.razor:20-31`). Rationale: distinguishes empty-tenant from empty-result, but not strictly required by AC1; adds value when an Empty fixture lands.
status: open

### DW-463: 17.1-CR6. `EvidencePacketEvidence.Degraded` / `AllEnabledAxesUnavailable` flags not fed into Trust Strip

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR6. `EvidencePacketEvidence.Degraded` / `AllEnabledAxesUnavailable` flags not fed into Trust Strip** (`MemoriesTrustStrip.razor`). Rationale: overlapping signal with `State`; defer until the precedence ladder is implemented and these flags can layer cleanly.
status: open

### DW-464: 17.1-CR7. Task 6 accessibility checkboxes marked `[x]` despite no automated forced-colors / focus-return / touch-target / no-text-overlap check

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: _bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md:90-95
reason: - **17.1-CR7. Task 6 accessibility checkboxes marked `[x]` despite no automated forced-colors / focus-return / touch-target / no-text-overlap check** (`_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md:90-95`). Rationale: Completion Notes correctly call out Playwright/axe deferred because no runnable web host was added in this RCL-only slice; refile a hardening story when a host (e.g., 17.5) lands.
status: open

### DW-465: 17.1-CR8. `aria-label="Inspect source N"` exposes raw Rank including 0

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR8. `aria-label="Inspect source N"` exposes raw Rank including 0** (`MemoriesSourceCitationStack.razor:34`). Rationale: only relevant if Inspect button is reinstated with command wiring; the dead-button removal patch closes this path.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:20

### DW-466: 17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - **17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: Task 5 mandates these tests, but they are vacuous until command primitives exist; couples to dead-button removal patch.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17SanitizationCanaryTests.cs:123

### DW-467: 17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - **17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: useful when actions trigger real state changes; trivial today since all states are rendered in isolation.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitRecoveryTransitionTests.cs:29

### DW-468: 17.1-CR11. `<article>` + nested `<section aria-label>` creates a verbose landmark list under assistive tech

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR11. `<article>` + nested `<section aria-label>` creates a verbose landmark list under assistive tech** (`MemoriesEvidenceCockpit.razor:1-2` and children). Rationale: a11y refinement after primary leakage / precedence findings settle; not blocking AC2.
status: open

### DW-469: 17.1-CR12. Source citation order test asserts `data-source-rank` attribute values rather than DOM iteration order

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR12. Source citation order test asserts `data-source-rank` attribute values rather than DOM iteration order** (`EvidenceCockpitTests.cs:104-106`). Rationale: acceptable proxy with the current rank-stable contract; tighten when contract permits rank duplication or absent ranks.
status: open

### DW-470: 17.1-CR13. `SourceCountLabel` does not handle negative or `int.MaxValue` counts

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR13. `SourceCountLabel` does not handle negative or `int.MaxValue` counts** (`EvidenceDisplay.cs:14-15`). Rationale: contract precludes negative counts; defensive-only.
status: open

### DW-471: 17.1-CR14. `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText`

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR14. `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText`** (`MemoriesSourceCitationStack.razor:17`, `MemoriesRetrievalAxisBreakdown.razor:17`). Rationale: enum-like strings have controlled vocabulary; revisit if contract loosens to free-form strings.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor:26

### DW-472: 17.1-CR15. Stale state never tested by fixture

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR15. Stale state never tested by fixture** — covered by the broader "5 of 8 states untested" patch above; standalone deferred.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Web.Specimens/Epic17EvidencePacketFixtures.cs:216

### DW-473: 17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout** (`MemoriesEvidenceCockpit.razor.css`). Rationale: minor compliance gap with Fluent UI primitive preference; wrapping behavior itself is correct.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceRemediationTests.cs:57

### DW-474: 17.1-CR17. `Sources[].SourceUri` rendered without trust-mark badging (external URL vs local memory reference)

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR17. `Sources[].SourceUri` rendered without trust-mark badging (external URL vs local memory reference)** (`MemoriesSourceCitationStack.razor`). Rationale: not in AC3; needs a UX call on visual trust marks before implementing.
status: open

### DW-475: 17.1-CR18. Graph path summary uses raw `<dl>/<dt>/<dd>` rather than `FluentDescriptionList`

origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
location: n/a
reason: - **17.1-CR18. Graph path summary uses raw `<dl>/<dt>/<dd>` rather than `FluentDescriptionList`** (`MemoriesGraphPathSummary.razor`). Rationale: FrontComposer primitive preference; current markup is semantically correct.
status: open

### DW-476: ID: 17.1-CR1

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`; `src/Hexalith.Memories.Web/Resources/*
reason: - ID: 17.1-CR1 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`; `src/Hexalith.Memories.Web/Resources/*` - Re-open trigger: any new Evidence Cockpit visible or assistive copy bypasses `EvidenceResourceKeys` and the EN/FR resource bundle. - Rationale: Story 25.7 routed cockpit headings, banners, enum labels, counts, freshness, timestamps, scores, captions, fallbacks, and accessible names through `IStringLocalizer<MemoriesWebResources>` with EN/FR parity tests. - Evidence: `EvidenceResourceKeys.cs`, localized Evidence components/helpers, and `EvidenceCockpitTests.Localization_EveryEvidenceKeyResolvesInEnglishAndFrench` plus the French multi-state rendering test.
status: done 2026-09-01
resolution: already resolved: commit 16958753

### DW-477: ID: 17.1-CR2

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`; `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketViewMapping.cs
reason: - ID: 17.1-CR2 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`; `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketViewMapping.cs` - Re-open trigger: before the web surface claims to render the full Evidence Packet scope contract or adds a permissions-context chip/detail surface. - Rationale: `permissionsContext` placement is a UX information-architecture decision. It should be implemented with the other metadata surfacing work so scope, permission, and isolation signals stay coherent.
status: open

### DW-478: ID: 17.1-CR3

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Omitted Details Expansion UX); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor
reason: - ID: 17.1-CR3 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Omitted Details Expansion UX); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor` - Re-open trigger: before omitted detail groups or expansion handles are exposed in any web cockpit, lens, copy/export, or command surface. - Rationale: Omitted detail expansion needs a deliberate disclosure pattern with token-budget, authorization, and backend-degradation semantics; adding ad hoc fields to the Trust Strip would make the state grammar harder to reason about.
status: open

### DW-479: ID: 17.1-CR4

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor
reason: - ID: 17.1-CR4 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor` - Re-open trigger: when annotations, cross-case source visibility, or case-name inspection becomes part of a selected web workflow. - Rationale: Source annotation and case metadata are valuable only when their placement is designed with trust marks and cross-case visibility; keep them in the metadata surfacing story rather than broadening Story 25.7.
status: open

### DW-480: ID: 17.1-CR5

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor
reason: - ID: 17.1-CR5 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor` - Re-open trigger: before the web surface ships an empty-tenant, empty-result, or indexed-memory-unit distinction to users. - Rationale: Result-summary counts affect user interpretation of empty and partial states. They belong with the metadata surfacing story so empty-result language, counts, and indexed-unit signals are tested together.
status: open

### DW-481: ID: 17.1-CR6

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor
reason: - ID: 17.1-CR6 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor` - Re-open trigger: before the Trust Strip adds a precedence ladder for `Degraded` or `AllEnabledAxesUnavailable`, or any new surface claims all-axis availability status. - Rationale: These flags overlap with `State` and recovery semantics. They should be layered only after UX approves precedence and display rules.
status: open

### DW-482: ID: 17.1-CR7

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Validation/*
reason: - ID: 17.1-CR7 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Validation/*` - Re-open trigger: before any product-route accessibility claim, release note, or stakeholder acceptance says the web surface is validated beyond component-specimen bUnit evidence. - Rationale: Forced-colors, focus return, touch target, and no-overlap checks need a runnable host or specimen app. Component tests alone cannot close browser/AT validation.
status: open

### DW-483: ID: 17.1-CR8

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor
reason: - ID: 17.1-CR8 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor` - Re-open trigger: before an Inspect Source command/button is reintroduced or wired to command-palette activation. - Rationale: The dead-button removal closed the immediate path, but command wiring can reintroduce rank-label leakage. Keep the fix coupled to the command-surface story.
status: open

### DW-484: ID: 17.1-CR9

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - ID: 17.1-CR9 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - Re-open trigger: before copy, export, or MCP-inspect commands are exposed from the web cockpit or command palette. - Rationale: Redaction parity tests are vacuous until the relevant command primitives exist. They become mandatory acceptance evidence for the command-surface story.
status: open

### DW-485: ID: 17.1-CR10

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - ID: 17.1-CR10 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - Re-open trigger: when a runnable host/specimen app exists or web actions introduce real loading-to-complete, complete-to-degraded, or recovery transition paths. - Rationale: Transition-state accessibility is meaningful only when state changes happen in a running interaction surface; current isolated fixture rendering does not exercise those paths.
status: open

### DW-486: ID: 17.1-CR11

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor
reason: - ID: 17.1-CR11 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor` - Re-open trigger: during the first screen-reader pass or host-level landmark/heading audit for the web cockpit. - Rationale: Landmark verbosity is a real browser/assistive-technology concern. It should be fixed with screen-reader evidence rather than guessed from host-less markup.
status: open

### DW-487: ID: 17.1-CR12

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - ID: 17.1-CR12 - Status: accepted - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - Re-open trigger: the Evidence Packet source-ranking contract allows duplicate ranks, absent ranks, or a separate user-visible order that can diverge from `data-source-rank`. - Rationale: The current rank-stable contract makes the attribute assertion an adequate proxy. Tightening to DOM-iteration assertions is defensive polish until the ordering contract changes.
status: open

### DW-488: ID: 17.1-CR13

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs
reason: - ID: 17.1-CR13 - Status: accepted - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs` - Re-open trigger: source counts become externally supplied, nullable, unbounded, or otherwise no longer contract-controlled non-negative integers. - Rationale: The contract precludes negative source counts. Adding defensive negative or `int.MaxValue` labels now would spend scope on an unreachable state.
status: open

### DW-489: ID: 17.1-CR14

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor
reason: - ID: 17.1-CR14 - Status: accepted - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor` - Re-open trigger: `SourceType` or axis values become provider-authored, user-authored, localized free text, or otherwise leave the controlled-vocabulary contract. - Rationale: Current values are enum-like controlled contract terms. Applying `SafeText` everywhere is acceptable future hardening, but not needed until the contract loosens.
status: open

### DW-490: ID: 17.1-CR15

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`; `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs
reason: - ID: 17.1-CR15 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`; `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs` - Re-open trigger: stale-state fixture coverage is removed or a new stale-state rendering path bypasses the existing fixture and per-state assertions. - Evidence: Story 17.1 review patch added the `Stale` fixture as part of the broader empty/stale/degraded/partial/weak fixture expansion, and current tests still reference `EvidencePacketFixtures.StalePacket()` in Evidence Cockpit, recovery, filters, lenses, and responsive/accessibility suites.
status: done 2026-09-01
resolution: already resolved: tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs:432-443 exercises the current EvidencePacketFixtures.StalePacket degraded-state fixture.

### DW-491: ID: 17.1-CR16

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css
reason: - ID: 17.1-CR16 - Status: resolved - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css` - Re-open trigger: Trust Strip wrapping is moved out of `FluentStack Wrap="true"` into hand-authored layout CSS. - Rationale: Story 25.7 keeps wrapping in the Fluent V5 stack primitive and verifies that the cockpit stylesheet contains no raw `flex-wrap` declaration. - Evidence: `MemoriesTrustStrip.razor` and `Epic17ConformanceRemediationTests.TrustStrip_UsesFluentStackWrappingInsteadOfRawFlexWrap`.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor:8-11 renders the trust strip in a wrapping FluentStack.

### DW-492: ID: 17.1-CR17

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor
reason: - ID: 17.1-CR17 - Status: carried-forward - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor` - Re-open trigger: before source trust marks, external/local source distinction, or cross-case source cues are productized. - Rationale: Trust-mark badging needs a UX decision alongside source metadata and case visibility. Implementing it alone would risk adding a visual security claim without a shared legend.
status: open

### DW-493: ID: 17.1-CR18

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor
reason: - ID: 17.1-CR18 - Status: accepted - Source story: 17-1-evidence-cockpit-and-trust-components - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor` - Re-open trigger: the pinned Fluent UI package or FrontComposer adds a description-list primitive. - Rationale: Story 25.7 verified that the pinned Fluent V5 assembly has no `FluentDescriptionList`; the semantic `<dl>/<dt>/<dd>` fallback remains explicitly allowlisted under owner Story 25.7. - Evidence: `Epic17ConformanceAllowlist` description-list entries and `Epic17ConformanceRemediationTests.GraphSummary_PinnedFluentPackageHasNoDescriptionListPrimitive`.
status: open

### DW-494: Non-cockpit grid and lens callers still consume locale-insensitive `EvidenceDisplay` overloads.

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md` summary: Non-cockpit grid and lens callers still consume locale-insensitive `EvidenceDisplay` overloads. evidence: The reviewed cockpit and its Evidence child components use localized overloads, but `Components/Grid` and `Components/Lenses` retain pre-existing calls to the invariant enum, timestamp, freshness, and score formatters; localizing those mapper-driven surfaces requires a separate cross-RCL design rather than widening Story 25.7.
status: open

### DW-495: ID: 17-WEB-AD1-COMMAND-PALETTE-SCOPE

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Interaction/*
reason: - ID: 17-WEB-AD1-COMMAND-PALETTE-SCOPE - Status: carried-forward - Source story: epic-17-retro-2026-06-24 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Interaction/*` - Re-open trigger: before the Memories web command palette becomes user-facing or a web action needs global, page-local, or role-density-scoped discovery. - Rationale: Command-palette scope changes user reachability, disabled reasons, tenant/case reset behavior, and redaction obligations. It is bounded with the command-surface story instead of patched inside a display component.
status: open

### DW-496: ID: 17-WEB-AD2-REFRESH-PERSISTENCE

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (future web story: Tenant-Scoped Refresh Persistence Policy); `src/Hexalith.Memories.Web/Components/Interaction/*
reason: - ID: 17-WEB-AD2-REFRESH-PERSISTENCE - Status: carried-forward - Source story: epic-17-retro-2026-06-24 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Tenant-Scoped Refresh Persistence Policy); `src/Hexalith.Memories.Web/Components/Interaction/*` - Re-open trigger: before preserving tenant, case, filter, selected packet/source, grid sort/page, or expanded evidence across browser refresh is claimed or implemented. - Rationale: Refresh persistence is not just convenience state; it can leak stale tenant/case context if it is not tenant-scoped and invalidated. It needs an explicit state-policy story before implementation; Story 17.7 can validate browser behavior but does not decide persistence semantics.
status: open

### DW-497: ID: 17-WEB-AD3-MOBILE-GRID-STRATEGY

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Lenses/*`; `src/Hexalith.Memories.Web/Components/Evidence/*
reason: - ID: 17-WEB-AD3-MOBILE-GRID-STRATEGY - Status: carried-forward - Source story: epic-17-retro-2026-06-24 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Lenses/*`; `src/Hexalith.Memories.Web/Components/Evidence/*` - Re-open trigger: before a mobile product surface claims data grids, timelines, or source lists remain usable without horizontal scrolling at phone/tablet widths. - Rationale: Mobile grid/card/timeline behavior needs real viewport evidence and trust-field preservation checks. Keep it tied to the runnable host validation story.
status: open

### DW-498: ID: 17-WEB-AD4-ROLE-AUTHORIZATION-MODEL

origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`; `docs/dev/adr-10.2-004-auth-granularity.md`; `_bmad-output/planning-artifacts/epics.md` (Epic 20 authorization evidence)
reason: - ID: 17-WEB-AD4-ROLE-AUTHORIZATION-MODEL - Status: accepted - Source story: epic-17-retro-2026-06-24 - Target artifact: `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`; `docs/dev/adr-10.2-004-auth-granularity.md`; `_bmad-output/planning-artifacts/epics.md` (Epic 20 authorization evidence) - Re-open trigger: a product/security requirement introduces role-scoped web permissions, per-tool scopes, read-only agent access, or separate ingestion delegation. - Rationale: Current role-specific web lenses are evidence-density profiles over the same canonical Evidence Packet, not a permission model. The accepted auth model is authenticated caller plus matching tenant claim, with per-tool/role scopes deferred until a real consumer requires them. Adding role authorization now would invent policy outside the current product requirement.
status: open

### DW-499: ID: MEM-1

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/dev/public-surface-stability.md (review-enforced Mcp `PackageId` stability half); _bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md
reason: - ID: MEM-1 - Status: carried-forward - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: docs/dev/public-surface-stability.md (review-enforced Mcp `PackageId` stability half); _bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md - Re-open trigger: the published `Hexalith.Memories.Mcp` NuGet `PackageId` is renamed without a semantic-release `BREAKING CHANGE:` note, or a pack-time/analyzer guard becomes available to enforce the `PackageId` half that reflection cannot cover. - Rationale: Story 18.1 (done) delivered the compile-resolution guard test (`AppHostProjectResolutionTests`) and the name-stability contract (`docs/dev/public-surface-stability.md`), test-enforcing 5 of 6 contract items (project-symbol resolution, Server/Mcp assembly name + root namespace, Aspire symbol shape); the Mcp `PackageId` is a pack-time NuGet property not reflectable from a built assembly, so it stays review-enforced only and is carried forward as the residual half. Owner: AppHost/release maintainer. Story 19.1 (2026-06-30) refreshed this entry against the now-completed Story 18.1 without reopening Epic 18.
status: open

### DW-500: ID: MEM-2

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.2)
reason: - ID: MEM-2 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.2) - Re-open trigger: a downstream operator cannot fill deployment placeholders because the canonical env/port/OTLP config surface is undocumented or has drifted from code. - Evidence: Story 18.2 published the canonical deploy-config contract at `docs/operations/deployment-configuration.md` (OTLP env gate, Dapr sidecar ports, required runtime env, pub/sub event-intake surface, app-id reconciliation) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (bidirectional doc<->code tie on the `EventIngestionController` constants plus authoritative source-file cross-checks). Residual full aspirate manifest emission is carried forward as `MEM-2-ASPIRATE`.
status: done 2026-09-01
resolution: already resolved: docs/operations/deployment-configuration.md and tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs publish and guard the deployment configuration contract.

### DW-501: ID: MEM-2-ASPIRATE

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires.
reason: - ID: MEM-2-ASPIRATE - Status: accepted - Source story: 18-2-deployment-configuration-contract-publication - Target artifact: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires. - Re-open trigger: a downstream consumer needs ready-to-apply Kubernetes/Dapr manifests emitted from the AppHost topology rather than a hand-filled documented contract. - Rationale: Story 19.2 (2026-06-30) accepts the documented-contract approach as sufficient for current consumers and declines to schedule aspirate emission. The maintained deploy-config contract publishes every env/port/OTLP/pub-sub literal an operator must supply, and DeploymentConfigurationContractTests fails the build on doc<->code drift, so consumers fill kustomization placeholders today without generated manifests; no current consumer requires emitted manifests, and no aspirate tooling exists in src/** or tools/**. Per the 2026-05-27 "document now, defer aspirate" locked decision this is accept-until-trigger. Owner: AppHost / release maintainer.
status: open

### DW-502: ID: MEM-3

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.3)
reason: - ID: MEM-3 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.3) - Re-open trigger: an external Dapr ACL cannot be verified against the Memories operation surface, or the published surface drifts from the mapped endpoints. - Evidence: Story 18.3 published the invocable route/operation-surface contract at `docs/operations/route-surface.md` (full 45-route `/api/*` inventory, pub/sub `/dapr/subscribe` + `POST /events/ingest` operation surface, health and MCP probes, and the explicit `/process` refutation tied to code) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` (forward code->doc route tie deriving the list from `Program.cs`, a 45-route count tie, bidirectional pub/sub + health constant ties, an MCP source-text tie, and a code-tied `/process` negative assertion). Residual OpenAPI/Swagger document emission is carried forward as `MEM-3-OPENAPI`.
status: done 2026-09-01
resolution: already resolved: docs/operations/route-surface.md and tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs publish and guard the current route surface.

### DW-503: ID: MEM-3-OPENAPI

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires.
reason: - ID: MEM-3-OPENAPI - Status: accepted - Source story: 18-3-invocable-route-and-operation-surface-publication - Target artifact: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires. - Re-open trigger: a downstream consumer needs a generated OpenAPI/Swagger document (machine-consumable schema for client/ACL generation) rather than the maintained route-surface contract. - Rationale: Story 19.2 (2026-06-30) accepts the maintained route-surface contract as sufficient for current consumers and declines to schedule OpenAPI/Swagger generation. Story 18.3 AC2 explicitly permitted "an OpenAPI document OR a maintained route-surface doc"; route-surface.md publishes the full 46-route ACL-verifiable surface and RouteSurfaceContractTests ties it to Program.cs so it cannot drift. The repo has no AddOpenApi/MapOpenApi/Swashbuckle today and no consumer needs a generated schema; standing up OpenAPI for 46 minimal-API endpoints plus the pub/sub controller is accept-until-trigger. Owner: Server / API maintainer.
status: open

### DW-504: ID: MEM-4

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.4)
reason: - ID: MEM-4 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.4) - Re-open trigger: concurrent same-source ingests race into duplicate/partial memory units, or consumers still require the `HXL001` suppression to ingest. - Evidence: Story 18.4 graduated `MemoriesClient.IngestAsync` out of `[Experimental("HXL001")]` (stable 8-param overload preserved + additive `idempotencyToken` overload; `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`), added the optional `IngestionInput.IdempotencyToken` contract property (`src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`), and closed the REST `/api/ingest` check-then-act race with an atomic Redis `SET … NX` preflight reservation (`src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs`, wired in `Program.cs`) keyed by idempotency-token-precedence/`sourceUri`-fallback while preserving the permanent `sourceUri → MemoryUnitId` mapping for Stories 18.5/18.6. Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` (winner/loser concurrent-ingest + fail-open), `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` (token precedence + sourceUri fallback), `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` (token-keyed duplicate short-circuit + dual permanent record), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` (stable client, token on the wire), and `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` (camelCase round-trip + back-compat). Stable contract documented at `docs/dev/ingest-contract.md`; `HXL001` ledger updated at `docs/dev/experimental-apis.md`.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:59 uses IngestDedupReservation, covered by IngestDedupReservationTests.cs.

### DW-505: ID: MEM-5

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.5)
reason: - ID: MEM-5 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.5) - Re-open trigger: a consumer resolving a memory unit from a known source URI must rely on free-text search and silently degrades graph mode to local. - Evidence: Story 18.5 exposed an exact source-URI-keyed lookup that reads the permanent dedup record as the authoritative index (no parallel store): new route `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri` (`src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`, mapped in `Program.cs`) backed by the lookup seam `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` (reuses `DedupKeyBuilder.BuildKey`, excludes the transient `PreflightDedupReservation` marker, and propagates Redis failures so the endpoint returns a structured `503 LOOKUP_BACKEND_UNAVAILABLE` rather than a false `404`). Surfaced through the additive `Contracts.V1` record `MemoryUnitIdLookupResponse` (registered in `MemoriesJsonContext`), the public `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` (`string?`, 404→null; D9 concrete/virtual), and the CLI diagnostic `memories search lookup` (`src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs`, `CliExitCodes.NotFound` on miss). MCP exposure deliberately declined (operational/diagnostic resolution). Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs`, `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs` (200 / structured-404 / 400 / cross-tenant / different-case / transient-reserved / Redis-down→503 / literal-route precedence), `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs` (camelCase round-trip), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` (path/encoding, 200→id, 404→null, error→MemoriesRemoteException), and `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs`. Published route surface updated at `docs/operations/route-surface.md` (45→46) with `RouteSurfaceContractTests` green.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs:266 delegates source-URI lookup to MemoryUnitLookupEndpoint.HandleAsync, covered by MemoryUnitLookupEndpointTests.cs.

### DW-506: ID: MEM-6

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.6)
reason: - ID: MEM-6 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.6) - Re-open trigger: a consumer's `MemoryUnitId`-keyed mapping accumulates ghost ids after a Memories restart/contract change because the stability semantics are unspecified. - Evidence: Story 18.6 published the MemoryUnitId stability contract at `docs/dev/memory-unit-id-stability.md` (MemoryUnitId is an opaque id string, not derived from `sourceUri` and not guaranteed to be a ULID; same `(tenantId, caseId, sourceUri)` re-ingestion returns the same canonical id while the permanent `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record persists; TTL-less `expiry: null` dependency made explicit; Redis-eviction / manual-deletion / TTL / key-format-change / cross-environment-reindex loss modes documented; the dedup record is the id-resolution authority, not the backend index; Story 18.4 token records `dedup:{tenantId}:{caseId}:tok:{sha256(token)}` augment-never-replace the source-URI record; Story 18.5 `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` / `GET .../memory-units/by-source-uri` named the authoritative resolution path; Parties 'decision D1' clarified as unrelated to Memories Architecture Decision D1 'FalkorDB for MVP') with an authoritative-guarantee cross-link added at `docs/dev/ingest-contract.md` section 6. Guarded by `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs` (doc<->code ties on SaveDedupKeyActivity `expiry: null`, DedupKeyBuilder `dedup:{tenantId}:{caseId}:` / `:tok:` shapes, and SourceUriMemoryUnitLookup `DedupKeyBuilder.BuildKey`) and `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/SaveDedupKeyActivityTests.cs` (TTL-less expiry assertion); existing IngestionWorkflowTests / DedupKeyBuilderTests continue to prove stable-instance-id reuse, independent id for `dedup:` event workflows, duplicate short-circuit, and dual permanent records.
status: done 2026-09-01
resolution: already resolved: docs/dev/memory-unit-id-stability.md is guarded by MemoryUnitIdStabilityContractTests.cs.

### DW-507: ID: MEM-7

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 18.7)
reason: - ID: MEM-7 - Status: resolved - Source story: parties-consumer-integration-intake-2026-05-27 - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.7) - Re-open trigger: `MemoriesClient` is sealed or has `virtual` members removed, breaking consumer subclass-based test fixtures. - Evidence: Story 18.7 published the MemoriesClient mockability stability contract at `docs/dev/client-mockability.md` (reaffirms Architecture Decision D9 — concrete class, avoid the abstraction tax, extract an interface only when a second implementation arrives — and explicitly declines to add `IMemoriesClient`; documents the two supported seams: the recommended `HttpClient`/`IHttpClientFactory` boundary with a worked example and subclass override; guarantees `MemoriesClient` stays public + non-sealed with `virtual` public methods; records the breaking-change rule that sealing the class or removing `virtual` requires the D9 escape hatch (extract `IMemoriesClient`) plus a sprint change; notes the non-virtual `BaseAddress` passthrough is outside the mock seam) with companion cross-links added in `docs/dev/public-surface-stability.md` and `docs/dev/experimental-apis.md`. Guarded by `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (doc mandatory-claims content ties + reflection guard asserting `MemoriesClient` is public, non-sealed, exposes no `IMemoriesClient`, and that every public declared instance method is `IsVirtual && !IsFinal`, plus worked-example `[Fact]`s for both seams). The subclass seam remains proven by `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` and the `HttpClient` seam by the `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` suite; no production code changed (`MemoriesClient` already satisfied the contract).
status: done 2026-09-01
resolution: already resolved: docs/dev/client-mockability.md and its contract tests preserve the non-sealed virtual MemoriesClient seam.

### DW-508: `HasIndexedMemoryUnits` captured but never consulted

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:61
reason: - **`HasIndexedMemoryUnits` captured but never consulted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:61] — true-empty (no index for the tenant) is not distinguished from query-empty; both yield `state: empty` + `broadenScope`, which is misleading for an unindexed tenant. Needs a recovery/state design choice (e.g. an "ingest first" recovery when `HasIndexedMemoryUnits==false`).
status: open

### DW-509: `state: empty` emitted when `returnedCount==0` but `totalCount>0` and nothing omitted

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:224
reason: - **`state: empty` emitted when `returnedCount==0` but `totalCount>0` and nothing omitted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:224] — the `returnedCount==0` short-circuit ignores `totalCount`, producing a self-contradictory empty packet. Low likelihood (requires upstream to report a contradictory result).
status: open

### DW-510: Graph summary hardcoded `Available=false`/empty even when a `graph` axisEvidence entry has a real `GraphScore`

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:71, 132
reason: - **Graph summary hardcoded `Available=false`/empty even when a `graph` axisEvidence entry has a real `GraphScore`** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:71, 132] — internal inconsistency between graph axis evidence and the graph summary section. Tolerable per spec (explicit-unavailable); graph mapping is optional/out of scope for this story.
status: open

### DW-511: [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs
reason: - **RESOLVED 2026-07-05 — `EvidencePacketSource` has no freshness field** [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.
status: done 2026-07-05
resolution: `EvidencePacketSource` has no freshness field** [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

### DW-512: 23.7-APPHOST-EVENTSTORE-FULLSTACK: accepted. Current package/source compilation and the

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 28.1)
reason: - **23.7-APPHOST-EVENTSTORE-FULLSTACK - accepted.** Current package/source compilation and the Memories-owned Aspire Redis/FalkorDB + Dapr ingestion lane pass, but the AppHost does not provision an `eventstore` gateway resource and current source/package identities do not match EventStore Story 1.20's exact approved pins. The focused event-ingestion lane publishes directly to Memories and is not EventStore-to-Memories proof. This preserves the original Story 21.2 finding: case, annotation, memory-unit, and case-deletion mutations target the `Hexalith.EventStore.Client` gateway at Dapr app-id `eventstore` by default, while topology/deployment work remained outside the A3 write-boundary closure. The historical 2120-server-test result remains provenance, not current full-stack evidence. - ID: 23.7-APPHOST-EVENTSTORE-FULLSTACK - Status: accepted - Source story: story-21.2 dev; Epic 23 retrospective corrective action - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 28.1) - Rationale: the AppHost lacks the approved `eventstore` gateway topology and the current Memories-owned ingestion lane is not EventStore-to-Memories proof, so Story 28.1 retains the full-stack work and its explicit evidence requirements. - Resolution criteria: adopt the exact owner-approved EventStore source/package identities; compose one `eventstore` gateway resource with unambiguous `statestore`/`pubsub` ownership; run a real EventStore-originating publish through Dapr into Memories; prove persisted/searchable Redis and FalkorDB outcomes plus ignored duplicate replay; attach tenant-isolation negative evidence. - Re-open trigger: Story 28.1 is selected; any story or review claims EventStore-to-Memories or unqualified full-stack EventStore proof; or the AppHost adds an `eventstore` resource without closing every resolution criterion. - **Correction (2026-09-01):** Re-open trigger fired — Story 28.1 was selected and implemented. Source and package identity adoption, and the single `eventstore` gateway resource (unambiguous `statestore`/`pubsub` ownership), are both done and verified. The remaining resolution criterion — "run a real EventStore-originating publish through Dapr into Memories, prove persisted/searchable Redis and FalkorDB outcomes plus ignored duplicate replay, attach tenant-isolation negative evidence" — is not met and, per `epics.md`'s Story 28.1 final Given/When/Then clause ("Given adoption exposes a behavioral incompatibility... fails closed and routes that behavior change to a separately approved compatibility story rather than expanding silently") and spec-28-1's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary, is not attempted further within Story 28.1. (Note: neither the spec nor `epics.md` numbers this clause "AC7" — that was an earlier miscitation, corrected here.) See new entry `28.1-TASK4-FULLSTACK-PROOF-NEEDS-DOMAIN-SERVICE` below for the precise remaining gap and candidate resolutions. This entry stays `accepted` (not `resolved`) until that gap closes.
status: open

### DW-513: Story 24.3 graph content and honest test evidence.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md
reason: - **Story 24.3 graph content and honest test evidence.** - ID: 24.3-GRAPH-CONTENT-EVIDENCE - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md - Backlog home: Story 24.6 - Owner: Murat / Test Architect and Developer - Rationale: The graph-content evidence is implemented and verified, but its owning Story 24.6 remains in review. Keeping the entry carried forward preserves the deferred-work lifecycle contract until the story reaches `done`. - Evidence: Story 24.6 implemented `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` with identical tenant A/B node IDs, topology, insertion order, graph-scoped relationship-ID collision, authenticated dual-tenant traversal, tenant-local node/edge markers, and zero foreign markers. The assertion-sensitivity control `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` plants a tenant B edge-marker literal in tenant A and proves the edge-locality assertion rejects it; it is not cross-tenant access evidence and does not mutation-test the node-marker assertions. The final authoritative real Aspire/FalkorDB owning-class run on 2026-08-14 passed 7 total, 0 failed, 0 skipped in 254.358 seconds with `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060` (following the tenant-B PreviousConfidence obligation); it supersedes every earlier method/class duration. The current verifier/runbook/authorization result is recorded once in the target story's verification table rather than duplicated with a competing duration here. Proof boundary: `SeedCollisionGraphAsync` writes through `falkor.SelectGraph(tenantId)` directly rather than through production ingestion, so this evidence proves authenticated read-path tenant routing and graph-content locality under collisions, not production write-path tenant selection; write-path isolation remains separately deferred. Exact commands are recorded in the target artifact and its linked implementation spec. This entry remains carried forward while Story 24.6 is in review and moves to resolved only when Story 24.6 reaches `done`. - Re-open trigger: Story 24.6 reaches `done` (promote this entry to `resolved`), or any change to the graph fixture, `AssertTraversalIsFixtureLocal`, `GraphQueryBuilder`, traversal route/authorization, FalkorDB tenant selection, removal/skip/rename of either the positive or assertion-sensitivity method, any claim that `GRAPH.LIST` or unit mocks prove content isolation, or a future required lane that cannot execute the real backend.
status: done 2026-09-01
resolution: already resolved: Story 24.6 is done and TenantIsolationIntegrationTests.cs:76 implements the identical-graph-structure isolation proof.

### DW-514: Story 24.3 configured vector-dimension authority.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md
reason: - **Story 24.3 configured vector-dimension authority.** - ID: 24.3-VECTOR-DIMENSION-SOURCE - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md - Backlog home: Story 24.7 - Owner: Winston / Architect and Developer - Rationale: Story 24.3 compares raw and natural-language semantic dimensions only with each other, so an equally wrong pair can pass. Story 24.7 makes the requested tenant's existing `ITenantEmbeddingConfigProvider` value authoritative without recreating indexes or running migration. - Re-open trigger: Story 24.7 is selected, or any verifier assurance relies only on raw-versus-natural-language dimension equality.
status: done 2026-09-01
resolution: already resolved: Story 24.7 is done and TenantIsolationVerifierTests.cs:279 verifies non-default tenant-configured dimensions.

### DW-515: Story 24.3 collision-safe semantic key-family membership.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md
reason: - **Story 24.3 collision-safe semantic key-family membership.** - ID: 24.3-SEMANTIC-KEY-FAMILY - Status: carried-forward - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md - Backlog home: Story 24.8 - Owner: Developer and Murat / Test Architect - Rationale: Broad `{tenantId}:vec:*` and `{tenantId}:vecnl:*` scans include markerless raw/NL migration staging hashes and legacy nested-NL hashes, causing false marker-mismatch evidence. Because memory-unit IDs are opaque, Story 24.8 requires canonical provenance and record shape rather than prefix-only shortcuts, plus collision-shaped tests and a guarded unknown-family outcome. - Re-open trigger: Story 24.8 is selected, a migration or legacy tenant reports false `SemanticIsolation`, a new semantic namespace appears, or a classifier assumes reserved-looking colon text cannot occur inside an opaque memory-unit ID.
status: done 2026-09-01
resolution: already resolved: Story 24.8 is done and IndexSchemaDefinitionsTests.cs:213 covers reserved-looking opaque identifiers.

### DW-516: Story 24.3 distinct and non-destructive marker remediation.

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md
reason: - **Story 24.3 distinct and non-destructive marker remediation.** - ID: 24.3-MARKER-REMEDIATION - Status: resolved - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling - Target artifact: _bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md - Backlog home: Story 24.9 - Owner: Winston / Architect, Murat / Test Architect, and Developer - Rationale: Pre-Story-24.3 active hashes can lack `tenantId`, yet current verification combines missing and foreign markers and recommends removing mismatched target-prefix hashes. Story 24.9 keeps both outcomes fail-closed but classifies missing markers as incomplete evidence, foreign values as possible contamination, and limits remediation to named-key inspection/quarantine plus tenant-scoped repair or reindex after provenance verification. - Evidence: Discharged 2026-08-31 by Story 24.9 dev-story. `TenantIsolationVerifier.ScanSemanticHashPrefixForTenantEvidenceAsync` (src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs) now carries an internal `MarkerDefectKind` (`Missing`/`Foreign`) on each `MarkerMismatchEvidence` entry, never exposed through the public V1 contract. `CheckSemanticIsolationAsync`'s non-classification-gap `Details`/`Remediation` construction branches on that kind via the new `BuildSemanticIsolationRemediation` helper: a foreign non-empty `tenantId` reports a confirmed marker mismatch/possible contamination naming the exact key, expected tenant, and observed tenant; a missing `tenantId` reports incomplete evidence, not confirmed cross-tenant leakage; a mixed check preserves both distinct diagnoses. Every marker-related `Remediation` path now directs named-key inspection/quarantine and tenant-scoped marker repair or reindex only after provenance is verified, and the anti-template "remove mismatched target-prefix hashes" phrase is removed from every marker path (the syntactic-only `ScanHashPrefixForTenantFieldMismatchesAsync` scanner, a separate non-semantic check, is untouched and still out of this story's scope). The V1 `TenantIsolationCheckResult.Details`/`Remediation` plain-string shape is unchanged. Focused evidence: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` passed 94/94, 0 failed/skipped; `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Contracts.Tests/bin/Debug/net10.0/Hexalith.Memories.Contracts.Tests.dll -class Hexalith.Memories.Contracts.Tests.V1.TenantIsolationCheckResultSerializationTests` passed 7/7, 0 failed/skipped. New tests `TenantIsolationVerifierTests.VerifyAsync_MissingActiveMarker_ReturnsIncompleteEvidenceWithoutDeleteGuidance`, `VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, and `VerifyAsync_MixedMissingAndForeignActiveMarkers_PreservesBothDistinctDiagnoses` each assert the new wording and that no marker `Remediation` contains the retired blanket-deletion phrase. Full detail is in `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md`'s 2026-08-31 dev-story Change Log row; the story remains at `Status: review` pending code review, so this ledger discharge is scoped to the rationale/re-open condition below, not a claim that Story 24.9 itself is `done`. **Scope precision (2026-08-31 code review):** this discharge applies only when no classification gap co-occurs with the marker mismatch in the same `SemanticIsolation` check. When a classification gap and an active marker defect are both present, `CheckSemanticIsolationAsync`'s `Remediation` ternary still selects the classification-gap-only sentence and does not compose in the marker-specific guidance this entry describes; `TenantIsolationVerifierTests.VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesBothDiagnostics` now pins that exact behavior. That co-occurrence prioritization gap is the pre-existing, separately tracked `24.6-F8-W9` entry below, which remains open and unchanged by this discharge. - Re-open trigger: missing markers are described as confirmed leakage, operator guidance recommends broad prefix deletion, or Story 24.9's code-review phase finds the marker `Remediation`/`Details` wording regresses to the pre-24.9 shared/destructive form.
status: done 2026-09-01
resolution: already resolved: Story 24.9 is done and TenantIsolationVerifier.cs:673-731 preserves distinct missing/foreign marker diagnostics and non-destructive remediation.

### DW-517: 24.2-RV1: One tenant's enrichment exception fails the entire `GET /api/tenants` page. `TenantEndpointHandlers.cs:73` — `Task.WhenAll` rethrows the first fault and discards all other already-computed summaries, so there is no per-tenant isolation. Mitigated because `BuildTenantSummaryCoreAsync` catches embedding-config exceptions and `TenantMetricsService` is designed not to throw (returns null/degraded); triggering it needs an unexpected exception such as `ObjectDisposedException` on multiplexer teardown. Re-open trigger: any change that lets `BuildTenantSummaryCoreAsync` throw for a single tenant, or a report of `GET /api/tenants` returning 500 in a multi-tenant deployment.

origin: migrated from legacy ledger ("Deferred from: code review of 24-2-read-path-caching-and-tenant-list-bounding (2026-07-05)"), 2026-09-01
location: n/a
reason: - **24.2-RV1 — One tenant's enrichment exception fails the entire `GET /api/tenants` page.** `TenantEndpointHandlers.cs:73` — `Task.WhenAll` rethrows the first fault and discards all other already-computed summaries, so there is no per-tenant isolation. Mitigated because `BuildTenantSummaryCoreAsync` catches embedding-config exceptions and `TenantMetricsService` is designed not to throw (returns null/degraded); triggering it needs an unexpected exception such as `ObjectDisposedException` on multiplexer teardown. Re-open trigger: any change that lets `BuildTenantSummaryCoreAsync` throw for a single tenant, or a report of `GET /api/tenants` returning 500 in a multi-tenant deployment.
status: open

### DW-518: 24.2-RV2: Degraded/null metric snapshots are cached for the full summary TTL. `TenantEndpointHandlers.cs:82` + `TenantSummaryCache.cs:49` — a summary composed during a transient backend outage (null counts / Unknown / Degraded health) is cached wholesale for the full summary TTL (default 15s), so the degraded view persists after backend recovery. AC6 letter is met (nulls preserved, not false zeros); bounded by the short default TTL. Re-open trigger: summary TTL raised toward its 120s clamp, or operators reporting stale degraded tenant health after a backend recovers.

origin: migrated from legacy ledger ("Deferred from: code review of 24-2-read-path-caching-and-tenant-list-bounding (2026-07-05)"), 2026-09-01
location: n/a
reason: - **24.2-RV2 — Degraded/null metric snapshots are cached for the full summary TTL.** `TenantEndpointHandlers.cs:82` + `TenantSummaryCache.cs:49` — a summary composed during a transient backend outage (null counts / Unknown / Degraded health) is cached wholesale for the full summary TTL (default 15s), so the degraded view persists after backend recovery. AC6 letter is met (nulls preserved, not false zeros); bounded by the short default TTL. Re-open trigger: summary TTL raised toward its 120s clamp, or operators reporting stale degraded tenant health after a backend recovers.
status: open

### DW-519: Tune explicit histogram bucket boundaries for the search-duration and natural-language-description latency instruments so the committed p95 dashboard panels are accurate at seconds scale.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md` summary: Tune explicit histogram bucket boundaries for the search-duration and natural-language-description latency instruments so the committed p95 dashboard panels are accurate at seconds scale. evidence: Neither `memories.search.duration` nor `memories.natural.language.description.duration` configures bucket boundaries (no `AddView` in the metrics pipeline), so both use the SDK default buckets (...1000, 2500, 5000, 7500, 10000 ms). Natural-language description latency is documented at ~1-3s p95, so `histogram_quantile(0.95, ...)` over those coarse buckets can misreport a true 2.6s p95 by seconds. Pre-existing emission config surfaced by the new p95 panels; Story 24.4's "Never" boundary forbids changing emission behavior, so bucket tuning is separate work.
status: open

### DW-520: Decide whether historical BMAD implementation-artifact records (stories 9.2/9.3/20.5) should be forward-referenced or updated so they stop citing pre-rename metric names.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md` summary: Decide whether historical BMAD implementation-artifact records (stories 9.2/9.3/20.5) should be forward-referenced or updated so they stop citing pre-rename metric names. evidence: Story 24.4 renamed instruments (e.g. `memories_conversation_cache_hit_total` -> `memories.conversation.cache.hits`, `memories.rate_limit.rejections` -> `memories.rate.limit.rejections`), but `_bmad-output/implementation-artifacts/9-2-*.md`, `20-5-*.md`, and `7-5-*.md` still name the old instruments. These are point-in-time story records outside the spec's "source, tests, or docs" scope, so whether to rewrite history or add forward-reference notes is a judgment call the orchestrator owns.
status: open
decision: 2026-09-01 Add forward references — Add concise dated pointers to current architecture without rewriting historical assertions.
decision: 2026-09-01 Add forward references — Add concise dated pointers to current architecture without rewriting historical assertions.

### DW-521: 24.2-RV3: Pre-existing: two `DeleteMemoryUnitProjectionActivityTests` fail at HEAD (unrelated to Story 24.2). `RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast` and `RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry` fail on the full server slice (2 of 2441). Verified pre-existing by stashing the 24.2 review patches and re-running the class (still 2/3 failing), so NOT introduced by read-path caching — the delete-projection Redis hash/vector ordering area, likely from a later commit (24.3/24.4/CI). Flagged during the 24.2 code review for separate triage. Re-open trigger: whoever owns the delete-projection area investigates the NSubstitute in-order sequence assertion on annotation/target/syntactic-hash deletion.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)"), 2026-09-01
location: n/a
reason: - **24.2-RV3 — Pre-existing: two `DeleteMemoryUnitProjectionActivityTests` fail at HEAD (unrelated to Story 24.2).** `RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast` and `RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry` fail on the full server slice (2 of 2441). Verified pre-existing by stashing the 24.2 review patches and re-running the class (still 2/3 failing), so NOT introduced by read-path caching — the delete-projection Redis hash/vector ordering area, likely from a later commit (24.3/24.4/CI). Flagged during the 24.2 code review for separate triage. Re-open trigger: whoever owns the delete-projection area investigates the NSubstitute in-order sequence assertion on annotation/target/syntactic-hash deletion.
status: open

### DW-522: Case activity legacy `failedCount` one-time backfill is pre-empted by the write-path `HashIncrement`, so a legacy case whose first post-24.5 event is `IngestionFailed` permanently undercounts its pre-existing stream failures.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: Case activity legacy `failedCount` one-time backfill is pre-empted by the write-path `HashIncrement`, so a legacy case whose first post-24.5 event is `IngestionFailed` permanently undercounts its pre-existing stream failures. evidence: `GetFailedCountAsync` (src/Hexalith.Memories.Server/Cases/CaseActivityService.cs:141-147) backfills from the stream only when the `failedCount` summary field is absent, but `UpdateSummaryAsync` (:260-265) calls `HashIncrementAsync(failedCount)` on every `IngestionFailed`, creating the field at 1. A legacy case (no summary hash yet) whose first post-deploy event is a failure therefore reports `1` forever and never reconciles the older stream failures. A naive backfill-then-increment would double-count the just-appended event, so the fix needs an explicit "summary initialized" marker or a backfill-before-append restructuring. Related: `BackfillSummaryFromStreamAsync` (:185-232) also undercounts when a case exceeded `StreamMaxLength` and older failed entries were trimmed. - ID: 24.5-CASE-ACTIVITY-BACKFILL-PREEMPTED - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs` - Evidence: `GetFailedCountAsync` only backfills while the `failedCount` field is absent, but `UpdateSummaryAsync` creates that field with `HashIncrementAsync` on the first post-deploy failure; legacy stream failures can therefore remain unreconciled. - Re-open trigger: an operator or dashboard reports a case `failedCount` lower than the observed `IngestionFailed` events, or the summary/stream reconciliation is redesigned.
status: open

### DW-523: NL retry `EnqueueAsync` writes tenant backlog-set membership, payload hash, and sorted-set member as three non-atomic Redis ops, and the tenant set is pruned by a check-then-`SREM`; the resulting TOCTOU can strand a tenant's live retry entries and can orphan payload-hash fields on a crash.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: NL retry `EnqueueAsync` writes tenant backlog-set membership, payload hash, and sorted-set member as three non-atomic Redis ops, and the tenant set is pruned by a check-then-`SREM`; the resulting TOCTOU can strand a tenant's live retry entries and can orphan payload-hash fields on a crash. evidence: `EnqueueAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:55-57) does `SetAdd(TenantBacklogKey)`, `HashSet(payload)`, `SortedSetAdd(member)` in sequence; `RemoveTenantBacklogIfEmptyAsync` (:341-348, called from Complete :139, dead-letter :190, trim :436, corrupt-remove :338) reads `SortedSetLength==0` then `SREM`. If a completion's length-read and `SREM` interleave around a concurrent enqueue's `SetAdd`/`SortedSetAdd`, the tenant is dropped from `nl-embedding-retry-tenants` while a live member remains; `ListTenantsWithBacklogAsync` (:270-286) then skips it and the legacy KEYS fallback runs only when the whole set is empty (:260), so the entry is retried only on that tenant's next enqueue. A crash between :56 and :57 also orphans a payload-hash field with no member (DequeueBatch reads members only), leaking `GetBacklogBytes`. A correct fix needs atomic (Lua) enqueue/prune rather than op reordering. - ID: 24.5-NL-RETRY-TENANT-SET-ATOMICITY - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs` - Evidence: `EnqueueAsync` writes tenant-set membership, payload hash, and sorted-set member as separate Redis operations, while `RemoveTenantBacklogIfEmptyAsync` uses a check-then-remove sequence that can interleave with enqueue. - Re-open trigger: a tenant's queued NL retry stops being polled while a live member remains, or payload-hash memory grows without a matching sorted-set member.
status: open

### DW-524: NL retry legacy-tenant discovery runs only when the new tenant-set is entirely empty, so pre-24.5 legacy tenant queues are never surfaced once any new enqueue populates the set.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: NL retry legacy-tenant discovery runs only when the new tenant-set is entirely empty, so pre-24.5 legacy tenant queues are never surfaced once any new enqueue populates the set. evidence: `ListTenantsWithBacklogAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:259-268) calls `ListLegacyTenantsWithBacklogAsync` only inside `if (tenantIds.Length == 0)`. During a 24.5 rollout, the first new failure for any tenant populates `nl-embedding-retry-tenants`, after which legacy tenants (queues with no tenant-set entry) are never discovered and their retries stall until each receives a fresh failure. Running the legacy KEYS scan unconditionally each poll is barred by the story's "no key scans on hot paths" boundary, so the fix needs a one-time startup migration sweep. - ID: 24.5-NL-RETRY-LEGACY-TENANT-DISCOVERY - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs` - Evidence: `ListTenantsWithBacklogAsync` calls the legacy tenant discovery path only when the new tenant set is empty, so any new enqueue can hide existing legacy queues until each legacy tenant receives fresh work. - Re-open trigger: legacy NL retry work remains unprocessed after a 24.5 deployment that also enqueued new failures.
status: open

### DW-525: NL retry `CompleteAsync`/`IncrementAttemptsAsync` skip their optimistic condition when the current payload-hash field is null (legacy JSON member or already-deleted), so an unconditional remove can silently clobber a concurrent fresh re-enqueue for the same memory unit.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: NL retry `CompleteAsync`/`IncrementAttemptsAsync` skip their optimistic condition when the current payload-hash field is null (legacy JSON member or already-deleted), so an unconditional remove can silently clobber a concurrent fresh re-enqueue for the same memory unit. evidence: In `CompleteAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:123-136) and `IncrementAttemptsAsync` (:168-176) the `Condition.HashEqual` guard is added only when `currentPayload.HasValue`; when it is null the transaction commits unconditionally and `SortedSetRemove(memoryUnitId)` + `HashDelete` run, so a fresh enqueue that wrote a payload + member for the same `MemoryUnitId` between the null `HashGet` and the transaction is deleted and the new failure is lost. Adding a `Condition.HashNotExists(payloadKey, memoryUnitId)` on the null branch would abort the transaction if a fresh payload appeared while still removing genuine legacy members; needs a regression test proving the concurrent-enqueue case. - ID: 24.5-NL-RETRY-NULL-PAYLOAD-CLOBBER - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs` - Evidence: `CompleteAsync` and `IncrementAttemptsAsync` add the optimistic `HashEqual` condition only when the current payload exists; the null-payload branch can remove a concurrently re-enqueued payload/member for the same memory unit. - Re-open trigger: a freshly enqueued NL retry disappears during legacy-format migration, or the null-payload transaction path is hardened.
status: open

### DW-526: The ingestion in-flight registry has no TTL or size cap and is pruned only by status polls or the startup gate, so un-polled fire-and-forget ingestions accumulate terminal entries unboundedly and inflate the next startup drain.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: The ingestion in-flight registry has no TTL or size cap and is pruned only by status polls or the startup gate, so un-polled fire-and-forget ingestions accumulate terminal entries unboundedly and inflate the next startup drain. evidence: `RedisIngestionWorkflowInFlightRegistry.TrackAsync` (src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs:33-46) only ever adds; the `IngestionWorkflow` and its activities never call `RemoveAsync`, and the only prunes are `DaprIngestionWorkflowStateReader.GetWorkflowStateAsync` (fires only when a client polls `/api/ingest/{instanceId}`) and the startup gate. A long-lived server whose ingestions are never polled grows `ingestion-workflow:in-flight` (sorted set) and `:members` (hash) without bound; the next restart's `TryCountInFlightAsync` (src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs:113-160) then issues one sequential `GetWorkflowStateAsync` (10s per-query timeout) per dead entry and can exceed the 5-minute `TotalTimeout`, so the replay gate proceeds (`event 9172`) without confirming the drain. `RemoveAsync`'s lookup-miss fallback (`FindMembersByInstanceIdAsync`, :159-174) also degrades to a full O(N) sorted-set read, compounding the drain cost. Needs terminal-state removal on workflow completion plus a TTL/size bound and batched status reads. - ID: 24.5-INFLIGHT-REGISTRY-UNBOUNDED - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs` - Evidence: `TrackAsync` adds entries but workflows never remove terminal entries directly; cleanup depends on polling or startup gating, which can leave unpolled terminal ingestions in Redis indefinitely. - Re-open trigger: the in-flight registry keys grow without bound, or the replay gate times out (`event 9172`) on a normal restart.
status: open

### DW-527: The replay-safety in-flight registry marks itself initialized on the first `TrackAsync` against shared Redis, so a multi-replica rolling upgrade can disable the one-time enumeration fallback for a sibling replica that still has untracked pre-24.5 in-flight workflows.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: The replay-safety in-flight registry marks itself initialized on the first `TrackAsync` against shared Redis, so a multi-replica rolling upgrade can disable the one-time enumeration fallback for a sibling replica that still has untracked pre-24.5 in-flight workflows. evidence: `TrackAsync` (src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs:45) unconditionally sets `InitializedKey`, and `WorkflowReplaySafetyHostedService.TryCountInFlightAsync` (src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs:122-132) runs the enumeration fallback only while the registry is empty AND uninitialized. In a rolling upgrade sharing one Redis, if the first upgraded replica proceeds past the 5-minute drain timeout (`event 9172`, already a Critical degraded state) with old pre-24.5 workflows still active and then schedules new work, `TrackAsync` sets the marker; a sibling replica starting afterward sees `IsInitialized=true`, skips enumeration, checks only tracked ids, and never observes the still-active untracked old workflows — the version-mismatch replay the gate exists to prevent. Marking initialized only after a confirmed zero-drain (not on track), or a rollout-scoped initialization signal, would close it. - ID: 24.5-REPLAY-GATE-ROLLOUT-MARKER - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs` - Evidence: `TrackAsync` sets the shared initialized marker before the startup gate has proven a zero-drain state, so another replica can skip enumeration fallback during a rolling upgrade. - Re-open trigger: a multi-replica rollout replays a pre-registry in-flight ingestion workflow after another replica passed the gate.
status: open

### DW-528: Ingestion endpoints dereference null JSON request bodies while setting telemetry before reaching the existing structured validation response.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Ingestion endpoints dereference null JSON request bodies while setting telemetry before reaching the existing structured validation response. evidence: The review traced `POST /api/ingest`, `/api/ingest/url`, and `/api/ingest/directory`; each path reads `input.TenantId` or `request.TenantId` before the existing `Validate*Request` helper can return `INVALID_INPUT`. This behavior existed in the original inline `Program.cs` handlers and was preserved by the decomposition.
status: open

### DW-529: Search source-type validation accepts numeric enum values that do not correspond to defined `SourceType` members.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Search source-type validation accepts numeric enum values that do not correspond to defined `SourceType` members. evidence: The review traced `/api/search` validation and found `Enum.TryParse<SourceType>(sourceType, ignoreCase: true, out _)` without `Enum.IsDefined`; numeric values can parse and flow into search filters. This behavior existed before endpoint extraction and is outside the mechanical decomposition scope.
status: open

### DW-530: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting. evidence: The review traced `/api/tenants/{tenantId}/traverse`; `edgeTypes=","` produces an empty split result and assigns an empty `parsedEdgeTypes` list. This behavior existed before endpoint extraction and can return no edges where the default traversal would have applied.
status: open

### DW-531: Edge-confidence promotion does not reject undefined `EdgeType` enum values after deserialization.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Edge-confidence promotion does not reject undefined `EdgeType` enum values after deserialization. evidence: The review traced `/api/tenants/{tenantId}/edges/confidence`; the payload is deserialized into `ConfidencePromotionRequest` and field presence is validated, but `Enum.IsDefined(request.EdgeType)` is not checked before the graph update. This behavior existed before endpoint extraction.
status: open

### DW-532: Tenant provision/deletion status endpoints do not translate Dapr sidecar outages into structured `DAPR_UNAVAILABLE` responses.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Tenant provision/deletion status endpoints do not translate Dapr sidecar outages into structured `DAPR_UNAVAILABLE` responses. evidence: The review traced `GET /api/tenants/{tenantId}/provision-status/{instanceId}` and `GET /api/tenants/{tenantId}/deletion-status/{instanceId}`; `GetWorkflowStateAsync` exceptions flow to the generic unhandled-exception path. This behavior existed before endpoint extraction.
status: open

### DW-533: Tenant deletion Dapr-unavailable rollback catches only `InvalidOperationException`, so other rollback failures can replace the intended 503 response.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Tenant deletion Dapr-unavailable rollback catches only `InvalidOperationException`, so other rollback failures can replace the intended 503 response. evidence: The review traced the Dapr-unavailable branch in `DELETE /api/tenants/{tenantId}`; rollback errors other than `InvalidOperationException` are not swallowed or logged. This behavior existed before endpoint extraction and can leave a tenant in deleting state while returning an unexpected 500.
status: open

### DW-534: Large extracted endpoint and service-registration files remain candidates for further focused decomposition.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md` summary: Large extracted endpoint and service-registration files remain candidates for further focused decomposition. evidence: The review noted that behavior was moved into per-resource files, but `SearchEndpoints`, `CasesEndpoints`, `TenantLifecycleEndpoints`, and `MemoriesServerServiceCollectionExtensions` remain large. Story 25.1 intentionally stopped at per-resource mechanical extraction to preserve behavior; finer-grained slices should be a follow-up once the route surface is stable.
status: open

### DW-535: The existing benchmark comparator happy-state progress indicator needs source-owned browser accessibility remediation before that state can be claimed axe-clean.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` summary: The existing benchmark comparator happy-state progress indicator needs source-owned browser accessibility remediation before that state can be claimed axe-clean. evidence: The Story 17.7 Playwright axe lane initially exposed an `aria-prohibited-attr` issue on the benchmark comparator progress indicator when rendered with the happy packet fixture. Story 17.7 source ownership is specimen/test-only, so the browser specimen uses the existing empty-state fixture and keeps the happy-state remediation deferred.
status: open

### DW-536: The existing benchmark comparator happy-state progress indicator remains source-owned browser accessibility follow-up work.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` summary: The existing benchmark comparator happy-state progress indicator remains source-owned browser accessibility follow-up work. evidence: The Story 17.7 review confirmed the browser specimen intentionally avoids claiming the benchmark happy-state progress-bar path as axe-clean because the underlying RCL/Fluent progress indicator produced `aria-prohibited-attr` browser accessibility evidence outside this specimen/test-only story's source ownership.
status: open

### DW-537: Hexalith.Memories.Web.Tests — including the Epic 17 machine-checked inventory/over-claim guards — is in the .slnx test inventory but absent from every tools/test-projects.*.txt lane, so it never runs in CI.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md` summary: Hexalith.Memories.Web.Tests — including the Epic 17 machine-checked inventory/over-claim guards — is in the .slnx test inventory but absent from every tools/test-projects.*.txt lane, so it never runs in CI. evidence: grep finds no reference to Web.Tests under `.github/` or `tools/`; the `test-unit-contract` CI job runs only the five projects listed in `tools/test-projects.unit-contract.txt` (Contracts/Server/Cli/Mcp/EventStore), yet that file's own header says "Keep in sync with Hexalith.Memories.slnx test inventory." The 476-test suite runs locally/pre-commit and passed here via `dotnet exec` (DiffEngine_Disabled=true), but no CI lane executes it, so the story's fail-closed inventory guards are not CI-enforced. bUnit is in-process/docker-free, so wiring Web.Tests into the unit-contract lane is viable but must be confirmed with a headless CI run that this sandbox cannot perform. Pre-existing drift broader than Story 17.7, surfaced because 17.7's headline machine-checked guards depend on CI execution.
status: done 2026-09-01
resolution: already resolved: tools/test-projects.unit-contract.txt:8 includes Hexalith.Memories.Web.Tests and CI executes the unit-contract lane.

### DW-538: Generic MCP tool failures direct operators to inspect server logs, but the current tool/executor path does not log the original exception.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md` summary: Generic MCP tool failures direct operators to inspect server logs, but the current tool/executor path does not log the original exception. evidence: `McpErrorMapper.MapGeneric` emits a sanitized suggestion to inspect MCP server logs, while the pre-existing tool catch blocks and the new shared executor map the exception without an `ILogger` emission; adding redacted source-generated diagnostics requires focused logging and telemetry ownership beyond this behavior-preserving refactor.
status: open

### DW-539: The trust strip still renders the packet's confidence (evidence strength) and freshness for an unauthorized or unknown-isolation packet, even though the source-count badge is now fail-closed to "sources unavailable"; the residual confidence/freshness badges leak a coarse "strong evidence exists" inference past the authorization wall.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-7-evidence-cockpit-ux-conformance (2026-07-11)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md` summary: The trust strip still renders the packet's confidence (evidence strength) and freshness for an unauthorized or unknown-isolation packet, even though the source-count badge is now fail-closed to "sources unavailable"; the residual confidence/freshness badges leak a coarse "strong evidence exists" inference past the authorization wall. evidence: `MemoriesTrustStrip.razor` gates the source-count badge on `Packet.State == Unauthorized || IsRestrictiveScope(Packet.Scope.IsolationStatus)` but gates `ConfidenceLabel`, `FreshnessText`, and `TokenBudgetText` only on `ShowPacketValues` (Packet mode), so a restrictive packet carrying real `EvidenceStrength`/`Freshness` still shows "Confidence: Strong" and the actual freshness while the count is suppressed. The exposure is pre-existing — the trust strip has always rendered confidence/freshness in Packet mode — and was surfaced incidentally because Story 25.7 hardened only the source-count badge. The intent scopes fail-closed suppression to source/axis/graph detail and explicitly forbids introducing new trust semantics, so tightening the trust-summary badges is out of scope for this story and needs a focused fail-closed trust-surface decision.
status: open

### DW-540: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
location: n/a
reason: - source_spec: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/spec-gh-29158532353-release-preflight-stale-branch.md` summary: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists. evidence: Repository-pinned semantic-release 25.0.5 emits `There is no previous release, the next release version is <version>`, while the pre-existing parser expects `The next release version is <version>`; this repository already has release tags, so first-release support is outside the stale-checkout incident fix.
status: open

### DW-541: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md` summary: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run. evidence: `.github/workflows/ci.yml` has no step that runs `validate-release-packages.ps1 -PackageDirectory`, no `python -m unittest` over `tests/tooling/release_packages` or `tests/tooling/publish_nuget`, and no `dotnet pack`; the `release.yml` bare invocation passes no `-PackageDirectory` (skipping the whole new block), the `release_packages` fixtures run only inside `release.yml`, and the real-package throws fire only inside `semantic-release` → `pack-release.ps1`. A PR that re-adds a `Hexalith.Memories.*` ProjectReference to the Redis compatibility package, drifts the Mcp→ServiceDefaults dependency version out of lockstep, or edits the ServiceDefaults/Redis/publish tooling passes all PR checks — the only PR-lane guard, `BackendProjects_ShouldNotUseRedisCompatibilityPackageAsDependencyFacade`, asserts csproj text, not packed nuspec dependency graphs. `tests/tooling/publish_nuget/publish_nuget_test.py` is edited by this story yet is discovered by no CI lane at all. Pre-existing CI-lane architecture (release validation has always run at release time), surfaced by Story 25.8 adding substantial new post-merge-only validation; closing it means wiring the tooling fixtures (and ideally a pack plus `-PackageDirectory` validation) into the PR lane, which must be confirmed with a headless CI run this sandbox cannot perform.
status: done 2026-09-01
resolution: already resolved: .github/workflows/ci.yml:289-293 runs release_packages and publish_nuget tests; line 330 runs pack-release.ps1 with PackageOnly.

### DW-542: Published commit `8e92fe7` advanced five root submodule pointers even though Story 25.3 excluded submodule edits.

origin: migrated from legacy ledger ("Deferred from: code review of 25-3-shared-route-table-and-client-consolidation (2026-07-11)"), 2026-09-01
location: _bmad-output/implementation-artifacts/25-3-shared-route-table-and-client-consolidation.md
reason: - source_spec: `_bmad-output/implementation-artifacts/25-3-shared-route-table-and-client-consolidation.md` summary: Published commit `8e92fe7` advanced five root submodule pointers even though Story 25.3 excluded submodule edits. evidence: Removing those historical gitlink changes now would require rewriting published history or rolling dependencies back across later `main` commits. The user finalized remediation commit `eb959d7` without authorizing either destructive operation, so the scope deviation remains documented rather than mutating current dependency state.
status: open

### DW-543: NonDisposingRateLimiter has no disposed-state guard; a host-shutdown ordering race can call a disposed inner limiter and throw ObjectDisposedException on an in-flight request.

origin: migrated from legacy ledger ("Deferred from: code review of spec-25-4-contract-persistence-separation-and-route-versioning (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md` summary: NonDisposingRateLimiter has no disposed-state guard; a host-shutdown ordering race can call a disposed inner limiter and throw ObjectDisposedException on an in-flight request. evidence: `src/Hexalith.Memories.Server/RateLimiting/NonDisposingRateLimiter.cs:24` delegates `AcquireAsyncCore`/`AttemptAcquireCore` blindly to `_inner`; `InboundRequestRateLimiter.DisposeAsync` (`src/Hexalith.Memories.Server/RateLimiting/InboundRequestRateLimiter.cs:56`) disposes every shared inner `FixedWindowRateLimiter` while the ASP.NET partitioned limiter may still hold the `NonDisposingRateLimiter` wrappers. Low-impact (shutdown only) and the fix (disposed-state guard or shutdown-ordering) is a design choice, so deferred rather than patched. The wrapper correctly solves the framework idle-eviction disposal it was written for; only the shutdown corner is unguarded.
status: open

### DW-544: The array element values of `deletedBackends`/`compensatedBackends` changed vocabulary (RediSearch→syntactic, RedisVector→semantic, FalkorDB→graph, RedisDataKeys→state); the JSON keys are pinned but no test pins the workflow-emitted values.

origin: migrated from legacy ledger ("Deferred from: code review of spec-25-4-contract-persistence-separation-and-route-versioning (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md` summary: The array element values of `deletedBackends`/`compensatedBackends` changed vocabulary (RediSearch→syntactic, RedisVector→semantic, FalkorDB→graph, RedisDataKeys→state); the JSON keys are pinned but no test pins the workflow-emitted values. evidence: `[JsonPropertyName]` preserves the keys, but `TenantDeletionWorkflow.cs`/`TenantProvisioningWorkflow.cs` now emit axis names in the arrays. `TenantProvisioningResultSerializationTests` uses arbitrary sample values (e.g. `["RediSearch","RedisVector"]`) that no longer reflect production output, so a regression in the emitted vocabulary would not be caught. Intended part of the retrieval-axis migration; deferred as a minor test-pin gap. Any downstream ACL/runbook that string-matches the old element values would break silently — relates to the "release as breaking" decision recorded in the story's Review Findings.
status: open

### DW-545: [Decision resolved — document & accept] Commits `94eb4c8` and `376096d` bumped the `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer` submodule gitlinks despite the story's "Never edit submodules" constraint.

origin: migrated from legacy ledger ("Deferred from: code review of spec-25-4-contract-persistence-separation-and-route-versioning (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md` summary: [Decision resolved — document & accept] Commits `94eb4c8` and `376096d` bumped the `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer` submodule gitlinks despite the story's "Never edit submodules" constraint. evidence: Both gitlink changes are already published on `main`; `Hexalith.FrontComposer` (Web UI composer) is unrelated to the contract/persistence/route scope of Story 25.4. User decision 2026-07-12: document and accept rather than rewrite published history (same resolution as the Story 25.3 review of commit `8e92fe7`). No revert performed. If a future release surfaces an unintended FrontComposer or EventStore change, revisit this entry.
status: open

### DW-546: [Decision resolved — release-PR action] The intentional breaking route/CLR cutover landed as `feat:` with no `BREAKING CHANGE:` footer (and test-only commit `94eb4c8` is mislabeled `feat:Add`).

origin: migrated from legacy ledger ("Deferred from: code review of spec-25-4-contract-persistence-separation-and-route-versioning (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md` summary: [Decision resolved — release-PR action] The intentional breaking route/CLR cutover landed as `feat:` with no `BREAKING CHANGE:` footer (and test-only commit `94eb4c8` is mislabeled `feat:Add`). evidence: User decision 2026-07-12: leave landed commit messages as-is (no history rewrite) and add a `BREAKING CHANGE:` footer to the eventual release/squash-merge PR so the generated CHANGELOG flags the `/api/v1` route + public-CLR-rename cutover as breaking for downstream consumers. Pre-GA (0.x) semver impact is limited, but the CHANGELOG breaking flag and Design-Notes intent ("must be released as a breaking refactor") require the footer. Action owner: whoever cuts the Epic 25 release PR.
status: open

### DW-547: [HIGH — gating, environment-blocked] The mandatory no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) has never executed; every runtime AC (AC-3/4/5 aggregate-health/degradation/fail-closed + AC-1 container-start) rests on it.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [HIGH — gating, environment-blocked] The mandatory no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) has never executed; every runtime AC (AC-3/4/5 aggregate-health/degradation/fail-closed + AC-1 container-start) rests on it. evidence: Dev sandbox lacks `docker`/`kind`; the story is correctly at `review`, not `done`. Must run green with ZERO skips on CI/an operator cluster before `done`. Prerequisite: the verifier's image-tag naming mismatch (patch finding, `verify-production-deployment.ps1:189-190` vs `publish-containers.ps1:64,72`) will likely make the first run fail at `docker tag` — fix it before relying on this gate. Re-open trigger: any change to the deploy topology, health/ACL semantics, or container publication.
status: done 2026-09-01
resolution: already resolved: Story 27.3 line 1633 records qualifying CI run 33400812038 at the reviewed source with all required steps.

### DW-548: [MEDIUM — hardening beyond AC] Redis Stack and FalkorDB containers run as root (only `allowPrivilegeEscalation:false` + `drop:[ALL]` + seccomp; no `runAsNonRoot`/`runAsUser`).

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [MEDIUM — hardening beyond AC] Redis Stack and FalkorDB containers run as root (only `allowPrivilegeEscalation:false` + `drop:[ALL]` + seccomp; no `runAsNonRoot`/`runAsUser`). evidence: `deploy/kubernetes/base/{redis,falkordb}-statefulset.yaml`. AC-1 mandates non-root only for Server/MCP (both UID 1654, satisfied). Making the data stores non-root needs `fsGroup`/PVC-permission handling for `/data`. Re-open trigger: a security-hardening pass on the deployment topology, or a Pod Security Standards enforcement decision.
status: open

### DW-549: [MEDIUM — hardening beyond AC] No NetworkPolicies or Pod Security Standards in the production deployment.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [MEDIUM — hardening beyond AC] No NetworkPolicies or Pod Security Standards in the production deployment. evidence: `deploy/kubernetes/base/**` has zero `NetworkPolicy` and no `pod-security` namespace labels; app port 8080 (no Service) is reachable by pod IP cluster-wide and health endpoints are anonymous. Not required by any AC. Re-open trigger: production network-segmentation / PSS requirement.
status: open

### DW-550: [LOW — drift seam] The cross-tenant cache-safety guard validates a container-bundled copy of the Conversation component, not the deployed one.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [LOW — drift seam] The cross-tenant cache-safety guard validates a container-bundled copy of the Conversation component, not the deployed one. evidence: `NaturalLanguageDescriptionOptionsValidator` reads `deploy/dapr/components/conversation-llm.yaml` (baked into the Server image); DAPR loads `deploy/kubernetes/base/dapr/conversation-openai.yaml`. Both `responseCacheTTL: 0s` today and the Production no-TTL branch (event 9165) closes the missing-material hole, but a nonzero TTL set on the deployed component is invisible to the guard. Near-best-achievable (an app cannot read a control-plane component). Re-open trigger: any change to `responseCacheTTL` handling or the component material path.
status: open

### DW-551: [LOW — cleanup] Two divergent DAPR config sources, plus an orphaned in-namespace `eventstore` identity.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [LOW — cleanup] Two divergent DAPR config sources, plus an orphaned in-namespace `eventstore` identity. evidence: `deploy/dapr/config.yaml` + `deploy/dapr/components/*` were rewritten but are not consumed by the authoritative `kubectl kustomize` render (which uses `deploy/kubernetes/base/dapr/*`); they must be hand-synced (one file is load-bearing only because the Server `.csproj` copies it into the image). `eventstore` gets a namespace-local ServiceAccount/Role/RoleBinding with no workload deployed here (external publisher by design). Re-open trigger: any DAPR component/config edit, to avoid the two copies drifting.
status: open

### DW-552: [LOW — hardening] Server/MCP images are tag-only with `imagePullPolicy: IfNotPresent` while data stores are digest-pinned (`@sha256:`).

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [LOW — hardening] Server/MCP images are tag-only with `imagePullPolicy: IfNotPresent` while data stores are digest-pinned (`@sha256:`). evidence: `deploy/kubernetes/base/{server,mcp}-deployment.yaml`. Safe only while semantic-release tags stay immutable; a reused tag lets nodes silently run a stale cached layer with no digest to detect it. Re-open trigger: a supply-chain/image-provenance hardening decision.
status: open

### DW-553: [LOW — unverified] `readOnlyRootFilesystem: true` with only `/tmp` writable may fault ASP.NET Core Data Protection.

origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md` summary: [LOW — unverified] `readOnlyRootFilesystem: true` with only `/tmp` writable may fault ASP.NET Core Data Protection. evidence: The default Data Protection key ring path (`~/.aspnet/DataProtection-Keys`) is not under the single writable `emptyDir` at `/tmp`; if antiforgery/cookie/ephemeral key material is ever touched, the app faults or warns. Unverified — no gate that ran boots the app under the read-only rootfs. Re-open trigger: the disposable-cluster rollout running, or adding any Data-Protection-dependent feature.
status: open

### DW-554: [LOW] Case-scoped restore does not enforce per-record case membership.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] Case-scoped restore does not enforce per-record case membership. evidence: `RestoreDataPlaneActivity.RunAsync` (`:71-124`) restores every case/unit/edge in the envelope and never reads `input.CaseId`; `ImportRequestValidator` checks only `manifest.CaseId`. No cross-tenant impact (caller is tenant-authorized). Re-open trigger: hardening the import validator to reject records outside the route case, or a multi-case case-scoped-export defect.
status: done 2026-09-01
resolution: already resolved: RestoreDataPlaneActivity.cs:105-116 validates case, memory-unit, and manifest targets before writes.

### DW-555: [LOW] Unknown edge `origin` is silently coerced to `Inferred` on restore.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] Unknown edge `origin` is silently coerced to `Inferred` on restore. evidence: `RestoreDataPlaneActivity.RestoreEdgeAsync` (`:232-235`) rewrites an unrecognized/future `origin` to `EdgeOrigin.Inferred` rather than preserving or rejecting it — a fidelity change on an audit field the story claims to round-trip exactly. Only fires on corrupt/foreign export data. Re-open trigger: an export produced by a newer edge-origin schema.
status: open

### DW-556: [LOW] No operation-level idempotency token — concurrent/duplicate import POSTs run duplicate full re-embeds.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] No operation-level idempotency token — concurrent/duplicate import POSTs run duplicate full re-embeds. evidence: `ImportEndpoints.HandleImportAsync` (`:147,164`) mints a fresh GUID instance id per request and unconditionally schedules a new `RestoreWorkflow`. End state converges (HSET overwrite + graph MERGE) so AC5's idempotency clause holds; the impact is doubled embedding-provider cost/load and interleaved writes on a retry/double-submit. Re-open trigger: an operator restore-cost incident, or a decision to reject a second in-flight restore per tenant.
status: open

### DW-557: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total. evidence: `RestoreReindexUnitActivity.RunAsync` (`:85-95`) returns `RestoreReindexResult(id, 0)` when the syntactic hash is absent, and `RestoreWorkflow` (`:68-71`) reports `RestoredMemoryUnits` from the data-plane count — so a partially-failed restore could report `completed` with full counts and units that are `Indexed` but have no `:vec:` vectors. Largely unreachable in the happy path (the data-plane activity writes every hash first and fails the workflow if it can't). Re-open trigger: any change that decouples data-plane restore from re-index, or an observed partial-restore incident.
status: done 2026-09-01
resolution: already resolved: RestoreWorkflow.cs:47-75 totals actual ProcessedMemoryUnits, and restore reindexing fails when syntactic content is missing.

### DW-558: [LOW — hygiene, not a defect] Line-ending normalization churn folded into the feature commits.

origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [LOW — hygiene, not a defect] Line-ending normalization churn folded into the feature commits. evidence: ~2,500 diff lines are LF→CRLF flips (correct direction, toward the repo's required CRLF standard), including `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` (959 lines, 0 substantive changes). Mixing a mass line-ending normalization into `feat` commits inflates the diff and can mask real edits. Re-open trigger: next time a bulk normalization is needed — isolate it in a dedicated `chore` commit.
status: open

### DW-559: 26.3-PRIVATE-HOST-FIXTURE: accepted. The shared Aspire ingestion fixture sets `Ingestion__UrlFetcher__AllowPrivateHosts=true` before AppHost startup and cannot vary that startup-only option per test.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs
reason: - **26.3-PRIVATE-HOST-FIXTURE - accepted.** The shared Aspire ingestion fixture sets `Ingestion__UrlFetcher__AllowPrivateHosts=true` before AppHost startup and cannot vary that startup-only option per test. - ID: 26.3-PRIVATE-HOST-FIXTURE - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs - Re-open trigger: add an isolated `AllowPrivateHosts=false` AppHost fixture variant that proves rejection creates neither workflow nor Redis/DAPR state. - Rationale: The production validation path is unit/API covered, but the current shared topology is intentionally private-host-enabled for scripted loopback ingestion. Owner: integration test maintainer.
status: open

### DW-560: 26.3-BULK-REINGEST-HICCUP: accepted. The five-way bulk re-ingestion scenario needs deterministic per-unit missing, claimed, and Redis-write-failure control in one request.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs
reason: - **26.3-BULK-REINGEST-HICCUP - accepted.** The five-way bulk re-ingestion scenario needs deterministic per-unit missing, claimed, and Redis-write-failure control in one request. - ID: 26.3-BULK-REINGEST-HICCUP - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs - Re-open trigger: provide a fixture-scoped claim/hiccup seam that can assign Scheduled, NotFound, Conflicted, and Errored outcomes without process-global mutation. - Rationale: Existing topology controls cannot inject one scoped Redis failure while preserving the four sibling outcomes and shared state store. Owner: ingestion reliability maintainer.
status: open

### DW-561: 26.3-COUNTER-STAGE-BARRIER: accepted. Exact simultaneous queued/extracting/embedding counts require deterministic workflow stage barriers.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs
reason: - **26.3-COUNTER-STAGE-BARRIER - accepted.** Exact simultaneous queued/extracting/embedding counts require deterministic workflow stage barriers. - ID: 26.3-COUNTER-STAGE-BARRIER - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs - Re-open trigger: add fixture-scoped extraction and embedding barriers that hold six real workflows at requested stages while the public case-status API and actor state are sampled. - Rationale: Direct actor transitions would not prove concurrent workflow integration, and timing-only delays would be flaky. Owner: workflow test maintainer.
status: open

### DW-562: 26.3-DIRECTORY-CROSS-TENANT-PERF: accepted. The cross-tenant directory latency claim needs a recorded baseline and bounded load harness.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs
reason: - **26.3-DIRECTORY-CROSS-TENANT-PERF - accepted.** The cross-tenant directory latency claim needs a recorded baseline and bounded load harness. - ID: 26.3-DIRECTORY-CROSS-TENANT-PERF - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs - Re-open trigger: an `IntegrationSlow` performance harness can create a bounded 100-file batch, record the single-tenant baseline, and assert persisted outcomes plus the two-times latency bound without CI noise. - Rationale: The ordinary integration lane has no stable performance baseline or load-isolated runner. Owner: performance test maintainer.
status: open

### DW-563: 26.3-BATCH-STARVATION-PERF: accepted. The 500-file batch-versus-single-ingest starvation claim requires the same missing load harness and latency baseline.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs
reason: - **26.3-BATCH-STARVATION-PERF - accepted.** The 500-file batch-versus-single-ingest starvation claim requires the same missing load harness and latency baseline. - ID: 26.3-BATCH-STARVATION-PERF - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs - Re-open trigger: an isolated performance lane can run the bounded 500-file workload, capture a control baseline, and retain per-unit Redis/actor evidence. - Rationale: A timing assertion in `integration-fast` would be environment-sensitive and would not prove persisted outcomes. Owner: performance test maintainer.
status: open

### DW-564: 26.3-SEMANTIC-CAPABILITY-FAULT: accepted. Semantic search shares the single `memories-vectors` Redis Stack resource with syntactic search, DAPR state, actors, workflows, and pub/sub.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
reason: - **26.3-SEMANTIC-CAPABILITY-FAULT - accepted.** Semantic search shares the single `memories-vectors` Redis Stack resource with syntactic search, DAPR state, actors, workflows, and pub/sub. - ID: 26.3-SEMANTIC-CAPABILITY-FAULT - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs - Re-open trigger: add a Development-only, request-scoped semantic capability fault that leaves RediSearch and DAPR state available and matches production exception behavior. - Rationale: Stopping `memories-vectors` cannot truthfully represent a semantic-only outage. Owner: search reliability maintainer.
status: open

### DW-565: 26.3-ALL-BACKENDS-STATESTORE: accepted. Stopping Redis Stack and FalkorDB also removes workflow, actor, pub/sub, and state-store availability.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
reason: - **26.3-ALL-BACKENDS-STATESTORE - accepted.** Stopping Redis Stack and FalkorDB also removes workflow, actor, pub/sub, and state-store availability. - ID: 26.3-ALL-BACKENDS-STATESTORE - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs - Re-open trigger: define and implement the supported API contract for total Redis-backed control-plane collapse, then add bounded recovery assertions against that real dependency graph. - Rationale: The legacy `ALL_BACKENDS_UNAVAILABLE` comment assumes independent retrieval containers that the AppHost does not have. Owner: platform reliability maintainer.
status: open
decision: 2026-09-01 Define fail-closed 503 — Define a bounded unavailable response and recovery contract, then add integration evidence.
decision: 2026-09-01 Define fail-closed 503 — Define a bounded unavailable response and recovery contract, then add integration evidence.

### DW-566: 26.3-SINGLE-AXIS-REDIS-COLLAPSE: accepted. A Redis resource stop is not a syntactic-only outage and can prevent the service from reading authorization, tenant, workflow, and actor state.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
reason: - **26.3-SINGLE-AXIS-REDIS-COLLAPSE - accepted.** A Redis resource stop is not a syntactic-only outage and can prevent the service from reading authorization, tenant, workflow, and actor state. - ID: 26.3-SINGLE-AXIS-REDIS-COLLAPSE - Status: accepted - Source story: 26-3-integration-stub-closure - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs - Re-open trigger: add a request-scoped RediSearch capability fault or publish a truthful state-store-collapse contract for the single-axis endpoint. - Rationale: A resource stop would overclaim `BACKEND_UNAVAILABLE` syntactic-only semantics. Owner: search reliability maintainer.
status: open

### DW-567: Distinguish a confirmed missing remote manifest from registry authentication, availability, and malformed-response failures before authorizing a container push.

origin: migrated from legacy ledger ("Story 26.3 Explicit Integration Deferrals (2026-07-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-container-publication-and-rollout-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-container-publication-and-rollout-verification.md` summary: Distinguish a confirmed missing remote manifest from registry authentication, availability, and malformed-response failures before authorizing a container push. evidence: The pre-existing publisher treats every nonzero `docker manifest inspect` exit as tag absence and pushes immediately, so an outage or authorization error can enter the blind retry path.
status: open

### DW-568: [MEDIUM] Add representative boundary coverage for the documented 512 MiB / approximately 100K-unit restore contract.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [MEDIUM] Add representative boundary coverage for the documented 512 MiB / approximately 100K-unit restore contract. evidence: The current fidelity integration restores three small units and `RedisChunkReadStreamTests` reads five mocked bytes; neither observes the endpoint ceiling, real multi-chunk staging, retention renewal, or high-cardinality workflow paging. Re-open trigger: restore-size/staging hardening or the first supported large-tenant recovery rehearsal.
status: open

### DW-569: Distinguish "manifest unknown" from transient errors in remote registry inspect so a transient failure cannot bypass the digest-conflict fail-closed guard before push.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md` summary: Distinguish "manifest unknown" from transient errors in remote registry inspect so a transient failure cannot bypass the digest-conflict fail-closed guard before push. evidence: publish-containers.ps1 treats any nonzero skopeo/docker remote inspect as "tag absent" and proceeds to push; behavior predates this story and was preserved as explicit spec parity, flagged by two independent review layers.
status: open

### DW-570: Probe skopeo availability in release-preflight so a missing runner binary fails before NuGet publish and tag creation instead of causing a partial release.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md` summary: Probe skopeo availability in release-preflight so a missing runner binary fails before NuGet publish and tag creation instead of causing a partial release. evidence: Container publish now hard-depends on the runner-preinstalled skopeo; the publish-time tooling-missing check fires only after NuGet packages and the release tag exist. The story spec froze "no touching preflight", so this hardening was out of scope.
status: open

### DW-571: Make independent-process benchmark reproducibility a permanent fail-closed CI comparison.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md` summary: Make independent-process benchmark reproducibility a permanent fail-closed CI comparison. evidence: Story 26.8 retained and compared two independent benchmark processes, but the nightly lane launches one process and its in-process reproducibility check cannot detect process-initialized drift. A future lane should normalize and compare two independently generated result payloads without weakening the existing gate.
status: open

### DW-572: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41. evidence: Blind review found non-canonical `Target artifacts:` and `Re-open/claim trigger:` labels, a validator that accepts incomplete debt metadata, and contradictory proposed/applied and open-action wording in the concurrent A41 artifacts; these issues predate and are outside the one-shot tracking correction.
status: open

### DW-573: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation. evidence: Blind review found that `architecture.md` describes a structured log file while `docs/dev/telemetry.md` documents console emission plus an operator-selected external pipeline; this unrelated documentation conflict predates the one-shot tracking correction.
status: done 2026-09-01
resolution: already resolved: docs/architecture.md:227 and docs/dev/telemetry.md:27 consistently define JSON console plus optional OTLP without an owned collector.

### DW-574: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md` summary: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal. evidence: Blind review found that the concurrent proposal both preserves and changes the unknown-ID error contract and omits the mixed GUID-N/GUID-D backend case that can suppress fallback; this is outside the one-shot tracking correction.
status: done 2026-09-01
resolution: already resolved: ConsistencyEndpointTests.cs:198-222 proves opaque identifier misses return 404 without GUID syntax.

### DW-575: Generalize positive route discovery beyond Program.cs and top-level `Endpoints/*Endpoints.cs` files so nested endpoint files, controllers, and differently named registration files cannot evade the route-surface guard.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md` summary: Generalize positive route discovery beyond Program.cs and top-level `Endpoints/*Endpoints.cs` files so nested endpoint files, controllers, and differently named registration files cannot evade the route-surface guard. evidence: Review confirmed the approved hardening preserves the pre-existing route-source scope; a future endpoint outside that scope would not enter the source-derived route count or exact-row tie.
status: open

### DW-576: Generalize the `/process` negative route scan across every production host and controller source so an unexpected route cannot evade the refutation guard by appearing outside the currently enumerated files.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md` summary: Generalize the `/process` negative route scan across every production host and controller source so an unexpected route cannot evade the refutation guard by appearing outside the currently enumerated files. evidence: Review confirmed the existing negative check reads Server Program/decomposed endpoint sources plus EventIngestionController only; broader production-source discovery predates and exceeds this contract-document hardening change.
status: open

### DW-577: Align the repository's OpenTelemetry core packages with the versions imported by the current Hexalith.Builds pointer so the exact working tree restores and builds again.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-consistency-inspect-opaque-id-contract.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-consistency-inspect-opaque-id-contract.md` summary: Align the repository's OpenTelemetry core packages with the versions imported by the current Hexalith.Builds pointer so the exact working tree restores and builds again. evidence: Exact-tree restore/build fails with NU1605 because Hexalith.Builds@8e0e2da imports OTLP exporter and hosting 1.17.0 while Directory.Packages.props pins OpenTelemetry core 1.16.0; the opaque-ID change neither caused nor is authorized to alter that concurrent dependency state.
status: done 2026-09-01
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:266-273 aligns OpenTelemetry core and exporters at 1.18.0.

### DW-578: Reconcile raw privacy-sensitive state on the preserved JSON-console and optional OTLP routes with the bounded lifecycle target.

origin: migrated from legacy ledger ("Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md
reason: - source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` summary: Reconcile raw privacy-sensitive state on the preserved JSON-console and optional OTLP routes with the bounded lifecycle target. evidence: Search and source-URI events can already expose raw query, subject, or source URI values through the existing logging routes. Story 27.1 documents that pre-existing deviation and sanitizes only the accepted Dapr lifecycle path; a later scope decision must choose sanitization before provider fan-out or explicit category exclusion from durable external routes.
status: open
decision: 2026-09-01 Sanitize before fan-out — Apply the lifecycle sanitizer before every provider and add privacy-negative tests.
decision: 2026-09-01 Sanitize before fan-out — Apply the lifecycle sanitizer before every provider and add privacy-negative tests.

### DW-579: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list.

origin: migrated from legacy ledger ("Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md
reason: - source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` summary: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list. evidence: The concurrent A41-entry rewrite in commit `8bb0708a` (sprint-change-proposal scope, not Story 27.1) replaced the old `Target artifact: docs/operations/rate-limiting.md and the future access-telemetry storage/purge implementation` line with a new target list that omits rate-limiting.md entirely, leaving that file's documentation obligation without a stated disposition. The fourth code-review pass of Story 27.1 (2026-07-17) surfaced the orphaned obligation; ownership belongs to the A41/Story 27.4 close-out coordination after Story 27.3 qualification, not this decision story.
status: open
decision: 2026-09-01 Restore obligation — Restore the document to A41 or Story 27.4 targets and reconcile it during close-out.
decision: 2026-09-01 Restore obligation — Restore the document to A41 or Story 27.4 targets and reconcile it during close-out.

### DW-580: Creation-lock release in `DaprAggregateCaseMappingStore.ReleaseCreationLockAsync` deletes unconditionally and can release a rival instance's active lock after the holder's TTL lease expired.

origin: migrated from legacy ledger ("Deferred from: code review of spec-infrastructure-dependency-abstraction (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md` summary: Creation-lock release in `DaprAggregateCaseMappingStore.ReleaseCreationLockAsync` deletes unconditionally and can release a rival instance's active lock after the holder's TTL lease expired. evidence: `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs:92-99` calls `DeleteStateAsync` without an owner token or ETag condition. Deferred as pre-existing: the prior Redis implementation used the same unconditional `DEL` after `SET NX`+TTL, so the migration preserved (did not introduce) this semantics; revisit if the F6 store design is reworked under the D1 review decision.
status: open

### DW-581: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major (F9).

origin: migrated from legacy ledger ("Deferred from: code review of spec-infrastructure-dependency-abstraction (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md` summary: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major (F9). evidence: See structured entry `IDA-F9-REDISPLACEHOLDER-REMOVAL` (appended 2026-08-09).
status: open

### DW-582: Reconcile the canonical project context's stale Aspire AppHost SDK version with the repository pin.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-retention-verification-operations-runbook-and-a41-close-out (2026-07-18)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md
reason: - source_spec: `_bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md` summary: Reconcile the canonical project context's stale Aspire AppHost SDK version with the repository pin. evidence: `_bmad-output/project-context.md` still names `Aspire.AppHost.Sdk/13.3.3`, while `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` and the reviewed story use the actual `13.4.6` SDK pin. The context drift predates Story 27.3; current source remains authoritative until its owning documentation lane repairs the canonical context.
status: open

### DW-583: Sibling helper `ContractDocumentGuard.cs`'s private `NormalizeLineEndings` has the same repeated-CR mishandling bug just fixed in `MarkdownContractDocument.cs`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-run-tests-and-fix-failures (2026-07-18)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md` summary: Sibling helper `ContractDocumentGuard.cs`'s private `NormalizeLineEndings` has the same repeated-CR mishandling bug just fixed in `MarkdownContractDocument.cs`. evidence: `tests/Hexalith.Memories.TestHelpers/Documentation/ContractDocumentGuard.cs:250-251` still uses the old `markdown.Replace("\r\n", "\n", ...).Replace('\r', '\n')` pattern (used by `FindLeakedToolCallMarkup`), which turns a doubled `\r` (e.g. `"\r\r\n"`) into an extra blank line instead of collapsing to one `\n` — the exact defect just fixed in the sibling file. Out of this spec's Code Map scope (which named only `MarkdownContractDocument.cs`); worth the same fix if the `\r\r\n` corruption risk is judged real for this file's consumers too.
status: open

### DW-584: Reconcile the `20.5-A41-ACCESS-TELEMETRY-RETENTION` entry's now-partially-resolved non-canonical labels with the earlier tracking entry that named them.

origin: migrated from legacy ledger ("Deferred from: code review of spec-run-tests-and-fix-failures (2026-07-18)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md` summary: Reconcile the `20.5-A41-ACCESS-TELEMETRY-RETENTION` entry's now-partially-resolved non-canonical labels with the earlier tracking entry that named them. evidence: An existing entry (source_spec `spec-one-shot-artifact-tracking.md`, "Reconcile the access-telemetry retention deferred-entry schema...") named both `Target artifacts:` and `Re-open/claim trigger:` as non-canonical labels needing reconciliation. This spec's fix renamed both labels on the `20.5-A41-ACCESS-TELEMETRY-RETENTION` entry to the canonical singular form, but the other issues that same tracking entry names (a validator accepting incomplete accepted-debt metadata, contradictory proposed/applied and open-action wording) remain unresolved. Flagging so the earlier entry isn't treated as fully obsolete, nor its label-naming half duplicated as new work.
status: open

### DW-585: The `[Collection(...)]` convention preventing cross-test pollution of `AccessTelemetryLifecycleMetrics`'s static counter is enforced only by an XML doc comment, not by tooling.

origin: migrated from legacy ledger ("Deferred from: code review of spec-run-tests-and-fix-failures (2026-07-18)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md` summary: The `[Collection(...)]` convention preventing cross-test pollution of `AccessTelemetryLifecycleMetrics`'s static counter is enforced only by an XML doc comment, not by tooling. evidence: `AccessTelemetryLifecycleMetricsTestCollection`'s doc comment states every test class touching the static `Records` counter via `MeterListener` "MUST be annotated" with the collection attribute, but nothing (analyzer or reflection-based guard test) verifies this. A third class added later that records to or listens on the counter without the attribute would silently reintroduce the exact flake this spec fixed, surfacing only as an intermittent CI failure. The same gap pre-exists for `Hexalith.Memories.Server.Tests`'s `TelemetryTestCollection`, suggesting a repo-wide guard test (e.g. reflection over `MeterListener` usages cross-checked against `[Collection]` attributes) would be the durable fix.
status: open

### DW-586: `MinimumDotnetSdkVersion` and its "10.0.302" user-facing message strings are duplicated as separate literals across multiple call sites instead of derived from one source of truth.

origin: migrated from legacy ledger ("Deferred from: code review of spec-run-tests-and-fix-failures (2026-07-18)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md` summary: `MinimumDotnetSdkVersion` and its "10.0.302" user-facing message strings are duplicated as separate literals across multiple call sites instead of derived from one source of truth. evidence: `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:27`'s `MinimumDotnetSdkVersion` constant and `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs:148`'s `"Install .NET SDK 10.0.302 or newer and retry."` string (plus other CLI message sites) each hardcode the version independently. This is the exact duplication pattern that caused the drift bug this spec fixed (the constant fell behind when messaging was bumped); deriving all user-facing strings from `MinimumDotnetSdkVersion.ToString()` would prevent recurrence, but is a refactor beyond this bugfix spec's scope.
status: open

### DW-587: Concurrency safety of the Dapr access-telemetry state store under actor-failover split-brain.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs
reason: - Concurrency safety of the Dapr access-telemetry state store under actor-failover split-brain. - ID: 27.3-CR1 - Status: carried-forward - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs - Re-open trigger: the pending C1 two-writer / partial-commit deployment probe runs against PG-ONPREM-1. - Rationale: WriteRecordAndIndexAsync/DeleteAndVerifyAsync have no retry loop and empty-etag FirstWrite on first bucket/catalog creation is last-write-wins, so a rare split-brain window could drop an expiry-index entry. Not reachable under the single global turn-based AccessTelemetryLifecycleActor in normal operation, and fail-closed (ETag conflict throws -> at-least-once retry; orphaned record bounded by its own TTL). The ADR assigns two-writer collision / partial-commit proof to the C1 deployment probe, not unit tests.
status: open

### DW-588: Live test-count totals recorded in the 27.3 Change Log predate current HEAD.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Live test-count totals recorded in the 27.3 Change Log predate current HEAD. - ID: 27.3-CR2 - Status: resolved 2026-07-26 — the live runner recount executes in this sandbox and was run again by dev-story. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: before Story 27.3 advances to done, or when any File List assembly changes. - Evidence: recount executed 2026-07-26 by dev-story on a clean tree at HEAD `a6753c11` with fresh Release builds (0 warnings, 0 errors). Pre-development: Server.Tests 2,190 / `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`, IntegrationTests 297 / `7836151bdf59ff8712f59911ed138a2f7afc792a7c4d2415c64122695c163856`, AccessTelemetry.Tests 55 / `973244b8ebcdfd55eeaf01ba56b8f33a1836aee158a98906817b5a5b2e3e60ef`, Cli.Tests 384 / `55e179bb6678fb671b1b342eeef71876b5f2f2c6106903c36507bb16769de312`. Command: `DiffEngine_Disabled=true dotnet exec <assembly> -list methods -noLogo | grep -E '^Hexalith\.'`. - Rationale: The entry's recorded figures (Server 2,188 / Integration 297 / AccessTelemetry 43) were stale: they predated the chunk-1 and chunk-2 review patches. The correct pre-development figures at HEAD `a6753c11` are recorded in the Evidence field above, and the post-development figures are in the 2026-07-26 `dev-story` Change Log row. The original claim that a full live runner recount "could not be executed in this sandbox" is false and is corrected here.
status: open

### DW-589: recordId charset is not validated before it is interpolated into the Dapr state key.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs
reason: - recordId charset is not validated before it is interpolated into the Dapr state key. - ID: 27.3-CR3 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs - Re-open trigger: a RecordId containing '/' or other key-delimiter characters can reach GetRecordKey/GetBucketKey. - Rationale: GetRecordKey builds `records/{shard}/{recordId}` and GetShard only guards null/whitespace; confirm the AccessTelemetryRecord contract constrains RecordId to a safe charset, otherwise add explicit validation.
status: done 2026-09-01
resolution: already resolved: DaprAccessTelemetryStateStore.cs:40 canonicalizes before key construction and AccessTelemetryCanonicalizer enforces uppercase Crockford ULIDs.

### DW-590: Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation. - ID: 27.3-CR4 - Status: resolved 2026-07-26 — the Server story/external attribution is restated and the recompute is done. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: before Story 27.3 advances to done / at the final-chunk code-review ledger reconciliation. - Evidence: the authoritative equation is `2,157 create + 6 Story 27.3 + 1 Story 31.1 + 26 external = 2,190`, recorded by the 2026-07-26 chunk-3b `code-review` Change Log row and restated in the 2026-07-26 `dev-story` row. It supersedes both the recorded `+1/+30` and this entry's own `+5/+26` target, which was correct only at Server 2,188. The `+1` is `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`, which follows Story 31.1 with `deploy/openbao/values.yaml` per the 2026-07-26 Administrator decision. Live discovery at HEAD `a6753c11` confirms Server 2,190 at hash `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`; the 2026-07-26 dev-story phase added no Server method, so the equation is unchanged after it. - Rationale: 4 Story-27.3 C1 methods (Adr_C1SourceEventMapping, Adr_C1TypedStateAndNullableMapping, Adr_C1QueryAndErrorMappings, Adr_ProductionAdapterQualification in AccessTelemetryRetentionDecisionTests.cs 6->10; plus ProductionDeploymentArtifactsTests +2) are booked under the +30 external delta rather than the +1 story delta. Recompute with live discovery (expected Server +5 story / +26 external) when the final review chunk finalizes the ledger. Administrator approved deferring the recompute to the final chunk on 2026-07-21.
status: done 2026-09-01
resolution: already resolved: Story 27.3 records the reconciled Server.Tests total as 2,190.

### DW-591: Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md
reason: - Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story. - ID: 27.3-CR5 - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 30 / Story 30.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3) - Target artifact: _bmad-output/planning-artifacts/epics.md - Re-open trigger: before Story 27.3 advances to done; the release/publish-pipeline work must own a separate story. - Evidence: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26-readiness-coherence-and-27-3-splits.md` (approved 2026-07-26) enumerates the ten transferred paths; `_bmad-output/planning-artifacts/epics.md` carries Epic 30 / Story 30.1; `_bmad-output/implementation-artifacts/sprint-status.yaml` registers `30-1-...` as `backlog`; Story 27.3's File List no longer declares any of the ten paths. - Rationale: The four-image publish/partial-recovery pipeline (CiTestInventoryTests.cs + tests/tooling/publish_containers/*) is independently demonstrable and was ledgered as an external CI/CD lane, yet is bundled into the single C1 adapter-qualification slice. Administrator approved splitting it into a new story via correct-course on 2026-07-21; 27.3's File List and ledger shrink to adapter/qualification scope.
status: done 2026-09-01
resolution: already resolved: epics.md and sprint-status.yaml register the Epic 30 release/publish stories.

### DW-592: Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`. - ID: 27.3-CR18 - Status: resolved 2026-07-26 — chunk 3a and chunk 3b are now reviewed and the live recount ran successfully; the surviving obligations are DW 27.3-CR4 (Server attribution) and the open review action items. (`superseded` is not one of the register's four documented statuses; corrected to `resolved` on 2026-07-26 by dev-story.) - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: superseded; do not reopen under this ID. - Evidence: chunk 3a (2026-07-26, HEAD `159d7216`) and chunk 3b (2026-07-26, HEAD `c9dfb06f`) are both recorded in **Code Review Evidence** of the story file, and the live recount executed successfully at `159d7216` (Server 2,190 / IntegrationTests 297 / AccessTelemetry.Tests 55). The surviving obligation is tracked separately as DW 27.3-CR4.
status: done 2026-09-01
resolution: already resolved: Story 27.3 records completed chunk-3a/chunk-3b review and a successful live recount.

### DW-593: Clock NetworkPolicy egress to TCP/443 is unrestricted (no `to:`); tighten to real UTC-source CIDRs before enablement.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: deploy/kubernetes/base/access-telemetry-network-policy.yaml
reason: - Clock NetworkPolicy egress to TCP/443 is unrestricted (no `to:`); tighten to real UTC-source CIDRs before enablement. [deploy/kubernetes/base/access-telemetry-network-policy.yaml:99] - ID: 27.3-CR19 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: deploy/kubernetes/base/access-telemetry-network-policy.yaml - Re-open trigger: before Production lifecycle enablement; the clock egress must be restricted to the real UTC-source CIDRs once the three `.example.invalid` authorities are replaced. - Rationale: The clock NetworkPolicy allows egress on TCP/443 with no `to:` selector, so the trusted-time workload can reach any address on the internet. The real UTC-source CIDRs are not knowable while all three configured authorities are `.example.invalid` placeholders, so narrowing the rule now would encode a fiction. Owner: clock-authority owner. Consequence: an unrestricted egress path exists on a workload that is scaled to zero and fail-closed.
status: open
decision: 2026-09-01 Supply restricted authorities — Provide approved endpoints and CIDRs, restrict egress, and add manifest guards.
decision: 2026-09-01 Supply restricted authorities — Provide approved endpoints and CIDRs, restrict egress, and add manifest guards.

### DW-594: `maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: deploy/kubernetes/base/dapr/access-telemetry-store.yaml:25
reason: - ~~`maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.~~ [deploy/kubernetes/base/dapr/access-telemetry-store.yaml:25] — **resolved 2026-07-26 (dev-story)**: `maxConns` lowered to `40`, so `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93 <= max_connections 100`. The derivation is a comment on the metadata entry and is enforced by the new `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`, which failed RED at `141 > 100` before the fix.
status: done 2026-07-26
resolution: story)**: `maxConns` lowered to `40`, so `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93 <= max_connections 100`. The derivation is a comment on the metadata entry and is enforced by the new `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`, which failed RED at `141 > 100` before the fix.

### DW-595: Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs
reason: - ~~Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.~~ [tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs] — **resolved 2026-07-26 (dev-story)**: all six surfaces are now bound by `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound`. Because the guard passed on first authoring, it was mutation-proven rather than RED-proven: six independent drift injections (`skipVerify:"true"`, `actorStateStore:"false"`, dropped `hostnossl ... reject`, RBAC verbs widened to `get,list`, ACL `defaultAction: allow`, and an extra secret smuggled into `allowedSecrets`) each failed the suite, and the baseline returned green after every revert.
status: done 2026-07-26
resolution: story)**: all six surfaces are now bound by `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound`. Because the guard passed on first authoring, it was mutation-proven rather than RED-proven: six independent drift injections (`skipVerify:"true"`, `actorStateStore:"false"`, dropped `hostnossl ... reject`, RBAC verbs widened to `get,list`, ACL `defaultAction: allow`, and an extra secret smuggled into `allowedSecrets`) each failed the suite, and the baseline returned green after every revert.

### DW-596: Pre-enablement operational hardening: probe `wget`/`sh` dependency, missing metrics ingress, startup-vs-initTimeout cold-start race, terminationGracePeriod/PDB node-drain block, manual restart on password/CA rotation.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: deploy/kubernetes/base/access-telemetry-deployments.yaml
reason: - Pre-enablement operational hardening: probe `wget`/`sh` dependency, missing metrics ingress, startup-vs-initTimeout cold-start race, terminationGracePeriod/PDB node-drain block, manual restart on password/CA rotation. [deploy/kubernetes/base/access-telemetry-deployments.yaml] - ID: 27.3-CR20 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: deploy/kubernetes/base/access-telemetry-deployments.yaml - Re-open trigger: before Production lifecycle enablement; each named hardening item must be resolved or accepted with an owner. - Rationale: Five independent pre-enablement items on a workload that is scaled to zero: the exec probes require `wget` and `/bin/sh` in images that are still `:0.0.0` placeholders; there is no metrics/monitoring ingress, so Prometheus reports NoData; the 60s startup probe races the store's `initTimeout: 1m` on a cold start; `terminationGracePeriodSeconds: 120` plus a `minAvailable: 1` PDB on a single replica blocks node drain; and DB/OpenBao password and TLS-CA rotation need a manual sidecar restart because HotReload is off for an actor state store. Owner: Hexalith Platform Operations. Consequence: none today (replicas are zero); each becomes live at enablement.
status: open

### DW-597: Docs: release-runbook four-image expansion belongs with DW 27.3-CR5; ADR byte-bucket boundary overlap and `edgeTypeCount>16` ambiguity; verify ADR `Story 27.2 C1 mapping` block attribution.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md
reason: - Docs: release-runbook four-image expansion belongs with DW 27.3-CR5; ADR byte-bucket boundary overlap and `edgeTypeCount>16` ambiguity; verify ADR `Story 27.2 C1 mapping` block attribution. [docs/dev/release-runbook.md; docs/dev/adr-27.1-001-access-telemetry-lifecycle.md] - ID: 27.3-CR21 - Status: open (release-runbook arm transferred to Story 30.1 on 2026-07-26 and reassigned to Story 30.3 on 2026-07-27 by the approved Sprint Change Proposal 2026-07-27, which owns the four-image expansion of `docs/dev/release-runbook.md`; Story 30.5 owns its cutover and rollback sections. The ADR arms remain Story 27.3's) - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md - Re-open trigger: before Story 27.3 advances to done; the ADR byte-bucket boundary overlap and the `edgeTypeCount>16` clamp-vs-reject ambiguity must be resolved. - Rationale: Two documentation defects in the ADR that no code reads today: adjacent byte-bucket labels overlap at their 64KiB/1MiB/10MiB boundaries, so a value exactly on a boundary has two valid labels, and the behaviour for `edgeTypeCount > 16` is unspecified between clamping and rejecting. The `Story 27.2 C1 mapping` block's attribution also needs confirmation. Owner: ADR owner. Consequence: an implementer reading the ADR can pick either reading; no shipped code depends on the ambiguity yet. The release-runbook arm of this entry transferred to Story 30.1 on 2026-07-26.
status: open

### DW-598: 27.3-CR23: chunk 3b unreviewed; fail-closed for `done`. (Renumbered 2026-07-26 by code review, chunk 3b: this entry was minted as `27.3-CR7`, colliding with the existing `DW 27.3-CR7` create-story-verifier entry. Both were open and both were cited as `done` blockers, so the ID resolved to two unrelated obligations. Resolved 2026-07-26: chunk 3b has now been reviewed; this entry is closed by that review. Renumbered again 2026-07-27 by code review, chunk 3: a later, unrelated entry — the AC6/C2 production-deployment-verification red-run record — was independently minted as `27.3-CR17`, recreating the identical collision this entry was renumbered once already to escape. This entry, being resolved and historical, is renumbered to `27.3-CR18` rather than the active, currently-cited `27.3-CR17`. Renumbered a third time 2026-07-27 by dev-story: `27.3-CR18` was itself already taken by the resolved chunk-2 fail-closed-blockers entry above, so that renumber recreated the very collision it was performing. This entry moves to `27.3-CR23`, the first free ID; the pre-existing `27.3-CR18` above keeps the ID it held first. The recurrence is now bound by `CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`, which failed RED on this exact duplicate.) The eight governance/planning record

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
reason: - **27.3-CR23 - chunk 3b unreviewed; fail-closed for `done`.** (Renumbered 2026-07-26 by code review, chunk 3b: this entry was minted as `27.3-CR7`, colliding with the existing `DW 27.3-CR7` create-story-verifier entry. Both were open and both were cited as `done` blockers, so the ID resolved to two unrelated obligations. Resolved 2026-07-26: chunk 3b has now been reviewed; this entry is closed by that review. **Renumbered again 2026-07-27 by code review, chunk 3:** a later, unrelated entry — the AC6/C2 production-deployment-verification red-run record — was independently minted as `27.3-CR17`, recreating the identical collision this entry was renumbered once already to escape. This entry, being resolved and historical, is renumbered to `27.3-CR18` rather than the active, currently-cited `27.3-CR17`. **Renumbered a third time 2026-07-27 by dev-story:** `27.3-CR18` was itself already taken by the resolved chunk-2 fail-closed-blockers entry above, so that renumber recreated the very collision it was performing. This entry moves to `27.3-CR23`, the first free ID; the pre-existing `27.3-CR18` above keeps the ID it held first. The recurrence is now bound by `CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`, which failed RED on this exact duplicate.) The eight governance/planning record - ID: 27.3-CR23 - Status: resolved 2026-07-26 — chunk 3b reviewed; all in-scope chunks are now complete. - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md - Re-open trigger: resolved; a new chunk would need a new ID. - Evidence: the chunk-3b record in **Code Review Evidence** of the story file (8 governance/planning paths, 2,264 diff lines at HEAD `c9dfb06f`, manifest SHA-256 `605152e597357936680f5f171d9a87e09dfcb7887e21ecd953bfab6d550d6344`), which states that chunk 3b completes path-level review coverage. paths of Story 27.3 (story file, `epics.md`, `architecture.md`, the 2026-07-20 sprint change proposal, `deferred-work.md`, `sprint-status.yaml`, and the create-scope and adapter-profile evidence packets; 2,076 diff lines) have not been reviewed. Per `story-phase-ledger.md`, an intermediate chunk may emit findings but cannot finalize the ledger or synchronize completion status. Owner: Story 27.3 review owner. Consequence: the final `code-review` row cannot be appended and Story 27.3 cannot reach `done`. Reopen trigger: run code review over the chunk-3b path set, then append the final row carrying evidence that all three chunks are complete.
status: done 2026-09-01
resolution: already resolved: Story 27.3 records the completed chunk-3b review closing 27.3-CR23.

### DW-599: 27.3-CR8: test-double state store ships in the product container assembly.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs
reason: - **27.3-CR8 - test-double state store ships in the product container assembly.** - ID: 27.3-CR8 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs - Re-open trigger: when the Story 27.2-origin structural move to a shared test-support project is scheduled. - Rationale: The class documents itself as a test double the runtime host does not register, yet it ships inside a project with `EnableContainer=true` and `ContainerRepository memories-access-telemetry`, reachable only through `InternalsVisibleTo`. A DI misregistration would satisfy `IAccessTelemetryStateStore` with no durability. Moving it to a shared test-support project is a Story 27.2-origin structural change. Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released image's assembly surface. Partially mitigated 2026-07-26 by dev-story: the adapter now validates `ttlInSeconds`, models the anti-resurrection conflict, prunes drained expiry minutes, and performs the same strong post-delete verification as the Dapr adapter, so a misregistration no longer silently discards expiry - but the structural placement is unchanged. `src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs` documents itself as a deterministic adapter for lifecycle tests that the runtime host does not register, yet it lives in a project with `EnableContainer=true` and `ContainerRepository` `memories-access-telemetry`, reachable only via `InternalsVisibleTo` from the two test assemblies. It is non-durable, non-transactional, and discards `ttlInSeconds`, so a DI misregistration would satisfy `IAccessTelemetryStateStore` with no durability and no expiry. Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released image's assembly surface. Reopen trigger: move it to a shared test-support project, or add a guard asserting the runtime composition root never registers it. Pre-existing: introduced by Story 27.2, not by this chunk.
status: open

### DW-600: 27.3-CR9: commit `358bef35` bypasses the Conventional Commits contract. Its subject carries

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: .githooks / commitlint configuration
reason: - **27.3-CR9 - commit `358bef35` bypasses the Conventional Commits contract.** Its subject carries - ID: 27.3-CR9 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: .githooks / commitlint configuration - Re-open trigger: confirm the commit-msg hook rejects a missing type prefix, and decide whether the omitted product change needs a follow-up release note. - Rationale: Commit `358bef35` carries no Conventional Commits type prefix, and its body lists only the three new test files while omitting the `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. It is already published on `main`, so correcting the message needs a history rewrite; the durable fix is the commit-msg gate, not this commit. Owner: repository workflow owner. Consequence: release semantics and the changed-surface audit trail are both wrong for that one commit. no type prefix and its body enumerates only the three new test files, omitting the `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. The commit is already published on `main`, so correcting the message itself would require a history rewrite. Owner: repository workflow owner. Consequence: `feat`/`fix` release semantics and the changed-surface audit trail are both wrong for that commit. Reopen trigger: confirm the commit-msg hook rejects a missing type prefix, and record whether the omitted product change needs a follow-up release note.
status: open
decision: 2026-09-01 Add follow-up note — Add a release-note correction and regression-test the commit-msg gate.
decision: 2026-09-01 Add follow-up note — Add a release-note correction and regression-test the commit-msg gate.

### DW-601: The readiness gate does not require completion dates for evidence rows at review or done.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate does not require completion dates for evidence rows at review or done. evidence: `check_evidence_rows` reads only the status-column index, while the phase-ledger policy requires completed evidence to carry a date and forbids dateless rows at done.
status: open

### DW-602: The readiness gate accepts a bare blocked evidence status without accountability metadata.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate accepts a bare blocked evidence status without accountability metadata. evidence: Any non-pending value bypasses C6, so `blocked` passes without the policy-required owner, consequence, and reopen trigger.
status: open

### DW-603: The readiness gate accepts unknown evidence statuses as completed.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate accepts unknown evidence statuses as completed. evidence: C6 rejects only values in `PENDING_CELLS`, so a typo or invented state such as `gibberish` passes without a recognized completed-state vocabulary.
status: open

### DW-604: The readiness gate silently ignores evidence rows whose review-status cell is absent.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate silently ignores evidence rows whose review-status cell is absent. evidence: `check_evidence_rows` continues when `status_index` exceeds the row length instead of reporting the missing status required by policy.
status: open

### DW-605: Readiness File List reconciliation checks changed-but-unlisted paths but not listed-but-unchanged paths.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness File List reconciliation checks changed-but-unlisted paths but not listed-but-unchanged paths. evidence: C1 builds only an `unlisted` set even though the phase-ledger policy requires the cumulative File List and changed set to contain identical entries.
status: open

### DW-606: Readiness File List entries are treated as globs instead of exact historical paths.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness File List entries are treated as globs instead of exact historical paths. evidence: C1 calls `matches_glob` for File List entries, allowing a broad entry such as `src/**` to replace the policy-required path-level inventory.
status: open

### DW-607: Cumulative readiness diff collection loses the source path of renames.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Cumulative readiness diff collection loses the source path of renames. evidence: `derive_cumulative_changed` uses `git diff --name-only`, while policy requires a rename entry to identify both old and new paths.
status: open

### DW-608: A review or done artifact can pass C2 without a create-story ledger row.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A review or done artifact can pass C2 without a create-story ledger row. evidence: `check_ledger` requires `dev-story` for review/done and `code-review` for done but never requires the creation baseline mandated by policy.
status: open

### DW-609: The newest reconciliation cell accepts unstructured blocker substrings.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The newest reconciliation cell accepts unstructured blocker substrings. evidence: C2 accepts any cell containing markers such as `not run` or `blocker` without validating the required command, owner, consequence, and reopen trigger.
status: open

### DW-610: A review artifact can carry governed File List or evidence data but no phase ledger and still pass.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A review artifact can carry governed File List or evidence data but no phase ledger and still pass. evidence: When `find_ledger` returns none, validation emits a skipped note instead of enforcing the ledger required at review or done.
status: open

### DW-611: An in-progress or review artifact can omit its File List and still pass readiness.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: An in-progress or review artifact can omit its File List and still pass readiness. evidence: When `parse_section_paths` returns none, validation emits a skipped note instead of enforcing the core cumulative completeness input.
status: open

### DW-612: The readiness gate does not enforce chronological order of canonical ledger phases.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate does not enforce chronological order of canonical ledger phases. evidence: `check_ledger` records phase presence but never compares canonical phase indices, so an impossible lifecycle order can pass.
status: open

### DW-613: Canonical ledger rows can retain placeholder Date or Change cells.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Canonical ledger rows can retain placeholder Date or Change cells. evidence: C2 checks placeholders only in Test count and File List reconciliation despite the canonical five-column record requiring Date and Change too.
status: open

### DW-614: A whitespace-only exclusion owner is accepted by the readiness parser.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A whitespace-only exclusion owner is accepted by the readiness parser. evidence: `parse_exclusions` checks only that the owner regex matched and strips the captured value without rejecting an empty result.
status: open

### DW-615: Readiness hook and CI adoption lack an executable entry-point contract test.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness hook and CI adoption lack an executable entry-point contract test. evidence: Existing tests exercise the CLI and policy prose but do not pin the hook's `--derive-cumulative` invocation or CI's prepared changed-file invocation.
status: open

### DW-616: The local commitlint hook fails open when repository dependencies are absent.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The local commitlint hook fails open when repository dependencies are absent. evidence: `.githooks/commit-msg` prints an installation hint but exits successfully when `node_modules/.bin/commitlint` is unavailable, contrary to mandatory local validation policy.
status: open

### DW-617: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy. evidence: `commitlint.config.mjs` does not disable default ignores, so commitlint's built-in ignored message shapes are outside the configured Conventional Commit rules.
status: open

### DW-618: Main-push commitlint runs can cancel an earlier range before it is validated.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Main-push commitlint runs can cancel an earlier range before it is validated. evidence: The workflow groups all main pushes together with `cancel-in-progress: true`, while each reusable run validates only its event-specific before-to-after range.
status: open

### DW-619: Contributor guidance still recommends the forbidden chore branch and commit type.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Contributor guidance still recommends the forbidden chore branch and commit type. evidence: `CONTRIBUTING.md` contains `chore/<short-name>` and `chore:` examples while the shared baseline and current commitlint type enum forbid `chore`.
status: open

### DW-620: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification. evidence: Repository tests do not execute the hook against an invalid message, reject forbidden message shapes through the pinned config, or contract-test PR-title edit wiring.
status: open

### DW-621: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests. evidence: The new header- and body-phase catches handle `OperationCanceledException`, but tests throw only the narrower `TaskCanceledException` unless caller cancellation is requested.
status: done 2026-09-01
resolution: already resolved: TenantsRestQueryClientTests.cs:911 and :929 cover plain non-caller OperationCanceledException at header and body reads and map it to timeout.

### DW-622: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage. evidence: REST-client tests stop at the `InvalidCursor` enum and gateway tests inject an already mapped `invalid-cursor` exception, leaving `ToReasonCode` disconnected from page-one recovery tests.
status: done 2026-09-01
resolution: already resolved: TenantsRestQueryClientTests.cs:468 distinguishes invalid cursors and TenantQueryGatewayTests.cs:2362 verifies typed transport mapping before recovery.

### DW-623: Independent tenant-detail and member read fault containment lacks page-level verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Independent tenant-detail and member read fault containment lacks page-level verification. evidence: The page now maps each faulted initial read to its own unavailable state, but existing tests cover pending success and cancellation rather than either read faulting independently.
status: done 2026-09-01
resolution: already resolved: TenantDetailSurfaceTests.cs:2378 verifies each independently faulted initial detail/member read while the sibling is still observed.

### DW-624: The Tenants member paging state machine has only happy-path coverage.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The Tenants member paging state machine has only happy-path coverage. evidence: Existing tests do not verify retained previous data, invalid-cursor recovery, failed-page state preservation, the 50-entry history cap, or navigation from an empty later page.
status: done 2026-09-01
resolution: already resolved: TenantDetailSurfaceTests.cs covers the 50-row cap, invalid-cursor recovery, and failed refresh retaining confirmed rows.

### DW-625: Failed refresh-subscription retry and duplicate-subscription protection lack verification.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Failed refresh-subscription retry and duplicate-subscription protection lack verification. evidence: Tests do not assert `Empty.IsSubscribed` is false followed by successful retry, nor exercise the audit page's in-flight setup guard under overlapping parameter passes.
status: done 2026-09-01
resolution: already resolved: TenantReadRefreshSubscriptionTests.cs:254-268 proves failed leases are unsubscribed and retry succeeds; TenantAuditPageTests.cs:167 covers overlapping setup.

### DW-626: The Tenants command-side URI scheme gate lacks non-HTTP composition tests.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The Tenants command-side URI scheme gate lacks non-HTTP composition tests. evidence: Existing malformed-scheme theories vary only `Tenants:BaseAddress` while retaining a valid EventStore address, so command-gateway fallback is not pinned.
status: done 2026-09-01
resolution: already resolved: TenantsUiCompositionTests.cs:622-640 covers non-HTTP and malformed EventStore base addresses.

### DW-627: Contributor coverage guidance still describes six projects although the authoritative Docker-free inventory contains seven.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md` summary: Contributor coverage guidance still describes six projects although the authoritative Docker-free inventory contains seven. evidence: The mismatch predates this fix; `tests/README.md` and `CONTRIBUTING.md` omit AccessTelemetry while `tools/test-projects.unit-contract.txt` and `requiredReportProjects` include it.
status: open

### DW-628: Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
location: n/a
reason: - Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.
status: done 2026-09-01
resolution: already resolved: Story 27.3 line 625 explicitly records both review chunks complete.

### DW-629: `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
location: tools/check-story-review-readiness.py
reason: - `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.
status: done 2026-09-01
resolution: already resolved: tools/check-story-review-readiness.py:851-855 rejects empty changed sets with a clear fail-closed message.

### DW-630: The C6 evidence-row gate can be satisfied by deleting rows rather than proving them. The 2026-07-30 correction moved the C1 umbrella row from `pending` to `complete` and deleted twelve `pending` child-gate rows, clearing thirteen mechanical C6 blockers by record edit alone; the gate now sees one evidence table where the story carried two. Not a defect of this story — the transfer is Administrator-approved and `epics.md` now carries a stronger 25-row table with a consequence-and-reopen-trigger column the deleted table lacked, so the record was moved, not erased. Owner: story-gate tooling. Re-open trigger: when `check-story-review-readiness.py` is next revised, make a `complete` completion state distinguishable from an administrative scope transfer that proved nothing.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
location: n/a
reason: - The C6 evidence-row gate can be satisfied by deleting rows rather than proving them. The 2026-07-30 correction moved the C1 umbrella row from `pending` to `complete` and deleted twelve `pending` child-gate rows, clearing thirteen mechanical C6 blockers by record edit alone; the gate now sees one evidence table where the story carried two. Not a defect of this story — the transfer is Administrator-approved and `epics.md` now carries a stronger 25-row table with a consequence-and-reopen-trigger column the deleted table lacked, so the record was moved, not erased. Owner: story-gate tooling. Re-open trigger: when `check-story-review-readiness.py` is next revised, make a `complete` completion state distinguishable from an administrative scope transfer that proved nothing.
status: open

### DW-631: Reconcile the documented approximately five-minute `integration-fast` budget with observed 15–20 minute executions.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md` summary: Reconcile the documented approximately five-minute `integration-fast` budget with observed 15–20 minute executions. evidence: The final exact selector passed in 15m38s and earlier unchanged broad runs took 19–20 minutes, while `tests/README.md` still defines this lane as an approximately five-minute budget; the discrepancy predates and is not caused by the OpenBao/MCP stabilization patch.
status: open
decision: 2026-09-01 Adopt measured budget — Benchmark current runs and update docs, workflow timeouts, and naming to the measured budget.
decision: 2026-09-01 Adopt measured budget — Benchmark current runs and update docs, workflow timeouts, and naming to the measured budget.

### DW-632: Make the normal story-readiness gate require an executed C0 receipt and reviewer-owned command evidence before accepting review or done.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-27-2-lifecycle-checkpoint-gaps-cr42-cr46.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-27-2-lifecycle-checkpoint-gaps-cr42-cr46.md` summary: Make the normal story-readiness gate require an executed C0 receipt and reviewer-owned command evidence before accepting review or done. evidence: The current readiness gate can accept a story whose C0 remains blocked because it validates declared paths, status vocabulary, sprint-status agreement, and evidence-table row state, but does not mechanically require this receipt row or prove that its recorded commands executed; independent review remains the fail-closed control.
status: open

### DW-633: Fix integration-fast Dapr actor Connection refused failures from CI run 30990821240 (rate limiting + tenant configuration tests against 127.0.0.1:35131).

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: n/a
reason: - source_spec: none summary: Fix integration-fast Dapr actor Connection refused failures from CI run 30990821240 (rate limiting + tenant configuration tests against 127.0.0.1:35131). evidence: Split from the CI fix intent so BMAD customization fixture drift can ship independently without waiting on sidecar/harness diagnosis.
status: open

### DW-634: Refresh historical governance docs that still cite `_bmad/custom/bmad-generate-project-context.toml` after the skill rename.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-30990821240-bmad-customization-fixtures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-30990821240-bmad-customization-fixtures.md` summary: Refresh historical governance docs that still cite `_bmad/custom/bmad-generate-project-context.toml` after the skill rename. evidence: Cross-tenant carry-forward and related process notes still document the deleted generate custom path and generate-skill verification commands; this fixture fix deleted that path without updating those historical references.
status: open

### DW-635: Historical container rebuilds are not bit-identical, so re-running Recover Partial Release after a successful image push hits immutable digest conflicts with no release-only workflow path.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: Historical container rebuilds are not bit-identical, so re-running Recover Partial Release after a successful image push hits immutable digest conflicts with no release-only workflow path. evidence: 2.6.5 first push succeeded then evidence failed; second run conflicted on config digest. 2.6.0-2.6.4 Releases had to be completed offline from already-present remote tags.
status: open

### DW-636: Workflow 2-or-4 Server/MCP evidence gates are only source-text pinned in CiTestInventoryTests, not executed as PowerShell.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: Workflow 2-or-4 Server/MCP evidence gates are only source-text pinned in CiTestInventoryTests, not executed as PowerShell. evidence: Verification-gap review showed deleting the hasServer/hasMcp throw while leaving pinned substrings would keep CiTestInventoryTests green.
status: open

### DW-637: PARTIAL PUBLISH incidents for 2.6.0-2.6.7 were already closed before evidence-backed recovery completed.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: PARTIAL PUBLISH incidents for 2.6.0-2.6.7 were already closed before evidence-backed recovery completed. evidence: Issues #22-#33 show closedAt before the 2026-08-08 recovery; complete-partial-release only closes open issues.
status: open

### DW-638: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test. evidence: Review found the registered 24.6 evidence contract omits GraphScopedSearchIntegrationTests, SyntacticSearchIntegrationTests, and SemanticSearchIntegrationTests that Dev Notes and Planned Verification require citing.
status: open
decision: 2026-09-01 Add axis-specific evidence — Add three axis-specific integration classes or equivalent cases without weakening the all-axis proof.
decision: 2026-09-01 Add axis-specific evidence — Add three axis-specific integration classes or equivalent cases without weakening the all-axis proof.

### DW-639: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement. evidence: The approved proposal says “all available values agree” while the registered story and epics.md require “all three values agree,” leaving undefined behavior when one index dimension is missing.
status: open
decision: 2026-09-01 Fail closed — Return incomplete verification whenever any required FT.INFO dimension is absent and test each field.
decision: 2026-09-01 Fail closed — Return incomplete verification whenever any required FT.INFO dimension is absent and test each field.

### DW-640: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics. evidence: Edge-case review showed proven-active hashes with empty/whitespace tenantId are unclassified and can be mislabeled as foreign contamination.
status: open
decision: 2026-09-01 Treat as missing marker — Classify blank tenantId as incomplete structural evidence and use non-destructive remediation.
decision: 2026-09-01 Treat as missing marker — Classify blank tenantId as incomplete structural evidence and use non-destructive remediation.

### DW-641: Add a retrospective addendum or reopen note when epic-24 returns to in-progress after epic-24-retrospective is done for Stories 24.6-24.9.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Add a retrospective addendum or reopen note when epic-24 returns to in-progress after epic-24-retrospective is done for Stories 24.6-24.9. evidence: sprint-status.yaml reopens epic-24 while leaving epic-24-retrospective done with no addendum covering the residual backlog registration.
status: open
decision: 2026-09-01 Append dated addendum — Add a dated correction with links to the governing artifacts and preserve the original record.
decision: 2026-09-01 Append dated addendum — Add a dated correction with links to the governing artifacts and preserve the original record.

### DW-642: Align Story 24.6 accepted-blocker schema so “proof boundary” is required consistently across proposal and registered story/epics text.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Align Story 24.6 accepted-blocker schema so “proof boundary” is required consistently across proposal and registered story/epics text. evidence: Registered AC4 requires a proof boundary field while the matching proposal AC omits it.
status: open
decision: 2026-09-01 Require proof boundary — Add the field to the proposal schema, migrate the entry, and guard it in validation.
decision: 2026-09-01 Require proof boundary — Add the field to the proposal schema, migrate the entry, and guard it in validation.

### DW-643: Name the concrete RedisEmbeddingMigrationStoreTests method required by Story 24.8 Cross-Tenant Negative Evidence.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Name the concrete RedisEmbeddingMigrationStoreTests method required by Story 24.8 Cross-Tenant Negative Evidence. evidence: Planned Verification commands the migration store tests without a named assertion contract in the evidence table.
status: open
decision: 2026-09-01 Name exact method — Record the exact RedisEmbeddingMigrationStoreTests method and verify the command selects it.
decision: 2026-09-01 Name exact method — Record the exact RedisEmbeddingMigrationStoreTests method and verify the command selects it.

### DW-644: Add a reciprocal pointer from docs/operations/route-surface.md to the new directory-ingestion authoritative contract.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md` summary: Add a reciprocal pointer from docs/operations/route-surface.md to the new directory-ingestion authoritative contract. evidence: Directory guidance links to route-surface as the prior route home, but route-surface is outside this story File Scope and gained no back-link.
status: open

### DW-645: Reconcile AccessTelemetry C1 ownership guard text with Story 27.21 C1.15 registration and the frozen Never/test File Scope carve-out.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md` summary: Reconcile AccessTelemetry C1 ownership guard text with Story 27.21 C1.15 registration and the frozen Never/test File Scope carve-out. evidence: This story co-shipped AccessTelemetryRetentionDecisionTests pinning unowned C1 text; later 27.21 registration and planning copies partially supersede that pin without updating the guard.
status: open
decision: 2026-09-01 Update the guard — Recognize Story 27.21 and C1.15 ownership while preserving the frozen-artifact carve-out.
decision: 2026-09-01 Update the guard — Recognize Story 27.21 and C1.15 ownership while preserving the frozen-artifact carve-out.

### DW-646: OpenBao root/unseal/scoped tokens and KV field values still ride kubectl exec / bao argv during disposable bootstrap.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md` summary: OpenBao root/unseal/scoped tokens and KV field values still ride kubectl exec / bao argv during disposable bootstrap. evidence: Review found BAO_TOKEN= and key=value on process argv; Protect-EvidenceText redacts evidence output but cannot hide argv from node process lists or kubectl audit trails without redesigning away from bao CLI over kubectl exec.
status: open

### DW-647: Disposable OpenBao namespace objects and unseal material are not retired by OpenBao helper code beyond local work-dir deletion.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md` summary: Disposable OpenBao namespace objects and unseal material are not retired by OpenBao helper code beyond local work-dir deletion. evidence: Review noted only the local TLS/work directory is deleted; cluster-scoped OpenBao Deployment/Secrets/ConfigMaps and unseal keys rely on kind cluster teardown rather than an explicit OpenBao cleanup stage.
status: open

### DW-648: Protect-EvidenceText token regex can redact ordinary s./b./r. substrings in diagnostics.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md` summary: Protect-EvidenceText token regex can redact ordinary s./b./r. substrings in diagnostics. evidence: Blind-hunter noted `\b(?:hvs|hvb|hvr|s|b|r)\.[A-Za-z0-9_-]{16,}\b` is broader than OpenBao/Vault token shapes and can over-redact unrelated diagnostics.
status: open

### DW-649: Disposable OpenBao container still runs with readOnlyRootFilesystem false despite emptyDir data volume.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md` summary: Disposable OpenBao container still runs with readOnlyRootFilesystem false despite emptyDir data volume. evidence: Edge/hardening review; securityContext is otherwise hardened but root filesystem writability remains a residual disposable-verifier hardening gap.
status: open

### DW-650: Full stubbed-kubectl execution suite for OpenBao init/unseal/KV/policy/seed/token paths is not unit-tested beyond source pins and kind e2e.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md` summary: Full stubbed-kubectl execution suite for OpenBao init/unseal/KV/policy/seed/token paths is not unit-tested beyond source pins and kind e2e. evidence: Verification-gap review; kind verification exercised the live path, but Confirm/Get-HealthResponse-style stub execution coverage was not added for every OpenBao helper function.
status: open

### DW-651: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major once no external consumer depends on it (F9).

origin: migrated from legacy ledger ("Infrastructure-Dependency Abstraction (IDA) Deferred (2026-08-09)"), 2026-09-01
location: src/Hexalith.Memories.Redis/RedisPlaceholder.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md` summary: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major once no external consumer depends on it (F9). - ID: IDA-F9-REDISPLACEHOLDER-REMOVAL - Status: open - Source story: spec-infrastructure-dependency-abstraction - Target artifact: src/Hexalith.Memories.Redis/RedisPlaceholder.cs - Re-open trigger: an owned breaking major of the Redis package is cut, or an external consumer audit confirms zero remaining references to `DefaultRedisPort` / `DefaultFalkorDbPort`. - Rationale: Constants are compile-time compat only (open no connections); removal is deferred to avoid an unforced package break while F9 already labels them non-leak.
status: open

### DW-652: Runbook proof text is duplicated across two operator documents.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: docs/operations/route-surface.md
reason: - **Runbook proof text is duplicated across two operator documents.** - ID: 24.6-CR-W1 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: docs/operations/route-surface.md - Rationale: Roughly 30 lines of build, port-discovery, and proof-invocation text remain duplicated here and in companion `docs/operations/tenant-onboarding-offboarding.md`. `OperationalRunbookSetTests.GraphIsolationEvidenceBoundary_SeparatesStructuralAndContentProof` asserts that each section contains the required tokens; it does not compare the sections to each other, and the sections are not identical. Extracting shared documentation is outside the bounded proof closure. - Re-open trigger: Either operator document drops a required token, a later change treats the two sections as byte-identical, or the documentation build gains a supported shared-snippet mechanism.
status: open

### DW-653: Collision fixtures remain in the shared real-backend topology.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Collision fixtures remain in the shared real-backend topology.** - ID: 24.6-CR-W2 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The proof seeds collision nodes, edges, and two provisioned tenants without teardown; unique tenant identifiers prevent cross-test identity conflicts, while shared-topology cleanup is a wider fixture-lifecycle concern. - Re-open trigger: A later test observes these records, integration storage growth becomes material, or the shared fixture adds a safe tenant teardown API.
status: open

### DW-654: The collision proof assumes newly provisioned graphs contain no relationships.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The collision proof assumes newly provisioned graphs contain no relationships.** - ID: 24.6-CR-W3 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: A pre-existing relationship would report a failed collision precondition rather than a dedicated fixture-precondition assertion; unique tenant provisioning makes that condition unlikely and diagnostic refinement is outside the closure. - Re-open trigger: The collision precondition fails on a non-empty newly provisioned graph or fixture provisioning begins seeding relationships.
status: open

### DW-655: Graph proof coverage is one bounded edge/origin/source/depth shape.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Graph proof coverage is one bounded edge/origin/source/depth shape.** - ID: 24.6-CR-W4 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: Story 24.6 proves the required `EdgeType.CausedBy`, `EdgeOrigin.Explicit`, `SourceType.File`, depth-one collision fixture; additional relationship variants and traversal depths are useful expansion coverage but not part of the accepted NFR8 slice. - Re-open trigger: A new edge type, origin, source type, or deeper traversal path changes tenant-routing behavior or a leakage defect appears outside the proven fixture.
status: open

### DW-656: The verifier class-level contract had been narrowed beyond Story 24.6.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The verifier class-level contract had been narrowed beyond Story 24.6.** - ID: 24.6-CR-W5 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Evidence: Fifth-pass Decision D2 restored the broad class-level architectural-isolation XML summary and kept the structural-only hedge local to `CheckGraphIsolationAsync`; focused verifier tests passed after the repair. - Re-open trigger: A future change again applies the graph-specific structural-only limitation to the verifier's class-wide Redis and semantic responsibilities.
status: done 2026-09-01
resolution: already resolved: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:21-22 restores the broad class summary.

### DW-657: Non-passing graph-isolation branches do not repeat the structural-only label.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Non-passing graph-isolation branches do not repeat the structural-only label.** - ID: 24.6-CR-W6 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Evidence: E-P12 prefixed the missing-graph and backend-unavailable `GraphIsolation.Details` branches with the structural-only label, confirmed by inspection of `TenantIsolationVerifier.cs`. The focused verifier, runbook, and authorization gate (the same three classes as the 86-case snapshot, since revised) passed 101/101 on 2026-08-29 after the `references/Hexalith.Builds` submodule bump unblocked the build; see the twelfth-pass Change Log row on `24-6-graph-content-level-tenant-isolation-evidence.md`. - Re-open trigger: An operator or automated consumer interprets a failed or unavailable `GraphIsolation` result as graph-content proof, or those branch messages are otherwise revised.
status: done 2026-09-01
resolution: already resolved: TenantIsolationVerifier.cs:448 and :915-918 qualify missing/backend-unavailable results as structural-only.

### DW-658: The HTTP-visible graph detail has no explicit length or format contract.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The HTTP-visible graph detail has no explicit length or format contract.** - ID: 24.6-CR-W7 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: The roughly 330-character prose detail is intentionally operator-facing and is now pinned for its required structural-only, `GRAPH.LIST`, and proof-method tokens; introducing a new structured or maximum-length contract would expand the public API surface. - Re-open trigger: The V1 response receives a formal details-length/format requirement or an operator surface truncates the required proof citation.
status: done 2026-09-01
decision: 2026-09-01 Retain prose contract — Keep the current bounded token assertions as the supported contract.
resolution: closed by human decision: Keep the current bounded token assertions as the supported contract.
decision: 2026-09-01 Retain prose contract — Keep the current bounded token assertions as the supported contract.

### DW-659: Production graph write-path tenant selection lacks a direct negative control.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineTests.cs
reason: - **Production graph write-path tenant selection lacks a direct negative control.** - ID: 24.6-CR-W8 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineTests.cs - Rationale: The collision proof writes directly through `falkor.SelectGraph(tenantId)` and proves authenticated read-path routing and content locality; production `IndexGraphActivity` tenant scoping is only pinned indirectly by post-ingestion node counts and requires a distinct ingestion-owned negative scenario. - Re-open trigger: An ingestion story changes graph selection or claims direct write-path cross-tenant proof, or a tenant A ingestion is observed in tenant B's graph.
status: open

### DW-660: The story embeds the Epic 23 checklist-preservation shell loop.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md
reason: - **The story embeds the Epic 23 checklist-preservation shell loop.** - ID: 24.6-CR-W9 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md - Rationale: The inline loop is executable and preserves the exact evidence used at review time; replacing it with only a document citation would reduce local reproducibility, while deduplicating governance commands is outside the proof closure. - Re-open trigger: The embedded command diverges from `spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` or becomes non-rerunnable.
status: open

### DW-661: Some traversal response assertions do not prove JSON wire presence.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Some traversal response assertions do not prove JSON wire presence.** - ID: 24.6-CR-W10 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: `Degraded`, `OmittedCount`, `UnavailableAxes`, and `PrimaryPathIntact` are asserted after deserialization, so omitted default-valued members could pass; the required node, edge, marker, completeness, and topology assertions still fail closed for the accepted content-isolation fixture. - Re-open trigger: The API serializer or response contract changes default-member emission, or these fields become part of the content-isolation acceptance claim.
status: open

### DW-662: Verifier unit mocks no longer rely on unconfigured graph-query defaults.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
reason: - **Verifier unit mocks no longer rely on unconfigured graph-query defaults.** - ID: 24.6-CR-W11 - Status: resolved - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs - Evidence: Fifth-pass repair inspects all `ReceivedCalls()` arguments for every `GRAPH.*` command and requires the non-empty executed set to contain only `GRAPH.LIST`; the companion source guard scans every `TenantIsolationVerifier*.cs` file and rejects any other graph command token. - Re-open trigger: A verifier collaborator can execute a graph command without being captured by `ReceivedCalls()`, or graph-command construction moves outside the guarded source family.
status: done 2026-09-01
resolution: already resolved: TenantIsolationVerifierTests.cs:1693-1720 and :1979 inspect graph calls and require GRAPH.LIST only.

### DW-663: Verifier source guard still lives in a runbook test class.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs
reason: - **Verifier source guard still lives in a runbook test class.** - ID: 24.6-F5-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs - Rationale: The first-pass patch read "strengthen **and relocate** the source-text guard". N5 strengthened it on 2026-08-13, but the assertion about `TenantIsolationVerifier.cs` source still sits in the runbook/deployment doc-contract class, so a verifier regression is reported by a test named for runbooks. Relocation is cosmetic to behaviour and was not attempted while the guard is green. - Re-open trigger: The guard fails and the failure is misattributed to runbook content, or another verifier source assertion is added to the same class.
status: open

### DW-664: `ReconnectPrimaryDaprClients` disposes before installing replacements.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **`ReconnectPrimaryDaprClients` disposes before installing replacements.** - ID: 24.6-F5-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The rewritten method constructs both actor-proxy replacements first and only then disposes `_actorHttpMessageHandler` and swaps `_actorProxyFactory`/`_actorProxyOptions`. The original dispose-before-assign window is closed; a brief null window remains during the swap. Not currently reachable: `[Collection("AspireIngestionPipeline")]` serialises the tests and the restart regression creates its proxy after the rotation. The untested allocation-failure cleanup path is recorded separately as `24.6-F8-W2`. - Re-open trigger: Any test caches an actor proxy across the OpenBao restart, or the collection gains parallel execution.
status: done 2026-09-01
resolution: already resolved: AspireIngestionPipelineFixture.cs:1191-1206 constructs replacements before disposing old clients at :1215-1218.

### DW-665: Reconnect fires only when the sidecar endpoint changes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **Reconnect fires only when the sidecar endpoint changes.** - ID: 24.6-F5-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The guard is `if (currentDaprEndpoint != DaprSidecarHttpEndpoint)`, so a sidecar that restarts on the same port skips the reconnect entirely and the fixture keeps pooled connections to the killed process. The correct trigger condition (endpoint change **or** process restart) needs a restart signal the fixture does not currently expose. - Re-open trigger: The restart regression flakes with a connection error on an unchanged port, or a process-restart signal becomes available.
status: open

### DW-666: Traversal assertions dereference without null guards.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Traversal assertions dereference without null guards.** - ID: 24.6-F5-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: `AssertTraversalIsFixtureLocal` dereferences `Nodes`, per-node `Edges`, and `GapMarkers` without null checks, so a response that omits or nulls one of them raises a `NullReferenceException` instead of an assertion naming the field that lost its marker. Diagnostic quality only; the assertions still fail closed. - Re-open trigger: A traversal failure is reported as a `NullReferenceException`, or the traversal contract makes any of those members nullable.
status: open

### DW-667: No cancellation coverage on `VerifyAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
reason: - **No cancellation coverage on `VerifyAsync`.** - ID: 24.6-F5-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs - Rationale: No test invokes `VerifyAsync` with an already-cancelled token, so the graph check's cancellation behaviour is unpinned and a regression would pass unnoticed. Outside AC3's structural-only scope. - Re-open trigger: Cancellation handling in `TenantIsolationVerifier` is changed, or a cancellation defect is observed in the verify endpoint.
status: open

### DW-668: No `/traverse` denial rows for invalid or out-of-range `depth`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs
reason: - **No `/traverse` denial rows for invalid or out-of-range `depth`.** - ID: 24.6-F5-W6 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs - Rationale: The Story 20.2 denial-before-dependency rows cover missing and blank `startNodeId` but not a malformed or out-of-range `depth`, where a 400 could pre-empt the 403 and reveal that the handler was reached. Beyond AC1's stated boundary. - Re-open trigger: `depth` validation moves relative to the tenant authorization filter, or a new query parameter is added to the traverse route.
status: open

### DW-669: Restart regression dereferences an actor config without a null guard.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs
reason: - **Restart regression dereferences an actor config without a null guard.** - ID: 24.6-F5-W7 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs - Rationale: The post-rotation actor call dereferences its result without a null guard and silently depends on the `OpenBaoRecoveryTenantId` tenant carrying a seeded embedding configuration, so a fixture-data gap surfaces as a `NullReferenceException` rather than a named assertion. - Re-open trigger: The regression fails with a `NullReferenceException`, or `OpenBaoRecoveryTenantId` seeding changes.
status: open

### DW-670: The HTTP-visible AC3 citation is hard-coded where the unit guards are manifest-bound.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The HTTP-visible AC3 citation is hard-coded where the unit guards are manifest-bound.** - ID: 24.6-F6-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: F5-P8 bound the operator proof citation to `tools/integration-fast-required-surfaces.txt` through `OperationalRunbookSetTests.GraphContentProofCitation`, and both Server.Tests guards now derive it. The real-backend assertion cannot reach that `internal` member from a different assembly, so it keeps the literal `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`. The binding is fail-closed — a manifest-driven rename reds the unit lane — so this is maintenance duplication in three places, not an escape hatch, and the fix would mean duplicating the manifest reader into the integration assembly. - Re-open trigger: The graph-content proof method is renamed or re-keyed in the manifest, or a shared test-support assembly becomes available to both test projects.
status: open

### DW-671: The restored verifier class-level summary has no test pinning either wording.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The restored verifier class-level summary has no test pinning either wording.** - ID: 24.6-F6-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: `24.6-CR-W5` was closed on the strength of fifth-pass Decision F5-D2 restoring the broad architectural-isolation XML summary and keeping the structural-only hedge local to `CheckGraphIsolationAsync`. The original finding's second half — "no test pins either wording" — was not addressed, so the entry's own re-open trigger ("a future change again applies the graph-specific structural-only limitation to the verifier's class-wide responsibilities") has no detector and would have to be caught by review. The reverted wording is net-zero against baseline `0ecdffed`, so nothing regressed; only the guard is missing. - Re-open trigger: The class-level summary is edited again in either direction, or a Story 24.7-24.9 slice re-scopes the verifier's Redis marker responsibilities.
status: open

### DW-672: The graph-check lookup throws an undiagnosable exception when the check is absent.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The graph-check lookup throws an undiagnosable exception when the check is absent.** - ID: 24.6-F6-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The new HTTP-observable AC3 block resolves the check with `result.Checks.Single(check => check.CheckName == "GraphIsolation")`. If the verifier stops emitting `GraphIsolation`, or emits it twice, the test fails with a bare `InvalidOperationException` naming neither the check nor the contract, instead of a Shouldly assertion. The surrounding `AssertCoreIsolationChecksPassed(result)` already fails closed on a missing check, so the diagnosis cost is the only impact. - Re-open trigger: The test fails with `InvalidOperationException`, or the verifier begins emitting per-backend `GraphIsolation` results.
status: open

### DW-673: Node-marker assertions lack a mutation-sensitivity control.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Node-marker assertions lack a mutation-sensitivity control.** - ID: 24.6-F6-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The positive real-backend fixture proves tenant-local node and edge markers, while the planted-marker mutation control exercises only the edge-marker assertion. The Administrator ratified C1's completed boundary as edge-only for mutation sensitivity; a node-marker control remains useful hardening but is outside that boundary. - Re-open trigger: A node-marker assertion is weakened or removed, a foreign-node regression appears, or Story 24.6's ratified C1 mutation-sensitivity boundary is reopened.
status: open

### DW-674: Graph-isolation verification does not convert a FalkorDB `RedisTimeoutException` into a failed backend check.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Graph-isolation verification does not convert a FalkorDB `RedisTimeoutException` into a failed backend check.** - ID: 24.6-F6-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: `CheckGraphIsolationAsync` catches `RedisConnectionException` and `RedisServerException` but not `RedisTimeoutException`, so a timeout can escape the verifier instead of preserving the existing graceful backend-unavailable result shape. - Re-open trigger: A FalkorDB timeout escapes `VerifyAsync` as an unhandled exception, or a later story converts `RedisTimeoutException` into a failed `GraphIsolation` check.
status: open

### DW-675: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed. evidence: The working tree shows `references/Hexalith.Builds` dirty at `5d268c6b` with `Props/Directory.Packages.props` modified. That leftover is owned by `spec-submodule-bumps-2026-08-11.md`, not this envelope, which was required to preserve unrelated root work.
status: done 2026-09-01
resolution: already resolved: commit 8ed18ed6 bumps references/Hexalith.Builds; its checked-out worktree is clean on main.

### DW-676: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope. evidence: The 2026-08-05 envelope still has an unchecked superproject-push task and was not superseded or partitioned by the 2026-08-09 closeout, so a later operator can restage the same gitlinks under a second Story-Key.
status: done 2026-09-01
resolution: already resolved: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-05.md:5 is done.

### DW-677: Direct origin/main push for authorized /pushall envelopes still trips GitHub branch-protection (PR required, expected status checks).

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: Direct origin/main push for authorized /pushall envelopes still trips GitHub branch-protection (PR required, expected status checks). evidence: Push `3e92ca36..8d47a46a` succeeded while GitHub reported a branch-protection bypass. This envelope's remaining task is to push the superproject, matching prior /pushall specs; the protection warning is a standing process tension, not a defect unique to this snapshot.
status: open

### DW-678: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md` summary: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11. evidence: Memories consumes its own contracts via ProjectReference, so this pin is not a direct PackageReference AC failure; bumping it locally would move Builds off origin/main unless a Builds PR lands first.
status: done 2026-09-01
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:10 pins HexalithMemoriesVersion 2.22.1 in commit 12b69515.

### DW-679: Restore/build still surfaces NU1903 for SSH.NET 2025.1.0.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md` summary: Restore/build still surfaces NU1903 for SSH.NET 2025.1.0. evidence: Pre-existing advisory warning observed during the Release package-mode verification of this dependency refresh; unrelated to the submodule gitlink bumps.
status: open

### DW-680: TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.** - ID: 24.7-CTOR-UNGUARDED-PARAMS - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: A future change to `TenantIsolationVerifier`'s constructor, or a dedicated null-safety hardening pass. - Rationale: Only the new `embeddingConfigProvider` parameter gained an ArgumentNullException guard; `registry`, `redis`, `falkorDb`, and `logger` predate Story 24.7 and still surface null as NullReferenceException at first use, deviating from the documented `ArgumentNullException.ThrowIfNull` boundary rule. Pre-existing behavior; only the new parameter was in Story 24.7's scope.
status: open

### DW-681: Make the concrete tenant embedding configuration provider stop its actor read when caller cancellation wins.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs
reason: - **Make the concrete tenant embedding configuration provider stop its actor read when caller cancellation wins.** - ID: 24.7-PROVIDER-CANCELLATION-IGNORED - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs - Re-open trigger: A provider-focused change to `TenantEmbeddingConfigProvider`, or evidence that a cancelled verification populates the cache with a result the caller never observed. - Rationale: `TenantEmbeddingConfigProvider.GetAsync` accepts a cancellation token but awaits `GetEmbeddingConfigAsync()` without applying it, so `TenantIsolationVerifier` can stop waiting through `WaitAsync` while the actor call continues and may populate the cache after the verification request is cancelled. Pre-existing behavior outside Story 24.7's mapped file scope.
status: open

### DW-682: Bound semantic-isolation mismatch evidence returned for large tenant key sets.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Bound semantic-isolation mismatch evidence returned for large tenant key sets.** - ID: 24.7-SEMANTIC-EVIDENCE-UNBOUNDED - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: The owning diagnostic-bounding slice is selected, or an operator reports an oversized `SemanticIsolation.Details` response. - Rationale: `ScanHashPrefixForTenantFieldMismatchesAsync` records every missing or foreign tenant marker and `CheckSemanticIsolationAsync` joins the full list into `Details`; this behavior predates Story 24.7 and can produce an unbounded diagnostic response when many hashes are contaminated. Pre-existing behavior; bounding it is out of Story 24.7's frozen scope.
status: open

### DW-683: `GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **`GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.** - ID: 24.6-F8-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: The success `Details` string reports `({graphDatabases.Count} graph database(s))` over `POST /api/v1/tenants/{tenantId}/verify`, which is a count of other tenants returned to a single tenant's caller. The count predates Story 24.6 and the endpoint is operator-facing rather than tenant-facing, so this is not a live cross-tenant data leak. It is recorded because this range rewrote that exact string for a story whose thesis is not overstating isolation evidence, and no existing entry covers it. - Re-open trigger: The verify endpoint becomes reachable by a tenant-scoped caller, or any story re-scopes `GraphIsolation` evidence semantics.
status: open

### DW-684: The allocation-failure cleanup path in `ReconnectPrimaryDaprClients` has no test in any lane.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **The allocation-failure cleanup path in `ReconnectPrimaryDaprClients` has no test in any lane.** - ID: 24.6-F8-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The seventh pass added a `try`/`catch` that disposes the partially constructed `HttpClientHandler` and state client on construction failure and rethrows. The ledger records that phase as `+0 test cases / +0 test methods`, and the path is not reachable from any existing test because it requires a client construction failure mid-rotation. Forcing it would mean injecting a fault into fixture startup, which the current fixture design does not expose. - Re-open trigger: The fixture gains a fault-injection seam, or a rotation failure is observed in CI leaving a leaked handler.
status: open

### DW-685: The planted-marker negative control leaves its mutation in a provisioned tenant graph and is now a required CI surface.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The planted-marker negative control leaves its mutation in a provisioned tenant graph and is now a required CI surface.** - ID: 24.6-F8-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` writes a foreign edge marker into tenant A's real graph with no teardown, and this range pinned it as a required `integration-fast` method surface. Each run provisions fresh GUID-suffixed tenants, so the corruption does not cross runs; the residual concern is that pinning a deliberately data-corrupting method into the required lane widens exactly the fixture-cleanup risk `24.6-CR-W2` already describes, without either entry recording the change. - Re-open trigger: A test outside this method observes a foreign marker on a shared fixture tenant, or the fixture moves to reused rather than per-run tenant identifiers.
status: open

### DW-686: The new assertions lack defensive guards that would name the failing field instead of throwing.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The new assertions lack defensive guards that would name the failing field instead of throwing.** - ID: 24.6-F8-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: Six hardening gaps were identified across the new assertions: no null guards on `traversal.Nodes`, `traversal.GapMarkers`, or per-node `Edges`; no already-cancelled `CancellationToken` case for `VerifyAsync`; no empty or null `GRAPH.LIST` case pinning `ParseGraphList`; `Single()` rather than `ShouldHaveSingleItem` for the graph-check lookup; no linked cancellation token on the seed query, so a command abandoned by `WaitAsync` is not actually cancelled; and no null guard on the OpenBao recovery tenant's embedding configuration. Each degrades diagnosis quality on failure rather than weakening the proof — the assertions themselves fail closed — so they are hardening, not correctness defects. `24.6-F6-W3` already covers the `Single()` case alone. - Re-open trigger: Any of these sites fails with a bare `NullReferenceException` or `InvalidOperationException` in CI, or the seed query is observed duplicating relationships.
status: open

### DW-687: Older Story 27.2 and `spec-gh-30838751196` still describe a standalone Access Telemetry AppHost for the routed Dapr proof.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-31871199175-fix-ci-cd.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-31871199175-fix-ci-cd.md` summary: Older Story 27.2 and `spec-gh-30838751196` still describe a standalone Access Telemetry AppHost for the routed Dapr proof. evidence: This change joined that proof to `AspireIngestionPipeline`; those artifacts were outside file scope and still document a second AppHost.
status: open

### DW-688: Backfill unavailable historical command-level validation and pruning logs for the five completed pushall synchronization specs.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-mark-completed-pushall-specs-done.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-mark-completed-pushall-specs-done.md` summary: Backfill unavailable historical command-level validation and pruning logs for the five completed pushall synchronization specs. evidence: The specs list intended validation commands but do not retain their contemporaneous output; the current repository proves the landed commits and absence of remaining branches but cannot reconstruct exact historical command results.
status: open

### DW-689: Make first creation of the aggregate-to-case mapping index concurrency-safe.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Make first creation of the aggregate-to-case mapping index concurrency-safe. evidence: `DaprAggregateCaseMappingStore.EnsureIndexedAsync` creates an absent index without `FirstWrite`, so concurrent first mappings can both save from an empty ETag and lose one aggregate type.
status: open

### DW-690: Make case-mapping deletion recoverable when map-key deletion fails after index removal.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Make case-mapping deletion recoverable when map-key deletion fails after index removal. evidence: `DeleteByCaseAsync` commits aggregate-type removal from the index before deleting map keys, so a later delete failure leaves unindexed state that retries cannot enumerate.
status: open

### DW-691: Coordinate aggregate-to-case writers with tenant purge completion.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Coordinate aggregate-to-case writers with tenant purge completion. evidence: `DeleteAllTenantDataAsync` can observe an empty index and return while a concurrent writer has persisted a map but has not yet recreated the index, leaving tenant data after purge reports success.
status: open

### DW-692: Rebuild missing observed-event discovery indexes when membership markers survive index expiry.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Rebuild missing observed-event discovery indexes when membership markers survive index expiry. evidence: `UpdateDiscoveryIndexAsync` treats an existing membership marker as sufficient and `RefreshIndexTtlAsync` returns on a missing index, allowing stored observations to disappear from discovery indefinitely.
status: open

### DW-693: Handle failed TTL refresh writes for observed-event indexes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Handle failed TTL refresh writes for observed-event indexes. evidence: Written and discovery index refresh paths discard false `TrySaveStateAsync` results while returning success, so indexes can expire before the observation keys they enumerate.
status: open

### DW-694: Coordinate observed-event writers with tenant deletion.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Coordinate observed-event writers with tenant deletion. evidence: `DaprObservedEventTypeStore.DeleteAllTenantDataAsync` enumerates one snapshot and then deletes both indexes without a writer barrier, allowing concurrent observations to survive while becoming unindexed.
status: open

### DW-695: Bound tenant-isolation embedding-configuration lookup time independently of caller cancellation.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Bound tenant-isolation embedding-configuration lookup time independently of caller cancellation. evidence: `TenantIsolationVerifier.CheckSemanticIsolationAsync` waits only on the caller token, so a provider that ignores cancellation can hang verification indefinitely.
status: open

### DW-696: Ensure scheduled nightly runs execute the fast integration lane.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Ensure scheduled nightly runs execute the fast integration lane. evidence: `.github/workflows/nightly.yml` declares a schedule but gates `integration-fast` exclusively on `workflow_dispatch`, causing scheduled runs to skip that verification job.
status: open

### DW-697: Restrict filesystem permissions on temporary OpenBao bootstrap credentials.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Restrict filesystem permissions on temporary OpenBao bootstrap credentials. evidence: `Publish-OpenBaoBootstrapSecrets` writes live runtime and access tokens to a normal temporary directory without explicitly enforcing owner-only directory and file permissions on Unix hosts.
status: open

### DW-698: Make the fake Dapr state store copy mutable values and enforce state options.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Make the fake Dapr state store copy mutable values and enforce state options. evidence: `FakeDaprStateStore` returns backing lists and dictionaries directly and ignores `StateOptions`, so failed-save and FirstWrite/TTL tests can mutate fake persistence in place or prove weaker semantics than production.
status: open

### DW-699: Convert Redis command timeouts across tenant-isolation checks into structured backend-unavailable evidence.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: Convert Redis command timeouts across tenant-isolation checks into structured backend-unavailable evidence. evidence: `TenantIsolationVerifier` catches connection and server exceptions but not `RedisTimeoutException`, so an index-info, cursor-adjacent, or hash-field timeout can escape `VerifyAsync` instead of returning the verifier's backend-unavailable result contract.
status: open

### DW-700: Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan.** - ID: 24.6-F8-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Syntactic isolation verification fails on non-hash Redis keys under tenant prefix or is unable to cancel in-flight hash inspections. - Rationale: `TenantIsolationVerifier.ScanHashPrefixForTenantFieldMismatchesAsync` calls `await db.HashGetAsync(key, "tenantId")` without passing `CancellationToken ct` and does not handle `RedisServerException` on WRONGTYPE non-hash keys. Pre-existing behavior outside story scope.
status: open

### DW-701: Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods.** - ID: 24.6-F8-W6 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Cancellation token propagation standards are audited across all server verification services. - Rationale: `CheckIndexExistenceAsync`, `CheckSyntacticIsolationAsync`, and `CheckOrphanedDatabasesAsync` lack entry `ct.ThrowIfCancellationRequested()` and do not bind Redis operations to `ct`. Pre-existing behavior outside story scope.
status: open

### DW-702: Optimize ScanSemanticHashPrefixForTenantEvidenceAsync to single Redis command round-trip per key.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Optimize ScanSemanticHashPrefixForTenantEvidenceAsync to single Redis command round-trip per key.** - ID: 24.6-F8-W7 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.8 optimizes semantic hash prefix scanning performance. - Rationale: For every scanned key, `ScanSemanticHashPrefixForTenantEvidenceAsync` executes separate `HashGetAsync` and `HashExistsAsync` calls; retrieving `naturalLanguageDescription` in the discriminator batch avoids redundant round-trips. Pre-existing behavior owned by Story 24.8.
status: open

### DW-703: Broaden IsEmbeddingConfigurationUnavailable exception filters in TenantIsolationVerifier.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Broaden IsEmbeddingConfigurationUnavailable exception filters in TenantIsolationVerifier.** - ID: 24.6-F8-W8 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.7 audits provider lookup error handling. - Rationale: `IsEmbeddingConfigurationUnavailable` filters for four specific exception types; unexpected I/O or RPC exceptions during provider lookups could surface as unhandled 500 errors instead of structured check results. Pre-existing behavior owned by Story 24.7.
status: open

### DW-704: Support dual remediation when classification gap and dimension mismatch co-occur.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Support dual remediation when classification gap and dimension mismatch co-occur.** - ID: 24.6-F8-W9 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.8 addresses dual remediation formatting. - Rationale: `TenantIsolationVerifier.CheckSemanticIsolationAsync` suppresses vector dimension remediation guidance when a key classification gap is also present. Pre-existing behavior owned by Story 24.8.
status: open

### DW-705: Add replica endpoint filtering in GetConnectedServers for clustered Redis deployments.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Add replica endpoint filtering in GetConnectedServers for clustered Redis deployments.** - ID: 24.6-F8-W10 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Redis cluster deployment topology validation is performed. - Rationale: `TenantIsolationVerifier.GetConnectedServers` does not filter out `server.IsReplica`, which can cause duplicate scans across replicas in clustered or primary-replica topologies. Pre-existing behavior outside story scope.
status: open

### DW-706: Tenants worktree HEAD `4a3eec38` is ahead of the staged gitlink `c5fa0082`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-3-100-0.md` summary: Tenants worktree HEAD `4a3eec38` is ahead of the staged gitlink `c5fa0082`. evidence: Pre-existing `MM` submodule state; this story must not unstage or edit Tenants, so a later parent commit of the staged SHA can miss checkout `4a3eec38`.
status: done 2026-09-01
resolution: already resolved: commit 8ed18ed6 bumps references/Hexalith.Tenants to clean main commit aa88a037.

### DW-707: Isolate the server embedding-provider options test from parallel static options state.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-ci-cd-and-release-2026-08-30.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-ci-cd-and-release-2026-08-30.md` summary: Isolate the server embedding-provider options test from parallel static options state. evidence: `AddMemoriesServerServices_WithHostEmbeddingProvidersConfig_SeedsCurrentOptionsAndOllama` observed the repository default endpoint during one full release-suite run, passed alone, and passed on an immediate complete rerun; the CI runner and telemetry-root changes do not touch that options surface.
status: open

### DW-708: Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence. evidence: `CheckSemanticIsolationAsync` awaits the raw-prefix scan then the natural-language-prefix scan under one shared `try`/`catch (RedisConnectionException/RedisServerException)`; if the second scan throws, any marker mismatches or classification gaps the first scan already collected are discarded and the check returns a generic backend-unavailable result instead of surfacing the already-detected isolation problem. Pre-existing pattern predating Story 24.8 (the same shared-try shape already existed for `ScanHashPrefixForTenantFieldMismatchesAsync`'s raw/NL split); this diff extends it to also carry classification-gap evidence.
status: open

### DW-709: `Remediation` text still does not support dual guidance when a classification gap co-occurs with another problem.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: `Remediation` text still does not support dual guidance when a classification gap co-occurs with another problem. evidence: `CheckSemanticIsolationAsync`'s `hasClassificationGap ? "Register or migrate..." : "Repair or re-provision..."` ternary always picks gap wording over marker-mismatch (or dimension-mismatch) wording when both are present in `Details`. This is the same unresolved gap as carried-forward ledger item `24.6-F8-W9` ("dual remediation when classification gap and dimension mismatch co-occur"), which already named Story 24.8 as its natural closure point; this diff did not close it, only extended the same single-priority pattern to also cover the new classification-gap case.
status: open

### DW-710: The two-round-trip discriminator read was not folded into one batched call.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: The two-round-trip discriminator read was not folded into one batched call. evidence: `ScanSemanticHashPrefixForTenantEvidenceAsync` still issues a separate `HashGetAsync` (5 fields) and `HashExistsAsync` (`naturalLanguageDescription`) per scanned key. This is the same unresolved gap as carried-forward ledger item `24.6-F8-W7`, which already named Story 24.8 as its natural closure point; this diff did not close it.
status: open

### DW-711: `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` still calls `AddHexalithMemoriesSearchIndexServer` with the retired `string secretStoreComponentPath` positional argument and will fail to build once it references an updated `Hexalith.Memories.Aspire` package.

origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
location: references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md` summary: `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` still calls `AddHexalithMemoriesSearchIndexServer` with the retired `string secretStoreComponentPath` positional argument and will fail to build once it references an updated `Hexalith.Memories.Aspire` package. evidence: Story 29.2 changed that parameter to `IResourceBuilder<IDaprComponentResource> secretStore` (an intentional, spec-required breaking change so the reusable extension accepts an externally-provisioned secret-store resource instead of hard-coding `secretstores.local.file`). Fixing the Tenants call site requires a coordinated change in the `Hexalith.Tenants` submodule/repo, which this story's boundaries explicitly exclude from casual editing. - ID: 29.2-TENANTS-SECRETSTORE-CALLSITE - Status: resolved - Source story: spec-29-2-provider-neutral-aspire-composition-and-secret-verification - Target artifact: references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs - Re-open trigger: a future `Hexalith.Memories.Aspire` signature change breaks the Tenants AppHost call site again, or the Tenants submodule pin is bumped without picking up commit `7453ba5b`. - Evidence: Hexalith.Tenants commit `7453ba5b` ("fix: update Memories secret-store call site for the Aspire 29.2 signature change") builds an externally-provisioned `secretstores.local.file` component via `AddDaprComponent` and passes it to `AddHexalithMemoriesSearchIndexServer`, matching the new signature; committed and pushed to `origin/main` during code review.
status: done 2026-09-01
resolution: already resolved: Hexalith.Tenants commit 7453ba5b updates the secret-store call site; Program.cs:124-129 passes the Dapr component builder.

### DW-712: `docs/operations/openbao.md`'s new Story 29.2 passage claims a standalone Dapr self-hosted host (not just Kubernetes) can supply the `openbao-runtime-bootstrap`/`openbao-access-telemetry-bootstrap` bootstrap Secrets that `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` reference via `secretKeyRef`, but this has not been verified against Dapr's actual self-hosted secret-store resolution behavior outside Kubernetes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
location: docs/operations/openbao.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-29-2-provider-neutral-aspire-composition-and-secret-verification.md` summary: `docs/operations/openbao.md`'s new Story 29.2 passage claims a standalone Dapr self-hosted host (not just Kubernetes) can supply the `openbao-runtime-bootstrap`/`openbao-access-telemetry-bootstrap` bootstrap Secrets that `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` reference via `secretKeyRef`, but this has not been verified against Dapr's actual self-hosted secret-store resolution behavior outside Kubernetes. evidence: `secretKeyRef` in a Dapr component's metadata typically resolves through a Kubernetes secret store in k8s-hosted Dapr; a bare self-hosted Dapr install would need its own separately configured secret-store component to resolve those references, which these standalone templates do not define or document. If that mechanism does not work outside Kubernetes as claimed, the "standalone Dapr self-hosted host" deployability statement in `docs/operations/openbao.md` is inaccurate and should be corrected or scoped to Kubernetes only. - ID: 29.2-OPENBAO-SELFHOSTED-SECRETKEYREF-CLAIM - Status: resolved - Source story: spec-29-2-provider-neutral-aspire-composition-and-secret-verification - Target artifact: docs/operations/openbao.md - Re-open trigger: a standalone (non-Kubernetes) Dapr self-hosted deployment is actually exercised and its `secretKeyRef` resolution behavior is verified, positively or negatively, which should replace this caveat with a confirmed statement. - Evidence: `docs/operations/openbao.md`'s Dapr secret boundaries section now scopes the `secretKeyRef` bootstrap resolution explicitly to Kubernetes and marks the standalone self-hosted case as unverified rather than asserting it works, resolved during code review.
status: done 2026-09-01
resolution: already resolved: docs/operations/openbao.md:366-375 scopes secretKeyRef bootstrap to Kubernetes and states the standalone resolver requirement.

### DW-713: Task 4 full-stack proof needs an EventStore domain-service resource Story 28.1 is not scoped to add.

origin: migrated from legacy ledger ("Deferred from: spec-28-1-adopt-owner-approved-eventstore-runtime-identity dev (2026-09-01)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Epic 28, a new follow-up story)
reason: - **Task 4 full-stack proof needs an EventStore domain-service resource Story 28.1 is not scoped to add.** - ID: 28.1-TASK4-FULLSTACK-PROOF-NEEDS-DOMAIN-SERVICE - Status: accepted - Source story: spec-28-1-adopt-owner-approved-eventstore-runtime-identity - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Epic 28, a new follow-up story) - Rationale: Story 28.1's Task 3 correctly added exactly one `eventstore` gateway resource to Memories' AppHost (`src/Hexalith.Memories.AppHost/Program.cs`), per the spec's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary. EventStore's own full-stack proof pattern (`references/Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Fixtures/AspirePubSubProofTestFixture.cs` + `ContractTests/PubSubDeliveryProofTests.cs`) submits a command to the Gateway HTTP API and asserts a Dapr-published CloudEvent, but that pattern's domain logic (e.g. the `counter` sample domain) is compiled into a **separate** Aspire-composed domain-service resource (`Hexalith.EventStore.AppHost/Program.cs:119`, `AddProject<Projects.Hexalith_EventStore_Sample>("sample")`) that Memories' AppHost has no equivalent of. `SandboxCommandRequest.cs`'s own doc comment confirms any command requires "the domain service Handle method" to exist. Adding one would expand Story 28.1's topology beyond its approved scope — exactly the case `epics.md`'s Story 28.1 final Given/When/Then clause ("Given adoption exposes a behavioral incompatibility... fails closed and routes that behavior change to a separately approved compatibility story rather than expanding silently") anticipates, and consistent with spec-28-1's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary. (Note: this is not a numbered "AC7" in either document; an earlier pass of this entry miscited it as one.) - Candidate resolutions (neither attempted, both need a human/architecture decision, not a unilateral dev choice): (1) add a Memories-owned EventStore domain-service resource to the AppHost so a real EventStore-originating command can be submitted and its Dapr-published event traced into Memories — the direct analog of EventStore's own proof pattern; or (2) route Memories' own existing Tenant/Case domain commands (which already reach the live `eventstore` gateway) back through `hexalith-eventstore/*` topic naming into Memories' own ingestion — investigated and set aside this session because no existing tenant-routing config maps that back to Memories' ingestion, and wiring one up risks an unverified self-referential duplicate-indexing loop. - Resolution criteria (per the original 23.7 entry, unchanged): a real EventStore-originating publish reaches Memories through Dapr; the resulting memory is persisted and searchable through Redis and FalkorDB; duplicate replay is ignored; negative evidence proves no cross-tenant result leakage. - Re-open trigger: a follow-up story is selected to close this gap; or any story/review claims EventStore-to-Memories full-stack proof without meeting every resolution criterion above.
status: open
decision: 2026-09-01 Own a domain service — Add a Memories-owned EventStore domain-service resource and prove commands through it end to end.
decision: 2026-09-01 Own a domain service — Add a Memories-owned EventStore domain-service resource and prove commands through it end to end.

### DW-714: Classification-gap co-occurring with an active marker defect still suppresses marker-specific `Remediation` entirely.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md` summary: Classification-gap co-occurring with an active marker defect still suppresses marker-specific `Remediation` entirely. evidence: `TenantIsolationVerifier.CheckSemanticIsolationAsync`'s `Remediation` ternary (`src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:389-395`) still selects only the classification-gap sentence whenever a classification gap co-occurs with a marker mismatch in the same `SemanticIsolation` check, giving the operator no inspect/quarantine/named-key guidance for the marker defect at all in that case. Pre-existing behavior, not introduced by Story 24.9; already tracked as ledger item `24.6-F8-W9` below, and Story 24.9's own spec Boundaries ("Ask First") explicitly declined to resolve it here since it is a different axis than this story's AC. Reopen trigger: Story 24.8/24.9 follow-up work touching the same ternary, or an operator report that this suppression hid actionable marker guidance during a real co-occurring incident.
status: open

### DW-715: `CheckSyntacticIsolationAsync` still returns the anti-template blanket-deletion wording Story 24.9 removed from `SemanticIsolation`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-9-non-destructive-tenant-marker-diagnostics.md` summary: `CheckSyntacticIsolationAsync` still returns the anti-template blanket-deletion wording Story 24.9 removed from `SemanticIsolation`. evidence: `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs:216` still reads `"Repair or re-provision the tenant RediSearch index and remove mismatched target-prefix hashes"`. Story 24.9's Boundaries explicitly forbid touching the syntactic-only `ScanHashPrefixForTenantFieldMismatchesAsync`/`CheckSyntacticIsolationAsync` path ("a separate, non-semantic check outside this AC's `SemanticIsolation` scope"), so this is pre-existing and correctly out of this story's scope — but it now leaves an inconsistent operator experience: `SemanticIsolation` failures get non-destructive, named-key guidance while `SyntacticIsolation` failures still get blanket-deletion language. Reopen trigger: a follow-up story extending the non-destructive marker-diagnostic pattern to `SyntacticIsolation`.
status: open

### DW-716: Story 28.1 code review — CI provisioning-step duplication and unconfirmed AppHost resource start-ordering.

origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-01
location: .github/workflows/ci.yml; .github/workflows/nightly.yml; src/Hexalith.Memories.AppHost/Program.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md` summary: `tools/ci/provision-eventstore-local-feed.sh`'s ~9-line invocation is copy-pasted verbatim into 7 CI job definitions across `ci.yml`/`nightly.yml` instead of a shared composite action, and local dev follows a separate, independently-maintained manual procedure (recorded in the spec's Verification section) for producing the same artifact rather than invoking the same script. evidence: confirmed by direct inspection of both workflow files -- the identical step comment and `run:` line appear in `build`, `test-unit-contract`, `web-e2e-specimen`, `integration-fast` (ci.yml) and `integration-fast`, `integration-slow`, `benchmark` (nightly.yml). Real DRY/drift risk (a future SHA/version rotation needs 7+ synchronized edits plus the local dev docs), but not a correctness defect in the current diff, and refactoring into a composite action is a real engineering task, not a trivial patch. **Partially mitigated (2026-09-01 follow-up patch pass):** the two hardcoded literals (`fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` / `999.1.20-proof.fa2d1c9910f8`) that were duplicated across all 7 CI call sites are now centralized in each workflow file's top-level `env:` block (`EVENTSTORE_APPROVED_SHA` / `EVENTSTORE_APPROVED_VERSION`), reducing the literal-duplication surface from ~14 hardcoded values to 2 per workflow file. The *step itself* (name, comment, `run:` line referencing those env vars) is still repeated 7 times -- collapsing that into a shared composite action remains the open, real-engineering-task portion of this finding. -- source_spec: same. summary: No explicit Aspire `WaitFor` ordering is established between the `memories` resource (which reaches `eventstore` via Dapr service invocation) and the new `eventStoreGateway` resource, so it's unclear whether Memories Server could attempt to invoke the `eventstore` app-id before that resource/sidecar is ready. evidence: `src/Hexalith.Memories.AppHost/Program.cs`'s new `eventStoreGateway` only has `.WaitFor(redis)`; no `WaitFor(eventStoreGateway)` was added to the `memories`/`server` resource. Whether this is a real race depends on Dapr's own service-invocation retry/resiliency semantics, which needs an architect's judgment call rather than a mechanical fix -- do not guess at an ordering constraint without confirming Dapr's actual behavior here. - Re-open trigger: a follow-up story touches this CI wiring or the AppHost resource graph again, a flaky/racy `eventstore` invocation is observed at runtime, or (specific to the rebuild-and-self-sign CI provisioning workaround as a whole) EventStore's own team reseals Story 1.20's proof packet under Memories' mandated SDK `10.0.400` -- at that point `tools/ci/provision-eventstore-local-feed.sh`, `tools/nuget-local-feeds/`, and every CI step wired to them become retirable in favor of restoring the now-reproducible, originally-approved package hash directly, and this whole workaround (script, local-dev config, per-run signing) should be removed rather than kept running indefinitely alongside a now-available real fix.
status: open
decision: 2026-09-01 Prove retry behavior — Extract the action, regression-test Dapr retry behavior, document it, and retain no WaitFor only if safe.
decision: 2026-09-01 Prove retry behavior — Extract the action, regression-test Dapr retry behavior, document it, and retain no WaitFor only if safe.

### DW-717: `production-deployment-verification` CI job not wired to the ephemeral EventStore local-feed provisioning script; unconfirmed whether it needs it.

origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-01
location: .github/workflows/ci.yml
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md` summary: the `production-deployment-verification` job in `ci.yml` builds container images via `tools/publish-containers.ps1`, which was not traced this session to confirm whether its own internal `dotnet publish`/`dotnet pack` calls also resolve `Hexalith.EventStore.Client` at the proof-identity version pinned in `references/Hexalith.Builds/Props/Directory.Packages.props`. Every other CI job that restores the full `Hexalith.Memories.slnx` graph was wired to `tools/ci/provision-eventstore-local-feed.sh` (see DW-716 and the spec's Task 2/CI checklist entries) because a plain `dotnet restore`/`dotnet build` against the tracked `NuGet.config` alone fails with `NU1102` for that version; `production-deployment-verification` was left unwired because its build path goes through a PowerShell script rather than a direct `dotnet restore`/`build` step in the YAML, and tracing that script's own restore graph was out of scope for the patch pass that wired the other 7 jobs. evidence: `.github/workflows/ci.yml`'s `production-deployment-verification` job (`Publish local release OCI archives` step, `run: ./tools/publish-containers.ps1 ...`) has no `Provision ephemeral EventStore local NuGet feed` step and no `--configfile`/`RestoreConfigFile` wiring, unlike `build`, `test-unit-contract`, `web-e2e-specimen`, and `integration-fast`. - Re-open trigger: `production-deployment-verification` is observed failing with `NU1102` (or an equivalent EventStore-package restore failure) in CI; or a follow-up story/review traces `tools/publish-containers.ps1`'s restore graph and confirms (or rules out) that it needs the same provisioning.
status: open

### DW-718: Capture and independently review the real C1.15 Production runtime/control-plane identity packet.

origin: Story 27.21 C1.15 producer identity hardening, 2026-09-01
location: artifacts/access-telemetry-c1/C1.15; _bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md` summary: Capture a complete immutable C1.15 packet from the approved PG-ONPREM-1 target and obtain independent review without treating producer success as gate acceptance. evidence: Repository fixtures prove fail-closed producer behavior only. The approved target is not currently eligible while either named lifecycle Deployment (`memories-access-telemetry` and `memories-access-telemetry-clock`) remains zero-scaled or the Production inputs lack the explicit `AccessTelemetryLifecycle__ComponentIsAlpha` / `AccessTelemetryLifecycle__AllowAlphaComponent` pair; no real packet or reviewer disposition was produced during this hardening pass. - ID: 27.21-C1.15-REAL-PACKET-REVIEW - Status: open - Source story: 27-21-runtime-and-control-plane-identity - Target artifact: `artifacts/access-telemetry-c1/C1.15` and `_bmad-output/implementation-artifacts/27-21-runtime-and-control-plane-identity.md` - Re-open trigger: the approved `jpiquot@local` / `hexalith-memories` target has at least one Ready lifecycle pod and explicit alpha values, allowing the exact operator command to run and an independent reviewer to record the C1.15 disposition. - Rationale: Cluster mutation, scaling, rollout, and synthesized Production evidence are outside this hardening scope; C1.15 therefore remains `pending` / `not complete`, Story 27.21 remains `in-progress`, and Production lifecycle writes remain disabled. Owner: Deployment Adapter Developer for capture; independent code/planning reviewer for disposition.
status: open

### DW-719: Decode and scan the complete Dapr metadata object for secret-shaped values before allowlist projection.

origin: migrated from legacy ledger (""), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-2.md
reason: The pre-existing collector scans raw metadata text and the allowlisted projection, so a Unicode-escaped canary in an unallowlisted property can be decoded and discarded between those checks without blocking; the packet remains secret-safe, but the original secret-shaped-output fail-closed contract is not fully enforced.
status: open

### DW-720: Two new `deferred-work.md` entries under `spec-24-9` (the syntactic-isolation-wording and classification-gap items added earlier in this same file, immediately preceding this section) use only the legacy free-text `source_spec`/`summary`/`evidence` shape, missing the `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` fields the file's own reformatted schema requires (as used by, e.g., the neighboring `DW-716`-`DW-718` entries and the reformatted `24.7-*` items).

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-09-01)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: Reviewer-confirmed inspection found that the two recent `spec-24-9-non-destructive-tenant-marker-diagnostics` entries used only the legacy free-text `source_spec`/`summary`/`evidence` shape and omitted the ledger schema's `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` fields. This pre-existing schema-migration drift in a shared governance file was authored by Story 24.9 rather than Story 27.3 and was deferred to that session or file owner for reformatting.
status: done 2026-09-01
resolution: already resolved: deferred-work.md:4997-5008 gives DW-714 and DW-715 canonical headings, origin, location, reason, and status fields.

### DW-721: A `correct-course` pass is needed to amend AC6's binding text (this story `:39` and its `epics.md` copy) to match the current staged-OpenBao verification design instead of the superseded vault-Component-substitution workaround it still literally describes.

origin: migrated from legacy ledger (""), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:39; _bmad-output/planning-artifacts/epics.md
reason: Commit `8a5fa3c6` (2026-08-08, landed under `spec-gh-29804293613-fix-production-deployment-verification`, outside Story 27.3) redesigned the lane to stage a real disposable OpenBao before scale-up; qualifying run `33400812038` (job `99516369413`, artifact `9761351293`) reports `secret-store-substitution.json` with `substitutionPerformed: false` and `substitutionVerified: true`, both Components still typed `secretstores.hashicorp.vault`, and a confirmed `200` health packet showing `dapr-statestore: Healthy`. This demonstrates the real OpenBao secret-resolution path that AC6 still calls unproven while making AC6's literal "the patch, the post-patch readback" clause structurally unreachable under the improved design; the 2026-09-01 review closed checkpoint C2 on this evidence, but the binding text still needs human-approved correction in both governed copies. Source story: 27-3-production-adapter-and-deployment-profile. Re-open trigger: an approved `correct-course` proposal rewrites AC6's patch/post-patch-readback clause and OpenBao-path disclosure in both governed copies.
status: open

### DW-722: The REST /api/v1/ingest DuplicateInFlight branch — the loser receiving the winner's instance id instead of scheduling a second workflow — has no test at any level.
origin: spec-deferred 5cecf9383b7b
location: src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:127
source_spec: `spec-dw-18-redis-ingest-race-proof.md`
severity: medium
reason: Grepping DuplicateInFlight across src/ and tests/ returns only IngestDedupReservation.cs, IngestDedupReservationTests.cs (substitute unit) and the new IngestDedupReservationIntegrationTests.cs. The endpoint branch at src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:127 that returns Accepted(IngestStatusLocation(winnerInstanceId)) is observed by none of them, so returning the caller's own instance id there — or falling through to a second ScheduleAsync — would not fail any test. Pre-existing: this bundle adds the class-level race proof and does not touch the endpoint.
status: open

### DW-723: IngestDedupReservation.TryReserveAsync and ReleaseAsync accept a CancellationToken and never pass it to any StackExchange.Redis call, so an aborted ingest request keeps waiting on Redis.
origin: spec-deferred 6eb563b84d8e
location: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs:71,116
source_spec: `spec-dw-18-redis-ingest-race-proof.md`
severity: medium
reason: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs declares `CancellationToken cancellationToken` on both public methods; the bodies call StringSetAsync, StringGetAsync and KeyDeleteAsync with no token overload and never read the parameter. The repository convention (project-context.md, "Async APIs carry CancellationToken - public async service/client methods should accept and pass through cancellation") is therefore only half met, and the dead parameter reads as if cancellation were honoured. Pre-existing: this test-only bundle does not touch the production class, and both new tests pass CancellationToken.None, which keeps the gap invisible.
status: open

### DW-724: Follow-up review still recommended for dw-redis-ingest-race-proof after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-dw-18-redis-ingest-race-proof.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260901-065621-43db; this entry preserves the lingering recommendation for a deliberate later review.
status: open

### DW-725: ID: 27.3-CR17 — production-deployment-verification lane never proved green against the reviewed Story 27.3 source; the entry recording its discharge was never written to this register.
origin: code review of 27-3-production-adapter-and-deployment-profile (2026-09-02), reconstructing a discharge the story file had cited at this ID since 2026-09-01 without a corresponding register entry
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:817 (C2 checkpoint row)
source_spec: n/a — Story 27.3 own checkpoint C2
severity: high
reason: C2 requires the `production-deployment-verification` job in `.github/workflows/ci.yml` to report `success` with no skipped render/apply/health/evidence-validation step, at a run/commit the story's reviewed source actually reaches. Independently verified 2026-09-02 via `gh run view 33400812038 --repo Hexalith/Hexalith.Memories --json status,conclusion,headSha` (`completed`/head SHA `8f8c00d57345394b470efd5f1148a361c4bf6731`, matching the story's cited reviewed HEAD `8f8c00d5`) and `gh api repos/Hexalith/Hexalith.Memories/actions/jobs/99516369413` — job `production-deployment-verification`, conclusion `success`, all four required steps (`Publish local release OCI archives`, `Verify disposable production rollout`, `Validate production deployment evidence`, `Upload production deployment evidence`) `success`, none skipped. `gh api repos/Hexalith/Hexalith.Memories/actions/runs/33400812038/artifacts` confirms artifact `9761351293`, `production-deployment-evidence`, `436886781` bytes, `expired: false` — matching the story's cited figures exactly. Disclosure: the overall workflow run's conclusion is `failure`, caused by the unrelated `test-unit-contract` job at the same run; this does not affect the `production-deployment-verification` job's own independent conclusion, which C2's text scopes to.
status: resolved 2026-09-02 by code review, on the evidence above

### DW-726: ID: 27.3-CR29 — the redesigned staged-OpenBao production-deployment-verification lane was never confirmed to actually exercise the OpenBao secret-resolution path; the entry recording its discharge was never written to this register.
origin: code review of 27-3-production-adapter-and-deployment-profile (2026-09-02), reconstructing a discharge the story file had cited at this ID since 2026-09-01 without a corresponding register entry
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:817,1039 (C2 checkpoint row and AC6 verification row)
source_spec: n/a — split from 27.3-CR17's Story 31.2 arm
severity: high
reason: Discharge requires the qualifying run's `secret-store-substitution.json` to show the production `secretstores.hashicorp.vault` Components resolving `secretKeyRef`s through a live OpenBao rather than a merge-patched substitute. Independently verified 2026-09-02 against run `33400812038` / job `99516369413` (see DW-725 for the run/job-level CI verification) — the artifact was not re-downloaded and re-parsed in this pass (436 MB; no local copy available), so the specific `substitutionPerformed: false` / `substitutionVerified: true` / `dapr-statestore: Healthy` packet contents the story cites are accepted on the job-level `success` conclusion plus the story's own detailed 2026-08-31 dev-story transcription of that packet, not independently re-parsed byte-for-byte in this pass.
status: resolved 2026-09-02 by code review, on the evidence above (job-level evidence independently verified; packet-level detail accepted from the story's existing transcription, not re-parsed)

### DW-727: ID: 27.3-CR30 — same reachable-OpenBao/secretKeyRef-resolution discharge condition as 27.3-CR29, tracked as a separate ID; the entry recording its discharge was never written to this register.
origin: code review of 27-3-production-adapter-and-deployment-profile (2026-09-02), reconstructing a discharge the story file had cited at this ID since 2026-09-01 without a corresponding register entry
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:817,1039 (C2 checkpoint row and AC6 verification row)
source_spec: n/a — same discharge evidence as 27.3-CR29
severity: high
reason: See DW-726 — same run/job/artifact evidence, same job-level-verified/packet-level-accepted disclosure.
status: resolved 2026-09-02 by code review, on the evidence above (job-level evidence independently verified; packet-level detail accepted from the story's existing transcription, not re-parsed)
