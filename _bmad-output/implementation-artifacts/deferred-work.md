# Deferred Work

The `location:` field identifies the affected repository path or component. In migrated entries it corresponds to the former `Target artifact:` field. When writing a new path list, put each repository-relative path on the same line with a semicolon and a space between paths, for example `src/Example.cs; tests/ExampleTests.cs`. Search those paths individually and preserve existing historical annotations. Use `n/a` for an entry with no recorded location.

### DW-1: `ID:` — unique entry id, exactly as referenced elsewhere (e.g. `12.4-RV6`,

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `ID:` — unique entry id, exactly as referenced elsewhere (e.g. `12.4-RV6`, `S11-FX`, `13.6-RV1`). Tests match the field value as a verbatim token; partial prose mentions and near-matches such as `12x4-RV6` or `112.4-RV6` do not count.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-2: `Status:` — one of `open`, `resolved`, `accepted`, or `carried-forward`. The

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Status:` — one of `open`, `resolved`, `accepted`, or `carried-forward`. The vocabulary is closed and lowercase. Synonyms such as `done`, `closed`, `fixed`, or `deferred-again` are not allowed and will fail validation.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-3: `Source story:` — the story key, retro key, or review pass that produced the

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Source story:` — the story key, retro key, or review pass that produced the entry (for example `12-4-baseline-failures-sweep`).
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-4: `Target artifact:` — the repository-relative path or planning artifact the entry

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: tools/test-release.ps1
reason: - `Target artifact:` — the repository-relative path or planning artifact the entry targets. For release-lane baseline entries, this points at the consumer that owns the release filter (typically `tools/test-release.ps1` or a parser test).
status: open
note: 2026-11-16 — Story 30.6 code review. Story 30.6 retired `tools/test-release.ps1` (D4), so no release-lane baseline consumer exists. The structured field contract (DW-1..DW-6) stays enforced by `DeferredWorkRegister_RealRepo_StructuredEntriesSatisfyFieldContract` in `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`.
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-5: `Re-open trigger:` — one sentence describing the event or evidence that would

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `Re-open trigger:` — one sentence describing the event or evidence that would re-open the entry. Required even for `resolved` and `accepted` so a future reviewer knows when the disposition no longer applies.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-6: One of `Evidence:` or `Rationale:` — `Evidence:` is required when `Status` is

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - One of `Evidence:` or `Rationale:` — `Evidence:` is required when `Status` is `resolved` and names the change (story, commit, test, or doc) that closes the risk; `Rationale:` is required when `Status` is `accepted` or `carried-forward` and explains why the risk remains intentionally.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-7: `open` — planned action is still needed.

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `open` — planned action is still needed.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-8: `resolved` — code, test, or documentation evidence shows the risk no longer

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `resolved` — code, test, or documentation evidence shows the risk no longer applies.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-9: `accepted` — the risk remains but is intentionally accepted with a written

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `accepted` — the risk remains but is intentionally accepted with a written rationale.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-10: `carried-forward` — the risk remains and has been moved to a named future

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: n/a
reason: - `carried-forward` — the risk remains and has been moved to a named future artifact, story, or trigger.
status: open
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-11: `Test:` — fully-qualified `Class.Method` name when the entry is paired with a

origin: migrated from legacy ledger ("Schema for Active Entries"), 2026-09-01
location: tools/test-release.ps1
reason: - `Test:` — fully-qualified `Class.Method` name when the entry is paired with a release-lane filter in `tools/test-release.ps1`. Required when `Target artifact` references the release-lane test script.
status: open
note: 2026-11-16 — Story 30.6 code review. The `tools/test-release.ps1` release-lane pairing was retired with the runner (D4). The structured reader still recognises a legacy `Test:` field, but no current check pairs it with a release filter.
note: 2026-11-24 — Skipped. This is an imported schema rule with no standalone change to implement.

### DW-12: Case activity stream append and summary-hash update should be made atomic if duplicate retry side effects become observable.

origin: migrated from legacy ledger ("Story 24.5 Review Deferred Items (2026-07-06)"), 2026-09-01
location: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md` summary: Case activity stream append and summary-hash update should be made atomic if duplicate retry side effects become observable. evidence: Story 24.5 bounded the stream and added summary backfill for missing legacy summaries, but `XADD` and summary hash updates remain separate Redis operations, so a failure between them can leave a read-model summary temporarily stale until rebuild/backfill. - ID: 24.5-CASE-ACTIVITY-ATOMIC-SUMMARY - Status: open - Source story: 24-5-hot-path-write-amplification-cleanup - Target artifact: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs - Re-open trigger: duplicate case activity appends, stale failed-count/last-activity summaries, or Redis partial-write telemetry are observed after Story 24.5 ships. - Evidence: Review pass found `RecordEventAsync` writes the stream and summary hash separately; Story 24.5 mitigated missing legacy summaries with backfill but did not introduce Lua/transactional atomicity for this projection summary.
status: open
note: 2026-11-24 — Skipped. The recorded trigger requires observed duplicate appends or stale summaries, and no such incident is linked.

### DW-13: `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
archived: 2026-11-23

### DW-14: `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
archived: 2026-11-23

### DW-15: `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
archived: 2026-11-23

### DW-16: Other active `open` or `carried-forward` entries are classified by Story 19.1

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred Register Backlog Home Rollup (2026-06-30)"), 2026-09-01
archived: 2026-11-23

### DW-17: 20.5-A41-ACCESS-TELEMETRY-RETENTION: carried-forward. Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision.

status: done 2026-11-01
origin: migrated from legacy ledger ("Story 20.5 Deferred Retention Slice (2026-07-04)"), 2026-09-01
archived: 2026-11-23

### DW-18: 18.4-REDIS-RACE: accepted. Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

status: done 2026-09-01
resolution: resolved by sweep bundle dw-redis-ingest-race-proof
resolution-undo: 359d6619c61ffa57f0fd81f56e167e58a181ed120faa003eb485b6f0fda64b4a 2026-09-01 7374617475733a206f70656e
origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
archived: 2026-11-23

### DW-19: 18.8-DAPR-SMOKE: accepted. Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

status: done 2026-09-01
origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
archived: 2026-11-23

### DW-20: 18.4-TOKEN-EDGE: accepted. Story 18.4 token-anchoring edge: a token whose first use falls back to a pre-existing `sourceUri` unit relies on the 24h reservation key rather than the permanent dedup record.

origin: migrated from legacy ledger ("New accepted-debt entries (Epic 18 retrospective Action Item 4)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs (idempotency-token anchoring path)
reason: - **18.4-TOKEN-EDGE - accepted.** Story 18.4 token-anchoring edge: a token whose first use falls back to a pre-existing `sourceUri` unit relies on the 24h reservation key rather than the permanent dedup record. - ID: 18.4-TOKEN-EDGE - Status: accepted - Source story: 19-1-deferred-register-active-entry-classification-sweep - Target artifact: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs (idempotency-token anchoring path) - Re-open trigger: a token whose first use falls back to a pre-existing `sourceUri` unit relying on the 24h reservation key (not a permanent record) causes a real dedup/idempotency defect, or a hardening story is scheduled. - Rationale: tokens augment and never replace the permanent source-URI dedup record, so the edge is a known narrow case accepted until it produces a real defect or a hardening story is scheduled. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]
status: open
note: 2026-11-24 — Skipped. The accepted token-anchoring tradeoff needs a deduplication defect or a dedicated hardening story.

### DW-21: 12.4-RV20: accepted. Strict literal per-SHA replay evidence is a

origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **12.4-RV20 - accepted.** Strict literal per-SHA replay evidence is a release-quality proof candidate, not a runtime defect. Ancestry-based HEAD-inheritance proof remains acceptable for existing close-out evidence; Story 19.3 declines to schedule a strict replay evidence story now. - ID: 12.4-RV20 - Status: accepted - Source story: 15-5-deferred-register-triage-sweep - Target artifact: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A release post-mortem traces a regression to a test that existed at one of the named anchor SHAs but was silently fixed before HEAD, or a release-quality story explicitly requests strict literal replay evidence over ancestry-based proof. - Rationale: Story 19.3 (2026-06-30) reviewed the release-evidence need (AC1) and accepts ancestry-based HEAD-inheritance proof as sufficient for current close-out evidence, declining to create the proposed "Strict Release Baseline Replay Evidence" story until the re-open trigger fires. This governance sweep does not run historical checkout/build/test lanes or mutate release tooling. Owner: release maintainer.
status: open
note: 2026-11-24 — Skipped. Story 19.3 accepted ancestry evidence, and no trigger justifies historical replay through the retired release lane.

### DW-22: 12.6-RV5: resolved. The `EmbeddingInputContentKindTests` telemetry

status: done 2026-09-01
origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
archived: 2026-11-23

### DW-23: Story-9.3-ProjectionRegistryCrossCheck: resolved. Handler mismatch

status: done 2026-09-01
origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
archived: 2026-11-23

### DW-24: 12.4-RV10: accepted. A parse-time warning for dropped bare-token bullets

status: done 2026-10-28
origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
archived: 2026-11-23

### DW-25: 12.4-RV11: accepted. Local Windows absolute-path cosmetic noise remains

status: done 2026-10-28
origin: migrated from legacy ledger ("Story 15.5 Triage Rollup (2026-05-15)"), 2026-09-01
archived: 2026-11-23

### DW-26: `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1:

status: done 2026-09-05
origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
archived: 2026-11-23

### DW-27: `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled

status: done 2026-09-05
origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
archived: 2026-11-23

### DW-28: `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live

status: done 2026-09-05
origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
archived: 2026-11-23

### DW-29: `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport

status: done 2026-09-01
origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
archived: 2026-11-23

### DW-30: `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator`

status: done 2026-09-01
origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
archived: 2026-11-23

### DW-31: The Epic 14 retrospective's "Preparation For The Next Work" note is stale

origin: migrated from legacy ledger ("Epic 14 Retrospective Reconciliation"), 2026-09-01
location: n/a
reason: - The Epic 14 retrospective's "Preparation For The Next Work" note is stale because Epic 15 now exists. This rollup records the reconciliation instead of rewriting retrospective history.
status: done 2026-11-24
resolution: The Epic 14 retrospective now links to the later Epic 15 plan and the Story 15.5 ownership reconciliation.

### DW-32: 15.5-RV1: `git diff --check` validation claim is inaccurate. Story 15.5 Dev Agent Record states `git diff --check ... passed with only expected LF-to-CRLF working-copy warnings`, but actual `git diff --check 9042c17..c2e575c` reports trailing-whitespace errors on `_15-5-review-diff.patch`. Re-open trigger: any future story's validation block reuses the same tolerance wording without verifying `--check` output is genuinely error-free.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV1 — `git diff --check` validation claim is inaccurate.** Story 15.5 Dev Agent Record states `git diff --check ... passed with only expected LF-to-CRLF working-copy warnings`, but actual `git diff --check 9042c17..c2e575c` reports trailing-whitespace errors on `_15-5-review-diff.patch`. Re-open trigger: any future story's validation block reuses the same tolerance wording without verifying `--check` output is genuinely error-free.
status: done 2026-11-23
resolution: Story 15.5 distinguishes the scoped check from the historical full-range failure and verifies the range after patch removal. See spec-ten-deferred-verification-references.md.

### DW-33: 15.5-RV2: `sprint-status.yaml:last_updated` not advanced when post-implementation commit `c2e575c` landed. Cosmetic drift; the dev-story timestamp `2026-05-15T12:45:15+02:00` predates the 15:55 follow-on commit. Re-open trigger: a tool starts treating `last_updated` as a freshness proxy across all commits on a story.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV2 — `sprint-status.yaml:last_updated` not advanced when post-implementation commit `c2e575c` landed.** Cosmetic drift; the dev-story timestamp `2026-05-15T12:45:15+02:00` predates the 15:55 follow-on commit. Re-open trigger: a tool starts treating `last_updated` as a freshness proxy across all commits on a story.
status: open
note: 2026-11-24 — Skipped. The sprint timestamp is current. The missing timestamp on the 2026-05-15 commit is historical, and backdating the current registry would be inaccurate.

### DW-34: 15.5-RV3: Task 3 prose "`13.1-RV6` and related provider work" is looser than the rollup's explicit 4-ID enumeration. `15-5-deferred-register-triage-sweep.md:79` lists one ID; `deferred-work.md:119` enumerates `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, `13.3-RV8`. Rollup is accurate; task description was provisional. Re-open trigger: a future story re-reads Task 3 prose as a complete ownership list and misses one of the four IDs.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV3 — Task 3 prose "`13.1-RV6` and related provider work" is looser than the rollup's explicit 4-ID enumeration.** `15-5-deferred-register-triage-sweep.md:79` lists one ID; `deferred-work.md:119` enumerates `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, `13.3-RV8`. Rollup is accurate; task description was provisional. Re-open trigger: a future story re-reads Task 3 prose as a complete ownership list and misses one of the four IDs.
status: done 2026-11-23
resolution: Story 15.5 Task 3 names all four provider findings assigned to Story 15.2. See spec-ten-deferred-verification-references.md.

### DW-35: 15.5-RV4: `Target artifact:` field uses `;`-joined multi-paths in `12.4-RV20` and `Story-9.3-ProjectionRegistryCrossCheck` blocks. `deferred-work.md:68,92` — the Story 14.5 schema describes `Target artifact:` as singular, but Story 15.4's `13.2-RV4` (line 178) and Story 15.2's HybridSearch entry (line 293) already use multi-path joined formats and the `CiTestInventoryTests` parser tolerates them (48/48 PASS). Pre-existing pattern; a Story-14.5-owned schema-cleanliness pass should either tighten the parser to require single-path values or formalize a multi-path separator. Re-open trigger: a parser regression where the joined string is treated as one literal path and a target-artifact filter misses a real consumer.

origin: migrated from legacy ledger ("Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)"), 2026-09-01
location: n/a
reason: - **15.5-RV4 — `Target artifact:` field uses `;`-joined multi-paths in `12.4-RV20` and `Story-9.3-ProjectionRegistryCrossCheck` blocks.** `deferred-work.md:68,92` — the Story 14.5 schema describes `Target artifact:` as singular, but Story 15.4's `13.2-RV4` (line 178) and Story 15.2's HybridSearch entry (line 293) already use multi-path joined formats and the `CiTestInventoryTests` parser tolerates them (48/48 PASS). Pre-existing pattern; a Story-14.5-owned schema-cleanliness pass should either tighten the parser to require single-path values or formalize a multi-path separator. Re-open trigger: a parser regression where the joined string is treated as one literal path and a target-artifact filter misses a real consumer.
status: done 2026-11-24
resolution: Documented the semicolon-and-space separator for multiple paths and how it relates to legacy Target artifact values.

### DW-36: 15.4-RV1: Sanitization-message assertions are tautological. `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-4-token-endpoint-transport-policy (2026-05-15)"), 2026-09-01
archived: 2026-11-23

### DW-37: 13.2-RV4: resolved. OIDC token endpoint validation now enforces HTTPS for production and

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.4 Token Endpoint Transport Policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-38: 13.6-RV1: resolved. Live migration cutover now writes a durable

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-39: 13.6-RV2: resolved. `IndexSemanticActivity.cs` now carries the standard

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-40: 13.6-RV3: accepted. The migration command keeps its local nullable

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV3 - accepted.** The migration command keeps its local nullable string/tuple helper shape for `ValidateOptions(...)` and `TryBuildTargetConfig(...)`, with `EmbeddingMigrationResult` plus stable exit codes as the project-approved equivalent for this command surface. - ID: 13.6-RV3 - Status: accepted - Source story: 15-3-live-migration-coordination-policy - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - Re-open trigger: Migration errors need `ApplicationError` metadata beyond a flat operator message, or `Hexalith.Memories.Server` adopts `Hexalith.Commons.ValueOrError<T>` as an approved dependency across this boundary. - Rationale: The helper results are private to `EmbeddingVectorMigrationService`, immediately converted into `EmbeddingMigrationResult`, and already produce automation-readable `Plumbing`, `DomainError`, and `Cancelled` exit codes with sanitized messages. Introducing `Hexalith.Commons.ValueOrError<T>` here would add cross-project reference churn without improving the public migration command contract.
status: open
note: 2026-11-24 — Skipped. The migration already exposes stable structured results and richer Commons metadata is outside this private helper scope.

### DW-41: 13.1-RV6: resolved. Provider validation now has a shared maximum

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-42: 13.1-RV10: accepted. Provider and model validation remains

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **13.1-RV10 - accepted.** Provider and model validation remains case-insensitive for compatibility, and caller-provided casing is preserved rather than normalized at validation time. - ID: 13.1-RV10 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: A persisted mixed-case provider/model value causes runtime dispatch, reindex detection, search metadata, or migration state comparisons to diverge. - Rationale: Ollama model tags may be case-sensitive outside the committed `qwen3-embedding:4b` model, so Story 15.2 keeps validation case-insensitive while preserving original values. Evidence is pinned by `EmbeddingProviderDefaultsTests.Validate_MixedCaseProviderAndModel_ShouldUseCaseInsensitiveRegistryLookup`; compatibility consumers continue to use `OrdinalIgnoreCase` where provider/model equality matters.
status: done 2026-11-23
resolution: Case-insensitive registry validation and the actor casing-only update test verify that registered provider and model spelling survives storage and does not trigger a reindex. See spec-ten-deferred-embedding-configuration.md.

### DW-43: 13.1-RV11: resolved. Provider/model/dimension validation now uses a

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-44: 13.3-RV8: accepted. The persisted provider/model parser continues to

origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
reason: - **13.3-RV8 - accepted.** The persisted provider/model parser continues to lowercase the provider and preserve the model string after the first colon. - ID: 13.3-RV8 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs - Re-open trigger: A persisted provider/model identifier with mixed casing fails a real runtime path, migration path, or equality comparison because provider and model casing are handled asymmetrically. - Rationale: Provider names are registry keys and safe to normalize for dispatch, while model tags can contain embedded colons and may be case-sensitive in provider-specific APIs. Story 15.2 pins the behavior with `EmbeddingClientTests.ParseEmbeddingProvider_NormalizesProviderAndPreservesModelAfterFirstColon` and leaves runtime parsing unchanged.
status: open
note: 2026-11-24 — Skipped. Existing parser and indexing tests protect legacy mixed-case identifiers, and no runtime divergence is linked.

### DW-45: 15.2-RV1: accepted. AC5 contract-tier serialization test not updated.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs
reason: - **15.2-RV1 - accepted.** AC5 contract-tier serialization test not updated. - ID: 15.2-RV1 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs - Re-open trigger: A casing/canonicalization change at the contract boundary is later considered. - Rationale: Task 2 chose `accepted` for casing semantics, so contract-tier serialization is intentionally unchanged. Recorded for traceability against AC5's "contract/server tests cover ... deferred-work dispositions" wording. Story 19.4 (2026-06-30) re-reviewed the current contract serializer against live code and accepts this until the contract boundary changes: the contract tests still preserve JSON shape and value round-trip, and provider/model validation lives in server/provider paths, not the contract serializer, so no contract-tier change is warranted now. Natural future home: a contract casing/canonicalization story, only if the contract boundary changes. Owner: Contracts / Server maintainer.
status: done 2026-11-23
resolution: TenantEmbeddingConfigSerializationTests verifies the raw JSON values and round-trip spelling for mixed-case Google and tagged Ollama configurations. See spec-ten-deferred-embedding-configuration.md.

### DW-46: 15.2-RV2: accepted. Actor reindex tests lost same-Provider/different-Model isolation.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs
reason: - **15.2-RV2 - accepted.** Actor reindex tests lost same-Provider/different-Model isolation. - ID: 15.2-RV2 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs - Re-open trigger: Registry adds a second model under Google or Ollama, OR `GetBreakingChangeFields` regresses to flag only provider changes. - Rationale: Tests now switch Provider AND Model (Google→Ollama) instead of Model-only; same-provider/model-change reindex-trigger coverage cannot be restored inside this story because the closed registry currently lists exactly one model per provider. Story 19.4 (2026-06-30) confirmed the closed registry still lists exactly one model per provider (Google + Ollama only), so same-provider/different-model reindex coverage stays trigger-bound and accepted until a supported provider gains a second model or `GetBreakingChangeFields(...)` regresses. Natural future home: provider-registry model-expansion tests when a second model lands under one provider. Owner: Server test maintainer.
status: open
note: 2026-11-24 — Skipped. Each provider still has only one registered model, so a valid model-only actor transition cannot be constructed.

### DW-47: 15.2-RV3: accepted. Other test files still use unregistered model literals.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-48: 15.2-RV4: accepted. `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-49: 15.2-RV5: accepted. `GenerateEmbeddingActivity` may emit mixed-case provider in persisted identifier.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs
reason: - **15.2-RV5 - accepted.** `GenerateEmbeddingActivity` may emit mixed-case provider in persisted identifier. - ID: 15.2-RV5 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs - Re-open trigger: A tenant persists `Provider = "Google"` (mixed case, now accepted by Validate) and an equality comparison or migration state diverges from the lowercased parsed form. - Rationale: Tenant-persisted casing is preserved, but `ParseEmbeddingProviderIdentifier` lowercases the provider on read — write and read forms can diverge. Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `GenerateEmbeddingActivity` writes the raw `$"{config.Provider}:{config.Model}"` form while the parser lowercases the provider, and the migration marker guard compares with `OrdinalIgnoreCase`, so this is accepted compatibility today, not a current defect. Accepted until a casing-sensitive equality or migration-state divergence is observed. Natural future home: a provider identifier canonicalization story covering write/read/migration equality (bundled with `15.2-RV4`/`15.2-RV6`). Owner: Server / ingestion maintainer.
status: done 2026-11-24
resolution: Single and chunked embedding activities lowercase the provider component of new identifiers and preserve the model spelling. All 104 focused activity and client tests passed.

### DW-50: 15.2-RV6: accepted. Migration tool uses binary Google/Ollama coin-flip, not registry.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **15.2-RV6 - accepted.** Migration tool uses binary Google/Ollama coin-flip, not registry. - ID: 15.2-RV6 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - Re-open trigger: Registry adds a third provider, OR a migration plan needs to target a new provider. - Rationale: `TargetProvider` defaults to `Ollama()` if not Google (lines 125-149). Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `EmbeddingVectorMigrationService.TryBuildTargetConfig(...)` still chooses Google defaults for a Google target and Ollama defaults otherwise, then relies on `EmbeddingProviderDefaults.Validate(...)` to reject unsupported providers — safe for two providers but a binary defaulting path, accepted until a third provider or a new migration target lands. Natural future home: a migration target factory/registry story for a third provider (bundled with `15.2-RV4`/`15.2-RV5`). Owner: Server / migration maintainer.
status: done 2026-11-23
resolution: EmbeddingVectorMigrationService gets target defaults from the configuration registry. Dry-run tests cover omitted, mixed-case registered, and invalid providers. See spec-ten-deferred-embedding-configuration.md.

### DW-51: 15.2-RV7: accepted. Whitespace-prefixed provider not trimmed before registry lookup.

origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
reason: - **15.2-RV7 - accepted.** Whitespace-prefixed provider not trimmed before registry lookup. - ID: 15.2-RV7 - Status: accepted - Source story: 15-2-provider-model-dimension-registry - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - Re-open trigger: Operator reports an unhelpful "Provider ' google' is not supported" error and the registry path needs to suggest the whitespace cause. - Rationale: `ArgumentException.ThrowIfNullOrWhiteSpace` accepts `" google"`, then `FindProvider` misses (no trim). Already family-deferred as `13.1-RV4`; surfaces again in the registry path. Story 19.4 (2026-06-30) accepts this until an operator UX issue justifies whitespace-specific diagnostics in `EmbeddingProviderDefaults.Validate(...)`. Natural future home: a provider validation UX cleanup if whitespace diagnostics become operator-visible. Owner: Server / ingestion maintainer.
status: done 2026-11-23
resolution: Provider validation gives a whitespace-specific Provider error for a padded registered name. Validator and dry-run migration tests pin the behavior. See spec-ten-deferred-embedding-configuration.md.

### DW-52: 15.2-RV8: accepted. Already-persisted invalid configs not surfaced on read.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-53: 15.2-RV9: accepted. "Order-sensitive metric test passed in isolation" acknowledged.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-54: S11-FC: resolved. Release execution now has a repository-owned stale-tag

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-55: 12.1-RV3: accepted. The repository removed its partial job-level

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-56: 12.1-RV4: resolved. The release restore contract is now explicitly

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-57: 15.1-RV1: accepted. Transient network failure in `Test-RemoteTagCollision` aborts the release lane.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV1 - accepted.** Transient network failure in `Test-RemoteTagCollision` aborts the release lane. - ID: 15.1-RV1 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: A release attempt fails because `git ls-remote` returns a transient network/DNS error and the preflight has no retry/backoff. - Rationale: The preflight currently has no retry. A DNS hiccup turns a recoverable error into a hard abort. Deferred because the right policy (number of retries, backoff window, idempotency boundary) is a release-owner decision rather than a clear patch. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no release has failed on it; the retry policy is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open
note: 2026-11-24 — Skipped. No qualifying release failure is linked, and the release owner has yet to choose a retry policy.

### DW-58: 15.1-RV2: accepted. Dry-run version regex hard-codes English semantic-release output.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tools/release-preflight.ps1
reason: - **15.1-RV2 - accepted.** Dry-run version regex hard-codes English semantic-release output. - ID: 15.1-RV2 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tools/release-preflight.ps1 - Re-open trigger: semantic-release ever rewords `The next release version is X.Y.Z` or ships i18n output that no longer matches the regex, and the preflight starts mis-detecting the next version. - Rationale: Bound to current semantic-release output. A more stable contract would be `semantic-release --dry-run --debug` JSON or a plugin hook, but switching is out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
status: open
note: 2026-11-24 — Skipped. The current parser contract still applies, and no release output drift is linked.

### DW-59: 15.1-RV3: accepted. Final `catch` block loses inner-exception and stack trace.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-60: 15.1-RV4: accepted. `CiTestInventoryTests` workflow-string assertions are brittle to cosmetic edits.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **15.1-RV4 - accepted.** `CiTestInventoryTests` workflow-string assertions are brittle to cosmetic edits. - ID: 15.1-RV4 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A future workflow edit needs `npm ci --ignore-scripts`, a renamed step name, or `pwsh ./tools/release-preflight.ps1` invocation form, and the existing strict `ShouldBe` assertions fail without a real contract violation. - Rationale: Exact-match `ShouldBe` for `Run`, `Name`, and `Shell` is consistent with the rest of `CiTestInventoryTests`. Loosening to `ShouldContain`/`ShouldStartWith` should be done as a sweep across the file, not in isolation. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: resolved 2026-10-28
resolution: `spec-five-deferred-ci-inventory.md` — Release operation lookup now matches a narrow command vocabulary instead of display names, accepts npm ci --ignore-scripts and explicit pwsh invocation, and retains ordering/blocking assertions. IsReleaseCommand_RecognizesOnlySupportedEquivalentOperations and GetReleaseWorkflowStep_DuplicateOperations_FailsLoudly cover positive and negative controls. Final inventory suite passed 117/117 cases and the complete CLI assembly passed 564/564, with no failures/skips.
reopen_trigger: A release workflow or deferred-register format change escapes or falsely fails the corresponding inventory regression.

### DW-61: 15.1-RV5: accepted. Windows tempdir cleanup can raise `PermissionError`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-62: 15.1-RV6: accepted. `Path | None` union syntax requires Python 3.10+.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-63: 15.1-RV7: accepted. Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-64: 15.1-RV8: accepted. `git init` default branch depends on host `init.defaultBranch`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-65: 15.1-RV9: accepted. Test runner hardcodes `pwsh` without availability guard.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-66: 15.1-RV10: accepted. Runbook release-day checklist renumbered to 17 items; other docs may reference old step numbers.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: docs/dev/release-runbook.md
reason: - **15.1-RV10 - accepted.** Runbook release-day checklist renumbered to 17 items; other docs may reference old step numbers. - ID: 15.1-RV10 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: docs/dev/release-runbook.md - Re-open trigger: A maintainer follows a stale "see step 7" cross-reference in CONTRIBUTING.md or another doc that no longer matches the renumbered checklist. - Rationale: A repo-wide `rg "step 7|step 8|step 9" docs/` sweep is warranted but out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: done 2026-11-24
resolution: The release runbook now links a named Release-Day Checklist for guarded dispatch and current assets. Reopen if maintained guidance points to retired step numbers. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-67: 15.1-RV11: accepted. `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/deferred-work.md
reason: - **15.1-RV11 - accepted.** `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path. - ID: 15.1-RV11 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md - Re-open trigger: The preflight script is renamed or relocated and the `S11-FC` re-open trigger silently fails to reference the right artifact. - Rationale: Use a more stable artifact phrasing like "the repository-owned release preflight script in `tools/`" if the script ever moves. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.
status: done 2026-11-24
resolution: The runbook Stale-Tag Preflight section gives S11-FC a path-independent trigger and names owner-approved collision handling. Reopen if the guarded release can publish after a collision or inconclusive tag read. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-68: 15.1-RV12: accepted. `12.1-RV3` accepted-until 2026-08-13 has no automated reminder.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-69: 15.1-RV13: accepted. `git show-ref --verify` allowed exit codes do not cover `128`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-70: 15.1-RV14: accepted. Peeled-only-ref remote response not fixtured.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-71: 15.1-RV15: accepted. `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
archived: 2026-11-23

### DW-72: 15.1-RV16: accepted. `GetReleaseWorkflowJobScalar` depends on 4-space indentation.

origin: migrated from legacy ledger ("Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
reason: - **15.1-RV16 - accepted.** `GetReleaseWorkflowJobScalar` depends on 4-space indentation. - ID: 15.1-RV16 - Status: accepted - Source story: 15-1-release-edge-case-preflight-hardening - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - Re-open trigger: A future `release.yml` reformat (2-space, tabs) silently passes the job-scalar contract test without actually inspecting the right scope. - Rationale: Structural YAML parsing would be ideal, but a hand-rolled prefix parser is consistent with the rest of `CiTestInventoryTests`. A broader test-helper sweep is the natural home. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.
status: resolved 2026-10-28
resolution: `spec-five-deferred-ci-inventory.md` — GetReleaseWorkflowJobScalar scopes block mappings by indentation and reads only immediate jobs.release properties. Width-1/2/4 fixtures exclude nested and sibling decoys; duplicate/missing jobs, duplicate scalars, tab indentation and unsupported inline/block forms fail loudly. Final inventory suite passed 117/117 cases and the complete CLI assembly passed 564/564, with no failures/skips.
reopen_trigger: A release workflow or deferred-register format change escapes or falsely fails the corresponding inventory regression.

### DW-73: 13.7-RV4: resolved. The AppHost and Aspire integration fixture now share the

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Deferred Work 13.7-RV4 Repository Root Locator Consolidation (2026-05-12)"), 2026-09-01
archived: 2026-11-23

### DW-74: 12.4-RV6: resolved. `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-75: 12.4-RV19: resolved. The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-76: 12.6-RV2: resolved. This entry explicitly realized 12.4-RV6 and is closed

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-77: 13.7-RV5: resolved. Sprint-status history hygiene is now a documented

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-78: 13.6-RV4: closed. `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-79: 13.6-RV5: closed. Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-80: 13.7-RV1: closed. `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-81: 13.7-RV2: closed. Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-82: 13.7-RV3: closed. `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-83: 13.7-RV6: closed. `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-84: 13.7-RV7: closed. The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-85: 13.6-RV1: carried forward. Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.

status: done 2026-09-01
origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-86: 13.6-RV3: carried forward. `EmbeddingVectorMigrationService` retains string-shaped error returns from `ValidateOptions` and `TryBuildTargetConfig` and routes all operator-visible failures through the structured `EmbeddingMigrationResult` surface. Adopting Hexalith's `ValueOrError<T>` convention requires a project reference to `Hexalith.Commons` (`src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` + `ApplicationError.cs`), which is in this story's forbidden-by-default file scope and would cascade through `Hexalith.Memories.Server`'s reference graph. The internal helpers feed exactly one consumer (the orchestrator) which immediately wraps each message into the public `EmbeddingMigrationResult`, so the local string shape is structurally equivalent to a `ValueOrError<T>` for this surface. Re-open trigger: a Hexalith-wide audit of result-pattern adoption that drops the `Hexalith.Commons` cross-project boundary, or any feature that needs to surface migration errors with `ApplicationError`'s richer shape (Title/Detail/TechnicalDetail/Arguments/Category) rather than a flat operator sentence.

origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
location: src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs
reason: - **13.6-RV3 — carried forward.** `EmbeddingVectorMigrationService` retains string-shaped error returns from `ValidateOptions` and `TryBuildTargetConfig` and routes all operator-visible failures through the structured `EmbeddingMigrationResult` surface. Adopting Hexalith's `ValueOrError<T>` convention requires a project reference to `Hexalith.Commons` (`src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` + `ApplicationError.cs`), which is in this story's forbidden-by-default file scope and would cascade through `Hexalith.Memories.Server`'s reference graph. The internal helpers feed exactly one consumer (the orchestrator) which immediately wraps each message into the public `EmbeddingMigrationResult`, so the local string shape is structurally equivalent to a `ValueOrError<T>` for this surface. Re-open trigger: a Hexalith-wide audit of result-pattern adoption that drops the `Hexalith.Commons` cross-project boundary, or any feature that needs to surface migration errors with `ApplicationError`'s richer shape (Title/Detail/TechnicalDetail/Arguments/Category) rather than a flat operator sentence.
status: open
note: 2026-11-24 — Skipped. Duplicates the migration error-shape decision in DW-40, which requires wider dependency scope.

### DW-87: 13.7-RV4: resolved 2026-05-12. Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.

status: done 2026-09-01
origin: migrated from legacy ledger ("Carried forward by Story 14.4 (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-88: 13.2-RV1: closed. `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-89: 13.2-RV2: closed. `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-90: 13.2-RV3: closed. `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-91: 13.2-RV5: closed. `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-92: 13.2-RV6: closed. Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-93: 13.3-RV6: closed. Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-94: 13.3-RV7: closed. `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-95: 13.3-RV11: closed. `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-96: 13.3-RV12: closed. `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-97: 13.3-RV14: closed. `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-98: 13.3-RV15: closed. `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-99: 13.4-RV5: closed. `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-100: 13.6-RV1: Concurrent ingestion racing the migration. `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-101: 13.6-RV2: Pre-existing missing copyright header. `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-102: 13.6-RV3: Migration tool surfaces use ad-hoc string error returns + exit codes rather than `ValueOrError<T>`. `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — `ValidateOptions` returns `string?`; result construction is manual. Hexalith convention prefers `ValueOrError<T>` for expected business failures. Re-open trigger: a refactor of the migration service surface, or a Hexalith-wide audit of result-pattern adoption.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
location: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
reason: - **13.6-RV3 — Migration tool surfaces use ad-hoc string error returns + exit codes rather than `ValueOrError<T>`.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — `ValidateOptions` returns `string?`; result construction is manual. Hexalith convention prefers `ValueOrError<T>` for expected business failures. Re-open trigger: a refactor of the migration service surface, or a Hexalith-wide audit of result-pattern adoption.
status: open
note: 2026-11-24 — Skipped. Duplicates DW-40 and DW-86, whose richer error shape depends on a cross-module decision.

### DW-103: 13.6-RV4: Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-104: 13.6-RV5: Redactor skips `client_secret named foo` style strings without `:` or `=` separator. `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.6 (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-105: 13.5-RV1: `Hexalith.EventStore` submodule pointer bump bundled in feat commit. Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. Process note: future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-106: 13.5-RV2: AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline. `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-107: 13.5-RV3: No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119` — existing Model/Dimensions breaking-change coverage uses `EmbeddingProviderDefaults.Google()` only; Ollama-specific `Validate(...)` ceilings (qwen3 dim lock at 2560, rate-limit ceiling 60_000) are exercised in `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dim/provider breaking-change matrix grows.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119
reason: - **13.5-RV3 — No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119` — existing Model/Dimensions breaking-change coverage uses `EmbeddingProviderDefaults.Google()` only; Ollama-specific `Validate(...)` ceilings (qwen3 dim lock at 2560, rate-limit ceiling 60_000) are exercised in `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dim/provider breaking-change matrix grows.
status: done 2026-11-24
resolution: Actor tests now cover both Google/Ollama switch directions, exact conflict fields, no unforced write, and one forced write with the reindex flag. All 244 actor and registry tests passed.

### DW-108: 13.5-RV4: Legacy `provider="ollama"` payload with missing OIDC fields not exercised. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-109: 13.5-RV5: Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-110: 13.5-RV6: `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals. `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-111: 13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-112: 13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-113: 13.3-RV8: Asymmetric provider/model casing in parser output. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146` — Provider lowercased, model preserved verbatim. May be intentional (Ollama tags can be case-sensitive). Re-open trigger: Story 13.4 / 13.5 introduces a persisted-config consumer that needs round-trip equality.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146
reason: - **13.3-RV8 — Asymmetric provider/model casing in parser output.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146` — Provider lowercased, model preserved verbatim. May be intentional (Ollama tags can be case-sensitive). Re-open trigger: Story 13.4 / 13.5 introduces a persisted-config consumer that needs round-trip equality.
status: done 2026-11-23
resolution: The identifier parser tests verify that the provider normalizes and the tagged model retains its spelling. IndexSemanticActivityTests verifies acceptance by the runtime migration marker guard and verifies the spelling of the written fields. See spec-ten-deferred-embedding-configuration.md.

### DW-114: 13.3-RV9: No per-tenant circuit-breaker on persistent OIDC 401s. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234` — AC5 mandates "exactly once" per request. Across many requests with a misconfigured client, each call still hits the IdP. Re-open trigger: a production incident where Keycloak traffic spikes correlate with embedding 401 storms.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234
reason: - **13.3-RV9 — No per-tenant circuit-breaker on persistent OIDC 401s.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234` — AC5 mandates "exactly once" per request. Across many requests with a misconfigured client, each call still hits the IdP. Re-open trigger: a production incident where Keycloak traffic spikes correlate with embedding 401 storms.
status: open
note: 2026-11-24 — Skipped. The trigger requires a production OIDC retry storm, and circuit-breaker timing and tenant partitioning need an operational decision.

### DW-115: 13.3-RV10: No 429/Retry-After test on the Ollama path. `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-116: 13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-117: 13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-118: 13.3-RV13: `BaseUrl` with query string or fragment silently dropped. `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-119: 13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-120: 13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-121: 13.4-RV1: `RateLimitPerMinute` boundary / arithmetic overflow concerns. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161` — Validator caps the value but downstream arithmetic on it is not audited. Pre-existing. Re-open trigger: any throughput refactor that multiplies rate by a window size or uses it in token-bucket math.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161
reason: - **13.4-RV1 — `RateLimitPerMinute` boundary / arithmetic overflow concerns.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161` — Validator caps the value but downstream arithmetic on it is not audited. Pre-existing. Re-open trigger: any throughput refactor that multiplies rate by a window size or uses it in token-bucket math.
status: done 2026-11-23
resolution: The rate limiter uses a fixed one-minute window, a minimum clamp, and a single decrement. RateLimiterLogicTests exercises the validated 60,000 Ollama ceiling before and at refill. See spec-ten-deferred-embedding-configuration.md.

### DW-122: 13.4-RV2: `OidcScope` whitespace-only not validated. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — Spec leaves `OidcScope` optional and unvalidated; a non-null whitespace-only value would silently flow into the token request and surface as `invalid_scope` from Keycloak. Out of story scope. Re-open trigger: Story 13.2 / 13.3 surfaces an IdP-side regression caused by malformed scope.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181
reason: - **13.4-RV2 — `OidcScope` whitespace-only not validated.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — Spec leaves `OidcScope` optional and unvalidated; a non-null whitespace-only value would silently flow into the token request and surface as `invalid_scope` from Keycloak. Out of story scope. Re-open trigger: Story 13.2 / 13.3 surfaces an IdP-side regression caused by malformed scope.
status: done 2026-11-23
resolution: OidcTokenProviderTests verifies that a blank or omitted scope on the first request omits the form field and shares the unscoped cache entry. See spec-ten-deferred-embedding-configuration.md.

### DW-123: 13.4-RV3: OIDC mode does not enforce `ApiSecretKeyName` distinctness/role. `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-124: 13.4-RV4: No assertion that endpoint paths invoke `Validate`. `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-125: 13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.4 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-126: 13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-127: 13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-128: 13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-129: 13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-130: 13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-131: 13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-132: 13.2-RV7: Unbounded `_cache` and `_guards` growth + undisposed `SemaphoreSlim`. `OidcTokenProvider.cs:24-25` — singleton dictionary growth is bounded by unique `(endpoint, clientId, scope)` tuples but never evicted; semaphores in `_guards` are never `Dispose()`'d. Re-open trigger: a long-running tenant churn scenario or a leak diagnostic that traces growth to this provider.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV7 — Unbounded `_cache` and `_guards` growth + undisposed `SemaphoreSlim`.** `OidcTokenProvider.cs:24-25` — singleton dictionary growth is bounded by unique `(endpoint, clientId, scope)` tuples but never evicted; semaphores in `_guards` are never `Dispose()`'d. Re-open trigger: a long-running tenant churn scenario or a leak diagnostic that traces growth to this provider.
status: done 2026-11-24
resolution: The token cache is capped at 1,024 keys, evicts expired or nearest-to-expiry entries, and retains existing detached-fetch cleanup. All 68 focused cache tests passed.

### DW-133: 13.2-RV8: `JsonDocument.Parse` materializes adversarial large bodies; `InvalidOperationException.Message` may pass provider-controlled text. `OidcTokenProvider.cs:188,217` — a 200 OK with an outsized body is read fully before parsing; provider-supplied `tokenType`/`propertyName` values appear inside the malformed-response reason string (then in the typed exception message). Modern HttpClient defaults bound the buffer at 2 MiB so practical risk is low. Re-open trigger: an SLA pass that wants explicit `MaxResponseContentBufferSize` or a sanitization audit on exception text.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
location: n/a
reason: - **13.2-RV8 — `JsonDocument.Parse` materializes adversarial large bodies; `InvalidOperationException.Message` may pass provider-controlled text.** `OidcTokenProvider.cs:188,217` — a 200 OK with an outsized body is read fully before parsing; provider-supplied `tokenType`/`propertyName` values appear inside the malformed-response reason string (then in the typed exception message). Modern HttpClient defaults bound the buffer at 2 MiB so practical risk is low. Re-open trigger: an SLA pass that wants explicit `MaxResponseContentBufferSize` or a sanitization audit on exception text.
status: done 2026-11-24
resolution: Token responses buffer at most 64 KiB. Malformed successful responses use fixed diagnostics without raw bodies, and both declared and undeclared oversized bodies are rejected. All 146 focused provider and client tests passed.

### DW-134: 13.2-RV9: `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot. `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-135: 13.1-RV1: `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body. Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-136: 13.1-RV2: `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects". Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-137: 13.1-RV3: `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google. Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-138: 13.1-RV4: Provider whitespace UX gap. `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but obscures the leading-whitespace root cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104
reason: - **13.1-RV4 — Provider whitespace UX gap.** `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but obscures the leading-whitespace root cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
status: done 2026-11-23
resolution: Registered provider names with leading or trailing whitespace now report the exact validation problem. EmbeddingProviderDefaultsTests covers both edges and both providers. See spec-ten-deferred-embedding-configuration.md.

### DW-139: 13.1-RV5: Per-provider rate-limit ternary fragile for future providers. `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-140: 13.1-RV6: `Dimensions = int.MaxValue` accepted. Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-141: 13.1-RV7: `GetBreakingChangeFields` case-sensitivity contract not pinned by tests. Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-142: 13.1-RV8: No null-config test for `Validate(null!)`. `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-143: 13.1-RV9: Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site. Spec rationale exists but the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at `Ollama()`'s `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57
reason: - **13.1-RV9 — Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site.** Spec rationale exists but the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at `Ollama()`'s `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)
status: done 2026-11-23
resolution: The Ollama factory documents why its 6,000 requests-per-minute default is lower than the registry ceiling. See spec-ten-deferred-embedding-configuration.md.

### DW-144: 13.1-RV10: Mixed-case provider/model strings persisted verbatim. `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)

origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104
reason: - **13.1-RV10 — Mixed-case provider/model strings persisted verbatim.** `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
status: done 2026-11-23
resolution: TenantConfigurationActorTests verifies that a casing-only Ollama update preserves provider and model spelling and retains the durable reindex flag for both prior states. See spec-ten-deferred-embedding-configuration.md.

### DW-145: 13.1-RV11: Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4). The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. Action when 13.4 lands: (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. Re-open trigger if 13.4 ships without: any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-13.1 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-146: 12.6-RV1: Real-repo positive parser canary lost. `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` and `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` both now expect empty against the real repo files; only fixture-shaped tests prove `ReadOpenDeferredBaselines` parses anything. Add a smoke test that exercises the parser against a fixture mirroring the current `deferred-work.md` (or a separate non-baseline open `S11-F*` entry) so future structural drift in the file format is caught loudly. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224
reason: - **12.6-RV1 — Real-repo positive parser canary lost.** `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` and `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` both now expect empty against the real repo files; only fixture-shaped tests prove `ReadOpenDeferredBaselines` parses anything. Add a smoke test that exercises the parser against a fixture mirroring the current `deferred-work.md` (or a separate non-baseline open `S11-F*` entry) so future structural drift in the file format is caught loudly. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224`)
status: resolved 2026-10-28
resolution: `spec-five-deferred-ci-inventory.md` — ReadOpenDeferredBaselines_RealLedgerWithPositiveCanary_ReturnsExactPair appends a complete synthetic canary to an in-memory copy of the actual register and asserts its exact ID/test pair. The real-ledger empty-inventory controls remain unchanged. Final inventory suite passed 117/117 cases and the complete CLI assembly passed 564/564, with no failures/skips.
reopen_trigger: A release workflow or deferred-register format change escapes or falsely fails the corresponding inventory regression.

### DW-147: 12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-148: 12.6-RV3: Single-item parser fixture masks over-matching. `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` exercises exactly one comment + one filter line and uses `ShouldHaveSingleItem()`, which would pass even if the parser is matching on the wrong line and dedupes. Strengthen with a 2-filter fixture or one with a comment-line that resembles a filter, to verify the proximity guard and uniqueness logic. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214
reason: - **12.6-RV3 — Single-item parser fixture masks over-matching.** `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` exercises exactly one comment + one filter line and uses `ShouldHaveSingleItem()`, which would pass even if the parser is matching on the wrong line and dedupes. Strengthen with a 2-filter fixture or one with a comment-line that resembles a filter, to verify the proximity guard and uniqueness logic. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214`)
status: resolved 2026-10-28
resolution: `spec-five-deferred-ci-inventory.md` — ReadAcceptedReleaseFilters_TwoPairsAndCommentDecoys_PreserveExactAssociations pins two distinct pairs with misleading comment directives; comment-only directives are ignored. ReadAcceptedReleaseFilters_SecondFilterWithoutFreshKey_FailsLoudly preserves per-filter key consumption. Final inventory suite passed 117/117 cases and the complete CLI assembly passed 564/564, with no failures/skips.
reopen_trigger: A release workflow or deferred-register format change escapes or falsely fails the corresponding inventory regression.

### DW-149: 12.6-RV4: Discoverability breadcrumb removed from `tools/test-release.ps1`. Along with the `$projectFilters` block went the only on-script reference to deferred-work bookkeeping. Consider a one-line trailing comment such as `# No per-project baseline waivers; if one becomes necessary, register it in _bmad-output/implementation-artifacts/deferred-work.md and pair it here.` so a future maintainer searching the script for "baseline" finds the policy. (`tools/test-release.ps1:25`)

origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
location: tools/test-release.ps1
reason: - **12.6-RV4 — Discoverability breadcrumb removed from `tools/test-release.ps1`.** Along with the `$projectFilters` block went the only on-script reference to deferred-work bookkeeping. Consider a one-line trailing comment such as `# No per-project baseline waivers; if one becomes necessary, register it in _bmad-output/implementation-artifacts/deferred-work.md and pair it here.` so a future maintainer searching the script for "baseline" finds the policy. (`tools/test-release.ps1:25`)
status: done 2026-11-24
resolution: The current tools/test.ps1 header points to Release Test Baseline and the deferred-work ledger. Reopen if operators can no longer locate the baseline waiver records from the maintained runner. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-150: 12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.6 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-151: 12.5-RV1: Workflow hardcodes summary path. `.github/workflows/release.yml:75` literal `artifacts/packages/release/publish-summary.json` is a duplicate of the path computed by `tools/publish-nuget.ps1` from its `-PackageDirectory` parameter. Aligned today via `.releaserc.json` invocation; if either ever changes the alert silently no-ops.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: .github/workflows/release.yml:75
reason: - **12.5-RV1 — Workflow hardcodes summary path.** `.github/workflows/release.yml:75` literal `artifacts/packages/release/publish-summary.json` is a duplicate of the path computed by `tools/publish-nuget.ps1` from its `-PackageDirectory` parameter. Aligned today via `.releaserc.json` invocation; if either ever changes the alert silently no-ops.
status: open
note: 2026-11-24 — Skipped. Removing the hardcoded workflow path needs a coordinated release producer and CI/CD workflow change.

### DW-152: 12.5-RV2: `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`. `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-153: 12.5-RV3: Race on concurrent partial-publish runs. Two workflow runs hitting partial-publish for the same version simultaneously can each create a new issue. `tools/create-partial-publish-issue.ps1:92-119`. Workflow `concurrency: release` reduces but does not eliminate the window. Needs server-side dedupe (search-with-create-or-comment idempotency) before becoming critical.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92-119
reason: - **12.5-RV3 — Race on concurrent partial-publish runs.** Two workflow runs hitting partial-publish for the same version simultaneously can each create a new issue. `tools/create-partial-publish-issue.ps1:92-119`. Workflow `concurrency: release` reduces but does not eliminate the window. Needs server-side dedupe (search-with-create-or-comment idempotency) before becoming critical.
status: open
note: 2026-11-24 — Skipped. The issue API has no atomic lookup-and-create operation, so a fix needs a shared release lock or CI workflow change.

### DW-154: 12.5-RV4: `Format-ListSection` does not skip `$null` items. `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-155: 12.5-RV5: No retry/backoff for transient `gh` failures in the alert path. `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-156: 12.5-RV6: Malformed `publish-summary.json` makes the alert step throw. `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-157: 12.5-RV7: Empty stdout from `gh issue list` trips `ConvertFrom-Json`. `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-158: 12.5-RV8: Closed-then-reopened partial-publish issue creates a duplicate. `tools/create-partial-publish-issue.ps1:92` filters `--state open` only. After a maintainer manually reconciles and closes the issue, a same-version rerun creates a new issue rather than reopening or commenting. Spec is silent; semantics may need refinement after first real reconciliation cycle.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.5 (2026-05-02)"), 2026-09-01
location: tools/create-partial-publish-issue.ps1:92
reason: - **12.5-RV8 — Closed-then-reopened partial-publish issue creates a duplicate.** `tools/create-partial-publish-issue.ps1:92` filters `--state open` only. After a maintainer manually reconciles and closes the issue, a same-version rerun creates a new issue rather than reopening or commenting. Spec is silent; semantics may need refinement after first real reconciliation cycle.
status: done 2026-11-24
resolution: The alert helper reopens exact-title closed incidents and appends the current evidence. The fake-GitHub tests cover search lag, an existing open match, lost responses and verified reopen retries. Reopen if a same-version alert opens a duplicate instead of reusing an existing exact match. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.
decision: 2026-09-01 Reopen and comment — Search open and closed exact-title matches, reopen the prior incident, append evidence, and test reruns.
decision: 2026-09-01 Reopen and comment — Search open and closed exact-title matches, reopen the prior incident, append evidence, and test reruns.

### DW-159: 12.3-RV1: CI duplicate runs on `pull_request` and `push`. Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-160: 12.3-RV2: CI fork-PR base SHA reachability. `actions/checkout@v6` with `fetch-depth: 0` on the head ref does not fetch the base repo's history. `git diff "$base_sha" "$head_sha"` may fail on PRs from forks. Deferred until fork PRs are accepted; if needed, add an explicit `git fetch origin "$base_sha"` step.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .github/workflows/ci.yml:43-46
reason: - **12.3-RV2 — CI fork-PR base SHA reachability.** `actions/checkout@v6` with `fetch-depth: 0` on the head ref does not fetch the base repo's history. `git diff "$base_sha" "$head_sha"` may fail on PRs from forks. Deferred until fork PRs are accepted; if needed, add an explicit `git fetch origin "$base_sha"` step. [.github/workflows/ci.yml:43-46]
status: open
note: 2026-11-24 — Skipped. Fork-specific checkout changes need a confirmed fork-PR workflow and CI/CD ownership review.

### DW-161: 12.3-RV3: CI brand-new branch first push enumerates entire head commit. `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-162: 12.3-RV4: Branch with no story key + no `Story:` trailer fails closed across all CI. No allowlist for automation branches (dependabot, renovate). Defer until automation PRs are configured for this repo.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:168-172
reason: - **12.3-RV4 — Branch with no story key + no `Story:` trailer fails closed across all CI.** No allowlist for automation branches (dependabot, renovate). Defer until automation PRs are configured for this repo. [tools/check-story-file-scope.py:168-172]
status: done 2026-11-24
resolution: The shared validator explicitly treats a missing owner as a no-op. CONTRIBUTING.md records the current rule, and the dependency-automation fixture proves an absent owner passes while an invalid owner fails.

### DW-163: 12.3-RV5: `commit-msg` re-reads index after pre-commit may have modified it. File set seen by the two hooks can differ in environments that auto-format during pre-commit. Low impact in this repo; reconsider if formatters are introduced.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/commit-msg:12
reason: - **12.3-RV5 — `commit-msg` re-reads index after pre-commit may have modified it.** File set seen by the two hooks can differ in environments that auto-format during pre-commit. Low impact in this repo; reconsider if formatters are introduced. [.githooks/commit-msg:12]
status: done 2026-11-24
resolution: Both hooks read the current index at their own phase. The real Git commit fixture stages a file during pre-commit and proves all three message checks receive the same refreshed snapshot.

### DW-164: 12.3-RV6: `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages. Edge case; clean error wrapping is a follow-up nicety.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-165: 12.3-RV7: `--changed-files-file` does not strip a UTF-8 BOM. A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-166: 12.3-RV8: `collect_changed_files` silently drops paths normalizing to empty. Theoretical; `..`-only inputs are not produced by `git diff --name-only`.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:250
reason: - **12.3-RV8 — `collect_changed_files` silently drops paths normalizing to empty.** Theoretical; `..`-only inputs are not produced by `git diff --name-only`. [tools/check-story-file-scope.py:250]
status: done 2026-11-24
resolution: The scope validator rejects a nonblank operand that normalizes to no path. The changed-file input fixture checks invalid entries, and verifies that a genuinely empty diff still passes.

### DW-167: 12.3-RV9: Pre-commit fails closed during rebase / cherry-pick / detached-HEAD. `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-168: 12.3-RV10: `python` fallback may land on Python 2 on legacy systems. Not a target environment today.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:13-17
reason: - **12.3-RV10 — `python` fallback may land on Python 2 on legacy systems.** Not a target environment today. [.githooks/pre-commit:13-17]
status: done 2026-11-24
resolution: Both hooks use the shared Python 3.9 or newer interpreter selection. The staged-path fixture checks recovery guidance with an incompatible interpreter and successful use of a compatible python fallback.

### DW-169: 12.3-RV11: Hooks consume newline-separated `git diff --name-only` output. Filenames containing newlines (legal POSIX) mishandled. No such filenames in repo.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: .githooks/pre-commit:12
reason: - **12.3-RV11 — Hooks consume newline-separated `git diff --name-only` output.** Filenames containing newlines (legal POSIX) mishandled. No such filenames in repo. [.githooks/pre-commit:12]
status: done 2026-11-24
resolution: Both hooks use NUL-delimited staged path records and preserve both rename endpoints. The real Git fixtures cover spaces, Unicode, newlines and the forbidden-path decision.

### DW-170: 12.3-RV12: `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check. Backslashes get normalized away before the test. Pair with override-vagueness rework P6.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-171: 12.3-RV13: `parse_allowed_scope` does not honor `## ` subheadings inside `## File Scope`. No current story uses this shape.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-172: 12.3-RV14: `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not in CONTRIBUTING.md. Either remove the aliases or document them in a follow-up.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:19-23
reason: - **12.3-RV14 — `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not in CONTRIBUTING.md.** Either remove the aliases or document them in a follow-up. [tools/check-story-file-scope.py:19-23]
status: done 2026-11-24
resolution: CONTRIBUTING.md documents both supported historical labels. The scope fixture verifies that each label still authorizes backticked paths and warns about bare entries.

### DW-173: 12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.3 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-174: W10: closed. AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: course correction (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-175: S11-FA: closed. `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 12.6 EmbeddingInputContentKind Baseline Resolution (2026-05-02)"), 2026-09-01
archived: 2026-11-23

### DW-176: Story-10.2-TokenBudgetServerTruncation: closed. MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-177: Story-10.2-DegradedStateAnnotations: closed for MCP ingress. Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-178: Story-10.2-IngressAuthentication-NFR11: closed for MCP ingress. `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-179: Story-10.x-McpTraceHopAspire (closes AC #12).

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs::TraceHop_McpToServer_PreservesTraceparent
reason: - **Story-10.x-McpTraceHopAspire (closes AC #12).** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs::TraceHop_McpToServer_PreservesTraceparent` — assert MCP→Memories Server parent/child trace span relationship at Tier-3. Implementation requires a parallel breadcrumb path through the MCP Aspire process (the existing `AspireEndToEndTraceTests` uses an in-process collector for the CLI side and audit-log JSON breadcrumbs for the Server side; the MCP service emits ActivitySource spans into its own process and lacks an in-test exporter). 10.2 ships the prerequisite Tier-3 auth bearer-minting fixture (`AspireIngestionPipelineFixture.MintDevBearer`) so the trace-hop work can attach to it directly. Re-open trigger: nightly Tier-3 lane stabilizes against the new auth-required surface and an in-test exporter for the MCP process becomes affordable; or first observation of MCP→Server trace breakage in production OTel data.
status: open
note: 2026-11-24 — Skipped. End-to-end parent-span evidence needs an exporter and correlation harness across Aspire processes.

### DW-180: Story-10.x-McpAuthTier3ExtendedSuite. Additional Tier-3 cases beyond the four critical scenarios shipped in 10.2 (`PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge`, `GetHealth_AllowsAnonymous`, `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`, `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden`): expired-bearer test, clock-skew tolerance test (M3), cross-request alternating-tenants leak test (P2), tool-class lifetime convention test (P1). Tier-2 already covers expiry / clock-skew at unit level (`ConfigureJwtBearerOptionsTests`); cross-request leak is structurally prevented by `TenantClaimAuthorizationFilter`'s `InvalidOperationException` guard (P3). Re-open trigger: first nightly Tier-3 regression against the auth path, or first cross-request leakage report.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAuthTier3ExtendedSuite.** Additional Tier-3 cases beyond the four critical scenarios shipped in 10.2 (`PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge`, `GetHealth_AllowsAnonymous`, `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`, `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden`): expired-bearer test, clock-skew tolerance test (M3), cross-request alternating-tenants leak test (P2), tool-class lifetime convention test (P1). Tier-2 already covers expiry / clock-skew at unit level (`ConfigureJwtBearerOptionsTests`); cross-request leak is structurally prevented by `TenantClaimAuthorizationFilter`'s `InvalidOperationException` guard (P3). Re-open trigger: first nightly Tier-3 regression against the auth path, or first cross-request leakage report.
status: done 2026-11-24
resolution: Added live JWT expiry/skew, alternating-tenant isolation and crossed-tenant denial tests plus a composition lifetime guard. All 7 live auth tests and 6 composition tests passed.

### DW-181: Story-10.x-McpStatelessTripwireTest (W3). `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — assert `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Requires an MCP-builder introspection surface that the SDK does not currently expose. Re-open trigger: SDK exposes a public reader for `WithHttpTransport` configuration, or any follow-up story discusses OAuth-PKCE / sampling / elicitation.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpStatelessTripwireTest (W3).** `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — assert `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Requires an MCP-builder introspection surface that the SDK does not currently expose. Re-open trigger: SDK exposes a public reader for `WithHttpTransport` configuration, or any follow-up story discusses OAuth-PKCE / sampling / elicitation.
status: done 2026-11-24
resolution: The composition test seeds a stateful value and asserts the registered MCP HTTP options override it to stateless, guarding against SDK default masking. All 7 composition tests passed.

### DW-182: Story-10.x-McpAuthAnonymousDevBindAddress (A2). Bind-address invariant for the anonymous-dev gate — refuse to run when `Authentication:JwtBearer:Authority` AND `SigningKey` are both unset and the process binds a non-loopback address. The 10.2 wiring requires `Authority` OR `SigningKey` at startup (validator hard-fails otherwise), so the anonymous-dev path no longer exists; this defense-in-depth check is N/A unless the anonymous-dev path is reintroduced. Re-open trigger: any future story re-introducing an anonymous mode for development.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAuthAnonymousDevBindAddress (A2).** Bind-address invariant for the anonymous-dev gate — refuse to run when `Authentication:JwtBearer:Authority` AND `SigningKey` are both unset and the process binds a non-loopback address. The 10.2 wiring requires `Authority` OR `SigningKey` at startup (validator hard-fails otherwise), so the anonymous-dev path no longer exists; this defense-in-depth check is N/A unless the anonymous-dev path is reintroduced. Re-open trigger: any future story re-introducing an anonymous mode for development.
status: open
note: 2026-11-24 — Skipped. Startup authentication already requires issuer credentials and the anonymous development mode was removed.

### DW-183: Story-10.x-McpAuthDriftDetectorCi (A14). Monthly GitHub Actions workflow that diffs `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` against `src/Hexalith.Memories.Mcp/Authentication/` and posts structural drift summaries to an ops issue. Allowed-divergence list mirrors ADR-10.2-001 invariants. Effort > 45 min for a robust diff + allowlist + ops-issue integration; deferred to keep the 10.2 envelope on track. Re-open trigger: first observed drift incident, or quarterly review.

origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
location: src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/
reason: - **Story-10.x-McpAuthDriftDetectorCi (A14).** Monthly GitHub Actions workflow that diffs `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` against `src/Hexalith.Memories.Mcp/Authentication/` and posts structural drift summaries to an ops issue. Allowed-divergence list mirrors ADR-10.2-001 invariants. Effort > 45 min for a robust diff + allowlist + ops-issue integration; deferred to keep the 10.2 envelope on track. Re-open trigger: first observed drift incident, or quarterly review.
status: open
note: 2026-11-24 — Skipped. A scheduled cross-repository drift report requires CI/CD ownership, an allowlist, and an issue-publishing policy.

### DW-184: Story-10.x-RetroLessonForwardReferenceGuards (A16). Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-185: Story-10.x-McpTraceHopAssertion. `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` does not yet include an MCP-hop assertion — adding one is out of scope for 10.1 and tempting-but-fragile until 10.2 auth is wired (so the test does not need to mint bearer tokens). The MCP integration test (`McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`) covers a happy-path execution, but does NOT yet assert that the outbound trace contains a span resolving to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). Re-open trigger: post-10.2 follow-up story or first observation that direct-HTTP regressions slip through.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs
reason: - **Story-10.x-McpTraceHopAssertion.** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` does not yet include an MCP-hop assertion — adding one is out of scope for 10.1 and tempting-but-fragile until 10.2 auth is wired (so the test does not need to mint bearer tokens). The MCP integration test (`McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`) covers a happy-path execution, but does NOT yet assert that the outbound trace contains a span resolving to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). **Re-open trigger:** post-10.2 follow-up story or first observation that direct-HTTP regressions slip through.
status: open
note: 2026-11-24 — Skipped. The existing fixture cannot attribute and export the outbound sidecar span to the same request without a cross-process trace harness.

### DW-186: Story-10.x-StatelessModeAuditFor10.2Auth: closed for bearer auth. Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-187: Story-10.x-McpAotCompatibility. ModelContextProtocol 1.2.0 uses reflection for tool-schema generation. Setting `<PublishAot>true</PublishAot>` on `Hexalith.Memories.Mcp.csproj` will likely surface trim/reflection warnings. 10.1 deliberately leaves AOT off — same default as `Contracts`, `Client.Rest`, `EventStore`, `Cli`. Re-open trigger: an explicit AOT-publishing requirement, or upstream MCP SDK release that ships a source-generator-based schema path.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpAotCompatibility.** ModelContextProtocol 1.2.0 uses reflection for tool-schema generation. Setting `<PublishAot>true</PublishAot>` on `Hexalith.Memories.Mcp.csproj` will likely surface trim/reflection warnings. 10.1 deliberately leaves AOT off — same default as `Contracts`, `Client.Rest`, `EventStore`, `Cli`. **Re-open trigger:** an explicit AOT-publishing requirement, or upstream MCP SDK release that ships a source-generator-based schema path.
status: open
note: 2026-11-24 — Skipped. There is no agreed AOT publication requirement for the retained compatibility MCP server.

### DW-188: Story-10.x-McpTokenizerAccurateBudget. Story 10.2 replaced the 10.1 per-result soft clamp with server-side `contentSnippet.Length / 4 + overhead` estimation. This is still heuristic and can over-prune non-ASCII content. Phase 2 Sprint 1 candidate; escalate on first non-ASCII tenant onboarding or quantitative observation that the estimate causes >2x under-utilization on real workloads.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-McpTokenizerAccurateBudget.** Story 10.2 replaced the 10.1 per-result soft clamp with server-side `contentSnippet.Length / 4 + overhead` estimation. This is still heuristic and can over-prune non-ASCII content. Phase 2 Sprint 1 candidate; escalate on first non-ASCII tenant onboarding or quantitative observation that the estimate causes >2x under-utilization on real workloads.
status: open
note: 2026-11-24 — Skipped. Accurate budgets need a tokenizer contract shared with the server and clients; the retained MCP surface is frozen for feature changes.

### DW-189: Story-10.x-TraverseSemanticPrimaryPath. Story 10.2 traversal truncation preserves a BFS shortest path to the deepest node before pruning branches. A semantically weighted primary path (`causedBy` > `correlatedWith`, etc.) may produce better narratives under tight budgets, but edge weights are not exposed today. Re-open when traversal edge weights become available.

origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Story-10.x-TraverseSemanticPrimaryPath.** Story 10.2 traversal truncation preserves a BFS shortest path to the deepest node before pruning branches. A semantically weighted primary path (`causedBy` > `correlatedWith`, etc.) may produce better narratives under tight budgets, but edge weights are not exposed today. Re-open when traversal edge weights become available.
status: open
note: 2026-11-24 — Skipped. Traversal edges do not expose weights, and the narrative ranking policy is undefined.

### DW-190: Story-10.x-OpenTelemetryAspNetCoreAlignment. Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. Re-open trigger: AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-191: Story-9.3-MemoriesServerAuthN: resolved by Story 20.1. Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-192: D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-193: Story-9.3-ObservationWindowConfig. The 24h observation window is hardcoded in `HandlerRegistryService.ObservationWindow`. Making it configurable per-tenant complicates Redis TTL (TTL must exceed the widest possible window) and global-config awkwardness. Re-open trigger: first operator explicitly requests a non-24h window in a real deployment.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ObservationWindowConfig.** The 24h observation window is hardcoded in `HandlerRegistryService.ObservationWindow`. Making it configurable per-tenant complicates Redis TTL (TTL must exceed the widest possible window) and global-config awkwardness. **Re-open trigger:** first operator explicitly requests a non-24h window in a real deployment.
status: open
note: 2026-11-24 — Skipped. No operator request defines the replacement window or how the observation retention should grow.

### DW-194: Story-9.3-ProjectionRegistryCrossCheck. The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. Re-open trigger: operator-driven demand for this gap to be closed.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-195: Story-9.3-SinceFlagForLowVolume. `--since <duration>` CLI flag on `memories handlers mismatches` to widen the observation window for low-volume tenants, reducing `StaleHandler` Info noise on weekly-publishing patterns. Requires the observation store TTL to be widened to `2 × max(window)` globally, or a dedicated expanded-window store. Material cost, deferred.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-SinceFlagForLowVolume.** `--since <duration>` CLI flag on `memories handlers mismatches` to widen the observation window for low-volume tenants, reducing `StaleHandler` Info noise on weekly-publishing patterns. Requires the observation store TTL to be widened to `2 × max(window)` globally, or a dedicated expanded-window store. Material cost, deferred.
status: open
note: 2026-11-24 — Skipped. The compatibility CLI is frozen for new flags, and a larger observation window requires a retention decision.

### DW-196: Story-9.3-TenantCardinalityBucketing. Switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0 / 1-10 / 10-100 / 100+ tenants) when N ≥ 1000 tenants approaches. Not an issue in current deployments. Re-open trigger: tenant count crosses 1000 in any real environment.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-TenantCardinalityBucketing.** Switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0 / 1-10 / 10-100 / 100+ tenants) when N ≥ 1000 tenants approaches. Not an issue in current deployments. **Re-open trigger:** tenant count crosses 1000 in any real environment.
status: open
note: 2026-11-24 — Skipped. No deployment has been identified as reaching the tenant-count trigger for changing the metrics contract.

### DW-197: Story-9.3-VersionMismatchAttributeApproach. Replace regex-based `VersionMismatch` with a publisher-declared `[EventType("ClaimSubmitted", Version=2)]` attribute, surfaced via `ReflectionTypeLoader` at startup. Becomes an O(1) dictionary lookup with no ReDoS surface, no length cap, no regex-timeout event id 9141. Deferred: requires coordinating a convention change with every publisher repo.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-VersionMismatchAttributeApproach.** Replace regex-based `VersionMismatch` with a publisher-declared `[EventType("ClaimSubmitted", Version=2)]` attribute, surfaced via `ReflectionTypeLoader` at startup. Becomes an O(1) dictionary lookup with no ReDoS surface, no length cap, no regex-timeout event id 9141. Deferred: requires coordinating a convention change with every publisher repo.
status: open
decision: 2026-09-01 Await publisher program
decision: 2026-09-01 Await publisher program
note: 2026-11-24 — Skipped. Publisher-declared version attributes require a coordinated convention and rollout across publisher repositories.

### DW-198: Story-9.3-SubscriptionStatusConfigured. 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. Re-open trigger: operator feedback post-landing indicates the 3-state model is ambiguous.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-199: Story-9.3-ObservationStoreRebuildFromAuditLog. Rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss (Risk #8 degraded mode). Out of scope for 9.3.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-ObservationStoreRebuildFromAuditLog.** Rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss (Risk #8 degraded mode). Out of scope for 9.3.
status: open
note: 2026-11-24 — Skipped. Boot-time replay needs a supported telemetry read model, ownership, and restart behavior decisions.

### DW-200: Story-9.3-PostgresObservationStoreAlternative. Investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-PostgresObservationStoreAlternative.** Investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.
status: open
decision: 2026-09-01 Benchmark Postgres view — Build a representative view prototype, measure query latency and write amplification, and recommend a substrate.
decision: 2026-09-01 Benchmark Postgres view — Build a representative view prototype, measure query latency and write amplification, and recommend a substrate.
note: 2026-11-24 — Skipped. No approved PostgreSQL observation read model or p95 benchmark is available.

### DW-201: Story-9.3-CrossTenantVersionConsumerLookup. A dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside 9.3; Epic 5 tenant-isolation invariant prevents a naive implementation.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-CrossTenantVersionConsumerLookup.** A dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside 9.3; Epic 5 tenant-isolation invariant prevents a naive implementation.
status: open
decision: 2026-09-01 Await operator auth model
decision: 2026-09-01 Await operator auth model
note: 2026-11-24 — Skipped. A cross-tenant endpoint requires explicit operator authorization and a new cross-tenant data contract.

### DW-202: Story-9.3-PostLaunchCategoryReview. Measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator acknowledgement or >95% false-positive rate. Target review: 2026-09 or later. The three-category decision is explicitly revisitable based on measured telemetry, not speculation.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-PostLaunchCategoryReview.** Measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator acknowledgement or >95% false-positive rate. Target review: 2026-09 or later. The three-category decision is explicitly revisitable based on measured telemetry, not speculation.
status: open
note: 2026-11-24 — Skipped. Three months of post-launch category and operator-acknowledgement telemetry are unavailable.

### DW-203: Story-9.3-Tier2IntegrationTests. 9.3's Task 10 (Tier-2 Aspire-AppHost integration tests 10.0–10.12) was deferred during the initial landing pass. Unit coverage (45 new tests) pins the per-component invariants; the Tier-2 tests would add cross-component proof against a real Redis + DAPR sidecar. Specific deferred tests: `HandlersFixtureSmokeTests`, `HandlersListIntegrationTests`, `HandlersMismatchIntegrationTests` (VersionMismatch + healthy + StaleHandler), `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (property-based, dropProbability ∈ {0.0, 0.1, 0.3}), `HandlerEndpointLatencyNfrTests` (N=100 tenants, p95 500ms/200ms), `HandlerObservationKillSwitchIntegrationTests` (AC #21), `EventIngestionTelemetryAdapterSlowRedisTests` (AC #22 — bounded-FAF contract), `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` (AC #26 — Finding N), `HandlerMetricsCardinalitySmokeTests`, `EndpointRoutingTests` + `MemoriesClientPathConstantTests` (AC #29 — Findings U, V), `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis` + operator-polish tests (AC #30 — Findings B, X). Re-open trigger: first post-landing Tier-2 regression run or any cross-component bug report on 9.3 surface. Each test ~30s startup via Aspire fixture; budget ~1d to close the set.

origin: migrated from legacy ledger ("Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Story-9.3-Tier2IntegrationTests.** 9.3's Task 10 (Tier-2 Aspire-AppHost integration tests 10.0–10.12) was deferred during the initial landing pass. Unit coverage (45 new tests) pins the per-component invariants; the Tier-2 tests would add cross-component proof against a real Redis + DAPR sidecar. Specific deferred tests: `HandlersFixtureSmokeTests`, `HandlersListIntegrationTests`, `HandlersMismatchIntegrationTests` (VersionMismatch + healthy + StaleHandler), `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (property-based, dropProbability ∈ {0.0, 0.1, 0.3}), `HandlerEndpointLatencyNfrTests` (N=100 tenants, p95 500ms/200ms), `HandlerObservationKillSwitchIntegrationTests` (AC #21), `EventIngestionTelemetryAdapterSlowRedisTests` (AC #22 — bounded-FAF contract), `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` (AC #26 — Finding N), `HandlerMetricsCardinalitySmokeTests`, `EndpointRoutingTests` + `MemoriesClientPathConstantTests` (AC #29 — Findings U, V), `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis` + operator-polish tests (AC #30 — Findings B, X). Re-open trigger: first post-landing Tier-2 regression run or any cross-component bug report on 9.3 surface. Each test ~30s startup via Aspire fixture; budget ~1d to close the set.
status: open
note: 2026-11-24 — Skipped. The entry calls for thirteen cross-component Aspire scenarios and needs a dedicated verification slice.

### DW-204: Source-prefix observation granularity / true per-handler fidelity.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-handler-registration-and-mismatch-detection (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Source-prefix observation granularity / true per-handler fidelity.** The current observation store records tenant + aggregateType + eventType only; it does not persist the CloudEvent `source` or matched `SourceToTenantMap` prefix. As a result, `HandlerRegistryService` duplicates one tenant-wide observation set across every row, `StaleHandler` can only mean "tenant saw nothing" rather than "this configured sourcePrefix saw nothing", and `UnhandledEventType` remains heuristic versus the real longest-prefix router on `source`. Re-open trigger: first operator/reporting need for true per-`sourcePrefix` fidelity, or any follow-up story that revisits the observation-store write model.
status: open
note: 2026-11-24 — Skipped. A handler-keyed observation contract requires coordinated producer and subscriber identity semantics.

### DW-205: Rolling 24h counts in `RedisObservedEventTypeStore`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-handler-registration-and-mismatch-detection (2026-04-25)"), 2026-09-01
location: n/a
reason: - **Rolling 24h counts in `RedisObservedEventTypeStore`.** `GetObservedTypesAsync` currently reads last-seen membership from the zset and counts from a lifetime `HINCRBY` hash, so `eventsProcessedCount` and per-type counts are not truly window-bounded. Accurate rolling counts require a different write model (per-occurrence events, rolling buckets, or another substrate) rather than a small read-side patch. Re-open trigger: when the observation-store model is next revised — ideally in the same change as the source-prefix-fidelity fix so the write/read contracts only churn once.
status: open
note: 2026-11-24 — Skipped. An exact sliding window needs a new aggregation model for the current Dapr-backed observation store.

### DW-206: F1: Retry backpressure + 9174 + exponential backoff (Task 8.5 / D2). `NaturalLanguageEmbeddingRetryHostedService.TickAsync` has no rate-limiter utilization check, no skip counter, no `9174` LoggerMessage, and no interval multiplier when `backlog > 1000`. Re-open trigger: rate-limiter consistently >80% utilization with retry queue contributing >20% of total calls.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F1 — Retry backpressure + 9174 + exponential backoff** (Task 8.5 / D2). `NaturalLanguageEmbeddingRetryHostedService.TickAsync` has no rate-limiter utilization check, no skip counter, no `9174` LoggerMessage, and no interval multiplier when `backlog > 1000`. Re-open trigger: rate-limiter consistently >80% utilization with retry queue contributing >20% of total calls.
status: open
note: 2026-11-24 — Skipped. The retry service lacks a utilization signal and needs an agreed backpressure and retry schedule.

### DW-207: F2: Tier-2 / Tier-3 integration test suite for AC #14/#15/#16 (Task 9.1–9.6). `DualEmbeddingRoundTripTests`, `OutOfOrderEventTests`, `DegradedNaturalLanguageEmbeddingTests`, `CorrelationRootEdgeTests`, `IngestionWorkflowReplaySafetyTests`, consistency verification NL cases. Deferred per Task 9 header; re-open when Tier-2 environment is stable.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F2 — Tier-2 / Tier-3 integration test suite for AC #14/#15/#16** (Task 9.1–9.6). `DualEmbeddingRoundTripTests`, `OutOfOrderEventTests`, `DegradedNaturalLanguageEmbeddingTests`, `CorrelationRootEdgeTests`, `IngestionWorkflowReplaySafetyTests`, consistency verification NL cases. Deferred per Task 9 header; re-open when Tier-2 environment is stable.
status: open
note: 2026-11-24 — Skipped. The requested cross-tier ingestion and recovery test matrix is larger than a focused backlog item.

### DW-208: F3: `RateLimiterSizingValidator` + event 9163 (Task 8.7). Needed when first `SourceType.Event` ingest hits an under-sized tenant configuration. Re-open trigger: first production `9162` warning or tenant NL-pipeline SLO breach.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F3 — `RateLimiterSizingValidator` + event 9163** (Task 8.7). Needed when first `SourceType.Event` ingest hits an under-sized tenant configuration. Re-open trigger: first production `9162` warning or tenant NL-pipeline SLO breach.
status: open
note: 2026-11-24 — Skipped. No recorded production sizing warning or SLO breach establishes a sizing policy.

### DW-209: F4: `retry-nl-embeddings` CLI dead-letter surface (Task 8.8). Re-open when dead-letter volume > 0 in any tenant for >24h.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F4 — `retry-nl-embeddings` CLI dead-letter surface** (Task 8.8). Re-open when dead-letter volume > 0 in any tenant for >24h.
status: open
note: 2026-11-24 — Skipped. A new dead-letter command is outside the retained compatibility CLI scope and needs a designated tool owner.

### DW-210: F5: Logprobs extraction for confidence promotion (Task 2.5 / D1). Blocked on Dapr.AI 1.17.6 SDK surface. Re-open when `ConversationClient` exposes `logprobs` or the equivalent shape on `ConversationResponse`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F5 — Logprobs extraction for confidence promotion** (Task 2.5 / D1). Blocked on Dapr.AI 1.17.6 SDK surface. Re-open when `ConversationClient` exposes `logprobs` or the equivalent shape on `ConversationResponse`.
status: open
note: 2026-11-24 — Skipped. The pinned Dapr.AI SDK does not expose token log probabilities on conversation choices.

### DW-211: F6: Per-tenant LLM configuration. Phase 2. MVP is single system-wide `conversation.openai` component; operators swap via YAML.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F6 — Per-tenant LLM configuration.** Phase 2. MVP is single system-wide `conversation.openai` component; operators swap via YAML.
status: open
note: 2026-11-24 — Skipped. Per-tenant LLM selection requires a configuration, credential, and routing design.

### DW-212: F7: `NaturalLanguageEmbeddingRetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter. When `ScheduleNewWorkflowAsync` + `WaitForWorkflowCompletionAsync` loop times out repeatedly for the same record, move to dead-letter after N ticks rather than leaving stuck instances in queue.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F7 — `NaturalLanguageEmbeddingRetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter.** When `ScheduleNewWorkflowAsync` + `WaitForWorkflowCompletionAsync` loop times out repeatedly for the same record, move to dead-letter after N ticks rather than leaving stuck instances in queue.
status: open
note: 2026-11-24 — Skipped. Dead-lettering repeated timeouts needs durable attempt tracking and a policy for workflows still running.

### DW-213: F8: Redis cluster multi-node enumeration in `FailedNaturalLanguageEmbeddingRegistry.ListTenantsWithBacklogAsync`. Current `GetFirstConnectedServer()` covers single-node / replicated deployments only. Cluster deployment re-open trigger: moving to Redis Cluster in production infrastructure.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F8 — Redis cluster multi-node enumeration in `FailedNaturalLanguageEmbeddingRegistry.ListTenantsWithBacklogAsync`.** Current `GetFirstConnectedServer()` covers single-node / replicated deployments only. Cluster deployment re-open trigger: moving to Redis Cluster in production infrastructure.
status: open
note: 2026-11-24 — Skipped. Supporting Redis Cluster needs topology-wide scanning and compatibility with the existing multi-key operations.

### DW-214: F9: `OrphanSemanticIndexReconciler` interval-based re-run. Currently one-shot startup sweep only (per D3 decision pending). Re-open if post-startup SIGKILL-during-provisioning produces orphan NL indexes in production.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F9 — `OrphanSemanticIndexReconciler` interval-based re-run.** Currently one-shot startup sweep only (per D3 decision pending). Re-open if post-startup SIGKILL-during-provisioning produces orphan NL indexes in production.
status: open
note: 2026-11-24 — Skipped. Periodic orphan cleanup needs a guard against racing live provisioning; the one-shot startup policy remains in effect.

### DW-215: F10: `IsStubBackfillMigration` atomic gate-write + backfill safety. Partial-commit risk after backfill if `MERGE SchemaMigration` throws. Defer to ops runbook monitoring; re-open if migration re-runs cause operator friction.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)"), 2026-09-01
location: n/a
reason: - **F10 — `IsStubBackfillMigration` atomic gate-write + backfill safety.** Partial-commit risk after backfill if `MERGE SchemaMigration` throws. Defer to ops runbook monitoring; re-open if migration re-runs cause operator friction.
status: open
note: 2026-11-24 — Skipped. The recorded operator trigger is absent, and changing the graph migration requires an atomicity and retry proof on FalkorDB.

### DW-216: DataContract/DataMember attributes missing on V1 contracts

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
archived: 2026-11-23

### DW-217: No transient/permanent exception classification for Kreuzberg errors

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **No transient/permanent exception classification for Kreuzberg errors** — AC4 is met (exceptions propagate for DAPR Workflow retry). However, permanent failures (corrupt files) will be retried indefinitely. Future work: classify KreuzbergValidationException as non-retriable, KreuzbergOcrException as retriable.
status: open
note: 2026-11-24 — Skipped. Changing extraction retry behavior requires classifying provider failures while preserving deterministic workflow replay.

### DW-218: Large byte[] in ExtractionInput persisted to workflow state store

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
archived: 2026-11-23

### DW-219: byte[] mutable on immutable record

origin: migrated from legacy ledger ("Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)"), 2026-09-01
location: n/a
reason: - **byte[] mutable on immutable record** — ExtractionInput uses byte[] which is mutable, breaking record immutability semantics and reference-based equality. No practical alternative exists in .NET for JSON-serializable binary data (ImmutableArray/ReadOnlyMemory don't serialize well).
status: open
note: 2026-11-24 — Skipped. Changing the binary workflow record needs a wire-compatibility and allocation review for retained workflow payloads.

### DW-220: End-to-end embedding flow is not wired into orchestration

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
archived: 2026-11-23

### DW-221: Rate-limiting scope conflicts with credential scope

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
archived: 2026-11-23

### DW-222: Story transition rationale is comment-only and not machine-readable

origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
location: n/a
reason: - **Story transition rationale is comment-only and not machine-readable** — The sprint tracking update relies on a free-form YAML comment for rationale, so tooling cannot query or validate why a story moved between workflow states.
status: open
decision: 2026-09-06 Add evidence field — Add and validate a machine-readable transition-rationale field.
decision: 2026-09-06 Add evidence field — Add and validate a machine-readable transition-rationale field.
note: 2026-11-24 — Skipped. A machine-readable transition field requires a shared writer, schema migration, and consumer updates.

### DW-223: Story status requires manual dual-write across tracking files

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-embedding-generation (2026-03-29)"), 2026-09-01
archived: 2026-11-23

### DW-224: ValidateResult.IsValid/ErrorMessage is dead code on failure path

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **ValidateResult.IsValid/ErrorMessage is dead code on failure path** — ValidateContentActivity throws exceptions for invalid input; the ValidateResult record is only used for the success path. Spec mandates this contract shape, so keeping as-is.
status: open
note: 2026-11-24 — Skipped. The result shape is prescribed by the existing workflow contract and may appear in historical payloads.

### DW-225: SaveDedupKeyActivity: no TTL on dedup keys

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
location: n/a
reason: - **SaveDedupKeyActivity: no TTL on dedup keys** — Dedup keys persist forever in DAPR state store, preventing re-ingestion of deleted content. Cleanup mechanism belongs to Epic 8 (Story 8.2 consistency verification).
status: open
note: 2026-11-24 — Skipped. Expiring permanent deduplication keys changes replay and duplicate-ingestion behavior and needs a retention policy.

### DW-226: ContentBytes serialized inline in DAPR workflow state

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)"), 2026-09-01
archived: 2026-11-23

### DW-227: Duplicate dedup entries are returned without confirming the referenced memory unit still exists

origin: migrated from legacy ledger ("Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **Duplicate dedup entries are returned without confirming the referenced memory unit still exists** — The workflow fast-returns on a dedup hit without verifying that the stored `MemoryUnitId` is still present in the indexed backends, so manual cleanup or backend drift can leave callers with a duplicate response that points at missing data.
status: open
note: 2026-11-24 — Skipped. Repairing stale dedup pointers requires consistent Redis, graph, and ingestion lifecycle state under concurrent work.

### DW-228: indexedAt set to ingestedAt in GraphQueryBuilder

origin: migrated from legacy ledger ("Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
location: n/a
reason: - **indexedAt set to ingestedAt in GraphQueryBuilder** — `BuildMergeMemoryUnitNode` sets the FalkorDB `indexedAt` property to the workflow's `ingestedAt` timestamp. These are semantically different (when ingestion started vs when the graph write happened). Fixing requires adding a separate `indexedAt` parameter to `IndexInput`, which is a cross-story contract change (Story 1.5).
status: open
decision: 2026-09-06 Add distinct timestamp — Persist and expose indexedAt with migration-safe tests.
decision: 2026-09-06 Add distinct timestamp — Persist and expose indexedAt with migration-safe tests.
note: 2026-11-24 — Skipped. Changing indexedAt requires a graph schema decision and an explicit fallback for legacy graph records.

### DW-229: CaseId not validated for special characters

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)"), 2026-09-01
archived: 2026-11-23

### DW-230: Return `offset` and `maxResults` pagination metadata in search response envelopes

origin: migrated from legacy ledger ("Deferred from: code review of 2-6-explain-mode-and-confidence-scores (2026-04-02)"), 2026-09-01
location: n/a
reason: - **Return `offset` and `maxResults` pagination metadata in search response envelopes** — AC 3 still calls for `offset`, `maxResults`, and `totalCount` in paginated responses, but the response contracts still expose only `TotalCount`. This appears to predate the explain-mode change and would require a broader response-contract update across `SearchResult` and `HybridSearchResult`.
status: done 2026-11-24
resolution: Both HTTP search envelopes report the effective page window, including clamped and empty pages. REST callers can set the hybrid offset. Endpoint and client tests verify the window and legacy JSON compatibility. See spec-ten-deferred-search-response-reliability.md.
decision: 2026-09-01 Add pagination metadata — Add fields to paginated envelopes, populate them consistently, update consumers, and test compatibility.
decision: 2026-09-01 Add pagination metadata — Add fields to paginated envelopes, populate them consistently, update consumers, and test compatibility.

### DW-231: InternalsVisibleTo in packable library without strong-name key

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-232: FusionEngine non-finite handling asymmetry across axes

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-233: Case creation is non-atomic across Redis and FalkorDB

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 3-1-create-and-list-cases (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-234: Case creation is non-atomic across Redis and FalkorDB

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 3-2-case-status-and-activity.md (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-235: Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: 3-3-case-member-management (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-236: Dedup key orphaning after MU deletion

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Dedup key orphaning after MU deletion** — Deleting a memory unit removes it from RediSearch, Redis Vector, and FalkorDB, but the DAPR state store dedup key persists. Re-ingesting identical content is silently blocked by dedup detection, returning a stale MU ID. Fix: add dedup key TTL or explicit dedup key deletion during MU deletion. Belongs in Epic 8 (Story 8.2 consistency verification).
status: open
note: 2026-11-24 — Skipped. Dedup deletion must coordinate with the existing reservation and publication authorities to keep retries consistent.

### DW-237: Ingestion workflow must check `CaseStatus.Deleting`

origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
location: n/a
reason: - **Ingestion workflow must check `CaseStatus.Deleting`** — Story 3.5 sets case status to `Deleting` during case deletion, but the ingestion workflow (`ValidateContentActivity`) does not yet check this status before creating CONTAINS edges. A concurrent ingestion during case deletion could create orphaned MUs. Wire the status check into ingestion validation.
status: open
note: 2026-11-24 — Skipped. A single status check cannot close the ingestion/deletion race; the lifecycle needs a shared admission boundary.

### DW-238: Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-239: metadataQuery no length/content validation

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **metadataQuery no length/content validation** — The `metadataQuery` query parameter has no length limit or format validation at the endpoint level, unlike `sourceType` (enum-validated) and `caseId` (existence-checked). General input validation concern across all query parameters. [Program.cs:436]
status: open
note: 2026-11-24 — Skipped. The metadata filter is free text and has no agreed format or compatible length limit.

### DW-240: cancellationToken not propagated in ResolveNamesAsync

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **cancellationToken not propagated in ResolveNamesAsync** — `CaseService.ResolveNamesAsync` accepts a CancellationToken but never passes it to Redis batch operations or Task.WhenAll. StackExchange.Redis batch ops have limited cancellation support; pre-existing pattern in other batch methods. [CaseService.cs:321]
status: done 2026-11-24
resolution: Case-name resolution checks cancellation before dispatch and stops waiting when the caller cancels an in-flight Redis batch. All 43 CaseService tests passed.

### DW-241: No input validation on caseId format before Redis key construction

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
archived: 2026-11-23

### DW-242: No error handling for Redis failure in case name enrichment

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)"), 2026-09-01
location: n/a
reason: - **No error handling for Redis failure in case name enrichment** — If Redis fails during the optional `ResolveNamesAsync` call, the entire search request returns 500 even though core search results are already available. Should degrade gracefully by returning results without case names. [Program.cs:988]
status: done 2026-11-24
resolution: Search preserves hits and case-ID groups during Redis case-name timeout or disconnect. Per-hit names are omitted; command errors still propagate. All 76 focused service and endpoint tests passed.

### DW-243: Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)"), 2026-09-01
archived: 2026-11-23

### DW-244: Breaking-change conflict response still returns the wrong error contract

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Program.cs:1888
reason: - **Breaking-change conflict response still returns the wrong error contract** — `CreateEmbeddingConfigConflictResponse` still emits `error = "EmbeddingConfigChangeRequired"` instead of the pinned `EMBEDDING_CONFIG_BREAKING_CHANGE` response contract. This predates Story 5.5 and was not introduced by the current diff, so it remains deferred here. [src/Hexalith.Memories.Server/Program.cs:1888]
status: done 2026-11-24
resolution: The embedding configuration PUT conflict returns EMBEDDING_CONFIG_BREAKING_CHANGE with both configurations and affected fields. The serialization tests exercise the production formatter and the integration assertion checks the HTTP contract.

### DW-245: Per-run Docker named volumes are never torn down

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
archived: 2026-11-23

### DW-246: `_logProvider` in the fixture accumulates entries across restart lifetimes

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **`_logProvider` in the fixture accumulates entries across restart lifetimes** — `RestartTopologyAsync` does not reset the shared log provider, so any future test code that captures a pre-restart index and inspects post-restart entries will see mixed lifetimes. Latent trap rather than a current bug. [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs]
status: done 2026-11-24
resolution: Each full topology start registers a fresh capture provider and carries forward the cursor. Fixture tests cover late writes to the previous provider and the live restart test verifies that public snapshots contain only current logs.

### DW-247: `[DataMember]` attributes omit explicit `Name`

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs
reason: - **`[DataMember]` attributes omit explicit `Name`** — Property renames on `CorpusStatistics`, `RateLimitState`, and `CaseIngestionCounts` will silently break wire format for existing persisted actor state. Set `[DataMember(Name = "...")]` explicitly before the next rename. [src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs]
status: done 2026-11-24
resolution: The affected DataContract and actor JSON members explicitly use their established wire names. Tests check DataMember metadata and round-trip legacy JSON for persisted state and the counts response.

### DW-248: `BuildDedupKey` duplicates server-side hash logic in the test

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774
reason: - **`BuildDedupKey` duplicates server-side hash logic in the test** — `PipelinePersistenceIntegrationTests.BuildDedupKey` recomputes `SHA256(sourceUri)` exactly the way the server does today. Any future change to URI normalization on the server will be invisible to the test. Replace with a server-side dedup-inspection query or an exposed helper. [tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774]
status: done 2026-11-24
resolution: The restart durability test uses DedupKeyBuilder.BuildKey and continues to verify the persisted dedup entry, syntactic and semantic keys, and indexed unit.

### DW-249: AppHost token propagation uses process-env side effects

origin: migrated from legacy ledger ("Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.AppHost/Program.cs:183-198
reason: - **AppHost token propagation uses process-env side effects** — `ApplyProcessEnvironmentTokens` seeds `APP_API_TOKEN` / `DAPR_API_TOKEN` into the AppHost process environment because CommunityToolkit.Aspire.Hosting.Dapr 9.7 does not expose a sidecar-scoped env API. Tokens leak to every child container/subprocess and are never unset. Revisit when the toolkit exposes a sidecar-env builder. [src/Hexalith.Memories.AppHost/Program.cs:183-198]
status: open
note: 2026-11-24 — Skipped. The pinned toolkit can scope sidecar annotations, but Aspire child executables still inherit the documented ambient token inputs. Removing the redundant process assignments alone leaves that exposure. Closure needs an AppHost-scoped token source compatible with existing operators or verified filtering of inherited values.

### DW-250: Dictionary iteration order in `AxisDetails` / `Metadata` is not sorted

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, SearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs, MemoryUnitTableFormatter.cs
reason: - **Dictionary iteration order in `AxisDetails` / `Metadata` is not sorted** — `SearchExplanation.AxisDetails` and `MemoryUnit.Metadata` formatters enumerate the underlying `Dictionary<,>` in server-insertion order. Test-fragility for golden snapshots if the server ever reorders its JSON payload; no AC broken today. Sort keys at emit time when that becomes load-bearing. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, SearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs, MemoryUnitTableFormatter.cs]
status: done 2026-11-24
resolution: Human explanation and human/table metadata formatters sort dictionary keys with StringComparer.Ordinal. Mixed-case keys inserted in reverse order verify the output order. See spec-ten-deferred-search-response-reliability.md.

### DW-251: `NaN` / `Infinity` confidence or composite scores poison the JSON envelope

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs
reason: - **`NaN` / `Infinity` confidence or composite scores poison the JSON envelope** — If the server ever emits non-finite floats, human/table prints `NaN`/`Infinity` and the JSON envelope emits bare `NaN` tokens that strict parsers (jq, `JSON.parse`, Python `allow_nan=False`) reject. Contracts don't currently enable `AllowNamedFloatingPointLiterals`; treat as contract-boundary work if non-finite scores ever become legitimate. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs]
status: done 2026-11-24
resolution: REST decoding returns INVALID_RESPONSE for non-finite search scores, explanation weights, or memory metadata confidence. Overflow regressions cover the REST reader and the CLI human, table, and JSON command paths. See spec-ten-deferred-search-response-reliability.md.

### DW-252: `IOutputFormatter<T>.Write` signature has no `CancellationToken`

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
archived: 2026-11-23

### DW-253: `Uri.EscapeDataString` on path-segment IDs produces `%2F` for embedded slashes

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810
reason: - **`Uri.EscapeDataString` on path-segment IDs produces `%2F` for embedded slashes** — ASP.NET Core rejects `%2F` in path segments by default (404 Not Found), surfacing as an opaque "not found" error to the user. IDs containing `/` are unusual; the clean fix is CLI-side rejection or server-side `AllowEncodedSlashes`. Server concern. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810]
status: done 2026-11-24
resolution: Compatibility CLI commands reject slash-bearing path identifiers with INVALID_INPUT before dispatch. CLI command tests cover the affected routes and lookup tests preserve source URIs in query parameters.

### DW-254: `BuildSearchPath` drops subpath when `--endpoint` has one

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath
reason: - **`BuildSearchPath` drops subpath when `--endpoint` has one** — Constructing `"api/search?..."` as a relative URI against an `HttpClient.BaseAddress` like `http://host:5000/v1` drops `/v1` per `Uri` resolution rules. No 7.2 AC exercises subpath endpoints; Story 7.1 owns endpoint normalization. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath]
status: done 2026-11-24
resolution: Both REST search methods keep the configured endpoint path prefix. Client tests cover slash-terminated, unterminated, and percent-escaped prefixes. See spec-ten-deferred-search-response-reliability.md.

### DW-255: `--max-results abc` exits via System.CommandLine default error

origin: migrated from legacy ledger ("Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs Options
reason: - **`--max-results abc` exits via System.CommandLine default error** — Non-integer input bypasses the 7.2 "plumbing = 2" exit-code contract because System.CommandLine's built-in parser emits its own error format. Parser-level concern consistent with the Story 7.1 baseline; neither story tests this edge. [src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs Options]
status: done 2026-11-24
resolution: CLI search validates the integer in its command action, returning INVALID_INPUT and plumbing exit code 2 for a nonnumeric or overflowing value. JSON command tests pin the envelope. See spec-ten-deferred-search-response-reliability.md.

### DW-256: `IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
archived: 2026-11-23

### DW-257: Port availability check binds `IPAddress.Loopback` only; IPv6-only services missed, and bind-success is TOCTOU-advisory on Windows port reservations

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:182
reason: - **Port availability check binds `IPAddress.Loopback` only; IPv6-only services missed, and bind-success is TOCTOU-advisory on Windows port reservations** — platform-specific caveat is already acknowledged in spec Task 2.4; upgrade to dual-stack check and/or `IpGlobalProperties.GetActiveTcpListeners` lookup when touched next. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:182]
status: done 2026-11-24
resolution: The advisory check binds both loopback addresses when available. Tests cover an occupied IPv6-only port and the quickstart guide records the platform reservation caveat.

### DW-258: `EnsureSampleCaseAsync` picks first match of `DefaultCaseName` without stable ordering

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:276-283
reason: - **`EnsureSampleCaseAsync` picks first match of `DefaultCaseName` without stable ordering** — only manifests after repeated failed runs leave duplicate cases in the tenant. Low likelihood; will be neutralized if `CreateCaseAsync` is removed per the open decision. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:276-283]
status: done 2026-11-24
resolution: The server sorts cases by newest creation time and ordinal ID before applying the limit. Quickstart uses the same order for identically named sample cases regardless of response order.

### DW-259: `NegativeCanaryQuery` is a literal constant — any future fixture copying the token into a sample body silently breaks the canary invariant

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:31
reason: - **`NegativeCanaryQuery` is a literal constant — any future fixture copying the token into a sample body silently breaks the canary invariant** — add a startup self-check (assert `SampleDocumentText` does not contain the canary token) when touched next. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:31]
status: done 2026-11-24
resolution: Quickstart verifies at startup that the negative canary is absent from the embedded document. Tests verify both submitted sample bodies exclude the token.

### DW-260: `HealthStep` failure suggestion interpolates `result.LastError` raw

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs:349-354
reason: - **`HealthStep` failure suggestion interpolates `result.LastError` raw** — may surface exception messages with proxy URLs or paths in CI logs. Low sensitivity for a dev wizard; sanitize when the wizard grows a non-local-dev use case. [src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs:349-354]
status: done 2026-11-24
resolution: The health probe returns a fixed diagnostic after an exception. The regression test injects a credential-bearing proxy URL and pins the safe diagnostic.

### DW-261: `DurationMs` uses `checked((int)Math.Round(...))` — overflows at ~24.85 days and would throw mid-serialization

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs:89
reason: - **`DurationMs` uses `checked((int)Math.Round(...))` — overflows at ~24.85 days and would throw mid-serialization** — real-world risk negligible given the wizard's bounded 60s probe + 30s provisioning. Switch to `long` or clamp if the wizard ever grows unbounded polls. [src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs:89]
status: done 2026-11-24
resolution: Quickstart durations preserve away-from-zero rounding and clamp to the existing Int32 JSON range. Serialization tests cover both bounds and ordinary rounding.

### DW-262: `CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
archived: 2026-11-23

### DW-263: `PrerequisiteCheckResult.IsSkipped` is a 7.4 refinement beyond the spec-pinned 3-field signature

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs:18
reason: - **`PrerequisiteCheckResult.IsSkipped` is a 7.4 refinement beyond the spec-pinned 3-field signature** — provides clearer "advisory pass" UX (SKIP vs. OK-with-advisory). Spec Task 2.1 should be amended to acknowledge the 4-field record and the "SKIP for soft-fail" rendering convention. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs:18]
status: done 2026-11-24
resolution: Story 7.4 documents IsSkipped and the optional Dapr sub-check rendering. Quickstart tests cover both a missing and an installed Dapr CLI.

### DW-264: `CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)"), 2026-09-01
archived: 2026-11-23

### DW-265: `IngestAsync` has no client-side content-size guard

origin: migrated from legacy ledger ("Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)"), 2026-09-01
location: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401
reason: - **`IngestAsync` has no client-side content-size guard** — server rejects >1 MiB via `IngestionInputValidator` (returns `INVALID_INPUT`) and >2 MiB via `RequestSizeLimitAttribute` (returns 413 with no `ErrorResponse` body, so `ErrorResponseDecoder.DecodeAsync` yields a terse diagnostic). UX polish, not correctness — the quickstart sample is well under 1 MiB. Revisit if `IngestAsync` grows beyond quickstart scope. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]
status: done 2026-11-24
resolution: The REST ingest and refresh paths share the server-side 1 MiB limit and reject oversized content before sending HTTP. The retained MCP tool preserves INVALID_INPUT for oversized UTF-8 data. Boundary and error-mapping tests passed.

### DW-266: Semantic re-index remains intentionally unsupported in `SemanticIndexer`

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-consistency-verification-and-repair (2026-04-20)"), 2026-09-01
location: src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84
reason: - **Semantic re-index remains intentionally unsupported in `SemanticIndexer`** — `SemanticIndexer.ReIndexFromSyntacticAsync` still throws `NotSupportedException`, so `ReIndexSemantic` / `ReIndexSemanticAndGraph` remain documented follow-up work rather than live repair paths. [src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84]
status: open
note: 2026-11-24 — Skipped. The requested syntactic-hash repair has been superseded by Story 34.30. Repair now requires an authoritative EventStore revision and active write fencing before regenerating vectors.

### DW-267: Classification metadata is not persisted into the syntactic export source of truth

origin: migrated from legacy ledger ("Deferred from: code review fix pass of 8-3-data-export (2026-04-20)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/IndexInput.cs, src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, src/Hexalith.Memories.Server/Cases/CaseService.cs
reason: - **Classification metadata is not persisted into the syntactic export source of truth** — `MemoryUnit.Classification` exists on the contract/export surface, but the ingestion/indexing path (`IndexInput`, `IndexSyntacticActivity`) does not write classification into the Redis memory-unit hash. `CaseService.ParseMemoryUnitFromHash` and `TenantExportService` therefore cannot recover it during export. Fix in a future story by persisting classification at ingest/index time, then plumb it through export. [src/Hexalith.Memories.Contracts/V1/IndexInput.cs, src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, src/Hexalith.Memories.Server/Cases/CaseService.cs]
status: open
note: 2026-11-24 — Skipped. The target IndexInput moved and the source-metadata field is absent from ingestion and retained source artifacts. Capturing it requires a caller contract and authoritative persistence through the EventStore source path before projecting it into Redis and exporting it.

### DW-268: Tier-3 Aspire end-to-end integration tests for EventStore subscription

origin: migrated from legacy ledger ("Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)"), 2026-09-01
location: tests/Hexalith.Memories.EventStore.Tests/
reason: - **Tier-3 Aspire end-to-end integration tests for EventStore subscription** — Task 6 guard tests that require a running DAPR sidecar + Redis + FalkorDB are deferred to a follow-up Tier-3 / nightly harness. This covers: `EventIngestionRoundTripTests` (publish via DAPR → search within 5 s, NFR6), `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic` + `Startup_FailsFast_WhenSubscribeEndpointEmpty`, `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency`, and `EventIngestionLatencyTests.SingleEvent_P50Under3s_Enforcement` / `SingleEvent_P95Under5s_Observation`. The existing Tier-1 (65 tests, `tests/Hexalith.Memories.EventStore.Tests/`) + Tier-2 (10 tests, `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/` — outcome mapping, middleware-order, documentation completeness) coverage pins every non-end-to-end guard test from the story's risk table. Rationale: standing up the full Aspire topology for one subscription round-trip is disproportionate to the risk the Tier-2 tests already cover at the controller / outcome-mapping level; the replay-after-restore case is behaviorally correct as long as `CheckIdempotencyActivity` runs (a Tier-2 concern covered by `CheckIdempotencyActivityTests`). The nightly Aspire harness is the right place to catch sidecar / broker wiring regressions.
status: open
note: 2026-11-24 — Skipped. Nightly already verifies the Dapr publish-to-search path and duplicate delivery. The full suite still needs a supported restore-and-replay procedure for the same tenant, which is pending DW-733, plus a latency sampling harness.

### DW-269: Review findings from the planning validation (Review Findings block in the story file)

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)"), 2026-09-01
archived: 2026-11-23

### DW-270: Activity / HttpResponseMessage using-scope pattern inconsistency in retry test

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-end-to-end-telemetry-integration-tests (2026-04-22)"), 2026-09-01
location: n/a
reason: - **Activity / HttpResponseMessage using-scope pattern inconsistency in retry test** — `SearchOperation_RetrySequence_EmitsDistinctAuditEventsPerStatus` uses mixed `using` statement vs `using` statement scopes for `Activity retryRoot` vs `Activity secondAttempt`; the pattern is correct, just inconsistent. Pre-existing style nit, not caused by 8.4 scope boundary.
status: done 2026-11-24
resolution: The nightly audit retry test now scopes both HTTP responses and attempt spans to matching sibling blocks. The live Dapr/Aspire test passed.

### DW-271: `OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease pin — upgrade-on-GA trigger.

origin: migrated from legacy ledger ("Deferred from: 8-5-redis-otel-instrumentation (2026-04-23)"), 2026-09-01
location: Directory.Packages.props, NuGet.config, docs/dev/telemetry.md ADR-8.5-001
reason: - **`OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease pin — upgrade-on-GA trigger.** Package pinned at `1.15.1-beta.1` in `Directory.Packages.props` per ADR-8.5-001 (b). Revisit this pin within **14 days of `1.15.0`** (non-prerelease) shipping on nuget.org, **OR by 2026-09-30**, whichever comes first. Owner: Memories release-manager rotation. Review-by: **2026-09-30**. On review, either (a) bump to the GA version and remove the `-beta.N` tag from `Directory.Packages.props` + update `packageSourceMapping` comment, or (b) file a new deferred-work entry with a fresh review-by if GA is still not shipped. Tracking: ADR-8.5-001 (g). [Directory.Packages.props, NuGet.config, docs/dev/telemetry.md ADR-8.5-001]
status: done 2026-11-24
resolution: NuGet still lists only prerelease versions and the imported Hexalith.Builds catalog pins 1.21.0-beta.1. ADR-8.5-001 documents the review and tracks the next review in DW-739.

### DW-272: Malformed or truncated Redis breadcrumbs are still silently dropped by `ServerActivityStreamReader`.

origin: migrated from legacy ledger ("Deferred from: code review of 8-5-redis-otel-instrumentation (2026-04-23)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs
reason: - **Malformed or truncated Redis breadcrumbs are still silently dropped by `ServerActivityStreamReader`.** `TryParse(...)` catches `JsonException` and returns `null`, so the Story 8.5 hard Redis-span assertion can report a missing span when the real failure is capture corruption / truncation. Deferred as pre-existing review debt in the existing stderr-breadcrumb reader path. [tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs]
status: done 2026-11-24
resolution: The trace reader identifies malformed Memories-server activity breadcrumbs with a sanitized capture position. Seven parser cases cover truncation, malformed fields, and ordinary log messages that mention the marker.

### DW-273: Task 8.7 — `RateLimiterSizingValidator` (Winston promoted, Improvement AB).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 8.7 — `RateLimiterSizingValidator` (Winston promoted, Improvement AB).** Validator emits `9163 RateLimiterUnderSizedForEvents` at Warning when a tenant's `EmbeddingRateLimiterActor` ceiling is below `sustainedUsage * 2` over a 15-min sliding window. Core dual-embedding path ships without the validator; the degraded-state queue path already protects against cascade failure under doubled API volume. Follow-up: add `RateLimiterSizingValidator.cs` that reuses the retry hosted service's scheduling slot, plus unit tests `.SustainedUnderSizing_Emits9163 / .TransientBurst_DoesNotEmit / .CeilingSufficient_DoesNotEmit`.
status: open
note: 2026-11-24 — Skipped. This is the sizing validator tracked by DW-208. No production measurement has triggered a sizing policy and the actor still exposes no sustained-utilization signal.

### DW-274: Task 8.8 — `memories retry-nl-embeddings` CLI surface (Improvement F).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 8.8 — `memories retry-nl-embeddings` CLI surface (Improvement F).** Dead-letter inspection + re-enqueue is interim via `redis-cli` commands (documented in operator runbook section 10.1.3). Follow-up: add the sub-command to `Hexalith.Memories.Cli` once that project surfaces — current story scope ships no CLI project. Interim commands: `redis-cli ZCARD nl-embedding-retry-dead:{tenant}` + `redis-cli ZRANGEBYSCORE ... | xargs redis-cli ZADD nl-embedding-retry:{tenant} ...`.
status: open
note: 2026-11-24 — Skipped. The new command would extend the frozen proprietary CLI. The dead-letter operation needs an authorized McpCli contract or another designated owner, as recorded in DW-209.

### DW-275: Task 9.1 — `DualEmbeddingRoundTripTests` (Tier 2/3).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Ingestion/
reason: - **Task 9.1 — `DualEmbeddingRoundTripTests` (Tier 2/3).** Publishes a test CloudEvent via `DaprClient.PublishEventAsync` and polls both raw + NL hashes for dual indexing within 7s. Requires Aspire `DistributedApplicationTestingBuilder` + DAPR slim + Redis + FalkorDB + `conversation.echo` component. Follow-up: add under `tests/Hexalith.Memories.IntegrationTests/Ingestion/`.
status: done 2026-11-24
resolution: The live nightly test publishes one CloudEvent through Dapr and checks both persisted vectors, their tenant and case, the AI description, the resolved graph node, and the completed Indexed workflow result within seven seconds.

### DW-276: Task 9.2 — `OutOfOrderEventTests` (Tier 2/3).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.2 — `OutOfOrderEventTests` (Tier 2/3).** Publishes event B (with `causationid = A_id`) before event A; asserts stub node is created with `isStub = true` + `stubCreatedAt` set; publishes A; asserts stub promoted (`isStub = false`, `9154 StubNodeResolved` emitted). Requires FalkorDB integration fixture + replay window.
status: open
note: 2026-11-24 — Skipped. The active CloudEvent mapper does not propagate causationid and the event source ID is distinct from the indexed memory-unit ID. A producer mapping and stable identity resolution are required before the requested Dapr publication can prove stub promotion.

### DW-277: Task 9.3 — `DegradedNaturalLanguageEmbeddingTests` (Tier 2, 3 scenarios).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.3 — `DegradedNaturalLanguageEmbeddingTests` (Tier 2, 3 scenarios).** Scenario A: LLM transient failure → `Queued` + retry completes on next tick. Scenario B: index-side partial failure → workflow-level retry recovers. Scenario C: index-side terminal failure → compensation drops both hashes. Requires NSubstitute-replaceable DAPR Conversation client + fault injector for `IndexNaturalLanguageSemanticActivity`.
status: open
note: 2026-11-24 — Skipped. The real Dapr call surfaces activity failures as WorkflowTaskFailedException. The ingestion branch catches the original description exception only, and the current integration topology exposes no per-activity NL index fault seam. The original three-scenario suite needs the durable failure contract and fault controls.

### DW-278: Task 9.4 — `CorrelationRootEdgeTests` (Tier 2 FalkorDB fixture).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.4 — `CorrelationRootEdgeTests` (Tier 2 FalkorDB fixture).** Publishes 1 root + 3 correlated events; asserts root has no self-edge, each correlated event has exactly one edge from root, no edges between correlated events, `9155` emitted once. Guard for Risk #3 at the integration level — the unit test already covers the activity-level behavior via `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `.CorrelationIdEqualsMemoryUnitId_NoSelfEdge_LogsDebug`.
status: open
note: 2026-11-24 — Skipped. The current CloudEvent mapper does not propagate correlationid or resolve event IDs to graph node IDs. The integration proof depends on that producer contract. Existing unit coverage verifies the root-only edge pattern.

### DW-279: Task 9.5 — `IngestionWorkflowReplaySafetyTests` (Tier 1).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Task 9.5 — `IngestionWorkflowReplaySafetyTests` (Tier 1).** Simulates a 9.1-shape history replaying under 9.2 code. Requires fabricating a `durable-task` state snapshot — the SDK surface for this is undocumented. Document the failure mode as deterministic (it is) and rely on the runbook quiesce (AC #11) + `WorkflowReplaySafetyHostedService` startup gate (Task 5.9) as the combined mitigation.
status: open
note: 2026-11-24 — Skipped. There is no captured pre-9.2 durable-task history that can verify replay against the current workflow. A fabricated mock transcript cannot establish compatibility.

### DW-280: Per-tenant LLM provider configuration (Phase 2 per architecture L1254).

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: deploy/dapr/components/conversation-llm.yaml
reason: - **Per-tenant LLM provider configuration (Phase 2 per architecture L1254).** MVP ships one system-wide `llm` DAPR Conversation component. Operators swap providers by editing `deploy/dapr/components/conversation-llm.yaml` (no code change required). Per-tenant LLM is tracked as a Phase 2 follow-up.
status: open
note: 2026-11-24 — Skipped. This duplicates the Phase 2 provider selection and credential-routing decision already recorded in DW-211.

### DW-281: Content-absent fallback in `GraphTraversalService`

origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:~95-100
reason: - **Content-absent fallback in `GraphTraversalService`** (`src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:~95-100`). Retire after the Task 7.6 `IsStubBackfillMigration` has been executed against all production databases. Until then, the fallback MUST be kept — pre-9.2 stubs have neither the `isStub` flag nor content.
status: open
note: 2026-11-24 — Skipped. The recorded prerequisite is confirmed stub-backfill execution on every production graph. That coverage is unavailable.

### DW-282: Integration tests for `NaturalLanguageSemanticSearchService`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-283: Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
archived: 2026-11-23

### DW-284: Risk-mapped guard tests that depend on integration topology

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk-mapped guard tests that depend on integration topology** — `DualEmbeddingLatencyTests` (Risk #2, P95 benchmark), `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` (Risk #11), `GraphQueryBuilderTests.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (Risk #12 Tier-2 FalkorDB fixture), `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` (Risk #17). All bound to the Task 9.x integration-test deferral above.
status: open
note: 2026-11-24 — Skipped. The risk bundle includes replay of a pre-9.2 activity history and a P95 workload baseline. No historical history fixture or controlled baseline is available. The conversation path has direct live coverage in DW-275.

### DW-285: Risk #3 coverage gap at unit level

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #3 coverage gap at unit level** — `IndexGraphActivityTests.MultipleEventsSameCorrelationId_NoFanOut` and `GraphTraversalServiceTests.CorrelatedWith_InboundDirection_ReturnsCorrelatedSiblings` — the fan-out-prevention and inbound-traversal tests name-listed in Risk #3 that go beyond the two shipped tests. Tied to Task 9.4 `CorrelationRootEdgeTests`.
status: done 2026-11-24
resolution: The traversal regression decodes the FalkorDB compact edge-map response for a correlated sibling and verifies the scoped CorrelatedWith query. The existing indexing test covers the no-fan-out producer behavior.

### DW-286: Risk #4 gap-marker unit tests

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
location: n/a
reason: - **Risk #4 gap-marker unit tests** — `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes`. Tied to Task 9.2 `OutOfOrderEventTests`.
status: done 2026-11-24
resolution: The traversal test proves explicit stubs and legacy content-absent nodes are gap markers, while an explicitly non-stub node remains a normal result.

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-303: S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-304: S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-305: S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-306: S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-307: S6-FE. Normalize the `ConsistencyVerificationResultHumanFormatter` "notes: none" / "Notes:" header layout across the discrepancy-empty / discrepancy-populated branches.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
location: n/a
reason: - **S6-FE. Normalize the `ConsistencyVerificationResultHumanFormatter` "notes: none" / "Notes:" header layout across the discrepancy-empty / discrepancy-populated branches.** Cosmetic; minimal operator impact.
status: open

### DW-308: S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)"), 2026-09-01
archived: 2026-11-23

### DW-309: W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-310: W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-311: W3. `tools/test.sh` Python heredoc has no error path if `python3` is missing.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh
reason: - **W3. `tools/test.sh` Python heredoc has no error path if `python3` is missing.** Linux/macOS runners always have it; Windows uses `test.ps1`. Add `command -v python3` guard with a clear error if Linux/macOS distros are added later that omit it. (`tools/test.sh:134`)
status: done 2026-11-23
resolution: 2026-11-23 — spec-five-deferred-integration-tooling.md. The Bash wrapper checks that python3 can run and import the report parser before replacing the results directory or running dotnet. Isolated PATH regressions prove that existing reports survive when Python is missing or broken and that runs without a results directory still work.

### DW-312: W4. Python `{*}Counters` namespace XPath requires Python ≥ 3.8.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh:140
reason: - **W4. Python `{*}Counters` namespace XPath requires Python ≥ 3.8.** Current ubuntu-latest ships 3.10+; document as a runner-version floor or hardcode the TRX namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`. (`tools/test.sh:140`, `tools/verify-integration-fast-coverage.py:54`)
status: done 2026-11-23
resolution: 2026-11-23 — spec-five-deferred-integration-tooling.md. The Bash runner and integration-fast verifier qualify element names from the TRX root namespace, avoiding wildcard XPath. The focused suite exercises standard namespaced and namespace-free reports through both entry points.

### DW-313: W5. `if-no-files-found: error` + `if: always()` upgrades a build failure to two red checks.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml:67-74,108-115
reason: - **W5. `if-no-files-found: error` + `if: always()` upgrades a build failure to two red checks.** Noisy but correct; reviewers must read the build step first. Resolve by gating artifact upload on `success() || failure()` against the test step specifically. (`.github/workflows/ci.yml:67-74,108-115`)
status: open

### DW-314: W6. `submodules: recursive` cannot fetch private submodules without PAT.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-315: W7. `Substitute.For<WorkflowActivityContext>()` may fail if Dapr.Workflow seals the type in a future SDK.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs:330
reason: - **W7. `Substitute.For<WorkflowActivityContext>()` may fail if Dapr.Workflow seals the type in a future SDK.** Works against 1.17.6; failure mode is loud (NSubstitute throws at instantiation). (`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs:330`)
status: open

### DW-316: W9. CONTRIBUTING.md skip wording (`Requires Docker - see CONTRIBUTING.md`) is documented but not wired into any test SkipAttribute.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **W9. CONTRIBUTING.md skip wording (`Requires Docker - see CONTRIBUTING.md`) is documented but not wired into any test SkipAttribute.** Spec text is aspirational, not enforced as a contract. Wire it via a custom SkipAttribute when Docker-required local skips are needed. (`CONTRIBUTING.md:76-81`)
status: done 2026-11-23
resolution: 2026-11-23 — spec-five-deferred-integration-tooling.md. CONTRIBUTING.md now points contributors without Docker to the unit and contract lane and describes a running engine as a prerequisite for integration and benchmarks. The guide no longer promises a skip diagnostic.

### DW-317: W11. `branch-protection.md` is a manual checklist with no automation.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/rulesets/main.json
reason: - **W11. `branch-protection.md` is a manual checklist with no automation.** Commit a `.github/rulesets/main.json` and a daily audit workflow. Out of scope for 11.x. (`docs/dev/branch-protection.md`)
status: open

### DW-318: W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-319: W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-320: W14. CI workflow `fetch-depth: 0` not set on PR checkout.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-321: W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-322: W16 [partially resolved in 14.2]. `Where-Object {-notlike *.snupkg}` masks regression risk.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W16 [partially resolved in 14.2]. `Where-Object {-notlike *.snupkg}` masks regression risk.** `tools/validate-release-packages.ps1` now uses `Where-Object { $_.Extension -ieq '.nupkg' }` for explicit extension matching. The mirror in `tools/publish-nuget.ps1:40-42` is intentionally not touched in 14.2 because the story's file scope only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic alignment alone does not meet that bar. Re-open trigger: a partial-publish recovery story that already touches `publish-nuget.ps1`, or first .snupkg-symbol introduction.
status: done 2026-11-24
resolution: The package validator and publisher both require regular files with the exact .nupkg extension. The successful publish fixture excludes symbols, directories and links. Reopen if the two selectors accept different staged files. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-323: W17. `verify-integration-fast-coverage.py` exit codes don't distinguish "missing surface" from "tool error".

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/verify-integration-fast-coverage.py
reason: - **W17. `verify-integration-fast-coverage.py` exit codes don't distinguish "missing surface" from "tool error".** Both yield exit 1; use distinct codes (e.g., 2 for parse error, 3 for empty results, 1 for missing surfaces). (`tools/verify-integration-fast-coverage.py`)
status: done 2026-11-23
resolution: 2026-11-23 — spec-five-deferred-integration-tooling.md. The integration-fast verifier distinguishes covered surfaces (0), missing surfaces (1), invalid input (2) and unusable evidence (3). Focused subprocess tests cover the exit codes and input diagnostics.

### DW-324: W18. CI `runs-on: ubuntu-latest` is unpinned.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: .github/workflows/ci.yml
reason: - **W18. CI `runs-on: ubuntu-latest` is unpinned.** Works today; pin to `ubuntu-22.04` if Docker engine version drift causes Testcontainers regression. (`.github/workflows/ci.yml`)
status: open

### DW-325: W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-326: W20. Release workflow runs build+restore+test+pack twice.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: n/a
reason: - **W20. Release workflow runs build+restore+test+pack twice.** Pre-release validation + semantic-release internal pack pipeline duplicate work; optimize when CI minutes become a constraint.
status: open

### DW-327: W21. `tools/test.sh` Slow/Integration arms collapsed via `|`.

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/test.sh
reason: - **W21. `tools/test.sh` Slow/Integration arms collapsed via `|`.** Functionally correct today (same project list); diverges from `test.ps1`. Resync if Slow ever gets its own list. (`tools/test.sh:79`)
status: done 2026-11-23
resolution: 2026-11-23 — spec-five-deferred-integration-tooling.md. The separated fast and slow inventories already route correctly in Bash and PowerShell. Added executable regressions for both wrappers covering project selection and trait arguments for the all-integration, fast, slow and performance selectors.

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
status: resolved 2026-10-28
resolution: `spec-five-deferred-ci-inventory.md` — ResolveRepoRoot validates missing/blank metadata with an actionable AssemblyMetadata Include="RepoRoot" hint naming Hexalith.Memories.Cli.Tests.csproj and rebuild instructions; null/empty/whitespace and absolute-path positive cases execute that resolver. Final inventory suite passed 117/117 cases and the complete CLI assembly passed 564/564, with no failures/skips.
reopen_trigger: A release workflow or deferred-register format change escapes or falsely fails the corresponding inventory regression.

### DW-331: S11-FB. Compile-time symbol verification for `tools/integration-fast-required-surfaces.txt` (review patch P9).

origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
location: tools/integration-fast-required-surfaces.txt
reason: - **S11-FB. Compile-time symbol verification for `tools/integration-fast-required-surfaces.txt` (review patch P9).** Currently the verifier surfaces missing classes only after CI runs the lane. Promoting the check to compile-time requires either (a) a `ProjectReference` to `Hexalith.Memories.IntegrationTests` from a Docker-free test project (pulls integration deps), (b) a refactor of the surfaces file into a typed C# inventory consumed by both the verifier (after build) and a unit test, or (c) a `dotnet test --list-tests` step in CI before the test run. Re-open trigger: first surface drift incident, or the next time the surfaces list grows past ~10 entries.
status: open

### DW-332: S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-333: S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)"), 2026-09-01
archived: 2026-11-23

### DW-334: 12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-335: 12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-336: 12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-337: 12.1-RV4. Verify `package-lock.json` is tracked in git.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-338: 12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-339: 12.1-RV6 [resolved in 12.1].

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.1 (2026-04-30)"), 2026-09-01
archived: 2026-11-23

### DW-340: 12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-341: 12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-342: 12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-343: 12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-344: 12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-345: 12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-346: 12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-347: 12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-348: 12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-349: 12.4-RV10. `extract_backtick_path` silently drops bare-token bullets without an author-facing diagnostic.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:204-212
reason: - **12.4-RV10. `extract_backtick_path` silently drops bare-token bullets without an author-facing diagnostic.** `tools/check-story-file-scope.py:204-212` — author who forgets backticks gets no warning. Story 12.3 author UX.
status: done 2026-11-24
resolution: The current parser emits a file-and-line warning for an unbackticked allowed-scope bullet. The scope fixture proves the warning and the subsequent refusal if the bare path is changed.

### DW-350: 12.4-RV11. `to_posix(path)` embeds Windows drive letter in diagnostic header.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: tools/check-story-file-scope.py:347-348
reason: - **12.4-RV11. `to_posix(path)` embeds Windows drive letter in diagnostic header.** `tools/check-story-file-scope.py:347-348` — emit `story_path.relative_to(REPO_ROOT).as_posix()` instead. Story 12.3 territory; cosmetic.
status: done 2026-11-24
resolution: The current display_path helper omits Windows drive and UNC anchors. The diagnostics fixture covers same-drive, cross-drive and UNC paths with Windows path semantics.

### DW-351: 12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-352: 12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-353: 12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-354: 12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-355: 12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-356: 12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-357: 12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-358: 12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
archived: 2026-11-23

### DW-359: 12.4-RV20. AC #1 strict literal per-SHA replay drill.

origin: migrated from legacy ledger ("Deferred from: code review of story-12.4 (2026-05-01)"), 2026-09-01
location: n/a
reason: - **12.4-RV20. AC #1 strict literal per-SHA replay drill.** Story 12.4 satisfied AC #1 via HEAD-replay coverage (HEAD strictly includes Epic 8.x SHA `d7495a3`, Epic 9.x SHA `bc4d5cc`, and Epic 10.x SHA `8207b54` in its ancestry, and the surviving test inventory at HEAD is a superset of those completion states — see Story 12.4 Decision Resolutions D3). A literal interpretation of AC #1 would also exercise each anchor SHA via `git checkout`, restore, build, and run both authoritative lanes against that exact tree. Re-open trigger: a release post-mortem that traces a regression to a test that existed at one of the named SHAs and was silently fixed before HEAD; or a future quality-discipline story that prefers strict literal AC #1 evidence over inheritance argumentation.
status: open

### DW-360: 12.4-RV1: closed. `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-361: 12.4-RV2: closed. The push-fallback path now resolves an explicit `base_sha=$(git rev-parse

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-362: 12.4-RV3: closed. When `origin/main` resolves to the same commit as `head_sha`, the job

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-363: 12.4-RV4: closed. `BRANCH_NAME` heredoc delimiter is randomized per run as

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-364: 12.4-RV5: closed. Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-365: 12.4-RV7: closed. `--story-key` rejects values containing more than one story key and lists

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-366: 12.4-RV8: closed. Branch-name parsing rejects branches whose value contains more than one

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-367: 12.4-RV9: closed. Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection,

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-368: 12.4-RV12: closed. `parse_allowed_scope` tracks the open fence's marker character and

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-369: 12.4-RV13: closed. Allow-list collection terminates only on known section labels

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-370: 12.4-RV14: closed. `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git`

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-371: 12.4-RV15: closed. `section_block` helper no longer terminates on blank lines; only

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-372: 12.4-RV16: closed. `test_branch_and_trailer_agreement_passes` asserts the diagnostic does

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-373: 12.4-RV17: closed. `test_unparseable_explicit_story_key_fails_closed` matches against

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-374: 12.4-RV18: closed. `test_fixture_test_reports_loaded_story_artifact_path` pins the full

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-375: 12.3-RV15: closed. Multi-block `Allowed files for this story:` parsing is now exercised by

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-376: 12.4-RV6: out of 14.1 scope (CI test inventory parser, not the story-scope lane).

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-377: 12.4-RV10: the existing `Out-of-scope files:` diagnostic surfaces dropped bare-token bullets

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV10** — the existing `Out-of-scope files:` diagnostic surfaces dropped bare-token bullets whenever a contributor's changed-file landing references one. A separate parse-time stderr warning would help story authors before any commit, but adding it was not part of 14.1's ACs and risks noisy false positives on legitimate non-bullet prose. Re-open trigger: an author-confusion incident or a story template redesign that needs pre-commit author warnings.
status: done 2026-11-24
resolution: The current parser issues a parse-time warning for a bare bullet even when that file is absent from the changed set. The scope fixture checks both the warning and the later scope refusal.

### DW-378: 12.4-RV11: cosmetic only. CI uses a repo-relative `_bmad-output/implementation-artifacts`

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: _bmad-output/implementation-artifacts
reason: - **12.4-RV11** — cosmetic only. CI uses a repo-relative `_bmad-output/implementation-artifacts` artifacts root, so production diagnostics never embed a drive letter; the issue surfaces only in local Windows runs. Re-open trigger: a maintainer-visible diagnostic that exposes a local Windows path in a maintainer-facing channel.
status: done 2026-11-24
resolution: The Windows diagnostics fixture verifies repo-relative same-drive paths and drive-free external diagnostics. CONTRIBUTING.md documents both forms.

### DW-379: 12.4-RV19: out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-380: 12.4-RV20: out of 14.1 scope (Story 12.4 strict-literal AC #1 evidence drill).

origin: migrated from legacy ledger ("Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **12.4-RV20** — out of 14.1 scope (Story 12.4 strict-literal AC #1 evidence drill).
status: open

### DW-381: W1: closed. `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`,

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-382: W2: closed. `tools/validate-release-packages.ps1` iterates every `src//*.csproj`, requires

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-383: W12: closed. `tools/release-packages.schema.json` defines required keys with

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-384: W15: closed. `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-385: W19: closed. `concurrency: cancel-in-progress: false` is preserved deliberately to keep

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-386: S11-FD: closed. Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation,

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-387: 12.1-RV1: closed. `docs/dev/release-runbook.md` Per-Release Package Audit Evidence

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-388: 12.1-RV2: closed. `docs/dev/release-runbook.md` Release Identity And Forensic Anchors

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

### DW-389: W3..W11, W13, W14, W17, W18, W20..W24

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **W3..W11, W13, W14, W17, W18, W20..W24** — out of 14.2 scope (CI workflow, test infra, and contracts/CLI hardening unrelated to the release-lane audit). 14.2 limits its file scope to release-pipeline artifacts.
status: open

### DW-390: W16: partially closed. Cleaned up in `tools/validate-release-packages.ps1`. The mirror in

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: tools/validate-release-packages.ps1
reason: - **W16 — partially closed.** Cleaned up in `tools/validate-release-packages.ps1`. The mirror in `tools/publish-nuget.ps1` is intentionally not touched in 14.2 because the story's file scope only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic alignment alone does not meet that bar.
status: done 2026-11-24
resolution: The publish-nuget.ps1 half of W16 now matches validation by exact .nupkg extension and regular file type. Reopen if the actual upload list includes staged symbols, directories or links. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-391: S11-FB: out of 14.2 scope (compile-time symbol verification for integration-fast surfaces).

origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
location: n/a
reason: - **S11-FB** — out of 14.2 scope (compile-time symbol verification for integration-fast surfaces).
status: open

### DW-392: S11-FC: carried forward with fresh defer-by 2026-08-04. Stale-tag preflight still requires

status: done 2026-09-01
origin: migrated from legacy ledger ("Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-396: 13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-397: 13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-398: 13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
archived: 2026-11-23

### DW-399: 13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-404: 15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-405: 15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-410: 15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

### DW-411: 15.3-RV27. Stale per-target marker can resume against drifted state.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)"), 2026-09-01
archived: 2026-11-23

### DW-423: 2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-424: 2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-425: 2.7-CR3. No table-driven sanitization tests across the spec'd categories

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-426: 2.7-CR4. No tenant/case negative isolation fixtures.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-427: 2.7-CR5. No server-side `EvidencePacketMapper` tests

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-428: 2.7-CR6. MCP error path uses `UnknownScope()` for known-tenant errors.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR6. MCP error path uses `UnknownScope()` for known-tenant errors.** `McpErrorMapper.cs:61, 86, 105, 132` — only the forbidden branch passes real scope. Rationale: requires plumbing `requestedTenantId`/`@case` through `Map`/`MapGeneric`/`MapValidation` from `SearchMemoryTool`; touches every MCP tool that hits the error mapper.
status: done 2026-11-24
resolution: MCP search diagnostics keep the well-formed tenant and case request context with unknown isolation. Missing or malformed identifiers are suppressed, and search tool tests cover validation and remote errors. See spec-ten-deferred-search-response-reliability.md.

### DW-429: 2.7-CR7. `MapOmittedReason` falls to default `None` for future enum values

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR7. `MapOmittedReason` falls to default `None` for future enum values** (Density, Redaction, Policy, Authorization, TrueAbsence). Rationale: lower-level `OmittedReason` enum doesn't expose those today; reopens when the server starts emitting them.
status: done 2026-11-24
resolution: The mapper explicitly handles all current omission reasons and refuses unknown values with a fixed diagnostic. SearchPackets_RejectUnrecognizedOmissionReasons covers single-axis and degraded hybrid input. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-430: 2.7-CR8. SHA-256 expansion handle truncated to 16 hex (64 bits) + `|`-delimited material allows injection collisions.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR8. SHA-256 expansion handle truncated to 16 hex (64 bits) + `|`-delimited material allows injection collisions.** `EvidencePacketMapper.cs:508-513`. Rationale: needs handle-format decision (length, delimiter, length-prefix vs delimiter).
status: done 2026-11-24
resolution: The mapper emits v2 handles with four length-prefixed fields and 128 bits of SHA-256. The golden vector and v1 JSON round trip are covered in the mapper and serialization suites. See spec-ten-deferred-evidence-packet-mapping.md.
decision: 2026-09-01 Versioned length-prefixed handle — Emit a v2 length-prefixed handle with at least 128 hash bits and accept v1 during migration.
decision: 2026-09-01 Versioned length-prefixed handle — Emit a v2 length-prefixed handle with at least 128 hash bits and accept v1 during migration.

### DW-431: 2.7-CR9. `source.CaseId` copied verbatim from upstream with no scope-consistency check

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR9. `source.CaseId` copied verbatim from upstream with no scope-consistency check** (decision-needed). Rationale: needs design call between trust-upstream / overwrite / skip / throw.
status: done 2026-11-24
resolution: Single-axis and hybrid mapping reject a source that explicitly names a different case. Contracts, CLI and MCP tests confirm the refusal and verify that the failed surface response carries no source results. See spec-ten-deferred-evidence-packet-mapping.md.
decision: 2026-09-06 Reject mismatch — Validate CaseId consistency and return a stable fail-closed error.
decision: 2026-09-06 Reject mismatch — Validate CaseId consistency and return a stable fail-closed error.

### DW-432: 2.7-CR10. CLI does not emit `EvidencePacket` on error responses

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-433: 2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-434: 2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-435: 2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-436: 2.7-CR14. Inconsistent `permissionsContext` values across surfaces

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR14. Inconsistent `permissionsContext` values across surfaces** (`tenant`/`tenant-case`/`mcp-auth`/`mcp-error`). Rationale: needs a single source-of-truth constant list.
status: done 2026-11-24
resolution: EvidencePacketPermissionsContext defines the existing scope vocabulary used by CLI, MCP and the mapper. The contract guide records when each wire value is used. See spec-ten-deferred-search-response-reliability.md.

### DW-437: 2.7-CR15. State precedence does not model `Redaction`/`Policy` states

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR15. State precedence does not model `Redaction`/`Policy` states** (Party-Mode Hardening). Rationale: no upstream signal exists today; paired with 2.7-CR11.
status: open

### DW-438: 2.7-CR16. `Combined` omission reason on degraded result strips token-budget hint from recovery.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR16. `Combined` omission reason on degraded result strips token-budget hint from recovery.** Rationale: paired with 2.7-CR11/CR12 precedence redesign.
status: done 2026-11-24
resolution: Combined backend and budget omissions keep retry and backend health guidance first, followed by IncreaseTokenBudget. The mapper and CLI JSON tests pin the order. See spec-ten-deferred-evidence-packet-mapping.md.
decision: 2026-09-01 Emit additive recoveries — Keep backend recovery primary and add deterministic IncreaseTokenBudget when Combined includes budget omission.
decision: 2026-09-01 Emit additive recoveries — Keep backend recovery primary and add deterministic IncreaseTokenBudget when Combined includes budget omission.

### DW-439: 2.7-CR17. `McpErrorPayload` not registered in source-gen `MemoriesJsonContext`

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR17. `McpErrorPayload` not registered in source-gen `MemoriesJsonContext`** (AOT-only risk; works today via reflection fallback). Rationale: needs Mcp-side source-gen context.
status: done 2026-11-24
resolution: McpErrorJsonContext generates the structured error envelope and packet metadata. MCP mapper and tool tests verify the existing camel-case fields and optional case behavior. See spec-ten-deferred-search-response-reliability.md.

### DW-440: 2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-441: 2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-442: 2.7-CR20. `EvidencePacketResultSummary.Query` echoes raw caller query verbatim

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR20. `EvidencePacketResultSummary.Query` echoes raw caller query verbatim** (defense-in-depth length cap / sanitize). Rationale: caller-supplied, not a leak.
status: done 2026-11-24
resolution: Search and diagnostic packets suppress queries matching the shared sensitive-text filter and limit query display to 1,024 UTF-16 code units. The mapper test covers a surrogate boundary, a sensitive suffix, and distinct handles for identical display text. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-443: 2.7-CR21. Whitespace-only `caseId` inconsistency

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR21. Whitespace-only `caseId` inconsistency** (`permissionsContext: "tenant"` while `scope.caseId: " "`). Rationale: input-validation polish.
status: done 2026-11-24
resolution: Search and diagnostic mapping normalize blank case IDs to tenant scope. SearchAndErrorPackets_NormalizeWhitespaceOnlyCaseScope also checks the expansion handle scope. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-444: 2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-445: 2.7-CR23. `HybridSearchResult.Results` not null-guarded in mapper.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR23. `HybridSearchResult.Results` not null-guarded in mapper.** Rationale: contract guarantees non-null via `required`; defensive-only.
status: done 2026-11-24
resolution: Both mappers reject a null results collection with a fixed diagnostic. SearchPackets_RejectMissingResultsCollections covers single-axis and hybrid results. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-446: 2.7-CR24. `FromSearchResult` passes `null` for `AllEnabledAxesUnavailable` vs hybrid passing actual value.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR24. `FromSearchResult` passes `null` for `AllEnabledAxesUnavailable` vs hybrid passing actual value.** Rationale: cosmetic; semantically correct since single-axis has no multi-axis concept.
status: done 2026-11-24
resolution: The all-axes availability signal is intentionally hybrid-only. The mapping guide and single-axis null regression pin this contract alongside the existing hybrid-degradation test. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-447: 2.7-CR25. `McpErrorMapper.MapAuthorization` forbidden message echoes `requestedTenantId`.

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
location: n/a
reason: - **2.7-CR25. `McpErrorMapper.MapAuthorization` forbidden message echoes `requestedTenantId`.** Rationale: caller-supplied input echoed back, not a confirmation of alternate tenant existence.
status: done 2026-11-24
resolution: Forbidden MCP prose and its message use fixed wording. MapAuthorization_Mismatch_ReturnsTenantForbidden checks both messages and the validated requested scope. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-448: 2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-449: 2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-450: 16.1-CR1. Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*`

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140
reason: - **16.1-CR1. Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*`** (`src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140`). Rationale: operator-error edge; promote to wildcard is consistent with empty-string semantics documented in `ProjectionBinding.SupportedEventTypePatterns` XML doc. Sweep later if any host reports surprise.
status: done 2026-11-24
resolution: Whitespace-only pattern entries grant no coverage. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-451: 16.1-CR2. Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR2. Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split** (`ProjectionBindingMatcher.cs:155-159, 183-187`). Rationale: not produced by current callers; defensive normalization deferred to a normalization sweep.
status: done 2026-11-24
resolution: Trim trailing separators before extracting route and event terminal segments. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-452: 16.1-CR3. Embedded `\` in event names not normalized to `.` or `/`

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR3. Embedded `\` in event names not normalized to `.` or `/`** (`ProjectionBindingMatcher.cs:135`). Rationale: serializers in this stack emit `.`-separated event types; backslash variant is theoretical.
status: done 2026-11-24
resolution: Normalize backslash-separated event names to their terminal segment. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-453: 16.1-CR4. Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR4. Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes** (`ProjectionBindingMatcher.cs:132, 167`). Rationale: current tenants/aggregates are ASCII; revisit when non-ASCII tenant ids are introduced.
status: done 2026-11-24
resolution: Normalize comparison tokens with NFC and invariant casing, with ordinal tenant identity checks. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-454: 16.1-CR5. Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: tenant/source//
reason: - **16.1-CR5. Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key** (`ProjectionBindingMatcher.cs:161`). Rationale: cosmetic — comparison key is internal, only surfaces in Subject if a bare `V2` event reaches the detector; unlikely.
status: done 2026-11-24
resolution: Keep a nonempty key for event names that consist only of a version token. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-455: 16.1-CR6. Tenant-leakage assertion absent on structured log/telemetry payload

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs
reason: - **16.1-CR6. Tenant-leakage assertion absent on structured log/telemetry payload** (`tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`). Rationale: currently no foreign-tenant fields are emitted on the warning; defensive assertion to add when telemetry shape expands.
status: done 2026-11-24
resolution: Assert requested tenant and route on structured mismatch logs and meter tags. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-456: 16.1-CR7. Multi-slash collapse (`while (... "//")`) has no direct test

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR7. Multi-slash collapse (`while (... "//")`) has no direct test** (`ProjectionBindingMatcher.cs:127-130`). Rationale: covered transitively by slash-normalization tests; explicit regression test is low priority.
status: done 2026-11-24
resolution: Verify repeated source slashes collapse and duplicate route expectations coalesce. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-457: 16.1-CR8. Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring

origin: migrated from legacy ledger ("Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)"), 2026-09-01
location: n/a
reason: - **16.1-CR8. Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring** (`ProjectionBindingMatcher.cs:108-111`). Rationale: add a targeted test when wildcard semantics is widened.
status: done 2026-11-24
resolution: Verify a nonexact suffix-wildcard prefix matches and an unrelated prefix fails. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-458: 17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-466: 17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-467: 17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-472: 17.1-CR15. Stale state never tested by fixture

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

### DW-473: 17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
archived: 2026-11-23

### DW-491: ID: 17.1-CR16

status: done 2026-09-01
origin: migrated from legacy ledger ("Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-05
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-500: ID: MEM-2

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-501: ID: MEM-2-ASPIRATE

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires.
reason: - ID: MEM-2-ASPIRATE - Status: accepted - Source story: 18-2-deployment-configuration-contract-publication - Target artifact: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires. - Re-open trigger: a downstream consumer needs ready-to-apply Kubernetes/Dapr manifests emitted from the AppHost topology rather than a hand-filled documented contract. - Rationale: Story 19.2 (2026-06-30) accepts the documented-contract approach as sufficient for current consumers and declines to schedule aspirate emission. The maintained deploy-config contract publishes every env/port/OTLP/pub-sub literal an operator must supply, and DeploymentConfigurationContractTests fails the build on doc<->code drift, so consumers fill kustomization placeholders today without generated manifests; no current consumer requires emitted manifests, and no aspirate tooling exists in src/** or tools/**. Per the 2026-05-27 "document now, defer aspirate" locked decision this is accept-until-trigger. Owner: AppHost / release maintainer.
status: open

### DW-502: ID: MEM-3

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-503: ID: MEM-3-OPENAPI

origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
location: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires.
reason: - ID: MEM-3-OPENAPI - Status: accepted - Source story: 18-3-invocable-route-and-operation-surface-publication - Target artifact: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires. - Re-open trigger: a downstream consumer needs a generated OpenAPI/Swagger document (machine-consumable schema for client/ACL generation) rather than the maintained route-surface contract. - Rationale: Story 19.2 (2026-06-30) accepts the maintained route-surface contract as sufficient for current consumers and declines to schedule OpenAPI/Swagger generation. Story 18.3 AC2 explicitly permitted "an OpenAPI document OR a maintained route-surface doc"; route-surface.md publishes the full 46-route ACL-verifiable surface and RouteSurfaceContractTests ties it to Program.cs so it cannot drift. The repo has no AddOpenApi/MapOpenApi/Swashbuckle today and no consumer needs a generated schema; standing up OpenAPI for 46 minimal-API endpoints plus the pub/sub controller is accept-until-trigger. Owner: Server / API maintainer.
status: open

### DW-504: ID: MEM-4

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-505: ID: MEM-5

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-506: ID: MEM-6

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-507: ID: MEM-7

status: done 2026-09-01
origin: migrated from legacy ledger ("Parties Consumer Integration Intake (2026-05-27)"), 2026-09-01
archived: 2026-11-23

### DW-508: `HasIndexedMemoryUnits` captured but never consulted

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:61
reason: - **`HasIndexedMemoryUnits` captured but never consulted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:61] — true-empty (no index for the tenant) is not distinguished from query-empty; both yield `state: empty` + `broadenScope`, which is misleading for an unindexed tenant. Needs a recovery/state design choice (e.g. an "ingest first" recovery when `HasIndexedMemoryUnits==false`).
status: open
decision: 2026-09-06 Build ingest-first proof — Create an unindexed tenant through ingestion and prove its first index.
decision: 2026-09-06 Build ingest-first proof — Create an unindexed tenant through ingestion and prove its first index.

### DW-509: `state: empty` emitted when `returnedCount==0` but `totalCount>0` and nothing omitted

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:224
reason: - **`state: empty` emitted when `returnedCount==0` but `totalCount>0` and nothing omitted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:224] — the `returnedCount==0` short-circuit ignores `totalCount`, producing a self-contradictory empty packet. Low likelihood (requires upstream to report a contradictory result).
status: done 2026-11-24
resolution: A healthy zero-row page with a positive total now maps to partial with preserved counts and retry guidance. SearchPackets_ReportEmptyPageWithMatchesAsPartial covers both mappers. See spec-ten-deferred-evidence-packet-mapping.md.

### DW-510: Graph summary hardcoded `Available=false`/empty even when a `graph` axisEvidence entry has a real `GraphScore`

origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
location: src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:71, 132
reason: - **Graph summary hardcoded `Available=false`/empty even when a `graph` axisEvidence entry has a real `GraphScore`** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:71, 132] — internal inconsistency between graph axis evidence and the graph summary section. Tolerable per spec (explicit-unavailable); graph mapping is optional/out of scope for this story.
status: open

### DW-511: [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

status: done 2026-07-05
origin: migrated from legacy ledger ("Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)"), 2026-09-01
archived: 2026-11-23

### DW-512: 23.7-APPHOST-EVENTSTORE-FULLSTACK: accepted. Current package/source compilation and the

origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Story 28.1)
reason: - **23.7-APPHOST-EVENTSTORE-FULLSTACK - accepted.** Current package/source compilation and the Memories-owned Aspire Redis/FalkorDB + Dapr ingestion lane pass, but the AppHost does not provision an `eventstore` gateway resource and current source/package identities do not match EventStore Story 1.20's exact approved pins. The focused event-ingestion lane publishes directly to Memories and is not EventStore-to-Memories proof. This preserves the original Story 21.2 finding: case, annotation, memory-unit, and case-deletion mutations target the `Hexalith.EventStore.Client` gateway at Dapr app-id `eventstore` by default, while topology/deployment work remained outside the A3 write-boundary closure. The historical 2120-server-test result remains provenance, not current full-stack evidence. - ID: 23.7-APPHOST-EVENTSTORE-FULLSTACK - Status: accepted - Source story: story-21.2 dev; Epic 23 retrospective corrective action - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 28.1) - Rationale: the AppHost lacks the approved `eventstore` gateway topology and the current Memories-owned ingestion lane is not EventStore-to-Memories proof, so Story 28.1 retains the full-stack work and its explicit evidence requirements. - Resolution criteria: adopt the exact owner-approved EventStore source/package identities; compose one `eventstore` gateway resource with unambiguous `statestore`/`pubsub` ownership; run a real EventStore-originating publish through Dapr into Memories; prove persisted/searchable Redis and FalkorDB outcomes plus ignored duplicate replay; attach tenant-isolation negative evidence. - Re-open trigger: Story 28.1 is selected; any story or review claims EventStore-to-Memories or unqualified full-stack EventStore proof; or the AppHost adds an `eventstore` resource without closing every resolution criterion. - **Correction (2026-09-01):** Re-open trigger fired — Story 28.1 was selected and implemented. Source and package identity adoption, and the single `eventstore` gateway resource (unambiguous `statestore`/`pubsub` ownership), are both done and verified. The remaining resolution criterion — "run a real EventStore-originating publish through Dapr into Memories, prove persisted/searchable Redis and FalkorDB outcomes plus ignored duplicate replay, attach tenant-isolation negative evidence" — is not met and, per `epics.md`'s Story 28.1 final Given/When/Then clause ("Given adoption exposes a behavioral incompatibility... fails closed and routes that behavior change to a separately approved compatibility story rather than expanding silently") and spec-28-1's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary, is not attempted further within Story 28.1. (Note: neither the spec nor `epics.md` numbers this clause "AC7" — that was an earlier miscitation, corrected here.) See new entry `28.1-TASK4-FULLSTACK-PROOF-NEEDS-DOMAIN-SERVICE` below for the precise remaining gap and candidate resolutions. This entry stays `accepted` (not `resolved`) until that gap closes.
status: open

### DW-513: Story 24.3 graph content and honest test evidence.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
archived: 2026-11-23

### DW-514: Story 24.3 configured vector-dimension authority.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
archived: 2026-11-23

### DW-515: Story 24.3 collision-safe semantic key-family membership.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
archived: 2026-11-23

### DW-516: Story 24.3 distinct and non-destructive marker remediation.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: story-21.2 dev (2026-07-04)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)"), 2026-09-01
archived: 2026-11-23

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
status: done 2026-11-24
resolution: The endpoint rejects undefined enum values and comma lists with INVALID_SOURCE_TYPE before case lookup or retrieval. Endpoint tests cover rejected inputs and canonicalized defined values. See spec-ten-deferred-search-response-reliability.md.

### DW-530: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
archived: 2026-11-23

### DW-538: Generic MCP tool failures direct operators to inspect server logs, but the current tool/executor path does not log the original exception.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md` summary: Generic MCP tool failures direct operators to inspect server logs, but the current tool/executor path does not log the original exception. evidence: `McpErrorMapper.MapGeneric` emits a sanitized suggestion to inspect MCP server logs, while the pre-existing tool catch blocks and the new shared executor map the exception without an `ILogger` emission; adding redacted source-generated diagnostics requires focused logging and telemetry ownership beyond this behavior-preserving refactor.
status: done 2026-11-24
resolution: The MCP executor records the tool name, exception type, and HRESULT through source-generated logging. Tests exercise sanitized validation and operation failures and the HTTP timeout mapping. See spec-ten-deferred-search-response-reliability.md.

### DW-539: The trust strip still renders the packet's confidence (evidence strength) and freshness for an unauthorized or unknown-isolation packet, even though the source-count badge is now fail-closed to "sources unavailable"; the residual confidence/freshness badges leak a coarse "strong evidence exists" inference past the authorization wall.

origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-7-evidence-cockpit-ux-conformance (2026-07-11)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md` summary: The trust strip still renders the packet's confidence (evidence strength) and freshness for an unauthorized or unknown-isolation packet, even though the source-count badge is now fail-closed to "sources unavailable"; the residual confidence/freshness badges leak a coarse "strong evidence exists" inference past the authorization wall. evidence: `MemoriesTrustStrip.razor` gates the source-count badge on `Packet.State == Unauthorized || IsRestrictiveScope(Packet.Scope.IsolationStatus)` but gates `ConfidenceLabel`, `FreshnessText`, and `TokenBudgetText` only on `ShowPacketValues` (Packet mode), so a restrictive packet carrying real `EvidenceStrength`/`Freshness` still shows "Confidence: Strong" and the actual freshness while the count is suppressed. The exposure is pre-existing — the trust strip has always rendered confidence/freshness in Packet mode — and was surfaced incidentally because Story 25.7 hardened only the source-count badge. The intent scopes fail-closed suppression to source/axis/graph detail and explicitly forbids introducing new trust semantics, so tightening the trust-summary badges is out of scope for this story and needs a focused fail-closed trust-surface decision.
status: open
decision: 2026-09-06 Suppress all metadata — Suppress all trust metadata for unauthorized callers.
decision: 2026-09-06 Suppress all metadata — Suppress all trust metadata for unauthorized callers.

### DW-540: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
archived: 2026-11-23

### DW-541: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)"), 2026-09-01
archived: 2026-11-23

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
decision: 2026-09-06 Add lifecycle guard — Add a shutdown-state guard and concurrency tests.
decision: 2026-09-06 Add lifecycle guard — Add a shutdown-state guard and concurrency tests.

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
archived: 2026-11-23

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
decision: 2026-09-06 Build idempotency — Add tenant-scoped idempotency and in-flight suppression.
decision: 2026-09-06 Build idempotency — Add tenant-scoped idempotency and in-flight suppression.

### DW-557: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of story-26.2 (2026-07-13)"), 2026-09-01
archived: 2026-11-23

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
status: done 2026-11-24
resolution: Historical publication requires authenticated HTTP 404 and the structured MANIFEST_UNKNOWN code before copying the exact manifest tag. Local TLS and publisher fixtures distinguish absence from outage, denial and malformed responses. Reopen if an uncertain read can authorize copying. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-568: [MEDIUM] Add representative boundary coverage for the documented 512 MiB / approximately 100K-unit restore contract.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/26-2-backup-and-restore.md
reason: - source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md` summary: [MEDIUM] Add representative boundary coverage for the documented 512 MiB / approximately 100K-unit restore contract. evidence: The current fidelity integration restores three small units and `RedisChunkReadStreamTests` reads five mocked bytes; neither observes the endpoint ceiling, real multi-chunk staging, retention renewal, or high-cardinality workflow paging. Re-open trigger: restore-size/staging hardening or the first supported large-tenant recovery rehearsal.
status: open

### DW-569: Distinguish "manifest unknown" from transient errors in remote registry inspect so a transient failure cannot bypass the digest-conflict fail-closed guard before push.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md` summary: Distinguish "manifest unknown" from transient errors in remote registry inspect so a transient failure cannot bypass the digest-conflict fail-closed guard before push. evidence: publish-containers.ps1 treats any nonzero skopeo/docker remote inspect as "tag absent" and proceeds to push; behavior predates this story and was preserved as explicit spec parity, flagged by two independent review layers.
status: done 2026-11-24
resolution: The historical immutable-tag guard rejects ambiguous reads and malformed digest objects, and retains matching-tag skip and conflicting-tag refusal. PublishContainersTests covers absent, matching, conflicting and uncertain tags. Reopen if a failed inspect can bypass the digest check and authorize a copy. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-570: Probe skopeo availability in release-preflight so a missing runner binary fails before NuGet publish and tag creation instead of causing a partial release.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md` summary: Probe skopeo availability in release-preflight so a missing runner binary fails before NuGet publish and tag creation instead of causing a partial release. evidence: Container publish now hard-depends on the runner-preinstalled skopeo; the publish-time tooling-missing check fires only after NuGet packages and the release tag exist. The story spec froze "no touching preflight", so this hardening was out of scope.
status: done 2026-11-24
resolution: The current release step requires Skopeo availability and version output before destination preflight, NuGet OIDC and tag creation. Executing the step with a failing Skopeo stopped at exit 42. Reopen if the first tooling check moves after a publication side effect. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-571: Make independent-process benchmark reproducibility a permanent fail-closed CI comparison.

origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md` summary: Make independent-process benchmark reproducibility a permanent fail-closed CI comparison. evidence: Story 26.8 retained and compared two independent benchmark processes, but the nightly lane launches one process and its in-process reproducibility check cannot detect process-initialized drift. A future lane should normalize and compare two independently generated result payloads without weakening the existing gate.
status: open

### DW-572: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
archived: 2026-11-23

### DW-573: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
archived: 2026-11-23

### DW-574: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 26-2-backup-and-restore (2026-07-15)"), 2026-09-01
archived: 2026-11-23

### DW-578: Reconcile raw privacy-sensitive state on the preserved JSON-console and optional OTLP routes with the bounded lifecycle target.

origin: migrated from legacy ledger ("Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)"), 2026-09-01
location: _bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md
reason: - source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` summary: Reconcile raw privacy-sensitive state on the preserved JSON-console and optional OTLP routes with the bounded lifecycle target. evidence: Search and source-URI events can already expose raw query, subject, or source URI values through the existing logging routes. Story 27.1 documents that pre-existing deviation and sanitizes only the accepted Dapr lifecycle path; a later scope decision must choose sanitization before provider fan-out or explicit category exclusion from durable external routes.
status: open
decision: 2026-09-01 Sanitize before fan-out — Apply the lifecycle sanitizer before every provider and add privacy-negative tests.
decision: 2026-09-01 Sanitize before fan-out — Apply the lifecycle sanitizer before every provider and add privacy-negative tests.

### DW-579: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)"), 2026-09-01
archived: 2026-11-23

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
status: done 2026-11-23
resolution: The canonical project context records Aspire AppHost SDK and Aspire.Hosting.Testing 17.0.0 and identifies the Builds Dapr version property. See spec-ten-deferred-verification-references.md.

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

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-589: recordId charset is not validated before it is interpolated into the Dapr state key.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-590: Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-591: Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-592: Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-593: Clock NetworkPolicy egress to TCP/443 is unrestricted (no `to:`); tighten to real UTC-source CIDRs before enablement.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
location: deploy/kubernetes/base/access-telemetry-network-policy.yaml
reason: - Clock NetworkPolicy egress to TCP/443 is unrestricted (no `to:`); tighten to real UTC-source CIDRs before enablement. [deploy/kubernetes/base/access-telemetry-network-policy.yaml:99] - ID: 27.3-CR19 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3) - Target artifact: deploy/kubernetes/base/access-telemetry-network-policy.yaml - Re-open trigger: before Production lifecycle enablement; the clock egress must be restricted to the real UTC-source CIDRs once the three `.example.invalid` authorities are replaced. - Rationale: The clock NetworkPolicy allows egress on TCP/443 with no `to:` selector, so the trusted-time workload can reach any address on the internet. The real UTC-source CIDRs are not knowable while all three configured authorities are `.example.invalid` placeholders, so narrowing the rule now would encode a fiction. Owner: clock-authority owner. Consequence: an unrestricted egress path exists on a workload that is scaled to zero and fail-closed.
status: open
decision: 2026-09-01 Supply restricted authorities — Provide approved endpoints and CIDRs, restrict egress, and add manifest guards.
decision: 2026-09-01 Supply restricted authorities — Provide approved endpoints and CIDRs, restrict egress, and add manifest guards.

### DW-594: `maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.

status: done 2026-07-26
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
archived: 2026-11-23

### DW-595: Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.

status: done 2026-07-26
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
archived: 2026-11-23

### DW-599: 27.3-CR8: test-double state store ships in the product container assembly.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
location: src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs
reason: - **27.3-CR8 - test-double state store ships in the product container assembly.** - ID: 27.3-CR8 - Status: open - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3) - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs - Re-open trigger: when the Story 27.2-origin structural move to a shared test-support project is scheduled. - Rationale: The class documents itself as a test double the runtime host does not register, yet it ships inside a project with `EnableContainer=true` and `ContainerRepository memories-access-telemetry`, reachable only through `InternalsVisibleTo`. A DI misregistration would satisfy `IAccessTelemetryStateStore` with no durability. Moving it to a shared test-support project is a Story 27.2-origin structural change. Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released image's assembly surface. Partially mitigated 2026-07-26 by dev-story: the adapter now validates `ttlInSeconds`, models the anti-resurrection conflict, prunes drained expiry minutes, and performs the same strong post-delete verification as the Dapr adapter, so a misregistration no longer silently discards expiry - but the structural placement is unchanged. `src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs` documents itself as a deterministic adapter for lifecycle tests that the runtime host does not register, yet it lives in a project with `EnableContainer=true` and `ContainerRepository` `memories-access-telemetry`, reachable only via `InternalsVisibleTo` from the two test assemblies. It is non-durable, non-transactional, and discards `ttlInSeconds`, so a DI misregistration would satisfy `IAccessTelemetryStateStore` with no durability and no expiry. Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released image's assembly surface. Reopen trigger: move it to a shared test-support project, or add a guard asserting the runtime composition root never registers it. Pre-existing: introduced by Story 27.2, not by this chunk.
status: open

### DW-600: 27.3-CR9: commit `358bef35` bypasses the Conventional Commits contract. Its subject carries

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)"), 2026-09-01
archived: 2026-11-23

### DW-601: The readiness gate does not require completion dates for evidence rows at review or done.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate does not require completion dates for evidence rows at review or done. evidence: `check_evidence_rows` reads only the status-column index, while the phase-ledger policy requires completed evidence to carry a date and forbids dateless rows at done.
status: resolved
resolution: 2026-10-28 — spec-five-deferred-evidence-readiness.md. C6 now requires a real ISO calendar completion date in the completion-date column or inline review-status text for completed states; done also dates accountable blocked dispositions. Missing columns, placeholders, impossible dates, timestamps and leap days have executable controls. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py" — 104/104 methods passed (17.113s, exit 0; baseline 81, delta +23). Reopen if the corresponding refusal or entry-point contract regresses.

### DW-602: The readiness gate accepts a bare blocked evidence status without accountability metadata.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate accepts a bare blocked evidence status without accountability metadata. evidence: Any non-pending value bypasses C6, so `blocked` passes without the policy-required owner, consequence, and reopen trigger.
status: resolved
resolution: 2026-10-28 — spec-five-deferred-evidence-readiness.md. C6 now requires non-placeholder owner, consequence and reopen-trigger fields for blocked dispositions, with an Owner/Accountable owner column fallback. Missing, duplicate, template and code-span-supplied fields fail; cell boundaries cannot disguise placeholders. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py" — 104/104 methods passed (17.113s, exit 0; baseline 81, delta +23). Reopen if the corresponding refusal or entry-point contract regresses.

### DW-603: The readiness gate accepts unknown evidence statuses as completed.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate accepts unknown evidence statuses as completed. evidence: C6 rejects only values in `PENDING_CELLS`, so a typo or invented state such as `gibberish` passes without a recognized completed-state vocabulary.
status: resolved
resolution: 2026-10-28 — spec-five-deferred-evidence-readiness.md. C6 now recognizes explicit completed and blocked state vocabularies at the start of the status cell. Unknown, misspelled, near-match and qualified-pending states fail closed; existing completed-state formatting and below-review behavior have passing controls. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py" — 104/104 methods passed (17.113s, exit 0; baseline 81, delta +23). Reopen if the corresponding refusal or entry-point contract regresses.

### DW-604: The readiness gate silently ignores evidence rows whose review-status cell is absent.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate silently ignores evidence rows whose review-status cell is absent. evidence: `check_evidence_rows` continues when `status_index` exceeds the row length instead of reporting the missing status required by policy.
status: resolved 2026-10-22
resolution: `spec-five-deferred-readiness-checks.md` implements the existing story-phase-ledger contract in `tools/check-story-review-readiness.py`; `test_missing_status_cell_fails_at_review_and_done` covers the refusal and fails against baseline `779db8694087c4459174429287ee07c1caff1a84`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 62/62 methods after independent review patches.
reopen_trigger: A change to readiness parsing or reconciliation lets the covered invalid artifact pass again.

### DW-605: Readiness File List reconciliation checks changed-but-unlisted paths but not listed-but-unchanged paths.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness File List reconciliation checks changed-but-unlisted paths but not listed-but-unchanged paths. evidence: C1 builds only an `unlisted` set even though the phase-ledger policy requires the cumulative File List and changed set to contain identical entries.
status: resolved 2026-10-22
resolution: `spec-five-deferred-readiness-checks.md` implements the existing story-phase-ledger contract in `tools/check-story-review-readiness.py`; `test_listed_but_unchanged_fails` covers the refusal and fails against baseline `779db8694087c4459174429287ee07c1caff1a84`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 62/62 methods after independent review patches.
reopen_trigger: A change to readiness parsing or reconciliation lets the covered invalid artifact pass again.

### DW-606: Readiness File List entries are treated as globs instead of exact historical paths.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness File List entries are treated as globs instead of exact historical paths. evidence: C1 calls `matches_glob` for File List entries, allowing a broad entry such as `src/**` to replace the policy-required path-level inventory.
status: resolved 2026-10-22
resolution: `spec-five-deferred-readiness-checks.md` implements the existing story-phase-ledger contract in `tools/check-story-review-readiness.py`; `test_wildcards_cannot_replace_exact_paths` covers the refusal and fails against baseline `779db8694087c4459174429287ee07c1caff1a84`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 62/62 methods after independent review patches.
reopen_trigger: A change to readiness parsing or reconciliation lets the covered invalid artifact pass again.

### DW-607: Cumulative readiness diff collection loses the source path of renames.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Cumulative readiness diff collection loses the source path of renames. evidence: `derive_cumulative_changed` uses `git diff --name-only`, while policy requires a rename entry to identify both old and new paths.
status: resolved
resolution: 2026-10-28 — spec-five-deferred-evidence-readiness.md. Checker staged/cumulative diffs, commit-msg input and all CI prepared ranges use --no-renames to preserve source and destination as separate exact paths. Executed disposable-repository CLI, hook and CI PR/push cases prove missing rename endpoints fail C1. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py" — 104/104 methods passed (17.113s, exit 0; baseline 81, delta +23). Reopen if the corresponding refusal or entry-point contract regresses.

### DW-608: A review or done artifact can pass C2 without a create-story ledger row.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A review or done artifact can pass C2 without a create-story ledger row. evidence: `check_ledger` requires `dev-story` for review/done and `code-review` for done but never requires the creation baseline mandated by policy.
status: resolved 2026-10-23
resolution: `spec-five-deferred-ledger-checks.md` implements the existing phase-ledger contract in `tools/check-story-review-readiness.py`; `test_review_and_done_without_create_story_row_fail` covers the refusal and fails against unchanged baseline `66c3c72b3814f2438603d2530a9c94d9b3110305`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 81/81 methods after independent review patches. Positive controls retain repeated development/review/course-correction work, complete blocked records and existing no-ops.
reopen_trigger: A readiness parser or lifecycle-validation change lets the covered invalid ledger or governed artifact pass again.

### DW-609: The newest reconciliation cell accepts unstructured blocker substrings.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The newest reconciliation cell accepts unstructured blocker substrings. evidence: C2 accepts any cell containing markers such as `not run` or `blocker` without validating the required command, owner, consequence, and reopen trigger.
status: resolved 2026-10-23
resolution: `spec-five-deferred-ledger-checks.md` implements the existing phase-ledger contract in `tools/check-story-review-readiness.py`; `test_blocked_record_requires_each_nonplaceholder_accountability_field` covers the refusal and fails against unchanged baseline `66c3c72b3814f2438603d2530a9c94d9b3110305`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 81/81 methods after independent review patches. Positive controls retain repeated development/review/course-correction work, complete blocked records and existing no-ops.
reopen_trigger: A readiness parser or lifecycle-validation change lets the covered invalid ledger or governed artifact pass again.

### DW-610: A review artifact can carry governed File List or evidence data but no phase ledger and still pass.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A review artifact can carry governed File List or evidence data but no phase ledger and still pass. evidence: When `find_ledger` returns none, validation emits a skipped note instead of enforcing the ledger required at review or done.
status: resolved 2026-10-23
resolution: `spec-five-deferred-ledger-checks.md` implements the existing phase-ledger contract in `tools/check-story-review-readiness.py`; `test_governed_review_and_done_require_ledger` covers the refusal and fails against unchanged baseline `66c3c72b3814f2438603d2530a9c94d9b3110305`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 81/81 methods after independent review patches. Positive controls retain repeated development/review/course-correction work, complete blocked records and existing no-ops.
reopen_trigger: A readiness parser or lifecycle-validation change lets the covered invalid ledger or governed artifact pass again.

### DW-611: An in-progress or review artifact can omit its File List and still pass readiness.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: An in-progress or review artifact can omit its File List and still pass readiness. evidence: When `parse_section_paths` returns none, validation emits a skipped note instead of enforcing the core cumulative completeness input.
status: resolved 2026-10-22
resolution: `spec-five-deferred-readiness-checks.md` implements the existing story-phase-ledger contract in `tools/check-story-review-readiness.py`; `test_missing_file_list_fails_for_active_governed_artifacts` covers the refusal and fails against baseline `779db8694087c4459174429287ee07c1caff1a84`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 62/62 methods after independent review patches.
reopen_trigger: A change to readiness parsing or reconciliation lets the covered invalid artifact pass again.

### DW-612: The readiness gate does not enforce chronological order of canonical ledger phases.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: The readiness gate does not enforce chronological order of canonical ledger phases. evidence: `check_ledger` records phase presence but never compares canonical phase indices, so an impossible lifecycle order can pass.
status: resolved 2026-10-23
resolution: `spec-five-deferred-ledger-checks.md` implements the existing phase-ledger contract in `tools/check-story-review-readiness.py`; `test_out_of_order_canonical_phases_fail` covers the refusal and fails against unchanged baseline `66c3c72b3814f2438603d2530a9c94d9b3110305`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 81/81 methods after independent review patches. Positive controls retain repeated development/review/course-correction work, complete blocked records and existing no-ops.
reopen_trigger: A readiness parser or lifecycle-validation change lets the covered invalid ledger or governed artifact pass again.

### DW-613: Canonical ledger rows can retain placeholder Date or Change cells.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Canonical ledger rows can retain placeholder Date or Change cells. evidence: C2 checks placeholders only in Test count and File List reconciliation despite the canonical five-column record requiring Date and Change too.
status: resolved 2026-10-23
resolution: `spec-five-deferred-ledger-checks.md` implements the existing phase-ledger contract in `tools/check-story-review-readiness.py`; `test_date_and_change_placeholders_fail_on_every_canonical_phase` covers the refusal and fails against unchanged baseline `66c3c72b3814f2438603d2530a9c94d9b3110305`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 81/81 methods after independent review patches. Positive controls retain repeated development/review/course-correction work, complete blocked records and existing no-ops.
reopen_trigger: A readiness parser or lifecycle-validation change lets the covered invalid ledger or governed artifact pass again.

### DW-614: A whitespace-only exclusion owner is accepted by the readiness parser.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: A whitespace-only exclusion owner is accepted by the readiness parser. evidence: `parse_exclusions` checks only that the owner regex matched and strips the captured value without rejecting an empty result.
status: resolved 2026-10-22
resolution: `spec-five-deferred-readiness-checks.md` implements the existing story-phase-ledger contract in `tools/check-story-review-readiness.py`; `test_blank_exclusion_owner_fails` covers the refusal and fails against baseline `779db8694087c4459174429287ee07c1caff1a84`. Focused discovery `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes 62/62 methods after independent review patches.
reopen_trigger: A change to readiness parsing or reconciliation lets the covered invalid artifact pass again.

### DW-615: Readiness hook and CI adoption lack an executable entry-point contract test.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md` summary: Readiness hook and CI adoption lack an executable entry-point contract test. evidence: Existing tests exercise the CLI and policy prose but do not pin the hook's `--derive-cumulative` invocation or CI's prepared changed-file invocation.
status: resolved
resolution: 2026-10-28 — spec-five-deferred-evidence-readiness.md. New readiness_entrypoints_test.py executes the actual commit-msg hook and extracted CI collection/readiness commands in disposable repositories. Passing inventories and refusal propagation prove hook cumulative derivation, CI prepared-range use, rename source retention and temporary-input cleanup. Unrelated hook gates and commitlint are isolated stand-ins; this does not claim their validation. Verification: PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p "*_test.py" — 104/104 methods passed (17.113s, exit 0; baseline 81, delta +23). Reopen if the corresponding refusal or entry-point contract regresses.

### DW-616: The local commitlint hook fails open when repository dependencies are absent.

status: done 2026-10-28
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-617: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy.

status: done 2026-10-28
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-618: Main-push commitlint runs can cancel an earlier range before it is validated.

status: done 2026-10-28
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-619: Contributor guidance still recommends the forbidden chore branch and commit type.

status: done 2026-09-05
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-620: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification.

status: done 2026-10-28
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-621: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-622: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-623: Independent tenant-detail and member read fault containment lacks page-level verification.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-624: The Tenants member paging state machine has only happy-path coverage.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-625: Failed refresh-subscription retry and duplicate-subscription protection lack verification.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-626: The Tenants command-side URI scheme gate lacks non-HTTP composition tests.

status: done 2026-09-01
origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
archived: 2026-11-23

### DW-627: Contributor coverage guidance still describes six projects although the authoritative Docker-free inventory contains seven.

origin: migrated from legacy ledger ("DW 27.3-CR24 - the deferred-work status verifier is wired to nothing"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md` summary: Contributor coverage guidance still describes six projects although the authoritative Docker-free inventory contains seven. evidence: The mismatch predates this fix; `tests/README.md` and `CONTRIBUTING.md` omit AccessTelemetry while `tools/test-projects.unit-contract.txt` and `requiredReportProjects` include it.
status: done 2026-11-23
resolution: The contributor guide, coverage inventory and required report projects agree on nine projects, including AccessTelemetry. The test README links to both machine-readable sources. See spec-ten-deferred-verification-references.md.

### DW-628: Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
archived: 2026-11-23

### DW-629: `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
archived: 2026-11-23

### DW-634: Refresh historical governance docs that still cite `_bmad/custom/bmad-generate-project-context.toml` after the skill rename.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-30990821240-bmad-customization-fixtures.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-30990821240-bmad-customization-fixtures.md` summary: Refresh historical governance docs that still cite `_bmad/custom/bmad-generate-project-context.toml` after the skill rename. evidence: Cross-tenant carry-forward and related process notes still document the deleted generate custom path and generate-skill verification commands; this fixture fix deleted that path without updating those historical references.
status: done 2026-11-23
resolution: The carry-forward spec links the current bmad-project-context override and gives rerun commands for both agent surfaces. See spec-ten-deferred-verification-references.md.

### DW-635: Historical container rebuilds are not bit-identical, so re-running Recover Partial Release after a successful image push hits immutable digest conflicts with no release-only workflow path.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: Historical container rebuilds are not bit-identical, so re-running Recover Partial Release after a successful image push hits immutable digest conflicts with no release-only workflow path. evidence: 2.6.5 first push succeeded then evidence failed; second run conflicted on config digest. 2.6.0-2.6.4 Releases had to be completed offline from already-present remote tags.
status: open

### DW-636: Workflow 2-or-4 Server/MCP evidence gates are only source-text pinned in CiTestInventoryTests, not executed as PowerShell.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: Workflow 2-or-4 Server/MCP evidence gates are only source-text pinned in CiTestInventoryTests, not executed as PowerShell. evidence: Verification-gap review showed deleting the hasServer/hasMcp throw while leaving pinned substrings would keep CiTestInventoryTests green.
status: done 2026-11-24
resolution: The historical inventory fixture executes the staged PowerShell publisher, asserts the GITHUB_OUTPUT selection for two and four images, and rejects two-row sets missing Server or MCP before any command or output. Reopen if the active recovery gate loses executable negative coverage. Evidence: `_bmad-output/implementation-artifacts/spec-ten-deferred-release-safeguards.md`.

### DW-637: PARTIAL PUBLISH incidents for 2.6.0-2.6.7 were already closed before evidence-backed recovery completed.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md` summary: PARTIAL PUBLISH incidents for 2.6.0-2.6.7 were already closed before evidence-backed recovery completed. evidence: Issues #22-#33 show closedAt before the 2026-08-08 recovery; complete-partial-release only closes open issues.
status: open

### DW-638: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
archived: 2026-11-23

### DW-639: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement.

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
archived: 2026-11-23

### DW-640: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
archived: 2026-11-23

### DW-641: Add a retrospective addendum or reopen note when epic-24 returns to in-progress after epic-24-retrospective is done for Stories 24.6-24.9.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Add a retrospective addendum or reopen note when epic-24 returns to in-progress after epic-24-retrospective is done for Stories 24.6-24.9. evidence: sprint-status.yaml reopens epic-24 while leaving epic-24-retrospective done with no addendum covering the residual backlog registration.
status: done 2026-11-23
resolution: A dated Epic 24 retrospective addendum documents the residual backlog registration and current sprint state. See spec-ten-deferred-verification-references.md.
decision: 2026-09-01 Append dated addendum — Add a dated correction with links to the governing artifacts and preserve the original record.
decision: 2026-09-01 Append dated addendum — Add a dated correction with links to the governing artifacts and preserve the original record.

### DW-642: Align Story 24.6 accepted-blocker schema so “proof boundary” is required consistently across proposal and registered story/epics text.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Align Story 24.6 accepted-blocker schema so “proof boundary” is required consistently across proposal and registered story/epics text. evidence: Registered AC4 requires a proof boundary field while the matching proposal AC omits it.
status: done 2026-11-23
resolution: The 2026-08-04 proposal requires a proof boundary. Its V7 check validates the three criteria and the existing Story 24.6 blocker. See spec-ten-deferred-verification-references.md.
decision: 2026-09-01 Require proof boundary — Add the field to the proposal schema, migrate the entry, and guard it in validation.
decision: 2026-09-01 Require proof boundary — Add the field to the proposal schema, migrate the entry, and guard it in validation.

### DW-643: Name the concrete RedisEmbeddingMigrationStoreTests method required by Story 24.8 Cross-Tenant Negative Evidence.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md` summary: Name the concrete RedisEmbeddingMigrationStoreTests method required by Story 24.8 Cross-Tenant Negative Evidence. evidence: Planned Verification commands the migration store tests without a named assertion contract in the evidence table.
status: done 2026-11-23
resolution: Story 24.8 names and provides the exact runner command for StagingSemanticWrites_UseCanonicalMarkerlessShapesWithoutLifecycleMutation. See spec-ten-deferred-verification-references.md.
decision: 2026-09-01 Name exact method — Record the exact RedisEmbeddingMigrationStoreTests method and verify the command selects it.
decision: 2026-09-01 Name exact method — Record the exact RedisEmbeddingMigrationStoreTests method and verify the command selects it.

### DW-644: Add a reciprocal pointer from docs/operations/route-surface.md to the new directory-ingestion authoritative contract.

origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md` summary: Add a reciprocal pointer from docs/operations/route-surface.md to the new directory-ingestion authoritative contract. evidence: Directory guidance links to route-surface as the prior route home, but route-surface is outside this story File Scope and gained no back-link.
status: done 2026-11-23
resolution: The REST route contract links to Directory Ingestion Operations for the directory submission and status routes. See spec-ten-deferred-verification-references.md.

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
status: done 2026-11-24
resolution: The route contract links to the tenant runbook for one canonical graph proof procedure. The runbook guard verifies the link, proof identity, and commands. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-653: Collision fixtures remain in the shared real-backend topology.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Collision fixtures remain in the shared real-backend topology.** - ID: 24.6-CR-W2 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The proof seeds collision nodes, edges, and two provisioned tenants without teardown; unique tenant identifiers prevent cross-test identity conflicts, while shared-topology cleanup is a wider fixture-lifecycle concern. - Re-open trigger: A later test observes these records, integration storage growth becomes material, or the shared fixture adds a safe tenant teardown API.
status: open

### DW-654: The collision proof assumes newly provisioned graphs contain no relationships.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The collision proof assumes newly provisioned graphs contain no relationships.** - ID: 24.6-CR-W3 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: A pre-existing relationship would report a failed collision precondition rather than a dedicated fixture-precondition assertion; unique tenant provisioning makes that condition unlikely and diagnostic refinement is outside the closure. - Re-open trigger: The collision precondition fails on a non-empty newly provisioned graph or fixture provisioning begins seeding relationships.
status: done 2026-11-23
resolution: The collision and planted-marker integration fixtures now assert zero existing relationships before seeding each tenant graph. See spec-ten-deferred-isolation-verification.md.

### DW-655: Graph proof coverage is one bounded edge/origin/source/depth shape.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Graph proof coverage is one bounded edge/origin/source/depth shape.** - ID: 24.6-CR-W4 - Status: carried-forward - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: Story 24.6 proves the required `EdgeType.CausedBy`, `EdgeOrigin.Explicit`, `SourceType.File`, depth-one collision fixture; additional relationship variants and traversal depths are useful expansion coverage but not part of the accepted NFR8 slice. - Re-open trigger: A new edge type, origin, source type, or deeper traversal path changes tenant-routing behavior or a leakage defect appears outside the proven fixture.
status: open

### DW-656: The verifier class-level contract had been narrowed beyond Story 24.6.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
archived: 2026-11-23

### DW-657: Non-passing graph-isolation branches do not repeat the structural-only label.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
archived: 2026-11-23

### DW-658: The HTTP-visible graph detail has no explicit length or format contract.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
archived: 2026-11-23

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
status: done 2026-11-23
resolution: The live collision test checks traversal arrays directly on the JSON wire and verifies omission of the four healthy default members. See spec-ten-deferred-isolation-verification.md.

### DW-662: Verifier unit mocks no longer rely on unconfigured graph-query defaults.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)"), 2026-09-01
archived: 2026-11-23

### DW-663: Verifier source guard still lives in a runbook test class.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs
reason: - **Verifier source guard still lives in a runbook test class.** - ID: 24.6-F5-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs - Rationale: The first-pass patch read "strengthen **and relocate** the source-text guard". N5 strengthened it on 2026-08-13, but the assertion about `TenantIsolationVerifier.cs` source still sits in the runbook/deployment doc-contract class, so a verifier regression is reported by a test named for runbooks. Relocation is cosmetic to behaviour and was not attempted while the guard is green. - Re-open trigger: The guard fails and the failure is misattributed to runbook content, or another verifier source assertion is added to the same class.
status: done 2026-11-23
resolution: The partial-class graph command source guard now runs in TenantIsolationVerifierTests. The runbook test retains the documentation checks. See spec-ten-deferred-isolation-verification.md.

### DW-664: `ReconnectPrimaryDaprClients` disposes before installing replacements.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-665: Reconnect fires only when the sidecar endpoint changes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **Reconnect fires only when the sidecar endpoint changes.** - ID: 24.6-F5-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The guard is `if (currentDaprEndpoint != DaprSidecarHttpEndpoint)`, so a sidecar that restarts on the same port skips the reconnect entirely and the fixture keeps pooled connections to the killed process. The correct trigger condition (endpoint change **or** process restart) needs a restart signal the fixture does not currently expose. - Re-open trigger: The restart regression flakes with a connection error on an unchanged port, or a process-restart signal becomes available.
status: open

### DW-666: Traversal assertions dereference without null guards.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Traversal assertions dereference without null guards.** - ID: 24.6-F5-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: `AssertTraversalIsFixtureLocal` dereferences `Nodes`, per-node `Edges`, and `GapMarkers` without null checks, so a response that omits or nulls one of them raises a `NullReferenceException` instead of an assertion naming the field that lost its marker. Diagnostic quality only; the assertions still fail closed. - Re-open trigger: A traversal failure is reported as a `NullReferenceException`, or the traversal contract makes any of those members nullable.
status: done 2026-11-23
resolution: Traversal assertions identify missing or null Nodes, per-node Edges and GapMarkers by field. See spec-ten-deferred-isolation-verification.md.

### DW-667: No cancellation coverage on `VerifyAsync`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
reason: - **No cancellation coverage on `VerifyAsync`.** - ID: 24.6-F5-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs - Rationale: No test invokes `VerifyAsync` with an already-cancelled token, so the graph check's cancellation behaviour is unpinned and a regression would pass unnoticed. Outside AC3's structural-only scope. - Re-open trigger: Cancellation handling in `TenantIsolationVerifier` is changed, or a cancellation defect is observed in the verify endpoint.
status: done 2026-11-23
resolution: The existing VerifyAsync_AlreadyCancelled_DoesNotDispatchBackendReads regression passed in the focused verifier run. See spec-ten-deferred-isolation-verification.md.

### DW-668: No `/traverse` denial rows for invalid or out-of-range `depth`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs
reason: - **No `/traverse` denial rows for invalid or out-of-range `depth`.** - ID: 24.6-F5-W6 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs - Rationale: The Story 20.2 denial-before-dependency rows cover missing and blank `startNodeId` but not a malformed or out-of-range `depth`, where a 400 could pre-empt the 403 and reveal that the handler was reached. Beyond AC1's stated boundary. - Re-open trigger: `depth` validation moves relative to the tenant authorization filter, or a new query parameter is added to the traverse route.
status: done 2026-11-24
resolution: The endpoint authorization theory covers malformed depth and both bounds. All cases return TENANT_FORBIDDEN before backend reads. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-669: Restart regression dereferences an actor config without a null guard.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs
reason: - **Restart regression dereferences an actor config without a null guard.** - ID: 24.6-F5-W7 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs - Rationale: The post-rotation actor call dereferences its result without a null guard and silently depends on the `OpenBaoRecoveryTenantId` tenant carrying a seeded embedding configuration, so a fixture-data gap surfaces as a `NullReferenceException` rather than a named assertion. - Re-open trigger: The regression fails with a `NullReferenceException`, or `OpenBaoRecoveryTenantId` seeding changes.
status: done 2026-11-23
resolution: The live OpenBao restart test checks the recovered actor configuration before accessing Dimensions and names the recovery tenant on failure. See spec-ten-deferred-isolation-verification.md.

### DW-670: The HTTP-visible AC3 citation is hard-coded where the unit guards are manifest-bound.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The HTTP-visible AC3 citation is hard-coded where the unit guards are manifest-bound.** - ID: 24.6-F6-W1 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: F5-P8 bound the operator proof citation to `tools/integration-fast-required-surfaces.txt` through `OperationalRunbookSetTests.GraphContentProofCitation`, and both Server.Tests guards now derive it. The real-backend assertion cannot reach that `internal` member from a different assembly, so it keeps the literal `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`. The binding is fail-closed — a manifest-driven rename reds the unit lane — so this is maintenance duplication in three places, not an escape hatch, and the fix would mean duplicating the manifest reader into the integration assembly. - Re-open trigger: The graph-content proof method is renamed or re-keyed in the manifest, or a shared test-support assembly becomes available to both test projects.
status: done 2026-11-23
resolution: The runbook, verifier and HTTP tests derive one exact proof identity from the graph-content-isolation inventory row. The HTTP test checks the compiled identity. See spec-ten-deferred-isolation-verification.md.

### DW-671: The restored verifier class-level summary has no test pinning either wording.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **The restored verifier class-level summary has no test pinning either wording.** - ID: 24.6-F6-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: `24.6-CR-W5` was closed on the strength of fifth-pass Decision F5-D2 restoring the broad architectural-isolation XML summary and keeping the structural-only hedge local to `CheckGraphIsolationAsync`. The original finding's second half — "no test pins either wording" — was not addressed, so the entry's own re-open trigger ("a future change again applies the graph-specific structural-only limitation to the verifier's class-wide responsibilities") has no detector and would have to be caught by review. The reverted wording is net-zero against baseline `0ecdffed`, so nothing regressed; only the guard is missing. - Re-open trigger: The class-level summary is edited again in either direction, or a Story 24.7-24.9 slice re-scopes the verifier's Redis marker responsibilities.
status: done 2026-11-23
resolution: The executed verifier source guard pins the class-wide architectural summary and the graph method's narrower structural scope. See spec-ten-deferred-isolation-verification.md.

### DW-672: The graph-check lookup throws an undiagnosable exception when the check is absent.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The graph-check lookup throws an undiagnosable exception when the check is absent.** - ID: 24.6-F6-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The new HTTP-observable AC3 block resolves the check with `result.Checks.Single(check => check.CheckName == "GraphIsolation")`. If the verifier stops emitting `GraphIsolation`, or emits it twice, the test fails with a bare `InvalidOperationException` naming neither the check nor the contract, instead of a Shouldly assertion. The surrounding `AssertCoreIsolationChecksPassed(result)` already fails closed on a missing check, so the diagnosis cost is the only impact. - Re-open trigger: The test fails with `InvalidOperationException`, or the verifier begins emitting per-backend `GraphIsolation` results.
status: done 2026-11-23
resolution: HTTP verifier tests use a named uniqueness assertion for core checks and the checks in the planted-data controls. See spec-ten-deferred-isolation-verification.md.

### DW-673: Node-marker assertions lack a mutation-sensitivity control.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **Node-marker assertions lack a mutation-sensitivity control.** - ID: 24.6-F6-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: The positive real-backend fixture proves tenant-local node and edge markers, while the planted-marker mutation control exercises only the edge-marker assertion. The Administrator ratified C1's completed boundary as edge-only for mutation sensitivity; a node-marker control remains useful hardening but is outside that boundary. - Re-open trigger: A node-marker assertion is weakened or removed, a foreign-node regression appears, or Story 24.6's ratified C1 mutation-sensitivity boundary is reopened.
status: done 2026-11-24
resolution: The live collision proof swaps each node marker field independently with the corresponding foreign node value and verifies that its locality assertion rejects the swap. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-674: Graph-isolation verification does not convert a FalkorDB `RedisTimeoutException` into a failed backend check.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Graph-isolation verification does not convert a FalkorDB `RedisTimeoutException` into a failed backend check.** - ID: 24.6-F6-W5 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Rationale: `CheckGraphIsolationAsync` catches `RedisConnectionException` and `RedisServerException` but not `RedisTimeoutException`, so a timeout can escape the verifier instead of preserving the existing graceful backend-unavailable result shape. - Re-open trigger: A FalkorDB timeout escapes `VerifyAsync` as an unhandled exception, or a later story converts `RedisTimeoutException` into a failed `GraphIsolation` check.
status: done 2026-11-23
resolution: The existing VerifyAsync_CommandTimeouts_ReportEveryAffectedCheckWithoutDriverDetails theory passed, including the FalkorDB timeout row. See spec-ten-deferred-isolation-verification.md.

### DW-675: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-676: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-677: Direct origin/main push for authorized /pushall envelopes still trips GitHub branch-protection (PR required, expected status checks).

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md` summary: Direct origin/main push for authorized /pushall envelopes still trips GitHub branch-protection (PR required, expected status checks). evidence: Push `3e92ca36..8d47a46a` succeeded while GitHub reported a branch-protection bypass. This envelope's remaining task is to push the superproject, matching prior /pushall specs; the protection warning is a standing process tension, not a defect unique to this snapshot.
status: open

### DW-678: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-679: Restore/build still surfaces NU1903 for SSH.NET 2025.1.0.

origin: migrated from legacy ledger ("Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md` summary: Restore/build still surfaces NU1903 for SSH.NET 2025.1.0. evidence: Pre-existing advisory warning observed during the Release package-mode verification of this dependency refresh; unrelated to the submodule gitlink bumps.
status: open

### DW-680: TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-681: Make the concrete tenant embedding configuration provider stop its actor read when caller cancellation wins.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
location: src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs
reason: - **Make the concrete tenant embedding configuration provider stop its actor read when caller cancellation wins.** - ID: 24.7-PROVIDER-CANCELLATION-IGNORED - Status: carried-forward - Source story: 24-7-tenant-configured-vector-dimension-verification - Target artifact: src/Hexalith.Memories.Server/Ingestion/TenantEmbeddingConfigProvider.cs - Re-open trigger: A provider-focused change to `TenantEmbeddingConfigProvider`, or evidence that a cancelled verification populates the cache with a result the caller never observed. - Rationale: `TenantEmbeddingConfigProvider.GetAsync` accepts a cancellation token but awaits `GetEmbeddingConfigAsync()` without applying it, so `TenantIsolationVerifier` can stop waiting through `WaitAsync` while the actor call continues and may populate the cache after the verification request is cancelled. Pre-existing behavior outside Story 24.7's mapped file scope.
status: open

### DW-682: Bound semantic-isolation mismatch evidence returned for large tenant key sets.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)"), 2026-09-01
archived: 2026-11-23

### DW-683: `GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-684: The allocation-failure cleanup path in `ReconnectPrimaryDaprClients` has no test in any lane.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
reason: - **The allocation-failure cleanup path in `ReconnectPrimaryDaprClients` has no test in any lane.** - ID: 24.6-F8-W2 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs - Rationale: The seventh pass added a `try`/`catch` that disposes the partially constructed `HttpClientHandler` and state client on construction failure and rethrows. The ledger records that phase as `+0 test cases / +0 test methods`, and the path is not reachable from any existing test because it requires a client construction failure mid-rotation. Forcing it would mean injecting a fault into fixture startup, which the current fixture design does not expose. - Re-open trigger: The fixture gains a fault-injection seam, or a rotation failure is observed in CI leaving a leaked handler.
status: open

### DW-685: The planted-marker negative control leaves its mutation in a provisioned tenant graph and is now a required CI surface.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The planted-marker negative control leaves its mutation in a provisioned tenant graph and is now a required CI surface.** - ID: 24.6-F8-W3 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` writes a foreign edge marker into tenant A's real graph with no teardown, and this range pinned it as a required `integration-fast` method surface. Each run provisions fresh GUID-suffixed tenants, so the corruption does not cross runs; the residual concern is that pinning a deliberately data-corrupting method into the required lane widens exactly the fixture-cleanup risk `24.6-CR-W2` already describes, without either entry recording the change. - Re-open trigger: A test outside this method observes a foreign marker on a shared fixture tenant, or the fixture moves to reused rather than per-run tenant identifiers.
status: done 2026-11-24
resolution: The planted-edge control restores the fixture marker and previous-confidence state in finally and verifies the restored traversal and persisted state. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-686: The new assertions lack defensive guards that would name the failing field instead of throwing.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
reason: - **The new assertions lack defensive guards that would name the failing field instead of throwing.** - ID: 24.6-F8-W4 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs - Rationale: Six hardening gaps were identified across the new assertions: no null guards on `traversal.Nodes`, `traversal.GapMarkers`, or per-node `Edges`; no already-cancelled `CancellationToken` case for `VerifyAsync`; no empty or null `GRAPH.LIST` case pinning `ParseGraphList`; `Single()` rather than `ShouldHaveSingleItem` for the graph-check lookup; no linked cancellation token on the seed query, so a command abandoned by `WaitAsync` is not actually cancelled; and no null guard on the OpenBao recovery tenant's embedding configuration. Each degrades diagnosis quality on failure rather than weakening the proof — the assertions themselves fail closed — so they are hardening, not correctness defects. `24.6-F6-W3` already covers the `Single()` case alone. - Re-open trigger: Any of these sites fails with a bare `NullReferenceException` or `InvalidOperationException` in CI, or the seed query is observed duplicating relationships.
status: done 2026-11-24
resolution: The earlier null, lookup, and cancellation guards remain covered. New empty/null GRAPH.LIST cases fail closed, and collision seeding awaits each bounded graph mutation before honoring cancellation. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-687: Older Story 27.2 and `spec-gh-30838751196` still describe a standalone Access Telemetry AppHost for the routed Dapr proof.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-gh-31871199175-fix-ci-cd.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-31871199175-fix-ci-cd.md` summary: Older Story 27.2 and `spec-gh-30838751196` still describe a standalone Access Telemetry AppHost for the routed Dapr proof. evidence: This change joined that proof to `AspireIngestionPipeline`; those artifacts were outside file scope and still document a second AppHost.
status: done 2026-11-23
resolution: Story 27.2 and the historical CI spec direct current routed Dapr reruns to the shared AspireIngestionPipeline fixture. See spec-ten-deferred-verification-references.md.

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
status: done 2026-11-24
resolution: Rebuild missing discovery from surviving membership markers with batched reads. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-693: Handle failed TTL refresh writes for observed-event indexes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Handle failed TTL refresh writes for observed-event indexes. evidence: Written and discovery index refresh paths discard false `TrySaveStateAsync` results while returning success, so indexes can expire before the observation keys they enumerate.
status: done 2026-11-24
resolution: Retry and report rejected TTL writes for indexes and membership markers. See _bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md.

### DW-694: Coordinate observed-event writers with tenant deletion.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-publish-approved-module-baselines-2026-08-01.md` summary: Coordinate observed-event writers with tenant deletion. evidence: `DaprObservedEventTypeStore.DeleteAllTenantDataAsync` enumerates one snapshot and then deletes both indexes without a writer barrier, allowing concurrent observations to survive while becoming unindexed.
status: open

### DW-695: Bound tenant-isolation embedding-configuration lookup time independently of caller cancellation.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

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

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-700: Missing CancellationToken and unhandled WRONGTYPE in syntactic hash prefix scan.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-701: Inconsistent entry CancellationToken checks in TenantIsolationVerifier methods.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-702: Optimize ScanSemanticHashPrefixForTenantEvidenceAsync to single Redis command round-trip per key.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
reason: - **Optimize ScanSemanticHashPrefixForTenantEvidenceAsync to single Redis command round-trip per key.** - ID: 24.6-F8-W7 - Status: carried-forward - Source story: 24-6-graph-content-level-tenant-isolation-evidence - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs - Re-open trigger: Story 24.8 optimizes semantic hash prefix scanning performance. - Rationale: For every scanned key, `ScanSemanticHashPrefixForTenantEvidenceAsync` executes separate `HashGetAsync` and `HashExistsAsync` calls; retrieving `naturalLanguageDescription` in the discriminator batch avoids redundant round-trips. Pre-existing behavior owned by Story 24.8.
status: done 2026-11-24
resolution: One read-only Redis script on the primary reads the discriminator fields and reports description presence without returning its text. Tests cover exact calls, empty descriptions, cancellation, wrong value types, and duplicate discovery. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-703: Broaden IsEmbeddingConfigurationUnavailable exception filters in TenantIsolationVerifier.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-704: Support dual remediation when classification gap and dimension mismatch co-occur.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-705: Add replica endpoint filtering in GetConnectedServers for clustered Redis deployments.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-706: Tenants worktree HEAD `4a3eec38` is ahead of the staged gitlink `c5fa0082`.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-707: Isolate the server embedding-provider options test from parallel static options state.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)"), 2026-09-01
archived: 2026-11-23

### DW-708: Transient Redis exception during the second of two sequential semantic scans discards already-collected first-scan evidence.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-709: `Remediation` text still does not support dual guidance when a classification gap co-occurs with another problem.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-710: The two-round-trip discriminator read was not folded into one batched call.

origin: migrated from legacy ledger ("Deferred from: code review of spec-24-8-semantic-isolation-key-family-classification (2026-08-31)"), 2026-09-01
location: _bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-24-8-semantic-isolation-key-family-classification.md` summary: The two-round-trip discriminator read was not folded into one batched call. evidence: `ScanSemanticHashPrefixForTenantEvidenceAsync` still issues a separate `HashGetAsync` (5 fields) and `HashExistsAsync` (`naturalLanguageDescription`) per scanned key. This is the same unresolved gap as carried-forward ledger item `24.6-F8-W7`, which already named Story 24.8 as its natural closure point; this diff did not close it.
status: done 2026-11-24
resolution: Same underlying read as DW-702. HMGET and HEXISTS run inside one Redis script with no separate client request. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-711: `references/Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Program.cs` still calls `AddHexalithMemoriesSearchIndexServer` with the retired `string secretStoreComponentPath` positional argument and will fail to build once it references an updated `Hexalith.Memories.Aspire` package.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-712: `docs/operations/openbao.md`'s new Story 29.2 passage claims a standalone Dapr self-hosted host (not just Kubernetes) can supply the `openbao-runtime-bootstrap`/`openbao-access-telemetry-bootstrap` bootstrap Secrets that `deploy/dapr/components/secretstore.yaml` and `access-telemetry-secrets.yaml` reference via `secretKeyRef`, but this has not been verified against Dapr's actual self-hosted secret-store resolution behavior outside Kubernetes.

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-29-2-provider-neutral-aspire-composition-and-secret-verification (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-713: Task 4 full-stack proof needs an EventStore domain-service resource Story 28.1 is not scoped to add.

origin: migrated from legacy ledger ("Deferred from: spec-28-1-adopt-owner-approved-eventstore-runtime-identity dev (2026-09-01)"), 2026-09-01
location: _bmad-output/planning-artifacts/epics.md` (Epic 28, a new follow-up story)
reason: - **Task 4 full-stack proof needs an EventStore domain-service resource Story 28.1 is not scoped to add.** - ID: 28.1-TASK4-FULLSTACK-PROOF-NEEDS-DOMAIN-SERVICE - Status: accepted - Source story: spec-28-1-adopt-owner-approved-eventstore-runtime-identity - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Epic 28, a new follow-up story) - Rationale: Story 28.1's Task 3 correctly added exactly one `eventstore` gateway resource to Memories' AppHost (`src/Hexalith.Memories.AppHost/Program.cs`), per the spec's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary. EventStore's own full-stack proof pattern (`references/Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Fixtures/AspirePubSubProofTestFixture.cs` + `ContractTests/PubSubDeliveryProofTests.cs`) submits a command to the Gateway HTTP API and asserts a Dapr-published CloudEvent, but that pattern's domain logic (e.g. the `counter` sample domain) is compiled into a **separate** Aspire-composed domain-service resource (`Hexalith.EventStore.AppHost/Program.cs:119`, `AddProject<Projects.Hexalith_EventStore_Sample>("sample")`) that Memories' AppHost has no equivalent of. `SandboxCommandRequest.cs`'s own doc comment confirms any command requires "the domain service Handle method" to exist. Adding one would expand Story 28.1's topology beyond its approved scope — exactly the case `epics.md`'s Story 28.1 final Given/When/Then clause ("Given adoption exposes a behavioral incompatibility... fails closed and routes that behavior change to a separately approved compatibility story rather than expanding silently") anticipates, and consistent with spec-28-1's own "Never redesign ingestion/projection/deployment topology beyond identity adoption plus the one `eventstore` resource" boundary. (Note: this is not a numbered "AC7" in either document; an earlier pass of this entry miscited it as one.) - Candidate resolutions (neither attempted, both need a human/architecture decision, not a unilateral dev choice): (1) add a Memories-owned EventStore domain-service resource to the AppHost so a real EventStore-originating command can be submitted and its Dapr-published event traced into Memories — the direct analog of EventStore's own proof pattern; or (2) route Memories' own existing Tenant/Case domain commands (which already reach the live `eventstore` gateway) back through `hexalith-eventstore/*` topic naming into Memories' own ingestion — investigated and set aside this session because no existing tenant-routing config maps that back to Memories' ingestion, and wiring one up risks an unverified self-referential duplicate-indexing loop. - Resolution criteria (per the original 23.7 entry, unchanged): a real EventStore-originating publish reaches Memories through Dapr; the resulting memory is persisted and searchable through Redis and FalkorDB; duplicate replay is ignored; negative evidence proves no cross-tenant result leakage. - Re-open trigger: a follow-up story is selected to close this gap; or any story/review claims EventStore-to-Memories full-stack proof without meeting every resolution criterion above.
status: open
decision: 2026-09-01 Own a domain service — Add a Memories-owned EventStore domain-service resource and prove commands through it end to end.
decision: 2026-09-01 Own a domain service — Add a Memories-owned EventStore domain-service resource and prove commands through it end to end.

### DW-714: Classification-gap co-occurring with an active marker defect still suppresses marker-specific `Remediation` entirely.

status: done 2026-11-23
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-715: `CheckSyntacticIsolationAsync` still returns the anti-template blanket-deletion wording Story 24.9 removed from `SemanticIsolation`.

status: done 2026-11-22
origin: migrated from legacy ledger ("Deferred from: code review of spec-24-9-non-destructive-tenant-marker-diagnostics (2026-08-31)"), 2026-09-01
archived: 2026-11-23

### DW-716: Story 28.1 code review — CI provisioning-step duplication and unconfirmed AppHost resource start-ordering.

origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-01
location: .github/workflows/ci.yml; .github/workflows/nightly.yml; src/Hexalith.Memories.AppHost/Program.cs
reason: - source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md` summary: `tools/ci/provision-eventstore-local-feed.sh`'s ~9-line invocation is copy-pasted verbatim into 7 CI job definitions across `ci.yml`/`nightly.yml` instead of a shared composite action, and local dev follows a separate, independently-maintained manual procedure (recorded in the spec's Verification section) for producing the same artifact rather than invoking the same script. evidence: confirmed by direct inspection of both workflow files -- the identical step comment and `run:` line appear in `build`, `test-unit-contract`, `web-e2e-specimen`, `integration-fast` (ci.yml) and `integration-fast`, `integration-slow`, `benchmark` (nightly.yml). Real DRY/drift risk (a future SHA/version rotation needs 7+ synchronized edits plus the local dev docs), but not a correctness defect in the current diff, and refactoring into a composite action is a real engineering task, not a trivial patch. **Partially mitigated (2026-09-01 follow-up patch pass):** the two hardcoded literals (`fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` / `999.1.20-proof.fa2d1c9910f8`) that were duplicated across all 7 CI call sites are now centralized in each workflow file's top-level `env:` block (`EVENTSTORE_APPROVED_SHA` / `EVENTSTORE_APPROVED_VERSION`), reducing the literal-duplication surface from ~14 hardcoded values to 2 per workflow file. The *step itself* (name, comment, `run:` line referencing those env vars) is still repeated 7 times -- collapsing that into a shared composite action remains the open, real-engineering-task portion of this finding. -- source_spec: same. summary: No explicit Aspire `WaitFor` ordering is established between the `memories` resource (which reaches `eventstore` via Dapr service invocation) and the new `eventStoreGateway` resource, so it's unclear whether Memories Server could attempt to invoke the `eventstore` app-id before that resource/sidecar is ready. evidence: `src/Hexalith.Memories.AppHost/Program.cs`'s new `eventStoreGateway` only has `.WaitFor(redis)`; no `WaitFor(eventStoreGateway)` was added to the `memories`/`server` resource. Whether this is a real race depends on Dapr's own service-invocation retry/resiliency semantics, which needs an architect's judgment call rather than a mechanical fix -- do not guess at an ordering constraint without confirming Dapr's actual behavior here. - Re-open trigger: a follow-up story touches this CI wiring or the AppHost resource graph again, a flaky/racy `eventstore` invocation is observed at runtime, or (specific to the rebuild-and-self-sign CI provisioning workaround as a whole) EventStore's own team reseals Story 1.20's proof packet under Memories' mandated SDK `10.0.400` -- at that point `tools/ci/provision-eventstore-local-feed.sh`, `tools/nuget-local-feeds/`, and every CI step wired to them become retirable in favor of restoring the now-reproducible, originally-approved package hash directly, and this whole workaround (script, local-dev config, per-run signing) should be removed rather than kept running indefinitely alongside a now-available real fix.
status: open
decision: 2026-09-01 Prove retry behavior — Extract the action, regression-test Dapr retry behavior, document it, and retain no WaitFor only if safe.
decision: 2026-09-01 Prove retry behavior — Extract the action, regression-test Dapr retry behavior, document it, and retain no WaitFor only if safe.

### DW-717: `production-deployment-verification` CI job not wired to the ephemeral EventStore local-feed provisioning script; unconfirmed whether it needs it.

status: done 2026-09-05
origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-01
archived: 2026-11-23

### DW-718: Capture and independently review the real C1.15 Production runtime/control-plane identity packet.

status: done 2026-10-04
origin: Story 27.21 C1.15 producer identity hardening, 2026-09-01
archived: 2026-11-23

### DW-719: Decode and scan the complete Dapr metadata object for secret-shaped values before allowlist projection.

status: done 2026-10-03
origin: migrated from legacy ledger (""), 2026-09-01
archived: 2026-11-23

### DW-720: Two new `deferred-work.md` entries under `spec-24-9` (the syntactic-isolation-wording and classification-gap items added earlier in this same file, immediately preceding this section) use only the legacy free-text `source_spec`/`summary`/`evidence` shape, missing the `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` fields the file's own reformatted schema requires (as used by, e.g., the neighboring `DW-716`-`DW-718` entries and the reformatted `24.7-*` items).

status: done 2026-09-01
origin: migrated from legacy ledger ("Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-09-01)"), 2026-09-01
archived: 2026-11-23

### DW-721: A `correct-course` pass is needed to amend AC6's binding text (this story `:39` and its `epics.md` copy) to match the current staged-OpenBao verification design instead of the superseded vault-Component-substitution workaround it still literally describes.

origin: migrated from legacy ledger (""), 2026-09-01
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:39; _bmad-output/planning-artifacts/epics.md
reason: Commit `8a5fa3c6` (2026-08-08, landed under `spec-gh-29804293613-fix-production-deployment-verification`, outside Story 27.3) redesigned the lane to stage a real disposable OpenBao before scale-up; qualifying run `33400812038` (job `99516369413`, artifact `9761351293`) reports `secret-store-substitution.json` with `substitutionPerformed: false` and `substitutionVerified: true`, both Components still typed `secretstores.hashicorp.vault`, and a confirmed `200` health packet showing `dapr-statestore: Healthy`. This demonstrates the real OpenBao secret-resolution path that AC6 still calls unproven while making AC6's literal "the patch, the post-patch readback" clause structurally unreachable under the improved design; the 2026-09-01 review closed checkpoint C2 on this evidence, but the binding text still needs human-approved correction in both governed copies. Source story: 27-3-production-adapter-and-deployment-profile. Re-open trigger: an approved `correct-course` proposal rewrites AC6's patch/post-patch-readback clause and OpenBao-path disclosure in both governed copies.
status: open
decision: 2026-09-06 Correct the criterion — Run a focused correct-course update and reconcile evidence anchors.
decision: 2026-09-06 Correct the criterion — Run a focused correct-course update and reconcile evidence anchors.

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

Story 31.2 reconciliation, 2026-11-22: retained the September staged-OpenBao disposition and its packet-verification limit. The current deployment preflight failed. See `tests/31-2-runtime-secret-store-evidence.md` for the separate scoped-read and inventory blockers.

Story 31.2 current deployment, 2026-11-22T15:23Z: the 14:44Z preflight had failed in a shell without the cluster kubeconfig. A later read-only replay against `jpiquot@local` resolved `redis-secret` through the vault-typed runtime `secretstore` from both memories pods, with HTTP 200 and the field present. It also recorded the component-scope and name-policy denials. That is current deployed evidence. It does not reparse the September packet and does not change this disposition. See `tests/31-2-runtime-secret-store-evidence.md`.

### DW-727: ID: 27.3-CR30 — same reachable-OpenBao/secretKeyRef-resolution discharge condition as 27.3-CR29, tracked as a separate ID; the entry recording its discharge was never written to this register.
origin: code review of 27-3-production-adapter-and-deployment-profile (2026-09-02), reconstructing a discharge the story file had cited at this ID since 2026-09-01 without a corresponding register entry
location: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md:817,1039 (C2 checkpoint row and AC6 verification row)
source_spec: n/a — same discharge evidence as 27.3-CR29
severity: high
reason: See DW-726 — same run/job/artifact evidence, same job-level-verified/packet-level-accepted disclosure.
status: resolved 2026-09-02 by code review, on the evidence above (job-level evidence independently verified; packet-level detail accepted from the story's existing transcription, not re-parsed)

Story 31.2 reconciliation, 2026-11-22: retained the September staged-OpenBao disposition and its packet-verification limit. The current deployment preflight failed. See `tests/31-2-runtime-secret-store-evidence.md` for the separate scoped-read and inventory blockers.

Story 31.2 current deployment, 2026-11-22T15:23Z: the 14:44Z preflight had failed in a shell without the cluster kubeconfig. A later read-only replay against `jpiquot@local` resolved `redis-secret` through the vault-typed runtime `secretstore` from both memories pods, with HTTP 200 and the field present. It also recorded the component-scope and name-policy denials. That is current deployed evidence. It does not reparse the September packet and does not change this disposition. See `tests/31-2-runtime-secret-store-evidence.md`.

### DW-728: `eventstore` AppHost gateway resource cannot start under Memories' mandated SDK 10.0.400 alone — Aspire always launches it via `dotnet run` against the EventStore submodule's own project, which pins SDK 10.0.302.

status: done 2026-09-05
origin: code review of spec-28-1-adopt-owner-approved-eventstore-runtime-identity, 2026-09-05, discovered by actually running a new runtime-health test this review added (`tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventStoreAppHostResourceGraphTests.cs`, `EventStoreResource_StartsAndBecomesHealthy`, currently `[Fact(Skip = ...)]`)
source_spec: `_bmad-output/implementation-artifacts/spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md`
severity: high
archived: 2026-11-23

## Deferred from: code review of 31-1-openbao-platform-hardening-and-documentation.md (2026-09-07)

### DW-729: `deployment-seal-external` NetworkPolicy `.spec` was not captured, so it is unknown whether it widens port 8200 while `hexalith-keys` still matches.

origin: Deferred from: code review of 31-1-openbao-platform-hardening-and-documentation.md (2026-09-07)
location: _bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md:831
source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation.md`
severity: medium (unverified)
reason: Evidence §8.8 names NetworkPolicy `deployment-seal-external` with no `.spec`. The `hexalith-keys` policy was captured and matched the 2026-07-28 table; this extra policy was not, so a second ingress source on 8200 would be invisible while the bound NetworkPolicy probe still reads as unchanged. Pre-existing relative to this continuation’s frozen probe list (nodes, replicas, HA, `hexalith-keys` NetworkPolicy, automount, secret names/types). - ID: 31.1-CR-NP-SPEC - Status: open - Source story: 31-1-openbao-platform-hardening-and-documentation - Target artifact: `_bmad-output/implementation-artifacts/tests/31-1-openbao-platform-evidence.md` - Re-open trigger: `kubectl -n openbao get networkpolicy deployment-seal-external -o jsonpath='{.spec}'` against `jpiquot@local` shows whether the policy admits extra 8200 sources.
status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: Telemetry, consistency, and MCP WebAppFactory hosts omit `Hexalith:Dapr:ApplicationTokenOverride`, so leaked `APP_API_TOKEN` can 401 those in-memory hosts.
  evidence: Concurrent Dapr-token overlay in this worktree, not Story 31.1. Settled by applying the EventStore/HealthCheck empty overlay to `TelemetryWebAppFactory`, `ConsistencyEndpointFactory`, and both `McpWebAppFactory` classes.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: Empty `Hexalith:Dapr:ApplicationTokenOverride` skips Dapr app-token checks in every environment, including Production.
  evidence: Concurrent `DaprApplicationTokenMiddleware` change. Restrict the bypass to test/Development, or document and test missing-overlay vs env fallback.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: `DaprTokenEnvironmentCollection` comment and duplicate `UseSetting` paths disagree with the overlay design.
  evidence: Concurrent Dapr test-host work. Align the comment, use the collection name constant, and keep a single configuration path.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: `tools/init-dapr-runtime.sh` starts placement/scheduler without waiting for ports 50005/50006.
  evidence: Concurrent CI work. Wait for the ports (or container health) before integration tests, and do not `dapr uninstall --all` on a slim-init failure.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: `addendum.md` still treats launch/release dates as assistant assumptions after Jerome confirmed 2027-01-01 / 2026-12-01.
  evidence: Concurrent planning review. Rewrite the Release decision paragraph so it does not contradict the PRD it must not override.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: `prd.md` now requires a README MIT-license pledge that `README.md` does not contain.
  evidence: Concurrent PRD close-out. Add the required README sentence or drop the PRD requirement.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: G1 is rewritten as a hard protocol with no owning story while FR17/FR25 remain partial.
  evidence: Concurrent PRD rewrite. Add backlog stories or an SCP that G1 is unreachable.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-1-openbao-platform-hardening-and-documentation-2.md`
  summary: After Dapr reconnect, `AspireIngestionPipelineFixture` waits for HTTP listen and does not call the OpenBao sidecar matrix wait.
  evidence: Concurrent integration-test work. Reuse `WaitForOpenBaoSidecarMatrixReadinessAsync` on that path.


### DW-730: Production access-telemetry lifecycle qualification (parked full campaign) — reopens only when a Production deployment path enables lifecycle writes.

origin: Story 27.4 scope reset, `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md` (owner "yes" to D1–D4), 2026-11-01
location: docs/operations/access-telemetry-full-qualification.md (34-row historical inventory); _bmad-output/planning-artifacts/epics.md Story 27.4 historical full-campaign acceptance criteria
reason: - source_spec: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-01.md` summary: Qualify the PostgreSQL access-telemetry lifecycle store for Production before any Production deployment enables lifecycle writes. Parked from Story 27.4 together with the former Story 27.3 Tasks 2–8 transferred scope and the 34 checkpoints of the historical inventory (C0, C1.1–C1.25, C1 aggregate, C2–C6, terminal validation, A41 publication), all `pending`/`open` when parked. Parked acceptance criteria, verbatim from epics.md Story 27.4: (AC1) **Given** the approved immutable profile, a short test retention window, and a production-shaped deployment, **When** old and new access events cross the expiry boundary across at least two Server writers and controlled workload, sidecar, actor, Placement, Scheduler, and backend-fault recovery, **Then** focused evidence proves exact acknowledgement, durable recovery, expired-record unavailability and purge, newer-record preservation, required audit emission continuity, and tenant/privacy denial before dependencies. (AC2) **Given** the ratified Production duration and exact adapter profile, **When** operators deploy, monitor, change, fail over, recover, or roll back the policy, **Then** telemetry, deployment configuration, capacity, monitoring, incident, recovery, adapter-reclamation, and decommission documentation identifies the owner, configuration, defaults, storage impact, purge verification, alarms, rollback, RPO/RTO limits, and assurance boundary. (AC3) **Given** C2-C6, terminal validation, and publish verification all pass against the unchanged approved profile, **When** A41 is closed, **Then** `20.5-A41-ACCESS-TELEMETRY-RETENTION` is reconciled from `carried-forward`, the matching sprint action is closed, architecture and all A41 summaries cite the same canonical evidence, and Epic 20/Story 20.5 remain historical `done` records rather than being reopened. (On reopening, AC3 converts the DW-17 accepted debt into a qualified closure.)
status: open
decision: 2026-11-01 keep-open — parked. Reopens only on the trigger: any Production deployment path (the Memories overlay or Platform composition through manifest reference `accesstelemetrylifecycle-enabled`) that sets `AccessTelemetryLifecycle:Enabled=true` or runs a lifecycle Deployment above zero replicas. "Do recommended", an available schedule window or new Platform capability does not reopen it. When triggered, re-plan from scratch against the real Production target; the 2026-10-25 → 2026-10-31 synthetic-target preparation (Spec 14 continuations, specs 15–26) is anti-template. Owner: Jérôme Piquot. Review: 2027-05-01 with DW-17. Follow-up, not scheduled: the five access-telemetry tooling CI lanes (`.github/workflows/ci.yml:368-397`) could move to nightly or be retired while this entry stays parked.

## Deferred from: DW-719 independent review (2026-10-03)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-4.md`
  summary: Reject duplicate Dapr metadata JSON properties before overwritten decoded secrets can escape C1.15 validation.
  evidence: Independent review and root fake-target verification reproduced earlier Unicode-escaped diagnostic canaries and credential properties overwritten by benign duplicates. Both the canonical pre-change collector at 42995692634af9ba35982ec0e1aedec4f5576e71 and the current collector return exit zero, producerStatus observed, and no blocker. This is a verified medium pre-existing parser defect; preserve every property during scanning or reject duplicates before conversion, with regression coverage. It does not reopen the completed DW-719 decoded-object walk or supply gate acceptance.
  status: resolved 2026-10-03
  resolution: `spec-27-21-runtime-control-plane-identity-5.md` — Disposable bounded JSON preflight rejects repeated decoded names, including nested objects/arrays and undecodable Unicode names, before lossy conversion. Encoded-secret and high/low surrogate collision regressions block safely without later alpha probes or metadata provenance. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed 27/27 methods in 246.606 seconds. Only this parser follow-up resolves; DW-718 remains open, the operator handoff remains awaiting-operator, C1.15 pending/not complete, and Story 27.21 in-progress.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-4.md`
  summary: Preserve Dapr metadata JSON root shape and reject a singleton array containing an otherwise valid object.
  evidence: Independent review and root fake-target verification reproduced [valid_metadata] returning exit zero and producerStatus observed on both the canonical pre-change collector at 42995692634af9ba35982ec0e1aedec4f5576e71 and the current collector because ConvertFrom-Json unwraps the root array. This is a verified medium pre-existing parser defect requiring an object-root check and a focused regression; C1.15 and operator capture/review remain pending.
  status: resolved 2026-10-03
  resolution: `spec-27-21-runtime-control-plane-identity-5.md` — JSON preflight requires an object root before conversion; singleton/nested/multiple/empty arrays and empty/whitespace responses return the constant malformed-metadata-json blocker without alpha probing or metadata provenance. `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v` passed 27/27 methods in 246.606 seconds. Only this parser follow-up resolves; DW-718 remains open, the operator handoff remains awaiting-operator, C1.15 pending/not complete, and Story 27.21 in-progress.

## Deferred from: Story 27.21 capture review (2026-10-04)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-EVIDENCE-RETENTION
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: artifacts/access-telemetry-c1/C1.15/
  Owner: Deployment Adapter Developer
  Re-open trigger: accepted C1.15 evidence must be consumed outside this workspace, or an approved evidence archive is available.
  Acceptance criteria: assign a retention owner and approved retrievable archive; preserve the exact immutable packet/review bytes and every bound collector source version; independently verify all stored SHA-256 hashes and retrieval.
  summary: Deployment Adapter Developer should name a durable retrievable archive and retention owner for independently accepted C1.15 packets, reviews, and their bound collector sources before those files must be consumed outside the current workspace.
  Evidence: The pre-existing collector stores immutable evidence under ignored `artifacts/`; a fresh checkout contains tracked hashes/disposition references but cannot retrieve the actual packet/review. Current local evidence was independently read and validated and remains accepted. Source snapshots `c1.15-collector-source-7a1a00a9ddf0dcaf1f1b295b7c3d62b7877443048689cc393cf75b1ed06317b8.ps1`, `c1.15-collector-source-23e5b55f5b11dbb546eda8d8772a25cfc8ecda52b3cd73968ac3d0369a379648.ps1`, and `c1.15-collector-source-030152bd368ccf1fd8d4e577b33cddef8fd8659b2ce7b3a347f9c9638eaac8d7.ps1` are preserved locally in that directory, mode `0444`, with SHA-256 equal to the hash in each filename; the final framed collector snapshot `c1.15-collector-source-00e68b0f6b5d9782941b2942eede60af54cac9db8101d19e740e1b89abf138ab.ps1` is also preserved, mode `0444`, 35,454 bytes, with matching filename SHA-256; preserve subsequent bound versions too. Completion requires an approved retrievable location, explicit retention ownership, and matching immutable hashes; no archive publication or Git mutation is authorized by this capture slice.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-SOURCE-LABELS
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1
  Owner: Deployment Adapter Developer
  Re-open trigger: the next collector provenance maintenance change.
  Acceptance criteria: produce purpose/stream-specific kubectl source labels and a regression proving unambiguous context/version/alpha stdout and stderr mapping; retain every immutable historical packet.
  summary: Deployment Adapter Developer should correct pre-existing kubectl stdout/stderr source labels to include exact purpose and stream and add a provenance-label regression.
  Evidence: Hash-bound collector interpolation labels six sources `kubectl:`; the independent review recovered and recomputed all six exactly by deterministic position, so current packet acceptance is unaffected. Completion requires purpose/stream-specific labels and a regression proving unambiguous context/version/alpha stdout and stderr mapping while preserving all immutable historical packets.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-RAW-IDENTITY-TYPES
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1; tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py
  Owner: Deployment Adapter Developer
  Re-open trigger: the next raw Pod identity/type-validation maintenance change.
  Acceptance criteria: reject non-string phase and label values during discovery, and non-string name/UID/label/image-ID values during recheck, including singleton arrays that PowerShell otherwise coerces; add negative regressions without weakening valid Kubernetes observations or constant safe blockers.
  summary: Deployment Adapter Developer should replace pre-existing comparison casts with explicit raw-type checks for malformed Pod identity fields.
  Evidence: Independent final review reproduced singleton-array phase/label discovery and name/UID/label/image recheck values being coerced to matching strings. Those casts predate this capture correction; actual Kubernetes typed fields are strings. This low malformed-input limitation is separate from the new per-Pod collection framing and supplies no Production or other-gate acceptance.

## Deferred from: Story 27.21 final framed review (2026-10-04)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-SELECTED-POD-SCHEMA
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1; tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py
  Owner: Deployment Adapter Developer
  Re-open trigger: the next selected-Pod schema maintenance change.
  Acceptance criteria: require valid selected-Pod phase values and complete typed name/UID identities before filtering Running Pods; reject null/invalid phases and incomplete Pending siblings with constant safe blockers before provenance/exec; retain legitimate Pending siblings and exact associations.
  summary: Validate complete selected-Pod schema before non-Running observations are filtered away.
  Evidence: Root independently reproduced null phase, invalid phase string and Pending missing name beside a valid Running Pod returning observed on both baseline 905e6628 and the corrected collector. The pre-existing phase filter ignores those malformed siblings. Current real selected identities were independently complete and typed; no captured gate authority is broadened. Exact synthetic results: `/tmp/c1-15-review3-root-finding-verification.log`.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-DAPR-SCALAR-TYPES
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1; tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py
  Owner: Deployment Adapter Developer
  Re-open trigger: the next Dapr metadata raw-type maintenance change.
  Acceptance criteria: require actual JSON strings for metadata id/runtimeVersion before casting; negative regressions reject singleton arrays and other scalar/root types without exposing secrets, hashing rejected metadata or continuing the alpha probe.
  summary: Reject singleton-array Dapr scalar identity values before PowerShell comparison casts.
  Evidence: Root independently reproduced id and runtimeVersion singleton arrays returning an observed packet on both baseline 905e6628 and current source. The casts are pre-existing; this is separate from Pod raw-type follow-up 27.21-C1.15-RAW-IDENTITY-TYPES. Current real Dapr values were independently observed as strings. Exact synthetic results: `/tmp/c1-15-review3-root-finding-verification.log`.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-CONTAINER-INSTANCE-DRIFT
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1; tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py
  Owner: Deployment Adapter Developer
  Re-open trigger: the next lifecycle capture/recheck stability maintenance change.
  Acceptance criteria: retain and compare lifecycle/daprd process-instance markers such as restart counts/container IDs across discovery/recheck; fail closed on changes while preserving Ready/UID/image checks; add a regression that changes either container between observations.
  summary: Detect container restarts between identity capture and Pod recheck.
  Evidence: Root independently increased both restartCount values between discovery/recheck and reproduced observed on current and baseline 905e6628 collectors. The existing recheck verifies Ready/UID/label/image but not process-instance continuity, a medium pre-existing stability limitation. The accepted real packet has matching initial/recheck projection hashes and independently observed zero restarts, so this does not falsify current identity acceptance. Exact synthetic results: `/tmp/c1-15-review3-root-finding-verification.log`.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-21-runtime-control-plane-identity-6.md`
  ID: 27.21-C1.15-PROJECTION-PROVENANCE-RETENTION
  Status: open
  Source story: 27-21-runtime-and-control-plane-identity
  Target artifact: tools/verify-access-telemetry-c1.ps1; artifacts/access-telemetry-c1/C1.15/
  Owner: Deployment Adapter Developer
  Re-open trigger: future evidence needs source-projection recomputation after live Kubernetes metadata changes.
  Acceptance criteria: design and retain a reconstructible secret-safe canonical identity projection or its exact validated input bytes for future packets; independently recompute its hashes from retained evidence without live state; preserve all earlier immutable packets and their capture-time verification limits.
  summary: Make future Pod source-projection hashes independently recomputable after live metadata changes.
  Evidence: Baseline and current producers hash raw Pod observation text but retain only packet observations, excluding transient metadata/status detail. The independent reviewer recomputed all current hashes against matching live responses; that proves capture-time provenance. Once live metadata changes, the discarded exact Pod input cannot be reconstructed solely from packet observations. This pre-existing temporal limitation is separate from locating/owning the external archive; no historical bytes may be fabricated and current accepted evidence remains unchanged.

- source_spec: `_bmad-output/implementation-artifacts/spec-c1-16-component-backend-capture-preparation.md`
  summary: Preserve container incarnation when shared C1 capture coherence must exclude in-Pod restarts.
  evidence: Existing C1.15 and inherited C1.16 identity patterns compare Pod UID and imageID only; injected containerID/restartCount changes leave an observed neutral capture. Future qualifying evidence must compare real incarnation before and after.

- source_spec: `_bmad-output/implementation-artifacts/spec-c1-16-component-backend-capture-preparation.md`
  summary: Bound the shared authenticated metadata response before remote shell command substitution.
  evidence: Existing C1.15 wget probe and inherited C1.16 probe buffer the full HTTP response before local one-megabyte capture limits apply; an oversized response can allocate remote memory. Reopen with a bounded remote reader and oversized actual-probe rejection evidence.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg-onprem-2-approved-profile-adoption.md`
  summary: Deployment Adapter Developer owns separately scoped C1 container-incarnation continuity maintenance before evidence relies on uninterrupted processes.
  evidence: B4/E1: the inherited tools/access-telemetry-c1-component-backend.ps1 Pod recheck compares UID/imageID but omits containerID/restartCount; an in-Pod restart retains an observed neutral capture. This predates the PG2 adoption snapshot. Current captures cannot prove unchanged process incarnation. Reopen by comparing typed initial/final containerID and restartCount and executing actual-path restart rejection fixtures while preserving immutable historical packets.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg-onprem-2-approved-profile-adoption.md`
  summary: Deployment Adapter Developer owns separately scoped remote metadata-reader bounding before response size is relied on as a remote memory bound.
  evidence: B5/E2: the inherited authenticated wget probe in tools/access-telemetry-c1-component-backend.ps1 buffers the full HTTP response in remote shell command substitution before the local one-megabyte limit; oversized responses can allocate remote memory. This predates PG2 adoption. Reopen with a remote reader bounded before substitution and executed oversized actual-probe rejection evidence, including secret-safe output and preserved historical packets.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg-onprem-2-approved-profile-adoption.md`
  summary: Deployment Adapter Developer owns separately scoped complete selected-Pod schema validation before filtering non-Running siblings.
  evidence: B6: inherited Get-C1StablePods in tools/access-telemetry-c1-component-backend.ps1 accepts any alphabetic phase and filters non-Running observations before validating complete name/UID identity; a malformed non-Running sibling beside a valid Running Pod can be omitted from an observed capture. This predates PG2 adoption. Reopen by validating Kubernetes phase values and complete typed identities for every selected Pod before filtering, with executed Bogus-phase/missing-UID negative fixtures and legitimate Pending-sibling coverage.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity.md`
  summary: Platform Operations must bind and protect the approved kubeconfig context's actual endpoint and cluster trust identity before live C1.16 collection.
  evidence: B6: Existing C1 producers and the new candidate collector trust a context-name mapping, which can be repointed independently of the scope receipt. This inherited operator trust boundary is not live-authorized by offline preparation. Reopen only with retrievable target authority binding the actual endpoint/trust fingerprint and protected configuration throughout the observation window; missing or uncertain binding prohibits contact and leaves C1.16 pending.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity.md`
  summary: Deployment Adapter Developer must execute the emitted session query contract in an isolated PostgreSQL 18.6 environment before live C1.16 collection.
  evidence: V3: The synthetic kubectl fixture manufactures JSON independently of SQL execution, so renaming a SQL output alias does not fail the 16-method suite. The approved slice is synthetic offline preparation. Reopen with an isolated same-version query execution check proving exact fields, role/database, timestamp and TLS shapes without tenant records; the live checkpoint stays pending until this prerequisite is met.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`
  summary: V3 remains open after real isolated PostgreSQL 18.6 execution exposed the emitted client-address contract mismatch.
  evidence: 2026-10-06 — The 10-method disposable integration run exited 1 (9 passed, 1 failed, zero errors/skips): unchanged `a.client_addr::text` returned `172.18.0.3/32`, and existing validation denied the valid idle TLS runtime-role/database session against selected IP `172.18.0.3` with `session-identity-or-tls-invalid`. Receipt `/tmp/c1-sql-receipt-hmkwj_j_/receipt.json` SHA-256 `f8b61d24ae5e324477f7ecc95d3dd4876e49e366a062731a1fb977980b61b168` binds actual 18.6 / 180006 / peer:postgres, exact approved child, source/query/test/command/raw-log hashes and successful uniquely owned cleanup. Raw runner log `/tmp/story-27-22-sql-contract-final-v2.log` SHA-256 `79efbe42cea6b790b574dbac793a75ed3e7e58f47da79adebc3e50c4d57c7486`. A temporary-copy `host(a.client_addr)` diagnostic passed existing validation; tracked collector/deployment bytes remain unchanged. Owner: Deployment Adapter Developer. Consequence: V3 unresolved; no live linkage collection. Reopen trigger: separately authorized collector correction, full no-skip successful integration execution and independent review. Original V3 and B6 endpoint/trust authority remain open; eligible target, credentials, exclusive window, external archive, named reviewer and pending C1.16 still apply. No other-gate, Production, Story 27.4 or A41 credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`
  summary: The user-authorized V3 client-address projection correction passes isolated execution; V3 closure is pending independent review.
  evidence: 2026-10-06 — After explicit "Approve the collector fix and finish verification" approval, emitted SQL uses `host(a.client_addr)` while existing validation/transport/neutral packet fields remain unchanged. Integration exits 0, 10/10 test methods, zero failures/errors/skips in 25.468 seconds, including restored old-projection real `/32` rejection and broken-alias mutation. Receipt `/tmp/c1-sql-receipt-gylthuxh/receipt.json` SHA-256 `12b08a9a9030c8b6d5ddec3437d44132127ceaea96fe42f3bd4f9c8e2050aaa3`; log `/tmp/story-27-22-sql-contract-corrected.log` SHA-256 `5e41e6c86996b89f7c0227dc8281ec7fbdef4e2b2cf4276eb2962a7b53362e1f`. This supersedes the source-correction blocker only. Owner: Deployment Adapter Developer. Consequence: V3 remains open until independent review; no live linkage collection. Reopen trigger: parent independent review of exact successful execution and focused regression evidence. B6 target endpoint/trust authority and all live target/credentials/exclusive-window/archive/reviewer prerequisites remain open; Story 27.22 in-progress, C1.16 pending, no other-gate/Production/Story 27.4/A41 credit.

  focused_regressions: 2026-10-06 — Existing linkage fixture command `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'linkage_test.py' -v` exits 0, 26/26 methods, zero failures/errors/skips in 280.057 seconds; `/tmp/story-27-22-linkage-corrected.log` SHA-256 `446f925434011a2bcb5b6023a99d1604ece8d14fd89e743df12e84bef698939f`. Slice, file scope, review-readiness, whitespace and PowerShell parse checks pass. V3 disposition is still pending parent independent review; B6/live boundaries remain open.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-2.md`
  summary: V3 isolated PostgreSQL emitted-SQL prerequisite closed after independent review and complete parent verification; B6 and live C1.16 remain open.
  evidence: 2026-10-06 — All three review layers completed; all 14 findings were triaged and patched, with an independent 9/9 harness recheck. Parent `PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_sql_contract -p '*_test.py' -v` exited 0: 19/19 methods (10 real PostgreSQL, 9 harness), zero failures/errors/skips, 26.289 seconds. Receipt `/tmp/c1-sql-receipt-3bd_gjy6/receipt.json` SHA-256 `c42eb91b0b356cee4135c8da8b2289e07469a93b289eae8aa87425f226ac52b9` binds exact approved child, actual 18.6 / 180006 / peer:postgres, all expected outcomes, current source/query/command/raw hashes and verified owned cleanup. Parent log `/tmp/story-27-22-parent-final-sql.log` SHA-256 `32482e58654fba8683843c39b4892dd6673c260a38e0233907cdeffb1978a111`; focused existing linkage 26/26 passed. This supersedes original V3 and both subsequent pending/correction-blocker entries only; original evidence remains historical. Owner: Deployment Adapter Developer. Reopen trigger: source/query/profile drift requires same-version re-execution and review. B6 actual endpoint/trust and protected configuration, eligible target, protected credentials, exclusive window, external archive and named live reviewer remain open. Story 27.22 in-progress, C1.16 pending / not complete, all neutral fields preserved; no live linkage, other-gate, Production, Story 27.4 or A41 credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: B6 target binding and bounded live preparation fulfilled; actual C1.16 capture is independently blocked by the collector capability contract.
  evidence: 2026-10-06 — User "do it yourself" delegated target preparation/archive/reviewer ownership. Protected endpoint/trust/config authority and narrow maxConns addendum are retained outside Git; approved PostgreSQL 18.6 / 180006 / peer:postgres and original 400 GiB PVC verified. Lifecycle has one Ready literal-disabled replica, clock zero, Production false, original ingress policy restored and zero owned probes. Two actual identity commands exited 1 with no positive partials: component-settings-mismatch, then metadata-component-capabilities-mismatch. Approved Dapr 1.18.1 advertises ACTOR in addition to the four required capabilities; no linkage/reminder calls ran. Independent blocked disposition `/home/administrator/evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/independent-blocked-disposition-1.json` SHA-256 `d25f8a805908df98efd633a0fef0f8dd3a135182667ca7a8449491def5b55454` verifies 203 retained operator entries and 111 operation ledgers. This supersedes only fulfilled B6 target binding/access/archive/reviewer preparation, not actual identity/linkage or exclusivity proof. Owner: Deployment Adapter Developer. Consequence: Story 27.22 in-progress, C1.16 pending / not complete. Reopen trigger: narrowly approved source correction, post-application regressions and V3 re-execution/review under new helper hash, fresh attributable live capture and independent acceptance.
  proposed_correction: Reviewed unapplied three-file patch `/home/administrator/evidence/hexalith-memories/C1.16/20261006T120300Z-1d9df8e4e777/collector-fix-proposal/v2/proposed.patch` SHA-256 `9085be3a099cbb6bea0ff441701ed9ebe9e6c1c44bd95f1e649e334481787d84`; four private-copy test methods, two actual-metadata offline replay cases and seven independent helper cases pass. Current repository source/profile bytes remain unchanged; current discovery 85 C1 / 19 SQL methods, delta +0. Scope amendment is concrete and pending; no synthetic/live acceptance, other-gate, Production, Story 27.4 or A41 credit. Retention is persistent local filesystem outside Git, with 0444/fsync/readback but no off-site/WORM guarantee. Original failed packets and prior V3 receipts remain immutable historical records.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: Approved capability correction and fresh V3 verified; live identity succeeds but actual Scheduler reminder lookup returns HTTP500 before linkage.
  evidence: 2026-10-06 — Exact approved three-file patch applied; complete no-skip identity20/20, linkage26/26 and real SQL19/19 modules pass. Fresh retained V3 receipt SHA-256 `859968055fd93a9551fc877b6f4dfd1d01ed6ad17b86e7b5232ffbf3d3fad582` is independently accepted for disposable SQL only in disposition `76a789eaf2a84fd6a0c8e858ec4ef6912d406dae207087b9cd4a59937de91f77`. Source binding `046f7f733b5122efe70827642cc486eba80157724d2aaeffab15185fd4dc7b77` precedes contact. Two fresh identity commands exit0 with neutral fields; latest packet SHA-256 `c0ae4a1193951b8a729742f146bcf54f37d0ae3fc8b6d92d1359d0b2d87c8e99`. The exact private reminder response is HTTP500 / ERR_ACTOR_REMINDER_GET, well-framed157bytes; no response body/header values retained. Genuine404absence is not established and linkage never runs. Latest window post SHA-256 `f909290793624fa2dfd622d0c413109066a971af4b620494a64fdb02ff61657a` verifies restored original ingress, zero owned probes and no restoration errors; final controls SHA-256 `f75d778cdbe07f2b02ad4dfd36e91afdb178165de5e2e68647a9acbe30698bfd` keep lifecycle1Ready disabled, clock0, actual Production referencefalse and original PVC. Artifacts are persistent local0444/fsync/readback under `/home/administrator/evidence/hexalith-memories/C1.16/20261006T125948Z-dbbe89368caa`, with no off-site/WORM claim. This supersedes the collector capability and fresh-V3-review prerequisites only; blocked attempt independent review and underlying runtime classification are pending. Owner: Deployment Adapter Developer. Consequence: Story27.22 in-progress, C1.16 pending / not complete. Reopen trigger: diagnose and resolve Scheduler reminder lookup under reviewed authority, then fresh genuine before/after absence and independently attributable accepted linkage. No Production activation, reminder/tenant mutation, other-gate, Story27.4 or A41 credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: The reminder diagnosis is resolved by bounded external transport; fresh attributable C1.16 capture and acceptance remain required.
  evidence: 2026-10-06 — Independent interim blocked disposition272c60 verifies earlier immutable attempts and restoration. A new private lookup classified request-canceled; actual BusyBox EOF and pinned Go context behavior support the transport correction, without reconstructing historical bodies. Eight bounded/privacy checks and independent disposition1ebac6 pass. Corrected actual read-only reminder receipt SHA-256 `493cdc413bb1b3091b3d31e9cfc1cdd38036c2e79a0cc27f2a928a59b46abecf` establishes genuine404 / ERR_ACTOR_REMINDER_NOT_FOUND, stable selected incarnation/protected binding and unchanged controls. Canonical repository collectors remain unchanged; transparent external state GET transport bridge passes27 checks and independent disposition38c1de, with exact submitted/executed command provenance and mandatory native0/real204/emptybody. Root released exact external scripts for a fresh bounded sole-operator capture. This supersedes the unresolved diagnosis only; it does not accept historical failures or fresh linkage prospectively. Owner: Deployment Adapter Developer. Consequence: C1.16 and Story27.22 remain pending until the fresh capture is independently accepted and full build review passes. Reopen trigger: actual before/after absence, exclusive attribution, immutable source/response/command evidence and restored controls satisfy the independent reviewer. No Production activation, reminder/tenant mutation, other-gate, Story27.4 or A41 credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: Canonical exact-one linkage refused two actual startup pool sessions; a separate full-set observer is being prepared without changing its contract.
  evidence: 2026-10-06 — Fresh identity exits0, but unchanged Observe linkage exits1 with session-attribution-ambiguous before the stateGET; packet SHA-256 `b8499bc2f221a6e5579961b00f4207b3aad07243bdaeb3a95b28b697ee3c2476`, observations=null and neutral acceptance fields. Actual query retains both same-pod idleTLS1.3 runtime sessions, PID21593/21594. Independent closed-attempt disposition SHA-256 `a0f0abf3f4c90f3f0d84b9ca35b0724c2cac66bdcb48af930c5a17f15baad716` verifies202 entries/77 ledgers, before404/actors0 and restored policy/probe/disabledcontrols, with no linkage/gate acceptance. Pinned startup pool analysis SHA-256 `e4b607a13948efbb8aa22cf5c4a6370c37da63e86928bdce03241335bae6d0af` explains two acquired connections and five-minute idle retirement; no assured exact-one procedure exists before the240-second capture boundary. Root authorizes only separate external full-set read-only attribution preparation within existing independent-linkage scope, preserving every actual candidate and the canonical refusal. Owner: Deployment Adapter Developer. Consequence: this fresh attempt remains unaccepted; the canonical tool's exact-one limitation remains for future captures. Reopen trigger: a separately reviewed full-set observer with meaningful actual PostgreSQL cases, exact fresh authority/source release, genuine204 correlation, before/after absence and independent acceptance; any permanent repository collector enhancement needs its own authorized change. No configuration/session termination/filtering, Production activation, other-gate, Story27.4 or A41 credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: Actual C1.16 identity and separate full-set independent linkage are accepted; only the canonical tool's future multi-session limitation remains.
  evidence: 2026-10-06 — Independent accepted disposition SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70` in external capture-pool-20261006T150600Z-3c2f874a verifies176 files/96 actual sequential ledgers, both identical neutral identities, all2 selected-IP idleTLS runtime sessions, exactly PID25584 advancing both clocks on a real native0 complete204/emptybody GET while PID25585 remains unchanged, before/after404/actor0, source/clock/incarnation/policy/timer proof and restored controls. Supplemental packetb8331c3a, post82d3a5e4, controls71b3832d and immutable handoff/inventories are linked from Story27.22; root reads back176/176 and824/824 snapshot files. This supersedes the unresolved actual C1.16 identity/linkage/exclusive-window/archive/review prerequisites only. Prior failed attempts retain their failed dispositions. Owner: Deployment Adapter Developer. Residual consequence: unchanged canonical Observe still exits1/session-attribution-ambiguous for two startup sessions; the accepted result uses separately authorized/reviewed external full-set evidence, with no filtered canonical input or fabricated success. Reopen trigger: future capture uses fresh approved source/authority/window and independent full-set review, or separately authorize/validate permanent canonical multi-session support. Any source/query/profile drift reopens affected V3 verification. C1.16 checkpoint completed2026-10-06; Story27.22 review pending full build review. Producer fields remain neutral, actual Production referencefalse (no Server-process uptake claim), no Lease, no other-gate/security/Production/Story27.4/A41 credit. Archive is persistent local0444/fsync/readback without off-site/WORM protection.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-22-component-and-backend-identity-3.md`
  summary: Story27.22 completed after accepted actual C1.16 and all three build reviews; canonical multi-session support remains a separately owned future limitation.
  evidence: 2026-10-06 — Accepted captured identity/full-set linkage disposition SHA-256 `ad2d3024dff5cd00cb8518a3e65b16d006acf596046d193373d0ae55ea2cff70`; full build review `2ccb4a78a125a34fdd71785ccd36dbd96b39c0111b2e5a4da2e9a508aaa05552` classifies10 claims, resolves6 documentation/ledger claims and rejects4 with evidence. Final code-review rows reconcile9/9 reviewed paths, comparable86 C1/19 SQL methods, phase+0 and cumulative+1 C1/+0 SQL; retained20/26/19 no-skip suites are unchanged. Protected-clone cleanup `62e05cd6795510647af07f4f5ce4ad00d8e5af53600f2abff15536e6c0d11430` proves actual owned-clone removal and preserved original linked configuration; closed live controls remain independently verified restored. This supersedes only the preceding full-build-review pending statement. Story27.22/spec/sprint are done; every earlier failed packet stays failed. Owner: Deployment Adapter Developer. Residual consequence: canonical Observe still refuses two startup sessions; accepted linkage uses the separately reviewed external full-set procedure. Reopen trigger: fresh scoped authority/source/window plus independent review for future capture, or separate authorization/verification for permanent canonical multi-session support; source/query/profile drift reopens V3. No Production activation, Lease, other-gate, Story27.4 or A41 credit; no off-site/WORM claim.

## Deferred from: code review of spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md (2026-10-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: `sprint-status.yaml:165` epic-27 order reason still says Story 27.22 C1.16 capture, connection linkage and independent acceptance remain pending, while `:473` marks `27-22-component-and-backend-identity: done` (2026-10-06).
  evidence: Pre-existing relative to the Story 27.4 review; the Story 27.4 spec forbids 27.4 edits to `sprint-status.yaml`. Owner: Story 27.22 completion sync (Deployment Adapter Developer). Consequence: the execution-order reason misstates C1.16 as pending. Reopen trigger: next approved sprint-status sync for Epic 27 reconciles the reason with Story 27.22's done row and its retained canonical single-session limitation.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: The Story 27.4 planning commit `b7a4377a` recompiled `epic-27-context.md` and dropped Technical Decisions: Dapr-only typed-state/actor/reminder lifecycle, millisecond logical timestamps with a one-second UTC attestation bound, writer/key-rotation barriers, separate clock/adapter authorities and secret-store prefixes, and the 250-events/s cluster-wide capacity envelope.
  evidence: medium (unverified). The edit fell outside both 27.4 reviews because `baseline_commit` was rewritten from `ed4528d5` to `b7a4377a`. Agent-context file, so deferred rather than patched. Settle by diffing `git show ed4528d5:_bmad-output/implementation-artifacts/epic-27-context.md` against `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` and the architecture spine: restore any decision still current, or confirm each removal is intentional. Story 27.22 has since edited the same file.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: bmad-build, the official implementation route, has no `_bmad/custom/bmad-build*.toml`, so `story-scope-guard.md`, `story-phase-ledger.md`, `epic-ac-verification.md` and `remediation-runtime-checklist.md` never load when it authors or implements a spec.
  evidence: Every Story 27.4 spec (four, since 2026-09-02) lacks a Change Log, File List, Historical Context Classification, Slice Proof, checkpoint table and Epic AC Verification section; `tools/check-story-slice-scope.py --require-record` exits 1 on the reviewed change set. Fix edits customization files and the resolver fixture (`tests/tooling/bmad_customization/bmad_customization_test.py`), so it is out of review scope. Owner: BMAD process maintainer. Reopen trigger: wire the four policies into bmad-build (and bmad-build-auto) the same way the story-authoring routes are wired, then extend the fixture.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: `tools/check-story-review-readiness.py:771` resolves a sprint story key only to `<key>.md` or `spec-<key>.md`, so numbered follow-up specs (`spec-<key>-2.md` and later) are never selected by story key and the gate no-ops green on the original spec.
  evidence: 2026-10-06: `--story-key 27-4-retention-verification-operations-runbook-and-a41-close-out` selected the 2026-09-02 spec and printed `Artifact carries no ledger, File List, or evidence table; check is a no-op.` (exit 0). Pre-existing tooling limitation. Owner: BMAD process maintainer. Reopen trigger: the gate selects the latest active numbered spec for a key (or fails closed on ambiguity), with tests under `tests/tooling/`.

## Deferred from: second-pass review patch application of spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md (2026-10-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: `epics.md` Epic 27 text still records Story 27.22/C1.16 capture, connection linkage and acceptance as pending (dated 2026-10-04 correction at `epics.md:5192-5204` and registration receipt at `epics.md:5360-5364`), while `sprint-status.yaml` marks `27-22-component-and-backend-identity: done` (2026-10-06).
  evidence: Found by the Story 27.4 Epic AC Verification on 2026-10-06. Both passages are dated and were true when written, but no dated note records Story 27.22's 2026-10-06 completion. Story 27.4 cannot edit `epics.md`: it is a protected path in the A41 close-out set and outside this spec's scope. Owner: Epic 27 planning sync, the same owner as the `sprint-status.yaml` epic-27 reason entry above (Story 27.22 completion sync, Deployment Adapter Developer). Consequence: a reader can mistake the dated text for the current C1.16 state. Reopen trigger: the next approved Epic 27 planning correction adds a dated note recording Story 27.22's completion, its captured-window-only acceptance and its retained canonical single-session limitation, with no Story 27.4 or A41 credit.

## Deferred from: patch-application review of spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md (2026-10-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: The `b7a4377a` recompile of `epic-27-context.md` also dropped content that the existing Technical Decisions deferral does not list. The losses are: Requirements scope "Search, ingestion, mutation, and rejected access" (now "search/access operations"), the "malformed" tenant-scope fail-closed case, startup qualification/configuration fail-closed, the short-expiry-window crossing, the PostgreSQL pod/process zero-loss bound, and the Dapr-only lifecycle sentences.
  evidence: `git diff ed4528d5 -- _bmad-output/implementation-artifacts/epic-27-context.md` shows each removed line. `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` still carries the decisions (for example `:133`, `:166`, `:235`). bmad-build step-01 loads a valid epic context instead of the raw planning docs, so agents lose them. This is an agent-context file, so the fix is deferred. Settle it by recompiling or diffing against the ADR and architecture spine together with the earlier Technical Decisions deferral.
- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: `tools/check-story-review-readiness.py:83` `VALID_STATUSES` lacks bmad-build's `in-review` status, so a bmad-build spec under review fails C3 and skips the C1 File List comparison.
  evidence: 2026-10-06: the `--story-key spec-27-4-…-close-out-4 --changed-files-file <21 paths>` run exited 1 with `C3: status 'in-review' is not one of backlog, ready-for-dev, in-progress, review, done.` and `C1: status 'in-review' is outside {in-progress, review}; cumulative-diff comparison skipped.` The same bytes with `in-progress` pass. This is a pre-existing vocabulary mismatch between bmad-build (draft/ready-for-dev/in-progress/in-review/done) and the gate. Owner: BMAD process maintainer.
- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: The Story 27.22 deferred-work entries added in `f5beeed5`/`8b9d87ab`/`b969cf32` are missing spaces, with run-together tokens such as "Story27.4", "Story27.22", "exit0" and "identity20/20". A search for "Story 27.4" therefore misses their no-credit disclaimers.
  evidence: `grep -c 'Story27\.' _bmad-output/implementation-artifacts/deferred-work.md` returns 6 (2026-10-06). The text is pre-existing Story 27.22 ledger text that Story 27.4 must not modify. Owner: Story 27.22 owner (Deployment Adapter Developer).

## Deferred from: offline resumption review of spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md (2026-10-07)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: Deployment Adapter Developer owns separately scoped correction or retirement of the canonical C1.16 EOF-sensitive HTTP transport before relying on it for a future capture.
  evidence: B1: tools/verify-access-telemetry-c1-linkage.ps1:315 pipes completed printf output into BusyBox nc, while Story 27.22's dated actual diagnosis records request cancellation and a reviewed external transport correction. This predates the Story 27.4 resumption. The accepted captured window used separately reviewed external transport, not a repaired canonical packet. Reopen with approved permanent transport scope and executed actual-runtime native-exit/HTTP framing regressions; preserve historical refused packets and neutral gate fields.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: Deployment Adapter Developer owns binding the disposable SQL-contract lane to its authorized local Docker endpoint before resource creation.
  evidence: B4: tests/tooling/access_telemetry_c1_sql_contract/linkage_sql_contract_test.py:383 checks daemon OS/architecture but invokes ambient Docker context/environment without an endpoint binding, so remote DOCKER_HOST/context can receive the lane's owned resources. This pre-existing Story 27.22 harness issue was reviewed read-only; no remote daemon was contacted. Reopen with an explicitly authorized endpoint contract, refusal before creation for another endpoint, and owned-resource cleanup regressions.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: Deployment Adapter Developer owns asserting deployment image identity in the real SQL-contract lane's manifest contract.
  evidence: B5: an isolated temporary manifest changed to postgres:18.7 with an unapproved digest still passed assert_manifest_contract, while actual SQL execution uses the hardcoded approved 18.6 child. This pre-existing Story 27.22 test contract can validate a changed deployment against the old image. Reopen with exact approved deployment/index/child identity checks and executed image-drift refusal cases; no tracked deployment bytes changed.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: Deployment Adapter Developer owns enrolling the existing real PostgreSQL SQL-contract assertions in an automated verification lane with their pinned prerequisites.
  evidence: V1: .github/workflows/ci.yml:297 discovers access_telemetry_c1 only; its fake transport manufactures normalized clientAddr values. The real valid-session and old-inet-projection regression tests are in the separate manual access_telemetry_c1_sql_contract lane. A restored a.client_addr::text projection can pass CI fixtures while refusing valid actual IPv4 sessions. This predates the Story 27.4 resumption. Reopen with automated isolated discovery and documented Docker/PowerShell/OpenSSL/pinned-image setup, proving the real projection mutation fails the gate.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md`
  summary: Deployment Adapter Developer owns a regression proving C1.16 source drift after target observation blocks positive publication.
  evidence: V2: linkage_test.py's existing drift fixtures change sources before contact, while the SQL-contract lane imports selected functions and never runs the final collector publication path. Removing the final Assert-LinkageSourcesStable call at tools/verify-access-telemetry-c1-linkage.ps1:885 is not caught by those tests. This predates the Story 27.4 resumption. Reopen with a copied-input mutation after an observed target call that asserts the drift blocker, null observations and neutral gate fields.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-5.md`
  summary: Verify Memories gateway composition adopts the workload issuer client credentials required by the already committed EventStore dependency advance.
  evidence: V1 (medium, pre-existing): EventStore Server/Configuration/ServiceCollectionExtensions.cs:88 requires workload issuer client credentials in authority mode; the EventStore AppHost supplies them at Program.cs:338, but src/Hexalith.Memories.AppHost/Program.cs:289 does not. EventStoreAppHostResourceGraphTests.cs:52 checks the model with Keycloak disabled without authentication assertions; its gateway startup test at :145 is skipped, and AspireIngestionPipelineFixture.cs:1264 substitutes an in-memory command store. Consequence: authority-backed gateway startup can fail issuer options validation while composition tests pass. Owner: AppHost/EventStore integration maintainers. Reopen with a Memories authority-backed model test asserting required workload client credentials and executed gateway startup proof. The gitlink advance predates this handoff run; no dependency or gateway source changed here.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg2-c1-p1-authenticated-bootstrap-and-receipts-2.md`
  summary: Bind any future P1 consumer verdict to the immutable receipt, status and subject bytes or their verified digests.
  evidence: P1ReceiptVerifier returns only bool, while P1SignedDocument exposes mutable byte arrays. A caller can mutate retained arrays after a true offline verdict and later associate it with different bytes. The existing contract requires immutable retention, and no operational adapter exists; settle the returned evidence shape with that adapter rather than changing the current pure verifier API.
  status: resolved 2026-10-09
  resolution: `spec-pg2-c1-p1-enrollment-receipt-transport.md` — `P1ReceiptVerifier.TryVerify` returns immutable digests of the exact receipt/status payload and signature snapshots plus subject bytes. The retained-byte comparison and operational custody adapter remain separate unimplemented prerequisites; this resolution covers only the offline evidence shape.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg2-c1-p1-authenticated-bootstrap-and-receipts-2.md`
  summary: Align the P1 HTTP response-body size cap with the maximum signed payload and envelope size before implementing transport.
  evidence: The contract permits a 1 MiB receipt/status payload but also limits HTTP response bodies to 1 MiB. The specified transport adds a four-byte length and 64-byte detached signature, so a maximal valid payload needs a 1 MiB plus 68-byte body. This predates the follow-up and no HTTP adapter exists yet.
  status: resolved 2026-10-09
  resolution: `spec-pg2-c1-p1-enrollment-receipt-transport.md` — The approved total response-body cap remains 1,048,576 bytes; the new envelope decoder caps transport payloads at 1,048,508 bytes and the contract now states the distinction from the larger pure-verifier payload limit. No live HTTP adapter is claimed.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg2-c1-p1-enrollment-receipt-transport.md`
  summary: Validate the P1 receipt and status signed wire formats against independent protocol vectors before deploying an issuer.
  evidence: The existing receipt/status encoder and decoder share implementation assumptions; the new transport tests use that encoder for signed fixture payloads. A separately authored canonical byte vector would detect shared encoding drift. This predates the offline transport boundary and requires an issuer interoperability fixture or independent vector author.

- source_spec: `_bmad-output/implementation-artifacts/spec-pg2-c1-p1-enrollment-receipt-transport.md`
  summary: Reject a changed validly signed P1 response under an already observed receipt ID using authenticated immutable custody history.
  evidence: The pure P1 verifier accepts any valid issuer signature matching the exact claims, including a second signature over the same payload. The new evidence object exposes distinct signature digests, but only an independently approved live retrieval and custody adapter can compare the current body with a previously retained body for the same receipt ID and raise an incident. No operational issuer, custody root or live adapter is enrolled.

- source_spec: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/spec-pg-onprem-2-c1-p1-http-retrieval-and-status.md`
  summary: Add isolated production server TLS tests for successful system trust and refusal when offline CRL evidence is absent.
  evidence: The local-root server trap refuses under system trust regardless of server CRL policy; an isolated system-trusted root and current offline CRL fixture is unavailable here, so a server-side NoCheck regression would not fail existing tests.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md`
  summary: Restore stable Aspire fixture startup for token-free sidecar and authenticated HTTP graph checks.
  evidence: Review B1/B2 (medium, pre-existing). The token-free method timed out after 65 seconds with test JWT settings and xUnit Total: 0; the HTTP graph test also stopped before its body after 120 seconds. The real-FalkorDB graph boundary test passed, but endpoint graph choice and default-mode sidecar behavior remain unverified locally. Reopen when fixture startup can complete both methods with bounded runs.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md`
  summary: Add live non-health-route denial checks for absent and wrong APP_API_TOKEN values.
  evidence: Review B5 (medium, pre-existing). The fixture now sets the valid token on direct clients in token mode, while only middleware unit tests cover invalid app tokens. Reopen with an enabled-mode direct application request that proves denial before protected endpoint work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md`
  summary: Enroll the enabled Dapr sidecar token test in a routine verification lane.
  evidence: Review V1 (medium, pre-existing). CI and nightly run integration filters without DAPR_API_TOKEN_MODE=enabled, so neither the prior enabled branch nor the new missing/wrong/valid token assertions execute routinely. Reopen with a focused token-enabled CI invocation and its fixture prerequisites.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md`
  summary: Make direct MCP integration transports usable and verifiable with APP_API_TOKEN enabled.
  evidence: Review V3 (medium, pre-existing). McpServerIntegrationTests and McpAuthenticationIntegrationTests construct independent HttpClientTransport headers with bearer authorization but no dapr-api-token. In token mode their requests can receive app-token 401 before bearer or tool assertions. Reopen with header propagation and one enabled-mode MCP request.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-live-tenant-isolation-evidence.md`
  summary: Verify that the routine integration lane supplies the test JWT settings needed for Aspire fixture startup.
  evidence: Blind review B1 (medium if reproduced, pre-existing). `.github/workflows/ci.yml` invokes `tools/test.sh` without the test JWT environment used by the focused Story 5.4 runs; the earlier local env-free attempt failed EventStore gateway startup before any test body. A CI-equivalent run with those variables unset, or a workflow inspection showing another source of equivalent settings, would settle whether the routine lane is affected. Owner: CI/AppHost integration maintainers. Reopen when qualifying the routine integration lane.

### DW-736: Add live authenticated HTTP proof that tenant A's bearer cannot traverse tenant B's graph route.

origin: migrated from legacy ledger on 2026-11-24
location: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
source_spec: _bmad-output/implementation-artifacts/spec-5-4-live-tenant-isolation-evidence.md
reason: Blind review B2 (medium, pre-existing). The executed two-tenant graph method mints a bearer matching each route, while the focused in-memory endpoint theory proves A-to-B denial before graph dependencies but does not exercise the live Aspire transport. Owner: tenant authorization integration tests. Reopen with an A-bearer-to-B-route request that returns 403 `TENANT_FORBIDDEN` and no foreign response data.
status: done 2026-11-24
resolution: The live two-tenant collision proof obtains both graphs with their own tokens, then sends tenant A’s bearer to the tenant B route and asserts 403 TENANT_FORBIDDEN without either foreign graph marker. See spec-ten-deferred-tenant-isolation-evidence.md.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-story-scope.md`
  summary: Support Setext parent and sibling scope boundaries with correct Markdown block context if that authoring syntax is adopted.
  evidence: Independent review confirmed the pre-existing ATX-only parser treats an underlined Setext title after an allowed list as ordinary content and can collect later bullets. Baseline and current implementations share this unsupported-syntax gap; CONTRIBUTING.md now requires ATX scope headings explicitly. A trial lookahead was withdrawn after review demonstrated false boundaries at thematic breaks and list continuations, plus incorrect handling of ordered markers within paragraphs. Correct support requires paragraph/list indentation context or an approved Markdown parser dependency. Owner: tooling maintainer. Reopen when Setext scope headings are proposed or found in authored artifacts; qualify both real headings and thematic-break, lazy-continuation, indented-list and ordered-marker controls before enabling support.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-commit-validation.md`
  summary: Reject empty repository commit messages permitted by explicit Git --allow-empty-message, including in history validation.
  evidence: Independent review and a disposable real-hook reproduction both created an empty-subject commit with exit 0 using git commit --allow-empty-message --file <empty-file>. The pinned commitlint CLI filters whitespace-only repository messages before applying rules (node_modules/@commitlint/cli/lib/cli.js:222-225), independently of defaultIgnores; this predates the five selected fixes. Ordinary Git rejects empty messages by default. Resolve through both the local hook and shared CI validation, without treating skipped empty messages as validated subjects.

## Deferred from: review of spec-five-deferred-release-preflight.md (2026-10-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-release-preflight.md`
  summary: Validate release versions consistently against the complete SemVer grammar in direct input and both semantic-release terminal marker forms.
  evidence: Independent disposable-repository execution confirms the grammar predates this change: baseline 46692dd0f6c7cd780608ce74fd827d81c481ae6d and changed tools/release-preflight.ps1 both exit 0 for invalid 01.0.0 and exit 1 for valid 1.0.0-beta.1+build.1 through ordinary next-version records and direct -NextVersion. The alternate first-release marker inherits the same grammar. Owner: release maintainer. Consequence: malformed version tags can pass preflight and valid prerelease-plus-build versions are refused. Reopen trigger: a version-parser hardening change or a release using these boundary forms.

## Deferred from: review of spec-five-deferred-preflight-harness.md (2026-10-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-preflight-harness.md`
  summary: Enroll the release-preflight fixture suite in pull-request validation before changes reach main.
  evidence: Independent review confirmed .github/workflows/release.yml:118 runs the suite only in its main-push workflow, while ci.yml has no release_preflight discovery step. This predates DW-61 through DW-65 corrections; their executed focused suite is local evidence only. Owner: CI/release maintainer. Reopen with a focused PR lane that supplies Git and PowerShell and proves a deliberately failing fixture blocks PR validation; retain an explicit missing-prerequisite policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-preflight-harness.md`
  summary: Add routine Python 3.9 module/annotation and harness verification for the preflight harness minimum runtime.
  evidence: Actual CPython 3.9.25 collection, typing.get_type_hints and all nine helper cases pass locally after the Optional correction; the exact command is documented in tests/README.md. Restoring Python 3.10 union annotations still passes helper checks on Python 3.14, and the existing release workflow uses only its runner-default interpreter. Owner: CI/release maintainer. Reopen with a maintained lower-runtime lane that rejects the baseline union-annotation mutation while accepting current source; this entry concerns recurring qualification rather than the already executed compatibility proof.


## Deferred from: Story 27.4 S3 integration final review (2026-10-29)

> 2026-11-01: parked under DW-730 by the approved Story 27.4 scope reset; S3 calibration admission is withdrawn work. Revisit only if DW-730 reopens.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
  summary: Medium-unverified: measure actual writer/validator credential-to-IAM-principal bindings before S3 calibration admission.
  evidence: B5; owner Jérôme Piquot / Platform Operations. Current outside-approved plan pins declared principal IDs and credential digests, but implementation/tests did not inspect real credentials or native IAM ownership. Distinct access-key pairs alone do not establish distinct principals. Reopen with current native, source-bound principal/scope evidence for both actual credential digests before admission; no independent identity, live qualification or Production credit is granted.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
  summary: Medium-unverified: measure the selected Scaleway bucket's actual owning project before S3 calibration admission.
  evidence: B6; owner Jérôme Piquot / Platform Operations. Fixed endpoint/bucket/project and signed provider reservation bind owner assertions; x-amz-meta-project-id proves metadata round-trip only. No native current bucket-owner/project observation was executed. Reopen with authoritative current bucket/project evidence bound to the actual destination and reservation before admission; no ownership, live qualification or A41 closure is inferred.


## Deferred from: Story 27.4 workspace package-boundary verification (2026-10-30)

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
  summary: Add a genuinely owner-only package fixture to the shared source-package boundary verification.
  evidence: Review B2 verified the existing ByteAether.Ulid assertion predates this correction and shared Builds already declares that package; it cannot prove preservation of an owner-only addition. This pre-existing coverage gap can miss a future regression dropping an owner-specific package. Current resolver correction and 92-case result remain valid; no package loss was observed. Owner: Platform qualification maintainers; reopen when strengthening source-packages.props coverage.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-14.md`
  summary: Assert exact single-occurrence cryptography catalogue pins in the Commons source-package boundary evaluation.
  evidence: Review B3 verified both original and corrected tests apply pin assertions to Platform composition, while Commons evaluation checks only ByteAether.Ulid presence. This pre-existing coverage gap could miss Commons-specific pin removal or duplication; no current incorrect version was demonstrated. Owner: Platform qualification maintainers; reopen with the Commons boundary test-hardening change.

## Deferred from: Epic 27 retro item 62 Dapr app-token Production guard review (2026-11-01)

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-62-dapr-app-token-production-guard.md`
  summary: Deployed non-Production environments, such as the Qualification overlay, still honor `Hexalith:Dapr:ApplicationTokenOverride`, and nothing requires the Dapr tokens at their startup.
  evidence: Blind Hunter review. `deploy/kubernetes/overlays/qualification/qualification-enabled-patch.yaml:14,35,48` sets `DOTNET_ENVIRONMENT=Qualification` for `memories`, `memories-access-telemetry` and `memories-access-telemetry-clock`, which carry `APP_API_TOKEN`. Retro item 62 scoped the guard to Production only. There the override adds no capability beyond unsetting `APP_API_TOKEN`, because `DaprTokenStartupValidator` validates only Production. The gap is pre-existing and not caused by this change. Hardening options: honor the override only in Development; require both tokens and reject the override in Qualification; log a warning at construction when the override is honored; add a deployment-artifacts test asserting no `deploy/` manifest sets the key. Related: `deferred-work.md:5574`. Owner: Memories security maintainers. Reopen when a deployed non-Production environment must fail closed like Production.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-62-dapr-app-token-production-guard.md`
  summary: The planned empty-override overlay for `TelemetryWebAppFactory` (`deferred-work.md:5570`) would make that factory's derived Production host fail startup.
  evidence: `AccessTelemetryQualificationWorkloadTests.Program_MapsTheQualificationRouteOnlyInTheQualificationEnvironment` (`:574`) derives a Production host from `TelemetryWebAppFactory` (`:602`). `RemoveInfrastructureHostedServices` keeps `DaprTokenStartupValidator`, which now fails Production startup while the override holds a value. Apply the overlay only to non-Production hosts derived from the factory. This change verified the leak itself as pre-existing: with `APP_API_TOKEN=leaked-process-token`, 5 `ServerEndpointRateLimitTests` cases return 401 because `TelemetryWebAppFactory` never set the override. Owner: Memories test maintainers. Reopen with the `deferred-work.md:5570` fix.

## Deferred from: Epic 27 retro items 63 and 64 inert disabled-lifecycle change (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: The parked access-telemetry C1 (`tests/tooling/access_telemetry_c1`), C1 interchange (`tests/tooling/access_telemetry_c1_interchange`) and lifecycle (`tests/tooling/access_telemetry_lifecycle`) tooling suites no longer run in PR CI or in any other workflow.
  evidence: Owner answer, 2026-11-01, recorded in the spec. The inert Production overlay changes `deploy/kubernetes/overlays/production/access-telemetry-disabled-patch.yaml` and `kustomization.yaml`, which are SHA-256 inputs of the approved PG-ONPREM-2 profile (`tools/access-telemetry-c1-profile.ps1`, `tools/verify-access-telemetry-c1-linkage.ps1`, `tools/verify_access_telemetry_lifecycle.py`). The pins, the profile identity and the profile hashes were deliberately left unchanged. With both files changed, `Assert-C1ApprovedProfileInputs` refuses with `approved-profile-input-drift`; with both restored to `e8956eb5` it passes. The three discover steps left `.github/workflows/ci.yml`, their mandatory commands left `tests/tooling/ci_alignment/ci_alignment_test.py`, and `CiTestInventoryTests.ParkedToolingSuites` requires each parked suite to exist and to stay out of every workflow. The other access-telemetry lanes stay wired and pass after the overlay change: owner discovery 31 tests, functional retention 162, C1 SQL contract 19. The setup steps "Build source Debug descriptor reader" and "Initialize root Platform review-reader dependency" stay for retro item 68 to judge. DW-17 control (2) still cites `tests/tooling/access_telemetry_c1`; the Production disable pins now run in `ProductionDeploymentArtifactsTests` (unit-contract) and `AccessTelemetryLifecycleIntegrationCheckpointTests` (integration-fast). Owner: Jérôme Piquot. Reopen with DW-730: re-plan or retire the suites against the real Production target, then re-wire them and drop them from `ParkedToolingSuites`. Reopening also requires reverting the Production overlay's JSON6902 scope and Role removals, the StatefulSet scale and PDB delete, and the verifier's out-of-scope denial and `can-i` checks.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: Applying the inert Production overlay to the live `jpiquot@local` cluster, and deciding the fate of the lifecycle PostgreSQL PVC, are operator actions that this change did not perform.
  evidence: Read-only `kubectl get` recorded in the spec on 2026-11-01: namespace `hexalith-memories` carries `disabled-pending-story-27-3`, `ACCESS_TELEMETRY_ENABLED=false` and hash `unproven`; `access-telemetry-postgresql` runs 1/1 (104 days) and its PDB allows 0 disruptions; `data-access-telemetry-postgresql-0` is a bound 400Gi PVC on `openebs-hostpath-retain`; `memories-access-telemetry` runs 1/1, drifted from the overlay's 0. No mutating `kubectl` command was run. On the next apply of the release render, the StatefulSet drains to 0 and keeps the Retain PVC, the two components drop the `memories` scope and the Server Role drops the bootstrap secret. `kubectl apply` does not prune, so the old PDB stays; it selects no pods once the StatefulSet is drained and cannot block node drains. Dapr HotReload is off and the Server pod template does not change, so Server pods drop the two components only after a restart. The apply also sets the drifted lifecycle Deployment back to the rendered 0. A client-side apply patches every rendered field whose live value differs, and a server-side apply reports a conflict if another field manager owns `replicas`. The cause of the drift is unknown, so confirm the replica count after the apply. Deleting the PVC data is irreversible. Owner: Jérôme Piquot (operator). Reopen trigger: the next Production apply or operator change window on `jpiquot@local`.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: The DW-17 accepted-debt record cites CI controls that this change moved: compensating control (2) names `tests/tooling/access_telemetry_c1` (`ci.yml:388`), which no workflow runs now, and its `ci.yml:421` and `ci.yml:545` line anchors shifted when the parked steps left `test-unit-contract`.
  evidence: Review finding BH1 (medium). The intent reserves DW-17 for the next prompt (retro items 65/66), so this change left it untouched. The Production disable pins now run in `ProductionDeploymentArtifactsTests.ProductionOverlay_DisabledLifecycleIsInert` (`ACCESS_TELEMETRY_ENABLED: "false"`, hash `unproven`, three zero-replica workloads) in test-unit-contract, and in `AccessTelemetryLifecycleIntegrationCheckpointTests.ProductionOverlay_RequiresUnprovenProfileToRemainDisabledAndLeavesPhysicalProofPending` in integration-fast. Re-derive every `ci.yml` anchor before citing it. Owner: Jérôme Piquot. Reopen with the DW-17 correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: Sprint-status retro items 65 and 68 describe a state this change overtook: item 65 says only the lifecycle Deployments, not the PostgreSQL StatefulSet, run at zero replicas, and item 68 plans to move the parked lanes to nightly or path-filtered runs that would now refuse with `approved-profile-input-drift`.
  evidence: Review finding BH2 (medium). The intent records the fix against items 63 and 64 only and forbids changing other retro items, so both entries stay as written. Since this change, the Production render runs `access-telemetry-postgresql` at 0 replicas with no PDB. The three drifted lanes run in no workflow (`CiTestInventoryTests.ParkedToolingSuites`), and the two setup steps that served them stay in `ci.yml` with comments deferring to item 68. Owner: Jérôme Piquot. Reopen when item 65 or item 68 is executed.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: Parking the whole `access_telemetry_c1_interchange` lane stops 636 passing tests, including direct tests of helpers that the still-wired functional-retention tool imports, to silence 6 drift-sensitive subtests.
  evidence: Review findings VG1 and BH10 (medium). The Verification Gap layer ran the suite on the patched tree: 642 tests, FAILED (failures=6). All 6 are subtests of `test_capture_streams.py:689` and `test_capture_commands.py:373`, refusing with `approved-profile-input-drift`. `tools/access_telemetry_functional_retention.py:35-36` imports `JsonSnapshot`, `InterchangeFormatError` and owner-dispatch helpers whose direct tests (`test_wire_primitives.py`, `test_owner_dispatch.py`) now run in no workflow. The owner chose lane-level parking with the 6-subtest count in hand. Narrowing it is a retro item 68 or owner decision: re-wire the lane with the two drift-dependent methods skipped citing DW-730, or add an import-only check. Owner: Jérôme Piquot.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-27-retro-63-64-inert-disabled-lifecycle.md`
  summary: While the lifecycle is disabled, the Server Role `memories-dapr-secret-reader` still grants `get` on the Kubernetes Secret `access-telemetry-marker-key`, and `memories-config` still allow-lists that name for `access-telemetry-secrets`; neither grant is used by any component the Server loads.
  evidence: Review finding EC5 (low; pre-existing). The intent limited this change to removing the Server's grant on `openbao-access-telemetry-bootstrap`. In the Production render, no component scoped to `memories` references `access-telemetry-marker-key`, and the marker itself is read from OpenBao through `access-telemetry-secrets`, which the Server no longer loads. Removing both is one more JSON6902 test+remove pair each (`/rules/0/resourceNames/6`, `/spec/secrets/scopes/1`) plus render assertions. Owner: Jérôme Piquot. Reopen with the DW-730 pre-enablement register (retro item 67) or the next Production least-privilege pass.

## Deferred from: Story 30.3 review — Production protection readback (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Resolved 2026-11-03 — production settings and protected reviewer approval independently verified in authorized Release run 41624526890.
  evidence: Review finding Blind 2 (high; external prerequisite). On 2026-11-02, `gh api repos/Hexalith/Hexalith.Memories/environments/production` returned `protection_rules: []` and `deployment_branch_policy: null`. The new source job fails closed on that state. The approved Story 30.3 intent excludes GitHub settings changes; a repository administrator must install the reviewer rule and verify it by API readback before a live release can be accepted. Update 2026-11-03 (code review round 2): commit `66822bc5` added a one-run owner bypass of both in-workflow readbacks, Release run `41495447050` published `v3.0.18` through it, and `d9d61d8a` removed it. The protection settings are unchanged, and no environment approval has been exercised. Owner: Jérôme Piquot (repository administrator). Consequence: a guarded dispatch at the current tip cannot enter the protected job. Reopen trigger: the administrator configures reviewers, disables admin bypass and restricts deployment branches, records an API readback, and the next dispatch's run approvals readback shows an approval. Update 2026-11-03, 10:34 UTC (explicit settings authorization): the user authorized required reviewer jpiquot, disabled admin bypass and main-only deployments. PUT environment and POST deployment-branch-policy both succeeded. GET environment confirms required reviewer `jpiquot` (`6775094`), `can_admins_bypass: false` and custom branch policies enabled; GET deployment-branch-policies confirms exactly one policy, branch `main` (`65337355`). The live workflow protection predicate exits 0. The settings portion is complete. Remaining consequence: no post-configuration run has exercised protected approval. Remaining reopen trigger: the next intentionally authorized dispatch records an approval in its run approvals readback. Owner remains Jérôme Piquot; this authorization covered settings only. Resolution 2026-11-03, 11:12 UTC: the user separately authorized one guarded dispatch (may publish). Exact-source push CI `41621265465` succeeded for `d7e44d01b66f0800bdb7edca8553934fe44dc145`; Release run `41624526890` passed source/protection proof and waited for production. The configured reviewer `jpiquot` submitted approval on the user's behalf through the normal pending-deployments API. Independent approvals GET records `state: approved`, user `jpiquot` (`6775094`), production environment `24712111554`, and the explicit authorization comment; pending deployments become empty. This exercised the approval gate without bypass or settings changes. The checkpoint and this deferred action are resolved; the publication flag remains unset.

## Deferred from: Story 30.3 review — Current five-image recovery deployment shape (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Make current five-image recovery accept the real four-image deployment asset without treating DomainService as a deployed workload.
  evidence: Round 2 Blind 1 and 2 (high). `publish-containers.ps1` publishes DomainService but has no render parameter for it; `complete-partial-release.ps1` requires every published image in the deployment, and the fixture invents a DomainService reference. Story 30.5 owns current-set recovery and should use the actual renderer output. Owner: Story 30.5 release maintainer. Consequence: current five-image releases cannot be recovered by this workflow. Reopen trigger: Story 30.5 implements the actual renderer deployment shape.
  status: resolved 2026-11-15
  resolution: Story 30.5 — The retained deployment is checked against the four hosted mappings only; DomainService remains a fifth publication/health member. `PartialReleaseRecoveryTests.test_all_missing_positive_control_verifies_fifteen_and_thirteen_assets` uses the actual four-workload shape and passes. Offline fixtures provide no live NuGet or registry proof.

## Deferred from: Story 30.3 review — NuGet duplicate after absence probe (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Reject a NuGet duplicate response that races the destination absence probe instead of recording it as a successful push.
  evidence: Round 2 Blind 5 (high). The pre-existing `publish-nuget.ps1` calls `dotnet nuget push --skip-duplicate` and records zero exit as pushed. A package uploaded by another writer after the probe can therefore produce false complete-release evidence. Story 30.5 must reconcile occupied immutable destinations and package identity. Owner: Story 30.5 release maintainer. Consequence: a raced HTTP 409 can be misreported as a successful package push. Reopen trigger: Story 30.5 reconciles immutable NuGet identities and removes false-success handling.
  status: open
  handoff: Story 30.6 (owner decision D3, 2026-11-15). Release-path `publish-nuget.ps1 --skip-duplicate` remains open. Recovery independently compares retained ZIP entries, omits `--skip-duplicate`, and fails on HTTP 409 (`test_409_push_refuses_without_release_completion_or_success_credit`); that is no release-path false-success fix.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `nuget-duplicate` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


## Deferred from: Story 30.3 review — Recovery publication approval (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Put partial-release recovery publication behind an authorized production approval gate.
  evidence: Round 2 Blind 6 (high). `recover-partial-release.yml` can publish missing images and create a GitHub Release without `environment: production`; this route predates Story 30.3. Story 30.5 owns recovery authorization. Owner: Story 30.5 release maintainer. Consequence: recovery publication lacks the guarded release approval gate. Reopen trigger: Story 30.5 adds production approval to recovery.
  status: resolved 2026-11-15
  resolution: Story 30.5 — `recover-partial-release.yml` now has read-only `release-source` and `recover` under `environment: production`, with protection readback before/after approval. `CiTestInventoryTests.PartialReleaseRecovery_UsesTrustedTagAndCompletesWholeReleaseUnit` passes (141/141 class cases); no settings or live release were changed. Offline fixtures provide no live NuGet or registry proof.

## Deferred from: Story 30.3 review — Recovery exact-source CI proof (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Bind recovery tooling to a reviewed source and exact successful push CI before it can publish.
  evidence: Round 2 Blind 7 (high). Recovery copies executable tools from its dispatched main checkout before proving that checkout is the live main tip or has successful exact-source push CI. Story 30.5 owns the recovery source/version proof. Owner: Story 30.5 release maintainer. Consequence: recovery tooling can run without exact-source CI proof. Reopen trigger: Story 30.5 binds and proves the recovery source.
  status: resolved 2026-11-15
  resolution: Story 30.5 — Both jobs call `check-release-source.py` against live main and exact push CI before staging/using tooling. Failed-run head, tag source, version, manifests, frozen set and approved helper hashes are then checked offline. Drift fixtures and CI inventory checks pass. Offline fixtures provide no live NuGet or registry proof.

## Deferred from: Story 30.3 review — Historical recovery registry scope (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Scope registry write checks for historical recovery to repositories present in the tagged release.
  evidence: Round 2 Blind 8 (medium). `recover-partial-release.yml` checks the verifier's current five repositories before checkout of a historical two- or four-image tag, so lost access to an unused newer repository blocks old recovery. Story 30.5 owns the tagged-set contract. Owner: Story 30.5 release maintainer. Consequence: a historical recovery can be blocked by an unused current repository. Reopen trigger: Story 30.5 scopes registry checks to the tagged set.
  status: resolved 2026-11-15
  resolution: Story 30.5 — Historical inventory validation derives repositories from the tagged project set before `verify-container-registry.ps1 -Repositories`; current recovery probes only the frozen five-image identity. Historical two/four-image completion fixtures remain required. Offline fixtures provide no live NuGet or registry proof.

## Deferred from: Story 30.3 review — Five-image project mapping identity (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Qualify the five image project-to-repository mappings as a single publication set.
  evidence: Round 2 Blind 9 (medium). The destination preflight checks the five repository names but cannot detect a project remap that keeps those names; the current publisher owns project mappings. Story 30.4 explicitly owns their verification. Owner: Story 30.4 release maintainer. Consequence: repository-name absence alone cannot detect a project remap. Reopen trigger: Story 30.4 qualifies each project-to-repository mapping.
  status: resolved 2026-11-03
  resolution: `spec-30-4-five-image-publication-contract.md` — `tools/release-containers.json` and `tools/container-publication-contract.py` require the exact five project-to-repository mappings and an exact recursive `EnableContainer` project inventory, then freeze them into the hashed release-set identity before any SDK build, preflight write or transport. A missing, added, nested or remapped project refuses (`test_manifest_requires_exact_five_mappings_health_platforms_and_approved_sha`, `test_missing_added_or_remapped_project_is_refused_before_sdk_or_transport`, `test_nested_container_project_is_refused`, `test_repository_swap_and_msbuild_true_project_are_refused` (a two-project repository swap that keeps all five names), `test_frozen_identity_rejects_mapping_version_run_and_digest_changes`; container suite 89/89). This is offline enforcement only; the live "Five mappings" checkpoint stays blocked in the Story 30.4 spec until an authorized credentialed run retains `identity.json`.

## Deferred from: Story 30.3 review — Complete current image set for recovery (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Require exactly the approved five image members when completing a ten-package current release.
  evidence: Round 2 Edge 1 (high). The existing recovery guard requires DomainService for ten packages but accepts a four-member summary omitting the telemetry clock, then can close the incident. Story 30.5 owns full current-set recovery. Owner: Story 30.5 release maintainer. Consequence: current-set completion is unsupported and remains fail-closed. Reopen trigger: Story 30.5 verifies all five images with the ten-package unit.
  status: resolved 2026-11-15
  resolution: Story 30.5 — The offline bundle verifier requires the exact frozen/tagged ten-package and five-image mappings and stores all fifteen classifications before writes. Complete-set, occupied-content and identity-drift fixtures pass. Offline fixtures provide no live NuGet or registry proof.

## Deferred from: code review of spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Regenerate `epic-30-context.md` from `epics.md`; the Story 30.3 rewrite (commit `637eaa1b`) dropped binding constraints that later stories read as loaded context.
  evidence: Review findings HSG-F6, BH10, AA9 (medium). Removed or weakened: the Zot challenge-response 401 re-check before calling publication green, "one immutable Builds revision", never `@main`, `cancel-in-progress: false`, registry push-blocker owner/consequence/trigger, "remove duplicate work only after equivalence is proven", and the Story 30.4/30.6 runbook section ownership. `epics.md` still carries them. Deferred because the fix edits an agent-context file. Owner: Jérôme Piquot. Reopen trigger: the next Epic 30 story (30.4) is created or its context is compiled.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: The pinned NuGet absence probe is eventually consistent, and `--skip-duplicate` records HTTP 409 as pushed; together they leave a window where an occupied version reads as absent and then as successfully published.
  evidence: Review findings BH17 and AA10b (pre-existing). The pinned helper probes `api.nuget.org/v3-flatcontainer`, which returns 404 while a pushed package is still validating, and `test_post_probe_duplicate_response_does_not_create_failure_summary` formalizes the existing 409-as-pushed behaviour. This extends the Round 2 Blind 5 entry above rather than duplicating it. Owner: Jérôme Piquot. Reopen trigger: Story 30.5 implements immutable partial-release reconciliation.
  status: open
  handoff: Story 30.6 (owner decision D3, 2026-11-15). Release-path `publish-nuget.ps1 --skip-duplicate` remains open. Recovery independently compares retained ZIP entries, omits `--skip-duplicate`, and fails on HTTP 409 (`test_409_push_refuses_without_release_completion_or_success_credit`); that is no release-path false-success fix.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `nuget-eventual-consistency` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


## Deferred from: Story 30.3 final review — External version floor (2026-11-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Prove a proposed release version is newer than every stable version observed at the external package and image destinations.
  evidence: Final Blind 4 (medium; pre-existing publisher gap). `check-release-destinations.py` invokes the pinned helper's exact-version `validate_destination_absence` but never calls its `validate_version_floor` or `read_external_version_observations`. An unoccupied candidate can therefore pass when an external destination holds a newer stable version; the prior publisher lacked this floor as well. Owner: Story 30.6 release maintainer. Consequence: a stale version may publish if Git tags lag an external destination. Reopen trigger: Story 30.6 cutover parity validates the floor with the complete current set.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `version-floor` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


## Deferred from: code review of spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md (2026-11-03)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Align the container child summary's recovery text with the current-release boundary.
  evidence: Round 2 Verification Gap finding (low). `tools/publish-containers.ps1:277` still writes `dispatch Recover Partial Release from main` into `artifacts/containers/release/publish-summary.json`. Since Story 30.3, `recover-partial-release.yml` refuses every current ten-package release, while the aggregate summary (`publish-release.ps1`) and the PARTIAL PUBLISH issue already give the current guidance. No `publish_containers` test asserts the container `recovery` text. Deferred because `tools/publish-containers.ps1` is a Story 30.4-owned path, and the original text becomes correct once Story 30.5 lands complete-set recovery. Owner: Jérôme Piquot. Consequence: after a failed current-release image push, the uploaded container summary contradicts the aggregate summary. Reopen trigger: Story 30.4 edits `publish-containers.ps1`, or Story 30.5 enables current-set recovery, whichever comes first.
  status: resolved 2026-11-03
  resolution: `spec-30-4-five-image-publication-contract.md` — Story 30.4 edited `tools/publish-containers.ps1`, which fired this trigger. The current five-image path already wrote Story 30.5 guidance, but a deployment-render failure on a current tree still wrote the historical text. The script now picks its `recovery` text from whether `tools/release-containers.json` is present. Current trees get "Stop and reconcile immutable evidence with Jérôme Piquot; current missing-only recovery belongs to Story 30.5." Historical tagged trees keep the config-digest recovery dispatch. `test_render_failure_recovery_guidance_matches_the_source_tree_boundary` covers both trees. Reverting the fix fails only its current-tree case. Follow-up 2026-11-15 (Story 30.5): current guidance now names Recover Partial Release with version/run ID and retained-byte/production prerequisites; the same boundary fixture is realigned.

## Deferred from: Story 30.3 resumed build review (2026-11-03)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Validate a tagged recovery set before the first registry mutation.
  evidence: Round 3 Blind 2 (high; pre-existing). The recovery workflow invokes staged publish-containers.ps1 with -Push before validating the supported historical image/package counts; baseline ordering is identical. Current-set completion refusal does not prevent earlier image pushes. Owner: Story 30.5 recovery maintainer. Consequence: a mistakenly dispatched current tag can mutate the registry before unsupported-set rejection. Reopen trigger: Story 30.5 introduces tagged-set authorization and pre-publication validation.
  status: resolved 2026-11-15
  resolution: Story 30.5 — The historical workflow validates nine package IDs and a two/four-image tagged set before registry write-scope probes or `-Push`; precontract five/ten sets refuse. Current sets require a complete zero-refusal fifteen-member plan before any mutation. The unsupported-historical fixture and no-write recovery fixtures cover both boundaries. Offline fixtures provide no live NuGet or registry proof.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Recheck exact tag absence at the terminal preparation boundary or reconcile an identically bound concurrent tag explicitly.
  evidence: Round 3 Blind 5 (medium; pre-existing). Initial local/remote tag checks precede packing in both baseline and current workflows. Installed semantic-release pushes tags without force before publication, rejecting different tag objects, but Git accepts a matching lightweight tag created concurrently on the same source commit. Owner: Story 30.6 cutover maintainer. Consequence: an initial tag-absence observation can age during preparation. Reopen trigger: Story 30.6 validates terminal tag collision or immutable-tag reconciliation semantics.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `terminal-tag` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Retain exact prepared package bytes after partial publication failures.
  evidence: Round 3 Blind 7 (medium; pre-existing). Release package upload still requires overall success, as at baseline, while failure evidence uploads only summaries. Owner: Story 30.5 recovery maintainer. Consequence: missing/unpublished prepared package bytes can be lost when the runner disappears, complicating immutable reconciliation. Reopen trigger: Story 30.5 extends failed-release artifact retention.
  status: resolved 2026-11-15
  resolution: Story 30.5 — `pack-release.ps1` writes hash-bound `package-identity.json` before publication; release failure archives ten nupkgs and five OCI archives plus deployment/evidence into `release-recovery-bundle` (90 days requested, last recorded repository cap 30 days, compression 0; actual expiry is authoritative). Receipt/hash drift and missing-preparation fixtures pass; incomplete bundles record named unrecoverable reasons. Offline fixtures provide no live NuGet or registry proof.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-3-guarded-release-dispatch-and-shared-caller-adoption.md`
  summary: Include failed GitHub Release and asset completion in aggregate partial-release incident handling.
  evidence: Round 3 Blind 8 (medium; pre-existing). Both publisher families can succeed before the GitHub semantic-release plugin fails. The unchanged aggregate remains succeeded, and create-partial-publish-issue.ps1 skips statuses other than partial-publish. Owner: Story 30.5 recovery maintainer. Consequence: a release missing GitHub assets can fail without the expected partial-publish incident. Reopen trigger: Story 30.5 qualifies complete-unit publication and GitHub asset recovery.
  status: open
  handoff: Story 30.6 (owner decision D3, 2026-11-15). The publish-failed and GitHub-plugin incident alert remains open; Story 30.5 implements asset recovery but does not change alert eligibility.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `github-plugin-incident` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


## Deferred from: code review of spec-30-4-five-image-publication-contract.md (2026-11-03)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Verify server-side tag immutability for the current skopeo release transport before claiming race-free publication (unverified high impact).
  evidence: Review E5 identifies an absence-check/copy race if another writer can mutate a tag; actual deployed policy is unknown. Owner: Jérôme Piquot; consequence: no race-free registry credit; reopen trigger: authorized immutability-policy readback or an isolated competing-writer registry fixture. No registry setting or publication was changed.
  Follow-up 2026-11-10: authorized credentialed runs 42108808279 (v3.0.24) and 42134622308 (v3.0.25) published through skopeo with no immutability-policy readback and no competing writer, so this trigger has not fired. The entry stays open.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Reconcile the independently advanced dependency cohort with the approved CI qualification profile and source-build references.
  evidence: External commit 8c0b907e0a200164d2c6ee7a732daefb9b29546d advanced six gitlinks during Story 30.4. The parent dispatch run passed 16/17; test_release_pin_is_approved_and_profile_tracks_the_gitlink fails because Builds gitlink 725754075e1d6369430a3adf94d46fb71e6eaf9e differs from approved profile 6e7011fc754e74ac2a8864da1ce0b6d8f7178a48. The recorded source Debug build also has CS1704 for EventStore.Contracts 4.0.4 versus 3.132.2. Owner: Jérôme Piquot; consequence: broad qualification and current CI cannot receive green credit; reopen trigger: explicitly approved dependency/profile reconciliation followed by the recorded exact commands. Story 30.4 changes no dependency pin or approval identity.
  Follow-up 2026-11-03: external commit 3d6e466238fe7f78dc6216918f5e749f513cca9b restored the approved Builds gitlink. Aligning the clean checkout to the recorded 6e7011fc revision restores dispatch pin parity (17/17 passed). The broader source/cohort qualification portion remains open; the earlier pin mismatch is historical.
  Follow-up 2026-11-03 (re-dispatch): on clean HEAD 41f8bdca7ee5e8b719f600b95e22a43dbc26f1dc, with the Builds checkout at 6e7011fc, the recorded exact commands now pass. The broad builds_publication_qualification suite passed 70/70, and the project-reference source Debug solution build finished with 0 warnings and 0 errors (CS1704 was not reproduced). Exact-source push CI run 41707067835 was still in progress, so this entry stays open until that CI completes green.
  status: resolved 2026-11-10
  resolution: The remaining exact-source CI condition is met. Push CI succeeded for the corrected story tree 4dac7decfdd33b3d048de3c77e9cb1562bb95c6f (run 41775358363), the two authorized release sources 859680b38a77f04cc8f81ab7a5fe7f40a617cc2a (42040080659) and 246157a7ff8e2dcd8d356c833a77a02514490d2f (42132182708), and current HEAD 4ff3e6866ecb6d1a497e7063e972c24259252c40 (42220908958). Each run includes the test-unit-contract "Run container publish fixtures" step, source-debug and the `build` gate. Both release logs show `[release-source] pass` naming 42040080659 and 42132182708. On current HEAD, with every gitlink at its recorded commit (Builds 5fa2b97c8b22047c2c33e2c9f7017fd5d604c9ef from owner commits 38572369 and 246157a7), the recorded exact commands pass. release_dispatch passed 17/17, including the pin/profile parity test, and builds_publication_qualification passed 70/70. `dotnet build Hexalith.Memories.slnx --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 -v:m` finished with 0 warnings and 0 errors (45 outputs, source EventStore.Contracts). That build first failed on a stale local restore (MSB4018 for a missing restored 3.132.2 Contracts file, plus AngleSharp and MCP API errors). `dotnet restore` of the declared dependencies, with no pin change, cleared it. Story 30.4 changed no dependency. Review R3-B9a (2026-11-10): the explicitly approved dependency/profile reconciliation this trigger requires is the owner's own pair of commits. 38572369ce83ea10e720993eb772860d8eca3f6c (Jérôme Piquot, 2026-11-05) moved the qualification profile `buildsSha` from 6e7011fc to fc212224, and 246157a7ff8e2dcd8d356c833a77a02514490d2f (Jérôme Piquot, 2026-11-06) moved the profile and the Builds gitlink together to 5fa2b97c.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Prove the 120-minute release job budget covers five multi-RID OCI builds, five Docker builds and ten digest-bound health runs (five under QEMU) before the first publication-enabled release (unverified high).
  evidence: Review R2-B7/R2-E5. During this review, external commit 10ac2d676f8a61dc5359d46574b9709db28012a5 raised the release job budget from 45 to 120 minutes and the preflight step from 10 to 20. That narrows the risk but proves nothing. No publication-enabled release run exists, and the enabled-flag-off runs (7–24 min) never build containers. A timeout cancels the job after irreversible pushes, and the pre-existing `if: failure()` alert does not run on cancellation. Owner: Jérôme Piquot. Consequence: a cancelled publication may skip the partial-publish incident. Reopen trigger: a timed publication dry run (build plus health without push) on ubuntu-24.04, or the first authorized credentialed run's step timings. This story changes no job timeout.
  status: resolved 2026-11-10
  resolution: The trigger's credentialed-run clause fired. Hardening commit 4dac7decfdd33b3d048de3c77e9cb1562bb95c6f is absent from v3.0.23, so authorized Release run 42108808279 (v3.0.24, source 859680b38a77f04cc8f81ab7a5fe7f40a617cc2a) is the first credentialed run of this contract, and 42134622308 (v3.0.25, source 246157a7ff8e2dcd8d356c833a77a02514490d2f) repeated it. Step timings come from the GitHub jobs API, job logs and packet member times. The `release` job took 14m35s and 16m50s against `timeout-minutes: 120`, at most 14% of the budget. "Run semantic-release" took 11m12s and 12m42s. The ten archive builds (five Docker, five two-RID OCI) took about 67 s and 68 s, from identity.json to build-summary.json. The five transports plus ten digest-bound health runs took 8m31s and 9m52s, and each linux/arm64 health run under QEMU took 70–89 s. "Run release preflight" took 8 s against its 20-minute step limit. Both runs used the `ubuntu-latest` image `ubuntu-26.04`, not the dry-run clause's ubuntu-24.04; the credentialed-run clause names no image. Neither run was cancelled. The `if: failure()` alert still does not run on cancellation, but the measured margin is about 86% of the budget. No job timeout changed. Review R3-B7 (2026-11-10), corrected in review round 4 (R4-B10): those timings do not bound a stalling dependency. Every `run()` call allows the 1800 s timeout: the ten archive publishes, the three shared preflight phases, the five `skopeo copy` calls and the five health runs, all sequential. The five copies alone can take 150 minutes, so a registry that stalls every copy can still outlast the 120-minute job. A timed-out copy now keeps its partial `transport.log` and refuses (R3-E3). The cancellation path stays as recorded in R2-B7: `artifacts/release/**` still uploads with `if: always()`, and the pre-existing `if: failure()` alert does not run on cancellation.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Establish whether skopeo can exit nonzero or time out after writing the release tag, which would leave an occupied tag recorded as `transported: false` (unverified medium).
  evidence: Review R2-E4. `publish_archives` sets `transported` only after `run([skopeo, "copy", ...])` returns 0. If the index PUT lands before a nonzero exit or the 1800 s timeout, the summary understates registry side effects. Owner: Jérôme Piquot. Consequence: partial-publish evidence could omit an occupied tag. Reopen trigger: a registry fixture that fails skopeo after the manifest PUT, or a post-failure tag re-probe, settles it before Story 30.5 relies on transport outcomes.
  Follow-up 2026-11-10: every skopeo copy in authorized runs 42108808279 and 42134622308 exited 0, and all ten transports recorded `transported: true`. Neither a post-failure tag re-probe nor a failing-registry fixture exists, so this trigger has not fired. The entry stays open. Follow-up 2026-11-15 (Story 30.5): `PartialReleaseRecoveryTests.test_copy_failure_after_manifest_put_is_reconciled_by_read_only_graph_on_rerun` now proves recovery re-probes every member instead of trusting `transported`; a landed index is skipped on the next run. The original release-path summary finding remains open; this fixture supplies no live interruption or registry-policy proof.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Verify that a real multi-RID `PublishContainer` OCI archive carries the approved provenance labels on both children (unverified medium).
  evidence: Review R2-B3. MSBuild evaluation of `RebindContainerProvenanceLabels` with the exact multi-RID arguments emits labels identical to the single-RID case, but the SDK's multi-RID inner-build label reconstruction was not exercised. The approved validator would refuse at prepareCmd, before any tag or write. Owner: Jérôme Piquot. Consequence: a release could fail closed at archive preparation. Reopen trigger: one real multi-RID PublishContainer archive passes `archive_graph` with the approved validator, or the first authorized credentialed run.
  status: resolved 2026-11-10
  resolution: The first authorized credentialed run fired the trigger. Runs 42108808279 (v3.0.24) and 42134622308 (v3.0.25) built every OCI archive with `RuntimeIdentifiers="linux-musl-x64;linux-musl-arm64"`, and each `build-oci.log` shows both inner builds. The approved Builds 81c88b34 validator's `archive_graph` accepted all five archives (`<member>/local/oci-validation.json` `result: pass`). In the retained v3.0.25 packet, both children of every image record the seven approved provenance labels (revision 246157a7ff8e2dcd8d356c833a77a02514490d2f, version 3.0.25, source, documentation, url and both created labels). Each raw `child-linux-<arch>.config.raw` carries the same labels. A read-only `docker buildx imagetools inspect` on 2026-11-10 returned the same labels and architecture for all ten registry child digests, and the v3.0.24 packet cross-checks the same way. Receipt SHA-256: v3.0.25 dfa54c81c8da35e002e99527f80a29a1bf8a0b3574876056bb3b81335f1d5c9f, v3.0.24 55f0f5352bcd38c15c0ed58b0608d3cd7cf3bd2e9b4426080c6a4e532b146449; both match the downloaded release assets.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Align the release workflow's 90-day diagnostics retention request with the repository's 30-day artifact retention maximum.
  evidence: Live reconciliation 2026-11-10. `.github/workflows/release.yml` uploads `release-publication-evidence` with `retention-days: 90`. Both authorized runs (42108808279, 42134622308) logged "Retention days cannot be greater than the maximum allowed retention set within the repository. Using 30 instead.", and artifact 13465662617 expires 2026-12-06, 30 days after creation. Successful packets are unaffected because they are durable GitHub release assets. Review round 3 (R3-B5) aligned `docs/dev/release-runbook.md` with the effective 30-day cap and the authorized-run evidence, so only the workflow/repository-setting mismatch remains. Owner: Jérôme Piquot. Consequence: a failed release's diagnostic packet expires after 30 days, not the 90 the workflow requests. Reopen trigger: the repository artifact retention maximum is raised to at least 90 days, or `release.yml` requests the effective cap.
  status: resolved 2026-11-11
  resolution: The authorized release follow-up fired the existing retention-policy trigger. Read-only `gh api --method GET repos/Hexalith/Hexalith.Memories/actions/permissions/artifact-and-log-retention` returned `days: 90` and `maximum_allowed_days: 90`. Failed Release run 43012088139 uploaded artifact 13850599888 with expiry 2027-02-09T11:27:25Z, matching the requested 90 days. The repository now permits the workflow's request; no setting was changed in this session. The runbook distinguishes this current policy from the historical 30-day cap observed by v3.0.24/v3.0.25. Readback is retained in /tmp/memories-30-4-release-20261111-u6vf60k8/artifact-retention-readback.json. This resolves only the retention mismatch and grants no publication-success credit.

## Deferred from: code review of spec-30-4-five-image-publication-contract.md round 3 (2026-11-10)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Do not deploy the published v3.0.20 through v3.0.25 Server images; both children of each carry `PATH=/app` (high).
  evidence: Review R3-B1, widened in review round 4 (R4-B1, R4-B3). SDK 10.0.403 multi-RID inner builds rebuild container environment variables with `Split(':')[1]`, so the Server's declared `PATH=/app:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin` shipped as `PATH=/app`. Read-only `docker buildx imagetools inspect` reads on 2026-11-10 show that each of 3.0.20, 3.0.21, 3.0.22, 3.0.23, 3.0.24 and 3.0.25 holds all five two-platform image indexes, and `registry.hexalith.com/memories` returned `PATH=/app` for linux/amd64 and linux/arm64 of every one. The 3.0.20 and 3.0.21 Server children also keep the relative `dotnet` entrypoint, which `PATH=/app` cannot resolve, so they cannot start; 3.0.22 onward use the absolute `/usr/share/dotnet/dotnet`. 3.0.19 pushed no image. The Production overlay's `wget` exec startup, readiness and liveness probes exit 127 on every affected Server image and can never pass; R3-B1 recorded that the live cluster stays Ready only because it runs the older single-platform `71e49b6e` image. The tags stay deployable: the v3.0.23, v3.0.24 and v3.0.25 GitHub releases each carry a `hexalith-memories-production.yaml` asset rendered against their own images, v3.0.25 was then the repository's Latest release (superseded by corrected v3.0.26 on 2026-11-11), and v3.0.20–v3.0.22 have no GitHub release. Published tags are immutable, so nothing was retagged, overwritten or deleted, and no remote action was taken. The fix (`RebindMultiRidContainerEnvironmentVariables` in `Directory.Build.targets`) and a child-versus-deployment-archive runtime-config parity check before the publish preflight and any registry write shipped with the qualified v3.0.26 release from f807701e; its packet proves full PATH and parity on both children. This does not withdraw old assets. Owner action (Jérôme Piquot): mark the v3.0.23, v3.0.24 and v3.0.25 GitHub releases do-not-deploy, and make the Story 30.6 cutover refuse the v3.0.20–v3.0.25 tags. Owner: Jérôme Piquot. Consequence: no v3.0.20–v3.0.25 Server image may be deployed, and the historical affected releases still offer those assets; corrected v3.0.26 has independently qualified children. Reopen trigger: the owner marks the affected GitHub releases do-not-deploy and the Story 30.6 cutover refuses those tags; a later fixed release alone does not close this entry.
  status: open
  retarget: 2026-11-15 — D3 satisfies the tag-refusal half through the release rollback rule (v3.0.19–v3.0.25 refused). Remaining owner/reopen trigger: Jérôme Piquot marks the affected GitHub Releases do-not-deploy. No external release edit was performed; a fixed later release does not close this action. See parity row `do-not-deploy`.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Align the pre-existing CI-ownership documentation with the current shared CI build timeout and eight-project coverage lane.
  evidence: Review R3-B6 (low; pre-existing), corrected in review round 4 (R4-B13). `docs/dev/release-runbook.md:45` still says "Seven Coverlet project reports" and `:50` says "a 20-minute build timeout". `CONTRIBUTING.md:506-507` says `shared-ci` calls `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml` at `81c88b34d09d34260b92d7f0928dbbc1c7446baa` with "a 20-minute budget", while `.github/workflows/ci.yml:171` uses `6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`. Those lines date from aa097b52 (Story 30.1). External commits 3d6e4662 (shared build timeout), 73510013 and 7e2e87e8 (which moved the `ci.yml` pin off 81c88b34 and then to 6e7011fc) and 246157a7 (eighth coverage project, DomainService.Tests) made them stale. Owner: Jérôme Piquot. Consequence: contributors read the wrong timeout, Builds workflow pin and coverage inventory. Reopen trigger: the next edit to the CI-ownership table or the CONTRIBUTING CI section.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Establish whether the shared CI Builds workflow pin is meant to differ from the Builds publication qualification profile (unverified medium).
  evidence: Review R3-B9b. `.github/workflows/ci.yml:171` calls `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@6e7011fc754e74ac2a8864da1ce0b6d8f7178a48`, while `tools/builds-publication-qualification.json` binds `buildsSha` 5fa2b97c8b22047c2c33e2c9f7017fd5d604c9ef (moved by owner commits 38572369 and 246157a7). No test asserts the intended relation. Owner: Jérôme Piquot. Consequence, if unintended: CI runs a Builds workflow revision other than the qualified one. Settle with an owner statement of the intended relation, or a pin-relation test that encodes it.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Record the provenance of the re-pinned qualification curl 8.20.0 musl asset hashes (unverified medium-to-high).
  evidence: Review R3-B10. External commit 49c913da (`fix(release): refresh pinned qualification curl hashes`) changed the SHA-256 pins in `tools/stage-qualification-curl.py` for the same-named `curl-linux-*-musl-8.20.0.tar.xz` assets and updated the tests to match, with no recorded reason. These binaries ship in `/app` of the Production Server image. Owner: Jérôme Piquot. Consequence, if the upstream bytes were not legitimately re-released: an unreviewed binary substitution entered the release image. Settle by comparing the pins with the upstream release's published checksums or re-release notice, and record the source next to the pins.
  status: resolved 2026-11-16
  resolution: Story 30.7 (`epic-30-retro-item-92`, retro F13). Read-only `gh api repos/stunnel/static-curl/releases/tags/8.20.0` reports GitHub asset digests `sha256:7a12a4c460967e769fad9ddae2de4fce6679633b5a1891628e657dad8df57f3d` for `curl-linux-x86_64-musl-8.20.0.tar.xz` and `sha256:b5dde6790a33f55dee04e965d5b0fcd9c82493b72e1e5608c09ce1483359fb81` for `curl-linux-aarch64-musl-8.20.0.tar.xz`, exactly the current pins. Both assets were created at 2026-04-29T16:26:33Z and last updated at 16:26:34Z and 16:26:35Z, so neither was re-uploaded after the release. Fresh downloads on 2026-11-16 hashed to the same values. The replaced 72af4eaf pins (`58c6fab6…`, `32799692…`) equal the release notes' table of inner `curl` binary checksums, and the extracted `curl` files hash to them, so they could never match the downloaded `.tar.xz`. 49c913da therefore corrected a wrong pin; it did not accept substituted bytes. The provenance and refresh command now sit next to the pins in `tools/stage-qualification-curl.py`. No pin, test or published image changed.

## Deferred from: code review of spec-30-4-five-image-publication-contract.md round 4 (2026-11-10)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Establish whether an interrupted failed-retry admission can leave a Preparing source-inventory entry that blocks every later re-ingestion of that unit (unverified medium).
  evidence: Review R4-E13. External ingestion commit 3a62bfe1 (`src/Hexalith.Memories.Server/Ingestion/SourceInventoryStore.cs:57-61`): if the process dies between `AdmitFailedRetryAsync` and the `Unscheduled` catch, a FailedRetry entry may stay in Preparing with no workflow state, and `LatestRetryPredecessor` may then answer Conflict permanently. Not caused by Story 30.4. Owner: Jérôme Piquot. Consequence, if real: one memory unit can no longer be re-ingested. Settle with a crash-injection test that stops after admission and before scheduling, then retries the same unit.

## Deferred from: resumed verification of spec-30-4-five-image-publication-contract.md (2026-11-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Reconcile the current Builds gitlink with the approved CI qualification profile.
  evidence: HEAD db769a95e2966dd42f5523ce091e7f8f317f22a4 advanced the Builds gitlink and checkout to 891886ddc60a9924d053bb74bf6bcc73e101b2ce, while tools/builds-publication-qualification.json still binds 5fa2b97c8b22047c2c33e2c9f7017fd5d604c9ef. `python3 -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v` passed 16/17; its pin/profile parity test fails. `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v` attempted 60 tests with 3 failures and 9 errors after the harness refused builds-gitlink-mismatch; 28 focused profile/release-set/committed-evidence/document cases passed within that run. The source Debug solution build initially found a stale EventStore.Contracts 4.2.0/4.1.0 assembly conflict; after a cache-only restore with package sources cleared, the identical build passed with 0 warnings and 0 errors (18.01 s). The focused Debug CLI build and 138 inventory tests also pass. The earlier cohort resolution remains historical evidence for its own source. Owner: Jérôme Piquot. Consequence: current dispatch and broad Builds qualification cannot receive green credit. Reopen trigger: explicitly approved reconciliation of the current Builds gitlink and qualification profile, followed by both exact failing commands passing and exact-source push CI succeeding. Story 30.4 changes no pin, reference, setting or approval identity; no remote restore was attempted.
  local_resolution: 2026-11-11 — The user's follow-up authorized reconciling the Builds profile. Aligned tools/builds-publication-qualification.json and the qualification test's PINNED_SHA with the existing gitlink/checkout 891886ddc60a9924d053bb74bf6bcc73e101b2ce, preserving the approved release-publication revision 81c88b34d09d34260b92d7f0928dbbc1c7446baa. Both exact failed commands now pass: release_dispatch 17/17 (0.295 s) and builds_publication_qualification 70/70 (25.119 s), the latter using the isolated XDG_CACHE_HOME recorded in the spec. No submodule update, approval change, commit, push or release occurred.
  status: resolved 2026-11-11
  resolution: The user's read-only authorization allowed verifying exact-source push CI. Remote main equals local HEAD f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb; CI run 43007809636 succeeded on that SHA, including source-debug, test-unit-contract (container publish fixtures 98/98 with zero skips), production-deployment-verification and the final build gate. Builds qualification run 43007808801 also succeeded on the same SHA. Together with the approved profile reconciliation and current local release_dispatch 17/17 and builds_publication_qualification 70/70, every condition in this entry's reopen trigger is met. Readback metadata and the CI container log excerpt are retained under /tmp/memories-30-4-remote-20261111-_gviollu/. No CI or Release workflow was triggered or approved. This resolves only the profile/CI entry: no corrected release exists, and Story 30.4's three live-evidence checkpoints remain blocked independently.

## Deferred from: authorized Story 30.4 release preparation failure (2026-11-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Retain the failed 3.0.26 MCP preparation evidence and verify the repaired source in CI and an authorized corrected release.
  evidence: Authorized guarded Release run 43012088139 selected 3.0.26 from source f2c1d8a813e7a03d8f3ac5e1217c283fe8af33bb and set baeee36366281ed440bcbcb6920a887b6ae4fae79cb07d06ee5d282cc2c069cb, but failed before publication in the SDK multi-RID MCP label parser (MSB4184). External commit 1143ba17a2336f0d718fcff71e049c06350a45a2 added `not retired; see` to Description; its literal semicolon splits the SDK's transported description labels before the approved provenance rebinding target can run. Artifact release-publication-evidence 13850599888 has verified ZIP SHA-256 cb9e403a3d7c77a88e45c6433a94db9eff00d05424a96f37b9dcec75fefa1ee9. The parent's diagnostic verifier confirms tar SHA-256 a723baeecc4e9150598afc459af955e1e5e30ed91182b936ec48ce71dbdd0729 and 48/48 re-hashed manifest entries. Server, lifecycle, clock and DomainService passed build/config parity, including the Server's full declared PATH on both unpublished children, but no publish hook ran. All five registry tag reads, ten NuGet version reads and the Git tag read found no 3.0.26 publication. Diagnostic packet, receipt verification and side-effect readback remain under /tmp/memories-30-4-release-20261111-u6vf60k8/; this is diagnostic/build evidence, not release success.
  local_resolution: Replaced only the MCP Description prefix punctuation with `not retired. See`, preserving the compatibility/removal-gate text and all other project bytes. The new real SDK ComputeContainerConfig/inner-parser regression was red on both RIDs before the fix and green after it, while both old-semicolon controls still reproduce MSB4184. Focused publication-contract selection passed 46/46 (47 discovered, exactly the Docker pull case excluded), and the MCP source Debug project-reference build passed with 0 warnings/errors. Local logs remain under /tmp/memories-30-4-mcp-20261111-ylbho0j4/. No SDK/shared helper, dependency, setting or publication changed.
  status: resolved 2026-11-11
  resolution: The user's subsequent authorization allowed committing/pushing the prepared fix and retrying publication after exact-source CI. Commit f807701e45297064f3968bcc8052e8002eac6d34 passed push CI 43015850134 (all 99 container tests, zero skips) and Builds qualification 43015849283. Authorized Release run 43018206370 then succeeded with v3.0.26. Its retained packet SHA-256 f3819e67d3f246e0a5c56dcf854ca34231cb8453753a047b89495541195d70d2 and all 128 entries re-hash; all five index/child/config graphs match the registry, ten platform health/cleanup outcomes pass, and every six-field runtime-config parity receipt recomputes. A fresh exact-source checkout reproduces the retained and release deployment YAML bytes. These facts fully meet this entry's trigger and close the three container evidence holds. Full readback is retained under /tmp/memories-30-4-retry-20261111-l30c4f3l/. No older bad tag was retagged, overwritten or withdrawn; the separate do-not-deploy/cutover and registry-policy entries remain open. Final public readback at 2026-11-11T12:45:09.338107+00:00 confirmed all ten 3.0.26 version indexes and exact package downloads; the CLI visibility hold cleared without any re-push.

## Deferred from: code review of Story 30.4 round 5 (2026-11-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Verify whether fresh-candidate modern provenance can suppress discovery and deletion of chunk-only legacy semantic output (potentially high, unverified).
  evidence: R5-E4 points to SourceInventoryStore.cs:405-417, introduced by external ingestion work: fresh admission checks exact output keys before retaining modern provenance. A candidate with only untracked historical semantic chunks may therefore need a stronger legacy reconciliation guard, but the complete deletion consequence is not yet established. This is outside the five-image publication intent and is not caused by Story 30.4. Owner: Jérôme Piquot. Consequence if real: a source deletion could leave historical vectors behind. Reopen trigger: a real Redis/graph seed-admit-delete test with chunk-only legacy output either proves it is removed or demonstrates the remaining keys/vectors; fix the owning ingestion story if demonstrated.

## Deferred from: code review of spec-30-4-five-image-publication-contract.md round 6 (2026-11-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Amend the Story 30.4 owned paths in `epics.md` to the delivered File Scope, including the edit to the 30.3-owned `CiTestInventoryTests.cs`.
  evidence: Review R6-H2. `epics.md:5805` lists four owned paths. Story 30.4 also edited `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` (named as Story 30.3's at `epics.md:5754` and in `epic-30-context.md`), `.github/workflows/release.yml`, `.releaserc.json`, `tests/tooling/release_dispatch/release_dispatch_test.py`, `Directory.Build.targets` and `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`. The spec records these as declared cross-story edits. The delivered breadth was ratified under the owner's standing review preference and is listed for veto. Owner: Jérôme Piquot. Consequence: the next reader of `epics.md` sees a narrower ownership than the code history. Reopen trigger: the next correct-course or story creation that touches Epic 30 planning text.
  status: resolved 2026-11-15
  resolution: Story 30.6 — Dated R6-H2 note at Story 30.4 owned paths points to the delivered File Scope and records the declared cross-story C# inventory edit.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Register the Runtime config parity checkpoint in the `epics.md` Story 30.4 checkpoint table.
  evidence: Review R6-H3a. The round-3 loopback (R3-B1) added a 14th row, Runtime config parity, with its own owner, artifact, review state and completion state. `epics.md:5812-5826` still registers only 13 rows. The check corrects the platform/health contract inside the frozen intent, so it is not a hidden slice. Owner: Jérôme Piquot. Consequence: the planning record understates the story's verified contract. Reopen trigger: the next correct-course or story creation that touches Epic 30 planning text.
  status: resolved 2026-11-15
  resolution: Story 30.6 — Dated R6-H3a note registers the fourteenth runtime-config checkpoint with owner, exact retained packet/run, review state and completion state.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Register a v3.0.20–v3.0.25 tag-refusal criterion in Story 30.6, or rewrite the do-not-deploy reopen trigger so it does not depend on unregistered scope.
  evidence: Review R6-H4. The round-3 do-not-deploy entry's reopen trigger requires that "the Story 30.6 cutover refuses those tags", but Story 30.6 (`epics.md:5860-5894`) has no such acceptance criterion or checkpoint. Owner: Jérôme Piquot. Consequence: the do-not-deploy entry can never close through its own trigger. Reopen trigger: Story 30.6 creation or the next Epic 30 correct-course.
  status: resolved 2026-11-15
  resolution: Story 30.6 — D3 release rollback now explicitly refuses v3.0.19–v3.0.25 and the pre-30.3 caller in the runbook and parity matrix. The do-not-deploy entry remains open solely on the owner's external release-marking action.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Correct the stale Story 30.4 planning text in `epics.md` against the verified story record.
  evidence: Review R6-P4. `epics.md:5774` reads `Status: backlog`. `:5810` says "All artifacts below are planned; no gate has passed", and all 13 checkpoint rows read `pending | not complete`. The zot "Known risk (carried, not yet re-verified)" at `:5801` was re-verified: authorized Release 43018206370 pushed every image through the skopeo authfile path, so the challenge-response 401 did not block publication. The story's Epic AC Verification table records the corrected values. Owner: Jérôme Piquot. Consequence: the planning artifact contradicts the completed story. Reopen trigger: the next correct-course or story creation that touches Epic 30 planning text.
  status: resolved 2026-11-15
  resolution: Story 30.6 — Dated notes at Story 30.4 status, known risk and checkpoint table point to done/reviewed story evidence and the successful skopeo transport without rewriting historic rows.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Restore the full `org.opencontainers.image.base.name` label on multi-RID image children.
  evidence: Review R6-E1/R6-E2, decision D2 (owner, 2026-11-11: defer and track). The SDK passes generated labels to each inner RID build as `Identity:Value` and rebuilds them with `Split(':')[1]`. In the retained v3.0.26 packet (run 43018206370), both children of every image therefore carry `"org.opencontainers.image.base.name": "mcr.microsoft.com/dotnet/aspnet"`; the declared base is `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (`Directory.Build.targets:10`). `org.opencontainers.image.base.digest` is intact. Runtime-config parity compares only the six runtime fields, so it cannot detect label drift. Owner reason for deferring: low-severity metadata only; base identity stays provable through base.digest. Owner: Jérôme Piquot. Consequence: tooling that reads `base.name` sees an untagged base reference on v3.0.20 and later multi-RID images, which are immutable. Reopen trigger: the next change to `Directory.Build.targets` container rebinding, or an SDK release that fixes the multi-RID label parser.

- source_spec: `_bmad-output/implementation-artifacts/spec-current-dependencies-ci-release.md`
  summary: Verify mutation sensitivity of the clock startup ordering regression under controlled Redis allocation and readiness delay.
  evidence: Review could not establish whether fast Redis startup can mask removal of the clock enrollment; actual start-boundary and existing sidecar endpoint checks pass, and production explicitly awaits rewrite and PING.

## Deferred from: Story 30.4 round-6 remediation verification (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Reconcile the current Builds gitlink and qualification profile without changing the Story 30.4 publication approval.
  evidence: Unchanged HEAD b7e19503 selects Builds gitlink 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee; tools/builds-publication-qualification.json and its qualification tests select ae77d5690a9411a2154fa4ed5231edc373942573. Neither file nor gitlink was changed by this remediation. Exact commands: `python3 -B -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v` passes 16/17, failing only test_release_pin_is_approved_and_profile_tracks_the_gitlink. `python3 -B -m unittest discover -s tests/tooling/builds_publication_qualification -p '*_test.py' -v` attempts 60 cases with 3 failures/9 errors after builds-gitlink-mismatch; 70 methods are discoverable and a separate ProfileTests/ReleaseSetTests/CommittedEvidenceTests/DocumentTests selection passes 28/28. Owner: Jérôme Piquot / workspace-release maintainer. Consequence: the current broad qualification gates are not green and Story 30.4 remains in-progress; no current CI or publication qualification is inferred from the released f807701e packet. Reopen trigger: explicitly authorized reconciliation of the current gitlink/profile identities, both exact commands passing and exact-source push CI succeeding. The five-image contract approval remains 81c88b34d09d34260b92d7f0928dbbc1c7446baa; no dependency, profile or protected setting was changed.
  local_resolution: 2026-11-15 — The user explicitly authorized aligning only tools/builds-publication-qualification.json buildsSha and the qualification test PINNED_SHA with existing gitlink/checkout 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee. Both previously failing suites now pass through run-with-temp: release_dispatch 17/17, zero skips (0.333 s), and builds_publication_qualification 70/70, zero skips (34.748 s). Logs are retained under artifacts/verification/story-30-4-remediation/reconciled-*.log. Current source publication helpers/workflow pins remain ae77d5690a9411a2154fa4ed5231edc373942573; the 81c88b34 reference in the original evidence describes the historical released-source approval. All relevant compared helper/provenance/workflow bytes are identical between ae77d5690a9411a2154fa4ed5231edc373942573 and 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee. The identity mismatch is locally fixed, but this entry stays open pending successful exact-source push CI and qualification. Owner, consequence and remaining CI trigger are unchanged; no publication authorization is inferred. No gitlink, release pin, protected setting, commit, push or release changed.

  status: resolved 2026-11-15
  resolution: The user authorized pushing reviewed commit 261e1b798c0a481b5a4841f2ccdf8574fe9ebe76 to main and monitoring its checks. Push CI 43707881607 succeeded on that exact SHA, including package Release/source Debug/build gates, all 111 publication fixtures with zero skips, integration coverage and disposable deployment verification. Builds qualification 43707881352 succeeded on the same clean source; artifact 14159347112 has verified ZIP SHA-256 eb89c2734f76907fc73499f4a855df0091d3907ed49e7c895691036347efd39d, and all six checkpoint/source/workflow bindings verify against current Builds 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee. Together with the explicitly authorized local identity reconciliation and the exact previously failing RD 17/17 and BQ 70/70 commands passing, this meets every clause of the trigger. Receipts are retained under artifacts/verification/story-30-4-remediation/pushed-261e1b79/. Qualification is synthetic with owner approval pending; release helper pin ae77d569 and historical v3.0.26 publication credit are unchanged. No new release, registry write, approval assertion or Production lifecycle action occurred.

## Deferred from: Story 30.4 round-7 cumulative review (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Complete fenced erasure after an unscheduled failed retry retains its predecessor's provenance (medium).
  evidence: R7-B1; external ingestion code selects FailedRetryMemoryUnitId for the Unscheduled entry, then the never-scheduled reconciliation refuses retained KeyProvenance, which projection deletion preserves. Current preparation-failure cases stop after rescheduling rather than asserting full erasure. Owner: Memories ingestion/erasure maintainer. Consequence: a failed retry may leave its source-inventory obligation permanently unreconciled. Reopen trigger: add a real Redis/graph prepare-or-track-failure plus fencing/erasure end-state regression, then correct the reconciliation ownership semantics in the ingestion work.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Verify the pre-existing one-hour NuGet OIDC key lifetime against delayed semantic-release preparation (medium, unverified).
  evidence: R7-B8; both original baseline d959bdae5af9c22f53573eda71c091cb5c0aae4c and current workflow mint the temporary key before semantic-release packing. The 120-minute job limit exceeds the key lifetime, but no reviewed execution demonstrates expiry before push. Owner: workspace-release maintainer. Consequence if confirmed: slow preparation can fail package publication despite successful earlier gates. Reopen trigger: key-to-push elapsed timing or a controlled delayed-prepare test demonstrates the boundary, followed by an explicitly approved credential refresh/order change.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-4-five-image-publication-contract.md`
  summary: Enroll the failed-retry and fresh-candidate producer integration proofs in normal CI execution (medium).
  evidence: R7-V1 pre-verified gap; SourceInventoryProducerIntegrationTests and ReferencedSourceProducerIntegrationTests have no Category=Integration trait on their classes, assembly or RedisStack collection, while CI filters by that category and unit-contract inventory excludes this project. Owner: Memories ingestion/test maintainer. Consequence: predecessor-status and coverage assertions can remain unexecuted while normal lanes pass. Reopen trigger: assign the integration category, register critical methods in tools/integration-fast-required-surfaces.txt, and retain passing required TRX records for the running/mismatched/fenced/tombstoned predecessor cases.

- source_spec: `spec-30-4-five-image-publication-contract.md`
  summary: Make tracked annotation scheduling retries repair an accepted schedule instead of attempting the same workflow again (R8-B8).
  evidence: Preserved external ingestion change c4addf98c6a4017d54e0c3aaeb3b57a52f82bffa makes AdmitSourceInventoryActivity accept an existing matching tracked workflow but return only IngestionInput. ScheduleAnnotationIngestionActivity then re-enters payload preparation, leaves Scheduling unchanged and calls ScheduleNewWorkflowAsync; DaprIngestionWorkflowScheduler instead repairs tracking/Scheduled state and returns for the accepted workflow. Owner: ingestion/workspace maintainer Jérôme Piquot. Consequence: an annotation activity retry after a lost scheduling acknowledgement can repeat a remote schedule and fail or reuse existing workflow authority; no annotation retry-safety credit is claimed. Reopen trigger: owning ingestion work adds an actual Dapr annotation lost-acknowledgement regression, demonstrates one accepted execution with repaired scheduling state and persisted source/output/history end-state, and verifies fence/identity refusals remain intact. This is outside the five-image publication intent and does not activate another story.

## Deferred from: Story 30.5 owner scope decision D3 (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Owner decision D3 hands release-path publish-failed incident eligibility to Story 30.6; GitHub-plugin failure is tracked by the existing Story 30.3 Round 3 Blind 8 entry.
  status: open
  target: Story 30.6 release cutover maintainer (Jérôme Piquot).
  evidence: Owner decision D3 leaves publish-failed incident alerts out of Story 30.5. `create-partial-publish-issue.ps1` still skips statuses other than partial-publish; the existing all-fail fixture pins that behavior. Consequence: an all-fail post-tag publication may have no incident. Reopen trigger: Story 30.6 implements release-path incident parity. Do not duplicate the separately owned GitHub-plugin failure entry.

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `publish-failed-incident` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Verify whether an interrupted GitHub CLI release create can leave only the generated recovery packet receipt, then qualify resume from retained recovery-run packet bytes if reachable (unverified medium).
  evidence: Review R1 BH3; current code refuses receipt-only recovery packets, but concurrent CLI upload ordering is not established from local source. An isolated actual CLI interruption fixture or authoritative implementation trace would settle reachability. Ordinary release packet/receipt subsets are handled in Story 30.5.
  status: resolved 2026-11-15
  resolution: R2 EC2 confirmed concurrent GitHub CLI uploads from its authoritative implementation. Recovery now creates an empty draft, uploads the packet before its receipt sequentially, and verifies all assets before publishing. Interrupted draft/packet/receipt and completed rerun fixtures pass; no live publication credit.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Require authoritative registry tag-immutability/race evidence for atomic protection against another writer between the terminal recovery probe and skopeo copy.
  status: open
  evidence: Review R2 EC1; Story 30.5 repeats the authenticated probe after archive materialization, but client-side absence checks cannot provide atomic exclusion. Owner: Jérôme Piquot. Consequence: no server-side race-free no-overwrite guarantee or live registry policy credit is claimed. Reopen trigger: authoritative registry tag-immutability policy readback or an isolated concurrent-writer registry fixture establishes atomic refusal. No settings or live publication are authorized by this story.

## Deferred from: code review of spec-30-5-partial-release-recovery.md (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Record dated planning corrections in `epics.md` Story 30.5 for the two `corrected` Epic AC claims. The owned paths were widened from two files to the 19 implementation/test/doc paths in the story File Scope, and the activation gate's Builds evidence is now owner-approved for `81c88b34` and `ae77d569` (D2).
  evidence: Code review 2026-11-15 (Historical Slice Guard F6). `_bmad/custom/epic-ac-verification.md` requires a planning-artifact correction or a dated note at the claim. `_bmad-output/planning-artifacts/epics.md:5832` (activation gate) and `:5847` (owned paths) still carry the pre-story text, and `:5830` still reads `Status: backlog`. Editing `epics.md` is outside the Story 30.5 File Scope. Owner: Jérôme Piquot. Consequence: the next reader of `epics.md` sees a two-file ownership claim and an unqualified activation gate that the code no longer matches. Reopen trigger: the next correct-course or epic-planning pass that touches Epic 30, or Story 30.6 creation, whichever comes first.
  status: resolved 2026-11-15
  resolution: Story 30.6 — Dated F6 notes at Story 30.5 status, activation gate and owned paths record done, the approved 81c88b34/ae77d569 recovery identities and the delivered File Scope. Historical planning text and evidence remain intact.


- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Align the `publish-nuget.ps1` child-summary, `::error` and step-summary guidance with the current Recover Partial Release path.
  evidence: Code review 2026-11-15 (Verification Gap). `tools/publish-nuget.ps1:45-47`, `:91` and `:106` still say "manually reconcile NuGet publication", and `publish_nuget_test.py:472-482` pins that text and asserts "Recover Partial Release" is absent. Story 30.5 changed the aggregate (`publish-release.ps1:196`) and incident guidance, so a current partial publish now carries contradictory guidance in one aggregate summary. `publish-nuget.ps1` is outside the Story 30.5 File Scope. Owner: Jérôme Piquot (Story 30.6 release cutover maintainer). Consequence: operators read contradictory recovery instructions inside one release evidence summary. Reopen trigger: Story 30.6 edits `publish-nuget.ps1` for the D3 `--skip-duplicate` hand-off.
  status: open

  retarget: 2026-11-15 — Story 30.6 D4; target and reopen trigger are `epic-30-retrospective` (Jérôme Piquot), parity row `nuget-child-guidance` in `30-6-cutover/parity-matrix.md`. This remains open as a shared gap; the prior 30.6 implementation trigger is superseded. No release-path fix was implemented.


## Deferred from: Story 30.5 remaining prerequisites and validation (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Configure the owner-required NuGet recovery Trusted Publishing policy (D1).
  status: open
  evidence: Frozen D1 requires policy `Hexalith/Hexalith.Memories/recover-partial-release.yml/production`, with `vars.NUGET_USER`. Owner: Jérôme Piquot. Consequence: absent-package recovery refuses at NuGet/login until the policy exists; image/asset-only recovery can proceed. Reopen trigger: owner configures the policy and an explicitly authorized exact-source recovery login retains successful policy-bound proof. No policy setting or live login was attempted during implementation.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Verify the NuGet repository-signing ZIP-entry identity rule against real retained and nuget.org-served bytes.
  status: open
  evidence: Offline fixtures compare every original ZIP entry and permit only repository `.signature.p7s` addition; they are no live NuGet compatibility proof. Owner: Jérôme Piquot. Consequence: live signing compatibility remains unverified and no live recovery credit is claimed. Reopen trigger: an authorized read-only download comparison of a real retained nupkg with its nuget.org-served counterpart retains exact hashes and entry differences proving the rule; any mismatch requires an owner-reviewed identity decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Reconcile the existing committed Builds gitlink and qualification profile before broad release qualification can pass.
  status: open
  evidence: At HEAD 1306a0f84cf78160a192be6bc341798fe32427d4, `git ls-tree HEAD references/Hexalith.Builds` selects e2bbbd0893f586e4d447ef8358ff747ea3d34435; `tools/builds-publication-qualification.json` declares 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee. Neither changed in this run. Exact commands: `run-with-temp python3 -B -m unittest discover -s tests/tooling/release_dispatch -p '*_test.py' -v` reports 16/17 passing, with the pin agreement failure; the same discovery command for `builds_publication_qualification` executes 60 tests with 3 failures/9 errors after the prerequisite failure (70 discoverable). Focused unaffected fallbacks pass 16/16 and 48/48 respectively. Owner: Jérôme Piquot / workspace-release maintainer. Consequence: broad qualification and story completion remain blocked; fixture passes do not authorize publication. Reopen trigger: explicitly authorized identity reconciliation followed by both exact full commands passing and exact-source push CI. The spec at that checkpoint forbade dependency/Builds pin changes in this implementation.
  local_resolution: 2026-11-15 — Owner decision D4 explicitly authorized reconciling only the qualification profile and current test expectation with existing committed/index/checkout Builds e2bbbd0893f586e4d447ef8358ff747ea3d34435. The two exact full commands now pass under run-with-temp: release_dispatch 17/17 (0.342 seconds), builds_publication_qualification 70/70 (28.896 seconds), both zero skips. Logs: artifacts/partial-release-d4-dispatch.log and artifacts/partial-release-d4-builds.log. Malformed/staged/checkout/helper drift refusals and immutable capture checks all executed. The local mismatch is resolved; this entry stays open for successful exact-source push CI. Owner: Jérôme Piquot / workspace-release maintainer. Consequence: no current clean-source CI qualification or live publication credit until that remaining trigger fires. No gitlink, release execution pin, D2 recovery allowlist, historical capture, setting, commit, push or release changed.

## Deferred from: Story 30.5 independent workflow review R4 (2026-11-15)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Qualify the recovery temporary NuGet key lifetime against several slow package-validation waits (unverified medium).
  evidence: R4 BH4. A single NuGet/login precedes recover, and each successful push may wait read-only for up to 600 seconds. Ten near-deadline successful validations could consume about 100 minutes, but no delayed multi-package key-expiry run was supplied. Owner: Jérôme Piquot / release maintainer. Consequence: no slow-validation credential-budget qualification. Reopen trigger: controlled elapsed-time/key-expiry evidence or an explicitly authorized delayed multi-package fixture demonstrates the boundary and selects the appropriate credential/order response. This is separate from the pre-existing release-path preparation lifetime entry; no login policy or credential surface is changed here.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Decide and qualify whether live-main validity must be rechecked after switching to the tagged recovery source (unverified medium).
  evidence: R4 BH7. Current source proofs run before/after production approval and immediately before source switching. No main-advancement-after-switch fixture or continuous-validity policy decision was supplied. Owner: Jérôme Piquot / release maintainer. Consequence: no continuous tip-validity guarantee across later initialization/publication; the exact green dispatched source is still proven at the existing boundaries. Reopen trigger: owner defines the required final boundary and an isolated advance-after-switch fixture demonstrates the expected refusal. No stronger continuous invariant or publication credit is inferred.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-30-5-partial-release-recovery.md`
  summary: Measure the supported recovery bundle's peak disk footprint on the GitHub runner (unverified medium).
  evidence: R4 BH9. Downloaded ZIP, retained bundle, extraction and materialized OCI layouts coexist; the review supplied no supported-size/free-space or ENOSPC execution. Owner: Jérôme Piquot / platform operations. Consequence: runner capacity for a full live release bundle is unqualified. Reopen trigger: measure a supported bundle's peak filesystem usage and runner free space, or reproduce a controlled capacity failure, before selecting cleanup/capacity changes. Offline tiny fixtures are not capacity evidence.
  status: open

## Deferred from: code review of spec-30-6-release-cutover-parity-and-rollback.md (2026-11-16)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-6-release-cutover-parity-and-rollback.md`
  summary: Prove the final 30.4-fix/30.5 release code live, with the D2 next-release identity snapshots and the additive-only comparison.
  evidence: Frozen D2 records that live proof of the final code is a recorded obligation, not passed evidence. The v4.0.0 rehearsal run `43432588932` ran pre-30.5 code. The exact commands and the tracked exact-byte evidence location are in `docs/dev/release-runbook.md` "Release Cutover Parity". Owner: Jérôme Piquot. Consequence: there is no final-code live publication credit. Reopen trigger: the next owner-authorized release dispatch, followed by `compare --expect additive` against its retained before-snapshot.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-30-6-release-cutover-parity-and-rollback.md`
  summary: Prove current-set retained-byte recovery live. D2's additive check observes only a successful release and cannot exercise recovery.
  evidence: Code review 2026-11-16 (Historical Slice Guard F5). The parity-matrix Failure/Recovery rows rest on static source plus the offline qualifier artifacts `30-6-cutover/recovery.json` and `partial-publication.json`, which carry no live credit. Parity row: `live-recovery`. Owner: Jérôme Piquot. Consequence: there is no live recovery credit for the final 30.5 code. Reopen trigger: the first owner-authorized Recover Partial Release run.
  status: open

## Deferred from: code review of spec-30-7-epic-30-retro-blockers.md (2026-11-16)

- source_spec: `_bmad-output/implementation-artifacts/spec-30-7-epic-30-retro-blockers.md`
  summary: Keep publication credentials out of the semantic-release prepareCmd build environment. This is the remaining half of Epic 30 retro F13, after `persist-credentials: false`.
  evidence: Review finding 16 (medium, pre-existing). In `release.yml`, the "Run semantic-release" step passes `GITHUB_TOKEN`, `NUGET_API_KEY`, `HEXALITH_ZOT_USERNAME` and `HEXALITH_ZOT_API_KEY` to the whole semantic-release process tree. That tree includes prepareCmd's `pack-release.ps1` (`dotnet build`/`pack`), which runs MSBuild targets and analyzers from every restored package. Story 30.7 removed only the persisted `.git/config` token. Owner: Jérôme Piquot. Consequence: a compromised restored package or build target could read the one-hour NuGet key or the registry credentials during preparation. Reopen trigger: the next change to the release publication hooks (`.releaserc.json`, `publish-release.ps1` or `pack-release.ps1`), or the A4 "Release-path failure-handling residuals" story, whichever comes first. Settle by scoping each secret to the step or child process that needs it, for example by packing before credentials enter the environment.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-30-7-epic-30-retro-blockers.md`
  summary: Establish whether an unprivileged main-branch job can seed a `nuget-*` Actions cache entry that the pinned `prepare-domain-release` restores in the production release job (unverified medium).
  evidence: Review finding 17 (maybe-false). The pinned `Github/prepare-domain-release@ae77d5690a9411a2154fa4ed5231edc373942573` restores `~/.nuget/packages` through `actions/cache`, reportedly with a `nuget-` restore-key prefix. Unprivileged `ci.yml` jobs on `main` still run Builds `@main` and major-tag actions. If any of them saves a `nuget-*` key, a compromised moving action could plant package contents that the release job packs from. NuGet does not re-hash extracted packages in the global packages folder. Owner: Jérôme Piquot. Settle by reading the exact cache `key` and `restore-keys` in `prepare-domain-release` at the pinned SHA, and listing the cache keys that `main` push runs save (`gh cache list`). If it is real, isolate the release cache key, or disable restore-key fallback in the publishing job. Reopen trigger: the next Builds execution-pin change (A6), or any `ci.yml` cache change.
  status: open


## Deferred from: Story 30.8 integration lane restoration (2026-11-16)

### DW-731: Sixty untagged IntegrationTests source methods have no automatic CI lane

- ID: 30.8-UNTAGGED-INTEGRATION
- Status: open
- Source story: 30.8 — Restore the orphaned Integration tests and guard CI lane selection
- Target artifact: `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServerTests.cs` (11 methods), `Fixtures/PublicSurfaceStabilityTests.cs` (5), `Ingestion/InventoryWirePaginationIntegrationTests.cs` (1), `Ingestion/MemoryErasureProcessProtocolTests.cs` (5), `Ingestion/ReferencedSourceProducerIntegrationTests.cs` (4), `Ingestion/SourceInventoryProducerIntegrationTests.cs` (17), `Ingestion/TenantIngestionLifecycleIntegrationTests.cs` (5), `Ingestion/UnitOwnershipAuthorityIntegrationTests.cs` (12); all paths after the first share the IntegrationTests project root.
- Re-open trigger: The next story that touches any of those eight files or the integration-fast filter must review and assign their automatic CI lane, resolve the failing producer case, and shrink `CiTestInventoryTests.ReviewedUnselectedTests` as methods gain selection.
- Evidence: Baseline xUnit discovery across all ten Release-built projects at `dd422905e563d0c23b7232df910a0c41d82d7b03` found these 60 untagged source methods were never selected by a lane, including the retired runner. The six Testcontainers classes passed 117/118 expanded cases locally; `ReferencedSourceProducerIntegrationTests` fails non-deterministically in 1–3 cases per run, always including `FailedReingestionCopiesPriorSourceAndReconcilesOnlyTheNewOwnerAcrossRestart(failBeforeScheduling: True, storedOwner: "foreign")`. `OllamaOidcFakeServerTests` needs the Aspire fixture; `PublicSurfaceStabilityTests` has no fixture. Story 30.8 preserves these sources and registers exact per-file counts in `UnselectedTests_MatchTheReviewedRegister`; tagging them before resolving the failure would not be earned coverage. Owner: Murat. Consequence: these 60 methods remain outside automatic CI until the trigger is handled. The inert pipe-worker entry point and opt-in CLI benchmark are separate design exclusions, not part of this debt.

**2026-11-20 priority update.** The PR filter selects 492 of 638 expanded integration cases on `11343d16b968`. The 146 omissions include all 55 source-inventory producer cases and all 13 tenant-ingestion lifecycle cases. The historical 60 above counts source methods. Prioritize this work for the next integration-lane change. Resolve the known flaky producer case, and compare discovered test identities with the union of executed lane results. Owner: Murat. Reproduction commands are in the [audit course correction](../planning-artifacts/sprint-change-proposal-2026-11-20.md).

## Deferred from the 2026-11-20 repository audit

### DW-732: Keep ingestion source credentials out of logs and case activity

- ID: AUDIT-20261120-SOURCE-DISPLAY
- Status: resolved
- Source story: Repository audit and [audit course correction](../planning-artifacts/sprint-change-proposal-2026-11-20.md), finding 2
- Target artifact: `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` and `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs`
- Re-open trigger: Either URL marker appears in a log or case activity, the fetch target changes, or duplicate detection loses the original URL or its tenant and case scope.
- Resolution (2026-11-20): FR2 and FR36 are satisfied across enqueue, fetch, successful indexing, duplicate delivery and failure. Logs identify work by its ID and retain the failure stage and error code. The named HTTP client omits raw request logging. Case activity uses the memory-unit ID. Previously emitted source-bearing descriptions are protected when read or redelivered.
- Acceptance evidence: Separate synthetic secrets in the URL path and query were absent from structured log fields, rendered logs, Redis activity entries and the activity HTTP response. The upstream received the exact submitted URL. Submitting it twice in one tenant and case returned the original memory-unit ID, with one fetch, one indexed unit across the backing stores and one ingestion activity. After indexing, changing the tenant, case, path or query yielded a distinct deduplication lookup. A 404 exercised the durable retry and failure path, including stored and API failure activity.
- Focused server tests: 159 passed, 0 failed, 0 skipped.

```bash
dotnet test --project tests/Hexalith.Memories.Server.Tests \
  --filter-class '*.IngestionWorkflowTests' \
  --filter-class '*.IngestionEndpointE2ETests' \
  --filter-class '*.FetchUrlActivityTests' \
  --filter-class '*.UrlContentFetcherTests' \
  --filter-class '*.UrlContentFetcherHttpPipelineTests' \
  --filter-class '*.CaseActivityServiceTests' \
  --filter-class '*.IngestionEndpointLogTests' \
  --filter-class '*.CheckIdempotencyActivityTests' \
  --filter-class '*.DedupKeyBuilderTests' \
  --filter-class '*.ContentExtractionClientTests' \
  --filter-class '*.SaveDedupKeyActivityTests'
```

- Live integration tests: Both prerequisite builds succeeded. Both selected tests passed with 0 errors, failures or skips in 45.383 seconds. The live run used .NET 11, Aspire and Dapr in a disposable Linux container with Redis. The initial host run stopped during Aspire startup before ingestion because the control plane could not write its per-user port file.

```bash
dotnet build references/Hexalith.EventStore/src/Hexalith.EventStore -c Debug -m:1
dotnet build tests/Hexalith.Memories.IntegrationTests -c Debug -m:1
dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net11.0/Hexalith.Memories.IntegrationTests.dll \
  -method '*.UrlIngestionIntegrationTests.UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable' \
  -method '*.UrlIngestionIntegrationTests.UrlIngestion_404Url_ShouldFailAfterRetries'
```

### DW-733: Demonstrate recovery after partial writes for a live tenant

- ID: AUDIT-20261120-LIVE-RESTORE
- Status: open
- Source story: Repository audit and [audit course correction](../planning-artifacts/sprint-change-proposal-2026-11-20.md), finding 3
- Target artifact: `docs/operations/backup-restore.md` and `tests/Hexalith.Memories.IntegrationTests/Restore/`
- Re-open trigger: Before advertising a partial logical restore retry or approving a production recovery drill that depends on it.
- Evidence: Restore requires a matching tenant and clean target. Deletion records a write fence and provisioning refuses that identifier. Owner: recovery owner. Rehearse a partial write on a disposable live tenant, demonstrate recovered contents and an unaffected control tenant, and publish only the procedure the drill proves. Follow the documented stop conditions until then. Stories 34.24/34.25 separately own erased-origin refusal.

### DW-734: Align the evaluated EventStore package floor and its tests

- ID: AUDIT-20261120-EVENTSTORE-FLOOR
- Status: resolved
- Source story: Repository audit and [audit course correction](../planning-artifacts/sprint-change-proposal-2026-11-20.md), finding 7
- Target artifact: `Directory.Packages.props` and `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- Re-open trigger: A package consumer resolves below the supported minimum, a higher selection is downgraded, or source-backed builds start applying the package floor.
- Resolution: [PR #73](https://github.com/Hexalith/Hexalith.Memories/pull/73) merged on 2026-11-20. Package consumers enforce a single 5.4.0 minimum and preserve higher selections. Source-backed consumers retain the selected version.
- Evidence: All 31 cases in `AppHostSecurityConfigurationTests` passed, including the nine that failed during the audit. Debug/source and Release/package builds passed. The [hosted unit and contract check](https://github.com/Hexalith/Hexalith.Memories/actions/runs/44731772842/job/136273081509) passed with 81.39% line coverage.

### DW-735: Measure per-tenant ingestion ownership capacity

- ID: AUDIT-20261120-OWNERSHIP-CAPACITY
- Status: open
- Source story: Repository audit and [audit course correction](../planning-artifacts/sprint-change-proposal-2026-11-20.md), finding 8
- Target artifact: `src/Hexalith.Memories.Server/Ingestion/TenantIngestionOwnership.cs` and `docs/operations/capacity-planning.md`
- Re-open trigger: Before claiming the 100K-unit reference workload, during the next Story 34.39 sizing exercise or on an observed ownership-capacity refusal, whichever comes first.
- Evidence: The retained-key limit is 65,536, including per-unit aliases on the tracked path. Owner: ingestion and performance maintainers. Measure key cardinality by input shape, admission behavior and deletion completeness across the target workload. Size any subsequent partitioning change from the observed distribution.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-2-runtime-dapr-secret-store-migration.md`
  summary: Close the Dapr built-in `kubernetes` secret-store path, which gives the memories, memories-mcp and access-telemetry sidecars Kubernetes Secret reads outside OpenBao and the secret allow-lists.
  evidence: Story 31.2 review finding Blind 1 (high, pre-existing deployment defect). At 2026-11-22T15:51:51Z an in-pod metadata-only request listed `kubernetes` / `secretstores.kubernetes` in memories-675fd5d64-5zqpv beside both Vault stores, and as the only secret store in memories-mcp-55d65d86d6-hh6mr. No tracked Configuration (`memories-config`, `memories-mcp-config`, lifecycle or clock) scopes `storeName: kubernetes`, so Dapr allows every name there. No workload sets `dapr.io/disable-builtin-k8s-secret-store`. The memories Role grants get on nine names, including `openbao-runtime-bootstrap`. No read through that store was attempted. Owner: Platform Operations / Memories Maintainer. Consequence: until this is closed, AC1's "outside the DAPR secret-store boundary" clause cannot hold, and the memories application can likely obtain the OpenBao runtime token through its own sidecar. Fix: disable the built-in store on every Dapr workload, or add a `kubernetes` deny scope to each Configuration, then apply and re-probe. Owner decision needed first: the base and production deployment files are PG-ONPREM-2 profile inputs, and component secretKeyRef resolution must be shown to survive the change. Reopen trigger: any change to Dapr workload annotations or Configuration secret scopes, or the next production overlay apply.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-2-runtime-dapr-secret-store-migration.md`
  summary: Run the runtime secret-store probe regression test (`tests/tooling/runtime_secret_store/test_scope_probe.py`) in an automated lane, with a BusyBox that supports `nc -e` on PATH.
  evidence: Story 31.2 review (verification gap, medium). No workflow, hook or script discovers `tests/tooling/runtime_secret_store`, and no runner installs BusyBox. The test is the only guard against a return of the half-close transport that made daprd cancel permitted OpenBao reads during the 15:18Z live replay. The fix edits `.github/workflows/ci.yml`, which Hexalith.CiCd owns, so propose it there. Owner: Memories Maintainer, with Hexalith.CiCd. Reopen trigger: any change to `tools/probe-dapr-secret-scope.sh`, or the next live replay.

- source_spec: `_bmad-output/implementation-artifacts/spec-31-2-runtime-dapr-secret-store-migration.md`
  summary: Establish whether `tools/verify-access-telemetry-c1-linkage.ps1` hits the same daprd cancel-on-half-close defect through its `printf ... | nc -w 5 127.0.0.1 3500` request (unverified medium).
  evidence: Story 31.2 review (maybe-false). The 31.2 evidence shows that BusyBox nc half-closes at stdin EOF and that daprd then cancels an in-flight secret-store read. The linkage tool sends state requests the same way at line 316. It is parked under DW-730, so nothing runs it today. Settle by replaying its request against a local daprd with an outbound-backed store, or by switching to the `nc -e` transport used by the 31.2 probe. Owner: Story 27.3 / Memories Maintainer. Reopen trigger: DW-730's production-enablement trigger fires.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-tenant-isolation-diagnostics.md`
  summary: Report incomplete Redis key coverage when a known cluster shard has no reachable primary.
  evidence: The scanner proceeds with any connected primary and does not account for expected slot ownership. A cluster-topology check needs to distinguish missing shard coverage from a disconnected seed or a completed failover.

### DW-737: Set a byte budget for individual stored values displayed in tenant isolation evidence.

origin: migrated from legacy ledger on 2026-11-24
location: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
source_spec: _bmad-output/implementation-artifacts/spec-five-deferred-tenant-isolation-diagnostics.md
reason: Sampling bounds the number of findings. A single unusually large key or stored tenant marker can still dominate the response. Define a display form that preserves evidence and lets operators identify affected keys.
status: done 2026-11-24
resolution: Oversized and non-UTF-8 key or stored tenant-marker references use a Unicode-safe prefix and SHA256 of the original Redis bytes. Focused tests verify the 256-byte budget, both scan axes, and remediation. See spec-ten-deferred-tenant-isolation-evidence.md.

### DW-738: Inspect surviving syntactic keys when the tenant index is missing and Redis is reachable.

origin: migrated from legacy ledger on 2026-11-24
location: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
source_spec: _bmad-output/implementation-artifacts/spec-five-deferred-tenant-isolation-diagnostics.md
reason: The missing-index branch returns provisioning guidance before the prefix scan. Existing hashes can still need provenance review before the index is recreated.
prior-evidence: 2026-11-23 — VerifyAsync_MissingSyntacticIndex_RetainsSurvivingKeyFindings proves the tenant prefix is scanned after the index disappears and that partial evidence survives a backend timeout.
status: done 2026-11-24
resolution: VerifyAsync_MissingSyntacticIndex_RetainsSurvivingKeyFindings passed for complete and interrupted scans. The previously implemented scan and its partial evidence were verified on this source. See spec-ten-deferred-tenant-isolation-evidence.md.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-integration-tooling.md`
  summary: Refuse Bash test results paths that resolve to the repository root.
  evidence: Independent review reproduced the pre-existing defect in a disposable copy. A dot-only results path passes the relative-path guard and lets report cleanup remove the checkout. Match the PowerShell wrapper's strict descendant check.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-integration-tooling.md`
  summary: Preserve Bash test failure details when dotnet exits without writing TRX.
  evidence: When dotnet fails before emitting the report, the pre-existing Bash report loop raises a missing-file error before reporting the native test process status. Preserve both the failure status and report diagnostics where a report exists.
  status: open

## Deferred from: review of spec-ten-deferred-story-scope-hooks.md (2026-11-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-ten-deferred-story-scope-hooks.md`
  summary: Represent literal Git filenames in the story review-readiness File List.
  evidence: A disposable repository proved C1 cannot represent a filename containing a literal newline or backslash, even when File Scope authorizes the parent glob. The Markdown path grammar needs a lossless representation for that case.

- source_spec: `_bmad-output/implementation-artifacts/spec-ten-deferred-story-scope-hooks.md`
  summary: Preserve literal Git filenames in the hosted story-scope job.
  evidence: The hosted job still sends Git's C-quoted, newline-delimited path list. A disposable reproduction showed the legacy parser reports a different path for a filename containing a newline. Hexalith.CiCd owns the workflow.

## Deferred from review of spec-ten-deferred-projection-registry.md (2026-11-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-ten-deferred-projection-registry.md`
  summary: Preserve concurrent observation writes when written-index compensation runs.
  evidence: After CAS exhaustion the observation store checks the written index and then deletes state for an absent aggregate. A writer can publish its observation and index entry between those calls. Coordinate compensation with the competing write and assert the resulting persisted observation and membership.

## Deferred from review of spec-ten-deferred-search-response-reliability.md (2026-11-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-ten-deferred-search-response-reliability.md`
  summary: Keep the hybrid fusion candidate set stable across pages.
  evidence: HybridSearchService sizes each axis candidate set from offset plus maxResults before fusion. With equal weights and axis rankings [A, X] and [B, X], the first page of size one returns A and the second can return A again after X gains two contributions. A stable retrieval window or continuation design needs its own ranking and latency validation.

## Deferred from review of spec-ten-ordered-deferred-work-follow-up.md (2026-11-24)

- source_spec: `_bmad-output/implementation-artifacts/spec-ten-ordered-deferred-work-follow-up.md`
  summary: Reuse a quickstart sample case when other cases push it outside the default case-list page.
  evidence: The server's default 100-case limit can omit an older sample before the wizard can match its name. The client and server now apply the same deterministic order. An exact-name filter or a lookup needs compatible REST and client behavior.

### DW-739: Review the shared Redis OpenTelemetry instrumentation pin for a stable release.

origin: DW-271 stable-release review, 2026-11-24
location: docs/dev/telemetry.md; NuGet.config; references/Hexalith.Builds/Props/Directory.Packages.props
reason: The current Builds pin is prerelease. The Memories release-manager rotation should review the official package feed within 14 days of its first stable release or by 2027-02-24, whichever comes first, and coordinate any pin change with Hexalith.Builds.
status: open
